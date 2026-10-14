using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_Location
    {
        #region Constant Strings

        const string Query_SelectLocationNotWareHouse = @" SELECT * FROM [SET_Location] WHERE [Status] = 1 AND ([IsActive] = @IsActive OR @IsActive is null) AND  IsWareHouse <> 1 or IsWareHouse is null ";

        const string Query_SelectLocationsforRequisition = @"Select * from [SET_Location] WHERE [Status] = 1 and IsActive = 1 And ISNULL(IsWareHouse,0) = 0";//Edit By Majid

        const string Query_SelectLocationsforSale = @"Select * from [SET_Location] WHERE [Status] = 1 and IsActive = 1 And ISNULL(IsWareHouse,0) = 1";//Add By Majid

        const string Query_SelectAllARLocations = @" SELECT * FROM [SET_Location] WHERE [Status] = 1 ";

        const string Query_SelectAllLocations = @" SELECT * FROM [SET_Location] WHERE [Status] = 1 AND [IsActive] = 1 ";

        const string Query_SelectAllDataForHOSITE = @" SELECT * FROM [SET_Location] WHERE [Status] = 1 AND [IsActive] = 1 and LocationName like '%Head Office%' or LocationName like '%SITE%'";

        const string Query_SelectAllWarehouses = @" SELECT * FROM [SET_Location] WHERE [Status] = 1 AND [IsActive] = 1 AND [IsWarehouse] = 1 ";

        const string Query_SelectLocationsByName = @" SELECT * FROM [SET_Location] WHERE [Status] = 1 and LocationName = @LocationName ";

        const string Query_SelectFromLocation = @" 
Select SET_Location.LocationID,INV_StockTransactionHeader.ToLocationID,Set_Location.LocationName 
From INV_StockTransactionHeader 
    Inner Join SET_Location On SET_Location.LocationID = INV_StockTransactionHeader.TolocationID 
    Inner Join SET_DocumentType On SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID 
        AND SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID AND SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
Where SET_DocumentType.DocumentType = 'Transfer Out' AND INV_StockTransactionHeader.CompanyID = @CompanyID And INV_StockTransactionHeader.PeriodID = @PeriodID 
    And INV_StockTransactionHeader.Status = 1 ";

        const string Query_SelectLocationsByNameWithLocationType = @"SELECT Case 
                                                    when SET_Location.LocationTypeID = 1 then 'HO'
                                                    when SET_Location.LocationTypeID = 2 then 'BR'
                                                    when SET_Location.LocationTypeID = 3 then 'Plant'
                                                    when SET_Location.LocationTypeID = 4 then 'Warehouse'
                                                    else ''
                                                    end AS LocationType,* FROM [SET_Location] WHERE [Status] = 1 and LocationName = @LocationName AND SET_Location.IsWareHouse = @IsWarehouse Order By LocationName";

        const string Query_SelectAllLocationsWithLocationType = @"SELECT Case 
                                                    when SET_Location.LocationTypeID = 1 then 'HO'
                                                    when SET_Location.LocationTypeID = 2 then 'BR'
                                                    when SET_Location.LocationTypeID = 3 then 'Plant'
                                                    when SET_Location.LocationTypeID = 4 then 'Warehouse'
                                                    else ''
                                                    end AS LocationType,* FROM [SET_Location] WHERE [Status] = 1 AND [IsActive] = 1 AND SET_Location.IsWareHouse = @IsWarehouse Order By LocationName";

        #endregion

        #region Class Methods

        public static DataTable GetAllLocationForReq()//Edit By Majid
        {
            DataTable dt = null;

            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectLocationsforRequisition).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        public static DataTable GetAllLocation()
        {
            DataTable dt = null;

            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllLocations).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllLocationForSale()//Add By Majid
        {
            DataTable dt = null;

            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectLocationsforSale).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        public static DataTable GetAllData()
        {
            DataTable dt = null;

            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllLocations).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataForHOSITE()
        {
            DataTable dt = null;

            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllDataForHOSITE).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllWarehouses()
        {
            DataTable dt = null;

            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllWarehouses).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllFromLocationData(int companyid , int periodid)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", periodid);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectFromLocation , sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetDataByLocationName(string LocationName, bool IsWarehouse = false )
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@LocationName", LocationName);
                sqlparams[1] = new SqlParameter("@IsWarehouse", IsWarehouse);

                if (LocationName == "")
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllLocationsWithLocationType,sqlparams[1]).Tables[0];
                }
                else
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectLocationsByNameWithLocationType, sqlparams).Tables[0];
                }

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        //public static DataTable GetAllDataLocation()
        //{
        //    DataTable dt = null;

        //    try
        //    {
        //        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllARLocations).Tables[0];
        //    }
        //    catch (Exception ex)
        //    {
        //        SqlHelper.LogException(ex);
        //        throw ex;
        //    }

        //    return dt;
        //}

        public static DataTable GetAllDataLocation(string LocationIDs)
        {
            DataTable dt = null;
            string filter = "";
            if (LocationIDs != "")
                filter = " AND SET_Location.LocationID in (" + LocationIDs + ")";
            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllARLocations + filter).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetLocationIDByCompany(string companyID)
        {
            DataTable dt = null;

            string query_LocIDByCompany = @"select (case when SET_OperatingUnit.OperatingUnitId = 1 then (Select top 1 LocationID from SET_Location where LocationName in ('SITE','Nawabshah') and Status = 1)
when SET_OperatingUnit.OperatingUnitId = 2 then (Select top 1 LocationID from SET_Location where LocationName like 'Steel Scrap Pit' and Status = 1)
else 0 end
) as locationID
,
 (case when SET_OperatingUnit.OperatingUnitId = 1 then (Select top 1 LocationName from SET_Location where LocationName in ('SITE','Nawabshah') and Status = 1)
when SET_OperatingUnit.OperatingUnitId = 2 then (Select top 1 LocationName from SET_Location where LocationName like 'Steel Scrap Pit' and Status = 1)
else '' end
) as locationName
 from SET_Company
inner join SET_OperatingUnit on SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId
where SET_Company.CompanyId = " + companyID + "";

            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, query_LocIDByCompany).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetLocationIDByCompanyForConnect(string companyID)
        {
            DataTable dt = null;

            string query_LocIDByCompanyFroConnect = @"select (case when SET_OperatingUnit.OperatingUnitId = 1 
then (Select top 1 LocationID from SET_Location where LocationName in ('Head Office','Head Office') and Status = 1)
when SET_OperatingUnit.OperatingUnitId = 2 
then (Select top 1 LocationID from SET_Location where LocationName like 'SITE' and Status = 1)
else 0 end
) as locationID
,
 (case when SET_OperatingUnit.OperatingUnitId = 1 then (Select top 1 LocationName from SET_Location where LocationName in ('Head Office','Head Office') and Status = 1)
when SET_OperatingUnit.OperatingUnitId = 2 then (Select top 1 LocationName from SET_Location where LocationName like 'SITE' and Status = 1)
else '' end
) as locationName
 from SET_Company
inner join SET_OperatingUnit on SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId
where SET_Company.CompanyId = " + companyID + "";

            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, query_LocIDByCompanyFroConnect).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetLocationNotWareHouse(Nullable<bool> isActive = true, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                if (isActive.HasValue)
                    sqlparams[0] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[0] = new SqlParameter("@IsActive", DBNull.Value);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectLocationNotWareHouse, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectLocationNotWareHouse, sqlparams);

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

        #endregion
    }
}
