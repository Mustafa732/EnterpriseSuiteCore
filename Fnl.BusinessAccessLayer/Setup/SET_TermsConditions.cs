using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using DAL.DataAccessLayer;
using Microsoft.Data.SqlClient;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_TermsConditions
    {
        #region Constant Queries

        const string Query_Terms_Conditions = @"
        select Description from SET_TermsConditions
        where CompanyID=@CompanyID and PeriodID=@PeriodID and ReportName='RFQ'
        Order by SortOrder";

        const string Query_GetAllData_ByReportName = @"SELECT * FROM SET_TermsConditions
        WHERE CompanyID = @CompanyID
        AND PeriodID = @PeriodID
        AND ReportName = @ReportName";

        #endregion

        #region Class Methods

        public static DataTable GetTermConditions(int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Terms_Conditions, sqlparams);

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

        public static DataTable GetSignatureValues(int companyID, int periodID, string reportName)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ReportName", reportName);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllData_ByReportName, sqlparams);

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

        #endregion
    }
}
