using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;

using FnI.BusinessAccessLayer.Setup;

namespace FnI.BusinessAccessLayer.Inventory.Setup
{
  public partial class INV_ItemsPriceHistory
    {

      #region Constant Strings

      const string Query_GetDataByIDAndCompanyID = @" SELECT 
     ROW_NUMBER() OVER(order by ItemPriceID) AS SNo
	,[INV_ItemsPriceHistory].[CompanyID]
    ,[INV_ItemsPriceHistory].[ItemPriceID]
    ,[INV_ItemsPriceHistory].[ItemID]
    ,[INV_Item].[Description]
    ,[INV_Item].[ListPricePerUnit]
    ,[INV_ItemsPriceHistory].[CategoryID]
    ,[INV_ItemsPriceHistory].[ListPrice]
    ,[INV_ItemsPriceHistory].[EffectiveDate]
    ,[INV_ItemsPriceHistory].[CreatedBy]
    ,[INV_ItemsPriceHistory].[CreateDate]
    ,[INV_ItemsPriceHistory].[CreatedByValue]
    ,[INV_ItemsPriceHistory].[UpdatedBy]
    ,[INV_ItemsPriceHistory].[UpdateDate]
    ,[INV_ItemsPriceHistory].[UpdatedByValue]
    ,[INV_ItemsPriceHistory].[ApprovedBy]
    ,[INV_ItemsPriceHistory].[ApprovedDate]
    ,[INV_ItemsPriceHistory].[ApprovedByValue]

FROM [INV_ItemsPriceHistory] 

INNER JOIN [INV_Item] ON
[INV_ItemsPriceHistory].[ItemID] = [INV_Item].[ItemID]

WHERE 

([INV_ItemsPriceHistory].[CompanyID] = @CompanyID OR @CompanyID is null) AND ([INV_ItemsPriceHistory].[CategoryID] = @CategoryID OR @CategoryID is null) 
";


      const string Query_SelectAllByCategoryIDAndCompanyID = @"SELECT 	
     ROW_NUMBER() OVER(order by ItemID) AS SNo
    ,0 as ItemPriceID
	,[INV_Item].[ItemID]
	,[INV_Item].[Description]
    ,[INV_Item].[ListPricePerUnit]
	,[INV_Item].[InventoryItemStatusCode]
    ,[SET_UnitOfMeasure].[UOMName]
   , INV_Item.ItemTypeID as  CategoryID
	,cat.[CategoryName]
    ,SET_SourceType.Description as SourceType
    ,INV_Item.ItemCatalogGroupID as  ItemTypeID
    FROM [INV_Item] 

    INNER JOIN [SET_ItemCatalog] ON
    [INV_Item].ItemCatalogGroupID = [SET_ItemCatalog].[ItemCatalogID]
    Inner join INV_SET_Category cat on cat.CategoryID = INV_Item.ItemTypeID
      inner join SET_UnitOfMeasure on INV_Item.PrimaryUOMCode = SET_UnitOfMeasure.UOMID
  INNER JOIN SET_SourceType on SET_SourceType.SourceTypeID = INV_Item.SourceTypeID
WHERE 

    --([INV_Item].[CompanyID] =  @CompanyID OR  @CompanyID is null) AND 
[INV_Item].[Status] = 1 AND (INV_Item.ItemTypeID =  @CategoryID OR  @CategoryID is null)
AND (INV_Item.ItemCatalogGroupID =  @ItemCatalogID OR  @ItemCatalogID is null) ";


      const string Query_SelectAllByCategoryIDAndCompanyIDAndDateTime = @"SELECT 	
        Distinct
       [INV_ItemsPriceHistory].[ItemPriceID]
     ,INV_ItemsPriceHistory.DocumentTypeID
     ,INV_ItemsPriceHistory.DocumentNo
	,[INV_Item].[ItemID]
	,[INV_Item].[Description]
    ,[INV_Item].[ListPricePerUnit]
    ,[INV_ItemsPriceHistory].[ListPrice]
    ,cat.[CategoryID]
    ,[INV_ItemsPriceHistory].[EffectiveDate]
	,[INV_ItemsPriceHistory].[StatusCode]
     ,INV_ItemsPriceHistory.ItemTypeID
     ,[INV_Item].[InventoryItemStatusCode]
    ,[SET_UnitOfMeasure].[UOMName]
     ,cat.[CategoryName]
    ,SET_SourceType.Description as SourceType
 ,row_number() OVER(order by  [INV_ItemsPriceHistory].[ItemPriceID]) AS SNo 
    FROM [INV_ItemsPriceHistory] 

   
    INNER JOIN [INV_Item] ON
    [INV_Item].[ItemID] = [INV_ItemsPriceHistory].[ItemID]
    Left join INV_SET_Category cat on cat.CategoryID = [INV_ItemsPriceHistory].CategoryID
 Left JOIN [SET_ItemCatalog] ON
    [INV_Item].[ItemCatalogGroupID] = INV_ItemsPriceHistory.ItemTypeID
 Left join SET_UnitOfMeasure on INV_Item.PrimaryUOMCode = SET_UnitOfMeasure.UOMID
Left JOIN SET_SourceType on SET_SourceType.SourceTypeID = INV_Item.SourceTypeID
    WHERE 
     INV_ItemsPriceHistory.DocumentNo=@DocumentNo
   AND ([INV_ItemsPriceHistory].[CompanyID] =  @CompanyID OR  @CompanyID is null) AND [INV_Item].[Status] = 1 AND ([INV_ItemsPriceHistory].[CategoryID] =  @CategoryID OR  @CategoryID is null) AND CAST(CONVERT(VARCHAR(10), [INV_ItemsPriceHistory].[EffectiveDate], 110) AS DATETIME) = @EffectDate
 
GROUP by  [INV_ItemsPriceHistory].[ItemPriceID]
     ,INV_ItemsPriceHistory.DocumentTypeID
     ,INV_ItemsPriceHistory.DocumentNo
    ,[INV_Item].[ItemID]
	,[INV_Item].[Description]
    ,[INV_Item].[ListPricePerUnit]
    ,[INV_ItemsPriceHistory].[ListPrice]
    ,cat.[CategoryID]
    ,[INV_ItemsPriceHistory].[EffectiveDate]
	,[INV_ItemsPriceHistory].[StatusCode]
     ,INV_ItemsPriceHistory.ItemTypeID
     ,[INV_Item].[InventoryItemStatusCode]
    ,[SET_UnitOfMeasure].[UOMName]
     ,cat.[CategoryName]
     ,INV_ItemsPriceHistory.[CategoryID]
    ,SET_SourceType.Description

";



      const string Query_SelectAllDistinct_WithPaging = @"SELECT 
   INV_ItemsPriceHistory.[CompanyID],  
   INV_ItemsPriceHistory.[CategoryID],
   INV_ItemsPriceHistory.[CategoryName],
   INV_ItemsPriceHistory.[StatusCode],
   INV_ItemsPriceHistory.[EffectiveDate],
   INV_ItemsPriceHistory.DocumentTypeID
   ,INV_ItemsPriceHistory.DocumentNo
   , ROW_NUMBER() Over (Order by INV_ItemsPriceHistory.[CategoryID], INV_ItemsPriceHistory.[CategoryID]) as 'Row' 
   
   from
   (select distinct
   [INV_ItemsPriceHistory].[CompanyID],
   [INV_ItemsPriceHistory].[CategoryID],
   [INV_SET_Category].[CategoryName],
   [INV_ItemsPriceHistory].[StatusCode],
   [INV_ItemsPriceHistory].[EffectiveDate]
   ,INV_ItemsPriceHistory.DocumentTypeID
   ,INV_ItemsPriceHistory.DocumentNo
   from [INV_ItemsPriceHistory] 
    INNER JOIN [INV_SET_Category] ON
   [INV_ItemsPriceHistory].[CategoryID] = [INV_SET_Category].[CategoryID]
   
   ) INV_ItemsPriceHistory";


      const string Query_SelectAllDistinctForApproval_WithPaging = @"SELECT 
   INV_ItemsPriceHistory.[CompanyID],  
   INV_ItemsPriceHistory.[CategoryID],
   INV_ItemsPriceHistory.[CategoryName],
   INV_ItemsPriceHistory.[EffectiveDate],
    INV_ItemsPriceHistory.DocumentTypeID
   ,INV_ItemsPriceHistory.DocumentNo
   ROW_NUMBER() Over (Order by INV_ItemsPriceHistory.[CategoryID], INV_ItemsPriceHistory.[CategoryID]) as 'Row' 
   
   from
   (select distinct
   [INV_ItemsPriceHistory].[CompanyID],
   [INV_ItemsPriceHistory].[CategoryID],
   [INV_SET_Category].[CategoryName],
   [INV_ItemsPriceHistory].[EffectiveDate],
  INV_ItemsPriceHistory.DocumentTypeID
   ,INV_ItemsPriceHistory.DocumentNo
   from [INV_ItemsPriceHistory] 
    INNER JOIN [INV_SET_Category] ON
   [INV_ItemsPriceHistory].[CategoryID] = [INV_SET_Category].[CategoryID]
   
   ) INV_ItemsPriceHistory";

      const string Query_GetAllDataItemType = @"
SELECT 	
     ROW_NUMBER() OVER(order by ItemID) AS SNo
    ,0 as ItemPriceID
	,[INV_Item].[ItemID]
	,[INV_Item].[Description]
    ,[INV_Item].[ListPricePerUnit]
	,[INV_Item].[InventoryItemStatusCode]
    ,[SET_UnitOfMeasure].[UOMName]
    ,cat.[CategoryID]
	,cat.[CategoryName]
    ,SET_SourceType.SourceTypeID
	,SET_SourceType.Description as SourceType
    ,INV_Item.ItemCatalogGroupID as ItemTypeID

    FROM [INV_Item] 

    Inner join INV_SET_Category cat on cat.CategoryID = INV_Item.ItemTypeID
      inner join SET_UnitOfMeasure on INV_Item.PrimaryUOMCode = SET_UnitOfMeasure.UOMID
      inner join SET_ItemCatalog on SET_ItemCatalog.ItemCatalogID = INV_Item.ItemCatalogGroupID
 INNER JOIN SET_SourceType on SET_SourceType.SourceTypeID = INV_Item.SourceTypeID
WHERE 

    --([INV_Item].[CompanyID] =  @CompanyID OR  @CompanyID is null) AND 
[INV_Item].[Status] = 1 AND (INV_Item.ItemCatalogGroupID =  @ItemCatalogID OR  @ItemCatalogID is null)
";


      const string Query_GridDataByDocumentTypeIDandDocumentNo = @"SELECT 	
 row_number() OVER(order by  [INV_ItemsPriceHistory].[ItemPriceID]) AS SerialNo 
       ,[INV_ItemsPriceHistory].[ItemPriceID]
	,[INV_Item].[ItemID]
	,[INV_Item].[Description]
    ,[INV_ItemsPriceHistory].UnitPrice
    ,[INV_ItemsPriceHistory].[ListPrice] as SellingPrice
    ,cat.[CategoryID]
	,[INV_ItemsPriceHistory].OverHead 
    ,[INV_ItemsPriceHistory].Margin
,SET_UnitOfMeasure.UOMID as ItemUnitID
    ,[SET_UnitOfMeasure].[UOMName]
     ,cat.[CategoryName]

 ,row_number() OVER(order by  [INV_ItemsPriceHistory].[ItemPriceID]) AS SNo 
 ,'' as Action
    FROM [INV_ItemsPriceHistory] 

   
    INNER JOIN [INV_Item] ON
    [INV_Item].[ItemID] = [INV_ItemsPriceHistory].[ItemID]
    Left join INV_SET_Category cat on cat.CategoryID = [INV_ItemsPriceHistory].CategoryID
 Left JOIN [SET_ItemCatalog] ON
    [INV_Item].[ItemCatalogGroupID] = INV_ItemsPriceHistory.ItemTypeID
 Left join SET_UnitOfMeasure on INV_Item.PrimaryUOMCode = SET_UnitOfMeasure.UOMID
Left JOIN SET_SourceType on SET_SourceType.SourceTypeID = INV_Item.SourceTypeID
    WHERE 
     INV_ItemsPriceHistory.DocumentNo=@DocumentNo
   AND ([INV_ItemsPriceHistory].[CompanyID] =  @CompanyID OR  @CompanyID is null) AND [INV_Item].[Status] = 1 
  AND INV_ItemsPriceHistory.DocumentTypeID=@DocumentTypeID";

      const string Query_SelectAllDistinct_Alimdad_WithPaging = @"
select distinct
   [INV_ItemsPriceHistory].[CompanyID],
   [SET_DocumentType].DocumentType,
   [INV_ItemsPriceHistory].[StatusCode],
   [INV_ItemsPriceHistory].[EffectiveDate]
   ,INV_ItemsPriceHistory.DocumentTypeID
   ,INV_ItemsPriceHistory.DocumentNo
    , DENSE_RANK()
	   Over ( order by INV_ItemsPriceHistory.DocumentTypeID, INV_ItemsPriceHistory.DocumentNo)
	   as 'Row'
   from [INV_ItemsPriceHistory] 
    INNER JOIN [SET_DocumentType]  ON
   [SET_DocumentType].DocumentTypeID = [INV_ItemsPriceHistory].DocumentTypeID";

      #endregion

      #region Class Methods

      public static DataTable GetAllDataByCategoryIDAndCompnyID(Nullable<int> categoryID, Nullable<int> ItemCatalogID, Nullable<int> companyID, SqlConnection objConnection = null)
		{
			DataTable dt = null;

			try
			{
				SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@CategoryID", categoryID);
				sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@ItemCatalogID", ItemCatalogID);

				DataSet ds = null;

				if(objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllByCategoryIDAndCompanyID, sqlparams);
				else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAllByCategoryIDAndCompanyID, sqlparams);

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



      public static DataTable GetAllDistinctData(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
      Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc)
      {
          List<SqlParameter> sqlparamsList = new List<SqlParameter>();
          OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
          DataTable dt = null;
          string GroupBy = "GROUP BY INV_ItemsPriceHistory.CompanyID,INV_ItemsPriceHistory.DocumentTypeID,INV_ItemsPriceHistory.DocumentNo,INV_ItemsPriceHistory.CategoryID,INV_ItemsPriceHistory.CategoryName,INV_ItemsPriceHistory.StatusCode,INV_ItemsPriceHistory.EffectiveDate";

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
                  dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAllDistinct_WithPaging + strWhere + GroupBy + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                  ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
              }
              else
                  dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllDistinct_WithPaging + strWhere + GroupBy + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
          }
          catch (Exception ex)
          {
              SqlHelper.LogException(ex);
              throw ex;
          }

          return dt;
      }

      public static DataTable GetAllDataByCategoryIDAndCompnyIDAndDateTime(int DocumentNo, Nullable<int> categoryID,Nullable<int> companyID,DateTime effectDate, SqlConnection objConnection = null)
		{
			DataTable dt = null;

			try
			{
				SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CategoryID", categoryID);
				sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@EffectDate", effectDate);
                sqlparams[3] = new SqlParameter("@DocumentNo", DocumentNo);
				DataSet ds = null;

				if(objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllByCategoryIDAndCompanyIDAndDateTime, sqlparams);
				else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAllByCategoryIDAndCompanyIDAndDateTime, sqlparams);

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


    
      	public static DataTable GetAllDataForApprove(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null, 
			Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc)
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAllDistinctForApproval_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
					ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
				}
				else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllDistinctForApproval_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
			}
			catch (Exception ex)
			{
				SqlHelper.LogException(ex);
				throw ex;
			}

			return dt;
		}
        public static DataTable GetAllDataByCatalogID(int itemCatalogID, int companyID, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@ItemCatalogID", itemCatalogID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllDataItemType, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetAllDataItemType, sqlparams);

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
        public bool UpdateDataItemPrice(int itemID, int companyID, double ItemPrice, SqlTransaction objTransaction = null)
        { 
             bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[17];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@ItemID", ItemID);
                sqlparams[2] = new SqlParameter("@PricePerUnit", ItemPrice);

                #region Query
                const string Query_Update_ItemPrice = @" UPDATE INV_Item SET ListPricePerUnit = @PricePerUnit
		WHERE [CompanyID] = @CompanyID and [ItemID] = @ItemID";
                #endregion

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_ItemPrice, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_ItemPrice, sqlparams) > 0)
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
        public static DataTable GetGridDataByDocumentTypeIDandDocumentNo( int companyID,int DocumenttypeID,int DocumentNo, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@companyID", companyID);
                sqlparams[1] = new SqlParameter("@DocumentTypeID", DocumenttypeID);
                sqlparams[2] = new SqlParameter("@DocumentNo", DocumentNo);
                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GridDataByDocumentTypeIDandDocumentNo, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GridDataByDocumentTypeIDandDocumentNo, sqlparams);

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
        public static DataTable GetAllDistinctDataAl_imdad(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
       Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc)
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAllDistinct_Alimdad_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAllDistinct_Alimdad_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllDistinct_Alimdad_WithPaging + strWhere  + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        public static DataTable GetDataByDocumentTypeIDandDocumentNo(int companyID, int DocumenttypeID, int DocumentNo, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {

                #region
                const string Query_DataByDocumentTypeIDandDocumentNo = @"
select distinct
   [INV_ItemsPriceHistory].[CompanyID],
   [SET_DocumentType].DocumentType,
   [INV_ItemsPriceHistory].[StatusCode],
   [INV_ItemsPriceHistory].[EffectiveDate]
   ,INV_ItemsPriceHistory.DocumentTypeID
   ,INV_ItemsPriceHistory.DocumentNo
   ,INV_ItemsPriceHistory.StatusCode
      , DENSE_RANK()
	   Over ( order by INV_ItemsPriceHistory.DocumentTypeID, INV_ItemsPriceHistory.DocumentNo)
	   as 'Row' 
   from [INV_ItemsPriceHistory] 
    INNER JOIN [SET_DocumentType]  ON
   [SET_DocumentType].DocumentTypeID = [INV_ItemsPriceHistory].DocumentTypeID

     WHERE    INV_ItemsPriceHistory.DocumentNo=@DocumentNo
   AND ([INV_ItemsPriceHistory].[CompanyID] =  @companyID OR  @companyID is null) 
  AND INV_ItemsPriceHistory.DocumentTypeID=@DocumentTypeID ";
                #endregion
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@companyID", companyID);
                sqlparams[1] = new SqlParameter("@DocumentTypeID", DocumenttypeID);
                sqlparams[2] = new SqlParameter("@DocumentNo", DocumentNo);
                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_DataByDocumentTypeIDandDocumentNo, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_DataByDocumentTypeIDandDocumentNo, sqlparams);

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


        public bool DeleteByKey(int CompanyId,int ItemPriceID, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyId);
                sqlparams[1] = new SqlParameter("@ItemPriceID", ItemPriceID);


                #region Query
                const string Query_DeleteByKey = @" Delete  from INV_ItemsPriceHistory  Where CompanyID=@CompanyID AND ItemPriceID=@ItemPriceID";
                #endregion

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteByKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DeleteByKey, sqlparams) > 0)
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
    }
}
