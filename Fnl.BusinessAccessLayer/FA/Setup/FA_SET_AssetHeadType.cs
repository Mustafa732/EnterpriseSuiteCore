using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using System.Text;

namespace FnI.BusinessAccessLayer.FA.Setup
{
    public partial class FA_SET_AssetHeadType
    {
        #region Field
        #endregion

        #region Properties
        #endregion

        #region Constant String

        const string Query_From_OtherTable = @"SELECT [CompanyID],[AssetHeadTypeID],[AssetHeadTypeName],FA_SET_AssetHeadType.CreateDate,[IsActive],[Status],[CategoryID], ROW_NUMBER() Over (Order by AssetHeadTypeID) as 'Row' FROM [FA_SET_AssetHeadType]";

        const string OverrideQueryString = @"
select FA_SET_AssetHeadType.AssetHeadTypeID,FA_SET_AssetHeadType.AssetHeadTypeName,
INV_SET_Category.CategoryName,INV_SET_Category.CategoryID,FA_SET_AssetHeadType.CreateDate
FROM FA_SET_AssetHeadType 
    Inner Join INV_SET_Category ON INV_SET_Category.CategoryID = FA_SET_AssetHeadType.CategoryID
     And INV_SET_Category.[Status] = 1      
WHERE FA_SET_AssetHeadType.[Status] = 1 AND ([FA_SET_AssetHeadType].[CompanyID] = @CompanyID OR @CompanyID is null) 
{0}
";
        const string OverrideQueryStringForPeriod = @"
select 
    [CompanyID],
	[AssetLifeID],
	(Cast(IntervalValue as Varchar(10)) + '  ' + PeriodType) as Description
	
from [FA_Set_AssetLife]
   
WHERE [FA_Set_AssetLife].[Status] = 1 
    AND [FA_Set_AssetLife].[CompanyID] =@CompanyID 
";

        const string Query_GetDataByCategoryID = @" SELECT * FROM [FA_SET_AssetHeadType] WHERE [CategoryID] = @CategoryID and [CompanyID] = @CompanyID ";
        //const string Query_GetDataByCategoryID = @" SELECT * FROM [FA_SET_AssetType] WHERE [CategoryID] = @CategoryID and [CompanyID] = @CompanyID ";

        #endregion

        #region Constructor
        #endregion

        #region Loading Methods
        #endregion

        #region Class Methods

        public static DataTable GetAllData_from_othertable(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_From_OtherTable + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_From_OtherTable + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_From_OtherTable + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllActiveDataForLookup(Nullable<int> companyID, Nullable<bool> isActive = true,
      SqlConnection objConnection = null, SqlTransaction objTransaction = null,string filter = null)
        {
            DataTable dt = null;           
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (isActive.HasValue)
                    sqlparams[1] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[1] = new SqlParameter("@IsActive", DBNull.Value);                              
                  
                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text,string.Format(OverrideQueryString,filter), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(OverrideQueryString, filter), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(OverrideQueryString, filter), sqlparams);

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

        public static DataTable GetAllActiveDataForPeriodLookup(Nullable<int> companyID, Nullable<bool> isActive = true,
            string filter = null, string periodType = null)
        {
            DataTable dt = null;
            StringBuilder additionalParameters = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                //if (isActive.HasValue)
                //    sqlparams[1] = new SqlParameter("@IsActive", isActive.Value);
                //else
                //    sqlparams[1] = new SqlParameter("@IsActive", DBNull.Value);

                //if (periodType != null)
                //{
                //    if (periodType == "Warranty")
                //        additionalParameters.Append(" AND PeriodTypeID = 2 ");
                //    else
                //        additionalParameters.Append(" AND PeriodTypeID = 1 ");
                //}

                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(OverrideQueryStringForPeriod), sqlparams);

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

        public static DataRow GetDataByCategoryID(int CategoryID, int companyID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CategoryID", CategoryID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetDataByCategoryID, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDataByCategoryID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByCategoryID, sqlparams);

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


        #endregion
    }
}
