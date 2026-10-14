using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Common.Utils;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.HRMS.Recruitment.Setup
{
    public partial class HRMS_CandidateProcessHeader
    {
        #region Constant Strings

        const string Query_SelectAll_WithOtherTablesWithPaging = @" SELECT 
                     [HRMS_CandidateProcessHeader].[FK_PositionRequirementID]
                     ,[SET_Designation].[DesignationName]
                     ,[HRMS_CandidateProcessHeader].[FK_CandidateID]
                     ,[HRMS_Candidate].[Name]
                     ,[HRMS_CandidateProcessHeader].[CandidateProcessHeaderID]
                     ,[HRMS_CandidateProcessHeader].[PositionRefNo]
                     ,[HRMS_CandidateProcessHeader].[IsActive]
                     ,[HRMS_CandidateProcessHeader].[Status]
                     ,[HRMS_CandidateProcessHeader].[CreatedBy]
                     ,[HRMS_CandidateProcessHeader].[CreateDate]
                     ,[HRMS_CandidateProcessHeader].[CreatedByValue]
                     ,[HRMS_CandidateProcessHeader].[UpdatedByValue]
                     ,[HRMS_CandidateProcessHeader].[UpdatedBy]
                     ,[HRMS_CandidateProcessHeader].[UpdateDate]
                     ,(SELECT COUNT([CandidateProcessDetailID]) FROM [HRMS_CandidateProcessDetail]
                     WHERE [HRMS_CandidateProcessDetail].[FK_CandidateProcessHeaderID] = [HRMS_CandidateProcessHeader].[CandidateProcessHeaderID])[TotalRateDetails]
                     , ROW_NUMBER() Over (Order by [HRMS_CandidateProcessHeader].[CandidateProcessHeaderID]) as 'Row'
                     FROM [HRMS_CandidateProcessHeader]
                     INNER JOIN  [HRMS_Candidate] ON [HRMS_Candidate].[CandidateID] = [HRMS_CandidateProcessHeader].[FK_CandidateID]
                     INNER JOIN  [HRMS_PositionRequirement] ON [HRMS_PositionRequirement].[PositionRequirementID] = [HRMS_CandidateProcessHeader].[FK_PositionRequirementID]
                     INNER JOIN [SET_Designation] ON [SET_Designation].[DesignationID] = [HRMS_PositionRequirement].[FK_DesignationID]";

        const string Query_SelectAll_WithOtherTablesWithOutPaging = @" SELECT 
                     [HRMS_CandidateProcessHeader].[FK_PositionRequirementID]
                     ,[SET_Designation].[DesignationName]
                     ,[HRMS_CandidateProcessHeader].[FK_CandidateID]
                     ,[HRMS_Candidate].[Name]
                     ,[HRMS_CandidateProcessHeader].[CandidateProcessHeaderID]
                     ,[HRMS_CandidateProcessHeader].[PositionRefNo]
                     ,[HRMS_CandidateProcessHeader].[IsActive]
                     ,[HRMS_CandidateProcessHeader].[Status]
                     ,[HRMS_CandidateProcessHeader].[CreatedBy]
                     ,[HRMS_CandidateProcessHeader].[CreateDate]
                     ,[HRMS_CandidateProcessHeader].[CreatedByValue]
                     ,[HRMS_CandidateProcessHeader].[UpdatedByValue]
                     ,[HRMS_CandidateProcessHeader].[UpdatedBy]
                     ,[HRMS_CandidateProcessHeader].[UpdateDate]
                     ,(SELECT COUNT([CandidateProcessDetailID]) FROM [HRMS_CandidateProcessDetail]
                     WHERE [HRMS_CandidateProcessDetail].[FK_CandidateProcessHeaderID] = [HRMS_CandidateProcessHeader].[CandidateProcessHeaderID])[TotalRateDetails]
                     FROM [HRMS_CandidateProcessHeader]
                     INNER JOIN  [HRMS_Candidate] ON [HRMS_Candidate].[CandidateID] = [HRMS_CandidateProcessHeader].[FK_CandidateID]
                     INNER JOIN  [HRMS_PositionRequirement] ON [HRMS_PositionRequirement].[PositionRequirementID] = [HRMS_CandidateProcessHeader].[FK_PositionRequirementID]
                     INNER JOIN [SET_Designation] ON [SET_Designation].[DesignationID] = [HRMS_PositionRequirement].[FK_DesignationID]";

        const string Query_GetData_byKeys = @" SELECT * FROM [HRMS_CandidateProcessHeader] 
                    WHERE [FK_CandidateID] = @FK_CandidateID AND [FK_PositionRequirementID] = @FK_PositionRequirementID AND [PositionRefNo] = @PositionRefNo AND [Status] = 1";

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

            public static bool InsertCPHeaderAndDetail(HRMS_CandidateProcessHeader CPH, List<HRMS_CandidateProcessDetail> CPD)
            {
                bool result = false;

                SqlTransaction sqltrans = SqlHelper.GetTransaction("Insert_HRMS_CPHeaderDetail");

                try
                {
                    CPH.CandidateProcessHeaderID = HRMS_CandidateProcessHeader.GetNewCandidateProcessHeaderID(sqltrans);

                    CPH.Insert(sqltrans);

                    int CandidateProcessDetailID = HRMS_CandidateProcessDetail.GetNewCandidateProcessDetailID(CPH.CandidateProcessHeaderID, sqltrans);

                    foreach (HRMS_CandidateProcessDetail cpd in CPD)
                    {
                        cpd.FK_CandidateProcessHeaderID = CPH.CandidateProcessHeaderID;
                        cpd.CandidateProcessDetailID = CandidateProcessDetailID++;
                        cpd.Insert(sqltrans);
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
                        SqlHelper.RollBackTransaction(sqltrans, "Insert_HRMS_CPHeaderDetail");
                }

                return result;
            }

            public static bool UpdateCPHeaderAndDetail(HRMS_CandidateProcessHeader CPH, List<HRMS_CandidateProcessDetail> CPD)
            {
                bool result = false;

                SqlTransaction sqltrans = SqlHelper.GetTransaction("Update_HRMS_CPHeaderDetail");

                try
                {
                    CPH.UpdateDataByKey(sqltrans);

                    HRMS_CandidateProcessDetail.DeleteDataByCandidateProcessHeaderID(sqltrans, CPH.CandidateProcessHeaderID);

                    int CandidateProcessDetailID = HRMS_CandidateProcessDetail.GetNewCandidateProcessDetailID(CPH.CandidateProcessHeaderID, sqltrans);

                    foreach (HRMS_CandidateProcessDetail cpd in CPD)
                    {
                        cpd.FK_CandidateProcessHeaderID = CPH.CandidateProcessHeaderID;
                        cpd.CandidateProcessDetailID = CandidateProcessDetailID++;
                        cpd.Insert(sqltrans);
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
                        SqlHelper.RollBackTransaction(sqltrans, "Update_HRMS_CPHeaderDetail");
                }

                return result;
            }

            public static bool DeleteCPHeaderAndDetail(HRMS_CandidateProcessHeader CPH, HRMS_CandidateProcessDetail CPD)
            {
                bool result = false;

                SqlTransaction sqltrans = SqlHelper.GetTransaction("Delete_HRMS_CPHeaderDetail");

                try
                {
                    HRMS_CandidateProcessHeader.DeleteDataByKey(sqltrans, (Guid)CPH.UpdatedBy, (DateTime)CPH.UpdateDate, CPH.UpdatedByValue, CPH.CandidateProcessHeaderID);

                    HRMS_CandidateProcessDetail.DeleteDataByKey_CandidateProcessHeaderID(sqltrans, CPD.FK_CandidateProcessHeaderID);

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
                        SqlHelper.RollBackTransaction(sqltrans, "Delete_HRMS_CPHeaderDetail");
                }

                return result;
            }

            public static DataRow GetDataByKeys(int candidateID, int positionRequirementID, string positionRefNo, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
            {
                DataRow dr = null;

                try
                {
                    SqlParameter[] sqlparams = new SqlParameter[3];
                    sqlparams[0] = new SqlParameter("@FK_CandidateID", candidateID);
                    sqlparams[1] = new SqlParameter("@FK_PositionRequirementID", positionRequirementID);
                    sqlparams[2] = new SqlParameter("@PositionRefNo", positionRefNo);

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
