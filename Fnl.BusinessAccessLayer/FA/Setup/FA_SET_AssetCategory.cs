using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using DAL.DataAccessLayer;
using Microsoft.Data.SqlClient;

namespace FnI.BusinessAccessLayer.FA.Setup
{
    public partial class FA_SET_AssetCategory
    {
        #region Const Strings

        #endregion

        const string Query_SelectDataByAssetTypeID = @" SELECT * FROM [FA_SET_AssetCategory] WHERE [Status] = 1 AND ([IsActive] = @IsActive OR @IsActive is null) AND ([CompanyID] = @CompanyID OR @CompanyID is null) AND AssetTypeID = @AssetTypeID ";

        #region Methods

        public static DataTable GetAllActiveDataByAssetTypeID(Nullable<int> companyID, int AssetTypeID, Nullable<bool> isActive = true, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@AssetTypeID", AssetTypeID);

                if (isActive.HasValue)
                    sqlparams[2] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[2] = new SqlParameter("@IsActive", DBNull.Value);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectDataByAssetTypeID, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectDataByAssetTypeID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll, sqlparams);

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
