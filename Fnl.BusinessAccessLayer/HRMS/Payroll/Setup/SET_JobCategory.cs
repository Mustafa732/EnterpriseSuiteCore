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
   public partial  class SET_JobCategory
   {
       #region Constants

       const string Query_GetOnlyJobCategory = @" SELECT [SET_JobCategory].[JobCategoryID], [SET_JobCategory].[JobCategoryName], [SET_JobCategory].[Status] AS JobCategoryStatus FROM [SET_JobCategory] 
                          WHERE [SET_JobCategory].Status = 1 AND ([SET_JobCategory].IsActive = @IsActive) 
                          {0}  
                        ORDER BY [SET_JobCategory].[JobCategoryName]";
       #endregion

       #region Methods

       public static DataTable GetOnlyJobCategory(int JobCategoryID, int? isActive = null)
       {
           DataTable dt = null;
           StringBuilder sb = new StringBuilder();
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[1];
          
               sqlparams[0] = new SqlParameter("@IsActive", isActive);
               if (JobCategoryID > 0)
               {
                   sb.Append(" AND  [SET_JobCategory].JobCategoryID = " + JobCategoryID);
               }
               DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text,string.Format(Query_GetOnlyJobCategory,sb.ToString()), sqlparams);

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

       #endregion
   }
}
