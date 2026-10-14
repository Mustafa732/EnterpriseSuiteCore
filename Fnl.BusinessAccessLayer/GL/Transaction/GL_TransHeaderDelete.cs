using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.GL.Transaction
{
     public partial class GL_TransHeaderDelete : BaseClass
    {
        #region Constant String
         const string Query_GetDataByDocumentType = @"Insert into GL_TransHeaderDelete select * from GL_TransactionHeader where CompanyID=@CompanyID AND PeriodID=@PeriodID AND DocumentTypeID=@DocumentTypeID AND DocumentNo=@DocumentNo and FiscalID = @FiscalID";

         const string Query_PutDataIntoGL_Replica = @"Insert into GL_TransHeaderDelete select * from GL_TransactionHeader where CompanyID=@CompanyID AND PeriodID=@PeriodID AND DocumentTypeID=@DocumentTypeID AND DocumentNo=@DocumentNo and FiscalID = @FiscalID";
        #endregion

        #region Methods

         public static bool GetDataByDocumentType(int companyID, int periodID, int documentTypeID, int documentNo, int FiscalID)
         {
             bool result = false;

             try
             {
                 SqlParameter[] sqlparams = new SqlParameter[5];
                 sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                 sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                 sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                 sqlparams[3] = new SqlParameter("@DocumentNo", documentNo);
                 sqlparams[4] = new SqlParameter("@FiscalID", FiscalID);
                 
                 //DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByDocumentType, sqlparams);

                 if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_PutDataIntoGL_Replica, sqlparams) > 0)
                         result = true;                
                                        
            }
			catch (Exception ex)
			{
				SqlHelper.LogException(ex);
				throw ex;
			}

			return result;
		}

         public static bool PutDataIntoGL_Replica(int companyID, int periodID, int documentTypeID, int documentNo, int FiscalID)
          {
              bool result = false;

              try
              {
                  SqlParameter[] sqlparams = new SqlParameter[5];
                  sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                  sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                  sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                  sqlparams[3] = new SqlParameter("@DocumentNo", documentNo);
                  sqlparams[4] = new SqlParameter("@FiscalID", FiscalID);
                  
                  //DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByDocumentType, sqlparams);

                  if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_GetDataByDocumentType, sqlparams) > 0)
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
