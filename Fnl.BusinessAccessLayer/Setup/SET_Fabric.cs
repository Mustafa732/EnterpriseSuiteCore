using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
   

    public partial class SET_Fabric
    {
        const string Query_Validate = @"SELECT FabricName FROM SET_Fabric where FabricName =  @FabricName And Status=1 ";

        const string Query_ValidateEditFabric = @"Select * from SET_Fabric where FabricID <>@FabricID AND FabricName = @FabricName and Status=1";

        const string Query_SelectAllForLookup = @" SELECT * FROM [SET_Fabric] WHERE [Status] = 1 AND ([IsActive] = @IsActive OR @IsActive is null) ";

        public static DataTable ValidateCode(string Code, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                sqlparams[0] = new SqlParameter("@FabricName", Code);
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

        public static DataTable ValidateEditFabric(int FabricId, string FabricName)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@FabricName", FabricName);
                sqlparams[1] = new SqlParameter("@FabricID", FabricId);
                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_ValidateEditFabric, sqlparams);

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

        public static DataTable GetAllDataForLookup(Nullable<bool> isActive = true, string CustomerID = "", Nullable<DateTime> FromDate = null, Nullable<DateTime> ToDate = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                if (isActive.HasValue)
                    sqlparams[0] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[0] = new SqlParameter("@IsActive", DBNull.Value);

                sqlparams[1] = new SqlParameter("@CustomerID", CustomerID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);

                string Query = Query_SelectAllForLookup;

                if (CustomerID != "")
                    Query = Query + " and FabricID in (Select FabricTypeID from Fabrics_CustomerChallanInfo where CustomerID = @CustomerID and ChallanDate between @FromDate and @ToDate and Status = 1)";

                DataSet ds = null;

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams).Tables[0];

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
