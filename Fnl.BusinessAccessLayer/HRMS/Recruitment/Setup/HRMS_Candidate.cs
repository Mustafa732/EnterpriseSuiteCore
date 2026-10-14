using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.HRMS.Recruitment.Setup
{
    public partial class HRMS_Candidate
    {
        #region Constant Strings

        const string Query_allShortListed = @"
select  CandidateID ,CandidateNo +' - '+ Name as Candidate  
from [HRMS_Candidate] 
where  [IsShortListCandidate] =  1 
and Status = 1
";

        const string Query_ShortlistCandidateReport = @"
SELECT  
		  SET_Department.DepartmentName AS REQ_DEP
		 ,SET_Section.SectionName AS REQ_SECTION
		 ,SET_OperatingUnit.OperatingUnitName AS REQ_OPERATINGUNIT
		 ,SET_Business.BusinessName AS REQ_BUSINESS
	     ,Set_JobDesignation.JobDesignationName AS REQ_JOBDESIGNATION
	 	 ,[HRMS_PositionRequirement].RefNo
		 ,[HRMS_PositionRequirement].Location
		 ,[HRMS_PositionRequirement].MaxAgeLimit
		 ,[HRMS_PositionRequirement].MinExperience
		 ,[HRMS_PositionRequirement].Comments
		 ,convert (varchar(20), [HRMS_PositionRequirement].RequestDate ,106 )  as RequestDate
		 ,convert (varchar(20), [HRMS_PositionRequirement].RequriedFromDate , 106)  as RequriedFromDate
		, convert (varchar(20), [HRMS_PositionRequirement].RequriedToDate , 106 )  as RequriedToDate
		 ,[HRMS_PositionRequirement].NoOfPositions
		 ,[HRMS_PositionRequirement].JobDescription
		 ,[HRMS_PositionRequirement].Justification
		 ,case  [HRMS_PositionRequirement].EmployeeStatusID 
			when 1 then 'Regular' 
			when 2 then 'PartTime' 
			when 3 then 'FullTime' 
			when 4 then 'Contract' 
			when 5 then 'Temporary' 
			when 6 then 'SeasonalContract'
			end 
		 as RequirementStatus
		 ,
			 case  [HRMS_PositionRequirement].EmploymentTypeID 
			 when 1 then 'Permanent' 
			 when 2 then 'Contract' 
			 when 3 then 'DailyWages' 
			 when 4 then 'SeasonalContract' 
			 when 5 then 'SeasonalDailyWages' 
			 when 6 then 'Trainee'
			end 
		as RequirementType   
		,[CandidateID]
		,[CandidateNo]
		,[Name]
		,[Email]
		,[Phone]
		,[CNICNo]
		,[HRMS_Candidate].[AddressLine1]
		,[HRMS_Candidate].[AddressLine2]
		,[HRMS_Candidate].[AddressLine3]
		,[FK_PositionRequirementID]
		,[Position]
		,convert(varchar(20), [DateOfBirth] , 106) as DateOFBirth
		,[Age]
		,[MobileNo2]
		,SET_CITY.CityName
		,SET_Country.CountryName
		,case  ProvinceID 
			  when  1  then 'Balochistan'
			  when  2  then 'KPK'
			  when  3  then 'Punjab'
			  when  4  then 'Sindh'
			  end as Province
		,[Education]
		,[Specialization]
		,[TotalExperience]
		,[CompaniesServed]
		,[LastDesignation]
		,[ExpectedSalary]
		,[LastSalary]
		,[InformedBy]
		,[IsShortListCandidate]
  FROM [HRMS-Testing].[dbo].[HRMS_Candidate]
  
    left join [HRMS_PositionRequirement] on  [HRMS_PositionRequirement].[PositionRequirementID] = [HRMS_Candidate].FK_PositionRequirementID
    and [HRMS_PositionRequirement].Status =1 
    and [HRMS_PositionRequirement].CompanyID = @CompanyID
    and [HRMS_PositionRequirement].PeriodID = @PeriodID
  
  
    left join  SET_City on SET_City.CityID = [HRMS_Candidate].CityID
    and SET_City.Status = 1
  
    left join SET_Country on SET_Country.CountryID = [HRMS_Candidate].CountryID
    and SET_Country.Status =1 
  
	left join SET_JobDesignation on SET_JobDesignation.JobDesignationID = [HRMS_PositionRequirement].FK_DesignationID
	and SET_JobDesignation.status = 1 

	left join SET_Department on SET_Department.DepartmentID = HRMS_PositionRequirement.FK_DepartmentID
	and SET_Department.Status = 1

	left join SET_Section on SET_Section.SectionID = HRMS_PositionRequirement.FK_SectionID 
	and Set_section.Status = 1 

	left join Set_OperatingUnit on SET_OperatingUnit.OperatingUnitID =  HRMS_PositionRequirement.FK_OperatingUnitID
	and SET_OperatingUnit.Status =1 

	left join SET_Business  on SET_Business.BusinessID =  HRMS_PositionRequirement.FK_BusinessID
	and SET_Business.Status =1 

  where [IsShortListCandidate] =  1
  and [HRMS_Candidate].status = 1

{0} 

";

        const string Query_GetData_byCandidateNo = @" SELECT * FROM [HRMS_Candidate] WHERE [CandidateNo] = @CandidateNo AND [Status] = 1 AND ([IsActive] = @IsActive OR @IsActive is null)";

        const string Query_UpdateCandidateToShortListed = @"
UPDATE HRMS_Candidate SET  IsShortListCandidate = 1 
WHERE CandidateID = @CandidateID 
";

        const string Query_UpdateIsAppointed_byCandidateNo = @" UPDATE [HRMS_Candidate] SET [IsAppointed] = 1 WHERE [CandidateNo] = @CandidateNo";

        const string Query_CandidateData = @"select 
ProcessName
,CandidateID
,CandidateNo
,Name
,Email
,Phone
,CNICNo
,AddressLine1
,AddressLine2
,AddressLine3
,HRMS_CandidateProcessDetail.IsAppointed
, case 
	when HRMS_CandidateProcessDetail.IsAppointed = 1 then 'Yes' else 'No'
	end 
as Appointed
,DesignationName
,RefNo
,RequestDate
,Location
,DepartmentName
 
  from HRMS_Candidate

inner join  HRMS_CandidateProcessHeader on HRMS_CandidateProcessHeader.FK_CandidateID = HRMS_Candidate.CandidateID
and HRMS_CandidateProcessHeader.Status = 1
and HRMS_Candidate.status =1 
inner join  HRMS_PositionRequirement on HRMS_CandidateProcessHeader.FK_PositionRequirementID = HRMS_PositionRequirement.PositionRequirementID
and HRMS_CandidateProcessHeader.status =1 
inner join set_Designation on set_Designation.DesignationID = HRMS_PositionRequirement.FK_DesignationID
and set_Designation.CompanyID = HRMS_PositionRequirement.CompanyID
and HRMS_PositionRequirement.Status = 1
and set_Designation.Status =1 
inner join SET_Department on SET_Department.DepartmentID =  HRMS_PositionRequirement.FK_DepartmentID
and SET_Department.Status = 1 
inner join HRMS_CandidateProcessDetail on HRMS_CandidateProcessDetail.FK_CandidateProcessHeaderID =  HRMS_CandidateProcessHeader.CandidateProcessHeaderID
and HRMS_CandidateProcessDetail.Status =1 
inner join HRMS_RecruitmentProcess on HRMS_RecruitmentProcess.RecruitmentID =  HRMS_CandidateProcessDetail.FK_RecruitmentProcessID
and HRMS_RecruitmentProcess.status = 1
and  HRMS_RecruitmentProcess.CompanyID = set_Designation.CompanyID 

where set_Designation.CompanyID  = @CompanyID
and HRMS_RecruitmentProcess.PeriodID = @PeriodID
{0}


--AND (HRMS_Candidate.IsActive = @IsActive OR @IsActive is null)

";
        const string Query_Department_CandidateRecruitment = @"

select DepartmentID,DepartmentName from HRMS_PositionRequirement
inner join SET_Designation on SET_Designation.DesignationID = HRMS_PositionRequirement.FK_DesignationID
and HRMS_PositionRequirement.Status =1 
and SET_Designation.Status =1 
inner join SET_Department on SET_Department.DepartmentID =  HRMS_PositionRequirement.FK_DepartmentID
and SET_Department.Status =1 
 group by DepartmentID ,DepartmentName
";
        const string Query_Designation_CandidateRecruitment = @"

select DesignationID,DesignationName from HRMS_PositionRequirement
inner join SET_Designation on SET_Designation.DesignationID = HRMS_PositionRequirement.FK_DesignationID
and HRMS_PositionRequirement.Status =1 
and SET_Designation.Status =1 
inner join SET_Department on SET_Department.DepartmentID =  HRMS_PositionRequirement.FK_DepartmentID
and SET_Department.Status =1 
 group by DesignationID,DesignationName
";
        const string Query_Department_Designation_withkey = @"

select DepartmentID,DesignationID,DesignationName,DepartmentName from HRMS_PositionRequirement
inner join SET_Designation on SET_Designation.DesignationID = HRMS_PositionRequirement.FK_DesignationID
and HRMS_PositionRequirement.Status =1 
and SET_Designation.Status =1 
inner join SET_Department on SET_Department.DepartmentID =  HRMS_PositionRequirement.FK_DepartmentID
and SET_Department.Status =1 
{0}
group by DepartmentID , DesignationID,DesignationName,DepartmentName

";
        const string Query_NonShortListedCandidate = @" SELECT * FROM [HRMS_Candidate] WHERE [Status] = 1 AND ([IsActive] = @IsActive OR @IsActive is null) And IsShortListCandidate is null or IsShortListCandidate = 0 ";

        const string Query_ShortListedCandidate = @" SELECT * FROM [HRMS_Candidate] WHERE [Status] = 1 AND ([IsActive] = @IsActive OR @IsActive is null) And IsShortListCandidate = 1 ";

        const string Query_GetCandidateDetail = @" SELECT  DISTINCT      
HRMS_Candidate.CandidateID
,HRMS_Candidate.AddressLine1
,HRMS_Candidate.AddressLine2
,HRMS_Candidate.AddressLine3
,HRMS_Candidate.AttachmentName
,HRMS_Candidate.CityID
,HRMS_Candidate.CountryID
,HRMS_Candidate.ProvinceID
,case 
when  HRMS_Candidate.[ProvinceID] = 1 then 'Balochistan' 
when  HRMS_Candidate.[ProvinceID] = 2 then 'KPK'
when  HRMS_Candidate.[ProvinceID] = 3 then 'Punjab'
when  HRMS_Candidate.[ProvinceID] = 4 then 'Sindh'
end as Province
,HRMS_Candidate.InformedBy
,HRMS_Candidate.IsShortListCandidate
,SET_City.CityName
,SET_Country.CountryName
-----
,HRMS_Candidate.CandidateNo
,HRMS_Candidate.Name
, HRMS_Candidate.Email
,HRMS_Candidate.Phone
, HRMS_Candidate.Status
,HRMS_Candidate.IsActive
, HRMS_Candidate.Attachment
,Case When HRMS_Candidate.IsAppointed = 0 THEN 'No' Else 'Yes' End AS IsAppointed
, HRMS_Candidate.DateOfBirth
,HRMS_Candidate.Age
, HRMS_Candidate.FK_PositionRequirementID
, HRMS_Candidate.Position
, HRMS_Candidate.MobileNo2
,HRMS_Candidate.CNICNo
,SET_Designation.DesignationName
,SET_Department.DepartmentName

-----
,HRMS_Candidate.Education
,HRMS_Candidate.Specialization
,HRMS_Candidate.TotalExperience
,HRMS_Candidate.CompaniesServed
,HRMS_Candidate.LastDesignation
,HRMS_Candidate.ExpectedSalary
,HRMS_Candidate.LastSalary

FROM HRMS_Candidate 
INNER JOIN SET_City ON HRMS_Candidate.CityID = SET_City.CityID 
INNER JOIN SET_Country ON HRMS_Candidate.CountryID = SET_Country.CountryID
left join [HRMS_PositionRequirement] on  [HRMS_PositionRequirement].[PositionRequirementID] = [HRMS_Candidate].FK_PositionRequirementID
Left Join SET_Designation on SET_Designation.DesignationID = [HRMS_PositionRequirement].FK_DesignationID
And SET_Designation.IsActive = 1
Left Join SET_Department on SET_Department.DepartmentID = [HRMS_PositionRequirement].FK_DepartmentID
And SET_Department.IsActive = 1

Where HRMS_Candidate.Status = 1
{0}
";

        #endregion

        #region Class Methods

      //  Query_ShortlistCandidateReport

        public static DataTable ShortlistCandidateReportData(Nullable<int> companyid , Nullable<int> periodid,Nullable<int > candidate)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                 SqlParameter[] sqlparams = new SqlParameter[2];
                 sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                 sqlparams[1] = new SqlParameter("@PeriodID", periodid);

                 if (candidate != null)
                 {
                     sb.Append("  and HRMS_Candidate.CandidateID  =  "+ candidate+ " ");
                 
                 }
                //if (isActive.HasValue)
                //    sqlparams[0] = new SqlParameter("@IsActive", isActive.Value);
                //else
                //    sqlparams[0] = new SqlParameter("@IsActive", DBNull.Value);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format( Query_ShortlistCandidateReport,sb),sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        // Query_allShortListed

        public static DataTable All_shortListedCandidates()
        {
            DataTable dt = null;

            try
            {
                //  SqlParameter[] sqlparams = new SqlParameter[1];

                //if (isActive.HasValue)
                //    sqlparams[0] = new SqlParameter("@IsActive", isActive.Value);
                //else
                //    sqlparams[0] = new SqlParameter("@IsActive", DBNull.Value);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_allShortListed).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }



        public static DataTable Designation_CandidateRecruitment(Nullable<bool> isActive = true)
        {
            DataTable dt = null;

            try
            {
              //  SqlParameter[] sqlparams = new SqlParameter[1];

                //if (isActive.HasValue)
                //    sqlparams[0] = new SqlParameter("@IsActive", isActive.Value);
                //else
                //    sqlparams[0] = new SqlParameter("@IsActive", DBNull.Value);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Designation_CandidateRecruitment).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable Department_CandidateRecruitment(Nullable<bool> isActive = true)
        {
            DataTable dt = null;

            try
            {
                //  SqlParameter[] sqlparams = new SqlParameter[1];

                //if (isActive.HasValue)
                //    sqlparams[0] = new SqlParameter("@IsActive", isActive.Value);
                //else
                //    sqlparams[0] = new SqlParameter("@IsActive", DBNull.Value);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Department_CandidateRecruitment).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static bool UpdateCandidateToShortListed(int CompanyID, int CandidateID,SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[1];                
                sqlparams[0] = new SqlParameter("@CandidateID", CandidateID);               

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateCandidateToShortListed, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_UpdateCandidateToShortListed, sqlparams) > 0)
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

        public static DataTable GetAllActiveDataOfNonShortListedCandidate(Nullable<bool> isActive = true, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
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
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_NonShortListedCandidate, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_NonShortListedCandidate, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_NonShortListedCandidate, sqlparams);

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

        public static DataTable GetAllActiveDataOfShortListedCandidate(Nullable<bool> isActive = true, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
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
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_ShortListedCandidate, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_ShortListedCandidate, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_ShortListedCandidate, sqlparams);

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

        public static DataTable Department_Designation_withKey(Nullable <int> Department )
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                if (Department !=  null)
                {
                    sb.Append(" Where SET_Department.DepartmentID = "+Department+" ");
                }

                //if (  Department.HasValue)
                //    sqlparams[0] = new SqlParameter("@DepartmentID", Department.Value);
                //else
                //    sqlparams[0] = new SqlParameter("@DepartmentID", DBNull.Value);

                dt = SqlHelper.ExecuteDataset(CommandType.Text,string.Format(Query_Department_Designation_withkey,sb)).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable Get_CanidateData(Nullable<int> department,Nullable<int> designation,string refno,
            Nullable<int> companyid,Nullable<int> periodid,
            Nullable<bool> isActive = true, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                StringBuilder sb = new StringBuilder();
                //if (isActive == true)
                //{ isActive = bool.Parse( 1).to; }

                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID",companyid);
                sqlparams[1] = new SqlParameter("@PeriodID",periodid);
                //if (isActive.HasValue)
                //    sqlparams[2] = new SqlParameter("@IsActive", isActive.Value);
                //else
                //    sqlparams[2] = new SqlParameter("@IsActive", DBNull.Value);

                if (department != -1)
                {

                    sb.Append(" and SET_Department.DepartmentID =  "+ department +" ");
                }

                if (designation != -1)
                {
                    sb.Append(" and  SET_Designation.DesignationID =  " + designation + " ");
                }

                if (refno.ToString() != "-1")
                {
                    sb.Append(" and RefNo = '"+ refno +"' ");
                
                }

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_CandidateData, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_CandidateData, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text,string.Format( Query_CandidateData,sb), sqlparams);

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


        public static DataRow GetDataByCandidateNo(string candidateNo, Nullable<bool> isActive = true, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
            {
                DataRow dr = null;

                try
                {
                    SqlParameter[] sqlparams = new SqlParameter[2];
                    sqlparams[0] = new SqlParameter("@CandidateNo", candidateNo);
                    if (isActive.HasValue)
                        sqlparams[1] = new SqlParameter("@IsActive", isActive.Value);
                    else
                        sqlparams[1] = new SqlParameter("@IsActive", DBNull.Value);

                    DataSet ds = null;

                    if (objTransaction != null)
                        ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byCandidateNo, sqlparams);
                    else if (objConnection != null)
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byCandidateNo, sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byCandidateNo, sqlparams);

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

            public static bool UpdateIsAppointedByCandidateNo(string candidateNo,SqlTransaction objTransaction = null)
            {
                bool result = false;

                try
                {
                    SqlParameter[] sqlparams = new SqlParameter[1];
                    sqlparams[0] = new SqlParameter("@CandidateNo", candidateNo);
                    if (objTransaction == null)
                    {
                        if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateIsAppointed_byCandidateNo, sqlparams) > 0)
                            result = true;
                    }
                    else
                    {
                        if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_UpdateIsAppointed_byCandidateNo, sqlparams) > 0)
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

            public static DataTable GetCandidateInfo(int companyid, Nullable<int> PersonID)
            {
                DataTable dt = null;
                StringBuilder sb = new StringBuilder();
                try
                {
                    SqlParameter[] sqlparams = new SqlParameter[2];

                    sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                    if (PersonID != null)
                    {
                        sb.Append(" and HRMS_Candidate.CandidateID =  " + PersonID + "  ");
                    }

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetCandidateDetail, sb), sqlparams).Tables[0];


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
