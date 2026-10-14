using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;

using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_OperatingUnit
    {
        #region Constant Queries

        const string Query_GetDataForOperatingUnit = @"  select Row_Number() OVER(Order by p.PeriodID) SerialNo, 
		cp.CompanyID
		,p.PeriodID
		,p.FiscalId
		,p.PeriodSetName
        ,p.PeriodTypeName
		,ou.OperatingUnitId
		,ou.OperatingUnitName
		,SET_OperatingUnitType.OperatingUnitTypeName
		,ou.AddressLine1
		,SET_Country .CountryName
        FROM SET_CompanyPeriod cp 
        INNER JOIN SET_PeriodCustom p on p.PeriodID = cp.PeriodID and p.status=1
        --INNER JOIN SET_PeriodType pt on p.PeriodTypeID = pt.PeriodTypeID 
        INNER JOIN UsersInPeriod uip on uip.CompanyID = cp.CompanyID and uip.FiscalID = p.FiscalID --and uip.UserId = 'a452fd2f-e6b4-4d5f-bee9-df5f23b99a59'
        INNER JOIN GL_CodeCombinationHeader cch on cch.ChartOfAccountID = cp.ChartOfAccountID and cch.CompanyID = cp.CompanyID
        inner join SET_Company c on c.CompanyID = cp.CompanyID
        --inner join SET_OperatingUnit ou on ou.OperatingUnitId =c.OperatingUnitId
		inner join SET_OperatingUnit ou on ou.BusinessId =cp.CompanyID
		left join SET_Country on SET_Country.CountryID = ou.CountryID
		inner join SET_OperatingUnitType on SET_OperatingUnitType.OperatingUnitTypeID = ou.OperatingUnitTypeID
        WHERE cp.Status = 1 and ou.BusinessId = @BusinessId";

        const string Query_GetData_By_BusinessID = @" SELECT * FROM SET_OperatingUnit WHERE BusinessID = @BusinessID AND Status = 1 AND IsActive = 1; ";

        const string Query_BaseCurrencyAndMultiCurrency = @" SELECT BaseCurrencyID, MultiCurrency, ISNULL(SET_CurrencyConversion.Rate, 0) AS Rate 
            FROM SET_OperatingUnit 
                INNER JOIN SET_Company ON SET_Company.OperatingUnitId = SET_OperatingUnit.OperatingUnitId 
                INNER JOIN SET_Currency ON SET_Currency.CurrencyID = SET_OperatingUnit.BaseCurrencyID 
                LEFT JOIN SET_CurrencyConversion ON SET_CurrencyConversion.CurrencyID = SET_Currency.CurrencyID 
            WHERE CompanyId = @CompanyID; ";

        const string Query_GetOperatingList = @" 
SELECT [SET_OperatingUnit].[OperatingUnitId], [SET_OperatingUnit].[BusinessId], [SET_Business].[BusinessName], [SET_OperatingUnit].[OperatingUnitName], 
    [SET_OperatingUnit].[OperatingUnitTypeID], [SET_OperatingUnitType].[OperatingUnitTypeName], [SET_OperatingUnit].[AddressLine1], 
    [SET_OperatingUnit].[AddressLine2], [SET_OperatingUnit].[AddressLine3],[SET_OperatingUnit].[PhoneNumber], [SET_OperatingUnit].[CityID], [SET_City].[CityName], [SET_OperatingUnit].[ZipCode], 
    [SET_OperatingUnit].[CountryID], [SET_Country].[CountryName], [SET_OperatingUnit].[CountryOfOriginID], CountryOrigin.[CountryName] AS CountryOfOriginName, 
    [SET_OperatingUnit].[DateOfIncorporation], [SET_OperatingUnit].[BaseCurrencyID], [SET_Currency].[CurrencyName], [SET_OperatingUnit].[NatureOfOperatingID], 
    [SET_NatureOfOperating].[NatureOfOperatingName], [SET_OperatingUnit].[NTNNumber], [SET_OperatingUnit].[SalesTaxNumber], [SET_OperatingUnit].[NoofSegments] 
FROM [SET_Business] 
    INNER JOIN [SET_OperatingUnit] ON [SET_Business].[BusinessId] = [SET_OperatingUnit].[BusinessId] 
    INNER JOIN [SET_OperatingUnitType] ON [SET_OperatingUnitType].[OperatingUnitTypeId] = [SET_OperatingUnit].[OperatingUnitTypeId] 
    INNER JOIN [SET_City] ON [SET_City].[CityId] = [SET_OperatingUnit].[CityId] 
    INNER JOIN [SET_NatureOfOperating] ON [SET_NatureOfOperating].[NatureOfOperatingId] = [SET_OperatingUnit].[NatureOfOperatingId] 
    INNER JOIN [SET_Country] ON [SET_Country].[CountryId] = [SET_OperatingUnit].[CountryId] 
    LEFT JOIN [SET_Country] AS CountryOrigin ON CountryOrigin.[CountryId] = [SET_OperatingUnit].[CountryOfOriginId] 
    INNER JOIN [SET_Currency] ON [SET_Currency].[CurrencyId] = [SET_OperatingUnit].[BaseCurrencyID] 
WHERE [SET_OperatingUnit].Status = 1 AND ([SET_OperatingUnit].BusinessID = @BusinessID OR @BusinessID IS NULL) 
    AND ([SET_OperatingUnit].IsActive = @IsActive OR @IsActive IS NULL) ";

        const string Query_SelectAll_Searching_WithPaging_WithOtherTables = @"select 
        SET_OperatingUnit.OperatingUnitId,
        SET_OperatingUnit.OperatingUnitName,
        SET_Business.BusinessName,
        SET_OperatingUnitType.OperatingUnitTypeName,
        SET_Country.CountryName,
        SET_City.CityName,
        Region.CountryName as CountryRegion
        , ROW_NUMBER() Over (Order by  
        set_operatingunit.operatingunitid) as 'Row' ,
        SET_OperatingUnit.COASeparater
		from SET_OperatingUnit
        inner join SET_Business on SET_Business.BusinessId = SET_OperatingUnit.BusinessId
        inner join SET_OperatingUnitType on SET_OperatingUnitType.OperatingUnitTypeID = SET_OperatingUnit.OperatingUnitTypeID
        inner join SET_Country on SET_Country.CountryID = SET_OperatingUnit.CountryID
        inner join SET_City on SET_City.CityID = SET_OperatingUnit.CityID
        inner join SET_Country Region on Region.CountryID = SET_OperatingUnit.CountryOfOriginID ";


        const string Query_SelectAll_Searching_WithOtherTables = @"select 
        SET_OperatingUnit.OperatingUnitId,
        SET_OperatingUnit.OperatingUnitName,
        SET_Business.BusinessName,
        SET_OperatingUnitType.OperatingUnitTypeName,
        SET_Country.CountryName,
        SET_City.CityName,
        Region.CountryName as CountryRegion
        from SET_OperatingUnit
        inner join SET_Business on SET_Business.BusinessId = SET_OperatingUnit.BusinessId
        inner join SET_OperatingUnitType on SET_OperatingUnitType.OperatingUnitTypeID = SET_OperatingUnit.OperatingUnitTypeID
        inner join SET_Country on SET_Country.CountryID = SET_OperatingUnit.CountryID
        inner join SET_City on SET_City.CityID = SET_OperatingUnit.CityID
        inner join SET_Country Region on Region.CountryID = SET_OperatingUnit.CountryOfOriginID";

        const string Query_ForCompanyName = @"
          SELECT SET_Company.CompanyId,SET_OperatingUnit.OperatingUnitName
          FROM SET_OperatingUnit 
              INNER JOIN SET_Company ON SET_Company.OperatingUnitId = SET_OperatingUnit.OperatingUnitId 
     		    and SET_Company.Status = 1
          WHERE CompanyID = @CompanyID
			 and SET_OperatingUnit.OperatingUnitName like '%Re-Rolling%'
			 and SET_OperatingUnit.Status = 1 and SET_OperatingUnit.IsActive = 1
           ";

        const string Query_GetCompanyUnitPrefix = @"
SELECT ISNULL(UnitPrefix,'') AS UnitPrefix FROM SET_OperatingUnit  
INNER JOIN SET_Company ON SET_Company.OperatingUnitId = SET_OperatingUnit.OperatingUnitId 
and SET_Company.Status = 1
WHERE CompanyID = @CompanyID and SET_OperatingUnit.Status = 1 and SET_OperatingUnit.IsActive = 1";

        #endregion

        #region Class Methods

        public static DataTable GetAllData_WithOtherTables(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null, Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;
            try
            {
                if (PageIndex != null)
                {
                    sqlparamsList.Add(new SqlParameter("@PageIndex", PageIndex));
                    sqlparamsList.Add(new SqlParameter("@PageSize", PageSize));
                    sqlparamsList.Add(new SqlParameter("@TotalRows", ReturnTotalRows));
                    SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                    sqlparamTotalRows.Direction = ParameterDirection.Output;
                    SqlParameter[] sqlparams = sqlparamsList.ToArray();
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_Searching_WithPaging_WithOtherTables + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), Query_SelectAll_Searching_WithPaging_WithOtherTables + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_Searching_WithOtherTables + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }


        public static DataTable GetDataByBusinessID(int businessID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlParams = new SqlParameter[1];
                sqlParams[0] = new SqlParameter("@BusinessID", businessID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_By_BusinessID, sqlParams);

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
        
        public static DataTable GetBaseCurrencyAndIsAllowMultiCurrency(int companyID, SqlConnection objSQL_Connection = null)
        {
            DataSet ds = null;
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if(objSQL_Connection != null)
                    ds = SqlHelper.ExecuteDataset(objSQL_Connection, CommandType.Text, Query_BaseCurrencyAndMultiCurrency, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_BaseCurrencyAndMultiCurrency, sqlparams);

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
        
        public bool Insert_OperatingUnitAndCompany(SET_Company objCompany)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("Insert_OperatingUnitAndCompany");

            try
            {
                this.Insert(sqltrans);
                objCompany.Insert(sqltrans);
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
                    SqlHelper.RollBackTransaction(sqltrans, "Insert_OperatingUnitAndCompany");
            }

            return result;
        }

        public bool Delete_OperatingUnitAndCompany(SET_Company objCompany)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("Delete_OperatingUnitAndCompany");

            try
            {
                SET_Company.DeleteDataByKey(sqltrans, objCompany.UpdatedBy.Value, objCompany.UpdateDate.Value, objCompany.UpdatedByValue, objCompany.CompanyId);
                SET_OperatingUnit.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_OperatingUnitId);

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
                    SqlHelper.RollBackTransaction(sqltrans, "Delete_OperatingUnitAndCompany");
            }

            return result;
        }
        
        public static DataTable GetOperatingUnitList(Nullable<int> businessID, Nullable<bool> isActive)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                if (businessID.HasValue)
                    sqlparams[0] = new SqlParameter("@BusinessID", businessID.Value);
                else
                    sqlparams[0] = new SqlParameter("@BusinessID", DBNull.Value);

                if (isActive.HasValue)
                    sqlparams[1] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[1] = new SqlParameter("@IsActive", DBNull.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetOperatingList, sqlparams);

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

        public static DataTable GetDataForOperatingUnit(int BusinessId, Guid userID, bool isFiscalSystem = false)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@BusinessId", BusinessId);
                sqlparams[1] = new SqlParameter("@UserID", userID);

                string query = string.Empty;


                query = Query_GetDataForOperatingUnit;


                dt = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetDataByOperatingUnitID(int OperatingUnitID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlParams = new SqlParameter[1];
                sqlParams[0] = new SqlParameter("@OperatingUnitID", OperatingUnitID);

                const string Query_GetData_By_OperatingUnitID = @" SELECT * FROM SET_OperatingUnit WHERE OperatingUnitID = OperatingUnitID ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_By_OperatingUnitID, sqlParams);

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

        public static DataTable GetCompanyName(int companyID, SqlConnection objSQL_Connection = null)
        {

            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlParams = new SqlParameter[1];
                sqlParams[0] = new SqlParameter("@CompanyID", companyID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_ForCompanyName, sqlParams);

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

        public static string GetCompanyUnitPrefix(int companyID, SqlConnection objSQL_Connection = null)
        {
            string result = string.Empty;

            try
            {
                SqlParameter[] sqlParams = new SqlParameter[1];
                sqlParams[0] = new SqlParameter("@CompanyID", companyID);

                result = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetCompanyUnitPrefix, sqlParams).Tables[0].Rows[0]["UnitPrefix"].ToString();
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return result;
        }

        #endregion
    }
}
