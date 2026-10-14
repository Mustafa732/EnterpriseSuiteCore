using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_ApprovalAlert
    {
        #region Constant Strings

        const string Query_Test = @" select SET_ApprovalAlert.IsApproved,
SET_ApprovalAlert.DocumentTypeID, DocumentNo,' UnApproved ' + FormName + ' Document No: ' + convert(varchar,DocumentNo) as [Description],
SET_ApprovalAlert.PathAndQuery,SET_ApprovalAlert.CreateDate,LastApproveBy,SET_ApprovalAlert.LastApproveByID,DocumentID,FormName
from SET_ApprovalAlert
inner join set_DocumentType on set_DocumentType.DocumenttypeID = SET_ApprovalAlert.DocumenttypeID
and set_DocumentType.CompanyID = SET_ApprovalAlert.CompanyID
and set_DocumentType.PeriodID = SET_ApprovalAlert.PeriodID
where DocumentID is not null
and IsApproved <> 1
--and LastApproveByID != @LastApproveByID or LastApproveBy is null
order by Createdate desc";

        const string Query_GetDateForApproval = @"
select 

TAB.Description
,TAB.DocumentNo
,TAB.DocumentTypeID
,TAB.PathAndQuery
from (
select distinct --poh_Outer.IsApproved,
poh_Outer.DocumentTypeID, poh_Outer.DocumentNo,
--' UnApproved ' + FormName + ' Document No: ' + convert(varchar,poh_Outer.DocumentNo) as [Description],
'Pending for Approval ' + FormName  + ' No: ' + convert(varchar,poh_Outer.DocumentNo) as [Description],
poh_Outer.PathAndQuery,
--poh_Outer.CreateDate,
--LastApproveBy,
--poh_Outer.LastApproveByID,
DocumentID,FormName
,SET_DocumentApprove.Createdate
from SET_ApprovalAlert poh_Outer
inner join set_DocumentType on set_DocumentType.DocumenttypeID = poh_Outer.DocumenttypeID
and set_DocumentType.CompanyID = poh_Outer.CompanyID
and set_DocumentType.PeriodID = poh_Outer.PeriodID

INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = poh_Outer.CompanyID AND SET_DocumentApprove.PeriodID = poh_Outer.PeriodID 
    AND SET_DocumentApprove.Status = 1 AND SET_DocumentApprove.DocumentTypeID = poh_Outer.DocumentTypeID 
    AND SET_DocumentApprove.DocumentNo = poh_Outer.DocumentNo 
	AND SET_DocumentApprove.CreateDate = 
    (
         SELECT MAX(CreateDate) FROM SET_DocumentApprove 
         WHERE SET_DocumentApprove.Status = 1 and CompanyID = poh_Outer.CompanyID AND PeriodID = poh_Outer.PeriodID AND DocumentTypeID = poh_Outer.DocumentTypeID 
         AND DocumentNo = poh_Outer.DocumentNo
     )
            
     inner join Set_documentPendingApprove on Set_documentPendingApprove.DocumentApproveID = SET_DocumentApprove.DocumentApproveID
     and Set_documentPendingApprove.companyID = SET_DocumentApprove.companyID
     and Set_documentPendingApprove.PeriodID = SET_DocumentApprove.PeriodID
    and Set_documentPendingApprove.Status = 1

INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = poh_Outer.CompanyID 
			AND SET_UserApprovalAmount.PeriodID = poh_Outer.PeriodID AND SET_UserApprovalAmount.Status = 1
			AND (SET_UserApprovalAmount.DocumentTypeID = poh_Outer.DocumentTypeID OR SET_UserApprovalAmount.DocumentTypeID IS NULL)
			AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1
						   WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom 
								AND SET_UserApprovalAmount.EffectiveDateTo THEN 1
			               ELSE 0 END))


where poh_Outer.companyID = @CompanyID and poh_Outer.PeriodID = @PeriodID
and DocumentID is not null
and IsApproved <> 1
and IsApproved <> 14
AND (Set_documentPendingApprove.ApprovalUserID = @LastApproveByID OR Set_documentPendingApprove.ApprovalUserID IS NULL)
) as TAB
order by TAB.Createdate desc
";
        const string Query_GetDataByUserId = @" declare @AuthorityLevel int
set @AuthorityLevel = @Level

if(@AuthorityLevel = 1) begin
select 
SET_ApprovalAlert.DocumentTypeID, DocumentNo,' UnApproved ' + FormName + ' Document No: ' + convert(varchar,DocumentNo) as [Description],
SET_ApprovalAlert.PathAndQuery,SET_ApprovalAlert.CreateDate,LastApproveBy
from SET_ApprovalAlert
inner join set_DocumentType on set_DocumentType.DocumenttypeID = SET_ApprovalAlert.DocumenttypeID
and set_DocumentType.CompanyID = SET_ApprovalAlert.CompanyID
and set_DocumentType.PeriodID = SET_ApprovalAlert.PeriodID
where SET_ApprovalAlert.CompanyID = @CompanyID
And SET_ApprovalAlert.PeriodID = @PeriodID
--and Isapproved <> 1 
--and LastApproveBy is not null
and Isapproved = 2
order by createdate desc
end

else
begin
select 
SET_ApprovalAlert.DocumentTypeID, DocumentNo,' UnApproved ' + FormName + ' Document No: ' + convert(varchar,DocumentNo) as [Description],
SET_ApprovalAlert.PathAndQuery,SET_ApprovalAlert.CreateDate,LastApproveBy
from SET_ApprovalAlert
inner join set_DocumentType on set_DocumentType.DocumenttypeID = SET_ApprovalAlert.DocumenttypeID
and set_DocumentType.CompanyID = SET_ApprovalAlert.CompanyID
and set_DocumentType.PeriodID = SET_ApprovalAlert.PeriodID
where SET_ApprovalAlert.CompanyID = @CompanyID
And SET_ApprovalAlert.PeriodID = @PeriodID
and Isapproved <> 1 
--and LastApproveBy is null
and SET_ApprovalAlert.LastApproveByID != @LastApproveByID or SET_ApprovalAlert.LastApproveByID is null
order by createdate desc
end";

        const string Query_GetAuthorityLevelByUserId = @" select SET_Authority.AuthorityLevel,SET_Authority.AuthorityName
from SET_Authority 
inner join SET_ApprovalAuthority on SET_Authority.AuthorityID = SET_ApprovalAuthority.AuthorityID
and SET_Authority.CompanyID = SET_ApprovalAuthority.CompanyID 
and SET_Authority.PeriodID = SET_ApprovalAuthority.PeriodID
and SET_Authority.Status = 1 
AND SET_ApprovalAuthority.STATUS = 1
WHERE SET_ApprovalAuthority.UserID = @UserID
AND SET_Authority.CompanyID = @CompanyID
AND SET_Authority.PeriodID = @PeriodID ";


        const string Query_Update_Status = @" update SET_ApprovalAlert set Isapproved = @Isapproved , LastApproveBy = @LastApproveBy , LastApproveByID = @LastApproveByID where CompanyID = @CompanyID and PeriodID = @PeriodID and DocumentTypeID = @DocumenttypeID and DocumentNo = @DocumentNo";


        const string Query_AddAlerts = @" INSERT INTO [SET_ApprovalAlert] ([CompanyID], [PeriodID], [DocumentTypeID], [DocumentNo], [UserID], [FormName], [IsApproved], [PathAndQuery], [CreateDate],[DocumentID],LastApproveByID)
		VALUES(@CompanyID, @PeriodID, @DocumentTypeID, @DocumentNo, @UserID, @FormName, @IsApproved, @PathAndQuery, getdate(),@DocumentID,@UserID)";

        #endregion
        #region Class Methods

        public static DataTable GetDataByUserId(Nullable<int> companyID, Nullable<int> periodID, int Level, Guid LastApproveByID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Level", Level);
                sqlparams[3] = new SqlParameter("@LastApproveByID", LastApproveByID);

                //dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByUserId, sqlparams).Tables[0];
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDateForApproval, sqlparams).Tables[0];
                
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetUser_AuthorityLevel(Nullable<int> companyID, Nullable<int> periodID , string UserID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@UserID", UserID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAuthorityLevelByUserId, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static bool UpdateAprovedStatus(int DocumentNo, int DocumenttypeID, int Isapproved, int CompanyID, int PeriodID, string LastApproveBy, Guid LastApproveByID, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@DocumentNo", DocumentNo);
                sqlparams[3] = new SqlParameter("@DocumenttypeID", DocumenttypeID);
                sqlparams[4] = new SqlParameter("@Isapproved", Isapproved);
                sqlparams[5] = new SqlParameter("@LastApproveBy", LastApproveBy);
                sqlparams[6] = new SqlParameter("@LastApproveByID", LastApproveByID);
                
                

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_Status, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_Status, sqlparams) > 0)
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

        public static bool AddAlerts(int CompanyID, int PeriodID, int DocumentTypeID, long DocumentNo, Guid UserID, string FormName, string PathAndQuery, int DocumentID, int IsApproved,SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[9];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[3] = new SqlParameter("@DocumentNo", DocumentNo);
                sqlparams[4] = new SqlParameter("@UserID", UserID);
                sqlparams[5] = new SqlParameter("@FormName", FormName);
                sqlparams[6] = new SqlParameter("@PathAndQuery", PathAndQuery);
                sqlparams[7] = new SqlParameter("@DocumentID", DocumentID);
                sqlparams[8] = new SqlParameter("@IsApproved", IsApproved);
                

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_AddAlerts, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_AddAlerts, sqlparams) > 0)
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


        public static DataTable GetPendingApprovalDocumentsForDashboard(Nullable<int> companyID, Nullable<int> periodID,Nullable<int> FiscalID, Guid LastApproveByID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FiscalID", FiscalID);
                sqlparams[3] = new SqlParameter("@LastApproveByID", LastApproveByID);
                
                #region Query

                const string Query_GetPendingApprovalDocumentsForDashboard = @"
WITH cte_DocumentApprove AS (

    select SET_DocumentApprove .CompanyID,SET_DocumentApprove .PeriodID,SET_DocumentApprove .DocumentApproveID,SET_DocumentApprove .DocumentTypeID,SET_DocumentApprove .DocumentNo,SET_DocumentApprove .ApprovalStatus,SET_DocumentApprove .Status,SET_DocumentApprove .Comments,SET_DocumentApprove .CreatedBy,SET_DocumentApprove .CreateDate,SET_DocumentApprove .CreatedByValue,SET_DocumentApprove .FiscalID
	from SET_DocumentApprove 
	inner join vw_DocumentApproveStatus on vw_DocumentApproveStatus.DocumentTypeID = SET_DocumentApprove.DocumentTypeID
	and vw_DocumentApproveStatus.DocumentNo = SET_DocumentApprove.DocumentNo
	and vw_DocumentApproveStatus.CompanyID = SET_DocumentApprove.CompanyID
	and vw_DocumentApproveStatus.FiscalID = SET_DocumentApprove.FiscalID
	
	where SET_DocumentApprove .CompanyID =  {AppendCompanyID}
)
select distinct MenuName,MenuPath + '?menuID=' + convert(varchar,MenuID) + '&DocTypeID=' + convert(varchar,DocumentTypeID) as  MenuPath ,MenuID,DocumentType,DocumentTypeID,count(TAB.DocumentNo) as NoofDocuments

--select distinct MenuName,MenuPath + '?menuID=' + converT(varchar,MenuID) as  MenuPath ,MenuID,DocumentType,DocumentTypeID,count(TAB.DocumentNo) as NoofDocuments
from (
select distinct 
poh_Outer.DocumentTypeID, poh_Outer.DocumentNo,cte_DocumentApprove.Createdate,cte_DocumentApprove.FiscalID,set_DocumentType.DocumentType
,ApplicationMenu.MenuName,ApplicationMenu.MenuPath,ApplicationMenu.MenuID,cte_DocumentApprove.CreatedBy

from SET_ApprovalAlert poh_Outer
inner join set_DocumentType on set_DocumentType.DocumenttypeID = poh_Outer.DocumenttypeID
and set_DocumentType.CompanyID = poh_Outer.CompanyID
and set_DocumentType.PeriodID = poh_Outer.PeriodID

inner join ApplicationMenu on ApplicationMenu.MenuID = set_DocumentType.MenuID

INNER JOIN cte_DocumentApprove ON cte_DocumentApprove.CompanyID = poh_Outer.CompanyID AND cte_DocumentApprove.PeriodID = poh_Outer.PeriodID 
    AND cte_DocumentApprove.Status = 1 AND cte_DocumentApprove.DocumentTypeID = poh_Outer.DocumentTypeID 
    AND cte_DocumentApprove.DocumentNo = poh_Outer.DocumentNo 
	AND cte_DocumentApprove.CreateDate = 
    (
         SELECT MAX(CreateDate) FROM SET_DocumentApprove 
         WHERE SET_DocumentApprove.Status = 1 and CompanyID = poh_Outer.CompanyID AND PeriodID = poh_Outer.PeriodID AND DocumentTypeID = poh_Outer.DocumentTypeID 
         AND DocumentNo = poh_Outer.DocumentNo
        AND FiscalID = @FiscalID
     )

 
     inner join Set_documentPendingApprove on Set_documentPendingApprove.DocumentApproveID = cte_DocumentApprove.DocumentApproveID
     and Set_documentPendingApprove.companyID = cte_DocumentApprove.companyID
     and Set_documentPendingApprove.PeriodID = cte_DocumentApprove.PeriodID
    and Set_documentPendingApprove.Status = 1

INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = poh_Outer.CompanyID 
			AND SET_UserApprovalAmount.PeriodID = poh_Outer.PeriodID AND SET_UserApprovalAmount.Status = 1
			AND (SET_UserApprovalAmount.DocumentTypeID = poh_Outer.DocumentTypeID OR SET_UserApprovalAmount.DocumentTypeID IS NULL)
			AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1
						   WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom 
								AND SET_UserApprovalAmount.EffectiveDateTo THEN 1
			               ELSE 0 END))

where poh_Outer.companyID = @CompanyID and poh_Outer.PeriodID = @PeriodID
and DocumentID is not null
--and IsApproved <> 1 and IsApproved <> 14
AND (Set_documentPendingApprove.ApprovalUserID = @LastApproveByID OR Set_documentPendingApprove.ApprovalUserID IS NULL)
and cte_DocumentApprove.CreatedBy <> @LastApproveByID  and cte_DocumentApprove.FiscalID = @FiscalID

) as TAB
group by MenuName,MenuPath,MenuID,DocumentType,DocumentTypeID
";
                #endregion

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetPendingApprovalDocumentsForDashboard.Replace("{AppendCompanyID}", companyID.ToString()), sqlparams).Tables[0];
                
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
