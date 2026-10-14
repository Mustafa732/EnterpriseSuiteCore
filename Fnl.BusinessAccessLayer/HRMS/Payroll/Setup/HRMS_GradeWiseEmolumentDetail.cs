using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;
namespace FnI.BusinessAccessLayer.HRMS.Payroll.Setup
{
    public partial class HRMS_GradeWiseEmolumentDetail
    {

        #region Constant Strings

        const string Query_SelectAll_Search = @"SELECT HRMS_GradeWiseEmolumentDetail.PayrollTypeCode as EmolumentID, HRMS_GradeWiseEmolumentDetail.CompanyID,HRMS_GradeWiseEmolumentDetail.PeriodID,
 HRMS_GradeWiseEmolumentDetail.GradeWiseEmolumentHeaderID,HRMS_GradeWiseEmolumentDetail.GradeWiseEmolumentDetailID,HRMS_GradeWiseEmolumentDetail.PayrollTypeCode,
 HRMS_GradeWiseEmolumentDetail.PayrollTypeId As AllowanceTypeID,HRMS_GradeWiseEmolumentDetail.Amount,HRMS_GradeWiseEmolumentDetail.Formula AS Formulas,
 HRMS_GradeWiseEmolumentDetail.EffectiveFrom,HRMS_GradeWiseEmolumentDetail.Status,HRMS_GradeWiseEmolumentDetail.IsActive,
 HRMS_GradeWiseEmolumentDetail.CreatedBy,HRMS_GradeWiseEmolumentDetail.CreatedByValue,HRMS_GradeWiseEmolumentDetail.CreateDate,
 HRMS_GradeWiseEmolumentDetail.UpdatedBy,HRMS_GradeWiseEmolumentDetail.UpdatedByValue,HRMS_GradeWiseEmolumentDetail.UpdateDate,
(CASE WHEN  HRMS_GradeWiseEmolumentDetail.PayrollTypeCode ='1' THEN 'Allowances'  WHEN HRMS_GradeWiseEmolumentDetail.PayrollTypeCode='2' THEN 'Deductions' WHEN
 HRMS_GradeWiseEmolumentDetail.PayrollTypeCode='3' THEN 'Contributions' END) as Emoluments,
 HRMS_PayrollType.Code AS AllowanceType,'' Action From HRMS_GradeWiseEmolumentDetail Inner Join HRMS_PayrollType 
 on HRMS_GradeWiseEmolumentDetail.CompanyID= HRMS_PayrollType.CompanyID And HRMS_GradeWiseEmolumentDetail.PayrollTypeID= HRMS_PayrollType.PayrollTypeID 
  WHERE
   ( HRMS_GradeWiseEmolumentDetail.[Status] = 1) AND ( HRMS_GradeWiseEmolumentDetail.[IsActive] = @IsActive OR @IsActive is null)
  AND ( HRMS_GradeWiseEmolumentDetail.[CompanyID] = @CompanyID OR @CompanyID is null)
  AND ( HRMS_GradeWiseEmolumentDetail.[GradeWiseEmolumentHeaderID] = @GradeWiseEmolumentHeaderID OR @GradeWiseEmolumentHeaderID is null) 
  AND ( HRMS_GradeWiseEmolumentDetail.[PeriodID] = @PeriodID OR @PeriodID is null)
--AND (HRMS_PayrollType.[PayrollTypeCode]= @PayrollTypeCode)
";


        const string Query_SelectAll_SearchOnSelectedChanged = @"

SELECT HRMS_PayrollType.PayrollTypeCode, HRMS_GradeWiseEmolumentDetail.PayrollTypeCode as  EmolumentID,HRMS_GradeWiseEmolumentDetail.CompanyID,HRMS_GradeWiseEmolumentDetail.PeriodID,
 HRMS_GradeWiseEmolumentDetail.GradeWiseEmolumentHeaderID,HRMS_GradeWiseEmolumentDetail.GradeWiseEmolumentDetailID,
 HRMS_GradeWiseEmolumentDetail.PayrollTypeId As AllowanceTypeID,HRMS_GradeWiseEmolumentDetail.Amount,
 HRMS_GradeWiseEmolumentDetail.Formula AS Formulas,
 HRMS_GradeWiseEmolumentDetail.EffectiveFrom,
(CASE WHEN  HRMS_GradeWiseEmolumentDetail.PayrollTypeCode = 1 THEN 'Allowances'  WHEN HRMS_GradeWiseEmolumentDetail.PayrollTypeCode = '2'
 THEN 'Deductions' WHEN HRMS_GradeWiseEmolumentDetail.PayrollTypeCode = 3 THEN 'Contributions' WHEN HRMS_GradeWiseEmolumentDetail.PayrollTypeCode = 4 THEN 'Leaves' END) as Emoluments,
 
 HRMS_PayrollType.Description AS AllowanceType,'' Action,HRMS_GradeWiseEmolumentHeader.GradeID
 
  From HRMS_GradeWiseEmolumentDetail 
  
  inner join HRMS_GradeWiseEmolumentHeader on HRMS_GradeWiseEmolumentHeader.GradeWiseEmolumentHeaderID =
  HRMS_GradeWiseEmolumentDetail.[GradeWiseEmolumentHeaderID]
  and  HRMS_GradeWiseEmolumentHeader.Status =1
 Inner Join HRMS_PayrollType 
 on  HRMS_GradeWiseEmolumentDetail.PayrollTypeID= HRMS_PayrollType.PayrollTypeID 
  WHERE
( HRMS_GradeWiseEmolumentDetail.[Status] = 1) AND ( HRMS_GradeWiseEmolumentDetail.[IsActive] = @IsActive OR @IsActive is null)
    AND ( HRMS_GradeWiseEmolumentHeader.GradeID = @GradeID ) 
";

        const string Query_SelectByHeaderId = @"SELECT HRMS_GradeWiseEmolumentHeader.GradeID,HRMS_GradeWiseEmolumentDetail.PayrollTypeCode, HRMS_GradeWiseEmolumentDetail.CompanyID,HRMS_GradeWiseEmolumentDetail.PeriodID,
 HRMS_GradeWiseEmolumentDetail.GradeWiseEmolumentHeaderID,HRMS_GradeWiseEmolumentDetail.GradeWiseEmolumentDetailID,
 HRMS_GradeWiseEmolumentDetail.PayrollTypeId,HRMS_GradeWiseEmolumentDetail.Amount,HRMS_GradeWiseEmolumentDetail.Formula,
 HRMS_GradeWiseEmolumentDetail.EffectiveFrom,HRMS_GradeWiseEmolumentDetail.Status,HRMS_GradeWiseEmolumentDetail.IsActive,
 HRMS_GradeWiseEmolumentDetail.CreatedBy,HRMS_GradeWiseEmolumentDetail.CreatedByValue,HRMS_GradeWiseEmolumentDetail.CreateDate,
 HRMS_GradeWiseEmolumentDetail.UpdatedBy,HRMS_GradeWiseEmolumentDetail.UpdatedByValue,HRMS_GradeWiseEmolumentDetail.UpdateDate
 ,HRMS_PayrollType.Description,HRMS_GradeWiseEmolumentDetail.Status
 From HRMS_GradeWiseEmolumentDetail 
inner join HRMS_GradeWiseEmolumentHeader on HRMS_GradeWiseEmolumentHeader.GradeWiseEmolumentHeaderID =
  HRMS_GradeWiseEmolumentDetail.[GradeWiseEmolumentHeaderID] AND HRMS_GradeWiseEmolumentHeader.Status=1
  INNER JOIN HRMS_PayrollType ON HRMS_PayrollType.PayrollTypeID=HRMS_GradeWiseEmolumentDetail.PayrollTypeID
  WHERE HRMS_GradeWiseEmolumentDetail.[IsActive] =1 
    AND HRMS_GradeWiseEmolumentDetail.[Status] = 1  

 AND HRMS_GradeWiseEmolumentDetail.[GradeWiseEmolumentHeaderID] = @GradeWiseEmolumentHeaderID  ";


        const string Query_SelectByHeaderIdForpayRoll = @"SELECT HRMS_GradeWiseEmolumentHeader.GradeID,HRMS_GradeWiseEmolumentDetail.PayrollTypeCode, HRMS_GradeWiseEmolumentDetail.CompanyID,HRMS_GradeWiseEmolumentDetail.PeriodID,
 HRMS_GradeWiseEmolumentDetail.GradeWiseEmolumentHeaderID,HRMS_GradeWiseEmolumentDetail.GradeWiseEmolumentDetailID,
 HRMS_GradeWiseEmolumentDetail.PayrollTypeId,HRMS_GradeWiseEmolumentDetail.Amount,HRMS_GradeWiseEmolumentDetail.Formula,
 HRMS_GradeWiseEmolumentDetail.EffectiveFrom,HRMS_GradeWiseEmolumentDetail.Status,HRMS_GradeWiseEmolumentDetail.IsActive,
 HRMS_GradeWiseEmolumentDetail.CreatedBy,HRMS_GradeWiseEmolumentDetail.CreatedByValue,HRMS_GradeWiseEmolumentDetail.CreateDate,
 HRMS_GradeWiseEmolumentDetail.UpdatedBy,HRMS_GradeWiseEmolumentDetail.UpdatedByValue,HRMS_GradeWiseEmolumentDetail.UpdateDate
 ,HRMS_PayrollType.Description,HRMS_GradeWiseEmolumentDetail.Status
 From HRMS_GradeWiseEmolumentDetail 
inner join HRMS_GradeWiseEmolumentHeader on HRMS_GradeWiseEmolumentHeader.GradeWiseEmolumentHeaderID = HRMS_GradeWiseEmolumentDetail.[GradeWiseEmolumentHeaderID]
and HRMS_GradeWiseEmolumentHeader.CompanyID =HRMS_GradeWiseEmolumentDetail.CompanyID
AND HRMS_GradeWiseEmolumentHeader.Status=1
  INNER JOIN HRMS_PayrollType ON HRMS_PayrollType.PayrollTypeID=HRMS_GradeWiseEmolumentDetail.PayrollTypeID
  WHERE HRMS_GradeWiseEmolumentDetail.[IsActive] =1 
    AND HRMS_GradeWiseEmolumentDetail.[Status] = 1  
and HRMS_GradeWiseEmolumentHeader.CompanyID = @CompanyID
 AND HRMS_GradeWiseEmolumentDetail.[GradeWiseEmolumentHeaderID] = @GradeWiseEmolumentHeaderID  ";
        #endregion

        #region Class Methods

        public static DataTable GetAllActiveData_Search(Nullable<int> companyID, Nullable<int> gradeWiseEmolumentHeaderID, Nullable<int> periodID,Nullable<int> PayrollTypeCode, Nullable<bool> isActive = true, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@GradeWiseEmolumentHeaderID", gradeWiseEmolumentHeaderID);
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



        public static DataTable GetAllActiveData_OnselectedIndexChanged(Nullable<int> GradeID,  Nullable<int> PayrollTypeCode, Nullable<bool> isActive = true, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@GradeID", GradeID);

                if (isActive.HasValue)
                    sqlparams[1] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[1] = new SqlParameter("@IsActive", DBNull.Value);
                if (PayrollTypeCode.HasValue)
                    sqlparams[2] = new SqlParameter("@PayrollTypeCode", PayrollTypeCode);
                else
                    sqlparams[2] = new SqlParameter("@PayrollTypeCode", DBNull.Value);
                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_SearchOnSelectedChanged, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAll_SearchOnSelectedChanged, sqlparams);

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

        public static DataTable GetAllActiveData_OnRowEditing(Nullable<int> companyID, Nullable<int> GradeID, Nullable<int> periodID, Nullable<int> PayrollTypeCode, Nullable<bool> isActive = true, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@GradeID", GradeID);
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

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_SearchOnSelectedChanged, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAll_SearchOnSelectedChanged, sqlparams);

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

        public static DataTable Get_ActiveDataByHeaderID(int gradeWiseEmolumentHeaderID, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@GradeWiseEmolumentHeaderID", gradeWiseEmolumentHeaderID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectByHeaderId, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectByHeaderId, sqlparams);

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

        public static DataTable Get_ActiveDataByHeaderIDForPayRoll(int gradeWiseEmolumentHeaderID,int CompanyID, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@GradeWiseEmolumentHeaderID", gradeWiseEmolumentHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectByHeaderIdForpayRoll, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectByHeaderIdForpayRoll, sqlparams);

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
