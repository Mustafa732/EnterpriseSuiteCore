using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Security
{
    public class UsersInCompanies : BaseObject
    {
        #region Fields
        private Guid f_UserId;
        private int f_CompanyID;
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
        #endregion Properties

        #region ConstantStrings

        const string Query_ViewCompanies_ByUserId = @"SELECT 
        ROW_NUMBER() Over (Order by  
        (SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName)
        ) as 'SNO',
        (SET_OperatingUnit.OperatingUnitName) AS CompanyName, SET_Company.CompanyId
        FROM SET_Company 
        INNER JOIN SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId
        INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId 
        inner join UsersInCompanies on UsersInCompanies.CompanyID = SET_Company.[CompanyId]
        where UsersInCompanies.UserId = @UserId";

        const string Query_Select_All = @" 
        Select
        [UserId],
        [CompanyID]
        from [UsersInCompanies]";

        const string Query_Insert = @"Insert Into UsersInCompanies
        ([UserId],
        [CompanyID]  
        )    
        Values
        (@UserId,
        @CompanyID
        ) ";

        const string Query_Update_ByKey = @"Select * from UsersInCompanies 
        where UserId = @UserId and CompanyID = @CompanyID";

        const string Query_Delete_ByKey = @"Delete From UsersInCompanies where UserId = @UserId";

        #endregion ConstantStrings

        #region Constructor

        public UsersInCompanies()
            : base()
        { }

        #endregion Constructor

        #region Methods
        public static DataTable GetAllData_ByKey(Guid UserId, int BusinessId = 0)
        {
            DataTable dt = null;
            string sb = string.Empty;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@UserId", UserId);
                sqlparams[1] = new SqlParameter("@BusinessId", BusinessId);
                //if (BusinessId > 0)
                    sb = " and SET_Business.BusinessId = @BusinessId";


                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_ViewCompanies_ByUserId + sb, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }
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

        public bool UpdateUser_CompaniesPeriodsAndRoles(UserOptions uo, List<UsersInCompanies> colUserInCompanies, List<UsersInPeriod> colUserInPeriods, 
            List<UsersInRoles> colUserInRoles)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateUser");

            try
            {
                UsersInPeriod.DeleteDataByKey(uo.UserId, sqltrans);
                UsersInRoles.DeleteDataByKey(uo.UserId, sqltrans);
                UsersInCompanies.DeleteDataByKey(uo.UserId, sqltrans);
                uo.UpdateDataByKey(sqltrans);

                foreach (UsersInCompanies uc in colUserInCompanies)
                    uc.Insert(sqltrans);

                foreach (UsersInPeriod up in colUserInPeriods)
                    up.Insert(sqltrans);

                foreach (UsersInRoles ur in colUserInRoles)
                    ur.Insert(sqltrans);

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
                if(result)
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateUser");
            }

            return result;
        }

        public bool InsertUser_CompaniesPeriodsAndRoles(UserOptions uo, List<UsersInCompanies> colUserInCompanies, List<UsersInPeriod> colUserInPeriods, 
            List<UsersInRoles> colUserInRoles)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertUser_CPR");
            
            try
            {
                uo.Insert(sqltrans);
        
                foreach (UsersInCompanies uc in colUserInCompanies)
                    uc.Insert(sqltrans);
                
                foreach (UsersInPeriod up in colUserInPeriods)
                    up.Insert(sqltrans);
                
                foreach (UsersInRoles ur in colUserInRoles)
                    ur.Insert(sqltrans);
                
                SqlHelper.CommitTransaction(sqltrans);
                result = true;
            }
            catch (BusinessLogicException ex)
            {
                SqlHelper.RollBackTransaction(sqltrans, "InsertUser_CPR");
                SqlHelper.LogException(ex);
                throw ex;
            }
            catch (Exception ex)
            {
                SqlHelper.RollBackTransaction(sqltrans, "InsertUser_CPR");
                SqlHelper.LogException(ex);
                throw ex;
            }
            
            return result;
        }

        public bool Insert(SqlTransaction trans)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@UserId", UserId);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
                if (SqlHelper.ExecuteNonQuery(trans, CommandType.Text, Query_Insert, sqlparams) > 0)
                    result = true;                
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw new BusinessLogicException("Error in insert class UsersInCompanies: " + ex.Message);
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
                if (SqlHelper.ExecuteNonQuery( CommandType.Text, Query_Insert, sqlparams) > 0)
                    result = true;                
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw new BusinessLogicException("Error in update class UsersInCompanies " + ex.Message);
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
                if (SqlHelper.ExecuteNonQuery(trans, CommandType.Text, Query_Delete_ByKey, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw new BusinessLogicException("Error in Delete class UsersInCompanies " + ex.Message);
            }
            return result;
        }

        public static DataTable GetAllData_ByMultipleKeys(Guid UserId, string BusinessIds)
        {
            DataTable dt = null;
            string sb = string.Empty;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserId", UserId);

                if (BusinessIds != "" && BusinessIds != "()" )
                    sb = " and SET_Business.BusinessId in "+ BusinessIds ;


                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_ViewCompanies_ByUserId + sb, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }
        #endregion Methods
    }
}
