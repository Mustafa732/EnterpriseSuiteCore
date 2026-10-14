using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_ItemCatalog
    {
        #region Query
        const string Query_Select_Catalog = @"
Select Distinct SET_ItemCatalog.Description as CatalogDescription, ItemCatalogID 
From SET_ItemCatalog 

  left JOIN INV_Item ON INV_Item.ItemCatalogGroupID = SET_ItemCatalog.ItemCatalogID
and INV_Item.Status = 1 

        --AND INV_Item.InvoiceableItemFlag = 1 
where 
--INV_SET_Category.CompanyID = @CompanyID AND 
SET_ItemCatalog.IsActive = 1 and SET_ItemCatalog.Status = 1";
        #endregion

        public static DataTable GetAllActiveData(int CompanyID, string where = "", Nullable<bool> isActive = true)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                    sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);

                    if (isActive.HasValue)
                        sqlparams[1] = new SqlParameter("@IsActive", isActive.Value);
                    else
                        sqlparams[1] = new SqlParameter("@IsActive", DBNull.Value);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll+where, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable Get_Select_Catalog(string where = "")
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[0];

                //sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_Catalog + where, sqlparams);

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

        public static DataTable GetAllActiveDataForCustomerCategory(Nullable<bool> isActive = true)
        {
            DataTable dt = null;
            const string Query_SelectAllForCustoemrCategory = @"
SELECT '-1' AS ItemCatalogID, 'Select' AS CatalogDescription ,'1' As Status ,'1' As IsActive
UNION ALL 
Select SET_ItemCatalog.ItemCatalogID, SET_ItemCatalog.Description as CatalogDescription, SET_ItemCatalog.Status, SET_ItemCatalog.IsActive from SET_ItemCatalog
where Status=1 AND (SET_ItemCatalog.IsActive = 1 or 1 is null) --and SET_ItemCatalog.Description like '%Finish%'";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                if (isActive.HasValue)
                    sqlparams[0] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[0] = new SqlParameter("@IsActive", DBNull.Value);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllForCustoemrCategory , sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static bool ValidateUniqueItemCatalog(string Description, SqlConnection objConnection = null)
        {
            #region Query

            const string QueryValidateUniqueItemCatalog = @"
                 select Description from 
                 SET_ItemCatalog where status=1
                and Description = @Description 
";

            #endregion

            bool IsValid = false;


            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@Description", Description);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, QueryValidateUniqueItemCatalog, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, QueryValidateUniqueItemCatalog, sqlparams);

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

        public static bool IsItemCatalogUsed(int ItemCatalogID, SqlConnection objConnection = null)
        {
            #region Query

            const string QueryIsItemCatalogUsed = @"
SELECT SET_ItemCatalog.Description,SET_ItemCatalog.ItemCatalogID,INV_Item.ItemCatalogGroupID
from SET_ItemCatalog
inner join INV_Item on INV_Item.ItemCatalogGroupID = SET_ItemCatalog.ItemCatalogID
where SET_ItemCatalog.Status = 1 and
SET_ItemCatalog.ItemcatalogID = @ItemCatalogID
";

            #endregion

            bool IsUsed = true;


            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@ItemCatalogID", ItemCatalogID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, QueryIsItemCatalogUsed, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, QueryIsItemCatalogUsed, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    IsUsed = true;
                else
                    IsUsed = false;



            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return IsUsed;
        }
        
        public static DataTable GetOnlyCatalog(string Description , SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
           const string  Query_GetOnlyCatalog = @"Select * from SET_ItemCatalog Where Description=@Description AND [Status] = 1";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                if (Description.ToString()!=string.Empty)
                    sqlparams[0] = new SqlParameter("@Description", Description);
                else
                    sqlparams[0] = new SqlParameter("@Description", DBNull.Value);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetOnlyCatalog, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetOnlyCatalog, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetOnlyCatalog, sqlparams);

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


        public static object GetAllActiveDataByItemCatalogID(int ItemCatalogID)
        {
            DataTable dt = null;
            #region Query

            const string Query_GetAllByItemCatalogID = @"
Select * from SET_ItemCatalog   where [Status] = 1 and [IsActive] =  1 and  ItemCatalogID = @ItemCatalogID
";

            #endregion
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@ItemCatalogID", ItemCatalogID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllByItemCatalogID, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
    }
}
