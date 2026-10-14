using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
   
    public partial class SET_TwillTape
    {
        const string Query_Validate = @"SELECT TwillTapeName FROM SET_TwillTape where TwillTapeName =  @TwillTapeName And Status=1 ";

        const string Query_ValidateEditTwillTape = @"Select * from SET_TwillTape where TwillTapeID <>@TwillTapeID AND TwillTapeName = @TwillTapeName and Status=1";

        public static DataTable ValidateCode(string Code, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                sqlparams[0] = new SqlParameter("@TwillTapeName", Code);
                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_Validate, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_Validate, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Validate, sqlparams);

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

        public static DataTable ValidateEditTwillTape(int TwillTapeId, string TwillTapeName)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@TwillTapeName", TwillTapeName);
                sqlparams[1] = new SqlParameter("@TwillTapeID", TwillTapeId);
                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_ValidateEditTwillTape, sqlparams);

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
