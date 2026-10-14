using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Security
{
    public partial class Set_DocumentSecurity
    {
        #region Constant Queries
        const string Query_MaxId = @" SELECT IsNull(max(ID), 0) as ID FROM Set_DocumentSecurity";
        const string QueryUpdate_By_PrimaryKey = @"UPDATE [Set_DocumentSecurity] SET [Date] = @Date, [LockForTransaction] = @LockForTransaction, [UpdateDate] = @UpdateDate, [UpdatedBy] = @UpdatedBy, [UpdatedByValue] = @UpdatedByValue, [Status] = @Status
		WHERE [ID] = @ID ";

        const string Query_GetMaximumDateForSecurity = @" Select top 1 Date from Set_DocumentSecurity
Where LockForTransaction=1 AND status=1
 Order by Date Desc ";
        #endregion

        #region Class Methods
        public bool UpdateData_ByKey(int? id, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@ID", id);
                sqlparams[1] = new SqlParameter("@Date", f_Date);
                sqlparams[2] = new SqlParameter("@LockForTransaction", f_LockForTransaction);

                if (f_UpdateDate.HasValue)
                    sqlparams[3] = new SqlParameter("@UpdateDate", f_UpdateDate.Value);
                else
                    sqlparams[3] = new SqlParameter("@UpdateDate", DBNull.Value);

                if (f_UpdatedBy.HasValue)
                    sqlparams[4] = new SqlParameter("@UpdatedBy", f_UpdatedBy.Value);
                else
                    sqlparams[4] = new SqlParameter("@UpdatedBy", DBNull.Value);

                if (!string.IsNullOrEmpty(f_UpdatedByValue))
                    sqlparams[5] = new SqlParameter("@UpdatedByValue", f_UpdatedByValue);
                else
                    sqlparams[5] = new SqlParameter("@UpdatedByValue", DBNull.Value);

                if (f_Status.HasValue)
                    sqlparams[6] = new SqlParameter("@Status", f_Status.Value);
                else
                    sqlparams[6] = new SqlParameter("@Status", DBNull.Value);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, QueryUpdate_By_PrimaryKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, QueryUpdate_By_PrimaryKey, sqlparams) > 0)
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

        public static int GetMaxID(SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;

            try
            {
                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_MaxId));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_MaxId));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_MaxId));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static int GetMaxIDbyLockForTransaction(bool LockForTransaction, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;
            try
            {
                string Query_GetMaxIDbyLockForTransaction = @" SELECT IsNull(max(ID), 0) as ID FROM Set_DocumentSecurity";

                string Fillter = " Where LockForTransaction = " + (LockForTransaction == false ? "1" : "0") ;

                if (LockForTransaction)
                    Query_GetMaxIDbyLockForTransaction = @" SELECT IsNull(min(ID), 0) as ID FROM Set_DocumentSecurity";

                



                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_GetMaxIDbyLockForTransaction + Fillter));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_GetMaxIDbyLockForTransaction + Fillter));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetMaxIDbyLockForTransaction + Fillter));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static DataRow GetMaximumDateForSecurity(SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                //SqlParameter[] sqlparams = new SqlParameter[3];
                //sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                //sqlparams[1] = new SqlParameter("@DocumentTypeID", documentTypeID);
                //sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetMaximumDateForSecurity);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetMaximumDateForSecurity);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetMaximumDateForSecurity);

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

        public static DataRow GetDataByID(int iD, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@ID", iD);

                DataSet ds = null;

                const string Query_GetDataByID = @" SELECT * FROM Set_DocumentSecurity WHERE [ID] = @ID";

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetDataByID, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDataByID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByID, sqlparams);

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
