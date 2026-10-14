using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class UsersDetail
    {
        #region Constant Strings

        const string Query_GetDetailsWithUserID = @"
        SELECT UsersDetail.UserID, UsersDetail.DesignationID, UsersDetail.FullName, Set_Designation.DesignationName FROM UsersDetail
        JOIN  Set_Designation ON Set_Designation.DesignationID = UsersDetail.DesignationID
        WHERE UsersDetail.UserId = @UserID";

        const string Query_SelectAll_ApprovalUser = @"SELECT 
		aspnet_Users.[UserId],
		[LastActivityDate],
		[IsAnonymous],
		[UserName],
		[LoweredUserName],
		[MobileAlias],
		[ApplicationId] 
		FROM aspnet_Users 
		INNER jOIN SET_ApprovalAuthority on SET_ApprovalAuthority.UserID= aspnet_Users.UserId
		And SET_ApprovalAuthority.Status=1";
        #endregion

        #region Class Methods


        public static DataRow GetDetailsWithUserID(Guid userID, SqlConnection objConnection = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserID", userID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDetailsWithUserID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDetailsWithUserID, sqlparams);

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

        public static DataRow GetMembershipUserByUserID(Guid userID, SqlConnection objConnection = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserID", userID);

                DataSet ds = null;

                string query =@" select username from aspnet_users where userid = @UserID ";

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, query, sqlparams);

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

        public static DataTable GetApprovalUser()
        {
            DataTable dt = null;
            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_ApprovalUser).Tables[0];
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
