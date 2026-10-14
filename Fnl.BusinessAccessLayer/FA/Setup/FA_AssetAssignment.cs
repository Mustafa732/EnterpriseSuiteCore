using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.FA.Transaction;
using FnI.BusinessAccessLayer.Setup;

namespace FnI.BusinessAccessLayer.FA.Setup
{
    public partial class FA_AssetAssignment
    {

        #region Fields
        #endregion


        #region Properties
        #endregion


        #region Constant Strings
        const string Query_for_Location_Dropdownlist = @"select SL.LocationID,SL.LocationName from [SET_Location] SL where SL.Status=1 order by SL.LocationName";
        
        const string Query_for_Department_Dropdownlist = @"SELECT SD.DepartmentID, SD.DepartmentName from [dbo].[SET_Department] SD where SD.Status=1 order by SD.DepartmentName";

////////        const string Query_From_OtherTable = @" select FAA.[AssetAssignmentID],FAA.AssetTransactionID,FAT.AssetTag, FAA.[ExistingCustodianID],FAC.CustomerName as ExistingCustomerName, FAA.[NewCustodianID],
////////  FACN.CustomerName as NewCustodianName,FAA.ExistingLocationID,FAL.LocationName as ExistingLocationName, FAA.NewLocationID,FALN.LocationName as NewLocationName,
////////  FAA.[FromDepartmentID],FAD.DepartmentName as FromDepartmentName, FAA.[ToDepartmentID],FADN.DepartmentName as ToDepartmentName,FAA.CreateDate,ROW_NUMBER() Over (Order by FAA.[AssetAssignmentID]) as 'Row'
////////   FROM [dbo].[FA_AssetAssignment] FAA
////////   Inner Join [FA_AssetTransaction] FAT
////////   ON FAT.CompanyID=FAA.CompanyID
////////   AND FAT.AssetTransactionID=FAA.AssetTransactionID
////////  Inner join [AR_SET_Customer] FAC
////////  on FAC.CustomerID=FAA.ExistingCustodianID
////////    Inner join [AR_SET_Customer] FACN
////////  on FACN.CustomerID=FAA.NewCustodianID
////////  Inner join [SET_Location] FAL
////////  ON FAL.LocationID=FAA.ExistingLocationID
////////Inner join [SET_Location] FALN
////////  ON FALN.LocationID=FAA.NewLocationID
////////  Left Join [SET_Department] FAD
////////  ON FAD.DepartmentID=FAA.FromDepartmentID
////////  Left Join [SET_Department] FADN
////////  ON FADN.DepartmentID=FAA.ToDepartmentID
////////     ";


        const string Query_From_OtherTable = @" SELECT FAA.[AssetAssignmentID],FAA.TagNumber,
  FAA.CustodianID,FAA.AssignedDate,
  FACN.CustomerName ,
  FAA.LocationID,FALN.LocationName,
  FADN.DepartmentID, FADN.DepartmentName,
  FAA.CreateDate,ROW_NUMBER() Over (Order by FAA.[AssetAssignmentID]) as 'Row'
  FROM [dbo].[FA_AssetAssignment] FAA
  INNER JOIN  [AR_SET_Customer] FACN
  on FACN.CustomerID=FAA.CustodianID
  INNER JOIN  [SET_Location] FALN
  ON FALN.LocationID=FAA.LocationID
  LEFT JOIN [SET_Department] FADN
  ON FADN.DepartmentID=FAA.DepartmentID";
  


//////        const string Query_GetData_AssetAssingment_Header_Detail = @" select FAA.[CompanyID], FAA.[AssetAssignmentID],FAA.AssetTransactionID,FAT.AssetTag,FAA.[ExistingCustodianID],FAC.CustomerName as ExistingCustomerName, FAA.[NewCustodianID],
//////  FACN.CustomerName as NewCustodianName,FAA.ExistingLocationID,FAL.LocationName as ExistingLocationName, FAA.NewLocationID,FALN.LocationName as NewLocationName,
//////  FAA.[FromDepartmentID],FAD.DepartmentName as FromDepartmentName, FAA.[ToDepartmentID],FADN.DepartmentName as ToDepartmentName,FAA.CreateDate,ROW_NUMBER() Over (Order by FAA.[AssetAssignmentID]) as 'Row'
//////   FROM [dbo].[FA_AssetAssignment] FAA
//////    Inner Join [FA_AssetTransaction] FAT
//////   ON FAT.CompanyID=FAA.CompanyID
//////   AND FAT.AssetTransactionID=FAA.AssetTransactionID
//////  Inner join [AR_SET_Customer] FAC
//////  on FAC.CustomerID=FAA.ExistingCustodianID
//////    Inner join [AR_SET_Customer] FACN
//////  on FACN.CustomerID=FAA.NewCustodianID
//////  Inner join [SET_Location] FAL
//////  ON FAL.LocationID=FAA.ExistingLocationID
//////Inner join [SET_Location] FALN
//////  ON FALN.LocationID=FAA.NewLocationID
//////  Left Join [SET_Department] FAD
//////  ON FAD.DepartmentID=FAA.FromDepartmentID
//////  Left Join [SET_Department] FADN
//////  ON FADN.DepartmentID=FAA.ToDepartmentID
//////
//////  where FAA.CompanyID=@CompanyID AND FAA.AssetAssignmentID=@AssetAssingmentID AND FAA.Status=1 
//////  order by FAA.AssetAssignmentID";

        const string Query_GetData_AssetAssingment_Header_Detail = @"SELECT FAA.CompanyID,FAA.[AssetAssignmentID],FAA.TagNumber,
  FAA.CustodianID,FAA.AssignedDate,
  FACN.CustomerName ,
  FAA.LocationID,FALN.LocationName,
  FADN.DepartmentID, FADN.DepartmentName,
  FAA.CreateDate,ROW_NUMBER() Over (Order by FAA.[AssetAssignmentID]) as 'Row'
  FROM [dbo].[FA_AssetAssignment] FAA
  INNER JOIN  [AR_SET_Customer] FACN
  on FACN.CustomerID=FAA.CustodianID
  INNER JOIN  [SET_Location] FALN
  ON FALN.LocationID=FAA.LocationID
  LEFT JOIN [SET_Department] FADN
  ON FADN.DepartmentID=FAA.DepartmentID
  
  
  where FAA.CompanyID=@CompanyID AND FAA.AssetAssignmentID=@AssetAssingmentID AND FAA.Status=1 
  order by FAA.AssetAssignmentID
";


   #endregion


        #region Constructor
        #endregion

        #region Loading Methods
        #endregion

        #region Class Methods

        public static DataTable Locationddl(SqlConnection objConnection = null)
        {
            DataTable dt = null;
            try
            {
                DataSet ds = null;
                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_for_Location_Dropdownlist);
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

        public static DataTable Departmentddl(SqlConnection objConnection = null)
        {
            DataTable dt = null;
            try
            {
                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_for_Department_Dropdownlist);
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

        public static DataTable GetDataByAssetAssingmentID(int companyID, int AssetAssingmentID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@AssetAssingmentID", AssetAssingmentID);
                

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_AssetAssingment_Header_Detail, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_AssetAssingment_Header_Detail, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_AssetAssingment_Header_Detail, sqlparams);

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
        #endregion
    }
}
