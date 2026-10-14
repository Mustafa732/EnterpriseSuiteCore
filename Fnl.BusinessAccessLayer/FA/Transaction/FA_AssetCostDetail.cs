using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.FA.Transaction
{
    public partial class FA_AssetCostDetail
    {
        #region Constant Queries

        const string Query_GetDataByKey = @" 
SELECT FA_AssetCostDetail.AssetCostHeaderID
      ,FA_AssetCostDetail.AssetCostDetailID
      ,FA_AssetCostDetail.CostID
      ,FA_SET_Cost.CostName 
      ,FA_SET_Cost.ImpactID
      ,FA_AssetCostDetail.Amount
      ,'' AS [Action]
  FROM FA_AssetCostDetail
  INNER JOIN FA_SET_Cost ON FA_SET_Cost.CostID = FA_AssetCostDetail.CostID AND FA_SET_Cost.Status = 1
  WHERE FA_AssetCostDetail.AssetCostHeaderID = @AssetCostHeaderID AND FA_AssetCostDetail.CompanyID = @CompanyID
	AND FA_AssetCostDetail.PeriodID = @PeriodID AND FA_AssetCostDetail.Status = 1 ";

        #endregion

        #region Class Methods

        public static DataTable GetDataByKeyWithOtherTables(int assetCostHeaderID, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@AssetCostHeaderID", assetCostHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByKey, sqlparams);

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
