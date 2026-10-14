using DAL.DataAccessLayer;
using System;
using System.Data;
using Microsoft.Data.SqlClient;
namespace FnI.BusinessAccessLayer.Setup
{
    public partial class HRMS_Set_Machine
    {

     const string Query_SelectAllWithoutCompanyID = @" SELECT * FROM [HRMS_Set_Machine] WHERE [Status] = 1 AND ([IsActive] = @IsActive OR @IsActive is null)";


        public static DataTable GetAllActiveDataWithoutCompanyID(Nullable<bool> isActive = true, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
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
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAllWithoutCompanyID, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAllWithoutCompanyID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllWithoutCompanyID, sqlparams);

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
