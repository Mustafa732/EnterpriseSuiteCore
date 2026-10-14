using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;

using Common.Utils;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.AR.Setup
{
    public partial class AR_SET_Bank
    {


        #region Constant Queries



        const string Query_SelectAllForSearch_WithPaging = @" SELECT [BankID], [BankName], [Status], [IsActive], [CreatedBy], [CreateDate], [CreatedByValue], [UpdatedBy], [UpdateDate], [UpdatedByValue], ROW_NUMBER() Over (Order by BankName) as 'Row' FROM [AR_SET_Bank] ";
        const string Query_SelectAllForSearch_WithoutPaging = @" SELECT [BankID], [BankName], [Status], [IsActive], [CreatedBy], [CreateDate], [CreatedByValue], [UpdatedBy], [UpdateDate], [UpdatedByValue] FROM [AR_SET_Bank] ";



        #endregion




        #region Class Methods

        public bool Get_BankStatusFalse(int companyID, int periodID, int BankAccountID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@BankAccountID", BankAccountID);

                string Query_BankStatusFalse = @" Update AR_SET_BankAccountDetail Set AR_SET_BankAccountDetail.Status = 0 From AR_SET_BankAccountDetail 
    Inner JOIN AR_SET_BankAccount ON AR_SET_BankAccount.BankAccountID = AR_SET_BankAccountDetail.BankAccountID 
        AND AR_SET_BankAccount.CompanyID = AR_SET_BankAccountDetail.CompanyID AND AR_SET_BankAccount.PeriodID = AR_SET_BankAccountDetail.PeriodID 
        AND AR_SET_BankAccount.Status = 1 
Where AR_SET_BankAccountDetail.BankAccountID = @BankAccountID AND AR_SET_BankAccountDetail.PeriodID = @PeriodID ";

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_BankStatusFalse, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public DataSet GetBankStatusData(String companyID, int periodID, Nullable<int> bankID, Nullable<int> branchID, Nullable<DateTime> fromDate,
            Nullable<DateTime> toDate, Nullable<int> bankAccountTypeID, Nullable<int> bankAccountNo, bool discardReversedDocuments, Guid userID,
            string documentGroupID = "", Nullable<int> DocumentTypeID = null, bool documentTypeAuthentication = false, bool PostedDocuments = true)
        {
            DataSet dsToReturn;
            string filterReceiptHeader = string.Empty;
            string filterPaymentHeader = string.Empty;
            string filterReceiptDetail = string.Empty;
            string filterPaymentDetail = string.Empty;
            string bankBranch = string.Empty;
            string qry_docuTypeAuthenticate = string.Empty;
            string postedDocumnetsFillter = string.Empty;
            StringBuilder otherFilters = new StringBuilder();
            StringBuilder DocumentGroupFilter = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                #region Bank filter

                if (bankID.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@BankID", bankID.Value);
                    filterReceiptHeader += " AND AR_Set_BankBranch.BankID = @BankID ";
                    filterReceiptDetail += " AND AR_Set_BankBranch.BankID = @BankID ";
                    filterPaymentHeader += " AND AR_Set_BankBranch.BankID = @BankID ";
                    filterPaymentDetail += " AND AR_Set_BankBranch.BankID = @BankID ";
                    bankBranch += " AND AR_Set_BankBranch.BankID = @BankID ";
                }
                else
                    sqlparams[2] = new SqlParameter("@BankID", DBNull.Value);

                #endregion

                #region Branch filter

                if (branchID.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@BranchID", branchID.Value);
                    filterReceiptHeader += " AND AR_Set_BankBranch.BankBranchID = @BranchID ";
                    filterReceiptDetail += " AND AR_Set_BankBranch.BankBranchID = @BranchID ";
                    filterPaymentHeader += " AND AR_Set_BankBranch.BankBranchID = @BranchID ";
                    filterPaymentDetail += " AND AR_Set_BankBranch.BankBranchID = @BranchID ";
                    bankBranch += " AND AR_Set_BankBranch.BankBranchID = @BranchID ";
                }
                else
                    sqlparams[3] = new SqlParameter("@BranchID", DBNull.Value);

                #endregion

                #region Date Range filter

                if (fromDate.HasValue && toDate.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@Fromdate", fromDate);
                    sqlparams[5] = new SqlParameter("@ToDate", toDate);
                    filterReceiptHeader += " AND (AR_ReceiptTransactionHeader.DocumentDate < @FromDate Or AR_ReceiptTransactionHeader.DocumentDate Is Null) ";
                    filterReceiptDetail += " AND AR_ReceiptTransactionHeader.DocumentDate Between @FromDate AND @ToDate ";
                    filterPaymentHeader += " And (AP_PaymentHeader.DocumentDate < @FromDate Or AP_PaymentHeader.DocumentDate Is Null) ";
                    filterPaymentDetail += " AND AP_PaymentHeader.DocumentDate Between @FromDate AND @ToDate ";
                }
                else if (fromDate.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@Fromdate", fromDate);
                    filterReceiptHeader += " AND (AR_ReceiptTransactionHeader.DocumentDate < @FromDate Or AR_ReceiptTransactionHeader.DocumentDate Is Null) ";
                    filterReceiptDetail += " AND AR_ReceiptTransactionHeader.DocumentDate >= @FromDate ";
                    filterPaymentHeader += " And (AP_PaymentHeader.DocumentDate < @FromDate Or AP_PaymentHeader.DocumentDate Is Null) ";
                    filterPaymentDetail += " AND AP_PaymentHeader.DocumentDate >= @FromDate ";
                }
                else if (toDate.HasValue)
                {
                    sqlparams[5] = new SqlParameter("@ToDate", toDate);
                    filterReceiptDetail += " AND AR_ReceiptTransactionHeader.DocumentDate <= @ToDate ";
                    filterPaymentDetail += " AND AP_PaymentHeader.DocumentDate <= @ToDate ";
                }

                if (bankAccountTypeID != -1)
                    otherFilters.Append(" AND AR_SET_BankAccount.BankAccountTypeID = " + bankAccountTypeID + " ");

                if (bankAccountNo != -1)
                    otherFilters.Append(" AND AR_SET_BankAccount.BankAccountID = " + bankAccountNo + " ");

                if (documentGroupID != "")
                {
                    //otherFilters.Append(" AND Set_DocumentType.GroupID = " + documentGroupID + "");
                    DocumentGroupFilter.Append(" AND Set_DocumentType.GroupID in " + documentGroupID + "");
                }

                #endregion

                #region Filter Reversed Documents

                if (discardReversedDocuments)
                {
                    filterReceiptHeader += " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                    filterPaymentHeader += " And AP_PaymentHeader.ReversalComments is null ";
                    filterReceiptDetail += " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                    filterPaymentDetail += " And AP_PaymentHeader.ReversalComments is null ";
                }

                #endregion

                if (documentTypeAuthentication)
                {
                    sqlparams[6] = new SqlParameter("@UserID", userID);
//                    qry_docuTypeAuthenticate = @"      Inner Join (
//		Select distinct CompanyID, Documenttypeid, UserID from UsersInDocumentType 
//		)as UserIndoc on  UserIndoc.CompanyID = SET_DocumentType.CompanyID
//		AND UserIndoc.DocumentTypeID = SET_DocumentType.DocumentTypeID 
//		AND UserIndoc.UserID = @UserID  ";
                }

                if (PostedDocuments)
                    postedDocumnetsFillter = " where tab2.unPosted = 1";

                if (DocumentTypeID.HasValue)
                {
                    sqlparams[7] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                }
                else
                {
                    sqlparams[7] = new SqlParameter("@DocumentTypeID", DBNull.Value);
                }

                companyID = "(" + companyID + ")";

                string query = string.Format(FnI.QueryResourceLibrary.AR.AR_SET_Bank.Query_GetBankStatus.ToString().Replace("@CompanyID", companyID), filterReceiptHeader, filterPaymentHeader, filterReceiptDetail, filterPaymentDetail,
                    otherFilters.ToString(), DocumentGroupFilter.ToString(), qry_docuTypeAuthenticate, postedDocumnetsFillter, bankBranch);

                dsToReturn = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
                RemoveEmptyAccounts(ref dsToReturn);
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }


        public DataSet GetBankReconcileData(String companyID, int periodID, Nullable<int> bankID, Nullable<int> branchID, Nullable<DateTime> fromDate,
            Nullable<DateTime> toDate, Nullable<int> bankAccountTypeID, Nullable<int> bankAccountNo, bool discardReversedDocuments, Guid userID,
            string documentGroupID = "", Nullable<int> DocumentTypeID = null, bool documentTypeAuthentication = false, bool PostedDocuments = true)
        {
            DataSet dsToReturn;
            string filterReceiptHeader = string.Empty;
            string filterPaymentHeader = string.Empty;
            string filterReceiptDetail = string.Empty;
            string filterPaymentDetail = string.Empty;
            string bankBranch = string.Empty;
            string qry_docuTypeAuthenticate = string.Empty;
            string postedDocumnetsFillter = string.Empty;
            StringBuilder otherFilters = new StringBuilder();
            StringBuilder DocumentGroupFilter = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                #region Bank filter

                if (bankID.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@BankID", bankID.Value);
                    filterReceiptHeader += " AND AR_Set_BankBranch.BankID = @BankID ";
                    filterReceiptDetail += " AND AR_Set_BankBranch.BankID = @BankID ";
                    filterPaymentHeader += " AND AR_Set_BankBranch.BankID = @BankID ";
                    filterPaymentDetail += " AND AR_Set_BankBranch.BankID = @BankID ";
                    bankBranch += " AND AR_Set_BankBranch.BankID = @BankID ";
                }
                else
                    sqlparams[2] = new SqlParameter("@BankID", DBNull.Value);

                #endregion

                #region Branch filter

                if (branchID.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@BranchID", branchID.Value);
                    filterReceiptHeader += " AND AR_Set_BankBranch.BankBranchID = @BranchID ";
                    filterReceiptDetail += " AND AR_Set_BankBranch.BankBranchID = @BranchID ";
                    filterPaymentHeader += " AND AR_Set_BankBranch.BankBranchID = @BranchID ";
                    filterPaymentDetail += " AND AR_Set_BankBranch.BankBranchID = @BranchID ";
                    bankBranch += " AND AR_Set_BankBranch.BankBranchID = @BranchID ";
                }
                else
                    sqlparams[3] = new SqlParameter("@BranchID", DBNull.Value);

                #endregion

                #region Date Range filter

                if (fromDate.HasValue && toDate.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@Fromdate", fromDate);
                    sqlparams[5] = new SqlParameter("@ToDate", toDate);
                    filterReceiptHeader += " AND (AR_ReceiptTransactionHeader.DocumentDate < @FromDate Or AR_ReceiptTransactionHeader.DocumentDate Is Null) ";
                    filterReceiptDetail += " AND AR_ReceiptTransactionHeader.DocumentDate Between @FromDate AND @ToDate ";
                    filterPaymentHeader += " And (AP_PaymentHeader.DocumentDate < @FromDate Or AP_PaymentHeader.DocumentDate Is Null) ";
                    filterPaymentDetail += " AND AP_PaymentHeader.DocumentDate Between @FromDate AND @ToDate ";
                }
                else if (fromDate.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@Fromdate", fromDate);
                    filterReceiptHeader += " AND (AR_ReceiptTransactionHeader.DocumentDate < @FromDate Or AR_ReceiptTransactionHeader.DocumentDate Is Null) ";
                    filterReceiptDetail += " AND AR_ReceiptTransactionHeader.DocumentDate >= @FromDate ";
                    filterPaymentHeader += " And (AP_PaymentHeader.DocumentDate < @FromDate Or AP_PaymentHeader.DocumentDate Is Null) ";
                    filterPaymentDetail += " AND AP_PaymentHeader.DocumentDate >= @FromDate ";
                }
                else if (toDate.HasValue)
                {
                    sqlparams[5] = new SqlParameter("@ToDate", toDate);
                    filterReceiptDetail += " AND AR_ReceiptTransactionHeader.DocumentDate <= @ToDate ";
                    filterPaymentDetail += " AND AP_PaymentHeader.DocumentDate <= @ToDate ";
                }

                if (bankAccountTypeID != -1)
                    otherFilters.Append(" AND AR_SET_BankAccount.BankAccountTypeID = " + bankAccountTypeID + " ");

                if (bankAccountNo != -1)
                    otherFilters.Append(" AND AR_SET_BankAccount.BankAccountID = " + bankAccountNo + " ");

                if (documentGroupID != "")
                {
                    DocumentGroupFilter.Append(" AND Set_DocumentType.GroupID in " + documentGroupID + "");
                }

                #endregion

                #region Filter Reversed Documents

                if (discardReversedDocuments)
                {
                    filterReceiptHeader += " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                    filterPaymentHeader += " And AP_PaymentHeader.ReversalComments is null ";
                    filterReceiptDetail += " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                    filterPaymentDetail += " And AP_PaymentHeader.ReversalComments is null ";
                }

                #endregion

                if (documentTypeAuthentication)
                {
                    sqlparams[6] = new SqlParameter("@UserID", userID);
                }

                if (PostedDocuments)
                    postedDocumnetsFillter = " where tab2.unPosted = 1";

                if (DocumentTypeID.HasValue)
                {
                    sqlparams[7] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                }
                else
                {
                    sqlparams[7] = new SqlParameter("@DocumentTypeID", DBNull.Value);
                }

                companyID = "(" + companyID + ")";

                string query = string.Format(FnI.QueryResourceLibrary.AR.AR_SET_Bank.Query_GetBankReconcile.ToString().Replace("@CompanyID", companyID), filterReceiptHeader, filterPaymentHeader, filterReceiptDetail, filterPaymentDetail,
                    otherFilters.ToString(), DocumentGroupFilter.ToString(), qry_docuTypeAuthenticate, postedDocumnetsFillter, bankBranch);

                dsToReturn = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
                RemoveEmptyAccounts(ref dsToReturn);
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }


        public DataSet GetSummaryOfDailyFundsPosition(int companyID, int periodID, Nullable<DateTime> toDate)
        {
            DataSet dsToReturn;
           
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@AsOnDate", toDate);

                dsToReturn = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_SET_Bank.Query_SummaryOfDailyFundsPosition, sqlparams);
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public DataSet GetSummaryOfDailyFundsPositionForCE(int companyID, int periodID, Nullable<DateTime> toDate)
        {
            DataSet dsToReturn;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@AsOnDate", toDate);

                dsToReturn = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_SET_Bank.Query_SummaryOfDailyFundsPositionForCE, sqlparams);
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }


        public DataSet GetBankReconciliationData(int companyID, int periodID, Nullable<int> bankID, Nullable<int> branchID, Nullable<DateTime> fromDate,
            Nullable<DateTime> toDate, Nullable<int> bankAccountTypeID, int? BankAccountID)
        {
            DataSet dsToReturn;
            string filterReceiptHeader = string.Empty;
            string filterPaymentHeader = string.Empty;
            string filterReceiptDetail = string.Empty;
            string filterPaymentDetail = string.Empty;
            StringBuilder otherFilters = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                #region Bank filter

                if (bankID.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@BankID", bankID.Value);
                    filterReceiptHeader += " AND AR_Set_BankBranch.BankID = @BankID ";
                    filterReceiptDetail += " AND AR_Set_BankBranch.BankID = @BankID ";
                    filterPaymentHeader += " AND AR_Set_BankBranch.BankID = @BankID ";
                    filterPaymentDetail += " AND AR_Set_BankBranch.BankID = @BankID ";
                }
                else
                    sqlparams[2] = new SqlParameter("@BankID", DBNull.Value);

                #endregion

                #region Branch filter

                if (branchID.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@BranchID", branchID.Value);
                    filterReceiptHeader += " AND AR_Set_BankBranch.BankBranchID = @BranchID ";
                    filterReceiptDetail += " AND AR_Set_BankBranch.BankBranchID = @BranchID ";
                    filterPaymentHeader += " AND AR_Set_BankBranch.BankBranchID = @BranchID ";
                    filterPaymentDetail += " AND AR_Set_BankBranch.BankBranchID = @BranchID ";
                }
                else
                    sqlparams[3] = new SqlParameter("@BranchID", DBNull.Value);

                #endregion

                #region Date Range filter

                if (fromDate.HasValue && toDate.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@Fromdate", fromDate);
                    sqlparams[5] = new SqlParameter("@ToDate", toDate);
                    filterReceiptHeader += " AND (AR_ReceiptTransactionHeader.DocumentDate < @FromDate Or AR_ReceiptTransactionHeader.DocumentDate Is Null) ";
                    filterReceiptDetail += " AND AR_ReceiptTransactionHeader.DocumentDate Between @FromDate AND @ToDate ";
                    filterPaymentHeader += " And (AP_PaymentHeader.DocumentDate < @FromDate Or AP_PaymentHeader.DocumentDate Is Null) ";
                    filterPaymentDetail += " AND AP_PaymentHeader.DocumentDate Between @FromDate AND @ToDate ";
                }
                else if (fromDate.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@Fromdate", fromDate);
                    filterReceiptHeader += " AND (AR_ReceiptTransactionHeader.DocumentDate < @FromDate Or AR_ReceiptTransactionHeader.DocumentDate Is Null) ";
                    filterReceiptDetail += " AND AR_ReceiptTransactionHeader.DocumentDate >= @FromDate ";
                    filterPaymentHeader += " And (AP_PaymentHeader.DocumentDate < @FromDate Or AP_PaymentHeader.DocumentDate Is Null) ";
                    filterPaymentDetail += " AND AP_PaymentHeader.DocumentDate >= @FromDate ";
                }
                else if (toDate.HasValue)
                {
                    sqlparams[5] = new SqlParameter("@ToDate", toDate);
                    filterReceiptDetail += " AND AR_ReceiptTransactionHeader.DocumentDate <= @ToDate ";
                    filterPaymentDetail += " AND AP_PaymentHeader.DocumentDate <= @ToDate ";
                }

                if (bankAccountTypeID != -1)
                    otherFilters.Append(" AND AR_SET_BankAccount.BankAccountTypeID = " + bankAccountTypeID + " ");

                #endregion

                #region Bank Account ID
                if (BankAccountID.HasValue)
                {
                    sqlparams[6] = new SqlParameter("@BankAccountID", BankAccountID.Value);
                    filterReceiptHeader += " AND AR_Set_BankAccount.BankAccountID = @BankAccountID ";
                    filterReceiptDetail += " AND AR_Set_BankAccount.BankAccountID = @BankAccountID ";
                    filterPaymentHeader += " AND AR_Set_BankAccount.BankAccountID = @BankAccountID ";
                    filterPaymentDetail += " AND AR_Set_BankAccount.BankAccountID = @BankAccountID ";
                }
                #endregion

                string query = string.Format(FnI.QueryResourceLibrary.AR.AR_SET_Bank.Query_GetBankReconciliationData, filterReceiptHeader, filterPaymentHeader, filterReceiptDetail, filterPaymentDetail, otherFilters.ToString());

                dsToReturn = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
                RemoveEmptyAccounts(ref dsToReturn);
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public DataSet GetBankReconciliationDetailData(int companyID, int periodID, Nullable<int> bankID, Nullable<int> branchID, Nullable<DateTime> fromDate,
          Nullable<DateTime> toDate, Nullable<int> bankAccountTypeID, int DocumentNo)
        {
            DataSet dsToReturn;

            StringBuilder otherFilters = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[6] = new SqlParameter("@BankReconcilationID", DocumentNo);
                #region Bank filter

                if (bankID.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@BankID", bankID.Value);
                    otherFilters.Append(" AND AR_Set_BankBranch.BankID = @BankID ");

                }
                else
                    sqlparams[2] = new SqlParameter("@BankID", DBNull.Value);

                #endregion

                #region Branch filter

                if (branchID.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@BranchID", branchID.Value);
                    otherFilters.Append(" AND AR_Set_BankBranch.BankBranchID = @BranchID ");

                }
                else
                    sqlparams[3] = new SqlParameter("@BranchID", DBNull.Value);

                #endregion

                #region Date Range filter

                if (fromDate.HasValue && toDate.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@Fromdate", fromDate);
                    sqlparams[5] = new SqlParameter("@ToDate", toDate);

                    otherFilters.Append(" AND BankReconcilation_Header.DocumentDate Between @FromDate AND @ToDate ");

                }
                else if (fromDate.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@Fromdate", fromDate);

                    otherFilters.Append(" AND BankReconcilation_Header.DocumentDate >= @FromDate ");

                }
                else if (toDate.HasValue)
                {
                    sqlparams[5] = new SqlParameter("@ToDate", toDate);
                    otherFilters.Append(" AND BankReconcilation_Header.DocumentDate <= @ToDate ");

                }

                if (bankAccountTypeID != -1)
                    otherFilters.Append(" AND AR_SET_BankAccount.BankAccountTypeID = " + bankAccountTypeID + " ");

                #endregion

                #region Query

                string Query_GetBankReconciliationData_Copy = @" 
SELECT BankReconcilation_Header.BankReconcilationID,BankReconcilation_Detail.BankReconcilationDetailID
,BankReconcilation_Header.BankNameID as BankID ,BankReconcilation_Header.BankBranchID,AR_Set_Bank.BankName,AR_Set_bankBranch.BankBranchName,
BankReconcilation_Header.BankAccountNameID,AR_Set_bankAccount.BankAccountNumber,AR_Set_bankAccount.BankAccountName,
Case When BankReconcilation_Detail.ItemName = 'Opening Balance' Then IsNull(BankReconcilation_Detail.Balance, 0)
 Else IsNull(BankReconcilation_Detail.Balance, 0) * -1 End As 
TotOpeningBalance,BankReconcilation_Detail.Balance,
BankReconcilation_Detail.ChequeNo As PaymentServerOrderNo,
BankReconcilation_Detail.DocumentNo,BankReconcilation_Detail.DocumentDate,ItemName,BankReconcilation_Detail.Debit AS CurrentBalanceDebit,
BankReconcilation_Detail.Credit AS CurrentBalanceCredit,BankReconcilation_Detail.ReportedByBank,BankReconcilation_Detail.ReportedDifferentAmount
,BankReconcilation_Detail.NoteReportedAmount,
0 as ReportedDifferentAmountCredit,
0 as ReportedDifferentAmountDebit 
--,DocumentType
,Case When BankReconcilation_Detail.ItemName = 'Opening Balance' Then IsNull(BankReconcilation_Detail.Balance,0)
else 0
 End As OpeningBalance
 ,
'' as Description,BankReconcilation_Detail.ReportedByDate

FROM BankReconcilation_Header
INNER JOIN BankReconcilation_Detail on BankReconcilation_Detail.BankReconcilationID = BankReconcilation_Header.BankReconcilationID
INNER JOIN AR_Set_bankBranch On AR_Set_bankBranch.BankBranchID = BankReconcilation_Header.BankBranchID 
INNER JOIN AR_Set_Bank ON AR_Set_bank.BankID = BankReconcilation_Header.BankNameID 
INNER JOIN AR_Set_bankAccount on AR_Set_bankAccount.BankAccountID = BankReconcilation_Header.BankAccountNameID
WHERE BankReconcilation_Header.BankReconcilationID = @BankReconcilationID

-- Not Reported
SELECT BankReconcilation_Header.BankReconcilationID,BankReconcilation_Detail.BankReconcilationDetailID
,BankReconcilation_Header.BankNameID as BankID,BankReconcilation_Header.BankBranchID,AR_Set_Bank.BankName,AR_Set_bankBranch.BankBranchName,
BankReconcilation_Header.BankAccountNameID,AR_Set_bankAccount.BankAccountNumber,AR_Set_bankAccount.BankAccountName,
Case When BankReconcilation_Detail.ItemName = 'Opening Balance' Then IsNull(BankReconcilation_Detail.Balance, 0)
 Else IsNull(BankReconcilation_Detail.Balance, 0) * -1 End As 
TotOpeningBalance,BankReconcilation_Detail.Balance,
BankReconcilation_Detail.ChequeNo As PaymentServerOrderNo,
BankReconcilation_Detail.DocumentNo,BankReconcilation_Detail.DocumentDate,ItemName,BankReconcilation_Detail.Debit AS CurrentBalanceDebit,
BankReconcilation_Detail.Credit AS CurrentBalanceCredit,BankReconcilation_Detail.ReportedByBank,BankReconcilation_Detail.ReportedDifferentAmount
,BankReconcilation_Detail.NoteReportedAmount,
0 as ReportedDifferentAmountCredit,
0 as ReportedDifferentAmountDebit 
--,DocumentType
,Case When BankReconcilation_Detail.ItemName = 'Opening Balance' Then IsNull(BankReconcilation_Detail.Balance,0)
else 0
 End As OpeningBalance
 ,
'' as Description,BankReconcilation_Detail.ReportedByDate

FROM BankReconcilation_Header
INNER JOIN BankReconcilation_Detail on BankReconcilation_Detail.BankReconcilationID = BankReconcilation_Header.BankReconcilationID

INNER JOIN AR_Set_bankBranch On AR_Set_bankBranch.BankBranchID = BankReconcilation_Header.BankBranchID 
INNER JOIN AR_Set_Bank ON AR_Set_bank.BankID = BankReconcilation_Header.BankNameID 
INNER JOIN AR_Set_bankAccount on AR_Set_bankAccount.BankAccountID = BankReconcilation_Header.BankAccountNameID
WHERE BankReconcilation_Header.BankReconcilationID = @BankReconcilationID
and BankReconcilation_Detail.ReportedByBank = 0


-- Reported Different 
SELECT BankReconcilation_Header.BankReconcilationID,BankReconcilation_Detail.BankReconcilationDetailID
,BankReconcilation_Header.BankNameID as BankID,BankReconcilation_Header.BankBranchID,AR_Set_Bank.BankName,AR_Set_bankBranch.BankBranchName,
BankReconcilation_Header.BankAccountNameID,AR_Set_bankAccount.BankAccountNumber,AR_Set_bankAccount.BankAccountName,
Case When BankReconcilation_Detail.ItemName = 'Opening Balance' Then IsNull(BankReconcilation_Detail.Balance, 0)
 Else IsNull(BankReconcilation_Detail.Balance, 0) * -1 End As 
TotOpeningBalance,BankReconcilation_Detail.Balance,
BankReconcilation_Detail.ChequeNo As PaymentServerOrderNo,
BankReconcilation_Detail.DocumentNo,BankReconcilation_Detail.DocumentDate,ItemName,BankReconcilation_Detail.Debit AS CurrentBalanceDebit,
BankReconcilation_Detail.Credit AS CurrentBalanceCredit,BankReconcilation_Detail.ReportedByBank,BankReconcilation_Detail.ReportedDifferentAmount
,BankReconcilation_Detail.NoteReportedAmount,
0 as ReportedDifferentAmountCredit,
0 as ReportedDifferentAmountDebit 
--,DocumentType
,Case When BankReconcilation_Detail.ItemName = 'Opening Balance' Then IsNull(BankReconcilation_Detail.Balance,0)
else 0
 End As OpeningBalance
 ,
''as Description,BankReconcilation_Detail.ReportedByDate

,
case when isnull(ReportedDifferentAmount,0) > 0 and isnull(BankReconcilation_Detail.Credit,0) > 0  then
ReportedDifferentAmount - BankReconcilation_Detail.Credit 

when isnull(ReportedDifferentAmount,0) > 0 and isnull(BankReconcilation_Detail.Debit,0) > 0  then
ReportedDifferentAmount + BankReconcilation_Detail.Debit


 when isnull(NoteReportedAmount,0) > 0 and isnull(BankReconcilation_Detail.Credit,0) > 0  then
BankReconcilation_Detail.Credit  + NoteReportedAmount 

when isnull(NoteReportedAmount,0) > 0 and isnull(BankReconcilation_Detail.Debit,0) > 0  then
BankReconcilation_Detail.Debit - NoteReportedAmount 


else 0 end 
as 
QueryBalnce
FROM BankReconcilation_Header
INNER JOIN BankReconcilation_Detail on BankReconcilation_Detail.BankReconcilationID = BankReconcilation_Header.BankReconcilationID
INNER JOIN AR_Set_bankBranch On AR_Set_bankBranch.BankBranchID = BankReconcilation_Header.BankBranchID 
INNER JOIN AR_Set_Bank ON AR_Set_bank.BankID = BankReconcilation_Header.BankNameID 
INNER JOIN AR_Set_bankAccount on AR_Set_bankAccount.BankAccountID = BankReconcilation_Header.BankAccountNameID
WHERE BankReconcilation_Header.BankReconcilationID = @BankReconcilationID
and (isnull(BankReconcilation_Detail.ReportedDifferentAmount,0) > 0
or isnull(BankReconcilation_Detail.NoteReportedAmount,0) > 0)

--Reported Different Amount

SELECT 
BankReconcilationSubDetail.BankReconcilationID,
BankReconcilationSubDetail.BankReconcilationSubDetailID,
BankReconcilationSubDetail.ReportedDifferentAmountCredit,
BankReconcilationSubDetail.ReportedDifferentAmountDebit ,
BankReconcilationSubDetail.Description,BankReconcilation_Header.BankNameID as BankID,BankReconcilation_Header.BankBranchID,
BankReconcilation_Header.BankAccountNameID,BankReconcilationSubDetail.Date


FROM BankReconcilationSubDetail
INNER JOIN BankReconcilation_Header on BankReconcilationSubDetail.BankReconcilationID = BankReconcilation_Header.BankReconcilationID



WHERE BankReconcilationSubDetail.BankReconcilationID = @BankReconcilationID
and (isnull(BankReconcilationSubDetail.ReportedDifferentAmountDebit,0) > 0
or isnull(BankReconcilationSubDetail.ReportedDifferentAmountCredit,0) > 0)
And BankReconcilationSubDetail.Status= 1

";

                #endregion

                //string query = string.Format(Query_GetBankReconciliationData, filterReceiptHeader, filterPaymentHeader, filterReceiptDetail, filterPaymentDetail, otherFilters.ToString());

                string query = string.Format(FnI.QueryResourceLibrary.AR.AR_SET_Bank.Query_GetBankReconciliationData_Copy, otherFilters.ToString());

                dsToReturn = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

                dsToReturn.Tables[0].TableName = "Tab1";
                dsToReturn.Tables[1].TableName = "Tab2";
                dsToReturn.Tables[2].TableName = "Tab3";
                dsToReturn.Tables[3].TableName = "Tab4";

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public DataSet GetBankReconciliationDetailWiseData(int companyID, int periodID, Nullable<int> bankID, Nullable<int> branchID, Nullable<DateTime> fromDate,
         Nullable<DateTime> toDate, Nullable<int> bankAccountTypeID, int DocumentNo)
        {
            DataSet dsToReturn;

            StringBuilder otherFilters = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[4] = new SqlParameter("@FromDate", fromDate);
                sqlparams[5] = new SqlParameter("@DateTo", toDate);
                //sqlparams[6] = new SqlParameter("@BankReconcilationID", DocumentNo);
                #region Bank filter

                if (bankID.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@BankID", bankID.Value);
                    otherFilters.Append(" AND AR_Set_BankBranch.BankID = @BankID ");

                }
                else
                    sqlparams[2] = new SqlParameter("@BankID", DBNull.Value);

                #endregion

                #region Branch filter

                if (branchID.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@BranchID", branchID.Value);
                    otherFilters.Append(" AND AR_Set_BankBranch.BankBranchID = @BranchID ");

                }
                else
                    sqlparams[3] = new SqlParameter("@BranchID", DBNull.Value);

                #endregion

                if (bankAccountTypeID != -1)
                {
                    otherFilters.Append(" AND AR_SET_BankAccount.BankAccountTypeID = " + bankAccountTypeID + " ");
                }
                if (DocumentNo > 0)
                {
                    otherFilters.Append(" AND BankReconcilation_Header.BankReconcilationID  = " + DocumentNo + " ");
                }


                #region Query
                string Query_GetBankReconciliationWiseData = @" 
SELECT BankReconcilation_Header.BankReconcilationID,BankReconcilation_Detail.BankReconcilationDetailID
,BankReconcilation_Header.BankNameID as BankID ,BankReconcilation_Header.BankBranchID,AR_Set_Bank.BankName,AR_Set_bankBranch.BankBranchName,
BankReconcilation_Header.BankAccountNameID,AR_Set_bankAccount.BankAccountNumber,AR_Set_bankAccount.BankAccountName,
Case When BankReconcilation_Detail.ItemName = 'Opening Balance' Then IsNull(BankReconcilation_Detail.Balance, 0)
 Else IsNull(BankReconcilation_Detail.Balance, 0) * -1 End As 
TotOpeningBalance,BankReconcilation_Detail.Balance,
BankReconcilation_Detail.ChequeNo As PaymentServerOrderNo,
BankReconcilation_Detail.DocumentNo,BankReconcilation_Detail.DocumentDate,ItemName,BankReconcilation_Detail.Debit AS CurrentBalanceDebit,
BankReconcilation_Detail.Credit AS CurrentBalanceCredit,BankReconcilation_Detail.ReportedByBank,BankReconcilation_Detail.ReportedDifferentAmount
,BankReconcilation_Detail.NoteReportedAmount,
0 as ReportedDifferentAmountCredit,
0 as ReportedDifferentAmountDebit 
--,DocumentType
,Case When BankReconcilation_Detail.ItemName = 'Opening Balance' Then IsNull(BankReconcilation_Detail.Balance,0)
else 0
 End As OpeningBalance
 ,
'' as Description,BankReconcilation_Detail.ReportedByDate

FROM BankReconcilation_Header
INNER JOIN BankReconcilation_Detail on BankReconcilation_Detail.BankReconcilationID = BankReconcilation_Header.BankReconcilationID
INNER JOIN AR_Set_bankBranch On AR_Set_bankBranch.BankBranchID = BankReconcilation_Header.BankBranchID 
INNER JOIN AR_Set_Bank ON AR_Set_bank.BankID = BankReconcilation_Header.BankNameID 
INNER JOIN AR_Set_bankAccount on AR_Set_bankAccount.BankAccountID = BankReconcilation_Header.BankAccountNameID
WHERE BankReconcilation_Detail.ReportedByDate Between @FromDate AND @DateTo
Order By BankReconcilation_Header.BankReconcilationID

-- Not Reported
SELECT BankReconcilation_Header.BankReconcilationID,BankReconcilation_Detail.BankReconcilationDetailID
,BankReconcilation_Header.BankNameID as BankID,BankReconcilation_Header.BankBranchID,AR_Set_Bank.BankName,AR_Set_bankBranch.BankBranchName,
BankReconcilation_Header.BankAccountNameID,AR_Set_bankAccount.BankAccountNumber,AR_Set_bankAccount.BankAccountName,
Case When BankReconcilation_Detail.ItemName = 'Opening Balance' Then IsNull(BankReconcilation_Detail.Balance, 0)
 Else IsNull(BankReconcilation_Detail.Balance, 0) * -1 End As 
TotOpeningBalance,BankReconcilation_Detail.Balance,
BankReconcilation_Detail.ChequeNo As PaymentServerOrderNo,
BankReconcilation_Detail.DocumentNo,BankReconcilation_Detail.DocumentDate,ItemName,BankReconcilation_Detail.Debit AS CurrentBalanceDebit,
BankReconcilation_Detail.Credit AS CurrentBalanceCredit,BankReconcilation_Detail.ReportedByBank,BankReconcilation_Detail.ReportedDifferentAmount
,BankReconcilation_Detail.NoteReportedAmount,
0 as ReportedDifferentAmountCredit,
0 as ReportedDifferentAmountDebit 
--,DocumentType
,Case When BankReconcilation_Detail.ItemName = 'Opening Balance' Then IsNull(BankReconcilation_Detail.Balance,0)
else 0
 End As OpeningBalance
 ,
'' as Description,BankReconcilation_Detail.ReportedByDate

FROM BankReconcilation_Header
INNER JOIN BankReconcilation_Detail on BankReconcilation_Detail.BankReconcilationID = BankReconcilation_Header.BankReconcilationID

INNER JOIN AR_Set_bankBranch On AR_Set_bankBranch.BankBranchID = BankReconcilation_Header.BankBranchID 
INNER JOIN AR_Set_Bank ON AR_Set_bank.BankID = BankReconcilation_Header.BankNameID 
INNER JOIN AR_Set_bankAccount on AR_Set_bankAccount.BankAccountID = BankReconcilation_Header.BankAccountNameID
WHERE BankReconcilation_Detail.ReportedByDate Between @FromDate AND @DateTo
and BankReconcilation_Detail.ReportedByBank = 0
Order By BankReconcilation_Header.BankReconcilationID

-- Reported Different 
SELECT BankReconcilation_Header.BankReconcilationID,BankReconcilation_Detail.BankReconcilationDetailID
,BankReconcilation_Header.BankNameID as BankID,BankReconcilation_Header.BankBranchID,AR_Set_Bank.BankName,AR_Set_bankBranch.BankBranchName,
BankReconcilation_Header.BankAccountNameID,AR_Set_bankAccount.BankAccountNumber,AR_Set_bankAccount.BankAccountName,
Case When BankReconcilation_Detail.ItemName = 'Opening Balance' Then IsNull(BankReconcilation_Detail.Balance, 0)
 Else IsNull(BankReconcilation_Detail.Balance, 0) * -1 End As 
TotOpeningBalance,BankReconcilation_Detail.Balance,
BankReconcilation_Detail.ChequeNo As PaymentServerOrderNo,
BankReconcilation_Detail.DocumentNo,BankReconcilation_Detail.DocumentDate,ItemName,BankReconcilation_Detail.Debit AS CurrentBalanceDebit,
BankReconcilation_Detail.Credit AS CurrentBalanceCredit,BankReconcilation_Detail.ReportedByBank,BankReconcilation_Detail.ReportedDifferentAmount
,BankReconcilation_Detail.NoteReportedAmount,
0 as ReportedDifferentAmountCredit,
0 as ReportedDifferentAmountDebit 
--,DocumentType
,Case When BankReconcilation_Detail.ItemName = 'Opening Balance' Then IsNull(BankReconcilation_Detail.Balance,0)
else 0
 End As OpeningBalance
 ,
''as Description,BankReconcilation_Detail.ReportedByDate

,
case when isnull(ReportedDifferentAmount,0) > 0 and isnull(BankReconcilation_Detail.Credit,0) > 0  then
ReportedDifferentAmount - BankReconcilation_Detail.Credit 

when isnull(ReportedDifferentAmount,0) > 0 and isnull(BankReconcilation_Detail.Debit,0) > 0  then
ReportedDifferentAmount + BankReconcilation_Detail.Debit


 when isnull(NoteReportedAmount,0) > 0 and isnull(BankReconcilation_Detail.Credit,0) > 0  then
BankReconcilation_Detail.Credit  + NoteReportedAmount 

when isnull(NoteReportedAmount,0) > 0 and isnull(BankReconcilation_Detail.Debit,0) > 0  then
BankReconcilation_Detail.Debit - NoteReportedAmount 


else 0 end 
as 
QueryBalnce
FROM BankReconcilation_Header
INNER JOIN BankReconcilation_Detail on BankReconcilation_Detail.BankReconcilationID = BankReconcilation_Header.BankReconcilationID
INNER JOIN AR_Set_bankBranch On AR_Set_bankBranch.BankBranchID = BankReconcilation_Header.BankBranchID 
INNER JOIN AR_Set_Bank ON AR_Set_bank.BankID = BankReconcilation_Header.BankNameID 
INNER JOIN AR_Set_bankAccount on AR_Set_bankAccount.BankAccountID = BankReconcilation_Header.BankAccountNameID
WHERE BankReconcilation_Detail.ReportedByDate Between @FromDate AND @DateTo
and (isnull(BankReconcilation_Detail.ReportedDifferentAmount,0) > 0
or isnull(BankReconcilation_Detail.NoteReportedAmount,0) > 0)
Order By BankReconcilation_Header.BankReconcilationID
--Reported Different Amount

SELECT 
BankReconcilationSubDetail.BankReconcilationID,
BankReconcilationSubDetail.BankReconcilationSubDetailID,
BankReconcilationSubDetail.ReportedDifferentAmountCredit,
BankReconcilationSubDetail.ReportedDifferentAmountDebit ,
BankReconcilationSubDetail.Description,BankReconcilation_Header.BankNameID as BankID,BankReconcilation_Header.BankBranchID,
BankReconcilation_Header.BankAccountNameID,BankReconcilationSubDetail.Date


FROM BankReconcilationSubDetail
INNER JOIN BankReconcilation_Header on BankReconcilationSubDetail.BankReconcilationID = BankReconcilation_Header.BankReconcilationID



WHERE BankReconcilationSubDetail.Date Between @FromDate AND @DateTo
and (isnull(BankReconcilationSubDetail.ReportedDifferentAmountDebit,0) > 0
or isnull(BankReconcilationSubDetail.ReportedDifferentAmountCredit,0) > 0)
And BankReconcilationSubDetail.Status= 1
Order By BankReconcilation_Header.BankReconcilationID
";

                #endregion

                //string query = string.Format(Query_GetBankReconciliationData, filterReceiptHeader, filterPaymentHeader, filterReceiptDetail, filterPaymentDetail, otherFilters.ToString());

                string query = string.Format(Query_GetBankReconciliationWiseData, otherFilters.ToString());

                dsToReturn = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

                dsToReturn.Tables[0].TableName = "Tab1";
                dsToReturn.Tables[1].TableName = "Tab2";
                dsToReturn.Tables[2].TableName = "Tab3";
                dsToReturn.Tables[3].TableName = "Tab4";

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }


        public DataSet GetBankAccountStatusData(string companyID, int PeriodId, Nullable<int> bankID, Nullable<int> branchID, Nullable<DateTime> DateFrom,
            Nullable<DateTime> DateTo, Nullable<int> bankAccountTypeID, Nullable<int> bankAccountNo, bool discardReversedDocuments, Guid userID, string documentGroupID = "", Nullable<int> DocumentTypeID = null, bool documentTypeAuthentication = false, bool PostedDocuments = true)
        {
            DataSet ds = null;
            StringBuilder additionalFilter = new StringBuilder();
            string qry_ReceiptFilter = string.Empty, qry_PaymentFilter = string.Empty, qry_docuTypeAuthenticate = string.Empty;
            StringBuilder DocumentGroupFilter = new StringBuilder();

            #region Filter Reversed Documents

            if (discardReversedDocuments)
            {
                qry_ReceiptFilter = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                qry_PaymentFilter = " And AP_PaymentHeader.ReversalComments is null ";
            }

            if (PostedDocuments)
            {
                qry_ReceiptFilter = " And AR_ReceiptTransactionHeader.StatusCode = 1 ";
                qry_PaymentFilter = " And AP_PaymentHeader.StatusCode = 1 ";
            }
            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodId);

                #region Filter by Bank

                if (bankID.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@BankID", bankID.Value);
                    additionalFilter.Append(" And AR_Set_bank.BankID = @BankID ");
                }
                else
                    sqlparams[2] = new SqlParameter("@BankID", DBNull.Value);

                #endregion

                #region Filter by Bank Branch

                if (branchID.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@BranchID", branchID.Value);
                    additionalFilter.Append(" And AR_SET_BankBranch.BankBranchID = @BranchID ");
                }

                #endregion

                if (bankAccountTypeID != -1)
                    additionalFilter.Append(" AND AR_SET_BankAccount.BankAccountTypeID = " + bankAccountTypeID + " ");

                if (bankAccountNo != -1)
                    additionalFilter.Append(" AND AR_SET_BankAccount.BankAccountID = " + bankAccountNo + " ");

                if (documentGroupID != "")
                {
                    DocumentGroupFilter.Append(" AND Set_DocumentType.GroupID in " + documentGroupID); // {3}
                }

                sqlparams[4] = new SqlParameter("@Fromdate", DateFrom);
                sqlparams[5] = new SqlParameter("@Dateto", DateTo);

                if (documentTypeAuthentication)
                {
                    sqlparams[6] = new SqlParameter("@UserID", userID);
                    qry_docuTypeAuthenticate = @" Inner Join (Select distinct CompanyID, Documenttypeid, UserID from UsersInDocumentType 
		                                            )as UserIndoc on  UserIndoc.CompanyID = SET_DocumentType.CompanyID
		                                            AND UserIndoc.DocumentTypeID = SET_DocumentType.DocumentTypeID 
		                                            AND UserIndoc.UserID = @UserID  ";
                }

                if (DocumentTypeID.HasValue)
                {
                    sqlparams[7] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                }
                else
                {
                    sqlparams[7] = new SqlParameter("@DocumentTypeID", DBNull.Value);
                }
                companyID = "(" + companyID + ")";

                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_SET_Bank.Query_GetBankAccountStatus.ToString().Replace("@CompanyID", companyID), additionalFilter.ToString(), qry_ReceiptFilter,
                    qry_PaymentFilter, DocumentGroupFilter.ToString(), qry_docuTypeAuthenticate), sqlparams);

                if (ds != null && ds.Tables.Count > 1)
                    RemoveEmptyAccounts(ref ds);
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }


        //Loan Summary
        public DataSet GetLoanSummary(int PeriodId, Nullable<DateTime> DateFrom, Nullable<DateTime> DateTo)
        {
            DataSet ds = null;
            StringBuilder additionalFilter = new StringBuilder();
            #region
            const string query = @"select Distinct Set_BankFacility.BankID, Set_BankFacilityDetail.FromDate,Set_BankFacilityDetail.ToDate,Tabdetail.DocumentDate ,
Set_BankFacility.LoanType,Set_BankFacility.LimitName,Tab1.BankName,
(Tabdetail.ReceiptAmount-Tabdetail.PaymentAmount) as BankBalance,
Set_BankFacility.Amount,Set_BankFacility.Markup,Tab1.Balance,Set_BankFacilityDetail.InterestRate, 
Round((((Tabdetail.ReceiptAmount-Tabdetail.PaymentAmount)*Set_BankFacilityDetail.InterestRate)/100)/365,2)*-1 as MarkupBalance,
0 as MarkupOutstanding
from Set_BankFacility 
inner join Set_BankFacilityDetail on Set_BankFacilityDetail.BankFacilityHeaderID=Set_BankFacility.BankFacilityID

inner join (select BankBranchID, BankName, BankAccountNumber As BankAccountName, BankBranchName, sum(ResultValue) as Balance from 
(
    Select isnull(sum(AR_ReceiptTransactionHeader.Amount), 0) As ResultValue, AR_Set_bank.BankName, AR_Set_bankAccount.BankAccountNumber, 
        AR_Set_bankBranch.BankBranchName,AR_Set_bankBranch.BankBranchID
    From AR_Set_bank 
        Inner Join AR_Set_bankBranch ON AR_Set_bankBranch.BankID = AR_Set_bank.BankID AND AR_Set_bankBranch.Status = 1 
        Inner Join AR_Set_bankAccount On AR_Set_bankBranch.BankBranchID = AR_Set_bankAccount.BankBranchID And AR_Set_bankAccount.Status = 1 
        Inner Join AR_ReceiptTransactionHeader ON AR_ReceiptTransactionHeader.BankAccountID = AR_Set_bankAccount.BankAccountID 
            --And AR_Set_bankAccount.CompanyId = AR_ReceiptTransactionHeader.CompanyId 
            And AR_Set_bankAccount.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
            And AR_ReceiptTransactionHeader.Status = 1 AND AR_ReceiptTransactionHeader.StatusCode = 1 
        INNER JOIN SET_DocumentType  ON AR_ReceiptTransactionHeader.CompanyID = SET_DocumentType.CompanyID 
            AND AR_ReceiptTransactionHeader.PeriodID = SET_DocumentType.PeriodID AND AR_ReceiptTransactionHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID 
         INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID --AND UsersInDocumentType.UserID = @UserID  
    where AR_Set_bankAccount.PeriodID = @PeriodID And AR_Set_bank.status = 1 
        and AR_ReceiptTransactionHeader.DocumentNo is not null --AND AR_ReceiptTransactionHeader.DocumentDate <= @Dateto 
        AND AR_ReceiptTransactionHeader.BankAccountID is not null and isnull(AR_SET_BankAccount.ShowInBankStatusReport, 0) = 1 
         
         
    group by AR_Set_bankAccount.BankAccountNumber, AR_Set_bank.BankName, AR_Set_bankBranch.BankBranchName,AR_Set_bankBranch.BankBranchID

    UNION ALL 

    Select isnull(sum(Case When AR_ReceiptBanksDetail.TransactionType = 'Debit'	Then AR_ReceiptBanksDetail.Amount Else AR_ReceiptBanksDetail.Amount * -1 End), 0) 
            As ResultValue, 
        AR_Set_bank.BankName, AR_Set_bankAccount.BankAccountNumber, AR_Set_bankBranch.BankBranchName,AR_Set_bankBranch.BankBranchID
    From AR_ReceiptTransactionHeader 
        INNER JOIN SET_DocumentType ON AR_ReceiptTransactionHeader.CompanyID = SET_DocumentType.CompanyID 
            AND AR_ReceiptTransactionHeader.PeriodID = SET_DocumentType.PeriodID AND AR_ReceiptTransactionHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID 
         INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID --AND UsersInDocumentType.UserID = @UserID  
        INNER Join AR_ReceiptBanksDetail ON AR_ReceiptBanksDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID 
            and AR_ReceiptBanksDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID and AR_ReceiptBanksDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
            AND AR_ReceiptBanksDetail.Status = 1 and AR_ReceiptBanksDetail.BankAccountID is not null 
        Inner Join AR_Set_BankAccount On AR_ReceiptBanksDetail.PeriodID = AR_Set_BankAccount.PeriodID And AR_ReceiptBanksDetail.BankAccountID = AR_Set_BankAccount.BankAccountID 
            And AR_Set_BankAccount.Status = 1 
        INNER JOIN AR_Set_BankBranch On AR_Set_BankBranch.BankBranchID = AR_Set_BankAccount.BankBranchID 
        Inner Join AR_SET_Bank on AR_SET_Bank.BankID = AR_Set_bankBranch.BankID 
    where AR_Set_bank.status = 1 And AR_ReceiptTransactionHeader.Status = 1 And AR_ReceiptTransactionHeader.StatusCode = 1 
        and AR_ReceiptTransactionHeader.DocumentNo is not null --AND AR_ReceiptTransactionHeader.DocumentDate <= @Dateto 
        and isnull(AR_SET_BankAccount.ShowInBankStatusReport, 0) = 1 
         
         
    group by AR_Set_bankAccount.BankAccountNumber, AR_Set_bank.BankName, AR_Set_bankBranch.BankBranchName,AR_Set_bankBranch.BankBranchID 

    Union All 

    Select isnull(Sum(dbo.AP_PaymentHeader.Amount), 0) * -1 As ResultValue, AR_Set_bank.BankName, AR_Set_bankAccount.BankAccountNumber, 
        AR_Set_bankBranch.BankBranchName,AR_Set_bankBranch.BankBranchID
    From AR_Set_bank 
        Inner Join AR_Set_bankBranch ON AR_Set_bankBranch.BankID = AR_Set_bank.BankID AND AR_Set_bankBranch.Status = 1 
        Inner Join AR_Set_bankAccount On AR_Set_bankBranch.BankBranchID = AR_Set_bankAccount.BankBranchID --And AR_Set_bankAccount.CompanyID = () 
            And AR_Set_bankAccount.PeriodID = @PeriodID And AR_Set_bankAccount.Status = 1 
        Inner Join AP_PaymentHeader ON AP_PaymentHeader.BankAccountID = AR_Set_bankAccount.BankAccountID --And AR_Set_bankAccount.CompanyId = AP_PaymentHeader.CompanyId 
            And AR_Set_bankAccount.PeriodID = AP_PaymentHeader.PeriodID And AP_PaymentHeader.Status = 1 AND AP_PaymentHeader.StatusCode = 1 
        INNER JOIN SET_DocumentType  ON AP_PaymentHeader.CompanyID = SET_DocumentType.CompanyID AND
        AP_PaymentHeader.PeriodID = SET_DocumentType.PeriodID AND
        AP_PaymentHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID 
         INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID --AND UsersInDocumentType.UserID = @UserID  
    where AR_Set_bank.status = 1 And AP_PaymentHeader.BankAccountID is not null and AP_PaymentHeader.DocumentNo is not null 
        --AND AP_PaymentHeader.DocumentDate <= @Dateto 
        and isnull(AR_SET_BankAccount.ShowInBankStatusReport,0) = 1
         
         
    group by AR_Set_bankAccount.BankAccountNumber, AR_Set_bank.BankName, AR_Set_bankBranch.BankBranchName,AR_Set_bankBranch.BankBranchID 

    UNION ALL 
	
    Select isnull(sum(Case When AP_PaymentBanksDetail.TransactionType = 'Credit' Then AP_PaymentBanksDetail.Amount * -1 Else AP_PaymentBanksDetail.Amount End), 0) 
            As ResultValue, 
        AR_Set_bank.BankName, AR_Set_bankAccount.BankAccountNumber, AR_Set_bankBranch.BankBranchName,AR_Set_bankBranch.BankBranchID 
    From AP_PaymentHeader 
        INNER JOIN SET_DocumentType  ON AP_PaymentHeader.CompanyID = SET_DocumentType.CompanyID AND
        AP_PaymentHeader.PeriodID = SET_DocumentType.PeriodID AND
        AP_PaymentHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID 
         INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID --AND UsersInDocumentType.UserID = @UserID  
        INNER Join AP_PaymentBanksDetail ON AP_PaymentBanksDetail.PaymentHeaderID = AP_PaymentHeader.PaymentID 
            and AP_PaymentBanksDetail.CompanyID = AP_PaymentHeader.CompanyID and AP_PaymentBanksDetail.PeriodID = AP_PaymentHeader.PeriodID 
            AND AP_PaymentBanksDetail.Status = 1 and AP_PaymentBanksDetail.BankAccountID is not null 
        Inner Join AR_Set_BankAccount On AP_PaymentBanksDetail.PeriodID = AR_Set_BankAccount.PeriodID And AP_PaymentBanksDetail.BankAccountID = AR_Set_BankAccount.BankAccountID 
            And AR_Set_BankAccount.Status = 1 
        Inner Join AR_Set_bankBranch ON AR_Set_bankBranch.BankBranchID = AR_Set_BankAccount.BankBranchID AND AR_Set_bankBranch.Status = 1 
        Inner Join AR_SET_Bank on AR_SET_Bank.BankID = AR_Set_bankBranch.BankID 
    where AR_Set_bank.status = 1 And AP_PaymentHeader.Status = 1 And AP_PaymentHeader.StatusCode = 1 and AP_PaymentHeader.DocumentNo is not null 
       -- AND AP_PaymentHeader.DocumentDate <= @Dateto 
        and isnull(AR_SET_BankAccount.ShowInBankStatusReport,0) = 1
         
         
    group by AR_Set_bankAccount.BankAccountNumber, AR_Set_bank.BankName, AR_Set_bankBranch.BankBranchName,AR_Set_bankBranch.BankBranchID
) tb 
group by BankName, BankAccountNumber, BankBranchName,BankBranchID 
) as Tab1 on Tab1.BankBranchID=Set_BankFacility.BankID

inner join (Select tab2.BankBranchID, tab2.BankAccountID, tab2.DocumentNo, tab2.DocumentDate, SUM(tab2.ReceiptAmount) ReceiptAmount, SUM(tab2.PaymentAmount) PaymentAmount, tab2.DocumentType, 
    tab2.InstrumentNo, tab2.Comments, tab2.BankAccountNumber ,tab2.BankID,unPosted
From 
(
	Select AR_Set_bankAccount.BankBranchID, AR_Set_bankAccount.BankAccountID, AR_ReceiptTransactionHeader.DocumentDate AS DocumentDate, AR_ReceiptTransactionHeader.DocumentNo, 
        AR_ReceiptTransactionHeader.Amount ReceiptAmount, 0 as PaymentAmount, Set_DocumentType.DocumentType, 
        AR_ReceiptTransactionHeader.PaymentServerOrderNo As InstrumentNo, 
        IsNull(AR_ReceiptTransactionHeader.Comments, '') + IsNull(AR_ReceiptTransactionHeader.ReversalComments, '') As Comments, AR_Set_bankAccount.BankAccountNumber ,'' as BankID
		, case AR_ReceiptTransactionHeader.StatusCode when 1 then 1 else 0 end as unPosted
	From AR_ReceiptTransactionHeader 
		INNER JOIN AR_Set_bankAccount On AR_Set_bankAccount.PeriodID = AR_ReceiptTransactionHeader.PeriodID And AR_Set_bankAccount.BankAccountID = AR_ReceiptTransactionHeader.BankAccountID 
        INNER JOIN AR_Set_BankBranch On AR_Set_BankBranch.BankBranchID = AR_Set_BankAccount.BankBranchID 
		Inner Join Set_DocumentType On Set_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
            And Set_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID And Set_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
         INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID --AND UsersInDocumentType.UserID = @UserID  
         
	WHERE AR_ReceiptTransactionHeader.CompanyID in (1) AND AR_ReceiptTransactionHeader.PeriodID = @PeriodID And AR_ReceiptTransactionHeader.Status = 1 
        --AND AR_ReceiptTransactionHeader.StatusCode = 1 
		AND AR_ReceiptTransactionHeader.BankAccountID is not null 
        
         --AND AR_ReceiptTransactionHeader.DocumentDate Between @FromDate AND @ToDate  
	
	UNION ALL 
	
	Select  AR_Set_bankAccount.BankBranchID,AR_Set_bankAccount.BankAccountID, AP_PaymentHeader.DocumentDate, AP_PaymentHeader.DocumentNo, 0 as ReceiptAmount, AP_PaymentHeader.Amount as PaymentAmount, 
        Set_DocumentType.DocumentType, AP_PaymentHeader.PaymentServerOrderNo As InstrumentNo, 
        IsNull(AP_PaymentHeader.Comments, '') + IsNull(AP_PaymentHeader.ReversalComments, '') As Comments, AR_Set_bankAccount.BankAccountNumber ,'' as BankID
		, case AP_PaymentHeader.StatusCode when 1 then 1 else 0 end as unPosted
	From AP_PaymentHeader 
		INNER JOIN AR_Set_bankAccount On AR_Set_bankAccount.PeriodID = AP_PaymentHeader.PeriodID 
            And AR_Set_bankAccount.BankAccountID = AP_PaymentHeader.BankAccountID 
        INNER JOIN AR_Set_BankBranch On AR_Set_BankBranch.BankBranchID = AR_Set_BankAccount.BankBranchID 
		Inner Join Set_DocumentType On Set_DocumentType.CompanyID = AP_PaymentHeader.CompanyID And Set_DocumentType.PeriodID = AP_PaymentHeader.PeriodID 
            And Set_DocumentType.DocumentTypeID = AP_PaymentHeader.DocumentTypeID 
         INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID --AND UsersInDocumentType.UserID = @UserID  
         
	WHERE AP_PaymentHeader.CompanyID in (1) AND AP_PaymentHeader.PeriodID = @PeriodID And AP_PaymentHeader.Status = 1 
	--AND AP_PaymentHeader.StatusCode = 1 
        AND AP_PaymentHeader.BankAccountID is not null 
        
         --AND AP_PaymentHeader.DocumentDate Between @FromDate AND @ToDate  
    
    UNION ALL 
    
    Select AR_Set_bankAccount.BankBranchID, AR_Set_bankAccount.BankAccountID, AR_ReceiptTransactionHeader.DocumentDate, AR_ReceiptTransactionHeader.DocumentNo, 
        (Case when AR_ReceiptBanksDetail.TransactionType = 'Debit' then AR_ReceiptBanksDetail.Amount else 0 end) as ReceiptAmount, 
        (Case when AR_ReceiptBanksDetail.TransactionType = 'Credit' then AR_ReceiptBanksDetail.Amount else 0 end) as PaymentAmount, Set_DocumentType.DocumentType, 
        AR_ReceiptBanksDetail.InstrumentNo, AR_ReceiptBanksDetail.Comments, AR_Set_bankAccount.BankAccountNumber ,AR_ReceiptBanksDetail.ReceiptBankID as BankID
		, case AR_ReceiptTransactionHeader.StatusCode when 1 then 1 else 0 end as unPosted
	From AR_ReceiptTransactionHeader 
		INNER JOIN AR_ReceiptBanksDetail ON AR_ReceiptBanksDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
			AND AR_ReceiptBanksDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
			AND AR_ReceiptBanksDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID 
			AND AR_ReceiptBanksDetail.Status=1
		INNER JOIN AR_Set_bankAccount On 
            AR_Set_bankAccount.PeriodID = AR_ReceiptBanksDetail.PeriodID And AR_Set_bankAccount.BankAccountID = AR_ReceiptBanksDetail.BankAccountID 
        INNER JOIN AR_Set_BankBranch On AR_Set_BankBranch.BankBranchID = AR_Set_BankAccount.BankBranchID 
		Inner Join Set_DocumentType On Set_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
            And Set_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID And Set_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
         INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID --AND UsersInDocumentType.UserID = @UserID  
         
	WHERE AR_ReceiptTransactionHeader.CompanyID in (1) AND AR_ReceiptTransactionHeader.PeriodID = @PeriodID And AR_ReceiptTransactionHeader.Status = 1 
        --AND AR_ReceiptTransactionHeader.StatusCode = 1 
         
         --AND AR_ReceiptTransactionHeader.DocumentDate Between @FromDate AND @ToDate  
	
	UNION ALL 
	
	Select AR_Set_bankAccount.BankBranchID, AR_Set_bankAccount.BankAccountID, AP_PaymentHeader.DocumentDate, AP_PaymentHeader.DocumentNo, 
        (Case when AP_PaymentBanksDetail.TransactionType = 'Debit' then AP_PaymentBanksDetail.Amount else 0 end) as ReceiptAmount, 
        (Case when AP_PaymentBanksDetail.TransactionType = 'Credit' then AP_PaymentBanksDetail.Amount else 0 end) as PaymentAmount, Set_DocumentType.DocumentType, 
        AP_PaymentBanksDetail.InstrumentNo, AP_PaymentBanksDetail.Comments, AR_Set_bankAccount.BankAccountNumber ,AP_PaymentBanksDetail.PaymentBankID as BankID
		, case AP_PaymentHeader.StatusCode when 1 then 1 else 0 end as unPosted
    From AP_PaymentHeader 
	    INNER JOIN AP_PaymentBanksDetail ON AP_PaymentBanksDetail.CompanyID = AP_PaymentHeader.CompanyID 
            AND AP_PaymentBanksDetail.PeriodID = AP_PaymentHeader.PeriodID AND AP_PaymentBanksDetail.PaymentHeaderID = AP_PaymentHeader.PaymentID 
            AND AP_PaymentBanksDetail.Status=1
	    INNER JOIN AR_Set_bankAccount On AR_Set_bankAccount.PeriodID = AP_PaymentBanksDetail.PeriodID And AR_Set_bankAccount.BankAccountID = AP_PaymentBanksDetail.BankAccountID 
        INNER JOIN AR_Set_BankBranch On AR_Set_BankBranch.BankBranchID = AR_Set_BankAccount.BankBranchID 
	    Inner Join Set_DocumentType On Set_DocumentType.CompanyID = AP_PaymentHeader.CompanyID And Set_DocumentType.PeriodID = AP_PaymentHeader.PeriodID 
            And Set_DocumentType.DocumentTypeID = AP_PaymentHeader.DocumentTypeID 
         INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID --AND UsersInDocumentType.UserID = @UserID  
         
    WHERE AP_PaymentHeader.CompanyID in (1) AND AP_PaymentHeader.PeriodID = @PeriodID And AP_PaymentHeader.Status = 1 --AND AP_PaymentHeader.StatusCode = 1 
         
        -- AND AP_PaymentHeader.DocumentDate Between @FromDate AND @ToDate 
)tab2 
 where tab2.unPosted = 1
Group By tab2.DocumentType, tab2.BankAccountID, tab2.DocumentNo, tab2.DocumentDate, 
tab2.InstrumentNo, tab2.Comments, tab2.BankAccountNumber,tab2.BankID,unPosted, tab2.BankBranchID) as TabDetail on TabDetail.BankBranchID=Set_BankFacility.BankID

where Set_BankFacilityDetail.FromDate between @fromDate and  @ToDate
";



            #endregion

            //int dateFromString = DateFrom.Value.Year;
            //int dateToString = DateTo.Value.Year;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@PeriodID", PeriodId);
                sqlparams[1] = new SqlParameter("@fromDate", DateFrom);
                sqlparams[2] = new SqlParameter("@ToDate", DateTo);


                ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }



        private static void RemoveEmptyAccounts(ref DataSet ds)
        {
            List<DataRow> headersToBeDeleted = new List<DataRow>(ds.Tables[0].Rows.Count);

            foreach (DataRow drHeader in ds.Tables[0].Rows)
            {
                var detailCount = (from drDetail in ds.Tables[1].AsEnumerable()
                                   where drDetail.Field<string>("BankAccountNumber") == drHeader["BankAccountNumber"].ToString()
                                   select drDetail).Count<DataRow>();

                if (ds.Tables[0].Columns.Contains("TotOpeningBalance"))
                {
                    if (detailCount == 0 && Convert.ToDecimal(drHeader["TotOpeningBalance"]) == 0)
                        headersToBeDeleted.Add(drHeader);
                }
                else
                {
                    if (detailCount == 0 && Convert.ToDecimal(drHeader["OpeningBalance"]) == 0)
                        headersToBeDeleted.Add(drHeader);
                }
            }

            foreach (DataRow drToDelete in headersToBeDeleted)
                ds.Tables[0].Rows.Remove(drToDelete);
        }

        public DataTable GetLeaseFinanceData(int companyID, DateTime fromDate, DateTime toDate)
        {
            DataTable dt;

            const string Financequery = @"select 'Lease Finance' as LeaseFinance,AR_SET_Bank.BankName as BankBranchName , FA_LeaseHeader.Rate , FA_LeaseHeader.StockGRNDate  from FA_LeaseHeader
inner join AR_SET_BankBranch on AR_SET_BankBranch.BankBranchID=FA_LeaseHeader.BankBranchID
inner join AR_SET_Bank on AR_SET_Bank.BankID =AR_SET_BankBranch.BankID
where FA_LeaseHeader.Status =1 and Ijara=0
and FA_LeaseHeader.CompanyID=@CompanyID
and StockGRNDate between @Fromdate and @Dateto
union all
select 'Ijarah' as LeaseFinance,AR_SET_Bank.BankName as BankBranchName, FA_LeaseHeader.Rate , FA_LeaseHeader.StockGRNDate from FA_LeaseHeader
inner join AR_SET_BankBranch on AR_SET_BankBranch.BankBranchID=FA_LeaseHeader.BankBranchID
inner join AR_SET_Bank on AR_SET_Bank.BankID =AR_SET_BankBranch.BankID
where FA_LeaseHeader.Status =1 and Ijara =1
and FA_LeaseHeader.CompanyID=@CompanyID
and StockGRNDate between @Fromdate and @Dateto
";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@Fromdate", DateUtil.ConvertToDate(DateUtil.ConvertToString(fromDate) + " 00:00:00"));
                sqlparams[2] = new SqlParameter("@Dateto", DateUtil.ConvertToDate(DateUtil.ConvertToString(toDate) + " 23:59:59"));


                dt = SqlHelper.ExecuteDataset(CommandType.Text, Financequery, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }






        public DataTable GetBankStatusDataSummary(String companyID, int periodID, int bankID, int branchID, DateTime fromDate, DateTime toDate, int? bankAccountTypeID,
        int? BankAccountNo, bool discardReversedDocuments, Guid userID, string documentGroupID = "", Nullable<int> DocumentTypeID = null, bool documentTypeAuthentication = false)
        {
            DataTable dt;
            StringBuilder filter = new StringBuilder();
            string qry_ReceiptReversal = string.Empty, qry_PaymentReversal = string.Empty, qry_docuTypeAuthenticate = string.Empty;

            if (discardReversedDocuments)
            {
                qry_ReceiptReversal = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                qry_PaymentReversal = " And AP_PaymentHeader.ReversalComments is null ";
            }

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Fromdate", DateUtil.ConvertToDate(DateUtil.ConvertToString(fromDate) + " 00:00:00"));
                sqlparams[3] = new SqlParameter("@Dateto", DateUtil.ConvertToDate(DateUtil.ConvertToString(toDate) + " 23:59:59"));

                #region Filter by Bank

                if (bankID != -1)
                {
                    filter.Append(" And AR_SET_Bank.BankID = @BankID ");
                    sqlparams[4] = new SqlParameter("@BankID", bankID);
                }

                #endregion

                #region Filter by Bank Branch

                if (branchID != -1)
                {
                    filter.Append(" And AR_SET_BankBranch.BankBranchID = @BranchID ");
                    sqlparams[5] = new SqlParameter("@BranchID", branchID);
                }

                #endregion

                if (bankAccountTypeID != -1)
                    filter.Append(" And AR_SET_BankAccount.BankAccountTypeID = " + bankAccountTypeID + " ");

                if (BankAccountNo != -1)
                    filter.Append(" And AR_SET_BankAccount.BankAccountID = " + BankAccountNo + " ");

                if ( documentGroupID != null && documentGroupID != "")
                    filter.Append(" AND Set_DocumentType.GroupID in " + documentGroupID + " or Set_DocumentType.GroupID IS NULL");

                if (documentTypeAuthentication)
                {
                    sqlparams[6] = new SqlParameter("@UserID", userID);
//                    qry_docuTypeAuthenticate = @" Inner Join (Select distinct CompanyID, Documenttypeid, UserID from UsersInDocumentType 
//		                )as UserIndoc on  UserIndoc.CompanyID = SET_DocumentType.CompanyID
//		                AND UserIndoc.DocumentTypeID = SET_DocumentType.DocumentTypeID 
//		                AND UserIndoc.UserID = @UserID ";
                }

                if (DocumentTypeID.HasValue)
                {
                    sqlparams[7] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                }
                else
                {
                    sqlparams[7] = new SqlParameter("@DocumentTypeID", DBNull.Value);
                }
                companyID = "(" + companyID + ")";


                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_SET_Bank.Query_GetBankStatusSummary.ToString().Replace("@CompanyID", companyID), filter.ToString(), qry_ReceiptReversal,
                    qry_PaymentReversal, qry_docuTypeAuthenticate), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }



        public DataTable GetBankReconcileDataSummary(String companyID, int periodID, int bankID, int branchID, DateTime fromDate, DateTime toDate, int? bankAccountTypeID,
        int? BankAccountNo, bool discardReversedDocuments, Guid userID, string documentGroupID = "", Nullable<int> DocumentTypeID = null, bool documentTypeAuthentication = false)
        {
            DataTable dt;
            StringBuilder filter = new StringBuilder();
            string qry_ReceiptReversal = string.Empty, qry_PaymentReversal = string.Empty, qry_docuTypeAuthenticate = string.Empty;

            if (discardReversedDocuments)
            {
                qry_ReceiptReversal = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                qry_PaymentReversal = " And AP_PaymentHeader.ReversalComments is null ";
            }

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Fromdate", DateUtil.ConvertToDate(DateUtil.ConvertToString(fromDate) + " 00:00:00"));
                sqlparams[3] = new SqlParameter("@Dateto", DateUtil.ConvertToDate(DateUtil.ConvertToString(toDate) + " 23:59:59"));

                #region Filter by Bank

                if (bankID != -1)
                {
                    filter.Append(" And AR_SET_Bank.BankID = @BankID ");
                    sqlparams[4] = new SqlParameter("@BankID", bankID);
                }

                #endregion

                #region Filter by Bank Branch

                if (branchID != -1)
                {
                    filter.Append(" And AR_SET_BankBranch.BankBranchID = @BranchID ");
                    sqlparams[5] = new SqlParameter("@BranchID", branchID);
                }

                #endregion

                if (bankAccountTypeID != -1)
                    filter.Append(" And AR_SET_BankAccount.BankAccountTypeID = " + bankAccountTypeID + " ");

                if (BankAccountNo != -1)
                    filter.Append(" And AR_SET_BankAccount.BankAccountID = " + BankAccountNo + " ");

                if (documentGroupID != null && documentGroupID != "")
                    filter.Append(" AND Set_DocumentType.GroupID in " + documentGroupID + " or Set_DocumentType.GroupID IS NULL");

                if (documentTypeAuthentication)
                {
                    sqlparams[6] = new SqlParameter("@UserID", userID);
                }

                if (DocumentTypeID.HasValue)
                {
                    sqlparams[7] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                }
                else
                {
                    sqlparams[7] = new SqlParameter("@DocumentTypeID", DBNull.Value);
                }
                companyID = "(" + companyID + ")";


                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_SET_Bank.Query_GetBankReconcileSummary.ToString().Replace("@CompanyID", companyID), filter.ToString(), qry_ReceiptReversal,
                    qry_PaymentReversal, qry_docuTypeAuthenticate), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }




        public static bool InsertBank_BranchDet(int bankID, AR_SET_Bank objbank, List<AR_SET_BankBranch> ColAddBranchDet)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertBank_BranchDet");

            try
            {
                DataRow drBank = AR_SET_Bank.GetDataByKey(bankID);

                if (drBank != null)
                    objbank.UpdateDataByKey(sqltrans);

                else
                    objbank.Insert(sqltrans);

                using (AR_SET_BankBranch obj_BankBranch = new AR_SET_BankBranch())
                    obj_BankBranch.DeletePermanentByForeignKey(sqltrans, bankID);

                foreach (AR_SET_BankBranch rBranch in ColAddBranchDet)
                    rBranch.Insert(sqltrans);

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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertBank_BranchDet");
            }

            return result;
        }

        public static bool UpdateBank_BranchDetail(AR_SET_Bank objBank, List<AR_SET_BankBranch> ColDeleteBrDt, List<AR_SET_BankBranch> ColAddBranchDt)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("Update_BankAndBranchDetail");

            try
            {
                if (!(objBank.UpdateDataByKey(sqltrans)))
                    throw new BusinessLogicException("Error in update Bank: ");

                foreach (AR_SET_BankBranch CD in ColDeleteBrDt)
                    if (!(AR_SET_BankBranch.DeletePermanentByKey(sqltrans, CD.BankBranchID)))
                        throw new BusinessLogicException("Error in delete Branch: ");

                int counter = 0;
                foreach (AR_SET_BankBranch BDt in ColAddBranchDt)
                {
                    if (BDt.BankBranchID == -1)
                    {
                        BDt.BankBranchID = AR_SET_BankBranch.GetNewBankBranchID()+counter;
                        counter++;
                    }
                }

                foreach (AR_SET_BankBranch BD in ColAddBranchDt)
                {
                    if (BD.BankID == -1)
                    {
                        BD.BankID = objBank.BankID;

                        if (!(BD.Insert(sqltrans)))
                            throw new BusinessLogicException("Error in insert Branch: ");
                    }
                    else if (!(BD.UpdateDataByKey(sqltrans)))
                        throw new BusinessLogicException("Error in update Branch: ");
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
                    SqlHelper.RollBackTransaction(sqltrans, "Update_BankAndBranchDetail");
            }

            return result;
        }

        public DataTable GetAllBankNames()
        {
            DataTable dt = null;

            try
            {
                string Query_BankNames = @" select * from AR_SET_Bank where status = 1 ";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_BankNames).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetCashNames()
        {
            DataTable dt = null;

            try
            {
                string Query_Cash = @" select * from AR_SET_Bank where BankName like '%cash%' and status = 1 ";
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Cash).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetDataBy(string searchColumn, string searchValue)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@ColumnValue", searchValue);

                string Query_GetDataByColumn = @" SELECT * FROM AR_SET_Bank WHERE {0} = @ColumnValue AND Status = 1; ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDataByColumn, searchColumn), sqlparams);

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

        public DataSet GetBankPosition(string companyID, int periodID, int bankID, int branchID, DateTime? documentDateFrom = null, DateTime? documentDateTo = null,
            string documentGroupID = "", Nullable<Guid> userID = null, bool documentTypeAuthorization = false, bool documentTypeAuthentication = false)
        {
            DataSet dtToReturn = null;
            StringBuilder additionalFilter = new StringBuilder();
            string documentFilter = string.Empty, qry_DocumentAuthorization = string.Empty, OpeningDocumentFilter = string.Empty;

            if (documentDateFrom != null && documentDateTo != null)
            {
                if (documentDateFrom <= documentDateTo)
                {
                    documentDateFrom = DateUtil.ConvertToDate(DateUtil.ConvertToString(documentDateFrom) + " 00:00:00");
                    documentDateTo = DateUtil.ConvertToDate(DateUtil.ConvertToString(documentDateTo) + " 23:59:59");
                }
            }
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                //sqlparams[2] = new SqlParameter("@FromDate", DateUtil.ConvertToString(documentDateFrom.Value, true) + " 00:00:00");
                //sqlparams[3] = new SqlParameter("@ToDate", DateUtil.ConvertToString(documentDateTo.Value, true) + " 23:59:59");
                sqlparams[2] = new SqlParameter("@FromDate", documentDateFrom);
                sqlparams[3] = new SqlParameter("@ToDate", documentDateTo);

                #region Filter by Bank

                if (bankID > -1)
                {
                    additionalFilter.Append(" AND AR_SET_BankBranch.BankID = @BankID ");
                    sqlparams[4] = new SqlParameter("@BankID", bankID);
                }

                #endregion

                #region Filter by Bank Branch

                if (branchID > -1)
                {
                    additionalFilter.Append(" AND AR_SET_BankBranch.BankBranchID = @BranchID ");
                    sqlparams[5] = new SqlParameter("@BranchID", branchID);
                }

                #endregion

                #region Filter by Document Type Group

                if (documentGroupID != "")
                {
                    //  documentFilter = " AND Set_DocumentType.GroupID = @DocumentGroupID ";
                    documentFilter = " AND AR_SET_BankAccount.DocumentGroupID in " + documentGroupID + " ";
                    OpeningDocumentFilter = " AND Set_DocumentType.GroupID  in " + documentGroupID + "";
                    sqlparams[6] = new SqlParameter("@DocumentGroupID", documentGroupID);
                }

                #endregion

                if (documentTypeAuthorization)
                {
//                    qry_DocumentAuthorization = @"Inner Join UsersInDocumentType On UsersInDocumentType.CompanyID = Set_DocumentType.CompanyID 
//                    And UsersInDocumentType.DocumentTypeID = Set_DocumentType.DocumentTypeID And UsersInDocumentType.UserID = @UserID ";

                    sqlparams[7] = new SqlParameter("@UserID", userID.Value);
                }


                companyID = "(" + companyID + ")";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_SET_Bank.Query_GetBankPosition.ToString().Replace("@CompanyID", companyID), additionalFilter.ToString(), documentFilter,
                    qry_DocumentAuthorization, OpeningDocumentFilter), sqlparams);

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


        public DataSet PaymentDetails_Report(int bankID, int branchID, int SupplierID, DateTime? documentDateFrom = null, DateTime? documentDateTo = null, string documentGroupID = "", bool filterZeroRecords = false)
        {
            DataSet dtToReturn = null;
            StringBuilder additionalFilter = new StringBuilder();
            //StringBuilder DocumentGroupFilter = new StringBuilder();
            
            if (documentDateFrom != null && documentDateTo != null)
            {
                if (documentDateFrom <= documentDateTo)
                {
                    documentDateFrom = DateUtil.ConvertToDate(DateUtil.ConvertToString(documentDateFrom) + " 00:00:00");
                    documentDateTo = DateUtil.ConvertToDate(DateUtil.ConvertToString(documentDateTo) + " 23:59:59");
                }
            }
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@FromDate", documentDateFrom);
                sqlparams[1] = new SqlParameter("@ToDate", documentDateTo);

                #region Filter by Bank

                if (bankID > -1)
                {
                    additionalFilter.Append(" AND AR_SET_Bank.BankID = @BankID ");
                    sqlparams[2] = new SqlParameter("@BankID", bankID);
                }
                if (SupplierID > -1)
                {
                    additionalFilter.Append(" AND AP_PaymentSupplierDetail.SupplierID = @SupplierID ");
                    sqlparams[3] = new SqlParameter("@SupplierID", SupplierID);
                }
                #endregion

                #region Filter by Bank Branch

                if (branchID > -1)
                {
                    additionalFilter.Append(" AND AR_SET_BankBranch.BankBranchID = @BranchID ");
                    sqlparams[4] = new SqlParameter("@BranchID", branchID);
                }

                #endregion

                if (documentGroupID != "")
                {
                    additionalFilter.Append(" AND Set_DocumentType.GroupID in " + documentGroupID);                    
                    //DocumentGroupFilter.Append(" AND AR_Set_bankAccount.DocumentGroupID in " + documentGroupID);
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_SET_Bank.Query_PaymentDetails_Report.ToString(),additionalFilter.ToString()), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds;

                #region Filter zero records

                if (filterZeroRecords)
                {
                    List<int> lst_BankAccountIDs = dtToReturn.Tables[0].AsEnumerable().Where(x => x.Field<string>("Type").Equals("RECEIPTS") && x.Field<decimal>("Balance") != 0)
                        .Select(x => x.Field<int>("BankAccountID")).Distinct().ToList();

                    //List<int> lst_PaymentBankAccountIDs = dtToReturn.AsEnumerable().Where(x => x.Field<string>("Type").Equals("PAYMENTS")
                    //    && x.Field<decimal>("Balance") != 0).Select(x => x.Field<int>("BankAccountID")).Distinct().ToList();
                    List<int> lst_PaymentBankAccountIDs = dtToReturn.Tables[0].AsEnumerable().Where(x =>
                    x.Field<decimal>("Balance") != 0 && x.Field<decimal>("OpeningBalance") != 0).Select(x => x.Field<int>("BankAccountID")).Distinct().ToList();

                    foreach (int bankAccountID in lst_PaymentBankAccountIDs)
                        if (!lst_BankAccountIDs.Contains(bankAccountID))
                            lst_BankAccountIDs.Add(bankAccountID);

                    DataTable dt_Filtered = dtToReturn.Tables[0].Clone();

                    foreach (int bankAccountID in lst_BankAccountIDs)
                    {
                        DataRow[] dr_BankAccountRecords = dtToReturn.Tables[0].AsEnumerable().Where(x => x.Field<int>("BankAccountID") == bankAccountID).ToArray();

                        foreach (DataRow dr_ToImport in dr_BankAccountRecords)
                            dt_Filtered.ImportRow(dr_ToImport);
                    }

                    dt_Filtered.AcceptChanges();


                    dtToReturn.Tables.Remove(dtToReturn.Tables[0]);
                    dtToReturn.Tables.Add(dt_Filtered.Copy());

                    //dtToReturn.Tables[0] = dt_Filtered.Copy();
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

        public DataSet CheckedAndUnCheckedTransaction_Report(int bankID, int branchID, int bankaccountID, int SupplierID, string companies, DateTime? documentDateFrom = null, DateTime? documentDateTo = null, string documentGroupID = "", bool filterZeroRecords = false, bool CheckedTransaction = false, bool UnCheckedTransaction = false, bool IsActivityDate = false)
        {
            DataSet dtToReturn = null;
            StringBuilder additionalFilter = new StringBuilder();
            StringBuilder additionalFilter2 = new StringBuilder();
            StringBuilder dateFilter = new StringBuilder();
            StringBuilder dateFilter2 = new StringBuilder();
            //StringBuilder DocumentGroupFilter = new StringBuilder();

            if (documentDateFrom != null && documentDateTo != null)
            {
                if (documentDateFrom <= documentDateTo)
                {
                    documentDateFrom = DateUtil.ConvertToDate(DateUtil.ConvertToString(documentDateFrom) + " 00:00:00");
                    documentDateTo = DateUtil.ConvertToDate(DateUtil.ConvertToString(documentDateTo) + " 23:59:59");
                }
            }
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@FromDate", documentDateFrom);
                sqlparams[1] = new SqlParameter("@ToDate", documentDateTo);

                #region Filter by Bank

                if (bankID > -1)
                {
                    additionalFilter.Append(" AND AR_SET_Bank.BankID = @BankID ");
                    additionalFilter2.Append(" AND AR_SET_Bank.BankID = @BankID ");
                    sqlparams[2] = new SqlParameter("@BankID", bankID);
                }
                if (SupplierID > -1)
                {
                    additionalFilter.Append(" AND AP_PaymentSupplierDetail.SupplierID = @SupplierID ");
                    additionalFilter2.Append(" AND AP_PaymentSupplierDetail.SupplierID = @SupplierID ");
                    sqlparams[3] = new SqlParameter("@SupplierID", SupplierID);
                }
                #endregion

                #region Filter by Bank Branch

                if (branchID > -1)
                {
                    additionalFilter.Append(" AND AR_SET_BankBranch.BankBranchID = @BranchID ");
                    additionalFilter2.Append(" AND AR_SET_BankBranch.BankBranchID = @BranchID ");
                    sqlparams[4] = new SqlParameter("@BranchID", branchID);
                }

                #endregion

                #region Filter by Bank Account

                if (branchID > -1)
                {
                    additionalFilter.Append(" AND AR_SET_BankAccount.BankAccountID = @BankAccountID ");
                    additionalFilter2.Append(" AND AR_SET_BankAccount.BankAccountID = @BankAccountID ");
                    sqlparams[5] = new SqlParameter("@BankAccountID", bankaccountID);
                }

                #endregion

                if (documentGroupID != "")
                {
                    additionalFilter.Append(" AND Set_DocumentType.GroupID in " + documentGroupID);
                    additionalFilter2.Append(" AND Set_DocumentType.GroupID in " + documentGroupID);
                    //DocumentGroupFilter.Append(" AND AR_Set_bankAccount.DocumentGroupID in " + documentGroupID);
                }

                if (CheckedTransaction)
                {
                    additionalFilter.Append(" AND AP_PaymentHeader.ActivityDate IS NOT NULL");
                    additionalFilter2.Append(" AND AR_ReceiptTransactionHeader.ActivityDate IS NOT NULL");
                }

                if (UnCheckedTransaction)
                {
                    additionalFilter.Append(" AND AP_PaymentHeader.ActivityDate IS NULL");
                    additionalFilter2.Append(" AND AR_ReceiptTransactionHeader.ActivityDate IS NULL");
                }

                if (IsActivityDate)
                {
                    dateFilter.Append("and AP_PaymentHeader.ActivityDate Between @fromDate And @ToDate");
                    dateFilter2.Append("and AR_ReceiptTransactionHeader.ActivityDate Between @fromDate And @ToDate");
                }
                else
                {
                    dateFilter.Append("and AP_PaymentHeader.DocumentDate Between @fromDate And @ToDate");
                    dateFilter2.Append("and AR_ReceiptTransactionHeader.DocumentDate Between @fromDate And @ToDate");
                }


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_SET_Bank.Query_ClearedAndUnclearedTransaction_Report.Replace("@CompanyID", companies).ToString(), additionalFilter.ToString(), additionalFilter2.ToString(), dateFilter.ToString(), dateFilter2.ToString()), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds;

                #region Filter zero records

                if (filterZeroRecords)
                {
                    List<int> lst_BankAccountIDs = dtToReturn.Tables[0].AsEnumerable().Where(x => x.Field<string>("Type").Equals("RECEIPTS") && x.Field<decimal>("Balance") != 0)
                        .Select(x => x.Field<int>("BankAccountID")).Distinct().ToList();

                    //List<int> lst_PaymentBankAccountIDs = dtToReturn.AsEnumerable().Where(x => x.Field<string>("Type").Equals("PAYMENTS")
                    //    && x.Field<decimal>("Balance") != 0).Select(x => x.Field<int>("BankAccountID")).Distinct().ToList();
                    List<int> lst_PaymentBankAccountIDs = dtToReturn.Tables[0].AsEnumerable().Where(x =>
                    x.Field<decimal>("Balance") != 0 && x.Field<decimal>("OpeningBalance") != 0).Select(x => x.Field<int>("BankAccountID")).Distinct().ToList();

                    foreach (int bankAccountID in lst_PaymentBankAccountIDs)
                        if (!lst_BankAccountIDs.Contains(bankAccountID))
                            lst_BankAccountIDs.Add(bankAccountID);

                    DataTable dt_Filtered = dtToReturn.Tables[0].Clone();

                    foreach (int bankAccountID in lst_BankAccountIDs)
                    {
                        DataRow[] dr_BankAccountRecords = dtToReturn.Tables[0].AsEnumerable().Where(x => x.Field<int>("BankAccountID") == bankAccountID).ToArray();

                        foreach (DataRow dr_ToImport in dr_BankAccountRecords)
                            dt_Filtered.ImportRow(dr_ToImport);
                    }

                    dt_Filtered.AcceptChanges();


                    dtToReturn.Tables.Remove(dtToReturn.Tables[0]);
                    dtToReturn.Tables.Add(dt_Filtered.Copy());

                    //dtToReturn.Tables[0] = dt_Filtered.Copy();
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

        public DataSet GetBankPosition_DashBoard(string companyID, int periodID, int bankID, int branchID, ref int returnTotalRows, string orderBy, DateTime? documentDateFrom = null, DateTime? documentDateTo = null,
          Nullable<short> documentGroupID = null, Nullable<Guid> userID = null, bool documentTypeAuthorization = false, bool documentTypeAuthentication = false
                        , int? pageIndex = null, int? pageSize = null,string BankType = "", SqlConnection objSQLConnection = null)
        {
            DataSet dtToReturn = null;
            StringBuilder additionalFilter = new StringBuilder();
            string documentFilter = string.Empty, qry_DocumentAuthorization = string.Empty;

            if (documentDateFrom != null && documentDateTo != null)
            {
                if (documentDateFrom <= documentDateTo)
                {
                    documentDateFrom = DateUtil.ConvertToDate(DateUtil.ConvertToString(documentDateFrom) + " 00:00:00");
                    documentDateTo = DateUtil.ConvertToDate(DateUtil.ConvertToString(documentDateTo) + " 23:59:59");
                }
            }
            try
            {
                List<SqlParameter> sqlparamsList = new List<SqlParameter>();
                sqlparamsList.Add(new SqlParameter("@CompanyID", companyID));
                sqlparamsList.Add(new SqlParameter("@PeriodID", periodID));
                sqlparamsList.Add(new SqlParameter("@FromDate", documentDateFrom));
                sqlparamsList.Add(new SqlParameter("@ToDate", documentDateTo));
                sqlparamsList.Add(new SqlParameter("@PageIndex", pageIndex));
                sqlparamsList.Add(new SqlParameter("@PageSize", pageSize));
                sqlparamsList.Add(new SqlParameter("@TotalRows", returnTotalRows));

                #region Filter by Bank

                if (bankID > -1)
                {
                    additionalFilter.Append(" AND AR_SET_BankBranch.BankID = @BankID ");
                    sqlparamsList.Add(new SqlParameter("@BankID", bankID));
                }

                #endregion

                #region Filter by Bank Branch

                if (branchID > -1)
                {
                    additionalFilter.Append(" AND AR_SET_BankBranch.BankBranchID = @BranchID ");
                    sqlparamsList.Add(new SqlParameter("@branchID", branchID));
                }

                #endregion

                #region Filter by Document Type Group

                if (documentGroupID.HasValue)
                {
                    documentFilter = " AND AR_SET_BankAccount.DocumentGroupID = @DocumentGroupID ";
                    sqlparamsList.Add(new SqlParameter("@DocumentGroupID", documentGroupID.Value));
                }

                #endregion

                if (documentTypeAuthorization)
                {
                    sqlparamsList.Add(new SqlParameter("@UserID", userID.Value));
                }



                if (BankType == "cash")
                {
                    additionalFilter.Append(" and AR_SET_BankAccount.bankaccountname like '%cash%'");
                }

                if (BankType == "bank")
                {
                    additionalFilter.Append(" and AR_SET_BankAccount.bankaccountname not like '%cash%'");
                }

                SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                sqlparamTotalRows.Direction = ParameterDirection.Output;

                SqlParameter[] sqlparams = sqlparamsList.ToArray();

                companyID = "(" + companyID + ")";

                //DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_SET_Bank.Query_GetBankPosition.ToString().Replace("@CompanyID", companyID), additionalFilter.ToString(), documentFilter,
                //    qry_DocumentAuthorization), sqlparams);

                DataSet ds = new DataSet();

                if (objSQLConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, string.Format(FnI.QueryResourceLibrary.AR.AR_SET_Bank.Query_GetBankPosition.ToString().Replace("@CompanyID", companyID), additionalFilter.ToString(), documentFilter,
                    qry_DocumentAuthorization) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty), string.Format(FnI.QueryResourceLibrary.AR.AR_SET_Bank.Query_GetBankPosition.ToString().Replace("@CompanyID", companyID), additionalFilter.ToString(), documentFilter,
                    qry_DocumentAuthorization) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty)), sqlparams);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objSQLConnection, CommandType.Text, string.Format(Paging, string.Format(FnI.QueryResourceLibrary.AR.AR_SET_Bank.Query_GetBankPosition.ToString().Replace("@CompanyID", companyID), additionalFilter.ToString(), documentFilter,
                    qry_DocumentAuthorization) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty), string.Format(FnI.QueryResourceLibrary.AR.AR_SET_Bank.Query_GetBankPosition.ToString().Replace("@CompanyID", companyID), additionalFilter.ToString(), documentFilter,
                    qry_DocumentAuthorization) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty)), sqlparams);
                }


                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds;


                returnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }
      

        
        public static DataTable GetAllDataForSearch(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_SelectAllForSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAllForSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAllForSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAllForSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAllForSearch_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllForSearch_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }





        public DataSet GetDailyBankPositionReport(string companyID, int periodID, DateTime fromDate, DateTime toDate, string BranchIds, int fromBank, int toBank,
            int FromAccountID, int ToAccountID, Guid userID, string documentGroupID = "", bool filterZeroRecords = false, bool discardReversedDocuments = false,
            bool documentTypeAuthentication = false)
        {
            DataSet dtToReturn = null;
            string qry_docuTypeAuthenticate = string.Empty;
            StringBuilder sb = new StringBuilder();
            StringBuilder DocumentGroupFilter = new StringBuilder();
            string qry_ReceiptReversal = string.Empty, qry_PaymentReversal = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);

                if (fromBank != -1 && toBank != -1)
                    sb.Append(" AND AR_SET_BankBranch.BankID between " + fromBank + " and " + toBank + " ");

                if (FromAccountID != -1 && ToAccountID != -1)
                {
                    sb.Append(" AND AR_SET_BankAccount.BankAccountID between " + FromAccountID + " and " + ToAccountID + " ");
                }
                else if (FromAccountID != -1 && ToAccountID == -1)
                {
                    sb.Append(" AND AR_SET_BankAccount.BankAccountID = " + FromAccountID + " ");
                }
                else if (FromAccountID == -1 && ToAccountID != -1)
                {
                    sb.Append(" AND AR_SET_BankAccount.BankAccountID = " + ToAccountID + " ");
                }

                if (documentGroupID != "")
                    DocumentGroupFilter.Append(" AND Set_DocumentType.GroupID in " + documentGroupID);
                    //DocumentGroupFilter.Append(" AND AR_Set_bankAccount.DocumentGroupID in " + documentGroupID);

                if (discardReversedDocuments)
                {
                    qry_ReceiptReversal = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                    qry_PaymentReversal = " And AP_PaymentHeader.ReversalComments is null ";
                }


                if (documentTypeAuthentication)
                {
                    sqlparams[4] = new SqlParameter("@UserID", userID);
                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_SET_Bank.Query_Get_DailyBankPosition, sb.ToString(), DocumentGroupFilter.ToString(),
                    qry_ReceiptReversal, qry_PaymentReversal, qry_ReceiptReversal.Replace("AR_ReceiptTransactionHeader", "rh"),
                    qry_PaymentReversal.Replace("AP_PaymentHeader", "ph"), qry_docuTypeAuthenticate).Replace("@CompanyID", companyID), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds;

                #region Filter zero records

                if (filterZeroRecords)
                {
                    List<int> lst_BankAccountIDs = dtToReturn.Tables[0].AsEnumerable().Where(x => x.Field<string>("Type").Equals("RECEIPTS") && x.Field<decimal>("Balance") != 0)
                        .Select(x => x.Field<int>("BankAccountID")).Distinct().ToList();

                    //List<int> lst_PaymentBankAccountIDs = dtToReturn.AsEnumerable().Where(x => x.Field<string>("Type").Equals("PAYMENTS")
                    //    && x.Field<decimal>("Balance") != 0).Select(x => x.Field<int>("BankAccountID")).Distinct().ToList();
                    List<int> lst_PaymentBankAccountIDs = dtToReturn.Tables[0].AsEnumerable().Where(x =>
                    x.Field<decimal>("Balance") != 0 && x.Field<decimal>("OpeningBalance") != 0).Select(x => x.Field<int>("BankAccountID")).Distinct().ToList();

                    foreach (int bankAccountID in lst_PaymentBankAccountIDs)
                        if (!lst_BankAccountIDs.Contains(bankAccountID))
                            lst_BankAccountIDs.Add(bankAccountID);

                    DataTable dt_Filtered = dtToReturn.Tables[0].Clone();

                    foreach (int bankAccountID in lst_BankAccountIDs)
                    {
                        DataRow[] dr_BankAccountRecords = dtToReturn.Tables[0].AsEnumerable().Where(x => x.Field<int>("BankAccountID") == bankAccountID).ToArray();

                        foreach (DataRow dr_ToImport in dr_BankAccountRecords)
                            dt_Filtered.ImportRow(dr_ToImport);
                    }

                    dt_Filtered.AcceptChanges();


                    dtToReturn.Tables.Remove(dtToReturn.Tables[0]);
                    dtToReturn.Tables.Add(dt_Filtered.Copy());

                    //dtToReturn.Tables[0] = dt_Filtered.Copy();
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


        public DataTable GetDailyCashPositionReport(string companyID, int periodID, DateTime fromDate, DateTime toDate, string BranchIds, int fromBank, int toBank,
    Guid userID, string documentGroupID = "", bool filterZeroRecords = false, bool discardReversedDocuments = false,
    bool documentTypeAuthentication = false)
        {
            DataTable dtToReturn = null;
            string qry_docuTypeAuthenticate = string.Empty;
            StringBuilder sb = new StringBuilder();
            StringBuilder DocumentGroupFilter = new StringBuilder();
            string qry_ReceiptReversal = string.Empty, qry_PaymentReversal = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);

                if (fromBank != -1 && toBank != -1)
                    sb.Append(" AND AR_SET_BankBranch.BankID between " + fromBank + " and " + toBank + " ");

                if (documentGroupID != "")
                    DocumentGroupFilter.Append(" AND Set_DocumentType.GroupID in " + documentGroupID);
                    //DocumentGroupFilter.Append(" AND AR_Set_bankAccount.DocumentGroupID in " + documentGroupID);

                if (discardReversedDocuments)
                {
                    qry_ReceiptReversal = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                    qry_PaymentReversal = " And AP_PaymentHeader.ReversalComments is null ";
                }


                if (documentTypeAuthentication)
                {
                    sqlparams[4] = new SqlParameter("@UserID", userID);
                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_SET_Bank.Query_Get_DailyCashPosition, sb.ToString(), DocumentGroupFilter.ToString(),
                    qry_ReceiptReversal, qry_PaymentReversal, qry_ReceiptReversal.Replace("AR_ReceiptTransactionHeader", "rh"),
                    qry_PaymentReversal.Replace("AP_PaymentHeader", "ph"), qry_docuTypeAuthenticate).Replace("@CompanyID", companyID), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];

                #region Filter zero records

                if (filterZeroRecords)
                {
                    List<int> lst_BankAccountIDs = dtToReturn.AsEnumerable().Where(x => x.Field<string>("Type").Equals("RECEIPTS") && x.Field<decimal>("Balance") != 0)
                        .Select(x => x.Field<int>("BankAccountID")).Distinct().ToList();

                    //List<int> lst_PaymentBankAccountIDs = dtToReturn.AsEnumerable().Where(x => x.Field<string>("Type").Equals("PAYMENTS")
                    //    && x.Field<decimal>("Balance") != 0).Select(x => x.Field<int>("BankAccountID")).Distinct().ToList();
                    List<int> lst_PaymentBankAccountIDs = dtToReturn.AsEnumerable().Where(x =>
                    x.Field<decimal>("Balance") != 0 && x.Field<decimal>("OpeningBalance") != 0).Select(x => x.Field<int>("BankAccountID")).Distinct().ToList();

                    foreach (int bankAccountID in lst_PaymentBankAccountIDs)
                        if (!lst_BankAccountIDs.Contains(bankAccountID))
                            lst_BankAccountIDs.Add(bankAccountID);

                    DataTable dt_Filtered = dtToReturn.Clone();

                    foreach (int bankAccountID in lst_BankAccountIDs)
                    {
                        DataRow[] dr_BankAccountRecords = dtToReturn.AsEnumerable().Where(x => x.Field<int>("BankAccountID") == bankAccountID).ToArray();

                        foreach (DataRow dr_ToImport in dr_BankAccountRecords)
                            dt_Filtered.ImportRow(dr_ToImport);
                    }

                    dt_Filtered.AcceptChanges();
                    dtToReturn = dt_Filtered.Copy();
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


        public static bool IsBranchEdit(int branchid, ref string message)
        {
            bool result = false;

            try
            {
                DataSet ds = null;
                SqlParameter[] parms = new SqlParameter[1];
                parms[0] = new SqlParameter("@branchid", branchid);

                #region Query
                string query_IsBatchInUse = @"Select [AR_SET_BankAccount].[BankAccountID] from AR_SET_BankAccount
                    where AR_SET_BankAccount.[Status]=1
                    And AR_SET_BankAccount.[BankBranchID]=@branchid";
                #endregion


                ds = SqlHelper.ExecuteDataset(CommandType.Text, query_IsBatchInUse, parms);

                if (ds.Tables.Count > 0)
                    if (ds.Tables[0].Rows.Count > 0)
                        message = " Batch is already exit  " + ds.Tables[0].Rows[0][0].ToString() + ".";
                    else
                        result = true;
                else if (ds.Tables[1].Rows.Count > 0)
                    message = " Batch is already exit  " + ds.Tables[1].Rows[0][0].ToString() + ".";
                else
                    result = true;

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return result;
        }
        public static object GetAllDataLetterOfCreadit(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> CurrentFiscalID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To)
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@FromDate", Date_From);
                sqlparams[4] = new SqlParameter("@ToDate", Date_To);

                ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_SET_Bank.Query_LetterOfCreadit, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return ds.Tables[0];
        }

       
        public static object GetAllDataLimeSlakerExpenses(string companyID, Nullable<int> periodID, Nullable<int> CurrentFiscalID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To)
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@FromDate", Date_From);
                sqlparams[4] = new SqlParameter("@ToDate", Date_To);

                companyID = "(" + companyID + ")";

                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_SET_Bank.Query_LimeSlakerExpenses).Replace("@CompanyID", companyID), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return ds.Tables[0];
        }

        public static object GetAllDataCaneUnloadingExpensesDetailReport(string companyID, Nullable<int> periodID, Nullable<int> CurrentFiscalID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To)
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@FromDate", Date_From);
                sqlparams[4] = new SqlParameter("@ToDate", Date_To);

                companyID = "(" + companyID + ")";

                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_SET_Bank.Query_CaneUnloadingExpensesDetailReport).Replace("@CompanyID", companyID), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return ds.Tables[0];
        }
       

        #endregion


    
        public object GetAllDataBankGuaranteesSummaryDetail(string companyID,int periodID,int? bankID,DateTime? Date_From,DateTime? Date_To,int? StatusID,Guid CurrentUserID)
        {
 	        DataTable dt = null;
                    DataSet ds = null;
                    StringBuilder sb = new StringBuilder(5);
                    try
                    {
                        SqlParameter[] sqlparams = new SqlParameter[5];
                        sqlparams[0] = new SqlParameter("@companyID", companyID);
                        sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                        sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                        sqlparams[3] = new SqlParameter("@Dateto", Date_To);

                        if (StatusID.HasValue)
                        {
                            sqlparams[4] = new SqlParameter("@StatusID", StatusID.ToString());
                        }
                        else
                        {
                            sqlparams[4] = new SqlParameter("@StatusID", DBNull.Value);
                        }
                        //if (Date_From != null && Date_To != null)
                        //    sb.Append(" AND [AP_PaymentHeader].[DocumentDate ] BETWEEN '" + ((DateTime)Date_From).ToString("yyyy-MM-dd") + "' AND '" + ((DateTime)Date_To).ToString("yyyy-MM-dd") + "'");
                        //else if (Date_From != null)
                        //    sb.Append(" AND [AP_PaymentHeader].[DocumentDate ] >= '" + ((DateTime)Date_From).ToString("yyyy-MM-dd") + "'");
                        //else if (Date_To != null)
                        //    sb.Append(" AND [AP_PaymentHeader].[DocumentDate ] <= '" + ((DateTime)Date_To).ToString("yyyy-MM-dd") + "'");
                        //if (bankID.HasValue)
                        //{
                        //    sqlparams[2] = new SqlParameter("@BankID", bankID.Value);
                        //}
                        //else
                        //    sqlparams[2] = new SqlParameter("@BankID", DBNull.Value);

                        if (bankID > 0)
                        {
                            sb.Append(" where AR_Set_bank.BankID =" + bankID);
                        }
                       // ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_SET_Bank.Query_GetAllDataBankGuaranteesSummaryDetail, sqlparams);

                        companyID = "(" + companyID + ")";
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_SET_Bank.Query_GetAllDataBankGuaranteesSummaryDetail, sb).Replace("@CompanyID", companyID), sqlparams);


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


        public object GetLetterOfGuarantee(int companyID, int periodID, int? bankID, DateTime? Date_From, DateTime? Date_To, int? businessPartnerID)
        {
            DataTable dt = null;
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@companyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@Dateto", Date_To);
                sqlparams[4] = new SqlParameter("@businessPartnerID", businessPartnerID);
                //sqlparams[5] = new SqlParameter("@BankID", bankID);
        

                if (bankID > 0)
                {
                    sb.Append(" and AR_Set_bank.bankID =" + bankID);
                }
                if (businessPartnerID > 0)
                {
                    sb.Append(" And Set_BankGuaranteeDetail.BusinessPartnerID = " + businessPartnerID);
                }
                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_SET_Bank.Query_LetterOfGuarantee, sb), sqlparams);


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


        public DataTable GetBankStatusDataSummaryForFinancial(String companyID, int periodID, int bankID, int branchID, DateTime fromDate, DateTime toDate, int? bankAccountTypeID,
        int? BankAccountNo, bool discardReversedDocuments, Guid userID, string documentGroupID = "", Nullable<int> DocumentTypeID = null, bool documentTypeAuthentication = false
            , string fromcode = "", string tocode = "",int FinancialHeaderID = 0)
        {
            DataTable dt;
            StringBuilder filter = new StringBuilder();
            string qry_ReceiptReversal = string.Empty, qry_PaymentReversal = string.Empty, qry_docuTypeAuthenticate = string.Empty;

            if (discardReversedDocuments)
            {
                qry_ReceiptReversal = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                qry_PaymentReversal = " And AP_PaymentHeader.ReversalComments is null ";
            }

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[11];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Fromdate", DateUtil.ConvertToDate(DateUtil.ConvertToString(fromDate) + " 00:00:00"));
                sqlparams[3] = new SqlParameter("@Dateto", DateUtil.ConvertToDate(DateUtil.ConvertToString(toDate) + " 23:59:59"));

                #region Filter by Bank

                if (bankID != -1)
                {
                    filter.Append(" And AR_SET_Bank.BankID = @BankID ");
                    sqlparams[4] = new SqlParameter("@BankID", bankID);
                }

                #endregion

                #region Filter by Bank Branch

                if (branchID != -1)
                {
                    filter.Append(" And AR_SET_BankBranch.BankBranchID = @BranchID ");
                    sqlparams[5] = new SqlParameter("@BranchID", branchID);
                }

                #endregion

                if (bankAccountTypeID != -1)
                    filter.Append(" And AR_SET_BankAccount.BankAccountTypeID = " + bankAccountTypeID + " ");

                if (BankAccountNo != -1)
                    filter.Append(" And AR_SET_BankAccount.BankAccountID = " + BankAccountNo + " ");

                if (documentGroupID != "")
                    filter.Append(" AND Set_DocumentType.GroupID in " + documentGroupID + " or Set_DocumentType.GroupID IS NULL");

                filter.Append(" AND AR_Set_bankAccount.PaymentCCID between @fromcode and @tocode ");


                if (documentTypeAuthentication)
                {
                    sqlparams[6] = new SqlParameter("@UserID", userID);
                }

                if (DocumentTypeID.HasValue)
                {
                    sqlparams[7] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                }
                else
                {
                    sqlparams[7] = new SqlParameter("@DocumentTypeID", DBNull.Value);
                }

                sqlparams[8] = new SqlParameter("@fromcode", fromcode);
                sqlparams[9] = new SqlParameter("@tocode", tocode);
                sqlparams[10] = new SqlParameter("@FinancialHeaderID", FinancialHeaderID); 
                

                companyID = "(" + companyID + ")";


                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_SET_Bank.Query_GetBankStatusSummaryFinancial.ToString().Replace("@CompanyID", companyID), filter.ToString(), qry_ReceiptReversal,
                    qry_PaymentReversal, qry_docuTypeAuthenticate), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetBankReconciliationSummary2Data(string companyID, int periodID, Nullable<int> bankID, Nullable<int> branchID, int? BankAccountID, Nullable<DateTime> fromDate,
Nullable<DateTime> toDate, Nullable<int> bankAccountTypeID)
        {
            DataTable dtToReturn = null;
            DataSet dsToReturn;
            string filterReceiptHeader = string.Empty;
            string filterPaymentHeader = string.Empty;
            string filterReceiptDetail = string.Empty;
            string filterPaymentDetail = string.Empty;
            StringBuilder otherFilters = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                #region Bank filter

                if (bankID.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@BankID", bankID.Value);
                    filterReceiptHeader += " AND AR_Set_BankBranch.BankID = @BankID ";
                    filterReceiptDetail += " AND AR_Set_BankBranch.BankID = @BankID ";
                    filterPaymentHeader += " AND AR_Set_BankBranch.BankID = @BankID ";
                    filterPaymentDetail += " AND AR_Set_BankBranch.BankID = @BankID ";
                }
                else
                    sqlparams[2] = new SqlParameter("@BankID", DBNull.Value);

                #endregion

                #region Branch filter

                if (branchID.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@BranchID", branchID.Value);
                    filterReceiptHeader += " AND AR_Set_BankBranch.BankBranchID = @BranchID ";
                    filterReceiptDetail += " AND AR_Set_BankBranch.BankBranchID = @BranchID ";
                    filterPaymentHeader += " AND AR_Set_BankBranch.BankBranchID = @BranchID ";
                    filterPaymentDetail += " AND AR_Set_BankBranch.BankBranchID = @BranchID ";
                }
                else
                    sqlparams[3] = new SqlParameter("@BranchID", DBNull.Value);

                #endregion

             

                #region Date Range filter

                if (toDate.HasValue)
                {
                    sqlparams[5] = new SqlParameter("@ToDate", toDate);
                    //filterReceiptDetail += " AND AR_ReceiptTransactionHeader.DocumentDate <= @ToDate ";
                    //filterPaymentDetail += " AND AP_PaymentHeader.DocumentDate <= @ToDate ";
                }

                #region Bank Acocount ID
                if (BankAccountID.HasValue)
                {
                    sqlparams[6] = new SqlParameter("@BankAccountID", BankAccountID.Value);
                    filterReceiptHeader += " AND AR_Set_BankAccount.BankAccountID = @BankAccountID ";
                    filterReceiptDetail += " AND AR_Set_BankAccount.BankAccountID = @BankAccountID ";
                    filterPaymentHeader += " AND AR_Set_BankAccount.BankAccountID = @BankAccountID ";
                    filterPaymentDetail += " AND AR_Set_BankAccount.BankAccountID = @BankAccountID ";
                }
                #endregion
                else
                    sqlparams[6] = new SqlParameter("@BankAccountID", DBNull.Value);

                if (bankAccountTypeID != -1)
                    otherFilters.Append(" AND AR_SET_BankAccount.BankAccountTypeID = " + bankAccountTypeID + " ");

                #endregion


                companyID = "(" + companyID + ")";

                string query = string.Format(FnI.QueryResourceLibrary.AR.AR_SET_Bank.Query_GetBankReconciliationSummary2Data, filterReceiptHeader, filterPaymentHeader, otherFilters.ToString()).Replace("@CompanyID", companyID);

                dsToReturn = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
                // RemoveEmptyAccounts(ref dsToReturn);

                if (dsToReturn != null && dsToReturn.Tables.Count > 0)
                    dtToReturn = dsToReturn.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public DataTable GetCombinedBankReconciliationReport2Data(int companyID, int periodID, Nullable<int> bankID, Nullable<int> branchID, Nullable<DateTime> fromDate,
Nullable<DateTime> toDate, Nullable<int> bankAccountTypeID)
        {
            DataTable dtToReturn = null;
            DataSet dsToReturn;
            string filterReceiptHeader = string.Empty;
            string filterPaymentHeader = string.Empty;
            string filterReceiptDetail = string.Empty;
            string filterPaymentDetail = string.Empty;
            StringBuilder otherFilters = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                #region Bank filter

                if (bankID.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@BankID", bankID.Value);
                    filterReceiptHeader += " AND AR_Set_BankBranch.BankID = @BankID ";
                    filterReceiptDetail += " AND AR_Set_BankBranch.BankID = @BankID ";
                    filterPaymentHeader += " AND AR_Set_BankBranch.BankID = @BankID ";
                    filterPaymentDetail += " AND AR_Set_BankBranch.BankID = @BankID ";
                }
                else
                    sqlparams[2] = new SqlParameter("@BankID", DBNull.Value);

                #endregion

                #region Branch filter

                if (branchID.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@BranchID", branchID.Value);
                    filterReceiptHeader += " AND AR_Set_BankBranch.BankBranchID = @BranchID ";
                    filterReceiptDetail += " AND AR_Set_BankBranch.BankBranchID = @BranchID ";
                    filterPaymentHeader += " AND AR_Set_BankBranch.BankBranchID = @BranchID ";
                    filterPaymentDetail += " AND AR_Set_BankBranch.BankBranchID = @BranchID ";
                }
                else
                    sqlparams[3] = new SqlParameter("@BranchID", DBNull.Value);

                #endregion

                #region Date Range filter

                if (toDate.HasValue)
                {
                    sqlparams[5] = new SqlParameter("@ToDate", toDate);
                    //filterReceiptDetail += " AND AR_ReceiptTransactionHeader.DocumentDate <= @ToDate ";
                    //filterPaymentDetail += " AND AP_PaymentHeader.DocumentDate <= @ToDate ";
                }

                if (bankAccountTypeID != -1)
                    otherFilters.Append(" AND AR_SET_BankAccount.BankAccountTypeID = " + bankAccountTypeID + " ");

                #endregion

                string query = string.Format(FnI.QueryResourceLibrary.AR.AR_SET_Bank.Query_GetCombinedBankReconciliationReport2Data, filterReceiptHeader, filterPaymentHeader, otherFilters.ToString());

                dsToReturn = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
                // RemoveEmptyAccounts(ref dsToReturn);

                if (dsToReturn != null && dsToReturn.Tables.Count > 0)
                    dtToReturn = dsToReturn.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public DataTable GetBranchNameByBankID(int BankID, SqlConnection obj_SQLConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@BankID", BankID);

                string Query_GetBranchName = @" SELECT BankBranchID, BankBranchName FROM AR_SET_BankBranch WHERE Status = 1 AND IsActive = 1 AND BankID = @BankID ";

                if (obj_SQLConnection != null)
                    dt = SqlHelper.ExecuteDataset(obj_SQLConnection, CommandType.Text, Query_GetBranchName, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetBranchName, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public  DataSet GetBankReconciliationReport2Data(string companyID, int periodID, Nullable<int> bankID, Nullable<int> branchID, int? BankAccountID, Nullable<DateTime> fromDate,
    Nullable<DateTime> toDate, Nullable<int> bankAccountTypeID)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;
            DataSet dsToReturn;
            string filterReceiptHeader = string.Empty;
            string filterPaymentHeader = string.Empty;
            string filterReceiptDetail = string.Empty;
            string filterPaymentDetail = string.Empty;
            StringBuilder otherFilters = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                #region Bank filter

                if (bankID.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@BankID", bankID.Value);
                    filterReceiptHeader += " AND AR_Set_BankBranch.BankID = @BankID ";
                    filterReceiptDetail += " AND AR_Set_BankBranch.BankID = @BankID ";
                    filterPaymentHeader += " AND AR_Set_BankBranch.BankID = @BankID ";
                    filterPaymentDetail += " AND AR_Set_BankBranch.BankID = @BankID ";
                }
                else
                    sqlparams[2] = new SqlParameter("@BankID", DBNull.Value);

                #endregion

                #region Branch filter

                if (branchID.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@BranchID", branchID.Value);
                    filterReceiptHeader += " AND AR_Set_BankBranch.BankBranchID = @BranchID ";
                    filterReceiptDetail += " AND AR_Set_BankBranch.BankBranchID = @BranchID ";
                    filterPaymentHeader += " AND AR_Set_BankBranch.BankBranchID = @BranchID ";
                    filterPaymentDetail += " AND AR_Set_BankBranch.BankBranchID = @BranchID ";
                }
                else
                    sqlparams[3] = new SqlParameter("@BranchID", DBNull.Value);

                #endregion


                #region Date Range filter

                if (toDate.HasValue)
                {
                    sqlparams[5] = new SqlParameter("@ToDate", toDate);
                    //filterReceiptDetail += " AND AR_ReceiptTransactionHeader.DocumentDate <= @ToDate ";
                    //filterPaymentDetail += " AND AP_PaymentHeader.DocumentDate <= @ToDate ";
                }

                if (bankAccountTypeID != -1)
                    otherFilters.Append(" AND AR_SET_BankAccount.BankAccountTypeID = " + bankAccountTypeID + " ");

                #endregion

                #region Bank Acocount ID
                if (BankAccountID.HasValue)
                {
                    sqlparams[6] = new SqlParameter("@BankAccountID", BankAccountID.Value);
                    filterReceiptHeader += " AND AR_Set_BankAccount.BankAccountID = @BankAccountID ";
                    filterReceiptDetail += " AND AR_Set_BankAccount.BankAccountID = @BankAccountID ";
                    filterPaymentHeader += " AND AR_Set_BankAccount.BankAccountID = @BankAccountID ";
                    filterPaymentDetail += " AND AR_Set_BankAccount.BankAccountID = @BankAccountID ";
                }
                #endregion
                else
                    sqlparams[6] = new SqlParameter("@BankAccountID", DBNull.Value);

                companyID = "(" + companyID + ")";

                string query = string.Format(FnI.QueryResourceLibrary.AR.AR_SET_Bank.Query_GetBankReconciliationReport2Data, filterReceiptHeader, filterPaymentHeader, otherFilters.ToString()).Replace("@CompanyID", companyID);

                ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
                // RemoveEmptyAccounts(ref dsToReturn);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }

    }

}
