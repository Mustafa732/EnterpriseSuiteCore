using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;
using Common.Utils;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_IPCamera
    {
        #region Constant Strings

        const string Query_GetData_byFormName = @" SELECT * FROM [SET_IPCamera] WHERE [FormName] = @FormName AND [Status] = 1 AND [IsActive] = 1";

        const string Query_GetData = @"SELECT CameraID,FormName as FormID
,case FormName when'1' then'FirstWeigth' when'2' then'SecongWeight' when'3' then'GrossWeight' when'4' then'Tear Weight' else '' end as FormName
,URL,UserID,Password,ImageSavePath,Status,IsActive,CreatedBy,CreateDate,CreatedByValue,UpdatedBy,UpdateDate,UpdatedByValue 
FROM [SET_IPCamera] 
WHERE [CameraID] = @CameraID ";

        const string Query_GetData_byFormNamecameraid = @" SELECT * FROM [SET_IPCamera] WHERE [FormName] = @FormName AND [CameraID] = @ip_camera_id AND [Status] = 1 AND [IsActive] = 1";

        const string Query_GetAll_WithPaging = @" SELECT [CameraID],FormName,  [URL], [UserID], [Password], [ImageSavePath], [Status], [IsActive], [CreatedBy], [CreateDate], [CreatedByValue], [UpdatedBy], [UpdateDate], [UpdatedByValue], ROW_NUMBER() Over (Order by CameraID) as 'Row' FROM [SET_IPCamera] ";
        const string Query_GetAll_WithoutPaging = @" SELECT [CameraID],FormName, [URL], [UserID], [Password], [ImageSavePath], [Status], [IsActive], [CreatedBy], [CreateDate], [CreatedByValue], [UpdatedBy], [UpdateDate], [UpdatedByValue] FROM [SET_IPCamera]";
        #endregion

        #region Class Methods

        public static DataTable GetDataByFormName(string formName, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                sqlparams[0] = new SqlParameter("@FormName", formName);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byFormName, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byFormName, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byFormName, sqlparams);

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



        public static DataTable GetAllDataWithFormName(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, SqlTransaction objTransaction = null)
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

                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_GetAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_GetAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_GetAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_GetAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetAll_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAll_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable Query_GetData_byFormNameipcameraid(string formName, int ip_camera_id, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@FormName", formName);
                sqlparams[1] = new SqlParameter("@ip_camera_id", ip_camera_id);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byFormNamecameraid, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byFormNamecameraid, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byFormNamecameraid, sqlparams);

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
