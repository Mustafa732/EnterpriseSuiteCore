using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Setup
{
    public partial class SET_JobDesignation
    {
        #region Constant Strings

        const string Query_GetOnlyJobDesignation = @" SELECT [SET_JobDesignation].[JobDesignationID], [SET_JobDesignation].[JobDesignationName], [SET_JobDesignation].[Status] AS JobDesignationStatus,[SET_Designation].DesignationName,[SET_JobDesignation].IsOverTime FROM [SET_JobDesignation]
							LEFT JOIN SET_Designation ON SET_Designation.DesignationID = SET_JobDesignation.FK_DesignationID
							AND SET_Designation.Status = 1	 
                          WHERE [SET_JobDesignation].Status = 1 AND ([SET_JobDesignation].IsActive = @IsActive) 
                         {0}  
                        ORDER BY [SET_JobDesignation].[JobDesignationName]";

        const string Query_GetData_byCompanyAndDesginationID = @" SELECT * FROM [SET_JobDesignation] WHERE  [JobDesignationID] = @JobDesignationID "; //[CompanyID] = @CompanyID and
        const string Query_SelectAllbySorted = @" SELECT * FROM [SET_JobDesignation] WHERE [Status] = 1 AND ([IsActive] = @IsActive OR @IsActive is null)";
        const string Query = @" SELECT * FROM [SET_JobDesignation] WHERE [Status] = 1 AND ([IsActive] = @IsActive OR @IsActive is null)";
        #endregion

        #region Class Methods

        public static DataTable GetDataByCompanyIDAndDesignationID(int companyID, int JobDesignationID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@JobDesignationID", JobDesignationID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byCompanyAndDesginationID, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byCompanyAndDesginationID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byCompanyAndDesginationID, sqlparams);

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

        public static DataTable GetOnlyJobDesignation(int DesignationID, int? isActive = null)
        {
            DataTable dt = null;
            StringBuilder sb =new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];             

                sqlparams[0] = new SqlParameter("@IsActive", isActive);
                if (DesignationID > 0)
                {
                sb.Append(" AND  [SET_JobDesignation].[JobDesignationID] = " + DesignationID);
                }
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text,string.Format(Query_GetOnlyJobDesignation,sb.ToString()), sqlparams);

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

        public static DataTable GetOnlyJobDesignationForOTbyPersonID(int CompanyID, int DesignationID, int PersonID, int? isActive = null)
        {
            #region Query
            const string Query_GetIsOTActive = @"
SELECT HRMS_Set_Employee.PersonID,SET_JobDesignation.IsActive,SET_JobDesignation.IsOverTime, * FROM [SET_JobDesignation] 
	
		inner join HRMS_Set_Employee on HRMS_Set_Employee.CompanyID = SET_JobDesignation.CompanyID
	
WHERE       SET_JobDesignation.CompanyID = @CompanyID 
        and SET_JobDesignation.JobDesignationID = @DesignationID 
		and HRMS_Set_Employee.PersonID = @PersonID
        and SET_JobDesignation.IsActive = @IsActive
        and SET_JobDesignation.IsOverTime = @IsOverTime
";
            #endregion

            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];

                sqlparams[0] = new SqlParameter("@IsActive", true);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[2] = new SqlParameter("@DesignationID", DesignationID);
                sqlparams[3] = new SqlParameter("@PersonID", PersonID);
                sqlparams[4] = new SqlParameter("@IsOverTime", true);

               //error
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetIsOTActive, sb.ToString()), sqlparams);

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

        public static DataTable GetAllActiveSortedData(Nullable<int> companyID, Nullable<bool> isActive = true, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                //sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (isActive.HasValue)
                    sqlparams[0] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[0] = new SqlParameter("@IsActive", DBNull.Value);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAllbySorted, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAllbySorted, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllbySorted, sqlparams);

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
        public static DataRow GetCurrentDesignation(Nullable<int> JobDesignationID, Nullable<bool> isActive = true, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
              SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@JobDesignationID", JobDesignationID);

                if (isActive.HasValue)
                    sqlparams[1] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[1] = new SqlParameter("@IsActive", DBNull.Value);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAllbySorted, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAllbySorted, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllbySorted, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dr = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }
        public static DataRow GetAllActiveSortedDataDesignation(int JobDesignationID,Nullable<bool> isActive = true, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;
            StringBuilder sb1 = new StringBuilder();
             const string Query = @"SELECT * FROM [SET_JobDesignation] WHERE [JobDesignationID] = @JobDesignationID";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@JobDesignationID", JobDesignationID);
                if (isActive.HasValue)
                    sqlparams[1] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[1] = new SqlParameter("@IsActive", DBNull.Value);
                DataSet ds = null;
                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);
                if (ds != null && ds.Tables.Count > 0)
                    dr = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }


    }
}
