using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.FA.Transaction
{
    public partial class FA_AssetCostHeader
    {
        #region Constant Queries

        const string Query_SelectAll_Searching = @" SELECT DISTINCT FA_AssetCostHeader.CompanyID
      ,FA_AssetCostHeader.PeriodID
      ,FA_AssetCostHeader.AssetCostID
      ,FA_AssetCostHeader.AssetTransactionID
      ,FA_AssetCostHeader.CostingDate
      ,FA_AssetCostHeader.DocumentReference
      ,FA_AssetCostHeader.VendorID
      ,FA_AssetCostHeader.CostofAsset
      ,FA_AssetCostHeader.Comments
      ,FA_AssetCostHeader.Status
      ,AR_SET_Customer.CustomerName 
  FROM FA_AssetCostHeader
  INNER JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = FA_AssetCostHeader.VendorID AND 
	  AR_SET_Customer.CompanyID = FA_AssetCostHeader.CompanyID AND AR_SET_Customer.PeriodID = FA_AssetCostHeader.PeriodID
	  AND AR_SET_Customer.Status = 1 ";

        const string Query_SelectAll_Searching_WithPaging = @" SELECT *, ROW_NUMBER() OVER (ORDER BY FA_AssetCostHeader.AssetCostID) AS 'Row'  FROM (SELECT DISTINCT tbl_ACH.CompanyID
      ,tbl_ACH.PeriodID
      ,tbl_ACH.AssetCostID
      ,tbl_ACH.AssetTransactionID
      ,tbl_ACH.CostingDate
      ,tbl_ACH.DocumentReference
      ,tbl_ACH.VendorID
      ,tbl_ACH.CostofAsset
      ,tbl_ACH.Comments
      ,tbl_ACH.Status
      ,AR_SET_Customer.CustomerName 
  FROM FA_AssetCostHeader AS tbl_ACH
  INNER JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = tbl_ACH.VendorID AND 
	  AR_SET_Customer.CompanyID = tbl_ACH.CompanyID AND AR_SET_Customer.PeriodID = tbl_ACH.PeriodID
	  AND AR_SET_Customer.Status = 1) AS FA_AssetCostHeader ";

        #endregion

        #region Class Methods

        public static DataTable GetAllDataWithOtherTables(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_Searching_WithPaging + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), Query_SelectAll_Searching_WithPaging + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_Searching + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""),
                        sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public bool InsertACHeaderAndDetail(List<FA_AssetCostDetail> colAssetCostDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertAssetCost");

            try
            {
                this.Insert(sqltrans);

                foreach (FA_AssetCostDetail ACd in colAssetCostDetail)
                {
                    ACd.AssetCostDetailID = FA_AssetCostDetail.GetNewAssetCostDetailID(f_AssetCostID, f_CompanyID, f_PeriodID, sqltrans);
                    ACd.Insert(sqltrans);
                }

                #region Update In FA_AssetIdentity

                //FA_AssetIdentity ai = new FA_AssetIdentity(f_AssetIdentityID, f_CompanyID, f_PeriodID)
                //{
                //    CostofAsset = f_CostofAsset,
                //};

                //ai.UpdateDataByKey(sqltrans);

                #endregion

                result = true;
            }
            catch (BusinessLogicException ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            finally
            {
                if (result)
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "InsertAssetCost");
            }

            return result;
        }

        public bool UpdateACHeaderAndDetail(List<FA_AssetCostDetail> colAddAssetCostDetail, List<FA_AssetCostDetail> colUpdateAssetCostDetail, List<FA_AssetCostDetail> colDeleteAssetCostDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateAssetCost");

            try
            {
                this.UpdateDataByKey(sqltrans);

                foreach (FA_AssetCostDetail ACdAdd in colAddAssetCostDetail)
                {
                    ACdAdd.AssetCostDetailID = FA_AssetCostDetail.GetNewAssetCostDetailID(f_AssetCostID, f_CompanyID, f_PeriodID, sqltrans);
                    ACdAdd.Insert(sqltrans);
                }

                foreach (FA_AssetCostDetail ACdUpdate in colUpdateAssetCostDetail)
                    ACdUpdate.UpdateDataByKey(sqltrans);

                foreach (FA_AssetCostDetail ACdDelete in colDeleteAssetCostDetail)
                    FA_AssetCostDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, ACdDelete.AssetCostDetailID, f_AssetCostID, f_CompanyID, f_PeriodID);

                #region Update In FA_AssetIdentity

                //FA_AssetIdentity ai = new FA_AssetIdentity(f_AssetIdentityID, f_CompanyID, f_PeriodID)
                //{
                //    CostofAsset = f_CostofAsset,
                //};

                //ai.UpdateDataByKey(sqltrans);

                #endregion

                result = true;
            }
            catch (BusinessLogicException ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            finally
            {
                if (result)
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateAssetCost");
            }

            return result;
        }

        public bool DeleteACHeaderAndDetail(List<FA_AssetCostDetail> colDeleteAssetCostDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("DeleteAssetCost");

            try
            {
                DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, UpdatedByValue, f_AssetCostID, f_CompanyID, f_PeriodID);

                foreach (FA_AssetCostDetail ACddDelete in colDeleteAssetCostDetail)
                    FA_AssetCostDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, ACddDelete.AssetCostDetailID, f_AssetCostID, f_CompanyID, f_PeriodID);

                result = true;
            }
            catch (BusinessLogicException ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            finally
            {
                if (result)
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "DeleteAssetCost");
            }
            return result;
        }

        #endregion
    }
}
