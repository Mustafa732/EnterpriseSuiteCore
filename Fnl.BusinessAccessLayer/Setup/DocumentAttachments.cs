using DAL.DataAccessLayer;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Configuration;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class DocumentAttachments
    {
        public static DataTable GetDataByAllKeys(Nullable<int> DocumentTypeID, Nullable<int> DocumentNo, Nullable<int> SourceHeaderID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[1] = new SqlParameter("@DocumentNo", DocumentNo);
                sqlparams[2] = new SqlParameter("@SourceHeaderID", SourceHeaderID);

                DataSet ds = null;

                string Query_GetData_byKey = @" select * from DocumentAttachments 
                        where SourceHeaderID = @SourceHeaderID 
                              and DocumentTypeID =@DocumentTypeID
                              and DocumentNo =@DocumentNo
                              and status = 1";




                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byKey, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byKey, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byKey, sqlparams);

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


        public static void databaseFileRead(string DocumentAttachmentsID, string varPathToNewLocation, out string filename)
        {

            string query = @" SELECT FileBinaryData FROM DocumentAttachments WHERE DocumentAttachmentsID = " + DocumentAttachmentsID;

            DataRow dr = DocumentAttachments.GetDataByKey(DocumentAttachmentsID.ToIntOrZero());


            using (var sqlQueryResult = SqlHelper.ExecuteReader(SqlHelper.GetConn(), CommandType.Text, query))
            {
                    if (sqlQueryResult != null)
                    {
                        sqlQueryResult.Read();
                        var blob = new Byte[(sqlQueryResult.GetBytes(0, 0, null, 0, int.MaxValue))];
                        sqlQueryResult.GetBytes(0, 0, blob, 0, blob.Length);
                        using (var fs = new FileStream(varPathToNewLocation+"\\"+dr["UserFileName"], FileMode.Create, FileAccess.Write))
                            fs.Write(blob, 0, blob.Length);
                    }
            }


            filename = varPathToNewLocation + "\\" + dr["UserFileName"];
        }

        public static DataRow GetDataForColorChangeByDocNoAndDocTypeID(int DocumentTypeID, int DocumentNo,int CompanyID,int FiscalID,SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                const string Query_GetData_ForColourChanging = @"Select * from DocumentAttachments where DocumentNo = @DocumentNo and DocumentTypeID = @DocumentTypeID 
									and Status=1 and CompanyID = @CompanyID and FiscalID = @FiscalID ";
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[1] = new SqlParameter("@DocumentNo", DocumentNo);
                sqlparams[2] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[3] = new SqlParameter("@FiscalID", FiscalID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_ForColourChanging, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_ForColourChanging, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_ForColourChanging, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dr = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }


    }
}
