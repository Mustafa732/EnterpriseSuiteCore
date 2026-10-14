using System;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Security
{
    public class UserOptions : BaseObject
    {
        #region Fields

        private Guid f_UserId;
        private bool f_PwdExpiresFirstLogin;
        private bool f_PwdNeverExpires;
        private bool f_PwdExpiryDays;
        private int f_Days;
        private Guid f_CreatedBy;
        private DateTime f_CreateDate;
        private Guid f_UpdatedBy;
        private DateTime f_UpdateDate;
        private string f_CreatedByValue;
        private string f_UpdatedByValue;
        private bool f_ByPassWelcomeScreen;
        private int f_DefaultCompanyID;
        private int f_DefaultPeriodID;
        private int f_ApprovalUserGroupID;
        private Nullable<bool> f_GrowerBusinessPartner;
        private bool f_IsDataLoaded;

        #endregion Fields

        #region Properties

        public Guid UserId
        {
            get { return this.f_UserId; }
            set { this.f_UserId = value; }
        }

        public bool PwdExpiresFirstLogin
        {
            get { return this.f_PwdExpiresFirstLogin; }
            set { this.f_PwdExpiresFirstLogin = value; }
        }

        public bool PwdNeverExpires
        {
            get { return this.f_PwdNeverExpires; }
            set { this.f_PwdNeverExpires = value; }
        }

        public bool PwdExpiryDays
        {
            get { return this.f_PwdExpiryDays; }
            set { this.f_PwdExpiryDays = value; }
        }

        public int Days
        {
            get { return this.f_Days; }
            set { this.f_Days = value; }
        }

        public Guid CreatedBy
        {
            get { return this.f_CreatedBy; }
            set { this.f_CreatedBy = value; }
        }

        public DateTime CreateDate
        {
            get { return this.f_CreateDate; }
            set { this.f_CreateDate = value; }
        }

        public Guid UpdatedBy
        {
            get { return this.f_UpdatedBy; }
            set { this.f_UpdatedBy = value; }
        }

        public DateTime UpdateDate
        {
            get { return this.f_UpdateDate; }
            set { this.f_UpdateDate = value; }
        }

        public string UpdatedByValue
        {
            get { return this.f_UpdatedByValue; }
            set { this.f_UpdatedByValue = value; }
        }

        public string CreatedByValue
        {
            get { return this.f_CreatedByValue; }
            set { this.f_CreatedByValue = value; }
        }

        public bool ByPassWelcomeScreen
        {
            get { return this.f_ByPassWelcomeScreen; }
            set { this.f_ByPassWelcomeScreen = value; }
        }

        public int DefaultCompanyID
        {
            get { return this.f_DefaultCompanyID; }
            set { this.f_DefaultCompanyID = value; }
        }

        public int DefaultPeriodID
        {
            get { return this.f_DefaultPeriodID; }
            set { this.f_DefaultPeriodID = value; }
        }

        public int ApprovalUserGroupID
        {
            get { return this.f_ApprovalUserGroupID; }
            set { this.f_ApprovalUserGroupID = value; }
        }

        public Nullable<bool> GrowerBusinessPartner
        {
            get { return f_GrowerBusinessPartner; }
            set { f_GrowerBusinessPartner = value; }
        }

        public bool IsDataLoaded
        {
            get { return this.f_IsDataLoaded; }
        }

        #endregion Properties

        #region ConstantStrings
        const string Query_GetCreatedUser_Id = @"select UserId FROM aspnet_Users WHERE UserName = @UserName";

        const string Query_InsertUserOptions = @" Insert Into UsersOption([UserId], [PwdExpiresFirstLogin], [PwdNeverExpires], [PwdExpiryDays], [Days], [CreatedBy], 
                [CreateDate], [CreatedByValue], [ByPassWelcomeScreen], [DefaultCompanyID], [DefaultPeriodID], [ApprovalUserGroupID], [GrowerBusinessPartner])
            Values(@UserId, @PwdExpiresFirstLogin, @pwdNeverExpires, @PwdExpiryDays, @Days, @CreatedBy, @CreateDate, @CreatedByValue, @ByPassWelcomeScreen, 
                @DefaultCompanyID, @DefaultPeriodID, @ApprovalUserGroupID, @GrowerBusinessPartner) ";

        const string Query_SelectPasswordOptions = @" Select u.UserName, m.Email, uo.PwdExpiresFirstLogin, uo.PwdNeverExpires, uo.PwdExpiryDays, uo.Days, 
                uo.BypassWelcomeScreen, uo.DefaultCompanyID, uo.DefaultPeriodID, uo.ApprovalUserGroupID, m.IsApproved, uo.CreatedBy, uo.CreateDate, uo.CreatedByValue, uo.UpdatedBy, 
                uo.UpdateDate, uo.UpdatedByValue, uo.GrowerBusinessPartner
            FROM aspnet_Users u 
                INNER JOIN UsersOption uo ON u.UserId = uo.UserId
                inner join aspnet_Membership m on m.UserId = u.UserId
            Where uo.UserId = @UserId";

        const string Query_Update_By_PrimaryKey = @" Update UsersOption SET PwdExpiresFirstLogin = @PwdExpiresFirstLogin, PwdNeverExpires = @PwdNeverExpires, 
                PwdExpiryDays = @PwdExpiryDays, Days = @Days, UpdatedBy = @UpdatedBy, UpdateDate = @UpdateDate, UpdatedByValue = @UpdatedByValue, 
                BypassWelcomeScreen = @ByPassWelcomeScreen, DefaultCompanyID = @DefaultCompanyID, DefaultPeriodID = @DefaultPeriodID, ApprovalUserGroupID = @ApprovalUserGroupID, GrowerBusinessPartner = @GrowerBusinessPartner 
            Where UserId = @UserId";

        const string Query_Delete_ByKey = @"Delete From UsersOption where UserId = @UserId";

        #endregion ConstantStrings

        #region Constructors

        public UserOptions()
            : base()
        { }

        public UserOptions(Guid userID)
        {
            Load(userID);
        }

        #endregion Constructors

        #region Loading Methods

        private void Load(Guid userID)
        {
            DataTable dtRecord = GetUserPasswordOptions(userID);

            if (dtRecord != null && dtRecord.Rows.Count > 0)
            {
                this.f_IsDataLoaded = true;
                this.f_UserId = userID;
                this.f_PwdExpiresFirstLogin = Convert.ToBoolean(dtRecord.Rows[0]["PwdExpiresFirstLogin"]);
                this.f_PwdNeverExpires = Convert.ToBoolean(dtRecord.Rows[0]["PwdNeverExpires"]);
                this.f_PwdExpiryDays = Convert.ToBoolean(dtRecord.Rows[0]["PwdExpiryDays"]);
                this.f_Days = Convert.ToInt16(dtRecord.Rows[0]["Days"]);
                this.f_CreatedBy = Guid.Parse(dtRecord.Rows[0]["CreatedBy"].ToString());
                this.f_CreateDate = Convert.ToDateTime(dtRecord.Rows[0]["CreateDate"]);

                if (dtRecord.Rows[0]["UpdatedBy"] != DBNull.Value)
                    this.f_UpdatedBy = Guid.Parse(dtRecord.Rows[0]["UpdatedBy"].ToString());


                if (dtRecord.Rows[0]["UpdateDate"] != DBNull.Value)
                    this.f_UpdateDate = Convert.ToDateTime(dtRecord.Rows[0]["UpdateDate"]);

                this.f_CreatedByValue = Convert.ToString(dtRecord.Rows[0]["CreatedByValue"]);

                if (dtRecord.Rows[0]["UpdatedByValue"] != DBNull.Value)
                    this.f_UpdatedByValue = Convert.ToString(dtRecord.Rows[0]["UpdatedByValue"]);

                this.f_ByPassWelcomeScreen = Convert.ToBoolean(dtRecord.Rows[0]["BypassWelcomeScreen"]);
                this.f_DefaultCompanyID = Convert.ToInt32(dtRecord.Rows[0]["DefaultCompanyID"]);
                this.f_DefaultPeriodID = Convert.ToInt32(dtRecord.Rows[0]["DefaultPeriodID"]);
                this.f_ApprovalUserGroupID = Convert.ToInt32(dtRecord.Rows[0]["ApprovalUserGroupID"]);

                if (dtRecord.Rows[0]["GrowerBusinessPartner"] != DBNull.Value)
                    this.f_GrowerBusinessPartner = Convert.ToBoolean(dtRecord.Rows[0]["GrowerBusinessPartner"]);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get User Id by UserName 
        /// </summary>
        public static DataTable GetCreatedUserId(string UserName)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserName", UserName);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetCreatedUser_Id, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetUserPasswordOptions(Guid UserId)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserId", UserId);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectPasswordOptions, sqlparams).Tables[0];
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
                SqlParameter[] sqlparams = new SqlParameter[13];

                sqlparams[0] = new SqlParameter("@UserId", f_UserId);
                sqlparams[1] = new SqlParameter("@PwdExpiresFirstLogin", f_PwdExpiresFirstLogin);
                sqlparams[2] = new SqlParameter("@PwdNeverExpires", f_PwdNeverExpires);
                sqlparams[3] = new SqlParameter("@PwdExpiryDays", f_PwdExpiryDays);
                sqlparams[4] = new SqlParameter("@Days", f_Days);
                sqlparams[5] = new SqlParameter("@CreatedBy", f_CreatedBy);
                sqlparams[6] = new SqlParameter("@CreateDate", f_CreateDate);
                sqlparams[7] = new SqlParameter("@CreatedByValue", f_CreatedByValue);
                sqlparams[8] = new SqlParameter("@ByPassWelcomeScreen", f_ByPassWelcomeScreen);
                sqlparams[9] = new SqlParameter("@DefaultCompanyID", f_DefaultCompanyID);
                sqlparams[10] = new SqlParameter("@DefaultPeriodID", f_DefaultPeriodID);
                sqlparams[11] = new SqlParameter("@ApprovalUserGroupID", f_ApprovalUserGroupID);

                if (f_GrowerBusinessPartner.HasValue)
                    sqlparams[12] = new SqlParameter("@GrowerBusinessPartner", f_GrowerBusinessPartner.Value);
                else
                    sqlparams[12] = new SqlParameter("@GrowerBusinessPartner", DBNull.Value);
                
                if (SqlHelper.ExecuteNonQuery(tran, CommandType.Text, Query_InsertUserOptions, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw new BusinessLogicException("Error in insert class company: " + ex.Message);
            }

            return result;
        }

        public bool UpdateDataByKey(SqlTransaction objTrans = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[13];
        
                sqlparams[0] = new SqlParameter("@PwdExpiresFirstLogin", f_PwdExpiresFirstLogin);
                sqlparams[1] = new SqlParameter("@PwdNeverExpires", PwdNeverExpires);
                sqlparams[2] = new SqlParameter("@PwdExpiryDays", f_PwdExpiryDays);
                sqlparams[3] = new SqlParameter("@Days", f_Days);
                sqlparams[4] = new SqlParameter("@UpdatedBy", f_UpdatedBy);
                sqlparams[5] = new SqlParameter("@UpdateDate", f_UpdateDate);
                sqlparams[6] = new SqlParameter("@UpdatedByValue", f_UpdatedByValue);
                sqlparams[7] = new SqlParameter("@UserId", f_UserId);
                sqlparams[8] = new SqlParameter("@ByPassWelcomeScreen", f_ByPassWelcomeScreen);
                sqlparams[9] = new SqlParameter("@DefaultCompanyID", f_DefaultCompanyID);
                sqlparams[10] = new SqlParameter("@DefaultPeriodID", f_DefaultPeriodID);
                sqlparams[11] = new SqlParameter("@ApprovalUserGroupID", f_ApprovalUserGroupID);

                if (f_GrowerBusinessPartner.HasValue)
                    sqlparams[12] = new SqlParameter("@GrowerBusinessPartner", f_GrowerBusinessPartner.Value);
                else
                    sqlparams[12] = new SqlParameter("@GrowerBusinessPartner", DBNull.Value);

                if (objTrans != null)
                {
                    if (SqlHelper.ExecuteNonQuery(objTrans, CommandType.Text, Query_Update_By_PrimaryKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_By_PrimaryKey, sqlparams) > 0)
                        result = true;
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw new BusinessLogicException("Error in update class UsersOption " + ex.Message);
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
                throw new BusinessLogicException("Error in Delete class UserOptions " + ex.Message);
            }
            return result;
        }


        public static int GetDataLocationByUsername(string UserName)
        {
            int LocationId = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];


                sqlparams[0] = new SqlParameter("@UserName", UserName);

                const string Query_GetDataLocationByUsername = @"
SELECT LocationID FROM aspnet_Users
INNER JOIN UsersOption ON UsersOption.UserId =aspnet_Users.UserId 
WHERE   aspnet_Users.UserName = @UserName

";

                LocationId = Convert.ToInt32(SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataLocationByUsername, sqlparams).Tables[0].Rows[0]["LocationID"]);
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return LocationId;
        }



        #endregion Methods
    }
}
