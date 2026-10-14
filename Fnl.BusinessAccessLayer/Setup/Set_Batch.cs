using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class Set_Batch
    {
        #region Queries

        const string Query_IsBatchAvailable = @"
select Set_Batch.BatchID from Set_Batch
        where Set_Batch.PeriodID = @PeriodId 
        and (@FromDate between Set_Batch.FromDate and Set_Batch.ToDate)
        and Status = 1
        
        select Set_Batch.BatchID from Set_Batch 
        where Set_Batch.PeriodID = @PeriodId 
        and (@ToDate between Set_Batch.FromDate and Set_Batch.ToDate)
        and Status = 1";

        const string Query_GetDateForDDLBatch = @"
           SELECT 
             Case When  [Description] Is Not Null 
             Then [Description]
             Else CAST((datepart(year,FromDate))AS VARCHAR(10))+'-'+CAST((datepart(year,ToDate))AS VARCHAR(10))
             End As Batch
            ,BatchID From Set_Batch
                WHERE Set_Batch.Status = 1
           ";

        const string Query_GetDateForDDLBatchHabib = @"
           SELECT 
             Case When  [Description] Is Not Null 
             Then [Description]
             Else CAST((datepart(year,FromDate))AS VARCHAR(10))+'-'+CAST((datepart(year,ToDate))AS VARCHAR(10))
             End As Batch
            ,BatchID From Set_Batch
                WHERE Set_Batch.Status = 1
order by BatchID desc
           ";

        const string Query_GetAllActiveYear = @"
select  BatchID, (CONVERT(CHAR(4),FromDate,120) + ' - ' + Convert(Char(4),ToDate,120))as [Year] from SET_Batch
 Where PeriodID = @PeriodID 
";


        #endregion




        #region Method
        public static DataTable GetAllActiveYear(Nullable<int> CompanyID, Nullable<int> PeriodID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllActiveYear, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static bool IsBatchAvailable(int companyId, int periodId, string fromdate, string todate, ref string message)
        {
            bool result = false;

            try
            {
                DataSet ds = null;

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyId", companyId);
                sqlparams[1] = new SqlParameter("@PeriodId", periodId);
                sqlparams[2] = new SqlParameter("@FromDate", fromdate);
                sqlparams[3] = new SqlParameter("@ToDate", todate);

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_IsBatchAvailable, sqlparams);

                if (ds.Tables.Count > 0)
                    if (ds.Tables[0].Rows.Count > 0)
                        message = " Batch is already exit  " + ds.Tables[0].Rows[0][0].ToString() + ".";
                    else if (ds.Tables[1].Rows.Count > 0)
                        message = " Batch is already exit  " + ds.Tables[1].Rows[0][0].ToString() + ".";
                    else
                        result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataTable GetAllActiveDataForDDL(SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[0];
                //sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                //sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDateForDDLBatch, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDateForDDLBatch, sqlparams);

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

        public static DataTable GetAllActiveDataForDDLHabib(SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[0];
                //sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                //sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDateForDDLBatchHabib, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDateForDDLBatchHabib, sqlparams);

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

        public static DataTable GetAllActiveDataForDDL(Nullable<int> companyID, Nullable<int> periodID, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDateForDDLBatch, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDateForDDLBatch, sqlparams);

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
        #endregion

    }
}
