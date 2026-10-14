using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Inventory.Setup
{
    public partial class INV_SET_Category
    {
        public static DataTable GetAllActiveDataINV_StatusCode(Nullable<int> companyID, Nullable<int> selected_Category, SqlConnection objConnection = null)
        {
            //Nullable<bool> isActive = true
            const string Query_Select_inv = @" SELECT INV_SET_Category.*,INV_Item.* FROM INV_Item
inner join SET_ItemCatalog on SET_ItemCatalog.ItemCatalogID = INV_Item.ItemCatalogGroupID
inner join INV_SET_Category on INV_SET_Category.CategoryID = INV_Item.ItemTypeID
	-- 
WHERE INV_SET_Category.Status = 1 and INV_Item.Status = 1  --AND (INV_SET_Category.CompanyID = @CompanyID OR @CompanyID is null)
--and INV_Item.CompanyID = INV_SET_Category.CompanyID
and INV_SET_Category.IsActive = 1
and INV_SET_Category.CategoryID = @CategoryID
";

            DataTable dt = null;
           
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                //if (isActive.HasValue)
                //    sqlparams[1] = new SqlParameter("@IsActive", isActive.Value);
                //else
                //    sqlparams[1] = new SqlParameter("@IsActive", DBNull.Value);

                if (selected_Category.HasValue)
                
                sqlparams[1] = new SqlParameter("@CategoryID",selected_Category.Value);
                    else
                       sqlparams[1] = new SqlParameter("@CategoryID", DBNull.Value);
                
                

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_inv, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_Select_inv, sqlparams);

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


        #region Constant Queries

        const string Query_Select_CategoryOnSecurityBasis = @"
SELECT Distinct INV_SET_Category.CategoryID, INV_SET_Category.CategoryName FROM [INV_SET_Category] 
    Inner Join UsersInCategories ON UsersInCategories.CategoryID = [INV_SET_Category].CategoryID --AND UsersInCategories.CompanyID = [INV_SET_Category].CompanyID 
WHERE [INV_SET_Category].[Status] = 1 AND [INV_SET_Category].[IsActive] = 1 --AND [INV_SET_Category].[CompanyID] = @CompanyID 
AND UsersInCategories.UserID = @UserID 
ORDER BY INV_SET_Category.CategoryName ";

        const string Query_Select_CategoryNameMinusRecordInTable = @"
SELECT INV_SET_Category.CategoryID, INV_SET_Category.CategoryName 
FROM INV_SET_Category 
LEFT JOIN FA_AssetTypePolicy ON FA_AssetTypePolicy.ItemCategoryID = INV_SET_Category.CategoryID 
--AND FA_AssetTypePolicy.CompanyID = INV_SET_Category.CompanyID 
    AND FA_AssetTypePolicy.Status = 1 
WHERE FA_AssetTypePolicy.ItemCategoryID IS NULL 
--AND INV_SET_Category.CompanyID = @CompanyID AND 
INV_SET_Category.Status = 1 ";

        const string Query_Select_Category_For_SalesOrder = @"
Select Distinct CategoryName, CategoryID 
From INV_SET_Category 

  left JOIN INV_Item ON INV_Item.ItemTypeID = INV_SET_Category.CategoryID 
and INV_Item.Status = 1 

        --AND INV_Item.InvoiceableItemFlag = 1 
where 
--INV_SET_Category.CompanyID = @CompanyID AND 
INV_SET_Category.IsActive = 1 and INV_SET_Category.Status = 1 ";


  

        const string Query_Get_Category = @"
SELECT INV_SET_Category.CategoryID, INV_SET_Category.CategoryName FROM INV_SET_Category 
where INV_SET_Category.CategoryID = @CategoryID --and INV_SET_Category.CompanyID = @CompanyID ";

        const string Query_Get_CategoryID_Mud = @"
SELECT INV_SET_Category.CategoryID, INV_SET_Category.CategoryName FROM INV_SET_Category 
where INV_SET_Category.CategoryName like 'Mud' --and INV_SET_Category.CompanyID = @CompanyID ";

        const string Query_GetAllCategoryByCompanyID = @"
select CategoryID, CategoryName from INV_SET_Category where [Status] = 1 and [IsActive] =  1 and CompanyID = @CompanyID ";

        const string Query_GetAllByCategoryID = @"
Select * from INV_SET_Category where [Status] = 1 and [IsActive] =  1 and CompanyID = @CompanyID and CategoryID = @CategoryID ";

        const string Query_SelectAllForStock = @" 
SELECT * FROM INV_SET_Category 
WHERE CompanyID = @CompanyID AND Status = 1 AND (IsActive = @IsActive OR @IsActive is null) 
    AND (IsInvisibleUponStockReports IS NULL OR IsInvisibleUponStockReports = 0) ";

        const string Query_UpdateDataByKeyForIsNotItemRateListBased = @" Update [INV_SET_Category] Set [IsNotItemRateListBased] = @IsNotItemRateListBased, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [CategoryID] = @CategoryID --and [CompanyID] = @CompanyID ";

        const string Query_Get_MaxIdwithoutCompanyId = @" SELECT IsNull(max(CategoryID), 0) + 1 as CategoryID FROM INV_SET_Category";

        const string Query_Get_CategoryID = @" 
Select Distinct CategoryName, CategoryID 
From INV_SET_Category 

  left JOIN INV_Item ON INV_Item.ItemTypeID = INV_SET_Category.CategoryID 
and INV_Item.Status = 1 

        --AND INV_Item.InvoiceableItemFlag = 1 
where 
--INV_SET_Category.CompanyID = @CompanyID AND 
INV_SET_Category.IsActive = 1 and INV_SET_Category.Status = 1 ";

        #endregion

        #region Class Methods

        public static bool UpdateDataByKeyForIsNotItemRateListBased(SqlTransaction objTransaction, bool IsNotItemRateListBased, Guid updatedBy, DateTime updateDate, string updatedByValue, int categoryID, int companyID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CategoryID", categoryID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@IsNotItemRateListBased", IsNotItemRateListBased);
                sqlparams[3] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[4] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[5] = new SqlParameter("@UpdatedByValue", updatedByValue);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateDataByKeyForIsNotItemRateListBased, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_UpdateDataByKeyForIsNotItemRateListBased, sqlparams) > 0)
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
        
        public static DataTable GET_Select_CategoryOnSecurityBasis(int companyID, int periodID, Guid userid, SqlTransaction sqlTransaction = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@UserID", userid);

                DataSet ds = null;
                if (sqlTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_CategoryOnSecurityBasis, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(sqlTransaction, CommandType.Text, Query_Select_CategoryOnSecurityBasis , sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }

            return dtToReturn;
        }

        public static DataTable Get_Select_Category_For_SalesOrder(string where ="")
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[0];

                //sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_Category_For_SalesOrder + where, sqlparams);

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


        public static DataTable getCategoryForLookup(int companyID, string categoryIds, string where = "")
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (categoryIds != null)
                {
                    where = "And INV_SET_Category.CategoryID IN (" + categoryIds + ")";
                }
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_CategoryID + where, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex) { SqlHelper.LogException(ex); }

            return dtToReturn;
        }

        public static DataTable GetCategoryNameMinusRecordInTable(int companyID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_CategoryNameMinusRecordInTable, sqlparams);

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

        public static DataTable GetCategoryNameByCategoryID(int companyID, int categoryID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@CategoryID", categoryID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_Category, sqlparams);

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

        public static DataTable GetCategoryNameByCompanyID(int companyID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllCategoryByCompanyID, sqlparams);

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

        public static DataTable GetAllActiveDataByCategoryID(int companyID, int categoryID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@CategoryID", categoryID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllByCategoryID, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllActiveDataForStock(int companyID, Nullable<bool> isActive = true, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (isActive.HasValue)
                    sqlparams[1] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[1] = new SqlParameter("@IsActive", DBNull.Value);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllForStock, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAllForStock, sqlparams);

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

        public static DataTable GetAllActiveData(Nullable<int> companyID,string where, Nullable<bool> isActive = true, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (isActive.HasValue)
                    sqlparams[1] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[1] = new SqlParameter("@IsActive", DBNull.Value);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll +where, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAll + where, sqlparams);

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

        public static DataTable GetCategory(int itemID, int companyID, SqlConnection obj_SQL_Connection = null)
        {
            DataSet ds = null;
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@ItemID", itemID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);

                string Query_GetCategory = @" select * From INV_SET_Category 
    inner join INV_Item on INV_Item.ItemTypeID = INV_SET_Category.CategoryID 
--AND INV_Item.CompanyID = INV_SET_Category.CompanyID 
where INV_Item.ItemID = @ItemID AND 
--INV_SET_CATEGory.CompanyID = @CompanyID AND 
INV_SET_CATEGory.Status = 1 ";

                if(obj_SQL_Connection != null)
                    ds = SqlHelper.ExecuteDataset(obj_SQL_Connection, CommandType.Text, Query_GetCategory, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetCategory, sqlparams);

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

        public static bool ValidateUniqueItemCategory(string CategoryName, SqlConnection objConnection = null)
        {
            #region Query

            const string QueryValidateUniqueItemCategory = @"
                 select CategoryName from 
                 INV_SET_Category where status=1
                and CategoryName = @CategoryName 
";

            #endregion

            bool IsValid = false;


            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CategoryName", CategoryName);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, QueryValidateUniqueItemCategory, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, QueryValidateUniqueItemCategory, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    IsValid = false;
                else
                    IsValid = true;



            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return IsValid;
        }



        public static int GetCategoryID(SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;

            try
            {
                //SqlParameter[] sqlparams = new SqlParameter[1];
                //sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_MaxIdwithoutCompanyId));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_Get_MaxIdwithoutCompanyId));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_MaxIdwithoutCompanyId));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public INV_SET_Category(int categoryID)
        {
            Load(categoryID);
        }

        public static DataRow GetDataByCategoryID(int categoryID,SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CategoryID", categoryID);
                //sqlparams[1] = new SqlParameter("@CompanyID", companyID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byKey, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byKey, sqlparams);
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

        private void Load(int categoryID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow drRecord = GetDataByCategoryID(categoryID, objConnection, objTransaction);

            if (drRecord != null)
            {
                this.f_IsDataLoaded = true;
                this.f_CompanyID = Convert.ToInt32(drRecord["CompanyID"]);
                this.f_CategoryID = Convert.ToInt32(drRecord["CategoryID"]);

                if (drRecord["CategoryName"] != DBNull.Value)
                    this.f_CategoryName = Convert.ToString(drRecord["CategoryName"]);

                this.f_Status = Convert.ToBoolean(drRecord["Status"]);
                this.f_CreatedBy = Guid.Parse(drRecord["CreatedBy"].ToString());
                this.f_CreateDate = Convert.ToDateTime(drRecord["CreateDate"]);

                if (drRecord["UpdatedBy"] != DBNull.Value)
                    this.f_UpdatedBy = Guid.Parse(drRecord["UpdatedBy"].ToString());


                if (drRecord["UpdateDate"] != DBNull.Value)
                    this.f_UpdateDate = Convert.ToDateTime(drRecord["UpdateDate"]);

                this.f_CreatedByValue = Convert.ToString(drRecord["CreatedByValue"]);

                if (drRecord["UpdatedByValue"] != DBNull.Value)
                    this.f_UpdatedByValue = Convert.ToString(drRecord["UpdatedByValue"]);

                this.f_IsActive = Convert.ToBoolean(drRecord["IsActive"]);

                if (drRecord["IsInvisibleUponStockReports"] != DBNull.Value)
                    this.f_IsInvisibleUponStockReports = Convert.ToBoolean(drRecord["IsInvisibleUponStockReports"]);


                if (drRecord["ImageFile"] != DBNull.Value)
                    this.f_ImageFile = (byte[])(drRecord["ImageFile"]);


                if (drRecord["UrduCatName"] != DBNull.Value)
                    this.f_UrduCatName = Convert.ToString(drRecord["UrduCatName"]);


                if (drRecord["IsFinishedGood"] != DBNull.Value)
                    this.f_IsFinishedGood = Convert.ToBoolean(drRecord["IsFinishedGood"]);


                if (drRecord["IsSemiFinished"] != DBNull.Value)
                    this.f_IsSemiFinished = Convert.ToBoolean(drRecord["IsSemiFinished"]);


                if (drRecord["IsOther"] != DBNull.Value)
                    this.f_IsOther = Convert.ToBoolean(drRecord["IsOther"]);

                if (drRecord["SeperateInvoices"] != DBNull.Value)
                    this.f_SeperateInvoices = Convert.ToBoolean(drRecord["SeperateInvoices"]);

            }
        }

        public static DataTable GetCategoryIdForMud(int companyID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_CategoryID_Mud, sqlparams);

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
