using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.FA.Transaction
{
    public partial class FA_AssetIdentity
    {
        #region Constant Queries

        const string Query_From_Other_Table = @" 
SELECT FA_AssetIdentity.*, FA_SET_AssetType.AssetTypeName, INV_Item.Description, ROW_NUMBER() Over (Order by AssetIdentityID) as 'Row' FROM FA_AssetIdentity 
    INNER JOIN FA_SET_AssetType ON FA_SET_AssetType.AssetTypeID = FA_AssetIdentity.AssetTypeID 
    INNER JOIN INV_Item ON FA_AssetIdentity.ItemID = INV_Item.ItemID --AND FA_AssetIdentity.CompanyID = INV_Item.CompanyID ";

        const string Query_For_DashBoard = @" 
Select TOP 7 FA_AssetIdentity.ItemName, AR_SET_Customer.CustomerName, FA_AssetIdentity.CostofAsset, FA_AssetIdentity.DepreciationRate From FA_AssetIdentity 
    Inner Join AR_SET_Customer On AR_SET_Customer.CustomerID = FA_AssetIdentity.CustodianID And AR_SET_Customer.CompanyID = FA_AssetIdentity.CompanyID 
Where FA_AssetIdentity.Status = 1 And FA_AssetIdentity.CompanyID = @CompanyID And FA_AssetIdentity.PeriodID = @PeriodID ";

        #endregion

        #region Class Methods

        public static DataTable Get_For_DashBoard(int companyID, int periodID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_For_DashBoard, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllData_FromOtherTable(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_From_Other_Table + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_From_Other_Table + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_From_Other_Table + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public bool Insert(FA_AssetInformation obj_AssetInformation)
        {
            bool result = false;
            SqlTransaction obj_SQL_Transaction = SqlHelper.GetTransaction("AssetIdentity");

            try
            {
                this.AssetIdentityID = GetNewAssetIdentityID(this.f_CompanyID, obj_SQL_Transaction);
                this.Insert(obj_SQL_Transaction);

                if (obj_AssetInformation != null)
                {
                    obj_AssetInformation.AssetIdentityID = this.f_AssetIdentityID;
                    obj_AssetInformation.AssetInfoID = FA_AssetInformation.GetNewAssetInfoID(obj_AssetInformation.CompanyID, obj_SQL_Transaction);
                    obj_AssetInformation.Insert(obj_SQL_Transaction);
                }

                result = true;
            }

            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            finally
            {
                if (result)
                    SqlHelper.CommitTransaction(obj_SQL_Transaction);
                else
                    SqlHelper.RollBackTransaction(obj_SQL_Transaction, "AssetIdentity");
            }

            return result;
        }

        public bool UpdateData()
        {
            bool result = false;
            SqlTransaction obj_SQL_Transaction = SqlHelper.GetTransaction("UpdateAssetIdentity");

            try
            {
                this.UpdateDataByKey(obj_SQL_Transaction);

                DataRow dr_AssetInformation = FA_AssetInformation.Get_DataByIdentity(this.CompanyID, this.AssetIdentityID, obj_SQL_Transaction);

                if (dr_AssetInformation != null)
                {
                    FA_AssetInformation obj_AssetInformation = new FA_AssetInformation(Convert.ToInt32(dr_AssetInformation["AssetInfoID"]), this.CompanyID, null,
                        obj_SQL_Transaction)
                        {
                            AssetTypeID = this.AssetTypeID,
                            CostCenterID = this.CostCenterID,
                            CustodianID = this.CustodianID,
                            DepartmentID = this.DepartmentID,
                            Description = this.ReferenceID,
                            ItemID = this.ItemID,
                            LocationID = this.LocationID,
                            NoOfUnits = this.Quantity,
                            PODetailID = this.PODetailID,
                            POHeaderID = this.POHeaderID,
                            PurchaseDate = this.PurchaseDate.Value,
                            SalvageValue = this.SalvageValue,
                            StockDetailID = this.GRNDetailID,
                            StockHeaderID = this.GRNHeaderID,
                            TagNumber = this.TagNumber,
                            TagPrefix = this.TagPrefix,
                            WrittenDownValue = this.WDV,
                            Status = true,
                            UpdateDate = this.UpdateDate,
                            UpdatedBy = this.UpdatedBy,
                            UpdatedByValue = this.UpdatedByValue
                        };

                    obj_AssetInformation.UpdateDataByKey(obj_SQL_Transaction);
                }

                result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            finally
            {
                if (result)
                    SqlHelper.CommitTransaction(obj_SQL_Transaction);
                else
                    SqlHelper.RollBackTransaction(obj_SQL_Transaction, "UpdateAssetIdentity");
            }

            return result;
        }

        public static bool DeleteData(Guid updatedBy, DateTime updateDate, string updatedByValue, int assetIdentityID, int companyID)
        {
            bool result = false;
            SqlTransaction obj_SQL_Transaction = SqlHelper.GetTransaction("DeleteAssetIdentity");

            try
            {
                FA_AssetIdentity.DeleteDataByKey(obj_SQL_Transaction, updatedBy, updateDate, updatedByValue, assetIdentityID, companyID);

                DataRow dr_AssetInformation = FA_AssetInformation.Get_DataByIdentity(companyID, assetIdentityID, obj_SQL_Transaction);

                if (dr_AssetInformation != null)
                {
                    FA_AssetInformation.DeleteDataByKey(obj_SQL_Transaction, updatedBy, updateDate, updatedByValue, 
                        Convert.ToInt32(dr_AssetInformation["AssetInfoID"]), companyID);
                }

                result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            finally
            {
                if (result)
                    SqlHelper.CommitTransaction(obj_SQL_Transaction);
                else
                    SqlHelper.RollBackTransaction(obj_SQL_Transaction, "DeleteAssetIdentity");
            }

            return result;
        }

        #endregion
    }
}
