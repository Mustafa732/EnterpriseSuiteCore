using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.FA.Setup
{
    public partial class FA_AssetTypePolicy
    {
        #region Constant Queries

        const string Query_From_OtherTable = @" 
SELECT AssetTypePolicyID, FA_AssetTypePolicy.AssetHeadTypeID, FA_AssetTypePolicy.DepreciationMethodCode, PeriodStatus, AssetLifeID, DepreciationRate, IsDefault, 
    AssetClassification, FA_AssetTypePolicy.IsActive, FA_AssetTypePolicy.Status, FA_AssetTypePolicy.CreatedBy, FA_AssetTypePolicy.CreateDate, 
    FA_AssetTypePolicy.CreatedByValue, FA_AssetTypePolicy.UpdatedBy, FA_AssetTypePolicy.UpdateDate, FA_AssetTypePolicy.UpdatedByValue, 
    FA_Set_AssetHeadType.AssetHeadTypeName, 
    case FA_AssetTypePolicy.DepreciationMethodCode when 1 then 'Declining_Balance' when 2 then 'Straight_Line' end as DepreciationMethodName, 
    INV_SET_Category.CategoryName, ROW_NUMBER() Over (Order by AssetTypePolicyID) as 'Row' 
FROM FA_AssetTypePolicy 
   inner join FA_Set_AssetHeadType on FA_Set_AssetHeadType.AssetHeadTypeID = FA_AssetTypePolicy.AssetHeadTypeID 
        and FA_Set_AssetHeadType.CompanyID = FA_AssetTypePolicy.CompanyID and FA_Set_AssetHeadType.Status = 1 
    inner join INV_SET_Category on INV_SET_Category.CategoryID = FA_Set_AssetHeadType.CategoryID 
--and INV_SET_Category.CompanyID = FA_Set_AssetHeadType.CompanyID 
        and INV_SET_Category.Status = 1 ";

        const string Query_AssetTypePolicyID = @" select FA_AssetTypePolicy.AssetTypePolicyID From FA_AssetIdentity 
	INNER JOIN INV_Item ON FA_AssetIdentity.ItemID = INV_Item.ItemID  --FA_AssetIdentity.CompanyID = INV_Item.CompanyID AND 
    INNER JOIN INV_SET_Category ON 
--INV_Item.CompanyID = INV_SET_Category.CompanyID AND 
INV_Item.ItemTypeID = INV_SET_Category.CategoryID 
    INNER JOIN FA_AssetTypePolicy ON 
--INV_SET_Category.CompanyID = FA_AssetTypePolicy.CompanyID AND 
INV_SET_Category.CategoryID = FA_AssetTypePolicy.ItemCategoryID 
where FA_AssetIdentity.Status = 1 AND  FA_AssetTypePolicy.IsAssetClassification = @IsAssetClassification AND FA_AssetTypePolicy.CompanyID = @CompanyID ";

        const string Query_SelectAll_AssestPolicy = @"
SELECT FA_AssetTypePolicy.*, FA_SET_AssetType.AssetTypeName, ROW_NUMBER() Over (Order by FA_SET_AssetType.AssetTypeName) as 'Row' 
FROM FA_AssetTypePolicy 
    INNER JOIN FA_SET_AssetType on FA_SET_AssetType.AssetTypeID = FA_AssetTypePolicy.AssetTypeID ";

        const string Query_AssestPolicy_By_AssetType = @" SELECT FA_AssetTypePolicy.*, FA_SET_AssetType.AssetTypeName,FA_SET_AssetType.Prefix FROM FA_AssetTypePolicy 
    INNER JOIN FA_SET_AssetType on FA_SET_AssetType.AssetTypeID = FA_AssetTypePolicy.AssetTypeID 
Where FA_AssetTypePolicy.CompanyID = @CompanyID And FA_AssetTypePolicy.AssetTypeID = @AssetTypeID AND FA_AssetTypePolicy.Status = 1 ";

        #endregion

        #region Class Methods

        public static DataTable GetAllData_from_othertable(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null, 
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_From_OtherTable + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_From_OtherTable + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_From_OtherTable + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static int GetAssetTypePolicyID(int companyID, int isAssetClassification, SqlTransaction objTransaction = null)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@IsAssetClassification", isAssetClassification);

                if (objTransaction == null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_AssetTypePolicyID, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_AssetTypePolicyID, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static DataTable GetAllDataAssestPolicy(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, SqlTransaction objTransaction = null)
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

                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_SelectAll_AssestPolicy + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_AssestPolicy + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_AssestPolicy + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_AssestPolicy + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAll_AssestPolicy + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_AssestPolicy + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAssetTypePolicy(int companyID, int assetTypeID, SqlConnection obj_SQL_Connection)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@AssetTypeID", assetTypeID);

                dtToReturn = SqlHelper.ExecuteDataset(obj_SQL_Connection, CommandType.Text, Query_AssestPolicy_By_AssetType, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static int GetMaxAssetTagNumber(int companyID, int assetTypeID, int itemid)
        {
            DataTable dtToReturn = null;

            int maxno = 0;

            const string query_max_assettagnumber = @"
                            select max(tagnumber)+1 TagNumber from fa_assetinformation where ItemID = @ItemID and AssetTypeID = @AssetTypeID and companyid = @CompanyID and FA_AssetInformation.Status = 1
                ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@AssetTypeID", assetTypeID);
                sqlparams[2] = new SqlParameter("@ItemID", itemid);

                dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text, query_max_assettagnumber, sqlparams).Tables[0];


                if (dtToReturn.Rows[0]["TagNumber"].ToString() != "")
                {
                    maxno = int.Parse(dtToReturn.Rows[0]["TagNumber"].ToString());
                }
                else
                {
                    maxno = 1;
                }

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return maxno;
        }

        #endregion
    }
}
