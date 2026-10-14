using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;
using System.Data;
using Common.Utils;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_DocTypeAssignment
    {
        #region Constant Strings
        const string Query_DeleteDoctypeAssignment = @"
        DELETE FROM SET_DocTypeAssignment
        WHERE 
        CompanyID = @CompanyID
        AND PeriodID = @PeriodID
        AND DocumentTypeID = @DocumentTypeID
        AND AssignedTo = @AssignedTo";

        const string Query_GetAll_WithPaging = @" SELECT [Set_DocumentType].[DocumentType],[SET_DocTypeAssignment].[CompanyID], [SET_DocTypeAssignment].[PeriodID], [SET_DocTypeAssignment].[DocumentTypeID], [AssignedTo], [SET_DocTypeAssignment].[Status], [SET_DocTypeAssignment].[CreatedBy], [SET_DocTypeAssignment].[CreateDate], [SET_DocTypeAssignment].[CreatedByValue], [SET_DocTypeAssignment].[UpdatedBy], [SET_DocTypeAssignment].[UpdateDate], [SET_DocTypeAssignment].[UpdatedByValue], ROW_NUMBER() Over (Order by  [SET_DocTypeAssignment].CompanyId)   as 'Row' FROM [SET_DocTypeAssignment] 
        JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = SET_DocTypeAssignment.DocumentTypeID ";

        const string Query_GetAll_WithoutPaging = @" SELECT [Set_DocumentType].[DocumentType],[SET_DocTypeAssignment].[CompanyID], [SET_DocTypeAssignment].[PeriodID], [SET_DocTypeAssignment].[DocumentTypeID], [AssignedTo], [SET_DocTypeAssignment].[Status], [SET_DocTypeAssignment].[CreatedBy], [SET_DocTypeAssignment].[CreateDate], [SET_DocTypeAssignment].[CreatedByValue], [SET_DocTypeAssignment].[UpdatedBy], [SET_DocTypeAssignment].[UpdateDate], [SET_DocTypeAssignment].[UpdatedByValue] FROM [SET_DocTypeAssignment] 
         JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = SET_DocTypeAssignment.DocumentTypeID "; 

        #endregion

        #region Class Methods

        public bool Delete(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", f_DocumentTypeID);
                sqlparams[3] = new SqlParameter("@AssignedTo", f_AssignedTo);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteDoctypeAssignment, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DeleteDoctypeAssignment, sqlparams) > 0)
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

        public static DataTable GetAll(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

            try
            {
                if (PageIndex != null)
                {
                    sqlparamsList.Add(new SqlParameter("@PageIndex", PageIndex));
                    sqlparamsList.Add(new SqlParameter("@PageSize", PageSize));
                    sqlparamsList.Add(new SqlParameter("@TotalRows", ReturnTotalRows));

                    SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                    sqlparamTotalRows.Direction = ParameterDirection.Output;

                    SqlParameter[] sqlparams = sqlparamsList.ToArray();
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_GetAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_GetAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAll_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
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
