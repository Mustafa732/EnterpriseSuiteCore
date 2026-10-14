using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;

using Common.Utils;

using DAL.DataAccessLayer;
namespace FnI.BusinessAccessLayer.Inventory.Setup
{
    public partial class Stoppages_ReasonDetail : BaseClass
    {
        const string Query_SelectAllWithTable = @" SELECT * FROM [Stoppages_ReasonDetail] WHERE [Status] = 1 and StoppageID = @StoppageID";
        const string Query_Delete_ByStoppageID = @" Update [Stoppages_ReasonDetail] Set [Status] = @Status , [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [StoppageID] = @StoppageID ";
        const string Query_Get_MaxDetailId = @" SELECT IsNull(max(StoppagesDetailID), 0) + 1 as StoppagesDetailID FROM Stoppages_ReasonDetail";
        public DataTable GetAllActiveData(int StoppageID, Nullable<bool> isActive = true)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] SqlParam = new SqlParameter[2];
                SqlParam[0] = new SqlParameter("@StoppageID", StoppageID);

                if (isActive.HasValue)
                    SqlParam[1] = new SqlParameter("@IsActive", isActive.Value);
                else
                    SqlParam[1] = new SqlParameter("@IsActive", DBNull.Value);

                string Query_SelectAll_By_StoppagesAndReason = @" 
SELECT * FROM Stoppages_ReasonDetail 
Where (StoppageID = @StoppageID OR @StoppageID is null) AND [Status] = 1";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_By_StoppagesAndReason, SqlParam).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetAllDataWithDetail(int stopageid ,SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                DataSet ds = null;

                SqlParameter[] SqlParam = new SqlParameter[1];
                SqlParam[0] = new SqlParameter("@StoppageID", stopageid);

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAllWithTable, SqlParam);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAllWithTable, SqlParam);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllWithTable, SqlParam);

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

        internal bool DeletePermanentByForeignKey(SqlTransaction objTransaction, int StoppageID)
        {
            bool result = false;

            try
            {
                SqlParameter[] SqlParam = new SqlParameter[1];
                SqlParam[0] = new SqlParameter("@StoppageID", StoppageID);

                string Query_Delete_ByForeignKey = @" DELETE FROM Stoppages_ReasonDetail WHERE StoppageID = @StoppageID; ";

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


       
          internal static bool DeletePermanentByKey(SqlTransaction objTransaction, int StoppagesDetailID)
        {
            bool result = false;

            try
            {
                SqlParameter[] SqlParam = new SqlParameter[1];
                SqlParam[0] = new SqlParameter("@StoppagesDetailID", StoppagesDetailID);

                string Query_PermanentDelete_ByKey = @" DELETE FROM Stoppages_ReasonDetail WHERE StoppagesDetailID = @StoppagesDetailID; ";

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


          public static bool DeleteDataByStoppageID(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, int StoppageID)
          {
              bool result = false;

              try
              {
                  SqlParameter[] sqlparams = new SqlParameter[5];
                  sqlparams[0] = new SqlParameter("@StoppageID", StoppageID);
                  sqlparams[1] = new SqlParameter("@Status", false);
                  sqlparams[2] = new SqlParameter("@UpdatedBy", updatedBy);
                  sqlparams[3] = new SqlParameter("@UpdateDate", updateDate);
                  sqlparams[4] = new SqlParameter("@UpdatedByValue", updatedByValue);

                  if (objTransaction == null)
                  {
                      if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByStoppageID, sqlparams) > 0)
                          result = true;
                  }
                  else
                  {
                      if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByStoppageID, sqlparams) > 0)
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


          public static int GetNewDetailID(int StoppageID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
          {
              int ID = 0;

              try
              {
                  SqlParameter[] sqlparams = new SqlParameter[1];
                  sqlparams[0] = new SqlParameter("@StoppageID", StoppageID);

                  if (objTransaction != null)
                      ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_MaxDetailId, sqlparams));
                  else if (objConnection != null)
                      ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_Get_MaxDetailId, sqlparams));
                  else
                      ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_MaxDetailId, sqlparams));
              }
              catch (Exception ex)
              {
                  SqlHelper.LogException(ex);
                  throw ex;
              }

              return ID;
          }

    

    }
}
