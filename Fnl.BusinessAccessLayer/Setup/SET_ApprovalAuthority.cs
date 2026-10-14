using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Security;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_ApprovalAuthority
    {
        #region Constant Queries

        const string Query_Get_Count_Set_ApprovalAuthority = @"
                                        select COUNT(companyid) AS Count from SET_ApprovalAuthority
                                            Where Status = 1
                                            and CompanyID = @CompanyID
                                            and PeriodID = @PeriodID";

        const string Query_AuthorityLevel = @"Select SET_Authority.AuthorityLevel From SET_Authority
Inner Join SET_ApprovalAuthority ON SET_ApprovalAuthority.AuthorityID = SET_Authority.AuthorityID
	AND SET_ApprovalAuthority.CompanyID = SET_Authority.CompanyID
	AND SET_ApprovalAuthority.PeriodID = SET_Authority.PeriodID
	AND SET_ApprovalAuthority.Status = 1
Where
	SET_Authority.CompanyID = @CompanyID And 
	SET_Authority.PeriodID = @PeriodID And
	SET_Authority.Status = 1 And
	SET_ApprovalAuthority.UserID = @UserID";


        const string Query_ListOfApprovers = @"
select 
(Select UserName from aspnet_Users where UserID = SET_UserApprovalAmount.UserID) as CreatedByUserName
,(Select UserName from aspnet_Users where UserID = SET_ApprovalAuthority.UserID) as ApproverName
,ASPNet_Users.UserID as CreatedByUser
,SET_ApprovalAuthority.UserID as Approver
, SET_DocumentApprove.ApprovalStatus
, INV_StockTransactionHeader.StockTransactionID
,* from SET_ApprovalAuthority 
inner join SET_UserApprovalAmount on SET_UserApprovalAmount.ApprovalAuthorityID = SET_ApprovalAuthority.ApprovalAuthorityID
	and SET_UserApprovalAmount.CompanyID = SET_ApprovalAuthority.CompanyID
	and SET_UserApprovalAmount.PeriodID = SET_ApprovalAuthority.PeriodID
inner join INV_StockTransactionHeader on INV_StockTransactionHeader.DocumentTypeID = SET_UserApprovalAmount.DocumentTypeID 
	and SET_UserApprovalAmount.CompanyID = INV_StockTransactionHeader.CompanyID
	and SET_UserApprovalAmount.PeriodID = INV_StockTransactionHeader.PeriodID
INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = INV_StockTransactionHeader.CompanyID 
	AND SET_DocumentApprove.PeriodID = INV_StockTransactionHeader.PeriodID 
	AND SET_DocumentApprove.Status = 1 
	AND SET_DocumentApprove.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
	AND CAST(SET_DocumentApprove.DocumentNo as int) = INV_StockTransactionHeader.DocumentNo 
	AND SET_DocumentApprove.CreateDate = 
	(
		SELECT MAX(CreateDate) FROM SET_DocumentApprove 
		WHERE CompanyID = INV_StockTransactionHeader.CompanyID 
			AND PeriodID = INV_StockTransactionHeader.PeriodID 
			AND DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
			AND CAST(SET_DocumentApprove.DocumentNo as int) = INV_StockTransactionHeader.DocumentNo
	) 
Inner join ASPNet_Users on ASPNet_Users.UserID = SET_UserApprovalAmount.UserID            
where SET_ApprovalAuthority.CompanyID = @CompanyID
	and SET_ApprovalAuthority.PeriodID = @PeriodID
    and SET_UserApprovalAmount.DocumentTypeID = @DocumentTypeID
	and SET_ApprovalAuthority.Status = 1
	and INV_StockTransactionHeader.StockTransactionID = @StockTransactionID
	and SET_DocumentApprove.DocumentNo = @DocumentNo
	and SET_DocumentApprove.ApprovalStatus = 'ReleaseForApprove'

";


        const string Query_SelectAll_Searching = @" 
SELECT DISTINCT SET_ApprovalAuthority.ApprovalAuthorityID
      ,SET_ApprovalAuthority.UserID
      ,SET_ApprovalAuthority.AuthorityID
      ,SET_ApprovalAuthority.CompanyID
      ,SET_ApprovalAuthority.PeriodID
      ,SET_ApprovalAuthority.Status
FROM SET_ApprovalAuthority
INNER JOIN aspnet_Users ON aspnet_Users.UserId = SET_ApprovalAuthority.UserID
LEFT JOIN SET_Authority ON SET_Authority.AuthorityID = SET_ApprovalAuthority.AuthorityID
	AND SET_Authority.CompanyID = SET_ApprovalAuthority.CompanyID AND SET_Authority.PeriodID = SET_ApprovalAuthority.PeriodID 
	AND SET_Authority.Status = 1 ";

        const string Query_SelectAll_Searching_WithPaging = @" 
SELECT *, ROW_NUMBER() OVER (ORDER BY SET_ApprovalAuthority.ApprovalAuthorityID) AS 'Row' FROM (SELECT DISTINCT tbl_AA.ApprovalAuthorityID
      ,tbl_AA.UserID
      ,aspnet_Users.UserName
      ,tbl_AA.AuthorityID
      ,SET_Authority.AuthorityName
      ,tbl_AA.CompanyID
      ,tbl_AA.PeriodID
      ,tbl_AA.Status
FROM SET_ApprovalAuthority AS tbl_AA
INNER JOIN aspnet_Users ON aspnet_Users.UserId = tbl_AA.UserID
LEFT JOIN SET_Authority ON SET_Authority.CompanyID = tbl_AA.CompanyID AND SET_Authority.PeriodID = tbl_AA.PeriodID 
	AND SET_Authority.AuthorityID = tbl_AA.AuthorityID AND SET_Authority.Status = 1) AS SET_ApprovalAuthority ";

        const string Query_SelectAllDataApprovalReport = @" 
SELECT *, ROW_NUMBER() OVER (ORDER BY SET_ApprovalAuthority.ApprovalAuthorityID) AS 'Row' FROM (SELECT Distinct tbl_AA.ApprovalAuthorityID
      ,tbl_AA.UserID
      ,aspnet_Users.UserName
      ,tbl_AA.AuthorityID
      ,SET_Authority.AuthorityName
      ,tbl_AA.CompanyID
      ,tbl_AA.PeriodID
      ,tbl_AA.Status
	  ,aspnet_Roles.RoleName
	  ,SET_UserApprovalAmount.ApprovalItemGroup
	  ,SET_UserApprovalAmount.DocumentTypeID
      ,SET_DocumentType.DocumentType
	  ,SET_UserApprovalAmount.RoleID
	  ,SET_UserApprovalAmount.EffectiveDateFrom
	  ,SET_UserApprovalAmount.EffectiveDateTo
	  ,SET_UserApprovalAmount.AmountFrom
	  ,SET_UserApprovalAmount.AmountTo
	  ,SET_UserApprovalAmount.ApprovalOperator
	  ,SET_UserApprovalAmount.EditPermission,aspnet.UserName as ApproverName
	  	  
FROM SET_ApprovalAuthority AS tbl_AA 

LEFT JOIN SET_Authority ON SET_Authority.CompanyID = tbl_AA.CompanyID AND SET_Authority.PeriodID = tbl_AA.PeriodID and SET_Authority.AuthorityID = tbl_AA.AuthorityID
AND SET_Authority.Status = 1

Inner join SET_UserApprovalAmount ON tbl_AA.ApprovalAuthorityID = SET_UserApprovalAmount.ApprovalAuthorityID and tbl_AA.CompanyID = SET_UserApprovalAmount.CompanyID
and tbl_AA.PeriodID = SET_UserApprovalAmount.PeriodID
AND SET_UserApprovalAmount.Status = 1 

LEFT JOIN aspnet_Roles ON aspnet_Roles.RoleId = SET_UserApprovalAmount.RoleID
INNER JOIN aspnet_Users as aspnet on aspnet.UserId = tbl_AA.UserID
INNER JOIN aspnet_Users ON aspnet_Users.UserId = SET_UserApprovalAmount.UserID
LEFT JOIN SET_DocumentType ON SET_DocumentType.DocumentTypeID = SET_UserApprovalAmount.DocumentTypeID
	AND SET_Authority.AuthorityID = tbl_AA.AuthorityID AND SET_Authority.Status = 1) AS SET_ApprovalAuthority";
        
        #endregion

        #region Class Methods

        public static int Get_Count_Set_ApprovalAuthority(int companyID, int periodID)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_Count_Set_ApprovalAuthority, sqlparams));

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static DataTable Get_AuthorityLevel(int companyID, int periodID, Guid userName)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@UserID", userName);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_AuthorityLevel, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataWithOtherTables(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc)
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_Searching_WithPaging + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), Query_SelectAll_Searching_WithPaging + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_Searching + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""),
                        sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public bool InsertUAAHeaderAndDetail(List<SET_UserApprovalAmount> colUserApprovalAmount)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertApprovalAuthority");

            try
            {
                this.Insert(sqltrans);

                foreach (SET_UserApprovalAmount uaa in colUserApprovalAmount)
                {
                    uaa.ApprovalAmountID = SET_UserApprovalAmount.GetNewApprovalAmountID(f_ApprovalAuthorityID, f_CompanyID, f_PeriodID, sqltrans);
                    uaa.Insert(sqltrans);
                }

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
                if (result)
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "InsertApprovalAuthority");
            }

            return result;
        }

        public bool UpdateUAAHeaderAndDetail(List<SET_UserApprovalAmount> colAddUserApprovalAmount, List<SET_UserApprovalAmount> colUpdateUserApprovalAmount, List<SET_UserApprovalAmount> colDeleteUserApprovalAmount)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateApprovalAuthority");

            try
            {
                this.UpdateDataByKey(sqltrans);

                foreach (SET_UserApprovalAmount uaaAdd in colAddUserApprovalAmount)
                {
                    uaaAdd.ApprovalAmountID = SET_UserApprovalAmount.GetNewApprovalAmountID(f_ApprovalAuthorityID, f_CompanyID, f_PeriodID, sqltrans);
                    uaaAdd.Insert(sqltrans);
                }

                foreach (SET_UserApprovalAmount uaaUpdate in colUpdateUserApprovalAmount)
                    uaaUpdate.UpdateDataByKey(sqltrans);

                foreach (SET_UserApprovalAmount uaaDelete in colDeleteUserApprovalAmount)
                    SET_UserApprovalAmount.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, uaaDelete.ApprovalAmountID, f_ApprovalAuthorityID, f_CompanyID, f_PeriodID);

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
                if (result)
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateApprovalAuthority");
            }

            return result;
        }

        public bool DeleteUAAHeaderAndDetail(List<SET_UserApprovalAmount> colDeleteUserApprovalAmount)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("DeleteApprovalAuthority");

            try
            {
                DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, UpdatedByValue, f_ApprovalAuthorityID, f_CompanyID, f_PeriodID);

                foreach (SET_UserApprovalAmount uaaDelete in colDeleteUserApprovalAmount)
                    SET_UserApprovalAmount.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, uaaDelete.ApprovalAmountID, f_ApprovalAuthorityID, f_CompanyID, f_PeriodID);

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
                if (result)
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "DeleteApprovalAuthority");
            }
            return result;
        }

        public static DataTable Get_ListOfApprovers(int companyID, int periodID, int documentTypeID, int stockTransactionID, int documentNo)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[3] = new SqlParameter("@StockTransactionID", stockTransactionID);
                sqlparams[4] = new SqlParameter("@DocumentNo", documentNo);
        
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_ListOfApprovers, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataReport(string strWhere)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            DataTable dt = null;

            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllDataApprovalReport + strWhere, sqlparamsList.ToArray()).Tables[0];
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
