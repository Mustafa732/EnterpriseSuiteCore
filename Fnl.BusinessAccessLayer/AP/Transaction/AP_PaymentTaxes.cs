using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.AP.Transaction
{
    public partial class AP_PaymentTaxes : BaseClass
    {
        #region Fields

        private int f_RowUniqueID;

        public int RowUniqueID
        {
            get { return f_RowUniqueID; }
            set { f_RowUniqueID = value; }
        }

        #endregion

        #region Methods

        public DataTable GetDataByPaymentHeaderID(int companyID, int paymentHeaderID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            string Query_GetDataByPaymentHeaderID = @" 
Select -1 as RowUniqueID,
	PaymentTaxID,
	PaymentHeaderID,
	PaymentInvoiceID,
	TaxName,
	TaxRate,
	AmountToBeTaxed,
	TaxAmount,
	TotalAmount
	From AP_PaymentTaxes
Where CompanyID = @CompanyID AND PeriodID = @PeriodID AND PaymentHeaderID = @PaymentHeaderID
";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PaymentHeaderID", paymentHeaderID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByPaymentHeaderID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDataByPaymentHeaderID, sqlparams);

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

        public static bool DeleteDataByPaymentHeaderID(SqlTransaction objTransaction, int companyID, int periodID, int paymentHeaderID, int paymentInvoiceID)
        {
            bool result = false;

            string Query_Delete_ByPaymentHeaderAndDetailID = @" Delete From AP_PaymentTaxes
Where PaymentHeaderID = @PaymentHeaderID AND PaymentInvoiceID = @PaymentInvoiceID  AND CompanyID = @CompanyID AND PeriodID = @PeriodID";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PaymentHeaderID", paymentHeaderID);
                sqlparams[3] = new SqlParameter("@PaymentInvoiceID", paymentInvoiceID);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByPaymentHeaderAndDetailID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByPaymentHeaderAndDetailID, sqlparams) > 0)
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
