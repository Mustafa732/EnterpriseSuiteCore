using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;

using Common.Utils;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_DocumentType
    {
        #region Constant Queries

        const string Query_SelectDocumentTypeOnTheBasesChecqs = @" 
select Distinct  UsersInDocumentType.UserID, convert(bit, 'True') IsAssigned, UsersInDocumentType.CompanyID, UsersInDocumentType.DocumentTypeID, Set_documenttype.DocumentType, 
    UsersInDocumentType.DisallowBackDate, Set_Period.PeriodSetName, Set_Period.PeriodYear,UsersInDocumentType.AllowPreviousMonth,isnull(SET_DocumentType.GroupID,0) as GroupID, null as DisallowedMonths    
from UsersInDocumentType 
    inner join SET_DocumentType ON SET_DocumentType.DocumentTypeID = UsersInDocumentType.DocumentTypeID 
--and SET_DocumentType.CompanyID = UsersInDocumentType.CompanyID 
    INNER JOIN Set_Period ON Set_Period.PeriodID = SET_DocumentType.PeriodID AND Set_Period.Status = 1 AND Set_Period.IsActive = 1 
where UsersInDocumentType.UserId = @UserId ";

        const string Query_AllTransactionTypes = @" 
select distinct TransactionType from Set_DocumentType where TransactionType is not null and TransactionType <> 'No Impact' ";

        const string Query_SelectAllData = @" 

Select Distinct DocumentTypeID, DocumentType, CompanyID, 'True' as IsAssigned, 'False' as DisallowBackDate, 
Set_Period.PeriodSetName, Set_Period.PeriodYear, GroupID, null as AllowPreviousMonth
from SET_DocumentType 
	Inner Join Set_Period On Set_Period.PeriodID = SET_DocumentType.PeriodID AND Set_Period.Status = 1 AND Set_Period.IsActive = 1 
Where SET_DocumentType.Status = 1 AND SET_DocumentType.IsActive = 1 AND IsNull(SET_DocumentType.IsHide, 0) = 0 --AND CompanyID = @CompanyID 
ORDER BY Set_Period.PeriodYear desc, SET_DocumentType.DocumentType asc ";

        const string Query_SelectAllDataNOT = @" 
    Select SET_DocumentType.DocumentTypeID, DocumentType, SET_DocumentType.CompanyID, 'False' as IsAssigned, UsersInDocumentType.DisallowBackDate, 
        Set_Period.PeriodSetName, Set_Period.PeriodYear,UsersInDocumentType.AllowPreviousMonth , null as DisallowedMonths
    from SET_DocumentType 
        Inner Join Set_Period On Set_Period.PeriodID = SET_DocumentType.PeriodID AND Set_Period.Status = 1 AND Set_Period.IsActive = 1 
        left Join UsersInDocumentType ON UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID 
            AND UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID and UsersInDocumentType.userid = @UserID 
    Where SET_DocumentType.Status = 1 and (UsersInDocumentType.userid != @UserID or UsersInDocumentType.userid is null) AND SET_DocumentType.CompanyID = @CompanyID 
        {0}
    ORDER BY Set_Period.PeriodYear desc, SET_DocumentType.DocumentType asc ";

        const string Query_GetCISDocumentTypesByMenuPath = @" SELECT DocumentTypeID, DocumentType FROM SET_DocumentType 
WHERE SET_DocumentType.CompanyID = @CompanyID AND SET_DocumentType.PeriodID = @PeriodID 
    AND SET_DocumentType.MenuID in (SELECT MenuID FROM ApplicationMenu WHERE MenuPath = '{0}') AND SET_DocumentType.IsActive = 1 AND SET_DocumentType.Status = 1 
    and SET_DocumentType.UserDefined = 0 
order by DocumentType asc ";

        const string Query_GetAllByMenuPath = @" SELECT DocumentTypeID, DocumentType FROM SET_DocumentType 
WHERE SET_DocumentType.CompanyID = @CompanyID AND SET_DocumentType.PeriodID = @PeriodID 
    AND SET_DocumentType.MenuID in (SELECT MenuID FROM ApplicationMenu WHERE MenuPath in ({0})) AND SET_DocumentType.IsActive = 1 AND SET_DocumentType.Status = 1 
ORDER BY DocumentType asc ";

        const string Query_GetAllByMenuPathAndDocType = @" SELECT DocumentTypeID, DocumentType FROM SET_DocumentType 
WHERE SET_DocumentType.CompanyID = @CompanyID AND SET_DocumentType.PeriodID = @PeriodID 
    AND SET_DocumentType.MenuID in (SELECT MenuID FROM ApplicationMenu 
	WHERE MenuPath in ({0})) AND SET_DocumentType.IsActive = 1 AND SET_DocumentType.Status = 1 
	 {1}
ORDER BY DocumentType asc ";

        const string Query_GetAllByMenuPathAndDispatchDocType = @" SELECT DocumentTypeID, DocumentType FROM SET_DocumentType 
WHERE SET_DocumentType.CompanyID = @CompanyID AND SET_DocumentType.PeriodID = @PeriodID 
    AND SET_DocumentType.MenuID in (SELECT MenuID FROM ApplicationMenu WHERE MenuPath in ({0})) AND SET_DocumentType.IsActive = 1 AND SET_DocumentType.Status = 1 
	and SET_DocumentType.DocumentType like {1}
ORDER BY DocumentType asc ";

        const string Query_GetAllByMenuPath_forIT = @" SELECT DocumentTypeID, DocumentType FROM SET_DocumentType 
WHERE SET_DocumentType.CompanyID = @CompanyID AND SET_DocumentType.PeriodID = @PeriodID 
    AND SET_DocumentType.MenuID in (SELECT MenuID FROM ApplicationMenu WHERE MenuPath in ({0})) AND SET_DocumentType.IsActive = 1 AND SET_DocumentType.Status = 1 
ORDER BY DocumentType asc ";

        const string Query_GetAllAllowedByMenuPath = @" SELECT SET_DocumentType.DocumentTypeID, DocumentType FROM SET_DocumentType 
	Inner Join UsersInDocumentType On UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
		And UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID And UsersInDocumentType.UserID = @UserID 
WHERE SET_DocumentType.CompanyID = @CompanyID AND SET_DocumentType.PeriodID = @PeriodID AND SET_DocumentType.IsActive = 1 AND SET_DocumentType.Status = 1 
    AND SET_DocumentType.MenuID in (SELECT MenuID FROM ApplicationMenu WHERE MenuPath in ({0})) 
ORDER BY DocumentType asc ";

        const string Query_SelectDocumentTypesFor_Receipt = @" SELECT DocumentTypeID, DocumentType FROM SET_DocumentType 
WHERE SET_DocumentType.CompanyID = @CompanyID AND SET_DocumentType.PeriodID = @PeriodID 
    AND SET_DocumentType.MenuID in (SELECT MenuID FROM ApplicationMenu WHERE MenuPath in ({0})) AND SET_DocumentType.IsActive = 1 AND SET_DocumentType.Status= 1 
order by DocumentType asc ";

        const string Query_SelectDocumentTypesFor_DirectInvoices = @" SELECT DocumentTypeID, DocumentType FROM SET_DocumentType 
WHERE SET_DocumentType.CompanyID = @CompanyID AND SET_DocumentType.PeriodID = @PeriodID 
    AND SET_DocumentType.MenuID in (SELECT MenuID FROM ApplicationMenu WHERE MenuPath in ({0})) AND DocumentType LIKE 'DI-%' AND SET_DocumentType.IsActive = 1 AND SET_DocumentType.Status= 1 
order by DocumentType asc ";

        const string Query_SelectAll_ByCompanyAndPeriodIDs = @" 
SELECT SET_DocumentType.CompanyID, SET_DocumentType.ChartOfAccountID, SET_DocumentType.PeriodID, SET_DocumentType.DocumentTypeID, SET_DocumentType.DocumentType, 
    SET_DocumentType.ChartOfAccountID, SET_DocumentType.DefaultAccountCode, SET_DocumentType.Status, SET_DocumentType.CreatedBy, SET_DocumentType.CreateDate, 
    SET_DocumentType.UpdatedBy, SET_DocumentType.UpdateDate, SET_DocumentType.CreatedByValue, SET_DocumentType.UpdatedByValue, SET_DocumentType.TransactionType 
FROM SET_DocumentType 
WHERE SET_DocumentType.CompanyID = @CompanyID AND SET_DocumentType.PeriodID = @PeriodID 
    AND (SET_DocumentType.ChartOfAccountID = @ChartOfAccountID OR @ChartOfAccountID IS NULL) AND SET_DocumentType.Status = 1 AND SET_DocumentType.IsActive = @IsActive 
    AND (SET_DocumentType.DocumentTypeID = @DocumentTypeID OR @DocumentTypeID IS NULL) 
    AND (SET_DocumentType.TransactionImpact = @TransactionImpact OR @TransactionImpact IS NULL) ";

        const string Query_SelectAll_ByMenuID = @" SELECT * FROM SET_DocumentType 
WHERE SET_DocumentType.CompanyID = @CompanyID AND SET_DocumentType.PeriodID = @PeriodID AND SET_DocumentType.MenuID = @MenuID 
    AND SET_DocumentType.IsActive = @IsActive AND SET_DocumentType.Status = 1 AND IsNull(SET_DocumentType.IsHide, 0) = 0 
ORDER BY SET_DocumentType.SetAsDefault desc, SET_DocumentType.UserDefined desc, SET_DocumentType.DocumentType asc ";

        const string Query_SelectAll_byMenuUDSecurityOnDocumentType = @"  SELECT Distinct SET_DocumentType.DocumentTypeID,SET_DocumentType.DocumentType,SET_DocumentType.SetAsDefault,SET_DocumentType.UserDefined FROM SET_DocumentType 
    Inner Join UsersInDocumentType ON UsersInDocumentType.Documenttypeid = SET_DocumentType.DocumentTypeID 
        AND UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
WHERE UsersInDocumentType.CompanyID = @CompanyID AND SET_DocumentType.PeriodID = @PeriodID AND SET_DocumentType.MenuID = @MenuID 
    AND SET_DocumentType.IsActive = @IsActive AND SET_DocumentType.Status = 1 AND IsNull(SET_DocumentType.IsHide, 0) = 0 AND UsersInDocumentType.UserID = @UserID 
ORDER BY SET_DocumentType.SetAsDefault desc, SET_DocumentType.UserDefined desc, SET_DocumentType.DocumentType asc ";

        const string Query_SelectVoucherDocumentType = @" SELECT * FROM SET_DocumentType 
WHERE SET_DocumentType.CompanyID = @CompanyID AND SET_DocumentType.PeriodID = @PeriodID AND SET_DocumentType.MenuID = @MenuID 
    AND SET_DocumentType.IsActive = @IsActive AND SET_DocumentType.Status = 1 

union all

(SELECT * FROM SET_DocumentType where DocumentTypeID in (22, 23, 5, 24, 25) and SET_DocumentType.CompanyID = @CompanyID) 
ORDER BY SET_DocumentType.SetAsDefault desc, SET_DocumentType.UserDefined, SET_DocumentType.DocumentType ";

        const string Query_SelectVoucherDocumentTypeForSearch = @" SELECT * FROM SET_DocumentType 
where SET_DocumentType.MenuID in 
(
    SELECT MenuID FROM ApplicationMenu WHERE MenuPath in ('/Sales/Dispatch_frm.aspx', '/AR/Transaction/Receipt_Transaction_frm.aspx', '/Sales/Sales_Invoice_frm.aspx', 
        '/GLForms/Transaction/GL_Transaction_frm.aspx', '/AP/Transaction/Payment.aspx', '/Inventory/Transaction/Stock_frm.aspx', 
        '/AR/Transaction/Invoice_Transaction_frm.aspx', '/Cane/Transaction/DepotCPR.aspx', '/Cane/Transaction/SecondWeight.aspx', 
        '/Inventory/Transaction/StockReturn.aspx'
        )
) and CompanyID = @CompanyID and PeriodID = @PeriodID and IsActive = @IsActive and Status = 1 ORDER BY DocumentType ";

        const string Query_IsValidAccountCode = @" SELECT DocumentTypeID FROM Set_DocumentType 
WHERE DefaultAccountCode = @DefaultAccountCode AND TransactionType = @TransactionType AND DocumentTypeID = @DocumentTypeID; ";

        const string GetMenuIDByQuery = @" SELECT MenuID FROM ApplicationMenu WHERE MenuPath = '/GLForms/Transaction/GL_Transaction_frm.aspx' ";

        const string GetMenuIDByQuery_PO = @" SELECT MenuID FROM ApplicationMenu WHERE MenuPath = '/AP/Transaction/PurchaseOrder_frm.aspx' ";

        const string GetMenuIDByQuery_POVehicle = @" SELECT MenuID FROM ApplicationMenu WHERE MenuPath = '/AP/AP_Vehicle/PurchaseOrder_frm.aspx' ";

        const string GetMenuIDByQuery_PI = @" SELECT MenuID FROM ApplicationMenu WHERE MenuPath = '/AR/Transaction/Invoice_Transaction_frm.aspx' ";

        const string Query_GetData_ByKey_WithOtherTables = @" 
Select SET_DocumentType.CompanyID, SET_DocumentType.PeriodID, SET_DocumentType.DocumentTypeID, SET_DocumentType.DocumentType, SET_DocumentType.ChartOfAccountID, 
    SET_DocumentType.DefaultAccountCode, SET_DocumentType.Status, SET_DocumentType.CreatedBy, SET_DocumentType.CreateDate, SET_DocumentType.UpdatedBy, 
    SET_DocumentType.UpdateDate, SET_DocumentType.CreatedByValue, SET_DocumentType.UpdatedByValue, SET_DocumentType.TransactionType 
FROM SET_DocumentType WHERE SET_DocumentType.DocumentTypeID = @DocumentTypeID and SET_DocumentType.CompanyID= @CompanyID and SET_DocumentType.PeriodID = @PeriodID ";

        const string Query_GetAutoEntryGeneratingDocumentReport = @" 
SELECT dt.DocumentTypeID, dt.DocumentType,dt.Description, dt.TransactionImpact,dt.TransactionType,dt.ExpenseAllowed, dt.MenuID, MenuName, dt.DefaultAccountCode, dt.GroupID, GroupName, dt.IsActive AS DocumentActive,dt.ExpenseAllowed, ds.SerialNo, 
    ds.Level2Description, ds.Level3Description, ds.Level4Description, ds.DateValidFrom, ds.FromNo, ds.ToNo, ds.IsActive AS SequenceActive 
FROM SET_DocumentType dt 
    LEFT JOIN SET_DocumentSequence ds ON dt.DocumentTypeID = ds.DocumentTypeID AND dt.CompanyID = ds.CompanyID AND dt.PeriodID = ds.PeriodID AND ds.Status = 1 
	Inner Join ApplicationMenu ON  dt.MenuID = ApplicationMenu.MenuID
	Inner Join DocumentTypesGroup on dt.GroupID = DocumentTypesGroup.GroupID
WHERE dt.Status = 1  AND dt.CompanyID = @CompanyID AND dt.PeriodID = @PeriodID AND (dt.DocumentType like '%' + @DocumentType + '%' OR @DocumentType is null) 
    AND (dt.DefaultAccountCode like '%' + @AccountCode + '%' OR @AccountCode is null) 
{0}
ORDER BY dt.DocumentType ";

        const string Query_NonUserDefinedTypes = @" 
SELECT DISTINCT DocumentTypeID, DocumentType, DefaultAccountCode, SET_DocumentType.MenuID, TransactionImpact, TransactionType FROM SET_DocumentType 
    {0}
WHERE Status = 1 AND IsActive = 1  AND CompanyID = @CompanyID AND PeriodID = @PeriodID ";

        const string Query_IsBackDateAllow = @" Select DisallowBackDate from UsersInDocumentType 
Where UserID = @UserID And CompanyID = @CompanyID And Documenttypeid = @Documenttypeid ";

        const string Query_IsAllowPreviousMonth = @" Select isnull(AllowPreviousMonth,0) as AllowPreviousMonth from UsersInDocumentType 
Where UserID = @UserID And CompanyID = @CompanyID And Documenttypeid = @Documenttypeid ";

        const string Query_DocumentTypePayment = @" Select DocumentTypeID, DocumentType from SET_DocumentType 
where DocumentType = 'Payment' And SET_DocumentType.CompanyID = @CompanyID AND SET_DocumentType.PeriodID = @PeriodID ";

        const string Query_DocumentTypeReciept = @" Select DocumentTypeID, DocumentType from SET_DocumentType 
where DocumentType = 'Receipt' and SET_DocumentType.CompanyID = @CompanyID AND SET_DocumentType.PeriodID = @PeriodID  ";

        const string GetformsMenuIDByQuery = @" SELECT MenuID FROM ApplicationMenu WHERE MenuPath =  @menupath ";

        const string GetformsEnabledMenuIDByQuery = @" SELECT MenuID FROM ApplicationMenu WHERE MenuPath =  @menupath AND IsEnabled = 1 ";

        const string Query_SelectAllWithID = @" SELECT DocumentTypeID,DocumentType FROM SET_DocumentType 
WHERE Status = 1 AND (IsActive = @IsActive OR @IsActive is null) AND (CompanyID = @CompanyID OR @CompanyID is null) AND (PeriodID = @PeriodID OR @PeriodID is null)
ORDER BY DocumentType asc ";

        const string Query_SelectAllWithDocumentID = @" SELECT DISTINCT SET_DocumentType.DocumentTypeID,DocumentType FROM SET_DocumentType 
INNER JOIN GL_TransactionHeader ON GL_TransactionHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID
AND GL_TransactionHeader.CompanyID = SET_DocumentType.CompanyID
AND GL_TransactionHeader.PeriodID = SET_DocumentType.PeriodID
WHERE SET_DocumentType.Status = 1 AND (IsActive = @IsActive OR @IsActive is null) AND (SET_DocumentType.CompanyID = @CompanyID OR @CompanyID is null) AND (SET_DocumentType.PeriodID = @PeriodID OR @PeriodID is null)
ORDER BY DocumentType asc  ";

//        const string Query_Get_MaxId_WithMinMaxLimit = @" SELECT IsNull(max(DocumentTypeID), 0) + 1 as DocumentTypeID FROM Set_Documenttype 
//WHERE CompanyID = @CompanyID and PeriodID = @PeriodID 
//    AND DocumentTypeID Between IsNull((Select FieldValue1 From Set_Parameter Where FieldName = 'DocumentTypeIDsRange'), 1) 
//    AND IsNull((Select FieldValue2 From Set_Parameter Where FieldName = 'DocumentTypeIDsRange'), 199) ";

        const string Query_Get_MaxId_WithMinMaxLimit = @"    SELECT IsNull(max(DocumentTypeID), 0) + 1 as DocumentTypeID FROM Set_Documenttype 
WHERE CompanyID = @CompanyID and PeriodID = @PeriodID 
    ";




        const string Query_GetDocumentGroups = @" select GroupID,GroupName from DocumentTypesGroup ";

        const string Query_GetDocumentGroupsByDocumentTypeID = @" select GroupID from SET_DocumentType where DocumentTypeID = @DocumentTypeID and companyid = @companyid and periodid = @periodid ";

        const string Query_GetDocumentTypesByGroupID = @" Select SET_DocumentType.DocumentTypeID, DocumentType, SET_DocumentType.CompanyID, 'True' as IsAssigned, UsersInDocumentType.DisallowBackDate,
        Set_Period.PeriodSetName, Set_Period.PeriodYear , SET_DocumentType.GroupID,UsersInDocumentType.AllowPreviousMonth,null as DisallowedMonths
    from SET_DocumentType 
        Inner Join Set_Period On Set_Period.PeriodID = SET_DocumentType.PeriodID AND Set_Period.Status = 1 AND Set_Period.IsActive = 1 
        left Join UsersInDocumentType ON UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID 
            AND UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID {1}
    Where SET_DocumentType.Status = 1  and SET_DocumentType.GroupID is not null
    {0}
    ORDER BY Set_Period.PeriodYear desc, SET_DocumentType.DocumentType asc";



        const string Query_GetDocumentTypesByGroupID2 = @" Select SET_DocumentType.DocumentTypeID, DocumentType, SET_DocumentType.CompanyID, case when UserID is null then  'false' else 'true' end as IsAssigned, UsersInDocumentType.DisallowBackDate,
        Set_Period.PeriodSetName, Set_Period.PeriodYear , SET_DocumentType.GroupID,UsersInDocumentType.AllowPreviousMonth,null as DisallowedMonths
    from SET_DocumentType 
        Inner Join Set_Period On Set_Period.PeriodID = SET_DocumentType.PeriodID AND Set_Period.Status = 1 AND Set_Period.IsActive = 1 
        left Join UsersInDocumentType ON UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID 
            AND UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID {1}
    Where SET_DocumentType.Status = 1  and SET_DocumentType.GroupID is not null
    {0}
    ORDER BY Set_Period.PeriodYear desc, SET_DocumentType.DocumentType asc";


        const string Query_GetDocumentTypesByGroupIDNew = @" Select distinct SET_DocumentType.DocumentTypeID, DocumentType, SET_DocumentType.CompanyID, case when UserID is null then  'false' else 'true' end as IsAssigned, UsersInDocumentType.DisallowBackDate,
        Set_Period.PeriodSetName, Set_Period.PeriodYear , SET_DocumentType.GroupID,isnull(UsersInDocumentType.AllowPreviousMonth,0) as AllowPreviousMonth,null as DisallowedMonths
    from SET_DocumentType 
        Inner Join Set_Period On Set_Period.PeriodID = SET_DocumentType.PeriodID AND Set_Period.Status = 1 AND Set_Period.IsActive = 1 
        left Join UsersInDocumentType ON UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID 
            AND UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID {1}
    Where SET_DocumentType.Status = 1  and SET_DocumentType.GroupID is not null
            --and AllowPreviousMonth is null or AllowPreviousMonth=0
            and isnull(AllowPreviousMonth,0) = 0 
    {0}
    ORDER BY Set_Period.PeriodYear desc, SET_DocumentType.DocumentType asc";


        const string Query_Get_UserDocumentGroups = @" Select Distinct Set_DocumentType.GroupID, DocumentTypesGroup.GroupName From UsersInDocumentType 
	Inner Join Set_DocumentType On Set_DocumentType.CompanyID = UsersInDocumentType.CompanyID And Set_DocumentType.DocumentTypeID = UsersInDocumentType.DocumentTypeID 
        And Set_DocumentType.IsActive = 1 And Set_DocumentType.Status = 1 
	Inner Join DocumentTypesGroup On  DocumentTypesGroup.GroupID = Set_DocumentType.GroupID
--DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And 
Where 
--UsersInDocumentType.CompanyID = @CompanyID And 
Set_DocumentType.PeriodID <= @PeriodID And UsersInDocumentType.UserID = @UserID 
order by 1
";

        const string Query_DocumentTypeLikeSearch = @" 
SELECT DISTINCT DocumentTypeID, DocumentType, DefaultAccountCode, SET_DocumentType.MenuID, TransactionImpact, TransactionType FROM SET_DocumentType 
WHERE Status = 1 AND IsActive = 1 AND CompanyID = @CompanyID AND PeriodID = @PeriodID AND DocumentType LIKE '%'  + @DocumentTypeName + '%' 
    AND (GroupID = @DocumentGroupID OR @DocumentGroupID IS NULL) ";
     
        const string Query_DocumentTypeLikeSearchForInvoice = @" 
SELECT DISTINCT DocumentTypeID, DocumentType, DefaultAccountCode, SET_DocumentType.MenuID, TransactionImpact, TransactionType FROM SET_DocumentType 
WHERE Status = 1 AND IsActive = 1 AND CompanyID = @CompanyID AND PeriodID = @PeriodID AND DocumentType LIKE '%'  + @DocumentTypeName + '%' 
    AND (GroupID = @DocumentGroupID OR @DocumentGroupID IS NULL) ";

        const string Query_DocumentTypeLikeSearchForSO = @" 
SELECT DISTINCT DocumentTypeID, DocumentType, DefaultAccountCode, SET_DocumentType.MenuID, TransactionImpact, TransactionType FROM SET_DocumentType 
WHERE Status = 1 AND IsActive = 1 AND CompanyID = @CompanyID AND PeriodID = @PeriodID AND DocumentType LIKE '%' + @DocumentTypeName + '%' AND DocumentType LIKE '%Sale%Order%' 
    AND (GroupID = @DocumentGroupID OR @DocumentGroupID IS NULL) ";

        const string Query_Get_UserDocumentType = @"
SELECT Distinct Set_DocumentType.DocumentTypeID,Set_DocumentType.DocumentType FROM UsersInDocumentType
		Inner Join Set_DocumentType On Set_DocumentType.CompanyID = UsersInDocumentType.CompanyID And Set_DocumentType.DocumentTypeID = UsersInDocumentType.DocumentTypeID 
        And Set_DocumentType.IsActive = 1 And Set_DocumentType.Status = 1  
		inner join GL_TransactionHeader on GL_TransactionHeader.DocumentTypeID = Set_DocumentType.DocumentTypeID
		and GL_TransactionHeader.CompanyID = Set_DocumentType.CompanyID
		and GL_TransactionHeader.status = 1
where  

UsersInDocumentType.CompanyID = @CompanyID And Set_DocumentType.PeriodID = @PeriodID 
And UsersInDocumentType.UserID = @UserID and IsActive = @IsActive and Set_DocumentType.Status = 1 
--and SET_DocumentType.MenuID in 
--(
--    SELECT MenuID FROM ApplicationMenu WHERE MenuPath in ('/Sales/Dispatch_frm.aspx', '/AR/Transaction/Receipt_Transaction_frm.aspx', '/Sales/Sales_Invoice_frm.aspx', 
--        '/GLForms/Transaction/GL_Transaction_frm.aspx', '/AP/Transaction/Payment.aspx', '/Inventory/Transaction/Stock_frm.aspx', 
--        '/AR/Transaction/Invoice_Transaction_frm.aspx', '/Cane/Transaction/DepotCPR.aspx', '/Cane/Transaction/SecondWeight.aspx', 
--        '/Inventory/Transaction/StockReturn.aspx','/AP/Transaction/DirectInvoicePayment.aspx', '/HRMS/Payroll/Transaction/EmployeeBonusPost_frm.aspx',
--        '/HRMS/Payroll/Transaction/EmployeeOverTimePost_frm.aspx','/HRMS/Payroll/Transaction/ScrollPayment.aspx','/HRMS/Payroll/Transaction/EmployeeBonusPosting_frm.aspx'
--,'/HRMS/Payroll/Transaction/ScrollPaymentBonus.aspx','/HRMS/Payroll/Transaction/ScrollPaymentOvertime.aspx','/HRMS/Payroll/Transaction/EmployeeOvertimePosting_frm.aspx'
--,'/ALImdad/Transaction/ImportDocument_frm.aspx','/Inventory/Transaction/Stock_frm.aspx','/ALImdad/Transaction/CustomClearing_frm.aspx'
--,'/ALImdad/Transaction/DirectLCExpense_frm.aspx','/ALImdad/SetupForms/ContainerTransportationandUnloading_frm.aspx'
--,'/ALImdad/SetupForms/WeightAssessment_frm.aspx'
--,'/HRMS/Payroll/Transaction/EmployeeOverTimePost_frm.aspx'
--,'/HRMS/Payroll/transaction/ScrollPaymentOvertime.aspx'
--        )
--)

ORDER BY DocumentType 
";


        const string Query_GetByUserName = @"select * from (

SELECT  UsersInDocumentType.CompanyID, UsersInDocumentType.Documenttypeid,DocumentType,SET_DocumentType .GroupID,case when DisallowBackDate = 1 then 'True' else 'False' end as DisallowBackDate,  CONVERT(BIT,'True')AS Checked
from UsersInDocumentType
inner join SET_DocumentType on SET_DocumentType .DocumentTypeID = UsersInDocumentType.Documenttypeid
and SET_DocumentType.CompanyID = UsersInDocumentType.CompanyID
WHERE UsersInDocumentType.CompanyID = @CompanyID and  UserID = @UserID
and SET_DocumentType.GroupID = @GroupID

union all

SELECT SET_DocumentType .CompanyID,SET_DocumentType.DocumentTypeID, DocumentType, SET_DocumentType .GroupID,'False' as DisallowBackDate,CONVERT(BIT,'False')AS Checked
from SET_DocumentType 
WHERE SET_DocumentType.CompanyID = @CompanyID
and SET_DocumentType .DocumentTypeID not in (select DocumentTypeID from UsersInDocumentType where UserID = @UserID and CompanyID = @CompanyID)
and SET_DocumentType.GroupID = @GroupID
)as tab
order by Documenttypeid

 ";
        #endregion

        #region Class Methods

        public static DataTable GETDocumentTypeForSalaryForm(string query = "")
        {
            DataTable dtToReturn = null;

            try
            {
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }

            return dtToReturn;
        }


        public static DataTable SelectDocumentTypeOnTheBasesChecqs(Guid? username)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@UserId", username);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectDocumentTypeOnTheBasesChecqs, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }

            return dtToReturn;
        }

        public static DataTable SelectAllData(int companyID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllData, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }

            return dtToReturn;
        }

        public static DataTable SelectAllTransactionType(int companyID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_AllTransactionTypes, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }

            return dtToReturn;
        }

        public static DataTable SelectAllDataNOT(Guid userid, string dt, int companyID)
        {
            DataTable dtToReturn = null;

            try
            {
                string strWhereRoleids = !string.IsNullOrEmpty(dt) ? " And SET_DocumentType.DocumentTypeID NOT IN (" + dt + ")" : string.Empty;

                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@UserID", userid);
                sqlparams[1] = new SqlParameter("@dt", dt);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectAllDataNOT, strWhereRoleids), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }

            return dtToReturn;
        }

        public static bool InsertUpdate_DocType(int documentTypeID, SET_DocumentType objDocumentType, List<SET_DocumentSequence> lstDocumentSequences)
        {
            bool result = false;
            SqlTransaction objTransaction = SqlHelper.GetTransaction("InsertUpdate_DocType");

            try
            {
                DataRow drDocumentType = SET_DocumentType.GetDataByKey(objDocumentType.CompanyID, documentTypeID, objDocumentType.PeriodID);

                if (drDocumentType == null)
                    objDocumentType.Insert(objTransaction);
                else
                    objDocumentType.UpdateDataByKey(objTransaction);

                //  Delete all Document Sequence records against this Document Type and then insert again
                if (drDocumentType != null)
                    SET_DocumentSequence.DeleteDataByKey(Convert.ToInt32(drDocumentType["DocumentTypeID"]), objTransaction);

                foreach (SET_DocumentSequence DSeq in lstDocumentSequences)
                {
                    DSeq.DocumentSequenceID = SET_DocumentSequence.GetNewDocumentSequenceID(objTransaction);
                    DSeq.Insert(objTransaction);
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
                    SqlHelper.CommitTransaction(objTransaction);
                else
                    SqlHelper.RollBackTransaction(objTransaction, "InsertUpdate_DocType");
            }

            return result;
        }

        public static DataTable GetAllDataBy(int companyID, int periodID, Nullable<int> chartOfAccountID, Nullable<int> documentTypeID = null, bool isActive = true,
            string transactionImpact = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@IsActive", isActive);

                if (chartOfAccountID.HasValue)
                    sqlparams[3] = new SqlParameter("@ChartOfAccountID", chartOfAccountID.Value);
                else
                    sqlparams[3] = new SqlParameter("@ChartOfAccountID", DBNull.Value);

                if (documentTypeID.HasValue && documentTypeID.Value > 0)
                    sqlparams[4] = new SqlParameter("@DocumentTypeID", documentTypeID.Value);
                else
                    sqlparams[4] = new SqlParameter("@DocumentTypeID", DBNull.Value);

                if (!string.IsNullOrEmpty(transactionImpact))
                    sqlparams[5] = new SqlParameter("@TransactionImpact", transactionImpact);
                else
                    sqlparams[5] = new SqlParameter("@TransactionImpact", DBNull.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_ByCompanyAndPeriodIDs, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }

            return dtToReturn;
        }

        public static DataTable GetAllDataByMenuID(int companyID, int periodID, int menuID, SqlTransaction sqlTransaction = null, bool isActive = true)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@MenuID", menuID);
                sqlparams[3] = new SqlParameter("@IsActive", isActive);

                DataSet ds = null;
                if (sqlTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_ByMenuID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(sqlTransaction, CommandType.Text, Query_SelectAll_ByMenuID, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }

            return dtToReturn;
        }

        public static DataTable GetAllDataByMenuIDSecurityOnDocumentType(int companyID, int periodID, int menuID, Guid userid, SqlTransaction sqlTransaction = null,
            bool isActive = true)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@MenuID", menuID);
                sqlparams[3] = new SqlParameter("@IsActive", isActive);
                sqlparams[4] = new SqlParameter("@UserID", userid);

                DataSet ds = null;
                if (sqlTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_byMenuUDSecurityOnDocumentType, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(sqlTransaction, CommandType.Text, Query_SelectAll_byMenuUDSecurityOnDocumentType, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }

            return dtToReturn;
        }

        public static DataTable GetDataByMenu(int companyID, int periodID, int menuID, bool isActive = true)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@MenuID", menuID);
                sqlparams[3] = new SqlParameter("@IsActive", isActive);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectVoucherDocumentType, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }

            return dtToReturn;
        }

        public static DataTable GetDataForVoucher(int companyID, int periodID, bool isActive = true)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                //sqlparams[2] = new SqlParameter("@MenuID", menuID);
                sqlparams[2] = new SqlParameter("@IsActive", isActive);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectVoucherDocumentTypeForSearch, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }

            return dtToReturn;
        }

        public static bool IsValidAccountCode(string defaultAccountCode, string transactionType, int documentTypeID)
        {
            bool isValidAccCode = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@DefaultAccountCode", defaultAccountCode);
                sqlparams[1] = new SqlParameter("@TransactionType", transactionType);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);

                if (SqlHelper.ExecuteDataset(CommandType.Text, Query_IsValidAccountCode, sqlparams).Tables[0].Rows.Count > 0)
                    throw new BusinessLogicException("Invalid transaction code or transaction type");

                isValidAccCode = true;
            }
            catch (BusinessLogicException bex)
            {
                SqlHelper.LogException(bex);
                throw bex;
            }
            catch (Exception ex) { throw ex; }

            return isValidAccCode;
        }

        public static int GetMenuID()
        {
            try
            {
                int MenuID;

                MenuID = Convert.ToInt32(SqlHelper.ExecuteDataset(CommandType.Text, GetMenuIDByQuery).Tables[0].Rows[0]["MenuID"].ToString());

                return MenuID;
            }
            catch (BusinessLogicException bex)
            {
                SqlHelper.LogException(bex);
                throw bex;
            }
            catch (Exception ex) { throw ex; }
        }

        public static int GetMenuID_PI()
        {
            try
            {
                int MenuID;

                MenuID = Convert.ToInt32(SqlHelper.ExecuteDataset(CommandType.Text, GetMenuIDByQuery_PI).Tables[0].Rows[0]["MenuID"].ToString());

                return MenuID;
            }
            catch (BusinessLogicException bex)
            {
                SqlHelper.LogException(bex);
                throw bex;
            }
            catch (Exception ex) { throw ex; }
        }

        public static int GetMenuID_PO(bool ISVehicle = false)
        {
            try
            {
                int MenuID;
                if (!ISVehicle)
                    MenuID = Convert.ToInt32(SqlHelper.ExecuteDataset(CommandType.Text, GetMenuIDByQuery_PO).Tables[0].Rows[0]["MenuID"].ToString());
                else
                    MenuID = Convert.ToInt32(SqlHelper.ExecuteDataset(CommandType.Text, GetMenuIDByQuery_POVehicle).Tables[0].Rows[0]["MenuID"].ToString());

                return MenuID;
            }
            catch (BusinessLogicException bex)
            {
                SqlHelper.LogException(bex);
                throw bex;
            }
            catch (Exception ex) { throw ex; }
        }

        public static DataTable GetDataByKey_WithOtherTables(int documentTypeID, int companyID, int periodID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_ByKey_WithOtherTables, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAutoEntryGeneratingDocument(int companyID, int periodID, string documentType = null, string accountCode = null,short? DocummentGroupID=null)
        {
            DataTable dt = null;

            try
            {
                StringBuilder sb = new StringBuilder();

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (!string.IsNullOrEmpty(documentType))
                    sqlparams[2] = new SqlParameter("@DocumentType", documentType);
                else
                    sqlparams[2] = new SqlParameter("@DocumentType", DBNull.Value);

                if (!string.IsNullOrEmpty(accountCode))
                    sqlparams[3] = new SqlParameter("@AccountCode", accountCode);
                else
                    sqlparams[3] = new SqlParameter("@AccountCode", DBNull.Value);

                if (DocummentGroupID.HasValue)
                    sb.Append(" AND dt.GroupID = " + DocummentGroupID);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAutoEntryGeneratingDocumentReport,sb.ToString()), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetNonUserDefinedTypes(int companyID, int periodID, string documentTypeName = null, string menuPath = null,
            SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataSet ds = null;
            DataTable dtToReturn = null;
            StringBuilder strWhere = new StringBuilder();
            string appMenuJoin = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (!string.IsNullOrEmpty(documentTypeName))
                    strWhere.Append(" AND DocumentType LIKE '%" + documentTypeName + "%'");

                if (!string.IsNullOrEmpty(menuPath))
                {
                    appMenuJoin = " Inner Join ApplicationMenu On ApplicationMenu.MenuID = Set_DocumentType.MenuID ";
                    strWhere.Append(" AND ApplicationMenu.MenuPath = '" + menuPath + "'");
                }

                if (objTransaction != null)
                {
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_NonUserDefinedTypes, appMenuJoin) + strWhere.ToString(),
                        sqlparams);
                }
                else if (objConnection != null)
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_NonUserDefinedTypes, appMenuJoin) + strWhere.ToString(),
                        sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_NonUserDefinedTypes, appMenuJoin) + strWhere.ToString(), sqlparams);

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

        public static DataTable GetNonUserDefinedTypesForLoan(int companyID, int periodID, string documentTypeName = null, string menuPath = null,
           SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataSet ds = null;
            DataTable dtToReturn = null;
            StringBuilder strWhere = new StringBuilder();
            string appMenuJoin = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (!string.IsNullOrEmpty(documentTypeName))
                    strWhere.Append(" AND DocumentType like '%loan sanction%' ");

                if (!string.IsNullOrEmpty(menuPath))
                {
                    appMenuJoin = " Inner Join ApplicationMenu On ApplicationMenu.MenuID = Set_DocumentType.MenuID ";
                    strWhere.Append(" AND ApplicationMenu.MenuPath = '" + menuPath + "'");
                }

                if (objTransaction != null)
                {
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_NonUserDefinedTypes, appMenuJoin) + strWhere.ToString(),
                        sqlparams);
                }
                else if (objConnection != null)
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_NonUserDefinedTypes, appMenuJoin) + strWhere.ToString(),
                        sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_NonUserDefinedTypes, appMenuJoin) + strWhere.ToString(), sqlparams);

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

        public static DataTable GetDocumentTypeWithLikeDocumentType(int companyID, int periodID, string documentTypeName, Nullable<int> documentGroupID = null)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeName", documentTypeName);

                if (documentGroupID.HasValue)
                    sqlparams[3] = new SqlParameter("@DocumentGroupID", documentGroupID.Value);
                else
                    sqlparams[3] = new SqlParameter("@DocumentGroupID", DBNull.Value);

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_DocumentTypeLikeSearch, sqlparams);

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

        public static DataRow GetDocumentTypeLikeSearch(int companyID, int periodID, string documentTypeName, Nullable<int> documentGroupID = null)
        {
            DataSet ds = null;
            DataRow drToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeName", documentTypeName);

                if(documentGroupID.HasValue)
                    sqlparams[3] = new SqlParameter("@DocumentGroupID", documentGroupID.Value);
                else
                    sqlparams[3] = new SqlParameter("@DocumentGroupID", DBNull.Value);

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_DocumentTypeLikeSearch, sqlparams);

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

        public static DataRow GetDocumentTypeLikeSearchForInvoice(int companyID, int periodID, string documentTypeName, Nullable<int> documentGroupID = null)
        {
            DataSet ds = null;
            DataRow drToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeName", documentTypeName);

                if (documentGroupID.HasValue)
                    sqlparams[3] = new SqlParameter("@DocumentGroupID", documentGroupID.Value);
                else
                    sqlparams[3] = new SqlParameter("@DocumentGroupID", DBNull.Value);

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_DocumentTypeLikeSearchForInvoice, sqlparams);

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
        public static DataTable GetAllDocumentTypesForReceiptAndPayment(int companyID, int periodID, string MenuPath)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectDocumentTypesFor_Receipt, MenuPath), sqlparams);

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

        public static DataTable GetAllDocumentTypesForDirectInvoices(int companyID, int periodID, string MenuPath)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectDocumentTypesFor_DirectInvoices, MenuPath), sqlparams);

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

        public static DataTable GetAllDocumentTypesForReceipt(int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_DocumentTypeReciept), sqlparams);

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

        public static DataTable GetAllDocumentTypesForPayment(int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_DocumentTypePayment), sqlparams);

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

        public static int GetAllCISDefinedDocumentTypesByMenuPath(int companyID, int periodID, string MenuPath, SqlTransaction sqlTran = null)
        {
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@UserDefined", false);

                DataSet ds = null;

                if (sqlTran != null)
                    ds = SqlHelper.ExecuteDataset(sqlTran, CommandType.Text, string.Format(Query_GetCISDocumentTypesByMenuPath, MenuPath), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetCISDocumentTypesByMenuPath, MenuPath), sqlparams);

                if ((ds != null && ds.Tables.Count > 0) ? ds.Tables[0].Rows.Count > 0 : false)
                    return int.Parse(ds.Tables[0].Rows[0]["DocumentTypeID"].ToString());
                else
                    throw new Exception(" CIS document type is not defined.");
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
        }

        public static DataTable GetDocumentTypeByMenuPath(int companyID, int periodID, string FilterMenuPath, string Query)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query + FilterMenuPath), sqlparams);

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

        public static DataTable GetDocumentTypeByMenuPathForCentralizedDB(int companyID, int periodID, string FilterMenuPath, string Query, 
            SqlTransaction sqlTran = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;
                if (sqlTran != null)
                    ds = SqlHelper.ExecuteDataset(sqlTran, CommandType.Text, string.Format(Query + FilterMenuPath), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query + FilterMenuPath), sqlparams);

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

        public static DataTable GetAllDocumentTypesByMenuPath(int companyID, int periodID, string menuPath, SqlTransaction sqlTran = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (sqlTran != null)
                    ds = SqlHelper.ExecuteDataset(sqlTran, CommandType.Text, string.Format(Query_GetAllByMenuPath, menuPath), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAllByMenuPath, menuPath), sqlparams);

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

        public static DataTable GetAllDocumentTypesByMenuPathAndDocType(int companyID, int periodID, string menuPath, string Where,  SqlTransaction sqlTran = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (sqlTran != null)
                    ds = SqlHelper.ExecuteDataset(sqlTran, CommandType.Text, string.Format(Query_GetAllByMenuPathAndDocType, menuPath, Where), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAllByMenuPathAndDocType, menuPath, Where), sqlparams);

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


        public static DataTable GetAllDocumentTypesByMenuPathAndDispatchDocType(int companyID, int periodID, string menuPath, string DispatchDocType, SqlTransaction sqlTran = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (sqlTran != null)
                    ds = SqlHelper.ExecuteDataset(sqlTran, CommandType.Text, string.Format(Query_GetAllByMenuPathAndDispatchDocType, menuPath, DispatchDocType), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAllByMenuPathAndDispatchDocType, menuPath, DispatchDocType), sqlparams);

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


        public static DataTable GetAllDocumentTypesByMenuPath_forIT(int companyID, int periodID, string menuPath, SqlTransaction sqlTran = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (sqlTran != null)
                    ds = SqlHelper.ExecuteDataset(sqlTran, CommandType.Text, string.Format(Query_GetAllByMenuPath_forIT, menuPath), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAllByMenuPath_forIT, menuPath), sqlparams);

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


        public static DataTable GetAllDocumentTypesByMenuPath(int companyID, int periodID, string menuPath, Guid userID, SqlTransaction obj_SQL_Transaction = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@UserID", userID);

                DataSet ds = null;

                if (obj_SQL_Transaction != null)
                    ds = SqlHelper.ExecuteDataset(obj_SQL_Transaction, CommandType.Text, string.Format(Query_GetAllAllowedByMenuPath, menuPath), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAllAllowedByMenuPath, menuPath), sqlparams);

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

        public static bool GetAllIsBackDateAllow(int CompanyID, int DocumentTypeID, Guid UserID, SqlConnection obj_SQLConnection = null)
        {
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[2] = new SqlParameter("@UserID", UserID);

                if (obj_SQLConnection != null)
                    ds = SqlHelper.ExecuteDataset(obj_SQLConnection, CommandType.Text, Query_IsBackDateAllow, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_IsBackDateAllow, sqlparams);

                if ((ds != null && ds.Tables.Count > 0) ? ds.Tables[0].Rows.Count > 0 : false)
                    return bool.Parse(ds.Tables[0].Rows[0]["DisallowBackDate"].ToString());
                else
                    return false;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
        }

        public static bool GetAllBlockedMonths(int CompanyID, int DocumentTypeID,int MonthID, Guid UserID, SqlConnection obj_SQLConnection = null)
        {
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[2] = new SqlParameter("@UserID", UserID);
                sqlparams[3] = new SqlParameter("@MonthID", MonthID);

                string Query = @"Select MonthID from UsersDocumentsMonthsBlocked 
                    Where UserID = @UserID And CompanyID = @CompanyID And Documenttypeid = @Documenttypeid and MonthID = @MonthID";

                if (obj_SQLConnection != null)
                    ds = SqlHelper.ExecuteDataset(obj_SQLConnection, CommandType.Text, Query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);

                if ((ds != null && ds.Tables.Count > 0) ? ds.Tables[0].Rows.Count > 0 : false)
                    return ds.Tables[0].Rows[0]["MonthID"].ToString() != "" ? true : false;
                else
                    return false;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
        }

        public static bool GetAllIsAllowPreviousMonth(int CompanyID, int DocumentTypeID, Guid UserID, SqlConnection obj_SQLConnection = null)
        {
            DataSet ds = null;
            bool isAllowed = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[2] = new SqlParameter("@UserID", UserID);

                if (obj_SQLConnection != null)
                    ds = SqlHelper.ExecuteDataset(obj_SQLConnection, CommandType.Text, Query_IsBackDateAllow, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_IsAllowPreviousMonth, sqlparams);

                if ((ds != null && ds.Tables.Count > 0) ? ds.Tables[0].Rows.Count > 0 : false)
                    isAllowed = ds.Tables[0].Rows[0]["AllowPreviousMonth"].ToString()!=""?false:true;

                return isAllowed;
                
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
        }

        public static int GetFormsMenuID(string path)
        {
            try
            {
                int MenuID;

                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@menupath", path);

                MenuID = Convert.ToInt32(SqlHelper.ExecuteDataset(CommandType.Text, GetformsMenuIDByQuery, sqlparams).Tables[0].Rows[0]["MenuID"].ToString());

                return MenuID;
            }
            catch (BusinessLogicException bex)
            {
                SqlHelper.LogException(bex);
                throw bex;
            }
            catch (Exception ex) { throw ex; }
        }

        public static int GetFormsEnabledMenuID(string path)
        {
            try
            {
                int MenuID;

                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@menupath", path);

                MenuID = Convert.ToInt32(SqlHelper.ExecuteDataset(CommandType.Text, GetformsEnabledMenuIDByQuery, sqlparams).Tables[0].Rows[0]["MenuID"].ToString());

                return MenuID;
            }
            catch (BusinessLogicException bex)
            {
                SqlHelper.LogException(bex);
                throw bex;
            }
            catch (Exception ex) { throw ex; }
        }

        public static DataTable GetAllActiveDataWithID(Nullable<int> companyID, Nullable<int> periodID, Nullable<bool> isActive = true, 
            SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (isActive.HasValue)
                    sqlparams[2] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[2] = new SqlParameter("@IsActive", DBNull.Value);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAllWithID, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAllWithID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllWithID, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllActiveDataWithDocumentID(Nullable<int> companyID, Nullable<int> periodID, Nullable<bool> isActive = true,
            SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (isActive.HasValue)
                    sqlparams[2] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[2] = new SqlParameter("@IsActive", DBNull.Value);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAllWithDocumentID, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAllWithDocumentID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllWithDocumentID, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static int GetNewDocumentTypeIDWithMinMaxLimit(int companyID, int periodID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_MaxId_WithMinMaxLimit, sqlparams));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_Get_MaxId_WithMinMaxLimit, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_MaxId_WithMinMaxLimit, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static DataTable GetDocumentGroups(Nullable<int> companyID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetDocumentGroups, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDocumentGroups, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDocumentGroups, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable SelectDocumenTypeByGroupID(string userid, string Ids, int companyID, int GroupID)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            try
            {
                string strWhereRoleids = "";
                if (Ids != "")
                    sb.Append(" And SET_DocumentType.DocumentTypeID IN (" + Ids + ")");

                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@UserID", userid);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@GroupID", GroupID);

                if (GroupID > 0)
                    sb.Append(" AND SET_DocumentType.GroupID = @GroupID");
                if (companyID > 0)
                    sb.Append(" AND SET_DocumentType.CompanyID = @CompanyID");

                //if (userid != "")
                //    sb1.Append("and UsersInDocumentType.userid = @UserID");

                //DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDocumentTypesByGroupID, sb.ToString(),sb1.ToString()), sqlparams);
                DataSet ds;
                if (userid != "")
                {
                    sb1.Append("and UsersInDocumentType.userid = @UserID");
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDocumentTypesByGroupID2, sb.ToString(), sb1.ToString()), sqlparams);
                }
                else
                {
                    //sb1.Append("and UsersInDocumentType.userid = @UserID");
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDocumentTypesByGroupIDNew, sb.ToString(), sb1.ToString()), sqlparams);
                }

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }

            return dtToReturn;
        }

        public static DataTable SelectDocumenType(string userid, string Ids, int companyID)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            try
            {
                string strWhereRoleids = "";
                if (Ids != "")
                    sb.Append(" And SET_DocumentType.DocumentTypeID IN (" + Ids + ")");

                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@UserID", userid);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);

                if (companyID > 0)
                    sb.Append(" AND SET_DocumentType.CompanyID = @CompanyID");

                //if (userid != "")
                //    sb1.Append("and UsersInDocumentType.userid = @UserID");

                //DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDocumentTypesByGroupID, sb.ToString(),sb1.ToString()), sqlparams);
                DataSet ds;
                if (userid != "")
                {
                    sb1.Append("and UsersInDocumentType.userid = @UserID");
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDocumentTypesByGroupID2, sb.ToString(), sb1.ToString()), sqlparams);
                }
                else
                {
                    //sb1.Append("and UsersInDocumentType.userid = @UserID");
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDocumentTypesByGroupIDNew, sb.ToString(), sb1.ToString()), sqlparams);
                }

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }

            return dtToReturn;
        }


        public static DataTable Get_UserDocumentGroups(int companyID, int periodID, Guid userID, SqlConnection obj_SQLConnection = null)
        {
            DataSet ds = null;
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@UserID", userID);

                if(obj_SQLConnection != null)
                    ds = SqlHelper.ExecuteDataset(obj_SQLConnection, CommandType.Text, Query_Get_UserDocumentGroups, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_UserDocumentGroups, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }

            return dtToReturn;
        }

        public static DataTable Get_DocumentGroupsByDocumentTypeID(int companyID, int periodID, int DocumentTypeID, SqlConnection obj_SQLConnection = null)
        {
            DataSet ds = null;
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                


                if (obj_SQLConnection != null)
                    ds = SqlHelper.ExecuteDataset(obj_SQLConnection, CommandType.Text, Query_GetDocumentGroupsByDocumentTypeID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDocumentGroupsByDocumentTypeID, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }

            return dtToReturn;
        }

        public static DataTable GetAllDocumentTypesForDICustomer(int companyID, int periodID, string menuPath, SqlTransaction sqlTran = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;
                #region Query
                const string Query_GetAllByMenuPathForDIInoice = @"SELECT DocumentTypeID, DocumentType FROM SET_DocumentType 
WHERE SET_DocumentType.CompanyID = @CompanyID AND SET_DocumentType.PeriodID = @PeriodID 
    AND SET_DocumentType.MenuID in (SELECT MenuID FROM ApplicationMenu WHERE MenuPath in ({0})) AND SET_DocumentType.IsActive = 1 AND SET_DocumentType.Status = 1 
   AND DocumentType  not like '%Sales Invoice%'
ORDER BY DocumentType asc";

                #endregion

                if (sqlTran != null)
                    ds = SqlHelper.ExecuteDataset(sqlTran, CommandType.Text, string.Format(Query_GetAllByMenuPathForDIInoice, menuPath), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAllByMenuPathForDIInoice, menuPath), sqlparams);

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

        #endregion

        public static DataTable GetDailyTransactionDocuments(int CompanyID, DateTime dateTime)
        {
            DataTable dtToReturn = null;
           DateTime Date_From = DateUtil.ConvertToDate(DateUtil.ConvertToString(dateTime) + " 00:00:00");
           DateTime Date_To= DateUtil.ConvertToDate(DateUtil.ConvertToString(dateTime) + " 23:59:59");
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@FromDate", Date_From);
                sqlparams[2] = new SqlParameter("@ToDate", Date_To);

                #region
                const string Query_SelectDailyTransactionData = @"
 SELECT Distinct SET_DocumentType.DocumentTypeID, SET_DocumentType.DocumentType
 FROM SET_DocumentType 
 INNER JOIN GL_TransactionHeader On GL_TransactionHeader.DocumentTypeID=SET_DocumentType.DocumentTypeID
 AND GL_TransactionHeader.CompanyID=SET_DocumentType.CompanyID
 AND GL_TransactionHeader.Status=1
 AND GL_TransactionHeader.DocumentDate between @FromDate and @ToDate 
 where
SET_DocumentType.CompanyID=@CompanyID 
AND SET_DocumentType.IsActive = 1 AND SET_DocumentType.Status = 1 
ORDER BY DocumentType asc ";
                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectDailyTransactionData, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }

            return dtToReturn;
        }

        public static DataTable Get_UserDocumentTypeForVoucher(int companyID, int periodID, Guid userID, bool isActive = true)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@IsActive", isActive);
                sqlparams[3] = new SqlParameter("@UserID", userID);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_UserDocumentType, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }

            return dtToReturn;
        }

        public static DataTable GetFormsMenuByDocumentTypeID(int DocumentTypeID, int CompanyID, int PeriodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                #region
                const string Query_SelectDailyTransactionData = @" Select * from ApplicationMenu 
	Inner Join SET_DocumentType  on SET_DocumentType.MenuID=ApplicationMenu.MenuID
 where SET_DocumentType.Status=1 
AND SET_DocumentType.CompanyID=@CompanyID
 AND SET_DocumentType.PeriodID=@PeriodID  
AND SET_DocumentType.DocumentTypeID=@DocumentTypeID
 ";
                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectDailyTransactionData, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }

            return dtToReturn;
        }

        public static DataTable GetNegativeDocumentTypes(int CompanyID, bool IsRaniPur = false)
        {
            DataTable dtToReturn = null;
            try
            {
                StringBuilder sb = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);

                if (IsRaniPur)
                    sb.Append("and SET_DocumentType.CompanyID = @CompanyID");

                #region
                const string GetNegativeDocumentTypes = @"
select SET_DocumentType.DocumentType,SET_DocumentType.DocumentTypeID ,count(INV_StockTransactionHeader.StockTransactionID)
from INV_StockTransactionHeader
inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID and  TransactionImpact =  'Negative'
where SET_DocumentType.Status = 1
and INV_StockTransactionHeader.Status = 1
{0}
group by SET_DocumentType.DocumentType ,SET_DocumentType.DocumentTypeID
--having count(INV_StockTransactionHeader.StockTransactionID) > 5

  ";
                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(GetNegativeDocumentTypes, sb.ToString()), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }

            return dtToReturn;
        }

        public static DataTable GetPositiveDocumentTypes(int CompanyID, bool IsRaniPur = false)
        {
            DataTable dtToReturn = null;
            try
            {
                StringBuilder sb = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);

                if (IsRaniPur)
                    sb.Append("and SET_DocumentType.CompanyID = @CompanyID");

                #region
                const string Query_GetPositiveDocumentTypes = @"
select SET_DocumentType.DocumentType,SET_DocumentType.DocumentTypeID ,count(INV_StockTransactionHeader.StockTransactionID)
from INV_StockTransactionHeader
inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID and  TransactionImpact =  'Positive'
where SET_DocumentType.Status = 1
and INV_StockTransactionHeader.Status = 1
group by SET_DocumentType.DocumentType ,SET_DocumentType.DocumentTypeID
--having count(INV_StockTransactionHeader.StockTransactionID) > 5

  ";
                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetPositiveDocumentTypes, sb), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }

            return dtToReturn;
        }

        public static DataTable GetOpeningDocumentTypes(int CompanyID, bool IsRaniPur = false)
        {
            DataTable dtToReturn = null;
            try
            {
                StringBuilder sb = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);

                if (IsRaniPur)
                    sb.Append("and SET_DocumentType.CompanyID = @CompanyID");

                #region
                const string GetOpeningDocumentTypes = @"
select SET_DocumentType.DocumentType,SET_DocumentType.DocumentTypeID 
from SET_DocumentType
where SET_DocumentType.Status = 1
{0}
And SET_DocumentType.DocumentTypeID in (39,53,54,114,128,129)
  ";
                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(GetOpeningDocumentTypes, sb), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }

            return dtToReturn;
        }

        public static DataTable GetDataByMenuID(int companyID, int periodID, int menuID, Guid CurrentUserID, bool isActive = true)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@MenuID", menuID);
                sqlparams[3] = new SqlParameter("@IsActive", isActive);
                sqlparams[4] = new SqlParameter("@UserID", CurrentUserID);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_byMenuUDSecurityOnDocumentType, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }

            return dtToReturn;
        }

        public static DataTable GetDataByDocTypeName(int companyID, int periodID, string doctype,int FiscalID, SqlTransaction sqlTran = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                

                DataSet ds = null;
                #region Query
                const string Query_GetAllByMenuPathForDIInoice = @"  Select * from Set_DocumentType where Status = 1 and COmpanyID = @CompanyID
and PeriodID = @PeriodID 
{0} ";

                #endregion

                if (sqlTran != null)
                    ds = SqlHelper.ExecuteDataset(sqlTran, CommandType.Text, string.Format(Query_GetAllByMenuPathForDIInoice, doctype), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAllByMenuPathForDIInoice, doctype), sqlparams);

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

        public static DataTable GetDashBoardDocumentTypes(int CompanyID)
        {
            DataTable dtToReturn = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);

                #region
                const string GetOpeningDocumentTypes = @"select SET_DocumentType.DocumentType,SET_DocumentType.DocumentTypeID 
from SET_DocumentType
where SET_DocumentType.Status = 1
And SET_DocumentType.DocumentType like 'Gate%Sugar'
OR SET_DocumentType.DocumentType like 'Gate%scrap'
OR SET_DocumentType.DocumentType like 'Gate%Molasses'
OR SET_DocumentType.DocumentType like 'Gate%Bagasse'
  ";
                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, GetOpeningDocumentTypes, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }

            return dtToReturn;
        }


        public static DataTable GetDataByUser(Guid UserID, int CompanyID, int GroupID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@UserID", UserID);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[2] = new SqlParameter("@GroupID", GroupID); 

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetByUserName, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static bool Assign_DeleteDataByKey(SqlTransaction objTransaction, Guid UserID, int CompanyID, int GroupID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@UserID", UserID);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[2] = new SqlParameter("@GroupID", GroupID);

                const string Query_Delete_ByKey = @" delete w from UsersInDocumentType w
inner join SET_DocumentType on SET_DocumentType .DocumentTypeID = w.Documenttypeid
and SET_DocumentType.CompanyID = w.CompanyID
where w.CompanyID = @CompanyID and  UserID = @UserID  and SET_DocumentType.GroupID = @GroupID ";

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByKey, sqlparams) > 0)
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

        public static object Get_DocumentTypeUserAssignment(int CurrentCompanyID, string query_DocumentTypeUserAssignment, Nullable<Guid> userID = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", CurrentCompanyID);
                

                if (userID.HasValue)
                    sqlparams[1] = new SqlParameter("@UserID", userID);
                else
                    sqlparams[1] = new SqlParameter("@UserID", DBNull.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query_DocumentTypeUserAssignment, sqlparams);
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dt = ds.Tables[0];

                //      if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                //      {
                //          dt = ds.Tables[0];
                //          dt = dt.AsEnumerable()
                //.Where(row => row.Field<int>("CompanyID") == companyID)
                //.CopyToDataTable();
                //      }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDocumentTypesByMenuPath_ForHabib(int companyID, int periodID, string menuPath, SqlTransaction sqlTran = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;


                const string Query_GetAllByMenuPath = @" SELECT DocumentTypeID, DocumentType FROM SET_DocumentType 
WHERE SET_DocumentType.CompanyID = @CompanyID AND SET_DocumentType.PeriodID = @PeriodID 
    AND SET_DocumentType.MenuID in (SELECT MenuID FROM ApplicationMenu WHERE MenuPath in ({0})) AND SET_DocumentType.IsActive = 1 AND SET_DocumentType.Status = 1 
and SET_DocumentType.DocumentType not like '%CO2%' and SET_DocumentType.DocumentType not like '%Ethanol%' and SET_DocumentType.DocumentType not like '%Export%'
ORDER BY DocumentType asc ";


                if (sqlTran != null)
                    ds = SqlHelper.ExecuteDataset(sqlTran, CommandType.Text, string.Format(Query_GetAllByMenuPath, menuPath), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAllByMenuPath, menuPath), sqlparams);

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

        public static DataRow GetDocumentTypeLikeSearchForSO(int companyID, int periodID, string documentTypeName, Nullable<int> documentGroupID = null)
        {
            DataSet ds = null;
            DataRow drToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeName", documentTypeName);

                if (documentGroupID.HasValue)
                    sqlparams[3] = new SqlParameter("@DocumentGroupID", documentGroupID.Value);
                else
                    sqlparams[3] = new SqlParameter("@DocumentGroupID", DBNull.Value);

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_DocumentTypeLikeSearchForSO, sqlparams);

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

        public static DataTable GetAllDataByForInventory(int companyID, int periodID, SqlTransaction sqlTransaction = null, bool isActive = true)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@IsActive", isActive);

                #region Query
                const string Query_SelectAll_ForInventoryDocOnly = @"
SELECT * FROM SET_DocumentType 
where SET_DocumentType.MenuID in 
(
    SELECT MenuID FROM ApplicationMenu WHERE MenuPath in (
                 	 --- For STORE -----
	   '/Inventory/Transaction/StockRequistion_frm.aspx', '/Inventory/Transaction/Stock_frm.aspx',
	    '/Inventory/Transaction/Average/StockGIN_frm.aspx', '/Inventory/Transaction/StockReturn.aspx'
		             --- For Godown -----
		,'/Inventory/Transaction/FinishedGoods.aspx',
		             --- Stock Transfer -----
		'/Inventory/Transaction/TransferOut_frm.aspx', '/Inventory/Transaction/TransferIn_frm.aspx',
		'/Inventory/Transaction/InventoryInOut.aspx', '/Inventory/Transaction/InventoryInOut_Store.aspx',
		             --- Gate Form -----
		'/Inventory/Transaction/GateOutward_frm.aspx', '/Inventory/Transaction/GrossWeight_Frm.aspx',
        '/Cane/Transaction/TareWeight.aspx',
		             --- Status Change -----
		 '/Inventory/Transaction/StockRequistion_frm.aspx', 
		 '/inventory/transaction/stock_frm.aspx'
        )
) and CompanyID = @CompanyID and PeriodID = @PeriodID and IsActive = @IsActive and Status = 1 
ORDER BY DocumentType 

";
                #endregion

                DataSet ds = null;
                if (sqlTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_ForInventoryDocOnly, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(sqlTransaction, CommandType.Text, Query_SelectAll_ForInventoryDocOnly, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }

            return dtToReturn;
        }

        public static int Get_DocumentTypeID_By_DocumentTypeName(int CompanyID, string DocumentTypeName)
        {
            try
            {
                int DocumentTypeID;

                const string Query = @"SELECT DocumentTypeID FROM SET_DocumentType WHERE CompanyID = @CompanyID AND DocumentType LIKE @DocumenType AND Status = 1 AND IsActive = 1 ";

                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@DocumenType", DocumentTypeName);

                DocumentTypeID = Convert.ToInt32(SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams).Tables[0].Rows[0]["DocumentTypeID"].ToString());

                return DocumentTypeID;
            }
            catch (BusinessLogicException bex)
            {
                SqlHelper.LogException(bex);
                throw bex;
            }
            catch (Exception ex) { throw ex; }
        }


        public static DataTable GetAllDocumentTypesByMenuPath_ForRaniSugar(int companyID, int periodID, string menuPath, SqlTransaction sqlTran = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;


                const string Query_GetAllByMenuPath = @" SELECT DocumentTypeID, DocumentType FROM SET_DocumentType 
WHERE SET_DocumentType.CompanyID = @CompanyID AND SET_DocumentType.PeriodID = @PeriodID 
    AND SET_DocumentType.MenuID in (SELECT MenuID FROM ApplicationMenu WHERE MenuPath in ({0})) AND SET_DocumentType.IsActive = 1 AND SET_DocumentType.Status = 1 
and SET_DocumentType.DocumentType not like '%CO2%' and SET_DocumentType.DocumentType not like '%Ethanol%' 
ORDER BY DocumentType asc ";


                if (sqlTran != null)
                    ds = SqlHelper.ExecuteDataset(sqlTran, CommandType.Text, string.Format(Query_GetAllByMenuPath, menuPath), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAllByMenuPath, menuPath), sqlparams);

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


    }
}
