using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;
using System.Text;

namespace FnI.BusinessAccessLayer.AR.Transaction
{
    public partial class AR_ReceiptAccountsDetail : BaseClass
    {
        public DataTable GetReceiptAccountsDetail(Nullable<int> companyID, Nullable<int> receiptHeaderID, Nullable<int> periodID, 
            SqlConnection objConnection = null)
        {
            DataSet ds = null;
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@ReceiptHeaderID", receiptHeaderID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                
                string Query_GetReceiptAccountsDetail = @" 
SELECT AR_ReceiptAccountsDetail.CompanyID, AR_ReceiptAccountsDetail.PeriodID, AR_ReceiptAccountsDetail.ReceiptAccountID, AR_ReceiptAccountsDetail.ReceiptHeaderID, 
    AR_ReceiptAccountsDetail.ChartOfAccountID, AR_ReceiptAccountsDetail.CodeCombinationID, AR_ReceiptAccountsDetail.DocumentReference, 
    AR_ReceiptAccountsDetail.Narration, AR_ReceiptAccountsDetail.DepartmentID, SET_Department.DepartmentName, AR_ReceiptAccountsDetail.CostCenterID, 
    SET_CostCenter.CostCenterName, AR_ReceiptAccountsDetail.IncurredOnID, INV_Item.Description as IncurredDescription, AR_ReceiptAccountsDetail.Amount, 
    AR_ReceiptAccountsDetail.TransactionType, GL_CodeCombinationDetail.Description, '' as TaxPercentage, '' As Action 
FROM AR_ReceiptAccountsDetail 
    INNER JOIN GL_CodeCombinationDetail ON GL_CodeCombinationDetail.ChartOfAccountID = AR_ReceiptAccountsDetail.ChartOfAccountID 
        AND GL_CodeCombinationDetail.CodeCombinationID = AR_ReceiptAccountsDetail.CodeCombinationID 
        --AND GL_CodeCombinationDetail.CompanyID = AR_ReceiptAccountsDetail.CompanyID 
    LEFT JOIN SET_CostCenter ON SET_CostCenter.CostCenterID = AR_ReceiptAccountsDetail.CostCenterID 
    LEFT JOIN SET_Department ON SET_Department.DepartmentID = AR_ReceiptAccountsDetail.DepartmentID 
    LEFT JOIN INV_Item ON INV_Item.ItemID = AR_ReceiptAccountsDetail.IncurredOnID --AND INV_Item.CompanyID = AR_ReceiptAccountsDetail.CompanyID 
WHERE AR_ReceiptAccountsDetail.ReceiptHeaderID = @ReceiptHeaderID AND AR_ReceiptAccountsDetail.CompanyID = @CompanyID 
    AND AR_ReceiptAccountsDetail.PeriodID = @PeriodID And AR_ReceiptAccountsDetail.Status = 1 ";

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetReceiptAccountsDetail, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetReceiptAccountsDetail, sqlparams);

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

        public static DataTable GetEmployeeLoanDetail(int companyID, int documentno, int periodID, int personID, string query  = "",SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@DocumentNo", documentno);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@PersonID", personID);

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

        public bool InsertPF(string DatabaseName, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                StringBuilder sb = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[18];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[2] = new SqlParameter("@ReceiptHeaderID", f_ReceiptHeaderID);
                sqlparams[3] = new SqlParameter("@ReceiptAccountID", f_ReceiptAccountID);
                sqlparams[4] = new SqlParameter("@ChartOfAccountID", f_ChartOfAccountID);
                sqlparams[5] = new SqlParameter("@CodeCombinationID", f_CodeCombinationID);

                if (!string.IsNullOrEmpty(f_DocumentReference))
                    sqlparams[6] = new SqlParameter("@DocumentReference", f_DocumentReference);
                else
                    sqlparams[6] = new SqlParameter("@DocumentReference", DBNull.Value);

                if (f_DepartmentID.HasValue)
                    sqlparams[7] = new SqlParameter("@DepartmentID", f_DepartmentID.Value);
                else
                    sqlparams[7] = new SqlParameter("@DepartmentID", DBNull.Value);

                if (f_CostCenterID.HasValue)
                    sqlparams[8] = new SqlParameter("@CostCenterID", f_CostCenterID.Value);
                else
                    sqlparams[8] = new SqlParameter("@CostCenterID", DBNull.Value);

                sqlparams[9] = new SqlParameter("@Amount", f_Amount);
                sqlparams[10] = new SqlParameter("@TransactionType", f_TransactionType);

                if (!string.IsNullOrEmpty(f_Narration))
                    sqlparams[11] = new SqlParameter("@Narration", f_Narration);
                else
                    sqlparams[11] = new SqlParameter("@Narration", DBNull.Value);

                if (f_IncurredOnID.HasValue)
                    sqlparams[12] = new SqlParameter("@IncurredOnID", f_IncurredOnID.Value);
                else
                    sqlparams[12] = new SqlParameter("@IncurredOnID", DBNull.Value);

                sqlparams[13] = new SqlParameter("@Status", f_Status);
                sqlparams[14] = new SqlParameter("@CreatedBy", f_CreatedBy);
                sqlparams[15] = new SqlParameter("@CreateDate", f_CreateDate);
                sqlparams[16] = new SqlParameter("@CreatedByValue", f_CreatedByValue);

                if (f_FiscalID.HasValue)
                    sqlparams[17] = new SqlParameter("@FiscalID", f_FiscalID.Value);
                else
                    sqlparams[17] = new SqlParameter("@FiscalID", DBNull.Value);

                sb.Append(DatabaseName);

                const string Query_InsertPF = @" INSERT INTO {0}.dbo.[AR_ReceiptAccountsDetail] ([CompanyID], [PeriodID], [ReceiptHeaderID], [ReceiptAccountID], [ChartOfAccountID], [CodeCombinationID], [DocumentReference], [DepartmentID], [CostCenterID], [Amount], [TransactionType], [Narration], [IncurredOnID], [Status], [CreatedBy], [CreateDate], [CreatedByValue], [FiscalID])
		VALUES(@CompanyID, @PeriodID, @ReceiptHeaderID, @ReceiptAccountID, @ChartOfAccountID, @CodeCombinationID, @DocumentReference, @DepartmentID, @CostCenterID, @Amount, @TransactionType, @Narration, @IncurredOnID, @Status, @CreatedBy, @CreateDate, @CreatedByValue, @FiscalID) ";

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

        public static int GetNewReceiptAccountIDForPF(int companyID, int periodID, int receiptHeaderID, string DatabaseName, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;

            try
            {
                StringBuilder sb = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ReceiptHeaderID", receiptHeaderID);

                #region Query

                string Query_Get_MaxIdForPF = @" SELECT IsNull(max(ReceiptAccountID), 0) + 1 as ReceiptAccountID FROM {0}.dbo.AR_ReceiptAccountsDetail WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [ReceiptHeaderID] = @ReceiptHeaderID ";

                #endregion

                sb.Append(DatabaseName);


                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, string.Format(Query_Get_MaxIdForPF, sb.ToString()), sqlparams));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, string.Format(Query_Get_MaxIdForPF, sb.ToString()), sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, string.Format(Query_Get_MaxIdForPF, sb.ToString()), sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
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

                const string Query_Delete_ByReceiptTransactionHeaderID = @" Update [AR_ReceiptAccountsDetail] Set [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [ReceiptHeaderID] = @ReceiptHeaderID ";

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

        public static bool DeleteDataByReceiptHeaderID(int companyID, int periodID, int transactionID, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ReceiptHeaderID", transactionID);

                const string Query_Delete_ByHeaderID = @" Delete from AR_ReceiptAccountsDetail where CompanyID = @CompanyID and PeriodID = @PeriodID and  ReceiptHeaderID = @ReceiptHeaderID ";

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByHeaderID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByHeaderID, sqlparams) > 0)
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
