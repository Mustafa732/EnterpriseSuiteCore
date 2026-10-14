using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class UsersInBusinessPartners
    {
        #region Constant String

        const string Query_GetByUserName = @"SELECT * FROM (
SELECT UsersInBusinessPartners.CustomerID ,AR_SET_Customer.CustomerName,AR_SET_BusinessPartner.BusinessPartnerName
,AR_SET_BusinessPartner.BusinessPartnerID
, CONVERT(BIT,'True')AS Checked
FROM UsersInBusinessPartners 
INNER JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = UsersInBusinessPartners.CustomerID
INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = UsersInBusinessPartners.BusinessPartnerID
WHERE UserID = @UserID
UNION all
SELECT AR_SET_Customer.CustomerID,AR_SET_Customer.CustomerName,AR_SET_BusinessPartner.BusinessPartnerName
,AR_SET_BusinessPartner.BusinessPartnerID
, CONVERT(BIT,'False')AS Checked
 FROM AR_SET_Customer
 INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID
 INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
where  AR_SET_Customer.CustomerID NOT IN (select CustomerID from UsersInBusinessPartners where UserID = @UserID) 
)AS tab
ORDER BY tab.CustomerID";
        const string Query_Delete_ByKey = @"DELETE FROM UsersInBusinessPartners Where UserID = @UserID  ";

        #endregion

        #region Method

        public static DataTable GetDataByUser(Guid UserID)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserID", UserID);

                //if (BusinessPartnerID > 0)
                //    sb.Append(" AND AR_SET_BusinessPartner.BusinessPartnerID = " + BusinessPartnerID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetByUserName, sb), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static bool DeleteDataByKey(SqlTransaction objTransaction, Guid UserID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                sqlparams[0] = new SqlParameter("@UserID", @UserID);

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

        #endregion
    }
}
