using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;
using System.Text;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Transaction
{
    public partial class HRMS_EmployeeInEmployement_Status
    {
        const string Query_Delete_ByKey = @"DELETE FROM HRMS_EmployeeInEmployement_Status Where UserID = @UserID  and CompanyID = @CompanyID ";

        const string Query_GetByUserName = @"SELECT * FROM (
SELECT HRMS_EmployeeInEmployement_Status.StatusID ,[HRMS_Employement_Status].Description
, CONVERT(BIT,'True')AS Checked
FROM HRMS_EmployeeInEmployement_Status 
INNER JOIN [HRMS_Employement_Status] ON [HRMS_Employement_Status].StatusID = HRMS_EmployeeInEmployement_Status .StatusID
Where  UserID = @UserID and HRMS_EmployeeInEmployement_Status.CompanyID = @CompanyID

UNION all
SELECT [HRMS_Employement_Status].StatusID ,[HRMS_Employement_Status].Description
, CONVERT(BIT,'False')AS Checked
 FROM [HRMS_Employement_Status] 
WHERE [HRMS_Employement_Status] .[Status] = 1 --AND [IsActive] = 1-- AND CodeCombinationID LIKE '%-%-%-%-%'
and [HRMS_Employement_Status].StatusID NOT IN (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = @UserID and CompanyID = @CompanyID)

)AS tab
ORDER BY tab.StatusID";

        public static bool DeleteDataByKey(SqlTransaction objTransaction, Guid UserID, int CompanyID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@UserID", UserID);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
                

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByKey, sqlparams) > 0)
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
        public static DataTable GetDataByUser(Guid UserID, int CompanyID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@UserID", UserID);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetByUserName, sqlparams).Tables[0];
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
