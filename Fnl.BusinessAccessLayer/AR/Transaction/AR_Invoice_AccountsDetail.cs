using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.AR.Transaction
{
    public partial class AR_Invoice_AccountsDetail : BaseClass
    {
        public DataTable GetDataByInvoiceHeader(int companyID, int invoiceHeaderID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@InvoiceHeaderID", invoiceHeaderID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                string Query_GetData_byInvoiceHeader = @" 
SELECT Distinct AR_Invoice_AccountsDetail.CompanyID, AR_Invoice_AccountsDetail.PeriodID, AR_Invoice_AccountsDetail.InvoiceHeaderID, 
    AR_Invoice_AccountsDetail.InvoiceDetailAccountID, AR_Invoice_AccountsDetail.ChartOfAccountID, AR_Invoice_AccountsDetail.CodeCombinationID, 
    AR_Invoice_AccountsDetail.Amount, AR_Invoice_AccountsDetail.Status, AR_Invoice_AccountsDetail.CreatedBy, AR_Invoice_AccountsDetail.CreateDate, 
    AR_Invoice_AccountsDetail.CreatedByValue, AR_Invoice_AccountsDetail.UpdatedBy, AR_Invoice_AccountsDetail.UpdateDate, AR_Invoice_AccountsDetail.UpdatedByValue, 
    AR_Invoice_AccountsDetail.DocumentReference, AR_Invoice_AccountsDetail.Narration, AR_Invoice_AccountsDetail.DepartmentID, 
    AR_Invoice_AccountsDetail.TransactionType, AR_Invoice_AccountsDetail.IncurredOnID, INV_Item.Description As IncurredDescription, 
    GL_CodeCombinationDetail.Description, SET_Department.DepartmentName, SET_CostCenter.CostcenterID, SET_CostCenter.CostcenterName,StockTransactionID, '' as Action 
FROM AR_Invoice_AccountsDetail 
    INNER JOIN GL_CodeCombinationDetail ON GL_CodeCombinationDetail.CodeCombinationID = AR_Invoice_AccountsDetail.CodeCombinationID 
        --AND GL_CodeCombinationDetail.CompanyID = AR_Invoice_AccountsDetail.CompanyID 
    left JOIN SET_Department ON SET_Department.DepartmentID = AR_Invoice_AccountsDetail.DepartmentID 
    LEFT JOIN SET_CostCenter ON SET_CostCenter.CostcenterID = AR_Invoice_AccountsDetail.CostCenterID 
    LEFT JOIN INV_Item ON AR_Invoice_AccountsDetail.IncurredOnID = INV_Item.ItemID --AND AR_Invoice_AccountsDetail.CompanyID = INV_Item.CompanyID 
WHERE AR_Invoice_AccountsDetail.CompanyID = @CompanyID and AR_Invoice_AccountsDetail.InvoiceHeaderID = @InvoiceHeaderID and PeriodID = @PeriodID 
    AND AR_Invoice_AccountsDetail.Status = 1 ";

                if(objConnection != null)
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byInvoiceHeader, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byInvoiceHeader, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public  static decimal Get_Accounts_StockTransactionID(int CompanyID, int PeriodID, int StockTransactionID, string DocumentReference, 
            SqlConnection objConnection = null)
        {
            decimal paidAmount = 0;
            DataTable dt = null;


//            string query = @" select sum(amount) Amount from AR_Invoice_AccountsDetail where CompanyID = @CompanyID and PeriodID = @PeriodID
//                           and StockTransactionID= @StockTransactionID and DocumentReference = @DocumentReference
//            ";

            string query = @"select sum(amount) Amount from AR_Invoice_AccountsDetail,AR_INVOICEHEADER where 
AR_Invoice_AccountsDetail.InvoiceHeaderID = AR_INVOICEHEADER.InvoiceID 
AND AR_Invoice_AccountsDetail.CompanyID = @CompanyID and AR_Invoice_AccountsDetail.PeriodID = @PeriodID
        and StockTransactionID= @StockTransactionID and DocumentReference = @DocumentReference
		and AR_INVOICEHEADER.ReversalID is null and isReversed  is null
";

            SqlParameter[] sqlparams = new SqlParameter[4];
            sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
            sqlparams[1] = new SqlParameter("@StockTransactionID", StockTransactionID);
            sqlparams[2] = new SqlParameter("@PeriodID", PeriodID);
            sqlparams[3] = new SqlParameter("@DocumentReference", DocumentReference);
            
            if (objConnection != null)
                dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, query, sqlparams).Tables[0];
            else
                dt = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];

            if (dt != null && dt.Rows.Count > 0)
            {
                if (dt.Rows[0]["Amount"].ToString() != "")
                {
                    paidAmount = decimal.Parse(dt.Rows[0]["Amount"].ToString());
                }
            }

            return paidAmount;
        }

        public static bool DeleteDataByKeyInvoiceHeaderID(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, int companyID, int invoiceHeaderID, int periodID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@InvoiceHeaderID", invoiceHeaderID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", false);
                sqlparams[4] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[5] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[6] = new SqlParameter("@UpdatedByValue", updatedByValue);

                const string Query_Delete_ByKeyInvoiceHeader = @" Update [AR_Invoice_AccountsDetail] Set [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [CompanyID] = @CompanyID and [InvoiceHeaderID] = @InvoiceHeaderID and [PeriodID] = @PeriodID ";

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByKeyInvoiceHeader, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByKeyInvoiceHeader, sqlparams) > 0)
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

        public DataTable GetDataByInvoiceHeader_OldDoc(int companyID, int invoiceHeaderID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@InvoiceHeaderID", invoiceHeaderID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                string Query_GetData_byInvoiceHeader = @" 
SELECT Distinct AR_Invoice_AccountsDetail.CompanyID, AR_Invoice_AccountsDetail.PeriodID, AR_Invoice_AccountsDetail.InvoiceHeaderID, 
    AR_Invoice_AccountsDetail.InvoiceDetailAccountID, AR_Invoice_AccountsDetail.ChartOfAccountID, AR_Invoice_AccountsDetail.CodeCombinationID, 
    AR_Invoice_AccountsDetail.Amount, AR_Invoice_AccountsDetail.Status, AR_Invoice_AccountsDetail.CreatedBy, AR_Invoice_AccountsDetail.CreateDate, 
    AR_Invoice_AccountsDetail.CreatedByValue, AR_Invoice_AccountsDetail.UpdatedBy, AR_Invoice_AccountsDetail.UpdateDate, AR_Invoice_AccountsDetail.UpdatedByValue, 
    AR_Invoice_AccountsDetail.DocumentReference, AR_Invoice_AccountsDetail.Narration, AR_Invoice_AccountsDetail.DepartmentID, 
    AR_Invoice_AccountsDetail.TransactionType, AR_Invoice_AccountsDetail.IncurredOnID, INV_Item.Description As IncurredDescription, 
    GL_CodeCombinationDetail.Description, SET_Department.DepartmentName, SET_CostCenter.CostcenterID, SET_CostCenter.CostcenterName,StockTransactionID, 'Add' as Action 
FROM AR_Invoice_AccountsDetail 
    INNER JOIN GL_CodeCombinationDetail ON GL_CodeCombinationDetail.CodeCombinationID = AR_Invoice_AccountsDetail.CodeCombinationID 
        --AND GL_CodeCombinationDetail.CompanyID = AR_Invoice_AccountsDetail.CompanyID 
    left JOIN SET_Department ON SET_Department.DepartmentID = AR_Invoice_AccountsDetail.DepartmentID 
    LEFT JOIN SET_CostCenter ON SET_CostCenter.CostcenterID = AR_Invoice_AccountsDetail.CostCenterID 
    LEFT JOIN INV_Item ON AR_Invoice_AccountsDetail.IncurredOnID = INV_Item.ItemID --AND AR_Invoice_AccountsDetail.CompanyID = INV_Item.CompanyID 
WHERE AR_Invoice_AccountsDetail.CompanyID = @CompanyID and AR_Invoice_AccountsDetail.InvoiceHeaderID = @InvoiceHeaderID and PeriodID = @PeriodID 
    AND AR_Invoice_AccountsDetail.Status = 1 ";

                if (objConnection != null)
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byInvoiceHeader, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byInvoiceHeader, sqlparams).Tables[0];
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
