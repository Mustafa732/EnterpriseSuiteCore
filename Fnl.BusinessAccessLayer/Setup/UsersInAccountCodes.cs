using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
   public partial class UsersInAccountCodes
   {
       #region Constant String

       const string Query_GetByUserName = @"SELECT * FROM (
SELECT UsersInAccountCodes .CodeCombinationID,UsersInAccountCodes .AccountTypeID ,[GL_CodeCombinationDetail].Description,SET_AccountType .AccountTypeName
, CONVERT(BIT,'True')AS Checked
FROM UsersInAccountCodes 
INNER JOIN [GL_CodeCombinationDetail] ON [GL_CodeCombinationDetail].CodeCombinationID = UsersInAccountCodes .CodeCombinationID
inner join SET_AccountType on SET_AccountType.AccountTypeID = [GL_CodeCombinationDetail].AccountTypeID

WHERE UserID = @UserID 
and DetailPostingAllowedFlag = 1
and [GL_CodeCombinationDetail].AccountTypeID = @AccountTypeID

UNION all
SELECT CodeCombinationID,[GL_CodeCombinationDetail].AccountTypeID ,[GL_CodeCombinationDetail].Description,SET_AccountType .AccountTypeName
, CONVERT(BIT,'False')AS Checked
 FROM [GL_CodeCombinationDetail] 
inner join SET_AccountType on SET_AccountType.AccountTypeID = [GL_CodeCombinationDetail].AccountTypeID
WHERE [GL_CodeCombinationDetail] .[Status] = 1 AND [IsActive] = 1 
and DetailPostingAllowedFlag = 1
and [GL_CodeCombinationDetail].CodeCombinationID NOT IN (select CodeCombinationID from UsersInAccountCodes where UserID = @UserID) 
and [GL_CodeCombinationDetail].AccountTypeID = @AccountTypeID
)AS tab
ORDER BY tab.CodeCombinationID


 ";
       const string Query_Delete_ByKey = @"DELETE FROM UsersInAccountCodes Where UserID = @UserID ";

       #endregion

       #region Method

       public static DataTable GetDataByUser(Guid UserID, int AccountTypeID)
       {
           DataTable dt = null;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[2];
               sqlparams[0] = new SqlParameter("@UserID", UserID);
               sqlparams[1] = new SqlParameter("@AccountTypeID", AccountTypeID);

               dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetByUserName, sqlparams).Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }

       public static DataTable GetDataByUserID(string UserID, int AccountTypeID)
       {
           DataTable dt = null;
           StringBuilder sb = new StringBuilder();

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[2];
               sqlparams[0] = new SqlParameter("@UserID", UserID);
               sqlparams[1] = new SqlParameter("@AccountTypeID", AccountTypeID);

               #region query
               const string Query_GetByUserNameNew = @"SELECT * FROM (
SELECT UsersInAccountCodes .CodeCombinationID,UsersInAccountCodes .AccountTypeID ,[GL_CodeCombinationDetail].Description,SET_AccountType .AccountTypeName
, CONVERT(BIT,'True')AS Checked
FROM UsersInAccountCodes 
INNER JOIN [GL_CodeCombinationDetail] ON [GL_CodeCombinationDetail].CodeCombinationID = UsersInAccountCodes .CodeCombinationID
inner join SET_AccountType on SET_AccountType.AccountTypeID = [GL_CodeCombinationDetail].AccountTypeID

WHERE 1 = 1 
{0}
and DetailPostingAllowedFlag = 1
and [GL_CodeCombinationDetail].AccountTypeID = @AccountTypeID

UNION all
SELECT CodeCombinationID,[GL_CodeCombinationDetail].AccountTypeID ,[GL_CodeCombinationDetail].Description,SET_AccountType .AccountTypeName
, CONVERT(BIT,'False')AS Checked
 FROM [GL_CodeCombinationDetail] 
inner join SET_AccountType on SET_AccountType.AccountTypeID = [GL_CodeCombinationDetail].AccountTypeID
WHERE [GL_CodeCombinationDetail] .[Status] = 1 AND [IsActive] = 1 
and DetailPostingAllowedFlag = 1
and [GL_CodeCombinationDetail].CodeCombinationID NOT IN (select CodeCombinationID from UsersInAccountCodes WHERE 1 = 1 {0}) 
and [GL_CodeCombinationDetail].AccountTypeID = @AccountTypeID
)AS tab
ORDER BY tab.CodeCombinationID

 ";
               #endregion query

               if (!string.IsNullOrEmpty(UserID))
               {
                   sb.Append(" AND UserId IN (" + UserID + ")");
               }

               dt = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(Query_GetByUserNameNew, sb.ToString()), sqlparams).Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }

       public static bool DeleteDataByKey(SqlTransaction objTransaction, Guid UserID,int AccountTypeID)
       {
           bool result = false;
           string filter = "";
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[2];
               
               sqlparams[0] = new SqlParameter("@UserID", UserID);
               sqlparams[1] = new SqlParameter("@AccountTypeID", AccountTypeID);

               if (AccountTypeID > 0)
                   filter = " and AccountTypeID = @AccountTypeID ";

               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByKey + filter, sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByKey + filter, sqlparams) > 0)
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
