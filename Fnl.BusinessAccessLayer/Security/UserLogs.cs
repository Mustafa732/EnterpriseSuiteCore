using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;
using System.Data;
using Common.Utils;

namespace FnI.BusinessAccessLayer.Security
{
    public partial class UserLogs
    {

        #region Const String

        const string Query_UpdateUserLogs = @"Update UserLogs
                                    Set 
                                    IsUserLoggedIn = 0,
                                    Reason = @Reason,
                                    AuthorizeBy = @UserName,
                                    AuthorizeOn = @DateTime1
                                    Where
                                    UserLogsID = @UserLogsID";

        const string Query_GetDataFromOtherTables = @"select distinct UserLogs.UserID,Aspnet_Users.UserName,*
                            ,ROW_NUMBER() OVER (ORDER BY UserLogs.CurrentDate Desc) AS 'Row','' As Action
                             from UserLogs 
                            Inner Join Aspnet_Users ON Aspnet_Users.UserID = UserLogs.UserID
                            Where IsUserLoggedIn = 1
                        ";

        const string Query_SelectLastStatusOfUser = @"select top 1 IsUserLoggedIn from userlogs 
                    Where UserID = @UserID
                    Order By CurrentDate Desc";

        const string Query_DeleteUserLogs = @" Delete From UserLogs
                   Where UserlogsId = 
                   (
                   Select top 1 UserlogsId from UserLogs
                   Where UserID = @UserID
                   Order By CurrentDate Desc
                   )
            ";

        #endregion

        #region Method

        public static DataTable GetUsersForUserLogs()
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataFromOtherTables);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataTable GetAllDataWithOtherTables(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

            try
            {
                if (PageIndex != null)
                {
                    sqlparamsList.Add(new SqlParameter("@PageIndex", PageIndex));
                    sqlparamsList.Add(new SqlParameter("@PageSize", PageSize));
                    sqlparamsList.Add(new SqlParameter("@TotalRows", ReturnTotalRows));

                    SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                    sqlparamTotalRows.Direction = ParameterDirection.Output;

                    SqlParameter[] sqlparams = sqlparamsList.ToArray();
                    string query = Query_GetDataFromOtherTables.Replace("{AdditionalParameters}", strWhere);
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, query
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), query
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataFromOtherTables + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""),
                        sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable Get_SelectLastStatusOfUser(Guid UserID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@UserID", UserID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectLastStatusOfUser, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static bool DeleteUserLogs(Guid UserID)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[10];


                sqlparams[0] = new SqlParameter("@UserID", UserID);

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteUserLogs, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static bool Get_UpdateUserLogs(string Reason, Guid UserName, DateTime DateTime1, int UserLogsID)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[10];


                sqlparams[0] = new SqlParameter("@Reason", Reason);
                sqlparams[1] = new SqlParameter("@UserName", UserName);
                sqlparams[2] = new SqlParameter("@DateTime1", DateTime1);
                sqlparams[3] = new SqlParameter("@UserLogsID", UserLogsID);

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateUserLogs, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }


        public static DataTable GetCurrencyByLocation(int LocationID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@LocationID", LocationID);

                const string Query_GetCurrencyByLocation = @"
select SET_Location.LocationID,SET_Currency.CurrencyID,SET_Currency.CurrencyName,SET_Location.LocationName
	
from SET_Location 
Left join SET_Currency on SET_Location.CurrencyID = SET_Currency.CurrencyID
and SET_Currency.sTATUS = 1
WHERE SET_Location.LocationID = @LocationID 
and SET_Location.STATUS = 1


";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetCurrencyByLocation, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        #endregion

    }
}
