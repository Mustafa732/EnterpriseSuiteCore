using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.AR.Setup
{
    public partial class AR_SET_BankAccount : BaseClass
    {
        public DataTable GetCheckNumberFirstTime(int BankBranchID, int BankAccountID, int companyID, int periodID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@BankBranchID", BankBranchID);
                sqlparams[1] = new SqlParameter("@BankAccountID", BankAccountID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);

                string Query_GotCheckNumberFirstTime = @"
Select AR_SET_BankAccount.BankAccountID, AR_SET_BankAccount.BankAccountName, AR_SET_BankAccountDetail.FromNo, AR_SET_BankAccountDetail.ToNo, 
    AR_SET_BankAccountDetail.FromNo As ReferenceNo, AR_SET_BankAccountDetail.DateValidForm 
From AR_SET_BankAccount 
    Inner Join AR_SET_BankAccountDetail ON AR_SET_BankAccount.BankAccountID = AR_SET_BankAccountDetail.BankAccountID 
        And AR_SET_BankAccount.CompanyID = AR_SET_BankAccountDetail.CompanyID And AR_SET_BankAccount.PeriodID = AR_SET_BankAccountDetail.PeriodID 
        And AR_SET_BankAccountDetail.Status = 1 And AR_SET_BankAccountDetail.Active = 1 
Where AR_SET_BankAccount.BankBranchID = @BankBranchID And AR_SET_BankAccount.BankAccountID = @BankAccountID --And AR_SET_BankAccount.CompanyID = @CompanyID 
    And AR_SET_BankAccount.PeriodID = @PeriodID And AR_SET_BankAccount.Status = 1 
group by AR_SET_BankAccount.BankAccountID, AR_SET_BankAccount.BankAccountName, AR_SET_BankAccountDetail.FromNo, AR_SET_BankAccountDetail.ToNo, 
    AR_SET_BankAccountDetail.DateValidForm
";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GotCheckNumberFirstTime, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetCheckNumber(int BankBranchID, int BankAccountID, int companyID, int periodID, SqlTransaction objSQL_Transaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@BankBranchID", BankBranchID);
                sqlparams[1] = new SqlParameter("@BankAccountID", BankAccountID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);

                string Query_GotCheckNumber = @"
Select AR_SET_BankAccount.BankAccountID, AR_SET_BankAccount.BankAccountName, AR_SET_BankAccountDetail.FromNo, AR_SET_BankAccountDetail.ToNo, 
    (
        Select Top 1 ISNULL(Reference, 0) From AP_PaymentHeader 
        Where BankAccountID = AR_SET_BankAccount.BankAccountID --And AP_PaymentHeader.CompanyID = AR_SET_BankAccount.CompanyID 
            And AP_PaymentHeader.PeriodID = AR_SET_BankAccount.PeriodID And Reference IS NOT NULL 
        Order By AP_PaymentHeader.CreateDate desc
    ) As ReferenceNo, 
    AR_SET_BankAccountDetail.DateValidForm 
From AR_SET_BankAccount 
    Inner Join AR_SET_BankAccountDetail ON AR_SET_BankAccount.BankAccountID = AR_SET_BankAccountDetail.BankAccountID 
        And AR_SET_BankAccount.CompanyID = AR_SET_BankAccountDetail.CompanyID And AR_SET_BankAccount.PeriodID = AR_SET_BankAccountDetail.PeriodID 
        And AR_SET_BankAccountDetail.Status = 1 And AR_SET_BankAccountDetail.Active = 1 
Where AR_SET_BankAccount.BankBranchID = @BankBranchID And AR_SET_BankAccount.BankAccountID = @BankAccountID --And AR_SET_BankAccount.CompanyID = @CompanyID 
    And AR_SET_BankAccount.PeriodID = @PeriodID And AR_SET_BankAccount.Status = 1 
order by AR_SET_BankAccountDetail.DateValidForm, AR_SET_BankAccountDetail.FromNo, AR_SET_BankAccountDetail.ToNo ";

                if(objSQL_Transaction != null)
                    dt = SqlHelper.ExecuteDataset(objSQL_Transaction, CommandType.Text, Query_GotCheckNumber, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GotCheckNumber, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetBankAccountByBankBranch(int BankBranchID, int companyID, int periodID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@BankBranchID", BankBranchID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                string Query_GetBankAccountByBankBranch = @" select BankAccountID, BankAccountName, BankAccountNumber from AR_SET_BankAccount 
where BankBranchID = @BankBranchID and PeriodID = @PeriodID ";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetBankAccountByBankBranch, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetBankAccountByBankAccount(int companyID, int periodID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@BankBranchID", BankBranchID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                string Query_GetBankAccountByBankAccount = @" select BankAccountID, BankAccountName, BankAccountNumber from AR_SET_BankAccount 
                   where CompanyID = @CompanyID and PeriodID = @PeriodID ";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetBankAccountByBankAccount, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetBankAccountByBankAccountRefBankID(int companyID, int periodID, int bankid)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@BankID", bankid);

                string Query_GetBankAccountByBankAccount = @"  Select AR_SET_Bank.BankID, AR_SET_Bank.BankName,BankAccountID, BankAccountName, BankAccountNumber 
      from AR_SET_BankAccount 
     Inner Join AR_Set_BankBranch On AR_Set_BankBranch.BankBranchID = AR_Set_BankAccount.BankBranchID 
	       And AR_Set_BankBranch.Status = 1
     Inner Join AR_SET_Bank on AR_SET_Bank.BankID = AR_Set_bankBranch.BankID 
	       And AR_SET_Bank.Status = 1	        
 where AR_SET_BankAccount.Status = 1 And  CompanyID = @CompanyID And PeriodID = @PeriodID And AR_SET_Bank.BankID  = @BankID";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetBankAccountByBankAccount, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllData(int companyID, int periodID, string strWhere)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                strWhere = strWhere + " and [PeriodID] = @PeriodID";

                const string Query_SelectAllAccount = @" SELECT * FROM [AR_SET_BankAccount] WHERE [Status] = 1 AND ([PeriodID] <= @PeriodID OR @PeriodID is null) ";
                
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllAccount + strWhere, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetBankName(int companyID, int periodID, int branchID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@BankBranchID", branchID);


                const string Query = @" SELECT  BankName,BankBranchName,AR_SET_Bank.BankID,BankBranchID FROM AR_SET_BankBranch 
                                    INNER JOIN AR_SET_Bank on AR_SET_Bank.BankID = AR_SET_BankBranch.BankID 
                                    And AR_SET_Bank.IsActive=1  AND AR_SET_Bank.Status=1
                                    WHERE AR_SET_BankBranch.Status = 1 
                                    AND AR_SET_BankBranch.IsActive = 1 
                                    and BankBranchID = @BankBranchID";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query , sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        public bool InsertACHeaderAndDetail(List<AR_SET_BankAccountDetail> colAssetCostDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertBankAccount");

            try
            {
                this.Insert(sqltrans);

                foreach (AR_SET_BankAccountDetail Bnk in colAssetCostDetail)
                {
                    Bnk.BankAccountDetailID = AR_SET_BankAccountDetail.GetNewBankAccountDetailID(f_CompanyID, f_PeriodID, sqltrans);
                    Bnk.Insert(sqltrans);
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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertBankAccount");
            }

            return result;
        }

        public bool UpdateACHeaderAndDetail(List<AR_SET_BankAccountDetail> colAddAssetCostDetail, List<AR_SET_BankAccountDetail> colUpdateAssetCostDetail,
            List<AR_SET_BankAccountDetail> colDeleteAssetCostDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateBankAccount");

            try
            {
                this.UpdateDataByKey(sqltrans);

                foreach (AR_SET_BankAccountDetail ACdAdd in colAddAssetCostDetail)
                {
                    ACdAdd.BankAccountDetailID = AR_SET_BankAccountDetail.GetNewBankAccountDetailID(f_CompanyID, f_PeriodID, sqltrans);
                    ACdAdd.Insert(sqltrans);
                }

                foreach (AR_SET_BankAccountDetail ACdUpdate in colUpdateAssetCostDetail)
                    ACdUpdate.UpdateDataByKey(sqltrans);

                foreach (AR_SET_BankAccountDetail BankDelete in colDeleteAssetCostDetail)
                    AR_SET_BankAccountDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, BankDelete.BankAccountDetailID, f_CompanyID, f_PeriodID);



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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateBankAccount");
            }

            return result;
        }

        public bool DeleteACHeaderAndDetail(List<AR_SET_BankAccountDetail> colDeleteAssetCostDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("DeleteBank");

            try
            {
                DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, UpdatedByValue, f_BankAccountID, f_CompanyID, f_PeriodID);

                foreach (AR_SET_BankAccountDetail ACddDelete in colDeleteAssetCostDetail)
                    AR_SET_BankAccountDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, ACddDelete.BankAccountDetailID, f_CompanyID, f_PeriodID);

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
                    SqlHelper.RollBackTransaction(sqltrans, "DeleteBank");
            }
            return result;
        }

        public DataTable GetAllData_Search(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null, 
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

                    #region Query

                    string Query_SelectAll_WithPaging_Search = @" 
SELECT AR_SET_BankAccount.CompanyID, AR_SET_BankAccount.PeriodID, AR_SET_BankAccount.BankAccountID, AR_SET_BankAccount.BankAccountName, 
    AR_SET_BankAccount.BankAccountNameALT, AR_SET_BankAccount.BankAccountNumber, AR_SET_BankAccount.BankBranchID, AR_SET_BankBranch.BankBranchName, 
    AR_SET_BankAccount.AccountHolderName, AR_SET_BankAccount.AccountHolderNameALT, AR_SET_BankAccount.BankAccountTypeID, AR_SET_BankAccount.MultiCurrency, 
    AR_SET_BankAccount.CurrencyID, AR_SET_BankAccount.Description, AR_SET_BankAccount.InActiveDate, AR_SET_BankAccount.MinCheckAmount, 
    AR_SET_BankAccount.MaxCheckAmount, AR_SET_BankAccount.ChartofAccountID, AR_SET_BankAccount.BankChargesCCID, AR_SET_BankAccount.BankErrorsCCID, 
    AR_SET_BankAccount.LossCCID, AR_SET_BankAccount.AssetCCID, AR_SET_BankAccount.ClearingCCID, AR_SET_BankAccount.EarnedCCID, AR_SET_BankAccount.FactorCCID, 
    AR_SET_BankAccount.FutureDatedPaymentCCID, AR_SET_BankAccount.GainCCID, AR_SET_BankAccount.OnAccountCCID, AR_SET_BankAccount.ReceiptClearingCCID, 
    AR_SET_BankAccount.RemittanceCCID, AR_SET_BankAccount.ShortTermDepositCCID, AR_SET_BankAccount.UnAppliedCCID, AR_SET_BankAccount.UnEarnedCCID, 
    AR_SET_BankAccount.UnIdentifiedCCID, AR_SET_BankAccount.BankGLAccountCCID, AR_SET_BankAccount.Status, AR_SET_BankAccount.CreatedBy, AR_SET_BankAccount.CreateDate, 
    AR_SET_BankAccount.UpdatedBy, AR_SET_BankAccount.UpdateDate, AR_SET_BankAccount.CreatedByValue, AR_SET_BankAccount.UpdatedByValue, 
	AR_SET_Bank.BankName,
    AR_SET_BankAccount.OperatingType, AR_SET_BankAccount.PaymentCCID, ROW_NUMBER() Over (Order by BankAccountID, CompanyID, PeriodID) as 'Row' 
FROM AR_SET_BankAccount 
    INNER JOIN AR_SET_BankBranch ON AR_SET_BankBranch.BankBranchID = AR_SET_BankAccount.BankBranchID
	inner JOin AR_SET_Bank On AR_SET_Bank.BankID=AR_SET_BankBranch.BankID ";

                    #endregion

                    SqlParameter[] sqlparams = sqlparamsList.ToArray();
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_WithPaging_Search + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPaging_Search + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    #region Query

                    string Query_SelectAll_WithoutPaging_Search = @"SELECT [AR_SET_BankAccount].[CompanyID], [AR_SET_BankAccount].[PeriodID], [AR_SET_BankAccount].[BankAccountID], 
 [AR_SET_BankAccount].[BankAccountName], [AR_SET_BankAccount].[BankAccountNameALT], [AR_SET_BankAccount].[BankAccountNumber], 
 [AR_SET_BankAccount].[BankBranchID], [AR_SET_BankBranch].[BankBranchName], [AR_SET_BankAccount].[AccountHolderName], [AR_SET_BankAccount].[AccountHolderNameALT], 
 [AR_SET_BankAccount].[BankAccountTypeID], [AR_SET_BankAccount].[MultiCurrency], [AR_SET_BankAccount].[CurrencyID], 
 [AR_SET_BankAccount].[Description], [AR_SET_BankAccount].[InActiveDate], [AR_SET_BankAccount].[MinCheckAmount], 
 [AR_SET_BankAccount].[MaxCheckAmount], [AR_SET_BankAccount].[ChartofAccountID], [AR_SET_BankAccount].[BankChargesCCID], 
 [AR_SET_BankAccount].[BankErrorsCCID], [AR_SET_BankAccount].[LossCCID], [AR_SET_BankAccount].[AssetCCID], 
 [AR_SET_BankAccount].[ClearingCCID], [AR_SET_BankAccount].[EarnedCCID], [AR_SET_BankAccount].[FactorCCID], 
 [AR_SET_BankAccount].[FutureDatedPaymentCCID], [AR_SET_BankAccount].[GainCCID], [AR_SET_BankAccount].[OnAccountCCID], 
 [AR_SET_BankAccount].[ReceiptClearingCCID], [AR_SET_BankAccount].[RemittanceCCID], [AR_SET_BankAccount].[ShortTermDepositCCID], 
 [AR_SET_BankAccount].[UnAppliedCCID], [AR_SET_BankAccount].[UnEarnedCCID], [AR_SET_BankAccount].[UnIdentifiedCCID], 
 [AR_SET_BankAccount].[BankGLAccountCCID], [AR_SET_BankAccount].[Status], [AR_SET_BankAccount].[CreatedBy], 
 [AR_SET_BankAccount].[CreateDate], [AR_SET_BankAccount].[UpdatedBy], [AR_SET_BankAccount].[UpdateDate], 
 [AR_SET_BankAccount].[CreatedByValue], [AR_SET_BankAccount].[UpdatedByValue], [AR_SET_BankAccount].[OperatingType], 
 [AR_SET_BankAccount].[PaymentCCID] 
 FROM [AR_SET_BankAccount]
INNER JOIN [AR_SET_BankBranch] ON [AR_SET_BankBranch].[BankBranchID] = [AR_SET_BankAccount].[BankBranchID]";

                    #endregion

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_WithoutPaging_Search + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public AR_SET_BankAccount(int bankAccountID, int? BankBranchID, int periodID, string objConnection = null, string objConnectionA = null, string objConnectionB = null)
        {
            LoadAcccounts(bankAccountID, BankBranchID, periodID);
        }

        private void LoadAcccounts(int bankAccountID, int? BankBranchID, int periodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow drRecord = GetDataByID(bankAccountID,BankBranchID, periodID, objConnection, objTransaction);

            if (drRecord != null)
            {
                this.f_IsDataLoaded = true;
                this.f_CompanyID = Convert.ToInt32(drRecord["CompanyID"]);
                this.f_PeriodID = Convert.ToInt32(drRecord["PeriodID"]);
                this.f_BankAccountID = Convert.ToInt32(drRecord["BankAccountID"]);
                this.f_BankAccountName = Convert.ToString(drRecord["BankAccountName"]);

                if (drRecord["BankAccountNameALT"] != DBNull.Value)
                    this.f_BankAccountNameALT = Convert.ToString(drRecord["BankAccountNameALT"]);

                this.f_BankAccountNumber = Convert.ToString(drRecord["BankAccountNumber"]);
                this.f_BankBranchID = Convert.ToInt32(drRecord["BankBranchID"]);

                if (drRecord["AccountHolderName"] != DBNull.Value)
                    this.f_AccountHolderName = Convert.ToString(drRecord["AccountHolderName"]);


                if (drRecord["AccountHolderNameALT"] != DBNull.Value)
                    this.f_AccountHolderNameALT = Convert.ToString(drRecord["AccountHolderNameALT"]);

                this.f_BankAccountTypeID = Convert.ToInt32(drRecord["BankAccountTypeID"]);
                this.f_MultiCurrency = Convert.ToBoolean(drRecord["MultiCurrency"]);

                if (drRecord["CurrencyID"] != DBNull.Value)
                    this.f_CurrencyID = Convert.ToInt32(drRecord["CurrencyID"]);


                if (drRecord["Description"] != DBNull.Value)
                    this.f_Description = Convert.ToString(drRecord["Description"]);


                if (drRecord["InActiveDate"] != DBNull.Value)
                    this.f_InActiveDate = Convert.ToDateTime(drRecord["InActiveDate"]);


                if (drRecord["MinCheckAmount"] != DBNull.Value)
                    this.f_MinCheckAmount = Convert.ToDouble(drRecord["MinCheckAmount"]);


                if (drRecord["MaxCheckAmount"] != DBNull.Value)
                    this.f_MaxCheckAmount = Convert.ToDouble(drRecord["MaxCheckAmount"]);

                this.f_ChartofAccountID = Convert.ToInt32(drRecord["ChartofAccountID"]);

                if (drRecord["BankChargesCCID"] != DBNull.Value)
                    this.f_BankChargesCCID = Convert.ToString(drRecord["BankChargesCCID"]);


                if (drRecord["BankErrorsCCID"] != DBNull.Value)
                    this.f_BankErrorsCCID = Convert.ToString(drRecord["BankErrorsCCID"]);


                if (drRecord["LossCCID"] != DBNull.Value)
                    this.f_LossCCID = Convert.ToString(drRecord["LossCCID"]);


                if (drRecord["AssetCCID"] != DBNull.Value)
                    this.f_AssetCCID = Convert.ToString(drRecord["AssetCCID"]);


                if (drRecord["ClearingCCID"] != DBNull.Value)
                    this.f_ClearingCCID = Convert.ToString(drRecord["ClearingCCID"]);


                if (drRecord["EarnedCCID"] != DBNull.Value)
                    this.f_EarnedCCID = Convert.ToString(drRecord["EarnedCCID"]);


                if (drRecord["FactorCCID"] != DBNull.Value)
                    this.f_FactorCCID = Convert.ToString(drRecord["FactorCCID"]);


                if (drRecord["FutureDatedPaymentCCID"] != DBNull.Value)
                    this.f_FutureDatedPaymentCCID = Convert.ToString(drRecord["FutureDatedPaymentCCID"]);


                if (drRecord["GainCCID"] != DBNull.Value)
                    this.f_GainCCID = Convert.ToString(drRecord["GainCCID"]);


                if (drRecord["OnAccountCCID"] != DBNull.Value)
                    this.f_OnAccountCCID = Convert.ToString(drRecord["OnAccountCCID"]);


                if (drRecord["ReceiptClearingCCID"] != DBNull.Value)
                    this.f_ReceiptClearingCCID = Convert.ToString(drRecord["ReceiptClearingCCID"]);


                if (drRecord["RemittanceCCID"] != DBNull.Value)
                    this.f_RemittanceCCID = Convert.ToString(drRecord["RemittanceCCID"]);


                if (drRecord["ShortTermDepositCCID"] != DBNull.Value)
                    this.f_ShortTermDepositCCID = Convert.ToString(drRecord["ShortTermDepositCCID"]);


                if (drRecord["UnAppliedCCID"] != DBNull.Value)
                    this.f_UnAppliedCCID = Convert.ToString(drRecord["UnAppliedCCID"]);


                if (drRecord["UnEarnedCCID"] != DBNull.Value)
                    this.f_UnEarnedCCID = Convert.ToString(drRecord["UnEarnedCCID"]);


                if (drRecord["UnIdentifiedCCID"] != DBNull.Value)
                    this.f_UnIdentifiedCCID = Convert.ToString(drRecord["UnIdentifiedCCID"]);


                if (drRecord["BankGLAccountCCID"] != DBNull.Value)
                    this.f_BankGLAccountCCID = Convert.ToString(drRecord["BankGLAccountCCID"]);

                this.f_Status = Convert.ToBoolean(drRecord["Status"]);
                this.f_CreatedBy = Guid.Parse(drRecord["CreatedBy"].ToString());
                this.f_CreateDate = Convert.ToDateTime(drRecord["CreateDate"]);

                if (drRecord["UpdatedBy"] != DBNull.Value)
                    this.f_UpdatedBy = Guid.Parse(drRecord["UpdatedBy"].ToString());


                if (drRecord["UpdateDate"] != DBNull.Value)
                    this.f_UpdateDate = Convert.ToDateTime(drRecord["UpdateDate"]);

                this.f_CreatedByValue = Convert.ToString(drRecord["CreatedByValue"]);

                if (drRecord["UpdatedByValue"] != DBNull.Value)
                    this.f_UpdatedByValue = Convert.ToString(drRecord["UpdatedByValue"]);


                if (drRecord["OperatingType"] != DBNull.Value)
                    this.f_OperatingType = Convert.ToString(drRecord["OperatingType"]);


                if (drRecord["PaymentCCID"] != DBNull.Value)
                    this.f_PaymentCCID = Convert.ToString(drRecord["PaymentCCID"]);


                if (drRecord["ShowInBankReport"] != DBNull.Value)
                    this.f_ShowInBankReport = Convert.ToBoolean(drRecord["ShowInBankReport"]);


                if (drRecord["DocumentGroupID"] != DBNull.Value)
                    this.f_DocumentGroupID = Convert.ToInt32(drRecord["DocumentGroupID"]);


                if (drRecord["ShowInBankStatusReport"] != DBNull.Value)
                    this.f_ShowInBankStatusReport = Convert.ToBoolean(drRecord["ShowInBankStatusReport"]);

                if (drRecord["CostCentreID"] != DBNull.Value)
                    this.f_CostCentreID = Convert.ToInt32(drRecord["CostCentreID"]);

            }
        }

        public static DataRow GetDataByID(int bankAccountID, int? BankBranchID, int periodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@BankAccountID", bankAccountID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@BankBranchID", BankBranchID);
                
                DataSet ds = null;
                const string Query_GetData_byID = @" SELECT * FROM [AR_SET_BankAccount] WHERE [BankAccountID] = @BankAccountID and BankBranchID = @BankBranchID and [PeriodID] <= @PeriodID ";
                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byID, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byID, sqlparams);

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

        public static DataTable GetDataForBankAccountReport(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> bankID, Nullable<int> branchID, Nullable<int> bankAccountNo, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@BankID", bankID);
                sqlparams[3] = new SqlParameter("@BranchID", branchID);
                sqlparams[4] = new SqlParameter("@BankAccountNo", bankAccountNo);

                DataSet ds = null;

                const string query = @" SELECT [AR_SET_Bank].BankName,AR_SET_BankBranch.BankBranchName,BankAccountName,BankAccountNumber,BankAccountTypeName,PaymentCCID,GL_CodeCombinationDetail.Description,
DocumentGroupID,GroupName,ShowInBankStatusReport,ShowInBankReport,IsShowInCashReport
FROM [AR_SET_BankAccount]
inner join AR_SET_BankBranch ON AR_SET_BankAccount.BankBranchID = AR_SET_BankBranch.BankBranchID
inner join AR_SET_Bank ON AR_SET_BankBranch.BankID = AR_SET_Bank.BankID
inner join SET_BankAccountType ON AR_SET_BankAccount.BankAccountTypeID = SET_BankAccountType.BankAccountTypeID
left join GL_CodeCombinationDetail on GL_CodeCombinationDetail.CodeCombinationID = AR_SET_BankAccount.PaymentCCID
inner join DocumentTypesGroup ON AR_SET_BankAccount.DocumentGroupID = DocumentTypesGroup.GroupID
WHERE [AR_SET_BankAccount].[Status] = 1 AND ([AR_SET_BankAccount].[CompanyID] = @CompanyID OR @CompanyID is null) 
AND ([PeriodID] = @PeriodID OR @PeriodID is null)
AND (AR_SET_Bank.BankID = @BankID OR @BankID is null)
And (AR_SET_BankBranch.BankBranchID = @BranchID OR @BranchID is null)
AND (AR_SET_BankAccount.BankAccountID = @BankAccountNo OR @BankAccountNo is null)
";

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, query, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

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
    }
}
