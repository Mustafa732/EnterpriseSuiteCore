using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Security
{
    public class MenusInUsers : BaseObject
    {
        #region Fields

        private int f_GeneralMenuID;        
        
        private Guid f_UserId;
        private Guid f_CreatedBy;
        private Guid f_UpdatedBy;

        private bool f_Status;
        
        private DateTime f_CreateDate;
        private DateTime f_UpdateDate;
        
        private string f_CreatedByValue;
        private string f_UpdatedByValue;

        #endregion Fields

        #region Properties

        public int GeneralMenuID
        {
            get { return this.f_GeneralMenuID; }
            set { this.f_GeneralMenuID = value; }
        }

        public Guid UserId
        {
            get { return this.f_UserId; }
            set { this.f_UserId = value; }
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
        
        public bool Status
        {
            get{ return this.f_Status; }
            set{ this.f_Status = value; }
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
        
        public bool Status_S{ get; set; }

        public DateTime? CreateDate_S { get; set; }
        public DateTime? UpdateDate_S { get; set; }
        
        public string CreatedByValue_S { get; set; }
        public string UpdatedByValue_S { get; set; }

        #endregion AutomaticPropertiesForSearching

        #region ConstantStrings

        const string Query_SelectAll = @" SELECT 
		[GeneralMenuID],		
		[UserId],
		[CreatedBy],
		[UpdatedBy],
		[CreateDate],
		[UpdateDate],
		[CreatedByValue],
		[UpdatedByValue], 
        [Status] 
		FROM MenusInUsers";

        const string Query_Select_By_PrimaryKey = @" SELECT 
		[GeneralMenuID],
		[UserId],
		[CreatedBy],
		[UpdatedBy],
		[CreateDate],
		[UpdateDate],
		[CreatedByValue],
		[UpdatedByValue],
        [Status]
		FROM MenusInUsers WHERE [GeneralMenuID] = @GeneralMenuID and [UserId] = @UserId";

        const string Query_Insert = @"
        INSERT INTO MenusInUsers( 
        [GeneralMenuID],
        [UserId],
		[CreatedBy],
		[UpdatedBy],
		[CreateDate],
		[UpdateDate],
		[CreatedByValue],
		[UpdatedByValue],
        [Status]) 
		Values 
		(
        @GeneralMenuID,
        @UserId,
        @CreatedBy,
		@UpdatedBy,
		@CreateDate,
		@UpdateDate,
		@CreatedByValue,
		@UpdatedByValue,
        @Status
		) ";

        const string Query_Update_By_PrimaryKey = @" UPDATE MenusInUsers SET 
		[UpdatedBy] = @UpdatedBy,
		[UpdateDate] = @UpdateDate,
		[UpdatedByValue] = @UpdatedByValue 
		WHERE [GeneralMenuID] = @GeneralMenuID,[CompanyID] = @CompanyID,[UserId] = @UserId";

        const string Query_Delete_By_PrimaryKey = @" Update MenusInUsers Set Status = 0, UpdatedBy = @UpdatedBy, UpdateDate = @UpdateDate, 
            UpdatedByValue = @UpdatedByValue 
		WHERE [GeneralMenuID] = @GeneralMenuID, [UserId] = @UserId";

        const string Query_Select_Field = @" Select {0} [{1}] from MenusInUsers where [{2}] like '%' + @Value + '%'";

        const string Query_Select_Menus = @"SELECT Distinct mu.GeneralMenuID, gm.GeneralMenuName, 
            case gm.GeneralMenuPath when '#' then gm.GeneralMenuPath else gm.GeneralMenuPath + (case when CharIndex('?', gm.GeneralMenuPath) > 0 then '&' else '?' end) + 'menuID=' + convert(varchar(4), gm.GeneralMenuID) end as GeneralMenuPath, 
            gm.ParentMenu, gm.IsRoot, gm.IsEnabled, gm.IsPageFeature, gm.SortOrder, mu.UserId
        FROM MenusInUsers AS mu INNER JOIN
            ApplicationGeneralMenu AS gm ON mu.GeneralMenuID = gm.GeneralMenuID 
        WHERE mu.UserId = @UserId AND gm.IsPageFeature = 0 AND gm.IsEnabled = 1 
        ORDER BY gm.IsRoot desc, gm.ParentMenu, gm.SortOrder, gm.GeneralMenuName";

        const string Query_Select_Menus_By_RoleName = @" 
SELECT DISTINCT ApplicationGeneralMenu.IsRoot, ApplicationGeneralMenu.GeneralMenuID, 
    case ApplicationGeneralMenu.GeneralMenuPath when '#' then ApplicationGeneralMenu.GeneralMenuPath else ApplicationGeneralMenu.GeneralMenuPath 
        + (case when CharIndex('?', ApplicationGeneralMenu.GeneralMenuPath) > 0 then '&' else '?' end) + 'menuID=' + convert(varchar(4), 
        ApplicationGeneralMenu.GeneralMenuID) end as GeneralMenuPath, 
    ApplicationGeneralMenu.GeneralMenuName, ApplicationGeneralMenu.ParentMenu, ApplicationGeneralMenu.SortOrder 
FROM ApplicationGeneralMenu 
	LEFT JOIN MenusInUsers ON ApplicationGeneralMenu.GeneralMenuID = MenusInUsers.GeneralMenuID 
	CROSS JOIN aspnet_UsersInRoles 
	INNER JOIN aspnet_Roles ON aspnet_Roles.RoleId = aspnet_UsersInRoles.RoleID 
WHERE aspnet_UsersInRoles.UserID = @UserID AND aspnet_Roles.RoleName in ('admin', 'cisadmin') AND ApplicationGeneralMenu.IsPageFeature = 0 
    AND ApplicationGeneralMenu.IsEnabled = 1 
ORDER BY ApplicationGeneralMenu.IsRoot desc, ApplicationGeneralMenu.SortOrder, ApplicationGeneralMenu.ParentMenu, ApplicationGeneralMenu.GeneralMenuName ";

        const string Query_Select_MenusByMenuID = @" SELECT Distinct mu.GeneralMenuID, gm.GeneralMenuName, 
                case gm.GeneralMenuPath when '#' then gm.GeneralMenuPath else gm.GeneralMenuPath + (case when CharIndex('?', gm.GeneralMenuPath) > 0 then '&' else '?' end) + 'menuID=' + convert(varchar(4), gm.GeneralMenuID) end as GeneralMenuPath, 
                gm.ParentMenu, gm.IsRoot, gm.IsEnabled, gm.IsPageFeature, gm.SortOrder, mu.UserId, u.UserName, r.RoleName 
            FROM MenusInUsers AS mu 
                INNER JOIN ApplicationGeneralMenu AS gm ON mu.GeneralMenuID = gm.GeneralMenuID 
                INNER JOIN aspnet_Users AS u ON u.UserId = mu.UserId 
                INNER JOIN aspnet_UsersInRoles as ur ON ur.UserId = u.UserId 
	            INNER JOIN aspnet_Roles as r ON r.RoleId = ur.RoleId 
            WHERE mu.UserId = @UserId AND gm.IsPageFeature = @IsPageFeature AND gm.ParentMenu = @MenuID 
            ORDER BY gm.IsRoot desc, gm.ParentMenu, gm.SortOrder, gm.GeneralMenuName ";

        #endregion ConstantStrings

        #region Constructors

        public MenusInUsers()
            : base()
        { }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Build User Menus 
        /// </summary>
        public static DataTable GetMenus_By_UserID(Guid UserID)
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

        public static DataTable GetMenus_By_RoleName(Guid UserID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserID", UserID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_Menus_By_RoleName, sqlparams);

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

        /// <summary>
        /// Build User Menus 
        /// </summary>
        public static DataTable GetMenus_By_UserID(Guid userID, bool isPageFeature, int parentMenuID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@UserID", userID);
                sqlparams[1] = new SqlParameter("@IsPageFeature", isPageFeature);
                sqlparams[2] = new SqlParameter("@MenuID", parentMenuID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_MenusByMenuID, sqlparams);

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


        public static DataTable GetDataByKey(int menuID,  Guid userID)
        {
            DataTable dt = null;
            
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@GeneralMenuID", menuID);                
                sqlparams[1] = new SqlParameter("@UserId", userID);
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
                sqlparams[0] = new SqlParameter("@GeneralMenuID", f_GeneralMenuID);
                sqlparams[1] = new SqlParameter("@UserId", f_UserId);                
                sqlparams[2] = new SqlParameter("@CreatedBy", f_CreatedBy);
                sqlparams[3] = new SqlParameter("@UpdatedBy", f_UpdatedBy);
                sqlparams[4] = new SqlParameter("@CreateDate", f_CreateDate);
                sqlparams[5] = new SqlParameter("@UpdateDate", f_UpdateDate);
                sqlparams[6] = new SqlParameter("@CreatedByValue", f_CreatedByValue);
                sqlparams[7] = new SqlParameter("@UpdatedByValue", f_UpdatedByValue);
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
                sqlparams[3] = new SqlParameter("@GeneralMenuID", f_GeneralMenuID);
                sqlparams[4] = new SqlParameter("@UserId", f_UserId);
            
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

        public static bool DeleteDataByKey(int menuID, Guid userID)
        {
            bool result = false;
            
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@GeneralMenuID", menuID);                
                sqlparams[1] = new SqlParameter("@UserId", userID);
            
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

        #endregion Methods
    }
}

