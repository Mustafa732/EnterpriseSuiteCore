using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.GL.Setup
{
    public partial class GL_CodeCombinationHeader
    {
        #region constantstrings

        const string Query_GetData_byKey_WithOtherTables = @" Select cch.ChartOfAccountID, cch.EndDateActive, cch.StartDateActive, cch.CompanyID, cch.Narration, 
                cch.Template, (b.BusinessName + ' - ' + ou.OperatingUnitName) AS CompanyName, cch.Status, cch.EnabledFlag 
            From GL_CodeCombinationHeader cch 
            	INNER JOIN SET_Company c on c.CompanyId = cch.CompanyID 
	            INNER JOIN SET_Business b ON b.BusinessId = c.BusinessId
	            INNER JOIN SET_OperatingUnit ou ON ou.OperatingUnitId = c.OperatingUnitId
            WHERE c.Status = 1 and cch.ChartOfAccountID = @ChartOfAccountID and c.CompanyID = @CompanyID ";

        const string query_Getall = @" Select *, ROW_NUMBER() Over (Order by Set_Company.ChartOfAccountID) as 'Row' 
            From (
                Select GL_CodeCombinationHeader.ChartOfAccountID, GL_CodeCombinationHeader.Narration, GL_CodeCombinationHeader.CompanyID, Set_CompanyTab.CompanyName
                From GL_CodeCombinationHeader
                    INNER JOIN 
                    (
                        Select SET_Company.CompanyId, (SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName) AS CompanyName  
                        From SET_Company 
                            INNER JOIN SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId
						    INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId
                        WHERE SET_Company.Status = 1 
                    ) Set_CompanyTab
                    ON GL_CodeCombinationHeader.CompanyID = Set_CompanyTab.CompanyId 
                Where GL_CodeCombinationHeader.Status = 1
                ) Set_Company";

        const string query_getid = @"select OperatingUnitId,BusinessId from set_company where CompanyId=@CompanyId";

        const string query_segment = @"select SegmentSize0,SegmentSize1,SegmentSize2,SegmentSize3,SegmentSize4,SegmentSize5,SegmentSize6,SegmentSize7,SegmentSize8,SegmentSize9 from SET_OperatingUnit where  OperatingUnitId =@OperatingUnitId and BusinessId=@BusinessId";

        const string Query_GetAllSegment = @"select Template from GL_CodeCombinationHeader where [ChartOfAccountID]=@ChartOfAccountID And [CompanyID] = @CompanyID";

        const string Query_GetAll_By_CompanyID = @"Select ChartOfAccountID, Narration, Template From GL_CodeCombinationHeader 
            Where [CompanyID] = @CompanyID and [EnabledFlag] = @EnabledFlag and [Status] = @Status";

        const string Query_GetAll_By_Template = @" SELECT ChartOfAccountID, Narration, EndDateActive, StartDateActive FROM GL_CodeCombinationHeader 
WHERE [CompanyID] = @CompanyID AND [Template] = @Template AND ([EnabledFlag] = @EnabledFlag OR @EnabledFlag IS NULL) AND [Status] = 1 ORDER BY [Narration] ";

        #endregion

        #region Methods

        public static DataTable GetAllDataWithOtherTables(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null, Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc)
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, query_Getall + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), query_Getall + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, query_Getall + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataRow GetDataByKeyWithOtherTables(int chartOfAccountID, int companyID)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@ChartOfAccountID", chartOfAccountID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byKey_WithOtherTables, sqlparams);

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

        public DataTable GetDataids(int compid)
        {
            DataTable dt = null;
            DataTable objdtsegment = new DataTable();
            int opid;
            int buid;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                sqlparams[0] = new SqlParameter("@CompanyId", compid);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, query_getid, sqlparams).Tables[0];
                opid = int.Parse(dt.Rows[0][0].ToString());
                buid = int.Parse(dt.Rows[0][1].ToString());
                objdtsegment = GetAllSegment(opid, buid);

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return objdtsegment;
        }

        public DataTable GetAllSegment(int opid, int buid)
        {
            DataTable dt = null;
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@OperatingUnitId", opid);
                sqlparams[1] = new SqlParameter("@BusinessId", buid);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, query_segment, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public DataTable GetDataSegmentByKey()
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@ChartOfAccountID", f_ChartOfAccountID);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllSegment, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetDataByCompanyID(int companyID, bool enabledFlag, bool status)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@EnabledFlag", enabledFlag);
                sqlparams[2] = new SqlParameter("@Status", status);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAll_By_CompanyID, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetDataByTemplates(int companyID, string template, Nullable<bool> enabledFlag = true)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if(enabledFlag.HasValue)
                    sqlparams[1] = new SqlParameter("@EnabledFlag", enabledFlag.Value);
                else
                    sqlparams[1] = new SqlParameter("@EnabledFlag", DBNull.Value);

                sqlparams[2] = new SqlParameter("@Template", template);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAll_By_Template, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        #endregion
    }
}
