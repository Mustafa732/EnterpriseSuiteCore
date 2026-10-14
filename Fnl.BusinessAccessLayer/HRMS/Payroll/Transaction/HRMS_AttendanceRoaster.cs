using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;
using System.Text;
namespace FnI.BusinessAccessLayer.HRMS.Payroll.Transaction
{
    public partial class HRMS_AttendanceRoaster
    {
        #region queries
        const string Query_CheckDuplicate = @"Select * from HRMS_AttendanceRoaster where CompanyID = @CompanyID and PeriodID = @PeriodID and Status = 1 
--and ShiftFrom between @FromDate and  @ToDate 
and ShiftFrom = @FromDate
{0}
";

        const string Query_Update_OffDays = @"
        UPDATE [HRMS_AttendanceRoaster] SET  [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue, [OffDays] = @OffDays
		WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [PersonID] = @PersonID and ShiftFrom = @ShiftFrom";

//        const string QUERy_Get_EmpPresentdays = @"
//   
//         ";


        const string query = @"
select PersonID , Fullname,BasicSalary  from HRMS_SET_Employee  
where  Status = 1 
and CompanyID = @CompanyID 

{0}
";

//        const string query_get_timingforemp = @" 
//        
//
//";

        const string Query_SelectAll_RaosterWithPaging = @" Select min(COnvert(Date,ShiftFrom)) as FromDate,max(Convert(Date,Shiftto)) as Todate,HRMS_Set_shift.Description,FullName 
,HRMS_AttendanceRoaster.PersonID,HRMS_Set_shift.ShiftID,RoastershiftID
,ROW_NUMBER() Over (Order by HRMS_AttendanceRoaster.CompanyID, PeriodID, HRMS_AttendanceRoaster.ShiftID) as 'Row' 
from HRMS_AttendanceRoaster
inner join HRMS_Set_shift on HRMS_Set_shift.ShiftID = HRMS_AttendanceRoaster.ShiftID
inner join HRMS_SET_Employee on HRMS_SET_Employee.PersonID = HRMS_AttendanceRoaster.PErsonID



--where HRMS_SET_Employee.PErsonID = 9

";

        const string Query_SelectAll_RosterWithoutPaging = @" Select min(COnvert(Date,ShiftFrom)) as FromDate,max(Convert(Date,Shiftto)) as Todate,HRMS_Set_shift.Description,FullName 
,HRMS_AttendanceRoaster.PersonID,HRMS_Set_shift.ShiftID,RoastershiftID

from HRMS_AttendanceRoaster
inner join HRMS_Set_shift on HRMS_Set_shift.ShiftID = HRMS_AttendanceRoaster.ShiftID
inner join HRMS_SET_Employee on HRMS_SET_Employee.PersonID = HRMS_AttendanceRoaster.PErsonID



--where HRMS_SET_Employee.PErsonID = 9

";
        const string Query_SelectAllData = @"
Select min(COnvert(Date,ShiftFrom)) as ShiftFrom,max(Convert(Date,Shiftto)) as Shiftto,HRMS_AttendanceRoaster.PersonID,HRMS_AttendanceRoaster.ShiftID
,FullName,HRMS_AttendanceRoaster.EmployeeCode,Description,RoastershiftID
 from HRMS_AttendanceRoaster

inner join HRMS_SET_Employee on HRMS_SET_Employee.PersonID = HRMS_AttendanceRoaster.PErsonID
inner join HRMS_Set_shift on HRMS_Set_shift.ShiftID = HRMS_AttendanceRoaster.ShiftID


where HRMS_AttendanceRoaster.Status = 1
and HRMS_AttendanceRoaster.CompanyID  = @CompanyID
and HRMS_AttendanceRoaster.PeriodID  = @PeriodID
and HRMS_AttendanceRoaster.PErsonID = @PersonID

and HRMS_AttendanceRoaster.RoastershiftID = @RoastershiftID


group by RoastershiftID,FullName,Description
,HRMS_AttendanceRoaster.ShiftID,HRMS_AttendanceRoaster.PersonID,HRMS_AttendanceRoaster.EmployeeCode";

        const string query_getmaxid = @" SELECT IsNull(max(RoasterShiftID), 0) + 1 as RoasterID FROM HRMS_AttendanceRoaster WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID ";

        const string Query_Update_By_PersonID = @" update HRMS_AttendanceRoaster set Shiftid = @ShiftID , ShiftFrom = @ShiftFrom , ShiftTo = @ShiftTo
where CompanyID = @CompanyID and  PeriodID = @PeriodID and PersonID = @PersonID and RoastershiftID = @RoastershiftID ";

        const string Query_getRoasterForProcess = @" Select FullName,Employeecode,PersonID,Reason,Max(TimeIN) as TimeIN,Max(Timeout) as TimeOut from 
(
Select FullName,EmployeeCode,HRMS_TMSDATA.PersonID,DepartmentName,Reason,TMSDATETIME as TimeIN,null as TimeOUT
from HRMS_TMSDATA
	
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_TMSDATA.PersonID
inner join Set_Department on Set_Department.DepartmentID =HRMS_Set_Employee.FK_DepartmentID
where
 --TableID between 45 and 50 and 
 StatusCode = 1

union all

Select FullName,EmployeeCode,HRMS_TMSDATA.PersonID,DepartmentName,Reason,null ,TMSDATETIME as TimeOut
from HRMS_TMSDATA
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_TMSDATA.PersonID
inner join Set_Department on Set_Department.DepartmentID =HRMS_Set_Employee.FK_DepartmentID
where 
--TableID between 45 and 50 and 
StatusCode = 2

) as tab

where Datepart(Month,TimeIn) = @Month or Datepart(Month,TimeOut) = @Month

group by FullName,EmployeeCode,Personid,DepartmentName,Reason


Order by PersonID,TimeIn
	  ";

        #endregion


        public static DataTable GetDuplicateDataRoaster(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> personID, Nullable<DateTime> FromDate, Nullable<DateTime> ToDate, Nullable<int> shiftid,string filter ="", SqlConnection objConnection = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            if (personID > 0)
            {
                sb.Append(" and  PersonID = " + personID);
            }

            if (shiftid > 0)
            {
                sb.Append(" and ShiftID = " + shiftid);
            
            }

            if (filter != "")
            {
                sb.Append(" and  offDays = 1 " );
            }



            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);
                
                DataSet ds = null;

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_CheckDuplicate,sb.ToString()), sqlparams); ;
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_CheckDuplicate, sb.ToString()), sqlparams);
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



        public bool UpdateDataoffdaysByPersonID(int CompanyID,int Periodid, int EmpID, bool Offdays,string UpdatedByVal,DateTime UpdateDates,Guid  UpdatedByy, DateTime FromDates, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[14];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", Periodid);
                sqlparams[2] = new SqlParameter("@PersonID", EmpID);

                sqlparams[3] = new SqlParameter("@UpdatedBy", UpdatedByy);


                sqlparams[4] = new SqlParameter("@UpdateDate", UpdateDates);


                sqlparams[5] = new SqlParameter("@UpdatedByValue", UpdatedByVal);


                sqlparams[6] = new SqlParameter("@OffDays", Offdays);

                sqlparams[7] = new SqlParameter("@ShiftFrom", FromDates);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_OffDays, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_OffDays, sqlparams) > 0)
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


        public static DataTable GetPresentDays(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> personID, Nullable<DateTime> FromDate, Nullable<DateTime> ToDate, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);


                if (personID != null && personID > 0)
                {
                    sb.Append("  where PersonID =  " + personID);
                }


                DataSet ds = null;

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.QUERy_Get_EmpPresentdays,sb.ToString()), sqlparams); ;
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.QUERy_Get_EmpPresentdays, sb.ToString()), sqlparams);
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

        public static DataTable getPersonIDS(int companyID, Nullable<int> fromPerson, Nullable<int> toPerson,Nullable<int> DesignationID,string filter)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();


            if (fromPerson != null && toPerson != null)
            {
                sb.Append(" and personID between " + fromPerson + " and " + toPerson);
            }

            else if (fromPerson != null)
            {
                sb.Append(" and personID = " + fromPerson );
            }

            if (DesignationID != null && DesignationID > 0)
            {
                sb.Append(" and FK_JobDesignationID = " + DesignationID);
            }

            if (filter != "")
            {
                sb.Append(filter);
            }

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@FromPerson", fromPerson);
                sqlparams[2] = new SqlParameter("@ToPerson", toPerson);
                sqlparams[2] = new SqlParameter("@DesignationID", DesignationID);

                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query,sb.ToString()), sqlparams);

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


        public static DataTable GetTimingForEmp(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> personID, Nullable<DateTime> FromDate, Nullable<DateTime> ToDate,SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);


                if (personID != null && personID > 0)
                {
                    sb.Append("  where fintab.personid =  " + personID);
                }


                DataSet ds = null;

                if (sqltrans == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.query_get_timingforemp, sb.ToString()), sqlparams); ;
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.query_get_timingforemp, sb.ToString()), sqlparams);
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


        public static DataTable GetAllRoastersData(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_SelectAll_RaosterWithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_RaosterWithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_RaosterWithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_RaosterWithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAll_RosterWithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_RosterWithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllActiveRoastersData(Nullable<int> companyID, Nullable<int> periodID, int PersonID, int RoastershiftID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", PersonID);
                sqlparams[3] = new SqlParameter("@RoastershiftID", RoastershiftID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAllData, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAllData, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllData, sqlparams);

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

        public static int GetNewShiftRoasterID(int companyID, int periodID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, query_getmaxid, sqlparams));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, query_getmaxid, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, query_getmaxid, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public bool UpdateData(int CompanyID, int PeriodID,int ShiftID, int PersonID, int RoastershiftID, DateTime ShiftFrom, DateTime ShiftTo, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@RoastershiftID", RoastershiftID);
                sqlparams[3] = new SqlParameter("@ShiftID", ShiftID);
                sqlparams[4] = new SqlParameter("@ShiftFrom", ShiftFrom);
                sqlparams[5] = new SqlParameter("@ShiftTo", ShiftTo);
                sqlparams[6] = new SqlParameter("@PersonID", PersonID);

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


        public static DataTable GetDataForRaosterReport(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> personID, Nullable<DateTime> FromDate, Nullable<DateTime> ToDate, Nullable<int> shiftid, Nullable<int> deptid, Nullable<int> designationid, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            const string query_report = @" Select  HRMS_AttendanceRoaster.PersonID,HRMS_Set_Employee.Fullname,HRMS_AttendanceRoaster.EmployeeCode,HRMS_AttendanceRoaster.ShiftID
,HRMS_Set_Shift.ShiftCode,
ShiftFrom as Date,Set_Department.DepartmentID,Set_Department.DepartmentName
from HRMS_AttendanceRoaster
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_AttendanceRoaster.PersonID
and HRMS_Set_Employee.CompanyID = HRMS_AttendanceRoaster.CompanyID
inner join HRMS_Set_Shift on HRMS_Set_Shift.ShiftID = HRMS_AttendanceRoaster.ShiftID 
inner join Set_Department on Set_Department.DepartmentID = HRMS_AttendanceRoaster.DepartmentID
inner join SET_JobDesignation on SET_JobDesignation.JobDesignationID = HRMS_Set_Employee.FK_JobDesignationID

where HRMS_AttendanceRoaster.CompanyID = @CompanyID
and HRMS_AttendanceRoaster.PeriodID = @PeriodID
and HRMS_AttendanceRoaster.Status = 1
and Convert(Date,ShiftFrom) between @FromDate and @ToDate

{0} ";

            if (personID > 0)
            {
                sb.Append(" and  HRMS_Set_Employee.PersonID = " + personID);
            }

            if (shiftid > 0)
            {
                sb.Append(" and HRMS_Set_Shift.ShiftID = " + shiftid);

            }
            if (deptid > 0)
            {
                sb.Append(" and Set_Department.DepartmentID = " + deptid);

            }
            if (designationid > 0)
            {
                sb.Append(" and SET_JobDesignation.JobDesignationID = " + designationid);
            }

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);

                DataSet ds = null;

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query_report, sb.ToString()), sqlparams); ;
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(query_report, sb.ToString()), sqlparams);
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


        public static DataTable GetDataForDesignationWiseRates(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> PayrolltypeID, Nullable<int> designationid, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            const string query_report = @" Select DesignationID,JobDesignationName,Rate,HRMS_PayrollType.PayrolltypeID,Description from HRMS_ExtraOT_Rates
inner  join Set_JobDesignation on Set_JobDesignation.JobDesignationID = HRMS_ExtraOT_Rates.DesignationID
and Set_JobDesignation.CompanyId = HRMS_ExtraOT_Rates.CompanyID
inner join HRMS_PayrollType on HRMS_PayrollType.PayrolltypeID =HRMS_ExtraOT_Rates.PayrolltypeID 
and HRMS_PayrollType.CompanyId = HRMS_ExtraOT_Rates.CompanyID


where HRMS_ExtraOT_Rates.Status = 1
and HRMS_ExtraOT_Rates.CompanyID = 1

{0} ";
           
            if (designationid > 0)
            {
                sb.Append(" and SET_JobDesignation.JobDesignationID = " + designationid);
            }

            if (PayrolltypeID > 0)
            {
                sb.Append(" and HRMS_PayrollType.Payrolltypeid =  " + PayrolltypeID);
            }
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                DataSet ds = null;

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query_report, sb.ToString()), sqlparams); ;
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(query_report, sb.ToString()), sqlparams);
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


        public static DataTable GetDeductionHoursForLate(Nullable<int> companyID, Nullable<int> periodID, Nullable<decimal> lateHours, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            const string query_dedutionhrs = @"  Select * from HRMS_DeductionHours where @LateHours >=FromHours and @LateHours <= ToHours 
";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@LateHours", lateHours);


                


                DataSet ds = null;

                if (sqltrans == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query_dedutionhrs, sb.ToString()), sqlparams); ;
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, string.Format(query_dedutionhrs, sb.ToString()), sqlparams);
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


        public static DataTable GetAtendanceForProcess(Nullable<int> companyID, Nullable<int> periodID, int Month, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Month", Month);

                DataSet ds = null;

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_getRoasterForProcess, sb.ToString()), sqlparams); ;
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_getRoasterForProcess, sb.ToString()), sqlparams);
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

        public static DataTable GetTimeInAtendanceForProcess(Nullable<int> companyID, Nullable<int> periodID, int Month, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            const string query_getdata = @" Select distinct FullName,EmployeeCode,HRMS_TMSDATA.PersonID,DepartmentName,Reason,TMSDATETIME as TimeIN,null as TimeOUT
from HRMS_TMSDATA
	
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_TMSDATA.PersonID
inner join Set_Department on Set_Department.DepartmentID =HRMS_Set_Employee.FK_DepartmentID
where
 StatusCode = 1
 
 --and  HRMS_TMSDATA.PersonID = 2
 and Datepart(Month,TMSDATETIME) = @Month
 order by personID ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Month", Month);

                DataSet ds = null;

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query_getdata, sb.ToString()), sqlparams); ;
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(query_getdata, sb.ToString()), sqlparams);
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

        public static DataTable Get_Emp_From_TMS(Nullable<int> companyID, Nullable<int> periodID, string Month,string Year,int PersonID, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            const string query_getdata = @" select tab.PersonID , tab.StatusCode , tab.TMSDateTime , tab.ShiftTimeOut,tab.TMSDate , 
--DATEDIFF(MINUTE, cast(tab.ShiftTimeOut as time) , cast(tab.TMSDateTime as time))
Abs(DATEDIFF(MINUTE, cast(tab.TMSDateTime as time) , cast(tab.TMSDateTimeIn as time))) - Abs(DATEDIFF(MINUTE, cast(tab.ShiftTimeOut as time) , cast(tab.ShiftTimeIn as time))) 
 as TimeDifferenceInMin , ShiftID from (
 Select distinct HRMS_TMSDATA.PersonID,StatusCode,convert(time,(HRMS_Set_Shift.TimeOut)) AS ShiftTimeOut,convert(time,(HRMS_Set_Shift.TimeIn)) AS ShiftTimeIn,
max(HRMS_TMSDATA.TMSDateTime) as TMSDateTime ,
isnull((select min(HRMS_TMSDatainner.TMSDateTime) from HRMS_TMSData  HRMS_TMSDatainner 
where HRMS_TMSDatainner.PersonID = HRMS_TMSData.PersonID
and convert(date, HRMS_TMSDatainner.TMSDateTime) = convert(date, HRMS_TMSData.TMSDateTime)
and HRMS_TMSDatainner.StatusCode = 0),0) as TMSDateTimeIn,
 convert(date,(HRMS_TMSDATA.TMSDateTime)) as TMSDate , HRMS_Set_Shift.ShiftID

from SET_DateTime

left join HRMS_TMSData on convert(date, HRMS_TMSData.TMSDateTime) = CONVERT(date,SET_DateTime.thedate)

--inner join HRMS_Set_Shift ON HRMS_Set_Shift.ShiftID = HRMS_TMSDATA.ShiftHeaderID
--AND HRMS_Set_Shift.Status = 1

inner join HRMS_AttendanceRoaster on HRMS_AttendanceRoaster.PersonID = HRMS_TMSData.PersonID
and convert(date,HRMS_AttendanceRoaster.ShiftFrom) = CONVERT(date,HRMS_TMSData.TMSDateTime)
and HRMS_AttendanceRoaster.Status = 1

inner join HRMS_Set_Shift ON HRMS_Set_Shift.ShiftID = HRMS_AttendanceRoaster.ShiftID
AND HRMS_Set_Shift.Status = 1

where
Datepart(Month,TMSDATETIME) = @Month
and Datepart(YEAR,TMSDATETIME) = @Year
and HRMS_TMSData.CompanyID = @CompanyID
and StatusCode = 1
and HRMS_TMSDATA.PersonID = @PersonID
group by HRMS_TMSDATA.PersonID,StatusCode,(HRMS_Set_Shift.TimeOut), convert(date,(HRMS_TMSDATA.TMSDateTime)) , HRMS_Set_Shift.ShiftID,HRMS_Set_Shift.TimeIn
) as tab
--where DATEDIFF(MINUTE, cast(tab.ShiftTimeOut as time) , cast(tab.TMSDateTime as time)) >= 60
where Abs(DATEDIFF(MINUTE, cast(tab.TMSDateTime as time) , cast(tab.TMSDateTimeIn as time))) - Abs(DATEDIFF(MINUTE, cast(tab.ShiftTimeOut as time) , cast(tab.ShiftTimeIn as time)))  >= 60
order by tab.PersonID ,tab.TMSDate
";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@Month", Month);
                sqlparams[2] = new SqlParameter("@Year", Year);
                sqlparams[3] = new SqlParameter("@PersonID", PersonID);

                DataSet ds = null;

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query_getdata, sb.ToString()), sqlparams); ;
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(query_getdata, sb.ToString()), sqlparams);
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


        public static DataTable GetTimeOutForProcess(Nullable<int> companyID, int PersonID, DateTime TimeIN, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            const string query_getdata = @"Select distinct FullName,EmployeeCode,HRMS_TMSDATA.PersonID,DepartmentName,Reason,Convert(char(25),TMSDATETIME,120) as TimeOut
from HRMS_TMSDATA
	
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_TMSDATA.PersonID
inner join Set_Department on Set_Department.DepartmentID =HRMS_Set_Employee.FK_DepartmentID
where
 StatusCode = 2
and HRMS_TMSDATA.PersonID = @PersonID
and Convert(Date,TMSDateTime) = Convert(date,@TimeIN)
 order by personID ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);
                sqlparams[2] = new SqlParameter("@TimeIN", TimeIN);

                DataSet ds = null;

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query_getdata, sb.ToString()), sqlparams); ;
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(query_getdata, sb.ToString()), sqlparams);
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

        public bool DeleteRoaster(int CompanyID, int PeriodID, DateTime ShiftFrom, DateTime ShiftTo, SqlTransaction objTransaction = null)
        {
            bool result = false;

            const string Query_DeleteRoaster = @"  delete from HRMS_AttendanceRoaster where Convert(date,ShiftFrom) between @ShiftFrom and @ShiftTo ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@ShiftFrom", ShiftFrom);
                sqlparams[3] = new SqlParameter("@ShiftTo", ShiftTo);
                

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteRoaster, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DeleteRoaster, sqlparams) > 0)
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

        public static DataTable Get_Emp_From_Duplicate(Nullable<int> companyID, Nullable<int> periodID, DateTime tmsdatetime , int PersonID, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            const string query = @" select * from HRMS_Emp_OT 
where
convert(date,TMSDateTime) = @TMSDateTime
and HRMS_Emp_OT.CompanyID = @CompanyID
and HRMS_Emp_OT.PeriodID = @PeriodID
and HRMS_Emp_OT.PersonID = @PersonID
and HRMS_Emp_OT.Status = 1

";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@TMSDateTime", tmsdatetime);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@PersonID", PersonID);

                DataSet ds = null;

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, sb.ToString()), sqlparams); ;
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(query, sb.ToString()), sqlparams);
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

        public static DataTable Get_Emp_Users_Duplicate(Nullable<int> companyID, Nullable<int> periodID, Guid UserID, int PersonID, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            const string query = @" 
select * from [AssignEmpToUsers] 
where
AssignEmpToUsers.UserID = @UserID
and AssignEmpToUsers.CompanyID = @CompanyID
and AssignEmpToUsers.PeriodID = @PeriodID
and AssignEmpToUsers.PersonID = @PersonID
and AssignEmpToUsers.Status = 1

";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@UserID", UserID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@PersonID", PersonID);

                DataSet ds = null;

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, sb.ToString()), sqlparams); ;
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(query, sb.ToString()), sqlparams);
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

    }
}
