using System;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.AR.Setup
{
    public partial class AR_SET_BankBranch : BaseClass
    {
        #region Class Methods

        /// <summary>
        /// Gets Data by Bank ID
        /// </summary>
        public DataTable GetAllActiveData(int bankID, Nullable<bool> isActive = true)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] SqlParam = new SqlParameter[2];
                SqlParam[0] = new SqlParameter("@BankID", bankID);

                if (isActive.HasValue)
                    SqlParam[1] = new SqlParameter("@IsActive", isActive.Value);
                else
                    SqlParam[1] = new SqlParameter("@IsActive", DBNull.Value);

                string Query_SelectAll_By_Bank = @" 
SELECT BankBranchID, BankBranchName, [BranchCode]
      ,[Phone]
      ,[ContactPerson] , [Status], BankID, IsActive, CreatedBy, CreateDate, UpdatedBy, UpdateDate, CreatedByValue, UpdatedByValue FROM AR_SET_BankBranch 
Where (BankId = @BankID OR @BankID is null) AND [Status] = 1 AND (IsActive = @IsActive OR @IsActive is null); ";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_By_Bank, SqlParam).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        /// <summary>
        /// Permanently Delete all Bank Branches on the basis of Bank
        /// </summary>
        internal bool DeletePermanentByForeignKey(SqlTransaction objTransaction, int bankID)
        {
            bool result = false;

            try
            {
                SqlParameter[] SqlParam = new SqlParameter[1];
                SqlParam[0] = new SqlParameter("@BankID", bankID);

                string Query_Delete_ByForeignKey = @" DELETE FROM AR_SET_BankBranch WHERE BankID = @BankID; ";

                if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByForeignKey, SqlParam) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw new BusinessLogicException("Error In Delete, Class Branch" + ex.Message);
            }

            return result;
        }

        /// <Summary>
        ///  Permanently Delete Branch
        ///  </Summary>
        internal static bool DeletePermanentByKey(SqlTransaction objTransaction, int bankbranchID)
        {
            bool result = false;

            try
            {
                SqlParameter[] SqlParam = new SqlParameter[1];
                SqlParam[0] = new SqlParameter("@BankBranchID", bankbranchID);

                string Query_PermanentDelete_ByKey = @" DELETE FROM AR_SET_BankBranch WHERE BankBranchID = @BankBranchID; ";

                if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_PermanentDelete_ByKey, SqlParam) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw new BusinessLogicException("Error In Delete. Class: Set_BankBranch, Function: DeletePermanentByKey. " + ex.Message + ex.StackTrace);
            }
            return result;
        }

        #endregion
    }
}
