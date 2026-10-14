using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using System.Data;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Setup
{
    public partial class HRMS_TransferPosting
    {
        #region Constant Srings

        const string Query_GetDatabyDocumentNo = @"
 SELECT [HRMS_TransferPosting].*,HRMS_Set_Employee.FullName,HRMS_Set_Employee.FatherName,HRMS_TransferPosting.TransferPostID,
HRMS_Set_Employee.BasicSalary,HRMS_Set_Employee.JoiningDate,HRMS_Set_Employee.PersonTypeCode,
HRMS_Set_Employee.DateOfBirth,HRMS_Set_Employee.Email,HRMS_Set_Employee.AddressLine1 FROM [HRMS_TransferPosting] 
 inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = [HRMS_TransferPosting].PersonID
 and HRMS_Set_Employee.CompanyID = [HRMS_TransferPosting].CompanyID and [HRMS_TransferPosting].status =1  {0}";


        const string Query_DataByPersonID = @"
select 
 HRMS_TransferPosting.PersonID as PersonID
,SET_Business.BusinessName +'-'+ SET_OperatingUnit.OperatingUnitName 
as CompanyFrom
,buss.BusinessName +'-'+ opt.OperatingUnitName 
as CompanyTo
,unitfrom.OperatingUnitName as UnitFrom
,unitto.OperatingUNitName as UnitTo
,lctfrom.LocationName as LocationFrom
,lctto.LocationName as LocationTo
,dptfrom.DepartmentName as DepartmentFrom
,dptto.DepartmentName as DepartmentTo
,desgfrom.DesignationName as DesignationFrom
,desgto.DesignationName as DesignationTo
,shiftfrom.Description as ShiftFrom 
,shiftto.Description as ShiftTo
,case HRMS_TransferPosting.PersonTypeCodeFrom when 1 then 'Permanent' when 2 then 'Contract'
when 3 then 'DailyWages' when 4 then 'SeasonalContract' when 5 then 'SeasonalDailyWages' end as PersonTypeCodeFrom
,case HRMS_TransferPosting.PersonTypeCodeTo when 1 then 'Permanent' when 2 then 'Contract'
when 3 then 'DailyWages' when 4 then 'SeasonalContract' when 5 then 'SeasonalDailyWages' end as
PersonTypeCodeTo
from
   
 HRMS_TransferPosting
 left join  SET_Company on SET_Company.CompanyID =  HRMS_TransferPosting.FromCompany
 and SET_Company.Status =1 
 and HRMS_TransferPosting.Status =1 
 left join SET_OperatingUnit
  on SET_OperatingUnit.OperatingUnitID  = SET_Company.OperatingUnitID
  and SET_OperatingUnit.Status =1 
  
  
  left join SET_Business on SET_Business.BusinessID  =  SET_Company.BusinessID 
  and SET_Business.Status =1 

left join  SET_Company SET_com on SET_com.CompanyID =  HRMS_TransferPosting.ToCompany
 and SET_com.Status =1 
 
 left join SET_OperatingUnit opt
  on opt.OperatingUnitID  = SET_com.OperatingUnitID
  and opt.Status =1 
  
  left join SET_Business buss on buss.BusinessID  =  SET_com.BusinessID 
  and buss.Status =1 

left join SET_OperatingUnit unitfrom
  on unitfrom.OperatingUnitID  = HRMS_TransferPosting.FromUnit
  and unitfrom.Status =1 
  
left join SET_OperatingUnit unitto
  on unitto.OperatingUnitID  = HRMS_TransferPosting.ToUnit
  and unitto.Status =1 
  
left join SET_Location lctfrom on lctfrom.LocationID =  HRMS_TransferPosting.FromLocation
and lctfrom.Status =1 


left join SET_Location lctto on lctto.LocationID =  HRMS_TransferPosting.ToLocation
and lctto.Status =1 

left join Set_department dptfrom on dptfrom.DepartmentID =  HRMS_TransferPosting.FromDepartment
and dptfrom.Status =1 


left join Set_department dptto on dptto.DepartmentID =  HRMS_TransferPosting.ToDepartment
and dptto.Status =1 

left join SET_Designation desgfrom on  desgfrom.DesignationID =  HRMS_TransferPosting.FromDesignation
and desgfrom.Status =1 


left join SET_Designation desgto on  desgto.DesignationID =  HRMS_TransferPosting.FromDesignation
and desgto.Status =1 

left join HRMS_SET_Shift shiftfrom on shiftfrom.ShiftID =  HRMS_TransferPosting.FromShift
and shiftfrom.Status =1 


left join HRMS_SET_Shift shiftto on shiftto.ShiftID =  HRMS_TransferPosting.ToShift
and shiftto.Status =1 

where HRMS_TransferPosting.CompanyID = @CompanyID
and HRMS_TransferPosting.PeriodID = @PeriodID
and HRMS_TransferPosting.PersonID = @PersonID
and HRMS_TransferPosting.Status =1 

";

        const string Query_EmployeeHistoryReportWithIncrement = @"SELECT 
HRMS_Set_Employee.EmployeeCode,HRMS_Set_Employee.FullName,HRMS_TransferPosting.ToDate  AS Months ,SET_Designation.DesignationName As Position,
HRMS_SalaryHeader.GrossAmount
FROM HRMS_TransferPosting
INNER JOIN SET_Designation on  SET_Designation.DesignationID =  HRMS_TransferPosting.ToDesignation
and SET_Designation.Status =1 
INNER JOIN HRMS_SalaryHeader on HRMS_SalaryHeader.PersonID = HRMS_TransferPosting.PersonID
INNER JOIN HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_TransferPosting.PersonID
Where HRMS_TransferPosting.CompanyID = @CompanyID
And HRMS_TransferPosting.PeriodID = @PeriodID
And HRMS_TransferPosting.PersonID = @PersonID
and HRMS_TransferPosting.Status =1  ";


        #endregion

        #region Methods
        public static DataTable GetDataByDocumentNo(int transferPostID = 0, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dr = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@TransferPostID", transferPostID);

                DataSet ds = null;
                if (transferPostID > 0)
                {
                    sb.Append(" WHERE [TransferPostID] = " + transferPostID);
                }
                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_GetDatabyDocumentNo,sb.ToString()), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetDatabyDocumentNo, sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDatabyDocumentNo, sb.ToString()), sqlparams);

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

        public static DataTable GetDataByPersonID(int company, int period,int Person = 0)
        {
            DataTable dr = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@PersonID", Person);
                sqlparams[1] = new SqlParameter("@CompanyID", company);
                sqlparams[2] = new SqlParameter("@PeriodID", period);

                DataSet ds = null;
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_DataByPersonID), sqlparams);

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

        public static DataTable EmployeeHistoryReportWithIncrement(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> emplyeeID, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (emplyeeID.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@PersonID", emplyeeID);
                }
                //else
                //    sqlparams[2] = new SqlParameter("@PersonID", 0);

              


                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_EmployeeHistoryReportWithIncrement, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_EmployeeHistoryReportWithIncrement, sqlparams);

                if (ds != null)
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
