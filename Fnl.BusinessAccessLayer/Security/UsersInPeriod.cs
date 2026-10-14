using System;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Security
{
    public class UsersInPeriod : BaseObject
    {
        #region Fields
        private Guid f_UserId;
        private int f_CompanyID;
        private int f_PeriodID;
        private int f_FiscalID;
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
        public int PeriodID
        {
            get { return this.f_PeriodID; }
            set { this.f_PeriodID = value; }
        }
        public int FiscalID
        {
            get { return this.f_FiscalID; }
            set { this.f_FiscalID = value; }
        }
        #endregion Properties

        #region ConstantStrings

        const string Query_ViewCompaniesInPeriods_ByUserId = @" SELECT 
        ROW_NUMBER() Over (Order by  SET_Company.CompanyName,SET_Period.[PeriodSetName]) as 'SNO',
        SET_Company.CompanyName,
        SET_Period.[PeriodSetName] PeriodName
        FROM SET_Period 
        INNER JOIN UsersInPeriod ON UsersInPeriod.PeriodID = SET_Period.PeriodId
        INNER JOIN (
	        SELECT SET_Company.[CompanyId],
	        (SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName) AS CompanyName
	        FROM SET_Company 
	        INNER JOIN SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId
	        INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId 
	        inner join UsersInCompanies on UsersInCompanies.CompanyID = SET_Company.[CompanyId]
	        group by SET_Company.[CompanyId],(SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName)
        )SET_Company on SET_Company.CompanyId = UsersInPeriod.CompanyID	where UsersInPeriod.UserId =@UserId";

        const string Query_ViewCompaniesInPeriods_ByUserIdFiscal = @" SELECT 
        ROW_NUMBER() Over (Order by  SET_Company.CompanyName,SET_PeriodCustom.[PeriodSetName]) as 'SNO',
        SET_Company.CompanyName,
        SET_PeriodCustom.[PeriodSetName] PeriodName
        FROM SET_PeriodCustom 
        INNER JOIN UsersInPeriod ON UsersInPeriod.PeriodID = SET_PeriodCustom.PeriodId and SET_PeriodCustom.FiscalID = UsersInPeriod.FiscalID
        INNER JOIN (
	        SELECT SET_Company.[CompanyId],
	        (SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName) AS CompanyName
	        FROM SET_Company 
	        INNER JOIN SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId
	        INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId 
	        inner join UsersInCompanies on UsersInCompanies.CompanyID = SET_Company.[CompanyId]
	        group by SET_Company.[CompanyId],(SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName)
        )SET_Company on SET_Company.CompanyId = UsersInPeriod.CompanyID	where UsersInPeriod.UserId =@UserId";

        const string Query_Select_All = @" 
        Select
        [UserId],
        [CompanyID],
        [PeriodID],[FiscalID]
        from [UsersInPeriod]";

        const string Query_Insert = @"Insert Into UsersInPeriod( 
        [UserId],
        [CompanyID],
        [PeriodID],[FiscalID])        
        Values
        (@UserId,
        @CompanyID,
        @PeriodID,
        @FiscalID
        ) ";

        const string Query_Update_ByKey = @"Update UsersInPeriod SET CompanyID = @CompanyID , PeriodID=@PeriodID, FiscalID = @FiscalID
        where UserId = @UserId";

        const string Query_Delete_ByKey = @"Delete From UsersInPeriod where UserId = @UserId";

        #endregion ConstantStrings

        #region Constructor

        public UsersInPeriod()
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
        public static DataTable GetAllData_ByKey(Guid UserId,int CurrentFiscalID =0)
        {
            DataTable dt = null;
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserId", UserId);
                if(CurrentFiscalID>0)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_ViewCompaniesInPeriods_ByUserIdFiscal, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_ViewCompaniesInPeriods_ByUserId, sqlparams).Tables[0];

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
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@UserId", UserId);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[2] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[3] = new SqlParameter("@FiscalID", FiscalID);
                if (SqlHelper.ExecuteNonQuery(tran, CommandType.Text, Query_Insert, sqlparams) > 0)
                    result = true;
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
                if (SqlHelper.ExecuteNonQuery(trans, CommandType.Text, Query_Delete_ByKey, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw new BusinessLogicException("Error in Delete class Users in period " + ex.Message);
            }
            return result;
        }

        #endregion Methods
    }
}
