using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using System.Text;

namespace FnI.BusinessAccessLayer.Security
{
    public class aspnet_Users : BaseObject
    {
        #region Fields

        private Guid f_UserId;
        private DateTime f_LastActivityDate;
        private bool f_IsAnonymous;
        private string f_UserName;
        private string f_LoweredUserName;
        private string f_MobileAlias;
        private Guid f_ApplicationId;

        //  Not database fields
        private bool f_IsDataLoaded;

        #endregion Fields

        #region Properties

        public Guid UserId
        {
            get { return this.f_UserId; }
            set { this.f_UserId = value; }
        }

        public DateTime LastActivityDate
        {
            get { return this.f_LastActivityDate; }
            set { this.f_LastActivityDate = value; }
        }

        public bool IsAnonymous
        {
            get { return this.f_IsAnonymous; }
            set { this.f_IsAnonymous = value; }
        }

        public string UserName
        {
            get { return this.f_UserName; }
            set { this.f_UserName = FilterInputs.FilterText(value); }
        }

        public string LoweredUserName
        {
            get { return this.f_LoweredUserName; }
            set { this.f_LoweredUserName = FilterInputs.FilterText(value); }
        }

        public string MobileAlias
        {
            get { return this.f_MobileAlias; }
            set { this.f_MobileAlias = FilterInputs.FilterText(value); }
        }
        
        public Guid ApplicationId
        {
            get { return this.f_ApplicationId; }
            set { this.f_ApplicationId = value; }
        }

        public bool IsDataLoaded
        {
            get { return f_IsDataLoaded; }
        }

        #endregion Properties

        #region AutomaticPropertiesForSearching

        public Guid? UserId_S { get; set; }
        public DateTime? LastActivityDate_S { get; set; }
        public bool? IsAnonymous_S { get; set; }
        public string UserName_S { get; set; }
        public string LoweredUserName_S { get; set; }
        public string MobileAlias_S { get; set; }
        public Guid? ApplicationId_S { get; set; }

        #endregion AutomaticPropertiesForSearching

        #region ConstantStrings

        const string Query_SelectAllForSearch_WithPaging = @" SELECT [Userid], [UserName], ROW_NUMBER() Over (Order by [UserName]) as 'Row' FROM [aspnet_Users] ";
        const string Query_SelectAllForSearch_WithoutPaging = @" SELECT [Userid], [UserName] FROM [aspnet_Users] ";

        const string Query_GetUserForApprovalExists = @" select TAB.AuthorityID,TAB.UserID,aspnet_users.UserName,TAB.AuthorityLevel from (
select SET_ApprovalAuthority.UserID,SET_ApprovalAuthority.AuthorityID,SET_Authority.AuthorityLevel
		from SET_ApprovalAuthority 
		inner join SET_Authority on SET_Authority.AuthorityID = SET_ApprovalAuthority.AuthorityID
		and SET_Authority.status = 1
		
		where SET_ApprovalAuthority.status = 1
		and SET_ApprovalAuthority.CompanyID = @CompanyID
		and SET_ApprovalAuthority.PeriodID = @PeriodID
		) as TAB
		inner join aspnet_users on aspnet_users.userid = TAb.UserID 
        where aspnet_users.UserName = @userName or @userName is null
        order by TAB.AuthorityLevel";

        const string Query_Get_DataAgainstLevel = @"
                        SELECT 
		                        aspnet_Users.[UserId],		
		                        [UserName]		
		                        FROM aspnet_Users
                        Inner Join SET_ApprovalAuthority ON SET_ApprovalAuthority.UserID = aspnet_Users.UserID
                        Inner Join Set_Authority ON Set_Authority.AuthorityID = SET_ApprovalAuthority.AuthorityID
	                        AND Set_Authority.CompanyID = SET_ApprovalAuthority.CompanyID
	                        AND Set_Authority.PeriodID = SET_ApprovalAuthority.PeriodID
	                        AND Set_Authority.Status = 1
                        Where SET_ApprovalAuthority.CompanyID = @CompanyID
                        AND SET_ApprovalAuthority.PeriodID = @PeriodID
                        AND SET_ApprovalAuthority.Status = 1
                        AND Set_Authority.Authoritylevel >= @Level";

        const string Query_SelectAll = @" SELECT 
		[UserId],
		[LastActivityDate],
		[IsAnonymous],
		[UserName],
		[LoweredUserName],
		[MobileAlias],
		[ApplicationId] 
		FROM aspnet_Users";

        const string Query_SelectByUserID = @" SELECT 
		[UserId],
		[LastActivityDate],
		[IsAnonymous],
		[UserName],
		[LoweredUserName],
		[MobileAlias],
		[ApplicationId] 
		FROM aspnet_Users
        Where 1 = 1
		{0}";

        const string Query_SelectAll_ForAssigment = @" select '-1' as  UserId,getdate() as LastActivityDate,0 as  IsAnonymous,'Select' as  UserName,'Select'  as LoweredUserName, ''as MobileAlias, null ApplicationId 
union all
SELECT distinct convert(varchar(100),aspnet_Users.UserId)as UserId,[LastActivityDate],[IsAnonymous],[UserName],[LoweredUserName],[MobileAlias],[ApplicationId] 
FROM aspnet_Users
inner join UsersOption on UsersOption.UserId =  aspnet_Users.UserId
inner join INV_Item on INV_Item.ApprovalItemGroupID = UsersOption.ApprovalUserGroupID";

        const string Paging = @"WITH ResultSet AS({0})
        select *
        from ResultSet
        WHERE Row between
        (@PageIndex - 1) * @PageSize + 1 and @PageIndex*@PageSize;
        set @TotalRows = (select count(*)from({1}) as Query)";

        const string Query_SelectAll_Searching_WithPaging = @" select 
        aspnet_Users.UserId,
        aspnet_Users.UserName,
        aspnet_membership.Email,
        'View Companies' Companies,
        'View Periods' [Periods],
        'View Roles' [Roles]
        /*,UsersInCompanies.CompanyName,
        UsersInPeriod.PeriodName,
        UsersInRoles.RoleName*/
        ,ROW_NUMBER() Over (Order by  
        aspnet_Users.UserId,
        aspnet_Users.UserName,
        aspnet_membership.Email
                ) as 'Row' 
        from aspnet_Users
        inner join aspnet_membership on aspnet_Membership.UserId = aspnet_Users.UserId
        left join (
			        SELECT 
			        UsersInCompanies.UserId,SET_Company.[CompanyId],(SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName) AS CompanyName
			        FROM SET_Company 
			        INNER JOIN SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId
			        INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId 
			        inner join UsersInCompanies on UsersInCompanies.CompanyID = SET_Company.[CompanyId]
			        ) UsersInCompanies on UsersInCompanies.UserId = aspnet_Users.UserId
        left join (
			        SELECT 
			        UsersInPeriod.UserId,SET_Period.[PeriodSetName] PeriodName
			        FROM SET_Period 
			        INNER JOIN UsersInPeriod ON UsersInPeriod.PeriodID = SET_Period.PeriodId			
			        inner join SET_Company on SET_Company.CompanyId = UsersInPeriod.CompanyID
			        ) UsersInPeriod on UsersInPeriod.UserId = aspnet_Users.UserId
        left join (
		        SELECT 
		        UsersInRoles.UserId, aspnet_Roles.RoleName
		        FROM aspnet_Roles 
		        INNER JOIN UsersInRoles ON UsersInRoles.RoleId = aspnet_Roles.RoleId			
		        inner join SET_Company on SET_Company.CompanyId = UsersInRoles.CompanyID		
		        ) UsersInRoles on UsersInRoles.UserId = aspnet_Users.UserId";

        const string Query_SelectAll_Searching = @" select DISTINCT 
        aspnet_Users.UserId,
        aspnet_Users.UserName,
        aspnet_membership.Email,
        'View Companies' Companies,
        'View Periods' [Periods],
        'View Roles' [Roles]
        /*,UsersInCompanies.CompanyName,
        UsersInPeriod.PeriodName,
        UsersInRoles.RoleName*/
        from aspnet_Users
        inner join aspnet_membership on aspnet_Membership.UserId = aspnet_Users.UserId
        left join (
			        SELECT 
			        UsersInCompanies.UserId,SET_Company.[CompanyId],(SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName) AS CompanyName
			        FROM SET_Company 
			        INNER JOIN SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId
			        INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId 
			        inner join UsersInCompanies on UsersInCompanies.CompanyID = SET_Company.[CompanyId]
			        ) UsersInCompanies on UsersInCompanies.UserId = aspnet_Users.UserId
        left join (
			        SELECT 
			        UsersInPeriod.UserId,SET_Period.[PeriodSetName] PeriodName
			        FROM SET_Period 
			        INNER JOIN UsersInPeriod ON UsersInPeriod.PeriodID = SET_Period.PeriodId			
			        inner join SET_Company on SET_Company.CompanyId = UsersInPeriod.CompanyID
			        ) UsersInPeriod on UsersInPeriod.UserId = aspnet_Users.UserId
        left join (
		        SELECT 
		        UsersInRoles.UserId, aspnet_Roles.RoleName
		        FROM aspnet_Roles 
		        INNER JOIN UsersInRoles ON UsersInRoles.RoleId = aspnet_Roles.RoleId			
		        inner join SET_Company on SET_Company.CompanyId = UsersInRoles.CompanyID		
		        ) UsersInRoles on UsersInRoles.UserId = aspnet_Users.UserId ";

        const string Query_Select_By_PrimaryKey = @" SELECT 
		[UserId],
		[LastActivityDate],
		[IsAnonymous],
		[UserName],
		[LoweredUserName],
		[MobileAlias],
		[ApplicationId] 
		FROM aspnet_Users WHERE [UserId] = @UserId";

        const string Query_Select_UserName = @" select distinct 
        aspnet_Users.UserName, aspnet_Users.UserId
        from aspnet_Users
        inner join UsersInCompanies on UsersInCompanies.UserId = aspnet_Users.UserId    
        inner join SET_Company on SET_Company.CompanyId = UsersInCompanies.CompanyID and SET_Company.Status=1
        inner join SET_Business on SET_Business.BusinessId = SET_Company.BusinessId
        inner join UsersInPeriod on UsersInPeriod.CompanyID = UsersInCompanies.CompanyID
        and UsersInPeriod.UserId = aspnet_Users.UserId
        inner join SET_PeriodCustom on SET_PeriodCustom.FiscalId = UsersInPeriod.FiscalID   
        where UsersInCompanies.companyid=@CompanyID
";

        const string Query_Insert = @" INSERT INTO aspnet_Users( 
		[LastActivityDate],
		[IsAnonymous],
		[UserName],
		[LoweredUserName],
		[MobileAlias],
		[ApplicationId]) 
		Values 
		 (@LastActivityDate,
		@IsAnonymous,
		@UserName,
		@LoweredUserName,
		@MobileAlias,
		@ApplicationId 
		) ";

        const string Query_Update_By_PrimaryKey = @" UPDATE aspnet_Users SET 
		[LastActivityDate] = @LastActivityDate,
		[IsAnonymous] = @IsAnonymous,
		[UserName] = @UserName,
		[LoweredUserName] = @LoweredUserName,
		[MobileAlias] = @MobileAlias,
		[ApplicationId] = @ApplicationId 
		WHERE [UserId] = @UserId";

        const string Query_Delete_By_PrimaryKey = @" DELETE FROM aspnet_Users 
		WHERE [UserId] = @UserId";

        const string Query_GetUserInformation = @" SELECT DISTINCT Users.FullName,Users.UserName,Users.DesignationName, Users.Email,Company.CompanyName, Company.CompanyID,Period.PeriodSetName, 
	 Period.PeriodTypeName, Period.StartDate, Period.EndDate,Users.DesignationName as RoleName--,Roles.RoleName 
FROM
(
	SELECT u.FullName,Users.UserName, D.DesignationName, m.Email, u.UserId
	FROM UsersDetail u 
	inner Join SET_Designation as D on D.DesignationID = u.DesignationID
	INNER JOIN aspnet_Membership m ON m.UserId = u.UserId
	INNER JOIN aspnet_Users aspnet_Users ON aspnet_Users.UserId = u.UserId
	inner JOin  aspnet_Users as Users on Users.UserId = u.UserId	   
)Users 
INNER JOIN 
(
	SELECT * FROM
	(
		SELECT (b.[BusinessName] + ' - ' + ou.[OperatingUnitName]) AS CompanyName, c.CompanyId,
		uic.UserId
		FROM SET_Company c
		INNER JOIN SET_Business b ON b.BusinessId = c.BusinessId 
		INNER JOIN SET_OperatingUnit ou ON ou.OperatingUnitId = c.OperatingUnitId 
		INNER JOIN UsersInCompanies uic ON uic.CompanyID = c.CompanyId
		WHERE c.Status = 1
	)iCompany
)Company ON Company.UserId = Users.UserId 
INNER JOIN
(
	SELECT * FROM
	(
		SELECT p.PeriodSetName,p.CompanyID, pt.PeriodTypeName, p.StartDate, p.EndDate, uip.UserId 
		FROM SET_PeriodCustom p
		INNER JOIN SET_PeriodType pt ON pt.PeriodTypeID = p.PeriodID 
		INNER JOIN UsersInPeriod uip ON uip.PeriodID = p.PeriodID
		and uip.CompanyID = p.CompanyID 
		and uip.FiscalID = p.FiscalID 
		WHERE p.[Status] = 1 AND p.[IsActive] = 1 AND p.[Status] = 1
	) iPeriod
)Period ON Period.UserId = Users.UserId AND Period.CompanyID = Company.CompanyID
INNER JOIN 
(
	SELECT * FROM       
    (
		SELECT  r.RoleName, uir.UserId, uir.CompanyID              
	    FROM aspnet_Roles r
	    INNER JOIN UsersInRoles uir ON uir.RoleId = r.RoleId 
	)iRoles
)Roles ON Roles.UserId = Users.UserId
Where (NULL IS NULL)
{0}
ORDER BY  Users.UserName ,Users.FullName, Company.CompanyName
 ";

        const string Query_GetDataForAuditTrail = @"select row_number() over (order by UsersDetail.FullName) as SrNo, UsersDetail.FullName,UsersDetail.UserId ,aspnet_Users.UserName,
	SET_Designation.DesignationName
	,AuditTrail.EventDescription as Module,AuditTrail.CurrentForm as FormName,AuditTrail.Timestamp as DateTime
	,SET_DocumentType.DocumentType,AuditTrail.DocumentNo,AuditTrail.EventName as Action
	from aspnet_Users
	inner join [UsersDetail] on [UsersDetail].UserID=aspnet_Users.UserId
	inner join SET_Designation on SET_Designation.DesignationID=UsersDetail.DesignationID
	And SET_Designation.IsActive=1 and SET_Designation.Status=1
	inner join AuditTrail on AuditTrail.UserID = aspnet_Users.UserId
	left join SET_DocumentType on SET_DocumentType.DocumentTypeID = AuditTrail.DocumentTypeID
	where AuditTrail.UserID != ''
    AND AuditTrail.UserID != '0'
    and AuditTrail.Timestamp between @FromDate and @ToDate
    {0}
	order by AuditTrail.EventUser,AuditTrail.DocumentTypeID,AuditTrail.DocumentNo,AuditTrail.EventID
	";

        const string Query_CheckUsersAllTables = @"
select top 1  CreatedBy from SAL_InvoiceHeader where status = 1 and CreatedBy = @userID
union all
select top 1  CreatedBy from AR_InvoiceHeader where status = 1 and CreatedBy = @userID
union all
select top 1  CreatedBy from AP_PaymentHeader where status = 1 and CreatedBy = @userID
union all
select top 1  CreatedBy from AR_ReceiptTransactionHeader where status = 1 and CreatedBy = @userID
union all
select top 1  CreatedBy from GL_TransactionHeader where status = 1 and CreatedBy = @userID
union all
select top 1  CreatedBy from INV_StockTransactionHeader where status = 1 and CreatedBy = @userID";

        #endregion ConstantStrings

        #region Constructors

        public aspnet_Users()
            : base()
        { }

        public aspnet_Users(Guid userID)
            : base()
        {
            Load(userID);
        }

        public aspnet_Users(int CompanyID)
            : base()
        {
            Load(CompanyID);
        }

        #endregion

        #region Loading Methods

        private void Load(Guid userID)
        {
            DataTable dtUser = GetDataByKey(userID);

            if (dtUser != null && dtUser.Rows.Count > 0)
            {
                this.f_IsDataLoaded = true;

                this.f_ApplicationId = Guid.Parse(dtUser.Rows[0]["ApplicationId"].ToString());
                this.f_UserId = Guid.Parse(dtUser.Rows[0]["UserId"].ToString());
                this.f_UserName = Convert.ToString(dtUser.Rows[0]["UserName"]);
                this.f_LoweredUserName = Convert.ToString(dtUser.Rows[0]["LoweredUserName"]);

                if (dtUser.Rows[0]["MobileAlias"] != DBNull.Value)
                    this.f_MobileAlias = Convert.ToString(dtUser.Rows[0]["MobileAlias"]);

                this.f_IsAnonymous = Convert.ToBoolean(dtUser.Rows[0]["IsAnonymous"]);
                this.f_LastActivityDate = Convert.ToDateTime(dtUser.Rows[0]["LastActivityDate"]);
            }
        }

        private void Load(int Companyid)
        {
            DataTable dtUser = GetCurrentCompanyUser(Companyid);

            if (dtUser != null && dtUser.Rows.Count > 0)
            {
                this.f_IsDataLoaded = true;

                this.f_ApplicationId = Guid.Parse(dtUser.Rows[0]["ApplicationId"].ToString());
                this.f_UserId = Guid.Parse(dtUser.Rows[0]["UserId"].ToString());
                this.f_UserName = Convert.ToString(dtUser.Rows[0]["UserName"]);
                this.f_LoweredUserName = Convert.ToString(dtUser.Rows[0]["LoweredUserName"]);

                if (dtUser.Rows[0]["MobileAlias"] != DBNull.Value)
                    this.f_MobileAlias = Convert.ToString(dtUser.Rows[0]["MobileAlias"]);

                this.f_IsAnonymous = Convert.ToBoolean(dtUser.Rows[0]["IsAnonymous"]);
                this.f_LastActivityDate = Convert.ToDateTime(dtUser.Rows[0]["LastActivityDate"]);
            }
        }

        #endregion

        #region Methods

        public static DataTable GetDataByLevel(int companyId , int periodId , int Level)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyId);
                sqlparams[1] = new SqlParameter("@PeriodID", periodId);
                sqlparams[2] = new SqlParameter("@Level", Level);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_DataAgainstLevel,sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetAllData()
        {
            DataTable dt = null;
            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetAllDataByUserID(string UserId)
        {
            StringBuilder sb = new StringBuilder();

            if (!string.IsNullOrEmpty(UserId))
            {
                sb.Append(" AND UserId IN (" + UserId + ")");
            }

            DataTable dt = null;
            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(Query_SelectByUserID, sb.ToString())).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetAllDataForAssigment(int ItemID = 0)
        {
            DataTable dt = null;
            string ItemFillter = "";
            try
            {
                if (ItemID > 0)
                    ItemFillter = " where INV_Item.ItemID = " + ItemID + "";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_ForAssigment + ItemFillter).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetAllData(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null, Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;
            try
            {
                if (PageIndex != null)
                {
                    sqlparamsList.Add(new SqlParameter("@PageIndex", PageIndex));
                    sqlparamsList.Add(new SqlParameter("@PageSize", PageSize));
                    sqlparamsList.Add(new SqlParameter("@TotalRows", ReturnTotalRows));
                    SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                    sqlparamTotalRows.Direction = ParameterDirection.Output;
                    SqlParameter[] sqlparams = sqlparamsList.ToArray();
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_Searching_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), Query_SelectAll_Searching_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_Searching + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetAllDataForSearch(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, SqlTransaction objTransaction = null)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

            try
            {
                if (PageIndex != null)
                {
                    sqlparamsList.Add(new SqlParameter("@PageIndex", PageIndex));
                    sqlparamsList.Add(new SqlParameter("@PageSize", PageSize));
                    sqlparamsList.Add(new SqlParameter("@TotalRows", ReturnTotalRows));

                    SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                    sqlparamTotalRows.Direction = ParameterDirection.Output;

                    SqlParameter[] sqlparams = sqlparamsList.ToArray();

                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_SelectAllForSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAllForSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAllForSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAllForSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAllForSearch_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllForSearch_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetDataByKey(Guid UserId)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserId", UserId);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_PrimaryKey, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetCurrentCompanyUser(int CompanyID)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID",CompanyID);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_UserName,sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        //public static DataTable GetData()
        //{
        //    DataTable dt = null;
        //    try
        //    {
        //        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_UserName).Tables[0];
        //    }
        //    catch (Exception ex)
        //    {
        //        SqlHelper.LogException(ex);
        //        throw ex;
        //    }
        //    return dt;
        //}

        public static DataTable Get_UserInformation(int CompanyID, string User, int FiscalID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@User", User);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[2] = new SqlParameter("@FiscalID", FiscalID);

                string query = @" select row_number() over (order by SET_Business.BusinessName) as SrNo,
 SET_Business .BusinessName as Business,SET_OperatingUnit.OperatingUnitName as Company,SET_PeriodCustom .PeriodSetName as Fiscal,--UsersInCompanies.CompanyID,
aspnet_Users.UserName as UserName,aspnet_Users.LastActivityDate + GETDATE() - GETUTCDATE() AS LastLoginDateTime
,UsersDetail.FullName, SET_Designation.DesignationName
from aspnet_Users
inner join UsersInCompanies on UsersInCompanies.UserId = aspnet_Users.UserId
inner join SET_Company on SET_Company.CompanyId = UsersInCompanies.CompanyID and SET_Company.Status=1
inner join SET_OperatingUnit on SET_OperatingUnit.OperatingUnitId=SET_Company.CompanyId and SET_OperatingUnit.Status=1
inner join SET_Business on SET_Business.BusinessId = SET_Company.BusinessId
inner join UsersInPeriod on UsersInPeriod.CompanyID = UsersInCompanies.CompanyID
and UsersInPeriod.UserId = aspnet_Users.UserId
inner join SET_PeriodCustom on SET_PeriodCustom.FiscalId = UsersInPeriod.FiscalID
inner join UsersDetail on UsersDetail.UserID = aspnet_Users.UserId
inner join SET_Designation on SET_Designation.DesignationID = UsersDetail.DesignationID

where aspnet_Users.UserName= @User
And UsersInCompanies.CompanyID=@CompanyID AND SET_PeriodCustom.FiscalId = @FiscalID
ORDER BY aspnet_Users.LastActivityDate DESC
";
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable Get_UserInformation(int CompanyID, int FiscalID)
        {
            DataTable dt = null;

            try
            {
                #region Query
                string query = @" select row_number() over (order by SET_Business.BusinessName) as SrNo,
 SET_Business .BusinessName as Business,SET_OperatingUnit.OperatingUnitName as Company,SET_PeriodCustom .PeriodSetName as Fiscal,--UsersInCompanies.CompanyID,
aspnet_Users.UserName as UserName,aspnet_Users.LastActivityDate + GETDATE() - GETUTCDATE() AS LastLoginDateTime
,UsersDetail.FullName, SET_Designation.DesignationName
from aspnet_Users
inner join UsersInCompanies on UsersInCompanies.UserId = aspnet_Users.UserId
inner join SET_Company on SET_Company.CompanyId = UsersInCompanies.CompanyID and SET_Company.Status=1
inner join SET_OperatingUnit on SET_OperatingUnit.OperatingUnitId=SET_Company.CompanyId and SET_OperatingUnit.Status=1
inner join SET_Business on SET_Business.BusinessId = SET_Company.BusinessId
inner join UsersInPeriod on UsersInPeriod.CompanyID = UsersInCompanies.CompanyID
and UsersInPeriod.UserId = aspnet_Users.UserId
inner join SET_PeriodCustom on SET_PeriodCustom.FiscalId = UsersInPeriod.FiscalID
inner join UsersDetail on UsersDetail.UserID = aspnet_Users.UserId
inner join SET_Designation on SET_Designation.DesignationID = UsersDetail.DesignationID

where UsersInCompanies.CompanyID=@CompanyID AND SET_PeriodCustom.FiscalId = @FiscalID
ORDER BY aspnet_Users.LastActivityDate DESC

";
                #endregion

                SqlParameter[] sqlparms = new SqlParameter[2];
                sqlparms[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparms[1] = new SqlParameter("@FiscalID", FiscalID);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query,sqlparms);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                   dt = ds.Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public bool Insert()
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@LastActivityDate", LastActivityDate);
                sqlparams[1] = new SqlParameter("@IsAnonymous", IsAnonymous);
                sqlparams[2] = new SqlParameter("@UserName", UserName);
                sqlparams[3] = new SqlParameter("@LoweredUserName", LoweredUserName);
                sqlparams[4] = new SqlParameter("@MobileAlias", MobileAlias);
                sqlparams[5] = new SqlParameter("@ApplicationId", ApplicationId);
                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Insert, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return result;
        }

        public bool UpdateDataByKey()
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@LastActivityDate", LastActivityDate);
                sqlparams[1] = new SqlParameter("@IsAnonymous", IsAnonymous);
                sqlparams[2] = new SqlParameter("@UserName", UserName);
                sqlparams[3] = new SqlParameter("@LoweredUserName", LoweredUserName);
                sqlparams[4] = new SqlParameter("@MobileAlias", MobileAlias);
                sqlparams[5] = new SqlParameter("@ApplicationId", ApplicationId);
                sqlparams[6] = new SqlParameter("@UserId", UserId);
                
                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_By_PrimaryKey, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static bool DeleteDataByKey(Guid UserId)
        {               
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("DeleteUser_CPR");

            try
            {
                UsersInRoles.DeleteDataByKey(UserId, sqltrans);
                UsersInPeriod.DeleteDataByKey(UserId, sqltrans);
                UsersInCompanies.DeleteDataByKey(UserId, sqltrans);
                UserOptions.DeleteDataByKey(UserId, sqltrans);
                aspnet_Users.DeleteDataByKey_aspMembership(UserId, sqltrans);
                aspnet_Users.DeleteDataByKey_aspUsers(UserId, sqltrans);

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
                    SqlHelper.RollBackTransaction(sqltrans, "DeleteUser_CPR");
            }

            return result;
        }

        public static bool DeleteDataByKey_aspMembership(Guid UserId, SqlTransaction trans)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserId", UserId);
            
                if (SqlHelper.ExecuteNonQuery(trans, CommandType.Text, "Delete From aspnet_Membership where UserId = @UserId", sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw new BusinessLogicException("Error in Delete class UserOptions " + ex.Message);
            }

            return result;
        }

        public static bool DeleteDataByKey_aspUsers(Guid UserId, SqlTransaction trans)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserId", UserId);
            
                if (SqlHelper.ExecuteNonQuery(trans, CommandType.Text, "Delete From aspnet_users where UserId = @UserId", sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                string detailMessage = ex.Message;

                #region Check for SQLException

                if (ex is SqlException)
                {
                    SqlException deleteException = ex as SqlException;

                    if (deleteException.Number.Equals(547))
                        detailMessage = "Record cannot be deleted or changed as it is being used somewhere else";
                }

                #endregion

                throw new BusinessLogicException("Error while deleting from UserOptions table. " + detailMessage);
            }

            return result;
        }

        public static DataTable GetUserInformation()
        {
            DataTable dt = null;
            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(Query_GetUserInformation,"")).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetUserInformationByName(string name)
        {
            DataTable dt = null;
            StringBuilder filter = new StringBuilder();
            filter.Append("AND UserName = '" + name + "'");
            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(Query_GetUserInformation, filter.ToString())).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }


        public static DataTable GetUserForApprovalExists(int companyId, int periodId, string userName)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyId);
                sqlparams[1] = new SqlParameter("@PeriodID", periodId);
                sqlparams[2] = new SqlParameter("@userName", userName);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetUserForApprovalExists, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetAllDataforAuditTrailReport(Nullable<DateTime> FromDate, Nullable<DateTime> ToDate, Nullable<int> DocumentTypeID, Nullable<int> DocumentNo, string UserID)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@FromDate", FromDate);
                sqlparams[1] = new SqlParameter("@ToDate", ToDate);

                if (DocumentTypeID > 0)
                {
                    sb.Append(" and [SET_DocumentType].DocumentTypeID = " + DocumentTypeID + "");
                }

                if (DocumentNo.HasValue)
                {
                    sb.Append(" and AuditTrail.DocumentNo = " + DocumentNo + "");
                }

                if (UserID != "")
                    sb.Append(" and AuditTrail.UserID = '" + UserID + "'");

                dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDataForAuditTrail,sb),sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }            
            return dtToReturn;
        }

        public static DataTable CheckUsersWithMultipleTables(Guid UserID)
        {
            DataTable dtToReturn = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@userID", UserID);

                dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text, Query_CheckUsersAllTables, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtToReturn;
        }

        #endregion Methods
    }
}
