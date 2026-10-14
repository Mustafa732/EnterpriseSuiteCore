using System;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using System.Text;

namespace FnI.BusinessAccessLayer.Security
{
    public class UsersInRoles : BaseObject
    {
        #region Fields
        private Guid f_UserId;
        private int f_CompanyID;
        private Guid f_RoleID;        
        #endregion Fields

        #region Properties
        public Guid UserId
        {
            get { return this.f_UserId; }
            set { this.f_UserId = value; }
        }
        public int CompanyID
        {
            get { return this.f_CompanyID; }
            set { this.f_CompanyID = value; }
        }
        public Guid RoleID
        {
            get { return this.f_RoleID; }
            set { this.f_RoleID = value; }
        }        
        #endregion Properties

        #region ConstantStrings

        const string Query_ViewCompaniesInRoles_ByUserId = @"SELECT 
        ROW_NUMBER() Over (Order by  SET_Company.CompanyName,aspnet_Roles.RoleName) as 'SNO',
        SET_Company.CompanyName,
        aspnet_Roles.RoleName,
        aspnet_Roles.RoleId
        FROM aspnet_Roles 
        INNER JOIN UsersInRoles ON UsersInRoles.RoleId = aspnet_Roles.RoleId			
        inner join (
	        SELECT SET_Company.[CompanyId],
	        (SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName) AS CompanyName
	        FROM SET_Company 
	        INNER JOIN SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId
	        INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId 
	        inner join UsersInCompanies on UsersInCompanies.CompanyID = SET_Company.[CompanyId]
	        group by SET_Company.[CompanyId],(SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName)
        )SET_Company on SET_Company.CompanyId = UsersInRoles.CompanyID		
        where UsersInRoles.UserId = @UserId
        {0}
";

        const string Query_SelectAll_By_UserID = @" Select [UserId], [CompanyID], [RoleID] from [UsersInRoles] Where UserID = @UserID";

        const string Query_Select_All = @" 
        Select
        [UserId],
        [CompanyID],
        [RoleID]
        from [UsersInRoles]";

        const string Query_Insert = @"Insert Into UsersInRoles( 
        [UserId],
        [CompanyID],
        [RoleID])        
        Values
        (@UserId,
        @CompanyID,
        @RoleID
        ) ";

        const string Query_Update_ByKey = @"Update UsersInRoles SET CompanyID = @CompanyID , RoleID=@RoleID
        where UserId = @UserId";

        const string Query_Delete_ByKey = @"Delete From aspnet_UsersInRoles where UserId = @UserId;
            Delete From UsersInRoles where UserId = @UserId;";

        #endregion ConstantStrings

        #region Constructor

        public UsersInRoles()
            : base()
        { }

        #endregion Constructor

        #region Methods

        public static DataTable GetAll()
        {
            DataTable dt = null;
            try
            {   
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_All).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetAllData_ByKey(Guid UserId, Nullable<int> companyId = null)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserId", UserId);

                StringBuilder sb = new StringBuilder();

                if (companyId.HasValue)
                {
                    sb.Append(" AND UsersInRoles.CompanyID = " + companyId);
                }

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_ViewCompaniesInRoles_ByUserId,sb.ToString()), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetAll_By_UserID(Guid userID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserId", userID);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_By_UserID, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAll_Roles_By_UserID(Guid UserId)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserId", UserId);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_ViewCompaniesInRoles_ByUserId, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public bool Insert(SqlTransaction tran)
        {
            bool result = false;

            try
            {                
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@UserId", UserId);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[2] = new SqlParameter("@RoleID", RoleID);

                if (SqlHelper.ExecuteNonQuery(tran, CommandType.Text, Query_Insert, sqlparams) > 0)
                {
                    InsertIntoASPNETUsersInRoles(tran);
                    result = true;
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw new BusinessLogicException("Error in insert class user in period: " + ex.Message);
            }

            return result;
        }

        public bool UpdateDataByKey(Guid UserId)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserId", UserId);
                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_ByKey, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw new BusinessLogicException("Error in update class Users in period " + ex.Message);
            }
            return result;
        }

        public static bool DeleteDataByKey(Guid UserId, SqlTransaction trans)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserId", UserId);

                if (SqlHelper.ExecuteNonQuery(trans,CommandType.Text, Query_Delete_ByKey, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw new BusinessLogicException("Error in Delete class Users in period " + ex.Message);
            }

            return result;
        }

        private void InsertIntoASPNETUsersInRoles(SqlTransaction objTransaction)
        {
            string qryDelete = "Delete from aspnet_UsersInRoles "
                + "Where not exists (Select * from UsersInRoles ur where ur.UserId = aspnet_UsersInRoles.UserId and ur.RoleId = aspnet_UsersInRoles.RoleId)";
            SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, qryDelete, null);

            string qrySelect = "Select Count(*) from aspnet_UsersInRoles Where UserID = @UserID and RoleID = @RoleID";

            SqlParameter[] parASPNETUsersInRoles = new SqlParameter[2];
            parASPNETUsersInRoles[0] = new SqlParameter("@UserId", UserId);
            parASPNETUsersInRoles[1] = new SqlParameter("@RoleID", RoleID);

            int noOfRecords = Convert.ToInt16(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, qrySelect, parASPNETUsersInRoles));

            if (noOfRecords == 0)
            {
                string qryInsert = "Insert Into aspnet_UsersInRoles Values(@UserID, @RoleID)";
                SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, qryInsert, parASPNETUsersInRoles);
            }
        }

        #endregion Methods
    }
}
