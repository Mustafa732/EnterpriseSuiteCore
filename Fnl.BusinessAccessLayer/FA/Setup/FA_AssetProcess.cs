using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.FA.Transaction;
using FnI.BusinessAccessLayer.Setup;

namespace FnI.BusinessAccessLayer.FA.Setup
{
    public partial class FA_AssetProcess
    {
        #region Constant Queries

        const string Query_Select_AssetProcess_By_AssetIdentityID = @" 
SELECT FA_AssetProcess.AssetProcessID, FA_AssetProcess.AssetIdentityID, FA_AssetProcess.ProcessDate, FA_AssetProcess.LastWDV, 
    FA_AssetProcess.DepreciationAmount, FA_AssetProcess.CurrentWDV
  FROM FA_AssetProcess
  WHERE FA_AssetProcess.CompanyID = @CompanyID AND FA_AssetProcess.PeriodID = @PeriodID AND FA_AssetProcess.Status = 1 
	AND FA_AssetProcess.IsAssetClassification = @IsAssetClassification AND FA_AssetProcess.AssetIdentityID = @AssetIdentityID 
    AND FA_AssetProcess.AssetProcessID = (SELECT ISNULL(MAX(FA_AssetProcess.AssetProcessID), 0) FROM FA_AssetProcess) ";

        #endregion

        #region Class Methods

        public static DataRow GetAssetProcessByIdentity(int companyID, int periodID, int assetIdentityID, bool isAssetClassification)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@AssetIdentityID", assetIdentityID);
                sqlparams[1] = new SqlParameter("@IsAssetClassification", isAssetClassification);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_AssetProcess_By_AssetIdentityID, sqlparams);

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

        public static bool InsertAssetProcess(List<FA_AssetProcess> colAssetProcess)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertAssetProcess");

            try
            {
                foreach (FA_AssetProcess ap in colAssetProcess)
                {
                    ap.AssetProcessID = FA_AssetProcess.GetNewAssetProcessID(ap.f_CompanyID, ap.f_PeriodID, sqltrans);
                    ap.Insert(sqltrans);
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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertAssetProcess");
            }

            return result;
        }

        #endregion
    }
}
