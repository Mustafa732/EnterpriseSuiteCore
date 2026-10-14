using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using Common.Utils;
using DAL.DataAccessLayer;
using System.Data;

namespace FnI.BusinessAccessLayer.Security
{
    public partial class UsersInCategories
    {

        #region Const Strings

        const string Query_Delete_ByKey = @"Delete From UsersInCategories where UserId = @UserId";

        const string Query_SelectAllData = @"SELECT [CompanyID],[CategoryID],[CategoryName],'False' as IsAssigned FROM [INV_SET_Category] Where Status = 1";

        const string Query_SelectCategoriesByUserID = @"select 
        UsersInCategories.UserID
        ,convert(bit,'True') IsAssigned                
        ,UsersInCategories.CompanyID
        ,UsersInCategories.CategoryID
        ,INV_SET_Category.CategoryName
        from UsersInCategories
        inner join INV_SET_Category ON INV_SET_Category.CategoryID = UsersInCategories.CategoryID
        where UsersInCategories.UserId = @UserId";

        const string Query_SelectAllDataNOT = @"Select 
INV_SET_Category.CategoryID,
INV_SET_Category.CategoryName,
INV_SET_Category.CompanyID, 
'False' as IsAssigned 
from INV_SET_Category 
left Join UsersInCategories ON UsersInCategories.CategoryID = INV_SET_Category.CategoryID
and UsersInCategories.userid = @UserID
Where INV_SET_Category.Status = 1
and (UsersInCategories.userid != @UserID
or UsersInCategories.userid is null)";

        #endregion

        #region Methods

        public static DataTable SelectAllData()
        {
            DataTable dtToReturn = null;

            try
            {

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllData);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }

            return dtToReturn;
        }

        public static bool DeleteDataByKey(Guid UserId, SqlTransaction trans)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserId", UserId);
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

        public bool InsertUserInCategories(List<UsersInCategories> colcategory)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertUser_DT");

            try
            {
                //uo.Insert(sqltrans);

                foreach (UsersInCategories uc in colcategory)
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

        public bool UpdateUser_CategoriesInUser(UserOptions uo, List<UsersInCategories> colcategory)
        {

            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateUser_DT");

            try
            {

                UsersInCategories.DeleteDataByKey(uo.UserId, sqltrans);

                uo.UpdateDataByKey(sqltrans);

                foreach (UsersInCategories uc in colcategory)
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



        public static object SelectCategoriesByUserID(Guid? username)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@UserId", username);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectCategoriesByUserID, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }

            return dtToReturn;
        }

        public static DataTable SelectAllDataNOT(Guid userid, string dt)
        {
            DataTable dtToReturn = null;

            try
            {
                string strWhereRoleids = !string.IsNullOrEmpty(dt) ? " And INV_SET_Category.CategoryID NOT IN (" + dt + ")" : string.Empty;
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@UserID", userid);
                sqlparams[1] = new SqlParameter("@dt", dt);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllDataNOT + strWhereRoleids, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }

            return dtToReturn;
        }
    }
}
