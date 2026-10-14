using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.AP.Transaction
{
    public partial class AP_PurchaseOrderRequisition
    {
        #region Fields

        private int f_RowUniqueID;

        #endregion

        #region Properties

        public int RowUniqueID
        {
            get { return f_RowUniqueID; }
            set { f_RowUniqueID = value; }
        }

        #endregion

        #region Class Methods

        public DataTable GetDataByKeyTable(int pOHeaderID, int CompanyID, int periodID, SqlConnection objConnection = null, SqlTransaction objSQLTransaction = null)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@POHeaderID", pOHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                string Query_Select_By_POHeaderID_CompanyId = @" SELECT Cast(1 AS SmallInt) AS RowUniqueID, *, '' as Action FROM AP_PurchaseOrderRequisition 
where AP_PurchaseOrderRequisition.POHeaderID = @POHeaderID and AP_PurchaseOrderRequisition.CompanyID = @CompanyID and AP_PurchaseOrderRequisition.PeriodID = @PeriodID ";

                if(objSQLTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objSQLTransaction, CommandType.Text, Query_Select_By_POHeaderID_CompanyId, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_Select_By_POHeaderID_CompanyId, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_POHeaderID_CompanyId, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            finally
            {
                if (dtToReturn == null)
                    dtToReturn = null;
            }

            return dtToReturn;
        }

        public bool DeletePORequisition(int companyID, int periodID, int poHeaderID, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@POHeaderID", poHeaderID);

                string Query_DeletePORequisition = @" Delete From AP_PurchaseOrderRequisition 
where CompanyID = @CompanyID and PeriodID = @PeriodID and POHeaderID = @POHeaderID ";

                if (objTransaction != null)
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DeletePORequisition, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeletePORequisition, sqlparams) > 0)
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
