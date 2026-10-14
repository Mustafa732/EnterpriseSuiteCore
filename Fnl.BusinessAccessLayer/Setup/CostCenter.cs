using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using DAL.DataAccessLayer;
using Common.Utils;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_CostCenter
    {
        #region Constant Queries

        const string Query_GetCostCenterList = @" 
SELECT SET_CostCenter.CostCenterID, SET_CostCenter.CostCenterName, SET_Department.DepartmentName, SET_CostCenter.HeadingCostCenterName, SET_CostCenter.IsActive, 
    SET_CostCenter.IsAllowPosting, SET_Department.DepartmentID, SET_CostCenter.AccountTypeID,SET_CostCenter.BSCostCenterID,AccountTypeName 
FROM SET_CostCenter 
	Inner Join Set_DepartmentsInCC On Set_DepartmentsInCC.FK_CostCenterID = SET_CostCenter.CostCenterID And Set_DepartmentsInCC.Status = 1 
	inner join SET_Department on SET_Department.DepartmentID = Set_DepartmentsInCC.FK_DepartmentID
	inner join SET_AccountType on SET_CostCenter.AccountTypeID = SET_AccountType.AccountTypeID 
WHERE SET_CostCenter.Status = 1  AND (SET_CostCenter.IsActive = @IsActive OR @IsActive Is Null) AND SET_CostCenter.CostCenterName Like '%' + @CostCenterFilter + '%'
{0}";

        const string Query_GetCostCenterForReport = @"Select Set_CostCenter.CostCenterName,Set_CostCenter.CostCenterID From 
BudgetAllocationHeader
Inner Join Set_CostCenter ON (Set_CostCenter.CostCenterID = BudgetAllocationHeader.CostCenter)";

        const string Query_GetCostCenterByAccountCode = @"select SET_CostCenter.CostCenterName,BudgetAllocationHeader.CostCenter,
BudgetAllocationHeader.AccountCode,SET_CostCenter.CostCenterID
From SET_CostCenter
Inner JOIN BudgetAllocationHeader ON (BudgetAllocationHeader.CostCenter = SET_CostCenter.CostCenterID)
AND BudgetAllocationHeader.CompanyID = @CompanyID
AND BudgetAllocationHeader.PeriodID = @PeriodID
AND BudgetAllocationHeader.Status = 1
AND SET_CostCenter.Status = 1
AND BudgetAllocationHeader.AccountCode = @AC";

  

        const string Query_GetCostCenterByAccountCodeForAllocation = @" select SET_CostCenter.CostCenterName,SET_CostCenter.CostCenterID
From SET_CostCenter
left join BudgetAllocationHeader on (BudgetAllocationHeader.CostCenter = SET_CostCenter.CostCenterID)
AND BudgetAllocationHeader.CompanyID = @CompanyID
AND BudgetAllocationHeader.PeriodID = @PeriodID
AND BudgetAllocationHeader.AccountCode = @AC
where SET_CostCenter.Status = 1
AND SET_CostCenter.IsActive = 1
AND BudgetAllocationHeader.CostCenter is null";

   

        const string Query_GetCostCenterByAccountCodeForReApp = @"select SET_CostCenter.CostCenterName,BudgetAppropriationHeader.CostCenter,
BudgetAppropriationHeader.AccountCode,SET_CostCenter.CostCenterID
From SET_CostCenter
Inner JOIN BudgetAppropriationHeader ON (BudgetAppropriationHeader.CostCenter = SET_CostCenter.CostCenterID)
AND BudgetAppropriationHeader.CompanyID = @CompanyID
AND BudgetAppropriationHeader.PeriodID = @PeriodID
AND BudgetAppropriationHeader.Status = 1
AND SET_CostCenter.Status = 1
AND BudgetAppropriationHeader.AccountCode = @AC";

        const string Query_CheckCostCenterInUsed = @" 
SELECT * FROM
    (
		SELECT CostCenterID, Status FROM AR_InvoiceHeader
		Union
		SELECT CostCenter AS CostCenterID, Status FROM BudgetAllocationHeader
		Union
		SELECT CostCenter AS CostCenterID, Status FROM BudgetAppropriationHeader
		Union
		SELECT CostCenter AS CostCenterID, Status FROM BudgetReAppropriation
		Union
		SELECT CostCenterID, Status FROM Gate_OutwardHeader
		Union
		SELECT CostCenterID, Status FROM GL_TransactionDetail
		Union
		SELECT CostCenterID, Status FROM INV_StockTransactionHeader
		Union
		SELECT CostCenterID, Status FROM PUR_QuotationHeader
    ) AS tbl_CostCenter 
WHERE tbl_CostCenter.CostCenterID = @CostCenterID AND tbl_CostCenter.Status = 1 ";




        const string Query_SelectAllForSearch_WithPaging = @" SELECT [CostCenterID], [CostCenterName], [Status], [CreatedBy], [CreateDate], [UpdatedBy], [UpdateDate], [CreatedByValue], [UpdatedByValue], [IsActive], [HeadingCostCenterName], [IsAllowPosting], [AccountTypeID], [BSCostCenterID], ROW_NUMBER() Over (Order by CostCenterName) as 'Row' FROM [SET_CostCenter] ";
        const string Query_SelectAllForSearch_WithoutPaging = @" SELECT [CostCenterID], [CostCenterName], [Status], [CreatedBy], [CreateDate], [UpdatedBy], [UpdateDate], [CreatedByValue], [UpdatedByValue], [IsActive], [HeadingCostCenterName], [IsAllowPosting], [AccountTypeID], [BSCostCenterID] FROM [SET_CostCenter] ";


        const string Query_SelectAllForHeading = @" SELECT * FROM [SET_CostCenter] WHERE [Status] = 1  And IsAllowPosting = 0 AND ([IsActive] = @IsActive OR @IsActive is null) ";

        const string Query_GetHeaderData_byCCID = @"SELECT Distinct HeaderCostCenter.* FROM [SET_CostCenter]
		inner Join  SET_CostCenter HeaderCostCenter on HeaderCostCenter.CostCenterName = [SET_CostCenter].HeadingCostCenterName
	         And HeaderCostCenter.Status=1
             WHERE [SET_CostCenter].[Status] = 1  
             And [SET_CostCenter].IsAllowPosting = 1 
             AND ([SET_CostCenter].[IsActive] = 1) 
             AND [SET_CostCenter].CostCenterID = @CostCenterID";

        #endregion

        #region Class Methods

        public static DataTable CostCenterForAllocationReport()
        {
            DataTable dt = null;

            try
            {
               
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetCostCenterForReport).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetCostCenterByAccountCode(int companyid , int periodid , string accountcode)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", periodid);
                sqlparams[2] = new SqlParameter("@AC", accountcode);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetCostCenterByAccountCode, sqlparams);

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

        public static DataTable GetCostCenterByAccountCodeForAllocation(int companyid, int periodid, string accountcode)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", periodid);
                sqlparams[2] = new SqlParameter("@AC", accountcode);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetCostCenterByAccountCodeForAllocation, sqlparams);

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

        public static DataTable GetCostCenterByAccountCodeForReApp(int companyid, int periodid, string accountcode)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", periodid);
                sqlparams[2] = new SqlParameter("@AC", accountcode);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetCostCenterByAccountCodeForReApp, sqlparams);

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

        public static DataTable GetCostCenterList(string costCenterFilter, int isActive, string AllowPostingFilter, int DepartmentID)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CostCenterFilter", costCenterFilter);

                if(isActive >= 0)
                    sqlparams[1] = new SqlParameter("@IsActive", isActive);
                else
                    sqlparams[1] = new SqlParameter("@IsActive", DBNull.Value);

                if (DepartmentID > 0)
                    sb.Append(" and SET_Department.DepartmentID = " + DepartmentID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetCostCenterList, sb.ToString()) + AllowPostingFilter, sqlparams);

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

        public static DataRow GetDataByName(string costCenterName, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CostCenterName", costCenterName);

                DataSet ds = null;

                string Query_GetData = @" SELECT * FROM [SET_CostCenter] WHERE CostCenterName = @CostCenterName ";

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dr = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }

        public static bool IsCostCenterInUsed(int costCenterID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                sqlparams[0] = new SqlParameter("@CostCenterID", costCenterID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_CheckCostCenterInUsed, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0] != null && ds.Tables[0].Rows.Count > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static int GetBSCostCenter(int CostCenterID)
        {
            int BSCostCenter = 0;

            SqlParameter[] sqlparams = new SqlParameter[1];
            sqlparams[0] = new SqlParameter("@CostCenterID", CostCenterID);


            const string query = @" select top 1 isnull(BSCostCenterID,costcenterid) as BSCostCenterID from set_costcenter 
                                    where 
                                    --costcentername like '%bs%' and 
                                   set_costcenter.CostCenterID = @CostCenterID ";

//            const string query_test = @"
//                    select top 1 isnull(BSCostCenterID,costcenterid) as BSCostCenterID from set_costcenter WHERE 
//                    (CASE WHEN set_costcenter.CostCenterName LIKE '%KHI%' THEN 'KHI'
//                    WHEN set_costcenter.CostCenterName LIKE '%ISB%' THEN 'ISB'
//                    WHEN set_costcenter.CostCenterName LIKE '%QTY%' THEN 'QTY'
//                    WHEN set_costcenter.CostCenterName LIKE '%HYD%' THEN 'HYD'
//                    WHEN set_costcenter.CostCenterName LIKE '%HO%' THEN 'HO'
//                    WHEN set_costcenter.CostCenterName LIKE '%LHR%' THEN 'LHR'
//                    WHEN set_costcenter.CostCenterName LIKE '%FSD%' THEN 'FSD'
//                    WHEN set_costcenter.CostCenterName LIKE '%MUL%' THEN 'MUL'
//                     END ) IN (select CASE WHEN DocumentTypesGroup.GroupName LIKE '%KHI%' THEN 'KHI'
//                    WHEN DocumentTypesGroup.GroupName LIKE '%ISB%' THEN 'ISB'
//                    WHEN DocumentTypesGroup.GroupName LIKE '%QTY%' THEN 'QTY'
//                    WHEN DocumentTypesGroup.GroupName LIKE '%HYD%' THEN 'HYD'
//                    WHEN DocumentTypesGroup.GroupName LIKE '%HO%' THEN 'HO'
//                    WHEN DocumentTypesGroup.GroupName LIKE '%LHR%' THEN 'LHR'
//                    WHEN DocumentTypesGroup.GroupName LIKE '%FSD%' THEN 'FSD'
//                    WHEN DocumentTypesGroup.GroupName LIKE '%MUL%' THEN 'MUL'
//                     END 
//                    from set_documenttype,DocumentTypesGroup 
//                    where set_documenttype.GroupID = DocumentTypesGroup.GroupID 
//                    and set_documenttype.documenttypeid =  @documenttypeid)  and @documenttypeid = @documenttypeid
//                    and costcentername like '%bs%'";
            try
            {
                BSCostCenter = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, query, sqlparams.ToArray()));
            }
            catch
            {
                BSCostCenter = 0;
            }

            return BSCostCenter;
        }
        
        public static DataTable GetAllDataForSearch(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, SqlTransaction objTransaction = null)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

            try
            {

                //Query_SelectAll_WithPaging

                if (PageIndex != null)
                {
                    sqlparamsList.Add(new SqlParameter("@PageIndex", PageIndex));
                    sqlparamsList.Add(new SqlParameter("@PageSize", PageSize));
                    sqlparamsList.Add(new SqlParameter("@TotalRows", ReturnTotalRows));

                    SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                    sqlparamTotalRows.Direction = ParameterDirection.Output;

                    SqlParameter[] sqlparams = sqlparamsList.ToArray();

                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_SelectAllForSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAllForSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }

                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_CheckCostCenterInUsed + Query_SelectAllForSearch_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllForSearch_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }



            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static int GetDepartmentIDbyCostCenter(int CostCenterID)
        {
            int DepartmentID = 0;

            SqlParameter[] sqlparams = new SqlParameter[1];
            sqlparams[0] = new SqlParameter("@CostCenterID", CostCenterID);


            const string query = @" Select FK_DepartmentID as DepartmentID from SET_DepartmentsInCC 
inner Join SET_CostCenter on SET_CostCenter.CostCenterID=SET_DepartmentsInCC.FK_CostCenterID
where SET_CostCenter.CostCenterID=@CostCenterID ";
            try
            {
                DepartmentID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, query, sqlparams.ToArray()));
            }
            catch
            {
                DepartmentID = 0;
            }

            return DepartmentID;
        }


        public static DataTable GetAllActiveDataForHeading(Nullable<bool> isActive = true, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                if (isActive.HasValue)
                    sqlparams[0] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[0] = new SqlParameter("@IsActive", DBNull.Value);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAllForHeading, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAllForHeading, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllForHeading, sqlparams);

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

        public static DataRow GetHeaderCostCenterByCostCenterID(int costCenterID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CostCenterID", costCenterID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetHeaderData_byCCID, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetHeaderData_byCCID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetHeaderData_byCCID, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dr = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }


        public static DataTable GetAllActiveDataASC(Nullable<bool> isActive = true, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                if (isActive.HasValue)
                    sqlparams[0] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[0] = new SqlParameter("@IsActive", DBNull.Value);

                #region Query
                const string Query_SelectAllASC = @" SELECT * FROM [SET_CostCenter] WHERE [Status] = 1 AND ([IsActive] = @IsActive OR @IsActive is null) 
                       Order by CostCenterName asc
                       ";
                #endregion

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAllASC, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAllASC, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllASC, sqlparams);

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
    }



}
