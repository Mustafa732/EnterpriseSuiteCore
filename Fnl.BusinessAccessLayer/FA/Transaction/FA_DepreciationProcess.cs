using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;

using Common.Utils;

using DAL.DataAccessLayer;

using FnI.BusinessAccessLayer.GL.Transaction;
using FnI.BusinessAccessLayer.Setup;

namespace FnI.BusinessAccessLayer.FA.Transaction
{
    public partial class FA_DepreciationProcess
    {
        #region Constants

        const string Query_Delete_ByAssetTypeID = @" Update [FA_DepreciationProcess] Set [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [CompanyID] = @CompanyID and ([AssetTypeID] = @AssetTypeID or  @AssetTypeID is Null)";


        const string Query_TransactionHistoryReport = @" 
select 
    FA_DepreciationProcess.AssetTypeID
    ,Isnull(FA_AssetInformation.TagPrefix,'') + '' + FA_AssetInformation.TagNumber as Tag
    ,
    INV_Item.Description as ItemName, 
    SET_Location.LocationName,
    SET_CostCenter.CostCenterName,
    FA_AssetInformation.StockGRNDate as GRNDate,
  FA_DepreciationProcess.DepreciationAmount,
    FA_AssetInformation.Rate as AssetRate
   ,FA_AssetInformation.DepreciationRate
    , FA_DepreciationProcess.NWDV
    , FA_DepreciationProcess.WDV
      , isnull((SELECT TOP 1 FA_AssetInformation.WrittenDownValue
     FROM FA_AssetInformation q
     WHERE  q.StockGRNDate<FA_AssetInformation.StockGRNDate),0) as Addition
  -- , Case When FA_DepreciationProcess.DepreciatedFrom = FA_AssetInformation.StockGRNDate Then FA_AssetInformation.WrittenDownValue else 0 End As Addition, 
   ,FA_DepreciationProcess.DepreciatedFrom, 
    FA_DepreciationProcess.DepreciatedUpto, FA_AssetTypePolicy.GLCodeCombinationID, 
    FA_DepreciationProcess.SalvageValue,
     FA_SET_AssetType.AssetTypeName, 
    case FA_DepreciationProcess.ProcessTypeID when 1 then 'Account Process' when 2 then 'Tax Process' end as ProcessType, 
    case FA_DepreciationProcess.DepreciationMethodID when 1 then 'Declining_Balance' when 2 then 'Straight_Line' end as MethodName, 
    case FA_DepreciationProcess.PeriodStatusID when 1 then 'Day' when 2 then 'Month' when 3 then 'Quarter' when 4 then 'Year' end as PeriodName, 
    FA_DepreciationProcess.ProcessTypeID, FA_DepreciationProcess.DepreciationMethodID, FA_DepreciationProcess.PeriodStatusID 
from FA_DepreciationProcess 
   inner JOIN FA_AssetInformation On FA_AssetInformation.AssetInfoID=FA_DepreciationProcess.AssetIdentityID
      AND FA_AssetInformation.CompanyID =FA_DepreciationProcess.CompanyID
    INNER JOIN FA_AssetTypePolicy ON FA_AssetTypePolicy.AssetTypePolicyID = FA_DepreciationProcess.ProcessTypeID 
        AND FA_AssetTypePolicy.CompanyID = FA_DepreciationProcess.CompanyID AND FA_AssetTypePolicy.Status = 1 
    INNER JOIN FA_SET_AssetType ON FA_SET_AssetType.AssetTypeID = FA_DepreciationProcess.AssetTypeID AND FA_SET_AssetType.CompanyID = FA_DepreciationProcess.CompanyID 
        AND FA_SET_AssetType.Status = 1 
        --Inner JOIN FA_AssetInformation On FA_AssetInformation.AssetTypeID=FA_DepreciationProcess.AssetTypeID
        -- AND FA_AssetInformation.CompanyID =FA_DepreciationProcess.CompanyID
     
     INNER JOIN INV_Item  ON INV_Item.ItemID=FA_AssetInformation.ItemID 
--AND INV_Item.CompanyID =FA_AssetInformation.CompanyID
     Left JOIN SET_Location ON SET_Location.LocationID =FA_AssetInformation.LocationID 
      AND SET_Location.Status=1
      LEft JOIN SET_CostCenter ON SET_CostCenter.CostCenterID= FA_AssetInformation.CostCenterID
      and SET_CostCenter.Status=1

where FA_DepreciationProcess.Status = 1 and FA_DepreciationProcess.CompanyID = @CompanyID 
  And (FA_DepreciationProcess.DepreciatedFrom between @FromDate AND @ToDate  Or FA_DepreciationProcess.DepreciatedUpTo  between @FromDate AND @ToDate) 
    {0}
   ORDER BY FA_DepreciationProcess.DepreciatedUpto ";

        const string Query_Department_History_Report = @"
select FA_DepreciationProcess.DepreciationProcessID, FA_DepreciationProcess.DepreciationAmount,FA_DepreciationProcess.NWDV,FA_DepreciationProcess.WDV,
FA_DepreciationProcess.DepreciationRate,FA_DepreciationProcess.DateOfProcess,FA_DepreciationProcess.FAUpTo,
FA_DepreciationProcess.AssetTag,FA_DepreciationProcess.CodeCombinationID,FA_AT.ResidualValue,INV_Item.Description,
case FA_DepreciationProcess.ProcessTypeID when 1 then 'Account Process' when 2 then 'Tax Process' end as ProcessType,
case FA_DepreciationProcess.DepreciationMethodID when 1 then 'Declining_Balance' when 2 then 'Straight_Line' end as MethodName,
case FA_DepreciationProcess.PeriodStatusID when 1 then 'Day' when 2 then 'Month' when 3 then 'Quarter' when 4 then 'Year' end as PeriodName,
FA_DepreciationProcess.ProcessTypeID,FA_DepreciationProcess.DepreciationMethodID,
FA_DepreciationProcess.PeriodStatusID,FA_DepreciationProcess.ParentAssetNumber,FA_AT.OwnerDepartmentID,SD.DepartmentID,SD.DepartmentName

from FA_DepreciationProcess

inner join FA_AssetTransaction FA_AT
on FA_AT.AssetTransactionID = FA_DepreciationProcess.AssetTransactionID
and FA_AT.CompanyID = FA_DepreciationProcess.CompanyID
and FA_AT.Status = 1

inner join INV_Item on INV_Item.ItemID = FA_AT.ItemID
--and INV_Item.CompanyID = FA_AT.CompanyID
and INV_Item.Status = 1

Left Join SET_Department SD
ON SD.DepartmentID=FA_AT.OwnerDepartmentID


where FA_DepreciationProcess.Status = 1
and FA_DepreciationProcess.CompanyID = @CompanyID
{0}";

        const string Query_GetDataForDepreciationForm = @"
SELECT FA_DepreciationProcess.DepreciationProcessID, FA_SET_AssetType.AssetTypeID, FA_SET_AssetType.AssetTypeName,
FA_AssetInformation.AssetInfoID, INV_Item.Description, FA_DepreciationProcess.DepreciatedFrom, FA_DepreciationProcess.DepreciatedUpto,
FA_DepreciationProcess.WDV, FA_DepreciationProcess.DepreciationAmount, NWDV, CONVERT(bit,1) AS IsChecked, ROW_NUMBER() OVER(PARTITION BY AssetInfoID ORDER BY DepreciationProcessID DESC) AS RowNumber
FROM FA_DepreciationProcess
INNER JOIN FA_SET_AssetType ON FA_SET_AssetType.AssetTypeID = FA_DepreciationProcess.AssetTypeID
AND FA_SET_AssetType.CompanyID = FA_DepreciationProcess.CompanyID AND FA_SET_AssetType.Status = 1
INNER JOIN FA_AssetInformation ON FA_AssetInformation.AssetInfoID = FA_DepreciationProcess.AssetIdentityID
AND FA_AssetInformation.CompanyID = FA_DepreciationProcess.CompanyID AND FA_AssetInformation.Status = 1 and FA_AssetInformation.isdisposed is null
INNER JOIN INV_Item ON INV_Item.ItemID = FA_AssetInformation.ItemID AND INV_Item.CompanyID = FA_AssetInformation.CompanyID
AND INV_Item.Status = 1
";

        #endregion

        #region Methods

        public static DataTable DepreciationReport(int companyID, DateTime fromDate,DateTime ToDate,int? AssetTypeID = null, int? CostCenterID = null, int? ItemID = null)
        {
            DataTable result = null;
            StringBuilder additionalParameters = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@FromDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(fromDate) + " 00:00:00"));
                sqlparams[2] = new SqlParameter("@ToDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(ToDate) + " 23:59:59"));
                  if(AssetTypeID.HasValue)
                      additionalParameters.Append(" AND FA_DepreciationProcess.AssetTypeID = " + AssetTypeID.Value);
              
                if(CostCenterID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.CostCenterID = " + CostCenterID.Value);
                
                if(ItemID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.ItemID = " + ItemID.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_TransactionHistoryReport, additionalParameters), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    result = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataTable DepartmentHistoryReport(int companyID, int DeprtmentID, string dateFrom = null, string dateTo = null)
        {
            DataTable result = null;
            StringBuilder additionalParameters = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (dateFrom != string.Empty && dateTo != string.Empty)
                {
                    additionalParameters.Append(" AND CAST(FA_DepreciationProcess.DateOfProcess AS Date) BETWEEN '" + dateFrom + "' AND '" + dateTo + "' ");
                }
                if (DeprtmentID > 0)
                {
                    additionalParameters.Append("AND FA_AT.OwnerDepartmentID = " + DeprtmentID);
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Department_History_Report, additionalParameters), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    result = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public bool InsertWithIdentity(List<FA_DepreciationProcess> lst_Depreciations, List<FA_AssetInformation> lst_Identities)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertDepreciationIdentities");

            try
            {
                foreach (FA_DepreciationProcess objDepreciation in lst_Depreciations)
                {
                    objDepreciation.DepreciationProcessID = FA_DepreciationProcess.GetNewDepreciationProcessID(objDepreciation.f_CompanyID, sqltrans);
                    objDepreciation.Insert(sqltrans);
                }

                foreach (FA_AssetInformation objIdentity in lst_Identities)
                    objIdentity.UpdateDataByKey(sqltrans);

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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertDepreciationIdentities");
            }

            return result;
        }

        public DataTable GetDepreciationsForPosting(int companyID, DateTime fromDate, DateTime toDate, SqlConnection objSQL_Connection)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@FromDate", fromDate);
                sqlparams[2] = new SqlParameter("@ToDate", toDate);

                string Query_DepreciationsForPosting = @"
Select FA_DepreciationProcess.DepreciationProcessID, FA_DepreciationProcess.DepreciationAmount, FA_DepreciationProcess.DepreciatedUpto, 
    FA_Set_AssetType.AccDeprAccountCode As CreditAccountCode, FA_AssetTypePolicy.GLCodeCombinationID As DebitAccountCode, FA_DepreciationProcess.CostCenterID, 
    FA_DepreciationProcess.DepartmentID, FA_DepreciationProcess.AssetTypeID, FA_Set_AssetType.AssetTypeName ,  FA_AssetInformation.ItemID
From FA_DepreciationProcess 
	Inner Join FA_Set_AssetType On FA_Set_AssetType.CompanyID = FA_DepreciationProcess.CompanyID And FA_Set_AssetType.AssetTypeID = FA_DepreciationProcess.AssetTypeID 
	Inner Join FA_AssetTypePolicy On FA_AssetTypePolicy.CompanyID = FA_DepreciationProcess.CompanyID 
    And FA_AssetTypePolicy.AssetTypeID = FA_DepreciationProcess.AssetTypeID 
    And FA_AssetTypePolicy.AssetClassification = FA_DepreciationProcess.ProcessTypeID 
    inner join FA_AssetInformation on FA_AssetInformation.AssetInfoID =  FA_DepreciationProcess.AssetIdentityID
Where FA_DepreciationProcess.CompanyID = @CompanyID And FA_DepreciationProcess.DepreciatedUpto Between @FromDate And @ToDate and FA_DepreciationProcess.isposted <> 1 ";

                DataSet ds = SqlHelper.ExecuteDataset(objSQL_Connection, CommandType.Text, Query_DepreciationsForPosting, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public DataTable GetDepreciationsForPostingDistinct(int companyID, DateTime fromDate, DateTime toDate, SqlConnection objSQL_Connection)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@FromDate", fromDate);
                sqlparams[2] = new SqlParameter("@ToDate", toDate);

                string Query_DepreciationsForPosting = @"
Select distinct FA_DepreciationProcess.AssetTypeID,FA_DepreciationProcess.CostCenterID  
From FA_DepreciationProcess 
	Inner Join FA_Set_AssetType On FA_Set_AssetType.CompanyID = FA_DepreciationProcess.CompanyID And FA_Set_AssetType.AssetTypeID = FA_DepreciationProcess.AssetTypeID 
	Inner Join FA_AssetTypePolicy On FA_AssetTypePolicy.CompanyID = FA_DepreciationProcess.CompanyID 
    And FA_AssetTypePolicy.AssetTypeID = FA_DepreciationProcess.AssetTypeID 
    And FA_AssetTypePolicy.AssetClassification = FA_DepreciationProcess.ProcessTypeID 
    inner join FA_AssetInformation on FA_AssetInformation.AssetInfoID =  FA_DepreciationProcess.AssetIdentityID
Where FA_DepreciationProcess.CompanyID = @CompanyID And FA_DepreciationProcess.DepreciatedUpto Between @FromDate And @ToDate and FA_DepreciationProcess.isposted <> 1 and FA_AssetInformation.isdisposed is null";

                DataSet ds = SqlHelper.ExecuteDataset(objSQL_Connection, CommandType.Text, Query_DepreciationsForPosting, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public DataTable GetDepreciationsForPostingCostCenter(int companyID, DateTime fromDate, DateTime toDate,int AssetTypeID, int CostCenterID, SqlConnection objSQL_Connection)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@FromDate", fromDate);
                sqlparams[2] = new SqlParameter("@ToDate", toDate);
                sqlparams[3] = new SqlParameter("@AssetTypeID", AssetTypeID);
                sqlparams[4] = new SqlParameter("@CostCenterID", CostCenterID);

                string Query_DepreciationsForPosting = @"
Select FA_DepreciationProcess.DepreciationProcessID, FA_DepreciationProcess.DepreciationAmount, FA_DepreciationProcess.DepreciatedUpto, 
    FA_Set_AssetType.AccDeprAccountCode As CreditAccountCode, FA_AssetTypePolicy.GLCodeCombinationID As DebitAccountCode,  FA_AssetInformation.CostCenterID, 
    FA_DepreciationProcess.DepartmentID, FA_DepreciationProcess.AssetTypeID, FA_Set_AssetType.AssetTypeName ,  FA_AssetInformation.ItemID, FA_AssetInformation.GRNCostCentreID
From FA_DepreciationProcess 
	Inner Join FA_Set_AssetType On FA_Set_AssetType.CompanyID = FA_DepreciationProcess.CompanyID And FA_Set_AssetType.AssetTypeID = FA_DepreciationProcess.AssetTypeID 
	Inner Join FA_AssetTypePolicy On FA_AssetTypePolicy.CompanyID = FA_DepreciationProcess.CompanyID 
    And FA_AssetTypePolicy.AssetTypeID = FA_DepreciationProcess.AssetTypeID 
    And FA_AssetTypePolicy.AssetClassification = FA_DepreciationProcess.ProcessTypeID 
    inner join FA_AssetInformation on FA_AssetInformation.AssetInfoID =  FA_DepreciationProcess.AssetIdentityID
Where FA_DepreciationProcess.CompanyID = @CompanyID And FA_DepreciationProcess.DepreciatedUpto Between @FromDate And @ToDate and FA_DepreciationProcess.isposted <> 1 and   FA_DepreciationProcess.AssetTypeID = @AssetTypeID and FA_DepreciationProcess.CostCenterID =  @CostCenterID 
and FA_AssetInformation.isdisposed is null

";

                DataSet ds = SqlHelper.ExecuteDataset(objSQL_Connection, CommandType.Text, Query_DepreciationsForPosting, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static decimal GetDepreciationByAssetInfoID(int companyID, int AssetIdentityID)
        {
            DataTable dtToReturn = null;
            decimal DepreciationAmount = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@AssetIdentityID", AssetIdentityID);
                

                string Query_Get_DepreciationAmount = @"
                            select sum(DepreciationAmount)DepreciationAmount
                            From FA_DepreciationProcess 
                            Where FA_DepreciationProcess.CompanyID = @CompanyID  And FA_DepreciationProcess.AssetIdentityID =  @AssetIdentityID and FA_DepreciationProcess.status = 1
                ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_DepreciationAmount, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];

                if(dtToReturn!=null && dtToReturn.Rows.Count>0)
                    DepreciationAmount = dtToReturn.Rows[0]["DepreciationAmount"].ToString() != "" ? Convert.ToDecimal(dtToReturn.Rows[0]["DepreciationAmount"]) : 0;


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return DepreciationAmount;
        }

        public static DataTable CheckUnpostedDepreciation(int companyID, int AssetInfoID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@AssetInfoID", AssetInfoID);

                string Query_CheckUnpostedDepreciation = @"select * from FA_DepreciationProcess where CompanyID = @CompanyID and AssetIdentityID = @AssetInfoID and IsPosted = 0 and status = 1";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_CheckUnpostedDepreciation, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public DataTable GetDepreciationsForPostingCostCenter(int companyID, DateTime fromDate, DateTime toDate, int CostCenterID, SqlConnection objSQL_Connection)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@FromDate", fromDate);
                sqlparams[2] = new SqlParameter("@ToDate", toDate);
                sqlparams[3] = new SqlParameter("@AssetTypeID", AssetTypeID);
                sqlparams[4] = new SqlParameter("@CostCenterID", CostCenterID);

                string Query_DepreciationsForPosting = @"
Select FA_DepreciationProcess.DepreciationProcessID, FA_DepreciationProcess.DepreciationAmount, FA_DepreciationProcess.DepreciatedUpto, 
    FA_Set_AssetType.AccDeprAccountCode As CreditAccountCode, FA_AssetTypePolicy.GLCodeCombinationID As DebitAccountCode,  FA_AssetInformation.CostCenterID, 
    FA_DepreciationProcess.DepartmentID, FA_DepreciationProcess.AssetTypeID, FA_Set_AssetType.AssetTypeName ,  FA_AssetInformation.ItemID, FA_AssetInformation.GRNCostCentreID
From FA_DepreciationProcess 
	Inner Join FA_Set_AssetType On FA_Set_AssetType.CompanyID = FA_DepreciationProcess.CompanyID And FA_Set_AssetType.AssetTypeID = FA_DepreciationProcess.AssetTypeID 
	Inner Join FA_AssetTypePolicy On FA_AssetTypePolicy.CompanyID = FA_DepreciationProcess.CompanyID 
    And FA_AssetTypePolicy.AssetTypeID = FA_DepreciationProcess.AssetTypeID 
    And FA_AssetTypePolicy.AssetClassification = FA_DepreciationProcess.ProcessTypeID 
    inner join FA_AssetInformation on FA_AssetInformation.AssetInfoID =  FA_DepreciationProcess.AssetIdentityID
Where FA_DepreciationProcess.CompanyID = @CompanyID And FA_DepreciationProcess.DepreciatedUpto Between @FromDate And @ToDate and FA_DepreciationProcess.isposted <> 1 and FA_DepreciationProcess.CostCenterID =  @CostCenterID 
and FA_AssetInformation.isdisposed is null

";

                DataSet ds = SqlHelper.ExecuteDataset(objSQL_Connection, CommandType.Text, Query_DepreciationsForPosting, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public DataTable GetDepreciationsForPostingCostCenter(int companyID, DateTime fromDate, DateTime toDate,bool mergeCostCentre , SqlConnection objSQL_Connection)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@FromDate", fromDate);
                sqlparams[2] = new SqlParameter("@ToDate", toDate);


                string Query_DepreciationsForPosting = @"
Select FA_DepreciationProcess.DepreciationProcessID, FA_DepreciationProcess.DepreciationAmount, FA_DepreciationProcess.DepreciatedUpto, 
    FA_Set_AssetType.AccDeprAccountCode As CreditAccountCode, FA_AssetTypePolicy.GLCodeCombinationID As DebitAccountCode,  FA_AssetInformation.CostCenterID, 
    FA_DepreciationProcess.DepartmentID, FA_DepreciationProcess.AssetTypeID, FA_Set_AssetType.AssetTypeName ,  FA_AssetInformation.ItemID, FA_AssetInformation.GRNCostCentreID
From FA_DepreciationProcess 
	Inner Join FA_Set_AssetType On FA_Set_AssetType.CompanyID = FA_DepreciationProcess.CompanyID And FA_Set_AssetType.AssetTypeID = FA_DepreciationProcess.AssetTypeID 
	Inner Join FA_AssetTypePolicy On FA_AssetTypePolicy.CompanyID = FA_DepreciationProcess.CompanyID 
    And FA_AssetTypePolicy.AssetTypeID = FA_DepreciationProcess.AssetTypeID 
    And FA_AssetTypePolicy.AssetClassification = FA_DepreciationProcess.ProcessTypeID 
    and FA_AssetTypePolicy.status = 1
    inner join FA_AssetInformation on FA_AssetInformation.AssetInfoID =  FA_DepreciationProcess.AssetIdentityID
Where FA_DepreciationProcess.CompanyID = @CompanyID And FA_DepreciationProcess.DepreciatedUpto Between @FromDate And @ToDate and FA_DepreciationProcess.isposted <> 1 
and FA_DepreciationProcess.status = 1
and FA_AssetInformation.isdisposed is null

";
                string Query_DepreciationsForPosting_Merge = @"
Select 0 as DepreciationProcessID, sum(FA_DepreciationProcess.DepreciationAmount)DepreciationAmount, FA_DepreciationProcess.DepreciatedUpto, 
    FA_Set_AssetType.AccDeprAccountCode As CreditAccountCode, FA_AssetTypePolicy.GLCodeCombinationID As DebitAccountCode,  FA_AssetInformation.CostCenterID, 
    FA_DepreciationProcess.DepartmentID, 0 as AssetTypeID, NULL AssetTypeName ,  0 as ItemID, NULL as GRNCostCentreID
From FA_DepreciationProcess 
	Inner Join FA_Set_AssetType On FA_Set_AssetType.CompanyID = FA_DepreciationProcess.CompanyID And FA_Set_AssetType.AssetTypeID = FA_DepreciationProcess.AssetTypeID 
	Inner Join FA_AssetTypePolicy On FA_AssetTypePolicy.CompanyID = FA_DepreciationProcess.CompanyID 
    And FA_AssetTypePolicy.AssetTypeID = FA_DepreciationProcess.AssetTypeID 
    And FA_AssetTypePolicy.AssetClassification = FA_DepreciationProcess.ProcessTypeID 
    and FA_AssetTypePolicy.status = 1
    inner join FA_AssetInformation on FA_AssetInformation.AssetInfoID =  FA_DepreciationProcess.AssetIdentityID
Where FA_DepreciationProcess.CompanyID = @CompanyID And FA_DepreciationProcess.DepreciatedUpto Between @FromDate And @ToDate and FA_DepreciationProcess.isposted <> 1 
and FA_DepreciationProcess.status = 1 and FA_AssetInformation.isdisposed is null
group by FA_AssetInformation.CostCenterID, FA_DepreciationProcess.DepreciatedUpto, FA_Set_AssetType.AccDeprAccountCode, FA_AssetTypePolicy.GLCodeCombinationID ,FA_DepreciationProcess.DepartmentID

                ";
                DataSet ds = null;

                if (mergeCostCentre == false)
                {
                    ds = SqlHelper.ExecuteDataset(objSQL_Connection, CommandType.Text, Query_DepreciationsForPosting, sqlparams);
                }
                else if (mergeCostCentre == true){
                    ds = SqlHelper.ExecuteDataset(objSQL_Connection, CommandType.Text, Query_DepreciationsForPosting_Merge, sqlparams);
                }

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public DataTable GetDepreciationsForPostingWOCostCenter(int companyID, DateTime fromDate, DateTime toDate, int AssetTypeID, SqlConnection objSQL_Connection)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@FromDate", fromDate);
                sqlparams[2] = new SqlParameter("@ToDate", toDate);
                sqlparams[3] = new SqlParameter("@AssetTypeID", AssetTypeID);

                string Query_DepreciationsForPosting = @"
Select FA_DepreciationProcess.DepreciationProcessID, FA_DepreciationProcess.DepreciationAmount, FA_DepreciationProcess.DepreciatedUpto, 
    FA_Set_AssetType.AccDeprAccountCode As CreditAccountCode, FA_AssetTypePolicy.GLCodeCombinationID As DebitAccountCode,  FA_AssetInformation.CostCenterID, 
    FA_DepreciationProcess.DepartmentID, FA_DepreciationProcess.AssetTypeID, FA_Set_AssetType.AssetTypeName ,  FA_AssetInformation.ItemID, FA_AssetInformation.GRNCostCentreID
From FA_DepreciationProcess 
	Inner Join FA_Set_AssetType On FA_Set_AssetType.CompanyID = FA_DepreciationProcess.CompanyID And FA_Set_AssetType.AssetTypeID = FA_DepreciationProcess.AssetTypeID 
	Inner Join FA_AssetTypePolicy On FA_AssetTypePolicy.CompanyID = FA_DepreciationProcess.CompanyID 
    And FA_AssetTypePolicy.AssetTypeID = FA_DepreciationProcess.AssetTypeID 
    And FA_AssetTypePolicy.AssetClassification = FA_DepreciationProcess.ProcessTypeID 
    inner join FA_AssetInformation on FA_AssetInformation.AssetInfoID =  FA_DepreciationProcess.AssetIdentityID
Where FA_DepreciationProcess.CompanyID = @CompanyID And FA_DepreciationProcess.DepreciatedUpto Between @FromDate And @ToDate and FA_DepreciationProcess.isposted <> 1 and   FA_DepreciationProcess.AssetTypeID = @AssetTypeID 
and FA_AssetInformation.isdisposed is null
and FA_DepreciationProcess.status = 1
";

                DataSet ds = SqlHelper.ExecuteDataset(objSQL_Connection, CommandType.Text, Query_DepreciationsForPosting, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public bool PostGL(List<FA_DepreciationProcess> lst_Depreciations, GL_TransactionHeader obj_GLHeader, SET_DocumentApprove obj_DocumentApprove,
            List<GL_TransactionDetail> lst_GLDetails, int CurrentFiscalID=0)
        {
            bool result = false;
            SqlTransaction objSQLTransaction = SqlHelper.GetTransaction("PostGLAgainstDepreciations");

            try
            {
                #region General Ledger Header

                obj_GLHeader.DocumentNo = SET_DocumentSequence.ManualDocumentSequence(obj_GLHeader.DocumentTypeID, obj_GLHeader.CompanyID, obj_GLHeader.PeriodID,
                    objSQLTransaction, CurrentFiscalID);

                obj_GLHeader.TransactionID = GL_TransactionHeader.GetNewTransactionID(obj_GLHeader.CompanyID, obj_GLHeader.PeriodID, objSQLTransaction);
                obj_GLHeader.Insert(objSQLTransaction);

                #endregion

                #region Depreciation Process

                foreach (FA_DepreciationProcess objDepreciation in lst_Depreciations)
                {
                    objDepreciation.GLHeaderID = obj_GLHeader.TransactionID;
                    objDepreciation.UpdateDataByKey(objSQLTransaction);
                }

                #endregion

                #region Document Approve

                obj_DocumentApprove.DocumentNo = obj_GLHeader.DocumentNo.ToString();
                obj_DocumentApprove.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(obj_DocumentApprove.CompanyID, obj_DocumentApprove.PeriodID,
                    objSQLTransaction);

                obj_DocumentApprove.Insert(objSQLTransaction);

                #endregion

                foreach (GL_TransactionDetail objGLDetail in lst_GLDetails)
                {
                    objGLDetail.TransactionID = obj_GLHeader.TransactionID;
                    objGLDetail.SerialNo = GL_TransactionDetail.GetNewSerialNo(objGLDetail.CompanyID, objGLDetail.PeriodID, obj_GLHeader.TransactionID,
                        objSQLTransaction);

                    objGLDetail.Insert(objSQLTransaction);
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
                    SqlHelper.CommitTransaction(objSQLTransaction);
                else
                    SqlHelper.RollBackTransaction(objSQLTransaction, "PostGLAgainstDepreciations");
            }

            return result;
        }


        public static bool DeleteDataByTypeID(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, int companyID, Nullable<int> AssetTypeID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@Status", false);
                sqlparams[2] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[3] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[4] = new SqlParameter("@UpdatedByValue", updatedByValue);

                if (AssetTypeID.HasValue)
                {

                    sqlparams[5] = new SqlParameter("@AssetTypeID", AssetTypeID);
                }
                else
                {
                    sqlparams[5] = new SqlParameter("@AssetTypeID", DBNull.Value);
                }

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByAssetTypeID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByAssetTypeID, sqlparams) > 0)
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

        public static bool IsDepreciationPosted(int companyID, DateTime fromDate, DateTime toDate)
        {
            DataTable dtToReturn = null;
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@FromDate", fromDate);
                sqlparams[2] = new SqlParameter("@ToDate", toDate);

                string Query_DepreciationsForPosting = @"
Select FA_DepreciationProcess.DepreciationProcessID, FA_DepreciationProcess.DepreciationAmount, FA_DepreciationProcess.DepreciatedUpto, 
    FA_Set_AssetType.AccDeprAccountCode As CreditAccountCode, FA_AssetTypePolicy.GLCodeCombinationID As DebitAccountCode, FA_DepreciationProcess.CostCenterID, 
    FA_DepreciationProcess.DepartmentID, FA_DepreciationProcess.AssetTypeID, FA_Set_AssetType.AssetTypeName ,  FA_AssetInformation.ItemID
From FA_DepreciationProcess 
	Inner Join FA_Set_AssetType On FA_Set_AssetType.CompanyID = FA_DepreciationProcess.CompanyID And FA_Set_AssetType.AssetTypeID = FA_DepreciationProcess.AssetTypeID 
	Inner Join FA_AssetTypePolicy On FA_AssetTypePolicy.CompanyID = FA_DepreciationProcess.CompanyID 
    And FA_AssetTypePolicy.AssetTypeID = FA_DepreciationProcess.AssetTypeID 
    And FA_AssetTypePolicy.AssetClassification = FA_DepreciationProcess.ProcessTypeID 
    inner join FA_AssetInformation on FA_AssetInformation.AssetInfoID =  FA_DepreciationProcess.AssetIdentityID
Where FA_DepreciationProcess.CompanyID = @CompanyID And FA_DepreciationProcess.DepreciatedUpto = @FromDate and FA_DepreciationProcess.isposted = 1 ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_DepreciationsForPosting, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    result = true;
                //dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataTable GetDepreciationForTheMonth(int companyID, DateTime fromDate, DateTime toDate)
        {
            DataTable dtToReturn = null;
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@FromDate", fromDate);
                sqlparams[2] = new SqlParameter("@ToDate", toDate);

                string Query_DepreciationsForPosting = @"
Select FA_DepreciationProcess.DepreciationProcessID, FA_DepreciationProcess.DepreciationAmount, FA_DepreciationProcess.DepreciatedUpto, 
    FA_Set_AssetType.AccDeprAccountCode As CreditAccountCode, FA_AssetTypePolicy.GLCodeCombinationID As DebitAccountCode, FA_DepreciationProcess.CostCenterID, 
    FA_DepreciationProcess.DepartmentID, FA_DepreciationProcess.AssetTypeID, FA_Set_AssetType.AssetTypeName ,  FA_AssetInformation.ItemID
    ,FA_DepreciationProcess.AssetIdentityID
From FA_DepreciationProcess 
	Inner Join FA_Set_AssetType On FA_Set_AssetType.CompanyID = FA_DepreciationProcess.CompanyID And FA_Set_AssetType.AssetTypeID = FA_DepreciationProcess.AssetTypeID 
	Inner Join FA_AssetTypePolicy On FA_AssetTypePolicy.CompanyID = FA_DepreciationProcess.CompanyID 
    And FA_AssetTypePolicy.AssetTypeID = FA_DepreciationProcess.AssetTypeID 
    And FA_AssetTypePolicy.AssetClassification = FA_DepreciationProcess.ProcessTypeID 
    inner join FA_AssetInformation on FA_AssetInformation.AssetInfoID =  FA_DepreciationProcess.AssetIdentityID
Where FA_DepreciationProcess.CompanyID = @CompanyID And FA_DepreciationProcess.DepreciatedUpto = @FromDate and FA_DepreciationProcess.isposted <> 1 ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_DepreciationsForPosting, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
                //
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }


        public static bool DepreciationDelete(DateTime FromDate, int CompanyID, int PeriodID, SqlTransaction sqltrans)
        {
            bool result = false;

            const string Query_DepreciationInProcess = @" Delete from FA_DepreciationProcess where FA_DepreciationProcess.DepreciatedUpto = @FromDate and FA_DepreciationProcess.isposted <> 1";



            //SqlTransaction sqltrans = SqlHelper.GetTransaction("DepreciationInProcess");
            StringBuilder sb = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            StringBuilder sb3 = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[2] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[3] = new SqlParameter("@FromDate", FromDate);

                if (sqltrans == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, string.Format(Query_DepreciationInProcess, sb), sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(sqltrans, CommandType.Text, string.Format(Query_DepreciationInProcess, sb), sqlparams) > 0)
                        result = true;
                }

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

            return result;
        }


        public static bool DepreciationInProcess(DateTime FromDate, int CompanyID, int PeriodID, List<FA_AssetInformation> lst_Identities)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InProcessDepreciationIdentities");

            try
            {
                DepreciationDelete(FromDate, CompanyID, PeriodID, sqltrans);

                foreach (FA_AssetInformation objIdentity in lst_Identities)
                    objIdentity.UpdateDataByKey(sqltrans);

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
                    SqlHelper.RollBackTransaction(sqltrans, "InProcessDepreciationIdentities");
            }

            return result;
        }


        public static DataTable GetLastDepreciationDate(int AssetTypeID, int AssetIdentityID, DateTime DepreciatedUpto)
        {
            DataTable dtToReturn = null;
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@AssetTypeID", AssetTypeID);
                sqlparams[1] = new SqlParameter("@AssetIdentityID", AssetIdentityID);
                sqlparams[2] = new SqlParameter("@DepreciatedUpto", DepreciatedUpto);

                string Query_DepreciationsForPosting = @"
Select Top 1 DepreciatedUpTo from FA_DepreciationProcess 
Where AssetTypeID = @AssetTypeID  And AssetIdentityID = @AssetIdentityID And DepreciatedUpto < @DepreciatedUpto Order by DepreciatedUpTo desc ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_DepreciationsForPosting, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
                //
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static int GetTotalDepreciationByAssetInfoID(int companyID, int AssetIdentityID)
        {
            DataTable dtToReturn = null;
            int DepreciationCount = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@AssetIdentityID", AssetIdentityID);


                string Query_Get_DepreciationCount = @"
                            select count(DepreciationProcessID)DepreciationProcessID
                            From FA_DepreciationProcess 
                            Where FA_DepreciationProcess.CompanyID = @CompanyID 
                            And FA_DepreciationProcess.AssetIdentityID =  @AssetIdentityID
                            And FA_DepreciationProcess.Status = 1

                ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_DepreciationCount, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];

                if (dtToReturn != null && dtToReturn.Rows.Count > 0)
                    DepreciationCount = dtToReturn.Rows[0]["DepreciationProcessID"].ToString() != "" ? Convert.ToInt16(dtToReturn.Rows[0]["DepreciationProcessID"]) : 0;


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return DepreciationCount;
        }


        public static decimal GetDepreciationAmountByAssetInfoIDYearWise(int companyID, int AssetIdentityID)
        {
            DataTable dtToReturn = null;
            decimal DepreciationCount = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@AssetIdentityID", AssetIdentityID);


                string Query_Get_DepreciationCount = @"
                   select sum(DepreciationAmount) as DepreciationAmount from (
         select top 12 DepreciationAmount 
                            From FA_DepreciationProcess 
                            Where FA_DepreciationProcess.CompanyID = @CompanyID 
                            And FA_DepreciationProcess.AssetIdentityID =  @AssetIdentityID
							and FA_DepreciationProcess.Status = 1
                            order by CreateDate desc
)as tab

                ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_DepreciationCount, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];

                if (dtToReturn != null && dtToReturn.Rows.Count > 0)
                    DepreciationCount = dtToReturn.Rows[0]["DepreciationAmount"].ToString() != "" ? Convert.ToDecimal(dtToReturn.Rows[0]["DepreciationAmount"]) : 0;


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return DepreciationCount;
        }

        public static decimal GetDepreciationAmountByAssetInfoIDYearWiseNew(int companyID, int AssetIdentityID)
        {
            DataTable dtToReturn = null;
            decimal DepreciationCount = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@AssetIdentityID", AssetIdentityID);


                string Query_Get_DepreciationCount = @"
                   select sum(DepreciationAmount) as DepreciationAmount from (
         select top 1 DepreciationAmount 
                            From FA_DepreciationProcess 
                            Where FA_DepreciationProcess.CompanyID = @CompanyID 
                            And FA_DepreciationProcess.AssetIdentityID =  @AssetIdentityID
							and FA_DepreciationProcess.Status = 1
                            order by CreateDate desc
)as tab

                ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_DepreciationCount, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];

                if (dtToReturn != null && dtToReturn.Rows.Count > 0)
                    DepreciationCount = dtToReturn.Rows[0]["DepreciationAmount"].ToString() != "" ? Convert.ToDecimal(dtToReturn.Rows[0]["DepreciationAmount"]) : 0;


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return DepreciationCount;
        }

        public static DataTable Get_DataForPrevQuaterStartAndEndDate(int companyID, int periodID, int fiscalid, int assittypeid, string query, string quarter)
        {
            DataTable drToReturn = null;
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@AssetTypeID", assittypeid);
                sqlparams[3] = new SqlParameter("@QuarterTypes", quarter);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

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

        public static DataTable Get_DataForPrevYearStartAndEndDate(int companyID, int periodID, int fiscalid, int assittypeid, string query, string year)
        {
            DataTable drToReturn = null;
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FiscalID", fiscalid);
                sqlparams[3] = new SqlParameter("@AssetTypeID", assittypeid);
                sqlparams[4] = new SqlParameter("@YearTypes", year);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

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

        public static DataTable GetDataForDepreciationForm(string StrngWhere)
        {
            DataTable dtToReturn = null;

            try
            {
                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataForDepreciationForm + StrngWhere);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static bool Check_TransactionExistByAssetIdentityID(int companyID, int AssetIdentityID)
        {
            bool Result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@AssetIdentityID", AssetIdentityID);


                string Query = @"
select * From FA_DepreciationProcess 
Where FA_DepreciationProcess.CompanyID = @CompanyID 
And FA_DepreciationProcess.AssetIdentityID =  @AssetIdentityID
AND FA_DepreciationProcess.Status = 1";

                if (SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams).Tables[0].Rows.Count > 0)
                    Result = true;

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
