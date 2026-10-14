using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Setup
{
    public partial class HRMS_Set_Appraisal
    {
        #region Constant Strings

        const string Query_SelectAllCoreData = @" SELECT * FROM [HRMS_Set_Appraisal] WHERE [Status] = 1 AND ([IsActive] = @IsActive OR @IsActive is null) AND [ParentAppraisalName] is null ";

        const string Query_GetData_byParentName = @" SELECT [AppraisalCode] FROM [HRMS_Set_Appraisal] WHERE [Status] = 1 AND ([IsActive] = @IsActive OR @IsActive is null) AND [AppraisalName] = @ParentAppraisalName ";

        #endregion

        #region Class Methods

        public static DataTable GetAllActiveCoreData(Nullable<bool> isActive = true, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                if (isActive.HasValue)
                    sqlparams[0] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[0] = new SqlParameter("@IsActive", DBNull.Value);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAllCoreData, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAllCoreData, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllCoreData, sqlparams);

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

        public static int GetCode_byParentName(string ParentAppraisalName, Nullable<bool> isActive = true, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            int _Return=0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                if (isActive.HasValue)
                    sqlparams[0] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[0] = new SqlParameter("@IsActive", DBNull.Value);

                sqlparams[1] = new SqlParameter("@ParentAppraisalName", ParentAppraisalName);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byParentName, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byParentName, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byParentName, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    _Return =int.Parse(ds.Tables[0].Rows[0][0].ToString());
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return _Return;
        }

        #endregion

    }
}
