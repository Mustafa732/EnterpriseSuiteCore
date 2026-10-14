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
   public partial class SET_Section
    {
       const string Query_GetData_byDepartID = @" SELECT * FROM [SET_Section] WHERE [DepartmentID] = @DepartmentID ";

       const string Query_GetAllSection = @"Select SectionID,SectionName from SET_Section where SET_Section.Status=1 ";

       const string Query_GetOnlySectionData = @"

	SELECT [SET_Section].[SectionID], [SET_Section].[SectionName], [SET_Section].[Status] AS SectionStatus FROM [SET_Section] 
                          WHERE [SET_Section].Status = 1 AND ([SET_Section].IsActive = @IsActive) 
                          {0}
                        ORDER BY [SET_Section].[SectionName]
";
       const string Query_GetSectionByDeptID = @"SELECT [SET_Section].[SectionID], [SET_Section].DepartmentID, [SET_Section].[SectionName], [SET_Section].[Status] AS SectionStatus FROM [SET_Section] 
                          WHERE [SET_Section].Status = 1 
                          {0}
                        ORDER BY [SET_Section].[SectionName]";


       public static DataTable GetDataByDepartmentID(int DepartmentID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       {
           DataTable dt = null;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[1];
               sqlparams[0] = new SqlParameter("@DepartmentID", DepartmentID);

               DataSet ds = null;

               if (objTransaction != null)
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byDepartID, sqlparams);
               else if (objConnection != null)
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byDepartID, sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byDepartID, sqlparams);

               if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                   dt = ds.Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }



       public static DataTable GetOnlySection(int SectionID, int? isActive = null)
       {
           StringBuilder sb =new StringBuilder();
           DataTable dt = null;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[1];
               sqlparams[0] = new SqlParameter("@IsActive", isActive);
               if (SectionID > 0)
               {
                   sb.Append(" AND  [SET_Section].[SectionID] = " + SectionID);
               }
               DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text,string.Format(Query_GetOnlySectionData,sb.ToString()), sqlparams);

               if (ds != null && ds.Tables.Count > 0)
                   dt = ds.Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }

       public static DataTable GetAllSection(SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       {
           DataTable dt = null;

           try
           {              

               DataSet ds = null;

               if (objTransaction != null)
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetAllSection);
               else if (objConnection != null)
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetAllSection);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllSection);

               if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                   dt = ds.Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }


       public static DataTable GetSectionDataByDepartmentIDs(int DeptFromID, int DeptToID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       {
           DataTable dt = null;
           StringBuilder sb = new StringBuilder();
         
           try
           {//SqlParameter[] sqlparams = new SqlParameter[2];
           if (DeptFromID != 0 && DeptToID != 0)
           {
               sb.Append(" and [SET_Section].DepartmentID between " + DeptFromID + " and " + DeptToID);
           }
           else
               if (DeptFromID != 0 )
           {
               sb.Append(" and ([SET_Section].DepartmentID=" + DeptFromID + ")");
               }   
               else
                   if (DeptToID != 0 )
                   {
                       sb.Append(" and ([SET_Section].DepartmentID=" + DeptToID + ")");

                   }
                    
           DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetSectionByDeptID, sb.ToString()));

               if (ds != null && ds.Tables.Count > 0)
                   dt = ds.Tables[0];
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
