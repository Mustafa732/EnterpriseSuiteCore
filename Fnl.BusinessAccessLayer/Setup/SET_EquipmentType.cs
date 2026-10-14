using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;

using Common.Utils;
using DAL.DataAccessLayer;




namespace FnI.BusinessAccessLayer.Setup
{
	public partial class SET_EquipmentType
    {
        #region query

        const string query = @"select AssetTypeID,AssetTypeName from FA_SET_AssetType 
                                where Status = 1";


        const string Query_SelectEquipmentType = @"SELECT INV_ITEM.[CompanyID]
      ,INV_ITem.[ItemID]
       ,[EquipmentTypeID]
       ,FA_SET_AssetType.AssetTypeName
      ,[Description]
      ,INV_ITEM.[ChartofAccountID]
      ,[CodeCombinationID]
      ,[ItemCatalogGroupID]
      ,[AssetCategoryID]
      ,[InventoryItemStatusCode]
      ,[ItemTypeID]
      ,INV_ITEM.[Status]
      ,[ApprovalItemGroupID]
      ,[InventoryAccountCCID]
      
  FROM [INV_Item]
  
  left join FA_SET_AssetType on INV_ITEM.EquipmentTypeID = FA_SET_AssetType.AssetTypeID
 inner join INV_StockTransactionDetail on INV_ITem.ItemID = INV_StockTransactionDetail.ItemID
  
  where INV_StockTransactionDetail.TransactionDetailID =@TransactionID --and INV_ITEM.CompanyID = @CompanyID ";

        const string Query_SelectAllEquipmentType = @"SELECT INV_ITEM.[CompanyID]
      ,INV_ITem.[ItemID]
       ,[EquipmentTypeID]
       ,FA_SET_AssetType.AssetTypeName
      ,[Description]
      ,INV_ITEM.[ChartofAccountID]
      ,[CodeCombinationID]
      ,[ItemCatalogGroupID]
      ,[AssetCategoryID]
      ,[InventoryItemStatusCode]
      ,[ItemTypeID]
      ,INV_ITEM.[Status]
      ,[ApprovalItemGroupID]
      ,[InventoryAccountCCID]
      
  FROM [INV_Item]
  
  inner join FA_SET_AssetType on INV_ITEM.EquipmentTypeID = FA_SET_AssetType.AssetTypeID
--inner join INV_StockTransactionDetail on INV_ITem.ItemID = INV_StockTransactionDetail.ItemID
  
  where INV_ITem.ItemID=@ItemID --and INV_ITEM.CompanyID =@CompanyID";
  
  


                           


        #endregion


        public static DataTable GetAllData()
        {
            DataTable dt = null;

            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, query).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }




        public static DataTable GetAssetType(int companyID, int transid, SqlConnection objConnection = null)
            
        {
            DataTable dtToReturn = null;
            string Filter = "";
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                //sqlparams[0] = new SqlParameter("@DocumentNo", documentNo);
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@TransactionID", transid);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectEquipmentType), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_SelectEquipmentType), sqlparams);

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

        public static DataTable GetAssetTypeForItem(int companyID, int itemid, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;
            string Filter = "";
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                //sqlparams[0] = new SqlParameter("@DocumentNo", documentNo);
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@ItemID", itemid);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectAllEquipmentType), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_SelectAllEquipmentType), sqlparams);

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
