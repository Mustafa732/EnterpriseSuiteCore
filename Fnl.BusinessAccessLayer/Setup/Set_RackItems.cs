using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Sql;
using Microsoft.Data.SqlClient;
using System.Data;
using DAL.DataAccessLayer;
namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SetRackItems
    {

        const string Query_GetDataByKeyTable_RackItems = @"
SELECT Set_RackItems.RackID ,Set_RackItems.ItemID,INV_Item.Description as ItemDescription,
INV_SET_Category.CategoryName,
INV_SET_Category.CategoryID as ItemCategoryID  ,
'' as Action
FROM Set_RackItems 
  
  INNER JOIN INV_Item on
  Set_RackItems.ItemID = INV_Item.ItemID and
  --Set_RackItems.CompanyID = INV_Item.CompanyID and
  INV_Item.Status = 1 
  
  INNER JOIN INV_SET_Category on
  INV_Item.ItemTypeID = INV_SET_Category.CategoryID and
  --INV_Item.CompanyID = INV_SET_Category.CompanyID and
  INV_SET_Category.Status = 1
  
  where Set_RackItems.RackID=@RackID and Set_RackItems.CompanyID=@CompanyID";

        const string
            Query_RackItemsbyItemIDandLocationID = @"SELECT Distinct Set_RackItems.RackID,SET_Rack.RackName 
,Set_RackItems.ItemID,INV_Item.Description as ItemDescription


FROM Set_RackItems 
  INNER JOIN SET_Rack ON Set_RackItems.RackID = SET_Rack.RackID 
     AND SET_Rack.Status=1
  
  INNER JOIN INV_Item on
  Set_RackItems.ItemID = INV_Item.ItemID and
  --Set_RackItems.CompanyID = INV_Item.CompanyID and
  INV_Item.Status = 1 
 
where Set_RackItems.ItemID=@ItemID and Set_RackItems.CompanyID=@CompanyID
AND SET_Rack.LocationID=@LocationID
  ";

        public static DataTable GetDataByKeyTable_RackItems(int rackid, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@RackID", rackid);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByKeyTable_RackItems, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataTable GetAllRackbyItemIDLocationID(int itemID, int LocationID, int companyID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@ItemID", itemID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@LocationID", LocationID);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_RackItemsbyItemIDandLocationID, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

    }
}
