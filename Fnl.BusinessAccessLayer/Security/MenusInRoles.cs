using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Security
{
    public class MenusInRoles : BaseObject
    {

        #region Fields

        private int f_MenuID;
        private Guid f_RoleId;
        private Guid f_CreatedBy;
        private Guid f_UpdatedBy;
        private DateTime f_CreateDate;
        private DateTime f_UpdateDate;
        private string f_CreatedByValue;
        private string f_UpdatedByValue;

        #endregion Fields

        #region Properties

        public int MenuID
        {
            get { return this.f_MenuID; }
            set { this.f_MenuID = value; }
        }
        public Guid RoleId
        {
            get { return this.f_RoleId; }
            set { this.f_RoleId = value; }
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

        #endregion Properties

        #region AutomaticPropertiesForSearching

        public int? MenuID_S { get; set; }
        public Guid? RoleId_S { get; set; }
        public Guid? CreatedBy_S { get; set; }
        public Guid? UpdatedBy_S { get; set; }
        public DateTime? CreateDate_S { get; set; }
        public DateTime? UpdateDate_S { get; set; }
        public string CreatedByValue_S { get; set; }
        public string UpdatedByValue_S { get; set; }

        #endregion AutomaticPropertiesForSearching

        #region ConstantStrings

        const string Query_SelectAll = @" SELECT MenuID, RoleId, CreatedBy, UpdatedBy, CreateDate, UpdateDate, CreatedByValue, UpdatedByValue FROM MenusInRoles ";

        const string Query_Select_By_PrimaryKey = @" SELECT MenuID, RoleId, CreatedBy, UpdatedBy, CreateDate, UpdateDate, CreatedByValue, UpdatedByValue 
FROM MenusInRoles WHERE MenuID = @MenuID and RoleId = @RoleId ";

        const string Query_Insert = @" INSERT INTO MenusInRoles (MenuID, RoleId, CreatedBy, UpdatedBy, CreateDate, UpdateDate, CreatedByValue, UpdatedByValue) 
Values (@MenuID, @RoleId, @CreatedBy, @UpdatedBy, @CreateDate, @UpdateDate, @CreatedByValue, @UpdatedByValue) ";

        const string Query_Update_By_PrimaryKey = @" UPDATE MenusInRoles SET UpdatedBy = @UpdatedBy, UpdateDate = @UpdateDate, UpdatedByValue = @UpdatedByValue 
WHERE MenuID = @MenuID, CompanyID = @CompanyID, RoleId = @RoleId ";

        const string Query_Delete_By_PrimaryKey = @" DELETE FROM MenusInRoles WHERE MenuID = @MenuID, RoleId = @RoleId ";

        const string Query_Delete_Menus_By_Role_Company = @" Delete from MenusInRoles where RoleID = @RoleId ";

        const string Query_GetCount_By_Role_Company = @" set @count = (select Count(*) from MenusInRoles where RoleID = @RoleId) ";

        const string Query_Select_Field = @" Select {0} [{1}] from MenusInRoles where [{2}] like '%' + @Value + '%' ";

        const string Query_Select_Menus = @" 
SELECT Distinct M.MenuID, M.MenuName,M.menuicon, 
    case M.MenuPath when '#' then M.MenuPath else M.MenuPath + (case when CharIndex('?', M.MenuPath) > 0 then '&' else '?' end) + 'menuID=' 
        + convert(varchar(10), M.MenuID) end as MenuPath, 
    M.ParentMenu, M.IsRoot, M.IsEnabled, M.IsPageFeature, M.SortOrder, UR.UserId 
FROM UsersInRoles AS UR 
            INNER JOIN aspnet_Roles AS R ON UR.RoleId = R.RoleId 
            INNER JOIN ApplicationMenu AS M 
            INNER JOIN MenusInRoles AS MR ON M.MenuID = MR.MenuID ON R.RoleId = MR.RoleId 
        WHERE UR.UserId = @UserId AND M.IsPageFeature = 0 AND M.IsEnabled = 1 
        ORDER BY M.IsRoot desc, M.ParentMenu, M.SortOrder, M.MenuName";

        const string Query_Select_Menu_By_MenuID_UserID = @"SELECT M.MenuID
        FROM UsersInRoles AS UR 
            INNER JOIN aspnet_Roles AS R ON UR.RoleId = R.RoleId 
            INNER JOIN ApplicationMenu AS M 
            INNER JOIN MenusInRoles AS MR ON M.MenuID = MR.MenuID ON R.RoleId = MR.RoleId 
        WHERE UR.UserId = @UserId AND M.MenuID = @MenuID AND M.IsPageFeature = 0 AND M.IsEnabled = 1";

        const string Query_Select_PageFeature_Rights = @"SELECT DISTINCT M.MenuID, M.MenuName, M.MenuPath, M.ParentMenu, M.IsRoot, M.IsEnabled, M.IsPageFeature, M.SortOrder, UR.UserId, R.RoleName
        FROM         UsersInRoles AS UR INNER JOIN
                                aspnet_Roles AS R ON UR.RoleId = R.RoleId INNER JOIN
                                ApplicationMenu AS M INNER JOIN
                                MenusInRoles AS MR ON M.MenuID = MR.MenuID ON R.RoleId = MR.RoleId INNER JOIN
                                    (SELECT     MenuID, MenuName, MenuPath, ParentMenu, IsRoot, IsEnabled, IsPageFeature, SortOrder, CreatedBy, CreateDate, UpdatedBy, 
                                                            UpdateDate, CreatedByValue, UpdatedByValue
                                    FROM          ApplicationMenu AS M
                                    WHERE      (MenuPath LIKE '%' + @PageNameWithExt + '%')) AS M2 ON M2.MenuID = M.ParentMenu
        WHERE    (UR.CompanyID = @CompanyID) AND (UR.UserId = @UserID) AND (M.IsPageFeature = 1)
        ORDER BY M.IsRoot DESC, M.ParentMenu, M.SortOrder, M.MenuName";

        const string Query_Select_Menus_Also_AssignedToRole = @"SELECT 
		MR.RoleID,
		ApplicationMenu.[MenuID],
		[ParentMenu],
		[SortOrder],
		[IsRoot],
		[IsEnabled],
		[IsPageFeature],
		[MenuName],
		[MenuPath],
		ApplicationMenu.[CreatedByValue],
		ApplicationMenu.[UpdatedByValue],
		ApplicationMenu.[CreateDate],
		ApplicationMenu.[UpdateDate],
		ApplicationMenu.[CreatedBy],
		ApplicationMenu.[UpdatedBy] 
		FROM ApplicationMenu
        left join 
        (Select MenusInRoles.* from MenusInRoles 
        inner join aspnet_Roles on aspnet_Roles.RoleID = MenusInRoles.RoleID
        where LoweredRoleName = @RoleName)
         MR on ApplicationMenu.MenuID = MR.MenuID
        order by IsRoot desc, SortOrder";

        #endregion ConstantStrings

        #region Constructors

        public MenusInRoles()
            : base()
        { }
        #endregion Constructors

        #region Methods

        /// <summary>
        /// Build User Menus 
        /// </summary>
        public static DataTable GetMenus_By_CompanyID_UserID(Guid UserID)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserID", UserID);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_Menus, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetMenus_By_MenuID_UserID(int menuID, Guid userID)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@MenuID", menuID);
                sqlparams[1] = new SqlParameter("@UserID", userID);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_Menu_By_MenuID_UserID, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }


        /// <summary>
        /// Build All User Menus Also Assigned To Selected Role
        /// </summary>
        public static DataTable GetMenus_Also_AssignedToRole(String RoleName)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@RoleName", (RoleName != null ? RoleName.ToLower() : RoleName));
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_Menus_Also_AssignedToRole, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        /// <summary>
        /// Build User Menus 
        /// </summary>
        public static DataTable Get_PageFeatureRights(Guid UserID, string PageNameWithExt, int CompanyID)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@UserID", UserID);
                sqlparams[1] = new SqlParameter("@PageNameWithExt", PageNameWithExt);
                sqlparams[2] = new SqlParameter("@CompanyID", CompanyID);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_PageFeature_Rights, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
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

        public static DataTable GetDataByKey(int MenuID, Guid RoleId)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@MenuID", MenuID);
                sqlparams[1] = new SqlParameter("@RoleId", RoleId);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_PrimaryKey, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
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
        public static DataTable GetAllData(MenusInRoles Obj, ref int ReturnTotalRows, string OrderBy, Enumaration.QueryType querytype = Enumaration.QueryType.Equals, int? PageIndex = null, int? PageSize = null, Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc)
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

        public bool Insert(SqlTransaction Trans)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@MenuID", MenuID);
                sqlparams[1] = new SqlParameter("@RoleId", RoleId);
                sqlparams[2] = new SqlParameter("@CreatedBy", CreatedBy);
                sqlparams[3] = new SqlParameter("@UpdatedBy", UpdatedBy);
                sqlparams[4] = new SqlParameter("@CreateDate", CreateDate);
                sqlparams[5] = new SqlParameter("@UpdateDate", UpdateDate);
                sqlparams[6] = new SqlParameter("@CreatedByValue", CreatedByValue);
                sqlparams[7] = new SqlParameter("@UpdatedByValue", UpdatedByValue);
                if (SqlHelper.ExecuteNonQuery(Trans, CommandType.Text, Query_Insert, sqlparams) > 0)
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
                sqlparams[0] = new SqlParameter("@UpdatedBy", UpdatedBy);
                sqlparams[1] = new SqlParameter("@UpdateDate", UpdateDate);
                sqlparams[2] = new SqlParameter("@UpdatedByValue", UpdatedByValue);
                sqlparams[3] = new SqlParameter("@MenuID", MenuID);
                sqlparams[4] = new SqlParameter("@RoleId", RoleId);
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

        public static bool DeleteDataByKey(int MenuID, Guid RoleId)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@MenuID", MenuID);
                sqlparams[1] = new SqlParameter("@RoleId", RoleId);
                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_By_PrimaryKey, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return result;
        }

        public static bool DeleteDataByCompanyAndRole(Guid RoleId, SqlTransaction Trans)
        {
            bool result = false;
            try
            {
                int hasRows = GetCountByCompanyAndRole(RoleId);
                if (hasRows == 0)
                    return true;

                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@RoleId", RoleId);
                if (SqlHelper.ExecuteNonQuery(Trans, CommandType.Text, Query_Delete_Menus_By_Role_Company, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return result;
        }

        private static int GetCountByCompanyAndRole(Guid RoleId)
        {
            int result = 0;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@RoleId", RoleId);
                sqlparams[1] = new SqlParameter("@count", result);
                sqlparams[1].Direction = ParameterDirection.Output;
                SqlHelper.ExecuteNonQuery(CommandType.Text, Query_GetCount_By_Role_Company, sqlparams);
                result = int.Parse(sqlparams[1].Value.ToString());
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return result;
        }

        public static bool Update_MenusInRole(List<MenusInRoles> MenusToAddinRole, Guid RoleID)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("MenusInRole");
            try
            {
                if (!DeleteDataByCompanyAndRole(RoleID, sqltrans))
                    throw new Exception("");
                foreach (MenusInRoles m in MenusToAddinRole)
                {
                    if (!m.Insert(sqltrans))
                        throw new Exception("");
                }
                SqlHelper.CommitTransaction(sqltrans);
            }
            catch (Exception ex)
            {
                SqlHelper.RollBackTransaction(sqltrans, "MenusInRole");

                SqlHelper.LogException(ex);
                throw ex;
            }
            return result;
        }

        #endregion Methods

    }
}

