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
    public partial class HRMS_OverTime
    {
        #region Constant String

        const string Query_OvertimeReportData = @"
select 
HRMS_SET_Employee.PersonID
,HRMS_SET_Employee.Fullname
,HRMS_SET_Employee.EmployeeCode
,HRMS_SET_Employee.FatherName
,HRMS_SET_Employee.BasicSalary
,SET_Department.DepartmentName 
,HRMS_OverTime.PayrollTypeID,
HRMS_PayrollType.Description as PayrollType
,FK_SectionID,SectionName,
HRMS_OverTime.*
 from HRMS_OverTime
inner join HRMS_SET_Employee on HRMS_SET_Employee.PersonID =  HRMS_OverTime.PersonID
and HRMS_SET_Employee.CompanyID = HRMS_OverTime.CompanyID
and HRMS_SET_Employee.Status =1 
left join SET_Department on SET_Department.DepartmentID =  HRMS_SET_Employee.FK_DepartmentID
and SET_Department.Status =1 
inner Join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID=HRMS_OverTime.PayrollTypeID
AND HRMS_PayrollType.PayrollTypeCode=HRMS_OverTime.PayrollTypeCode
AND HRMS_PayrollType.Status=1
left join SET_Section on SET_Section.SectionID = HRMS_SET_Employee.FK_SectionID
where HRMS_OverTime.Status=1 And
HRMS_OverTime.CompanyID = @CompanyID
and HRMS_OverTime.PeriodID = @PeriodID
 And (HRMS_OverTime.FromDate >= @FromDate Or @FromDate is null) 
 And (HRMS_OverTime.FromDate <= @ToDate  Or @ToDate is null)
{0}
";

        const string Query_SelectAll_WithPaging_Search = @"SELECT [HRMS_OverTime].[CompanyID], [HRMS_OverTime].[PeriodID], [HRMS_OverTime].[PayrollTypeID], 
[HRMS_OverTime].[PayrollTypeCode], [HRMS_OverTime].[OverTimeID], CONVERT(VARCHAR(10),[HRMS_OverTime].[FromDate],105) AS FromDate, CONVERT(VARCHAR(10),[HRMS_OverTime].[ToDate],105) AS ToDate, 
[HRMS_OverTime].[PersonID], HRMS_Set_Employee.Fullname, [HRMS_OverTime].[Salary], [HRMS_OverTime].[OverTimeHours], [HRMS_OverTime].[OverTimeRate], 
[HRMS_OverTime].[OverTimeAmount], [HRMS_OverTime].[Status], [HRMS_OverTime].[CreatedBy], [HRMS_OverTime].[CreateDate], 
[HRMS_OverTime].[CreatedByValue], [HRMS_OverTime].[UpdatedBy], [HRMS_OverTime].[UpdateDate], 
[HRMS_OverTime].[UpdatedByValue], HRMS_PayrollType.Description as PayrollType,
ROW_NUMBER() Over (Order by [HRMS_OverTime].CompanyID, [HRMS_OverTime].OverTimeID desc, [HRMS_OverTime].PeriodID) as 'Row' 

FROM [HRMS_OverTime]

Inner Join HRMS_Set_Employee on HRMS_OverTime.PersonID = HRMS_Set_Employee.PersonID
and HRMS_OverTime.CompanyID = HRMS_Set_Employee.CompanyID
Inner join HRMS_EmployeeInEmployement_Status on HRMS_EmployeeInEmployement_Status.StatusID = HRMS_Set_Employee.StatusID
	 --and HRMS_EmployeeInEmployement_Status.UserID = @UserID
     and HRMS_EmployeeInEmployement_Status.[CompanyID] = HRMS_Set_Employee.[CompanyID]
left join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_OverTime.PayrollTypeID and HRMS_PayrollType.Status = 1
";

        const string Query_SelectAll_WithoutPaging_Search = @"SELECT [HRMS_OverTime].[CompanyID], [HRMS_OverTime].[PeriodID], [HRMS_OverTime].[PayrollTypeID], 
[HRMS_OverTime].[PayrollTypeCode], [HRMS_OverTime].[OverTimeID], CONVERT(VARCHAR(10),[HRMS_OverTime].[FromDate],105) AS FromDate, CONVERT(VARCHAR(10),[HRMS_OverTime].[ToDate],105) AS ToDate,  
[HRMS_OverTime].[PersonID], [HRMS_OverTime].[Salary], [HRMS_OverTime].[OverTimeHours], [HRMS_OverTime].[OverTimeRate], 
[HRMS_OverTime].[OverTimeAmount], [HRMS_OverTime].[Status], [HRMS_OverTime].[CreatedBy], [HRMS_OverTime].[CreateDate], HRMS_PayrollType.Description as PayrollType,
[HRMS_OverTime].[CreatedByValue], [HRMS_OverTime].[UpdatedBy], [HRMS_OverTime].[UpdateDate], [HRMS_OverTime].[UpdatedByValue] 
FROM [HRMS_OverTime]

Inner Join HRMS_Set_Employee on HRMS_OverTime.PersonID = HRMS_Set_Employee.PersonID
and HRMS_OverTime.CompanyID = HRMS_Set_Employee.CompanyID
Inner join HRMS_EmployeeInEmployement_Status on HRMS_EmployeeInEmployement_Status.StatusID = HRMS_Set_Employee.StatusID
	-- and HRMS_EmployeeInEmployement_Status.UserID = @UserID
     and HRMS_EmployeeInEmployement_Status.[CompanyID] = HRMS_Set_Employee.[CompanyID]
left join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_OverTime.PayrollTypeID and HRMS_PayrollType.Status = 1
";

        const string Query_GetData_byKey_Search = @"SELECT *, [HRMS_Set_Employee].[FullName],HRMS_PayrollType.Description FROM [HRMS_OverTime] 
Inner Join HRMS_Set_Employee on HRMS_OverTime.PersonID = HRMS_Set_Employee.PersonID
and HRMS_OverTime.CompanyID = HRMS_Set_Employee.CompanyID
left join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_OverTime.PayrollTypeID and HRMS_PayrollType.Status = 1
WHERE HRMS_OverTime.[CompanyID] = @CompanyID and HRMS_OverTime.[OverTimeID] = @OverTimeID and HRMS_OverTime.[PeriodID] = @PeriodID";

        const string Query_CheckDuplicate2 = @"SELECT * FROM [HRMS_OverTime] WHERE [Status] = 1 AND ([CompanyID] = @CompanyID ) AND ([PeriodID] = @PeriodID) AND ([PersonID]=@PersonID) AND (PayrollTypeID = @PayrollTypeID or  @PayrollTypeID is null) AND ([OverTimeID] <> @overtimeID)
AND ((FromDate between CONVERT(varchar,@FromDate,101)and CONVERT(varchar,@ToDate,101)) or ToDate between CONVERT(varchar,@FromDate,101)and CONVERT(varchar,@ToDate,101))";

        const string Query_CheckDuplicate = @"SELECT * FROM [HRMS_OverTime] WHERE [Status] = 1 AND ([CompanyID] = @CompanyID ) AND ([PeriodID] = @PeriodID) AND ([PersonID]=@PersonID) AND (PayrollTypeID = @PayrollTypeID or  @PayrollTypeID is null)
AND ((FromDate between CONVERT(varchar,@FromDate,101)and CONVERT(varchar,@ToDate,101)) or ToDate between CONVERT(varchar,@FromDate,101)and CONVERT(varchar,@ToDate,101))";

        const string Query_OTHours = @"   select (isnull(OverTimeHours,0)) as OverTimeHours from HRMS_Overtime 
	 
		where HRMS_Overtime.Status = 1 
		and year(HRMS_Overtime.FromDate) = @Year
		and month(HRMS_Overtime.FromDate) = @Month 
		and HRMS_Overtime.PersonID = @PersonID";

        const string Query_ShiftHours = @"     select Round((DATEDIFF(second, TimeIn, TimeOut) / 3600.0),0) as Shifthours from HRMS_EmployeeShift 

	 inner join HRMS_Set_Shift on HRMS_Set_Shift.ShiftID = HRMS_EmployeeShift.ShiftID
	 and HRMS_Set_Shift.Status = 1
		where HRMS_EmployeeShift.Status = 1 
		--and year(HRMS_Overtime.FromDate) = @Year
		--and month(HRMS_Overtime.FromDate) = @Month 
		and HRMS_EmployeeShift.PersonID = @PersonID";


        const string Query_OverTimeForPosting = @"Select HRMS_Set_Employee.StatusID,HRMS_Set_Employee.EmployeeCode,HRMS_Set_Employee.FullName as EmployeeName
,HRMS_OverTime.* ,HRMS_PayrollType.Description as OverTimeType, HRMS_PayrollType.PayrollTypeID
,(select DISTINCT FK_CodeCombinationID from AR_SET_BusinessPartnerTypes
inner join AR_SET_BusinessPartner on AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode
 where AR_SET_BusinessPartnerTypes.BusinessPartnerID = HRMS_Set_Employee.FK_CustomerID 
 and AR_SET_BusinessPartner.BusinessPartnerName = 'over time' )OverTimeCode
from HRMS_OverTime
		inner Join HRMS_Set_Employee on HRMS_Set_Employee.PersonID=HRMS_OverTime.PersonID
		Inner Join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID=HRMS_OverTime.PayrollTypeID
		and HRMS_PayrollType.Status=1
WHERE [HRMS_OverTime].[Status] = 1 
And ([HRMS_OverTime].IsPosted is null OR [HRMS_OverTime].IsPosted = 0 )
And CAST([HRMS_OverTime].FromDate AS DATE) >= @OverTimeDateFrom
And CAST([HRMS_OverTime].ToDate AS DATE) <= @OverTimeDateTo
and [HRMS_OverTime].CompanyID = @CompanyID 
and HRMS_Set_Employee.status = 1
";

        const string Query_DataHeaderDetailForOverTimePost = @"
SELECT OT.CompanyID,OT.PeriodID,OT.OverTimeID,OT.FromDate,OT.ToDate,OT.PayrollTypeID,
        OT.PersonID,
        OT.PayrollTypeCode,
		PT.ChartOfAccountID as GLAccountID,
        ARC.CustomerAccountCCID as GLEmpAccountCode,
        PT.GLCodeCombinationID as GLAccountCode,
        PT.GLImpact, 
        OT.OvertimeAmount as GLAmount,ARC.CustomerName,EMP.CostCenterID
        FROM HRMS_OverTime OT

        INNER JOIN HRMS_Set_Employee EMP on EMP.PersonID=OT.PersonID
	    AND EMP.CompanyID = OT.CompanyID
        left JOIN AR_SET_Customer ARC on ARC.CustomerID=EMP.FK_CustomerID 

        INNER JOIN [HRMS_PayrollType] PT on PT.PayrollTypeID=OT.PayrollTypeID 
		and PT.PayrollTypeCode=OT.PayrollTypeCode

        WHERE OT.[Status] = 1 
And CAST(OT.FromDate AS DATE) >= @OverTimeDateFrom
And CAST(OT.ToDate AS DATE) <= @OverTimeDateTo 
        --And (@OverTimeDate between FromDate and ToDate) 
		      And (OT.IsPosted is null OR OT.IsPosted = 0 )
AND OT.[CompanyID] = @CompanyID
        --AND OT.OverTimeID in (@OverTimeID)
        union all 
        SELECT OT.CompanyID,OT.PeriodID,OT.OverTimeID,OT.FromDate,OT.ToDate,OT.PayrollTypeID,
        OT.PersonID,
        OT.PayrollTypeCode,
		PT.ChartOfAccountID as GLAccountID,
        ARC.CustomerAccountCCID as GLEmpAccountCode,
        HRMS_PayRollTypeDetail.GLCodeCombination as GLAccountCode,
        PT.GLImpactTwo as GLImpact, 
        OT.OvertimeAmount as GLAmount,ARC.CustomerName,EMP.CostCenterID
        FROM HRMS_OverTime OT

        INNER JOIN HRMS_Set_Employee EMP on EMP.PersonID=OT.PersonID
	    AND EMP.CompanyID = OT.CompanyID
        left JOIN AR_SET_Customer ARC on ARC.CustomerID=EMP.FK_CustomerID 

        INNER JOIN [HRMS_PayrollType] PT on PT.PayrollTypeID=OT.PayrollTypeID 
		and PT.PayrollTypeCode=OT.PayrollTypeCode

		Inner Join HRMS_PayRollTypeDetail on HRMS_PayRollTypeDetail.PayrollTypeID=PT.PayrollTypeID
		And HRMS_PayRollTypeDetail.EmployeeStatusCode=EMP.StatusID
		And HRMS_PayRollTypeDetail.ExpenseTypeID=EMP.ExpenseTypeID
		--And HRMS_PayRollTypeDetail.CompanyID=PT.CompanyID
		And HRMS_PayRollTypeDetail.Status=1
        WHERE OT.[Status] = 1 
And CAST(OT.FromDate AS DATE) >= @OverTimeDateFrom
And CAST(OT.ToDate AS DATE) <= @OverTimeDateTo 
        --And (@OverTimeDate between FromDate and ToDate)  
        And (OT.IsPosted is null OR OT.IsPosted = 0 )
AND OT.[CompanyID] = @CompanyID
order By  PayrollTypeCode
";

        #endregion

        #region Method

        public static DataTable GetAllData_Search(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_WithPaging_Search + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPaging_Search + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_WithoutPaging_Search + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
//Query_OvertimeReportData
        public static DataTable GetOvertimeReportData(int companyID,  int periodID,
            Nullable <DateTime> datefrom, Nullable<DateTime > dateto,Nullable<int> personfrom , 
            Nullable<int > personto,Nullable<int > DepartmentFrom , Nullable<int > DepartmentTo,
            SqlConnection objConnection = null, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, int EmployeeStatusID = 0, Nullable<Guid> UserID = null, int OTTypeID = 0)
        {
           DataTable dt = null;
           StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (datefrom.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", datefrom);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@FromDate", DBNull.Value);
                }
                if (dateto.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@ToDate", dateto);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@ToDate", DBNull.Value);
 
                }
                //datefrom.Value.Date.Date.Date
                DataSet ds = null;

                if (personfrom != null && personto != null)
                {
                    sb.Append(" and HRMS_SET_Employee.PersonID between " + personfrom + "  and " + personto + " ");

                }

                else
                    if (personfrom != null)
                    {
                        sb.Append(" and HRMS_SET_Employee.PersonID = " + personfrom + " ");
                    }
                    else if (personto != null)
                    {
                        sb.Append(" and HRMS_SET_Employee.PersonID = " + personto + " ");
                    }

                if (DepartmentFrom != null && DepartmentTo != null)
                {
                    sb.Append("  and SET_Department.DepartmentID between " + DepartmentFrom + " and " + DepartmentTo + " ");
                }
                else if (DepartmentFrom != null)
                {
                    sb.Append(" and SET_Department.DepartmentID =  " + DepartmentFrom + " ");
                }
                else if (DepartmentTo != null)
                {
                    sb.Append(" and SET_Department.DepartmentID = " + DepartmentTo + "");
                }

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
                    sb.Append(" and HRMS_Set_Employee.PersonTypeCode = " + EmployeeTypeID);
                }
                if (OTTypeID > 0)
                {
                    sb.Append(" And HRMS_PayrollType.PayrollTypeID = " + OTTypeID);
                }
                if (EmployeeStatusID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + EmployeeStatusID);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                }

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format( Query_OvertimeReportData ,sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_OvertimeReportData,sb), sqlparams);

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


        public static DataTable GetOvertimeArrearReportData(int companyID, int periodID,
            Nullable<DateTime> datefrom, Nullable<DateTime> dateto, Nullable<int> personfrom,
            Nullable<int> personto, Nullable<int> DepartmentFrom, Nullable<int> DepartmentTo,
            SqlConnection objConnection = null, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, int EmployeeStatusID = 0, Nullable<Guid> UserID = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (datefrom.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", datefrom);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@FromDate", DBNull.Value);
                }
                if (dateto.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@ToDate", dateto);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@ToDate", DBNull.Value);

                }
                //datefrom.Value.Date.Date.Date
                DataSet ds = null;

                if (personfrom != null && personto != null)
                {
                    sb.Append(" and HRMS_SET_Employee.PersonID between " + personfrom + "  and " + personto + " ");

                }

                else
                    if (personfrom != null)
                    {
                        sb.Append(" and HRMS_SET_Employee.PersonID = " + personfrom + " ");
                    }
                    else if (personto != null)
                    {
                        sb.Append(" and HRMS_SET_Employee.PersonID = " + personto + " ");
                    }

                if (DepartmentFrom != null && DepartmentTo != null)
                {
                    sb.Append("  and SET_Department.DepartmentID between " + DepartmentFrom + " and " + DepartmentTo + " ");
                }
                else if (DepartmentFrom != null)
                {
                    sb.Append(" and SET_Department.DepartmentID =  " + DepartmentFrom + " ");
                }
                else if (DepartmentTo != null)
                {
                    sb.Append(" and SET_Department.DepartmentID = " + DepartmentTo + "");
                }

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
                    sb.Append(" and HRMS_Set_Employee.PersonTypeCode = " + EmployeeTypeID);
                }
                if (EmployeeStatusID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + EmployeeStatusID);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                }

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_OvertimeArrearReportData, sb), sqlparams);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_OvertimeArrearReportData, sb), sqlparams);
                }

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


        public static DataRow GetDataByKeySearch(int companyID, int overTimeID, int periodID, SqlConnection objConnection = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@OverTimeID", overTimeID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byKey_Search, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byKey_Search, sqlparams);

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

        public static DataTable GetDuplicateData(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> personID, string FromDate, string ToDate, Nullable<int> overtimeID, Nullable<int> PayrollTypeID, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", personID);
                sqlparams[3] = new SqlParameter("@FromDate", FromDate);
                sqlparams[4] = new SqlParameter("@ToDate", ToDate);
                if (PayrollTypeID > 0)
                {
                    sqlparams[5] = new SqlParameter("@PayrollTypeID", PayrollTypeID);
                }
                else
                {
                    sqlparams[5] = new SqlParameter("@PayrollTypeID", DBNull.Value);
                }

                    if (overtimeID.HasValue)
                {
                    sqlparams[6] = new SqlParameter("@overtimeID", overtimeID);
                }


                DataSet ds = null;

                if (objConnection == null)
                {
                    if (overtimeID.HasValue)
                    { ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_CheckDuplicate2, sqlparams); }
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_CheckDuplicate, sqlparams);
                }
                else
                {
                    if (overtimeID.HasValue)
                    { ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_CheckDuplicate2, sqlparams); }
                    else
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_CheckDuplicate, sqlparams);
                }

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


        public static DataTable GetDataHeaderForPost(Nullable<DateTime> OverTimeDateTo, Nullable<DateTime> OverTimeDateFrom, SqlTransaction sqltrans = null, SqlConnection objConnection = null, Nullable<int> overTimeTypeID = null, Nullable<int> StatusCode = null, int CompanyID = 0)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@OverTimeDateFrom", OverTimeDateFrom);
                sqlparams[1] = new SqlParameter("@OverTimeDateTo", OverTimeDateTo);
                sqlparams[2] = new SqlParameter("@CompanyID", CompanyID);

                DataSet ds = null;

                if (StatusCode.HasValue)
                {
                    sb.Append(" And HRMS_Set_Employee.StatusID = " + StatusCode + " ");
                }

                if (overTimeTypeID.HasValue)
                {
                    sb.Append(" AND HRMS_OverTime.PayrollTypeID = " + overTimeTypeID + " ");
                }

                if (objConnection == null)
                {
                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, string.Format(Query_OverTimeForPosting + sb), sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_OverTimeForPosting + sb), sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_OverTimeForPosting + sb), sqlparams);

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

        public static DataTable Get_DIInvoiceOTDocument(int companyID, int periodID)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_DocumentTypeDirectInvoiceOT, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetDataHeaderDetailForOTPost(Nullable<DateTime> ToOverTimeDate, Nullable<DateTime> FromOverTimeDate, string OverTimeIDs, int companyID, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@OverTimeDateFrom", FromOverTimeDate);
                sqlparams[1] = new SqlParameter("@OverTimeDateTo", ToOverTimeDate);
                sqlparams[2] = new SqlParameter("@OverTimeID", OverTimeIDs);
                sqlparams[3] = new SqlParameter("@CompanyID", companyID);
                DataSet ds = null;

                if (objConnection == null)
                {
                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query_DataHeaderDetailForOverTimePost, sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_DataHeaderDetailForOverTimePost , sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_DataHeaderDetailForOverTimePost, sqlparams);


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

        public bool UpdateEmpForPostingByKey(SqlTransaction objTransaction = null)
        {
            bool result = false;
            const string Query_UpdateForPosting_By_PrimaryKey = @" UPDATE [HRMS_OverTime] SET [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue, [IsPosted] = @IsPosted
		WHERE [CompanyID] = @CompanyID and [OverTimeID] = @OverTimeID and [PeriodID] = @PeriodID
		AND [PayrollTypeID] = @PayrollTypeID AND [PayrollTypeCode] = @PayrollTypeCode ";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[10];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[2] = new SqlParameter("@PayrollTypeID", f_PayrollTypeID);
                sqlparams[3] = new SqlParameter("@PayrollTypeCode", f_PayrollTypeCode);
                sqlparams[4] = new SqlParameter("@OverTimeID", f_OverTimeID);
                               
                sqlparams[5] = new SqlParameter("@Status", f_Status);
                sqlparams[6] = new SqlParameter("@IsPosted", f_IsPosted);

                if (f_UpdatedBy.HasValue)
                    sqlparams[7] = new SqlParameter("@UpdatedBy", f_UpdatedBy.Value);
                else
                    sqlparams[7] = new SqlParameter("@UpdatedBy", DBNull.Value);

                if (f_UpdateDate.HasValue)
                    sqlparams[8] = new SqlParameter("@UpdateDate", f_UpdateDate.Value);
                else
                    sqlparams[8] = new SqlParameter("@UpdateDate", DBNull.Value);

                if (!string.IsNullOrEmpty(f_UpdatedByValue))
                    sqlparams[9] = new SqlParameter("@UpdatedByValue", f_UpdatedByValue);
                else
                    sqlparams[9] = new SqlParameter("@UpdatedByValue", DBNull.Value);
                
                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateForPosting_By_PrimaryKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_UpdateForPosting_By_PrimaryKey, sqlparams) > 0)
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

        public static DataTable GetOThours(int companyID, int personID, DateTime datetime)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                sqlparams[1] = new SqlParameter("@PersonID", personID);
                sqlparams[2] = new SqlParameter("@Month", datetime.Month.ToString());
                sqlparams[3] = new SqlParameter("@Year", datetime.Year.ToString());
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_OTHours, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetShifthours(int companyID, int personID, DateTime datetime)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                sqlparams[1] = new SqlParameter("@PersonID", personID);
                sqlparams[2] = new SqlParameter("@Month", datetime.Month.ToString());
                sqlparams[3] = new SqlParameter("@Year", datetime.Year.ToString());
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_ShiftHours, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetCheckpersonID(Nullable<int> companyID, int personID, DateTime? FromDate, DateTime? ToDate)
        {
            DataTable dt = null;
            #region query
            const string Query_datforcheckperson = @"
                   Select * from HRMS_OverTime
            where HRMS_OverTime.Status = 1 and HRMS_OverTime.PersonID = @PersonID
                   And HRMS_OverTime.CompanyID = @CompanyID 
                   And HRMS_OverTime.FromDate >= @FromDate
                   And HRMS_OverTime.ToDate <= @ToDate
                 ";
            #endregion
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PersonID", personID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);
               
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_datforcheckperson, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetDataByPersonID_ActiveStatus(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> personID, DateTime datetime, Nullable<int> overtimeID, Nullable<int> PayrollTypeID, SqlConnection objConnection = null)
        {

            SqlParameter[] sqlparams = new SqlParameter[5];
            sqlparams[0] = new SqlParameter("@CompanyID", companyID);
            sqlparams[1] = new SqlParameter("@PeriodID", periodID);
            sqlparams[2] = new SqlParameter("@personID", personID);
            sqlparams[3] = new SqlParameter("@FromDate", datetime);
            //sqlparams[3] = new SqlParameter("@Month", datetime.Month.ToString());
            //sqlparams[4] = new SqlParameter("@Year", datetime.Year.ToString());
            sqlparams[4] = new SqlParameter("@PayrollTypeID", PayrollTypeID);

            const string Query_forActiveData = @"
select HRMS_SalaryDetail.PayrollTypeID, * from HRMS_SalaryHeader 
left JOIN HRMS_SalaryDetail on HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID 
and HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID 
and HRMS_SalaryDetail.Status = 1

WHERE HRMS_SalaryHeader.Status = 1
AND HRMS_SalaryHeader.CompanyID = @CompanyID 
AND HRMS_SalaryHeader.PeriodID = @PeriodID 
AND HRMS_SalaryHeader.PersonID = @personID
 AND HRMS_SalaryDetail.PayrollTypeID = @PayrollTypeID
--AND Month(HRMS_SalaryHeader.SalaryDate) =	@Month
--AND Year(HRMS_SalaryHeader.SalaryDate) =	@Year
And HRMS_SalaryHeader.SalaryDate = @FromDate

            ";

            DataTable dt = null;
            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_forActiveData), sqlparams).Tables[0];
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
