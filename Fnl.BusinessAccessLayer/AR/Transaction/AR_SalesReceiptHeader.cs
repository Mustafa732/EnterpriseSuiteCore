using Common.Utils;
using DAL.DataAccessLayer;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;

namespace FnI.BusinessAccessLayer.AR.Transaction
{
    public partial class AR_SalesReceiptHeader
    {
        #region Query

        const string Query_GetDataby_Key = @" Select AR_SalesReceiptHeader.REceiptHeaderID, AR_SalesReceiptHeader.PaymentDate, AR_SalesReceiptHeader.CreatedBy, AR_SalesReceiptHeader.CreateDate,
AR_SalesReceiptHeader.CreatedByValue, AR_SalesReceiptHeader.UpdatedBy, AR_SalesReceiptHeader.UpdatedByValue, AR_SalesReceiptHeader.CustomerID, AR_SET_Customer.CustomerName
From AR_SalesReceiptHeader
Inner Join AR_SET_Customer  on AR_SET_Customer.CustomerID = AR_SalesReceiptHeader.CustomerID
And AR_SET_Customer.CompanyID = AR_SalesReceiptHeader.CompanyID
And AR_SET_Customer.PeriodID = AR_SalesReceiptHeader.PeriodID
Where AR_SalesReceiptHeader.CompanyID = @CompanyID 
And AR_SalesReceiptHeader.PeriodID = @PeriodID
And AR_SalesReceiptHeader.ReceiptHeaderID = @ReceiptHeaderID ";

        const string Query_Get_MaxDocNo = @" SELECT IsNull(max(DocumentNo), 0) + 1 as ReceiptHeaderID FROM AR_SalesReceiptHeader WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID ";

        #endregion

        public static int GetNewDocumentNo(int companyID, int periodID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_MaxDocNo, sqlparams));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_Get_MaxDocNo, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_MaxDocNo, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static DataRow GetDataBy_Key(int companyid, int periodid, int ReceiptHeaderID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@ReceiptHeaderID", ReceiptHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyid);
                sqlparams[2] = new SqlParameter("@PeriodID", periodid);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetDataby_Key, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDataby_Key, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataby_Key, sqlparams);

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

        public static bool InsertBank_PaymentDetail(int companyid, int periodid, int headerID, AR_SalesReceiptHeader objHeader, List<AR_SalesReceipt> ColAddBranchDet)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertBank_BranchDet");

            try
            {
                DataRow drBank = AR_SalesReceiptHeader.GetDataByKey(companyid, periodid, headerID);

                if (drBank != null)
                    objHeader.UpdateDataByKey(sqltrans);

                else
                    objHeader.Insert(sqltrans);

                AR_SalesReceipt obj_BankBranch = new AR_SalesReceipt();
                obj_BankBranch.DeletePermanentByForeignKey(sqltrans, headerID);

                foreach (AR_SalesReceipt rBranch in ColAddBranchDet)
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

        public static bool UpdateBank_PaymentDetail(int companyid, int periodid, AR_SalesReceiptHeader objHeader, List<AR_SalesReceipt> ColDeleteBrDt, List<AR_SalesReceipt> ColAddBranchDt)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("Update_BankAndBranchDetail");

            try
            {
                objHeader.CompanyID = companyid;
                objHeader.PeriodID = periodid;
                if (!(objHeader.UpdateDataByKey(sqltrans)))
                    throw new BusinessLogicException("Error in update Header: ");

                foreach (AR_SalesReceipt CD in ColDeleteBrDt)
                    if (!(AR_SalesReceipt.DeletePermanentByKey(sqltrans, CD.SaleReceiptID)))
                        throw new BusinessLogicException("Error in delete Branch: ");

                int counter = 0;
                foreach (AR_SalesReceipt BDt in ColAddBranchDt)
                {
                    if (BDt.BankBranchID == -1)
                    {
                        BDt.BankBranchID = AR_SalesReceipt.GetNewSaleReceiptID(companyid, periodid) + counter;
                        counter++;
                    }
                }

                foreach (AR_SalesReceipt BD in ColAddBranchDt)
                {
                    BD.CompanyID = companyid;
                    BD.PeriodID = periodid;
                    if (BD.SaleReceiptID == -1)
                    {
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
    }
}
