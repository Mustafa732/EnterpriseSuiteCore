using DAL.DataAccessLayer;
using System;
using System.Data;
using Microsoft.Data.SqlClient;
namespace FnI.BusinessAccessLayer.Setup
{
    public partial class Hrms_CheckInOut
    {

        const string Query_Delete_DuplicateRecords = @"
        --DECLARE @Year int = 2021
        --DECLARE @Month int = 06
        --DECLARE @Day int = 05

         DELETE FROM Hrms_CheckInOut WHERE ID IN (  
          SELECT ID FROM Hrms_CheckInOut  
        WHERE DAY(CHECKTIME)=@Day  
        AND MONTH(CHECKTIME)=@Month  
        AND YEAR(CHECKTIME)=@Year  
        AND ID NOT IN (  
        select MIN(ID)--,AttendanceDateTime  
        from Hrms_CheckInOut  
        WHERE DAY(CHECKTIME)=@Day  
        AND MONTH(CHECKTIME)=@Month  
        AND YEAR(CHECKTIME)=@Year  
        --AND userId=1  
        Group By USERID, CHECKTIME,CHECKTYPE,VERIFYCODE,SENSORID,Memoinfo,WorkCode,sn,UserExtFmt
        )  
         )  
         --DECLARE @RefrenceId INT  
         --SET @RefrenceId=@Day  
         --RETURN @RefrenceId 

        ";

        const string Query_Delete_DuplicateRecordsWithoutSpecificDate = @"

        DELETE FROM Hrms_CheckInOut WHERE ID IN (  
          SELECT ID FROM Hrms_CheckInOut  
        WHERE
         ID NOT IN (  
        select MIN(ID)
        from Hrms_CheckInOut    
        Group By USERID, CHECKTIME,CHECKTYPE,VERIFYCODE,SENSORID,Memoinfo,WorkCode,sn,UserExtFmt
        )  
       )  

        ";


        public static bool DeleteDuplicateRecord(int Day, int Month, int Year, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@Year", Year);
                sqlparams[1] = new SqlParameter("@Month", Month);
                sqlparams[2] = new SqlParameter("@Day", Day);

                if (objTransaction != null)
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_DuplicateRecords, sqlparams) > 0)
                        result = true;
                }
                else if (objConnection != null)
                {
                    if (SqlHelper.ExecuteNonQuery(objConnection, CommandType.Text, Query_Delete_DuplicateRecords, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_DuplicateRecords, sqlparams) > 0)
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

        public static bool DeleteDuplicateRecordWithoutSpecificDate(SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            bool result = false;

            try
            {
                if (objTransaction != null)
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_DuplicateRecordsWithoutSpecificDate) > 0)
                        result = true;
                }
                else if (objConnection != null)
                {
                    if (SqlHelper.ExecuteNonQuery(objConnection, CommandType.Text, Query_Delete_DuplicateRecordsWithoutSpecificDate) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_DuplicateRecordsWithoutSpecificDate) > 0)
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
