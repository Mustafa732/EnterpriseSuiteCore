using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using System.Text;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Setup
{
   public partial class HRMS_Division
    {
        #region Queries

       const string Query_GetOnlyDivision = @" SELECT [HRMS_Division].[DivisionID], [HRMS_Division].[DivisionName], [HRMS_Division].[Status] AS DivisionStatus FROM [HRMS_Division] 
                          WHERE [HRMS_Division].Status = 1 AND [HRMS_Division].[DivisionName] Like '%' + @DivisionName +'%'
                        ORDER BY [HRMS_Division].[DivisionName]";
       const string Query_GetData = @"SELECT [DivisionName],[DivisionCode] from [HRMS_Division] WHERE Status = 1 ";

       const string Query_GetData_byDivisionCode = @" SELECT DivisionCode FROM [HRMS_Division] WHERE DivisionCode = @DivisionCode And Status = 1 ";

       const string Query_GetAllDivisions = @" Select [DivisionCode] as [Code], [DivisionName] as [Name], [CreateDate] from HRMS_Division
        Where [Status] = 1";

        #endregion

        #region Class Methods

        public static DataRow GetDataByDivisionCode(int Divisioncode, string where = null)
        {
            DataRow dr = null;

            try
            {
                string Query = Query_GetData_byDivisionCode;
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@DivisionCode", Divisioncode);
                if (where != null)
                    Query += where;
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);
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

        public static DataTable GetAllActiveDataForLookUp()
        {
            DataTable dt = null;

            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllDivisions).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetOnlyDivision(string DivisionName, int? isActive = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@DivisionName", DivisionName);

                sqlparams[1] = new SqlParameter("@IsActive", isActive);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetOnlyDivision, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        public static DataTable GetReportData(string DivisionName, int DivisionCode)
        {
            DataTable dt = null;
            string filter = "";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@DivisionName", DivisionName);

                sqlparams[1] = new SqlParameter("@DivisionCode", DivisionCode);

                if (DivisionName != "")
                {
                    filter = " AND [DivisionName] Like '%" + DivisionName+ "%'";
                }

                if (DivisionCode != 0)
                {
                    filter = " AND [DivisionCode]  = " +DivisionCode;
                
                }
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData+filter, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetLogReportData(int CompanyID,DateTime DateFrom,DateTime DateTo,Nullable<int>PersonID)
        {
            DataTable dt = null;

            #region Query 
            const string Query_Report = @"  ";
            #endregion

            string filter = "";

            if (PersonID.HasValue)
                filter = " and HRMS_EmployeeUpdate_Log.PersonID =  " + PersonID.Value;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);

                sqlparams[1] = new SqlParameter("@FromDate", DateFrom);
                sqlparams[2] = new SqlParameter("@ToDate", DateTo);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_GetLogReportData + filter, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
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
