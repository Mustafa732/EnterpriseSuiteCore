using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;
using System.Data;

namespace FnI.BusinessAccessLayer.HRMS.Recruitment.Setup
{
    public partial class HRMS_CandidateProcessDetail
    {
        #region Constant Strings

        const string Query_SelectAll_ByCandidateProcessHeaderID = @"SELECT [FK_RecruitmentProcessID], [Comments], [Attachment], [ConductDate], [IsCompeleted], [IsActive], [IsAppointed], convert(bit,1) as [IsUsed] FROM [HRMS_CandidateProcessDetail]
                                                                   Where [FK_CandidateProcessHeaderID] = @FK_CandidateProcessHeaderID ";

        const string Query_SelectAll_ByCandidateProcessHeaderIDWithOtherTable = @" SELECT 
 [HRMS_CandidateProcessDetail].[FK_CandidateProcessHeaderID]            
, [HRMS_CandidateProcessDetail].[CandidateProcessDetailID]
               , [HRMS_CandidateProcessDetail].[FK_RecruitmentProcessID]
             ,[HRMS_RecruitmentProcess].[ProcessName]
             ,[HRMS_CandidateProcessDetail].[Comments]
             ,[HRMS_CandidateProcessDetail].[Attachment]
             ,[HRMS_CandidateProcessDetail].[AttachmentName]
             ,[HRMS_CandidateProcessDetail].[ConductDate]
             ,[HRMS_CandidateProcessDetail].[IsCompeleted]
             ,[HRMS_CandidateProcessDetail].[IsActive]
             ,[HRMS_CandidateProcessDetail].[Status]
             ,[HRMS_CandidateProcessDetail].[CreatedBy]
             ,[HRMS_CandidateProcessDetail].[CreateDate]
             ,[HRMS_CandidateProcessDetail].[CreatedByValue]
             ,[HRMS_CandidateProcessDetail].[UpdatedByValue]
             ,[HRMS_CandidateProcessDetail].[UpdatedBy]
             ,[HRMS_CandidateProcessDetail].[UpdateDate]
             ,[HRMS_CandidateProcessDetail].[FK_CandidateProcessHeaderID]
             ,[HRMS_CandidateProcessDetail].[IsAppointed]
             FROM [HRMS_CandidateProcessDetail]
             INNER JOIN  [HRMS_RecruitmentProcess] ON [HRMS_CandidateProcessDetail].[FK_RecruitmentProcessID] = [HRMS_RecruitmentProcess].[RecruitmentID]
             Where [HRMS_CandidateProcessDetail].[FK_CandidateProcessHeaderID] = @FK_CandidateProcessHeaderID ";

        const string Query_SelectAll_ByPositionRefNoWithOtherTable = @" SELECT 
             [HRMS_CandidateProcessDetail].[FK_RecruitmentProcessID]
             ,[HRMS_RecruitmentProcess].[ProcessName]
             ,[HRMS_CandidateProcessDetail].[Comments]
             ,[HRMS_CandidateProcessDetail].[Attachment]
             ,[HRMS_CandidateProcessDetail].[ConductDate]
             ,[HRMS_CandidateProcessDetail].[IsCompeleted]
             ,[HRMS_CandidateProcessDetail].[IsActive]
             ,[HRMS_CandidateProcessDetail].[Status]
             ,[HRMS_CandidateProcessDetail].[CreatedBy]
             ,[HRMS_CandidateProcessDetail].[CreateDate]
             ,[HRMS_CandidateProcessDetail].[CreatedByValue]
             ,[HRMS_CandidateProcessDetail].[UpdatedByValue]
             ,[HRMS_CandidateProcessDetail].[UpdatedBy]
             ,[HRMS_CandidateProcessDetail].[UpdateDate]
             ,[HRMS_CandidateProcessDetail].[FK_CandidateProcessHeaderID]
             ,[HRMS_CandidateProcessDetail].[IsAppointed]
             FROM [HRMS_CandidateProcessDetail]
             INNER JOIN  [HRMS_RecruitmentProcess] ON [HRMS_CandidateProcessDetail].[FK_RecruitmentProcessID] = [HRMS_RecruitmentProcess].[RecruitmentID]
             WHERE [HRMS_CandidateProcessDetail].[FK_CandidateProcessHeaderID] IN ( SELECT [HRMS_CandidateProcessHeader].[CandidateProcessHeaderID]  FROM [HRMS_CandidateProcessHeader]
             WHERE [HRMS_CandidateProcessHeader].[PositionRefNo] =@PositionRefNo AND [HRMS_CandidateProcessHeader].[Status] = 1 )";


        const string Query_CandidateProcessReport = @"SELECT
            [HRMS_Candidate].[Name]
            ,[HRMS_Candidate].[CandidateNo]
            ,[HRMS_CandidateProcessDetail].[FK_RecruitmentProcessID]
            ,[HRMS_RecruitmentProcess].[ProcessName]
            ,(SELECT (SELECT ProcessName FROM HRMS_RecruitmentProcess RP where RP.RecruitmentID = [HRMS_RecruitmentProcess].ParentID) 
            FROM [HRMS_RecruitmentProcess] WHERE [HRMS_RecruitmentProcess].[RecruitmentID]=[HRMS_CandidateProcessDetail].[FK_RecruitmentProcessID] )ParentProcessName
            ,[HRMS_CandidateProcessHeader].[PositionRefNo]
            ,[SET_Designation].[DesignationName]
            ,[SET_Business].[BusinessName]
            ,[SET_OperatingUnit].[OperatingUnitName]
            ,[SET_Department].[DepartmentName]
            ,[HRMS_CandidateProcessDetail].[Comments]
            ,[HRMS_CandidateProcessDetail].[Attachment]
            ,CONVERT(VARCHAR(10),[HRMS_CandidateProcessDetail].[ConductDate],110) ConductDate
            ,'CompletedStatus'=
             CASE
				WHEN [HRMS_CandidateProcessDetail].[IsCompeleted] = 1 THEN 'Compeleted'
                ELSE 'Pendding'
				END
            ,[HRMS_CandidateProcessDetail].[IsCompeleted]
            ,[HRMS_CandidateProcessDetail].[IsActive]
            ,[HRMS_CandidateProcessDetail].[Status]
            ,[HRMS_CandidateProcessDetail].[CreatedBy]
            ,[HRMS_CandidateProcessDetail].[CreateDate]
            ,[HRMS_CandidateProcessDetail].[CreatedByValue]
            ,[HRMS_CandidateProcessDetail].[UpdatedByValue]
            ,[HRMS_CandidateProcessDetail].[UpdatedBy]
            ,[HRMS_CandidateProcessDetail].[UpdateDate]
            ,[HRMS_CandidateProcessDetail].[FK_CandidateProcessHeaderID]
            FROM [HRMS_CandidateProcessDetail]
            INNER JOIN [HRMS_RecruitmentProcess] ON [HRMS_CandidateProcessDetail].[FK_RecruitmentProcessID] = [HRMS_RecruitmentProcess].[RecruitmentID]
            INNER JOIN [HRMS_CandidateProcessHeader] ON [HRMS_CandidateProcessDetail].[FK_CandidateProcessHeaderID] = [HRMS_CandidateProcessHeader].[CandidateProcessHeaderID]
            INNER JOIN [HRMS_PositionRequirement] ON [HRMS_PositionRequirement].[PositionRequirementID] = [HRMS_CandidateProcessHeader].[FK_PositionRequirementID]
            INNER JOIN [SET_Business] ON [SET_Business].[BusinessId] = [HRMS_PositionRequirement].[FK_BusinessID]
            INNER JOIN [SET_OperatingUnit] ON [SET_OperatingUnit].[OperatingUnitId] = [HRMS_PositionRequirement].[FK_OperatingUnitID]
            INNER JOIN [SET_Department] ON [SET_Department].[DepartmentID] = [HRMS_PositionRequirement].[FK_DepartmentID] 
            INNER JOIN [HRMS_Candidate] ON [HRMS_Candidate].[CandidateID] = [HRMS_CandidateProcessHeader].[FK_CandidateID]
            INNER JOIN [SET_Designation] ON [SET_Designation].[DesignationID] = [HRMS_PositionRequirement].[FK_DesignationID]
            WHERE [HRMS_CandidateProcessDetail].[FK_CandidateProcessHeaderID] IN (SELECT CandidateProcessHeaderID FROM HRMS_CandidateProcessHeader WHERE  FK_CandidateID = @FK_CandidateID )
            AND [HRMS_CandidateProcessDetail].[Status] = 1";

        const string Query_CandidateProcessReport_New = @"Select 
HRMS_Candidate.CandidateNo, 
HRMS_Candidate.Name as CandidateName,
HRMS_Candidate.Email, HRMS_Candidate.CNICNo,
HRMS_Candidate.AddressLine1,
HRMS_Candidate.Age,
HRMS_Candidate.MobileNo2,
HRMS_Candidate.Education,
HRMS_Candidate.TotalExperience,

HRMS_PositionRequirement.RefNo PositionRefNo,
CONVERT(VARCHAR(10),[HRMS_PositionRequirement].[RequestDate],110) RequestDate,
--HRMS_PositionRequirement.RequestDate,
HRMS_PositionRequirement.Comments,
Case
When [HRMS_PositionRequirement].[MinQualificationID] = 1 THEN 'MatriculationOrEquivalent'
When [HRMS_PositionRequirement].[MinQualificationID] = 2 THEN 'IntermediateOrEquivalent'
When [HRMS_PositionRequirement].[MinQualificationID] = 3 THEN 'GraduationOrEquivalent'
When [HRMS_PositionRequirement].[MinQualificationID] = 4 THEN 'PostGraduationOrEquivalent'
When [HRMS_PositionRequirement].[MinQualificationID] = 5 THEN 'DoctorateOrEquivalent'
END AS MinQualification,
HRMS_PositionRequirement.MinExperience, 
HRMS_PositionRequirement.MaxAgeLimit,
[SET_Business].BusinessName,
[SET_OperatingUnit].[OperatingUnitName],
[SET_Department].[DepartmentName],
[SET_Designation].DesignationName,

[HRMS_RecruitmentProcess].ProcessName,
CONVERT(VARCHAR(10),[HRMS_CandidateProcessDetail].[ConductDate],110) ConductDate,
--HRMS_CandidateProcessDetail.ConductDate,
HRMS_CandidateProcessDetail.Comments as Remarks
,'CompletedStatus'=
CASE
WHEN [HRMS_CandidateProcessDetail].[IsCompeleted] = 1 THEN 'Compeleted'
ELSE 'Pendding'
END

From HRMS_CandidateProcessHeader
Inner join HRMS_CandidateProcessDetail on HRMS_CandidateProcessDetail.FK_CandidateProcessHeaderID = HRMS_CandidateProcessHeader.CandidateProcessHeaderID
INNER JOIN [HRMS_RecruitmentProcess] ON [HRMS_CandidateProcessDetail].[FK_RecruitmentProcessID] = [HRMS_RecruitmentProcess].[RecruitmentID]
INNER JOIN  [HRMS_PositionProcessDetail] ON [HRMS_PositionProcessDetail].[FK_RecruitmentProcessID] = [HRMS_RecruitmentProcess].[RecruitmentID]
Inner join HRMS_Candidate on HRMS_Candidate.CandidateID = HRMS_CandidateProcessHeader.FK_CandidateID
Inner join HRMS_PositionRequirement on HRMS_PositionRequirement.RefNo = HRMS_CandidateProcessHeader.[PositionRefNo]
INNER JOIN [SET_Business] ON [SET_Business].[BusinessId] = [HRMS_PositionRequirement].[FK_BusinessID]
INNER JOIN [SET_OperatingUnit] ON [SET_OperatingUnit].[OperatingUnitId] = [HRMS_PositionRequirement].[FK_OperatingUnitID]
INNER JOIN [SET_Department] ON [SET_Department].[DepartmentID] = [HRMS_PositionRequirement].[FK_DepartmentID] 
INNER JOIN [SET_Designation] ON [SET_Designation].[DesignationID] = [HRMS_PositionRequirement].[FK_DesignationID]
Where HRMS_CandidateProcessHeader.Status = 1
And HRMS_PositionRequirement.RefNo = @RefNo
";

        const string Query_ProcessCandidatesReport = @"SELECT
            [HRMS_Candidate].[Name]
            ,[HRMS_Candidate].[CandidateNo]
            ,[HRMS_CandidateProcessDetail].[FK_RecruitmentProcessID]
            ,[HRMS_RecruitmentProcess].[ProcessName]
            ,(SELECT (SELECT ProcessName FROM HRMS_RecruitmentProcess RP where RP.RecruitmentID = [HRMS_RecruitmentProcess].ParentID) 
            FROM [HRMS_RecruitmentProcess] WHERE [HRMS_RecruitmentProcess].[RecruitmentID]=[HRMS_CandidateProcessDetail].[FK_RecruitmentProcessID] )ParentProcessName
            ,[HRMS_CandidateProcessHeader].[PositionRefNo]
            ,[SET_Designation].[DesignationName]
            ,[SET_Business].[BusinessName]
            ,[SET_OperatingUnit].[OperatingUnitName]
            ,[SET_Department].[DepartmentName]
            ,[HRMS_CandidateProcessDetail].[Comments]
            ,[HRMS_CandidateProcessDetail].[Attachment]
            ,CONVERT(VARCHAR(10),[HRMS_CandidateProcessDetail].[ConductDate],110) ConductDate
            ,'CompletedStatus'=
             CASE
				WHEN [HRMS_CandidateProcessDetail].[IsCompeleted] = 1 THEN 'Compeleted'
                ELSE 'Pendding'
				END
            ,[HRMS_CandidateProcessDetail].[IsCompeleted]
            ,[HRMS_CandidateProcessDetail].[IsActive]
            ,[HRMS_CandidateProcessDetail].[Status]
            ,[HRMS_CandidateProcessDetail].[CreatedBy]
            ,[HRMS_CandidateProcessDetail].[CreateDate]
            ,[HRMS_CandidateProcessDetail].[CreatedByValue]
            ,[HRMS_CandidateProcessDetail].[UpdatedByValue]
            ,[HRMS_CandidateProcessDetail].[UpdatedBy]
            ,[HRMS_CandidateProcessDetail].[UpdateDate]
            ,[HRMS_CandidateProcessDetail].[FK_CandidateProcessHeaderID]
            FROM [HRMS_CandidateProcessDetail]
            INNER JOIN [HRMS_RecruitmentProcess] ON [HRMS_CandidateProcessDetail].[FK_RecruitmentProcessID] = [HRMS_RecruitmentProcess].[RecruitmentID]
            INNER JOIN [HRMS_CandidateProcessHeader] ON [HRMS_CandidateProcessDetail].[FK_CandidateProcessHeaderID] = [HRMS_CandidateProcessHeader].[CandidateProcessHeaderID]
            INNER JOIN [HRMS_PositionRequirement] ON [HRMS_PositionRequirement].[PositionRequirementID] = [HRMS_CandidateProcessHeader].[FK_PositionRequirementID]
            INNER JOIN [SET_Business] ON [SET_Business].[BusinessId] = [HRMS_PositionRequirement].[FK_BusinessID]
            INNER JOIN [SET_OperatingUnit] ON [SET_OperatingUnit].[OperatingUnitId] = [HRMS_PositionRequirement].[FK_OperatingUnitID]
            INNER JOIN [SET_Department] ON [SET_Department].[DepartmentID] = [HRMS_PositionRequirement].[FK_DepartmentID] 
            INNER JOIN [HRMS_Candidate] ON [HRMS_Candidate].[CandidateID] = [HRMS_CandidateProcessHeader].[FK_CandidateID]
            INNER JOIN [SET_Designation] ON [SET_Designation].[DesignationID] = [HRMS_PositionRequirement].[FK_DesignationID]
            WHERE [HRMS_CandidateProcessDetail].[FK_RecruitmentProcessID]= @FK_RecruitmentProcessID
            AND [HRMS_CandidateProcessDetail].[Status] = 1"; 

        const string Query_Delete_ByCandidateProcessHeaderIDU = @" Update [HRMS_CandidateProcessDetail] Set [Status] = @Status WHERE [FK_CandidateProcessHeaderID] = @FK_CandidateProcessHeaderID ";

        const string Query_Delete_ByCandidateProcessHeaderID = @" Delete from [HRMS_CandidateProcessDetail] WHERE [FK_CandidateProcessHeaderID] = @FK_CandidateProcessHeaderID ";

        #endregion

        #region Class Methods

        public static DataTable GetAllCandidateProcessDetailByCandidateProcessHeaderID(int candidateProcessHeaderID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@FK_CandidateProcessHeaderID", candidateProcessHeaderID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_ByCandidateProcessHeaderID, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDetailWithOtherTablesByCandidateProcessHeaderID(int candidateProcessHeaderID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@FK_CandidateProcessHeaderID", candidateProcessHeaderID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_ByCandidateProcessHeaderIDWithOtherTable, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDetailWithOtherTablesByPositionRefNo(int positionRefNo)
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

        public static bool DeleteDataByCandidateProcessHeaderID(SqlTransaction objTransaction, int candidateProcessHeaderID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[2] = new SqlParameter("@FK_CandidateProcessHeaderID", candidateProcessHeaderID);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByCandidateProcessHeaderID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByCandidateProcessHeaderID, sqlparams) > 0)
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

        public static bool DeleteDataByKey_CandidateProcessHeaderID(SqlTransaction objTransaction, int candidateProcessHeaderID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@FK_CandidateProcessHeaderID", candidateProcessHeaderID);
                sqlparams[1] = new SqlParameter("@Status", false);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByCandidateProcessHeaderIDU, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByCandidateProcessHeaderIDU, sqlparams) > 0)
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

        public static DataTable CandidateProcessReport(int candidateID, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@FK_CandidateID", candidateID);


                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_CandidateProcessReport, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_CandidateProcessReport, sqlparams);

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

        public static DataTable CandidateProcessReport_New(string PositionRef, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@RefNo", PositionRef);


                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_CandidateProcessReport_New, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_CandidateProcessReport_New, sqlparams);

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

        public static DataTable ProcessCandidatesReport(int processID, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@FK_RecruitmentProcessID", processID);


                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_ProcessCandidatesReport, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_ProcessCandidatesReport, sqlparams);

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
