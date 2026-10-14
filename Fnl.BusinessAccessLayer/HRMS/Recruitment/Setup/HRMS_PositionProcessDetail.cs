using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Common.Utils;
using FnI.BusinessAccessLayer.Setup;
using DAL.DataAccessLayer;
using System.Data;
using Microsoft.Data.SqlClient;

namespace FnI.BusinessAccessLayer.HRMS.Recruitment.Setup
{
    public partial class HRMS_PositionProcessDetail
    {
        #region Constant Strings

        const string Query_SelectAll_ByPositionProcessHeaderID = @"SELECT [FK_RecruitmentProcessID], [Comments], [Attachment], [ConductDate], [IsCompeleted], [IsActive], convert(bit,1) as [IsUsed] FROM [HRMS_PositionProcessDetail]
                                                                   Where [CompanyID] = @CompanyID AND [PeriodID] = @PeriodID AND [FK_PositionProcessHeaderID] = @FK_PositionProcessHeaderID ";

        const string Query_SelectAll_ByPositionProcessHeaderIDWithOtherTable = @" SELECT 
          [HRMS_PositionProcessDetail].[FK_RecruitmentProcessID]
         ,[HRMS_RecruitmentProcess].[ProcessName]
         ,[HRMS_RecruitmentProcess].[ParentID]
         ,[HRMS_RecruitmentProcess].[IsMarking]
         ,[HRMS_PositionProcessDetail].[Comments]
         ,[HRMS_PositionProcessDetail].[Attachment]
         ,[HRMS_PositionProcessDetail].[ConductDate]
         ,[HRMS_PositionProcessDetail].[IsCompeleted]
         ,[HRMS_PositionProcessDetail].[IsActive]
         ,[HRMS_PositionProcessDetail].[Status]
         ,[HRMS_PositionProcessDetail].[CreatedBy]
         ,[HRMS_PositionProcessDetail].[CreateDate]
         ,[HRMS_PositionProcessDetail].[CreatedByValue]
         ,[HRMS_PositionProcessDetail].[UpdatedByValue]
         ,[HRMS_PositionProcessDetail].[UpdatedBy]
         ,[HRMS_PositionProcessDetail].[UpdateDate]
         ,[HRMS_PositionProcessDetail].[FK_PositionProcessHeaderID]
         ,[HRMS_PositionProcessDetail].[FK_PositionProcessHeaderID]
         FROM [HRMS_PositionProcessDetail]
         INNER JOIN  [HRMS_RecruitmentProcess] ON [HRMS_PositionProcessDetail].[FK_RecruitmentProcessID] = [HRMS_RecruitmentProcess].[RecruitmentID]
         Where [HRMS_PositionProcessDetail].[CompanyID] = @CompanyID AND [HRMS_PositionProcessDetail].[PeriodID] = @PeriodID AND [HRMS_PositionProcessDetail].[FK_PositionProcessHeaderID] = @FK_PositionProcessHeaderID ";

        const string Query_SelectAll_ByPositionRefNoWithOtherTable = @" SELECT 
        [HRMS_PositionProcessDetail].[FK_RecruitmentProcessID]
        ,[HRMS_RecruitmentProcess].[ProcessName]
        ,[HRMS_RecruitmentProcess].[IsAppointmentProcess]
        ,[HRMS_RecruitmentProcess].[ParentID]
        ,[HRMS_PositionProcessDetail].[Comments]
        ,[HRMS_PositionProcessDetail].[Attachment]
        ,[HRMS_PositionProcessDetail].[ConductDate]
        ,[HRMS_PositionProcessDetail].[IsCompeleted]
        ,[HRMS_PositionProcessDetail].[IsActive]
        ,[HRMS_PositionProcessDetail].[Status]
        ,[HRMS_PositionProcessDetail].[CreatedBy]
        ,[HRMS_PositionProcessDetail].[CreateDate]
        ,[HRMS_PositionProcessDetail].[CreatedByValue]
        ,[HRMS_PositionProcessDetail].[UpdatedByValue]
        ,[HRMS_PositionProcessDetail].[UpdatedBy]
        ,[HRMS_PositionProcessDetail].[UpdateDate]
        ,[HRMS_PositionProcessDetail].[FK_PositionProcessHeaderID]
        FROM [HRMS_PositionProcessDetail]
        INNER JOIN  [HRMS_RecruitmentProcess] ON [HRMS_PositionProcessDetail].[FK_RecruitmentProcessID] = [HRMS_RecruitmentProcess].[RecruitmentID]
        WHERE [HRMS_PositionProcessDetail].[FK_PositionProcessHeaderID] IN ( SELECT [HRMS_PositionProcessHeader].[PositionProcessHeaderID]  FROM [HRMS_PositionProcessHeader]
        WHERE [HRMS_PositionProcessHeader].[PositionRefNo] =@PositionRefNo AND [HRMS_PositionProcessHeader].[Status] = 1 )";

        const string Query_PositionProcessReport = @" SELECT 
        [HRMS_PositionProcessDetail].[FK_RecruitmentProcessID]
        ,[HRMS_PositionProcessHeader].[PositionRefNo]
        ,[HRMS_PositionProcessHeader].[FK_PositionRequirementID]
        ,[HRMS_RecruitmentProcess].[ProcessName]
        ,(SELECT (SELECT ProcessName FROM HRMS_RecruitmentProcess RP where RP.RecruitmentID = [HRMS_RecruitmentProcess].ParentID) 
        FROM [HRMS_RecruitmentProcess] WHERE [HRMS_RecruitmentProcess].[RecruitmentID]=[HRMS_PositionProcessDetail].[FK_RecruitmentProcessID] )ParentProcessName
        ,[SET_Designation].[DesignationName]
        ,[SET_Business].[BusinessName]
        ,[SET_OperatingUnit].[OperatingUnitName]
        ,[SET_Department].[DepartmentName]
        ,[HRMS_PositionProcessDetail].[Comments]
        ,CONVERT(VARCHAR(10),[HRMS_PositionProcessDetail].[ConductDate],110) ConductDate
        ,'CompletedStatus'=
            CASE
			WHEN [HRMS_PositionProcessDetail].[IsCompeleted] = 1 THEN 'Completed'
            ELSE 'Pendding'
			END
        ,[HRMS_PositionProcessDetail].[IsActive]
        ,[HRMS_PositionProcessDetail].[Status]
        ,[HRMS_PositionProcessDetail].[CreatedBy]
        ,[HRMS_PositionProcessDetail].[CreateDate]
        ,[HRMS_PositionProcessDetail].[CreatedByValue]
        ,[HRMS_PositionProcessDetail].[UpdatedByValue]
        ,[HRMS_PositionProcessDetail].[UpdatedBy]
        ,[HRMS_PositionProcessDetail].[UpdateDate]
        ,[HRMS_PositionProcessDetail].[FK_PositionProcessHeaderID]
        FROM [HRMS_PositionProcessDetail]
        INNER JOIN  [HRMS_RecruitmentProcess] ON [HRMS_PositionProcessDetail].[FK_RecruitmentProcessID] = [HRMS_RecruitmentProcess].[RecruitmentID]
        INNER JOIN [HRMS_PositionProcessHeader] ON [HRMS_PositionProcessDetail].[FK_PositionProcessHeaderID] = [HRMS_PositionProcessHeader].[PositionProcessHeaderID]
        INNER JOIN [HRMS_PositionRequirement] ON [HRMS_PositionRequirement].[PositionRequirementID] = [HRMS_PositionProcessHeader].[FK_PositionRequirementID]
        INNER JOIN [SET_Business] ON [SET_Business].[BusinessId] = [HRMS_PositionRequirement].[FK_BusinessID]
        INNER JOIN [SET_OperatingUnit] ON [SET_OperatingUnit].[OperatingUnitId] = [HRMS_PositionRequirement].[FK_OperatingUnitID]
        INNER JOIN [SET_Department] ON [SET_Department].[DepartmentID] = [HRMS_PositionRequirement].[FK_DepartmentID]
        INNER JOIN [SET_Designation] ON [SET_Designation].[DesignationID] = [HRMS_PositionRequirement].[FK_DesignationID]
        WHERE [HRMS_PositionProcessDetail].[FK_PositionProcessHeaderID] IN ( SELECT [HRMS_PositionProcessHeader].[PositionProcessHeaderID]  FROM [HRMS_PositionProcessHeader]
        WHERE [HRMS_PositionProcessHeader].[PositionRefNo] =@PositionRefNo AND [HRMS_PositionProcessHeader].[Status] = 1 )";


        const string Query_ProcessPositionsReport = @" SELECT 
        [HRMS_PositionProcessDetail].[FK_RecruitmentProcessID]
        ,[HRMS_PositionProcessHeader].[PositionRefNo]
        ,[HRMS_PositionProcessHeader].[FK_PositionRequirementID]
        ,[HRMS_PositionProcessHeader].[FK_ParentProcessID]
        ,[HRMS_RecruitmentProcess].[ProcessName]
        ,(SELECT ProcessName FROM HRMS_RecruitmentProcess RP where RP.RecruitmentID = @FK_ParentProcessID) Process 
        ,(SELECT (SELECT ProcessName FROM HRMS_RecruitmentProcess RP where RP.RecruitmentID = [HRMS_RecruitmentProcess].ParentID) 
        FROM [HRMS_RecruitmentProcess] WHERE [HRMS_RecruitmentProcess].[RecruitmentID]=[HRMS_PositionProcessDetail].[FK_RecruitmentProcessID] )ParentProcessName
        ,[SET_Designation].[DesignationName]
        ,[SET_Business].[BusinessName]
        ,[SET_OperatingUnit].[OperatingUnitName]
        ,[SET_Department].[DepartmentName]
        ,[HRMS_PositionProcessDetail].[Comments]
        ,CONVERT(VARCHAR(10),[HRMS_PositionProcessDetail].[ConductDate],110) ConductDate
        ,'CompletedStatus'=
            CASE
			WHEN [HRMS_PositionProcessDetail].[IsCompeleted] = 1 THEN 'Completed'
            ELSE 'Pendding'
			END
        ,[HRMS_PositionProcessDetail].[IsActive]
        ,[HRMS_PositionProcessDetail].[Status]
        ,[HRMS_PositionProcessDetail].[CreatedBy]
        ,[HRMS_PositionProcessDetail].[CreateDate]
        ,[HRMS_PositionProcessDetail].[CreatedByValue]
        ,[HRMS_PositionProcessDetail].[UpdatedByValue]
        ,[HRMS_PositionProcessDetail].[UpdatedBy]
        ,[HRMS_PositionProcessDetail].[UpdateDate]
        ,[HRMS_PositionProcessDetail].[FK_PositionProcessHeaderID]
        FROM [HRMS_PositionProcessDetail]
        INNER JOIN  [HRMS_RecruitmentProcess] ON [HRMS_PositionProcessDetail].[FK_RecruitmentProcessID] = [HRMS_RecruitmentProcess].[RecruitmentID]
        INNER JOIN [HRMS_PositionProcessHeader] ON [HRMS_PositionProcessDetail].[FK_PositionProcessHeaderID] = [HRMS_PositionProcessHeader].[PositionProcessHeaderID]
        INNER JOIN [HRMS_PositionRequirement] ON [HRMS_PositionRequirement].[PositionRequirementID] = [HRMS_PositionProcessHeader].[FK_PositionRequirementID]
        INNER JOIN [SET_Business] ON [SET_Business].[BusinessId] = [HRMS_PositionRequirement].[FK_BusinessID]
        INNER JOIN [SET_OperatingUnit] ON [SET_OperatingUnit].[OperatingUnitId] = [HRMS_PositionRequirement].[FK_OperatingUnitID]
        INNER JOIN [SET_Department] ON [SET_Department].[DepartmentID] = [HRMS_PositionRequirement].[FK_DepartmentID]
        INNER JOIN [SET_Designation] ON [SET_Designation].[DesignationID] = [HRMS_PositionRequirement].[FK_DesignationID]
        WHERE [HRMS_PositionProcessDetail].[FK_PositionProcessHeaderID] IN ( SELECT [HRMS_PositionProcessHeader].[PositionProcessHeaderID]  FROM [HRMS_PositionProcessHeader]
        WHERE [HRMS_PositionProcessHeader].[FK_ParentProcessID] =@FK_ParentProcessID AND [HRMS_PositionProcessHeader].[Status] = 1 )";



        const string Query_Delete_ByKeyPositionProcessHeaderIDU = @" Update [HRMS_PositionProcessDetail] Set [Status] = @Status WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [FK_PositionProcessHeaderID] = @FK_PositionProcessHeaderID ";

        const string Query_Delete_ByKeyPositionProcessHeaderID = @" Delete from [HRMS_PositionProcessDetail] WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [FK_PositionProcessHeaderID] = @FK_PositionProcessHeaderID ";
        
        #endregion

        #region Class Methods

        public static DataTable GetAllPositionProcessDetailByPositionProcessHeaderID(int companyID, int periodID, int positionProcessHeaderId)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FK_PositionProcessHeaderID", positionProcessHeaderId);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_ByPositionProcessHeaderID, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDetailWithOtherTablesByPositionProcessHeaderID(int companyID, int periodID, int positionProcessHeaderId)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FK_PositionProcessHeaderID", positionProcessHeaderId);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_ByPositionProcessHeaderIDWithOtherTable, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDetailWithOtherTablesByPositionRefNo(string positionRefNo)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@PositionRefNo", positionRefNo);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_ByPositionRefNoWithOtherTable, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static bool DeleteDataByKeyPositionProcessHeaderID(SqlTransaction objTransaction, int companyID, int periodID, int positionProcessHeaderId)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FK_PositionProcessHeaderID", positionProcessHeaderId);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByKeyPositionProcessHeaderID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByKeyPositionProcessHeaderID, sqlparams) > 0)
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

        public static bool DeleteDataByKey_PositionProcessHeaderID(SqlTransaction objTransaction, int companyID, int periodID, long positionProcessHeaderId)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FK_PositionProcessHeaderID", positionProcessHeaderId);
                sqlparams[3] = new SqlParameter("@Status", false);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByKeyPositionProcessHeaderIDU, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByKeyPositionProcessHeaderIDU, sqlparams) > 0)
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

        public static DataTable PositionProcessReport(string positionRefNo, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@PositionRefNo", positionRefNo);


                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_PositionProcessReport, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_PositionProcessReport, sqlparams);

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

        public static DataTable ProcessPositionsReport(int parentProcessID, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@FK_ParentProcessID", parentProcessID);


                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_ProcessPositionsReport, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_ProcessPositionsReport, sqlparams);

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

        #endregion
    }
}
