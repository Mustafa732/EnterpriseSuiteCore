using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
    [Serializable]
    public partial class SET_Currency
    {
        #region Constant Queries

        const string Query_Select_All_WithConversion = @" SELECT c.[CurrencyID], c.[CurrencyName], isnull(cc.Rate, 0) Rate, cc.Date
            FROM Set_Currency c 
                LEFT JOIN SET_CurrencyConversion cc on cc.CurrencyID = c.CurrencyID 
            WHERE c.[Status] = 1 AND c.IsActive = @IsActive and (c.CurrencyID = @CurrencyID OR @CurrencyID is null) and (cc.[Date] <= @Date OR @Date is null); ";

        const string Query_GetCurrencyList = @" SELECT [CurrencyID], [CurrencyName], [Status], [CurrencySign] 
            From [SET_Currency] 
            WHERE Status = 1 AND (IsActive = @IsActive OR @IsActive is null)  
                AND (CurrencyID Between @CurrencyIDFrom AND @CurrencyIDTo OR @CurrencyIDFrom is null OR @CurrencyIDTo is null) ";

        #endregion

        #region Methods

        public static DataTable GetAllDataWithConversion(Nullable<int> currencyID = null, Nullable<DateTime> date = null, bool isActive = true, 
            SqlTransaction sqlTrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@IsActive", isActive);

                if (currencyID.HasValue)
                    sqlparams[1] = new SqlParameter("@CurrencyID", currencyID.Value);
                else
                    sqlparams[1] = new SqlParameter("@CurrencyID", DBNull.Value);

                if (date.HasValue)
                    sqlparams[2] = new SqlParameter("@Date", date.Value);
                else
                    sqlparams[2] = new SqlParameter("@Date", DBNull.Value);

                DataSet ds = null;

                if (sqlTrans != null)
                    ds = SqlHelper.ExecuteDataset(sqlTrans, CommandType.Text, Query_Select_All_WithConversion, sqlparams);
                else if(objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_Select_All_WithConversion, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_All_WithConversion, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetCurrencyList(Nullable<int> currencyIDFrom = null, Nullable<int> currencyIDTo = null, Nullable<int> isActive = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];

                if (currencyIDFrom.HasValue)
                    sqlparams[0] = new SqlParameter("@CurrencyIDFrom", currencyIDFrom.Value);
                else
                    sqlparams[0] = new SqlParameter("@CurrencyIDFrom", DBNull.Value);

                if (currencyIDTo.HasValue)
                    sqlparams[1] = new SqlParameter("@CurrencyIDTo", currencyIDTo.Value);
                else
                    sqlparams[1] = new SqlParameter("@CurrencyIDTo", DBNull.Value);

                if (isActive >= 0)
                    sqlparams[2] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[2] = new SqlParameter("@IsActive", DBNull.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetCurrencyList, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
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
