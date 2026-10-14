using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_Country
    {
        #region Constant Queries

                const string Query_PermanentDelete_ByKey = @" DELETE FROM SET_Country WHERE [CountryID] = @CountryID";

                const string Query_GetCountryList = @" SELECT [SET_Country].[CountryID], [SET_Country].[CountryName], [SET_Country].[Status] AS Status_Country,[SET_City].[CityID], [SET_City].[CityName],
                          [SET_City].[Status] AS Status_City FROM [SET_Country] INNER JOIN [SET_City] ON [SET_Country].[CountryID]  = [SET_City].[CountryID]  WHERE [SET_Country].Status = 1 AND                                           ([SET_Country].IsActive = @IsActiveCountry OR @IsActiveCountry = -1)
                          AND [SET_Country].[CountryName] like '%' + @CountryName + '%' AND [SET_City].[CityName] like '%' + @CityName + '%' 
                          AND [SET_City].Status = 1 AND ([SET_City].IsActive = @IsActiveCity OR @IsActiveCity = -1) ORDER BY [SET_Country].[CountryName], [SET_City].[CityName] ";

                const string Query_GetOnlyCity = @" SELECT [SET_City].[CityID], [SET_City].[CityName], [SET_City].[Status] AS Status_City,[SET_Country].[CountryName] FROM [SET_Country] INNER JOIN [SET_City] ON [SET_Country].[CountryID] = [SET_City].[CountryID] 
                          Where [SET_City].[CityName] like '%' + @CityName + '%' and [Set_City].[IsActive] = @IsActive OR @IsActive = -1  ORDER BY [SET_City].[CityName]";

                const string Query_GetOnlyCountry = @" SELECT [SET_Country].[CountryID], [SET_Country].[CountryName],[SET_Country].[Status] AS   Status_Country FROM [SET_Country] 
                          WHERE [SET_Country].Status = 1 AND ([SET_Country].IsActive = @IsActive OR IsActive = -1) AND  [SET_Country].[CountryName] Like '%' + @CountryName +'%'
                        ORDER BY [SET_Country].[CountryName]";

const string Query_GetDataByColumn = @" SELECT * FROM [SET_Country] WHERE {0} = @ColumnValue AND [Status] = 1";

        #endregion

        #region Class Methods

        public static bool InsertCountry_CityDet(int countryID, SET_Country objCountry, List<SET_City> ColAddCityDet)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertCountry_CityDet");

            try
            {
                DataRow drCountry = SET_Country.GetDataByKey(countryID);

                if (drCountry != null)
                    objCountry.UpdateDataByKey(sqltrans);
                else
                    objCountry.Insert(sqltrans);

                SET_City.DeletePermanentByForeignKey(sqltrans, countryID);

                foreach (SET_City rCity in ColAddCityDet)
                    rCity.Insert(sqltrans);

                result = true;
            }
            catch (BusinessLogicException ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            finally
            {
                if(result)
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "InsertCountry_CityDet");
            }

            return result;
        }

        public static bool UpdateCountry_CityDetail(SET_Country objCountry, List<SET_City> ColDeleteCtDt, List<SET_City> ColAddCityDt, List<SET_City> ColAupdateCityDt)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("Update_CountryAndCityDetail");

            try
            {
                if (!(objCountry.UpdateDataByKey(sqltrans)))
                    throw new BusinessLogicException("Error in update country: ");

                foreach (SET_City CD in ColDeleteCtDt)
                    if (!(SET_City.DeletePermanentByKey(sqltrans, CD.CityID)))
                        throw new BusinessLogicException("Error in delete city: ");

                foreach (SET_City CDt in ColAddCityDt)
                {
                    CDt.CityID = SET_City.GetNewCityID(sqltrans);
                     if (!(CDt.Insert(sqltrans)))
                     throw new BusinessLogicException("Error in insert city: ");
                }

                foreach (SET_City CD in ColAupdateCityDt)
                { 
                if (!(CD.UpdateDataByKey(sqltrans)))
                        throw new BusinessLogicException("Error in update city: ");
                }

                //foreach (SET_City CD in ColAddCityDt)
                //{
                //    if (CD.CountryID == -1)
                //    {
                //        CD.CountryID = objCountry.CountryID;

                //        if (!(CD.Insert(sqltrans)))
                //            throw new BusinessLogicException("Error in insert city: ");
                //    }
                //    else if (!(CD.UpdateDataByKey(sqltrans)))
                //        throw new BusinessLogicException("Error in update city: ");
                //}

                result = true;
            }
            catch (BusinessLogicException ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            finally
            {
                if(result)
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "Update_CountryAndCityDetail");
            }

            return result;
        }

        public static bool DeletePermanentByKey(int countryID)
        {
            bool result = false;

            try
            {
                SqlParameter[] SqlParam = new SqlParameter[1];
                SqlParam[0] = new SqlParameter("@CountryID", countryID);

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_PermanentDelete_ByKey, SqlParam) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw new BusinessLogicException("Error In Delete Class Country" + ex.Message);
            }

            return result;
        }

        public static DataTable GetCountryList(string CountryName , string CityName, int? isActiveCountry = null,
        int? isActiveCity = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CountryName", CountryName);
                sqlparams[1] = new SqlParameter("@CityName", CityName);

                sqlparams[2] = new SqlParameter("@IsActiveCountry", isActiveCountry);
                sqlparams[3] = new SqlParameter("@IsActiveCity", isActiveCity);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetCountryList, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetOnlyCity(string CityName, int? Status1 = null)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CityName", CityName);
                sqlparams[1] = new SqlParameter("@IsActive", Status1);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetOnlyCity, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetOnlyCountry(string CountryName, int? isActive = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@CountryName", CountryName);
            
                sqlparams[1] = new SqlParameter("@IsActive", isActive);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetOnlyCountry, sqlparams);

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

        public static DataTable GetDataBy(string searchColumn, string searchValue)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@ColumnValue", searchValue);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDataByColumn, searchColumn), sqlparams);

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
