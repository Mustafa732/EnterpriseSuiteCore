using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.GL.Setup
{
    public partial class GL_NotesDetails

         {

             const string Delete_Query = @" delete FROM [GL_NotesDetails] where NoteID=@NoteID and CompanyID=@CompanyID";


             const string Query_GetDataByKey = @"
SELECT GL_NotesDetails.CompanyID,GL_NotesDetails.NoteID,GL_NotesDetails.ChartOfAccountID,GL_NotesDetails.FromAccountCode,GL_NotesDetails.ToAccountCode,
GL_NotesDetails.ImpactType,'' as Action

FROM GL_NotesDetails   
  where GL_NotesDetails.NoteID=@NoteID and GL_NotesDetails.CompanyID=@CompanyID";


             const string Query_Update_By_PrimaryKey = @" UPDATE [GL_NotesDetails] SET [NoteID] = @NoteID, [CompanyID] = @CompanyID, [ChartOfAccountID] = @ChartOfAccountID, [FromAccountCode] = @FromAccountCode, [ToAccountCode] = @ToAccountCode, [ImpactType] = @ImpactType
		WHERE [CompanyID] = @CompanyID and [NoteID] = @NoteID ";

             public static bool delete(int noteid, int companyid, SqlTransaction sqltran = null)
             {
                 bool result = false;

                 try
                 {
                     SqlParameter[] sqlparams = new SqlParameter[2];


                     sqlparams[0] = new SqlParameter("@NoteID", noteid);
                     sqlparams[1] = new SqlParameter("@CompanyID", companyid);

                     if (sqltran != null)
                     {
                         if (SqlHelper.ExecuteNonQuery(sqltran, CommandType.Text, Delete_Query, sqlparams) > 0)
                             result = true;
                     }
                     else
                     {
                         if (SqlHelper.ExecuteNonQuery(CommandType.Text, Delete_Query, sqlparams) > 0)
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


             public static DataTable GetDataByKey(int noteid, int companyID)
             {
                 DataTable dtToReturn = null;

                 try
                 {
                     SqlParameter[] sqlparams = new SqlParameter[2];
                     sqlparams[0] = new SqlParameter("@NoteID", noteid);
                     sqlparams[1] = new SqlParameter("@CompanyID", companyID);

                     DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByKey, sqlparams);

                     if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                         dtToReturn = ds.Tables[0];
                 }
                 catch (Exception ex)
                 {
                     SqlHelper.LogException(ex);
                     throw ex;
                 }

                 return dtToReturn;
             }

             public bool UpdateDataByKey(SqlTransaction objTransaction = null)
             {
                 bool result = false;

                 try
                 {
                     SqlParameter[] sqlparams = new SqlParameter[6];
                     sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                     sqlparams[1] = new SqlParameter("@NoteID", f_NoteID);
                     sqlparams[2] = new SqlParameter("@ChartOfAccountID", f_ChartOfAccountID);
                     sqlparams[3] = new SqlParameter("@FromAccountCode", f_FromAccountCode);
                     sqlparams[4] = new SqlParameter("@ToAccountCode", f_ToAccountCode);
                     sqlparams[5] = new SqlParameter("@ImpactType", f_ImpactType);

                     if (objTransaction == null)
                     {
                         if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_By_PrimaryKey, sqlparams) > 0)
                             result = true;
                     }
                     else
                     {
                         if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_By_PrimaryKey, sqlparams) > 0)
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
