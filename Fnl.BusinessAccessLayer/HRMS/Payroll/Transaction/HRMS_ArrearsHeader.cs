using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;
using Common.Utils;
using FnI.BusinessAccessLayer.Setup;
using FnI.BusinessAccessLayer.HRMS.Payroll.Setup;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Transaction
{
   public partial class HRMS_ArrearsHeader
    {
       const string Query_GetArrears = @"
select sum(HRMS_ArrearsDetail.Amount) as Amount
from HRMS_ArrearsHeader
inner join HRMS_ArrearsDetail on HRMS_ArrearsDetail.ArrearsHeaderID = HRMS_ArrearsHeader.ArrearsHeaderID
and HRMS_ArrearsDetail.CompanyID = HRMS_ArrearsHeader.CompanyID
and HRMS_ArrearsDetail.PeriodID = HRMS_ArrearsHeader.PeriodID
and HRMS_ArrearsDetail.Status = 1
where HRMS_ArrearsHeader.personid= @PersonID and HRMS_ArrearsHeader.CompanyID = @CompanyID 
and HRMS_ArrearsHeader.PeriodID = @PeriodID
--and paymentDate = @paymentDate
and DATENAME(month, paymentDate) = @Month 
and DATEPART(Year,paymentDate) = @Year 
and HRMS_ArrearsHeader.Status = 1
";

       const string Query_GetAllArrears = @"
select isnull(sum(HRMS_ArrearsDetail.Amount),0) as Amount
from HRMS_ArrearsHeader
inner join HRMS_ArrearsDetail on HRMS_ArrearsDetail.ArrearsHeaderID = HRMS_ArrearsHeader.ArrearsHeaderID
and HRMS_ArrearsDetail.CompanyID = HRMS_ArrearsHeader.CompanyID
and HRMS_ArrearsDetail.PeriodID = HRMS_ArrearsHeader.PeriodID
and HRMS_ArrearsDetail.Status = 1
where HRMS_ArrearsHeader.personid= @PersonID and HRMS_ArrearsHeader.CompanyID = @CompanyID 
and HRMS_ArrearsHeader.PeriodID = @PeriodID
--and paymentDate = @paymentDate
and DATEPART(month, paymentDate) >= @Month 
and DATEPART(Year,paymentDate) = @Year 
and HRMS_ArrearsHeader.Status = 1
";
       public static DataTable GetArrears(int companyID, int periodID, int personid, Nullable<DateTime> paymentdate)
       {
           DataTable dt = null;
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[5];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@PersonID", personid);
               //sqlparams[3] = new SqlParameter("@paymentDate", paymentdate);
               sqlparams[3] = new SqlParameter("@Month", String.Format("{0:MMMM}", paymentdate));
               sqlparams[4] = new SqlParameter("@Year", paymentdate.Value.Year.ToString());
         

               dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetArrears, sqlparams).Tables[0];


           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }

       public static DataTable GetAllArrears(int companyID, int periodID, int personid, Nullable<DateTime> paymentdate, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
       {
           DataTable dt = null;
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[5];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@PersonID", personid);
               //sqlparams[3] = new SqlParameter("@paymentDate", paymentdate);
               sqlparams[3] = new SqlParameter("@Month", String.Format("{0:MM}", paymentdate));
               sqlparams[4] = new SqlParameter("@Year", paymentdate.Value.Year.ToString());


               if (objTransaction != null)
                   dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetAllArrears,sqlparams).Tables[0];
               else if (objConnection != null)
                   dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetAllArrears,sqlparams).Tables[0];
               else
                   dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllArrears,sqlparams).Tables[0];

           


           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }
    }
}
