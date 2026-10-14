using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Security
{
	public partial class ApplicationGeneralMenu
    {
        #region Queries

        const string Query_Select_By_MenuPath = @" SELECT [GeneralMenuID] FROM ApplicationGeneralMenu WHERE GeneralMenuPath = @GeneralMenuPath; ";

        #endregion

        #region Methods

        public static DataTable GetDataByMenuPath(string menuPath)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@GeneralMenuPath", menuPath);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_MenuPath, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
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

