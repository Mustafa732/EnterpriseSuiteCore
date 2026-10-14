using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;


namespace FnI.BusinessAccessLayer.FA.Setup
{
    public partial class FA_SET_AssetType
    {
        #region Constant String

//////////        const string Query_AssetTransaction = @"
//////////
//////////select
//////////      FA_SET_AssetType.AssetTypeName
//////////      ,INV_Item.Description as ItemName
//////////      ,manufacturer.CustomerName as Manufacturer
//////////      ,vendor.CustomerName as Vendor
//////////      ,rp.CustomerName as ResponsiblePerson
//////////      ,cust.CustomerName as Custodian
//////////      ,warranty.IntervalValue as Warranty
//////////      ,life.IntervalValue as ExpectedLife
//////////      ,set_UnitofMeasure.UOMName
//////////      ,set_department.DepartmentName  as OwnerDepartment
//////////      ,set_Location.LocationName
//////////      ,sth.DocumentNo as GRN_NO
//////////      ,POH.DocumentNumber as PO_No
//////////      ,IH.InvoiceNumber	
//////////	  ,case DepreciationMethodCode
//////////			  when 1 then 
//////////			  'Declining_Balance'
//////////			  when 2 then 
//////////			  'Straight_Line' 
//////////			 end as DepreciationMethod
//////////	  ,convert (varchar(20), FA_AssetTransaction.Date, 106 ) as AssetTransactionDate
//////////	  ,FA_AssetTransaction.CompanyID
//////////      ,FA_AssetTransaction.AssetTransactionID
//////////      ,FA_AssetTransaction.AssetTag
//////////      ,FA_AssetTransaction.ItemID
//////////      ,CONVERT ( varchar(20), FA_AssetTransaction.Date  ,106) as Date 
//////////      ,FA_AssetTransaction.AssetTypeID
//////////      ,FA_AssetTransaction.ManufacturarID
//////////      ,FA_AssetTransaction.ModelNo
//////////      ,FA_AssetTransaction.SerialNo
//////////      ,FA_AssetTransaction.VendorID
//////////      ,FA_AssetTransaction.Value
//////////      ,FA_AssetTransaction.CustodianID
//////////      ,FA_AssetTransaction.WrittenDownValue
//////////      ,FA_AssetTransaction.ResidualValue
//////////      ,FA_AssetTransaction.POHeaderID
//////////      ,FA_AssetTransaction.StockTransactionID
//////////       ,convert (varchar(20), FA_AssetTransaction.PurchaseDate ,106)  as PurchaseDate
//////////      ,FA_AssetTransaction.UOMID
//////////      ,FA_AssetTransaction.NoOfUnits
//////////      ,FA_AssetTransaction.InvoiceID
//////////      ,FA_AssetTransaction.WarrantyID
//////////       ,convert (varchar(20), FA_AssetTransaction.ExpiringOn  , 106 )  as ExpiringOn
//////////      ,FA_AssetTransaction.OwnerDepartmentID
//////////      ,FA_AssetTransaction.LocationID
//////////      ,FA_AssetTransaction.RoomNo
//////////      ,FA_AssetTransaction.ResponsiblePersonID
//////////      ,convert (varchar(20), FA_AssetTransaction.TakenOverOn, 106) as TakenOverOn
//////////      ,FA_AssetTransaction.LeasedBy
//////////      ,FA_AssetTransaction.DepreciationRate
//////////      ,FA_AssetTransaction.ExpectedLifeID
//////////      ,FA_AssetTransaction.DepreciationMethodCode
//////////      ,FA_AssetTransaction.IsChild
//////////      ,FA_AssetTransaction.ParentAssetNumber
//////////      
//////////       from FA_AssetTransaction 
//////////      
//////////      left join FA_SET_AssetType on FA_SET_AssetType.AssetTypeID = FA_AssetTransaction.AssetTypeID
//////////      and FA_SET_AssetType.CompanyID = FA_AssetTransaction.CompanyID
//////////      and FA_SET_AssetType.Status =  1 
//////////      and FA_AssetTransaction .Status = 1
//////////      
//////////      left join INV_Item on INV_Item.ItemID  =   FA_AssetTransaction.ItemID
//////////      and INV_Item.CompanyID =   FA_AssetTransaction.CompanyID  
//////////      and INV_Item.Status = 1      
//////////      
//////////      left join AR_SET_Customer manufacturer on manufacturer.CustomerID = FA_AssetTransaction.ManufacturarID
//////////      and manufacturer.CompanyID = FA_AssetTransaction.CompanyID
//////////      and manufacturer.Status = 1
//////////      
//////////      left join AR_SET_Customer vendor on vendor.CustomerID = FA_AssetTransaction.VendorID 
//////////      and vendor.CompanyID = FA_AssetTransaction.CompanyID
//////////      and vendor.Status = 1
//////////      
//////////      left join AR_SET_Customer rp on rp.CustomerID = FA_AssetTransaction.ResponsiblePersonID 
//////////      and rp.CompanyID = FA_AssetTransaction.CompanyID
//////////      and rp.Status = 1
//////////      
//////////      left join AR_SET_Customer cust on cust.CustomerID = FA_AssetTransaction.CustodianID 
//////////      and cust.CompanyID = FA_AssetTransaction.CompanyID
//////////      and cust.Status = 1
//////////      
//////////      left join  dbo.FA_Period warranty on  warranty.FAPeriodID = FA_AssetTransaction.WarrantyID
//////////      and warranty.CompanyID = FA_AssetTransaction.CompanyID
//////////      and warranty.Status  = 1 
//////////      and warranty.PeriodTypeID  = 2
//////////      
//////////      left join  set_UnitofMeasure  on set_UnitofMeasure.UOMID = FA_AssetTransaction.UOMID
//////////      and set_UnitofMeasure.Status = 1 
//////////      
//////////      left join  set_department on  set_department.DepartmentID =  FA_AssetTransaction.OwnerDepartmentID
//////////      and set_department.Status = 1 
//////////      
//////////      left join set_Location on set_Location.LocationID = FA_AssetTransaction.LocationID
//////////      and SET_location.Status = 1
//////////      
//////////      left join INV_StockTransactionHeader sth on sth.StockTransactionID =  FA_AssetTransaction.StockTransactionID
//////////      and sth.CompanyID =  FA_AssetTransaction.CompanyID
//////////      and sth.Status = 1 
//////////      
//////////      left join dbo.AP_PurchaseOrderHeader  POH on POH.POHeaderID =  FA_AssetTransaction.POHeaderID
//////////     and POH.CompanyID =  FA_AssetTransaction.CompanyID
//////////     and POH.Status =  1
//////////     
//////////     
//////////     left join  dbo.AR_InvoiceHeader IH on IH.InvoiceID = FA_AssetTransaction.InvoiceID
//////////     and IH.CompanyID = FA_AssetTransaction.CompanyID 
//////////     and IH.Status =  1 
//////////     
//////////     left join  dbo.FA_Period life on  life.FAPeriodID = FA_AssetTransaction.ExpectedLifeID
//////////      and life.CompanyID = FA_AssetTransaction.CompanyID
//////////      and life.Status  = 1 
//////////      and life.PeriodTypeID  = 1
//////////     
//////////     where FA_AssetTransaction.Status = 1
//////////     and FA_AssetTransaction.CompanyID = @CompanyID
//////////
//////////"; 

        const string Query_AssetTransaction = @"select
      FA_SET_AssetType.AssetTypeName
      ,INV_Item.Description as ItemName
      ,manufacturer.CustomerName as Manufacturer
      ,vendor.CustomerName as Vendor
      ,rp.CustomerName as ResponsiblePerson
      ,cust.CustomerName as Custodian
      --,warranty.IntervalValue as Warranty
      --,life.IntervalValue as ExpectedLife
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
	  ,convert (varchar(20), FA_AssetInformation.TransactionDate, 106 )  AS AssetTransactionDate 
	  ,FA_AssetInformation.CompanyID
      ,FA_AssetInformation.AssetTransactionID
      ,FA_AssetInformation.AssetTag
      ,FA_AssetInformation.ItemID
      ,CONVERT ( varchar(20), FA_AssetInformation.CreateDate  ,106) as Date 
      ,FA_AssetInformation.AssetTypeID
      ,FA_AssetInformation.ManufacturarID
      ,FA_AssetInformation.ModelNo
      ,FA_AssetInformation.SerialNo
      ,FA_AssetInformation.VendorID
      ,FA_AssetInformation.TestValue
      ,FA_AssetInformation.CustodianID
      ,FA_AssetInformation.WrittenDownValue
      ,FA_AssetInformation.ResidualValue
      ,FA_AssetInformation.POHeaderID
      ,FA_AssetInformation.StockTransactionID
       ,convert (varchar(20), FA_AssetInformation.PurchaseDate ,106)  as PurchaseDate
      ,FA_AssetInformation.UOMID
      ,FA_AssetInformation.NoOfUnits
      ,FA_AssetInformation.InvoiceID
      ,FA_AssetInformation.WarrantyID
       ,convert (varchar(20), FA_AssetInformation.ExpiringOn  , 106 )  as ExpiringOn
      ,FA_AssetInformation.OwnerDepartmentID
      ,FA_AssetInformation.LocationID
      ,FA_AssetInformation.RoomNo
      ,FA_AssetInformation.ResponsiblePersonID
      ,convert (varchar(20), FA_AssetInformation.TakenOverOn, 106) as TakenOverOn
      ,FA_AssetInformation.LeasedBy
      ,FA_AssetInformation.DepreciationRate
      ,FA_AssetInformation.ExpectedLifeID
      ,FA_AssetInformation.DepreciationMethodCode
      ,FA_AssetInformation.IsChild
      ,FA_AssetInformation.ParentAssetNumber
      
       from FA_AssetInformation 
      left join FA_SET_AssetType on FA_SET_AssetType.AssetTypeID = FA_AssetInformation.AssetTypeID
      and FA_SET_AssetType.CompanyID = FA_AssetInformation.CompanyID
      and FA_SET_AssetType.Status =  1 
      and FA_AssetInformation .Status = 1
      
      left join INV_Item on INV_Item.ItemID  =   FA_AssetInformation.ItemID
      --and INV_Item.CompanyID =   FA_AssetInformation.CompanyID  
      and INV_Item.Status = 1      
      
      left join AR_SET_Customer manufacturer on manufacturer.CustomerID = FA_AssetInformation.ManufacturarID
      and manufacturer.CompanyID = FA_AssetInformation.CompanyID
      and manufacturer.Status = 1
      
      left join AR_SET_Customer vendor on vendor.CustomerID = FA_AssetInformation.VendorID 
      and vendor.CompanyID = FA_AssetInformation.CompanyID
      and vendor.Status = 1
      
      left join AR_SET_Customer rp on rp.CustomerID = FA_AssetInformation.ResponsiblePersonID 
      and rp.CompanyID = FA_AssetInformation.CompanyID
      and rp.Status = 1
      
      left join AR_SET_Customer cust on cust.CustomerID = FA_AssetInformation.CustodianID 
      and cust.CompanyID = FA_AssetInformation.CompanyID
      and cust.Status = 1
      
      ----left join  dbo.FA_Period warranty on  warranty.FAPeriodID = FA_AssetInformation.WarrantyID
      ----and warranty.CompanyID = FA_AssetInformation.CompanyID
      ----and warranty.Status  = 1 
      ----and warranty.PeriodTypeID  = 2
      
      left join  set_UnitofMeasure  on set_UnitofMeasure.UOMID = FA_AssetInformation.UOMID
      and set_UnitofMeasure.Status = 1 
      
      left join  set_department on  set_department.DepartmentID =  FA_AssetInformation.OwnerDepartmentID
      and set_department.Status = 1 
      
      left join set_Location on set_Location.LocationID = FA_AssetInformation.LocationID
      and SET_location.Status = 1
      
      left join INV_StockTransactionHeader sth on sth.StockTransactionID =  FA_AssetInformation.StockTransactionID
      and sth.CompanyID =  FA_AssetInformation.CompanyID
      and sth.Status = 1 
      
      left join dbo.AP_PurchaseOrderHeader  POH on POH.POHeaderID =  FA_AssetInformation.POHeaderID
     and POH.CompanyID =  FA_AssetInformation.CompanyID
     and POH.Status =  1
     
     
     left join  dbo.AR_InvoiceHeader IH on IH.InvoiceID = FA_AssetInformation.InvoiceID
     and IH.CompanyID = FA_AssetInformation.CompanyID 
     and IH.Status =  1 
     
     ----left join  dbo.FA_Period life on  life.FAPeriodID = FA_AssetInformation.ExpectedLifeID
     ---- and life.CompanyID = FA_AssetInformation.CompanyID
     ---- and life.Status  = 1 
     ---- and life.PeriodTypeID  = 1
     
     where FA_AssetInformation.Status = 1
     and FA_AssetInformation.CompanyID = 1

";

        const string Query_GetDataByItemID = @"
SELECT 
	AssetTypeID,
	AssetTypeName,
	FA_SET_AssetHeadType.AssetHeadTypeID,
	AssetHeadTypeName,
	INV_Item.ItemID,
	INV_Item.Description,
	SET_UnitOfMeasure.UOMID,
	SET_UnitOfMeasure.UOMName,
	CategoryID 
FROM [FA_SET_AssetType] 
INNER JOIN FA_SET_AssetHeadType on FA_SET_AssetHeadType.AssetHeadTypeID = FA_SET_AssetType.AssetHeadTypeID
	and FA_SET_AssetHeadType.CompanyID = FA_SET_AssetType.CompanyID
Inner Join INV_Item on INV_Item.ItemID = FA_SET_AssetType.ItemID
	--and INV_Item.CompanyID = FA_SET_AssetType.CompanyID
INNER JOIN SET_UnitOfMeasure on SET_UnitofMeasure.UOMID = INV_Item.PrimaryUOMCode
WHERE [FA_SET_AssetType].ItemID = @ItemID
	AND [FA_SET_AssetType].CompanyID = @CompanyID
	AND [FA_SET_AssetType].[Status] = 1 
";


        const string OverrideQueryString = @"select FA_SET_AssetType.AssetTypeID,FA_SET_AssetType.AssetTypeName,
AccDeprAccountCode,FA_SET_AssetType.CreateDate
FROM FA_SET_AssetType 
     
WHERE FA_SET_AssetType.[Status] = 1 
{0}

";



        const string OverrideQueryStringForPeriod = @"
select 
	[AssetLifeID],
	(Cast(IntervalValue as Varchar(10)) + '  ' + PeriodType) as Description
	
from [FA_Set_AssetLife]
   
WHERE [FA_Set_AssetLife].[Status] = 1 

";

        #endregion
        
        #region Methods

        public static DataTable GetAssetTransaction(int companyID, string filters = "")
        {
            DataTable dt = null;


            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                DataSet ds = null;
                string query = Query_AssetTransaction + filters;
                ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

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

        public static DataTable GetAllActiveDataForLookup(Nullable<int> companyID, Nullable<bool> isActive = true, SqlConnection objConnection = null, 
            SqlTransaction objTransaction = null, string filter = null)
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

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(OverrideQueryString, filter), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(OverrideQueryString, filter), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(OverrideQueryString, filter), sqlparams);

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


        public static DataTable GetAllActiveDataForPeriodLookup( Nullable<bool> isActive = true,
            string filter = null, string periodType = null)
        {
            DataSet ds = null;
            DataTable dt = null;
            
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[0];

                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(OverrideQueryStringForPeriod + filter), sqlparams);

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

        public static DataTable Get_ItemCategoryName(int companyID, SqlConnection obj_SQL_Connection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                string Query_Select_ItemCategoryNameFromAssetTypePolicy = @" Select * From FA_SET_AssetType 
Where FA_SET_AssetType.Status = 1 ";

                if(obj_SQL_Connection != null)
                    dt = SqlHelper.ExecuteDataset(obj_SQL_Connection, CommandType.Text, Query_Select_ItemCategoryNameFromAssetTypePolicy, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_ItemCategoryNameFromAssetTypePolicy, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable Get_AssetTypeInFA(int companyID, SqlConnection obj_SQL_Connection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                string Query_Select_ItemCategoryNameFromAssetTypePolicy = @" Select * From FA_SET_AssetType 
Where FA_SET_AssetType.Status = 1 AND CompanyID = @CompanyID ";

                if (obj_SQL_Connection != null)
                    dt = SqlHelper.ExecuteDataset(obj_SQL_Connection, CommandType.Text, Query_Select_ItemCategoryNameFromAssetTypePolicy, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_ItemCategoryNameFromAssetTypePolicy, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable Get_AssetTypeGLCode(int companyID,int AssetTypeID, SqlConnection obj_SQL_Connection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[0] = new SqlParameter("@AssetTypeID", AssetTypeID);

                string Query = @" Select * From FA_SET_AssetType 
                            Where FA_SET_AssetType.Status = 1 AND CompanyID = @CompanyID and AssetTypeID =@AssetTypeID ";

                if (obj_SQL_Connection != null)
                    dt = SqlHelper.ExecuteDataset(obj_SQL_Connection, CommandType.Text, Query, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams).Tables[0];
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
