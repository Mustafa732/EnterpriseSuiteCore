using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;
using Common.Utils;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Setup
{
   public partial  class HRMS_OrganoGramDetail
    {

       const string Query_DeleteDataByOrganogramID = @"

delete from HRMS_organogramdetail where organogramid = @OrganogramID and CompanyID =@CompanyID 
";

       const string Query_InsertWithAutoID = @" INSERT INTO [HRMS_OrganoGramDetail] ([CompanyID],[PeriodID], [DetailOrganogramID], [OrganogramID], [DesignationID], [Status], [CreateDate], [CreatedByValue], [CreatedBy])
		VALUES(@CompanyID,@PeriodID, (select isnull(Max(DetailOrganogramID),0)+1 from HRMS_organogramdetail), @OrganogramID, @DesignationID, @Status, @CreateDate, @CreatedByValue, @CreatedBy) ";

    
       public bool InsertWithAutoID(SqlTransaction objTransaction = null)
       {
           bool result = false;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[10];
               sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
               sqlparams[1] = new SqlParameter("@DetailOrganogramID", f_DetailOrganogramID);
               sqlparams[2] = new SqlParameter("@OrganogramID", f_OrganogramID);
               sqlparams[3] = new SqlParameter("@DesignationID", f_DesignationID);
               sqlparams[4] = new SqlParameter("@Status", f_Status);
               sqlparams[5] = new SqlParameter("@CreateDate", f_CreateDate);
               sqlparams[6] = new SqlParameter("@CreatedByValue", f_CreatedByValue);
               sqlparams[7] = new SqlParameter("@CreatedBy", f_CreatedBy);
               sqlparams[8] = new SqlParameter("@PeriodID", f_PeriodID);
               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_InsertWithAutoID, sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_InsertWithAutoID, sqlparams) > 0)
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

       public static bool DeleteDataByOrganoGramID(int OrganogramID, int CompanyID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       {
           bool result = false;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[2];
               sqlparams[0] = new SqlParameter("@OrganogramID", OrganogramID);
               sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
              

               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteDataByOrganogramID, sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DeleteDataByOrganogramID, sqlparams) > 0)
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
