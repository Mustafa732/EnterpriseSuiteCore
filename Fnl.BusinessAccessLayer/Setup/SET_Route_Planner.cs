using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using System.Data;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_Route_Planner
    {
        public static DataTable Get_RoutePlannerByRouteID(string filter,SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                DataSet ds = null;

                string Query = @"
                    select * from SET_Route_Planner,SET_Route,SET_City,SET_Area
                    Where 
                    SET_Route_Planner.RouteID =SET_Route.RouteID
                    and SET_Route_Planner.CityID =SET_City.CityID
                    and SET_Route_Planner.AreaID =SET_Area.AreaID
                ";

                Query = Query + filter;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static bool DeleteByID(SqlTransaction objTransaction, int sET_Route_PlannerID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@SET_Route_PlannerID", sET_Route_PlannerID);

                string Query_Delete_ByKey = @"delete from SET_Route_Planner where SET_Route_PlannerID = @SET_Route_PlannerID";

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
    }
}
