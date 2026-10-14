using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using System.Text;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Setup
{

    public partial class HRMS_AssignShiftsDetail
    {
        #region strings

        const string Query_SelectAll_WithPagingForSearch = @" 
";

        #endregion

        #region Method


        public static DataTable Get_DataForRoasterReportMonthWise(int companyID, int PersonID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To,Nullable<int> EmployeeStatus = null, Nullable<Guid> UserID = null)
        {
            StringBuilder sb = new StringBuilder();
            DataTable dt = null;
            DataSet dsToReturn = null;
            try
            {
                #region Query
                const string Query_ForRoasterReportMonthWise = @"

                ";
                #endregion

                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                //sqlparams[1] = new SqlParameter("@month", Date_From.Value);
                //sqlparams[2] = new SqlParameter("@year", Date_From.Value);
                sqlparams[1] = new SqlParameter("@FromDate", Date_From);
                sqlparams[2] = new SqlParameter("@ToDate", Date_To);

                if (PersonID > 0)
                {
                    sb.Append(" AND HRMS_Set_Employee.PersonID = " + PersonID);
                }
                if (EmployeeStatus != null && EmployeeStatus > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + EmployeeStatus);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                }

               // dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_ForRoasterReportMonthWise, sb), sqlparams).Tables[0];
                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_ForRoasterReportMonthWiseAlnoor, sb), sqlparams).Tables[0];
                
               // dsToReturn = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_ForRoasterReportMonthWise, sb), sqlparams);

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable Get_DataForEmail(int companyID, int periodID)
        {
            StringBuilder sb = new StringBuilder();
            DataTable dt = null;
            DataSet dsToReturn = null;
            try
            {
                #region Query
                const string Query = @"
select  distinct 
		WeekDayName,HRMS_Set_Employee.Email,
		HRMS_AssignShiftsHeader.PersonID,HRMS_AssignShiftsDetail.ShiftID,ShiftCode,
		cast(TimeIn as time) as  OrigianlTimeIn,
		cast(TimeOut as time) as  OrigianlTimeOut 
		,DATEADD(MINUTE, 30, cast(TimeIn as time)) as TimeIn

        from HRMS_AssignShiftsDetail

       Inner Join HRMS_AssignShiftsHeader on HRMS_AssignShiftsHeader.HeaderID = HRMS_AssignShiftsDetail.HeaderID
	   and HRMS_AssignShiftsHeader.Status = 1

       Inner join HRMS_Set_Shift on HRMS_Set_Shift.ShiftID = HRMS_AssignShiftsDetail.ShiftID
	   and HRMS_Set_Shift.Status = 1

	   inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_AssignShiftsHeader.PersonID
	   and HRMS_Set_Employee.CompanyID = HRMS_AssignShiftsHeader.CompanyID
	   and HRMS_Set_Employee.Status = 1

	   where HRMS_AssignShiftsHeader.CompanyID = @CompanyID and HRMS_AssignShiftsHeader.PeriodID = @PeriodID
	   and HRMS_AssignShiftsDetail.Status = 1
	   order by HRMS_AssignShiftsHeader.personid
 
                ";
                #endregion

                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                //sqlparams[1] = new SqlParameter("@month", Date_From.Value);
                //sqlparams[2] = new SqlParameter("@year", Date_From.Value);
                //sqlparams[1] = new SqlParameter("@FromDate", Date_From);
                //sqlparams[2] = new SqlParameter("@ToDate", Date_To);

                //if (PersonID > 0)
                //{
                //    sb.Append(" AND HRMS_Set_Employee.PersonID = " + PersonID);
                //}

                //if (EmployeeStatus != null && EmployeeStatus > 0)
                //{
                //    sb.Append(" and HRMS_Set_Employee.StatusID = " + EmployeeStatus);
                //}
                //else
                //{
                //    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                //}

                 dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query), sqlparams).Tables[0];
               // dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_ForRoasterReportMonthWiseAlnoor, sb), sqlparams).Tables[0];

                // dsToReturn = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_ForRoasterReportMonthWise, sb), sqlparams);

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static bool DeleteDataByKeyDetailTable(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, int headerID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                //sqlparams[0] = new SqlParameter("@DetailID", detailID);
                sqlparams[0] = new SqlParameter("@HeaderID", headerID);
                sqlparams[1] = new SqlParameter("@Status", false);
                sqlparams[2] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[3] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[4] = new SqlParameter("@UpdatedByValue", updatedByValue);

                #region Query
                const string Query_Delete_ByKeyDetailtable = @" Update [HRMS_AssignShiftsDetail] Set [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE  [HeaderID] = @HeaderID ";
                #endregion

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByKeyDetailtable, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByKeyDetailtable, sqlparams) > 0)
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

        public static DataTable GetAllActiveDataFromDetailTable(Nullable<int> headerID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {

            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@HeaderID", headerID);
                #region Query
                const string Query_ForGetDatafromshiftDetailTable = @"
              SELECT * FROM [HRMS_AssignShiftsDetail] WHERE [Status] = 1 AND ([HeaderID] = @HeaderID)
                ";
                #endregion
                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_ForGetDatafromshiftDetailTable, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_ForGetDatafromshiftDetailTable, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_ForGetDatafromshiftDetailTable, sqlparams);

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

        #endregion

        public static DataTable Get_DataForDuplicate(int companyID, int PersonID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> EmployeeStatus = null, Nullable<Guid> UserID = null)
        {
            StringBuilder sb = new StringBuilder();
            DataTable dt = null;
            DataSet dsToReturn = null;
            try
            {
                #region Query
                const string Query = @"
select  * from  HRMS_AssignShiftsDetail

       Inner Join HRMS_AssignShiftsHeader on HRMS_AssignShiftsHeader.HeaderID = HRMS_AssignShiftsDetail.HeaderID
	   and HRMS_AssignShiftsHeader.Status = 1

   inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_AssignShiftsHeader.PersonID
	   and HRMS_Set_Employee.CompanyID = HRMS_AssignShiftsHeader.CompanyID
	   and HRMS_Set_Employee.Status = 1

	   where HRMS_AssignShiftsHeader.CompanyID = @CompanyID 
	   {0}
	   and HRMS_AssignShiftsDetail.Fromdate = @FromDate and HRMS_AssignShiftsDetail.ToDate = @ToDate
	   and HRMS_AssignShiftsDetail.Status = 1

                ";
                #endregion

                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@FromDate", Date_From);
                sqlparams[2] = new SqlParameter("@ToDate", Date_To);

                if (PersonID > 0)
                {
                    sb.Append(" AND HRMS_AssignShiftsHeader.PersonID = " + PersonID);
                }


                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, sb), sqlparams).Tables[0];

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
