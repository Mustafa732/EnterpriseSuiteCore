using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Common.Utils;
using DAL.DataAccessLayer;
using System.Text;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Setup
{
  public partial class HRMS_Policy
  {
      #region Constants

      const string query_GetTraineeConfirmationPeriod = @"

select Top 1 isnull(ConfirmationPeriod1,0) as ConfirmationPeriod1
 from HRMS_Policy where Status = 1
";

      const string query_GetOthersConfirmationPeriod = @"

select Top 1 isnull(ConfirmationPeriod2,0) as ConfirmationPeriod2,isnull(TraineeEngineerPeriod,0) as TraineeEngineerPeriod
 from HRMS_Policy where Status = 1
";
      const string query_GetSalaryProcessPoint = @"
select isnull(SalaryProcessPoint,0) as SalaryProcessPoint,isnull(SalaryProcessPointOnDate,0) as SalaryProcessPointOnDate
 from HRMS_Policy where Status = 1

";
      const string query_GetHolidays = @"
 select * from SET_DateTime where IsHoliday = 1 and 
 DATEPART(Year, SET_DateTime.theDate) = @Year
";

      const string query_GetSalaryProcessPointID = @"
Select top 1 SalaryProcessPointID from HRMS_Policy where Status = 1

";

      #endregion 

      #region Methods

      public static DataTable GetTraineeConfirmationPeriod(int companyid)
      {
          DataTable dt = null;

          try
          {
              SqlParameter[] sqlparams = new SqlParameter[1];

              sqlparams[0] = new SqlParameter("@CompanyID", companyid);

              dt = SqlHelper.ExecuteDataset(CommandType.Text, query_GetTraineeConfirmationPeriod, sqlparams).Tables[0];


          }
          catch (Exception ex)
          {
              SqlHelper.LogException(ex);
              throw ex;
          }

          return dt;
      }

      public static DataTable GetOthersConfirmationPeriod(int companyid)
      {
          DataTable dt = null;

          try
          {
              SqlParameter[] sqlparams = new SqlParameter[1];

              sqlparams[0] = new SqlParameter("@CompanyID", companyid);

              dt = SqlHelper.ExecuteDataset(CommandType.Text, query_GetOthersConfirmationPeriod, sqlparams).Tables[0];


          }
          catch (Exception ex)
          {
              SqlHelper.LogException(ex);
              throw ex;
          }

          return dt;
      }

      public static DataTable GetSalaryProcessPoint(int companyid)
      {
          DataTable dt = null;

          try
          {
              SqlParameter[] sqlparams = new SqlParameter[1];

              sqlparams[0] = new SqlParameter("@CompanyID", companyid);

              dt = SqlHelper.ExecuteDataset(CommandType.Text, query_GetSalaryProcessPoint, sqlparams).Tables[0];


          }
          catch (Exception ex)
          {
              SqlHelper.LogException(ex);
              throw ex;
          }

          return dt;
      }

      public static DataTable GetHolidays(int companyid,int Year)
      {
          DataTable dt = null;

          try
          {
              SqlParameter[] sqlparams = new SqlParameter[2];

              sqlparams[0] = new SqlParameter("@CompanyID", companyid);
              sqlparams[1] = new SqlParameter("@Year", Year);

              dt = SqlHelper.ExecuteDataset(CommandType.Text, query_GetHolidays, sqlparams).Tables[0];


          }
          catch (Exception ex)
          {
              SqlHelper.LogException(ex);
              throw ex;
          }

          return dt;
      }

      public static DataTable GetSalaryProcessPointID(int companyid)
      {
          DataTable dt = null;

          try
          {
              SqlParameter[] sqlparams = new SqlParameter[1];

              sqlparams[0] = new SqlParameter("@CompanyID", companyid);
              dt = SqlHelper.ExecuteDataset(CommandType.Text, query_GetSalaryProcessPointID, sqlparams).Tables[0];


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
