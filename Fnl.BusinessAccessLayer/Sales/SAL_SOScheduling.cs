using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Data;
using Microsoft.Data.SqlClient;
using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Sales
{
    public partial class SAL_SOScheduling
    {

        #region Constant String

        const string Query_GetSOSchedulingByOrderDetailID = @" 
  select SAL_SOScheduling.CompanyID, 
	SAL_SOScheduling.PeriodID, 
	SAL_SOScheduling.OrderID,
	SAL_SOScheduling.OrderDetailID,
	SAL_SOScheduling.Quantity, 
	SAL_SOScheduling.DispatchDate, '' [Action]
	from SAL_SOScheduling
inner join SAL_OrderDetail on SAL_OrderDetail.OrderDetailID = SAL_SOScheduling.OrderDetailID 
	and SAL_OrderDetail.CompanyID = SAL_SOScheduling.CompanyID
	and SAL_OrderDetail.PeriodID = SAL_SOScheduling.PeriodID
	and SAL_OrderDetail.OrderID = SAL_SOScheduling.OrderID
where SAL_SOScheduling.OrderDetailID = @OrderDetailID
	and SAL_SOScheduling.OrderID = @OrderID
	and SAL_SOScheduling.CompanyID = @CompanyID
	and SAL_SOScheduling.PeriodID = @PeriodID
";

        const string Query_Delete_BykeySOHandSOD = @"Delete from SAL_SOScheduling where OrderID = @OrderID and OrderDetailID = @OrderDetailID and CompanyID = @CompanyID and PeriodID = @PeriodID";

        const string Query_Select_By_POHeaderID_CompanyId = @"select SAL_SOScheduling.CompanyID, SAL_SOScheduling.PeriodID, SAL_SOScheduling.OrderID, 
  SAL_SOScheduling.OrderDetailID, SAL_SOScheduling.Quantity, SAL_SOScheduling.DispatchDate, '' [Action]
  from SAL_SOScheduling inner join SAL_OrderDetail on SAL_SOScheduling.CompanyID = SAL_OrderDetail.CompanyID
  and SAL_SOScheduling.PeriodID = SAL_OrderDetail.PeriodID and SAL_SOScheduling.OrderID = SAL_OrderDetail.OrderID
  and SAL_SOScheduling.OrderDetailID = SAL_OrderDetail.OrderDetailID
  WHERE SAL_SOScheduling.OrderID = @OrderID and SAL_SOScheduling.CompanyID = @CompanyID and SAL_SOScheduling.PeriodID = @PeriodID";

        #endregion


        #region Class Methods

        public static DataTable GetSOSchedulingByOrderDetail(int companyID, int periodID, int OrderDetailID, int OrderID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@OrderDetailID", OrderDetailID);
                sqlparams[3] = new SqlParameter("@OrderID", OrderID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetSOSchedulingByOrderDetailID, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public bool DeleteSOSchedulingForUpdate(List<SAL_SOScheduling> colSOSDelete)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateSOS");

            try
            {
                foreach (SAL_SOScheduling posDel in colSOSDelete)
                {
                    DeleteDataByKey(posDel.CompanyID, posDel.PeriodID, posDel.OrderID, posDel.OrderDetailID, sqltrans);
                }



                result = true;
            }
            catch (BusinessLogicException ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            finally
            {
                if (result)
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateSOS");
            }

            return result;
        }

        public bool DeleteDataByKey(int CompanyID, int PeriodID, int OrderID, int OrderDetailID, SqlTransaction objTransaction = null)
        {
            bool result = false;
            //SqlTransaction sqltrans = SqlHelper.GetTransaction("DeletePOScheduling");
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@OrderID", OrderID);
                sqlparams[3] = new SqlParameter("@OrderDetailID", OrderDetailID);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, string.Format(Query_Delete_BykeySOHandSOD), sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, string.Format(Query_Delete_BykeySOHandSOD), sqlparams) > 0)
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

        public bool InsertSOSchedulingForUpdate(List<SAL_SOScheduling> colSOSAdd, int OrderID)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateSOS");

            try
            {

                foreach (SAL_SOScheduling sosAdd in colSOSAdd)
                {
                    sosAdd.OrderID = OrderID;
                    sosAdd.Insert(sqltrans);
                }



                result = true;
            }
            catch (BusinessLogicException ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            finally
            {
                if (result)
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateSOS");
            }

            return result;
        }

        public static DataTable GetDataByKeyTable(int OrderID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@OrderID", OrderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);


                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_POHeaderID_CompanyId, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_Select_By_POHeaderID_CompanyId, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }



        #endregion

    }
}
