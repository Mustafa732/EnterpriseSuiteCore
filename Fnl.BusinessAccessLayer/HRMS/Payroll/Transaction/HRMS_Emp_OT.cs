using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;
using System.Text;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Transaction
{
   public partial class HRMS_Emp_OT
    {

       public bool UpdateIsApproved_By_Key(List<HRMS_Emp_OT> Emp_OT, SqlTransaction objTransaction = null)
       {
           bool result = false;

           foreach (HRMS_Emp_OT sod in Emp_OT)
           {
               sod.Update_Data_ForIsApproved(objTransaction);
           }

           result = true;

           return result;
       }

       public bool Update_Data_ForIsApproved(SqlTransaction objTransaction = null)
       {

           bool result = false;

           const string Query = @" UPDATE HRMS_Emp_OT SET IsApproved = @IsApproved,IsRejected = @IsRejected, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue
		WHERE [CompanyID] = @CompanyID and [OTID] = @OTID and [PeriodID] = @PeriodID ";

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[10];
               sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
               sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
               sqlparams[2] = new SqlParameter("@OTID", f_OTID);
               sqlparams[3] = new SqlParameter("@IsApproved", f_IsApproved);
               sqlparams[7] = new SqlParameter("@IsRejected", f_IsRejected);

               if (f_UpdatedBy.HasValue)
                   sqlparams[4] = new SqlParameter("@UpdatedBy", f_UpdatedBy.Value);
               else
                   sqlparams[4] = new SqlParameter("@UpdatedBy", DBNull.Value);

               if (f_UpdateDate.HasValue)
                   sqlparams[5] = new SqlParameter("@UpdateDate", f_UpdateDate.Value);
               else
                   sqlparams[5] = new SqlParameter("@UpdateDate", DBNull.Value);

               if (!string.IsNullOrEmpty(f_UpdatedByValue))
                   sqlparams[6] = new SqlParameter("@UpdatedByValue", f_UpdatedByValue);
               else
                   sqlparams[6] = new SqlParameter("@UpdatedByValue", DBNull.Value);

               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query, sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query, sqlparams) > 0)
                       result = true;
               }
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return result;
       }
    }
}
