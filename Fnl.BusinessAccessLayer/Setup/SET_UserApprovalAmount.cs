using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Security;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_UserApprovalAmount
    {
        #region Constant Queries

        const string Query_GetDataByKey = @"
SELECT SET_UserApprovalAmount.ApprovalAuthorityID
      ,SET_UserApprovalAmount.ApprovalAmountID
      ,SET_UserApprovalAmount.DocumentTypeID
      ,SET_DocumentType.DocumentType
      ,SET_UserApprovalAmount.UserID
      ,aspnet_Users.UserName
      ,SET_UserApprovalAmount.RoleID
      ,aspnet_Roles.RoleName
      ,SET_UserApprovalAmount.EffectiveDateFrom
      ,SET_UserApprovalAmount.EffectiveDateTo
      ,SET_UserApprovalAmount.AmountFrom
      ,SET_UserApprovalAmount.AmountTo
      ,SET_UserApprovalAmount.ApprovalOperator
      ,SET_UserApprovalAmount.ApprovalItemGroup
      ,SET_ApprovalItemGroup.GroupName as ApprovalItemGroupName
      ,SET_UserApprovalAmount.ApprovalItemGroup
      ,ISNULL(SET_UserApprovalAmount.EditPermission,0)EditPermission
      ,'' AS [Action]
FROM SET_UserApprovalAmount
LEFT JOIN aspnet_Users ON aspnet_Users.UserId = SET_UserApprovalAmount.UserID
LEFT JOIN aspnet_Roles ON aspnet_Roles.RoleId = SET_UserApprovalAmount.RoleID
LEFT JOIN SET_DocumentType ON SET_DocumentType.DocumentTypeID = SET_UserApprovalAmount.DocumentTypeID
	AND SET_DocumentType.CompanyID = SET_UserApprovalAmount.CompanyID AND 
	SET_DocumentType.PeriodID = SET_UserApprovalAmount.PeriodID AND SET_DocumentType.Status = 1
LEFT JOIN SET_ApprovalItemGroup ON SET_ApprovalItemGroup.ApprovalGroupId = SET_UserApprovalAmount.ApprovalItemGroup
 
WHERE SET_UserApprovalAmount.ApprovalAuthorityID = @ApprovalAuthorityID AND SET_UserApprovalAmount.CompanyID = @CompanyID AND 
	SET_UserApprovalAmount.PeriodID = @PeriodID AND SET_UserApprovalAmount.Status = 1 ";

        const string Query_CheckAuthorityExist = @"
IF @EffectiveDateTo IS NULL
	BEGIN 
		SELECT COUNT(ApprovalAmountID)
		FROM SET_UserApprovalAmount
		WHERE (DocumentTypeID = @DocumentTypeID OR @DocumentTypeID IS NULL) AND
			(UserID = @UserID OR @UserID IS NULL) AND 
			(RoleID = @RoleID OR @RoleID IS NULL) AND 
			(EffectiveDateFrom >= @EffectiveDateFrom) AND
			CompanyID = @CompanyID AND PeriodID = @PeriodID AND Status = 1 {AdditionalParameters}
	END
ELSE 
	BEGIN 
		SELECT COUNT(ApprovalAmountID)
		FROM SET_UserApprovalAmount
		WHERE (DocumentTypeID = @DocumentTypeID OR @DocumentTypeID IS NULL) AND
			(UserID = @UserID OR @UserID IS NULL) AND 
			(RoleID = @RoleID OR @RoleID IS NULL) AND 
			((EffectiveDateFrom BETWEEN @EffectiveDateFrom AND @EffectiveDateTo) OR (EffectiveDateTo BETWEEN @EffectiveDateFrom AND @EffectiveDateTo)) AND
			CompanyID = @CompanyID AND PeriodID = @PeriodID AND Status = 1 {AdditionalParameters}
	END ";

        const string Query_GetData_byKeys_WithoutANDOperator = @"
SELECT SET_UserApprovalAmount.* 
FROM SET_UserApprovalAmount 
    INNER JOIN SET_ApprovalAuthority ON SET_ApprovalAuthority.ApprovalAuthorityID = SET_UserApprovalAmount.ApprovalAuthorityID 
        AND SET_ApprovalAuthority.CompanyID = SET_UserApprovalAmount.CompanyID AND SET_ApprovalAuthority.PeriodID = SET_UserApprovalAmount.PeriodID 
        AND SET_ApprovalAuthority.Status = 1 
WHERE SET_UserApprovalAmount.CompanyID = @CompanyID AND SET_UserApprovalAmount.PeriodID = @PeriodID AND SET_UserApprovalAmount.Status = 1 
    AND (SET_UserApprovalAmount.DocumentTypeID = @DocumentTypeID OR SET_UserApprovalAmount.DocumentTypeID IS NULL) 
    --AND (SET_UserApprovalAmount.ApprovalItemGroup = @ApprovalItemGroupID OR SET_UserApprovalAmount.ApprovalItemGroup = -1) AND SET_ApprovalAuthority.UserID = @UserID 
AND (SET_UserApprovalAmount.ApprovalItemGroup = @ApprovalItemGroupID OR SET_UserApprovalAmount.ApprovalItemGroup is Null) AND SET_ApprovalAuthority.UserID = @UserID    
AND (SET_UserApprovalAmount.ApprovalOperator IS NULL OR SET_UserApprovalAmount.ApprovalOperator = 'OR') ";

        const string Query_GetData_byKeys_ApprovalAuthority = @"
SELECT COUNT(ApprovalAmountID)
FROM SET_UserApprovalAmount 
inner join UsersInRoles on UsersInRoles.RoleId = SET_UserApprovalAmount.RoleId
and UsersInRoles.CompanyID = SET_UserApprovalAmount.CompanyID
--user not mandatory in this case 
--and UsersInRoles.UserId = SET_UserApprovalAmount.UserID
and (UsersInRoles.UserId = SET_UserApprovalAmount.UserID or SET_UserApprovalAmount.UserID is null)


------------------------------NEW WORKING (ROLE Wise)------------------------------------------

inner join SET_DocumentType on SET_DocumentType.DocumentTypeID =  @DocumentTypeID
And SET_DocumentType.COmpanyId = SET_UserApprovalAmount.CompanyID

	inner join ApplicationMenu on ApplicationMenu.MenuID = SET_DocumentType.MenuID
		inner join MenusInRoles on MenusInRoles.MenuID = SET_DocumentType.MenuID
		ANd MenusInRoles.RoleID = SET_UserApprovalAmount.RoleID

------------------------------------------------------------------------

WHERE (SET_UserApprovalAmount.DocumentTypeID IS NULL OR SET_UserApprovalAmount.DocumentTypeID = @DocumentTypeID)
    AND (ApprovalItemGroup IS NULL OR ApprovalItemGroup = @ApprovalItemGroupID)
	AND (1 = (CASE WHEN EffectiveDateTo IS NULL AND EffectiveDateFrom <= @DocumentDate THEN 1
				   WHEN EffectiveDateTo IS NOT NULL AND (@DocumentDate BETWEEN EffectiveDateFrom AND EffectiveDateTo) THEN 1
				   ELSE 0 END))
	AND (1 = (CASE WHEN AmountTo IS NULL AND AmountFrom <= @Amount THEN 1
				   WHEN AmountTo IS NOT NULL AND (@Amount BETWEEN AmountFrom AND AmountTo) THEN 1
				   ELSE 0 END))
	AND SET_UserApprovalAmount.CompanyID = @CompanyID AND SET_UserApprovalAmount.PeriodID = @PeriodID AND SET_UserApprovalAmount.Status = 1 
    --and (SET_UserApprovalAmount.UserID = @UserID OR SET_UserApprovalAmount.UserID is null)
    and (UsersInRoles.UserID = @UserID OR UsersInRoles.UserID is null)";

        const string Query_GetData_CurrentUserExits = @"select count(ApprovalAuthorityID) from SET_ApprovalAuthority
where UserId = @UserID and CompanyID = @CompanyID and PeriodID = @PeriodID and Status = 1";



        const string Query_GetData_byKeys_EditAuthorityExist = @"SELECT SET_UserApprovalAmount.ApprovalAuthorityID
      ,SET_UserApprovalAmount.ApprovalAmountID
      ,SET_UserApprovalAmount.DocumentTypeID
      ,SET_DocumentType.DocumentType
      ,SET_ApprovalAuthority.UserID
      ,SET_UserApprovalAmount.UserID
      ,aspnet_Users.UserName
      ,SET_UserApprovalAmount.RoleID
      ,aspnet_Roles.RoleName
      ,SET_UserApprovalAmount.EffectiveDateFrom
      ,SET_UserApprovalAmount.EffectiveDateTo
      ,SET_UserApprovalAmount.AmountFrom
      ,SET_UserApprovalAmount.AmountTo
      ,SET_UserApprovalAmount.ApprovalOperator
      ,SET_UserApprovalAmount.ApprovalItemGroup
      ,SET_ApprovalItemGroup.GroupName as ApprovalItemGroupName
      ,SET_UserApprovalAmount.ApprovalItemGroup
      ,ISNULL(SET_UserApprovalAmount.EditPermission,0)EditPermission
      ,'' AS [Action]
FROM SET_UserApprovalAmount

INNER JOIN SET_ApprovalAuthority on SET_UserApprovalAmount.ApprovalAuthorityID = SET_ApprovalAuthority.ApprovalAuthorityID AND SET_ApprovalAuthority.Status = 1
LEFT JOIN aspnet_Users ON aspnet_Users.UserId = SET_UserApprovalAmount.UserID
LEFT JOIN aspnet_Roles ON aspnet_Roles.RoleId = SET_UserApprovalAmount.RoleID
LEFT JOIN SET_DocumentType ON SET_DocumentType.DocumentTypeID = SET_UserApprovalAmount.DocumentTypeID
	AND SET_DocumentType.CompanyID = SET_UserApprovalAmount.CompanyID AND 
	SET_DocumentType.PeriodID = SET_UserApprovalAmount.PeriodID AND SET_DocumentType.Status = 1
LEFT JOIN SET_ApprovalItemGroup ON SET_ApprovalItemGroup.ApprovalGroupId = SET_UserApprovalAmount.ApprovalItemGroup
 
WHERE SET_ApprovalAuthority.UserID = @UserID AND SET_UserApprovalAmount.CompanyID = @CompanyID AND 
	SET_UserApprovalAmount.PeriodID = @PeriodID AND SET_UserApprovalAmount.Status = 1 and SET_UserApprovalAmount.EditPermission = 1 
";

        #endregion

        #region Class Methods

        public static DataTable GetDataByKeyWithOtherTables(int approvalAuthorityID, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@ApprovalAuthorityID", approvalAuthorityID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByKey, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static bool IsAuthorityExist(int companyID, int periodID, Nullable<int> documentTypeID, Nullable<Guid> userID, Nullable<Guid> roleID, 
            DateTime effectiveDateFrom, Nullable<DateTime> effectiveDateTo, string strWhere)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (documentTypeID.HasValue)
                    sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                else
                    sqlparams[2] = new SqlParameter("@DocumentTypeID", DBNull.Value);

                if (userID.HasValue)
                    sqlparams[3] = new SqlParameter("@UserID", userID);
                else
                    sqlparams[3] = new SqlParameter("@UserID", DBNull.Value);

                if (roleID.HasValue)
                    sqlparams[4] = new SqlParameter("@RoleID", roleID);
                else
                    sqlparams[4] = new SqlParameter("@RoleID", DBNull.Value);

                sqlparams[5] = new SqlParameter("@EffectiveDateFrom", effectiveDateFrom);

                if (effectiveDateTo.HasValue)
                    sqlparams[6] = new SqlParameter("@EffectiveDateTo", effectiveDateTo);
                else
                    sqlparams[6] = new SqlParameter("@EffectiveDateTo", DBNull.Value);

                string query = Query_CheckAuthorityExist.Replace("{AdditionalParameters}", strWhere);
                if (Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, query, sqlparams)) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataTable GetDataByKeysWithoutANDOperator(int companyID, int periodID, int documentTypeID, Guid userID, long approvalItemGroupID = -1, 
            SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[3] = new SqlParameter("@UserID", userID);
                sqlparams[4] = new SqlParameter("@ApprovalItemGroupID", approvalItemGroupID);

                DataSet ds = null;

                if(objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byKeys_WithoutANDOperator, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byKeys_WithoutANDOperator, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static bool IsApprovalAuthorityExist(int companyID, int periodID, Nullable<int> documentTypeID, DateTime documentDate, decimal amount,
            long approvalItemGroupID = -1, string userID = null, SqlConnection objConnection = null, SqlTransaction obj_SQLTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (documentTypeID.HasValue)
                    sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                else
                    sqlparams[2] = new SqlParameter("@DocumentTypeID", DBNull.Value);

                sqlparams[3] = new SqlParameter("@DocumentDate", documentDate);
               
                sqlparams[4] = new SqlParameter("@Amount", Math.Abs(amount));
                
                sqlparams[5] = new SqlParameter("@ApprovalItemGroupID", approvalItemGroupID);
                sqlparams[6] = new SqlParameter("@UserID", userID);

                if (obj_SQLTransaction != null)
                {
                    if (Convert.ToInt32(SqlHelper.ExecuteScalar(obj_SQLTransaction, CommandType.Text, Query_GetData_byKeys_ApprovalAuthority, sqlparams)) > 0)
                        result = true;
                }
                else if (objConnection != null)
                {
                    if (Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_GetData_byKeys_ApprovalAuthority, sqlparams)) > 0)
                        result = true;
                }
                else
                {
                    if (Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetData_byKeys_ApprovalAuthority, sqlparams)) > 0)
                        result = true;
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataTable IsEditAuthorityExist(int companyID, int periodID, Nullable<int> documentTypeID, DateTime documentDate, string userID = null)
        {
            DataTable dtToReturn = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (documentTypeID.HasValue)
                    sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                else
                    sqlparams[2] = new SqlParameter("@DocumentTypeID", DBNull.Value);

                sqlparams[3] = new SqlParameter("@DocumentDate", documentDate);
                //sqlparams[4] = new SqlParameter("@Amount", amount);
                //sqlparams[5] = new SqlParameter("@ApprovalItemGroupID", approvalItemGroupID);
                sqlparams[4] = new SqlParameter("@UserID", userID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byKeys_EditAuthorityExist, sqlparams);

                 if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                       dtToReturn = ds.Tables[0];
                
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }


        public static bool CurrentUserExist(int companyID, int periodID, string userID = null, SqlConnection objConnection = null)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@UserID", userID);

                if (objConnection != null)
                {
                    if (Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_GetData_CurrentUserExits, sqlparams)) > 0)
                        result = true;
                }
                else
                {
                    if (Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetData_CurrentUserExits, sqlparams)) > 0)
                        result = true;
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }


        public static DataTable GetApprovalsForGRN(int companyID, int periodID, Guid userID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@UserID", userID);
                

                DataSet ds = null;


                #region Query

                const string Query_GetApprovalsForGRN = @"

	select Distinct aspnet_Users.UserId,aspnet_Users.UserName
	from SET_UserApprovalAmount
	INNER JOIN SET_ApprovalAuthority ON SET_ApprovalAuthority.ApprovalAuthorityID = SET_UserApprovalAmount.ApprovalAuthorityID 
	AND SET_ApprovalAuthority.CompanyID = SET_UserApprovalAmount.CompanyID 
	AND SET_ApprovalAuthority.PeriodID = SET_UserApprovalAmount.PeriodID 
	AND SET_ApprovalAuthority.Status = 1

	inner join aspnet_Users  on aspnet_Users.UserId = SET_ApprovalAuthority.UserID
	left join SET_DocumentType on SET_DocumentType.DocumentTypeID = SET_UserApprovalAmount.DocumentTypeID
	and SET_DocumentType.CompanyID = SET_UserApprovalAmount.CompanyID

	INNER JOIN aspnet_UsersInRoles on aspnet_UsersInRoles.UserId = @UserID

	where (SET_DocumentType.DocumentType like '%GRN%' or SET_DocumentType.DocumentType is null)
	and (SET_UserApprovalAmount.UserID = @UserID or SET_UserApprovalAmount.UserID is null )   --Login User ID
	and (SET_UserApprovalAmount.RoleID = aspnet_UsersInRoles.RoleId or SET_UserApprovalAmount.RoleID is null )   --Login user Role
	and SET_UserApprovalAmount.CompanyID = @CompanyID

";
                #endregion

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetApprovalsForGRN, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetApprovalsForGRN, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        #endregion
    }
}
