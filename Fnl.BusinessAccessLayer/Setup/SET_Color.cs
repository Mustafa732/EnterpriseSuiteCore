using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_Color
    {
        const string Query_ValidateColor = @"Select * from Set_Color where ColorName = @Color and Status=1";
        const string Query_ValidateEditColor = @"Select * from Set_Color where ColorID <>@ColorID AND ColorName = @ColorName and Status=1";

        const string Query_SelectAllForLookup = @" SELECT * FROM [SET_Color] WHERE [Status] = 1 AND ([IsActive] = @IsActive OR @IsActive is null) ";
        public static DataTable ValidateColor(string color)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@Color", color);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_ValidateColor, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables[0].Rows.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable ValidateEditColor(int ColorId, string ColorName)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@ColorName", ColorName);
                sqlparams[1] = new SqlParameter("@ColorID", ColorId);
                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_ValidateEditColor, sqlparams);

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


        public static DataTable GetAllDataForLookUp(Nullable<bool> isActive = true, string CustomerID = "", Nullable<DateTime> FromDate = null, Nullable<DateTime> ToDate = null)
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
                    Query = Query + " and ColorID in (Select ColorID from Fabrics_CustomerChallanInfo where CustomerID = @CustomerID and ChallanDate between @FromDate and @ToDate and Status = 1)";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams).Tables[0];
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
