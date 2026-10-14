using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
    [Serializable]
    public partial class SET_CurrencyConversion
    {
        #region Constant Queries

        const string Query_GetData_byKey = @" SELECT * FROM [SET_CurrencyConversion] WHERE [CurrencyID] = @CurrencyID AND [Date] = @Date; ";

        const string Query_Update_By_PrimaryKey = @" UPDATE [SET_CurrencyConversion] SET [Type] = @Type, [Rate] = @Rate, [UpdatedBy] = @UpdatedBy, 
                [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue, [SerialNo] = @SerialNo, [Status] = @Status 
            WHERE [CurrencyID] = @CurrencyID AND [Date] = @Date; ";

        const string Query_Getdata_By_CurrencyRates = @" SELECT cc.CurrencyID, cc.Date, c.CurrencyName, cc.Rate, cc.Type, cc.Status, c.CurrencySign 
            FROM SET_CurrencyConversion cc 
                INNER JOIN SET_Currency c ON cc.CurrencyID = c.CurrencyID where c.CurrencyName Like '%'+@CurrencyName+'%'; ";


        #endregion

        #region Constructors

        public SET_CurrencyConversion(int currencyID, DateTime date)
            : base()
        {
            Load(currencyID, date);
        }

        public SET_CurrencyConversion(int currencyID, DateTime date, SqlTransaction objSQL_Transaction)
            : base()
        {
            Load(currencyID, date, objSQL_Transaction);
        }

        #endregion

        #region Loading Methods

        private void Load(int currencyID, DateTime date, SqlTransaction objSQL_Transaction = null)
        {
            DataRow drCurrencyConversion = GetDataByKeyAndDate(currencyID, date, objSQL_Transaction);

            if (drCurrencyConversion != null)
            {
                this.f_IsDataLoaded = true;

                this.f_CurrencyID = Convert.ToInt16(drCurrencyConversion["CurrencyID"]);
                this.f_Date = Convert.ToDateTime(drCurrencyConversion["Date"]);
                this.f_Type = Convert.ToString(drCurrencyConversion["Type"]);
                this.f_Rate = Convert.ToDecimal(drCurrencyConversion["Rate"]);
                this.f_SerialNo = Convert.ToInt16(drCurrencyConversion["SerialNo"]);
                this.f_Status = Convert.ToBoolean(drCurrencyConversion["Status"]);

                this.f_CreatedBy = Guid.Parse(drCurrencyConversion["CreatedBy"].ToString());
                this.f_CreateDate = Convert.ToDateTime(drCurrencyConversion["CreateDate"]);
                this.f_CreatedByValue = Convert.ToString(drCurrencyConversion["CreatedByValue"]);

                if (drCurrencyConversion["UpdatedBy"] != null && drCurrencyConversion["UpdatedBy"] != DBNull.Value)
                {
                    this.f_UpdatedBy = Guid.Parse(drCurrencyConversion["UpdatedBy"].ToString());
                    this.f_UpdateDate = Convert.ToDateTime(drCurrencyConversion["UpdateDate"]);
                    this.f_UpdatedByValue = Convert.ToString(drCurrencyConversion["UpdatedByValue"]);
                }
            }
        }

        #endregion

        #region Class Methods

        public static DataRow GetDataByKeyAndDate(int currencyID, DateTime date, SqlTransaction objSQL_Transaction = null)
        {
            DataSet ds = null;
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CurrencyID", currencyID);
                sqlparams[1] = new SqlParameter("@Date", date);

                if(objSQL_Transaction != null)
                    ds = SqlHelper.ExecuteDataset(objSQL_Transaction, CommandType.Text, Query_GetData_byKey, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byKey, sqlparams);

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

        public bool UpdateDataByKey(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[9];
                sqlparams[0] = new SqlParameter("@CurrencyID", f_CurrencyID);
                sqlparams[1] = new SqlParameter("@Date", f_Date);
                sqlparams[2] = new SqlParameter("@Type", f_Type);
                sqlparams[3] = new SqlParameter("@Rate", f_Rate);
                sqlparams[4] = new SqlParameter("@SerialNo", f_SerialNo);
                sqlparams[5] = new SqlParameter("@Status", f_Status);
                sqlparams[6] = new SqlParameter("@UpdatedBy", f_UpdatedBy.Value);
                sqlparams[7] = new SqlParameter("@UpdateDate", f_UpdateDate.Value);
                sqlparams[8] = new SqlParameter("@UpdatedByValue", f_UpdatedByValue);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_By_PrimaryKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_By_PrimaryKey, sqlparams) > 0)
                        result = true;
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataTable GetCurrencyConversion(string CurrencyName)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CurrencyName", CurrencyName);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Getdata_By_CurrencyRates,sqlparams);

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
