using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.AP
{
    public partial class AP_ProductionCardStoppageDetail
    {
        #region Class Methods

        public DataTable GetDatabyProcardID(int companyID, int ProcardID)
        {
            DataTable dt = null;

            string Query_GetByStoppageDetailID = @" 
select '-1' as SRNO, AP_ProductionCardStoppageDetail.CompanyID, AP_ProductionCardStoppageDetail.ProCardID, AP_ProductionCardStoppageDetail.StoppageID, 
AP_ProductionCardStoppageDetail.StoppageDetailID,
AP_ProductionCardStoppageDetail.SubProcessID,
AS_SubProcess.SubProcessName as SubProcess,
AP_ProductionCardStoppageDetail.StoppageFrom,
AP_ProductionCardStoppageDetail.StoppageTo,
AP_ProductionCardStoppageDetail.ReasonID,
AP_StoppageReason.Reason,
AP_ProductionCardStoppageDetail.Remarks,
'' as Action
 from AP_ProductionCardStoppageDetail
 inner join AP_StoppageReason on AP_StoppageReason.ReasonID = AP_ProductionCardStoppageDetail.ReasonID
 and AP_StoppageReason.CompanyID = AP_ProductionCardStoppageDetail.CompanyID
 inner join AS_SubProcess on AS_SubProcess.SubProcessID = AP_ProductionCardStoppageDetail.SubProcessID
 and AS_SubProcess.CompanyID = AP_ProductionCardStoppageDetail.CompanyID
 Where  AP_ProductionCardStoppageDetail.CompanyID = @CompanyID
AND AP_ProductionCardStoppageDetail.ProCardID = @ProCardID ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@ProCardID", ProcardID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetByStoppageDetailID, sqlparams);

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

        public static bool DeleteDataByKey(SqlTransaction objTransaction, int companyID, int StoppageDetailID)
        {
            bool result = false;

            string Query_Delete_ByKey = @" delete from ap_productionCardStoppageDetail where CompanyID = @CompanyID and StoppageDetailID = @StoppageDetailID";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@StoppageDetailID", StoppageDetailID);

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

        public static bool DeleteALLDataByProCardID(SqlTransaction objTransaction, int companyID, int ProCardID)
        {
            bool result = false;
            string Query_Delete_ByProcardID = @" delete from ap_productionCardStoppageDetail where CompanyID = @CompanyID and ProCardID = @ProCardID";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@ProCardID", ProCardID);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByProcardID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByProcardID, sqlparams) > 0)
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
