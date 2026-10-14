using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Common.Utils;
using DAL.DataAccessLayer;
using System.Text;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Setup
{
   public partial class HRMS_EmployeeDevelopmentPlan
    {

#region Constants

       const string Query_GetDatabyEmployeeID= @"
select HRMS_Set_Employee.FullName,HRMS_EmployeeDevelopmentPlan.*,'' as Action from HRMS_EmployeeDevelopmentPlan
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_EmployeeDevelopmentPlan.PersonID
and HRMS_Set_Employee.CompanyID = HRMS_EmployeeDevelopmentPlan.CompanyID
where HRMS_EmployeeDevelopmentPlan.CompanyID = @CompanyID
and HRMS_EmployeeDevelopmentPlan.PeriodID = @PeriodID
and HRMS_EmployeeDevelopmentPlan.Status = 1
{0}

";

       const string Query_DeleteDataByDevelopmentID = @" delete from HRMS_EmployeeDevelopmentPlan where PersonID = @PersonID and CompanyID =@CompanyID and PeriodID =@PeriodID";

       const string Query_GetDataByDevelopmentPlanID = @" 
SELECT HRMS_Set_Employee.FullName,HRMS_EmployeeDevelopmentPlan.*,'' as Action FROM [HRMS_EmployeeDevelopmentPlan]
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_EmployeeDevelopmentPlan.PersonID
and HRMS_Set_Employee.CompanyID = HRMS_EmployeeDevelopmentPlan.CompanyID
 WHERE HRMS_EmployeeDevelopmentPlan.[CompanyID] = @CompanyID and HRMS_EmployeeDevelopmentPlan.[DevelopmentPlanID] = @DevelopmentPlanID
  and HRMS_EmployeeDevelopmentPlan.[PeriodID] = @PeriodID ";
#endregion

        #region Methods
       public static DataTable GetDataByEmployeeID(int CompanyID, int PeriodID, int PersonID = 0, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       {
           DataTable dr = null;
           StringBuilder sb = new StringBuilder();
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
               sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
               sqlparams[2] = new SqlParameter("@PersonID", PersonID);

               DataSet ds = null;

               if (PersonID > 0)
                   sb.Append(" and HRMS_EmployeeDevelopmentPlan.PersonID = " + PersonID);
               if (objTransaction != null)
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_GetDatabyEmployeeID, sb.ToString()), sqlparams);
               else if (objConnection != null)
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetDatabyEmployeeID, sb.ToString()), sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDatabyEmployeeID, sb.ToString()), sqlparams);

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


       public static DataTable GetDataByDevelopmentPlanID(int companyID, int developmentPlanID, int periodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       {
           DataTable dr = null;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@DevelopmentPlanID", developmentPlanID);
               sqlparams[2] = new SqlParameter("@PeriodID", periodID);

               DataSet ds = null;

               if (objTransaction != null)
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetDataByDevelopmentPlanID, sqlparams);
               else if (objConnection != null)
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDataByDevelopmentPlanID, sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByDevelopmentPlanID, sqlparams);

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

       public static DataTable DeleteDataByDevelopmentID(int PersonID, int CompanyID, int PeriodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       {
           DataTable dr = null;
           StringBuilder sb = new StringBuilder();
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@PersonID", PersonID);
               sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
               sqlparams[2] = new SqlParameter("@PeriodID", PeriodID);

               DataSet ds = null;

               if (objTransaction != null)
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_DeleteDataByDevelopmentID, sqlparams);
               else if (objConnection != null)
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_DeleteDataByDevelopmentID, sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_DeleteDataByDevelopmentID, sqlparams);

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
