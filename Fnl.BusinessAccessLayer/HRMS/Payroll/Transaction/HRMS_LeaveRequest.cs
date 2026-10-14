using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Common.Utils;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Transaction
{
    public partial class HRMS_LeaveRequest
    {
        #region Constant Strings


        const string Query_GetData_byKey1 = @" 
SELECT * from [HRMS_LeaveRequest]
WHERE [HRMS_LeaveRequest].CompanyID = @CompanyID and [HRMS_LeaveRequest].LeaveRequestID = @LeaveRequestID and [HRMS_LeaveRequest].PeriodID = @PeriodID
";

       

        const string Query_GetTotalShortLeavesOfMonthByPersonID = @"SELECT  
                    [HRMS_LeaveRequest].[Fk_PersonID], 
                    COUNT([HRMS_LeaveRequest].[ShortLeaveType]) as Quantity,
                    MAX([HRMS_LeaveRequest].[RequestDate]) as MonthYear

                    FROM [HRMS_LeaveRequest]


                    WHERE 
                    DATENAME(month, [HRMS_LeaveRequest].[RequestDate]) = @Month AND
                    DATEPART(Year, [HRMS_LeaveRequest].[RequestDate]) = @Year AND
                    [HRMS_LeaveRequest].[CompanyID] = @CompanyID AND 
                    [HRMS_LeaveRequest].[Fk_PersonID]= @PersonID AND
                    [HRMS_LeaveRequest].[IsApproved] = 1 AND
                    [HRMS_LeaveRequest].[Fk_PayrollTypeCode] = 7 AND
                    [HRMS_LeaveRequest].Status = 1


                    GROUP BY 
                    [HRMS_LeaveRequest].[Fk_PersonID]";


        const string Query_GetHalfDaySlip = @" 
Select FullName,PersonID,EmployeeCode,FK_DepartmentID,DepartmentName,FromTime as TimeOut,LeavePurpose
 from HRMS_LeaveRequest
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_LeaveRequest.FK_PersonID
inner join Set_Department on Set_Department.DepartmentID =HRMS_Set_Employee.FK_DepartmentID
where HRMS_LeaveRequest.Status = 1
and HRMS_LeaveRequest.CompanyID = 1
and HRMS_LeaveRequest.PeriodID = 1
{0}
 ";

        const string QUery_GetHolidayVisitSlip = @" Select FullName,EmployeeCode,PersonID as EmployeeID,Reason,Max(TimeIN) as TimeIn,Max(Timeout) as TimeOut,DepartmentName from 
(
Select FullName,EmployeeCode,HRMS_TMSDATA.PersonID,DepartmentName,HRMS_TMSDATA.Reason,TMSDATETIME as TimeIN,null as TimeOUT
from HRMS_TMSDATA
	
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_TMSDATA.PersonID
inner join Set_Department on Set_Department.DepartmentID =HRMS_Set_Employee.FK_DepartmentID
inner join Set_dateTime on Convert(Date,SET_DateTime.TheDate) >= Convert(Date,TMSDateTime) 
and Convert(Date,SET_DateTime.TheDate) <= Convert(Date,TMSDateTime) and Set_DateTime.Isholiday = 1  

where HRMS_TMSDATA.CompanyID = 1 and HRMS_TMSDATA.PeriodID = 1
and StatusCode = 1
{0}--and TableID between 26100 and 26101


union all

Select FullName,EmployeeCode,HRMS_TMSDATA.PersonID,DepartmentName,HRMS_TMSDATA.Reason,null ,TMSDATETIME as TimeOut
from HRMS_TMSDATA
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_TMSDATA.PersonID
inner join Set_Department on Set_Department.DepartmentID =HRMS_Set_Employee.FK_DepartmentID
inner join Set_dateTime on Convert(Date,SET_DateTime.TheDate) >= Convert(Date,TMSDateTime) 
and Convert(Date,SET_DateTime.TheDate) <= Convert(Date,TMSDateTime) and Set_DateTime.Isholiday = 1 
 
where HRMS_TMSDATA.CompanyID = 1 and HRMS_TMSDATA.PeriodID = 1
and StatusCode = 2
{0}--and TableID between 26100 and 26101 


) as tab
group by FullName,EmployeeCode,Personid,DepartmentName,Reason
	
	
	
 ";

        const string Query_GetLeaveType = @"Select [ShortLeaveTypeID] keys,ShortLeaveType as value from HRMS_ShortLeaveType";

        const string Query_SelectAll_ForApproveDetail = @"
SELECT DISTINCT HRMS_LeaveRequest.LeaveRequestID
	,HRMS_LeaveRequest.CompanyID	,HRMS_LeaveRequest.PeriodID
	,HRMS_LeaveRequest.Fk_PersonID	,HRMS_LeaveRequest.LeavePurpose
	,HRMS_LeaveRequest.ToDate	,HRMS_LeaveRequest.FromDate
	,HRMS_LeaveRequest.ToTime	,HRMS_LeaveRequest.ShortLeaveType
	,HRMS_LeaveRequest.Fk_PayrollTypeCode	,HRMS_LeaveRequest.FK_PayrollTypeID
	,HRMS_LeaveRequest.IsSandwichLeave	,HRMS_LeaveRequest.IsLate
	,HRMS_LeaveRequest.IsLate	,HRMS_LeaveRequest.IsApproved
	,HRMS_LeaveRequest.Remarks	,HRMS_LeaveRequest.Status
	,HRMS_LeaveRequest.StatusCode	,HRMS_LeaveRequest.CreatedBy
	,HRMS_LeaveRequest.CreateDate	,HRMS_LeaveRequest.UpdatedBy
	,HRMS_LeaveRequest.UpdateDate	,HRMS_LeaveRequest.CreatedByValue
	,HRMS_LeaveRequest.UpdatedByValue	,SET_DocumentApprove.ApprovalStatus
    ,SET_ApprovalAuthority.UserID AS UserID    ,SET_Authority.AuthorityLevel
FROM HRMS_LeaveRequest

INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = HRMS_LeaveRequest.CompanyID
	AND SET_DocumentApprove.PeriodID = HRMS_LeaveRequest.PeriodID AND SET_DocumentApprove.Status =1 
	And SET_DocumentApprove.DocumentTypeID=HRMS_LeaveRequest.DocumentTypeID

INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = HRMS_LeaveRequest.CompanyID 
	AND SET_UserApprovalAmount.PeriodID = HRMS_LeaveRequest.PeriodID AND SET_UserApprovalAmount.Status = 1
	AND (SET_UserApprovalAmount.DocumentTypeID = HRMS_LeaveRequest.DocumentTypeID)
INNER JOIN SET_ApprovalAuthority ON SET_ApprovalAuthority.ApprovalAuthorityID = SET_UserApprovalAmount.ApprovalAuthorityID 
	AND SET_ApprovalAuthority.CompanyID = SET_UserApprovalAmount.CompanyID AND SET_ApprovalAuthority.PeriodID = SET_UserApprovalAmount.PeriodID 
	AND SET_ApprovalAuthority.Status = 1
INNER JOIN SET_Authority ON SET_Authority.AuthorityID = SET_ApprovalAuthority.AuthorityID 
	AND SET_Authority.CompanyID = SET_ApprovalAuthority.CompanyID AND SET_Authority.PeriodID = SET_ApprovalAuthority.PeriodID 
	AND SET_Authority.Status = 1
WHERE 
    HRMS_LeaveRequest.Status = 1 AND HRMS_LeaveRequest.PeriodID = @PeriodID 
    AND HRMS_LeaveRequest.CompanyID = @CompanyID AND HRMS_LeaveRequest.LeaveRequestID = @RequestID AND HRMS_LeaveRequest.DocumentTypeID = @DocumentTypeID 
    
    
order by SET_Authority.AuthorityLevel
";

        #endregion

        #region Class Methods

        public static DataTable GetAllDataWithOtherTables(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_SelectAll_WithOtherTables_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_SelectAll_WithOtherTables_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_SelectAll_WithOtherTables_WithOutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_SelectAll_WithOtherTables_WithOutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataRow GetTotalLeavesOfMonthByPersonID(int companyID, int personID, string Month, string Year, SqlConnection objConnection = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PersonID", personID);
                sqlparams[2] = new SqlParameter("@Month", Month);
                sqlparams[3] = new SqlParameter("@Year", Year);


                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetTotalShortLeavesOfMonthByPersonID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetTotalShortLeavesOfMonthByPersonID, sqlparams);

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

        public static DataRow GetLeaveDetailByPersonID(int companyID, int personID, int LeaveTypeID, bool IsActive, SqlConnection objConnection = null,int CurrentFiscalID=0)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", companyID);
                sqlparams[2] = new SqlParameter("@PersonID", personID);
                sqlparams[3] = new SqlParameter("@LeaveTypeID", LeaveTypeID);
                sqlparams[4] = new SqlParameter("@IsActive", IsActive);
                sqlparams[5] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_GetLeaveDetailByPersonID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_GetLeaveDetailByPersonID, sqlparams);

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


        public static DataTable GetHalfDaySlipData(Nullable<DateTime> DateFrom, Nullable<DateTime> DateTo, int companyID, int? personID = 0, int? LeaveID = 0, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", companyID);
               // sqlparams[2] = new SqlParameter("@PersonID", personID);
                //sqlparams[3] = new SqlParameter("@LeaveTypeID", LeaveTypeID);
                //sqlparams[4] = new SqlParameter("@IsActive", IsActive);

                if (personID > 0)
                {
                    sb.Append(" and HRMS_LeaveRequest.FK_PersonID =  " + personID);
                }

                if (LeaveID > 0)
                {
                    sb.Append(" and HRMS_LeaveRequest.LeaveRequestID =  " + LeaveID);
                }

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetHalfDaySlip,sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetHalfDaySlip, sb.ToString()), sqlparams);

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

        public static DataTable GetHolidayVisitSlip(Nullable<DateTime> DateFrom, Nullable<DateTime> DateTo, int companyID,int PeriodID, int? personID = 0,int?TMSIDTO = 0,SqlConnection objConnection = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            int? TMSIDFrom = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", companyID);
                // sqlparams[2] = new SqlParameter("@PersonID", personID);
                //sqlparams[3] = new SqlParameter("@LeaveTypeID", LeaveTypeID);
                //sqlparams[4] = new SqlParameter("@IsActive", IsActive);

                if (personID > 0)
                {
                    sb.Append(" and HRMS_TMSDATA.PersonID =  " + personID);
                }

                if (TMSIDTO > 0)
                {
                    TMSIDFrom = TMSIDTO - 1;
                    sb.Append(" and TableID between " + TMSIDFrom + " and " + TMSIDTO + "");
                }
                if (DateFrom != null && DateTo != null && TMSIDTO > 0 )
                {
                    sb.Append(" and Convert(Date,TMSDateTime) between '" + DateFrom.Value.ToString("yyyy/MM/dd") + "' and '" + DateTo.Value.ToString("yyyy/MM/dd") + "'");
                }

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(QUery_GetHolidayVisitSlip, sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(QUery_GetHolidayVisitSlip, sb.ToString()), sqlparams);

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

        public static DataTable GetLeaveType(SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
              
                DataSet ds = null;

                 if (objConnection != null)
                     ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetLeaveType);
                else
                     ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetLeaveType);

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



        public static DataTable GetAllDataWithOtherTablesForApproveDetail(int companyID, int periodID, int RequestID, int documentTypeID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@RequestID", RequestID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", documentTypeID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_ForApproveDetail, sqlparams);

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

        public static DataRow GetDataByKey1(int companyID, int LeaveRequestID, int periodID)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@LeaveRequestID", LeaveRequestID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byKey1, sqlparams);

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

        public static DataTable GetDataOfLeave(int companyID, int periodID, int FK_PersonID, Nullable<int> FK_PayrollTypeID, Nullable<short> FK_PayrollTypeCode, DateTime FromDate, DateTime ToDate)
        {
            DataTable dt = null;
        
            const string Query_GetAllData = @"Select * from HRMS_LeaveRequest Where Status=1 and IsApproved=1 
            and CompanyID=@CompanyID 
            and periodID=@periodID and FK_PersonID=@FK_PersonID 
            and CAST(CONVERT(VARCHAR(10), [HRMS_LeaveRequest].[FromDate], 110) AS DATETIME)=@FromDate
            and CAST(CONVERT(VARCHAR(10), [HRMS_LeaveRequest].[ToDate], 110) AS DATETIME)=@ToDate
            and FK_PayrollTypeID=@FK_PayrollTypeID and 
            FK_PayrollTypeCode=@FK_PayrollTypeCode";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@periodID", periodID);
                sqlparams[2] = new SqlParameter("@FK_PersonID", FK_PersonID);
                if (FK_PayrollTypeID.HasValue)
                    sqlparams[3] = new SqlParameter("@FK_PayrollTypeID", FK_PayrollTypeID.Value);
                else
                    sqlparams[3] = new SqlParameter("@FK_PayrollTypeID", DBNull.Value);

                if (FK_PayrollTypeCode.HasValue)
                    sqlparams[4] = new SqlParameter("@Fk_PayrollTypeCode", FK_PayrollTypeCode.Value);
                else
                    sqlparams[4] = new SqlParameter("@Fk_PayrollTypeCode", DBNull.Value);

                if (FromDate!=null)
                    sqlparams[5] = new SqlParameter("@FromDate", FromDate.Date);
                else
                    sqlparams[5] = new SqlParameter("@FromDate", DBNull.Value);

                if (ToDate!=null)
                    sqlparams[6] = new SqlParameter("@ToDate", ToDate.Date);
                else
                    sqlparams[6] = new SqlParameter("@ToDate", DBNull.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllData, sqlparams);

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


        public static DataTable GetDataLeaveBalance(int companyID, int periodID, int FK_PersonID,SqlTransaction sqltrans = null)
        {
            DataTable dt = null;

            const string Query_GetAllData = @"
            select * from (
			select HRMS_PayrollType .Description ,HRMS_EmployeePayroll .LeaveDays
			-
			isnull((
			select count(LeaveRequestID) 
			from HRMS_LeaveRequest 
			inner join SET_PeriodCustom on HRMS_LeaveRequest.FromDate >= SET_PeriodCustom.StartDate
			and HRMS_LeaveRequest.ToDate <= SET_PeriodCustom.EndDate
			where HRMS_LeaveRequest.FK_PayrollTypeID = HRMS_PayrollType.PayrollTypeID
			and HRMS_LeaveRequest.Fk_PayrollTypeCode = HRMS_PayrollType.PayrollTypeCode
            and HRMS_LeaveRequest.Fk_PersonID = HRMS_EmployeePayroll.PersonID
            and HRMS_LeaveRequest.status = 1
			 ),0)
			 as Balance
            ,sortingOrder,HRMS_EmployeePayroll.PayrollTypeCode,HRMS_EmployeePayroll.PayrollTypeID
			from HRMS_EmployeePayroll 
			inner join HRMS_PayrollType  on HRMS_PayrollType.PayrollTypeID = HRMS_EmployeePayroll .PayrollTypeID
			and HRMS_PayrollType.PayrollTypeCode = HRMS_EmployeePayroll .PayrollTypeCode
			where HRMS_EmployeePayroll.PayrollTypeCode=4
			and HRMS_EmployeePayroll.PersonID = @FK_PersonID
			)as tab
			where Balance > 0 
			order by sortingOrder
";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@periodID", periodID);
                sqlparams[2] = new SqlParameter("@FK_PersonID", FK_PersonID);

                DataSet ds = null;

                if (sqltrans != null)
                    ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query_GetAllData , sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllData, sqlparams);

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

        public static bool DeleteDataFromLeaeves(int companyID, int periodID, string month, string year, int PersonID, SqlTransaction objTransaction)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Month", month);
                sqlparams[3] = new SqlParameter("@Year", year);
                sqlparams[4] = new SqlParameter("@PersonID", PersonID);


                const string Query_DeleteDataFromLeaeves = @" update HRMS_LeaveRequest set Status = 0 where LeavePurpose='auto Leave adjustment' and  datepart(month,HRMS_LeaveRequest.FromDate) = @Month
			and datename(year,HRMS_LeaveRequest.FromDate) = @Year and Fk_PersonID = @PersonID";


                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteDataFromLeaeves, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DeleteDataFromLeaeves, sqlparams) > 0)
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

    }
}
