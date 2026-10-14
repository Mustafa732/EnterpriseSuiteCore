using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;
using System.Text;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Setup
{
    public partial class HRMS_TMSData
    {
        #region Non TMS Queries

        const string Query_Get_Shift = @" select hrms_employeeshift.ShiftID from
 hrms_employeeshift
where hrms_employeeshift.PeriodID = @PeriodID
and hrms_employeeshift.CompanyID = @CompanyID
and hrms_employeeshift.PersonID = @PersonID";

        const string Query_Get_ShiftAssignShift = @" select top 1 HRMS_AssignShiftsDetail.ShiftID ,HRMS_Set_Shift.TimeIn,HRMS_Set_Shift.TimeOut
from HRMS_AssignShiftsDetail 
INNER JOIN	HRMS_Set_Shift on HRMS_Set_Shift.ShiftID = HRMS_AssignShiftsDetail .ShiftID
inner join HRMS_AssignShiftsHeader on HRMS_AssignShiftsHeader.HeaderID = HRMS_AssignShiftsDetail.HeaderID
and HRMS_AssignShiftsHeader.Status = 1
where HRMS_AssignShiftsHeader.PersonID = @PersonID
and HRMS_AssignShiftsHeader .CompanyID = @CompanyID
and convert(date,@Date) between convert(Date,HRMS_AssignShiftsDetail.FromDate) and convert(Date,HRMS_AssignShiftsDetail.ToDate) and WeekDayName = @WeekDayName
order by HRMS_AssignShiftsDetail.HeaderID desc";

        const string Query_Validate_RoasterExecutedForMonth = @"if Exists (Select HRMS_AssignShiftsHeader.HeaderID from HRMS_AssignShiftsHeader
        inner join HRMS_AssignShiftsDetail on HRMS_AssignShiftsDetail.HeaderID=HRMS_AssignShiftsHeader.HeaderID
        where @ToDate between HRMS_AssignShiftsDetail.FromDate and HRMS_AssignShiftsDetail.ToDate)
	       select 1 as [Status]
        else
	       select 0 as [Status]";

        const string Query_InsertWithPersonCode = @" INSERT INTO [HRMS_TMSData] ([CompanyID], [PeriodID], [TableID], [TMSID], [TMSDateTime], [ShiftHeaderID], [PersonID], [ManagementCode], [TerminalNo], [CreatedBy], [CreateDate], [CreatedByValue], [RawData], [StatusCode], [PersonCode],[Reason])
		VALUES(@CompanyID, @PeriodID, @TableID, @TMSID, @TMSDateTime, @ShiftHeaderID, @PersonID, @ManagementCode, @TerminalNo, @CreatedBy, @CreateDate, @CreatedByValue, @RawData, @StatusCode, @PersonCode, @Reason) ";

        const string Query_Get_StatusCode = @"select StatusCode from HRMS_TMS_Status where Description = @Description";


        const string Query_AttendSheetCountWiseSummary = @"

     SELECT fintab.TheDate,convert(varchar(2),TheDate,106)as NewDate,
        case when fintab.IsHoliday = 1 then fintab.DayDescription else datename(dw,fintab.TheDate) end as Day_Name,
        case when fintab.IsHoliday = 1 then fintab.DayDescription else datename(dw,fintab.TheDate) end as Day_Name,
         ( Case when  fintab.DayDescription ='Monday' then 'Mon'
            when  fintab.DayDescription ='Tuesday' then 'Tue'
             when  fintab.DayDescription ='Wednesday' then 'Wed'
             when  fintab.DayDescription ='Thursday' then 'Thu'
             When  fintab.DayDescription ='Friday' then 'Fri'
             When  fintab.DayDescription ='Saturday' then 'Sat'
             When  fintab.DayDescription ='Sunday' then 'Sun'
          else ''  end ) as ShortDay_Name,
        datename(month,fintab.TheDate)  Month_Name,
        fintab.CompanyID,fintab.PersonID,fintab.PersonTypeCode,EmployeeCode,FullName,FatherName,DesignationName,DepartmentName,ShiftName,ShiftCode,
		(SELECT DAY(DATEADD(DD,-1,DATEADD(mm, DATEDIFF(mm, 0, GETDATE()) + 1, 0))) as NoofDay) as NoofDay,

        ShiftTimeIN as STimeIN,ShiftTimeOUT as STimeOUT,

        STimeIN,STimeOUT,MIN(TMSTimeIN) as ETimeIN,MAX(TMSTimeOUT) as ETimeOUT,SectionName,LocationName,
        fintab.JoiningDate,

        case 
	        when MIN(TMSTimeIN) < ShiftTimeIN then 'Early In'
	        when MIN(TMSTimeIN) >= ShiftTimeIN AND MIN(TMSTimeIN) <= STimeIN then 'On Time'
	        when MIN(TMSTimeIN) > STimeIN then 'Late In'
	        else '--'
        end 
        +
        case when DataTypeIN='*' then '*' else '' end
        + ' / ' +
        case 
	        when MAX(TMSTimeOUT) < STimeOUT then 'Early Out'
	        when MAX(TMSTimeOUT) >= STimeOUT AND MAX(TMSTimeOUT) <= ShiftTimeOUT then 'On Time'
	        when MAX(TMSTimeOUT) > ShiftTimeOUT then 'Late Out'
	        else '--'
        end 
        +
        case when DataTypeOUT='*' then '*' else '' end
        as AttStatus,

        case when CONVERT(varchar(20),MIN(TMSTimeIN),108) is not null then CONVERT(varchar(20),MIN(TMSTimeIN),108) else '----' end as Time_IN,
        case when CONVERT(varchar(20),MAX(TMSTimeOUT),108) is not null then CONVERT(varchar(20),MAX(TMSTimeOUT),108) else '----' end as Time_OUT,

   --     case when count(LR.LeaveRequestID) > 0 then 
			--case when MAX(PT.[Abbreviation]) is not null then MAX(PT.[Abbreviation]) else 'L' end
   --     else
	  --      case 
			--when fintab.IsHoliday = 1 and fintab.Description = 'Sunday' and MIN(TMSTimeIN) is null and MAX(TMSTimeOUT) is null then 'W/O'
			--when fintab.Reason != '' then 'G.H'	    	       
			--when MIN(TMSTimeIN) is not null then 'P'
	  --      when MAX(TMSTimeOUT) is not null then 'P'
	  --      else
	  --      'A'
	  --      end
   --     end as [Status],

      --newStatus

         case when count(LR.LeaveRequestID) > 0 then 
			case when MAX(PT.[Abbreviation]) is not null then MAX(PT.[Abbreviation]) else 'L' end

			when fintab.Reason != '' then 'G.H'
						  --3
  when (CASE WHEN max(TMSTimeOUT) is null and shiftName != 'OD' and min(STimeIN) is null then null 
		else case when isnull(IsNormalDay,0) = 1 and shiftName = 'OD' then
			LAG(ShiftName) OVER(ORDER BY fintab.TheDate,fintab.PersonID)
		else ShiftName end
		end ) = 'OD' then 'OD' 

        else
	        case 
			    	       
			    	       
				--4
 --   when CONVERT(VARCHAR(5),min(TMSTimeIN),108) < CONVERT(VARCHAR(5),ShiftTimeIN,108) and CONVERT(VARCHAR(5),max(TMSTimeOUT),108) < CONVERT(VARCHAR(5),ShiftTimeIN,108) then  'A'
	--when CONVERT(VARCHAR(5),min(TMSTimeIN),108) = null or CONVERT(VARCHAR(5),max(TMSTimeOUT),108) = null then  'A'
	--when CONVERT(VARCHAR(5),min(TMSTimeIN),108) > CONVERT(VARCHAR(5),ShiftTimeOUT,108) then  'A'
	
	----5
	--when CONVERT(VARCHAR(5),min(TMSTimeIN),108) between CONVERT(VARCHAR(5),ShiftTimeIN,108)  and CONVERT(VARCHAR(5),ShiftTimeOUT,108) then 'P'
	--when CONVERT(VARCHAR(5),max(TMSTimeOUT),108) between CONVERT(VARCHAR(5),ShiftTimeIN,108)  and  CONVERT(VARCHAR(5),ShiftTimeOUT,108) then 'P'
	--when CONVERT(VARCHAR(5),min(TMSTimeIN),108) > CONVERT(VARCHAR(5),ShiftTimeIN,108)  and  CONVERT(VARCHAR(5),min(TMSTimeIN),108) < CONVERT(VARCHAR(5),ShiftTimeOUT,108) then 'P'
	--when CONVERT(VARCHAR(5),max(TMSTimeOUT),108) > CONVERT(VARCHAR(5),ShiftTimeIN,108)  and  CONVERT(VARCHAR(5),max(TMSTimeOUT),108)< CONVERT(VARCHAR(5),ShiftTimeOUT,108) then 'P'
	--when CONVERT(VARCHAR(5),min(TMSTimeIN),108) < CONVERT(VARCHAR(5),ShiftTimeIN,108) and CONVERT(VARCHAR(5),max(TMSTimeOUT),108) > CONVERT(VARCHAR(5),ShiftTimeOUT,108)  then 'P'
	--else 'A' end
	  when  (stuff(convert(varchar(19), min(TMSTimeIN), 126),11,1,' ') < stuff(convert(varchar(19),DATEADD(DAY, 0, (ShiftTimeIN)), 126),11,1,' ')) 
  and  (stuff(convert(varchar(19), max(TMSTimeOUT), 126),11,1,' ') < stuff(convert(varchar(19),DATEADD(DAY, 0, (ShiftTimeIN)), 126),11,1,' '))  then 'A' 
	when CONVERT(VARCHAR(5),min(TMSTimeIN),108) = null or CONVERT(VARCHAR(5),max(TMSTimeOUT),108) = null then 'A'
	 when  (stuff(convert(varchar(19), min(TMSTimeIN), 126),11,1,' ') > stuff(convert(varchar(19),DATEADD(DAY, 0, (ShiftTimeOUT)), 126),11,1,' '))  then 'A'

	
	--5
	--when CONVERT(VARCHAR(5),min(TMSTimeIN),108) between CONVERT(VARCHAR(5),ShiftTimeIN,108)  and CONVERT(VARCHAR(5),ShiftTimeOUT,108) then 'P'
	when  (stuff(convert(varchar(19), min(TMSTimeIN), 126),11,1,' ') 
	between stuff(convert(varchar(19),DATEADD(DAY, 0, (ShiftTimeIN)), 126),11,1,' ') 
	and stuff(convert(varchar(19),DATEADD(DAY, 0, (ShiftTimeOUT)), 126),11,1,' ') ) then 'P'

	--when CONVERT(VARCHAR(5),max(TMSTimeOUT),108) between CONVERT(VARCHAR(5),ShiftTimeIN,108)  and  CONVERT(VARCHAR(5),ShiftTimeOUT,108) then 'P'
	when  (stuff(convert(varchar(19), max(TMSTimeOUT), 126),11,1,' ') 
	between stuff(convert(varchar(19),DATEADD(DAY, 0, (ShiftTimeIN)), 126),11,1,' ') 
	and stuff(convert(varchar(19),DATEADD(DAY, 0, (ShiftTimeOUT)), 126),11,1,' ') ) then 'P'

	--when CONVERT(VARCHAR(5),min(TMSTimeIN),108) > CONVERT(VARCHAR(5),ShiftTimeIN,108)  
	--and  CONVERT(VARCHAR(5),min(TMSTimeIN),108) < CONVERT(VARCHAR(5),ShiftTimeOUT,108) then 'P'
	when  (stuff(convert(varchar(19), min(TMSTimeIN), 126),11,1,' ') > stuff(convert(varchar(19), (ShiftTimeIN), 126),11,1,' ')
	and  stuff(convert(varchar(19),DATEADD(DAY, 0, min(TMSTimeIN)), 126),11,1,' ') <
	 stuff(convert(varchar(19),DATEADD(DAY, 0, (ShiftTimeOUT)), 126),11,1,' ') ) then 'P'


	--when CONVERT(VARCHAR(5),max(TMSTimeOUT),108) > CONVERT(VARCHAR(5),ShiftTimeIN,108)  
	--and  CONVERT(VARCHAR(5),max(TMSTimeOUT),108)< CONVERT(VARCHAR(5),ShiftTimeOUT,108) then 'P'
	when  (stuff(convert(varchar(19), max(TMSTimeOUT), 126),11,1,' ') > stuff(convert(varchar(19), (ShiftTimeIN), 126),11,1,' ')
	and  stuff(convert(varchar(19),DATEADD(DAY, 0, max(TMSTimeOUT)), 126),11,1,' ') <
	 stuff(convert(varchar(19),DATEADD(DAY, 0, (ShiftTimeOUT)), 126),11,1,' ') ) then 'P'

	--when CONVERT(VARCHAR(5),min(TMSTimeIN),108) < CONVERT(VARCHAR(5),ShiftTimeIN,108)
	-- and CONVERT(VARCHAR(5),max(TMSTimeOUT),108) > CONVERT(VARCHAR(5),ShiftTimeOUT,108)  then 'P'
		when  (stuff(convert(varchar(19), min(TMSTimeIN), 126),11,1,' ') < stuff(convert(varchar(19), (ShiftTimeIN), 126),11,1,' ')
	and  stuff(convert(varchar(19),DATEADD(DAY, 0, max(TMSTimeOUT)), 126),11,1,' ') >
	 stuff(convert(varchar(19),DATEADD(DAY, 0, (ShiftTimeOUT)), 126),11,1,' ') ) then 'P'

	else 'A' end
        end as [Status],

		--endNewStatus

        CONVERT(varchar(20),(MIN(TMSTimeOUT)-MIN(TMSTimeIN)),108)as CalculatedHours,IsOverTime,
         convert(varchar(5),Breaktime,108)  Breaktime,
        --case when MIN(TMSTimeOUT) > ShiftTimeOUT then
        --convert(varchar(5),DateDiff(s, ShiftTimeOUT, MIN(TMSTimeOUT))/3600)+':'+convert(varchar(5),DateDiff(s, ShiftTimeOUT, MIN(TMSTimeOUT))%3600/60) 
        --else '00:00' end as overtime,
        case when STimeOUT is null then ( case isnull(IsOverTime,0) when 0 then '00:00' else 
 ( case 
  when fintab.Reason != '' or fintab.Description = 'Sunday' then 
        CONVERT(varchar(20),(MIN(TMSTimeOUT)-MIN(TMSTimeIN) 
        -
        (case when CONVERT(varchar(20),MAX(Breaktime),108) is not null 
       then CONVERT(time,MAX(Breaktime),108) end )
         ),108) 
 when MIN(TMSTimeOUT) > ShiftTimeOUT then 
 convert(varchar(5),DateDiff(s, ShiftTimeOUT, MIN(TMSTimeOUT))/3600)+':'+convert(varchar(5),DateDiff(s, ShiftTimeOUT, MIN(TMSTimeOUT))%3600/60) 
          else '00:00' end )
          end
       ) else (
       case isnull(IsOverTime,0) when 0 then '00:00' else 
 (        
 case 
   when fintab.Reason != '' or fintab.Description = 'Sunday' then 
        
        CONVERT(varchar(20),(MIN(TMSTimeOUT)-MIN(TMSTimeIN) 
       -
        (case when CONVERT(varchar(20),MAX(Breaktime),108) is not null 
        then CONVERT(time,MAX(Breaktime),108) end )
         ),108) 
 when MIN(TMSTimeOUT) > STimeOUT then 
 convert(varchar(5),DateDiff(s, STimeOUT, MIN(TMSTimeOUT))/3600)+':'+convert(varchar(5),DateDiff(s, STimeOUT, MIN(TMSTimeOUT))%3600/60) 
          else '00:00' end )
          end
       ) end as OverTime,
        (DateDiff(s,MIN(TMSTimeIN),MIN(TMSTimeOUT))/3600)*60 + DateDiff(s, MIN(TMSTimeIN),MIN(TMSTimeOUT))%3600/60 as TotWorkMinuts,
        case when MIN(TMSTimeOUT) > ShiftTimeOUT then
        (DateDiff(s, ShiftTimeOUT, MIN(TMSTimeOUT))/3600)*60 + DateDiff(s, ShiftTimeOUT, MIN(TMSTimeOUT))%3600/60 
        else 0 end as TotOverMinuts,
        --TerminalNo,
        TerminalNo1,
        TerminalNo2,
        case when count(LR.LeaveRequestID) > 0 then 1 else 0 end as CountLeave,

        case when MIN(TMSTimeIN) is not null AND count(LR.LeaveRequestID) = 0  then 1 when MAX(TMSTimeOUT) is not null  AND count(LR.LeaveRequestID) = 0 then 1 else 0 end as CountPresent,
        case when (MIN(TMSTimeIN) is null AND MAX(TMSTimeOUT) is null) AND count(LR.LeaveRequestID)=0 then 1 else 0 end as CountAbsent,
        
        case when MIN(TMSTimeIN) < ShiftTimeIN then 1 else 0 end CountInEarly,
        case when MIN(TMSTimeIN) >= ShiftTimeIN AND MIN(TMSTimeIN) <= STimeIN then 1 else 0 end CountInOnTime,
        case when MIN(TMSTimeIN) > STimeIN then 1 else 0 end CountInLate,
        
        case when MAX(TMSTimeOUT) < STimeOUT then 1 else 0 end CountOutEarly,
        case when MAX(TMSTimeOUT) >= STimeOUT AND MAX(TMSTimeOUT) <= ShiftTimeIN then 1 else 0 end CountOutOnTime,
        case when MAX(TMSTimeOUT) > ShiftTimeIN then 1 else 0 end CountOutLate 
,
       case when MIN(TMSTIMEIN) > ShiftTImeIn then
        (DateDiff(s, ( case when STIMEIN is null then ShiftTimeIN else STIMEIN end ), MIN(TMSTIMEIN))/3600)*60 + DateDiff(s, ( case when STIMEIN is null then ShiftTimeIN else STIMEIN end ), MIN(TMSTIMEIN))%3600/60 
        else 0 end as TotlateMinuts

        from (

        SELECT SET_DateTime.IsNormalDay, SET_DateTime.Reason, SS.ShiftCode, SET_DateTime.Description,EP.FK_SectionID,EP.FK_LocationID,SET_DateTime.TheDate,EP.JoiningDate,
        EP.CompanyID,EP.PersonID,EP.PersonTypeCode,EP.EmployeeCode,EP.FullName,EP.FatherName,ED.JobDesignationName AS DesignationName,DP.DepartmentName AS DepartmentName,EP.IsOverTime as IsOverTime ,
        SET_DateTime.IsHoliday,SET_DateTime.[Description] as DayDescription,ETMS1.TerminalNo as TerminalNo1,ETMS2.TerminalNo as TerminalNo2,
        ETMS1.TMSDateTime as TMSTimeIN,ETMS2.TMSDateTime as TMSTimeOUT,SS.[Description] as ShiftName,
        (case  when ETMS1.TMSDateTime IS null and  ETMS2.TMSDateTime  is null then '00:00' else ABC.Breaktime  end) as Breaktime,
		case when ETMS1.[ManagementCode]='0' AND ETMS1.[TerminalNo]='0' AND ETMS1.[RawData]='M' then '*' else '' end DataTypeIN,
		case when ETMS2.[ManagementCode]='0' AND ETMS2.[TerminalNo]='0' AND ETMS2.[RawData]='M' then '*' else '' end DataTypeOUT,

        CONVERT(datetime,
		        CONVERT(varchar(2), DATEPART(mm, SET_DateTime.TheDate)) + '-' + 
		        CONVERT(varchar(2), DATEPART(dd, SET_DateTime.TheDate)) + '-' + 
		        CONVERT(varchar(4), DATEPART(year, SET_DateTime.TheDate)) + ' ' + CONVERT(varchar(100),SS.[TimeIN],114) 
	        ,101) as ShiftTimeIN,
        CONVERT(datetime,
	        CONVERT(varchar(2), DATEPART(mm, SET_DateTime.TheDate)) + '-' + 
	        CONVERT(varchar(2), DATEPART(dd, SET_DateTime.TheDate)) + '-' + 
	        CONVERT(varchar(4), DATEPART(year, SET_DateTime.TheDate)) + ' ' + CONVERT(varchar(100),SS.[TimeOUT],114) 
        ,101) as ShiftTimeOUT,

        
        CONVERT(datetime,
		        CONVERT(varchar(2), DATEPART(mm, SET_DateTime.TheDate)) + '-' + 
		        CONVERT(varchar(2), DATEPART(dd, SET_DateTime.TheDate)) + '-' + 
		        CONVERT(varchar(4), DATEPART(year, SET_DateTime.TheDate)) + ' ' + CONVERT(varchar(100),SS.[TimeINGrace],114) 
	        ,101) as STimeIN,
        CONVERT(datetime,
	        CONVERT(varchar(2), DATEPART(mm, SET_DateTime.TheDate)) + '-' + 
	        CONVERT(varchar(2), DATEPART(dd, SET_DateTime.TheDate)) + '-' + 
	        CONVERT(varchar(4), DATEPART(year, SET_DateTime.TheDate)) + ' ' + CONVERT(varchar(100),SS.[TimeOUTGrace],114) 
        ,101) as STimeOUT,
  ETMS1.TerminalNo

        from SET_DateTime 

        inner join HRMS_Set_Employee EP on (EP.CompanyID in @CompanyID)
		       {3}
		       and EP.Status = 1 and EP.IsActive = 1
  {1} 
        and EP.Status=1
left JOIN SET_JobDesignation ED ON ED.JobDesignationID = EP.FK_JobDesignationID AND ED.IsActive = 1 AND ED.Status = 1   
        left JOIN SET_Department DP ON DP.DepartmentID = EP.FK_DepartmentID AND DP.IsActive = 1 AND DP.Status = 1 
        
  --      left JOIN HRMS_EmployeeShift ES on ( ES.FromDate <= SET_DateTime.TheDate AND ES.ToDate >= SET_DateTime.TheDate) 
		--and ES.PersonID = EP.PersonID and ES.Status=1
		 left join HRMS_AttendanceRoaster ES on ES.PersonID =  EP.PersonID and ES.Status = 1
        --and ES.ShiftFrom <= SET_DateTime.TheDate AND ES.ShiftTo >= SET_DateTime.TheDate
		AND  CONVERT(date,ES.ShiftFrom)=Convert(date,SET_DateTime.TheDate)
        left JOIN HRMS_Set_Shift SS on SS.ShiftID = ES.ShiftID and SS.Status=1 and SS.IsActive = 1
           LEFT join HRMS_Set_Shift ABC on SET_DateTime.THEDATE >= ABC.FromDate and SET_DateTime.THEDATE <= ABC.ToDate 
            and ABC.Status=1 and ABC.IsActive = 1
        left JOIN HRMS_TMS_Status TMS1 on TMS1.StatusID=1 and TMS1.Status=1 -- Min
        left JOIN HRMS_TMS_Status TMS2 on TMS2.StatusID=2 and TMS2.Status=1 -- Max

        left join HRMS_TMSData ETMS1 on ETMS1.CompanyID = EP.CompanyID and ETMS1.PersonID = EP.PersonID and ETMS1.StatusCode=TMS1.StatusCode and
        CONVERT(datetime,
	        CONVERT(varchar(2), DATEPART(mm, ETMS1.TMSDateTime)) + '-' + 
	        CONVERT(varchar(2), DATEPART(dd, ETMS1.TMSDateTime)) + '-' + 
	        CONVERT(varchar(4), DATEPART(year, ETMS1.TMSDateTime)) 
        ,101)
         = SET_DateTime.TheDate

        left join HRMS_TMSData ETMS2 on ETMS2.CompanyID = EP.CompanyID and ETMS2.PersonID = EP.PersonID and ETMS2.StatusCode=TMS2.StatusCode and
        CONVERT(datetime,
	        CONVERT(varchar(2), DATEPART(mm, ETMS2.TMSDateTime)) + '-' + 
	        CONVERT(varchar(2), DATEPART(dd, ETMS2.TMSDateTime)) + '-' + 
	        CONVERT(varchar(4), DATEPART(year, ETMS2.TMSDateTime))
        ,101) 
        = SET_DateTime.TheDate
   {0}
        ) as fintab

        left join HRMS_LeaveRequest LR on 
        LR.CompanyID = fintab.CompanyID and LR.FK_PersonID = fintab.PersonID
        and LR.FromDate <= fintab.TheDate and LR.ToDate >= fintab.TheDate and LR.[Status]=1 and LR.[IsApproved]=1

        left join HRMS_PayrollType PT on 
        PT.PayrollTypeID=LR.FK_PayrollTypeID and PT.PayrollTypeCode=LR.FK_PayrollTypeCode and PT.[Status]=1 and PT.IsActive = 1
     
        left join Set_Section on Set_Section.SectionID =  fintab.FK_SectionID
        and Set_Section.Status =1 and Set_Section.IsActive = 1
        
        left join SET_Location on SET_Location.LocationID =  fintab.FK_LocationID
        and SET_Location.Status =1 and SET_Location.IsActive = 1
 {2}
        group by TheDate,
        fintab.CompanyID,fintab.PersonID,fintab.PersonTypeCode,EmployeeCode,FullName,FatherName,DesignationName,IsOverTime,DepartmentName,
        IsHoliday,fintab.DayDescription,ShiftName,DataTypeIN,DataTypeOUT,ShiftTimeIN,ShiftTimeOUT,TerminalNo1,ShiftCode,
        TerminalNo2,STimeIN,STimeOUT,SectionName,LocationName,fintab.JoiningDate,fintab.Reason,fintab.Description,Breaktime
		,fintab.IsNormalDay
        Order by TheDate,EmployeeCode,Month_Name

 ";

        public static int GetShiftID_BYPersonID(int companyID, int periodID, int PersonID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", PersonID);

                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_Shift, sqlparams));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_Get_Shift, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_Shift, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }


        public static int GetShiftID_BYPersonIDAssignShift(int companyID, int periodID, int PersonID, DateTime Date,string WeekDayName, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", PersonID);
                sqlparams[3] = new SqlParameter("@Date", Date);
                sqlparams[4] = new SqlParameter("@WeekDayName", WeekDayName);
                

                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_ShiftAssignShift, sqlparams));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_Get_ShiftAssignShift, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_ShiftAssignShift, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }




        const string Query_EmployeeIncrementHistory = @"
        select distinct --HRMS_Appraisal.CurrentSalary,
         HRMS_Set_Employee.EmployeeCode,HRMS_Set_Employee.FullName
        ,HRMS_Set_Employee.FatherName,HRMS_Set_Employee.PersonID,--DF.JobDesignationName as PromotionFrom,
        HRMS_AppraisalDetail.EffectiveFrom,HRMS_AppraisalDetail.PerformanceValue,
        --DT.JobDesignationName as PromotionTo,
		HRMS_EmployeePayroll.ManualAmount
		--HRMS_Appraisal.ChangeEmployeeTypeID,
       -- HRMS_Appraisal.PreviousEmployeeTypeID,
        --case HRMS_Appraisal.ChangeEmployeeTypeID when 1 then 'Permanant' when 2 then 'Contract' 
        --when 3 then 'Daily Wages' when 4 then 'Seasonal Contract' when 5 then 'Seasonal Daily Wages'
        -- end as ChangeEmployeeType,
        --case HRMS_Appraisal.PreviousEmployeeTypeID when 1 then 'Permanant' when 2 then 'Contract' 
        --when 3 then 'Daily Wages' when 4 then 'Seasonal Contract' when 5 then 'Seasonal Daily Wages'
        -- end as PreviousEmployeeType

         from HRMS_Appraisal
 
         inner join HRMS_AppraisalDetail on HRMS_AppraisalDetail.AppraisalID = HRMS_Appraisal.AppraisalID
         and HRMS_AppraisalDetail.CompanyID = HRMS_Appraisal.CompanyID

        inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_Appraisal.PersonID
        and HRMS_Set_Employee.CompanyID = HRMS_Appraisal.CompanyID
        and HRMS_Set_Employee.Status = 1

         left JOIN HRMS_EmployeePayroll On HRMS_Set_Employee.PersonID = HRMS_EmployeePayroll.PersonID
         And HRMS_Set_Employee.CompanyID = HRMS_EmployeePayroll.CompanyID

        --left JOIN SET_JobDesignation DF ON
        --HRMS_Appraisal.PromotionFrom =  DF.JobDesignationID AND
        --HRMS_Appraisal.CompanyID=  DF.CompanyID AND
        --DF.Status = 1 AND DF.IsActive = 1

        --left JOIN SET_JobDesignation DT ON
        --HRMS_Appraisal.PromotionTo =  DT.JobDesignationID AND
        --HRMS_Appraisal.CompanyID=  DT.CompanyID AND
        --DT.Status = 1 AND DT.IsActive = 1

        where HRMS_Appraisal.Status = 1 and HRMS_Appraisal.CompanyID =@CompanyID
   {0}
        ";

        const string Query_EmployeeIncrementHistoryNew = @"
        SELECT DISTINCT
		HRMS_Set_Employee.PersonID,
         HRMS_Set_Employee.EmployeeCode,HRMS_Set_Employee.FullName
        ,HRMS_Set_Employee.FatherName,HRMS_Set_Employee.PersonID,
        HRMS_EmployeeIncrement.PaymentDate,
		HRMS_EmployeeIncrement.EffectiveDate,
		HRMS_EmployeeIncrement.OldGrossPay AS PreviousSalary,
		(HRMS_EmployeeIncrement.OldGrossPay + HRMS_EmployeeIncrement.IncrementAmount) AS CurrentSalary,
		HRMS_EmployeeIncrement.IncrementPercentage, 
		HRMS_EmployeeIncrement.IncrementAmount
		

        from dbo.HRMS_EmployeeIncrement

        inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_EmployeeIncrement.PersonID
        and HRMS_Set_Employee.CompanyID = HRMS_EmployeeIncrement.CompanyID
        and HRMS_Set_Employee.Status = 1

        where HRMS_EmployeeIncrement.Status = 1 and HRMS_EmployeeIncrement.CompanyID = @CompanyID
        {0}
        ";

        const string Query_EmployeeRelativeInformation = @"
        select HRMS_EmployeeRelatives.*,
        HRMS_Set_Employee.FullName
        from HRMS_Set_Employee

        inner join HRMS_EmployeeRelatives on HRMS_EmployeeRelatives.PersonID = HRMS_Set_Employee.PersonID
        and HRMS_EmployeeRelatives.CompanyID = HRMS_Set_Employee.CompanyID

        where HRMS_EmployeeRelatives.ApprovedDate between @FromDate and @ToDate
        and HRMS_EmployeeRelatives.CompanyID =@CompanyID and HRMS_EmployeeRelatives.PeriodID =@PeriodID
        {0}
        ";

        const string Query_GetTrainingNeedAnalysisData = @"
        select HRMS_Set_Employee.PersonID,HRMS_Set_Employee.FullName,HRMS_Set_Employee.EmployeeCode,HRMS_EmployeeDevelopmentPlan.* 
        from HRMS_EmployeeDevelopmentPlan
        inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_EmployeeDevelopmentPlan.PersonID
        and HRMS_Set_Employee.CompanyID = HRMS_EmployeeDevelopmentPlan.CompanyID
        where HRMS_EmployeeDevelopmentPlan.CreateDate between @FromDate and @ToDate
        and HRMS_EmployeeDevelopmentPlan.CompanyID= @CompanyID
        and HRMS_EmployeeDevelopmentPlan.PeriodID= @PeriodID
        {0}
        ";
        #region Commented Query Area
        //        const string Query_EmployeeLeaveAdjustment = @"
        //        Select  HRMS_Set_Employee.FullName,HRMS_Set_Employee.FatherName,HRMS_Set_Employee.PersonID,HRMS_Set_Employee.EmployeeCode,
        //        HRMS_PayrollType.Description as LeaveTypeName,isnull(((HRMS_EmployeePayRoll.LeaveDays) - Sum(HRMS_EmployeeAttendance.Quantity)),0)as Balance,
        //        (HRMS_EmployeePayRoll.LeaveDays)as LeaveDays,isnull(SUM(HRMS_EmployeeAttendance.Quantity),0) as Quantity
        //        FROM HRMS_Set_Employee 
        //
        //		inner JOIN HRMS_EmployeePayRoll ON HRMS_EmployeePayRoll.PersonID = HRMS_Set_Employee.PersonID
        //		AND HRMS_EmployeePayRoll.CompanyID = HRMS_Set_Employee.CompanyID 
        //		
        //		inner JOIN HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_EmployeePayRoll.PayrollTypeID
        //		AND HRMS_PayrollType.CompanyID = HRMS_EmployeePayRoll.CompanyID		
        //		
        //		left join HRMS_EmployeeAttendance on
        //	    HRMS_EmployeePayroll.PersonID = HRMS_EmployeeAttendance.PersonID and
        //	    HRMS_EmployeePayroll.PayrollTypeID = HRMS_EmployeeAttendance.LeaveID and	 
        //	    HRMS_EmployeePayroll.CompanyID = HRMS_EmployeeAttendance.CompanyID 
        //	    
        //		where HRMS_Set_Employee.CompanyID = @CompanyID 
        //		--and HRMS_EmployeeAttendance.MonthYear between @FromDate and @ToDate
        //		and HRMS_PayrollType.PayRollTypeCode = 4
        //{0}
        //		and HRMS_Set_Employee.Status =1
        //		and HRMS_EmployeePayRoll.Status =1
        //		and HRMS_PayrollType.Status = 1
        //		--and HRMS_EmployeeAttendance.Status = 1
        //		group by 
        //		HRMS_Set_Employee.FullName,HRMS_Set_Employee.FatherName,
        //		HRMS_Set_Employee.PersonID,HRMS_Set_Employee.EmployeeCode,
        //        HRMS_PayrollType.Description,HRMS_EmployeePayRoll.LeaveDays
        //        ";
        //const string Query_EmployeeLeaveAdjustment = @"";

        #endregion
        const string Query_EmployeeBankAdvice = @"select HRMS_Set_Employee.PersonID,HRMS_Set_Employee.EmployeeCode,HRMS_Set_Employee.FullName,
        case when HRMS_SalaryHeader.NetAmount%10 = 9 then  ROUND(HRMS_SalaryHeader.NetAmount,-1) else HRMS_SalaryHeader.NetAmount end as NetAmount,
        AR_SET_Bank.BankName, AR_SET_BankBranch.BankBranchName,AR_SET_Bank.BankID,AR_SET_BankBranch.BankBranchID,HRMS_SalaryHeader.SalaryDate
        ,HRMS_Set_Employee.CreatedByValue as UserName
        FROM HRMS_Set_Employee 
  
        INNER JOIN HRMS_SalaryHeader ON HRMS_SalaryHeader.PersonID = HRMS_Set_Employee.PersonID
        AND HRMS_SalaryHeader.CompanyID = HRMS_Set_Employee.CompanyID 
   
        RIGHT JOIN AR_SET_BankBranch on 
        AR_SET_BankBranch.BankBranchID =  HRMS_Set_Employee.FK_BankBranchID
        and AR_SET_BankBranch.status =1 
        
        LEFT JOIN AR_SET_Bank on
        HRMS_SET_Employee.FK_BankAccountID = AR_SET_Bank.BankID and 
        AR_SET_Bank.Status = 1
        
        INNER JOIN AR_Set_bankAccount On AR_SET_BankBranch.BankBranchID = AR_Set_bankAccount.BankBranchID
        And AR_Set_bankAccount.PeriodID = 1 And AR_Set_bankAccount.Status = 1 
          
        INNER JOIN SET_EmployeeType ON SET_EmployeeType.TypeID = HRMS_Set_Employee.PersonTypeCode
        AND SET_EmployeeType.Status = 1 
        
        INNER JOIN HRMS_EmployeePayment ON HRMS_EmployeePayment.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID
        AND HRMS_EmployeePayment.Status = 1

        where  HRMS_SalaryHeader.SalaryDate between @FromDate and @ToDate
        and HRMS_SalaryHeader.CompanyID =@CompanyID and HRMS_SalaryHeader.PeriodID =@PeriodID
        and HRMS_Set_Employee.IsActive = 1
        {0}
        ";

     
        const string Query_GetTMSActiveData = @"SELECT  
        HRMS_TMSData.PersonID,
        PersonTypeCode,
        SET_Designation.DesignationName AS JobDesignationName,
        SET_Department.DepartmentName AS DepartmentName,
        TableID,
        StatusCode,
        EnumeratedDescription AS [Status],
        ShiftHeaderID,
        HRMS_Set_Employee.FullName,
        TMSDateTime AS TMSDate,
        TMSDateTime AS TMSTime

        from HRMS_TMSData

        left join LookupDetail on
        LookupDetail.LookUpHeaderID = (select LookupID from LookupHeader where [LookupType]='TMS_Status')
        and LookupDetail.LookupDetailID = HRMS_TMSData.StatusCode

        left join HRMS_Set_Employee on
        HRMS_TMSData.PersonID =HRMS_Set_Employee.PersonID AND
        HRMS_Set_Employee.Status = 1 and HRMS_Set_Employee.IsActive = 1

        LEFT JOIN  SET_Designation ON HRMS_Set_Employee.FK_JobDesignationID = SET_Designation.DesignationID AND
        SET_Designation.IsActive = 1 AND SET_Designation.Status = 1 
  
        LEFT JOIN SET_Department ON HRMS_Set_Employee.FK_DepartmentID = SET_Department.DepartmentID AND
        SET_Department.IsActive = 1 AND SET_Department.Status = 1 

        where (HRMS_TMSData.CompanyID = @CompanyID OR @CompanyID is null) AND  (HRMS_TMSData.PeriodID = @PeriodID OR @PeriodID is null) 
        and HRMS_TMSData.TMSDateTime between @FromDate and @ToDate and( HRMS_TMSData.PersonID = @PersonID OR @PersonID is null)  
        ";

        #endregion

        #region attendanceregister two Hannan

        const string Query_EmployeeSummary = @"
Select    fintab1.CompanyID,fintab1.PersonID,fintab1.PersonTypeCode,fintab1.EmployeeCode,fintab1.FullName,fintab1.FatherName,fintab1.DesignationName,fintab1.DepartmentName,
        SUm(fintab1.leaveDays) as leaveDays,CAst(isnull(Sum(fintab1.CountLeave) + SUM(fintab1.CountAbsent),0)as int) as Quantity,
        isnull(SUm(fintab1.leaveDays) - CAst(isnull(Sum(fintab1.CountLeave) + SUM(fintab1.CountAbsent),0)as int),0) as Balance,
        ISNULL(SUM(fintab1.CountInLate),0) as Late ,ISNULL(SUM(fintab1.CountOutEarly),0) as EarlyDeparture,
        sum(fintab1.ShortLeaveType) as ShortLeaveType
        
from (
SELECT
        fintab.CompanyID,fintab.PersonID,
       fintab.PersonTypeCode,EmployeeCode
        ,FullName,FatherName,DesignationName,DepartmentName,
        fintab.PersonCode,
        LR.ShortLeaveType,
     Sum(EPR.leaveDays) as leaveDays,									
        case when count(LR.LeaveRequestID) > 0 then 1 else 0 end as CountLeave,

        case when MIN(TMSTimeIN) is not null AND count(LR.LeaveRequestID) = 0  then 1 when MAX(TMSTimeOUT) is not null  AND count(LR.LeaveRequestID) = 0 then 1 else 0 end as CountPresent,
        case when (MIN(TMSTimeIN) is null AND MAX(TMSTimeOUT) is null) AND count(LR.LeaveRequestID)=0 then 1 else 0 end as CountAbsent,
        
        case when MIN(TMSTimeIN) < ShiftTimeIN then 1 else 0 end CountInEarly,
        case when MIN(TMSTimeIN) >= ShiftTimeIN AND MIN(TMSTimeIN) <= STimeIN then 1 else 0 end CountInOnTime,
        case when MIN(TMSTimeIN) > STimeIN then 1 else 0 end CountInLate,
        
        case when MAX(TMSTimeOUT) < STimeOUT then 1 else 0 end CountOutEarly,
        
        case when MAX(TMSTimeOUT) >= STimeOUT AND MAX(TMSTimeOUT) <= ShiftTimeIN then 1 else 0 end CountOutOnTime,
        
        case when MAX(TMSTimeOUT) > ShiftTimeIN then 1 else 0 end CountOutLate 
        
        from (

        SELECT SET_DateTime.Reason, SET_DateTime.Description,EP.FK_SectionID,EP.FK_LocationID,SET_DateTime.TheDate,EP.JoiningDate,
        EP.CompanyID,ES.ShiftID AS SDI,HS.Description AS sh,ABC.Breaktime,EP.PersonID,EP.PersonTypeCode,EP.EmployeeCode,EP.FullName,EP.FatherName,ED.JobDesignationName AS DesignationName,DP.DepartmentName AS DepartmentName,EP.IsOverTime as IsOverTime ,
        SET_DateTime.IsHoliday,SET_DateTime.[Description] as DayDescription,ETMS1.PersonCode,
        ETMS1.TMSDateTime as TMSTimeIN,ETMS2.TMSDateTime as TMSTimeOUT,SS.Description as ShiftName, 

		case when ETMS1.[ManagementCode]='0' AND ETMS1.[TerminalNo]='0' AND ETMS1.[RawData]='M' then '*' else '' end DataTypeIN,
		case when ETMS2.[ManagementCode]='0' AND ETMS2.[TerminalNo]='0' AND ETMS2.[RawData]='M' then '*' else '' end DataTypeOUT,

        CONVERT(datetime,
		        CONVERT(varchar(2), DATEPART(mm, SET_DateTime.TheDate)) + '-' + 
		        CONVERT(varchar(2), DATEPART(dd, SET_DateTime.TheDate)) + '-' + 
		        CONVERT(varchar(4), DATEPART(year, SET_DateTime.TheDate)) + ' ' + CONVERT(varchar(100),SS.[TimeIN],114) 
	        ,101) as ShiftTimeIN,
        CONVERT(datetime,
	        CONVERT(varchar(2), DATEPART(mm, SET_DateTime.TheDate)) + '-' + 
	        CONVERT(varchar(2), DATEPART(dd, SET_DateTime.TheDate)) + '-' + 
	        CONVERT(varchar(4), DATEPART(year, SET_DateTime.TheDate)) + ' ' + CONVERT(varchar(100),SS.[TimeOUT],114) 
        ,101) as ShiftTimeOUT,

        
        CONVERT(datetime,
		        CONVERT(varchar(2), DATEPART(mm, SET_DateTime.TheDate)) + '-' + 
		        CONVERT(varchar(2), DATEPART(dd, SET_DateTime.TheDate)) + '-' + 
		        CONVERT(varchar(4), DATEPART(year, SET_DateTime.TheDate)) + ' ' + CONVERT(varchar(100),SS.[TimeINGrace],114) 
	        ,101) as STimeIN,
        CONVERT(datetime,
	        CONVERT(varchar(2),  DATEPART(mm, SET_DateTime.TheDate)) + '-' + 
	        CONVERT(varchar(2), DATEPART(dd, SET_DateTime.TheDate)) + '-' + 
	        CONVERT(varchar(4), DATEPART(year, SET_DateTime.TheDate)) + ' ' + CONVERT(varchar(100),SS.[TimeOUTGrace],114) 
        ,101) as STimeOUT,
  ETMS1.TerminalNo

        from SET_DateTime
	
        inner join HRMS_Set_Employee EP on (EP.CompanyID = @CompanyID OR @CompanyID is null)
  {0}
{1}

    and
        
        EP.Status=1
left JOIN SET_JobDesignation ED ON ED.JobDesignationID = EP.FK_JobDesignationID AND ED.IsActive = 1

AND ED.Status = 1   
        left JOIN SET_Department DP ON DP.DepartmentID = EP.FK_DepartmentID AND DP.IsActive = 1 AND DP.Status = 1 
      
        
        left JOIN HRMS_EmployeeShift ES on ( ES.FromDate <= SET_DateTime.TheDate AND ES.ToDate >= SET_DateTime.TheDate) 
        and  ES.PersonID = EP.PersonID and ES.Status=1
        
		left join HRMS_Set_Shift HS on HS.ShiftID = ES.ShiftID
		and HS.Status=1
      
        
       
       --left JOIN HRMS_Set_Shift SS on SS.ShiftID = ES.ShiftID and SS.Status=1
    
     
        left JOIN HRMS_TMS_Status TMS1 on TMS1.StatusID=1 and TMS1.Status=1 -- Min
        left JOIN HRMS_TMS_Status TMS2 on TMS2.StatusID=2 and TMS2.Status=1 -- Max

        left join HRMS_TMSData ETMS1 on ETMS1.CompanyID = EP.CompanyID and ETMS1.PersonID = EP.PersonID and ETMS1.StatusCode=TMS1.StatusCode and
        CONVERT(datetime,
	        CONVERT(varchar(2), DATEPART(mm, ETMS1.TMSDateTime)) + '-' + 
	        CONVERT(varchar(2), DATEPART(dd, ETMS1.TMSDateTime)) + '-' + 
	        CONVERT(varchar(4), DATEPART(year, ETMS1.TMSDateTime)) 
        ,101)
         = SET_DateTime.TheDate

        left join HRMS_TMSData ETMS2 on ETMS2.CompanyID = EP.CompanyID and ETMS2.PersonID = EP.PersonID and ETMS2.StatusCode=TMS2.StatusCode 
        and CONVERT(datetime,
	        CONVERT(varchar(2), DATEPART(mm, ETMS2.TMSDateTime)) + '-' + 
	        CONVERT(varchar(2), DATEPART(dd, ETMS2.TMSDateTime)) + '-' + 
	        CONVERT(varchar(4), DATEPART(year, ETMS2.TMSDateTime))
        ,101) 
        = SET_DateTime.TheDate
        
        left join HRMS_TMSData ETMS_Shift on ETMS_Shift.CompanyID = EP.CompanyID and ETMS_Shift.PersonID = EP.PersonID 
        and CONVERT(datetime, 
	        CONVERT(varchar(2), DATEPART(mm, ETMS_Shift.TMSDateTime)) + '-' + 
	        CONVERT(varchar(2), DATEPART(dd, ETMS_Shift.TMSDateTime)) + '-' + 
	        CONVERT(varchar(4), DATEPART(year, ETMS_Shift.TMSDateTime))
        ,101) 
        = SET_DateTime.TheDate
			
        
       LEFT join HRMS_Set_Shift SS on ETMS_Shift.TMSDateTime >= SS.FromDate AND ETMS_Shift.TMSDateTime <= SS.ToDate 
         LEFT join HRMS_Set_Shift ABC on SET_DateTime.THEDATE >= ABC.FromDate and SET_DateTime.THEDATE <= ABC.ToDate 
    
      
  where
  
   --EP.EmployeeCode  not in (ETMS_Shift.PersonCode)
  --and
   SET_DateTime.TheDate >= @FromDate and SET_DateTime.TheDate <= @ToDate  and EP.IsActive = 1   and SET_DateTime.TheDate >= EP.JoiningDate  and SET_DateTime.TheDate <= isnull(EP.jobEndingDate,getdate()) 

   				
        ) as fintab

        left join HRMS_LeaveRequest LR on 
        LR.CompanyID = fintab.CompanyID and LR.FK_PersonID = fintab.PersonID
        and LR.FromDate <= fintab.TheDate and LR.ToDate >= fintab.TheDate and LR.[Status]=1 and LR.[IsApproved]=1

        left join HRMS_PayrollType PT on 
        PT.CompanyID=LR.CompanyID and PT.PayrollTypeID=LR.FK_PayrollTypeID and PT.PayrollTypeCode=LR.FK_PayrollTypeCode and PT.[Status]=1
     
    Left join HRMS_EmployeePayroll EPR on
	      EPR.PersonID = LR.Fk_PersonID and
	      EPR.PayrollTypeID = LR.FK_PayrollTypeID and
	      EPR.PayrollTypeCode = LR.FK_PayrollTypeCode and
	      EPR.CompanyID = LR.CompanyID and 
	      (EPR.IsOneTime is null or EPR.IsOneTime=0)  and EPR.Status = 1 and
	      EPR.IsActive = 1
        left join Set_Section on Set_Section.SectionID =  fintab.FK_SectionID
        and Set_Section.Status =1 
        
        left join SET_Location on SET_Location.LocationID =  fintab.FK_LocationID
        and SET_Location.Status =1 
  and (TerminalNo = 1) 
 
        group by TheDate,
        fintab.CompanyID,fintab.PersonID,fintab.PersonTypeCode,EmployeeCode,FullName,FatherName,DesignationName,IsOverTime,DepartmentName,
        IsHoliday,fintab.DayDescription,ShiftName,DataTypeIN,DataTypeOUT,ShiftTimeIN,ShiftTimeOUT,--EPR.leaveDays,--ERP.EmployeePayrollID,
        STimeIN,STimeOUT,fintab.TerminalNo,SectionName,LocationName,fintab.JoiningDate,fintab.Reason,fintab.Description,fintab.Breaktime,LR.ShortLeaveType
        ,SDI,sh,fintab.PersonCode,fintab.TMSTimeIN
       
        ) as fintab1
      GROUP BY
  fintab1.CompanyID,fintab1.PersonID,fintab1.PersonTypeCode,fintab1.EmployeeCode,fintab1.FullName,fintab1.FatherName,fintab1.DesignationName,fintab1.DepartmentName
 Order by EmployeeCode ";

        const string attTwo2 = @"
               
                                          
            ";

        const string attTwo = @"
 
                                            ";


        #endregion

        #region Basic TMS Queries

        const string Query_GEtTMSDataForAdam = @"
select convert(datetime,(CONVERT(VARCHAR(10), cdate, 111) + ' '+ CONVERT(VARCHAR(8),TimeIn,108))) as TMSDateTime,* from tblTransactionActual
where timein is not null and cdate  between @FromDate and @ToDate
";

        const string Query_GEtTMSDataForAdamTimeOut = @"
select convert(datetime,(CONVERT(VARCHAR(10), cdate, 111) + ' '+ CONVERT(VARCHAR(8),Timeout,108))) as TMSDateTime,* from tblTransactionActual
where timeout is not null and cdate  between @FromDate and @ToDate
";

        const string Query_GEtTMSbyEMployeeID = @"

select top 1 * from HRMS_TMSDATA 
where PersonID = @PersonID and CompanyID = @CompanyID 
  And dateName(Month, TMSDateTime) = @Month
  and dateName(Year, TMSDateTime) = @Year;
 order by TMSDateTime
";

        const string Query_EmployeeAttendanceList = @"
        Select 
        DATEName(dw,SET_DateTime.TheDate) as TMSDay,
        SET_DateTime.TheDate
        ,ES.Description as Shift
        ,EP.FullName
        ,EP.PersonID
        ,EP.EmployeeCode
        ,SET_Designation.DesignationName AS JobDesignationName,
        SET_Department.DepartmentName AS DepartmentName,ETMS1.TMSDateTime,TMS1.[Description]
        from HRMS_TMSData ETMS1

        inner join SET_DateTime on CONVERT(datetime,
	        CONVERT(varchar(2), DATEPART(mm, ETMS1.TMSDateTime)) + '-' + 
	        CONVERT(varchar(2), DATEPART(dd, ETMS1.TMSDateTime)) + '-' + 
	        CONVERT(varchar(4), DATEPART(year, ETMS1.TMSDateTime)) 
        ,101)
         = SET_DateTime.TheDate

        inner join HRMS_Set_Employee EP on ( EP.PersonID = @PersonID OR @PersonID is null) and (EP.CompanyID = @CompanyID OR @CompanyID is null) and EP.Status=1
        inner join SET_Designation ON EP.FK_JobDesignationID = SET_Designation.DesignationID AND SET_Designation.IsActive = 1 AND SET_Designation.Status = 1 
        inner join SET_Department ON EP.FK_DepartmentID = SET_Department.DepartmentID AND SET_Department.IsActive = 1 AND SET_Department.Status = 1 

        LEFT JOIN HRMS_EmployeeShift ES on ( ES.FromDate <= SET_DateTime.TheDate AND ES.ToDate >= SET_DateTime.TheDate) and ES.PersonID = EP.PersonID and ES.Status=1
        LEFT JOIN HRMS_Set_Shift SS on SS.ShiftID = ES.ShiftID and SS.Status=1
  
        LEFT JOIN HRMS_TMS_Status TMS1 on TMS1.StatusCode=ETMS1.StatusCode and TMS1.Status=1

        where ETMS1.CompanyID = EP.CompanyID and ETMS1.PersonID = EP.PersonID 
        and  (CONVERT(datetime,
	        CONVERT(varchar(2), DATEPART(mm, ETMS1.TMSDateTime)) + '-' + 
	        CONVERT(varchar(2), DATEPART(dd, ETMS1.TMSDateTime)) + '-' + 
	        CONVERT(varchar(4), DATEPART(year, ETMS1.TMSDateTime)) 
        ,101) between @FromDate and @ToDate)
{0}
        order by thedate,TMS1.[Description]
        ";

        const string Query_AttendanceSheet = @"
        Select distinct
        case 
        when ETMS1.TMSDateTime is null then
	        case 
	        when SET_DateTime.IsHoliday = 1 then 
		        SET_DateTime.[Description] 
	        else 
		        'A' 
	        end
        else
	        'P' 
        end as Status, 
        SS.TimeIn,SS.TimeOut,SS.Description
        ,SET_Department.DepartmentName
        ,SET_Designation.DesignationName AS JobDesignationName
         ,DATEName(dw,SET_DateTime.TheDate) as TMSDay,
        SET_DateTime.TheDate,cast( SS.TimeIn as time) as TimeIn
        ,cast( SS.TimeOut as time) as TimeOut
        --,ES.Description
        ,EP.FullName
        ,EP.PersonID
        ,EP.EmployeeCode

        from SET_DateTime 

        INNER JOIN HRMS_Set_Employee EP on ( EP.PersonID = @PersonID OR @PersonID is null) and (EP.CompanyID = @CompanyID OR @CompanyID is null) and EP.Status = 1 and EP.IsActive = 1

        LEFT JOIN HRMS_EmployeeShift ES on ( ES.FromDate <= SET_DateTime.TheDate AND ES.ToDate >= SET_DateTime.TheDate) and ES.PersonID = EP.PersonID and ES.Status=1
        LEFT JOIN HRMS_Set_Shift SS on SS.ShiftID = ES.ShiftID and SS.Status=1

        LEFT JOIN  SET_Designation ON EP.FK_JobDesignationID = SET_Designation.DesignationID AND SET_Designation.IsActive = 1 AND SET_Designation.Status = 1 
  
        LEFT JOIN SET_Department ON EP.FK_DepartmentID = SET_Department.DepartmentID AND SET_Department.IsActive = 1 AND SET_Department.Status = 1 
        left join HRMS_TMSData ETMS1 on ETMS1.CompanyID = EP.CompanyID and ETMS1.PersonID = EP.PersonID and --ETMS1.StatusCode=TMS1.StatusID and

        CONVERT(datetime,
	        CONVERT(varchar(2), DATEPART(mm, ETMS1.TMSDateTime)) + '-' + 
	        CONVERT(varchar(2), DATEPART(dd, ETMS1.TMSDateTime)) + '-' + 
	        CONVERT(varchar(4), DATEPART(year, ETMS1.TMSDateTime)) 
        ,101)
         = SET_DateTime.TheDate
  
        left join HRMS_TMS_Status TMS1 on TMS1.StatusID=ETMS1.StatusCode and TMS1.Status=1

        where (thedate between @FromDate and @ToDate)
{0}
        order by SET_DateTime.TheDate 
        ";

        const string Query_EmployeeExcessAttendance = @"
        Select COUNT(HRMS_TMSData.PersonID) as TotalDays, Min(TMSDateTime) As FirstAttendanceDate, Max(TMSDateTime) As LastAttendanceDate, HRMS_TMSData.PersonCode as EmployeeCode, HRMS_Set_Employee.FullName as EmployeeName
    From HRMS_TMSData
    Inner Join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_TMSData.PersonID
    And HRMS_Set_Employee.CompanyID = HRMS_TMSData.CompanyID
    And HRMS_Set_Employee.Status = 1
    And HRMS_TMSData.StatusCode = 1
    Inner Join HRMS_Employement_Status on HRMS_Employement_Status.StatusID = HRMS_Set_Employee.StatusID
    And HRMS_Employement_Status.CompanyID = HRMS_Set_Employee.CompanyID
    Where HRMS_TMSData.CompanyID = @CompanyID
    And HRMS_TMSData.PeriodID = @PeriodID
    And HRMS_Employement_Status.Description like '%daily%wages%'
    Group by  HRMS_TMSData.PersonCode, HRMS_Set_Employee.FullName
    Having COUNT(HRMS_TMSData.PersonID) >= @FromDays and COUNT(HRMS_TMSData.PersonID) <= @ToDays
        ";

        #endregion

        #region TMS Queries

        const string Query_MySelectAll_WithPaging = @"

SELECT HRMS_TMSData.[CompanyID], HRMS_TMSData.[PeriodID], HRMS_TMSData.[TableID], HRMS_TMSData.[TMSID],
HRMS_TMSData.[TMSDateTime] as TimeIn, HRMS_TMSData.[ShiftHeaderID], HRMS_TMSData.[PersonID],
HRMS_TMSData.[ManagementCode], HRMS_TMSData.[TerminalNo], HRMS_TMSData.[CreatedBy], HRMS_TMSData.[CreateDate],
HRMS_TMSData.[CreatedByValue],HRMS_TMSData.[UpdatedBy], HRMS_TMSData.[UpdateDate],
HRMS_TMSData.[UpdatedByValue], HRMS_TMSData.[RawData], HRMS_TMSData.[StatusCode], HRMS_TMSData.[PersonCode], HRMS_TMSData.[Reason],
HRMS_set_Employee.FullName,
ROW_NUMBER() Over (Order by HRMS_TMSData.CompanyID, HRMS_TMSData.PeriodID, HRMS_TMSData.TableID) as 'Row' FROM [HRMS_TMSData]
inner join HRMS_set_Employee on HRMS_set_Employee.PersonID = HRMS_TMSData.PersonID
and  HRMS_set_Employee.CompanyID = HRMS_TMSData.CompanyID
where HRMS_set_Employee.Status = 1

";

        const string Query_MySelectAll_WithoutPaging = @" 

SELECT HRMS_TMSData.[CompanyID], HRMS_TMSData.[PeriodID], HRMS_TMSData.[TableID], HRMS_TMSData.[TMSID],
HRMS_TMSData.[TMSDateTime] as TimeIn, HRMS_TMSData.[ShiftHeaderID], HRMS_TMSData.[PersonID],
HRMS_TMSData.[ManagementCode], HRMS_TMSData.[TerminalNo], HRMS_TMSData.[CreatedBy], HRMS_TMSData.[CreateDate],
HRMS_TMSData.[CreatedByValue],HRMS_TMSData.[UpdatedBy], HRMS_TMSData.[UpdateDate],
HRMS_TMSData.[UpdatedByValue], HRMS_TMSData.[RawData], HRMS_TMSData.[StatusCode], HRMS_TMSData.[PersonCode], HRMS_TMSData.[Reason],
HRMS_set_Employee.FullName FROM [HRMS_TMSData]
inner join HRMS_set_Employee on HRMS_set_Employee.PersonID = HRMS_TMSData.PersonID
and  HRMS_set_Employee.CompanyID = HRMS_TMSData.CompanyID
where HRMS_set_Employee.Status = 1

";
        #region Commented Query Area
        
        #endregion
        const string Query_Delete_ByKey = @"
--Delete from HRMS_TMSDATA where PersonID = @PersonID and TMSID =@TMSID and TableID = @TableID  --Comment By Ali Jaseem
";

        const string Query_SelectAllTMS = @" SELECT HRMS_set_Employee.FullName,HRMS_TMSData.* FROM [HRMS_TMSData]
inner join HRMS_set_Employee on HRMS_set_Employee.PersonID = HRMS_TMSData.PersonID
and  HRMS_set_Employee.CompanyID = HRMS_TMSData.CompanyID
WHERE (HRMS_TMSData.[CompanyID] = @CompanyID OR @CompanyID is null) AND (HRMS_TMSData.[PeriodID] = @PeriodID OR @PeriodID is null) and HRMS_TMSData.PersonID = @PersonID and HRMS_TMSData.TMSID = @TMSID and HRMS_TMSData.TableID = @TableID and HRMS_set_Employee.Status = 1";
        #endregion

        const string Query_GetData_byPersonID = @" SELECT * FROM [HRMS_TMSData] WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [PersonID] = @PersonID ";


        const string Query_Update_By_PersonID = @" UPDATE [HRMS_TMSData] set [TMSDateTime] = @TMSDateTime 
		WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [TableID] = @TableID and [PersonID] = @PersonID and [TMSID] = @TMSID ";


        const string Query_Update_By_PersonID_New = @" 
			 UPDATE [HRMS_TMSData] set [TMSDateTime] = @TMSDateTime , RawData = 'M' , TerminalNo = 0 , TMSID = 1
		WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [TableID] = @TableID and [PersonID] = @PersonID and [TMSID] = @TMSID ";


        #region TMS Attendence Delete Form Queries
        const string QueryFOrTMSDataSearching_Detail = @"
        SELECT Distinct
       ( DateName(Month,TMSDATETIME) +' '+ DateName(YEAR,TMSDATETIME) ) as TMSDateTime,
	   HRMS_Set_Employee.PersonID, HRMS_Set_Employee.FullName, HRMS_Set_Employee.EmployeeCode
	 , DENSE_RANK() OVER (ORDER BY HRMS_Set_Employee.PersonID  asc) AS Row  
        ,convert(bit,0) as IsChecked 
   FROM HRMS_TMSDATA
     Inner Join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_TMSDATA.PersonID
           and HRMS_Set_Employee.CompanyID = HRMS_TMSDATA.CompanyID
           and HRMS_Set_Employee.IsActive = 1 and HRMS_Set_Employee.Status = 1 

     Inner join HRMS_EmployeeInEmployement_Status on HRMS_EmployeeInEmployement_Status.StatusID = HRMS_Set_Employee.StatusID
        and HRMS_EmployeeInEmployement_Status.[CompanyID] = HRMS_Set_Employee.[CompanyID]
          ";

        const string QueryFOrTMSDataSearching_Detail_WithoutPaging = @"
      SELECT Distinct
       ( DateName(Month,TMSDATETIME) +' '+ DateName(YEAR,TMSDATETIME) ) as TMSDateTime,
	   HRMS_Set_Employee.PersonID, HRMS_Set_Employee.FullName, HRMS_Set_Employee.EmployeeCode
	 , DENSE_RANK() OVER (ORDER BY HRMS_Set_Employee.PersonID  asc) AS Row  
        ,convert(bit,0) as IsChecked 
   FROM HRMS_TMSDATA
     Inner Join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_TMSDATA.PersonID
           and HRMS_Set_Employee.CompanyID = HRMS_TMSDATA.CompanyID
           and HRMS_Set_Employee.IsActive = 1 and HRMS_Set_Employee.Status = 1 

     Inner join HRMS_EmployeeInEmployement_Status on HRMS_EmployeeInEmployement_Status.StatusID = HRMS_Set_Employee.StatusID
        and HRMS_EmployeeInEmployement_Status.[CompanyID] = HRMS_Set_Employee.[CompanyID]
          ";

        const string QueryFOrTMSSearching_Detail = @"
       SELECT Distinct
       ( DateName(Month,TMSDATETIME) +' '+ DateName(YEAR,TMSDATETIME) ) as TMSDateTime,
	   HRMS_Set_Employee.PersonID, HRMS_Set_Employee.FullName, HRMS_Set_Employee.EmployeeCode
	 , DENSE_RANK() OVER (ORDER BY HRMS_Set_Employee.PersonID  asc) AS Row  
        ,convert(bit,0) as IsChecked 
   FROM HRMS_TMSDATA
     Inner Join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_TMSDATA.PersonID
           and HRMS_Set_Employee.CompanyID = HRMS_TMSDATA.CompanyID
           and HRMS_Set_Employee.IsActive = 1 and HRMS_Set_Employee.Status = 1 
           
     
     Inner join HRMS_EmployeeInEmployement_Status on HRMS_EmployeeInEmployement_Status.StatusID = HRMS_Set_Employee.StatusID
        and HRMS_EmployeeInEmployement_Status.[CompanyID] = HRMS_Set_Employee.[CompanyID]
          ";

        const string Query_DeleteTMSData_HeaderSelected = @"  Delete from HRMS_TMSData WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and  datepart(month,HRMS_TMSData.TMSDateTime) = @Month and datename(year,HRMS_TMSData.TMSDateTime) = @Year and HRMS_TMSDATA.PersonID = @PersonID";
        const string Query_DeleteTMSData_AttendenceSelected_SalDel = @"  Delete from HRMS_TMSData WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and  datepart(month,HRMS_TMSData.TMSDateTime) = @Month and datename(year,HRMS_TMSData.TMSDateTime) = @Year {0}";
        #endregion


        #region ManualAttendace
        const string Query_SelectAll_ManualAttendaceWithPaging = @" SELECT     HRMS_TMSData.CompanyID, HRMS_TMSData.PeriodID, HRMS_TMSData.TableID, HRMS_TMSData.TMSID, HRMS_TMSData.TMSDateTime, 
                      HRMS_TMSData.ShiftHeaderID, HRMS_TMSData.PersonID, HRMS_TMSData.ManagementCode, HRMS_TMSData.TerminalNo, HRMS_TMSData.CreatedBy, 
                      HRMS_TMSData.CreateDate, HRMS_TMSData.CreatedByValue, HRMS_TMSData.UpdatedBy, HRMS_TMSData.UpdateDate, HRMS_TMSData.UpdatedByValue, 
                      HRMS_TMSData.RawData, HRMS_TMSData.StatusCode, HRMS_TMSData.PersonCode, HRMS_TMSData.Reason, HRMS_TMSData.Long, HRMS_TMSData.Lat, 
                      HRMS_TMSData.ipAddr, HRMS_TMS_Status.StatusID, HRMS_TMS_Status.StatusCode AS Expr1, HRMS_TMS_Status.Description, HRMS_TMS_Status.Status, 
                      HRMS_TMS_Status.Description AS Expr2, ROW_NUMBER() Over (Order by CompanyID, PeriodID, TableID) as 'Row'
                      FROM         HRMS_TMSData INNER JOIN
                      HRMS_TMS_Status ON HRMS_TMSData.StatusCode = HRMS_TMS_Status.StatusCode ";

        const string Query_SelectAll_ManualAttendaceWithoutPaging = @" SELECT     HRMS_TMSData.CompanyID, HRMS_TMSData.PeriodID, HRMS_TMSData.TableID, HRMS_TMSData.TMSID, HRMS_TMSData.TMSDateTime, 
                      HRMS_TMSData.ShiftHeaderID, HRMS_TMSData.PersonID, HRMS_TMSData.ManagementCode, HRMS_TMSData.TerminalNo, HRMS_TMSData.CreatedBy, 
                      HRMS_TMSData.CreateDate, HRMS_TMSData.CreatedByValue, HRMS_TMSData.UpdatedBy, HRMS_TMSData.UpdateDate, HRMS_TMSData.UpdatedByValue, 
                      HRMS_TMSData.RawData, HRMS_TMSData.StatusCode, HRMS_TMSData.PersonCode, HRMS_TMSData.Reason, HRMS_TMSData.Long, HRMS_TMSData.Lat, 
                      HRMS_TMSData.ipAddr, HRMS_TMS_Status.StatusID, HRMS_TMS_Status.StatusCode AS Expr1, HRMS_TMS_Status.Description, HRMS_TMS_Status.Status, 
                      HRMS_TMS_Status.Description AS Expr2
                      FROM         HRMS_TMSData INNER JOIN
                      HRMS_TMS_Status ON HRMS_TMSData.StatusCode = HRMS_TMS_Status.StatusCode";

        #endregion
        #region Methods

        public bool UpdateDataByPersonID(int CompanyID, int PeriodID, int PersonID, int TMSID, int TableID, DateTime TMSDateTime, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@TableID", TableID);
                sqlparams[3] = new SqlParameter("@TMSID", TMSID);
                sqlparams[4] = new SqlParameter("@TMSDateTime", TMSDateTime);
                sqlparams[5] = new SqlParameter("@PersonID", PersonID);

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

        public bool UpdateDataByPersonID_ForManual(int CompanyID, int PeriodID, Nullable<int> PersonID, short TMSID, int TableID, DateTime TMSDateTime)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@TableID", TableID);
                sqlparams[3] = new SqlParameter("@TMSID", TMSID);
                sqlparams[4] = new SqlParameter("@TMSDateTime", TMSDateTime);
                sqlparams[5] = new SqlParameter("@PersonID", PersonID);

              
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_By_PersonID_New, sqlparams) > 0)
                        result = true;
               
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }
        public static DataTable GetAllActiveDataTMS(Nullable<int> companyID, Nullable<int> periodID, int PersonID, int TMSID, int TableID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", PersonID);
                sqlparams[3] = new SqlParameter("@TMSID", TMSID);
                sqlparams[4] = new SqlParameter("@TableID", TableID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAllTMS, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAllTMS, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllTMS, sqlparams);

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

        public static DataTable GetDataByPersonID(int companyID, int periodID, int PersonID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TableID", PersonID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byPersonID, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byPersonID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byPersonID, sqlparams);

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

        public static bool UpdateHRMS_AttendanceRoasterByPersonCode(int CompanyID,int PeriodID,int ShiftID, DateTime Date, string EmpCode, int PersonID, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@ShiftID", ShiftID);
                sqlparams[3] = new SqlParameter("@Date", Date);
                sqlparams[4] = new SqlParameter("@EmpCode", EmpCode);
                sqlparams[5] = new SqlParameter("@PersonID", PersonID);
              
                string Query = @"update HRMS_AttendanceRoaster set ShiftID = @ShiftID where @Date between ShiftFrom and ShiftTo and Status = 1 and EmployeeCode = @EmpCode and PersonID = @PersonID 
                and CompanyID = @CompanyID and PeriodID   = @PeriodID";
                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query, sqlparams) > 0)
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


        public bool InsertWithPersonCode(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[16];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[2] = new SqlParameter("@TableID", f_TableID);
                sqlparams[3] = new SqlParameter("@TMSID", f_TMSID);
                sqlparams[4] = new SqlParameter("@TMSDateTime", f_TMSDateTime);
                sqlparams[5] = new SqlParameter("@ShiftHeaderID", f_ShiftHeaderID);
                sqlparams[6] = new SqlParameter("@PersonID", f_PersonID);
                sqlparams[7] = new SqlParameter("@ManagementCode", f_ManagementCode);
                sqlparams[8] = new SqlParameter("@TerminalNo", f_TerminalNo);
                sqlparams[9] = new SqlParameter("@CreatedBy", f_CreatedBy);
                sqlparams[10] = new SqlParameter("@CreateDate", f_CreateDate);
                sqlparams[11] = new SqlParameter("@CreatedByValue", f_CreatedByValue);
                sqlparams[12] = new SqlParameter("@RawData", f_RawData);
                sqlparams[13] = new SqlParameter("@StatusCode", f_StatusCode);
                sqlparams[14] = new SqlParameter("@PersonCode", f_PersonCode);
                if (!string.IsNullOrEmpty(f_Reason))
                    sqlparams[15] = new SqlParameter("@Reason", f_Reason);
                else
                    sqlparams[15] = new SqlParameter("@Reason", DBNull.Value);
                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_InsertWithPersonCode, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_InsertWithPersonCode, sqlparams) > 0)
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

        public static DataTable EmployeeBankAdvice(int companyID, int periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, int employeestatusID = 0)
        {
            DataTable dt = null;
            try
            {
                StringBuilder sb = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.DivisionID = " + DivisionID);
                }

                if (EmployeeTypeID > 0)
                {
                    //sb.Append(" and HRMS_Set_Employee.PersonTypeCode = " + EmployeeTypeID);
                    sb.Append(" and SET_EmployeeType.TypeID = " + EmployeeTypeID);
                }

                if (employeestatusID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + employeestatusID);
                }
                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_EmployeeBankAdvice, sb), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;

        }

        public static DataTable EmployeeLeaveAdjustment(string companyID, int PersonID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, bool isActive = true, int CurrentFiscalID = 0, Nullable<Guid> UserID = null)
        {
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            DataTable dt = null;
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[8];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@IsActive", isActive);
                sqlparams[2] = new SqlParameter("@month", Date_From.Value);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[4] = new SqlParameter("@FromDate", Date_From);
                sqlparams[5] = new SqlParameter("@sDate", Date_From);
                sqlparams[6] = new SqlParameter("@ToDate", Date_To);
                sqlparams[7] = new SqlParameter("@edate", Date_To);

                if (PersonID > 0)
                {
                    sb.Append(" AND HRMS_EmployeePayroll.PersonID = " + PersonID);

                }
                sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                companyID = "(" + companyID + ")";


                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Setup.HRMS_TMSData.Query_EmployeeLeaveAdjustment.ToString().Replace("@CompanyID", companyID), sb.ToString()), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable LeaveLogDetail(string companyID, int PersonID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, bool isActive = true, int CurrentFiscalID = 0, Nullable<Guid> UserID = null)
        {
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            DataTable dt = null;
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[8];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@IsActive", isActive);
                sqlparams[2] = new SqlParameter("@month", Date_From.Value);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[4] = new SqlParameter("@FromDate", Date_From);
                sqlparams[5] = new SqlParameter("@sDate", Date_From);
                sqlparams[6] = new SqlParameter("@ToDate", Date_To);
                sqlparams[7] = new SqlParameter("@edate", Date_To);

                if (PersonID > 0)
                {
                    sb.Append(" AND HRMS_EmployeePayroll.PersonID = " + PersonID);

                }
                sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                companyID = "(" + companyID + ")";


                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Setup.HRMS_TMSData.Query_LeaveLog.ToString().Replace("@CompanyID", companyID), sb.ToString()), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }



        public static DataTable LeaveLogSummary(string companyID, int PersonID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, bool isActive = true, int CurrentFiscalID = 0, Nullable<Guid> UserID = null)
        {
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            DataTable dt = null;
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[8];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@IsActive", isActive);
                sqlparams[2] = new SqlParameter("@month", Date_From.Value);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[4] = new SqlParameter("@FromDate", Date_From);
                sqlparams[5] = new SqlParameter("@sDate", Date_From);
                sqlparams[6] = new SqlParameter("@ToDate", Date_To);
                sqlparams[7] = new SqlParameter("@edate", Date_To);

                if (PersonID > 0)
                {
                    sb.Append(" AND HRMS_EmployeePayroll.PersonID = " + PersonID);

                }
                sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                companyID = "(" + companyID + ")";


                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Setup.HRMS_TMSData.Query_LeaveLogSummary.ToString().Replace("@CompanyID", companyID), sb.ToString()), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable EmployeeIncrementHistory(int companyID, int PersonID)
        {
            StringBuilder sb = new StringBuilder();
            DataTable dt = null;
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[1];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                //  sqlparams[1] = new SqlParameter("@FromDate", Date_From);
                //    sqlparams[1] = new SqlParameter("@ToDate", Date_To);
                if (PersonID > 0)
                {
                    sb.Append(" and HRMS_Appraisal.PersonID = " + PersonID);
                }
                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_EmployeeIncrementHistory, sb.ToString()), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable EmployeeIncrementHistoryNew(int companyID, int PersonID)
        {
            StringBuilder sb = new StringBuilder();
            DataTable dt = null;
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[1];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                
                if (PersonID > 0)
                {
                    sb.Append(" and HRMS_EmployeeIncrement.PersonID = " + PersonID);
                }
                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_EmployeeIncrementHistoryNew, sb.ToString()), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetTMS_MasterSummary(Nullable<int> companyID, Nullable<int> periodID,
            Nullable<DateTime> Date_From, Nullable<DateTime> Date_To,string PersonIDs = "")
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                if (PersonIDs != "")
                    sb.Append(" and EP.personID in (" + PersonIDs + ")");

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Setup.HRMS_TMSData.Query_TMS_MasterSummary, sb.ToString()), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetTMS_MasterSummaryForAsadRice(Nullable<int> companyID, Nullable<int> periodID,
         Nullable<DateTime> Date_From, Nullable<DateTime> Date_To)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.PayRoll.Setup.HRMS_TMSData.Query_TMS_MasterSummaryForAsadRice, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }


        public static DataTable GetTMS_MasterSummaryForDailyWages(Nullable<int> companyID, Nullable<int> periodID,
           Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, string PersonIDs = "", SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {

            #region Query

            const string QueryForDailyWages = @"

Select 
        CompanyID,
        PersonID,
        EmployeeCode,
        FullName,
        DesignationName,
        DepartmentName,
        ShiftName,
        sum(CountLeave) as Days_Leave,
        sum(CountPresent) as Days_Present,
        sum(CountAbsent) as Days_Absent,
        sum(CountLeave) + sum(CountPresent) + sum(CountAbsent) as Days_Total,
        sum(CountInLate) as Days_Late,
        sum(CountOutEarly) as Days_Early
        
        from (
	        SELECT distinct 
					fintab.TheDate,
					convert(varchar(2),TheDate,106)as NewDate,
					case when fintab.IsHoliday = 1 then fintab.DayDescription 
						 else datename(dw,fintab.TheDate) end as Day_Name,
					fintab.CompanyID,
					fintab.PersonID,
					EmployeeCode,
					FullName,
					DesignationName,
					DepartmentName,
					ShiftName,

					--case when max(LeavesCount)>0 then 'L' 
					--else
					--    case 
					--    when fintab.IsHoliday = 1 then 'P'
					--    when MIN(TMSTimeIN) is not null then 'P'
					--    when MAX(TMSTimeOUT) is not null then 'P'
					--    else 'A' end
					--end 
					--as Status,

					case when count(LR.LeaverequestID) > 0 then 
						case when MAX(PT.[Abbreviation]) is not null then MAX(PT.[Abbreviation]) else 'L' end
					else
						case 
						when fintab.IsHoliday = 1 and fintab.Description = 'Sunday' and MIN(TMSTimeIN) is null and MAX(TMSTimeOUT) is null then 'W/O'
						when fintab.Reason != '' then 'G.H'	    	       
						when MIN(TMSTimeIN) is not null then 'P'
						when MAX(TMSTimeOUT) is not null then 'P'
						else
						'A'
						end
					end as [Status],


					max(LeavesCount) as CountLeave,
             
					case
						when fintab.IsHoliday = 1 then 0 else
						case when MIN(TMSTimeIN) is not null then 1 when MAX(TMSTimeOUT) is not null then 1 else 0 end 
					end as CountPresent,
					case 
						when fintab.IsHoliday = 1 then 1 else
						Case when (MIN(TMSTimeIN) is null AND MAX(TMSTimeOUT) is null) AND max(fintab.LeavesCount)=0 
						then 1 else 0 end
					end as CountAbsent,

					case when MIN(TMSTimeIN) > STimeIN then 1 else 0 end CountInLate,
					case when MAX(TMSTimeOUT) < STimeOUT then 1 else 0 end CountOutEarly

						 from (

							SELECT SET_DateTime.Reason, SET_DateTime.Description,EP.FK_SectionID,EP.FK_LocationID,SET_DateTime.TheDate,EP.JoiningDate,
							EP.CompanyID,EP.PersonID,EP.EmployeeCode,EP.FullName,EP.FatherName,ED.DesignationName AS DesignationName,DP.DepartmentName AS DepartmentName,
							SET_DateTime.IsHoliday,SET_DateTime.[Description] as DayDescription,
							ETMS1.TMSDateTime as TMSTimeIN,ETMS2.TMSDateTime as TMSTimeOUT,SS.[Description] as ShiftName,
							(Select case when count(LR.LeaveRequestID) >0 then 1 else 0 end	        
									 from HRMS_LeaveRequest LR where 
											LR.CompanyID = EP.CompanyID and LR.FK_PersonID = EP.PersonID
											and LR.FromDate <= SET_DateTime.TheDate and LR.ToDate >= SET_DateTime.TheDate
											and LR.[Status]=1 and LR.[IsApproved]=1) as LeavesCount,

							case when ETMS1.[ManagementCode]='0' AND ETMS1.[TerminalNo]='0' AND ETMS1.[RawData]='M' then '*' else '' end DataTypeIN,
							case when ETMS2.[ManagementCode]='0' AND ETMS2.[TerminalNo]='0' AND ETMS2.[RawData]='M' then '*' else '' end DataTypeOUT,

							CONVERT(datetime,
									CONVERT(varchar(2), DATEPART(mm, SET_DateTime.TheDate)) + '-' + 
									CONVERT(varchar(2), DATEPART(dd, SET_DateTime.TheDate)) + '-' + 
									CONVERT(varchar(4), DATEPART(year, SET_DateTime.TheDate)) + ' ' + CONVERT(varchar(100),SS.[TimeIN],114) 
								,101) as ShiftTimeIN,
							CONVERT(datetime,
								CONVERT(varchar(2), DATEPART(mm, SET_DateTime.TheDate)) + '-' + 
								CONVERT(varchar(2), DATEPART(dd, SET_DateTime.TheDate)) + '-' + 
								CONVERT(varchar(4), DATEPART(year, SET_DateTime.TheDate)) + ' ' + CONVERT(varchar(100),SS.[TimeOUT],114) 
							,101) as ShiftTimeOUT,

							--CONVERT(datetime,
							--        CONVERT(varchar(2), DATEPART(mm, SET_DateTime.TheDate)) + '-' + 
							--        CONVERT(varchar(2), DATEPART(dd, SET_DateTime.TheDate)) + '-' + 
							--        CONVERT(varchar(4), DATEPART(year, SET_DateTime.TheDate)) + ' ' + CONVERT(varchar(100),SS.[TimeIN],114) 
							--    ,101) as STimeIN,
							--CONVERT(datetime,
							--    CONVERT(varchar(2), DATEPART(mm, SET_DateTime.TheDate)) + '-' + 
							--    CONVERT(varchar(2), DATEPART(dd, SET_DateTime.TheDate)) + '-' + 
							--    CONVERT(varchar(4), DATEPART(year, SET_DateTime.TheDate)) + ' ' + CONVERT(varchar(100),SS.[TimeOUT],114) 
							--,101) as STimeOUT,

							CONVERT(datetime,
									CONVERT(varchar(2), DATEPART(mm, SET_DateTime.TheDate)) + '-' + 
									CONVERT(varchar(2), DATEPART(dd, SET_DateTime.TheDate)) + '-' + 
									CONVERT(varchar(4), DATEPART(year, SET_DateTime.TheDate)) + ' ' + CONVERT(varchar(100),SS.[TimeINGrace],114) 
								,101) as STimeIN,
							CONVERT(datetime,
								CONVERT(varchar(2), DATEPART(mm, SET_DateTime.TheDate)) + '-' + 
								CONVERT(varchar(2), DATEPART(dd, SET_DateTime.TheDate)) + '-' + 
								CONVERT(varchar(4), DATEPART(year, SET_DateTime.TheDate)) + ' ' + CONVERT(varchar(100),SS.[TimeOUTGrace],114) 
							,101) as STimeOUT,

							ETMS1.TerminalNo

							from SET_DateTime 

							inner join HRMS_Set_Employee EP on (EP.CompanyID = @CompanyID OR @CompanyID is null)

							and EP.Status=1 and EP.IsActive = 1
							left JOIN SET_Designation ED ON ED.DesignationID = EP.FK_JobDesignationID AND ED.IsActive = 1 AND ED.Status = 1   
							left JOIN SET_Department DP ON DP.DepartmentID = EP.FK_DepartmentID AND DP.IsActive = 1 AND DP.Status = 1 
					        
							left JOIN HRMS_EmployeeShift ES on ( ES.FromDate <= SET_DateTime.TheDate AND ES.ToDate >= SET_DateTime.TheDate) and ES.PersonID = EP.PersonID and ES.Status=1
							left JOIN HRMS_Set_Shift SS on SS.ShiftID = ES.ShiftID and SS.Status=1

							left JOIN HRMS_TMS_Status TMS1 on TMS1.StatusID=1 and TMS1.Status=1 -- Min
							left JOIN HRMS_TMS_Status TMS2 on TMS2.StatusID=2 and TMS2.Status=1 -- Max

							left join HRMS_TMSData ETMS1 on ETMS1.CompanyID = EP.CompanyID and ETMS1.PersonID = EP.PersonID and ETMS1.StatusCode=TMS1.StatusCode and
							CONVERT(datetime,
								CONVERT(varchar(2), DATEPART(mm, ETMS1.TMSDateTime)) + '-' + 
								CONVERT(varchar(2), DATEPART(dd, ETMS1.TMSDateTime)) + '-' + 
								CONVERT(varchar(4), DATEPART(year, ETMS1.TMSDateTime)) 
							,101)
							 = SET_DateTime.TheDate

							left join HRMS_TMSData ETMS2 on ETMS2.CompanyID = EP.CompanyID and ETMS2.PersonID = EP.PersonID and ETMS2.StatusCode=TMS2.StatusCode and
							CONVERT(datetime,
								CONVERT(varchar(2), DATEPART(mm, ETMS2.TMSDateTime)) + '-' + 
								CONVERT(varchar(2), DATEPART(dd, ETMS2.TMSDateTime)) + '-' + 
								CONVERT(varchar(4), DATEPART(year, ETMS2.TMSDateTime))
							,101) 
							= SET_DateTime.TheDate
							where SET_DateTime.TheDate >= @FromDate and SET_DateTime.TheDate <= @ToDate 
                            {0}
					                 
							) as fintab
                 
             
                
                
		left join HRMS_LeaveRequest LR on 
        LR.CompanyID = fintab.CompanyID and LR.FK_PersonID = fintab.PersonID
        and LR.FromDate <= fintab.TheDate and LR.ToDate >= fintab.TheDate and LR.[Status]=1 and LR.[IsApproved]=1

        left join HRMS_PayrollType PT on 
        PT.PayrollTypeID=LR.FK_PayrollTypeID and PT.PayrollTypeCode=LR.FK_PayrollTypeCode and PT.[Status]=1
     
        left join Set_Section on Set_Section.SectionID =  fintab.FK_SectionID
        and Set_Section.Status =1 
        
        left join SET_Location on SET_Location.LocationID =  fintab.FK_LocationID
        and SET_Location.Status =1 
                
		
		
                group by 
							TheDate,
							fintab.CompanyID,
							fintab.PersonID,
							EmployeeCode,
							FullName,
							DesignationName,
							DepartmentName,
							IsHoliday,
							fintab.DayDescription,
							ShiftName,
							STimeIN,
							STimeOUT,
							fintab.TerminalNo,
							fintab.JoiningDate,
							fintab.Description ,
							fintab.Reason

        
	        ) TAB
	
	        Group by 
						CompanyID,
						PersonID,
						EmployeeCode,
						FullName,
						DesignationName,
						DepartmentName,
						ShiftName
						
	        Order by EmployeeCode

";



            #endregion

            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                if (PersonIDs != "")
                    sb.Append(" and EP.personID in (" + PersonIDs + ")");

                if (objTransaction == null)
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text,string.Format(QueryForDailyWages,sb), sqlparams).Tables[0];
                }
                else
                {
                    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(QueryForDailyWages, sb), sqlparams).Tables[0];
                }

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }
       
        public static DataTable GetAttendanceSheetTMS(string companyID, Nullable<int> periodID, Nullable<int> persontype, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> personIDFrom, Nullable<int> personIDTo, Nullable<int> ShiftIDFrom, Nullable<int> ShiftIDTo, Nullable<int> TerminalNoFrom, Nullable<int> TerminalNoTo,
      Nullable<int> DepartmentIDFrom, Nullable<int> DepartmentIDTo, bool alabbas = false)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                companyID = "(" + companyID + ")";
                if (Date_From != null && Date_To != null)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                    sb.Append("  where SET_DateTime.TheDate >= @FromDate and SET_DateTime.TheDate <= @ToDate  and EP.IsActive = 1 and ( (EP.JobEndingDate is not null and SET_DateTime.TheDate between EP.JoiningDate and EP.JobEndingDate) OR (EP.JobEndingDate is null and SET_DateTime.TheDate >= EP.JoiningDate) ) -- and SET_DateTime.TheDate >= EP.JoiningDate");//  and SET_DateTime.TheDate <= isnull(EP.jobEndingDate,getdate()) 

                    if (persontype != null)
                    {
                        sb.Append(" and EP.PersonTypeCode = " + persontype + "");
                    }
                }
                //if (Date_From != null && Date_To != null)
                //{
                //    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                //    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                //    sb.Append("  where SET_DateTime.TheDate >= @FromDate and SET_DateTime.TheDate <= @ToDate  and EP.IsActive = 1 ");
                ////and EP.PersonTypeCode =  
                //}
                //sqlparams[2] = new SqlParameter("@PersonID", personID);
                //sqlparams[3] = new SqlParameter("@ShiftID", ShiftID);
                //sqlparams[4] = new SqlParameter("@TerminalNo", TerminalNo);

                //sqlparams[7] = new SqlParameter("@DepartmentID", DepartmentID);


                //date 
                //if (Date_From != null && Date_To != null)
                //{
                //    sb.Append(" where ( cast (SET_DateTime.TheDate as date) >= " + Date_From + " and cast( SET_DateTime.TheDate  as date) <= " + Date_To + " )");
                //}


                ////person 
                if (personIDFrom != null && personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID between " + personIDFrom + " and   " + personIDTo + " )");
                }
                else if (personIDFrom != null)
                {
                    sb1.Append(" and ( EP.PersonID = " + personIDFrom + " )");
                }
                else if (personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID  = " + personIDTo + " )");
                }



                ////department
                if (DepartmentIDFrom != null && DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID between " + DepartmentIDFrom + " and " + DepartmentIDTo + ") ");

                }
                else if (DepartmentIDFrom != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID =  " + DepartmentIDFrom + " ) ");

                }
                else if (DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID  =  " + DepartmentIDTo + ") ");
                }

                ////terminal 
                if (TerminalNoFrom != null && TerminalNoTo != null)
                {
                    sb2.Append(" where (TerminalNo between " + TerminalNoFrom + " and " + TerminalNoTo + ") ");
                }
                else if (TerminalNoFrom != null)
                {
                    sb2.Append(" where (TerminalNo = " + TerminalNoFrom + ") ");
                }
                else if (TerminalNoTo != null)
                {
                    sb2.Append(" where (TerminalNo  = " + TerminalNoTo + ") ");
                }
                ////shift
                //if (ShiftIDFrom != null && ShiftIDTo != null)
                //{


                //}
                //else if (ShiftIDFrom != null)
                //{
                //}
                //else if (ShiftIDTo != null)
                //{

                //}
                if (alabbas == true)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Transaction.HRMS_Appraisal.Query_TMS_Alabbas, sb, sb1, sb2), sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Transaction.HRMS_Appraisal.Query_DataTMS, sb, sb1, sb2).Replace("@CompanyID", companyID), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }


        public static DataTable GetTMS_Master2(string companyID, Nullable<int> periodID, Nullable<int> persontype, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> personIDFrom, Nullable<int> personIDTo, Nullable<int> ShiftIDFrom, Nullable<int> ShiftIDTo, Nullable<int> TerminalNoFrom, Nullable<int> TerminalNoTo,
          Nullable<int> DepartmentIDFrom, Nullable<int> DepartmentIDTo, Nullable<int> EmpStatusTypeID = null, Nullable<Guid> UserID = null, int LocationID = 0, bool alabbas = false)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            StringBuilder sb3 = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);


                if (Date_From != null && Date_To != null)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                    sb.Append("  where SET_DateTime.TheDate >= @FromDate and SET_DateTime.TheDate <= @ToDate  and EP.IsActive = 1 and ( (EP.JobEndingDate is not null and SET_DateTime.TheDate between EP.JoiningDate and EP.JobEndingDate) OR (EP.JobEndingDate is null and SET_DateTime.TheDate >= EP.JoiningDate) ) -- and SET_DateTime.TheDate >= EP.JoiningDate");//  and SET_DateTime.TheDate <= isnull(EP.jobEndingDate,getdate()) 

                    if (persontype != null)
                    {
                        sb.Append(" and EP.PersonTypeCode = " + persontype + "");
                    }
                }
                //if (Date_From != null && Date_To != null)
                //{
                //    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                //    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                //    sb.Append("  where SET_DateTime.TheDate >= @FromDate and SET_DateTime.TheDate <= @ToDate  and EP.IsActive = 1 ");
                ////and EP.PersonTypeCode =  
                //}
                //sqlparams[2] = new SqlParameter("@PersonID", personID);
                //sqlparams[3] = new SqlParameter("@ShiftID", ShiftID);
                //sqlparams[4] = new SqlParameter("@TerminalNo", TerminalNo);

                //sqlparams[7] = new SqlParameter("@DepartmentID", DepartmentID);


                //date 
                //if (Date_From != null && Date_To != null)
                //{
                //    sb.Append(" where ( cast (SET_DateTime.TheDate as date) >= " + Date_From + " and cast( SET_DateTime.TheDate  as date) <= " + Date_To + " )");
                //}


                ////person 
                if (personIDFrom != null && personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID between " + personIDFrom + " and   " + personIDTo + " )");
                }
                else if (personIDFrom != null)
                {
                    sb1.Append(" and ( EP.PersonID = " + personIDFrom + " )");
                }
                else if (personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID  = " + personIDTo + " )");
                }

                if (EmpStatusTypeID > 0)
                {
                    sb3.Append(" and EP.StatusID = " + EmpStatusTypeID);
                }
                else
                {
                    sb3.Append(" and EP.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where Convert(varchar(50),UserID) = '" + UserID + "'" + ")");

                }

                if (LocationID > 0)
                {
                    sb3.Append(" and EP.FK_LocationID = " + LocationID);
                } 
                ////department
                if (DepartmentIDFrom != null && DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID between " + DepartmentIDFrom + " and " + DepartmentIDTo + ") ");

                }
                else if (DepartmentIDFrom != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID =  " + DepartmentIDFrom + " ) ");

                }
                else if (DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID  =  " + DepartmentIDTo + ") ");
                }

                ////terminal 
                if (TerminalNoFrom != null && TerminalNoTo != null)
                {
                    sb2.Append(" where (TerminalNo between " + TerminalNoFrom + " and " + TerminalNoTo + ") ");
                }
                else if (TerminalNoFrom != null)
                {
                    sb2.Append(" where (TerminalNo = " + TerminalNoFrom + ") ");
                }
                else if (TerminalNoTo != null)
                {
                    sb2.Append(" where (TerminalNo  = " + TerminalNoTo + ") ");
                }
                ////shift
                //if (ShiftIDFrom != null && ShiftIDTo != null)
                //{


                //}
                //else if (ShiftIDFrom != null)
                //{
                //}
                //else if (ShiftIDTo != null)
                //{

                //}

                companyID = "(" + companyID + ")";

                if (alabbas == true)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Transaction.HRMS_Appraisal.Query_TMS_Alabbas.ToString().Replace("@CompanyID", companyID), sb, sb1, sb2), sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Setup.HRMS_TMSData.Query_TMS_Master.ToString().Replace("@CompanyID", companyID), sb, sb1, sb2, sb3), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetdataforAttendanceSheetCountWise(string companyID, Nullable<int> periodID, Nullable<int> persontype, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> personIDFrom, Nullable<int> personIDTo, Nullable<int> ShiftIDFrom, Nullable<int> ShiftIDTo, Nullable<int> TerminalNoFrom, Nullable<int> TerminalNoTo,
          Nullable<int> DepartmentIDFrom, Nullable<int> DepartmentIDTo, Nullable<int> EmpStatusTypeID = null, Nullable<Guid> UserID = null, int LocationID = 0)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            StringBuilder sb3 = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);


                if (Date_From != null && Date_To != null)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                    sb.Append("  where SET_DateTime.TheDate between @FromDate and  @ToDate  and EP.IsActive = 1 and ( (EP.JobEndingDate is not null and SET_DateTime.TheDate between EP.JoiningDate and EP.JobEndingDate) OR (EP.JobEndingDate is null and SET_DateTime.TheDate >= EP.JoiningDate) ) -- and SET_DateTime.TheDate >= EP.JoiningDate");//  and SET_DateTime.TheDate <= isnull(EP.jobEndingDate,getdate()) 

                    if (persontype != null)
                    {
                        sb.Append(" and EP.PersonTypeCode = " + persontype + "");
                    }
                }
                //if (Date_From != null && Date_To != null)
                //{
                //    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                //    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                //    sb.Append("  where SET_DateTime.TheDate >= @FromDate and SET_DateTime.TheDate <= @ToDate  and EP.IsActive = 1 ");
                ////and EP.PersonTypeCode =  
                //}
                //sqlparams[2] = new SqlParameter("@PersonID", personID);
                //sqlparams[3] = new SqlParameter("@ShiftID", ShiftID);
                //sqlparams[4] = new SqlParameter("@TerminalNo", TerminalNo);

                //sqlparams[7] = new SqlParameter("@DepartmentID", DepartmentID);


                //date 
                //if (Date_From != null && Date_To != null)
                //{
                //    sb.Append(" where ( cast (SET_DateTime.TheDate as date) >= " + Date_From + " and cast( SET_DateTime.TheDate  as date) <= " + Date_To + " )");
                //}


                ////person 
                if (personIDFrom != null && personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID between " + personIDFrom + " and   " + personIDTo + " )");
                }
                else if (personIDFrom != null)
                {
                    sb1.Append(" and ( EP.PersonID = " + personIDFrom + " )");
                }
                else if (personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID  = " + personIDTo + " )");
                }

                if (EmpStatusTypeID > 0)
                {
                    sb3.Append(" and EP.StatusID = " + EmpStatusTypeID);
                }
                else
                {
                    sb3.Append(" and EP.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where Convert(varchar(50),UserID) = '" + UserID + "'" + ")");

                }

                if (LocationID > 0)
                {
                    sb3.Append(" and EP.FK_LocationID = " + LocationID);
                }
                ////department
                if (DepartmentIDFrom != null && DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID between " + DepartmentIDFrom + " and " + DepartmentIDTo + ") ");

                }
                else if (DepartmentIDFrom != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID =  " + DepartmentIDFrom + " ) ");

                }
                else if (DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID  =  " + DepartmentIDTo + ") ");
                }

                ////terminal 
                if (TerminalNoFrom != null && TerminalNoTo != null)
                {
                    sb2.Append(" where (TerminalNo between " + TerminalNoFrom + " and " + TerminalNoTo + ") ");
                }
                else if (TerminalNoFrom != null)
                {
                    sb2.Append(" where (TerminalNo = " + TerminalNoFrom + ") ");
                }
                else if (TerminalNoTo != null)
                {
                    sb2.Append(" where (TerminalNo  = " + TerminalNoTo + ") ");
                }
                ////shift
                //if (ShiftIDFrom != null && ShiftIDTo != null)
                //{


                //}
                //else if (ShiftIDFrom != null)
                //{
                //}
                //else if (ShiftIDTo != null)
                //{

                //}

                companyID = "(" + companyID + ")";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Transaction.HRMS_Appraisal.Query_AttendSheetCountWiseSummary.ToString().Replace("@CompanyID", companyID), sb, sb1, sb2, sb3), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }


        public static DataTable GetTMS_Sheikhoo(String companyID, Nullable<int> periodID, Nullable<int> persontype, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> personIDFrom, Nullable<int> personIDTo, Nullable<int> ShiftIDFrom, Nullable<int> ShiftIDTo, Nullable<int> TerminalNoFrom, Nullable<int> TerminalNoTo,
      Nullable<int> DepartmentIDFrom, Nullable<int> DepartmentIDTo, Nullable<int> EmpStatusTypeID = null, int LocationID = 0, bool alabbas = false)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            StringBuilder sb3 = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);


                if (Date_From != null && Date_To != null)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                    //sb.Append("  where SET_DateTime.TheDate >= @FromDate and SET_DateTime.TheDate <= @ToDate  and EP.IsActive = 1 and ( (EP.JobEndingDate is not null and SET_DateTime.TheDate between EP.JoiningDate and EP.JobEndingDate) OR (EP.JobEndingDate is null and SET_DateTime.TheDate >= EP.JoiningDate) ) -- and SET_DateTime.TheDate >= EP.JoiningDate");//  and SET_DateTime.TheDate <= isnull(EP.jobEndingDate,getdate()) 

                    if (persontype != null)
                    {
                        sb.Append(" and hrms_set_employee.PersonID = " + persontype + "");
                    }
                }
                //if (Date_From != null && Date_To != null)
                //{
                //    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                //    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                //    sb.Append("  where SET_DateTime.TheDate >= @FromDate and SET_DateTime.TheDate <= @ToDate  and EP.IsActive = 1 ");
                ////and EP.PersonTypeCode =  
                //}
                //sqlparams[2] = new SqlParameter("@PersonID", personID);
                //sqlparams[3] = new SqlParameter("@ShiftID", ShiftID);
                //sqlparams[4] = new SqlParameter("@TerminalNo", TerminalNo);

                //sqlparams[7] = new SqlParameter("@DepartmentID", DepartmentID);


                //date 
                //if (Date_From != null && Date_To != null)
                //{
                //    sb.Append(" where ( cast (SET_DateTime.TheDate as date) >= " + Date_From + " and cast( SET_DateTime.TheDate  as date) <= " + Date_To + " )");
                //}


                ////person 
                if (personIDFrom != null && personIDTo != null)
                {
                    sb1.Append(" and ( hrms_set_employee.PersonID between " + personIDFrom + " and   " + personIDTo + " )");
                }
                else if (personIDFrom != null)
                {
                    sb1.Append(" and ( hrms_set_employee.PersonID = " + personIDFrom + " )");
                }
                else if (personIDTo != null)
                {
                    sb1.Append(" and ( hrms_set_employee.PersonID  = " + personIDTo + " )");
                }



                ////department
                if (DepartmentIDFrom != null && DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID between " + DepartmentIDFrom + " and " + DepartmentIDTo + ") ");

                }
                else if (DepartmentIDFrom != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID =  " + DepartmentIDFrom + " ) ");

                }
                else if (DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID  =  " + DepartmentIDTo + ") ");
                }

                ////terminal 
                if (TerminalNoFrom != null && TerminalNoTo != null)
                {
                    sb2.Append(" where (TerminalNo between " + TerminalNoFrom + " and " + TerminalNoTo + ") ");
                }
                else if (TerminalNoFrom != null)
                {
                    sb2.Append(" where (TerminalNo = " + TerminalNoFrom + ") ");
                }
                else if (TerminalNoTo != null)
                {
                    sb2.Append(" where (TerminalNo  = " + TerminalNoTo + ") ");
                }
                ////shift
                //if (ShiftIDFrom != null && ShiftIDTo != null)
                //{


                //}
                //else if (ShiftIDFrom != null)
                //{
                //}
                //else if (ShiftIDTo != null)
                //{

                //}
                if (EmpStatusTypeID > 0)
                {
                    sb3.Append(" and hrms_set_employee.StatusID = " + EmpStatusTypeID);
                }



                if (LocationID > 0)
                {
                    sb3.Append(" and hrms_set_employee.FK_LocationID = " + LocationID);
                } 

                companyID = "(" + companyID + ")";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Setup.Tax.TMS_Sheikhoo.Replace("@CompanyID", companyID), sb1, sb3), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetTMSAttendanceSheetCountWiseSummary(String companyID, Nullable<int> periodID, Nullable<int> persontype, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> personIDFrom, Nullable<int> personIDTo, Nullable<int> ShiftIDFrom, Nullable<int> ShiftIDTo, Nullable<int> TerminalNoFrom, Nullable<int> TerminalNoTo,
        Nullable<int> DepartmentIDFrom, Nullable<int> DepartmentIDTo, Nullable<int> EmpStatusTypeID = null, int LocationID = 0, bool alabbas = false)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            StringBuilder sb3 = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);


                if (Date_From != null && Date_To != null)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                    //sb.Append("  where SET_DateTime.TheDate >= @FromDate and SET_DateTime.TheDate <= @ToDate  and EP.IsActive = 1 and ( (EP.JobEndingDate is not null and SET_DateTime.TheDate between EP.JoiningDate and EP.JobEndingDate) OR (EP.JobEndingDate is null and SET_DateTime.TheDate >= EP.JoiningDate) ) -- and SET_DateTime.TheDate >= EP.JoiningDate");//  and SET_DateTime.TheDate <= isnull(EP.jobEndingDate,getdate()) 

                    if (persontype != null)
                    {
                        sb.Append(" and EP.PersonID = " + persontype + "");
                    }
                }
                //if (Date_From != null && Date_To != null)
                //{
                //    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                //    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                //    sb.Append("  where SET_DateTime.TheDate >= @FromDate and SET_DateTime.TheDate <= @ToDate  and EP.IsActive = 1 ");
                ////and EP.PersonTypeCode =  
                //}
                //sqlparams[2] = new SqlParameter("@PersonID", personID);
                //sqlparams[3] = new SqlParameter("@ShiftID", ShiftID);
                //sqlparams[4] = new SqlParameter("@TerminalNo", TerminalNo);

                //sqlparams[7] = new SqlParameter("@DepartmentID", DepartmentID);


                //date 
                //if (Date_From != null && Date_To != null)
                //{
                //    sb.Append(" where ( cast (SET_DateTime.TheDate as date) >= " + Date_From + " and cast( SET_DateTime.TheDate  as date) <= " + Date_To + " )");
                //}


                ////person 
                if (personIDFrom != null && personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID between " + personIDFrom + " and   " + personIDTo + " )");
                }
                else if (personIDFrom != null)
                {
                    sb1.Append(" and ( EP.PersonID = " + personIDFrom + " )");
                }
                else if (personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID  = " + personIDTo + " )");
                }



                ////department
                if (DepartmentIDFrom != null && DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID between " + DepartmentIDFrom + " and " + DepartmentIDTo + ") ");

                }
                else if (DepartmentIDFrom != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID =  " + DepartmentIDFrom + " ) ");

                }
                else if (DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID  =  " + DepartmentIDTo + ") ");
                }

                ////terminal 
                if (TerminalNoFrom != null && TerminalNoTo != null)
                {
                    sb2.Append(" where (TerminalNo between " + TerminalNoFrom + " and " + TerminalNoTo + ") ");
                }
                else if (TerminalNoFrom != null)
                {
                    sb2.Append(" where (TerminalNo = " + TerminalNoFrom + ") ");
                }
                else if (TerminalNoTo != null)
                {
                    sb2.Append(" where (TerminalNo  = " + TerminalNoTo + ") ");
                }
                ////shift
                //if (ShiftIDFrom != null && ShiftIDTo != null)
                //{


                //}
                //else if (ShiftIDFrom != null)
                //{
                //}
                //else if (ShiftIDTo != null)
                //{

                //}
                if (EmpStatusTypeID > 0)
                {
                    sb3.Append(" and EP.StatusID = " + EmpStatusTypeID);
                }



                if (LocationID > 0)
                {
                    sb3.Append(" and EP.FK_LocationID = " + LocationID);
                }

                companyID = "(" + companyID + ")";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Setup.Tax.TMS_CountAttendenceReport.Replace("@CompanyID", companyID), sb1, sb3), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }



        public static DataTable GetTMS_Master(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> personIDFrom, Nullable<int> personIDTo, Nullable<int> ShiftIDFrom, Nullable<int> ShiftIDTo, Nullable<int> TerminalNoFrom, Nullable<int> TerminalNoTo,
            Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> DepartmentIDFrom, Nullable<int> DepartmentIDTo)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (Date_From != null && Date_To != null)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                    sb.Append("  where SET_DateTime.TheDate >= @FromDate and SET_DateTime.TheDate <= @ToDate  and EP.IsActive = 1 ");
                    //and EP.PersonTypeCode =  
                }
                //sqlparams[2] = new SqlParameter("@PersonID", personID);
                //sqlparams[3] = new SqlParameter("@ShiftID", ShiftID);
                //sqlparams[4] = new SqlParameter("@TerminalNo", TerminalNo);

                //sqlparams[7] = new SqlParameter("@DepartmentID", DepartmentID);


                //date 
                //if (Date_From != null && Date_To != null)
                //{
                //    sb.Append(" where ( cast (SET_DateTime.TheDate as date) >= " + Date_From + " and cast( SET_DateTime.TheDate  as date) <= " + Date_To + " )");
                //}


                ////person 
                if (personIDFrom != null && personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID between " + personIDFrom + " and   " + personIDTo + " )");
                }
                else if (personIDFrom != null)
                {
                    sb1.Append(" and ( EP.PersonID = " + personIDFrom + " )");
                }
                else if (personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID  = " + personIDTo + " )");
                }



                ////department
                if (DepartmentIDFrom != null && DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID between " + DepartmentIDFrom + " and " + DepartmentIDTo + ") ");

                }
                else if (DepartmentIDFrom != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID =  " + DepartmentIDFrom + " ) ");

                }
                else if (DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID  =  " + DepartmentIDTo + ") ");
                }

                ////terminal 
                if (TerminalNoFrom != null && TerminalNoTo != null)
                {
                    sb2.Append(" where (TerminalNo between " + TerminalNoFrom + " and " + TerminalNoTo + ") ");
                }
                else if (TerminalNoFrom != null)
                {
                    sb2.Append(" where (TerminalNo = " + TerminalNoFrom + ") ");
                }
                else if (TerminalNoTo != null)
                {
                    sb2.Append(" where (TerminalNo  = " + TerminalNoTo + ") ");
                }
                ////shift
                //if (ShiftIDFrom != null && ShiftIDTo != null)
                //{


                //}
                //else if (ShiftIDFrom != null)
                //{
                //}
                //else if (ShiftIDTo != null)
                //{

                //}

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Setup.HRMS_TMSData.Query_TMS_Master, sb, sb1, sb2), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }




        public static DataTable GetTMS_MasterForRegister(Nullable<int> persontype, string companyID, Nullable<int> periodID,int CurrentFiscalID, Nullable<int> personIDFrom, Nullable<int> personIDTo, Nullable<int> ShiftIDFrom, Nullable<int> ShiftIDTo, Nullable<int> TerminalNoFrom, Nullable<int> TerminalNoTo,
            Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> DepartmentIDFrom, Nullable<int> DepartmentIDTo)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            StringBuilder sb3 = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                if (Date_From != null && Date_To != null)
                {
                    sqlparams[3] = new SqlParameter("@FromDate", Date_From);
                    sqlparams[4] = new SqlParameter("@ToDate", Date_To);
                    sb.Append("  where SET_DateTime.TheDate >= @FromDate and SET_DateTime.TheDate <= @ToDate  and EP.IsActive = 1 and ( (EP.JobEndingDate is not null and SET_DateTime.TheDate between EP.JoiningDate and EP.JobEndingDate) OR (EP.JobEndingDate is null and SET_DateTime.TheDate >= EP.JoiningDate) ) --and SET_DateTime.TheDate >= EP.JoiningDate "); //and SET_DateTime.TheDate <= isnull(EP.jobEndingDate,getdate()) 

                    if (persontype != null)
                    {
                        sb.Append(" and EP.PersonTypeCode = " + persontype + "");
                    }
                }
                //sqlparams[2] = new SqlParameter("@PersonID", personID);
                //sqlparams[3] = new SqlParameter("@ShiftID", ShiftID);
                //sqlparams[4] = new SqlParameter("@TerminalNo", TerminalNo);

                //sqlparams[7] = new SqlParameter("@DepartmentID", DepartmentID);


                //date 
                //if (Date_From != null && Date_To != null)
                //{
                //    sb.Append(" where ( cast (SET_DateTime.TheDate as date) >= " + Date_From + " and cast( SET_DateTime.TheDate  as date) <= " + Date_To + " )");
                //}


                ////person 
                //if (personIDFrom != null && personIDTo != null)
                //{
                //    sb1.Append(" and ( EP.PersonID between " + personIDFrom + " and   " + personIDTo + " )");
                //}
                //else if (personIDFrom != null)
                //{
                //    sb1.Append(" and ( EP.PersonID = " + personIDFrom + " )");
                //}
                //else if (personIDTo != null)
                //{
                //    sb1.Append(" and ( EP.PersonID  = " + personIDTo + " )");
                //}
                if (personIDFrom != null && personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID between " + personIDFrom + " and   " + personIDTo + " )");
                    sb3.Append("and ( HRMS_LeaveRequest.Fk_PersonID between " + personIDFrom + " and   " + personIDTo + " )");
                }
                else if (personIDFrom != null)
                {
                    sb1.Append(" and ( EP.PersonID = " + personIDFrom + " )");
                    sb3.Append(" and ( HRMS_LeaveRequest.Fk_PersonID = " + personIDFrom + " )");
                }
                else if (personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID  = " + personIDTo + " )");
                    sb3.Append(" and ( HRMS_LeaveRequest.Fk_PersonID  = " + personIDTo + " )");
                }


                ////department
                if (DepartmentIDFrom != null && DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID between " + DepartmentIDFrom + " and " + DepartmentIDTo + ") ");

                }
                else if (DepartmentIDFrom != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID =  " + DepartmentIDFrom + " ) ");

                }
                else if (DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID  =  " + DepartmentIDTo + ") ");
                }

                ////terminal 
                if (TerminalNoFrom != null && TerminalNoTo != null)
                {
                    sb2.Append(" where (TerminalNo between " + TerminalNoFrom + " and " + TerminalNoTo + ") ");
                }
                else if (TerminalNoFrom != null)
                {
                    sb2.Append(" where (TerminalNo = " + TerminalNoFrom + ") ");
                }
                else if (TerminalNoTo != null)
                {
                    sb2.Append(" where (TerminalNo  = " + TerminalNoTo + ") ");
                }
                ////shift
                //if (ShiftIDFrom != null && ShiftIDTo != null)
                //{


                //}
                //else if (ShiftIDFrom != null)
                //{
                //}
                //else if (ShiftIDTo != null)
                //{

                //}

                companyID = "(" + companyID + ")";

               // dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Setup.HRMS_TMSData.Query_TMS_Master, sb, sb1, sb2), sqlparams).Tables[0];
                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Transaction.HRMS_Appraisal.Query_TMSAttendanceRegister.ToString().Replace("@CompanyID", companyID), sb, sb1, sb2, sb3), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }


        public static DataSet GetTMS_MasterAttendenceTwo(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> persontype, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> personIDFrom, Nullable<int> personIDTo, Nullable<int> ShiftIDFrom, Nullable<int> ShiftIDTo, Nullable<int> TerminalNoFrom, Nullable<int> TerminalNoTo,
     Nullable<int> DepartmentIDFrom, Nullable<int> DepartmentIDTo, Nullable<int> SectionID, Nullable<int> LocationFrom, Nullable<int> LocationTo, bool OutDoorDuty = false, Nullable<int> AttStatus = null)
        {
            DataTable dt = null;
            DataTable dt_Temp = null;
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            StringBuilder sb3 = new StringBuilder();
            StringBuilder sb4 = new StringBuilder();
            StringBuilder sb5 = new StringBuilder();
            StringBuilder sb6 = new StringBuilder();
            StringBuilder sb7 = new StringBuilder();
            //StringBuilder sb3 = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);


                if (Date_From != null && Date_To != null)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                    sb.Append("  where SET_DateTime.TheDate >= @FromDate and SET_DateTime.TheDate <= @ToDate  and EP.IsActive = 1   and SET_DateTime.TheDate >= EP.JoiningDate  and SET_DateTime.TheDate <= isnull(EP.jobEndingDate,getdate()) ");


                }
                else if (Date_From != null)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                    sb.Append("  where SET_DateTime.TheDate >= @FromDate and EP.IsActive = 1   and SET_DateTime.TheDate >= EP.JoiningDate  and SET_DateTime.TheDate <= isnull(EP.jobEndingDate,getdate()) ");
                }
                else if (Date_To != null)
                {
                    sqlparams[2] = new SqlParameter("@ToDate", Date_To);
                    sb.Append("  where SET_DateTime.TheDate <= @ToDate  and EP.IsActive = 1   and SET_DateTime.TheDate >= EP.JoiningDate  and SET_DateTime.TheDate <= isnull(EP.jobEndingDate,getdate()) ");

                }

                //if (Date_From != null && Date_To != null)
                //{
                //    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                //    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                //    sb.Append("  where SET_DateTime.TheDate >= @FromDate and SET_DateTime.TheDate <= @ToDate  and EP.IsActive = 1 ");
                ////and EP.PersonTypeCode =  
                //}
                //sqlparams[2] = new SqlParameter("@PersonID", personID);
                //sqlparams[3] = new SqlParameter("@ShiftID", ShiftID);
                sqlparams[4] = new SqlParameter("@OutDoorDuty", OutDoorDuty);

                //sqlparams[7] = new SqlParameter("@DepartmentID", DepartmentID);


                //date 
                //if (Date_From != null && Date_To != null)
                //{
                //    sb.Append(" where ( cast (SET_DateTime.TheDate as date) >= " + Date_From + " and cast( SET_DateTime.TheDate  as date) <= " + Date_To + " )");
                //}


                ////person 
                if (personIDFrom != null && personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID between " + personIDFrom + " and   " + personIDTo + " )");
                }
                else if (personIDFrom != null)
                {
                    sb1.Append(" and ( EP.PersonID = " + personIDFrom + " )");
                }
                else if (personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID  = " + personIDTo + " )");
                }

                ////department
                if (DepartmentIDFrom != null && DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID between " + DepartmentIDFrom + " and " + DepartmentIDTo + ") ");

                }
                else if (DepartmentIDFrom != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID =  " + DepartmentIDFrom + " ) ");

                }
                else if (DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID  =  " + DepartmentIDTo + ") ");
                }
                if (SectionID != null)
                {
                    sb1.Append("and (EP.FK_SectionID=" + SectionID + ")");

                }
                ////location

                if (LocationFrom != null && LocationTo != null)
                {
                    sb1.Append(" and (EP.FK_LocationID between " + LocationFrom + " and " + LocationTo + ") ");

                }
                else if (LocationFrom != null)
                {
                    sb1.Append(" and (EP.FK_LocationID =  " + LocationFrom + " ) ");

                }
                else if (LocationTo != null)
                {
                    sb1.Append(" and (EP.FK_LocationID  =  " + LocationTo + ") ");
                }

                ////terminal 
                if (TerminalNoFrom != null && TerminalNoTo != null)
                {
                    sb2.Append(" and (TerminalNo between " + TerminalNoFrom + " and " + TerminalNoTo + ") ");
                }
                else if (TerminalNoFrom != null)
                {
                    sb2.Append(" and (TerminalNo = " + TerminalNoFrom + ") ");
                }
                else if (TerminalNoTo != null)
                {
                    sb2.Append(" and (TerminalNo  = " + TerminalNoTo + ") ");
                }
                ////shift
                if (ShiftIDFrom != null && ShiftIDTo != null)
                {
                    sb3.Append(" and (ABC.ShiftID between " + ShiftIDFrom + " and " + ShiftIDTo + ") ");

                }
                else if (ShiftIDFrom != null)
                {
                    sb3.Append(" and (ABC.ShiftID = " + ShiftIDFrom + ") ");
                }
                else if (ShiftIDTo != null)
                {
                    sb3.Append(" and (ABC.ShiftID = " + ShiftIDTo + ") ");
                }
                if (persontype != null)
                {
                    sb5.Append(" and EP.PersonTypeCode = " + persontype + "");
                }
                if (AttStatus > 0)
                {
                    sb6.Append("and Abbreviation = 'F'");
                    //sb7.Append("where PT.PayrolltypeCode = 4");
                    sb7.Append("where PT.Abbreviation = 'F' ");
                }

                // dt_Temp = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(attTwo, sb, sb1, sb2, sb3, sb4, sb5), sqlparams).Tables[0];
               // ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(attTwo2, sb, sb1, sb2, sb3, sb4, sb5, sb6, sb7), sqlparams);

                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Setup.HRMS_TMSData.Query_TMS_Attendence_two, sb, sb1, sb2, sb3, sb4, sb5, sb6, sb7), sqlparams);

                //ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Transaction.HRMS_Appraisal.Query_TMS_AlAbbasWithRoaster, sb, sb1, sb2, sb3, sb4), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                {
                    ds.Tables[0].TableName = "tblEmpTimeSheet";
                    ds.Tables[1].TableName = "Table1";

                    int total_rows = ds.Tables[0].Rows.Count;
                    int j = 1;
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        if (total_rows == j)
                            break;
                        if (ds.Tables[0].Rows[i]["TheDate"].ToString() == ds.Tables[0].Rows[i + 1]["TheDate"].ToString())
                        {
                            if (ds.Tables[0].Rows[i]["FullName"].ToString() == ds.Tables[0].Rows[i + 1]["FullName"].ToString())
                            {
                                ds.Tables[0].Rows[i].Delete();
                            }
                        }
                        j++;

                    }
                    ds.Tables[0].AcceptChanges();
                }

                #region Filters

                // var Rows = dt_Temp.Select("CountInLate=1");
                // dt = ds.Tables[0].Select("").CopyToDataTable();
                #endregion
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return ds;
        }


        public static DataTable GetTMS_MasterForDailyAttendance(string companyID, Nullable<int> periodID, Nullable<int> personIDFrom, Nullable<int> personIDTo, Nullable<int> ShiftIDFrom, Nullable<int> ShiftIDTo, Nullable<int> TerminalNoFrom, Nullable<int> TerminalNoTo,
          Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> DepartmentIDFrom, Nullable<int> DepartmentIDTo, Nullable<int> PersonType,Nullable<int> EmpStatusTypeID = null, int LocationID = 0,bool summary = false, bool alabbas = false)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            StringBuilder sb3 = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (Date_From != null && Date_To != null)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                    sb.Append("  where SET_DateTime.TheDate >= @FromDate and SET_DateTime.TheDate <= @ToDate  and EP.IsActive = 1   and SET_DateTime.TheDate >= EP.JoiningDate  and SET_DateTime.TheDate <= isnull(EP.jobEndingDate,getdate())");

                    if (PersonType != null)
                    {
                        sb.Append(" and EP.PersonTypeCode = " + PersonType + "");
                    }
                }
                //sqlparams[2] = new SqlParameter("@PersonID", personID);
                //sqlparams[3] = new SqlParameter("@ShiftID", ShiftID);
                //sqlparams[4] = new SqlParameter("@TerminalNo", TerminalNo);

                //sqlparams[7] = new SqlParameter("@DepartmentID", DepartmentID);


                //date 
                //if (Date_From != null && Date_To != null)
                //{
                //    sb.Append(" where ( cast (SET_DateTime.TheDate as date) >= " + Date_From + " and cast( SET_DateTime.TheDate  as date) <= " + Date_To + " )");
                //}


                ////person 
                if (personIDFrom != null && personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID between " + personIDFrom + " and   " + personIDTo + " )");
                }
                else if (personIDFrom != null)
                {
                    sb1.Append(" and ( EP.PersonID = " + personIDFrom + " )");
                }
                else if (personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID  = " + personIDTo + " )");
                }



                ////department
                if (DepartmentIDFrom != null && DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID between " + DepartmentIDFrom + " and " + DepartmentIDTo + ") ");

                }
                else if (DepartmentIDFrom != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID =  " + DepartmentIDFrom + " ) ");

                }
                else if (DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID  =  " + DepartmentIDTo + ") ");
                }

                ////terminal 
                if (TerminalNoFrom != null && TerminalNoTo != null)
                {
                    sb2.Append(" where (TerminalNo between " + TerminalNoFrom + " and " + TerminalNoTo + ") ");
                }
                else if (TerminalNoFrom != null)
                {
                    sb2.Append(" where (TerminalNo = " + TerminalNoFrom + ") ");
                }
                else if (TerminalNoTo != null)
                {
                    sb2.Append(" where (TerminalNo  = " + TerminalNoTo + ") ");
                }

                if (EmpStatusTypeID > 0)
                {
                    sb3.Append(" and EP.StatusID = " + EmpStatusTypeID);
                }



                if (LocationID > 0)
                {
                    sb3.Append(" and EP.FK_LocationID = " + LocationID);
                } 
                ////shift
                //if (ShiftIDFrom != null && ShiftIDTo != null)
                //{
                //


                //}
                //else if (ShiftIDFrom != null)
                //{
                //}
                //else if (ShiftIDTo != null)
                //{

                //}

                companyID = "(" + companyID + ")";

                if (summary)
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Transaction.HRMS_Appraisal.Query_TMS_MonthwiseAttendance.ToString().Replace("@CompanyID", companyID), sb.ToString(), sb1.ToString(), sb2.ToString(), sb3.ToString()), sqlparams).Tables[0];
                }
                else
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Setup.HRMS_TMSData.Query_TMS_Master.ToString().Replace("@CompanyID", companyID), sb.ToString(), sb1.ToString(), sb2.ToString(), sb3.ToString()), sqlparams).Tables[0];
                }

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetAttendance_ForManualRegister(Nullable<int> periodID, Nullable<int> personIDFrom, Nullable<int> personIDTo, 
            DateTime? Date_From, DateTime? Date_To, Nullable<int> DepartmentIDFrom, Nullable<int> EmpStatusTypeID = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];

                if (Date_From != null && Date_To != null)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                    sb.Append(" AND MonthYear between DATEFROMPARTS(YEAR(@FromDate), MONTH(@FromDate), 1) AND EOMONTH(@ToDate, 0) ");
                }
                else if (Date_From != null)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                    sb.Append(" AND MonthYear >= DATEFROMPARTS(YEAR(@FromDate), MONTH(@FromDate), 1) ");
                }
                else if (Date_To != null)
                {
                    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                    sb.Append(" AND MonthYear >= DATEFROMPARTS(YEAR(@FromDate), MONTH(@FromDate), 1) ");
                }

                

                ////person 
                if (personIDFrom != null && personIDTo != null)
                {
                    sb.Append(" and ( HRMS_Set_Employee.PersonID between " + personIDFrom + " and   " + personIDTo + " )");
                }
                else if (personIDFrom != null)
                {
                    sb.Append(" and ( HRMS_Set_Employee.PersonID = " + personIDFrom + " )");
                }
                else if (personIDTo != null)
                {
                    sb.Append(" and ( HRMS_Set_Employee.PersonID  = " + personIDTo + " )");
                }

                ////department
                if (DepartmentIDFrom != null)
                {
                    sb.Append(" and (HRMS_Set_Employee.FK_DepartmentID =  " + DepartmentIDFrom + " ) ");

                }
                

                if (EmpStatusTypeID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + EmpStatusTypeID);
                }

                #region Query
                string Query = @"
--DECLARE @FromDate DATE = '2024-09-01' ,
--	@ToDate DATE = '2024-10-01';

WITH AttendanceData as 
(
	SELECT 
		--HRMS_EmployeeAttendance.EmployeeAttendanceID,
		HRMS_EmployeeAttendance.PersonID, 
		HRMS_Set_Employee.EmployeeCode,
		HRMS_EmployeeAttendance.LeaveID, 
		DesignationName,
		DAY(EOMONTH(HRMS_EmployeeAttendance.MonthYear, 0)) AS NoOfDays,
		HRMS_EmployeeAttendance.Quantity AS Quantity, 
		EOMONTH(MonthYear) AS MonthYear,
		--DATENAME(month, HRMS_EmployeeAttendance.MonthYear) AS MonthYear,
		HRMS_PayrollType.Description, 
		HRMS_Set_Employee.FullName,
		HRMS_EmployeeAttendance.StatusCode
	FROM 
		HRMS_EmployeeAttendance
	INNER JOIN 
		HRMS_PayrollType 
		ON HRMS_EmployeeAttendance.LeaveID = HRMS_PayrollType.PayrollTypeID
		AND HRMS_PayrollType.PayrollTypeCode = 4
	INNER JOIN 
		HRMS_Set_Employee 
		ON HRMS_EmployeeAttendance.PersonID = HRMS_Set_Employee.PersonID
		AND HRMS_EmployeeAttendance.CompanyID = HRMS_Set_Employee.CompanyID
	LEFT JOIN 
		SET_Department 
		ON SET_Department.DepartmentID = HRMS_Set_Employee.FK_DepartmentID
	LEFT JOIN 
		HRMS_Employement_Status 
		ON HRMS_Employement_Status.StatusID = HRMS_Set_Employee.StatusID
	LEFT JOIN
		SET_Designation 
		ON SET_Designation.DesignationID = HRMS_Set_Employee.FK_JobDesignationID
	WHERE 
		HRMS_EmployeeAttendance.status = 1
		{0}
)


SELECT 
    PersonID,
    EmployeeCode,
    DesignationName,
    MonthYear,
    MAX(FullName) AS FullName,
    NoOfDays,
    MAX(CASE WHEN Description = 'CPL ENCSHMENT' THEN Quantity END) AS CPL_ENCSHMENT,
    MAX(CASE WHEN Description = 'Absent Days' THEN Quantity END) AS Absent_Days,
    MAX(CASE WHEN Description = 'Total Absent Days' THEN Quantity END) AS Total_Absent_Days,
    NoOfDays - COALESCE(MAX(CASE WHEN Description = 'Total Absent Days' THEN Quantity END), 0) AS PresentDays
FROM AttendanceData
GROUP BY 
    PersonID,
    EmployeeCode,
    MonthYear,
    NoOfDays,
    DesignationName

ORDER BY
	MonthYear,
	FullName
";
                #endregion
                
                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, sb.ToString()), sqlparams).Tables[0];
               

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }


        public static DataTable GetTMS_MasterForDailyAttendanceForYear(string companyID, Nullable<int> periodID, Nullable<int> personIDFrom, Nullable<int> personIDTo, Nullable<int> ShiftIDFrom, Nullable<int> ShiftIDTo, Nullable<int> TerminalNoFrom, Nullable<int> TerminalNoTo,
        string Date_From, string Date_To, Nullable<int> DepartmentIDFrom, Nullable<int> DepartmentIDTo, Nullable<int> PersonType)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            StringBuilder sb3 = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (Date_From != null && Date_To != null)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                    sb.Append("  where SET_DateTime.TheDate >= @FromDate and SET_DateTime.TheDate <= @ToDate  and EP.IsActive = 1   and SET_DateTime.TheDate >=                                     EP.JoiningDate  and SET_DateTime.TheDate <= isnull(EP.jobEndingDate,getdate())");
                }

                ////person 
                if (personIDFrom != null && personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID between " + personIDFrom + " and   " + personIDTo + " )");

                 
                }
                else if (personIDFrom != null)
                {
                    sb1.Append(" and ( EP.PersonID = " + personIDFrom + " )");
                }
                else if (personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID  = " + personIDTo + " )");
                }


                ////department
                if (DepartmentIDFrom != null && DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID between " + DepartmentIDFrom + " and " + DepartmentIDTo + ") ");

                }
                else if (DepartmentIDFrom != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID =  " + DepartmentIDFrom + " ) ");

                }
                else if (DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID  =  " + DepartmentIDTo + ") ");
                }

                ////terminal 
                if (TerminalNoFrom != null && TerminalNoTo != null)
                {
                    sb2.Append(" where (TerminalNo between " + TerminalNoFrom + " and " + TerminalNoTo + ") ");
                }
                else if (TerminalNoFrom != null)
                {
                    sb2.Append(" where (TerminalNo = " + TerminalNoFrom + ") ");
                }
                else if (TerminalNoTo != null)
                {
                    sb2.Append(" where (TerminalNo  = " + TerminalNoTo + ") ");
                }

                if (PersonType != null)
                {
                    sb3.Append(" and EP.PersonTypeCode = " + PersonType + "");
                }
                ////shift
                //if (ShiftIDFrom != null && ShiftIDTo != null)
                //{


                //}
                //else if (ShiftIDFrom != null)
                //{
                //}
                //else if (ShiftIDTo != null)
                //{

                //}

                companyID = "(" + companyID + ")";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Setup.HRMS_TMSData.Query_YearlyAttendance.ToString().Replace("@CompanyID", companyID), sb, sb1, sb2,sb3), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetTMS_MasterForAbsenteesAndLeaves(string param, Nullable<int> companyID, Nullable<int> periodID, Nullable<int> personIDFrom, Nullable<int> personIDTo, Nullable<int> ShiftIDFrom, Nullable<int> ShiftIDTo, Nullable<int> TerminalNoFrom, Nullable<int> TerminalNoTo,
            Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> DepartmentIDFrom, Nullable<int> DepartmentIDTo,int location = 0, Nullable<int> FiscalID = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            StringBuilder sb3 = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (Date_From != null && Date_To != null)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                    sb.Append("  where SET_DateTime.TheDate >= @FromDate and SET_DateTime.TheDate <= @ToDate and EP.IsActive = 1 ");
                    // sb3.Append(" where fintab2.Status = 'A'");
                    sb3.Append(param);
                }
                sqlparams[4] = new SqlParameter("@FiscalID", FiscalID);
                //sqlparams[2] = new SqlParameter("@PersonID", personID);
                //sqlparams[3] = new SqlParameter("@ShiftID", ShiftID);
                //sqlparams[4] = new SqlParameter("@TerminalNo", TerminalNo);

                //sqlparams[7] = new SqlParameter("@DepartmentID", DepartmentID);


                //date 
                //if (Date_From != null && Date_To != null)
                //{
                //    sb.Append(" where ( cast (SET_DateTime.TheDate as date) >= " + Date_From + " and cast( SET_DateTime.TheDate  as date) <= " + Date_To + " )");
                //}


                ////person 
                if (personIDFrom != null && personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID between " + personIDFrom + " and   " + personIDTo + " )");
                }
                else if (personIDFrom != null)
                {
                    sb1.Append(" and ( EP.PersonID = " + personIDFrom + " )");
                }
                else if (personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID  = " + personIDTo + " )");
                }
                if (location > 0)
                {
                    sb1.Append(" and ( EP.FK_LocationID  = " + location + " )");
                }



                ////department
                if (DepartmentIDFrom != null && DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID between " + DepartmentIDFrom + " and " + DepartmentIDTo + ") ");

                }
                else if (DepartmentIDFrom != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID =  " + DepartmentIDFrom + " ) ");

                }
                else if (DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID  =  " + DepartmentIDTo + ") ");
                }

                ////terminal 
                if (TerminalNoFrom != null && TerminalNoTo != null)
                {
                    sb2.Append(" where (TerminalNo between " + TerminalNoFrom + " and " + TerminalNoTo + ") ");
                }
                else if (TerminalNoFrom != null)
                {
                    sb2.Append(" where (TerminalNo = " + TerminalNoFrom + ") ");
                }
                else if (TerminalNoTo != null)
                {
                    sb2.Append(" where (TerminalNo  = " + TerminalNoTo + ") ");
                }
                ////shift
                //if (ShiftIDFrom != null && ShiftIDTo != null)
                //{


                //}
                //else if (ShiftIDFrom != null)
                //{
                //}
                //else if (ShiftIDTo != null)
                //{

                //}

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetLeavesData, sb, sb1, sb2, sb3), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable Get_AttendanceReport(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> personIDFrom, Nullable<int> personIDTo,Nullable<DateTime> Date_From, Nullable<DateTime> Date_To)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (Date_From != null && Date_To != null)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                    sb.Append("and HRMS_EmployeeAttendance.MonthYear >= @FromDate and HRMS_EmployeeAttendance.MonthYear <= @ToDate ");
  
                }
             
                if (personIDFrom != null && personIDTo != null)
                {
                    sb1.Append(" and ( HRMS_EmployeeAttendance.PersonID between " + personIDFrom + " and   " + personIDTo + " )");
                }
                else if (personIDFrom != null)
                {
                    sb1.Append(" and ( HRMS_EmployeeAttendance.PersonID = " + personIDFrom + " )");
                }
                else if (personIDTo != null)
                {
                    sb1.Append(" and ( HRMS_EmployeeAttendance.PersonID  = " + personIDTo + " )");
                }

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Transaction.HRMS_Appraisal.Query_GetAttendance, sb, sb1), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }


        public static DataTable GetTMS_MasterForAbsenteesAndLeaves2(string param, string companyID, Nullable<int> periodID, Nullable<int> personIDFrom, Nullable<int> personIDTo, Nullable<int> ShiftIDFrom, Nullable<int> ShiftIDTo, Nullable<int> TerminalNoFrom, Nullable<int> TerminalNoTo,
        Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> DepartmentIDFrom, Nullable<int> DepartmentIDTo, Nullable<int> persontype, Nullable<int> EmpStatusTypeID = null, int LocationID = 0)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            StringBuilder sb3 = new StringBuilder();
            StringBuilder sb4 = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (Date_From != null && Date_To != null)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                    sb.Append("  where SET_DateTime.TheDate >= @FromDate and SET_DateTime.TheDate <= @ToDate and EP.IsActive = 1 and SET_DateTime.TheDate >= EP.JoiningDate  and SET_DateTime.TheDate <= isnull(EP.jobEndingDate,getdate()) ");

                    if (persontype != null)
                    {
                        sb.Append(" and EP.PersonTypeCode = " + persontype + "");
                    }
                    // sb3.Append(" where fintab2.Status = 'A'");
                    sb3.Append(param);
                }
                //sqlparams[2] = new SqlParameter("@PersonID", personID);
                //sqlparams[3] = new SqlParameter("@ShiftID", ShiftID);
                //sqlparams[4] = new SqlParameter("@TerminalNo", TerminalNo);

                //sqlparams[7] = new SqlParameter("@DepartmentID", DepartmentID);


                //date 
                //if (Date_From != null && Date_To != null)
                //{
                //    sb.Append(" where ( cast (SET_DateTime.TheDate as date) >= " + Date_From + " and cast( SET_DateTime.TheDate  as date) <= " + Date_To + " )");
                //}


                ////person 
                if (personIDFrom != null && personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID between " + personIDFrom + " and   " + personIDTo + " )");
                }
                else if (personIDFrom != null)
                {
                    sb1.Append(" and ( EP.PersonID = " + personIDFrom + " )");
                }
                else if (personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID  = " + personIDTo + " )");
                }



                ////department
                if (DepartmentIDFrom != null && DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID between " + DepartmentIDFrom + " and " + DepartmentIDTo + ") ");

                }
                else if (DepartmentIDFrom != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID =  " + DepartmentIDFrom + " ) ");

                }
                else if (DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID  =  " + DepartmentIDTo + ") ");
                }

                ////terminal 
                if (TerminalNoFrom != null && TerminalNoTo != null)
                {
                    sb2.Append(" where (TerminalNo between " + TerminalNoFrom + " and " + TerminalNoTo + ") ");
                }
                else if (TerminalNoFrom != null)
                {
                    sb2.Append(" where (TerminalNo = " + TerminalNoFrom + ") ");
                }
                else if (TerminalNoTo != null)
                {
                    sb2.Append(" where (TerminalNo  = " + TerminalNoTo + ") ");
                }
                ////shift
                //if (ShiftIDFrom != null && ShiftIDTo != null)
                //{
                //}
                //else if (ShiftIDFrom != null)
                //{
                //}
                //else if (ShiftIDTo != null)
                //{

                //}

                if (EmpStatusTypeID > 0)
                {
                    sb4.Append(" and EP.StatusID = " + EmpStatusTypeID);
                }



                if (LocationID > 0)
                {
                    sb4.Append(" and EP.FK_LocationID = " + LocationID);
                } 

                companyID = "(" + companyID + ")";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Setup.HRMS_TMSData.Query_TMS_Absentees.ToString().Replace("@CompanyID", companyID), sb, sb1, sb2, sb3, sb4), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }


        public static DataTable GetLoanRegisterEmployeeWise(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> personIDFrom, Nullable<int> personIDTo,
          Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> EmployeeStatusID, int EmployeeTypeID = 0, Nullable<Guid> UserID = null, bool Active = false,bool InActive = false)
        {
            DataTable dt = null;
            StringBuilder sb1 = new StringBuilder();

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (Date_From != null && Date_To != null)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                    sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                }

                ////person 
                if (personIDFrom != null && personIDTo != null)
                {
                    sb1.Append(" and ( HRMS_Set_Employee.PersonID between " + personIDFrom + " and " + personIDTo + " )");
                }
                else if (personIDFrom != null)
                {
                    sb1.Append(" and ( HRMS_Set_Employee.PersonID = " + personIDFrom + " )");
                }
                else if (personIDTo != null)
                {
                    sb1.Append(" and ( HRMS_Set_Employee.PersonID  = " + personIDTo + " )");
                }
                if (EmployeeStatusID > 0)
                {
                    sb1.Append(" and HRMS_Set_Employee.StatusID = " + EmployeeStatusID);
                }
                else
                {
                    sb1.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                }
                if (EmployeeTypeID > 0)
                {
                    sb1.Append(" and HRMS_Set_Employee.PersonTypeCode = " + EmployeeTypeID);
                }
                
                if (Active == false && InActive == true)
                {
                    sb1.Append(" and HRMS_Set_Employee.isActive = 0");
                }
                else if (Active == true && InActive == false)
                {
                    sb1.Append(" and HRMS_Set_Employee.isActive = 1");
                }

               // dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_LoanRegisterEmployeeWise, sb1.ToString()), sqlparams).Tables[0];
                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Transaction.HRMS_Appraisal.Query_LoanRegisterEmployeeWise, sb1.ToString()), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetLoanRegisterEmployeeWiseAlNoor(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> personIDFrom, Nullable<int> personIDTo,
          Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> EmployeeStatusID, int EmployeeTypeID = 0, Nullable<Guid> UserID = null, bool Active = false, bool InActive = false, int LocationID = 0)
        {
            DataTable dt = null;
            StringBuilder sb1 = new StringBuilder();

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (Date_From != null && Date_To != null)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                    sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                }

                ////person 
                if (personIDFrom != null && personIDTo != null)
                {
                    sb1.Append(" and ( HRMS_Set_Employee.PersonID between " + personIDFrom + " and " + personIDTo + " )");
                }
                else if (personIDFrom != null)
                {
                    sb1.Append(" and ( HRMS_Set_Employee.PersonID = " + personIDFrom + " )");
                }
                else if (personIDTo != null)
                {
                    sb1.Append(" and ( HRMS_Set_Employee.PersonID  = " + personIDTo + " )");
                }
                if (EmployeeStatusID > 0)
                {
                    sb1.Append(" and HRMS_Set_Employee.StatusID = " + EmployeeStatusID);
                }
                else
                {
                    sb1.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                }
                if (EmployeeTypeID > 0)
                {
                    sb1.Append(" and HRMS_Set_Employee.PersonTypeCode = " + EmployeeTypeID);
                }

                if (Active == false && InActive == true)
                {
                    sb1.Append(" and HRMS_Set_Employee.isActive = 0");
                }
                else if (Active == true && InActive == false)
                {
                    sb1.Append(" and HRMS_Set_Employee.isActive = 1");
                }
                if (LocationID > 0)
                {
                    sb1.Append(" and HRMS_Set_Employee.FK_LocationID = " + LocationID);
                }

                // dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_LoanRegisterEmployeeWise, sb1.ToString()), sqlparams).Tables[0];
                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Transaction.HRMS_Appraisal.Query_LoanRegisterEmployeeWise, sb1.ToString()), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetLoanRegisterEmployeeWise_LoanDateWise(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> personIDFrom, Nullable<int> personIDTo,
     Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> EmployeeStatusID, int EmployeeTypeID = 0, Nullable<Guid> UserID = null, bool Active = false, bool InActive = false)
        {
            DataTable dt = null;
            StringBuilder sb1 = new StringBuilder();

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (Date_From != null && Date_To != null)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                    sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                }

                ////person 
                if (personIDFrom != null && personIDTo != null)
                {
                    sb1.Append(" and ( HRMS_Set_Employee.PersonID between " + personIDFrom + " and " + personIDTo + " )");
                }
                else if (personIDFrom != null)
                {
                    sb1.Append(" and ( HRMS_Set_Employee.PersonID = " + personIDFrom + " )");
                }
                else if (personIDTo != null)
                {
                    sb1.Append(" and ( HRMS_Set_Employee.PersonID  = " + personIDTo + " )");
                }
                if (EmployeeStatusID > 0)
                {
                    sb1.Append(" and HRMS_Set_Employee.StatusID = " + EmployeeStatusID);
                }
                else
                {
                    sb1.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                }
                if (EmployeeTypeID > 0)
                {
                    sb1.Append(" and HRMS_Set_Employee.PersonTypeCode = " + EmployeeTypeID);
                }

                if (Active == false && InActive == true)
                {
                    sb1.Append(" and HRMS_Set_Employee.isActive = 0");
                }
                else if (Active == true && InActive == false)
                {
                    sb1.Append(" and HRMS_Set_Employee.isActive = 1");
                }


                // dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_LoanRegisterEmployeeWise, sb1.ToString()), sqlparams).Tables[0];
                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Transaction.HRMS_Appraisal.Query_LoanRegisterEmployeeWise_LoanDateWise, sb1.ToString()), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetLoanRegisterEmployeeWise_LoanDateWiseAlNoor(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> personIDFrom, Nullable<int> personIDTo,
     Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> EmployeeStatusID, int EmployeeTypeID = 0, Nullable<Guid> UserID = null, bool Active = false, bool InActive = false, int LocationID = 0)
        {
            DataTable dt = null;
            StringBuilder sb1 = new StringBuilder();

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (Date_From != null && Date_To != null)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                    sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                }

                ////person 
                if (personIDFrom != null && personIDTo != null)
                {
                    sb1.Append(" and ( HRMS_Set_Employee.PersonID between " + personIDFrom + " and " + personIDTo + " )");
                }
                else if (personIDFrom != null)
                {
                    sb1.Append(" and ( HRMS_Set_Employee.PersonID = " + personIDFrom + " )");
                }
                else if (personIDTo != null)
                {
                    sb1.Append(" and ( HRMS_Set_Employee.PersonID  = " + personIDTo + " )");
                }
                if (EmployeeStatusID > 0)
                {
                    sb1.Append(" and HRMS_Set_Employee.StatusID = " + EmployeeStatusID);
                }
                else
                {
                    sb1.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                }
                if (EmployeeTypeID > 0)
                {
                    sb1.Append(" and HRMS_Set_Employee.PersonTypeCode = " + EmployeeTypeID);
                }

                if (Active == false && InActive == true)
                {
                    sb1.Append(" and HRMS_Set_Employee.isActive = 0");
                }
                else if (Active == true && InActive == false)
                {
                    sb1.Append(" and HRMS_Set_Employee.isActive = 1");
                }
                if (LocationID > 0)
                {
                    sb1.Append(" and HRMS_Set_Employee.FK_LocationID = " + LocationID);
                }


                // dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_LoanRegisterEmployeeWise, sb1.ToString()), sqlparams).Tables[0];
                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Transaction.HRMS_Appraisal.Query_LoanRegisterEmployeeWise_LoanDateWise, sb1.ToString()), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }


        public static DataTable Get_EmployeeRelativeInformation(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> personIDFrom, Nullable<int> personIDTo,
       Nullable<DateTime> Date_From, Nullable<DateTime> Date_To)
        {
            DataTable dt = null;
            StringBuilder sb1 = new StringBuilder();

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (Date_From != null && Date_To != null)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                    sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                }

                ////person 
                if (personIDFrom != null && personIDTo != null)
                {
                    sb1.Append(" and ( HRMS_Set_Employee.PersonID between " + personIDFrom + " and " + personIDTo + " )");
                }
                else if (personIDFrom != null)
                {
                    sb1.Append(" and ( HRMS_Set_Employee.PersonID = " + personIDFrom + " )");
                }
                else if (personIDTo != null)
                {
                    sb1.Append(" and ( HRMS_Set_Employee.PersonID  = " + personIDTo + " )");
                }


                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_EmployeeRelativeInformation, sb1.ToString()), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetTrainingNeedAnalysisData(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> personID)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                if (personID != null)
                {
                    if (personID > 0)
                        sb.Append(" and HRMS_EmployeeDevelopmentPlan.PersonID=" + personID);
                }
                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetTrainingNeedAnalysisData, sb.ToString()), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;

        }

        public static DataTable GetTMSActiveDateWithLeaves(string companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> personID, Nullable<int> PersonType, bool IsVehicle = false)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", personID);
                sqlparams[3] = new SqlParameter("@FromDate", Date_From);
                sqlparams[4] = new SqlParameter("@ToDate", Date_To);
                sqlparams[5] = new SqlParameter("@PersonType", PersonType);


                companyID = "(" + companyID + ")";


                if (IsVehicle == true)
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.PayRoll.Setup.HRMS_TMSData.Query_GetTMSActiveDateWithLeaves.ToString().Replace("@CompanyID" , companyID), sqlparams).Tables[0];
                }
                else
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.PayRoll.Setup.HRMS_TMSData.Query_GetTMSActiveDateWithLeaves_INN.ToString().Replace("@CompanyID", companyID), sqlparams).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;

        }

        public static DataTable GetTMSActiveData(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> personID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                //sqlparams[2] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                //sqlparams[3] = new SqlParameter("@Year", Date_From.Value.Year.ToString());
                sqlparams[4] = new SqlParameter("@PersonID", personID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetTMSActiveData, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataByPersonIDandDate(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> PersonID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, string Query_GetAllTMSByPersonID, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                //sqlparams[2] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                //sqlparams[3] = new SqlParameter("@Year", Date_From.Value.Year.ToString());
                sqlparams[4] = new SqlParameter("@PersonID", PersonID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetAllTMSByPersonID, sqlparams);

                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllTMSByPersonID, sqlparams);

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

        public static DataTable GetTimeSheetStatusData(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> personID, Nullable<int> Department = null)
        {
            DataTable dt = null;

            try
            {
                StringBuilder sb = new StringBuilder();
                if (Department != null && Department > 0)
                {
                    sb.Append(" and DP.DepartmentID = " + Department + "");
                }

                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                sqlparams[4] = new SqlParameter("@PersonID", personID);


                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Setup.HRMS_TMSData.Query_GetTimeSheetStatus_INN, sb), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable AttendanceSheet(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> personID, Nullable<int> Department = null)
        {
            DataTable dt = null;

            try
            {
                StringBuilder sb = new StringBuilder();
                if (Department != null && Department > 0)
                {
                    sb.Append(" and SET_Department.DepartmentID = " + Department + "");
                }

                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                sqlparams[4] = new SqlParameter("@PersonID", personID);


                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_AttendanceSheet, sb), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable EmployeeExcessAttendance(int companyID, int periodID, int FromDays, int ToDays)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDays", FromDays);
                sqlparams[3] = new SqlParameter("@ToDays", ToDays);


                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_EmployeeExcessAttendance), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable Get_EmployeeAttendanceList(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> personID, Nullable<int> Department = null)
        {
            DataTable dt = null;

            try
            {
                StringBuilder sb = new StringBuilder();
                if (Department != null && Department > 0)
                {
                    sb.Append(" and SET_Department.DepartmentID = " + Department + "");
                }

                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                sqlparams[4] = new SqlParameter("@PersonID", personID);


                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_EmployeeAttendanceList, sb), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GEtTMSbyEMployeeID(int companyID, int PersonID, Nullable<DateTime> Date_From)
        {
            DataTable dt = null;
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PersonID);
                sqlparams[2] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                sqlparams[3] = new SqlParameter("@Year", Date_From.Value.Year.ToString());
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GEtTMSbyEMployeeID, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GEtTMSDataForAdam(string Date_From = null, string Date_To = null)
        {
            DataTable dt = null;
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@FromDate", Date_From);
                sqlparams[1] = new SqlParameter("@ToDate", Date_To);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GEtTMSDataForAdam, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GEtTMSDataForAdamForTimeOut(string Date_From = null, string Date_To = null)
        {
            DataTable dt = null;
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@FromDate", Date_From);
                sqlparams[1] = new SqlParameter("@ToDate", Date_To);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GEtTMSDataForAdamTimeOut, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable MyGetAllData(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_MySelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_MySelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_MySelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_MySelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_MySelectAll_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_MySelectAll_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static bool DeleteDataByKey(SqlTransaction objTransaction, int PersonID, int TableID, int TMSID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@PersonID", PersonID);
                sqlparams[1] = new SqlParameter("@TableID", TableID);
                sqlparams[2] = new SqlParameter("@TMSID", TMSID);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByKey, sqlparams) > 0)
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


        #endregion

        public static DataTable GetTMS_MasterAttendence(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> persontype, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> personIDFrom, Nullable<int> personIDTo, Nullable<int> ShiftIDFrom, Nullable<int> ShiftIDTo, Nullable<int> TerminalNoFrom, Nullable<int> TerminalNoTo,
           Nullable<int> DepartmentIDFrom, Nullable<int> DepartmentIDTo, Nullable<int> SectionID)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            //StringBuilder sb3 = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);


                if (Date_From != null && Date_To != null)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                    sb.Append("  where SET_DateTime.TheDate >= @FromDate and SET_DateTime.TheDate <= @ToDate  and EP.IsActive = 1   and SET_DateTime.TheDate >= EP.JoiningDate");//and SET_DateTime.TheDate <= isnull(EP.jobEndingDate,getdate())

                    if (persontype != null)
                    {
                        sb.Append(" and EP.PersonTypeCode = " + persontype + "");
                    }
                }
                //if (Date_From != null && Date_To != null)
                //{
                //    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                //    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                //    sb.Append("  where SET_DateTime.TheDate >= @FromDate and SET_DateTime.TheDate <= @ToDate  and EP.IsActive = 1 ");
                ////and EP.PersonTypeCode =  
                //}
                //sqlparams[2] = new SqlParameter("@PersonID", personID);
                //sqlparams[3] = new SqlParameter("@ShiftID", ShiftID);
                //sqlparams[4] = new SqlParameter("@TerminalNo", TerminalNo);

                //sqlparams[7] = new SqlParameter("@DepartmentID", DepartmentID);


                //date 
                //if (Date_From != null && Date_To != null)
                //{
                //    sb.Append(" where ( cast (SET_DateTime.TheDate as date) >= " + Date_From + " and cast( SET_DateTime.TheDate  as date) <= " + Date_To + " )");
                //}


                ////person 
                if (personIDFrom != null && personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID between " + personIDFrom + " and   " + personIDTo + " )");
                }
                else if (personIDFrom != null)
                {
                    sb1.Append(" and ( EP.PersonID = " + personIDFrom + " )");
                }
                else if (personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID  = " + personIDTo + " )");
                }



                ////department
                if (DepartmentIDFrom != null && DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID between " + DepartmentIDFrom + " and " + DepartmentIDTo + ") ");

                }
                else if (DepartmentIDFrom != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID =  " + DepartmentIDFrom + " ) ");

                }
                else if (DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID  =  " + DepartmentIDTo + ") ");
                }
                if (SectionID != null)
                {
                    sb1.Append("and (EP.FK_SectionID=" + SectionID + ")");

                }

                ////terminal 
                if (TerminalNoFrom != null && TerminalNoTo != null)
                {
                    sb2.Append(" where (TerminalNo between " + TerminalNoFrom + " and " + TerminalNoTo + ") ");
                }
                else if (TerminalNoFrom != null)
                {
                    sb2.Append(" where (TerminalNo = " + TerminalNoFrom + ") ");
                }
                else if (TerminalNoTo != null)
                {
                    sb2.Append(" where (TerminalNo  = " + TerminalNoTo + ") ");
                }
                ////shift
                //if (ShiftIDFrom != null && ShiftIDTo != null)
                //{


                //}
                //else if (ShiftIDFrom != null)
                //{
                //}
                //else if (ShiftIDTo != null)
                //{

                //}

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Setup.HRMS_TMSData.Query_TMS_Master, sb, sb1, sb2), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }


        public static DataTable GetTMS_MasterLateIN(string param, Nullable<int> companyID, Nullable<int> periodID, Nullable<int> personIDFrom, Nullable<int> personIDTo, Nullable<int> ShiftIDFrom, Nullable<int> ShiftIDTo, Nullable<int> TerminalNoFrom, Nullable<int> TerminalNoTo,
         Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> DepartmentIDFrom, Nullable<int> DepartmentIDTo, Nullable<int> persontype, Nullable<int> SectionID)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            //StringBuilder sb3 = new StringBuilder();
            //StringBuilder sb4 = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (Date_From != null && Date_To != null)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                    sb.Append("  where SET_DateTime.TheDate >= @FromDate and SET_DateTime.TheDate <= @ToDate and EP.IsActive = 1 and SET_DateTime.TheDate >= EP.JoiningDate  and SET_DateTime.TheDate <= isnull(EP.jobEndingDate,getdate()) ");

                    if (persontype != null)
                    {
                        sb.Append(" and EP.PersonTypeCode = " + persontype + "");
                    }
                    // sb3.Append(" where fintab2.Status = 'A'");
                    //sb3.Append(param);
                }
                //sqlparams[2] = new SqlParameter("@PersonID", personID);
                //sqlparams[3] = new SqlParameter("@ShiftID", ShiftID);
                //sqlparams[4] = new SqlParameter("@TerminalNo", TerminalNo);

                //sqlparams[7] = new SqlParameter("@DepartmentID", DepartmentID);


                //date 
                //if (Date_From != null && Date_To != null)
                //{
                //    sb.Append(" where ( cast (SET_DateTime.TheDate as date) >= " + Date_From + " and cast( SET_DateTime.TheDate  as date) <= " + Date_To + " )");
                //}


                ////person 
                if (personIDFrom != null && personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID between " + personIDFrom + " and   " + personIDTo + " )");
                }
                else if (personIDFrom != null)
                {
                    sb1.Append(" and ( EP.PersonID = " + personIDFrom + " )");
                }
                else if (personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID  = " + personIDTo + " )");
                }



                ////department
                if (DepartmentIDFrom != null && DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID between " + DepartmentIDFrom + " and " + DepartmentIDTo + ") ");

                }
                else if (DepartmentIDFrom != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID =  " + DepartmentIDFrom + " ) ");

                }
                else if (DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID  =  " + DepartmentIDTo + ") ");
                }

                ////terminal 
                if (TerminalNoFrom != null && TerminalNoTo != null)
                {
                    sb2.Append(" where (TerminalNo between " + TerminalNoFrom + " and " + TerminalNoTo + ") ");
                }
                else if (TerminalNoFrom != null)
                {
                    sb2.Append(" where (TerminalNo = " + TerminalNoFrom + ") ");
                }
                else if (TerminalNoTo != null)
                {
                    sb2.Append(" where (TerminalNo  = " + TerminalNoTo + ") ");
                }
                ////shift
                //if (ShiftIDFrom != null && ShiftIDTo != null)
                //{
                if (SectionID != null)
                {
                    sb1.Append("and (EP.FK_SectionID=" + SectionID + ")");

                }
                //}
                //else if (ShiftIDFrom != null)
                //{
                //}
                //else if (ShiftIDTo != null)
                //{

                //}

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Setup.HRMS_TMSData.Query_TMS_Absentees, sb, sb1, sb2), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }


        public static DataTable GetAllDataForManualAttendace(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_SelectAll_ManualAttendaceWithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_ManualAttendaceWithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAll_ManualAttendaceWithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_ManualAttendaceWithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }






        public static DataTable EmployeeGet_MonthSummary(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> personIDFrom, Nullable<int>personIDTo,
         Nullable<int> DepartmentIDFrom, Nullable<int> DepartmentIDTo, Nullable<DateTime> StartofMonth, Nullable<DateTime> EndofMonth)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
 
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", StartofMonth.Value);
                sqlparams[3] = new SqlParameter("@ToDate", EndofMonth.Value);

                ////person 
                if (personIDFrom != null && personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID between " + personIDFrom + " and   " + personIDTo + " )");
                }
                else if (personIDFrom != null)
                {
                    sb1.Append(" and ( EP.PersonID = " + personIDFrom + " )");
                }
                else if (personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID  = " + personIDTo + " )");
                }



                ////department
                if (DepartmentIDFrom != null && DepartmentIDTo != null)
                {
                    sb2.Append(" and (EP.FK_DepartmentID between " + DepartmentIDFrom + " and " + DepartmentIDTo + ") ");

                }
                else if (DepartmentIDFrom != null)
                {
                    sb2.Append(" and (EP.FK_DepartmentID =  " + DepartmentIDFrom + " ) ");

                }
                else if (DepartmentIDTo != null)
                {
                    sb2.Append(" and (EP.FK_DepartmentID  =  " + DepartmentIDTo + ") ");
                }


                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_EmployeeSummary, sb1, sb2), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable DailyStrengthReportTMS_Data(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> ToDate, int LocationID)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ToDate", String.Format("{0:yyyy-MM-dd}", ToDate));
                //sqlparams[2] = new SqlParameter("@ToDate", ToDate);
                #region PersonId and Department Filter

                //if (PersonID > 0)
                //{
                //    sb.Append(" and ( HRMS_Set_Employee.PersonID = " + PersonID + " )");
                //}

                //else if (personIDFrom != null)
                //{
                //    sb1.Append(" and ( EP.PersonID = " + personIDFrom + " )");
                //}
                //else if (personIDTo != null)
                //{
                //    sb1.Append(" and ( EP.PersonID  = " + personIDTo + " )");
                //}



                ////department
                //if (DepartmentIDFrom != null && DepartmentIDTo != null)
                //{
                //    sb2.Append(" and (EP.FK_DepartmentID between " + DepartmentIDFrom + " and " + DepartmentIDTo + ") ");

                //}
                //else if (DepartmentIDFrom != null)
                //{
                //    sb2.Append(" and (EP.FK_DepartmentID =  " + DepartmentIDFrom + " ) ");

                //}
                //else if (DepartmentIDTo != null)
                //{
                //    sb2.Append(" and (EP.FK_DepartmentID  =  " + DepartmentIDTo + ") ");
                //}

                #endregion

                if (ToDate != null)
                {
                    sb.Append(" AND CAST(HRMS_TMSData.TMSDateTime as date) = @ToDate");
                }

                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.FK_LocationID = " + LocationID);
                    sb1.Append(" and HRMS_Set_Employee.FK_LocationID = " + LocationID);
                }

                #region Query
                const string Query_DailyStrengthReportTMS = @"
                     

--- New Report Query For Alnoor
---  DailyStrengthReportTMS

--Select  FinalTab.CompanyID, FinalTab.PersonID, FinalTab.DepartmentName,
--        FinalTab.SectionName, FinalTab.Empl_Status,
--        sum(StatusCount) as StatusCount,
--		FinalTab.DepartmentID ,FinalTab.SectionID
--    from 

--    (


--Select   Tab2.CompanyID, Tab2.PersonID, Tab2.DepartmentName,
--        Tab2.SectionName, Tab2.Empl_Status,
--        sum(StatusCount) as StatusCount,
--		Tab2.DepartmentID ,Tab2.SectionID
--From (

-- Select  Tab.CompanyID, Tab.PersonID, Tab.DepartmentName,
--        Tab.SectionName, Tab.Empl_Status,
--        Count(Tab.StatusCount) as StatusCount,
--		Tab.DepartmentID ,Tab.SectionID
--      from (

-- Select Distinct HRMS_TMSData.CompanyID, HRMS_Set_Employee.PersonID, SET_Department.DepartmentName,
--        SET_Section.SectionName, HRMS_Employement_Status.Description as Empl_Status,
--        CAST(HRMS_TMSData.StatusCode as Int) as StatusCount,
--		SET_Department.DepartmentID ,SET_Section.SectionID
--  From HRMS_TMSData 
--   Inner Join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_TMSData.PersonID 
--         And  HRMS_Set_Employee.CompanyID = HRMS_TMSData.CompanyID
--		 And HRMS_Set_Employee.Status = 1 And HRMS_Set_Employee.IsActive = 1
--   left Join HRMS_Employement_Status on HRMS_Employement_Status.StatusID = HRMS_Set_Employee.StatusID
--         And HRMS_Employement_Status.CompanyID = HRMS_Set_Employee.CompanyID
--         And HRMS_Employement_Status.Status = 1 and HRMS_Employement_Status.IsActive = 1
--   Left join SET_Department on SET_Department.DepartmentID = HRMS_SET_Employee.FK_DepartmentID
--        And SET_Department.status =1
--   Left join SET_Section on SET_Section.SectionID = HRMS_SET_Employee.FK_SectionID
--        And SET_Section.Status = 1  and SET_Section.IsActive = 1
--  Where HRMS_TMSData.StatusCode = 0 And  HRMS_TMSData.CompanyID = @CompanyID
        
--        {0}

--   Group by HRMS_TMSData.CompanyID, HRMS_Set_Employee.PersonID, SET_Department.DepartmentName,
--           HRMS_Employement_Status.Description,SET_Department.DepartmentID,SET_Section.SectionID,
--           SET_Section.SectionName, HRMS_TMSData.StatusCode, HRMS_Set_Employee.FK_SectionID

--		)Tab
--   Group by Tab.CompanyID, Tab.PersonID, Tab.DepartmentName,
--            Tab.SectionName, Tab.Empl_Status,Tab.DepartmentID ,Tab.SectionID
--	) Tab2
--	Group by Tab2.CompanyID, Tab2.PersonID, Tab2.DepartmentName,
--             Tab2.SectionName, Tab2.Empl_Status,Tab2.DepartmentID ,Tab2.SectionID


--    Union All

--	 -- For Employee_Status
-- Select Distinct HRMS_Set_Employee.CompanyID, HRMS_Set_Employee.PersonID, '' as DepartmentName,
--        '' as SectionName, tbl_Status.Description as  Empl_Status,
--        0 as StatusCount,
--		0 as DepartmentID, 0 as SectionID
--  From HRMS_Employement_Status as tbl_Status
--       Inner Join HRMS_Set_Employee on HRMS_Set_Employee.StatusID = tbl_Status.StatusID
--	         And HRMS_Set_Employee.Status= 1 and HRMS_Set_Employee.IsActive = 1
   
--  Where tbl_Status.Status = 1 And  tbl_Status.IsActive = 1
--        and tbl_Status.StatusID not in (
--		Select HRMS_Employement_Status.StatusID
--  From HRMS_TMSData 
--   Inner Join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_TMSData.PersonID 
--         And  HRMS_Set_Employee.CompanyID = HRMS_TMSData.CompanyID
--		 And HRMS_Set_Employee.Status = 1 And HRMS_Set_Employee.IsActive = 1
--   left Join HRMS_Employement_Status on HRMS_Employement_Status.StatusID = HRMS_Set_Employee.StatusID
--         And HRMS_Employement_Status.CompanyID = HRMS_Set_Employee.CompanyID
--         And HRMS_Employement_Status.Status = 1 and HRMS_Employement_Status.IsActive = 1
--   Left join SET_Department on SET_Department.DepartmentID = HRMS_SET_Employee.FK_DepartmentID
--        And SET_Department.status =1
--   Left join SET_Section on SET_Section.SectionID = HRMS_SET_Employee.FK_SectionID
--        And SET_Section.Status = 1  and SET_Section.IsActive = 1
--  Where HRMS_TMSData.StatusCode = 0 And  HRMS_TMSData.CompanyID = @CompanyID
        
--        {0}
--	)

--		Union All

--	 -- For Department
-- Select Distinct HRMS_Set_Employee.CompanyID, HRMS_Set_Employee.PersonID, tbl_Department.DepartmentName,
--        '' as SectionName, '' as  Empl_Status,
--        0 as StatusCount,
--		0 as DepartmentID, 0 as SectionID
--  From SET_Department as tbl_Department
--       Inner Join HRMS_Set_Employee on HRMS_Set_Employee.FK_DepartmentID = tbl_Department.DepartmentID
--	         And HRMS_Set_Employee.Status= 1 and HRMS_Set_Employee.IsActive = 1
   
--  Where tbl_Department.Status = 1 And  tbl_Department.IsActive = 1
--        and tbl_Department.DepartmentID not in (
--		Select SET_Department.DepartmentID
--  From HRMS_TMSData 
--   Inner Join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_TMSData.PersonID 
--         And  HRMS_Set_Employee.CompanyID = HRMS_TMSData.CompanyID
--		 And HRMS_Set_Employee.Status = 1 And HRMS_Set_Employee.IsActive = 1
--   left Join HRMS_Employement_Status on HRMS_Employement_Status.StatusID = HRMS_Set_Employee.StatusID
--         And HRMS_Employement_Status.CompanyID = HRMS_Set_Employee.CompanyID
--         And HRMS_Employement_Status.Status = 1 and HRMS_Employement_Status.IsActive = 1
--   Left join SET_Department on SET_Department.DepartmentID = HRMS_SET_Employee.FK_DepartmentID
--        And SET_Department.status =1 and SET_Department.IsActive = 1
--   Left join SET_Section on SET_Section.SectionID = HRMS_SET_Employee.FK_SectionID
--        And SET_Section.Status = 1  and SET_Section.IsActive = 1
--  Where HRMS_TMSData.StatusCode = 0 And  HRMS_TMSData.CompanyID = @CompanyID
        
--        {0}
--	)

--   Union All

--	 ---- For Section

-- Select Distinct HRMS_Set_Employee.CompanyID, HRMS_Set_Employee.PersonID, '' as DepartmentName,
--        tbl_Section.SectionName, '' as  Empl_Status,
--        0 as StatusCount,
--		0 as DepartmentID, 0 as SectionID
--  From SET_Section as tbl_Section
--       Inner Join HRMS_Set_Employee on HRMS_Set_Employee.FK_SectionID = tbl_Section.SectionID
--	         And HRMS_Set_Employee.Status= 1 and HRMS_Set_Employee.IsActive = 1
   
--  Where tbl_Section.Status = 1 And  tbl_Section.IsActive = 1
--        and tbl_Section.SectionID not in (
--		Select SET_Section.SectionID
--  From HRMS_TMSData 
--   Inner Join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_TMSData.PersonID 
--         And  HRMS_Set_Employee.CompanyID = HRMS_TMSData.CompanyID
--		 And HRMS_Set_Employee.Status = 1 And HRMS_Set_Employee.IsActive = 1
--   left Join HRMS_Employement_Status on HRMS_Employement_Status.StatusID = HRMS_Set_Employee.StatusID
--         And HRMS_Employement_Status.CompanyID = HRMS_Set_Employee.CompanyID
--         And HRMS_Employement_Status.Status = 1 and HRMS_Employement_Status.IsActive = 1
--   Left join SET_Department on SET_Department.DepartmentID = HRMS_SET_Employee.FK_DepartmentID
--        And SET_Department.status =1 and SET_Department.IsActive = 1
--   Left join SET_Section on SET_Section.SectionID = HRMS_SET_Employee.FK_SectionID
--        And SET_Section.Status = 1  and SET_Section.IsActive = 1
--  Where HRMS_TMSData.StatusCode = 0 And  HRMS_TMSData.CompanyID = @CompanyID
--        and SET_Section.SectionName is not null
--        {0}

--		)

--    )FinalTab
--	GROUP BY FinalTab.CompanyID, FinalTab.PersonID, FinalTab.DepartmentName, FinalTab.SectionName,
--	         FinalTab.SectionName, FinalTab.Empl_Status, FinalTab.DepartmentID,FinalTab.SectionID
--  Order by FinalTab.DepartmentName asc

Select  FinalTab.CompanyID, FinalTab.DepartmentName,
        isnull(FinalTab.SectionName,'N/A') as SectionName, FinalTab.Empl_Status,
        sum(StatusCount) as StatusCount,
	    case when  FinalTab.Empl_Status = 'Total On Roll' then 0 else (sum(StatusCount)) end as Total_Emp,
		case when  FinalTab.Empl_Status = 'Total On Roll' then (sum(isnull(StatusCount,0))) else 0 end as Total_TMS_Emp,
		FinalTab.DepartmentID ,FinalTab.SectionID
    from 

    (
Select Distinct HRMS_Set_Employee.CompanyID, DepartmentName,
        SectionName, HRMS_Employement_Status.Description as  Empl_Status,
        count(PersonID) as  StatusCount,
	    SET_Department.DepartmentID, SectionID
  
  From HRMS_Set_Employee 

   left Join HRMS_Employement_Status on HRMS_Employement_Status.StatusID = HRMS_Set_Employee.StatusID
         And HRMS_Employement_Status.CompanyID = HRMS_Set_Employee.CompanyID
         And HRMS_Employement_Status.Status = 1 and HRMS_Employement_Status.IsActive = 1
   Left join SET_Department on SET_Department.DepartmentID = HRMS_SET_Employee.FK_DepartmentID
        And SET_Department.status =1
   Left join SET_Section on SET_Section.SectionID = HRMS_SET_Employee.FK_SectionID
        And SET_Section.Status = 1  and SET_Section.IsActive = 1

		 Where HRMS_Set_Employee.Status = 1 And  HRMS_Set_Employee.CompanyID =  @CompanyID
		 and HRMS_Set_Employee.IsActive = 1
{1}

		group by HRMS_Set_Employee.CompanyID, DepartmentName,
        SectionName, HRMS_Employement_Status.Description,
	    SET_Department.DepartmentID, SectionID 

union all

Select  HRMS_Set_Employee.CompanyID, DepartmentName,
        SectionName, 'Total Emp' as  Empl_Status,
        count(PersonID) as  StatusCount,
	    SET_Department.DepartmentID, SectionID
  
  From HRMS_Set_Employee 

   left Join HRMS_Employement_Status on HRMS_Employement_Status.StatusID = HRMS_Set_Employee.StatusID
         And HRMS_Employement_Status.CompanyID = HRMS_Set_Employee.CompanyID
         And HRMS_Employement_Status.Status = 1 and HRMS_Employement_Status.IsActive = 1
   Left join SET_Department on SET_Department.DepartmentID = HRMS_SET_Employee.FK_DepartmentID
        And SET_Department.status =1
   Left join SET_Section on SET_Section.SectionID = HRMS_SET_Employee.FK_SectionID
        And SET_Section.Status = 1  and SET_Section.IsActive = 1

		 Where HRMS_Set_Employee.Status = 1 And  HRMS_Set_Employee.CompanyID =  @CompanyID
		 and HRMS_Set_Employee.IsActive = 1

{1}
		group by HRMS_Set_Employee.CompanyID, DepartmentName,
        SectionName, HRMS_Employement_Status.Description,
	    SET_Department.DepartmentID, SectionID 

		union all

		
Select   Tab2.CompanyID, Tab2.DepartmentName,
        Tab2.SectionName, Tab2.Empl_Status,
       sum(StatusCount) as StatusCount,
		Tab2.DepartmentID ,Tab2.SectionID
From (

 Select  Tab.CompanyID, Tab.PersonID, Tab.DepartmentName,
        Tab.SectionName, Tab.Empl_Status,
        Count(Tab.StatusCount) as StatusCount,
		Tab.DepartmentID ,Tab.SectionID
      from (

 Select Distinct HRMS_TMSData.CompanyID, HRMS_Set_Employee.PersonID, SET_Department.DepartmentName,
        SET_Section.SectionName, 'Total Present' as Empl_Status,
        CAST(HRMS_TMSData.StatusCode as Int) as StatusCount,
		SET_Department.DepartmentID ,SET_Section.SectionID
  From HRMS_TMSData 
   Inner Join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_TMSData.PersonID 
         And  HRMS_Set_Employee.CompanyID = HRMS_TMSData.CompanyID
		 And HRMS_Set_Employee.Status = 1 And HRMS_Set_Employee.IsActive = 1
   left Join HRMS_Employement_Status on HRMS_Employement_Status.StatusID = HRMS_Set_Employee.StatusID
         And HRMS_Employement_Status.CompanyID = HRMS_Set_Employee.CompanyID
         And HRMS_Employement_Status.Status = 1 and HRMS_Employement_Status.IsActive = 1
   Left join SET_Department on SET_Department.DepartmentID = HRMS_SET_Employee.FK_DepartmentID
        And SET_Department.status =1
   Left join SET_Section on SET_Section.SectionID = HRMS_SET_Employee.FK_SectionID
        And SET_Section.Status = 1  and SET_Section.IsActive = 1
  Where HRMS_TMSData.StatusCode = 0 And  HRMS_TMSData.CompanyID =  @CompanyID
     {0}

   Group by HRMS_TMSData.CompanyID, HRMS_Set_Employee.PersonID, SET_Department.DepartmentName,
           HRMS_Employement_Status.Description,SET_Department.DepartmentID,SET_Section.SectionID,
           SET_Section.SectionName, HRMS_TMSData.StatusCode, HRMS_Set_Employee.FK_SectionID

		)Tab
   Group by Tab.CompanyID, Tab.PersonID, Tab.DepartmentName,
            Tab.SectionName, Tab.Empl_Status,Tab.DepartmentID ,Tab.SectionID

union all 

			Select  HRMS_Set_Employee.CompanyID,PersonID, DepartmentName,
        SectionName, 'Total Present' as  Empl_Status,
        0 as  StatusCount,
	    SET_Department.DepartmentID, SectionID
  
  From HRMS_Set_Employee 

   left Join HRMS_Employement_Status on HRMS_Employement_Status.StatusID = HRMS_Set_Employee.StatusID
         And HRMS_Employement_Status.CompanyID = HRMS_Set_Employee.CompanyID
         And HRMS_Employement_Status.Status = 1 and HRMS_Employement_Status.IsActive = 1
   Left join SET_Department on SET_Department.DepartmentID = HRMS_SET_Employee.FK_DepartmentID
        And SET_Department.status =1
   Left join SET_Section on SET_Section.SectionID = HRMS_SET_Employee.FK_SectionID
        And SET_Section.Status = 1  and SET_Section.IsActive = 1

		 Where HRMS_Set_Employee.Status = 1 And  HRMS_Set_Employee.CompanyID =  @CompanyID
		 and HRMS_Set_Employee.IsActive = 1
		 {1}

		group by HRMS_Set_Employee.CompanyID, DepartmentName,PersonID,
        SectionName, HRMS_Employement_Status.Description,
	    SET_Department.DepartmentID, SectionID 

	) Tab2
	Group by Tab2.CompanyID, Tab2.DepartmentName,
             Tab2.SectionName, Tab2.Empl_Status,Tab2.DepartmentID ,Tab2.SectionID

-------------------Total----------------

union all

select innerTab.CompanyID,innerTab.DepartmentName,innerTab.SectionName,'Total Leaves/Absent' as Empl_Status,sum(innerTab.StatusCount) as StatusCount,
innerTab.DepartmentID,innerTab.SectionID   from (

		Select  HRMS_Set_Employee.CompanyID, DepartmentName,
        SectionName, 'Total Emp' as  Empl_Status,
        count(PersonID) as  StatusCount,
	    SET_Department.DepartmentID, SectionID
  
  From HRMS_Set_Employee 

   left Join HRMS_Employement_Status on HRMS_Employement_Status.StatusID = HRMS_Set_Employee.StatusID
         And HRMS_Employement_Status.CompanyID = HRMS_Set_Employee.CompanyID
         And HRMS_Employement_Status.Status = 1 and HRMS_Employement_Status.IsActive = 1
   Left join SET_Department on SET_Department.DepartmentID = HRMS_SET_Employee.FK_DepartmentID
        And SET_Department.status =1
   Left join SET_Section on SET_Section.SectionID = HRMS_SET_Employee.FK_SectionID
        And SET_Section.Status = 1  and SET_Section.IsActive = 1

		 Where HRMS_Set_Employee.Status = 1 And  HRMS_Set_Employee.CompanyID =  @CompanyID
		 and HRMS_Set_Employee.IsActive = 1
{1}

		group by HRMS_Set_Employee.CompanyID, DepartmentName,
        SectionName, HRMS_Employement_Status.Description,
	    SET_Department.DepartmentID, SectionID 



		union all

		
Select   Tab2.CompanyID, Tab2.DepartmentName,
        Tab2.SectionName, Tab2.Empl_Status,
        sum(StatusCount)*-1 as StatusCount,
		Tab2.DepartmentID ,Tab2.SectionID
From (

 Select  Tab.CompanyID, Tab.PersonID, Tab.DepartmentName,
        Tab.SectionName, Tab.Empl_Status,
        Count(Tab.StatusCount) as StatusCount,
		Tab.DepartmentID ,Tab.SectionID
      from (

 Select Distinct HRMS_TMSData.CompanyID, HRMS_Set_Employee.PersonID, SET_Department.DepartmentName,
        SET_Section.SectionName, 'Total Present' as Empl_Status,
        CAST(HRMS_TMSData.StatusCode as Int) as StatusCount,
		SET_Department.DepartmentID ,SET_Section.SectionID
  From HRMS_TMSData 

   Inner Join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_TMSData.PersonID 
         And  HRMS_Set_Employee.CompanyID = HRMS_TMSData.CompanyID
		 And HRMS_Set_Employee.Status = 1 And HRMS_Set_Employee.IsActive = 1

   left Join HRMS_Employement_Status on HRMS_Employement_Status.StatusID = HRMS_Set_Employee.StatusID
         And HRMS_Employement_Status.CompanyID = HRMS_Set_Employee.CompanyID
         And HRMS_Employement_Status.Status = 1 and HRMS_Employement_Status.IsActive = 1
   Left join SET_Department on SET_Department.DepartmentID = HRMS_SET_Employee.FK_DepartmentID
        And SET_Department.status =1
   Left join SET_Section on SET_Section.SectionID = HRMS_SET_Employee.FK_SectionID
        And SET_Section.Status = 1  and SET_Section.IsActive = 1
  Where HRMS_TMSData.StatusCode = 0 And  HRMS_TMSData.CompanyID =  @CompanyID
 {0}

   Group by HRMS_TMSData.CompanyID, HRMS_Set_Employee.PersonID, SET_Department.DepartmentName,
           HRMS_Employement_Status.Description,SET_Department.DepartmentID,SET_Section.SectionID,
           SET_Section.SectionName, HRMS_TMSData.StatusCode, HRMS_Set_Employee.FK_SectionID

		)Tab
   Group by Tab.CompanyID, Tab.PersonID, Tab.DepartmentName,
            Tab.SectionName, Tab.Empl_Status,Tab.DepartmentID ,Tab.SectionID
	) Tab2
	Group by Tab2.CompanyID, Tab2.DepartmentName,
             Tab2.SectionName, Tab2.Empl_Status,Tab2.DepartmentID ,Tab2.SectionID

)innerTab

group by 
innerTab.CompanyID,innerTab.DepartmentName,innerTab.SectionName,Empl_Status,
innerTab.DepartmentID,innerTab.SectionID 


    )FinalTab
	GROUP BY FinalTab.CompanyID, FinalTab.DepartmentName, FinalTab.SectionName,
	         FinalTab.SectionName, FinalTab.Empl_Status, FinalTab.DepartmentID,FinalTab.SectionID
  Order by FinalTab.DepartmentName asc

        


                    ";
                #endregion

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_DailyStrengthReportTMS,sb,sb1), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetOverTimeDataForEmployees(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To,
         Nullable<int> DepartmentIDFrom)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);


                if (Date_From != null && Date_To != null)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                    sb.Append("  where SET_DateTime.TheDate >= @FromDate and SET_DateTime.TheDate <= @ToDate  and EP.IsActive = 1   and SET_DateTime.TheDate >= EP.JoiningDate");//  and SET_DateTime.TheDate <= isnull(EP.jobEndingDate,getdate()) 
                }
              
               if (DepartmentIDFrom != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID =  " + DepartmentIDFrom + " ) ");

                }
              
              
                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Transaction.HRMS_Appraisal.Query_AlAbbas_OverTimeData, sb, sb1, sb2), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        
        public static DataTable GetOverTimeDataForHR(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To,
         Nullable<int> DepartmentIDFrom)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (DepartmentIDFrom != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID =  " + DepartmentIDFrom + " ) ");
                }

               
                if (Date_From != null && Date_To != null)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                    sb.Append("  where SET_DateTime.TheDate >= @FromDate and SET_DateTime.TheDate <= @ToDate  and EP.IsActive = 1   and SET_DateTime.TheDate >= EP.JoiningDate");//  and SET_DateTime.TheDate <= isnull(EP.jobEndingDate,getdate()) 
                }
                               

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Transaction.HRMS_Appraisal.Query_GetOvertimeforHR, sb, sb1, sb2), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }


        public static bool GetDuplicateRawData(Nullable<int> companyID, Nullable<int> periodID, string NewRawData)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            
            bool result = false;
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@NewRawData", NewRawData);


                const string Query_GetPrvData = @" Select * from HRMS_TMSDAta where RawData =  @NewRawData ";
                
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetPrvData, sqlparams).Tables[0];
                if(dt!= null && dt.Rows.Count > 0)
                {
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

        public static DataTable GetTMS_DataForOverTIme(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> personIDFrom)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);


                if (Date_From != null && Date_To != null)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                    sb.Append("  where SET_DateTime.TheDate >= @FromDate and SET_DateTime.TheDate <= @ToDate  and EP.IsActive = 1   and SET_DateTime.TheDate >= EP.JoiningDate");//  and SET_DateTime.TheDate <= isnull(EP.jobEndingDate,getdate()) 

                }

                if (personIDFrom != null)
                {
                    sb1.Append(" and ( EP.PersonID = " + personIDFrom + " )");
                }

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Transaction.HRMS_Appraisal.Query_GetOVerAllEmpDataForOVerTime, sb, sb1, sb2), sqlparams).Tables[0];



            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataSet GetTMS_Master2_WithRoaster(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> persontype, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> personIDFrom, Nullable<int> personIDTo, Nullable<int> ShiftIDFrom, Nullable<int> ShiftIDTo,
          Nullable<int> DepartmentIDFrom, Nullable<int> DepartmentIDTo, Nullable<int> EmlpoymentType, Nullable<DateTime> JoiningDate, Nullable<DateTime> EndDate)
        {
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            StringBuilder sb3 = new StringBuilder();
            StringBuilder sb4 = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);


                if (Date_From != null && Date_To != null)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                    sb.Append("  where SET_DateTime.TheDate >= @FromDate and SET_DateTime.TheDate <= @ToDate  and EP.IsActive = 1   and SET_DateTime.TheDate >= EP.JoiningDate");//  and SET_DateTime.TheDate <= isnull(EP.jobEndingDate,getdate()) 

                    if (persontype != null)
                    {
                       // sb.Append(" and EP.StatusID = " + persontype + "");
                        sb.Append(" and ET.TypeID = " + persontype + "");
                        sb4.Append(" and ET.TypeID = " + persontype + "");
                    }
                }
                //if (Date_From != null && Date_To != null)
                //{
                //    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                //    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                //    sb.Append("  where SET_DateTime.TheDate >= @FromDate and SET_DateTime.TheDate <= @ToDate  and EP.IsActive = 1 ");
                ////and EP.PersonTypeCode =  
                //}
                //sqlparams[2] = new SqlParameter("@PersonID", personID);
                //sqlparams[3] = new SqlParameter("@ShiftID", ShiftID);
                //sqlparams[4] = new SqlParameter("@TerminalNo", TerminalNo);

                //sqlparams[7] = new SqlParameter("@DepartmentID", DepartmentID);


                //date 
                //if (Date_From != null && Date_To != null)
                //{
                //    sb.Append(" where ( cast (SET_DateTime.TheDate as date) >= " + Date_From + " and cast( SET_DateTime.TheDate  as date) <= " + Date_To + " )");
                //}


                ////person 
                if (personIDFrom != null && personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID between " + personIDFrom + " and   " + personIDTo + " )");
                    sb3.Append("and ( HRMS_LeaveRequest.Fk_PersonID between " + personIDFrom + " and   " + personIDTo + " )");
                    sb4.Append("and ( HRMS_EmployeePayroll.PersonID between " + personIDFrom + " and   " + personIDTo + " )");
                }
                else if (personIDFrom != null)
                {
                    sb1.Append(" and ( EP.PersonID = " + personIDFrom + " )");
                    sb3.Append(" and ( HRMS_LeaveRequest.Fk_PersonID = " + personIDFrom + " )");
                    sb4.Append(" and ( HRMS_EmployeePayroll.PersonID = " + personIDFrom + " )");
                }
                else if (personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID  = " + personIDTo + " )");
                    sb3.Append(" and ( HRMS_LeaveRequest.Fk_PersonID  = " + personIDTo + " )");
                    sb4.Append(" and ( HRMS_EmployeePayroll.PersonID  = " + personIDTo + " )");
                }



                ////department
                if (DepartmentIDFrom != null && DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID between " + DepartmentIDFrom + " and " + DepartmentIDTo + ") ");
                    sb4.Append(" and (EP.FK_DepartmentID between " + DepartmentIDFrom + " and " + DepartmentIDTo + ") ");

                }
                else if (DepartmentIDFrom != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID =  " + DepartmentIDFrom + " ) ");
                    sb4.Append(" and (EP.FK_DepartmentID =  " + DepartmentIDFrom + " ) ");

                }
                else if (DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID  =  " + DepartmentIDTo + ") ");
                    sb.Append(" and (EP.FK_DepartmentID  =  " + DepartmentIDTo + ") ");
                }


                //////Employemnet Status Code
                //if (EmployeementStatusCode != null)
                //{
                //    sb1.Append(" and (EP.PersonTypeCode =" +EmployeementStatusCode +" " );

                //}

                ////Employemnet Type
                if (EmlpoymentType != null)
                {
                    sb1.Append(" and EP.PersonTypeCode = " + EmlpoymentType + " ");
                    sb4.Append(" and EP.PersonTypeCode = " + EmlpoymentType + " ");

                }

                ////Joining Date
                if (JoiningDate != null)
                {
                    sqlparams[4] = new SqlParameter("@JoiningDate", JoiningDate);

                    sb1.Append(" and EP.JoiningDate = @JoiningDate");

                }


                ////End date
                if (EndDate != null)
                {
                    sqlparams[5] = new SqlParameter("@EndDate", EndDate);
                    sb1.Append(" and EP.JobEndingDate = @EndDate");

                }



                ////terminal 
                //if (TerminalNoFrom != null && TerminalNoTo != null)
                //{
                //    sb2.Append(" where (TerminalNo between " + TerminalNoFrom + " and " + TerminalNoTo + ") ");
                //}
                //else if (TerminalNoFrom != null)
                //{
                //    sb2.Append(" where (TerminalNo = " + TerminalNoFrom + ") ");
                //}
                //else if (TerminalNoTo != null)
                //{
                //    sb2.Append(" where (TerminalNo  = " + TerminalNoTo + ") ");
                //}
                ////shift
                //if (ShiftIDFrom != null && ShiftIDTo != null)
                //{


                //}
                //else if (ShiftIDFrom != null)
                //{
                //}
                //else if (ShiftIDTo != null)
                //{

                //}

                //ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Transaction.HRMS_Appraisal.Query_TMS_AlAbbasWithRoaster, sb, sb1, sb2), sqlparams);

                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Transaction.HRMS_Appraisal.Query_TMS_AlAbbasWithRoaster, sb, sb1, sb2, sb3, sb4), sqlparams);
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return ds;
        }


        public static DataSet GetTMS_Master2_Attd_Monthwise(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> persontype, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> personIDFrom, Nullable<int> personIDTo, Nullable<int> ShiftIDFrom, Nullable<int> ShiftIDTo,
          Nullable<int> DepartmentIDFrom, Nullable<int> DepartmentIDTo, Nullable<int> EmlpoymentType, Nullable<DateTime> JoiningDate, Nullable<DateTime> EndDate,  int LocationID = 0)
        {
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            StringBuilder sb3 = new StringBuilder();
            StringBuilder sb4 = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);


                if (Date_From != null && Date_To != null)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                    sb.Append("  where SET_DateTime.TheDate >= @FromDate and SET_DateTime.TheDate <= @ToDate  and EP.IsActive = 1   and SET_DateTime.TheDate >= EP.JoiningDate");//  and SET_DateTime.TheDate <= isnull(EP.jobEndingDate,getdate()) 

                    if (persontype != null)
                    {
                        // sb.Append(" and EP.StatusID = " + persontype + "");
                        sb.Append(" and ET.TypeID = " + persontype + "");
                        sb4.Append(" and ET.TypeID = " + persontype + "");
                        sb3.Append(" and ET.TypeID = " + persontype + "");
                    }
                }
                //if (Date_From != null && Date_To != null)
                //{
                //    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                //    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                //    sb.Append("  where SET_DateTime.TheDate >= @FromDate and SET_DateTime.TheDate <= @ToDate  and EP.IsActive = 1 ");
                ////and EP.PersonTypeCode =  
                //}
                //sqlparams[2] = new SqlParameter("@PersonID", personID);
                //sqlparams[3] = new SqlParameter("@ShiftID", ShiftID);
                //sqlparams[4] = new SqlParameter("@TerminalNo", TerminalNo);

                //sqlparams[7] = new SqlParameter("@DepartmentID", DepartmentID);


                //date 
                //if (Date_From != null && Date_To != null)
                //{
                //    sb.Append(" where ( cast (SET_DateTime.TheDate as date) >= " + Date_From + " and cast( SET_DateTime.TheDate  as date) <= " + Date_To + " )");
                //}


                ////person 
                if (personIDFrom != null && personIDTo != null)
                {
                    sb1.Append(" and ( cast(EP.EmployeeCode as int) between " + personIDFrom + " and   " + personIDTo + " )");
                    sb3.Append("and ( cast(EP.EmployeeCode as int) between " + personIDFrom + " and   " + personIDTo + " )");
                    sb4.Append("and ( cast(EP.EmployeeCode as int) between " + personIDFrom + " and   " + personIDTo + " )");
                }
                else if (personIDFrom != null)
                {
                    sb1.Append(" and ( cast(EP.EmployeeCode as int) = " + personIDFrom + " )");
                    sb3.Append(" and ( cast(EP.EmployeeCode as int) = " + personIDFrom + " )");
                    sb4.Append(" and ( cast(EP.EmployeeCode as int) = " + personIDFrom + " )");
                }
                else if (personIDTo != null)
                {
                    sb1.Append(" and ( cast(EP.EmployeeCode as int)  = " + personIDTo + " )");
                    sb3.Append(" and ( cast(EP.EmployeeCode as int)  = " + personIDTo + " )");
                    sb4.Append(" and ( cast(EP.EmployeeCode as int)  = " + personIDTo + " )");
                }



                ////department
                if (DepartmentIDFrom != null && DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID between " + DepartmentIDFrom + " and " + DepartmentIDTo + ") ");
                    sb4.Append(" and (EP.FK_DepartmentID between " + DepartmentIDFrom + " and " + DepartmentIDTo + ") ");

                }
                else if (DepartmentIDFrom != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID =  " + DepartmentIDFrom + " ) ");
                    sb4.Append(" and (EP.FK_DepartmentID =  " + DepartmentIDFrom + " ) ");

                }
                else if (DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID  =  " + DepartmentIDTo + ") ");
                    sb.Append(" and (EP.FK_DepartmentID  =  " + DepartmentIDTo + ") ");
                }

                if (LocationID > 0)
                {
                    sb1.Append(" and EP.FK_LocationID = " + LocationID);
                }


                //////Employemnet Status Code
                //if (EmployeementStatusCode != null)
                //{
                //    sb1.Append(" and (EP.PersonTypeCode =" +EmployeementStatusCode +" " );

                //}

                ////Employemnet Type
                if (EmlpoymentType != null)
                {
                    sb1.Append(" and EP.PersonTypeCode = " + EmlpoymentType + " ");
                    sb4.Append(" and EP.PersonTypeCode = " + EmlpoymentType + " ");

                }

                ////Joining Date
                if (JoiningDate != null)
                {
                    sqlparams[4] = new SqlParameter("@JoiningDate", JoiningDate);

                    sb1.Append(" and EP.JoiningDate = @JoiningDate");

                }


                ////End date
                if (EndDate != null)
                {
                    sqlparams[5] = new SqlParameter("@EndDate", EndDate);
                    sb1.Append(" and EP.JobEndingDate = @EndDate");

                }


                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Transaction.HRMS_Appraisal.Query_AttendanceRegister_Monthwise, sb, sb1, sb2, sb3, sb4), sqlparams);
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return ds;
        }

        public static DataSet GetTMS_Master2_AttdPayOrderReport_Monthwise(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> persontype, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> personIDFrom, Nullable<int> personIDTo, Nullable<int> ShiftIDFrom, Nullable<int> ShiftIDTo,
          Nullable<int> DepartmentIDFrom, Nullable<int> DepartmentIDTo, Nullable<int> EmlpoymentType, Nullable<DateTime> JoiningDate, Nullable<DateTime> EndDate, int LocationID = 0)
        {
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            StringBuilder sb3 = new StringBuilder();
            StringBuilder sb4 = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);


                if (Date_From != null && Date_To != null)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                    sb.Append("  where SET_DateTime.TheDate >= @FromDate and SET_DateTime.TheDate <= @ToDate  and EP.IsActive = 1   and SET_DateTime.TheDate >= EP.JoiningDate");//  and SET_DateTime.TheDate <= isnull(EP.jobEndingDate,getdate()) 

                    if (persontype != null)
                    {
                        // sb.Append(" and EP.StatusID = " + persontype + "");
                        sb.Append(" and ET.TypeID = " + persontype + "");
                        sb4.Append(" and ET.TypeID = " + persontype + "");
                    }
                }
                //if (Date_From != null && Date_To != null)
                //{
                //    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                //    sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                //    sb.Append("  where SET_DateTime.TheDate >= @FromDate and SET_DateTime.TheDate <= @ToDate  and EP.IsActive = 1 ");
                ////and EP.PersonTypeCode =  
                //}
                //sqlparams[2] = new SqlParameter("@PersonID", personID);
                //sqlparams[3] = new SqlParameter("@ShiftID", ShiftID);
                //sqlparams[4] = new SqlParameter("@TerminalNo", TerminalNo);

                //sqlparams[7] = new SqlParameter("@DepartmentID", DepartmentID);


                //date 
                //if (Date_From != null && Date_To != null)
                //{
                //    sb.Append(" where ( cast (SET_DateTime.TheDate as date) >= " + Date_From + " and cast( SET_DateTime.TheDate  as date) <= " + Date_To + " )");
                //}


                ////person 
                if (personIDFrom != null && personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID between " + personIDFrom + " and   " + personIDTo + " )");
                    sb3.Append("and ( HRMS_LeaveRequest.Fk_PersonID between " + personIDFrom + " and   " + personIDTo + " )");
                    sb4.Append("and ( HRMS_EmployeePayroll.PersonID between " + personIDFrom + " and   " + personIDTo + " )");
                }
                else if (personIDFrom != null)
                {
                    sb1.Append(" and ( EP.PersonID = " + personIDFrom + " )");
                    sb3.Append(" and ( HRMS_LeaveRequest.Fk_PersonID = " + personIDFrom + " )");
                    sb4.Append(" and ( HRMS_EmployeePayroll.PersonID = " + personIDFrom + " )");
                }
                else if (personIDTo != null)
                {
                    sb1.Append(" and ( EP.PersonID  = " + personIDTo + " )");
                    sb3.Append(" and ( HRMS_LeaveRequest.Fk_PersonID  = " + personIDTo + " )");
                    sb4.Append(" and ( HRMS_EmployeePayroll.PersonID  = " + personIDTo + " )");
                }



                ////department
                if (DepartmentIDFrom != null && DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID between " + DepartmentIDFrom + " and " + DepartmentIDTo + ") ");
                    sb4.Append(" and (EP.FK_DepartmentID between " + DepartmentIDFrom + " and " + DepartmentIDTo + ") ");

                }
                else if (DepartmentIDFrom != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID =  " + DepartmentIDFrom + " ) ");
                    sb4.Append(" and (EP.FK_DepartmentID =  " + DepartmentIDFrom + " ) ");

                }
                else if (DepartmentIDTo != null)
                {
                    sb1.Append(" and (EP.FK_DepartmentID  =  " + DepartmentIDTo + ") ");
                    sb.Append(" and (EP.FK_DepartmentID  =  " + DepartmentIDTo + ") ");
                }

                if (LocationID > 0)
                {
                    sb1.Append(" and EP.FK_LocationID = " + LocationID);
                }


                //////Employemnet Status Code
                //if (EmployeementStatusCode != null)
                //{
                //    sb1.Append(" and (EP.PersonTypeCode =" +EmployeementStatusCode +" " );

                //}

                ////Employemnet Type
                if (EmlpoymentType != null)
                {
                    sb1.Append(" and EP.PersonTypeCode = " + EmlpoymentType + " ");
                    sb4.Append(" and EP.PersonTypeCode = " + EmlpoymentType + " ");

                }

                ////Joining Date
                if (JoiningDate != null)
                {
                    sqlparams[4] = new SqlParameter("@JoiningDate", JoiningDate);

                    sb1.Append(" and EP.JoiningDate = @JoiningDate");

                }


                ////End date
                if (EndDate != null)
                {
                    sqlparams[5] = new SqlParameter("@EndDate", EndDate);
                    sb1.Append(" and EP.JobEndingDate = @EndDate");

                }


                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Transaction.HRMS_Appraisal.Query_AttendanceRegister_Monthwise, sb, sb1, sb2, sb3, sb4), sqlparams);
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return ds;
        }

        public static bool Get_ValidateRoasterExecutedForMonth(DateTime toDate)
        {
            bool result = false;

            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@ToDate", toDate);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Validate_RoasterExecutedForMonth, sqlparams).Tables[0];

                if (dt.Rows.Count > 0)
                {
                    result = Convert.ToBoolean(dt.Rows[0]["Status"]);
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataTable GetLoanRegisterEmployeeMonthlyWise(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> personIDFrom, Nullable<int> personIDTo,
          Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> EmployeeStatusID, int EmployeeTypeID = 0, bool Active = false, bool inActive = false)
        {
            DataTable dt = null;
            StringBuilder sb1 = new StringBuilder();

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (Date_From != null && Date_To != null)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                    sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                }

                ////person 
                if (personIDFrom != null && personIDTo != null)
                {
                    sb1.Append(" and ( HRMS_Set_Employee.PersonID between " + personIDFrom + " and " + personIDTo + " )");
                }
                else if (personIDFrom != null)
                {
                    sb1.Append(" and ( HRMS_Set_Employee.PersonID = " + personIDFrom + " )");
                }
                else if (personIDTo != null)
                {
                    sb1.Append(" and ( HRMS_Set_Employee.PersonID  = " + personIDTo + " )");
                }

                if (EmployeeStatusID > 0)
                {
                    sb1.Append(" AND HRMS_Set_Employee.StatusID = " + EmployeeStatusID);
                }
                if (EmployeeTypeID > 0)
                {
                    sb1.Append(" and HRMS_Set_Employee.PersonTypeCode = " + EmployeeTypeID);
                }

                if (Active == false && inActive == true)
                {
                    sb1.Append(" and HRMS_Set_Employee.isActive = 0");
                }
                else if (Active == true && inActive == false)
                {
                    sb1.Append(" and HRMS_Set_Employee.isActive = 1");
                }


                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Transaction.HRMS_Appraisal.Query_EmployeeLoanRegisterMonthlyWiseSummary, sb1.ToString()), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static string GetStatusCode(string Description,SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            string statuscode = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@Description", Description);

                statuscode = (SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_StatusCode, sqlparams)).ToString();
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return statuscode;
        }

        public static DataTable GetTMS_Data_Against_PersonID(short statuscode, Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<int> personIDFrom, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TMSDateTime", Date_From);
                sqlparams[3] = new SqlParameter("@PersonID", personIDFrom);
                sqlparams[4] = new SqlParameter("@StatusCode", statuscode);

                const string Query_GetData = @" Select * from HRMS_TMSDAta 
where CompanyID = @CompanyID and PeriodID = @PeriodID
and convert(date,TMSDateTime) = @TMSDateTime
and PersonID = @PersonID
and StatusCode = @StatusCode
order by TMSDateTime desc";

               // dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Transaction.HRMS_Appraisal.Query_GetOVerAllEmpDataForOVerTime, sb, sb1, sb2), sqlparams).Tables[0];
               // dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData, sqlparams).Tables[0];
                if (objTransaction == null)
                {
                   dt =  SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData, sqlparams).Tables[0];
                        
                }
                else
                {
                    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData, sqlparams).Tables[0];
                }

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetAllTMSDataFORSearch(Nullable<int> PersnType, string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null, Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, SqlTransaction objTransaction = null)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            sb.Append(strWhere);
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
                    //if (LocationID > 0)
                    //{
                    //    sb.Append(" AND HRMS_Set_Employee.FK_LocationID = " + LocationID);
                    //}

                    //if (DivID > 0)
                    //{
                    //    sb.Append(" AND HRMS_Set_Employee.DivisionID = " + DivID);
                    //}

                    if (PersnType > 0)
                    {
                        sb.Append(" And HRMS_Set_Employee.PersonTypeCode = " + PersnType);
                    }

                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, QueryFOrTMSDataSearching_Detail + sb + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), QueryFOrTMSDataSearching_Detail + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, QueryFOrTMSDataSearching_Detail + sb + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), QueryFOrTMSDataSearching_Detail + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    //if (LocationID > 0)
                    //{
                    //    strWhere = strWhere + (" AND HRMS_Set_Employee.FK_LocationID = " + LocationID);
                    //}

                    //if (DivID > 0)
                    //{
                    //    strWhere = strWhere + (" AND HRMS_Set_Employee.DivisionID = " + DivID);
                    //}
                    //if (Personid > 0)
                    //{
                    //    sb.Append(" And HRMS_TMSData.PersonID = " + PersnType);
                    //}
                    if (PersnType > 0)
                    {
                        strWhere = strWhere + (" And HRMS_Set_Employee.PersonTypeCode = " + PersnType);
                    }

                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, QueryFOrTMSDataSearching_Detail_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, QueryFOrTMSDataSearching_Detail_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetSearchAllTMSDatatForDelete(Nullable<int> PersnType, string strWhere, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {

            DataTable dt = null;
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            sb.Append(strWhere);
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                //sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                //sqlparams[0] = new SqlParameter("@CompanyID", periodID);
                //sqlparams[1] = new SqlParameter("@PeriodID", PersonID);
                //sqlparams[2] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                //sqlparams[3] = new SqlParameter("@Year", Date_From.Value.Year.ToString());
                //if (LocationID > 0)
                //{
                //    sb.Append(" AND HRMS_Set_Employee.FK_LocationID = " + LocationID);
                //}

                //if (DivID > 0)
                //{
                //    sb.Append(" AND HRMS_Set_Employee.DivisionID = " + DivID);
                //}
                if (PersnType > 0)
                {
                    sb.Append(" And HRMS_Set_Employee.PersonTypeCode = " + PersnType);
                }
                if (objTransaction != null)
                    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(QueryFOrTMSSearching_Detail + strWhere), sqlparams).Tables[0];
                else if (objConnection != null)
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(QueryFOrTMSSearching_Detail + strWhere), sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(QueryFOrTMSSearching_Detail + strWhere), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static bool DeleteTMSDataHeaderSelected(int companyID, int periodID, string month, string year, int Personid, SqlTransaction objTransaction)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Month", month);
                sqlparams[3] = new SqlParameter("@Year", year);
                sqlparams[4] = new SqlParameter("@PersonID", Personid);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteTMSData_HeaderSelected, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DeleteTMSData_HeaderSelected, sqlparams) > 0)
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

        public static bool DeleteTMSDataSelected(int companyID, int periodID, string month, string year, int personid, bool DeleteAll, SqlTransaction objTransaction)
        {
            bool result = false;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Month", month);
                sqlparams[3] = new SqlParameter("@Year", year);
                //sqlparams[4] = new SqlParameter("@HRMS_TMSData", tmsid);

                if (DeleteAll == false)
                    sb.Append(" and HRMS_TMSData.PersonID = " + personid);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, string.Format(Query_DeleteTMSData_AttendenceSelected_SalDel, sb.ToString()), sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, string.Format(Query_DeleteTMSData_AttendenceSelected_SalDel, sb.ToString()), sqlparams) > 0)
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


        public static DataTable CheckStatusDataFromSalaryHeader(int companyID, int periodID, int PersonID, string month, string year, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {

            DataTable dt = null;
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", PersonID);
                sqlparams[3] = new SqlParameter("@Month", month);
                sqlparams[4] = new SqlParameter("@Year", year);

                #region Query
                const string Query_forStatusCheckfromSalaryHeader = @"
    Select * from HRMS_SalaryHeader
       where HRMS_SalaryHeader.Status = 1 and HRMS_SalaryHeader.CompanyID = @CompanyID
	      and HRMS_SalaryHeader.PeriodID = @PeriodID and  PersonID = @PersonID
	      and datepart(month,HRMS_SalaryHeader.SalaryDate) = @Month 
		 and datename(year,HRMS_SalaryHeader.SalaryDate) = @Year
                             ";
                #endregion

                //sqlparams[2] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                //sqlparams[3] = new SqlParameter("@Year", Date_From.Value.Year.ToString());
                if (objTransaction != null)
                    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_forStatusCheckfromSalaryHeader + sb), sqlparams).Tables[0];
                else if (objConnection != null)
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_forStatusCheckfromSalaryHeader + sb), sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_forStatusCheckfromSalaryHeader + sb), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataRow Get_PersonIDFromShhiftTable(int companyID, int periodID, int headerID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {

            DataRow dr = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@HeaderID", headerID);

                #region Query
                const string Query_ForGetPersonIDfromshiftTable = @"
       Select * from HRMS_AssignShiftsHeader
       where HRMS_AssignShiftsHeader.Status = 1 and HRMS_AssignShiftsHeader.CompanyID = @CompanyID
	   and HRMS_AssignShiftsHeader.PeriodID = @PeriodID and  HRMS_AssignShiftsHeader.HeaderID = @HeaderID ";
                #endregion
                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_ForGetPersonIDfromshiftTable, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_ForGetPersonIDfromshiftTable, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_ForGetPersonIDfromshiftTable, sqlparams);

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

        public static DataTable CheckStatusDataFromSalaryTable(int companyID, int periodID, int personID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {

            DataTable dt = null;
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", personID);
                sqlparams[3] = new SqlParameter("@FromDate", Date_From);
                sqlparams[4] = new SqlParameter("@ToDate", Date_To);

                #region Query
                const string Query_forStatusCheckdatafromSalaryHeader = @"
      Select * from HRMS_SalaryHeader
      where HRMS_SalaryHeader.Status = 1 And HRMS_SalaryHeader.CompanyID = @CompanyID
        and HRMS_SalaryHeader.PeriodID = @PeriodID
        and HRMS_SalaryHeader.PersonID = @PersonID
       -- and datepart(month,HRMS_SalaryHeader.SalaryDate) = @Month 
		-- and datename(year,HRMS_SalaryHeader.SalaryDate) = @Year
        and HRMS_SalaryHeader.SalaryDate between @FromDate and @ToDate
      ";
                #endregion

                if (objTransaction != null)
                    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_forStatusCheckdatafromSalaryHeader + sb), sqlparams).Tables[0];
                else if (objConnection != null)
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_forStatusCheckdatafromSalaryHeader + sb), sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_forStatusCheckdatafromSalaryHeader + sb), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetExcessEmployeeData(Nullable<int> PersonID, Nullable<int> companyID, Nullable<int> periodID, Nullable<int> FromDay, Nullable<int> ToDay, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", PersonID);
                sqlparams[3] = new SqlParameter("@FromDay", FromDay);
                sqlparams[4] = new SqlParameter("@ToDay", ToDay);

                string Query_SelectAll = @"
Select HRMS_TMSData.PersonID, COUNT(HRMS_TMSData.PersonID)as TotalDays, Min(TMSDateTime) As FirstAttendanceDate, Max(TMSDateTime) As LastAttendanceDate, HRMS_TMSData.PersonCode, HRMS_Set_Employee.FullName as EmployeeName
From HRMS_TMSData
Inner Join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_TMSData.PersonID
And HRMS_Set_Employee.CompanyID = HRMS_TMSData.CompanyID
And HRMS_Set_Employee.Status = 1
And HRMS_TMSData.StatusCode = 1
Inner Join HRMS_Employement_Status on HRMS_Employement_Status.StatusID = HRMS_Set_Employee.StatusID
And HRMS_Employement_Status.CompanyID = HRMS_Set_Employee.CompanyID
Where HRMS_TMSData.CompanyID = @CompanyID
And HRMS_TMSData.PeriodID = @PeriodID
And HRMS_Employement_Status.Description like '%daily%wages%'
And HRMS_TMSData.PersonID = @PersonID
Group by  HRMS_TMSData.PersonCode, HRMS_Set_Employee.FullName,HRMS_TMSData.PersonID
Having COUNT(HRMS_TMSData.PersonID) >= @FromDay and COUNT(HRMS_TMSData.PersonID) <= @ToDay
                ";

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAll, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAll, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll, sqlparams);

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

        public static DataTable GetCheckInOut(int companyID, int periodID, DateTime fromdate, DateTime todate, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            try
            {

                const string Query = @"Select CHECKINOUT.*,HRMS_Set_Employee.PersonID,HRMS_Set_Employee.OldEmployeeCode as EmployeeCode From CHECKINOUT 
INNER JOIN USERINFO ON USERINFO.USERID = CHECKINOUT.USERID
INNER JOIN HRMS_Set_Employee ON Cast(HRMS_Set_Employee.OldEmployeeCode as int) = Cast(USERINFO.BADGENUMBER as int)
where CAST(CheckTime as Date) Between @FromDate And @ToDate";

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromdate);
                sqlparams[3] = new SqlParameter("@ToDate", todate);

                if (objTransaction != null)
                    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query + sb), sqlparams).Tables[0];
                else if (objConnection != null)
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query + sb), sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query + sb), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
 
        }

        public static DataTable GetDataByPersonIDandDate_DR(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> PersonID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, string Query_GetAllTMSByPersonID, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                //sqlparams[2] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                //sqlparams[3] = new SqlParameter("@Year", Date_From.Value.Year.ToString());
                sqlparams[4] = new SqlParameter("@PersonID", PersonID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetAllTMSByPersonID, sqlparams);

                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllTMSByPersonID, sqlparams);

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

        public static DataTable DeleteDataandgetfromSelectedDate_DR(int companyID, int periodID, DateTime fromdate, DateTime todate, string UserID, string Username, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {

            DataTable dt = null;
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromdate);
                sqlparams[3] = new SqlParameter("@ToDate", todate);
                sqlparams[4] = new SqlParameter("@UserID", UserID);
                sqlparams[5] = new SqlParameter("@Username", Username);

                SqlHelper.ExecuteNonQuery(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Transaction.HRMS_Appraisal.Query_forAttendenceDataDeletequery_New.ToString() + sb), sqlparams);

                if (objTransaction != null)
                    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Transaction.HRMS_Appraisal.Query_forAttendenceDataDeletequery_New.ToString() + sb), sqlparams).Tables[0];
                else if (objConnection != null)
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Transaction.HRMS_Appraisal.Query_forAttendenceDataDeletequery_New.ToString() + sb), sqlparams).Tables[0];
                else
                    SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Transaction.HRMS_Appraisal.Query_forAttendenceDataDeletequery_New.ToString() + sb), sqlparams);


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
