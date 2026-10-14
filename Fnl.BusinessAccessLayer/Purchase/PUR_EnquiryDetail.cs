using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Purchase
{
    public partial class PUR_EnquiryDetail : BaseClass
    {
        #region Properties

        private short f_RowUniqueID;

        public short RowUniqueID
        {
            get { return f_RowUniqueID; }
            set { f_RowUniqueID = value; }
        }
        #endregion

        #region Constant Queries

        const string Query_NewSerialNo_ByCompanyPeriodEHIDs = @" SELECT ISNULL(MAX(SerialNo), 0) + 1 AS SerialNo FROM [PUR_EnquiryDetail] 
            WHERE [EnquiryHeaderID] = @EnquiryHeaderID AND [CompanyID] = @CompanyID AND [PeriodID] = @PeriodID; ";

        const string Query_Select_By_EHID_CompanyId_PeriodId = @" 
SELECT PUR_EnquiryDetail.CompanyID, PUR_EnquiryDetail.PeriodID, PUR_EnquiryDetail.EnquiryHeaderID, PUR_EnquiryDetail.EnquiryDetailID, PUR_EnquiryDetail.SerialNo, 
    PUR_EnquiryDetail.ItemCategoryID, PUR_EnquiryDetail.ItemID, PUR_EnquiryDetail.ItemDescription, PUR_EnquiryDetail.ItemUnitID, PUR_EnquiryDetail.Quantity, 
    PUR_EnquiryDetail.Status, PUR_EnquiryDetail.CreatedBy, PUR_EnquiryDetail.CreateDate, PUR_EnquiryDetail.CreatedByValue, PUR_EnquiryDetail.UpdatedBy, 
    PUR_EnquiryDetail.UpdateDate, PUR_EnquiryDetail.UpdatedByValue, INV_SET_Category.CategoryName, INV_Item.Description, SET_UnitofMeasure.UOMName, 
    ISNULL(INV_Item.ListPricePerUnit, 0) AS ListPricePerUnit, ISNULL(ISNULL(INV_Item.ListPricePerUnit, 0) * (PUR_EnquiryDetail.Quantity), 0) AS TotalAmount, 
    Pur_EnquiryRequisition.DocumentNo as ReqNo, Pur_EnquiryRequisition.DocumentTypeID as ReqTypeID, Pur_EnquiryRequisition.StockHeaderID as StockTransactionID, 
    Pur_EnquiryRequisition.StockDetailID as TransactionDetailID, '' As Action, PUR_EnquiryDetail.SerialNo As RowUniqueID, convert(bit,'false') as IsChecked 
FROM [PUR_EnquiryDetail] 
    INNER JOIN INV_SET_Category ON INV_SET_Category.CategoryID = PUR_EnquiryDetail.ItemCategoryID 
--AND INV_SET_Category.CompanyID = PUR_EnquiryDetail.CompanyID 
        AND INV_SET_Category.Status = 1 
    INNER JOIN INV_Item ON INV_Item.ItemID = PUR_EnquiryDetail.ItemID 
--AND INV_Item.CompanyID = PUR_EnquiryDetail.CompanyID 
AND INV_Item.Status = 1 
    INNER JOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = PUR_EnquiryDetail.ItemUnitID AND SET_UnitofMeasure.Status = 1 
    LEFT JOIN Pur_EnquiryRequisition ON Pur_EnquiryRequisition.EnquiryHeaderID = PUR_EnquiryDetail.EnquiryHeaderID 
        AND Pur_EnquiryRequisition.EnquiryDetailID = PUR_EnquiryDetail.EnquiryDetailID AND Pur_EnquiryRequisition.CompanyID = PUR_EnquiryDetail.CompanyID 
        AND Pur_EnquiryRequisition.PeriodID = PUR_EnquiryDetail.PeriodID 
WHERE PUR_EnquiryDetail.EnquiryHeaderID = @EnquiryHeaderID and PUR_EnquiryDetail.CompanyID = @CompanyID and PUR_EnquiryDetail.PeriodID = @PeriodID 
    and PUR_EnquiryDetail.Status = 1 
ORDER BY PUR_EnquiryDetail.SerialNo ";

        const string Query_Select_By_SupplierID = @" 
SELECT [PUR_EnquiryDetail].[SerialNo], [PUR_EnquiryDetail].[ItemID], [PUR_EnquirySupplier].[SupplierID], [INV_SET_Category].[CategoryName], 
    [PUR_EnquiryDetail].[ItemDescription], [SET_UnitofMeasure].[UOMName], [PUR_EnquiryDetail].[Quantity], [PUR_EnquiryDetail].[EnquiryHeaderID], 
    [PUR_EnquiryDetail].[EnquiryDetailID] , 0 as NegotiationPrice1, 0 as NegotiationPrice2, 0 as Specs
,isnull((select  top 1 UnitPrice 
from AP_PurchaseOrderDetail where AP_PurchaseOrderDetail.ItemID = PUR_EnquiryDetail.ItemID 
and AP_PurchaseOrderDetail.CompanyID = @CompanyID  and AP_PurchaseOrderDetail.PeriodID = @PeriodID and AP_PurchaseOrderDetail.Status=1 order by createdate desc),0) as LastPrice 
FROM [PUR_EnquiryDetail] 
    INNER JOIN [PUR_EnquirySupplier] ON [PUR_EnquirySupplier].[EnquiryDetailID] = [PUR_EnquiryDetail].[EnquiryDetailID] 
        AND [PUR_EnquirySupplier].[EnquiryHeaderID] = [PUR_EnquiryDetail].[EnquiryHeaderID] AND [PUR_EnquirySupplier].[ItemID] = [PUR_EnquiryDetail].[ItemID] 
        AND [PUR_EnquirySupplier].[CompanyID] = [PUR_EnquiryDetail].[CompanyID] AND [PUR_EnquirySupplier].[PeriodID] = [PUR_EnquiryDetail].[PeriodID] 
    INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [PUR_EnquiryDetail].[ItemCategoryID] 
        --AND [INV_SET_Category].[CompanyID] = [PUR_EnquiryDetail].[CompanyID]
AND [INV_SET_Category].[Status] = 1 
    INNER JOIN [INV_Item] ON [INV_Item].[ItemID] = [PUR_EnquiryDetail].[ItemID] 
--AND [INV_Item].[CompanyID] = [PUR_EnquiryDetail].[CompanyID] 
        AND [INV_Item].[Status] = 1 
    INNER JOIN [SET_UnitofMeasure] ON [SET_UnitofMeasure].[UOMID] = [PUR_EnquiryDetail].[ItemUnitID] AND [SET_UnitofMeasure].[Status] = 1 


left join PUR_QuotationDetail on PUR_QuotationDetail.EnquiryHeaderID = [PUR_EnquiryDetail].EnquiryHeaderID
	and PUR_QuotationDetail.EnquiryDetailID = [PUR_EnquiryDetail].EnquiryDetailID
	and PUR_QuotationDetail.SupplierID = [PUR_EnquirySupplier].SupplierID
	and PUR_QuotationDetail.CompanyID = [PUR_EnquiryDetail].CompanyID
	and PUR_QuotationDetail.Status = 1

WHERE [PUR_EnquiryDetail].[EnquiryHeaderID] = @EnquiryHeaderID and [PUR_EnquirySupplier].SupplierID = @SupplierID and [PUR_EnquiryDetail].[CompanyID] = @CompanyID 
    and [PUR_EnquiryDetail].[PeriodID] = @PeriodID and [PUR_EnquiryDetail].[Status] = 1 
and PUR_QuotationDetail.EnquiryHeaderID is null
ORDER BY [PUR_EnquiryDetail].[SerialNo]

 ";

        const string Query_Select_By_SupplierID_RSM = @" 
SELECT PUR_EnquiryHeader.DocumentTypeID,[PUR_EnquiryDetail].[SerialNo], [PUR_EnquiryDetail].[ItemID], [PUR_EnquirySupplier].[SupplierID], [INV_SET_Category].[CategoryName], 
    [PUR_EnquiryDetail].[ItemDescription], [SET_UnitofMeasure].[UOMName], [PUR_EnquiryDetail].[Quantity], [PUR_EnquiryDetail].[EnquiryHeaderID], 
    [PUR_EnquiryDetail].[EnquiryDetailID] , 0 as NegotiationPrice1, 0 as NegotiationPrice2, 0 as Specs
,isnull((select  top 1 UnitPrice 
from AP_PurchaseOrderDetail where AP_PurchaseOrderDetail.ItemID = PUR_EnquiryDetail.ItemID 
and AP_PurchaseOrderDetail.CompanyID = @CompanyID  and AP_PurchaseOrderDetail.PeriodID = @PeriodID and AP_PurchaseOrderDetail.Status=1 order by createdate desc),0) as LastPrice 
FROM [PUR_EnquiryDetail] 
    INNER JOIN [PUR_EnquirySupplier] ON [PUR_EnquirySupplier].[EnquiryDetailID] = [PUR_EnquiryDetail].[EnquiryDetailID] 
        AND [PUR_EnquirySupplier].[EnquiryHeaderID] = [PUR_EnquiryDetail].[EnquiryHeaderID] AND [PUR_EnquirySupplier].[ItemID] = [PUR_EnquiryDetail].[ItemID] 
        AND [PUR_EnquirySupplier].[CompanyID] = [PUR_EnquiryDetail].[CompanyID] AND [PUR_EnquirySupplier].[PeriodID] = [PUR_EnquiryDetail].[PeriodID] 
    INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [PUR_EnquiryDetail].[ItemCategoryID] 
        --AND [INV_SET_Category].[CompanyID] = [PUR_EnquiryDetail].[CompanyID]
AND [INV_SET_Category].[Status] = 1 
    INNER JOIN [INV_Item] ON [INV_Item].[ItemID] = [PUR_EnquiryDetail].[ItemID] 
--AND [INV_Item].[CompanyID] = [PUR_EnquiryDetail].[CompanyID] 
        AND [INV_Item].[Status] = 1 
    INNER JOIN [SET_UnitofMeasure] ON [SET_UnitofMeasure].[UOMID] = [PUR_EnquiryDetail].[ItemUnitID] AND [SET_UnitofMeasure].[Status] = 1 

LEFT JOIN dbo.PUR_EnquiryHeader ON PUR_EnquiryHeader.CompanyID = PUR_EnquiryDetail.CompanyID AND PUR_EnquiryHeader.PeriodID = PUR_EnquiryDetail.PeriodID AND PUR_EnquiryHeader.EnquiryID = PUR_EnquiryDetail.EnquiryHeaderID

LEFT JOIN dbo.PUR_QuotationHeader ON PUR_QuotationHeader.CompanyID = PUR_EnquiryHeader.CompanyID AND PUR_QuotationHeader.PeriodID = PUR_EnquiryHeader.PeriodID AND PUR_QuotationHeader.EnquiryHeaderID = PUR_EnquiryHeader.EnquiryID

left join PUR_QuotationDetail on PUR_QuotationDetail.EnquiryHeaderID = [PUR_EnquiryDetail].EnquiryHeaderID
	and PUR_QuotationDetail.EnquiryDetailID = [PUR_EnquiryDetail].EnquiryDetailID
	and PUR_QuotationDetail.SupplierID = [PUR_EnquirySupplier].SupplierID
	and PUR_QuotationDetail.CompanyID = [PUR_EnquiryDetail].CompanyID
	and PUR_QuotationDetail.Status = 1

WHERE [PUR_EnquiryDetail].[EnquiryHeaderID] = @EnquiryHeaderID and [PUR_EnquirySupplier].SupplierID = @SupplierID and [PUR_EnquiryDetail].[CompanyID] = @CompanyID 
    and [PUR_EnquiryDetail].[PeriodID] = @PeriodID and [PUR_EnquiryDetail].[Status] = 1 
and PUR_QuotationDetail.EnquiryHeaderID is null
ORDER BY [PUR_EnquiryDetail].[SerialNo]

 ";

        const string Query_Delete_ByHeader = @" 
Update [PUR_EnquiryDetail] Set [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue 
WHERE [CompanyID] = @CompanyID and [EnquiryHeaderID] = @EnquiryHeaderID and [PeriodID] = @PeriodID ";

        #endregion

        #region Class Methods

        public DataTable GetQuantityFromRequisition(int StockTransactionID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            string Query_GetQuantityFromRequisition = @" Select ISNull(SUM(PUR_EnquiryDetail.Quantity),0) As Quantity From PUR_EnquiryDetail
		Inner Join PUR_EnquiryHeader ON PUR_EnquiryHeader.EnquiryID = PUR_EnquiryDetail.EnquiryHeaderID
			and PUR_EnquiryHeader.CompanyID = PUR_EnquiryDetail.CompanyID
			and PUR_EnquiryHeader.PeriodID = PUR_EnquiryDetail.PeriodID
			and PUR_EnquiryHeader.Status = 1
		Where PUR_EnquiryDetail.CompanyID = @CompanyID
			ANd PUR_EnquiryDetail.PeriodID = @PeriodID
			ANd PUR_EnquiryDetail.Status = 1
			AND PUR_EnquiryHeader.FK_Inv_stockTransactionID = @StockTransactionID";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@StockTransactionID", StockTransactionID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if(objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetQuantityFromRequisition, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetQuantityFromRequisition, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

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

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataTable GetDataBySupplier(int enquiryHeaderID, int supplierID, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@EnquiryHeaderID", enquiryHeaderID);
                sqlparams[1] = new SqlParameter("@SupplierID", supplierID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_SupplierID, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataTable GetDataBySupplier_RSM(int enquiryHeaderID, int supplierID, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@EnquiryHeaderID", enquiryHeaderID);
                sqlparams[1] = new SqlParameter("@SupplierID", supplierID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_SupplierID_RSM, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public short GetNewSerialNo(int enquiryHeaderID, int companyID, int periodID)
        {
            short maxSerialNo = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@EnquiryHeaderID", enquiryHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                maxSerialNo = Convert.ToInt16(SqlHelper.ExecuteScalar(CommandType.Text, Query_NewSerialNo_ByCompanyPeriodEHIDs, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return maxSerialNo;
        }

        public static bool DeleteDataByHeader(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, int companyID, 
            long enquiryHeaderID, int periodID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@EnquiryHeaderID", enquiryHeaderID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", false);
                sqlparams[4] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[5] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[6] = new SqlParameter("@UpdatedByValue", updatedByValue);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByHeader, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByHeader, sqlparams) > 0)
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

        #endregion
    }
}
