using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.AP.Setup
{ 
    public partial class AP_BusinessPartnerCodes : BaseClass
    {
        #region "Method"

        public DataTable GetAllData_BusinessPartnerCodes(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
        Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

            try
            {
                if (PageIndex != null)
                {
                    #region Query

                    string Query_SelectAll_WithPaging_BusinessPartnerCodes = @" 
SELECT AP_BusinessPartnerCodes.CompanyID, AP_BusinessPartnerCodes.PeriodID, AP_BusinessPartnerCodes.BusinessPartnerID, 
    AR_SET_Customer.CustomerName AS BusinessPartnerName, AP_BusinessPartnerCodes.BusinessPartnerTypeCode, AP_BusinessPartnerCodes.ChartofAccountID, 
    AP_BusinessPartnerCodes.CodeCombinationID, AP_BusinessPartnerCodes.Status, AP_BusinessPartnerCodes.CreatedBy, AP_BusinessPartnerCodes.CreateDate, 
    AP_BusinessPartnerCodes.CreatedByValue, AP_BusinessPartnerCodes.UpdatedBy, AP_BusinessPartnerCodes.UpdateDate, AP_BusinessPartnerCodes.UpdatedByValue, 
    ROW_NUMBER() OVER (Order by BusinessPartnerID) as 'Row' 
FROM AP_BusinessPartnerCodes 
    inner join AR_SET_Customer ON AP_BusinessPartnerCodes.BusinessPartnerID = AR_SET_Customer.CustomerID 
        --AND AP_BusinessPartnerCodes.CompanyID = AR_SET_Customer.CompanyID
        AND AP_BusinessPartnerCodes.PeriodID = AR_SET_Customer.PeriodID 
        AND AR_SET_Customer.Status = 1 ";

                    #endregion

                    sqlparamsList.Add(new SqlParameter("@PageIndex", PageIndex));
                    sqlparamsList.Add(new SqlParameter("@PageSize", PageSize));
                    sqlparamsList.Add(new SqlParameter("@TotalRows", ReturnTotalRows));

                    SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                    sqlparamTotalRows.Direction = ParameterDirection.Output;

                    SqlParameter[] sqlparams = sqlparamsList.ToArray();
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_WithPaging_BusinessPartnerCodes + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPaging_BusinessPartnerCodes + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    #region Query

                    string Query_SelectAll_WithoutPaging_BusinessPartnerCodes = @" 
SELECT AP_BusinessPartnerCodes.CompanyID, AP_BusinessPartnerCodes.PeriodID, AP_BusinessPartnerCodes.BusinessPartnerID, 
    AR_SET_Customer.CustomerName AS BusinessPartnerName, AP_BusinessPartnerCodes.BusinessPartnerTypeCode, AP_BusinessPartnerCodes.ChartofAccountID, 
    AP_BusinessPartnerCodes.CodeCombinationID, AP_BusinessPartnerCodes.Status, AP_BusinessPartnerCodes.CreatedBy, AP_BusinessPartnerCodes.CreateDate, 
    AP_BusinessPartnerCodes.CreatedByValue, AP_BusinessPartnerCodes.UpdatedBy, AP_BusinessPartnerCodes.UpdateDate, AP_BusinessPartnerCodes.UpdatedByValue 
FROM AP_BusinessPartnerCodes 
    inner join AR_SET_Customer ON AP_BusinessPartnerCodes.BusinessPartnerID = AR_SET_Customer.CustomerID 
        --AND AP_BusinessPartnerCodes.CompanyID = AR_SET_Customer.CompanyID
        AND AP_BusinessPartnerCodes.PeriodID = AR_SET_Customer.PeriodID 
        AND AR_SET_Customer.Status = 1 ";

                    #endregion

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_WithoutPaging_BusinessPartnerCodes + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetAllData_Update(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

            try
            {
                if (PageIndex != null)
                {
                    #region Query

                    string Query_SelectAll_WithPaging_Update = @" 
SELECT CompanyID, PeriodID, BusinessPartnerID, BusinessPartnerTypeCode, ChartofAccountID, CodeCombinationID, Status, CreatedBy, CreateDate, CreatedByValue, UpdatedBy, 
    UpdateDate, UpdatedByValue, '' as Action, ROW_NUMBER() OVER (Order by BusinessPartnerID) as 'Row' 
FROM AP_BusinessPartnerCodes ";

                    #endregion

                    sqlparamsList.Add(new SqlParameter("@PageIndex", PageIndex));
                    sqlparamsList.Add(new SqlParameter("@PageSize", PageSize));
                    sqlparamsList.Add(new SqlParameter("@TotalRows", ReturnTotalRows));

                    SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                    sqlparamTotalRows.Direction = ParameterDirection.Output;

                    SqlParameter[] sqlparams = sqlparamsList.ToArray();
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_WithPaging_Update + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    string Query_SelectAll_WithoutPaging_Update = @" 
SELECT CompanyID, PeriodID, BusinessPartnerID, BusinessPartnerTypeCode, ChartofAccountID, CodeCombinationID, Status, CreatedBy, CreateDate, CreatedByValue, UpdatedBy, 
    UpdateDate, UpdatedByValue, '' as Action 
FROM AP_BusinessPartnerCodes ";

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_WithoutPaging_Update + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public bool InsertUpdateDelete(List<AP_BusinessPartnerCodes> colACDetAdd, List<AP_BusinessPartnerCodes> colACDetUpdate, List<AP_BusinessPartnerCodes> colACDetDelete)
        {

            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateAP_BusinessPartnerCodes");

            try
            {
                this.UpdateDataByKey(sqltrans);

                foreach (AP_BusinessPartnerCodes colSDetailAdd in colACDetAdd)
                    colSDetailAdd.Insert(sqltrans);

                foreach (AP_BusinessPartnerCodes colSDetailEdit in colACDetUpdate)
                    colSDetailEdit.UpdateDataByKey(sqltrans);

                foreach (AP_BusinessPartnerCodes colSDetailDelete in colACDetDelete)
                {
                    colSDetailDelete.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, CompanyID, PeriodID, 
                        colSDetailDelete.BusinessPartnerID);
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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateDailyStoppageEntry");
            }

            return result;

        }

        private bool UpdateDataByKey(SqlTransaction objTransaction = null)
        {
            bool result = false;

            #region Query

            string Query_Update_By_PrimaryKey = @" 
UPDATE AP_BusinessPartnerCodes SET BusinessPartnerID = @BusinessPartnerID, BusinessPartnerTypeCode = @BusinessPartnerTypeCode, Status = @Status, 
    UpdatedBy = @UpdatedBy, UpdateDate = @UpdateDate, UpdatedByValue = @UpdatedByValue 
WHERE CompanyID = @CompanyID and PeriodID = @PeriodID and BusinessPartnerID = @BusinessPartnerID ";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[9];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[2] = new SqlParameter("@BusinessPartnerID", f_BusinessPartnerID);
                sqlparams[3] = new SqlParameter("@BusinessPartnerTypeCode", f_BusinessPartnerTypeCode);
                sqlparams[5] = new SqlParameter("@Status", f_Status);

                if (f_UpdatedBy.HasValue)
                    sqlparams[6] = new SqlParameter("@UpdatedBy", f_UpdatedBy.Value);
                else
                    sqlparams[6] = new SqlParameter("@UpdatedBy", DBNull.Value);

                if (f_UpdateDate.HasValue)
                    sqlparams[7] = new SqlParameter("@UpdateDate", f_UpdateDate.Value);
                else
                    sqlparams[7] = new SqlParameter("@UpdateDate", DBNull.Value);

                if (!string.IsNullOrEmpty(f_UpdatedByValue))
                    sqlparams[8] = new SqlParameter("@UpdatedByValue", f_UpdatedByValue);
                else
                    sqlparams[8] = new SqlParameter("@UpdatedByValue", DBNull.Value);

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

        private bool DeleteDataByKey(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, int companyID, int periodID, int businessPartnerID)
        {
            bool result = false;

            string Query_Delete_ByKey = @" 
Update AP_BusinessPartnerCodes Set Status = @Status, UpdatedBy = @UpdatedBy, UpdateDate = @UpdateDate, UpdatedByValue = @UpdatedByValue 
WHERE CompanyID = @CompanyID and PeriodID = @PeriodID and BusinessPartnerID = @BusinessPartnerID ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@BusinessPartnerID", businessPartnerID);
                sqlparams[3] = new SqlParameter("@Status", false);
                sqlparams[4] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[5] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[6] = new SqlParameter("@UpdatedByValue", updatedByValue);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByKey, sqlparams) > 0)
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

        public static string GetCodeAgainstBusinessPartnerType(int companyID, int periodID, int businessPartnerID, int businessPartnerTypeCode)
        {
            string returnString = string.Empty;

            string Query_GetCodeAgainstBusinessPartnerType = @" 
SELECT CodeCombinationID FROM AP_BusinessPartnerCodes 
WHERE BusinessPartnerID = @BusinessPartnerID AND BusinessPartnerTypeCode = @BusinessPartnerTypeCode AND CompanyID = @CompanyID AND PeriodID = @PeriodID AND Status = 1 ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@BusinessPartnerID", businessPartnerID);
                sqlparams[3] = new SqlParameter("@BusinessPartnerTypeCode", businessPartnerTypeCode);

                object result = SqlHelper.ExecuteScalar(CommandType.Text, Query_GetCodeAgainstBusinessPartnerType, sqlparams);

                if (result != null)
                    returnString = result.ToString();
            }
            catch(Exception ex) 
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return returnString;
        }

        public static DataTable GetTypeCodeAgainstBusinessPartner(int companyID, int periodID, int businessPartnerID)
        {
            DataTable dt = null;

            string Query_CodeAgainstBusinessPartner = @" SELECT TypeCode FROM AR_SET_BusinessPartnerTypes 
WHERE BusinessPartnerID = @BusinessPartnerID AND CompanyID = @CompanyID AND PeriodID = @PeriodID ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@BusinessPartnerID", businessPartnerID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_CodeAgainstBusinessPartner, sqlparams).Tables[0];
            }

            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }

            return dt;
        }

        #endregion
    }
}
