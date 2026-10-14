using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Sql;
using Microsoft.Data.SqlClient;
using System.Data;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Inventory.Setup
{
    public partial class INV_Item_Brands
    {
        #region Constant Strings

        const string Query_SelectAllBrands = @" SELECT * FROM [SET_Item_Brands] WHERE [Status] = 1 ";
        const string Query_SelectAllBrandsDetail = @" select item.ItemID,item.BrandID,item.Description,item.InventoryItemStatusCode,SET_Item_Brands.BrandName from INV_Item item
left join INV_Item_Brands on item.ItemID = INV_Item_Brands.ItemID
left join SET_Item_Brands on SET_Item_Brands.BrandID = item.BrandID 
where item.Status=1 ";
        
        #endregion

        #region Class Methods

        public static DataTable GetAllActiveDataBrand(SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAll);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAll);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllBrands);

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

        public static DataTable GetAllActiveDataBrandDetail(int CompanyID, string Filter, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
               
                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAllBrandsDetail + Filter, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAllBrandsDetail + Filter, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllBrandsDetail + Filter, sqlparams);

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
