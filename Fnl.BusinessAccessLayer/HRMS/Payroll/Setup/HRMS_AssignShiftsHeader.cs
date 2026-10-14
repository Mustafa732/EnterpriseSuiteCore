using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Linq;
using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Setup
{
	
	public partial class HRMS_AssignShiftsHeader
    {
        #region strings

        const string Query_SelectAll_WithPagingForSearch = @" 
Select HeaderID,HRMS_AssignShiftsHeader.PersonID,FullName,HRMS_AssignShiftsHeader.Status
,ROW_NUMBER() Over (Order by HeaderID) as 'Row',
(select convert(date,min(FromDate)) from HRMS_AssignShiftsDetail
where HRMS_AssignShiftsDetail.HeaderID = [HRMS_AssignShiftsHeader].HeaderID
and HRMS_AssignShiftsDetail.Status = 1) as FromDate,
(select convert(date,max(FromDate)) from HRMS_AssignShiftsDetail
where HRMS_AssignShiftsDetail.HeaderID = [HRMS_AssignShiftsHeader].HeaderID
and HRMS_AssignShiftsDetail.Status = 1) as ToDate
 from HRMS_AssignShiftsHeader
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_AssignShiftsHeader.PersonID
";


        const string Query_SelectAll_WithoutPagingForSearch = @"  Select HeaderID,HRMS_AssignShiftsHeader.PersonID,FullName,HRMS_AssignShiftsHeader.Status

 from HRMS_AssignShiftsHeader
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_AssignShiftsHeader.PersonID
";

        const string Query_SelectAll_WithPagingForSearchForEditing = @" 

 Select distinct 
ROW_NUMBER() Over (Order by HRMS_AssignShiftsHeader.Status) as 'Row',HRMS_AssignShiftsHeader.SerialNo,Convert(varchar(15),HRMS_AssignShiftsDetail.FromDate,103)FromDate
,Convert(varchar(15),HRMS_AssignShiftsDetail.ToDate,103)ToDate
 from HRMS_AssignShiftsHeader
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_AssignShiftsHeader.PersonID
inner join HRMS_AssignShiftsDetail on HRMS_AssignShiftsDetail.HeaderID = HRMS_AssignShiftsHeader.HeaderID



";

        #endregion


        public bool InsertShiftHeaderAndDetail(List<HRMS_AssignShiftsHeader> colSOHeader, List<HRMS_AssignShiftsDetail> colSODetail,List<int>ShiftHeaderID = null)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertShift");

            try
            {
                if (ShiftHeaderID != null && ShiftHeaderID.Count > 0)
                {
                    foreach (int id in ShiftHeaderID)
                    {

                        HRMS_AssignShiftsDetail.DeleteDataByKeyDetailTable(sqltrans, colSOHeader[0].CreatedBy.Value, DateTime.Now, colSOHeader[0].CreatedByValue, id);
                        HRMS_AssignShiftsHeader.DeleteDataByKey(sqltrans, colSOHeader[0].CreatedBy.Value, DateTime.Now, colSOHeader[0].CreatedByValue, id);
                    }
                    //HRMS_AssignShiftsHeader.DeleteDataByKey(sqltrans
                }

                int SerialNo = HRMS_AssignShiftsHeader.GetNewSerialNo(sqltrans);

                foreach (HRMS_AssignShiftsHeader hdr in colSOHeader)
                {
                    //hdr.HeaderID = HRMS_AssignShiftsHeader.GetNewHeaderID(sqltrans);
                    hdr.SerialNo = SerialNo;
                    hdr.Insert(sqltrans);

                    int DetailsID = HRMS_AssignShiftsDetail.GetNewDetailID(hdr.HeaderID, sqltrans);
                    foreach (HRMS_AssignShiftsDetail sod in colSODetail)
                    {
                        if (hdr.PersonID == sod.dummyPersonID)
                        {
                            sod.HeaderID = hdr.HeaderID;
                            sod.DetailID = DetailsID++;
                            sod.Insert(sqltrans);
                        }
                    }
                   
                }

              

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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertShift");
            }

            return result;
        }


        public static DataTable GetAssignedShifts(DateTime timefrom, DateTime timeto,int PersonID,string DayName)
        {
            DataTable dt = null;
            const string query_getshifts = @" 
	Select PersonID,HRMS_AssignShiftsDetail.ShiftID,DayID,WeekDayName,ShiftCode,HRMS_Set_Shift.Description,
TimeIn,TimeOut,abs(DateDiff(hour,CONVERT(VARCHAR(8),@timefrom,108),CONVERT(VARCHAR(8),TimeIN,108))) as Difference
 from HRMS_AssignShiftsHeader
inner join HRMS_AssignShiftsDetail on HRMS_AssignShiftsDetail.HeaderID = HRMS_AssignShiftsHeader.HeaderID
inner join HRMS_Set_Shift on HRMS_Set_Shift.ShiftID = HRMS_AssignShiftsDetail.ShiftID
where PersonID = @PersonID and WeekDayName = @DayName 

order by abs(DateDiff(hour,CONVERT(VARCHAR(8),@timefrom,108),CONVERT(VARCHAR(8),TimeIN,108))) asc
--and CONVERT(VARCHAR(8),TimeIN,108) <= CONVERT(VARCHAR(8),@timefrom,108)
--and  CONVERT(VARCHAR(8),TimeOut,108) <= CONVERT(VARCHAR(8),@timeto,108)";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@timefrom", timefrom);
                sqlparams[1] = new SqlParameter("@timeto", timeto);
                sqlparams[2] = new SqlParameter("@PersonID", PersonID);
                sqlparams[3] = new SqlParameter("@DayName", DayName);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, query_getshifts,sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAssignedShiftsNew(DateTime timefrom, DateTime timeto, int PersonID, string DayName)
        {
            DataTable dt = null;
            const string query = @" 
	 
	Select HRMS_AssignShiftsDetail.ShiftID
 from HRMS_AssignShiftsHeader
inner join HRMS_AssignShiftsDetail on HRMS_AssignShiftsDetail.HeaderID = HRMS_AssignShiftsHeader.HeaderID
inner join HRMS_Set_Shift on HRMS_Set_Shift.ShiftID = HRMS_AssignShiftsDetail.ShiftID
where PersonID = @PersonID and WeekDayName = @DayName 
and @timefrom between convert(date,HRMS_AssignShiftsDetail.FromDate)  and convert(date,HRMS_AssignShiftsDetail.ToDate) 
";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@timefrom", timefrom);
                sqlparams[1] = new SqlParameter("@timeto", timeto);
                sqlparams[2] = new SqlParameter("@PersonID", PersonID);
                sqlparams[3] = new SqlParameter("@DayName", DayName);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        public static DataTable GetAllDataForSearch(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_SelectAll_WithPagingForSearch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPagingForSearch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_WithPagingForSearch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPagingForSearch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAll_WithoutPagingForSearch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_WithoutPagingForSearch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetShiftsDetailData(int HeaderID,int CompanyID)
        {
            DataTable dt = null;
            const string query_getshifts = @" 
	Select HeaderID,DetailID,HRMS_AssignShiftsDetail.ShiftID,WeekDayName as DayDesc,DayID,
HRMS_AssignShiftsDetail.FromDate,HRMS_AssignShiftsDetail.ToDate,Description,'' as Action ,HRMS_AssignShiftsDetail.dummyPersonID as PersonID
from HRMS_AssignShiftsDetail
inner join HRMS_Set_Shift on HRMS_Set_Shift.ShiftID = HRMS_AssignShiftsDetail.ShiftID
where HRMS_AssignShiftsDetail.Status = 1
and HeaderID = @HeaderID";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@HeaderID", HeaderID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, query_getshifts, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAssignedShiftsFromPersonID(DateTime datefrom, DateTime dateto, int PersonID, string DayName,int HeaderID)
        {
            DataTable dt = null;
            const string query_getshifts = @" 
	
Select PersonID,HRMS_AssignShiftsDetail.ShiftID,DayID,WeekDayName,HRMS_AssignShiftsHeader.HeaderID

 from HRMS_AssignShiftsHeader
inner join HRMS_AssignShiftsDetail on HRMS_AssignShiftsDetail.HeaderID = HRMS_AssignShiftsHeader.HeaderID
--where FromDate >= @FromDate and ToDate <= @ToDate
where @FromDate between FromDate and ToDate 
and PersonID = @PersonID
and WeekDayName = @DayName 
and HRMS_AssignShiftsHeader.HeaderID = @HeaderID 

order by HRMS_AssignShiftsDetail.CreateDate desc";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@FromDate", datefrom);
                sqlparams[1] = new SqlParameter("@ToDate", dateto);
                sqlparams[2] = new SqlParameter("@PersonID", PersonID);
                sqlparams[3] = new SqlParameter("@HeaderID", HeaderID);
                sqlparams[4] = new SqlParameter("@DayName", DayName);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, query_getshifts, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetdataFromRoaster(DateTime datefrom, DateTime dateto, int PersonID, string DayName, int ShiftID)
        {
            DataTable dt = null;
            const string query_getshifts = @" 
	
 Select * from HRMS_AttendanceRoaster 
 where PersonID = @PersonID 
 and ShiftID = @ShiftID 
 and datename(dw,ShiftFrom) = @DayName
 and Convert(date,ShiftFrom) between @FromDate and @ToDate ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@FromDate", datefrom);
                sqlparams[1] = new SqlParameter("@ToDate", dateto);
                sqlparams[2] = new SqlParameter("@PersonID", PersonID);
                sqlparams[3] = new SqlParameter("@ShiftID", ShiftID);
                sqlparams[4] = new SqlParameter("@DayName", DayName);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, query_getshifts, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetShiftsDetailDataByPersonID(int PersonID, int CompanyID)
        {
            DataTable dt = null;
            const string query_getshifts = @" 
	Select HRMS_AssignShiftsHeader.HeaderID,DetailID,HRMS_AssignShiftsDetail.ShiftID,WeekDayName as DayDesc,DayID,
HRMS_AssignShiftsDetail.FromDate,HRMS_AssignShiftsDetail.ToDate,Description,'Add' as Action 
from HRMS_AssignShiftsDetail
inner join HRMS_AssignShiftsHeader on HRMS_AssignShiftsHeader.HeaderID = HRMS_AssignShiftsDetail.HeaderID
inner join HRMS_Set_Shift on HRMS_Set_Shift.ShiftID = HRMS_AssignShiftsDetail.ShiftID

where HRMS_AssignShiftsDetail.Status = 1
and HRMS_AssignShiftsHeader.Status = 1
and HRMS_AssignShiftsHeader.CompanyID = @CompanyID
and PersonID = @PersonID";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@PersonID", PersonID);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, query_getshifts, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetPrvShiftsFromDate(int Month, int year)
        {
            DataTable dt = null;
            const string query_getshifts = @" 
	
Select PersonID,-1 as HeaderID,-1 as DetailID,ShiftID,DayID,WeekDayName,FromDate,ToDate from HRMS_AssignShiftsHeader
inner join HRMS_AssignShiftsDetail on HRMS_AssignShiftsDetail.HeaderID = HRMS_AssignShiftsHeader.HeaderID
and HRMS_AssignShiftsHeader.CompanyID = HRMS_AssignShiftsHeader.CompanyID
and HRMS_AssignShiftsHeader.PeriodID = HRMS_AssignShiftsHeader.PeriodID


where HRMS_AssignShiftsHeader.Status = 1
and HRMS_AssignShiftsDetail.Status = 1
and DatePart(month,HRMS_AssignShiftsDetail.FromDate) = @Month and DatePart(year,HRMS_AssignShiftsDetail.FromDate) = @year
and DatePart(month,HRMS_AssignShiftsDetail.ToDate) = @Month and DatePart(year,HRMS_AssignShiftsDetail.ToDate) = @year";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@Month", Month);
                sqlparams[1] = new SqlParameter("@year", year);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, query_getshifts, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static int GetNewSerialNo(SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;

            const string Query_Get_MaxSerailNO = @" SELECT IsNull(max(SerialNo), 0) + 1 as SerialNo FROM HRMS_AssignShiftsHeader";

            try
            {
                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_MaxSerailNO));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_Get_MaxSerailNO));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_MaxSerailNO));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static DataTable GetAllDataForSearchForEditing(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_SelectAll_WithPagingForSearchForEditing + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPagingForSearchForEditing + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_WithPagingForSearchForEditing + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPagingForSearchForEditing + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAll_WithoutPagingForSearch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_WithoutPagingForSearch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable CheckAtendanceDataBySerialNo(int SerialNo)
        {
            DataTable dt = null;
            const string query_getshifts = @" 
	
Select  distinct ShiftID,DayID,WeekDayName,HRMS_AssignShiftsDetail.FromDate,HRMS_AssignShiftsDetail.ToDate
 from HRMS_AssignShiftsHeader 
inner join HRMS_AssignShiftsDetail on HRMS_AssignShiftsDetail.HeaderID = HRMS_AssignShiftsHeader.HeaderID
where SerialNo = @SerialNo
and HRMS_AssignShiftsHeader.Status = 1

and PersonID in
(Select PersonID from HRMS_TMSData
where 
 CONVERT(Date,TMSDateTime) between  Convert(Date,HRMS_AssignShiftsDetail.FromDate) and Convert(Date,HRMS_AssignShiftsDetail.ToDate))

order by DayID,ShiftID";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@SerialNo", SerialNo);


                dt = SqlHelper.ExecuteDataset(CommandType.Text, query_getshifts, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetDataBySerialNo(int SerialNo)
        {
            DataTable dt = null;
            const string query_getshifts = @" 
	
Select  distinct ShiftID,DayID,WeekDayName,HRMS_AssignShiftsDetail.FromDate,HRMS_AssignShiftsDetail.ToDate
 from HRMS_AssignShiftsHeader 
inner join HRMS_AssignShiftsDetail on HRMS_AssignShiftsDetail.HeaderID = HRMS_AssignShiftsHeader.HeaderID
where SerialNo = @SerialNo
and HRMS_AssignShiftsHeader.Status = 1

order by DayID,ShiftID";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@SerialNo", SerialNo);
               

                dt = SqlHelper.ExecuteDataset(CommandType.Text, query_getshifts, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetEmpBySerialNo(int SerialNo)
        {
            DataTable dt = null;
            const string query_getshifts = @" 
	
Select PersonID from HRMS_AssignShiftsHeader
where SerialNo = @SerialNo
and HRMS_AssignShiftsHeader.Status = 1 ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@SerialNo", SerialNo);


                dt = SqlHelper.ExecuteDataset(CommandType.Text, query_getshifts, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllActiveDatAfortShiftHeader(int CompanyID, Nullable<int> Statuscode = null, Nullable<int> departmentId = null, Nullable<Guid> UserID = null, Nullable<int> PersonID = null,int Month = 0, int Year = 0)
        {
            DataTable dt = null;

            StringBuilder sb = new StringBuilder();
            const string query_getshifts = @" 
	
       SELECT distinct [HRMS_AssignShiftsHeader].HeaderID,[HRMS_AssignShiftsHeader] .PersonID  FROM [HRMS_AssignShiftsHeader] 
inner join HRMS_AssignShiftsDetail on HRMS_AssignShiftsDetail.HeaderID = HRMS_AssignShiftsHeader.HeaderID
	     Inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID =  HRMS_AssignShiftsHeader.PersonID
            And HRMS_Set_Employee.CompanyID =  HRMS_AssignShiftsHeader.CompanyID
		Inner Join HRMS_Employement_Status on HRMS_Employement_Status.StatusID = HRMS_Set_Employee.StatusID
           --And HRMS_Employement_Status.CompanyID = HRMS_Set_Employee.CompanyID
         WHERE [HRMS_AssignShiftsHeader].[Status] = 1
		      and [HRMS_AssignShiftsHeader].CompanyID = @CompanyID
               {0}
      ";
           
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);

                DataSet ds = null;

                if (departmentId != null && departmentId > 0 && departmentId != -1)
                {
                    sb.Append(" and HRMS_Set_Employee.FK_DepartmentID = " + departmentId);
                }

                if (Statuscode != null && Statuscode != -1)
                {
                    sb.Append(" and HRMS_Employement_Status.StatusID = " + Statuscode);
                }
                else
                {
                    sb.Append(" and HRMS_Employement_Status.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }

                if(PersonID.HasValue)
                    sb.Append(" and HRMS_Set_Employee.PersonID = " + PersonID.Value);


                sb.Append("and " + Month + " between month(FromDate) and month(ToDate) ");
                sb.Append("and " + Year + " between year(FromDate) and year(ToDate) ");



               // dt = SqlHelper.ExecuteDataset(CommandType.Text, query_getshifts, sqlparams).Tables[0];

                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query_getshifts, sb), sqlparams);

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

        public bool UpdateShiftHeaderAndDetail(List<HRMS_AssignShiftsHeader> colSOHeader, List<HRMS_AssignShiftsDetail> colSODetail,int SerialNo)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertShift");

            try
            {
                //int SerialNo = HRMS_AssignShiftsHeader.GetNewSerialNo(sqltrans);

                bool DeleteData  = DeletePrvData(SerialNo, sqltrans);

                foreach (HRMS_AssignShiftsHeader hdr in colSOHeader)
                {
                    hdr.HeaderID = HRMS_AssignShiftsHeader.GetNewHeaderID(sqltrans);
                    hdr.SerialNo = SerialNo;
                    hdr.Insert(sqltrans);

                    int DetailsID = HRMS_AssignShiftsDetail.GetNewDetailID(this.HeaderID, sqltrans);
                    foreach (HRMS_AssignShiftsDetail sod in colSODetail)
                    {
                        sod.HeaderID = hdr.HeaderID;
                        sod.DetailID = DetailsID++;
                        sod.Insert(sqltrans);
                    }
                }

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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertShift");
            }

            return result;
        }


        public bool DeletePrvData(int SerialNo,SqlTransaction objTransaction = null)
        {
            bool result = false;

            const string Query_Delete = @" Delete from HRMS_AssignShiftsDetail
where HeaderID in
(Select HeaderID from HRMS_AssignShiftsHeader where SerialNo = @SerialNo)

Delete from HRMS_AssignShiftsHeader where SerialNo = @SerialNo ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@SerialNo", SerialNo);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete, sqlparams) > 0)
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
      
	}


}
