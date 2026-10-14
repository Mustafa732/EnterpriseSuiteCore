using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;
using Common.Utils;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Transaction
{
    public partial class HRMS_EmployeeAttendance
    {

        #region Constant Strings

        const string Query_GetEntitledAndAvailLeaveTypeSumry = @"
	     Select  
	     TAB.leaveDays
	    ,
	    TAB.PersonID
	    ,TAB.LeavelID
	    ,TAB.PayrollTypeID
	    ,TAB.PayrollTypeCode
	    ,TAB.Description
	    , cast( isnull( SUM(TAB.Quantity),0) as int) AS QUANTITY
	    , cast (isnull (TAB.leaveDays - SUM(QUANTITY),0 ) as int) as Balance
	    from 
      (
	   select distinct    
 HRMS_EmployeePayroll.leaveDays
  ,HRMS_PayrollType.PayrollTypeID as LeavelID
, HRMS_EmployeePayroll.PersonID
	    ,HRMS_PayrollType.PayrollTypeID
	    ,HRMS_PayrollType.PayrollTypeCode
	    ,HRMS_PayrollType.Description
,isnull(sum(HRMS_EmployeeAttendance.Quantity),0) as Quantity
 from HRMS_EmployeePayroll

	    left join HRMS_EmployeeAttendance on
	    HRMS_EmployeePayroll.PersonID = HRMS_EmployeeAttendance.PersonID and
	    HRMS_EmployeePayroll.PayrollTypeID = HRMS_EmployeeAttendance.LeaveID and
	    HRMS_EmployeePayroll.PayrollTypeCode = HRMS_EmployeePayroll.PayrollTypeCode 
	     and
HRMS_EmployeePayroll.CompanyID = HRMS_EmployeeAttendance.CompanyID 
	    and (HRMS_EmployeePayroll.IsOneTime is null or HRMS_EmployeePayroll.IsOneTime=0)   
	     and HRMS_EmployeePayroll.Status = 1 
	     and HRMS_EmployeePayroll.IsActive = 1
	    and( cast (HRMS_EmployeeAttendance.MonthYear as datetime) between @FromDate AND @ToDate) 
		
	    inner join HRMS_PayrollType on 
	    --HRMS_PayrollType.CompanyID = HRMS_EmployeePayroll.CompanyID and
	    HRMS_PayrollType.PayrollTypeID= HRMS_EmployeePayroll.PayrollTypeID
	    and HRMS_EmployeePayroll.[Status] = 1 
	    
	    WHERE 
	    HRMS_EmployeePayroll.CompanyID = @CompanyID AND 
	    HRMS_EmployeePayroll.PersonID = @PersonID AND
		HRMS_EmployeePayroll.Status = 1
		and HRMS_EmployeePayroll.PayrollTypeCode = @PayrollTypeCOde
         AND (HRMS_EmployeePayroll.FiscalID=@CurrentFiscalID OR @CurrentFiscalID is null)
	    group by 
	     HRMS_EmployeePayroll.leaveDays,
	     HRMS_EmployeePayroll.PersonID
	    ,HRMS_PayrollType.PayrollTypeID 
	    ,HRMS_PayrollType.PayrollTypeID
	   , HRMS_PayrollType.PayrollTypeCode
	    ,HRMS_PayrollType.Description
	    
	    union all
	
	      --CASE 0
	      Select
	     HRMS_EmployeePayroll.leaveDays,
	      HRMS_PayrollType.PayrollTypeID as LeavelID,
	       HRMS_LeaveRequest.FK_PersonID as PersonID,
	      HRMS_PayrollType.PayrollTypeID,
	      HRMS_PayrollType.PayrollTypeCode,
	      HRMS_PayrollType.Description,	      
	      isnull (sum(DATEDIFF(dd,HRMS_LeaveRequest.FromDate,HRMS_LeaveRequest.ToDate) + 1),0) as Quantity
	  
	      from HRMS_LeaveRequest
	  
	      inner join HRMS_EmployeePayroll on
	      HRMS_EmployeePayroll.PersonID = HRMS_LeaveRequest.Fk_PersonID and
	      HRMS_EmployeePayroll.PayrollTypeID = HRMS_LeaveRequest.FK_PayrollTypeID and
	      HRMS_EmployeePayroll.PayrollTypeCode = HRMS_LeaveRequest.FK_PayrollTypeCode and
	      HRMS_EmployeePayroll.CompanyID = HRMS_LeaveRequest.CompanyID and 
	      (HRMS_EmployeePayroll.IsOneTime is null or HRMS_EmployeePayroll.IsOneTime=0)  and HRMS_EmployeePayroll.Status = 1 and
	      HRMS_EmployeePayroll.IsActive = 1
		 AND (HRMS_EmployeePayroll.FiscalID=@CurrentFiscalID OR @CurrentFiscalID is null)
	      inner join HRMS_PayrollType on 
	     -- HRMS_PayrollType.CompanyID = HRMS_LeaveRequest.CompanyID and
	      HRMS_PayrollType.PayrollTypeID = HRMS_LeaveRequest.FK_PayrollTypeID and
	      HRMS_PayrollType.PayrollTypeCode = HRMS_LeaveRequest.FK_PayrollTypeCode and
	      HRMS_PayrollType.[Status] = 1 and HRMS_PayrollType.IsActive = 1
	    INNER join Set_PeriodCustom on HRMS_LeaveRequest.FromDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
		and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID

	      where HRMS_LeaveRequest.Fk_PersonID = @PersonID and HRMS_LeaveRequest.FK_PayrollTypeCode = @PayrollTypeCode 
	      and (HRMS_LeaveRequest.FromDate >= @FromDate and HRMS_LeaveRequest.ToDate <= @ToDate)
	      and HRMS_LeaveRequest.[Status]=1 and HRMS_LeaveRequest.[IsApproved]=@IsActive
	  
	      group by
	      HRMS_LeaveRequest.FK_PersonID,
	      HRMS_PayrollType.PayrollTypeID,
	      HRMS_PayrollType.PayrollTypeCode,
	      HRMS_PayrollType.Description,
	      HRMS_EmployeePayroll.leaveDays
	  
	      union
	  
	      --CASE 1
	      Select 
	      HRMS_EmployeePayroll.leaveDays,
	       HRMS_PayrollType.PayrollTypeID as LeavelID,
	      HRMS_LeaveRequest.FK_PersonID  as PersonID,	     
	      HRMS_PayrollType.PayrollTypeID,
	      HRMS_PayrollType.PayrollTypeCode,
	      HRMS_PayrollType.Description,	      
	      isnull( sum(DATEDIFF(dd,@FromDate,HRMS_LeaveRequest.ToDate) + 1),0) as Quantity
	  
	      from HRMS_LeaveRequest
	  
	      inner join HRMS_EmployeePayroll on
	      HRMS_EmployeePayroll.PersonID = HRMS_LeaveRequest.Fk_PersonID and
	      HRMS_EmployeePayroll.PayrollTypeID = HRMS_LeaveRequest.FK_PayrollTypeID and
	      HRMS_EmployeePayroll.PayrollTypeCode = HRMS_LeaveRequest.FK_PayrollTypeCode and
	      HRMS_EmployeePayroll.CompanyID = HRMS_LeaveRequest.CompanyID and 
	      (HRMS_EmployeePayroll.IsOneTime is null or HRMS_EmployeePayroll.IsOneTime=0)  and HRMS_EmployeePayroll.Status = 1 and
	      HRMS_EmployeePayroll.IsActive = 1
	    AND (HRMS_EmployeePayroll.FiscalID=@CurrentFiscalID OR @CurrentFiscalID is null)
	      inner join HRMS_PayrollType on 
	      --HRMS_PayrollType.CompanyID = HRMS_LeaveRequest.CompanyID and
	      HRMS_PayrollType.PayrollTypeID = HRMS_LeaveRequest.FK_PayrollTypeID and
	      HRMS_PayrollType.PayrollTypeCode = HRMS_LeaveRequest.FK_PayrollTypeCode and
	      HRMS_PayrollType.[Status] = 1 and HRMS_PayrollType.IsActive = 1
	    INNER join Set_PeriodCustom on HRMS_LeaveRequest.FromDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
		and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID

	      where HRMS_LeaveRequest.Fk_PersonID = @PersonID and HRMS_LeaveRequest.FK_PayrollTypeCode = @PayrollTypeCode
	      and (HRMS_LeaveRequest.FromDate < @FromDate and (HRMS_LeaveRequest.ToDate between @FromDate and @ToDate))
	      and HRMS_LeaveRequest.[Status]=1 and HRMS_LeaveRequest.[IsApproved]=@IsActive
	  
	      group by
	      HRMS_LeaveRequest.FK_PersonID,
	      HRMS_PayrollType.PayrollTypeID,
	      HRMS_PayrollType.PayrollTypeCode,
	      HRMS_PayrollType.Description,
	      HRMS_EmployeePayroll.leaveDays
	  
	      union
	  
	      --CASE 2
	      Select 
	       HRMS_EmployeePayroll.leaveDays,
	       HRMS_PayrollType.PayrollTypeID as LeavelID,
	      HRMS_LeaveRequest.FK_PersonID  as PersonID,
	      
	      HRMS_PayrollType.PayrollTypeID,
	      HRMS_PayrollType.PayrollTypeCode,
	      HRMS_PayrollType.Description,
	     
	     isnull( sum(DATEDIFF(dd,HRMS_LeaveRequest.FromDate,@ToDate) + 1),0) as Quantity
	      --'0' as Quantity
	  
	      from HRMS_LeaveRequest
	  
	      inner join HRMS_EmployeePayroll on
	      HRMS_EmployeePayroll.PersonID = HRMS_LeaveRequest.Fk_PersonID and
	      HRMS_EmployeePayroll.PayrollTypeID = HRMS_LeaveRequest.FK_PayrollTypeID and
	      HRMS_EmployeePayroll.PayrollTypeCode = HRMS_LeaveRequest.FK_PayrollTypeCode and
	      HRMS_EmployeePayroll.CompanyID = HRMS_LeaveRequest.CompanyID and 
	      (HRMS_EmployeePayroll.IsOneTime is null or HRMS_EmployeePayroll.IsOneTime=0)  and HRMS_EmployeePayroll.Status = 1 and
	      HRMS_EmployeePayroll.IsActive = 1
	    AND (HRMS_EmployeePayroll.FiscalID=@CurrentFiscalID OR @CurrentFiscalID is null)
	      inner join HRMS_PayrollType on 
	      --HRMS_PayrollType.CompanyID = HRMS_LeaveRequest.CompanyID and
	      HRMS_PayrollType.PayrollTypeID = HRMS_LeaveRequest.FK_PayrollTypeID and
	      HRMS_PayrollType.PayrollTypeCode = HRMS_LeaveRequest.FK_PayrollTypeCode and
	      HRMS_PayrollType.[Status] = 1 and HRMS_PayrollType.IsActive = 1
	    INNER join Set_PeriodCustom on HRMS_LeaveRequest.FromDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
		and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID
	      where HRMS_LeaveRequest.Fk_PersonID = @PersonID and HRMS_LeaveRequest.FK_PayrollTypeCode = @PayrollTypeCode
	      and (HRMS_LeaveRequest.ToDate > @ToDate and (HRMS_LeaveRequest.FromDate between @FromDate and @ToDate))
	      and HRMS_LeaveRequest.[Status]=1 and HRMS_LeaveRequest.[IsApproved]=@IsActive
	  
	      group by
	      HRMS_LeaveRequest.FK_PersonID,
	      HRMS_PayrollType.PayrollTypeID,
	      HRMS_PayrollType.PayrollTypeCode,
	      HRMS_PayrollType.Description,
	      HRMS_EmployeePayroll.leaveDays
	  
	      union
	  
	      --CASE 3
	      Select 
	       HRMS_EmployeePayroll.leaveDays,
	     
	      HRMS_PayrollType.PayrollTypeID as LeavelID,
	       HRMS_LeaveRequest.FK_PersonID  as PersonID,
	      HRMS_PayrollType.PayrollTypeID,
	      HRMS_PayrollType.PayrollTypeCode,
	      HRMS_PayrollType.Description,
	     
	     isnull( sum(DATEDIFF(dd,@FromDate,@ToDate) + 1),0) as Quantity
	      --'0' as Quantity
	  
	      from HRMS_LeaveRequest
	  
	      inner join HRMS_EmployeePayroll on
	      HRMS_EmployeePayroll.PersonID = HRMS_LeaveRequest.Fk_PersonID and
	      HRMS_EmployeePayroll.PayrollTypeID = HRMS_LeaveRequest.FK_PayrollTypeID and
	      HRMS_EmployeePayroll.PayrollTypeCode = HRMS_LeaveRequest.FK_PayrollTypeCode and
	      HRMS_EmployeePayroll.CompanyID = HRMS_LeaveRequest.CompanyID and 
	      (HRMS_EmployeePayroll.IsOneTime is null or HRMS_EmployeePayroll.IsOneTime=0)  and HRMS_EmployeePayroll.Status = 1 and
	      HRMS_EmployeePayroll.IsActive = 1
	    AND (HRMS_EmployeePayroll.FiscalID=@CurrentFiscalID OR @CurrentFiscalID is null)
	      inner join HRMS_PayrollType on 
	      --HRMS_PayrollType.CompanyID = HRMS_LeaveRequest.CompanyID and
	      HRMS_PayrollType.PayrollTypeID = HRMS_LeaveRequest.FK_PayrollTypeID and
	      HRMS_PayrollType.PayrollTypeCode = HRMS_LeaveRequest.FK_PayrollTypeCode and
	      HRMS_PayrollType.[Status] = 1 and HRMS_PayrollType.IsActive = 1
	    INNER join Set_PeriodCustom on HRMS_LeaveRequest.FromDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
		and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID
	      where HRMS_LeaveRequest.Fk_PersonID = @PersonID and HRMS_LeaveRequest.FK_PayrollTypeCode = @PayrollTypeCode
	      and (HRMS_LeaveRequest.FromDate < @FromDate and HRMS_LeaveRequest.ToDate >@ToDate)
	      and HRMS_LeaveRequest.[Status]=1 and HRMS_LeaveRequest.[IsApproved]=@IsActive
	  
	      group by
	      HRMS_LeaveRequest.FK_PersonID,
	      HRMS_PayrollType.PayrollTypeID,
	      HRMS_PayrollType.PayrollTypeCode,
	      HRMS_PayrollType.Description,
	      HRMS_EmployeePayroll.leaveDays
	  
	    )TAB Group By  
	    TAB.PersonID,
	    TAB.LeavelID,
	    TAB.PayrollTypeID,
	    TAB.PayrollTypeCode,
	    TAB.[Description],
	    TAB.leaveDays
";

        const string Query_GetEntitledAndAvailLeaveType = @"  SELECT 
  FINTAB.PersonID,
  FINTAB.PayrollTypeID as LeavelID,
  FINTAB.PayrollTypeID,
  FINTAB.PayrollTypeCode,
  FINTAB.Description,
  FINTAB.leaveDays,
  cast(SUM(FINTAB.Quantity) as int) AS QUANTITY
  FROM
 (select 
  
  HRMS_EmployeeAttendance.PersonID,
  HRMS_PayrollType.PayrollTypeID as LeavelID,
  HRMS_PayrollType.PayrollTypeID,
  HRMS_PayrollType.PayrollTypeCode,
  HRMS_PayrollType.Description,
  HRMS_EmployeePayroll.leaveDays,
  HRMS_EmployeeAttendance.Quantity
  
  from HRMS_PayrollType 

  join HRMS_EmployeePayroll on
  HRMS_PayrollType.PayrollTypeID = HRMS_EmployeePayroll.PayrollTypeID and
  HRMS_PayrollType.PayrollTypeCode = HRMS_EmployeePayroll.PayrollTypeCode and
  HRMS_PayrollType.CompanyID = HRMS_EmployeePayroll.CompanyID and 
  (HRMS_EmployeePayroll.IsOneTime is null or HRMS_EmployeePayroll.IsOneTime=0)  and HRMS_EmployeePayroll.Status = 1 and
  HRMS_EmployeePayroll.IsActive = 1
  
  join HRMS_EmployeeAttendance on
  HRMS_EmployeePayroll.PayrollTypeID = HRMS_EmployeeAttendance.LeaveID and
  HRMS_EmployeePayroll.CompanyID = HRMS_EmployeeAttendance.CompanyID and
  HRMS_EmployeeAttendance.Status= 1
  
  
  where HRMS_PayrollType.payrolltypecode = 4 and 
  HRMS_PayrollType.status = 1 and 
  HRMS_PayrollType.isActive = @IsActive and 
  HRMS_PayrollType.CompanyID = @CompanyID and
  HRMS_EmployeeAttendance.PersonID = @PersonID and
  HRMS_EmployeeAttendance.MonthYear > @FromDate and HRMS_EmployeeAttendance.MonthYear < @ToDate )FINTAB
 GROUP BY
  FINTAB.PersonID,
  FINTAB.PayrollTypeID,
  FINTAB.PayrollTypeID,
  FINTAB.PayrollTypeCode,
  FINTAB.Description,
  FINTAB.leaveDays
";

        const string Query_GetLeaveTypeAlnoorAttendaceAndOvertime = @"
        select PayrollTypeID as LeavelID,Description from HRMS_PayrollType 
        where payrolltypecode = 4 and status = 1 and isActive = @IsActive
        and Description like 'PresentDays'

        UNION ALL

        select PayrollTypeID as LeavelID,Description from HRMS_PayrollType 
        where payrolltypecode = 4 and status = 1 and isActive = @IsActive
	    and Description not like 'PresentDays'

";


        const string Query_GetLeaveType = @"
        select PayrollTypeID as LeavelID,Description from HRMS_PayrollType 
        where payrolltypecode = 4 and status = 1 and isActive = @IsActive ";

        const string Query_SelectAllEmployeeCode = @"select PersonID, FullName from HRMS_Set_Employee where CompanyID = @CompanyID and IsActive = @IsActive";

        const string Query_GetData_byPersonID = @" SELECT * FROM [HRMS_EmployeeAttendance] WHERE [CompanyID] = @CompanyID and [PersonID] = @PersonID and [PeriodID] = @PeriodID ";

        
        const string Query_SelectAllDetail_WithPaging = @"  select *, ROW_NUMBER() Over (Order by FullName) as 'Row' from
  (
  select distinct HRMS_Set_Employee.FullName,HRMS_Set_Employee.EmployeeCode,HRMS_EmployeeAttendance.PersonID,
  DATENAME(month, MonthYear)as month ,datepart(Year,MonthYear) as Year 
  from HRMS_EmployeeAttendance inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_EmployeeAttendance.PersonID
  AND HRMS_Set_Employee.CompanyID = HRMS_EmployeeAttendance.CompanyID {0}) as Employee";

        const string Query_SelectAllDetail_WithoutPaging = @"select distinct HRMS_Set_Employee.FullName,HRMS_Set_Employee.EmployeeCode,
  DATENAME(month, MonthYear)as month ,datepart(Year,MonthYear) as Year 
  from HRMS_EmployeeAttendance inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_EmployeeAttendance.PersonID
  AND HRMS_Set_Employee.CompanyID = HRMS_EmployeeAttendance.CompanyID ";


        const string Query_GetEmpAttendByPersonID = @"
SELECT 
--HRMS_EmployeeAttendance.EmployeeAttendanceID,
HRMS_EmployeeAttendance.PersonID, 
HRMS_EmployeeAttendance.LeaveID, 
 HRMS_EmployeeAttendance.Quantity as Quantity, 
MIN(HRMS_EmployeeAttendance.MonthYear) as FromDate,
MAX(HRMS_EmployeeAttendance.MonthYear) as MonthYear,
HRMS_PayrollType.Description, 
HRMS_Set_Employee.FullName,
HRMS_EmployeeAttendance.StatusCode

FROM HRMS_EmployeeAttendance

INNER JOIN HRMS_PayrollType on 
HRMS_EmployeeAttendance.LeaveID = HRMS_PayrollType.PayrollTypeID AND
HRMS_PayrollType.PayrollTypeCode = 4

INNER JOIN HRMS_Set_Employee on 
HRMS_EmployeeAttendance.PersonID = HRMS_Set_Employee.PersonID
AND HRMS_EmployeeAttendance.CompanyID = HRMS_Set_Employee.CompanyID

WHERE 

DATENAME(month, HRMS_EmployeeAttendance.MonthYear) = @Month AND
DATEPART(Year, [HRMS_EmployeeAttendance].[MonthYear]) = @Year AND
HRMS_EmployeeAttendance.CompanyID = @CompanyID AND 
HRMS_EmployeeAttendance.PeriodID = @PeriodID AND 
HRMS_EmployeeAttendance.PersonID = @PersonID AND
HRMS_EmployeeAttendance.Status = 1


GROUP BY 
HRMS_EmployeeAttendance.PersonID, 
HRMS_PayrollType.[Description], 
HRMS_EmployeeAttendance.LeaveID,
HRMS_Set_Employee.FullName, HRMS_EmployeeAttendance.Quantity,HRMS_EmployeeAttendance.StatusCode

ORDER BY 
HRMS_EmployeeAttendance.PersonID, HRMS_Set_Employee.FullName

";


    const string Query_GetTotalLeavesOfMonthByPersonID = @"
DECLARE @FromDate date;
DECLARE @ToDate date;
DECLARE @Days varchar(2);

SET @FromDate=(Select convert(datetime, @Month + '/01/' + @Year, 101) );
SET @Days=(Select DAY(DATEADD(DD,-1,DATEADD(MM,DATEDIFF(MM,-1,@FromDate),0))) );
SET @ToDate=(Select convert(datetime, @Month + '/' + @Days + '/' + @Year, 101) ); 

	Select  
	TAB.PersonID,
	--TAB.LeavelID,
	--TAB.PayrollTypeID,
	--TAB.PayrollTypeCode,
	TAB.MonthYear,
	--TAB.Description,
	--TAB.leaveDays,
	SUM(TAB.Quantity) AS QUANTITY,
    Case TAB.Impact 
    When 1 Then 'Positive'
    When 2 Then 'Negative'
    When 3 Then 'No Impact'
    END 'Impact',
    TAB.Factor

	from 
	(
	SELECT
	HRMS_EmployeeAttendance.PersonID,
	HRMS_PayrollType.PayrollTypeID as LeavelID,
	HRMS_PayrollType.PayrollTypeID,
	HRMS_PayrollType.PayrollTypeCode,
	CONVERT(datetime,
	CONVERT(varchar(2), DATEPART(mm, HRMS_EmployeeAttendance.MonthYear))  + '-01-' + CONVERT(varchar(4), DATEPART(year, HRMS_EmployeeAttendance.MonthYear))
	,101) as MonthYear,
	HRMS_PayrollType.Description,
	HRMS_EmployeePayroll.leaveDays,
	isnull(sum(HRMS_EmployeeAttendance.Quantity),0) as Quantity,
    HRMS_PayrollType.Impact,
    HRMS_PayrollType.Factor
	FROM HRMS_EmployeeAttendance

	inner join HRMS_PayrollType on 
	--HRMS_PayrollType.CompanyID = HRMS_EmployeeAttendance.CompanyID and
	HRMS_PayrollType.PayrollTypeID = HRMS_EmployeeAttendance.LeaveID and
	HRMS_PayrollType.PayrollTypeCode = @PayrollTypeCode and
	HRMS_EmployeeAttendance.[Status] = 1 

	inner join HRMS_EmployeePayroll on
	HRMS_EmployeePayroll.PersonID = HRMS_EmployeeAttendance.PersonID and
	HRMS_EmployeePayroll.PayrollTypeID = HRMS_EmployeeAttendance.LeaveID and
	HRMS_EmployeePayroll.PayrollTypeCode = @PayrollTypeCode and
	HRMS_EmployeePayroll.CompanyID = HRMS_EmployeeAttendance.CompanyID and 
	(HRMS_EmployeePayroll.IsOneTime is null or HRMS_EmployeePayroll.IsOneTime=0)  and HRMS_EmployeePayroll.Status = 1 and
	HRMS_EmployeePayroll.IsActive = 1

	WHERE 
	DATEPART(mm, HRMS_EmployeeAttendance.MonthYear) = @Month AND
	DATEPART(yy, HRMS_EmployeeAttendance.MonthYear) = @Year AND
	HRMS_EmployeeAttendance.CompanyID = @CompanyID AND 
	HRMS_EmployeeAttendance.PersonID = @PersonID AND
	HRMS_EmployeeAttendance.Status = 1

	GROUP BY 
	HRMS_EmployeeAttendance.PersonID,
	HRMS_PayrollType.PayrollTypeID,
	HRMS_PayrollType.PayrollTypeCode,
	CONVERT(datetime,
	CONVERT(varchar(2), DATEPART(mm, HRMS_EmployeeAttendance.MonthYear))  + '-01-' + CONVERT(varchar(4), DATEPART(year, HRMS_EmployeeAttendance.MonthYear))
	,101),
	HRMS_EmployeePayroll.leaveDays,
	HRMS_PayrollType.Description,
	HRMS_EmployeePayroll.leaveDays,
    HRMS_PayrollType.Impact,
    HRMS_PayrollType.Factor

	union all
	
	  --CASE 0
	  Select
	  HRMS_LeaveRequest.FK_PersonID,
	  HRMS_PayrollType.PayrollTypeID as LeavelID,
	  HRMS_PayrollType.PayrollTypeID,
	  HRMS_PayrollType.PayrollTypeCode, 
	  CONVERT(datetime,
	  CONVERT(varchar(2), DATEPART(mm, HRMS_LeaveRequest.FromDate))  + '-01-' + CONVERT(varchar(4), DATEPART(year, HRMS_LeaveRequest.FromDate))
	  ,101) as MonthYear,
	  HRMS_PayrollType.Description,
	  HRMS_EmployeePayroll.leaveDays,
	  sum(DATEDIFF(dd,HRMS_LeaveRequest.FromDate,HRMS_LeaveRequest.ToDate) + 1) as Quantity,
	  HRMS_PayrollType.Impact,
      HRMS_PayrollType.Factor

	  from HRMS_LeaveRequest
	  
	  inner join HRMS_EmployeePayroll on
	  HRMS_EmployeePayroll.PersonID = HRMS_LeaveRequest.Fk_PersonID and
	  HRMS_EmployeePayroll.PayrollTypeID = HRMS_LeaveRequest.FK_PayrollTypeID and
	  HRMS_EmployeePayroll.PayrollTypeCode = HRMS_LeaveRequest.FK_PayrollTypeCode and
	  HRMS_EmployeePayroll.CompanyID = HRMS_LeaveRequest.CompanyID and 
	  (HRMS_EmployeePayroll.IsOneTime is null or HRMS_EmployeePayroll.IsOneTime=0)  and HRMS_EmployeePayroll.Status = 1 and
	  HRMS_EmployeePayroll.IsActive = 1
	  
	  inner join HRMS_PayrollType on 
	  --HRMS_PayrollType.CompanyID = HRMS_LeaveRequest.CompanyID and
	  HRMS_PayrollType.PayrollTypeID = HRMS_LeaveRequest.FK_PayrollTypeID and
	  HRMS_PayrollType.PayrollTypeCode = HRMS_LeaveRequest.FK_PayrollTypeCode and
	  HRMS_PayrollType.[Status] = 1 and HRMS_PayrollType.IsActive = 1
	  
	  where HRMS_LeaveRequest.Fk_PersonID = @PersonID and HRMS_LeaveRequest.FK_PayrollTypeCode = @PayrollTypeCode
	  and (DATEPART(year, HRMS_LeaveRequest.FromDate) = DATEPART(year, @FromDate) and DATEPART(year, HRMS_LeaveRequest.ToDate) = DATEPART(year, @FromDate))
	  and (DATEPART(month, HRMS_LeaveRequest.FromDate) >= DATEPART(month, @FromDate) and DATEPART(month, HRMS_LeaveRequest.ToDate) <= DATEPART(month, @ToDate))
	  and HRMS_LeaveRequest.[Status]=1 and HRMS_LeaveRequest.[IsApproved]=@IsActive
	  
	  group by
	  HRMS_LeaveRequest.FK_PersonID,
	  HRMS_PayrollType.PayrollTypeID,
	  HRMS_PayrollType.PayrollTypeCode,
	  CONVERT(datetime,
	  CONVERT(varchar(2), DATEPART(mm, HRMS_LeaveRequest.FromDate))  + '-01-' + CONVERT(varchar(4), DATEPART(year, HRMS_LeaveRequest.FromDate))
	  ,101),
	  HRMS_PayrollType.Description,
	  HRMS_EmployeePayroll.leaveDays,
      HRMS_PayrollType.Impact,
	  HRMS_PayrollType.Factor

	  union
	  
	  --CASE 1
	  Select 
	  HRMS_LeaveRequest.FK_PersonID,
	  HRMS_PayrollType.PayrollTypeID as LeavelID,
	  HRMS_PayrollType.PayrollTypeID,
	  HRMS_PayrollType.PayrollTypeCode,
	  CONVERT(datetime,
	  CONVERT(varchar(2), DATEPART(mm, @ToDate))  + '-01-' + CONVERT(varchar(4), DATEPART(year, @ToDate))
	  ,101) as MonthYear,
	  HRMS_PayrollType.Description,
	  HRMS_EmployeePayroll.leaveDays,
	  sum(DATEDIFF(dd,@FromDate,HRMS_LeaveRequest.ToDate) + 1) as Quantity,
	  HRMS_PayrollType.Impact,
      HRMS_PayrollType.Factor

	  from HRMS_LeaveRequest
	  
	  inner join HRMS_EmployeePayroll on
	  HRMS_EmployeePayroll.PersonID = HRMS_LeaveRequest.Fk_PersonID and
	  HRMS_EmployeePayroll.PayrollTypeID = HRMS_LeaveRequest.FK_PayrollTypeID and
	  HRMS_EmployeePayroll.PayrollTypeCode = HRMS_LeaveRequest.FK_PayrollTypeCode and
	  HRMS_EmployeePayroll.CompanyID = HRMS_LeaveRequest.CompanyID and 
	  (HRMS_EmployeePayroll.IsOneTime is null or HRMS_EmployeePayroll.IsOneTime=0)  and HRMS_EmployeePayroll.Status = 1 and
	  HRMS_EmployeePayroll.IsActive = 1
	  
	  inner join HRMS_PayrollType on 
	  --HRMS_PayrollType.CompanyID = HRMS_LeaveRequest.CompanyID and
	  HRMS_PayrollType.PayrollTypeID = HRMS_LeaveRequest.FK_PayrollTypeID and
	  HRMS_PayrollType.PayrollTypeCode = HRMS_LeaveRequest.FK_PayrollTypeCode and
	  HRMS_PayrollType.[Status] = 1 and HRMS_PayrollType.IsActive = 1
	  
	  where HRMS_LeaveRequest.Fk_PersonID = @PersonID and HRMS_LeaveRequest.FK_PayrollTypeCode = @PayrollTypeCode
	  and DATEPART(year, HRMS_LeaveRequest.FromDate) <= DATEPART(year, @FromDate) 
	  and DATEPART(year, HRMS_LeaveRequest.ToDate) = DATEPART(year, @FromDate) 
	  and DATEPART(month, HRMS_LeaveRequest.FromDate) < DATEPART(month, @FromDate)
	  and DATEPART(month, HRMS_LeaveRequest.ToDate) = DATEPART(month, @ToDate)
	  and HRMS_LeaveRequest.[Status]=1 and HRMS_LeaveRequest.[IsApproved]=@IsActive
	  
	  group by
	  HRMS_LeaveRequest.FK_PersonID,
	  HRMS_PayrollType.PayrollTypeID,
	  HRMS_PayrollType.PayrollTypeCode,
	  HRMS_PayrollType.Description,
	  HRMS_EmployeePayroll.leaveDays,
      HRMS_PayrollType.Impact,
      HRMS_PayrollType.Factor
	  
	  union
	  
	  --CASE 2
	  Select 
	  HRMS_LeaveRequest.FK_PersonID,
	  HRMS_PayrollType.PayrollTypeID as LeavelID,
	  HRMS_PayrollType.PayrollTypeID,
	  HRMS_PayrollType.PayrollTypeCode,
	  CONVERT(datetime,
	  CONVERT(varchar(2), DATEPART(mm, @FromDate))  + '-01-' + CONVERT(varchar(4), DATEPART(year, @FromDate))
	  ,101) as MonthYear,
	  HRMS_PayrollType.Description,
	  HRMS_EmployeePayroll.leaveDays,
	  sum(DATEDIFF(dd,HRMS_LeaveRequest.FromDate,@ToDate) + 1) as Quantity,
	  HRMS_PayrollType.Impact,
      HRMS_PayrollType.Factor

	  from HRMS_LeaveRequest
	  
	  inner join HRMS_EmployeePayroll on
	  HRMS_EmployeePayroll.PersonID = HRMS_LeaveRequest.Fk_PersonID and
	  HRMS_EmployeePayroll.PayrollTypeID = HRMS_LeaveRequest.FK_PayrollTypeID and
	  HRMS_EmployeePayroll.PayrollTypeCode = HRMS_LeaveRequest.FK_PayrollTypeCode and
	  HRMS_EmployeePayroll.CompanyID = HRMS_LeaveRequest.CompanyID and 
	  (HRMS_EmployeePayroll.IsOneTime is null or HRMS_EmployeePayroll.IsOneTime=0)  and HRMS_EmployeePayroll.Status = 1 and
	  HRMS_EmployeePayroll.IsActive = 1
	  
	  inner join HRMS_PayrollType on 
	  --HRMS_PayrollType.CompanyID = HRMS_LeaveRequest.CompanyID and
	  HRMS_PayrollType.PayrollTypeID = HRMS_LeaveRequest.FK_PayrollTypeID and
	  HRMS_PayrollType.PayrollTypeCode = HRMS_LeaveRequest.FK_PayrollTypeCode and
	  HRMS_PayrollType.[Status] = 1 and HRMS_PayrollType.IsActive = 1
	  
	  where HRMS_LeaveRequest.Fk_PersonID = @PersonID and HRMS_LeaveRequest.FK_PayrollTypeCode = @PayrollTypeCode
	  and DATEPART(year, HRMS_LeaveRequest.FromDate) = DATEPART(year, @FromDate) 
	  and DATEPART(year, HRMS_LeaveRequest.ToDate) >= DATEPART(year, @FromDate) 
	  and DATEPART(month, HRMS_LeaveRequest.FromDate) = DATEPART(month, @FromDate)
	  and DATEPART(month, HRMS_LeaveRequest.ToDate) > DATEPART(month, @ToDate)
	  and HRMS_LeaveRequest.[Status]=1 and HRMS_LeaveRequest.[IsApproved]=@IsActive
	  
	  group by
	  HRMS_LeaveRequest.FK_PersonID,
	  HRMS_PayrollType.PayrollTypeID,
	  HRMS_PayrollType.PayrollTypeCode,
	  HRMS_PayrollType.Description,
	  HRMS_EmployeePayroll.leaveDays,
      HRMS_PayrollType.Impact,
      HRMS_PayrollType.Factor
	  
	  union
	  
	  --CASE 3
	  Select 
	  HRMS_LeaveRequest.FK_PersonID,
	  HRMS_PayrollType.PayrollTypeID as LeavelID,
	  HRMS_PayrollType.PayrollTypeID,
	  HRMS_PayrollType.PayrollTypeCode,
	  CONVERT(datetime,
	  CONVERT(varchar(2), DATEPART(mm, @FromDate))  + '-01-' + CONVERT(varchar(4), DATEPART(year, @FromDate))
	  ,101) as MonthYear,
	  HRMS_PayrollType.Description,
	  HRMS_EmployeePayroll.leaveDays,
	  sum(DATEDIFF(dd,@FromDate,@ToDate) + 1) as Quantity,
	  HRMS_PayrollType.Impact,
      HRMS_PayrollType.Factor

	  from HRMS_LeaveRequest
	  
	  inner join HRMS_EmployeePayroll on
	  HRMS_EmployeePayroll.PersonID = HRMS_LeaveRequest.Fk_PersonID and
	  HRMS_EmployeePayroll.PayrollTypeID = HRMS_LeaveRequest.FK_PayrollTypeID and
	  HRMS_EmployeePayroll.PayrollTypeCode = HRMS_LeaveRequest.FK_PayrollTypeCode and
	  HRMS_EmployeePayroll.CompanyID = HRMS_LeaveRequest.CompanyID and 
	  (HRMS_EmployeePayroll.IsOneTime is null or HRMS_EmployeePayroll.IsOneTime=0)  and HRMS_EmployeePayroll.Status = 1 and
	  HRMS_EmployeePayroll.IsActive = 1
	  
	  inner join HRMS_PayrollType on 
	  --HRMS_PayrollType.CompanyID = HRMS_LeaveRequest.CompanyID and
	  HRMS_PayrollType.PayrollTypeID = HRMS_LeaveRequest.FK_PayrollTypeID and
	  HRMS_PayrollType.PayrollTypeCode = HRMS_LeaveRequest.FK_PayrollTypeCode and
	  HRMS_PayrollType.[Status] = 1 and HRMS_PayrollType.IsActive = 1
	  
	  where HRMS_LeaveRequest.Fk_PersonID = @PersonID and HRMS_LeaveRequest.FK_PayrollTypeCode = @PayrollTypeCode
	  and DATEPART(year, HRMS_LeaveRequest.FromDate) <= DATEPART(year, @FromDate) 
	  and DATEPART(year, HRMS_LeaveRequest.ToDate) >= DATEPART(year, @FromDate) 
	  and DATEPART(month, HRMS_LeaveRequest.FromDate) < DATEPART(month, @FromDate)
	  and DATEPART(month, HRMS_LeaveRequest.ToDate) > DATEPART(month, @ToDate)
	  and HRMS_LeaveRequest.[Status]=1 and HRMS_LeaveRequest.[IsApproved]=@IsActive
	  
	  group by
	  HRMS_LeaveRequest.FK_PersonID,
	  HRMS_PayrollType.PayrollTypeID,
	  HRMS_PayrollType.PayrollTypeCode,
	  HRMS_PayrollType.Description,
	  HRMS_EmployeePayroll.leaveDays,
	  HRMS_PayrollType.Impact,
      HRMS_PayrollType.Factor
	) TAB Group By  
	TAB.PersonID,
	--TAB.LeavelID,
	--TAB.PayrollTypeID,
	--TAB.PayrollTypeCode,
	TAB.MonthYear,
	--TAB.[Description],
	TAB.leaveDays,
    TAB.Impact,
    TAB.Factor
    ";

        const string Query_GetLeavesByPersonIDANDMonths = @"SELECT 
                    Sum(HRMS_EmployeeAttendance.Quantity) AS Quantity,
                    HRMS_EmployeeAttendance.PersonID,
                    HRMS_EmployeeAttendance.MonthYear 
                    from HRMS_EmployeeAttendance

                    WHERE
                    HRMS_EmployeeAttendance.MonthYear BETWEEN @StartDate AND @EndDate  AND
                    HRMS_EmployeeAttendance.CompanyID = @CompanyID AND 
                    HRMS_EmployeeAttendance.PeriodID = @PeriodID AND 
                    HRMS_EmployeeAttendance.PersonID = @PersonID AND
                    HRMS_EmployeeAttendance.Status = 1

                    GROUP BY 
                    HRMS_EmployeeAttendance.MonthYear,
                    HRMS_EmployeeAttendance.PersonID ";


        const string Query_Delete_ByKey_PersonID = @" Update [HRMS_EmployeeAttendance] Set [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [CompanyID] = @CompanyID and [PersonID] = @PersonID and [PeriodID] = @PeriodID and DATEPART(Year, [HRMS_EmployeeAttendance].[MonthYear]) = DATEPART(Year, @Year) and DATEPART(m, [HRMS_EmployeeAttendance].[MonthYear]) = DATEPART(m, @Month)";

        const string Query_DeletePhysically_ByKey_PersonID = @" Delete From [HRMS_EmployeeAttendance] WHERE [CompanyID] = @CompanyID and [PersonID] = @PersonID and [PeriodID] = @PeriodID and DATEPART(Year, [HRMS_EmployeeAttendance].[MonthYear]) = DATEPART(Year, @Year) and DATEPART(m, [HRMS_EmployeeAttendance].[MonthYear]) = DATEPART(m, @Month)";


        const string Query_CheckEmployeeBYPersonID = @"select distinct HRMS_EmployeeAttendance.PersonID, HRMS_EmployeeAttendance.LeaveID, 
HRMS_EmployeeAttendance.Quantity, HRMS_EmployeeAttendance.MonthYear
FROM HRMS_EmployeeAttendance
where HRMS_EmployeeAttendance.PersonID = @PersonID AND DATEPART(m, [HRMS_EmployeeAttendance].[MonthYear]) = DATEPART(m, @Month)
and DATEPART(Year, [HRMS_EmployeeAttendance].[MonthYear]) = DATEPART(Year, @Year)
and HRMS_EmployeeAttendance.CompanyID = @CompanyID and HRMS_EmployeeAttendance.PeriodID = @PeriodID and HRMS_EmployeeAttendance.Status = 1";

        const string Query_Leaves = @"
 
       select isnull(abs(Sum(HRMS_EmployeeAttendance.Quantity)),0) as Quantity from HRMS_EmployeeAttendance 
	   inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_EmployeeAttendance.LeaveID
        and HRMS_PayrollType.Status = 1 and HRMS_PayrollType.IsActive = 1
		and Description like '%Total Absent Days%'
		where HRMS_EmployeeAttendance.Status = 1 
		and year(MonthYear) =  @Year
		and month(MonthYear) = @Month 
		and PersonID = @PersonID
		 ";

        #endregion

        #region Class Methods

        public static DataTable GetLeaveType(int companyID, Nullable<bool> isActive = true)
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

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetLeaveType, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetDataByPersonID(int companyID, int PersonID, int periodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PersonIDID", PersonID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byPersonID, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byPersonID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byPersonID, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetLeaveTypeAlnoorAttendaceAndOvertime(int companyID, Nullable<bool> isActive = true)
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

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetLeaveTypeAlnoorAttendaceAndOvertime, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable getdatabyPersonIDandDatealnoor(int companyID, int month, int year, string person, Nullable<bool> isActive = true)
        {
            DataTable dt = null;

            #region query 
            const string Query_Getforrecordchecking = @"
                select  HRMS_EmployeeAttendance.MonthYear, HRMS_Set_Employee.FullName,HRMS_Set_Employee.EmployeeCode,HRMS_EmployeeAttendance.PersonID,
  datepart(month, MonthYear)as month ,datepart(Year,MonthYear) as Year 
  from HRMS_EmployeeAttendance inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_EmployeeAttendance.PersonID
  AND HRMS_Set_Employee.CompanyID = HRMS_EmployeeAttendance.CompanyID 
  where HRMS_EmployeeAttendance.Status = 1 and isActive = @IsActive
        and HRMS_EmployeeAttendance.CompanyID = @CompanyID
        and  datepart(month, MonthYear) = @Month
		and  datepart(Year, MonthYear) = @Year
		And (cast(HRMS_EmployeeAttendance.PersonID as varchar) +','+HRMS_Set_Employee.FullName) = @Person
             ";
            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@Month", month);
                sqlparams[2] = new SqlParameter("@Year", year);
                sqlparams[3] = new SqlParameter("@Person", person);

                if (isActive.HasValue)
                    sqlparams[4] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[4] = new SqlParameter("@IsActive", DBNull.Value);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Getforrecordchecking, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable getdatabyPersonIDandDateSanghar(int companyID, int month, int year, int PersonID, Nullable<bool> isActive = true)
        {
            DataTable dt = null;

            #region query
            const string Query_Getforrecordchecking = @"
                select  HRMS_EmployeeAttendance.MonthYear, HRMS_Set_Employee.FullName,HRMS_Set_Employee.EmployeeCode,HRMS_EmployeeAttendance.PersonID,
  datepart(month, MonthYear)as month ,datepart(Year,MonthYear) as Year 
  from HRMS_EmployeeAttendance inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_EmployeeAttendance.PersonID
  AND HRMS_Set_Employee.CompanyID = HRMS_EmployeeAttendance.CompanyID 
  where HRMS_EmployeeAttendance.Status = 1 
        and HRMS_EmployeeAttendance.CompanyID = @CompanyID
        and  datepart(month, MonthYear) = @Month
		and  datepart(Year, MonthYear) = @Year
		And HRMS_EmployeeAttendance.PersonID = @PersonID
             ";
            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@Month", month);
                sqlparams[2] = new SqlParameter("@Year", year);
                sqlparams[3] = new SqlParameter("@PersonID", PersonID);

                if (isActive.HasValue)
                    sqlparams[4] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[4] = new SqlParameter("@IsActive", DBNull.Value);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Getforrecordchecking, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }



        public static DataTable GetAllActiveEmployee(Nullable<int> companyID, Nullable<bool> isActive = true)
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

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllEmployeeCode, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataDetail(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, string.Format(Query_SelectAllDetail_WithPaging, strWhere) + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), string.Format(Query_SelectAllDetail_WithPaging, strWhere) + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text,string.Format(Query_SelectAllDetail_WithoutPaging , strWhere) + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetEmpAttendByPersonID(int companyID, int periodID, int personID, string Month, string Year)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", personID);
                sqlparams[3] = new SqlParameter("@Month", Month);
                sqlparams[4] = new SqlParameter("@Year", Year);
               

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetEmpAttendByPersonID, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataRow GetTotalLeavesOfMonthByPersonID(int companyID, int @PayrollTypeCode, int personID, string Month, string Year, bool? IsActive = true, SqlConnection objConnection = null)
        {//int periodID,
            DataRow dr = null;

            string iMonth = Convert.ToDateTime(Month + " 01, 1900").Month.ToString("00");

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PayrollTypeCode", PayrollTypeCode);
                //sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", personID);
                sqlparams[3] = new SqlParameter("@Month", iMonth);
                sqlparams[4] = new SqlParameter("@Year", Year);

                if (IsActive == true )
                    sqlparams[5] = new SqlParameter("@IsActive", "1");
                else
                    sqlparams[5] = new SqlParameter("@IsActive", "0");

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetTotalLeavesOfMonthByPersonID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetTotalLeavesOfMonthByPersonID, sqlparams);

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

        public static DataTable GetTotalLeavesOfMonthBy_PersonID(int companyID, int @PayrollTypeCode, int personID, string Month, string Year, bool? IsActive = true, SqlTransaction sqltrans = null , SqlConnection objConnection = null )
        {//int periodID,
            DataTable dt = null;

            string iMonth = Convert.ToDateTime(Month + " 01, 1900").Month.ToString("00");

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PayrollTypeCode", PayrollTypeCode);
                //sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", personID);
                sqlparams[3] = new SqlParameter("@Month", iMonth);
                sqlparams[4] = new SqlParameter("@Year", Year);

                if (IsActive == true)
                    sqlparams[5] = new SqlParameter("@IsActive", "1");
                else
                    sqlparams[5] = new SqlParameter("@IsActive", "0");

                DataSet ds = null;

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetTotalLeavesOfMonthByPersonID, sqlparams);
                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query_GetTotalLeavesOfMonthByPersonID, sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetTotalLeavesOfMonthByPersonID, sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetTotalLeavesOfMonthByPersonID, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static bool DeleteDataPersonIDByKey(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, int companyID, int periodID, int PersonID, DateTime Month, DateTime Year)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[9];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Month", Month);
                sqlparams[4] = new SqlParameter("@Year", Year);
                sqlparams[5] = new SqlParameter("@Status", false);
                sqlparams[6] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[7] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[8] = new SqlParameter("@UpdatedByValue", updatedByValue);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByKey_PersonID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByKey_PersonID, sqlparams) > 0)
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

        public bool UpdateEmployeeAttendance(List<HRMS_EmployeeAttendance> colUpdateQDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateEmployeeAttendance");

            try
            {
               
                foreach (HRMS_EmployeeAttendance qdUpdate in colUpdateQDetail)
                    qdUpdate.UpdateDataByKey(sqltrans);

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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateEmployeeAttendance");
            }

            return result;
        }

        public static DataTable CheckEmployeeByPersonID(int companyID, int periodID, int personID, DateTime Month, DateTime Year)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", personID);
                sqlparams[3] = new SqlParameter("@Month", Month);
                sqlparams[4] = new SqlParameter("@Year", Year);


                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_CheckEmployeeBYPersonID, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static bool DeleteDataPhysicalyPersonIDByKey(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, int companyID, int periodID, int PersonID, DateTime Month, DateTime Year)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[9];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Month", Month);
                sqlparams[4] = new SqlParameter("@Year", Year);
                sqlparams[5] = new SqlParameter("@Status", false);
                sqlparams[6] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[7] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[8] = new SqlParameter("@UpdatedByValue", updatedByValue);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeletePhysically_ByKey_PersonID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DeletePhysically_ByKey_PersonID, sqlparams) > 0)
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

        public static DataTable GetEntitledAndAvailLeaveType(int companyID,int personID,DateTime FromDate,DateTime ToDate, Nullable<bool> isActive = true)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PersonID", personID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);

                if (isActive.HasValue)
                    sqlparams[4] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[4] = new SqlParameter("@IsActive", DBNull.Value);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetEntitledAndAvailLeaveType, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetEntitledAndAvailLeaveTypeSmry(int companyID, int PayrollTypeCode, int personID, DateTime FromDate, DateTime ToDate, Nullable<bool> isActive = true, int CurrentFiscalID=0)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PayrollTypeCode", @PayrollTypeCode);
                sqlparams[2] = new SqlParameter("@PersonID", personID);
                sqlparams[3] = new SqlParameter("@FromDate", FromDate);
                sqlparams[4] = new SqlParameter("@ToDate", ToDate);
                sqlparams[5] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                if (isActive.HasValue)
                    sqlparams[6] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[6] = new SqlParameter("@IsActive", DBNull.Value);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetEntitledAndAvailLeaveTypeSumry, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetAbsentFromDuration(int companyID,int personID,DateTime datetime)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                
                sqlparams[1] = new SqlParameter("@PersonID", personID);
                sqlparams[2] = new SqlParameter("@Month", datetime.Month.ToString());
                sqlparams[3] = new SqlParameter("@Year", datetime.Year.ToString());
                //sqlparams[2] = new SqlParameter("@FromYear", fromYear);
                //sqlparams[3] = new SqlParameter("@FromMonth", fromMonth);
                //sqlparams[4] = new SqlParameter("@ToYear", toYear);
                //sqlparams[5] = new SqlParameter("@ToMonth", toMonth);


                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Leaves, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetDataByPersonID_BonusForm(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> personID, DateTime datetime,  SqlConnection objConnection = null)
        {

            SqlParameter[] sqlparams = new SqlParameter[4];
            sqlparams[0] = new SqlParameter("@CompanyID", companyID);
            sqlparams[1] = new SqlParameter("@PeriodID", periodID);
            sqlparams[2] = new SqlParameter("@PersonID", personID);
            sqlparams[3] = new SqlParameter("@FromDate", datetime);
            #region Query
            const string Query_forActiveDataForattendencebonus = @"
select HRMS_SalaryDetail.PayrollTypeID, * from HRMS_SalaryHeader 
left JOIN HRMS_SalaryDetail on HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID 
and HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID and HRMS_SalaryDetail.Status = 1
WHERE HRMS_SalaryHeader.Status = 1 AND HRMS_SalaryHeader.CompanyID = @CompanyID  AND HRMS_SalaryHeader.PeriodID = @PeriodID AND HRMS_SalaryHeader.PersonID = @PersonID And HRMS_SalaryHeader.SalaryDate = @FromDate
            ";
            #endregion
            DataTable dt = null;
            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_forActiveDataForattendencebonus), sqlparams).Tables[0];
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
