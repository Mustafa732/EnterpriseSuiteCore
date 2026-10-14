using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;

using Common.Utils;

using DAL.DataAccessLayer;

using FnI.BusinessAccessLayer.AP.Transaction;
using FnI.BusinessAccessLayer.GL.Transaction;
using FnI.BusinessAccessLayer.Inventory.Transaction;
using FnI.BusinessAccessLayer.Setup;
using System.Text;

namespace FnI.BusinessAccessLayer.AR.Transaction
{
    public partial class AR_InvoiceHeader : BaseClass
    {
        #region Class Methods

        public static DataTable GetAmountOnDirectPayment(int paymentid, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@PST", paymentid);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                string Query_GetAmountOnDirectPayment = @" Select IsNull(SUM(AdvanceAmount), 0) As AmountPaid From AR_InvoiceAdvancePaymentDetail
Where Status = 1 And CompanyID = @CompanyID And PeriodID = @PeriodID  and PaymentHeaderID = @PST ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAmountOnDirectPayment, sqlparams);

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

        public static bool Get_UpdateIsPayment(int companyID, int periodID, long headerID, int flag, SqlTransaction objSQLTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Doc", headerID);
                sqlparams[3] = new SqlParameter("@flag", flag);

                string Query_UpdateIsPayment = @" Update AR_InvoiceHeader Set ISPaymentCompleted = @flag 
Where CompanyID = @CompanyID AND PeriodID = @PeriodId And InvoiceID = @Doc ";

                if (objSQLTransaction != null)
                {
                    if (SqlHelper.ExecuteNonQuery(objSQLTransaction, CommandType.Text, Query_UpdateIsPayment, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateIsPayment, sqlparams) > 0)
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

        public static bool Get_UpdateIsPaymentNo(int companyID, int periodID, int invoiceHeaderID, int flag)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@InvoiceHeaderID", invoiceHeaderID);
                sqlparams[3] = new SqlParameter("@flag", flag);

                string Query_UpdateIsPaymentNo = @" Update AR_InvoiceHeader Set ISPaymentCompleted = @flag 
Where CompanyID = @CompanyID AND PeriodID = @PeriodId And InvoiceID = @InvoiceHeaderID ";

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateIsPaymentNo, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataTable Get_AmountFromPayment(int Invoice, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@Doc", Invoice);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                string Query_AmountFromPayment = @" Select isnull(Sum(Amount), 0) As Amount From AP_PaymentInvoicesDetail 
Where invoiceheaderid = @Doc and companyid = @CompanyID and periodid = @PeriodID and status = 1 ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_AmountFromPayment, sqlparams);

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

        public static DataTable Get_AmountFromInvoice(int Invoice, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@Doc", Invoice);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                string Query_AmountFromInvoice = @" Select isnull(AmountPaid, 0) As AmountPaid From AR_InvoiceHeader 
where InvoiceID = @Doc and companyid = @CompanyID and periodid = @PeriodID and status = 1 ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_AmountFromInvoice, sqlparams);

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

        public static DataTable Get_AmountFromPaymentNo(int Invoice, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@Doc", Invoice);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_AmountFromPaymentNo, sqlparams);

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

        public static DataTable Get_AmountFromInvoiceNo(int Invoice, int companyID, int periodID, int? iid = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@Doc", Invoice);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@IID", iid);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_AmountFromInvoiceNo, sqlparams);

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

        public static DataTable Get_AmountFromPaymentDirectPaymentEdit(int Invoice, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@Doc", Invoice);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text,
                    FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_AmountFromPaymentNoForDirectPaymentEditMode, sqlparams);

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

        public static DataTable Get_AmountFromInvoiceDirectPayment(int Invoice, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@Doc", Invoice);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                string Query_AmountFromPaymentForDirectPayment = @" Select isnull(Sum(NetAmount), 0) As Amount From AP_PaymentDetail 
Where invoiceheaderid = @Doc and companyid = @CompanyID and periodid = @PeriodID and status = 1 ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_AmountFromPaymentForDirectPayment, sqlparams);

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

        public DataSet Get_DirectReport(int? DocumentTypeID, int InvoiceFrom, int InvoiceTo, int companyID, int periodID, int FiscalID = 0)
        {
            DataSet dsToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@InvoiceFrom", InvoiceFrom);
                sqlparams[1] = new SqlParameter("@InvoiceTo", InvoiceTo);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);//@CurrentFiscalID
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", FiscalID);

                if (InvoiceFrom > 0 && InvoiceTo > 0)
                    sb.Append(" AND InvoiceNumber between @InvoiceFrom AND @InvoiceTo ");
                else if (InvoiceFrom > 0 && InvoiceTo == 0)
                    sb.Append(" AND InvoiceNumber = @InvoiceFrom ");
                else if (InvoiceFrom == 0 && InvoiceTo > 0)
                    sb.Append(" AND InvoiceNumber = @InvoiceTo ");
                if (DocumentTypeID > 0)
                    sb.Append(" AND InvoiceType = " + DocumentTypeID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_DirectInvoiceReport, sb.ToString()), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    ds.Tables[1].TableName = "tblSupplier";
                    ds.Tables[2].TableName = "tblAccountCodes";
                    ds.Tables[3].TableName = "tblGLDetails";
                    dsToReturn = ds;
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public int GetMaxInvoiceNumberPerDocument(int companyID, int periodID, int documentTypeId)
        {
            int invoiceNum = 0;

            SqlParameter[] sqlparams = new SqlParameter[3];
            sqlparams[0] = new SqlParameter("@DocumentTypeID", documentTypeId);
            sqlparams[1] = new SqlParameter("@CompanyID", companyID);
            sqlparams[2] = new SqlParameter("@PeriodID", periodID);

            string Query_MaxInvoiceNumberPerDocument = @" 
            select isnull(Max(CONVERT(int, InvoiceNumber)), 0) + 1 from AR_InvoiceHeader 
            Where CompanyID = @CompanyID AND PeriodID = @PeriodID AND InvoiceType = @DocumentTypeID ";

            invoiceNum = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_MaxInvoiceNumberPerDocument, sqlparams));

            return invoiceNum;
        }

        public int Get_GLTransactionHeaderID(int companyID, int periodID, int documentNo, int documentTypeID)
        {
            int gl_HeaderID;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@DocumentNo", documentNo);
                sqlparams[1] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[2] = new SqlParameter("@companyid", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);

                string Query_GetGL_TransHeaderID = @" SELECT TransactionID FROM GL_TransactionHeader 
                Where PeriodID = @PeriodID and CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID and DocumentNo = @DocumentNo and status = 1 ";

                gl_HeaderID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetGL_TransHeaderID, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return gl_HeaderID;
        }

        public DataTable GetAll_Report_APBillWise(int companyID, int periodID, int? vendorFilter, Nullable<DateTime> dateFrom = null,
            Nullable<DateTime> dateTo = null, string paidOptions = "2", Nullable<int> documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            string additionalParameters = string.Empty;
            string paidClause = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@VendorFilter", vendorFilter);

                #region Filter by Invoice Date

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    additionalParameters = " AND AR_InvoiceHeader.InvoiceDate BETWEEN @FromDate AND @ToDate ";
                    sqlparams[3] = new SqlParameter("@FromDate", dateFrom.Value);
                    sqlparams[4] = new SqlParameter("@ToDate", dateTo.Value);
                }
                else if (dateFrom.HasValue)
                {
                    additionalParameters = " AND AR_InvoiceHeader.InvoiceDate >= @FromDate ";
                    sqlparams[3] = new SqlParameter("@FromDate", dateFrom.Value);
                }
                else if (dateTo.HasValue)
                {
                    additionalParameters = " AND AR_InvoiceHeader.InvoiceDate <= @ToDate ";
                    sqlparams[3] = new SqlParameter("@ToDate", dateTo.Value);
                }
                if (documentGroupID.HasValue)
                    sqlparams[5] = new SqlParameter("@DocumentGroupID", documentGroupID.Value);
                else
                    sqlparams[5] = new SqlParameter("@DocumentGroupID", DBNull.Value);



                #endregion

                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                #region Filter by payment

                switch (paidOptions)
                {
                    case "0":
                        paidClause = @" 

   where ((NetAmount) - (OtherTax)) -(AdvanceAmount) - PaymentAmount  <= 0
";
    //                    and exists 
    //(
    //    select * from  AP_PaymentHeader where AP_PaymentHeader.CompanyID = ap_paymentinvoicesDetail.CompanyID And AP_PaymentHeader.PeriodID = ap_paymentinvoicesDetail.PeriodID 
    //    and ap_paymentinvoicesDetail.PaymentHeaderID = AP_PaymentHeader.PaymentID
    //)
                        break;

                    case "1":
                        paidClause = @" 
 where ((NetAmount) - (OtherTax)) -(AdvanceAmount) - PaymentAmount  > 0
";
                        break;

    //                    and not exists 
    //(
    //    select * from  AP_PaymentHeader where AP_PaymentHeader.CompanyID = ap_paymentinvoicesDetail.CompanyID And AP_PaymentHeader.PeriodID = ap_paymentinvoicesDetail.PeriodID 
    //    and ap_paymentinvoicesDetail.PaymentHeaderID = AP_PaymentHeader.PaymentID
    //)

                    default:
                        break;
                }

                #endregion

                dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text,
                    string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_Report_APBillWise, paidClause, additionalParameters), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public DataTable GetAll_APWithHoldingTax_RSM(int companyID, int periodID, int? vendorFilter, int? PaymentTypeID, Nullable<DateTime> dateFrom = null,
           Nullable<DateTime> dateTo = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            string additionalParameters = string.Empty;
            string paidClause = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@VendorFilter", vendorFilter);
                
                #region Filter by Invoice Date

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    //additionalParameters = " AND AR_InvoiceHeader.InvoiceDate BETWEEN @FromDate AND @ToDate ";
                    sqlparams[3] = new SqlParameter("@FromDate", dateFrom.Value);
                    sqlparams[4] = new SqlParameter("@ToDate", dateTo.Value);
                }
             
                #endregion

                sqlparams[5] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[6] = new SqlParameter("@PaymentTypeID", PaymentTypeID);

                dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text,
                    string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_APWithHoldingTax_RSM, additionalParameters), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }


        public static DataTable GetAll_Report_APBillReport(int companyID, int periodID, string VendorID, Nullable<DateTime> dateFrom = null,
            Nullable<DateTime> dateTo = null, Nullable<int> documentGroupID = null)
        {
            DataTable dt = null;
            string additionalParameters = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (!string.IsNullOrEmpty(VendorID))
                {
                    additionalParameters += " AND AR_InvoiceHeader.VendorID = @VendorID ";
                    sqlparams[2] = new SqlParameter("@VendorID", VendorID);
                }

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    additionalParameters += " AND AR_InvoiceHeader.InvoiceDate BETWEEN @FromDate AND @ToDate ";
                    sqlparams[3] = new SqlParameter("@FromDate", dateFrom.Value);
                    sqlparams[4] = new SqlParameter("@ToDate", dateTo.Value);
                }
                else if (dateFrom.HasValue)
                {
                    additionalParameters += " AND AR_InvoiceHeader.InvoiceDate >= @FromDate ";
                    sqlparams[3] = new SqlParameter("@FromDate", dateFrom.Value);
                }
                else if (dateTo.HasValue)
                {
                    additionalParameters += " AND AR_InvoiceHeader.InvoiceDate <= @ToDate ";
                    sqlparams[3] = new SqlParameter("@ToDate", dateTo.Value);
                }
                if (documentGroupID.HasValue)
                    sqlparams[5] = new SqlParameter("@DocumentGroupID", documentGroupID.Value);
                else
                    sqlparams[5] = new SqlParameter("@DocumentGroupID", DBNull.Value);

                string query = string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_Report_APBillReport, additionalParameters);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetAll_Report_APBillReport_ForSKU(int companyID, int periodID, string VendorID, string paidOptions = "2", Nullable<DateTime> dateFrom = null,
            Nullable<DateTime> dateTo = null, Nullable<int> documentGroupID = null)
        {
            DataTable dt = null;
            string additionalParameters = string.Empty;
            string paidClause = string.Empty;
            string paidClause2 = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (!string.IsNullOrEmpty(VendorID))
                {
                    additionalParameters += " AND AR_InvoiceHeader.VendorID = @VendorID ";
                    sqlparams[2] = new SqlParameter("@VendorID", VendorID);
                }

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    additionalParameters += " AND AR_InvoiceHeader.InvoiceDate BETWEEN @FromDate AND @ToDate ";
                    sqlparams[3] = new SqlParameter("@FromDate", dateFrom.Value);
                    sqlparams[4] = new SqlParameter("@ToDate", dateTo.Value);
                }
                else if (dateFrom.HasValue)
                {
                    additionalParameters += " AND AR_InvoiceHeader.InvoiceDate >= @FromDate ";
                    sqlparams[3] = new SqlParameter("@FromDate", dateFrom.Value);
                }
                else if (dateTo.HasValue)
                {
                    additionalParameters += " AND AR_InvoiceHeader.InvoiceDate <= @ToDate ";
                    sqlparams[3] = new SqlParameter("@ToDate", dateTo.Value);
                }
                if (documentGroupID.HasValue)
                    sqlparams[5] = new SqlParameter("@DocumentGroupID", documentGroupID.Value);
                else
                    sqlparams[5] = new SqlParameter("@DocumentGroupID", DBNull.Value);

                #region Filter by payment

                switch (paidOptions)
                {
                    case "0":
                        paidClause = @" where (((GrossAmount + TaxAmount + OtherAmount)) - (OtherTax)) -
		                          (AdvanceAmount) - PaymentAmount  <= 0
                  ";
                        break;

                    case "1":
                        paidClause = @" where (((GrossAmount + TaxAmount + OtherAmount)) - (OtherTax)) -
		                          (AdvanceAmount) - PaymentAmount  > 0
                  ";
                        break;

                    default:
                        break;

                }
                #endregion

                string query = string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_Report_APBillReport_ForSKU, additionalParameters, paidClause);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAll_Report_APBillReport_ForSSML(int companyID, int periodID, int FiscalID, string VendorID, string paidOptions = "2", Nullable<DateTime> dateFrom = null,
            Nullable<DateTime> dateTo = null, Nullable<int> documentGroupID = null)
        {
            DataTable dt = null;
            string additionalParameters = string.Empty;
            string paidClause = string.Empty;
            string paidClause2 = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FiscalID", FiscalID);
                
                if (!string.IsNullOrEmpty(VendorID))
                {
                    additionalParameters += " AND AR_InvoiceHeader.VendorID = @VendorID ";
                    sqlparams[3] = new SqlParameter("@VendorID", VendorID);
                }

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    additionalParameters += " AND AR_InvoiceHeader.InvoiceDate BETWEEN @FromDate AND @ToDate ";
                    sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                    sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                }
                else if (dateFrom.HasValue)
                {
                    additionalParameters += " AND AR_InvoiceHeader.InvoiceDate >= @FromDate ";
                    sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                }
                else if (dateTo.HasValue)
                {
                    additionalParameters += " AND AR_InvoiceHeader.InvoiceDate <= @ToDate ";
                    sqlparams[4] = new SqlParameter("@ToDate", dateTo.Value);
                }
                if (documentGroupID.HasValue)
                    sqlparams[6] = new SqlParameter("@DocumentGroupID", documentGroupID.Value);
                else
                    sqlparams[6] = new SqlParameter("@DocumentGroupID", DBNull.Value);

                #region Filter by payment

//                switch (paidOptions)
//                {
//                    case "0":
//                        paidClause = @" where (((GrossAmount + TaxAmount + OtherAmount)) - (OtherTax)) -
//		                          (AdvanceAmount) - PaymentAmount  <= 0
//                  ";
//                        break;

//                    case "1":
//                        //(((GrossAmount + TaxAmount + OtherAmount)) - (OtherTax)) -
//                        paidClause = @" where ((NetAmount) - (OtherTax)) -
//		                          (AdvanceAmount) - PaymentAmount  > 0
//                  ";
//                        break;

//                    default:
//                        break;

//                }

                switch (paidOptions)
                {
                    case "0":
                        paidClause = "'Paid'";
                        break;

                    case "1":
                        //(((GrossAmount + TaxAmount + OtherAmount)) - (OtherTax)) -
                        paidClause = "'Unpaid'";
                        break;
                    default:
                        paidClause = "'Both'";
                        break;

                }


                #endregion

                string query = string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_Report_APBillReport_ForSSML, additionalParameters, paidClause);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAll_Report_APBillReport_Fiscal(int companyID, int periodID, string VendorID, Nullable<DateTime> dateFrom = null,
           Nullable<DateTime> dateTo = null, Nullable<int> documentGroupID = null)
        {
            DataTable dt = null;
            string additionalParameters = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (!string.IsNullOrEmpty(VendorID))
                {
                    additionalParameters += " AND AR_InvoiceHeader.VendorID = @VendorID ";
                    sqlparams[2] = new SqlParameter("@VendorID", VendorID);
                }

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    additionalParameters += " AND AR_InvoiceHeader.InvoiceDate BETWEEN @FromDate AND @ToDate ";
                    sqlparams[3] = new SqlParameter("@FromDate", dateFrom.Value);
                    sqlparams[4] = new SqlParameter("@ToDate", dateTo.Value);
                }
                else if (dateFrom.HasValue)
                {
                    additionalParameters += " AND AR_InvoiceHeader.InvoiceDate >= @FromDate ";
                    sqlparams[3] = new SqlParameter("@FromDate", dateFrom.Value);
                }
                else if (dateTo.HasValue)
                {
                    additionalParameters += " AND AR_InvoiceHeader.InvoiceDate <= @ToDate ";
                    sqlparams[3] = new SqlParameter("@ToDate", dateTo.Value);
                }
                if (documentGroupID.HasValue)
                    sqlparams[5] = new SqlParameter("@DocumentGroupID", documentGroupID.Value);
                else
                    sqlparams[5] = new SqlParameter("@DocumentGroupID", DBNull.Value);


                string query = string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_Report_APBillReport_Fiscal, additionalParameters);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetAll_Report_APBillReportDashboard(int companyID, int periodID, string vendorFilter, Nullable<DateTime> dateFrom = null,
            Nullable<DateTime> dateTo = null)
        {
            DataTable dt = null;
            string additionalParameters = string.Empty;
            string orderBy = " ORDER BY AR_InvoiceHeader.InvoiceDate, AR_InvoiceHeader.InvoiceNumber ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@VendorFilter", vendorFilter);

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    additionalParameters = " AND AR_InvoiceHeader.InvoiceDate BETWEEN @FromDate AND @ToDate ";
                    sqlparams[3] = new SqlParameter("@FromDate", dateFrom.Value);
                    sqlparams[4] = new SqlParameter("@ToDate", dateTo.Value);
                }
                else if (dateFrom.HasValue)
                {
                    additionalParameters = " AND AR_InvoiceHeader.InvoiceDate >= @FromDate ";
                    sqlparams[3] = new SqlParameter("@FromDate", dateFrom.Value);
                }
                else if (dateTo.HasValue)
                {
                    additionalParameters = " AND AR_InvoiceHeader.InvoiceDate <= @ToDate ";
                    sqlparams[3] = new SqlParameter("@ToDate", dateTo.Value);
                }

                #region Query

                string Query_Report_APBillReportDashboard = @"
SELECT TOP 7 InvoiceType AS InvoiceTypeID, SET_DocumentType.DocumentType AS InvoiceType, InvoiceDate, InvoiceNumber, C.CustomerName, CurrencyName, 
    ISNULL(AR_InvoiceDetail.GrossAmount, 0) - ISNULL(AR_InvoiceDetail.TaxAmount, 0) - ISNULL(AR_InvoiceDetail.DiscountAmountTaken, 0) AS NetAmount, 
    SET_Term.Description As PaymentTerms, AP_PurchaseOrderHeader.DocumentNumber AS PurchaseOrderNumber 
FROM AR_InvoiceHeader 
    INNER JOIN AR_InvoiceDetail ON AR_InvoiceHeader.InvoiceID = AR_InvoiceDetail.InvoiceHeaderID AND AR_InvoiceHeader.CompanyID = AR_InvoiceDetail.CompanyID 
        AND AR_InvoiceHeader.PeriodID = AR_InvoiceDetail.PeriodID AND AR_InvoiceDetail.Status = 1 
    INNER JOIN AP_PurchaseOrderHeader ON AP_PurchaseOrderHeader.CompanyID = AR_InvoiceDetail.CompanyID 
        AND AP_PurchaseOrderHeader.PeriodID = AR_InvoiceDetail.PeriodID AND AP_PurchaseOrderHeader.POHeaderID = AR_InvoiceDetail.PurchaseOrderHeaderID 
    INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = AR_InvoiceHeader.CompanyID AND SET_DocumentType.PeriodID = AR_InvoiceHeader.PeriodID 
        AND SET_DocumentType.DocumentTypeID = AR_InvoiceHeader.InvoiceType 
    INNER JOIN [SET_Currency] ON AR_InvoiceHeader.InvoiceCurrencyID = SET_Currency.CurrencyID 
    INNER JOIN [AR_SET_Customer] C ON AR_InvoiceHeader.PeriodID = C.PeriodID  --AR_InvoiceHeader.CompanyID = C.CompanyID AND 
        AND AR_InvoiceHeader.VendorID = C.CustomerID AND C.CustomerName like '%' + @VendorFilter + '%' 
    LEFT JOIN [SET_Term] ON AR_InvoiceHeader.TermsID = SET_Term.TermID 
WHERE AR_InvoiceHeader.CompanyID = @CompanyID AND AR_InvoiceHeader.PeriodID = @PeriodID AND AR_InvoiceHeader.Status = 1 ";

                #endregion

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Report_APBillReportDashboard + additionalParameters + orderBy, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataWithOtherTables(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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

                    #region Query

                    string Query_SelectAll_Searching_WithPaging = @" 

SELECT *, ROW_NUMBER() OVER (ORDER BY [AR_InvoiceHeader].[InvoiceID] desc) AS 'Row' FROM 
(
    SELECT DISTINCT ARINVH.CompanyID, ARINVH.PeriodID, ARINVH.InvoiceID, ARINVH.CostCenterID, ARINVH.AcctsPayCodeCombinationID, ARINVH.AmountApplicableToDiscount, 
        ARINVH.AmountPaid, ARINVH.ApprovalDescription, ARINVH.ApprovalStatus, ARINVH.ApprovedAmount, ISNULL(SET_DocumentApprove.CreatedByValue,ARINVH.UpdatedByValue) as AuthorizedBy,
		ARINVH.AutoTaxCalculationFlag, ARINVH.AutoWithholdingTaxFlag, ARINVH.AutoWithholdingTaxGroupID, ARINVH.BatchID, ARINVH.CancelledAmount, ARINVH.CancelledBy, 
        ARINVH.CancelledDate, ARINVH.CreatedBy, ARINVH.CreateDate, ARINVH.CreatedByValue, ARINVH.Description, ARINVH.DiscountAmountTaken, ARINVH.DocumentSequenceID, 
        ARINVH.EarliestSettlementDate, ARINVH.ExchangeDate, ARINVH.ExchangeRate, ARINVH.ExchangeRateType, ARINVH.ExclusivePaymentFlag, ARINVH.FreightAmount, 
        ARINVH.GLPostingDate, ARINVH.GoodsReceivedDate, ARINVH.GrossAmount, ARINVH.InvoiceCurrencyID, ARINVH.InvoiceDate, ARINVH.InvoiceNumber, 
        ARINVH.InvoiceReceivedDate, ARINVH.InvoiceType, ARINVH.PayCurrencyInvoiceAmount, ARINVH.PaymentGroupType, ARINVH.PaymentCrossRate, 
        ARINVH.PaymentCrossRateDate, ARINVH.PaymentCrossRateType, ARINVH.PaymentCurrencyID, ARINVH.PaymentMethodType, ARINVH.PaymentStatusType, 
        ARINVH.PostingStatus, ARINVH.RecurringPaymentID, ARINVH.TaxAmount, ARINVH.TermsDate, ARINVH.TermsID, ARINVH.UpdatedBy, ARINVH.UpdateDate, 
        ARINVH.UpdatedByValue, ARINVH.VATCode, ARINVH.VendorSiteID, ARINVH.VoucherNumber, ARINVH.Status, SET_DocumentType.DocumentType,
case 
	when DocumentType = 'DI-Invoice (HR)' then  ARINVH.Description
	else 
	case when C.CustomerName is null then (
	Select TOP 1 (AR_SET_Customer.CustomerName) from [AR_InvoiceHeader] sal_hdr
	inner join  AR_InvoiceSupplierDetail on AR_InvoiceSupplierDetail.InvoiceHeaderID= sal_hdr.InvoiceID
	and AR_InvoiceSupplierDetail.CompanyID= sal_hdr.CompanyID
	And  AR_InvoiceSupplierDetail.PeriodID= sal_hdr.PeriodID
	And AR_InvoiceSupplierDetail.Status=1
	and sal_hdr.InvoiceID = ARINVH.InvoiceID
	and sal_hdr.CompanyID = ARINVH.CompanyID
	 inner join AR_SET_Customer on AR_SET_Customer.CustomerID=AR_InvoiceSupplierDetail.SupplierID
	  and AR_SET_Customer.PeriodID = AR_InvoiceSupplierDetail.PeriodID 
	 ) else C.CustomerName end end  AS VendorName,
    ARINVH.StatusCode ,Set_PeriodCustom.PeriodSetName,ARINVH.InvoiceDate as DocumentDate
,	case when C.CustomerID is null then (
	Select TOP 1 (AR_SET_Customer.CustomerID) from [AR_InvoiceHeader] sal_hdr
	inner join  AR_InvoiceSupplierDetail on AR_InvoiceSupplierDetail.InvoiceHeaderID= sal_hdr.InvoiceID
	and AR_InvoiceSupplierDetail.CompanyID= sal_hdr.CompanyID
	And  AR_InvoiceSupplierDetail.PeriodID= sal_hdr.PeriodID
	And AR_InvoiceSupplierDetail.Status=1
	and sal_hdr.InvoiceID = ARINVH.InvoiceID
	and sal_hdr.CompanyID = ARINVH.CompanyID
	 inner join AR_SET_Customer on AR_SET_Customer.CustomerID=AR_InvoiceSupplierDetail.SupplierID
	  and AR_SET_Customer.PeriodID = AR_InvoiceSupplierDetail.PeriodID 
	 ) else C.CustomerID end  AS VendorID
    FROM AR_InvoiceHeader ARINVH 
        INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = ARINVH.CompanyID AND SET_DocumentType.PeriodID = ARINVH.PeriodID 
            AND SET_DocumentType.DocumentTypeID = ARINVH.InvoiceType 
        LEFT JOIN AR_SET_Customer C on ARINVH.VendorID = C.CustomerID --and ARINVH.CompanyID = C.CompanyID 
and ARINVH.PeriodID = C.PeriodID 
        LEFT JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = ARINVH.CompanyID AND SET_DocumentApprove.PeriodID = ARINVH.PeriodID 
            AND SET_DocumentApprove.Status = 1 AND SET_DocumentApprove.DocumentTypeID = ARINVH.InvoiceType AND SET_DocumentApprove.DocumentNo = ARINVH.InvoiceNumber 
            AND SET_DocumentApprove.CreateDate = 
            (
                SELECT MAX(CreateDate) FROM SET_DocumentApprove 
                WHERE CompanyID = ARINVH.CompanyID AND PeriodID = ARINVH.PeriodID AND DocumentTypeID = ARINVH.InvoiceType AND DocumentNo = ARINVH.InvoiceNumber
                and SET_DocumentApprove.FiscalID = ARINVH.FiscalID
            )
        inner join Set_PeriodCustom on ARINVH.InvoiceDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate
        and Set_PeriodCustom.Status=1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID
) AR_InvoiceHeader
 ";

                    #endregion

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_Searching_WithPaging + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), Query_SelectAll_Searching_WithPaging + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    #region Query

                    string Query_SelectAll_Searching = @" 

SELECT *, ROW_NUMBER() OVER (ORDER BY [AR_InvoiceHeader].[InvoiceID] desc) AS 'Row' FROM 
(
    SELECT DISTINCT ARINVH.CompanyID, ARINVH.PeriodID, ARINVH.InvoiceID, ARINVH.CostCenterID, ARINVH.AcctsPayCodeCombinationID, ARINVH.AmountApplicableToDiscount, 
        ARINVH.AmountPaid, ARINVH.ApprovalDescription, ARINVH.ApprovalStatus, ARINVH.ApprovedAmount, SET_DocumentApprove.CreatedByValue as AuthorizedBy, 
        ARINVH.AutoTaxCalculationFlag, ARINVH.AutoWithholdingTaxFlag, ARINVH.AutoWithholdingTaxGroupID, ARINVH.BatchID, ARINVH.CancelledAmount, ARINVH.CancelledBy, 
        ARINVH.CancelledDate, ARINVH.CreatedBy, ARINVH.CreateDate, ARINVH.CreatedByValue, ARINVH.Description, ARINVH.DiscountAmountTaken, ARINVH.DocumentSequenceID, 
        ARINVH.EarliestSettlementDate, ARINVH.ExchangeDate, ARINVH.ExchangeRate, ARINVH.ExchangeRateType, ARINVH.ExclusivePaymentFlag, ARINVH.FreightAmount, 
        ARINVH.GLPostingDate, ARINVH.GoodsReceivedDate, ARINVH.GrossAmount, ARINVH.InvoiceCurrencyID, ARINVH.InvoiceDate, ARINVH.InvoiceNumber, 
        ARINVH.InvoiceReceivedDate, ARINVH.InvoiceType, ARINVH.PayCurrencyInvoiceAmount, ARINVH.PaymentGroupType, ARINVH.PaymentCrossRate, 
        ARINVH.PaymentCrossRateDate, ARINVH.PaymentCrossRateType, ARINVH.PaymentCurrencyID, ARINVH.PaymentMethodType, ARINVH.PaymentStatusType, 
        ARINVH.PostingStatus, ARINVH.RecurringPaymentID, ARINVH.TaxAmount, ARINVH.TermsDate, ARINVH.TermsID, ARINVH.UpdatedBy, ARINVH.UpdateDate, 
        ARINVH.UpdatedByValue, ARINVH.VATCode, ARINVH.VendorID, ARINVH.VendorSiteID, ARINVH.VoucherNumber, ARINVH.Status, SET_DocumentType.DocumentType, 
INV_StockTransactionHeader.StockTransactionID,
	case when C.CustomerName is null then (
	Select max (AR_SET_Customer.CustomerName) from [AR_InvoiceHeader] sal_hdr
	inner join  AR_InvoiceSupplierDetail on AR_InvoiceSupplierDetail.InvoiceHeaderID= sal_hdr.InvoiceID
	and AR_InvoiceSupplierDetail.CompanyID= sal_hdr.CompanyID
	And  AR_InvoiceSupplierDetail.PeriodID= sal_hdr.PeriodID
	And AR_InvoiceSupplierDetail.Status=1
	and sal_hdr.InvoiceID = ARINVH.InvoiceID
	 inner join AR_SET_Customer on AR_SET_Customer.CustomerID=AR_InvoiceSupplierDetail.SupplierID
	  and AR_SET_Customer.PeriodID = AR_InvoiceSupplierDetail.PeriodID 
	 ) else C.CustomerName end  AS VendorName, ARINVH.StatusCode ,Set_PeriodCustom.PeriodSetName,ARINVH.InvoiceDate as DocumentDate
    FROM AR_InvoiceHeader ARINVH 
        INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = ARINVH.CompanyID AND SET_DocumentType.PeriodID = ARINVH.PeriodID 
            AND SET_DocumentType.DocumentTypeID = ARINVH.InvoiceType 
        LEFT JOIN AR_SET_Customer C on ARINVH.VendorID = C.CustomerID --and ARINVH.CompanyID = C.CompanyID 
and ARINVH.PeriodID = C.PeriodID 
	    LEFT JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = ARINVH.CompanyID AND SET_DocumentApprove.PeriodID = ARINVH.PeriodID 
            AND SET_DocumentApprove.Status = 1 AND SET_DocumentApprove.DocumentTypeID = ARINVH.InvoiceType AND SET_DocumentApprove.DocumentNo = ARINVH.InvoiceNumber 
            AND SET_DocumentApprove.CreateDate = 
            (
                SELECT MAX(CreateDate) FROM SET_DocumentApprove 
                WHERE CompanyID = ARINVH.CompanyID AND PeriodID = ARINVH.PeriodID AND DocumentTypeID = ARINVH.InvoiceType AND DocumentNo = ARINVH.InvoiceNumber
            )
        inner join Set_PeriodCustom on ARINVH.InvoiceDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate
        and Set_PeriodCustom.Status=1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID
  left join AR_InvoiceDetail on AR_InvoiceDetail.InvoiceDetailID = ARINVH.InvoiceID 
		     left  join INV_StockTransactionHeader on INV_StockTransactionHeader.StockTransactionID  = AR_InvoiceDetail.stockHeaderID
) AR_InvoiceHeader

";

                    #endregion

                    #region Query Voucher
                    string query = @"
    SELECT DISTINCT ARINVH.CompanyID, ARINVH.PeriodID, ARINVH.InvoiceID, ARINVH.CostCenterID, ARINVH.AcctsPayCodeCombinationID, ARINVH.AmountApplicableToDiscount, 
        ARINVH.AmountPaid, ARINVH.ApprovalDescription, ARINVH.ApprovalStatus, ARINVH.ApprovedAmount, ISNULL(SET_DocumentApprove.CreatedByValue,ARINVH.UpdatedByValue) as AuthorizedBy,
		ARINVH.AutoTaxCalculationFlag, ARINVH.AutoWithholdingTaxFlag, ARINVH.AutoWithholdingTaxGroupID, ARINVH.BatchID, ARINVH.CancelledAmount, ARINVH.CancelledBy, 
        ARINVH.CancelledDate, ARINVH.CreatedBy, ARINVH.CreateDate, ARINVH.CreatedByValue, ARINVH.Description, ARINVH.DiscountAmountTaken, ARINVH.DocumentSequenceID, 
        ARINVH.EarliestSettlementDate, ARINVH.ExchangeDate, ARINVH.ExchangeRate, ARINVH.ExchangeRateType, ARINVH.ExclusivePaymentFlag, ARINVH.FreightAmount, 
        ARINVH.GLPostingDate, ARINVH.GoodsReceivedDate, ARINVH.GrossAmount, ARINVH.InvoiceCurrencyID, ARINVH.InvoiceDate, ARINVH.InvoiceNumber, 
        ARINVH.InvoiceReceivedDate, ARINVH.InvoiceType, ARINVH.PayCurrencyInvoiceAmount, ARINVH.PaymentGroupType, ARINVH.PaymentCrossRate, 
        ARINVH.PaymentCrossRateDate, ARINVH.PaymentCrossRateType, ARINVH.PaymentCurrencyID, ARINVH.PaymentMethodType, ARINVH.PaymentStatusType, 
        ARINVH.PostingStatus, ARINVH.RecurringPaymentID, ARINVH.TaxAmount, ARINVH.TermsDate, ARINVH.TermsID, ARINVH.UpdatedBy, ARINVH.UpdateDate, 
        ARINVH.UpdatedByValue, ARINVH.VATCode, ARINVH.VendorID, ARINVH.VendorSiteID, ARINVH.VoucherNumber, ARINVH.Status, SET_DocumentType.DocumentType,
	case when C.CustomerName is null then (
	Select max (AR_SET_Customer.CustomerName) from [AR_InvoiceHeader] sal_hdr
	inner join  AR_InvoiceSupplierDetail on AR_InvoiceSupplierDetail.InvoiceHeaderID= sal_hdr.InvoiceID
	and AR_InvoiceSupplierDetail.CompanyID= sal_hdr.CompanyID
	And  AR_InvoiceSupplierDetail.PeriodID= sal_hdr.PeriodID
	And AR_InvoiceSupplierDetail.Status=1
	and sal_hdr.InvoiceID = ARINVH.InvoiceID
	 inner join AR_SET_Customer on AR_SET_Customer.CustomerID=AR_InvoiceSupplierDetail.SupplierID
	  and AR_SET_Customer.PeriodID = AR_InvoiceSupplierDetail.PeriodID 
	 ) else C.CustomerName end  AS VendorName, ARINVH.StatusCode ,Set_PeriodCustom.PeriodSetName,ARINVH.InvoiceDate as DocumentDate
    FROM AR_InvoiceHeader ARINVH 
	LEFT JOIN GL_TransactionHeader ON GL_TransactionHeader.DocumentTypeID = ARINVH.InvoiceType 
        AND GL_TransactionHeader.DocumentNo = ARINVH.InvoiceNumber AND GL_TransactionHeader.CompanyID = ARINVH.CompanyID 
        AND GL_TransactionHeader.PeriodID = ARINVH.PeriodID AND GL_TransactionHeader.Status = 1 
			and GL_TransactionHeader.FiscalID = ARINVH.FiscalID

        INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = ARINVH.CompanyID AND SET_DocumentType.PeriodID = ARINVH.PeriodID 
            AND SET_DocumentType.DocumentTypeID = ARINVH.InvoiceType 
        LEFT JOIN AR_SET_Customer C on ARINVH.VendorID = C.CustomerID --and ARINVH.CompanyID = C.CompanyID 
and ARINVH.PeriodID = C.PeriodID 
        LEFT JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = ARINVH.CompanyID AND SET_DocumentApprove.PeriodID = ARINVH.PeriodID 
            AND SET_DocumentApprove.Status = 1 AND SET_DocumentApprove.DocumentTypeID = ARINVH.InvoiceType AND SET_DocumentApprove.DocumentNo = ARINVH.InvoiceNumber 
            AND SET_DocumentApprove.CreateDate = 
            (
                SELECT MAX(CreateDate) FROM SET_DocumentApprove 
                WHERE CompanyID = ARINVH.CompanyID AND PeriodID = ARINVH.PeriodID AND DocumentTypeID = ARINVH.InvoiceType AND DocumentNo = ARINVH.InvoiceNumber
                and SET_DocumentApprove.FiscalID = ARINVH.FiscalID
            )
        inner join Set_PeriodCustom on ARINVH.InvoiceDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate
        and Set_PeriodCustom.Status=1 --and SET_PeriodCustom.FiscalID = @CurrentFiscalID

";

                    #endregion
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, query + strWhere
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

        public static DataTable GetAllDataWithOtherTablesForApprove(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null,
            int? PageSize = null, Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, int CurrentFiscalID = 0)
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

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_SelectAll_Searching_WithPaging_ForApprove + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_SelectAll_Searching_WithPaging_ForApprove + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {


                    dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_SelectAll_Searching_ForApprove + strWhere
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

        public DataTable GetAllDataWithOtherTablesForApproveDetail(int companyID, int periodID, int invoiceID, int documentTypeID, string documentNo)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@InvoiceID", invoiceID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[4] = new SqlParameter("@DocumentNo", documentNo);

                #region Query

                string Query_SelectAll_ForApproveDetail = @" 
SELECT DISTINCT AR_InvoiceHeader.InvoiceID, AR_InvoiceHeader.CompanyID, AR_InvoiceHeader.PeriodID, AR_InvoiceHeader.Status, AR_InvoiceHeader.InvoiceType, 
    SET_DocumentType.DocumentType, AR_InvoiceHeader.InvoiceNumber, AR_InvoiceHeader.InvoiceDate, AR_InvoiceHeader.InvoiceReceivedDate, AR_InvoiceHeader.VendorID, 
    AR_SET_Customer.CustomerName AS VendorName, AR_InvoiceHeader.StatusCode, SET_DocumentApprove.ApprovalStatus, SET_UserApprovalAmount.ApprovalOperator, 
    SET_ApprovalAuthority.UserID AS UserID, SET_Authority.AuthorityLevel 
FROM AR_InvoiceHeader 
    INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = AR_InvoiceHeader.CompanyID AND SET_DocumentType.PeriodID = AR_InvoiceHeader.PeriodID 
        AND SET_DocumentType.DocumentTypeID = AR_InvoiceHeader.InvoiceType 
    Left JOIN AR_SET_Customer on AR_InvoiceHeader.VendorID = AR_SET_Customer.CustomerID --AND AR_InvoiceHeader.CompanyID = AR_SET_Customer.CompanyID 
        AND AR_InvoiceHeader.PeriodID = AR_SET_Customer.PeriodID AND AR_SET_Customer.Status = 1 
    INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = AR_InvoiceHeader.CompanyID AND SET_DocumentApprove.PeriodID = AR_InvoiceHeader.PeriodID 
        AND SET_DocumentApprove.Status = 1 AND SET_DocumentApprove.DocumentTypeID = AR_InvoiceHeader.InvoiceType 
        AND SET_DocumentApprove.DocumentNo = AR_InvoiceHeader.InvoiceNumber 
    INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = AR_InvoiceHeader.CompanyID AND SET_UserApprovalAmount.PeriodID = AR_InvoiceHeader.PeriodID 
        AND SET_UserApprovalAmount.Status = 1 
        AND (SET_UserApprovalAmount.DocumentTypeID = AR_InvoiceHeader.InvoiceType OR SET_UserApprovalAmount.DocumentTypeID IS NULL) 
        AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
                AND CAST(CONVERT(VARCHAR(10), AR_InvoiceHeader.InvoiceDate, 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1 
            WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
                AND CAST(CONVERT(VARCHAR(10), AR_InvoiceHeader.InvoiceDate, 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom AND SET_UserApprovalAmount.EffectiveDateTo 
                THEN 1 ELSE 0 END)) 
        AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND (SELECT ih.AmountPaid FROM AR_InvoiceHeader ih WHERE ih.CompanyID = AR_InvoiceHeader.CompanyID 
                AND ih.PeriodID = AR_InvoiceHeader.PeriodID AND ih.InvoiceID = AR_InvoiceHeader.InvoiceID) >= SET_UserApprovalAmount.AmountFrom THEN 1 
            WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND (SELECT ih.AmountPaid FROM AR_InvoiceHeader ih WHERE ih.CompanyID = AR_InvoiceHeader.CompanyID 
                AND ih.PeriodID = AR_InvoiceHeader.PeriodID AND ih.InvoiceID = AR_InvoiceHeader.InvoiceID) BETWEEN SET_UserApprovalAmount.AmountFrom 
                AND SET_UserApprovalAmount.AmountTo THEN 1 ELSE 0 END)) 
    INNER JOIN SET_ApprovalAuthority ON SET_ApprovalAuthority.ApprovalAuthorityID = SET_UserApprovalAmount.ApprovalAuthorityID 
        AND SET_ApprovalAuthority.CompanyID = SET_UserApprovalAmount.CompanyID AND SET_ApprovalAuthority.PeriodID = SET_UserApprovalAmount.PeriodID 
        AND SET_ApprovalAuthority.Status = 1 
    INNER JOIN SET_Authority ON SET_Authority.AuthorityID = SET_ApprovalAuthority.AuthorityID AND SET_Authority.CompanyID = SET_ApprovalAuthority.CompanyID 
AND SET_Authority.PeriodID = SET_ApprovalAuthority.PeriodID 
	AND SET_Authority.Status = 1
WHERE AR_InvoiceHeader.Status = 1 AND AR_InvoiceHeader.PeriodID = @PeriodID 
    AND AR_InvoiceHeader.CompanyID = @CompanyID AND AR_InvoiceHeader.InvoiceID = @InvoiceID AND AR_InvoiceHeader.InvoiceType = @DocumentTypeID 
    AND AR_InvoiceHeader.InvoiceNumber = @DocumentNo ";

                #endregion

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

        public DataTable GetDataByKeyForApprove(int companyID, int periodID, int invoiceID, int documentTypeID, string documentNo)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@InvoiceID", invoiceID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[4] = new SqlParameter("@DocumentNo", documentNo);

                #region Query

                string Query_Select_DataByKey_ForApprove = @" 
SELECT DISTINCT AR_InvoiceHeader.InvoiceID, AR_InvoiceHeader.CompanyID, AR_InvoiceHeader.PeriodID, AR_InvoiceHeader.Status, SET_UserApprovalAmount.ApprovalOperator, 
    SET_ApprovalAuthority.UserID AS UserID, SET_Authority.AuthorityLevel, SET_UserApprovalAmount.ApprovalAmountID, SET_UserApprovalAmount.ApprovalAuthorityID 
FROM AR_InvoiceHeader 
    INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = AR_InvoiceHeader.CompanyID AND SET_UserApprovalAmount.PeriodID = AR_InvoiceHeader.PeriodID 
        AND SET_UserApprovalAmount.Status = 1 
        AND (SET_UserApprovalAmount.DocumentTypeID = AR_InvoiceHeader.InvoiceType OR SET_UserApprovalAmount.DocumentTypeID IS NULL) 
        AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
                AND CAST(CONVERT(VARCHAR(10), AR_InvoiceHeader.InvoiceDate, 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1 
            WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL AND CAST(CONVERT(VARCHAR(10), AR_InvoiceHeader.InvoiceDate, 110) AS DATETIME) 
                BETWEEN SET_UserApprovalAmount.EffectiveDateFrom AND SET_UserApprovalAmount.EffectiveDateTo THEN 1 ELSE 0 END)) 
        AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND (SELECT ih.AmountPaid FROM AR_InvoiceHeader ih WHERE ih.CompanyID = AR_InvoiceHeader.CompanyID 
                AND ih.PeriodID = AR_InvoiceHeader.PeriodID AND ih.InvoiceID = AR_InvoiceHeader.InvoiceID) >= SET_UserApprovalAmount.AmountFrom THEN 1 
            WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND (SELECT ih.AmountPaid FROM AR_InvoiceHeader ih WHERE ih.CompanyID = AR_InvoiceHeader.CompanyID 
                AND ih.PeriodID = AR_InvoiceHeader.PeriodID AND ih.InvoiceID = AR_InvoiceHeader.InvoiceID) BETWEEN SET_UserApprovalAmount.AmountFrom 
                AND SET_UserApprovalAmount.AmountTo THEN 1 ELSE 0 END)) 
    INNER JOIN SET_ApprovalAuthority ON SET_ApprovalAuthority.ApprovalAuthorityID = SET_UserApprovalAmount.ApprovalAuthorityID 
        AND SET_ApprovalAuthority.CompanyID = SET_UserApprovalAmount.CompanyID AND SET_ApprovalAuthority.PeriodID = SET_UserApprovalAmount.PeriodID 
        AND SET_ApprovalAuthority.Status = 1 
    INNER JOIN SET_Authority ON SET_Authority.AuthorityID = SET_ApprovalAuthority.AuthorityID AND SET_Authority.CompanyID = SET_ApprovalAuthority.CompanyID 
        AND SET_Authority.PeriodID = SET_ApprovalAuthority.PeriodID AND SET_Authority.Status = 1 
    
INNER JOIN aspnet_UsersInRoles On aspnet_UsersInRoles.UserID = AR_InvoiceHeader.CreatedBy 
And (aspnet_UsersInRoles.RoleID = SET_UserApprovalAmount.RoleID or SET_UserApprovalAmount.RoleID is null)
And (aspnet_UsersInRoles.UserId = SET_UserApprovalAmount.UserId or SET_UserApprovalAmount.UserId is null)

        --new Working (check Roles Against Approver)
		inner join SET_DocumentType on SET_DocumentType.DocumentTypeID =  AR_InvoiceHeader.InvoiceType
		inner join ApplicationMenu on ApplicationMenu.MenuID = SET_DocumentType.MenuID
		inner join MenusInRoles on MenusInRoles.MenuID = SET_DocumentType.MenuID
		inner join aspnet_Roles on aspnet_Roles.RoleId =  MenusInRoles.RoleId

And (aspnet_UsersInRoles.RoleID = SET_UserApprovalAmount.RoleID or SET_UserApprovalAmount.RoleID is null) 
where AR_InvoiceHeader.CompanyID = @CompanyID and AR_InvoiceHeader.PeriodID = @PeriodID and AR_InvoiceHeader.InvoiceID = @InvoiceID 
    and AR_InvoiceHeader.InvoiceType = @DocumentTypeID and AR_InvoiceHeader.InvoiceNumber = @DocumentNo
";

                #endregion

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

        public DataTable GetVendorAgingReportData(int companyID, int periodID, DateTime asOnDate, Nullable<short> businessPartnerType = null,
            Nullable<int> businessPartnerIDFrom = null, Nullable<int> businessPartnerIDTo = null)
        {
            DataTable dtToReturn = null;
            string strFilter = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@AsOnDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(asOnDate, true) + " 23:59:59"));

                #region Filter by Business Partner Type Code

                if (businessPartnerType.HasValue)
                    strFilter = " and AR_SET_BusinessPartnerTypes.TypeCode = " + businessPartnerType.HasValue + " ";

                #endregion

                #region Filter by Business Partners

                if (businessPartnerIDFrom.HasValue && businessPartnerIDTo.HasValue)
                {
                    strFilter = " and AR_SET_Customer.CustomerID BETWEEN " + businessPartnerIDFrom.Value + " AND " + businessPartnerIDTo.Value + "";
                }
                else if (businessPartnerIDFrom.HasValue)
                {
                    strFilter = " AND AR_SET_Customer.CustomerID >= " + businessPartnerIDFrom.Value + "";
                }
                else if (businessPartnerIDTo.HasValue)
                {
                    strFilter = "AND AR_SET_Customer.CustomerID <= " + businessPartnerIDFrom.Value + "";
                    
                }
                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_AgingVendor, strFilter), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dtToReturn = ds.Tables[0];

                #region Knock off Payment and Sales Invoice amount from Aging Purchase Invoice and Receipt Amount

                //if (dtToReturn != null)
                //{
                //    DataTable dtPaymentSalesInvoices = ds.Tables[1];

                //    foreach (DataRow drPaymSalInvo in dtPaymentSalesInvoices.Rows)
                //    {
                //        decimal knockOffAmount = Convert.ToDecimal(drPaymSalInvo.Field<object>("Amount"));

                //        dtToReturn.AsEnumerable().Where(x => x.Field<int>("CustomerID") == drPaymSalInvo.Field<int>("CustomerID")
                //            && Convert.ToInt16(x.Field<object>("ToNoOfDays")) != -1).OrderByDescending(o => Convert.ToInt16(o.Field<object>("ToNoOfDays")))
                //            .ToList<DataRow>().ForEach(s =>
                //            {
                //                decimal invoiceAmount = Convert.ToDecimal(s.Field<object>("Amount"));

                //                if (invoiceAmount != 0)
                //                {
                //                    if (Math.Abs(invoiceAmount) >= Math.Abs(knockOffAmount))
                //                    {
                //                        s.SetField("Amount", (Math.Abs(invoiceAmount) - Math.Abs(knockOffAmount)) * (invoiceAmount < 0 ? -1 : 1));
                //                        knockOffAmount = 0;
                //                    }
                //                    else
                //                    {
                //                        s.SetField("Amount", 0);
                //                        knockOffAmount -= Math.Abs(invoiceAmount);
                //                    }
                //                }
                //            });
                //    }
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

        public DataTable GetVendorAgingReportDataForDashBoard(string companyID, int periodID, DateTime asOnDate, Nullable<short> businessPartnerType = null,
           Nullable<int> businessPartnerIDFrom = null, Nullable<int> businessPartnerIDTo = null)
        {
            DataTable dtToReturn = null;
            string strFilter = string.Empty;

            companyID = "(" + companyID + ")";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@AsOnDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(asOnDate, true) + " 23:59:59"));

                #region Filter by Business Partner Type Code

                if (businessPartnerType.HasValue)
                    strFilter = " and AR_SET_BusinessPartnerTypes.TypeCode = " + businessPartnerType.HasValue + " ";

                #endregion

                #region Filter by Business Partners

                if (businessPartnerIDFrom.HasValue && businessPartnerIDTo.HasValue)
                {
                    strFilter = " and AR_SET_Customer.CustomerID BETWEEN " + businessPartnerIDFrom.Value + " AND " + businessPartnerIDTo.Value + "";
                }
                else if (businessPartnerIDFrom.HasValue)
                {
                    strFilter = " AND AR_SET_Customer.CustomerID >= " + businessPartnerIDFrom.Value + "";
                }
                else if (businessPartnerIDTo.HasValue)
                {
                    strFilter = "AND AR_SET_Customer.CustomerID <= " + businessPartnerIDFrom.Value + "";

                }
                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_AgingVendorDashBoard.Replace("@companyId", companyID), strFilter), sqlparams);
                //DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_POGRNBalanceReport_New.Replace("@CompanyID", CompanyID), sb.ToString(), sb1.ToString()), sqlparams);
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

        public DataTable GetPayablleAgingReportData(string companyID, int periodID, DateTime asOnDate, Nullable<short> businessPartnerType = null,
            Nullable<int> businessPartnerIDFrom = null, Nullable<int> businessPartnerIDTo = null, Nullable<int> documentGroupID = null, int CurrentFiscalID = 0
            , bool IsSummary = false)
        {
            DataTable dtToReturn = null;
            string strFilter = string.Empty;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[10];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@AsOnDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(asOnDate, true) + " 23:59:59"));

                if (businessPartnerType.HasValue)
                    sqlparams[3] = new SqlParameter("@BusinessPartnerType", businessPartnerType.Value);
                else
                    sqlparams[3] = new SqlParameter("@BusinessPartnerType", DBNull.Value);

                if (businessPartnerIDFrom.HasValue && businessPartnerIDTo.HasValue)
                {
                    sb.Append("and [AR_SET_Customer].CustomerID = @CustomerID");
                }
                else if (businessPartnerIDFrom.HasValue)
                    sb.Append("and [AR_SET_Customer].CustomerID = @CustomerID");
                else if (businessPartnerIDTo.HasValue)
                    sb.Append("and [AR_SET_Customer].CustomerID = @CustomerID");

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

                if (documentGroupID.HasValue)
                    sqlparams[6] = new SqlParameter("@DocumentGroupID", documentGroupID.Value);
                else
                    sqlparams[6] = new SqlParameter("@DocumentGroupID", DBNull.Value);


                sqlparams[7] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[8] = new SqlParameter("@CustomerID", businessPartnerIDFrom);
                sqlparams[9] = new SqlParameter("@FromDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(asOnDate, true) + " 23:59:59"));


                string Query = "";
                if (IsSummary)
                    Query = FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_PayableAgingVendorSummary;
                else
                    Query = FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_PayableAgingVendor;

                companyID = "(" + companyID + ")";
                //DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, strFilter), sqlparams);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_Aging.ToString().Replace("@CompanyID", companyID), sb.ToString()), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dtToReturn = ds.Tables[0];

                #region Knock off Payment and Sales Invoice amount from Aging Purchase Invoice and Receipt Amount

                //if (dtToReturn != null)
                //{
                //    DataTable dtPaymentSalesInvoices = ds.Tables[1];

                //    foreach (DataRow drPaymSalInvo in dtPaymentSalesInvoices.Rows)
                //    {
                //        decimal knockOffAmount = Convert.ToDecimal(drPaymSalInvo.Field<object>("Amount"));

                //        dtToReturn.AsEnumerable().Where(x => x.Field<int>("CustomerID") == drPaymSalInvo.Field<int>("CustomerID")
                //            && Convert.ToInt16(x.Field<object>("ToNoOfDays")) != -1).OrderByDescending(o => Convert.ToInt16(o.Field<object>("ToNoOfDays")))
                //            .ToList<DataRow>().ForEach(s =>
                //            {
                //                decimal invoiceAmount = Convert.ToDecimal(s.Field<object>("Amount"));

                //                if (invoiceAmount != 0)
                //                {
                //                    if (Math.Abs(invoiceAmount) >= Math.Abs(knockOffAmount))
                //                    {
                //                        s.SetField("Amount", (Math.Abs(invoiceAmount) - Math.Abs(knockOffAmount)) * (invoiceAmount < 0 ? -1 : 1));
                //                        knockOffAmount = 0;
                //                    }
                //                    else
                //                    {
                //                        s.SetField("Amount", 0);
                //                        knockOffAmount -= Math.Abs(invoiceAmount);
                //                    }
                //                }
                //            });
                //    }
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

        public DataTable GetCustomerAgingReportData(int companyID, int periodID, DateTime asOnDate, Nullable<short> businessPartnerType = null,Nullable<int> CostCenterID = null,
            Nullable<int> businessPartnerIDFrom = null, Nullable<int> businessPartnerIDTo = null, Nullable<int> brokerIDFrom = null, Nullable<int> brokerIDTo = null, Nullable<int> documentGroupID = null, Nullable<int> DocumentTypeID = null)
        {
            DataTable dtToReturn = null;
            string strFilter = string.Empty;
            string strFilterBroker = string.Empty;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[10];
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

                #region Filter by Broker

                if (brokerIDFrom.HasValue && brokerIDTo.HasValue)
                {
                    strFilterBroker = " AND AR_SET_CustomerBroker.CustomerID BETWEEN @BrokerIDFrom AND @BrokerIDTo ";
                    sqlparams[7] = new SqlParameter("@BrokerIDFrom", brokerIDFrom.Value);
                    sqlparams[8] = new SqlParameter("@BrokerIDTo", brokerIDTo.Value);
                }
                else if (brokerIDFrom.HasValue)
                {
                    strFilterBroker = " AND AR_SET_CustomerBroker.CustomerID >= @BrokerIDFrom ";
                    sqlparams[7] = new SqlParameter("@BrokerIDFrom", brokerIDFrom.Value);
                }
                else if (brokerIDTo.HasValue)
                {
                    strFilterBroker = "AND AR_SET_CustomerBroker.CustomerID <= @BrokerIDFrom";
                    sqlparams[8] = new SqlParameter("@BrokerIDTo", brokerIDTo.Value);
                }

                #endregion

                if (documentGroupID.HasValue)
                    sqlparams[6] = new SqlParameter("@DocumentGroupID", documentGroupID.Value);
                else
                    sqlparams[6] = new SqlParameter("@DocumentGroupID", DBNull.Value);


                if (DocumentTypeID.HasValue)
                    sqlparams[9] = new SqlParameter("@DocumentTypeID", DocumentTypeID.Value);
                else
                    sqlparams[9] = new SqlParameter("@DocumentTypeID", DBNull.Value);


                

                if (CostCenterID.HasValue)
                {
                    sb.Append(" where FinalTab.CostCenterID =  " + CostCenterID.Value);
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_CustomerAgingVendor, strFilter, strFilterBroker, sb.ToString()), sqlparams);

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

        public DataTable GetCustomerAgingReportData_HSM(int companyID, int periodID, DateTime asOnDate, Nullable<short> businessPartnerType = null, Nullable<int> CostCenterID = null,
            Nullable<int> businessPartnerIDFrom = null, Nullable<int> businessPartnerIDTo = null, Nullable<int> brokerIDFrom = null, Nullable<int> brokerIDTo = null, Nullable<int> documentGroupID = null, Nullable<int> DocumentTypeID = null)
        {
            DataTable dtToReturn = null;
            string strFilter = string.Empty;
            string strFilterBroker = string.Empty;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            StringBuilder strFilterCTE = new StringBuilder();
            bool AllCustomer = true;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[10];
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
                    AllCustomer = false;
                    strFilterCTE.Append(" AND CustomerID BETWEEN @BusinessPartnerIDFrom AND @BusinessPartnerIDTo ");
                    sb.Append( " AND AR_SET_Customer.CustomerID BETWEEN @BusinessPartnerIDFrom AND @BusinessPartnerIDTo ");
                    sqlparams[4] = new SqlParameter("@BusinessPartnerIDFrom", businessPartnerIDFrom.Value);
                    sqlparams[5] = new SqlParameter("@BusinessPartnerIDTo", businessPartnerIDTo.Value);
                }
                else if (businessPartnerIDFrom.HasValue)
                {
                    sb.Append( " AND AR_SET_Customer.CustomerID >= @BusinessPartnerIDFrom ");
                    sqlparams[4] = new SqlParameter("@BusinessPartnerIDFrom", businessPartnerIDFrom.Value);
                }
                else if (businessPartnerIDTo.HasValue)
                {
                    sb.Append( "AND AR_SET_Customer.CustomerID <= @BusinessPartnerIDFrom");
                    sqlparams[5] = new SqlParameter("@BusinessPartnerIDTo", businessPartnerIDTo.Value);
                }

                if (businessPartnerIDFrom.HasValue && businessPartnerIDTo.HasValue)
                {
                    sb2.Append(" AND AP_PaymentSupplierDetail.SupplierID BETWEEN @BusinessPartnerIDFrom AND @BusinessPartnerIDTo ");
                    sqlparams[4] = new SqlParameter("@BusinessPartnerIDFrom", businessPartnerIDFrom.Value);
                    sqlparams[5] = new SqlParameter("@BusinessPartnerIDTo", businessPartnerIDTo.Value);
                }
                else if (businessPartnerIDFrom.HasValue)
                {
                    sb2.Append(" AND AP_PaymentSupplierDetail.SupplierID >= @BusinessPartnerIDFrom ");
                    sqlparams[4] = new SqlParameter("@BusinessPartnerIDFrom", businessPartnerIDFrom.Value);
                }
                else if (businessPartnerIDTo.HasValue)
                {
                    sb2.Append(" AND AP_PaymentSupplierDetail.SupplierID <= @BusinessPartnerIDFrom");
                    sqlparams[5] = new SqlParameter("@BusinessPartnerIDTo", businessPartnerIDTo.Value);
                }

                #endregion

                #region Filter by Broker

                if (brokerIDFrom.HasValue && brokerIDTo.HasValue)
                {
                    AllCustomer = false;
                    strFilterCTE.Append("AND BrokerID BETWEEN @BrokerIDFrom AND @BrokerIDTo ");
                    strFilterBroker = " AND AR_SET_CustomerBroker.CustomerID BETWEEN @BrokerIDFrom AND @BrokerIDTo ";
                    sqlparams[7] = new SqlParameter("@BrokerIDFrom", brokerIDFrom.Value);
                    sqlparams[8] = new SqlParameter("@BrokerIDTo", brokerIDTo.Value);
                }
                else if (brokerIDFrom.HasValue)
                {
                    strFilterBroker = " AND AR_SET_CustomerBroker.CustomerID >= @BrokerIDFrom ";
                    sqlparams[7] = new SqlParameter("@BrokerIDFrom", brokerIDFrom.Value);
                }
                else if (brokerIDTo.HasValue)
                {
                    strFilterBroker = "AND AR_SET_CustomerBroker.CustomerID <= @BrokerIDFrom";
                    sqlparams[8] = new SqlParameter("@BrokerIDTo", brokerIDTo.Value);
                }

                #endregion

                if (documentGroupID.HasValue)
                    sqlparams[6] = new SqlParameter("@DocumentGroupID", documentGroupID.Value);
                else
                    sqlparams[6] = new SqlParameter("@DocumentGroupID", DBNull.Value);


                if (DocumentTypeID.HasValue)
                    sqlparams[9] = new SqlParameter("@DocumentTypeID", DocumentTypeID.Value);
                else
                    sqlparams[9] = new SqlParameter("@DocumentTypeID", DBNull.Value);

                if (AllCustomer)
                    strFilterCTE.Append("and CustomerID in (select distinct CustomerID  from ( select distinct CustomerID from SAL_InvoiceHeader union all select distinct CustomerID from Sal_InvoiceCustomerDetail union all select distinct VendorID as CustomerID  from AR_InvoiceHeader union all select distinct SupplierID as CustomerID  from AR_InvoiceSupplierDetail )as tab )");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_CustomerAgingVendor_HSM, sb.ToString(), sb2.ToString(), strFilterBroker, strFilterCTE.ToString()), sqlparams);

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

        public DataTable GetCreditAgingReportData_HSM(int companyID, int periodID, DateTime asOnDate, Nullable<short> businessPartnerType = null, Nullable<int> CostCenterID = null,
            Nullable<int> businessPartnerIDFrom = null, Nullable<int> businessPartnerIDTo = null, Nullable<int> brokerIDFrom = null, Nullable<int> brokerIDTo = null, Nullable<int> documentGroupID = null, Nullable<int> DocumentTypeID = null)
        {
            DataTable dtToReturn = null;
            string strFilter = string.Empty;
            string strFilterBroker = string.Empty;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[10];
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
                    sb.Append(" AND AR_SET_Customer.CustomerID BETWEEN @BusinessPartnerIDFrom AND @BusinessPartnerIDTo ");
                    sqlparams[4] = new SqlParameter("@BusinessPartnerIDFrom", businessPartnerIDFrom.Value);
                    sqlparams[5] = new SqlParameter("@BusinessPartnerIDTo", businessPartnerIDTo.Value);
                }
                else if (businessPartnerIDFrom.HasValue)
                {
                    sb.Append(" AND AR_SET_Customer.CustomerID >= @BusinessPartnerIDFrom ");
                    sqlparams[4] = new SqlParameter("@BusinessPartnerIDFrom", businessPartnerIDFrom.Value);
                }
                else if (businessPartnerIDTo.HasValue)
                {
                    sb.Append("AND AR_SET_Customer.CustomerID <= @BusinessPartnerIDFrom");
                    sqlparams[5] = new SqlParameter("@BusinessPartnerIDTo", businessPartnerIDTo.Value);
                }

                if (businessPartnerIDFrom.HasValue && businessPartnerIDTo.HasValue)
                {
                    sb2.Append(" AND AP_PaymentSupplierDetail.SupplierID BETWEEN @BusinessPartnerIDFrom AND @BusinessPartnerIDTo ");
                    sqlparams[4] = new SqlParameter("@BusinessPartnerIDFrom", businessPartnerIDFrom.Value);
                    sqlparams[5] = new SqlParameter("@BusinessPartnerIDTo", businessPartnerIDTo.Value);
                }
                else if (businessPartnerIDFrom.HasValue)
                {
                    sb2.Append(" AND AP_PaymentSupplierDetail.SupplierID >= @BusinessPartnerIDFrom ");
                    sqlparams[4] = new SqlParameter("@BusinessPartnerIDFrom", businessPartnerIDFrom.Value);
                }
                else if (businessPartnerIDTo.HasValue)
                {
                    sb2.Append(" AND AP_PaymentSupplierDetail.SupplierID <= @BusinessPartnerIDFrom");
                    sqlparams[5] = new SqlParameter("@BusinessPartnerIDTo", businessPartnerIDTo.Value);
                }

                #endregion

                #region Filter by Broker

                if (brokerIDFrom.HasValue && brokerIDTo.HasValue)
                {
                    strFilterBroker = " AND AR_SET_CustomerBroker.CustomerID BETWEEN @BrokerIDFrom AND @BrokerIDTo ";
                    sqlparams[7] = new SqlParameter("@BrokerIDFrom", brokerIDFrom.Value);
                    sqlparams[8] = new SqlParameter("@BrokerIDTo", brokerIDTo.Value);
                }
                else if (brokerIDFrom.HasValue)
                {
                    strFilterBroker = " AND AR_SET_CustomerBroker.CustomerID >= @BrokerIDFrom ";
                    sqlparams[7] = new SqlParameter("@BrokerIDFrom", brokerIDFrom.Value);
                }
                else if (brokerIDTo.HasValue)
                {
                    strFilterBroker = "AND AR_SET_CustomerBroker.CustomerID <= @BrokerIDFrom";
                    sqlparams[8] = new SqlParameter("@BrokerIDTo", brokerIDTo.Value);
                }

                #endregion

                if (documentGroupID.HasValue)
                    sqlparams[6] = new SqlParameter("@DocumentGroupID", documentGroupID.Value);
                else
                    sqlparams[6] = new SqlParameter("@DocumentGroupID", DBNull.Value);


                if (DocumentTypeID.HasValue)
                    sqlparams[9] = new SqlParameter("@DocumentTypeID", DocumentTypeID.Value);
                else
                    sqlparams[9] = new SqlParameter("@DocumentTypeID", DBNull.Value);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_CreditAgingVendor_HSM, sb.ToString(), sb2.ToString(), strFilterBroker), sqlparams);

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

        public DataTable GetCustomerAgingReportData2(int companyID, int periodID, DateTime asOnDate, Nullable<short> businessPartnerType = null,
            Nullable<int> businessPartnerIDFrom = null, Nullable<int> businessPartnerIDTo = null, Nullable<int> brokerIDFrom = null, Nullable<int> brokerIDTo = null, Nullable<int> documentGroupID = null)
        {
            DataTable dtToReturn = null;
            string strFilter = string.Empty;
            string strFilterBroker = string.Empty;

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

                #region Filter by Broker

                if (brokerIDFrom.HasValue && brokerIDTo.HasValue)
                {
                    strFilterBroker = " AND AR_SET_CustomerBroker.CustomerID BETWEEN @BrokerIDFrom AND @BrokerIDTo ";
                    sqlparams[7] = new SqlParameter("@BrokerIDFrom", brokerIDFrom.Value);
                    sqlparams[8] = new SqlParameter("@BrokerIDTo", brokerIDTo.Value);
                }
                else if (brokerIDFrom.HasValue)
                {
                    strFilterBroker = " AND AR_SET_CustomerBroker.CustomerID >= @BrokerIDFrom ";
                    sqlparams[7] = new SqlParameter("@BrokerIDFrom", brokerIDFrom.Value);
                }
                else if (brokerIDTo.HasValue)
                {
                    strFilterBroker = "AND AR_SET_CustomerBroker.CustomerID <= @BrokerIDFrom";
                    sqlparams[8] = new SqlParameter("@BrokerIDTo", brokerIDTo.Value);
                }

                #endregion

                if (documentGroupID.HasValue)
                    sqlparams[6] = new SqlParameter("@DocumentGroupID", documentGroupID.Value);
                else
                    sqlparams[6] = new SqlParameter("@DocumentGroupID", DBNull.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_CustomerAgingVendor2, strFilter, strFilterBroker), sqlparams);

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

        public DataTable GetCustomerAgeAnalysisReport(int companyID, int periodID, DateTime asOnDate, Nullable<short> businessPartnerType = null,
           Nullable<int> businessPartnerIDFrom = null, Nullable<int> businessPartnerIDTo = null)
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


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_CustomerAgeAnalysisReport, strFilter), sqlparams);

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

        public DataTable GetDataByKeyTableDashboard(int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                string Query_AgingVendorDashboard = @" 
SELECT TOP 7 SET_Bucket.Description, AR_SET_Customer.CustomerName AS SupplierName, SET_Currency.CurrencyName, AR_InvoiceHeader.AmountPaid, SET_Bucket.ToNoOfDays, 
    AR_InvoiceHeader.InvoiceDate, 
    (CASE WHEN DATEDIFF(day, AR_InvoiceHeader.InvoiceDate, GETDATE()) between FromNoOfDays AND tonoofdays THEN amountpaid ELSE 0 END) AS Amount 
FROM AR_InvoiceHeader 
    INNER JOIN SET_Company ON AR_InvoiceHeader.CompanyID = SET_Company.CompanyId 
    INNER JOIN SET_Bucket ON AR_InvoiceHeader.CompanyID = SET_Bucket.CompanyID and SET_Bucket.Status = 1 and SET_Bucket.IsActive = 1 
    INNER JOIN SET_Currency ON AR_InvoiceHeader.InvoiceCurrencyID = SET_Currency.CurrencyID 
    LEFT JOIN AR_SET_Customer on AR_InvoiceHeader.VendorID = AR_SET_Customer.CustomerID --and AR_InvoiceHeader.CompanyID = AR_SET_Customer.CompanyID 
        and AR_InvoiceHeader.PeriodID = AR_SET_Customer.PeriodID and AR_SET_Customer.Status = 1 
    LEFT JOIN AR_SET_BusinessPartnerTypes BP on AR_SET_Customer.CustomerID = BP.BusinessPartnerID --and AR_SET_Customer.CompanyID = BP.CompanyID 
        and AR_SET_Customer.PeriodID = BP.PeriodID 
WHERE AR_InvoiceHeader.CompanyId = @CompanyID and AR_InvoiceHeader.PeriodID = @PeriodID and AR_InvoiceHeader.Status = 1 AND BP.TypeCode = 1 
GROUP BY SET_Bucket.Description, AR_SET_Customer.CustomerName, SET_Currency.CurrencyName, AR_InvoiceHeader.AmountPaid, SET_Bucket.FromNoOfDays, SET_Bucket.ToNoOfDays, 
    AR_InvoiceHeader.InvoiceDate; ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_AgingVendorDashboard, sqlparams);

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

        public bool InsertTransactionHeaderAndDetail(List<AR_InvoiceDetail> colTranDetail, GL_TransactionHeader colGLTransHeader,
            List<GL_TransactionDetail> colGLTransDetail, List<AR_Invoice_AccountsDetail> colInvoiceAccountDetail,
            List<AR_InvoiceSupplierDetail> colInvoiceSupplierDetail, SET_DocumentApprove da, double lessDiscount = 0, bool MultiGRN = false, int CurrentFiscalID = 0, List<AR_InvoiceAdvancePaymentDetail> coladvancePaymentDtl = null)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertTransactionHeaderAndDetail");

            try
            {
                this.InvoiceNumber = SET_DocumentSequence.ManualDocumentSequence(f_InvoiceType, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID).ToString();
                this.InvoiceID = AR_InvoiceHeader.GetNewInvoiceID(f_CompanyID, f_PeriodID, sqltrans);

                this.FiscalID = CurrentFiscalID;

                if (lessDiscount > 0)
                    this.AmountPaid = AmountPaid - lessDiscount;


                #region If 'Advance against PO' amount is greater than Invoice amount, then this Invoice must not be displayed upon Payment



                #endregion

                if (this.AmountPaid != null && this.AdvanceAmount > 0 && this.AdvanceAmount > decimal.Parse(this.AmountPaid.ToString()) )
                    this.AdvanceAmount = decimal.Parse(this.AmountPaid.ToString());

                this.Insert(sqltrans);

                short invoiceDetailId = (short)AR_InvoiceDetail.GetNewInvoiceDetailID(f_CompanyID, this.InvoiceID, f_PeriodID, sqltrans);
                decimal POQty = 0;
                List<AP_PurchaseOrderHeader> listPoIDs = new List<AP_PurchaseOrderHeader>();

                foreach (AR_InvoiceDetail td in colTranDetail)
                {
                    if (td.InvoiceDetailID.Equals(-99))
                    {
                        td.InvoiceHeaderID = this.InvoiceID;
                        td.InvoiceDetailID = invoiceDetailId++;
                        td.FiscalID = CurrentFiscalID;
                        td.Insert(sqltrans);
                    }
                    else
                        td.UpdateDataByKey(sqltrans);

                    #region Update Status Code = 2 when Vendor Invoice Done

                    int POID = int.Parse(td.PurchaseOrderHeaderID.ToString() == "" ? "0" : td.PurchaseOrderHeaderID.ToString());
                    AP_PurchaseOrderHeader Pur = new AP_PurchaseOrderHeader(td.CompanyID, td.PeriodID, POID, null, sqltrans);

                    if (listPoIDs.Where(c => c.POHeaderID == Pur.POHeaderID).Count() == 0)
                        listPoIDs.Add(Pur);

                    int DocumentNo = AP_PurchaseOrderHeader.Get_POHeaderForVendorInvoiceInteger(td.CompanyID, td.PeriodID, td.PurchaseOrderHeaderID, sqltrans);

                    DataTable dt_QuantityOfPO = AP_PurchaseOrderHeader.GetQuantityOfPurchaseOrder(td.CompanyID, td.PeriodID, td.ItemID, DocumentNo,
                        Convert.ToInt64(td.StockHeaderID), Convert.ToInt64(td.StockDetailID), sqltrans);

                    int quantityvendorinvoice = AP_PurchaseOrderHeader.GetQuantityOfVendorInvoice(td.CompanyID, td.PeriodID, td.ItemID, Pur.POHeaderID, sqltrans);

                    POQty = POQty + quantityvendorinvoice;

                    if (dt_QuantityOfPO.Rows.Count > 0)
                    {
                        if (quantityvendorinvoice >= decimal.Parse(dt_QuantityOfPO.Rows[0]["Quantity"].ToString()))
                        {
                            INV_StockTransactionHeader.Update_TransactionStatusForGRN(td.CompanyID, td.PeriodID, Convert.ToInt64(td.StockHeaderID), Convert.ToInt64(td.StockDetailID), sqltrans);
                            AP_PurchaseOrderDetail.Update_CloseCodeForPO(td.CompanyID, td.PeriodID, Convert.ToInt64(td.PurchaseOrderHeaderID), Convert.ToInt64(td.PurchaseOrderDetailID), sqltrans);
                        }

                    }


                    #endregion
                }

                // PO Closed
                foreach (AP_PurchaseOrderHeader pur in listPoIDs)
                {
                    DataTable dt = AP_PurchaseOrderDetail.GetAllActiveData(f_CompanyID, f_PeriodID, pur.POHeaderID, true, null, sqltrans);
                    DataRow[] dr = dt.Select("ClosedCode = '3'  or  ClosedCode = '13'  or  ClosedCode is null ");

                    if (dr.Length == 0)
                        AP_PurchaseOrderHeader.Get_UpdatePOHeaderClosedCodeForCloseForInvoice(f_CompanyID, f_PeriodID, Convert.ToInt64(pur.POHeaderID), sqltrans);
                }

                #region INsertion in AR_InvoiceAccountDetail

                if (colInvoiceAccountDetail != null && colInvoiceAccountDetail.Count > 0)
                {
                    short invoiceAccountDetailID = (short)AR_Invoice_AccountsDetail.GetNewInvoiceDetailAccountID(f_CompanyID, this.InvoiceID, f_PeriodID, sqltrans);

                    foreach (AR_Invoice_AccountsDetail td in colInvoiceAccountDetail)
                    {
                        td.InvoiceHeaderID = this.InvoiceID;
                        td.InvoiceDetailAccountID = invoiceAccountDetailID++;
                        td.FiscalID = CurrentFiscalID;
                        td.Insert(sqltrans);

                        var filteredRows = from detail in colGLTransDetail
                                           where (detail.TransactionType == td.TransactionType && detail.CodeCombinationID == td.CodeCombinationID)
                                           select detail;

                        foreach (var filteredRow in filteredRows)
                        {
                            filteredRow.SourceHeaderID = this.InvoiceID;
                            filteredRow.SourceDetailID = td.InvoiceDetailAccountID;
                        }


                    }
                }

                #endregion

                #region Insertion in AR_InvoiceSupplierDetail

                if (colInvoiceSupplierDetail != null && colInvoiceSupplierDetail.Count > 0)
                {
                    short invoiceSupplierID = Convert.ToInt16(AR_InvoiceSupplierDetail.GetNewInvoiceSupplierID(f_CompanyID, this.InvoiceID, f_PeriodID, sqltrans));

                    foreach (AR_InvoiceSupplierDetail invoiceSupplier in colInvoiceSupplierDetail)
                    {
                        invoiceSupplier.InvoiceSupplierID = invoiceSupplierID++;
                        invoiceSupplier.InvoiceHeaderID = this.InvoiceID;
                        invoiceSupplier.FiscalID = CurrentFiscalID;
                        invoiceSupplier.Insert(sqltrans);
                    }
                }

                #endregion

                #region Insertion in Advance Payment

                if (coladvancePaymentDtl != null && coladvancePaymentDtl.Count > 0)
                {
                    short InvoiceDetailID = Convert.ToInt16(AR_InvoiceAdvancePaymentDetail.GetNewInvoiceDetailID(f_CompanyID, f_PeriodID, sqltrans));

                    foreach (AR_InvoiceAdvancePaymentDetail advpayment in coladvancePaymentDtl)
                    {
                        advpayment.InvoiceHeaderID = this.InvoiceID;
                        advpayment.InvoiceDetailID = InvoiceDetailID++;
                        advpayment.Insert(sqltrans);
                    }
                }

                #endregion

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                {
                    #region Insert In GL_TransactionHeader

                    if (colGLTransDetail.Count > 0)
                    {
                        colGLTransHeader.DocumentNo = int.Parse(f_InvoiceNumber);
                        colGLTransHeader.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                        colGLTransHeader.FiscalID = CurrentFiscalID;
                        colGLTransHeader.Insert(sqltrans);
                    }

                    #endregion

                    #region Insert In GL_TransactionDetail

                    decimal AmountDebit = Math.Round(colGLTransDetail.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                    decimal AmountCredit = Math.Round(colGLTransDetail.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                    if (AmountDebit != AmountCredit)
                        throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");


                    foreach (GL_TransactionDetail TranD in colGLTransDetail)
                    {
                        TranD.TransactionID = colGLTransHeader.TransactionID;
                        TranD.SourceHeaderID = this.InvoiceID;
                        TranD.FiscalID = CurrentFiscalID;
                        // TranD.SourceDetailID = Sdetail++;
                        TranD.Insert(sqltrans);
                    }

                    #endregion
                }

                #endregion

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)))
                {
                    da.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
                    da.DocumentNo = this.InvoiceNumber;
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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertTransactionHeaderAndDetail");
            }

            return result;
        }

        public bool UpdateTransactionHederAndDetail(List<AR_InvoiceDetail> colTranDetailAdd, List<AR_InvoiceDetail> colTranDetailEdit,
            List<AR_InvoiceDetail> colTranDetailDelete, GL_TransactionHeader colGLTransHeader, List<GL_TransactionDetail> colGLTransDetailUpate, List<AR_Invoice_AccountsDetail> colInvoiceAccountEdit, List<AR_Invoice_AccountsDetail> colInvoiceAccountAdd, List<AR_Invoice_AccountsDetail> colInvoiceAccountDelete,
            int currentChartOfAccID, SET_DocumentApprove da, DataTable newdt = null, bool IsAccountDetail = false)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateTransactionHederAndDetail");

            try
            {
                this.UpdateDataByKey(sqltrans);



                foreach (AR_InvoiceDetail tdAdd in colTranDetailAdd)
                {
                    tdAdd.InvoiceHeaderID = this.InvoiceID;
                    tdAdd.InvoiceDetailID = Convert.ToInt16(AR_InvoiceDetail.GetNewInvoiceDetailID(tdAdd.CompanyID, tdAdd.InvoiceHeaderID, tdAdd.PeriodID, sqltrans));
                    tdAdd.Insert(sqltrans);
                }

                foreach (AR_InvoiceDetail tdUpdate in colTranDetailEdit)
                    tdUpdate.UpdateDataByKey(sqltrans);

                foreach (AR_InvoiceDetail tdDelete in colTranDetailDelete)
                    AR_InvoiceDetail.DeleteDataByKey(sqltrans, tdDelete.UpdatedBy.Value, tdDelete.UpdateDate.Value, tdDelete.UpdatedByValue, tdDelete.CompanyID,
                        tdDelete.InvoiceDetailID, tdDelete.InvoiceHeaderID, tdDelete.PeriodID);

                foreach (AR_Invoice_AccountsDetail tdAddDetail in colInvoiceAccountAdd)
                {
                    tdAddDetail.InvoiceHeaderID = this.InvoiceID;
                    tdAddDetail.InvoiceDetailAccountID = (short)AR_Invoice_AccountsDetail.GetNewInvoiceDetailAccountID(tdAddDetail.CompanyID, this.InvoiceID, tdAddDetail.PeriodID);
                    tdAddDetail.Insert(sqltrans);
                }

                foreach (AR_Invoice_AccountsDetail tdEditDetail in colInvoiceAccountEdit)
                {
                    tdEditDetail.InvoiceHeaderID = this.InvoiceID;
                    tdEditDetail.UpdateDataByKey(sqltrans);

                }
                foreach (AR_Invoice_AccountsDetail tdEditDetail in colInvoiceAccountDelete)
                {
                    AR_Invoice_AccountsDetail.DeleteDataByKey(sqltrans, tdEditDetail.UpdatedBy.Value, tdEditDetail.UpdateDate.Value, tdEditDetail.UpdatedByValue, tdEditDetail.CompanyID, tdEditDetail.InvoiceDetailAccountID, this.InvoiceID, tdEditDetail.PeriodID);

                }



                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"


                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                {
                    #region Insert In GL_TransactionHeader

                    colGLTransHeader.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    colGLTransHeader.Insert(sqltrans);

                    #endregion

                    if (IsAccountDetail == true && newdt != null)
                    {
                        #region Insert In GL_TransactionDetail
                        short i = 0;

                        decimal AmountDebit = Math.Round(colGLTransDetailUpate.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                        decimal AmountCredit = Math.Round(colGLTransDetailUpate.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                        if (AmountDebit != AmountCredit)
                            throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");

                        foreach (GL_TransactionDetail td in colGLTransDetailUpate)
                        {
                            td.SourceHeaderID = this.InvoiceID;
                            td.SourceDetailID = Convert.ToInt16(newdt.Rows[i]["InvoiceDetailAccountID"].ToString());
                            td.TransactionID = colGLTransHeader.TransactionID;
                            td.Insert(sqltrans);
                            i++;
                        }

                        #endregion
                    }
                    else
                    {
                        #region Insert In GL_TransactionDetail

                        decimal AmountDebit = Math.Round(colGLTransDetailUpate.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                        decimal AmountCredit = Math.Round(colGLTransDetailUpate.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                        if (AmountDebit != AmountCredit)
                            throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");

                        foreach (GL_TransactionDetail td in colGLTransDetailUpate)
                        {
                            td.TransactionID = colGLTransHeader.TransactionID;
                            td.Insert(sqltrans);
                        }

                        #endregion
                    }
                }

                #endregion

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)))
                {
                    da.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateTransactionHederAndDetail");
            }

            return result;
        }

        public bool UpdateTransactionHeaderAndDetail(List<AR_InvoiceDetail> colTranDetailAdd, List<AR_InvoiceDetail> colTranDetailEdit,
            List<AR_InvoiceDetail> colTranDetailDelete, GL_TransactionHeader colGLTransHeader, List<GL_TransactionDetail> colGLTransDetailUpate,
            List<AR_Invoice_AccountsDetail> colInvoiceAccountEdit, List<AR_Invoice_AccountsDetail> colInvoiceAccountAdd,
            List<AR_Invoice_AccountsDetail> colInvoiceAccountDelete, List<AR_InvoiceSupplierDetail> colSupplierDetailAdd,
            List<AR_InvoiceSupplierDetail> colSupplierDetailEdit, List<AR_InvoiceSupplierDetail> colSupplierDetailDelete, int currentChartOfAccID,
            SET_DocumentApprove da, DataTable newdt = null, bool IsAccountDetail = false, List<AR_InvoiceAdvancePaymentDetail> coladvancePaymentDtl = null )
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateTransactionHeaderAndDetail");

            try
            {
                this.UpdateDataByKey(sqltrans);

                foreach (AR_InvoiceDetail tdAdd in colTranDetailAdd)
                {
                    tdAdd.InvoiceHeaderID = this.InvoiceID;
                    tdAdd.InvoiceDetailID = Convert.ToInt16(AR_InvoiceDetail.GetNewInvoiceDetailID(tdAdd.CompanyID, tdAdd.InvoiceHeaderID, tdAdd.PeriodID, sqltrans));
                    tdAdd.Insert(sqltrans);
                }

                foreach (AR_InvoiceDetail tdUpdate in colTranDetailEdit)
                    tdUpdate.UpdateDataByKey(sqltrans);

                foreach (AR_InvoiceDetail tdDelete in colTranDetailDelete)
                {
                    AR_InvoiceDetail.DeleteDataByKey(sqltrans, tdDelete.UpdatedBy.Value, tdDelete.UpdateDate.Value, tdDelete.UpdatedByValue, tdDelete.CompanyID,
                        tdDelete.InvoiceDetailID, tdDelete.InvoiceHeaderID, tdDelete.PeriodID);
                }

                foreach (AR_Invoice_AccountsDetail tdAddDetail in colInvoiceAccountAdd)
                {
                    tdAddDetail.InvoiceHeaderID = this.InvoiceID;
                    tdAddDetail.InvoiceDetailAccountID = (short)AR_Invoice_AccountsDetail.GetNewInvoiceDetailAccountID(tdAddDetail.CompanyID, this.InvoiceID,
                        tdAddDetail.PeriodID, sqltrans);

                    tdAddDetail.Insert(sqltrans);
                }

                foreach (AR_Invoice_AccountsDetail tdEditDetail in colInvoiceAccountEdit)
                {
                    tdEditDetail.InvoiceHeaderID = this.InvoiceID;
                    tdEditDetail.UpdateDataByKey(sqltrans);
                }

                foreach (AR_Invoice_AccountsDetail tdEditDetail in colInvoiceAccountDelete)
                {
                    AR_Invoice_AccountsDetail.DeleteDataByKey(sqltrans, tdEditDetail.UpdatedBy.Value, tdEditDetail.UpdateDate.Value, tdEditDetail.UpdatedByValue,
                        tdEditDetail.CompanyID, tdEditDetail.InvoiceDetailAccountID, this.InvoiceID, tdEditDetail.PeriodID);
                }

                #region Invoice Supplier

                foreach (AR_InvoiceSupplierDetail tdAddDetail in colSupplierDetailAdd)
                {
                    tdAddDetail.InvoiceHeaderID = this.InvoiceID;
                    tdAddDetail.InvoiceSupplierID = (short)AR_InvoiceSupplierDetail.GetNewInvoiceSupplierID(tdAddDetail.CompanyID, this.InvoiceID,
                        tdAddDetail.PeriodID, sqltrans);

                    tdAddDetail.Insert(sqltrans);
                }

                short SupDt_Id = 1;

                foreach (AR_InvoiceSupplierDetail tdEditDetail in colSupplierDetailEdit)
                {
                    tdEditDetail.InvoiceHeaderID = this.InvoiceID;
                    //tdEditDetail.InvoiceSupplierID = SupDt_Id++;
                    tdEditDetail.UpdateDataByKey(sqltrans);
                }

                foreach (AR_InvoiceSupplierDetail tdDeleteDetail in colSupplierDetailDelete)
                {
                    AR_InvoiceSupplierDetail.DeleteDataByKey(sqltrans, tdDeleteDetail.UpdatedBy.Value, tdDeleteDetail.UpdateDate.Value, tdDeleteDetail.UpdatedByValue,
                        tdDeleteDetail.CompanyID, this.InvoiceID, tdDeleteDetail.InvoiceSupplierID, tdDeleteDetail.PeriodID);
                }

                #endregion

                #region Insertion in Advance Payment

                if (coladvancePaymentDtl != null && coladvancePaymentDtl.Count > 0)
                {
                    short InvoiceDetailID = Convert.ToInt16(AR_InvoiceAdvancePaymentDetail.GetNewInvoiceDetailID(f_CompanyID, f_PeriodID, sqltrans));

                    foreach (AR_InvoiceAdvancePaymentDetail advpayment in coladvancePaymentDtl)
                    {
                        advpayment.InvoiceHeaderID = this.InvoiceID;
                        advpayment.InvoiceDetailID = InvoiceDetailID++;
                        advpayment.Insert(sqltrans);
                    }
                }

                #endregion

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"


                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                {
                    #region Insert In GL_TransactionHeader

                    colGLTransHeader.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    colGLTransHeader.Insert(sqltrans);

                    #endregion

                    if (IsAccountDetail == true && newdt != null)
                    {
                        #region Insert In GL_TransactionDetail
                        short i = 0;

                        decimal AmountDebit = Math.Round(colGLTransDetailUpate.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                        decimal AmountCredit = Math.Round(colGLTransDetailUpate.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                        if (AmountDebit != AmountCredit)
                            throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");

                        foreach (GL_TransactionDetail td in colGLTransDetailUpate)
                        {
                            td.SourceHeaderID = this.InvoiceID;
                            td.SourceDetailID = Convert.ToInt16(newdt.Rows[i]["InvoiceDetailAccountID"].ToString());
                            td.TransactionID = colGLTransHeader.TransactionID;
                            td.Insert(sqltrans);
                            i++;
                        }

                        #endregion
                    }
                    else
                    {
                        #region Insert In GL_TransactionDetail

                        decimal AmountDebit = Math.Round(colGLTransDetailUpate.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                        decimal AmountCredit = Math.Round(colGLTransDetailUpate.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                        if (AmountDebit != AmountCredit)
                            throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");

                        foreach (GL_TransactionDetail td in colGLTransDetailUpate)
                        {
                            td.TransactionID = colGLTransHeader.TransactionID;
                            td.Insert(sqltrans);
                        }

                        #endregion
                    }
                }

                #endregion

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)))
                {
                    da.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
                    da.DocumentNo = this.InvoiceNumber;
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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateTransactionHeaderAndDetail");
            }

            return result;
        }

        public bool DeleteTransactionHederAndDetail(int currentChartOfAccID, Guid UpdatedBy, DateTime UpdateDate, string UpdateByValue, int companyid,
                int periodid, int invoiceid, int transactionID)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("DeleteTransactionHederAndDetail");

            try
            {
                AR_InvoiceHeader.DeleteDataByKey(sqltrans, UpdatedBy, UpdateDate, UpdatedByValue, companyid, invoiceid, periodid);

                using (AR_InvoiceDetail obj_PurchaseInvoiceDetail = new AR_InvoiceDetail())
                {
                    obj_PurchaseInvoiceDetail.DeleteDataByKeyInvoiceHeaderID(sqltrans, UpdatedBy, UpdateDate, UpdatedByValue, companyid, invoiceid, periodid);
                }

                AR_InvoiceSupplierDetail.DeleteDataByKeyInvoiceHeaderID(sqltrans, UpdatedBy, UpdateDate, UpdatedByValue, companyid, invoiceid, periodid);
                AR_Invoice_AccountsDetail.DeleteDataByKeyInvoiceHeaderID(sqltrans, UpdatedBy, UpdateDate, UpdatedByValue, companyid, invoiceid, periodid);

                AP_PurchaseOrderHeader.Get_UpdatePOHeaderClosedCodeForDeleteInvoice(companyid, periodid, invoiceid, sqltrans);

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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateTransactionHederAndDetail");
            }

            return result;
        }

        public bool UpdateInvoiceHeaderForApprove(GL_TransactionHeader th, List<GL_TransactionDetail> colAddTranDetail, SET_DocumentApprove dA,List<AR_InvoiceSupplierDetail> colAddSupplierAdd)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateInvoiceHeader");

            try
            {
                this.UpdateDataByKey(sqltrans);

                if (colAddSupplierAdd != null && colAddSupplierAdd.Count > 0)
                {
                    short invoiceSupplierID = Convert.ToInt16(AR_InvoiceSupplierDetail.GetNewInvoiceSupplierID(f_CompanyID, this.InvoiceID, f_PeriodID, sqltrans));

                    foreach (AR_InvoiceSupplierDetail invoiceSupplier in colAddSupplierAdd)
                    {
                        invoiceSupplier.InvoiceSupplierID = invoiceSupplierID++;
                        invoiceSupplier.InvoiceHeaderID = this.InvoiceID;
                        invoiceSupplier.FiscalID = f_FiscalID;
                        invoiceSupplier.Insert(sqltrans);
                    }
                }

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                {
                    #region Insert In GL_TransactionHeader

                    th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    th.Insert(sqltrans);

                    #endregion

                    #region Insert In GL_TransactionDetail

                    if (colAddTranDetail.Count > 0)
                    {

                        decimal AmountDebit = Math.Round(colAddTranDetail.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                        decimal AmountCredit = Math.Round(colAddTranDetail.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                        if (AmountDebit != AmountCredit)
                            throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");

                        foreach (GL_TransactionDetail td in colAddTranDetail)
                        {
                            td.TransactionID = th.TransactionID;
                            td.Insert(sqltrans);
                        }
                    }
                    else
                    {
                        result = false;
                        throw new BusinessLogicException("There is no detail found under General Ledger object");
                    }

                    #endregion
                }

                #endregion

                #region Update Status To False, If "InProcess" In SET_DocumentApprove

                if (dA.ApprovalStatus.Equals(Enumaration.CurrentStatus.InProcess.ToString()))
                    dA.UpdateDataByKeyStatus(sqltrans);

                #endregion

                #region Update Status To False, If "Discuss" In SET_DocumentApprove

                if (dA.ApprovalStatus.Equals(Enumaration.CurrentStatus.Discuss.ToString()))
                    dA.UpdateStatusByKeysWithoutDiscuss(sqltrans);

                #endregion

                #region Insert In SET_DocumentApprove

                dA.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(f_CompanyID, f_PeriodID, sqltrans);
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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateInvoiceHeader");
            }

            return result;
        }

        public DataRow GetInvoiceHeaderDataByKey(int companyID, int invoiceID, int periodID, bool IsVehicle = false, SqlConnection objConnection = null, bool DIPayment = false)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@InvoiceID", invoiceID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (!IsVehicle)
                {

                    string Query = string.Empty;
                    if (!DIPayment)
                        Query = FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_GetInvoiceHeaderDataByKey;
                    else
                        Query = FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_GetInvoiceHeaderDataByKeyForDI;


                    if (objConnection == null)
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query, sqlparams);
                }
                else
                {

                    if (objConnection == null)
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_GetInvoiceHeaderDataByKeyForVehicle, sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_GetInvoiceHeaderDataByKeyForVehicle, sqlparams);
                }

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

        public decimal GetSalesTaxAmount(int companyID, int periodID, int invoiceID)
        {
            decimal invoiceNum = 0;

            SqlParameter[] sqlparams = new SqlParameter[3];
            sqlparams[0] = new SqlParameter("@InvoiceID", invoiceID);
            sqlparams[1] = new SqlParameter("@CompanyID", companyID);
            sqlparams[2] = new SqlParameter("@PeriodID", periodID);

            string Query_GetSalesTaxAmount = @" select SUM((ISNULL(AR_InvoiceDetail.GrossAmount, 0) * ISNULL(AR_InvoiceDetail.TaxAmount, 0)) / 100) as SalesTaxAmount 
from AR_InvoiceHeader 
-- Since some Doc Types does not contain detail records
    left join AR_InvoiceDetail on AR_InvoiceDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID and AR_InvoiceDetail.CompanyID = AR_InvoiceHeader.CompanyID 
        and AR_InvoiceDetail.PeriodID = AR_InvoiceHeader.PeriodID 
where AR_InvoiceHeader.InvoiceID = @InvoiceID and AR_InvoiceHeader.CompanyID = @CompanyID and AR_InvoiceHeader.PeriodID = @PeriodID ";

            invoiceNum = Convert.ToDecimal(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetSalesTaxAmount, sqlparams));

            return invoiceNum;
        }

        public static bool Delete_By_PrimaryKey(int companyID, int periodID, int invoiceID, SqlTransaction objTransaction)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@InvoiceID", invoiceID);

                string Query_Delete_By_PrimaryKey = @" DELETE FROM AR_InvoiceHeader WHERE CompanyID = @CompanyID and InvoiceID = @InvoiceID and PeriodID = @PeriodID ";

                if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_By_PrimaryKey, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public DataTable Get_AllDocumentNos(int companyID, int periodID, int documentTypeID, int CurrentFiscalID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                string Query_Get_AllDocumentNos = @" 
SELECT AR_InvoiceHeader.InvoiceNumber As DocumentNo FROM AR_InvoiceHeader 
inner join Set_PeriodCustom on AR_InvoiceHeader.InvoiceDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID
WHERE AR_InvoiceHeader.CompanyID = @CompanyID AND AR_InvoiceHeader.PeriodID = @PeriodID AND AR_InvoiceHeader.InvoiceType = @DocumentTypeID AND AR_InvoiceHeader.Status = 1 AND IsNull(AR_InvoiceHeader.IsReversed, 0) = 0 AND AR_InvoiceHeader.ReversalID IS NULL 
and AR_InvoiceHeader.StatusCode = 1
and AR_InvoiceHeader.InvoiceID not in (select InvoiceHeaderID from AP_PaymentInvoicesDetail where status = 1 and AP_PaymentInvoicesDetail.CompanyID = @CompanyID )
ORDER BY Convert(BigInt, AR_InvoiceHeader.InvoiceNumber) ";

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

        public DataRow Get_Invoice_By_DocumentType(int companyID, int periodID, int documentTypeID, int documentNo, int FiscalID)
        {
            DataRow drToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[3] = new SqlParameter("@DocumentNo", documentNo);
                sqlparams[4] = new SqlParameter("@FiscalID", FiscalID);


                string Query_Get_Invoice_By_DocumentType = @" SELECT * FROM AR_InvoiceHeader 
WHERE CompanyID = @CompanyID AND PeriodID = @PeriodID AND InvoiceType = @DocumentTypeID AND InvoiceNumber = @DocumentNo AND Status = 1 and FiscalID = @FiscalID";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_Invoice_By_DocumentType, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    drToReturn = ds.Tables[0].Rows[0];
            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return drToReturn;
        }

        public bool ReverseDocument(List<AR_InvoiceDetail> lst_PurInvoiceDetail, List<AR_Invoice_AccountsDetail> lst_PurInvoiceAccounts,
            List<AR_InvoiceSupplierDetail> lst_PurInvoiceSuppliers, DateTime dt_ReverseDate, Guid reversedBy, string currentusername, GL_TransactionHeader objGLHeader,
            List<GL_TransactionDetail> lst_GLDetails, SET_DocumentApprove obj_Approval, out int ReversalDocumentNo, string Comments, int CurrentFiscalID)
        {
            bool result = false;
            SqlTransaction objSQLTransaction = SqlHelper.GetTransaction("ReverseDirectInvoices");

            try
            {
                string NarrationOld = objGLHeader.Narration.ToString();
                DataTable dtInvoiceTransactionDetail = AR_InvoiceDetail.GetDataByKey(this.CompanyID, this.PeriodID, this.InvoiceID, null, objSQLTransaction);

                if (dtInvoiceTransactionDetail != null && dtInvoiceTransactionDetail.Rows.Count > 0)
                {
                    foreach (DataRow Drow in dtInvoiceTransactionDetail.Rows)
                    {
                        INV_StockTransactionDetail objStockDetail = new INV_StockTransactionDetail(Convert.ToInt64(Drow["StockDetailID"].ToString()), null,
                            objSQLTransaction);

                        if (objStockDetail.IsDataLoaded && objStockDetail.SourceHeaderID.HasValue)
                        {
                            AP_PurchaseOrderHeader objPOHeader = new AP_PurchaseOrderHeader(this.CompanyID, this.PeriodID, int.Parse(objStockDetail.SourceHeaderID.Value.ToString()), null,
                                objSQLTransaction);

                            if (objPOHeader.IsDataLoaded)
                            {
                                AP_PurchaseOrderHeader POUpdate = new AP_PurchaseOrderHeader(this.CompanyID, this.PeriodID, objPOHeader.POHeaderID, null,
                                    objSQLTransaction);

                                POUpdate.ClosedCode = 3;
                                POUpdate.UpdateDataByKey(objSQLTransaction);
                            }
                        }
                    }
                }

                this.UpdateDataByKey(objSQLTransaction);    //  Update old Purchase Invoice Header

                #region Make Payment record available

                if (this.PaymentStatusType.HasValue)
                {
                    AP_PaymentHeader obj_PaymentHeader = new AP_PaymentHeader(this.CompanyID, this.PaymentStatusType.Value, this.PeriodID, null, objSQLTransaction);

                    if (obj_PaymentHeader.IsDataLoaded)
                    {
                        obj_PaymentHeader.IsInvoiceAdjusted = false;
                        obj_PaymentHeader.UpdateDataByKey(objSQLTransaction);
                    }
                }

                #endregion

                int newDocumentNo = SET_DocumentSequence.ManualDocumentSequence(this.InvoiceType, this.CompanyID, this.PeriodID, objSQLTransaction, CurrentFiscalID);
                string originalNarration = objGLHeader.Narration != null ? objGLHeader.Narration : "";
                if (objGLHeader.Narration != null)
                    objGLHeader.Narration = objGLHeader.Narration + (objGLHeader.Narration.Trim().EndsWith(".") ? " " : ". ")
                        + "This document is reversed (Reversed document no.: " + newDocumentNo.ToString() + ").";
                else
                    objGLHeader.Narration = "This document is reversed (Reversed document no.: " + newDocumentNo.ToString() + ").";
                objGLHeader.FiscalID = this.f_FiscalID;
                objGLHeader.UpdateDataByKey(objSQLTransaction); //  Update old General Ledger Header

                #region Insert new Purchase Invoice Header
                string Narration = Comments + "  " + this.Description;

                this.IsReversed = null;
                this.ReversalDate = null;
                this.ReversalID = this.InvoiceID;
                this.Description = Narration + "  (This document is generated against the Reversal process (Doc. No.: " + newDocumentNo + ")";
                this.InvoiceID = AR_InvoiceHeader.GetNewInvoiceID(this.CompanyID, this.PeriodID, objSQLTransaction);
                this.InvoiceNumber = newDocumentNo.ToString();
                this.AmountPaid = this.AmountPaid * -1;
                this.GrossAmount = this.GrossAmount * -1;
                this.FiscalID = CurrentFiscalID;
                ReversalDocumentNo = Convert.ToInt32(this.InvoiceNumber);
                this.InvoiceDate = dt_ReverseDate;
                this.CreateDate = DateTime.Now;
                this.CreatedBy = reversedBy;
                this.CreatedByValue = currentusername;
                 //dt_ReverseDate
                this.Insert(objSQLTransaction);

                #endregion

                #region Insert in Purchase Invoice Detail table

                if (lst_PurInvoiceDetail != null)
                {
                    short purInvoiceDetailID = (short)AR_InvoiceDetail.GetNewInvoiceDetailID(f_CompanyID, this.InvoiceID, f_PeriodID, objSQLTransaction);

                    foreach (AR_InvoiceDetail obj_InvoiceDetail in lst_PurInvoiceDetail)
                    {
                        obj_InvoiceDetail.InvoiceHeaderID = this.InvoiceID;
                        obj_InvoiceDetail.InvoiceDetailID = purInvoiceDetailID++;
                        obj_InvoiceDetail.FiscalID = f_FiscalID;
                        obj_InvoiceDetail.Insert(objSQLTransaction);
                    }
                }

                #endregion

                #region Insert in Purchase Invoice Accounts table

                if (lst_PurInvoiceAccounts != null)
                {
                    short purInvoiceAccountID = (short)AR_Invoice_AccountsDetail.GetNewInvoiceDetailAccountID(f_CompanyID, this.InvoiceID, f_PeriodID,
                        objSQLTransaction);

                    foreach (AR_Invoice_AccountsDetail obj_PurInvoiceAccounts in lst_PurInvoiceAccounts)
                    {
                        obj_PurInvoiceAccounts.InvoiceHeaderID = this.InvoiceID;
                        obj_PurInvoiceAccounts.InvoiceDetailAccountID = purInvoiceAccountID++;
                        obj_PurInvoiceAccounts.FiscalID = f_FiscalID;
                        obj_PurInvoiceAccounts.Insert(objSQLTransaction);
                    }
                }

                #endregion

                #region Insert in Purchase Invoice Suppliers table

                if (lst_PurInvoiceSuppliers != null)
                {
                    short receiptSupplierID = (short)AR_InvoiceSupplierDetail.GetNewInvoiceSupplierID(f_CompanyID, this.InvoiceID, f_PeriodID, objSQLTransaction);

                    foreach (AR_InvoiceSupplierDetail obj_PurInvoiceSupplier in lst_PurInvoiceSuppliers)
                    {
                        obj_PurInvoiceSupplier.InvoiceHeaderID = this.InvoiceID;
                        obj_PurInvoiceSupplier.InvoiceSupplierID = receiptSupplierID++;
                        obj_PurInvoiceSupplier.FiscalID = f_FiscalID;
                        obj_PurInvoiceSupplier.Insert(objSQLTransaction);
                    }
                }

                #endregion

                #region Insert new General Ledger Header
                string GlNaration = NarrationOld += (originalNarration == null ? "" : originalNarration.Trim().EndsWith(".") ? " " : ". ")
                    + "This document is generated against the Reversal process. Original Document No.: " + objGLHeader.DocumentNo;

                objGLHeader.IsReversed = null;
                objGLHeader.Narration = Comments + GlNaration;

                objGLHeader.ReversalDate = null;
                objGLHeader.ReversedBy = null;
                objGLHeader.ReversalID = objGLHeader.TransactionID;
                objGLHeader.TransactionID = GL_TransactionHeader.GetNewTransactionID(objGLHeader.CompanyID, objGLHeader.PeriodID, objSQLTransaction);
                objGLHeader.DocumentNo = newDocumentNo; // Convert.ToInt32(this.InvoiceNumber);
                objGLHeader.FiscalID = f_FiscalID;
                objGLHeader.DocumentDate = dt_ReverseDate;
                objGLHeader.DateOfApproval = dt_ReverseDate;
                objGLHeader.FiscalID = CurrentFiscalID;
                objGLHeader.CreateDate = DateTime.Now;
                objGLHeader.CreatedBy = reversedBy;
                objGLHeader.CreatedByValue = currentusername;
                objGLHeader.Insert(objSQLTransaction);

                #endregion

                #region Insert new General Ledger Detail

                decimal AmountDebit = Math.Round(lst_GLDetails.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                decimal AmountCredit = Math.Round(lst_GLDetails.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                if (AmountDebit != AmountCredit)
                    throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");


                foreach (GL_TransactionDetail objGLDetail in lst_GLDetails)
                {
                    objGLDetail.TransactionID = objGLHeader.TransactionID;
                    objGLDetail.SourceHeaderID = objGLHeader.ReversalID;
                    objGLDetail.FiscalID = f_FiscalID;
                    objGLDetail.CreateDate = DateTime.Now;
                    objGLDetail.CreatedBy = reversedBy;
                    objGLDetail.CreatedByValue = currentusername;
                    objGLDetail.Insert(objSQLTransaction);
                }

                #endregion

                #region Insert new Document Approve

                obj_Approval.Status = true;
                obj_Approval.DocumentNo = objGLHeader.DocumentNo.ToString();
                obj_Approval.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(objGLHeader.CompanyID, objGLHeader.PeriodID, objSQLTransaction);
                obj_Approval.FiscalID = f_FiscalID;
                obj_Approval.Insert(objSQLTransaction);

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
                    SqlHelper.CommitTransaction(objSQLTransaction);
                else
                    SqlHelper.RollBackTransaction(objSQLTransaction, "ReverseDirectInvoices");
            }

            return result;
        }

        public decimal Get_DirectPaymentAmount(int companyID, int periodID, int invoiceID)
        {
            decimal amount = 0;

            SqlParameter[] sqlparams = new SqlParameter[3];
            sqlparams[0] = new SqlParameter("@InvoiceID", invoiceID);
            sqlparams[1] = new SqlParameter("@CompanyID", companyID);
            sqlparams[2] = new SqlParameter("@PeriodID", periodID);

            string Query_DirectPaymentAmount = @" 
--	select sum(AP_PaymentSupplierDetail.Amount) - PreviousInvoices.Amount from AR_InvoiceHeader 
--    Inner Join AP_PaymentHeader On AP_PaymentHeader.CompanyID = AR_InvoiceHeader.CompanyID And AP_PaymentHeader.PeriodID = AR_InvoiceHeader.PeriodID 
--        And AP_PaymentHeader.PaymentID = AR_InvoiceHeader.PaymentStatusType And AP_PaymentHeader.Status = 1 And AP_PaymentHeader.StatusCode = 1 
--	Inner Join AP_PaymentSupplierDetail On AP_PaymentSupplierDetail.CompanyID = AP_PaymentHeader.CompanyID 
--		And AP_PaymentSupplierDetail.PeriodID = AP_PaymentHeader.PeriodID And AP_PaymentSupplierDetail.PaymentHeaderID = AP_PaymentHeader.PaymentID 
--    Outer Apply 
--	(
--		Select IsNull(Sum(AR_InvoiceHeader.AmountPaid), 0) As Amount From AR_InvoiceHeader 
--		Where CompanyID = AP_PaymentHeader.CompanyID And PeriodID = AP_PaymentHeader.PeriodID And PaymentStatusType = AP_PaymentHeader.PaymentID And Status = 1 
--            And StatusCode = 1 And InvoiceID <> @InvoiceID
--	) As PreviousInvoices 
--where AR_InvoiceHeader.InvoiceID = @InvoiceID and AR_InvoiceHeader.CompanyID = @CompanyID and AR_InvoiceHeader.PeriodID = @PeriodID
--group by PreviousInvoices.Amount 

Select  PreviousInvoices.Amount from  AR_InvoiceHeader 
INNER join AR_InvoiceSupplierDetail on AR_InvoiceSupplierDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID and AR_InvoiceSupplierDetail.PeriodID = AR_InvoiceHeader.PeriodID 
        and AR_InvoiceSupplierDetail.CompanyID = AR_InvoiceHeader.CompanyID 
			Inner Join AP_PaymentInvoicesDetail On AP_PaymentInvoicesDetail.CompanyID = AR_InvoiceHeader.CompanyID 
		And AP_PaymentInvoicesDetail.PeriodID = AR_InvoiceHeader.PeriodID And AP_PaymentInvoicesDetail.InvoiceHeaderID = AR_InvoiceSupplierDetail.InvoiceHeaderID 
   Inner Join AP_PaymentHeader On AP_PaymentHeader.CompanyID = AP_PaymentInvoicesDetail.CompanyID And AP_PaymentHeader.PeriodID = AP_PaymentInvoicesDetail.PeriodID 
       And AP_PaymentHeader.PaymentID = AP_PaymentInvoicesDetail.PaymentHeaderID And AP_PaymentHeader.Status = 1 --And AP_PaymentHeader.StatusCode = 1 
	    Outer Apply 
	(
		Select IsNull(Sum(Pa.Amount), 0) As Amount From AP_PaymentInvoicesDetail  Pa
		Where CompanyID = 1 And PeriodID =1  And Status = 1 
		AND Pa.InvoiceHeaderID=AR_InvoiceHeader.InvoiceID
		AND Pa.CompanyID = AP_PaymentHeader.CompanyID And Pa.PeriodID = AP_PaymentHeader.PeriodID 
	) As PreviousInvoices 
	  where AR_InvoiceHeader.InvoiceID = @InvoiceID and AR_InvoiceHeader.CompanyID = @CompanyID and AR_InvoiceHeader.PeriodID = @PeriodID
	group by PreviousInvoices.Amount";

            amount = Convert.ToDecimal(SqlHelper.ExecuteScalar(CommandType.Text, Query_DirectPaymentAmount, sqlparams));

            return amount;
        }


        public decimal Get_LessAdvanceAmount(int companyID, int periodID, int invoiceID, int SupplierID)
        {
            decimal amount = 0;
            DataRow drToReturn = null;

            SqlParameter[] sqlparams = new SqlParameter[4];
            sqlparams[0] = new SqlParameter("@InvoiceID", invoiceID);
            sqlparams[1] = new SqlParameter("@CompanyID", companyID);
            sqlparams[2] = new SqlParameter("@PeriodID", periodID);
            sqlparams[3] = new SqlParameter("@SupplierID", SupplierID);


            string Query_DirectPaymentAmount = @" 
                                select sum(isnull(AP_PaymentSupplierDetail.amount,0))LessAdvance from 
                                AP_PaymentSupplierDetail, AR_InvoiceHeader
                                WHERE  
                                supplierid = @SupplierID
                                And AR_InvoiceHeader.PaymentStatusType = AP_PaymentSupplierDetail.PaymentHeaderID
                                and AR_InvoiceHeader.InvoiceID = @InvoiceID
                                and  AP_PaymentSupplierDetail.companyid = @CompanyID 
                                and AP_PaymentSupplierDetail.PeriodID = @PeriodID 
                                ";

            DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_DirectPaymentAmount, sqlparams);

            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                drToReturn = ds.Tables[0].Rows[0];

            if (drToReturn != null)
                amount = drToReturn["LessAdvance"] != DBNull.Value ? Convert.ToDecimal(drToReturn["LessAdvance"]) : 0;

            return amount;
        }

        public DataTable GetVendorSatementReport_Data(int CompanyID, int PeriodID, Guid userID, string documentGroup, DateTime? DateFrom, DateTime? DateTo, int? VendorFrom, int? VendorTo, bool filterZeroResults = false)
        {
            {
                List<SqlParameter> sqlparams = new List<SqlParameter>();
                StringBuilder additionalParameters = new StringBuilder();
                StringBuilder AddParameters = new StringBuilder();
                DataTable dtToReturn = null;

                try
                {
                    sqlparams.Add(new SqlParameter("@CompanyID", CompanyID));
                    sqlparams.Add(new SqlParameter("@PeriodID", PeriodID));
                    sqlparams.Add(new SqlParameter("@UserID", userID));
                    sqlparams.Add(new SqlParameter("@FromDate", DateFrom.Value));
                    sqlparams.Add(new SqlParameter("@ToDate", DateTo.Value));
                    if (VendorFrom.HasValue && VendorTo.HasValue)
                    {
                        sqlparams.Add(new SqlParameter("@VendorIDFrom", VendorFrom.Value));
                        sqlparams.Add(new SqlParameter("@VendorIDTo", VendorFrom.Value));
                        additionalParameters.Append(" And FinalTAB.CustomerID between @VendorIDFrom and @VendorIDTo ");
                    }
                    else if (VendorFrom.HasValue)
                    {
                        sqlparams.Add(new SqlParameter("@VendorID", VendorFrom.Value));
                        additionalParameters.Append(" And FinalTAB.CustomerID =@VendorID ");
                    }
                    else if (VendorTo.HasValue)
                    {
                        sqlparams.Add(new SqlParameter("@VendorID", VendorTo.Value));
                        additionalParameters.Append(" And FinalTAB.CustomerID =@VendorID ");
                    }


                    if (documentGroup != "")
                        additionalParameters.Append(" AND FinalTAB.GroupName = '" + documentGroup + "'");


                    DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_VendorStatement, additionalParameters.ToString()), sqlparams.ToArray());

                    if (ds != null && ds.Tables.Count > 0)
                        dtToReturn = ds.Tables[0];
                    #region Filter Zero Results


                    //    DataTable dt_Filtered = dtToReturn;
                    dtToReturn.Columns.Add("IsZeroFilter", typeof(bool));
                    if (filterZeroResults)
                    {
                        foreach (DataRow dr in dtToReturn.Rows)
                        {
                            dr["IsZeroFilter"] = true;
                        }

                    }
                    else
                        foreach (DataRow dr in dtToReturn.Rows)
                        {
                            dr["IsZeroFilter"] = false;
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
        }
     
        #endregion

        public DataTable APRAdjustmentReport(int CompanyID, int PeriodID, string doctype, string APRNo, DateTime? DateFrom, DateTime? DateTo, int? CustomerID)
        {
            {
                List<SqlParameter> sqlparams = new List<SqlParameter>();
                DataTable dtToReturn = null;
                StringBuilder additionalParameters = new StringBuilder();
                try
                {
                    sqlparams.Add(new SqlParameter("@CompanyID", CompanyID));
                    sqlparams.Add(new SqlParameter("@PeriodID", PeriodID));
                    sqlparams.Add(new SqlParameter("@FromDate", DateFrom.Value));
                    sqlparams.Add(new SqlParameter("@ToDate", DateTo.Value));
                    sqlparams.Add(new SqlParameter("@DocumentTypeID", doctype));                    
                    if (CustomerID.HasValue)
                    {
                        sqlparams.Add(new SqlParameter("@CustomerID", CustomerID.Value));
                    }
                    else
                    {
                        sqlparams.Add(new SqlParameter("@CustomerID", DBNull.Value));
                    }
                    if (APRNo != "")
                    {
                        additionalParameters.Append(" and AP_AdvanceSugarPayment.DocumentNo = " + APRNo.ToString());
                    }

                    DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_APRAdjustmentReport.ToString().Replace("@DocumentTypeID", doctype),additionalParameters), sqlparams.ToArray());                   

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
     

        public DataTable GetCustomerSatementReport_Data(int CompanyID, int PeriodID, Guid userID, string documentGroup, DateTime? DateFrom, DateTime? DateTo, int? VendorFrom, int? VendorTo, bool filterZeroResults = true)
        {
            {
                List<SqlParameter> sqlparams = new List<SqlParameter>();
                StringBuilder additionalParameters = new StringBuilder();
                StringBuilder AddParameters = new StringBuilder();
                DataTable dtToReturn = null;

                try
                {
                    sqlparams.Add(new SqlParameter("@CompanyID", CompanyID));
                    sqlparams.Add(new SqlParameter("@PeriodID", PeriodID));
                    sqlparams.Add(new SqlParameter("@UserID", userID));
                    sqlparams.Add(new SqlParameter("@FromDate", DateFrom.Value));
                    sqlparams.Add(new SqlParameter("@ToDate", DateTo.Value));
                    if (VendorFrom.HasValue && VendorTo.HasValue)
                    {
                        sqlparams.Add(new SqlParameter("@VendorIDFrom", VendorFrom.Value));
                        sqlparams.Add(new SqlParameter("@VendorIDTo", VendorFrom.Value));
                        additionalParameters.Append(" And FinalTAB.CustomerID between @VendorIDFrom and @VendorIDTo ");
                    }
                    else if (VendorFrom.HasValue)
                    {
                        sqlparams.Add(new SqlParameter("@VendorID", VendorFrom.Value));
                        additionalParameters.Append(" And FinalTAB.CustomerID =@VendorID ");
                    }
                    else if (VendorTo.HasValue)
                    {
                        sqlparams.Add(new SqlParameter("@VendorID", VendorTo.Value));
                        additionalParameters.Append(" And FinalTAB.CustomerID =@VendorID ");
                    }
                    if (documentGroup != "")
                        additionalParameters.Append(" AND FinalTAB.GroupName = '" + documentGroup + "'");


                    DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_CustomerStatment_New, additionalParameters.ToString()), sqlparams.ToArray());

                    if (ds != null && ds.Tables.Count > 0)
                        dtToReturn = ds.Tables[0];
                    #region Filter Zero Results


                    //    DataTable dt_Filtered = dtToReturn;
                    dtToReturn.Columns.Add("IsZeroFilter", typeof(bool));
                    if (filterZeroResults)
                    {
                        foreach (DataRow dr in dtToReturn.Rows)
                        {
                            dr["IsZeroFilter"] = true;
                        }

                    }
                    else
                        foreach (DataRow dr in dtToReturn.Rows)
                        {
                            dr["IsZeroFilter"] = false;
                        }
                    #endregion

                    dtToReturn.Columns.Add("RecieptNo", typeof(String));
                    for (int i = 1; i < dtToReturn.Rows.Count; i++)
                    {
                        if (dtToReturn.Rows[i]["InvoiceGroup"].ToString() == "")
                        {
                            for (int k = 0; k < dtToReturn.Rows.Count; k++)
                            {
                                if (dtToReturn.Rows[k]["ReceiptTransactionID"].ToString() == dtToReturn.Rows[i]["SourceID"].ToString())
                                    dtToReturn.Rows[i]["InvoiceGroup"] = dtToReturn.Rows[i]["SourceID"];

                            }
                        }

                        if (dtToReturn.Rows[i]["Reversal"].ToString() != "" && dtToReturn.Rows[i]["GroupNo"].ToString() == "-1")
                        {

                            for (int j = 0; j < dtToReturn.Rows.Count; j++)
                            {
                                if (dtToReturn.Rows[j]["InvoiceGroup"].ToString() == dtToReturn.Rows[i]["InvoiceGroup"].ToString())
                                {

                                    dtToReturn.Rows[j]["Reversal"] = dtToReturn.Rows[i]["Reversal"];

                                    //if (Convert.ToDecimal(dtToReturn.Rows[j]["Amount"].ToString() != "" ? dtToReturn.Rows[j]["Amount"] : "0") != (Convert.ToDecimal(dtToReturn.Rows[i]["Amount"].ToString() != "" ? dtToReturn.Rows[i]["Amount"] : "0")))
                                    //{
                                    //    decimal difference=0;
                                    //    //check partial amount
                                    //    difference  = Convert.ToDecimal(dtToReturn.Rows[j]["Amount"]) + (Convert.ToDecimal(dtToReturn.Rows[i]["Amount"]));
                                    //   // 
                                    //    if (difference == 0)
                                    //    {
                                    //        dtToReturn.Rows[j]["Reversal"] = dtToReturn.Rows[i]["Reversal"];
                                    //    }
                                    //    else
                                    //    {
                                    //       // dtToReturn.Rows[j]["Amount"] = difference;
                                    //        dtToReturn.Rows[j]["Reversal"] = "";
                                    //            dtToReturn.Rows[i]["Reversal"] ="";
                                    //    }
                                    //}
                                    //else {

                                    //}
                                }
                            }


                        }

                        else
                        {
                            for (int j = 0; j < dtToReturn.Rows.Count; j++)
                            {
                                if (dtToReturn.Rows[i]["DocumentNo"].ToString() != "0" && dtToReturn.Rows[j]["InvoiceGroup"].ToString() == dtToReturn.Rows[i]["InvoiceGroup"].ToString() && dtToReturn.Rows[j]["GroupNo"].ToString() == "-1")
                                    dtToReturn.Rows[i]["RecieptNo"] = dtToReturn.Rows[j]["DocumentNo"];

                            }



                        }

                    }

                    try
                    {
                        if (!dtToReturn.Columns.Contains("ZeroBalance"))
                            dtToReturn.Columns.Add("ZeroBalance");



                        List<int> lsCustomer = dtToReturn.AsEnumerable().ToList().Select(x => x.Field<int>("CustomerID")).Distinct().ToList();

                        List<int> toDeleteCustomer = new List<int>();

                        DataTable dr_RowDelete = null;
                        dr_RowDelete = dtToReturn.Clone();

                        if (lsCustomer.Count > 0)
                        {
                            for (int i = 0; i < lsCustomer.Count; i++)
                            {
                                decimal computeTotalIssuance = 0;
                                DataTable dt_Customer = dtToReturn.Select("CustomerID = " + lsCustomer[i].ToString()).CopyToDataTable();

                                computeTotalIssuance = Convert.ToDecimal(dt_Customer.Compute("Sum(Amount)", ""));

                                if (computeTotalIssuance <= 0)
                                {
                                    toDeleteCustomer.Add(Convert.ToInt32(dt_Customer.Rows[0]["CustomerID"]));
                                }

                                dt_Customer = null;

                            }
                        }

                        for (int i = 0; i < toDeleteCustomer.Count; i++)
                        {
                            for (int x = 0; x < dtToReturn.Rows.Count; x++)
                            {
                                if (toDeleteCustomer[i].ToString() == dtToReturn.Rows[x]["CustomerID"].ToString())
                                {

                                    dtToReturn.Rows[x]["ZeroBalance"] = "1";

                                }
                            }
                        }



                    }
                    catch { }
                    #region

                    //dtToReturn.Columns.Add("Credit", typeof(decimal));

                    //for (int i = 1; i < dtToReturn.Rows.Count; i++)
                    //{
                    //    if (dtToReturn.Rows[i]["InvoiceGroup"].ToString() == dtToReturn.Rows[i - 1]["InvoiceGroup"].ToString() && dtToReturn.Rows[i]["GroupNo"].ToString() == "-1")
                    //    {
                    //        dtToReturn.Rows[i - 1]["RecieptNo"] = dtToReturn.Rows[i]["DocumentNo"];

                    //    }

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
        }


        public DataRow GetInvoiceHeaderDataByKey_Alimdad(int companyID, int invoiceID, int periodID, bool IsVehicle = false, SqlConnection objConnection = null, bool DIPayment = false)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@InvoiceID", invoiceID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (!IsVehicle)
                {
                    #region Query

                    string Query_GetInvoiceHeaderDataByKey_Almdad = @" select distinct ROW_NUMBER() OVER (ORDER BY InvoiceID asc) RowUniqueID, * from 
(
    SELECT distinct AR_InvoiceHeader.CompanyID, AR_InvoiceHeader.PeriodID, InvoiceID, AcctsPayCodeCombinationID, AmountApplicableToDiscount, 
	case when (select sum(isnull(Amount,0)) 
from  AR_InvoiceSupplierDetail 
where AR_InvoiceSupplierDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID
			and AR_InvoiceSupplierDetail.CompanyID = AR_InvoiceHeader.CompanyID
			and AR_InvoiceSupplierDetail.PeriodID = AR_InvoiceHeader.PeriodID
            and AR_InvoiceSupplierDetail.Status = 1
			and AR_InvoiceSupplierDetail.SupplierID = AR_InvoiceHeader.VendorID) > 0 
			
			then
			(ISNULL((select sum(isnull(Amount,0)) 
from  AR_InvoiceSupplierDetail 
where AR_InvoiceSupplierDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID
			and AR_InvoiceSupplierDetail.CompanyID = AR_InvoiceHeader.CompanyID
			and AR_InvoiceSupplierDetail.PeriodID = AR_InvoiceHeader.PeriodID
            and AR_InvoiceSupplierDetail.Status = 1
			and AR_InvoiceSupplierDetail.SupplierID = AR_InvoiceHeader.VendorID) + ISNULL(FreightAmount,0)  - ISNULL(OtherAmount,0),0))
else		 
(ISNULL(AmountPaid + ISNULL(FreightAmount,0)  - ISNULL(OtherAmount,0),0)
- (case when AR_Invoice_AccountsDetail.TransactionType = 'Credit' then AR_Invoice_AccountsDetail.Amount
else 0 end))

end 
 as ActualInvoiceAmount,
       
 case when (select sum(isnull(Amount,0)) 
from  AR_InvoiceSupplierDetail 
where AR_InvoiceSupplierDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID
			and AR_InvoiceSupplierDetail.CompanyID = AR_InvoiceHeader.CompanyID
			and AR_InvoiceSupplierDetail.PeriodID = AR_InvoiceHeader.PeriodID
            and AR_InvoiceSupplierDetail.Status = 1
			and AR_InvoiceSupplierDetail.SupplierID = AR_InvoiceHeader.VendorID) > 0 
			then 

(ISNUll((select sum(isnull(Amount,0)) 
from  AR_InvoiceSupplierDetail 
where AR_InvoiceSupplierDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID
			and AR_InvoiceSupplierDetail.CompanyID = AR_InvoiceHeader.CompanyID
			and AR_InvoiceSupplierDetail.PeriodID = AR_InvoiceHeader.PeriodID
            and AR_InvoiceSupplierDetail.Status = 1
			and AR_InvoiceSupplierDetail.SupplierID = AR_InvoiceHeader.VendorID),0) + ISNULL(FreightAmount,0)  - ISNULL(OtherAmount,0)  
- isnull(
        (
            select distinct top 1 isnull(GL_TransactionDetail.Amount, 0) from AR_InvoiceHeader AR 
                inner join set_DocumentType on set_DocumentType.DocumentTypeID = AR.invoiceType and set_DocumentType.DocumentType = 'Debit Note' 
                inner join GL_TransactionHeader on GL_TransactionHeader.DocumentTypeID = AR.InvoiceType and GL_TransactionHeader.CompanyID = ar.CompanyID 
                    and GL_TransactionHeader.PeriodID = ar.PeriodID and GL_TransactionHeader.DocumentNo = AR.InvoiceNumber 
                inner join GL_TransactionDetail on GL_TransactionDetail.TransactionID = GL_TransactionHeader.TransactionID 
                    and GL_TransactionDetail.CompanyID = ar.CompanyID and GL_TransactionDetail.PeriodID = ar.PeriodID 
                    and GL_TransactionDetail.TransactionType = 'Debit' 
            where AR.VendorID = AR_InvoiceHeader.VendorID
        ), 0) 
        -
        (
        select isnull(sum(AP_PaymentInvoicesDetail.Amount),0) 
		from AP_PaymentInvoicesDetail 
		inner join Ap_paymentHeader inv_hdr on inv_hdr.PaymentID = AP_PaymentInvoicesDetail.PaymentHeaderID
        AND inv_hdr.Status=1
		and inv_hdr.CompanyID = AP_PaymentInvoicesDetail.CompanyID
		and inv_hdr.PeriodID = AP_PaymentInvoicesDetail.PeriodID
		and inv_hdr.ReversalDate is null and ReversalID is null
    	--AND MenuID in (Select MenuID from ApplicationMenu where MenuPath in ('/AP/Transaction/Payment.aspx','/AP/Transaction/DirectInvoicePayment.aspx'))
		
where AP_PaymentInvoicesDetail.InvoiceHeaderID = AR_InvoiceHeader.invoiceID
    and AP_PaymentInvoicesDetail.CompanyID = AR_InvoiceHeader.CompanyID
    and AP_PaymentInvoicesDetail.PeriodID = AR_InvoiceHeader.PeriodID
        )

)
			else

 (ISNUll(AmountPaid,0) + ISNULL(FreightAmount,0)  - ISNULL(OtherAmount,0)  
- isnull(
        (
            select distinct top 1 isnull(GL_TransactionDetail.Amount, 0) from AR_InvoiceHeader AR 
                inner join set_DocumentType on set_DocumentType.DocumentTypeID = AR.invoiceType and set_DocumentType.DocumentType = 'Debit Note' 
                inner join GL_TransactionHeader on GL_TransactionHeader.DocumentTypeID = AR.InvoiceType and GL_TransactionHeader.CompanyID = ar.CompanyID 
                    and GL_TransactionHeader.PeriodID = ar.PeriodID and GL_TransactionHeader.DocumentNo = AR.InvoiceNumber 
                inner join GL_TransactionDetail on GL_TransactionDetail.TransactionID = GL_TransactionHeader.TransactionID 
                    and GL_TransactionDetail.CompanyID = ar.CompanyID and GL_TransactionDetail.PeriodID = ar.PeriodID 
                    and GL_TransactionDetail.TransactionType = 'Debit' 
            where AR.VendorID = AR_InvoiceHeader.VendorID
        ), 0) 
        -
        (
        select isnull(sum(AP_PaymentInvoicesDetail.Amount),0) 
		from AP_PaymentInvoicesDetail 
		inner join Ap_paymentHeader inv_hdr on inv_hdr.PaymentID = AP_PaymentInvoicesDetail.PaymentHeaderID
        AND inv_hdr.Status=1
		and inv_hdr.CompanyID = AP_PaymentInvoicesDetail.CompanyID
		and inv_hdr.PeriodID = AP_PaymentInvoicesDetail.PeriodID
		and inv_hdr.ReversalDate is null and ReversalID is null
    	--AND MenuID in (Select MenuID from ApplicationMenu where MenuPath in ('/AP/Transaction/Payment.aspx','/AP/Transaction/DirectInvoicePayment.aspx'))
		
where AP_PaymentInvoicesDetail.InvoiceHeaderID = AR_InvoiceHeader.invoiceID
    and AP_PaymentInvoicesDetail.CompanyID = AR_InvoiceHeader.CompanyID
    and AP_PaymentInvoicesDetail.PeriodID = AR_InvoiceHeader.PeriodID
        )
- (case when AR_Invoice_AccountsDetail.TransactionType = 'Credit' then AR_Invoice_AccountsDetail.Amount
else 0 end)

)

end

as AmountPaid, 
        ApprovalDescription, ApprovalStatus, ApprovedAmount, AuthorizedBy, AutoTaxCalculationFlag, AutoWithholdingTaxFlag, AutoWithholdingTaxGroupID, BatchID, 
        CancelledAmount, CancelledBy, CancelledDate, AR_InvoiceHeader.CreatedBy, AR_InvoiceHeader.CreateDate, AR_InvoiceHeader.CreatedByValue, 
        AR_InvoiceHeader.Description, DiscountAmountTaken, DocumentSequenceID, EarliestSettlementDate, ExchangeDate, ExchangeRate, ExchangeRateType, 
        ExclusivePaymentFlag, FreightAmount, GLPostingDate, GoodsReceivedDate, GrossAmount, InvoiceCurrencyID, InvoiceDate, InvoiceNumber, InvoiceReceivedDate, 
        InvoiceType, PayCurrencyInvoiceAmount, PaymentGroupType, PaymentCrossRate, PaymentCrossRateDate, PaymentCrossRateType, PaymentCurrencyID, PaymentMethodType, 
        PaymentStatusType, PostingStatus, RecurringPaymentID, TaxAmount, TermsDate, TermsID, AR_InvoiceHeader.UpdatedBy, AR_InvoiceHeader.UpdateDate, 
        AR_InvoiceHeader.UpdatedByValue, VATCode, VendorID, VendorSiteID, VoucherNumber, AR_InvoiceHeader.Status, VendorReferenceNumber, AR_InvoiceHeader.StatusCode, 
        AR_InvoiceHeader.CostCenterID, AR_InvoiceHeader.DepartmentID, ExpenseAccountCCID, AR_InvoiceHeader.IncurredOnID, LoanHeaderID, IsPaymentCompleted, 
        IsIncurredOnAsset, BusinessPartnerTypeID, OtherAmount, OtherChartOfAccountID, OtherCCID, AR_InvoiceHeader.TransactionType, AR_InvoiceHeader.LocationID, 
        WithHeldTax, WithHeldCCID, GroupID 
    FROM AR_InvoiceHeader 
        inner join set_DocumentType on set_DocumentType.DocumentTypeID = AR_InvoiceHeader.InvoiceType and set_DocumentType.CompanyID = AR_InvoiceHeader.CompanyID 
            and set_DocumentType.PeriodID = AR_InvoiceHeader.PeriodID 

left join AR_Invoice_AccountsDetail on AR_Invoice_AccountsDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID
			and AR_Invoice_AccountsDetail.CompanyID = AR_InvoiceHeader.CompanyID
			and AR_Invoice_AccountsDetail.PeriodID = AR_InvoiceHeader.PeriodID
            and AR_Invoice_AccountsDetail.Status = 1

    WHERE AR_InvoiceHeader.CompanyID = @CompanyID and AR_InvoiceHeader.InvoiceID = @InvoiceID and AR_InvoiceHeader.PeriodID = @PeriodID
) as TAb ";
                    #endregion

                    #region For DI Query
                    string Query_GetInvoiceHeaderDataByKeyForDI_Almdad = @" select distinct ROW_NUMBER() OVER (ORDER BY InvoiceID asc) RowUniqueID,* from 
(
    SELECT distinct AR_InvoiceHeader.CompanyID, AR_InvoiceHeader.PeriodID, InvoiceID, AcctsPayCodeCombinationID, AmountApplicableToDiscount, 
sum(AR_InvoiceSupplierDetail.Amount) as ActualInvoiceAmount,
sum(AR_InvoiceSupplierDetail.Amount)
        
- isnull(
        (
            select distinct top 1 isnull(GL_TransactionDetail.Amount, 0) from AR_InvoiceHeader AR 
                inner join set_DocumentType on set_DocumentType.DocumentTypeID = AR.invoiceType and set_DocumentType.DocumentType = 'Debit Note' 
                inner join GL_TransactionHeader on GL_TransactionHeader.DocumentTypeID = AR.InvoiceType and GL_TransactionHeader.CompanyID = ar.CompanyID 
                    and GL_TransactionHeader.PeriodID = ar.PeriodID and GL_TransactionHeader.DocumentNo = AR.InvoiceNumber 
                inner join GL_TransactionDetail on GL_TransactionDetail.TransactionID = GL_TransactionHeader.TransactionID 
                    and GL_TransactionDetail.CompanyID = ar.CompanyID and GL_TransactionDetail.PeriodID = ar.PeriodID 
                    and GL_TransactionDetail.TransactionType = 'Debit' 
            where AR.VendorID = AR_InvoiceHeader.VendorID
        ), 0) 
       
-- Already Paid (Partial Case)
        -
        (
        select isnull(sum(AP_PaymentInvoicesDetail.Amount),0) 
		from AP_PaymentInvoicesDetail 
		inner join Ap_paymentHeader inv_hdr on inv_hdr.PaymentID = AP_PaymentInvoicesDetail.PaymentHeaderID
		and inv_hdr.CompanyID = AP_PaymentInvoicesDetail.CompanyID
		and inv_hdr.PeriodID = AP_PaymentInvoicesDetail.PeriodID
		and inv_hdr.ReversalDate is null and ReversalID is null
    
		where AP_PaymentInvoicesDetail.InvoiceHeaderID = AR_InvoiceHeader.invoiceID
     AND AP_PaymentInvoicesDetail.SourcetypeID = AR_InvoiceHeader.InvoiceTypeID
    and AP_PaymentInvoicesDetail.CompanyID = AR_InvoiceHeader.CompanyID

    and AP_PaymentInvoicesDetail.PeriodID = AR_InvoiceHeader.PeriodID
and AP_PaymentInvoicesDetail.Status = 1
        )
as AmountPaid

        ,InvoiceDate, InvoiceNumber, InvoiceReceivedDate
        
    FROM AR_InvoiceHeader 
        inner join set_DocumentType on set_DocumentType.DocumentTypeID = AR_InvoiceHeader.InvoiceType and set_DocumentType.CompanyID = AR_InvoiceHeader.CompanyID 
            and set_DocumentType.PeriodID = AR_InvoiceHeader.PeriodID 
            inner join AR_InvoiceSupplierDetail on AR_InvoiceSupplierDetail.InvoiceHeaderID = AR_InvoiceHeader.invoiceID
            and AR_InvoiceSupplierDetail.CompanyID = AR_InvoiceHeader.CompanyID
            and AR_InvoiceSupplierDetail.PeriodID = AR_InvoiceHeader.PeriodID
            and AR_InvoiceSupplierDetail.TransactionType = 'Credit'
and AR_InvoiceSupplierDetail.Status = 1

    WHERE AR_InvoiceHeader.CompanyID = @CompanyID and AR_InvoiceHeader.InvoiceID = @InvoiceID and AR_InvoiceHeader.PeriodID = @PeriodID
    
    group by InvoiceDate, InvoiceNumber, InvoiceReceivedDate,AR_InvoiceHeader.CompanyID
    ,AR_InvoiceHeader.CompanyID, AR_InvoiceHeader.PeriodID, InvoiceID, AcctsPayCodeCombinationID, AmountApplicableToDiscount
    ,AR_InvoiceHeader.VendorID,AR_InvoiceHeader.ReversalID
) as TAb  ";
                    #endregion


                    string Query = string.Empty;
                    if (!DIPayment)
                        Query = Query_GetInvoiceHeaderDataByKey_Almdad;
                    else
                        Query = Query_GetInvoiceHeaderDataByKeyForDI_Almdad;


                    if (objConnection == null)
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query, sqlparams);
                }
                else
                {
                    #region Query

                    string Query_GetInvoiceHeaderDataByKeyForVehicle = @" select distinct ROW_NUMBER() OVER (ORDER BY InvoiceID asc) RowUniqueID, * from 
(
    SELECT distinct AR_InvoiceHeader.CompanyID, AR_InvoiceHeader.PeriodID, InvoiceID, AcctsPayCodeCombinationID, AmountApplicableToDiscount, AmountPaid - isnull(
        (
            select sum(Amount) from AR_Invoice_AccountsDetail 
            where AR_Invoice_AccountsDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID and TransactionType = 'Credit'
        ), 0) - isnull(
        (
            select distinct top 1 isnull(GL_TransactionDetail.Amount, 0) from AR_InvoiceHeader AR 
                inner join set_DocumentType on set_DocumentType.DocumentTypeID = AR.invoiceType and set_DocumentType.DocumentType = 'Debit Invoice' 
                inner join GL_TransactionHeader on GL_TransactionHeader.DocumentTypeID =  AR.InvoiceType and GL_TransactionHeader.CompanyID = ar.CompanyID 
                    and GL_TransactionHeader.PeriodID = ar.PeriodID and GL_TransactionHeader.DocumentNo = AR.InvoiceNumber 
                inner join GL_TransactionDetail on GL_TransactionDetail.TransactionID = GL_TransactionHeader.TransactionID 
                    and GL_TransactionDetail.CompanyID = ar.CompanyID and GL_TransactionDetail.PeriodID = ar.PeriodID 
                    and GL_TransactionDetail.TransactionType = 'Debit' 
            where AR.VendorID = AR_InvoiceHeader.VendorID
        ), 0) as AmountPaid, 
        ApprovalDescription, ApprovalStatus, ApprovedAmount, AuthorizedBy, AutoTaxCalculationFlag, AutoWithholdingTaxFlag, AutoWithholdingTaxGroupID, BatchID, 
        CancelledAmount, CancelledBy, CancelledDate, AR_InvoiceHeader.CreatedBy, AR_InvoiceHeader.CreateDate, AR_InvoiceHeader.CreatedByValue, 
        AR_InvoiceHeader.Description, DiscountAmountTaken, DocumentSequenceID, EarliestSettlementDate, ExchangeDate, ExchangeRate, ExchangeRateType, 
        ExclusivePaymentFlag, FreightAmount, GLPostingDate, GoodsReceivedDate, GrossAmount, InvoiceCurrencyID, InvoiceDate, InvoiceNumber, InvoiceReceivedDate, 
        InvoiceType, PayCurrencyInvoiceAmount, PaymentGroupType, PaymentCrossRate, PaymentCrossRateDate, PaymentCrossRateType, PaymentCurrencyID, PaymentMethodType, 
        PaymentStatusType, PostingStatus, RecurringPaymentID, TaxAmount, TermsDate, TermsID, AR_InvoiceHeader.UpdatedBy, AR_InvoiceHeader.UpdateDate, 
        AR_InvoiceHeader.UpdatedByValue, VATCode, VendorID, VendorSiteID, VoucherNumber, AR_InvoiceHeader.Status, VendorReferenceNumber, AR_InvoiceHeader.StatusCode, 
        AR_InvoiceHeader.CostCenterID, AR_InvoiceHeader.DepartmentID, ExpenseAccountCCID, AR_InvoiceHeader.IncurredOnID, LoanHeaderID, IsPaymentCompleted, 
        IsIncurredOnAsset, BusinessPartnerTypeID, OtherAmount, OtherChartOfAccountID, OtherCCID, AR_InvoiceHeader.TransactionType, AR_InvoiceHeader.LocationID, 
        WithHeldTax, WithHeldCCID 
    FROM AR_InvoiceHeader 
        inner join set_DocumentType on set_DocumentType.DocumentTypeID = AR_InvoiceHeader.InvoiceType and set_DocumentType.CompanyID = AR_InvoiceHeader.CompanyID 
            and set_DocumentType.PeriodID = AR_InvoiceHeader.PeriodID 
    WHERE AR_InvoiceHeader.CompanyID = @CompanyID and AR_InvoiceHeader.InvoiceID = @InvoiceID and AR_InvoiceHeader.PeriodID = @PeriodID
) as TAb ";

                    #endregion

                    if (objConnection == null)
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetInvoiceHeaderDataByKeyForVehicle, sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetInvoiceHeaderDataByKeyForVehicle, sqlparams);
                }

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

        public bool InsertTransactionHeaderAndEmpDetail(List<AR_InvoiceSupplierDetail> colInvoiceSupplierDetail, SqlTransaction sqlTrans = null)
        {
            bool result = false;
            //sqlTrans = SqlHelper.GetTransaction("HRMS_EmployeeSalaryPost");

            try
            {
                //this.InvoiceNumber = SET_DocumentSequence.ManualDocumentSequence(f_InvoiceType, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID).ToString();
                //this.InvoiceID = AR_InvoiceHeader.GetNewInvoiceID(f_CompanyID, f_PeriodID, sqltrans);

                this.Insert(sqlTrans);

                short invoiceDetailId = (short)AR_InvoiceDetail.GetNewInvoiceDetailID(f_CompanyID, this.InvoiceID, f_PeriodID, sqlTrans);
                decimal POQty = 0;

                #region Insertion in AR_InvoiceSupplierDetail

                if (colInvoiceSupplierDetail != null && colInvoiceSupplierDetail.Count > 0)
                {
                    short invoiceSupplierID = Convert.ToInt16(AR_InvoiceSupplierDetail.GetNewInvoiceSupplierID(f_CompanyID, this.InvoiceID, f_PeriodID, sqlTrans));

                    foreach (AR_InvoiceSupplierDetail invoiceSupplier in colInvoiceSupplierDetail)
                    {
                        invoiceSupplier.InvoiceSupplierID = invoiceSupplierID++;
                        invoiceSupplier.InvoiceHeaderID = this.InvoiceID;
                        invoiceSupplier.FiscalID = this.FiscalID;
                        invoiceSupplier.Insert(sqlTrans);
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
                if (!result)
                    //    SqlHelper.CommitTransaction(sqlTrans);
                    //else
                    SqlHelper.RollBackTransaction(sqlTrans, "HRMS_EmployeeSalaryPost");
            }

            return result;
        }

        public bool UpdateDataByDDLValues(int Companyid, int perioid, int PurchaseInvoiceNo, int InvoiceType, string Query, int StatusCode)
        {
            SqlTransaction objTransaction = null;
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", Companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", perioid);
                sqlparams[2] = new SqlParameter("@PurchaseInvoiceNo", PurchaseInvoiceNo);
                sqlparams[3] = new SqlParameter("@StatusCode", StatusCode);
                sqlparams[4] = new SqlParameter("@InvoiceType", InvoiceType);

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

        public AR_InvoiceHeader(int CompanyID, int PeriodID, int InvoiceNo, int invoicetypeID, Nullable<int> fiscalID = null)
        {
            Load(CompanyID, PeriodID, InvoiceNo, invoicetypeID, fiscalID);
        }

        private void Load(int CompanyID, int PeriodID, int InvoiceNo, int invoicetypeID, int? fiscalID)
        {
            DataRow drRecord = GetDataByKey(CompanyID, PeriodID, InvoiceNo, invoicetypeID, fiscalID);

            if (drRecord != null)
            {
                this.f_IsDataLoaded = true;
                this.f_CompanyID = Convert.ToInt32(drRecord["CompanyID"]);
                this.f_PeriodID = Convert.ToInt32(drRecord["PeriodID"]);
                this.f_InvoiceID = Convert.ToInt32(drRecord["InvoiceID"]);

                if (drRecord["AcctsPayCodeCombinationID"] != DBNull.Value)
                    this.f_AcctsPayCodeCombinationID = Convert.ToString(drRecord["AcctsPayCodeCombinationID"]);


                if (drRecord["AmountApplicableToDiscount"] != DBNull.Value)
                    this.f_AmountApplicableToDiscount = Convert.ToDouble(drRecord["AmountApplicableToDiscount"]);


                if (drRecord["AmountPaid"] != DBNull.Value)
                    this.f_AmountPaid = Convert.ToDouble(drRecord["AmountPaid"]);


                if (drRecord["ApprovalDescription"] != DBNull.Value)
                    this.f_ApprovalDescription = Convert.ToString(drRecord["ApprovalDescription"]);


                if (drRecord["ApprovalStatus"] != DBNull.Value)
                    this.f_ApprovalStatus = Convert.ToInt32(drRecord["ApprovalStatus"]);


                if (drRecord["ApprovedAmount"] != DBNull.Value)
                    this.f_ApprovedAmount = Convert.ToDouble(drRecord["ApprovedAmount"]);


                if (drRecord["AuthorizedBy"] != DBNull.Value)
                    this.f_AuthorizedBy = Guid.Parse(drRecord["AuthorizedBy"].ToString());


                if (drRecord["AutoTaxCalculationFlag"] != DBNull.Value)
                    this.f_AutoTaxCalculationFlag = Convert.ToChar(drRecord["AutoTaxCalculationFlag"]);

                this.f_AutoWithholdingTaxFlag = Convert.ToBoolean(drRecord["AutoWithholdingTaxFlag"]);

                if (drRecord["AutoWithholdingTaxGroupID"] != DBNull.Value)
                    this.f_AutoWithholdingTaxGroupID = Convert.ToInt32(drRecord["AutoWithholdingTaxGroupID"]);


                if (drRecord["BatchID"] != DBNull.Value)
                    this.f_BatchID = Convert.ToInt32(drRecord["BatchID"]);


                if (drRecord["CancelledAmount"] != DBNull.Value)
                    this.f_CancelledAmount = Convert.ToDouble(drRecord["CancelledAmount"]);


                if (drRecord["CancelledBy"] != DBNull.Value)
                    this.f_CancelledBy = Guid.Parse(drRecord["CancelledBy"].ToString());


                if (drRecord["CancelledDate"] != DBNull.Value)
                    this.f_CancelledDate = Convert.ToDateTime(drRecord["CancelledDate"]);

                this.f_CreatedBy = Guid.Parse(drRecord["CreatedBy"].ToString());
                this.f_CreateDate = Convert.ToDateTime(drRecord["CreateDate"]);
                this.f_CreatedByValue = Convert.ToString(drRecord["CreatedByValue"]);

                if (drRecord["Description"] != DBNull.Value)
                    this.f_Description = Convert.ToString(drRecord["Description"]);


                if (drRecord["DiscountAmountTaken"] != DBNull.Value)
                    this.f_DiscountAmountTaken = Convert.ToDouble(drRecord["DiscountAmountTaken"]);


                if (drRecord["DocumentSequenceID"] != DBNull.Value)
                    this.f_DocumentSequenceID = Convert.ToInt32(drRecord["DocumentSequenceID"]);


                if (drRecord["EarliestSettlementDate"] != DBNull.Value)
                    this.f_EarliestSettlementDate = Convert.ToDateTime(drRecord["EarliestSettlementDate"]);


                if (drRecord["ExchangeDate"] != DBNull.Value)
                    this.f_ExchangeDate = Convert.ToDateTime(drRecord["ExchangeDate"]);


                if (drRecord["ExchangeRate"] != DBNull.Value)
                    this.f_ExchangeRate = Convert.ToDouble(drRecord["ExchangeRate"]);


                if (drRecord["ExchangeRateType"] != DBNull.Value)
                    this.f_ExchangeRateType = Convert.ToInt32(drRecord["ExchangeRateType"]);

                this.f_ExclusivePaymentFlag = Convert.ToBoolean(drRecord["ExclusivePaymentFlag"]);

                if (drRecord["FreightAmount"] != DBNull.Value)
                    this.f_FreightAmount = Convert.ToDouble(drRecord["FreightAmount"]);


                if (drRecord["GLPostingDate"] != DBNull.Value)
                    this.f_GLPostingDate = Convert.ToDateTime(drRecord["GLPostingDate"]);


                if (drRecord["GoodsReceivedDate"] != DBNull.Value)
                    this.f_GoodsReceivedDate = Convert.ToDateTime(drRecord["GoodsReceivedDate"]);


                if (drRecord["GrossAmount"] != DBNull.Value)
                    this.f_GrossAmount = Convert.ToDouble(drRecord["GrossAmount"]);

                this.f_InvoiceCurrencyID = Convert.ToInt32(drRecord["InvoiceCurrencyID"]);
                this.f_InvoiceDate = Convert.ToDateTime(drRecord["InvoiceDate"]);
                this.f_InvoiceNumber = Convert.ToString(drRecord["InvoiceNumber"]);

                if (drRecord["InvoiceReceivedDate"] != DBNull.Value)
                    this.f_InvoiceReceivedDate = Convert.ToDateTime(drRecord["InvoiceReceivedDate"]);

                this.f_InvoiceType = Convert.ToInt32(drRecord["InvoiceType"]);

                if (drRecord["PayCurrencyInvoiceAmount"] != DBNull.Value)
                    this.f_PayCurrencyInvoiceAmount = Convert.ToDouble(drRecord["PayCurrencyInvoiceAmount"]);


                if (drRecord["PaymentGroupType"] != DBNull.Value)
                    this.f_PaymentGroupType = Convert.ToInt32(drRecord["PaymentGroupType"]);


                if (drRecord["PaymentCrossRate"] != DBNull.Value)
                    this.f_PaymentCrossRate = Convert.ToDouble(drRecord["PaymentCrossRate"]);


                if (drRecord["PaymentCrossRateDate"] != DBNull.Value)
                    this.f_PaymentCrossRateDate = Convert.ToDateTime(drRecord["PaymentCrossRateDate"]);


                if (drRecord["PaymentCrossRateType"] != DBNull.Value)
                    this.f_PaymentCrossRateType = Convert.ToInt32(drRecord["PaymentCrossRateType"]);


                if (drRecord["PaymentCurrencyID"] != DBNull.Value)
                    this.f_PaymentCurrencyID = Convert.ToInt32(drRecord["PaymentCurrencyID"]);


                if (drRecord["PaymentMethodType"] != DBNull.Value)
                    this.f_PaymentMethodType = Convert.ToInt32(drRecord["PaymentMethodType"]);


                if (drRecord["PaymentStatusType"] != DBNull.Value)
                    this.f_PaymentStatusType = Convert.ToInt32(drRecord["PaymentStatusType"]);

                this.f_PostingStatus = Convert.ToBoolean(drRecord["PostingStatus"]);

                if (drRecord["RecurringPaymentID"] != DBNull.Value)
                    this.f_RecurringPaymentID = Convert.ToInt32(drRecord["RecurringPaymentID"]);


                if (drRecord["TaxAmount"] != DBNull.Value)
                    this.f_TaxAmount = Convert.ToDouble(drRecord["TaxAmount"]);


                if (drRecord["TermsDate"] != DBNull.Value)
                    this.f_TermsDate = Convert.ToDateTime(drRecord["TermsDate"]);


                if (drRecord["TermsID"] != DBNull.Value)
                    this.f_TermsID = Convert.ToInt32(drRecord["TermsID"]);


                if (drRecord["UpdatedBy"] != DBNull.Value)
                    this.f_UpdatedBy = Guid.Parse(drRecord["UpdatedBy"].ToString());


                if (drRecord["UpdateDate"] != DBNull.Value)
                    this.f_UpdateDate = Convert.ToDateTime(drRecord["UpdateDate"]);


                if (drRecord["UpdatedByValue"] != DBNull.Value)
                    this.f_UpdatedByValue = Convert.ToString(drRecord["UpdatedByValue"]);


                if (drRecord["VATCode"] != DBNull.Value)
                    this.f_VATCode = Convert.ToString(drRecord["VATCode"]);


                if (drRecord["VendorID"] != DBNull.Value)
                    this.f_VendorID = Convert.ToInt32(drRecord["VendorID"]);


                if (drRecord["VendorSiteID"] != DBNull.Value)
                    this.f_VendorSiteID = Convert.ToInt32(drRecord["VendorSiteID"]);


                if (drRecord["VoucherNumber"] != DBNull.Value)
                    this.f_VoucherNumber = Convert.ToString(drRecord["VoucherNumber"]);

                this.f_Status = Convert.ToBoolean(drRecord["Status"]);
                this.f_VendorReferenceNumber = Convert.ToString(drRecord["VendorReferenceNumber"]);

                if (drRecord["StatusCode"] != DBNull.Value)
                    this.f_StatusCode = Convert.ToInt16(drRecord["StatusCode"]);


                if (drRecord["CostCenterID"] != DBNull.Value)
                    this.f_CostCenterID = Convert.ToInt32(drRecord["CostCenterID"]);


                if (drRecord["DepartmentID"] != DBNull.Value)
                    this.f_DepartmentID = Convert.ToInt32(drRecord["DepartmentID"]);


                if (drRecord["ExpenseAccountCCID"] != DBNull.Value)
                    this.f_ExpenseAccountCCID = Convert.ToString(drRecord["ExpenseAccountCCID"]);


                if (drRecord["IncurredOnID"] != DBNull.Value)
                    this.f_IncurredOnID = Convert.ToInt32(drRecord["IncurredOnID"]);


                if (drRecord["LoanHeaderID"] != DBNull.Value)
                    this.f_LoanHeaderID = Convert.ToInt32(drRecord["LoanHeaderID"]);

                this.f_IsPaymentCompleted = Convert.ToBoolean(drRecord["IsPaymentCompleted"]);

                if (drRecord["IsIncurredOnAsset"] != DBNull.Value)
                    this.f_IsIncurredOnAsset = Convert.ToBoolean(drRecord["IsIncurredOnAsset"]);


                if (drRecord["BusinessPartnerTypeID"] != DBNull.Value)
                    this.f_BusinessPartnerTypeID = Convert.ToInt16(drRecord["BusinessPartnerTypeID"]);


                if (drRecord["OtherAmount"] != DBNull.Value)
                    this.f_OtherAmount = Convert.ToDecimal(drRecord["OtherAmount"]);


                if (drRecord["OtherChartOfAccountID"] != DBNull.Value)
                    this.f_OtherChartOfAccountID = Convert.ToInt32(drRecord["OtherChartOfAccountID"]);


                if (drRecord["OtherCCID"] != DBNull.Value)
                    this.f_OtherCCID = Convert.ToString(drRecord["OtherCCID"]);


                if (drRecord["TransactionType"] != DBNull.Value)
                    this.f_TransactionType = Convert.ToString(drRecord["TransactionType"]);


                if (drRecord["LocationID"] != DBNull.Value)
                    this.f_LocationID = Convert.ToInt32(drRecord["LocationID"]);


                if (drRecord["WithHeldTax"] != DBNull.Value)
                    this.f_WithHeldTax = Convert.ToDecimal(drRecord["WithHeldTax"]);


                if (drRecord["WithHeldCCID"] != DBNull.Value)
                    this.f_WithHeldCCID = Convert.ToString(drRecord["WithHeldCCID"]);


                if (drRecord["IsReversed"] != DBNull.Value)
                    this.f_IsReversed = Convert.ToBoolean(drRecord["IsReversed"]);


                if (drRecord["ReversalDate"] != DBNull.Value)
                    this.f_ReversalDate = Convert.ToDateTime(drRecord["ReversalDate"]);


                if (drRecord["ReversalID"] != DBNull.Value)
                    this.f_ReversalID = Convert.ToInt32(drRecord["ReversalID"]);


                if (drRecord["ControlCredit"] != DBNull.Value)
                    this.f_ControlCredit = Convert.ToDecimal(drRecord["ControlCredit"]);


                if (drRecord["ControlDebit"] != DBNull.Value)
                    this.f_ControlDebit = Convert.ToDecimal(drRecord["ControlDebit"]);


                if (drRecord["FreightAmountCCID"] != DBNull.Value)
                    this.f_FreightAmountCCID = Convert.ToString(drRecord["FreightAmountCCID"]);


                if (drRecord["FiscalID"] != DBNull.Value)
                    this.f_FiscalID = Convert.ToInt32(drRecord["FiscalID"]);


                //if (drRecord["GrossWeightID"] != DBNull.Value)
                //    this.f_GrossWeightID = Convert.ToInt32(drRecord["GrossWeightID"]);

            }
        }

        public static DataRow GetDataByKey(int companyID, int periodID, int InvoiceNo, int invoicetypeID, int? fiscalID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;
            const string Query_GetData_byInvoiceNoandType = @" SELECT * FROM [AR_InvoiceHeader] WHERE [CompanyID] = @CompanyID 
and InvoiceNumber = @InvoiceNo and [PeriodID] = @PeriodID
And InvoiceType=@invoicetypeID
And (FiscalID = @fiscalID or @fiscalID is null)";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@InvoiceNo", InvoiceNo);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@invoicetypeID", invoicetypeID);
                if (fiscalID.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@fiscalID", fiscalID);
                }
                else
                    sqlparams[4] = new SqlParameter("@fiscalID", DBNull.Value);
                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byInvoiceNoandType, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byInvoiceNoandType, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byInvoiceNoandType, sqlparams);

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

        public bool InProcessDocument(GL_TransactionHeader th, List<GL_TransactionDetail> colTranDetail, int CompanyID, int PeriodID, int CurrentFiscalID)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InProcessInvoice");

            try
            {
                this.StatusCode = 5;
                this.UpdateDataByKey(sqltrans);
                GL_TransactionDetail.DeletePermenantDataByKeyTranasctionID(sqltrans, CompanyID, PeriodID, th.TransactionID);
                GL_TransactionHeader.Delete_By_PrimaryKey(CompanyID, PeriodID, th.TransactionID, sqltrans);

                SET_DocumentApprove.DeleteApprovalHistroy(CompanyID, PeriodID, int.Parse(this.InvoiceNumber), this.InvoiceType, sqltrans, int.Parse(this.FiscalID.ToString()));
                
                AR_InvoiceSupplierDetail.DeletePermenantDataByKeyInvoiceID(sqltrans, CompanyID, PeriodID, this.InvoiceID);

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
                    SqlHelper.RollBackTransaction(sqltrans, "InProcessInvoice");
            }

            return result;
        }
        public DataTable Get_AllDocumentNosForInprcoess(int companyID, int periodID, int documentTypeID, int CurrentFiscalID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                string Query_Get_AllDocumentNos = @" 
SELECT AR_InvoiceHeader.InvoiceNumber As DocumentNo FROM AR_InvoiceHeader 
inner join Set_PeriodCustom on AR_InvoiceHeader.InvoiceDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID
WHERE AR_InvoiceHeader.CompanyID = @CompanyID AND AR_InvoiceHeader.PeriodID = @PeriodID AND AR_InvoiceHeader.InvoiceType = @DocumentTypeID AND AR_InvoiceHeader.Status = 1 AND AR_InvoiceHeader.StatusCode=1 AND IsNull(AR_InvoiceHeader.IsReversed, 0) = 0 AND AR_InvoiceHeader.ReversalID IS NULL 
AND AR_InvoiceHeader.InvoiceID not in (SELECT AP_PaymentInvoicesDetail.InvoiceHeaderID FROM AP_PaymentHeader 
INNER JOIN  AP_PaymentInvoicesDetail ON AP_PaymentInvoicesDetail.PaymentHeaderID=AP_PaymentHeader.PaymentID
AND AP_PaymentInvoicesDetail.CompanyID=AP_PaymentHeader.CompanyID
AND AP_PaymentInvoicesDetail.PeriodID=AP_PaymentHeader.PeriodID
and AP_PaymentHeader.CompanyID = @CompanyID
AND AP_PaymentInvoicesDetail.Status=1
inner join Set_PeriodCustom on AP_PaymentHeader.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1
and AP_PaymentHeader.Status = 1 
)

ORDER BY Convert(BigInt, AR_InvoiceHeader.InvoiceNumber) ";

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


        public static DataTable GetDataPurchaseReportUnderSalesTaxAct1990(string companies, int CurrentCompanyID, int periodID, int CurrentFiscalID, Nullable<DateTime> FromDate = null, Nullable<DateTime> ToDate = null, int? ItemCatalogID = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];             
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@FiscalID", CurrentFiscalID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_PurchaseReportUnderSalesTaxAct1990.Replace("@CompanyID", "(" + companies + ")") + sb.ToString(), sqlparams);

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

        public static DataTable GetDataPurhaseReturnReportUnderSalesTaxAct1990(string companies, int CurrentCompanyID, int periodID, int CurrentFiscalID, Nullable<DateTime> FromDate = null, Nullable<DateTime> ToDate = null, int? ItemCatalogID = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@FiscalID", CurrentFiscalID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_PurchaseReturnReportUnderSalesTaxAct1990.Replace("@CompanyID", "(" + companies + ")") + sb.ToString(), sqlparams);

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

        public static DataTable GetDataAuditReportsOfIncomeOrdinance2001(string companies, int CurrentCompanyID, int periodID, int CurrentFiscalID, string AccountCode, Nullable<DateTime> FromDate = null, Nullable<DateTime> ToDate = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", CurrentCompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@FromDate", FromDate);
                sqlparams[4] = new SqlParameter("@ToDate", ToDate);
                sqlparams[4] = new SqlParameter("@AccountCode", AccountCode);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_AuditReportsOfIncomeOrdinance2001.Replace("@CompanyID", "(" + companies + ")") + sb.ToString(), sqlparams);

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

        public static DataTable TradeOtherPayable(string companyID, int periodID, int CurrentFiscalID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@FromDate", dateFrom);
                sqlparams[4] = new SqlParameter("@ToDate", dateTo);

                companyID = "(" + companyID + ")";
                
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_TradeOtherPayable.Replace("@CompanyID", companyID), sqlparams);

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


        public static DataTable TaxationReports(int CompanyID, int periodID, int fiscalID, int CustomerCategoryWiseTaxID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FiscalID", fiscalID);
                sqlparams[3] = new SqlParameter("@CustomerCategoryWiseTaxID", CustomerCategoryWiseTaxID);
                sqlparams[4] = new SqlParameter("@fromDate", dateFrom);
                sqlparams[5] = new SqlParameter("@Todate", dateTo);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_TaxtaionReport, sqlparams);

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

        public static DataTable GetLegalProfessionalCharges(string companyID, int periodID, Nullable<DateTime> dateTo, Nullable<int> CustomerID)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            string additionalParameters = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);                
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);                
                sqlparams[2] = new SqlParameter("@ToDate", dateTo);

                companyID = "(" + companyID + ")";

                if (CustomerID != null)
                {
                    sb.Append("and AR_SET_Customer.CustomerID = @CustomerID");
                    sqlparams[5] = new SqlParameter("@CustomerID", CustomerID);
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_LegalProfessionalCharges, sb.ToString()).Replace("@CompanyID", companyID), sqlparams);

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

        public static DataTable GetDataProfessionalChargesReport(string companyID, int periodID, int FiscalID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> CustomerID)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            string additionalParameters = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FiscalID", FiscalID);
                sqlparams[3] = new SqlParameter("@FromDate", dateFrom);
                sqlparams[4] = new SqlParameter("@ToDate", dateTo);
                
                companyID = "(" + companyID + ")";

                if (CustomerID != null)
                {
                    sb.Append("and AR_SET_Customer.CustomerID = @CustomerID");
                    sqlparams[5] = new SqlParameter("@CustomerID", CustomerID);
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_GetData_ProfessionalChargesReport, sb.ToString()).Replace("@CompanyID", companyID), sqlparams);

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

        public static DataTable GetDataLegalChargesReport(string companyID, int periodID, int FiscalID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> CustomerID)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            string additionalParameters = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FiscalID", FiscalID);
                sqlparams[3] = new SqlParameter("@FromDate", dateFrom);
                sqlparams[4] = new SqlParameter("@ToDate", dateTo);

                companyID = "(" + companyID + ")";

                if (CustomerID != null)
                {
                    sb.Append("and AR_SET_Customer.CustomerID = @CustomerID");
                    sqlparams[5] = new SqlParameter("@CustomerID", CustomerID);
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_GetData_LegalChargesReport, sb.ToString()).Replace("@CompanyID", companyID), sqlparams);

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




        public static DataTable Get_Paymentagainstservices(string companyID, int periodID, Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null, Nullable<int> CustomerID = null, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;


            StringBuilder filter = new StringBuilder();


            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);

                if (CustomerID > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + CustomerID.ToString());

                companyID = "(" + companyID + ")";

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_Paymentagainstservices_Report, filter.ToString()).Replace("@CompanyID", companyID), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_Paymentagainstservices_Report, filter.ToString()).Replace("@CompanyID", companyID), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_Paymentagainstservices_Report, filter.ToString()).Replace("@CompanyID", companyID), sqlparams);

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

        public static DataTable Get_ExpansesOfSugar(Nullable<int> companyID, int periodID, int fiscalID, Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null,  SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                Nullable<int> DocumentTypeID = null;
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FiscalID", fiscalID);
                sqlparams[3] = new SqlParameter("@FromDate", fromDate);
                sqlparams[4] = new SqlParameter("@ToDate", toDate);
                sqlparams[5] = new SqlParameter("@DocumentTypeiD", DocumentTypeID);



                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_ExpansesOfSugar), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_ExpansesOfSugar), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_ExpansesOfSugar), sqlparams);

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

        public static DataTable Get_RemaningGSTBalance(int InvoiceID, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@InvoiceID", InvoiceID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                string Query_Get_RemaningGSTBalance = @"
select AR_InvoiceSupplierDetail.Amount * AP_PurchaseOrderHeader.AdvanceAmountPercentage / 100 as TaxAmount
from AR_InvoiceSupplierDetail 
inner join AR_InvoiceDetail on AR_InvoiceDetail.InvoiceHeaderID = AR_InvoiceSupplierDetail.InvoiceHeaderID
and AR_InvoiceDetail.CompanyID = AR_InvoiceSupplierDetail.CompanyID
and AR_InvoiceDetail.PeriodID = AR_InvoiceSupplierDetail.PeriodID
and AR_InvoiceDetail.Status = 1
inner join AP_PurchaseOrderHeader on AP_PurchaseOrderHeader.POHeaderID = AR_InvoiceDetail.PurchaseOrderHeaderID
and AP_PurchaseOrderHeader.CompanyID = AR_InvoiceDetail.CompanyID
and AP_PurchaseOrderHeader.PeriodID = AR_InvoiceDetail.PeriodID
and AP_PurchaseOrderHeader.Status = 1

--inner join AP_PaymentHeader on AP_PaymentHeader.POHeaderID = AP_PurchaseOrderHeader.POHeaderID
--and AP_PaymentHeader.CompanyID = AP_PurchaseOrderHeader.CompanyID
--and AP_PaymentHeader.PeriodID = AP_PurchaseOrderHeader.PeriodID
--and AP_PaymentHeader.Status = 1

where AR_InvoiceSupplierDetail.InvoiceHeaderID=@InvoiceID
and AR_InvoiceSupplierDetail.Narration = 'GST'
and isnull(AP_PurchaseOrderHeader.AdvanceAmountPercentage,0) > 0
and AP_PurchaseOrderHeader.CompanyID = @CompanyID
and AP_PurchaseOrderHeader.PeriodID = @PeriodID
and isnull(AP_PurchaseOrderHeader.OnNetAmount,0) = 1
";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_RemaningGSTBalance, sqlparams);

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

        public DataTable APRAdjustmentSummaryReport(int CompanyID, int PeriodID, string doctype, string APRNo, DateTime? DateFrom, DateTime? DateTo, int? CustomerID)
        {
            {
                List<SqlParameter> sqlparams = new List<SqlParameter>();
                DataTable dtToReturn = null;
                StringBuilder additionalParameters = new StringBuilder();
                try
                {
                    sqlparams.Add(new SqlParameter("@CompanyID", CompanyID));
                    sqlparams.Add(new SqlParameter("@PeriodID", PeriodID));
                    sqlparams.Add(new SqlParameter("@FromDate", DateFrom.Value));
                    sqlparams.Add(new SqlParameter("@ToDate", DateTo.Value));
                    sqlparams.Add(new SqlParameter("@DocumentTypeID", doctype));
                    if (CustomerID.HasValue)
                    {
                        sqlparams.Add(new SqlParameter("@CustomerID", CustomerID.Value));
                    }
                    else
                    {
                        sqlparams.Add(new SqlParameter("@CustomerID", DBNull.Value));
                    }
                    if (APRNo != "")
                    {
                        additionalParameters.Append(" and AP_AdvanceSugarPayment.DocumentNo = " + APRNo.ToString());
                    }

                    DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_APRAdjustmentSummaryReport.ToString().Replace("@DocumentTypeID", doctype), additionalParameters), sqlparams.ToArray());

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

        public DataTable GETDATAforPurchaseRegisterSummary_Report(string companyID, int periodID,int FiscalID, int chartOfAccountID, DateTime dateFrom, DateTime dateTo,
           int fromDocumentType, int toDocumentType, bool reversedDocuments, string documentGroupID, Guid userID,
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
                sqlParams.Add(new SqlParameter("@CurrentFiscalID", FiscalID));
                sqlParams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));


                if (fromDocumentType != -1 && toDocumentType != -1)
                    additionalFilter.Append(" AND ARINVH.InvoiceType between " + fromDocumentType + " AND " + toDocumentType);

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
                    additionalFilter.Append(" AND ARINVH.CostCenterID = " + costCenterID.Value.ToString() + " ");
                //Set_CostCenter.CostCenterID

                companyID = "(" + companyID + ")";
                string query = FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.PurchaseRegisterSummaryReportQuery.Replace("{AccountCodeFilter}", accountCodeFilter);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, additionalFilter.ToString(), qry_docuTypeAuthenticate).Replace("@CompanyID", companyID),
                    sqlParams.ToArray());
               // DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_APRAdjustmentSummaryReport.ToString().Replace("@DocumentTypeID", doctype), additionalParameters), sqlparams.ToArray());

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


        public static DataRow Get_Payment_Against_Invoices(int companyID, int periodID, int InvoiceHeaderID, int DocumentTypeID)
        {
            DataRow drToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@InvoiceHeaderID", InvoiceHeaderID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", DocumentTypeID);

                string query = @" select AR_ReceiptInvoicesDetail.* from 
                                    AR_ReceiptTransactionHeader
                                    left join AR_ReceiptInvoicesDetail on AR_ReceiptInvoicesDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID
                                    inner join SAL_InvoiceHeader on AR_ReceiptInvoicesDetail.InvoiceHeaderID = SAL_InvoiceHeader.InvoiceID
                                    where AR_ReceiptTransactionHeader.SourceHeaderID is null and AR_ReceiptTransactionHeader.ReceiptTransactionID not in 
                                    (select SourceHeaderID from AR_ReceiptTransactionHeader rev 
                                    where rev.SourceHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID)
                                    and AR_ReceiptInvoicesDetail.CompanyID = @CompanyID 
                                    and AR_ReceiptInvoicesDetail.PeriodID = @PeriodID 
                                    and AR_ReceiptInvoicesDetail.InvoiceHeaderID = @InvoiceHeaderID 
                                    and SAL_InvoiceHeader.InvoiceType = @DocumentTypeID ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    drToReturn = ds.Tables[0].Rows[0];
            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return drToReturn;
        }

        public static bool UpdatePaymentStatusOnInvoiceByPaymentID(SqlTransaction objTransaction, int companyID, int periodID, int PaymentHeaderID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PaymentHeaderID", PaymentHeaderID);


                const string Query_Update_PaymentStatusOnInvoiceByPaymentID = @"
UPDATE AR_InvoiceHeader
SET IsPaymentCompleted = 0
FROM AR_InvoiceHeader
INNER JOIN AP_PaymentInvoicesDetail
    ON AR_InvoiceHeader.InvoiceID = AP_PaymentInvoicesDetail.invoiceHeaderID
	AND AR_InvoiceHeader.CompanyID = AP_PaymentInvoicesDetail.CompanyID
WHERE AP_PaymentInvoicesDetail.CompanyID = @CompanyID AND AP_PaymentInvoicesDetail.PeriodID = @PeriodID
AND AP_PaymentInvoicesDetail.PaymentHeaderID = @PaymentHeaderID;
";

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_PaymentStatusOnInvoiceByPaymentID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_PaymentStatusOnInvoiceByPaymentID, sqlparams) > 0)
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


        public DataTable GetCustomerSatementReport_DashBoardData(string CompanyID, int PeriodID, Guid userID, string documentGroup, DateTime? DateFrom, DateTime? DateTo, int? VendorFrom, int? VendorTo, bool filterZeroResults = true)
        {
            {
                List<SqlParameter> sqlparams = new List<SqlParameter>();
                StringBuilder additionalParameters = new StringBuilder();
                StringBuilder AddParameters = new StringBuilder();
                DataTable dtToReturn = null;

                CompanyID = "(" + CompanyID + ")";
                try
                {
                    sqlparams.Add(new SqlParameter("@CompanyID", CompanyID));
                    sqlparams.Add(new SqlParameter("@PeriodID", PeriodID));
                    sqlparams.Add(new SqlParameter("@UserID", userID));
                    sqlparams.Add(new SqlParameter("@FromDate", DateFrom.Value));
                    sqlparams.Add(new SqlParameter("@ToDate", DateTo.Value));
                    if (VendorFrom.HasValue && VendorTo.HasValue)
                    {
                        sqlparams.Add(new SqlParameter("@VendorIDFrom", VendorFrom.Value));
                        sqlparams.Add(new SqlParameter("@VendorIDTo", VendorFrom.Value));
                        additionalParameters.Append(" And FinalTAB.CustomerID between @VendorIDFrom and @VendorIDTo ");
                    }
                    else if (VendorFrom.HasValue)
                    {
                        sqlparams.Add(new SqlParameter("@VendorID", VendorFrom.Value));
                        additionalParameters.Append(" And FinalTAB.CustomerID =@VendorID ");
                    }
                    else if (VendorTo.HasValue)
                    {
                        sqlparams.Add(new SqlParameter("@VendorID", VendorTo.Value));
                        additionalParameters.Append(" And FinalTAB.CustomerID =@VendorID ");
                    }
                    if (documentGroup != "")
                        additionalParameters.Append(" AND FinalTAB.GroupName = '" + documentGroup + "'");


                    DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_CustomerStatment_NewDashBoard.Replace("@CompanyId", CompanyID), additionalParameters.ToString()), sqlparams.ToArray());

                    if (ds != null && ds.Tables.Count > 0)
                        dtToReturn = ds.Tables[0];
                    #region Filter Zero Results


                    //    DataTable dt_Filtered = dtToReturn;
                    dtToReturn.Columns.Add("IsZeroFilter", typeof(bool));
                    if (filterZeroResults)
                    {
                        foreach (DataRow dr in dtToReturn.Rows)
                        {
                            dr["IsZeroFilter"] = true;
                        }

                    }
                    else
                        foreach (DataRow dr in dtToReturn.Rows)
                        {
                            dr["IsZeroFilter"] = false;
                        }
                    #endregion

                    dtToReturn.Columns.Add("RecieptNo", typeof(String));
                    for (int i = 1; i < dtToReturn.Rows.Count; i++)
                    {
                        if (dtToReturn.Rows[i]["InvoiceGroup"].ToString() == "")
                        {
                            for (int k = 0; k < dtToReturn.Rows.Count; k++)
                            {
                                if (dtToReturn.Rows[k]["ReceiptTransactionID"].ToString() == dtToReturn.Rows[i]["SourceID"].ToString())
                                    dtToReturn.Rows[i]["InvoiceGroup"] = dtToReturn.Rows[i]["SourceID"];

                            }
                        }

                        if (dtToReturn.Rows[i]["Reversal"].ToString() != "" && dtToReturn.Rows[i]["GroupNo"].ToString() == "-1")
                        {

                            for (int j = 0; j < dtToReturn.Rows.Count; j++)
                            {
                                if (dtToReturn.Rows[j]["InvoiceGroup"].ToString() == dtToReturn.Rows[i]["InvoiceGroup"].ToString())
                                {

                                    dtToReturn.Rows[j]["Reversal"] = dtToReturn.Rows[i]["Reversal"];

                                    //if (Convert.ToDecimal(dtToReturn.Rows[j]["Amount"].ToString() != "" ? dtToReturn.Rows[j]["Amount"] : "0") != (Convert.ToDecimal(dtToReturn.Rows[i]["Amount"].ToString() != "" ? dtToReturn.Rows[i]["Amount"] : "0")))
                                    //{
                                    //    decimal difference=0;
                                    //    //check partial amount
                                    //    difference  = Convert.ToDecimal(dtToReturn.Rows[j]["Amount"]) + (Convert.ToDecimal(dtToReturn.Rows[i]["Amount"]));
                                    //   // 
                                    //    if (difference == 0)
                                    //    {
                                    //        dtToReturn.Rows[j]["Reversal"] = dtToReturn.Rows[i]["Reversal"];
                                    //    }
                                    //    else
                                    //    {
                                    //       // dtToReturn.Rows[j]["Amount"] = difference;
                                    //        dtToReturn.Rows[j]["Reversal"] = "";
                                    //            dtToReturn.Rows[i]["Reversal"] ="";
                                    //    }
                                    //}
                                    //else {

                                    //}
                                }
                            }


                        }

                        else
                        {
                            for (int j = 0; j < dtToReturn.Rows.Count; j++)
                            {
                                if (dtToReturn.Rows[i]["DocumentNo"].ToString() != "0" && dtToReturn.Rows[j]["InvoiceGroup"].ToString() == dtToReturn.Rows[i]["InvoiceGroup"].ToString() && dtToReturn.Rows[j]["GroupNo"].ToString() == "-1")
                                    dtToReturn.Rows[i]["RecieptNo"] = dtToReturn.Rows[j]["DocumentNo"];

                            }



                        }

                    }

                    try
                    {
                        if (!dtToReturn.Columns.Contains("ZeroBalance"))
                            dtToReturn.Columns.Add("ZeroBalance");



                        List<int> lsCustomer = dtToReturn.AsEnumerable().ToList().Select(x => x.Field<int>("CustomerID")).Distinct().ToList();

                        List<int> toDeleteCustomer = new List<int>();

                        DataTable dr_RowDelete = null;
                        dr_RowDelete = dtToReturn.Clone();

                        if (lsCustomer.Count > 0)
                        {
                            for (int i = 0; i < lsCustomer.Count; i++)
                            {
                                decimal computeTotalIssuance = 0;
                                DataTable dt_Customer = dtToReturn.Select("CustomerID = " + lsCustomer[i].ToString()).CopyToDataTable();

                                computeTotalIssuance = Convert.ToDecimal(dt_Customer.Compute("Sum(Amount)", ""));

                                if (computeTotalIssuance <= 0)
                                {
                                    toDeleteCustomer.Add(Convert.ToInt32(dt_Customer.Rows[0]["CustomerID"]));
                                }

                                dt_Customer = null;

                            }
                        }

                        for (int i = 0; i < toDeleteCustomer.Count; i++)
                        {
                            for (int x = 0; x < dtToReturn.Rows.Count; x++)
                            {
                                if (toDeleteCustomer[i].ToString() == dtToReturn.Rows[x]["CustomerID"].ToString())
                                {

                                    dtToReturn.Rows[x]["ZeroBalance"] = "1";

                                }
                            }
                        }



                    }
                    catch { }
                    #region

                    //dtToReturn.Columns.Add("Credit", typeof(decimal));

                    //for (int i = 1; i < dtToReturn.Rows.Count; i++)
                    //{
                    //    if (dtToReturn.Rows[i]["InvoiceGroup"].ToString() == dtToReturn.Rows[i - 1]["InvoiceGroup"].ToString() && dtToReturn.Rows[i]["GroupNo"].ToString() == "-1")
                    //    {
                    //        dtToReturn.Rows[i - 1]["RecieptNo"] = dtToReturn.Rows[i]["DocumentNo"];

                    //    }

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
        }

        public static DataTable GetInvoiceAndGLBySalaryHeaderID(int companyID, int periodID, int FiscalID, string SalaryHeaderID)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            string additionalParameters = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FiscalID", FiscalID);
                sqlparams[3] = new SqlParameter("@SalaryHeaderID", SalaryHeaderID);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_GetInvoiceAndGLDetails, sb.ToString()), sqlparams);

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


        public static DataTable GetCustomerBalance(int CompanyID, int CustomerID, DateTime Date)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@CustomerID", CustomerID);
                sqlparams[3] = new SqlParameter("@fromDate", Date);

                string Query_GetCustomerBalance = @" EXEC GetNetBalanceOfCustomer @CompanyID,@CustomerID,@fromDate ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetCustomerBalance, sqlparams);

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

    }
}
