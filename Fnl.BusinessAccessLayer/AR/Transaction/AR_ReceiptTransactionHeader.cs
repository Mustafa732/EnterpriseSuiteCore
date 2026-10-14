using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Common.Utils;
using System.Text;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.AR.Transaction
{
    public partial class AR_ReceiptTransactionHeader : BaseClass
    {
        const string Query_Delete_ByHeaderID = @" Delete from AR_ReceiptTransactionHeader where CompanyID = @CompanyID and PeriodID = @PeriodID and  ReceiptTransactionID = @ReceiptTransactionID ";

        #region Class Methods

        public DataRow GetDataBySourceHeaderID(int companyID, int periodID, int sourceHdrId, int docTypeId)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@SourceHeaderId", sourceHdrId);
                sqlparams[2] = new SqlParameter("@DocumentTypeId", docTypeId);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);

                string Query_GetData_by_SourceHeaderId = @" 
SELECT * FROM AR_ReceiptTransactionHeader 
WHERE [CompanyID] = @CompanyID 
and [PeriodID] = @PeriodID
and [SourceHeaderId] = @SourceHeaderId
and DocumentTypeId = @DocumentTypeId
";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_by_SourceHeaderId, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dr = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }

        public DataTable GetAdvanceAgainstSOData(int companyID, int periodID, int? customerID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                
                if (customerID.HasValue)
                    sqlparams[2] = new SqlParameter("@CustomerID", customerID);
                else
                    sqlparams[2] = new SqlParameter("@CustomerID", DBNull.Value);

                string Query_GetAdvanceAgainstSOData = @" 
Select SourceHeaderID as OrderID, SAL_OrderHeader.ContractNo, AR_ReceiptTransactionHeader.DocumentNo, ContractDate, DocumentType from AR_ReceiptTransactionHeader 
    Inner Join SAL_OrderHeader on SAL_OrderHeader.OrderID = AR_ReceiptTransactionHeader.SourceHeaderID 
        and SAL_OrderHeader.CompanyID = AR_ReceiptTransactionHeader.CompanyID and SAL_OrderHeader.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
    Inner Join SET_DocumentType On SET_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
        and SET_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID and SET_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
where AR_ReceiptTransactionHeader.DocumentTypeID = 
        (Select DocumentTypeID from SET_DocumentType where DocumentType Like '%Advance Against SO%' and CompanyID = @CompanyID and PeriodID = @PeriodID) 
    and AR_ReceiptTransactionHeader.CompanyID = @CompanyID and AR_ReceiptTransactionHeader.PeriodID = @PeriodID 
    and AR_ReceiptTransactionHeader.CustomerID = @CustomerID or @CustomerID is null and AR_ReceiptTransactionHeader.SourceHeaderID not in 
	    (
            select SourceHeaderID from AR_ReceiptTransactionHeader where DocumentTypeID = 
                (Select DocumentTypeID from SET_DocumentType where DocumentType Like '%Refund Against SO%' and CompanyID = @CompanyID and PeriodID = @PeriodID)
        ) ";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAdvanceAgainstSOData, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable Get_AllDocumentNos(int companyID, int periodID, int documentTypeID, int CurrentFiscalID, bool IsReversalForm = false)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);


                string Query_Get_AllDocumentNos = @" SELECT AR_ReceiptTransactionHeader.* FROM AR_ReceiptTransactionHeader 
inner join Set_PeriodCustom on AR_ReceiptTransactionHeader.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID
WHERE AR_ReceiptTransactionHeader.CompanyID = @CompanyID and AR_ReceiptTransactionHeader.PeriodID = @PeriodID and AR_ReceiptTransactionHeader.DocumentTypeID = @DocumentTypeID And AR_ReceiptTransactionHeader.StatusCode = 1 And AR_ReceiptTransactionHeader.Status = 1 AND AR_ReceiptTransactionHeader.ReversalDate IS NULL AND AR_ReceiptTransactionHeader.Amount >= 0 
@IsReversalForm
ORDER BY AR_ReceiptTransactionHeader.DocumentNo ";

                if (IsReversalForm)
                    Query_Get_AllDocumentNos = Query_Get_AllDocumentNos.Replace("@IsReversalForm", "AND AR_ReceiptTransactionHeader.ActivityDate IS NULL");
                else
                    Query_Get_AllDocumentNos = Query_Get_AllDocumentNos.Replace("@IsReversalForm", "");

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

        public DataTable Get_DataForBankReconciliation(int companyID, int periodID, string fromDate, int bankID, int branchID, string BankAccountID, int CredirDebitID, string InstrumentNo)
        {
            DataTable dtToReturn = null;
            string additionalparameter = string.Empty;
            StringBuilder CreditDebitFilter = new StringBuilder();
             StringBuilder Orderby = new StringBuilder();
           
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@BankID", bankID);
                sqlparams[4] = new SqlParameter("@BranchID", branchID);
                sqlparams[5] = new SqlParameter("@BankAccountID", BankAccountID);
                sqlparams[6] = new SqlParameter("@InstrumentNo", InstrumentNo);
                //sqlparams[6] = new SqlParameter("@ReceiptAmount", RptAmount);

                #region Query

                string Query_Get_DataForBankReconciliation = @" 
Select * from (

Select '' as Action, tab2.ActivityDate, tab2.ReceiptID, tab2.PaymentID, tab2.BankAccountID, tab2.DocumentNo, tab2.DocumentDate, SUM(tab2.ReceiptAmount) ReceiptAmount, 
    SUM(tab2.PaymentAmount) PaymentAmount, tab2.DocumentType, tab2.InstrumentNo, tab2.Comments, tab2.BankAccountNumber 
From 
(
	Select AR_ReceiptTransactionHeader.ActivityDate, AR_ReceiptTransactionHeader.ReceiptTransactionID as ReceiptID, 0 as PaymentID, AR_Set_bankAccount.BankAccountID, 
        AR_ReceiptTransactionHeader.DocumentDate, AR_ReceiptTransactionHeader.DocumentNo, AR_ReceiptTransactionHeader.Amount ReceiptAmount, 0 as PaymentAmount, 
        Set_DocumentType.DocumentType, AR_ReceiptTransactionHeader.PaymentServerOrderNo As InstrumentNo, 
        IsNull(AR_ReceiptTransactionHeader.Comments, '') + IsNull(AR_ReceiptTransactionHeader.ReversalComments, '') As Comments, AR_Set_bankAccount.BankAccountNumber 
        From AR_ReceiptTransactionHeader 
		INNER JOIN AR_Set_bankAccount On AR_Set_bankAccount.PeriodID = AR_ReceiptTransactionHeader.PeriodID And AR_Set_bankAccount.BankAccountID = AR_ReceiptTransactionHeader.BankAccountID 
        INNER JOIN AR_Set_BankBranch On AR_Set_BankBranch.BankBranchID = AR_Set_BankAccount.BankBranchID 
		Inner Join Set_DocumentType On Set_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
            And Set_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID And Set_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
	WHERE AR_ReceiptTransactionHeader.CompanyID = @CompanyID AND AR_ReceiptTransactionHeader.PeriodID = @PeriodID And AR_ReceiptTransactionHeader.Status = 1 
        AND AR_ReceiptTransactionHeader.StatusCode = 1 AND AR_ReceiptTransactionHeader.BankAccountID is not null AND AR_Set_BankBranch.BankID = @BankID 
		
        AND AR_Set_BankBranch.BankBranchID = @BranchID AND AR_ReceiptTransactionHeader.DocumentDate <= @FromDate 
	    AND AR_SET_BankAccount.BankAccountID = @BankAccountID 
        AND (REPLACE(REPLACE(AR_ReceiptTransactionHeader.PaymentServerOrderNo, CHAR(160), ''), CHAR(9), '') = @InstrumentNo OR @InstrumentNo IS NULL)
	UNION ALL 
	
	Select AP_PaymentHeader.ActivityDate, 0 as ReceiptID, AP_PaymentHeader.PaymentID, AR_Set_bankAccount.BankAccountID, AP_PaymentHeader.DocumentDate, 
        AP_PaymentHeader.DocumentNo, 0 as ReceiptAmount, AP_PaymentHeader.Amount as PaymentAmount, Set_DocumentType.DocumentType, 
        AP_PaymentHeader.PaymentServerOrderNo As InstrumentNo, IsNull(AP_PaymentHeader.Comments, '') + IsNull(AP_PaymentHeader.ReversalComments, '') As Comments, 
        AR_Set_bankAccount.BankAccountNumber 
	From AP_PaymentHeader 
		INNER JOIN AR_Set_bankAccount On AR_Set_bankAccount.PeriodID = AP_PaymentHeader.PeriodID 
            And AR_Set_bankAccount.BankAccountID = AP_PaymentHeader.BankAccountID 
        INNER JOIN AR_Set_BankBranch On AR_Set_BankBranch.BankBranchID = AR_Set_BankAccount.BankBranchID 
		Inner Join Set_DocumentType On Set_DocumentType.CompanyID = AP_PaymentHeader.CompanyID And Set_DocumentType.PeriodID = AP_PaymentHeader.PeriodID 
            And Set_DocumentType.DocumentTypeID = AP_PaymentHeader.DocumentTypeID 
	WHERE AP_PaymentHeader.CompanyID = @CompanyID AND AP_PaymentHeader.PeriodID = @PeriodID And AP_PaymentHeader.Status = 1 AND AP_PaymentHeader.StatusCode = 1 
        AND AP_PaymentHeader.BankAccountID is not null 
        AND AR_Set_BankBranch.BankID = @BankID  AND AR_Set_BankBranch.BankBranchID = @BranchID  AND AP_PaymentHeader.DocumentDate <= @FromDate  
        AND AR_SET_BankAccount.BankAccountID = @BankAccountID  
        AND (REPLACE(REPLACE(AP_PaymentHeader.PaymentServerOrderNo, CHAR(160), ''), CHAR(9), '') = @InstrumentNo OR @InstrumentNo IS NULL)
    UNION ALL 
    
    Select AR_ReceiptTransactionHeader.ActivityDate,AR_ReceiptTransactionHeader.ReceiptTransactionID as ReceiptID,0 as PaymentID,AR_Set_bankAccount.BankAccountID, AR_ReceiptTransactionHeader.DocumentDate, AR_ReceiptTransactionHeader.DocumentNo, 
        (Case when AR_ReceiptBanksDetail.TransactionType = 'Debit' then AR_ReceiptBanksDetail.Amount else 0 end) as ReceiptAmount, 
        (Case when AR_ReceiptBanksDetail.TransactionType = 'Credit' then AR_ReceiptBanksDetail.Amount else 0 end) as PaymentAmount, Set_DocumentType.DocumentType, 
        AR_ReceiptBanksDetail.InstrumentNo, AR_ReceiptBanksDetail.Comments, AR_Set_bankAccount.BankAccountNumber 
	From AR_ReceiptTransactionHeader 
		INNER JOIN AR_ReceiptBanksDetail ON AR_ReceiptBanksDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
			AND AR_ReceiptBanksDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
			AND AR_ReceiptBanksDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID 
		INNER JOIN AR_Set_bankAccount On AR_Set_bankAccount.PeriodID = AR_ReceiptBanksDetail.PeriodID And AR_Set_bankAccount.BankAccountID = AR_ReceiptBanksDetail.BankAccountID 
        INNER JOIN AR_Set_BankBranch On AR_Set_BankBranch.BankBranchID = AR_Set_BankAccount.BankBranchID 
		Inner Join Set_DocumentType On Set_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
            And Set_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID And Set_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
	WHERE AR_ReceiptTransactionHeader.CompanyID = @CompanyID AND AR_ReceiptTransactionHeader.PeriodID = @PeriodID And AR_ReceiptTransactionHeader.Status = 1 
        AND AR_ReceiptTransactionHeader.StatusCode = 1 
        AND AR_Set_BankBranch.BankID = @BankID  AND AR_Set_BankBranch.BankBranchID = @BranchID  AND AR_ReceiptTransactionHeader.DocumentDate <= @FromDate  
	    AND AR_SET_BankAccount.BankAccountID = @BankAccountID and AR_ReceiptBanksDetail.status = 1
        AND (REPLACE(REPLACE(AR_ReceiptBanksDetail.InstrumentNo, CHAR(160), ''), CHAR(9), '') = @InstrumentNo OR @InstrumentNo IS NULL)
	UNION ALL 
	
	Select AP_PaymentHeader.ActivityDate,0 as ReceiptID,AP_PaymentHeader.PaymentID as PaymentID,AR_Set_bankAccount.BankAccountID, AP_PaymentHeader.DocumentDate, AP_PaymentHeader.DocumentNo, 
        (Case when AP_PaymentBanksDetail.TransactionType = 'Debit' then AP_PaymentBanksDetail.Amount else 0 end) as ReceiptAmount, 
        (Case when AP_PaymentBanksDetail.TransactionType = 'Credit' then AP_PaymentBanksDetail.Amount else 0 end) as PaymentAmount, Set_DocumentType.DocumentType, 
        AP_PaymentBanksDetail.InstrumentNo, AP_PaymentBanksDetail.Comments, AR_Set_bankAccount.BankAccountNumber 
    From AP_PaymentHeader 
	    INNER JOIN AP_PaymentBanksDetail ON AP_PaymentBanksDetail.CompanyID = AP_PaymentHeader.CompanyID 
            AND AP_PaymentBanksDetail.PeriodID = AP_PaymentHeader.PeriodID AND AP_PaymentBanksDetail.PaymentHeaderID = AP_PaymentHeader.PaymentID 
	    INNER JOIN AR_Set_bankAccount On AR_Set_bankAccount.PeriodID = AP_PaymentBanksDetail.PeriodID And AR_Set_bankAccount.BankAccountID = AP_PaymentBanksDetail.BankAccountID 
        INNER JOIN AR_Set_BankBranch On AR_Set_BankBranch.BankBranchID = AR_Set_BankAccount.BankBranchID 
	    Inner Join Set_DocumentType On Set_DocumentType.CompanyID = AP_PaymentHeader.CompanyID And Set_DocumentType.PeriodID = AP_PaymentHeader.PeriodID 
            And Set_DocumentType.DocumentTypeID = AP_PaymentHeader.DocumentTypeID 
    WHERE AP_PaymentHeader.CompanyID = @CompanyID AND AP_PaymentHeader.PeriodID = @PeriodID And AP_PaymentHeader.Status = 1 AND AP_PaymentHeader.StatusCode = 1 
        AND AR_Set_BankBranch.BankID = @BankID  AND AR_Set_BankBranch.BankBranchID = @BranchID  AND AP_PaymentHeader.DocumentDate <= @FromDate  
        AND AR_SET_BankAccount.BankAccountID = @BankAccountID  and AP_PaymentBanksDetail.status = 1
        AND (REPLACE(REPLACE(AP_PaymentBanksDetail.InstrumentNo, CHAR(160), ''), CHAR(9), '') = @InstrumentNo OR @InstrumentNo IS NULL)
)tab2 
where Tab2.ActivityDate is null
Group By tab2.DocumentType, tab2.BankAccountID, tab2.DocumentNo, tab2.DocumentDate, tab2.InstrumentNo, tab2.Comments, tab2.BankAccountNumber
,tab2.ReceiptID,tab2.PaymentID,tab2.ActivityDate

) FinalTab



";

                #endregion


                if (CredirDebitID > 0 && CredirDebitID != null && CredirDebitID != -1)
                {
                    if (CredirDebitID == 1)
                    {
                        CreditDebitFilter.Append(" where FinalTab.PaymentAmount > 0");
                    }
                    else if (CredirDebitID == 2)
                    {
                        CreditDebitFilter.Append(" where FinalTab.ReceiptAmount > 0");
                    }
                }

                Orderby.Append("  order by FinalTab.DocumentDate ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_DataForBankReconciliation + CreditDebitFilter.ToString() + Orderby.ToString(), sqlparams);

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

        public DataTable Get_DataForBankReconciliationTSML(int companyID, int periodID, string fromDate, int bankID, int branchID, string BankAccountID, int CredirDebitID, string InstrumentNo)
        {
            DataTable dtToReturn = null;
            string additionalparameter = string.Empty;
            StringBuilder CreditDebitFilter = new StringBuilder();
            StringBuilder Orderby = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@BankID", bankID);
                sqlparams[4] = new SqlParameter("@BranchID", branchID);
                sqlparams[5] = new SqlParameter("@BankAccountID", BankAccountID);
                sqlparams[6] = new SqlParameter("@InstrumentNo", InstrumentNo);

                if (CredirDebitID > 0 && CredirDebitID != null && CredirDebitID != -1)
                {
                    if (CredirDebitID == 1)
                    {
                        CreditDebitFilter.Append(" where FinalTab.PaymentAmount > 0");
                    }
                    else if (CredirDebitID == 2)
                    {
                        CreditDebitFilter.Append(" where FinalTab.ReceiptAmount > 0");
                    }
                }

                Orderby.Append("  order by FinalTab.DocumentDate ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_Get_DataForBankReconciliationTSML) + CreditDebitFilter.ToString() + Orderby.ToString(), sqlparams);

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

        public DataTable Get_DataForBankReconciliation_IsActiveDate(int companyID, int periodID, string fromDate, int bankID, int branchID, string BankAccountID, int CredirDebitID, string InstrumentNo, bool isActivityDate = false)
        {
            DataTable dtToReturn = null;
            string additionalparameter = string.Empty;
            StringBuilder CreditDebitFilter = new StringBuilder();
            StringBuilder Orderby = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@BankID", bankID);
                sqlparams[4] = new SqlParameter("@BranchID", branchID);
                sqlparams[5] = new SqlParameter("@BankAccountID", BankAccountID);
                sqlparams[6] = new SqlParameter("@InstrumentNo", InstrumentNo);                
                //sqlparams[6] = new SqlParameter("@ReceiptAmount", RptAmount);

                #region Query

                string Query_Get_DataForBankReconciliation = @" 
Select * from (

Select '' as Action, tab2.ActivityDate, tab2.ReceiptID, tab2.PaymentID, tab2.BankAccountID, tab2.DocumentNo, tab2.DocumentDate, SUM(tab2.ReceiptAmount) ReceiptAmount, 
    SUM(tab2.PaymentAmount) PaymentAmount, tab2.DocumentType, tab2.InstrumentNo, tab2.Comments, tab2.BankAccountNumber 
From 
(
	Select AR_ReceiptTransactionHeader.ActivityDate, AR_ReceiptTransactionHeader.ReceiptTransactionID as ReceiptID, 0 as PaymentID, AR_Set_bankAccount.BankAccountID, 
        AR_ReceiptTransactionHeader.DocumentDate, AR_ReceiptTransactionHeader.DocumentNo, AR_ReceiptTransactionHeader.Amount ReceiptAmount, 0 as PaymentAmount, 
        Set_DocumentType.DocumentType, AR_ReceiptTransactionHeader.PaymentServerOrderNo As InstrumentNo, 
        IsNull(AR_ReceiptTransactionHeader.Comments, '') + IsNull(AR_ReceiptTransactionHeader.ReversalComments, '') As Comments, AR_Set_bankAccount.BankAccountNumber 
        From AR_ReceiptTransactionHeader 
		INNER JOIN AR_Set_bankAccount On AR_Set_bankAccount.PeriodID = AR_ReceiptTransactionHeader.PeriodID And AR_Set_bankAccount.BankAccountID = AR_ReceiptTransactionHeader.BankAccountID 
        INNER JOIN AR_Set_BankBranch On AR_Set_BankBranch.BankBranchID = AR_Set_BankAccount.BankBranchID 
		Inner Join Set_DocumentType On Set_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
            And Set_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID And Set_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
	WHERE AR_ReceiptTransactionHeader.CompanyID = @CompanyID AND AR_ReceiptTransactionHeader.PeriodID = @PeriodID And AR_ReceiptTransactionHeader.Status = 1 
        AND AR_ReceiptTransactionHeader.StatusCode = 1 AND AR_ReceiptTransactionHeader.BankAccountID is not null AND AR_Set_BankBranch.BankID = @BankID 
		
        AND AR_Set_BankBranch.BankBranchID = @BranchID AND AR_ReceiptTransactionHeader.DocumentDate <= @FromDate 
	    AND AR_SET_BankAccount.BankAccountID = @BankAccountID 
        AND (REPLACE(REPLACE(AR_ReceiptTransactionHeader.PaymentServerOrderNo, CHAR(160), ''), CHAR(9), '') = @InstrumentNo OR @InstrumentNo IS NULL)
	UNION ALL 
	
	Select AP_PaymentHeader.ActivityDate, 0 as ReceiptID, AP_PaymentHeader.PaymentID, AR_Set_bankAccount.BankAccountID, AP_PaymentHeader.DocumentDate, 
        AP_PaymentHeader.DocumentNo, 0 as ReceiptAmount, AP_PaymentHeader.Amount as PaymentAmount, Set_DocumentType.DocumentType, 
        AP_PaymentHeader.PaymentServerOrderNo As InstrumentNo, IsNull(AP_PaymentHeader.Comments, '') + IsNull(AP_PaymentHeader.ReversalComments, '') As Comments, 
        AR_Set_bankAccount.BankAccountNumber 
	From AP_PaymentHeader 
		INNER JOIN AR_Set_bankAccount On AR_Set_bankAccount.PeriodID = AP_PaymentHeader.PeriodID 
            And AR_Set_bankAccount.BankAccountID = AP_PaymentHeader.BankAccountID 
        INNER JOIN AR_Set_BankBranch On AR_Set_BankBranch.BankBranchID = AR_Set_BankAccount.BankBranchID 
		Inner Join Set_DocumentType On Set_DocumentType.CompanyID = AP_PaymentHeader.CompanyID And Set_DocumentType.PeriodID = AP_PaymentHeader.PeriodID 
            And Set_DocumentType.DocumentTypeID = AP_PaymentHeader.DocumentTypeID 
	WHERE AP_PaymentHeader.CompanyID = @CompanyID AND AP_PaymentHeader.PeriodID = @PeriodID And AP_PaymentHeader.Status = 1 AND AP_PaymentHeader.StatusCode = 1 
        AND AP_PaymentHeader.BankAccountID is not null 
        AND AR_Set_BankBranch.BankID = @BankID  AND AR_Set_BankBranch.BankBranchID = @BranchID  AND AP_PaymentHeader.DocumentDate <= @FromDate  
        AND AR_SET_BankAccount.BankAccountID = @BankAccountID  
        AND (REPLACE(REPLACE(AP_PaymentHeader.PaymentServerOrderNo, CHAR(160), ''), CHAR(9), '') = @InstrumentNo OR @InstrumentNo IS NULL)
    UNION ALL 
    
    Select AR_ReceiptTransactionHeader.ActivityDate,AR_ReceiptTransactionHeader.ReceiptTransactionID as ReceiptID,0 as PaymentID,AR_Set_bankAccount.BankAccountID, AR_ReceiptTransactionHeader.DocumentDate, AR_ReceiptTransactionHeader.DocumentNo, 
        (Case when AR_ReceiptBanksDetail.TransactionType = 'Debit' then AR_ReceiptBanksDetail.Amount else 0 end) as ReceiptAmount, 
        (Case when AR_ReceiptBanksDetail.TransactionType = 'Credit' then AR_ReceiptBanksDetail.Amount else 0 end) as PaymentAmount, Set_DocumentType.DocumentType, 
        AR_ReceiptBanksDetail.InstrumentNo, AR_ReceiptBanksDetail.Comments, AR_Set_bankAccount.BankAccountNumber 
	From AR_ReceiptTransactionHeader 
		INNER JOIN AR_ReceiptBanksDetail ON AR_ReceiptBanksDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
			AND AR_ReceiptBanksDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
			AND AR_ReceiptBanksDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID 
            AND AR_ReceiptBanksDetail.status = 1
		INNER JOIN AR_Set_bankAccount On AR_Set_bankAccount.PeriodID = AR_ReceiptBanksDetail.PeriodID And AR_Set_bankAccount.BankAccountID = AR_ReceiptBanksDetail.BankAccountID 
        INNER JOIN AR_Set_BankBranch On AR_Set_BankBranch.BankBranchID = AR_Set_BankAccount.BankBranchID 
		Inner Join Set_DocumentType On Set_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
            And Set_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID And Set_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
	WHERE AR_ReceiptTransactionHeader.CompanyID = @CompanyID AND AR_ReceiptTransactionHeader.PeriodID = @PeriodID And AR_ReceiptTransactionHeader.Status = 1 
        AND AR_ReceiptTransactionHeader.StatusCode = 1 
        AND AR_Set_BankBranch.BankID = @BankID  AND AR_Set_BankBranch.BankBranchID = @BranchID  AND AR_ReceiptTransactionHeader.DocumentDate <= @FromDate  
	    AND AR_SET_BankAccount.BankAccountID = @BankAccountID 
        AND (REPLACE(REPLACE(AR_ReceiptBanksDetail.InstrumentNo, CHAR(160), ''), CHAR(9), '') = @InstrumentNo OR @InstrumentNo IS NULL)
	UNION ALL 
	
	Select AP_PaymentHeader.ActivityDate,0 as ReceiptID,AP_PaymentHeader.PaymentID as PaymentID,AR_Set_bankAccount.BankAccountID, AP_PaymentHeader.DocumentDate, AP_PaymentHeader.DocumentNo, 
        (Case when AP_PaymentBanksDetail.TransactionType = 'Debit' then AP_PaymentBanksDetail.Amount else 0 end) as ReceiptAmount, 
        (Case when AP_PaymentBanksDetail.TransactionType = 'Credit' then AP_PaymentBanksDetail.Amount else 0 end) as PaymentAmount, Set_DocumentType.DocumentType, 
        AP_PaymentBanksDetail.InstrumentNo, AP_PaymentBanksDetail.Comments, AR_Set_bankAccount.BankAccountNumber 
    From AP_PaymentHeader 
	    INNER JOIN AP_PaymentBanksDetail ON AP_PaymentBanksDetail.CompanyID = AP_PaymentHeader.CompanyID 
            AND AP_PaymentBanksDetail.PeriodID = AP_PaymentHeader.PeriodID AND AP_PaymentBanksDetail.PaymentHeaderID = AP_PaymentHeader.PaymentID 
	    INNER JOIN AR_Set_bankAccount On AR_Set_bankAccount.PeriodID = AP_PaymentBanksDetail.PeriodID And AR_Set_bankAccount.BankAccountID = AP_PaymentBanksDetail.BankAccountID 
        INNER JOIN AR_Set_BankBranch On AR_Set_BankBranch.BankBranchID = AR_Set_BankAccount.BankBranchID 
	    Inner Join Set_DocumentType On Set_DocumentType.CompanyID = AP_PaymentHeader.CompanyID And Set_DocumentType.PeriodID = AP_PaymentHeader.PeriodID 
            And Set_DocumentType.DocumentTypeID = AP_PaymentHeader.DocumentTypeID 
    WHERE AP_PaymentHeader.CompanyID = @CompanyID AND AP_PaymentHeader.PeriodID = @PeriodID And AP_PaymentHeader.Status = 1 AND AP_PaymentHeader.StatusCode = 1 
        AND AR_Set_BankBranch.BankID = @BankID  AND AR_Set_BankBranch.BankBranchID = @BranchID  AND AP_PaymentHeader.DocumentDate <= @FromDate  
        AND AR_SET_BankAccount.BankAccountID = @BankAccountID 
        AND (REPLACE(REPLACE(AP_PaymentBanksDetail.InstrumentNo, CHAR(160), ''), CHAR(9), '') = @InstrumentNo OR @InstrumentNo IS NULL)
)tab2 
{0}
Group By tab2.DocumentType, tab2.BankAccountID, tab2.DocumentNo, tab2.DocumentDate, tab2.InstrumentNo, tab2.Comments, tab2.BankAccountNumber
,tab2.ReceiptID,tab2.PaymentID,tab2.ActivityDate

) FinalTab



";

                #endregion

                if (CredirDebitID > 0 && CredirDebitID != null && CredirDebitID != -1)
                {
                    if (CredirDebitID == 1)
                    {
                        CreditDebitFilter.Append(" where FinalTab.PaymentAmount > 0");
                    }
                    else if (CredirDebitID == 2)
                    {
                        CreditDebitFilter.Append(" where FinalTab.ReceiptAmount > 0");
                    }
                }

                Orderby.Append("  order by FinalTab.DocumentDate ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_DataForBankReconciliation + CreditDebitFilter.ToString() + Orderby.ToString(), sqlparams);

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

        public DataTable Get_DataForBankReconciliation_IsActiveDateTSML(int companyID, int periodID, string fromDate, int bankID, int branchID, string BankAccountID, int CredirDebitID, string InstrumentNo, bool isActivityDate = false)
        {
            DataTable dtToReturn = null;
            string additionalparameter = string.Empty;
            StringBuilder sb = new StringBuilder();
            StringBuilder CreditDebitFilter = new StringBuilder();
            StringBuilder Orderby = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@BankID", bankID);
                sqlparams[4] = new SqlParameter("@BranchID", branchID);
                sqlparams[5] = new SqlParameter("@BankAccountID", BankAccountID);
                sqlparams[6] = new SqlParameter("@InstrumentNo", InstrumentNo);

                if (isActivityDate)
                    sb.Append("where Tab2.ActivityDate is not null");
                else
                    sb.Append("where Tab2.ActivityDate is null");

                if (CredirDebitID > 0 && CredirDebitID != null && CredirDebitID != -1)
                {
                    if (CredirDebitID == 1)
                    {
                        CreditDebitFilter.Append(" where FinalTab.PaymentAmount > 0");
                    }
                    else if (CredirDebitID == 2)
                    {
                        CreditDebitFilter.Append(" where FinalTab.ReceiptAmount > 0");
                    }
                }

                Orderby.Append("  order by FinalTab.DocumentDate ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_Get_DataForBankReconciliation_IsActiveDataTSML, sb.ToString()) + CreditDebitFilter.ToString() + Orderby.ToString(), sqlparams);                

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

        public DataTable Get_DataForBankReconciliationNew(int companyID, int periodID, string fromDate, string UptoDate, int bankID, int branchID, string BankAccountID, int CredirDebitID, string InstrumentNo, bool isActivityDate = false)
        {
            DataTable dtToReturn = null;
            string additionalparameter = string.Empty;
            StringBuilder sb = new StringBuilder();
            StringBuilder CreditDebitFilter = new StringBuilder();
            StringBuilder Orderby = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@UptoDate", UptoDate);
                sqlparams[4] = new SqlParameter("@BankID", bankID);
                sqlparams[5] = new SqlParameter("@BranchID", branchID);
                sqlparams[6] = new SqlParameter("@BankAccountID", BankAccountID);
                sqlparams[7] = new SqlParameter("@InstrumentNo", InstrumentNo);

                if (isActivityDate)
                    sb.Append("where Tab2.ActivityDate is not null");
                else
                    sb.Append("where Tab2.ActivityDate is null");

                if (CredirDebitID > 0 && CredirDebitID != null && CredirDebitID != -1)
                {
                    if (CredirDebitID == 1)
                    {
                        CreditDebitFilter.Append(" where FinalTab.PaymentAmount > 0");
                    }
                    else if (CredirDebitID == 2)
                    {
                        CreditDebitFilter.Append(" where FinalTab.ReceiptAmount > 0");
                    }
                }

                Orderby.Append("  order by FinalTab.DocumentDate ");
                
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_Get_DataForBankReconciliationNew,sb.ToString()) + CreditDebitFilter.ToString() + Orderby.ToString(), sqlparams);

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

        public DataTable Get_DataForBankReconciliationNew_IsActiveDate(int companyID, int periodID, string fromDate, string UptoDate, int bankID, int branchID, string BankAccountID, int CredirDebitID, string InstrumentNo, bool isActivityDate = false)
        {
            DataTable dtToReturn = null;
            string additionalparameter = string.Empty;
            StringBuilder sb = new StringBuilder();
            StringBuilder CreditDebitFilter = new StringBuilder();
            StringBuilder Orderby = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@UptoDate", UptoDate);
                sqlparams[4] = new SqlParameter("@BankID", bankID);
                sqlparams[5] = new SqlParameter("@BranchID", branchID);
                sqlparams[6] = new SqlParameter("@BankAccountID", BankAccountID);
                sqlparams[7] = new SqlParameter("@InstrumentNo", InstrumentNo);

                if (isActivityDate)
                    sb.Append("where Tab2.ActivityDate is not null");
                else
                    sb.Append("where Tab2.ActivityDate is null");

                if (CredirDebitID > 0 && CredirDebitID != null && CredirDebitID != -1)
                {
                    if (CredirDebitID == 1)
                    {
                        CreditDebitFilter.Append(" where FinalTab.PaymentAmount > 0");
                    }
                    else if (CredirDebitID == 2)
                    {
                        CreditDebitFilter.Append(" where FinalTab.ReceiptAmount > 0");
                    }
                }

                Orderby.Append("  order by FinalTab.DocumentDate ");
                
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_Get_DataForBankReconciliationNew,sb.ToString()) + CreditDebitFilter.ToString() + Orderby.ToString(), sqlparams);

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

        public bool InsertPF(string DatabaseName, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                StringBuilder sb = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[72];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[2] = new SqlParameter("@ReceiptTransactionID", f_ReceiptTransactionID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", f_DocumentTypeID);
                sqlparams[4] = new SqlParameter("@DocumentNo", f_DocumentNo);
                sqlparams[5] = new SqlParameter("@DocumentDate", f_DocumentDate);

                if (!string.IsNullOrEmpty(f_AddressVerificationCode))
                    sqlparams[6] = new SqlParameter("@AddressVerificationCode", f_AddressVerificationCode);
                else
                    sqlparams[6] = new SqlParameter("@AddressVerificationCode", DBNull.Value);

                sqlparams[7] = new SqlParameter("@Amount", f_Amount);

                if (f_AntiCipatedClearingDate.HasValue)
                    sqlparams[8] = new SqlParameter("@AntiCipatedClearingDate", f_AntiCipatedClearingDate.Value);
                else
                    sqlparams[8] = new SqlParameter("@AntiCipatedClearingDate", DBNull.Value);

                if (f_PayFromCustomerID.HasValue)
                    sqlparams[9] = new SqlParameter("@PayFromCustomerID", f_PayFromCustomerID.Value);
                else
                    sqlparams[9] = new SqlParameter("@PayFromCustomerID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_PaymentServerOrderNo))
                    sqlparams[10] = new SqlParameter("@PaymentServerOrderNo", f_PaymentServerOrderNo);
                else
                    sqlparams[10] = new SqlParameter("@PaymentServerOrderNo", DBNull.Value);

                if (f_PostMarkDate.HasValue)
                    sqlparams[11] = new SqlParameter("@PostMarkDate", f_PostMarkDate.Value);
                else
                    sqlparams[11] = new SqlParameter("@PostMarkDate", DBNull.Value);

                if (f_ProgramID.HasValue)
                    sqlparams[12] = new SqlParameter("@ProgramID", f_ProgramID.Value);
                else
                    sqlparams[12] = new SqlParameter("@ProgramID", DBNull.Value);

                if (f_ReceiptDate.HasValue)
                    sqlparams[13] = new SqlParameter("@ReceiptDate", f_ReceiptDate.Value);
                else
                    sqlparams[13] = new SqlParameter("@ReceiptDate", DBNull.Value);

                if (f_ReceiptMethodID.HasValue)
                    sqlparams[14] = new SqlParameter("@ReceiptMethodID", f_ReceiptMethodID.Value);
                else
                    sqlparams[14] = new SqlParameter("@ReceiptMethodID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ReceiptNo))
                    sqlparams[15] = new SqlParameter("@ReceiptNo", f_ReceiptNo);
                else
                    sqlparams[15] = new SqlParameter("@ReceiptNo", DBNull.Value);

                if (!string.IsNullOrEmpty(f_Comments))
                    sqlparams[16] = new SqlParameter("@Comments", f_Comments);
                else
                    sqlparams[16] = new SqlParameter("@Comments", DBNull.Value);

                sqlparams[17] = new SqlParameter("@CurrencyId", f_CurrencyId);

                if (f_BankAccountID.HasValue)
                    sqlparams[18] = new SqlParameter("@BankAccountID", f_BankAccountID.Value);
                else
                    sqlparams[18] = new SqlParameter("@BankAccountID", DBNull.Value);

                if (f_CustomerBankBranchID.HasValue)
                    sqlparams[19] = new SqlParameter("@CustomerBankBranchID", f_CustomerBankBranchID.Value);
                else
                    sqlparams[19] = new SqlParameter("@CustomerBankBranchID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_CustomerReceiptReference))
                    sqlparams[20] = new SqlParameter("@CustomerReceiptReference", f_CustomerReceiptReference);
                else
                    sqlparams[20] = new SqlParameter("@CustomerReceiptReference", DBNull.Value);

                if (f_DepositDate.HasValue)
                    sqlparams[21] = new SqlParameter("@DepositDate", f_DepositDate.Value);
                else
                    sqlparams[21] = new SqlParameter("@DepositDate", DBNull.Value);

                if (f_ExchangeDate.HasValue)
                    sqlparams[22] = new SqlParameter("@ExchangeDate", f_ExchangeDate.Value);
                else
                    sqlparams[22] = new SqlParameter("@ExchangeDate", DBNull.Value);

                if (f_ExchangeRate.HasValue)
                    sqlparams[23] = new SqlParameter("@ExchangeRate", f_ExchangeRate.Value);
                else
                    sqlparams[23] = new SqlParameter("@ExchangeRate", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ExchangeRateType))
                    sqlparams[24] = new SqlParameter("@ExchangeRateType", f_ExchangeRateType);
                else
                    sqlparams[24] = new SqlParameter("@ExchangeRateType", DBNull.Value);

                if (f_FactorDiscountAmount.HasValue)
                    sqlparams[25] = new SqlParameter("@FactorDiscountAmount", f_FactorDiscountAmount.Value);
                else
                    sqlparams[25] = new SqlParameter("@FactorDiscountAmount", DBNull.Value);

                if (f_IssueDate.HasValue)
                    sqlparams[26] = new SqlParameter("@IssueDate", f_IssueDate.Value);
                else
                    sqlparams[26] = new SqlParameter("@IssueDate", DBNull.Value);

                if (f_IssuerBankBranchID.HasValue)
                    sqlparams[27] = new SqlParameter("@IssuerBankBranchID", f_IssuerBankBranchID.Value);
                else
                    sqlparams[27] = new SqlParameter("@IssuerBankBranchID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_IssuerName))
                    sqlparams[28] = new SqlParameter("@IssuerName", f_IssuerName);
                else
                    sqlparams[28] = new SqlParameter("@IssuerName", DBNull.Value);

                sqlparams[29] = new SqlParameter("@OverRemitAccountFlag", f_OverRemitAccountFlag);

                if (f_ReceivablesTrxID.HasValue)
                    sqlparams[30] = new SqlParameter("@ReceivablesTrxID", f_ReceivablesTrxID.Value);
                else
                    sqlparams[30] = new SqlParameter("@ReceivablesTrxID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_Reference))
                    sqlparams[31] = new SqlParameter("@Reference", f_Reference);
                else
                    sqlparams[31] = new SqlParameter("@Reference", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ReferenceType))
                    sqlparams[32] = new SqlParameter("@ReferenceType", f_ReferenceType);
                else
                    sqlparams[32] = new SqlParameter("@ReferenceType", DBNull.Value);

                if (!string.IsNullOrEmpty(f_RemittanceBankAccount))
                    sqlparams[33] = new SqlParameter("@RemittanceBankAccount", f_RemittanceBankAccount);
                else
                    sqlparams[33] = new SqlParameter("@RemittanceBankAccount", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ReversalCategory))
                    sqlparams[34] = new SqlParameter("@ReversalCategory", f_ReversalCategory);
                else
                    sqlparams[34] = new SqlParameter("@ReversalCategory", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ReversalComments))
                    sqlparams[35] = new SqlParameter("@ReversalComments", f_ReversalComments);
                else
                    sqlparams[35] = new SqlParameter("@ReversalComments", DBNull.Value);

                if (f_ReversalDate.HasValue)
                    sqlparams[36] = new SqlParameter("@ReversalDate", f_ReversalDate.Value);
                else
                    sqlparams[36] = new SqlParameter("@ReversalDate", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ReversalReasonCode))
                    sqlparams[37] = new SqlParameter("@ReversalReasonCode", f_ReversalReasonCode);
                else
                    sqlparams[37] = new SqlParameter("@ReversalReasonCode", DBNull.Value);

                sqlparams[38] = new SqlParameter("@SelectedForFactoringFlag", f_SelectedForFactoringFlag);

                if (f_SelectedRemittanceBatchID.HasValue)
                    sqlparams[39] = new SqlParameter("@SelectedRemittanceBatchID", f_SelectedRemittanceBatchID.Value);
                else
                    sqlparams[39] = new SqlParameter("@SelectedRemittanceBatchID", DBNull.Value);

                if (f_ApprovalCode.HasValue)
                    sqlparams[40] = new SqlParameter("@ApprovalCode", f_ApprovalCode.Value);
                else
                    sqlparams[40] = new SqlParameter("@ApprovalCode", DBNull.Value);

                sqlparams[41] = new SqlParameter("@OverrideRemitAccountFlag", f_OverrideRemitAccountFlag);
                sqlparams[42] = new SqlParameter("@ConfirmedFlag", f_ConfirmedFlag);

                if (f_LumSumPayment.HasValue)
                    sqlparams[43] = new SqlParameter("@LumSumPayment", f_LumSumPayment.Value);
                else
                    sqlparams[43] = new SqlParameter("@LumSumPayment", DBNull.Value);

                sqlparams[44] = new SqlParameter("@Status", f_Status);
                sqlparams[45] = new SqlParameter("@CreatedBy", f_CreatedBy);
                sqlparams[46] = new SqlParameter("@CreateDate", f_CreateDate);
                sqlparams[47] = new SqlParameter("@CreatedByValue", f_CreatedByValue);

                if (f_StatusCode.HasValue)
                    sqlparams[48] = new SqlParameter("@StatusCode", f_StatusCode.Value);
                else
                    sqlparams[48] = new SqlParameter("@StatusCode", DBNull.Value);

                if (f_SourceHeaderID.HasValue)
                    sqlparams[49] = new SqlParameter("@SourceHeaderID", f_SourceHeaderID.Value);
                else
                    sqlparams[49] = new SqlParameter("@SourceHeaderID", DBNull.Value);

                if (f_CustomerID.HasValue)
                    sqlparams[50] = new SqlParameter("@CustomerID", f_CustomerID.Value);
                else
                    sqlparams[50] = new SqlParameter("@CustomerID", DBNull.Value);

                sqlparams[51] = new SqlParameter("@IsInvoiceAdjusted", f_IsInvoiceAdjusted);

                if (f_AdditionalTaxAmount.HasValue)
                    sqlparams[52] = new SqlParameter("@AdditionalTaxAmount", f_AdditionalTaxAmount.Value);
                else
                    sqlparams[52] = new SqlParameter("@AdditionalTaxAmount", DBNull.Value);

                if (!string.IsNullOrEmpty(f_AdditionalTaxCOA))
                    sqlparams[53] = new SqlParameter("@AdditionalTaxCOA", f_AdditionalTaxCOA);
                else
                    sqlparams[53] = new SqlParameter("@AdditionalTaxCOA", DBNull.Value);

                if (f_BusinessPartnerTypeID.HasValue)
                    sqlparams[54] = new SqlParameter("@BusinessPartnerTypeID", f_BusinessPartnerTypeID.Value);
                else
                    sqlparams[54] = new SqlParameter("@BusinessPartnerTypeID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_TransactionType))
                    sqlparams[55] = new SqlParameter("@TransactionType", f_TransactionType);
                else
                    sqlparams[55] = new SqlParameter("@TransactionType", DBNull.Value);

                if (f_LocationID.HasValue)
                    sqlparams[56] = new SqlParameter("@LocationID", f_LocationID.Value);
                else
                    sqlparams[56] = new SqlParameter("@LocationID", DBNull.Value);

                if (f_EmployeeID.HasValue)
                    sqlparams[57] = new SqlParameter("@EmployeeID", f_EmployeeID.Value);
                else
                    sqlparams[57] = new SqlParameter("@EmployeeID", DBNull.Value);

                if (f_CommissionPercentage.HasValue)
                    sqlparams[58] = new SqlParameter("@CommissionPercentage", f_CommissionPercentage.Value);
                else
                    sqlparams[58] = new SqlParameter("@CommissionPercentage", DBNull.Value);

                if (!string.IsNullOrEmpty(f_CommissionCCID))
                    sqlparams[59] = new SqlParameter("@CommissionCCID", f_CommissionCCID);
                else
                    sqlparams[59] = new SqlParameter("@CommissionCCID", DBNull.Value);

                if (f_EmployeeTypeID.HasValue)
                    sqlparams[60] = new SqlParameter("@EmployeeTypeID", f_EmployeeTypeID.Value);
                else
                    sqlparams[60] = new SqlParameter("@EmployeeTypeID", DBNull.Value);

                if (f_ActivityDate.HasValue)
                    sqlparams[61] = new SqlParameter("@ActivityDate", f_ActivityDate.Value);
                else
                    sqlparams[61] = new SqlParameter("@ActivityDate", DBNull.Value);

                if (f_ActivityEntryDate.HasValue)
                    sqlparams[62] = new SqlParameter("@ActivityEntryDate", f_ActivityEntryDate.Value);
                else
                    sqlparams[62] = new SqlParameter("@ActivityEntryDate", DBNull.Value);

                if (f_BankBalance.HasValue)
                    sqlparams[63] = new SqlParameter("@BankBalance", f_BankBalance.Value);
                else
                    sqlparams[63] = new SqlParameter("@BankBalance", DBNull.Value);

                if (f_ControlCredit.HasValue)
                    sqlparams[64] = new SqlParameter("@ControlCredit", f_ControlCredit.Value);
                else
                    sqlparams[64] = new SqlParameter("@ControlCredit", DBNull.Value);

                if (f_ControlDebit.HasValue)
                    sqlparams[65] = new SqlParameter("@ControlDebit", f_ControlDebit.Value);
                else
                    sqlparams[65] = new SqlParameter("@ControlDebit", DBNull.Value);

                if (f_CostCentreID.HasValue)
                    sqlparams[66] = new SqlParameter("@CostCentreID", f_CostCentreID.Value);
                else
                    sqlparams[66] = new SqlParameter("@CostCentreID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_InvoiceFor))
                    sqlparams[67] = new SqlParameter("@InvoiceFor", f_InvoiceFor);
                else
                    sqlparams[67] = new SqlParameter("@InvoiceFor", DBNull.Value);

                if (f_FiscalID.HasValue)
                    sqlparams[68] = new SqlParameter("@FiscalID", f_FiscalID.Value);
                else
                    sqlparams[68] = new SqlParameter("@FiscalID", DBNull.Value);

                if (f_SROAmount.HasValue)
                    sqlparams[69] = new SqlParameter("@SROAmount", f_SROAmount.Value);
                else
                    sqlparams[69] = new SqlParameter("@SROAmount", DBNull.Value);

                if (f_PledgeQuantity.HasValue)
                    sqlparams[70] = new SqlParameter("@PledgeQuantity", f_PledgeQuantity.Value);
                else
                    sqlparams[70] = new SqlParameter("@PledgeQuantity", DBNull.Value);

                if (f_PledgeAmount.HasValue)
                    sqlparams[71] = new SqlParameter("@PledgeAmount", f_PledgeAmount.Value);
                else
                    sqlparams[71] = new SqlParameter("@PledgeAmount", DBNull.Value);

                sb.Append(DatabaseName);

                const string Query_InsertPF = @" INSERT INTO {0}.dbo.[AR_ReceiptTransactionHeader] ([CompanyID], [PeriodID], [ReceiptTransactionID], [DocumentTypeID], [DocumentNo], [DocumentDate], [AddressVerificationCode], [Amount], [AntiCipatedClearingDate], [PayFromCustomerID], [PaymentServerOrderNo], [PostMarkDate], [ProgramID], [ReceiptDate], [ReceiptMethodID], [ReceiptNo], [Comments], [CurrencyId], [BankAccountID], [CustomerBankBranchID], [CustomerReceiptReference], [DepositDate], [ExchangeDate], [ExchangeRate], [ExchangeRateType], [FactorDiscountAmount], [IssueDate], [IssuerBankBranchID], [IssuerName], [OverRemitAccountFlag], [ReceivablesTrxID], [Reference], [ReferenceType], [RemittanceBankAccount], [ReversalCategory], [ReversalComments], [ReversalDate], [ReversalReasonCode], [SelectedForFactoringFlag], [SelectedRemittanceBatchID], [ApprovalCode], [OverrideRemitAccountFlag], [ConfirmedFlag], [LumSumPayment], [Status], [CreatedBy], [CreateDate], [CreatedByValue], [StatusCode], [SourceHeaderID], [CustomerID], [IsInvoiceAdjusted], [AdditionalTaxAmount], [AdditionalTaxCOA], [BusinessPartnerTypeID], [TransactionType], [LocationID], [EmployeeID], [CommissionPercentage], [CommissionCCID], [EmployeeTypeID], [ActivityDate], [ActivityEntryDate], [BankBalance], [ControlCredit], [ControlDebit], [CostCentreID], [InvoiceFor], [FiscalID], [SROAmount], [PledgeQuantity], [PledgeAmount])
		VALUES(@CompanyID, @PeriodID, @ReceiptTransactionID, @DocumentTypeID, @DocumentNo, @DocumentDate, @AddressVerificationCode, @Amount, @AntiCipatedClearingDate, @PayFromCustomerID, @PaymentServerOrderNo, @PostMarkDate, @ProgramID, @ReceiptDate, @ReceiptMethodID, @ReceiptNo, @Comments, @CurrencyId, @BankAccountID, @CustomerBankBranchID, @CustomerReceiptReference, @DepositDate, @ExchangeDate, @ExchangeRate, @ExchangeRateType, @FactorDiscountAmount, @IssueDate, @IssuerBankBranchID, @IssuerName, @OverRemitAccountFlag, @ReceivablesTrxID, @Reference, @ReferenceType, @RemittanceBankAccount, @ReversalCategory, @ReversalComments, @ReversalDate, @ReversalReasonCode, @SelectedForFactoringFlag, @SelectedRemittanceBatchID, @ApprovalCode, @OverrideRemitAccountFlag, @ConfirmedFlag, @LumSumPayment, @Status, @CreatedBy, @CreateDate, @CreatedByValue, @StatusCode, @SourceHeaderID, @CustomerID, @IsInvoiceAdjusted, @AdditionalTaxAmount, @AdditionalTaxCOA, @BusinessPartnerTypeID, @TransactionType, @LocationID, @EmployeeID, @CommissionPercentage, @CommissionCCID, @EmployeeTypeID, @ActivityDate, @ActivityEntryDate, @BankBalance, @ControlCredit, @ControlDebit, @CostCentreID, @InvoiceFor, @FiscalID, @SROAmount, @PledgeQuantity, @PledgeAmount) ";


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

        public static DataTable GetAllActiveDataFiscal(Nullable<int> companyID, Nullable<int> periodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null,int CurrentFiscalID=0)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                DataSet ds = null;

                string Query_SelectAll = @" SELECT AR_ReceiptTransactionHeader.* FROM [AR_ReceiptTransactionHeader] 
                        inner join Set_PeriodCustom on  
                        AR_ReceiptTransactionHeader.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
                        and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID
                        WHERE AR_ReceiptTransactionHeader.[Status] = 1 AND (AR_ReceiptTransactionHeader.[CompanyID] = @CompanyID OR @CompanyID is null) 
                        AND (AR_ReceiptTransactionHeader.[PeriodID] = @PeriodID OR @PeriodID is null) ";

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

        public static DataTable GetDataMovementSummaryLeaseLiablityReport(Nullable<int> companyID, int periodID, int fiscalid, Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FiscalID", fiscalid);
                sqlparams[3] = new SqlParameter("@FromDate", fromDate);
                sqlparams[4] = new SqlParameter("@ToDate", toDate);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_GetData_MovementSummaryLeaseLiablityReport, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_GetData_MovementSummaryLeaseLiablityReport, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_GetData_MovementSummaryLeaseLiablityReport, sqlparams);

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

        public static DataTable GetDataMomentSummaryLeaseLiablity2Report(Nullable<int> companyID, int periodID,int fiscalid, Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FiscalID", fiscalid);
                sqlparams[3] = new SqlParameter("@FromDate", fromDate);
                sqlparams[4] = new SqlParameter("@ToDate", toDate);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_GetData_MomentSummaryLeaseLiablity2Report, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_GetData_MomentSummaryLeaseLiablity2Report, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_GetData_MomentSummaryLeaseLiablity2Report, sqlparams);

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

        public static int GetNewReceiptTransactionIDForPF(int companyID, int periodID, string DatabaseName, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;

            try
            {
                StringBuilder sb = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                sb.Append(DatabaseName);

                string Query_Get_MaxIdForPF = @" SELECT IsNull(max(ReceiptTransactionID), 0) + 1 as ReceiptTransactionID FROM {0}.dbo.AR_ReceiptTransactionHeader WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID ";

                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, string.Format(Query_Get_MaxIdForPF, sb.ToString()), sqlparams));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, string.Format(Query_Get_MaxIdForPF, sb.ToString()), sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, string.Format(Query_Get_MaxIdForPF, sb.ToString()), sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static bool DeleteDataByHeaderID(int companyID, int periodID, int transactionID, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ReceiptTransactionID", transactionID);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByHeaderID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByHeaderID, sqlparams) > 0)
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

        #endregion
    }
}
