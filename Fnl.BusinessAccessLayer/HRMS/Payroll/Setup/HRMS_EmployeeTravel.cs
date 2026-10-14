using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;
using System.Data;
using Common.Utils;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Setup
{
   public partial class HRMS_EmployeeTravel
   {
       #region Queries

       const string Query_Get_MyMaxId = @" SELECT IsNull(max(EmployeeTravelID), 0) + 1 as EmployeeTravelID FROM HRMS_EmployeeTravel WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID";

       const string Query_DeleteDataByEmployeeID = @"
delete from HRMS_EmployeeTravel where PersonID = @PersonID and CompanyID =@CompanyID and PeriodID =@PeriodID

";
       const string Query_GetDataByEmployeeID = @"
select HRMS_Set_Employee.FullName,HRMS_EmployeeTravel.*,case  HRMS_EmployeeTravel.VisaID  when 1 then 'International' when 2 then 'Local' when 3 then 'Domestic' else 'Select' end as VisaName,'Add' as Action,
case  HRMS_EmployeeTravel.VisaTypeID  when 1 then 'Education' when 2 then 'Labour' when 3 then 'Visit' 
when 4 then 'Medical' when 5 then 'Business' else 'Select'  end as VisaTypeName
from HRMS_EmployeeTravel 
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_EmployeeTravel.PersonID
and HRMS_Set_Employee.CompanyID = HRMS_EmployeeTravel.CompanyID
where HRMS_EmployeeTravel.Status = 1
 and HRMS_EmployeeTravel.CompanyID = @CompanyID and HRMS_EmployeeTravel.PeriodID = @PeriodID and HRMS_EmployeeTravel.PersonID = @PersonID
";

       const string Query_Delete_ByKeyMyFunction = @" Update [HRMS_EmployeeTravel] Set [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [PersonID] = @PersonID ";

       const string Query_GetDatabyEmployeeIDOnSearch = @"

select distinct 
HRMS_EmployeeTravel.Purpose,HRMS_EmployeeTravel.Destination,HRMS_EmployeeTravel.FromDate,HRMS_EmployeeTravel.VisaValidUpTo,
HRMS_EmployeeTravel.VisaID,HRMS_EmployeeTravel.VisaTypeID,
HRMS_EmployeeTravel.ToDate,HRMS_EmployeeTravel.IsCompanySponsor,HRMS_EmployeeTravel.EmployeeTravelID,
case  HRMS_EmployeeTravel.VisaID  when 1 then 'International' when 2 then 'Local' when 3 then 'Domestic' end as VisaName,
case  HRMS_EmployeeTravel.VisaTypeID  when 1 then 'Education' when 2 then 'Labour' when 3 then 'Visit' 
when 4 then 'Medical' when 5 then 'Business' end as VisaTypeName,
'' as [Action], Set_Country.CountryName,
 HRMS_Set_Employee.FullName
,HRMS_Set_Employee.FatherName
,HRMS_Set_Employee.PersonID,
HRMS_Set_Employee.BasicSalary,
Set_JobDesignation.JobDesignationName as Designation
,SET_Department.DepartmentName as Department
,HRMS_Set_Employee.JoiningDate,
HRMS_Set_Employee.DateOfBirth,
HRMS_Set_Employee.Email
,HRMS_Set_Employee.AddressLine1,
Set_OperatingUnit.OperatingUnitName as Unit
,HRMS_Set_Shift.Description as Shift
,HRMS_Set_Shift.ShiftID
,Set_OperatingUnit.OperatingUnitID,
SET_Department.DepartmentID
,Set_JobDesignation.JobDesignationID as DesignationID
,set_Location.LocationID
,set_Location.Description as Location
,Set_Company.CompanyID
  from HRMS_EmployeeTravel
 
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_EmployeeTravel.PersonID
and HRMS_Set_Employee.CompanyID = HRMS_EmployeeTravel.CompanyID

left join Set_Country on Set_Country.CountryID = HRMS_Set_Employee.FK_CountryID

left join Set_JobDesignation on Set_JobDesignation.JobDesignationID =  HRMS_SET_Employee.FK_JobDesignationID
		--AND Set_JobDesignation.CompanyID = HRMS_Set_Employee.CompanyID
		and Set_JobDesignation.status =1
		
        left join SET_Department on SET_Department.DepartmentID = HRMS_SET_Employee.FK_DepartmentID
        and SET_Department.status =1
        
        left join Set_Company on  Set_Company.CompanyID = HRMs_SET_Employee.CompanyID
        and Set_Company.Status=1
        
		left join Set_OperatingUnit on  Set_OperatingUnit.OperatingUnitID = Set_Company.OperatingUnitID
		and Set_OperatingUnit.status = 1
        
          left join HRMS_EmployeeShift on HRMS_EmployeeShift.PersonID = HRMS_Set_Employee.PersonID
        
           left join HRMS_Set_Shift on HRMS_Set_Shift.ShiftID = HRMS_EmployeeShift.ShiftID
        
        left join set_Location on set_Location.LocationID = HRMS_Set_Employee.FK_LocationID
        and set_Location.status = 1

where HRMS_EmployeeTravel.CompanyID = @CompanyID
and HRMS_EmployeeTravel.PeriodID = @PeriodID
and HRMS_EmployeeTravel.Status = 1
and HRMS_Set_Employee.Status = 1
and Set_Country.Status= 1
{0}
";

       #endregion

       #region Methods

       public static int GetMyNewEmployeeTravelID(int companyID, int periodID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
       {
           int ID = 0;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);              

               if (objTransaction != null)
                   ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_MyMaxId, sqlparams));
               else if (objConnection != null)
                   ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_Get_MyMaxId, sqlparams));
               else
                   ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_MyMaxId, sqlparams));
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return ID;
       }

       public static DataTable GetDataByEmployeeIDOnSearch(int CompanyID, int PeriodID, int PersonID = 0, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
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
                   sb.Append(" and HRMS_EmployeeTravel.PersonID = " + PersonID);
               if (objTransaction != null)
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_GetDatabyEmployeeIDOnSearch, sb.ToString()), sqlparams);
               else if (objConnection != null)
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetDatabyEmployeeIDOnSearch, sb.ToString()), sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDatabyEmployeeIDOnSearch, sb.ToString()), sqlparams);

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

       public static DataTable GetDataByEmployeeID(int CompanyID, int PeriodID, int PersonID , SqlConnection objConnection = null, SqlTransaction objTransaction = null)
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
            
               if (objTransaction != null)
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_GetDataByEmployeeID), sqlparams);
               else if (objConnection != null)
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetDataByEmployeeID), sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDataByEmployeeID), sqlparams);

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


       public static bool MyFunctionDeleteDataByKey(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, int companyID, int periodID, int personID)
       {
           bool result = false;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[7];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);              
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@PersonID", personID);
               sqlparams[3] = new SqlParameter("@Status", false);
               sqlparams[4] = new SqlParameter("@UpdatedBy", updatedBy);
               sqlparams[5] = new SqlParameter("@UpdateDate", updateDate);
               sqlparams[6] = new SqlParameter("@UpdatedByValue", updatedByValue);

               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByKeyMyFunction, sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByKeyMyFunction, sqlparams) > 0)
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

       public bool UpdateOtherDetail(List<HRMS_EmployeeTravel> colAddStockTranDetailAdd,
List<HRMS_EmployeeTravel> colUpdateStockTranDetailUpdate, List<HRMS_EmployeeTravel> colDeleteStockTranDetailDelete)
       {
           bool result = false;
           SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateTravelDetail");

           try
           {
               foreach (HRMS_EmployeeTravel std in colAddStockTranDetailAdd)
               {             
                   std.EmployeeTravelID = HRMS_EmployeeTravel.GetMyNewEmployeeTravelID(std.CompanyID, std.PeriodID, null);          
                   std.Insert(sqltrans);
               }
               foreach (HRMS_EmployeeTravel std in colUpdateStockTranDetailUpdate)
               {

                   std.UpdateDataByKey(sqltrans);
               }
               foreach (HRMS_EmployeeTravel std in colDeleteStockTranDetailDelete)
               {
                   HRMS_EmployeeTravel.DeleteDataByKey(sqltrans, Guid.Parse(std.UpdatedBy.ToString()), DateTime.Parse(std.UpdateDate.ToString()), std.UpdatedByValue, std.CompanyID,std.EmployeeTravelID,std.PeriodID,std.PersonID);
               }

               result = true;
           }
           catch (BusinessLogicException ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }
           finally
           {
               if (result)
                   SqlHelper.CommitTransaction(sqltrans);
               else
                   SqlHelper.RollBackTransaction(sqltrans, "UpdateTravelDetail");
           }

           return result;
       }

       public static bool DeleteDataByEmployeeID(int PersonID, int CompanyID, int PeriodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       {
           bool result;
           StringBuilder sb = new StringBuilder();
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@PersonID", PersonID);
               sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
               sqlparams[2] = new SqlParameter("@PeriodID", PeriodID);

               DataSet ds = null;
           
               if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteDataByEmployeeID, sqlparams) > 0)
               { 
                   result = true;
               }
               else
               {
                   result = false;
               }
              
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return result;
       }

       #endregion
   }
}
