using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.AR.Setup
{
    public partial class Set_BP_TaxExemption : BaseClass
    {
        public int GetNewMaxExemptionID(int companyID, int periodID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                string Query_GetMaxIdfortble = @" SELECT IsNull(max(ExemptionID), 0) + 1 as ExemptionID FROM Set_BP_TaxExemption 
WHERE CompanyID = @CompanyID and PeriodID = @PeriodID ";

                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_GetMaxIdfortble, sqlparams));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_GetMaxIdfortble, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetMaxIdfortble, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public bool UpdateTaxExemption(List<Set_BP_TaxExemption> colTaxExemptionAdd, List<Set_BP_TaxExemption> colTaxExemptionUpdate, List<Set_BP_TaxExemption> colTaxExemptionDel)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateDailyStoppageEntry");

            try
            {
                //this.UpdateDataByKey(sqltrans);

                foreach (Set_BP_TaxExemption colExDetailAdd in colTaxExemptionAdd)
                {
                    colExDetailAdd.ExemptionID = short.Parse(Set_BP_TaxExemption.GetNewExemptionID(CompanyID, PeriodID, sqltrans).ToString());
                    colExDetailAdd.Insert(sqltrans);
                }

                foreach (Set_BP_TaxExemption colSDetailEdit in colTaxExemptionUpdate)
                {
                    colSDetailEdit.UpdateDataByKey(sqltrans);
                }

                foreach (Set_BP_TaxExemption colSDetailDelete in colTaxExemptionDel)
                {
                    Set_BP_TaxExemption.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID,colSDetailDelete.ExemptionID, f_PeriodID );
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

        public DataTable GetAllCustomData(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null, 
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

                    #region Queries

                    string CustomPaging = @" WITH ResultSet AS({0}) 
SELECT * FROM ResultSet WHERE Row between (@PageIndex - 1) * @PageSize + 1 and @PageIndex * @PageSize; 
SET @TotalRows = (SELECT COUNT(*) FROM({1}) as Query) ";

                    string Query_SelectAllCustom_WithPaging = @" 
SELECT Set_BP_TaxExemption.CompanyID, Set_BP_TaxExemption.PeriodID, Set_BP_TaxExemption.BusinessPartnerID, AR_SET_Customer.CustomerName, 
    min(Set_BP_TaxExemption.FromDate) as MinFromDate, max(Set_BP_TaxExemption.ToDate) as MaxToDate, 
    ROW_NUMBER() Over (Order by Set_BP_TaxExemption.BusinessPartnerID) as 'Row' 
FROM Set_BP_TaxExemption 
    inner join AR_SET_Customer on Set_BP_TaxExemption.BusinessPartnerID = AR_SET_Customer.CustomerID --and Set_BP_TaxExemption.CompanyID = AR_SET_Customer.CompanyID 
        and Set_BP_TaxExemption.PeriodID = AR_SET_Customer.PeriodID and AR_SET_Customer.Status = 1 ";

                    #endregion

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(CustomPaging, Query_SelectAllCustom_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAllCustom_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    #region Query

                    string Query_SelectAllCustom_WithoutPaging = @" 
SELECT Set_BP_TaxExemption.CompanyID, Set_BP_TaxExemption.PeriodID, Set_BP_TaxExemption.BusinessPartnerID, AR_SET_Customer.CustomerName, 
    min(Set_BP_TaxExemption.FromDate) as MinFromDate, max(Set_BP_TaxExemption.ToDate) as MaxToDate
FROM Set_BP_TaxExemption 
    inner join AR_SET_Customer on Set_BP_TaxExemption.BusinessPartnerID = AR_SET_Customer.CustomerID --and Set_BP_TaxExemption.CompanyID = AR_SET_Customer.CompanyID 
        and Set_BP_TaxExemption.PeriodID = AR_SET_Customer.PeriodID and AR_SET_Customer.Status = 1 ";

                    #endregion

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllCustom_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy)
                        ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetAllCustomActiveData(Nullable<int> companyID, Nullable<int> periodID,Nullable<int> bussinessPartnerId, SqlConnection objConnection = null)
        {
            DataSet ds = null;
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@BusinessPartnerID", bussinessPartnerId);

                string Query_SelectAllCustomDataByBPId = @" SELECT * FROM Set_BP_TaxExemption 
WHERE Status = 1 AND (CompanyID = @CompanyID OR @CompanyID is null) AND (PeriodID = @PeriodID OR @PeriodID is null) 
    AND (BusinessPartnerID = @BusinessPartnerID OR @BusinessPartnerID is null) ";

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllCustomDataByBPId, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAllCustomDataByBPId, sqlparams);

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

        public bool DeleteCustomDataByKey(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, int companyID, 
            int businessPartnerid, int periodID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@BusinessPartnerID", businessPartnerid);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", false);
                sqlparams[4] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[5] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[6] = new SqlParameter("@UpdatedByValue", updatedByValue);

                string Query_CustomDelete_ByKey = @" Update Set_BP_TaxExemption 
Set Status = @Status, UpdatedBy = @UpdatedBy, UpdateDate = @UpdateDate, UpdatedByValue = @UpdatedByValue 
WHERE CompanyID = @CompanyID and BusinessPartnerID = @BusinessPartnerID and PeriodID = @PeriodID ";

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_CustomDelete_ByKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_CustomDelete_ByKey, sqlparams) > 0)
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
