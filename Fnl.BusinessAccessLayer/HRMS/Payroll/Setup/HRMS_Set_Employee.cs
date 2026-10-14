using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using System.Data;
using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Setup
{
    public partial class HRMS_Set_Employee
    {
        #region Constant Srings

        const string Query_InsertSETEmployeePF = @" INSERT INTO {0}.dbo.[HRMS_Set_Employee] ([CompanyID], [PersonID], [PersonTypeCode], [Suffix], [Title], [FirstName], [MiddleName], [LastName], [FullName], [KnownAs], [FatherName], [Gender], [DateOfBirth], [DateOfDeath], [FK_MaritalStatusID], [AddressLine1], [AddressLine2], [AddressLine3], [MobileNumber], [PhoneNumber1], [PhoneNumber2], [EmergencyContactNumber], [OfficeNumber], [Email], [Nationality], [FK_CountryID], [FK_CityID], [PassportNo], [BloodGroup], [NICNumber], [CNICNumber], [NTNNo], [SocialSecNo], [Domicile], [EOBINo], [MotherLanguage], [LastMedicalTestBy], [JobStatusCode], [EmployeeCode], [EmpAccountNo], [ApplicantNumber], [RecommendedBy1], [RecommendedBy2], [RecommendedBy3], [RehireReason], [RehireRecommendation], [ExtraComments], [JoiningDate], [AppointedOn], [BackgroudCheckDate], [DateEmployeeDataVerified], [EffectiveEndDate], [EffectiveStartDate], [EOBIJoinDate], [OrignalDateOfHire], [ReceiptOfDeathCertDate], [SocSecJoinDate], [HoldApplicantDateUntil], [JobEndingDate], [LastMedicalTestDate], [ResumeLastUpdate], [IsPersonDisabled], [IsBackgroundChecked], [IsCurrentApplicant], [IsCurrentEmployee], [IsOnJob], [IsOnMilitaryService], [IsPFMember], [IsEOBIMember], [IsResumeExist], [IsSecondPassportExist], [IsStudent], [UseTobacco], [BasicSalary], [LastJob], [FK_CommentID], [FK_BankBranchID], [FK_BankAccountID], [FK_CustomerID], [FK_DepartmentID], [FK_GradeID], [FK_JobCategoryID], [FK_JobDesignationID], [FK_SectionID], [Picture], [Status], [CreatedBy], [CreateDate], [CreatedByValue], [IsActive], [AppraisalFrequency], [AppraisalDate], [AppraisalDueDate], [FK_LocationID], [IsSSMember], [IsOvertime], [EmergencyContactPersonName], [MobileNumber2], [ExtNo], [Area], [PassportExpiryDate], [FamilyNo], [RecommendedByID1], [RecommendedByID2], [RecommendedByID3], [IsHealthInsurance], [HealthInsuranceNo], [IsEligiableForAppraisal], [FileNumber], [ReferencePersonName], [ReferencePersonFatherName], [ReferencePersonContactNo], [ReferencePersonEmail], [ReferencePersonOfficeName], [ReferencePersonjobDesignation], [Religion], [LanguageSpoken], [FrequencyType], [ReferencePersonName2], [ReferencePersonFatherName2], [ReferencePersonContactNo2], [ReferencePersonEmail2], [ReferencePersonOfficeName2], [ReferencePersonjobDesignation2], [StatusID], [ProvinceID], [OldEmployeeCode], [IsWillingTravelInPakistan], [IsWillingTravelAbroad], [AvalibilityinShift], [DrivingLicense], [DrivingLicenseExpiryDate], [EmployeeHeight], [EmployeeWeight], [FamilyDoctorName], [HospitalName], [HospitalContact], [HospitalAddress], [EmployeeReferenceDepartment1], [EmployeeReferenceDepartment2], [CoveredLifeInsurance], [LifePremium], [LifeCategory], [SumAssured], [HealthInsuranceCategory], [HealthInsurancePremium], [HealthCertID], [Fluency], [LanguageSpoken2], [LanguageSpoken3], [MedicalIllness], [PermanentDate], [DivisionID], [SubDivisionID], [LocationTypeID], [OfficeEmail], [KinName1], [KinRelationShipID1], [KinCNIC1], [KinAddress1], [KinContactNo1], [KinName2], [KinRelationShipID2], [KinCNIC2], [KinAddress2], [KinContactNo2], [IsUnionMember], [IsRehire], [ReplacementEmployeeID], [IsReferenceChecked], [CostCenterID], [PlaceOfBirth], [IsHajjPolice], [IsAttendanceAllowance], [IsLeaveEncash], [IsPEE], [IsOffDayGazt], [IsTimeAllownce], [IsWorkingDayFoodAllownce], [IsOffDayFoodAllownce], [IsAttendanceWaver], [IsProductionIncentive], [IsHolidayVisit], [IsExtraOverTime], [IsLateFine], [IsStopPayment], [RedFlag], [JobEndingStatus], [MotherName], [MotherCNIC], [FatherCNIC], [IsEOBIRegister], [IsSSRegister], [ExpenseTypeID], [IsBonusAllow], [PreviousGrossSalary], [IsIncrementAllow], [IBanNo], [IsEligibleForBonus], [IsBonus1Assigned], [IsBonus2Assigned], [IsBonus3Assigned], [Formula1], [Formula2], [Formula3], [IsExemptedFromGH], [JoiningSalary])
		VALUES(@CompanyID, @PersonID, @PersonTypeCode, @Suffix, @Title, @FirstName, @MiddleName, @LastName, @FullName, @KnownAs, @FatherName, @Gender, @DateOfBirth, @DateOfDeath, @FK_MaritalStatusID, @AddressLine1, @AddressLine2, @AddressLine3, @MobileNumber, @PhoneNumber1, @PhoneNumber2, @EmergencyContactNumber, @OfficeNumber, @Email, @Nationality, @FK_CountryID, @FK_CityID, @PassportNo, @BloodGroup, @NICNumber, @CNICNumber, @NTNNo, @SocialSecNo, @Domicile, @EOBINo, @MotherLanguage, @LastMedicalTestBy, @JobStatusCode, @EmployeeCode, @EmpAccountNo, @ApplicantNumber, @RecommendedBy1, @RecommendedBy2, @RecommendedBy3, @RehireReason, @RehireRecommendation, @ExtraComments, @JoiningDate, @AppointedOn, @BackgroudCheckDate, @DateEmployeeDataVerified, @EffectiveEndDate, @EffectiveStartDate, @EOBIJoinDate, @OrignalDateOfHire, @ReceiptOfDeathCertDate, @SocSecJoinDate, @HoldApplicantDateUntil, @JobEndingDate, @LastMedicalTestDate, @ResumeLastUpdate, @IsPersonDisabled, @IsBackgroundChecked, @IsCurrentApplicant, @IsCurrentEmployee, @IsOnJob, @IsOnMilitaryService, @IsPFMember, @IsEOBIMember, @IsResumeExist, @IsSecondPassportExist, @IsStudent, @UseTobacco, @BasicSalary, @LastJob, @FK_CommentID, @FK_BankBranchID, @FK_BankAccountID, @FK_CustomerID, @FK_DepartmentID, @FK_GradeID, @FK_JobCategoryID, @FK_JobDesignationID, @FK_SectionID, @Picture, @Status, @CreatedBy, @CreateDate, @CreatedByValue, @IsActive, @AppraisalFrequency, @AppraisalDate, @AppraisalDueDate, @FK_LocationID, @IsSSMember, @IsOvertime, @EmergencyContactPersonName, @MobileNumber2, @ExtNo, @Area, @PassportExpiryDate, @FamilyNo, @RecommendedByID1, @RecommendedByID2, @RecommendedByID3, @IsHealthInsurance, @HealthInsuranceNo, @IsEligiableForAppraisal, @FileNumber, @ReferencePersonName, @ReferencePersonFatherName, @ReferencePersonContactNo, @ReferencePersonEmail, @ReferencePersonOfficeName, @ReferencePersonjobDesignation, @Religion, @LanguageSpoken, @FrequencyType, @ReferencePersonName2, @ReferencePersonFatherName2, @ReferencePersonContactNo2, @ReferencePersonEmail2, @ReferencePersonOfficeName2, @ReferencePersonjobDesignation2, @StatusID, @ProvinceID, @OldEmployeeCode, @IsWillingTravelInPakistan, @IsWillingTravelAbroad, @AvalibilityinShift, @DrivingLicense, @DrivingLicenseExpiryDate, @EmployeeHeight, @EmployeeWeight, @FamilyDoctorName, @HospitalName, @HospitalContact, @HospitalAddress, @EmployeeReferenceDepartment1, @EmployeeReferenceDepartment2, @CoveredLifeInsurance, @LifePremium, @LifeCategory, @SumAssured, @HealthInsuranceCategory, @HealthInsurancePremium, @HealthCertID, @Fluency, @LanguageSpoken2, @LanguageSpoken3, @MedicalIllness, @PermanentDate, @DivisionID, @SubDivisionID, @LocationTypeID, @OfficeEmail, @KinName1, @KinRelationShipID1, @KinCNIC1, @KinAddress1, @KinContactNo1, @KinName2, @KinRelationShipID2, @KinCNIC2, @KinAddress2, @KinContactNo2, @IsUnionMember, @IsRehire, @ReplacementEmployeeID, @IsReferenceChecked, @CostCenterID, @PlaceOfBirth, @IsHajjPolice, @IsAttendanceAllowance, @IsLeaveEncash, @IsPEE, @IsOffDayGazt, @IsTimeAllownce, @IsWorkingDayFoodAllownce, @IsOffDayFoodAllownce, @IsAttendanceWaver, @IsProductionIncentive, @IsHolidayVisit, @IsExtraOverTime, @IsLateFine, @IsStopPayment, @RedFlag, @JobEndingStatus, @MotherName, @MotherCNIC, @FatherCNIC, @IsEOBIRegister, @IsSSRegister, @ExpenseTypeID, @IsBonusAllow, @PreviousGrossSalary, @IsIncrementAllow, @IBanNo, @IsEligibleForBonus, @IsBonus1Assigned, @IsBonus2Assigned, @IsBonus3Assigned, @Formula1, @Formula2, @Formula3, @IsExemptedFromGH, @JoiningSalary) ";

        const string Query_UpdateGSalary_By_PersonID = @"Update HRMS_Set_Employee Set BasicSalary = @BasicSalary , PreviousGrossSalary = @PreviousGrossSalary Where PersonID = @PersonID and CompanyID = @CompanyID";

        const string Query_SelectAllForFinsih = @" SELECT PersonID,FullName FROM [HRMS_Set_Employee] WHERE [Status] = 1 AND ([IsActive] = @IsActive OR @IsActive is null) AND ([CompanyID] = @CompanyID OR @CompanyID is null) ";
        const string Query_GetDesignation = @" SELECT *,SET_JobDesignation.JobDesignationName [DesignationName]  FROM [HRMS_Set_Employee] 
left join SET_JobDesignation on SET_JobDesignation.JobDesignationID = HRMS_Set_Employee.FK_JobDesignationID
WHERE HRMS_Set_Employee.[Status] = 1 AND (HRMS_Set_Employee.[IsActive] = @IsActive OR @IsActive is null) AND (HRMS_Set_Employee.[CompanyID] = @CompanyID OR @CompanyID is null) and PersonID = @PersonID";

        const string Query_GetGrossAmt = @" SELECT * FROM [HRMS_Set_Employee] 

WHERE HRMS_Set_Employee.[Status] = 1 
AND (HRMS_Set_Employee.[IsActive] = @IsActive OR @IsActive is null) 
AND (HRMS_Set_Employee.[CompanyID] = @CompanyID OR @CompanyID is null) 
and PersonID = @PersonID";

        const string Query_With_Filter = @" SELECT *,DepartmentID,DepartmentName,JobDesignationID as DesignationID,JobDesignationName as DesignationName FROM [HRMS_Set_Employee]  
inner join HRMS_EmployeeInEmployement_Status on HRMS_EmployeeInEmployement_Status.StatusID = HRMS_Set_Employee.StatusID
and HRMS_EmployeeInEmployement_Status.CompanyID = HRMS_Set_Employee.CompanyID
	 and HRMS_EmployeeInEmployement_Status.UserID = @UserID
inner join SET_Department on 
	 SET_Department.DepartmentID=HRMS_Set_Employee.FK_DepartmentID
	 left join SET_JobDesignation on SET_JobDesignation.JobDesignationID  =  HRMS_Set_Employee.FK_JobDesignationID
	 
 
 
 WHERE HRMS_Set_Employee.Status = 1  and HRMS_Set_Employee.IsActive = @IsActive

AND ([HRMS_Set_Employee].[CompanyID]=@CompanyID OR [HRMS_Set_Employee].[CompanyID] is null)
";

        const string Query_With_Filtershift = @" SELECT Top 1 *,DepartmentID,DepartmentName,JobDesignationID as DesignationID,JobDesignationName as DesignationName FROM [HRMS_Set_Employee]  
inner join HRMS_EmployeeInEmployement_Status on HRMS_EmployeeInEmployement_Status.StatusID = HRMS_Set_Employee.StatusID
and HRMS_EmployeeInEmployement_Status.CompanyID = HRMS_Set_Employee.CompanyID
	 and HRMS_EmployeeInEmployement_Status.UserID = @UserID
inner join SET_Department on 
	 SET_Department.DepartmentID=HRMS_Set_Employee.FK_DepartmentID
	 left join SET_JobDesignation on SET_JobDesignation.JobDesignationID  =  HRMS_Set_Employee.FK_JobDesignationID
	 
 
 
 WHERE HRMS_Set_Employee.Status = 1

AND ([HRMS_Set_Employee].[CompanyID]=@CompanyID OR [HRMS_Set_Employee].[CompanyID] is null)
";

  
        const string Query_SelectAllEmployeeinPayRoll = @"
select * from
(
SELECT distinct ISNULL(MONTH(JobEndingDate),0) as MnthDigit,ISNULL(YEAR(JobEndingDate),0) as YearDigit,
HRMS_Set_Employee.*,convert(bit,0) as IsChecked FROM [HRMS_Set_Employee] 
inner JOIN HRMS_EmployeePayRoll ON HRMS_EmployeePayRoll.PersonID = HRMS_Set_Employee.PersonID
		AND HRMS_EmployeePayRoll.CompanyID = HRMS_Set_Employee.CompanyID 
		and HRMS_EmployeePayRoll.Status = 1
)tab		
WHERE tab.Status = 1 AND (tab.IsActive = 1) 
AND (tab.CompanyID = @CompanyID OR @CompanyID is null)
{0}

Order By CAST(isnull(tab.EmployeeCode,0) as Int)  asc

";
        
        const string Query_SelectAllEmployee_TMS = @"
select * from
(
SELECT distinct ISNULL(MONTH(JobEndingDate),0) as MnthDigit,ISNULL(YEAR(JobEndingDate),0) as YearDigit,
HRMS_Set_Employee.*,convert(bit,0) as IsChecked FROM HRMS_TMSDATA
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_TMSDATA.PersonID
and HRMS_Set_Employee.CompanyID = HRMS_TMSDATA.CompanyID
and HRMS_Set_Employee.IsActive = 1 and HRMS_Set_Employee.Status = 1 and isnull(HRMS_Set_Employee.IsOvertime,0) = 1
and Datepart(Month,TMSDATETIME) = @Month
and Datepart(YEAR,TMSDATETIME) = @Year
)tab		
WHERE tab.Status = 1 AND (tab.IsActive = 1) 
AND (tab.CompanyID = @CompanyID OR @CompanyID is null)
{0}
";

        const string Query_SelectAllEmployee_TMS_WithOT = @"
select * from
(
SELECT distinct ISNULL(MONTH(JobEndingDate),0) as MnthDigit,ISNULL(YEAR(JobEndingDate),0) as YearDigit,
HRMS_Set_Employee.*,convert(bit,0) as IsChecked,convert(bit,0) as IsCheckedRej, HRMS_Emp_OT.TMSDate,HRMS_Emp_OT.TimeDifferenceInMin,HRMS_Emp_OT.OTInHrs,HRMS_Emp_OT.OTID FROM HRMS_Emp_OT
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_Emp_OT.PersonID
and HRMS_Set_Employee.CompanyID = HRMS_Emp_OT.CompanyID
and HRMS_Set_Employee.IsActive = 1 and HRMS_Set_Employee.Status = 1
and isnull(HRMS_Emp_OT.IsApproved,0) = 0 and  isnull(HRMS_Emp_OT.IsRejected,0) = 0
and Datepart(Month,TMSDATETIME) = @Month
and Datepart(YEAR,TMSDATETIME) = @Year
)tab		
WHERE tab.Status = 1 AND (tab.IsActive = 1) 
AND (tab.CompanyID = @CompanyID OR @CompanyID is null)
{0}

";


        const string Query_GetNewEmployeeCodeForConnect = @"
SELECT (isnull(count(isnull(EmployeeCode,0)),0) + 1)  as PersonID
 FROM HRMS_Set_Employee WHERE HRMS_Set_Employee.CompanyID=@CompanyID and HRMS_Set_Employee.Status = 1
 and  month(AppointedOn) = (@Month) and year(AppointedOn) = (@Year)



";

        const string Query_SelectAllEmployeeinPayRollForArears = @"
select * from
(
SELECT distinct -1 as AreaID,-1 as AreaDetailID,convert(date,null) as  StartingMonth,convert(date,null) as EndMonth,convert(date,null) as paymentMonth,null as IncrementPercentage,null as TotalAmount, ISNULL(MONTH(JobEndingDate),0) as MnthDigit,ISNULL(YEAR(JobEndingDate),0) as YearDigit,
HRMS_Set_Employee.*,convert(bit,0) as IsChecked FROM [HRMS_Set_Employee] 
inner JOIN HRMS_EmployeePayRoll ON HRMS_EmployeePayRoll.PersonID = HRMS_Set_Employee.PersonID
		AND HRMS_EmployeePayRoll.CompanyID = HRMS_Set_Employee.CompanyID 
		and HRMS_EmployeePayRoll.Status = 1
)tab		
WHERE tab.Status = 1 AND (tab.IsActive = 1) 
AND (tab.CompanyID = @CompanyID OR @CompanyID is null)
{0}
";

        const string Query_MaritalStatus = @"
select * from LookUpDetail
inner join LookupHeader on  LookupHeader.LookupID = LookupDetail.LookupHeaderID
and LookupDetail.IsActive = 1
and LookupHeader.LookUpType like 'MaritalStatus'
";

        const string Query_EmployeeDLL_OT = @"SELECT SET_JobDesignation.JobDesignationName as DesignationName,SET_Department.DepartmentName,[HRMS_Set_Employee].* FROM [HRMS_Set_Employee]
inner join HRMS_EmployeeInEmployement_Status on HRMS_EmployeeInEmployement_Status.StatusID = HRMS_Set_Employee.StatusID
     and HRMS_EmployeeInEmployement_Status.CompanyID = HRMS_Set_Employee.CompanyID
	 and HRMS_EmployeeInEmployement_Status.UserID = @UserID
left JOIN SET_JobDesignation on SET_JobDesignation.JobDesignationID=HRMS_Set_Employee.Fk_JobDesignationID
  --AND SET_JobDesignation.CompanyID = HRMS_Set_Employee.CompanyID
left JOIN SET_Department ON SET_Department.DepartmentID=HRMS_Set_Employee.FK_DepartmentID
 AND SET_Department.Status=1
 WHERE [HRMS_Set_Employee].[Status] = 1 AND ([HRMS_Set_Employee].[IsActive] = @IsActive OR @IsActive is null) 
AND (HRMS_Set_Employee.CompanyID = @CompanyID OR @CompanyID is null)
and [HRMS_Set_Employee].IsOvertime = 1 ";

        const string Query_EmployeeDLL_OTAlnoor = @"SELECT top 1 SET_JobDesignation.JobDesignationName as DesignationName,SET_Department.DepartmentName,[HRMS_Set_Employee].* FROM [HRMS_Set_Employee]
inner join HRMS_EmployeeInEmployement_Status on HRMS_EmployeeInEmployement_Status.StatusID = HRMS_Set_Employee.StatusID
	 and HRMS_EmployeeInEmployement_Status.UserID = @UserID
left JOIN SET_JobDesignation on SET_JobDesignation.JobDesignationID=HRMS_Set_Employee.Fk_JobDesignationID
  --AND SET_JobDesignation.CompanyID = HRMS_Set_Employee.CompanyID
left JOIN SET_Department ON SET_Department.DepartmentID=HRMS_Set_Employee.FK_DepartmentID
 AND SET_Department.Status=1
 WHERE [HRMS_Set_Employee].[Status] = 1 AND ([HRMS_Set_Employee].[IsActive] = @IsActive OR @IsActive is null) 
AND (HRMS_Set_Employee.CompanyID = @CompanyID OR @CompanyID is null)
and [HRMS_Set_Employee].IsOvertime = 1 ";

        const string query_GetEmployeesAllCasualLeaves = @"

select HRMS_PayrollType.Description,HRMS_EmployeePayRoll.PersonID,
		  HRMS_EmployeePayRoll.LeaveDays,HRMS_EmployeePayRoll.PayrollTypeID
		  from HRMS_EmployeePayRoll
		  
		  inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_EmployeePayRoll.PayrollTypeID
		  and HRMS_PayrollType.PayrollTypeCode = HRMS_EmployeePayRoll.PayrollTypeCode
		  and HRMS_PayrollType.Status = 1
		  
		  where HRMS_PayrollType.Payrolltypecode = @PayrollTypeCode
		  and HRMS_PayrollType.Description like '%Casual%'
		  and HRMS_EmployeePayRoll.PersonID = @PersonID
		  and HRMS_EmployeePayRoll.Status = 1
          and HRMS_EmployeePayRoll.CompanyID = @CompanyID
";

        const string query_GetEmployeesAllSickLeaves = @"
select HRMS_PayrollType.Description,HRMS_EmployeePayRoll.PersonID,
		  HRMS_EmployeePayRoll.LeaveDays,HRMS_EmployeePayRoll.PayrollTypeID
		  from HRMS_EmployeePayRoll
		  
		  inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_EmployeePayRoll.PayrollTypeID
		  and HRMS_PayrollType.PayrollTypeCode = HRMS_EmployeePayRoll.PayrollTypeCode
		  and HRMS_PayrollType.Status = 1
		  
		  where HRMS_PayrollType.Payrolltypecode = @PayrollTypeCode
		  and HRMS_PayrollType.Description like '%Sick%'
		  and HRMS_EmployeePayRoll.PersonID = @PersonID
		  and HRMS_EmployeePayRoll.Status = 1
          and HRMS_EmployeePayRoll.CompanyID = @CompanyID
";

        const string query_GetEmployeesAllAnnualLeaves = @"
select HRMS_PayrollType.Description,HRMS_EmployeePayRoll.PersonID,
		  HRMS_EmployeePayRoll.LeaveDays,HRMS_EmployeePayRoll.PayrollTypeID
		  from HRMS_EmployeePayRoll
		  
		  inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_EmployeePayRoll.PayrollTypeID
		  and HRMS_PayrollType.PayrollTypeCode = HRMS_EmployeePayRoll.PayrollTypeCode
		  and HRMS_PayrollType.Status = 1
		  
		  where HRMS_PayrollType.Payrolltypecode = @PayrollTypeCode
		  and HRMS_PayrollType.Description like '%Sick%'
		  and HRMS_EmployeePayRoll.PersonID = @PersonID
		  and HRMS_EmployeePayRoll.Status = 1
          and HRMS_EmployeePayRoll.CompanyID = @CompanyID
";

        const string query_GetAbsentLeavePayRollID = @"
 select isnull(PayrollTypeID,0) as PayrollTypeID from HRMS_PayRollType
	      where [Description] like '%Absent%'
	      and Status = 1
	      --and CompanyID = @CompanyID
	      and PayrollTypeCode = @PayrollTypeCode
";

        const string query_GetAbsentEmployeeLeavePayRollID = @"

 select HRMS_PayrollType.Description,HRMS_EmployeePayRoll.PersonID,
		  HRMS_EmployeePayRoll.LeaveDays,HRMS_EmployeePayRoll.PayrollTypeID
		  from HRMS_EmployeePayRoll
		  
		  inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_EmployeePayRoll.PayrollTypeID
		  and HRMS_PayrollType.PayrollTypeCode = HRMS_EmployeePayRoll.PayrollTypeCode
		  and HRMS_PayrollType.Status = 1
		  
		  where HRMS_PayrollType.Payrolltypecode = @PayrollTypeCode
		  and HRMS_PayrollType.Description like '%Absent%'
		  and HRMS_EmployeePayRoll.PersonID = @PersonID
		  and HRMS_EmployeePayRoll.Status = 1
          and HRMS_EmployeePayRoll.CompanyID = @CompanyID
";

        const string query_GetEmployeesEntryForTMS = @"
select * from (
 Select CompanyID,PersonID,EmployeeCode,FullName,DesignationName,DepartmentName,ShiftName,
        sum(CountLeave) as Days_Leave,sum(CountPresent) as Days_Present,sum(CountAbsent) as Days_Absent,
        sum(CountLeave) + sum(CountPresent) + sum(CountAbsent) as Days_Total,TheDate,Day_Name,((day(TheDate)-1) / 7) + 1 as Day_Week,
        sum(CountInLate) as Days_Late,sum(CountOutEarly) as Days_Early,datename(day,JoiningDate) as dayOfJoining,datepart(MONTH,JoiningDate) as MonthOfJoining,JoiningDate
        from (
	        SELECT distinct fintab.TheDate,convert(varchar(2),TheDate,106)as NewDate,JoiningDate,
                case when fintab.IsHoliday = 1 then fintab.DayDescription else datename(dw,fintab.TheDate) end as Day_Name,
                fintab.CompanyID,fintab.PersonID,EmployeeCode,FullName,DesignationName,DepartmentName,ShiftName,
                max(LeavesCount) as CountLeave,
                case
			        when fintab.IsHoliday = 1 and fintab.ShiftName = 'OD' then 1 else
			        case 
                        --when MIN(TMSTimeIN) is not null and  max(LeavesCount) = 0 then 1 
                        --when MAX(TMSTimeOUT) is not null and  max(LeavesCount) = 0 then 1 
                        when MIN(TMSTimeIN) is not null and MAX(TMSTimeOUT) is not null  and  max(LeavesCount) = 0 then 1 -- TIme in and out  is must
                        when max(LeavesCount) > 0 then 1 
                        else 0 end 
		        end as CountPresent,
                case 
			        when fintab.IsHoliday = 1 then 0 else
			        Case when (MIN(TMSTimeIN) is null AND MAX(TMSTimeOUT) is null) AND max(fintab.LeavesCount)=0 
			        then 1 else 0 end
		        end as CountAbsent,
                case when MIN(TMSTimeIN) > STimeIN then 1 else 0 end CountInLate,
                case when MAX(TMSTimeOUT) < STimeOUT then 1 else 0 end CountOutEarly

                from (
                SELECT EP.FK_SectionID,EP.FK_LocationID,SET_DateTime.TheDate,EP.JoiningDate,
                EP.CompanyID,EP.PersonID,EP.EmployeeCode,EP.FullName,ED.DesignationName AS DesignationName,
                DP.DepartmentName AS DepartmentName,

                SET_DateTime.IsHoliday,SET_DateTime.[Description] as DayDescription,
                
				(Select case when count(LR.LeaveRequestID) > 0 then 1 else 0 end	        
				 from HRMS_LeaveRequest LR where 
						LR.CompanyID = EP.CompanyID and LR.FK_PersonID = EP.PersonID
						and LR.FromDate <= SET_DateTime.TheDate and LR.ToDate >= SET_DateTime.TheDate
						and LR.[Status]=1 and LR.[IsApproved]=1) as LeavesCount,                
                ETMS1.TMSDateTime as TMSTimeIN,ETMS2.TMSDateTime as TMSTimeOUT,SS.[Description] as ShiftName,  
              
                CONVERT(datetime,
		                CONVERT(varchar(2), DATEPART(mm, SET_DateTime.TheDate)) + '-' + 
		                CONVERT(varchar(2), DATEPART(dd, SET_DateTime.TheDate)) + '-' + 
		                CONVERT(varchar(4), DATEPART(year, SET_DateTime.TheDate)) + ' ' + CONVERT(varchar(100),SS.[TimeINGrace],114) 
	                ,101) as STimeIN,
                CONVERT(datetime,
	                CONVERT(varchar(2), DATEPART(mm, SET_DateTime.TheDate)) + '-' + 
	                CONVERT(varchar(2), DATEPART(dd, SET_DateTime.TheDate)) + '-' + 
	                CONVERT(varchar(4), DATEPART(year, SET_DateTime.TheDate)) + ' ' + CONVERT(varchar(100),SS.[TimeOUT],114) 
                ,101) as STimeOUT,                
                ETMS1.TerminalNo
                
                from SET_DateTime 
                
                INNER JOIN HRMS_Set_Employee EP on (EP.CompanyID = @CompanyID)and EP.Status=1
                left JOIN SET_Designation ED ON ED.DesignationID = EP.FK_JobDesignationID AND ED.IsActive = 1 AND ED.Status = 1   
                left JOIN SET_Department DP ON DP.DepartmentID = EP.FK_DepartmentID AND DP.IsActive = 1 AND DP.Status = 1 

                left join HRMS_AttendanceRoaster ES on ES.PersonID =  EP.PersonID and ES.Status = 1
				and ES.ShiftFrom  =  SET_DateTime.TheDate
				left JOIN HRMS_Set_Shift SS on SS.ShiftID = ES.ShiftID and SS.Status=1

                --left JOIN HRMS_EmployeeShift ES on ( ES.FromDate <= SET_DateTime.TheDate
                --AND ES.ToDate >= SET_DateTime.TheDate) and ES.PersonID = EP.PersonID and ES.Status=1
                --left JOIN HRMS_Set_Shift SS on SS.ShiftID = ES.ShiftID and SS.Status=1

                left JOIN HRMS_TMS_Status TMS1 on TMS1.StatusID=1 and TMS1.Status=1 -- Min
                left JOIN HRMS_TMS_Status TMS2 on TMS2.StatusID=2 and TMS2.Status=1 -- Max

                left join HRMS_TMSData ETMS1 on ETMS1.CompanyID = EP.CompanyID and ETMS1.PersonID = EP.PersonID
                and ETMS1.StatusCode=TMS1.StatusCode and
                CONVERT(datetime,
	                CONVERT(varchar(2), DATEPART(mm, ETMS1.TMSDateTime)) + '-' + 
	                CONVERT(varchar(2), DATEPART(dd, ETMS1.TMSDateTime)) + '-' + 
	                CONVERT(varchar(4), DATEPART(year, ETMS1.TMSDateTime)) 
                ,101)
                 = SET_DateTime.TheDate

                left join HRMS_TMSData ETMS2 on ETMS2.CompanyID = EP.CompanyID and ETMS2.PersonID = EP.PersonID
                and ETMS2.StatusCode=TMS2.StatusCode and
                CONVERT(datetime,
	                CONVERT(varchar(2), DATEPART(mm, ETMS2.TMSDateTime)) + '-' + 
	                CONVERT(varchar(2), DATEPART(dd, ETMS2.TMSDateTime)) + '-' + 
	                CONVERT(varchar(4), DATEPART(year, ETMS2.TMSDateTime))
                ,101) 
                = SET_DateTime.TheDate

                 where dateName(Month, SET_DateTime.TheDate) = @Month
                 and dateName(Year, SET_DateTime.TheDate) = @Year
                 and EP.personID = @PersonID
                 
                ) as fintab

                group by TheDate,
                fintab.CompanyID,fintab.PersonID,EmployeeCode,FullName,DesignationName,DepartmentName,
                IsHoliday,fintab.DayDescription,ShiftName,
                STimeIN,STimeOUT,fintab.TerminalNo,fintab.JoiningDate
        
	        ) TAB
	
	        Group by CompanyID,PersonID,EmployeeCode,FullName,DesignationName,
	        DepartmentName,ShiftName,TheDate,Day_Name,JoiningDate
	        
	        )tab1
	        where tab1.TheDate >= joiningDate
	        and
	        tab1.Days_Present = 1

";
        const string Query_GetEmployeeData = @"
Select 
DISTINCT
HRMS_Set_Employee.PersonID,
HRMS_Set_Employee.EmployeeCode,
HRMS_Set_Employee.FullName as EmployeeName,
Case when HRMS_Set_Employee.Gender=1 then 'Female'
else  'Male'
end as Gender,
HRMS_Set_Employee.FatherName,
SET_JobDesignation.JobDesignationName as FunctionalTitle,
[SET_Grade].GradeName  as Grade,
[SET_JobCategory].JobCategoryName,
SET_Department.DepartmentName,
SET_Section.SectionName,
HRMS_Set_Employee.JoiningDate,
HRMS_Set_Employee.BasicSalary as GrossSalary,
HRMS_Set_Employee.DateOfBirth,
HRMS_Set_Employee.CNICNumber,
HRMS_Set_Employee.AddressLine1 as PresentAddress,
HRMS_Set_Employee.MobileNumber as ContactNo,
[HRMS_PayrollType].Description,
HRMS_EmployeePayroll.CalculatedAmount as Amount
from 
HRMS_Set_Employee

INNER JOIN HRMS_EmployeePayroll ON HRMS_EmployeePayroll.PersonID= HRMS_Set_Employee.PersonID
AND HRMS_EmployeePayroll.CompanyID=HRMS_Set_Employee.CompanyID
--AND HRMS_EmployeePayroll.Status=1
INNER JOIN [HRMS_PayrollType] on [HRMS_PayrollType].PayrollTypeID = [HRMS_EmployeePayroll].[PayrollTypeID]
AND [HRMS_PayrollType].IsPayable=1
left JOIN SET_JobDesignation on SET_JobDesignation.JobDesignationID=HRMS_Set_Employee.Fk_JobDesignationID
  AND SET_JobDesignation.CompanyID = HRMS_Set_Employee.CompanyID
left JOIN [SET_Grade] ON [SET_Grade].GradeID= HRMS_Set_Employee.FK_GradeID
AND  [SET_Grade].CompanyID=HRMS_Set_Employee.CompanyID AND [SET_Grade].IsActive=1
Left JOIN [SET_JobCategory] ON [SET_JobCategory].JobCategoryID=HRMS_Set_Employee.FK_JobCategoryID
AND [SET_JobCategory].CompanyID=HRMS_Set_Employee.CompanyID AND [SET_JobCategory].Status=1
left JOIN SET_Department ON SET_Department.DepartmentID=HRMS_Set_Employee.FK_DepartmentID
 AND SET_Department.Status=1
 left JOIN SET_Section ON SET_Section.SectionID=HRMS_Set_Employee.FK_SectionID
 AND SET_Section.Status=1
 Where HRMS_Set_Employee.CompanyID=@CompanyID ";

        const string query_GetEmployeesLateAndSandwichLeaves = @"

  Select CompanyID,PersonID,EmployeeCode,FullName,DesignationName,DepartmentName,ShiftName,
        sum(CountLeave) as Days_Leave,sum(CountPresent) as Days_Present,sum(CountAbsent) as Days_Absent,
        sum(CountLeave) + sum(CountPresent) + sum(CountAbsent) as Days_Total,TheDate,Day_Name,((day(TheDate)-1) / 7) + 1 as Day_Week,
        sum(CountInLate) as Days_Late,sum(CountOutEarly) as Days_Early,datename(day,JoiningDate) as dayOfJoining,datepart(MONTH,JoiningDate) as MonthOfJoining
        from (
	        SELECT distinct fintab.TheDate,convert(varchar(2),TheDate,106)as NewDate,JoiningDate,
                case when fintab.IsHoliday = 1 then fintab.DayDescription else datename(dw,fintab.TheDate) end as Day_Name,
                fintab.CompanyID,fintab.PersonID,EmployeeCode,FullName,DesignationName,DepartmentName,ShiftName,
                max(LeavesCount) as CountLeave,
                case
			        when fintab.IsHoliday = 1 then 1 else
			        case when MIN(TMSTimeIN) is not null then 1 when MAX(TMSTimeOUT) is not null then 1 else 0 end 
		        end as CountPresent,
                case 
			        when fintab.IsHoliday = 1 then 0 else
			        Case when (MIN(TMSTimeIN) is null AND MAX(TMSTimeOUT) is null) AND max(fintab.LeavesCount)=0 
			        then 1 else 0 end
		        end as CountAbsent,
                case when MIN(TMSTimeIN) > STimeIN then 1 else 0 end CountInLate,
                case when MAX(TMSTimeOUT) < STimeOUT then 1 else 0 end CountOutEarly

                from (
                SELECT EP.FK_SectionID,EP.FK_LocationID,SET_DateTime.TheDate,EP.JoiningDate,
                EP.CompanyID,EP.PersonID,EP.EmployeeCode,EP.FullName,ED.DesignationName AS DesignationName,
                DP.DepartmentName AS DepartmentName,
                SET_DateTime.IsHoliday,SET_DateTime.[Description] as DayDescription,
                
				(Select case when count(LR.LeaveRequestID) > 0 then 1 else 0 end	        
				 from HRMS_LeaveRequest LR where 
						LR.CompanyID = EP.CompanyID and LR.FK_PersonID = EP.PersonID
						and LR.FromDate <= SET_DateTime.TheDate and LR.ToDate >= SET_DateTime.TheDate
						and LR.[Status]=1 and LR.[IsApproved]=1) as LeavesCount,                
                ETMS1.TMSDateTime as TMSTimeIN,ETMS2.TMSDateTime as TMSTimeOUT,SS.[Description] as ShiftName,                
                CONVERT(datetime,
		                CONVERT(varchar(2), DATEPART(mm, SET_DateTime.TheDate)) + '-' + 
		                CONVERT(varchar(2), DATEPART(dd, SET_DateTime.TheDate)) + '-' + 
		                CONVERT(varchar(4), DATEPART(year, SET_DateTime.TheDate)) + ' ' + CONVERT(varchar(100),SS.[TimeINGrace],114) 
	                ,101) as STimeIN,
                CONVERT(datetime,
	                CONVERT(varchar(2), DATEPART(mm, SET_DateTime.TheDate)) + '-' + 
	                CONVERT(varchar(2), DATEPART(dd, SET_DateTime.TheDate)) + '-' + 
	                CONVERT(varchar(4), DATEPART(year, SET_DateTime.TheDate)) + ' ' + CONVERT(varchar(100),SS.[TimeOUT],114) 
                ,101) as STimeOUT,                
                ETMS1.TerminalNo
                
                from SET_DateTime 
                
                INNER JOIN HRMS_Set_Employee EP on (EP.CompanyID = @CompanyID)and EP.Status=1
                left JOIN SET_Designation ED ON ED.DesignationID = EP.FK_JobDesignationID AND ED.IsActive = 1 AND ED.Status = 1   
                left JOIN SET_Department DP ON DP.DepartmentID = EP.FK_DepartmentID AND DP.IsActive = 1 AND DP.Status = 1 

                left JOIN HRMS_EmployeeShift ES on ( ES.FromDate <= SET_DateTime.TheDate
                AND ES.ToDate >= SET_DateTime.TheDate) and ES.PersonID = EP.PersonID and ES.Status=1
                left JOIN HRMS_Set_Shift SS on SS.ShiftID = ES.ShiftID and SS.Status=1

                left JOIN HRMS_TMS_Status TMS1 on TMS1.StatusID=1 and TMS1.Status=1 -- Min
                left JOIN HRMS_TMS_Status TMS2 on TMS2.StatusID=2 and TMS2.Status=1 -- Max

                left join HRMS_TMSData ETMS1 on ETMS1.CompanyID = EP.CompanyID and ETMS1.PersonID = EP.PersonID
                and ETMS1.StatusCode=TMS1.StatusCode and
                CONVERT(datetime,
	                CONVERT(varchar(2), DATEPART(mm, ETMS1.TMSDateTime)) + '-' + 
	                CONVERT(varchar(2), DATEPART(dd, ETMS1.TMSDateTime)) + '-' + 
	                CONVERT(varchar(4), DATEPART(year, ETMS1.TMSDateTime)) 
                ,101)
                 = SET_DateTime.TheDate

                left join HRMS_TMSData ETMS2 on ETMS2.CompanyID = EP.CompanyID and ETMS2.PersonID = EP.PersonID
                and ETMS2.StatusCode=TMS2.StatusCode and
                CONVERT(datetime,
	                CONVERT(varchar(2), DATEPART(mm, ETMS2.TMSDateTime)) + '-' + 
	                CONVERT(varchar(2), DATEPART(dd, ETMS2.TMSDateTime)) + '-' + 
	                CONVERT(varchar(4), DATEPART(year, ETMS2.TMSDateTime))
                ,101) 
                = SET_DateTime.TheDate

                 where dateName(Month, SET_DateTime.TheDate) = @Month
                 and dateName(Year, SET_DateTime.TheDate) = @Year
                 and EP.personID = @PersonID
                 
                ) as fintab

                group by TheDate,
                fintab.CompanyID,fintab.PersonID,EmployeeCode,FullName,DesignationName,DepartmentName,
                IsHoliday,fintab.DayDescription,ShiftName,
                STimeIN,STimeOUT,fintab.TerminalNo,fintab.JoiningDate
        
	        ) TAB
	
	        Group by CompanyID,PersonID,EmployeeCode,FullName,DesignationName,
	        DepartmentName,ShiftName,TheDate,Day_Name,JoiningDate

	        Order by EmployeeCode 
";

        const string Query_EmployeeRecord = @"


select  distinct
------ Employee Information ---- 
HRMS_SET_Employee.EmployeeCode
,HRMS_SET_Employee.PersonID as EmpPersonID
,HRMS_SET_Employee.FullName  as EmpName
,HRMS_SET_Employee.FatherName as EMPFatherName
,HRMS_SET_Employee.Dateofbirth as EMP_DOB
,HRMS_SET_Employee.AddressLine1 +','+HRMS_SET_Employee.AddressLine2 +','+HRMS_SET_Employee.AddressLine1 
as EMP_Address
,HRMS_SET_Employee.Email
, case HRMS_SET_Employee.Gender
when 0 then 'Male'
when 1 then 'Female'
else null end  as EMP_Gender
,empdep.DepartmentName  as Emp_DepName
,empJDes.JobDesignationName as EMPJobDesgName
,HRMS_SET_Employee.Picture
from HRMS_SET_Employee
 
 left join SET_Department empdep on empdep.DepartmentID =  HRMS_SET_Employee.FK_DepartmentID 
 and empdep.Status =1 
 
 left join SET_JobDesignation empJDes on empJDes.JobDesignationID = HRMS_SET_Employee.FK_JobDesignationID
 and empJDes.Status =1 
 and HRMS_SET_Employee.Status =1 
 
 where HRMS_SET_Employee.CompanyID = @CompanyID
 
  {0}
order by HRMS_SET_Employee.PersonID 
 
 ----------------Employee Status
 
 select 
 HRMS_EmployeeStatus.PersonID as E_SPersonID
 ,HRMS_SET_EmployeeStatus.StatusName 
,HRMS_EmployeeStatus.Comments as StatusComments
,HRMS_EmployeeStatus.StatusDate

from dbo.HRMS_EmployeeStatus 
left
 join HRMS_SET_EmployeeStatus on HRMS_SET_EmployeeStatus.StatusID =  HRMS_EmployeeStatus.EmployeeStatusID
where HRMS_SET_EmployeeStatus.status =1 
and HRMS_EmployeeStatus.Status =1 
order by  HRMS_EmployeeStatus.PersonID

--------------- Transfer Posting


select 
 HRMS_TransferPosting.PersonID as  E_TPPersonID
,SET_Business.BusinessName +'-'+ SET_OperatingUnit.OperatingUnitName 
as CompanyFrom
,buss.BusinessName +'-'+ opt.OperatingUnitName 
as CompanyTo
,unitfrom.OperatingUnitName as UnitFrom
,unitto.OperatingUNitName as UnitTo
,lctfrom.LocationName as LocationFrom
,lctto.LocationName as LocationTo
,dptfrom.DepartmentName as DepartmentFrom
,dptto.DepartmentName as DepartmentTo
,desgfrom.DesignationName as DesignationFrom
,desgto.DesignationName as DesignationTo
,shiftfrom.Description as ShiftFrom 
,shiftto.Description as ShiftTo
from
   
 HRMS_TransferPosting
 left join  SET_Company on SET_Company.CompanyID =  HRMS_TransferPosting.FromCompany
 and SET_Company.Status =1 
 and HRMS_TransferPosting.Status =1 
 left join SET_OperatingUnit
  on SET_OperatingUnit.OperatingUnitID  = SET_Company.OperatingUnitID
  and SET_OperatingUnit.Status =1 
  
  
  left join SET_Business on SET_Business.BusinessID  =  SET_Company.BusinessID 
  and SET_Business.Status =1 

left join  SET_Company SET_com on SET_com.CompanyID =  HRMS_TransferPosting.ToCompany
 and SET_com.Status =1 
 
 left join SET_OperatingUnit opt
  on opt.OperatingUnitID  = SET_com.OperatingUnitID
  and opt.Status =1 
  
  left join SET_Business buss on buss.BusinessID  =  SET_com.BusinessID 
  and buss.Status =1 

left join SET_OperatingUnit unitfrom
  on unitfrom.OperatingUnitID  = HRMS_TransferPosting.FromUnit
  and unitfrom.Status =1 
  
left join SET_OperatingUnit unitto
  on unitto.OperatingUnitID  = HRMS_TransferPosting.ToUnit
  and unitto.Status =1 
  
left join SET_Location lctfrom on lctfrom.LocationID =  HRMS_TransferPosting.FromLocation
and lctfrom.Status =1 


left join SET_Location lctto on lctto.LocationID =  HRMS_TransferPosting.ToLocation
and lctto.Status =1 

left join Set_department dptfrom on dptfrom.DepartmentID =  HRMS_TransferPosting.FromDepartment
and dptfrom.Status =1 


left join Set_department dptto on dptto.DepartmentID =  HRMS_TransferPosting.ToDepartment
and dptto.Status =1 

left join SET_Designation desgfrom on  desgfrom.DesignationID =  HRMS_TransferPosting.FromDesignation
and desgfrom.Status =1 


left join SET_Designation desgto on  desgto.DesignationID =  HRMS_TransferPosting.FromDesignation
and desgto.Status =1 

left join HRMS_SET_Shift shiftfrom on shiftfrom.ShiftID =  HRMS_TransferPosting.FromShift
and shiftfrom.Status =1 


left join HRMS_SET_Shift shiftto on shiftto.ShiftID =  HRMS_TransferPosting.ToShift
and shiftto.Status =1 
and HRMS_TransferPosting.CompanyID = @CompanyID
and HRMS_TransferPosting.PeriodID = @PeriodID

order by HRMS_TransferPosting.PersonID
--------- Employee Education
select 
HRMS_EmployeeEducation.PersonID as E_EdPersonID
, HRMS_EmployeeEducation.Degree
,HRMS_EmployeeEducation.DateFrom as EDUDateFrom
,HRMS_EmployeeEducation.DateTo as EDUDateTO
,HRMS_EmployeeEducation.Institute
,HRMS_EmployeeEducation.GradingCriteria
,HRMS_EmployeeEducation.KeySubjects
,HRMS_EmployeeEducation.Grade
,HRMS_EmployeeEducation.Comments as EducationComments
,[LookupDetail].[EnumeratedDescription] as GradingCriteriaName
,eduCntry.CountryName as EducationCountry
 from  
 
  HRMS_EmployeeEducation 

left join SET_Country eduCntry on eduCntry.CountryID = HRMS_EmployeeEducation.Country
and eduCntry.Status =1 

left join [LookupDetail] on [LookupDetail].LookupDetailID  =  HRMS_EmployeeEducation.GradingCriteria 
and [LookupDetail].LookupHeaderID = 18 
and HRMS_EmployeeEducation.Status =1 
and HRMS_EmployeeEducation.CompanyID = @CompanyID
and HRMS_EmployeeEducation.PeriodID = @PeriodID

order by HRMS_EmployeeEducation.PersonID

------------- Employee Experience------
select 
HRMS_EmployeeExperience.PersonID as  E_ExPersonID
,expCntry.CountryName as ExperienceCountry
,HRMS_EmployeeExperience.CompanyName as EXPCompnay
,HRMS_EmployeeExperience.Department as EXPDepartment
,HRMS_EmployeeExperience.Designation as EXPDesignation
,HRMS_EmployeeExperience.DateFrom as ExperienceDateFrom
,HRMS_EmployeeExperience.DateTo  as ExperienceDateTo
,HRMS_EmployeeExperience.Salary
,HRMS_EmployeeExperience.Comments
from 

  HRMS_EmployeeExperience

left join SET_Country expCntry on expCntry.CountryID = HRMS_EmployeeExperience.Country
and expCntry.Status =1 
and HRMS_EmployeeExperience.Status =1 
and HRMS_EmployeeExperience.CompanyID = @CompanyID
and HRMS_EmployeeExperience.PeriodID = @PeriodID
order by HRMS_EmployeeExperience.PersonID


----------HRMS_Employee   Relatives-------------
select
 HRMS_EmployeeRelatives.PersonId as E_RPersonID
, HRMS_EmployeeRelatives.DocumentNo
,HRMS_EmployeeRelatives.Name as RelativeName
,HRMS_EmployeeRelatives.Age as RELAge
,HRMS_EmployeeRelatives.AgeType as RELAgeType
,HRMS_EmployeeRelatives.DateOfBirth as REL_DOB 
,HRMS_EmployeeRelatives.ContactNo as RELContactNo
,HRMS_EmployeeRelatives.Relation
,HRMS_EmployeeRelatives.MotherName as RELMotherName
,HRMS_EmployeeRelatives.CNICNo as REL_CNICNo
,HRMS_EmployeeRelatives.Qualification as REL_Qualification
,HRMS_EmployeeRelatives.MaritalStatus as REL_M_Status 
 , case  HRMS_EmployeeRelatives.UnderEmploymentSupport
 when 1 then 'Yes'
 When 0 then 'No'
  else null end 
  as REL_U_Emp_Support
  ,case  HRMS_EmployeeRelatives.BenefitEntitlement
 when 1 then 'Yes'
 When 0 then 'No'
  else null end 
  as REL_B_Ent
  
,case  HRMS_EmployeeRelatives.IsActiveEmployee
 when 1 then 'Yes'
 When 0 then 'No'
  else null end 
  as REL_JobinSameOrg
  ,HRMS_EmployeeRelatives.EmployeeCode as REL_EmpCode
   , HRMS_EmployeeRelatives.OnJobOrganizationName
   from 
    HRMS_EmployeeRelatives 
    inner join HRMS_SET_Employee on  HRMS_SET_Employee.PersonID =  HRMS_EmployeeRelatives.PersonID
    and HRMS_SET_Employee.CompanyID =  HRMS_EmployeeRelatives.CompanyID
where  HRMS_EmployeeRelatives.status =1 
and HRMS_EmployeeRelatives.CompanyID = @CompanyID
and HRMS_EmployeeRelatives.PeriodID = @PeriodID

order by HRMS_EmployeeRelatives.PersonId
";
       const string query_GetEmployeeLeaveBalance = @"

 Select HRMS_Set_Employee.FullName,HRMS_Set_Employee.FatherName,HRMS_Set_Employee.PersonID,HRMS_Set_Employee.EmployeeCode,
        HRMS_PayrollType.Description as LeaveTypeName,isnull(((HRMS_EmployeePayRoll.LeaveDays)-isnull(Sum(HRMS_EmployeeAttendance.Quantity),0)),0)
        as Balance,HRMS_EmployeePayRoll.PayRollTypeID,
        isnull((HRMS_EmployeePayRoll.LeaveDays),0) as LeaveDays,isnull(SUM(HRMS_EmployeeAttendance.Quantity),0) as Avail

        FROM HRMS_Set_Employee 

		inner JOIN HRMS_EmployeePayRoll ON HRMS_EmployeePayRoll.PersonID = HRMS_Set_Employee.PersonID
		AND HRMS_EmployeePayRoll.CompanyID = HRMS_Set_Employee.CompanyID 
		
		inner JOIN HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_EmployeePayRoll.PayrollTypeID
		AND HRMS_PayrollType.CompanyID = HRMS_EmployeePayRoll.CompanyID		
		
		left join HRMS_EmployeeAttendance on
	    HRMS_EmployeePayroll.PersonID = HRMS_EmployeeAttendance.PersonID and
	    HRMS_EmployeePayroll.PayrollTypeID = HRMS_EmployeeAttendance.LeaveID and	 
	    HRMS_EmployeePayroll.CompanyID = HRMS_EmployeeAttendance.CompanyID 
	    
		where HRMS_Set_Employee.CompanyID = @CompanyID 
		and HRMS_EmployeePayRoll.PersonID = @PersonID
		and HRMS_PayrollType.PayRollTypeCode = @PayRollTypeCode
		and HRMS_Set_Employee.Status =1
		and HRMS_EmployeePayRoll.Status =1
		and HRMS_PayrollType.Status = 1
	    and HRMS_PayrollType.Description like '%Annual%'

		group by 

		HRMS_Set_Employee.FullName,HRMS_Set_Employee.FatherName,
		HRMS_Set_Employee.PersonID,HRMS_Set_Employee.EmployeeCode,
        HRMS_PayrollType.Description,HRMS_EmployeePayRoll.LeaveDays,
        HRMS_EmployeePayRoll.PayRollTypeID
";

       const string Query_GetJoiningLetter = @"
    select format(convert(float(50), isnull(replace(HRMS_SET_Employee.NICNumber, '-',''),0)),'#####-#######-#') as NICNumber,
        format(convert(float(50), isnull(replace(HRMS_SET_Employee.MobileNumber, '-',''),0)),'0###-#######') as MobileNumber,
        HRMS_SET_Employee.FullName,HRMS_SET_Employee.AddressLine1, HRMS_SET_Employee.FK_JobDesignationID,
        HRMS_SET_Employee.JoiningDate, HRMS_SET_Employee.BasicSalary, SET_JobDesignation.JobDesignationName,
		SET_Business.BusinessName,SET_OperatingUnit.LogoImage,
		 SET_OperatingUnit.OperatingUnitName, HRMS_SET_Employee.FatherName
   ,* from HRMS_SET_Employee

   inner join SET_JobDesignation on SET_JobDesignation.JobDesignationID = HRMS_Set_Employee.FK_JobDesignationID
           and SET_JobDesignation.Status= 1 
	inner join SET_OperatingUnit on SET_OperatingUnit.BusinessId = HRMS_SET_Employee.CompanyID

	inner join SET_Business on SET_Business.BusinessId = SET_OperatingUnit.BusinessId


where HRMS_Set_Employee.CompanyID = @CompanyID
    and HRMS_SET_Employee.Status = 1
	and SET_JobDesignation.Status = 1

	{0}


  ";

       const string Query_GetEmployeeExperienceLetter = @"

    select HRMS_Set_Employee.fullname, HRMS_Set_Employee.fathername,HRMS_Set_Employee.FK_JobDesignationID, 
       HRMS_Set_Employee.JoiningDate, HRMS_Set_Employee.JobEndingDate, HRMS_Set_Employee.BasicSalary ,
       SET_JobDesignation.JobDesignationName  , UsersDetail.CreatedByValue as UserName , 
	   UserDesignation.JobDesignationName as UserDesignation, HRMS_Set_Employee.PersonID,Set_Business.BusinessName,
	   SET_OperatingUnit.LogoImage
from HRMS_Set_Employee

inner join SET_JobDesignation on SET_JobDesignation.JobDesignationID = HRMS_Set_Employee.FK_JobDesignationID
           and SET_JobDesignation.Status= 1 

 left join UsersDetail on UsersDetail.CreatedByValue = HRMS_Set_Employee.CreatedByValue	 


 left join SET_JobDesignation as UserDesignation on UserDesignation.JobDesignationID =  UsersDetail.DesignationID

inner join SET_OperatingUnit on SET_OperatingUnit.BusinessId = HRMS_SET_Employee.CompanyID
 and SET_OperatingUnit.Status = 1
	inner join SET_Business on SET_Business.BusinessId = SET_OperatingUnit.BusinessId
	and SET_Business.Status = 1

   where HRMS_Set_Employee.CompanyID = @CompanyID
    and HRMS_SET_Employee.Status = 1

    {0}

   ";

       const string Query_GetEmployeeIdCardInformation = @"

            Select HRMS_Set_Employee.FullName, HRMS_Set_Employee.FK_JobDesignationID, HRMS_Set_Employee.MobileNumber,
       HRMS_Set_Employee.AddressLine1,HRMS_Set_Employee.PhoneNumber1,HRMS_Set_Employee.DateOfBirth,
	   HRMS_Set_Employee.Email,HRMS_Set_Employee.CNICNumber,HRMS_Set_Employee.BloodGroup,
	   HRMS_Set_Employee.Picture,HRMS_Set_Employee.PersonID,
	   SET_JobDesignation.JobDesignationName,
	   SET_OperatingUnit.AddressLine1 +' : '+ SET_OperatingUnit.PhoneNumber as CompanyAddressPhoneNo ,
       SET_OperatingUnit.PhoneNumber as CompanyPhoneNo,
	   SET_OperatingUnit.LogoImage,SET_Business.BusinessName


 from HRMS_Set_Employee

    Inner join SET_JobDesignation on SET_JobDesignation.JobDesignationID = HRMS_Set_Employee.FK_JobDesignationID
	      and SET_JobDesignation.Status = 1

	inner join SET_OperatingUnit on SET_OperatingUnit.BusinessId = HRMS_SET_Employee.CompanyID

	inner join SET_Business on SET_Business.BusinessId = SET_OperatingUnit.BusinessId


where HRMS_Set_Employee.CompanyID = @CompanyID
    and HRMS_SET_Employee.Status = 1
{0}

";


        const string Query_GetEmployeeMasterDetail = @"


Select 

distinct

        case
         when  HRMS_Set_Employee.PersonTypeCode = 1 then 'Regular'
        when HRMS_Set_Employee.PersonTypeCode = 2 then 'PartTime'
         when HRMS_Set_Employee.PersonTypeCode = 3 then 'FullTime'
         when HRMS_Set_Employee.PersonTypeCode = 4 then 'Contract'
         when HRMS_Set_Employee.PersonTypeCode = 5 then 'Temporary'
         when HRMS_Set_Employee.PersonTypeCode = 6 then 'SeasonalContract'
          end as PersonStatus,
          
        case
         when  HRMS_Set_Employee.StatusID = 1 then 'Permanent'
         when HRMS_Set_Employee.PersonTypeCode = 2 then 'Contract'
         when HRMS_Set_Employee.PersonTypeCode = 3 then 'DailyWages'
         when HRMS_Set_Employee.PersonTypeCode = 4 then 'SeasonalContract'
         when HRMS_Set_Employee.PersonTypeCode = 5 then 'SeasonalDailyWages'
          end as PersonType,
          
         case 
         when HRMS_Set_Employee.gender = 0 then 'Male'
         when HRMS_Set_Employee.gender = 0 then 'Female'
         end as Gender
         ,
	case 
       
         when HRMS_Set_Employee.FK_MaritalStatusID = 1 then 'Married'
         when HRMS_Set_Employee.FK_MaritalStatusID = 2 then 'Single'
         when HRMS_Set_Employee.FK_MaritalStatusID = 3 then 'Divorced'
         when HRMS_Set_Employee.FK_MaritalStatusID = 4 then 'Widowed'
         when HRMS_Set_Employee.FK_MaritalStatusID = 5 then 'Separated'
         when HRMS_Set_Employee.FK_MaritalStatusID = 6 then 'Domestic Partner'
			 end 
				as MaritalStatus
,        HRMS_Set_Employee.Suffix
        ,HRMS_Set_Employee.Picture
        ,HRMS_Set_Employee.FullName,HRMS_Set_Employee.FatherName
        ,HRMS_SET_Employee.SocialSecNo
		,CONVERT (varchar(20), HRMS_SET_Employee.SocSecJoinDate, 106)  
				as SocSecJoinDate
		,HRMS_Set_Employee.PersonID
		,HRMS_Set_Employee.AddressLine1
		,HRMS_Set_Employee.AddressLine2
		,HRMS_Set_Employee.AddressLine3
		,HRMS_Set_Employee.MobileNumber
		,HRMS_Set_Employee.PhoneNumber1
		,HRMS_Set_Employee.PhoneNumber2
		,HRMS_Set_Employee.OfficeNumber
		,HRMS_Set_Employee.EmergencyContactNumber
		,HRMS_Set_Employee.Email
		,HRMS_Set_Employee.Nationality
		,HRMS_Set_Employee.Email
		,Set_country.CountryName
		,SET_City.CityName
		,HRMS_Set_Employee.Domicile
		,HRMS_Set_Employee.PassportNo
		,HRMS_Set_Employee.CNICNumber
        ,HRMS_Set_Employee.CNICNumber as CNICNo
		,HRMS_Set_Employee.ApplicantNumber
		,HRMS_SET_Employee.HoldApplicantDateUntil
		,HRMS_SET_Employee.JobStatusCode
		,HRMS_SET_Employee.RecommendedBy1
		,HRMS_SET_Employee.RecommendedBy2
		,HRMS_SET_Employee.RecommendedBy3
		,HRMS_SET_Employee.RehireReason
		,HRMS_SET_Employee.RehireRecommendation
		,HRMS_SET_Employee.ExtraComments
		,HRMS_SET_Employee.NICNumber
		,HRMS_SET_Employee.BloodGroup
		,HRMS_SET_Employee.NTNNo
		,case
			when  HRMS_SET_Employee.IsCurrentEmployee =1 then 'Yes'
			when HRMS_SET_Employee.IsCurrentEmployee = 0 then 'No'
				end as IsCurrentEmployee
		,case
			when  HRMS_SET_Employee.IsEOBIMember =1 then 'Yes'
			when HRMS_SET_Employee.IsEOBIMember = 0 then 'No'
				end as IsEOBIMember
		,HRMS_SET_Employee.EmployeeCode
		,CONVERT (varchar(20), HRMS_SET_Employee.AppointedOn  , 106)
				as AppointedDate
		,CONVERT(varchar(20),  HRMS_SET_Employee.JoiningDate  ,106 )  
				as JoiningDate
		,convert (varchar(20),  HRMS_SET_Employee.EffectiveStartDate  ,106) 
				as EffectiveStartDate
		 ,convert( varchar(20), HRMS_SET_Employee.EffectiveEndDate , 106 ) 
				as EffectiveEndDate
		,HRMS_SET_Employee.LastJob
		,HRMS_Set_Employee.BasicSalary
        ,HRMS_Set_Employee.JoiningSalary
		,AR_SET_Customer.CustomerAccountCCID
		,SET_JobCategory.JobCategoryName
		,SET_JobDesignation.JobDesignationName
		,SET_Grade.GradeName
		,SET_Section.SectionName
		,AR_SET_BankBranch.BankBranchName
		,AR_SET_Bank.BankName
		,AR_SET_Customer.Accountnumber
		,HRMS_SET_Employee.IsResumeExist
		,convert ( varchar(20), HRMS_SET_Employee.ResumeLastUpdate , 106)  
				as ResumeLastUpdate
		,HRMS_SET_Employee.IsBackgroundChecked
		,convert (varchar (20), HRMS_SET_Employee.BackgroudCheckDate , 106) 
				as BackgroudCheckDate
		,Set_Designation.DesignationName as Designation
		,SET_Department.DepartmentName as Department
		,convert (varchar(20), HRMS_Set_Employee.DateOfBirth ,106 ) 
				as DateOfBirth
		,HRMS_Set_Shift.Description as Shift
		,HRMS_Set_Shift.ShiftID
		,SET_Department.DepartmentID
		,Set_Designation.DesignationID
		,set_Location.LocationID
		,set_Location.Description as Location,Set_Company.CompanyID


		----------------------

		,HRMS_Set_Employee.Area
		,HRMS_Set_Employee.[ExtNo]
		,HRMS_Set_Employee.[EmergencyContactPersonName]
		,case 
		when  HRMS_Set_Employee.[ProvinceID] = 1 then 'Balochistan' 
		when  HRMS_Set_Employee.[ProvinceID] = 2 then 'KPK'
		when  HRMS_Set_Employee.[ProvinceID] = 3 then 'Punjab'
		when  HRMS_Set_Employee.[ProvinceID] = 4 then 'Sindh'
		end as Province
		,HRMS_Set_Employee.Email
		,HRMS_Set_Employee.PassportNo
		,convert(varchar(20), HRMS_Set_Employee.PassportExpiryDate ,106)  as PassportExpiryDate
		, case 
		when  HRMS_Set_Employee.IsSecondPassportExist =  1  then 'Yes' 
		when  HRMS_Set_Employee.IsSecondPassportExist =  0  then 'No'
		end as SecondPassportExist 
		,HRMS_Set_Employee.MobileNumber
		,HRMS_Set_Employee.MobileNumber2
		,HRMS_Set_Employee.Domicile
		,HRMS_Set_Employee.ApplicantNumber
		, case 
		when  HRMS_Set_Employee.IsOvertime = 1  then 'Yes'
		when  HRMS_Set_Employee.IsOvertime = 0  then 'No'
		end as OverTimeGiven
		,HRMS_Set_Employee.RecommendedByID1
		,HRMS_Set_Employee.RecommendedByID2
		,HRMS_Set_Employee.RecommendedByID3
		,HRMS_Set_Employee.EOBINo
		 ,convert (varchar(20) ,HRMS_Set_Employee.EOBIJoinDate  , 106)  as EOBIJoiningDate
		,case 
		when  HRMS_Set_Employee.IsPFMember =  1 then 'Yes'
		when  HRMS_Set_Employee.IsPFMember =  0 then 'No'
		end  as
		PFMember
		,HRMS_Set_Employee.MotherLanguage
		,
		case 
		when HRMS_Set_Employee.IsStudent =  1 then 'Yes'
		when HRMS_Set_Employee.IsStudent =  0 then 'No'
		end as IsStudent
		, case
		when HRMS_Set_Employee.IsOnMilitaryService =  1 then 'Yes'
		when HRMS_Set_Employee.IsOnMilitaryService =  0 then 'No'
		end as IsONMilitiryService
		,case 
		when  HRMS_Set_Employee.UseTobacco =  1  then 'Yes'  
		when  HRMS_Set_Employee.UseTobacco =  0  then 'No'  
		end  as UseTobacco
		,convert (varchar(20), HRMS_Set_Employee.ReceiptOfDeathCertDate ,106) as ReceiptOfDeathCertDate
		 ,convert (varchar(20), HRMS_Set_Employee.DateOfDeath , 106) as DateOfDeath
		,HRMS_Set_Employee.DrivingLicense
		,convert ( varchar(20), HRMS_Set_Employee.DrivingLicenseExpiryDate ,106)  AS	DrivingLicenseExpiryDate
		,case 
		when HRMS_Set_Employee.IsWillingTravelInPakistan = 1 then 'Yes'
		  when HRMS_Set_Employee.IsWillingTravelInPakistan = 0 then 'No'
		  end as IsWillingTravelInPakistan
		,case 
		when HRMS_Set_Employee.IsWillingTravelAbroad =  1 then 'Yes' 
		when HRMS_Set_Employee.IsWillingTravelAbroad =  0 then 'No' 
		end as WillingtoTravelAbroad
		,HRMS_Set_Employee.OldEmployeeCode
		,convert ( varchar (20),  HRMS_Set_Employee.AppointedOn  ,  106) as AppointedOnDate
		 ,convert (varchar(20), HRMS_Set_Employee.JoiningDate,106) as JoiningDate
		,case 
		when  HRMS_Set_Employee.IsEligiableForAppraisal = 1  then  'Yes' 
		when  HRMS_Set_Employee.IsEligiableForAppraisal = 0  then  'No' 
		end as EligibleForAppraisal
		,HRMS_Set_Employee.AppraisalFrequency
		 ,convert ( varchar(20), HRMS_Set_Employee.AppraisalDate ,106) as AppraisalDate
		,convert (varchar(20), HRMS_Set_Employee.AppraisalDueDate , 106 ) as AppraisalDueDate 
		,Case
		when  HRMS_Set_Employee.IsResumeExist =  1 then 'Yes' 
		when  HRMS_Set_Employee.IsResumeExist =  0 then 'No' end
		as ResumeExist
		,HRMS_Set_Employee.FileNumber
		,convert ( varchar(20), HRMS_Set_Employee.DateEmployeeDataVerified ,106 )  as DateEmployeeDataVerified
		 ,convert( varchar(20), HRMS_Set_Employee.OrignalDateOfHire, 106) as OrignalDateOfHire
		, convert (varchar (20),  HRMS_Set_Employee.JobEndingDate ,106)  as JobEndingDate
		,HRMS_Set_Employee.Religion
		,HRMS_Set_Employee.LanguageSpoken
		,HRMS_Set_Employee.LanguageSpoken2
		,HRMS_Set_Employee.LanguageSpoken3
		,HRMS_Set_Employee.EmployeeHeight
		,HRMS_Set_Employee.EmployeeWeight
		 ,case 
		 when  HRMS_Set_Employee.AvalibilityinShift = 1 then 'Yes'
		 when  HRMS_Set_Employee.AvalibilityinShift = 0 then 'No'
		 end as  AvailableINShift
		 ,HRMS_Set_Employee.Fluency
		,HRMS_Set_Employee .ReferencePersonName
		,HRMS_Set_Employee.ReferencePersonFatherName
		,HRMS_Set_Employee.ReferencePersonContactNo
		,HRMS_Set_Employee.ReferencePersonEmail
		,HRMS_Set_Employee.ReferencePersonOfficeName
		,HRMS_Set_Employee.ReferencePersonjobDesignation
		,HRMS_Set_Employee.EmployeeReferenceDepartment1
		,HRMS_Set_Employee .ReferencePersonName2
		,HRMS_Set_Employee.ReferencePersonFatherName2
		,HRMS_Set_Employee.ReferencePersonContactNo2
		,HRMS_Set_Employee.ReferencePersonEmail2
		,HRMS_Set_Employee.ReferencePersonOfficeName2
		,HRMS_Set_Employee.ReferencePersonjobDesignation2
		,HRMS_Set_Employee.EmployeeReferenceDepartment2
		,HRMS_Set_Employee.FamilyDoctorName
		,HRMS_Set_Employee.[HospitalName]
		,HRMS_Set_Employee.HospitalContact
		,HRMS_Set_Employee.HospitalAddress
		,HRMS_Set_Employee.LastMedicalTestBy
		,convert( varchar(20),  HRMS_Set_Employee.LastMedicalTestDate ,106) as LastMedicalTestDate
		,HRMS_Set_Employee.MedicalIllness
		,case 
		when  HRMS_Set_Employee.CoveredLifeInsurance  = 'true' then 'Yes' 
		when  HRMS_Set_Employee.CoveredLifeInsurance  = 'false' then 'No'
		end
		as CoveredLifeInsurance
		,HRMS_Set_Employee.LifePremium
		,HRMS_Set_Employee.LifeCategory
		,HRMS_Set_Employee.SumAssured
		,case 
		when  HRMS_Set_Employee.IsHealthInsurance = 1 then 'Yes'
		when  HRMS_Set_Employee.IsHealthInsurance = 0 then 'No'
		end as HealthInsured
		,HRMS_Set_Employee.HealthInsuranceNo
		,HRMS_Set_Employee.HealthInsuranceCategory
		,HRMS_Set_Employee.HealthInsurancePremium
		,HRMS_Set_Employee.HealthCertID


FROM HRMS_Set_Employee 
		
		left join Set_Designation on Set_Designation.DesignationID =  HRMS_SET_Employee.FK_JobDesignationID
--		AND Set_Designation.CompanyID = HRMS_Set_Employee.CompanyID
		and Set_Designation.status =1
		
        left join SET_Department on SET_Department.DepartmentID = HRMS_SET_Employee.FK_DepartmentID
        and SET_Department.status =1
        
        left join Set_Company on  Set_Company.CompanyID = HRMs_SET_Employee.CompanyID
        and Set_Company.Status=1

        left join HRMS_EmployeeShift on HRMS_EmployeeShift.PersonID = HRMS_SET_Employee.PersonID
        and HRMS_EmployeeShift.Status = 1
        
        left join HRMS_Set_Shift on HRMS_Set_Shift.ShiftID = HRMS_EmployeeShift.ShiftID 
        and HRMS_Set_Shift.ShiftCode = HRMS_EmployeeShift.ShiftCode 
        and HRMS_SET_Shift.status =1 
        
        left join set_Location on set_Location.LocationID = HRMS_Set_Employee.FK_LocationID
        and set_Location.status = 1

   left join SET_Country on SET_Country.CountryID = HRMS_Set_Employee.FK_CountryID
        and SET_Country.status = 1
        
        left join SET_City on SET_City.CityID = HRMS_Set_Employee.FK_CityID
        and SET_City.status = 1
        
         inner join AR_SET_Customer on AR_SET_Customer.CustomerID = HRMS_SET_Employee.FK_CustomerID
        and AR_SET_Customer.CompanyID = HRMS_SET_Employee.CompanyID
        and AR_SET_Customer.Status = 1
      
        left join SET_JobCategory on SET_JobCategory.JobCategoryID =  HRMS_SET_Employee.FK_JobCategoryID
--        and SET_JobCategory.CompanyID = HRMS_SET_Employee.CompanyID
      and SET_JobCategory.Status =1
     
        left join SET_JobDesignation on SET_JobDesignation.JobDesignationID =  HRMS_SET_Employee.FK_JobDesignationID
--        and SET_JobDesignation.CompanyID = HRMS_SET_Employee.CompanyID
      and SET_JobDesignation.Status =1
      
      left join SET_Grade on SET_Grade.GradeID =  HRMS_SET_Employee.FK_GradeID
--      and SET_Grade.CompanyID = HRMS_SET_Employee.CompanyID 
      and SET_Grade.Status =1
    
    left join SET_Section on SET_Section.SectionID = HRMS_SET_Employee.FK_SectionID
          and SET_Section.Status =1
    
         left join AR_SET_BankBranch on AR_SET_BankBranch.BankBranchID = HRMS_SET_Employee.FK_BankBranchID
         and AR_SET_BankBranch.status =1 
         left join AR_SET_Bank on  AR_SET_Bank.BankID = AR_SET_BankBranch.BankID
         and AR_SET_Bank.status =1
    
    where HRMS_Set_Employee.CompanyID = @CompanyID
    and HRMS_SET_Employee.Status = 1
{0}
";

        const string query_EmployeeInfo = @" Select top 1 HRMS_Set_Employee.FullName,HRMS_Set_Employee.FatherName,HRMS_Set_Employee.PersonID,
HRMS_Set_Employee.BasicSalary,
Set_Designation.DesignationName as Designation,SET_Department.DepartmentName as Department,HRMS_Set_Employee.JoiningDate,
HRMS_Set_Employee.DateOfBirth,HRMS_Set_Employee.Email,HRMS_Set_Employee.AddressLine1,
Set_OperatingUnit.OperatingUnitName as Unit,HRMS_Set_Shift.Description as Shift,HRMS_Set_Shift.ShiftID,Set_OperatingUnit.OperatingUnitID,
SET_Department.DepartmentID,Set_Designation.DesignationID,set_Location.LocationID,set_Location.Description as Location,Set_Company.CompanyID
,SET_Grade.GradeID,SET_Section.SectionID,SET_JobCategory.JobCategoryID
FROM HRMS_Set_Employee 
		
		left join Set_Designation on Set_Designation.DesignationID =  HRMS_SET_Employee.FK_JobDesignationID
		AND Set_Designation.CompanyID = HRMS_Set_Employee.CompanyID
		and Set_Designation.status =1
		
        left join SET_Department on SET_Department.DepartmentID = HRMS_SET_Employee.FK_DepartmentID
        and SET_Department.status =1
        
        left join Set_Company on  Set_Company.CompanyID = HRMs_SET_Employee.CompanyID
        and Set_Company.Status=1
        
		left join Set_OperatingUnit on  Set_OperatingUnit.OperatingUnitID = Set_Company.OperatingUnitID
		and Set_OperatingUnit.status = 1
        
         left join HRMS_EmployeeShift on HRMS_EmployeeShift.PersonID = HRMS_Set_Employee.PersonID
		    and HRMS_EmployeeShift.CompanyID = HRMS_Set_Employee.CompanyID
			and HRMS_EmployeeShift.Status = 1

        left join HRMS_Set_Shift on HRMS_Set_Shift.ShiftID = HRMS_EmployeeShift.ShiftID
		     and HRMS_Set_Shift.Status = 1
        
        left join set_Location on set_Location.LocationID = HRMS_Set_Employee.FK_LocationID
        and set_Location.status = 1

 left join SET_Grade on SET_Grade.GradeID =  HRMS_SET_Employee.FK_GradeID
     -- and SET_Grade.CompanyID = HRMS_SET_Employee.CompanyID 
      and SET_Grade.Status =1
    
    left join SET_Section on SET_Section.SectionID = HRMS_SET_Employee.FK_SectionID
          and SET_Section.Status =1
          
           left join SET_JobCategory on SET_JobCategory.JobCategoryID =  HRMS_SET_Employee.FK_JobCategoryID
        --and SET_JobCategory.CompanyID = HRMS_SET_Employee.CompanyID
      and SET_JobCategory.Status =1
      
        where 
        HRMS_Set_Employee.CompanyID =@CompanyID         
        and HRMS_Set_Employee.PersonID = @PersonID";

        const string query_GetEmployeesForPermanant = @"

select HRMS_SET_Employee.EmployeeCode,HRMS_SET_Employee.PersonID,HRMS_SET_Employee.AppointedOn,HRMS_SET_Employee.JoiningDate,
HRMS_SET_Employee.FullName,HRMS_SET_Employee.PersonTypeCode,
HRMS_SET_Employee.FK_JobDesignationID,HRMS_Set_Employee.StatusID,
SET_JobDesignation.JobDesignationName,CAST(0 as bit) as PermanantCheck,
 case
         when HRMS_Set_Employee.StatusID = 1 then 'Regular'
         when HRMS_Set_Employee.StatusID = 2 then 'Part Time'
         when HRMS_Set_Employee.StatusID = 3 then 'Full Time'
         when HRMS_Set_Employee.StatusID = 4 then 'Contract'
         when HRMS_Set_Employee.StatusID = 5 then 'Temporary'
         when HRMS_Set_Employee.StatusID = 6 then 'Seasonal Contract'

  end as PersonType 
 from HRMS_SET_Employee
 left join SET_JobDesignation on SET_JobDesignation.JobDesignationID = HRMS_SET_Employee.FK_JobDesignationID
 and SET_JobDesignation.Status = 1
 where HRMS_SET_Employee.Status = 1 and HRMS_Set_Employee.StatusID = @StatusID
 and HRMS_SET_Employee.CompanyID = @CompanyID
";
        const string query_GetAllLeavesTypes = @"
select PayRollTypeID,Description from HRMS_PayrollType where PayrollTypeCode = 4
 and HRMS_PayrollType.Status = 1
 and HRMS_PayrollType.CompanyID = @CompanyID

";

        const string query_GetEmployeeInfo = @"

        Select top 1 HRMS_Set_Employee.FullName,HRMS_Set_Employee.FatherName,HRMS_Set_Employee.PersonID,
HRMS_Set_Employee.BasicSalary,HRMS_Set_Employee.PersonTypeCode,HRMS_Set_Employee.MobileNumber,
Set_Designation.DesignationName as Designation,SET_Department.DepartmentName as Department,HRMS_Set_Employee.JoiningDate,
HRMS_Set_Employee.DateOfBirth,HRMS_Set_Employee.Email,HRMS_Set_Employee.AddressLine1,
Set_OperatingUnit.OperatingUnitName as Unit,HRMS_Set_Shift.Description as Shift,HRMS_Set_Shift.ShiftID,Set_OperatingUnit.OperatingUnitID,
SET_Department.DepartmentID,Set_Designation.DesignationID,set_Location.LocationID,set_Location.Description as Location,Set_Company.CompanyID

FROM HRMS_Set_Employee 
		
		left join Set_Designation on Set_Designation.DesignationID =  HRMS_SET_Employee.FK_JobDesignationID
		--AND Set_Designation.CompanyID = HRMS_Set_Employee.CompanyID
		and Set_Designation.status =1
		
        left join SET_Department on SET_Department.DepartmentID = HRMS_SET_Employee.FK_DepartmentID
        and SET_Department.status =1
        
        left join Set_Company on  Set_Company.CompanyID = HRMs_SET_Employee.CompanyID
        and Set_Company.Status=1
        
		left join Set_OperatingUnit on  Set_OperatingUnit.OperatingUnitID = Set_Company.OperatingUnitID
		and Set_OperatingUnit.status = 1
        
        left join HRMS_EmployeeShift on HRMS_EmployeeShift.PersonID = HRMS_Set_Employee.PersonID
        
        left join HRMS_Set_Shift on HRMS_Set_Shift.ShiftID = HRMS_EmployeeShift.ShiftID
        
        left join set_Location on set_Location.LocationID = HRMS_Set_Employee.FK_LocationID
        and set_Location.status = 1

        where 
        HRMS_Set_Employee.CompanyID =@CompanyID         
        and HRMS_Set_Employee.PersonID = @PersonID   

";

        const string Query_Update_By_PersonID = @" UPDATE [HRMS_Set_Employee] SET  [BasicSalary] = @BasicSalary,[AppraisalDate] = @AppraisalDate, [AppraisalDueDate] = @AppraisalDueDate,[PersonTypeCode] = @PersonTypeCode , [FK_JobDesignationID] = @FK_JobDesignationID
WHERE [CompanyID] = @CompanyID and [PersonID] = @PersonID 
";

        const string Query_UpdatePersonType_By_PersonID = @" UPDATE [HRMS_Set_Employee] SET  [PersonTypeCode] = @PersonTypeCode 
WHERE [CompanyID] = @CompanyID and [PersonID] = @PersonID 
";

        const string Query_UpdatePersonStatus = @" UPDATE [HRMS_Set_Employee] SET  [StatusID] = @StatusID,[PersonTypeCode] = @PersonTypeCode ,[PermanentDate] = @PermanentDate
WHERE [CompanyID] = @CompanyID and [PersonID] = @PersonID 
";


        const string query_GetEmployeeInfoForPayRoll = @"
select EmployeePayrollID from HRMS_EmployeePayroll where PersonID = @PersonID and CompanyID = @CompanyID

";
        const string Query_GetEmployeesPermanentDate = @"select PersonID,EmployeeCode,FullName,PermanentDate,PersonTypeCode
from HRMS_Set_Employee
where [Status] = 1 and PersonTypeCode = 1 
and PermanentDate is not null
and CompanyID = @CompanyID
 ";
        const string Query_SelectAllData_WithPaging = @"SELECT HRMS_Set_Employee.*,SET_JobDesignation.JobDesignationName as Designation,CustomerAccountCCID
, ROW_NUMBER() Over (Order by HRMS_Set_Employee.CompanyID, HRMS_Set_Employee.PersonID) as 'Row' FROM [HRMS_Set_Employee]

	left join SET_JobDesignation on SET_JobDesignation.JobDesignationID =  HRMS_SET_Employee.FK_JobDesignationID
         and SET_JobDesignation.status =1
	inner join AR_SET_Customer on AR_SET_Customer.CustomerID=HRMS_Set_Employee.FK_CustomerID
		--AND SET_JobDesignation.CompanyID = HRMS_Set_Employee.CompanyID
		
    Inner join HRMS_EmployeeInEmployement_Status on HRMS_EmployeeInEmployement_Status.StatusID = HRMS_Set_Employee.StatusID
        and HRMS_EmployeeInEmployement_Status.[CompanyID] = HRMS_Set_Employee.[CompanyID]
";


        const string Query_SelectAllData_WithoutPaging = @"	SELECT HRMS_Set_Employee.*,SET_JobDesignation.JobDesignationName as Designation,CustomerAccountCCID
              FROM [HRMS_Set_Employee]
               
               	left join SET_JobDesignation on SET_JobDesignation.JobDesignationID =  HRMS_SET_Employee.FK_JobDesignationID
				inner join AR_SET_Customer on AR_SET_Customer.CustomerID=HRMS_Set_Employee.FK_CustomerID
		AND SET_JobDesignation.CompanyID = HRMS_Set_Employee.CompanyID
		and SET_JobDesignation.status =1 
       Inner join HRMS_EmployeeInEmployement_Status on HRMS_EmployeeInEmployement_Status.StatusID = HRMS_Set_Employee.StatusID
        and HRMS_EmployeeInEmployement_Status.[CompanyID] = HRMS_Set_Employee.[CompanyID]

  "; 



        const string query_GetEmployeesApprovedStrength = @"
select PersonID,FullName,EmployeeCode,FK_SectionID,FK_DepartmentID,FK_JobDesignationID
from HRMS_Set_Employee
where Status = 1 and FK_DepartmentID = @departmentid
and FK_JobDesignationID = @designationid
and FK_SectionID = @sectionid
";

        const string Query_GetNewApplicantNumber = @"SELECT IsNull(max(ApplicantNumber), 0) + 1 as PersonID FROM HRMS_Set_Employee WHERE [CompanyID] = @CompanyID ";

        const string Query_GetNewEmployeeID = @"SELECT IsNull(max(EmployeeID), 0) + 1 as PersonID FROM HRMS_Set_Employee WHERE [CompanyID] = @CompanyID ";
        const string Query_GetEmployeeIDByCode = @"SELECT IsNull(PersonID, 0) as PersonID FROM HRMS_Set_Employee WHERE [CompanyID] = @CompanyID and CAST(EmployeeCode as int) = @EmployeeCode";

        const string Query_GetEmployeeCodeByID = @"select 
case when (SELECT EmployeeCode FROM HRMS_Set_Employee WHERE [CompanyID] = @CompanyID and EmployeeCode = @PersonID) = @PersonID then
(SELECT PersonID as EmployeeCode FROM HRMS_Set_Employee WHERE [CompanyID] = @CompanyID and EmployeeCode = @PersonID)
else '' end";

        const string Query_GetEmployeeIDByCodeForAdam = @"SELECT IsNull(PersonID, 0) as PersonID FROM HRMS_Set_Employee WHERE [CompanyID] = @CompanyID and EmployeeCode = @EmployeeCode";

        const string Query_GetEmployeeBasicSalary = @"select BasicSalary from HRMS_SET_Employee where CompanyID =@CompanyID AND PersonID =@PersonID AND Status =1";


        const string FaimlyTracking_Quality_Report = @"
SELECT *,(Case when HRMS_Set_Employee.CNICNumber='' then null
else substring(HRMS_Set_Employee.CNICNumber,0,6) +'-'+
substring(HRMS_Set_Employee.CNICNumber,6,7) +'-'+
substring( HRMS_Set_Employee.CNICNumber ,13,len( HRMS_Set_Employee.CNICNumber )-1)
end )as CNICNumber1
FROM HRMS_Set_Employee
where CompanyID = @CompanyID --and PeriodID = @PeriodID
{0}
";

        const string Query_SelectAll_WithPagingEmp = @" SELECT  [PersonID],'' as Date,[FullName],Convert(int,ISnull(EmployeeCode,0)) as EmployeeCode,BasicSalary, 
convert(bit,0) as IsChecked,convert(bit,0) as IsOverTime,'' as OverTimeHours,ROW_NUMBER() Over (Order by CompanyID, PersonID) as 'Row' FROM [HRMS_Set_Employee] ";

        const string Query_SelectAll_WithoutPagingEmp = @" SELECT [PersonID],'' as Date,[FullName],Convert(int,ISnull(EmployeeCode,0)) as EmployeeCode,BasicSalary
,convert(bit,0) as IsChecked,convert(bit,0) as IsOverTime,'' as OverTimeHours FROM [HRMS_Set_Employee] ";

        const string Query_GetData_ForHiring = @"Select DateDiff(dd, joiningdate,@Date) as DateDiff,joiningdate,* from HRMS_Set_Employee
where Isactive = 1 and Status = 1 and
DateDiff(day, joiningdate,@Date) = @days";

        const string Query_SelectAll_WithPagingEmpRoaster = @" SELECT  [PersonID],[FullName],Convert(int,ISnull(EmployeeCode,0)) as EmployeeCode,FK_departmentID, 
convert(bit,0) as IsChecked, ROW_NUMBER() Over (Order by CompanyID, PersonID) as 'Row' FROM [HRMS_Set_Employee] ";

        const string Query_SelectAll_WithoutPagingEmpRoaster = @" sELECT [PersonID],[FullName],Convert(int,ISnull(EmployeeCode,0)) as EmployeeCode,FK_DepartmentID
,convert(bit,0) as IsChecked,GETDATE() as FromDate , GETDATE() as ToDate,-1 as SundayShift,-1 as MondayShift,-1 as TuesdayShift,-1 as WednesdayShift
,-1 as ThursdayShift, -1 as FridayShift,-1 as SaturdayShift   FROM [HRMS_Set_Employee] ";

        const string Query_GetNewEmployeeCode = @"SELECT IsNull(max(Cast(EmployeeCode as bigint )), 0) + 1 as PersonID FROM HRMS_Set_Employee WHERE HRMS_Set_Employee.CompanyID=@CompanyID";

        #endregion

        const string Query_MajorStaffAdvances = @"
select 'Staff Advances - against salaries' as Heading,  HRMS_Employement_Status.Description ,sum(AP_PaymentSupplierDetail.Amount) as Amount
​
from HRMS_Set_Employee
inner join AP_PaymentSupplierDetail on AP_PaymentSupplierDetail.PaymentSupplierID = HRMS_Set_Employee.FK_CustomerID
   and AP_PaymentSupplierDetail.CompanyID = HRMS_Set_Employee.CompanyID
   and AP_PaymentSupplierDetail.status = 1
inner join AP_PaymentHeader on AP_PaymentHeader.PaymentID = AP_PaymentSupplierDetail.PaymentHeaderID
   and AP_PaymentHeader.CompanyID = AP_PaymentSupplierDetail.CompanyID
   and AP_PaymentHeader.PeriodID = AP_PaymentSupplierDetail.PeriodID
   and AP_PaymentHeader.FiscalID = AP_PaymentSupplierDetail.FiscalID
   and AP_PaymentHeader.Status = 1
​
​
inner join HRMS_Employement_Status on HRMS_Employement_Status.StatusID =  HRMS_Set_Employee.StatusID
​
inner join GL_CodeCombinationDetail on GL_CodeCombinationDetail.CodeCombinationID = AP_PaymentSupplierDetail.SupplierAccountCode
      and GL_CodeCombinationDetail.CompanyID = AP_PaymentSupplierDetail.CompanyID
​
where GL_CodeCombinationDetail.Description like '%AGAINST SALARY%'
   and AP_PaymentHeader.CompanyID = @CompanyID
   and AP_PaymentHeader.PeriodID = @PeriodID
   and AP_PaymentHeader.FiscalID = @FiscalID
   and convert(date,AP_PaymentHeader.DocumentDate ) <= @FromDate
​
group by HRMS_Employement_Status.Description
​
union all
​
select 'Staff Advances - against expenses' as Heading,  HRMS_Employement_Status.Description ,sum(AP_PaymentSupplierDetail.Amount) as Amount
from HRMS_Set_Employee
inner join AP_PaymentSupplierDetail on AP_PaymentSupplierDetail.PaymentSupplierID = HRMS_Set_Employee.FK_CustomerID
  and AP_PaymentSupplierDetail.CompanyID = HRMS_Set_Employee.CompanyID
  and AP_PaymentSupplierDetail.Status = 1
​
inner join AP_PaymentHeader on AP_PaymentHeader.PaymentID = AP_PaymentSupplierDetail.PaymentHeaderID
  and AP_PaymentHeader.CompanyID = AP_PaymentSupplierDetail.CompanyID
  and AP_PaymentHeader.PeriodID = AP_PaymentSupplierDetail.PeriodID
  and AP_PaymentHeader.FiscalID = AP_PaymentSupplierDetail.FiscalID
  and AP_PaymentHeader.Status = 1
​
inner join HRMS_Employement_Status on HRMS_Employement_Status.StatusID =  HRMS_Set_Employee.StatusID
​
inner join GL_CodeCombinationDetail on GL_CodeCombinationDetail.CodeCombinationID = AP_PaymentSupplierDetail.SupplierAccountCode
  and GL_CodeCombinationDetail.CompanyID = AP_PaymentSupplierDetail.CompanyID
  and GL_CodeCombinationDetail.Status = 1
​
where GL_CodeCombinationDetail.Description like '%AGAINST EXPENSES%'
  and AP_PaymentHeader.CompanyID = @CompanyID
  and AP_PaymentHeader.PeriodID = @PeriodID
  and AP_PaymentHeader.FiscalID = @FiscalID
  and convert(date,AP_PaymentHeader.DocumentDate ) <= @FromDate
group by HRMS_Employement_Status.Description";

        #region Class Methods

        public static DataTable GetAllActiveDataCustome(string filter, Nullable<int> companyID, Nullable<bool> isActive = true,
            SqlConnection objConnection = null, SqlTransaction objTransaction = null, string NewSellectionMode = null, string UserID = "")
        {
            const string Query_Select = @" SELECT *,DepartmentID,DepartmentName,JobDesignationID as DesignationID,JobDesignationName as DesignationName FROM [HRMS_Set_Employee]  inner join SET_Department
	on 
	 SET_Department.DepartmentID=HRMS_Set_Employee.FK_DepartmentID
	 left join SET_JobDesignation on SET_JobDesignation.JobDesignationID  =  HRMS_Set_Employee.FK_JobDesignationID
	 inner join HRMS_EmployeeInEmployement_Status on HRMS_EmployeeInEmployement_Status.StatusID = HRMS_Set_Employee.StatusID
	 and HRMS_EmployeeInEmployement_Status.UserID = @UserID
     and HRMS_EmployeeInEmployement_Status.[CompanyID] = HRMS_Set_Employee.[CompanyID]
 
 WHERE HRMS_Set_Employee.Status = 1 and HRMS_Set_Employee.IsActive = @IsActive

AND ([HRMS_Set_Employee].[CompanyID]=@CompanyID OR [HRMS_Set_Employee].[CompanyID] is null)
";




            DataTable dt = null;
            string OverrideQueryString = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (isActive.HasValue)
                    sqlparams[1] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[1] = new SqlParameter("@IsActive", DBNull.Value);

                sqlparams[2] = new SqlParameter("@UserID", UserID);

                if (filter == string.Empty)
                {
                    if (NewSellectionMode == "OT")
                        OverrideQueryString = Query_EmployeeDLL_OT;
                    else
                        OverrideQueryString = Query_Select;
                }
                else
                {
                    OverrideQueryString = Query_With_Filter+filter;
                }
                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, OverrideQueryString, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, OverrideQueryString, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, OverrideQueryString, sqlparams);

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

        public static DataTable GetAllActiveDataCustomeralnoor(string filter, Nullable<int> companyID, Nullable<bool> isActive = true,
            SqlConnection objConnection = null, SqlTransaction objTransaction = null, string NewSellectionMode = null, string UserID = "")
        {
            const string Query_Select = @" SELECT Top 1 *,DepartmentID,DepartmentName,JobDesignationID as DesignationID,JobDesignationName as DesignationName FROM [HRMS_Set_Employee]  inner join SET_Department
	on 
	 SET_Department.DepartmentID=HRMS_Set_Employee.FK_DepartmentID
	 left join SET_JobDesignation on SET_JobDesignation.JobDesignationID  =  HRMS_Set_Employee.FK_JobDesignationID
	 inner join HRMS_EmployeeInEmployement_Status on HRMS_EmployeeInEmployement_Status.StatusID = HRMS_Set_Employee.StatusID
	 and HRMS_EmployeeInEmployement_Status.UserID = @UserID
     and HRMS_EmployeeInEmployement_Status.[CompanyID] = HRMS_Set_Employee.[CompanyID]
 
 WHERE HRMS_Set_Employee.Status = 1 and HRMS_Set_Employee.IsActive = 1

AND ([HRMS_Set_Employee].[CompanyID]=@CompanyID OR [HRMS_Set_Employee].[CompanyID] is null)
";




            DataTable dt = null;
            string OverrideQueryString = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (isActive.HasValue)
                    sqlparams[1] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[1] = new SqlParameter("@IsActive", DBNull.Value);

                sqlparams[2] = new SqlParameter("@UserID", UserID);

                if (filter == string.Empty)
                {
                    if (NewSellectionMode == "OT")
                        OverrideQueryString = Query_EmployeeDLL_OT;
                    else
                        OverrideQueryString = Query_Select;
                }
                else
                {
                    OverrideQueryString = Query_With_Filtershift + filter;
                }
                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, OverrideQueryString, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, OverrideQueryString, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, OverrideQueryString, sqlparams);

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

        public static DataTable GetAllActiveDataDepartmentWise( Nullable<int> companyID,Nullable<int>departmentID, Nullable<DateTime>Endingdate,Nullable<DateTime>StartDate,
    SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            #region Query

            const string Query_Select = @" SELECT [HRMS_Set_Employee].PersonID as EmployeeID, [HRMS_Set_Employee].FullName as EmployeeName, [HRMS_Set_Employee].JoiningDate, 
		DepartmentID,DepartmentName,JobDesignationID as DesignationID,JobDesignationName as DesignationName,
                                        BasicSalary as GrossSalary, 										(select Convert(int,(case when [HRMS_EmployeePayroll].CalculatedAmount = 0 then [HRMS_EmployeePayroll].ManualAmount else [HRMS_EmployeePayroll].CalculatedAmount end),0) as Basicsalary
			                                                			 from  [HRMS_EmployeePayroll] 
			                                                 where 
			                                                 [HRMS_EmployeePayroll].PayrollTypeID=2
			                                                 And  [HRMS_EmployeePayroll].Status=1
			                                                 And [HRMS_EmployeePayroll].PersonID=HRMS_Set_Employee.PersonID
			                                                 ) as Bonus, 'True' as Eligibility,0 as BonusAbsent,(Select 
			                                                			Convert(int,(case when [HRMS_EmployeePayroll].CalculatedAmount = 0 then [HRMS_EmployeePayroll].ManualAmount else [HRMS_EmployeePayroll].CalculatedAmount end),0) as Basicsalary
			                                                			 from  [HRMS_EmployeePayroll] 
			                                                 where 
			                                                 [HRMS_EmployeePayroll].PayrollTypeID=2
			                                                 And  [HRMS_EmployeePayroll].Status=1
			                                                 And [HRMS_EmployeePayroll].PersonID=HRMS_Set_Employee.PersonID
			                                                 )as BasicSalary,DATEDIFF(DAY,  HRMS_Set_Employee.JoiningDate,CURRENT_TIMESTAMP)as Days, '' as Action
                                        				                                        
                                        FROM [HRMS_Set_Employee]  
                                                inner join SET_Department on SET_Department.DepartmentID=HRMS_Set_Employee.FK_DepartmentID
                                                            	and SET_Department.Status=1
                                        	 left join SET_JobDesignation on SET_JobDesignation.JobDesignationID  =  HRMS_Set_Employee.FK_JobDesignationID
                                        	 And SET_JobDesignation.Status=1
                                         
                                         WHERE HRMS_Set_Employee.Status = 1
                                        
                                        AND ([HRMS_Set_Employee].[CompanyID]=@CompanyID )
                                        AND ([HRMS_Set_Employee].FK_DepartmentID=@DepartmentID or [HRMS_Set_Employee].FK_DepartmentID is null) 
                                        And (HRMS_Set_Employee.JoiningDate between @StartDate and @Endingdate)
                ";


            #endregion




            DataTable dt = null;
            
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (departmentID.HasValue)
                    sqlparams[1] = new SqlParameter("@DepartmentID", departmentID.Value);
                else
                    sqlparams[1] = new SqlParameter("@DepartmentID", DBNull.Value);

                if (Endingdate.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@Endingdate", Endingdate); 
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@Endingdate",DBNull.Value);
                }

                if (StartDate.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@StartDate", StartDate);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@StartDate", DBNull.Value);
                }
                
                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_Select, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_Select, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select, sqlparams);

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

        public static DataTable GetAllActiveDataEmployeStatusWise(DateTime MillStartDate, DateTime CurrentDate, int PresentDays, Nullable<int> companyID, Nullable<int> EmpStatusID, Nullable<int> TotalDays, Nullable<int> NoOfBonus = null, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            #region Query

            const string Query_BonusEmployee = @"Select Days, PersonID, CompanyID,-1 as BonusHeaderID,-1 as BonusDetailID, CEILING(BasicSalary) BasicSalary
--,Case when Days >= @totaldays then CEILING(BasicSalary) else CAst(Round((CEILING(BasicSalary)/@totaldays) * Days,0) AS NUmeric(36,0) ) end Bonus
,(case when isnull(@NoOfBonus,0) >= 1 then (Case when Days >= @totaldays then CEILING(BasicSalary) else CAst(Round((CEILING(BasicSalary)/@totaldays) * Days,0) AS NUmeric(36,0) ) end) * @NoOfBonus else (Case when Days >= @totaldays then CEILING(BasicSalary) else CAst(Round((CEILING(BasicSalary)/@totaldays) * Days,0) AS NUmeric(36,0) ) end) end ) as Bonus
, EmployeeName, EmployeeCode, JoiningDate, Eligibility, GrossSalary, EmployeeID, DesignationID, DesignationName, BonusAbsent, Action, StatusID, EmployeeStatusName, IncomeTax
--, NetBonusAmount

--, Case when Days >= @totaldays then CEILING(BasicSalary) + ArrearOfBonus - DeductionOfBonus - AdvanceSalary -IncomeTax 
--else Cast(Round((CEILING(BasicSalary)/@totaldays) * Days,0) + ArrearOfBonus - DeductionOfBonus - AdvanceSalary -IncomeTax AS NUmeric(36,0) )
--end  NetBonusAmount

,(case when isnull(@NoOfBonus,0) >= 1 then (Case when Days >= @totaldays then CEILING(BasicSalary) + ArrearOfBonus - DeductionOfBonus - AdvanceSalary -IncomeTax 
else Cast(Round((CEILING(BasicSalary)/@totaldays) * Days,0) + ArrearOfBonus - DeductionOfBonus - AdvanceSalary -IncomeTax AS NUmeric(36,0) )
end) * @NoOfBonus else (Case when Days >= @totaldays then CEILING(BasicSalary) + ArrearOfBonus - DeductionOfBonus - AdvanceSalary -IncomeTax 
else Cast(Round((CEILING(BasicSalary)/@totaldays) * Days,0) + ArrearOfBonus - DeductionOfBonus - AdvanceSalary -IncomeTax AS NUmeric(36,0) )
end) end ) as NetBonusAmount

, ArrearOfBonus, DeductionOfBonus, AdvanceSalary, IsChecked, DepartmentID 
from (

Select Distinct  Isnull(CAST((DATEDIFF(DAY,  (case when HRMS_Emp.JoiningDate > @MillStartDate then HRMS_Emp.JoiningDate else  @MillStartDate end) ,@CurrentDate) - 
(Select isnull(Sum(Quantity),0) from HRMS_EmployeeAttendance
 Where HRMS_EmployeeAttendance.LeaveID in (Select PayrollTypeID from HRMS_PayrollType 
 where Description like 'Absent%Days' or Description like 'Deduct%Days')
  And  HRMS_EmployeeAttendance.PersonID =HRMS_Emp.PersonID
and HRMS_EmployeeAttendance.CompanyID = HRMS_Emp.CompanyID
 And MonthYear between @MillStartDate And @CurrentDate and Status=1
	Group By  HRMS_EmployeeAttendance.PersonID 
  ) ) +
(Select isnull(Sum(Quantity),0) from HRMS_EmployeeAttendance
 Where HRMS_EmployeeAttendance.LeaveID in (Select PayrollTypeID from HRMS_PayrollType
  where Description like 'Arrears%Days') AND  HRMS_EmployeeAttendance.PersonID =HRMS_Emp.PersonID
and HRMS_EmployeeAttendance.CompanyID = HRMS_Emp.CompanyID
 And MonthYear between @MillStartDate And @CurrentDate and Status=1
	Group By  HRMS_EmployeeAttendance.PersonID 

  ) as INt) ,(DATEDIFF(DAY,  (case when HRMS_Emp.JoiningDate > @MillStartDate then HRMS_Emp.JoiningDate else  @MillStartDate end)  ,@CurrentDate))) + 1 as Days,
   HRMS_Emp.PersonID ,HRMS_Emp.CompanyID,
  case when HRMS_EmployeePayroll.ManualAmount > 0 then HRMS_EmployeePayroll.ManualAmount else  HRMS_EmployeePayroll.CalculatedAmount end as BasicSalary,

  --(HRMS_EmployeePayroll.CalculatedAmount / DATEDIFF(DAY,  @MillStartDate,@CurrentDate)) * Isnull(((DATEDIFF(DAY,  @MillStartDate,@CurrentDate) - 
--(Select isnull(Sum(Quantity),0) from HRMS_EmployeeAttendance
 --Where HRMS_EmployeeAttendance.LeaveID in (Select PayrollTypeID from HRMS_PayrollType where Description like 'Absent%Days' OR Description like 'Deduct%Days') And  HRMS_EmployeeAttendance.PersonID =HRMS_Emp.PersonID
 --And MonthYear between @MillStartDate And @CurrentDate and Status=1
	--Group By  HRMS_EmployeeAttendance.PersonID 
  --) ) +
--(Select isnull(Sum(Quantity),0) from HRMS_EmployeeAttendance
 --Where HRMS_EmployeeAttendance.LeaveID in (Select PayrollTypeID from HRMS_PayrollType where Description like 'Arrears%Days') 
 --And  HRMS_EmployeeAttendance.PersonID =HRMS_Emp.PersonID
 --And MonthYear between @MillStartDate And @CurrentDate and Status=1
--	Group By  HRMS_EmployeeAttendance.PersonID 
  --)), DATEDIFF(DAY,  @MillStartDate,@CurrentDate)) as Bonus,
0.00 as Bonus,
  HRMS_Emp.FullName as EmployeeName, HRMS_Emp.EmployeeCode, HRMS_Emp.JoiningDate,
  'True' as Eligibility,
  HRMS_Emp.basicSalary as GrossSalary
  ,HRMS_Emp.PersonID as EmployeeID
  ,HRMS_Emp.FK_JobDesignationID as DesignationID, SET_JobDesignation.JobDesignationName As DesignationName
  ,0 as BonusAbsent, 'Add' as Action
  , HRMS_Emp.StatusID ,
  HRMS_Employement_Status.Description as EmployeeStatusName
  ,0 as IncomeTax,
   -- (HRMS_EmployeePayroll.CalculatedAmount / DATEDIFF(DAY,  @MillStartDate,@CurrentDate)) * Isnull(((DATEDIFF(DAY,  @MillStartDate,@CurrentDate) - 
--(Select isnull(Sum(Quantity),0) from HRMS_EmployeeAttendance
 --Where HRMS_EmployeeAttendance.LeaveID in (Select PayrollTypeID from HRMS_PayrollType where Description like 'Absent%Days' OR Description like 'Deduct%Days') And  HRMS_EmployeeAttendance.PersonID =HRMS_Emp.PersonID
 --And MonthYear between @MillStartDate And @CurrentDate and Status=1
	--Group By  HRMS_EmployeeAttendance.PersonID 
  --) ) +
--(Select isnull(Sum(Quantity),0) from HRMS_EmployeeAttendance
 --Where HRMS_EmployeeAttendance.LeaveID in (Select PayrollTypeID from HRMS_PayrollType where Description like 'Arrears%Days') 
 --And  HRMS_EmployeeAttendance.PersonID =HRMS_Emp.PersonID
 --And MonthYear between @MillStartDate And @CurrentDate and Status=1
--	Group By  HRMS_EmployeeAttendance.PersonID 
 -- )), DATEDIFF(DAY,  @MillStartDate,@CurrentDate)) as NetBonusAmount
0.00 as NetBonusAmount
,0 as ArrearOfBonus
,0 as DeductionOfBonus
,0 as AdvanceSalary
,'false' as IsChecked
,HRMS_Emp.FK_DepartmentID as DepartmentID
   from [HRMS_Set_Employee] HRMS_Emp
   
   left Join HRMS_EmployeeAttendance  On HRMS_Emp.PersonID= HRMS_EmployeeAttendance.PersonID
 and HRMS_Emp.CompanyID = HRMS_EmployeeAttendance.CompanyID
   And HRMS_Emp.Status=1 
			left Join HRMS_EmployeePayroll on HRMS_EmployeePayroll.PersonID= HRMS_Emp.PersonID
and HRMS_EmployeePayroll.CompanyID = HRMS_Emp.CompanyID
			And (HRMS_EmployeePayroll.PayrollTypeID=2 
             --or HRMS_EmployeePayroll.PayrollTypeID=14
             )
            And HRMS_EmployeePayroll.Status=1
			left join SET_JobDesignation on SET_JobDesignation.JobDesignationID  =  HRMS_Emp.FK_JobDesignationID
            And SET_JobDesignation.Status=1
			Left Join HRMS_Employement_Status on HRMS_Employement_Status.StatusID =HRMS_Emp.StatusID
			And HRMS_Employement_Status.Status=1
			Inner Join SET_Department on SET_Department.DepartmentID=HRMS_Emp.FK_DepartmentID
			And SET_Department.Status=1
    Where HRMS_Emp.Status=1 
and HRMS_Emp.IsActive = 1
and HRMS_Emp.jobendingdate is null
		And (HRMS_Emp.StatusID = @StatusID or @StatusID is null) 
And HRMS_Emp.IsBonusAllow = 1
		)FinalFintab
		Where CompanyID = @CompanyID and FinalFintab.Days >= @PresentDays
order by EmployeeCode
 ";
            #endregion




            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (EmpStatusID.HasValue)
                    sqlparams[1] = new SqlParameter("@StatusID", EmpStatusID.Value);
                else
                    sqlparams[1] = new SqlParameter("@StatusID", DBNull.Value);


                sqlparams[2] = new SqlParameter("@MillStartDate", MillStartDate);
                sqlparams[3] = new SqlParameter("@CurrentDate", CurrentDate);

                sqlparams[4] = new SqlParameter("@PresentDays", PresentDays);

                sqlparams[5] = new SqlParameter("@totaldays", TotalDays);

                sqlparams[6] = new SqlParameter("@NoOfBonus", NoOfBonus);

              DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_BonusEmployee, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_BonusEmployee, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_BonusEmployee, sqlparams);

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

        public static DataTable GetAllActiveDataEmployeStatusWiseForHabib(string formula, DateTime CurrentDate, Nullable<int> companyID, Nullable<int> EmpStatusID, Nullable<int> TypeID, Nullable<int> JobCategoryID, int personid, string NoofBonus, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
      
            DataTable dt = null;
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            StringBuilder sb3 = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (EmpStatusID.HasValue)
                    sqlparams[1] = new SqlParameter("@StatusID", EmpStatusID.Value);
                else
                    sqlparams[1] = new SqlParameter("@StatusID", DBNull.Value);

                //if (TypeID.HasValue)
                //    sqlparams[2] = new SqlParameter("@PersonTypeID", TypeID.Value);
                //else
                //    sqlparams[2] = new SqlParameter("@PersonTypeID", DBNull.Value);

                //if (JobCategoryID.HasValue)
                //    sqlparams[3] = new SqlParameter("@JobCategoryID", JobCategoryID.Value);
                //else
                //    sqlparams[3] = new SqlParameter("@JobCategoryID", DBNull.Value);

                sqlparams[3] = new SqlParameter("@Formula", formula);
                sqlparams[4] = new SqlParameter("@CurrentDate", CurrentDate);
                sqlparams[5] = new SqlParameter("@NoofBonus", NoofBonus);

               // sqlparams[2] = new SqlParameter("@MillStartDate", MillStartDate);              
                //sqlparams[4] = new SqlParameter("@PresentDays", PresentDays);
                //sqlparams[5] = new SqlParameter("@totaldays", TotalDays);

                if (TypeID != -1 && TypeID > 0)
                {
                    sb1.Append(" and SET_EmployeeType.TypeID =  " + TypeID + "  ");
                }
                if (JobCategoryID != -1 && JobCategoryID > 0)
                {
                    sb2.Append(" and SET_JobCategory.JobCategoryID =  " + JobCategoryID + "  ");
                }
                if (personid != -1 && personid > 0)
                {
                    sb3.Append(" and HRMS_Emp.PersonID = " + personid + " ");
                }

                DataSet ds = null;

                if (objTransaction != null)
                    //ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_BonusEmployee, sqlparams);
                    ds = SqlHelper.ExecuteDataset(objTransaction,CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_BonusEmployee, sb1 ,sb2 ,sb3), sqlparams);           
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_BonusEmployee , sb1 , sb2, sb3), sqlparams);

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

        public static DataTable GetAllActiveDataEmployeStatusWiseForSanghar(string formula, DateTime CurrentDate, Nullable<int> companyID, Nullable<int> EmpStatusID, Nullable<int> TypeID, Nullable<int> JobCategoryID, int personid, string NoofBonus, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {

            DataTable dt = null;
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            StringBuilder sb3 = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (EmpStatusID != -1 && EmpStatusID > 0)
                    sqlparams[1] = new SqlParameter("@StatusID", EmpStatusID.Value);
                else
                    sqlparams[1] = new SqlParameter("@StatusID", DBNull.Value);

                //if (TypeID.HasValue)
                //    sqlparams[2] = new SqlParameter("@PersonTypeID", TypeID.Value);
                //else
                //    sqlparams[2] = new SqlParameter("@PersonTypeID", DBNull.Value);

                //if (JobCategoryID.HasValue)
                //    sqlparams[3] = new SqlParameter("@JobCategoryID", JobCategoryID.Value);
                //else
                //    sqlparams[3] = new SqlParameter("@JobCategoryID", DBNull.Value);

                sqlparams[3] = new SqlParameter("@Formula", formula);
                sqlparams[4] = new SqlParameter("@CurrentDate", CurrentDate);
                sqlparams[5] = new SqlParameter("@NoofBonus", NoofBonus);

                // sqlparams[2] = new SqlParameter("@MillStartDate", MillStartDate);              
                //sqlparams[4] = new SqlParameter("@PresentDays", PresentDays);
                //sqlparams[5] = new SqlParameter("@totaldays", TotalDays);

                if (TypeID != -1 && TypeID > 0)
                {
                    sb1.Append(" and SET_EmployeeType.TypeID =  " + TypeID + "  ");
                }
                if (JobCategoryID != -1 && JobCategoryID > 0)
                {
                    sb2.Append(" and SET_JobCategory.JobCategoryID =  " + JobCategoryID + "  ");
                }
                if (personid != -1 && personid > 0)
                {
                    sb3.Append(" and HRMS_Emp.PersonID = " + personid + " ");
                }

                DataSet ds = null;

                if (objTransaction != null)
                    //ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_BonusEmployee, sqlparams);
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_BonusEmployeeSanghar, sb1, sb2, sb3), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_BonusEmployeeSanghar, sb1, sb2, sb3), sqlparams);

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

        public static int GetNewApplicantNo(int companyID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_GetNewApplicantNumber, sqlparams));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_GetNewApplicantNumber, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetNewApplicantNumber, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static int GetNewEmployeeCode(int companyID,string prefix , SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int EmployeeCode = 0;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                string filter = "";
                if (prefix != "")
                    filter = " AND  EmployeeCode like '"+prefix+"%'";

                if (objTransaction != null)
                    EmployeeCode = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_GetNewEmployeeCode + filter, sqlparams));
                else if (objConnection != null)
                    EmployeeCode = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_GetNewEmployeeCode + filter, sqlparams));
                else
                    EmployeeCode = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetNewEmployeeCode + filter, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return EmployeeCode;
        }

        public static int GetNewEmployeeCodeForConnect(int companyID, string prefix, Nullable<DateTime> month, Nullable<DateTime>  year, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int EmployeeCode = 0;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@Month", String.Format("{0:MM}", month));
                sqlparams[2] = new SqlParameter("@Year", year.Value.Year.ToString());
                string filter = "";
                if (prefix != "")
                    filter = " AND  EmployeeCode like '" + prefix + "%'";

                if (objTransaction != null)
                    EmployeeCode = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_GetNewEmployeeCodeForConnect + filter, sqlparams));
                else if (objConnection != null)
                    EmployeeCode = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_GetNewEmployeeCodeForConnect + filter, sqlparams));
                else
                    EmployeeCode = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetNewEmployeeCodeForConnect + filter, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return EmployeeCode;
        }
        public static DataRow GetJobDesignationByKey(int companyID, int personID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;
            const string Query_GetJobDesignation_byKey = @"Select FK_JobDesignationID,SET_Designation.DesignationName
 from HRMS_Set_Employee
		Inner Join SET_Designation on SET_Designation.DesignationID=HRMS_Set_Employee.FK_JobDesignationID
		And SET_Designation.Status=1
Where HRMS_Set_Employee.CompanyID=@CompanyID
	And HRMS_Set_Employee.PersonID=@PersonID";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PersonID", personID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetJobDesignation_byKey, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetJobDesignation_byKey, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetJobDesignation_byKey, sqlparams);

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


        public static DataTable GetManualAttendanceByPersonID(int companyID, int personID, DateTime fromdate, DateTime todate, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dr = null;
            const string Query_GetByKey = @"select Distinct HRMS_EmployeeAttendance.Quantity as Absentees,HRMS_PayrollType.Description from HRMS_Set_Employee
inner join HRMS_EmployeeAttendance on HRMS_EmployeeAttendance.PersonID=HRMS_Set_Employee.PersonID
and HRMS_EmployeeAttendance.CompanyID=HRMS_Set_Employee.CompanyID
inner Join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID=HRMS_EmployeeAttendance.LeaveID
where HRMS_EmployeeAttendance.Status=1
and HRMS_EmployeeAttendance.PersonID=@PersonID
and HRMS_Set_Employee.CompanyID=@CompanyID
And HRMS_EmployeeAttendance.MonthYear between @fromdate and  @todate
--And  HRMS_PayrollType.Description like '%absent%'
";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PersonID", personID);
                sqlparams[2] = new SqlParameter("@fromdate", fromdate);
                sqlparams[3] = new SqlParameter("@todate", todate);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetByKey, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetByKey, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetByKey, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dr = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }

        public static DataTable GetManualAttendanceByPersonIDBonusForSanghar(int companyID, int personID, DateTime fromdate, DateTime todate, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dr = null;
            StringBuilder sb = new StringBuilder();

            const string Query_GetByKey = @"
select SUM(HRMS_EmployeeAttendance.Quantity) as Absentees,HRMS_PayrollType.Description,HRMS_Set_Employee.FullName 
FROM HRMS_Set_Employee
inner join HRMS_EmployeeAttendance on HRMS_EmployeeAttendance.PersonID=HRMS_Set_Employee.PersonID
and HRMS_EmployeeAttendance.CompanyID=HRMS_Set_Employee.CompanyID
inner Join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID=HRMS_EmployeeAttendance.LeaveID
where HRMS_EmployeeAttendance.Status=1
{0}
and HRMS_Set_Employee.CompanyID=@CompanyID
And HRMS_EmployeeAttendance.MonthYear between @fromdate and  @todate
And  HRMS_PayrollType.Description = 'Absent Days'
GROUP BY HRMS_PayrollType.Description,HRMS_Set_Employee.FullName
";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PersonID", personID);
                sqlparams[2] = new SqlParameter("@fromdate", fromdate);
                sqlparams[3] = new SqlParameter("@todate", todate);

                DataSet ds = null;

                if (personID > 0)
                {
                    sb.Append("and HRMS_EmployeeAttendance.PersonID=@PersonID");
                }

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_GetByKey, sb.ToString()), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetByKey, sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetByKey, sb.ToString()), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dr = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }


        public static DataTable GetPayrollAbsentLeaves()
        {
            DataTable dt = null;
            const string Query_GetByKey = @"Select HRMS_PayrollType.PayrollTypeID,[HRMS_EmployeePayroll].EmployeePayrollID, Code, Description,365 as LeaveDays from HRMS_PayrollType
inner join [HRMS_EmployeePayroll] on [HRMS_EmployeePayroll].PayrollTypeID =HRMS_PayrollType.PayrollTypeID
 where HRMS_PayrollType.PayrollTypeCode=4 and [HRMS_EmployeePayroll].EmployeePayrollID=4 ";
            try
            {
               
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetByKey).Tables[0];

               
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        
        #endregion

        public bool Insert(AR.Setup.AR_SET_Customer objCustomer, AR.Setup.AR_SET_BusinessPartnerType obBP)
        {
            bool result = false;
            SqlTransaction objTransaction = SqlHelper.GetTransaction("InsertEmployeeBussinessPartner");

            try
            {

                
                objCustomer.CustomerID = AR.Setup.AR_SET_Customer.GetNewCustomerID(objTransaction,null);
                this.FK_CustomerID = objCustomer.CustomerID;
                this.Insert(objTransaction);
                objCustomer.Insert(objTransaction);
                obBP.BusinessPartnerID = objCustomer.CustomerID;
                obBP.Insert(objTransaction);
                result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            finally
            {
                if (result)
                    objTransaction.Commit();
                else
                    objTransaction.Rollback("InsertEmployeeBussinessPartner");
            }

            return result;
        }

        public bool InsertSETEmployeePF(string DatabaseName, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                StringBuilder sb = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[207];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@PersonID", f_PersonID);
                sqlparams[2] = new SqlParameter("@PersonTypeCode", f_PersonTypeCode);

                if (!string.IsNullOrEmpty(f_Suffix))
                    sqlparams[3] = new SqlParameter("@Suffix", f_Suffix);
                else
                    sqlparams[3] = new SqlParameter("@Suffix", DBNull.Value);

                if (!string.IsNullOrEmpty(f_Title))
                    sqlparams[4] = new SqlParameter("@Title", f_Title);
                else
                    sqlparams[4] = new SqlParameter("@Title", DBNull.Value);

                if (!string.IsNullOrEmpty(f_FirstName))
                    sqlparams[5] = new SqlParameter("@FirstName", f_FirstName);
                else
                    sqlparams[5] = new SqlParameter("@FirstName", DBNull.Value);

                if (!string.IsNullOrEmpty(f_MiddleName))
                    sqlparams[6] = new SqlParameter("@MiddleName", f_MiddleName);
                else
                    sqlparams[6] = new SqlParameter("@MiddleName", DBNull.Value);

                if (!string.IsNullOrEmpty(f_LastName))
                    sqlparams[7] = new SqlParameter("@LastName", f_LastName);
                else
                    sqlparams[7] = new SqlParameter("@LastName", DBNull.Value);

                if (!string.IsNullOrEmpty(f_FullName))
                    sqlparams[8] = new SqlParameter("@FullName", f_FullName);
                else
                    sqlparams[8] = new SqlParameter("@FullName", DBNull.Value);

                if (!string.IsNullOrEmpty(f_KnownAs))
                    sqlparams[9] = new SqlParameter("@KnownAs", f_KnownAs);
                else
                    sqlparams[9] = new SqlParameter("@KnownAs", DBNull.Value);

                if (!string.IsNullOrEmpty(f_FatherName))
                    sqlparams[10] = new SqlParameter("@FatherName", f_FatherName);
                else
                    sqlparams[10] = new SqlParameter("@FatherName", DBNull.Value);

                sqlparams[11] = new SqlParameter("@Gender", f_Gender);

                if (f_DateOfBirth.HasValue)
                    sqlparams[12] = new SqlParameter("@DateOfBirth", f_DateOfBirth.Value);
                else
                    sqlparams[12] = new SqlParameter("@DateOfBirth", DBNull.Value);

                if (f_DateOfDeath.HasValue)
                    sqlparams[13] = new SqlParameter("@DateOfDeath", f_DateOfDeath.Value);
                else
                    sqlparams[13] = new SqlParameter("@DateOfDeath", DBNull.Value);

                sqlparams[14] = new SqlParameter("@FK_MaritalStatusID", f_FK_MaritalStatusID);

                if (!string.IsNullOrEmpty(f_AddressLine1))
                    sqlparams[15] = new SqlParameter("@AddressLine1", f_AddressLine1);
                else
                    sqlparams[15] = new SqlParameter("@AddressLine1", DBNull.Value);

                if (!string.IsNullOrEmpty(f_AddressLine2))
                    sqlparams[16] = new SqlParameter("@AddressLine2", f_AddressLine2);
                else
                    sqlparams[16] = new SqlParameter("@AddressLine2", DBNull.Value);

                if (!string.IsNullOrEmpty(f_AddressLine3))
                    sqlparams[17] = new SqlParameter("@AddressLine3", f_AddressLine3);
                else
                    sqlparams[17] = new SqlParameter("@AddressLine3", DBNull.Value);

                if (!string.IsNullOrEmpty(f_MobileNumber))
                    sqlparams[18] = new SqlParameter("@MobileNumber", f_MobileNumber);
                else
                    sqlparams[18] = new SqlParameter("@MobileNumber", DBNull.Value);

                if (!string.IsNullOrEmpty(f_PhoneNumber1))
                    sqlparams[19] = new SqlParameter("@PhoneNumber1", f_PhoneNumber1);
                else
                    sqlparams[19] = new SqlParameter("@PhoneNumber1", DBNull.Value);

                if (!string.IsNullOrEmpty(f_PhoneNumber2))
                    sqlparams[20] = new SqlParameter("@PhoneNumber2", f_PhoneNumber2);
                else
                    sqlparams[20] = new SqlParameter("@PhoneNumber2", DBNull.Value);

                if (!string.IsNullOrEmpty(f_EmergencyContactNumber))
                    sqlparams[21] = new SqlParameter("@EmergencyContactNumber", f_EmergencyContactNumber);
                else
                    sqlparams[21] = new SqlParameter("@EmergencyContactNumber", DBNull.Value);

                if (!string.IsNullOrEmpty(f_OfficeNumber))
                    sqlparams[22] = new SqlParameter("@OfficeNumber", f_OfficeNumber);
                else
                    sqlparams[22] = new SqlParameter("@OfficeNumber", DBNull.Value);

                if (!string.IsNullOrEmpty(f_Email))
                    sqlparams[23] = new SqlParameter("@Email", f_Email);
                else
                    sqlparams[23] = new SqlParameter("@Email", DBNull.Value);

                if (!string.IsNullOrEmpty(f_Nationality))
                    sqlparams[24] = new SqlParameter("@Nationality", f_Nationality);
                else
                    sqlparams[24] = new SqlParameter("@Nationality", DBNull.Value);

                if (f_FK_CountryID.HasValue)
                    sqlparams[25] = new SqlParameter("@FK_CountryID", f_FK_CountryID.Value);
                else
                    sqlparams[25] = new SqlParameter("@FK_CountryID", DBNull.Value);

                if (f_FK_CityID.HasValue)
                    sqlparams[26] = new SqlParameter("@FK_CityID", f_FK_CityID.Value);
                else
                    sqlparams[26] = new SqlParameter("@FK_CityID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_PassportNo))
                    sqlparams[27] = new SqlParameter("@PassportNo", f_PassportNo);
                else
                    sqlparams[27] = new SqlParameter("@PassportNo", DBNull.Value);

                if (!string.IsNullOrEmpty(f_BloodGroup))
                    sqlparams[28] = new SqlParameter("@BloodGroup", f_BloodGroup);
                else
                    sqlparams[28] = new SqlParameter("@BloodGroup", DBNull.Value);

                if (!string.IsNullOrEmpty(f_NICNumber))
                    sqlparams[29] = new SqlParameter("@NICNumber", f_NICNumber);
                else
                    sqlparams[29] = new SqlParameter("@NICNumber", DBNull.Value);

                if (!string.IsNullOrEmpty(f_CNICNumber))
                    sqlparams[30] = new SqlParameter("@CNICNumber", f_CNICNumber);
                else
                    sqlparams[30] = new SqlParameter("@CNICNumber", DBNull.Value);

                if (!string.IsNullOrEmpty(f_NTNNo))
                    sqlparams[31] = new SqlParameter("@NTNNo", f_NTNNo);
                else
                    sqlparams[31] = new SqlParameter("@NTNNo", DBNull.Value);

                if (!string.IsNullOrEmpty(f_SocialSecNo))
                    sqlparams[32] = new SqlParameter("@SocialSecNo", f_SocialSecNo);
                else
                    sqlparams[32] = new SqlParameter("@SocialSecNo", DBNull.Value);

                if (!string.IsNullOrEmpty(f_Domicile))
                    sqlparams[33] = new SqlParameter("@Domicile", f_Domicile);
                else
                    sqlparams[33] = new SqlParameter("@Domicile", DBNull.Value);

                if (!string.IsNullOrEmpty(f_EOBINo))
                    sqlparams[34] = new SqlParameter("@EOBINo", f_EOBINo);
                else
                    sqlparams[34] = new SqlParameter("@EOBINo", DBNull.Value);

                if (!string.IsNullOrEmpty(f_MotherLanguage))
                    sqlparams[35] = new SqlParameter("@MotherLanguage", f_MotherLanguage);
                else
                    sqlparams[35] = new SqlParameter("@MotherLanguage", DBNull.Value);

                if (!string.IsNullOrEmpty(f_LastMedicalTestBy))
                    sqlparams[36] = new SqlParameter("@LastMedicalTestBy", f_LastMedicalTestBy);
                else
                    sqlparams[36] = new SqlParameter("@LastMedicalTestBy", DBNull.Value);

                if (f_JobStatusCode.HasValue)
                    sqlparams[37] = new SqlParameter("@JobStatusCode", f_JobStatusCode.Value);
                else
                    sqlparams[37] = new SqlParameter("@JobStatusCode", DBNull.Value);

                if (!string.IsNullOrEmpty(f_EmployeeCode))
                    sqlparams[38] = new SqlParameter("@EmployeeCode", f_EmployeeCode);
                else
                    sqlparams[38] = new SqlParameter("@EmployeeCode", DBNull.Value);

                if (!string.IsNullOrEmpty(f_EmpAccountNo))
                    sqlparams[39] = new SqlParameter("@EmpAccountNo", f_EmpAccountNo);
                else
                    sqlparams[39] = new SqlParameter("@EmpAccountNo", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ApplicantNumber))
                    sqlparams[40] = new SqlParameter("@ApplicantNumber", f_ApplicantNumber);
                else
                    sqlparams[40] = new SqlParameter("@ApplicantNumber", DBNull.Value);

                if (!string.IsNullOrEmpty(f_RecommendedBy1))
                    sqlparams[41] = new SqlParameter("@RecommendedBy1", f_RecommendedBy1);
                else
                    sqlparams[41] = new SqlParameter("@RecommendedBy1", DBNull.Value);

                if (!string.IsNullOrEmpty(f_RecommendedBy2))
                    sqlparams[42] = new SqlParameter("@RecommendedBy2", f_RecommendedBy2);
                else
                    sqlparams[42] = new SqlParameter("@RecommendedBy2", DBNull.Value);

                if (!string.IsNullOrEmpty(f_RecommendedBy3))
                    sqlparams[43] = new SqlParameter("@RecommendedBy3", f_RecommendedBy3);
                else
                    sqlparams[43] = new SqlParameter("@RecommendedBy3", DBNull.Value);

                if (!string.IsNullOrEmpty(f_RehireReason))
                    sqlparams[44] = new SqlParameter("@RehireReason", f_RehireReason);
                else
                    sqlparams[44] = new SqlParameter("@RehireReason", DBNull.Value);

                if (!string.IsNullOrEmpty(f_RehireRecommendation))
                    sqlparams[45] = new SqlParameter("@RehireRecommendation", f_RehireRecommendation);
                else
                    sqlparams[45] = new SqlParameter("@RehireRecommendation", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ExtraComments))
                    sqlparams[46] = new SqlParameter("@ExtraComments", f_ExtraComments);
                else
                    sqlparams[46] = new SqlParameter("@ExtraComments", DBNull.Value);

                if (f_JoiningDate.HasValue)
                    sqlparams[47] = new SqlParameter("@JoiningDate", f_JoiningDate.Value);
                else
                    sqlparams[47] = new SqlParameter("@JoiningDate", DBNull.Value);

                if (f_AppointedOn.HasValue)
                    sqlparams[48] = new SqlParameter("@AppointedOn", f_AppointedOn.Value);
                else
                    sqlparams[48] = new SqlParameter("@AppointedOn", DBNull.Value);

                if (f_BackgroudCheckDate.HasValue)
                    sqlparams[49] = new SqlParameter("@BackgroudCheckDate", f_BackgroudCheckDate.Value);
                else
                    sqlparams[49] = new SqlParameter("@BackgroudCheckDate", DBNull.Value);

                if (f_DateEmployeeDataVerified.HasValue)
                    sqlparams[50] = new SqlParameter("@DateEmployeeDataVerified", f_DateEmployeeDataVerified.Value);
                else
                    sqlparams[50] = new SqlParameter("@DateEmployeeDataVerified", DBNull.Value);

                if (f_EffectiveEndDate.HasValue)
                    sqlparams[51] = new SqlParameter("@EffectiveEndDate", f_EffectiveEndDate.Value);
                else
                    sqlparams[51] = new SqlParameter("@EffectiveEndDate", DBNull.Value);

                if (f_EffectiveStartDate.HasValue)
                    sqlparams[52] = new SqlParameter("@EffectiveStartDate", f_EffectiveStartDate.Value);
                else
                    sqlparams[52] = new SqlParameter("@EffectiveStartDate", DBNull.Value);

                if (f_EOBIJoinDate.HasValue)
                    sqlparams[53] = new SqlParameter("@EOBIJoinDate", f_EOBIJoinDate.Value);
                else
                    sqlparams[53] = new SqlParameter("@EOBIJoinDate", DBNull.Value);

                if (f_OrignalDateOfHire.HasValue)
                    sqlparams[54] = new SqlParameter("@OrignalDateOfHire", f_OrignalDateOfHire.Value);
                else
                    sqlparams[54] = new SqlParameter("@OrignalDateOfHire", DBNull.Value);

                if (f_ReceiptOfDeathCertDate.HasValue)
                    sqlparams[55] = new SqlParameter("@ReceiptOfDeathCertDate", f_ReceiptOfDeathCertDate.Value);
                else
                    sqlparams[55] = new SqlParameter("@ReceiptOfDeathCertDate", DBNull.Value);

                if (f_SocSecJoinDate.HasValue)
                    sqlparams[56] = new SqlParameter("@SocSecJoinDate", f_SocSecJoinDate.Value);
                else
                    sqlparams[56] = new SqlParameter("@SocSecJoinDate", DBNull.Value);

                if (f_HoldApplicantDateUntil.HasValue)
                    sqlparams[57] = new SqlParameter("@HoldApplicantDateUntil", f_HoldApplicantDateUntil.Value);
                else
                    sqlparams[57] = new SqlParameter("@HoldApplicantDateUntil", DBNull.Value);

                if (f_JobEndingDate.HasValue)
                    sqlparams[58] = new SqlParameter("@JobEndingDate", f_JobEndingDate.Value);
                else
                    sqlparams[58] = new SqlParameter("@JobEndingDate", DBNull.Value);

                if (f_LastMedicalTestDate.HasValue)
                    sqlparams[59] = new SqlParameter("@LastMedicalTestDate", f_LastMedicalTestDate.Value);
                else
                    sqlparams[59] = new SqlParameter("@LastMedicalTestDate", DBNull.Value);

                if (f_ResumeLastUpdate.HasValue)
                    sqlparams[60] = new SqlParameter("@ResumeLastUpdate", f_ResumeLastUpdate.Value);
                else
                    sqlparams[60] = new SqlParameter("@ResumeLastUpdate", DBNull.Value);

                sqlparams[61] = new SqlParameter("@IsPersonDisabled", f_IsPersonDisabled);

                if (f_IsBackgroundChecked.HasValue)
                    sqlparams[62] = new SqlParameter("@IsBackgroundChecked", f_IsBackgroundChecked.Value);
                else
                    sqlparams[62] = new SqlParameter("@IsBackgroundChecked", DBNull.Value);

                if (f_IsCurrentApplicant.HasValue)
                    sqlparams[63] = new SqlParameter("@IsCurrentApplicant", f_IsCurrentApplicant.Value);
                else
                    sqlparams[63] = new SqlParameter("@IsCurrentApplicant", DBNull.Value);

                if (f_IsCurrentEmployee.HasValue)
                    sqlparams[64] = new SqlParameter("@IsCurrentEmployee", f_IsCurrentEmployee.Value);
                else
                    sqlparams[64] = new SqlParameter("@IsCurrentEmployee", DBNull.Value);

                if (f_IsOnJob.HasValue)
                    sqlparams[65] = new SqlParameter("@IsOnJob", f_IsOnJob.Value);
                else
                    sqlparams[65] = new SqlParameter("@IsOnJob", DBNull.Value);

                if (f_IsOnMilitaryService.HasValue)
                    sqlparams[66] = new SqlParameter("@IsOnMilitaryService", f_IsOnMilitaryService.Value);
                else
                    sqlparams[66] = new SqlParameter("@IsOnMilitaryService", DBNull.Value);

                if (f_IsPFMember.HasValue)
                    sqlparams[67] = new SqlParameter("@IsPFMember", f_IsPFMember.Value);
                else
                    sqlparams[67] = new SqlParameter("@IsPFMember", DBNull.Value);

                if (f_IsEOBIMember.HasValue)
                    sqlparams[68] = new SqlParameter("@IsEOBIMember", f_IsEOBIMember.Value);
                else
                    sqlparams[68] = new SqlParameter("@IsEOBIMember", DBNull.Value);

                if (f_IsResumeExist.HasValue)
                    sqlparams[69] = new SqlParameter("@IsResumeExist", f_IsResumeExist.Value);
                else
                    sqlparams[69] = new SqlParameter("@IsResumeExist", DBNull.Value);

                if (f_IsSecondPassportExist.HasValue)
                    sqlparams[70] = new SqlParameter("@IsSecondPassportExist", f_IsSecondPassportExist.Value);
                else
                    sqlparams[70] = new SqlParameter("@IsSecondPassportExist", DBNull.Value);

                if (f_IsStudent.HasValue)
                    sqlparams[71] = new SqlParameter("@IsStudent", f_IsStudent.Value);
                else
                    sqlparams[71] = new SqlParameter("@IsStudent", DBNull.Value);

                if (f_UseTobacco.HasValue)
                    sqlparams[72] = new SqlParameter("@UseTobacco", f_UseTobacco.Value);
                else
                    sqlparams[72] = new SqlParameter("@UseTobacco", DBNull.Value);

                if (f_BasicSalary.HasValue)
                    sqlparams[73] = new SqlParameter("@BasicSalary", f_BasicSalary.Value);
                else
                    sqlparams[73] = new SqlParameter("@BasicSalary", DBNull.Value);

                if (!string.IsNullOrEmpty(f_LastJob))
                    sqlparams[74] = new SqlParameter("@LastJob", f_LastJob);
                else
                    sqlparams[74] = new SqlParameter("@LastJob", DBNull.Value);

                if (f_FK_CommentID.HasValue)
                    sqlparams[75] = new SqlParameter("@FK_CommentID", f_FK_CommentID.Value);
                else
                    sqlparams[75] = new SqlParameter("@FK_CommentID", DBNull.Value);

                if (f_FK_BankBranchID.HasValue)
                    sqlparams[76] = new SqlParameter("@FK_BankBranchID", f_FK_BankBranchID.Value);
                else
                    sqlparams[76] = new SqlParameter("@FK_BankBranchID", DBNull.Value);

                if (f_FK_BankAccountID.HasValue)
                    sqlparams[77] = new SqlParameter("@FK_BankAccountID", f_FK_BankAccountID.Value);
                else
                    sqlparams[77] = new SqlParameter("@FK_BankAccountID", DBNull.Value);

                if (f_FK_CustomerID.HasValue)
                    sqlparams[78] = new SqlParameter("@FK_CustomerID", f_FK_CustomerID.Value);
                else
                    sqlparams[78] = new SqlParameter("@FK_CustomerID", DBNull.Value);

                if (f_FK_DepartmentID.HasValue)
                    sqlparams[79] = new SqlParameter("@FK_DepartmentID", f_FK_DepartmentID.Value);
                else
                    sqlparams[79] = new SqlParameter("@FK_DepartmentID", DBNull.Value);

                if (f_FK_GradeID.HasValue)
                    sqlparams[80] = new SqlParameter("@FK_GradeID", f_FK_GradeID.Value);
                else
                    sqlparams[80] = new SqlParameter("@FK_GradeID", DBNull.Value);

                if (f_FK_JobCategoryID.HasValue)
                    sqlparams[81] = new SqlParameter("@FK_JobCategoryID", f_FK_JobCategoryID.Value);
                else
                    sqlparams[81] = new SqlParameter("@FK_JobCategoryID", DBNull.Value);

                if (f_FK_JobDesignationID.HasValue)
                    sqlparams[82] = new SqlParameter("@FK_JobDesignationID", f_FK_JobDesignationID.Value);
                else
                    sqlparams[82] = new SqlParameter("@FK_JobDesignationID", DBNull.Value);

                if (f_FK_SectionID.HasValue)
                    sqlparams[83] = new SqlParameter("@FK_SectionID", f_FK_SectionID.Value);
                else
                    sqlparams[83] = new SqlParameter("@FK_SectionID", DBNull.Value);

                sqlparams[84] = new SqlParameter("@Picture", SqlDbType.VarBinary, -1);
                if (f_Picture != null)
                    sqlparams[84].Value = f_Picture;
                else
                    sqlparams[84].Value = DBNull.Value;

                sqlparams[85] = new SqlParameter("@Status", f_Status);
                sqlparams[86] = new SqlParameter("@CreatedBy", f_CreatedBy);
                sqlparams[87] = new SqlParameter("@CreateDate", f_CreateDate);
                sqlparams[88] = new SqlParameter("@CreatedByValue", f_CreatedByValue);
                sqlparams[89] = new SqlParameter("@IsActive", f_IsActive);

                if (f_AppraisalFrequency.HasValue)
                    sqlparams[90] = new SqlParameter("@AppraisalFrequency", f_AppraisalFrequency.Value);
                else
                    sqlparams[90] = new SqlParameter("@AppraisalFrequency", DBNull.Value);

                if (f_AppraisalDate.HasValue)
                    sqlparams[91] = new SqlParameter("@AppraisalDate", f_AppraisalDate.Value);
                else
                    sqlparams[91] = new SqlParameter("@AppraisalDate", DBNull.Value);

                if (f_AppraisalDueDate.HasValue)
                    sqlparams[92] = new SqlParameter("@AppraisalDueDate", f_AppraisalDueDate.Value);
                else
                    sqlparams[92] = new SqlParameter("@AppraisalDueDate", DBNull.Value);

                if (f_FK_LocationID.HasValue)
                    sqlparams[93] = new SqlParameter("@FK_LocationID", f_FK_LocationID.Value);
                else
                    sqlparams[93] = new SqlParameter("@FK_LocationID", DBNull.Value);

                if (f_IsSSMember.HasValue)
                    sqlparams[94] = new SqlParameter("@IsSSMember", f_IsSSMember.Value);
                else
                    sqlparams[94] = new SqlParameter("@IsSSMember", DBNull.Value);

                if (f_IsOvertime.HasValue)
                    sqlparams[95] = new SqlParameter("@IsOvertime", f_IsOvertime.Value);
                else
                    sqlparams[95] = new SqlParameter("@IsOvertime", DBNull.Value);

                if (!string.IsNullOrEmpty(f_EmergencyContactPersonName))
                    sqlparams[96] = new SqlParameter("@EmergencyContactPersonName", f_EmergencyContactPersonName);
                else
                    sqlparams[96] = new SqlParameter("@EmergencyContactPersonName", DBNull.Value);

                if (!string.IsNullOrEmpty(f_MobileNumber2))
                    sqlparams[97] = new SqlParameter("@MobileNumber2", f_MobileNumber2);
                else
                    sqlparams[97] = new SqlParameter("@MobileNumber2", DBNull.Value);

                if (f_ExtNo.HasValue)
                    sqlparams[98] = new SqlParameter("@ExtNo", f_ExtNo.Value);
                else
                    sqlparams[98] = new SqlParameter("@ExtNo", DBNull.Value);

                if (!string.IsNullOrEmpty(f_Area))
                    sqlparams[99] = new SqlParameter("@Area", f_Area);
                else
                    sqlparams[99] = new SqlParameter("@Area", DBNull.Value);

                if (f_PassportExpiryDate.HasValue)
                    sqlparams[100] = new SqlParameter("@PassportExpiryDate", f_PassportExpiryDate.Value);
                else
                    sqlparams[100] = new SqlParameter("@PassportExpiryDate", DBNull.Value);

                if (f_FamilyNo.HasValue)
                    sqlparams[101] = new SqlParameter("@FamilyNo", f_FamilyNo.Value);
                else
                    sqlparams[101] = new SqlParameter("@FamilyNo", DBNull.Value);

                if (f_RecommendedByID1.HasValue)
                    sqlparams[102] = new SqlParameter("@RecommendedByID1", f_RecommendedByID1.Value);
                else
                    sqlparams[102] = new SqlParameter("@RecommendedByID1", DBNull.Value);

                if (f_RecommendedByID2.HasValue)
                    sqlparams[103] = new SqlParameter("@RecommendedByID2", f_RecommendedByID2.Value);
                else
                    sqlparams[103] = new SqlParameter("@RecommendedByID2", DBNull.Value);

                if (f_RecommendedByID3.HasValue)
                    sqlparams[104] = new SqlParameter("@RecommendedByID3", f_RecommendedByID3.Value);
                else
                    sqlparams[104] = new SqlParameter("@RecommendedByID3", DBNull.Value);

                if (f_IsHealthInsurance.HasValue)
                    sqlparams[105] = new SqlParameter("@IsHealthInsurance", f_IsHealthInsurance.Value);
                else
                    sqlparams[105] = new SqlParameter("@IsHealthInsurance", DBNull.Value);

                if (f_HealthInsuranceNo.HasValue)
                    sqlparams[106] = new SqlParameter("@HealthInsuranceNo", f_HealthInsuranceNo.Value);
                else
                    sqlparams[106] = new SqlParameter("@HealthInsuranceNo", DBNull.Value);

                if (f_IsEligiableForAppraisal.HasValue)
                    sqlparams[107] = new SqlParameter("@IsEligiableForAppraisal", f_IsEligiableForAppraisal.Value);
                else
                    sqlparams[107] = new SqlParameter("@IsEligiableForAppraisal", DBNull.Value);

                if (f_FileNumber.HasValue)
                    sqlparams[108] = new SqlParameter("@FileNumber", f_FileNumber.Value);
                else
                    sqlparams[108] = new SqlParameter("@FileNumber", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ReferencePersonName))
                    sqlparams[109] = new SqlParameter("@ReferencePersonName", f_ReferencePersonName);
                else
                    sqlparams[109] = new SqlParameter("@ReferencePersonName", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ReferencePersonFatherName))
                    sqlparams[110] = new SqlParameter("@ReferencePersonFatherName", f_ReferencePersonFatherName);
                else
                    sqlparams[110] = new SqlParameter("@ReferencePersonFatherName", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ReferencePersonContactNo))
                    sqlparams[111] = new SqlParameter("@ReferencePersonContactNo", f_ReferencePersonContactNo);
                else
                    sqlparams[111] = new SqlParameter("@ReferencePersonContactNo", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ReferencePersonEmail))
                    sqlparams[112] = new SqlParameter("@ReferencePersonEmail", f_ReferencePersonEmail);
                else
                    sqlparams[112] = new SqlParameter("@ReferencePersonEmail", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ReferencePersonOfficeName))
                    sqlparams[113] = new SqlParameter("@ReferencePersonOfficeName", f_ReferencePersonOfficeName);
                else
                    sqlparams[113] = new SqlParameter("@ReferencePersonOfficeName", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ReferencePersonjobDesignation))
                    sqlparams[114] = new SqlParameter("@ReferencePersonjobDesignation", f_ReferencePersonjobDesignation);
                else
                    sqlparams[114] = new SqlParameter("@ReferencePersonjobDesignation", DBNull.Value);

                if (!string.IsNullOrEmpty(f_Religion))
                    sqlparams[115] = new SqlParameter("@Religion", f_Religion);
                else
                    sqlparams[115] = new SqlParameter("@Religion", DBNull.Value);

                if (!string.IsNullOrEmpty(f_LanguageSpoken))
                    sqlparams[116] = new SqlParameter("@LanguageSpoken", f_LanguageSpoken);
                else
                    sqlparams[116] = new SqlParameter("@LanguageSpoken", DBNull.Value);

                if (f_FrequencyType.HasValue)
                    sqlparams[117] = new SqlParameter("@FrequencyType", f_FrequencyType.Value);
                else
                    sqlparams[117] = new SqlParameter("@FrequencyType", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ReferencePersonName2))
                    sqlparams[118] = new SqlParameter("@ReferencePersonName2", f_ReferencePersonName2);
                else
                    sqlparams[118] = new SqlParameter("@ReferencePersonName2", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ReferencePersonFatherName2))
                    sqlparams[119] = new SqlParameter("@ReferencePersonFatherName2", f_ReferencePersonFatherName2);
                else
                    sqlparams[119] = new SqlParameter("@ReferencePersonFatherName2", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ReferencePersonContactNo2))
                    sqlparams[120] = new SqlParameter("@ReferencePersonContactNo2", f_ReferencePersonContactNo2);
                else
                    sqlparams[120] = new SqlParameter("@ReferencePersonContactNo2", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ReferencePersonEmail2))
                    sqlparams[121] = new SqlParameter("@ReferencePersonEmail2", f_ReferencePersonEmail2);
                else
                    sqlparams[121] = new SqlParameter("@ReferencePersonEmail2", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ReferencePersonOfficeName2))
                    sqlparams[122] = new SqlParameter("@ReferencePersonOfficeName2", f_ReferencePersonOfficeName2);
                else
                    sqlparams[122] = new SqlParameter("@ReferencePersonOfficeName2", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ReferencePersonjobDesignation2))
                    sqlparams[123] = new SqlParameter("@ReferencePersonjobDesignation2", f_ReferencePersonjobDesignation2);
                else
                    sqlparams[123] = new SqlParameter("@ReferencePersonjobDesignation2", DBNull.Value);

                if (f_StatusID.HasValue)
                    sqlparams[124] = new SqlParameter("@StatusID", f_StatusID.Value);
                else
                    sqlparams[124] = new SqlParameter("@StatusID", DBNull.Value);

                if (f_ProvinceID.HasValue)
                    sqlparams[125] = new SqlParameter("@ProvinceID", f_ProvinceID.Value);
                else
                    sqlparams[125] = new SqlParameter("@ProvinceID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_OldEmployeeCode))
                    sqlparams[126] = new SqlParameter("@OldEmployeeCode", f_OldEmployeeCode);
                else
                    sqlparams[126] = new SqlParameter("@OldEmployeeCode", DBNull.Value);

                if (f_IsWillingTravelInPakistan.HasValue)
                    sqlparams[127] = new SqlParameter("@IsWillingTravelInPakistan", f_IsWillingTravelInPakistan.Value);
                else
                    sqlparams[127] = new SqlParameter("@IsWillingTravelInPakistan", DBNull.Value);

                if (f_IsWillingTravelAbroad.HasValue)
                    sqlparams[128] = new SqlParameter("@IsWillingTravelAbroad", f_IsWillingTravelAbroad.Value);
                else
                    sqlparams[128] = new SqlParameter("@IsWillingTravelAbroad", DBNull.Value);

                if (f_AvalibilityinShift.HasValue)
                    sqlparams[129] = new SqlParameter("@AvalibilityinShift", f_AvalibilityinShift.Value);
                else
                    sqlparams[129] = new SqlParameter("@AvalibilityinShift", DBNull.Value);

                if (!string.IsNullOrEmpty(f_DrivingLicense))
                    sqlparams[130] = new SqlParameter("@DrivingLicense", f_DrivingLicense);
                else
                    sqlparams[130] = new SqlParameter("@DrivingLicense", DBNull.Value);

                if (f_DrivingLicenseExpiryDate.HasValue)
                    sqlparams[131] = new SqlParameter("@DrivingLicenseExpiryDate", f_DrivingLicenseExpiryDate.Value);
                else
                    sqlparams[131] = new SqlParameter("@DrivingLicenseExpiryDate", DBNull.Value);

                if (f_EmployeeHeight.HasValue)
                    sqlparams[132] = new SqlParameter("@EmployeeHeight", f_EmployeeHeight.Value);
                else
                    sqlparams[132] = new SqlParameter("@EmployeeHeight", DBNull.Value);

                if (f_EmployeeWeight.HasValue)
                    sqlparams[133] = new SqlParameter("@EmployeeWeight", f_EmployeeWeight.Value);
                else
                    sqlparams[133] = new SqlParameter("@EmployeeWeight", DBNull.Value);

                if (!string.IsNullOrEmpty(f_FamilyDoctorName))
                    sqlparams[134] = new SqlParameter("@FamilyDoctorName", f_FamilyDoctorName);
                else
                    sqlparams[134] = new SqlParameter("@FamilyDoctorName", DBNull.Value);

                if (!string.IsNullOrEmpty(f_HospitalName))
                    sqlparams[135] = new SqlParameter("@HospitalName", f_HospitalName);
                else
                    sqlparams[135] = new SqlParameter("@HospitalName", DBNull.Value);

                if (!string.IsNullOrEmpty(f_HospitalContact))
                    sqlparams[136] = new SqlParameter("@HospitalContact", f_HospitalContact);
                else
                    sqlparams[136] = new SqlParameter("@HospitalContact", DBNull.Value);

                if (!string.IsNullOrEmpty(f_HospitalAddress))
                    sqlparams[137] = new SqlParameter("@HospitalAddress", f_HospitalAddress);
                else
                    sqlparams[137] = new SqlParameter("@HospitalAddress", DBNull.Value);

                if (!string.IsNullOrEmpty(f_EmployeeReferenceDepartment1))
                    sqlparams[138] = new SqlParameter("@EmployeeReferenceDepartment1", f_EmployeeReferenceDepartment1);
                else
                    sqlparams[138] = new SqlParameter("@EmployeeReferenceDepartment1", DBNull.Value);

                if (!string.IsNullOrEmpty(f_EmployeeReferenceDepartment2))
                    sqlparams[139] = new SqlParameter("@EmployeeReferenceDepartment2", f_EmployeeReferenceDepartment2);
                else
                    sqlparams[139] = new SqlParameter("@EmployeeReferenceDepartment2", DBNull.Value);

                if (!string.IsNullOrEmpty(f_CoveredLifeInsurance))
                    sqlparams[140] = new SqlParameter("@CoveredLifeInsurance", f_CoveredLifeInsurance);
                else
                    sqlparams[140] = new SqlParameter("@CoveredLifeInsurance", DBNull.Value);

                if (f_LifePremium.HasValue)
                    sqlparams[141] = new SqlParameter("@LifePremium", f_LifePremium.Value);
                else
                    sqlparams[141] = new SqlParameter("@LifePremium", DBNull.Value);

                if (!string.IsNullOrEmpty(f_LifeCategory))
                    sqlparams[142] = new SqlParameter("@LifeCategory", f_LifeCategory);
                else
                    sqlparams[142] = new SqlParameter("@LifeCategory", DBNull.Value);

                if (f_SumAssured.HasValue)
                    sqlparams[143] = new SqlParameter("@SumAssured", f_SumAssured.Value);
                else
                    sqlparams[143] = new SqlParameter("@SumAssured", DBNull.Value);

                if (!string.IsNullOrEmpty(f_HealthInsuranceCategory))
                    sqlparams[144] = new SqlParameter("@HealthInsuranceCategory", f_HealthInsuranceCategory);
                else
                    sqlparams[144] = new SqlParameter("@HealthInsuranceCategory", DBNull.Value);

                if (f_HealthInsurancePremium.HasValue)
                    sqlparams[145] = new SqlParameter("@HealthInsurancePremium", f_HealthInsurancePremium.Value);
                else
                    sqlparams[145] = new SqlParameter("@HealthInsurancePremium", DBNull.Value);

                if (!string.IsNullOrEmpty(f_HealthCertID))
                    sqlparams[146] = new SqlParameter("@HealthCertID", f_HealthCertID);
                else
                    sqlparams[146] = new SqlParameter("@HealthCertID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_Fluency))
                    sqlparams[147] = new SqlParameter("@Fluency", f_Fluency);
                else
                    sqlparams[147] = new SqlParameter("@Fluency", DBNull.Value);

                if (!string.IsNullOrEmpty(f_LanguageSpoken2))
                    sqlparams[148] = new SqlParameter("@LanguageSpoken2", f_LanguageSpoken2);
                else
                    sqlparams[148] = new SqlParameter("@LanguageSpoken2", DBNull.Value);

                if (!string.IsNullOrEmpty(f_LanguageSpoken3))
                    sqlparams[149] = new SqlParameter("@LanguageSpoken3", f_LanguageSpoken3);
                else
                    sqlparams[149] = new SqlParameter("@LanguageSpoken3", DBNull.Value);

                if (!string.IsNullOrEmpty(f_MedicalIllness))
                    sqlparams[150] = new SqlParameter("@MedicalIllness", f_MedicalIllness);
                else
                    sqlparams[150] = new SqlParameter("@MedicalIllness", DBNull.Value);

                if (f_PermanentDate.HasValue)
                    sqlparams[151] = new SqlParameter("@PermanentDate", f_PermanentDate.Value);
                else
                    sqlparams[151] = new SqlParameter("@PermanentDate", DBNull.Value);

                if (f_DivisionID.HasValue)
                    sqlparams[152] = new SqlParameter("@DivisionID", f_DivisionID.Value);
                else
                    sqlparams[152] = new SqlParameter("@DivisionID", DBNull.Value);

                if (f_SubDivisionID.HasValue)
                    sqlparams[153] = new SqlParameter("@SubDivisionID", f_SubDivisionID.Value);
                else
                    sqlparams[153] = new SqlParameter("@SubDivisionID", DBNull.Value);

                if (f_LocationTypeID.HasValue)
                    sqlparams[154] = new SqlParameter("@LocationTypeID", f_LocationTypeID.Value);
                else
                    sqlparams[154] = new SqlParameter("@LocationTypeID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_OfficeEmail))
                    sqlparams[155] = new SqlParameter("@OfficeEmail", f_OfficeEmail);
                else
                    sqlparams[155] = new SqlParameter("@OfficeEmail", DBNull.Value);

                if (!string.IsNullOrEmpty(f_KinName1))
                    sqlparams[156] = new SqlParameter("@KinName1", f_KinName1);
                else
                    sqlparams[156] = new SqlParameter("@KinName1", DBNull.Value);

                if (f_KinRelationShipID1.HasValue)
                    sqlparams[157] = new SqlParameter("@KinRelationShipID1", f_KinRelationShipID1.Value);
                else
                    sqlparams[157] = new SqlParameter("@KinRelationShipID1", DBNull.Value);

                if (!string.IsNullOrEmpty(f_KinCNIC1))
                    sqlparams[158] = new SqlParameter("@KinCNIC1", f_KinCNIC1);
                else
                    sqlparams[158] = new SqlParameter("@KinCNIC1", DBNull.Value);

                if (!string.IsNullOrEmpty(f_KinAddress1))
                    sqlparams[159] = new SqlParameter("@KinAddress1", f_KinAddress1);
                else
                    sqlparams[159] = new SqlParameter("@KinAddress1", DBNull.Value);

                if (!string.IsNullOrEmpty(f_KinContactNo1))
                    sqlparams[160] = new SqlParameter("@KinContactNo1", f_KinContactNo1);
                else
                    sqlparams[160] = new SqlParameter("@KinContactNo1", DBNull.Value);

                if (!string.IsNullOrEmpty(f_KinName2))
                    sqlparams[161] = new SqlParameter("@KinName2", f_KinName2);
                else
                    sqlparams[161] = new SqlParameter("@KinName2", DBNull.Value);

                if (f_KinRelationShipID2.HasValue)
                    sqlparams[162] = new SqlParameter("@KinRelationShipID2", f_KinRelationShipID2.Value);
                else
                    sqlparams[162] = new SqlParameter("@KinRelationShipID2", DBNull.Value);

                if (!string.IsNullOrEmpty(f_KinCNIC2))
                    sqlparams[163] = new SqlParameter("@KinCNIC2", f_KinCNIC2);
                else
                    sqlparams[163] = new SqlParameter("@KinCNIC2", DBNull.Value);

                if (!string.IsNullOrEmpty(f_KinAddress2))
                    sqlparams[164] = new SqlParameter("@KinAddress2", f_KinAddress2);
                else
                    sqlparams[164] = new SqlParameter("@KinAddress2", DBNull.Value);

                if (!string.IsNullOrEmpty(f_KinContactNo2))
                    sqlparams[165] = new SqlParameter("@KinContactNo2", f_KinContactNo2);
                else
                    sqlparams[165] = new SqlParameter("@KinContactNo2", DBNull.Value);

                if (f_IsUnionMember.HasValue)
                    sqlparams[166] = new SqlParameter("@IsUnionMember", f_IsUnionMember.Value);
                else
                    sqlparams[166] = new SqlParameter("@IsUnionMember", DBNull.Value);

                if (f_IsRehire.HasValue)
                    sqlparams[167] = new SqlParameter("@IsRehire", f_IsRehire.Value);
                else
                    sqlparams[167] = new SqlParameter("@IsRehire", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ReplacementEmployeeID))
                    sqlparams[168] = new SqlParameter("@ReplacementEmployeeID", f_ReplacementEmployeeID);
                else
                    sqlparams[168] = new SqlParameter("@ReplacementEmployeeID", DBNull.Value);

                if (f_IsReferenceChecked.HasValue)
                    sqlparams[169] = new SqlParameter("@IsReferenceChecked", f_IsReferenceChecked.Value);
                else
                    sqlparams[169] = new SqlParameter("@IsReferenceChecked", DBNull.Value);

                if (f_CostCenterID.HasValue)
                    sqlparams[170] = new SqlParameter("@CostCenterID", f_CostCenterID.Value);
                else
                    sqlparams[170] = new SqlParameter("@CostCenterID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_PlaceOfBirth))
                    sqlparams[171] = new SqlParameter("@PlaceOfBirth", f_PlaceOfBirth);
                else
                    sqlparams[171] = new SqlParameter("@PlaceOfBirth", DBNull.Value);

                if (f_IsHajjPolice.HasValue)
                    sqlparams[172] = new SqlParameter("@IsHajjPolice", f_IsHajjPolice.Value);
                else
                    sqlparams[172] = new SqlParameter("@IsHajjPolice", DBNull.Value);

                if (f_IsAttendanceAllowance.HasValue)
                    sqlparams[173] = new SqlParameter("@IsAttendanceAllowance", f_IsAttendanceAllowance.Value);
                else
                    sqlparams[173] = new SqlParameter("@IsAttendanceAllowance", DBNull.Value);

                if (f_IsLeaveEncash.HasValue)
                    sqlparams[174] = new SqlParameter("@IsLeaveEncash", f_IsLeaveEncash.Value);
                else
                    sqlparams[174] = new SqlParameter("@IsLeaveEncash", DBNull.Value);

                if (f_IsPEE.HasValue)
                    sqlparams[175] = new SqlParameter("@IsPEE", f_IsPEE.Value);
                else
                    sqlparams[175] = new SqlParameter("@IsPEE", DBNull.Value);

                if (f_IsOffDayGazt.HasValue)
                    sqlparams[176] = new SqlParameter("@IsOffDayGazt", f_IsOffDayGazt.Value);
                else
                    sqlparams[176] = new SqlParameter("@IsOffDayGazt", DBNull.Value);

                if (f_IsTimeAllownce.HasValue)
                    sqlparams[177] = new SqlParameter("@IsTimeAllownce", f_IsTimeAllownce.Value);
                else
                    sqlparams[177] = new SqlParameter("@IsTimeAllownce", DBNull.Value);

                if (f_IsWorkingDayFoodAllownce.HasValue)
                    sqlparams[178] = new SqlParameter("@IsWorkingDayFoodAllownce", f_IsWorkingDayFoodAllownce.Value);
                else
                    sqlparams[178] = new SqlParameter("@IsWorkingDayFoodAllownce", DBNull.Value);

                if (f_IsOffDayFoodAllownce.HasValue)
                    sqlparams[179] = new SqlParameter("@IsOffDayFoodAllownce", f_IsOffDayFoodAllownce.Value);
                else
                    sqlparams[179] = new SqlParameter("@IsOffDayFoodAllownce", DBNull.Value);

                if (f_IsAttendanceWaver.HasValue)
                    sqlparams[180] = new SqlParameter("@IsAttendanceWaver", f_IsAttendanceWaver.Value);
                else
                    sqlparams[180] = new SqlParameter("@IsAttendanceWaver", DBNull.Value);

                if (f_IsProductionIncentive.HasValue)
                    sqlparams[181] = new SqlParameter("@IsProductionIncentive", f_IsProductionIncentive.Value);
                else
                    sqlparams[181] = new SqlParameter("@IsProductionIncentive", DBNull.Value);

                if (f_IsHolidayVisit.HasValue)
                    sqlparams[182] = new SqlParameter("@IsHolidayVisit", f_IsHolidayVisit.Value);
                else
                    sqlparams[182] = new SqlParameter("@IsHolidayVisit", DBNull.Value);

                if (f_IsExtraOverTime.HasValue)
                    sqlparams[183] = new SqlParameter("@IsExtraOverTime", f_IsExtraOverTime.Value);
                else
                    sqlparams[183] = new SqlParameter("@IsExtraOverTime", DBNull.Value);

                if (f_IsLateFine.HasValue)
                    sqlparams[184] = new SqlParameter("@IsLateFine", f_IsLateFine.Value);
                else
                    sqlparams[184] = new SqlParameter("@IsLateFine", DBNull.Value);

                if (f_IsStopPayment.HasValue)
                    sqlparams[185] = new SqlParameter("@IsStopPayment", f_IsStopPayment.Value);
                else
                    sqlparams[185] = new SqlParameter("@IsStopPayment", DBNull.Value);

                if (f_RedFlag.HasValue)
                    sqlparams[186] = new SqlParameter("@RedFlag", f_RedFlag.Value);
                else
                    sqlparams[186] = new SqlParameter("@RedFlag", DBNull.Value);

                if (f_JobEndingStatus.HasValue)
                    sqlparams[187] = new SqlParameter("@JobEndingStatus", f_JobEndingStatus.Value);
                else
                    sqlparams[187] = new SqlParameter("@JobEndingStatus", DBNull.Value);

                if (!string.IsNullOrEmpty(f_MotherName))
                    sqlparams[188] = new SqlParameter("@MotherName", f_MotherName);
                else
                    sqlparams[188] = new SqlParameter("@MotherName", DBNull.Value);

                if (!string.IsNullOrEmpty(f_MotherCNIC))
                    sqlparams[189] = new SqlParameter("@MotherCNIC", f_MotherCNIC);
                else
                    sqlparams[189] = new SqlParameter("@MotherCNIC", DBNull.Value);

                if (!string.IsNullOrEmpty(f_FatherCNIC))
                    sqlparams[190] = new SqlParameter("@FatherCNIC", f_FatherCNIC);
                else
                    sqlparams[190] = new SqlParameter("@FatherCNIC", DBNull.Value);

                if (f_IsEOBIRegister.HasValue)
                    sqlparams[191] = new SqlParameter("@IsEOBIRegister", f_IsEOBIRegister.Value);
                else
                    sqlparams[191] = new SqlParameter("@IsEOBIRegister", DBNull.Value);

                if (f_IsSSRegister.HasValue)
                    sqlparams[192] = new SqlParameter("@IsSSRegister", f_IsSSRegister.Value);
                else
                    sqlparams[192] = new SqlParameter("@IsSSRegister", DBNull.Value);

                if (f_ExpenseTypeID.HasValue)
                    sqlparams[193] = new SqlParameter("@ExpenseTypeID", f_ExpenseTypeID.Value);
                else
                    sqlparams[193] = new SqlParameter("@ExpenseTypeID", DBNull.Value);

                if (f_IsBonusAllow.HasValue)
                    sqlparams[194] = new SqlParameter("@IsBonusAllow", f_IsBonusAllow.Value);
                else
                    sqlparams[194] = new SqlParameter("@IsBonusAllow", DBNull.Value);

                if (f_PreviousGrossSalary.HasValue)
                    sqlparams[195] = new SqlParameter("@PreviousGrossSalary", f_PreviousGrossSalary.Value);
                else
                    sqlparams[195] = new SqlParameter("@PreviousGrossSalary", DBNull.Value);

                if (f_IsIncrementAllow.HasValue)
                    sqlparams[196] = new SqlParameter("@IsIncrementAllow", f_IsIncrementAllow.Value);
                else
                    sqlparams[196] = new SqlParameter("@IsIncrementAllow", DBNull.Value);

                if (!string.IsNullOrEmpty(f_IBanNo))
                    sqlparams[197] = new SqlParameter("@IBanNo", f_IBanNo);
                else
                    sqlparams[197] = new SqlParameter("@IBanNo", DBNull.Value);

                if (f_IsEligibleForBonus.HasValue)
                    sqlparams[198] = new SqlParameter("@IsEligibleForBonus", f_IsEligibleForBonus.Value);
                else
                    sqlparams[198] = new SqlParameter("@IsEligibleForBonus", DBNull.Value);

                if (f_IsBonus1Assigned.HasValue)
                    sqlparams[199] = new SqlParameter("@IsBonus1Assigned", f_IsBonus1Assigned.Value);
                else
                    sqlparams[199] = new SqlParameter("@IsBonus1Assigned", DBNull.Value);

                if (f_IsBonus2Assigned.HasValue)
                    sqlparams[200] = new SqlParameter("@IsBonus2Assigned", f_IsBonus2Assigned.Value);
                else
                    sqlparams[200] = new SqlParameter("@IsBonus2Assigned", DBNull.Value);

                if (f_IsBonus3Assigned.HasValue)
                    sqlparams[201] = new SqlParameter("@IsBonus3Assigned", f_IsBonus3Assigned.Value);
                else
                    sqlparams[201] = new SqlParameter("@IsBonus3Assigned", DBNull.Value);

                if (!string.IsNullOrEmpty(f_Formula1))
                    sqlparams[202] = new SqlParameter("@Formula1", f_Formula1);
                else
                    sqlparams[202] = new SqlParameter("@Formula1", DBNull.Value);

                if (!string.IsNullOrEmpty(f_Formula2))
                    sqlparams[203] = new SqlParameter("@Formula2", f_Formula2);
                else
                    sqlparams[203] = new SqlParameter("@Formula2", DBNull.Value);

                if (!string.IsNullOrEmpty(f_Formula3))
                    sqlparams[204] = new SqlParameter("@Formula3", f_Formula3);
                else
                    sqlparams[204] = new SqlParameter("@Formula3", DBNull.Value);

                if (f_IsExemptedFromGH.HasValue)
                    sqlparams[205] = new SqlParameter("@IsExemptedFromGH", f_IsExemptedFromGH.Value);
                else
                    sqlparams[205] = new SqlParameter("@IsExemptedFromGH", DBNull.Value);

                if (f_JoiningSalary.HasValue)
                    sqlparams[206] = new SqlParameter("@JoiningSalary", f_JoiningSalary.Value);
                else
                    sqlparams[206] = new SqlParameter("@JoiningSalary", DBNull.Value);

                sb.Append(DatabaseName);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, string.Format(Query_InsertSETEmployeePF, sb.ToString()), sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, string.Format(Query_InsertSETEmployeePF, sb.ToString()), sqlparams) > 0)
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

        public static DataTable GetAllActiveDataForFinishGoods(Nullable<int> companyID, Nullable<bool> isActive = true)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (isActive.HasValue)
                    sqlparams[1] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[1] = new SqlParameter("@IsActive", DBNull.Value);

                DataSet ds = null;


                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllForFinsih, sqlparams);

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

        public static DataTable GetDesignationForFinishGoods(int PersonID, Nullable<int> companyID, Nullable<bool> isActive = true)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (isActive.HasValue)
                    sqlparams[1] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[1] = new SqlParameter("@IsActive", DBNull.Value);
                sqlparams[2] = new SqlParameter("@PersonID", PersonID);

                DataSet ds = null;


                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDesignation, sqlparams);

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

        public static DataTable GetGrossSalaryByPersonID(int PersonID, Nullable<int> companyID, Nullable<bool> isActive = true)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (isActive.HasValue)
                    sqlparams[1] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[1] = new SqlParameter("@IsActive", DBNull.Value);
                sqlparams[2] = new SqlParameter("@PersonID", PersonID);

                DataSet ds = null;


                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetGrossAmt, sqlparams);

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

        public static int GetEmployeeIDByCode(int companyID, string employeeCode, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@EmployeeCode", employeeCode);


                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_GetEmployeeIDByCode, sqlparams));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_GetEmployeeIDByCode, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetEmployeeIDByCode, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static string GetEmployeeCodeByID(int companyID, int PersonID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            string ID = string.Empty;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);


                if (objTransaction != null)
                    ID = SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_GetEmployeeCodeByID, sqlparams).ToString();
                else if (objConnection != null)
                    ID = SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_GetEmployeeCodeByID, sqlparams).ToString();
                else
                    ID = SqlHelper.ExecuteScalar(CommandType.Text, Query_GetEmployeeCodeByID, sqlparams).ToString();
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }


        public static int GetEmployeeIDByCodeForAdam(int companyID, string employeeCode, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@EmployeeCode", employeeCode);


                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_GetEmployeeIDByCodeForAdam, sqlparams));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_GetEmployeeIDByCodeForAdam, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetEmployeeIDByCodeForAdam, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        //Query_EmployeeRecord
        //Query_MaritalStatus

        public static DataSet GetEmployeeRecord(int companyid,int period, Nullable<int> PersonID)
        {
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PeriodID",period);
                if (PersonID != null)
                {
                    sb.Append(" and HRMS_SET_Employee.PersonID =  " + PersonID + "  ");
                }

                ds = SqlHelper.ExecuteDataset(CommandType.Text,string.Format( Query_EmployeeRecord,sb), sqlparams);


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }

        public static DataTable GetMaritalStatusData()
        {
            DataTable dt = null;
           
            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_MaritalStatus).Tables[0];
         
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public bool UpdateDataByPersonID(int CompanyID , int PersonID,SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@PersonID", f_PersonID);
                //if (f_AppraisalFrequency.HasValue)
                //    sqlparams[0] = new SqlParameter("@AppraisalFrequency", f_AppraisalFrequency.Value);
                //else
                //    sqlparams[0] = new SqlParameter("@AppraisalFrequency", DBNull.Value);
                if (f_BasicSalary.HasValue)
                    sqlparams[2] = new SqlParameter("@BasicSalary", f_BasicSalary.Value);
                else
                    sqlparams[2] = new SqlParameter("@BasicSalary", DBNull.Value);

                if (f_AppraisalDate.HasValue)
                    sqlparams[3] = new SqlParameter("@AppraisalDate", f_AppraisalDate.Value);
                else
                    sqlparams[3] = new SqlParameter("@AppraisalDate", DBNull.Value);

                if (f_AppraisalDueDate.HasValue)
                    sqlparams[4] = new SqlParameter("@AppraisalDueDate", f_AppraisalDueDate.Value);
                else
                    sqlparams[4] = new SqlParameter("@AppraisalDueDate", DBNull.Value);
                sqlparams[5] = new SqlParameter("@PersonTypeCode", f_PersonTypeCode);

                if (f_FK_JobDesignationID.HasValue)
                    sqlparams[6] = new SqlParameter("@FK_JobDesignationID", f_FK_JobDesignationID.Value);
                else
                    sqlparams[6] = new SqlParameter("@FK_JobDesignationID", DBNull.Value);
                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_By_PersonID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_By_PersonID, sqlparams) > 0)
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

        public bool UpdatePersonTypeByPersonID(int CompanyID, int PersonID, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {

             
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);
                //if (f_AppraisalFrequency.HasValue)
                //    sqlparams[0] = new SqlParameter("@AppraisalFrequency", f_AppraisalFrequency.Value);
                //else
                //    sqlparams[0] = new SqlParameter("@AppraisalFrequency", DBNull.Value);
                
                    sqlparams[2] = new SqlParameter("@PersonTypeCode", f_PersonTypeCode);
                
                
                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdatePersonType_By_PersonID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_UpdatePersonType_By_PersonID, sqlparams) > 0)
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
     
        //Query_GetEmployeeMasterDetail
        public static DataTable GetEmployeeMasterInfo(int companyid, Nullable< int> PersonID)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                if (PersonID != null)
                {
                    sb.Append(" and HRMS_SET_Employee.PersonID =  "+PersonID+"  ");
               }
                
                dt = SqlHelper.ExecuteDataset(CommandType.Text,string.Format( Query_GetEmployeeMasterDetail,sb), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetEmployeeIdCardInformation(int companyid, Nullable<int> PersonID)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                if (PersonID != null)
                {
                    sb.Append(" and HRMS_SET_Employee.PersonID =  " + PersonID + "  ");
                }

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetEmployeeIdCardInformation, sb), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetEmployeeExperienceLetter(int companyid, Nullable<int> PersonID)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                if (PersonID != null)
                {
                    sb.Append(" and HRMS_SET_Employee.PersonID =  " + PersonID + "  ");
                }

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetEmployeeExperienceLetter, sb), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetJoiningLetter(int companyid, Nullable<int> PersonID)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                if (PersonID != null)
                {
                    sb.Append(" and HRMS_SET_Employee.PersonID =  " + PersonID + "  ");
                }

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetJoiningLetter, sb), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetEmployeesPermanentDate(int companyid)
        {
            DataTable dt = null;
           
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);

                dt = SqlHelper.ExecuteDataset(CommandType.Text,Query_GetEmployeesPermanentDate, sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public bool UpdatePersonStatus(int CompanyID, int PersonID,int StatusID,int PersonTypeCode,DateTime PermanentDate,SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {


                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);
                sqlparams[2] = new SqlParameter("@StatusID", StatusID);
                sqlparams[3] = new SqlParameter("@PersonTypeCode", PersonTypeCode);
                sqlparams[4] = new SqlParameter("@PermanentDate", PermanentDate);   

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdatePersonStatus, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_UpdatePersonStatus, sqlparams) > 0)
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

        public static DataTable GetEmployeeInfo(int companyid,int PersonID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
               
                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, query_GetEmployeeInfo, sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetEmployeesLateAndSandwichLeaves(int companyid, int PersonID, Nullable<DateTime> Date_From)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);
                sqlparams[2] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                sqlparams[3] = new SqlParameter("@Year", Date_From.Value.Year.ToString());

                dt = SqlHelper.ExecuteDataset(CommandType.Text, query_GetEmployeesLateAndSandwichLeaves, sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetEmployeesEntryForTMS(int companyid, int PersonID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);
                sqlparams[2] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                sqlparams[3] = new SqlParameter("@Year", Date_To.Value.Year.ToString());

               // dt = SqlHelper.ExecuteDataset(CommandType.Text, query_GetEmployeesEntryForTMS, sqlparams).Tables[0];

                if (sqltrans != null)
                    dt = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, query_GetEmployeesEntryForTMS, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, query_GetEmployeesEntryForTMS, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetEmployeesAllCasualLeaves(int companyid, int PersonID,int PayrollTypeCode)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);
                sqlparams[2] = new SqlParameter("@PayrollTypeCode", PayrollTypeCode);
                

                dt = SqlHelper.ExecuteDataset(CommandType.Text, query_GetEmployeesAllCasualLeaves, sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetEmployeesAllSickLeaves(int companyid, int PersonID, int PayrollTypeCode)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);
                sqlparams[2] = new SqlParameter("@PayrollTypeCode", PayrollTypeCode);


                dt = SqlHelper.ExecuteDataset(CommandType.Text, query_GetEmployeesAllSickLeaves, sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetEmployeesAllAnnualLeaves(int companyid, int PersonID, int PayrollTypeCode)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);
                sqlparams[2] = new SqlParameter("@PayrollTypeCode", PayrollTypeCode);


                dt = SqlHelper.ExecuteDataset(CommandType.Text, query_GetEmployeesAllAnnualLeaves, sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAbsentLeavePayRollID(int companyid,int PayrollTypeCode)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);                
                sqlparams[1] = new SqlParameter("@PayrollTypeCode", PayrollTypeCode);


                dt = SqlHelper.ExecuteDataset(CommandType.Text, query_GetAbsentLeavePayRollID, sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetGradeByEmployyeID(int companyid, int EmployeeID, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            #region
            const string QueryForEmployeeID = @"select Distinct SET_Grade.GradeName from HRMS_Set_Employee 
inner join SET_Grade on SET_Grade.GradeID = HRMS_Set_Employee.FK_GradeID
where HRMS_Set_Employee.PersonID=@EmployeeID
and HRMS_Set_Employee.CompanyID=@CompanyID";
            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@EmployeeID", EmployeeID);


                if (objTransaction == null)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, QueryForEmployeeID, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, QueryForEmployeeID, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetPersonByID(int companyid, int EmployeeID, string DatabaseName, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            #region
            const string Query_GetPersonByIDTarget = @"select * from {0}.dbo.HRMS_Set_Employee 
where HRMS_Set_Employee.PersonID=@EmployeeID
and HRMS_Set_Employee.CompanyID=@CompanyID";
            const string Query_GetPersonByIDSource = @"select * from HRMS_Set_Employee 
where HRMS_Set_Employee.PersonID=@EmployeeID
and HRMS_Set_Employee.CompanyID=@CompanyID";
            #endregion

            try
            {
                StringBuilder sb = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@EmployeeID", EmployeeID);

                sb.Append(DatabaseName);
                if (DatabaseName != "")
                {
                    if (objTransaction == null)
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetPersonByIDTarget, sb.ToString()), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_GetPersonByIDTarget, sb.ToString()), sqlparams).Tables[0];
                }
                else
                {
                    if (objTransaction == null)
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetPersonByIDSource, sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetPersonByIDSource, sqlparams).Tables[0];
                }
                
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAbsentEmployeeLeavePayRollID(int companyid, int PersonID, int PayrollTypeCode)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);             
                sqlparams[2] = new SqlParameter("@PayrollTypeCode", PayrollTypeCode);


                dt = SqlHelper.ExecuteDataset(CommandType.Text, query_GetAbsentEmployeeLeavePayRollID, sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetEmployeeLeaveBalance(int companyid, int PersonID, int PayrollTypeCode)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);
                sqlparams[2] = new SqlParameter("@PayrollTypeCode", PayrollTypeCode);
                
               dt = SqlHelper.ExecuteDataset(CommandType.Text, query_GetEmployeeLeaveBalance, sqlparams).Tables[0];
               
        
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetEmployeesForPermanant(int companyid,int StatusID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@StatusID", StatusID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, query_GetEmployeesForPermanant, sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllLeavesTypes(int companyid)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, query_GetAllLeavesTypes, sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable CheckEmployeeCode_Unique(int companyid, string EmployeeCode, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            #region
            const string QueryForEmployeeCode = @" Select * from [HRMS_Set_Employee] where [CompanyID] =  @CompanyID and EmployeeCode = @EmployeeCode";
            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@EmployeeCode", EmployeeCode);

              
                if (objTransaction == null)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, QueryForEmployeeCode, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(objTransaction,CommandType.Text, QueryForEmployeeCode, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static bool CheckCNIC_Unique(int companyid, string whr, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            bool duplicate = false;
            #region

            const string QueryForEmployeeCode = @" Select * from [HRMS_Set_Employee] where [CompanyID] =  @CompanyID and Status=1";
            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                //sqlparams[1] = new SqlParameter("@EmployeeCode", EmployeeCode);


                if (objTransaction == null)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, QueryForEmployeeCode + whr, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, QueryForEmployeeCode + whr, sqlparams).Tables[0];
                
                
                
                if (dt != null && dt.Rows.Count > 0)
                {
                    duplicate = true;
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return duplicate;
        }

        public static DataTable GetEmployeeInfoForPayRoll(int companyid, int PersonID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, query_GetEmployeeInfoForPayRoll, sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetEmployeesForApprovedStrength(int companyid, int departmentid,int designationid,int sectionid)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@departmentid", departmentid);
                sqlparams[2] = new SqlParameter("@designationid", designationid);
                sqlparams[3] = new SqlParameter("@sectionid", sectionid);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, query_GetEmployeesApprovedStrength, sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetEmployeesInfo(int companyid, int PersonID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, query_EmployeeInfo, sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllActiveDataHavingPayRoll(Nullable<int> companyID, Nullable<bool> isActive = true, SqlConnection objConnection = null, SqlTransaction objTransaction = null, int LocationID = 0, int DivisionID = 0, int personTypeID = 0)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
           
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (isActive.HasValue)
                    sqlparams[1] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[1] = new SqlParameter("@IsActive", DBNull.Value);

                DataSet ds = null;
                if (LocationID > 0)
                {
                    sb.Append(" And tab.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" And tab.DivisionID = " + DivisionID);
                }

                if (personTypeID > 0)
                {
                    sb.Append(" And tab.PersonTypeCode = " + personTypeID);
                }
              
                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text,string.Format(Query_SelectAllEmployeeinPayRoll,sb), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_SelectAllEmployeeinPayRoll, sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectAllEmployeeinPayRoll, sb), sqlparams);

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

        public static DataTable GetAllActiveDataHavingPayRollINN(string SalMonth, Nullable<int> companyID, Nullable<bool> isActive = true, SqlConnection objConnection = null, SqlTransaction objTransaction = null, int LocationID = 0, int DivisionID = 0, int personTypeID = 0, Nullable<int> Statuscode = null, Nullable<int> DesignationID = null, Nullable<int> DepartmentID = null, Nullable<int> SectionID = null, Nullable<DateTime> SalaryDate = null, Nullable<Guid> UserID = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            int SalMth = int.Parse(SalMonth.ToString() == "" ? "0" : SalMonth.ToString());
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (isActive.HasValue)
                    sqlparams[1] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[1] = new SqlParameter("@IsActive", DBNull.Value);
                if (SalaryDate.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@SalaryDate", SalaryDate);
                    sb.Append(" And (@SalaryDate <= tab.JobEndingDate OR  tab.JobEndingDate is null)");
                }

                DataSet ds = null;



                if (LocationID > 0)
                {
                    sb.Append(" And tab.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" And tab.DivisionID = " + DivisionID);
                }

                if (personTypeID > 0)
                {
                    sb.Append(" And tab.PersonTypeCode = " + personTypeID);
                }
                if (DesignationID != null && DesignationID > 0)
                {
                    sb.Append(" And tab.FK_JobDesignationID = " + DesignationID);
                }
                if (DepartmentID != null && DepartmentID > 0)
                {
                    sb.Append(" And tab.FK_DepartmentID = " + DepartmentID);
                }

                if (SectionID != null && SectionID > 0)
                {
                    sb.Append(" And tab.FK_SectionID = " + SectionID);
                }

                if (Statuscode != null && Statuscode != -1)
                {
                    sb.Append(" and tab.StatusID = " + Statuscode);
                }
                else
                {
                    sb.Append(" and tab.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_SelectAllEmployeeinPayRoll, sb), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_SelectAllEmployeeinPayRoll, sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectAllEmployeeinPayRoll, sb), sqlparams);

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

        public static DataTable GetAllActiveDataHavingPayRollINNDailyWages(Nullable<int> companyID, Nullable<bool> isActive = true, SqlConnection objConnection = null, SqlTransaction objTransaction = null, int LocationID = 0, int DivisionID = 0, int personTypeID = 0, Nullable<int> Statuscode = null, Nullable<int> DesignationID = null, Nullable<int> DepartmentID = null, Nullable<int> SectionID = null, Nullable<DateTime> SalaryDate = null, Nullable<Guid> UserID = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            //int SalMth = int.Parse(SalMonth.ToString() == "" ? "0" : SalMonth.ToString());
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (isActive.HasValue)
                    sqlparams[1] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[1] = new SqlParameter("@IsActive", DBNull.Value);
                if (SalaryDate.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@SalaryDate", SalaryDate);
                    sb.Append(" And (@SalaryDate <= tab.JobEndingDate OR  tab.JobEndingDate is null)");
                }

                DataSet ds = null;



                if (LocationID > 0)
                {
                    sb.Append(" And tab.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" And tab.DivisionID = " + DivisionID);
                }

                if (personTypeID > 0)
                {
                    sb.Append(" And tab.PersonTypeCode = " + personTypeID);
                }
                if (DesignationID != null && DesignationID > 0)
                {
                    sb.Append(" And tab.FK_JobDesignationID = " + DesignationID);
                }
                if (DepartmentID != null && DepartmentID > 0)
                {
                    sb.Append(" And tab.FK_DepartmentID = " + DepartmentID);
                }

                if (SectionID != null && SectionID > 0)
                {
                    sb.Append(" And tab.FK_SectionID = " + SectionID);
                }

                if (Statuscode != null && Statuscode != -1)
                {
                    sb.Append(" and tab.StatusID = " + Statuscode);
                }
                else
                {
                    sb.Append(" and tab.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }

                if (objTransaction != null)
                    //ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_SelectAllEmployeeinPayRoll, sb), sqlparams);
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_SelectAllEmployeeinPayRollDailyWages, sb.ToString()), sqlparams);
                else if (objConnection != null)
                    //ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_SelectAllEmployeeinPayRoll, sb), sqlparams);
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_SelectAllEmployeeinPayRollDailyWages, sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_SelectAllEmployeeinPayRollDailyWages, sb.ToString()), sqlparams);
                    //ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectAllEmployeeinPayRoll, sb), sqlparams);

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


        public static DataTable GetAllActiveDataHavingPayRollINN1(string SalMonth, Nullable<int> companyID, Nullable<bool> isActive = true, SqlConnection objConnection = null, SqlTransaction objTransaction = null, int LocationID = 0, int DivisionID = 0, int personTypeID = 0, Nullable<int> JobCategoryID = null, Nullable<int> Statuscode = null, Nullable<int> DesignationID = null, Nullable<int> DepartmentID = null, Nullable<int> SectionID = null, Nullable<DateTime> SalaryDate = null, Nullable<Guid> UserID = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            int SalMth = int.Parse(SalMonth.ToString() == "" ? "0" : SalMonth.ToString());
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (isActive.HasValue)
                    sqlparams[1] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[1] = new SqlParameter("@IsActive", DBNull.Value);
                if (SalaryDate.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@SalaryDate", SalaryDate);
                    sb.Append(" And (@SalaryDate <= tab.JobEndingDate OR  tab.JobEndingDate is null)");
                }

                DataSet ds = null;



                if (LocationID > 0)
                {
                    sb.Append(" And tab.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" And tab.DivisionID = " + DivisionID);
                }

                if (personTypeID > 0)
                {
                    sb.Append(" And tab.PersonTypeCode = " + personTypeID);
                }
                if (JobCategoryID != -1 && JobCategoryID > 0)
                {
                    sb.Append(" And tab.FK_JobCategoryID =  " + JobCategoryID + "  ");
                }
                if (DesignationID != null && DesignationID > 0)
                {
                    sb.Append(" And tab.FK_JobDesignationID = " + DesignationID);
                }
                if (DepartmentID != null && DepartmentID > 0)
                {
                    sb.Append(" And tab.FK_DepartmentID = " + DepartmentID);
                }

                if (SectionID != null && SectionID > 0)
                {
                    sb.Append(" And tab.FK_SectionID = " + SectionID);
                }

                if (Statuscode != null && Statuscode != -1)
                {
                    sb.Append(" and tab.StatusID = " + Statuscode);
                }
                else
                {
                    sb.Append(" and tab.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_SelectAllEmployeeinPayRoll, sb), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_SelectAllEmployeeinPayRoll, sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectAllEmployeeinPayRoll, sb), sqlparams);

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

        public static DataTable GetAllActiveDataHavingPayRollINN1DailyWages(Nullable<int> companyID, Nullable<bool> isActive = true, SqlConnection objConnection = null, SqlTransaction objTransaction = null, int LocationID = 0, int DivisionID = 0, int personTypeID = 0, Nullable<int> JobCategoryID = null, Nullable<int> Statuscode = null, Nullable<int> DesignationID = null, Nullable<int> DepartmentID = null, Nullable<int> SectionID = null, string ToDate = null, Nullable<Guid> UserID = null, int FromEmployee = 0, int ToEmployee = 0, string FromDate = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            //int SalMth = int.Parse(SalMonth.ToString() == "" ? "0" : SalMonth.ToString());
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (isActive.HasValue)
                    sqlparams[1] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[1] = new SqlParameter("@IsActive", DBNull.Value);
                if (ToDate != "")
                {
                    sqlparams[2] = new SqlParameter("@SalaryDate", ToDate);
                    sb.Append(" And (@SalaryDate <= tab.JobEndingDate OR  tab.JobEndingDate is null)");
                }

                DataSet ds = null;

                if (FromDate != "" && ToDate != "")
                {
                    sb1.Append(" AND ( EXISTS ( SELECT DISTINCT(tms.PersonID) FROM HRMS_TMSData tms WHERE tms.CompanyID = tab.CompanyID AND tms.PeriodID = tab.PeriodID AND tms.PersonID = tab.PersonID AND tms.TMSDateTime BETWEEN '"+ FromDate +"' AND '"+ ToDate +"' ) OR EXISTS ( SELECT DISTINCT(att.PersonID) FROM HRMS_EmployeeAttendance att WHERE att.CompanyID = tab.CompanyID AND att.PeriodID = tab.PeriodID AND att.PersonID = tab.PersonID AND att.MonthYear BETWEEN '"+ FromDate +"' AND '"+ ToDate +"' ) )");
                }

                if (LocationID > 0)
                {
                    sb.Append(" And tab.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" And tab.DivisionID = " + DivisionID);
                }

                if (personTypeID > 0)
                {
                    sb.Append(" And tab.PersonTypeCode = " + personTypeID);
                }
                if (JobCategoryID != -1 && JobCategoryID > 0)
                {
                    sb.Append(" And tab.FK_JobCategoryID =  " + JobCategoryID + "  ");
                }
                if (DesignationID != null && DesignationID > 0)
                {
                    sb.Append(" And tab.FK_JobDesignationID = " + DesignationID);
                }
                if (DepartmentID != null && DepartmentID > 0)
                {
                    sb.Append(" And tab.FK_DepartmentID = " + DepartmentID);
                }

                if (SectionID != null && SectionID > 0)
                {
                    sb.Append(" And tab.FK_SectionID = " + SectionID);
                }

                if (Statuscode != null && Statuscode != -1)
                {
                    sb.Append(" and tab.StatusID = " + Statuscode);
                }
                else
                {
                    sb.Append(" and tab.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }

                if (FromEmployee > 0 && ToEmployee > 0)
                {
                    sb.Append(" AND tab.PersonID BETWEEN "+ FromEmployee +" AND "+ ToEmployee +"");
                }

                if (FromEmployee > 0 && ToEmployee == 0)
                {
                    sb.Append(" AND tab.PersonID = " + FromEmployee);
                }

                if (FromEmployee == 0 && ToEmployee > 0)
                {
                    sb.Append(" AND tab.PersonID = " + ToEmployee);
                }

                if (objTransaction != null)
                    //ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_SelectAllEmployeeinPayRollDailyWages, sb), sqlparams);
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_SelectAllEmployeeinPayRollDailyWages, sb.ToString(), sb1.ToString()), sqlparams);
                else if (objConnection != null)
                    //ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_SelectAllEmployeeinPayRollDailyWages, sb), sqlparams);
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_SelectAllEmployeeinPayRollDailyWages, sb.ToString(), sb1.ToString()), sqlparams);
                else
                    //ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectAllEmployeeinPayRollDailyWages, sb), sqlparams);
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_SelectAllEmployeeinPayRollDailyWages, sb.ToString(), sb1.ToString()), sqlparams);

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

        public static DataTable GetAllActiveDataFromTMS(string SalMonth,string SalYear,  Nullable<int> companyID, Nullable<bool> isActive = true, SqlConnection objConnection = null, SqlTransaction objTransaction = null, int LocationID = 0, int DivisionID = 0, int personTypeID = 0, Nullable<int> Statuscode = null, Nullable<int> DesignationID = null, Nullable<int> DepartmentID = null, Nullable<int> SectionID = null, Nullable<DateTime> SalaryDate = null, Nullable<Guid> UserID = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            int SalMth = int.Parse(SalMonth.ToString() == "" ? "0" : SalMonth.ToString());
            int SalYr = int.Parse(SalYear.ToString() == "" ? "0" : SalYear.ToString());
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (isActive.HasValue)
                    sqlparams[1] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[1] = new SqlParameter("@IsActive", DBNull.Value);
                if (SalaryDate.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@SalaryDate", SalaryDate);
                    sb.Append(" And (@SalaryDate <= tab.JobEndingDate OR  tab.JobEndingDate is null)");
                }

                sqlparams[3] = new SqlParameter("@Month", SalMonth);
                sqlparams[4] = new SqlParameter("@Year", SalYear);

                DataSet ds = null;

                if (LocationID > 0)
                {
                    sb.Append(" And tab.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" And tab.DivisionID = " + DivisionID);
                }

                if (personTypeID > 0)
                {
                    sb.Append(" And tab.PersonTypeCode = " + personTypeID);
                }
                if (DesignationID != null && DesignationID > 0)
                {
                    sb.Append(" And tab.FK_JobDesignationID = " + DesignationID);
                }
                if (DepartmentID != null && DepartmentID > 0)
                {
                    sb.Append(" And tab.FK_DepartmentID = " + DepartmentID);
                }

                if (SectionID != null && SectionID > 0)
                {
                    sb.Append(" And tab.FK_SectionID = " + SectionID);
                }

                if (Statuscode != null && Statuscode != -1)
                {
                    sb.Append(" and tab.StatusID = " + Statuscode);
                }
                else
                {
                    sb.Append(" and tab.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_SelectAllEmployee_TMS, sb), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_SelectAllEmployee_TMS, sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectAllEmployee_TMS, sb), sqlparams);

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

        public static DataTable GetAllActiveDataFromTMSWithOT(string SalMonth, string SalYear, Nullable<int> companyID, Nullable<bool> isActive = true, SqlConnection objConnection = null, SqlTransaction objTransaction = null, int LocationID = 0, int DivisionID = 0, int personTypeID = 0, Nullable<int> Statuscode = null,int empid = -1, Nullable<int> DesignationID = null, Nullable<int> DepartmentID = null, Nullable<int> SectionID = null, Nullable<DateTime> SalaryDate = null, Nullable<Guid> UserID = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            int SalMth = int.Parse(SalMonth.ToString() == "" ? "0" : SalMonth.ToString());
            int SalYr = int.Parse(SalYear.ToString() == "" ? "0" : SalYear.ToString());
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (isActive.HasValue)
                    sqlparams[1] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[1] = new SqlParameter("@IsActive", DBNull.Value);
                if (SalaryDate.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@SalaryDate", SalaryDate);
                    sb.Append(" And (@SalaryDate <= tab.JobEndingDate OR  tab.JobEndingDate is null)");
                }

                sqlparams[3] = new SqlParameter("@Month", SalMonth);
                sqlparams[4] = new SqlParameter("@Year", SalYear);

                DataSet ds = null;

                if (LocationID > 0)
                {
                    sb.Append(" And tab.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" And tab.DivisionID = " + DivisionID);
                }

                if (personTypeID > 0)
                {
                    sb.Append(" And tab.PersonTypeCode = " + personTypeID);
                }
                if (DesignationID != null && DesignationID > 0)
                {
                    sb.Append(" And tab.FK_JobDesignationID = " + DesignationID);
                }
                if (DepartmentID != null && DepartmentID > 0)
                {
                    sb.Append(" And tab.FK_DepartmentID = " + DepartmentID);
                }

                if (SectionID != null && SectionID > 0)
                {
                    sb.Append(" And tab.FK_SectionID = " + SectionID);
                }

                if (empid > 0)
                {
                    sb.Append(" and tab.PersonID = " + empid);
                }

                if (Statuscode != null && Statuscode != -1)
                {
                    sb.Append(" and tab.StatusID = " + Statuscode);
                }
                else
                {
                    sb.Append(" and tab.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }

                sb.Append(" and tab.PersonID in (select PersonID from [AssignEmpToUsers] where UserID = '" + UserID + "'" + ")");

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_SelectAllEmployee_TMS_WithOT, sb), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_SelectAllEmployee_TMS_WithOT, sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectAllEmployee_TMS_WithOT, sb), sqlparams);

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


        public static DataTable GetAllActiveDataHavingPayRollINNForArears(string SalMonth, Nullable<int> companyID, Nullable<bool> isActive = true, SqlConnection objConnection = null, SqlTransaction objTransaction = null, int LocationID = 0, int DivisionID = 0, int personTypeID = 0, Nullable<int> Statuscode = null, Nullable<int> DesignationID = null, Nullable<int> DepartmentID = null, Nullable<int> SectionID = null, Nullable<DateTime> SalaryDate = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            int SalMth = int.Parse(SalMonth.ToString() == "" ? "0" : SalMonth.ToString());
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (isActive.HasValue)
                    sqlparams[1] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[1] = new SqlParameter("@IsActive", DBNull.Value);
                if (SalaryDate.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@SalaryDate", SalaryDate);
                    sb.Append(" And (@SalaryDate < tab.JobEndingDate OR  tab.JobEndingDate is null)");
                }

                DataSet ds = null;



                if (LocationID > 0)
                {
                    sb.Append(" And tab.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" And tab.DivisionID = " + DivisionID);
                }

                if (personTypeID > 0)
                {
                    sb.Append(" And tab.PersonTypeCode = " + personTypeID);
                }
                if (DesignationID != null && DesignationID > 0)
                {
                    sb.Append(" And tab.FK_JobDesignationID = " + DesignationID);
                }
                if (DepartmentID != null && DepartmentID > 0)
                {
                    sb.Append(" And tab.FK_DepartmentID = " + DepartmentID);
                }

                if (SectionID != null && SectionID > 0)
                {
                    sb.Append(" And tab.FK_SectionID = " + SectionID);
                }
                if (Statuscode != null && Statuscode != -1)
                {
                    sb.Append(" and tab.StatusID = " + Statuscode);
                }

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_SelectAllEmployeeinPayRollForArears, sb), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_SelectAllEmployeeinPayRollForArears, sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectAllEmployeeinPayRollForArears, sb), sqlparams);

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
        //public static DataTable GetAllActiveDataHavingPayRollINN(string SalMonth, Nullable<int> companyID, Nullable<bool> isActive = true, SqlConnection objConnection = null, SqlTransaction objTransaction = null, int LocationID = 0, int DivisionID = 0, int personTypeID = 0, Nullable<int> DesignationID = null, Nullable<int> DepartmentID = null, Nullable<int> SectionID = null)
        //{
        //    DataTable dt = null;
        //    StringBuilder sb = new StringBuilder();
        //    int SalMth = int.Parse(SalMonth.ToString() == "" ? "0" : SalMonth.ToString());
        //    try
        //    {
        //        SqlParameter[] sqlparams = new SqlParameter[2];
        //        sqlparams[0] = new SqlParameter("@CompanyID", companyID);

        //        if (isActive.HasValue)
        //            sqlparams[1] = new SqlParameter("@IsActive", isActive.Value);
        //        else
        //            sqlparams[1] = new SqlParameter("@IsActive", DBNull.Value);

        //        DataSet ds = null;
        //        if (LocationID > 0)
        //        {
        //            sb.Append(" And tab.FK_LocationID = " + LocationID);
        //        }

        //        if (DivisionID > 0)
        //        {
        //            sb.Append(" And tab.DivisionID = " + DivisionID);
        //        }

        //        if (personTypeID > 0)
        //        {
        //            sb.Append(" And tab.PersonTypeCode = " + personTypeID);
        //        }
        //        if (DesignationID != null && DesignationID > 0)
        //        {
        //            sb.Append(" And tab.FK_JobDesignationID = " + DesignationID);
        //        }
        //        if (DepartmentID != null && DepartmentID > 0)
        //        {
        //            sb.Append(" And tab.FK_DepartmentID = " + DepartmentID);
        //        }

        //        if (SectionID != null && SectionID > 0)
        //        {
        //            sb.Append(" And tab.FK_SectionID = " + SectionID);
        //        }

        //        if (objTransaction != null)
        //            ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_SelectAllEmployeeinPayRoll, sb), sqlparams);
        //        else if (objConnection != null)
        //            ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_SelectAllEmployeeinPayRoll, sb), sqlparams);
        //        else
        //            ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectAllEmployeeinPayRoll, sb), sqlparams);

        //        if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
        //            dt = ds.Tables[0];
        //    }
        //    catch (Exception ex)
        //    {
        //        SqlHelper.LogException(ex);
        //        throw ex;
        //    }

        //    return dt;
        //}

        public static DataTable GetShiftByPersonID(int companyID, int personID, string Query_GetShift, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PersonID", personID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetShift, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetShift, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetShift, sqlparams);

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

        public static int GetEmployeeBasicSalary(int companyID, int PersonID,SqlTransaction sqltrans = null)
        {
            int ID = 0;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);

                if (sqltrans != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(sqltrans,CommandType.Text, Query_GetEmployeeBasicSalary, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetEmployeeBasicSalary, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static DataTable GetDataByKeySingle(int companyID, int personID)
        {
            DataTable dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PersonID", personID);

                DataSet ds = null;

               
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byKey, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dr = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }


        public static DataTable GetAllEmployeeData(int CompanyID,string CnicNo ,int? personIDFrom=null, int? DesignationID=null)
        {
            DataTable dtReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
             
                DataSet ds = null;

                if (personIDFrom != null)
                {
                    sb.Append("AND HRMS_Set_Employee.PersonID =" + personIDFrom);
                }
                if (DesignationID != null)
                {

                    sb.Append("AND HRMS_Set_Employee.Fk_JobDesignationID= " + DesignationID);
                }
                if (CnicNo.ToString() != "-1")
                {
                    sb.Append(" and HRMS_Set_Employee.CNICNumber= '" + CnicNo + "' ");

                }
                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetEmployeeData + sb.ToString(), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dtReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }


            return dtReturn;
        }

        public static DataTable GetAllDataWithDesignation(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, SqlTransaction objTransaction = null)
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

                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_SelectAllData_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAllData_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAllData_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAllData_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAllData_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllData_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable FamilyTracking_Report(int companyID, int periodID, string Employee, string AddressLine, string PlaceOfBirth, int CityID, string Domicile, string MotherLanguage, string RecommendedBy1, string LanguageSpoken, int ProvinceID, string KinCNIC1, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                //sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                DataSet ds = null;

                if (Employee != "")
                    sb.Append(" and FullName like '%" + Employee + "%'");


                if (AddressLine != "")
                    sb.Append(" and AddressLine1 like '%" + AddressLine + "%'");

              

                if (CityID > 0)
                    sb.Append(" and FK_CityID = " + CityID + "");

                if (PlaceOfBirth != "")
                    sb.Append(" and PlaceOfBirth like '%" + PlaceOfBirth + "%'");

                if (Domicile != "")
                    sb.Append(" and Domicile like '%" + Domicile + "%'");

                if (MotherLanguage != "")
                    sb.Append(" and MotherLanguage like '%" + MotherLanguage + "%'");

                if (RecommendedBy1 != "")
                    sb.Append(" and RecommendedBy1 like '%" + RecommendedBy1 + "%'");

                
                if (LanguageSpoken != "")
                    sb.Append(" and LanguageSpoken like '%" + LanguageSpoken + "%'");

                if (ProvinceID > 0)
                    sb.Append(" and ProvinceID = " + ProvinceID + "");

                if (KinCNIC1 != "")
                    sb.Append(" and KinCNIC1 like '%" + KinCNIC1 + "%'");

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FaimlyTracking_Quality_Report, sb), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FaimlyTracking_Quality_Report, sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FaimlyTracking_Quality_Report, sb), sqlparams);

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


        public static DataTable GetAllEmpData(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, SqlTransaction objTransaction = null)
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

                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_SelectAll_WithPagingEmp + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPagingEmp + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_WithPagingEmp + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPagingEmp + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAll_WithoutPagingEmp + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_WithoutPagingEmp + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetDataForHiringProcess(int companyID, int days ,DateTime date)
        {
            DataTable dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@days", days);
                sqlparams[2] = new SqlParameter("@Date", date);

                DataSet ds = null;


                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_ForHiring, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dr = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }

        public static DataTable GetNewEmpCode(int companyID)
        {
            DataTable dr = null;
            #region query
            const string query_empcode = @"Select max(EmployeeCode) + 1 as EmpCode from HRMS_SET_Employee where IsActive = 1 and Status =1 and CompanyID = @CompanyID";
            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                //sqlparams[1] = new SqlParameter("@days", days);

                DataSet ds = null;


                ds = SqlHelper.ExecuteDataset(CommandType.Text, query_empcode, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dr = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }

        public static DataTable GetAllEmpDataForRoaster(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, SqlTransaction objTransaction = null)
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

                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_SelectAll_WithPagingEmpRoaster + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPagingEmpRoaster + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_WithPagingEmpRoaster + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPagingEmpRoaster + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAll_WithoutPagingEmpRoaster + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_WithoutPagingEmpRoaster + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        public static DataTable GetEmployeeAllLeaveBalance(int companyid, int PersonID, int PayrollTypeCode, int CurrentFiscalID=0)
        {
            DataTable dt = null;

            try
            {


                #region
                const string Query_GellEmployeeLeavesBalances = @"
Select HRMS_Set_Employee.FullName,HRMS_Set_Employee.FatherName,HRMS_Set_Employee.PersonID,HRMS_Set_Employee.EmployeeCode,
        HRMS_PayrollType.Description as LeaveTypeName,isnull(((HRMS_EmployeePayRoll.LeaveDays)-isnull (sum(DATEDIFF(dd,HRMS_LeaveRequest.FromDate,HRMS_LeaveRequest.ToDate) + 1),0)),0)
        as Balance,HRMS_EmployeePayRoll.PayRollTypeID,
        isnull((HRMS_EmployeePayRoll.LeaveDays),0) as LeaveDays,  isnull (sum(DATEDIFF(dd,HRMS_LeaveRequest.FromDate,HRMS_LeaveRequest.ToDate) + 1),0)  as Avail

        FROM HRMS_Set_Employee 

		inner JOIN HRMS_EmployeePayRoll ON HRMS_EmployeePayRoll.PersonID = HRMS_Set_Employee.PersonID
		AND HRMS_EmployeePayRoll.CompanyID = HRMS_Set_Employee.CompanyID 
		
		inner JOIN HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_EmployeePayRoll.PayrollTypeID
		AND HRMS_PayrollType.CompanyID = HRMS_EmployeePayRoll.CompanyID		
		
		left join HRMS_EmployeeAttendance on
	    HRMS_EmployeePayroll.PersonID = HRMS_EmployeeAttendance.PersonID and
	    HRMS_EmployeePayroll.PayrollTypeID = HRMS_EmployeeAttendance.LeaveID and	 
	    HRMS_EmployeePayroll.CompanyID = HRMS_EmployeeAttendance.CompanyID 
	      inner join HRMS_LeaveRequest on
	      HRMS_EmployeePayroll.PersonID = HRMS_LeaveRequest.Fk_PersonID and
	      HRMS_EmployeePayroll.PayrollTypeID = HRMS_LeaveRequest.FK_PayrollTypeID and
	      HRMS_EmployeePayroll.PayrollTypeCode = HRMS_LeaveRequest.FK_PayrollTypeCode and
	      HRMS_EmployeePayroll.CompanyID = HRMS_LeaveRequest.CompanyID and 
	      (HRMS_EmployeePayroll.IsOneTime is null or HRMS_EmployeePayroll.IsOneTime=0)  and HRMS_EmployeePayroll.Status = 1 and
	      HRMS_EmployeePayroll.IsActive = 1
		AND  HRMS_EmployeePayroll.FiscalID = @CurrentFiscalID
		    INNER join Set_PeriodCustom on HRMS_LeaveRequest.FromDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
			and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID

		--  AND HRMS_LeaveRequest.ToDate in (Select StartDate from Set_PeriodCustom Where Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID)	
        where HRMS_Set_Employee.CompanyID = @CompanyID 
		and HRMS_EmployeePayRoll.PersonID = @PersonID
		and HRMS_PayrollType.PayRollTypeCode = @PayRollTypeCode
		and HRMS_Set_Employee.Status =1
		and HRMS_EmployeePayRoll.Status =1
		and HRMS_PayrollType.Status = 1
	   -- and HRMS_PayrollType.Description like '%Annual%'

		group by 

		HRMS_Set_Employee.FullName,HRMS_Set_Employee.FatherName,
		HRMS_Set_Employee.PersonID,HRMS_Set_Employee.EmployeeCode,
        HRMS_PayrollType.Description,HRMS_EmployeePayRoll.LeaveDays,
        HRMS_EmployeePayRoll.PayRollTypeID";
                #endregion
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);
                sqlparams[2] = new SqlParameter("@PayrollTypeCode", PayrollTypeCode);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GellEmployeeLeavesBalances, sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetEmloyeeCodePrefix(int companyid, int StatusID = 0,string UserID = "")
        {
            DataTable dt = null;
            string filter = "";

            if (StatusID > 0)
                filter = " and StatusID = @StatusID ";

            if (UserID != "")
                filter = filter + " and StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "' ) ";

            try
            {


                #region
                const string Query_GetEmloyeeCodePrefix = @" Select * from HRMS_Employement_Status where Status = 1  ";
                #endregion
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@StatusID", StatusID);



                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetEmloyeeCodePrefix + filter, sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetEmloyeeBonusFormula(int companyid)
        {
            DataTable dt = null;
            string filter = "";

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
            



                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeeBonus + filter), sqlparams).Tables[0];
                


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        public static DataTable GetEmloyeeBySatusID(int companyid, int StatusID = 0)
        {
            DataTable dt = null;
            string filter = "";

            if (StatusID > 0)
                filter = " and StatusID = @StatusID ";

            try
            {


                #region
                const string Query_GetEmloyeeBYStatusID = @" Select * from HRMS_Set_Employee where Status = 1 and CompanyID = @CompanyID ";
                #endregion
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@StatusID", StatusID);


                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetEmloyeeBYStatusID + filter, sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static int GetNewEmployeeCode(int companyID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_MaxId, sqlparams));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_Get_MaxId, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_MaxId, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static DataTable GetEmloyeesByGradeID(int companyid, int GradeID = 0)
        {
            DataTable dt = null;
            string filter = "";

            if (GradeID > 0)
                filter = "   and FK_GradeID = @GradeID ";

            try
            {


                #region
                const string Query_GetEmloyeeByGradeID = @" Select * from hrms_set_employee where Status = 1 AND hrms_set_employee.CompanyID = @CompanyID ";
                #endregion
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@GradeID", GradeID);


                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetEmloyeeByGradeID+ filter, sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetEmloyeesByDepartmentID(int companyid, int DepartmentID = 0)
        {
            DataTable dt = null;
            string filter = "";

            if (DepartmentID > 0)
                filter = " and fk_departmentid = @DepartmentID ";

            try
            {


                #region
                const string Query_GetEmloyeeByDepartID = @" Select convert(bit,0) as IsChecked, -1 as AreaID,-1 as AreaDetailID,null as  StartingMonth,null as EndMonth,null as paymentMonth,null as IncrementPercentage,null as TotalAmount,* from hrms_set_employee where Status = 1  ";
                #endregion
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@DepartmentID", DepartmentID);


                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetEmloyeeByDepartID + filter, sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataRow GetEmpAgeByKey(int companyID, int personID,DateTime SalaryDate, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            #region Query
            const string Query_GetAge_byKey = @"SELECT FLOOR(DATEDIFF(DAY, DateOfBirth, @SalaryDate) / 365.25) as Age,
			Case when FLOOR(DATEDIFF(DAY, DateOfBirth, @SalaryDate) / 365.25) > 60 then 1 else 0 end as IsOld,*
			from HRMS_Set_Employee
			where CompanyID=@CompanyID    And PersonID=@PersonID    and Status=1";
            #endregion
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PersonID", personID);
                sqlparams[2] = new SqlParameter("@SalaryDate", SalaryDate);
                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetAge_byKey, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetAge_byKey, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAge_byKey, sqlparams);

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

        public static bool CheckISSSMember_Employee(int CompanyID, int PersonID, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            bool IsSSMember = false;
            #region

            const string QueryForEmployeeSecurity = @" Select IsSSMember from HRMS_Set_Employee Where PersonID=@PersonID and Status=1 and CompanyID = @CompanyID";
            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@PersonID", PersonID);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);


                if (objTransaction == null)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, QueryForEmployeeSecurity, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, QueryForEmployeeSecurity, sqlparams).Tables[0];



                if (dt != null && dt.Rows.Count > 0)
                {
                    IsSSMember = Convert.ToBoolean(dt.Rows[0]["IsSSMember"].ToString());
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return IsSSMember;
        }

        public static bool CheckISEmployeeStatusDailyWages(int companyid, int StatusID = 0)
        {
            DataTable dt = null;
            bool IsStatusDailyWages = false;
            #region

            const string Query_GetEmloyeeStatus = @" Select * from HRMS_Employement_Status where Status = 1 AND StatusID =  @StatusID  ";
            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                sqlparams[0] = new SqlParameter("@StatusID", StatusID);
                
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetEmloyeeStatus, sqlparams).Tables[0];

                if (dt != null && dt.Rows.Count > 0)
                {
                    if(dt.Rows[0]["Description"].ToString().Replace(" ","").ToLower().Contains("dailywages"))
                    IsStatusDailyWages = true;
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return IsStatusDailyWages;
        }

        public bool UpdateBasicSalaryByPersonID( SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@PersonID", f_PersonID);
                sqlparams[1] = new SqlParameter("@CompanyID", f_CompanyID);

                if (f_BasicSalary.HasValue)
                    sqlparams[2] = new SqlParameter("@BasicSalary", f_BasicSalary.Value);
                else
                    sqlparams[2] = new SqlParameter("@BasicSalary", DBNull.Value);

                if (f_PreviousGrossSalary.HasValue)
                    sqlparams[3] = new SqlParameter("@PreviousGrossSalary", f_PreviousGrossSalary.Value);
                else
                    sqlparams[3] = new SqlParameter("@PreviousGrossSalary", DBNull.Value);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateGSalary_By_PersonID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_UpdateGSalary_By_PersonID, sqlparams) > 0)
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

        public static DataTable StafffacilitiesReport(int companyid,int fiscalid, int PeriodID, DateTime FromDate, DateTime ToDate)
        {
            DataTable dt = null;
            string additionalFilter = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@FiscalID", fiscalid);
                sqlparams[2] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[3] = new SqlParameter("@FromDate", FromDate);
                sqlparams[4] = new SqlParameter("@ToDate", ToDate);

#region MyRegion
                const string Query_StafffacilitiesReport = @" select distinct AR_SET_Customer.CustomerID,(AR_SET_Customer.CustomerName + ' - ' + SET_JobDesignation .JobDesignationName) as CustomerName,SET_JobDesignation .JobDesignationName, 
Set_StaffFacilitesDetail.FacilityName as BusinessPartnerName
,(AP_PaymentSupplierDetail.Amount) as Amount
,Set_StaffFacilitesDetail.Limit
,DATENAME(month,AP_PaymentHeader.DocumentDate) as MonthDate , Set_StaffFacilites.VehicleNo
from AP_PaymentHeader
inner join AP_PaymentSupplierDetail on AP_PaymentSupplierDetail.PaymentHeaderID = AP_PaymentHeader.PaymentID
and AP_PaymentSupplierDetail.CompanyID = AP_PaymentHeader.CompanyID
and AP_PaymentSupplierDetail.PeriodID = AP_PaymentHeader.PeriodID
and AP_PaymentSupplierDetail.Status = 1
inner join Set_StaffFacilites on Set_StaffFacilites.StaffID =  AP_PaymentSupplierDetail.SupplierID
inner join Set_StaffFacilitesDetail on Set_StaffFacilitesDetail.StaffFacilitesHeaderID = Set_StaffFacilites.StaffFacilityID

inner join HRMS_Set_Employee on HRMS_Set_Employee.FK_CustomerID = Set_StaffFacilites.StaffID
inner join AR_SET_BusinessPartnerTypes on AR_SET_BusinessPartnerTypes.BusinessPartnerID = AP_PaymentSupplierDetail.SupplierID
and AR_SET_BusinessPartnerTypes.TypeCode = AP_PaymentSupplierDetail.BusinessPartnerTypeID
inner join AR_SET_BusinessPartner on AR_SET_BusinessPartner.BusinessPartnerID =  AR_SET_BusinessPartnerTypes.TypeCode 
inner join AR_SET_Customer on AR_SET_Customer.CustomerID = HRMS_Set_Employee.FK_CustomerID
inner join SET_JobDesignation on SET_JobDesignation.JobDesignationID = HRMS_Set_Employee.FK_JobDesignationID
where --AR_SET_BusinessPartner.BusinessPartnerName like '%'+Set_StaffFacilitesDetail.FacilityName +'%' and
  AP_PaymentHeader.CompanyID = @CompanyID
 and AP_PaymentHeader.PeriodID  = @PeriodID
and AP_PaymentHeader.status = 1
and AP_PaymentHeader.statuscode = 1
 --and AP_PaymentHeader.FiscalID  = @FiscalID
 and convert(date,AP_PaymentHeader.DocumentDate) between @FromDate and @ToDate
and AP_PaymentSupplierDetail.SupplierAccountCode = Set_StaffFacilitesDetail.AccountCode
-- group  by AR_SET_Customer.CustomerName,SET_JobDesignation .JobDesignationName, Set_StaffFacilitesDetail.FacilityName
--,Set_StaffFacilitesDetail.Limit
--,DATENAME(month,AP_PaymentHeader.DocumentDate),AR_SET_Customer.CustomerID, Set_StaffFacilites.VehicleNo
";
	#endregion

          dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_StafffacilitiesReport, additionalFilter), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable NewStafffacilitiesReport(int companyid, int fiscalid, int PeriodID, DateTime FromDate, DateTime ToDate)
        {
            DataTable dt = null;
            string additionalFilter = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@FiscalID", fiscalid);
                sqlparams[2] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[3] = new SqlParameter("@FromDate", FromDate);
                sqlparams[4] = new SqlParameter("@ToDate", ToDate);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AP.AP_PaymentHeader.QueryFor_NewStafffacilitiesReport), sqlparams).Tables[0];
                //    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_NewStafffacilitiesReport, additionalFilter), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

       

        public static DataTable StafffacilitiesReportNew(int companyid, int fiscalid, int PeriodID, DateTime FromDate, DateTime ToDate)
        {
            DataTable dt = null;
            string additionalFilter = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@FiscalID", fiscalid);
                sqlparams[2] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[3] = new SqlParameter("@FromDate", FromDate);
                sqlparams[4] = new SqlParameter("@ToDate", ToDate);

                #region MyRegion
                const string Query_StafffacilitiesReport = @" select AR_SET_Customer.CustomerID,(AR_SET_Customer.CustomerName + ' - ' + SET_JobDesignation .JobDesignationName) as CustomerName,SET_JobDesignation .JobDesignationName, 
Set_StaffFacilitesDetail.FacilityName as BusinessPartnerName
,(AP_PaymentSupplierDetail.Amount) as Amount
,Set_StaffFacilitesDetail.Limit
,DATENAME(month,AP_PaymentHeader.DocumentDate) as MonthDate , Set_StaffFacilites.VehicleNo
from AP_PaymentHeader
inner join AP_PaymentSupplierDetail on AP_PaymentSupplierDetail.PaymentHeaderID = AP_PaymentHeader.PaymentID
and AP_PaymentSupplierDetail.CompanyID = AP_PaymentHeader.CompanyID
and AP_PaymentSupplierDetail.PeriodID = AP_PaymentHeader.PeriodID
and AP_PaymentSupplierDetail.Status = 1
inner join Set_StaffFacilites on Set_StaffFacilites.StaffID =  AP_PaymentSupplierDetail.SupplierID
inner join Set_StaffFacilitesDetail on Set_StaffFacilitesDetail.StaffFacilitesHeaderID = Set_StaffFacilites.StaffFacilityID

inner join HRMS_Set_Employee on HRMS_Set_Employee.FK_CustomerID = Set_StaffFacilites.StaffID
inner join AR_SET_BusinessPartnerTypes on AR_SET_BusinessPartnerTypes.BusinessPartnerID = AP_PaymentSupplierDetail.SupplierID
and AR_SET_BusinessPartnerTypes.TypeCode = AP_PaymentSupplierDetail.BusinessPartnerTypeID
inner join AR_SET_BusinessPartner on AR_SET_BusinessPartner.BusinessPartnerID =  AR_SET_BusinessPartnerTypes.TypeCode 
inner join AR_SET_Customer on AR_SET_Customer.CustomerID = HRMS_Set_Employee.FK_CustomerID
inner join SET_JobDesignation on SET_JobDesignation.JobDesignationID = HRMS_Set_Employee.FK_JobDesignationID
where --AR_SET_BusinessPartner.BusinessPartnerName like '%'+Set_StaffFacilitesDetail.FacilityName +'%' and
  AP_PaymentHeader.CompanyID = @CompanyID
 and AP_PaymentHeader.PeriodID  = @PeriodID
 --and AP_PaymentHeader.FiscalID  = @FiscalID
 and convert(date,AP_PaymentHeader.DocumentDate) between @FromDate and @ToDate
and AP_PaymentSupplierDetail.SupplierAccountCode = Set_StaffFacilitesDetail.AccountCode
-- group  by AR_SET_Customer.CustomerName,SET_JobDesignation .JobDesignationName, Set_StaffFacilitesDetail.FacilityName
--,Set_StaffFacilitesDetail.Limit
--,DATENAME(month,AP_PaymentHeader.DocumentDate),AR_SET_Customer.CustomerID, Set_StaffFacilites.VehicleNo
";
                #endregion

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_StafffacilitiesReport, additionalFilter), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetMajorStaffAdvance(Nullable<int> companyID, Nullable<int> PeriodID, int fiscalid, Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FiscalID", fiscalid);
                sqlparams[3] = new SqlParameter("@FromDate", fromDate);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_MajorStaffAdvances, sqlparams);

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



        public static DataTable GetEmployeeLoanBalance(int companyid, int PersonID, int LoanID)
        {
            DataTable dt = null;

            try
            {


                #region
                const string Query_GetEmployeeLoanBalance = @"

";
                #endregion
                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);
                sqlparams[2] = new SqlParameter("@LoanID", LoanID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_GetEmployeeLoanBalance, sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetEmployeeInfoForBonus(int companyid,string UserID, StringBuilder whereCaluse)
        {
            DataTable dt = null;

            try
            {


                #region
                const string Query_GetEmployeeInfoForBonus = @"
select ROW_NUMBER() OVER (ORDER BY personid) AS SerialNo, HRMS_Set_Employee.PersonID, HRMS_Set_Employee.EmployeeCode, HRMS_Set_Employee.FullName  as EmployeeName
,HRMS_Employement_Status.description as  EmployeeStatus,HRMS_Set_Employee.basicSalary as GrossSalary,0 as NetBonus
from HRMS_Set_Employee
inner join HRMS_EmployeeInEmployement_Status on HRMS_EmployeeInEmployement_Status.StatusID = HRMS_Set_Employee.StatusID
	 and HRMS_EmployeeInEmployement_Status.UserID = @UserID
     and HRMS_EmployeeInEmployement_Status.[CompanyID] = @CompanyID
Left Join HRMS_Employement_Status on HRMS_Employement_Status.StatusID =HRMS_Set_Employee.StatusID
And HRMS_Employement_Status.Status=1
where HRMS_Set_Employee.Status = 1 and HRMS_Set_Employee.CompanyID = @CompanyID
{0}

";
                #endregion
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@UserID", UserID);
                
                
                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetEmployeeInfoForBonus, whereCaluse.ToString()), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetEmployeeInfoForRate(int companyid, string UserID,int personiD, StringBuilder whereClause)
        {
            DataTable dt = null;

            try
            {


                #region
                const string Query_GetEmployeeInfoForRate = @"
select ROW_NUMBER() OVER (ORDER BY personid) AS SerialNo, HRMS_Set_Employee.PersonID, HRMS_Set_Employee.EmployeeCode, HRMS_Set_Employee.FullName  as EmployeeName
,HRMS_Employement_Status.description as  EmployeeStatus,HRMS_Set_Employee.basicSalary as GrossSalary,0 as NetRate
from HRMS_Set_Employee
inner join HRMS_EmployeeInEmployement_Status on HRMS_EmployeeInEmployement_Status.StatusID = HRMS_Set_Employee.StatusID
	 and HRMS_EmployeeInEmployement_Status.UserID = @UserID
     and HRMS_EmployeeInEmployement_Status.[CompanyID] = @CompanyID
Left Join HRMS_Employement_Status on HRMS_Employement_Status.StatusID =HRMS_Set_Employee.StatusID
And HRMS_Employement_Status.Status=1
where HRMS_Set_Employee.Status = 1 and HRMS_Set_Employee.CompanyID = @CompanyID and HRMS_Set_Employee.PersonID = @PersonID 
{0}

";
                #endregion
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@UserID", UserID);
                sqlparams[2] = new SqlParameter("@PersonID", personiD);


                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetEmployeeInfoForRate, whereClause.ToString()), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GETEmployeeCodebyPersonID(Nullable<int> companyID, int? PersonID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);

                DataSet ds = null;

                #region Query
                const string Query_GetEmployeeCodebyPersonID = @"
                Select EmployeeCode from HRMS_Set_Employee where HRMS_Set_Employee.Status = 1 
                  and HRMS_Set_Employee.CompanyID = @CompanyID 
                  and HRMS_Set_Employee.PersonID = @PersonID
                ";
                #endregion


                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetEmployeeCodebyPersonID, sqlparams);

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

        public static int GETPersonIDbyEmployeeCode(Nullable<int> companyID, string EmployeeCode)
        {
            int Result = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@EmployeeCode", EmployeeCode);

                DataSet ds = null;

                #region Query
                const string Query_GetEmployeeCodebyPersonID = @"
                Select PersonID from HRMS_Set_Employee where HRMS_Set_Employee.Status = 1 
                  and HRMS_Set_Employee.CompanyID = @CompanyID 
                  and HRMS_Set_Employee.EmployeeCode = @EmployeeCode
                ";
                #endregion


                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetEmployeeCodebyPersonID, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    Result = int.Parse(ds.Tables[0].Rows.Count > 0 ? ds.Tables[0].Rows[0]["PersonID"].ToString() : "0");
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return Result;
        }

        public static DataRow GetDataByEmpCode(int companyID, string EmpCode, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                //sqlparams[1] = new SqlParameter("@EmployeeCode", EmpCode);

                DataSet ds = null;

                string Qry = @" SELECT * FROM [HRMS_Set_Employee] WHERE [CompanyID] = @CompanyID and [EmployeeCode] = " + EmpCode;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Qry, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Qry, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Qry, sqlparams);

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

        public static DataRow UpdateEmailDate(int emailID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            #region Query
            const string Query_UpdateEmailDate_byKey = @"
Update  SET_Emails Set EmailDate = FORMAT(GETDATE(), 'yyyy-MM-dd HH:mm:ss.fff') where EmailID = @EmailID
";
            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@EmailID", emailID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_UpdateEmailDate_byKey, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_UpdateEmailDate_byKey, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_UpdateEmailDate_byKey, sqlparams);

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

        public static DataTable AttendanceSheet_Pattoki(int CompanyID, DateTime FromDate, DateTime ToDate, Nullable<int> PersonID)
        {
            DataTable dt = null;

            #region Query
            const string Query_Report = @"
Select 
HRMS_SET_Employee.PersonID
,SET_Department.DepartmentName
,HRMS_SET_Employee.EmployeeCode
,HRMS_SET_Employee.CNICNumber
,HRMS_SET_Employee.FullName
,HRMS_SET_Employee.FatherName
,SET_Designation.DesignationName
,SET_JobDesignation.JobDesignationName 
,HRMS_SET_Employee.BasicSalary
,HRMS_SalaryHeader.DaysWorking
From HRMS_SET_Employee
Inner Join SET_Department on SET_Department.DepartmentID = HRMS_SET_Employee.FK_DepartmentID
Inner Join SET_JobDesignation on SET_JobDesignation.JobDesignationID = HRMS_SET_Employee.FK_JobDesignationID
Inner Join SET_Designation on SET_Designation.DesignationID = SET_JobDesignation.FK_DesignationID
Inner Join HRMS_SalaryHeader on HRMS_SalaryHeader.PersonID = HRMS_SET_Employee.PersonID
ANd HRMS_SalaryHeader.CompanyID = HRMS_SET_Employee.CompanyID
AND HRMS_SalaryHeader.Status= 1
Where HRMS_SET_Employee.CompanyID = @CompanyID
AND HRMS_SalaryHeader.SalaryDate Between @FromDate And @ToDate
";
            #endregion

            string filter = "";

            if (PersonID.HasValue)
                filter = " and HRMS_SET_Employee.PersonID =  " + PersonID.Value;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);

                sqlparams[1] = new SqlParameter("@FromDate", FromDate);
                sqlparams[2] = new SqlParameter("@ToDate", ToDate);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Report + filter, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetEmployeeCodePrefixDailyWages(int companyid, int StatusID = 0, string UserID = "")
        {
            DataTable dt = null;
            string filter = "";

            if (StatusID > 0)
                filter = " and StatusID = @StatusID ";

            if (UserID != "")
                filter = filter + " and StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "' ) ";

            try
            {


                #region
                const string Query_GetEmloyeeCodePrefixDailyWages = @" Select * from HRMS_Employement_Status where Status = 1 AND Description LIKE '%Daily Wages%' ";
                #endregion
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@StatusID", StatusID);



                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetEmloyeeCodePrefixDailyWages + filter, sqlparams).Tables[0];


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
