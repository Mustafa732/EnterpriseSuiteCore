using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;

namespace FnI.BusinessAccessLayer.Security
{
    public partial class aspnet_Roles : BaseObject
    {
        #region Fields
        private Guid f_ApplicationId;
        private Guid f_RoleId;
        private string f_RoleName;
        private string f_LoweredRoleName;
        private string f_Description;
        //	Not database fields
        private bool f_IsDataLoaded;

        #endregion Fields

        #region Properties
        public Guid ApplicationId
        {
            get { return this.f_ApplicationId; }
            set { this.f_ApplicationId = value; }
        }
        public Guid RoleId
        {
            get { return this.f_RoleId; }
            set { this.f_RoleId = value; }
        }
        public string RoleName
        {
            get { return this.f_RoleName; }
            set { this.f_RoleName = value; }
        }
        public string LoweredRoleName
        {
            get { return this.f_LoweredRoleName; }
            set { this.f_LoweredRoleName = value; }
        }
        public string Description
        {
            get { return this.f_Description; }
            set { this.f_Description = value; }
        }
        public bool IsDataLoaded
        {
            get { return f_IsDataLoaded; }
        }
        #endregion Properties

        #region AutomaticPropertiesForSearching

        public Guid? ApplicationId_S { get; set; }
        public Guid? RoleId_S { get; set; }
        public string RoleName_S { get; set; }
        public string LoweredRoleName_S { get; set; }
        public string Description_S { get; set; }


        #endregion AutomaticPropertiesForSearching

        #region ConstantStrings

        const string Query_GetRole_By_RoleName = @"select * from aspnet_Roles where LoweredRoleName = @LoweredRoleName";

        const string Query_SelectRoleName = @" SELECT 
		[ApplicationId],		
		[RoleId],
        [RoleName],		
        [LoweredRoleName],
        [Description]        
		FROM aspnet_Roles where LoweredRoleName = @LoweredRoleName";


        const string Query_SelectAll_ByUserId = @"select 
        UsersInRoles.UserId
        ,aspnet_Roles.RoleId
        ,aspnet_Roles.RoleName
        ,UsersInRoles.CompanyID
        ,convert(bit,'True') IsAssigned                
        from aspnet_Roles
        inner join UsersInRoles on UsersInRoles.RoleId = aspnet_Roles.RoleId
        where UserId = @UserId";

        const string Query_SelectAll_ByRoleIds = @"
        select 
        @UserId UserId
        ,aspnet_Roles.RoleId
        ,aspnet_Roles.RoleName
        ,@CompanyID CompanyID
        ,convert(bit,'False') IsAssigned        
        from aspnet_Roles ";

        const string Query_SelectAll = @" SELECT 
		[ApplicationId],		
		[RoleId],
        [RoleName],		
        [LoweredRoleName],
        [Description] ,
        'False' as IsAssigned
		FROM aspnet_Roles";

        const string Query_Select_By_PrimaryKey = @"SELECT 
		[ApplicationId],		
		[RoleId],
        [RoleName],		
        [LoweredRoleName],
        [Description]        
		FROM aspnet_Roles  where RoleID = @RoleID";

        const string Query_Insert = @" INSERT INTO aspnet_Roles( 
		[ApplicationId],		
		[RoleId],
        [RoleName],		
        [LoweredRoleName],
        [Description]) 
		Values 
		 (@ApplicationId,
		@RoleId,
		@RoleName,        
		@LoweredRoleName,
		@Description		
		) ";

        const string Query_Update_By_PrimaryKey = @" UPDATE aspnet_Roles SET 
		[RoleName] = @RoleName		 
		WHERE [RoleId] = @RoleId";

        const string Query_Delete_By_PrimaryKey = @" DELETE FROM aspnet_Roles 
		WHERE [RoleId] = @RoleId";

        const string Query_Select_Field = @" Select {0} [{1}] from aspnet_Roles where [{2}] like '%' + @Value + '%'";

        #endregion ConstantStrings

        #region Constructors

        public aspnet_Roles()
            : base()
        { }

        public aspnet_Roles(Guid roleId)
        {
            Load(roleId);
        }

        #endregion Constructors

        #region Loading Methods

        private void Load(Guid roleId)
        {
            DataRow drRecord = GetDataByKey(roleId);

            if (drRecord != null)
            {
                this.f_IsDataLoaded = true;
                this.f_ApplicationId = Guid.Parse(drRecord["ApplicationId"].ToString());
                this.f_RoleId = Guid.Parse(drRecord["RoleId"].ToString());
                this.f_RoleName = Convert.ToString(drRecord["RoleName"]);
                this.f_LoweredRoleName = Convert.ToString(drRecord["LoweredRoleName"]);

                if (drRecord["Description"] != DBNull.Value)
                    this.f_Description = Convert.ToString(drRecord["Description"]);

            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get Role by RoleName 
        /// </summary>
        public static DataTable GetRole_By_RoleName(string LoweredRoleName)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@LoweredRoleName", LoweredRoleName);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetRole_By_RoleName, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataRow GetDataByKey(Guid roleID)
        {
            DataRow drToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@RoleID", roleID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_PrimaryKey, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    drToReturn = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return drToReturn;
        }

        public static DataTable GetAllRoles_ByKeyUserId(Guid? userId, Nullable<Guid> currentUserID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserId", userId);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_ByUserId, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                {
                    dtToReturn = ds.Tables[0];

                    if (!currentUserID.HasValue || !aspnet_UsersInRoles.IsCurrentUserBelongToCISAdmin(currentUserID.Value))
                    {
                        DataRow[] drCISAdmin = dtToReturn.Select("RoleName = 'cisadmin'");

                        if (drCISAdmin.Length > 0)
                            dtToReturn.Rows.Remove(drCISAdmin[0]);
                    }
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }
        public static DataTable GetField(string FieldName, string Value, int? Top = null, bool? IsDateType = null)
        {
            DataTable dt = null;
            try
            {
                string str = string.Format(Query_Select_Field, (Top == null ? "" : "Top " + Top.ToString()), FieldName, (IsDateType == true ? BaseObject.ConvertDateSqlFormat(FieldName) : FieldName));
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@Value", Value);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, str, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllData(string strRoleIDs, Guid? userID, int companyID, Nullable<Guid> currentUserID)
        {
            DataTable dtToReturn = null;

            try
            {
                string strWhereRoleids = !string.IsNullOrEmpty(strRoleIDs) ? " WHERE RoleID NOT IN (" + strRoleIDs + ")" : string.Empty;

                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@UserId", userID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_ByRoleIds + strWhereRoleids, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                {
                    dtToReturn = ds.Tables[0];

                    if (!currentUserID.HasValue || !aspnet_UsersInRoles.IsCurrentUserBelongToCISAdmin(currentUserID.Value))
                    {
                        DataRow[] drCISAdmin = dtToReturn.Select("RoleName = 'cisadmin'");

                        if(drCISAdmin.Length > 0)
                            dtToReturn.Rows.Remove(drCISAdmin[0]);
                    }
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataTable GetAllData(Nullable<Guid> currentUserID)
        {
            DataTable dtToReturn = null;

            try
            {
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll);

                if (ds != null && ds.Tables.Count > 0)
                {
                    dtToReturn = ds.Tables[0];

                    if (!currentUserID.HasValue || !aspnet_UsersInRoles.IsCurrentUserBelongToCISAdmin(currentUserID.Value))
                    {
                        DataRow[] drCISAdmin = dtToReturn.Select("RoleName = 'cisadmin'");

                        if (drCISAdmin.Length > 0)
                            dtToReturn.Rows.Remove(drCISAdmin[0]);
                    }
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        /// <summary>
        /// Used for All Data with and without Paging
        /// </summary>
        public static DataTable GetAllData(ref int ReturnTotalRows, int? PageIndex = null, int? PageSize = null)
        {
            DataTable dt = null;
            try
            {
                if (PageIndex != null)
                {
                    SqlParameter[] sqlparams = new SqlParameter[3];
                    sqlparams[0] = new SqlParameter("@PageIndex", PageIndex);
                    sqlparams[1] = new SqlParameter("@PageSize", PageSize);
                    sqlparams[2] = new SqlParameter("@TotalRows", ReturnTotalRows);
                    sqlparams[2].Direction = ParameterDirection.Output;
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, ApplyPagingQueryWithCount(Query_SelectAll), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparams[2].Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        /// <summary>
        /// Used for All Data Sorted with and without Paging
        /// </summary>
        public static DataTable GetAllData(ref int ReturnTotalRows, string OrderBy, Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, int? PageIndex = null, int? PageSize = null)
        {
            DataTable dt = null;
            try
            {
                if (PageIndex != null)
                {
                    SqlParameter[] sqlparams = new SqlParameter[3];
                    sqlparams[0] = new SqlParameter("@PageIndex", PageIndex);
                    sqlparams[1] = new SqlParameter("@PageSize", PageSize);
                    sqlparams[2] = new SqlParameter("@TotalRows", ReturnTotalRows);
                    sqlparams[2].Direction = ParameterDirection.Output;
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, ApplyPagingQueryWithCount(Query_SelectAll + " Order By [" + OrderBy + "] " + sortdirection.ToString()), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparams[2].Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll + " Order By [" + OrderBy + "] " + sortdirection.ToString()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        /// <summary>
        /// Used for All Data Sorted with and without Paging
        /// </summary>
        public static DataTable GetAllData(SET_Company Obj, ref int ReturnTotalRows, string OrderBy, Enumaration.QueryType querytype = Enumaration.QueryType.Equals, int? PageIndex = null, int? PageSize = null, Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc)
        {
            string strWhere = BaseObject.GetWhere(Obj, querytype);
            List<SqlParameter> sqlparamsList = BaseObject.GetSqlParameters(Obj);
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, ApplyPagingQueryWithCount(Query_SelectAll + " " + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll + " " + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static bool DeleteDataByKey(Guid RoleID)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@RoleID", RoleID);
                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_By_PrimaryKey, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw new BusinessLogicException("Error in delete class aspnet_Roles: " + ex.Message);
            }
            return result;
        }

        #endregion Methods
    }
}
