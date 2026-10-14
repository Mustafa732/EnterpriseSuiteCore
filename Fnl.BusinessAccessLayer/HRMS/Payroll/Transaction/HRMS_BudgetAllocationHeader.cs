using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.AR.Setup;
using FnI.BusinessAccessLayer.Inventory.Setup;
using FnI.BusinessAccessLayer.Setup;
using System.Text;


namespace FnI.BusinessAccessLayer.HRMS.Payroll.Transaction
{

    public partial class HRMS_BudgetAllocationHeader
    {


        #region Constant Queries

        const string Query_DepartmentAndSalaryBudgetVarience = @"
select * from (

SELECT 

  datediff(month, min (HRMS_SalaryHeader.SalaryDate),max (HRMS_SalaryHeader.SalaryDate)) + 1 as NoOfMonths
	,HRMS_BudgetAllocationHeader.BudgetAllocationHeaderID
	,count (HRMS_SET_Employee.PersonID) as EmployeesStrenght
	,sum(HRMS_SalaryHeader.NetAmount) as NetAmount
	,sum(HRMS_SalaryHeader.NetAmount) as UtilizedAmount
	,SET_Department.DepartmentName
	,SET_Department.DepartmentID
	,HRMS_BudgetAllocationHeader.FromDate
	,HRMS_BudgetAllocationHeader.ToDate
	,HRMS_BudgetAllocationHeader.Frequency
	,HRMS_BudgetAllocationDetail.BudgetAmount
 
 , case 
		 when HRMS_BudgetAllocationDetail.Period < 13 
		 then DateName( month , DateAdd( month , HRMS_BudgetAllocationDetail.Period , 0 ) - 1 )
		 
		 when HRMS_BudgetAllocationDetail.Period  =  13 
		 then 'First Quater'  
		 
		 when HRMS_BudgetAllocationDetail.Period  =  14 
		 then 'Second Quater' 
		 
		 when HRMS_BudgetAllocationDetail.Period  =  15 
		 then 'Third Quater' 
		 
		 when HRMS_BudgetAllocationDetail.Period  =  16 
		 then 'Forth Quater' 
		 
		 when HRMS_BudgetAllocationDetail.Period  =  17 
		 then 'Yearly'
		 
		 end as PeriodName 
		 
		 ,case 
		 when HRMS_BudgetAllocationDetail.Period < 13 
		 then 1
		 when HRMS_BudgetAllocationDetail.Period between 13 and 16 
		 then 2
		 when HRMS_BudgetAllocationDetail.Period = 17 
		 then 3
		 end 
		 as FrequencyType
		 
		 
	,HRMS_BudgetAllocationDetail.Period 
	,HRMS_BudgetAllocationHeader.CompanyID
	,HRMS_BudgetAllocationHeader.PeriodID

 FROM dbo.HRMS_BudgetAllocationHeader
 
INNER join HRMS_BudgetAllocationDetail on  HRMS_BudgetAllocationDetail.BudgetAllocationHeaderID = HRMS_BudgetAllocationHeader.BudgetAllocationHeaderID
		and HRMS_BudgetAllocationDetail.CompanyID = HRMS_BudgetAllocationHeader.CompanyID
		and HRMS_BudgetAllocationDetail.PeriodID = HRMS_BudgetAllocationHeader.PeriodID
		and  HRMS_BudgetAllocationDetail.Status  =  1

inner join SET_Department on SET_Department.DepartmentID  =  HRMS_BudgetAllocationHeader.Department
		and SET_Department.Status =1 

left join HRMS_SET_Employee on HRMS_SET_Employee.FK_DepartmentID  = HRMS_BudgetAllocationHeader.Department
		and HRMS_SET_Employee.CompanyID  = HRMS_BudgetAllocationHeader.CompanyID
		and HRMS_SET_Employee.Status =1 

left join HRMS_SalaryHeader on HRMS_SalaryHeader.PersonID  =  HRMS_SET_Employee.PersonID
		and HRMS_SalaryHeader.CompanyID =  HRMS_SET_Employee.CompanyID
		and HRMS_SalaryHeader.SalaryDate between HRMS_BudgetAllocationHeader.FromDate and  HRMS_BudgetAllocationHeader.ToDate
		and   HRMS_SalaryHeader.Status =1 



group by 
		SET_Department.DepartmentName
		,HRMS_BudgetAllocationHeader.FromDate
		,HRMS_BudgetAllocationHeader.ToDate
		,HRMS_BudgetAllocationHeader.Frequency
		,HRMS_BudgetAllocationDetail.BudgetAmount
		,HRMS_BudgetAllocationDetail.Period
		,HRMS_BudgetAllocationHeader.BudgetAllocationHeaderID
		,HRMS_BudgetAllocationHeader.CompanyID
	,HRMS_BudgetAllocationHeader.PeriodID
	,SET_Department.DepartmentID


		) as tab
		
		
		where 			
		tab.CompanyID = @CompanyID
		and tab.PeriodID = @PeriodID

        {0}
		order by 
		tab.BudgetAllocationHeaderID

";

        const string Query_SelectAll_BudgetAllocationHeader = @" SELECT * FROM [HRMS_BudgetAllocationHeader] ";//WHERE [Status] = 1

        //        const string Query_SelectAccountCode = @"
        //        Select Distinct GL_CodeCombinationDetail.Description
        //        From 
        //        HRMS_BudgetAllocationHeader
        //        Inner Join GL_CodeCombinationDetail.CompanyID = HRMS_BudgetAllocationHeader.CompanyID
        //        Where 
        //        HRMS_BudgetAllocationHeader.CompanyID = @CompanyID and HRMS_BudgetAllocationHeader.PeriodID = @PeriodID and HRMS_BudgetAllocationHeader.Status = 1
        //        ";

        //        const string Query_SelectAccountCodeForAllocation = @"select GL_CodeCombinationDetail.CodeCombinationID,Description 
        //        from GL_CodeCombinationDetail
        //        inner join Set_AccountType on (Set_AccountType.AccountTypeID = GL_CodeCombinationDetail.AccountTypeID)
        //        and Set_AccountType.AccountTypeName = 'Expenses'
        //        and GL_CodeCombinationDetail.CompanyID = @CompanyID
        //        ";

        const string Query_SelectAllForBudgetAllocation = @"
        SELECT *,GL_CodeCombinationDetail.[Description]  FROM [HRMS_BudgetAllocationHeader]
        left join GL_CodeCombinationDetail on HRMS_BudgetAllocationHeader.CompanyID = GL_CodeCombinationDetail.CompanyID 
        WHERE [HRMS_BudgetAllocationHeader].[Status] = 1 
        ";

        const string Query_SelectAllWithPaging = @" SELECT [BudgetAllocationHeaderID], GL_CodeCombinationDetail.Description,  [ToDate], [Frequency],HRMS_BudgetAllocationHeader.[Status], HRMS_BudgetAllocationHeader.[CompanyID], 
        [PeriodID], HRMS_BudgetAllocationHeader.[CreatedBy], HRMS_BudgetAllocationHeader.[CreateDate], HRMS_BudgetAllocationHeader.[CreatedByValue], HRMS_BudgetAllocationHeader.[UpdatedBy], HRMS_BudgetAllocationHeader.[UpdateDate], HRMS_BudgetAllocationHeader.[UpdatedByValue], 
        [FromDate], [Department], 
        ROW_NUMBER() Over (Order by BudgetAllocationHeaderID, HRMS_BudgetAllocationHeader.CompanyID, PeriodID) as 'Row' 
        FROM [HRMS_BudgetAllocationHeader]
        Inner Join GL_CodeCombinationDetail.CompanyID = HRMS_BudgetAllocationHeader.CompanyID";

        const string Query_SelectDataForBudgetAllocation_WithPaging = @"
         SELECT [BudgetAllocationHeaderID], 
        SET_Department.[DepartmentName] as Description,
        [ToDate], 
        [Frequency], 
        HRMS_BudgetAllocationHeader.[Status], 
        HRMS_BudgetAllocationHeader.[CompanyID], 
        HRMS_BudgetAllocationHeader.[PeriodID], 
        HRMS_BudgetAllocationHeader.[CreatedBy], 
        HRMS_BudgetAllocationHeader.[CreateDate], 
        HRMS_BudgetAllocationHeader.[CreatedByValue], 
        HRMS_BudgetAllocationHeader.[UpdatedBy], 
        HRMS_BudgetAllocationHeader.[UpdateDate], 
        HRMS_BudgetAllocationHeader.[UpdatedByValue], 
        [FromDate], 
        ROW_NUMBER() Over (Order by BudgetAllocationHeaderID, HRMS_BudgetAllocationHeader.CompanyID, HRMS_BudgetAllocationHeader.PeriodID) as 'Row' 
        FROM [HRMS_BudgetAllocationHeader]
       inner join SET_Department on HRMS_BudgetAllocationHeader.Department = SET_Department.DepartmentID 
  ";

        //        const string Query_GetCostCentersByDepartmentID = @"select [SET_CostCenter].[CostCenterID], [SET_CostCenter].[CostCenterName] from [HRMS_BudgetAllocationHeader]
        //        inner join [SET_CostCenter] on [SET_CostCenter].[CostCenterID] = [HRMS_BudgetAllocationHeader].[CostCenter]
        //        where [HRMS_BudgetAllocationHeader].[CompanyID] = @CompanyID and [HRMS_BudgetAllocationHeader].[PeriodID] = @PeriodID and [HRMS_BudgetAllocationHeader].[Department] = @DepartmentID
        //        group by  [SET_CostCenter].[CostCenterID], [SET_CostCenter].[CostCenterName],[HRMS_BudgetAllocationHeader].[Department]";

        #endregion

        public static DataTable GetAllData_BudgetAllocationHeader(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAllWithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAllWithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_BudgetAllocationHeader + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        //public static DataTable GetAllCostCentersByDepartmentID(int companyID, int periodID, int departmentId)
        //{
        //    DataTable dt = null;

        //    try
        //    {
        //        SqlParameter[] sqlparams = new SqlParameter[3];
        //        sqlparams[0] = new SqlParameter("@CompanyID", companyID);
        //        sqlparams[1] = new SqlParameter("@PeriodID", periodID);
        //        sqlparams[2] = new SqlParameter("@DepartmentID", departmentId);
        //        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetCostCentersByDepartmentID, sqlparams).Tables[0];
        //    }
        //    catch (Exception ex)
        //    {
        //        SqlHelper.LogException(ex);
        //        throw ex;
        //    }

        //    return dt;
        //}

        public bool InsertACHeaderAndDetail(List<HRMS_BudgetAllocationDetail> colAssetCostDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertAssetCost");

            try
            {
                this.Insert(sqltrans);

                foreach (HRMS_BudgetAllocationDetail ACd in colAssetCostDetail)
                {
                    ACd.BudgetAllocationDetailID = HRMS_BudgetAllocationDetail.GetNewBudgetAllocationDetailID(f_CompanyID, f_PeriodID, sqltrans);
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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertAssetCost");
            }

            return result;
        }

        public bool UpdateACHeaderAndDetail(List<HRMS_BudgetAllocationDetail> colAddAssetCostDetail, List<HRMS_BudgetAllocationDetail> colUpdateAssetCostDetail, List<HRMS_BudgetAllocationDetail> colDeleteAssetCostDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateBudgetAllocation");

            try
            {
                this.UpdateDataByKey(sqltrans);

                foreach (HRMS_BudgetAllocationDetail ACdAdd in colAddAssetCostDetail)
                {
                    ACdAdd.BudgetAllocationDetailID = HRMS_BudgetAllocationDetail.GetNewBudgetAllocationDetailID(f_CompanyID, f_PeriodID, sqltrans);
                    ACdAdd.Insert(sqltrans);
                }

                foreach (HRMS_BudgetAllocationDetail ACdUpdate in colUpdateAssetCostDetail)
                    ACdUpdate.UpdateDataByKey(sqltrans);

                foreach (HRMS_BudgetAllocationDetail ACdDelete in colDeleteAssetCostDetail)


                    HRMS_BudgetAllocationDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, ACdDelete.BudgetAllocationDetailID, f_CompanyID, f_PeriodID);



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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateBudgetAllocation");
            }

            return result;
        }

        public bool DeleteACHeaderAndDetail(List<HRMS_BudgetAllocationDetail> colDeleteAssetCostDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("DeleteAssetCost");

            try
            {

                DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, UpdatedByValue, f_BudgetAllocationHeaderID, f_CompanyID, f_PeriodID);

                foreach (HRMS_BudgetAllocationDetail ACddDelete in colDeleteAssetCostDetail)

                    HRMS_BudgetAllocationDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, ACddDelete.BudgetAllocationDetailID, f_CompanyID, f_PeriodID);

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
                    SqlHelper.RollBackTransaction(sqltrans, "DeleteBudgetAllocation");
            }
            return result;
        }

        public static DataTable GetAllDataForBudgetAllocation(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectDataForBudgetAllocation_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllForBudgetAllocation + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable get_DepartmentAndSalaryBudgetVarience(int companyID, int periodID, Nullable< int> Department,Nullable< int > frequency)
        {

            //Query_RestrictionOnSalaryDate
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (Department != null)
                {
                    if (Department > 0)
                    {
                        sb.Append(" and tab.DepartmentID = " + Department + "");
                    }
                    else { sb.Append(""); }
                }
                if (frequency != null)
                {
                    if (frequency > 0)
                    {
                        sb.Append(" and tab.FrequencyType = " + frequency + " ");

                    }
                    else { sb.Append("  "); }
                    //   sqlparams[2] = new SqlParameter("@PersonID", personID);
                }

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format( Query_DepartmentAndSalaryBudgetVarience,sb), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;

            //Query_DepartmentAndSalaryBudgetVarience
        }
    }
}