using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;
namespace FnI.BusinessAccessLayer.HRMS.Payroll.Setup
{
    
    public partial class HRMS_GradeWiseLeaveDetail
    {

        #region Constant Strings

        const string Query_SelectAll_Search = @" 
 SELECT [CompanyID],[PeriodID],[GradeWiseLeaveDetailID],
  [GradeWiseLeaveHeaderID],
  [PayrollTypeID] AS LeaveTypeID, [PayrollTypeCode] AS LeaveType,
  [Days] As NOsOfDays,[MaxAccumulation],
  [Status], [IsActive], 
  [CreatedBy],[CreatedByValue],
   [CreateDate],[UpdatedBy],
   [UpdatedByValue],[UpdateDate],
   [ExpiryAfterType] As ExpiryAfter,[ExpiryAfterValue] As ExpiryAfterID, '' Action  From HRMS_GradeWiseLeaveDetail WHERE [Status] = 1 AND ([IsActive] = @IsActive OR @IsActive is null) AND ([CompanyID] = @CompanyID OR @CompanyID is null) AND ([GradeWiseLeaveHeaderID] = @GradeWiseLeaveHeaderID OR @GradeWiseLeaveHeaderID is null) AND ([PeriodID] = @PeriodID OR @PeriodID is null) AND ([PayrollTypeCode]= @PayrollTypeCode) ";

        #endregion


        #region Class Methods

        public static DataTable GetAllActiveData_Search(Nullable<int> companyID, Nullable<int> gradeWiseLeaveHeaderID, Nullable<int> periodID,Nullable<int> PayrollTypeCode, Nullable<bool> isActive = true, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@GradeWiseLeaveHeaderID", gradeWiseLeaveHeaderID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                if (isActive.HasValue)
                    sqlparams[3] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[3] = new SqlParameter("@IsActive", DBNull.Value);
                if (PayrollTypeCode.HasValue)
                    sqlparams[4] = new SqlParameter("@PayrollTypeCode", PayrollTypeCode);
                else
                    sqlparams[4] = new SqlParameter("@PayrollTypeCode", DBNull.Value);
                DataSet ds = null;
                //DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_Search, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAll_Search, sqlparams);

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

