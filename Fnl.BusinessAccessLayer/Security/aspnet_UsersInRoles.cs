using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;

namespace FnI.BusinessAccessLayer.Security
{
    public class aspnet_UsersInRoles
    {
        #region Fields

        private Guid f_UserID;
        private Guid f_RoleID;

        #endregion Fields

        #region Properties

        public Guid UserID
        {
            get { return this.f_UserID; }
            set { this.f_UserID = value; }
        }
        
        public Guid RoleId
        {
            get { return this.f_RoleID; }
            set { this.f_RoleID = value; }
        }
        
        #endregion Properties

        #region ConstantStrings

        const string Query_SelectAll_ByUserId = @" SELECT aspnet_UsersInRoles.UserID, aspnet_UsersInRoles.RoleID, aspnet_Roles.RoleName 
FROM aspnet_UsersInRoles
	INNER JOIN aspnet_Roles ON aspnet_Roles.RoleId = aspnet_UsersInRoles.RoleId 
WHERE aspnet_UsersInRoles.UserID = @UserID ";

        #endregion ConstantStrings

        #region Methods

        public static DataTable GetAll_Roles(Guid userID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserID", userID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_ByUserId, sqlparams);

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

        public static bool IsCurrentUserBelongToCISAdmin(Guid userID)
        {
            DataTable dtUserRoles = GetAll_Roles(userID);

            if (dtUserRoles != null && dtUserRoles.Rows.Count > 0)
            {
                DataRow[] drRoles = dtUserRoles.Select("RoleName = 'cisadmin'");

                if (drRoles.Length > 0)
                    return true;
                else
                    return false;
            }

            return false;
        }

        #endregion Methods
    }
}
