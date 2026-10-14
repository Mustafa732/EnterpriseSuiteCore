using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;

using Common.Utils;

using DAL.DataAccessLayer;

using FnI.BusinessAccessLayer.Inventory.Setup;
using FnI.BusinessAccessLayer.Inventory.Transaction;
using System.Collections.Generic;

namespace FnI.BusinessAccessLayer.FA.Transaction
{
    public partial class FA_AssetInformation
    {
        #region Queries

        const string Query_From_Other_Table = @" 
SELECT FA_AssetInformation.*,(case when FA_AssetInformation.isdisposed is null then 'Not Disposed' else 'Disposed' end)AssetStatus, FA_SET_AssetType.AssetTypeName, INV_Item.Description As ItemName, INV_StockTransactionHeader.DocumentNo,ROW_NUMBER() Over (Order by FA_AssetInformation.AssetInfoID) as 'Row' 
FROM FA_AssetInformation 
    INNER JOIN FA_SET_AssetType ON FA_SET_AssetType.AssetTypeID = FA_AssetInformation.AssetTypeID 
    INNER JOIN INV_Item ON FA_AssetInformation.ItemID = INV_Item.ItemID --AND FA_AssetInformation.CompanyID = INV_Item.CompanyID 
    LEFT JOIN INV_StockTransactionDetail ON INV_StockTransactionDetail.StockTransactionID = FA_AssetInformation.StockHeaderID and  INV_StockTransactionDetail.TransactionDetailID = FA_AssetInformation.StockDetailID
    LEFT JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 

";
#endregion 


        #region Methods

        public static DataRow Get_DataByIdentity(int companyID, int identityID, SqlTransaction obj_SQL_Transaction=null)
        {
            DataSet ds = null;
            DataRow drToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@IdentityID", identityID);
                
                string qry_DataByIdentity = @" Select * From FA_AssetInformation where CompanyID = @CompanyID And AssetIdentityID = @IdentityID And Status = 1 ";

                if(obj_SQL_Transaction != null)
                    ds = SqlHelper.ExecuteDataset(obj_SQL_Transaction, CommandType.Text, qry_DataByIdentity, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, qry_DataByIdentity, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    drToReturn = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return drToReturn;
        }

        public static DataRow Get_DataByAssetInfoID(int companyID, int AssetInfoID, SqlTransaction obj_SQL_Transaction = null)
        {
            DataSet ds = null;
            DataRow drToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@AssetInfoID", AssetInfoID);

                string qry_DataByIdentity = @" Select (select AccDeprAccountCode from FA_SET_AssetType where FA_SET_AssetType.AssetTypeID = FA_AssetInformation.AssetTypeID and   FA_SET_AssetType.CompanyID = FA_AssetInformation.CompanyID) as AssetTypeCode, * From FA_AssetInformation where CompanyID = @CompanyID And AssetInfoID = @AssetInfoID And Status = 1 ";

                if (obj_SQL_Transaction != null)
                    ds = SqlHelper.ExecuteDataset(obj_SQL_Transaction, CommandType.Text, qry_DataByIdentity, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, qry_DataByIdentity, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    drToReturn = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return drToReturn;
        }


        public static DataTable Get_DataByItemID(int companyID, int? itemID, SqlTransaction obj_SQL_Transaction, int? AssetTypeID = null)
        {
            DataSet ds = null;
            DataTable drToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (itemID.HasValue)
                    sqlparams[1] = new SqlParameter("@itemID", itemID);
                else
                    sqlparams[1] = new SqlParameter("@itemID", DBNull.Value);

                if (AssetTypeID.HasValue)
                    sqlparams[2] = new SqlParameter("@AssetTypeID", AssetTypeID);
                else
                    sqlparams[2] = new SqlParameter("@AssetTypeID", DBNull.Value);

                string qry_DataByIdentity = @" Select (tagprefix+'-'+tagnumber)as assettag,assetinfoid From FA_AssetInformation where CompanyID = @CompanyID And (ItemID = @itemID OR @itemID IS NULL) AND (AssetTypeID = @AssetTypeID OR @AssetTypeID IS NULL) And Status = 1  and FA_AssetInformation.isdisposed is null ";

                if (obj_SQL_Transaction != null)
                    ds = SqlHelper.ExecuteDataset(obj_SQL_Transaction, CommandType.Text, qry_DataByIdentity, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, qry_DataByIdentity, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    drToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return drToReturn;
        }

        public static DataTable GetAllAssetTagNumber(int companyID, SqlTransaction obj_SQL_Transaction=null)
        {
            DataSet ds = null;
            DataTable drToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                

                string qry_DataByIdentity = @" Select (tagprefix+'-'+tagnumber)as assettag,assetinfoid From FA_AssetInformation where CompanyID = @CompanyID  And Status = 1 ";

                if (obj_SQL_Transaction != null)
                    ds = SqlHelper.ExecuteDataset(obj_SQL_Transaction, CommandType.Text, qry_DataByIdentity, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, qry_DataByIdentity, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    drToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return drToReturn;
        }


        public static DataTable GetDataForLookup(int companyID, string filter)
        {
            DataSet ds = null;
            DataTable drToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                string qry_DataByIdentity = @" select AssetInfoID as AssetTransactionID , (TagPrefix +'-'+ TagNumber)  as  AssetTag, INV_Item.Description as ItemName, PurchaseDate from FA_AssetInformation, INV_Item 
where FA_AssetInformation.ItemID = INV_Item.ItemID and FA_AssetInformation.CompanyID = @CompanyID  And FA_AssetInformation.Status = 1 {0} order by PurchaseDate desc, (TagPrefix +'-'+ TagNumber) ";

                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(qry_DataByIdentity,filter), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    drToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return drToReturn;
        }

        public bool Insert()
        {
            bool result = false;
            SqlTransaction obj_SQL_Transaction = SqlHelper.GetTransaction("InsertAssetInformation");

            try
            {
                INV_Item obj_Item = new INV_Item(this.f_CompanyID, this.f_ItemID, null, obj_SQL_Transaction);

                

                int stockQuantity = this.NoOfUnits;
                decimal rate = Math.Round(this.Rate / stockQuantity, 0);
                decimal WrittenDownValue = Math.Round(this.WrittenDownValue / stockQuantity, 0);
                decimal OpeningDepreciation = this.OpeningDepreciation.HasValue ? Math.Round(this.OpeningDepreciation.Value / stockQuantity, 0) : 0;
                int itemTagNumber = Get_Max_TagNo(this.AssetTypeID, obj_SQL_Transaction);


                for (int itemQuantity = 1; itemQuantity <= stockQuantity; itemQuantity++)
                {
                    this.NoOfUnits = 1;
                    this.Rate = rate;
                    this.WrittenDownValue = WrittenDownValue;
                    this.OpeningDepreciation = OpeningDepreciation;
                    //this.TagNumber = Get_Max_TagNo(obj_Item.AutoLotAlphaPrefix, obj_SQL_Transaction).ToString();
                    this.TagNumber = (itemTagNumber+=1).ToString();
                    this.AssetInfoID = FA_AssetInformation.GetNewAssetInfoID(this.f_CompanyID, obj_SQL_Transaction);
                    this.Insert(obj_SQL_Transaction);
                }

                INV_StockTransactionDetail obj_StockDetail=null;
                if (this.f_StockDetailID != null)
                {
                    obj_StockDetail = new INV_StockTransactionDetail(this.f_StockDetailID.Value, null, obj_SQL_Transaction);
                }

                if (obj_StockDetail!=null && obj_StockDetail.IsDataLoaded)
                {
                    obj_StockDetail.IsIncurred = true;
                    obj_StockDetail.UpdateDataByKey(obj_SQL_Transaction);
                }

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
                    SqlHelper.CommitTransaction(obj_SQL_Transaction);
                else
                    SqlHelper.RollBackTransaction(obj_SQL_Transaction, "InsertAssetInformation");
            }

            return result;
        }

        public bool Insert_New()
        {
            bool result = false;
            SqlTransaction obj_SQL_Transaction = SqlHelper.GetTransaction("InsertAssetInformation");

            try
            {
                INV_Item obj_Item = new INV_Item(this.f_CompanyID, this.f_ItemID, null, obj_SQL_Transaction);



                int stockQuantity = this.NoOfUnits;
                decimal rate = Math.Round(this.Rate / stockQuantity, 0);
                decimal WrittenDownValue = Math.Round(this.WrittenDownValue / stockQuantity, 0);
                decimal OpeningDepreciation = this.OpeningDepreciation.HasValue ? Math.Round(this.OpeningDepreciation.Value / stockQuantity, 0) : 0;
                int? itemTagNumber = Get_Max_TagNo_New(this.CompanyID, this.AssetTypeID, Convert.ToInt32(!string.IsNullOrEmpty(this.SubCategory.ToString()) ? this.SubCategory.ToString() : "0"));


                for (int itemQuantity = 1; itemQuantity <= stockQuantity; itemQuantity++)
                {
                    this.NoOfUnits = 1;
                    this.Rate = rate;
                    this.WrittenDownValue = WrittenDownValue;
                    this.OpeningDepreciation = OpeningDepreciation;
                    if (this.TagNumber.Contains("-"))
                        this.TagNumber = this.TagNumber.Split('-')[0].ToString() + '-' + itemTagNumber.Value.ToString("D2");
                    else
                        this.TagNumber = "00" + '-' + itemTagNumber.Value.ToString("D2");
                    this.AssetInfoID = FA_AssetInformation.GetNewAssetInfoID(this.f_CompanyID, obj_SQL_Transaction);
                    this.Insert(obj_SQL_Transaction);
                }

                INV_StockTransactionDetail obj_StockDetail = null;
                if (this.f_StockDetailID != null)
                {
                    obj_StockDetail = new INV_StockTransactionDetail(this.f_StockDetailID.Value, null, obj_SQL_Transaction);
                }

                if (obj_StockDetail != null && obj_StockDetail.IsDataLoaded)
                {
                    obj_StockDetail.IsIncurred = true;
                    obj_StockDetail.UpdateDataByKey(obj_SQL_Transaction);
                }

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
                    SqlHelper.CommitTransaction(obj_SQL_Transaction);
                else
                    SqlHelper.RollBackTransaction(obj_SQL_Transaction, "InsertAssetInformation");
            }

            return result;
        }

        public static bool UpdateDisposedEntry(int AssetInfoID, int CompanyID)
        {
            bool result = false;
            SqlTransaction obj_SQL_Transaction = SqlHelper.GetTransaction("UpdateIsDisposed");

            try
            {
                FA_AssetInformation obj_FA_AssetInformation = null;



                obj_FA_AssetInformation = new FA_AssetInformation(AssetInfoID, CompanyID, null, obj_SQL_Transaction);

                obj_FA_AssetInformation.IsDisposed = true;

                obj_FA_AssetInformation.UpdateDataByKey(obj_SQL_Transaction);

                result = true;


            }
            catch (Exception ex)
            {
                
               SqlHelper.LogException(ex);
               throw ex;
            }

            if (result)
                SqlHelper.CommitTransaction(obj_SQL_Transaction);
            else
                SqlHelper.RollBackTransaction(obj_SQL_Transaction, "UpdateIsDisposed");

            return result;
        
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

        public static int Get_Max_TagNo(int AssetTypeID, SqlTransaction obj_SQL_Transaction = null)
        {
            int toReturn =0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@AssetTypeID", AssetTypeID);

                string qry_Max_TagNo = @" Select IsNull(count(TagNumber), 0)  From FA_AssetInformation Where AssetTypeID = @AssetTypeID And Status = 1 ";
                                
                toReturn = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, qry_Max_TagNo, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return toReturn;
        }



        public static DataTable DataRegisterDetail(int CompanyID, DateTime fromDate, DateTime ToDate, int? AssetTypeID, int? CostCenterID, int? ItemID, int? AssetInfoID, bool? isAll, bool? chk_FilterNonZero, bool? chk_disposal)
        {
            DataTable dt = null;
           StringBuilder  additionalParameters = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@FromDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(fromDate) + " 00:00:00"));
                sqlparams[2] = new SqlParameter("@ToDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(ToDate) + " 23:59:59"));
                if (AssetTypeID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.AssetTypeID = " + AssetTypeID.Value);

                if (CostCenterID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.CostCenterID = " + CostCenterID.Value);

                if (ItemID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.ItemID = " + ItemID.Value);

                if (AssetInfoID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.AssetInfoID = " + AssetInfoID.Value);

                if (chk_FilterNonZero.HasValue && chk_FilterNonZero.Value==true)
                    additionalParameters.Append(" AND isnull(FA_AssetInformation.CurrentValue,FA_AssetInformation.rate) <> 0 ");

                if(chk_disposal.HasValue && chk_disposal.Value==true)
                    additionalParameters.Append(" AND FA_AssetInformation.IsDisposed is not null ");

                //additionalParameters.Append(" and FA_SET_AssetType.AssetTypeID  <> 11 ");

                const string query_no_depreciated = @" union all select FA_SET_AssetType.AssetTypeName ,CONVERt(Date,@FromDate,8)as fromdate ,CONVERt(Date,@ToDate,8) as ToDate,Isnull(FA_AssetInformation.TagPrefix,'') + ' ' + FA_AssetInformation.TagNumber  as tagNumber,
FA_AssetInformation.ItemID,INV_Item.Description ItemName, FA_AssetInformation.StockGRNDate,0 as AsAtCost,0 as DepreciationRate,FA_AssetInformation.Rate Addition,FA_AssetInformation.WrittenDownValue wdv,'' as PeriodName ,0 as AsAtDepreciation,
null as lastdepreciationdate,'Hascombe' as Description,null as DepreciationFrom,0 as DepreciationAmount ,isnull(FA_AssetInformation.isdisposed,0) isdisposed
,isnull(FA_AssetInformation.ParentAssetInfoID,FA_AssetInformation.AssetInfoID)ParentAssetInfoID
,isnull(FA_AssetInformation.IsChild,0)IsChild,

--SET_CostCenter.CostCenterName
(select  scc.CostCenterName from SET_CostCenter scc where scc.CostCenterID = SET_CostCenter.BSCostCenterID) CostCenterName
from FA_AssetInformation,FA_SET_AssetType,INV_Item,SET_CostCenter
  where   FA_AssetInformation.AssetTypeID = FA_SET_AssetType.AssetTypeID   and FA_AssetInformation.ItemID =  INV_Item.ItemID
and SET_CostCenter.CostCenterID = FA_AssetInformation.CostCenterID
  and LastDepreciationDate is null  and FA_AssetInformation.CompanyID = @CompanyID  and FA_AssetInformation.StockGRNDate between @FromDate AND @ToDate {0} and FA_AssetInformation.opening is null



 union all select FA_SET_AssetType.AssetTypeName ,CONVERT(Date,@FromDate,8)as fromdate ,CONVERT(Date,@ToDate,8) as ToDate,
 Isnull(FA_AssetInformation.TagPrefix,'') + ' ' + FA_AssetInformation.TagNumber  as tagNumber,
FA_AssetInformation.ItemID,INV_Item.Description ItemName, FA_AssetInformation.StockGRNDate,
(case when CONVERT(Date,FA_AssetInformation.StockGRNDate,8) <> CONVERT(Date,@ToDate,8) then FA_AssetInformation.Rate else 0 end) as AsAtCost,
FA_AssetInformation.DepreciationRate,

(case when  CONVERT(Date,FA_AssetInformation.StockGRNDate,8)<> CONVERT(Date,@ToDate,8) then 0 else FA_AssetInformation.Rate end) Addition,
FA_DepreciationProcess.NWDV  wdv,

'' as PeriodName ,


isnull((select sum(DepreciationAmount) from  FA_DepreciationProcess Where AssetIdentityID = FA_AssetInformation.AssetInfoID
  and FA_DepreciationProcess.DepreciatedFrom < @FromDate) ,0)
  
  as AsAtDepreciation,


FA_AssetInformation.lastdepreciationdate,'Hascombe' as Description,FA_DepreciationProcess.DepreciatedFrom,


isnull((select sum(DepreciationAmount) from  FA_DepreciationProcess Where AssetIdentityID = FA_AssetInformation.AssetInfoID
  and FA_DepreciationProcess.DepreciatedFrom >= @FromDate and FA_DepreciationProcess.DepreciatedUpto <= @ToDate) ,0) as DepreciationAmount 


,isnull(FA_AssetInformation.isdisposed,0) isdisposed 
,isnull(FA_AssetInformation.ParentAssetInfoID,FA_AssetInformation.AssetInfoID)ParentAssetInfoID
,isnull(FA_AssetInformation.IsChild,0)IsChild,
--set_costcenter.CostCenterName
(select  scc.CostCenterName from SET_CostCenter scc where scc.CostCenterID = SET_CostCenter.BSCostCenterID) CostCenterName
from FA_AssetInformation



inner join FA_SET_AssetType on FA_AssetInformation.AssetTypeID = FA_SET_AssetType.AssetTypeID

inner join INV_Item on FA_AssetInformation.ItemID =  INV_Item.ItemID

left join FA_DepreciationProcess on FA_DepreciationProcess.AssetIdentityID = FA_AssetInformation.AssetInfoID
left join set_costcenter on set_costcenter.costcenterid = FA_AssetInformation.CostCenterID
  where 
    FA_AssetInformation.AssetTypeID = FA_SET_AssetType.AssetTypeID   and FA_AssetInformation.ItemID =  INV_Item.ItemID
  and FA_AssetInformation.CompanyID = @CompanyID {0}  
    and FA_AssetInformation.opening = 1
 and FA_DepreciationProcess.DepreciatedFrom >= @FromDate 
and FA_DepreciationProcess.DepreciatedUpto <= @ToDate


";

                string vCTEQuery = @"


 with vCTE as (
 
 select FA_AssetInformation.AssetInfoID
from FA_AssetInformation
inner join FA_SET_AssetType on FA_AssetInformation.AssetTypeID = FA_SET_AssetType.AssetTypeID
inner join INV_Item on FA_AssetInformation.ItemID =  INV_Item.ItemID
left join FA_DepreciationProcess on FA_DepreciationProcess.AssetIdentityID = FA_AssetInformation.AssetInfoID
 where 
  FA_AssetInformation.AssetTypeID = FA_SET_AssetType.AssetTypeID   and FA_AssetInformation.ItemID =  INV_Item.ItemID
  and FA_AssetInformation.CompanyID = @CompanyID  {0}  and FA_AssetInformation.opening = 1
 and FA_DepreciationProcess.DepreciatedFrom >= @FromDate 
and FA_DepreciationProcess.DepreciatedUpto <= @ToDate
)


                ";

                string notInOpening = @"
    
  select FA_SET_AssetType.AssetTypeName ,CONVERt(Date,@FromDate,8)as fromdate ,CONVERt(Date,@ToDate,8) as ToDate,Isnull(FA_AssetInformation.TagPrefix,'') + ' ' + FA_AssetInformation.TagNumber  as tagNumber,
	FA_AssetInformation.ItemID,INV_Item.Description ItemName, FA_AssetInformation.StockGRNDate,FA_AssetInformation.Rate as AsAtCost,0 as DepreciationRate,0 Addition,FA_AssetInformation.CurrentValue wdv,'' as PeriodName ,
	isnull((select sum(DepreciationAmount) from  FA_DepreciationProcess Where AssetIdentityID = FA_AssetInformation.AssetInfoID
  and FA_DepreciationProcess.DepreciatedFrom < @FromDate) ,0)
  
  as AsAtDepreciation,
  null as lastdepreciationdate,'Hascombe' as Description,null as DepreciationFrom,0 as DepreciationAmount ,isnull(FA_AssetInformation.isdisposed,0) isdisposed 

,isnull(FA_AssetInformation.ParentAssetInfoID,FA_AssetInformation.AssetInfoID)ParentAssetInfoID
,isnull(FA_AssetInformation.IsChild,0)IsChild,

  
  --SET_CostCenter.CostCenterName
    (select  scc.CostCenterName from SET_CostCenter scc where scc.CostCenterID = SET_CostCenter.BSCostCenterID) CostCenterName
  from FA_AssetInformation,FA_SET_AssetType,INV_Item,SET_CostCenter
  where   
  FA_AssetInformation.AssetTypeID = FA_SET_AssetType.AssetTypeID   and FA_AssetInformation.ItemID =  INV_Item.ItemID
    and SET_CostCenter.CostCenterID = FA_AssetInformation.CostCenterID
   
  and FA_AssetInformation.CompanyID = @CompanyID  
  and FA_AssetInformation.LastDepreciationDate is not null 
  and FA_AssetInformation.LastDepreciationDate <=@ToDate
   and FA_AssetInformation.opening = 1 
{0}
  and FA_AssetInformation.AssetInfoID not in (select AssetInfoID from vCTE)

union all

";

                
// union all select FA_SET_AssetType.AssetTypeName ,CONVERt(Date,@FromDate,8)as fromdate ,CONVERt(Date,@ToDate,8) as ToDate,
// Isnull(FA_AssetInformation.TagPrefix,'') + ' ' + FA_AssetInformation.TagNumber  as tagNumber,
//FA_AssetInformation.ItemID,INV_Item.Description ItemName, FA_AssetInformation.StockGRNDate,
//(case when CONVERt(Date,FA_AssetInformation.StockGRNDate,8) <> CONVERt(Date,@ToDate,8) then FA_AssetInformation.Rate else 0 end) as AsAtCost,
//FA_AssetInformation.DepreciationRate,
//(case when  CONVERt(Date,FA_AssetInformation.StockGRNDate,8)<> CONVERt(Date,@ToDate,8) then 0 else FA_AssetInformation.Rate end) Addition,
//FA_AssetInformation.WrittenDownValue wdv,'' as PeriodName ,
//(case when  CONVERt(Date,FA_AssetInformation.StockGRNDate,8) <> CONVERt(Date,@ToDate,8) then FA_AssetInformation.CurrentValue else 0 end) as AsAtDepreciation,
//null as lastdepreciationdate,'Hascombe' as Description,null as DepreciationFrom,
//(case when  CONVERt(Date,FA_AssetInformation.StockGRNDate,8) <> CONVERt(Date,@ToDate,8) then 0 else FA_AssetInformation.CurrentValue end)  as DepreciationAmount ,0 isdisposed from FA_AssetInformation,FA_SET_AssetType,INV_Item
//  where   FA_AssetInformation.AssetTypeID = FA_SET_AssetType.AssetTypeID   and FA_AssetInformation.ItemID =  INV_Item.ItemID
//  and LastDepreciationDate is null  and FA_AssetInformation.CompanyID = @CompanyID  and FA_AssetInformation.opening = 1

//                union all select FA_SET_AssetType.AssetTypeName ,CONVERt(Date,@FromDate,8)as fromdate ,CONVERt(Date,@ToDate,8) as ToDate,Isnull(FA_AssetInformation.TagPrefix,'') + ' ' + FA_AssetInformation.TagNumber  as tagNumber,
//FA_AssetInformation.ItemID,INV_Item.Description ItemName, FA_AssetInformation.StockGRNDate,0 as AsAtCost,FA_AssetInformation.DepreciationRate,FA_AssetInformation.Rate Addition,FA_AssetInformation.WrittenDownValue wdv,'' as PeriodName ,0 as AsAtDepreciation,
//null as lastdepreciationdate,'Hascombe' as Description,null as DepreciationFrom,FA_AssetInformation.CurrentValue as DepreciationAmount ,0 isdisposed from FA_AssetInformation,FA_SET_AssetType,INV_Item
//  where   FA_AssetInformation.AssetTypeID = FA_SET_AssetType.AssetTypeID   and FA_AssetInformation.ItemID =  INV_Item.ItemID
//  and LastDepreciationDate is null  and FA_AssetInformation.CompanyID = @CompanyID   {0} and FA_AssetInformation.opening =1

                //const string query_no_opening = @"  ";
//                const string query_no_depreciated = @" union all
//
//select 
//FA_SET_AssetType.AssetTypeName, 
//CONVERt(Date,@FromDate,8)as fromdate,
//CONVERt(Date,@ToDate,8) as ToDate,
//'' as tagNumber,
//INV_Item.ItemID,
//INV_Item.Description ItemName,
//INV_StockTransactionHeader.DocumentDate StockGRNDate,
//0 as AsAtCost,
//0 as DepreciationRate,
//INV_StockTransactionDetail.Amount Addition,
//INV_StockTransactionDetail.Amount WDV,
//'' as PeriodName,
//0 as AsAtDepreciation,
//null as LastDepreciationDate,
//'Hascombe' as Description,
//null as DepreciatedFrom,
//0 as DepreciationAmount,
//0 as isDisposed
//
//
// from 
//AP_PurchaseOrderDetail,
//AP_PurchaseOrderHeader,
//INV_StockTransactionDetail,
//INV_StockTransactionHeader,
//INV_Item,FA_SET_AssetType
//where 
//CapitalExpenseFlag =1 
//and AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID
//and AP_PurchaseOrderHeader.PeriodID = AP_PurchaseOrderDetail.PeriodID
//and AP_PurchaseOrderHeader.CompanyID = AP_PurchaseOrderDetail.CompanyID
//and AP_PurchaseOrderDetail.PeriodID = INV_StockTransactionDetail.PeriodID
//and AP_PurchaseOrderDetail.CompanyID = INV_StockTransactionDetail.CompanyID
//and AP_PurchaseOrderHeader.POHeaderID = INV_StockTransactionDetail.SourceHeaderID
//and AP_PurchaseOrderDetail.ItemID = INV_StockTransactionDetail.ItemID
//and INV_StockTransactionDetail.ItemID = INV_Item.ItemID
//and INV_StockTransactionDetail.StockTransactionID not in (select stockheaderid from FA_AssetInformation)
//and INV_StockTransactionDetail.transactiondetailid not in (select StockDetailID from FA_AssetInformation)
//and INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID
//and INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID
//and INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID
//and INV_Item.EquipmentTypeID = FA_SET_AssetType.AssetTypeID
//and INV_StockTransactionDetail.CompanyID =  @CompanyID
//--and INV_StockTransactionDetail.PeriodID =  @PeriodID
//and INV_StockTransactionHeader.DocumentDate between @FromDate AND @ToDate ";

                string Query_Final_For_Report = string.Empty;

                if (isAll.HasValue && isAll.Value == true)
                    Query_Final_For_Report = FnI.QueryResourceLibrary.FA.Transaction.FA_AssetInformation.Query_FARegisterDetailReport.Replace("{Query_Add_Purchase}", query_no_depreciated);
                else
                    Query_Final_For_Report = FnI.QueryResourceLibrary.FA.Transaction.FA_AssetInformation.Query_FARegisterDetailReport.Replace("{Query_Add_Purchase}", "");


                Query_Final_For_Report = Query_Final_For_Report.Replace("{vCTEQuery}", vCTEQuery);

                Query_Final_For_Report = Query_Final_For_Report.Replace("{notInOpening}", notInOpening);

                    //if (isOpening.HasValue && isOpening.Value == true)
                    //    Query_Final_For_Report = Query_FARegisterDetailReport.Replace("{Query_Add_Opening}", query_no_opening);
                    //else
                    //    Query_Final_For_Report = Query_FARegisterDetailReport.Replace("{Query_Add_Opening}", "");


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Final_For_Report, additionalParameters), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable DataRegisterDetail_SSML(int CompanyID, DateTime fromDate, DateTime ToDate, int? AssetTypeID, int? CostCenterID, int? ItemID, int? AssetInfoID, bool? isAll, bool? chk_FilterNonZero, bool? chk_disposal)
        {
            DataTable dt = null;
            StringBuilder additionalParameters = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@FromDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(fromDate) + " 00:00:00"));
                sqlparams[2] = new SqlParameter("@ToDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(ToDate) + " 23:59:59"));
                if (AssetTypeID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.AssetTypeID = " + AssetTypeID.Value);

                if (CostCenterID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.CostCenterID = " + CostCenterID.Value);

                if (ItemID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.ItemID = " + ItemID.Value);

                if (AssetInfoID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.AssetInfoID = " + AssetInfoID.Value);

                if (chk_FilterNonZero.HasValue && chk_FilterNonZero.Value == true)
                    additionalParameters.Append(" AND isnull(FA_AssetInformation.CurrentValue,FA_AssetInformation.rate) <> 0 ");

                if (chk_disposal.HasValue && chk_disposal.Value == true)
                    additionalParameters.Append(" AND FA_AssetInformation.IsDisposed is not null ");

                additionalParameters.Append(" and FA_SET_AssetType.AssetTypeID  <> 11 ");

                const string query_no_depreciated = @" union all select FA_SET_AssetType.AssetTypeName ,CONVERt(Date,@FromDate,8)as fromdate ,CONVERt(Date,@ToDate,8) as ToDate,Isnull(FA_AssetInformation.TagPrefix,'') + ' ' + FA_AssetInformation.TagNumber  as tagNumber,
FA_AssetInformation.ItemID,INV_Item.Description ItemName, FA_AssetInformation.StockGRNDate,0 as AsAtCost,0 as DepreciationRate,FA_AssetInformation.Rate Addition,FA_AssetInformation.WrittenDownValue wdv,'' as PeriodName ,0 as AsAtDepreciation,
null as lastdepreciationdate,'Hascombe' as Description,null as DepreciationFrom,0 as DepreciationAmount ,isnull(FA_AssetInformation.isdisposed,0) isdisposed
,isnull(FA_AssetInformation.ParentAssetInfoID,FA_AssetInformation.AssetInfoID)ParentAssetInfoID
,isnull(FA_AssetInformation.IsChild,0)IsChild,

--SET_CostCenter.CostCenterName
(select  scc.CostCenterName from SET_CostCenter scc where scc.CostCenterID = SET_CostCenter.BSCostCenterID) CostCenterName, '' as LocationName
from FA_AssetInformation,FA_SET_AssetType,INV_Item,SET_CostCenter
  where   FA_AssetInformation.AssetTypeID = FA_SET_AssetType.AssetTypeID   and FA_AssetInformation.ItemID =  INV_Item.ItemID
and SET_CostCenter.CostCenterID = FA_AssetInformation.CostCenterID
  and LastDepreciationDate is null  and FA_AssetInformation.CompanyID = @CompanyID  and FA_AssetInformation.StockGRNDate between @FromDate AND @ToDate {0} and FA_AssetInformation.opening is null



 union all select FA_SET_AssetType.AssetTypeName ,CONVERT(Date,@FromDate,8)as fromdate ,CONVERT(Date,@ToDate,8) as ToDate,
 Isnull(FA_AssetInformation.TagPrefix,'') + ' ' + FA_AssetInformation.TagNumber  as tagNumber,
FA_AssetInformation.ItemID,INV_Item.Description ItemName, FA_AssetInformation.StockGRNDate,
(case when CONVERT(Date,FA_AssetInformation.StockGRNDate,8) <> CONVERT(Date,@ToDate,8) then FA_AssetInformation.Rate else 0 end) as AsAtCost,
FA_AssetInformation.DepreciationRate,

(case when  CONVERT(Date,FA_AssetInformation.StockGRNDate,8)<> CONVERT(Date,@ToDate,8) then 0 else FA_AssetInformation.Rate end) Addition,
FA_DepreciationProcess.NWDV  wdv,

'' as PeriodName ,


isnull((select sum(DepreciationAmount) from  FA_DepreciationProcess Where AssetIdentityID = FA_AssetInformation.AssetInfoID
  and FA_DepreciationProcess.DepreciatedFrom < @FromDate) ,0)
  
  as AsAtDepreciation,


FA_AssetInformation.lastdepreciationdate,'Hascombe' as Description,FA_DepreciationProcess.DepreciatedFrom,


isnull((select sum(DepreciationAmount) from  FA_DepreciationProcess Where AssetIdentityID = FA_AssetInformation.AssetInfoID
  and FA_DepreciationProcess.DepreciatedFrom >= @FromDate and FA_DepreciationProcess.DepreciatedUpto <= @ToDate) ,0) as DepreciationAmount 


,isnull(FA_AssetInformation.isdisposed,0) isdisposed 
,isnull(FA_AssetInformation.ParentAssetInfoID,FA_AssetInformation.AssetInfoID)ParentAssetInfoID
,isnull(FA_AssetInformation.IsChild,0)IsChild,
--set_costcenter.CostCenterName
(select  scc.CostCenterName from SET_CostCenter scc where scc.CostCenterID = SET_CostCenter.BSCostCenterID) CostCenterName, SET_Location.LocationName 
from FA_AssetInformation



inner join FA_SET_AssetType on FA_AssetInformation.AssetTypeID = FA_SET_AssetType.AssetTypeID

inner join INV_Item on FA_AssetInformation.ItemID =  INV_Item.ItemID

left join FA_DepreciationProcess on FA_DepreciationProcess.AssetIdentityID = FA_AssetInformation.AssetInfoID
left join set_costcenter on set_costcenter.costcenterid = FA_AssetInformation.CostCenterID
left join SET_Location on SET_Location.LocationID = FA_DepreciationProcess.LocationID
  where 
    FA_AssetInformation.AssetTypeID = FA_SET_AssetType.AssetTypeID   and FA_AssetInformation.ItemID =  INV_Item.ItemID
  and FA_AssetInformation.CompanyID = @CompanyID {0}  
    and FA_AssetInformation.opening = 1
 and FA_DepreciationProcess.DepreciatedFrom >= @FromDate 
and FA_DepreciationProcess.DepreciatedUpto <= @ToDate


";

                string vCTEQuery = @"


 with vCTE as (
 
 select FA_AssetInformation.AssetInfoID
from FA_AssetInformation
inner join FA_SET_AssetType on FA_AssetInformation.AssetTypeID = FA_SET_AssetType.AssetTypeID
inner join INV_Item on FA_AssetInformation.ItemID =  INV_Item.ItemID
left join FA_DepreciationProcess on FA_DepreciationProcess.AssetIdentityID = FA_AssetInformation.AssetInfoID
 where 
  FA_AssetInformation.AssetTypeID = FA_SET_AssetType.AssetTypeID   and FA_AssetInformation.ItemID =  INV_Item.ItemID
  and FA_AssetInformation.CompanyID = @CompanyID  {0}  and FA_AssetInformation.opening = 1
 and FA_DepreciationProcess.DepreciatedFrom >= @FromDate 
and FA_DepreciationProcess.DepreciatedUpto <= @ToDate
)


                ";

                string notInOpening = @"
    
  select FA_SET_AssetType.AssetTypeName ,CONVERt(Date,@FromDate,8)as fromdate ,CONVERt(Date,@ToDate,8) as ToDate,Isnull(FA_AssetInformation.TagPrefix,'') + ' ' + FA_AssetInformation.TagNumber  as tagNumber,
	FA_AssetInformation.ItemID,INV_Item.Description ItemName, FA_AssetInformation.StockGRNDate,FA_AssetInformation.Rate + ISNULL(FA_AssetInformation.OpeningDepreciation,0) as AsAtCost,0 as DepreciationRate,0 Addition,FA_AssetInformation.CurrentValue wdv,'' as PeriodName ,
	isnull((select sum(DepreciationAmount) from  FA_DepreciationProcess Where AssetIdentityID = FA_AssetInformation.AssetInfoID
  and FA_DepreciationProcess.DepreciatedFrom < @FromDate) ,0)
  
  as AsAtDepreciation,
  null as lastdepreciationdate,'Hascombe' as Description,null as DepreciationFrom,0 as DepreciationAmount ,isnull(FA_AssetInformation.isdisposed,0) isdisposed 

,isnull(FA_AssetInformation.ParentAssetInfoID,FA_AssetInformation.AssetInfoID)ParentAssetInfoID
,isnull(FA_AssetInformation.IsChild,0)IsChild,

  
  --SET_CostCenter.CostCenterName
    (select  scc.CostCenterName from SET_CostCenter scc where scc.CostCenterID = SET_CostCenter.BSCostCenterID) CostCenterName, '' as LocationName
  from FA_AssetInformation,FA_SET_AssetType,INV_Item,SET_CostCenter
  where   
  FA_AssetInformation.AssetTypeID = FA_SET_AssetType.AssetTypeID   and FA_AssetInformation.ItemID =  INV_Item.ItemID
    and SET_CostCenter.CostCenterID = FA_AssetInformation.CostCenterID
   
  and FA_AssetInformation.CompanyID = @CompanyID  
  and FA_AssetInformation.LastDepreciationDate is not null 
  and FA_AssetInformation.LastDepreciationDate <=@ToDate
   and FA_AssetInformation.opening = 1 
{0}
  and FA_AssetInformation.AssetInfoID not in (select AssetInfoID from vCTE)

union all

";


                // union all select FA_SET_AssetType.AssetTypeName ,CONVERt(Date,@FromDate,8)as fromdate ,CONVERt(Date,@ToDate,8) as ToDate,
                // Isnull(FA_AssetInformation.TagPrefix,'') + ' ' + FA_AssetInformation.TagNumber  as tagNumber,
                //FA_AssetInformation.ItemID,INV_Item.Description ItemName, FA_AssetInformation.StockGRNDate,
                //(case when CONVERt(Date,FA_AssetInformation.StockGRNDate,8) <> CONVERt(Date,@ToDate,8) then FA_AssetInformation.Rate else 0 end) as AsAtCost,
                //FA_AssetInformation.DepreciationRate,
                //(case when  CONVERt(Date,FA_AssetInformation.StockGRNDate,8)<> CONVERt(Date,@ToDate,8) then 0 else FA_AssetInformation.Rate end) Addition,
                //FA_AssetInformation.WrittenDownValue wdv,'' as PeriodName ,
                //(case when  CONVERt(Date,FA_AssetInformation.StockGRNDate,8) <> CONVERt(Date,@ToDate,8) then FA_AssetInformation.CurrentValue else 0 end) as AsAtDepreciation,
                //null as lastdepreciationdate,'Hascombe' as Description,null as DepreciationFrom,
                //(case when  CONVERt(Date,FA_AssetInformation.StockGRNDate,8) <> CONVERt(Date,@ToDate,8) then 0 else FA_AssetInformation.CurrentValue end)  as DepreciationAmount ,0 isdisposed from FA_AssetInformation,FA_SET_AssetType,INV_Item
                //  where   FA_AssetInformation.AssetTypeID = FA_SET_AssetType.AssetTypeID   and FA_AssetInformation.ItemID =  INV_Item.ItemID
                //  and LastDepreciationDate is null  and FA_AssetInformation.CompanyID = @CompanyID  and FA_AssetInformation.opening = 1

                //                union all select FA_SET_AssetType.AssetTypeName ,CONVERt(Date,@FromDate,8)as fromdate ,CONVERt(Date,@ToDate,8) as ToDate,Isnull(FA_AssetInformation.TagPrefix,'') + ' ' + FA_AssetInformation.TagNumber  as tagNumber,
                //FA_AssetInformation.ItemID,INV_Item.Description ItemName, FA_AssetInformation.StockGRNDate,0 as AsAtCost,FA_AssetInformation.DepreciationRate,FA_AssetInformation.Rate Addition,FA_AssetInformation.WrittenDownValue wdv,'' as PeriodName ,0 as AsAtDepreciation,
                //null as lastdepreciationdate,'Hascombe' as Description,null as DepreciationFrom,FA_AssetInformation.CurrentValue as DepreciationAmount ,0 isdisposed from FA_AssetInformation,FA_SET_AssetType,INV_Item
                //  where   FA_AssetInformation.AssetTypeID = FA_SET_AssetType.AssetTypeID   and FA_AssetInformation.ItemID =  INV_Item.ItemID
                //  and LastDepreciationDate is null  and FA_AssetInformation.CompanyID = @CompanyID   {0} and FA_AssetInformation.opening =1

                //const string query_no_opening = @"  ";
                //                const string query_no_depreciated = @" union all
                //
                //select 
                //FA_SET_AssetType.AssetTypeName, 
                //CONVERt(Date,@FromDate,8)as fromdate,
                //CONVERt(Date,@ToDate,8) as ToDate,
                //'' as tagNumber,
                //INV_Item.ItemID,
                //INV_Item.Description ItemName,
                //INV_StockTransactionHeader.DocumentDate StockGRNDate,
                //0 as AsAtCost,
                //0 as DepreciationRate,
                //INV_StockTransactionDetail.Amount Addition,
                //INV_StockTransactionDetail.Amount WDV,
                //'' as PeriodName,
                //0 as AsAtDepreciation,
                //null as LastDepreciationDate,
                //'Hascombe' as Description,
                //null as DepreciatedFrom,
                //0 as DepreciationAmount,
                //0 as isDisposed
                //
                //
                // from 
                //AP_PurchaseOrderDetail,
                //AP_PurchaseOrderHeader,
                //INV_StockTransactionDetail,
                //INV_StockTransactionHeader,
                //INV_Item,FA_SET_AssetType
                //where 
                //CapitalExpenseFlag =1 
                //and AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID
                //and AP_PurchaseOrderHeader.PeriodID = AP_PurchaseOrderDetail.PeriodID
                //and AP_PurchaseOrderHeader.CompanyID = AP_PurchaseOrderDetail.CompanyID
                //and AP_PurchaseOrderDetail.PeriodID = INV_StockTransactionDetail.PeriodID
                //and AP_PurchaseOrderDetail.CompanyID = INV_StockTransactionDetail.CompanyID
                //and AP_PurchaseOrderHeader.POHeaderID = INV_StockTransactionDetail.SourceHeaderID
                //and AP_PurchaseOrderDetail.ItemID = INV_StockTransactionDetail.ItemID
                //and INV_StockTransactionDetail.ItemID = INV_Item.ItemID
                //and INV_StockTransactionDetail.StockTransactionID not in (select stockheaderid from FA_AssetInformation)
                //and INV_StockTransactionDetail.transactiondetailid not in (select StockDetailID from FA_AssetInformation)
                //and INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID
                //and INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID
                //and INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID
                //and INV_Item.EquipmentTypeID = FA_SET_AssetType.AssetTypeID
                //and INV_StockTransactionDetail.CompanyID =  @CompanyID
                //--and INV_StockTransactionDetail.PeriodID =  @PeriodID
                //and INV_StockTransactionHeader.DocumentDate between @FromDate AND @ToDate ";

                string Query_Final_For_Report = string.Empty;

                if (isAll.HasValue && isAll.Value == true)
                    Query_Final_For_Report = FnI.QueryResourceLibrary.FA.Transaction.FA_AssetInformation.Query_FARegisterDetailReport_SSML.Replace("{Query_Add_Purchase}", query_no_depreciated);
                else
                    Query_Final_For_Report = FnI.QueryResourceLibrary.FA.Transaction.FA_AssetInformation.Query_FARegisterDetailReport_SSML.Replace("{Query_Add_Purchase}", "");


                Query_Final_For_Report = Query_Final_For_Report.Replace("{vCTEQuery}", vCTEQuery);

                Query_Final_For_Report = Query_Final_For_Report.Replace("{notInOpening}", notInOpening);

                //if (isOpening.HasValue && isOpening.Value == true)
                //    Query_Final_For_Report = Query_FARegisterDetailReport.Replace("{Query_Add_Opening}", query_no_opening);
                //else
                //    Query_Final_For_Report = Query_FARegisterDetailReport.Replace("{Query_Add_Opening}", "");


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Final_For_Report, additionalParameters), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        
        public static DataTable FA_Disposal(int CompanyID, DateTime fromDate, DateTime ToDate, int? AssetTypeID, int? CostCenterID, int? ItemID, int? AssetInfoID)
        {
            DataTable dt = null;
            StringBuilder additionalParameters = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@FromDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(fromDate) + " 00:00:00"));
                sqlparams[2] = new SqlParameter("@ToDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(ToDate) + " 23:59:59"));
                if (AssetTypeID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.AssetTypeID = " + AssetTypeID.Value);

                if (CostCenterID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.CostCenterID = " + CostCenterID.Value);

                if (ItemID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.ItemID = " + ItemID.Value);

                if (AssetInfoID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.AssetInfoID = " + AssetInfoID.Value);

                //if (chk_FilterNonZero.HasValue && chk_FilterNonZero.Value == true)
                //    additionalParameters.Append(" AND isnull(FA_AssetInformation.CurrentValue,FA_AssetInformation.rate) <> 0 ");

                //if (chk_disposal.HasValue && chk_disposal.Value == true)
                //    additionalParameters.Append(" AND FA_AssetInformation.IsDisposed is not null ");

                //additionalParameters.Append(" and FA_SET_AssetType.AssetTypeID  <> 11 ");

                string Query_Final_For_Report = string.Empty;

                Query_Final_For_Report = FnI.QueryResourceLibrary.FA.Transaction.FA_AssetInformation.Query_FADisposalReport;

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Final_For_Report, additionalParameters), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        
        public static DataTable GetAllDataSummaryOfDisposals(string companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, SqlConnection objConnection = null)//, Nullable<int> StatusID
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                companyID = "(" + companyID + ")";

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.FA.Transaction.FA_AssetInformation.Query_SummaryOfDisposals).Replace("@CompanyID", companyID), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.FA.Transaction.FA_AssetInformation.Query_SummaryOfDisposals).Replace("@CompanyID", companyID), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds.Tables[0];
        }

        public static DataTable GetAllDataDisposalsOfFixedAssets(string companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, SqlConnection objConnection = null)//, Nullable<int> StatusID
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                companyID = "(" + companyID + ")";

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.FA.Transaction.FA_AssetInformation.Query_DisposalsOfFixedAssets).Replace("@CompanyID", companyID), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.FA.Transaction.FA_AssetInformation.Query_DisposalsOfFixedAssets).Replace("@CompanyID", companyID), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds.Tables[0];
        }


        public static DataTable DataRegisterDetailIntagible(int CompanyID, DateTime fromDate, DateTime ToDate, int? AssetTypeID, int? CostCenterID, int? ItemID, int? AssetInfoID, bool? isAll, bool? chk_FilterNonZero, bool? chk_disposal)
        {

            DataTable dt = null;
            StringBuilder additionalParameters = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@FromDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(fromDate) + " 00:00:00"));
                sqlparams[2] = new SqlParameter("@ToDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(ToDate) + " 23:59:59"));
                if (AssetTypeID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.AssetTypeID = " + AssetTypeID.Value);

                if (CostCenterID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.CostCenterID = " + CostCenterID.Value);

                if (ItemID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.ItemID = " + ItemID.Value);

                if (AssetInfoID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.AssetInfoID = " + AssetInfoID.Value);

                if (chk_FilterNonZero.HasValue && chk_FilterNonZero.Value == true)
                    additionalParameters.Append(" AND isnull(FA_AssetInformation.CurrentValue,FA_AssetInformation.rate) <> 0 ");

                if (chk_disposal.HasValue && chk_disposal.Value == true)
                    additionalParameters.Append(" AND FA_AssetInformation.IsDisposed is not null ");

                additionalParameters.Append(" and FA_SET_AssetType.AssetTypeID  = 11 ");
                

                const string query_no_depreciated = @" union all select FA_SET_AssetType.AssetTypeName ,CONVERt(Date,@FromDate,8)as fromdate ,CONVERt(Date,@ToDate,8) as ToDate,Isnull(FA_AssetInformation.TagPrefix,'') + ' ' + FA_AssetInformation.TagNumber  as tagNumber,
FA_AssetInformation.ItemID,INV_Item.Description ItemName, FA_AssetInformation.StockGRNDate,0 as AsAtCost,0 as DepreciationRate,FA_AssetInformation.Rate Addition,FA_AssetInformation.WrittenDownValue wdv,'' as PeriodName ,0 as AsAtDepreciation,
null as lastdepreciationdate,'Hascombe' as Description,null as DepreciationFrom,0 as DepreciationAmount ,isnull(FA_AssetInformation.isdisposed,0) isdisposed
,isnull(FA_AssetInformation.ParentAssetInfoID,FA_AssetInformation.AssetInfoID)ParentAssetInfoID
,isnull(FA_AssetInformation.IsChild,0)IsChild,

--SET_CostCenter.CostCenterName
(select  scc.CostCenterName from SET_CostCenter scc where scc.CostCenterID = SET_CostCenter.BSCostCenterID) CostCenterName
from FA_AssetInformation,FA_SET_AssetType,INV_Item,SET_CostCenter
  where   FA_AssetInformation.AssetTypeID = FA_SET_AssetType.AssetTypeID   and FA_AssetInformation.ItemID =  INV_Item.ItemID
and SET_CostCenter.CostCenterID = FA_AssetInformation.CostCenterID
  and LastDepreciationDate is null  and FA_AssetInformation.CompanyID = @CompanyID  and FA_AssetInformation.StockGRNDate between @FromDate AND @ToDate {0} and FA_AssetInformation.opening is null



 union all select FA_SET_AssetType.AssetTypeName ,CONVERT(Date,@FromDate,8)as fromdate ,CONVERT(Date,@ToDate,8) as ToDate,
 Isnull(FA_AssetInformation.TagPrefix,'') + ' ' + FA_AssetInformation.TagNumber  as tagNumber,
FA_AssetInformation.ItemID,INV_Item.Description ItemName, FA_AssetInformation.StockGRNDate,
(case when CONVERT(Date,FA_AssetInformation.StockGRNDate,8) <> CONVERT(Date,@ToDate,8) then FA_AssetInformation.Rate else 0 end) as AsAtCost,
FA_AssetInformation.DepreciationRate,

(case when  CONVERT(Date,FA_AssetInformation.StockGRNDate,8)<> CONVERT(Date,@ToDate,8) then 0 else FA_AssetInformation.Rate end) Addition,
FA_DepreciationProcess.NWDV  wdv,

'' as PeriodName ,


(isnull((select sum(DepreciationAmount) from  FA_DepreciationProcess Where AssetIdentityID = FA_AssetInformation.AssetInfoID
  and FA_DepreciationProcess.DepreciatedFrom < @FromDate) ,0) + ISNULL(FA_AssetInformation.OpeningDepreciation,0))
  
  as AsAtDepreciation,


FA_AssetInformation.lastdepreciationdate,'Hascombe' as Description,FA_DepreciationProcess.DepreciatedFrom,


isnull((select sum(DepreciationAmount) from  FA_DepreciationProcess Where AssetIdentityID = FA_AssetInformation.AssetInfoID
  and FA_DepreciationProcess.DepreciatedFrom >= @FromDate and FA_DepreciationProcess.DepreciatedUpto <= @ToDate) ,0) as DepreciationAmount 


,isnull(FA_AssetInformation.isdisposed,0) isdisposed 
,isnull(FA_AssetInformation.ParentAssetInfoID,FA_AssetInformation.AssetInfoID)ParentAssetInfoID
,isnull(FA_AssetInformation.IsChild,0)IsChild,
--set_costcenter.CostCenterName
(select  scc.CostCenterName from SET_CostCenter scc where scc.CostCenterID = SET_CostCenter.BSCostCenterID) CostCenterName
from FA_AssetInformation



inner join FA_SET_AssetType on FA_AssetInformation.AssetTypeID = FA_SET_AssetType.AssetTypeID

inner join INV_Item on FA_AssetInformation.ItemID =  INV_Item.ItemID

left join FA_DepreciationProcess on FA_DepreciationProcess.AssetIdentityID = FA_AssetInformation.AssetInfoID
left join set_costcenter on set_costcenter.costcenterid = FA_AssetInformation.CostCenterID
  where 
    FA_AssetInformation.AssetTypeID = FA_SET_AssetType.AssetTypeID   and FA_AssetInformation.ItemID =  INV_Item.ItemID
  and FA_AssetInformation.CompanyID = @CompanyID {0}   and FA_AssetInformation.opening = 1
 and FA_DepreciationProcess.DepreciatedFrom >= @FromDate 
and FA_DepreciationProcess.DepreciatedUpto <= @ToDate


";

                string vCTEQuery = @"


 with vCTE as (
 
 select FA_AssetInformation.AssetInfoID
from FA_AssetInformation
inner join FA_SET_AssetType on FA_AssetInformation.AssetTypeID = FA_SET_AssetType.AssetTypeID
inner join INV_Item on FA_AssetInformation.ItemID =  INV_Item.ItemID
left join FA_DepreciationProcess on FA_DepreciationProcess.AssetIdentityID = FA_AssetInformation.AssetInfoID
 where 
  FA_AssetInformation.AssetTypeID = FA_SET_AssetType.AssetTypeID   and FA_AssetInformation.ItemID =  INV_Item.ItemID
  and FA_AssetInformation.CompanyID = @CompanyID  {0}  and FA_AssetInformation.opening = 1
 and FA_DepreciationProcess.DepreciatedFrom >= @FromDate 
and FA_DepreciationProcess.DepreciatedUpto <= @ToDate
)


                ";

                string notInOpening = @"
    
  select FA_SET_AssetType.AssetTypeName ,CONVERt(Date,@FromDate,8)as fromdate ,CONVERt(Date,@ToDate,8) as ToDate,Isnull(FA_AssetInformation.TagPrefix,'') + ' ' + FA_AssetInformation.TagNumber  as tagNumber,
	FA_AssetInformation.ItemID,INV_Item.Description ItemName, FA_AssetInformation.StockGRNDate,FA_AssetInformation.Rate as AsAtCost,0 as DepreciationRate,0 Addition,FA_AssetInformation.CurrentValue wdv,'' as PeriodName ,
	(isnull((select sum(DepreciationAmount) from  FA_DepreciationProcess Where AssetIdentityID = FA_AssetInformation.AssetInfoID
  and FA_DepreciationProcess.DepreciatedFrom < @FromDate) ,0) + ISNULL(FA_AssetInformation.OpeningDepreciation,0))
  
  as AsAtDepreciation,
  null as lastdepreciationdate,'Hascombe' as Description,null as DepreciationFrom,0 as DepreciationAmount ,isnull(FA_AssetInformation.isdisposed,0) isdisposed 

,isnull(FA_AssetInformation.ParentAssetInfoID,FA_AssetInformation.AssetInfoID)ParentAssetInfoID
,isnull(FA_AssetInformation.IsChild,0)IsChild,

  
  --SET_CostCenter.CostCenterName
    (select  scc.CostCenterName from SET_CostCenter scc where scc.CostCenterID = SET_CostCenter.BSCostCenterID) CostCenterName
  from FA_AssetInformation,FA_SET_AssetType,INV_Item,SET_CostCenter
  where   
  FA_AssetInformation.AssetTypeID = FA_SET_AssetType.AssetTypeID   and FA_AssetInformation.ItemID =  INV_Item.ItemID
    and SET_CostCenter.CostCenterID = FA_AssetInformation.CostCenterID
   
  and FA_AssetInformation.CompanyID = @CompanyID  
  and FA_AssetInformation.LastDepreciationDate is not null 
  and FA_AssetInformation.LastDepreciationDate <=@ToDate
  and FA_AssetInformation.opening = 1 {0}
  and FA_AssetInformation.AssetInfoID not in (select AssetInfoID from vCTE)

union all

";


                // union all select FA_SET_AssetType.AssetTypeName ,CONVERt(Date,@FromDate,8)as fromdate ,CONVERt(Date,@ToDate,8) as ToDate,
                // Isnull(FA_AssetInformation.TagPrefix,'') + ' ' + FA_AssetInformation.TagNumber  as tagNumber,
                //FA_AssetInformation.ItemID,INV_Item.Description ItemName, FA_AssetInformation.StockGRNDate,
                //(case when CONVERt(Date,FA_AssetInformation.StockGRNDate,8) <> CONVERt(Date,@ToDate,8) then FA_AssetInformation.Rate else 0 end) as AsAtCost,
                //FA_AssetInformation.DepreciationRate,
                //(case when  CONVERt(Date,FA_AssetInformation.StockGRNDate,8)<> CONVERt(Date,@ToDate,8) then 0 else FA_AssetInformation.Rate end) Addition,
                //FA_AssetInformation.WrittenDownValue wdv,'' as PeriodName ,
                //(case when  CONVERt(Date,FA_AssetInformation.StockGRNDate,8) <> CONVERt(Date,@ToDate,8) then FA_AssetInformation.CurrentValue else 0 end) as AsAtDepreciation,
                //null as lastdepreciationdate,'Hascombe' as Description,null as DepreciationFrom,
                //(case when  CONVERt(Date,FA_AssetInformation.StockGRNDate,8) <> CONVERt(Date,@ToDate,8) then 0 else FA_AssetInformation.CurrentValue end)  as DepreciationAmount ,0 isdisposed from FA_AssetInformation,FA_SET_AssetType,INV_Item
                //  where   FA_AssetInformation.AssetTypeID = FA_SET_AssetType.AssetTypeID   and FA_AssetInformation.ItemID =  INV_Item.ItemID
                //  and LastDepreciationDate is null  and FA_AssetInformation.CompanyID = @CompanyID  and FA_AssetInformation.opening = 1

                //                union all select FA_SET_AssetType.AssetTypeName ,CONVERt(Date,@FromDate,8)as fromdate ,CONVERt(Date,@ToDate,8) as ToDate,Isnull(FA_AssetInformation.TagPrefix,'') + ' ' + FA_AssetInformation.TagNumber  as tagNumber,
                //FA_AssetInformation.ItemID,INV_Item.Description ItemName, FA_AssetInformation.StockGRNDate,0 as AsAtCost,FA_AssetInformation.DepreciationRate,FA_AssetInformation.Rate Addition,FA_AssetInformation.WrittenDownValue wdv,'' as PeriodName ,0 as AsAtDepreciation,
                //null as lastdepreciationdate,'Hascombe' as Description,null as DepreciationFrom,FA_AssetInformation.CurrentValue as DepreciationAmount ,0 isdisposed from FA_AssetInformation,FA_SET_AssetType,INV_Item
                //  where   FA_AssetInformation.AssetTypeID = FA_SET_AssetType.AssetTypeID   and FA_AssetInformation.ItemID =  INV_Item.ItemID
                //  and LastDepreciationDate is null  and FA_AssetInformation.CompanyID = @CompanyID   {0} and FA_AssetInformation.opening =1

                //const string query_no_opening = @"  ";
                //                const string query_no_depreciated = @" union all
                //
                //select 
                //FA_SET_AssetType.AssetTypeName, 
                //CONVERt(Date,@FromDate,8)as fromdate,
                //CONVERt(Date,@ToDate,8) as ToDate,
                //'' as tagNumber,
                //INV_Item.ItemID,
                //INV_Item.Description ItemName,
                //INV_StockTransactionHeader.DocumentDate StockGRNDate,
                //0 as AsAtCost,
                //0 as DepreciationRate,
                //INV_StockTransactionDetail.Amount Addition,
                //INV_StockTransactionDetail.Amount WDV,
                //'' as PeriodName,
                //0 as AsAtDepreciation,
                //null as LastDepreciationDate,
                //'Hascombe' as Description,
                //null as DepreciatedFrom,
                //0 as DepreciationAmount,
                //0 as isDisposed
                //
                //
                // from 
                //AP_PurchaseOrderDetail,
                //AP_PurchaseOrderHeader,
                //INV_StockTransactionDetail,
                //INV_StockTransactionHeader,
                //INV_Item,FA_SET_AssetType
                //where 
                //CapitalExpenseFlag =1 
                //and AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID
                //and AP_PurchaseOrderHeader.PeriodID = AP_PurchaseOrderDetail.PeriodID
                //and AP_PurchaseOrderHeader.CompanyID = AP_PurchaseOrderDetail.CompanyID
                //and AP_PurchaseOrderDetail.PeriodID = INV_StockTransactionDetail.PeriodID
                //and AP_PurchaseOrderDetail.CompanyID = INV_StockTransactionDetail.CompanyID
                //and AP_PurchaseOrderHeader.POHeaderID = INV_StockTransactionDetail.SourceHeaderID
                //and AP_PurchaseOrderDetail.ItemID = INV_StockTransactionDetail.ItemID
                //and INV_StockTransactionDetail.ItemID = INV_Item.ItemID
                //and INV_StockTransactionDetail.StockTransactionID not in (select stockheaderid from FA_AssetInformation)
                //and INV_StockTransactionDetail.transactiondetailid not in (select StockDetailID from FA_AssetInformation)
                //and INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID
                //and INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID
                //and INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID
                //and INV_Item.EquipmentTypeID = FA_SET_AssetType.AssetTypeID
                //and INV_StockTransactionDetail.CompanyID =  @CompanyID
                //--and INV_StockTransactionDetail.PeriodID =  @PeriodID
                //and INV_StockTransactionHeader.DocumentDate between @FromDate AND @ToDate ";

                string Query_Final_For_Report = string.Empty;

                if (isAll.HasValue && isAll.Value == true)
                    Query_Final_For_Report = FnI.QueryResourceLibrary.FA.Transaction.FA_AssetInformation.Query_FARegisterDetailReport.Replace("{Query_Add_Purchase}", query_no_depreciated);
                else
                    Query_Final_For_Report = FnI.QueryResourceLibrary.FA.Transaction.FA_AssetInformation.Query_FARegisterDetailReport.Replace("{Query_Add_Purchase}", "");


                Query_Final_For_Report = Query_Final_For_Report.Replace("{vCTEQuery}", vCTEQuery);

                Query_Final_For_Report = Query_Final_For_Report.Replace("{notInOpening}", notInOpening);

                //if (isOpening.HasValue && isOpening.Value == true)
                //    Query_Final_For_Report = Query_FARegisterDetailReport.Replace("{Query_Add_Opening}", query_no_opening);
                //else
                //    Query_Final_For_Report = Query_FARegisterDetailReport.Replace("{Query_Add_Opening}", "");


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Final_For_Report, additionalParameters), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        public static DataTable FARegisterWithMonthlyBalances(int CompanyID, DateTime fromDate, DateTime ToDate, int? AssetTypeID, int? CostCenterID, int? ItemID, int? AssetInfoID, bool? isAll, bool? chk_FilterNonZero, bool? chk_disposal)
        {
            DataTable dt = null;
            StringBuilder additionalParameters = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@FromDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(fromDate) + " 00:00:00"));
                sqlparams[2] = new SqlParameter("@ToDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(ToDate) + " 23:59:59"));
                if (AssetTypeID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.AssetTypeID = " + AssetTypeID.Value);

                if (CostCenterID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.CostCenterID = " + CostCenterID.Value);

                if (ItemID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.ItemID = " + ItemID.Value);

                if (AssetInfoID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.AssetInfoID = " + AssetInfoID.Value);

                if (chk_FilterNonZero.HasValue && chk_FilterNonZero.Value == true)
                    additionalParameters.Append(" AND isnull(FA_AssetInformation.CurrentValue,FA_AssetInformation.rate) <> 0 ");

                if (chk_disposal.HasValue && chk_disposal.Value == true)
                    additionalParameters.Append(" AND FA_AssetInformation.IsDisposed is not null ");

                additionalParameters.Append(" and FA_SET_AssetType.AssetTypeID  <> 11 ");
                #region Queries
                const string query_no_depreciated = @" union all select FA_SET_AssetType.AssetTypeName ,CONVERt(Date,@FromDate,8)as fromdate ,CONVERt(Date,@ToDate,8) as ToDate,Isnull(FA_AssetInformation.TagPrefix,'') + ' ' + FA_AssetInformation.TagNumber  as tagNumber,
FA_AssetInformation.ItemID,INV_Item.Description ItemName, FA_AssetInformation.StockGRNDate,0 as AsAtCost,0 as DepreciationRate,FA_AssetInformation.Rate Addition,FA_AssetInformation.WrittenDownValue wdv,'' as PeriodName ,0 as AsAtDepreciation,
null as lastdepreciationdate,'Hascombe' as Description,null as DepreciationFrom,0 as DepreciationAmount ,isnull(FA_AssetInformation.isdisposed,0) isdisposed
,isnull(FA_AssetInformation.ParentAssetInfoID,FA_AssetInformation.AssetInfoID)ParentAssetInfoID
,isnull(FA_AssetInformation.IsChild,0)IsChild,

--SET_CostCenter.CostCenterName
(select  scc.CostCenterName from SET_CostCenter scc where scc.CostCenterID = SET_CostCenter.BSCostCenterID) CostCenterName
from FA_AssetInformation,FA_SET_AssetType,INV_Item,SET_CostCenter
  where   FA_AssetInformation.AssetTypeID = FA_SET_AssetType.AssetTypeID   and FA_AssetInformation.ItemID =  INV_Item.ItemID
and SET_CostCenter.CostCenterID = FA_AssetInformation.CostCenterID
  and LastDepreciationDate is null  and FA_AssetInformation.CompanyID = @CompanyID  and FA_AssetInformation.StockGRNDate between @FromDate AND @ToDate {0} and FA_AssetInformation.opening is null



 union all select FA_SET_AssetType.AssetTypeName ,CONVERT(Date,@FromDate,8)as fromdate ,CONVERT(Date,@ToDate,8) as ToDate,
 Isnull(FA_AssetInformation.TagPrefix,'') + ' ' + FA_AssetInformation.TagNumber  as tagNumber,
FA_AssetInformation.ItemID,INV_Item.Description ItemName, FA_AssetInformation.StockGRNDate,
(case when CONVERT(Date,FA_AssetInformation.StockGRNDate,8) <> CONVERT(Date,@ToDate,8) then FA_AssetInformation.Rate else 0 end) as AsAtCost,
FA_AssetInformation.DepreciationRate,

(case when  CONVERT(Date,FA_AssetInformation.StockGRNDate,8)<> CONVERT(Date,@ToDate,8) then 0 else FA_AssetInformation.Rate end) Addition,
FA_DepreciationProcess.NWDV  wdv,

'' as PeriodName ,


isnull((select sum(DepreciationAmount) from  FA_DepreciationProcess Where AssetIdentityID = FA_AssetInformation.AssetInfoID
  and FA_DepreciationProcess.DepreciatedFrom < @FromDate) ,0)
  
  as AsAtDepreciation,


FA_AssetInformation.lastdepreciationdate,'Hascombe' as Description,FA_DepreciationProcess.DepreciatedFrom,


isnull((select sum(DepreciationAmount) from  FA_DepreciationProcess Where AssetIdentityID = FA_AssetInformation.AssetInfoID
  and FA_DepreciationProcess.DepreciatedFrom >= @FromDate and FA_DepreciationProcess.DepreciatedUpto <= @ToDate) ,0) as DepreciationAmount 


,isnull(FA_AssetInformation.isdisposed,0) isdisposed 
,isnull(FA_AssetInformation.ParentAssetInfoID,FA_AssetInformation.AssetInfoID)ParentAssetInfoID
,isnull(FA_AssetInformation.IsChild,0)IsChild,
--set_costcenter.CostCenterName
(select  scc.CostCenterName from SET_CostCenter scc where scc.CostCenterID = SET_CostCenter.BSCostCenterID) CostCenterName
from FA_AssetInformation



inner join FA_SET_AssetType on FA_AssetInformation.AssetTypeID = FA_SET_AssetType.AssetTypeID

inner join INV_Item on FA_AssetInformation.ItemID =  INV_Item.ItemID

left join FA_DepreciationProcess on FA_DepreciationProcess.AssetIdentityID = FA_AssetInformation.AssetInfoID
left join set_costcenter on set_costcenter.costcenterid = FA_AssetInformation.CostCenterID
  where 
    FA_AssetInformation.AssetTypeID = FA_SET_AssetType.AssetTypeID   and FA_AssetInformation.ItemID =  INV_Item.ItemID
  and FA_AssetInformation.CompanyID = @CompanyID {0}  
    and FA_AssetInformation.opening = 1
 and FA_DepreciationProcess.DepreciatedFrom >= @FromDate 
and FA_DepreciationProcess.DepreciatedUpto <= @ToDate


";

                string vCTEQuery = @"


 with vCTE as (
 
 select FA_AssetInformation.AssetInfoID
from FA_AssetInformation
inner join FA_SET_AssetType on FA_AssetInformation.AssetTypeID = FA_SET_AssetType.AssetTypeID
inner join INV_Item on FA_AssetInformation.ItemID =  INV_Item.ItemID
left join FA_DepreciationProcess on FA_DepreciationProcess.AssetIdentityID = FA_AssetInformation.AssetInfoID
 where 
  FA_AssetInformation.AssetTypeID = FA_SET_AssetType.AssetTypeID   and FA_AssetInformation.ItemID =  INV_Item.ItemID
  and FA_AssetInformation.CompanyID = @CompanyID  {0}  and FA_AssetInformation.opening = 1
 and FA_DepreciationProcess.DepreciatedFrom >= @FromDate 
and FA_DepreciationProcess.DepreciatedUpto <= @ToDate
)


                ";

                string notInOpening = @"
    
  select FA_SET_AssetType.AssetTypeName ,CONVERt(Date,@FromDate,8)as fromdate ,CONVERt(Date,@ToDate,8) as ToDate,Isnull(FA_AssetInformation.TagPrefix,'') + ' ' + FA_AssetInformation.TagNumber  as tagNumber,
	FA_AssetInformation.ItemID,INV_Item.Description ItemName, FA_AssetInformation.StockGRNDate,FA_AssetInformation.Rate as AsAtCost,0 as DepreciationRate,0 Addition,FA_AssetInformation.CurrentValue wdv,'' as PeriodName ,
	isnull((select sum(DepreciationAmount) from  FA_DepreciationProcess Where AssetIdentityID = FA_AssetInformation.AssetInfoID
  and FA_DepreciationProcess.DepreciatedFrom < @FromDate) ,0)
  
  as AsAtDepreciation,
  null as lastdepreciationdate,'Hascombe' as Description,null as DepreciationFrom,0 as DepreciationAmount ,isnull(FA_AssetInformation.isdisposed,0) isdisposed 

,isnull(FA_AssetInformation.ParentAssetInfoID,FA_AssetInformation.AssetInfoID)ParentAssetInfoID
,isnull(FA_AssetInformation.IsChild,0)IsChild,

  
  --SET_CostCenter.CostCenterName
    (select  scc.CostCenterName from SET_CostCenter scc where scc.CostCenterID = SET_CostCenter.BSCostCenterID) CostCenterName
  from FA_AssetInformation,FA_SET_AssetType,INV_Item,SET_CostCenter
  where   
  FA_AssetInformation.AssetTypeID = FA_SET_AssetType.AssetTypeID   and FA_AssetInformation.ItemID =  INV_Item.ItemID
    and SET_CostCenter.CostCenterID = FA_AssetInformation.CostCenterID
   
  and FA_AssetInformation.CompanyID = @CompanyID  
  and FA_AssetInformation.LastDepreciationDate is not null 
  and FA_AssetInformation.LastDepreciationDate <=@ToDate
  -- and FA_AssetInformation.opening = 1 
    AND FA_AssetInformation.AssetInfoID   not in (Select AssetIdentityID from   FA_DepreciationProcess where DepreciatedFrom  between @FromDate AND @ToDate  Or FA_DepreciationProcess.DepreciatedUpTo  between @FromDate AND @ToDate)
{0}
  and FA_AssetInformation.AssetInfoID not in (select AssetInfoID from vCTE)

union all   ";
#endregion
                string Query_Final_For_Report = string.Empty;

                if (isAll.HasValue && isAll.Value == true)
                    Query_Final_For_Report = FnI.QueryResourceLibrary.FA.Transaction.FA_AssetInformation.Query_FARegisterDetailReport.Replace("{Query_Add_Purchase}", query_no_depreciated);
                else
                    Query_Final_For_Report = FnI.QueryResourceLibrary.FA.Transaction.FA_AssetInformation.Query_FARegisterDetailReport.Replace("{Query_Add_Purchase}", "");


                Query_Final_For_Report = Query_Final_For_Report.Replace("{vCTEQuery}", vCTEQuery);

                Query_Final_For_Report = Query_Final_For_Report.Replace("{notInOpening}", notInOpening);

                //if (isOpening.HasValue && isOpening.Value == true)
                //    Query_Final_For_Report = Query_FARegisterDetailReport.Replace("{Query_Add_Opening}", query_no_opening);
                //else
                //    Query_Final_For_Report = Query_FARegisterDetailReport.Replace("{Query_Add_Opening}", "");


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Final_For_Report, additionalParameters), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable SummaryOfDisposal(int companyID, int periodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo)//, Nullable<int> StatusID
        {
            DataTable dt = null;
            DataSet ds = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@FromDate", dateFrom);
                sqlparams[2] = new SqlParameter("@ToDate", dateTo);
                //if (objConnection == null)
                ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.FA.Transaction.FA_AssetInformation.Query_SummaryOfDisposal, sqlparams);
                //else
                //    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.FA.Transaction.FA_AssetInformation.Query_SummaryOfDisposals, sqlparams);
                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return ds.Tables[0];
        }


        public static object GetDetailofAdditionsToFixedAssets(Nullable<int> periodID, Nullable<int> CurrentFiscalID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, string companyID)
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@FromDate", Date_From);
                sqlparams[4] = new SqlParameter("@ToDate", Date_To);

                companyID = "(" + companyID + ")";

                //if (objConnection == null)
                //    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_PODetailImported, sqlparams);
                //else
                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.FA.Transaction.FA_AssetInformation.Query_DetailAdditionsFixedAssets).Replace("@CompanyID", companyID), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return ds.Tables[0];
        }

        public static object GetDetailForSummaryAdditionsToFixedAssets(Nullable<int> periodID, Nullable<int> CurrentFiscalID, Nullable<DateTime> Date_To, string companyID)
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);                
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                companyID = "(" + companyID + ")";

                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.FA.Transaction.FA_AssetInformation.Query_SummaryAdditionsToFixedAssets).Replace("@CompanyID", companyID), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return ds.Tables[0];
        }


        public static DataTable GetData_AssetInformation(Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@FromDate", fromDate);
                sqlparams[1] = new SqlParameter("@ToDate", toDate);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, FnI.QueryResourceLibrary.FA.Transaction.FA_AssetInformation.Query_GetData_ForAssetInformation, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.FA.Transaction.FA_AssetInformation.Query_GetData_ForAssetInformation, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.FA.Transaction.FA_AssetInformation.Query_GetData_ForAssetInformation, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];

                return dt;
            }

            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
        }

        public static DataTable GetDataAreaWiseDetailofLandReport(Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@FromDate", fromDate);
                sqlparams[1] = new SqlParameter("@ToDate", toDate);

                DataSet ds = null;


                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, FnI.QueryResourceLibrary.FA.Transaction.FA_AssetInformation.Query_GetData_AreaWiseDetailofLandReport, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.FA.Transaction.FA_AssetInformation.Query_GetData_AreaWiseDetailofLandReport, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.FA.Transaction.FA_AssetInformation.Query_GetData_AreaWiseDetailofLandReport, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];

                return dt;
            }

            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
        }


        public static DataTable GetDataAreaWiseDetailofLandCorporateFarms_Report(Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@FromDate", fromDate);
                sqlparams[1] = new SqlParameter("@ToDate", toDate);

                DataSet ds = null;


                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, FnI.QueryResourceLibrary.FA.Transaction.FA_AssetInformation.Query_GetData_AreaWiseDetailofLandCorporateFarmsReport, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.FA.Transaction.FA_AssetInformation.Query_GetData_AreaWiseDetailofLandCorporateFarmsReport, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.FA.Transaction.FA_AssetInformation.Query_GetData_AreaWiseDetailofLandCorporateFarmsReport, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];

                return dt;
            }

            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
        }

        public static DataTable Get_LastDepreciateDateByKey(int CompanyID, int AssetTypeID, int ItemID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@AssetTypeID", AssetTypeID);
                sqlparams[2] = new SqlParameter("@ItemID", ItemID);

                const string Query = @"SELECT TOP 1 AssetInfoID, LastDepreciationDate, StockGRNDate FROM FA_AssetInformation WHERE FA_AssetInformation.Status = 1 AND FA_AssetInformation.CompanyID = @CompanyID AND FA_AssetInformation.AssetTypeID = @AssetTypeID AND FA_AssetInformation.ItemID = @ItemID ORDER BY AssetInfoID DESC";

                dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataTable Get_DataByStockData(int CompanyID, int StockHeaderID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@StockHeaderID", StockHeaderID);

                const string Query = @"
SELECT DISTINCT FA_AssetInformation.*, CASE WHEN FA_DepreciationProcess.DepreciationProcessID IS NOT NULL THEN 1 ELSE NULL END AS DepreciationProcessID, 
CASE WHEN FA_DepreciationProcess.IsPosted IS NOT NULL THEN 1 ELSE NULL END AS IsPosted FROM FA_AssetInformation
LEFT JOIN FA_DepreciationProcess ON FA_DepreciationProcess.CompanyID = FA_AssetInformation.CompanyID
AND FA_DepreciationProcess.AssetIdentityID = FA_AssetInformation.AssetInfoID AND FA_DepreciationProcess.Status = 1
WHERE FA_AssetInformation.Status = 1 AND FA_AssetInformation.CompanyID = @CompanyID AND FA_AssetInformation.StockHeaderID = @StockHeaderID
";

                dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static Nullable<int> Get_Max_TagNo_New(int companyID, int assetTypeID, int SubCategory)
        {
            Nullable<int> Result = null;

            const string query_max_assettagnumber = @"
SELECT FORMAT(ISNULL(MAX(CONVERT(int,TagNo)) + 1,1) , '00') AS TagNo FROM ( 
SELECT CASE WHEN CHARINDEX('-', TagNumber) > 0 THEN RIGHT(TagNumber, LEN(TagNumber) - CHARINDEX('-', TagNumber)) END AS TagNo
FROM fa_assetinformation
where SubCategory = @SubCategory and AssetTypeID = @AssetTypeID and companyid = @CompanyID and FA_AssetInformation.Status = 1
)Fintab";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@AssetTypeID", assetTypeID);
                sqlparams[2] = new SqlParameter("@SubCategory", SubCategory);

                Result = Convert.ToInt32(SqlHelper.ExecuteDataset(CommandType.Text, query_max_assettagnumber, sqlparams).Tables[0].Rows[0]["TagNo"].ToString());
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return Result;
        }

        #endregion
    }
}
