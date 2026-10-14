using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.AP
{
    public partial class Ap_ProductionjobcardDetail : BaseClass
    {
        #region Class Methods

        public DataTable GetServicesByOfferID(int companyID, int recipeHeaderID)
        {
            DataTable dt = null;

            #region Query

            string Query_GetByProcDetailID = @" 
SELECT ap_ProductionJobCardDetail.CompanyID, ap_ProductionJobCardDetail.ProCardID, ap_ProductionJobCardDetail.MainProcessID, AS_MainProcess.MainProcessName, 
    ap_ProductionJobCardDetail.ProCardDetailID, ap_ProductionJobCardDetail.StartDate, ap_ProductionJobCardDetail.EndDate, ap_ProductionJobCardDetail.InchargeID, 
AR_SET_Customer.CustomerName as Incharge,
ap_ProductionJobCardDetail.SuperVisorID,
sp.CustomerName as SuperVisor,
ap_ProductionJobCardDetail.StoppageID,
'' as Action 
From ap_ProductionJobCardDetail
left join AR_SET_Customer on AR_SET_Customer.CustomerID = ap_ProductionJobCardDetail.InchargeID
--and AR_SET_Customer.CompanyID = ap_ProductionJobCardDetail.CompanyID
left join AR_SET_Customer sp on sp.CustomerID = ap_ProductionJobCardDetail.SuperVisorID
and sp.CompanyID = ap_ProductionJobCardDetail.CompanyID
inner join AS_MainProcess on AS_MainProcess.MainProcessID = ap_ProductionJobCardDetail.MainProcessID
and AS_MainProcess.CompanyID = ap_ProductionJobCardDetail.CompanyID
Where  ap_ProductionJobCardDetail.CompanyID = @CompanyID
AND ap_ProductionJobCardDetail.ProCardID = @ProCardID ";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@ProCardID", recipeHeaderID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetByProcDetailID, sqlparams);

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

        public static bool DeleteDataByKey(SqlTransaction objTransaction, int companyID, int ProCardDetailID)
        {
            bool result = false;

            string Query_Delete_ByKey = @" delete from ap_productionJobCardDetail where CompanyID = @CompanyID and ProCardDetailID = @ProCardDetailID";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@ProCardDetailID", ProCardDetailID);

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

        public bool UpdateDataByProDetailID(SqlTransaction objTransaction = null)
        {
            bool result = false;

            #region Query

            string Query_Update_By_PrimaryKeyAndCompanyID = @" UPDATE Ap_ProductionjobcardDetail 
SET MainProcessID = @MainProcessID, StartDate = @StartDate, EndDate = @EndDate, InchargeID = @InchargeID, SuperVisorID = @SuperVisorID, StoppageID = @StoppageID, UpdatedBy = @UpdatedBy, UpdateDate = @UpdateDate, UpdatedByValue = @UpdatedByValue 
WHERE ProCardDetailID = @ProCardDetailID and CompanyID = @CompanyID ";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[12];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@ProCardID", f_ProCardID);
                sqlparams[2] = new SqlParameter("@ProCardDetailID", f_ProCardDetailID);
                sqlparams[3] = new SqlParameter("@MainProcessID", f_MainProcessID);

                if (f_StartDate.HasValue)
                    sqlparams[4] = new SqlParameter("@StartDate", f_StartDate.Value);
                else
                    sqlparams[4] = new SqlParameter("@StartDate", DBNull.Value);

                if (f_EndDate.HasValue)
                    sqlparams[5] = new SqlParameter("@EndDate", f_EndDate.Value);
                else
                    sqlparams[5] = new SqlParameter("@EndDate", DBNull.Value);

                if (f_InchargeID.HasValue)
                    sqlparams[6] = new SqlParameter("@InchargeID", f_InchargeID.Value);
                else
                    sqlparams[6] = new SqlParameter("@InchargeID", DBNull.Value);

                if (f_SuperVisorID.HasValue)
                    sqlparams[7] = new SqlParameter("@SuperVisorID", f_SuperVisorID.Value);
                else
                    sqlparams[7] = new SqlParameter("@SuperVisorID", DBNull.Value);

                if (f_StoppageID.HasValue)
                    sqlparams[8] = new SqlParameter("@StoppageID", f_StoppageID.Value);
                else
                    sqlparams[8] = new SqlParameter("@StoppageID", DBNull.Value);

                if (f_UpdatedBy.HasValue)
                    sqlparams[9] = new SqlParameter("@UpdatedBy", f_UpdatedBy.Value);
                else
                    sqlparams[9] = new SqlParameter("@UpdatedBy", DBNull.Value);

                if (f_UpdateDate.HasValue)
                    sqlparams[10] = new SqlParameter("@UpdateDate", f_UpdateDate.Value);
                else
                    sqlparams[10] = new SqlParameter("@UpdateDate", DBNull.Value);

                if (!string.IsNullOrEmpty(f_UpdatedByValue))
                    sqlparams[11] = new SqlParameter("@UpdatedByValue", f_UpdatedByValue);
                else
                    sqlparams[11] = new SqlParameter("@UpdatedByValue", DBNull.Value);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_By_PrimaryKeyAndCompanyID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_By_PrimaryKeyAndCompanyID, sqlparams) > 0)
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

        public DataTable GetTopOneByMainProcessID(int CompanyID, int MainProcessID, string BodyNo ,SqlConnection objConnection = null)
        {
            DataTable dt = null;

            string Query_SelectAllByMainProcess = @"select top 1 AR_SET_Customer.CustomerName,InchargeID ,StartDate,EndDate  
from  ap_productionJobCardDetail
inner join AP_ProductionJobCardHeader on AP_ProductionJobCardHeader.ProCardID  = ap_productionJobCardDetail.ProCardID
and AP_ProductionJobCardHeader.ComapnyID = ap_productionJobCardDetail.CompanyID
left join AR_SET_Customer on AR_SET_Customer.CustomerID = ap_productionJobCardDetail.InchargeID
	--and AR_SET_Customer.CompanyID = ap_productionJobCardDetail.CompanyID
where AP_ProductionJobCardHeader.BodyNo = @BodyNo
and ap_productionJobCardDetail.MainProcessID = @MainProcessID
and ap_productionJobCardDetail.CompanyID = @CompanyID
order by ap_productionJobCardDetail.StoppageID desc ";

            try
            {
                DataSet ds = null;

                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@MainProcessID", MainProcessID);
                sqlparams[2] = new SqlParameter("@BodyNo", BodyNo);

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllByMainProcess, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAllByMainProcess, sqlparams);

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

        #endregion
    }
}
