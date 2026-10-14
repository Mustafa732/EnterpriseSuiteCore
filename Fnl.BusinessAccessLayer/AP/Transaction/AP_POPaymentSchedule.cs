using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;

namespace FnI.BusinessAccessLayer.AP.Transaction
{
    
    public partial class AP_POPaymentSchedule
    {

        public static DataTable GetDataByKeyTable(int pOHeaderID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            string Query_Select_By_POHeaderID_CompanyId = @"
select AP_POPaymentSchedule.CompanyID, AP_POPaymentSchedule.PeriodID, AP_POPaymentSchedule.POHeaderID, AP_POPaymentSchedule.PODetailID,
Percentage,Amount,Date,Remarks,isnull(IsAdvance,0) as IsAdvance, '' [Action] 
from AP_POPaymentSchedule 
WHERE AP_POPaymentSchedule.POHeaderID = @POHeaderID and AP_POPaymentSchedule.CompanyID = @CompanyID and AP_POPaymentSchedule.PeriodID = @PeriodID  and AP_POPaymentSchedule.status = 1";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@POHeaderID", pOHeaderID);
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
    }
}
