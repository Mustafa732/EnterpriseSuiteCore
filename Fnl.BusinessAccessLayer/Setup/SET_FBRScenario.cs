using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_FBRScenario
    {

        public static DataTable GetDataWithSenarioID(SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            const string Query_GetDataWithSenarioID = @" select ScenarioID, (ScenarioID + ' - ' + ScenarioType) as ScenarioType from SET_FBRScenario ";

            try
            {
                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetDataWithSenarioID);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDataWithSenarioID);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataWithSenarioID);

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

    }
}
