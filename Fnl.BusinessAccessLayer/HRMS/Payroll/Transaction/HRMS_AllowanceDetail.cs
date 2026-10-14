using DAL.DataAccessLayer;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Transaction
{
    public partial class HRMS_AllowanceDetail
    {
        public static bool DeleteDataBySalaryHeader2(int companyID, int periodID, int SalaryHeaderID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            bool result = false;

            #region Query

            const string Query_DeleteData_BySalaryHeaderID = @" update [HRMS_AllowanceDetail]  set Status = 0 WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and SalaryHeaderID = @SalaryHeaderID ";
            #endregion
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SalaryHeaderID", SalaryHeaderID);


                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteData_BySalaryHeaderID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DeleteData_BySalaryHeaderID, sqlparams) > 0)
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
    }
}
