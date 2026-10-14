using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_CollarCuff
    {
        const string Query_ValidateCollarCuff = @"Select * from SET_CollarCuff where CollarCuff = @CollarCuff and Status=1";

        const string Query_ValidateEditCollarCuff = @"Select * from SET_CollarCuff where CollarCuffID <>@CollarCuffID AND CollarCuff = @CollarCuff and Status=1";

        public static DataTable ValidateCollarCuff(string CollarCuff)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                sqlparams[0] = new SqlParameter("@CollarCuff", CollarCuff);
               
                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_ValidateCollarCuff, sqlparams);

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


        public static DataTable ValidateEditCollarCuff(int CollarCuffID, string CollarCuff)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@CollarCuff", CollarCuff);
                sqlparams[1] = new SqlParameter("@CollarCuffID", CollarCuffID);
                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_ValidateEditCollarCuff, sqlparams);

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
