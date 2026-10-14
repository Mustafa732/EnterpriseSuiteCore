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
   public partial  class HRMS_SeperationCheckList
   {
       #region Constant

       const string Query_Get_MyMaxId = @" SELECT IsNull(max(EmpCheckListID), 0) + 1 as EmpCheckListID FROM HRMS_SeperationCheckList WHERE [CompanyID] = @CompanyID ";

       const string Query_MyInsert = @" INSERT INTO [HRMS_SeperationCheckList] ([CompanyID], [PersonID], [EmployeeStatusID], [EmpCheckListID], [ListID], [IsChecked], [Status], [CreatedBy], [CreateDate], [CreatedByValue])
		VALUES(@CompanyID, @PersonID, @EmployeeStatusID,(SELECT IsNull(max(EmpCheckListID), 0) + 1 as EmpCheckListID FROM HRMS_SeperationCheckList), @ListID, @IsChecked, @Status, @CreatedBy, @CreateDate, @CreatedByValue) ";


       const string Query_SelectAllCheckList = @"SELECT *,cast ( 0 as bit) as Checked FROM [HRMS_EmployeeCheckList] WHERE [Status] = 1 AND ([IsActive] = @IsActive OR @IsActive is null) ";

        const string Query_Delete_ByPersonID = @" Update [HRMS_SeperationCheckList] Set [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [CompanyID] = @CompanyID and [PersonID] = @PersonID ";


        const string Query_DeleteDataByEmployeeID = @" delete from HRMS_SeperationCheckList where PersonID = @PersonID and CompanyID =@CompanyID and PeriodID =@PeriodID";
       #endregion


        const string Query_GetCheckListData = @" SELECT  distinct

HRMS_EmployeeCheckList.ListID,HRMS_EmployeeCheckList.Description,
isnull(HRMS_SeperationCheckList.IsChecked,0) as Checked

from HRMS_EmployeeCheckList

left join  HRMS_SeperationCheckList 
on HRMS_SeperationCheckList.ListID = HRMS_EmployeeCheckList.ListID and 
HRMS_SeperationCheckList.PersonID =@PersonID
and HRMS_SeperationCheckList.Status = 1
where 
HRMS_EmployeeCheckList.Status = 1  ";



       #region Methods

       public static int GetMyNewEmpCheckListID(int companyID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
       {
           int ID = 0;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[1];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);            

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

       public static DataTable GetCheckListDataonRowEditing(int PersonID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       {
           DataTable dr = null;
           StringBuilder sb = new StringBuilder();
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[1];
               sqlparams[0] = new SqlParameter("@PersonID", PersonID);
       
               DataSet ds = null;

               if (objTransaction != null)
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetCheckListData, sqlparams);
               else if (objConnection != null)
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetCheckListData, sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetCheckListData, sqlparams);

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

    

       public bool MyInsert(SqlTransaction objTransaction = null)
       {
           bool result = false;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[10];
               sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
               sqlparams[1] = new SqlParameter("@PersonID", f_PersonID);
               sqlparams[2] = new SqlParameter("@EmployeeStatusID", f_EmployeeStatusID);
               sqlparams[3] = new SqlParameter("@EmpCheckListID", f_EmpCheckListID);
               sqlparams[4] = new SqlParameter("@ListID", f_ListID);

               if (f_IsChecked.HasValue)
                   sqlparams[5] = new SqlParameter("@IsChecked", f_IsChecked.Value);
               else
                   sqlparams[5] = new SqlParameter("@IsChecked", DBNull.Value);

               sqlparams[6] = new SqlParameter("@Status", f_Status);
               sqlparams[7] = new SqlParameter("@CreatedBy", f_CreatedBy);
               sqlparams[8] = new SqlParameter("@CreateDate", f_CreateDate);
               sqlparams[9] = new SqlParameter("@CreatedByValue", f_CreatedByValue);

               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_MyInsert, sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_MyInsert, sqlparams) > 0)
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
       
       public static bool InsertEmployeeSeperationHeaderandDetail(FnI.BusinessAccessLayer.HRMS.Payroll.Transaction.HRMS_EmployeeStatus obj, List<FnI.BusinessAccessLayer.HRMS.Payroll.Transaction.HRMS_SeperationCheckList> colEmployeedetailItems)
       {
           bool result = false;
           SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertSeperationHeaderDetail");
           try
           {
    
               if (!obj.Insert(sqltrans))
                   throw new BusinessLogicException("Record is not inserted in Employee Seperation Header");

               foreach (FnI.BusinessAccessLayer.HRMS.Payroll.Transaction.HRMS_SeperationCheckList td in colEmployeedetailItems)
               {

                   td.EmployeeStatusID = obj.EmployeeStatusID;
                   td.MyInsert(sqltrans);
               }

               result = true;
           }

           catch (BusinessLogicException ex)
           {
               SqlHelper.LogException(ex);
               throw ex;

           }

           finally
           {
               if (result)
                   SqlHelper.CommitTransaction(sqltrans);
               else
                   SqlHelper.RollBackTransaction(sqltrans, "InsertSeperationHeaderDetail");
           }

           return result;
       }


       public static DataTable GetAllActiveDataForCheckList(Nullable<bool> isActive = true, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       {
           DataTable dt = null;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[1];

               if (isActive.HasValue)
                   sqlparams[0] = new SqlParameter("@IsActive", isActive.Value);
               else
                   sqlparams[0] = new SqlParameter("@IsActive", DBNull.Value);

               DataSet ds = null;

               if (objTransaction != null)
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAllCheckList, sqlparams);
               else if (objConnection != null)
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAllCheckList, sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllCheckList, sqlparams);

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


       public static DataTable DeleteDataByEmployeeID(int PersonID, int CompanyID, int PeriodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
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
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_DeleteDataByEmployeeID, sqlparams);
               else if (objConnection != null)
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_DeleteDataByEmployeeID, sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_DeleteDataByEmployeeID, sqlparams);

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

       public static bool DeleteDataByPersonID(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, int companyID, int personID)
       {
           bool result = false;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[6];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);         
               sqlparams[1] = new SqlParameter("@PersonID", personID);
               sqlparams[2] = new SqlParameter("@Status", false);
               sqlparams[3] = new SqlParameter("@UpdatedBy", updatedBy);
               sqlparams[4] = new SqlParameter("@UpdateDate", updateDate);
               sqlparams[5] = new SqlParameter("@UpdatedByValue", updatedByValue);

               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByPersonID, sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByPersonID, sqlparams) > 0)
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
       #endregion
   }
}
