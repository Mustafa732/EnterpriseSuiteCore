using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.AR.Setup;
using FnI.BusinessAccessLayer.Inventory.Setup;
using FnI.BusinessAccessLayer.Setup;

namespace FnI.BusinessAccessLayer.Purchase
{
    public partial class PUR_EnquirySupplier
    {
        public int RowUniqueID;

        #region Constant Strings

        const string Query_Select_By_EHID_CompanyId_PeriodId = @" SELECT [PUR_EnquirySupplier].[CompanyID]
      ,[PUR_EnquirySupplier].[PeriodID]
      ,[PUR_EnquirySupplier].[EnquiryHeaderID]
      ,[PUR_EnquirySupplier].[EnquiryDetailID]
      ,[PUR_EnquirySupplier].[ItemID]
      ,[PUR_EnquirySupplier].[SupplierID] AS [CustomerID]
      ,[AR_SET_Customer].[CustomerName]
      ,'' AS [GridRow]
  FROM [PUR_EnquirySupplier]
  INNER JOIN [AR_SET_Customer] ON [AR_SET_Customer].[CustomerID] = [PUR_EnquirySupplier].[SupplierID] AND [AR_SET_Customer].[Status] = 1
  WHERE [PUR_EnquirySupplier].[CompanyID] = @CompanyID AND [PUR_EnquirySupplier].[PeriodID] <= @PeriodID AND [PUR_EnquirySupplier].[EnquiryHeaderID] = @EnquiryHeaderID  ORDER BY [PUR_EnquirySupplier].[EnquiryDetailID] ";

        const string Query_Select_Distinct_SuppliersHaveQuotations = @" 
SELECT DISTINCT [PUR_EnquirySupplier].[SupplierID] AS [CustomerID], ([AR_SET_Customer].[CustomerName] + ' (Ref#: ' + ISNULL([PUR_QuotationHeader].[ReferenceNumber], ' ') + ')') AS [CustomerName] 
FROM [PUR_EnquirySupplier] 
	INNER JOIN [AR_SET_Customer] ON [AR_SET_Customer].[CustomerID] = [PUR_EnquirySupplier].[SupplierID] 
        AND [AR_SET_Customer].[Status] = 1 
    INNER JOIN [PUR_QuotationHeader] ON [PUR_QuotationHeader].[CompanyID] = [PUR_EnquirySupplier].[CompanyID] 
        AND [PUR_QuotationHeader].[PeriodID] = [PUR_EnquirySupplier].[PeriodID] AND [PUR_QuotationHeader].[EnquiryHeaderID] = [PUR_EnquirySupplier].[EnquiryHeaderID] 
        AND [PUR_QuotationHeader].[SupplierID] = [PUR_EnquirySupplier].[SupplierID] AND [PUR_QuotationHeader].[Status] = 1
WHERE [PUR_EnquirySupplier].[CompanyID] = @CompanyID 
AND [PUR_EnquirySupplier].[PeriodID] <= @PeriodID AND [PUR_EnquirySupplier].[EnquiryHeaderID] = @EnquiryHeaderID and [PUR_QuotationHeader].QuotationID = @QuotationID
ORDER BY [CustomerName] ";

        const string Query_Select_QuotationApprovalDocumentType = @" 
SELECT SET_DocumentType.DocumentTypeID, SET_DocumentType.DocumentType FROM dbo.SET_DocumentType 
WHERE SET_DocumentType.DocumentType LIKE '%Request for Quotation%' 
AND SET_DocumentType.CompanyID = @CompanyID
AND SET_DocumentType.PeriodID = @PeriodID
AND SET_DocumentType.Status = 1
 ";

        const string Query_DeleteDetail_ByKeys = @" DELETE FROM [PUR_EnquirySupplier] WHERE [CompanyID] = @CompanyID AND [PeriodID] = @PeriodID AND [EnquiryHeaderID] = @EnquiryHeaderID ";

        #endregion

        #region Methods

        public static DataTable GetDataByKeyTable(int enquiryHeaderID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@EnquiryHeaderID", enquiryHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if(objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_EHID_CompanyId_PeriodId, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_Select_By_EHID_CompanyId_PeriodId, sqlparams);

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

        public bool Insert2(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[9];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[2] = new SqlParameter("@EnquiryHeaderID", f_EnquiryHeaderID);
                sqlparams[3] = new SqlParameter("@EnquiryDetailID", f_EnquiryDetailID);
                sqlparams[4] = new SqlParameter("@ItemID", f_ItemID);
                sqlparams[5] = new SqlParameter("@SupplierID", f_SupplierID);
                sqlparams[6] = new SqlParameter("@CreatedBy", f_CreatedBy);
                sqlparams[7] = new SqlParameter("@CreateDate", f_CreateDate);
                sqlparams[8] = new SqlParameter("@CreatedByValue", f_CreatedByValue);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Insert, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Insert, sqlparams) > 0)
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

        public static bool DeleteDetailDataByKeys(SqlTransaction objTransaction, int companyID, int periodID, long enquiryHeaderID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@EnquiryHeaderID", enquiryHeaderID);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteDetail_ByKeys, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DeleteDetail_ByKeys, sqlparams) > 0)
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

        public static DataTable GetDistinctSuppliersHaveQuotations(int companyID, int enquiryHeaderID, int periodID, int QuotationID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@EnquiryHeaderID", enquiryHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@QuotationID", QuotationID);
                

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_Distinct_SuppliersHaveQuotations, sqlparams);

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

        public static DataTable GetQuotationApprovalDocumentType(int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);



                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_QuotationApprovalDocumentType, sqlparams);

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
