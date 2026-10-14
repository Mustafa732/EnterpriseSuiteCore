using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using System.Text;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Setup
{
   public partial class HRMS_AppraisalOfficer
   {
       #region Constant String

       const string Query_Get_MaxIds = @" SELECT IsNull(max(AppraisalLevel), 0) + 100 as AppraisalOfficerID FROM HRMS_AppraisalOfficer";

       const string Query_GetDatabyUserID = @" 
select 
--aspnet_Users.UserName,
DM.JobDesignationName as UserName,SET_JobDesignation.JobDesignationName as DesignationName,
HRMS_AppraisalOfficer.*,'' as Action from HRMS_AppraisalOfficer
left join SET_JobDesignation on SET_JobDesignation.JobDesignationID = HRMS_AppraisalOfficer.DesignationID 
left join SET_JobDesignation DM on DM.JobDesignationID = HRMS_AppraisalOfficer.UserID 
where HRMS_AppraisalOfficer.Status = 1 {0}";

       const string Query_GetDataByAppraisalID = @"
select 
--aspnet_Users.UserName,
DM.JobDesignationName as UserName,SET_JobDesignation.JobDesignationName as DesignationName,
HRMS_AppraisalOfficer.*,'' as Action from HRMS_AppraisalOfficer
left join SET_JobDesignation on SET_JobDesignation.JobDesignationID = HRMS_AppraisalOfficer.DesignationID and Set_Designation.CompanyID = HRMS_AppraisalOfficer.CompanyID
left join SET_JobDesignation DM on DM.JobDesignationID = HRMS_AppraisalOfficer.UserID and DM.CompanyID = HRMS_AppraisalOfficer.CompanyID
where AppraisalOfficerID =@AppraisalID and HRMS_AppraisalOfficer.CompanyID =@CompanyID and HRMS_AppraisalOfficer.PeriodID =@PeriodID 
and HRMS_AppraisalOfficer.Status = 1
";

       const string Query_DeleteDataByUserID = @"
delete from HRMS_AppraisalOfficer where AppraisalOfficerID = @AppraisalID and CompanyID =@CompanyID and PeriodID =@PeriodID;

";


       const string Query_DeleteDataByUsersID = @"
delete from HRMS_AppraisalOfficer where UserID = @UserID and CompanyID =@CompanyID and PeriodID =@PeriodID;

";

        #endregion

        #region Methods
        public static DataTable GetDataByUserID(string UserID = null,int DesignationID = 0 ,SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dr = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserID", UserID);

                DataSet ds = null;
                if (UserID != null && UserID != "")
                    if (int.Parse(UserID.ToString())>0)
                        sb.Append(" and HRMS_AppraisalOfficer.UserID = '" + UserID + "'");

                if (DesignationID > 0)
                    sb.Append(" and HRMS_AppraisalOfficer.DesignationID = " + DesignationID);

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_GetDatabyUserID,sb.ToString()), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetDatabyUserID, sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDatabyUserID, sb.ToString()), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dr = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }
       
       public static int GetMyNewAppraisalOfficerID(SqlTransaction objTransaction = null, SqlConnection objConnection = null)
       {
           int ID = 0;

           try
           {
               if (objTransaction != null)
                   ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_MaxIds));
               else if (objConnection != null)
                   ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_Get_MaxIds));
               else
                   ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_MaxIds));
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return ID;
       }

       public static DataTable GetDataByAppraisalID(int AppraisalID,int CompanyID,int PeriodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       {
           DataTable dr = null;
           StringBuilder sb = new StringBuilder();
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@AppraisalID", AppraisalID);
               sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
               sqlparams[2] = new SqlParameter("@PeriodID", PeriodID);

               DataSet ds = null;
                             
               if (objTransaction != null)
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetDataByAppraisalID, sqlparams);
               else if (objConnection != null)
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDataByAppraisalID, sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByAppraisalID, sqlparams);

               if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                   dr = ds.Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dr;
       }


       public static DataTable DeleteDataByAppraisalID(int  AppraisalID, int CompanyID, int PeriodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       {
           DataTable dr = null;
           StringBuilder sb = new StringBuilder();
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@AppraisalID", AppraisalID);
               sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
               sqlparams[2] = new SqlParameter("@PeriodID", PeriodID);

               DataSet ds = null;

               if (objTransaction != null)
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_DeleteDataByUserID, sqlparams);
               else if (objConnection != null)
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_DeleteDataByUserID, sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_DeleteDataByUserID, sqlparams);

               if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                   dr = ds.Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dr;
       }


       public static DataTable DeleteDataByUserID(string UserID, int CompanyID, int PeriodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       {
           DataTable dr = null;
           StringBuilder sb = new StringBuilder();
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@UserID", UserID);
               sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
               sqlparams[2] = new SqlParameter("@PeriodID", PeriodID);

               DataSet ds = null;

               if (objTransaction != null)
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_DeleteDataByUsersID, sqlparams);
               else if (objConnection != null)
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_DeleteDataByUsersID, sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_DeleteDataByUsersID, sqlparams);

               if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                   dr = ds.Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dr;
       }
        #endregion
   }
}
