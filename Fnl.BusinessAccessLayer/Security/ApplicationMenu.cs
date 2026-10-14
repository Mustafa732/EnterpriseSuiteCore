using System;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Security
{
    public partial class ApplicationMenu
    {

        #region Constant Queries

        const string Query_Select_MenuPathByFormName = @"SELECT [MenuID],case MenuName when 'Dispatch' then MenuName + ' (UnApproved)' when 'Stock Transaction' then MenuName + ' (UnApproved)' when 'Stock GIN' then MenuName + ' (UnApproved)' when 'Stock Return' then MenuName + ' (UnApproved)' else MenuName end as MenuName , MenuPath FROM ApplicationMenu WHERE IsEnabled = 1 AND DocumentContainer = 1 AND MenuName = @MenuName 
ORDER BY MenuName";

        const string Query_Delete_By_PrimaryKey = @" DELETE FROM ApplicationMenu WHERE [MenuID] = @MenuID; ";

        const string Query_Update_ParentMenus_ToRoot = @" UPDATE ApplicationMenu SET ParentMenu = null, IsRoot = 1 WHERE MenuID IN {0}; ";

        const string Query_Update_ParentMenus = @" UPDATE ApplicationMenu SET ParentMenu = @ParentMenu, IsRoot = 0 WHERE MenuID IN {0}; ";

        const string Query_Select_By_MenuPath = @" SELECT [MenuID] FROM ApplicationMenu WHERE MenuPath = @MenuPath; ";

        //        const string Query_Select_DocumentContainer_Menus = @" SELECT [MenuID], MenuName FROM ApplicationMenu WHERE IsEnabled = 1 AND DocumentContainer = 1 
        //ORDER BY MenuName ";

        const string Query_Select_DocumentContainer_Menus = @"SELECT [MenuID],case MenuName when 'Dispatch' then MenuName + ' (UnApproved)' when 'Stock Transaction' then MenuName + ' (UnApproved)' when 'Stock GIN' then MenuName + ' (UnApproved)' when 'Stock Return' then MenuName + ' (UnApproved)' else MenuName end as MenuName FROM ApplicationMenu WHERE IsEnabled = 1 AND DocumentContainer = 1 
ORDER BY MenuName";

        const string Query_SelectMenuToRole = @" SELECT ar.RoleName, am1.MenuName,--(CASE WHEN am1.MenuPath = '#' Then ' ' ELSE am1.MenuPath END) AS MenuPath,
                                                ISNULL (am2.MenuName, ' ') AS ParentMenuName
                                                ,ISNULL (am3.MenuName, ' ') AS ChildMenuName,
                                                (CASE WHEN am3.MenuPath = '#' THEN '' ELSE am4.MenuName END) as ButtonsName
                                                ,(CASE WHEN am3.MenuPath = '#' THEN am4.MenuName ELSE '' END) as ReportsName
                                                FROM aspnet_Roles ar

                                                INNER JOIN MenusInRoles mir
                                                ON mir.RoleID = ar.RoleId
                                                INNER JOIN ApplicationMenu am1
                                                ON am1.MenuID = mir.MenuID
                                                LEFT JOIN ApplicationMenu am2
                                                ON am1.ParentMenu = am2.MenuID

                                                LEFT JOIN ApplicationMenu am3
                                                ON am1.MenuID = am3.ParentMenu

                                                LEFT JOIN ApplicationMenu am4
                                                ON am3.MenuID = am4.ParentMenu

                                                WHERE am1.IsEnabled = 1 
                                                AND am1.IsPageFeature = 0 
                                                AND am3.IsPageFeature = 0
                                                ORDER BY am1.SortOrder, am1.MenuName ";

        const string Query_SelectMenuToRoleByRoleName = @" SELECT 
	OperatingUnitName,
	RoleName,
	UserName,
	MenuID,
	MenuName,
	ParentMenuName,
	ChildMenuName,
	ThirdMenu,
	ReportsName,
	ButtonsName,
	isRoot,
	isEnabled,
	isPageFeature,
	SortOrder,
	UserID
FROM 
(

SELECT DISTINCT 
 SET_OperatingUnit.OperatingUnitName ,R.RoleName,Usr.UserName,  M.MenuID, M.MenuName,
    --M.ParentMenu,
    
    
    FirstCh.MenuName AS ParentMenuName,
    SecondCh.MenuName AS ChildMenuName,
    ThirdCh.MenuName AS ThirdMenu,
	CASE WHEN M.MenuName like '%Reports%'
		THEN ISNULL(ThirdCh.MenuName,ISNULL(SecondCh.MenuName,FirstCh.MenuName))
		ELSE NULL 
	END as ReportsName,

	CASE WHEN ISNULL(ThirdCh.IsPageFeature, SecondCh.IsPageFeature) = 1
		THEN ISNULL(ThirdCh.MenuName, SecondCh.MenuName)
		ELSE NULL
	END as ButtonsName,


     M.IsRoot, M.IsEnabled, M.IsPageFeature, M.SortOrder, UR.UserId 
FROM UsersInRoles AS UR 
            
            INNER JOIN aspnet_Roles AS R ON UR.RoleId = R.RoleId 
            INNER JOIN ApplicationMenu AS M 
            INNER JOIN MenusInRoles AS MR ON M.MenuID = MR.MenuID ON R.RoleId = MR.RoleId 
            INNER JOIN aspnet_Users AS Usr ON Usr.UserId = UR.UserId
			INNER JOIN SET_Company ON SET_Company.CompanyId = UR.CompanyID
			INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId
            
            left JOIN (
            SELECT  M.MenuID,  M.ParentMenu, M.MenuName,UR.UserId, M.IsPageFeature, SET_OperatingUnit. OperatingUnitId, SET_OperatingUnit.OperatingUnitName
            FROM 
            UsersInRoles AS UR 
            INNER JOIN aspnet_Roles AS R ON UR.RoleId = R.RoleId 
            INNER JOIN ApplicationMenu AS M 
            INNER JOIN MenusInRoles AS MR ON M.MenuID = MR.MenuID ON R.RoleId = MR.RoleId 
			INNER JOIN SET_Company ON SET_Company.CompanyId = UR.CompanyID
			INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId
            WHERE M.IsPageFeature = 0 AND M.IsEnabled = 1
            ) AS FirstCh ON FirstCh.ParentMenu =  M.MenuID AND SET_OperatingUnit.OperatingUnitId = FirstCh.OperatingUnitId
            AND FirstCh.UserId = UR.UserId
            left JOIN (
            SELECT  M.MenuID,  M.ParentMenu, M.MenuName,UR.UserId, M.IsPageFeature, SET_OperatingUnit. OperatingUnitId, SET_OperatingUnit.OperatingUnitName
            FROM 
            UsersInRoles AS UR 
            INNER JOIN aspnet_Roles AS R ON UR.RoleId = R.RoleId 
            INNER JOIN ApplicationMenu AS M 
            INNER JOIN MenusInRoles AS MR ON M.MenuID = MR.MenuID ON R.RoleId = MR.RoleId 
			INNER JOIN SET_Company ON SET_Company.CompanyId = UR.CompanyID
			INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId
            WHERE M.IsEnabled = 1
            --AND M.IsPageFeature = 0 --commit for showing buttons
            ) AS SecondCh ON SecondCh.ParentMenu =  FirstCh.MenuID AND SET_OperatingUnit.OperatingUnitId = SecondCh.OperatingUnitId
            AND SecondCh.UserId = UR.UserId
            left JOIN (
            SELECT  M.MenuID,  M.ParentMenu, M.MenuName,UR.UserId, M.IsPageFeature, SET_OperatingUnit. OperatingUnitId, SET_OperatingUnit.OperatingUnitName
            FROM 
            UsersInRoles AS UR 
            INNER JOIN aspnet_Roles AS R ON UR.RoleId = R.RoleId 
            INNER JOIN ApplicationMenu AS M 
            INNER JOIN MenusInRoles AS MR ON M.MenuID = MR.MenuID ON R.RoleId = MR.RoleId 
			INNER JOIN SET_Company ON SET_Company.CompanyId = UR.CompanyID
			INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId
            WHERE M.IsEnabled = 1
            --AND M.IsPageFeature = 0 --commit for showing buttons
            ) AS ThirdCh ON ThirdCh.ParentMenu =  SecondCh.MenuID AND SET_OperatingUnit.OperatingUnitId = ThirdCh.OperatingUnitId
            AND ThirdCh.UserId = UR.UserId
        WHERE M.IsEnabled = 1 
        AND M.IsRoot = 1
        AND M.IsPageFeature = 0 
        AND UR.CompanyID = @CompanyID
        
		{0}

		) TAB
        
        ORDER BY IsRoot desc,  SortOrder, MenuName";

        const string Query_SelectRolesForDashboard = @"Select ApplicationMenu.MenuName
From ApplicationMenu
Inner Join MenusInRoles ON (MenusInRoles.MenuID = ApplicationMenu.MenuID)
Inner Join aspnet_roles ON (aspnet_roles.RoleId = MenusInRoles.RoleId)
Inner Join UsersInRoles ON (UsersInRoles.RoleId = aspnet_roles.RoleId)
Inner Join aspnet_Users ON (aspnet_Users.UserID = UsersInRoles.UserId)
Where ApplicationMenu.IsRoot = 1 and aspnet_Users.UserName = @UserName 
Order By aspnet_Roles.RoleName
";

        const string Query_Select_AllDocument_Menus=@"SELECT [MenuID], MenuName
  FROM ApplicationMenu WHERE IsEnabled = 1 AND DocumentContainer = 1 
ORDER BY MenuName";

        const string Query_Select_Root_Menus = @"
    select * from ApplicationMenu ParentMenu where IsRoot = 1
";

        const string Query_Select_Parent_Menus = @"
    select ParentMenu.* from ApplicationMenu ParentMenu
	INNER JOIN ApplicationMenu Menu ON Menu.MenuID = ParentMenu.ParentMenu AND Menu.ParentMenu is NULL AND Menu.IsRoot = 1
	Order BY ParentMenu.MenuName
";

        #endregion

        #region Class Methods

        public static DataTable GetSelectRolesForDashboard(string UserName, int companyid, SqlConnection sqlcon = null)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@UserName", UserName);
                sqlparams[1] = new SqlParameter("@CompanyID", companyid);
                if (sqlcon == null)
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectRolesForDashboard, sqlparams).Tables[0];
                }
                else
                    dt = SqlHelper.ExecuteDataset(sqlcon, CommandType.Text, Query_SelectRolesForDashboard, sqlparams).Tables[0];
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, ControlPaging.ApplyPagingQueryWithCount(Query_SelectAll), sqlparams).Tables[0];
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

        public static DataTable GetDataByMenuPath(string menuPath)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@MenuPath", menuPath);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_MenuPath, sqlparams);

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

        public static DataTable GetDocumentContainerMenus()
        {
            DataTable dtToReturn = null;

            try
            {
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_DocumentContainer_Menus);

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

        public static DataTable GetRootMenus()
        {
            DataTable dtToReturn = null;

            try
            {
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_Root_Menus);

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

        public static DataTable GetParentMenus()
        {
            DataTable dtToReturn = null;

            try
            {
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_Parent_Menus);

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

        public static bool Update_ParentMenus_ToRoot_By_MenuID(string menus)
        {
            bool result = false;

            try
            {
                if (SqlHelper.ExecuteNonQuery(CommandType.Text, String.Format(FilterInputs.FilterText(Query_Update_ParentMenus_ToRoot), menus)) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static bool Update_ParentMenus_By_MenuID(int parentMenu, string menus)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@ParentMenu", parentMenu);

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, String.Format(FilterInputs.FilterText(Query_Update_ParentMenus), menus), sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static bool DeleteDataByKey(int menuID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@MenuID", menuID);

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

        public static DataTable GetAllActiveMenuToRole()
        {
            DataTable dt = null;
            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectMenuToRole).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetRoleForUserForRadioButtons(string MenuPath, string UserID, string query)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@MenuPath", MenuPath);
                sqlparams[1] = new SqlParameter("@UserID", UserID);



                dt = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetAllActiveMenuToRoleByRoleName(string RoleName, int CompanyID, string userName = null, string menuName = null, string parentMenuName = null)
        {
            DataTable dt = null;
            string filter = string.Empty;
            try
            {
                if (RoleName != string.Empty)
                    filter += " AND R.RoleName like '%" + RoleName + "%'";

                if(!string.IsNullOrEmpty(userName))
                    filter += " AND Usr.UserName like '%" + userName + "%'";

                if (menuName != string.Empty)
                    filter += " AND M.MenuName = '" + menuName + "'";

                if (parentMenuName != string.Empty)
                    filter += " AND FirstCh.MenuName = '" + parentMenuName + "'";

              
                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectMenuToRoleByRoleName.Replace("@CompanyID", CompanyID.ToString()), filter)).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }
        public static DataTable GetMenuPathByFormName(string FormName)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@MenuName", FormName);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_MenuPathByFormName, sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetAllDocumentMenus()
        {
            DataTable dtToReturn = null;

            try
            {
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_AllDocument_Menus);

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

        public static DataTable getMenuForBox(SqlConnection sqlcon = null)
        {
            DataTable dt = null;

            const string Query_SelectRolesForDashboard = @"
SELECT MenuID, MenuName 
                                 FROM ApplicationMenu 
                                 WHERE (IsRoot = 1 OR ParentMenu IS NULL) 
                                   AND IsEnabled = 1 
                                 ORDER BY SortOrder ASC";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@ParentMenu ", 0);
                if (sqlcon == null)
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectRolesForDashboard, sqlparams).Tables[0];
                }
                else
                    dt = SqlHelper.ExecuteDataset(sqlcon, CommandType.Text, Query_SelectRolesForDashboard, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable getMenuForBoxbyID(int parentMenuId, SqlConnection sqlcon = null)
        {
            DataTable dt = null;

            const string Query_SelectRolesForDashboard = @"SELECT MenuID, MenuName, MenuPath 
                                 FROM ApplicationMenu  
                                 WHERE ParentMenu = @ParentMenu 
                                   AND IsEnabled = 1 
                                 ORDER BY SortOrder ASC";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@ParentMenu ", parentMenuId);
                if (sqlcon == null)
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectRolesForDashboard, sqlparams).Tables[0];
                }
                else
                    dt = SqlHelper.ExecuteDataset(sqlcon, CommandType.Text, Query_SelectRolesForDashboard, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }


        #endregion
    }
}

