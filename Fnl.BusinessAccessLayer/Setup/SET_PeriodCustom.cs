using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;
using System.Collections;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_PeriodCustom
    {
        //to handle custom period / fiscal id

        #region Constant Queries

        #endregion

        #region Class Methods

        public static DataTable RemoveDuplicateRows(DataTable dTable, string colName)
        {
            Hashtable hTable = new Hashtable();
            ArrayList duplicateList = new ArrayList();

            //Add list of all the unique item value to hashtable, which stores combination of key, value pair.
            //And add duplicate item value in arraylist.
            foreach (DataRow drow in dTable.Rows)
            {
                if (hTable.Contains(drow[colName]))
                    duplicateList.Add(drow);
                else
                    hTable.Add(drow[colName], string.Empty);
            }

            //Removing a list of duplicate items from datatable.
            foreach (DataRow dRow in duplicateList)
                dTable.Rows.Remove(dRow);

            //Datatable which contains unique records will be return as output.
            return dTable;
        }

        public static DataRow SetFiscalyearDate(int periodID)
        {
            DataRow drToReturn = null;

            try
            {
                string Query = @"
                        select * from SET_PeriodCustom 
                                where PeriodID =  @PeriodID and  Status = 1 and IsActive = 1 
                        ";

                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    drToReturn = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return drToReturn;
        }

        public static DataRow Get_SET_PeriodCustom(int PeriodID)
        {
            DataRow drToReturn = null;

            //RETURNING PARENT PERIOD ID 1 
            try
            {
                string Query = @"
                        select * from SET_PeriodCustom 
						where PeriodID = @PeriodID
						and  Status = 1 
						and IsActive = 1 
                        ";

                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@PeriodID", PeriodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    drToReturn = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return drToReturn;
        }


        public static DataRow Get_FirstLastDateOfMonth(DateTime? month)
        {
            DataRow drToReturn = null;

            //RETURNING PARENT PERIOD ID 1 
            try
            {
                string Query = @"
Declare @sDate datetime,@eDate datetime

                      select  @sDate = @month,
                                @eDate = @month

                        ;with cte as (
                          select convert(date,left(convert(varchar,@sdate,112),6) + '01') startDate,
                                 month(@sdate) n
                          union all
                          select dateadd(month,n,convert(date,convert(varchar,year(@sdate)) + '0101')) startDate,
                                (n+1) n
                          from cte
                          where n < month(@sdate) + datediff(month,@sdate,@edate)
                        )
                        select @sDate=startdate, @eDate=dateadd(day,-1,dateadd(month,1,startdate)) 

                        from cte

select @sDate StartDate,@eDate EndDate
                        ";

                SqlParameter[] sqlparams = new SqlParameter[1];

                sqlparams[0] = new SqlParameter("@month", month.Value.Date);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    drToReturn = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return drToReturn;
        }

        public static DataRow GetFiscalByBatch(DateTime date,int CompanyID,int PeriodID)
        {
            DataRow drToReturn = null;

            //RETURNING PARENT PERIOD ID 1 
            try
            {
                string Query = @"
Select * from SET_PeriodCustom 
 where CompanyID = @CompanyID 
 and StartDate = @date
 and Status = 1
 and IsActive= 1
                        ";

                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@date", date);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[2] = new SqlParameter("@PeriodID", PeriodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    drToReturn = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return drToReturn;
        }


        public static DataTable GetFiscals(int FiscalId, int PeriodID, int companyID)
        {
            DataTable drToReturn = null;

            //RETURNING PARENT PERIOD ID 1 
            try
            {
                string Query = @"
                        select * from SET_PeriodCustom 
						where PeriodID = @PeriodID and CompanyID = @CompanyID and  Status = 1  and IsActive = 1 and FiscalId <= @FiscalId
                        ";

                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[1] = new SqlParameter("@FiscalId", FiscalId);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    drToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return drToReturn;
        }

        public static DataTable Get_DataForQuaterStartAndEndDate(int companyID, int periodID, int fiscalid, int quarterno, string query)
        {
            DataTable drToReturn = null;

            //RETURNING PARENT PERIOD ID 1 
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FiscalId", fiscalid);
                sqlparams[3] = new SqlParameter("@SelectedQuarter", quarterno);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    drToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return drToReturn;
        }

        public static DataTable Get_DataForYeartartAndEndDate(int companyID, int periodID, int fiscalid, string query)
        {
            DataTable drToReturn = null;

            //RETURNING PARENT PERIOD ID 1 
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FiscalId", fiscalid);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    drToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return drToReturn;
        }


        public static DataTable GetFiscalsForAssetForm(int FiscalId, int PeriodID, int companyID)
        {
            DataTable drToReturn = null;

            try
            {
                string Query = @"
                        select * from SET_PeriodCustom 
						where PeriodID = @PeriodID and CompanyID = @CompanyID and  Status = 1  and IsActive = 1 and FiscalId = @FiscalId
                        ";

                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[1] = new SqlParameter("@FiscalId", FiscalId);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    drToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return drToReturn;
        }

        public static DataTable GetDataByFiscalID(int FiscalId, int companyID)
        {
            DataTable drToReturn = null;

            try
            {
                string Query = @"
                        select * from SET_PeriodCustom 
						where CompanyID = @CompanyID and FiscalId = @FiscalId
                        ";

                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@FiscalId", FiscalId);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    drToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return drToReturn;
        }


        public static DataTable GetAllFiscalIDbyCompanyID(int companyID)
        {
            DataTable drToReturn = null;

            try
            {
                string Query = @"
                        select FiscalId, convert(varchar, year(StartDate)) +'-'+ convert(varchar, year(EndDate)) as Fiscal from set_PeriodCustom
						where CompanyID = @CompanyID
                        ";

                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    drToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return drToReturn;
        }

        public static DataRow SetFiscalByDate(DateTime date, SqlTransaction sqlTrans = null)
        {
            DataRow drToReturn = null;

            try
            {
                string Query = @"select * from SET_PeriodCustom WHERE Status = 1 and IsActive = 1 AND @date BETWEEN StartDate AND EndDate";

                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@date", date);

                DataSet ds = null;

                if (sqlTrans == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(sqlTrans, CommandType.Text, Query, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    drToReturn = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return drToReturn;
        }
        
        #endregion
    }
}
