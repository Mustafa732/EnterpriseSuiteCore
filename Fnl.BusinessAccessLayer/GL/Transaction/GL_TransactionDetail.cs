using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;

using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;

namespace FnI.BusinessAccessLayer.GL.Transaction
{
    public partial class GL_TransactionDetail : BaseClass
    {
        #region Class Methods

        public static DataTable GetTransactionIDandData(int companyID, int periodID, int paymentdetailID, int loansanctionDID, int loansanctionHID, int DOCTypeID, string query = "")
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DNo", paymentdetailID);
                sqlparams[3] = new SqlParameter("@SHID", loansanctionHID);
                sqlparams[4] = new SqlParameter("@SDID", loansanctionDID);
                sqlparams[5] = new SqlParameter("@DTID", DOCTypeID);


                dt = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetAllActiveData(int companyID, int periodID, int transactionID, int FiscalID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TransactionID", transactionID);
                sqlparams[3] = new SqlParameter("@FiscalID", FiscalID);


                dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_SelectAll_By_TransactionID, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public bool DeleteDataByKey(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[9];
                sqlparams[0] = new SqlParameter("@UpdatedBy", f_UpdatedBy.Value);
                sqlparams[1] = new SqlParameter("@UpdateDate", f_UpdateDate.Value);
                sqlparams[2] = new SqlParameter("@UpdatedByValue", f_UpdatedByValue);
                sqlparams[3] = new SqlParameter("@Status", f_Status);
                sqlparams[4] = new SqlParameter("@TransactionID", f_TransactionID);
                sqlparams[5] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[6] = new SqlParameter("@ChartOfAccountID", f_ChartOfAccountID);
                sqlparams[7] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[8] = new SqlParameter("@SerialNo", f_SerialNo);

                if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_Delete_By_PrimaryKey, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static bool DeleteDataOnRateRevers(int companyId, int periodId, string glts, SqlTransaction objTransaction)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyId);
                sqlparams[1] = new SqlParameter("@PeriodID", periodId);

                if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_DeleteAllOnRateRevers, glts), sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static bool DeleteDetailDataByKey(SqlTransaction objTransaction, int companyID, int periodID, int transactionID, int chartOfAccountID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TransactionID", transactionID);
                sqlparams[3] = new SqlParameter("@ChartOfAccountID", chartOfAccountID);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_DeleteDetail_ByKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_DeleteDetail_ByKey, sqlparams) > 0)
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

        public bool DeleteDataBy_HeaderKey(SqlTransaction objTransaction, int currentChartOfAccID, Guid UpdatedBy, DateTime UpdateDate, string UpdateByValue,
            bool Status, int companyid, int periodid, int transactionID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@UpdatedBy", UpdatedBy);
                sqlparams[1] = new SqlParameter("@UpdateDate", UpdateDate);
                sqlparams[2] = new SqlParameter("@UpdatedByValue", UpdateByValue);
                sqlparams[3] = new SqlParameter("@Status", Status);
                sqlparams[4] = new SqlParameter("@TransactionID", transactionID);
                sqlparams[5] = new SqlParameter("@CompanyID", companyid);
                sqlparams[6] = new SqlParameter("@ChartOfAccountID", currentChartOfAccID);
                sqlparams[7] = new SqlParameter("@PeriodID", periodid);

                SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_Delete_By_Primary_HeaderKey, sqlparams);
                result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public DataTable GetDataByKeyTable(int transactionID, int companyID, int periodID, bool status = true, SqlConnection objSQL_Connection = null)
        {
            DataSet ds = null;
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@TransactionID", transactionID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", status);

                #region Query



                #endregion

                if (objSQL_Connection != null)
                    ds = SqlHelper.ExecuteDataset(objSQL_Connection, CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_Select_By_TransactionID_ChartOfAccId_CompanyId, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_Select_By_TransactionID_ChartOfAccId_CompanyId, sqlparams);

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

        public int GetNewSerialNo(int transactionID, int companyID, int periodID)
        {
            int maxSerialNo = 0;

            string Query_NewSerialNo_ByCompanyPeriodTransactionIDs = @" SELECT ISNULL(MAX(SerialNo), 0) + 1 AS SerialNo FROM [GL_TransactionDetail] 
            WHERE [TransactionID] = @TransactionID AND [CompanyID] = @CompanyID AND [PeriodID] = @PeriodID; ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@TransactionID", transactionID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                maxSerialNo = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_NewSerialNo_ByCompanyPeriodTransactionIDs, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return maxSerialNo;
        }

        public static DataTable GetTrialBalanceDataDepartmentWise(int costcenter, int department, string companyID, int chartOfAccountID, int periodID, int fromDocumentType,
            int toDocumentType, Guid userID, DateTime? dateFrom = null, DateTime? dateTo = null, string ReportType = null, bool? chk1 = null, bool? chk2 = null,
            bool? chk3 = null, bool? chk4 = null, Nullable<short> documentGroupID = null, bool discardReversedDocuments = false,
            bool documentTypeAuthentication = false, bool filterZeroResults = false, string accountCodeFrom = null, string accountCodeTo = null, int FiscalID = 0, string documentGroupIDs = "", string AccountTypeIDs = "", bool isdepartmentwisereport = false)
        {
            DataTable dtToReturn = null;
            string qry_docuTypeAuthenticate = string.Empty;
            StringBuilder debitCreditFilter = new StringBuilder();
            StringBuilder additionalFilter = new StringBuilder();
            string accountCodeFilter = string.Empty;
            try
            {
                #region Query Parameters

                List<SqlParameter> sqlparams = new List<SqlParameter>();
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));
                sqlparams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlparams.Add(new SqlParameter("@ToDate", dateTo));

                if (costcenter == -1)
                    sqlparams.Add(new SqlParameter("@cc", DBNull.Value));
                else
                    sqlparams.Add(new SqlParameter("@cc", costcenter));

                if (department == -1)
                    sqlparams.Add(new SqlParameter("@did", DBNull.Value));
                else
                    sqlparams.Add(new SqlParameter("@did", department));

                #endregion

                #region Filter by Account Codes

                if (!string.IsNullOrEmpty(accountCodeFrom) && !string.IsNullOrEmpty(accountCodeTo))
                {
                    accountCodeFilter = " AND GL_CodeCombinationDetail.CodeCombinationID BETWEEN @AccountCodeFrom AND @AccountCodeTo ";
                    sqlparams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                    sqlparams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                }
                else if (!string.IsNullOrEmpty(accountCodeFrom))
                {
                    accountCodeFilter = " AND GL_CodeCombinationDetail.CodeCombinationID >= @AccountCodeFrom ";
                    sqlparams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                }
                else if (!string.IsNullOrEmpty(accountCodeTo))
                {
                    accountCodeFilter = " AND GL_CodeCombinationDetail.CodeCombinationID <= @AccountCodeTo ";
                    sqlparams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                }

                #endregion

                if (ReportType.Equals("TrialBalanceReport"))
                    sqlparams.Add(new SqlParameter("@DetailPostingAllowedFlag", DBNull.Value));
                else if (ReportType.Equals("DetailTrialBalanceReport"))
                {
                    #region

                    sqlparams.Add(new SqlParameter("@DetailPostingAllowedFlag", DBNull.Value));

                    if (chk1.Value)
                        debitCreditFilter.AppendLine(" having SUM(Debit) <> 0 and SUM(credit) <> 0 ");

                    if (chk2.Value)
                        debitCreditFilter.AppendLine("having SUM(Debit)  = 0 and SUM(credit) = 0");

                    if (chk3.Value)
                    {
                        debitCreditFilter.AppendLine("having (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) > 0 THEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END) -  (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) < 0 THEN ABS((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END)=0 ");
                    }

                    if (chk4.Value)
                    {
                        debitCreditFilter.AppendLine(" having (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) > 0 THEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END) -  (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) < 0 THEN ABS((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END) <> 0 ");
                    }

                    #endregion
                }
                else if (ReportType.Equals("DetailTrialBalanceReportNetOFF") || ReportType.Equals("ClosingTrialBalanceReport") || ReportType.Equals("DetailTrialBalanceReport_Department"))
                {
                    #region

                    sqlparams.Add(new SqlParameter("@DetailPostingAllowedFlag", DBNull.Value));

                    if (chk1 == true)
                        debitCreditFilter.AppendLine(" having SUM(Debit) <> 0 and SUM(credit) <> 0 ");

                    if (chk2 == true)
                        debitCreditFilter.AppendLine("having SUM(Debit)  = 0 and SUM(credit) = 0");

                    if (chk3 == true)
                    {
                        debitCreditFilter.AppendLine("having (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) > 0 THEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END) -  (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) < 0 THEN ABS((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END)=0 ");
                    }

                    if (chk4 == true)
                    {
                        debitCreditFilter.AppendLine(" having (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) > 0 THEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END) -  (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) < 0 THEN ABS((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END) <> 0 ");
                    }

                    #endregion
                }
                else if (ReportType.Equals("LedgerWiseTrialBalanceReport"))
                    sqlparams.Add(new SqlParameter("@DetailPostingAllowedFlag", true));

                if (documentGroupID.HasValue)
                    additionalFilter.Append(" AND Set_DocumentType.GroupID = " + documentGroupID);

                if (fromDocumentType != -1 && toDocumentType != -1)
                    additionalFilter.Append(" AND GL_TransactionHeader.DocumentTypeID between " + fromDocumentType + " AND " + toDocumentType + " ");


                if (documentGroupIDs.ToString() != string.Empty)
                    additionalFilter.Append(" AND Set_DocumentType.GroupID in " + documentGroupIDs.ToString());

                if (AccountTypeIDs.ToString() != string.Empty)
                    additionalFilter.Append(" AND GL_CodeCombinationDetail.AccountTypeID in " + AccountTypeIDs.ToString());



                if (discardReversedDocuments)
                    additionalFilter.Append(" AND IsNull(GL_TransactionHeader.IsReversed, 0) = 0 And GL_TransactionHeader.ReversalID is null ");

                if (documentTypeAuthentication)
                {
                    sqlparams.Add(new SqlParameter("@UserID", userID));
                    //                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
                    //AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                sqlparams.Add(new SqlParameter("@FiscalID", FiscalID));

                companyID = "(" + companyID + ")";
                string Query_TrailBalance = FnI.QueryResourceLibrary.GL.GL_CodeCombinationDetail.QueryTrailbalanceNetOFF.ToString();
                if (isdepartmentwisereport == true)
                    Query_TrailBalance = FnI.QueryResourceLibrary.GL.GL_CodeCombinationDetail.Query_TrialBalanceDepartmentWise.ToString();

                string query = Query_TrailBalance.Replace("{AccountCodeFilter}", accountCodeFilter);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query.ToString().Replace("@CompanyID", companyID), debitCreditFilter.ToString(), additionalFilter.ToString(),
                    qry_docuTypeAuthenticate), sqlparams.ToArray());

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];


                /* #region Filter Zero Results

                 if (filterZeroResults && !ReportType.Equals("TrialBalanceReport"))
                 {
                     DataTable dt_Filtered = dtToReturn.Clone();
                     List<string> lst_AccountCodes = dtToReturn.AsEnumerable()
                         .Select(x => x.Field<string>("CodeCombinationID")).ToList();

                     foreach (string codeCombinationID in lst_AccountCodes)
                     {
                         decimal Openingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                           .Sum(x => x.Field<decimal>("OpeningDebit"));
                         decimal Openingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                         .Sum(x => x.Field<decimal>("OpeningCredit"));

                         decimal _Openingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                         .Sum(x => x.Field<decimal>("_OpeningDebit"));
                         decimal _Openingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                         .Sum(x => x.Field<decimal>("_OpeningCredit"));

                         decimal Closingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                           .Sum(x => x.Field<decimal>("ClosingDebit"));
                         decimal Closingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                          .Sum(x => x.Field<decimal>("ClosingCredit"));

                         decimal _Closingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                           .Sum(x => x.Field<decimal>("_ClosingDebit"));
                         decimal _Closingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                          .Sum(x => x.Field<decimal>("_ClosingCredit"));
                        
                         decimal DebitAmount = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                             .Sum(x => x.Field<decimal>("Debit"));

                         decimal CreditAmount = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                             .Sum(x => x.Field<decimal>("Credit"));


                         if (Math.Abs(Openingdebit) > 0 || Math.Abs(Openingcredit) > 0 || Math.Abs(_Openingdebit) > 0 || Math.Abs(_Openingcredit) > 0 || Math.Abs(Closingdebit) > 0 || Math.Abs(Closingcredit) > 0 || Math.Abs(_Closingdebit) > 0 || Math.Abs(_Closingcredit) > 0 || Math.Abs(DebitAmount) > 0 || Math.Abs(CreditAmount) > 0)
                         {
                             DataRow[] drs_ToImport = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                                 .ToArray();

                             foreach (DataRow dr_ToImport in drs_ToImport)
                                 dt_Filtered.ImportRow(dr_ToImport);

                             dt_Filtered.AcceptChanges();
                         }
                        
                     }

                     dtToReturn = dt_Filtered.Copy();
                 }

                 #endregion*/




            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataTable GetTrialBalanceData(int costcenter, int department, string companyID, int chartOfAccountID, int periodID, int fromDocumentType,
            int toDocumentType, Guid userID, DateTime? dateFrom = null, DateTime? dateTo = null, string ReportType = null, bool? chk1 = null, bool? chk2 = null,
            bool? chk3 = null, bool? chk4 = null, Nullable<short> documentGroupID = null, bool discardReversedDocuments = false,
            bool documentTypeAuthentication = false, bool filterZeroResults = false, string accountCodeFrom = null, string accountCodeTo = null, int FiscalID = 0, string documentGroupIDs = "", string AccountTypeIDs = "", bool isdepartmentwisereport = false, bool WithOutClosing = false, string multifiscalIds = "",
            string accountCodeFrom2 = null, string accountCodeTo2 = null
            )
        {
            DataTable dtToReturn = null;
            string qry_docuTypeAuthenticate = string.Empty;
            StringBuilder debitCreditFilter = new StringBuilder();
            StringBuilder additionalFilter = new StringBuilder();
            string accountCodeFilter = string.Empty;
            string WithOutClosingFilter = string.Empty;
            List<string> lst_company = companyID.Split(',').ToList();

            try
            {
                #region Query Parameters

                List<SqlParameter> sqlparams = new List<SqlParameter>();
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));
                sqlparams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlparams.Add(new SqlParameter("@ToDate", dateTo));

                if (costcenter == -1)
                    sqlparams.Add(new SqlParameter("@cc", DBNull.Value));
                else
                    sqlparams.Add(new SqlParameter("@cc", costcenter));

                if (department == -1)
                    sqlparams.Add(new SqlParameter("@did", DBNull.Value));
                else
                    sqlparams.Add(new SqlParameter("@did", department));

                #endregion

                #region Filter by Account Codes

                if (!string.IsNullOrEmpty(accountCodeFrom) && !string.IsNullOrEmpty(accountCodeTo))
                {
                    accountCodeFilter = " AND GL_CodeCombinationDetail.CodeCombinationID BETWEEN @AccountCodeFrom AND @AccountCodeTo ";
                    sqlparams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                    sqlparams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                }
                else if (!string.IsNullOrEmpty(accountCodeFrom))
                {
                    accountCodeFilter = " AND GL_CodeCombinationDetail.CodeCombinationID >= @AccountCodeFrom ";
                    sqlparams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                }
                else if (!string.IsNullOrEmpty(accountCodeTo))
                {
                    accountCodeFilter = " AND GL_CodeCombinationDetail.CodeCombinationID <= @AccountCodeTo ";
                    sqlparams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                }

                #endregion

                #region Filter by Second Account Codes

                if (!string.IsNullOrEmpty(accountCodeFrom2) && !string.IsNullOrEmpty(accountCodeTo2))
                {
                    accountCodeFilter += " AND GL_CodeCombinationDetail.CodeCombinationID BETWEEN @AccountCodeFrom2 AND @AccountCodeTo2 ";
                    sqlparams.Add(new SqlParameter("@AccountCodeFrom2", accountCodeFrom2));
                    sqlparams.Add(new SqlParameter("@AccountCodeTo2", accountCodeTo2));
                }
                else if (!string.IsNullOrEmpty(accountCodeFrom2))
                {
                    accountCodeFilter += " AND GL_CodeCombinationDetail.CodeCombinationID >= @AccountCodeFrom2 ";
                    sqlparams.Add(new SqlParameter("@AccountCodeFrom2", accountCodeFrom2));
                }
                else if (!string.IsNullOrEmpty(accountCodeTo2))
                {
                    accountCodeFilter += " AND GL_CodeCombinationDetail.CodeCombinationID <= @AccountCodeTo2 ";
                    sqlparams.Add(new SqlParameter("@AccountCodeTo2", accountCodeTo2));
                }

                #endregion

                if (ReportType.Equals("TrialBalanceReport"))
                    sqlparams.Add(new SqlParameter("@DetailPostingAllowedFlag", DBNull.Value));
                else if (ReportType.Equals("DetailTrialBalanceReport"))
                {
                    #region

                    sqlparams.Add(new SqlParameter("@DetailPostingAllowedFlag", DBNull.Value));

                    if (chk1.Value)
                        debitCreditFilter.AppendLine(" having SUM(Debit) <> 0 and SUM(credit) <> 0 ");

                    if (chk2.Value)
                        debitCreditFilter.AppendLine("having SUM(Debit)  = 0 and SUM(credit) = 0");

                    if (chk3.Value)
                    {
                        debitCreditFilter.AppendLine("having (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) > 0 THEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END) -  (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) < 0 THEN ABS((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END)=0 ");
                    }

                    if (chk4.Value)
                    {
                        debitCreditFilter.AppendLine(" having (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) > 0 THEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END) -  (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) < 0 THEN ABS((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END) <> 0 ");
                    }

                    #endregion
                }
                else if (ReportType.Equals("DetailTrialBalanceReportNetOFFUnitWise") || ReportType.Equals("DetailTrialBalanceReportNetOFF") || ReportType.Equals("TrialBalanceReportUnitWise") || ReportType.Equals("ClosingTrialBalanceReport") || ReportType.Equals("DetailTrialBalanceReport_Department") || ReportType.Equals("DetailTrialBalanceReportNetOFFUnitGroupWise") || ReportType.Equals("DetailTrialBalanceReportGroupWise"))
                {
                    #region

                    sqlparams.Add(new SqlParameter("@DetailPostingAllowedFlag", DBNull.Value));

                    if (chk1 == true)
                        debitCreditFilter.AppendLine(" having SUM(Debit) <> 0 and SUM(credit) <> 0 ");

                    if (chk2 == true)
                        debitCreditFilter.AppendLine("having SUM(Debit)  = 0 and SUM(credit) = 0");

                    if (chk3 == true)
                    {
                        debitCreditFilter.AppendLine("having (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) > 0 THEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END) -  (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) < 0 THEN ABS((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END)=0 ");
                    }

                    if (chk4 == true)
                    {
                        debitCreditFilter.AppendLine(" having (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) > 0 THEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END) -  (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) < 0 THEN ABS((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END) <> 0 ");
                    }

                    #endregion
                }
                else if (ReportType.Equals("LedgerWiseTrialBalanceReport"))
                {
                    sqlparams.Add(new SqlParameter("@DetailPostingAllowedFlag", true));
                }

                if (documentGroupID.HasValue)
                    additionalFilter.Append(" AND Set_DocumentType.GroupID = " + documentGroupID);

                if (fromDocumentType != -1 && toDocumentType != -1)
                    additionalFilter.Append(" AND GL_TransactionHeader.DocumentTypeID between " + fromDocumentType + " AND " + toDocumentType + " ");


                if (documentGroupIDs.ToString() != string.Empty)
                    additionalFilter.Append(" AND Set_DocumentType.GroupID in " + documentGroupIDs.ToString());

                if (AccountTypeIDs.ToString() != string.Empty)
                    additionalFilter.Append(" AND GL_CodeCombinationDetail.AccountTypeID in " + AccountTypeIDs.ToString());



                if (discardReversedDocuments)
                    additionalFilter.Append(" AND IsNull(GL_TransactionHeader.IsReversed, 0) = 0 And GL_TransactionHeader.ReversalID is null ");

                if (documentTypeAuthentication)
                {
                    sqlparams.Add(new SqlParameter("@UserID", userID));
                    //                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
                    //AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                sqlparams.Add(new SqlParameter("@FiscalID", multifiscalIds));


                if (WithOutClosing)
                {
                    WithOutClosingFilter = @" AND GL_TransactionHeader.TransactionID not in (
		
		select GL.TransactionID  
		from GL_TransactionHeader GL 
		inner join Set_DocumentType Doctype on Doctype.DocumentTypeID = GL.DocumentTypeID
		and Doctype.CompanyID = GL.CompanyID

		inner join SET_PeriodCustom on SET_PeriodCustom.CompanyID = GL .CompanyID
		and SET_PeriodCustom.FiscalId IN @FiscalID

		where GL.CompanyID = GL_TransactionHeader.CompanyID
		and GL.PeriodID = GL_TransactionHeader.PeriodID
		and GL.FiscalID  IN @FiscalID
		and Doctype.DocumentType = 'GL-OPENING'
		and GL.DocumentDate = SET_PeriodCustom.EndDate
		) ";
                }

                companyID = "(" + companyID + ")";
                multifiscalIds = "(" + multifiscalIds + ")";
                string Query_TrailBalance = FnI.QueryResourceLibrary.GL.GL_CodeCombinationDetail.QueryTrailbalanceNetOFF.ToString();
                if (isdepartmentwisereport == true)
                    Query_TrailBalance = FnI.QueryResourceLibrary.GL.GL_CodeCombinationDetail.Query_TrialBalanceDepartmentWise.ToString();

                if (ReportType.Equals("DetailTrialBalanceReportNetOFFUnitWise") || ReportType.Equals("DetailTrialBalanceReportGroupWise") || ReportType.Equals("TrialBalanceReportUnitWise"))
                    Query_TrailBalance = FnI.QueryResourceLibrary.GL.GL_CodeCombinationDetail.QueryTrailbalanceNetOFF_UNitWise.ToString();


                string query = Query_TrailBalance.Replace("{AccountCodeFilter}", accountCodeFilter);
                query = query.Replace("{WithoutClosing}", WithOutClosingFilter);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query.ToString().Replace("@CompanyID", companyID).Replace("@FiscalID", multifiscalIds), debitCreditFilter.ToString(), additionalFilter.ToString(),
                    qry_docuTypeAuthenticate), sqlparams.ToArray());

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];

                //|| ReportType.Equals("DetailTrialBalanceReportNetOFF") 
                if (ReportType.Equals("DetailTrialBalanceReportNetOFFUnitWise") || ReportType.Equals("TrialBalanceReportUnitWise") || ReportType.Equals("DetailTrialBalanceReportGroupWise"))
                {
                    #region Filter Zero Results

                    if (filterZeroResults)
                    {
                        DataTable dt_Filtered = dtToReturn.Clone();
                        foreach (string CompanyID in lst_company)
                        {


                            List<string> lst_AccountCodes = dtToReturn.AsEnumerable().Where(x => x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                .Select(x => x.Field<string>("CodeCombinationID")).ToList();

                            foreach (string codeCombinationID in lst_AccountCodes)
                            {
                                decimal Openingdebit = Math.Round(dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                  .Sum(x => x.Field<decimal>("OpeningDebit")));

                                decimal Openingcredit = Math.Round(dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                .Sum(x => x.Field<decimal>("OpeningCredit")));

                                decimal _Openingdebit = Math.Round(dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                .Sum(x => x.Field<decimal>("_OpeningDebit")));

                                decimal _Openingcredit = Math.Round(dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                .Sum(x => x.Field<decimal>("_OpeningCredit")));

                                decimal Closingdebit = Math.Round(dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                  .Sum(x => x.Field<decimal>("ClosingDebit")));

                                decimal Closingcredit = Math.Round(dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                 .Sum(x => x.Field<decimal>("ClosingCredit")));

                                decimal _Closingdebit = Math.Round(dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                  .Sum(x => x.Field<decimal>("_ClosingDebit")));

                                decimal _Closingcredit = Math.Round(dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                 .Sum(x => x.Field<decimal>("_ClosingCredit")));

                                decimal DebitAmount = Math.Round(dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                    .Sum(x => x.Field<decimal>("Debit")));

                                decimal CreditAmount = Math.Round(dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                    .Sum(x => x.Field<decimal>("Credit")));

                                int SummaryFlag = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero() && x.Field<bool>("SummaryFlag") == true)
                                .Sum(x => x.Field<int>("SegmentLength"));


                                if (Math.Abs(Openingdebit) > 0 || Math.Abs(Openingcredit) > 0 || Math.Abs(Closingdebit) > 0 || Math.Abs(Closingcredit) > 0 || Math.Abs(DebitAmount) > 0 || Math.Abs(CreditAmount) > 0)
                                {
                                    DataRow[] drs_ToImport = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                            .ToArray();

                                    foreach (DataRow dr_ToImport in drs_ToImport)
                                        dt_Filtered.ImportRow(dr_ToImport);

                                    dt_Filtered.AcceptChanges();
                                }
                                else
                                {
                                    if (SummaryFlag > 0)
                                    {
                                        //if (dt_Filtered.Select("CodeCombinationID = ' " + codeCombinationID + "'").Length == 0)
                                        //{
                                        DataRow[] drs_ToImport = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                        .ToArray();

                                        foreach (DataRow dr_ToImport in drs_ToImport)
                                            dt_Filtered.ImportRow(dr_ToImport);

                                        dt_Filtered.AcceptChanges();
                                        //}

                                    }
                                }


                            }
                        }
                        dtToReturn = dt_Filtered.Copy();

                    }

                    #endregion
                }
                else if (ReportType.Equals("DetailTrialBalanceReportNetOFF") || ReportType.Equals("TrialBalanceReport"))
                {
                    #region Filter Zero Results

                    if (filterZeroResults)
                    {
                        DataTable dt_Filtered = dtToReturn.Clone();
                        List<string> lst_AccountCodes = dtToReturn.AsEnumerable()
                            .Select(x => x.Field<string>("CodeCombinationID")).ToList();

                        foreach (string codeCombinationID in lst_AccountCodes)
                        {
                            decimal Openingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                              .Sum(x => x.Field<decimal>("OpeningDebit"));
                            decimal Openingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                            .Sum(x => x.Field<decimal>("OpeningCredit"));

                            decimal _Openingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                            .Sum(x => x.Field<decimal>("_OpeningDebit"));
                            decimal _Openingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                            .Sum(x => x.Field<decimal>("_OpeningCredit"));

                            decimal Closingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                              .Sum(x => x.Field<decimal>("ClosingDebit"));
                            decimal Closingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                             .Sum(x => x.Field<decimal>("ClosingCredit"));

                            decimal _Closingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                              .Sum(x => x.Field<decimal>("_ClosingDebit"));
                            decimal _Closingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                             .Sum(x => x.Field<decimal>("_ClosingCredit"));

                            decimal DebitAmount = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                                .Sum(x => x.Field<decimal>("Debit"));

                            decimal CreditAmount = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                                .Sum(x => x.Field<decimal>("Credit"));

                            int SummaryFlag = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<bool>("SummaryFlag") == true)
                            .Sum(x => x.Field<int>("SegmentLength"));


                            if (Math.Abs(Openingdebit) > 0 || Math.Abs(Openingcredit) > 0 || Math.Abs(_Openingdebit) > 0 || Math.Abs(_Openingcredit) > 0 || Math.Abs(Closingdebit) > 0 || Math.Abs(Closingcredit) > 0 || Math.Abs(_Closingdebit) > 0 || Math.Abs(_Closingcredit) > 0 || Math.Abs(DebitAmount) > 0 || Math.Abs(CreditAmount) > 0)
                            {
                                DataRow[] drs_ToImport = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                                    .ToArray();

                                foreach (DataRow dr_ToImport in drs_ToImport)
                                    dt_Filtered.ImportRow(dr_ToImport);

                                dt_Filtered.AcceptChanges();
                            }
                            else
                            {

                                if (SummaryFlag > 0)
                                {

                                    DataRow[] drs_ToImport = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                                    .ToArray();

                                    foreach (DataRow dr_ToImport in drs_ToImport)
                                        dt_Filtered.ImportRow(dr_ToImport);

                                    dt_Filtered.AcceptChanges();


                                }

                            }


                        }

                        dtToReturn = dt_Filtered.Copy();
                    }

                    #endregion
                }
                else
                {
                    #region Filter Zero Results

                    if (filterZeroResults)
                    {
                        DataTable dt_Filtered = dtToReturn.Clone();

                        foreach (string CompanyID in lst_company)
                        {

                            List<string> lst_AccountCodes = dtToReturn.AsEnumerable().Where(x => x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                .Select(x => x.Field<string>("CodeCombinationID")).ToList();

                            foreach (string codeCombinationID in lst_AccountCodes)
                            {
                                decimal Openingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                  .Sum(x => x.Field<decimal>("OpeningDebit"));
                                decimal Openingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                .Sum(x => x.Field<decimal>("OpeningCredit"));

                                decimal _Openingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                .Sum(x => x.Field<decimal>("_OpeningDebit"));
                                decimal _Openingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                .Sum(x => x.Field<decimal>("_OpeningCredit"));

                                decimal Closingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                  .Sum(x => x.Field<decimal>("ClosingDebit"));
                                decimal Closingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                 .Sum(x => x.Field<decimal>("ClosingCredit"));

                                decimal _Closingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                  .Sum(x => x.Field<decimal>("_ClosingDebit"));
                                decimal _Closingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                 .Sum(x => x.Field<decimal>("_ClosingCredit"));

                                decimal DebitAmount = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                    .Sum(x => x.Field<decimal>("Debit"));

                                decimal CreditAmount = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                    .Sum(x => x.Field<decimal>("Credit"));

                                int SummaryFlag = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero() && x.Field<bool>("SummaryFlag") == true)
                                .Sum(x => x.Field<int>("SegmentLength"));


                                if (Math.Abs(Openingdebit) > 0 || Math.Abs(Openingcredit) > 0 || Math.Abs(_Openingdebit) > 0 || Math.Abs(_Openingcredit) > 0 || Math.Abs(Closingdebit) > 0 || Math.Abs(Closingcredit) > 0 || Math.Abs(_Closingdebit) > 0 || Math.Abs(_Closingcredit) > 0 || Math.Abs(DebitAmount) > 0 || Math.Abs(CreditAmount) > 0)
                                {
                                    DataRow[] drs_ToImport = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                        .ToArray();

                                    foreach (DataRow dr_ToImport in drs_ToImport)
                                        dt_Filtered.ImportRow(dr_ToImport);

                                    dt_Filtered.AcceptChanges();
                                }
                                else
                                {
                                    if (SummaryFlag > 0)
                                    {

                                        DataRow[] drs_ToImport = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                        .ToArray();

                                        foreach (DataRow dr_ToImport in drs_ToImport)
                                            dt_Filtered.ImportRow(dr_ToImport);

                                        dt_Filtered.AcceptChanges();


                                    }
                                }


                            }
                        }

                        dtToReturn = dt_Filtered.Copy();
                    }

                    #endregion
                }



            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }


        public static DataTable GetTrialBalanceData_HSM(int costcenter, int department, string companyID, int chartOfAccountID, int periodID, int fromDocumentType,
            int toDocumentType, Guid userID, DateTime? dateFrom = null, DateTime? dateTo = null, string ReportType = null, bool? chk1 = null, bool? chk2 = null,
            bool? chk3 = null, bool? chk4 = null, Nullable<short> documentGroupID = null, bool discardReversedDocuments = false,
            bool documentTypeAuthentication = false, bool filterZeroResults = false, string accountCodeFrom = null, string accountCodeTo = null, int FiscalID = 0, string documentGroupIDs = "", string AccountTypeIDs = "", bool isdepartmentwisereport = false, bool WithOutClosing = false, string multifiscalIds = "",
            string accountCodeFrom2 = null, string accountCodeTo2 = null
            )
        {
            DataTable dtToReturn = null;
            string qry_docuTypeAuthenticate = string.Empty;
            StringBuilder debitCreditFilter = new StringBuilder();
            StringBuilder additionalFilter = new StringBuilder();
            string accountCodeFilter = string.Empty;
            string WithOutClosingFilter = string.Empty;
            List<string> lst_company = companyID.Split(',').ToList();

            try
            {
                #region Query Parameters

                List<SqlParameter> sqlparams = new List<SqlParameter>();
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));
                sqlparams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlparams.Add(new SqlParameter("@ToDate", dateTo));

                if (costcenter == -1)
                    sqlparams.Add(new SqlParameter("@cc", DBNull.Value));
                else
                    sqlparams.Add(new SqlParameter("@cc", costcenter));

                if (department == -1)
                    sqlparams.Add(new SqlParameter("@did", DBNull.Value));
                else
                    sqlparams.Add(new SqlParameter("@did", department));

                #endregion

                #region Filter by Account Codes

                if (!string.IsNullOrEmpty(accountCodeFrom) && !string.IsNullOrEmpty(accountCodeTo))
                {
                    accountCodeFilter = " AND GL_CodeCombinationDetail.CodeCombinationID BETWEEN @AccountCodeFrom AND @AccountCodeTo ";
                    sqlparams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                    sqlparams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                }
                else if (!string.IsNullOrEmpty(accountCodeFrom))
                {
                    accountCodeFilter = " AND GL_CodeCombinationDetail.CodeCombinationID >= @AccountCodeFrom ";
                    sqlparams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                }
                else if (!string.IsNullOrEmpty(accountCodeTo))
                {
                    accountCodeFilter = " AND GL_CodeCombinationDetail.CodeCombinationID <= @AccountCodeTo ";
                    sqlparams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                }

                #endregion

                #region Filter by Second Account Codes

                if (!string.IsNullOrEmpty(accountCodeFrom2) && !string.IsNullOrEmpty(accountCodeTo2))
                {
                    accountCodeFilter += " AND GL_CodeCombinationDetail.CodeCombinationID BETWEEN @AccountCodeFrom2 AND @AccountCodeTo2 ";
                    sqlparams.Add(new SqlParameter("@AccountCodeFrom2", accountCodeFrom2));
                    sqlparams.Add(new SqlParameter("@AccountCodeTo2", accountCodeTo2));
                }
                else if (!string.IsNullOrEmpty(accountCodeFrom2))
                {
                    accountCodeFilter += " AND GL_CodeCombinationDetail.CodeCombinationID >= @AccountCodeFrom2 ";
                    sqlparams.Add(new SqlParameter("@AccountCodeFrom2", accountCodeFrom2));
                }
                else if (!string.IsNullOrEmpty(accountCodeTo2))
                {
                    accountCodeFilter += " AND GL_CodeCombinationDetail.CodeCombinationID <= @AccountCodeTo2 ";
                    sqlparams.Add(new SqlParameter("@AccountCodeTo2", accountCodeTo2));
                }

                #endregion

                if (ReportType.Equals("TrialBalanceReport"))
                    sqlparams.Add(new SqlParameter("@DetailPostingAllowedFlag", DBNull.Value));
                else if (ReportType.Equals("DetailTrialBalanceReport"))
                {
                    #region

                    sqlparams.Add(new SqlParameter("@DetailPostingAllowedFlag", DBNull.Value));

                    if (chk1.Value)
                        debitCreditFilter.AppendLine(" having SUM(Debit) <> 0 and SUM(credit) <> 0 ");

                    if (chk2.Value)
                        debitCreditFilter.AppendLine("having SUM(Debit)  = 0 and SUM(credit) = 0");

                    if (chk3.Value)
                    {
                        debitCreditFilter.AppendLine("having (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) > 0 THEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END) -  (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) < 0 THEN ABS((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END)=0 ");
                    }

                    if (chk4.Value)
                    {
                        debitCreditFilter.AppendLine(" having (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) > 0 THEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END) -  (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) < 0 THEN ABS((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END) <> 0 ");
                    }

                    #endregion
                }
                else if (ReportType.Equals("DetailTrialBalanceReportNetOFFUnitWise") || ReportType.Equals("DetailTrialBalanceReportNetOFF") || ReportType.Equals("TrialBalanceReportUnitWise") || ReportType.Equals("ClosingTrialBalanceReport") || ReportType.Equals("DetailTrialBalanceReport_Department") || ReportType.Equals("DetailTrialBalanceReportNetOFFUnitGroupWise") || ReportType.Equals("DetailTrialBalanceReportGroupWise"))
                {
                    #region

                    sqlparams.Add(new SqlParameter("@DetailPostingAllowedFlag", DBNull.Value));

                    if (chk1 == true)
                        debitCreditFilter.AppendLine(" having SUM(Debit) <> 0 and SUM(credit) <> 0 ");

                    if (chk2 == true)
                        debitCreditFilter.AppendLine("having SUM(Debit)  = 0 and SUM(credit) = 0");

                    if (chk3 == true)
                    {
                        debitCreditFilter.AppendLine("having (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) > 0 THEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END) -  (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) < 0 THEN ABS((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END)=0 ");
                    }

                    if (chk4 == true)
                    {
                        debitCreditFilter.AppendLine(" having (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) > 0 THEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END) -  (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) < 0 THEN ABS((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END) <> 0 ");
                    }

                    #endregion
                }
                else if (ReportType.Equals("LedgerWiseTrialBalanceReport"))
                {
                    sqlparams.Add(new SqlParameter("@DetailPostingAllowedFlag", true));
                }

                if (documentGroupID.HasValue)
                    additionalFilter.Append(" AND Set_DocumentType.GroupID = " + documentGroupID);

                if (fromDocumentType != -1 && toDocumentType != -1)
                    additionalFilter.Append(" AND GL_TransactionHeader.DocumentTypeID between " + fromDocumentType + " AND " + toDocumentType + " ");


                if (documentGroupIDs.ToString() != string.Empty)
                    additionalFilter.Append(" AND Set_DocumentType.GroupID in " + documentGroupIDs.ToString());

                if (AccountTypeIDs.ToString() != string.Empty)
                    additionalFilter.Append(" AND GL_CodeCombinationDetail.AccountTypeID in " + AccountTypeIDs.ToString());



                if (discardReversedDocuments)
                    additionalFilter.Append(" AND IsNull(GL_TransactionHeader.IsReversed, 0) = 0 And GL_TransactionHeader.ReversalID is null ");

                if (documentTypeAuthentication)
                {
                    sqlparams.Add(new SqlParameter("@UserID", userID));
                    //                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
                    //AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                sqlparams.Add(new SqlParameter("@FiscalID", multifiscalIds));


                if (WithOutClosing)
                {
                    WithOutClosingFilter = @" AND GL_TransactionHeader.TransactionID not in (
		
		select GL.TransactionID  
		from GL_TransactionHeader GL 
		inner join Set_DocumentType Doctype on Doctype.DocumentTypeID = GL.DocumentTypeID
		and Doctype.CompanyID = GL.CompanyID

		inner join SET_PeriodCustom on SET_PeriodCustom.CompanyID = GL .CompanyID
		and SET_PeriodCustom.FiscalId IN @FiscalID

		where GL.CompanyID = GL_TransactionHeader.CompanyID
		and GL.PeriodID = GL_TransactionHeader.PeriodID
		and GL.FiscalID  IN @FiscalID
		and Doctype.DocumentType = 'GL-OPENING'
		and GL.DocumentDate = SET_PeriodCustom.EndDate
		) ";
                }

                companyID = "(" + companyID + ")";
                multifiscalIds = "(" + multifiscalIds + ")";
                string Query_TrailBalance = FnI.QueryResourceLibrary.GL.GL_CodeCombinationDetail.QueryTrailbalanceNetOFF_HSM.ToString();
                if (isdepartmentwisereport == true)
                    Query_TrailBalance = FnI.QueryResourceLibrary.GL.GL_CodeCombinationDetail.Query_TrialBalanceDepartmentWise_HSM.ToString();

                if (ReportType.Equals("DetailTrialBalanceReportNetOFFUnitWise") || ReportType.Equals("DetailTrialBalanceReportGroupWise") || ReportType.Equals("TrialBalanceReportUnitWise"))
                    Query_TrailBalance = FnI.QueryResourceLibrary.GL.GL_CodeCombinationDetail.QueryTrailbalanceNetOFF_UNitWise_HSM.ToString();


                string query = Query_TrailBalance.Replace("{AccountCodeFilter}", accountCodeFilter);
                query = query.Replace("{WithoutClosing}", WithOutClosingFilter);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query.ToString().Replace("@CompanyID", companyID).Replace("@FiscalID", multifiscalIds), debitCreditFilter.ToString(), additionalFilter.ToString(),
                    qry_docuTypeAuthenticate), sqlparams.ToArray());

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];

                //|| ReportType.Equals("DetailTrialBalanceReportNetOFF") 
                if (ReportType.Equals("DetailTrialBalanceReportNetOFFUnitWise") || ReportType.Equals("TrialBalanceReportUnitWise") || ReportType.Equals("DetailTrialBalanceReportGroupWise"))
                {
                    #region Filter Zero Results

                    if (filterZeroResults)
                    {
                        DataTable dt_Filtered = dtToReturn.Clone();
                        foreach (string CompanyID in lst_company)
                        {


                            List<string> lst_AccountCodes = dtToReturn.AsEnumerable().Where(x => x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                .Select(x => x.Field<string>("CodeCombinationID")).ToList();

                            foreach (string codeCombinationID in lst_AccountCodes)
                            {
                                decimal Openingdebit = Math.Round(dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                  .Sum(x => x.Field<decimal>("OpeningDebit")));

                                decimal Openingcredit = Math.Round(dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                .Sum(x => x.Field<decimal>("OpeningCredit")));

                                decimal _Openingdebit = Math.Round(dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                .Sum(x => x.Field<decimal>("_OpeningDebit")));

                                decimal _Openingcredit = Math.Round(dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                .Sum(x => x.Field<decimal>("_OpeningCredit")));

                                decimal Closingdebit = Math.Round(dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                  .Sum(x => x.Field<decimal>("ClosingDebit")));

                                decimal Closingcredit = Math.Round(dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                 .Sum(x => x.Field<decimal>("ClosingCredit")));

                                decimal _Closingdebit = Math.Round(dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                  .Sum(x => x.Field<decimal>("_ClosingDebit")));

                                decimal _Closingcredit = Math.Round(dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                 .Sum(x => x.Field<decimal>("_ClosingCredit")));

                                decimal DebitAmount = Math.Round(dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                    .Sum(x => x.Field<decimal>("Debit")));

                                decimal CreditAmount = Math.Round(dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                    .Sum(x => x.Field<decimal>("Credit")));

                                int SummaryFlag = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero() && x.Field<bool>("SummaryFlag") == true)
                                .Sum(x => x.Field<int>("SegmentLength"));


                                if (Math.Abs(Openingdebit) > 0 || Math.Abs(Openingcredit) > 0 || Math.Abs(Closingdebit) > 0 || Math.Abs(Closingcredit) > 0 || Math.Abs(DebitAmount) > 0 || Math.Abs(CreditAmount) > 0)
                                {
                                    DataRow[] drs_ToImport = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                            .ToArray();

                                    foreach (DataRow dr_ToImport in drs_ToImport)
                                        dt_Filtered.ImportRow(dr_ToImport);

                                    dt_Filtered.AcceptChanges();
                                }
                                else
                                {
                                    //if (SummaryFlag > 0)
                                    //{
                                    //    //if (dt_Filtered.Select("CodeCombinationID = ' " + codeCombinationID + "'").Length == 0)
                                    //    //{
                                    //    DataRow[] drs_ToImport = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                    //    .ToArray();

                                    //    foreach (DataRow dr_ToImport in drs_ToImport)
                                    //        dt_Filtered.ImportRow(dr_ToImport);

                                    //    dt_Filtered.AcceptChanges();
                                    //    //}

                                    //}
                                }


                            }
                        }
                        dtToReturn = dt_Filtered.Copy();

                    }

                    #endregion
                }
                else if (ReportType.Equals("DetailTrialBalanceReportNetOFF"))
                {
                    #region Filter Zero Results

                    if (filterZeroResults)
                    {
                        DataTable dt_Filtered = dtToReturn.Clone();
                        List<string> lst_AccountCodes = dtToReturn.AsEnumerable()
                            .Select(x => x.Field<string>("CodeCombinationID")).ToList();

                        foreach (string codeCombinationID in lst_AccountCodes)
                        {
                            decimal Openingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                              .Sum(x => x.Field<decimal>("OpeningDebit"));
                            decimal Openingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                            .Sum(x => x.Field<decimal>("OpeningCredit"));

                            decimal _Openingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                            .Sum(x => x.Field<decimal>("_OpeningDebit"));
                            decimal _Openingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                            .Sum(x => x.Field<decimal>("_OpeningCredit"));

                            decimal Closingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                              .Sum(x => x.Field<decimal>("ClosingDebit"));
                            decimal Closingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                             .Sum(x => x.Field<decimal>("ClosingCredit"));

                            decimal _Closingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                              .Sum(x => x.Field<decimal>("_ClosingDebit"));
                            decimal _Closingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                             .Sum(x => x.Field<decimal>("_ClosingCredit"));

                            decimal DebitAmount = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                                .Sum(x => x.Field<decimal>("Debit"));

                            decimal CreditAmount = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                                .Sum(x => x.Field<decimal>("Credit"));

                            int SummaryFlag = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<bool>("SummaryFlag") == true)
                            .Sum(x => x.Field<int>("SegmentLength"));

                            if ((Math.Abs(Openingdebit) > 0 && Math.Abs(_Openingdebit) > 0) || (Math.Abs(Openingcredit) > 0 && Math.Abs(_Openingcredit) > 0) || (Math.Abs(_Closingdebit) > 0 && Math.Abs(Closingdebit) > 0) || (Math.Abs(Closingcredit) > 0 && Math.Abs(_Closingcredit) > 0) || Math.Abs(DebitAmount) > 0 || Math.Abs(CreditAmount) > 0)
                            {
                                DataRow[] drs_ToImport = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                                    .ToArray();

                                foreach (DataRow dr_ToImport in drs_ToImport)
                                    dt_Filtered.ImportRow(dr_ToImport);

                                dt_Filtered.AcceptChanges();
                            }
                            else
                            {

                            }


                        }

                        dtToReturn = dt_Filtered.Copy();
                    }

                    #endregion
                }

                else if (ReportType.Equals("TrialBalanceReport"))
                {
                    if (filterZeroResults)
                    {

                        DataTable dt_Filtered = dtToReturn.Clone();

                        List<string> lst_AccountCodes = dtToReturn.AsEnumerable()
                            .Select(x => x.Field<string>("CodeCombinationID")).Distinct().ToList();

                        // Step 1: Build parent-child map
                        var childMap = dtToReturn.AsEnumerable()
                            .GroupBy(x => x.Field<string>("HeadAccountCode"))
                            .ToDictionary(
                                g => g.Key,
                                g => g.Select(x => x.Field<string>("CodeCombinationID")).ToList()
                            );

                        foreach (string codeCombinationID in lst_AccountCodes)
                        {
                            // Inline recursive logic to get all descendants
                            List<string> allCodes = new List<string>();
                            Stack<string> stack = new Stack<string>();
                            stack.Push(codeCombinationID);

                            while (stack.Count > 0)
                            {
                                string current = stack.Pop();
                                if (!allCodes.Contains(current))
                                    allCodes.Add(current);

                                if (childMap.ContainsKey(current))
                                {
                                    foreach (var child in childMap[current])
                                    {
                                        if (!allCodes.Contains(child))
                                            stack.Push(child);
                                    }
                                }
                            }

                            var relatedRows = dtToReturn.AsEnumerable()
                                .Where(x => allCodes.Contains(x.Field<string>("CodeCombinationID")));

                            decimal Openingdebit = relatedRows.Sum(x => x.Field<decimal>("OpeningDebit"));
                            decimal Openingcredit = relatedRows.Sum(x => x.Field<decimal>("OpeningCredit"));
                            decimal _Openingdebit = relatedRows.Sum(x => x.Field<decimal>("_OpeningDebit"));
                            decimal _Openingcredit = relatedRows.Sum(x => x.Field<decimal>("_OpeningCredit"));
                            decimal Closingdebit = relatedRows.Sum(x => x.Field<decimal>("ClosingDebit"));
                            decimal Closingcredit = relatedRows.Sum(x => x.Field<decimal>("ClosingCredit"));
                            decimal _Closingdebit = relatedRows.Sum(x => x.Field<decimal>("_ClosingDebit"));
                            decimal _Closingcredit = relatedRows.Sum(x => x.Field<decimal>("_ClosingCredit"));
                            decimal DebitAmount = relatedRows.Sum(x => x.Field<decimal>("Debit"));
                            decimal CreditAmount = relatedRows.Sum(x => x.Field<decimal>("Credit"));

                            if ((Math.Abs(Openingdebit) > 0 && Math.Abs(_Openingdebit) > 0) || (Math.Abs(Openingcredit) > 0 && Math.Abs(_Openingcredit) > 0) || (Math.Abs(_Closingdebit) > 0 && Math.Abs(Closingdebit) > 0) || (Math.Abs(Closingcredit) > 0 && Math.Abs(_Closingcredit) > 0) || Math.Abs(DebitAmount) > 0 || Math.Abs(CreditAmount) > 0)
                            {
                                DataRow[] drs_ToImport = dtToReturn.AsEnumerable()
                                    .Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                                    .ToArray();

                                foreach (DataRow dr_ToImport in drs_ToImport)
                                    dt_Filtered.ImportRow(dr_ToImport);

                                dt_Filtered.AcceptChanges();
                            }
                        }

                        dtToReturn = dt_Filtered.Copy();
                    }
                }
                else
                {
                    #region Filter Zero Results

                    if (filterZeroResults)
                    {
                        DataTable dt_Filtered = dtToReturn.Clone();

                        foreach (string CompanyID in lst_company)
                        {

                            List<string> lst_AccountCodes = dtToReturn.AsEnumerable().Where(x => x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                .Select(x => x.Field<string>("CodeCombinationID")).ToList();

                            foreach (string codeCombinationID in lst_AccountCodes)
                            {
                                decimal Openingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                  .Sum(x => x.Field<decimal>("OpeningDebit"));
                                decimal Openingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                .Sum(x => x.Field<decimal>("OpeningCredit"));

                                decimal _Openingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                .Sum(x => x.Field<decimal>("_OpeningDebit"));
                                decimal _Openingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                .Sum(x => x.Field<decimal>("_OpeningCredit"));

                                decimal Closingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                  .Sum(x => x.Field<decimal>("ClosingDebit"));
                                decimal Closingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                 .Sum(x => x.Field<decimal>("ClosingCredit"));

                                decimal _Closingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                  .Sum(x => x.Field<decimal>("_ClosingDebit"));
                                decimal _Closingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                 .Sum(x => x.Field<decimal>("_ClosingCredit"));

                                decimal DebitAmount = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                    .Sum(x => x.Field<decimal>("Debit"));

                                decimal CreditAmount = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                    .Sum(x => x.Field<decimal>("Credit"));

                                int SummaryFlag = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero() && x.Field<bool>("SummaryFlag") == true)
                                .Sum(x => x.Field<int>("SegmentLength"));


                                if ((Math.Abs(Openingdebit) > 0 && Math.Abs(_Openingdebit) > 0) || (Math.Abs(Openingcredit) > 0 && Math.Abs(_Openingcredit) > 0) || (Math.Abs(_Closingdebit) > 0 && Math.Abs(Closingdebit) > 0) || (Math.Abs(Closingcredit) > 0 && Math.Abs(_Closingcredit) > 0) || Math.Abs(DebitAmount) > 0 || Math.Abs(CreditAmount) > 0)
                                {
                                    DataRow[] drs_ToImport = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                        .ToArray();

                                    foreach (DataRow dr_ToImport in drs_ToImport)
                                        dt_Filtered.ImportRow(dr_ToImport);

                                    dt_Filtered.AcceptChanges();
                                }
                                else
                                {
                                    //if (SummaryFlag > 0)
                                    //{

                                    //    DataRow[] drs_ToImport = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                    //    .ToArray();

                                    //    foreach (DataRow dr_ToImport in drs_ToImport)
                                    //        dt_Filtered.ImportRow(dr_ToImport);

                                    //    dt_Filtered.AcceptChanges();


                                    //}
                                }


                            }
                        }

                        dtToReturn = dt_Filtered.Copy();
                    }

                    #endregion
                }

                if (WithOutClosing)
                {
                    if (dtToReturn.Rows.Count > 0 && dtToReturn != null)
                    {
                        foreach (DataRow row in dtToReturn.Rows)
                        {
                            row["ClosingDebit"] = 0.00;
                            row["ClosingCredit"] = 0.00;
                            row["_ClosingDebit"] = 0.00;
                            row["_ClosingCredit"] = 0.00;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }


        public static DataTable GetTrialBalanceDataForFinancialForm(int costcenter, int department, string companyID, int chartOfAccountID, int periodID, int fromDocumentType,
            int toDocumentType, Guid userID, DateTime? datefrom = null, DateTime? dateTo = null, string ReportType = null, bool? chk1 = null, bool? chk2 = null,
            bool? chk3 = null, bool? chk4 = null, Nullable<short> documentGroupID = null, bool discardReversedDocuments = false,
            bool documentTypeAuthentication = false, bool filterZeroResults = false, string chartofAccountFrom = "", string chartofAccountTo = "", int? FinancialHeaderID = null, int? segment = null)
        {
            DataTable dtToReturn = null;
            string qry_docuTypeAuthenticate = string.Empty;
            StringBuilder debitCreditFilter = new StringBuilder();
            StringBuilder additionalFilter = new StringBuilder();

            try
            {
                #region Query Parameters

                List<SqlParameter> sqlparams = new List<SqlParameter>();
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));
                //sqlparams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlparams.Add(new SqlParameter("@FromDate", datefrom));
                sqlparams.Add(new SqlParameter("@ToDate", dateTo));
                sqlparams.Add(new SqlParameter("@FinancialHeaderID", FinancialHeaderID.Value));

                sqlparams.Add(new SqlParameter("@chartofAccountFrom", chartofAccountFrom));
                sqlparams.Add(new SqlParameter("@chartofAccountTo", chartofAccountTo));
                sqlparams.Add(new SqlParameter("@UserID", userID));

                #endregion
                companyID = "(" + companyID + ")";

                string queryDetail = FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_FinancialDetail;
                string codefromfilter = string.Empty;

                if (chartofAccountFrom != "" && chartofAccountTo != "")// && !segment.HasValue)
                    codefromfilter = "and GL_CodeCombinationDetail.CodeCombinationID between @chartofAccountFrom and @chartofAccountTo ";
                else if (chartofAccountFrom != "")// && chartofAccountTo != "" && segment.HasValue)
                    codefromfilter = "and GL_CodeCombinationDetail.CodeCombinationID like '" + chartofAccountFrom + "%'";

                if (documentGroupID.HasValue)
                    additionalFilter.Append(" AND Set_DocumentType.GroupID = " + documentGroupID);

                string query = string.Format(queryDetail, codefromfilter, additionalFilter.ToString()).Replace("@CompanyID", companyID);





                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query), sqlparams.ToArray());

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];


                #region Filter Zero Results

                if (filterZeroResults)
                {
                    DataTable dt_Filtered = dtToReturn.Clone();
                    List<string> lst_AccountCodes = dtToReturn.AsEnumerable()
                        // .Where(x => x.Field<string>("CodeCombinationID").Split(delimeter).Length == noOfSegments)
                        .Select(x => x.Field<string>("CodeCombinationID")).ToList();

                    foreach (string codeCombinationID in lst_AccountCodes)
                    {



                        decimal Openingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                          .Sum(x => x.Field<decimal>("OpeningDebit"));
                        decimal Openingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                        .Sum(x => x.Field<decimal>("OpeningCredit"));

                        decimal _Openingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                        .Sum(x => x.Field<decimal>("_OpeningDebit"));
                        decimal _Openingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                        .Sum(x => x.Field<decimal>("_OpeningCredit"));

                        decimal Closingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                          .Sum(x => x.Field<decimal>("ClosingDebit"));
                        decimal Closingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                         .Sum(x => x.Field<decimal>("ClosingCredit"));

                        decimal _Closingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                          .Sum(x => x.Field<decimal>("_ClosingDebit"));
                        decimal _Closingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                         .Sum(x => x.Field<decimal>("_ClosingCredit"));
                        //if (debitAmount > 0 || creditAmount > 0)

                        decimal DebitAmount = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                            .Sum(x => x.Field<decimal>("Debit"));

                        decimal CreditAmount = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                            .Sum(x => x.Field<decimal>("Credit"));

                        //decimal ActivityBal = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").StartsWith(codeCombinationID))
                        //    .Sum(x => x.Field<decimal>("ActivityBalance"));

                        //decimal yearToDateBal = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").StartsWith(codeCombinationID))
                        //    .Sum(x => x.Field<decimal>("yearTodateBalance"));


                        if (Math.Abs(Openingdebit) > 0 || Math.Abs(Openingcredit) > 0 || Math.Abs(_Openingdebit) > 0 || Math.Abs(_Openingcredit) > 0 || Math.Abs(Closingdebit) > 0 || Math.Abs(Closingcredit) > 0 || Math.Abs(_Closingdebit) > 0 || Math.Abs(_Closingcredit) > 0 || Math.Abs(DebitAmount) > 0 || Math.Abs(CreditAmount) > 0)
                        {
                            DataRow[] drs_ToImport = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                                .ToArray();

                            foreach (DataRow dr_ToImport in drs_ToImport)
                                dt_Filtered.ImportRow(dr_ToImport);

                            dt_Filtered.AcceptChanges();
                        }
                    }

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


        public static DataTable GetTrialBalanceDataForFinancialBusiness(int costcenter, int department, string companyID, int chartOfAccountID, int periodID, int fromDocumentType,
          int toDocumentType, Guid userID, DateTime? datefrom = null, DateTime? dateTo = null, string ReportType = null, bool? chk1 = null, bool? chk2 = null,
          bool? chk3 = null, bool? chk4 = null, Nullable<short> documentGroupID = null, bool discardReversedDocuments = false,
          bool documentTypeAuthentication = false, bool filterZeroResults = false, string chartofAccountFrom = "", string chartofAccountTo = "", int? FinancialHeaderID = null, int? segment = null, int TypeCodeBusiness = 0)
        {
            DataTable dtToReturn = null;
            string qry_docuTypeAuthenticate = string.Empty;
            StringBuilder debitCreditFilter = new StringBuilder();
            StringBuilder additionalFilter = new StringBuilder();

            try
            {
                #region Query Parameters

                List<SqlParameter> sqlparams = new List<SqlParameter>();
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));
                sqlparams.Add(new SqlParameter("@FromDate", datefrom));
                sqlparams.Add(new SqlParameter("@ToDate", dateTo));

                sqlparams.Add(new SqlParameter("@FinancialHeaderID", FinancialHeaderID.Value));

                sqlparams.Add(new SqlParameter("@fromcode", chartofAccountFrom));
                sqlparams.Add(new SqlParameter("@tocode", chartofAccountTo));
                sqlparams.Add(new SqlParameter("@UserID", userID));
                sqlparams.Add(new SqlParameter("@TypeCode", TypeCodeBusiness));



                #endregion


                companyID = "(" + companyID + ")";

                string queryDetail = FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_FinancialDetailBusinessPosted;
                string codefromfilter = string.Empty;

                if (chartofAccountFrom != "" && chartofAccountTo != "" && !segment.HasValue)
                    codefromfilter = "and GL_CodeCombinationDetail.CodeCombinationID between @chartofAccountFrom and @chartofAccountTo ";
                else if (chartofAccountFrom != "" && chartofAccountTo != "" && segment.HasValue)
                    codefromfilter = "and GL_CodeCombinationDetail.CodeCombinationID like '" + chartofAccountFrom + "%'";

                string query = string.Format(queryDetail, codefromfilter).Replace("@CompanyID", companyID);





                //DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, debitCreditFilter.ToString(), additionalFilter.ToString(),
                //    qry_docuTypeAuthenticate), sqlparams.ToArray());

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams.ToArray());

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];


                #region Filter Zero Results

                if (filterZeroResults)
                {
                    DataTable dt_Filtered = dtToReturn.Clone();
                    List<string> lst_AccountCodes = dtToReturn.AsEnumerable()
                        // .Where(x => x.Field<string>("CodeCombinationID").Split(delimeter).Length == noOfSegments)
                        .Select(x => x.Field<string>("CodeCombinationID")).ToList();

                    foreach (string codeCombinationID in lst_AccountCodes)
                    {



                        decimal Openingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                          .Sum(x => x.Field<decimal>("OpeningDebit"));
                        decimal Openingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                        .Sum(x => x.Field<decimal>("OpeningCredit"));

                        decimal _Openingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                        .Sum(x => x.Field<decimal>("_OpeningDebit"));
                        decimal _Openingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                        .Sum(x => x.Field<decimal>("_OpeningCredit"));

                        decimal Closingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                          .Sum(x => x.Field<decimal>("ClosingDebit"));
                        decimal Closingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                         .Sum(x => x.Field<decimal>("ClosingCredit"));

                        decimal _Closingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                          .Sum(x => x.Field<decimal>("_ClosingDebit"));
                        decimal _Closingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                         .Sum(x => x.Field<decimal>("_ClosingCredit"));
                        //if (debitAmount > 0 || creditAmount > 0)

                        decimal DebitAmount = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                            .Sum(x => x.Field<decimal>("Debit"));

                        decimal CreditAmount = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                            .Sum(x => x.Field<decimal>("Credit"));

                        //decimal ActivityBal = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").StartsWith(codeCombinationID))
                        //    .Sum(x => x.Field<decimal>("ActivityBalance"));

                        //decimal yearToDateBal = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").StartsWith(codeCombinationID))
                        //    .Sum(x => x.Field<decimal>("yearTodateBalance"));


                        if (Math.Abs(Openingdebit) > 0 || Math.Abs(Openingcredit) > 0 || Math.Abs(_Openingdebit) > 0 || Math.Abs(_Openingcredit) > 0 || Math.Abs(Closingdebit) > 0 || Math.Abs(Closingcredit) > 0 || Math.Abs(_Closingdebit) > 0 || Math.Abs(_Closingcredit) > 0 || Math.Abs(DebitAmount) > 0 || Math.Abs(CreditAmount) > 0)
                        {
                            DataRow[] drs_ToImport = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                                .ToArray();

                            foreach (DataRow dr_ToImport in drs_ToImport)
                                dt_Filtered.ImportRow(dr_ToImport);

                            dt_Filtered.AcceptChanges();
                        }
                    }

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


        public static DataTable Get_SegmentWiseTrialBalance(int costcenter, int department, string companyID, int chartOfAccountID, int periodID, int fromDocumentType,
            int toDocumentType, Guid userID, DateTime? dateFrom = null, DateTime? dateTo = null, string ReportType = null, bool? chk1 = null, bool? chk2 = null,
            bool? chk3 = null, bool? chk4 = null, Nullable<short> documentGroupID = null, bool discardReversedDocuments = false,
            bool documentTypeAuthentication = false, bool filterZeroResults = false, string accountCodeFrom = null, string accountCodeTo = null, int FiscalID = 0, string documentGroupIDs = "", string AccountTypeIDs = "", bool isdepartmentwisereport = false, short noOfSegments = 2, char[] delimeter = null)
        {
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            DataTable dtToReturn = null;
            string qry_docuTypeAuthenticate = string.Empty;
            StringBuilder debitCreditFilter = new StringBuilder();
            StringBuilder additionalFilter = new StringBuilder();
            string accountCodeFilter = string.Empty;
            List<string> lst_company = companyID.Split(',').ToList();


            try
            {
                #region Query Parameters

                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));
                sqlparams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlparams.Add(new SqlParameter("@ToDate", dateTo));

                if (costcenter == -1)
                    sqlparams.Add(new SqlParameter("@cc", DBNull.Value));
                else
                    sqlparams.Add(new SqlParameter("@cc", costcenter));

                if (department == -1)
                    sqlparams.Add(new SqlParameter("@did", DBNull.Value));
                else
                    sqlparams.Add(new SqlParameter("@did", department));

                #endregion

                #region Filter by Account Codes

                if (!string.IsNullOrEmpty(accountCodeFrom) && !string.IsNullOrEmpty(accountCodeTo))
                {
                    accountCodeFilter = " AND GL_CodeCombinationDetail.CodeCombinationID BETWEEN @AccountCodeFrom AND @AccountCodeTo ";
                    sqlparams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                    sqlparams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                }
                else if (!string.IsNullOrEmpty(accountCodeFrom))
                {
                    accountCodeFilter = " AND GL_CodeCombinationDetail.CodeCombinationID >= @AccountCodeFrom ";
                    sqlparams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                }
                else if (!string.IsNullOrEmpty(accountCodeTo))
                {
                    accountCodeFilter = " AND GL_CodeCombinationDetail.CodeCombinationID <= @AccountCodeTo ";
                    sqlparams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                }

                #endregion

                sqlparams.Add(new SqlParameter("@DetailPostingAllowedFlag", DBNull.Value));

                if (documentGroupID.HasValue)
                    additionalFilter.Append(" AND Set_DocumentType.GroupID = " + documentGroupID);

                if (fromDocumentType != -1 && toDocumentType != -1)
                    additionalFilter.Append(" AND GL_TransactionHeader.DocumentTypeID between " + fromDocumentType + " AND " + toDocumentType + " ");


                if (documentGroupIDs.ToString() != string.Empty)
                    additionalFilter.Append(" AND Set_DocumentType.GroupID in " + documentGroupIDs.ToString());

                if (AccountTypeIDs.ToString() != string.Empty)
                    additionalFilter.Append(" AND GL_CodeCombinationDetail.AccountTypeID in " + AccountTypeIDs.ToString());



                if (discardReversedDocuments)
                    additionalFilter.Append(" AND IsNull(GL_TransactionHeader.IsReversed, 0) = 0 And GL_TransactionHeader.ReversalID is null ");

                if (documentTypeAuthentication)
                {
                    sqlparams.Add(new SqlParameter("@UserID", userID));
                    //                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
                    //AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                sqlparams.Add(new SqlParameter("@FiscalID", FiscalID));

                companyID = "(" + companyID + ")";


                //DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query.ToString().Replace("@CompanyID", companyID), debitCreditFilter.ToString(), additionalFilter.ToString(),
                //    qry_docuTypeAuthenticate), sqlparams.ToArray());


                string query = FnI.QueryResourceLibrary.GL.GL_CodeCombinationDetail.QueryTrailbalanceNetOFF_SegmentWise;

                query = query.Replace("{AccountCodeFilter}", accountCodeFilter);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query.ToString().Replace("@CompanyID", companyID), additionalFilter), sqlparams.ToArray());

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];

                #region Filter Zero Results

                if (filterZeroResults)
                {
                    DataTable dt_Filtered = dtToReturn.Clone();
                    foreach (string CompanyID in lst_company)
                    {


                        List<string> lst_AccountCodes = dtToReturn.AsEnumerable().Where(x => x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                            .Select(x => x.Field<string>("CodeCombinationID")).ToList();

                        foreach (string codeCombinationID in lst_AccountCodes)
                        {
                            decimal Openingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                              .Sum(x => x.Field<decimal>("OpeningDebit"));
                            decimal Openingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                            .Sum(x => x.Field<decimal>("OpeningCredit"));

                            decimal _Openingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                            .Sum(x => x.Field<decimal>("_OpeningDebit"));
                            decimal _Openingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                            .Sum(x => x.Field<decimal>("_OpeningCredit"));

                            decimal Closingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                              .Sum(x => x.Field<decimal>("ClosingDebit"));
                            decimal Closingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                             .Sum(x => x.Field<decimal>("ClosingCredit"));

                            decimal _Closingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                              .Sum(x => x.Field<decimal>("_ClosingDebit"));
                            decimal _Closingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                             .Sum(x => x.Field<decimal>("_ClosingCredit"));

                            decimal DebitAmount = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                .Sum(x => x.Field<decimal>("Debit"));

                            decimal CreditAmount = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                .Sum(x => x.Field<decimal>("Credit"));

                            int SummaryFlag = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero() && x.Field<bool>("SummaryFlag") == true)
                            .Sum(x => x.Field<int>("SegmentLength"));


                            if (Math.Abs(Openingdebit) > 0 || Math.Abs(Openingcredit) > 0 || Math.Abs(_Openingdebit) > 0 || Math.Abs(_Openingcredit) > 0 || Math.Abs(Closingdebit) > 0 || Math.Abs(Closingcredit) > 0 || Math.Abs(_Closingdebit) > 0 || Math.Abs(_Closingcredit) > 0 || Math.Abs(DebitAmount) > 0 || Math.Abs(CreditAmount) > 0)
                            {
                                DataRow[] drs_ToImport = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                    .ToArray();

                                foreach (DataRow dr_ToImport in drs_ToImport)
                                    dt_Filtered.ImportRow(dr_ToImport);

                                dt_Filtered.AcceptChanges();
                            }
                            else
                            {
                                if (SummaryFlag > 0)
                                {
                                    //if (dt_Filtered.Select("CodeCombinationID = ' " + codeCombinationID + "'").Length == 0)
                                    //{
                                    DataRow[] drs_ToImport = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID) && x.Field<int>("OperatingUnitId") == CompanyID.ToIntOrZero())
                                    .ToArray();

                                    foreach (DataRow dr_ToImport in drs_ToImport)
                                        dt_Filtered.ImportRow(dr_ToImport);

                                    dt_Filtered.AcceptChanges();
                                    //}

                                }
                            }


                        }
                    }
                    dtToReturn = dt_Filtered.Copy();

                }

                //if (filterZeroResults)
                //{
                //    DataTable dt_Filtered = dtToReturn.Clone();
                //    List<string> lst_AccountCodes = dtToReturn.AsEnumerable()
                //       .Where(x => x.Field<string>("CodeCombinationID").Split(delimeter).Length == noOfSegments)
                //        .Select(x => x.Field<string>("CodeCombinationID")).ToList();

                //    foreach (string codeCombinationID in lst_AccountCodes)
                //    {

                //        decimal Openingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").StartsWith(codeCombinationID))
                //       .Sum(x => x.Field<decimal>("OpeningDebit"));
                //        decimal Openingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").StartsWith(codeCombinationID))
                //        .Sum(x => x.Field<decimal>("OpeningCredit"));


                //        decimal Closingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").StartsWith(codeCombinationID))
                //          .Sum(x => x.Field<decimal>("ClosingDebit"));
                //        decimal Closingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").StartsWith(codeCombinationID))
                //         .Sum(x => x.Field<decimal>("ClosingCredit"));

                //        decimal debitAmount = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").StartsWith(codeCombinationID))
                //       .Sum(x => x.Field<decimal>("Debit"));

                //        decimal creditAmount = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").StartsWith(codeCombinationID))
                //            .Sum(x => x.Field<decimal>("Credit"));

                //        if (Math.Abs(Openingdebit) > 0 || Math.Abs(Openingcredit) > 0 || Math.Abs(Closingdebit) > 0 || Math.Abs(Closingcredit) > 0 || debitAmount > 0 || creditAmount > 0)
                //        {
                //            DataRow[] drs_ToImport = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").StartsWith(codeCombinationID))
                //                .ToArray();

                //            foreach (DataRow dr_ToImport in drs_ToImport)
                //                dt_Filtered.ImportRow(dr_ToImport);

                //            dt_Filtered.AcceptChanges();
                //        }
                //    }

                //    dtToReturn = dt_Filtered.Copy();
                //}

                #endregion
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataTable GetTrialBalanceDataDashBoard(int companyID, int chartOfAccountID, int periodID, int ChartOfAccountSeg, DateTime? dateFrom = null, DateTime? dateTo = null, string ReportType = null)
        {
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            DataTable dtToReturn = null;
            string str = string.Empty;

            #region Query



            #endregion

            try
            {
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));
                sqlparams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlparams.Add(new SqlParameter("@ToDate", dateTo));
                sqlparams.Add(new SqlParameter("@ChartOfAccountSEG", ChartOfAccountSeg));

                if (ReportType.Equals("TrialBalanceReport"))
                    sqlparams.Add(new SqlParameter("@DetailPostingAllowedFlag", DBNull.Value));

                else if (ReportType.Equals("DetailTrialBalanceReport"))
                    sqlparams.Add(new SqlParameter("@DetailPostingAllowedFlag", DBNull.Value));

                else if (ReportType.Equals("LedgerWiseTrialBalanceReport"))
                    sqlparams.Add(new SqlParameter("@DetailPostingAllowedFlag", true));

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_GetTrialBalanceReportForDashBoard, sqlparams.ToArray());

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

        public static DataTable GL_LogReport_data(string companyID, int periodID, string DocumentType = "", DateTime? dateFrom = null, DateTime? dateTo = null, DateTime? docdateFrom = null,
            DateTime? docdateTo = null, DateTime? updatedateFrom = null, DateTime? updatedateTo = null, string accountCodeFrom = "", string accountCodeTo = "", string ReportFile = "")
        {
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            DataTable dtToReturn = null;
            string str = string.Empty;
            string accountCodeFilter = string.Empty;
            StringBuilder additionalFilter = new StringBuilder();
            StringBuilder sb = new StringBuilder();

            #region Query

            const string Query_GL_LogReport_data = @" 

";

            #endregion

            try
            {
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));
                sqlparams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlparams.Add(new SqlParameter("@ToDate", dateTo));
                sqlparams.Add(new SqlParameter("@DocFromDate", docdateFrom));
                sqlparams.Add(new SqlParameter("@DocToDate", docdateTo));
                sqlparams.Add(new SqlParameter("@UpdateFromDate", updatedateFrom));
                sqlparams.Add(new SqlParameter("@UpdateToDate", updatedateTo));

                if (docdateFrom != null && docdateTo != null)
                    additionalFilter.Append(" AND convert(DATE ,GL_TransactionHeader.DocumentDate) BETWEEN @DocFromDate AND @DocToDate ");

                if (dateFrom != null && dateTo != null)
                    additionalFilter.Append(" AND convert(DATE ,GL_TransactionHeader.CreateDate) BETWEEN @FromDate AND @ToDate ");

                if (updatedateFrom != null && updatedateTo != null)
                    additionalFilter.Append(" AND convert(DATE ,GL_TransactionHeader.UpdateDate) BETWEEN @UpdateFromDate AND @UpdateToDate --or convert(DATE ,GL_TransactionHeader.UpdateDate) IS NULL ");

                additionalFilter.Append(" AND GL_TransactionHeader.Status = 1 and GL_TransactionDetail.Status = 1 ");

                #region Filter by Account Codes

                if (!string.IsNullOrEmpty(accountCodeFrom) && !string.IsNullOrEmpty(accountCodeTo))
                {
                    additionalFilter.Append(" AND GL_TransactionDetail.CodeCombinationID BETWEEN @AccountCodeFrom AND @AccountCodeTo ");
                    sqlparams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                    sqlparams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                }
                else if (!string.IsNullOrEmpty(accountCodeFrom))
                {
                    additionalFilter.Append(" AND GL_TransactionDetail.CodeCombinationID >= @AccountCodeFrom ");
                    sqlparams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                }
                else if (!string.IsNullOrEmpty(accountCodeTo))
                {
                    additionalFilter.Append(" AND GL_TransactionDetail.CodeCombinationID <= @AccountCodeTo ");
                    sqlparams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                }

                #endregion

                if (DocumentType != "")
                    //additionalFilter.Append(" AND GL_TransactionHeader.DocumentTypeID = " + DocumentType + " ");
                    additionalFilter.Append(" AND GL_TransactionHeader.DocumentTypeID in (" + DocumentType + ") ");
                else
                {
                    additionalFilter.Append(" AND GL_TransactionHeader.DocumentTypeID IS NULL ");
                }

                if (ReportFile == "GLLogReport_SSML")
                {
                    sb.Append("  group by GL_TransactionHeader.DocumentDate, GL_TransactionHeader.DocumentNo, GL_TransactionHeader.DocumentTypeID, SET_DocumentType.DocumentType,");
                    sb.Append("  GL_TransactionHeader.CreateDate, GL_TransactionHeader.CreatedByValue,");
                    sb.Append("  GL_TransactionHeader.UpdateDate, GL_TransactionHeader.UpdatedByValue, GL_TransactionHeader.ApprovedBy,  aspnet_Users.UserName, GL_TransactionHeader.StatusCode");
                }
                else
                {
                    sb.Append("  group by GL_TransactionHeader.DocumentDate, GL_TransactionHeader.DocumentNo, GL_TransactionHeader.DocumentTypeID, SET_DocumentType.DocumentType, ");
                    sb.Append("  GL_TransactionDetail.CodeCombinationID,  GL_TransactionHeader.CreateDate, GL_TransactionHeader.CreatedByValue, GL_TransactionDetail.TransactionType, ");
                    sb.Append("  GL_TransactionHeader.UpdateDate, GL_TransactionHeader.UpdatedByValue, GL_TransactionHeader.ApprovedBy,  aspnet_Users.UserName, GL_TransactionDetail.Narration, GL_TransactionHeader.StatusCode, GL_CodeCombinationDetail.Description,Set_OPeratingUnit.OperatingUnitName ");
                }

                string query = "";

                if (ReportFile == "GLLogReport_SSML")
                    query = FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_GL_LogReport_data_SSML.Replace("{CompanyIDs}", companyID);
                else
                    query = FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_GL_LogReport_data.Replace("{CompanyIDs}", companyID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, additionalFilter, sb), sqlparams.ToArray());

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

        public DataTable Consolidated_Trial_Balance_Report(string companyIDs, string chartOfAccountIDs, string periodIDs, DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            DataTable dtToReturn = null;

            #region Query


            #endregion

            try
            {
                sqlparams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlparams.Add(new SqlParameter("@ToDate", dateTo));

                StringBuilder finalizedQuery = new StringBuilder(FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_Consolidated_Trial_Balance_Report);
                finalizedQuery.Replace("{CompanyIDs}", companyIDs);
                finalizedQuery.Replace("{PeriodIDs}", periodIDs);
                finalizedQuery.Replace("{ChartOfAccountIDs}", chartOfAccountIDs);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, finalizedQuery.ToString(), sqlparams.ToArray());

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

        public DataTable Consolidated_Profit_And_Loss_Report(string companyIDs, string chartOfAccountIDs, string periodIDs, DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            DataTable dtToReturn = null;

            #region Query



            #endregion

            try
            {
                sqlparams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlparams.Add(new SqlParameter("@ToDate", dateTo));

                StringBuilder finalizedQuery = new StringBuilder(FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_Consolidated_Profit_And_Loss_Report);

                // Taqi --> If Company , Period , ChartOfAccount checklist is unchecked then error showing becuase of null going in these paremeters 

                if (companyIDs == "")
                {
                    finalizedQuery.Replace("{CompanyIDs}", "0");
                }
                else
                {
                    finalizedQuery.Replace("{CompanyIDs}", companyIDs);
                }
                if (periodIDs == "")
                {
                    finalizedQuery.Replace("{PeriodIDs}", "0");
                }
                else
                {
                    finalizedQuery.Replace("{PeriodIDs}", periodIDs);
                }
                if (chartOfAccountIDs == "")
                {
                    finalizedQuery.Replace("{ChartOfAccountIDs}", "0");
                }
                else
                {
                    finalizedQuery.Replace("{ChartOfAccountIDs}", chartOfAccountIDs);
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, finalizedQuery.ToString(), sqlparams.ToArray());

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

        public DataTable Consolidated_Balance_Sheet_Report(string companyIDs, string chartOfAccountIDs, string periodIDs, DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            DataTable dtToReturn = null;



            try
            {
                sqlparams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlparams.Add(new SqlParameter("@ToDate", dateTo));

                StringBuilder finalizedQuery = new StringBuilder(FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_Consolidated_Balance_Sheet_Report);
                finalizedQuery.Replace("{CompanyIDs}", companyIDs);
                finalizedQuery.Replace("{PeriodIDs}", periodIDs);
                finalizedQuery.Replace("{ChartOfAccountIDs}", chartOfAccountIDs);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, finalizedQuery.ToString(), sqlparams.ToArray());

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

        public static DataTable GetProfitAndLossDataDashBoard(int companyID, int chartOfAccountID, int periodID, int ChartOfAccountSeg, DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            StringBuilder AdditionalQueryParameters = new StringBuilder();
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            DataTable dtToReturn = null;



            try
            {
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));
                sqlparams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlparams.Add(new SqlParameter("@ToDate", dateTo));
                sqlparams.Add(new SqlParameter("@ChartOfAccountSEG", ChartOfAccountSeg));

                string str = AdditionalQueryParameters.ToString() != string.Empty ? " and " + AdditionalQueryParameters.ToString() : "";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_GETProfitAndLossReportForDashboard + str, sqlparams.ToArray());

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


        public static DataTable GetProfitAndLossData(int companyID, int chartOfAccountID, int periodID, DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            StringBuilder AdditionalQueryParameters = new StringBuilder();
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            DataTable dtToReturn = null;

            try
            {
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));
                sqlparams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlparams.Add(new SqlParameter("@ToDate", dateTo));

                string str = AdditionalQueryParameters.ToString() != string.Empty ? " and " + AdditionalQueryParameters.ToString() : "";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_GetProfitAndLossReport + str,
                    sqlparams.ToArray());

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

        public DataTable GetProfitAndLossDetailData(int companyID, int chartOfAccountID, int periodID, DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            StringBuilder AdditionalQueryParameters = new StringBuilder();
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            DataTable dtToReturn = null;

            try
            {
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));
                sqlparams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlparams.Add(new SqlParameter("@ToDate", dateTo));

                string str = AdditionalQueryParameters.ToString() != string.Empty ? " and " + AdditionalQueryParameters.ToString() : "";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_GetProfitAndLossReport + str,
                    sqlparams.ToArray());

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

        public static DataTable GetDataByDetailSourceIDs(int companyID, int periodID, int sourceHeaderID, int sourceDetailID, int documentTypeID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SourceHeaderID", sourceHeaderID);
                sqlparams[3] = new SqlParameter("@SourceDetailID", sourceDetailID);
                sqlparams[4] = new SqlParameter("@DocumentTypeID", documentTypeID);

                string Query = @" select * from GL_TransactionHeader 
    inner join GL_TransactionDetail on GL_TransactionDetail.TransactionID = GL_TransactionHeader.TransactionID 
        and GL_TransactionDetail.CompanyID = GL_TransactionHeader.CompanyID and GL_TransactionDetail.PeriodID = GL_TransactionHeader.PeriodID 
        and GL_TransactionDetail.STatus = 1 and GL_TransactionHeader.Status = 1 
where SourceHeaderID =  @SourceHeaderID and SourceDetailID = @SourceDetailID and GL_TransactionHeader.CompanyID = @CompanyID 
    and GL_TransactionHeader.PeriodID = @PeriodID and DocumentTypeID = @DocumentTypeID ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);

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

        public static DataTable GetBalanceSheetDataDashboard(int companyID, int chartOfAccountID, int periodID, int ChartOfAccountSeg, DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            StringBuilder AdditionalQueryParameters = new StringBuilder();
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            DataTable dtToReturn = null;



            try
            {
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));
                sqlparams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlparams.Add(new SqlParameter("@ToDate", dateTo));
                sqlparams.Add(new SqlParameter("@ChartOfAccountSEG", ChartOfAccountSeg));

                string str = AdditionalQueryParameters.ToString() != string.Empty ? " and " + AdditionalQueryParameters.ToString() : "";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_GetBalanceSheetReportForDashboard + str, sqlparams.ToArray());

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

        public DataTable GetBalanceSheetData(int companyID, int chartOfAccountID, int periodID, DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            StringBuilder AdditionalQueryParameters = new StringBuilder();
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            DataTable dtToReturn = null;

            try
            {
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));
                sqlparams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlparams.Add(new SqlParameter("@ToDate", dateTo));

                string str = AdditionalQueryParameters.ToString() != string.Empty ? " and " + AdditionalQueryParameters.ToString() : "";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_GetBalanceSheetReport + str, sqlparams.ToArray());

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

        public DataTable GetTrialBalanceDrillDown(int companyID, int chartOfAccountID, int periodID, string strWhere)
        {
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            DataTable dtToReturn = null;

            try
            {
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_GetTrialBalanceDrillDown + strWhere, sqlparams.ToArray());

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

        public DataTable Account_Ledger_Report(string companyID, int periodID, int chartOfAccountID, DateTime dateFrom, DateTime dateTo, string accountCodeFrom,
            string accountCodeTo, int fromDocumentType, int toDocumentType, bool reversedDocuments, string documentGroupID, Guid userID,
            bool documentTypeAuthentication, int? costCenterID, string Narration, Nullable<int> DepartmentID = null)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            string accountCodeFilter = string.Empty;
            string qry_docuTypeAuthenticate = string.Empty;
            StringBuilder additionalFilter = new StringBuilder();
            StringBuilder narrationFilter = new StringBuilder();
            DataTable dtToReturn = null;

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));

                #region Filter by Account Codes

                if (!string.IsNullOrEmpty(accountCodeFrom) && !string.IsNullOrEmpty(accountCodeTo))
                {
                    accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID BETWEEN @AccountCodeFrom AND @AccountCodeTo ";
                    sqlParams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                }
                else if (!string.IsNullOrEmpty(accountCodeFrom))
                {
                    accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID >= @AccountCodeFrom ";
                    sqlParams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                }
                else if (!string.IsNullOrEmpty(accountCodeTo))
                {
                    accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID <= @AccountCodeTo ";
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                }

                #endregion

                if (fromDocumentType != -1 && toDocumentType != -1)
                    additionalFilter.Append(" AND GL_TransactionHeader.DocumentTypeID between " + fromDocumentType + " AND " + toDocumentType);

                if (reversedDocuments)
                    additionalFilter.Append(" AND IsNull(GL_TransactionHeader.IsReversed, 0) = 0 And GL_TransactionHeader.ReversalID is null ");

                if (documentGroupID != "")
                    additionalFilter.Append(" AND Set_DocumentType.GroupID in " + documentGroupID.ToString() + " ");

                if (documentTypeAuthentication)
                {
                    sqlParams.Add(new SqlParameter("@UserID", userID));
                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                if (costCenterID.HasValue)
                    additionalFilter.Append(" AND Set_CostCenter.CostCenterID = " + costCenterID.Value.ToString() + " ");

                if (Narration != "")
                    narrationFilter.Append(" and isnull(GL_TransactionDetail.Narration,'') like '%" + Narration.ToString() + "%' ");

                if (DepartmentID.HasValue)
                {
                    additionalFilter.Append(" and GL_TransactionDetail.DepartmentID = " + DepartmentID.Value.ToString() + " ");
                }

                companyID = "(" + companyID + ")";
                string query = FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_Account_Ledger.Replace("{AccountCodeFilter}", accountCodeFilter);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, additionalFilter.ToString(), qry_docuTypeAuthenticate, narrationFilter).Replace("@CompanyID", companyID),
                    sqlParams.ToArray());

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


        public DataTable PF_FinancialReport(DateTime dateFrom, DateTime dateTo)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();

            DataTable dtToReturn = null;

            try
            {

                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));

                string query = FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_FinancialReport_PF;
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query), sqlParams.ToArray());

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


        public DataTable Account_Ledger_Report2(string companyID, int periodID, int chartOfAccountID, DateTime dateFrom, DateTime dateTo, string accountCodeFrom,
            string accountCodeTo, int fromDocumentType, int toDocumentType, bool reversedDocuments, string documentGroupID, Guid userID,
            bool documentTypeAuthentication, int? costCenterID, string Narration, string Particular, Nullable<int> DepartmentID = null
            , DateTime? CreateDateFrom = null, DateTime? CreateDateTo = null
            ,int? TocostCenterID = null
            , int? ToDepartmentID = null
            )
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            string accountCodeFilter = string.Empty;
            string qry_docuTypeAuthenticate = string.Empty;
            StringBuilder additionalFilter = new StringBuilder();
            StringBuilder narrationFilter = new StringBuilder();
            StringBuilder particularFilter = new StringBuilder();
            DataTable dtToReturn = null;

            int _FromCSID = 0, _ToCSID = 0;
            int _FromDepID = 0, _ToDepID = 0;

            if (costCenterID.HasValue && TocostCenterID.HasValue)
            {
                if (costCenterID > TocostCenterID)
                {
                    _FromCSID = TocostCenterID.Value;
                    _ToCSID = costCenterID.Value;
                }
                else
                {
                    _FromCSID = costCenterID.Value;
                    _ToCSID = TocostCenterID.Value;
                }
            }
            else if (costCenterID.HasValue)
            {
                _FromCSID = costCenterID.Value;
            }

            if (DepartmentID.HasValue && ToDepartmentID.HasValue)
            {
                if (DepartmentID > ToDepartmentID)
                {
                    _FromDepID = ToDepartmentID.Value;
                    _ToDepID = DepartmentID.Value;
                }
                else
                {
                    _FromDepID = DepartmentID.Value;
                    _ToDepID = ToDepartmentID.Value;
                }
            }
            else if (DepartmentID.HasValue)
            {
                _FromDepID = DepartmentID.Value;
            }

            #region Query

            string Query_Account_Ledger = @"
select * from (
select 
 FinTAB.CodeCombinationID,FinTAB.DocumentNo , FinTAB.DocumentDate ,UPPER(FinTAB.Narration) AS Narration
 ,case when FinTAB.Amount > 0 then FinTAB.Amount else 0  end as DebitAmount,
 case when FinTAB.Amount < 0 then abs(FinTAB.Amount) else 0  end as CreditAmount
 ,UPPER(FinTAB.TransactionType) AS TransactionType,UPPER(FinTAB.Description) AS Description,UPPER(FinTAB.CodeDescription) AS CodeDescription,UPPER(FinTAB.DocumentType) AS DocumentType
,UPPER(FinTAB.CostCenter) AS CostCenter,FinTAB.SerialNo,null as CreateDate,null as CreatedBy,OperatingUnitName
,'' AS NarrationHeader,0  as DocumentTypeID,DepartmentName
 from
(

SELECT GL_TransactionDetail.CodeCombinationID, '' as DocumentNo, @FromDate as DocumentDate, 'Opening Balance' as Narration, 
    (ISNULL(SUM(CASE WHEN GL_TransactionDetail.TransactionType = 'Debit' THEN GL_TransactionDetail.Amount ELSE 0 END), 0) -
    ISNULL(SUM(CASE WHEN GL_TransactionDetail.TransactionType = 'Credit' THEN GL_TransactionDetail.Amount ELSE 0 END), 0)) AS Amount, '' as TransactionType, 
    GL_CodeCombinationDetail.Description ,
    GL_CodeCombinationDetail.Description + '-' + SET_OperatingUnit.OperatingUnitName as  CodeDescription, '' as DocumentType, 
--Set_CostCenter.CostCenterName As CostCenter,
'' As CostCenter,
0 As SerialNo,SET_OperatingUnit.OperatingUnitName
,'' As DepartmentName
FROM GL_TransactionDetail 
    INNER JOIN GL_TransactionHeader ON GL_TransactionHeader.CompanyID = GL_TransactionDetail.CompanyID 
        AND GL_TransactionHeader.PeriodID = GL_TransactionDetail.PeriodID AND GL_TransactionHeader.TransactionID = GL_TransactionDetail.TransactionID 
        AND GL_TransactionHeader.Status = 1 
    INNER JOIN GL_CodeCombinationDetail ON 
        --GL_CodeCombinationDetail.CompanyID = GL_TransactionDetail.CompanyID AND 
        GL_CodeCombinationDetail.ChartOfAccountID = GL_TransactionDetail.ChartOfAccountID 
        AND GL_CodeCombinationDetail.CodeCombinationID = GL_TransactionDetail.CodeCombinationID 
    INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = GL_TransactionHeader.CompanyID AND SET_DocumentType.PeriodID = GL_TransactionHeader.PeriodID 
        AND SET_DocumentType.DocumentTypeID = GL_TransactionHeader.DocumentTypeID 
    LEFT JOIN Set_CostCenter ON Set_CostCenter.CostCenterID = GL_TransactionDetail.CostCenterID 
inner join SET_Company on SET_Company.CompanyId = GL_TransactionHeader.CompanyID
	inner join SET_OperatingUnit on SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId
    {1} 
WHERE GL_TransactionDetail.CompanyID in @CompanyID AND GL_TransactionDetail.PeriodID = @PeriodID AND GL_TransactionDetail.ChartOfAccountID = @ChartOfAccountID 
    AND GL_TransactionDetail.Status = 1 AND GL_TransactionHeader.DocumentDate < @FromDate 
    {AccountCodeFilter} 
    {0}
    And GL_TransactionHeader.StatusCode = 1 
GROUP BY GL_TransactionDetail.CodeCombinationID, GL_CodeCombinationDetail.Description,SET_OperatingUnit.OperatingUnitName
)as FinTAB
--{2}
union all 
select * from (
SELECT distinct GL_TransactionDetail.CodeCombinationID, GL_TransactionHeader.DocumentNo, GL_TransactionHeader.DocumentDate, GL_TransactionDetail.Narration, 
    CASE WHEN GL_TransactionDetail.TransactionType = 'Debit' THEN GL_TransactionDetail.Amount ELSE 0 END AS DebitAmount, 
    CASE WHEN GL_TransactionDetail.TransactionType = 'Credit' THEN GL_TransactionDetail.Amount ELSE 0 END AS CreditAmount, GL_TransactionDetail.TransactionType, 
    GL_CodeCombinationDetail.Description ,
    GL_CodeCombinationDetail.Description + '-' + SET_OperatingUnit.OperatingUnitName as  CodeDescription, 
    SET_DocumentType.DocumentType, Set_CostCenter.CostCenterName As CostCenter, GL_TransactionDetail.SerialNo
,	GL_TransactionHeader.CreateDate,GL_TransactionHeader.CreatedByValue as CreatedBy,OperatingUnitName
,GL_TransactionHeader.Narration AS NarrationHeader,GL_TransactionHeader.DocumentTypeID
,SET_Department.DepartmentName
FROM GL_TransactionDetail 
    INNER JOIN GL_TransactionHeader ON GL_TransactionHeader.CompanyID = GL_TransactionDetail.CompanyID 
        AND GL_TransactionHeader.PeriodID = GL_TransactionDetail.PeriodID AND GL_TransactionHeader.TransactionID = GL_TransactionDetail.TransactionID 
        AND GL_TransactionHeader.Status = 1 
    INNER JOIN GL_CodeCombinationDetail ON 
--GL_CodeCombinationDetail.CompanyID = GL_TransactionDetail.CompanyID  AND 
GL_CodeCombinationDetail.ChartOfAccountID = GL_TransactionDetail.ChartOfAccountID 
        AND GL_CodeCombinationDetail.CodeCombinationID = GL_TransactionDetail.CodeCombinationID 
    INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = GL_TransactionDetail.CompanyID AND SET_DocumentType.PeriodID = GL_TransactionDetail.PeriodID 
        AND SET_DocumentType.DocumentTypeID = GL_TransactionHeader.DocumentTypeID 
    LEFT JOIN Set_CostCenter ON Set_CostCenter.CostCenterID = GL_TransactionDetail.CostCenterID 
LEFT JOIN SET_Department ON SET_Department.DepartmentID = GL_TransactionDetail.DepartmentID
inner join SET_Company on SET_Company.CompanyId = GL_TransactionHeader.CompanyID
	inner join SET_OperatingUnit on SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId
    {1} 
WHERE GL_TransactionDetail.CompanyID in @CompanyID AND GL_TransactionDetail.PeriodID = @PeriodID AND GL_TransactionDetail.ChartOfAccountID = @ChartOfAccountID 
    AND GL_TransactionDetail.Status = 1 AND GL_TransactionHeader.StatusCode = 1 AND GL_TransactionHeader.DocumentDate BETWEEN @FromDate AND @ToDate 
--AND GL_TransactionHeader.TransactionID  not in (
--		 select GL_TransactionHeader_inner.TransactionID from GL_TransactionHeader as GL_TransactionHeader_inner
-- INNER JOIN SET_DocumentType as SET_DocumentType_inner  ON GL_TransactionHeader_inner.CompanyID = SET_DocumentType.CompanyID AND
--        GL_TransactionHeader_inner.PeriodID = SET_DocumentType_inner.PeriodID AND
--        GL_TransactionHeader_inner.DocumentTypeID = SET_DocumentType_inner.DocumentTypeID 
--		where SET_DocumentType_inner.DocumentType LIKE 'GL-OPENING' + '%'
--		AND GL_TransactionHeader_inner.DocumentDate BETWEEN '2024-09-02' AND '2024-10-29' 
--		 ) Done By Shahmir Shah And Sir Kumail
    {AccountCodeFilter} 
    {0}
    And GL_TransactionHeader.StatusCode = 1 
Group By GL_TransactionHeader.DocumentNo, GL_TransactionDetail.CodeCombinationID, GL_TransactionHeader.DocumentDate, GL_TransactionDetail.Narration, 
    GL_TransactionDetail.Amount, GL_TransactionDetail.TransactionType, GL_CodeCombinationDetail.Description, SET_DocumentType.DocumentType, 
    GL_TransactionDetail.SerialNo, Set_CostCenter.CostCenterName,GL_TransactionDetail.SerialNo,GL_TransactionHeader.CreateDate,OperatingUnitName
	,GL_TransactionHeader.Narration,GL_TransactionHeader.DocumentTypeID,SET_Department.DepartmentName,GL_TransactionHeader.CreatedByValue
) as FinTAB
{2}
{3}

) as tab
order by CodeCombinationID

--	Union All

--	SELECT distinct '' as CodeCombinationID, '' as DocumentNo, '' as DocumentDate, '' as Narration, 
--    0 as DebitAmount, 
--    0 AS CreditAmount, '' as TransactionType, 

--    '' as Description, '' as DocumentType, '' As CostCenter, '' as SerialNo,null as CreateDate,'' as OperatingUnitName
--	,'' AS NarrationHeader,0 as DocumentTypeID
--FROM GL_TransactionDetai
 ";

            #endregion

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));

                #region Filter by Account Codes

                if (!string.IsNullOrEmpty(accountCodeFrom) && !string.IsNullOrEmpty(accountCodeTo))
                {
                    accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID BETWEEN @AccountCodeFrom AND @AccountCodeTo ";
                    sqlParams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                }
                else if (!string.IsNullOrEmpty(accountCodeFrom))
                {
                    accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID >= @AccountCodeFrom ";
                    sqlParams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                }
                else if (!string.IsNullOrEmpty(accountCodeTo))
                {
                    accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID <= @AccountCodeTo ";
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                }

                #endregion

                if (fromDocumentType != -1 && toDocumentType != -1)
                    additionalFilter.Append(" AND GL_TransactionHeader.DocumentTypeID between " + fromDocumentType + " AND " + toDocumentType);

                if (reversedDocuments)
                    additionalFilter.Append(" AND IsNull(GL_TransactionHeader.IsReversed, 0) = 0 And GL_TransactionHeader.ReversalID is null ");

                if (documentGroupID != "")
                    additionalFilter.Append(" AND Set_DocumentType.GroupID in " + documentGroupID.ToString() + " ");

                if (CreateDateFrom.HasValue && CreateDateTo.HasValue)
                {
                    additionalFilter.Append(" AND CONVERT(DATE,GL_TransactionHeader.CreateDate) BETWEEN '" + ((DateTime)CreateDateFrom).ToString("yyyy-MM-dd") + "' AND '" + ((DateTime)CreateDateTo).ToString("yyyy-MM-dd") + "' ");
                }
                else if (CreateDateFrom.HasValue)
                {
                    additionalFilter.Append(" AND CONVERT(DATE,GL_TransactionHeader.CreateDate)  >='" + ((DateTime)CreateDateFrom).ToString("yyyy-MM-dd") + "'");
                }
                else if (CreateDateTo.HasValue)
                {
                    additionalFilter.Append(" AND CONVERT(DATE,GL_TransactionHeader.CreateDate)  <='" + ((DateTime)CreateDateTo).ToString("yyyy-MM-dd") + "'");
                }


                if (documentTypeAuthentication)
                {
                    sqlParams.Add(new SqlParameter("@UserID", userID));
                    //                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
                    //AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                if (_FromCSID > 0 && _ToCSID > 0)
                {
                    additionalFilter.Append(" AND Set_CostCenter.CostCenterID Between " + _FromCSID + " AND " + _ToCSID);
                }
                else if (_FromCSID > 0)
                {
                    additionalFilter.Append(" AND Set_CostCenter.CostCenterID = " + _FromCSID);
                }

                if (Narration != "")
                    narrationFilter.Append(" where isnull(FinTAB.NarrationHeader,'') like '%" + Narration.ToString() + "%' ");

                if (Particular != "" && Narration != "")
                {
                    particularFilter.Append(" and isnull(FinTAB.Narration, '') like '%" + Particular.ToString() + "%' ");
                }
                if (Particular != "" && Narration == "")
                {
                    particularFilter.Append(" where  isnull(FinTAB.Narration, '') like '%" + Particular.ToString() + "%' ");
                }

                if (_FromDepID > 0 && _ToDepID > 0)
                {
                    additionalFilter.Append(" And GL_TransactionDetail.DepartmentID Between " + _FromDepID + " AND " + _ToDepID);
                }
                else if (_FromDepID > 0)
                {
                    additionalFilter.Append(" And GL_TransactionDetail.DepartmentID = " + _FromDepID);
                }

                companyID = "(" + companyID + ")";

                string query = Query_Account_Ledger.Replace("{AccountCodeFilter}", accountCodeFilter);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, additionalFilter.ToString(), qry_docuTypeAuthenticate, narrationFilter, particularFilter).Replace("@CompanyID", companyID),
                    sqlParams.ToArray());

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

        public DataTable AccountLedgerCostCenterWise_ReportData(string companyID, int periodID, int chartOfAccountID, DateTime dateFrom, DateTime dateTo, string accountCodeFrom,
            string accountCodeTo, int fromDocumentType, int toDocumentType, bool reversedDocuments, string documentGroupID, Guid userID,
            bool documentTypeAuthentication, int? costCenterID, string Narration, string Particular, Nullable<int> DepartmentID = null
            , DateTime? CreateDateFrom = null, DateTime? CreateDateTo = null
            , int? TocostCenterID = null
            , int? ToDepartmentID = null
            )
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            string accountCodeFilter = string.Empty;
            string qry_docuTypeAuthenticate = string.Empty;
            StringBuilder additionalFilter = new StringBuilder();
            StringBuilder narrationFilter = new StringBuilder();
            StringBuilder particularFilter = new StringBuilder();
            DataTable dtToReturn = null;

            int _FromCSID = 0, _ToCSID = 0;
            int _FromDepID = 0, _ToDepID = 0;

            if (costCenterID.HasValue && TocostCenterID.HasValue)
            {
                if (costCenterID > TocostCenterID)
                {
                    _FromCSID = TocostCenterID.Value;
                    _ToCSID = costCenterID.Value;
                }
                else
                {
                    _FromCSID = costCenterID.Value;
                    _ToCSID = TocostCenterID.Value;
                }
            }
            else if (costCenterID.HasValue)
            {
                _FromCSID = costCenterID.Value;
            }

            if (DepartmentID.HasValue && ToDepartmentID.HasValue)
            {
                if (DepartmentID > ToDepartmentID)
                {
                    _FromDepID = ToDepartmentID.Value;
                    _ToDepID = DepartmentID.Value;
                }
                else
                {
                    _FromDepID = DepartmentID.Value;
                    _ToDepID = ToDepartmentID.Value;
                }
            }
            else if (DepartmentID.HasValue)
            {
                _FromDepID = DepartmentID.Value;
            }

            #region Query

            string Query_AccountLedgerCostCenterWise = @"
select * from (
select 
 FinTAB.CodeCombinationID,FinTAB.DocumentNo , FinTAB.DocumentDate ,UPPER(FinTAB.Narration) AS Narration
 ,case when FinTAB.Amount > 0 then FinTAB.Amount else 0  end as DebitAmount,
 case when FinTAB.Amount < 0 then abs(FinTAB.Amount) else 0  end as CreditAmount
 ,UPPER(FinTAB.TransactionType) AS TransactionType,UPPER(FinTAB.Description) AS Description,UPPER(FinTAB.CodeDescription) AS CodeDescription,UPPER(FinTAB.DocumentType) AS DocumentType
,UPPER(FinTAB.CostCenter) AS CostCenter,FinTAB.SerialNo,null as CreateDate,null as CreatedBy,OperatingUnitName
,'' AS NarrationHeader,0  as DocumentTypeID,DepartmentName
 from
(

SELECT GL_TransactionDetail.CodeCombinationID, '' as DocumentNo, @FromDate as DocumentDate, 'Opening Balance' as Narration, 
    (ISNULL(SUM(CASE WHEN GL_TransactionDetail.TransactionType = 'Debit' THEN GL_TransactionDetail.Amount ELSE 0 END), 0) -
    ISNULL(SUM(CASE WHEN GL_TransactionDetail.TransactionType = 'Credit' THEN GL_TransactionDetail.Amount ELSE 0 END), 0)) AS Amount, '' as TransactionType, 
    GL_CodeCombinationDetail.Description ,
    GL_CodeCombinationDetail.Description + '-' + SET_OperatingUnit.OperatingUnitName as  CodeDescription, '' as DocumentType, 
--Set_CostCenter.CostCenterName As CostCenter,
'' As CostCenter,
0 As SerialNo,SET_OperatingUnit.OperatingUnitName
,'' As DepartmentName
FROM GL_TransactionDetail 
    INNER JOIN GL_TransactionHeader ON GL_TransactionHeader.CompanyID = GL_TransactionDetail.CompanyID 
        AND GL_TransactionHeader.PeriodID = GL_TransactionDetail.PeriodID AND GL_TransactionHeader.TransactionID = GL_TransactionDetail.TransactionID 
        AND GL_TransactionHeader.Status = 1 
    INNER JOIN GL_CodeCombinationDetail ON 
        --GL_CodeCombinationDetail.CompanyID = GL_TransactionDetail.CompanyID AND 
        GL_CodeCombinationDetail.ChartOfAccountID = GL_TransactionDetail.ChartOfAccountID 
        AND GL_CodeCombinationDetail.CodeCombinationID = GL_TransactionDetail.CodeCombinationID 
    INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = GL_TransactionHeader.CompanyID AND SET_DocumentType.PeriodID = GL_TransactionHeader.PeriodID 
        AND SET_DocumentType.DocumentTypeID = GL_TransactionHeader.DocumentTypeID 
    LEFT JOIN Set_CostCenter ON Set_CostCenter.CostCenterID = GL_TransactionDetail.CostCenterID 
inner join SET_Company on SET_Company.CompanyId = GL_TransactionHeader.CompanyID
	inner join SET_OperatingUnit on SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId
    {1} 
WHERE GL_TransactionDetail.CompanyID in @CompanyID AND GL_TransactionDetail.PeriodID = @PeriodID AND GL_TransactionDetail.ChartOfAccountID = @ChartOfAccountID 
    AND GL_TransactionDetail.Status = 1 AND GL_TransactionHeader.DocumentDate < @FromDate 
    {AccountCodeFilter} 
    {0}
    And GL_TransactionHeader.StatusCode = 1 
GROUP BY GL_TransactionDetail.CodeCombinationID, GL_CodeCombinationDetail.Description,SET_OperatingUnit.OperatingUnitName
)as FinTAB
--{2}
union all 
select * from (
SELECT distinct GL_TransactionDetail.CodeCombinationID, GL_TransactionHeader.DocumentNo, GL_TransactionHeader.DocumentDate, GL_TransactionDetail.Narration, 
    CASE WHEN GL_TransactionDetail.TransactionType = 'Debit' THEN GL_TransactionDetail.Amount ELSE 0 END AS DebitAmount, 
    CASE WHEN GL_TransactionDetail.TransactionType = 'Credit' THEN GL_TransactionDetail.Amount ELSE 0 END AS CreditAmount, GL_TransactionDetail.TransactionType, 
    GL_CodeCombinationDetail.Description ,
    GL_CodeCombinationDetail.Description + '-' + SET_OperatingUnit.OperatingUnitName as  CodeDescription, 
    SET_DocumentType.DocumentType, Set_CostCenter.CostCenterName As CostCenter, GL_TransactionDetail.SerialNo
,	GL_TransactionHeader.CreateDate,GL_TransactionHeader.CreatedByValue as CreatedBy,OperatingUnitName
,GL_TransactionHeader.Narration AS NarrationHeader,GL_TransactionHeader.DocumentTypeID
,SET_Department.DepartmentName
FROM GL_TransactionDetail 
    INNER JOIN GL_TransactionHeader ON GL_TransactionHeader.CompanyID = GL_TransactionDetail.CompanyID 
        AND GL_TransactionHeader.PeriodID = GL_TransactionDetail.PeriodID AND GL_TransactionHeader.TransactionID = GL_TransactionDetail.TransactionID 
        AND GL_TransactionHeader.Status = 1 
    INNER JOIN GL_CodeCombinationDetail ON 
--GL_CodeCombinationDetail.CompanyID = GL_TransactionDetail.CompanyID  AND 
GL_CodeCombinationDetail.ChartOfAccountID = GL_TransactionDetail.ChartOfAccountID 
        AND GL_CodeCombinationDetail.CodeCombinationID = GL_TransactionDetail.CodeCombinationID 
    INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = GL_TransactionDetail.CompanyID AND SET_DocumentType.PeriodID = GL_TransactionDetail.PeriodID 
        AND SET_DocumentType.DocumentTypeID = GL_TransactionHeader.DocumentTypeID 
    LEFT JOIN Set_CostCenter ON Set_CostCenter.CostCenterID = GL_TransactionDetail.CostCenterID 
LEFT JOIN SET_Department ON SET_Department.DepartmentID = GL_TransactionDetail.DepartmentID
inner join SET_Company on SET_Company.CompanyId = GL_TransactionHeader.CompanyID
	inner join SET_OperatingUnit on SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId
    {1} 
WHERE GL_TransactionDetail.CompanyID in @CompanyID AND GL_TransactionDetail.PeriodID = @PeriodID AND GL_TransactionDetail.ChartOfAccountID = @ChartOfAccountID 
    AND GL_TransactionDetail.Status = 1 AND GL_TransactionHeader.StatusCode = 1 AND GL_TransactionHeader.DocumentDate BETWEEN @FromDate AND @ToDate 
--AND GL_TransactionHeader.TransactionID  not in (
--		 select GL_TransactionHeader_inner.TransactionID from GL_TransactionHeader as GL_TransactionHeader_inner
-- INNER JOIN SET_DocumentType as SET_DocumentType_inner  ON GL_TransactionHeader_inner.CompanyID = SET_DocumentType.CompanyID AND
--        GL_TransactionHeader_inner.PeriodID = SET_DocumentType_inner.PeriodID AND
--        GL_TransactionHeader_inner.DocumentTypeID = SET_DocumentType_inner.DocumentTypeID 
--		where SET_DocumentType_inner.DocumentType LIKE 'GL-OPENING' + '%'
--		AND GL_TransactionHeader_inner.DocumentDate BETWEEN '2024-09-02' AND '2024-10-29' 
--		 ) Done By Shahmir Shah And Sir Kumail
    {AccountCodeFilter} 
    {0}
    And GL_TransactionHeader.StatusCode = 1 
Group By GL_TransactionHeader.DocumentNo, GL_TransactionDetail.CodeCombinationID, GL_TransactionHeader.DocumentDate, GL_TransactionDetail.Narration, 
    GL_TransactionDetail.Amount, GL_TransactionDetail.TransactionType, GL_CodeCombinationDetail.Description, SET_DocumentType.DocumentType, 
    GL_TransactionDetail.SerialNo, Set_CostCenter.CostCenterName,GL_TransactionDetail.SerialNo,GL_TransactionHeader.CreateDate,OperatingUnitName
	,GL_TransactionHeader.Narration,GL_TransactionHeader.DocumentTypeID,SET_Department.DepartmentName,GL_TransactionHeader.CreatedByValue
) as FinTAB
{2}
{3}

) as tab
order by CodeCombinationID

 ";

            #endregion

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));

                #region Filter by Account Codes

                if (!string.IsNullOrEmpty(accountCodeFrom) && !string.IsNullOrEmpty(accountCodeTo))
                {
                    accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID BETWEEN @AccountCodeFrom AND @AccountCodeTo ";
                    sqlParams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                }
                else if (!string.IsNullOrEmpty(accountCodeFrom))
                {
                    accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID >= @AccountCodeFrom ";
                    sqlParams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                }
                else if (!string.IsNullOrEmpty(accountCodeTo))
                {
                    accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID <= @AccountCodeTo ";
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                }

                #endregion

                if (fromDocumentType != -1 && toDocumentType != -1)
                    additionalFilter.Append(" AND GL_TransactionHeader.DocumentTypeID between " + fromDocumentType + " AND " + toDocumentType);

                if (reversedDocuments)
                    additionalFilter.Append(" AND IsNull(GL_TransactionHeader.IsReversed, 0) = 0 And GL_TransactionHeader.ReversalID is null ");

                if (documentGroupID != "")
                    additionalFilter.Append(" AND Set_DocumentType.GroupID in " + documentGroupID.ToString() + " ");

                if (CreateDateFrom.HasValue && CreateDateTo.HasValue)
                {
                    additionalFilter.Append(" AND CONVERT(DATE,GL_TransactionHeader.CreateDate) BETWEEN '" + ((DateTime)CreateDateFrom).ToString("yyyy-MM-dd") + "' AND '" + ((DateTime)CreateDateTo).ToString("yyyy-MM-dd") + "' ");
                }
                else if (CreateDateFrom.HasValue)
                {
                    additionalFilter.Append(" AND CONVERT(DATE,GL_TransactionHeader.CreateDate)  >='" + ((DateTime)CreateDateFrom).ToString("yyyy-MM-dd") + "'");
                }
                else if (CreateDateTo.HasValue)
                {
                    additionalFilter.Append(" AND CONVERT(DATE,GL_TransactionHeader.CreateDate)  <='" + ((DateTime)CreateDateTo).ToString("yyyy-MM-dd") + "'");
                }


                if (documentTypeAuthentication)
                {
                    sqlParams.Add(new SqlParameter("@UserID", userID));
                    //                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
                    //AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                if (_FromCSID > 0 && _ToCSID > 0)
                {
                    additionalFilter.Append(" AND Set_CostCenter.CostCenterID Between " + _FromCSID + " AND " + _ToCSID);
                }
                else if (_FromCSID > 0)
                {
                    additionalFilter.Append(" AND Set_CostCenter.CostCenterID = " + _FromCSID);
                }

                if (Narration != "")
                    narrationFilter.Append(" where isnull(FinTAB.NarrationHeader,'') like '%" + Narration.ToString() + "%' ");

                if (Particular != "" && Narration != "")
                {
                    particularFilter.Append(" and isnull(FinTAB.Narration, '') like '%" + Particular.ToString() + "%' ");
                }
                if (Particular != "" && Narration == "")
                {
                    particularFilter.Append(" where  isnull(FinTAB.Narration, '') like '%" + Particular.ToString() + "%' ");
                }

                if (_FromDepID > 0 && _ToDepID > 0)
                {
                    additionalFilter.Append(" And GL_TransactionDetail.DepartmentID Between " + _FromDepID + " AND " + _ToDepID);
                }
                else if (_FromDepID > 0)
                {
                    additionalFilter.Append(" And GL_TransactionDetail.DepartmentID = " + _FromDepID);
                }

                companyID = "(" + companyID + ")";

                string query = Query_AccountLedgerCostCenterWise.Replace("{AccountCodeFilter}", accountCodeFilter);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, additionalFilter.ToString(), qry_docuTypeAuthenticate, narrationFilter, particularFilter).Replace("@CompanyID", companyID),
                    sqlParams.ToArray());

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

        public DataTable Account_Ledger_ReportNew(string companyID, int periodID, int chartOfAccountID, DateTime dateFrom, DateTime dateTo, string accountCodeFrom,
           string accountCodeTo, int fromDocumentType, int toDocumentType, bool reversedDocuments, string documentGroupID, Guid userID,
           bool documentTypeAuthentication, int? costCenterID, string Narration, string Particular, Nullable<int> DepartmentID = null)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            string accountCodeFilter = string.Empty;
            string qry_docuTypeAuthenticate = string.Empty;
            StringBuilder additionalFilter = new StringBuilder();
            StringBuilder narrationFilter = new StringBuilder();
            StringBuilder particularFilter = new StringBuilder();
            DataTable dtToReturn = null;

            #region Query

            string Query_Account_LedgerNew = FnI.QueryResourceLibrary.GL.GL_TransactionHeader4.Query_Account_LedgerNew;

            #endregion

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));

                #region Filter by Account Codes

                if (!string.IsNullOrEmpty(accountCodeFrom) && !string.IsNullOrEmpty(accountCodeTo))
                {
                    accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID BETWEEN @AccountCodeFrom AND @AccountCodeTo ";
                    sqlParams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                }
                else if (!string.IsNullOrEmpty(accountCodeFrom))
                {
                    accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID >= @AccountCodeFrom ";
                    sqlParams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                }
                else if (!string.IsNullOrEmpty(accountCodeTo))
                {
                    accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID <= @AccountCodeTo ";
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                }

                #endregion

                if (fromDocumentType != -1 && toDocumentType != -1)
                    additionalFilter.Append(" AND GL_TransactionHeader.DocumentTypeID between " + fromDocumentType + " AND " + toDocumentType);

                if (reversedDocuments)
                    additionalFilter.Append(" AND IsNull(GL_TransactionHeader.IsReversed, 0) = 0 And GL_TransactionHeader.ReversalID is null ");

                if (documentGroupID != "")
                    additionalFilter.Append(" AND Set_DocumentType.GroupID in " + documentGroupID.ToString() + " ");

                if (documentTypeAuthentication)
                {
                    sqlParams.Add(new SqlParameter("@UserID", userID));
                    //                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
                    //AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                if (costCenterID.HasValue)
                    additionalFilter.Append(" AND Set_CostCenter.CostCenterID = " + costCenterID.Value.ToString() + " ");

                if (Narration != "")
                    narrationFilter.Append(" where isnull(FinTAB.NarrationHeader,'') like '%" + Narration.ToString() + "%' ");

                if (Particular != "" && Narration != "")
                {
                    particularFilter.Append(" and isnull(FinTAB.Narration, '') like '%" + Particular.ToString() + "%' ");
                }
                if (Particular != "" && Narration == "")
                {
                    particularFilter.Append(" where  isnull(FinTAB.Narration, '') like '%" + Particular.ToString() + "%' ");
                }

                if (DepartmentID.HasValue)
                {
                    additionalFilter.Append(" And GL_TransactionDetail.DepartmentID = " + DepartmentID.Value.ToString() + " ");
                }

                companyID = "(" + companyID + ")";

                string query = Query_Account_LedgerNew.Replace("{AccountCodeFilter}", accountCodeFilter);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, additionalFilter.ToString(), qry_docuTypeAuthenticate, narrationFilter, particularFilter).Replace("@CompanyID", companyID),
                    sqlParams.ToArray());

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

        public DataTable Account_Ledger_ReportForHTMLPopup(int companyID, int periodID, int chartOfAccountID, string Filter)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataTable dtToReturn = null;

            #region Query

            string Query_Account_LedgerForHTMLOnPopup = @"
SELECT GL_TransactionDetail.CodeCombinationID, '' as DocumentNo,GL_TransactionHeader.DocumentDate, 'Opening Balance' as Narration, 
    ISNULL(SUM(CASE WHEN GL_TransactionDetail.TransactionType = 'Debit' THEN GL_TransactionDetail.Amount ELSE 0 END), 0) AS DebitAmount, 
    ISNULL(SUM(CASE WHEN GL_TransactionDetail.TransactionType = 'Credit' THEN GL_TransactionDetail.Amount ELSE 0 END), 0) AS CreditAmount, '' as TransactionType, 
    GL_CodeCombinationDetail.Description, '' as DocumentType, Set_CostCenter.CostCenterName As CostCenter 
FROM GL_TransactionDetail 
    INNER JOIN GL_TransactionHeader ON GL_TransactionHeader.CompanyID = GL_TransactionDetail.CompanyID 
        AND GL_TransactionHeader.PeriodID = GL_TransactionDetail.PeriodID AND GL_TransactionHeader.TransactionID = GL_TransactionDetail.TransactionID 
        AND GL_TransactionHeader.Status = 1 
    INNER JOIN GL_CodeCombinationDetail ON 
--GL_CodeCombinationDetail.CompanyID = GL_TransactionDetail.CompanyID AND 
GL_CodeCombinationDetail.ChartOfAccountID = GL_TransactionDetail.ChartOfAccountID 
        AND GL_CodeCombinationDetail.CodeCombinationID = GL_TransactionDetail.CodeCombinationID 
    LEFT JOIN Set_CostCenter ON Set_CostCenter.CostCenterID = GL_TransactionDetail.CostCenterID 
WHERE GL_TransactionDetail.CompanyID = @CompanyID AND GL_TransactionDetail.PeriodID = @PeriodID AND GL_TransactionDetail.ChartOfAccountID = @ChartOfAccountID 
    AND GL_TransactionDetail.Status = 1 
 {0} 
   
    And GL_TransactionHeader.StatusCode = 1 
GROUP BY GL_TransactionDetail.CodeCombinationID, GL_CodeCombinationDetail.Description, Set_CostCenter.CostCenterName ,GL_TransactionHeader.DocumentDate

union all 

SELECT GL_TransactionDetail.CodeCombinationID, GL_TransactionHeader.DocumentNo, GL_TransactionHeader.DocumentDate, GL_TransactionHeader.Narration, 
    CASE WHEN GL_TransactionDetail.TransactionType = 'Debit' THEN GL_TransactionDetail.Amount ELSE 0 END AS DebitAmount, 
    CASE WHEN GL_TransactionDetail.TransactionType = 'Credit' THEN GL_TransactionDetail.Amount ELSE 0 END AS CreditAmount, GL_TransactionDetail.TransactionType, 
    GL_CodeCombinationDetail.Description, SET_DocumentType.DocumentType, Set_CostCenter.CostCenterName As CostCenter 
FROM GL_TransactionDetail 
    INNER JOIN GL_TransactionHeader ON GL_TransactionHeader.CompanyID = GL_TransactionDetail.CompanyID 
        AND GL_TransactionHeader.PeriodID = GL_TransactionDetail.PeriodID AND GL_TransactionHeader.TransactionID = GL_TransactionDetail.TransactionID 
        AND GL_TransactionHeader.Status = 1 
    INNER JOIN GL_CodeCombinationDetail ON 
--GL_CodeCombinationDetail.CompanyID = GL_TransactionDetail.CompanyID  AND 
GL_CodeCombinationDetail.ChartOfAccountID = GL_TransactionDetail.ChartOfAccountID 
        AND GL_CodeCombinationDetail.CodeCombinationID = GL_TransactionDetail.CodeCombinationID 
    INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = GL_TransactionDetail.CompanyID AND SET_DocumentType.PeriodID = GL_TransactionDetail.PeriodID 
        AND SET_DocumentType.DocumentTypeID = GL_TransactionHeader.DocumentTypeID 
    LEFT JOIN Set_CostCenter ON Set_CostCenter.CostCenterID = GL_TransactionDetail.CostCenterID 
WHERE GL_TransactionDetail.CompanyID = @CompanyID AND GL_TransactionDetail.PeriodID = @PeriodID AND GL_TransactionDetail.ChartOfAccountID = @ChartOfAccountID 
    AND GL_TransactionDetail.Status = 1 AND GL_TransactionHeader.StatusCode = 1 
 {0}     
    And GL_TransactionHeader.StatusCode = 1 
Group By GL_TransactionHeader.DocumentNo, GL_TransactionDetail.CodeCombinationID, GL_TransactionHeader.DocumentDate, GL_TransactionHeader.Narration, 
    GL_TransactionDetail.Amount, GL_TransactionDetail.TransactionType, GL_CodeCombinationDetail.Description, SET_DocumentType.DocumentType, 
    GL_TransactionDetail.SerialNo, Set_CostCenter.CostCenterName ";

            #endregion

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Account_LedgerForHTMLOnPopup, Filter), sqlParams.ToArray());

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

        public DataTable CostCenter_Report(int companyID, int periodID, DateTime dateFrom, DateTime dateTo, Nullable<int> costCenterIDFrom = null,
            Nullable<int> costCenterIDTo = null, short? documentGroupID = null, string accountCodeFrom = "", string accountCodeTo = "")
        {


            List<SqlParameter> sqlParams = new List<SqlParameter>();
            string accountCodeFilter = string.Empty;
            DataTable dtToReturn = null;

            #region Query

            string Query_CostCenter_Ledger = @" select * from 
(
    SELECT GL_TransactionDetail.CodeCombinationID, '' as DocumentNo, @FromDate as DocumentDate, 'Opening Balance' as Narration, 
        ISNULL(SUM(CASE WHEN GL_TransactionDetail.TransactionType = 'Debit' THEN GL_TransactionDetail.Amount ELSE 0 END), 0) AS DebitAmount, 
        ISNULL(SUM(CASE WHEN GL_TransactionDetail.TransactionType = 'Credit' THEN GL_TransactionDetail.Amount ELSE 0 END), 0) AS CreditAmount, '' as TransactionType, 
        GL_CodeCombinationDetail.Description, '' as DocumentType, costcentername
    FROM GL_TransactionDetail 
        INNER JOIN GL_TransactionHeader ON GL_TransactionHeader.CompanyID = GL_TransactionDetail.CompanyID 
            AND GL_TransactionHeader.PeriodID = GL_TransactionDetail.PeriodID AND GL_TransactionHeader.TransactionID = GL_TransactionDetail.TransactionID 
            AND GL_TransactionHeader.Status = 1 
        INNER JOIN GL_CodeCombinationDetail ON 
--GL_CodeCombinationDetail.CompanyID = GL_TransactionDetail.CompanyID  AND 
GL_CodeCombinationDetail.ChartOfAccountID = GL_TransactionDetail.ChartOfAccountID 
            AND GL_CodeCombinationDetail.CodeCombinationID = GL_TransactionDetail.CodeCombinationID 
    inner join SET_costcenter on GL_TransactionDetail.CostCenterID = SET_costcenter.CostCenterID
    WHERE GL_TransactionDetail.CompanyID = @CompanyID AND GL_TransactionDetail.PeriodID = @PeriodID AND GL_TransactionDetail.Status = 1 
        AND GL_TransactionHeader.DocumentDate < @FromDate And GL_TransactionHeader.StatusCode = 1 and GL_TransactionDetail.CostCenterID is not null 
        {AccountCodeFilter} 
GROUP BY GL_TransactionDetail.CodeCombinationID, GL_CodeCombinationDetail.Description,costcentername

union all 

SELECT GL_TransactionDetail.CodeCombinationID, GL_TransactionHeader.DocumentNo, GL_TransactionHeader.DocumentDate, GL_TransactionHeader.Narration, 
    CASE WHEN GL_TransactionDetail.TransactionType = 'Debit' THEN GL_TransactionDetail.Amount ELSE 0 END AS DebitAmount, 
    CASE WHEN GL_TransactionDetail.TransactionType = 'Credit' THEN GL_TransactionDetail.Amount ELSE 0 END AS CreditAmount, GL_TransactionDetail.TransactionType, 
    GL_CodeCombinationDetail.Description, SET_DocumentType.DocumentType, costcentername
FROM GL_TransactionDetail 
    INNER JOIN GL_TransactionHeader ON GL_TransactionHeader.CompanyID = GL_TransactionDetail.CompanyID 
        AND GL_TransactionHeader.PeriodID = GL_TransactionDetail.PeriodID AND GL_TransactionHeader.TransactionID = GL_TransactionDetail.TransactionID 
        AND GL_TransactionHeader.Status = 1 
    INNER JOIN GL_CodeCombinationDetail ON 
--GL_CodeCombinationDetail.CompanyID = GL_TransactionDetail.CompanyID AND 
GL_CodeCombinationDetail.ChartOfAccountID = GL_TransactionDetail.ChartOfAccountID 
        AND GL_CodeCombinationDetail.CodeCombinationID = GL_TransactionDetail.CodeCombinationID 
    INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = GL_TransactionDetail.CompanyID AND SET_DocumentType.PeriodID = GL_TransactionDetail.PeriodID 
        AND SET_DocumentType.DocumentTypeID = GL_TransactionHeader.DocumentTypeID 
		    inner join SET_costcenter on GL_TransactionDetail.CostCenterID = SET_costcenter.CostCenterID
WHERE GL_TransactionDetail.CompanyID = @CompanyID AND GL_TransactionDetail.PeriodID = @PeriodID AND GL_TransactionDetail.Status = 1 
    AND GL_TransactionHeader.StatusCode = 1 AND GL_TransactionHeader.DocumentDate BETWEEN @FromDate AND @ToDate And GL_TransactionHeader.StatusCode = 1 
    and GL_TransactionDetail.CostCenterID is not null 
    {AccountCodeFilter}  {0}
Group By GL_TransactionHeader.DocumentNo, GL_TransactionDetail.CodeCombinationID, GL_TransactionHeader.DocumentDate, GL_TransactionHeader.Narration, 
    GL_TransactionDetail.Amount, GL_TransactionDetail.TransactionType, GL_CodeCombinationDetail.Description, SET_DocumentType.DocumentType, 
    GL_TransactionDetail.SerialNo,costcentername
) as TAB
order by convert(int, TAB.DocumentNo) asc ";

            #endregion

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));

                #region Filter by Cost Center

                if (costCenterIDFrom.HasValue && costCenterIDTo.HasValue)
                {
                    accountCodeFilter = " AND GL_TransactionDetail.CostCenterID BETWEEN @CostCenterFrom AND @CostCenterTo ";
                    sqlParams.Add(new SqlParameter("@CostCenterFrom", costCenterIDFrom.Value));
                    sqlParams.Add(new SqlParameter("@CostCenterTo", costCenterIDTo.Value));
                }
                else if (costCenterIDFrom.HasValue)
                {
                    accountCodeFilter = " AND GL_TransactionDetail.CostCenterID >= @CostCenterFrom ";
                    sqlParams.Add(new SqlParameter("@CostCenterFrom", costCenterIDFrom.Value));
                }
                else if (costCenterIDTo.HasValue)
                {
                    accountCodeFilter = " AND GL_TransactionDetail.CostCenterID <= @CostCenterTo ";
                    sqlParams.Add(new SqlParameter("@CostCenterTo", costCenterIDTo.Value));
                }

                string additionalParam = "";

                if (documentGroupID.HasValue)
                    additionalParam = " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();



                if (accountCodeTo != "" && accountCodeFrom != "")
                {
                    accountCodeFilter += " AND GL_TransactionDetail.CodeCombinationID BETWEEN '" + accountCodeFrom + "' AND '" + accountCodeTo + "' ";
                }


                #endregion

                string query = Query_CostCenter_Ledger.Replace("{AccountCodeFilter}", accountCodeFilter);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(query, additionalParam), sqlParams.ToArray());

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

        public DataTable CostCenter_ReportNew(string companyID, int periodID, DateTime dateFrom, DateTime dateTo, Nullable<int> costCenterIDFrom = null,
            Nullable<int> costCenterIDTo = null, short? documentGroupID = null, string accountCodeFrom = "", string accountCodeTo = "")
        {


            List<SqlParameter> sqlParams = new List<SqlParameter>();
            string accountCodeFilter = string.Empty;
            DataTable dtToReturn = null;

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));

                companyID = "(" + companyID + ")";

                #region Filter by Cost Center

                if (costCenterIDFrom.HasValue && costCenterIDTo.HasValue)
                {
                    accountCodeFilter = " AND GL_TransactionDetail.CostCenterID BETWEEN @CostCenterFrom AND @CostCenterTo ";
                    sqlParams.Add(new SqlParameter("@CostCenterFrom", costCenterIDFrom.Value));
                    sqlParams.Add(new SqlParameter("@CostCenterTo", costCenterIDTo.Value));
                }
                else if (costCenterIDFrom.HasValue)
                {
                    accountCodeFilter = " AND GL_TransactionDetail.CostCenterID >= @CostCenterFrom ";
                    sqlParams.Add(new SqlParameter("@CostCenterFrom", costCenterIDFrom.Value));
                }
                else if (costCenterIDTo.HasValue)
                {
                    accountCodeFilter = " AND GL_TransactionDetail.CostCenterID <= @CostCenterTo ";
                    sqlParams.Add(new SqlParameter("@CostCenterTo", costCenterIDTo.Value));
                }

                string additionalParam = "";

                if (documentGroupID.HasValue)
                    additionalParam = " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();



                if (accountCodeTo != "" && accountCodeFrom != "")
                {
                    accountCodeFilter += " AND GL_TransactionDetail.CodeCombinationID BETWEEN '" + accountCodeFrom + "' AND '" + accountCodeTo + "' ";
                }


                #endregion

                string query = FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_CostCenter_LedgerNew.Replace("{AccountCodeFilter}", accountCodeFilter).Replace("@CompanyID", companyID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, additionalParam.ToString()), sqlParams.ToArray());

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

        public DataTable CostCenter_ReportIncurredNew(string QueryPass, int companyID, int periodID, DateTime dateFrom, DateTime dateTo, string accountCodeFrom, string accountCodeTo, Nullable<int> costCenterIDFrom = null,
           Nullable<int> DocumentTypeID = null, string DivisionIDs = "")
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            string accountCodeFilter = string.Empty;
            string accountCodeFilter2 = string.Empty;
            StringBuilder sb = new StringBuilder();
            DataTable dtToReturn = null;

            #region Query

            string Query_CostCenter_Ledger = QueryPass;

            #endregion

            try
            {

                if (!string.IsNullOrEmpty(DivisionIDs))
                    DivisionIDs = "(" + DivisionIDs + ")";

                //sqlParams.Add(new SqlParameter("@CompanyID", DivisionIDs));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));

                #region Filter by Cost Center

                //if (costCenterIDFrom.HasValue && costCenterIDTo.HasValue)
                //{
                //    accountCodeFilter = " AND GL_TransactionDetail.IncurredOnID BETWEEN @IncurredOnIDFrom AND @IncurredOnIDTo ";
                //    sqlParams.Add(new SqlParameter("@IncurredOnIDFrom", costCenterIDFrom.Value));
                //    sqlParams.Add(new SqlParameter("@IncurredOnIDTo", costCenterIDTo.Value));
                //}
                if (costCenterIDFrom.HasValue)
                {
                    accountCodeFilter = " AND GL_TransactionDetail.IncurredOnID = @IncurredOnIDFrom";
                    sqlParams.Add(new SqlParameter("@IncurredOnIDFrom", costCenterIDFrom.Value));
                }
                //else if (costCenterIDTo.HasValue)
                //{
                //    accountCodeFilter = " AND GL_TransactionDetail.IncurredOnID <= @CostCenterTo ";
                //    sqlParams.Add(new SqlParameter("@IncurredOnIDTo", costCenterIDTo.Value));
                //}
                if (DocumentTypeID.HasValue)
                {
                    sb.Append(" AND GL_TransactionHeader.DocumentTypeID = " + DocumentTypeID);
                }

                #endregion

                #region Filter by Account Codes

                if (!string.IsNullOrEmpty(accountCodeFrom) && !string.IsNullOrEmpty(accountCodeTo))
                {
                    accountCodeFilter2 = " AND GL_TransactionDetail.CodeCombinationID BETWEEN @AccountCodeFrom AND @AccountCodeTo ";
                    sqlParams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                }
                else if (!string.IsNullOrEmpty(accountCodeFrom))
                {
                    accountCodeFilter2 = " AND GL_TransactionDetail.CodeCombinationID >= @AccountCodeFrom ";
                    sqlParams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                }
                else if (!string.IsNullOrEmpty(accountCodeTo))
                {
                    accountCodeFilter2 = " AND GL_TransactionDetail.CodeCombinationID <= @AccountCodeTo ";
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                }

                #endregion

                string query = Query_CostCenter_Ledger.Replace("{AccountCodeFilter}", accountCodeFilter);
                string query2 = query.Replace("{AccountCodeFilter2}", accountCodeFilter2);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query2.Replace("@CompanyID", DivisionIDs), sb.ToString()), sqlParams.ToArray());

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

        public DataTable CostCenter_ReportIncurred(string QueryPass, int companyID, int periodID, DateTime dateFrom, DateTime dateTo, Nullable<int> costCenterIDFrom = null,
           Nullable<int> DocumentTypeID = null)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            string accountCodeFilter = string.Empty;
            StringBuilder sb = new StringBuilder();
            DataTable dtToReturn = null;

            #region Query

            string Query_CostCenter_Ledger = QueryPass;

            #endregion

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));

                #region Filter by Cost Center

                //if (costCenterIDFrom.HasValue && costCenterIDTo.HasValue)
                //{
                //    accountCodeFilter = " AND GL_TransactionDetail.IncurredOnID BETWEEN @IncurredOnIDFrom AND @IncurredOnIDTo ";
                //    sqlParams.Add(new SqlParameter("@IncurredOnIDFrom", costCenterIDFrom.Value));
                //    sqlParams.Add(new SqlParameter("@IncurredOnIDTo", costCenterIDTo.Value));
                //}
                if (costCenterIDFrom.HasValue)
                {
                    accountCodeFilter = " AND GL_TransactionDetail.IncurredOnID = @IncurredOnIDFrom";
                    sqlParams.Add(new SqlParameter("@IncurredOnIDFrom", costCenterIDFrom.Value));
                }
                //else if (costCenterIDTo.HasValue)
                //{
                //    accountCodeFilter = " AND GL_TransactionDetail.IncurredOnID <= @CostCenterTo ";
                //    sqlParams.Add(new SqlParameter("@IncurredOnIDTo", costCenterIDTo.Value));
                //}
                if (DocumentTypeID.HasValue)
                {
                    sb.Append(" AND GL_TransactionHeader.DocumentTypeID = " + DocumentTypeID);
                }

                #endregion

                string query = Query_CostCenter_Ledger.Replace("{AccountCodeFilter}", accountCodeFilter);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, sb.ToString()), sqlParams.ToArray());

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

        public static DataTable GETTransactionDetailByTransactionID(int companyID, int periodID, int transactionID, SqlTransaction obj_SQLTransaction)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            string accountCodeFilter = string.Empty;
            DataTable dtToReturn = null;

            #region Query

            string QueryTransactionDetail = @"
select * from gl_transactionDetail where Transactionid = @TransactionID
and CompanyID = @CompanyID 
and PeriodID = @PeriodID
and status = 1
";
            #endregion

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@TransactionID", transactionID));

                DataSet ds = SqlHelper.ExecuteDataset(obj_SQLTransaction, CommandType.Text, QueryTransactionDetail, sqlParams.ToArray());

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

        public DataTable AccountLedgerReport_ChequeWise(int companyID, int periodID, int chartOfAccountID, DateTime dateFrom, DateTime dateTo, string accountCodeFrom,
            string accountCodeTo, int fromDocumentType, int toDocumentType)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            string accountCodeFilter = string.Empty;
            string documentTypeFilter = string.Empty;
            DataTable dtToReturn = null;

            #region Query

            #endregion

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));

                if (!string.IsNullOrEmpty(accountCodeFrom) && !string.IsNullOrEmpty(accountCodeTo))
                {
                    accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID BETWEEN @AccountCodeFrom AND @AccountCodeTo ";
                    sqlParams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                }
                else if (!string.IsNullOrEmpty(accountCodeFrom))
                {
                    accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID >= @AccountCodeFrom ";
                    sqlParams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                }
                else if (!string.IsNullOrEmpty(accountCodeTo))
                {
                    accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID <= @AccountCodeTo ";
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                }
                else
                    accountCodeFilter = "";

                if (fromDocumentType != -1 && toDocumentType != -1)
                    documentTypeFilter = " AND GL_TransactionHeader.DocumentTypeID between " + fromDocumentType + " AND " + toDocumentType + " ";

                string query = FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_Account_Ledger_ChequeWise.Replace("{AccountCodeFilter}", accountCodeFilter);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, documentTypeFilter), sqlParams.ToArray());

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

        public bool UpdateAmountByCodeCombination(SqlTransaction objTransaction)
        {
            bool result = false;

            string Query_Update_Amount_CodeCombination = @" Update GL_TransactionDetail SET AmountFC = AmountFC - @Amount, Amount = Amount - @Amount 
Where CompanyId = @CompanyId and PeriodId = @PeriodId and TransactionId = @TransactionId and CodeCombinationId = @CodeCombinationId ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[9];
                sqlparams[5] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[7] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[4] = new SqlParameter("@TransactionID", f_TransactionID);
                sqlparams[6] = new SqlParameter("@CodeCombinationId", f_CodeCombinationID);
                sqlparams[8] = new SqlParameter("@Amount", f_Amount);

                if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_Amount_CodeCombination, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }


        public bool UpdateAmountByTransactionID(SqlTransaction objTransaction)
        {
            bool result = false;

            string Query_Update_Amount_TransactionID = @" Update GL_TransactionDetail 
SET AmountFC = @AmountFC, Amount = @Amount, UpdateDate = @UpdateDate, UpdatedBy = @UpdatedBy, UpdatedByValue = @UpdatedByValue 
Where CompanyId = @CompanyId and PeriodId = @PeriodID and TransactionId = @TransactionId ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[2] = new SqlParameter("@TransactionID", f_TransactionID);
                sqlparams[3] = new SqlParameter("@Amount", f_Amount);
                sqlparams[4] = new SqlParameter("@AmountFC", f_AmountFC);
                sqlparams[5] = new SqlParameter("@UpdateDate", f_UpdateDate);
                sqlparams[6] = new SqlParameter("@UpdatedBy", f_UpdatedBy);
                sqlparams[7] = new SqlParameter("@UpdatedByValue", f_UpdatedByValue);

                if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_Amount_TransactionID, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataRow GetTranIdBySourceHeaderId(int companyID, int periodID, long sourceHeaderID, int documentTypeID, SqlConnection objConnection = null)
        {
            DataRow dr = null;

            string Query_GetData_bySourceHdrId = @" SELECT * FROM GL_TransactionDetail 
WHERE CompanyID = @CompanyID and PeriodID = @PeriodID and SourceHeaderId = @SourceHeaderId 
    AND TransactionId IN (Select TransactionId from GL_TransactionHeader where DocumentTypeID = @DocumentTypeId) ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SourceHeaderId", sourceHeaderID);
                sqlparams[3] = new SqlParameter("@DocumentTypeId", documentTypeID);

                DataSet ds = null;

                if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_bySourceHdrId, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_bySourceHdrId, sqlparams);

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

        public bool UpdateDetailAmountCodeCombination(int companyID, int periodID, int transactionID, decimal amount, decimal oldAmount, string ccidWhere, SqlTransaction objTransaction)
        {
            bool result = false;

            string Query_UpdateDetailAmountByCodeCombination = @" Update GL_TransactionDetail Set Amount = @Amount, AmountFC = @Amount 
where TransactionID = @TransactionID and CompanyID = @CompanyID and PeriodID = @PeriodID and Amount = @OldAmount 
    {0} ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TransactionID", transactionID);
                sqlparams[3] = new SqlParameter("@Amount", amount);
                sqlparams[4] = new SqlParameter("@OldAmount", oldAmount);

                if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, string.Format(Query_UpdateDetailAmountByCodeCombination, ccidWhere), sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public bool InsertPF(string DatabaseName, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                StringBuilder sb = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[23];
                sqlparams[0] = new SqlParameter("@TransactionID", f_TransactionID);
                sqlparams[1] = new SqlParameter("@CodeCombinationID", f_CodeCombinationID);
                sqlparams[2] = new SqlParameter("@CurrencyID", f_CurrencyID);

                if (!string.IsNullOrEmpty(f_DocumentReference))
                    sqlparams[3] = new SqlParameter("@DocumentReference", f_DocumentReference);
                else
                    sqlparams[3] = new SqlParameter("@DocumentReference", DBNull.Value);

                if (f_CostCenterID.HasValue)
                {
                    int BSCostCenterID = 0, AccountTypeID = 0, BankCostCentreID = 0;

                    bool customCostCenter = Get_BSCostCentreID_Parameter_GL();

                    bool customCostCenternew = Get_BSCostCentreID_Parameter_GLNew();


                    if (customCostCenternew)
                    {
                        BSCostCenterID = SET_CostCenter.GetBSCostCenter(f_CostCenterID.Value);
                        try
                        {
                            AccountTypeID = GL_TransactionDetail.GetGL_CodeCombinationDetailAccountTypeIDForPF(f_CompanyID, f_CodeCombinationID, DatabaseName);
                            //GL_TransactionHeader.GetAllData(" transactionid = "
                            if (AccountTypeID == 9 || AccountTypeID == 8 || AccountTypeID == 10)
                            {



                                // GL_TransactionHeader g = new GL_TransactionHeader(f_CompanyID, f_PeriodID, f_TransactionID);

                                BSCostCenterID = SET_CostCenter.GetBSCostCenter(f_CostCenterID.Value);

                                try { BankCostCentreID = GetGL_CodeCombinationDetailBankAccountCostCentreID(f_CompanyID, f_CodeCombinationID); }
                                catch { }


                                if (BankCostCentreID != 0)
                                    sqlparams[4] = new SqlParameter("@CostCenterID", BankCostCentreID);
                                else
                                    sqlparams[4] = new SqlParameter("@CostCenterID", BSCostCenterID);





                            }
                            if (BSCostCenterID == 0)
                            {
                                sqlparams[4] = new SqlParameter("@CostCenterID", DBNull.Value);
                            }
                            else
                            {
                                sqlparams[4] = new SqlParameter("@CostCenterID", f_CostCenterID.Value);
                            }

                        }
                        catch
                        {
                            sqlparams[4] = new SqlParameter("@CostCenterID", CostCenterID);
                        }
                    } //customCostCenter end
                    else
                        sqlparams[4] = new SqlParameter("@CostCenterID", CostCenterID);

                }
                else
                {
                    sqlparams[4] = new SqlParameter("@CostCenterID", DBNull.Value);
                }

                sqlparams[5] = new SqlParameter("@ConversionRate", f_ConversionRate);
                sqlparams[6] = new SqlParameter("@CreatedBy", f_CreatedBy);
                sqlparams[7] = new SqlParameter("@CreateDate", f_CreateDate);
                sqlparams[8] = new SqlParameter("@CreatedByValue", f_CreatedByValue);
                sqlparams[9] = new SqlParameter("@SerialNo", f_SerialNo);
                sqlparams[10] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[11] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[12] = new SqlParameter("@TransactionType", f_TransactionType);
                sqlparams[13] = new SqlParameter("@Status", f_Status);
                sqlparams[14] = new SqlParameter("@AmountFC", f_AmountFC);
                sqlparams[15] = new SqlParameter("@Amount", f_Amount);
                sqlparams[16] = new SqlParameter("@ChartOfAccountID", f_ChartOfAccountID);

                if (f_SourceHeaderID.HasValue)
                    sqlparams[17] = new SqlParameter("@SourceHeaderID", f_SourceHeaderID.Value);
                else
                    sqlparams[17] = new SqlParameter("@SourceHeaderID", DBNull.Value);

                if (f_SourceDetailID.HasValue)
                    sqlparams[18] = new SqlParameter("@SourceDetailID", f_SourceDetailID.Value);
                else
                    sqlparams[18] = new SqlParameter("@SourceDetailID", DBNull.Value);

                if (f_DepartmentID.HasValue)
                    sqlparams[19] = new SqlParameter("@DepartmentID", f_DepartmentID.Value);
                else
                    sqlparams[19] = new SqlParameter("@DepartmentID", DBNull.Value);

                if (f_IncurredOnID.HasValue)
                    sqlparams[20] = new SqlParameter("@IncurredOnID", f_IncurredOnID.Value);
                else
                    sqlparams[20] = new SqlParameter("@IncurredOnID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_Narration))
                    sqlparams[21] = new SqlParameter("@Narration", f_Narration);
                else
                    sqlparams[21] = new SqlParameter("@Narration", DBNull.Value);

                if (f_FiscalID.HasValue)
                    sqlparams[22] = new SqlParameter("@FiscalID", f_FiscalID.Value);
                else
                    sqlparams[22] = new SqlParameter("@FiscalID", DBNull.Value);

                sb.Append(DatabaseName);

                const string Query_InsertPF = @" INSERT INTO {0}.dbo.[GL_TransactionDetail] ([TransactionID], [CodeCombinationID], [CurrencyID], [DocumentReference], [CostCenterID], [ConversionRate], [CreatedBy], [CreateDate], [CreatedByValue], [SerialNo], [PeriodID], [CompanyID], [TransactionType], [Status], [AmountFC], [Amount], [ChartOfAccountID], [SourceHeaderID], [SourceDetailID], [DepartmentID], [IncurredOnID], [Narration], [FiscalID])
		VALUES(@TransactionID, @CodeCombinationID, @CurrencyID, @DocumentReference, @CostCenterID, @ConversionRate, @CreatedBy, @CreateDate, @CreatedByValue, @SerialNo, @PeriodID, @CompanyID, @TransactionType, @Status, @AmountFC, @Amount, @ChartOfAccountID, @SourceHeaderID, @SourceDetailID, @DepartmentID, @IncurredOnID, @Narration, @FiscalID) ";


                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, string.Format(Query_InsertPF, sb.ToString()), sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, string.Format(Query_InsertPF, sb.ToString()), sqlparams) > 0)
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

        public bool InsertNew(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[23];
                sqlparams[0] = new SqlParameter("@TransactionID", f_TransactionID);
                sqlparams[1] = new SqlParameter("@CodeCombinationID", f_CodeCombinationID);
                sqlparams[2] = new SqlParameter("@CurrencyID", f_CurrencyID);

                if (!string.IsNullOrEmpty(f_DocumentReference))
                    sqlparams[3] = new SqlParameter("@DocumentReference", f_DocumentReference);
                else
                    sqlparams[3] = new SqlParameter("@DocumentReference", DBNull.Value);

                if (f_CostCenterID.HasValue)
                {
                    int BSCostCenterID = 0, AccountTypeID = 0, BankCostCentreID = 0;

                    bool customCostCenter = Get_BSCostCentreID_Parameter_GL();
                    bool customCostCenterNew = Get_BSCostCentreID_Parameter_GLNew();
                    if (customCostCenterNew)
                    {
                        BSCostCenterID = SET_CostCenter.GetBSCostCenter(f_CostCenterID.Value);
                        try
                        {
                            AccountTypeID = GL_TransactionDetail.GetGL_CodeCombinationDetailAccountTypeID(f_CompanyID, f_CodeCombinationID);
                            //GL_TransactionHeader.GetAllData(" transactionid = "
                            if (AccountTypeID == 9 || AccountTypeID == 8 || AccountTypeID == 10)
                            {



                                // GL_TransactionHeader g = new GL_TransactionHeader(f_CompanyID, f_PeriodID, f_TransactionID);

                                BSCostCenterID = SET_CostCenter.GetBSCostCenter(f_CostCenterID.Value);

                                try { BankCostCentreID = GetGL_CodeCombinationDetailBankAccountCostCentreID(f_CompanyID, f_CodeCombinationID); }
                                catch { }


                                if (BankCostCentreID != 0)
                                    sqlparams[4] = new SqlParameter("@CostCenterID", BankCostCentreID);
                                else
                                    sqlparams[4] = new SqlParameter("@CostCenterID", BSCostCenterID);





                            }
                            if (BSCostCenterID == 0)
                            {
                                sqlparams[4] = new SqlParameter("@CostCenterID", DBNull.Value);
                            }
                            else
                            {
                                sqlparams[4] = new SqlParameter("@CostCenterID", f_CostCenterID.Value);
                            }

                        }
                        catch
                        {
                            sqlparams[4] = new SqlParameter("@CostCenterID", CostCenterID);
                        }
                    } //customCostCenter end
                    else
                        sqlparams[4] = new SqlParameter("@CostCenterID", CostCenterID);

                }
                else
                {
                    sqlparams[4] = new SqlParameter("@CostCenterID", DBNull.Value);
                }

                sqlparams[5] = new SqlParameter("@ConversionRate", f_ConversionRate);
                sqlparams[6] = new SqlParameter("@CreatedBy", f_CreatedBy);
                sqlparams[7] = new SqlParameter("@CreateDate", f_CreateDate);
                sqlparams[8] = new SqlParameter("@CreatedByValue", f_CreatedByValue);
                sqlparams[9] = new SqlParameter("@SerialNo", f_SerialNo);
                sqlparams[10] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[11] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[12] = new SqlParameter("@TransactionType", f_TransactionType);
                sqlparams[13] = new SqlParameter("@Status", f_Status);
                sqlparams[14] = new SqlParameter("@AmountFC", f_AmountFC);
                sqlparams[15] = new SqlParameter("@Amount", f_Amount);
                sqlparams[16] = new SqlParameter("@ChartOfAccountID", f_ChartOfAccountID);

                if (f_SourceHeaderID.HasValue)
                    sqlparams[17] = new SqlParameter("@SourceHeaderID", f_SourceHeaderID.Value);
                else
                    sqlparams[17] = new SqlParameter("@SourceHeaderID", DBNull.Value);

                if (f_SourceDetailID.HasValue)
                    sqlparams[18] = new SqlParameter("@SourceDetailID", f_SourceDetailID.Value);
                else
                    sqlparams[18] = new SqlParameter("@SourceDetailID", DBNull.Value);

                if (f_DepartmentID.HasValue)
                    sqlparams[19] = new SqlParameter("@DepartmentID", f_DepartmentID.Value);
                else
                    sqlparams[19] = new SqlParameter("@DepartmentID", DBNull.Value);

                if (f_IncurredOnID.HasValue)
                    sqlparams[20] = new SqlParameter("@IncurredOnID", f_IncurredOnID.Value);
                else
                    sqlparams[20] = new SqlParameter("@IncurredOnID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_Narration))
                    sqlparams[21] = new SqlParameter("@Narration", f_Narration);
                else
                    sqlparams[21] = new SqlParameter("@Narration", DBNull.Value);

                if (f_FiscalID.HasValue)
                    sqlparams[22] = new SqlParameter("@FiscalID", f_FiscalID.Value);
                else
                    sqlparams[22] = new SqlParameter("@FiscalID", DBNull.Value);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Insert, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Insert, sqlparams) > 0)
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

        public DataTable RecalculateControlDebitCredit(int companyID, int periodID, int glTransactionID, SqlTransaction sqltrans)
        {
            DataTable dtToReturn = null;

            string Query_RecalculateControlDebitCredit = @" select Distinct 
(
    select Sum(glDetail.Amount) from GL_TransactionDetail glDetail 
    where glDetail.TransactionID = GL_TransactionDetail.TransactionID and glDetail.CompanyID = GL_TransactionDetail.CompanyID 
        and glDetail.PeriodID = GL_TransactionDetail.PeriodID and glDetail.TransactionType = 'Debit'
) as ControlDebit, 
(
    select Sum(glDetailCredit.Amount) from GL_TransactionDetail glDetailCredit 
    where glDetailCredit.TransactionID = GL_TransactionDetail.TransactionID and glDetailCredit.CompanyID = GL_TransactionDetail.CompanyID 
        and glDetailCredit.PeriodID = GL_TransactionDetail.PeriodID and glDetailCredit.TransactionType = 'Credit'
) as ControlCredit 
from GL_TransactionDetail 
where TransactionID = @TransactionID and CompanyID = @CompanyID and PeriodID = @PeriodID ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@TransactionID", glTransactionID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query_RecalculateControlDebitCredit, sqlparams);

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

        public bool UpdateNewNetPayableForGrowerGL(int companyID, int periodID, int transactionID, decimal amount, string ccidWhere, SqlTransaction objTransaction)
        {
            bool result = false;

            string Query_UpdateNewNetPayableForGrowerGL = @" Update GL_TransactionDetail Set Amount = @Amount, AmountFC = @Amount 
where TransactionID = @TransactionID and CompanyID = @CompanyID and PeriodID = @PeriodID 
	{0} ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TransactionID", transactionID);
                sqlparams[3] = new SqlParameter("@Amount", amount);

                if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, string.Format(Query_UpdateNewNetPayableForGrowerGL, ccidWhere), sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public bool DeleteDataBy_Code(SqlTransaction objTransaction)
        {
            bool result = false;

            string Query_DeleteDetail_ByCode = @" Declare @AmountBalance decimal(18, 0) = 0;

Select @AmountBalance = (Amount - @Amount) From GL_TransactionDetail 
WHERE CompanyID = @CompanyID AND PeriodID = @PeriodID AND TransactionID = @TransactionID AND CodeCombinationID = @CodeCombinationID 
    AND TransactionType = @TransactionType;

if(@AmountBalance > 0) 
	Update GL_TransactionDetail Set Amount = Amount - @Amount, AmountFC = AmountFC - @Amount 
    WHERE CompanyID = @CompanyID AND PeriodID = @PeriodID AND TransactionID = @TransactionID AND CodeCombinationID = @CodeCombinationID 
        AND TransactionType = @TransactionType;
else
	DELETE FROM GL_TransactionDetail 
    WHERE CompanyID = @CompanyID AND PeriodID = @PeriodID AND TransactionID = @TransactionID AND CodeCombinationID = @CodeCombinationID 
        AND TransactionType = @TransactionType
        and GL_TransactionDetail.TransactionID not in (select distinct GL_TransactionHeaderID from Cane_CPR); ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[2] = new SqlParameter("@TransactionID", f_TransactionID);
                sqlparams[3] = new SqlParameter("@CodeCombinationID", f_CodeCombinationID);
                sqlparams[4] = new SqlParameter("@TransactionType", f_TransactionType);
                sqlparams[5] = new SqlParameter("@Amount", f_Amount);

                SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DeleteDetail_ByCode, sqlparams);
                result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataTable GetTrialBalanceDataYearToDate(int costcenter, int department, string companyID, int chartOfAccountID, int periodID, int fromDocumentType,
            int toDocumentType, Guid userID, DateTime? dateFrom = null, DateTime? dateTo = null, string ReportType = null, bool? chk1 = null, bool? chk2 = null,
            bool? chk3 = null, bool? chk4 = null, Nullable<short> documentGroupID = null, bool discardReversedDocuments = false,
            bool documentTypeAuthentication = false, short noOfSegments = 2, char[] delimeter = null, bool filterZeroResults = false
            , string FromAccountCode = "", string ToAccountCode = "", DateTime? fiscalYearStarted = null)
        {
            DataTable dtToReturn = null;
            string qry_docuTypeAuthenticate = string.Empty;
            StringBuilder debitCreditFilter = new StringBuilder();
            StringBuilder additionalFilter = new StringBuilder();
            StringBuilder additionalFilter2 = new StringBuilder();

            try
            {
                #region Query Parameters

                List<SqlParameter> sqlparams = new List<SqlParameter>();
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));
                sqlparams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlparams.Add(new SqlParameter("@ToDate", dateTo));

                if (costcenter == -1)
                    sqlparams.Add(new SqlParameter("@cc", DBNull.Value));
                else
                    sqlparams.Add(new SqlParameter("@cc", costcenter));

                if (department == -1)
                    sqlparams.Add(new SqlParameter("@did", DBNull.Value));
                else
                    sqlparams.Add(new SqlParameter("@did", department));

                #endregion

                if (ReportType.Equals("TrialBalanceReport"))
                    sqlparams.Add(new SqlParameter("@DetailPostingAllowedFlag", DBNull.Value));
                else if (ReportType.Equals("DetailTrialBalanceReport"))
                {
                    #region

                    sqlparams.Add(new SqlParameter("@DetailPostingAllowedFlag", DBNull.Value));

                    if (chk1.Value)
                        debitCreditFilter.AppendLine(" having SUM(Debit) <> 0 and SUM(credit) <> 0 ");

                    if (chk2.Value)
                        debitCreditFilter.AppendLine("having SUM(Debit)  = 0 and SUM(credit) = 0");

                    if (chk3.Value)
                    {
                        debitCreditFilter.AppendLine("having (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) > 0 THEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END) -  (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) < 0 THEN ABS((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END)=0 ");
                    }

                    if (chk4.Value)
                    {
                        debitCreditFilter.AppendLine(" having (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) > 0 THEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END) -  (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) < 0 THEN ABS((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END) <> 0 ");
                    }

                    #endregion
                }
                else if (ReportType.Equals("DetailTrialBalanceYearToDateReport"))
                {
                    #region

                    sqlparams.Add(new SqlParameter("@DetailPostingAllowedFlag", DBNull.Value));

                    if (chk1 == true)
                        debitCreditFilter.AppendLine(" having SUM(Debit) <> 0 and SUM(credit) <> 0 ");

                    if (chk2 == true)
                        debitCreditFilter.AppendLine("having SUM(Debit)  = 0 and SUM(credit) = 0");

                    if (chk3 == true)
                    {
                        debitCreditFilter.AppendLine("having (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) > 0 THEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END) -  (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) < 0 THEN ABS((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END)=0 ");
                    }

                    if (chk4 == true)
                    {
                        debitCreditFilter.AppendLine(" having (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) > 0 THEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END) -  (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) < 0 THEN ABS((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END) <> 0 ");
                    }

                    #endregion
                }
                else if (ReportType.Equals("LedgerWiseTrialBalanceReport"))
                    sqlparams.Add(new SqlParameter("@DetailPostingAllowedFlag", true));

                if (documentGroupID.HasValue)
                    additionalFilter.Append(" AND Set_DocumentType.GroupID = " + documentGroupID);

                if (fromDocumentType != -1 && toDocumentType != -1)
                    additionalFilter.Append(" AND GL_TransactionHeader.DocumentTypeID between " + fromDocumentType + " AND " + toDocumentType + " ");

                if (discardReversedDocuments)
                    additionalFilter.Append(" AND IsNull(GL_TransactionHeader.IsReversed, 0) = 0 And GL_TransactionHeader.ReversalID is null ");

                if (documentTypeAuthentication)
                {
                    sqlparams.Add(new SqlParameter("@UserID", userID));
                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID  ";
                }

                if (FromAccountCode != "" && ToAccountCode != "")
                {
                    additionalFilter.Append(" AND GL_TransactionDetail.CodeCombinationID between '" + FromAccountCode + "' and '" + ToAccountCode + "'");
                    additionalFilter2.Append(" AND GL_CodeCombinationDetail.CodeCombinationID between '" + FromAccountCode + "' and '" + ToAccountCode + "'");

                }

                if (fiscalYearStarted.HasValue)
                    sqlparams.Add(new SqlParameter("@fiscalYearStarted", fiscalYearStarted.Value));


                string query = FnI.QueryResourceLibrary.GL.GL_CodeCombinationDetail.QueryTrialBalanceYearToDateReport.ToString();

                companyID = "(" + companyID + ")";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query.ToString().Replace("@CompanyID", companyID), debitCreditFilter.ToString(), additionalFilter.ToString(),
                    qry_docuTypeAuthenticate, additionalFilter2.ToString()), sqlparams.ToArray());

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];


                #region Filter Zero Results

                if (filterZeroResults)
                {
                    DataTable dt_Filtered = dtToReturn.Clone();
                    List<string> lst_AccountCodes = dtToReturn.AsEnumerable()
                        .Where(x => x.Field<string>("CodeCombinationID").Split(delimeter).Length == noOfSegments)
                        .Select(x => x.Field<string>("CodeCombinationID")).ToList();

                    foreach (string codeCombinationID in lst_AccountCodes)
                    {
                        //decimal debitAmount = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").StartsWith(codeCombinationID))
                        //    .Sum(x => x.Field<decimal>("Debit"));

                        //decimal creditAmount = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").StartsWith(codeCombinationID))
                        //    .Sum(x => x.Field<decimal>("Credit"));

                        //if (debitAmount > 0 || creditAmount > 0)

                        decimal Balance = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").StartsWith(codeCombinationID))
                            .Sum(x => x.Field<decimal>("Balance"));

                        decimal OpeningBal = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").StartsWith(codeCombinationID))
                            .Sum(x => x.Field<decimal>("OpeningBalance"));

                        decimal ActivityBal = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").StartsWith(codeCombinationID))
                            .Sum(x => x.Field<decimal>("ActivityBalance"));

                        decimal yearToDateBal = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").StartsWith(codeCombinationID))
                            .Sum(x => x.Field<decimal>("yearTodateBalance"));


                        if (Math.Abs(OpeningBal) > 0 || Math.Abs(Balance) > 0 || Math.Abs(ActivityBal) > 0 || Math.Abs(yearToDateBal) > 0)
                        {
                            DataRow[] drs_ToImport = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").StartsWith(codeCombinationID))
                                .ToArray();

                            foreach (DataRow dr_ToImport in drs_ToImport)
                                dt_Filtered.ImportRow(dr_ToImport);

                            dt_Filtered.AcceptChanges();
                        }
                    }

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

        public static int GetGL_CodeCombinationDetailAccountTypeID(int CompanyID, string CodeCombinationID)
        {
            int AccountTypeID = 0;

            SqlParameter[] sqlparams = new SqlParameter[2];
            sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
            sqlparams[1] = new SqlParameter("@CodeCombinationID", CodeCombinationID);


            const string query = @" select top 1 AccountTypeID from GL_CodeCombinationDetail 
                                    where CodeCombinationID = @CodeCombinationID 
                                        and CompanyID = @CompanyID";

            try
            {
                AccountTypeID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, query, sqlparams.ToArray()));
            }
            catch
            {
                AccountTypeID = 0;
            }

            return AccountTypeID;
        }

        public static int GetGL_CodeCombinationDetailAccountTypeIDForPF(int CompanyID, string CodeCombinationID, string DatabaseName)
        {
            int AccountTypeID = 0;
            StringBuilder sb = new StringBuilder();
            SqlParameter[] sqlparams = new SqlParameter[2];
            sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
            sqlparams[1] = new SqlParameter("@CodeCombinationID", CodeCombinationID);


            const string queryPF = @" select top 1 AccountTypeID from {0}.dbo.GL_CodeCombinationDetail 
                                    where CodeCombinationID = @CodeCombinationID 
                                        and CompanyID = @CompanyID";

            sb.Append(DatabaseName);

            try
            {
                AccountTypeID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, string.Format(queryPF, sb.ToString()), sqlparams.ToArray()));
            }
            catch
            {
                AccountTypeID = 0;
            }

            return AccountTypeID;
        }

        public static int GetGL_CodeCombinationDetailBankAccountCostCentreID(int CompanyID, string CodeCombinationID)
        {
            int AccountTypeID = 0;

            SqlParameter[] sqlparams = new SqlParameter[2];
            sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
            sqlparams[1] = new SqlParameter("@CodeCombinationID", CodeCombinationID);


            const string query = @" select top 1 AR_SET_BankAccount.CostCentreID from 
                dbo.AR_SET_BankAccount,GL_CodeCombinationDetail
                WHERE AR_SET_BankAccount.PaymentCCID = GL_CodeCombinationDetail.CodeCombinationID and   GL_CodeCombinationDetail.CodeCombinationID = @CodeCombinationID 
--and GL_CodeCombinationDetail.CompanyID =  @CompanyID
";

            try
            {
                AccountTypeID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, query, sqlparams.ToArray()));
            }
            catch
            {
                AccountTypeID = 0;
            }

            return AccountTypeID;
        }

        public static bool Get_BSCostCentreID_Parameter_GL()
        {
            bool isBSCostCentreAllowed = false;

            DataTable dt = SET_Parameter.GetDataByFieldNameDescription("BSCostCenterID");
            if (dt != null && dt.Rows.Count > 0)
            {
                isBSCostCentreAllowed = dt.Rows[0]["FieldValue1"].ToString() != "" ? Convert.ToBoolean(dt.Rows[0]["FieldValue1"]) : false;
            }
            else
            {
                isBSCostCentreAllowed = false;

            }
            return isBSCostCentreAllowed;
        }

        public static bool Get_BSCostCentreID_Parameter_GLNew()
        {
            bool isBSCostCentreAllowed = false;

            DataTable dt = SET_Parameter.GetDataByFieldNameDescription("GlClosingCostCenterWise");
            if (dt != null && dt.Rows.Count > 0)
            {
                isBSCostCentreAllowed = dt.Rows[0]["FieldValue1"].ToString() != "" ? Convert.ToBoolean(dt.Rows[0]["FieldValue1"]) : false;
            }
            else
            {
                isBSCostCentreAllowed = false;

            }
            return isBSCostCentreAllowed;
        }

        public DataTable Account_Ledger_Report_WithUnpostedDocuments(int companyID, int periodID, int chartOfAccountID, DateTime dateFrom, DateTime dateTo, string accountCodeFrom,
            string accountCodeTo, int fromDocumentType, int toDocumentType, bool reversedDocuments, Nullable<int> documentGroupID, Guid userID,
            bool documentTypeAuthentication, int? costCenterID)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            string accountCodeFilter = string.Empty;
            string qry_docuTypeAuthenticate = string.Empty;
            StringBuilder additionalFilter = new StringBuilder();
            DataTable dtToReturn = null;

            #region Query

            #endregion

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));
                if (fromDocumentType > 0)
                    sqlParams.Add(new SqlParameter("@DocumentTypeID", fromDocumentType));
                else
                    sqlParams.Add(new SqlParameter("@DocumentTypeID", DBNull.Value));

                #region Filter by Account Codes

                string CodeCombinationFilter = string.Empty;

                if (!string.IsNullOrEmpty(accountCodeFrom) && !string.IsNullOrEmpty(accountCodeTo))
                {
                    CodeCombinationFilter = " where t.codecombinationid  BETWEEN @AccountCodeFrom AND @AccountCodeTo ";

                    accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID BETWEEN @AccountCodeFrom AND @AccountCodeTo ";

                    sqlParams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                }
                else if (!string.IsNullOrEmpty(accountCodeFrom))
                {
                    CodeCombinationFilter = " where t.codecombinationid  >= @AccountCodeFrom ";

                    accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID >= @AccountCodeFrom ";
                    sqlParams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                }

                else if (!string.IsNullOrEmpty(accountCodeTo))
                {

                    CodeCombinationFilter = " where t.codecombinationid  <= @AccountCodeFrom ";


                    accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID <= @AccountCodeTo ";
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                }





                #endregion

                if (fromDocumentType != -1 && toDocumentType != -1)
                    additionalFilter.Append(" AND GL_TransactionHeader.DocumentTypeID between " + fromDocumentType + " AND " + toDocumentType);

                if (reversedDocuments)
                    additionalFilter.Append(" AND IsNull(GL_TransactionHeader.IsReversed, 0) = 0 And GL_TransactionHeader.ReversalID is null ");

                if (documentGroupID.HasValue)
                    additionalFilter.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (documentTypeAuthentication)
                {
                    sqlParams.Add(new SqlParameter("@UserID", userID));
                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                if (costCenterID.HasValue)
                    additionalFilter.Append(" AND Set_CostCenter.CostCenterID = " + costCenterID.Value.ToString() + " ");
                //Set_CostCenter.CostCenterID

                string query = FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_Account_Ledger_WithUnpostedDocuments.Replace("{AccountCodeFilter}", accountCodeFilter);

                query = query.Replace("{CodeCombinationFilter}", CodeCombinationFilter);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, additionalFilter.ToString(), qry_docuTypeAuthenticate),
                    sqlParams.ToArray());

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

        public static decimal GetSumByHeadCode(int companyID, int periodID, int chartOfAccountID, string accountCodeFrom, string accountCodeTo, Guid userID,
          Nullable<DateTime> dateto = null, int? Segment = null, Nullable<short> DocumentGroupID = null)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>(6);
            string accountCodeFilter = string.Empty;
            string DocumentGroupFilter = string.Empty;
            decimal valueToReturn = 0;

            #region Query

            string Query_GetSumBy_HeaderAccountCodes = @" 
SELECT IsNull(SUM(CASE WHEN GL_TransactionDetail.TransactionType = 'Debit' THEN Amount ELSE Amount * -1 END), 0) As SummarizedValue FROM GL_TransactionDetail 
    INNER JOIN GL_TransactionHeader ON GL_TransactionHeader.CompanyID = GL_TransactionDetail.CompanyID 
        AND GL_TransactionHeader.PeriodID = GL_TransactionDetail.PeriodID AND GL_TransactionHeader.TransactionID = GL_TransactionDetail.TransactionID 
        AND GL_TransactionHeader.Status = 1 
    INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = GL_TransactionHeader.CompanyID AND SET_DocumentType.PeriodID = GL_TransactionHeader.PeriodID 
        AND SET_DocumentType.DocumentTypeID = GL_TransactionHeader.DocumentTypeID 

{DocumentGroupID}

    INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
        AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID 
WHERE GL_TransactionDetail.CompanyID = @CompanyID AND GL_TransactionDetail.PeriodID = @PeriodID AND GL_TransactionDetail.ChartOfAccountID = @ChartOfAccountID 
    AND GL_TransactionDetail.Status = 1 And GL_TransactionHeader.StatusCode = 1 
    {AccountCodeFilter} ";

            #endregion

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));
                sqlParams.Add(new SqlParameter("@UserID", userID));

                if (Segment.HasValue && Segment.Value < 5)
                {
                    accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID  in (Select  CodeCombinationID from GL_CodeCombinationDetail Where HeadAccountCode like @AccountCodeFrom+'%')";
                    sqlParams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                    //accountCodeFilter += "AND GL_TransactionHeader.DocumentDate BETWEEN @DateFrom AND @DateTo ";
                    accountCodeFilter += "AND GL_TransactionHeader.DocumentDate <= @DateTo ";
                    sqlParams.Add(new SqlParameter("@DateFrom", "2014-12-31"));
                    sqlParams.Add(new SqlParameter("@DateTo", dateto));
                }
                else
                {

                    if (!string.IsNullOrEmpty(accountCodeFrom) && !string.IsNullOrEmpty(accountCodeTo))
                    {
                        accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID  in (Select  CodeCombinationID from GL_CodeCombinationDetail Where HeadAccountCode like @AccountCodeFrom+'%')";
                        //accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID <= @AccountCodeTo ";
                        sqlParams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                        sqlParams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                    }
                    else if (!string.IsNullOrEmpty(accountCodeFrom))
                    {
                        accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID >= @AccountCodeFrom ";
                        sqlParams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                    }
                    else if (!string.IsNullOrEmpty(accountCodeTo))
                    {
                        accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID <= @AccountCodeTo ";
                        sqlParams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                    }

                    if (dateto != null)
                    {
                        //accountCodeFilter += "AND GL_TransactionHeader.DocumentDate BETWEEN @DateFrom AND @DateTo ";
                        accountCodeFilter += "AND GL_TransactionHeader.DocumentDate <= @DateTo ";
                        //sqlParams.Add(new SqlParameter("@DateFrom", datefrom));
                        sqlParams.Add(new SqlParameter("@DateTo", dateto));
                    }



                    //else if (datefrom != null)
                    //{
                    //    accountCodeFilter += "AND GL_TransactionHeader.DocumentDate >= @DateFrom";
                    //    sqlParams.Add(new SqlParameter("@DateFrom", datefrom));
                    //}

                    else if (dateto != null)
                    {
                        accountCodeFilter += "AND GL_TransactionHeader.DocumentDate <= @DateTo";
                        sqlParams.Add(new SqlParameter("@DateTo", dateto));
                    }

                    if (DocumentGroupID.HasValue)
                        DocumentGroupFilter = " and  SET_DocumentType.GroupID = " + DocumentGroupID;

                }

                Query_GetSumBy_HeaderAccountCodes = Query_GetSumBy_HeaderAccountCodes.Replace("{DocumentGroupID}", DocumentGroupFilter);


                valueToReturn = Convert.ToDecimal(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetSumBy_HeaderAccountCodes.Replace("{AccountCodeFilter}",
                    accountCodeFilter), sqlParams.ToArray()));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return valueToReturn;
        }
        public static DataTable GetAccountCoderialBalanceData(int costcenter, int department, int companyID, int chartOfAccountID, int periodID, int fromDocumentType,
           int toDocumentType, Guid userID, DateTime? dateFrom = null, DateTime? dateTo = null, string ReportType = null, bool? chk1 = null, bool? chk2 = null,
           bool? chk3 = null, bool? chk4 = null, Nullable<short> documentGroupID = null, bool discardReversedDocuments = false,
           bool documentTypeAuthentication = false, bool filterZeroResults = false, string AccountCode = "")
        {
            DataTable dtToReturn = null;
            string qry_docuTypeAuthenticate = string.Empty;
            StringBuilder debitCreditFilter = new StringBuilder();
            StringBuilder additionalFilter = new StringBuilder();
            StringBuilder sbr3 = new StringBuilder();
            try
            {
                #region Query Parameters

                List<SqlParameter> sqlparams = new List<SqlParameter>();
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));
                sqlparams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlparams.Add(new SqlParameter("@ToDate", dateTo));

                if (costcenter == -1)
                    sqlparams.Add(new SqlParameter("@cc", DBNull.Value));
                else
                    sqlparams.Add(new SqlParameter("@cc", costcenter));

                if (department == -1)
                    sqlparams.Add(new SqlParameter("@did", DBNull.Value));
                else
                    sqlparams.Add(new SqlParameter("@did", department));

                #endregion

                if (ReportType.Equals("AccountCodeWiseTrailBalance"))
                    sqlparams.Add(new SqlParameter("@DetailPostingAllowedFlag", DBNull.Value));
                else if (ReportType.Equals("DetailTrialBalanceReport"))
                {
                    #region

                    sqlparams.Add(new SqlParameter("@DetailPostingAllowedFlag", DBNull.Value));

                    if (chk1.Value)
                        debitCreditFilter.AppendLine(" having SUM(Debit) <> 0 and SUM(credit) <> 0 ");

                    if (chk2.Value)
                        debitCreditFilter.AppendLine("having SUM(Debit)  = 0 and SUM(credit) = 0");

                    if (chk3.Value)
                    {
                        debitCreditFilter.AppendLine("having (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) > 0 THEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END) -  (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) < 0 THEN ABS((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END)=0 ");
                    }

                    if (chk4.Value)
                    {
                        debitCreditFilter.AppendLine(" having (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) > 0 THEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END) -  (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) < 0 THEN ABS((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END) <> 0 ");
                    }

                    if (AccountCode != "")
                    {
                        //     sbr3.Append(" AND Len(GL_CodeCombinationDetail.CodeCombinationID) BETWEEN  Len(@uc_COAFrom) AND Len(@uc_COATo)");
                        sbr3.Append(" AND GL_CodeCombinationDetail.CodeCombinationID like '" + AccountCode.ToString() + "%'");
                    }

                    #endregion
                }
                else if (ReportType.Equals("AccountCodeDetailTrialBalanceReportOff"))
                {
                    #region

                    sqlparams.Add(new SqlParameter("@DetailPostingAllowedFlag", DBNull.Value));

                    if (chk1 == true)
                        debitCreditFilter.AppendLine(" having SUM(Debit) <> 0 and SUM(credit) <> 0 ");

                    if (chk2 == true)
                        debitCreditFilter.AppendLine("having SUM(Debit)  = 0 and SUM(credit) = 0");

                    if (chk3 == true)
                    {
                        debitCreditFilter.AppendLine("having (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) > 0 THEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END) -  (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) < 0 THEN ABS((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END)=0 ");
                    }

                    if (chk4 == true)
                    {
                        debitCreditFilter.AppendLine(" having (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) > 0 THEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END) -  (CASE WHEN ((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) < 0 THEN ABS((SUM(OpeningDebit) + SUM(Debit)) - (SUM(OpeningCredit) + SUM(Credit))) ELSE 0 END) <> 0 ");
                    }
                    if (AccountCode != "")
                    {
                        //     sbr3.Append(" AND Len(GL_CodeCombinationDetail.CodeCombinationID) BETWEEN  Len(@uc_COAFrom) AND Len(@uc_COATo)");
                        sbr3.Append(" AND GL_CodeCombinationDetail.CodeCombinationID like '" + AccountCode.ToString() + "%'");
                    }

                    #endregion
                }
                else if (ReportType.Equals("LedgerWiseTrialBalanceReport"))
                    sqlparams.Add(new SqlParameter("@DetailPostingAllowedFlag", true));

                if (documentGroupID.HasValue)
                    additionalFilter.Append(" AND Set_DocumentType.GroupID = " + documentGroupID);

                if (fromDocumentType != -1 && toDocumentType != -1)
                    additionalFilter.Append(" AND GL_TransactionHeader.DocumentTypeID between " + fromDocumentType + " AND " + toDocumentType + " ");

                if (discardReversedDocuments)
                    additionalFilter.Append(" AND IsNull(GL_TransactionHeader.IsReversed, 0) = 0 And GL_TransactionHeader.ReversalID is null ");

                if (documentTypeAuthentication)
                {
                    sqlparams.Add(new SqlParameter("@UserID", userID));
                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                string query = FnI.QueryResourceLibrary.GL.GL_CodeCombinationDetail.Query_AccountCodeTrailbalanceNetOFF.ToString();

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, debitCreditFilter.ToString(), additionalFilter.ToString(),
                    qry_docuTypeAuthenticate, sbr3.ToString()), sqlparams.ToArray());

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
                #region Filter Zero Results

                if (filterZeroResults)
                {
                    DataTable dt_Filtered = dtToReturn.Clone();
                    List<string> lst_AccountCodes = dtToReturn.AsEnumerable()
                        // .Where(x => x.Field<string>("CodeCombinationID").Split(delimeter).Length == noOfSegments)
                        .Select(x => x.Field<string>("CodeCombinationID")).ToList();

                    foreach (string codeCombinationID in lst_AccountCodes)
                    {



                        decimal Openingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                          .Sum(x => x.Field<decimal>("OpeningDebit"));
                        decimal Openingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                        .Sum(x => x.Field<decimal>("OpeningCredit"));

                        decimal _Openingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                        .Sum(x => x.Field<decimal>("_OpeningDebit"));
                        decimal _Openingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                        .Sum(x => x.Field<decimal>("_OpeningCredit"));

                        decimal Closingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                          .Sum(x => x.Field<decimal>("ClosingDebit"));
                        decimal Closingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                         .Sum(x => x.Field<decimal>("ClosingCredit"));

                        decimal _Closingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                          .Sum(x => x.Field<decimal>("_ClosingDebit"));
                        decimal _Closingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                         .Sum(x => x.Field<decimal>("_ClosingCredit"));
                        //if (debitAmount > 0 || creditAmount > 0)

                        decimal DebitAmount = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                            .Sum(x => x.Field<decimal>("Debit"));

                        decimal CreditAmount = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                            .Sum(x => x.Field<decimal>("Credit"));

                        //decimal ActivityBal = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").StartsWith(codeCombinationID))
                        //    .Sum(x => x.Field<decimal>("ActivityBalance"));

                        //decimal yearToDateBal = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").StartsWith(codeCombinationID))
                        //    .Sum(x => x.Field<decimal>("yearTodateBalance"));


                        if (Math.Abs(Openingdebit) > 0 || Math.Abs(Openingcredit) > 0 || Math.Abs(_Openingdebit) > 0 || Math.Abs(_Openingcredit) > 0 || Math.Abs(Closingdebit) > 0 || Math.Abs(Closingcredit) > 0 || Math.Abs(_Closingdebit) > 0 || Math.Abs(_Closingcredit) > 0 || Math.Abs(DebitAmount) > 0 || Math.Abs(CreditAmount) > 0)
                        {
                            DataRow[] drs_ToImport = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                                .ToArray();

                            foreach (DataRow dr_ToImport in drs_ToImport)
                                dt_Filtered.ImportRow(dr_ToImport);

                            dt_Filtered.AcceptChanges();
                        }
                    }

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

        const string Query_GLDetail_Update_By_PrimaryKey = @" UPDATE [GL_TransactionDetail] SET [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [TransactionID] = @TransactionID ";
        public bool UpdateGLDetailDataByKey(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@TransactionID", f_TransactionID);
                sqlparams[1] = new SqlParameter("@Status", f_Status);

                if (f_UpdatedBy.HasValue)
                    sqlparams[2] = new SqlParameter("@UpdatedBy", f_UpdatedBy.Value);
                else
                    sqlparams[2] = new SqlParameter("@UpdatedBy", DBNull.Value);

                if (f_UpdateDate.HasValue)
                    sqlparams[3] = new SqlParameter("@UpdateDate", f_UpdateDate.Value);
                else
                    sqlparams[3] = new SqlParameter("@UpdateDate", DBNull.Value);

                if (!string.IsNullOrEmpty(f_UpdatedByValue))
                    sqlparams[4] = new SqlParameter("@UpdatedByValue", f_UpdatedByValue);
                else
                    sqlparams[4] = new SqlParameter("@UpdatedByValue", DBNull.Value);

                sqlparams[5] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[6] = new SqlParameter("@CompanyID", f_CompanyID);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_GLDetail_Update_By_PrimaryKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_GLDetail_Update_By_PrimaryKey, sqlparams) > 0)
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

        public static DataTable StoreMovementSummaryReport(string companies, int CurrentCompanyID, int periodID, int CurrentFiscalID, DateTime AsOnDate, int? DocumentGroupID = null)
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

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_StoreMovementSummary.Replace("@CompanyID", "(" + companies + ")") + sb.ToString(), sqlparams);

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

        public static bool DeletePermenantDataByKeyTranasctionID(SqlTransaction objTransaction, int companyID, int periodID, int transactionID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TransactionID", transactionID);
                sqlparams[3] = new SqlParameter("@Status", false);


                const string Query_Delete_PermenantDataByKey = @" Delete from  GL_TransactionDetail Where TransactionID=@TransactionID AND CompanyID=@CompanyID AND PeriodID=@PeriodID";

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_PermenantDataByKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_PermenantDataByKey, sqlparams) > 0)
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

        public static DataTable GetAmountsForDashBoard(int CurrentCompanyID, int periodID, int CurrentFiscalID)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            //const string Query = FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_GetAmountsForDashBoard;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", CurrentCompanyID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (CurrentFiscalID > 0)
                    sb.Append(" and GL_TransactionHeader.FiscalID = @CurrentFiscalID ");

                //DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_StoreMovementSummary.Replace("@CompanyID", "(" + companies + ")") + sb.ToString(), sqlparams);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_GetAmountsForDashBoard, sb.ToString()), sqlparams);

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



        public static DataTable Get_ExpansesOfYear(Nullable<int> companyID, int periodID, Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null, int FiscalID = 0, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", FiscalID);



                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_ExpansesOfYears), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_ExpansesOfYears), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_ExpansesOfYears), sqlparams);

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


        #endregion

        public static DataTable GetTrialBalanceDataForFinancialFormMonthWise(int costcenter, int department, string companyID, int chartOfAccountID, int periodID, int fromDocumentType,
      int toDocumentType, Guid userID, DateTime? datefrom = null, DateTime? dateTo = null, string ReportType = null, bool? chk1 = null, bool? chk2 = null,
      bool? chk3 = null, bool? chk4 = null, Nullable<short> documentGroupID = null, bool discardReversedDocuments = false,
      bool documentTypeAuthentication = false, bool filterZeroResults = false, string chartofAccountFrom = "", string chartofAccountTo = "", int? FinancialHeaderID = null, int? segment = null)
        {
            DataTable dtToReturn = null;
            string qry_docuTypeAuthenticate = string.Empty;
            StringBuilder debitCreditFilter = new StringBuilder();
            StringBuilder additionalFilter = new StringBuilder();

            try
            {
                #region Query Parameters

                List<SqlParameter> sqlparams = new List<SqlParameter>();
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));
                //sqlparams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlparams.Add(new SqlParameter("@FromDate", datefrom));
                sqlparams.Add(new SqlParameter("@ToDate", dateTo));
                sqlparams.Add(new SqlParameter("@FinancialHeaderID", FinancialHeaderID.Value));

                sqlparams.Add(new SqlParameter("@chartofAccountFrom", chartofAccountFrom));
                sqlparams.Add(new SqlParameter("@chartofAccountTo", chartofAccountTo));
                sqlparams.Add(new SqlParameter("@UserID", userID));

                #endregion


                string queryDetail = FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_FinancialDetailMonthWise;
                string codefromfilter = string.Empty;

                if (chartofAccountFrom != "" && chartofAccountTo != "")// && !segment.HasValue)
                    codefromfilter = "and GL_CodeCombinationDetail.CodeCombinationID between @chartofAccountFrom and @chartofAccountTo ";
                else if (chartofAccountFrom != "")// && chartofAccountTo != "" && segment.HasValue)
                    codefromfilter = "and GL_CodeCombinationDetail.CodeCombinationID like '" + chartofAccountFrom + "%'";

                if (documentGroupID.HasValue)
                    additionalFilter.Append(" AND Set_DocumentType.GroupID = " + documentGroupID);

                string query = string.Format(queryDetail, codefromfilter, additionalFilter.ToString());



                companyID = "(" + companyID + ")";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query.Replace("@CompanyID", companyID)), sqlparams.ToArray());

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];


                #region Filter Zero Results

                if (filterZeroResults)
                {
                    DataTable dt_Filtered = dtToReturn.Clone();
                    List<string> lst_AccountCodes = dtToReturn.AsEnumerable()
                        // .Where(x => x.Field<string>("CodeCombinationID").Split(delimeter).Length == noOfSegments)
                        .Select(x => x.Field<string>("CodeCombinationID")).ToList();

                    foreach (string codeCombinationID in lst_AccountCodes)
                    {



                        decimal Openingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                          .Sum(x => x.Field<decimal>("OpeningDebit"));
                        decimal Openingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                        .Sum(x => x.Field<decimal>("OpeningCredit"));

                        decimal _Openingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                        .Sum(x => x.Field<decimal>("_OpeningDebit"));
                        decimal _Openingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                        .Sum(x => x.Field<decimal>("_OpeningCredit"));

                        decimal Closingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                          .Sum(x => x.Field<decimal>("ClosingDebit"));
                        decimal Closingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                         .Sum(x => x.Field<decimal>("ClosingCredit"));

                        decimal _Closingdebit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                          .Sum(x => x.Field<decimal>("_ClosingDebit"));
                        decimal _Closingcredit = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                         .Sum(x => x.Field<decimal>("_ClosingCredit"));
                        //if (debitAmount > 0 || creditAmount > 0)

                        decimal DebitAmount = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                            .Sum(x => x.Field<decimal>("Debit"));

                        decimal CreditAmount = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                            .Sum(x => x.Field<decimal>("Credit"));

                        //decimal ActivityBal = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").StartsWith(codeCombinationID))
                        //    .Sum(x => x.Field<decimal>("ActivityBalance"));

                        //decimal yearToDateBal = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").StartsWith(codeCombinationID))
                        //    .Sum(x => x.Field<decimal>("yearTodateBalance"));


                        if (Math.Abs(Openingdebit) > 0 || Math.Abs(Openingcredit) > 0 || Math.Abs(_Openingdebit) > 0 || Math.Abs(_Openingcredit) > 0 || Math.Abs(Closingdebit) > 0 || Math.Abs(Closingcredit) > 0 || Math.Abs(_Closingdebit) > 0 || Math.Abs(_Closingcredit) > 0 || Math.Abs(DebitAmount) > 0 || Math.Abs(CreditAmount) > 0)
                        {
                            DataRow[] drs_ToImport = dtToReturn.AsEnumerable().Where(x => x.Field<string>("CodeCombinationID").Equals(codeCombinationID))
                                .ToArray();

                            foreach (DataRow dr_ToImport in drs_ToImport)
                                dt_Filtered.ImportRow(dr_ToImport);

                            dt_Filtered.AcceptChanges();
                        }
                    }

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

        public static decimal GetSum(string companyID, int periodID, int chartOfAccountID, string accountCodeFrom, string accountCodeTo, Guid userID,
                  Nullable<DateTime> datefrom = null, Nullable<DateTime> dateto = null, int? Segment = null, Nullable<short> DocumentGroupID = null,
                    Nullable<bool> Withoutclosing = null,
            bool WithoutClosing_New = false,
            string fiscalId = "", int? CCID = null)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>(6);
            string accountCodeFilter = string.Empty;
            string DocumentGroupFilter = string.Empty;
            decimal valueToReturn = 0;
            string withoutClosingFilter = string.Empty;

            #region Query

            string Query_GetSumBy_AccountCodes = @" 
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED
SELECT IsNull(SUM(CASE WHEN GL_TransactionDetail.TransactionType = 'Debit' THEN Amount ELSE Amount * -1 END), 0) As SummarizedValue FROM GL_TransactionDetail 
    INNER JOIN GL_TransactionHeader ON GL_TransactionHeader.CompanyID = GL_TransactionDetail.CompanyID 
        AND GL_TransactionHeader.PeriodID = GL_TransactionDetail.PeriodID AND GL_TransactionHeader.TransactionID = GL_TransactionDetail.TransactionID 
        AND GL_TransactionHeader.Status = 1 
    INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = GL_TransactionHeader.CompanyID AND SET_DocumentType.PeriodID = GL_TransactionHeader.PeriodID 
        AND SET_DocumentType.DocumentTypeID = GL_TransactionHeader.DocumentTypeID 

{DocumentGroupID}

    --INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
        --AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID 

WHERE GL_TransactionDetail.CompanyID in @CompanyID AND GL_TransactionDetail.PeriodID = @PeriodID AND GL_TransactionDetail.ChartOfAccountID = @ChartOfAccountID 
    AND GL_TransactionDetail.Status = 1 And GL_TransactionHeader.StatusCode = 1 
    {AccountCodeFilter} 
    {WithoutClosingFilter}
";

            if (WithoutClosing_New)
            {
                withoutClosingFilter = @"
	 AND GL_TransactionHeader.TransactionID not in (
		
		select GL.TransactionID  
		from GL_TransactionHeader GL 
		inner join Set_DocumentType Doctype on Doctype.DocumentTypeID = GL.DocumentTypeID
		and Doctype.CompanyID = GL.CompanyID

		inner join SET_PeriodCustom on SET_PeriodCustom.CompanyID = GL .CompanyID
		and SET_PeriodCustom.FiscalId IN @FiscalID

		where GL.CompanyID  = GL_TransactionHeader.CompanyID
		and GL.PeriodID = GL_TransactionHeader.PeriodID
		and GL.FiscalID  IN @FiscalID
		and Doctype.DocumentType = 'GL-OPENING'
		and GL.DocumentDate = SET_PeriodCustom.EndDate
		)
";
            }

            #endregion

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));
                sqlParams.Add(new SqlParameter("@UserID", userID));
                sqlParams.Add(new SqlParameter("@FiscalID", fiscalId));

                //if (Segment.HasValue && Segment.Value < 5)
                //{
                //    accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID like @AccountCodeFrom+'%'";
                //    sqlParams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                //    //accountCodeFilter += "AND GL_TransactionHeader.DocumentDate BETWEEN @DateFrom AND @DateTo ";
                //    accountCodeFilter += "AND GL_TransactionHeader.DocumentDate <= @DateTo ";
                //    //  sqlParams.Add(new SqlParameter("@DateFrom", "2014-12-31"));
                //    sqlParams.Add(new SqlParameter("@DateFrom", datefrom));
                //    sqlParams.Add(new SqlParameter("@DateTo", dateto));
                //}
                //else
                //{
                fiscalId = "(" + fiscalId + ")";
                if (!string.IsNullOrEmpty(accountCodeFrom) && !string.IsNullOrEmpty(accountCodeTo))
                {
                    accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID BETWEEN @AccountCodeFrom AND @AccountCodeTo ";
                    //accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID <= @AccountCodeTo ";
                    sqlParams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                }
                else if (!string.IsNullOrEmpty(accountCodeFrom))
                {
                    accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID >= @AccountCodeFrom ";
                    sqlParams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                }
                else if (!string.IsNullOrEmpty(accountCodeTo))
                {
                    accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID <= @AccountCodeTo ";
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                }

                if (dateto != null && datefrom != null)
                {
                    accountCodeFilter += "AND GL_TransactionHeader.DocumentDate BETWEEN @DateFrom AND @DateTo ";
                    //accountCodeFilter += "AND GL_TransactionHeader.DocumentDate <= @DateTo ";
                    //sqlParams.Add(new SqlParameter("@DateFrom", datefrom));
                    sqlParams.Add(new SqlParameter("@DateFrom", datefrom));
                    sqlParams.Add(new SqlParameter("@DateTo", dateto));
                }



                //else if (datefrom != null)
                //{
                //    accountCodeFilter += "AND GL_TransactionHeader.DocumentDate >= @DateFrom";
                //    sqlParams.Add(new SqlParameter("@DateFrom", datefrom));
                //}

                else if (dateto != null)
                {
                    accountCodeFilter += "AND GL_TransactionHeader.DocumentDate <= @DateTo";
                    sqlParams.Add(new SqlParameter("@DateTo", dateto));
                }

                if (DocumentGroupID.HasValue)
                    DocumentGroupFilter = " and  SET_DocumentType.GroupID = " + DocumentGroupID;

                if (Withoutclosing.HasValue)
                    DocumentGroupFilter = " and  SET_DocumentType.DocumentType  not like  'GL-OPENING'";

                if (CCID.HasValue)
                    accountCodeFilter += " AND GL_TransactionDetail.CostCenterID =" + CCID;

                // }

                companyID = "(" + companyID + ")";
                Query_GetSumBy_AccountCodes = Query_GetSumBy_AccountCodes.Replace("{DocumentGroupID}", DocumentGroupFilter);
                Query_GetSumBy_AccountCodes = Query_GetSumBy_AccountCodes.Replace("@CompanyID", companyID);


                valueToReturn = Convert.ToDecimal(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetSumBy_AccountCodes.Replace("{AccountCodeFilter}",
                    accountCodeFilter).Replace("{WithoutClosingFilter}", withoutClosingFilter).Replace("@FiscalID", fiscalId).Replace("@CompanyID", companyID), sqlParams.ToArray()));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return valueToReturn;
        }


        public DataTable CashExpenseReport(string companyID, int periodID, int chartOfAccountID, DateTime dateFrom, DateTime dateTo, string accountCodeFrom,
           string accountCodeTo, int fromDocumentType, int toDocumentType, bool reversedDocuments, Nullable<int> documentGroupID, Guid userID,
           bool documentTypeAuthentication, int? costCenterID)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            string accountCodeFilter = string.Empty;
            string qry_docuTypeAuthenticate = string.Empty;
            StringBuilder additionalFilter = new StringBuilder();
            DataTable dtToReturn = null;

            #region Query

            #endregion

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));

                #region Filter by Account Codes

                //if (!string.IsNullOrEmpty(accountCodeFrom) && !string.IsNullOrEmpty(accountCodeTo))
                //{
                //    accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID BETWEEN @AccountCodeFrom AND @AccountCodeTo ";
                //    sqlParams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                //    sqlParams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                //}
                //else if (!string.IsNullOrEmpty(accountCodeFrom))
                //{
                //    accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID >= @AccountCodeFrom ";
                //    sqlParams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                //}
                //else if (!string.IsNullOrEmpty(accountCodeTo))
                //{
                //    accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID <= @AccountCodeTo ";
                //    sqlParams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                //}

                #endregion

                if (fromDocumentType != -1 && toDocumentType != -1)
                    additionalFilter.Append(" AND GL_TransactionHeader.DocumentTypeID between " + fromDocumentType + " AND " + toDocumentType);

                //if (reversedDocuments)
                //    additionalFilter.Append(" AND IsNull(GL_TransactionHeader.IsReversed, 0) = 0 And GL_TransactionHeader.ReversalID is null ");

                if (documentGroupID.HasValue)
                    additionalFilter.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                //if (documentTypeAuthentication)
                //{
                //    sqlParams.Add(new SqlParameter("@UserID", userID));
                //    //                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
                //    //AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                //}

                //if (costCenterID.HasValue)
                //    additionalFilter.Append(" AND Set_CostCenter.CostCenterID = " + costCenterID.Value.ToString() + " ");


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.GL.GL_CodeCombinationDetail.Query_CashExpenseReport, additionalFilter.ToString()), sqlParams.ToArray());

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


        public DataTable CashExpenseDetailReport(string companyID, int periodID, int chartOfAccountID, DateTime dateFrom, DateTime dateTo, string accountCodeFrom,
           string accountCodeTo, int fromDocumentType, int toDocumentType, bool reversedDocuments, string documentGroupID, Guid userID,
           bool documentTypeAuthentication, int? costCenterID, int? BankID, int? BranchID, int FromAccountID, int ToAccountID, bool FillterReversedDoc = false)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            string accountCodeFilter = string.Empty;
            string qry_docuTypeAuthenticate = string.Empty;
            StringBuilder additionalFilter = new StringBuilder();
            StringBuilder BankAccountFilter = new StringBuilder();


            StringBuilder ReverseFillter_Payment = new StringBuilder();
            StringBuilder ReverseFillter_Receipt = new StringBuilder();

            DataTable dtToReturn = null;



            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));


                if (fromDocumentType != -1 && toDocumentType != -1)
                    additionalFilter.Append(" AND GL_TransactionHeader.DocumentTypeID between " + fromDocumentType + " AND " + toDocumentType);


                if (documentGroupID != "")
                    additionalFilter.Append(" AND Set_DocumentType.GroupID in " + documentGroupID + " ");

                if (BankID.HasValue)
                    additionalFilter.Append(" and AR_SET_Bank.BankID = " + BankID.Value + " ");

                if (BranchID.HasValue)
                    additionalFilter.Append(" and AR_SET_BankBranch.BankBranchID = " + BranchID.Value + " ");

                if (FillterReversedDoc)
                {
                    ReverseFillter_Payment.Append(" And AP_PaymentHeader.ReversalComments is null  ");
                    ReverseFillter_Receipt.Append(" And AR_ReceiptTransactionHeader.ReversalComments is null");

                }
                if (FromAccountID != -1 && ToAccountID != -1)
                {
                    BankAccountFilter.Append(" AND AR_SET_BankAccount.BankAccountID between " + FromAccountID + " and " + ToAccountID + " ");
                }
                else if (FromAccountID != -1 && ToAccountID == -1)
                {
                    BankAccountFilter.Append(" AND AR_SET_BankAccount.BankAccountID = " + FromAccountID + " ");
                }
                else if (FromAccountID == -1 && ToAccountID != -1)
                {
                    BankAccountFilter.Append(" AND AR_SET_BankAccount.BankAccountID = " + ToAccountID + " ");
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.GL.GL_CodeCombinationDetail.Query_CashExpenseDetailReport.Replace("@CompanyID", "(" + companyID + ")"), additionalFilter.ToString(), ReverseFillter_Payment.ToString(), ReverseFillter_Receipt.ToString(), BankAccountFilter.ToString()), sqlParams.ToArray());

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


        public DataTable ExpenseLedgerDepartmentWise(string companyID, int periodID, int chartOfAccountID, DateTime dateFrom, DateTime dateTo, string accountCodeFrom,
           string accountCodeTo, int fromDocumentType, int toDocumentType, bool reversedDocuments, string documentGroupID, Guid userID,
           bool documentTypeAuthentication, int? costCenterID, bool FillterReversedDoc = false, int DepartmentFrom = -1, int DepartmentTo = -1)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            string accountCodeFilter = string.Empty;
            string qry_docuTypeAuthenticate = string.Empty;
            StringBuilder additionalFilter = new StringBuilder();


            StringBuilder ReverseFillter_Payment = new StringBuilder();
            StringBuilder ReverseFillter_Receipt = new StringBuilder();

            DataTable dtToReturn = null;



            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));


                if (fromDocumentType != -1 && toDocumentType != -1)
                    additionalFilter.Append(" AND GL_TransactionHeader.DocumentTypeID between " + fromDocumentType + " AND " + toDocumentType);

                if (documentGroupID != "")
                    additionalFilter.Append(" AND Set_DocumentType.GroupID in " + documentGroupID + " ");

                if (FillterReversedDoc)
                {
                    ReverseFillter_Payment.Append(" And AP_PaymentHeader.ReversalComments is null  ");
                    ReverseFillter_Receipt.Append(" And AR_ReceiptTransactionHeader.ReversalComments is null");

                }

                if (DepartmentFrom != -1 && DepartmentTo != -1)
                    additionalFilter.Append(" AND GL_TransactionDetail.departmentID between " + DepartmentFrom + " AND " + DepartmentTo);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.GL.GL_CodeCombinationDetail.Query_ExpenseAccountDepartmentWise.Replace("@CompanyID", "(" + companyID + ")"), additionalFilter.ToString(), ReverseFillter_Payment.ToString(), ReverseFillter_Receipt.ToString()), sqlParams.ToArray());

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


        public static DataTable GetTrialBalanceData_Monthly(string companyID, int periodID, int chartOfAccountID, DateTime dateFrom, DateTime dateTo)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            string accountCodeFilter = string.Empty;
            string qry_docuTypeAuthenticate = string.Empty;
            StringBuilder additionalFilter = new StringBuilder();
            DataTable dtToReturn = null;



            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));



                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.GL.GL_CodeCombinationDetail.Query_TrailBalacnce_Monthly.Replace("@CompanyID", "(" + companyID + ")"), additionalFilter.ToString()), sqlParams.ToArray());

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

        public DataTable CashExpenseLedgerHSM_Report(string companyID, int periodID, int chartOfAccountID, DateTime dateFrom, DateTime dateTo, string accountCodeFrom,
           string accountCodeTo, int fromDocumentType, int toDocumentType, bool reversedDocuments, Nullable<int> documentGroupID, Guid userID,
           bool documentTypeAuthentication, int? costCenterID)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            string accountCodeFilter = string.Empty;
            string qry_docuTypeAuthenticate = string.Empty;
            StringBuilder additionalFilter = new StringBuilder();
            DataTable dtToReturn = null;



            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));


                if (fromDocumentType != -1 && toDocumentType != -1)
                    additionalFilter.Append(" AND GL_TransactionHeader.DocumentTypeID between " + fromDocumentType + " AND " + toDocumentType);


                if (documentGroupID.HasValue)
                    additionalFilter.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");



                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.GL.GL_CodeCombinationDetail.Query_CashExpenseLedgerHSM_Report.Replace("@CompanyID", "(" + companyID + ")"), additionalFilter.ToString()), sqlParams.ToArray());

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


        public DataTable Account_Ledger_ReportForFinancial(string companyID, int periodID, int chartOfAccountID, DateTime dateFrom, DateTime dateTo, string accountCodeFrom,
            string accountCodeTo, int fromDocumentType, int toDocumentType, bool reversedDocuments, string documentGroupID, Guid userID,
            bool documentTypeAuthentication, int? costCenterID, string Narration, string Particular, int FinancialHeaderID = 0, bool isCurrentYearActivity = false)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            string accountCodeFilter = string.Empty;
            string qry_docuTypeAuthenticate = string.Empty;
            StringBuilder additionalFilter = new StringBuilder();
            StringBuilder narrationFilter = new StringBuilder();
            StringBuilder particularFilter = new StringBuilder();
            DataTable dtToReturn = null;

            #region Query

            string Query_Account_Ledger = FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Account_Ledger_ReportForFinancial;

            #endregion

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));
                sqlParams.Add(new SqlParameter("@FinancialHeaderID", FinancialHeaderID));
                sqlParams.Add(new SqlParameter("@isCurrentYearActivity", isCurrentYearActivity));

                #region Filter by Account Codes

                if (!string.IsNullOrEmpty(accountCodeFrom) && !string.IsNullOrEmpty(accountCodeTo))
                {
                    //accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID BETWEEN @AccountCodeFrom AND @AccountCodeTo ";
                    accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID like @AccountCodeFrom+'%' ";
                    sqlParams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                }
                else if (!string.IsNullOrEmpty(accountCodeFrom))
                {
                    accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID >= @AccountCodeFrom ";
                    sqlParams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                }
                else if (!string.IsNullOrEmpty(accountCodeTo))
                {
                    accountCodeFilter = " AND GL_TransactionDetail.CodeCombinationID <= @AccountCodeTo ";
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                }

                #endregion

                if (fromDocumentType != -1 && toDocumentType != -1)
                    additionalFilter.Append(" AND GL_TransactionHeader.DocumentTypeID between " + fromDocumentType + " AND " + toDocumentType);

                if (reversedDocuments)
                    additionalFilter.Append(" AND IsNull(GL_TransactionHeader.IsReversed, 0) = 0 And GL_TransactionHeader.ReversalID is null ");

                if (documentGroupID != "")
                    additionalFilter.Append(" AND Set_DocumentType.GroupID in " + documentGroupID.ToString() + " ");

                if (documentTypeAuthentication)
                {
                    sqlParams.Add(new SqlParameter("@UserID", userID));
                    //                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
                    //AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                if (costCenterID.HasValue)
                    additionalFilter.Append(" AND Set_CostCenter.CostCenterID = " + costCenterID.Value.ToString() + " ");

                if (Narration != "")
                    narrationFilter.Append(" where isnull(FinTAB.NarrationHeader,'') like '%" + Narration.ToString() + "%' ");

                if (Particular != "" && Narration != "")
                {
                    particularFilter.Append(" and isnull(FinTAB.Narration, '') like '%" + Particular.ToString() + "%' ");
                }
                if (Particular != "" && Narration == "")
                {
                    particularFilter.Append(" where  isnull(FinTAB.Narration, '') like '%" + Particular.ToString() + "%' ");
                }

                companyID = "(" + companyID + ")";

                string query = Query_Account_Ledger.Replace("{AccountCodeFilter}", accountCodeFilter);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, additionalFilter.ToString(), qry_docuTypeAuthenticate, narrationFilter, particularFilter).Replace("@CompanyID", companyID),
                    sqlParams.ToArray());

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
}
