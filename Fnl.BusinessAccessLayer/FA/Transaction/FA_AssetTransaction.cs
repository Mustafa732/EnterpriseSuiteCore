using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;

namespace FnI.BusinessAccessLayer.FA.Transaction
{
	public partial class FA_AssetTransaction
	{
		#region Constant Strings

        const string Query_GetDataBYItemID = @"
select * from FA_AssetTransaction where Status = 1 and CompanyID = @CompanyID and ItemID = @ItemID
";
        const string Query_GetvendorNamebyAssetTag = @"
select VendorID from FA_AssetTransaction where AssetTag = @AssetTag
and FA_AssetTransaction.Status = 1
and FA_AssetTransaction.CompanyID = @CompanyID

";
        const string Query_GetNewAssetTag = @" 
select Case 
    when len(CAST((ISNULL(Max(Cast(AssetTag as int)), 0) + 1) as VarChar(6))) = 1 then '00000' + CAST((ISNULL(Max(Cast(AssetTag as int)), 0) + 1) as VarChar(6)) 
    when len(CAST((ISNULL(Max(Cast(AssetTag as int)), 0) + 1) as VarChar(6))) = 2 then '0000' + CAST((ISNULL(Max(Cast(AssetTag as int)), 0) + 1) as VarChar(6)) 
    when len(CAST((ISNULL(Max(Cast(AssetTag as int)), 0) + 1) as VarChar(6))) = 3 then '000' + CAST((ISNULL(Max(Cast(AssetTag as int)), 0) + 1) as VarChar(6)) 
    when len(CAST((ISNULL(Max(Cast(AssetTag as int)), 0) + 1) as VarChar(6))) = 4 then '00' + CAST((ISNULL(Max(Cast(AssetTag as int)), 0) + 1) as VarChar(6)) 
    when len(CAST((ISNULL(Max(Cast(AssetTag as int)), 0) + 1) as VarChar(6))) = 5 then '0' + CAST((ISNULL(Max(Cast(AssetTag as int)), 0) + 1) as VarChar(6)) 
    ELSE CAST((ISNULL(Max(Cast(AssetTag as int)), 0) + 1) as VarChar(6)) END 
from FA_AssetInformation 
where CompanyID = @CompanyID ";

        const string Query_GetDataForLookup = @"
SELECT 
*,
INV_Item.Description as ItemName,
[FA_AssetTransaction].PurchaseDate
FROM [FA_AssetTransaction] 
Inner Join INV_Item On INV_Item.ItemID = [FA_AssetTransaction].ItemID
	--and INV_Item.CompanyID = [FA_AssetTransaction].CompanyID
WHERE [FA_AssetTransaction].[Status] = 1 
	AND ([FA_AssetTransaction].[CompanyID] = @CompanyID OR @CompanyID is null)
{0}

";

        const string Query_CheckAsset = @"SELECT 
*,
INV_Item.Description as ItemName,
[FA_AssetTransaction].PurchaseDate
FROM [FA_AssetTransaction] 
Inner Join INV_Item On INV_Item.ItemID = [FA_AssetTransaction].ItemID
	--and INV_Item.CompanyID = [FA_AssetTransaction].CompanyID
WHERE [FA_AssetTransaction].[Status] = 1 
	AND ([FA_AssetTransaction].[CompanyID] = @CompanyID OR @CompanyID is null)
	AND AssetTag = @AssetTag
	";

        const string Query_Asset_Transaction_Dropdownlist = @"SELECT AssetTransactionID, AssetTag from FA_AssetTransaction WHERE FA_AssetTransaction.status =1";

        
        const string Query_AssetTransactionDetail_WithPaging = @"
select
    [FA_SET_AssetType].AssetTypeName
    ,INV_Item.Description as ItemName,INV_SET_Category.CategoryName
    ,manufacturer.CustomerName as Manufacturer
    ,vendor.CustomerName as Vendor
    ,rp.CustomerName as ResponsiblePerson
    ,cust.CustomerName as Custodian
    ,warranty.IntervalValue as Warranty
    ,life.IntervalValue as ExpectedLife
    ,set_UnitofMeasure.UOMName
    ,set_department.DepartmentName  as OwnerDepartment
    ,set_Location.LocationName
    ,sth.DocumentNo as GRN_NO
    ,POH.DocumentNumber as PO_No
    ,IH.InvoiceNumber	
	,case DepreciationMethodCode
        when 1 then 
		'Declining_Balance'
		when 2 then 
		'Straight_Line' 
    end as DepreciationMethod
    ,FA_AssetTransaction.TransactionDate as AssetTransactionDate
    ,FA_AssetTransaction.*
    ,ROW_NUMBER() Over (Order by FA_AssetTransaction.AssetTransactionID, FA_AssetTransaction.CompanyID) as 'Row'
from FA_AssetTransaction 
      
    left join [FA_SET_AssetType] on [FA_SET_AssetType].AssetTypeID = FA_AssetTransaction.AssetTypeID
        and [FA_SET_AssetType].CompanyID = FA_AssetTransaction.CompanyID
        and [FA_SET_AssetType].Status =  1 
        and FA_AssetTransaction .Status = 1
      
    left join INV_Item on INV_Item.ItemID = FA_AssetTransaction.ItemID
        --and INV_Item.CompanyID = FA_AssetTransaction.CompanyID  
        and INV_Item.Status = 1      

 left join INV_SET_Category on INV_SET_Category.CategoryID = INV_Item.ItemTypeID
    --   
      AND INV_SET_Category.Status = 1

    left join AR_SET_Customer manufacturer on manufacturer.CustomerID = FA_AssetTransaction.ManufacturarID
        and manufacturer.CompanyID = FA_AssetTransaction.CompanyID
        and manufacturer.Status = 1
      
    left join AR_SET_Customer vendor on vendor.CustomerID = FA_AssetTransaction.VendorID 
        and vendor.CompanyID = FA_AssetTransaction.CompanyID
        and vendor.Status = 1
      
    left join AR_SET_Customer rp on rp.CustomerID = FA_AssetTransaction.ResponsiblePersonID 
        and rp.CompanyID = FA_AssetTransaction.CompanyID
        and rp.Status = 1
      
    left join AR_SET_Customer cust on cust.CustomerID = FA_AssetTransaction.CustodianID 
        and cust.CompanyID = FA_AssetTransaction.CompanyID
        and cust.Status = 1
      
    left join  dbo.FA_Period warranty on  warranty.FAPeriodID = FA_AssetTransaction.WarrantyID
        and warranty.CompanyID = FA_AssetTransaction.CompanyID
        and warranty.Status  = 1 
        and warranty.PeriodTypeID  = 2
      
    left join  set_UnitofMeasure  on set_UnitofMeasure.UOMID = FA_AssetTransaction.UOMID
        and set_UnitofMeasure.Status = 1 
      
    left join  set_department on  set_department.DepartmentID =  FA_AssetTransaction.OwnerDepartmentID
        and set_department.Status = 1 
      
    left join set_Location on set_Location.LocationID = FA_AssetTransaction.LocationID
        and SET_location.Status = 1
      
    left join INV_StockTransactionHeader sth on sth.StockTransactionID =  FA_AssetTransaction.StockTransactionID
        and sth.CompanyID =  FA_AssetTransaction.CompanyID
        and sth.Status = 1 
      
    left join dbo.AP_PurchaseOrderHeader POH on POH.POHeaderID = FA_AssetTransaction.POHeaderID
        and POH.CompanyID = FA_AssetTransaction.CompanyID
        and POH.Status =  1
     
    left join  dbo.AR_InvoiceHeader IH on IH.InvoiceID = FA_AssetTransaction.InvoiceID
        and IH.CompanyID = FA_AssetTransaction.CompanyID 
        and IH.Status =  1 
     
    left join  dbo.FA_Period life on  life.FAPeriodID = FA_AssetTransaction.ExpectedLifeID
        and life.CompanyID = FA_AssetTransaction.CompanyID
        and life.Status  = 1 
        and life.PeriodTypeID  = 1
";
  
        const string Query_AssetTransactionDetail_WithoutPaging = @"
select
    [FA_SET_AssetType].AssetTypeName
    ,INV_Item.Description as ItemName
    ,manufacturer.CustomerName as Manufacturer
    ,vendor.CustomerName as Vendor
    ,rp.CustomerName as ResponsiblePerson
    ,cust.CustomerName as Custodian
    ,warranty.IntervalValue as Warranty
    ,life.IntervalValue as ExpectedLife
    ,set_UnitofMeasure.UOMName
    ,set_department.DepartmentName  as OwnerDepartment
    ,set_Location.LocationName
    ,sth.DocumentNo as GRN_NO
    ,POH.DocumentNumber as PO_No
    ,IH.InvoiceNumber	
	,case DepreciationMethodCode
        when 1 then 
		'Declining_Balance'
		when 2 then 
		'Straight_Line' 
    end as DepreciationMethod
    ,FA_AssetTransaction.Date as AssetTransactionDate
    ,FA_AssetTransaction.*
from FA_AssetTransaction 
      
    left join [FA_SET_AssetType] on [FA_SET_AssetType].AssetTypeID = FA_AssetTransaction.AssetTypeID
        and [FA_SET_AssetType].CompanyID = FA_AssetTransaction.CompanyID
        and [FA_SET_AssetType].Status =  1 
        and FA_AssetTransaction .Status = 1
      
    left join INV_Item on INV_Item.ItemID = FA_AssetTransaction.ItemID
        --and INV_Item.CompanyID = FA_AssetTransaction.CompanyID  
        and INV_Item.Status = 1      
      
    left join AR_SET_Customer manufacturer on manufacturer.CustomerID = FA_AssetTransaction.ManufacturarID
        and manufacturer.CompanyID = FA_AssetTransaction.CompanyID
        and manufacturer.Status = 1
      
    left join AR_SET_Customer vendor on vendor.CustomerID = FA_AssetTransaction.VendorID 
        and vendor.CompanyID = FA_AssetTransaction.CompanyID
        and vendor.Status = 1
      
    left join AR_SET_Customer rp on rp.CustomerID = FA_AssetTransaction.ResponsiblePersonID 
        and rp.CompanyID = FA_AssetTransaction.CompanyID
        and rp.Status = 1
      
    left join AR_SET_Customer cust on cust.CustomerID = FA_AssetTransaction.CustodianID 
        and cust.CompanyID = FA_AssetTransaction.CompanyID
        and cust.Status = 1
      
    left join  dbo.FA_Period warranty on  warranty.FAPeriodID = FA_AssetTransaction.WarrantyID
        and warranty.CompanyID = FA_AssetTransaction.CompanyID
        and warranty.Status  = 1 
        and warranty.PeriodTypeID = 2
      
    left join  set_UnitofMeasure  on set_UnitofMeasure.UOMID = FA_AssetTransaction.UOMID
        and set_UnitofMeasure.Status = 1 
      
    left join  set_department on  set_department.DepartmentID =  FA_AssetTransaction.OwnerDepartmentID
        and set_department.Status = 1 
      
    left join set_Location on set_Location.LocationID = FA_AssetTransaction.LocationID
        and SET_location.Status = 1
      
    left join INV_StockTransactionHeader sth on sth.StockTransactionID =  FA_AssetTransaction.StockTransactionID
        and sth.CompanyID =  FA_AssetTransaction.CompanyID
        and sth.Status = 1 
      
    left join dbo.AP_PurchaseOrderHeader  POH on POH.POHeaderID =  FA_AssetTransaction.POHeaderID
        and POH.CompanyID =  FA_AssetTransaction.CompanyID
        and POH.Status =  1
     
    left join  dbo.AR_InvoiceHeader IH on IH.InvoiceID = FA_AssetTransaction.InvoiceID
        and IH.CompanyID = FA_AssetTransaction.CompanyID 
        and IH.Status =  1 
     
    left join  dbo.FA_Period life on  life.FAPeriodID = FA_AssetTransaction.ExpectedLifeID
        and life.CompanyID = FA_AssetTransaction.CompanyID
        and life.Status  = 1 
        and life.PeriodTypeID  = 1
";



		#endregion

		#region Class Methods

		public static string GetNewAssetTag(int companyID)
		{
			string assetTag = "";

			try
			{
				SqlParameter[] sqlparams = new SqlParameter[1];
				sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                assetTag = SqlHelper.ExecuteScalar(CommandType.Text, Query_GetNewAssetTag, sqlparams).ToString();
			}
			catch (Exception ex)
			{
				SqlHelper.LogException(ex);
				throw ex;
			}

            return assetTag;
		}

        public static DataTable GetDataBYItemID(int companyID, int ItemID, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@ItemID", ItemID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataBYItemID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDataBYItemID, sqlparams);

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

        public static DataTable GetvendorNamebyAssetTag(int companyID, string AssetTag, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@AssetTag", AssetTag);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetvendorNamebyAssetTag, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetvendorNamebyAssetTag, sqlparams);

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

        
        public static DataTable CheckAsset(int companyID, int AssetTag, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@AssetTag", AssetTag);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_CheckAsset, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_CheckAsset, sqlparams);

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


        public static DataTable GetDataForLookup(Nullable<int> companyID, string filter = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format((Query_GetDataForLookup),filter), sqlparams);

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

        public static DataTable AssetTransactionddl(SqlConnection objConnection = null)
        {
            DataTable dt = null;
            try
            {
                DataSet ds = null;
                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Asset_Transaction_Dropdownlist);
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

        public static DataTable GetAssetTransactionData(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_AssetTransactionDetail_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_AssetTransactionDetail_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_AssetTransactionDetail_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_AssetTransactionDetail_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_AssetTransactionDetail_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_AssetTransactionDetail_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
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
