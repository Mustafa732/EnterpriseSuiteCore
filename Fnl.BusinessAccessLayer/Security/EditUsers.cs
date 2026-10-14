using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Security
{
    public class EditUsers : BaseObject
    {
        #region Fields
        private Guid f_UserID;
        private Guid f_RoleID;
        private int f_CompanyID;
        private DateTime f_CreateDate;
        private DateTime f_UpdateDate;
        private string f_UserName;
        private string f_Email;
        private string f_CreatedByValue;
        private string f_UpdatedByValue;
        private Guid f_CreatedBy;
        private Guid f_UpdatedBy;
        private bool f_PwdExpiresFirst;
        private bool f_PwdNaverExpires;
        private bool f_PwdExpiryDays;
        private int f_Days;


        #endregion Fields

        #region Properties

        public string UserName_S
        {
            get { return this.f_UserName; }
            set { this.f_UserName = value; }
        }

        public Guid UserId
        {
            get { return this.f_UserID; }
            set { this.f_UserID = value; }
        }

        public string UserName
        {
            get { return this.f_UserName; }
            set { this.f_UserName = value; }
        }

        public DateTime CreateDate
        {
            get { return this.f_CreateDate; }
            set { this.f_CreateDate = value; }
        }
        public DateTime UpdateDate
        {
            get { return this.f_UpdateDate; }
            set { this.f_UpdateDate = value; }
        }

        public string Email
        {
            get { return this.f_Email; }
            set { this.f_Email = value; }
        }

        public string CreatedByValue
        {
            get { return this.f_CreatedByValue; }
            set { this.f_CreatedByValue = value; }
        }
        public string UpdatedByValue
        {
            get { return this.f_UpdatedByValue; }
            set { this.f_UpdatedByValue = value; }
        }
        public Guid CreatedBy
        {
            get { return this.f_CreatedBy; }
            set { this.f_CreatedBy = value; }
        }
        public Guid UpdatedBy
        {
            get { return this.f_UpdatedBy; }
            set { this.f_UpdatedBy = value; }
        }

        public bool PwdExpiresFirst
        {
            get { return this.f_PwdExpiresFirst; }
            set { this.f_PwdExpiresFirst = value; }
        }

        public bool PwdNeverExpires
        {
            get { return this.f_PwdNaverExpires; }
            set { this.f_PwdNaverExpires = value; }
        }

        public bool PwdExpiryDays
        {
            get { return this.f_PwdExpiryDays; }
            set { this.f_PwdExpiryDays = value; }
        }


        public int PwdDays
        {
            get { return this.f_Days; }
            set { this.f_Days = value; }
        }

        public Guid RoleId
        {
            get { return this.f_RoleID; }
            set { this.f_RoleID = value; }
        }

        public int CompanyId
        {
            get { return this.f_CompanyID; }
            set { this.f_CompanyID = value; }
        }

        #endregion Properties

        #region ConstantStrings

        const string Query_GetUser_By_UserName = @"select * from aspnet_Users where UserName = @LoweredUserName";

        const string Query_GetAllUser = @"SELECT dbo.aspnet_Users.UserId, dbo.aspnet_Users.UserName, dbo.aspnet_Membership.Email FROM dbo.aspnet_Users INNER JOIN dbo.aspnet_Membership ON dbo.aspnet_Users.UserId = dbo.aspnet_Membership.UserId";

        const string Query_GetUserBy_UserID = @"SELECT dbo.aspnet_Users.UserId, dbo.aspnet_Users.UserName, dbo.aspnet_Membership.Email FROM dbo.aspnet_Users INNER JOIN dbo.aspnet_Membership ON dbo.aspnet_Users.UserId = dbo.aspnet_Membership.UserId Where dbo.aspnet_Users.UserId = @UserId ";

        const string Query_GetUserRolesAndCompanyBy_UserID = @"SELECT     
        UsersInRoles.UserId,
        UsersInRoles.RoleId,
        UsersInRoles.CompanyID,
        aspnet_Roles.RoleName,
        (SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName) AS CompanyName 
            FROM UsersInRoles 
        INNER JOIN aspnet_Roles ON UsersInRoles.RoleId = aspnet_Roles.RoleId 
        INNER JOIN SET_Company ON UsersInRoles.CompanyID = SET_Company.CompanyID 
        INNER JOIN SET_Business ON SET_Company.BusinessId =  SET_Business.BusinessId
        INNER JOIN SET_OperatingUnit ON SET_Company.OperatingUnitId =  SET_OperatingUnit.OperatingUnitId
        Where UsersInRoles.UserId = @UserId ";

        const string Query_GetCompany_ByUserID = @"Select * from UsersInRoles Where dbo.UsersInRoles.UserId = @UserId and dbo.UsersInRoles.CompanyId = @CompanyId";

        const string Query_GetCompanies = @"SELECT     
        UserId, RoleName, CompanyName, CompanyID, RoleId
        FROM         
        (SELECT
        UsersInRoles.UserId, aspnet_Roles.RoleName, 
        SET_CompanyTAB.CompanyName, SET_CompanyTAB.CompanyID, aspnet_Roles.RoleId
        FROM
            UsersInRoles RIGHT OUTER JOIN
            aspnet_Roles ON UsersInRoles.RoleId = aspnet_Roles.RoleId RIGHT OUTER JOIN
        (Select 
            SET_Company.CompanyId, (SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName) AS CompanyName  From SET_Company
        INNER JOIN SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId
        INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId 
        WHERE SET_Company.Status = 1) SET_CompanyTAB ON UsersInRoles.CompanyID = SET_CompanyTAB.CompanyID
        WHERE
        (UsersInRoles.UserId = @UserId)) AS Tab
        UNION ALL
        SELECT '00000000-0000-0000-0000-000000000000' AS UserId, '' AS RoleName,
        CompanyName, CompanyID, '00000000-0000-0000-0000-000000000000' AS RoleId
        FROM
        (Select 
            SET_Company.CompanyId, (SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName) AS CompanyName  From SET_Company
            INNER JOIN SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId
            INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId 
            WHERE SET_Company.Status = 1) AS Company_1
        WHERE (CompanyID NOT IN
        (SELECT
            CompanyID
        FROM UsersInRoles AS UsersInRoles_1
        WHERE (UserId = @UserId)))";

        const string Query_GetAllCompanies = @"Select 
        SET_Company.CompanyId, 
        (SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName) AS CompanyName 
            From SET_Company
        INNER JOIN SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId
        INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId 
        WHERE SET_Company.Status = 1";

        const string Query_UserOptionsBy_UserID = @"Select UserID,PwdExpiresFirstLogin,PwdNeverExpires,PwdExpiryDays,Days From UsersOption Where UserID = @UserId ";

        
        const string Query_InsertUserInRole= @"Insert Into UsersInRoles 
        ([UserId],[RoleId],[CompanyId])
        Values(@UserId,@RoleId,@CompanyId)";

        const string Query_DeleteUserBy_UserID = @"Delete from aspnet_Users Where UserID = @UserId ";

        const string Query_UpdateUser_By_PrimaryKey = @" UPDATE aspnet_Users SET [UserName] = @UserName WHERE [UserID] = @UserID";

        const string Query_UpdateEmail_By_PrimaryKey = @" UPDATE aspnet_Membership SET [Email] = @Email WHERE [UserID] = @UserID";

        const string Query_UpdatePasswordOptions_By_PrimaryKey = @" UPDATE UsersOption SET [PwdExpiresFirstLogin] = @PwdExpiresFirstLogin,[PwdNeverExpires] = @PwdNeverExpires,[PwdExpiryDays]=@PwdExpiryDays,Days=@Days  WHERE [UserID] = @UserID";

        const string Query_DeleteRole_By_PrimaryKey = @" Delete From UsersInRoles  WHERE [UserID] = @UserID";


        #endregion ConstantStrings

        #region Methods

        /// <summary>
        /// Get Company by user id 
        /// </summary>
        public static DataTable GetCompany_ByUserID(Guid UserID, int CompanyID)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@UserID", UserID);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetCompany_ByUserID, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        /// <summary>
        /// Get All Companies
        /// </summary>
        public static DataTable GetAllData()
        {
            DataTable dt = null;
            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllCompanies).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        /// <summary>
        /// Get Companies
        /// </summary>
        public static DataTable GetCompanies(Guid UserID)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserID", UserID);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetCompanies, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }


        /// <summary>
        /// Get User Roles and Company by UserID
        /// </summary>
        public static DataTable GetUserRolesAndCompanyBy_UserID(Guid UserID)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserID", UserID);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetUserRolesAndCompanyBy_UserID, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }


        /// <summary>
        /// Get User Option by UserID
        /// </summary>
        public static DataTable GetUserOptions_By_UserID(Guid UserID)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserID", UserID);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_UserOptionsBy_UserID, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        /// <summary>
        /// Get User by UserName 
        /// </summary>
        public static DataTable GetUser_By_UserName(string LoweredRoleName)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@LoweredUserName", LoweredRoleName);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetUser_By_UserName, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        /// <summary>
        /// Get All User
        /// </summary>
        public static DataTable GetAllUsers()
        {
            DataTable dt = null;
            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllUser).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        /// <summary>
        /// Get User by UserID
        /// </summary>
        public static DataTable GetUser_By_UserID(Guid UserID)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserID", UserID);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetUserBy_UserID, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public bool InsertUserInRole()
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@UserID", UserId);
                sqlparams[1] = new SqlParameter("@RoleId", RoleId);
                sqlparams[2] = new SqlParameter("@CompanyID", CompanyId);
                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_InsertUserInRole, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }
            return result;
        }

        public bool DeleteUserRoles()
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserID", UserId);
                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteRole_By_PrimaryKey, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }
            return result;
        }

        public bool UpdateUserDataByKey()
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@UserID", UserId);
                sqlparams[1] = new SqlParameter("@UserName", UserName);
                //sqlparams[2] = new SqlParameter("@UpdatedByValue", UpdatedByValue);
                //sqlparams[3] = new SqlParameter("@UpdatedBy", UpdatedBy);
                //sqlparams[4] = new SqlParameter("@CityID", CityID);
                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateUser_By_PrimaryKey, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }
            return result;
        }



        public bool UpdateUserEmailByKey()
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@UserID", UserId);
                sqlparams[1] = new SqlParameter("@Email", Email);
                //sqlparams[2] = new SqlParameter("@UpdatedByValue", UpdatedByValue);
                //sqlparams[3] = new SqlParameter("@UpdatedBy", UpdatedBy);
                //sqlparams[4] = new SqlParameter("@CityID", CityID);
                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateEmail_By_PrimaryKey, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }
            return result;
        }

        public bool UpdatePasswordOptions_By_PrimaryKey()
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@UserID", UserId);
                sqlparams[1] = new SqlParameter("@PwdExpiresFirstLogin", PwdExpiresFirst);
                sqlparams[2] = new SqlParameter("@PwdNeverExpires", PwdNeverExpires);
                sqlparams[3] = new SqlParameter("@PwdExpiryDays", PwdExpiryDays);
                sqlparams[4] = new SqlParameter("@Days", PwdDays);
                //sqlparams[2] = new SqlParameter("@UpdatedByValue", UpdatedByValue);
                //sqlparams[3] = new SqlParameter("@UpdatedBy", UpdatedBy);
                //sqlparams[4] = new SqlParameter("@CityID", CityID);
                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdatePasswordOptions_By_PrimaryKey, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }
            return result;
        }

        /// <summary>
        /// Delete User by UserID
        /// </summary>
        public static DataTable DeleteUser_By_UserID(Guid UserID)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserID", UserID);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_DeleteUserBy_UserID, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        //public static DataTable GetAllData(EditUsers Obj, ref int ReturnTotalRows, string OrderBy, Enumaration.QueryType querytype = Enumaration.QueryType.Equals, int? CompanyID = 0, int? PageIndex = null, int? PageSize = null, Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc)
        //{
        //    string strWhere = BaseObject.GetWhere(Obj, querytype);
        //    List<SqlParameter> sqlparamsList = BaseObject.GetSqlParameters(Obj);
        //    OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
        //    DataTable dt = null;
        //    try
        //    {
        //        if (PageIndex != null)
        //        {
        //            sqlparamsList.Add(new SqlParameter("@PageIndex", PageIndex));
        //            sqlparamsList.Add(new SqlParameter("@PageSize", PageSize));
        //            sqlparamsList.Add(new SqlParameter("@TotalRows", ReturnTotalRows));
        //            sqlparamsList.Add(new SqlParameter("@CompanyID", CompanyID));
        //            SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
        //            sqlparamTotalRows.Direction = ParameterDirection.Output;
        //            SqlParameter[] sqlparams = sqlparamsList.ToArray();
        //            dt = SqlHelper.ExecuteDataset(CommandType.Text, ApplyPagingQueryWithCount(Query_GetAllUser + " " + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];
        //            ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
        //        }
        //        else
        //        {
        //            sqlparamsList.Add(new SqlParameter("@CompanyID", CompanyID));
        //            SqlParameter[] sqlparams = sqlparamsList.ToArray();
        //            dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll + " " + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), sqlparams).Tables[0];
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        SqlHelper.LogException(ex);
        //        throw ex;
        //    }
        //    return dt;
        //}

        //public bool UpdateDataByKey()
        //{
        //    bool result = false;
        //    try
        //    {
        //        SqlParameter[] sqlparams = new SqlParameter[5];
        //        sqlparams[0] = new SqlParameter("@UpdateDate", UpdateDate);
        //        sqlparams[1] = new SqlParameter("@CityName", CityName);
        //        sqlparams[2] = new SqlParameter("@UpdatedByValue", UpdatedByValue);
        //        sqlparams[3] = new SqlParameter("@UpdatedBy", UpdatedBy);
        //        sqlparams[4] = new SqlParameter("@CityID", CityID);
        //        if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_By_PrimaryKey, sqlparams) > 0)
        //            result = true;
        //    }            
        //    catch (Exception ex)
        //    {
        //        SqlHelper.LogException(ex);                
        //    }
        //    return result;
        //}

        //public bool AddData()
        //{
        //    bool result = false;
        //    try
        //    {
        //        SqlParameter[] sqlparams = new SqlParameter[5];
        //        sqlparams[0] = new SqlParameter("@ApplicationId", ApplicationId);
        //        sqlparams[1] = new SqlParameter("@RoleID", RoleId);
        //        sqlparams[2] = new SqlParameter("@RleName", RoleName);
        //        sqlparams[3] = new SqlParameter("@LoweredRoleName", LoweredRoleName);
        //        sqlparams[4] = new SqlParameter("@Description", Description);
        //        if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_By_PrimaryKey, sqlparams) > 0)

        //            result = true;
        //    }            
        //    catch (Exception ex)
        //    {
        //        SqlHelper.LogException(ex);                
        //    }
        //    return result;
        //}
        #endregion Methods

    }
}
