using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;
using Common.Utils;
using System.Data;

namespace FnI.BusinessAccessLayer.GL.Transaction
{
    public partial class BudgetAppropriationHeader
    {
        #region Constant Queries
        
        const string Query_SelectAccountCode = @"
Select Distinct BudgetAppropriationHeader.AccountCode , GL_CodeCombinationDetail.Description
From 
BudgetAppropriationHeader
Inner Join GL_CodeCombinationDetail ON (GL_CodeCombinationDetail.CodeCombinationID = BudgetAppropriationHeader.AccountCode)
--And GL_CodeCombinationDetail.CompanyID = BudgetAppropriationHeader.CompanyID
Where 
BudgetAppropriationHeader.CompanyID = @CompanyID and BudgetAppropriationHeader.PeriodID = @PeriodID and BudgetAppropriationHeader.Status = 1 And BudgetAppropriationHeader.AccountCode <> '' ";

        const string Query_GetAllData_WithPaging = @" 
        SELECT  [BudgetAppropriationHeader].[CompanyID], 
		[BudgetAppropriationHeader].[PeriodID], 
		[BudgetAppropriationHeaderID], 
		[AccountCode],
		[GL_CodeCombinationDetail].[Description],
		[CostCenter], 
		[SET_CostCenter].[CostCenterName],
		[ReferenceNo], 
		[AppropriationDate], 
		[BudgetAppropriationHeader].[CreatedBy], 
		[BudgetAppropriationHeader].[CreateDate], 
		[BudgetAppropriationHeader].[CreatedByValue], 
		[BudgetAppropriationHeader].[UpdatedBy], 
		[BudgetAppropriationHeader].[UpdateDate], 
		[BudgetAppropriationHeader].[UpdatedByValue], 
		[BudgetAppropriationHeader].[Status], 
ROW_NUMBER() Over (Order by BudgetAppropriationHeaderID, [BudgetAppropriationHeader].[CompanyID], 
	[BudgetAppropriationHeader].PeriodID) as 'Row' 
FROM [BudgetAppropriationHeader] 
Inner join GL_CodeCombinationDetail 
ON GL_CodeCombinationDetail.CodeCombinationID = [BudgetAppropriationHeader].AccountCode
	--and GL_CodeCombinationDetail.CompanyID = [BudgetAppropriationHeader].CompanyID
Inner join SET_CostCenter on SET_CostCenter.CostCenterID = [BudgetAppropriationHeader].CostCenter	  ";

        const string Query_GetAllData_WithoutPaging = @" SELECT  [BudgetAppropriationHeader].[CompanyID], 
		[BudgetAppropriationHeader].[PeriodID], 
		[BudgetAppropriationHeaderID], 
		[AccountCode],
		GL_CodeCombinationDetail.[Description],
		[CostCenter], 
		SET_CostCenter.CostCenterName,
		[ReferenceNo], 
		[AppropriationDate], 
		[BudgetAppropriationHeader].[CreatedBy], 
		[BudgetAppropriationHeader].[CreateDate], 
		[BudgetAppropriationHeader].[CreatedByValue], 
		[BudgetAppropriationHeader].[UpdatedBy], 
		[BudgetAppropriationHeader].[UpdateDate], 
		[BudgetAppropriationHeader].[UpdatedByValue], 
		[BudgetAppropriationHeader].[Status]
FROM [BudgetAppropriationHeader] 
Inner join GL_CodeCombinationDetail 
ON GL_CodeCombinationDetail.CodeCombinationID = [BudgetAppropriationHeader].AccountCode
	--and GL_CodeCombinationDetail.CompanyID = [BudgetAppropriationHeader].CompanyID
Inner join SET_CostCenter on SET_CostCenter.CostCenterID = [BudgetAppropriationHeader].CostCenter	  "; 

        #endregion

        public static DataTable SelectAccountCode(int companyID , int periodid)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodid);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAccountCode, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public bool InsertACHeaderAndDetail(List<BudgetAppropriationDetail> colAssetCostDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertBudgetAppropriation");

            try
            {
                this.Insert(sqltrans);

                foreach (BudgetAppropriationDetail ACd in colAssetCostDetail)
                {
                    ACd.BudgetAppropriationDetailID = BudgetAppropriationDetail.GetNewBudgetAppropriationDetailID(f_CompanyID, f_PeriodID, sqltrans);
                    //ACd.BudgetAllocationDetailID = BudgetAllocationDetail.GetNewAssetCostDetailID(f_AssetCostID, f_CompanyID, f_PeriodID, sqltrans);
                    ACd.Insert(sqltrans);
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
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "InsertBudgetAppropriation");
            }

            return result;
        }

        public bool UpdateACHeaderAndDetail(List<BudgetAppropriationDetail> colAddAssetCostDetail, List<BudgetAppropriationDetail> colUpdateAssetCostDetail, List<BudgetAppropriationDetail> colDeleteAssetCostDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateBudgetApp");

            try
            {
                this.UpdateDataByKey(sqltrans);

                foreach (BudgetAppropriationDetail ACdAdd in colAddAssetCostDetail)
                {
                    ACdAdd.BudgetAppropriationDetailID = BudgetAppropriationDetail.GetNewBudgetAppropriationDetailID(f_CompanyID, f_PeriodID, sqltrans);
                    ACdAdd.Insert(sqltrans);
                }

                foreach (BudgetAppropriationDetail ACdUpdate in colUpdateAssetCostDetail)
                    ACdUpdate.UpdateDataByKey(sqltrans);

                foreach (BudgetAppropriationDetail ACdDelete in colDeleteAssetCostDetail)


                    BudgetAppropriationDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, ACdDelete.BudgetAppropriationDetailID, f_CompanyID, f_PeriodID);



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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateBudgetApp");
            }

            return result;
        }

        public bool DeleteACHeaderAndDetail(List<BudgetAppropriationDetail> colDeleteAssetCostDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("DeleteBudgetApp");

            try
            {

                DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, UpdatedByValue, f_BudgetAppropriationHeaderID, f_CompanyID, f_PeriodID);

                foreach (BudgetAppropriationDetail ACddDelete in colDeleteAssetCostDetail)

                    BudgetAppropriationDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, ACddDelete.BudgetAppropriationDetailID, f_CompanyID, f_PeriodID);

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
                    SqlHelper.RollBackTransaction(sqltrans, "DeleteBudgetApp");
            }
            return result;
        }

        public static DataTable GetData(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_GetAllData_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_GetAllData_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllData_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

    }
}
