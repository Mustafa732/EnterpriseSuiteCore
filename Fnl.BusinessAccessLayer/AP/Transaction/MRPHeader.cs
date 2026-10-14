using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.AP
{
    public partial class MRPHeader
    {
        #region methods

        public bool InsertHeaderAndDetail(List<MRPDetail> colRDDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("Insert");

            try
            {
                this.Insert(sqltrans);

                short MRPDetailID = short.Parse(MRPDetail.GetNewMRPDetail_ID().ToString());

                foreach (MRPDetail rd in colRDDetail)
                {
                    rd.MRPID = this.MRPID;
                    rd.MRPDetail_ID = MRPDetailID++;
                    rd.Insert(sqltrans);
                }

                result = true;
            }
            catch (BusinessLogicException ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            finally
            {
                if (result)
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "Insert");
            }

            return result;
        }

        public DataTable GetAllDataGird(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null, 
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

                    #region Query

                    string Query_SelectAllGird_WithPaging = @" 
SELECT MRPHeader.CompanyID, MRPHeader.MRPID, MRPHeader.DocumentTypeID, SET_DocumentType.DocumentType, MRPHeader.DocumentNo, MRPHeader.DocumentDate, 
    MRPHeader.DepartmentID, SET_Department.DepartmentName, MRPHeader.LocationID, SET_Location.LocationName, MRPHeader.CategoryID, MRPHeader.ItemID, 
    MRPHeader.Quantity, MRPHeader.StatusCode, ROW_NUMBER() Over (Order by MRPHeader.CompanyID, MRPID) as 'Row' 
FROM MRPHeader 
    inner join SET_Department on SET_Department.DepartmentID = MRPHeader.DepartmentID 
    inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = MRPHeader.DocumentTypeID 
    inner join SET_Location on SET_Location.LocationID = MRPHeader.LocationID and SET_Location.Status = 1 and SET_DocumentType.CompanyID = MRPHeader.CompanyID ";

                    #endregion

                    SqlParameter[] sqlparams = sqlparamsList.ToArray();
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAllGird_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy)
                        ? OrderBy : string.Empty), Query_SelectAllGird_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)),
                        sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy 
                        : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
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
