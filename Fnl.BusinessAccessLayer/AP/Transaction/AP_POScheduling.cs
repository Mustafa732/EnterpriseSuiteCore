using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.AP.Transaction
{
    public partial class AP_POScheduling : BaseClass
    {
        private short f_RowUniqueID;

        public short RowUniqueID
        {
            get { return f_RowUniqueID; }
            set { f_RowUniqueID = value; }
        }

        #region Class methods

        public DataTable GetPOSchedulingByPODetail(int companyID, int periodID, int PODetailID, int POHeaderID)
        {
            DataTable dt = null;

            string Query_GetPOSchedulingByPODetailID = @" 
select AP_POScheduling.CompanyID, AP_POScheduling.PeriodID, AP_POScheduling.POHeaderID, AP_POScheduling.PODetailID, AP_POScheduling.Quantity, 
    AP_POScheduling.ReceivingDate, '' [Action] 
from AP_POScheduling 
    inner join AP_PurchaseOrderDetail on AP_PurchaseOrderDetail.PODetailID = AP_POScheduling.PODetailID 
        and AP_PurchaseOrderDetail.CompanyID = AP_POScheduling.CompanyID and AP_PurchaseOrderDetail.PeriodID = AP_POScheduling.PeriodID 
        and AP_PurchaseOrderDetail.POHeaderID = AP_POScheduling.POHeaderID 
where AP_POScheduling.PODetailID = @PODetailID and AP_POScheduling.POHeaderID = @POHeaderID and AP_POScheduling.CompanyID = @CompanyID 
    and AP_POScheduling.PeriodID = @PeriodID ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PODetailID", PODetailID);
                sqlparams[3] = new SqlParameter("@POHeaderID", POHeaderID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetPOSchedulingByPODetailID, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetDataByKeyTable(int pOHeaderID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            string Query_Select_By_POHeaderID_CompanyId = @"
select AP_POScheduling.CompanyID, AP_POScheduling.PeriodID, AP_POScheduling.POHeaderID, AP_POScheduling.PODetailID, AP_POScheduling.Quantity, 
    AP_POScheduling.ReceivingDate, '' [Action] 
from AP_POScheduling 
    inner join AP_PurchaseOrderDetail on AP_POScheduling.CompanyID = AP_PurchaseOrderDetail.CompanyID and AP_POScheduling.PeriodID = AP_PurchaseOrderDetail.PeriodID 
        and AP_POScheduling.POHeaderID = AP_PurchaseOrderDetail.POHeaderID and AP_POScheduling.PODetailID = AP_PurchaseOrderDetail.PODetailID 
WHERE AP_POScheduling.POHeaderID = @POHeaderID and AP_POScheduling.CompanyID = @CompanyID and AP_POScheduling.PeriodID = @PeriodID ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@POHeaderID", pOHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);


                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_POHeaderID_CompanyId, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_Select_By_POHeaderID_CompanyId, sqlparams);

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

        public DataTable GetDataByKeyTableForConnect(int pOHeaderID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            string Query_Select_By_POHeaderID_CompanyId = @"
select AP_POScheduling.CompanyID, AP_POScheduling.PeriodID, AP_POScheduling.POHeaderID, AP_POScheduling.PODetailID, AP_POScheduling.Quantity, 
    AP_POScheduling.ReceivingDate, '' [Action] 
from AP_POScheduling 
    inner join AP_PurchaseOrderDetail on AP_POScheduling.CompanyID = AP_PurchaseOrderDetail.CompanyID and AP_POScheduling.PeriodID = AP_PurchaseOrderDetail.PeriodID 
        and AP_POScheduling.POHeaderID = AP_PurchaseOrderDetail.POHeaderID and AP_POScheduling.PODetailID = AP_PurchaseOrderDetail.PODetailID 
WHERE AP_POScheduling.POHeaderID = @POHeaderID and AP_POScheduling.CompanyID = @CompanyID and AP_POScheduling.PeriodID = @PeriodID ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@POHeaderID", pOHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);


                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_POHeaderID_CompanyId, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_Select_By_POHeaderID_CompanyId, sqlparams);

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

        internal bool DeleteDataByKey(int companyID, int periodID, int poHeaderID, SqlTransaction objTransaction = null)
        {
            bool result = false;

            string Query_Delete_ByPOHeader = @" 
IF (EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME in ('AP_POScheduling')))
BEGIN
    Delete from AP_POScheduling where POHeaderID = @POHeaderID and CompanyID = @CompanyID and PeriodID = @PeriodID 
END ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@POHeaderID", poHeaderID);

                if (objTransaction != null)
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByPOHeader, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByPOHeader, sqlparams) > 0)
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
