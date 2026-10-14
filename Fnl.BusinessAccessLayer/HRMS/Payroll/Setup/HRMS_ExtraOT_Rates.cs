using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Setup
{
	
	public partial class HRMS_ExtraOT_Rates
    {
        #region constant strings
        const string query_paging = @" SELECT [RateID], [HRMS_ExtraOT_Rates].[CompanyID], [HRMS_ExtraOT_Rates].[DesignationID],SET_JobDesignation.JobDesignationName
, [Rate], [HRMS_ExtraOT_Rates].[Status],[PayrollTypeID], [PayrollCode], ROW_NUMBER() Over (Order by [HRMS_ExtraOT_Rates].CompanyID, RateID) as 'Row' 


FROM [HRMS_ExtraOT_Rates] 
inner join SET_JobDesignation on SET_JobDesignation.JobDesignationID = [HRMS_ExtraOT_Rates].[DesignationID]
and SET_JobDesignation .CompanyID = [HRMS_ExtraOT_Rates].CompanyID ";

        const string query_withoutpaging = @" SELECT [RateID], [HRMS_ExtraOT_Rates].[CompanyID], [HRMS_ExtraOT_Rates].[DesignationID],SET_JobDesignation.JobDesignationName
, [Rate], [HRMS_ExtraOT_Rates].[Status],[PayrollTypeID], [PayrollCode]


FROM [HRMS_ExtraOT_Rates] 
inner join SET_JobDesignation on SET_JobDesignation.JobDesignationID = [HRMS_ExtraOT_Rates].[DesignationID]
and SET_JobDesignation .CompanyID = [HRMS_ExtraOT_Rates].CompanyID ";


        const string query_getrateswithpayroll = @" Select * from HRMS_ExtraOT_Rates where Status = 1 
and CompanyID = @CompanyID and DesignationID = @DesignationID and PayrollTypeID = @payrolltypeid";




        #endregion

        #region Methods

        public static DataTable GetAllRatesData(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, SqlTransaction objTransaction = null)
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

                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, query_paging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), query_paging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, query_paging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), query_paging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, query_withoutpaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, query_withoutpaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetAllActiveDataWithpayroll(Nullable<int> companyID, int DesignationID,int payrolltypeid,SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@DesignationID", DesignationID);
                sqlparams[2] = new SqlParameter("@payrolltypeid", payrolltypeid);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, query_getrateswithpayroll, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, query_getrateswithpayroll, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query_getrateswithpayroll, sqlparams);

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
