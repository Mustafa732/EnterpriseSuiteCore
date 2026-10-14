using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_Notification
    {
        #region Constant Strings

        const string Query_Testing_Notification = @"select NotificationID, Description As Notification123 from Set_Notification order By NotificationID desc";

        const string Query_SelectAllByID = @" SELECT Description FROM [SET_Notification] where NotificationID = '{0}' ";

        const string Query_SelectTopNotification = @" SELECT Top 1 Description FROM [SET_Notification] where CompanyID = @CompanyID and PeriodID = @PeriodID ";

        const string Query_DeleteTable = @" Delete from SET_Notification where CompanyID = @CompanyID and PeriodID = @PeriodID ";

        const string query_DeleteNotification = @" Delete from SET_Notification where CompanyID = @CompanyID and PeriodID = @PeriodID AND NotificationID = @NotificationID";
        #endregion


        #region Class Methods

        public static DataTable GetAllNotificationByID(string NotificationID)
        {
            DataTable dt = null;

            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectAllByID, NotificationID)).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable Get_Notification_test(Nullable<int> companyID, Nullable<int> periodID, Nullable<bool> isActive = true)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (isActive.HasValue)
                    sqlparams[2] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[2] = new SqlParameter("@IsActive", DBNull.Value);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Testing_Notification, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static bool DeleteAll(int companyId, int periodId)
        {
            bool result = false;
           // DataTable dt = null;


            try
            {

                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyId);
                sqlparams[1] = new SqlParameter("@PeriodID", periodId);
                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteTable, sqlparams) > 0)
                    result = true;
                
             //   dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_DeleteTable).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return result;

        }


        public static DataTable GetTopNotification(int companyID, int periodID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectTopNotification, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static bool DeleteNotification(int companyId, int periodId,int notificationID)
        {
            bool result = false;
            // DataTable dt = null;


            try
            {

                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyId);
                sqlparams[1] = new SqlParameter("@PeriodID", periodId);
                sqlparams[2] = new SqlParameter("@NotificationID", notificationID);
                if (SqlHelper.ExecuteNonQuery(CommandType.Text, query_DeleteNotification, sqlparams) > 0)
                    result = true;

                //   dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_DeleteTable).Tables[0];
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
