using System;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_City
    {
        #region Constant Queries

        const string Query_SelectAll_By_Country = @" SELECT [CityID], [CityName], [Status], [CountryID], [IsActive], [CreatedBy], [CreateDate], 
                [UpdatedBy], [UpdateDate], [CreatedByValue], [UpdatedByValue] 
            FROM [SET_City] Where CountryId = @CountryID AND [Status] = 1 AND ([IsActive] = @IsActive OR @IsActive is null) ORDER BY [CityName]; ";

        const string Query_Delete_ByForeignKey = @" DELETE FROM SET_City WHERE [CountryID] = @CountryID; ";

        const string Query_PermanentDelete_ByKey = @" DELETE FROM SET_City WHERE [CityID] = @CityID; ";

        const string Query_SelectAllForCitydrop = @" 
            SELECT * FROM [SET_City]
    inner join SET_Country on SET_Country.CountryID  = [SET_City].CountryID
	      and SET_Country.Status = 1 and SET_Country.[IsActive] = 1
	 WHERE [SET_City].[Status] = 1 AND [SET_City].[IsActive] = 1 


             ";

        #endregion

        #region Class Methods

        /// <summary>
        /// Gets All Data by Country
        /// </summary>
        public static DataTable GetAllActiveData(int countryID, Nullable<bool> isActive = true, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] SqlParam = new SqlParameter[2];
                SqlParam[0] = new SqlParameter("@CountryID", countryID);

                if(isActive.HasValue)
                    SqlParam[1] = new SqlParameter("@IsActive", isActive.Value);
                else
                    SqlParam[1] = new SqlParameter("@IsActive", DBNull.Value);

                DataSet ds = null;

                if(objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_By_Country, SqlParam);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAll_By_Country, SqlParam);

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

        /// <summary>
        /// Permanently Delete cities on the basis of given country
        /// </summary>
        public static bool DeletePermanentByForeignKey(SqlTransaction objTransaction, int countryID)
        {
            bool result = false;

            try
            {
                SqlParameter[] SqlParam = new SqlParameter[1];
                SqlParam[0] = new SqlParameter("@CountryID", countryID);

                if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByForeignKey, SqlParam) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw new BusinessLogicException("Error In Delete, Class City" + ex.Message);
            }

            return result;
        }

        /// <Summary>
        ///  Permanently Delete City
        ///  </Summary>
        public static bool DeletePermanentByKey(SqlTransaction objTransaction, int cityID)
        {
            bool result = false;

            try
            {
                SqlParameter[] SqlParam = new SqlParameter[1];
                SqlParam[0] = new SqlParameter("@CityID", cityID);

                if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_PermanentDelete_ByKey, SqlParam) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw new BusinessLogicException("Error In Delete. Class: City, Function: DeletePermanentByKey. " + ex.Message + ex.StackTrace);
            }
            return result;
        }

        public static DataTable GetAllActiveDataforCitydrop()
        {
            DataTable dt = null;

            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllForCitydrop).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataRow GetDataByCityName(string cityName)
        {
            DataRow dr = null;

            try
            {
                string query = "SELECT * FROM SET_City WHERE CityName = '" + cityName + "'";

                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, query);

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

        #endregion
    }
}
