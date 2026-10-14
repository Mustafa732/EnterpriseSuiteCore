using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Sales
{
    public partial class SAL_SOPaymentTerms
    {
        #region Constant Strings

        const string Query_Select_By_OrderID_CompanyId_PeriodId = @"
SELECT '' AS SerialNo, SAL_SOPaymentTerms.CompanyID, SAL_SOPaymentTerms.PeriodID, SAL_SOPaymentTerms.PaymentTermID, SAL_SOPaymentTerms.SOHeaderID, 
    SAL_SOPaymentTerms.PaymentDate, SAL_SOPaymentTerms.ReceivingDate, SAL_SOPaymentTerms.Amount, SAL_SOPaymentTerms.PaymentMode, 
    SET_ReceiptMethod.ReceiptMethodName AS PaymentModeName, IsNull(SAL_SOPaymentTerms.BankID, 0) BankID, SAL_SOPaymentTerms.InstrumentNo, AR_SET_Bank.BankName, 
    SAL_SOPaymentTerms.Status, SAL_SOPaymentTerms.CreatedBy, SAL_SOPaymentTerms.CreateDate, SAL_SOPaymentTerms.CreatedByValue, SAL_SOPaymentTerms.UpdatedBy, 
    SAL_SOPaymentTerms.UpdateDate, SAL_SOPaymentTerms.UpdatedByValue, SAL_SOPaymentTerms.Comments, '' AS Action
FROM SAL_SOPaymentTerms
            LEFT JOIN SET_ReceiptMethod ON SET_ReceiptMethod.ReceiptMethodID = SAL_SOPaymentTerms.PaymentMode
            LEFT JOIN AR_SET_Bank ON AR_SET_Bank.BankID = SAL_SOPaymentTerms.BankID
            WHERE SAL_SOPaymentTerms.SOHeaderID = @OrderID
            AND SAL_SOPaymentTerms.CompanyID = @CompanyID
            AND SAL_SOPaymentTerms.PeriodID = @PeriodID
            AND SAL_SOPaymentTerms.Status = 1
            ";

        #endregion

        #region Class Methods

        public bool InsertSOPTHeaderAndDetail(int Cid, int pID,List<SAL_SOPaymentTerms> colSOPTDetail)
        {
            bool result = false;

            try
            {
                foreach (SAL_SOPaymentTerms sopt in colSOPTDetail)
                {
                    sopt.PaymentTermID = short.Parse(SAL_SOPaymentTerms.GetNewPaymentTermID(Cid, pID).ToString());
                    sopt.Insert();
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
                
            }

            return result;
        }

        public bool DeleteData( Guid updatedBy, DateTime updateDate, string updatedByValue, int companyID, int paymentTermID, int periodID)
        {
             bool result = false;
            SqlTransaction objTransaction = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PaymentTermID", paymentTermID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", false);
                sqlparams[4] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[5] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[6] = new SqlParameter("@UpdatedByValue", updatedByValue);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByKey, sqlparams) > 0)
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

        public static DataTable GetDataByKeyTable(int orderID, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@OrderID", orderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_OrderID_CompanyId_PeriodId, sqlparams);

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

        #endregion
    }
}
