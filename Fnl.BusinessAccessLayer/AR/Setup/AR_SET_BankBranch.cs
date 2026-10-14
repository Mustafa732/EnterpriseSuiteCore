using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.AR.Setup
{
    public partial class AR_SET_BankBranch : BaseClass
    {
        #region Class Methods

        public DataTable GetAllActiveBankAndBranch(Nullable<bool> isActive = true, Nullable<int> BankID = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@IsActive", isActive);
                sqlparams[1] = new SqlParameter("@BankID", BankID);

                string Query_SelectAllBankAndBranch = @" SELECT (BankName + ' - ' + BankBranchName) as BankBranch, * FROM AR_SET_BankBranch 
    INNER JOIN AR_SET_Bank on AR_SET_Bank.BankID = AR_SET_BankBranch.BankID 
    And AR_SET_Bank.IsActive=@IsActive  AND AR_SET_Bank.Status=1
WHERE AR_SET_BankBranch.Status = 1 AND AR_SET_BankBranch.IsActive = @IsActive
ORDER BY BankName, BankBranchName ";

                string Query_SelectSelectedBankAndBranch = @" SELECT (BankName + ' - ' + BankBranchName) as BankBranch, * FROM AR_SET_BankBranch 
    INNER JOIN AR_SET_Bank on AR_SET_Bank.BankID = AR_SET_BankBranch.BankID 
    And AR_SET_Bank.IsActive=@IsActive  AND AR_SET_Bank.Status=1
WHERE AR_SET_BankBranch.Status = 1 AND AR_SET_BankBranch.IsActive = @IsActive and AR_SET_BankBranch.BankID = @BankID
ORDER BY BankName, BankBranchName ";

                if (BankID > 0)
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectSelectedBankAndBranch, sqlparams).Tables[0];
                }
                else
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllBankAndBranch, sqlparams).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetBranchNameByBankID(int BankID, SqlConnection obj_SQLConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@BankID", BankID);

                string Query_GetBranchName = @" SELECT BankBranchID, BankBranchName FROM AR_SET_BankBranch WHERE Status = 1 AND IsActive = 1 AND BankID = @BankID ";

                if(obj_SQLConnection != null)
                    dt = SqlHelper.ExecuteDataset(obj_SQLConnection, CommandType.Text, Query_GetBranchName, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetBranchName, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetAllActiveBankAndBranchByAccountNo(string AccountNo, Nullable<bool> isActive = true)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@IsActive", isActive);
                sqlparams[1] = new SqlParameter("@AccountNo", AccountNo);
                string Query_SelectAllBankAndBranch = @" Select (BankName + ' - ' + BankBranchName) as BankBranch,* 
		from AR_SET_BankAccount 
		Inner Join AR_Set_BankBranch  on AR_Set_BankBranch.BankBranchID=AR_SET_BankAccount.BankBranchID
		Inner Join AR_Set_Bank  on AR_Set_Bank.BankID=AR_Set_BankBranch.BankID
 
WHERE AR_SET_BankBranch.Status = 1 AND AR_SET_BankBranch.IsActive = @IsActive 
And  BankAccountNumber=@AccountNo
ORDER BY BankName, BankBranchName ";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllBankAndBranch, sqlparams).Tables[0];
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
