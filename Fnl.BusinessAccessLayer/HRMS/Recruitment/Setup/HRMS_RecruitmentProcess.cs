using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Data.SqlClient;
using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;
using System.Data;

namespace FnI.BusinessAccessLayer.HRMS.Recruitment.Setup
{
   public partial class HRMS_RecruitmentProcess
   {
       #region Constant Strings

       const string Query_GetAllInterviewData = @"
select 
HRMS_PositionRequirement.PositionRequirementID
,JobDesignationName
, DepartmentName
,CityName
, HRMS_RecruitmentProcess.ProcessName
 ,HRMS_PositionRequirement.RefNo
,HRMS_PositionRequirement.RequestDate
,HRMS_PositionRequirement.Location
,HRMS_PositionRequirement.NoOfPositions
,HRMS_PositionRequirement.RequriedFromDate
,HRMS_PositionRequirement.RequriedToDate
,HRMS_PositionRequirement.MinExperience
,HRMS_PositionRequirement.MaxAgeLimit
,HRMS_PositionRequirement.EmploymentTypeID
,HRMS_PositionRequirement.MinQualificationID
,HRMS_PositionRequirement.Comments as PR_Comments
,HRMS_Candidate.CandidateNo
,HRMS_Candidate.Name
,HRMS_Candidate.Email
,HRMS_Candidate.Phone
,HRMS_Candidate.CNICNo
,HRMS_CandidateProcessDetail.Comments as CanPD_Comments
,HRMS_CandidateProcessDetail.ConductDate
 from HRMS_CandidateProcessDetail
inner join HRMS_CandidateProcessHeader on    HRMS_CandidateProcessHeader.CandidateProcessHeaderID = HRMS_CandidateProcessDetail.FK_CandidateProcessHeaderID  
and HRMS_CandidateProcessHeader.Status =1 
and HRMS_CandidateProcessDetail.status =1 
inner join HRMS_Candidate on HRMS_Candidate.CandidateID =  HRMS_CandidateProcessHeader.FK_CandidateID
and HRMS_Candidate.Status =1 
inner join HRMS_RecruitmentProcess on HRMS_RecruitmentProcess.RecruitmentID = HRMS_CandidateProcessDetail.FK_RecruitmentProcessID
and HRMS_RecruitmentProcess.Status =1 
and HRMS_RecruitmentProcess.CompanyID = @CompanyID
and HRMS_RecruitmentProcess.PeriodID = @PeriodID
inner join HRMS_PositionRequirement on HRMS_PositionRequirement.PositionRequirementID =  HRMS_CandidateProcessHeader.FK_PositionRequirementID
and HRMS_PositionRequirement.Status =1 
left join SET_Department on SET_Department.DepartmentID =  HRMS_PositionRequirement.FK_DepartmentID
and SET_Department.Status =1 
and HRMS_PositionRequirement.Status =1 
left join SET_City on SET_City.CityiD = HRMS_PositionRequirement.FK_CityID 
and SET_City.Status =1 
left join Set_JobDesignation on Set_JobDesignation.JobDesignationID =  HRMS_PositionRequirement.FK_DesignationID
and Set_JobDesignation.Status =1 
and Set_JobDesignation.CompanyID = HRMS_PositionRequirement.CompanyID
left join SET_OperatingUnit on SET_OperatingUnit.OperatingUnitID =  HRMS_PositionRequirement.FK_OperatingUnitID
and  HRMS_PositionRequirement.status =1 
where HRMS_PositionRequirement.CompanyID =  @CompanyID
and HRMS_PositionRequirement.PeriodID = @PeriodID
{0}
order by HRMS_PositionRequirement.PositionRequirementID,HRMS_CandidateProcessDetail.ConductDate
"; 

       const string Query_getAllInterviews = @"
select * from HRMS_RecruitmentProcess 
where ProcessName like '%interview%' and status =1 
and IsActive = 1
and CompanyID =  @CompanyID
and PeriodID = @PeriodID
order by RecruitmentID


";

       const string Query_SelectAllChildProcesses_ByParentID = @"
        WITH ChildProcess 
        (
         RecruitmentID, 
         ProcessName, 
         IsRoot, 
         SortingOrder, 
         ParentID, 
         IsMarking,
         [Status],
         CreateDate
         )
        AS
        (
	        -- Anchor member definition
	        SELECT RecruitmentID, ProcessName,IsRoot, SortingOrder, ParentID, IsMarking, [Status], CreateDate  FROM HRMS_RecruitmentProcess
	        where RecruitmentID = @RecruitmentID AND CompanyID = @CompanyID AND PeriodID = @PeriodID AND [Status] = 1 AND IsActive = 1 
	
	        UNION ALL
	        -- Recursive member definition
	        SELECT e.RecruitmentID, e.ProcessName,e.IsRoot, e.SortingOrder, e.ParentID, e.IsMarking, e.[Status], e.CreateDate 
	        FROM HRMS_RecruitmentProcess AS e
	        INNER JOIN ChildProcess AS d ON
	        e.ParentID = d.RecruitmentID
            WHERE e.[Status] = 1 AND e.IsActive = 1
        )
        -- Statement that executes the CTE

        SELECT * FROM ChildProcess ORDER BY ParentID,SortingOrder";

       #endregion

       #region Class Methods

       //Query_getAllInterviews

       public static DataTable GetInterviewData(Nullable<int> department,Nullable<int > requirmentprocess, Nullable<DateTime> datefrom, Nullable<DateTime> dateto, Nullable<int> companyid, Nullable<int> period, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       {
           DataTable dt = null;
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[2];
               sqlparams[0] = new SqlParameter("@CompanyID", companyid);
               sqlparams[1] = new SqlParameter("@PeriodID", period);
               DataSet ds;
               StringBuilder sb = new StringBuilder();
               sb.Append("  and HRMS_CandidateProcessDetail.ConductDate between cast( '" + datefrom.Value.ToString("dd-MMM-yy") + "' as datetime) and  cast( '" + dateto.Value.ToString("dd-MMM-yy") + "'  as datetime)");
               if (department > 0)
               {

                   sb.Append("  and SET_Department.DepartmentID = " + department + " ");
               }
             
               
               if (requirmentprocess > 0)
               {
                   sb.Append("  and HRMS_RecruitmentProcess.RecruitmentID = " + requirmentprocess + " ");

               }
               else
               if (requirmentprocess == 0)
               {
                   sb.Append("  and HRMS_RecruitmentProcess.ProcessName LIKE '%interview%'");
               }



               if (objTransaction != null)
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_GetAllInterviewData, sb), sqlparams);
               else if (objConnection != null)
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetAllInterviewData, sb), sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAllInterviewData, sb), sqlparams);

               if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                   dt = ds.Tables[0];

           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }
           //Query_JobRequisitionData
           return dt;
       }


       public static DataTable GetAllInterviewData(int companyID, int periodID)
       {
           DataTable dt = null;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
           //    sqlparams[2] = new SqlParameter("@RecruitmentID", parentProcessID);

               dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_getAllInterviews, sqlparams).Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }


       public static DataTable GetAllChildProcessByParentID(int companyID, int periodID, int parentProcessID)
       {
           DataTable dt = null;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@RecruitmentID", parentProcessID);

               dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllChildProcesses_ByParentID, sqlparams).Tables[0];
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
