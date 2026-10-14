using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.AR.Setup
{
    public partial class AR_SET_BankAccount : BaseClass
    {
        #region Class Methods

        public DataTable GetAllActiveData(Nullable<int> companyID, Nullable<int> periodID, int BankBranchID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@BankBranchID", BankBranchID);

                string Query_SelectAll_By_BankBranch = @" SELECT * FROM AR_SET_BankAccount 
WHERE BankBranchID = @BankBranchID and Status = 1  AND (PeriodID = @PeriodID OR @PeriodID is null) ";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_By_BankBranch, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataRow GetDep_CCenter_ByKey(int bankAccountID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;
            const string Query_GetDep_CostCenter_byKey = @"SELECT SET_Department.DepartmentName,SET_Department.DepartmentID,CostCentreID,CostCenterName,BankAccountID,* 
FROM [AR_SET_BankAccount] 
	Inner Join SET_DepartmentsInCC on SET_DepartmentsInCC.FK_CostCenterID=AR_SET_BankAccount.CostCentreID
	inner Join SET_Department on SET_Department.DepartmentID= SET_DepartmentsInCC.FK_DepartmentID
	inner Join SET_CostCenter on SET_CostCenter.CostCenterID=SET_DepartmentsInCC.FK_CostCenterID
WHERE [BankAccountID] = @BankAccountID  And AR_SET_BankAccount.Status=1
";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@BankAccountID", bankAccountID);
                //sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                //sqlparams[2] = new SqlParameter("@PeriodID", PeriodID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetDep_CostCenter_byKey, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDep_CostCenter_byKey, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDep_CostCenter_byKey, sqlparams);

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

        public static DataRow GetGLCodeForBankAccount(int bankAccountID,string BankAccountName, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;
            const string Query_GetDep_CostCenter_byKey = @"select BankAccountID,PaymentCCID,GL_CodeCombinationDetail.Description from AR_SET_BankAccount
 left join GL_CodeCombinationDetail on GL_CodeCombinationDetail.CodeCombinationID = AR_SET_BankAccount.PaymentCCID


WHERE [BankAccountID] = @BankAccountID  And AR_SET_BankAccount.Status=1
AND BankAccountName = @BankAccountName
";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@BankAccountID", bankAccountID);
                sqlparams[1] = new SqlParameter("@BankAccountName", BankAccountName);
                //sqlparams[2] = new SqlParameter("@PeriodID", PeriodID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetDep_CostCenter_byKey, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDep_CostCenter_byKey, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDep_CostCenter_byKey, sqlparams);

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


        #endregion
    }
}
