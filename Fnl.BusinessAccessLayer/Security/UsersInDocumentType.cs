using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;
using Common.Utils;
using System.Data;

namespace FnI.BusinessAccessLayer.Security
{
    public partial class UsersInDocumentType
    {

        #region Const Strings

        const string Query_Delete_ByKey = @"Delete From UsersInDocumentType where UserId = @UserId and CompanyID = @CompanyID ";

        #endregion

        #region Methods

        public static bool DeleteDataByKey(int companyID, Guid userID, SqlTransaction trans)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@UserId", userID);
            
                if (SqlHelper.ExecuteNonQuery(trans, CommandType.Text, Query_Delete_ByKey, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw new BusinessLogicException("Error in Delete class UsersInDocumentType " + ex.Message);
            }

            return result;
        }

        public bool InsertUserInDocumentType(List<UsersInDocumentType> colUserInDT)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertUser_DT");

            try
            {
                //uo.Insert(sqltrans);

                foreach (UsersInDocumentType uc in colUserInDT)
                    uc.Insert(sqltrans);

                SqlHelper.CommitTransaction(sqltrans);
                result = true;
            }
            catch (BusinessLogicException ex)
            {
                SqlHelper.RollBackTransaction(sqltrans, "InsertUser_DT");
                SqlHelper.LogException(ex);
                throw ex;
            }
            catch (Exception ex)
            {
                SqlHelper.RollBackTransaction(sqltrans, "InsertUser_DT");
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public bool UpdateUser_CompaniesPeriodsAndRoles(int companyID, UserOptions uo,List<UsersInDocumentType> colUserInDT)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateUser_DT");

            try
            {

                UsersInDocumentType.DeleteDataByKey(companyID, uo.UserId, sqltrans);
                
                uo.UpdateDataByKey(sqltrans);

                foreach (UsersInDocumentType uc in colUserInDT)
                    uc.Insert(sqltrans);

                SqlHelper.CommitTransaction(sqltrans);
                result = true;
            }
            catch (BusinessLogicException ex)
            {
                SqlHelper.RollBackTransaction(sqltrans, "UpdateUser_DT");
                SqlHelper.LogException(ex);
                throw ex;
            }
            catch (Exception ex)
            {
                SqlHelper.RollBackTransaction(sqltrans, "UpdateUser_DT");
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        #endregion

    }
}
