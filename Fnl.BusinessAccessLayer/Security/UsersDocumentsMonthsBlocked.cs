using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;
using System.Data;

namespace FnI.BusinessAccessLayer.Security
{
    public partial class UsersDocumentsMonthsBlocked : BaseObject
    {

        public static bool DeleteDataByKey(Guid UserID,int companyID)
        {
            bool result = false;

     

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@UserID", UserID);
                sqlparams[1] = new SqlParameter("@companyID", companyID);
                

                string Query = @" delete from UsersDocumentsMonthsBlocked where UserID =  @UserID and  companyID = @CompanyID  ";


                if (SqlHelper.ExecuteNonQuery(CommandType.Text,  Query, sqlparams) > 0)
                    result = true;

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataTable Get_ByDocUserID(Guid UserID, int CompanyID, int DocumentTypeID)
        {
            
            DataTable dt = null;


            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@UserID", UserID);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", DocumentTypeID);


                string Query = @" select MonthID from UsersDocumentsMonthsBlocked where UserID = @UserID and CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID  ";


                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams).Tables[0];

                

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
