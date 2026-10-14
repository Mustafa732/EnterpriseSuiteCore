using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;

namespace FnI.BusinessAccessLayer.AR.Setup
{
    public partial class AR_SET_Tax : BaseClass
    {
        const string Query_GetDataWithOtherFeilds = @" SELECT * FROM [AR_SET_Tax] WHERE [PeriodID] = @PeriodID and [TaxTypeID] = @TaxTypeID --and [CompanyID] = @CompanyID  ";

        public static DataRow GetDataByKeyWithOtherFeilds(int companyID, int periodID, int taxtypeID)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TaxTypeID", taxtypeID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataWithOtherFeilds, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dr = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }

        public static string GetTaxAccountCode(int companyID, int periodID, int taxTypeID)
        {
            //DataRow dr = null;
            string GLCode=string.Empty;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TaxTypeID", taxTypeID);

                string query = @" SELECT * FROM [AR_SET_Tax] WHERE [PeriodID] = @PeriodID and [TaxTypeID] = @TaxTypeID and Status = 1 --  and CompanyID = @CompanyID";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    GLCode = ds.Tables[0].Rows[0]["TaxCCID"].ToString();
                    //dr = ds.Tables[0].Rows[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return GLCode;
        }
    } 
}
