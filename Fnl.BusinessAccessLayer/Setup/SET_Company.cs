using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_Company
    {
        #region Properties

        public string CompanyName
        {
            get { return BusinessId_Business.BusinessName + " - " + OperatingUnitId_OperatingUnit.OperatingUnitName; }
        }

        public string CompanyNameWithoutOperatingUnit
        {
            get { return BusinessId_Business.BusinessName; }
        }

        #endregion

        #region Constant Queries
        const string Query_Select_By_PrimaryKey_WithOtherTable = @" SELECT 
		SET_Company.[CompanyId],
		(SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName) AS CompanyName,
		SET_Company.[OperatingUnitId],
		SET_OperatingUnit.OperatingUnitName,
		ISNULL(SET_OperatingUnit.NoofSegments,0) As NoofSegments,
		ISNULL(SET_OperatingUnit.SegmentSize0,0) AS SegmentSize0,
		ISNULL(SET_OperatingUnit.SegmentSize1,0) AS SegmentSize1,
		ISNULL(SET_OperatingUnit.SegmentSize2,0) AS SegmentSize2,
		ISNULL(SET_OperatingUnit.SegmentSize3,0) AS SegmentSize3,
		ISNULL(SET_OperatingUnit.SegmentSize4,0) AS SegmentSize4,
		ISNULL(SET_OperatingUnit.SegmentSize5,0) AS SegmentSize5,
		ISNULL(SET_OperatingUnit.SegmentSize6,0) AS SegmentSize6,
		ISNULL(SET_OperatingUnit.SegmentSize7,0) AS SegmentSize7,
		ISNULL(SET_OperatingUnit.SegmentSize8,0) AS SegmentSize8,
		ISNULL(SET_OperatingUnit.SegmentSize9,0) AS SegmentSize9,
		SET_Company.[BusinessId],
		SET_Business.BusinessName,
		SET_Company.[CreateDate],
		SET_Company.[UpdateDate],
		SET_Company.[Status],
		SET_Company.[CreatedByValue],
		SET_Company.[UpdatedByValue],
		SET_Company.[CreatedBy],
		SET_Company.[UpdatedBy]
		FROM SET_Company 
		INNER JOIN SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId
        INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId 
		WHERE SET_Company.[CompanyId] = @CompanyId AND SET_Company.[Status] = 1";
        
        const string Query_SelectAllWithCompanyName = @" SELECT [SET_Company].[CompanyId], 
                ([SET_Business].[BusinessName] + ' - ' + [SET_OperatingUnit].[OperatingUnitName]) AS CompanyName, [SET_Company].[OperatingUnitId], 
                [SET_Company].[BusinessId], [SET_Company].[CreateDate], [SET_Company].[UpdateDate], [SET_Company].[Status], [SET_Company].[CreatedByValue], 
                [SET_Company].[UpdatedByValue], [SET_Company].[CreatedBy], [SET_Company].[UpdatedBy], 'False' AS IsAssigned 
            FROM SET_Company 
                INNER JOIN SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId 
                INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId 
            WHERE SET_Company.Status = 1; ";

        const string Query_SelectAllAssingedCompaniesToUser = @" SELECT [SET_Company].[CompanyId], 
                ([SET_Business].[BusinessName] + ' - ' + [SET_OperatingUnit].[OperatingUnitName]) AS CompanyName, 
                CASE WHEN UsersInCompanies.IsAssigned > 0 THEN 'True' ELSE 'False' END AS IsAssigned 
            FROM SET_Company 
                INNER JOIN SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId 
                INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId 
                left join 
		        (
			        SELECT SET_Company.[CompanyId] AS IsAssigned 
			        FROM SET_Company 
			            INNER JOIN UsersInCompanies ON UsersInCompanies.CompanyID = SET_Company.[CompanyId]	
			        WHERE UsersInCompanies.UserId = @UserId AND SET_Company.Status = 1 
			        GROUP BY SET_Company.[CompanyId] 
		        )UsersInCompanies ON UsersInCompanies.IsAssigned = SET_Company.CompanyId 
            WHERE SET_Company.Status = 1; ";

        const string Query_Get_CompanyID_ByBussinessAndOperatingUnitIDs = @" SELECT [CompanyId] FROM SET_Company WHERE [OperatingUnitId] = @OperatingUnitID 
                AND [BusinessId] = @BusinessID; ";

        const string Query_GetCompanyList = @" SELECT [SET_Company].[CompanyId], 
                ([SET_Business].[BusinessName] + ' - ' + [SET_OperatingUnit].[OperatingUnitName]) AS [CompanyName], [SET_Company].[OperatingUnitId], 
                [SET_Company].[BusinessId], [SET_BusinessType].[BusinessTypeName], [SET_Company].[Status] 
            FROM [SET_Company] 
                CROSS JOIN [SET_BusinessType] 
                INNER JOIN SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId 
                INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId 
            WHERE [SET_Company].Status = @Status or @Status is null; ";

        const string Query_GetCompanyListByBusiness = @" 
SELECT SET_Company.CompanyId, (SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName) AS CompanyName, SET_Company.OperatingUnitId, 
    SET_Company.BusinessId, SET_BusinessType.BusinessTypeName, SET_Company.Status,SET_OperatingUnit.PhoneNumber,SET_OperatingUnit.AddressLine1
	,SET_OperatingUnit.ZipCode,SET_OperatingUnit.FaxNumber,SET_OperatingUnit.NTNNumber,SET_OperatingUnit.SalesTaxNumber as STRNNo
	,SET_Country.CountryName,SET_City.CityName,SET_Currency.CurrencyName
	,CASE SET_OperatingUnit.ProvinceID
	WHEN 1 THEN 'Balochistan'
	WHEN 2 THEN 'KPK'
	WHEN 3 THEN 'Punjab'
	WHEN 4 THEN 'Sindh'
	WHEN 5 THEN 'Other'
	ELSE NULL
	END AS ProvinceName
FROM SET_Company 
    INNER JOIN SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId 
    INNER JOIN SET_BusinessType ON SET_BusinessType.BusinessTypeID = SET_Business.BusinessTypeID
    INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId
	INNER JOIN SET_Country ON SET_Country.CountryID = SET_OperatingUnit.CountryID AND SET_Country.Status = 1
	INNER JOIN SET_City ON SET_City.CityID = SET_OperatingUnit.CityID AND SET_City.Status = 1
	INNER JOIN SET_Currency ON SET_Currency.CurrencyID = SET_OperatingUnit.BaseCurrencyID AND SET_Currency.Status = 1
WHERE (SET_Company.BusinessID = @BusinessID OR @BusinessID IS NULL) AND (SET_Company.Status = @Status OR @Status IS NULL); ";   // IsActive field is not present in SET_Company table

        #endregion

        #region Class Methods

        public static DataTable GetAllDataWithCompanyName()
        {
            DataTable dtToReturn = null;

            try
            {
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllWithCompanyName);

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

        public static DataTable GetAllCompaniesWithAssignedToUser(Guid userID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserId", userID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllAssingedCompaniesToUser, sqlparams);

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
        
        public static int GetIdBy(int operatingUnitID, int businessID)
        {
            int id = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@OperatingUnitID", operatingUnitID);
                sqlparams[1] = new SqlParameter("@BusinessID", businessID);

                id = int.Parse(Convert.ToString(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_CompanyID_ByBussinessAndOperatingUnitIDs, sqlparams)));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return id;
        }

        public static DataTable GetCompanyList(int status)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@Status", status);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetCompanyList, sqlparams);

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

        public static DataTable GetCompanyList(Nullable<int> businessID, Nullable<int> status)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                if(businessID.HasValue)
                    sqlparams[0] = new SqlParameter("@BusinessID", businessID.Value);
                else
                    sqlparams[0] = new SqlParameter("@BusinessID", DBNull.Value);

                if(status.HasValue)
                    sqlparams[1] = new SqlParameter("@Status", status);
                else
                    sqlparams[1] = new SqlParameter("@Status", DBNull.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetCompanyListByBusiness, sqlparams);

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

        public static DataRow GetDataByKeyWithOtherTables(int companyId)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyId", companyId);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_PrimaryKey_WithOtherTable, sqlparams);

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

