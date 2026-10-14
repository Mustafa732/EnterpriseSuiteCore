using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Linq;
using Common.Utils;

using DAL.DataAccessLayer;

using FnI.BusinessAccessLayer.Setup;
using FnI.QueryResourceLibrary.Inventory.Transaction;
using FnI.BusinessAccessLayer.AR.Setup;
using FnI.BusinessAccessLayer.Security;

namespace FnI.BusinessAccessLayer.GL.Transaction
{
    public partial class GL_TransactionHeader
    {

        public bool ToBeInserted
        {
            get;
            set;
        }

        #region Constant Queries

        const string Query_UpdateOnRateReverse = @" 
Update GL_TransactionHeader set UpdatedBy = @UpdatedBy, UpdatedByValue = @UpdatedByValue, UpdateDate = @UpdateDate, ControlCredit = @ControlCredit, 
    ControlDebit = @ControlDebit, ApprovedBy = @ApprovedBy, DateOfApproval = @DateOfApproval, Narration = @Narration 
where CompanyID = @CompanyID and PeriodId = @PeriodId and TransactionID = @TransactionID ";

        const string Query_Select_Min_Max_DocumentDate_By_Detail = @" SELECT MIN(th.DocumentDate) AS MinDate, MAX(th.DocumentDate) AS MaxDate 
FROM GL_TransactionHeader th INNER JOIN GL_TransactionDetail td ON td.TransactionID = th.TransactionID AND td.CompanyID = th.CompanyID 
WHERE th.CompanyID = @CompanyID AND td.ChartOfAccountID = @ChartOfAccountID GROUP BY th.CompanyID, td.ChartOfAccountID ";

        const string Query_Select_Max_DocumentNo = @" SELECT ISNULL(MAX(DocumentNo), 0) AS MaxDocumentNo FROM GL_TransactionHeader 
WHERE DocumentTypeID = @DocumentTypeID AND Status = @Status AND CompanyID = @CompanyID AND PeriodID = @PeriodID; ";

        const string Query_Select_By_PrimaryKey_WithOtherTables = @" 
SELECT DISTINCT GL_TransactionHeader.TransactionID, GL_TransactionDetail.ChartOfAccountID, GL_TransactionHeader.PeriodID, GL_TransactionHeader.CompanyID, 
    GL_TransactionHeader.DocumentTypeID, GL_TransactionHeader.DocumentNo, GL_TransactionHeader.DocumentDate, GL_TransactionHeader.CreateDate, 
    GL_TransactionHeader.UpdateDate, GL_TransactionHeader.DateOfApproval, GL_TransactionHeader.ControlCredit, GL_TransactionHeader.ControlDebit, 
    GL_TransactionHeader.Status, GL_TransactionHeader.CreatedByValue, GL_TransactionHeader.UpdatedByValue, GL_TransactionHeader.Narration, 
    GL_TransactionHeader.CreatedBy, GL_TransactionHeader.UpdatedBy, GL_TransactionHeader.ApprovedBy, SET_DocumentType.TransactionType, SET_DocumentType.DocumentType, 
    GL_TransactionHeader.StatusCode, GL_TransactionHeader.LocationID,SET_DocumentType.GroupID 
FROM GL_TransactionHeader 
    INNER JOIN SET_DocumentType ON SET_DocumentType.DocumentTypeID = GL_TransactionHeader.DocumentTypeID AND SET_DocumentType.PeriodID = GL_TransactionHeader.PeriodID 
        AND SET_DocumentType.CompanyID = GL_TransactionHeader.CompanyID 
    INNER JOIN GL_TransactionDetail ON GL_TransactionDetail.CompanyID = GL_TransactionHeader.CompanyID 
        AND GL_TransactionDetail.PeriodID = GL_TransactionHeader.PeriodID AND GL_TransactionDetail.TransactionID = GL_TransactionHeader.TransactionID 
        AND GL_TransactionDetail.Status = 1 
WHERE GL_TransactionHeader.TransactionID = @TransactionID AND GL_TransactionHeader.PeriodID = @PeriodID AND GL_TransactionHeader.CompanyID = @CompanyID 
    AND GL_TransactionHeader.Status = 1; ";

        const string Query_SelectAll_Searching_WithPaging = @" 
Select *,ROW_NUMBER() OVER (ORDER BY abc.TransactionID DESC) AS 'Row' From 
(
	SELECT DISTINCT GL_TransactionHeader.TransactionID, GL_TransactionHeader.PeriodID, GL_TransactionHeader.CompanyID, GL_TransactionHeader.DocumentTypeID, 
        SET_DocumentType.DocumentType, GL_TransactionHeader.DocumentNo, GL_TransactionHeader.DocumentDate, GL_TransactionHeader.StatusCode, 
        GL_TransactionHeader.DateOfApproval, --aspnet_Users.UserName ,
Set_PeriodCustom.PeriodSetName,GL_TransactionHeader.CreatedByValue As UserName,
aspnet_Users.UserName as ApprovedBy,GL_TransactionHeader.ControlDebit as Amount
	FROM GL_TransactionHeader 
		INNER JOIN SET_DocumentType ON SET_DocumentType.documentTypeid = GL_TransactionHeader.DocumentTypeID 
			AND SET_DocumentType.PeriodID = GL_TransactionHeader.PeriodID AND SET_DocumentType.CompanyID = GL_TransactionHeader.CompanyID 
		Left Join Set_DocumentApprove On Set_DocumentApprove.CompanyId = GL_TransactionHeader.CompanyID 
			AND Set_DocumentApprove.PeriodID = GL_TransactionHeader.PeriodID AND Set_DocumentApprove.DocumentTypeID = GL_TransactionHeader.DocumentTypeID 
			AND Set_DocumentApprove.DocumentNo = GL_TransactionHeader.DocumentNo 
            {1}
		--LEFT JOIN aspnet_Users ON aspnet_Users.UserId = Set_DocumentApprove.CreatedBy 
		LEFT JOIN aspnet_Users ON aspnet_Users.UserId = GL_TransactionHeader.ApprovedBy
		inner join Set_PeriodCustom on GL_TransactionHeader.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate
        and Set_PeriodCustom.Status=1 and Set_PeriodCustom.fiscalid = @currentfiscalid 
	{0}
) abc ";

        const string Query_SelectAll_Sorting = @" 
SELECT DISTINCT GL_TransactionHeader.TransactionID, GL_TransactionHeader.PeriodID, GL_TransactionHeader.CompanyID, GL_TransactionHeader.DocumentTypeID, 
    SET_DocumentType.DocumentType, GL_TransactionHeader.DocumentNo, GL_TransactionHeader.DocumentDate, GL_TransactionHeader.StatusCode, 
    GL_TransactionHeader.DateOfApproval, aspnet_Users.UserName 
FROM GL_TransactionHeader 
    INNER JOIN SET_DocumentType ON SET_DocumentType.documentTypeid = GL_TransactionHeader.DocumentTypeID AND SET_DocumentType.PeriodID = GL_TransactionHeader.PeriodID 
        AND SET_DocumentType.CompanyID = GL_TransactionHeader.CompanyID 
    LEFT JOIN aspnet_Users ON aspnet_Users.UserId = GL_TransactionHeader.ApprovedBy ";

        const string Query_Select_Max_Min_DocumentDate = @" SELECT MIN(DocumentDate) AS MinDate, MAX(DocumentDate) AS MaxDate FROM GL_TransactionHeader 
        WHERE PeriodID = @PeriodID GROUP BY PeriodID ";


        const string Query_Update_By_Keys_Status = @" 
UPDATE [GL_TransactionHeader] SET [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue
WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [DocumentTypeID] = @DocumentTypeID and [DocumentNo] = @DocumentNo

UPDATE [GL_TransactionDetail] SET [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue
WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID 
	and [TransactionID] = (SELECT [TransactionID] FROM [GL_TransactionHeader] 
							WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID 
								and [DocumentTypeID] = @DocumentTypeID and [DocumentNo] = @DocumentNo) ";

        const string Query_Get_LedgerTransactionList = @" 
SELECT Distinct 
GL_TransactionDetail.SerialNo,
GL_TransactionHeader.DocumentNo AS VoucherNo, SET_DocumentType.DocumentType, Convert(Varchar(10), GL_TransactionHeader.DocumentDate, 103) AS Date, 
    GL_CodeCombinationDetail.CodeCombinationID AS AccountCode, GL_CodeCombinationDetail.Description AS AccountName, SET_Department.DepartmentName, 
    SET_CostCenter.CostCenterName, case when GL_TransactionDetail.TransactionType = 'Credit' then GL_TransactionDetail.Amount else 0 end as CreditAmount, 
    case when GL_TransactionDetail.TransactionType = 'Debit' then GL_TransactionDetail.Amount else 0 end as DebitAmount, GL_TransactionDetail.TransactionType, 
    GL_TransactionDetail.DocumentReference as Narration,GL_TransactionDetail.Narration as  DtlNarration 
FROM GL_TransactionHeader 
    INNER JOIN GL_TransactionDetail ON GL_TransactionDetail.TransactionID = GL_TransactionHeader.TransactionID 
        AND GL_TransactionDetail.CompanyID = GL_TransactionHeader.CompanyID AND GL_TransactionDetail.PeriodID = GL_TransactionHeader.PeriodID 
        AND GL_TransactionDetail.Status = 1 
    INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = GL_TransactionHeader.CompanyID AND SET_DocumentType.PeriodID = GL_TransactionHeader.PeriodID 
        AND SET_DocumentType.DocumentTypeID = GL_TransactionHeader.DocumentTypeID 
    INNER JOIN GL_CodeCombinationDetail ON GL_CodeCombinationDetail.CodeCombinationID = GL_TransactionDetail.CodeCombinationID 
        --AND GL_CodeCombinationDetail.CompanyID = GL_TransactionDetail.CompanyID 
    {2} 
    LEFT JOIN SET_Department ON GL_TransactionDetail.DepartmentID = SET_Department.DepartmentID 
    LEFT JOIN SET_CostCenter ON GL_TransactionDetail.CostCenterID = SET_CostCenter.CostCenterID 
WHERE GL_TransactionHeader.CompanyID in @CompanyID AND GL_TransactionHeader.PeriodID = @PeriodID AND GL_TransactionHeader.Status = 1 
    And GL_TransactionHeader.StatusCode = 1 
    {0}
    {1} 
ORDER BY VoucherNo, CreditAmount ";

        const string Query_Update_StatusCode_By_DocNo = @" UPDATE [GL_TransactionHeader] SET [ApprovedBy] = @ApprovedBy, [DateOfApproval] = @DateOfApproval, [StatusCode] = @StatusCode
WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [DocumentTypeID] = @DocumentTypeID and [DocumentNo] = @DocumentNo";

        const string Query_ExpenseAccountReport = @" 
SELECT GL_TransactionHeader.TransactionID, GL_TransactionHeader.PeriodID, GL_TransactionHeader.CompanyID, GL_TransactionHeader.CreateDate, 
    cast(GL_TransactionHeader.DocumentDate as Date)as DocumentDate, GL_TransactionDetail.CodeCombinationID, GL_CodeCombinationDetail.Description, 
    Case When GL_TransactionDetail.TransactionType = 'Debit' then GL_TransactionDetail.Amount else 0 end AS Debit, 
    Case When GL_TransactionDetail.TransactionType = 'Credit' then GL_TransactionDetail.Amount else 0 end AS Credit, GL_TransactionDetail.TransactionType, 
    GL_TransactionDetail.CostCenterID, GL_TransactionDetail.DepartmentID, Set_Department.DepartmentName, Set_CostCenter.CostCenterName, 
    cast(GL_TransactionDetail.CreateDate as Date) CreateDate 
FROM GL_TransactionHeader 
    INNER JOIN GL_TransactionDetail ON GL_TransactionDetail.TransactionID = GL_TransactionHeader.TransactionID 
        AND GL_TransactionDetail.CompanyID = GL_TransactionHeader.CompanyID AND GL_TransactionDetail.PeriodID = GL_TransactionHeader.PeriodID 
        AND GL_TransactionDetail.Status = 1 
    INNER JOIN GL_CodeCombinationDetail ON 
--GL_CodeCombinationDetail.CompanyID = GL_TransactionDetail.CompanyID AND 
GL_CodeCombinationDetail.ChartOfAccountID = GL_TransactionDetail.ChartOfAccountID 
        AND GL_CodeCombinationDetail.CodeCombinationID = GL_TransactionDetail.CodeCombinationID AND GL_CodeCombinationDetail.Status = 1 
        and GL_CodeCombinationDetail.AccountTypeID = (select AccountTypeID from SET_AccountType where AccountTypeName = 'Expenses') 
    Left Join Set_Department ON Set_Department.DepartmentID = GL_TransactionDetail.DepartmentID 
    Left Join Set_CostCenter ON Set_CostCenter.CostCenterID = GL_TransactionDetail.CostCenterID 
Where GL_TransactionHeader.CompanyID = @CompanyID and GL_TransactionHeader.PeriodID = @PeriodID and GL_TransactionHeader.Status = 1 
    and (GL_TransactionDetail.CostCenterID = @CostCenterID or @CostCenterID IS NULL) and (GL_TransactionDetail.DepartmentID = @DepartmentID or @DepartmentID is null) 
    {0} ";

        const string Query_GetAccountGINData = @"
Select GL_TransactionHeader.TransactionID, GL_TransactionHeader.DocumentTypeID, GL_TransactionHeader.DocumentNo, GL_TransactionDetail.SerialNo,  
    GL_TransactionDetail.CodeCombinationID, GL_TransactionDetail.ChartOfAccountID, GL_TransactionDetail.TransactionType 
from GL_TransactionHeader 
	inner join GL_TransactionDetail on GL_TransactionDetail.CompanyID = GL_TransactionHeader.CompanyID 
        and GL_TransactionDetail.PeriodID = GL_TransactionHeader.PeriodID AND GL_TransactionDetail.TransactionID = GL_TransactionHeader.TransactionID 
where GL_TransactionHeader.CompanyID = @CompanyID and GL_TransactionHeader.PeriodID = @PeriodID
 and GL_TransactionHeader.DocumentTypeID = @DocumentTypeID 
AND GL_TransactionHeader.DocumentNo = @DocumentNo
and GL_TransactionHeader.FiscalID = @FiscalID
    --And GL_TransactionDetail.SourceHeaderID = @StockHeaderID 
	AND GL_TransactionHeader.Status = 1 AND GL_TransactionDetail.Status = 1
";

        const string Query_GetAccountGRNData = @"
Select GL_TransactionHeader.TransactionID, GL_TransactionHeader.DocumentTypeID, GL_TransactionHeader.DocumentNo, GL_TransactionDetail.SerialNo,  
    GL_TransactionDetail.CodeCombinationID, GL_TransactionDetail.ChartOfAccountID, GL_TransactionDetail.TransactionType 
from GL_TransactionHeader 
	inner join GL_TransactionDetail on GL_TransactionDetail.CompanyID = GL_TransactionHeader.CompanyID 
        and GL_TransactionDetail.PeriodID = GL_TransactionHeader.PeriodID AND GL_TransactionDetail.TransactionID = GL_TransactionHeader.TransactionID 
where GL_TransactionHeader.CompanyID = @CompanyID and GL_TransactionHeader.PeriodID = @PeriodID 
AND (GL_TransactionHeader.FiscalID = @fiscalID or @fiscalID is null)
and GL_TransactionHeader.DocumentTypeID = @DocumentTypeID 
    And GL_TransactionHeader.DocumentNo = @DocumentNo 
AND GL_TransactionHeader.Status = 1 
AND GL_TransactionDetail.Status = 1
";

        const string Query_GetAccountRETURNData = @"
Select GL_TransactionHeader.TransactionID, GL_TransactionHeader.DocumentTypeID, GL_TransactionHeader.DocumentNo, GL_TransactionDetail.SerialNo,  
    GL_TransactionDetail.CodeCombinationID, GL_TransactionDetail.ChartOfAccountID, GL_TransactionDetail.TransactionType 
from GL_TransactionHeader 
	inner join GL_TransactionDetail on GL_TransactionDetail.CompanyID = GL_TransactionHeader.CompanyID 
        and GL_TransactionDetail.PeriodID = GL_TransactionHeader.PeriodID AND GL_TransactionDetail.TransactionID = GL_TransactionHeader.TransactionID 
where GL_TransactionHeader.CompanyID = @CompanyID and GL_TransactionHeader.PeriodID = @PeriodID 
AND (GL_TransactionHeader.FiscalID = @fiscalID or @fiscalID is null)
and GL_TransactionHeader.DocumentTypeID = @DocumentTypeID 
    And GL_TransactionHeader.DocumentNo = @DocumentNo 
AND GL_TransactionHeader.Status = 1 
AND GL_TransactionDetail.Status = 1
";


        const string Query_UpdateGLHeaderDebitCredit = @" Update GL_TransactionHeader 
set ControlDebit = @ControlDebit, ControlCredit = @ControlCredit, Narration = @Narration 
where TransactionID = @TransactionID and CompanyID = @CompanyID and PeriodID = @PeriodID ";

        const string Query_UpdateControlDebitCreditAutomatically = @" Update GL_TransactionHeader 
set ControlDebit = (Select Sum(Amount) From GL_TransactionDetail 
        Where CompanyID = @CompanyID And PeriodID = @PeriodID And TransactionID = @TransactionID And TransactionType = 'Debit'), 
    ControlCredit = (Select Sum(Amount) From GL_TransactionDetail 
        Where CompanyID = @CompanyID And PeriodID = @PeriodID And TransactionID = @TransactionID And TransactionType = 'Credit') 
where TransactionID = @TransactionID and CompanyID = @CompanyID and PeriodID = @PeriodID ";

        const string Query_GetDataByDocumentType = @" Select GL_TransactionHeader.* from GL_TransactionHeader 
inner join Set_PeriodCustom on  
GL_TransactionHeader.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID
where GL_TransactionHeader.CompanyID = @CompanyID 
and GL_TransactionHeader.PeriodID = @PeriodID and GL_TransactionHeader.DocumentTypeID = @DocumentTypeID 
And GL_TransactionHeader.DocumentNo = @DocumentNo AND GL_TransactionHeader.Status = 1";

        const string Query_Delete_By_PrimaryKey = @" DELETE FROM GL_TransactionHeader 
WHERE CompanyID = @CompanyID AND PeriodID = @PeriodID AND TransactionID = @TransactionID

DELETE FROM DocumentAttachments  where SourceHeaderID = @TransactionID and CompanyID = @CompanyID 

";

        const string Query_Get_AllDocumentNos = @" SELECT * FROM GL_TransactionHeader 
WHERE CompanyID = @CompanyID and PeriodID = @PeriodID and DocumentTypeID = @DocumentTypeID And Status = 1 AND IsNull(IsReversed, 0) = 0 AND ReversalID IS NULL 
and FiscalID = @FiscalID
ORDER BY DocumentNo ";

        const string Query_Select_CheckDataByKeyForNarration = @"Select * from Gl_TransactionHeader where CompanyID=@CompanyID AND PeriodID = @PeriodID AND DocumentTypeID=@DocumentTypeID AND DocumentNo = @DocumentNo AND Status=1";

        const string Query_GetData_MovementSummaryPrepaidExpenses = @"
select CodeDescription,CostCenterName,sum(Opening) as Opening,sum(Amount) as Amount, sum(Opening)  + sum(Amount) as Closing from (
select GL_CodeCombinationDetail.Description  as CodeDescription,SET_CostCenter.CostCenterName, case when GL_TransactionDetail.TransactionType = 'Debit' then sum(GL_TransactionDetail.Amount) else sum(GL_TransactionDetail.Amount)*-1 end as Opening ,0 as Amount
​
from GL_TransactionHeader
inner join GL_TransactionDetail on  GL_TransactionDetail.TransactionID = GL_TransactionHeader.TransactionID
and GL_TransactionDetail.CompanyID = GL_TransactionHeader.CompanyID
and GL_TransactionDetail.PeriodID = GL_TransactionHeader.PeriodID
--and GL_TransactionDetail.TransactionType = 'Debit'
and GL_TransactionDetail.Status = 1
inner join SET_CostCenter on SET_CostCenter.CostCenterID = GL_TransactionDetail.CostCenterID
​
​
inner join GL_CodeCombinationDetail on GL_CodeCombinationDetail.CodeCombinationID = GL_TransactionDetail.CodeCombinationID
inner join SET_AccountType on SET_AccountType.AccountTypeID = GL_CodeCombinationDetail.AccountTypeID
 where SET_AccountType.AccountTypeName like '%expenses%'
 and (GL_CodeCombinationDetail.Description like '%RENT%'  or GL_CodeCombinationDetail.Description like '%Subscription%')
 and GL_TransactionHeader.DocumentDate < @FromDate
 and GL_TransactionHeader.CompanyID = @CompanyID
 and GL_TransactionHeader.PeriodID = @PeriodID
 group by GL_CodeCombinationDetail.Description  ,SET_CostCenter.CostCenterName , GL_TransactionDetail.TransactionType
​
 union all
​
 select GL_CodeCombinationDetail.Description  as CodeDescription,SET_CostCenter.CostCenterName,  0 as Opening ,case when GL_TransactionDetail.TransactionType = 'Debit' then sum(GL_TransactionDetail.Amount) else sum(GL_TransactionDetail.Amount)*-1 end as Amount
 
from GL_TransactionHeader
inner join GL_TransactionDetail on  GL_TransactionDetail.TransactionID = GL_TransactionHeader.TransactionID
and GL_TransactionDetail.CompanyID = GL_TransactionHeader.CompanyID
and GL_TransactionDetail.PeriodID = GL_TransactionHeader.PeriodID
--and GL_TransactionDetail.TransactionType = 'Debit'
and GL_TransactionDetail.Status = 1
inner join SET_CostCenter on SET_CostCenter.CostCenterID = GL_TransactionDetail.CostCenterID
​
inner join GL_CodeCombinationDetail on GL_CodeCombinationDetail.CodeCombinationID = GL_TransactionDetail.CodeCombinationID
inner join SET_AccountType on SET_AccountType.AccountTypeID = GL_CodeCombinationDetail.AccountTypeID
 where SET_AccountType.AccountTypeName like '%expenses%'
 and (GL_CodeCombinationDetail.Description like '%RENT%'  or GL_CodeCombinationDetail.Description like '%Subscription%')
 and GL_TransactionHeader.CompanyID = @CompanyID
 and GL_TransactionHeader.PeriodID = @PeriodID
 and GL_TransactionHeader.DocumentDate between @FromDate and @ToDate
 group by GL_CodeCombinationDetail.Description  ,SET_CostCenter.CostCenterName , GL_TransactionDetail.TransactionType
​
 ) as tab
 group by CodeDescription,CostCenterName
";

        const string Query_YearWiseVarianceReport = @"Select FInTab.CodeCombinationID,FInTab.Description, 1 as SerialNo, 
{1}
--ISNULL(SUM(FInTab.CurrentAmount),0) as CurrentAmount, ISNULL(SUM(FInTab.PreviousAmount),0) as PreviousAmount  
from (
Select Tab.CodeCombinationID,Tab.Description, ISNULL(sum(Tab.Debit),0) - ISNULL(Sum(Tab.Credit),0) as CurrentAmount, 0 as PreviousAmount
 From (
Select GL_TransactionDetail.CodeCombinationID,GL_CodeCombinationDetail.Description,
sum((case when GL_TransactionDetail.TransactionType = 'Credit' then amount else 0 end)) as Credit,
sum((case when GL_TransactionDetail.TransactionType = 'Debit' then amount else 0 end)) as Debit
From GL_TransactionDetail 
Inner join GL_TransactionHeader on GL_TransactionHeader.TransactionID = GL_TransactionDetail.TransactionID
And GL_TransactionHeader.CompanyID = GL_TransactionDetail.CompanyID
And GL_TransactionHeader.PeriodID = GL_TransactionDetail.PeriodID
And GL_TransactionHeader.Status = 1 
And GL_TransactionDetail.Status = 1 
INNER JOIN GL_CodeCombinationDetail on GL_CodeCombinationDetail.CodeCombinationID = GL_TransactionDetail.CodeCombinationID
and GL_CodeCombinationDetail.Status = 1
And GL_TransactionHeader.StatusCode = 1
where GL_TransactionHeader.DocumentDate between @CurrentFromDate and @CurrentToDate
--AND GL_TransactionDetail.CodeCombinationID between '@AccountCodeFrom' and '@AccountCodeFrom'
and GL_TransactionHeader.CompanyID in ({CompanyIDs}) 
and GL_TransactionHeader.PeriodID = @PeriodID 
{2}
{0}
Group By GL_TransactionDetail.CodeCombinationID,GL_CodeCombinationDetail.Description,GL_TransactionDetail.TransactionType
) as Tab
Group By Tab.CodeCombinationID,Tab.Description
Union All
Select Tab.CodeCombinationID,Tab.Description, 0 as CurrentAmount, ISNULL(sum(Tab.Debit),0) - ISNULL(Sum(Tab.Credit),0) as PreviousAmount
 From (
Select GL_TransactionDetail.CodeCombinationID,GL_CodeCombinationDetail.Description,
sum((case when GL_TransactionDetail.TransactionType = 'Credit' then amount else 0 end)) as Credit,
sum((case when GL_TransactionDetail.TransactionType = 'Debit' then amount else 0 end)) as Debit
From GL_TransactionDetail 
Inner join GL_TransactionHeader on GL_TransactionHeader.TransactionID = GL_TransactionDetail.TransactionID
And GL_TransactionHeader.CompanyID = GL_TransactionDetail.CompanyID
And GL_TransactionHeader.PeriodID = GL_TransactionDetail.PeriodID
And GL_TransactionHeader.Status = 1 
And GL_TransactionDetail.Status = 1 
INNER JOIN GL_CodeCombinationDetail on GL_CodeCombinationDetail.CodeCombinationID = GL_TransactionDetail.CodeCombinationID
and GL_CodeCombinationDetail.Status = 1
And GL_TransactionHeader.StatusCode = 1
where GL_TransactionHeader.DocumentDate between @PrevFromDate and @PrevToDate
--AND GL_TransactionDetail.CodeCombinationID between '@AccountCodeFrom' and '@AccountCodeFrom'
and GL_TransactionHeader.CompanyID in ({CompanyIDs}) 
and GL_TransactionHeader.PeriodID = @PeriodID 
{2}
{0}
Group By GL_TransactionDetail.CodeCombinationID,GL_CodeCombinationDetail.Description,GL_TransactionDetail.TransactionType
) as Tab
Group By Tab.CodeCombinationID,Tab.Description
) as FInTab
Group By FInTab.CodeCombinationID,FInTab.Description";

        const string Query_Select_ColumnarGLReportMonthWise = @" 
Select Tab.DocumentDate, Tab.TransactionID, Tab.CodeCombinationID , Tab.Description, Tab.CostCenterID, Tab.CostCenterName, Tab.MonthNames, Tab.MonthNum, Tab.Years, Tab.Credit, Tab.Debit,
{1}
From (
select GL_TransactionHeader.DocumentDate, GL_TransactionHeader.TransactionID, GL_TransactionDetail.CodeCombinationID, GL_CodeCombinationDetail.Description
, GL_TransactionDetail.CostCenterID, SET_CostCenter.CostCenterName, DATENAME(MONTH,GL_TransactionHeader.DocumentDate)  + ' - ' + substring(convert(varchar,year(GL_TransactionHeader.DocumentDate)),3,4) as MonthNames,
MONTH( DATENAME(MONTH,GL_TransactionHeader.DocumentDate) + '1,1') MonthNum,
substring(convert(varchar,year(GL_TransactionHeader.DocumentDate)),3,4) as Years,
sum((case when GL_TransactionDetail.TransactionType = 'Credit' then amount else 0 end)) as Credit,
sum((case when GL_TransactionDetail.TransactionType = 'Debit' then amount else 0 end)) as Debit
,@WithOpening as WithOpening
from GL_TransactionHeader
INNER JOIN GL_TransactionDetail on GL_TransactionDetail.TransactionID = GL_TransactionHeader.TransactionID
and GL_TransactionDetail.PeriodID = GL_TransactionHeader.PeriodID
and GL_TransactionDetail.CompanyID = GL_TransactionHeader.CompanyID
and GL_TransactionHeader.Status = 1 and GL_TransactionDetail.Status = 1
INNER JOIN GL_CodeCombinationDetail on GL_CodeCombinationDetail.CodeCombinationID = GL_TransactionDetail.CodeCombinationID
and GL_CodeCombinationDetail.Status = 1
And GL_TransactionHeader.StatusCode = 1
left JOIN SET_CostCenter ON SET_CostCenter.CostCenterID = GL_TransactionDetail.CostCenterID 
where GL_TransactionHeader.DocumentDate between @FromDate and @ToDate
--AND GL_TransactionDetail.CodeCombinationID between '@AccountCodeFrom' and '@AccountCodeFrom'
and GL_TransactionHeader.CompanyID in ({CompanyIDs}) 
and GL_TransactionHeader.PeriodID = @PeriodID 
--and GL_TransactionHeader.FiscalID = @FiscalID
{3}
{2}
{WithoutClosing}
Group By GL_TransactionHeader.DocumentDate, GL_TransactionHeader.TransactionID, GL_TransactionDetail.CodeCombinationID,GL_CodeCombinationDetail.Description,GL_TransactionDetail.CostCenterID, SET_CostCenter.CostCenterName


union all

select GL_TransactionHeader.DocumentDate, GL_TransactionHeader.TransactionID, GL_TransactionDetail.CodeCombinationID, GL_CodeCombinationDetail.Description
,GL_TransactionDetail.CostCenterID, SET_CostCenter.CostCenterName, 'Opening' as MonthNames,
'1' MonthNum,
'1' as Years,
sum((case when GL_TransactionDetail.TransactionType = 'Credit' then amount else 0 end)) as Credit,
sum((case when GL_TransactionDetail.TransactionType = 'Debit' then amount else 0 end)) as Debit
,@WithOpening as WithOpening
from GL_TransactionHeader
INNER JOIN GL_TransactionDetail on GL_TransactionDetail.TransactionID = GL_TransactionHeader.TransactionID
and GL_TransactionDetail.PeriodID = GL_TransactionHeader.PeriodID
and GL_TransactionDetail.CompanyID = GL_TransactionHeader.CompanyID
and GL_TransactionHeader.Status = 1 and GL_TransactionDetail.Status = 1
INNER JOIN GL_CodeCombinationDetail on GL_CodeCombinationDetail.CodeCombinationID = GL_TransactionDetail.CodeCombinationID
and GL_CodeCombinationDetail.Status = 1
And GL_TransactionHeader.StatusCode = 1
left JOIN SET_CostCenter ON SET_CostCenter.CostCenterID = GL_TransactionDetail.CostCenterID 
where GL_TransactionHeader.DocumentDate < @FromDate 
--AND GL_TransactionDetail.CodeCombinationID between '@AccountCodeFrom' and '@AccountCodeFrom'
and GL_TransactionHeader.CompanyID in ({CompanyIDs}) 
and GL_TransactionHeader.PeriodID = @PeriodID 
--and GL_TransactionHeader.FiscalID = @FiscalID
{3}
{WithoutClosing}
{0}
";


        #endregion

        #region Private (Helper) Methods

        private static void Load(ref GL_TransactionHeader objGLHeader, DataRow drToSet)
        {
            if (drToSet != null)
            {
                objGLHeader.f_IsDataLoaded = true;
                objGLHeader.f_TransactionID = Convert.ToInt32(drToSet["TransactionID"]);
                objGLHeader.f_PeriodID = Convert.ToInt32(drToSet["PeriodID"]);
                objGLHeader.f_CompanyID = Convert.ToInt32(drToSet["CompanyID"]);
                objGLHeader.f_ControlCredit = Convert.ToDecimal(drToSet["ControlCredit"]);
                objGLHeader.f_ControlDebit = Convert.ToDecimal(drToSet["ControlDebit"]);
                objGLHeader.f_Status = Convert.ToBoolean(drToSet["Status"]);
                objGLHeader.f_DocumentTypeID = Convert.ToInt32(drToSet["DocumentTypeID"]);
                objGLHeader.f_DocumentDate = Convert.ToDateTime(drToSet["DocumentDate"]);
                objGLHeader.f_DocumentNo = Convert.ToInt32(drToSet["DocumentNo"]);
                objGLHeader.f_CreatedBy = Guid.Parse(drToSet["CreatedBy"].ToString());
                objGLHeader.f_CreateDate = Convert.ToDateTime(drToSet["CreateDate"]);

                if (drToSet["UpdatedBy"] != DBNull.Value)
                    objGLHeader.f_UpdatedBy = Guid.Parse(drToSet["UpdatedBy"].ToString());

                if (drToSet["UpdateDate"] != DBNull.Value)
                    objGLHeader.f_UpdateDate = Convert.ToDateTime(drToSet["UpdateDate"]);

                objGLHeader.f_CreatedByValue = Convert.ToString(drToSet["CreatedByValue"]);

                if (drToSet["UpdatedByValue"] != DBNull.Value)
                    objGLHeader.f_UpdatedByValue = Convert.ToString(drToSet["UpdatedByValue"]);

                if (drToSet["ApprovedBy"] != DBNull.Value)
                    objGLHeader.f_ApprovedBy = Guid.Parse(drToSet["ApprovedBy"].ToString());

                if (drToSet["DateOfApproval"] != DBNull.Value)
                    objGLHeader.f_DateOfApproval = Convert.ToDateTime(drToSet["DateOfApproval"]);

                if (drToSet["Narration"] != DBNull.Value)
                    objGLHeader.f_Narration = Convert.ToString(drToSet["Narration"]);

                if (drToSet["DefaultChartofAccountID"] != DBNull.Value)
                    objGLHeader.f_DefaultChartofAccountID = Convert.ToInt32(drToSet["DefaultChartofAccountID"]);

                if (drToSet["DefaultAccountCode"] != DBNull.Value)
                    objGLHeader.f_DefaultAccountCode = Convert.ToString(drToSet["DefaultAccountCode"]);

                if (drToSet["DefaultTransactionType"] != DBNull.Value)
                    objGLHeader.f_DefaultTransactionType = Convert.ToString(drToSet["DefaultTransactionType"]);

                if (drToSet["StatusCode"] != DBNull.Value)
                    objGLHeader.f_StatusCode = Convert.ToInt16(drToSet["StatusCode"]);

                if (drToSet["LocationID"] != DBNull.Value)
                    objGLHeader.f_LocationID = Convert.ToInt32(drToSet["LocationID"]);
            }
        }

        #endregion

        #region Class Methods

        public bool UpdateDataByKeyOnRateRevers(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[11];
                sqlparams[0] = new SqlParameter("@TransactionID", f_TransactionID);
                sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[2] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[3] = new SqlParameter("@ControlCredit", f_ControlCredit);
                sqlparams[4] = new SqlParameter("@ControlDebit", f_ControlDebit);

                if (f_UpdatedBy.HasValue)
                    sqlparams[5] = new SqlParameter("@UpdatedBy", f_UpdatedBy.Value);
                else
                    sqlparams[5] = new SqlParameter("@UpdatedBy", DBNull.Value);

                if (f_UpdateDate.HasValue)
                    sqlparams[6] = new SqlParameter("@UpdateDate", f_UpdateDate.Value);
                else
                    sqlparams[6] = new SqlParameter("@UpdateDate", DBNull.Value);

                if (!string.IsNullOrEmpty(f_UpdatedByValue))
                    sqlparams[7] = new SqlParameter("@UpdatedByValue", f_UpdatedByValue);
                else
                    sqlparams[7] = new SqlParameter("@UpdatedByValue", DBNull.Value);

                if (f_ApprovedBy.HasValue)
                    sqlparams[8] = new SqlParameter("@ApprovedBy", f_ApprovedBy.Value);
                else
                    sqlparams[8] = new SqlParameter("@ApprovedBy", DBNull.Value);

                if (f_DateOfApproval.HasValue)
                    sqlparams[9] = new SqlParameter("@DateOfApproval", f_DateOfApproval.Value);
                else
                    sqlparams[9] = new SqlParameter("@DateOfApproval", DBNull.Value);

                sqlparams[10] = new SqlParameter("@Narration", f_Narration);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateOnRateReverse, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_UpdateOnRateReverse, sqlparams) > 0)
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

        public static DataSet GetMinMaxDocumentDatesByDetail(int companyID, int chartOfAccountID)
        {
            DataSet dsToReturn;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@ChartOfAccountID", chartOfAccountID);

                dsToReturn = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_Min_Max_DocumentDate_By_Detail, sqlparams);
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static int GetMaxDocumentNo(int companyID, int periodID, int documentTypeID, bool status = true, SqlTransaction objTransaction = null)
        {
            int toReturn = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[1] = new SqlParameter("@Status", status);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);

                if (objTransaction != null)
                    toReturn = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Select_Max_DocumentNo, sqlparams));
                else
                    toReturn = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Select_Max_DocumentNo, sqlparams));
                //Convert.ToInt32(SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_Max_DocumentNo, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return toReturn;
        }

        public static int GetMaxDocumentNo_SqlTrans(int companyID, int periodID, int documentTypeID, bool status = true, SqlTransaction sqltr = null)
        {
            int toReturn = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[1] = new SqlParameter("@Status", status);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);
                if (sqltr == null)
                    toReturn = Convert.ToInt32(SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_Max_DocumentNo, sqlparams).Tables[0].Rows[0]["MaxDocumentNo"]);
                else
                    toReturn = Convert.ToInt32(SqlHelper.ExecuteDataset(sqltr, CommandType.Text, Query_Select_Max_DocumentNo, sqlparams).Tables[0].Rows[0]["MaxDocumentNo"]);
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return toReturn;
        }

        public static DataRow GetDataByKeyWithOtherTables(int companyID, int periodID, int transactionID)
        {
            DataRow drToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@TransactionID", transactionID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_PrimaryKey_WithOtherTables, sqlparams);

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

        public bool InsertTransactionHederAndDetail(List<GL_TransactionDetail> colTranDetail, List<SET_CurrencyConversion> lstCurrConversion,
            SET_DocumentApprove da, int CurrentFiscalID = 0)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertTransactionHederAndDetail");

            try
            {
                this.DocumentNo = SET_DocumentSequence.ManualDocumentSequence(f_DocumentTypeID, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID);
                this.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                this.Insert(sqltrans);


                decimal AmountDebit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                decimal AmountCredit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                if (AmountDebit != AmountCredit)
                    throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");


                foreach (GL_TransactionDetail td in colTranDetail)
                {
                    td.TransactionID = this.TransactionID;
                    td.Insert(sqltrans);
                }

                if (lstCurrConversion != null)
                {
                    foreach (SET_CurrencyConversion objCurrConversion in lstCurrConversion)
                    {
                        SET_CurrencyConversion objCurrConversionFound = new SET_CurrencyConversion(objCurrConversion.CurrencyID, objCurrConversion.Date, sqltrans);

                        if (!objCurrConversionFound.IsDataLoaded)
                            objCurrConversion.Insert(sqltrans);

                        else
                            objCurrConversion.UpdateDataByKey(sqltrans);
                    }
                }

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                {
                    da.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
                    da.DocumentNo = this.DocumentNo.ToString();
                    da.Insert(sqltrans);
                }

                #endregion

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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertTransactionHederAndDetail");
            }

            return result;
        }

        public bool UpdateTransactionHederAndDetail(List<GL_TransactionDetail> colAddTranDetail, List<GL_TransactionDetail> colUpdateTranDetail,
            List<GL_TransactionDetail> colDeletTranDetail, List<SET_CurrencyConversion> lstCurrConversion, SET_DocumentApprove da, int CurrentFiscalID)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateTransactionHederAndDetail");

            try
            {
                this.UpdateDataByKey(sqltrans);


                //decimal AmountDebit = Math.Round(colAddTranDetail.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                //decimal AmountCredit = Math.Round(colAddTranDetail.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                //if (AmountDebit != AmountCredit)
                //    throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");

                foreach (GL_TransactionDetail tdAdd in colAddTranDetail)
                    tdAdd.Insert(sqltrans);


                // AmountDebit = Math.Round(colUpdateTranDetail.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                // AmountCredit = Math.Round(colUpdateTranDetail.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                //if (AmountDebit != AmountCredit)
                //    throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");


                foreach (GL_TransactionDetail tdUpdate in colUpdateTranDetail)
                    tdUpdate.UpdateDataByKey(sqltrans);

                foreach (GL_TransactionDetail tdDelete in colDeletTranDetail)
                    tdDelete.DeleteDataByKey(sqltrans);

                if (lstCurrConversion != null)
                {
                    foreach (SET_CurrencyConversion objCurrConversion in lstCurrConversion)
                    {
                        SET_CurrencyConversion objCurrConversionFound = new SET_CurrencyConversion(objCurrConversion.CurrencyID, objCurrConversion.Date, sqltrans);

                        if (!objCurrConversionFound.IsDataLoaded)
                            objCurrConversion.Insert(sqltrans);
                        else
                            objCurrConversion.UpdateDataByKey(sqltrans);
                    }
                }

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                {
                    da.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
                    da.DocumentNo = this.DocumentNo.ToString();
                    da.Insert(sqltrans);
                }

                #endregion

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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateTransactionHederAndDetail");
            }

            return result;
        }

        public bool UpdateTransactionHederForApprove(SET_DocumentApprove dA, int CurrentFiscalID)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateTransactionHeader");

            try
            {
                this.FiscalID = CurrentFiscalID;
                this.UpdateDataByKey(sqltrans);

                #region Update Status To False, If "InProcess" In SET_DocumentApprove

                if (dA.ApprovalStatus.Equals(Enumaration.CurrentStatus.InProcess.ToString()))
                {
                    dA.FiscalID = CurrentFiscalID;
                    dA.UpdateDataByKeyStatus(sqltrans);
                }

                #endregion

                #region Update Status To False, If "Discuss" In SET_DocumentApprove

                if (dA.ApprovalStatus.Equals(Enumaration.CurrentStatus.Discuss.ToString()))
                {
                    dA.FiscalID = CurrentFiscalID;
                    dA.UpdateStatusByKeysWithoutDiscuss(sqltrans);
                }

                #endregion

                #region Insert In SET_DocumentApprove

                // delete after disscus
                if (dA.ApprovalStatus == "Discuss")
                    SET_DocumentApprove.DeleteDataByDocNoanddocTypeid(f_CompanyID, f_PeriodID, int.Parse(dA.DocumentNo), dA.DocumentTypeID, sqltrans, CurrentFiscalID);

                dA.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(f_CompanyID, f_PeriodID, sqltrans);
                dA.FiscalID = CurrentFiscalID;
                dA.Insert(sqltrans);

                #endregion

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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateTransactionHeader");
            }

            return result;
        }

        public bool DeleteTransactionHederAndDetail(List<GL_TransactionDetail> colDeletTranDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("DeleteTransactionHederAndDetail");

            try
            {
                DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, f_PeriodID, f_TransactionID);

                foreach (GL_TransactionDetail tdDelete in colDeletTranDetail)
                    tdDelete.DeleteDataByKey(sqltrans);

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
                    SqlHelper.RollBackTransaction(sqltrans, "DeleteTransactionHederAndDetail");
            }
            return result;
        }

        public bool UpdateDataByKeyStatus(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", f_DocumentTypeID);
                sqlparams[3] = new SqlParameter("@DocumentNo", f_DocumentNo);
                sqlparams[4] = new SqlParameter("@Status", false);
                sqlparams[5] = new SqlParameter("@UpdatedBy", f_UpdatedBy.Value);
                sqlparams[6] = new SqlParameter("@UpdateDate", f_UpdateDate.Value);
                sqlparams[7] = new SqlParameter("@UpdatedByValue", f_UpdatedByValue);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_By_Keys_Status, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_By_Keys_Status, sqlparams) > 0)
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

        public static DataTable GetAllDataWithOtherTables(string strWhere, string UserIDClause, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc , int CurrentFiscalID = 0)
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
                    sqlparamsList.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));
                    
                    SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                    sqlparamTotalRows.Direction = ParameterDirection.Output;

                    SqlParameter[] sqlparams = sqlparamsList.ToArray();

                    string query = string.Format(Query_SelectAll_Searching_WithPaging, strWhere, UserIDClause);

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, query
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), query
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_Sorting + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""),
                        sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataWithOtherTablesForApprove(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null,
            int? PageSize = null, Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc)
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
                    //dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_Searching_WithPaging_ForApprove + strWhere
                    //    + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), Query_SelectAll_Searching_WithPaging_ForApprove + strWhere
                    //    + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    string query = FnI.QueryResourceLibrary.GL.GL_TransactionHeader.Query_SelectAll_Searching_WithPaging_ForApprove.Replace("{AdditionalParameters}", strWhere);
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, query + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""),
                        query + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionHeader.Query_SelectAll_Sorting_ForApprove + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""),
                        sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GET_CustomerHistory(int companyID, int periodID, Nullable<DateTime> datefrom, Nullable<DateTime> dateto, Nullable<int> customerfrom,
            Nullable<int> customerto)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", datefrom);
                sqlparams[3] = new SqlParameter("@ToDate", dateto);

                if (customerfrom != null && customerto != null)
                    sb.Append(" and AR_SET_Customer.CustomerID between " + customerfrom + " and " + customerto + " ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.GL.GL_TransactionHeader.Query_History, sb), sqlparams);

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

        public static DataTable GET_CustomerTrailBalance(int companyID, int periodID, Nullable<int> customer, Nullable<int> customerto, Nullable<DateTime> datefrom, Nullable<DateTime> dateto)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", datefrom);
                sqlparams[3] = new SqlParameter("@ToDate", dateto);


                if (customer != null)
                {

                    sb.Append(" and AR_SET_Customer.CustomerID between " + customer + "  and " + customerto + "");
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.GL.GL_TransactionHeader.Query_CustomerTrailBalance, sb), sqlparams);

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

        public static DataSet GetAllDataSummaryOfAdditionsToPropertyPlantEquipment(Nullable<int> periodID, Nullable<DateTime> Date_From, int? StatusID, Nullable<DateTime> Date_To, int? PersonID, SqlConnection objConnection = null)
        {
            DataSet dt = null;
            DataSet ds = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@FromDate", Date_From);
                sqlparams[2] = new SqlParameter("@ToDate", Date_To);
                if (StatusID.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@StatusID", StatusID.ToString());
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@StatusID", DBNull.Value);
                }
                if (PersonID.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@PersonID", PersonID.ToString());
                }
                else
                {
                    sqlparams[4] = new SqlParameter("@PersonID", DBNull.Value);
                }
                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionHeader.Query_SummaryOfAdditionsToPropertyPlantEquipment, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionHeader.Query_SummaryOfAdditionsToPropertyPlantEquipment, sqlparams);
                //if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                //    dt = ds.Tables[0];
                if (ds.Tables.Count > 0)
                    return ds;

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GET_BusinessPartnerTrailBalance(string companyID, int periodID, Nullable<int> customer, Nullable<int> customerto,
            Nullable<DateTime> datefrom, Nullable<DateTime> dateto, Guid userID, Nullable<int> bpType = null, string documentGroupID = "", bool WithOrderByGL = false,
            bool documentTypeAuthentication = false, bool discardReversedDocuments = false, int? CustomerCategoryID = null, string AccountCode = "", string BalanceFilter = "", bool WithType = false, bool WithConsolidated = false,bool isMonthwise = false)
        {
            companyID = "(" + companyID + ")";
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder ss = new StringBuilder();
            string qry_docuTypeAuthenticate = string.Empty;
            string qry_SalesInvoiceFilter = string.Empty, qry_ReceiptFilter = string.Empty, qry_PurchaseInvoiceFilter = string.Empty,
                qry_PaymentFilter = string.Empty;

            #region Filter Reversed Documents

            if (discardReversedDocuments)
            {
                qry_SalesInvoiceFilter = " And IsNull(Sal_InvoiceHeader.IsReversed, 0) = 0 And Sal_InvoiceHeader.ReversalID is null ";
                qry_ReceiptFilter = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                qry_PurchaseInvoiceFilter = " And IsNull(AR_InvoiceHeader.IsReversed, 0) = 0 And AR_InvoiceHeader.ReversalID is null ";
                qry_PaymentFilter = " And AP_PaymentHeader.ReversalComments is null ";
            }

            #endregion


            #region query

            string Query_BusinessPartnerTrailBalance = FnI.QueryResourceLibrary.GL.GL_TransactionHeader4.Query_BusinessPartnerTrailBalance.ToString().Replace("@CompanyID", companyID);
            if(isMonthwise == true)
                Query_BusinessPartnerTrailBalance = FnI.QueryResourceLibrary.GL.GL_TransactionHeader4.Query_BusinesspartnerTrialBalanceMonthWise.ToString().Replace("@CompanyID", companyID);


            if (WithType)
            {
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithType", " ,LastTab.BusinessPartnerName ");
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithGroup", " ,LastTab.BusinessPartnerName ");
            }
            else
            {
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithType", " ,'' as BusinessPartnerName ");
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithGroup", " ");
            }


            if (WithConsolidated)
            {
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithConsolidated", " ,'' as BusinessPartnerTypeCode ");
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@ConsolidatedGroup", " ");
            }
            else
            {
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithConsolidated", " ,AR_SET_BusinessPartnerTypes.FK_CodeCombinationID as BusinessPartnerTypeCode");
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@ConsolidatedGroup", " ,AR_SET_BusinessPartnerTypes.FK_CodeCombinationID");
            }


            if (WithOrderByGL)
            {
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithOrderByGL", " Order by fintab.BusinessPartnerTypeCode asc");
            }
            else 
            {
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithOrderByGL", " ");
            }

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", datefrom);
                sqlparams[3] = new SqlParameter("@ToDate", dateto);


                if (customer != null)
                    sb.Append(" and AR_SET_Customer.CustomerID between " + customer + "  and " + customerto + "");

                if (CustomerCategoryID != null)
                    sb.Append(" and AR_SET_Customer.CustomerCategoryID = " + CustomerCategoryID.ToString() + "");

                if (bpType.HasValue)
                    ss.Append("and AR_SET_BusinessPartnerTypes.TypeCode = " + bpType);

                if (documentGroupID != "")
                    ss.Append(" AND Set_DocumentType.GroupID in " + documentGroupID);

                if(AccountCode != "")
                    ss.Append("and AR_SET_BusinessPartnerTypes.FK_CodeCombinationID = '" + AccountCode + "'");


                if (documentTypeAuthentication)
                {
                    sqlparams[4] = new SqlParameter("@UserID", userID);
//                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
//                    AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                sqlparams[5] = new SqlParameter("@AccountCode", DBNull.Value);

                string CustoemrFilter = "";
                //if (customer.HasValue)
                //{
                //    CustoemrFilter = " WHERE CustomerID = " + customer + "";
                //}

                string BalanceWiseFilter = "";
                if (BalanceFilter != "")
                {
                    if (BalanceFilter == "Credit")
                        BalanceWiseFilter = " where ClosingAmountType = 'Credit'";

                    if (BalanceFilter == "Debit")
                        BalanceWiseFilter = " where ClosingAmountType = 'Debit'";
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_BusinessPartnerTrailBalance + BalanceWiseFilter, sb.ToString(), ss.ToString(),
                    qry_docuTypeAuthenticate, qry_SalesInvoiceFilter, qry_ReceiptFilter, qry_PurchaseInvoiceFilter, qry_PaymentFilter, qry_docuTypeAuthenticate) + CustoemrFilter,
                    sqlparams);

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

        public static DataTable ProvidentFundRegister(string companyID, int periodID, Nullable<int> customer, Nullable<int> customerto,
            Nullable<DateTime> datefrom, Nullable<DateTime> dateto, Guid userID, string bpType, string documentGroupID = "", bool WithOrderByGL = false,
            bool documentTypeAuthentication = false, bool discardReversedDocuments = false, int? CustomerCategoryID = null, string AccountCode = "", string BalanceFilter = "", bool WithType = false, bool WithConsolidated = false, bool isMonthwise = false)
        {
            companyID = "(" + companyID + ")";
            if (!string.IsNullOrEmpty(bpType))
                bpType = "(" + bpType + ")";

            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder ss = new StringBuilder();
            string qry_docuTypeAuthenticate = string.Empty;
            string qry_SalesInvoiceFilter = string.Empty, qry_ReceiptFilter = string.Empty, qry_PurchaseInvoiceFilter = string.Empty,
                qry_PaymentFilter = string.Empty;

            #region Filter Reversed Documents

            if (discardReversedDocuments)
            {
                qry_SalesInvoiceFilter = " And IsNull(Sal_InvoiceHeader.IsReversed, 0) = 0 And Sal_InvoiceHeader.ReversalID is null ";
                qry_ReceiptFilter = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                qry_PurchaseInvoiceFilter = " And IsNull(AR_InvoiceHeader.IsReversed, 0) = 0 And AR_InvoiceHeader.ReversalID is null ";
                qry_PaymentFilter = " And AP_PaymentHeader.ReversalComments is null ";
            }

            #endregion


            #region query

            string Query_BusinessPartnerTrailBalance = FnI.QueryResourceLibrary.GL.GL_TransactionHeader4.Query_BusinessPartnerTrailBalance.ToString().Replace("@CompanyID", companyID);
            if (isMonthwise == true)
                Query_BusinessPartnerTrailBalance = FnI.QueryResourceLibrary.GL.GL_TransactionHeader4.Query_ProvidentFundRegister.ToString().Replace("@CompanyID", companyID);


            if (WithType)
            {
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithType", " ,LastTab.BusinessPartnerName ");
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithGroup", " ,LastTab.BusinessPartnerName ");
            }
            else
            {
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithType", " ,'' as BusinessPartnerName ");
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithGroup", " ");
            }


            if (WithConsolidated)
            {
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithConsolidated", " ,'' as BusinessPartnerTypeCode ");
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@ConsolidatedGroup", " ");
            }
            else
            {
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithConsolidated", " ,AR_SET_BusinessPartnerTypes.FK_CodeCombinationID as BusinessPartnerTypeCode");
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@ConsolidatedGroup", " ,AR_SET_BusinessPartnerTypes.FK_CodeCombinationID");
            }


            if (WithOrderByGL)
            {
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithOrderByGL", " Order by fintab.BusinessPartnerTypeCode asc");
            }
            else
            {
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithOrderByGL", " ");
            }

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", datefrom);
                sqlparams[3] = new SqlParameter("@ToDate", dateto);


                if (customer != null)
                    sb.Append(" and AR_SET_Customer.CustomerID between " + customer + "  and " + customerto + "");

                if (CustomerCategoryID != null)
                    sb.Append(" and AR_SET_Customer.CustomerCategoryID = " + CustomerCategoryID.ToString() + "");

                if (bpType != null)
                    ss.Append("and AR_SET_BusinessPartnerTypes.TypeCode in " + bpType);

                if (documentGroupID != "")
                    ss.Append(" AND Set_DocumentType.GroupID in " + documentGroupID);

                if (AccountCode != "")
                    ss.Append("and AR_SET_BusinessPartnerTypes.FK_CodeCombinationID = '" + AccountCode + "'");


                if (documentTypeAuthentication)
                {
                    sqlparams[4] = new SqlParameter("@UserID", userID);
                    //                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
                    //                    AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                sqlparams[5] = new SqlParameter("@AccountCode", DBNull.Value);

                string CustoemrFilter = "";
                //if (customer.HasValue)
                //{
                //    CustoemrFilter = " WHERE CustomerID = " + customer + "";
                //}

                string BalanceWiseFilter = "";
                if (BalanceFilter != "")
                {
                    if (BalanceFilter == "Credit")
                        BalanceWiseFilter = " where ClosingAmountType = 'Credit'";

                    if (BalanceFilter == "Debit")
                        BalanceWiseFilter = " where ClosingAmountType = 'Debit'";
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_BusinessPartnerTrailBalance + BalanceWiseFilter, sb.ToString(), ss.ToString(),
                    qry_docuTypeAuthenticate, qry_SalesInvoiceFilter, qry_ReceiptFilter, qry_PurchaseInvoiceFilter, qry_PaymentFilter, qry_docuTypeAuthenticate) + CustoemrFilter,
                    sqlparams);

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

        public static DataTable ProvidentFundRegister_NEW(string companyID, int periodID, string customer,string customerto,
            Nullable<DateTime> datefrom, Nullable<DateTime> dateto, Guid userID, string bpType, string documentGroupID = "", bool WithOrderByGL = false,
            bool documentTypeAuthentication = false, bool discardReversedDocuments = false, int? CustomerCategoryID = null, string AccountCode = "", string BalanceFilter = "", bool WithType = false, bool WithConsolidated = false, bool isMonthwise = false)
        {
            companyID = "(" + companyID + ")";
            if (!string.IsNullOrEmpty(bpType))
                bpType = "(" + bpType + ")";

            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder ss = new StringBuilder();
            string qry_docuTypeAuthenticate = string.Empty;
            string qry_SalesInvoiceFilter = string.Empty, qry_ReceiptFilter = string.Empty, qry_PurchaseInvoiceFilter = string.Empty,
                qry_PaymentFilter = string.Empty;

            #region Filter Reversed Documents

            if (discardReversedDocuments)
            {
                qry_SalesInvoiceFilter = " And IsNull(Sal_InvoiceHeader.IsReversed, 0) = 0 And Sal_InvoiceHeader.ReversalID is null ";
                qry_ReceiptFilter = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                qry_PurchaseInvoiceFilter = " And IsNull(AR_InvoiceHeader.IsReversed, 0) = 0 And AR_InvoiceHeader.ReversalID is null ";
                qry_PaymentFilter = " And AP_PaymentHeader.ReversalComments is null ";
            }

            #endregion


            #region query

            string Query_BusinessPartnerTrailBalance = FnI.QueryResourceLibrary.GL.GL_TransactionHeader4.Query_BusinessPartnerTrailBalance.ToString().Replace("@CompanyID", companyID);
            if (isMonthwise == true)
                Query_BusinessPartnerTrailBalance = FnI.QueryResourceLibrary.GL.GL_TransactionHeader4.ProvidentFundRegister_NEW.ToString().Replace("@CompanyID", companyID);


            if (WithType)
            {
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithType", " ,LastTab.BusinessPartnerName ");
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithGroup", " ,LastTab.BusinessPartnerName ");
            }
            else
            {
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithType", " ,'' as BusinessPartnerName ");
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithGroup", " ");
            }


            if (WithConsolidated)
            {
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithConsolidated", " ,'' as BusinessPartnerTypeCode ");
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@ConsolidatedGroup", " ");
            }
            else
            {
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithConsolidated", " ,AR_SET_BusinessPartnerTypes.FK_CodeCombinationID as BusinessPartnerTypeCode");
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@ConsolidatedGroup", " ,AR_SET_BusinessPartnerTypes.FK_CodeCombinationID");
            }


            if (WithOrderByGL)
            {
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithOrderByGL", " Order by fintab.BusinessPartnerTypeCode asc");
            }
            else
            {
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithOrderByGL", " ");
            }

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", datefrom);
                sqlparams[3] = new SqlParameter("@ToDate", dateto);


                if (!string.IsNullOrEmpty(customer))
                    sb.Append(" and CAST(AR_SET_Customer.EmployeeCode AS bigint) between " + customer + "  and " + customerto + "");

                if (CustomerCategoryID != null)
                    sb.Append(" and AR_SET_Customer.CustomerCategoryID = " + CustomerCategoryID.ToString() + "");

                if (bpType != null)
                    ss.Append("and AR_SET_BusinessPartnerTypes.TypeCode in " + bpType);

                if (documentGroupID != "")
                    ss.Append(" AND Set_DocumentType.GroupID in " + documentGroupID);

                if (AccountCode != "")
                    ss.Append("and AR_SET_BusinessPartnerTypes.FK_CodeCombinationID = '" + AccountCode + "'");


                if (documentTypeAuthentication)
                {
                    sqlparams[4] = new SqlParameter("@UserID", userID);
                    //                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
                    //                    AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                sqlparams[5] = new SqlParameter("@AccountCode", DBNull.Value);

                string CustoemrFilter = "";
                //if (customer.HasValue)
                //{
                //    CustoemrFilter = " WHERE CustomerID = " + customer + "";
                //}

                string BalanceWiseFilter = "";
                if (BalanceFilter != "")
                {
                    if (BalanceFilter == "Credit")
                        BalanceWiseFilter = " where ClosingAmountType = 'Credit'";

                    if (BalanceFilter == "Debit")
                        BalanceWiseFilter = " where ClosingAmountType = 'Debit'";
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_BusinessPartnerTrailBalance, sb.ToString(), ss.ToString(),
                    qry_docuTypeAuthenticate, qry_SalesInvoiceFilter, qry_ReceiptFilter, qry_PurchaseInvoiceFilter, qry_PaymentFilter, qry_docuTypeAuthenticate, BalanceWiseFilter) + CustoemrFilter,
                    sqlparams);

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



        public static DataTable ProvidentFundRegisterTwo(string companyID, int periodID, string customer, string customerto,
            Nullable<DateTime> datefrom, Nullable<DateTime> dateto, Guid userID, string bpType, string documentGroupID = "", bool WithOrderByGL = false,
            bool documentTypeAuthentication = false, bool discardReversedDocuments = false, int? CustomerCategoryID = null, string AccountCode = "", string BalanceFilter = "", bool WithType = false, bool WithConsolidated = false, bool isMonthwise = false)
        {
            companyID = "(" + companyID + ")";
            if (!string.IsNullOrEmpty(bpType))
                bpType = "(" + bpType + ")";

            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder ss = new StringBuilder();
            string qry_docuTypeAuthenticate = string.Empty;
            string qry_SalesInvoiceFilter = string.Empty, qry_ReceiptFilter = string.Empty, qry_PurchaseInvoiceFilter = string.Empty,
                qry_PaymentFilter = string.Empty;

            #region Filter Reversed Documents

            if (discardReversedDocuments)
            {
                qry_SalesInvoiceFilter = " And IsNull(Sal_InvoiceHeader.IsReversed, 0) = 0 And Sal_InvoiceHeader.ReversalID is null ";
                qry_ReceiptFilter = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                qry_PurchaseInvoiceFilter = " And IsNull(AR_InvoiceHeader.IsReversed, 0) = 0 And AR_InvoiceHeader.ReversalID is null ";
                qry_PaymentFilter = " And AP_PaymentHeader.ReversalComments is null ";
            }

            #endregion


            #region query

            string Query_BusinessPartnerTrailBalance = FnI.QueryResourceLibrary.GL.GL_TransactionHeader4.Query_BusinessPartnerTrailBalance.ToString().Replace("@CompanyID", companyID);
            if (isMonthwise == true)
                Query_BusinessPartnerTrailBalance = FnI.QueryResourceLibrary.GL.GL_TransactionHeader4.Query_ProvidentFundRegisterTwo.ToString().Replace("@CompanyID", companyID);


            if (WithType)
            {
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithType", " ,LastTab.BusinessPartnerName ");
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithGroup", " ,LastTab.BusinessPartnerName ");
            }
            else
            {
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithType", " ,'' as BusinessPartnerName ");
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithGroup", " ");
            }


            if (WithConsolidated)
            {
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithConsolidated", " ,'' as BusinessPartnerTypeCode ");
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@ConsolidatedGroup", " ");
            }
            else
            {
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithConsolidated", " ,AR_SET_BusinessPartnerTypes.FK_CodeCombinationID as BusinessPartnerTypeCode");
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@ConsolidatedGroup", " ,AR_SET_BusinessPartnerTypes.FK_CodeCombinationID");
            }


            if (WithOrderByGL)
            {
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithOrderByGL", " Order by fintab.BusinessPartnerTypeCode asc");
            }
            else
            {
                Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithOrderByGL", " ");
            }

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", datefrom);
                sqlparams[3] = new SqlParameter("@ToDate", dateto);


                if (!string.IsNullOrEmpty(customer))
                    sb.Append(" and CAST(AR_SET_Customer.EmployeeCode AS bigint) between " + customer + "  and " + customerto + "");

                if (CustomerCategoryID != null)
                    sb.Append(" and AR_SET_Customer.CustomerCategoryID = " + CustomerCategoryID.ToString() + "");

                if (bpType != null)
                    ss.Append("and AR_SET_BusinessPartnerTypes.TypeCode in " + bpType);

                if (documentGroupID != "")
                    ss.Append(" AND Set_DocumentType.GroupID in " + documentGroupID);

                if (AccountCode != "")
                    ss.Append("and AR_SET_BusinessPartnerTypes.FK_CodeCombinationID = '" + AccountCode + "'");


                if (documentTypeAuthentication)
                {
                    sqlparams[4] = new SqlParameter("@UserID", userID);
                    //                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
                    //                    AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                sqlparams[5] = new SqlParameter("@AccountCode", DBNull.Value);

                string CustoemrFilter = "";
                //if (customer.HasValue)
                //{
                //    CustoemrFilter = " WHERE CustomerID = " + customer + "";
                //}

                string BalanceWiseFilter = "";
                if (BalanceFilter != "")
                {
                    if (BalanceFilter == "Credit")
                        BalanceWiseFilter = " where ClosingAmountType = 'Credit'";

                    if (BalanceFilter == "Debit")
                        BalanceWiseFilter = " where ClosingAmountType = 'Debit'";
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_BusinessPartnerTrailBalance + BalanceWiseFilter, sb.ToString(), ss.ToString(),
                    qry_docuTypeAuthenticate, qry_SalesInvoiceFilter, qry_ReceiptFilter, qry_PurchaseInvoiceFilter, qry_PaymentFilter, qry_docuTypeAuthenticate) + CustoemrFilter,
                    sqlparams);

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

        public static DataTable GET_BusinessPartnerTrailBalanceEmployeeWise(string companyID, int periodID, Nullable<int> customer, Nullable<int> customerto,
            Nullable<DateTime> datefrom, Nullable<DateTime> dateto, Guid userID, Nullable<int> bpType = null, string documentGroupID = "",
            bool documentTypeAuthentication = false, bool discardReversedDocuments = false, int? CustomerCategoryID = null, string AccountCode = "", string BalanceFilter = "", bool WithType = false, bool WithConsolidated = false)
        {
            companyID = "(" + companyID + ")";
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder ss = new StringBuilder();
            string qry_docuTypeAuthenticate = string.Empty;
            string qry_SalesInvoiceFilter = string.Empty, qry_ReceiptFilter = string.Empty, qry_PurchaseInvoiceFilter = string.Empty,
                qry_PaymentFilter = string.Empty;

            #region Filter Reversed Documents

            if (discardReversedDocuments)
            {
                qry_SalesInvoiceFilter = " And IsNull(Sal_InvoiceHeader.IsReversed, 0) = 0 And Sal_InvoiceHeader.ReversalID is null ";
                qry_ReceiptFilter = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                qry_PurchaseInvoiceFilter = " And IsNull(AR_InvoiceHeader.IsReversed, 0) = 0 And AR_InvoiceHeader.ReversalID is null ";
                qry_PaymentFilter = " And AP_PaymentHeader.ReversalComments is null ";
            }

            #endregion


            #region query

            string Query_BusinessPartnerTrailBalanceEmployeeWise = FnI.QueryResourceLibrary.GL.GL_TransactionHeader4.Query_BusinessPartnerTrailBalanceEmployeeWise.ToString().Replace("@CompanyID", companyID);

            if (WithType)
            {
                Query_BusinessPartnerTrailBalanceEmployeeWise = Query_BusinessPartnerTrailBalanceEmployeeWise.ToString().Replace("@WithType", " ,LastTab.BusinessPartnerName ");
                Query_BusinessPartnerTrailBalanceEmployeeWise = Query_BusinessPartnerTrailBalanceEmployeeWise.ToString().Replace("@WithGroup", " ,LastTab.BusinessPartnerName ");
            }
            else
            {
                Query_BusinessPartnerTrailBalanceEmployeeWise = Query_BusinessPartnerTrailBalanceEmployeeWise.ToString().Replace("@WithType", " ,'' as BusinessPartnerName ");
                Query_BusinessPartnerTrailBalanceEmployeeWise = Query_BusinessPartnerTrailBalanceEmployeeWise.ToString().Replace("@WithGroup", " ");
            }


            if (WithConsolidated)
            {
                Query_BusinessPartnerTrailBalanceEmployeeWise = Query_BusinessPartnerTrailBalanceEmployeeWise.ToString().Replace("@WithConsolidated", " ,'' as BusinessPartnerTypeCode ");
                Query_BusinessPartnerTrailBalanceEmployeeWise = Query_BusinessPartnerTrailBalanceEmployeeWise.ToString().Replace("@ConsolidatedGroup", " ");
            }
            else
            {
                Query_BusinessPartnerTrailBalanceEmployeeWise = Query_BusinessPartnerTrailBalanceEmployeeWise.ToString().Replace("@WithConsolidated", " ,AR_SET_BusinessPartnerTypes.FK_CodeCombinationID as BusinessPartnerTypeCode");
                Query_BusinessPartnerTrailBalanceEmployeeWise = Query_BusinessPartnerTrailBalanceEmployeeWise.ToString().Replace("@ConsolidatedGroup", " ,AR_SET_BusinessPartnerTypes.FK_CodeCombinationID");
            }





            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", datefrom);
                sqlparams[3] = new SqlParameter("@ToDate", dateto);


                if (customer != null)
                    sb.Append(" and AR_SET_Customer.CustomerID between " + customer + "  and " + customerto + "");

                if (CustomerCategoryID != null)
                    sb.Append(" and AR_SET_Customer.CustomerCategoryID = " + CustomerCategoryID.ToString() + "");

                if (bpType.HasValue)
                    ss.Append("and AR_SET_BusinessPartnerTypes.TypeCode = " + bpType);

                if (documentGroupID != "")
                    ss.Append(" AND Set_DocumentType.GroupID in " + documentGroupID);

                if (AccountCode != "")
                    ss.Append("and AR_SET_BusinessPartnerTypes.FK_CodeCombinationID = '" + AccountCode + "'");


                if (documentTypeAuthentication)
                {
                    sqlparams[4] = new SqlParameter("@UserID", userID);
                    //                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
                    //                    AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                sqlparams[5] = new SqlParameter("@AccountCode", DBNull.Value);

                string CustoemrFilter = "";
                //if (customer.HasValue)
                //{
                //    CustoemrFilter = " WHERE CustomerID = " + customer + "";
                //}

                string BalanceWiseFilter = "";
                if (BalanceFilter != "")
                {
                    if (BalanceFilter == "Credit")
                        BalanceWiseFilter = " where ClosingAmountType = 'Credit'";

                    if (BalanceFilter == "Debit")
                        BalanceWiseFilter = " where ClosingAmountType = 'Debit'";
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_BusinessPartnerTrailBalanceEmployeeWise + BalanceWiseFilter, sb.ToString(), ss.ToString(),
                    qry_docuTypeAuthenticate, qry_SalesInvoiceFilter, qry_ReceiptFilter, qry_PurchaseInvoiceFilter, qry_PaymentFilter, qry_docuTypeAuthenticate) + CustoemrFilter,
                    sqlparams);

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

        public static DataTable GetDataByKeyForApprove(int companyID, int periodID, int transactionID, int documentTypeID, string documentNo)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TransactionID", transactionID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[4] = new SqlParameter("@DocumentNo", documentNo);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionHeader.Query_Select_DataByKey_ForApprove, sqlparams);

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



        public static DataTable GetAllDataWithOtherTablesForApproveDetail(int companyID, int periodID, int transactionID, int documentTypeID, string documentNo)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TransactionID", transactionID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[4] = new SqlParameter("@DocumentNo", documentNo);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionHeader.Query_SelectAll_ForApproveDetail, sqlparams);

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

        public static DataTable GetTransactionVoucherPrintingData(int companyID, int periodID, Guid userID, Nullable<int> documentType = null,
            Nullable<int> documentFrom = null, Nullable<int> documentTo = null, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null,
            string DocumentTypeName = null, Nullable<short> documentGroupID = null, bool documentTypeAuthentication = false, int CurrentFiscalID = 0, bool Isfabrics = false)
        {
            StringBuilder additionalQueryParameters = new StringBuilder();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataTable dtToReturn = null;
            string qry_docuTypeAuthenticate = string.Empty;

            try
            {
                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    additionalQueryParameters.Append(" AND (Cast(GL_TransactionHeader.DocumentDate as date) BETWEEN @DocumentDateFrom AND @DocumentDateTo)");
                    sqlParams.Add(new SqlParameter("@DocumentDateFrom", dateFrom.Value));
                    sqlParams.Add(new SqlParameter("@DocumentDateTo", dateTo.Value));
                }
                else if (dateFrom.HasValue)
                {
                    additionalQueryParameters.Append(" And (GL_TransactionHeader.DocumentDate >= @DocumentDateFrom)");
                    sqlParams.Add(new SqlParameter("@DocumentDateFrom", dateFrom.Value));
                }
                else if (dateTo.HasValue)
                {
                    additionalQueryParameters.Append(" And (GL_TransactionHeader.DocumentDate <= @DocumentDateTo)");
                    sqlParams.Add(new SqlParameter("@DocumentDateTo", dateTo.Value));
                }

                if (documentType.HasValue)
                {
                    additionalQueryParameters.Append(" AND (GL_TransactionHeader.DocumentTypeID = @DocumentTypeID)");
                    sqlParams.Add(new SqlParameter("@DocumentTypeID", documentType.Value));
                }

                if (documentFrom.HasValue && documentTo.HasValue)
                {
                    additionalQueryParameters.Append(" AND (GL_TransactionHeader.DocumentNo Between @DocumentNoFrom AND @DocumentNoTo)");
                    sqlParams.Add(new SqlParameter("@DocumentNoFrom", documentFrom.Value));
                    sqlParams.Add(new SqlParameter("@DocumentNoTo", documentTo.Value));
                }

                if (DocumentTypeName != null)
                    additionalQueryParameters.Append(" and SET_DocumentType.DocumentType like '%" + DocumentTypeName + "%'");

                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));

                if (documentGroupID.HasValue)
                    additionalQueryParameters.Append(" AND Set_DocumentType.GroupID = " + documentGroupID);

                if (documentTypeAuthentication)
                {
                    sqlParams.Add(new SqlParameter("@UserID", userID));
                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                string str = !string.IsNullOrEmpty(additionalQueryParameters.ToString()) ? additionalQueryParameters.ToString() : string.Empty;
                str = " " + str + "  Order by  GL_TransactionHeader.DocumentDate Asc,TransactionType Desc ";
                string Query = "";
                if (!Isfabrics)
                    Query = FnI.QueryResourceLibrary.GL.GL_TransactionHeader4.Query_TransactionVoucherPrinting;
                else
                    Query = FnI.QueryResourceLibrary.GL.GL_TransactionHeader.Query_TransactionVoucherPrintingAFF;


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, qry_docuTypeAuthenticate) + str,
                    sqlParams.ToArray());

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

        public static DataTable GetTransactionVoucherPrintingData_New(int companyID, int periodID, Guid userID, Nullable<int> documentType = null,
            Nullable<int> documentFrom = null, Nullable<int> documentTo = null, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null,
            string DocumentTypeName = null, Nullable<short> documentGroupID = null, bool documentTypeAuthentication = false, int CurrentFiscalID = 0, bool Isfabrics = false)
        {
            StringBuilder additionalQueryParameters = new StringBuilder();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataTable dtToReturn = null;
            string qry_docuTypeAuthenticate = string.Empty;

            try
            {
                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    additionalQueryParameters.Append(" AND (Cast(GL_TransactionHeader.DocumentDate as date) BETWEEN @DocumentDateFrom AND @DocumentDateTo)");
                    sqlParams.Add(new SqlParameter("@DocumentDateFrom", dateFrom.Value));
                    sqlParams.Add(new SqlParameter("@DocumentDateTo", dateTo.Value));
                }
                else if (dateFrom.HasValue)
                {
                    additionalQueryParameters.Append(" And (GL_TransactionHeader.DocumentDate >= @DocumentDateFrom)");
                    sqlParams.Add(new SqlParameter("@DocumentDateFrom", dateFrom.Value));
                }
                else if (dateTo.HasValue)
                {
                    additionalQueryParameters.Append(" And (GL_TransactionHeader.DocumentDate <= @DocumentDateTo)");
                    sqlParams.Add(new SqlParameter("@DocumentDateTo", dateTo.Value));
                }

                if (documentType.HasValue)
                {
                    additionalQueryParameters.Append(" AND (GL_TransactionHeader.DocumentTypeID = @DocumentTypeID)");
                    sqlParams.Add(new SqlParameter("@DocumentTypeID", documentType.Value));
                }

                if (documentFrom.HasValue && documentTo.HasValue)
                {
                    additionalQueryParameters.Append(" AND (GL_TransactionHeader.DocumentNo Between @DocumentNoFrom AND @DocumentNoTo)");
                    sqlParams.Add(new SqlParameter("@DocumentNoFrom", documentFrom.Value));
                    sqlParams.Add(new SqlParameter("@DocumentNoTo", documentTo.Value));
                }

                if (DocumentTypeName != null)
                    additionalQueryParameters.Append(" and SET_DocumentType.DocumentType like '%" + DocumentTypeName + "%'");

                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));

                if (documentGroupID.HasValue)
                    additionalQueryParameters.Append(" AND Set_DocumentType.GroupID = " + documentGroupID);

                if (documentTypeAuthentication)
                {
                    sqlParams.Add(new SqlParameter("@UserID", userID));
                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                string str = !string.IsNullOrEmpty(additionalQueryParameters.ToString()) ? additionalQueryParameters.ToString() : string.Empty;
                str = " " + str + "  Order by  GL_TransactionHeader.DocumentDate Asc,TransactionType Desc ";
                string Query = "";
                if (!Isfabrics)
                    Query = FnI.QueryResourceLibrary.GL.GL_TransactionHeader4.Query_TransactionVoucherPrinting_New;
                else
                    Query = FnI.QueryResourceLibrary.GL.GL_TransactionHeader.Query_TransactionVoucherPrintingAFF;


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, qry_docuTypeAuthenticate)+ str,
                    sqlParams.ToArray());

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

        public static DataTable GetAuditTrailData(string companyID, int periodID, Guid userID, int? documentType = null, int? toDocumentType = null,
            DateTime? dateFrom = null, DateTime? dateTo = null, Guid? approvedBy = null, Guid? createdBy = null, Nullable<int> documentGroupID = null,
            bool documentTypeAuthentication = false)
        {
            StringBuilder additionalQueryParameters = new StringBuilder();
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            DataTable dtToReturn = null;
            string documentTypeFilter = string.Empty;
            string qry_docuTypeAuthenticate = string.Empty;

            try
            {
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));
                sqlparams.Add(new SqlParameter("@DocumentTypeID", documentType));

                if (approvedBy.HasValue)
                    sqlparams.Add(new SqlParameter("@ApprovedBy", approvedBy));
                else
                    sqlparams.Add(new SqlParameter("@ApprovedBy", DBNull.Value));

                if (createdBy.HasValue)
                    sqlparams.Add(new SqlParameter("@UserID", createdBy));
                else
                    sqlparams.Add(new SqlParameter("@UserID", DBNull.Value));

                if (dateFrom != null && dateTo != null)
                {
                    sqlparams.Add(new SqlParameter("@DateFrom", ((DateTime)dateFrom).ToString("yyyy-MM-dd")));
                    sqlparams.Add(new SqlParameter("@DateTo", ((DateTime)dateTo).ToString("yyyy-MM-dd")));
                    additionalQueryParameters.Append(" AND Cast(GL_TransactionHeader.DocumentDate as Date) Between @DateFrom AND @DateTo ");
                }

                if (dateFrom != null && dateTo == null)
                {
                    sqlparams.Add(new SqlParameter("@DateFrom", ((DateTime)dateFrom).ToString("yyyy-MM-dd")));
                    additionalQueryParameters.Append(" AND Cast(GL_TransactionHeader.DocumentDate as Date) = @DateFrom ");
                }

                if (documentGroupID.HasValue)
                    additionalQueryParameters.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (documentType != -1 && toDocumentType != -1)
                    documentTypeFilter = " AND GL_TransactionHeader.DocumentTypeID between " + documentType + " AND " + toDocumentType + " ";

                if (documentGroupID.HasValue)
                    documentTypeFilter = " AND Set_DocumentType.GroupID = " + documentGroupID;

                if (documentTypeAuthentication)
                {
                    sqlparams.Add(new SqlParameter("@ViewerID", userID));
                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @ViewerID ";
                }

                companyID = "(" + companyID + ")";
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_GetAuditTrailReport, additionalQueryParameters, documentTypeFilter,
                    qry_docuTypeAuthenticate).Replace("@CompanyID", companyID), sqlparams.ToArray());

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

        public static DataSet GetMinMaxDocumentDates(int periodID)
        {
            DataSet dsToReturn;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);

                dsToReturn = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_Max_Min_DocumentDate, sqlparams);
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static DataTable GetLedgerTransactionListReport(string companyID, int periodID, string accountCodeFrom, Guid userID, string dateFrom = null,
            string dateTo = null, Nullable<short> documentGroupID = null, Nullable<short> DocumentTypeID = null, bool documentTypeAuthentication = false)
        {
            DataTable dt = null;
            string accountCodeFilter = string.Empty;
            string additionalParameters = string.Empty;
            string additionalParameters1 = string.Empty;
            string qry_docuTypeAuthenticate = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (dateFrom != "")
                {
                    additionalParameters = " AND Cast(Convert(Varchar(10),GL_TransactionHeader.DocumentDate,110) as datetime) between @FromDate and @ToDate";
                    sqlparams[2] = new SqlParameter("@FromDate", dateFrom);
                    sqlparams[3] = new SqlParameter("@ToDate", dateTo);
                }

                if (accountCodeFrom != string.Empty)
                {
                    accountCodeFilter = " And GL_TransactionHeader.TransactionID in (select TransactionID from GL_TransactionDetail where GL_TransactionDetail.CodeCombinationID =  '" + accountCodeFrom + "') ";
                }

                if (documentGroupID.HasValue)
                    additionalParameters = additionalParameters + " AND Set_DocumentType.GroupID = " + documentGroupID;
                if (DocumentTypeID.HasValue)
                    additionalParameters = additionalParameters + " AND Set_DocumentType.DocumentTypeID = " + DocumentTypeID;
                if (documentTypeAuthentication)
                {
                    sqlparams[4] = new SqlParameter("@UserID", userID);
                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                companyID = "(" + companyID + ")";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Get_LedgerTransactionList.ToString().Replace("@CompanyID", companyID), additionalParameters, accountCodeFilter,
                    qry_docuTypeAuthenticate), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetLedgerTransactionListReport2(string companyID, int periodID, string accountCodeFrom, Guid userID, string dateFrom = null,
            string dateTo = null, string documentGroupID = null, string DocumentTypeID = null, bool documentTypeAuthentication = false, int? CostCenterID = null)
        {
            DataTable dt = null;
            string accountCodeFilter = string.Empty;
            string additionalParameters = string.Empty;
            string additionalParameters1 = string.Empty;
            string qry_docuTypeAuthenticate = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (dateFrom != "")
                {
                    additionalParameters = " AND Cast(Convert(Varchar(10),GL_TransactionHeader.DocumentDate,110) as datetime) between @FromDate and @ToDate";
                    sqlparams[2] = new SqlParameter("@FromDate", dateFrom);
                    sqlparams[3] = new SqlParameter("@ToDate", dateTo);
                }

                if (accountCodeFrom != string.Empty)
                {
                    accountCodeFilter = " And GL_TransactionHeader.TransactionID in (select TransactionID from GL_TransactionDetail where GL_TransactionDetail.CodeCombinationID =  '" + accountCodeFrom + "') ";
                }

                //if (documentGroupID.HasValue)
                //    additionalParameters = additionalParameters + " AND Set_DocumentType.GroupID = " + documentGroupID;
                if (documentGroupID != "")
                    additionalParameters = additionalParameters + (" AND Set_DocumentType.GroupID in " + documentGroupID.ToString() + " ");

                //if (DocumentTypeID.HasValue)
                //    additionalParameters = additionalParameters + " AND Set_DocumentType.DocumentTypeID = " + DocumentTypeID;
                if (DocumentTypeID != "")
                    additionalParameters = additionalParameters + (" AND Set_DocumentType.DocumentTypeID in " + DocumentTypeID.ToString() + " ");
                if (documentTypeAuthentication)
                {
                    sqlparams[4] = new SqlParameter("@UserID", userID);
                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                companyID = "(" + companyID + ")";

                if (CostCenterID.HasValue)
                    additionalParameters = additionalParameters + (" AND SET_CostCenter.CostCenterID = " + CostCenterID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Get_LedgerTransactionList.ToString().Replace("@CompanyID", companyID), additionalParameters, accountCodeFilter,
                    qry_docuTypeAuthenticate), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public bool UpdateDataByKey_StatusCode(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[1] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", f_DocumentTypeID);
                sqlparams[3] = new SqlParameter("@DocumentNo", f_DocumentNo);


                if (f_ApprovedBy.HasValue)
                    sqlparams[4] = new SqlParameter("@ApprovedBy", f_ApprovedBy.Value);
                else
                    sqlparams[4] = new SqlParameter("@ApprovedBy", DBNull.Value);

                if (f_DateOfApproval.HasValue)
                    sqlparams[5] = new SqlParameter("@DateOfApproval", f_DateOfApproval.Value);
                else
                    sqlparams[5] = new SqlParameter("@DateOfApproval", DBNull.Value);


                if (f_StatusCode.HasValue)
                    sqlparams[6] = new SqlParameter("@StatusCode", f_StatusCode.Value);
                else
                    sqlparams[6] = new SqlParameter("@StatusCode", DBNull.Value);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_StatusCode_By_DocNo, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_StatusCode_By_DocNo, sqlparams) > 0)
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

        public static DataTable GetStatementOfAccountReport(int companyID, int periodID, string dateFrom = null, string dateTo = null, string CodeCombinationID = "")
        {
            DataTable dt = null;
            string additionalParameters = string.Empty;
            string additionalParameters1 = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", dateFrom);
                sqlparams[3] = new SqlParameter("@ToDate", dateTo);
                sqlparams[4] = new SqlParameter("@CodeCombinationID", CodeCombinationID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.GL.GL_TransactionHeader.Query_GetStatementOfAccountReport), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetExpenseAccountReport(int companyID, int periodID, Nullable<int> costCenterID, int chartOfAccountID, string accountCodeFrom,
            string accountCodeTo, Nullable<int> depatmentID, Nullable<DateTime> date)
        {
            StringBuilder additionalQueryParameters = new StringBuilder();
            List<SqlParameter> sqlParams = new List<SqlParameter>();

            DataTable dtToReturn = null;

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@ChartOfAccountID", chartOfAccountID));

                if (costCenterID.HasValue)
                    sqlParams.Add(new SqlParameter("@CostCenterID", costCenterID.Value));
                else
                    sqlParams.Add(new SqlParameter("@CostCenterID", DBNull.Value));

                if (depatmentID.HasValue)
                    sqlParams.Add(new SqlParameter("@DepartmentID", depatmentID.Value));
                else
                    sqlParams.Add(new SqlParameter("@DepartmentID", DBNull.Value));

                #region Account Code filter

                if (!string.IsNullOrEmpty(accountCodeFrom) && !string.IsNullOrEmpty(accountCodeTo))
                {
                    additionalQueryParameters.Append(" AND GL_TransactionDetail.CodeCombinationID BETWEEN @AccountCodeFrom AND @AccountCodeTo ");
                    sqlParams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                }
                else if (!string.IsNullOrEmpty(accountCodeFrom))
                {
                    additionalQueryParameters.Append(" AND GL_TransactionDetail.CodeCombinationID >= @AccountCodeFrom ");
                    sqlParams.Add(new SqlParameter("@AccountCodeFrom", accountCodeFrom));
                }
                else if (!string.IsNullOrEmpty(accountCodeTo))
                {
                    additionalQueryParameters.Append(" AND GL_TransactionDetail.CodeCombinationID <= @AccountCodeTo ");
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", accountCodeTo));
                }

                #endregion

                if (date.HasValue)
                {
                    additionalQueryParameters.Append(" AND GL_TransactionDetail.CreateDate BETWEEN @FromDate AND @ToDate ");
                    sqlParams.Add(new SqlParameter("@FromDate", DateUtil.ConvertToString(date.Value, true) + " 00:00:00"));
                    sqlParams.Add(new SqlParameter("@ToDate", DateUtil.ConvertToString(date.Value, true) + " 23:59:59"));
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_ExpenseAccountReport, additionalQueryParameters.ToString()), sqlParams.ToArray());

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

        public static DataTable GetAccountGINData(int companyID, int periodID, int documentTypeID, long DocumentNo,int FiscalID, SqlConnection objConnection = null,
            SqlTransaction objSQL_Transaction = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[3] = new SqlParameter("@DocumentNo", DocumentNo);
                sqlparams[4] = new SqlParameter("@FiscalID", FiscalID);

                DataSet ds = null;

                if (objSQL_Transaction != null)
                    ds = SqlHelper.ExecuteDataset(objSQL_Transaction, CommandType.Text, Query_GetAccountGINData, sqlparams);
                if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetAccountGINData, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAccountGINData, sqlparams);

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

        public static DataTable GetAccountGRNData(int companyID, int periodID, int documentTypeID, long DocumentNo, SqlConnection objConnection = null,
    SqlTransaction objSQL_Transaction = null, Nullable<int> fiscalID = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[3] = new SqlParameter("@DocumentNo", DocumentNo);
                if (fiscalID.HasValue)
                sqlparams[4] = new SqlParameter("@fiscalID", fiscalID);
                else
                    sqlparams[4] = new SqlParameter("@fiscalID", DBNull.Value);
                DataSet ds = null;

                if (objSQL_Transaction != null)
                    ds = SqlHelper.ExecuteDataset(objSQL_Transaction, CommandType.Text, Query_GetAccountGRNData, sqlparams);
                if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetAccountGRNData, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAccountGRNData, sqlparams);

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

        public static DataTable GetAccountRETURNData(int companyID, int periodID, int documentTypeID, long DocumentNo, SqlConnection objConnection = null,
    SqlTransaction objSQL_Transaction = null, Nullable<int> fiscalID = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[3] = new SqlParameter("@DocumentNo", DocumentNo);
                if (fiscalID.HasValue)
                    sqlparams[4] = new SqlParameter("@fiscalID", fiscalID);
                else
                    sqlparams[4] = new SqlParameter("@fiscalID", DBNull.Value);
                DataSet ds = null;

                if (objSQL_Transaction != null)
                    ds = SqlHelper.ExecuteDataset(objSQL_Transaction, CommandType.Text, Query_GetAccountRETURNData, sqlparams);
                if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetAccountRETURNData, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAccountRETURNData, sqlparams);

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

        public static bool UpdateGLHeaderDebitCredit(int companyID, int periodID, int transactionID, decimal controlDebit, decimal controlCredit, string narration,
            SqlTransaction objTransaction)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TransactionID", transactionID);
                sqlparams[3] = new SqlParameter("@ControlDebit", controlDebit);
                sqlparams[4] = new SqlParameter("@ControlCredit", controlCredit);
                sqlparams[5] = new SqlParameter("@Narration", narration);

                if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_UpdateGLHeaderDebitCredit, sqlparams) > 0)
                    result = true;

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataRow GetDataByDocumentType(int companyID, int periodID, int documentTypeID, int documentNo, int CurrentFiscalID)
        {
            DataRow drToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[3] = new SqlParameter("@DocumentNo", documentNo);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByDocumentType, sqlparams);

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

        public static DataRow GetSaleAgainstSO_FromGL(int companyID, int periodID, int documentTypeID, int documentNo)
        {
            DataRow drToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[3] = new SqlParameter("@DocumentNo", documentNo);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(FnI.QueryResourceLibrary.GL.GL_TransactionHeader.Query_Select_SaleAgainstSo_DispatchNo, documentNo), sqlparams);

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

        public static bool Update_ControlDebitCreditAutomatically(int companyID, int periodID, int transactionID, SqlTransaction objTransaction)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TransactionID", transactionID);

                if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_UpdateControlDebitCreditAutomatically, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static bool Delete_By_PrimaryKey(int companyID, int periodID, int transactionID, SqlTransaction objTransaction)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TransactionID", transactionID);

                if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_By_PrimaryKey, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataTable GetAllActiveDataForLookupVoucher(Nullable<int> companyID, Nullable<bool> isActive = true,
    SqlConnection objConnection = null, SqlTransaction objTransaction = null, string filter = null)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (isActive.HasValue)
                    sqlparams[1] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[1] = new SqlParameter("@IsActive", DBNull.Value);

                DataSet ds = null;

                if (objTransaction != null)
                {
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text,
                        string.Format(FnI.QueryResourceLibrary.GL.GL_TransactionHeader.OverrideQueryString, filter), sqlparams);
                }
                else if (objConnection != null)
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.GL.GL_TransactionHeader.OverrideQueryString,
                        filter), sqlparams);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.GL.GL_TransactionHeader.OverrideQueryString, filter),
                        sqlparams);
                }

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

        public static DataTable Get_DataForGLCodeParameterReport(Nullable<int> CompanyID, Nullable<int> PeriodID, string reportsFilter, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);

                const string Query_ForGLCodeParameterReport = @"
SELECT DISTINCT FinancialName, ElementName as ElementType, HeaderCOACodeFrom, HeaderCOACodeTo, DetailCOACodeFrom, DetailCOACodeTo
--(CASE WHEN COACodeFrom = '' THEN COACodeFromDetail
--      WHEN COACodeFromDetail = '' THEN COACodeFrom
--      ELSE '' END) AS COACodeFrom,
--(CASE WHEN COACodeTo = '' THEN COACodeToDetail
--      WHEN COACodeToDetail = '' THEN COACodeTo
--      ELSE '' END) AS COACodeTo
FROM (

SELECT DISTINCT Set_FinancialHeader.FinancialID, FinancialName, ElementName, ISNULL(Set_FinancialHeader.COACodeFrom,'') AS HeaderCOACodeFrom, ISNULL(Set_FinancialHeader.COACodeTo,'') AS HeaderCOACodeTo, 
Set_FinancialDetail.FinancialHeaderID, Set_FinancialDetail.FinancialDetailID, ISNULL(Set_FinancialDetail.COACodeFrom,'') AS DetailCOACodeFrom, ISNULL(Set_FinancialDetail.COACodeTo,'') AS DetailCOACodeTo
,Set_FinancialHeader.CompanyID, Set_FinancialHeader.PeriodID
FROM Set_FinancialHeader
LEFT JOIN Set_FinancialDetail ON Set_FinancialDetail.FinancialHeaderID = Set_FinancialHeader.FinancialID
) AS Fintab
WHERE (Fintab.DetailCOACodeFrom != '' OR Fintab.DetailCOACodeTo != ''
AND Fintab.HeaderCOACodeFrom != '' OR Fintab.HeaderCOACodeTo != '')
AND (Fintab.CompanyID = @CompanyID OR @CompanyID IS NULL)
AND (Fintab.PeriodID = @PeriodID OR @PeriodID IS NULL)
{0}
";
                if (reportsFilter != null && reportsFilter != "" && reportsFilter != "'All'")
                {
                    sb.Append(" AND Fintab.FinancialName in (" + reportsFilter + ")");
                }

                DataSet ds = null;
                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_ForGLCodeParameterReport, sb),
                     sqlparams);

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

        //public static DataTable Get_ProfitAndLossAccountReport(int companyID, int periodID, int chartOfAccountID, string reportsFilter, Guid userID,
        //    Nullable<DateTime> datefrom = null, Nullable<DateTime> dateto = null, bool? IsProfitAndLoss = false, Nullable<short> DocumentGroupID = null)
        public static DataTable Get_ProfitAndLossAccountReport(string companyID, int periodID, int chartOfAccountID, string reportsFilter, Guid userID, Nullable<DateTime> datefrom = null,
       Nullable<DateTime> dateto = null, bool? IsProfitAndLoss = false, Nullable<short> DocumentGroupID = null, int? Segment = null, string NoteNumber = "", Nullable<bool> WithoutClosing = null, bool WithoutClosing_New = false, string fiscalId = "", int? CCID = null, bool chk_DateFromBelowFiscal = false)
        {
            DataTable dtToReturn = null;
            SqlConnection objConnection = ConnectionHelper.GetOpenSqlConnection();

            try
            {
                Dictionary<string, decimal> dic_Results = new Dictionary<string, decimal>();
                DataTable dt_FinancialHeader = Set_FinancialHeader.Get_FinacialHeader(reportsFilter, objConnection, NoteNumber,false, companyID);
                DataTable dt_FinancialDetail = Set_FinancialDetail.Get_FinacialDetail(reportsFilter, objConnection, Segment, companyID);

                // BASIC SETUP FOR HEADER - ADDING CUSTOM REPORTS
                DataTable dtFromTB = new DataTable();


                dtFromTB = dt_FinancialHeader.Clone();
                #region Segment Filter Header

                if (Segment.HasValue)
                {

                    string stringDel = new SET_Company(companyID.ToIntOrZero()).BusinessId_Business.COASeparater;

                    char[] delimeter = stringDel.ToCharArray();

                    DataTable dt_Filtered = dt_FinancialHeader.Clone();

                    List<string> lst_AccountCodes = dt_FinancialHeader.AsEnumerable()
                 .Where(x => x.Field<string>("COACodeFrom") != "" && x.Field<string>("COACodeFrom").Split(delimeter).Length == Segment)
                 .Select(x => x.Field<string>("COACodeFrom")).ToList();
                    foreach (string codeCombinationID in lst_AccountCodes)
                    {

                        //{
                        DataRow[] drs_ToImport = dt_FinancialHeader.AsEnumerable().Where(x => x.Field<string>("COACodeFrom").Equals(codeCombinationID)).ToArray();

                        foreach (DataRow dr_ToImport in drs_ToImport)
                            dt_Filtered.ImportRow(dr_ToImport);

                        dt_Filtered.AcceptChanges();
                        // }
                    }

                    dt_FinancialHeader = dt_Filtered.Copy();
                }

                #endregion

                if (dt_FinancialHeader != null)
                {

                    //header data explosion

                    foreach (DataRow drFinancial in dt_FinancialHeader.Rows)
                    {
                        if (drFinancial["COACodeFrom"].ToString() != "")// && drFinancial["COACodeTo"].ToString()!="")
                        {

                            DataTable dt = GL_TransactionDetail.GetTrialBalanceDataForFinancialForm(-1, -1, companyID, chartOfAccountID, periodID,
                                -1, -1, userID, datefrom, dateto, null, null, null, null, null, DocumentGroupID, false, false, false, drFinancial["COACodeFrom"].ToString()
                            , drFinancial["COACodeTo"].ToString(), Convert.ToInt32(drFinancial["ElementSequence"]));



                            if (dt != null)
                            {

                                foreach (DataRow dr in dt.Rows)
                                {
                                    dtFromTB.Rows.Add(dr.ItemArray);
                                }

                            }
                        }


                    }



                    //detail chart of account from to business partner explosion
                    #region Segment Filter Detail

                    if (Segment.HasValue)
                    {

                        string stringDel = new SET_Company(companyID.ToIntOrZero()).BusinessId_Business.COASeparater;

                        char[] delimeter = stringDel.ToCharArray();

                        DataTable dt_Filtered = dt_FinancialDetail.Clone();

                        List<string> lst_AccountCodes = dt_FinancialDetail.AsEnumerable()
                     .Where(x => x.Field<string>("COACodeFrom").Split(delimeter).Length == Segment)
                     .Select(x => x.Field<string>("COACodeFrom")).ToList();
                        foreach (string codeCombinationID in lst_AccountCodes)
                        {

                            //{
                            DataRow[] drs_ToImport = dt_FinancialDetail.AsEnumerable().Where(x => x.Field<string>("COACodeFrom").Equals(codeCombinationID)).ToArray();

                            foreach (DataRow dr_ToImport in drs_ToImport)
                                dt_Filtered.ImportRow(dr_ToImport);

                            dt_Filtered.AcceptChanges();
                            // }
                        }

                        dt_FinancialDetail = dt_Filtered.Copy();
                    }

                    #endregion

                    foreach (DataRow drFinancial in dt_FinancialDetail.Rows)
                    {
                        if (drFinancial["COACodeFrom"].ToString() != "" && drFinancial["COACodeTo"].ToString() != "" && drFinancial["isBusinessPartner"].ToString() != "False" && drFinancial["BankID"].ToString() == "")
                        {
                            DataTable dt = GL_TransactionDetail.GetTrialBalanceDataForFinancialBusiness(-1, -1, companyID, chartOfAccountID, periodID,
                            -1, -1, userID, datefrom, dateto, null, null, null, null, null, (short?)null, false, false, false, drFinancial["COACodeFrom"].ToString()
                            , drFinancial["COACodeTo"].ToString(), Convert.ToInt32(drFinancial["FinancialID"]), drFinancial["FinancialID"].ToString() != "" ? Convert.ToInt32(drFinancial["FinancialID"]) : (int?)null, Convert.ToInt32(drFinancial["TypeCodeID"]));

                            if (dt != null)
                            {

                                foreach (DataRow dr in dt.Rows)
                                {
                                    dtFromTB.Rows.Add(dr.ItemArray);
                                }

                            }
                        }

                        if (drFinancial["COACodeFrom"].ToString() != "" && drFinancial["COACodeTo"].ToString() != "" && drFinancial["BankID"].ToString() != "")
                        {
                            DataTable dt = null;
                            using (AR_SET_Bank obj_Bank = new AR_SET_Bank())
                            {
                                dt = obj_Bank.GetBankStatusDataSummaryForFinancial(companyID.ToString(), periodID, -1,
                                    -1, DateTime.Parse(datefrom.ToString()),DateTime.Parse(dateto.ToString()), -1, int.Parse(drFinancial["BankID"].ToString()), false, userID,
                                    "", null, true, drFinancial["COACodeFrom"].ToString(), drFinancial["COACodeTo"].ToString(), int.Parse(drFinancial["FinancialID"].ToString()));
                            };

                            if (dt != null)
                            {

                                foreach (DataRow dr in dt.Rows)
                                {
                                    dtFromTB.Rows.Add(dr.ItemArray);
                                }

                            }
                        
                        }

                        if (drFinancial["COACodeFrom"].ToString() != "" && drFinancial["COACodeTo"].ToString() != "" && drFinancial["BankID"].ToString() == "" && drFinancial["isBusinessPartner"].ToString() == "")
                        {

                            using (GL_TransactionDetail obj_GLDetail = new GL_TransactionDetail())
                            {
                                DataTable dsData = obj_GLDetail.Account_Ledger_ReportForFinancial(companyID.ToString(), periodID, drFinancial["ChartOfAccountID"].ToString().ToIntOrZero(), DateTime.Parse(datefrom.ToString()), DateTime.Parse(dateto.ToString()),
                                    drFinancial["COACodeFrom"].ToString(), drFinancial["COACodeTo"].ToString(), -1, -1, false, "", userID,
                                    true, null, "", "", int.Parse(drFinancial["FinancialID"].ToString()), bool.Parse(drFinancial["isCurrentYearActivity"].ToString()));


                                if (dsData != null && dsData.Rows.Count > 0)
                                {
                                    foreach (DataRow dr in dsData.Rows)
                                    {
                                        dtFromTB.Rows.Add(dr.ItemArray);
                                    }

                                    
                                }

                            };

                        }
                    }

                }

                foreach (DataRow dr in dtFromTB.Rows)
                {
                    dt_FinancialHeader.Rows.Add(dr.ItemArray);
                }

                if (dt_FinancialHeader != null)
                {


                    if (IsProfitAndLoss == true)
                    {
                        dtToReturn = dt_FinancialHeader.Copy();
                        dtToReturn.Columns.Add("ResultValue", typeof(decimal));
                        dtToReturn.Columns["ResultValue"].AllowDBNull = true;

                        foreach (DataRow drFinancial in dtToReturn.Rows)
                        {
                            if (drFinancial["FinancialID"].ToString() == "1031")
                            {
                            
                            }

                            if (drFinancial.Field<string>("ElementName").ToLower().StartsWith("%") && !drFinancial.Field<string>("ElementName").Equals("%NoText"))
                            {
                                switch (drFinancial.Field<string>("ElementName").ToLower())
                                {
                                    case "%period":

                                        if (reportsFilter == "'Balance Sheet'")
                                        {
                                            //drFinancial.SetField<string>("ElementName", "As at " + dateto.Value.ToString("MMMM") + " "
                                            //    + dateto.Value.Year);
                                            //drFinancial.SetField<string>("ElementName", "Between  " + datefrom.Value.ToString("dd-MM-yyyy") + " AND  "
                                            //   + dateto.Value.ToString("dd-MM-yyyy"));
                                            drFinancial.SetField<string>("ElementName", "As at " + dateto.Value.ToString("dd-MM-yyyy"));
                                        }

                                        else if (reportsFilter == "'Profit And Loss Statement'")
                                        {
                                            drFinancial.SetField<string>("ElementName", "For the year ended December "
                                                + dateto.Value.Year);
                                        }
                                        else
                                        {

                                            //drFinancial.SetField<string>("ElementName", "For the year ended " + dateto.Value.ToString("MMMM") + " "
                                            //        + dateto.Value.Year);
                                            drFinancial.SetField<string>("ElementName", "For the Period " + datefrom.Value.ToString("dd-MM-yyyy") + " To  "
                                               + dateto.Value.ToString("dd-MM-yyyy"));
                                            if (chk_DateFromBelowFiscal == true)
                                            {
                                                drFinancial.SetField<string>("ElementName", "As at " + dateto.Value.ToString("dd-MM-yyyy"));
                                            }
                                        }

                                        break;

                                    case "%colheadings":
                                        drFinancial.SetField<decimal?>("ResultValue", dateto.Value.Year);
                                        break;
                                }
                            }
                            else if (drFinancial.Field<string>("ElementType").Equals("D") && drFinancial["COACodeFrom"].ToString() != "" && drFinancial["COACodeTo"].ToString() != "" && drFinancial["Amount"].ToString() == "")
                            {

                                decimal resultValue = 0;

                                resultValue += GL_TransactionDetail.GetSum(companyID, periodID, chartOfAccountID, drFinancial.Field<string>("COACodeFrom"),
                                           drFinancial.Field<string>("COACodeTo"), userID, datefrom, dateto, (int?)null, DocumentGroupID, WithoutClosing,WithoutClosing_New,fiscalId,CCID);
                                //resultValue = Negation(drFinancial, resultValue);
                                drFinancial.SetField<decimal?>("ResultValue", resultValue);


                            }
                            //else if (drFinancial.Field<string>("ElementType").Equals("H") && drFinancial["COACodeFrom"].ToString() != "" && drFinancial["COACodeTo"].ToString() != "" && drFinancial["Amount"].ToString() == "")
                            //{

                            //    decimal resultValue = 0;

                            //    resultValue += GL_TransactionDetail.GetSumByHeadCode(companyID, periodID, chartOfAccountID, drFinancial.Field<string>("COACodeFrom"),
                            //               drFinancial.Field<string>("COACodeTo"), userID, dateto, (int?)Segment, DocumentGroupID
                            //               );

                            //    drFinancial.SetField<decimal?>("ResultValue", resultValue);


                            //}

                            else if (drFinancial.Field<string>("ElementType").Equals("T") && !string.IsNullOrEmpty(drFinancial.Field<string>("Formula")))
                            {

                                decimal resultValue = 0;

                                if (!string.IsNullOrEmpty(drFinancial.Field<string>("Formula")))
                                {
                                    string formula = drFinancial.Field<string>("Formula");

                                    while (formula.Contains("["))
                                    {
                                        int startingIndex = formula.IndexOf("[");
                                        int endingIndex = formula.IndexOf("]", startingIndex);
                                        string variable = formula.Substring(startingIndex + 1, (endingIndex - startingIndex - 1));
                                        try
                                        { formula = formula.Replace("[" + variable + "]", dic_Results[variable].ToString()); }
                                        catch { break; }

                                    }

                                    if (formula.Contains("/0"))
                                        resultValue = 0;
                                    else
                                        try
                                        { resultValue = Convert.ToDecimal(new DataTable().Compute(formula, null)); }
                                        catch { }

                                    //resultValue = Negation(drFinancial, resultValue);
                                    drFinancial.SetField<decimal?>("ResultValue", resultValue);
                                }

                            }
                            else if (drFinancial.Field<string>("ElementType").Equals("D"))
                            {
                                decimal resultValue = 0;

                                if (!string.IsNullOrEmpty(drFinancial.Field<string>("Formula")))
                                {
                                    string formula = drFinancial.Field<string>("Formula");

                                    while (formula.Contains("["))
                                    {
                                        int startingIndex = formula.IndexOf("[");
                                        int endingIndex = formula.IndexOf("]", startingIndex);
                                        string variable = formula.Substring(startingIndex + 1, (endingIndex - startingIndex - 1));
                                        try
                                        { formula = formula.Replace("[" + variable + "]", dic_Results[variable].ToString()); }
                                        catch
                                        {

                                            break;

                                            //   variable = variable.Replace("ES", "");



                                            //  resultValue += Convert.ToDecimal(dtToReturn.Compute("Sum(ResultValue)", "ResultValue is not null and ElementSequence=" + variable));



                                        }

                                    }

                                    if (formula.Contains("/0"))
                                        resultValue = 0;
                                    else
                                        try
                                        { resultValue = Convert.ToDecimal(new DataTable().Compute(formula, null)); }
                                        catch { }

                                    resultValue = Negation(drFinancial, resultValue);
                                    drFinancial.SetField<decimal?>("ResultValue", resultValue);
                                }
                                else if (!string.IsNullOrEmpty(drFinancial.Field<string>("NoteNo")))
                                {
                                    resultValue = GetResultRecursively(companyID, periodID, chartOfAccountID, drFinancial.Field<string>("NoteNo"), objConnection, userID,
                                         dateto);

                                    //resultValue = Negation(drFinancial, resultValue);
                                    drFinancial.SetField<decimal?>("ResultValue", resultValue);
                                }
                                else if (drFinancial.Field<bool>("FixedInput") == true)
                                {
                                    try
                                    {
                                        decimal detailAmount = 0;

                                        foreach (DataRow item in dt_FinancialDetail.Rows)
                                        {
                                            if (item.Field<bool>("FixedInput") == true && (drFinancial["FinancialID"].ToString() == item["FinancialHeaderID"].ToString()))
                                            {
                                                detailAmount = item["Amount"].ToString() != "" ? item.Field<decimal>("Amount") : 0;
                                            }
                                        }


                                        resultValue = drFinancial["Amount"].ToString() != "" ? drFinancial.Field<decimal>("Amount") + detailAmount : 0;
                                        drFinancial.SetField<decimal?>("ResultValue", resultValue);
                                    }
                                    catch { }

                                }
                                else
                                {
                                    IEnumerable<DataRow> dr_FinancialDetails = from dr_Detail in dt_FinancialDetail.AsEnumerable()
                                                                               where dr_Detail.Field<int>("FinancialHeaderID") == drFinancial.Field<int>("FinancialID")
                                                                               select dr_Detail;

                                    foreach (DataRow dr_FinancialDetail in dr_FinancialDetails)
                                    {
                                        resultValue += GL_TransactionDetail.GetSum(companyID, periodID, chartOfAccountID, dr_FinancialDetail.Field<string>("COACodeFrom"),
                                           dr_FinancialDetail.Field<string>("COACodeTo"), userID, datefrom, dateto, dr_FinancialDetail["Segment"].ToString() != "" ? dr_FinancialDetail.Field<int>("Segment") : (int?)null,null,null, WithoutClosing_New,fiscalId,CCID
                                           );
                                    }

                                    resultValue = Negation(drFinancial, resultValue);
                                    drFinancial.SetField<decimal?>("ResultValue", resultValue);
                                }

                                if (!dic_Results.ContainsKey("ES" + drFinancial.Field<short>("ElementSequence")))
                                    dic_Results.Add("ES" + drFinancial.Field<short>("ElementSequence"), resultValue);


                            }

                            if (drFinancial.Field<string>("ElementName").ToLower().Contains("net profit"))
                            {
                            }
                        }

                        try
                        {


                            foreach (DataRow drFinancial in dtToReturn.Rows)
                            {

                                if (drFinancial.Field<string>("ElementType").Equals("D") && !string.IsNullOrEmpty(drFinancial.Field<string>("Formula")))
                                {
                                    decimal resultValue = 0;

                                    if (!string.IsNullOrEmpty(drFinancial.Field<string>("Formula")))
                                    {


                                        #region MyRegion

                                        string formula = drFinancial.Field<string>("Formula");

                                        while (formula.Contains("["))
                                        {
                                            int startingIndex = formula.IndexOf("[");
                                            int endingIndex = formula.IndexOf("]", startingIndex);
                                            string variable = formula.Substring(startingIndex + 1, (endingIndex - startingIndex - 1));
                                            try
                                            { formula = formula.Replace("[" + variable + "]", dic_Results[variable].ToString()); }
                                            catch
                                            {
                                                break;
                                            }
                                        }

                                        if (formula.Contains("/0"))
                                            resultValue = 0;
                                        else
                                            try
                                            { resultValue = Convert.ToDecimal(new DataTable().Compute(formula, null)); }
                                            catch { }

                                        resultValue = Negation(drFinancial, resultValue);
                                        drFinancial.SetField<decimal?>("ResultValue", resultValue);


                                        #endregion


                                        //string formula = drFinancial.Field<string>("Formula");


                                        //while (formula.Contains("["))
                                        //{
                                        //    int startingIndex = formula.IndexOf("[");
                                        //    int endingIndex = formula.IndexOf("]", startingIndex);
                                        //    string variable = formula.Substring(startingIndex + 1, (endingIndex - startingIndex - 1));

                                        //    if (!dic_Results.ContainsKey(variable))
                                        //        dic_Results.Add(variable, resultValue);
                                        //    try
                                        //    {
                                        //        formula = formula.Replace("[" + variable + "]", dic_Results[variable].ToString());

                                        //        try
                                        //        {
                                        //            resultValue += Convert.ToDecimal(dtToReturn.Compute("Sum(ResultValue)", "ResultValue is not null and ElementSequence=" + variable.Replace("ES", "")));
                                        //            //  drFinancial.SetField<decimal?>("ResultValue", resultValue);
                                        //        }
                                        //        catch { resultValue += 0; }

                                        //    }
                                        //    catch
                                        //    {

                                        //        // break;

                                        //        variable = variable.Replace("ES", "");


                                        //        try
                                        //        {
                                        //            resultValue += Convert.ToDecimal(dtToReturn.Compute("Sum(ResultValue)", "ResultValue is not null and ElementSequence=" + variable));
                                        //        }
                                        //        catch { resultValue += 0; }
                                        //        break;

                                        //    }

                                        //}

                                        //if (formula.Contains("/0"))
                                        //    resultValue = 0;
                                        //else
                                        //    try
                                        //    { //resultValue = Convert.ToDecimal(new DataTable().Compute(formula, null));
                                        //    }
                                        //    catch { }

                                        ////resultValue = Negation(drFinancial, resultValue);
                                        //drFinancial.SetField<decimal?>("ResultValue", resultValue);
                                    }
                                }
                            }
                        }
                        catch { }



                    }  //parameter in 
                    else
                    {



                        dtToReturn = dt_FinancialHeader.Copy();
                        dtToReturn.Columns.Add("ResultValue", typeof(decimal));
                        dtToReturn.Columns["ResultValue"].AllowDBNull = true;

                        foreach (DataRow drFinancial in dtToReturn.Rows)
                            if (drFinancial.Field<string>("ElementName").ToLower().StartsWith("%") && !drFinancial.Field<string>("ElementName").Equals("%NoText"))
                            {
                                switch (drFinancial.Field<string>("ElementName").ToLower())
                                {
                                    case "%period":

                                        if (reportsFilter == "'Balance Sheet'")
                                        {
                                            drFinancial.SetField<string>("ElementName", "As at " + dateto.Value.ToString("MMMM") + " "
                                                + dateto.Value.Year);
                                        }

                                        else if (reportsFilter == "'Profit And Loss Statement'")
                                        {
                                            drFinancial.SetField<string>("ElementName", "For the year ended December "
                                                + dateto.Value.Year);
                                        }
                                        else
                                        {

                                            drFinancial.SetField<string>("ElementName", "For the year ended " + dateto.Value.ToString("MMMM") + " "
                                                    + dateto.Value.Year);
                                        }

                                        break;

                                    case "%colheadings":
                                        drFinancial.SetField<decimal?>("ResultValue", dateto.Value.Year);
                                        break;
                                }
                            }
                            else if (drFinancial.Field<string>("ElementType").Equals("T"))
                            {
                                decimal resultValue = 0;

                                if (!string.IsNullOrEmpty(drFinancial.Field<string>("Formula")))
                                {
                                    string formula = drFinancial.Field<string>("Formula");

                                    while (formula.Contains("["))
                                    {
                                        int startingIndex = formula.IndexOf("[");
                                        int endingIndex = formula.IndexOf("]", startingIndex);
                                        string variable = formula.Substring(startingIndex + 1, (endingIndex - startingIndex - 1));
                                        try
                                        { formula = formula.Replace("[" + variable + "]", dic_Results[variable].ToString()); }
                                        catch { break; }

                                    }

                                    if (formula.Contains("/0"))
                                        resultValue = 0;
                                    else
                                        try
                                        { resultValue = Convert.ToDecimal(new DataTable().Compute(formula, null)); }
                                        catch { }

                                    resultValue = Negation(drFinancial, resultValue);
                                    drFinancial.SetField<decimal?>("ResultValue", resultValue);
                                }
                            }
                            else if (drFinancial.Field<string>("ElementType").Equals("D"))
                            {
                                decimal resultValue = 0;

                                if (!string.IsNullOrEmpty(drFinancial.Field<string>("Formula")))
                                {
                                    string formula = drFinancial.Field<string>("Formula");

                                    while (formula.Contains("["))
                                    {
                                        int startingIndex = formula.IndexOf("[");
                                        int endingIndex = formula.IndexOf("]", startingIndex);
                                        string variable = formula.Substring(startingIndex + 1, (endingIndex - startingIndex - 1));
                                        try
                                        { formula = formula.Replace("[" + variable + "]", dic_Results[variable].ToString()); }
                                        catch { break; }

                                    }

                                    if (formula.Contains("/0"))
                                        resultValue = 0;
                                    else
                                        try
                                        { resultValue = Convert.ToDecimal(new DataTable().Compute(formula, null)); }
                                        catch { }

                                    resultValue = Negation(drFinancial, resultValue);
                                    drFinancial.SetField<decimal?>("ResultValue", resultValue);
                                }
                                else if (!string.IsNullOrEmpty(drFinancial.Field<string>("NoteNo")))
                                {
                                    resultValue = GetResultRecursively(companyID, periodID, chartOfAccountID, drFinancial.Field<string>("NoteNo"), objConnection, userID,
                                         dateto);

                                    resultValue = Negation(drFinancial, resultValue);
                                    drFinancial.SetField<decimal?>("ResultValue", resultValue);
                                }
                                else if (drFinancial.Field<bool>("FixedInput") == true)
                                {
                                    try
                                    {
                                        decimal detailAmount = 0;

                                        foreach (DataRow item in dt_FinancialDetail.Rows)
                                        {
                                            if (item.Field<bool>("FixedInput") == true && (drFinancial["FinancialID"].ToString() == item["FinancialHeaderID"].ToString()))
                                            {
                                                detailAmount = item["Amount"].ToString() != "" ? item.Field<decimal>("Amount") : 0;
                                            }
                                        }


                                        resultValue = drFinancial["Amount"].ToString() != "" ? drFinancial.Field<decimal>("Amount") + detailAmount : 0;
                                        drFinancial.SetField<decimal?>("ResultValue", resultValue);
                                    }
                                    catch { }

                                }
                                else
                                {
                                    IEnumerable<DataRow> dr_FinancialDetails = from dr_Detail in dt_FinancialDetail.AsEnumerable()
                                                                               where dr_Detail.Field<int>("FinancialHeaderID") == drFinancial.Field<int>("FinancialID")
                                                                               select dr_Detail;



                                    foreach (DataRow dr_FinancialDetail in dr_FinancialDetails)
                                    {
                                        resultValue += GL_TransactionDetail.GetSum(companyID, periodID, chartOfAccountID, dr_FinancialDetail.Field<string>("COACodeFrom"),
                                           dr_FinancialDetail.Field<string>("COACodeTo"), userID, dateto
                                           ,null,null,null,null,WithoutClosing_New,fiscalId,CCID
                                           );
                                    }

                                    resultValue = Negation(drFinancial, resultValue);
                                    drFinancial.SetField<decimal?>("ResultValue", resultValue);
                                }

                                if (!dic_Results.ContainsKey("ES" + drFinancial.Field<short>("ElementSequence")))
                                    dic_Results.Add("ES" + drFinancial.Field<short>("ElementSequence"), resultValue);
                            }

                    }//parameter else


                    //full total 

                    try
                    {


                        foreach (DataRow drFinancial in dtToReturn.Rows)
                        {
                            //if(drFinancial.Field<string>("ElementType").ToString().ToLower().Contains
                            if (drFinancial.Field<string>("ElementName").ToLower().Contains("net profit"))
                            {
                            }


                            if (drFinancial.Field<string>("ElementType").Equals("L") && !string.IsNullOrEmpty(drFinancial.Field<string>("Formula")))
                            {
                                decimal resultValue = 0;

                                if (!string.IsNullOrEmpty(drFinancial.Field<string>("Formula")))
                                {
                                    string formula = drFinancial.Field<string>("Formula");


                                    while (formula.Contains("["))
                                    {
                                        int startingIndex = formula.IndexOf("[");
                                        int endingIndex = formula.IndexOf("]", startingIndex);
                                        string variable = formula.Substring(startingIndex + 1, (endingIndex - startingIndex - 1));

                                        string operat = "";
                                        try
                                        { operat = formula.Substring(startingIndex - 1, 0 + 1); }
                                        catch { }

                                        if (!dic_Results.ContainsKey(variable))
                                            dic_Results.Add(variable, resultValue);
                                        try
                                        {
                                            formula = formula.Replace("[" + variable + "]", dic_Results[variable].ToString());

                                            try
                                            {
                                                if (operat == "-")
                                                    resultValue -= Convert.ToDecimal(dtToReturn.Compute("Sum(ResultValue)", "ResultValue is not null and ElementSequence=" + variable.Replace("ES", "")));
                                                else
                                                    resultValue += Convert.ToDecimal(dtToReturn.Compute("Sum(ResultValue)", "ResultValue is not null and ElementSequence=" + variable.Replace("ES", "")));
                                                //  drFinancial.SetField<decimal?>("ResultValue", resultValue);
                                            }
                                            catch { resultValue += 0; }

                                        }
                                        catch
                                        {

                                            // break;

                                            variable = variable.Replace("ES", "");


                                            try
                                            {
                                                if (operat == "-")
                                                    resultValue -= Convert.ToDecimal(dtToReturn.Compute("Sum(ResultValue)", "ResultValue is not null and ElementSequence=" + variable));
                                                else
                                                    resultValue += Convert.ToDecimal(dtToReturn.Compute("Sum(ResultValue)", "ResultValue is not null and ElementSequence=" + variable));

                                            }
                                            catch { resultValue += 0; }
                                            break;

                                        }

                                    }

                                    if (formula.Contains("/0"))
                                        resultValue = 0;
                                    else
                                        try
                                        { //resultValue = Convert.ToDecimal(new DataTable().Compute(formula, null));
                                        }
                                        catch { }

                                    //resultValue = Negation(drFinancial, resultValue);
                                    drFinancial.SetField<decimal?>("ResultValue", resultValue);
                                }
                            }
                        }
                    }
                    catch { }


                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            finally
            {
                ConnectionHelper.CloseConnection(objConnection);
            }

            return dtToReturn;
        }


        private static decimal GetResultRecursively(string companyID, int periodID, int chartOfAccountID, string noteNo, SqlConnection objConnection, Guid userID,
            Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null)
        {
            decimal resultValue = 0;
            Dictionary<string, decimal> dic_Results = new Dictionary<string, decimal>();
            DataTable dt_FinancialHeader = Set_FinancialHeader.Get_Data_By_NoteNo(noteNo, true, objConnection);
            DataTable dt_FinancialDetail = Set_FinancialDetail.Get_Data_By_NoteNo(noteNo, objConnection);

            if (dt_FinancialHeader != null)
            {
                DataTable dtForProcessing = dt_FinancialHeader.Copy();
                dtForProcessing.Columns.Add("ResultValue", typeof(decimal));
                dtForProcessing.Columns["ResultValue"].AllowDBNull = true;

                foreach (DataRow drFinancial in dtForProcessing.Rows)
                {
                    if ((!drFinancial.Field<string>("ElementName").ToLower().StartsWith("%") || drFinancial.Field<string>("ElementName").ToLower().Equals("%notext"))
                        && drFinancial.Field<string>("ElementType").Equals("D"))
                    {
                        resultValue = 0;

                        if (!string.IsNullOrEmpty(drFinancial.Field<string>("Formula")))
                        {
                            string formula = drFinancial.Field<string>("Formula");

                            while (formula.Contains("["))
                            {
                                int startingIndex = formula.IndexOf("[");
                                int endingIndex = formula.IndexOf("]", startingIndex);
                                string variable = formula.Substring(startingIndex + 1, (endingIndex - startingIndex - 1));
                                try { formula = formula.Replace("[" + variable + "]", dic_Results[variable].ToString()); }
                                catch { break; }
                            }

                            try { resultValue = Convert.ToDecimal(new DataTable().Compute(formula, null)); }
                            catch { }
                            resultValue = Negation(drFinancial, resultValue);
                            drFinancial.SetField<decimal?>("ResultValue", resultValue);
                        }
                        else if (!string.IsNullOrEmpty(drFinancial.Field<string>("NoteNo")))
                        {
                            resultValue = GetResultRecursively(companyID, periodID, chartOfAccountID, drFinancial.Field<string>("NoteNo"), objConnection, userID,
                                fromDate, toDate);

                            resultValue = Negation(drFinancial, resultValue);
                            drFinancial.SetField<decimal?>("ResultValue", resultValue);
                        }
                        else
                        {
                            IEnumerable<DataRow> dr_FinancialDetails = from dr_Detail in dt_FinancialDetail.AsEnumerable()
                                                                       where dr_Detail.Field<int>("FinancialHeaderID") == drFinancial.Field<int>("FinancialID")
                                                                       select dr_Detail;

                            foreach (DataRow dr_FinancialDetail in dr_FinancialDetails)
                            {
                                resultValue += GL_TransactionDetail.GetSum(companyID, periodID, chartOfAccountID, dr_FinancialDetail.Field<string>("COACodeFrom"),
                                   dr_FinancialDetail.Field<string>("COACodeTo"), userID, toDate);
                            }

                            resultValue = Negation(drFinancial, resultValue);
                            drFinancial.SetField<decimal?>("ResultValue", resultValue);
                        }

                        if (!dic_Results.ContainsKey("ES" + drFinancial.Field<short>("ElementSequence")))
                            dic_Results.Add("ES" + drFinancial.Field<short>("ElementSequence"), resultValue);
                    }
                }
            }

            return resultValue;
        }

        private static decimal Negation(DataRow dr_FinancialHeader, decimal resultValue)
        {
            if (dr_FinancialHeader.Field<bool>("DataIn"))   //  Data in Negative
                resultValue = Math.Abs(resultValue) * -1;
            else
            {
                //  Expected Result     True (Debit)        False (Credit)
                //  Original Result     Debit   Credit      Debit   Credit
                //  Final Result        +ve     -ve         -ve     +ve
                if (!dr_FinancialHeader.Field<bool?>("ExpectedResultInDb").HasValue || !dr_FinancialHeader.Field<bool?>("ExpectedResultInDb").Value)
                    resultValue *= -1;
            }

            return resultValue;
        }

        public static DataTable Get_AllDocumentNos(int companyID, int periodID, int documentTypeID, int CurrentFiscalID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                string Query_Get_AllDocumentNos = @" SELECT GL_TransactionHeader.* FROM GL_TransactionHeader 
inner join Set_PeriodCustom on GL_TransactionHeader.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID
WHERE GL_TransactionHeader.CompanyID = @CompanyID and GL_TransactionHeader.PeriodID = @PeriodID 
and GL_TransactionHeader.DocumentTypeID = @DocumentTypeID And GL_TransactionHeader.Status = 1 and GL_TransactionHeader.StatusCode = 1 
AND IsNull(GL_TransactionHeader.IsReversed, 0) = 0 AND GL_TransactionHeader.ReversalID IS NULL 
ORDER BY GL_TransactionHeader.DocumentNo ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_AllDocumentNos, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return dtToReturn;
        }

        public static bool ReverseDocument(DateTime dt_ReverseDate, Guid reversedBy, GL_TransactionHeader objGLHeader, List<GL_TransactionDetail> lst_GLDetails, SET_DocumentApprove obj_Approval, out int ReversalDocumentNo, string Comments, int CurrentFiscalID)
        {
            bool result = false;
            SqlTransaction objSQLTransaction = SqlHelper.GetTransaction("ReverseDocument");

            try
            {
                #region Update old General Ledger Header

                int newDocumentNo = SET_DocumentSequence.ManualDocumentSequence(objGLHeader.DocumentTypeID, objGLHeader.CompanyID, objGLHeader.PeriodID,
                    objSQLTransaction, CurrentFiscalID);


                if (objGLHeader.Narration == null)
                    objGLHeader.Narration = "";


                string originalNarration = objGLHeader.Narration;
                objGLHeader.Narration = objGLHeader.Narration + (objGLHeader.Narration.Trim().EndsWith(".") ? " " : ". ")
                    + "This document is reversed (Reversed document no.: " + newDocumentNo.ToString() + ").";

                objGLHeader.UpdateDataByKey(objSQLTransaction);

                #endregion

                #region Insert new General Ledger Header

                objGLHeader.IsReversed = null;

                if (string.IsNullOrEmpty(objGLHeader.Narration))
                    objGLHeader.Narration = Comments + " (This document is generated against the Reversal process. Previous Document No.: " + objGLHeader.DocumentNo + ").";
                else
                {
                    objGLHeader.Narration += Comments + (originalNarration.Trim().EndsWith(".") ? " " : ". ")
                        + "(This document is generated against the Reversal process. Previous Document No.: " + objGLHeader.DocumentNo + ").";
                }

                objGLHeader.DocumentDate = dt_ReverseDate;
                objGLHeader.ReversalDate = null;
                objGLHeader.ReversedBy = null;
                objGLHeader.ReversalID = objGLHeader.TransactionID;
                objGLHeader.TransactionID = GL_TransactionHeader.GetNewTransactionID(objGLHeader.CompanyID, objGLHeader.PeriodID, objSQLTransaction);
                objGLHeader.DocumentNo = newDocumentNo;

                ReversalDocumentNo = int.Parse(objGLHeader.DocumentNo.ToString());

                objGLHeader.Insert(objSQLTransaction);

                #endregion

                #region Insert new General Ledger Detail

                decimal AmountDebit = Math.Round(lst_GLDetails.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                decimal AmountCredit = Math.Round(lst_GLDetails.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                if (AmountDebit != AmountCredit)
                    throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");


                foreach (GL_TransactionDetail objGLDetail in lst_GLDetails)
                {
                    objGLDetail.TransactionID = objGLHeader.TransactionID;
                    objGLDetail.SourceHeaderID = objGLHeader.ReversalID;
                    objGLDetail.Insert(objSQLTransaction);
                }

                #endregion

                #region Insert new Document Approve

                obj_Approval.Status = true;
                obj_Approval.DocumentNo = objGLHeader.DocumentNo.ToString();
                obj_Approval.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(objGLHeader.CompanyID, objGLHeader.PeriodID, objSQLTransaction);
                obj_Approval.Insert(objSQLTransaction);

                #endregion

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
                    SqlHelper.CommitTransaction(objSQLTransaction);
                else
                    SqlHelper.RollBackTransaction(objSQLTransaction, "ReverseDocument");
            }

            return result;
        }

        public static DataTable CheckDataByKeyForNarration(int companyID, int periodID, int documentTypeID, int documentNo)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[3] = new SqlParameter("@DocumentNo", documentNo);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_CheckDataByKeyForNarration, sqlparams);

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

        public static DataTable GET_CaneReconciliation_Report_Data(int companyID, int periodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            string additionalFilter = string.Empty;
            DataTable dtToReturn = null;

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    additionalFilter = " AND GL_TransactionHeader.DocumentDate BETWEEN @FromDate AND @ToDate ";
                    sqlParams.Add(new SqlParameter("@FromDate", DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00"));
                    sqlParams.Add(new SqlParameter("@ToDate", DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59"));
                }
                else if (dateFrom.HasValue)
                {
                    additionalFilter = " AND GL_TransactionHeader.DocumentDate >= @FromDate ";
                    sqlParams.Add(new SqlParameter("@FromDate", DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00"));
                }
                else
                {
                    additionalFilter = " AND GL_TransactionHeader.DocumentDate <= @ToDate ";
                    sqlParams.Add(new SqlParameter("@ToDate", DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59"));
                }


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.GL.GL_TransactionHeader.Query_CaneReconciliationReport, additionalFilter), sqlParams.ToArray());

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

        public static DataTable Get_AccountsTrial_ReportData(string companyID, int periodID, string accountCode, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, short? documentGroupID = null)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            StringBuilder additionalFilter = new StringBuilder();
            DataTable dtToReturn = null;

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@CodeCombinationID", accountCode));
                sqlParams.Add(new SqlParameter("@FromDate", DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00"));
                sqlParams.Add(new SqlParameter("@ToDate", DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59"));

                if (accountCode != "")
                    additionalFilter.Append(" AND GL_TransactionDetail.CodeCombinationID = @CodeCombinationID ");

                if (documentGroupID.HasValue)
                    additionalFilter.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString());

                #region Query

                string Query_NewAccountsTrial_Report = @"  select * from 
(
    SELECT FINTAB.DocumentType, Fintab.CreateDate, FINTAB.Name as CustomerName, FinTab.SourceID, 
        case when SUM(FINTAB.Amount) >= 0 then SUM(FINTAB.Amount) else 0 end AS Debit, 
        case when SUM(FINTAB.Amount) < 0 then abs(SUM(FINTAB.Amount)) else 0 end AS Credit, FINTAB.InstrumentNo, FINTAB.Comments as Narration,  FINTAB.CostCenterName, 
        FINTAB.InvoiceNumbers, FINTAB.InvoiceDocumentNo, FINTAB.DocumentNo, FINTAB.DocumentDate, FINTAB.FreightAmount, FINTAB.ForAlhajAmount 
    FROM 
    (
	   Select 'Opening' as DocumentType, @FromDate AS CreateDate, openingTAB.Name, -1 AS SourceID, sum(openingTAB.Amount) as Amount, '' as InstrumentNo, 
            '' as Comments,'' as CostCenterName, '' as InvoiceNumbers, '' as InvoiceDocumentNo, '' as DocumentNo, @FromDate as DocumentDate, 0 AS FreightAmount, 0 As ForAlhajAmount 
	    From 
	    (        
            SELECT 0 as Amount,  '' AS Name, 0 as SourceID FROM SET_DocumentType 
                INNER JOIN AR_ReceiptTransactionHeader ON SET_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                    AND SET_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
                    AND SET_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID AND AR_ReceiptTransactionHeader.Status = 1 
                    AND AR_ReceiptTransactionHeader.StatusCode = 1 
                INNER JOIN AR_ReceiptCustomerDetail on AR_ReceiptCustomerDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID 
                    and AR_ReceiptCustomerDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                    and AR_ReceiptCustomerDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
                Inner Join GL_TransactionDetail On GL_TransactionDetail.CompanyID = AR_ReceiptCustomerDetail.CompanyID 
				    And GL_TransactionDetail.PeriodID = AR_ReceiptCustomerDetail.PeriodID 
				    and GL_TransactionDetail.CodeCombinationID = AR_ReceiptCustomerDetail.CustomerAccountCode And GL_TransactionDetail.Status = 1 
                INNER JOIN AR_SET_Customer ON AR_ReceiptCustomerDetail.CustomerID = AR_SET_Customer.CustomerID 
                     AND AR_ReceiptCustomerDetail.PeriodID = AR_SET_Customer.PeriodID 
                    AND AR_SET_Customer.Status = 1 
                INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
                     AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
                    AND AR_SET_BusinessPartnerTypes.TypeCode = AR_ReceiptCustomerDetail.BusinessPartnerTypeID 
                INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
                LEFT Join AR_ReceiptAccountsDetail on AR_ReceiptAccountsDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                    AND AR_ReceiptAccountsDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
                    AND AR_ReceiptAccountsDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID	
                 Left JOIN SET_CostCenter ON SET_CostCenter.CostCenterID=GL_TransactionDetail.CostCenterID
                    AND SET_CostCenter.Status=1
            WHERE AR_SET_Customer.PeriodID = @PeriodID AND AR_ReceiptTransactionHeader.DocumentDate < @FromDate 
                And AR_ReceiptTransactionHeader.ReversalComments is null 
                {0}
            GROUP BY AR_SET_Customer.CustomerName, AR_ReceiptCustomerDetail.TransactionType, AR_ReceiptCustomerDetail.ReceiptCustomerID
        ) OpeningTAB 
        GROUP BY OpeningTAB.Name 

        UNION ALL 

        select distinct TABInvoice.DocumentType, TABInvoice.CreateDate, TABInvoice.BusinessPartnerName, TABInvoice.SourceID, TABInvoice.Amount, 
            TABInvoice.InstrumentNo, TABInvoice.Comments + convert(varchar(100), isnull(TABInvoice.InvoiceSupplierID, '')),TABInvoice.CostCenterName, TABInvoice.InvoiceNumbers, 
            TABInvoice.InvoiceDocumentNo, TABInvoice.DocumentNo, TABInvoice.DocumentDate, TABInvoice.FreightAmount, TABInvoice.ForAlhajAmount 
        from 
        (
            ---------- Vendor Invoice ----------
            SELECT distinct AR_InvoiceSupplierDetail.InvoiceSupplierID, AR_InvoiceHeader.companyID, Set_DocumentType.TransactionType, 
                case when AR_SET_BusinessPartnerTypes.TypeCode is null then 
                    (
                        select top 1 CUS.TypeCode from AR_SET_BusinessPartnerTypes CUS 
                        where AR_InvoiceSupplierDetail.SupplierID = CUS.BusinessPartnerID AND AR_InvoiceSupplierDetail.CompanyID = CUS.CompanyID 
                            AND AR_InvoiceSupplierDetail.PeriodID = CUS.PeriodID
		            ) 
                    else AR_SET_BusinessPartnerTypes.TypeCode end as TypeCode, 
                case when AR_InvoiceHeader.vendorID is null then 
                    (
                        select CUS.CustomerID from AR_SET_Customer CUS 
                        where AR_InvoiceSupplierDetail.SupplierID = CUS.CustomerID --AND AR_InvoiceSupplierDetail.CompanyID = CUS.CompanyID 
                            AND AR_InvoiceSupplierDetail.PeriodID = CUS.PeriodID
		            ) 
                    else 
                    (
                        select CUS.CustomerID from AR_SET_Customer CUS 
                        where AR_InvoiceHeader.vendorID = CUS.CustomerID AND AR_InvoiceHeader.PeriodID = CUS.PeriodID
--AND AR_InvoiceHeader.CompanyID = CUS.CompanyID 
                    ) end as CustomerID, 
                SET_DocumentType.DocumentType, AR_InvoiceHeader.CreateDate, case when AR_InvoiceHeader.vendorID is null then 
                    (
                        select CUS.CustomerName from AR_SET_Customer CUS 
                        where AR_InvoiceSupplierDetail.SupplierID = CUS.CustomerID --AND AR_InvoiceSupplierDetail.CompanyID = CUS.CompanyID 
                            AND AR_InvoiceSupplierDetail.PeriodID = CUS.PeriodID
                    ) else 
                    (
                        select CUS.CustomerName from AR_SET_Customer CUS 
                        where AR_InvoiceHeader.vendorID = CUS.CustomerID AND AR_InvoiceHeader.PeriodID = CUS.PeriodID
--AND AR_InvoiceHeader.CompanyID = CUS.CompanyID 
                    ) end as BusinessPartnerName, 
                AR_InvoiceSupplierDetail.InvoiceSupplierID as SourceID, case when AR_InvoiceHeader.vendorID is null then 
                    (Case when AR_InvoiceSupplierDetail.TransactionType = 'Debit' then AR_InvoiceSupplierDetail.Amount else AR_InvoiceSupplierDetail.Amount * -1 end) 
                    else 
                    (
                        Case when AR_InvoiceSupplierDetail.TransactionType = 'Debit' then AR_InvoiceHeader.AmountPaid 
                            when SET_DocumentType.DocumentType = 'Debit Note' then AR_InvoiceHeader.AmountPaid 
                            else 
                                case GL_TransactionDetail.TransactionType when 'Debit' then GL_TransactionDetail.Amount else GL_TransactionDetail.Amount  * -1 end 
                        end
                    ) end as Amount, 
                '0' AS InstrumentNo, AR_InvoiceHeader.Description AS Comments,SET_CostCenter.CostCenterName ,'' as InvoiceNumbers, '' as InvoiceDocumentNo, 
                ISNULL(AR_InvoiceHeader.InvoiceNumber, 0) AS DocumentNo, AR_InvoiceHeader.InvoiceDate AS DocumentDate, 0 AS FreightAmount, 0 As ForAlhajAmount, 
                GL_TransactionDetail.serialNo 
            FROM SET_DocumentType 
                INNER JOIN AR_InvoiceHeader ON SET_DocumentType.CompanyID = AR_InvoiceHeader.CompanyID AND SET_DocumentType.PeriodID = AR_InvoiceHeader.PeriodID 
                    AND SET_DocumentType.DocumentTypeID = AR_InvoiceHeader.InvoiceType AND AR_InvoiceHeader.Status = 1 AND AR_InvoiceHeader.StatusCode = 1 
                inner join GL_TransactionHeader on GL_TransactionHeader.DocumentTypeID = AR_InvoiceHeader.InvoiceType 
				    And GL_TransactionHeader.DocumentNo = AR_InvoiceHeader.InvoiceNumber 
				Inner Join GL_TransactionDetail On GL_TransactionDetail.CompanyID = GL_TransactionHeader.CompanyID 
				    And GL_TransactionDetail.PeriodID = GL_TransactionHeader.PeriodID And GL_TransactionDetail.TransactionID = GL_TransactionHeader.TransactionID 
                    And GL_TransactionDetail.Status = 1 
	            LEFT JOIN AR_InvoiceDetail on AR_InvoiceDetail.CompanyID = AR_InvoiceHeader.CompanyID and AR_InvoiceDetail.PeriodID = AR_InvoiceHeader.PeriodID 
                    and AR_InvoiceDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID 
                LEFT JOIN AR_InvoiceSupplierDetail on AR_InvoiceSupplierDetail.CompanyID = AR_InvoiceHeader.CompanyID 
                    and AR_InvoiceSupplierDetail.PeriodID = AR_InvoiceHeader.PeriodID and AR_InvoiceSupplierDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID 
                    and AR_InvoiceSupplierDetail.SupplierAccountCode = GL_TransactionDetail.CodeCombinationID 
	            LEFT JOIN AR_SET_Customer ASCUS ON AR_InvoiceSupplierDetail.SupplierID = ASCUS.CustomerID 
--AND AR_InvoiceSupplierDetail.CompanyID = ASCUS.CompanyID 
                    AND AR_InvoiceSupplierDetail.PeriodID = ASCUS.PeriodID AND ASCUS.Status = 1 
                Left JOIN SET_CostCenter ON SET_CostCenter.CostCenterID=GL_TransactionDetail.CostCenterID
                    AND SET_CostCenter.Status=1
	            left JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_InvoiceHeader.vENDORid 
                    AND AR_SET_BusinessPartnerTypes.CompanyID = AR_InvoiceHeader.CompanyID AND AR_SET_BusinessPartnerTypes.PeriodID = AR_InvoiceHeader.PeriodID 
	            LEFT JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
	        WHERE AR_InvoiceHeader.CompanyID in @CompanyId AND AR_InvoiceHeader.PeriodID = @PeriodId AND AR_InvoiceHeader.InvoiceDate BETWEEN @FromDate AND @ToDate 
                And IsNull(AR_InvoiceHeader.IsReversed, 0) = 0 And AR_InvoiceHeader.ReversalID is null 
                {0}
        ) TABInvoice 
        where TABInvoice.companyID in @CompanyId 
        
        UNION ALL 
            ---------- Payment ----------
        SELECT distinct SET_DocumentType.DocumentType, AP_PaymentHeader.CreateDate, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            GL_TransactionDetail.SerialNo as SourceID, 
            (Case GL_TransactionDetail.TransactionType when 'Debit' then GL_TransactionDetail.Amount else GL_TransactionDetail.Amount * -1 end) as Amount, 
            '' AS InstrumentNo, 
            IsNull(AP_PaymentHeader.Comments, '') + (Case When AP_PaymentHeader.ReversalComments IS NULL Then '' Else ' ' + AP_PaymentHeader.ReversalComments End) 
                AS Comments,SET_CostCenter.CostCenterName, 
            '' as InvoiceNumbers, 0 as InvoiceDocumentNo, ISNULL(AP_PaymentHeader.DocumentNo, 0) AS DocumentNo, AP_PaymentHeader.DocumentDate AS DocumentDate, 
            0 AS FreightAmount, 0 As ForAlhajAmount 
        FROM SET_DocumentType 
            INNER JOIN AP_PaymentHeader ON SET_DocumentType.CompanyID = AP_PaymentHeader.CompanyID AND SET_DocumentType.PeriodID = AP_PaymentHeader.PeriodID 
	            AND SET_DocumentType.DocumentTypeID = AP_PaymentHeader.DocumentTypeID AND AP_PaymentHeader.Status = 1 AND AP_PaymentHeader.StatusCode = 1 
            inner join GL_TransactionHeader on GL_TransactionHeader.DocumentTypeID = AP_PaymentHeader.DocumentTypeID 
				And GL_TransactionHeader.DocumentNo = AP_PaymentHeader.DocumentNo 
            Inner Join GL_TransactionDetail On GL_TransactionDetail.CompanyID = GL_TransactionHeader.CompanyID 
				And GL_TransactionDetail.PeriodID = GL_TransactionHeader.PeriodID And GL_TransactionDetail.TransactionID = GL_TransactionHeader.TransactionID 
                And GL_TransactionDetail.Status = 1 
            INNER Join AP_PaymentSupplierDetail on AP_PaymentSupplierDetail.PaymentHeaderID = AP_PaymentHeader.PaymentID 
	            and AP_PaymentSupplierDetail.CompanyID = AP_PaymentHeader.CompanyID and AP_PaymentSupplierDetail.PeriodID = AP_PaymentHeader.PeriodID 
                and AP_PaymentSupplierDetail.SupplierAccountCode = GL_TransactionDetail.CodeCombinationID 
                and (AP_PaymentSupplierDetail.PaymentSupplierID = GL_TransactionDetail.SourceDetailID 
                    Or (GL_TransactionDetail.SourceDetailID = -1 And AP_PaymentSupplierDetail.Amount = GL_TransactionDetail.Amount)) 
	        left join AP_PaymentBanksDetail on AP_PaymentBanksDetail.PaymentHeaderID = AP_PaymentHeader.PaymentID 
                and AP_PaymentBanksDetail.CompanyID = AP_PaymentHeader.CompanyID and AP_PaymentBanksDetail.PeriodID = AP_PaymentHeader.PeriodID 
	        left join ar_set_bankAccount on ar_set_bankAccount.BankAccountID = AP_PaymentBanksDetail.BankAccountID 
            INNER JOIN AR_SET_Customer ON AP_PaymentSupplierDetail.SupplierID = AR_SET_Customer.CustomerID 
--                AND AP_PaymentSupplierDetail.CompanyID = AR_SET_Customer.CompanyID
AND AP_PaymentSupplierDetail.PeriodID = AR_SET_Customer.PeriodID 
                AND AR_SET_Customer.Status = 1 
            left join AP_Payment_AccountsDetail on AP_Payment_AccountsDetail.PaymentHeaderID = AP_PaymentHeader.PaymentID and AP_Payment_AccountsDetail.IsBP <> 0 
                and AP_Payment_AccountsDetail.TransactionType = 'Credit' 
             Left JOIN SET_CostCenter ON SET_CostCenter.CostCenterID=GL_TransactionDetail.CostCenterID
                    AND SET_CostCenter.Status=1
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
		        AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
        WHERE AP_PaymentHeader.CompanyID in @CompanyId AND AP_PaymentHeader.PeriodID = @PeriodId AND AP_PaymentHeader.DocumentDate BETWEEN @FromDate AND @ToDate 
              {0}
    UNION ALL

    -- PAYMENT -- SINGLE BUSINESS PARTNER CASE
    SELECT SET_DocumentType.DocumentType, AP_PaymentHeader.CreateDate, AR_SET_Customer.CustomerName AS BusinessPartnerName, AP_PaymentHeader.PaymentID as SourceID, 
        (Case GL_TransactionDetail.TransactionType when 'Debit' then GL_TransactionDetail.Amount else GL_TransactionDetail.Amount * -1 end) as Amount, 
        AP_PaymentHeader.PaymentServerOrderNo AS InstrumentNo, 
        AP_PaymentHeader.Comments + (Case When AP_PaymentHeader.ReversalComments IS NULL Then '' Else ' ' + AP_PaymentHeader.ReversalComments End) AS Comments,
        SET_CostCenter.CostCenterName, 
        case when AP_PaymentHeader.POHeaderID is null then (ISNULL(
            (
                SELECT STUFF(
                (
                    SELECT distinct  AR_InvoiceHeader.InvoiceNumber FROM AP_PaymentHeader childPayment 
                        LEFT JOIN AP_PaymentInvoicesDetail ON AP_PaymentInvoicesDetail.PaymentHeaderID = childPayment.PaymentID 
                            AND AP_PaymentInvoicesDetail.CompanyID = childPayment.CompanyID AND AP_PaymentInvoicesDetail.PeriodID = childPayment.PeriodID 
                        LEFT JOIN AR_InvoiceHeader ON AR_InvoiceHeader.InvoiceID = AP_PaymentInvoicesDetail.InvoiceHeaderID 
                            AND AR_InvoiceHeader.CompanyID = AP_PaymentInvoicesDetail.CompanyID AND AR_InvoiceHeader.PeriodID = AP_PaymentInvoicesDetail.PeriodID 
                        LEFT JOIN AR_InvoiceDetail ON AR_InvoiceDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID 
                            AND AR_InvoiceDetail.CompanyID = AR_InvoiceHeader.CompanyID AND AR_InvoiceDetail.PeriodID = AR_InvoiceHeader.PeriodID 
                    where childPayment.PaymentID = payment.PaymentID and childPayment.CompanyID = payment.CompanyID and childPayment.PeriodID = payment.PeriodID 
                    ORDER BY AR_InvoiceHeader.InvoiceNumber FOR XML PATH(''), TYPE
                ).value('.', 'varchar(max)'), 1, 0, '') AS InvoiceNumbers
                FROM AP_PaymentHeader payment 
                where payment.PaymentID = AP_PaymentHeader.PaymentID and payment.CompanyID = AP_PaymentHeader.CompanyID 
                    and payment.PeriodID = AP_PaymentHeader.PeriodID
            ), '')) else 
            (
                Select DocumentNumber from AP_PurchaseOrderHeader 
                where AP_PurchaseOrderHeader.POHeaderID = AP_PaymentHeader.POHeaderID and AP_PurchaseOrderHeader.CompanyID = AP_PaymentHeader.CompanyID 
                    and AP_PurchaseOrderHeader.PeriodID = AP_PaymentHeader.PeriodID
			) end as InvoiceNumbers, 
            ISNULL(
            (
                SELECT STUFF(
		        (
                    SELECT ', ' + AP_PaymentDetail.InvoiceDocumentNo FROM AP_PaymentDetail WHERE payment1.PaymentID = AP_PaymentDetail.PaymentHeaderID 
                    ORDER BY AP_PaymentDetail.InvoiceDocumentNo FOR XML PATH(''), TYPE
                ).value('.', 'varchar(max)'), 1, 2, '') AS InvoiceDocumentNo 
                FROM AP_PaymentHeader payment1 
                where payment1.PaymentID = AP_PaymentHeader.PaymentID and payment1.CompanyID = AP_PaymentHeader.CompanyID 
                    and payment1.PeriodID = AP_PaymentHeader.PeriodID
            ), '') as InvoiceDocumentNo, 
            ISNULL(AP_PaymentHeader.DocumentNo, 0) AS DocumentNo, AP_PaymentHeader.DocumentDate AS DocumentDate, 0 AS FreightAmount, 0 As ForAlhajAmount 
        FROM SET_DocumentType 
            INNER JOIN AP_PaymentHeader ON SET_DocumentType.CompanyID = AP_PaymentHeader.CompanyID AND SET_DocumentType.PeriodID = AP_PaymentHeader.PeriodID 
                AND SET_DocumentType.DocumentTypeID = AP_PaymentHeader.DocumentTypeID AND AP_PaymentHeader.Status = 1 AND AP_PaymentHeader.StatusCode = 1 
            inner join GL_TransactionHeader on GL_TransactionHeader.DocumentTypeID = AP_PaymentHeader.DocumentTypeID 
				And GL_TransactionHeader.DocumentNo = AP_PaymentHeader.DocumentNo 
            Inner Join GL_TransactionDetail On GL_TransactionDetail.CompanyID = GL_TransactionHeader.CompanyID 
				And GL_TransactionDetail.PeriodID = GL_TransactionHeader.PeriodID And GL_TransactionDetail.TransactionID = GL_TransactionHeader.TransactionID 
                And GL_TransactionDetail.Status = 1 
            INNER JOIN AR_SET_Customer ON AP_PaymentHeader.VendorID = AR_SET_Customer.CustomerID --AND AP_PaymentHeader.CompanyID = AR_SET_Customer.CompanyID 
                AND AP_PaymentHeader.PeriodID = AR_SET_Customer.PeriodID AND AR_SET_Customer.Status = 1 
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
                AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
            INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
            left join AP_Payment_AccountsDetail on AP_Payment_AccountsDetail.PaymentHeaderID = AP_PaymentHeader.PaymentID and AP_Payment_AccountsDetail.IsBP <> 0 
                and AP_Payment_AccountsDetail.TransactionType = 'Credit' 
            Left JOIN SET_CostCenter ON SET_CostCenter.CostCenterID=GL_TransactionDetail.CostCenterID
                    AND SET_CostCenter.Status=1
        WHERE AP_PaymentHeader.CompanyID in @CompanyId AND AP_PaymentHeader.PeriodID = @PeriodId AND AP_PaymentHeader.DocumentDate BETWEEN @FromDate AND @ToDate 
            And AP_PaymentHeader.ReversalComments is null 
            {0} 
	    GROUP BY AP_PaymentHeader.CreateDate, AR_SET_Customer.CustomerName, AP_PaymentHeader.DocumentNo, AP_PaymentHeader.DocumentDate, 
            SET_DocumentType.DocumentType, AP_PaymentHeader.PaymentID, AP_PaymentHeader.CompanyID, AP_PaymentHeader.PeriodID,AP_PaymentHeader.Comments, 
            AP_PaymentHeader.POHeaderID,AP_PaymentHeader.PaymentServerOrderNo, AP_Payment_AccountsDetail.Amount, AP_PaymentHeader.ReversalComments, 
            GL_TransactionDetail.TransactionType, GL_TransactionDetail.Amount,SET_CostCenter.CostCenterName

        UNION ALL
                    -----------Sale Invoice Header-----------
        SELECT SET_DocumentType.DocumentType, SAL_InvoiceHeader.CreateDate, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            SAL_InvoiceHeader.InvoiceID as SourceID, case when SET_DocumentType.DocumentType = 'Credit Note' then SAL_InvoiceHeader.AmountPaid * -1 
                else SAL_InvoiceHeader.AmountPaid end as Amount, 
            '0' AS InstrumentNo, '' AS Comments,SET_CostCenter.CostCenterName ,'' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(SAL_InvoiceHeader.InvoiceNumber, 0) AS DocumentNo, 
            SAL_InvoiceHeader.InvoiceDate AS DocumentDate, 
            CASE WHEN GL_Invoice.TransactionType = 'Debit' THEN ISNULL(SAL_InvoiceHeader.FreightAmount * -1, 0) else ISNULL(SAL_InvoiceHeader.FreightAmount, 0) 
                end as FreightAmount, 
            0 As ForAlhajAmount 
        FROM SET_DocumentType 
            INNER JOIN SAL_InvoiceHeader ON SET_DocumentType.CompanyID = SAL_InvoiceHeader.CompanyID AND SET_DocumentType.PeriodID = SAL_InvoiceHeader.PeriodID 
                AND SET_DocumentType.DocumentTypeID = SAL_InvoiceHeader.InvoiceType 
            inner join GL_TransactionHeader on GL_TransactionHeader.DocumentTypeID = SAL_InvoiceHeader.InvoiceType 
				And GL_TransactionHeader.DocumentNo = SAL_InvoiceHeader.InvoiceNumber
            Inner Join GL_TransactionDetail On GL_TransactionDetail.CompanyID = GL_TransactionHeader.CompanyID 
				And GL_TransactionDetail.PeriodID = GL_TransactionHeader.PeriodID And GL_TransactionDetail.TransactionID = GL_TransactionHeader.TransactionID 
                And GL_TransactionDetail.Status = 1 
            INNER JOIN AR_SET_Customer ON SAL_InvoiceHeader.CustomerID = AR_SET_Customer.CustomerID --AND SAL_InvoiceHeader.CompanyID = AR_SET_Customer.CompanyID 
                AND SAL_InvoiceHeader.PeriodID = AR_SET_Customer.PeriodID AND SAL_InvoiceHeader.Status = 1 AND SAL_InvoiceHeader.StatusCode = 1 
                AND AR_SET_Customer.Status = 1 
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
                AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
            INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
                AND AR_SET_BusinessPartner.BusinessPartnerName = 'Customer' 
            LEFT JOIN GL_TransactionDetail GL_Invoice ON GL_Invoice.CodeCombinationID = SAL_InvoiceHeader.FreightCode 
                AND GL_Invoice.SourceHeaderID = SAL_InvoiceHeader.InvoiceID AND GL_Invoice.CompanyID = SAL_InvoiceHeader.CompanyID 
                AND GL_Invoice.PeriodID = SAL_InvoiceHeader.PeriodID 
                Left JOIN SET_CostCenter ON SET_CostCenter.CostCenterID=GL_TransactionDetail.CostCenterID
                    AND SET_CostCenter.Status=1
        WHERE AR_SET_Customer.PeriodID = @PeriodID AND SAL_InvoiceHeader.InvoiceDate BETWEEN @FromDate AND @ToDate 
        And IsNull(Sal_InvoiceHeader.IsReversed, 0) = 0 And Sal_InvoiceHeader.ReversalID is null
        {0}
             
        GROUP BY SAL_InvoiceHeader.CreateDate, AR_SET_Customer.CustomerName, SAL_InvoiceHeader.InvoiceID, SAL_InvoiceHeader.AmountPaid, SAL_InvoiceHeader.InvoiceNumber, SAL_InvoiceHeader.InvoiceDate, 
            SET_DocumentType.DocumentType, GL_Invoice.TransactionType, SAL_InvoiceHeader.FreightAmount ,SET_CostCenter.CostCenterName

        UNION ALL
                    -----------Sale Invoice Header Detail Data-----------
        SELECT SET_DocumentType.DocumentType, SAL_InvoiceHeader.CreateDate, 
            AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            SAL_InvoiceHeader.InvoiceID as SourceID,
            case when SET_DocumentType.DocumentType = 'Credit Note' then SAL_InvoiceHeader.AmountPaid * -1 else
            SAL_InvoiceHeader.AmountPaid end as Amount,
    '0' AS InstrumentNo, '' AS Comments,SET_CostCenter.CostCenterName ,'' as InvoiceNumbers, '' as InvoiceDocumentNo, 
            ISNULL(SAL_InvoiceHeader.InvoiceNumber, 0) AS DocumentNo, SAL_InvoiceHeader.InvoiceDate AS DocumentDate, 
            CASE WHEN GL_Invoice.TransactionType = 'Debit' THEN ISNULL(SAL_InvoiceHeader.FreightAmount * -1, 0) else ISNULL(SAL_InvoiceHeader.FreightAmount, 0) 
                end as FreightAmount , 0 As ForAlhajAmount
        FROM SET_DocumentType 
            INNER JOIN SAL_InvoiceHeader ON SET_DocumentType.CompanyID = SAL_InvoiceHeader.CompanyID AND SET_DocumentType.PeriodID = SAL_InvoiceHeader.PeriodID 
                AND SET_DocumentType.DocumentTypeID = SAL_InvoiceHeader.InvoiceType 
                
            inner join Sal_InvoiceCustomerDetail on Sal_InvoiceCustomerDetail.InvoiceHeaderID = SAL_InvoiceHeader.InvoiceID
            and Sal_InvoiceCustomerDetail.CompanyID = SAL_InvoiceHeader.CompanyID and Sal_InvoiceCustomerDetail.PeriodID = SAL_InvoiceHeader.PeriodID    
            
            Inner Join GL_TransactionDetail On GL_TransactionDetail.CompanyID = Sal_InvoiceCustomerDetail.CompanyID 
				And GL_TransactionDetail.PeriodID = Sal_InvoiceCustomerDetail.PeriodID 
				and GL_TransactionDetail.CodeCombinationID = Sal_InvoiceCustomerDetail.CustomerAccountCode
				And GL_TransactionDetail.Status = 1
            
            INNER JOIN AR_SET_Customer ON Sal_InvoiceCustomerDetail.CustomerID = AR_SET_Customer.CustomerID --AND Sal_InvoiceCustomerDetail.CompanyID = AR_SET_Customer.CompanyID 
                AND Sal_InvoiceCustomerDetail.PeriodID = AR_SET_Customer.PeriodID AND SAL_InvoiceHeader.Status = 1 AND SAL_InvoiceHeader.StatusCode = 1  AND AR_SET_Customer.Status = 1 
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
                AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
                 
            inner JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
            
            LEFT JOIN GL_TransactionDetail GL_Invoice ON GL_Invoice.CodeCombinationID = SAL_InvoiceHeader.FreightCode 
                AND GL_Invoice.SourceHeaderID = SAL_InvoiceHeader.InvoiceID AND GL_Invoice.CompanyID = SAL_InvoiceHeader.CompanyID 
                AND GL_Invoice.PeriodID = SAL_InvoiceHeader.PeriodID
                 Left JOIN SET_CostCenter ON SET_CostCenter.CostCenterID=GL_TransactionDetail.CostCenterID
                    AND SET_CostCenter.Status=1
             
        WHERE AR_SET_Customer.PeriodID = @PeriodID AND SAL_InvoiceHeader.InvoiceDate BETWEEN @FromDate AND @ToDate 
        And IsNull(Sal_InvoiceHeader.IsReversed, 0) = 0 And Sal_InvoiceHeader.ReversalID is null
        {0}
             
        GROUP BY SAL_InvoiceHeader.CreateDate, AR_SET_Customer.CustomerName, SAL_InvoiceHeader.InvoiceID, SAL_InvoiceHeader.AmountPaid, SAL_InvoiceHeader.InvoiceNumber, SAL_InvoiceHeader.InvoiceDate, 
            SET_DocumentType.DocumentType, GL_Invoice.TransactionType, SAL_InvoiceHeader.FreightAmount ,SET_CostCenter.CostCenterName


        union all
                    --RECEIPT TRANSACTION
        SELECT distinct SET_DocumentType.DocumentType, AR_ReceiptTransactionHeader.CreateDate, 
            AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            AR_ReceiptTransactionHeader.ReceiptTransactionID as SourceID,
            Case when 
            (
                select Amount from AR_ReceiptAccountsDetail rad 
                where rad.TransactionType = 'Credit' and rad.ReceiptHeaderID = AR_ReceiptAccountsDetail.ReceiptHeaderID 
                    and rad.ReceiptAccountID = AR_ReceiptAccountsDetail.ReceiptAccountID
	        ) is null then AR_ReceiptTransactionHeader.Amount * -1 else AR_ReceiptTransactionHeader.Amount * -1 end as Amount, 
            '0' AS InstrumentNo, 
            IsNull(AR_ReceiptTransactionHeader.Comments, '') + (Case When AR_ReceiptTransactionHeader.ReversalComments IS NULL Then '' Else ' ' 
                + AR_ReceiptTransactionHeader.ReversalComments End) AS Comments,SET_CostCenter.CostCenterName, 
            '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(AR_ReceiptTransactionHeader.DocumentNo, 0) AS DocumentNo, 
            AR_ReceiptTransactionHeader.DocumentDate AS DocumentDate, 0 AS FreightAmount, 0 As ForAlhajAmount 
        FROM SET_DocumentType 
            INNER JOIN AR_ReceiptTransactionHeader ON SET_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                AND SET_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID AND SET_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
                AND AR_ReceiptTransactionHeader.Status = 1 AND AR_ReceiptTransactionHeader.StatusCode = 1 
              
                inner join GL_TransactionHeader on GL_TransactionHeader.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
				And GL_TransactionHeader.DocumentNo = AR_ReceiptTransactionHeader.DocumentNo 
				Inner Join GL_TransactionDetail On GL_TransactionDetail.CompanyID = GL_TransactionHeader.CompanyID 
				And GL_TransactionDetail.PeriodID = GL_TransactionHeader.PeriodID 
				And GL_TransactionDetail.TransactionID = GL_TransactionHeader.TransactionID And GL_TransactionDetail.Status = 1
                
            INNER JOIN AR_SET_Customer ON AR_ReceiptTransactionHeader.CustomerID = AR_SET_Customer.CustomerID 
--                AND AR_ReceiptTransactionHeader.CompanyID = AR_SET_Customer.CompanyID 
AND AR_ReceiptTransactionHeader.PeriodID = AR_SET_Customer.PeriodID 
                AND AR_SET_Customer.Status = 1 
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
                AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
            Left JOIN SET_CostCenter ON SET_CostCenter.CostCenterID=GL_TransactionDetail.CostCenterID
                    AND SET_CostCenter.Status=1
            INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
            Left Join AR_ReceiptAccountsDetail on AR_ReceiptAccountsDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                AND AR_ReceiptAccountsDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
                AND AR_ReceiptAccountsDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID	
             
        WHERE AR_SET_Customer.PeriodID = @PeriodID AND AR_ReceiptTransactionHeader.DocumentDate BETWEEN @FromDate AND @ToDate 
        And AR_ReceiptTransactionHeader.ReversalComments is null 
        {0}
        and AR_ReceiptTransactionHeader.ReceiptTransactionID not in 
        (
			select ReceiptHeaderID from AR_ReceiptCustomerDetail
			inner join AR_ReceiptTransactionHeader RecHDR on RecHDR.ReceiptTransactionID = AR_ReceiptTransactionHeader.ReceiptTransactionID
			and RecHDR.CompanyID = AR_ReceiptTransactionHeader.CompanyID
			and RecHDR.PeriodID = AR_ReceiptTransactionHeader.PeriodID
			And AR_ReceiptTransactionHeader.ReversalComments is null 
			inner join set_documentType on set_documentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID
			and set_documentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID
			and set_documentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
             
			where AR_ReceiptCustomerDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID
			and AR_ReceiptCustomerDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID
			and AR_ReceiptCustomerDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID
			and set_documentType.documentType not like 'Carriage Contractor Adjustment'
        ) 
             
        GROUP BY AR_ReceiptTransactionHeader.CreateDate, AR_SET_Customer.CustomerName, AR_ReceiptTransactionHeader.ReceiptTransactionID, AR_ReceiptAccountsDetail.ReceiptHeaderID, 
            AR_ReceiptAccountsDetail.ReceiptAccountID,AR_ReceiptAccountsDetail.Amount, AR_ReceiptTransactionHeader.Amount, AR_ReceiptTransactionHeader.DocumentNo, 
            AR_ReceiptTransactionHeader.DocumentDate, SET_DocumentType.DocumentType, Comments, ReversalComments ,SET_CostCenter.CostCenterName

        UNION ALL
    -- Receipt Transaction Without Opening
        SELECT distinct SET_DocumentType.DocumentType, AR_ReceiptTransactionHeader.CreateDate, 
            GL_TransactionDetail.Narration AS BusinessPartnerName, 
            GL_TransactionDetail.SerialNo as SourceID,
            Case when GL_TransactionDetail.TransactionType = 'Debit' then GL_TransactionDetail.Amount else GL_TransactionDetail.Amount * -1 end as Amount, 
            '0' AS InstrumentNo, 
            IsNull(AR_ReceiptTransactionHeader.Comments, '') + (Case When AR_ReceiptTransactionHeader.ReversalComments IS NULL Then '' Else ' ' 
                + AR_ReceiptTransactionHeader.ReversalComments End) AS Comments,SET_CostCenter.CostCenterName, 
            '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(AR_ReceiptTransactionHeader.DocumentNo, 0) AS DocumentNo, AR_ReceiptTransactionHeader.DocumentDate, 
            0 AS FreightAmount, 0 As ForAlhajAmount 
        FROM SET_DocumentType 
            INNER JOIN AR_ReceiptTransactionHeader ON SET_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                AND SET_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID AND SET_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
                AND AR_ReceiptTransactionHeader.Status = 1 AND AR_ReceiptTransactionHeader.StatusCode = 1 
              
                
                inner join GL_TransactionHeader on GL_TransactionHeader.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
				And GL_TransactionHeader.DocumentNo = AR_ReceiptTransactionHeader.DocumentNo 
				Inner Join GL_TransactionDetail On GL_TransactionDetail.CompanyID = GL_TransactionHeader.CompanyID 
				And GL_TransactionDetail.PeriodID = GL_TransactionHeader.PeriodID 
				And GL_TransactionDetail.TransactionID = GL_TransactionHeader.TransactionID And GL_TransactionDetail.Status = 1
                
            INNER JOIN AR_ReceiptCustomerDetail on AR_ReceiptCustomerDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID 
                and AR_ReceiptCustomerDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                and AR_ReceiptCustomerDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
                
                
                
            INNER JOIN AR_SET_Customer ON AR_ReceiptCustomerDetail.CustomerID = AR_SET_Customer.CustomerID 
--                AND AR_ReceiptCustomerDetail.CompanyID = AR_SET_Customer.CompanyID
AND AR_ReceiptCustomerDetail.PeriodID = AR_SET_Customer.PeriodID 
                AND AR_SET_Customer.Status = 1 
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
                AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
	             
            INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
            LEFT Join AR_ReceiptAccountsDetail on AR_ReceiptAccountsDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                AND AR_ReceiptAccountsDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
                AND AR_ReceiptAccountsDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID	
              Left JOIN SET_CostCenter ON SET_CostCenter.CostCenterID=GL_TransactionDetail.CostCenterID
                    AND SET_CostCenter.Status=1
        WHERE AR_SET_Customer.PeriodID = @PeriodID AND AR_ReceiptTransactionHeader.DocumentDate BETWEEN @FromDate AND @ToDate 
              and SET_DocumentType.DocumentType not like '%opening%' 
              
               {0}
             
--GROUP BY AR_ReceiptTransactionHeader.CreateDate, AR_SET_Customer.CustomerName, 
        --    AR_ReceiptTransactionHeader.Amount, AR_ReceiptTransactionHeader.DocumentNo, AR_ReceiptTransactionHeader.DocumentDate, SET_DocumentType.DocumentType, Comments, 
        --    AR_ReceiptTransactionHeader.ReversalComments
        --    ,GL_TransactionDetail.SerialNo,GL_TransactionDetail.TransactionType,GL_TransactionDetail.Amount

    UNION ALL
        -- Receipt Transaction [Only Opening]

        SELECT SET_DocumentType.DocumentType, AR_ReceiptTransactionHeader.CreateDate, 
            AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            AR_ReceiptCustomerDetail.ReceiptCustomerID as SourceID,
            Case when AR_ReceiptCustomerDetail.TransactionType = 'Debit' then AR_ReceiptCustomerDetail.Amount else AR_ReceiptCustomerDetail.Amount * -1 end as Amount, 
            '0' AS InstrumentNo, 
            IsNull(AR_ReceiptTransactionHeader.Comments, '') + (Case When AR_ReceiptTransactionHeader.ReversalComments IS NULL Then '' Else ' ' 
                + AR_ReceiptTransactionHeader.ReversalComments End) AS Comments,SET_CostCenter.CostCenterName, 
            '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(AR_ReceiptTransactionHeader.DocumentNo, 0) AS DocumentNo, AR_ReceiptTransactionHeader.DocumentDate, 
            0 AS FreightAmount, 0 As ForAlhajAmount 
        FROM SET_DocumentType 
            INNER JOIN AR_ReceiptTransactionHeader ON SET_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                AND SET_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID AND SET_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
                AND AR_ReceiptTransactionHeader.Status = 1 AND AR_ReceiptTransactionHeader.StatusCode = 1 
           
INNER JOIN AR_ReceiptCustomerDetail on AR_ReceiptCustomerDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID 
                and AR_ReceiptCustomerDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                and AR_ReceiptCustomerDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
                Inner Join GL_TransactionDetail On GL_TransactionDetail.CompanyID = AR_ReceiptCustomerDetail.CompanyID 
				And GL_TransactionDetail.PeriodID = AR_ReceiptCustomerDetail.PeriodID 
				and GL_TransactionDetail.CodeCombinationID = AR_ReceiptCustomerDetail.CustomerAccountCode
				And GL_TransactionDetail.Status = 1
                
            INNER JOIN AR_SET_Customer ON AR_ReceiptCustomerDetail.CustomerID = AR_SET_Customer.CustomerID 
--                AND AR_ReceiptCustomerDetail.CompanyID = AR_SET_Customer.CompanyID
AND AR_ReceiptCustomerDetail.PeriodID = AR_SET_Customer.PeriodID 
                AND AR_SET_Customer.Status = 1 
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
                AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
	             
            INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
            LEFT Join AR_ReceiptAccountsDetail on AR_ReceiptAccountsDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                AND AR_ReceiptAccountsDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
                AND AR_ReceiptAccountsDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID	
              Left JOIN SET_CostCenter ON SET_CostCenter.CostCenterID=GL_TransactionDetail.CostCenterID
                    AND SET_CostCenter.Status=1
        WHERE AR_SET_Customer.PeriodID = @PeriodID 
              AND AR_ReceiptTransactionHeader.DocumentDate between @FromDate and @Todate
              and SET_DocumentType.DocumentType like '%opening%' 
              And AR_ReceiptTransactionHeader.ReversalComments is null 
              {0}
             
        GROUP BY AR_ReceiptTransactionHeader.CreateDate, AR_SET_Customer.CustomerName, AR_ReceiptCustomerDetail.ReceiptCustomerID,
            AR_ReceiptCustomerDetail.TransactionType, AR_ReceiptCustomerDetail.Amount, 
            AR_ReceiptTransactionHeader.Amount, AR_ReceiptTransactionHeader.DocumentNo, AR_ReceiptTransactionHeader.DocumentDate, SET_DocumentType.DocumentType, Comments, 
            AR_ReceiptTransactionHeader.ReversalComments,SET_CostCenter.CostCenterName

	    UNION ALL
	    -- Commission On Receipt
	    SELECT SET_DocumentType.DocumentType, AR_ReceiptTransactionHeader.CreateDate, 
            AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            AR_ReceiptTransactionHeader.ReceiptTransactionID as SourceID,	    
		    ((AR_ReceiptTransactionHeader.Amount * AR_ReceiptTransactionHeader.CommissionPercentage)/100) * -1 as Amount,
            '0' AS InstrumentNo, 
            IsNull(AR_ReceiptTransactionHeader.Comments, '') + (Case When AR_ReceiptTransactionHeader.ReversalComments IS NULL Then '' Else ' ' 
                + AR_ReceiptTransactionHeader.ReversalComments End) AS Comments,SET_CostCenter.CostCenterName, 
            '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(AR_ReceiptTransactionHeader.DocumentNo, 0) AS DocumentNo, 
            AR_ReceiptTransactionHeader.DocumentDate AS DocumentDate, 0 AS FreightAmount, 0 As ForAlhajAmount 
        FROM SET_DocumentType 
            INNER JOIN AR_ReceiptTransactionHeader ON SET_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                AND SET_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID AND SET_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
                AND AR_ReceiptTransactionHeader.Status = 1 AND AR_ReceiptTransactionHeader.StatusCode = 1 
               
                inner join GL_TransactionHeader on GL_TransactionHeader.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
				And GL_TransactionHeader.DocumentNo = AR_ReceiptTransactionHeader.DocumentNo 
				 and AR_ReceiptTransactionHeader.FiscalID=GL_TransactionHeader.FiscalID
Inner Join GL_TransactionDetail On GL_TransactionDetail.CompanyID = GL_TransactionHeader.CompanyID 
				And GL_TransactionDetail.PeriodID = GL_TransactionHeader.PeriodID 
				And GL_TransactionDetail.TransactionID = GL_TransactionHeader.TransactionID And GL_TransactionDetail.Status = 1
                
            INNER JOIN AR_SET_Customer ON AR_ReceiptTransactionHeader.EmployeeID = AR_SET_Customer.CustomerID 
--                AND AR_ReceiptTransactionHeader.CompanyID = AR_SET_Customer.CompanyID
AND AR_ReceiptTransactionHeader.PeriodID = AR_SET_Customer.PeriodID 
                AND AR_SET_Customer.Status = 1 
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
                AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID
AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
             Left JOIN SET_CostCenter ON SET_CostCenter.CostCenterID=GL_TransactionDetail.CostCenterID
                    AND SET_CostCenter.Status=1      
            INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
            Left Join AR_ReceiptAccountsDetail on AR_ReceiptAccountsDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                AND AR_ReceiptAccountsDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
                AND AR_ReceiptAccountsDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID	
             
        WHERE AR_SET_Customer.PeriodID = @PeriodID AND AR_ReceiptTransactionHeader.DocumentDate BETWEEN @FromDate AND @ToDate 
        And AR_ReceiptTransactionHeader.ReversalComments is null 
        {0}
             
        GROUP BY AR_ReceiptTransactionHeader.CreateDate, AR_SET_Customer.CustomerName, 
		    AR_ReceiptTransactionHeader.ReceiptTransactionID, AR_ReceiptTransactionHeader.CommissionPercentage, AR_ReceiptAccountsDetail.ReceiptHeaderID, 
            AR_ReceiptAccountsDetail.ReceiptAccountID,AR_ReceiptAccountsDetail.Amount, AR_ReceiptTransactionHeader.Amount, AR_ReceiptTransactionHeader.DocumentNo, 
            AR_ReceiptTransactionHeader.DocumentDate, SET_DocumentType.DocumentType, Comments, ReversalComments,SET_CostCenter.CostCenterName 

     -------- SALE DISCOUNT -----------
        
            union All
            SELECT distinct SET_DocumentType.DocumentType, SAL_DiscountTransactionHeader.CreateDate, 
            AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            SAL_DiscountTransactionHeader.DiscountTransactionHeaderID as SourceID,
            (SAL_DiscountTransactionHeader.CalculatedAmount * -1) as Amount, 
            '0' AS InstrumentNo, 
            '' AS Comments,SET_CostCenter.CostCenterName, 
            '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(SAL_DiscountTransactionHeader.DiscountTransactionHeaderID, 0) AS DocumentNo, 
            SAL_DiscountTransactionHeader.RunDateTime AS DocumentDate, 0 AS FreightAmount, 0 As ForAlhajAmount 
        FROM SET_DocumentType 
            INNER JOIN SAL_DiscountTransactionHeader ON SET_DocumentType.CompanyID = SAL_DiscountTransactionHeader.CompanyID 
                AND SET_DocumentType.PeriodID = SAL_DiscountTransactionHeader.PeriodID 
                AND SET_DocumentType.DocumentTypeID = SAL_DiscountTransactionHeader.DocumentTypeID
                
            INNER join GL_TransactionHeader on GL_TransactionHeader.DocumentTypeID = SAL_DiscountTransactionHeader.DocumentTypeID 
				And GL_TransactionHeader.DocumentNo = SAL_DiscountTransactionHeader.DiscountTransactionHeaderID
				Inner Join GL_TransactionDetail On GL_TransactionDetail.CompanyID = GL_TransactionHeader.CompanyID 
				And GL_TransactionDetail.PeriodID = GL_TransactionHeader.PeriodID 
				And GL_TransactionDetail.TransactionID = GL_TransactionHeader.TransactionID And GL_TransactionDetail.Status = 1
                
            INNER JOIN AR_SET_Customer ON SAL_DiscountTransactionHeader.CustomerID = AR_SET_Customer.CustomerID 
--                AND SAL_DiscountTransactionHeader.CompanyID = AR_SET_Customer.CompanyID
AND SAL_DiscountTransactionHeader.PeriodID = AR_SET_Customer.PeriodID 
                AND AR_SET_Customer.Status = 1 
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
                AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
                   
            INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
              Left JOIN SET_CostCenter ON SET_CostCenter.CostCenterID=GL_TransactionDetail.CostCenterID
                    AND SET_CostCenter.Status=1
                    
        WHERE AR_SET_Customer.PeriodID = @PeriodID 
        AND SAL_DiscountTransactionHeader.RunDateTime BETWEEN @FromDate AND @ToDate 
        {0}

          -- JOURNAL VOUCHER
     union all
         SELECT distinct SET_DocumentType.DocumentType, GL_TransactionHeader.CreateDate, 
            '' AS BusinessPartnerName, 
            GL_TransactionDetail.SerialNo as SourceID,
            case GL_TransactionDetail.TransactionType when 'Debit' then GL_TransactionDetail.Amount else GL_TransactionDetail.Amount * -1 end  as Amount, 
            '0' AS InstrumentNo, 
            '' AS Comments,SET_CostCenter.CostCenterName, 
            '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(GL_TransactionHeader.documentNo, 0) AS DocumentNo, 
            GL_TransactionHeader.DocumentDate AS DocumentDate, 0 AS FreightAmount, 0 As ForAlhajAmount 
        FROM SET_DocumentType     
            INNER join GL_TransactionHeader on GL_TransactionHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID 
				Inner Join GL_TransactionDetail On GL_TransactionDetail.CompanyID = GL_TransactionHeader.CompanyID 
				And GL_TransactionDetail.PeriodID = GL_TransactionHeader.PeriodID 
				And GL_TransactionDetail.TransactionID = GL_TransactionHeader.TransactionID And GL_TransactionDetail.Status = 1
              Left JOIN SET_CostCenter ON SET_CostCenter.CostCenterID=GL_TransactionDetail.CostCenterID
                    AND SET_CostCenter.Status=1
        WHERE GL_TransactionHeader.CompanyID in @CompanyId AND GL_TransactionHeader.PeriodID = @PeriodID 
        AND GL_TransactionHeader.DocumentDate BETWEEN @FromDate AND @ToDate 
        and (SET_DocumentType.DocumentType like '%JOURNAL VOUCHER%' Or SET_DocumentType.DocumentType like '%GL-OPENING%')
         {0}

 ----- Payment With out ANY Customer
          union all
          SELECT SET_DocumentType.DocumentType, 
        AP_PaymentHeader.CreateDate, 
        '' AS BusinessPartnerName, 
         GL_TransactionDetail.SerialNo as SourceID,
      	case GL_TransactionDetail.TransactionType when 'Debit' then GL_TransactionDetail.Amount else GL_TransactionDetail.Amount * -1
		end as Amount,
	    AP_PaymentHeader.PaymentServerOrderNo AS InstrumentNo, 
        AP_PaymentHeader.Comments + (Case When AP_PaymentHeader.ReversalComments IS NULL Then '' Else ' ' + AP_PaymentHeader.ReversalComments End) AS Comments, 
        SET_CostCenter.CostCenterName,'' as InvoiceNumbers, 
            '' as InvoiceDocumentNo, 
            ISNULL(AP_PaymentHeader.DocumentNo, 0) AS DocumentNo, AP_PaymentHeader.DocumentDate AS DocumentDate, 0 AS FreightAmount
            , 0 As ForAlhajAmount 
        FROM SET_DocumentType 
            INNER JOIN AP_PaymentHeader ON SET_DocumentType.CompanyID = AP_PaymentHeader.CompanyID AND SET_DocumentType.PeriodID = AP_PaymentHeader.PeriodID 
                AND SET_DocumentType.DocumentTypeID = AP_PaymentHeader.DocumentTypeID AND AP_PaymentHeader.Status = 1 AND AP_PaymentHeader.StatusCode = 1 
                
                inner join GL_TransactionHeader on GL_TransactionHeader.DocumentTypeID = AP_PaymentHeader.DocumentTypeID 
				And GL_TransactionHeader.DocumentNo = AP_PaymentHeader.DocumentNo 
                  and GL_TransactionHeader.FiscalID=AP_PaymentHeader.FiscalID
				Inner Join GL_TransactionDetail On GL_TransactionDetail.CompanyID = GL_TransactionHeader.CompanyID 
				And GL_TransactionDetail.PeriodID = GL_TransactionHeader.PeriodID 
				And GL_TransactionDetail.TransactionID = GL_TransactionHeader.TransactionID And GL_TransactionDetail.Status = 1
				 Left JOIN SET_CostCenter ON SET_CostCenter.CostCenterID=GL_TransactionDetail.CostCenterID
                    AND SET_CostCenter.Status=1
                 
        WHERE AP_PaymentHeader.CompanyID in @CompanyId AND AP_PaymentHeader.PeriodID = @PeriodId AND AP_PaymentHeader.DocumentDate BETWEEN @FromDate AND @ToDate 
        And AP_PaymentHeader.ReversalComments is null 
        and AP_PaymentHeader.VendorID is null
        --and AP_PaymentHeader.PaymentID not in
        --(
        --select PaymentHeaderID 
        --from AP_PaymentSupplierDetail
        --where AP_PaymentSupplierDetail.PaymentHeaderID = AP_PaymentHeader.PaymentID
        --and AP_PaymentSupplierDetail.CompanyID = AP_PaymentHeader.CompanyID and AP_PaymentSupplierDetail.periodID = AP_PaymentHeader.periodID
        --) 
        {0}

        union all
        ----- Receipt With out ANY Customer
          SELECT SET_DocumentType.DocumentType, 
        AR_ReceiptTransactionHeader.CreateDate, 
        '' AS BusinessPartnerName, 
        GL_TransactionDetail.SerialNo as SourceID,
      	case GL_TransactionDetail.TransactionType when 'Debit' then GL_TransactionDetail.Amount else GL_TransactionDetail.Amount * -1
		end as Amount,
	    AR_ReceiptTransactionHeader.PaymentServerOrderNo AS InstrumentNo, 
        AR_ReceiptTransactionHeader.Comments + (Case When AR_ReceiptTransactionHeader.ReversalComments IS NULL Then '' Else ' ' + AR_ReceiptTransactionHeader.ReversalComments End) AS Comments, 
        SET_CostCenter.CostCenterName,'' as InvoiceNumbers, 
            '' as InvoiceDocumentNo, 
            ISNULL(AR_ReceiptTransactionHeader.DocumentNo, 0) AS DocumentNo, AR_ReceiptTransactionHeader.DocumentDate AS DocumentDate, 0 AS FreightAmount
            , 0 As ForAlhajAmount 
        FROM SET_DocumentType 
            INNER JOIN AR_ReceiptTransactionHeader ON SET_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID AND SET_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
                AND SET_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID AND AR_ReceiptTransactionHeader.Status = 1 AND AR_ReceiptTransactionHeader.StatusCode = 1 
                
                inner join GL_TransactionHeader on GL_TransactionHeader.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
				And GL_TransactionHeader.DocumentNo = AR_ReceiptTransactionHeader.DocumentNo 
				Inner Join GL_TransactionDetail On GL_TransactionDetail.CompanyID = GL_TransactionHeader.CompanyID 
				And GL_TransactionDetail.PeriodID = GL_TransactionHeader.PeriodID 
                  AND AR_ReceiptTransactionHeader.FiscalID=GL_TransactionHeader.FiscalID
				And GL_TransactionDetail.TransactionID = GL_TransactionHeader.TransactionID And GL_TransactionDetail.Status = 1
             Left JOIN SET_CostCenter ON SET_CostCenter.CostCenterID=GL_TransactionDetail.CostCenterID
                    AND SET_CostCenter.Status=1     
        WHERE AR_ReceiptTransactionHeader.CompanyID in @CompanyId AND AR_ReceiptTransactionHeader.PeriodID = @PeriodId AND AR_ReceiptTransactionHeader.DocumentDate BETWEEN @FromDate AND @ToDate 
        And AR_ReceiptTransactionHeader.ReversalComments is null 
        and AR_ReceiptTransactionHeader.CustomerID is null
        and AR_ReceiptTransactionHeader.ReceiptTransactionID not in
        (
        select ReceiptTransactionID
        from AR_ReceiptCustomerDetail
        where AR_ReceiptCustomerDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID
        and AR_ReceiptCustomerDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID and AR_ReceiptCustomerDetail.periodID = AR_ReceiptTransactionHeader.periodID
        ) 
        {0}

    ) AS FINTAB
    GROUP BY DocumentType, CreateDate, NAME, SourceID, InstrumentNo, Comments, InvoiceNumbers, InvoiceDocumentNo, DocumentNo, DocumentDate, FreightAmount, ForAlhajAmount,
    FINTAB.CostCenterName 

) as TAb ";
                #endregion

                companyID = "(" + companyID + ")";
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_NewAccountsTrial_Report, additionalFilter.ToString()).Replace("@CompanyId", companyID), sqlParams.ToArray());

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

        public static DataTable Get_BusinessPartnerGL_ReportData(string companyID, int periodID, Nullable<int> businessPartnerID, Nullable<DateTime> dateFrom, Nullable<int> DocumentTypeID, string Chartofacc, string ChartofaccTo,
            string businessPartnerType, Nullable<DateTime> dateTo, int CurrentFiscalID, bool discardReversedDocuments, int LocationID = 0, int CostCenterID = 0, string documentGroupID = "", string Comments = "", int? CustomerCategoryID = null, int? BrokerID = null, int? EmployeeStatus = null)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            StringBuilder generalLedgerFilter = new StringBuilder();
            StringBuilder paymentFilter = new StringBuilder();
            StringBuilder purchaseInvoiceSupplierFilter = new StringBuilder();
            StringBuilder SaleInvoiceFilter = new StringBuilder();
            StringBuilder OnlyCustomerfilter = new StringBuilder();
            StringBuilder LocationFillter = new StringBuilder();
            StringBuilder CostCenterFillter = new StringBuilder();
            StringBuilder documentGroupFilter = new StringBuilder();
            DataTable dtToReturn = null;
            StringBuilder sbCC = new StringBuilder();
            StringBuilder sbCC2 = new StringBuilder();

            StringBuilder AccountCodeFilter = new StringBuilder();
            StringBuilder CommentsFilter = new StringBuilder();
            string businessPartnerTypeFilter = string.Empty, businessPartnerTypeFilter2 = string.Empty;

            string qry_SalesInvoiceFilter = string.Empty, qry_ReceiptFilter = string.Empty, qry_PurchaseInvoiceFilter = string.Empty,
                qry_PaymentFilter = string.Empty;

            #region Filter Reversed Documents

            if (discardReversedDocuments)
            {
                qry_SalesInvoiceFilter = " And IsNull(Sal_InvoiceHeader.IsReversed, 0) = 0 And Sal_InvoiceHeader.ReversalID is null ";
                qry_ReceiptFilter = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                qry_PurchaseInvoiceFilter = " And IsNull(AR_InvoiceHeader.IsReversed, 0) = 0 And AR_InvoiceHeader.ReversalID is null ";
                qry_PaymentFilter = " And AP_PaymentHeader.ReversalComments is null ";
            }

            #endregion

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));
                sqlParams.Add(new SqlParameter("@FromDate", ((DateTime)dateFrom).ToString("yyyy-MM-dd")));
                sqlParams.Add(new SqlParameter("@ToDate", ((DateTime)dateTo).ToString("yyyy-MM-dd")));
               
                companyID = "(" + companyID + ")";

                if (businessPartnerID.HasValue)
                {
                    generalLedgerFilter.Append(" and AR_Set_Customer.CustomerID = " + businessPartnerID + " ");
                    paymentFilter.Append(" and AR_Set_Customer.CustomerID = " + businessPartnerID + " ");
                    purchaseInvoiceSupplierFilter.Append(" And AR_InvoiceSupplierDetail.SupplierID = " + businessPartnerID + " ");
                    SaleInvoiceFilter.Append(" and TABInvoice.CustomerID = " + businessPartnerID + " ");
                    OnlyCustomerfilter.Append(" and AR_Set_Customer.CustomerID = " + businessPartnerID + "");
                    sqlParams.Add(new SqlParameter("@BusinessPartnerID", businessPartnerID));

                }
                else
                    sqlParams.Add(new SqlParameter("@BusinessPartnerID", DBNull.Value));

                if (Chartofacc != "" && ChartofaccTo != "")
                {
                    sqlParams.Add(new SqlParameter("@AccountCode", Chartofacc));
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", ChartofaccTo));
                    AccountCodeFilter.Append(" and  (GL_codecombinationDetail.CodeCombinationID between '" + Chartofacc + "' and '" + ChartofaccTo + "') ");
                }
                else if (Chartofacc != "")
                {
                    sqlParams.Add(new SqlParameter("@AccountCode", Chartofacc));
                    AccountCodeFilter.Append(" and  (GL_codecombinationDetail.CodeCombinationID = '" + Chartofacc + "' ) ");

                }
                else if (ChartofaccTo != "")
                {
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", ChartofaccTo));
                    AccountCodeFilter.Append(" and  (GL_codecombinationDetail.CodeCombinationID = '" + ChartofaccTo + "' ) ");
                }

                if (BrokerID.HasValue)
                {
                    generalLedgerFilter.Append(" and AR_Set_Customer.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                    paymentFilter.Append(" and AR_Set_Customer.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                    purchaseInvoiceSupplierFilter.Append(" And AR_InvoiceSupplierDetail.SupplierID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                    SaleInvoiceFilter.Append(" and TABInvoice.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                    OnlyCustomerfilter.Append(" and AR_Set_Customer.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                }


                if (DocumentTypeID.HasValue)
                {
                    sqlParams.Add(new SqlParameter("@DocumentTypeID", DocumentTypeID));
                }
                else
                {
                    sqlParams.Add(new SqlParameter("@DocumentTypeID", DBNull.Value));
                }
                if (LocationID > 0)
                    LocationFillter.Append("and GL_TransactionHeader.LocationID = " + LocationID + "");

                if (CustomerCategoryID.HasValue)
                {
                    sbCC.Append(" left join AR_SET_Customer as CusCat on CusCat.CustomerID = tab.CustomerID ");
                    sbCC2.Append(" where CusCat.CustomerCategoryID = " + CustomerCategoryID.ToString() + "");
                }

                if (Comments != "" && CustomerCategoryID.HasValue)
                {
                    CommentsFilter.Append(" and isnull(tab.Comments,'') like '%" + Comments.ToString() + "%' ");
                }
                else if (Comments != "")
                {
                    CommentsFilter.Append(" where isnull(tab.Comments,'') like '%" + Comments.ToString() + "%' ");
                }

                if (CostCenterID > 0)
                {
                    CostCenterFillter.Append(" AND Set_CostCenter.CostCenterID = " + CostCenterID + " ");
                }

                #region Document Group new group
                if (documentGroupID != "" )
                    documentGroupFilter.Append(" AND Set_DocumentType.GroupID in " + documentGroupID);
                #endregion


                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                {
                    businessPartnerTypeFilter = " AND AR_SET_BusinessPartnerTypes.TypeCode in (" + businessPartnerType + ") ";
                }

                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                {
                    businessPartnerTypeFilter2 = " AND TABInvoice.TypeCode in (" + businessPartnerType + ") ";
                }



                if (EmployeeStatus.HasValue)
                {

                    generalLedgerFilter.Append(" and AR_SET_Customer.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ") ");
                    paymentFilter.Append(" and AR_SET_Customer.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ")");
                    purchaseInvoiceSupplierFilter.Append(" And AR_InvoiceSupplierDetail.SupplierID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ") ");
                    SaleInvoiceFilter.Append(" and TABInvoice.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ") ");
                    OnlyCustomerfilter.Append(" and AR_Set_Customer.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ")");
                }


                string Query = string.Format(FnI.QueryResourceLibrary.GL.GL_TransactionHeader4.Query_BusinessPartnerGLDetail,
                    generalLedgerFilter.ToString(), 
                    paymentFilter.ToString(), 
                    purchaseInvoiceSupplierFilter.ToString(), 
                    SaleInvoiceFilter.ToString(), 
                    OnlyCustomerfilter.ToString(), 
                    LocationFillter.ToString(), 
                    documentGroupFilter.ToString(), 
                    sbCC.ToString(), 
                    sbCC2.ToString(), 
                    AccountCodeFilter.ToString(), 
                    CommentsFilter.ToString(), 
                    CostCenterFillter.ToString(), 
                    businessPartnerTypeFilter.ToString(),
                    //13
                    qry_SalesInvoiceFilter.ToString(), qry_ReceiptFilter.ToString(), qry_PurchaseInvoiceFilter.ToString(), qry_PaymentFilter.ToString()).Replace("@CompanyID", companyID);


                if (DocumentTypeID.HasValue)
                    Query = Query.Replace("@DocumentTypeID", DocumentTypeID.ToString());
                else
                    Query = Query.Replace("@DocumentTypeID", "null");



                if (businessPartnerID.HasValue)
                    Query = Query.Replace("@BusinessPartnerID", businessPartnerID.ToString());
                else
                    Query = Query.Replace("@BusinessPartnerID", "null");


                Query = Query.Replace("@PeriodID", periodID.ToString());
                Query = Query.Replace("@CurrentFiscalID", CurrentFiscalID.ToString());

                Query = Query.Replace("@FromDate", "'" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                Query = Query.Replace("@ToDate", "'" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");


                if (Chartofacc != "" && ChartofaccTo != "")
                {
                    Query = Query.Replace("@AccountCodeTo", "'" + ChartofaccTo + "'");
                    Query = Query.Replace("@AccountCode", "'" + Chartofacc + "'");
                    
                }
                else
                {
                    Query = Query.Replace("@AccountCodeTo", "null");
                    Query = Query.Replace("@AccountCode", "null");
                    
                }


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text,Query , sqlParams.ToArray());


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

        public static DataTable Get_BusinessPartnerGL_ReportData_ForDKSM(string companyID, int periodID, Nullable<int> businessPartnerID, Nullable<DateTime> dateFrom, Nullable<int> DocumentTypeID, string Chartofacc, string ChartofaccTo,
            string businessPartnerType, Nullable<DateTime> dateTo, int CurrentFiscalID, bool discardReversedDocuments, int LocationID = 0, int CostCenterID = 0, string documentGroupID = "", string Comments = "", int? CustomerCategoryID = null, int? BrokerID = null, int? EmployeeStatus = null, string SheetNo = null, string FAX = null)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            StringBuilder generalLedgerFilter = new StringBuilder();
            StringBuilder paymentFilter = new StringBuilder();
            StringBuilder purchaseInvoiceSupplierFilter = new StringBuilder();
            StringBuilder SaleInvoiceFilter = new StringBuilder();
            StringBuilder OnlyCustomerfilter = new StringBuilder();
            StringBuilder LocationFillter = new StringBuilder();
            StringBuilder CostCenterFillter = new StringBuilder();
            StringBuilder documentGroupFilter = new StringBuilder();
            DataTable dtToReturn = null;
            StringBuilder sbCC = new StringBuilder();
            StringBuilder sbCC2 = new StringBuilder();

            StringBuilder AccountCodeFilter = new StringBuilder();
            StringBuilder CommentsFilter = new StringBuilder();
            StringBuilder SheetNo_FAXNo_Filter = new StringBuilder();
            string businessPartnerTypeFilter = string.Empty, businessPartnerTypeFilter2 = string.Empty;

            string qry_SalesInvoiceFilter = string.Empty, qry_ReceiptFilter = string.Empty, qry_PurchaseInvoiceFilter = string.Empty,
                qry_PaymentFilter = string.Empty;

            #region Filter Reversed Documents

            if (discardReversedDocuments)
            {
                qry_SalesInvoiceFilter = " And IsNull(Sal_InvoiceHeader.IsReversed, 0) = 0 And Sal_InvoiceHeader.ReversalID is null ";
                qry_ReceiptFilter = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                qry_PurchaseInvoiceFilter = " And IsNull(AR_InvoiceHeader.IsReversed, 0) = 0 And AR_InvoiceHeader.ReversalID is null ";
                qry_PaymentFilter = " And AP_PaymentHeader.ReversalComments is null ";
            }

            #endregion

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));
                sqlParams.Add(new SqlParameter("@FromDate", ((DateTime)dateFrom).ToString("yyyy-MM-dd")));
                sqlParams.Add(new SqlParameter("@ToDate", ((DateTime)dateTo).ToString("yyyy-MM-dd")));

                companyID = "(" + companyID + ")";

                if (businessPartnerID.HasValue)
                {
                    generalLedgerFilter.Append(" and AR_Set_Customer.CustomerID = " + businessPartnerID + " ");
                    paymentFilter.Append(" and AR_Set_Customer.CustomerID = " + businessPartnerID + " ");
                    purchaseInvoiceSupplierFilter.Append(" And AR_InvoiceSupplierDetail.SupplierID = " + businessPartnerID + " ");
                    SaleInvoiceFilter.Append(" and TABInvoice.CustomerID = " + businessPartnerID + " ");
                    OnlyCustomerfilter.Append(" and AR_Set_Customer.CustomerID = " + businessPartnerID + "");
                    sqlParams.Add(new SqlParameter("@BusinessPartnerID", businessPartnerID));

                }
                else
                    sqlParams.Add(new SqlParameter("@BusinessPartnerID", DBNull.Value));

                if (Chartofacc != "" && ChartofaccTo != "")
                {
                    sqlParams.Add(new SqlParameter("@AccountCode", Chartofacc));
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", ChartofaccTo));
                    AccountCodeFilter.Append(" and  (GL_codecombinationDetail.CodeCombinationID between '" + Chartofacc + "' and '" + ChartofaccTo + "') ");
                }
                else if (Chartofacc != "")
                {
                    sqlParams.Add(new SqlParameter("@AccountCode", Chartofacc));
                    AccountCodeFilter.Append(" and  (GL_codecombinationDetail.CodeCombinationID = '" + Chartofacc + "' ) ");

                }
                else if (ChartofaccTo != "")
                {
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", ChartofaccTo));
                    AccountCodeFilter.Append(" and  (GL_codecombinationDetail.CodeCombinationID = '" + ChartofaccTo + "' ) ");
                }

                if (BrokerID.HasValue)
                {
                    generalLedgerFilter.Append(" and AR_Set_Customer.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                    paymentFilter.Append(" and AR_Set_Customer.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                    purchaseInvoiceSupplierFilter.Append(" And AR_InvoiceSupplierDetail.SupplierID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                    SaleInvoiceFilter.Append(" and TABInvoice.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                    OnlyCustomerfilter.Append(" and AR_Set_Customer.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                }


                if (DocumentTypeID.HasValue)
                {
                    sqlParams.Add(new SqlParameter("@DocumentTypeID", DocumentTypeID));
                }
                else
                {
                    sqlParams.Add(new SqlParameter("@DocumentTypeID", DBNull.Value));
                }
                if (LocationID > 0)
                    LocationFillter.Append("and GL_TransactionHeader.LocationID = " + LocationID + "");

                if (CustomerCategoryID.HasValue)
                {
                    sbCC.Append(" left join AR_SET_Customer as CusCat on CusCat.CustomerID = tab.CustomerID ");
                    sbCC2.Append(" where CusCat.CustomerCategoryID = " + CustomerCategoryID.ToString() + "");
                }

                if (Comments != "" && CustomerCategoryID.HasValue)
                {
                    CommentsFilter.Append(" and isnull(tab.Comments,'') like '%" + Comments.ToString() + "%' ");
                }
                else if (Comments != "")
                {
                    CommentsFilter.Append(" where isnull(tab.Comments,'') like '%" + Comments.ToString() + "%' ");
                }

                if (CostCenterID > 0)
                {
                    CostCenterFillter.Append(" AND Set_CostCenter.CostCenterID = " + CostCenterID + " ");
                }

                #region Document Group new group
                if (documentGroupID != "")
                    documentGroupFilter.Append(" AND Set_DocumentType.GroupID in " + documentGroupID);
                #endregion


                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                {
                    businessPartnerTypeFilter = " AND AR_SET_BusinessPartnerTypes.TypeCode in (" + businessPartnerType + ") ";
                }

                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                {
                    businessPartnerTypeFilter2 = " AND TABInvoice.TypeCode in (" + businessPartnerType + ") ";
                }



                if (EmployeeStatus.HasValue)
                {

                    generalLedgerFilter.Append(" and AR_SET_Customer.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ") ");
                    paymentFilter.Append(" and AR_SET_Customer.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ")");
                    purchaseInvoiceSupplierFilter.Append(" And AR_InvoiceSupplierDetail.SupplierID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ") ");
                    SaleInvoiceFilter.Append(" and TABInvoice.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ") ");
                    OnlyCustomerfilter.Append(" and AR_Set_Customer.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ")");
                }

                if (!string.IsNullOrEmpty(SheetNo) && !string.IsNullOrEmpty(FAX))
                {
                    SheetNo_FAXNo_Filter.Append("WHERE FINTAB.SheetNo LIKE '%" + SheetNo + "%'");
                    SheetNo_FAXNo_Filter.Append(" AND FINTAB.FAX LIKE '%" + FAX + "%'");
                }
                else if (!string.IsNullOrEmpty(SheetNo))
                {
                    SheetNo_FAXNo_Filter.Append("WHERE FINTAB.SheetNo LIKE '%" + SheetNo + "%'");
                }
                else if (!string.IsNullOrEmpty(FAX))
                {
                    SheetNo_FAXNo_Filter.Append("WHERE FINTAB.FAX LIKE '%" + FAX + "%'");
                }

                string Query = string.Format(FnI.QueryResourceLibrary.GL.GL_TransactionHeader4.Query_BusinessPartnerGLDetail_ForDKSM,
                    generalLedgerFilter.ToString(), 
                    paymentFilter.ToString(), 
                    purchaseInvoiceSupplierFilter.ToString(), 
                    SaleInvoiceFilter.ToString(), 
                    OnlyCustomerfilter.ToString(), 
                    LocationFillter.ToString(), 
                    documentGroupFilter.ToString(), 
                    sbCC.ToString(), 
                    sbCC2.ToString(), 
                    AccountCodeFilter.ToString(), 
                    CommentsFilter.ToString(), 
                    CostCenterFillter.ToString(), 
                    businessPartnerTypeFilter.ToString(), 
                    SheetNo_FAXNo_Filter.ToString(),
                    //14
                    qry_SalesInvoiceFilter,
                    qry_ReceiptFilter,
                    qry_PurchaseInvoiceFilter,
                    qry_PaymentFilter
                    ).Replace("@CompanyID", companyID);


                if (DocumentTypeID.HasValue)
                    Query = Query.Replace("@DocumentTypeID", DocumentTypeID.ToString());
                else
                    Query = Query.Replace("@DocumentTypeID", "null");



                if (businessPartnerID.HasValue)
                    Query = Query.Replace("@BusinessPartnerID", businessPartnerID.ToString());
                else
                    Query = Query.Replace("@BusinessPartnerID", "null");


                Query = Query.Replace("@PeriodID", periodID.ToString());
                Query = Query.Replace("@CurrentFiscalID", CurrentFiscalID.ToString());

                Query = Query.Replace("@FromDate", "'" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                Query = Query.Replace("@ToDate", "'" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");


                if (Chartofacc != "" && ChartofaccTo != "")
                {
                    Query = Query.Replace("@AccountCodeTo", "'" + ChartofaccTo + "'");
                    Query = Query.Replace("@AccountCode", "'" + Chartofacc + "'");

                }
                else
                {
                    Query = Query.Replace("@AccountCodeTo", "null");
                    Query = Query.Replace("@AccountCode", "null");

                }


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlParams.ToArray());


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

        public static DataTable Get_BusinessPartnerGL_ReportData_ForDKSM_2(string companyID, int periodID, Nullable<int> businessPartnerID, Nullable<DateTime> dateFrom, Nullable<int> DocumentTypeID, string Chartofacc, string ChartofaccTo,
            string businessPartnerType, Nullable<DateTime> dateTo, int CurrentFiscalID, int LocationID = 0, int CostCenterID = 0, string documentGroupID = "", string Comments = "", int? CustomerCategoryID = null, int? BrokerID = null, int? EmployeeStatus = null, string SheetNo = null, string FAX = null)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            StringBuilder generalLedgerFilter = new StringBuilder();
            StringBuilder paymentFilter = new StringBuilder();
            StringBuilder purchaseInvoiceSupplierFilter = new StringBuilder();
            StringBuilder SaleInvoiceFilter = new StringBuilder();
            StringBuilder OnlyCustomerfilter = new StringBuilder();
            StringBuilder LocationFillter = new StringBuilder();
            StringBuilder CostCenterFillter = new StringBuilder();
            StringBuilder documentGroupFilter = new StringBuilder();
            DataTable dtToReturn = null;
            StringBuilder sbCC = new StringBuilder();
            StringBuilder sbCC2 = new StringBuilder();

            StringBuilder AccountCodeFilter = new StringBuilder();
            StringBuilder CommentsFilter = new StringBuilder();
            StringBuilder SheetNo_FAXNo_Filter = new StringBuilder();
            string businessPartnerTypeFilter = string.Empty, businessPartnerTypeFilter2 = string.Empty;

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));
                sqlParams.Add(new SqlParameter("@FromDate", ((DateTime)dateFrom).ToString("yyyy-MM-dd")));
                sqlParams.Add(new SqlParameter("@ToDate", ((DateTime)dateTo).ToString("yyyy-MM-dd")));

                companyID = "(" + companyID + ")";

                if (businessPartnerID.HasValue)
                {
                    generalLedgerFilter.Append(" and AR_Set_Customer.CustomerID = " + businessPartnerID + " ");
                    paymentFilter.Append(" and AR_Set_Customer.CustomerID = " + businessPartnerID + " ");
                    purchaseInvoiceSupplierFilter.Append(" And AR_InvoiceSupplierDetail.SupplierID = " + businessPartnerID + " ");
                    SaleInvoiceFilter.Append(" and TABInvoice.CustomerID = " + businessPartnerID + " ");
                    OnlyCustomerfilter.Append(" and AR_Set_Customer.CustomerID = " + businessPartnerID + "");
                    sqlParams.Add(new SqlParameter("@BusinessPartnerID", businessPartnerID));

                }
                else
                    sqlParams.Add(new SqlParameter("@BusinessPartnerID", DBNull.Value));

                if (Chartofacc != "" && ChartofaccTo != "")
                {
                    sqlParams.Add(new SqlParameter("@AccountCode", Chartofacc));
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", ChartofaccTo));
                    AccountCodeFilter.Append(" and  (GL_codecombinationDetail.CodeCombinationID between '" + Chartofacc + "' and '" + ChartofaccTo + "') ");
                }
                else if (Chartofacc != "")
                {
                    sqlParams.Add(new SqlParameter("@AccountCode", Chartofacc));
                    AccountCodeFilter.Append(" and  (GL_codecombinationDetail.CodeCombinationID = '" + Chartofacc + "' ) ");

                }
                else if (ChartofaccTo != "")
                {
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", ChartofaccTo));
                    AccountCodeFilter.Append(" and  (GL_codecombinationDetail.CodeCombinationID = '" + ChartofaccTo + "' ) ");
                }

                if (BrokerID.HasValue)
                {
                    generalLedgerFilter.Append(" and AR_Set_Customer.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                    paymentFilter.Append(" and AR_Set_Customer.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                    purchaseInvoiceSupplierFilter.Append(" And AR_InvoiceSupplierDetail.SupplierID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                    SaleInvoiceFilter.Append(" and TABInvoice.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                    OnlyCustomerfilter.Append(" and AR_Set_Customer.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                }


                if (DocumentTypeID.HasValue)
                {
                    sqlParams.Add(new SqlParameter("@DocumentTypeID", DocumentTypeID));
                }
                else
                {
                    sqlParams.Add(new SqlParameter("@DocumentTypeID", DBNull.Value));
                }
                if (LocationID > 0)
                    LocationFillter.Append("and GL_TransactionHeader.LocationID = " + LocationID + "");

                if (CustomerCategoryID.HasValue)
                {
                    sbCC.Append(" left join AR_SET_Customer as CusCat on CusCat.CustomerID = tab.CustomerID ");
                    sbCC2.Append(" where CusCat.CustomerCategoryID = " + CustomerCategoryID.ToString() + "");
                }

                if (Comments != "" && CustomerCategoryID.HasValue)
                {
                    CommentsFilter.Append(" and isnull(tab.Comments,'') like '%" + Comments.ToString() + "%' ");
                }
                else if (Comments != "")
                {
                    CommentsFilter.Append(" where isnull(tab.Comments,'') like '%" + Comments.ToString() + "%' ");
                }

                if (CostCenterID > 0)
                {
                    CostCenterFillter.Append(" AND Set_CostCenter.CostCenterID = " + CostCenterID + " ");
                }

                #region Document Group new group
                if (documentGroupID != "")
                    documentGroupFilter.Append(" AND Set_DocumentType.GroupID in " + documentGroupID);
                #endregion


                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                {
                    businessPartnerTypeFilter = " AND AR_SET_BusinessPartnerTypes.TypeCode in (" + businessPartnerType + ") ";
                }

                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                {
                    businessPartnerTypeFilter2 = " AND TABInvoice.TypeCode in (" + businessPartnerType + ") ";
                }



                if (EmployeeStatus.HasValue)
                {

                    generalLedgerFilter.Append(" and AR_SET_Customer.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ") ");
                    paymentFilter.Append(" and AR_SET_Customer.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ")");
                    purchaseInvoiceSupplierFilter.Append(" And AR_InvoiceSupplierDetail.SupplierID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ") ");
                    SaleInvoiceFilter.Append(" and TABInvoice.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ") ");
                    OnlyCustomerfilter.Append(" and AR_Set_Customer.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ")");
                }

                if (!string.IsNullOrEmpty(SheetNo) && !string.IsNullOrEmpty(FAX))
                {
                    SheetNo_FAXNo_Filter.Append("WHERE FINTAB.SheetNo LIKE '%" + SheetNo + "%'");
                    SheetNo_FAXNo_Filter.Append(" AND FINTAB.FAX LIKE '%" + FAX + "%'");
                }
                else if (!string.IsNullOrEmpty(SheetNo))
                {
                    SheetNo_FAXNo_Filter.Append("WHERE FINTAB.SheetNo LIKE '%" + SheetNo + "%'");
                }
                else if (!string.IsNullOrEmpty(FAX))
                {
                    SheetNo_FAXNo_Filter.Append("WHERE FINTAB.FAX LIKE '%" + FAX + "%'");
                }

                string Query = string.Format(FnI.QueryResourceLibrary.GL.GL_TransactionHeader4.Query_BusinessPartnerGLDetail_ForDKSM_2,
                    generalLedgerFilter.ToString(), paymentFilter.ToString(), purchaseInvoiceSupplierFilter.ToString(), SaleInvoiceFilter.ToString(), OnlyCustomerfilter.ToString(), LocationFillter.ToString(), documentGroupFilter.ToString(), sbCC.ToString(), sbCC2.ToString(), AccountCodeFilter.ToString(), CommentsFilter.ToString(), CostCenterFillter.ToString(), businessPartnerTypeFilter.ToString(), SheetNo_FAXNo_Filter.ToString()).Replace("@CompanyID", companyID);


                if (DocumentTypeID.HasValue)
                    Query = Query.Replace("@DocumentTypeID", DocumentTypeID.ToString());
                else
                    Query = Query.Replace("@DocumentTypeID", "null");



                if (businessPartnerID.HasValue)
                    Query = Query.Replace("@BusinessPartnerID", businessPartnerID.ToString());
                else
                    Query = Query.Replace("@BusinessPartnerID", "null");


                Query = Query.Replace("@PeriodID", periodID.ToString());
                Query = Query.Replace("@CurrentFiscalID", CurrentFiscalID.ToString());

                Query = Query.Replace("@FromDate", "'" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                Query = Query.Replace("@ToDate", "'" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");


                if (Chartofacc != "" && ChartofaccTo != "")
                {
                    Query = Query.Replace("@AccountCodeTo", "'" + ChartofaccTo + "'");
                    Query = Query.Replace("@AccountCode", "'" + Chartofacc + "'");

                }
                else
                {
                    Query = Query.Replace("@AccountCodeTo", "null");
                    Query = Query.Replace("@AccountCode", "null");

                }


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlParams.ToArray());


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

        public static DataTable Get_CustomerLedger_ReportData(string companyID, int periodID, Nullable<int> businessPartnerID, Nullable<DateTime> dateFrom, Nullable<int> DocumentTypeID, string Chartofacc, string ChartofaccTo,
            string businessPartnerType, Nullable<DateTime> dateTo, int CurrentFiscalID, int LocationID = 0, int CostCenterID = 0, string documentGroupID = "", string Comments = "", int? CustomerCategoryID = null, int? BrokerID = null, int? EmployeeStatus = null)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            StringBuilder generalLedgerFilter = new StringBuilder();
            StringBuilder paymentFilter = new StringBuilder();
            StringBuilder purchaseInvoiceSupplierFilter = new StringBuilder();
            StringBuilder SaleInvoiceFilter = new StringBuilder();
            StringBuilder OnlyCustomerfilter = new StringBuilder();
            StringBuilder LocationFillter = new StringBuilder();
            StringBuilder CostCenterFillter = new StringBuilder();
            StringBuilder documentGroupFilter = new StringBuilder();
            DataTable dtToReturn = null;
            StringBuilder sbCC = new StringBuilder();
            StringBuilder sbCC2 = new StringBuilder();

            StringBuilder AccountCodeFilter = new StringBuilder();
            StringBuilder CommentsFilter = new StringBuilder();
            string businessPartnerTypeFilter = string.Empty, businessPartnerTypeFilter2 = string.Empty;

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));
                sqlParams.Add(new SqlParameter("@FromDate", ((DateTime)dateFrom).ToString("yyyy-MM-dd")));
                sqlParams.Add(new SqlParameter("@ToDate", ((DateTime)dateTo).ToString("yyyy-MM-dd")));

                companyID = "(" + companyID + ")";

                if (businessPartnerID.HasValue)
                {
                    generalLedgerFilter.Append(" and AR_Set_Customer.CustomerID = " + businessPartnerID + " ");
                    paymentFilter.Append(" and AR_Set_Customer.CustomerID = " + businessPartnerID + " ");
                    purchaseInvoiceSupplierFilter.Append(" And AR_InvoiceSupplierDetail.SupplierID = " + businessPartnerID + " ");
                    SaleInvoiceFilter.Append(" and TABInvoice.CustomerID = " + businessPartnerID + " ");
                    OnlyCustomerfilter.Append(" and AR_Set_Customer.CustomerID = " + businessPartnerID + "");
                    sqlParams.Add(new SqlParameter("@BusinessPartnerID", businessPartnerID));

                }
                else
                    sqlParams.Add(new SqlParameter("@BusinessPartnerID", DBNull.Value));

                if (Chartofacc != "" && ChartofaccTo != "")
                {
                    sqlParams.Add(new SqlParameter("@AccountCode", Chartofacc));
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", ChartofaccTo));
                    AccountCodeFilter.Append(" and  (GL_codecombinationDetail.CodeCombinationID between '" + Chartofacc + "' and '" + ChartofaccTo + "') ");
                }
                else if (Chartofacc != "")
                {
                    sqlParams.Add(new SqlParameter("@AccountCode", Chartofacc));
                    AccountCodeFilter.Append(" and  (GL_codecombinationDetail.CodeCombinationID = '" + Chartofacc + "' ) ");

                }
                else if (ChartofaccTo != "")
                {
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", ChartofaccTo));
                    AccountCodeFilter.Append(" and  (GL_codecombinationDetail.CodeCombinationID = '" + ChartofaccTo + "' ) ");
                }

                if (BrokerID.HasValue)
                {
                    generalLedgerFilter.Append(" and AR_Set_Customer.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                    paymentFilter.Append(" and AR_Set_Customer.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                    purchaseInvoiceSupplierFilter.Append(" And AR_InvoiceSupplierDetail.SupplierID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                    SaleInvoiceFilter.Append(" and TABInvoice.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                    OnlyCustomerfilter.Append(" and AR_Set_Customer.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                }


                if (DocumentTypeID.HasValue)
                {
                    sqlParams.Add(new SqlParameter("@DocumentTypeID", DocumentTypeID));
                }
                else
                {
                    sqlParams.Add(new SqlParameter("@DocumentTypeID", DBNull.Value));
                }
                if (LocationID > 0)
                    LocationFillter.Append("and GL_TransactionHeader.LocationID = " + LocationID + "");

                if (CustomerCategoryID.HasValue)
                {
                    sbCC.Append(" left join AR_SET_Customer as CusCat on CusCat.CustomerID = tab.CustomerID ");
                    sbCC2.Append(" where CusCat.CustomerCategoryID = " + CustomerCategoryID.ToString() + "");
                }

                if (Comments != "" && CustomerCategoryID.HasValue)
                {
                    CommentsFilter.Append(" and isnull(tab.Comments,'') like '%" + Comments.ToString() + "%' ");
                }
                else if (Comments != "")
                {
                    CommentsFilter.Append(" where isnull(tab.Comments,'') like '%" + Comments.ToString() + "%' ");
                }

                if (CostCenterID > 0)
                {
                    CostCenterFillter.Append(" AND Set_CostCenter.CostCenterID = " + CostCenterID + " ");
                }

                #region Document Group new group
                if (documentGroupID != "")
                    documentGroupFilter.Append(" AND Set_DocumentType.GroupID in " + documentGroupID);
                #endregion


                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                {
                    businessPartnerTypeFilter = " AND AR_SET_BusinessPartnerTypes.TypeCode in (" + businessPartnerType + ") ";
                }

                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                {
                    businessPartnerTypeFilter2 = " AND TABInvoice.TypeCode in (" + businessPartnerType + ") ";
                }



                if (EmployeeStatus.HasValue)
                {

                    generalLedgerFilter.Append(" and AR_SET_Customer.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ") ");
                    paymentFilter.Append(" and AR_SET_Customer.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ")");
                    purchaseInvoiceSupplierFilter.Append(" And AR_InvoiceSupplierDetail.SupplierID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ") ");
                    SaleInvoiceFilter.Append(" and TABInvoice.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ") ");
                    OnlyCustomerfilter.Append(" and AR_Set_Customer.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ")");
                }


                string Query = string.Format(FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_CustomerLedgerReport,
                    generalLedgerFilter.ToString(), paymentFilter.ToString(), purchaseInvoiceSupplierFilter.ToString(), SaleInvoiceFilter.ToString(), OnlyCustomerfilter.ToString(), LocationFillter.ToString(), documentGroupFilter.ToString(), sbCC.ToString(), sbCC2.ToString(), AccountCodeFilter.ToString(), CommentsFilter.ToString(), CostCenterFillter.ToString(), businessPartnerTypeFilter.ToString()).Replace("@CompanyID", companyID);


                if (DocumentTypeID.HasValue)
                    Query = Query.Replace("@DocumentTypeID", DocumentTypeID.ToString());
                else
                    Query = Query.Replace("@DocumentTypeID", "null");



                if (businessPartnerID.HasValue)
                    Query = Query.Replace("@BusinessPartnerID", businessPartnerID.ToString());
                else
                    Query = Query.Replace("@BusinessPartnerID", "null");


                Query = Query.Replace("@PeriodID", periodID.ToString());
                Query = Query.Replace("@CurrentFiscalID", CurrentFiscalID.ToString());

                Query = Query.Replace("@FromDate", "'" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                Query = Query.Replace("@ToDate", "'" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");


                if (Chartofacc != "" && ChartofaccTo != "")
                {
                    Query = Query.Replace("@AccountCodeTo", "'" + ChartofaccTo + "'");
                    Query = Query.Replace("@AccountCode", "'" + Chartofacc + "'");

                }
                else
                {
                    Query = Query.Replace("@AccountCodeTo", "null");
                    Query = Query.Replace("@AccountCode", "null");

                }


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlParams.ToArray());


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

        public static DataTable Get_BusinessPartnerGLDetailWithOrWithOutTaxData(string companyID, int periodID, Nullable<int> businessPartnerID, Nullable<DateTime> dateFrom, Nullable<int> DocumentTypeID, string Chartofacc, string ChartofaccTo,
            string businessPartnerType, Nullable<DateTime> dateTo, int CurrentFiscalID, int LocationID = 0, int CostCenterID = 0, int? taxid = 0, string documentGroupID = "", string Comments = "", int? CustomerCategoryID = null, int? BrokerID = null, int? EmployeeStatus = null)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            StringBuilder generalLedgerFilter = new StringBuilder();
            StringBuilder paymentFilter = new StringBuilder();
            StringBuilder purchaseInvoiceSupplierFilter = new StringBuilder();
            StringBuilder SaleInvoiceFilter = new StringBuilder();
            StringBuilder OnlyCustomerfilter = new StringBuilder();
            StringBuilder LocationFillter = new StringBuilder();
            StringBuilder CostCenterFillter = new StringBuilder();
            StringBuilder documentGroupFilter = new StringBuilder();
            DataTable dtToReturn = null;
            StringBuilder sbCC = new StringBuilder();
            StringBuilder sbCC2 = new StringBuilder();

            StringBuilder AccountCodeFilter = new StringBuilder();
            StringBuilder CommentsFilter = new StringBuilder();
            StringBuilder TaxFilter = new StringBuilder();

            string businessPartnerTypeFilter = string.Empty, businessPartnerTypeFilter2 = string.Empty;

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));
                sqlParams.Add(new SqlParameter("@FromDate", ((DateTime)dateFrom).ToString("yyyy-MM-dd")));
                sqlParams.Add(new SqlParameter("@ToDate", ((DateTime)dateTo).ToString("yyyy-MM-dd")));

                companyID = "(" + companyID + ")";

                if (businessPartnerID.HasValue)
                {
                    generalLedgerFilter.Append(" and AR_Set_Customer.CustomerID = " + businessPartnerID + " ");
                    paymentFilter.Append(" and AR_Set_Customer.CustomerID = " + businessPartnerID + " ");
                    purchaseInvoiceSupplierFilter.Append(" And AR_InvoiceSupplierDetail.SupplierID = " + businessPartnerID + " ");
                    SaleInvoiceFilter.Append(" and TABInvoice.CustomerID = " + businessPartnerID + " ");
                    OnlyCustomerfilter.Append(" and AR_Set_Customer.CustomerID = " + businessPartnerID + "");
                    sqlParams.Add(new SqlParameter("@BusinessPartnerID", businessPartnerID));

                }
                else
                    sqlParams.Add(new SqlParameter("@BusinessPartnerID", DBNull.Value));

                if (Chartofacc != "" && ChartofaccTo != "")
                {
                    sqlParams.Add(new SqlParameter("@AccountCode", Chartofacc));
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", ChartofaccTo));
                    AccountCodeFilter.Append(" and  (GL_codecombinationDetail.CodeCombinationID between '" + Chartofacc + "' and '" + ChartofaccTo + "') ");
                }
                else if (Chartofacc != "")
                {
                    sqlParams.Add(new SqlParameter("@AccountCode", Chartofacc));
                    AccountCodeFilter.Append(" and  (GL_codecombinationDetail.CodeCombinationID = '" + Chartofacc + "' ) ");

                }
                else if (ChartofaccTo != "")
                {
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", ChartofaccTo));
                    AccountCodeFilter.Append(" and  (GL_codecombinationDetail.CodeCombinationID = '" + ChartofaccTo + "' ) ");
                }

                if (BrokerID.HasValue)
                {
                    generalLedgerFilter.Append(" and AR_Set_Customer.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                    paymentFilter.Append(" and AR_Set_Customer.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                    purchaseInvoiceSupplierFilter.Append(" And AR_InvoiceSupplierDetail.SupplierID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                    SaleInvoiceFilter.Append(" and TABInvoice.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                    OnlyCustomerfilter.Append(" and AR_Set_Customer.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                }


                if (DocumentTypeID.HasValue)
                {
                    sqlParams.Add(new SqlParameter("@DocumentTypeID", DocumentTypeID));
                }
                else
                {
                    sqlParams.Add(new SqlParameter("@DocumentTypeID", DBNull.Value));
                }
                if (LocationID > 0)
                    LocationFillter.Append("and GL_TransactionHeader.LocationID = " + LocationID + "");

                if (CustomerCategoryID.HasValue)
                {
                    sbCC.Append(" left join AR_SET_Customer as CusCat on CusCat.CustomerID = tab.CustomerID ");
                    sbCC2.Append(" where CusCat.CustomerCategoryID = " + CustomerCategoryID.ToString() + "");
                }

                if (Comments != "" && CustomerCategoryID.HasValue)
                {
                    CommentsFilter.Append(" and isnull(tab.Comments,'') like '%" + Comments.ToString() + "%' ");
                }
                else if (Comments != "")
                {
                    CommentsFilter.Append(" where isnull(tab.Comments,'') like '%" + Comments.ToString() + "%' ");
                }

                if (CostCenterID > 0)
                {
                    CostCenterFillter.Append(" AND Set_CostCenter.CostCenterID = " + CostCenterID + " ");
                }

                #region Document Group new group
                if (documentGroupID != "")
                    documentGroupFilter.Append(" AND Set_DocumentType.GroupID in " + documentGroupID);
                #endregion


                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                {
                    businessPartnerTypeFilter = " AND AR_SET_BusinessPartnerTypes.TypeCode in (" + businessPartnerType + ") ";
                }

                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                {
                    businessPartnerTypeFilter2 = " AND TABInvoice.TypeCode in (" + businessPartnerType + ") ";
                }



                if (EmployeeStatus.HasValue)
                {

                    generalLedgerFilter.Append(" and AR_SET_Customer.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ") ");
                    paymentFilter.Append(" and AR_SET_Customer.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ")");
                    purchaseInvoiceSupplierFilter.Append(" And AR_InvoiceSupplierDetail.SupplierID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ") ");
                    SaleInvoiceFilter.Append(" and TABInvoice.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ") ");
                    OnlyCustomerfilter.Append(" and AR_Set_Customer.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ")");
                }

                if (taxid > 0)
                {
                    if (taxid == 1)
                    {
                        TaxFilter.Append(" where Finaltab.TaxPercentage is null Or Finaltab.TaxPercentage = 'Select' or Finaltab.TaxPercentage = '' ");
                    }
                    else if (taxid == 2)
                    {
                        TaxFilter.Append(" where Finaltab.TaxPercentage is not null and Finaltab.TaxPercentage != ''  and Finaltab.TaxPercentage != 'Select' ");
                    }
                }

                string Query = string.Format(FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_BusinessPartnerGLDetailWithOrWithOutTax,
                    generalLedgerFilter.ToString(), paymentFilter.ToString(), purchaseInvoiceSupplierFilter.ToString(), SaleInvoiceFilter.ToString(), OnlyCustomerfilter.ToString(), LocationFillter.ToString(), documentGroupFilter.ToString(), sbCC.ToString(), sbCC2.ToString(), AccountCodeFilter.ToString(), CommentsFilter.ToString(), CostCenterFillter.ToString(), businessPartnerTypeFilter.ToString(), TaxFilter.ToString()).Replace("@CompanyID", companyID);


                if (DocumentTypeID.HasValue)
                    Query = Query.Replace("@DocumentTypeID", DocumentTypeID.ToString());
                else
                    Query = Query.Replace("@DocumentTypeID", "null");



                if (businessPartnerID.HasValue)
                    Query = Query.Replace("@BusinessPartnerID", businessPartnerID.ToString());
                else
                    Query = Query.Replace("@BusinessPartnerID", "null");


                Query = Query.Replace("@PeriodID", periodID.ToString());
                Query = Query.Replace("@CurrentFiscalID", CurrentFiscalID.ToString());

                Query = Query.Replace("@FromDate", "'" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                Query = Query.Replace("@ToDate", "'" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");


                if (Chartofacc != "" && ChartofaccTo != "")
                {
                    Query = Query.Replace("@AccountCodeTo", "'" + ChartofaccTo + "'");
                    Query = Query.Replace("@AccountCode", "'" + Chartofacc + "'");

                }
                else
                {
                    Query = Query.Replace("@AccountCodeTo", "null");
                    Query = Query.Replace("@AccountCode", "null");

                }


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlParams.ToArray());


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

        public static DataTable Get_BusinessPartnerGLDetailWithOrWithOutTaxDataNew(string companyID, int periodID, Nullable<int> businessPartnerID, Nullable<DateTime> dateFrom, Nullable<int> DocumentTypeID, string Chartofacc, string ChartofaccTo,
           string businessPartnerType, Nullable<DateTime> dateTo, int CurrentFiscalID, int LocationID = 0, int CostCenterID = 0, int? taxid = 0, string documentGroupID = "", string Comments = "", int? CustomerCategoryID = null, int? BrokerID = null, int? EmployeeStatus = null)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            StringBuilder generalLedgerFilter = new StringBuilder();
            StringBuilder paymentFilter = new StringBuilder();
            StringBuilder purchaseInvoiceSupplierFilter = new StringBuilder();
            StringBuilder SaleInvoiceFilter = new StringBuilder();
            StringBuilder OnlyCustomerfilter = new StringBuilder();
            StringBuilder LocationFillter = new StringBuilder();
            StringBuilder CostCenterFillter = new StringBuilder();
            StringBuilder documentGroupFilter = new StringBuilder();
            DataTable dtToReturn = null;
            StringBuilder sbCC = new StringBuilder();
            StringBuilder sbCC2 = new StringBuilder();
            StringBuilder AccountCodeFilter = new StringBuilder();
            StringBuilder CommentsFilter = new StringBuilder();
            StringBuilder TaxFilter = new StringBuilder();
            StringBuilder TaxFilterNew = new StringBuilder();
            string businessPartnerTypeFilter = string.Empty, businessPartnerTypeFilter2 = string.Empty;

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));
                sqlParams.Add(new SqlParameter("@FromDate", ((DateTime)dateFrom).ToString("yyyy-MM-dd")));
                sqlParams.Add(new SqlParameter("@ToDate", ((DateTime)dateTo).ToString("yyyy-MM-dd")));

                companyID = "(" + companyID + ")";

                if (businessPartnerID.HasValue)
                {
                    generalLedgerFilter.Append(" and AR_Set_Customer.CustomerID = " + businessPartnerID + " ");
                    paymentFilter.Append(" and AR_Set_Customer.CustomerID = " + businessPartnerID + " ");
                    purchaseInvoiceSupplierFilter.Append(" And AR_InvoiceSupplierDetail.SupplierID = " + businessPartnerID + " ");
                    SaleInvoiceFilter.Append(" and TABInvoice.CustomerID = " + businessPartnerID + " ");
                    OnlyCustomerfilter.Append(" and AR_Set_Customer.CustomerID = " + businessPartnerID + "");
                    sqlParams.Add(new SqlParameter("@BusinessPartnerID", businessPartnerID));

                }
                else
                    sqlParams.Add(new SqlParameter("@BusinessPartnerID", DBNull.Value));

                if (Chartofacc != "" && ChartofaccTo != "")
                {
                    sqlParams.Add(new SqlParameter("@AccountCode", Chartofacc));
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", ChartofaccTo));
                    AccountCodeFilter.Append(" and  (GL_codecombinationDetail.CodeCombinationID between '" + Chartofacc + "' and '" + ChartofaccTo + "') ");
                }
                else if (Chartofacc != "")
                {
                    sqlParams.Add(new SqlParameter("@AccountCode", Chartofacc));
                    AccountCodeFilter.Append(" and  (GL_codecombinationDetail.CodeCombinationID = '" + Chartofacc + "' ) ");

                }
                else if (ChartofaccTo != "")
                {
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", ChartofaccTo));
                    AccountCodeFilter.Append(" and  (GL_codecombinationDetail.CodeCombinationID = '" + ChartofaccTo + "' ) ");
                }

                if (BrokerID.HasValue)
                {
                    generalLedgerFilter.Append(" and AR_Set_Customer.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                    paymentFilter.Append(" and AR_Set_Customer.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                    purchaseInvoiceSupplierFilter.Append(" And AR_InvoiceSupplierDetail.SupplierID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                    SaleInvoiceFilter.Append(" and TABInvoice.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                    OnlyCustomerfilter.Append(" and AR_Set_Customer.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                }


                if (DocumentTypeID.HasValue)
                {
                    sqlParams.Add(new SqlParameter("@DocumentTypeID", DocumentTypeID));
                }
                else
                {
                    sqlParams.Add(new SqlParameter("@DocumentTypeID", DBNull.Value));
                }
                if (LocationID > 0)
                    LocationFillter.Append("and GL_TransactionHeader.LocationID = " + LocationID + "");

                if (CustomerCategoryID.HasValue)
                {
                    sbCC.Append(" left join AR_SET_Customer as CusCat on CusCat.CustomerID = tab.CustomerID ");
                    sbCC2.Append(" where CusCat.CustomerCategoryID = " + CustomerCategoryID.ToString() + "");
                }

                if (Comments != "" && CustomerCategoryID.HasValue)
                {
                    CommentsFilter.Append(" and isnull(tab.Comments,'') like '%" + Comments.ToString() + "%' ");
                }
                else if (Comments != "")
                {
                    CommentsFilter.Append(" where isnull(tab.Comments,'') like '%" + Comments.ToString() + "%' ");
                }

                if (CostCenterID > 0)
                {
                    CostCenterFillter.Append(" AND Set_CostCenter.CostCenterID = " + CostCenterID + " ");
                }

                #region Document Group new group

                if (documentGroupID != "")
                    documentGroupFilter.Append(" AND Set_DocumentType.GroupID in " + documentGroupID);

                #endregion


                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                {
                    businessPartnerTypeFilter = " AND AR_SET_BusinessPartnerTypes.TypeCode in (" + businessPartnerType + ") ";
                }

                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                {
                    businessPartnerTypeFilter2 = " AND TABInvoice.TypeCode in (" + businessPartnerType + ") ";
                }



                if (EmployeeStatus.HasValue)
                {
                    generalLedgerFilter.Append(" and AR_SET_Customer.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ") ");
                    paymentFilter.Append(" and AR_SET_Customer.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ")");
                    purchaseInvoiceSupplierFilter.Append(" And AR_InvoiceSupplierDetail.SupplierID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ") ");
                    SaleInvoiceFilter.Append(" and TABInvoice.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ") ");
                    OnlyCustomerfilter.Append(" and AR_Set_Customer.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ")");
                }

                if (taxid > 0)
                {
                    if (taxid == 1)
                    {
                        TaxFilter.Append(" where Finaltab.TaxPercentage is null Or Finaltab.TaxPercentage = 'Select' or Finaltab.TaxPercentage = '' ");
                        TaxFilterNew.Append(" AND AP_PaymentSupplierDetail.BusinessPartnerTypeID not in (51) ");
                    }
                    else if (taxid == 2)
                    {
                        TaxFilter.Append(" where Finaltab.TaxPercentage is not null and Finaltab.TaxPercentage != ''  and Finaltab.TaxPercentage != 'Select' ");
                    }
                }

                string Query = string.Format(FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_BusinessPartnerGLDetailWithOrWithOutTaxNew,
                    generalLedgerFilter.ToString(), paymentFilter.ToString(), purchaseInvoiceSupplierFilter.ToString(), SaleInvoiceFilter.ToString(), OnlyCustomerfilter.ToString(), LocationFillter.ToString(), documentGroupFilter.ToString(), sbCC.ToString(), sbCC2.ToString(), AccountCodeFilter.ToString(), CommentsFilter.ToString(), CostCenterFillter.ToString(), businessPartnerTypeFilter.ToString(), TaxFilter.ToString(), TaxFilterNew.ToString()).Replace("@CompanyID", companyID);


                if (DocumentTypeID.HasValue)
                    Query = Query.Replace("@DocumentTypeID", DocumentTypeID.ToString());
                else
                    Query = Query.Replace("@DocumentTypeID", "null");



                if (businessPartnerID.HasValue)
                    Query = Query.Replace("@BusinessPartnerID", businessPartnerID.ToString());
                else
                    Query = Query.Replace("@BusinessPartnerID", "null");


                Query = Query.Replace("@PeriodID", periodID.ToString());
                Query = Query.Replace("@CurrentFiscalID", CurrentFiscalID.ToString());

                Query = Query.Replace("@FromDate", "'" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                Query = Query.Replace("@ToDate", "'" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");


                if (Chartofacc != "" && ChartofaccTo != "")
                {
                    Query = Query.Replace("@AccountCodeTo", "'" + ChartofaccTo + "'");
                    Query = Query.Replace("@AccountCode", "'" + Chartofacc + "'");

                }
                else
                {
                    Query = Query.Replace("@AccountCodeTo", "null");
                    Query = Query.Replace("@AccountCode", "null");

                }


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlParams.ToArray());


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

        public static DataTable Get_BusinessPartnerGL_ReportData_SKU(string companyID, int periodID, Nullable<int> businessPartnerID, Nullable<DateTime> dateFrom, Nullable<int> DocumentTypeID, string Chartofacc, string ChartofaccTo,
           string businessPartnerType, Nullable<DateTime> dateTo, int CurrentFiscalID, int LocationID = 0, int CostCenterID = 0, string documentGroupID = "", string Comments = "", int? CustomerCategoryID = null, int? BrokerID = null, int? EmployeeStatus = null)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            StringBuilder generalLedgerFilter = new StringBuilder();
            StringBuilder paymentFilter = new StringBuilder();
            StringBuilder purchaseInvoiceSupplierFilter = new StringBuilder();
            StringBuilder SaleInvoiceFilter = new StringBuilder();
            StringBuilder OnlyCustomerfilter = new StringBuilder();
            StringBuilder LocationFillter = new StringBuilder();
            StringBuilder CostCenterFillter = new StringBuilder();
            StringBuilder documentGroupFilter = new StringBuilder();
            DataTable dtToReturn = null;
            StringBuilder sbCC = new StringBuilder();
            StringBuilder sbCC2 = new StringBuilder();

            StringBuilder AccountCodeFilter = new StringBuilder();
            StringBuilder CommentsFilter = new StringBuilder();
            string businessPartnerTypeFilter = string.Empty, businessPartnerTypeFilter2 = string.Empty;

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));
                sqlParams.Add(new SqlParameter("@FromDate", ((DateTime)dateFrom).ToString("yyyy-MM-dd")));
                sqlParams.Add(new SqlParameter("@ToDate", ((DateTime)dateTo).ToString("yyyy-MM-dd")));

                companyID = "(" + companyID + ")";

                if (businessPartnerID.HasValue)
                {
                    generalLedgerFilter.Append(" and AR_Set_Customer.CustomerID = " + businessPartnerID + " ");
                    paymentFilter.Append(" and AR_Set_Customer.CustomerID = " + businessPartnerID + " ");
                    purchaseInvoiceSupplierFilter.Append(" And AR_InvoiceSupplierDetail.SupplierID = " + businessPartnerID + " ");
                    SaleInvoiceFilter.Append(" and TABInvoice.CustomerID = " + businessPartnerID + " ");
                    OnlyCustomerfilter.Append(" and AR_Set_Customer.CustomerID = " + businessPartnerID + "");
                    sqlParams.Add(new SqlParameter("@BusinessPartnerID", businessPartnerID));

                }
                else
                    sqlParams.Add(new SqlParameter("@BusinessPartnerID", DBNull.Value));

                if (Chartofacc != "" && ChartofaccTo != "")
                {
                    sqlParams.Add(new SqlParameter("@AccountCode", Chartofacc));
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", ChartofaccTo));
                    AccountCodeFilter.Append(" and  (GL_codecombinationDetail.CodeCombinationID between '" + Chartofacc + "' and '" + ChartofaccTo + "') ");
                }
                else if (Chartofacc != "")
                {
                    sqlParams.Add(new SqlParameter("@AccountCode", Chartofacc));
                    AccountCodeFilter.Append(" and  (GL_codecombinationDetail.CodeCombinationID = '" + Chartofacc + "' ) ");

                }
                else if (ChartofaccTo != "")
                {
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", ChartofaccTo));
                    AccountCodeFilter.Append(" and  (GL_codecombinationDetail.CodeCombinationID = '" + ChartofaccTo + "' ) ");
                }

                if (BrokerID.HasValue)
                {
                    generalLedgerFilter.Append(" and AR_Set_Customer.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                    paymentFilter.Append(" and AR_Set_Customer.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                    purchaseInvoiceSupplierFilter.Append(" And AR_InvoiceSupplierDetail.SupplierID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                    SaleInvoiceFilter.Append(" and TABInvoice.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                    OnlyCustomerfilter.Append(" and AR_Set_Customer.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                }


                if (DocumentTypeID.HasValue)
                {
                    sqlParams.Add(new SqlParameter("@DocumentTypeID", DocumentTypeID));
                }
                else
                {
                    sqlParams.Add(new SqlParameter("@DocumentTypeID", DBNull.Value));
                }
                if (LocationID > 0)
                    LocationFillter.Append("and GL_TransactionHeader.LocationID = " + LocationID + "");

                if (CustomerCategoryID.HasValue)
                {
                    sbCC.Append(" left join AR_SET_Customer as CusCat on CusCat.CustomerID = tab.CustomerID ");
                    sbCC2.Append(" where CusCat.CustomerCategoryID = " + CustomerCategoryID.ToString() + "");
                }

                if (Comments != "" && CustomerCategoryID.HasValue)
                {
                    CommentsFilter.Append(" and isnull(tab.Comments,'') like '%" + Comments.ToString() + "%' ");
                }
                else if (Comments != "")
                {
                    CommentsFilter.Append(" where isnull(tab.Comments,'') like '%" + Comments.ToString() + "%' ");
                }

                if (CostCenterID > 0)
                {
                    CostCenterFillter.Append(" AND Set_CostCenter.CostCenterID = " + CostCenterID + " ");
                }

                #region Document Group new group
                if (documentGroupID != "")
                    documentGroupFilter.Append(" AND Set_DocumentType.GroupID in " + documentGroupID);
                #endregion


                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                {
                    businessPartnerTypeFilter = " AND AR_SET_BusinessPartnerTypes.TypeCode in (" + businessPartnerType + ") ";
                }

                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                {
                    businessPartnerTypeFilter2 = " AND TABInvoice.TypeCode in (" + businessPartnerType + ") ";
                }



                if (EmployeeStatus.HasValue)
                {

                    generalLedgerFilter.Append(" and AR_SET_Customer.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ") ");
                    paymentFilter.Append(" and AR_SET_Customer.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ")");
                    purchaseInvoiceSupplierFilter.Append(" And AR_InvoiceSupplierDetail.SupplierID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ") ");
                    SaleInvoiceFilter.Append(" and TABInvoice.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ") ");
                    OnlyCustomerfilter.Append(" and AR_Set_Customer.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ")");
                }


                string Query = string.Format(FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_BusinessPartnerGL_ReportNew2_SKU,
                    generalLedgerFilter.ToString(), paymentFilter.ToString(), purchaseInvoiceSupplierFilter.ToString(), SaleInvoiceFilter.ToString(), OnlyCustomerfilter.ToString(), LocationFillter.ToString(), documentGroupFilter.ToString(), sbCC.ToString(), sbCC2.ToString(), AccountCodeFilter.ToString(), CommentsFilter.ToString(), CostCenterFillter.ToString(), businessPartnerTypeFilter.ToString()).Replace("@CompanyID", companyID);


                if (DocumentTypeID.HasValue)
                    Query = Query.Replace("@DocumentTypeID", DocumentTypeID.ToString());
                else
                    Query = Query.Replace("@DocumentTypeID", "null");



                if (businessPartnerID.HasValue)
                    Query = Query.Replace("@BusinessPartnerID", businessPartnerID.ToString());
                else
                    Query = Query.Replace("@BusinessPartnerID", "null");


                Query = Query.Replace("@PeriodID", periodID.ToString());
                Query = Query.Replace("@CurrentFiscalID", CurrentFiscalID.ToString());

                Query = Query.Replace("@FromDate", "'" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                Query = Query.Replace("@ToDate", "'" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");


                if (Chartofacc != "" && ChartofaccTo != "")
                {
                    Query = Query.Replace("@AccountCodeTo", "'" + ChartofaccTo + "'");
                    Query = Query.Replace("@AccountCode", "'" + Chartofacc + "'");

                }
                else
                {
                    Query = Query.Replace("@AccountCodeTo", "null");
                    Query = Query.Replace("@AccountCode", "null");

                }


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlParams.ToArray());


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


        public static DataTable Get_BusinessPartnerGL_ReportData_Fiscal(int companyID, int periodID, Nullable<int> businessPartnerID, Nullable<DateTime> dateFrom,
            Nullable<DateTime> dateTo)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            StringBuilder generalLedgerFilter = new StringBuilder();
            StringBuilder paymentFilter = new StringBuilder();
            StringBuilder purchaseInvoiceSupplierFilter = new StringBuilder();
            DataTable dtToReturn = null;

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));

                #region Date Range Filter

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    generalLedgerFilter.Append(" AND GL_TransactionHeader.DocumentDate BETWEEN @FromDate AND @ToDate ");
                    paymentFilter.Append(" AND Ap_paymentHeader.DocumentDate BETWEEN @FromDate AND @ToDate ");
                    purchaseInvoiceSupplierFilter.Append(" AND AR_InvoiceHeader.InvoiceDate BETWEEN @FromDate AND @ToDate ");
                    sqlParams.Add(new SqlParameter("@FromDate", DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00"));
                    sqlParams.Add(new SqlParameter("@ToDate", DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59"));
                }
                else if (dateFrom.HasValue)
                {
                    generalLedgerFilter.Append(" AND GL_TransactionHeader.DocumentDate >= @FromDate ");
                    paymentFilter.Append(" AND Ap_paymentHeader.DocumentDate >= @FromDate ");
                    purchaseInvoiceSupplierFilter.Append(" AND AR_InvoiceHeader.InvoiceDate >= @FromDate ");
                    sqlParams.Add(new SqlParameter("@FromDate", DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00"));
                }
                else
                {
                    generalLedgerFilter.Append(" AND GL_TransactionHeader.DocumentDate <= @ToDate ");
                    paymentFilter.Append(" AND Ap_paymentHeader.DocumentDate <= @ToDate ");
                    purchaseInvoiceSupplierFilter.Append(" AND AR_InvoiceHeader.InvoiceDate <= @ToDate ");
                    sqlParams.Add(new SqlParameter("@ToDate", DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59"));
                }

                #endregion

                if (businessPartnerID.HasValue)
                {
                    generalLedgerFilter.Append(" and AR_Set_Customer.CustomerID = @BusinessPartnerID ");
                    paymentFilter.Append(" and AR_Set_Customer.CustomerID = @BusinessPartnerID ");
                    purchaseInvoiceSupplierFilter.Append(" And AR_InvoiceSupplierDetail.SupplierID = @BusinessPartnerID ");
                    sqlParams.Add(new SqlParameter("@BusinessPartnerID", businessPartnerID));
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.GL.GL_TransactionHeader.Query_BusinessPartnerGL_Report_Fiscal,
                    generalLedgerFilter.ToString(), paymentFilter.ToString(), purchaseInvoiceSupplierFilter.ToString()), sqlParams.ToArray());

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

        public static int GetNewDocNo(int companyID, int periodID, int documentTypeID, bool status = true, SqlTransaction objTransaction = null)
        {
            int toReturn = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[1] = new SqlParameter("@Status", status);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);

                string Query_Select_New_DocNo = @" SELECT IsNull(max(DocumentNo), 0) + 1 AS MaxDocumentNo FROM GL_TransactionHeader 
WHERE DocumentTypeID = @DocumentTypeID AND Status = @Status AND CompanyID = @CompanyID AND PeriodID = @PeriodID; ";

                if (objTransaction != null)
                    toReturn = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Select_New_DocNo, sqlparams));
                else
                    toReturn = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Select_New_DocNo, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return toReturn;
        }
        public static DataTable GetDailyCashTransactionListReport(int companyID, int periodID, string accountCodeFrom, Guid userID, string dateFrom = null,
                   string dateTo = null, Nullable<short> documentGroupID = null, Nullable<short> DocumentTypeID = null, bool documentTypeAuthentication = false)
        {
            DataTable dt = null;
            string accountCodeFilter = string.Empty;
            string additionalParameters = string.Empty;
            string additionalParameters1 = string.Empty;
            string qry_docuTypeAuthenticate = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (dateFrom != "")
                {
                    additionalParameters = " AND Cast(Convert(Varchar(10),GL_TransactionHeader.DocumentDate,110) as datetime) between @FromDate and @ToDate";
                    sqlparams[2] = new SqlParameter("@FromDate", dateFrom);
                    sqlparams[3] = new SqlParameter("@ToDate", dateTo);
                }

                if (accountCodeFrom != string.Empty)
                {
                    accountCodeFilter = " And GL_TransactionHeader.TransactionID in (select TransactionID from GL_TransactionDetail where GL_TransactionDetail.CodeCombinationID =  '" + accountCodeFrom + "') ";
                }

                if (documentGroupID.HasValue)
                    additionalParameters = additionalParameters + " AND Set_DocumentType.GroupID = " + documentGroupID;
                if (DocumentTypeID.HasValue)
                    additionalParameters = additionalParameters + " AND Set_DocumentType.DocumentTypeID = " + DocumentTypeID;
                if (documentTypeAuthentication)
                {
                    sqlparams[4] = new SqlParameter("@UserID", userID);
                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.GL.GL_TransactionHeader.Query_Get_DailyCashTransactionList, additionalParameters, accountCodeFilter,
                    qry_docuTypeAuthenticate), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        public bool InsertGLopeningHeaderAndDetail(GL_TransactionHeader th, List<GL_TransactionDetail> colTranDetail, int CurrentFiscalID = 0)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InserOpening");

            try
            {

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (th.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                {


                    th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    th.Insert(sqltrans);

                #endregion

                    #region Insert In GL_TransactionDetail

                    decimal AmountDebit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                    decimal AmountCredit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                    if (AmountDebit != AmountCredit)
                        throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");


                    foreach (GL_TransactionDetail td in colTranDetail)
                    {
                        string naration = td.Narration.ToString();
                        if (naration.Contains("_") && td.TransactionType == "Credit")
                        {
                            naration = naration.Substring(0, naration.LastIndexOf('_') - 1);
                        }
                        //
                        td.TransactionID = th.TransactionID;
                        // td.SourceHeaderID = this.PaymentID;
                        td.Narration = naration;
                        td.FiscalID = CurrentFiscalID;
                        td.Insert(sqltrans);
                    }
                    #endregion
                    result = true;
                }

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
                    SqlHelper.RollBackTransaction(sqltrans, "InserOpening");
            }

            return result;
        }
        public bool InsertGLopeningHeaderAndDetailForHSM(GL_TransactionHeader th, List<GL_TransactionDetail> colTranDetail, int CurrentFiscalID = 0)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InserOpening");

            try
            {

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (th.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                {


                    th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    th.Insert(sqltrans);

                #endregion

                    #region Insert In GL_TransactionDetail

                    decimal AmountDebit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                    decimal AmountCredit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                    if (AmountDebit != AmountCredit)
                        throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");


                    foreach (GL_TransactionDetail td in colTranDetail)
                    {
                        string naration = td.Narration.ToString();
                        if (naration.Contains("_") && td.TransactionType == "Credit")
                        {
                            naration = naration.Substring(0, naration.LastIndexOf('_') - 1);
                        }
                        //
                        td.CostCenterID = td.CostCenterID;
                        td.DepartmentID = td.DepartmentID;
                        td.TransactionID = th.TransactionID;
                        // td.SourceHeaderID = this.PaymentID;
                        td.Narration = naration;
                        td.FiscalID = CurrentFiscalID;
                        td.InsertNew(sqltrans);
                    }
                    #endregion
                    result = true;
                }

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
                    SqlHelper.RollBackTransaction(sqltrans, "InserOpening");
            }

            return result;
        }
        public static DataTable Get_BusinessPartnerGL_ReportAl_imdadData(int companyID, int periodID, Nullable<int> businessPartnerID, Nullable<DateTime> dateFrom, string Chartofacc,
          Nullable<DateTime> dateTo, int CurrentFiscalID, bool discardReversedDocuments)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            StringBuilder generalLedgerFilter = new StringBuilder();
            StringBuilder paymentFilter = new StringBuilder();
            StringBuilder purchaseInvoiceSupplierFilter = new StringBuilder();
            StringBuilder SaleInvoiceFilter = new StringBuilder();
            StringBuilder OnlyCustomerfilter = new StringBuilder();

            DataTable dtToReturn = null;

            string qry_SalesInvoiceFilter = string.Empty, qry_ReceiptFilter = string.Empty, qry_PurchaseInvoiceFilter = string.Empty,
                qry_PaymentFilter = string.Empty;

            #region Filter Reversed Documents

            if (discardReversedDocuments)
            {
                qry_SalesInvoiceFilter = " And IsNull(Sal_InvoiceHeader.IsReversed, 0) = 0 And Sal_InvoiceHeader.ReversalID is null ";
                qry_ReceiptFilter = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                qry_PurchaseInvoiceFilter = " And IsNull(AR_InvoiceHeader.IsReversed, 0) = 0 And AR_InvoiceHeader.ReversalID is null ";
                qry_PaymentFilter = " And AP_PaymentHeader.ReversalComments is null ";
            }

            #endregion

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));
                sqlParams.Add(new SqlParameter("@FromDate", DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00"));
                sqlParams.Add(new SqlParameter("@ToDate", DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59"));

                if (businessPartnerID.HasValue)
                {
                    generalLedgerFilter.Append(" and AR_Set_Customer.CustomerID = @BusinessPartnerID ");
                    paymentFilter.Append(" and AR_Set_Customer.CustomerID = @BusinessPartnerID ");
                    purchaseInvoiceSupplierFilter.Append(" And AR_InvoiceSupplierDetail.SupplierID = @BusinessPartnerID ");
                    SaleInvoiceFilter.Append(" and TABInvoice.CustomerID = @BusinessPartnerID ");
                    OnlyCustomerfilter.Append(" and AR_Set_Customer.CustomerID = @BusinessPartnerID");
                    sqlParams.Add(new SqlParameter("@BusinessPartnerID", businessPartnerID));

                }

                if (Chartofacc != "")
                {
                    sqlParams.Add(new SqlParameter("@AccountCode", Chartofacc));

                }
                else
                    sqlParams.Add(new SqlParameter("@AccountCode", DBNull.Value));

                //DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.GL.GL_TransactionHeader.Query_BusinessPartnerGL_Report,
                //    generalLedgerFilter.ToString(), paymentFilter.ToString(), purchaseInvoiceSupplierFilter.ToString(), SaleInvoiceFilter.ToString(), OnlyCustomerfilter.ToString()), sqlParams.ToArray());
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.GL.GL_TransactionHeader.Query_BusinessPartnerGL_ReportAl_imdad,
                    generalLedgerFilter.ToString(), 
                    paymentFilter.ToString(), 
                    purchaseInvoiceSupplierFilter.ToString(),
                    SaleInvoiceFilter.ToString(), 
                    OnlyCustomerfilter.ToString(),
                    //5
                    qry_SalesInvoiceFilter.ToString(),
                    qry_ReceiptFilter.ToString(),
                    qry_PurchaseInvoiceFilter.ToString(),
                    qry_PaymentFilter.ToString()
                    ), sqlParams.ToArray());


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

        public static DataTable GET_BusinessPartnerTrailBalance(int companyID, int periodID, Nullable<int> customer, Nullable<int> customerto,
            Nullable<DateTime> datefrom, Nullable<DateTime> dateto, Guid userID, Nullable<int> bpType = null, string documentGroupID = "",
            bool documentTypeAuthentication = false, bool discardReversedDocuments = false, string Chartofacc = "")
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder ss = new StringBuilder();
            string qry_docuTypeAuthenticate = string.Empty;
            string qry_SalesInvoiceFilter = string.Empty, qry_ReceiptFilter = string.Empty, qry_PurchaseInvoiceFilter = string.Empty,
                qry_PaymentFilter = string.Empty,
             businessPartnerTypeFilter2 = string.Empty, customerFilter = string.Empty, customerFilter2 = string.Empty;
            #region Filter Reversed Documents

            if (discardReversedDocuments)
            {
                qry_SalesInvoiceFilter = " And IsNull(Sal_InvoiceHeader.IsReversed, 0) = 0 And Sal_InvoiceHeader.ReversalID is null ";
                qry_ReceiptFilter = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                qry_PurchaseInvoiceFilter = " And IsNull(AR_InvoiceHeader.IsReversed, 0) = 0 And AR_InvoiceHeader.ReversalID is null ";
                qry_PaymentFilter = " And AP_PaymentHeader.ReversalComments is null ";
            }

            #endregion

            #region query

            //   string Query_BusinessPartnerTrailBalance = FnI.QueryResourceLibrary.GL.GL_TransactionHeader.Query_BusinessPartnerTrailBalance;
            string Query_BusinessPartnerTrailBalance = FnI.QueryResourceLibrary.GL.GL_TransactionHeader.Query_BusinessPartnerTrailBalance_New;
            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", datefrom);
                sqlparams[3] = new SqlParameter("@ToDate", dateto);

                if (customer != null)
                {
                    sb.Append(" and AR_SET_Customer.CustomerID between " + customer + "  and " + customerto + "");
                    customerFilter2 = " and TABInvoice.CustomerID between " + customer + "  and " + customerto + " ";
                }
                if (bpType.HasValue)
                {
                    ss.Append("and AR_SET_BusinessPartnerTypes.TypeCode = " + bpType);
                    businessPartnerTypeFilter2 = "and TABInvoice.TypeCode = " + bpType;

                }

                if (documentGroupID != "")
                    ss.Append(" AND Set_DocumentType.GroupID in " + documentGroupID);

                if (documentTypeAuthentication)
                {
                    sqlparams[4] = new SqlParameter("@UserID", userID);
                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
                    AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                if (Chartofacc != "")
                {
                    sqlparams[5] = new SqlParameter("@AccountCode", Chartofacc);


                }
                else
                    sqlparams[5] = new SqlParameter("@AccountCode", DBNull.Value);


                //string query = string.Format(FnI.QueryResourceLibrary.Setup.BusinessPartnerType.Query_BusinessPartnerStatusAll, businessPartnerTypeFilter, customerFilter, filter, qry_SalesInvoiceFilter,
                //  qry_ReceiptFilter, qry_PurchaseInvoiceFilter, qry_PaymentFilter, qry_docuTypeAuthenticate, businessPartnerTypeFilter2, customerFilter2, additionalTrade, appendingOpeningQueryForOthers,StatusCodeFilter)

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_BusinessPartnerTrailBalance, ss.ToString(), sb.ToString(),
                    qry_SalesInvoiceFilter, qry_ReceiptFilter, qry_PurchaseInvoiceFilter, qry_PaymentFilter, qry_docuTypeAuthenticate, customerFilter2, businessPartnerTypeFilter2),
                    sqlparams);

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

        public static DataTable GET_BusinessPartnerTrailBalanceTwoData(int companyID, int periodID, Nullable<int> customer, Nullable<int> customerto,
       Nullable<DateTime> datefrom, Nullable<DateTime> dateto, Guid userID, Nullable<int> bpType = null, Nullable<short> documentGroupID = null,
       bool documentTypeAuthentication = false, bool discardReversedDocuments = false, string Chartofacc = "", string ChartofaccTo = "")
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder ss = new StringBuilder();
            string AccountCodefilter = "";
            string qry_docuTypeAuthenticate = string.Empty;
            string qry_SalesInvoiceFilter = string.Empty, qry_ReceiptFilter = string.Empty, qry_PurchaseInvoiceFilter = string.Empty,
                qry_PaymentFilter = string.Empty,
             businessPartnerTypeFilter2 = string.Empty, customerFilter = string.Empty, customerFilter2 = string.Empty;
            #region Filter Reversed Documents

            if (discardReversedDocuments)
            {
                qry_SalesInvoiceFilter = " And IsNull(Sal_InvoiceHeader.IsReversed, 0) = 0 And Sal_InvoiceHeader.ReversalID is null ";
                qry_ReceiptFilter = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                qry_PurchaseInvoiceFilter = " And IsNull(AR_InvoiceHeader.IsReversed, 0) = 0 And AR_InvoiceHeader.ReversalID is null ";
                qry_PaymentFilter = " And AP_PaymentHeader.ReversalComments is null ";
            }

            #endregion

            #region query

            //   string Query_BusinessPartnerTrailBalance = FnI.QueryResourceLibrary.GL.GL_TransactionHeader.Query_BusinessPartnerTrailBalance;
            string Query_BusinessPartnerTrailBalance = FnI.QueryResourceLibrary.GL.GL_TransDetailDelete.Query_BusinessPartnerTrailBalance_Two;
            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", datefrom);
                sqlparams[3] = new SqlParameter("@ToDate", dateto);

                if (customer != null)
                {
                    sb.Append(" and AR_SET_Customer.CustomerID between " + customer + "  and " + customerto + "");
                    customerFilter2 = " and TABInvoice.CustomerID between " + customer + "  and " + customerto + " ";
                }
                if (bpType.HasValue)
                {
                    ss.Append("and AR_SET_BusinessPartnerTypes.TypeCode = " + bpType);
                    businessPartnerTypeFilter2 = "and TABInvoice.TypeCode = " + bpType;

                }

                if (documentGroupID.HasValue)
                    ss.Append(" AND Set_DocumentType.GroupID = " + documentGroupID);

                if (documentTypeAuthentication)
                {
                    sqlparams[4] = new SqlParameter("@UserID", userID);
                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
                    AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                //    if (Chartofacc != "")
                //  {
                //    sqlparams[5] = new SqlParameter("@AccountCode", Chartofacc);


                //}
                //else
                sqlparams[5] = new SqlParameter("@AccountCode", DBNull.Value);


                if (Chartofacc != "" && ChartofaccTo != "")
                {
                    sqlparams[6] = new SqlParameter("@AccountCodeFrom", Chartofacc);
                    sqlparams[7] = new SqlParameter("@AccountCodeTo", ChartofaccTo);

                    AccountCodefilter = " Where finaltab.codeCombinationID between @AccountCodeFrom  and @AccountCodeTo ";
                }

                string CustomerFilter = "";
                if (customer.HasValue)
                    CustomerFilter = " where customerid=" + customer + "";

                //else
                //         sqlparams[5] = new SqlParameter("@AccountCodeTo", DBNull.Value);


                //string query = string.Format(FnI.QueryResourceLibrary.Setup.BusinessPartnerType.Query_BusinessPartnerStatusAll, businessPartnerTypeFilter, customerFilter, filter, qry_SalesInvoiceFilter,
                //  qry_ReceiptFilter, qry_PurchaseInvoiceFilter, qry_PaymentFilter, qry_docuTypeAuthenticate, businessPartnerTypeFilter2, customerFilter2, additionalTrade, appendingOpeningQueryForOthers,StatusCodeFilter)

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_BusinessPartnerTrailBalance, sb.ToString(), qry_docuTypeAuthenticate, ss.ToString(), AccountCodefilter) + CustomerFilter,
                    sqlparams);

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
        public static DataRow GetDataByKeyDocumentNoandDocumentTypeID(int companyID, int periodID, int DocumentTypeID, int DocumentNo, int? fiscalID = null)
        {
            DataRow drToReturn = null;

            const string Query_Select_Voucher_ByDocnoAndDocType = @"Select * from GL_TransactionHeader Where
CompanyID=@CompanyID
AND PeriodID=@PeriodID
AND FiscalID=@fiscalID
And DocumentTypeID=@DocumentTypeID
And DocumentNo= @DocumentNo
";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@DocumentNo", DocumentNo);
                sqlparams[4] = new SqlParameter("@fiscalID", fiscalID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_Voucher_ByDocnoAndDocType, sqlparams);

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

        public bool DeleteTransactionHederAndDetail(int transactionID)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("DeleteTransactionHederAndDetail");

            try
            {
                DeleteDataByTransactionID(sqltrans, transactionID);

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
                    SqlHelper.RollBackTransaction(sqltrans, "DeleteTransactionHederAndDetail");
            }
            return result;
        }

        public static bool DeleteDataByTransactionID(SqlTransaction objTransaction, int transactionID)
        {
            bool result = false;
            const string Query_Delete_ByTransactionIdForInvoiceStatus = @"
Delete from GL_TransactionDetail where TransactionID=@TransactionID
Delete From GL_TransactionHeader Where TransactionID=@TransactionID
";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@TransactionID", transactionID);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByTransactionIdForInvoiceStatus, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByTransactionIdForInvoiceStatus, sqlparams) > 0)
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

        public bool InsertPF(string DatabaseName, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                if (f_ControlCredit != f_ControlDebit)
                    throw new Exception("Total Debit and Total Credit amount do not tally (Debit: " + f_ControlDebit + ") (Credit: " + f_ControlCredit + ").");

                #region DocumentSecurityDate

                DataRow drDocumentSecurityDate = Set_DocumentSecurity.GetDataByKey(1);
                if (drDocumentSecurityDate != null && bool.Parse(drDocumentSecurityDate["Lockfortransaction"].ToString()) == true)
                {
                    DateTime DocumentSecurityDate = DateTime.Parse(drDocumentSecurityDate["Date"].ToString());

                    if (DocumentSecurityDate >= f_DocumentDate)
                        throw new BusinessLogicException("Log date not allowed.");
                }
                #endregion

                StringBuilder sb = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[26];
                sqlparams[0] = new SqlParameter("@TransactionID", f_TransactionID);
                sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[2] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[3] = new SqlParameter("@ControlCredit", f_ControlCredit);
                sqlparams[4] = new SqlParameter("@ControlDebit", f_ControlDebit);
                sqlparams[5] = new SqlParameter("@Status", f_Status);
                sqlparams[6] = new SqlParameter("@DocumentTypeID", f_DocumentTypeID);
                sqlparams[7] = new SqlParameter("@DocumentDate", f_DocumentDate);
                sqlparams[8] = new SqlParameter("@DocumentNo", f_DocumentNo);
                sqlparams[9] = new SqlParameter("@CreatedBy", f_CreatedBy);
                sqlparams[10] = new SqlParameter("@CreateDate", f_CreateDate);
                sqlparams[11] = new SqlParameter("@CreatedByValue", f_CreatedByValue);

                if (f_ApprovedBy.HasValue)
                    sqlparams[12] = new SqlParameter("@ApprovedBy", f_ApprovedBy.Value);
                else
                    sqlparams[12] = new SqlParameter("@ApprovedBy", DBNull.Value);

                if (f_DateOfApproval.HasValue)
                    sqlparams[13] = new SqlParameter("@DateOfApproval", f_DateOfApproval.Value);
                else
                    sqlparams[13] = new SqlParameter("@DateOfApproval", DBNull.Value);

                if (!string.IsNullOrEmpty(f_Narration))
                    sqlparams[14] = new SqlParameter("@Narration", f_Narration);
                else
                    sqlparams[14] = new SqlParameter("@Narration", DBNull.Value);

                if (f_DefaultChartofAccountID.HasValue)
                    sqlparams[15] = new SqlParameter("@DefaultChartofAccountID", f_DefaultChartofAccountID.Value);
                else
                    sqlparams[15] = new SqlParameter("@DefaultChartofAccountID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_DefaultAccountCode))
                    sqlparams[16] = new SqlParameter("@DefaultAccountCode", f_DefaultAccountCode);
                else
                    sqlparams[16] = new SqlParameter("@DefaultAccountCode", DBNull.Value);

                if (!string.IsNullOrEmpty(f_DefaultTransactionType))
                    sqlparams[17] = new SqlParameter("@DefaultTransactionType", f_DefaultTransactionType);
                else
                    sqlparams[17] = new SqlParameter("@DefaultTransactionType", DBNull.Value);

                if (f_StatusCode.HasValue)
                    sqlparams[18] = new SqlParameter("@StatusCode", f_StatusCode.Value);
                else
                    sqlparams[18] = new SqlParameter("@StatusCode", DBNull.Value);

                if (f_LocationID.HasValue)
                    sqlparams[19] = new SqlParameter("@LocationID", f_LocationID.Value);
                else
                    sqlparams[19] = new SqlParameter("@LocationID", DBNull.Value);

                if (f_IsReversed.HasValue)
                    sqlparams[20] = new SqlParameter("@IsReversed", f_IsReversed.Value);
                else
                    sqlparams[20] = new SqlParameter("@IsReversed", DBNull.Value);

                if (f_ReversalDate.HasValue)
                    sqlparams[21] = new SqlParameter("@ReversalDate", f_ReversalDate.Value);
                else
                    sqlparams[21] = new SqlParameter("@ReversalDate", DBNull.Value);

                if (f_ReversedBy.HasValue)
                    sqlparams[22] = new SqlParameter("@ReversedBy", f_ReversedBy.Value);
                else
                    sqlparams[22] = new SqlParameter("@ReversedBy", DBNull.Value);

                if (f_ReversalID.HasValue)
                    sqlparams[23] = new SqlParameter("@ReversalID", f_ReversalID.Value);
                else
                    sqlparams[23] = new SqlParameter("@ReversalID", DBNull.Value);

                if (f_FiscalID.HasValue)
                    sqlparams[24] = new SqlParameter("@FiscalID", f_FiscalID.Value);
                else
                    sqlparams[24] = new SqlParameter("@FiscalID", DBNull.Value);

                if (f_GroupID.HasValue)
                    sqlparams[25] = new SqlParameter("@GroupID", f_GroupID.Value);
                else
                    sqlparams[25] = new SqlParameter("@GroupID", DBNull.Value);

                sb.Append(DatabaseName);

                const string Query_InsertPF = @" INSERT INTO {0}.dbo.[GL_TransactionHeader] ([TransactionID], [PeriodID], [CompanyID], [ControlCredit], [ControlDebit], [Status], [DocumentTypeID], [DocumentDate], [DocumentNo], [CreatedBy], [CreateDate], [CreatedByValue], [ApprovedBy], [DateOfApproval], [Narration], [DefaultChartofAccountID], [DefaultAccountCode], [DefaultTransactionType], [StatusCode], [LocationID], [IsReversed], [ReversalDate], [ReversedBy], [ReversalID], [FiscalID], [GroupID])
		VALUES(@TransactionID, @PeriodID, @CompanyID, @ControlCredit, @ControlDebit, @Status, @DocumentTypeID, @DocumentDate, @DocumentNo, @CreatedBy, @CreateDate, @CreatedByValue, @ApprovedBy, @DateOfApproval, @Narration, @DefaultChartofAccountID, @DefaultAccountCode, @DefaultTransactionType, @StatusCode, @LocationID, @IsReversed, @ReversalDate, @ReversedBy, @ReversalID, @FiscalID, @GroupID) ";

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, string.Format(Query_InsertPF, sb.ToString()), sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, string.Format(Query_InsertPF, sb.ToString()), sqlparams) > 0)
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

        public static int GetNewTransactionIDPF(int companyID, int periodID, string DatabaseName, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;

            const string Query_Get_MaxIdforPF = @"SELECT IsNull(max(TransactionID), 0) + 1 as TransactionID FROM {0}.dbo.GL_TransactionHeader WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID ";

            try
            {
                StringBuilder sb = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                sb.Append(DatabaseName);

                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, string.Format(Query_Get_MaxIdforPF, sb.ToString()), sqlparams));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, string.Format(Query_Get_MaxIdforPF, sb.ToString()), sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, string.Format(Query_Get_MaxIdforPF, sb.ToString()), sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static DataTable GetJobData(int companyID, int periodID, int CurrentFiscalID = 0, Nullable<int> JobName = null, Nullable<DateTime> dateFrom = null,
            Nullable<DateTime> dateTo = null, string JobCode = null)
        {
            StringBuilder additionalQueryParameters = new StringBuilder();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataTable dtToReturn = null;
            if (dateFrom.HasValue && dateTo.HasValue)
            {
                //additionalQueryParameters.Append(" AND (INV_StockTransactionHeader.DocumentDate BETWEEN @FromDate AND @ToDate)");
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom.Value));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo.Value));
            }
            else if (dateFrom.HasValue)
            {
                //additionalQueryParameters.Append(" And (INV_StockTransactionHeader.DocumentDate >= @FromDate)");
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom.Value));
            }
            else if (dateTo.HasValue)
            {
                //additionalQueryParameters.Append(" And (INV_StockTransactionHeader.DocumentDate <= @ToDate)");
                sqlParams.Add(new SqlParameter("@ToDate", dateTo.Value));
            }

            if (JobName.HasValue)
            {
                additionalQueryParameters.Append(" AND (Set_job.jobID = @JobID)");
                sqlParams.Add(new SqlParameter("@JobID", JobName.Value));
            }
            if (JobCode != "")
            {
                additionalQueryParameters.Append(" AND (Set_job.AccountCode = @JobCode)");
                sqlParams.Add(new SqlParameter("@JobCode", JobCode));
            }
            //additionalQueryParameters.Append("  order by INV_StockTransactionHeader.DocumentNo");
            sqlParams.Add(new SqlParameter("@CompanyID", companyID));
            sqlParams.Add(new SqlParameter("@PeriodID", periodID));
            sqlParams.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));

            string str = !string.IsNullOrEmpty(additionalQueryParameters.ToString()) ? additionalQueryParameters.ToString() : string.Empty;
            DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_JobReport, str), sqlParams.ToArray());

            if (ds != null && ds.Tables.Count > 0)
                dtToReturn = ds.Tables[0];

            //catch (Exception ex)
            //{
            //    SqlHelper.LogException(ex);
            //    throw ex;
            //}

            return dtToReturn;
        }

        public static DataTable ColumnarGLReportMonthWise_Data(string companyID, Nullable<int> periodID, Nullable<int> CostCenterID, int CurrentFiscalID, Nullable<DateTime> Date_From, 
            Nullable<DateTime> Date_To, bool chk_DivideBy, bool chk_Opening, string accountCodeFrom = null, string accountCodeTo = null, 
            string TransactionType = null, bool WithoutClosing = false, string multiFiscalIds = "")
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            StringBuilder sb3 = new StringBuilder();
            //StringBuilder sb4 = new StringBuilder();
            //string accountCodeFilter = string.Empty;

            string WithOutClosingFilter = String.Empty;

            multiFiscalIds = "(" + multiFiscalIds + ")";

            SqlParameter[] sqlparams = new SqlParameter[6];
            sqlparams[0] = new SqlParameter("@CompanyID", companyID);
            sqlparams[1] = new SqlParameter("@PeriodID", periodID);
            sqlparams[2] = new SqlParameter("@FromDate", Date_From);
            sqlparams[3] = new SqlParameter("@ToDate", Date_To);
            sqlparams[4] = new SqlParameter("@FiscalID", CurrentFiscalID);
            if (chk_Opening == true)
            {
                sqlparams[5] = new SqlParameter("@WithOpening", "1");
            }
            else
            {
                sqlparams[5] = new SqlParameter("@WithOpening", "0");
            }
            if (CostCenterID > 0)
            {
                sb3.Append(" AND GL_TransactionDetail.CostCenterID = " + CostCenterID + " ");
            }
            //else
            //{
                
            //}
                try
                {

                #region Filter by Account Codes

                if (!string.IsNullOrEmpty(accountCodeFrom) && !string.IsNullOrEmpty(accountCodeTo))
                {
                    sb.Append(" AND GL_TransactionDetail.CodeCombinationID BETWEEN '" + accountCodeFrom + "' AND '" + accountCodeTo + "' ");
                    sb2.Append(" AND GL_TransactionDetail.CodeCombinationID BETWEEN '" + accountCodeFrom + "' AND '" + accountCodeTo + "' ");
                }
                else if (!string.IsNullOrEmpty(accountCodeFrom))
                {
                    sb.Append(" AND GL_TransactionDetail.CodeCombinationID >= '" + accountCodeFrom + "'  ");
                    sb2.Append(" AND GL_TransactionDetail.CodeCombinationID >= '" + accountCodeFrom + "'  ");
                }
                else if (!string.IsNullOrEmpty(accountCodeTo))
                {
                    sb.Append(" AND GL_TransactionDetail.CodeCombinationID <= '" + accountCodeTo + "' ");
                    sb2.Append(" AND GL_TransactionDetail.CodeCombinationID <= '" + accountCodeTo + "' ");
                    
                }
                if (TransactionType != "Both")
                {
                    sb.Append(" And GL_TransactionDetail.TransactionType = '" + TransactionType + "' ");
                    sb2.Append(" And GL_TransactionDetail.TransactionType = '" + TransactionType + "' ");
                }


                sb.Append(" Group By GL_TransactionHeader.DocumentDate, GL_TransactionHeader.TransactionID, GL_TransactionDetail.CodeCombinationID,GL_CodeCombinationDetail.Description,GL_TransactionDetail.CostCenterID, SET_CostCenter.CostCenterName ) Tab");
                sb.Append(" Group by Tab.DocumentDate, Tab.TransactionID, Tab.CodeCombinationID , Tab.Description, Tab.CostCenterID, Tab.CostCenterName, Tab.MonthNames, Tab.Credit, Tab.Debit, Tab.MonthNum, Tab.Years,Tab.WithOpening order by Tab.Years, Tab.MonthNum asc");

                if (chk_DivideBy == true)
                {
                    sb1.Append("(Case When Tab.MonthNames = 'Opening' And Tab.WithOpening = '0' Then 0 Else");
                    sb1.Append("(ISNULL(sum(Tab.Debit),0) - ISNULL(Sum(Tab.Credit),0))/1000 End) as Amount");
                }
                else
                {
                    sb1.Append("(Case When Tab.MonthNames = 'Opening' And Tab.WithOpening = '0' Then 0 Else");
                    sb1.Append("(ISNULL(sum(Tab.Debit),0) - ISNULL(Sum(Tab.Credit),0)) End) as Amount");
                }

                if (WithoutClosing)
                {
                    WithOutClosingFilter = @" AND GL_TransactionHeader.TransactionID not in (
		
		select GL.TransactionID  
		from GL_TransactionHeader GL 
		inner join Set_DocumentType Doctype on Doctype.DocumentTypeID = GL.DocumentTypeID
		and Doctype.CompanyID = GL.CompanyID

		inner join SET_PeriodCustom on SET_PeriodCustom.CompanyID = GL .CompanyID
		and SET_PeriodCustom.FiscalId IN {multiFiscalIds}

		where GL.CompanyID = GL_TransactionHeader.CompanyID
		and GL.PeriodID = GL_TransactionHeader.PeriodID
		and GL.FiscalID  IN {multiFiscalIds}
		and Doctype.DocumentType = 'GL-OPENING'
		and GL.DocumentDate = SET_PeriodCustom.EndDate)";
                }

                

                #endregion

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Select_ColumnarGLReportMonthWise.Replace("{CompanyIDs}", companyID).Replace("{WithoutClosing}", WithOutClosingFilter.Replace("{multiFiscalIds}", multiFiscalIds).Replace("{CompanyIDs}", companyID)), sb, sb1, sb2, sb3), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;

        }

        public static DataTable ColumnarInvReportMonthWise_Data(string companyID, Nullable<int> periodID, Nullable<int> CostCenterID, int CurrentFiscalID, Nullable<DateTime> Date_From,
    Nullable<DateTime> Date_To, string TransactionType = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();

            SqlParameter[] sqlparams = new SqlParameter[6];
            sqlparams[0] = new SqlParameter("@CompanyID", companyID);
            sqlparams[1] = new SqlParameter("@PeriodID", periodID);
            sqlparams[2] = new SqlParameter("@FromDate", Date_From);
            sqlparams[3] = new SqlParameter("@ToDate", Date_To);
            sqlparams[4] = new SqlParameter("@FiscalID", CurrentFiscalID);

            if (TransactionType == "Both")
            {
            }
            else if (TransactionType == "Addition")
            {
                sb1.Append("WHERE TAB.TransactionImpact = 'Positive'");
                sb2.Append("WHERE TABOpening.TransactionImpact = 'Positive'");
            }
            else if (TransactionType == "Deduction")
            {
                sb1.Append("WHERE TAB.TransactionImpact = 'Negative'");
                sb2.Append("WHERE TABOpening.TransactionImpact = 'Negative'");
            }

            if (CostCenterID > 0)
            {
                sb.Append(" AND INV_StockTransactionHeader.CostCenterID = " + CostCenterID + " ");
            }

            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Select_ColumnarInvReportMonthWise.Replace("{CompanyIDs}", companyID), sb, sb1, sb2), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;

        }


        public static DataTable YearWiseVarianceReport_Data(string companyID, Nullable<int> periodID, int CurrentFiscalID, Nullable<DateTime> CurrentFromDate, Nullable<DateTime> CurrentToDate, Nullable<DateTime> PrevFromDate, Nullable<DateTime> PrevToDate, bool chk_DivideBy, bool chk_Opening, string accountCodeFrom = null, string accountCodeTo = null, string TransactionType = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            StringBuilder sb3 = new StringBuilder();
            //string accountCodeFilter = string.Empty;
            SqlParameter[] sqlparams = new SqlParameter[8];
            sqlparams[0] = new SqlParameter("@CompanyID", companyID);
            sqlparams[1] = new SqlParameter("@PeriodID", periodID);
            sqlparams[2] = new SqlParameter("@CurrentFromDate", CurrentFromDate);
            sqlparams[3] = new SqlParameter("@CurrentToDate", CurrentToDate);
            sqlparams[4] = new SqlParameter("@PrevFromDate", PrevFromDate);
            sqlparams[5] = new SqlParameter("@PrevToDate", PrevToDate);
            sqlparams[6] = new SqlParameter("@FiscalID", CurrentFiscalID);
            if (chk_Opening == true)
            {
                sqlparams[7] = new SqlParameter("@WithOpening", "1");
            }
            else
            {
                sqlparams[7] = new SqlParameter("@WithOpening", "0");
            }
           
            try
            {

                #region Filter by Account Codes

                if (!string.IsNullOrEmpty(accountCodeFrom) && !string.IsNullOrEmpty(accountCodeTo))
                {
                    sb.Append(" AND GL_TransactionDetail.CodeCombinationID BETWEEN '" + accountCodeFrom + "' AND '" + accountCodeTo + "' ");
                    sb2.Append(" AND GL_TransactionDetail.CodeCombinationID BETWEEN '" + accountCodeFrom + "' AND '" + accountCodeTo + "' ");
                }
                else if (!string.IsNullOrEmpty(accountCodeFrom))
                {
                    sb.Append(" AND GL_TransactionDetail.CodeCombinationID >= '" + accountCodeFrom + "'  ");
                    sb2.Append(" AND GL_TransactionDetail.CodeCombinationID >= '" + accountCodeFrom + "'  ");
                }
                else if (!string.IsNullOrEmpty(accountCodeTo))
                {
                    sb.Append(" AND GL_TransactionDetail.CodeCombinationID <= '" + accountCodeTo + "' ");
                    sb2.Append(" AND GL_TransactionDetail.CodeCombinationID <= '" + accountCodeTo + "' ");

                }
                if (TransactionType != "Both")
                {
                    sb.Append(" And GL_TransactionDetail.TransactionType = '" + TransactionType + "' ");
                    sb2.Append(" And GL_TransactionDetail.TransactionType = '" + TransactionType + "' ");
                }


                //sb.Append(" Group By GL_TransactionHeader.DocumentDate, GL_TransactionHeader.TransactionID, GL_TransactionDetail.CodeCombinationID,GL_CodeCombinationDetail.Description ) Tab");
                //sb.Append(" Group by Tab.DocumentDate, Tab.TransactionID, Tab.CodeCombinationID , Tab.Description, Tab.MonthNames, Tab.Credit, Tab.Debit, Tab.MonthNum, Tab.Years,Tab.WithOpening order by Tab.Years, Tab.MonthNum asc");

                if (chk_DivideBy == true)
                {
                    sb1.Append("ISNULL(SUM(FInTab.CurrentAmount),0) / 1000 as CurrentAmount, ISNULL(SUM(FInTab.PreviousAmount),0) / 1000 as PreviousAmount,");
                    sb1.Append(" ISNULL(SUM(FInTab.CurrentOpening),0) / 1000 as CurrentOpening, ISNULL(SUM(FInTab.PreviousOpening),0) / 1000 as PreviousOpening");
                }
                else
                {
                    sb1.Append("ISNULL(SUM(FInTab.CurrentAmount),0) as CurrentAmount, ISNULL(SUM(FInTab.PreviousAmount),0) as PreviousAmount,");
                    sb1.Append(" ISNULL(SUM(FInTab.CurrentOpening),0) as CurrentOpening, ISNULL(SUM(FInTab.PreviousOpening),0) as PreviousOpening");
                }

                #endregion

                string Query_YearWiseVariance = FnI.QueryResourceLibrary.GL.GL_TransactionHeader4.Query_YearWiseVarianceReport.ToString();

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_YearWiseVariance.Replace("{CompanyIDs}", companyID), sb, sb1, sb2), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;

        }

        public static DataTable GetJobCodeSetupReportData(int companyID, int periodID, Nullable<int> JobName = null, string JobCode = null)
        {
            StringBuilder additionalQueryParameters = new StringBuilder();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataTable dtToReturn = null;

            if (JobName.HasValue)
            {
                additionalQueryParameters.Append(" AND (Set_job.jobID = @JobID)");
                sqlParams.Add(new SqlParameter("@JobID", JobName.Value));
            }
            if (JobCode != "")
            {
                additionalQueryParameters.Append(" AND (Set_job.AccountCode = @JobCode)");
                sqlParams.Add(new SqlParameter("@JobCode", JobCode));
            }
            
            sqlParams.Add(new SqlParameter("@CompanyID", companyID));
            sqlParams.Add(new SqlParameter("@PeriodID", periodID));
           // sqlParams.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));

            string str = !string.IsNullOrEmpty(additionalQueryParameters.ToString()) ? additionalQueryParameters.ToString() : string.Empty;
            DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.GL.GL_TransactionHeader.Query_JobCodeSetupReport) + str, sqlParams.ToArray());

            if (ds != null && ds.Tables.Count > 0)
                dtToReturn = ds.Tables[0];

            //catch (Exception ex)
            //{
            //    SqlHelper.LogException(ex);
            //    throw ex;
            //}

            return dtToReturn;
        }
        public static DataTable GetAllActiveDataFiscal(Nullable<int> companyID, Nullable<int> periodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null, int CurrentFiscalID = 0)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                DataSet ds = null;

                string Query_SelectAll = @"SELECT GL_TransactionHeader.* FROM [GL_TransactionHeader] 
                                    inner join Set_PeriodCustom on  
                                    GL_TransactionHeader.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
                                    and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID
                                    WHERE GL_TransactionHeader.[Status] = 1 AND (GL_TransactionHeader.[CompanyID] = @CompanyID OR @CompanyID is null) 
                                    AND (GL_TransactionHeader.[PeriodID] = @PeriodID OR @PeriodID is null)";

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAll, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAll, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll, sqlparams);

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
        public bool InProcessDocument(GL_TransactionHeader th, List<GL_TransactionDetail> colTranDetail, int CompanyID, int PeriodID, int CurrentFiscalID)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InProcessVoucher");

            try
            {
                this.StatusCode = 5;
                this.UpdateDataByKey(sqltrans);


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
                    SqlHelper.RollBackTransaction(sqltrans, "InProcessVoucher");
            }

            return result;
        }

        public DataTable Get_AllDocumentNosForInprocess(int companyID, int periodID, int documentTypeID, int CurrentFiscalID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);


                string Query_Get_AllDocumentNos = @" SELECT GL_TransactionHeader.* FROM GL_TransactionHeader 
inner join Set_PeriodCustom on GL_TransactionHeader.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID
WHERE GL_TransactionHeader.CompanyID = @CompanyID and GL_TransactionHeader.PeriodID = @PeriodID and GL_TransactionHeader.DocumentTypeID = @DocumentTypeID And GL_TransactionHeader.StatusCode = 1 And GL_TransactionHeader.Status = 1 AND GL_TransactionHeader.ReversalDate IS NULL
ORDER BY GL_TransactionHeader.DocumentNo";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_AllDocumentNos, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return dtToReturn;
        }


        public static DataTable CostofGoodsManufactured(int companyid,int PeriodID, string coaCodeFrom, string coaCodeTo,DateTime FromDate,DateTime ToDate,int FiscalID)
        {
            DataTable dt = null;
            string additionalFilter = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);



                if (!string.IsNullOrEmpty(coaCodeFrom) && !string.IsNullOrEmpty(coaCodeTo))
                {
                    additionalFilter = " And GL_CodeCombinationDetail.CodeCombinationID Between @CombinationCodeFrom And @CombinationCodeTo ";
                    sqlparams[2] = new SqlParameter("@CombinationCodeFrom", coaCodeFrom);
                    sqlparams[3] = new SqlParameter("@CombinationCodeTo", coaCodeTo);
                }

                if (!string.IsNullOrEmpty(coaCodeFrom))
                {
                    additionalFilter = " And GL_CodeCombinationDetail.CodeCombinationID >= @CombinationCodeFrom ";
                    sqlparams[2] = new SqlParameter("@CombinationCodeFrom", coaCodeFrom);
                    sqlparams[3] = new SqlParameter("@CombinationCodeTo", DBNull.Value);
                }

                if (!string.IsNullOrEmpty(coaCodeTo))
                {
                    additionalFilter = " And GL_CodeCombinationDetail.CodeCombinationID <= @CombinationCodeTo ";
                    sqlparams[2] = new SqlParameter("@CombinationCodeFrom", DBNull.Value);
                    sqlparams[3] = new SqlParameter("@CombinationCodeTo", coaCodeTo);
                }

                sqlparams[4] = new SqlParameter("@FromDate", FromDate);
                sqlparams[5] = new SqlParameter("@ToDate", ToDate);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", FiscalID);


                
                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.GL.GL_TransactionHeader2.Query_CostofGoodsManufactured,additionalFilter), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static object GetAllDataMovementSummaryPrepaidExpenses(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, int? StatusID, Nullable<DateTime> Date_To, int? PersonID, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            DataSet ds = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                if (StatusID.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@StatusID", StatusID.ToString());
                }
                else
                {
                    sqlparams[4] = new SqlParameter("@StatusID", DBNull.Value);
                }
                if (PersonID.HasValue)
                {
                    sqlparams[5] = new SqlParameter("@PersonID", PersonID.ToString());
                }
                else
                {
                    sqlparams[5] = new SqlParameter("@PersonID", DBNull.Value);
                }
                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionHeader.Query_MovementSummaryPrepaidExpenses, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionHeader.Query_MovementSummaryPrepaidExpenses, sqlparams);
                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return ds.Tables[0];
        }

        public static object GetAllDataPrepaidExpensesMovement(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> CurrentFiscalID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To)
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@FromDate", Date_From);
                sqlparams[4] = new SqlParameter("@ToDate", Date_To);

                //if (objConnection == null)
                //    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_PODetailImported, sqlparams);
                //else
                ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionHeader.Query_PrepaidExpensesMovement, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return ds.Tables[0];
        }

        public static DataTable GetDataMovementSummaryPrepaidExpenses(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_MovementSummaryPrepaidExpenses, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_MovementSummaryPrepaidExpenses, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_MovementSummaryPrepaidExpenses, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];

                return dt;
            }

            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
        }
        public static DataRow GetDataByDocumentTypeNew(int companyID, int periodID, int documentTypeID, int documentNo, int documentId, int CurrentFiscalID)
        {
            DataRow drToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[3] = new SqlParameter("@DocumentNo", documentNo);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[5] = new SqlParameter("@ID", documentId);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionHeader2.Query_GetDataByDocumentTypeNew, sqlparams);

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


      
             public static object GetDataForTransportationDetail_Report(int CompanyID, int PeriodID, DateTime? Date_From, DateTime? Date_To)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                //if (Date_From != null && Date_To != null)
                //{
                //    sb.Append(" and  Set_ContainerTransportationandUnloading.CreateDate between @FromDate and @ToDate ");
                //}
                //else if (Date_From != null)
                //{
                //    sb.Append(" and Set_ContainerTransportationandUnloading.CreateDate = @FromDate ");
                //}
                //else if (Date_From == null)
                //{
                //    sb.Append(" and Set_ContainerTransportationandUnloading.CreateDate = @ToDate");
                //}
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionHeader2.Query_ForTransportationDetail_Report + sb.ToString(), sqlparams); 

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


             public static DataTable GetDataForCustomClearingDetail(int CompanyID, int PeriodID, DateTime? Date_From, DateTime? Date_To)
             {
                 DataTable dtToReturn = null;
                 StringBuilder sb = new StringBuilder();
                 try
                 {
                     SqlParameter[] sqlparams = new SqlParameter[4];
                     sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                     sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                     sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                     sqlparams[3] = new SqlParameter("@ToDate", Date_To);


                     DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionHeader2.Query_ForCustomClearingDetail_Report + sb.ToString(), sqlparams);

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



             public static DataTable Get_ExpansesOfhalfYear(Nullable<int> companyID, int periodID, Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
             {
                 DataTable dt = null;

                 try
                 {

                     SqlParameter[] sqlparams = new SqlParameter[4];
                     sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                     sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                     sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                     sqlparams[3] = new SqlParameter("@ToDate", toDate);



                     DataSet ds = null;

                     if (objTransaction != null)
                         ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.GL.GL_TransactionHeader.Query_ExpansesOfhalfYears), sqlparams);
                     else if (objConnection != null)
                         ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.GL.GL_TransactionHeader.Query_ExpansesOfhalfYears), sqlparams);
                     else
                         ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.GL.GL_TransactionHeader.Query_ExpansesOfhalfYears), sqlparams);

                     if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                         dt = ds.Tables[0];

                     return dt;
                 }
                 catch (Exception ex)
                 {
                     SqlHelper.LogException(ex);
                     throw ex;
                 }

             }

             public static DataTable Get_ProfitAndLossAccountMonthWiseReport(string companyID, int periodID, int chartOfAccountID, string reportsFilter, Guid userID, Nullable<DateTime> datefrom = null,
Nullable<DateTime> dateto = null, bool? IsProfitAndLoss = false, Nullable<short> DocumentGroupID = null, int? Segment = null, string NoteNumber = "", Nullable<bool> WithoutClosing = null, int? CCID = null, bool chk_DateFromBelowFiscal = false)
             {
                 DataTable dtToReturn = null;
                 SqlConnection objConnection = ConnectionHelper.GetOpenSqlConnection();

                 try
                 {
                     Dictionary<string, decimal> dic_Results = new Dictionary<string, decimal>();
                     DataTable dt_FinancialHeader = Set_FinancialHeader.Get_FinacialHeader(reportsFilter, objConnection, NoteNumber, true, companyID);
                     DataTable dt_FinancialDetail = Set_FinancialDetail.Get_FinacialDetail(reportsFilter, objConnection, Segment, companyID);

                     // BASIC SETUP FOR HEADER - ADDING CUSTOM REPORTS
                     DataTable dtFromTB = new DataTable();

                     dt_FinancialHeader.Columns.Add("Month", typeof(string));
                     dt_FinancialHeader.Columns["Month"].AllowDBNull = true;

                     dt_FinancialHeader.Columns.Add("Months", typeof(string));
                     dt_FinancialHeader.Columns["Months"].AllowDBNull = true;

                     dt_FinancialHeader.Columns.Add("yearname", typeof(string));
                     dt_FinancialHeader.Columns["yearname"].AllowDBNull = true;

                     dtFromTB = dt_FinancialHeader.Clone();

                     //dtFromTB.Columns.Add("Month", typeof(string));
                     //dtFromTB.Columns["Month"].AllowDBNull = true;

                     //dtFromTB.Columns.Add("yearname", typeof(string));
                     //dtFromTB.Columns["yearname"].AllowDBNull = true;

                     #region Segment Filter Header

                     if (Segment.HasValue)
                     {

                         string stringDel = new SET_Company(companyID.ToIntOrZero()).BusinessId_Business.COASeparater;

                         char[] delimeter = stringDel.ToCharArray();

                         DataTable dt_Filtered = dt_FinancialHeader.Clone();

                         List<string> lst_AccountCodes = dt_FinancialHeader.AsEnumerable()
                      .Where(x => x.Field<string>("COACodeFrom") != "" && x.Field<string>("COACodeFrom").Split(delimeter).Length == Segment)
                      .Select(x => x.Field<string>("COACodeFrom")).ToList();
                         foreach (string codeCombinationID in lst_AccountCodes)
                         {

                             //{
                             DataRow[] drs_ToImport = dt_FinancialHeader.AsEnumerable().Where(x => x.Field<string>("COACodeFrom").Equals(codeCombinationID)).ToArray();

                             foreach (DataRow dr_ToImport in drs_ToImport)
                                 dt_Filtered.ImportRow(dr_ToImport);

                             dt_Filtered.AcceptChanges();
                             // }
                         }

                         dt_FinancialHeader = dt_Filtered.Copy();
                     }

                     #endregion

                     if (dt_FinancialHeader != null)
                     {

                         //header data explosion

                         foreach (DataRow drFinancial in dt_FinancialHeader.Rows)
                         {
                             if (drFinancial["COACodeFrom"].ToString() != "")// && drFinancial["COACodeTo"].ToString()!="")
                             {
                                 //DateTime now = DateTime.Now;
                                 //var startDate = new DateTime(int.Parse(drFinancial["yearname"].ToString()), int.Parse(drFinancial["Months"].ToString()), 1);
                                 //var endDate = startDate.AddMonths(1).AddDays(-1);

                                 DataTable dt = GL_TransactionDetail.GetTrialBalanceDataForFinancialFormMonthWise(-1, -1, companyID, chartOfAccountID, periodID,
                                 -1, -1, userID, datefrom, dateto, null, null, null, null, null, DocumentGroupID, false, false, false, drFinancial["COACodeFrom"].ToString()
                                 , drFinancial["COACodeTo"].ToString(), Convert.ToInt32(drFinancial["ElementSequence"]));

                                 if (dt != null)
                                 {


                                     foreach (DataRow dr in dt.Rows)
                                     {
                                         drFinancial["Months"] = dr["Months"].ToString();
                                         drFinancial["Month"] = dr["Month"].ToString();
                                         drFinancial["yearname"] = dr["yearname"].ToString();
                                         drFinancial.AcceptChanges();

                                         dtFromTB.Rows.Add(dr.ItemArray);
                                         dtFromTB.Rows.Add(drFinancial.ItemArray);
                                     }


                                 }
                             }


                         }



                         //detail chart of account from to business partner explosion
                         #region Segment Filter Detail

                         if (Segment.HasValue)
                         {

                             string stringDel = new SET_Company(companyID.ToIntOrZero()).BusinessId_Business.COASeparater;

                             char[] delimeter = stringDel.ToCharArray();

                             DataTable dt_Filtered = dt_FinancialDetail.Clone();

                             List<string> lst_AccountCodes = dt_FinancialDetail.AsEnumerable()
                          .Where(x => x.Field<string>("COACodeFrom").Split(delimeter).Length == Segment)
                          .Select(x => x.Field<string>("COACodeFrom")).ToList();
                             foreach (string codeCombinationID in lst_AccountCodes)
                             {

                                 //{
                                 DataRow[] drs_ToImport = dt_FinancialDetail.AsEnumerable().Where(x => x.Field<string>("COACodeFrom").Equals(codeCombinationID)).ToArray();

                                 foreach (DataRow dr_ToImport in drs_ToImport)
                                     dt_Filtered.ImportRow(dr_ToImport);

                                 dt_Filtered.AcceptChanges();
                                 // }
                             }

                             dt_FinancialDetail = dt_Filtered.Copy();
                         }

                         #endregion

                         foreach (DataRow drFinancial in dt_FinancialDetail.Rows)
                         {
                             if (drFinancial["COACodeFrom"].ToString() != "" && drFinancial["COACodeTo"].ToString() != "" && drFinancial["isBusinessPartner"].ToString() != "")
                             {
                                 DataTable dt = GL_TransactionDetail.GetTrialBalanceDataForFinancialBusiness(-1, -1, companyID.ToString(), chartOfAccountID, periodID,
                                 -1, -1, userID, datefrom, dateto, null, null, null, null, null, (short?)null, false, false, false, drFinancial["COACodeFrom"].ToString()
                                 , drFinancial["COACodeTo"].ToString(), Convert.ToInt32(drFinancial["FinancialID"]), drFinancial["FinancialID"].ToString() != "" ? Convert.ToInt32(drFinancial["FinancialID"]) : (int?)null, Convert.ToInt32(drFinancial["TypeCodeID"]));

                                 if (dt != null)
                                 {

                                     foreach (DataRow dr in dt.Rows)
                                     {
                                         dtFromTB.Rows.Add(dr.ItemArray);
                                     }

                                 }
                             }
                         }

                     }

                     foreach (DataRow dr in dtFromTB.Rows)
                     {
                         //  dt_FinancialHeader = dtFromTB.Copy();
                         //  dt_FinancialHeader.AcceptChanges();
                         dt_FinancialHeader.Rows.Add(dr.ItemArray);
                         dt_FinancialHeader.AcceptChanges();
                     }

                     if (dt_FinancialHeader != null)
                     {


                         if (IsProfitAndLoss == true)
                         {
                             dtToReturn = dt_FinancialHeader.Copy();
                             dtToReturn.Columns.Add("ResultValue", typeof(decimal));
                             dtToReturn.Columns["ResultValue"].AllowDBNull = true;
                             int index = 0;
                             foreach (DataRow drFinancial in dtToReturn.Rows)
                             {
                                 index++;
                                 if (drFinancial.Field<string>("ElementName").ToLower().StartsWith("%") && !drFinancial.Field<string>("ElementName").Equals("%NoText"))
                                 {
                                     switch (drFinancial.Field<string>("ElementName").ToLower())
                                     {
                                         case "%period":

                                             if (reportsFilter == "'Balance Sheet'")
                                             {
                                                 //drFinancial.SetField<string>("ElementName", "As at " + dateto.Value.ToString("MMMM") + " "
                                                 //    + dateto.Value.Year);
                                                 drFinancial.SetField<string>("ElementName", "Between  " + datefrom.Value.ToString("dd-MM-yyyy") + " AND  "
                                                    + dateto.Value.ToString("dd-MM-yyyy"));
                                             }

                                             else if (reportsFilter == "'Profit And Loss Statement'")
                                             {
                                                 drFinancial.SetField<string>("ElementName", "For the year ended December "
                                                     + dateto.Value.Year);

                                             }
                                             else
                                             {

                                                 //drFinancial.SetField<string>("ElementName", "For the year ended " + dateto.Value.ToString("MMMM") + " "
                                                 //        + dateto.Value.Year);
                                                 drFinancial.SetField<string>("ElementName", "For the Period " + datefrom.Value.ToString("dd-MM-yyyy") + " To  "
                                                    + dateto.Value.ToString("dd-MM-yyyy"));
                                                 if (chk_DateFromBelowFiscal == true)
                                                 {
                                                     drFinancial.SetField<string>("ElementName", "As at " + dateto.Value.ToString("dd-MM-yyyy"));
                                                 }

                                             }

                                             break;

                                         case "%colheadings":
                                             drFinancial.SetField<decimal?>("ResultValue", dateto.Value.Year);
                                             break;
                                     }

                                 }
                                 else if (drFinancial.Field<string>("ElementType").Equals("D") && drFinancial["COACodeFrom"].ToString() != "" && drFinancial["COACodeTo"].ToString() != "" && drFinancial["Amount"].ToString() == "")
                                 {

                                     decimal resultValue = 0;
                                     DateTime now = DateTime.Now;
                                     var startDate = new DateTime(int.Parse(drFinancial["yearname"].ToString()), int.Parse(drFinancial["Months"].ToString()), 1);
                                     var endDate = startDate.AddMonths(1).AddDays(-1);
                                     endDate = DateUtil.ConvertToDate(DateUtil.ConvertToString((DateTime)endDate) + " 23:59:59");

                                     resultValue += GL_TransactionDetail.GetSum(companyID.ToString(), periodID, chartOfAccountID, drFinancial.Field<string>("COACodeFrom"),
                                                drFinancial.Field<string>("COACodeTo"), userID, startDate, endDate, (int?)null, DocumentGroupID, null, false, "", CCID
                                                );
                                     resultValue = Negation(drFinancial, resultValue);
                                     drFinancial.SetField<decimal?>("ResultValue", resultValue);


                                 }
                                 //else if (drFinancial.Field<string>("ElementType").Equals("H") && drFinancial["COACodeFrom"].ToString() != "" && drFinancial["COACodeTo"].ToString() != "" && drFinancial["Amount"].ToString() == "")
                                 //{

                                 //    decimal resultValue = 0;

                                 //    resultValue += GL_TransactionDetail.GetSumByHeadCode(companyID, periodID, chartOfAccountID, drFinancial.Field<string>("COACodeFrom"),
                                 //               drFinancial.Field<string>("COACodeTo"), userID, dateto, (int?)Segment, DocumentGroupID
                                 //               );

                                 //    drFinancial.SetField<decimal?>("ResultValue", resultValue);


                                 //}

                                 else if (drFinancial.Field<string>("ElementType").Equals("T") && !string.IsNullOrEmpty(drFinancial.Field<string>("Formula")))
                                 {

                                     decimal resultValue = 0;

                                     if (!string.IsNullOrEmpty(drFinancial.Field<string>("Formula")))
                                     {
                                         string formula = drFinancial.Field<string>("Formula");

                                         while (formula.Contains("["))
                                         {
                                             int startingIndex = formula.IndexOf("[");
                                             int endingIndex = formula.IndexOf("]", startingIndex);
                                             string variable = formula.Substring(startingIndex + 1, (endingIndex - startingIndex - 1));
                                             try
                                             { formula = formula.Replace("[" + variable + "]", dic_Results[variable].ToString()); }
                                             catch { break; }

                                         }

                                         if (formula.Contains("/0"))
                                             resultValue = 0;
                                         else
                                             try
                                             { resultValue = Convert.ToDecimal(new DataTable().Compute(formula, null)); }
                                             catch { }

                                         //resultValue = Negation(drFinancial, resultValue);
                                         drFinancial.SetField<decimal?>("ResultValue", resultValue);
                                     }

                                 }
                                 else if (drFinancial.Field<string>("ElementType").Equals("D"))
                                 {
                                     decimal resultValue = 0;

                                     if (!string.IsNullOrEmpty(drFinancial.Field<string>("Formula")))
                                     {
                                         string formula = drFinancial.Field<string>("Formula");

                                         while (formula.Contains("["))
                                         {
                                             int startingIndex = formula.IndexOf("[");
                                             int endingIndex = formula.IndexOf("]", startingIndex);
                                             string variable = formula.Substring(startingIndex + 1, (endingIndex - startingIndex - 1));
                                             try
                                             { formula = formula.Replace("[" + variable + "]", dic_Results[variable].ToString()); }
                                             catch
                                             {

                                                 break;

                                                 //   variable = variable.Replace("ES", "");



                                                 //  resultValue += Convert.ToDecimal(dtToReturn.Compute("Sum(ResultValue)", "ResultValue is not null and ElementSequence=" + variable));



                                             }

                                         }

                                         if (formula.Contains("/0"))
                                             resultValue = 0;
                                         else
                                             try
                                             { resultValue = Convert.ToDecimal(new DataTable().Compute(formula, null)); }
                                             catch { }

                                         //resultValue = Negation(drFinancial, resultValue);
                                         drFinancial.SetField<decimal?>("ResultValue", resultValue);
                                     }
                                     else if (!string.IsNullOrEmpty(drFinancial.Field<string>("NoteNo")))
                                     {
                                         resultValue = GetResultRecursively(companyID.ToString(), periodID, chartOfAccountID, drFinancial.Field<string>("NoteNo"), objConnection, userID,
                                              dateto);

                                         //resultValue = Negation(drFinancial, resultValue);
                                         drFinancial.SetField<decimal?>("ResultValue", resultValue);
                                     }
                                     else if (drFinancial.Field<bool>("FixedInput") == true)
                                     {
                                         try
                                         {
                                             decimal detailAmount = 0;

                                             foreach (DataRow item in dt_FinancialDetail.Rows)
                                             {
                                                 if (item.Field<bool>("FixedInput") == true && (drFinancial["FinancialID"].ToString() == item["FinancialHeaderID"].ToString()))
                                                 {
                                                     detailAmount = item["Amount"].ToString() != "" ? item.Field<decimal>("Amount") : 0;
                                                 }
                                             }


                                             resultValue = drFinancial["Amount"].ToString() != "" ? drFinancial.Field<decimal>("Amount") + detailAmount : 0;
                                             drFinancial.SetField<decimal?>("ResultValue", resultValue);
                                         }
                                         catch { }

                                     }
                                     else
                                     {
                                         IEnumerable<DataRow> dr_FinancialDetails = from dr_Detail in dt_FinancialDetail.AsEnumerable()
                                                                                    where dr_Detail.Field<int>("FinancialHeaderID") == drFinancial.Field<int>("FinancialID")
                                                                                    select dr_Detail;

                                         foreach (DataRow dr_FinancialDetail in dr_FinancialDetails)
                                         {
                                             resultValue += GL_TransactionDetail.GetSum(companyID.ToString(), periodID, chartOfAccountID, dr_FinancialDetail.Field<string>("COACodeFrom"),
                                                dr_FinancialDetail.Field<string>("COACodeTo"), userID, datefrom, dateto, dr_FinancialDetail["Segment"].ToString() != "" ? dr_FinancialDetail.Field<int>("Segment") : (int?)null, 
                                                null, null, false, "",CCID);
                                         }

                                         //resultValue = Negation(drFinancial, resultValue);
                                         drFinancial.SetField<decimal?>("ResultValue", resultValue);
                                     }

                                     if (!dic_Results.ContainsKey("ES" + drFinancial.Field<short>("ElementSequence")))
                                         dic_Results.Add("ES" + drFinancial.Field<short>("ElementSequence"), resultValue);


                                 }


                                 if (drFinancial.Field<string>("ElementName").ToLower().Contains("net profit"))
                                 {
                                 }

                             }


                             try
                             {


                                 foreach (DataRow drFinancial in dtToReturn.Rows)
                                 {

                                     if (drFinancial.Field<string>("ElementType").Equals("D") && !string.IsNullOrEmpty(drFinancial.Field<string>("Formula")))
                                     {
                                         decimal resultValue = 0;

                                         if (!string.IsNullOrEmpty(drFinancial.Field<string>("Formula")))
                                         {


                                             DataView view = new DataView(dtToReturn);
                                             DataTable dtSegmentHeader = view.ToTable(true, "Month", "Months", "yearname");
                                             DataRow[] dr_row = dtSegmentHeader.Select("yearname is null");

                                             for (int i = dr_row.Length - 1; i >= 0; i--)
                                             {
                                                 dtSegmentHeader.Rows.Remove(dr_row[i]);
                                             }
                                             dtSegmentHeader.AcceptChanges();
                                             //  dtSegmentHeader.Rows.Remove(dr_row);

                                             //  dtSegmentHeader = dtSegmentHeader.DefaultView.ToTable(true);
                                             foreach (DataRow drSegmentHeader in dtSegmentHeader.Rows)
                                             {
                                                 resultValue = 0;
                                                 DataRow drdup = drFinancial;

                                                 string formula = drFinancial.Field<string>("Formula");



                                                 while (formula.Contains("["))
                                                 {
                                                     int startingIndex = formula.IndexOf("[");
                                                     int endingIndex = formula.IndexOf("]", startingIndex);
                                                     string variable = formula.Substring(startingIndex + 1, (endingIndex - startingIndex - 1));
                                                     string operat = "";
                                                     try
                                                     { operat = formula.Substring(startingIndex - 1, 0 + 1); }
                                                     catch { }
                                                     if (!dic_Results.ContainsKey(variable))
                                                         dic_Results.Add(variable, resultValue);
                                                     try
                                                     {
                                                         formula = formula.Replace("[" + variable + "]", dic_Results[variable].ToString());

                                                         try
                                                         {

                                                             if (operat == "-")
                                                                 resultValue -= Convert.ToDecimal(dtToReturn.Compute("Sum(ResultValue)", "ResultValue is not null and ElementSequence=" + variable.Replace("ES", "") + " AND Months='" + drSegmentHeader["Months"].ToString() + "' AND yearname='" + drSegmentHeader["yearname"] + "'"));
                                                             //dtToReturn.Rows.Remove(drdup);
                                                             else
                                                                 resultValue += Convert.ToDecimal(dtToReturn.Compute("Sum(ResultValue)", "ResultValue is not null and ElementSequence=" + variable.Replace("ES", "") + " AND Months='" + drSegmentHeader["Months"].ToString() + "' AND yearname='" + drSegmentHeader["yearname"] + "'"));





                                                         }
                                                         catch { resultValue += 0; }

                                                     }
                                                     catch
                                                     {

                                                         // break;

                                                         variable = variable.Replace("ES", "");


                                                         try
                                                         {

                                                             if (operat == "-")
                                                                 resultValue -= Convert.ToDecimal(dtToReturn.Compute("Sum(ResultValue)", "ResultValue is not null and ElementSequence=" + variable));
                                                             else
                                                                 resultValue += Convert.ToDecimal(dtToReturn.Compute("Sum(ResultValue)", "ResultValue is not null and ElementSequence=" + variable));

                                                         }
                                                         catch { resultValue += 0; }
                                                         break;

                                                     }

                                                 }


                                                 if (formula.Contains("/0"))
                                                     resultValue = 0;
                                                 else
                                                     try
                                                     { //resultValue = Convert.ToDecimal(new DataTable().Compute(formula, null));
                                                     }
                                                     catch { }

                                                 //resultValue = Negation(drFinancial, resultValue);
                                                 // drFinancial.SetField<decimal?>("ResultValue", resultValue);
                                                 drdup.SetField<decimal?>("ResultValue", resultValue);
                                                 drdup["Months"] = drSegmentHeader["Months"].ToString();
                                                 drdup["Month"] = drSegmentHeader["Month"].ToString();
                                                 drdup["yearname"] = drSegmentHeader["yearname"].ToString();

                                                 dtToReturn.ImportRow(drdup);
                                             }
                                         }
                                     }
                                     //dtToReturn = dtToReturn.DefaultView.ToTable(true);
                                 }
                                 dtToReturn = dtToReturn.DefaultView.ToTable(true);

                             }
                             catch { }



                         }  //parameter in 
                         else
                         {



                             dtToReturn = dt_FinancialHeader.Copy();
                             dtToReturn.Columns.Add("ResultValue", typeof(decimal));
                             dtToReturn.Columns["ResultValue"].AllowDBNull = true;

                             foreach (DataRow drFinancial in dtToReturn.Rows)
                                 if (drFinancial.Field<string>("ElementName").ToLower().StartsWith("%") && !drFinancial.Field<string>("ElementName").Equals("%NoText"))
                                 {
                                     switch (drFinancial.Field<string>("ElementName").ToLower())
                                     {
                                         case "%period":

                                             if (reportsFilter == "'Balance Sheet'")
                                             {
                                                 drFinancial.SetField<string>("ElementName", "As at " + dateto.Value.ToString("MMMM") + " "
                                                     + dateto.Value.Year);
                                                 dtToReturn.Rows.Remove(drFinancial);
                                             }

                                             else if (reportsFilter == "'Profit And Loss Statement'")
                                             {
                                                 drFinancial.SetField<string>("ElementName", "For the year ended December "
                                                     + dateto.Value.Year);
                                                 dtToReturn.Rows.Remove(drFinancial);
                                             }
                                             else
                                             {

                                                 drFinancial.SetField<string>("ElementName", "For the year ended " + dateto.Value.ToString("MMMM") + " "
                                                         + dateto.Value.Year);
                                                 dtToReturn.Rows.Remove(drFinancial);
                                             }

                                             break;

                                         case "%colheadings":
                                             drFinancial.SetField<decimal?>("ResultValue", dateto.Value.Year);
                                             dtToReturn.Rows.Remove(drFinancial);
                                             break;
                                     }
                                 }
                                 else if (drFinancial.Field<string>("ElementType").Equals("T"))
                                 {
                                     decimal resultValue = 0;

                                     if (!string.IsNullOrEmpty(drFinancial.Field<string>("Formula")))
                                     {
                                         string formula = drFinancial.Field<string>("Formula");

                                         while (formula.Contains("["))
                                         {
                                             int startingIndex = formula.IndexOf("[");
                                             int endingIndex = formula.IndexOf("]", startingIndex);
                                             string variable = formula.Substring(startingIndex + 1, (endingIndex - startingIndex - 1));
                                             try
                                             { formula = formula.Replace("[" + variable + "]", dic_Results[variable].ToString()); }
                                             catch { break; }

                                         }

                                         if (formula.Contains("/0"))
                                             resultValue = 0;
                                         else
                                             try
                                             { resultValue = Convert.ToDecimal(new DataTable().Compute(formula, null)); }
                                             catch { }

                                         resultValue = Negation(drFinancial, resultValue);
                                         drFinancial.SetField<decimal?>("ResultValue", resultValue);
                                     }
                                 }
                                 else if (drFinancial.Field<string>("ElementType").Equals("D"))
                                 {
                                     decimal resultValue = 0;

                                     if (!string.IsNullOrEmpty(drFinancial.Field<string>("Formula")))
                                     {
                                         string formula = drFinancial.Field<string>("Formula");

                                         while (formula.Contains("["))
                                         {
                                             int startingIndex = formula.IndexOf("[");
                                             int endingIndex = formula.IndexOf("]", startingIndex);
                                             string variable = formula.Substring(startingIndex + 1, (endingIndex - startingIndex - 1));
                                             try
                                             { formula = formula.Replace("[" + variable + "]", dic_Results[variable].ToString()); }
                                             catch { break; }

                                         }

                                         if (formula.Contains("/0"))
                                             resultValue = 0;
                                         else
                                             try
                                             { resultValue = Convert.ToDecimal(new DataTable().Compute(formula, null)); }
                                             catch { }

                                         resultValue = Negation(drFinancial, resultValue);
                                         drFinancial.SetField<decimal?>("ResultValue", resultValue);
                                     }
                                     else if (!string.IsNullOrEmpty(drFinancial.Field<string>("NoteNo")))
                                     {
                                         resultValue = GetResultRecursively(companyID.ToString(), periodID, chartOfAccountID, drFinancial.Field<string>("NoteNo"), objConnection, userID,
                                              dateto);

                                         resultValue = Negation(drFinancial, resultValue);
                                         drFinancial.SetField<decimal?>("ResultValue", resultValue);
                                     }
                                     else if (drFinancial.Field<bool>("FixedInput") == true)
                                     {
                                         try
                                         {
                                             decimal detailAmount = 0;

                                             foreach (DataRow item in dt_FinancialDetail.Rows)
                                             {
                                                 if (item.Field<bool>("FixedInput") == true && (drFinancial["FinancialID"].ToString() == item["FinancialHeaderID"].ToString()))
                                                 {
                                                     detailAmount = item["Amount"].ToString() != "" ? item.Field<decimal>("Amount") : 0;
                                                 }
                                             }


                                             resultValue = drFinancial["Amount"].ToString() != "" ? drFinancial.Field<decimal>("Amount") + detailAmount : 0;
                                             drFinancial.SetField<decimal?>("ResultValue", resultValue);
                                         }
                                         catch { }

                                     }
                                     else
                                     {
                                         IEnumerable<DataRow> dr_FinancialDetails = from dr_Detail in dt_FinancialDetail.AsEnumerable()
                                                                                    where dr_Detail.Field<int>("FinancialHeaderID") == drFinancial.Field<int>("FinancialID")
                                                                                    select dr_Detail;



                                         foreach (DataRow dr_FinancialDetail in dr_FinancialDetails)
                                         {
                                             resultValue += GL_TransactionDetail.GetSum(companyID.ToString(), periodID, chartOfAccountID, dr_FinancialDetail.Field<string>("COACodeFrom"),
                                                 dr_FinancialDetail.Field<string>("COACodeTo"), userID, dateto, null, null, null, null, false, "", CCID);
                                         }

                                         resultValue = Negation(drFinancial, resultValue);
                                         drFinancial.SetField<decimal?>("ResultValue", resultValue);
                                     }

                                     if (!dic_Results.ContainsKey("ES" + drFinancial.Field<short>("ElementSequence")))
                                         dic_Results.Add("ES" + drFinancial.Field<short>("ElementSequence"), resultValue);
                                 }
                             dtToReturn.AcceptChanges();
                         }//parameter else


                         //full total 

                         try
                         {


                             foreach (DataRow drFinancial in dtToReturn.Rows)
                             {
                                 if (drFinancial.Field<string>("ElementName").ToLower().Contains("net profit"))
                                 {
                                 }

                                 if (drFinancial.Field<string>("ElementType").Equals("L") && !string.IsNullOrEmpty(drFinancial.Field<string>("Formula")))
                                 {
                                     decimal resultValue = 0;

                                     DataView view = new DataView(dtToReturn);
                                     DataTable dtSegmentHeader = view.ToTable(true, "Month", "Months", "yearname");
                                     DataRow[] dr_row = dtSegmentHeader.Select("yearname is null");

                                     for (int i = dr_row.Length - 1; i >= 0; i--)
                                     {
                                         dtSegmentHeader.Rows.Remove(dr_row[i]);
                                     }
                                     dtSegmentHeader.AcceptChanges();
                                     //  dtSegmentHeader.Rows.Remove(dr_row);

                                     //  dtSegmentHeader = dtSegmentHeader.DefaultView.ToTable(true);
                                     foreach (DataRow drSegmentHeader in dtSegmentHeader.Rows)
                                     {
                                         resultValue = 0;

                                         DataRow drdup = drFinancial;
                                         if (!string.IsNullOrEmpty(drFinancial.Field<string>("Formula")))
                                         {
                                             string formula = drFinancial.Field<string>("Formula");
                                             string operat = "";


                                             while (formula.Contains("["))
                                             {
                                                 int startingIndex = formula.IndexOf("[");
                                                 int endingIndex = formula.IndexOf("]", startingIndex);
                                                 string variable = formula.Substring(startingIndex + 1, (endingIndex - startingIndex - 1));
                                                 try
                                                 { operat = formula.Substring(startingIndex - 1, 0 + 1); }
                                                 catch { }

                                                 if (!dic_Results.ContainsKey(variable))
                                                     dic_Results.Add(variable, resultValue);
                                                 try
                                                 {
                                                     formula = formula.Replace("[" + variable + "]", dic_Results[variable].ToString());

                                                     try
                                                     {


                                                         //dtToReturn.Rows.Remove(drdup);
                                                         //if (operat == "-1")
                                                         //    resultValue -= Convert.ToDecimal(dtToReturn.Compute("Sum(ResultValue)", "ResultValue is not null and ElementType='D' AND Bold=false AND ElementSequence=" + variable.Replace("ES", "") + " AND Months='" + drSegmentHeader["Months"].ToString() + "' AND yearname='" + drSegmentHeader["yearname"] + "'"));
                                                         //else
                                                         //    resultValue += Convert.ToDecimal(dtToReturn.Compute("Sum(ResultValue)", "ResultValue is not null and ElementType='D' AND Bold=false AND ElementSequence=" + variable.Replace("ES", "") + " AND Months='" + drSegmentHeader["Months"].ToString() + "' AND yearname='" + drSegmentHeader["yearname"] + "'"));


                                                         if (operat == "-")
                                                             resultValue -= Convert.ToDecimal(dtToReturn.Compute("Sum(ResultValue)", "ResultValue is not null AND ElementSequence=" + variable.Replace("ES", "") + " AND Months='" + drSegmentHeader["Months"].ToString() + "' AND yearname='" + drSegmentHeader["yearname"] + "'"));
                                                         else
                                                             resultValue += Convert.ToDecimal(dtToReturn.Compute("Sum(ResultValue)", "ResultValue is not null AND ElementSequence=" + variable.Replace("ES", "") + " AND Months='" + drSegmentHeader["Months"].ToString() + "' AND yearname='" + drSegmentHeader["yearname"] + "'"));



                                                         //  resultValue += Convert.ToDecimal(dtToReturn.Compute("Sum(ResultValue)", "ResultValue is not null and ElementSequence=" + variable.Replace("ES", "")));
                                                         //  drFinancial.SetField<decimal?>("ResultValue", resultValue);
                                                     }
                                                     catch { resultValue += 0; }

                                                 }
                                                 catch
                                                 {

                                                     // break;

                                                     variable = variable.Replace("ES", "");


                                                     try
                                                     {
                                                         if (operat == "-1")
                                                             resultValue -= Convert.ToDecimal(dtToReturn.Compute("Sum(ResultValue)", "ResultValue is not null and ElementSequence=" + variable));
                                                         else
                                                             resultValue += Convert.ToDecimal(dtToReturn.Compute("Sum(ResultValue)", "ResultValue is not null and ElementSequence=" + variable));

                                                         drFinancial.SetField<decimal?>("ResultValue", resultValue);
                                                     }
                                                     catch { resultValue += 0; }
                                                     break;

                                                 }

                                             }

                                             if (formula.Contains("/0"))
                                             {
                                                 resultValue = 0;
                                                 drFinancial.SetField<decimal?>("ResultValue", resultValue);
                                             }
                                             else
                                                 try
                                                 { //resultValue = Convert.ToDecimal(new DataTable().Compute(formula, null));
                                                 }
                                                 catch { }

                                             //resultValue = Negation(drFinancial, resultValue);
                                             //  drFinancial.SetField<decimal?>("ResultValue", resultValue);

                                         }

                                         //for (int i = 0; i < dtToReturn.Rows.Count; i++)
                                         //{
                                         //    if (dtToReturn.Rows[i]["ElementName"].ToString() == drdup["ElementName"].ToString())
                                         //    {
                                         //        dtToReturn.Rows.RemoveAt(i);
                                         //    }
                                         //}
                                         // dtToReturn.AcceptChanges();


                                         drdup.SetField<decimal?>("ResultValue", resultValue);
                                         drdup["Months"] = drSegmentHeader["Months"].ToString();
                                         drdup["Month"] = drSegmentHeader["Month"].ToString();
                                         drdup["yearname"] = drSegmentHeader["yearname"].ToString();

                                         dtToReturn.ImportRow(drdup);



                                     }
                                 }
                             }
                         }
                         catch { }


                     }
                 }
                 catch (Exception ex)
                 {
                     SqlHelper.LogException(ex);
                     throw ex;
                 }
                 finally
                 {
                     ConnectionHelper.CloseConnection(objConnection);
                 }
                 dtToReturn = dtToReturn.DefaultView.ToTable(true);
                 return dtToReturn;
             }

             public static DataTable GetCustomerSummaryReport(string companyID, int periodID, Nullable<int> customer, Nullable<int> customerto,
                Nullable<DateTime> datefrom, Nullable<DateTime> dateto, Guid userID, Nullable<int> bpType = null, Nullable<short> documentGroupID = null,
                bool documentTypeAuthentication = false, bool discardReversedDocuments = false, int? CustomerCategoryID = null, string AccountCode = "", string BalanceFilter = "", bool WithType = false,string DocumentType = "")
             {
                 companyID = "(" + companyID + ")";
                 DataTable dtToReturn = null;
                 StringBuilder sb = new StringBuilder();
                 StringBuilder ss = new StringBuilder();
                 string qry_docuTypeAuthenticate = string.Empty;
                 string qry_SalesInvoiceFilter = string.Empty, qry_ReceiptFilter = string.Empty, qry_PurchaseInvoiceFilter = string.Empty,
                     qry_PaymentFilter = string.Empty;

                 #region Filter Reversed Documents

                 if (discardReversedDocuments)
                 {
                     qry_SalesInvoiceFilter = " And IsNull(Sal_InvoiceHeader.IsReversed, 0) = 0 And Sal_InvoiceHeader.ReversalID is null ";
                     qry_ReceiptFilter = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                     qry_PurchaseInvoiceFilter = " And IsNull(AR_InvoiceHeader.IsReversed, 0) = 0 And AR_InvoiceHeader.ReversalID is null ";
                     qry_PaymentFilter = " And AP_PaymentHeader.ReversalComments is null ";
                 }

                 #endregion


                 #region query

                 string Query_BusinessPartnerTrailBalance = FnI.QueryResourceLibrary.GL.GL_TransactionHeader2.Query_CustomerSummary.ToString().Replace("@CompanyID", companyID);

                 if (WithType)
                 {
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithType", " ,LastTab.BusinessPartnerName ");
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithGroup", " ,LastTab.BusinessPartnerName ");
                 }
                 else
                 {
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithType", " ,'' as BusinessPartnerName ");
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithGroup", " ");
                 }




                 #endregion

                 try
                 {
                     SqlParameter[] sqlparams = new SqlParameter[6];
                     sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                     sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                     sqlparams[2] = new SqlParameter("@FromDate", datefrom);
                     sqlparams[3] = new SqlParameter("@ToDate", dateto);


                     if (customer != null)
                         sb.Append(" and AR_SET_Customer.CustomerID between " + customer + "  and " + customerto + "");

                     if (CustomerCategoryID != null)
                         sb.Append(" and AR_SET_Customer.CustomerCategoryID = " + CustomerCategoryID.ToString() + "");

                     if (bpType.HasValue)
                         ss.Append("and AR_SET_BusinessPartnerTypes.TypeCode = " + bpType);

                     if (documentGroupID.HasValue)
                         ss.Append(" AND Set_DocumentType.GroupID = " + documentGroupID);

                     if (DocumentType != "")
                         ss.Append(" and Set_DocumentType.DocumentType like '%" + DocumentType + "%' ");

                     if (AccountCode != "")
                         ss.Append("and AR_SET_BusinessPartnerTypes.FK_CodeCombinationID = '" + AccountCode + "'");


                     if (documentTypeAuthentication)
                     {
                         sqlparams[4] = new SqlParameter("@UserID", userID);
                         //                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
                         //                    AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                     }

                     sqlparams[5] = new SqlParameter("@AccountCode", DBNull.Value);

                     string CustoemrFilter = "";
                     //if (customer.HasValue)
                     //{
                     //    CustoemrFilter = " WHERE CustomerID = " + customer + "";
                     //}

                     string BalanceWiseFilter = "";
                     if (BalanceFilter != "")
                     {
                         if (BalanceFilter == "Credit")
                             BalanceWiseFilter = " where ClosingAmountType = 'Credit'";

                         if (BalanceFilter == "Debit")
                             BalanceWiseFilter = " where ClosingAmountType = 'Debit'";
                     }

                     DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_BusinessPartnerTrailBalance + BalanceWiseFilter, sb.ToString(), ss.ToString(),
                         qry_docuTypeAuthenticate, qry_SalesInvoiceFilter, qry_ReceiptFilter, qry_PurchaseInvoiceFilter, qry_PaymentFilter, qry_docuTypeAuthenticate) + CustoemrFilter,
                         sqlparams);

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

             public static DataTable Get_BusinessPartnerGL_ReportDataEmployeeWise(string companyID, int periodID, Nullable<int> businessPartnerID, Nullable<DateTime> dateFrom, Nullable<int> DocumentTypeID, string Chartofacc, string ChartofaccTo,
                 string businessPartnerType, Nullable<DateTime> dateTo, int CurrentFiscalID, int LocationID = 0, int CostCenterID = 0, string documentGroupID = "", string Comments = "", int? CustomerCategoryID = null, int? BrokerID = null, int? EmployeStatusID = 0) //int? TypeCode = 0,
             {
                 List<SqlParameter> sqlParams = new List<SqlParameter>();
                 StringBuilder generalLedgerFilter = new StringBuilder();
                 StringBuilder paymentFilter = new StringBuilder();
                 StringBuilder purchaseInvoiceSupplierFilter = new StringBuilder();
                 StringBuilder SaleInvoiceFilter = new StringBuilder();
                 StringBuilder OnlyCustomerfilter = new StringBuilder();
                 StringBuilder LocationFillter = new StringBuilder();
                 StringBuilder CostCenterFillter = new StringBuilder();
                 StringBuilder documentGroupFilter = new StringBuilder();
                 DataTable dtToReturn = null;
                 StringBuilder sbCC = new StringBuilder();
                 StringBuilder sbCC2 = new StringBuilder();

                 StringBuilder AccountCodeFilter = new StringBuilder();
                 StringBuilder CommentsFilter = new StringBuilder();

                 try
                 {
                     sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                     sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                     sqlParams.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));
                     sqlParams.Add(new SqlParameter("@FromDate", ((DateTime)dateFrom).ToString("yyyy-MM-dd")));
                     sqlParams.Add(new SqlParameter("@ToDate", ((DateTime)dateTo).ToString("yyyy-MM-dd")));

                     companyID = "(" + companyID + ")";

                     //generalLedgerFilter.Append(" and AR_Set_Customer.CustomerID in (select BusinessPartnerID from AR_SET_BusinessPartnerTypes where  TypeCode = " + 0 + " )");
                     //paymentFilter.Append(" and AR_Set_Customer.CustomerID in (select BusinessPartnerID from AR_SET_BusinessPartnerTypes where  TypeCode = " + 0 + " )");
                     //purchaseInvoiceSupplierFilter.Append(" And AR_InvoiceSupplierDetail.SupplierID in (select BusinessPartnerID from AR_SET_BusinessPartnerTypes where  TypeCode = " + 0 + " )");
                     //SaleInvoiceFilter.Append(" and TABInvoice.CustomerID in (select BusinessPartnerID from AR_SET_BusinessPartnerTypes where  TypeCode = " + 0 + " )");
                     //OnlyCustomerfilter.Append(" and AR_Set_Customer.CustomerID in (select BusinessPartnerID from AR_SET_BusinessPartnerTypes where  TypeCode = " + 0 + " )");

                     if (businessPartnerID.HasValue)
                     {
                         generalLedgerFilter.Append(" and AR_Set_Customer.CustomerID = " + businessPartnerID + " ");
                         paymentFilter.Append(" and AR_Set_Customer.CustomerID = " + businessPartnerID + " ");
                         purchaseInvoiceSupplierFilter.Append(" And AR_InvoiceSupplierDetail.SupplierID = " + businessPartnerID + " ");
                         SaleInvoiceFilter.Append(" and TABInvoice.CustomerID = " + businessPartnerID + " ");
                         OnlyCustomerfilter.Append(" and AR_Set_Customer.CustomerID = " + businessPartnerID + "");
                         sqlParams.Add(new SqlParameter("@BusinessPartnerID", businessPartnerID));

                     }
                     else
                         sqlParams.Add(new SqlParameter("@BusinessPartnerID", DBNull.Value));

                     if (Chartofacc != "" && ChartofaccTo != "")
                     {
                         sqlParams.Add(new SqlParameter("@AccountCode", Chartofacc));
                         sqlParams.Add(new SqlParameter("@AccountCodeTo", ChartofaccTo));
                         AccountCodeFilter.Append(" and  (GL_codecombinationDetail.CodeCombinationID between '" + Chartofacc + "' and '" + ChartofaccTo + "') ");
                     }
                     else if (Chartofacc != "")
                     {
                         sqlParams.Add(new SqlParameter("@AccountCode", Chartofacc));
                         AccountCodeFilter.Append(" and  (GL_codecombinationDetail.CodeCombinationID = '" + Chartofacc + "' ) ");

                     }
                     else if (ChartofaccTo != "")
                     {
                         sqlParams.Add(new SqlParameter("@AccountCodeTo", ChartofaccTo));
                         AccountCodeFilter.Append(" and  (GL_codecombinationDetail.CodeCombinationID = '" + ChartofaccTo + "' ) ");
                     }

                     if (BrokerID.HasValue)
                     {
                         generalLedgerFilter.Append(" and AR_Set_Customer.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                         paymentFilter.Append(" and AR_Set_Customer.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                         purchaseInvoiceSupplierFilter.Append(" And AR_InvoiceSupplierDetail.SupplierID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                         SaleInvoiceFilter.Append(" and TABInvoice.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                         OnlyCustomerfilter.Append(" and AR_Set_Customer.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                     }


                     if (DocumentTypeID.HasValue)
                     {
                         sqlParams.Add(new SqlParameter("@DocumentTypeID", DocumentTypeID));
                     }
                     else
                     {
                         sqlParams.Add(new SqlParameter("@DocumentTypeID", DBNull.Value));
                     }

                     if(EmployeStatusID > 0)
                         sqlParams.Add(new SqlParameter("@StatusID", EmployeStatusID));
                     else
                         sqlParams.Add(new SqlParameter("@StatusID", DBNull.Value));


                     if (LocationID > 0)
                         sqlParams.Add(new SqlParameter("@LocationID", LocationID));
                     else
                         sqlParams.Add(new SqlParameter("@LocationID", DBNull.Value));
                         //LocationFillter.Append("and GL_TransactionHeader.LocationID = " + LocationID + "");

                     if (CustomerCategoryID.HasValue)
                     {
                         sbCC.Append(" left join AR_SET_Customer as CusCat on CusCat.CustomerID = tab.CustomerID ");
                         sbCC2.Append(" where CusCat.CustomerCategoryID = " + CustomerCategoryID.ToString() + "");
                     }

                     if (Comments != "" && CustomerCategoryID.HasValue)
                     {
                         CommentsFilter.Append(" and isnull(tab.Comments,'') like '%" + Comments.ToString() + "%' ");
                     }
                     else if (Comments != "")
                     {
                         CommentsFilter.Append(" where isnull(tab.Comments,'') like '%" + Comments.ToString() + "%' ");
                     }

                     if (CostCenterID > 0)
                     {
                         CostCenterFillter.Append(" AND Set_CostCenter.CostCenterID = " + CostCenterID + " ");
                     }

                     #region Document Group new group
                     if (documentGroupID != "")
                         documentGroupFilter.Append(" AND Set_DocumentType.GroupID in " + documentGroupID);
                     #endregion


                     string Query = string.Format(FnI.QueryResourceLibrary.GL.GL_TransactionHeader3.Query_BusinessPartnerGl_EmployeeWise,
                         generalLedgerFilter.ToString(), paymentFilter.ToString(), purchaseInvoiceSupplierFilter.ToString(), SaleInvoiceFilter.ToString(), OnlyCustomerfilter.ToString(), LocationFillter.ToString(), documentGroupFilter.ToString(), sbCC.ToString(), sbCC2.ToString(), AccountCodeFilter.ToString(), CommentsFilter.ToString(), CostCenterFillter.ToString()).Replace("@CompanyID", companyID);


                     if (DocumentTypeID.HasValue)
                         Query = Query.Replace("@DocumentTypeID", DocumentTypeID.ToString());
                     else
                         Query = Query.Replace("@DocumentTypeID", "null");



                     if (businessPartnerID.HasValue)
                         Query = Query.Replace("@BusinessPartnerID", businessPartnerID.ToString());
                     else
                         Query = Query.Replace("@BusinessPartnerID", "null");


                     Query = Query.Replace("@PeriodID", periodID.ToString());
                     Query = Query.Replace("@CurrentFiscalID", CurrentFiscalID.ToString());

                     Query = Query.Replace("@FromDate", "'" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                     Query = Query.Replace("@ToDate", "'" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");


                     if (Chartofacc != "" && ChartofaccTo != "")
                     {
                         Query = Query.Replace("@AccountCodeTo", "'" + ChartofaccTo + "'");
                         Query = Query.Replace("@AccountCode", "'" + Chartofacc + "'");

                     }
                     else
                     {
                         Query = Query.Replace("@AccountCodeTo", "null");
                         Query = Query.Replace("@AccountCode", "null");

                     }


                     DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlParams.ToArray());


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


             public static DataTable ProvidentFundRegisterBPWise(string companyID, int periodID, Nullable<int> customer, Nullable<int> customerto,
               Nullable<DateTime> datefrom, Nullable<DateTime> dateto, Guid userID, string bpType, string documentGroupID = "", bool WithOrderByGL = false,
               bool documentTypeAuthentication = false, bool discardReversedDocuments = false, int? CustomerCategoryID = null, string AccountCode = "", string BalanceFilter = "", bool WithType = false, bool WithConsolidated = false, bool isMonthwise = false)
             {
                 companyID = "(" + companyID + ")";
                 if (!string.IsNullOrEmpty(bpType))
                     bpType = "(" + bpType + ")";

                 DataTable dtToReturn = null;
                 StringBuilder sb = new StringBuilder();
                 StringBuilder ss = new StringBuilder();
                 string qry_docuTypeAuthenticate = string.Empty;
                 string qry_SalesInvoiceFilter = string.Empty, qry_ReceiptFilter = string.Empty, qry_PurchaseInvoiceFilter = string.Empty,
                     qry_PaymentFilter = string.Empty;

                 #region Filter Reversed Documents

                 if (discardReversedDocuments)
                 {
                     qry_SalesInvoiceFilter = " And IsNull(Sal_InvoiceHeader.IsReversed, 0) = 0 And Sal_InvoiceHeader.ReversalID is null ";
                     qry_ReceiptFilter = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                     qry_PurchaseInvoiceFilter = " And IsNull(AR_InvoiceHeader.IsReversed, 0) = 0 And AR_InvoiceHeader.ReversalID is null ";
                     qry_PaymentFilter = " And AP_PaymentHeader.ReversalComments is null ";
                 }

                 #endregion


                 #region query

                 string Query_BusinessPartnerTrailBalance = FnI.QueryResourceLibrary.GL.GL_TransactionHeader6.Query_ProvidentFundRegisterBPWise.ToString().Replace("@CompanyID", companyID);


                 if (WithType)
                 {
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithType", " ,LastTab.BusinessPartnerName ");
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithGroup", " ,LastTab.BusinessPartnerName ");
                 }
                 else
                 {
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithType", " ,'' as BusinessPartnerName ");
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithGroup", " ");
                 }


                 if (WithConsolidated)
                 {
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithConsolidated", " ,'' as BusinessPartnerTypeCode ");
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@ConsolidatedGroup", " ");
                 }
                 else
                 {
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithConsolidated", " ,AR_SET_BusinessPartnerTypes.FK_CodeCombinationID as BusinessPartnerTypeCode");
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@ConsolidatedGroup", " ,AR_SET_BusinessPartnerTypes.FK_CodeCombinationID");
                 }


                 if (WithOrderByGL)
                 {
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithOrderByGL", " Order by fintab.BusinessPartnerTypeCode asc");
                 }
                 else
                 {
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithOrderByGL", " ");
                 }

                 #endregion

                 try
                 {
                     SqlParameter[] sqlparams = new SqlParameter[6];
                     sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                     sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                     sqlparams[2] = new SqlParameter("@FromDate", datefrom);
                     sqlparams[3] = new SqlParameter("@ToDate", dateto);


                     if (customer != null)
                         sb.Append(" and AR_SET_Customer.CustomerID between " + customer + "  and " + customerto + "");

                     if (CustomerCategoryID != null)
                         sb.Append(" and AR_SET_Customer.CustomerCategoryID = " + CustomerCategoryID.ToString() + "");

                     if (bpType != null)
                         ss.Append("and AR_SET_BusinessPartnerTypes.TypeCode in " + bpType);

                     if (documentGroupID != "")
                         ss.Append(" AND Set_DocumentType.GroupID in " + documentGroupID);

                     if (AccountCode != "")
                         ss.Append("and AR_SET_BusinessPartnerTypes.FK_CodeCombinationID = '" + AccountCode + "'");


                     if (documentTypeAuthentication)
                     {
                         sqlparams[4] = new SqlParameter("@UserID", userID);
                         //                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
                         //                    AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                     }

                     sqlparams[5] = new SqlParameter("@AccountCode", DBNull.Value);

                     string CustoemrFilter = "";
                     //if (customer.HasValue)
                     //{
                     //    CustoemrFilter = " WHERE CustomerID = " + customer + "";
                     //}

                     string BalanceWiseFilter = "";
                     if (BalanceFilter != "")
                     {
                         if (BalanceFilter == "Credit")
                             BalanceWiseFilter = " where ClosingAmountType = 'Credit'";

                         if (BalanceFilter == "Debit")
                             BalanceWiseFilter = " where ClosingAmountType = 'Debit'";
                     }

                     DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_BusinessPartnerTrailBalance + BalanceWiseFilter, sb.ToString(), ss.ToString(),
                         qry_docuTypeAuthenticate, qry_SalesInvoiceFilter, qry_ReceiptFilter, qry_PurchaseInvoiceFilter, qry_PaymentFilter, qry_docuTypeAuthenticate) + CustoemrFilter,
                         sqlparams);

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


             public static DataTable ProvidentFundRegisterBPWiseCompany(string companyID, int periodID, Nullable<int> customer, Nullable<int> customerto,
                    Nullable<DateTime> datefrom, Nullable<DateTime> dateto, Guid userID, string bpType, string documentGroupID = "", bool WithOrderByGL = false,
                    bool documentTypeAuthentication = false, bool discardReversedDocuments = false, int? CustomerCategoryID = null, string AccountCode = "", string BalanceFilter = "", bool WithType = false, bool WithConsolidated = false, bool isMonthwise = false)
             {
                 companyID = "(" + companyID + ")";
                 if (!string.IsNullOrEmpty(bpType))
                     bpType = "(" + bpType + ")";

                 DataTable dtToReturn = null;
                 StringBuilder sb = new StringBuilder();
                 StringBuilder ss = new StringBuilder();
                 string qry_docuTypeAuthenticate = string.Empty;
                 string qry_SalesInvoiceFilter = string.Empty, qry_ReceiptFilter = string.Empty, qry_PurchaseInvoiceFilter = string.Empty,
                     qry_PaymentFilter = string.Empty;

                 #region Filter Reversed Documents

                 if (discardReversedDocuments)
                 {
                     qry_SalesInvoiceFilter = " And IsNull(Sal_InvoiceHeader.IsReversed, 0) = 0 And Sal_InvoiceHeader.ReversalID is null ";
                     qry_ReceiptFilter = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                     qry_PurchaseInvoiceFilter = " And IsNull(AR_InvoiceHeader.IsReversed, 0) = 0 And AR_InvoiceHeader.ReversalID is null ";
                     qry_PaymentFilter = " And AP_PaymentHeader.ReversalComments is null ";
                 }

                 #endregion


                 #region query

                 string Query_BusinessPartnerTrailBalance = FnI.QueryResourceLibrary.GL.GL_TransactionHeader6.Query_ProvidentFundRegisterBPWiseCompany.ToString().Replace("@CompanyID", companyID);


                 if (WithType)
                 {
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithType", " ,LastTab.BusinessPartnerName ");
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithGroup", " ,LastTab.BusinessPartnerName ");
                 }
                 else
                 {
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithType", " ,'' as BusinessPartnerName ");
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithGroup", " ");
                 }


                 if (WithConsolidated)
                 {
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithConsolidated", " ,'' as BusinessPartnerTypeCode ");
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@ConsolidatedGroup", " ");
                 }
                 else
                 {
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithConsolidated", " ,AR_SET_BusinessPartnerTypes.FK_CodeCombinationID as BusinessPartnerTypeCode");
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@ConsolidatedGroup", " ,AR_SET_BusinessPartnerTypes.FK_CodeCombinationID");
                 }


                 if (WithOrderByGL)
                 {
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithOrderByGL", " Order by fintab.BusinessPartnerTypeCode asc");
                 }
                 else
                 {
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithOrderByGL", " ");
                 }

                 #endregion

                 try
                 {
                     SqlParameter[] sqlparams = new SqlParameter[6];
                     sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                     sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                     sqlparams[2] = new SqlParameter("@FromDate", datefrom);
                     sqlparams[3] = new SqlParameter("@ToDate", dateto);


                     if (customer != null)
                         sb.Append(" and AR_SET_Customer.CustomerID between " + customer + "  and " + customerto + "");

                     if (CustomerCategoryID != null)
                         sb.Append(" and AR_SET_Customer.CustomerCategoryID = " + CustomerCategoryID.ToString() + "");

                     if (bpType != null)
                         ss.Append("and AR_SET_BusinessPartnerTypes.TypeCode in " + bpType);

                     if (documentGroupID != "")
                         ss.Append(" AND Set_DocumentType.GroupID in " + documentGroupID);

                     if (AccountCode != "")
                         ss.Append("and AR_SET_BusinessPartnerTypes.FK_CodeCombinationID = '" + AccountCode + "'");


                     if (documentTypeAuthentication)
                     {
                         sqlparams[4] = new SqlParameter("@UserID", userID);
                         //                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
                         //                    AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                     }

                     sqlparams[5] = new SqlParameter("@AccountCode", DBNull.Value);

                     string CustoemrFilter = "";
                     //if (customer.HasValue)
                     //{
                     //    CustoemrFilter = " WHERE CustomerID = " + customer + "";
                     //}

                     string BalanceWiseFilter = "";
                     if (BalanceFilter != "")
                     {
                         if (BalanceFilter == "Credit")
                             BalanceWiseFilter = " where ClosingAmountType = 'Credit'";

                         if (BalanceFilter == "Debit")
                             BalanceWiseFilter = " where ClosingAmountType = 'Debit'";
                     }

                     DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_BusinessPartnerTrailBalance + BalanceWiseFilter, sb.ToString(), ss.ToString(),
                         qry_docuTypeAuthenticate, qry_SalesInvoiceFilter, qry_ReceiptFilter, qry_PurchaseInvoiceFilter, qry_PaymentFilter, qry_docuTypeAuthenticate) + CustoemrFilter,
                         sqlparams);

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

             public static DataTable StaffProvidentFund(string companyID, int periodID, Nullable<int> customer, Nullable<int> customerto,
                    Nullable<DateTime> datefrom, Nullable<DateTime> dateto, Guid userID, string bpType, string documentGroupID = "", bool WithOrderByGL = false,
                    bool documentTypeAuthentication = false, bool discardReversedDocuments = false, int? CustomerCategoryID = null, string AccountCode = "", string BalanceFilter = "", bool WithType = false, bool WithConsolidated = false, bool isMonthwise = false)
             {
                 companyID = "(" + companyID + ")";
                 if (!string.IsNullOrEmpty(bpType))
                     bpType = "(" + bpType + ")";

                 DataTable dtToReturn = null;
                 StringBuilder sb = new StringBuilder();
                 StringBuilder ss = new StringBuilder();
                 string qry_docuTypeAuthenticate = string.Empty;
                 string qry_SalesInvoiceFilter = string.Empty, qry_ReceiptFilter = string.Empty, qry_PurchaseInvoiceFilter = string.Empty,
                     qry_PaymentFilter = string.Empty;

                 #region Filter Reversed Documents

                 if (discardReversedDocuments)
                 {
                     qry_SalesInvoiceFilter = " And IsNull(Sal_InvoiceHeader.IsReversed, 0) = 0 And Sal_InvoiceHeader.ReversalID is null ";
                     qry_ReceiptFilter = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                     qry_PurchaseInvoiceFilter = " And IsNull(AR_InvoiceHeader.IsReversed, 0) = 0 And AR_InvoiceHeader.ReversalID is null ";
                     qry_PaymentFilter = " And AP_PaymentHeader.ReversalComments is null ";
                 }

                 #endregion


                 #region query

                 string Query_BusinessPartnerTrailBalance = FnI.QueryResourceLibrary.GL.GL_TransactionHeader6.Query_StaffProvidentFund.ToString().Replace("@CompanyID", companyID);


                 if (WithType)
                 {
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithType", " ,LastTab.BusinessPartnerName ");
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithGroup", " ,LastTab.BusinessPartnerName ");
                 }
                 else
                 {
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithType", " ,'' as BusinessPartnerName ");
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithGroup", " ");
                 }


                 if (WithConsolidated)
                 {
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithConsolidated", " ,'' as BusinessPartnerTypeCode ");
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@ConsolidatedGroup", " ");
                 }
                 else
                 {
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithConsolidated", " ,AR_SET_BusinessPartnerTypes.FK_CodeCombinationID as BusinessPartnerTypeCode");
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@ConsolidatedGroup", " ,AR_SET_BusinessPartnerTypes.FK_CodeCombinationID");
                 }


                 if (WithOrderByGL)
                 {
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithOrderByGL", " Order by fintab.BusinessPartnerTypeCode asc");
                 }
                 else
                 {
                     Query_BusinessPartnerTrailBalance = Query_BusinessPartnerTrailBalance.ToString().Replace("@WithOrderByGL", " ");
                 }

                 #endregion

                 try
                 {
                     SqlParameter[] sqlparams = new SqlParameter[6];
                     sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                     sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                     sqlparams[2] = new SqlParameter("@FromDate", datefrom);
                     sqlparams[3] = new SqlParameter("@ToDate", dateto);


                     if (customer != null)
                         sb.Append(" and AR_SET_Customer.CustomerID between " + customer + "  and " + customerto + "");

                     if (CustomerCategoryID != null)
                         sb.Append(" and AR_SET_Customer.CustomerCategoryID = " + CustomerCategoryID.ToString() + "");

                     if (bpType != null)
                         ss.Append("and AR_SET_BusinessPartnerTypes.TypeCode in " + bpType);

                     if (documentGroupID != "")
                         ss.Append(" AND Set_DocumentType.GroupID in " + documentGroupID);

                     if (AccountCode != "")
                         ss.Append("and AR_SET_BusinessPartnerTypes.FK_CodeCombinationID = '" + AccountCode + "'");


                     if (documentTypeAuthentication)
                     {
                         sqlparams[4] = new SqlParameter("@UserID", userID);
                         //                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
                         //                    AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                     }

                     sqlparams[5] = new SqlParameter("@AccountCode", DBNull.Value);

                     string CustoemrFilter = "";
                     //if (customer.HasValue)
                     //{
                     //    CustoemrFilter = " WHERE CustomerID = " + customer + "";
                     //}

                     string BalanceWiseFilter = "";
                     if (BalanceFilter != "")
                     {
                         if (BalanceFilter == "Credit")
                             BalanceWiseFilter = " where ClosingAmountType = 'Credit'";

                         if (BalanceFilter == "Debit")
                             BalanceWiseFilter = " where ClosingAmountType = 'Debit'";
                     }

                     DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_BusinessPartnerTrailBalance + BalanceWiseFilter, sb.ToString(), ss.ToString(),
                         qry_docuTypeAuthenticate, qry_SalesInvoiceFilter, qry_ReceiptFilter, qry_PurchaseInvoiceFilter, qry_PaymentFilter, qry_docuTypeAuthenticate) + CustoemrFilter,
                         sqlparams);

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

             public static DataTable GetProvidentFundData_For_Com_And_Mem(string companyID, int periodID, Nullable<DateTime> datefrom, Nullable<DateTime> dateto)
             {
                 companyID = "(" + companyID + ")";

                 DataTable dtToReturn = null;

                 try
                 {
                     SqlParameter[] sqlparams = new SqlParameter[6];
                     sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                     sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                     sqlparams[2] = new SqlParameter("@FromDate", datefrom);
                     sqlparams[3] = new SqlParameter("@ToDate", dateto);

                     DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionHeader6.Query_GetProvidentFundData_For_Com_And_Mem.ToString().Replace("@CompanyID", companyID), sqlparams);

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


             public static DataTable Get_CreditAgingReport_HSM(string companyID, int periodID, Nullable<int> businessPartnerID, Nullable<DateTime> dateFrom, Nullable<int> DocumentTypeID, string Chartofacc, string ChartofaccTo,
                string businessPartnerType, Nullable<DateTime> dateTo, int CurrentFiscalID, int LocationID = 0, int CostCenterID = 0, string documentGroupID = "", string Comments = "", int? CustomerCategoryID = null, int? BrokerID = null, int? EmployeeStatus = null)
             {
                 List<SqlParameter> sqlParams = new List<SqlParameter>();
                 StringBuilder generalLedgerFilter = new StringBuilder();
                 StringBuilder paymentFilter = new StringBuilder();
                 StringBuilder purchaseInvoiceSupplierFilter = new StringBuilder();
                 StringBuilder SaleInvoiceFilter = new StringBuilder();
                 StringBuilder OnlyCustomerfilter = new StringBuilder();
                 StringBuilder LocationFillter = new StringBuilder();
                 StringBuilder CostCenterFillter = new StringBuilder();
                 StringBuilder documentGroupFilter = new StringBuilder();
                 DataTable dtToReturn = null;
                 StringBuilder sbCC = new StringBuilder();
                 StringBuilder sbCC2 = new StringBuilder();

                 StringBuilder AccountCodeFilter = new StringBuilder();
                 StringBuilder CommentsFilter = new StringBuilder();
                 string businessPartnerTypeFilter = string.Empty, businessPartnerTypeFilter2 = string.Empty;

                 try
                 {
                     sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                     sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                     sqlParams.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));
                     sqlParams.Add(new SqlParameter("@FromDate", ((DateTime)dateFrom).ToString("yyyy-MM-dd")));
                     sqlParams.Add(new SqlParameter("@ToDate", ((DateTime)dateTo).ToString("yyyy-MM-dd")));

                     companyID = "(" + companyID + ")";

                     if (businessPartnerID.HasValue)
                     {
                         generalLedgerFilter.Append(" and AR_Set_Customer.CustomerID = " + businessPartnerID + " ");
                         paymentFilter.Append(" and AR_Set_Customer.CustomerID = " + businessPartnerID + " ");
                         purchaseInvoiceSupplierFilter.Append(" And AR_InvoiceSupplierDetail.SupplierID = " + businessPartnerID + " ");
                         SaleInvoiceFilter.Append(" and TABInvoice.CustomerID = " + businessPartnerID + " ");
                         OnlyCustomerfilter.Append(" and AR_Set_Customer.CustomerID = " + businessPartnerID + "");
                         sqlParams.Add(new SqlParameter("@BusinessPartnerID", businessPartnerID));

                     }
                     else
                         sqlParams.Add(new SqlParameter("@BusinessPartnerID", DBNull.Value));

                     if (Chartofacc != "" && ChartofaccTo != "")
                     {
                         sqlParams.Add(new SqlParameter("@AccountCode", Chartofacc));
                         sqlParams.Add(new SqlParameter("@AccountCodeTo", ChartofaccTo));
                         AccountCodeFilter.Append(" and  (GL_codecombinationDetail.CodeCombinationID between '" + Chartofacc + "' and '" + ChartofaccTo + "') ");
                     }
                     else if (Chartofacc != "")
                     {
                         sqlParams.Add(new SqlParameter("@AccountCode", Chartofacc));
                         AccountCodeFilter.Append(" and  (GL_codecombinationDetail.CodeCombinationID = '" + Chartofacc + "' ) ");

                     }
                     else if (ChartofaccTo != "")
                     {
                         sqlParams.Add(new SqlParameter("@AccountCodeTo", ChartofaccTo));
                         AccountCodeFilter.Append(" and  (GL_codecombinationDetail.CodeCombinationID = '" + ChartofaccTo + "' ) ");
                     }

                     if (BrokerID.HasValue)
                     {
                         generalLedgerFilter.Append(" and AR_Set_Customer.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                         paymentFilter.Append(" and AR_Set_Customer.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                         purchaseInvoiceSupplierFilter.Append(" And AR_InvoiceSupplierDetail.SupplierID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                         SaleInvoiceFilter.Append(" and TABInvoice.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                         OnlyCustomerfilter.Append(" and AR_Set_Customer.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )");
                     }


                     if (DocumentTypeID.HasValue)
                     {
                         sqlParams.Add(new SqlParameter("@DocumentTypeID", DocumentTypeID));
                     }
                     else
                     {
                         sqlParams.Add(new SqlParameter("@DocumentTypeID", DBNull.Value));
                     }
                     if (LocationID > 0)
                         LocationFillter.Append("and GL_TransactionHeader.LocationID = " + LocationID + "");

                     if (CustomerCategoryID.HasValue)
                     {
                         sbCC.Append(" left join AR_SET_Customer as CusCat on CusCat.CustomerID = tab.CustomerID ");
                         sbCC2.Append(" where CusCat.CustomerCategoryID = " + CustomerCategoryID.ToString() + "");
                     }

                     if (Comments != "" && CustomerCategoryID.HasValue)
                     {
                         CommentsFilter.Append(" and isnull(tab.Comments,'') like '%" + Comments.ToString() + "%' ");
                     }
                     else if (Comments != "")
                     {
                         CommentsFilter.Append(" where isnull(tab.Comments,'') like '%" + Comments.ToString() + "%' ");
                     }

                     if (CostCenterID > 0)
                     {
                         CostCenterFillter.Append(" AND Set_CostCenter.CostCenterID = " + CostCenterID + " ");
                     }

                     #region Document Group new group
                     if (documentGroupID != "")
                         documentGroupFilter.Append(" AND Set_DocumentType.GroupID in " + documentGroupID);
                     #endregion


                     if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                     {
                         businessPartnerTypeFilter = " AND AR_SET_BusinessPartnerTypes.TypeCode in (" + businessPartnerType + ") ";
                     }

                     if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                     {
                         businessPartnerTypeFilter2 = " AND TABInvoice.TypeCode in (" + businessPartnerType + ") ";
                     }



                     if (EmployeeStatus.HasValue)
                     {

                         generalLedgerFilter.Append(" and AR_SET_Customer.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ") ");
                         paymentFilter.Append(" and AR_SET_Customer.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ")");
                         purchaseInvoiceSupplierFilter.Append(" And AR_InvoiceSupplierDetail.SupplierID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ") ");
                         SaleInvoiceFilter.Append(" and TABInvoice.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ") ");
                         OnlyCustomerfilter.Append(" and AR_Set_Customer.CustomerID in (select FK_CustomerID  from hrms_set_employee where StatusID = " + EmployeeStatus + ")");
                     }


                     string Query = string.Format(FnI.QueryResourceLibrary.GL.GL_TransactionHeader4.Query_CreditAgingReport_HSM,
                         generalLedgerFilter.ToString(), paymentFilter.ToString(), purchaseInvoiceSupplierFilter.ToString(), SaleInvoiceFilter.ToString(), OnlyCustomerfilter.ToString(), LocationFillter.ToString(), documentGroupFilter.ToString(), sbCC.ToString(), sbCC2.ToString(), AccountCodeFilter.ToString(), CommentsFilter.ToString(), CostCenterFillter.ToString(), businessPartnerTypeFilter.ToString()).Replace("@CompanyID", companyID);


                     if (DocumentTypeID.HasValue)
                         Query = Query.Replace("@DocumentTypeID", DocumentTypeID.ToString());
                     else
                         Query = Query.Replace("@DocumentTypeID", "null");



                     if (businessPartnerID.HasValue)
                         Query = Query.Replace("@BusinessPartnerID", businessPartnerID.ToString());
                     else
                         Query = Query.Replace("@BusinessPartnerID", "null");


                     Query = Query.Replace("@PeriodID", periodID.ToString());
                     Query = Query.Replace("@CurrentFiscalID", CurrentFiscalID.ToString());

                     Query = Query.Replace("@FromDate", "'" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                     Query = Query.Replace("@ToDate", "'" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");


                     if (Chartofacc != "" && ChartofaccTo != "")
                     {
                         Query = Query.Replace("@AccountCodeTo", "'" + ChartofaccTo + "'");
                         Query = Query.Replace("@AccountCode", "'" + Chartofacc + "'");

                     }
                     else
                     {
                         Query = Query.Replace("@AccountCodeTo", "null");
                         Query = Query.Replace("@AccountCode", "null");

                     }


                     DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlParams.ToArray());


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
