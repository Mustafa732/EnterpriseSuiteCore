using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_UnitofMeasure
    {
        #region Queries

        const string Query_Units_By_Item = @"
SELECT 
i.ItemID, 
i.Description, 
i.ItemCatalogGroupID, 
i.WeightUOMCode, 
i.PrimaryUOMCode, 
i.VolumeUOMCode, 
wu.UOMName AS WeightUOMName, 
pu.UOMName AS PrimaryUOMName, 
vu.UOMName AS VolumeUOMName, 
ISNULL(i.ListPricePerUnit ,0) AS ListPricePerUnit,
isnull([UnitWeight],0) as [UnitWeight], isnull([UnitVolume],0) as [UnitVolume]
FROM INV_Item i
LEFT JOIN SET_UnitofMeasure wu ON wu.UOMID = i.WeightUOMCode 
LEFT JOIN SET_UnitofMeasure pu ON pu.UOMID = i.PrimaryUOMCode 
LEFT JOIN SET_UnitofMeasure vu ON vu.UOMID = i.VolumeUOMCode
LEFT JOIN INV_SET_Category ic ON ic.CategoryID = i.ItemTypeID 
WHERE  (i.ItemID = @ItemID OR @ItemID IS NULL) and (ic.CategoryID = @CategoryID OR null IS NULL)";

        const string qry = @"  select  *,ROW_NUMBER() OVER (ORDER BY p.itemid) AS 'UOMID' from
            
             ( select i.itemid, i.weightuomcode AS Code, wu.UOMName AS UOMName, isnull(i.UnitWeight,'0') as qty
            FROM INV_Item i
	            LEFT JOIN SET_UnitofMeasure wu ON wu.UOMID = i.WeightUOMCode 
	           
	            LEFT JOIN INV_SET_Category ic ON ic.CategoryID = i.ItemCatalogGroupID
            WHERE i.CompanyID = @CompanyID and (i.ItemID = @ItemID OR @ItemID IS NULL)
            union all
            select i.itemid, i.PrimaryUOMCode  AS Code, pu.UOMName AS UOMName,'1' as qty
            FROM INV_Item i
	          
	            LEFT JOIN SET_UnitofMeasure pu ON pu.UOMID = i.PrimaryUOMCode 
	          
	            LEFT JOIN INV_SET_Category ic ON ic.CategoryID = i.ItemCatalogGroupID
            WHERE i.CompanyID = @CompanyID and (i.ItemID = @ItemID OR @ItemID IS NULL)
            
            union all 
            
               select i.itemid, i.VolumeUOMCode   AS Code, vu.UOMName AS UOMName , isnull(i.UnitVolume,'0')  as qty
            FROM INV_Item i
	           
	            LEFT JOIN SET_UnitofMeasure vu ON vu.UOMID = i.VolumeUOMCode 
	            LEFT JOIN INV_SET_Category ic ON ic.CategoryID = i.ItemCatalogGroupID
            WHERE  (i.ItemID = @ItemID OR @ItemID IS NULL)
            
             )p where Code is not null";

        private const string query_GetStatus_ItemDescription = @"SELECT [AllowItemDescUpdateFlag]   FROM [INV_Item] where companyid = @companyid and ItemID = @itemid";

        const string Query_GetData_byName = @"
        SELECT UOMName FROM [SET_UnitofMeasure] WHERE [UOMName]  = @UOMname";

        const string Query_GetData_byNameForOperations = @"
        SELECT UOMID FROM [SET_UnitofMeasure] WHERE [UOMName]  = @UOMname";

        const string Query_GetUOMWeightByUnitID = @"
Select
ISNULL((Case 
			when @ItemUnitID = INV_Item.PrimaryUOMCode 
						then 1
			when @ItemUnitID = INV_Item.VolumeUOMCode 
						then  ISNULL(INV_Item.UnitVolume,1) 
			when @ItemUnitID = INV_Item.WeightUOMCode 
					then ISNULL(INV_Item.UnitWeight,1) 
					
			 end),1) UnitQuantity
			 
				From INV_ITem
		Where INV_ITem.ITemID = @ItemID --AND INV_Item.CompanyID = @CompanyID";
        #endregion

        #region Class Methods

        public static DataTable GetByItem_Or_ItemCategory(int companyID, Nullable<int> itemID)
        {
            DataTable dtToReturn = null;

            SqlParameter[] sqlparams = new SqlParameter[2];
            sqlparams[0] = new SqlParameter("@CompanyID", companyID);

            if (itemID.HasValue)
                sqlparams[1] = new SqlParameter("@ItemID", itemID.Value);
            else
                sqlparams[1] = new SqlParameter("@ItemID", DBNull.Value);

            dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text, qry, sqlparams).Tables[0];

            return dtToReturn;
        }

        public static DataTable GetByItem_Or_ItemCategory(int companyID, Nullable<int> itemID, Nullable<int> itemCategoryID, out decimal itemPrice,
            out bool ItemDescStatus)
        {
            DataTable dtToReturn = null;
            itemPrice = 0;
            ItemDescStatus = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (itemID.HasValue)
                    sqlparams[1] = new SqlParameter("@ItemID", itemID.Value);
                else
                    sqlparams[1] = new SqlParameter("@ItemID", DBNull.Value);

                if (itemCategoryID.HasValue)
                    sqlparams[2] = new SqlParameter("@CategoryID", itemCategoryID.Value);
                else
                    sqlparams[2] = new SqlParameter("@CategoryID", DBNull.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Units_By_Item, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    #region Add units to dictionary

                    itemPrice = decimal.Parse(ds.Tables[0].Rows[0].ItemArray[9].ToString());
                    Dictionary<int, string> units = new Dictionary<int, string>();

                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        if (dr["WeightUOMCode"] != null && dr["WeightUOMCode"] != DBNull.Value)
                        {
                            AddUnitToDictionary(ref units, Convert.ToInt32(dr["WeightUOMCode"]), Convert.ToString(dr["WeightUOMName"]));

                        }

                        if (dr["PrimaryUOMCode"] != null && dr["PrimaryUOMCode"] != DBNull.Value)
                        {
                            AddUnitToDictionary(ref units, Convert.ToInt32(dr["PrimaryUOMCode"]), Convert.ToString(dr["PrimaryUOMName"]));

                        }

                        if (dr["VolumeUOMCode"] != null && dr["VolumeUOMCode"] != DBNull.Value)
                        {
                            AddUnitToDictionary(ref units, Convert.ToInt32(dr["VolumeUOMCode"]), Convert.ToString(dr["VolumeUOMName"]));

                        }
                    }

                    #endregion

                    if (units.Count > 0)
                    {
                        #region Move units from Dictionary to DataTable

                        dtToReturn = new DataTable();
                        dtToReturn.Columns.Add("UOMID");
                        dtToReturn.Columns.Add("UOMName");

                        foreach (KeyValuePair<int, string> unit in units)
                        {
                            DataRow drUnit = dtToReturn.NewRow();
                            drUnit["UOMID"] = unit.Key;
                            drUnit["UOMName"] = unit.Value;

                            dtToReturn.Rows.Add(drUnit);
                        }

                        dtToReturn.AcceptChanges();

                        #endregion
                    }
                }

                ItemDescStatus = Convert.ToBoolean(SqlHelper.ExecuteScalar(CommandType.Text, query_GetStatus_ItemDescription, sqlparams));
            }
            catch (Exception ex) { SqlHelper.LogException(ex); }

            return dtToReturn;
        }

        public static DataTable GetByItem_Or_ItemCategory(int companyID, Nullable<int> itemID, Nullable<int> itemCategoryID, out decimal itemPrice)
        {
            DataTable dtToReturn = null;
            itemPrice = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (itemID.HasValue)
                    sqlparams[1] = new SqlParameter("@ItemID", itemID.Value);
                else
                    sqlparams[1] = new SqlParameter("@ItemID", DBNull.Value);

                if (itemCategoryID.HasValue)
                    sqlparams[2] = new SqlParameter("@CategoryID", itemCategoryID.Value);
                else
                    sqlparams[2] = new SqlParameter("@CategoryID", DBNull.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Units_By_Item, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    #region Add units to dictionary

                    itemPrice = decimal.Parse(ds.Tables[0].Rows[0].ItemArray[9].ToString());
                    Dictionary<int, string> units = new Dictionary<int, string>();

                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        if (dr["WeightUOMCode"] != null && dr["WeightUOMCode"] != DBNull.Value)
                        {
                            AddUnitToDictionary(ref units, Convert.ToInt32(dr["WeightUOMCode"]), Convert.ToString(dr["WeightUOMName"]));

                        }

                        if (dr["PrimaryUOMCode"] != null && dr["PrimaryUOMCode"] != DBNull.Value)
                        {
                            AddUnitToDictionary(ref units, Convert.ToInt32(dr["PrimaryUOMCode"]), Convert.ToString(dr["PrimaryUOMName"]));

                        }

                        if (dr["VolumeUOMCode"] != null && dr["VolumeUOMCode"] != DBNull.Value)
                        {
                            AddUnitToDictionary(ref units, Convert.ToInt32(dr["VolumeUOMCode"]), Convert.ToString(dr["VolumeUOMName"]));

                        }
                    }

                    #endregion

                    if (units.Count > 0)
                    {
                        #region Move units from Dictionary to DataTable

                        dtToReturn = new DataTable();
                        dtToReturn.Columns.Add("UOMID");
                        dtToReturn.Columns.Add("UOMName");

                        foreach (KeyValuePair<int, string> unit in units)
                        {
                            DataRow drUnit = dtToReturn.NewRow();
                            drUnit["UOMID"] = unit.Key;
                            drUnit["UOMName"] = unit.Value;

                            dtToReturn.Rows.Add(drUnit);
                        }

                        dtToReturn.AcceptChanges();

                        #endregion
                    }
                }
            }
            catch (Exception ex) { SqlHelper.LogException(ex); }

            return dtToReturn;
        }

        public static DataTable GetByItems_Or_ItemCategory(int companyID, Nullable<int> itemID, Nullable<int> itemCategoryID, out decimal itemPrice)
        {
            DataSet ds = null;
            itemPrice = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (itemID.HasValue)
                    sqlparams[1] = new SqlParameter("@ItemID", itemID.Value);
                else
                    sqlparams[1] = new SqlParameter("@ItemID", DBNull.Value);

                if (itemCategoryID.HasValue)
                    sqlparams[2] = new SqlParameter("@CategoryID", itemCategoryID.Value);
                else
                    sqlparams[2] = new SqlParameter("@CategoryID", DBNull.Value);

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Units_By_Item, sqlparams);
                itemPrice = decimal.Parse(ds.Tables[0].Rows[0].ItemArray[9].ToString());
            }
            catch (Exception ex) { SqlHelper.LogException(ex); }

            return ds.Tables[0];
        }

        public static DataTable GetUnitsByItemOrCategory(int companyID, Nullable<int> itemID, Nullable<int> categoryID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (itemID.HasValue)
                    sqlparams[1] = new SqlParameter("@ItemID", itemID.Value);
                else
                    sqlparams[1] = new SqlParameter("@ItemID", DBNull.Value);

                if (categoryID.HasValue)
                    sqlparams[2] = new SqlParameter("@CategoryID", categoryID.Value);
                else
                    sqlparams[2] = new SqlParameter("@CategoryID", DBNull.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Units_By_Item, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    #region Add units to dictionary

                    Dictionary<int, string> units = new Dictionary<int, string>();

                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        if (dr["WeightUOMCode"] != null && dr["WeightUOMCode"] != DBNull.Value)
                            AddUnitToDictionary(ref units, Convert.ToInt32(dr["WeightUOMCode"]), Convert.ToString(dr["WeightUOMName"]));

                        if (dr["PrimaryUOMCode"] != null && dr["PrimaryUOMCode"] != DBNull.Value)
                            AddUnitToDictionary(ref units, Convert.ToInt32(dr["PrimaryUOMCode"]), Convert.ToString(dr["PrimaryUOMName"]));

                        if (dr["VolumeUOMCode"] != null && dr["VolumeUOMCode"] != DBNull.Value)
                            AddUnitToDictionary(ref units, Convert.ToInt32(dr["VolumeUOMCode"]), Convert.ToString(dr["VolumeUOMName"]));
                    }

                    #endregion

                    if (units.Count > 0)
                    {
                        #region Move units from Dictionary to DataTable

                        dtToReturn = new DataTable();
                        dtToReturn.Columns.Add("UOMID");
                        dtToReturn.Columns.Add("UOMName");

                        foreach (KeyValuePair<int, string> unit in units)
                        {
                            DataRow drUnit = dtToReturn.NewRow();
                            drUnit["UOMID"] = unit.Key;
                            drUnit["UOMName"] = unit.Value;

                            dtToReturn.Rows.Add(drUnit);
                        }

                        dtToReturn.AcceptChanges();

                        #endregion
                    }
                }
            }
            catch (Exception ex) { SqlHelper.LogException(ex); }

            return dtToReturn;
        }

        public static DataRow GetDataByName(string uOMName)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UOMname", uOMName);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byName, sqlparams);

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

        public static DataRow GetDataByNameForOperations(string uOMName)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UOMname", uOMName);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byNameForOperations, sqlparams);

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

        public static DataRow GetUOMWeightByUnitID(int unitID, int itemID, int companyid)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@ItemUnitID", unitID);
                sqlparams[1] = new SqlParameter("@ItemID", itemID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyid);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetUOMWeightByUnitID, sqlparams);

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

        #region Helping Methods

        private static void AddUnitToDictionary(ref Dictionary<int, string> units, int key, string value)
        {
            if (!units.ContainsKey(key))
            {
                units.Add(key, value);
            }



        }

        public static DataTable GetByItem_Or_ItemCategory_UnitWeight(int companyID, Nullable<int> itemID, Nullable<int> itemCategoryID, out decimal itemPrice,
   out bool ItemDescStatus)
        {
            DataTable dtToReturn = null;
            itemPrice = 0;
            ItemDescStatus = false;
            decimal UnitWeight;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (itemID.HasValue)
                    sqlparams[1] = new SqlParameter("@ItemID", itemID.Value);
                else
                    sqlparams[1] = new SqlParameter("@ItemID", DBNull.Value);

                if (itemCategoryID.HasValue)
                    sqlparams[2] = new SqlParameter("@CategoryID", itemCategoryID.Value);
                else
                    sqlparams[2] = new SqlParameter("@CategoryID", DBNull.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Units_By_Item, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    #region Add units to dictionary

                    itemPrice = decimal.Parse(ds.Tables[0].Rows[0].ItemArray[9].ToString());
                    UnitWeight = decimal.Parse(ds.Tables[0].Rows[0].ItemArray[10].ToString());

                    Dictionary<int, string> units = new Dictionary<int, string>();

                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        if (dr["WeightUOMCode"] != null && dr["WeightUOMCode"] != DBNull.Value)
                        {
                            AddUnitToDictionary(ref units, Convert.ToInt32(dr["WeightUOMCode"]), Convert.ToString(dr["WeightUOMName"]));

                        }

                        if (dr["PrimaryUOMCode"] != null && dr["PrimaryUOMCode"] != DBNull.Value)
                        {
                            AddUnitToDictionary(ref units, Convert.ToInt32(dr["PrimaryUOMCode"]), Convert.ToString(dr["PrimaryUOMName"]));

                        }

                        if (dr["VolumeUOMCode"] != null && dr["VolumeUOMCode"] != DBNull.Value)
                        {
                            AddUnitToDictionary(ref units, Convert.ToInt32(dr["VolumeUOMCode"]), Convert.ToString(dr["VolumeUOMName"]));

                        }

                    }

                    #endregion

                    if (units.Count > 0)
                    {
                        #region Move units from Dictionary to DataTable

                        dtToReturn = new DataTable();
                        dtToReturn.Columns.Add("UOMID");
                        dtToReturn.Columns.Add("UOMName");
                        dtToReturn.Columns.Add("UnitWeight");

                        foreach (KeyValuePair<int, string> unit in units)
                        {
                            DataRow drUnit = dtToReturn.NewRow();
                            drUnit["UOMID"] = unit.Key;
                            drUnit["UOMName"] = unit.Value;
                            drUnit["UnitWeight"] = UnitWeight;
                            dtToReturn.Rows.Add(drUnit);
                        }

                        dtToReturn.AcceptChanges();

                        #endregion
                    }
                }

                ItemDescStatus = Convert.ToBoolean(SqlHelper.ExecuteScalar(CommandType.Text, query_GetStatus_ItemDescription, sqlparams));
            }
            catch (Exception ex) { SqlHelper.LogException(ex); }

            return dtToReturn;
        }


        #endregion

        public static DataTable GetAllActiveDataAlphabetically(Nullable<bool> isActive = true)
        {
            DataTable dt = null;

            const string Query_SelectAllAlphabetically = @" SELECT * FROM [SET_UnitofMeasure] WHERE [Status] = 1 AND [IsActive] = 1
 Order by [SET_UnitofMeasure].UOMName asc ";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@IsActive", isActive);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllAlphabetically, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        public static DataRow GetDataByKey(int uOMID, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UOMID", uOMID);

                DataSet ds = null;
                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byKey, sqlparams);
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
    }
}
