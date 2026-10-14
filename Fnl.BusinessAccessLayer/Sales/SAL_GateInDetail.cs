using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DAL.DataAccessLayer;
using Microsoft.Data.SqlClient;
using Common.Utils;
using System.Data;

namespace FnI.BusinessAccessLayer.Sales
{
    public partial class SAL_GateInDetail : BaseClass
    {
        public static bool InsertGateInHeaderAndDetail(SAL_GateIn colGateIn, List<SAL_GateInDetail> colGateInDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertGateIn");

            try
            {
                colGateIn.GateInNo = SAL_GateIn.GetNewDocumentNo(colGateIn.CompanyID, colGateIn.PeriodID, colGateIn.CurrentFiscalID.Value, colGateIn.DocumentTypeID, sqltrans, null);
                colGateIn.GateInID = SAL_GateIn.GetNewGateInID(colGateIn.CompanyID, colGateIn.PeriodID, sqltrans, null);
                colGateIn.Insert(sqltrans);

                foreach (SAL_GateInDetail col in colGateInDetail)
                {
                    col.GateInID = colGateIn.GateInID;
                    col.GateInDetailID = SAL_GateInDetail.GetNewGateInDetailID(colGateIn.CompanyID, colGateIn.GateInID, colGateIn.PeriodID, sqltrans, null);
                    col.Insert(sqltrans);
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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertPayment");
            }

            return result;
        }

        public static DataTable GetDetailDataForGateInForm(int companyID, int periodID, int gateInID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@gateInID", gateInID);

                DataSet ds = null;

                string Query = FnI.QueryResourceLibrary.Sales.SAL_DeliveryOrderHeader.Query_GetDetailDataForGateInForm;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);

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
    }
}
