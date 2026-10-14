using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using Common.Utils;

using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_Business
    {
        #region Constant Queries

        const string Query_GetChartOfAccountParameterSegment = @"select SET_Business.SegmentSize0 
  from SET_Business
  Inner Join SET_Company ON (SET_Business.BusinessId = SET_Company.BusinessId)
  where
  SET_Company.CompanyId = @CompanyID And SET_Business.Status = 1 and SET_Company.Status = 1";

        const string Query_GetBusinessList = @" 
SELECT     dbo.SET_Business.BusinessId, dbo.SET_Business.BusinessName, dbo.SET_BusinessType.BusinessTypeName, dbo.SET_Country.CountryName, 
                     dbo.SET_Business.ImportRegNo, dbo.SET_Business.ExportRegNo,  dbo.SET_Business.IBANNo,  dbo.SET_Business.SwiftCode,
					   dbo.SET_Business.NTNNumber as NTNNo,  dbo.SET_Business.SalesTaxNumber as STRNNo, 
                      dbo.SET_City.CityName, Region.CountryName AS CountryRegion, dbo.SET_Business.AddressLine1 as Address1,
					   dbo.SET_Business.AddressLine2 as Address2, dbo.SET_Business.PhoneNumber, dbo.SET_Business.ZipCode, 
					   dbo.SET_Business.DateOfIncorporation, dbo.SET_Currency.CurrencyName, 
                      dbo.SET_NatureOfBusiness.NatureOfBusinessName, 
					  
					  ROW_NUMBER() OVER (ORDER BY SET_Business.BusinessId) AS 'Row' 

               FROM         dbo.SET_Business INNER JOIN
                      dbo.SET_Country ON dbo.SET_Country.CountryID = dbo.SET_Business.CountryID INNER JOIN
                      dbo.SET_City ON dbo.SET_City.CityID = dbo.SET_Business.CityID INNER JOIN
                      dbo.SET_Country AS Region ON Region.CountryID = dbo.SET_Business.CountryOfOriginID INNER JOIN
                      dbo.SET_BusinessType ON dbo.SET_BusinessType.BusinessTypeID = dbo.SET_Business.BusinessTypeID LEFT OUTER JOIN
                      dbo.SET_NatureOfBusiness ON dbo.SET_Business.NatureOfBusinessID = dbo.SET_NatureOfBusiness.NatureOfBusinessID LEFT OUTER JOIN
                      dbo.SET_Currency ON dbo.SET_Business.BaseCurrencyID = dbo.SET_Currency.CurrencyID 
            WHERE [SET_Business].Status = 1 AND ([SET_Business].IsActive = @IsActive OR @IsActive is null)
                AND (SET_Business.BusinessName Like '%' + @BusinessName + '%' or Set_Business.BusinessName is null) 
                And (SET_Business.BusinessTypeID = @BusinessType OR @BusinessType IS NULL) ";

        const string Query_SelectAll_Searching_WithPaging_WithOtherTables = @"select  
        SET_Business.BusinessId,
        SET_Business.BusinessName,        
        SET_BusinessType.BusinessTypeName,
        SET_Country.CountryName,
        SET_City.CityName,
        Region.CountryName as CountryRegion
        , ROW_NUMBER() Over (Order by  
        SET_Business.BusinessId) as 'Row' 
		from SET_Business
        inner join SET_Country on SET_Country.CountryID = SET_Business.CountryID
        inner join SET_City on SET_City.CityID = SET_Business.CityID
        left outer join SET_Country Region on Region.CountryID = SET_Business.CountryOfOriginID 
        inner join SET_BusinessType on SET_BusinessType.BusinessTypeID =SET_Business.BusinessTypeID";


        const string Query_SelectAll_Searching_WithOtherTables = @"select 
        SET_Business.BusinessId,
        SET_Business.BusinessName,        
        SET_BusinessType.BusinessTypeID ,  
        SET_BusinessType.BusinessTypeName,
        SET_Country.CountryName,
        SET_City.CityName,
        Region.CountryName as CountryRegion        
		from SET_Business
        inner join SET_Country on SET_Country.CountryID = SET_Business.CountryID
        inner join SET_City on SET_City.CityID = SET_Business.CityID
        inner join SET_Country Region on Region.CountryID = SET_Business.CountryOfOriginID 
        inner join SET_BusinessType on SET_BusinessType.BusinessTypeID =SET_Business.BusinessTypeID";
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



        public static int GetChartOfAccountParameterSegment(int companyID)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
          

                ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetChartOfAccountParameterSegment, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static DataTable GetBusinessList(string businessName, Nullable<int> businessType = null, 
            Nullable<int> isActive = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];

                
                sqlparams[0] = new SqlParameter("@BusinessName", businessName);
                               

                if(businessType.HasValue)
                    sqlparams[1] = new SqlParameter("@BusinessType", businessType.Value);
                else
                    sqlparams[1] = new SqlParameter("@BusinessType", DBNull.Value);

                if(isActive.HasValue)
                    sqlparams[2] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[2] = new SqlParameter("@IsActive", DBNull.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetBusinessList, sqlparams);

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


        public static DataTable GetBusniessByUser(string userID)
        {
            DataTable dt = null;

            try
            {

                const string Query_GetBusniessByUser = @" select distinct SET_Business.BusinessId,SET_Business.BusinessName
		from SET_Business 
		INNER JOIN SET_Company ON SET_Business.BusinessId = SET_Company.BusinessId
		inner join UsersInCompanies on UsersInCompanies.CompanyID = SET_Company.[CompanyId]
		where UsersInCompanies.UserId = @userID
		and SET_Business.IsActive = 1 and SET_Business.Status = 1";

                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@userID", userID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetBusniessByUser, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetBusniessPlusModule(int ModuleID)
        {
            DataTable dt = null;

            try
            {

                const string Query_GetBusniessPlusModule = @" select lower(replace(rtrim(ltrim(BusinessName)),' ','')) + '-' + (select lower(replace(rtrim(ltrim(Module)),' ','')) from SET_Module where ModuleID = @ModuleID) as ModuleKey  from SET_Business";

                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@ModuleID", ModuleID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetBusniessPlusModule, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
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
