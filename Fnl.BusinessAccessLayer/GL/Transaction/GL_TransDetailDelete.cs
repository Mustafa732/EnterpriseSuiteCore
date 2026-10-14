using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.GL.Transaction
{
    public partial class GL_TransDetailDelete : BaseClass
    {
        #region Methods

        public bool GetDataByDocumentType(int companyID, int periodID, int transID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TransactionID", transID);                
                
                if (SqlHelper.ExecuteNonQuery(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransDetailDelete.Query_GetDataByDocumentType, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public bool PutDataIntoGLDt_Replica(int companyID, int periodID, int transID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TransactionID", transID);

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransDetailDelete.Query_PutDataIntoGLDt_Replica, sqlparams) > 0)
                    result = true;
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
