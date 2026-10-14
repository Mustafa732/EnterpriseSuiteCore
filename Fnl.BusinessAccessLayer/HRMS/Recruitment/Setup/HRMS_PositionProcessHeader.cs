using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Common.Utils;
using FnI.BusinessAccessLayer.Setup;
using DAL.DataAccessLayer;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FnI.BusinessAccessLayer.HRMS.Recruitment.Setup
{
    public partial class HRMS_PositionProcessHeader
    {

        #region Constant Strings

        const string Query_SelectAll_WithOtherTablesWithPaging = @" SELECT 
                     [HRMS_PositionProcessHeader].[FK_PositionRequirementID]
                     ,[SET_Designation].[DesignationName]
                     ,[HRMS_PositionProcessHeader].[FK_ParentProcessID]
                     ,[HRMS_RecruitmentProcess].[ProcessName]
                     ,[HRMS_PositionProcessHeader].[PositionProcessHeaderID]
                     ,[HRMS_PositionProcessHeader].[PositionRefNo]
                     ,[HRMS_PositionProcessHeader].[IsActive]
                     ,[HRMS_PositionProcessHeader].[Status]
                     ,[HRMS_PositionProcessHeader].[CreatedBy]
                     ,[HRMS_PositionProcessHeader].[CreateDate]
                     ,[HRMS_PositionProcessHeader].[CreatedByValue]
                     ,[HRMS_PositionProcessHeader].[UpdatedByValue]
                     ,[HRMS_PositionProcessHeader].[UpdatedBy]
                     ,[HRMS_PositionProcessHeader].[UpdateDate]
                     ,(SELECT COUNT([PositionProcessDetailID]) FROM [HRMS_PositionProcessDetail]
                     WHERE [HRMS_PositionProcessDetail].[CompanyID] = [HRMS_PositionProcessHeader].[CompanyID] AND [HRMS_PositionProcessDetail].[PeriodID] = [HRMS_PositionProcessHeader].[PeriodID] 
                     AND [HRMS_PositionProcessDetail].[FK_PositionProcessHeaderID] = [HRMS_PositionProcessHeader].[PositionProcessHeaderID])[TotalRateDetails]
                     , ROW_NUMBER() Over (Order by [HRMS_PositionProcessHeader].[CompanyID],[HRMS_PositionProcessHeader].[PeriodID],[HRMS_PositionProcessHeader].[PositionProcessHeaderID]) as 'Row'
                     FROM [HRMS_PositionProcessHeader]
                     INNER JOIN  [HRMS_RecruitmentProcess] ON [HRMS_RecruitmentProcess].[RecruitmentID] = [HRMS_PositionProcessHeader].[FK_ParentProcessID]
                     INNER JOIN  [HRMS_PositionRequirement] ON [HRMS_PositionRequirement].[PositionRequirementID] = [HRMS_PositionProcessHeader].[FK_PositionRequirementID]
                     INNER JOIN [SET_Designation] ON [SET_Designation].[DesignationID] = [HRMS_PositionRequirement].[FK_DesignationID]";

        const string Query_SelectAll_WithOtherTablesWithOutPaging = @" SELECT 
                     [HRMS_PositionProcessHeader].[FK_PositionRequirementID]
                     ,[HRMS_Position].[Description]
                     ,[HRMS_PositionProcessHeader].[FK_ParentProcessID]
                     ,[HRMS_PositionProcessHeader].[PositionRefNo]
                     ,[HRMS_RecruitmentProcess].[ProcessName]
                     ,[HRMS_PositionProcessHeader].[PositionProcessHeaderID]
                     ,[HRMS_PositionProcessHeader].[IsActive]
                     ,[HRMS_PositionProcessHeader].[Status]
                     ,[HRMS_PositionProcessHeader].[CreatedBy]
                     ,[HRMS_PositionProcessHeader].[CreateDate]
                     ,[HRMS_PositionProcessHeader].[CreatedByValue]
                     ,[HRMS_PositionProcessHeader].[UpdatedByValue]
                     ,[HRMS_PositionProcessHeader].[UpdatedBy]
                     ,[HRMS_PositionProcessHeader].[UpdateDate]
                     ,(SELECT COUNT([PositionProcessDetailID]) FROM [HRMS_PositionProcessDetail]
                     WHERE [HRMS_PositionProcessDetail].[CompanyID] = [HRMS_PositionProcessHeader].[CompanyID] AND [HRMS_PositionProcessDetail].[PeriodID] = [HRMS_PositionProcessHeader].[PeriodID] 
                     AND [HRMS_PositionProcessDetail].[FK_PositionProcessHeaderID] = [HRMS_PositionProcessHeader].[PositionProcessHeaderID])[TotalRateDetails]
                     FROM [HRMS_PositionProcessHeader]
                     INNER JOIN  [HRMS_RecruitmentProcess] ON [HRMS_RecruitmentProcess].[RecruitmentID] = [HRMS_PositionProcessHeader].[FK_ParentProcessID]
                     INNER JOIN  [HRMS_PositionRequirement] ON [HRMS_PositionRequirement].[PositionRequirementID] = [HRMS_PositionProcessHeader].[FK_PositionRequirementID]
                     INNER JOIN [SET_Designation] ON [SET_Designation].[DesignationID] = [HRMS_PositionRequirement].[FK_DesignationID]";

        const string Query_GetData_byKeys = @" SELECT * FROM [HRMS_PositionProcessHeader] WHERE [CompanyID] = @CompanyID AND [PeriodID] = @PeriodID AND [FK_ParentProcessID] = @FK_ParentProcessID AND [FK_PositionRequirementID] = @FK_PositionRequirementID AND [PositionRefNo] = @PositionRefNo AND [Status] = 1";

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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_WithOtherTablesWithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithOtherTablesWithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_WithOtherTablesWithOutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static bool InsertPPHeaderAndDetail(HRMS_PositionProcessHeader PPH, List<HRMS_PositionProcessDetail> PPD)
        {
            bool result = false;

            SqlTransaction sqltrans = SqlHelper.GetTransaction("Insert_HRMS_PPHeaderDetail");

            try
            {
                PPH.PositionProcessHeaderID = HRMS_PositionProcessHeader.GetNewPositionProcessHeaderID(PPH.CompanyID, PPH.PeriodID, sqltrans);

                PPH.Insert(sqltrans);

                Int16 PositionProcessDetailID = (Int16)HRMS_PositionProcessDetail.GetNewPositionProcessDetailID(PPH.CompanyID,PPH.PositionProcessHeaderID, PPH.PeriodID, sqltrans);

                foreach (HRMS_PositionProcessDetail ppd in PPD)
                {
                    ppd.FK_PositionProcessHeaderID = PPH.PositionProcessHeaderID;
                    ppd.PositionProcessDetailID = PositionProcessDetailID++;
                    ppd.Insert(sqltrans);
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
                    SqlHelper.RollBackTransaction(sqltrans, "Insert_HRMS_PPHeaderDetail");
            }

            return result;
        }

        public static bool UpdatePPHeaderAndDetail(HRMS_PositionProcessHeader PPH, List<HRMS_PositionProcessDetail> PPD)
        {
            bool result = false;

            SqlTransaction sqltrans = SqlHelper.GetTransaction("Update_HRMS_PPHeaderDetail");

            try
            {
                PPH.UpdateDataByKey(sqltrans);

                HRMS_PositionProcessDetail.DeleteDataByKeyPositionProcessHeaderID(sqltrans, PPH.CompanyID, PPH.PeriodID, PPH.PositionProcessHeaderID);

                Int16 PositionProcessDetailID = (Int16)HRMS_PositionProcessDetail.GetNewPositionProcessDetailID(PPH.CompanyID, PPH.PositionProcessHeaderID, PPH.PeriodID, sqltrans);

                foreach (HRMS_PositionProcessDetail ppd in PPD)
                {
                    ppd.FK_PositionProcessHeaderID = PPH.PositionProcessHeaderID;
                    ppd.PositionProcessDetailID = PositionProcessDetailID++;
                    ppd.Insert(sqltrans);
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
                    SqlHelper.RollBackTransaction(sqltrans, "Update_HRMS_PPHeaderDetail");
            }

            return result;
        }

        public static bool DeletePPHeaderAndDetail(HRMS_PositionProcessHeader PPH, HRMS_PositionProcessDetail PPD)
        {
            bool result = false;

            SqlTransaction sqltrans = SqlHelper.GetTransaction("Delete_HRMS_PPHeaderDetail");

            try
            {
                HRMS_PositionProcessHeader.DeleteDataByKey(sqltrans, (Guid)PPH.UpdatedBy, (DateTime)PPH.UpdateDate, PPH.UpdatedByValue, PPH.CompanyID, PPH.PeriodID, PPH.PositionProcessHeaderID);

                HRMS_PositionProcessDetail.DeleteDataByKey_PositionProcessHeaderID(sqltrans, PPD.CompanyID, PPD.PeriodID, PPD.FK_PositionProcessHeaderID);

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
                    SqlHelper.RollBackTransaction(sqltrans, "Delete_HRMS_PPHeaderDetail");
            }

            return result;
        }

        public static DataRow GetDataByKeys(int companyID, int periodID, int processID, int positionRequirementID ,string positionRefNo, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FK_ParentProcessID", processID);
                sqlparams[3] = new SqlParameter("@FK_PositionRequirementID", positionRequirementID);
                sqlparams[4] = new SqlParameter("@PositionRefNo", positionRefNo);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byKeys, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byKeys, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byKeys, sqlparams);

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

        #endregion

    }
}
