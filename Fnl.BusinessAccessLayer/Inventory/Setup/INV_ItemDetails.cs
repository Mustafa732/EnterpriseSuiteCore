using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;

using FnI.BusinessAccessLayer.Setup;
using System.Text;

namespace FnI.BusinessAccessLayer.Inventory.Setup
{
    public partial class INV_ItemDetails
    {

        const string Query_GetData_byKey_Master = @" SELECT * FROM [INV_ItemDetails] inner join INV_Item on INV_Item.ItemID = INV_ItemDetails.itemid WHERE INV_ItemDetails.CompanyID = @CompanyID and INV_ItemDetails.ItemID = @ItemID ";

        const string Query_itemDetail_By_Category = @" select  INV_SET_Category.CategoryName, INV_Item.Description, INV_SET_Category.CategoryID, INV_Item.ItemID, INV_Item.InventoryItemStatusCode
,ISNULL(AR_SET_Customer.CustomerName,'---') as CustomerName
 ,(CASE WHEN INV_ItemDetails.Embellishment='Select' then '---' ELSE INV_ItemDetails.Embellishment END) as Embellishment
  ,(CASE WHEN INV_ItemDetails.PackingStyle='Select' then '---' ELSE INV_ItemDetails.PackingStyle END ) as PackingStyle
 ,INV_Item.UnitWeight
 ,ISNULL(convert(varchar(10),CAST(INV_Item.Length AS INT)) +' x '+convert(varchar(10),CAST(INV_Item.Width AS INT)),'---') as Area
 ,(CASE WHEN INV_ItemDetails.GoodsType='Select' then '---' ELSE INV_ItemDetails.GoodsType END ) as GoodsType, isnull(INV_ItemDetails.PiecesinCarton,0) as PiecesInPackage,
INV_ItemDetails.OSCode
 
 from INV_Item 
left join INV_ItemDetails on INV_ItemDetails.ItemID = INV_Item.ItemID
 left JOIN INV_SET_Category ON INV_Item.ItemTypeID = INV_SET_Category.CategoryID and INV_Item.CompanyID = INV_SET_Category.CompanyID 
   left JOIN SET_SourceType ON SET_SourceType.SourceTypeID=INV_Item.SourceTypeID
LEFT JOIN AR_SET_Customer oN AR_SET_Customer.CustomerID=INV_Item.DefaultSupplierID
where INV_Item.Status = 1
--and INV_ItemDetails.Status = 1
 AND INV_SET_Category.Status = 1 AND INV_Item.Status = 1
AND (INV_SET_Category.CategoryID = @CategoryID or @CategoryID ='')  
and (INV_ItemDetails.ProductType = @productType or @productType ='')
and (INV_Item.ItemCatalogGroupID = @CatalogID or @CatalogID ='')  ";



        public static DataTable GetAllFinishedProduct(Nullable<int> companyID, string where = "")
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();



            const string Query_GetAllFinishedProduct = @" Select Description,INV_Item.ItemID from INV_Item 
inner join INV_ItemDetails on INV_ItemDetails.ItemID = INV_Item.ItemID

where INV_Item.Status = 1
and INV_ItemDetails.Status = 1
AND INV_ItemDetails.Producttype=2
--and INV_Item.CompanyID = @CompanyID  ";



            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllFinishedProduct + where, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllSemiFinishedProduct(Nullable<int> companyID, string where = "")
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();



            const string Query_GetAllSemiFinishedProduct = @" select distinct * from ( 
Select  Description,INV_Item.ItemID from INV_Item 
--inner join INV_ItemDetails on INV_ItemDetails.ItemID = INV_Item.ItemID

where INV_Item.Status = 1
--and INV_ItemDetails.Status = 1
--AND INV_ItemDetails.Producttype IN (1,3)
and INV_Item.CompanyID = 1 
{0}

union all
Select Description,INV_Item.ItemID from INV_Item 
--inner join INV_ItemDetails on INV_ItemDetails.ItemID = INV_Item.ItemID

where INV_Item.Status = 1
--and INV_ItemDetails.Status = 1

--and INV_Item.ProductType =3
)fintab 
  ";
            /*@" Select Description,INV_Item.ItemID from INV_Item 
inner join INV_ItemDetails on INV_ItemDetails.ItemID = INV_Item.ItemID

where INV_Item.Status = 1
and INV_ItemDetails.Status = 1
AND INV_ItemDetails.Producttype=1
--and INV_Item.CompanyID = @CompanyID  ";*/

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                if (!string.IsNullOrEmpty(where))
                {
                    sb.Append(where);
                }
                else
                    sb.Append("");
                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAllSemiFinishedProduct, sb.ToString()), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDateForAl_itemLookUp(Nullable<int> companyID, string Where = "", int? supplierID = null, string filter = "")
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            if (supplierID.HasValue)
                sb.Append(" AND  INV_Item.DefaultSupplierID in(" + supplierID + ")");

            const string Query_GetDateForLookUp = @" Select Description,INV_Item.ItemID,ISNULL(AR_SET_Customer.CustomerName,'---') as CustomerName, INV_Item.InventoryItemStatusCode
 ,(CASE WHEN INV_ItemDetails.Embellishment='Select' then '---' ELSE INV_ItemDetails.Embellishment END) as Embellishment
  ,(CASE WHEN INV_ItemDetails.PackingStyle='Select' then '---' ELSE INV_ItemDetails.PackingStyle END ) as PackingStyle
 ,INV_Item.UnitWeight
 ,convert(varchar(10),CAST(INV_Item.Length AS INT)) +' x '+convert(varchar(10),CAST(INV_Item.Width AS INT)) as Area
 
  from INV_Item 
left join INV_ItemDetails on INV_ItemDetails.ItemID = INV_Item.ItemID
LEFT JOIN AR_SET_Customer oN AR_SET_Customer.CustomerID=INV_Item.DefaultSupplierID
where INV_Item.Status = 1
--and INV_ItemDetails.Status = 1
--and INV_Item.CompanyID = @CompanyID
";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDateForLookUp + Where + sb.ToString() + filter, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        public static DataTable getAl_ImdadItem_By_Category(int companyID, string categoryID, string productType, string catalogid, string where = "")
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@categoryID", categoryID);
                sqlparams[2] = new SqlParameter("@productType", productType);
                sqlparams[3] = new SqlParameter("@CatalogID", catalogid);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_itemDetail_By_Category + where, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex) { SqlHelper.LogException(ex); }

            return dtToReturn;
        }

        public static DataRow GetDataByKeyWithMaster(int companyID, int itemID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@ItemID", itemID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byKey_Master, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byKey_Master, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byKey_Master, sqlparams);

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
