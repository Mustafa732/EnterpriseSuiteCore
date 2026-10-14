using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.AR.Setup
{
    public partial class AR_SET_Tax : BaseClass
    {
        #region Class Methods

        public DataTable GetAllDataWithOtherTables(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null, 
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, FnI.QueryResourceLibrary.Setup.Tax.Query_SelectAll_WithPaging_WithOtherTables + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), FnI.QueryResourceLibrary.Setup.Tax.Query_SelectAll_WithPaging_WithOtherTables + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }


                else
                {
                    #region Query

                    string Query_SelectAll_WithOtherTables = @" 
SELECT AR_SET_Tax.CompanyID, AR_SET_Tax.PeriodID, AR_SET_Tax.TaxID, AR_SET_Tax.TaxName, AR_SET_Tax.Description, AR_SET_Tax.TaxTypeID, SET_TaxType.TaxTypeName, 
    AR_SET_Tax.TaxRate, AR_SET_Tax.TaxRecoveryRate, AR_SET_Tax.TaxRecoverRuleID, SET_TaxRecoverRule.TaxRecoverRuleName, AR_SET_Tax.TaxCCID, 
    GL_CodeCombinationDetail.Description AS CCDescription, AR_SET_Tax.OffSetTaxCCID, GL_CodeCombinationDetail_1.Description AS OffSetDescription, 
    AR_SET_Tax.StartDate, AR_SET_Tax.InActiveDate, AR_SET_Tax.EnabledFlag, AR_SET_Tax.Status, AR_SET_Tax.CreatedBy, AR_SET_Tax.CreateDate, AR_SET_Tax.UpdatedBy, 
    AR_SET_Tax.UpdateDate, AR_SET_Tax.CreatedByValue, AR_SET_Tax.UpdatedByValue FROM AR_SET_Tax 
    INNER JOIN SET_TaxType ON AR_SET_Tax.TaxTypeID = SET_TaxType.TaxTypeID 
    LEFT JOIN SET_TaxRecoverRule ON AR_SET_Tax.TaxRecoverRuleID = SET_TaxRecoverRule.TaxRecoverRuleID 
    LEFT JOIN GL_CodeCombinationDetail ON 
--AR_SET_Tax.CompanyID = GL_CodeCombinationDetail.CompanyID AND 
AR_SET_Tax.TaxCCID = GL_CodeCombinationDetail.CodeCombinationID 
    LEFT JOIN GL_CodeCombinationDetail AS GL_CodeCombinationDetail_1 ON AR_SET_Tax.OffSetTaxCCID = GL_CodeCombinationDetail_1.CodeCombinationID ";

                    #endregion

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_WithOtherTables + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : TaxName), sqlparamsList.ToArray()).Tables[0];
                }
            }

            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetTaxData()
        {
            DataTable dt = null;

            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Setup.Tax.Query_SelectAll_WithPaging_WithOtherTables 
                    + " ORDER BY AR_SET_Tax.TaxName").Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }



        public DataTable GetDatabytaxID(Nullable<int> companyID, Nullable<int> periodID,int TaxID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TaxID", TaxID);

                string Query_SelectdataByTaxID = @" select * from AR_SET_Tax where TaxID = @TaxID And PeriodID = @PeriodID and Status = 1 ";
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectdataByTaxID, sqlparams).Tables[0];
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
