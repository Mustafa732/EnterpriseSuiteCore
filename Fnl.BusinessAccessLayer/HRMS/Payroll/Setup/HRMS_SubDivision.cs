using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using System.Text;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Setup
{
  public partial  class HRMS_SubDivision
    {
        #region Queries
      const string Query_GetData_byDivisionCode = @" SELECT SubDivisionCode FROM [HRMS_SubDivision] WHERE SubDivisionCode = @DivisionCode And Status = 1 ";

      const string Query_GetReportData = @"SELECT SubDivisionName,SubDivisionCode,DivisionName from HRMS_SubDivision 
                                        inner join HRMS_Division on HRMS_SubDivision.FK_DivisionID=HRMS_Division.DivisionID where HRMS_SubDivision.Status=1";
        #endregion

      const string Query_GetData_byDivisionID = @" SELECT * FROM [HRMS_SubDivision] WHERE [FK_DivisionID] = @DivisionID ";

        #region Methods

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

        public static DataTable GetDataByDivisionID(int DivisionID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@DivisionID", DivisionID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byDivisionID, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byDivisionID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byDivisionID, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dr = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }



        public static DataTable GetReportData(string SubDivisionName, int SubDivisionCode)
        {
            DataTable dt = null;
            string filter = "";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@SubDivisionName", SubDivisionName);

                sqlparams[1] = new SqlParameter("@SubDivisionCode", SubDivisionCode);

                if (SubDivisionName != "")
                {
                    filter = " AND [SubDivisionName] Like '%" + SubDivisionName + "%'";
                }

                if (SubDivisionCode != 0)
                {
                    filter = " AND [SubDivisionCode]  = " + SubDivisionCode;

                }
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetReportData + filter, sqlparams);

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
