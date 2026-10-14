using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;

using Common.Utils;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.AP.Transaction
{
    public partial class AP_PurchaseOrderDetail : BaseClass
    {
        #region Fields

        private int f_RowUniqueID;

        #endregion

        #region Properties

        public int RowUniqueID
        {
            get { return f_RowUniqueID; }
            set { f_RowUniqueID = value; }
        }

        #endregion

        #region Class Methods

        public DataTable GET_PurchaseBookSupplierWise(int company, int period, Nullable<DateTime> datefrom, Nullable<DateTime> dateto, Nullable<int> supplierfrom,
            Nullable<int> supplierto, Nullable<int> docNofrom, Nullable<int> docNoto, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", company);
                sqlparams[1] = new SqlParameter("@PeriodID", period);
                sqlparams[2] = new SqlParameter("@FromDate", datefrom);
                sqlparams[3] = new SqlParameter("@ToDate", dateto);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (supplierfrom != null && supplierto != null)
                    sb.Append(" and AR_SET_Customer.CustomerID between " + supplierfrom + " and " + supplierto + " ");

                if (docNofrom != null && docNoto != null)
                    sb.Append(" and  INV_StockTransactionHeader.DocumentNo between " + docNofrom + "  and " + docNoto + "  ");

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                #region Query

                string Query_PurchaseBookSupplierWise = @" 
select distinct SET_DocumentType.DocumentType, '(' + cast (AR_SET_Customer.CustomerID as varchar(20)) + ') ' + AR_SET_Customer.CustomerName as Supplier, 
    '(' + INV_Item.InventoryItemStatusCode + ') ' + INV_Item.Description as Item, convert(int, AP_PurchaseOrderHeader.DocumentNumber) as PONumber, 
    INV_StockTransactionHeader.DocumentNo as GRN_No, INV_StockTransactionHeader.DocumentDate as GRN_Date, AP_PurchaseOrderDetail.Quantity as PO_Quantity, 
    INV_StockTransactionDetail.PrimaryQuantity as GRN_Quantity, Set_UnitOfMeasure.UOMName, INV_StockTransactionDetail.TransactionQuantity as ApprovedGRN_Quantity, 
    INV_StockTransactionDetail.RejectedQuantity as RejectedGRN_Quantity, 
    AP_PurchaseOrderDetail.Quantity - INV_StockTransactionDetail.TransactionQuantity as RemainingQuantity, INV_StockTransactionDetail.TransactionCost as Rate, 
    round (AP_PurchaseOrderDetail.Quantity * INV_StockTransactionDetail.TransactionCost, 1) as PO_Amount, 
INV_StockTransactionDetail.Amount,
(INV_StockTransactionDetail.TransactionCost * INV_StockTransactionDetail.TransactionQuantity) as GRN_Amount, 
    round ((AP_PurchaseOrderDetail.Quantity * INV_StockTransactionDetail.TransactionCost) -  INV_StockTransactionDetail.Amount, 3) as BalanceAmount, 
    INV_StockTransactionHeader.ChallanNo, INV_StockTransactionHeader.ChallanDate, INV_SET_Category.CategoryName, Set_UnitOfMeasure.UOMName, 
    INV_StockTransactionDetail.TransactionQuantity, isnull(AR_SET_Tax.TaxRate, 0) as SalesTax,  DocumentTypesGroup.GroupName  
from INV_StockTransactionHeader 
    inner join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID 
        and INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID and INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 
        and INV_StockTransactionDetail.Status = 1 and INV_StockTransactionHeader.Status = 1 
	inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
        and SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID and SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
        and SET_DocumentType.Status = 1 
	INNER JOIN INV_Item on INV_Item.ItemID = INV_StockTransactionDetail.ItemID --and INV_Item.CompanyID = INV_StockTransactionHeader.CompanyID 
and INV_Item.status = 1 
	left join INV_SET_Category on INV_SET_Category.CategoryID = INV_Item.ItemTypeID and INV_SET_Category.Status = 1 
        --and INV_SET_Category.CompanyID = INV_StockTransactionHeader.CompanyID 
	inner join Set_UnitOfMeasure on Set_UnitOfMeasure.UOMID = INV_StockTransactionDetail.TransactionUOMID and Set_UnitOfMeasure.Status = 1 and INV_Item.Status = 1 
	left join SET_Department on SET_Department.DepartmentID = INV_StockTransactionHeader.DepartmentID and SET_Department.Status = 1 
     Left Join DocumentTypesGroup On DocumentTypesGroup.GroupID = Set_DocumentType.GroupID
--DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And  
	inner join [ApplicationMenu] on [ApplicationMenu].MenuID =  [SET_DocumentType].menuID 
	               
	inner join  AP_PurchaseOrderDetail on  AP_PurchaseOrderDetail.POHeaderID = INV_StockTransactionDetail.SourceHeaderID 
	 and AP_PurchaseOrderDetail.PODetailID = INV_StockTransactionDetail.SourceLineID 
	 

	inner join AP_PurchaseOrderHeader on AP_PurchaseOrderHeader.POHeaderID  =  AP_PurchaseOrderDetail.POHeaderID
	and AP_PurchaseOrderHeader.CompanyID   =  AP_PurchaseOrderDetail.CompanyID
	and AP_PurchaseOrderHeader.PeriodID   =  AP_PurchaseOrderDetail.PeriodID
	and AP_PurchaseOrderHeader.Status =1 
	and  AP_PurchaseOrderDetail.ItemID = INV_StockTransactionDetail.ItemID
	 and  AP_PurchaseOrderHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
	and AP_PurchaseOrderHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
	AND AP_PurchaseOrderDetail.Status = 1

    inner join  AR_SET_Customer on  AR_SET_Customer.CustomerID  =  INV_StockTransactionHeader.bUSINESSpARTNERid 
	--and AR_SET_Customer.CompanyID  =  INV_StockTransactionHeader.CompanyID 
	and AR_SET_Customer.PeriodID  =  INV_StockTransactionHeader.PeriodID 
	and AR_SET_Customer.Status = 1

	left join AR_SET_Tax on AR_SET_Tax.TaxID = AP_PurchaseOrderDetail.TaxID
	--and AR_SET_Tax.CompanyID = AP_PurchaseOrderDetail.CompanyID
	and AR_SET_Tax.PeriodID = AP_PurchaseOrderDetail.PeriodID

    inner join Set_PeriodCustom on INV_StockTransactionHeader.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
    and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID
          
		
where   [ApplicationMenu].MenuPath  like '%/Stock_frm.aspx'
 and INV_StockTransactionHeader.DocumentDate  between @FromDate and @ToDate
 and INV_StockTransactionHeader.CompanyID = @CompanyID
 and INV_StockTransactionHeader.PeriodID = @PeriodID
 {0}
 order by  convert(int,AP_PurchaseOrderHeader.DocumentNumber) 

";

                #endregion

                dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_PurchaseBookSupplierWise, sb), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }


        public static DataTable Get_PurchaseTracking(int company, int period, Nullable<DateTime> datefrom, Nullable<DateTime> dateto, Nullable<int> supplierfrom,
            Nullable<int> supplierto, Nullable<int> docNofrom, Nullable<int> docNoto, Nullable<int> itemfrom,
            Nullable<int> itemto, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            #region Query

            string Query_PurchaseTracking = @" 
select distinct SET_DocumentType.DocumentType, AR_InvoiceHeader.Description as Narration, INV_StockTransactionHeader.DocumentDate as GRN_Date,  
    AP_PurchaseOrderHeader.DocumentNumber as PO_No, AP_PurchaseOrderHeader.DocumentDate as PO_Date, AP_PurchaseOrderDetail.UnitPrice as PO_Rate, 
    '(' + cast(AR_SET_Customer.CustomerID as varchar(20)) + ') ' + AR_SET_Customer.CustomerName as Supplier, INV_SET_Category.CategoryName, Set_UnitOfMeasure.UOMName, 
    '(' + INV_Item.InventoryItemStatusCode + ') ' + INV_Item.Description as Item, AP_PurchaseOrderHeader.DocumentNumber as PONumber, 
    cast(AP_PurchaseOrderDetail.Quantity as varchar(50)) + ' ' + Set_UnitOfMeasure.UOMName as PO_Quantity,
    AP_PurchaseOrderDetail.Quantity as PO_Quantity_OR,
    INV_StockTransactionHeader.DocumentNo as GRN_No, 
    cast(INV_StockTransactionDetail.PrimaryQuantity as varchar(20)) + ' ' + Set_UnitOfMeasure.UOMName as GRN_Quantity, 
    INV_StockTransactionDetail.TransactionQuantity as ApprovedGRN_Quantity, INV_StockTransactionDetail.RejectedQuantity as RejectedGRN_Quantity, 
    INV_StockTransactionDetail.ShortQuantity, INV_StockTransactionDetail.RejectedQuantity, 
    AP_PurchaseOrderDetail.Quantity - INV_StockTransactionDetail.TransactionQuantity as RemainingQuantity, INV_StockTransactionDetail.TransactionCost as Rate, 
    round(AP_PurchaseOrderDetail.Quantity * INV_StockTransactionDetail.TransactionCost, 1) as PO_Amount, INV_StockTransactionDetail.Amount as GRN_Amount, 
    round((AP_PurchaseOrderDetail.Quantity * INV_StockTransactionDetail.TransactionCost) - INV_StockTransactionDetail.Amount, 3) as BalanceAmount, 
    INV_StockTransactionHeader.ChallanNo, INV_StockTransactionHeader.ChallanDate, INV_StockTransactionDetail.TransactionQuantity, AR_InvoiceHeader.InvoiceNumber, 
    AR_InvoiceHeader.InvoiceDate, AR_InvoiceDetail.QuantityInvoiced, (AR_InvoiceDetail.UnitPrice * QuantityInvoiced) as AmountPaid, 
    isnull(AR_InvoiceDetail.NetAmount, 0) as INV_NetAmount 
from AP_PurchaseOrderHeader 
    inner join AP_PurchaseOrderDetail on AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID 
        and AP_PurchaseOrderDetail.CompanyID = AP_PurchaseOrderHeader.CompanyID and AP_PurchaseOrderDetail.PeriodID = AP_PurchaseOrderHeader.PeriodID 
        and AP_PurchaseOrderDetail.Status = 1 and AP_PurchaseOrderDetail.Status = 1 
    INNER JOIN INV_Item on INV_Item.ItemID = AP_PurchaseOrderDetail.ItemID --and INV_Item.CompanyID = AP_PurchaseOrderDetail.CompanyID 
and INV_Item.status = 1 
	left join INV_SET_Category on INV_SET_Category.CategoryID = AP_PurchaseOrderDetail.CategoryID and INV_SET_Category.Status = 1 
        --and INV_SET_Category.CompanyID = AP_PurchaseOrderDetail.CompanyID 
    left  join INV_StockTransactionDetail on INV_StockTransactionDetail.SourceHeaderID = AP_PurchaseOrderDetail.POHeaderID 
        and AP_PurchaseOrderDetail.PODetailID = INV_StockTransactionDetail.SourceLineID and AP_PurchaseOrderDetail.ItemID = INV_StockTransactionDetail.ItemID 
        and INV_StockTransactionDetail.CompanyID = AP_PurchaseOrderHeader.CompanyID and INV_StockTransactionDetail.PeriodID = AP_PurchaseOrderHeader.PeriodID 
        AND AP_PurchaseOrderDetail.Status = 1 
    left join INV_StockTransactionHeader on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID 
        and INV_StockTransactionHeader.CompanyID = INV_StockTransactionHeader.CompanyID and INV_StockTransactionHeader.PeriodID = INV_StockTransactionHeader.PeriodID 
        and INV_StockTransactionHeader.Status = 1 and INV_StockTransactionDetail.Status = 1 
    inner join Set_UnitOfMeasure on Set_UnitOfMeasure.UOMID = AP_PurchaseOrderDetail.ItemUOMID and Set_UnitOfMeasure.Status = 1 
    left join SET_DocumentType on SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
        and SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID and SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
        and SET_DocumentType.Status = 1 
    left join ApplicationMenu on ApplicationMenu.MenuID = SET_DocumentType.menuID and ApplicationMenu.MenuPath like '%/Stock_frm.aspx' 
    left join AR_SET_Customer on AR_SET_Customer.CustomerID = AP_PurchaseOrderHeader.VendorID --and AR_SET_Customer.CompanyID = AP_PurchaseOrderHeader.CompanyID 
        and AR_SET_Customer.PeriodID = AP_PurchaseOrderHeader.PeriodID and AR_SET_Customer.Status = 1 
    left join AR_InvoiceDetail on AR_InvoiceDetail.StockDetailID = INV_StockTransactionDetail.TransactionDetailID 
        AND AR_InvoiceDetail.StockHeaderID = INV_StockTransactionDetail.StockTransactionID AND AR_InvoiceDetail.CompanyID = INV_StockTransactionDetail.CompanyID 
        AND AR_InvoiceDetail.PeriodID = INV_StockTransactionDetail.PeriodID and AR_InvoiceDetail.ItemID = INV_StockTransactionDetail.ItemID 
			left join AR_InvoiceHeader on AR_InvoiceHeader.InvoiceID = AR_InvoiceDetail.InvoiceHeaderID
			and AR_InvoiceHeader.CompanyID = AR_InvoiceDetail.CompanyID
			and AR_InvoiceHeader.PeriodID = AR_InvoiceDetail.PeriodID
			and AR_InvoiceHeader.VendorID = INV_StockTransactionHeader.BusinessPartnerID 
			and AR_InvoiceHeader.Status =1

inner join Set_PeriodCustom on AP_PurchaseOrderHeader.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID			
			where  
	        INV_StockTransactionHeader.Status = 1
			and AP_PurchaseOrderHeader.DocumentDate  between @FromDate and @ToDate
			and AP_PurchaseOrderHeader.CompanyID = @CompanyID
			and AP_PurchaseOrderHeader.PeriodID = @PeriodID
            

{0}
";

            #endregion


            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", company);
                sqlparams[1] = new SqlParameter("@PeriodID", period);
                sqlparams[2] = new SqlParameter("@FromDate", datefrom);
                sqlparams[3] = new SqlParameter("@ToDate", dateto);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

               
                if (supplierfrom != null && supplierto != null)
                {
                    sb.Append("  and  AR_SET_Customer.CustomerID  between " + supplierfrom + " and " + supplierto + " ");
                }
                else
                {
                    sb.Append("");
                }


                if (docNofrom != null && docNoto != null)
                {
                    sb.Append(" and  AP_PurchaseOrderHeader.DocumentNumber between " + docNofrom + "  and " + docNoto + "  ");
                }
                else
                {
                    sb.Append("");
                }



                if (itemfrom != null && itemto != null)
                {
                    sb.Append(" and  AP_PurchaseOrderDetail.ItemID between " + itemfrom + "  and " + itemto + "  ");
                }
                else
                {
                    sb.Append("");
                }


                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                //AP_PurchaseOrderDetail.ItemID

                //if (itemfrom != null && itemto != null)
                //{
                //    sb.Append("and INV_Item.ItemID between " + itemfrom + " and " + itemto + "");
                //}
                //else
                //{
                //    sb.Append("");
                //}

                //if (location != null)
                //{
                //    sb.Append(" and [INV_StockTransactionDetail].LocationID = " + location + "");
                //}
                //else
                //{
                //    sb.Append("");

                //}


                dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_PurchaseTracking, sb), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataTable Get_LastUnitPrice(long ItemID, int companyID, int periodID, SqlTransaction objTransaction = null)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;

            string Query_Get_LastUnitPrice = @" Select Top 1 UnitPrice, ItemID From AP_PurchaseOrderDetail 
Where CompanyID = @CompanyID And PeriodID = @PeriodID And ItemID = @ItemID Order By CreateDate Desc ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@ItemID", ItemID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                if (objTransaction == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_LastUnitPrice, sqlparams);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_Get_LastUnitPrice, sqlparams);
                }

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

        public static decimal Get_SumAllPODetailTransactionQuantity(int itemID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            decimal qtyAllTransaction = 0;
            DataTable dt = null;

            string Query_SumAllPODetailTransactionQuantity = @" select sum(Quantity) As SumOfPODetailQuantity from AP_PurchaseOrderDetail 
where ItemID  = @ItemID And CompanyID = @CompanyId And PeriodID = @PeriodID And Status = 1 ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@ItemID", itemID);
                sqlparams[1] = new SqlParameter("@CompanyId", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                if (objConnection == null)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SumAllPODetailTransactionQuantity, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SumAllPODetailTransactionQuantity, sqlparams).Tables[0];

                if (dt.Rows.Count > 0)
                    qtyAllTransaction = decimal.Parse(!string.IsNullOrEmpty(dt.Rows[0][0].ToString()) ? dt.Rows[0][0].ToString() : "0");
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return qtyAllTransaction;
        }

        public static decimal GetTaxRate(int companyID, int periodID, int taxid, SqlTransaction objTransaction = null)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@taxid", taxid);

                if (objTransaction == null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_MaxId, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_MaxId, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static DataTable GetDataByKeyTable(int pOHeaderID, int companyID, int periodID, string AllowedPeriods, bool status = true, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            #region Query

            string Query_Select_By_POHeaderID_CompanyId = FnI.QueryResourceLibrary.AP.AP_PurchaseOrderDetail.Query_Select_By_POHeaderID_CompanyId;

            #endregion

            //Fiscal Year Implementation  against PO
            if (AllowedPeriods != "")
                Query_Select_By_POHeaderID_CompanyId = Query_Select_By_POHeaderID_CompanyId.Replace("@PeriodID", AllowedPeriods);






            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@POHeaderID", pOHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", status);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_POHeaderID_CompanyId, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_Select_By_POHeaderID_CompanyId, sqlparams);

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

        public int GetNewSerialNo(int pODetailID, int companyID, int periodID, SqlTransaction objTransaction = null)
        {
            int maxSerialNo = 0;

            string Query_NewSerialNo_ByCompanyPeriodPODetailIDs = @" SELECT ISNULL(MAX(SerialNo), 0) + 1 AS SerialNo FROM AP_PurchaseOrderDetail 
WHERE PODetailID = @PODetailID AND CompanyID = @CompanyID AND PeriodID = @PeriodID ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@PODetailID", pODetailID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                if (objTransaction == null)
                    maxSerialNo = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_NewSerialNo_ByCompanyPeriodPODetailIDs, sqlparams));
                else
                    maxSerialNo = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_NewSerialNo_ByCompanyPeriodPODetailIDs, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return maxSerialNo;
        }

        public DataTable GoodReceiptStatmnet(int companyID, int periodID, Nullable<int> categoryID, Nullable<int> itemID, Nullable<int> documentTypeID,
            Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> statusCode = null, int FromSupplier = -1, int ToSupplier = -1, int FromDocumentNo = -1,
            int ToDocumentNo = -1, short? documentGroupID = null, int? ApprovingGroupID = null, int LocationID = -1, int CurrentFiscalID = 0)
        {
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            string additionalFilter = string.Empty;
            DataTable dtToReturn = null;

            #region Query

            string Query_Select_GoodReceiptStatment = @" 
Select stockDet.Comments,AP_PurchaseOrderHeader.DocumentNumber as PONum,stockHead.DocumentNo as GRNNo,AP_PurchaseOrderDetail.POHeaderID, AP_PurchaseOrderDetail.PODetailID, INV_SET_Category.CategoryName, INV_Item.InventoryItemStatusCode As ItemCode, INV_Item.Description As ItemName, 
    Set_UnitOfMeasure.UOMName, AP_PurchaseOrderDetail.Quantity AS QtyInhand, 
--	(
--        Select AVG(AP_PurchaseOrderDetail2.UnitPrice) from AP_PurchaseOrderDetail as AP_PurchaseOrderDetail2 
--	    where AP_PurchaseOrderDetail2.CompanyID = AP_PurchaseOrderDetail.CompanyID and AP_PurchaseOrderDetail2.PeriodID = AP_PurchaseOrderDetail.PeriodID 
--            and AP_PurchaseOrderDetail2.ItemID = AP_PurchaseOrderDetail.ItemID And AP_PurchaseOrderDetail2.PODetailID <= AP_PurchaseOrderDetail.PODetailID 
--            and AP_PurchaseOrderDetail2.Status = 1 and AP_PurchaseOrderDetail2.isactive = 1
--    ) As AvgPrice, 


(
        Select AVG(AP_PurchaseOrderDetail2.UnitPrice * isnull(AP_PurchaseOrderHeader2.Rate,1))+isnull(stockDet.ValueChange,0) from AP_PurchaseOrderDetail as AP_PurchaseOrderDetail2 , AP_PurchaseOrderHeader as AP_PurchaseOrderHeader2
	    where 
	       AP_PurchaseOrderDetail2.POHeaderID = AP_PurchaseOrderHeader2.POHeaderID 
	       and AP_PurchaseOrderDetail2.CompanyID = AP_PurchaseOrderDetail.CompanyID 
			and AP_PurchaseOrderDetail2.PeriodID = AP_PurchaseOrderDetail.PeriodID 
            and AP_PurchaseOrderDetail2.ItemID = AP_PurchaseOrderDetail.ItemID And AP_PurchaseOrderDetail2.PODetailID <= AP_PurchaseOrderDetail.PODetailID 
            and AP_PurchaseOrderDetail2.Status = 1 and AP_PurchaseOrderDetail2.isactive = 1
    ) As AvgPrice, 


    --AP_PurchaseOrderDetail.UnitPrice AS SellingPrice,

 (AP_PurchaseOrderDetail.UnitPrice * 
    
    (Case when stockHead.DocumentNo is not null then 
      isnull(AP_PurchaseOrderHeader.Rate,1) 
     else
       1
       end)  )+isnull(stockDet.ValueChange,0)
      AS SellingPrice,


    AR_SET_Customer.CustomerName as Supplier, stockHead.DocumentDate AS StockDate, 
    SET_DocumentType.DocumentType AS TransactionType, stockdet.TransactionQuantity, ISNULL(
    (
        select LookupDetail.EnumeratedDescription from LookupDetail where LookUpDetailID = stockHead.StatusCode and LookupHeaderID = 5
    ), '') as DocumentStatus, DocumentTypesGroup.GroupName 
from AP_PurchaseOrderDetail 
    inner join AP_PurchaseOrderHeader on AP_PurchaseOrderDetail.POHeaderID = AP_PurchaseOrderHeader.POHeaderID
	and AP_PurchaseOrderDetail.CompanyID = AP_PurchaseOrderHeader.CompanyID
	and AP_PurchaseOrderDetail.PeriodID = AP_PurchaseOrderHeader.PeriodID
	Inner Join INV_SET_Category ON 
--AP_PurchaseOrderDetail.CompanyID = INV_SET_Category.CompanyID And 
AP_PurchaseOrderDetail.CategoryID = INV_SET_Category.CategoryID 
	Inner Join INV_Item ON AP_PurchaseOrderDetail.ItemID = INV_Item.ItemID 
	Inner Join Set_UnitOfMeasure ON AP_PurchaseOrderDetail.ItemUOMID = Set_UnitOfMeasure.UOMID 
	Left Join INV_StockTransactionDetail stockDet On AP_PurchaseOrderDetail.CompanyID = stockDet.CompanyID And AP_PurchaseOrderDetail.PeriodID = stockDet.PeriodID 
        And AP_PurchaseOrderDetail.POHeaderID = stockDet.SourceHeaderID And AP_PurchaseOrderDetail.PODetailID = stockDet.SourceLineID And stockDet.Status = 1 
        And stockDet.IsActive = 1 AND stockDet.TransactionQuantity > 0 
	Left Join INV_StockTransactionHeader stockHead On stockDet.CompanyID = stockHead.CompanyID And stockDet.PeriodID = stockHead.PeriodID 
        And stockDet.StockTransactionID = stockHead.StockTransactionID And stockHead.Status = 1 And stockHead.IsActive = 1 
	Left Join AR_SET_Customer On stockHead.PeriodID = AR_SET_Customer.PeriodID 
        --and stockHead.CompanyID = AR_SET_Customer.CompanyID
        And stockHead.BusinessPartnerID = AR_SET_Customer.CustomerID 
	Left Join SET_DocumentType On stockHead.CompanyID = SET_DocumentType.CompanyID And stockHead.PeriodID = SET_DocumentType.PeriodID 
        And stockHead.DocumentTypeID = SET_DocumentType.DocumentTypeID 
    Left Join DocumentTypesGroup On DocumentTypesGroup.GroupID = Set_DocumentType.GroupID 
--DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And 
    inner join Set_PeriodCustom on stockHead.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
    and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID
Where AP_PurchaseOrderDetail.CompanyID = @CompanyID And AP_PurchaseOrderDetail.PeriodID = @PeriodID And AP_PurchaseOrderDetail.Status = 1 
    And AP_PurchaseOrderDetail.IsActive = 1 And (AP_PurchaseOrderDetail.CategoryID = @CategoryID OR @CategoryID is null) 
    And (AP_PurchaseOrderDetail.ItemID = @ItemID OR @ItemID is null) And (stockHead.DocumentTypeID = @DocumentTypeID OR @DocumentTypeID is null) ";

            #endregion

            try
            {
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));

                if (categoryID.HasValue)
                    sqlparams.Add(new SqlParameter("@CategoryID", categoryID.Value));
                else
                    sqlparams.Add(new SqlParameter("@CategoryID", DBNull.Value));

                if (itemID.HasValue)
                    sqlparams.Add(new SqlParameter("@ItemID", itemID.Value));
                else
                    sqlparams.Add(new SqlParameter("@ItemID", DBNull.Value));

                #region Stock Date filter

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    additionalFilter = " AND stockHead.DocumentDate between @FromDate AND @ToDate ";
                    sqlparams.Add(new SqlParameter("@FromDate", dateFrom.Value));
                    sqlparams.Add(new SqlParameter("@ToDate", dateTo.Value));
                }
                else if (dateFrom.HasValue)
                {
                    additionalFilter = " AND stockHead.DocumentDate >= @FromDate ";
                    sqlparams.Add(new SqlParameter("@FromDate", dateFrom.Value));
                }
                else if (dateTo.HasValue)
                {
                    additionalFilter = " AND stockHead.DocumentDate <= @ToDate ";
                    sqlparams.Add(new SqlParameter("@ToDate", dateTo.Value));
                }

                #endregion

                if (statusCode > 0)
                    additionalFilter += " AND stockHead.StatusCode = " + statusCode + "";

                if (documentTypeID.HasValue)
                    sqlparams.Add(new SqlParameter("@DocumentTypeID", documentTypeID.Value));
                else
                    sqlparams.Add(new SqlParameter("@DocumentTypeID", DBNull.Value));


                sqlparams.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));

                if (FromSupplier != -1 && ToSupplier != -1)
                {
                    additionalFilter += " AND AR_SET_Customer.CustomerID between " + FromSupplier + " and " + ToSupplier + " ";
                }
                else if (FromSupplier != -1)
                {
                    additionalFilter += " AND AR_SET_Customer.CustomerID >= " + FromSupplier;
                }
                else if (ToSupplier != -1)
                {
                    additionalFilter += " AND AR_SET_Customer.CustomerID <= " + ToSupplier;
                }


                if (FromDocumentNo != -1 && ToDocumentNo != -1)
                {
                    additionalFilter += " AND stockHead.DocumentNo between " + FromDocumentNo + " and " + ToDocumentNo + " ";
                }
                else if (FromDocumentNo != -1)
                {
                    additionalFilter += " AND stockHead.DocumentNo >= " + FromDocumentNo;
                }
                else if (ToDocumentNo != -1)
                {
                    additionalFilter += " AND stockHead.DocumentNo <= " + ToDocumentNo;
                }

                if (documentGroupID.HasValue)
                    additionalFilter += " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                if (ApprovingGroupID.HasValue && ApprovingGroupID > 0)
                    additionalFilter = additionalFilter + " AND INV_Item.ApprovalItemGroupID = " + ApprovingGroupID;

                if (LocationID > -1)
                    additionalFilter = additionalFilter + " AND  stockHead.LocationID =" + LocationID.ToString();

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_GoodReceiptStatment + additionalFilter, sqlparams.ToArray());

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



        //Method for Purchase_Order_Status_JK_Report

        public DataTable POStatus_JK(int CompanyID, int PeriodID, int FiscalID, DateTime? DateFrom, DateTime? DateTo, int? VendorFrom, int? VendorTo, Nullable<int> documentType = null, int DemandType = 0, int SeasonType = 0, int? DepartmentID = null)
        {
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            StringBuilder additionalParameters = new StringBuilder();
            StringBuilder AddParameters = new StringBuilder();
            DataTable dtToReturn = null;


            #region Query
            string Query_POStatus_JK = @"Select * from 
(
 SELECT SET_OperatingUnit.OperatingUnitName ,
        PM.DocumentNumber AS PurchaseOrderNo,
        PM.DocumentDate ,
        SET_DocumentType.DocumentType ,
        Case PM.DemandType When 1 THEN 'BMR' When 2 THEN 'Repair_Maintenance' When 3 THEN 'General' When 4 THEN 'Agri_Related' Else '' END As DemandType,
        SUM(PD.Quantity * PD.unitprice) AS Amount ,
        SUM(PD.TaxAmount) AS TaxAmount ,
        SUM(PD.NetAmount) AS TotalValue ,
        Case PM.SeasonType When 1 THEN 'Season' When 2 THEN 'Off_Season' Else '' END SeasonType,
        PR.DocumentNo AS RequisitionNo ,
        INV_StockTransactionHeader.Documentdate AS RequisitionDate ,
        SET_Department.DepartmentName ,
        AR_SET_Customer.CustomerName AS SupplierName
 FROM   AP_PurchaseOrderHeader PM
        INNER JOIN AP_PurchaseOrderDetail PD ON PM.POHeaderID = PD.POHeaderID
                                                AND PM.CompanyID = PD.CompanyID
                                                AND PM.PeriodID = PD.PeriodID
                                                AND PD.Status = 1
        LEFT OUTER JOIN AP_PurchaseOrderRequisition PR ON PR.POHeaderID = PM.POHeaderID
                                                          AND PR.CompanyID = PM.CompanyID
                                                          AND PR.PeriodID = PM.PeriodID
                                                          AND PR.PODetailID = PD.PODetailID
                                                          AND PD.Status = 1
        INNER JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = PM.VendorID
        INNER JOIN SET_DocumentType ON SET_DocumentType.DocumentTypeID = PM.TypeCode
                                       AND SET_DocumentType.CompanyID = PM.CompanyID
                                       AND SET_DocumentType.PeriodID = PM.PeriodID
        INNER JOIN AR_SET_Customer c ON c.CustomerID = PM.VendorID
                                        AND c.PeriodID = PM.PeriodID
                                        AND c.Status = 1
        INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitId = PD.CompanyID
        LEFT JOIN DocumentTypesGroup ON DocumentTypesGroup.GroupID = SET_DocumentType.GroupID
        LEFT JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.companyid = PR.companyid
                                                AND INV_StockTransactionHeader.periodid = PR.periodid
                                                AND INV_StockTransactionHeader.StockTransactionID = PR.RequisitionHeaderID
        LEFT JOIN SET_Department ON SET_Department.departmentid = INV_StockTransactionHeader.departmentid
 WHERE  --PM.FiscalID IN ( 5, 6 )
        --AND PM.DocumentNumber = 1
        --AND
        PM.CompanyID = @CompanyID AND PM.PeriodID = @PeriodID AND PM.Status = 1
        AND PM.FiscalID = @FiscalID 
        {0}
 GROUP BY SET_OperatingUnit.OperatingUnitName ,
        PM.DocumentNumber ,
        PM.DocumentDate ,
        SET_DocumentType.DocumentType ,
        PM.DemandType ,
        PM.SeasonType ,
        PR.DocumentNo ,
        INV_StockTransactionHeader.Documentdate ,
        SET_Department.departmentname ,
        AR_SET_Customer.CustomerName
)as TAB 
order by TAB.PurchaseOrderNo 
";

            #endregion

         try
                {
                    sqlparams.Add(new SqlParameter("@CompanyID", CompanyID));
                    sqlparams.Add(new SqlParameter("@PeriodID", PeriodID));
                    sqlparams.Add(new SqlParameter("@FiscalID", FiscalID));

               
                    if (VendorFrom.HasValue && VendorTo.HasValue)
                    {
                        sqlparams.Add(new SqlParameter("@VendorIDFrom", VendorFrom.Value));
                        sqlparams.Add(new SqlParameter("@VendorIDTo", VendorFrom.Value));
                        additionalParameters.Append(" And PM.VendorID between @VendorIDFrom and @VendorIDTo ");
                    }
                    else if(VendorFrom.HasValue)
                    {
                        sqlparams.Add(new SqlParameter("@VendorID", VendorFrom.Value));
                        additionalParameters.Append(" And PM.VendorID =@VendorID ");
                    }
                   else if (VendorTo.HasValue)
                    {
                        sqlparams.Add(new SqlParameter("@VendorID", VendorTo.Value));
                        additionalParameters.Append(" And PM.VendorID =@VendorID ");
                    }

                    #region Purchase Order Date filter

                    if (DateFrom.HasValue && DateTo.HasValue)
                    {
                        additionalParameters.Append(" And PM.DocumentDate between @FromDate and @ToDate ");
                        sqlparams.Add(new SqlParameter("@FromDate", DateFrom.Value));
                        sqlparams.Add(new SqlParameter("@ToDate", DateTo.Value));
                    }
                    else if (DateFrom.HasValue)
                    {
                        additionalParameters.Append(" And PM.DocumentDate >= @FromDate ");
                        sqlparams.Add(new SqlParameter("@FromDate", DateFrom.Value));
                    }
                    else if (DateTo.HasValue)
                    {
                       // additionalParameters.Append(" And AP_PurchaseOrderHeader.DocumentDate <= @ToDate ");
                       
                        additionalParameters.Append(" And PM.DocumentDate <= @ToDate ");
                        sqlparams.Add(new SqlParameter("@ToDate", DateTo.Value));
                    }

                   
                    if (documentType.HasValue)
                        additionalParameters.Append(" AND Set_DocumentType.DocumentTypeID = " + documentType.Value.ToString() + " ");

                    if (DemandType > 0)
                        additionalParameters.Append(" AND PM.DemandType = " + DemandType);

                    if (SeasonType > 0)
                        additionalParameters.Append(" AND PM.SeasonType = " + SeasonType);
                  

                    #endregion

                    if (DepartmentID > 0)
                    {
                        //sb.Append(" And SET_Department.DepartmentID = " + DepartmentID);
                        sqlparams.Add(new SqlParameter("@DepartmentID", DepartmentID));
                        additionalParameters.Append(" And SET_Department.DepartmentID = @DepartmentID ");
                    }

                    DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_POStatus_JK, additionalParameters.ToString(), AddParameters.ToString()), sqlparams.ToArray());

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




        public DataSet POStatus(int companyID, int periodID, string DocumentTypePO , Nullable<int> poNumberFrom, Nullable<int> poNumberTo, Nullable<int> categoryID, Nullable<int> itemID,
            Nullable<int> vendorID, Nullable<int> documentTypeID, Guid userID, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null,
            bool IsInvisibleUponStock = false, int? DocumentType = null, bool documentTypeAuthentication = false, short? documentGroupID = null, int CurrentFiscalID = 0, int? DepartmentID = null)
        {
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            string qry_docuTypeAuthenticate = string.Empty;
            string additionalParameters = string.Empty;
            string strCatLink = "";
            DataSet dtToReturn = null;

            #region Query

            string Query_POStatus = @" 
";

            #endregion

            try
            {
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));

                #region Purchase Order Header - Document Date filter

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    additionalParameters += " AND PM.DocumentDate Between @FromDate AND @ToDate ";
                    sqlparams.Add(new SqlParameter("@FromDate", dateFrom.Value));
                    sqlparams.Add(new SqlParameter("@ToDate", dateTo.Value));
                }
                else if (dateFrom.HasValue)
                {
                    additionalParameters += " AND PM.DocumentDate >= @FromDate ";
                    sqlparams.Add(new SqlParameter("@FromDate", dateFrom.Value));
                }
                else if (dateTo.HasValue)
                {
                    additionalParameters += " AND PM.DocumentDate <= @ToDate ";
                    sqlparams.Add(new SqlParameter("@ToDate", dateTo.Value));
                }

                if (DocumentType != null)
                    additionalParameters += "AND SET_DocumentType.GroupID = " + DocumentType;
                #endregion

                #region Purchase Order Detail - Category ID filter

                if (categoryID.HasValue)
                    sqlparams.Add(new SqlParameter("@CategoryID", categoryID.Value));
                else
                    sqlparams.Add(new SqlParameter("@CategoryID", DBNull.Value));

                #endregion

                #region Filter by Item ID

                if (itemID.HasValue)
                    sqlparams.Add(new SqlParameter("@ItemID", itemID.Value));
                else
                    sqlparams.Add(new SqlParameter("@ItemID", DBNull.Value));

                #endregion

                #region Filter by Supplier ID

                if (vendorID.HasValue)
                    sqlparams.Add(new SqlParameter("@VendorID", vendorID.Value));
                else
                    sqlparams.Add(new SqlParameter("@VendorID", DBNull.Value));

                #endregion

                #region Filter by Document Type

                if (documentTypeID.HasValue)
                    sqlparams.Add(new SqlParameter("@DocumentTypeID", documentTypeID.Value));
                else
                    sqlparams.Add(new SqlParameter("@DocumentTypeID", DBNull.Value));
                
                #endregion

                sqlparams.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));

                if (IsInvisibleUponStock)
                    strCatLink = " where (TAB.IsInvisibleUponStockReports is null or TAB.IsInvisibleUponStockReports = 0) ";

                if (poNumberFrom.HasValue && poNumberTo.HasValue)
                    additionalParameters = " and  PM.DocumentNumber between " + poNumberFrom + " and " + poNumberTo;

                if (DepartmentID > 0)
                {
                    additionalParameters += " AND  stock.DepartmentID = " + DepartmentID;                  
                }

                if (DocumentTypePO != "")
                    additionalParameters += " AND PM.TypeCode IN " + DocumentTypePO + "";

                if (documentTypeAuthentication)
                {
                    sqlparams.Add(new SqlParameter("@UserID", userID));

                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
                        AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                if (documentGroupID.HasValue)
                    additionalParameters += " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_POStatus, additionalParameters, strCatLink, qry_docuTypeAuthenticate),
                   sqlparams.ToArray());

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public DataTable GetPODetailForGateEntry(int companyID, int periodID, int PurchaseOrderDetailID)
        {
            DataTable dtToReturn = null;

            string Query_poDetailForyGateEnty = @" 
SELECT AP_PurchaseOrderDetail.CompanyID, AP_PurchaseOrderDetail.PeriodID, AP_PurchaseOrderHeader.POHeaderID as SourceHeaderID, 
    AP_PurchaseOrderDetail.PODetailID as SourceLineID, AP_PurchaseOrderHeader.TypeCode as SourceDocumentTypeID, 
    AP_PurchaseOrderHeader.DocumentNumber as SourceDocumentNo, AP_PurchaseOrderHeader.TotalAmount, 0 as Gate_OutwardDetailID, 0 as Gate_OutwardHeaderID, 
    AP_PurchaseOrderDetail.ItemID, INV_Item.[Description], INV_Item.ListPricePerUnit, AP_PurchaseOrderDetail.ItemUOMID as TransactionUOMID, SET_UnitofMeasure.UOMName, 
    AP_PurchaseOrderDetail.Quantity, ISNULL((Abs(AP_PurchaseOrderDetail.Quantity)), 0) - ISNULL(
    (
        SELECT SUM (Gate_OutwardDetail.TransactionQuantity) FROM Gate_OutwardDetail 
            INNER JOIN Gate_OutwardHeader on Gate_OutwardHeader.Gate_OutwardHeaderID = Gate_OutwardDetail.Gate_OutwardHeaderID 
                AND Gate_OutwardHeader.CompanyID = Gate_OutwardDetail.CompanyID AND Gate_OutwardHeader.PeriodID = Gate_OutwardDetail.PeriodID 
        WHERE Gate_OutwardHeader.SourceHeaderID = 196
    ), 0) AS TransactionQuantity,' ' as ReferenceNo, 
    '' AS [Action], convert(bit,0) AS  IsCheck 
FROM AP_PurchaseOrderDetail 
    INNER JOIN INV_Item ON AP_PurchaseOrderDetail.ItemID = INV_Item.ItemID --AP_PurchaseOrderDetail.CompanyID = INV_Item.CompanyID AND 
    INNER JOIN SET_UnitofMeasure ON AP_PurchaseOrderDetail.ItemUOMID = SET_UnitofMeasure.UOMID 
    Inner join AP_PurchaseOrderHeader on AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID 
        and AP_PurchaseOrderHeader.CompanyID = AP_PurchaseOrderDetail.CompanyID and AP_PurchaseOrderHeader.PeriodID = AP_PurchaseOrderDetail.PeriodID 
WHERE AP_PurchaseOrderDetail.CompanyID = @CompanyID and AP_PurchaseOrderDetail.PeriodID = @PeriodID and AP_PurchaseOrderDetail.POHeaderID = @PurchaseOrderDetailID ";

            try
            {
                SqlParameter[] sqlParams = new SqlParameter[3];
                sqlParams[0] = new SqlParameter("@PurchaseOrderDetailID", PurchaseOrderDetailID);
                sqlParams[1] = new SqlParameter("@CompanyID", companyID);
                sqlParams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_poDetailForyGateEnty, sqlParams);

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

        public DataTable Get_POHeaderDetail_ByKey(int companyID, int periodID, int PurchaseOrderDetailID)
        {
            DataTable dtToReturn = null;

            string Query_poDetailForVendorInvoice = @" 
SELECT AP_PurchaseOrderDetail.CompanyID, AP_PurchaseOrderDetail.PeriodID, NULL AS InvoiceDetailID, NULL AS InvoiceHeaderID, AP_PurchaseOrderDetail.ItemID, 
    INV_Item.Description AS ItemDescription, AP_PurchaseOrderDetail.ItemUOMID as ItemUnitOfMeasureID, SET_UnitofMeasure.UOMName, null AS AssetsAdditionFlag, 
    INV_Item.ListPricePerUnit AS BaseAmount, 0 AS  QuantityInvoiced, AP_PurchaseOrderDetail.UnitPrice,
    AP_PurchaseOrderDetail.Quantity * AP_PurchaseOrderDetail.UnitPrice AS GrossAmount, NULL AS AmountIncludesTaxFlag, '0' AS DiscountAmountTaken, '0' AS TaxAmount, 
    '0' AS NetAmount, AP_PurchaseOrderDetail.PODetailID AS TransactionDetailID, AP_PurchaseOrderDetail.POHeaderID AS StockTransactionID, 
    ISNULL(ISNULL(INV_Item.ListPricePerUnit, 0) * (AP_PurchaseOrderDetail.Quantity), 0) AS TotalAmount, '' AS [Action], 
    AP_PurchaseOrderDetail.SerialNo AS SerialNumber 
    ,  isnull(AP_PurchaseOrderDetail.ForexValue,0) ForexAmount
    , Round(isnull(AP_PurchaseOrderDetail.ForexValue,0)/isnull(AP_PurchaseOrderHeader.Rate,1),2) ForexUnitPrice
FROM AP_PurchaseOrderDetail 
    INNER JOIN AP_PurchaseOrderHeader ON AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID 
    INNER JOIN INV_Item ON AP_PurchaseOrderDetail.ItemID = INV_Item.ItemID --AP_PurchaseOrderDetail.CompanyID = INV_Item.CompanyID AND 
    INNER JOIN SET_UnitofMeasure ON AP_PurchaseOrderDetail.ItemUOMID = SET_UnitofMeasure.UOMID 
WHERE AP_PurchaseOrderDetail.CompanyID = @CompanyID and AP_PurchaseOrderDetail.PeriodID = @PeriodID and AP_PurchaseOrderDetail.POHeaderID = @PurchaseOrderDetailID ";

            try
            {
                SqlParameter[] sqlParams = new SqlParameter[3];
                sqlParams[0] = new SqlParameter("@PurchaseOrderDetailID", PurchaseOrderDetailID);
                sqlParams[1] = new SqlParameter("@CompanyID", companyID);
                sqlParams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_poDetailForVendorInvoice, sqlParams);

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

        public DataTable POBook(int companyID, int periodID, Nullable<int> vendorID, Nullable<int> poNumberFrom, Nullable<int> poNumberTo, Nullable<int> itemID, Nullable<int> doctypeid = null, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null, bool IsDetail = false, string Query_POBookHeaderDetail = "", string Query_POBookHeader = "", int CurrentFiscalID = 0, int? DepartmentID = null)
        {
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            StringBuilder additionalParameters = new StringBuilder();
            DataTable dtToReturn = null;


            try
            {
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));

                #region Purchase Order Header - Document Date filter

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    additionalParameters.Append(" AND AP_PurchaseOrderHeader.DocumentDate BETWEEN @FromDate AND @ToDate ");
                    sqlparams.Add(new SqlParameter("@FromDate", dateFrom.Value));
                    sqlparams.Add(new SqlParameter("@ToDate", dateTo.Value));
                }
                else if (dateFrom.HasValue)
                {
                    additionalParameters.Append(" AND AP_PurchaseOrderHeader.DocumentDate >= @FromDate ");
                    sqlparams.Add(new SqlParameter("@FromDate", dateFrom.Value));
                }
                else if (dateTo.HasValue)
                {
                    additionalParameters.Append(" AND AP_PurchaseOrderHeader.DocumentDate <= @ToDate ");
                    sqlparams.Add(new SqlParameter("@ToDate", dateTo.Value));
                }


                if (DepartmentID > 0)
                {
                    additionalParameters.Append(" And SET_Department.DepartmentID = " + DepartmentID);
                }

                #endregion

                if (vendorID.HasValue)
                    sqlparams.Add(new SqlParameter("@VendorID", vendorID.Value));
                else
                    sqlparams.Add(new SqlParameter("@VendorID", DBNull.Value));

                #region Purchase Order Header - Purchase Order Number (Document Number) filter

                if (poNumberFrom.HasValue && poNumberTo.HasValue)
                {
                    additionalParameters.Append(" AND AP_PurchaseOrderHeader.DocumentNumber BETWEEN @PONumberFrom AND @PONumberTo ");
                    sqlparams.Add(new SqlParameter("@PONumberFrom", poNumberFrom.Value));
                    sqlparams.Add(new SqlParameter("@PONumberTo", poNumberTo.Value));
                }
                else if (poNumberFrom.HasValue)
                {
                    additionalParameters.Append(" AND AP_PurchaseOrderHeader.DocumentNumber >= @PONumberFrom ");
                    sqlparams.Add(new SqlParameter("@PONumberFrom", poNumberFrom.Value));
                }
                else if (poNumberTo.HasValue)
                {
                    additionalParameters.Append(" AND AP_PurchaseOrderHeader.DocumentNumber <= @PONumberTo ");
                    sqlparams.Add(new SqlParameter("@PONumberTo", poNumberTo.Value));
                }

                #endregion

                if (itemID.HasValue)
                    sqlparams.Add(new SqlParameter("@ItemID", itemID.Value));
                else
                    sqlparams.Add(new SqlParameter("@ItemID", DBNull.Value));
                if (doctypeid.HasValue)
                    sqlparams.Add(new SqlParameter("@TypeCode", doctypeid.Value));
                else
                    sqlparams.Add(new SqlParameter("@TypeCode", DBNull.Value));

                sqlparams.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));


                string Query = string.Empty;
                if (IsDetail)
                    Query = string.Format(Query_POBookHeaderDetail, additionalParameters);
                else
                    Query = string.Format(Query_POBookHeader, additionalParameters);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams.ToArray());

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

        public DataTable PO_Book(int companyID, int periodID, Nullable<int> vendorID, Nullable<int> poNumberFrom, Nullable<int> poNumberTo, Nullable<int> itemID, Nullable<int> doctypeid = null, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null, bool IsDetail = false, string Query_PO_BookHeaderDetail = "", string Query_PO_BookHeader = "", int CurrentFiscalID = 0, int? DepartmentID = null)
        {
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            StringBuilder additionalParameters = new StringBuilder();
            DataTable dtToReturn = null;


            try
            {
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));

                #region Purchase Order Header - Document Date filter

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    additionalParameters.Append(" AND AP_PurchaseOrderHeader.DocumentDate BETWEEN @FromDate AND @ToDate ");
                    sqlparams.Add(new SqlParameter("@FromDate", dateFrom.Value));
                    sqlparams.Add(new SqlParameter("@ToDate", dateTo.Value));
                }
                else if (dateFrom.HasValue)
                {
                    additionalParameters.Append(" AND AP_PurchaseOrderHeader.DocumentDate >= @FromDate ");
                    sqlparams.Add(new SqlParameter("@FromDate", dateFrom.Value));
                }
                else if (dateTo.HasValue)
                {
                    additionalParameters.Append(" AND AP_PurchaseOrderHeader.DocumentDate <= @ToDate ");
                    sqlparams.Add(new SqlParameter("@ToDate", dateTo.Value));
                }


                if (DepartmentID > 0)
                {
                    additionalParameters.Append(" And SET_Department.DepartmentID = " + DepartmentID);
                }

                #endregion

                if (vendorID.HasValue)
                    sqlparams.Add(new SqlParameter("@VendorID", vendorID.Value));
                else
                    sqlparams.Add(new SqlParameter("@VendorID", DBNull.Value));

                #region Purchase Order Header - Purchase Order Number (Document Number) filter

                if (poNumberFrom.HasValue && poNumberTo.HasValue)
                {
                    additionalParameters.Append(" AND AP_PurchaseOrderHeader.DocumentNumber BETWEEN @PONumberFrom AND @PONumberTo ");
                    sqlparams.Add(new SqlParameter("@PONumberFrom", poNumberFrom.Value));
                    sqlparams.Add(new SqlParameter("@PONumberTo", poNumberTo.Value));
                }
                else if (poNumberFrom.HasValue)
                {
                    additionalParameters.Append(" AND AP_PurchaseOrderHeader.DocumentNumber >= @PONumberFrom ");
                    sqlparams.Add(new SqlParameter("@PONumberFrom", poNumberFrom.Value));
                }
                else if (poNumberTo.HasValue)
                {
                    additionalParameters.Append(" AND AP_PurchaseOrderHeader.DocumentNumber <= @PONumberTo ");
                    sqlparams.Add(new SqlParameter("@PONumberTo", poNumberTo.Value));
                }

                #endregion

                if (itemID.HasValue)
                    sqlparams.Add(new SqlParameter("@ItemID", itemID.Value));
                else
                    sqlparams.Add(new SqlParameter("@ItemID", DBNull.Value));
                if (doctypeid.HasValue)
                    sqlparams.Add(new SqlParameter("@TypeCode", doctypeid.Value));
                else
                    sqlparams.Add(new SqlParameter("@TypeCode", DBNull.Value));

                sqlparams.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));


                string Query = string.Empty;
                if (IsDetail)
                    Query = string.Format(Query_PO_BookHeaderDetail, additionalParameters);
                else
                    Query = string.Format(Query_PO_BookHeader, additionalParameters);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams.ToArray());

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

        public DataTable PO_Outstanding(int companyID, int periodID, Nullable<int> vendorID, Nullable<int> poNoFrom, Nullable<int> poNoTo,
            Nullable<int> DocumentTypeID, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null,
            string itemDescription = null, short? documentGroupID = null, bool? IsPending = false, int CurrentFiscalID = 0)
        {
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            StringBuilder additionalParameters = new StringBuilder();
            StringBuilder AddParameters = new StringBuilder();
            DataTable dtToReturn = null;

            #region Query

            string Query_PO_Outstanding = @" select * from 
(
    SELECT AP_PurchaseOrderHeader.DocumentNumber, SET_DocumentType.DocumentType AS POType, AP_PurchaseOrderHeader.POHeaderID, AP_PurchaseOrderHeader.DocumentDate, AR_SET_Customer.CustomerName, 
        AP_PurchaseOrderHeader.TotalAmount, SET_UnitofMeasure.UOMName,INV_Item.InventoryItemStatusCode as ItemCode, INV_Item.Description, AP_PurchaseOrderDetail.Quantity, isnull(
            (
                Select abs(SUM(isnull(sd.TransactionQuantity,0))) from INV_StockTransactionDetail sd 
                where AP_PurchaseOrderDetail.POHeaderID = sd.SourceHeaderID and AP_PurchaseOrderDetail.PODetailID = sd.SourceLineID 
                    and AP_PurchaseOrderHeader.CompanyID = sd.CompanyID and AP_PurchaseOrderHeader.PeriodID = sd.PeriodID AND sd.Status = 1
                    and sd.SourceDocumentTypeID =SET_DocumentType.DocumentTypeID
            ), 0) as RcvQty, AP_PurchaseOrderDetail.UnitPrice 
    FROM AP_PurchaseOrderDetail 
        INNER JOIN AP_PurchaseOrderHeader ON AP_PurchaseOrderDetail.CompanyID = AP_PurchaseOrderHeader.CompanyID AND AP_PurchaseOrderHeader.Status = 1
            AND AP_PurchaseOrderDetail.PeriodID = AP_PurchaseOrderHeader.PeriodID AND AP_PurchaseOrderDetail.POHeaderID = AP_PurchaseOrderHeader.POHeaderID
        INNER JOIN INV_Item ON AP_PurchaseOrderDetail.ItemID = INV_Item.ItemID AND INV_Item.Status = 1    --AP_PurchaseOrderDetail.CompanyID = INV_Item.CompanyID AND 
        LEFT OUTER JOIN AR_SET_Customer ON AR_SET_Customer.PeriodID = AP_PurchaseOrderHeader.PeriodID --and AR_SET_Customer.CompanyID = AP_PurchaseOrderHeader.CompanyID 
            AND AR_SET_Customer.CustomerID = AP_PurchaseOrderHeader.VendorID AND AR_SET_Customer.Status = 1
        LEFT OUTER JOIN SET_UnitofMeasure ON AP_PurchaseOrderDetail.ItemUOMID = SET_UnitofMeasure.UOMID AND SET_UnitofMeasure.Status = 1 
        INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = AP_PurchaseOrderHeader.CompanyID AND SET_DocumentType.PeriodID = AP_PurchaseOrderHeader.PeriodID 
            AND SET_DocumentType.DocumentTypeID = AP_PurchaseOrderHeader.TypeCode 
        --inner join Set_PeriodCustom on AP_PurchaseOrderHeader.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
        --and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID
    WHERE AP_PurchaseOrderDetail.CompanyID = @CompanyID AND AP_PurchaseOrderDetail.PeriodID = @PeriodID AND AP_PurchaseOrderDetail.Status = 1 
        AND (AP_PurchaseOrderHeader.VendorID = @VendorID OR @VendorID IS NULL) AND INV_Item.Description like '%' + @ItemDescription + '%'
      --  and AP_PurchaseOrderHeader.ClosedCode Is Null
           and ISNULL(AP_PurchaseOrderHeader.ClosedCode,0) <> 1  
          AND AP_PurchaseOrderHeader.StatusCode = 1
        {0}
) as TAB 
{1}
order by TAB.DocumentDate asc,TAB.DocumentNumber ";

            #endregion

            try
            {
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));
                sqlparams.Add(new SqlParameter("@ItemDescription", itemDescription));

                if (vendorID.HasValue)
                    sqlparams.Add(new SqlParameter("@VendorID", vendorID.Value));
                else
                    sqlparams.Add(new SqlParameter("@VendorID", DBNull.Value));
                
                sqlparams.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));
                

                #region Purchase Order Date filter

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    additionalParameters.Append(" And AP_PurchaseOrderHeader.DocumentDate between @FromDate and @ToDate ");
                    sqlparams.Add(new SqlParameter("@FromDate", dateFrom.Value));
                    sqlparams.Add(new SqlParameter("@ToDate", dateTo.Value));
                }
                else if (dateFrom.HasValue)
                {
                    additionalParameters.Append(" And AP_PurchaseOrderHeader.DocumentDate >= @FromDate ");
                    sqlparams.Add(new SqlParameter("@FromDate", dateFrom.Value));
                }
                else if (dateTo.HasValue)
                {
                    additionalParameters.Append(" And AP_PurchaseOrderHeader.DocumentDate <= @ToDate ");
                    sqlparams.Add(new SqlParameter("@ToDate", dateTo.Value));
                }

                if (documentGroupID.HasValue)
                    additionalParameters.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");



                #endregion

                #region Purchase Order Number filter

                if (poNoFrom.HasValue && poNoTo.HasValue)
                {
                    additionalParameters.Append(" And AP_PurchaseOrderHeader.DocumentNumber between @POFrom and @POTo ");
                    sqlparams.Add(new SqlParameter("@POFrom", poNoFrom.Value));
                    sqlparams.Add(new SqlParameter("@POTo", poNoTo.Value));
                }
                else if (poNoFrom.HasValue)
                {
                    additionalParameters.Append(" And AP_PurchaseOrderHeader.DocumentNumber >= @POFrom ");
                    sqlparams.Add(new SqlParameter("@POFrom", poNoFrom.Value));
                }
                else if (poNoTo.HasValue)
                {
                    additionalParameters.Append(" And AP_PurchaseOrderHeader.DocumentNumber <= @POTo ");
                    sqlparams.Add(new SqlParameter("@POTo", poNoTo.Value));
                }
                if (DocumentTypeID > 0)
                {
                    additionalParameters.Append(" and SET_DocumentType.DocumentTypeID = " + DocumentTypeID);
                }
                if (IsPending == true)
                    AddParameters.Append(" Where TAB.RcvQty < TAB.Quantity ");
                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_PO_Outstanding, additionalParameters.ToString(), AddParameters.ToString()), sqlparams.ToArray());

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

        public DataTable PurchaseOrderImport(int companyID, int periodID, int poNoFrom, int poNoTo, int statusCode, int? DocumentType,
            Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null)
        {
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            StringBuilder sb = new StringBuilder();
            DataTable dtToReturn = null;
            string additionparameter = "";

            try
            {
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));
                sqlparams.Add(new SqlParameter("@PONumberFrom", poNoFrom));
                sqlparams.Add(new SqlParameter("@PONumberTo", poNoTo));

                if (statusCode > 0)
                    sb.Append(" AND AP_PurchaseOrderHeader.StatusCode = " + statusCode + "");

                if (DocumentType.HasValue)
                    additionparameter = " AND Set_DocumentType.DocumentTypeID = " + DocumentType.Value;

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    sb.Append(" AND AP_PurchaseOrderHeader.DocumentDate between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd")
                        + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "' ");
                }

                #region Query

                string Query_PurchaseOrderImport = @" 
Select distinct item.Description as RecipeItem, std.DocumentNo as ReqNO, AP_PurchaseOrderHeader.DocumentNumber as PONo, 
    AP_PurchaseOrderHeader.AdvanceAmountPercentage, AP_PurchaseOrderDetail.POHeaderID, AP_PurchaseOrderHeader.CancelFlag, AP_PurchaseOrderDetail.PODetailID, 
    INV_Item.Description, INV_SET_Category.CategoryName, IsNull(AP_PurchaseOrderDetail.ItemDescription, INV_Item.Description) As ItemDescription, 
    INV_Item.InventoryItemStatusCode, SET_UnitOfMeasure.UOMName, 
    case AP_PurchaseOrderHeader.RateType when 1 then 'DemandDraft' when 2 then 'Ready' when 3 then 'TelegraphicTransfer' when 4 then 'Cash' else null end as 
        PaymentMode, 
    PUR_EnquiryHeader.DeliveryDate as DeliveryPeriod, PUR_QuotationHeader.PlaceOfDelivery, AP_PurchaseOrderDetail.Quantity, AP_PurchaseOrderDetail.UnitPrice, 
    AP_PurchaseOrderHeader.DocumentDate, SET_DocumentType.DocumentType AS POType, AP_PurchaseOrderHeader.DocumentNumber, 
    PUR_QuotationHeader.DocumentNo AS QuotationNumber, Convert(Varchar(10), PUR_QuotationHeader.QuotedDate, 103) AS QuotationDate, AP_PurchaseOrderHeader.ClosedDate, 
    Isnull(
    (
        Select top 1 LocationName from SET_Location inner join AP_PurchaseOrderHeader on AP_PurchaseOrderHeader.ShipToLocationID = SET_Location.LocationID 
        where AP_PurchaseOrderHeader.CompanyID = @CompanyID And AP_PurchaseOrderHeader.PeriodID = @PeriodID And AP_PurchaseOrderHeader.Status = 1 
            And AP_PurchaseOrderHeader.IsActive = 1 And AP_PurchaseOrderHeader.DocumentNumber Between @PONumberFrom And @PONumberTo
    ), '') as ShipToLocation, 
		
		Isnull((Select top 1 LocationName from SET_Location 
			inner join AP_PurchaseOrderHeader on AP_PurchaseOrderHeader.BillToLocationID = SET_Location.LocationID
			where AP_PurchaseOrderHeader.CompanyID = @CompanyID 
			And AP_PurchaseOrderHeader.PeriodID = @PeriodID 
			And AP_PurchaseOrderHeader.Status = 1 
			And AP_PurchaseOrderHeader.IsActive = 1 
			And AP_PurchaseOrderHeader.DocumentNumber Between @PONumberFrom And @PONumberTo
		),'') as BillToLocation,
		
		(Select case 
				when AP_PurchaseOrderHeader.FreightTermsCode = 1 then 'CF'  
				when AP_PurchaseOrderHeader.FreightTermsCode = 2 then 'FOB' 
				else ' '
				 end)
				  as ShippingTerms, 
    FOBLookup.EnumeratedDescription as FOB, 
    ShipLookup.EnumeratedDescription AS ShipVia, 
    SET_Term.Description AS Terms, 
    AP_PurchaseOrderHeader.Comments, 
    IsNull(AR_SET_Tax.TaxRate, 0) As TaxRate, 
    AR_SET_Customer.CustomerName, 
    AR_SET_Customer.AddressLine1 AS VendorAddress, 
    AR_SET_Customer.ZipCode AS VendorZipCode, 
    CitySupp.CityName AS VendorCity, 
    AR_SET_Customer.PhoneNumber AS VendorPhone, 
    AR_SET_Customer.ContactNumber as VenerContactNumber,
    AR_SET_Customer.NTNNumber as VenderNTNNumber,
    AR_SET_Customer.STRNNumber as VenderSalesTaxNumber,    
    SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName AS CompanyName, 
    SET_OperatingUnit.AddressLine1 AS CompanyAddressLine1, 
    SET_OperatingUnit.AddressLine2 AS CompanyAddressLine2, 
    SET_OperatingUnit.AddressLine3 AS CompanyAddressLine3, SET_OperatingUnit.ZipCode AS CompanyZipCode, CityComp.CityName AS CompanyCity, 
    SET_OperatingUnit.PhoneNumber AS CompanyPhone, 
    SET_OperatingUnit.FaxNumber AS CompanyFax, 
	[SET_OperatingUnit].Logoimage as LogoImage  ,
    isnull(SET_OperatingUnit.NTNNumber, SET_Business.NTNNumber) as CompanyNTNNumber,
    isnull(SET_OperatingUnit.SalesTaxNumber,SET_Business.SalesTaxNumber) as CompanySalesTaxNumber,
   SET_Currency.CurrencySign, 
    SET_Currency.CurrencyName as CurrencySignature,
    ISNULL(AP_PurchaseOrderDetail.NoteToVendor, '') as NoteToVendor,
    ISNULL(AP_PurchaseOrderHeader.NoteToVendor, '') as NoteToVendorHeader
    ,ISNULL((select LookupDetail.EnumeratedDescription from LookupDetail where LookUpDetailID = AP_PurchaseOrderHeader.StatusCode 
    and LookupHeaderID = 5),'') as DocumentStatus 
--,INV_StockTransactionHeader.DocumentNo as ReqNO
,ISNULL(AP_PurchaseOrderHeader.otherTax,0)AS otherTax 
, ISNULL(AP_PurchaseOrderHeader.FreightAmount,0)AS FreightAmount
,AP_PurchaseOrderDetail.QtyRcvTolerance

From AP_PurchaseOrderDetail 
	Inner Join AP_PurchaseOrderHeader ON AP_PurchaseOrderHeader.CompanyID = AP_PurchaseOrderDetail.CompanyID 
        And AP_PurchaseOrderHeader.PeriodID = AP_PurchaseOrderDetail.PeriodID And AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID 
        And AP_PurchaseOrderHeader.Status = 1 And AP_PurchaseOrderHeader.IsActive = 1 
	Inner Join INV_Item ON INV_Item.ItemID = AP_PurchaseOrderDetail.ItemID --INV_Item.CompanyID = AP_PurchaseOrderDetail.CompanyID And 
    Inner Join INV_SET_Category On (INV_SET_Category.CategoryID = INV_Item.ItemTypeID)
	-- 
    INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = AP_PurchaseOrderHeader.CompanyID AND SET_DocumentType.PeriodID = AP_PurchaseOrderHeader.PeriodID 
		AND SET_DocumentType.DocumentTypeID = AP_PurchaseOrderHeader.TypeCode 	
    Left Join SET_UnitOfMeasure ON AP_PurchaseOrderDetail.ItemUOMID = SET_UnitOfMeasure.UOMID 
	Inner Join SET_Company ON SET_Company.CompanyId = AP_PurchaseOrderDetail.CompanyID 
	Inner Join SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId 
	Inner Join SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId 
    Inner Join SET_Currency ON SET_Currency.CurrencyID = AP_PurchaseOrderHeader.CurrencyID

	Left Join LookupDetail AS FOBLookup ON FOBLookup.LookupHeaderID = 7 And FOBLookup.LookupDetailID = AP_PurchaseOrderHeader.FOBCode 
	Left Join LookupDetail AS ShipLookup ON ShipLookup.LookupHeaderID = 10 And ShipLookup.LookupDetailID = AP_PurchaseOrderHeader.ShipViaCode 
	Left Join SET_Term ON SET_Term.TermID = AP_PurchaseOrderHeader.TermsID 
	Left Join AR_SET_Tax ON AR_SET_Tax.PeriodID = AP_PurchaseOrderDetail.PeriodID --AR_SET_Tax.CompanyID = AP_PurchaseOrderDetail.CompanyID And 
        And AR_SET_Tax.TaxID = AP_PurchaseOrderDetail.TaxID 
	Left Join AR_SET_Customer ON AR_SET_Customer.PeriodID = AP_PurchaseOrderHeader.PeriodID 
    --And AR_SET_Customer.CompanyID = AP_PurchaseOrderHeader.CompanyID 
        And AR_SET_Customer.CustomerID = AP_PurchaseOrderHeader.VendorID 
	Left Join SET_City AS CitySupp ON CitySupp.CityID = AR_SET_Customer.CityID 
	Left Join SET_City AS CityComp ON CityComp.CityID = SET_OperatingUnit.CityID 
    left join PUR_QuotationHeader ON PUR_QuotationHeader.DocumentNo = AP_PurchaseOrderHeader.VendorQuoteNumber
	and PUR_QuotationHeader.CompanyID = AP_PurchaseOrderHeader.CompanyID
	and PUR_QuotationHeader.PeriodID = AP_PurchaseOrderHeader.PeriodID
	and PUR_QuotationHeader.Status = 1

Left Join PUR_EnquiryHeader ON PUR_EnquiryHeader.EnquiryID = PUR_QuotationHeader.EnquiryHeaderID
	AND PUR_EnquiryHeader.CompanyID = PUR_QuotationHeader.CompanyID
	AND  PUR_EnquiryHeader.PeriodID = PUR_QuotationHeader.PeriodID

--Left Join INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = AP_PurchaseOrderHeader.StockRequisitionID
--	AND INV_StockTransactionHeader.CompanyID = AP_PurchaseOrderHeader.CompanyID
--	AND  INV_StockTransactionHeader.PeriodID = AP_PurchaseOrderHeader.PeriodID

inner join INV_StocktransactionHeader std on  std.StockTransactionID =  AP_PurchaseOrderHeader.StockRequisitionID
and std.CompanyID =  AP_PurchaseOrderHeader.CompanyID
and std.PeriodID =  AP_PurchaseOrderHeader.PeriodID 
 and std.status = 1
 
 left join AS_RecipeHeader on AS_RecipeHeader.RecipeID = std.ProductRecipeID
  and AS_RecipeHeader.CompanyID = std.CompanyID
and AS_RecipeHeader.Status =1 

left join INV_Item item  on item.ItemID = AS_RecipeHeader.ItemID
--and item.CompanyID = AS_RecipeHeader.CompanyID



inner join INV_StockTransactionDetail  on INV_StockTransactionDetail.StockTransactionID = std.StockTransactionID
and INV_StockTransactionDetail.CompanyID = std.CompanyID
and INV_StockTransactionDetail.PeriodID = std.PeriodID
and INV_StockTransactionDetail.Status = 1

where AP_PurchaseOrderDetail.CompanyID = @CompanyID And AP_PurchaseOrderDetail.PeriodID = @PeriodID And AP_PurchaseOrderDetail.Status = 1 
    And AP_PurchaseOrderDetail.IsActive = 1 And AP_PurchaseOrderHeader.DocumentNumber Between @PONumberFrom And @PONumberTo 
    {0}
   
   
    order by INV_Item.Description asc

";

                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_PurchaseOrderImport, sb.ToString() + additionparameter), sqlparams.ToArray());

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

        public DataTable Purchase_Order(int companyID, int periodID, int poNoFrom, int poNoTo, int statusCode, int? DocumentType, Nullable<DateTime> dateFrom = null,
            Nullable<DateTime> dateTo = null, int FromCustomer = -1, int ToCustomer = -1, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            StringBuilder sb = new StringBuilder();
            DataTable dtToReturn = null;
            string additionparameter = "";

            #region Query

            string Query_PO_Invoice = @" 
    Select distinct AP_PurchaseOrderHeader.AdvanceAmountPercentage, AP_PurchaseOrderHeader.DeliveryPeriod as DeliveryPeriod1, AP_PurchaseOrderDetail.POHeaderID, 
        AP_PurchaseOrderHeader.CancelFlag, AP_PurchaseOrderDetail.PODetailID, RTRIM(LTRIM(INV_Item.Description)) as Description, 
        RTRIM(LTRIM(INV_SET_Category.CategoryName)) as CategoryName, IsNull(AP_PurchaseOrderDetail.ItemDescription, INV_Item.Description) As ItemDescription, 
        INV_Item.InventoryItemStatusCode, SET_UnitOfMeasure.UOMName, case AP_PurchaseOrderHeader.RateType when 1 then 'DemandDraft' when 2 then 'Ready' 
            when 3 then 'TelegraphicTransfer' when 4 then 'Cash' else null end as PaymentMode, 
        PUR_EnquiryHeader.DeliveryDate as DeliveryPeriod, PUR_QuotationHeader.PlaceOfDelivery, AP_PurchaseOrderDetail.Quantity, AP_PurchaseOrderDetail.UnitPrice, 
        AP_PurchaseOrderHeader.DocumentDate, SET_DocumentType.DocumentType AS POType, AP_PurchaseOrderHeader.DocumentNumber, 
        PUR_QuotationHeader.DocumentNo AS QuotationNumber, Convert(Varchar(10), PUR_QuotationHeader.QuotedDate, 103) AS QuotationDate, 
        AP_PurchaseOrderHeader.ClosedDate, Isnull(
        (
            Select top 1 (LocationName) from SET_Location inner join AP_PurchaseOrderHeader AS POHeader on POHeader.ShipToLocationID = SET_Location.LocationID 
			where POHeader.CompanyID = @CompanyID And POHeader.PeriodID = @PeriodID And POHeader.Status = 1 And POHeader.IsActive = 1 
                And POHeader.DocumentNumber = AP_PurchaseOrderHeader.DocumentNumber And POHeader.TypeCode = AP_PurchaseOrderHeader.TypeCode
		 ), '') as ShipToLocation, 
		Isnull(
        (
            Select top 1 LocationName from SET_Location inner join AP_PurchaseOrderHeader AS POHeader on POHeader.BillToLocationID = SET_Location.LocationID 
			where POHeader.CompanyID = @CompanyID And POHeader.PeriodID = @PeriodID And POHeader.Status = 1 And POHeader.IsActive = 1 
                And POHeader.DocumentNumber = AP_PurchaseOrderHeader.DocumentNumber And POHeader.TypeCode = AP_PurchaseOrderHeader.TypeCode
		), '') as BillToLocation, 
		case when AP_PurchaseOrderHeader.FreightTermsCode = 1 then 'CF' when AP_PurchaseOrderHeader.FreightTermsCode = 2 then 'FOB' else ' ' end as ShippingTerms, 
        FOBLookup.EnumeratedDescription as FOB, ShipLookup.EnumeratedDescription AS ShipVia, SET_Term.Description AS Terms, AP_PurchaseOrderHeader.Comments, 
        IsNull(AR_SET_Tax.TaxRate, 0) As TaxRate, AR_SET_Customer.CustomerName, AR_SET_Customer.AddressLine1 AS VendorAddress, 
        AR_SET_Customer.ZipCode AS VendorZipCode, CitySupp.CityName AS VendorCity, AR_SET_Customer.PhoneNumber AS VendorPhone, 
        AR_SET_Customer.ContactNumber as VenerContactNumber, AR_SET_Customer.NTNNumber as VenderNTNNumber, AR_SET_Customer.STRNNumber as VenderSalesTaxNumber, 
        SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName AS CompanyName, SET_OperatingUnit.AddressLine1 AS CompanyAddressLine1, 
        SET_OperatingUnit.AddressLine2 AS CompanyAddressLine2, SET_OperatingUnit.AddressLine3 AS CompanyAddressLine3, SET_OperatingUnit.ZipCode AS CompanyZipCode, 
        CityComp.CityName AS CompanyCity, SET_OperatingUnit.PhoneNumber AS CompanyPhone, SET_OperatingUnit.FaxNumber AS CompanyFax, 
        SET_OperatingUnit.Logoimage as LogoImage, isnull(SET_OperatingUnit.NTNNumber, SET_Business.NTNNumber) as CompanyNTNNumber, 
        isnull(SET_OperatingUnit.SalesTaxNumber, SET_Business.SalesTaxNumber) as CompanySalesTaxNumber, SET_Currency.CurrencySign, 
        SET_Currency.CurrencyName as CurrencySignature, ISNULL(AP_PurchaseOrderDetail.NoteToVendor, '') as NoteToVendor, 
        ISNULL(AP_PurchaseOrderHeader.NoteToVendor, '') as NoteToVendorHeader, 
        ISNULL((select LookupDetail.EnumeratedDescription from LookupDetail where LookUpDetailID = AP_PurchaseOrderHeader.StatusCode and LookupHeaderID = 5), '') as 
            DocumentStatus, 
        Case When INV_StockTransactionHeader.DocumentNo IS NULL Then AP_PurchaseOrderRequisition.DocumentNo Else INV_StockTransactionHeader.DocumentNo End as ReqNO, 
        STUFF(
        (
            SELECT distinct ',' + convert(varchar(24), POR.DocumentNo) FROM AP_PurchaseOrderRequisition AS POR 
            where AP_PurchaseOrderHeader.POHeaderID = POR.POHeaderID AND AP_PurchaseOrderHeader.CompanyID = POR.CompanyID 
                AND AP_PurchaseOrderHeader.PeriodID = POR.PeriodID 
            FOR XML PATH(''), TYPE
        ).value('.', 'varchar(max)'), 1, 2, '') as ReqNOPO, 
        ISNULL(AP_PurchaseOrderHeader.otherTax, 0) AS otherTax, ISNULL(AP_PurchaseOrderHeader.FreightAmount, 0) AS FreightAmount, 
        AP_PurchaseOrderDetail.QtyRcvTolerance, AP_PurchaseOrderDetail.TaxAmount 
 ,(Case  DocumentTypesGroup.GroupName when 'Head Office-KHI' then 
         'K'+'PO' +'-'+ substring(convert(varchar(20),year(AP_PurchaseOrderHeader.DocumentDate)),3,2) + '000' +convert(varchar(24),AP_PurchaseOrderHeader.DocumentNumber)
          when null
          then '--' +' '+'PO' +'-'+ substring(convert(varchar(20),year(AP_PurchaseOrderHeader.DocumentDate)),3,2) + '000' +convert(varchar(24),AP_PurchaseOrderHeader.DocumentNumber)
          else  SUBSTRING(GroupName,1,1)+'PO' +'-'+ substring(convert(varchar(20),year(AP_PurchaseOrderHeader.DocumentDate)),3,2) + '000' +convert(varchar(24),AP_PurchaseOrderHeader.DocumentNumber) end  ) as PONumber,
        DocumentTypesGroup.Comments as BranchAddress,AP_PurchaseOrderHeader.Rate
,SET_Designation .DesignationName,UsersDetail.FullName as Username, 
isnull((AP_PurchaseOrderHeader.UpdatedByValue),AP_PurchaseOrderHeader.CreatedByValue) as PreparedBy
	,Case when AP_PurchaseOrderHeader.StatusCode=1 then isnull((
        select top 1 CreatedByValue from SET_DocumentApprove 
        where Set_DocumentApprove.DocumentTypeID = AP_PurchaseOrderHeader.TypeCode 
    and Set_DocumentApprove.DocumentNo = AP_PurchaseOrderHeader.DocumentNumber
    and Set_DocumentApprove.FiscalID = AP_PurchaseOrderHeader.FiscalID
    order by DocumentApproveID desc
        ),AP_PurchaseOrderHeader.CreatedByValue) else '' end as ApprovedBy 
,AP_PurchaseOrderDetail.SerialNo ,AP_PurchaseOrderHeader.OnNetAmount, AP_PurchaseOrderHeader.PODueDate , AP_PurchaseOrderHeader.NoteToReceiver, Set_PeriodCustom.PeriodSetName
,'Purchase Copy' as ReportType,1 as ReportNo
From AP_PurchaseOrderDetail
	Inner Join AP_PurchaseOrderHeader ON AP_PurchaseOrderHeader.CompanyID = AP_PurchaseOrderDetail.CompanyID 
        And AP_PurchaseOrderHeader.PeriodID = AP_PurchaseOrderDetail.PeriodID And AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID 
        And AP_PurchaseOrderHeader.Status = 1 And AP_PurchaseOrderHeader.IsActive = 1 
    Left Join AP_PurchaseOrderRequisition On AP_PurchaseOrderRequisition.CompanyID = AP_PurchaseOrderDetail.CompanyID 
		And AP_PurchaseOrderRequisition.PeriodID = AP_PurchaseOrderDetail.PeriodID And AP_PurchaseOrderRequisition.POHeaderID = AP_PurchaseOrderDetail.POHeaderID 
		And AP_PurchaseOrderRequisition.PODetailID = AP_PurchaseOrderDetail.PODetailID 
	Inner Join INV_Item ON INV_Item.ItemID = AP_PurchaseOrderDetail.ItemID 
    Inner Join INV_SET_Category On INV_SET_Category.CategoryID = INV_Item.ItemTypeID 
--  
    INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = AP_PurchaseOrderHeader.CompanyID AND SET_DocumentType.PeriodID = AP_PurchaseOrderHeader.PeriodID 
		AND SET_DocumentType.DocumentTypeID = AP_PurchaseOrderHeader.TypeCode AND isnull(SET_DocumentType.IsHide, 0) <> 1 
    Left Join SET_UnitOfMeasure ON AP_PurchaseOrderDetail.ItemUOMID = SET_UnitOfMeasure.UOMID 
	Inner Join SET_Company ON SET_Company.CompanyId = AP_PurchaseOrderDetail.CompanyID 
	Inner Join SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId 
	Inner Join SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId 
    Inner Join SET_Currency ON SET_Currency.CurrencyID = AP_PurchaseOrderHeader.CurrencyID 
	Left Join LookupDetail AS FOBLookup ON FOBLookup.LookupHeaderID = 7 And FOBLookup.LookupDetailID = AP_PurchaseOrderHeader.FOBCode 
	Left Join LookupDetail AS ShipLookup ON ShipLookup.LookupHeaderID = 10 And ShipLookup.LookupDetailID = AP_PurchaseOrderHeader.ShipViaCode 
	Left Join SET_Term ON SET_Term.TermID = AP_PurchaseOrderHeader.TermsID 
	Left Join AR_SET_Tax ON AR_SET_Tax.PeriodID = AP_PurchaseOrderDetail.PeriodID --AR_SET_Tax.CompanyID = AP_PurchaseOrderDetail.CompanyID And 
        And AR_SET_Tax.TaxID = AP_PurchaseOrderDetail.TaxID 
	Left Join AR_SET_Customer ON AR_SET_Customer.PeriodID = AP_PurchaseOrderHeader.PeriodID 
        --AR_SET_Customer.CompanyID = AP_PurchaseOrderHeader.CompanyID 
        And AR_SET_Customer.CustomerID = AP_PurchaseOrderHeader.VendorID 
	Left Join SET_City AS CitySupp ON CitySupp.CityID = AR_SET_Customer.CityID 
	Left Join SET_City AS CityComp ON CityComp.CityID = SET_OperatingUnit.CityID 
    left join PUR_QuotationHeader ON PUR_QuotationHeader.DocumentNo = AP_PurchaseOrderHeader.VendorQuoteNumber 
        and PUR_QuotationHeader.CompanyID = AP_PurchaseOrderHeader.CompanyID and PUR_QuotationHeader.PeriodID = AP_PurchaseOrderHeader.PeriodID 
        and PUR_QuotationHeader.Status = 1 
    Left Join PUR_EnquiryHeader ON PUR_EnquiryHeader.EnquiryID = PUR_QuotationHeader.EnquiryHeaderID AND PUR_EnquiryHeader.CompanyID = PUR_QuotationHeader.CompanyID 
        AND  PUR_EnquiryHeader.PeriodID = PUR_QuotationHeader.PeriodID 
  LEFT JOIN  DocumentTypesGroup On DocumentTypesGroup.GroupID = Set_DocumentType.GroupID 
--DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And 
    Left Join INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = AP_PurchaseOrderHeader.StockRequisitionID 
        AND INV_StockTransactionHeader.CompanyID = AP_PurchaseOrderHeader.CompanyID AND INV_StockTransactionHeader.PeriodID = AP_PurchaseOrderHeader.PeriodID 
        
inner join aspnet_Users on aspnet_Users.UserId = AP_PurchaseOrderHeader.CreatedBy
left join UsersDetail on UsersDetail.UserID = aspnet_Users.UserId
left join SET_Designation on SET_Designation.DesignationID = UsersDetail.DesignationID
inner join Set_PeriodCustom on AP_PurchaseOrderHeader.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID        
        		left Join SET_DocumentApprove on  SET_DocumentApprove.DocumentNo=AP_PurchaseOrderHeader.DocumentNumber
		AND SET_DocumentApprove.DocumentTypeID=AP_PurchaseOrderHeader.TypeCode
AND SET_DocumentApprove.ApprovalStatus='Approved'
where AP_PurchaseOrderDetail.CompanyID = @CompanyID And AP_PurchaseOrderDetail.PeriodID = @PeriodID And AP_PurchaseOrderDetail.Status = 1 
    And AP_PurchaseOrderDetail.IsActive = 1 And AP_PurchaseOrderHeader.DocumentNumber Between @PONumberFrom And @PONumberTo 
    {0} ";

            #endregion

            try
            {
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));
                sqlparams.Add(new SqlParameter("@PONumberFrom", poNoFrom));
                sqlparams.Add(new SqlParameter("@PONumberTo", poNoTo));
                sqlparams.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));

                if (statusCode > 0)
                    sb.Append(" AND AP_PurchaseOrderHeader.StatusCode = " + statusCode + "");

                if (DocumentType.HasValue)
                    additionparameter = " AND Set_DocumentType.DocumentTypeID = " + DocumentType.Value;

                if (documentGroupID.HasValue)
                    additionparameter = additionparameter + " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    sb.Append(" AND AP_PurchaseOrderHeader.DocumentDate between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '"
                        + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "' ");
                }

                if (FromCustomer != -1 && ToCustomer != -1)
                    sb.Append(" AND AR_SET_Customer.CustomerID between " + FromCustomer + " and " + ToCustomer + " ");
                else if (FromCustomer != -1)
                    sb.Append(" AND AR_SET_Customer.CustomerID = " + FromCustomer);
                else if (ToCustomer != -1)
                    sb.Append(" AND AR_SET_Customer.CustomerID <= " + FromCustomer);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_PO_Invoice, sb.ToString() + additionparameter.ToString()),
                    sqlparams.ToArray());

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


        public DataTable Purchase_Invoice(int companyID, int periodID, int poNoFrom, int poNoTo, int statusCode, int? DocumentType,int? VendorID,int FiscalID = 0)
        {
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            DataTable dtToReturn = null;
            string additionparameter = "";

            #region Query

            string Query_AR_Invoice = @"
SELECT AR_SET_Customer.CustomerName, AR_InvoiceHeader.InvoiceDate as CreateDate, AR_InvoiceHeader.Description, AR_InvoiceHeader.InvoiceNumber as DocumentNo, 
    AR_InvoiceHeader.VendorReferenceNumber AS refNo, 
    ISNULL((SELECT EnumeratedDescription FROM  LookupDetail WHERE LookupDetailID = AR_InvoiceHeader.StatusCode AND LookupHeaderID = 5), '') AS DocumentStatus, 
    AR_InvoiceHeader.InvoiceReceivedDate, SET_Term.Description AS TermsType, SET_Department.DepartmentName, SET_CostCenter.CostCenterName,
    INV_StockTransactionHeader.DocumentNo AS GRNNo,  INV_Item.Description AS ItemDescription, SET_UnitofMeasure.UOMName AS Unit, AR_InvoiceDetail.QuantityInvoiced, 
    AR_InvoiceDetail.UnitPrice, AR_InvoiceDetail.GrossAmount, AR_InvoiceDetail.TaxAmount, AR_InvoiceDetail.DiscountAmountTaken, AR_InvoiceDetail.NetAmount,  
    SET_CostCenter.CostCenterID, SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName AS CompanyName, 
    SET_OperatingUnit.AddressLine1 AS CompanyAddressLine1, SET_OperatingUnit.AddressLine2 AS CompanyAddressLine2, 
    SET_OperatingUnit.AddressLine3 AS CompanyAddressLine3, SET_OperatingUnit.ZipCode AS CompanyZipCode, CityComp.CityName AS CompanyCity, 
    SET_OperatingUnit.Logoimage as LogoImage, AP_PurchaseOrderHeader.DocumentNumber, AR_InvoiceHeader.OtherAmount as OtherTax,
    isnull(AR_InvoiceDetail.OtherTaxAmountOnNet,0) as OtherTaxAmountOnNet,
isnull((AR_InvoiceHeader.UpdatedByValue),AR_InvoiceHeader.CreatedByValue) as PreParedBy
	,Case when AR_InvoiceHeader.StatusCode=1 then isnull((
        select top 1 CreatedByValue from SET_DocumentApprove 
        where Set_DocumentApprove.DocumentTypeID = AR_InvoiceHeader.InvoiceType 
    and Set_DocumentApprove.DocumentNo = AR_InvoiceHeader.InvoiceNumber
    and Set_DocumentApprove.FiscalID = AR_InvoiceHeader.FiscalID
    order by DocumentApproveID desc
        ),AR_InvoiceHeader.CreatedByValue) else '' end as ApprovedBY
FROM AR_InvoiceHeader 
INNER JOIN AR_InvoiceDetail ON AR_InvoiceHeader.CompanyID = AR_InvoiceDetail.CompanyID AND AR_InvoiceHeader.PeriodID = AR_InvoiceDetail.PeriodID 
        AND AR_InvoiceHeader.InvoiceID = AR_InvoiceDetail.InvoiceHeaderID AND AR_InvoiceDetail.Status = 1 
    INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = AR_InvoiceHeader.CompanyID AND SET_DocumentType.PeriodID = AR_InvoiceHeader.PeriodID 
        AND SET_DocumentType.DocumentTypeID = AR_InvoiceHeader.InvoiceType 
    INNER JOIN  AR_SET_Customer ON AR_SET_Customer.PeriodID = AR_InvoiceHeader.PeriodID 
        --and AR_SET_Customer.CompanyID = AR_InvoiceHeader.CompanyID 
        AND  AR_SET_Customer.CustomerID = AR_InvoiceHeader.VendorID 
    --LEFT JOIN  GL_TransactionHeader ON GL_TransactionHeader.CompanyID = AR_InvoiceHeader.CompanyID AND GL_TransactionHeader.PeriodID = AR_InvoiceHeader.PeriodID 
      --  AND GL_TransactionHeader.DocumentTypeID = AR_InvoiceHeader.InvoiceType AND GL_TransactionHeader.DocumentNo = AR_InvoiceHeader.InvoiceNumber 
        --AND GL_TransactionHeader.Status = 1 
    LEFT  JOIN  SET_Term ON SET_Term.TermID = AR_InvoiceHeader.TermsID AND SET_Term.Status = 1 
    Inner Join SET_Company ON SET_Company.CompanyId = AR_InvoiceDetail.CompanyID 
    Inner Join SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId 
    Inner Join SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId 
    Left Join SET_City AS CityComp ON CityComp.CityID = SET_OperatingUnit.CityID 
    INNER JOIN  INV_StockTransactionHeader ON INV_StockTransactionHeader.CompanyID = AR_InvoiceDetail.CompanyID 
        AND INV_StockTransactionHeader.PeriodID = AR_InvoiceDetail.PeriodID AND INV_StockTransactionHeader.StockTransactionID = AR_InvoiceDetail.StockHeaderID 
    inner join INV_StockTransactionDetail on AR_InvoiceDetail.companyid = INV_StockTransactionDetail.companyid 
        and AR_InvoiceDetail.periodid = INV_StockTransactionDetail.periodid and AR_InvoiceDetail.StockHeaderID = INV_StockTransactionDetail.StockTransactionID 
        and AR_InvoiceDetail.StockDetailID = INV_StockTransactionDetail.TransactionDetailID 
    inner join AP_PurchaseOrderDetail on INV_StockTransactionDetail.companyid = AP_PurchaseOrderDetail.companyid 
        and INV_StockTransactionDetail.periodid = AP_PurchaseOrderDetail.periodid and INV_StockTransactionDetail.SourceHeaderID = AP_PurchaseOrderDetail.POHeaderID 
        and INV_StockTransactionDetail.SourceLineID = AP_PurchaseOrderDetail.PODetailID 
    inner join AP_PurchaseOrderHeader on AP_PurchaseOrderHeader.CompanyID = AP_PurchaseOrderDetail.CompanyID 
        and AP_PurchaseOrderHeader.PeriodID = AP_PurchaseOrderDetail.PeriodID and AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID 
    INNER JOIN  INV_Item ON INV_Item.ItemID = AR_InvoiceDetail.ItemID
                      
                      INNER JOIN  SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = AR_InvoiceDetail.ItemUnitOfMeasureID
                      LEFT JOIN  SET_Department ON INV_StockTransactionHeader.DepartmentID = SET_Department.DepartmentID
                      
                      LEFT JOIN SET_CostCenter ON INV_StockTransactionHeader.CostCenterID = SET_CostCenter.CostCenterID

inner join Set_PeriodCustom on AR_InvoiceHeader.InvoiceDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID
                      
                     
                    Where AR_InvoiceHeader.CompanyID = @CompanyID AND AR_InvoiceHeader.PeriodID = @PeriodID AND AR_InvoiceHeader.Status = 1 
                    ";

            #endregion

            try
            {
                StringBuilder sb = new StringBuilder();

                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));
                sqlparams.Add(new SqlParameter("@CurrentFiscalID", FiscalID));

                if (poNoFrom > 0 && poNoTo == 0)
                    sb.Append(" AND  AR_InvoiceHeader.InvoiceNumber = " + poNoFrom + " ");

                if (poNoFrom == 0 && poNoTo > 0)
                    sb.Append(" AND  AR_InvoiceHeader.InvoiceNumber = " + poNoTo + " ");

                if (poNoFrom > 0 && poNoTo > 0)
                    sb.Append(" AND  AR_InvoiceHeader.InvoiceNumber Between " + poNoFrom + " AND " + poNoTo + " ");

                if (statusCode > 0)
                    sb.Append(" AND AR_InvoiceHeader.StatusCode = " + statusCode + "");

                if (DocumentType > 0)
                    additionparameter = " AND Set_DocumentType.DocumentTypeID = " + DocumentType.Value;

                if (VendorID > 0)
                    sb.Append(" AND AR_InvoiceHeader.VendorID = " + VendorID + "");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_AR_Invoice + sb.ToString() + additionparameter.ToString()), sqlparams.ToArray());

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

        public bool DeleteDataByKey(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, int companyID, int periodID, short pODetailID, int pOHeaderID, string Query = "")
        {
            bool result = false;

            string Query_Delete_ByKeyForIndent = @" Update [AP_PurchaseOrderDetail] Set [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue 
        WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and {0} ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PODetailID", pODetailID);
                sqlparams[3] = new SqlParameter("@POHeaderID", pOHeaderID);
                sqlparams[4] = new SqlParameter("@Status", false);
                sqlparams[5] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[6] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[7] = new SqlParameter("@UpdatedByValue", updatedByValue);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, string.Format(Query_Delete_ByKeyForIndent, Query), sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, string.Format(Query_Delete_ByKeyForIndent, Query), sqlparams) > 0)
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

        public DataTable Get_InputRegister(int companyID, int periodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, SqlTransaction objTransaction = null)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;
            string whereFilter = string.Empty;

            #region Query

            string Query_InputRegister = @"
select AR_Invoiceheader.InvoiceDate, AR_Invoiceheader.invoicenumber, ar_set_customer.customername, ar_set_customer.addressline1, 
    isnull(ar_set_customer.strnnumber, 0) strnnumber, INV_ITEM.[description], AR_InvoiceDetail.QuantityInvoiced, AR_InvoiceDetail.UnitPrice as 'Rate', 
    ar_set_tax.taxrate, AR_InvoiceDetail.QuantityInvoiced * AR_InvoiceDetail.UnitPrice as 'ValueExSalesTax', 
    (ar_set_tax.taxrate * AR_InvoiceDetail.QuantityInvoiced * AR_InvoiceDetail.UnitPrice / 100) as 'SalesTax', 
    AR_InvoiceDetail.UnitPrice + AR_InvoiceDetail.QuantityInvoiced * AR_InvoiceDetail.UnitPrice as 'ValueIncSalesTax'
from AR_Invoiceheader 
	Inner Join AR_InvoiceDetail on AR_Invoiceheader.CompanyID = AR_InvoiceDetail.CompanyID And AR_Invoiceheader.PeriodID = AR_InvoiceDetail.PeriodID 
        And AR_Invoiceheader.InvoiceID = AR_InvoiceDetail.InvoiceHeaderID 
	Inner join ar_set_customer on AR_Invoiceheader.PeriodID = ar_set_customer.PeriodID 
        --AR_Invoiceheader.companyid = ar_set_customer.companyid 
        and AR_Invoiceheader.VendorID = ar_set_customer.CustomerID 
	Inner join INV_ITEM on AR_InvoiceDetail.itemid = INV_ITEM.itemid 
    inner join INV_StockTransactionDetail on AR_InvoiceDetail.companyid = INV_StockTransactionDetail.companyid 
        and AR_InvoiceDetail.periodid = INV_StockTransactionDetail.periodid and AR_InvoiceDetail.StockHeaderID = INV_StockTransactionDetail.StockTransactionID 
        and AR_InvoiceDetail.StockDetailID = INV_StockTransactionDetail.TransactionDetailID 
	inner join AP_PurchaseOrderDetail on INV_StockTransactionDetail.companyid = AP_PurchaseOrderDetail.companyid 
        and INV_StockTransactionDetail.periodid=AP_PurchaseOrderDetail.periodid and INV_StockTransactionDetail.SourceHeaderID = AP_PurchaseOrderDetail.POHeaderID 
        and INV_StockTransactionDetail.SourceLineID = AP_PurchaseOrderDetail.PODetailID 
	inner join ar_set_tax on AP_PurchaseOrderDetail.periodid = ar_set_tax.periodid --AP_PurchaseOrderDetail.companyid = ar_set_tax.companyid and 
        and AP_PurchaseOrderDetail.taxid=ar_set_tax.taxid
where AR_Invoiceheader.CompanyID = @CompanyID and AR_Invoiceheader.PeriodID = @PeriodID 
    {0} ";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    whereFilter = " And AR_Invoiceheader.InvoiceDate between '" + DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00' and '"
                        + DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59'";
                }
                else if (dateFrom.HasValue)
                    whereFilter = " And AR_Invoiceheader.InvoiceDate >= '" + DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00'";
                else if (dateTo.HasValue)
                    whereFilter = " And AR_Invoiceheader.InvoiceDate <= '" + DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59'";

                string query = string.Format(Query_InputRegister, whereFilter);

                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, query, sqlparams);

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

        public static DataTable GetStockRequsitionNoByPOHeaderID(int companyID, int periodID, int poHeaderID)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;
            string whereFilter = string.Empty;

            string Query_GetStockRequsitionNoByPOHeaderID = @" 
Select Distinct AP_PurchaseOrderRequisition.DocumentTypeID, AP_PurchaseOrderRequisition.DocumentNo, AP_PurchaseOrderRequisition.RequisitionHeaderID 
From AP_PurchaseOrderRequisition 
    INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.DocumentTypeID = AP_PurchaseOrderRequisition.DocumentTypeID 
        AND INV_StockTransactionHeader.DocumentNo = AP_PurchaseOrderRequisition.DocumentNo 
        AND INV_StockTransactionHeader.CompanyID = AP_PurchaseOrderRequisition.CompanyID 
        AND INV_StockTransactionHeader.PeriodID = AP_PurchaseOrderRequisition.PeriodID 
Where AP_PurchaseOrderRequisition.POHeaderID = @pOHeaderID AND AP_PurchaseOrderRequisition.CompanyID = @CompanyID AND AP_PurchaseOrderRequisition.PeriodID = @PeriodID ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@pOHeaderID", poHeaderID);

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetStockRequsitionNoByPOHeaderID, sqlparams);

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

        public static Nullable<float> Get_AvgRateForGIN(int companyID, int periodID, int itemID, SqlConnection objConnection = null)
        {
            object obj;
            Nullable<float> rateToReturn = null;

            string Query_GetAverageRateFromPO = @" 
Select Isnull(Sum(isnull(TransactionQuantity,0) * isnull(TransactionCost,0)), 0) / Isnull(Sum(TransactionQuantity), 1) As TransactionCost From INV_StockTransactionDetail 
    Inner Join INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
        AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
        AND INV_StockTransactionHeader.Status = 1 
    Inner Join Set_DocumentType on Set_DocumentType.CompanyID = Inv_StockTransactionHeader.CompanyID 
        And Set_DocumentType.PeriodID = Inv_StockTransactionHeader.PeriodID And Set_DocumentType.DocumentTypeID = Inv_StockTransactionHeader.DocumentTypeID 
    Inner Join ApplicationMenu On ApplicationMenu.MenuID = Set_DocumentType.MenuID And ApplicationMenu.MenuPath in ('/Inventory/Transaction/Stock_frm.aspx','/Inventory/Transaction/FinishedGoodsEntry.aspx') 
Where INV_StockTransactionDetail.CompanyID = @CompanyID And INV_StockTransactionDetail.PeriodID = @PeriodID And INV_StockTransactionDetail.Status = 1 
    And INV_StockTransactionDetail.ItemID = @ItemID ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@companyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ItemID", itemID);


                if (objConnection == null)
                    obj = SqlHelper.ExecuteScalar(CommandType.Text, Query_GetAverageRateFromPO, sqlparams);
                else
                    obj = SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_GetAverageRateFromPO, sqlparams);

                if (obj != null && obj != DBNull.Value)
                    rateToReturn = float.Parse(obj.ToString());
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return rateToReturn;
        }

        public static Nullable<float> Get_AvgRateForGINLocation(int companyID, int periodID, int itemID, int locationid, SqlConnection objConnection = null)
        {
            object obj;
            Nullable<float> rateToReturn = null;

            string Query_GetAverageRateFromPO = @" 
Select Isnull(Sum(isnull(TransactionQuantity,0) * isnull(TransactionCost,0)), 0) / Isnull(Sum(TransactionQuantity), 1) As TransactionCost From INV_StockTransactionDetail 
    Inner Join INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
        AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
        AND INV_StockTransactionHeader.Status = 1 
    Inner Join Set_DocumentType on Set_DocumentType.CompanyID = Inv_StockTransactionHeader.CompanyID 
        And Set_DocumentType.PeriodID = Inv_StockTransactionHeader.PeriodID And Set_DocumentType.DocumentTypeID = Inv_StockTransactionHeader.DocumentTypeID 
    Inner Join ApplicationMenu On ApplicationMenu.MenuID = Set_DocumentType.MenuID And ApplicationMenu.MenuPath in ('/Inventory/Transaction/Stock_frm.aspx','/Inventory/Transaction/FinishedGoodsEntry.aspx') 
Where INV_StockTransactionDetail.CompanyID = @CompanyID And INV_StockTransactionDetail.PeriodID = @PeriodID And INV_StockTransactionDetail.Status = 1 
    And INV_StockTransactionDetail.ItemID = @ItemID and isnull(INV_StockTransactionHeader.LocationID,INV_StockTransactionHeader.LocationID) = @locationid";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@companyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ItemID", itemID);
                sqlparams[3] = new SqlParameter("@locationid", locationid);

                if (objConnection == null)
                    obj = SqlHelper.ExecuteScalar(CommandType.Text, Query_GetAverageRateFromPO, sqlparams);
                else
                    obj = SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_GetAverageRateFromPO, sqlparams);

                if (obj != null && obj != DBNull.Value)
                    rateToReturn = float.Parse(obj.ToString());
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return rateToReturn;
        }

        public DataTable GetPurchaseOrderSummaryReport(int companyID, int periodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? poNoFrom,
            int? poNoTo, int? DocumentType, int? VendorID, short? documentGroupID = null,int CurrentFiscalID = 0)
        {
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            DataTable dtToReturn = null;
            string additionparameter = "";

            #region Query
            #endregion

            try
            {
                StringBuilder sb = new StringBuilder();

                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));
                sqlparams.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));

                if (DocumentType.HasValue)
                    additionparameter = " AND Set_DocumentType.DocumentTypeID = " + DocumentType.Value;

                if (documentGroupID.HasValue)
                    additionparameter = additionparameter + " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                if (VendorID.HasValue)
                    additionparameter = additionparameter + " And AP_PurchaseOrderHeader.VendorID = " + VendorID.Value;

                if (poNoFrom != null && poNoTo != null)
                    sb.Append(" and Convert(int,AP_PurchaseOrderHeader.DocumentNumber) between '" + poNoFrom + "' and '" + poNoTo + "'");
                else if (poNoFrom != null)
                    sb.Append(" and AP_PurchaseOrderHeader.DocumentNumber = '" + poNoFrom + "'");

                if (dateFrom != null && dateTo != null)
                    //sb.Append(" AND SAL_DispatchHeader.DispatchDate BETWEEN " + dateFrom + " AND " + dateTo + "");
                    sb.Append(" and Cast(AP_PurchaseOrderHeader.DocumentDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");
                else if (dateFrom != null)
                    // sb.Append(" AND SAL_DispatchHeader.DispatchDate >= '" + dateFrom + "'");
                    sb.Append(" and Cast(AP_PurchaseOrderHeader.DocumentDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    // sb.Append(" AND SAL_DispatchHeader.DispatchDate <= '" + dateTo + "'");
                    sb.Append(" and Cast(AP_PurchaseOrderHeader.DocumentDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");


                sb.Append(" order By Convert(int,AP_PurchaseOrderHeader.DocumentNumber)");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format( FnI.QueryResourceLibrary.AP.AP_PurchaseOrderDetail.Query_PurchaseOrderSummary + additionparameter.ToString() + sb.ToString()), sqlparams.ToArray());

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

        // for alhaj only
        public bool InsertAlhaj(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[58];
                sqlparams[0] = new SqlParameter("@PODetailID", f_PODetailID);
                sqlparams[1] = new SqlParameter("@POHeaderID", f_POHeaderID);
                sqlparams[2] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[3] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[4] = new SqlParameter("@AllowPriceOverrideFlag", f_AllowPriceOverrideFlag);

                if (f_BaseQuantity.HasValue)
                    sqlparams[5] = new SqlParameter("@BaseQuantity", f_BaseQuantity.Value);
                else
                    sqlparams[5] = new SqlParameter("@BaseQuantity", DBNull.Value);

                if (f_BaseUOMID.HasValue)
                    sqlparams[6] = new SqlParameter("@BaseUOMID", f_BaseUOMID.Value);
                else
                    sqlparams[6] = new SqlParameter("@BaseUOMID", DBNull.Value);

                if (f_CancelledDate.HasValue)
                    sqlparams[7] = new SqlParameter("@CancelledDate", f_CancelledDate.Value);
                else
                    sqlparams[7] = new SqlParameter("@CancelledDate", DBNull.Value);

                sqlparams[8] = new SqlParameter("@CancelledFlag", f_CancelledFlag);

                if (!string.IsNullOrEmpty(f_CancelledReason))
                    sqlparams[9] = new SqlParameter("@CancelledReason", f_CancelledReason);
                else
                    sqlparams[9] = new SqlParameter("@CancelledReason", DBNull.Value);

                if (f_CancelledBy.HasValue)
                    sqlparams[10] = new SqlParameter("@CancelledBy", f_CancelledBy.Value);
                else
                    sqlparams[10] = new SqlParameter("@CancelledBy", DBNull.Value);

                sqlparams[11] = new SqlParameter("@CapitalExpenseFlag", f_CapitalExpenseFlag);

                if (f_CategoryID.HasValue)
                    sqlparams[12] = new SqlParameter("@CategoryID", f_CategoryID.Value);
                else
                    sqlparams[12] = new SqlParameter("@CategoryID", DBNull.Value);

                if (f_ClosedBy.HasValue)
                    sqlparams[13] = new SqlParameter("@ClosedBy", f_ClosedBy.Value);
                else
                    sqlparams[13] = new SqlParameter("@ClosedBy", DBNull.Value);

                if (f_ClosedCode.HasValue)
                    sqlparams[14] = new SqlParameter("@ClosedCode", f_ClosedCode.Value);
                else
                    sqlparams[14] = new SqlParameter("@ClosedCode", DBNull.Value);

                if (f_ClosedDate.HasValue)
                    sqlparams[15] = new SqlParameter("@ClosedDate", f_ClosedDate.Value);
                else
                    sqlparams[15] = new SqlParameter("@ClosedDate", DBNull.Value);

                sqlparams[16] = new SqlParameter("@ClosedFlag", f_ClosedFlag);

                if (!string.IsNullOrEmpty(f_ClosedReason))
                    sqlparams[17] = new SqlParameter("@ClosedReason", f_ClosedReason);
                else
                    sqlparams[17] = new SqlParameter("@ClosedReason", DBNull.Value);

                if (f_CommittedAmount.HasValue)
                    sqlparams[18] = new SqlParameter("@CommittedAmount", f_CommittedAmount.Value);
                else
                    sqlparams[18] = new SqlParameter("@CommittedAmount", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ContractNumber))
                    sqlparams[19] = new SqlParameter("@ContractNumber", f_ContractNumber);
                else
                    sqlparams[19] = new SqlParameter("@ContractNumber", DBNull.Value);

                if (f_ExpirationDate.HasValue)
                    sqlparams[20] = new SqlParameter("@ExpirationDate", f_ExpirationDate.Value);
                else
                    sqlparams[20] = new SqlParameter("@ExpirationDate", DBNull.Value);

                if (f_FromHeaderID.HasValue)
                    sqlparams[21] = new SqlParameter("@FromHeaderID", f_FromHeaderID.Value);
                else
                    sqlparams[21] = new SqlParameter("@FromHeaderID", DBNull.Value);

                if (f_FromDetailID.HasValue)
                    sqlparams[22] = new SqlParameter("@FromDetailID", f_FromDetailID.Value);
                else
                    sqlparams[22] = new SqlParameter("@FromDetailID", DBNull.Value);

                if (f_HazardClassID.HasValue)
                    sqlparams[23] = new SqlParameter("@HazardClassID", f_HazardClassID.Value);
                else
                    sqlparams[23] = new SqlParameter("@HazardClassID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ItemDescription))
                    sqlparams[24] = new SqlParameter("@ItemDescription", f_ItemDescription);
                else
                    sqlparams[24] = new SqlParameter("@ItemDescription", DBNull.Value);

                sqlparams[25] = new SqlParameter("@ItemID", f_ItemID);

                if (f_ItemRevision.HasValue)
                    sqlparams[26] = new SqlParameter("@ItemRevision", f_ItemRevision.Value);
                else
                    sqlparams[26] = new SqlParameter("@ItemRevision", DBNull.Value);

                if (f_ItemUOMID.HasValue)
                    sqlparams[27] = new SqlParameter("@ItemUOMID", f_ItemUOMID.Value);
                else
                    sqlparams[27] = new SqlParameter("@ItemUOMID", DBNull.Value);

                if (f_ListPricePerUnit.HasValue)
                    sqlparams[28] = new SqlParameter("@ListPricePerUnit", f_ListPricePerUnit.Value);
                else
                    sqlparams[28] = new SqlParameter("@ListPricePerUnit", DBNull.Value);

                if (f_MarketPrice.HasValue)
                    sqlparams[29] = new SqlParameter("@MarketPrice", f_MarketPrice.Value);
                else
                    sqlparams[29] = new SqlParameter("@MarketPrice", DBNull.Value);

                if (f_MaxOrderQuantity.HasValue)
                    sqlparams[30] = new SqlParameter("@MaxOrderQuantity", f_MaxOrderQuantity.Value);
                else
                    sqlparams[30] = new SqlParameter("@MaxOrderQuantity", DBNull.Value);

                if (f_MinOrderQuantity.HasValue)
                    sqlparams[31] = new SqlParameter("@MinOrderQuantity", f_MinOrderQuantity.Value);
                else
                    sqlparams[31] = new SqlParameter("@MinOrderQuantity", DBNull.Value);

                if (f_MinReleaseAmount.HasValue)
                    sqlparams[32] = new SqlParameter("@MinReleaseAmount", f_MinReleaseAmount.Value);
                else
                    sqlparams[32] = new SqlParameter("@MinReleaseAmount", DBNull.Value);

                sqlparams[33] = new SqlParameter("@NegotiatedByPreparerFlag", f_NegotiatedByPreparerFlag);

                if (f_NotToExceedPrice.HasValue)
                    sqlparams[34] = new SqlParameter("@NotToExceedPrice", f_NotToExceedPrice.Value);
                else
                    sqlparams[34] = new SqlParameter("@NotToExceedPrice", DBNull.Value);

                if (!string.IsNullOrEmpty(f_NoteToVendor))
                    sqlparams[35] = new SqlParameter("@NoteToVendor", f_NoteToVendor);
                else
                    sqlparams[35] = new SqlParameter("@NoteToVendor", DBNull.Value);

                sqlparams[36] = new SqlParameter("@OverToleranceErrorFlag", f_OverToleranceErrorFlag);

                if (f_PriceTypeCode.HasValue)
                    sqlparams[37] = new SqlParameter("@PriceTypeCode", f_PriceTypeCode.Value);
                else
                    sqlparams[37] = new SqlParameter("@PriceTypeCode", DBNull.Value);

                if (f_QtyRcvTolerance.HasValue)
                    sqlparams[38] = new SqlParameter("@QtyRcvTolerance", f_QtyRcvTolerance.Value);
                else
                    sqlparams[38] = new SqlParameter("@QtyRcvTolerance", DBNull.Value);

                if (f_Quantity.HasValue)
                    sqlparams[39] = new SqlParameter("@Quantity", f_Quantity.Value);
                else
                    sqlparams[39] = new SqlParameter("@Quantity", DBNull.Value);

                if (f_QuantityCommitted.HasValue)
                    sqlparams[40] = new SqlParameter("@QuantityCommitted", f_QuantityCommitted.Value);
                else
                    sqlparams[40] = new SqlParameter("@QuantityCommitted", DBNull.Value);

                if (f_SecondaryQuantity.HasValue)
                    sqlparams[41] = new SqlParameter("@SecondaryQuantity", f_SecondaryQuantity.Value);
                else
                    sqlparams[41] = new SqlParameter("@SecondaryQuantity", DBNull.Value);

                if (f_SecondaryUOMID.HasValue)
                    sqlparams[42] = new SqlParameter("@SecondaryUOMID", f_SecondaryUOMID.Value);
                else
                    sqlparams[42] = new SqlParameter("@SecondaryUOMID", DBNull.Value);

                if (f_SerialNo.HasValue)
                    sqlparams[43] = new SqlParameter("@SerialNo", f_SerialNo.Value);
                else
                    sqlparams[43] = new SqlParameter("@SerialNo", DBNull.Value);

                if (f_TaxID.HasValue)
                    sqlparams[44] = new SqlParameter("@TaxID", f_TaxID.Value);
                else
                    sqlparams[44] = new SqlParameter("@TaxID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_TaxName))
                    sqlparams[45] = new SqlParameter("@TaxName", f_TaxName);
                else
                    sqlparams[45] = new SqlParameter("@TaxName", DBNull.Value);

                sqlparams[46] = new SqlParameter("@TaxableFlag", f_TaxableFlag);

                if (f_TransactionReasonCode.HasValue)
                    sqlparams[47] = new SqlParameter("@TransactionReasonCode", f_TransactionReasonCode.Value);
                else
                    sqlparams[47] = new SqlParameter("@TransactionReasonCode", DBNull.Value);

                if (f_UnitPrice.HasValue)
                    sqlparams[48] = new SqlParameter("@UnitPrice", f_UnitPrice.Value);
                else
                    sqlparams[48] = new SqlParameter("@UnitPrice", DBNull.Value);

                sqlparams[49] = new SqlParameter("@UnorderedFlag", f_UnorderedFlag);

                if (!string.IsNullOrEmpty(f_VendorProductNumber))
                    sqlparams[50] = new SqlParameter("@VendorProductNumber", f_VendorProductNumber);
                else
                    sqlparams[50] = new SqlParameter("@VendorProductNumber", DBNull.Value);

                sqlparams[51] = new SqlParameter("@Status", f_Status);
                sqlparams[52] = new SqlParameter("@CreatedBy", f_CreatedBy);
                sqlparams[53] = new SqlParameter("@CreateDate", f_CreateDate);
                sqlparams[54] = new SqlParameter("@CreatedByValue", f_CreatedByValue);

                if (f_IsActive.HasValue)
                    sqlparams[55] = new SqlParameter("@IsActive", f_IsActive.Value);
                else
                    sqlparams[55] = new SqlParameter("@IsActive", DBNull.Value);


                sqlparams[56] = new SqlParameter("@taxAmount", f_taxAmount);

                sqlparams[57] = new SqlParameter("@NetAmount", f_NetAmount);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Insert, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Insert, true, sqlparams) > 0)
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


        public static bool Update_CloseCodeForPO(int companyID, int periodID, long POHeaderID, long PODetailID,
            SqlTransaction objTransaction = null)
        {
            bool result = false;

            string Query_Update_CloseCodeForPO = @" update AP_PurchaseOrderDetail 
		set ClosedCode = 2
		where CompanyID = @CompanyID
		and PeriodID = @PeriodID
		and POHeaderID = @POHeaderID
		and PODetailID = @PODetailID";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@POHeaderID", POHeaderID);
                sqlparams[3] = new SqlParameter("@PODetailID", PODetailID);

                if (objTransaction == null)
                    SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_CloseCodeForPO, sqlparams);
                else
                    SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_CloseCodeForPO, sqlparams);

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

            return result;
        }


        public static bool GetItemCapital(int companyID, int periodID, int? pOHeaderID, int? pODetailID, int? itemID)
        {
            DataRow dr = null;
            bool isCapital = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PODetailID", pODetailID.Value);
                sqlparams[3] = new SqlParameter("@POHeaderID", pOHeaderID.Value);
                sqlparams[4] = new SqlParameter("@itemID", itemID);

                DataSet ds = null;

                const string query = @" select CapitalExpenseFlag from AP_PurchaseOrderDetail where 
                                        companyid = @companyid and periodid = @periodid and @PODetailID = PODetailID and POHeaderID  =@POHeaderID
                                        and itemID  = @itemID";

                ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    if (ds.Tables[0].Rows[0]["CapitalExpenseFlag"].ToString() == "True")
                        isCapital = true;
                    else
                        isCapital = false;
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return isCapital;
        }

        public static DataTable Get_DataForGDAssessment(int companyID, int periodID, int PoHeaderID)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[4];
                sqlParams[0] = new SqlParameter("@PoHeaderID", PoHeaderID);
                sqlParams[1] = new SqlParameter("@CompanyID", companyID);
                sqlParams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AP.AP_PurchaseOrderDetail.Query_GetPoDetail, sb.ToString()), sqlParams);

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

        public static DataTable Get_DataForWeightAssessment(int companyID, int periodID, int PoHeaderID)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            const string Query_GetWeightAssDetail = @"
Select PODetailID,ContainerNo,StockTransactionID,SaleContractID,NoofContainers,BLNumber,GDNumber,SUM(GDAssessWeight)GDAssessWeight
,SUM(InvoiceAmount)InvoiceAmount,isnull(SUM(InvoiceWeight),0)InvoiceWeight,Rate,CreateDate,UOMName
 from
(
Select distinct AP_PurchaseOrderDetail.ContainerNo,AP_PurchaseOrderDetail.PODetailID,
--,INV_StockTransactionDetail.ItemID,Item.Description,AP_PurchaseOrderDetail.ItemUOMID as  TransactionUOMID
--,SET_UnitofMeasure.UOMName,
AP_PurchaseOrderHeader.POHeaderID as StockTransactionID,
AP_PurchaseOrderHeader.SaleContractID,
AP_PurchaseOrderHeader.NoofContainers
,AP_PurchaseOrderHeader.BLNumber,Fabric_ImportCostingHeader.Gl_Number as GDNumber,
ISNULL((Fabric_ImportCostingDetail.GDAssessWeight),0) as GDAssessWeight,
(AP_PurchaseOrderDetail.NetAmount) as InvoiceAmount,
--(AP_PurchaseOrderDetail.Quantity) as InvoiceWeight 
(SecondaryQuantity)
,case when UOMName like 'kg%' then AP_PurchaseOrderDetail.Quantity else SecondaryQuantity end as InvoiceWeight
,UOMName
,AP_PurchaseOrderHeader.Rate
,AP_PurchaseOrderHeader.CreateDate
from AP_PurchaseOrderDetail
inner join AP_PurchaseOrderHeader on AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID
	and AP_PurchaseOrderHeader.CompanyID = AP_PurchaseOrderDetail.CompanyID
	and AP_PurchaseOrderHeader.PeriodID = AP_PurchaseOrderDetail.PeriodID
	AND AP_PurchaseOrderHeader.Status=1  and AP_PurchaseOrderDetail.Status=1
	--LEFT join INV_StockTransactionHeader on INV_StockTransactionHeader.StockTransactionID = AP_PurchaseOrderHeader.S
	--AND INV_StockTransactionHeader.CompanyID = AP_PurchaseOrderHeader.CompanyID
	--AND INV_StockTransactionHeader.PeriodID = AP_PurchaseOrderHeader.PeriodID
	--AND INV_StockTransactionHeader.Status=1
	LEFT jOIN Fabric_ImportCostingHeader on Fabric_ImportCostingHeader.StockTransactionID=AP_PurchaseOrderHeader.POHeaderID
	AND Fabric_ImportCostingHeader.CompanyID=AP_PurchaseOrderHeader.CompanyID
	AND Fabric_ImportCostingHeader.PeriodID=AP_PurchaseOrderHeader.PeriodID
	AND Fabric_ImportCostingHeader.Status=1
left join Fabric_ImportCostingDetail on Fabric_ImportCostingDetail.CostingID = Fabric_ImportCostingHeader.CostingID
and Fabric_ImportCostingDetail.ItemID = AP_PurchaseOrderDetail.ItemID
and Fabric_ImportCostingDetail.Quantity = AP_PurchaseOrderDetail.Quantity
--and Fabric_ImportCostingDetail.CostingDetailID = AP_PurchaseOrderDetail.serialno
inner join SET_UnitofMeasure on SET_UnitofMeasure.UOMID = AP_PurchaseOrderDetail.ItemUOMID
 Where AP_PurchaseOrderHeader.POHeaderID=@POHeaderID
AND   AP_PurchaseOrderHeader.PeriodID=@PeriodID
AND   AP_PurchaseOrderHeader.CompanyID=@CompanyID
) as tab

 group by ContainerNo,StockTransactionID,SaleContractID,NoofContainers,BLNumber,GDNumber,Rate,PODetailID,CreateDate,UOMName

   ";



            try
            {
                SqlParameter[] sqlParams = new SqlParameter[4];
                sqlParams[0] = new SqlParameter("@PoHeaderID", PoHeaderID);
                sqlParams[1] = new SqlParameter("@CompanyID", companyID);
                sqlParams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetWeightAssDetail, sb.ToString()), sqlParams);

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
        public static DataTable GetDataByKeyTable_ForImportDocumentsDetails_AlImdad(int POHeaderID, int companyID, int periodID, bool status = true, bool recipeitems = false)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            if (recipeitems)
                sb.Append(" and ISNULL(AP_PurchaseOrderDetail.NetAmount,0) <> 0 ");

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@POHeaderID", POHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", status);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AP.AP_PurchaseOrderDetail.Query_GetImportDocumentDetailForAlImdad, sb.ToString()), sqlparams);

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


        public static DataTable GetDataForLCByKeyTable(int pOHeaderID, int companyID, int periodID, string AllowedPeriods, bool status = true, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            #region Query

            string Query_Select_By_POHeaderID_CompanyId = @" 
SELECT AP_PurchaseOrderDetail.PODetailID as LCDetailID, AP_PurchaseOrderDetail.POHeaderID as LCHeaderID, Cast(Row_Number() over (Order By AP_PurchaseOrderDetail.POHeaderID) AS SmallInt) as RowUniqueID, 
    AP_PurchaseOrderDetail.CompanyID, AP_PurchaseOrderDetail.PeriodID, AP_PurchaseOrderDetail.AllowPriceOverrideFlag, AP_PurchaseOrderDetail.BaseQuantity, 
    AP_PurchaseOrderDetail.BaseUOMID, AP_PurchaseOrderDetail.CancelledDate, AP_PurchaseOrderDetail.CancelledFlag, AP_PurchaseOrderDetail.CancelledReason, 
    AP_PurchaseOrderDetail.CancelledBy, AP_PurchaseOrderDetail.CapitalExpenseFlag, AP_PurchaseOrderDetail.CategoryID, AP_PurchaseOrderDetail.ClosedBy, 
    AP_PurchaseOrderDetail.ClosedCode, AP_PurchaseOrderDetail.ClosedDate, AP_PurchaseOrderDetail.ClosedFlag, AP_PurchaseOrderDetail.ClosedReason, 
    AP_PurchaseOrderDetail.CommittedAmount, AP_PurchaseOrderDetail.ContractNumber, AP_PurchaseOrderDetail.ExpirationDate, AP_PurchaseOrderDetail.FromHeaderID, 
    AP_PurchaseOrderDetail.FromDetailID, AP_PurchaseOrderDetail.HazardClassID, AP_PurchaseOrderDetail.ItemDescription, AP_PurchaseOrderDetail.ItemID, 
    AP_PurchaseOrderDetail.ItemRevision, AP_PurchaseOrderDetail.ItemUOMID, AP_PurchaseOrderDetail.ListPricePerUnit, AP_PurchaseOrderDetail.MarketPrice, 
    AP_PurchaseOrderDetail.MaxOrderQuantity, AP_PurchaseOrderDetail.MinOrderQuantity, AP_PurchaseOrderDetail.MinReleaseAmount, 
    AP_PurchaseOrderDetail.NegotiatedByPreparerFlag, AP_PurchaseOrderDetail.NotToExceedPrice, AP_PurchaseOrderDetail.NoteToVendor, 
    AP_PurchaseOrderDetail.OverToleranceErrorFlag, AP_PurchaseOrderDetail.PriceTypeCode, AP_PurchaseOrderDetail.QtyRcvTolerance, AP_PurchaseOrderDetail.Quantity, 
    AP_PurchaseOrderDetail.QuantityCommitted, AP_PurchaseOrderDetail.SecondaryQuantity, AP_PurchaseOrderDetail.SecondaryUOMID, AP_PurchaseOrderDetail.SerialNo, 
    AP_PurchaseOrderDetail.TaxID, AP_PurchaseOrderDetail.TaxName, AP_PurchaseOrderDetail.TaxableFlag, AP_PurchaseOrderDetail.TransactionReasonCode, 
    AP_PurchaseOrderDetail.UnitPrice AS UnitPrice, AP_PurchaseOrderDetail.ExciseDuty, AP_PurchaseOrderDetail.ExciseDutyAmount,
AP_PurchaseOrderDetail.JobID,AP_PurchaseOrderDetail.JobCode,
    --round(AP_PurchaseOrderDetail.Quantity * AP_PurchaseOrderDetail.UnitPrice, 2) as GrossAmount, 

    round((case when 
		[AP_PurchaseOrderHeader].Rate IS NOT NULL
     then 
		[AP_PurchaseOrderDetail].[Quantity] * [AP_PurchaseOrderDetail].[UnitPrice] * [AP_PurchaseOrderHeader].Rate
    else 
		[AP_PurchaseOrderDetail].[Quantity] * [AP_PurchaseOrderDetail].[UnitPrice] 

    end
    ),2)as GrossAmount,
    
    AP_PurchaseOrderDetail.UnorderedFlag, AP_PurchaseOrderDetail.VendorProductNumber, AP_PurchaseOrderDetail.Status, AP_PurchaseOrderDetail.CreatedBy, 
    AP_PurchaseOrderDetail.CreateDate, AP_PurchaseOrderDetail.CreatedByValue, AP_PurchaseOrderDetail.UpdatedBy, AP_PurchaseOrderDetail.UpdateDate, 
    AP_PurchaseOrderDetail.UpdatedByValue, AP_PurchaseOrderDetail.IsActive, ISNULL(AP_PurchaseOrderDetail.NetAmount, 0) as NetAmount, INV_Item.Description, 
    SET_UnitofMeasure.UOMName, INV_SET_Category.CategoryName, AR_SET_Tax.TaxName AS 'TaxNames', AP_PurchaseOrderDetail.TaxAmount, 'Add' AS [Action] , 
    isnull(AP_PurchaseOrderDetail.ForexValue,0) as [ForexValue]
FROM AP_PurchaseOrderDetail 
    INNER JOIN INV_Item ON INV_Item.ItemID = AP_PurchaseOrderDetail.ItemID --AND INV_Item.CompanyID = AP_PurchaseOrderDetail.CompanyID 
    LEFT JOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = AP_PurchaseOrderDetail.ItemUOMID 
    LEFT JOIN INV_SET_Category ON INV_SET_Category.CategoryID = AP_PurchaseOrderDetail.CategoryID 
--and INV_SET_Category.CompanyID = AP_PurchaseOrderDetail.CompanyID 
    LEFT JOIN AR_SET_Tax ON AR_SET_Tax.TaxID = AP_PurchaseOrderDetail.TaxID --AND AR_SET_Tax.CompanyID = AP_PurchaseOrderDetail.CompanyID 
        AND AR_SET_Tax.PeriodID = AP_PurchaseOrderDetail.PeriodID 
    LEFT join AP_PurchaseOrderHeader on AP_PurchaseOrderHeader.poheaderid = AP_PurchaseOrderdetail.poheaderid 
and AP_PurchaseOrderHeader.companyid = AP_PurchaseOrderdetail.companyid
	and AP_PurchaseOrderHeader.PeriodID  = AP_PurchaseOrderdetail.PeriodID 
where AP_PurchaseOrderDetail.POHeaderID = @POHeaderID and AP_PurchaseOrderDetail.CompanyID = @CompanyID and AP_PurchaseOrderDetail.PeriodID in (@PeriodID)
    and AP_PurchaseOrderDetail.Status = @Status 
order by AP_PurchaseOrderDetail.SerialNo asc ";

            #endregion

            //Fiscal Year Implementation  against PO
            if (AllowedPeriods != "")
                Query_Select_By_POHeaderID_CompanyId = Query_Select_By_POHeaderID_CompanyId.Replace("@PeriodID", AllowedPeriods);






            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@POHeaderID", pOHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", status);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_POHeaderID_CompanyId, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_Select_By_POHeaderID_CompanyId, sqlparams);

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

        public static DataTable Get_InvoiceAndPaymentAmount(int companyID, int periodID, int PoHeaderID)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            const string Query = @"select Isnull(sum(AmountPaid),0) +
Isnull((select Isnull(sum(Amount),0) from AP_PaymentHeader where ImportDocumentID = @PoHeaderID and  Status = 1 and StatusCode = 1 and CompanyID = @CompanyID and PeriodID = @PeriodID),0)
as TotalInvoiceAndPaymentAmount 
from AR_InvoiceHeader 
where ImportDocumentID = @PoHeaderID and Status = 1 and StatusCode = 1 and CompanyID = @CompanyID and PeriodID = @PeriodID";
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[4];
                sqlParams[0] = new SqlParameter("@PoHeaderID", PoHeaderID);
                sqlParams[1] = new SqlParameter("@CompanyID", companyID);
                sqlParams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, sb.ToString()), sqlParams);

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


        public DataSet Purchase_OrderSKU(int companyID, int periodID, int poNoFrom, int poNoTo, int statusCode, int? DocumentType, Nullable<DateTime> dateFrom = null,
            Nullable<DateTime> dateTo = null, int FromCustomer = -1, int ToCustomer = -1, short? documentGroupID = null, int CurrentFiscalID = 0, int? DepartmentID = null,bool MultiCopies = true)
        {
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            StringBuilder sb = new StringBuilder();
            DataSet dtToReturn = null;
            string additionparameter = "";

            #region Query

            string Query_PO_Invoice = FnI.QueryResourceLibrary.AP.AP_PurchaseOrderDetail.QueryForPO_InvoiceSingle;


            if (MultiCopies)
                Query_PO_Invoice = FnI.QueryResourceLibrary.AP.AP_PurchaseOrderDetail.QueryForPO_Invoice;

            #endregion

            try
            {
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));
                sqlparams.Add(new SqlParameter("@PONumberFrom", poNoFrom));
                sqlparams.Add(new SqlParameter("@PONumberTo", poNoTo));
                sqlparams.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));

                if (statusCode > 0)
                    sb.Append(" AND AP_PurchaseOrderHeader.StatusCode = " + statusCode + "");

                if (DocumentType.HasValue)
                    additionparameter = " AND Set_DocumentType.DocumentTypeID = " + DocumentType.Value;

                if (documentGroupID.HasValue)
                    additionparameter = additionparameter + " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    sb.Append(" AND AP_PurchaseOrderHeader.DocumentDate between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '"
                        + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "' ");
                }

                if (FromCustomer != -1 && ToCustomer != -1)
                    sb.Append(" AND AR_SET_Customer.CustomerID between " + FromCustomer + " and " + ToCustomer + " ");
                else if (FromCustomer != -1)
                    sb.Append(" AND AR_SET_Customer.CustomerID = " + FromCustomer);
                else if (ToCustomer != -1)
                    sb.Append(" AND AR_SET_Customer.CustomerID <= " + FromCustomer);

                if (DepartmentID > 0)
                {
                    sb.Append(" And SET_Department.DepartmentID = " + DepartmentID);
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_PO_Invoice, sb.ToString() + additionparameter.ToString()),
                    sqlparams.ToArray());

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                {
                    ds.Tables[0].TableName = "PurchaseOrder";
                    ds.Tables[1].TableName = "PaymentSchedule";
                }

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }


        public DataTable Purchase_OrderHSM(int companyID, int periodID, int poNoFrom, int poNoTo, int statusCode, int? DocumentType, Nullable<DateTime> dateFrom = null,
            Nullable<DateTime> dateTo = null, int FromCustomer = -1, int ToCustomer = -1, short? documentGroupID = null, int CurrentFiscalID = 0, int? DepartmentID = null)
        {
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            StringBuilder sb = new StringBuilder();
            DataTable dtToReturn = null;
            string additionparameter = "";

            #region Query

            string Query_PO_Invoice = FnI.QueryResourceLibrary.AP.AP_PurchaseOrderDetail.QueryForPO_Invoice_HSM;

            #endregion

            try
            {
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));
                sqlparams.Add(new SqlParameter("@PONumberFrom", poNoFrom));
                sqlparams.Add(new SqlParameter("@PONumberTo", poNoTo));
                sqlparams.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));

                if (statusCode > 0)
                    sb.Append(" AND AP_PurchaseOrderHeader.StatusCode = " + statusCode + "");

                if (DocumentType.HasValue)
                    additionparameter = " AND Set_DocumentType.DocumentTypeID = " + DocumentType.Value;

                if (documentGroupID.HasValue)
                    additionparameter = additionparameter + " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    sb.Append(" AND AP_PurchaseOrderHeader.DocumentDate between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '"
                        + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "' ");
                }

                if (FromCustomer != -1 && ToCustomer != -1)
                    sb.Append(" AND AR_SET_Customer.CustomerID between " + FromCustomer + " and " + ToCustomer + " ");
                else if (FromCustomer != -1)
                    sb.Append(" AND AR_SET_Customer.CustomerID = " + FromCustomer);
                else if (ToCustomer != -1)
                    sb.Append(" AND AR_SET_Customer.CustomerID <= " + FromCustomer);

                if (DepartmentID > 0)
                {
                    sb.Append(" And SET_Department.DepartmentID = " + DepartmentID);
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_PO_Invoice, sb.ToString() + additionparameter.ToString()),
                    sqlparams.ToArray());

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

        public DataTable Purchase_OrderSSM(int companyID, int periodID, int poNoFrom, int poNoTo, int statusCode, int? DocumentType, Nullable<DateTime> dateFrom = null,
            Nullable<DateTime> dateTo = null, int FromCustomer = -1, int ToCustomer = -1, short? documentGroupID = null, int CurrentFiscalID = 0, int? DepartmentID = null)
        {
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            StringBuilder sb = new StringBuilder();
            DataTable dtToReturn = null;
            string additionparameter = "";

            #region Query

            string Query_For_PurchaseOrderSSMReport = FnI.QueryResourceLibrary.AP.AP_PurchaseOrderDetail.QueryForPO_Invoice_SSM;

            #endregion

            try
            {
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));
                sqlparams.Add(new SqlParameter("@PONumberFrom", poNoFrom));
                sqlparams.Add(new SqlParameter("@PONumberTo", poNoTo));
                sqlparams.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));

                if (statusCode > 0)
                    sb.Append(" AND AP_PurchaseOrderHeader.StatusCode = " + statusCode + "");

                if (DocumentType.HasValue)
                    additionparameter = " AND Set_DocumentType.DocumentTypeID = " + DocumentType.Value;

                if (documentGroupID.HasValue)
                    additionparameter = additionparameter + " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    sb.Append(" AND AP_PurchaseOrderHeader.DocumentDate between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '"
                        + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "' ");
                }

                if (FromCustomer != -1 && ToCustomer != -1)
                    sb.Append(" AND AR_SET_Customer.CustomerID between " + FromCustomer + " and " + ToCustomer + " ");
                else if (FromCustomer != -1)
                    sb.Append(" AND AR_SET_Customer.CustomerID = " + FromCustomer);
                else if (ToCustomer != -1)
                    sb.Append(" AND AR_SET_Customer.CustomerID <= " + FromCustomer);

                if (DepartmentID > 0)
                {
                    sb.Append(" And SET_Department.DepartmentID = " + DepartmentID);
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_For_PurchaseOrderSSMReport, sb.ToString() + additionparameter.ToString()),
                    sqlparams.ToArray());

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

        public DataTable Purchase_OrderJk(int companyID, int periodID, int poNoFrom, int poNoTo, int statusCode, int? DocumentType, Nullable<DateTime> dateFrom = null,
            Nullable<DateTime> dateTo = null, int FromCustomer = -1, int ToCustomer = -1, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            StringBuilder sb = new StringBuilder();
            DataTable dtToReturn = null;
            string additionparameter = "";

            #region Query

            string Query_PO_Invoice = @" 

SELECT * from 
(
    Select distinct AP_PurchaseOrderHeader.AdvanceAmountPercentage, AP_PurchaseOrderHeader.DeliveryPeriod as DeliveryPeriod1, AP_PurchaseOrderDetail.POHeaderID, 
        AP_PurchaseOrderHeader.CancelFlag, AP_PurchaseOrderDetail.PODetailID, RTRIM(LTRIM(INV_Item.Description)) as Description, 
        RTRIM(LTRIM(INV_SET_Category.CategoryName)) as CategoryName, IsNull(AP_PurchaseOrderDetail.ItemDescription, INV_Item.Description) As ItemDescription, 
        INV_Item.InventoryItemStatusCode, SET_UnitOfMeasure.UOMName,AP_PurchaseOrderDetail.JobCode, case AP_PurchaseOrderHeader.RateType when 1 then 'DemandDraft' when 2 then 'Ready' 
            when 3 then 'TelegraphicTransfer' when 4 then 'Cash' else null end as PaymentMode, 
        PUR_EnquiryHeader.DeliveryDate as DeliveryPeriod, PUR_QuotationHeader.PlaceOfDelivery, AP_PurchaseOrderDetail.Quantity, AP_PurchaseOrderDetail.UnitPrice, 
        AP_PurchaseOrderDetail.DiscountAmount, AP_PurchaseOrderHeader.DocumentDate, SET_DocumentType.DocumentType AS POType, AP_PurchaseOrderHeader.DocumentNumber, 
        PUR_QuotationHeader.DocumentNo AS QuotationNumber, Convert(Varchar(10), PUR_QuotationHeader.QuotedDate, 103) AS QuotationDate,
		AP_PurchaseOrderHeader.POReviseDate,AP_PurchaseOrderHeader.POReviseReason, AP_PurchaseOrderHeader.Subjects,
		AP_PurchaseOrderHeader.BankDetails, AP_PurchaseOrderHeader.TermsAndConditions,
		case AP_PurchaseOrderHeader.FreightTermsCode  when 1 then 'Delivered' when 2 then 'ExMill' else null end as FreightTerms,  
        AP_PurchaseOrderHeader.ClosedDate, Isnull(
        (
            Select top 1 (LocationName) from SET_Location inner join AP_PurchaseOrderHeader AS POHeader on POHeader.ShipToLocationID = SET_Location.LocationID 
			where POHeader.CompanyID = @CompanyID And POHeader.PeriodID = @PeriodID And POHeader.Status = 1 And POHeader.IsActive = 1 
                And POHeader.DocumentNumber = AP_PurchaseOrderHeader.DocumentNumber And POHeader.TypeCode = AP_PurchaseOrderHeader.TypeCode
		 ), '') as ShipToLocation, 
		Isnull(
        (
            Select top 1 LocationName from SET_Location inner join AP_PurchaseOrderHeader AS POHeader on POHeader.BillToLocationID = SET_Location.LocationID 
			where POHeader.CompanyID = @CompanyID And POHeader.PeriodID = @PeriodID And POHeader.Status = 1 And POHeader.IsActive = 1 
                And POHeader.DocumentNumber = AP_PurchaseOrderHeader.DocumentNumber And POHeader.TypeCode = AP_PurchaseOrderHeader.TypeCode
		), '') as BillToLocation, 
		case when AP_PurchaseOrderHeader.FreightTermsCode = 1 then 'CF' when AP_PurchaseOrderHeader.FreightTermsCode = 2 then 'FOB' else ' ' end as ShippingTerms, 
        FOBLookup.EnumeratedDescription as FOB, ShipLookup.EnumeratedDescription AS ShipVia, SET_Term.Description AS Terms, AP_PurchaseOrderHeader.Comments, 
        IsNull(AR_SET_Tax.TaxRate, 0) As TaxRate, AR_SET_Customer.CustomerName, AR_SET_Customer.AddressLine1 AS VendorAddress, 
        AR_SET_Customer.ZipCode AS VendorZipCode, CitySupp.CityName AS VendorCity, AR_SET_Customer.PhoneNumber AS VendorPhone, 
        AR_SET_Customer.ContactNumber as VenerContactNumber, AR_SET_Customer.NTNNumber as VenderNTNNumber, AR_SET_Customer.STRNNumber as VenderSalesTaxNumber,
		AR_SET_Customer.EmailAddress, SET_Business.AddressLine1 AS BusinessAddress,
        SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName AS CompanyName, SET_OperatingUnit.AddressLine1 AS CompanyAddressLine1, 
        SET_OperatingUnit.AddressLine2 AS CompanyAddressLine2, SET_OperatingUnit.AddressLine3 AS CompanyAddressLine3, SET_OperatingUnit.ZipCode AS CompanyZipCode, 
        CityComp.CityName AS CompanyCity, SET_OperatingUnit.PhoneNumber AS CompanyPhone, SET_OperatingUnit.FaxNumber AS CompanyFax, 
        SET_OperatingUnit.Logoimage as LogoImage, isnull(SET_OperatingUnit.NTNNumber, SET_Business.NTNNumber) as CompanyNTNNumber, 
        isnull(SET_OperatingUnit.SalesTaxNumber, SET_Business.SalesTaxNumber) as CompanySalesTaxNumber, SET_Currency.CurrencySign, 
        SET_Currency.CurrencyName as CurrencySignature, ISNULL(AP_PurchaseOrderDetail.NoteToVendor, '') as NoteToVendor, 
        ISNULL(AP_PurchaseOrderHeader.NoteToVendor, '') as NoteToVendorHeader, 
        ISNULL((select LookupDetail.EnumeratedDescription from LookupDetail where LookUpDetailID = AP_PurchaseOrderHeader.StatusCode and LookupHeaderID = 5), '') as 
            DocumentStatus, 
        Case When INV_StockTransactionHeader.DocumentNo IS NULL Then AP_PurchaseOrderRequisition.DocumentNo Else INV_StockTransactionHeader.DocumentNo End as ReqNO, 
        STUFF(
        (
            SELECT distinct ',' + convert(varchar(24), POR.DocumentNo) FROM AP_PurchaseOrderRequisition AS POR 
            where AP_PurchaseOrderHeader.POHeaderID = POR.POHeaderID AND AP_PurchaseOrderHeader.CompanyID = POR.CompanyID 
                AND AP_PurchaseOrderHeader.PeriodID = POR.PeriodID 
            FOR XML PATH(''), TYPE
        ).value('.', 'varchar(max)'), 1, 2, '') as ReqNOPO, 
        ISNULL(AP_PurchaseOrderDetail.OtherTaxAmount, 0) AS otherTax, ISNULL(AP_PurchaseOrderHeader.FreightAmount, 0) AS FreightAmount, 
        AP_PurchaseOrderDetail.QtyRcvTolerance, AP_PurchaseOrderDetail.TaxAmount 
 ,(Case  DocumentTypesGroup.GroupName when 'Head Office-KHI' then 
         'K'+'PO' +'-'+ substring(convert(varchar(20),year(AP_PurchaseOrderHeader.DocumentDate)),3,2) + '000' +convert(varchar(24),AP_PurchaseOrderHeader.DocumentNumber)
          when null
          then '--' +' '+'PO' +'-'+ substring(convert(varchar(20),year(AP_PurchaseOrderHeader.DocumentDate)),3,2) + '000' +convert(varchar(24),AP_PurchaseOrderHeader.DocumentNumber)
          else  SUBSTRING(GroupName,1,1)+'PO' +'-'+ substring(convert(varchar(20),year(AP_PurchaseOrderHeader.DocumentDate)),3,2) + '000' +convert(varchar(24),AP_PurchaseOrderHeader.DocumentNumber) end  ) as PONumber,
        DocumentTypesGroup.Comments as BranchAddress,AP_PurchaseOrderHeader.Rate
,SET_Designation .DesignationName,UsersDetail.FullName as Username, 
isnull((AP_PurchaseOrderHeader.UpdatedByValue),AP_PurchaseOrderHeader.CreatedByValue) as PreparedBy
	,Case when AP_PurchaseOrderHeader.StatusCode=1 then isnull((
        select top 1 CreatedByValue from SET_DocumentApprove 
        where Set_DocumentApprove.DocumentTypeID = AP_PurchaseOrderHeader.TypeCode 
    and Set_DocumentApprove.DocumentNo = AP_PurchaseOrderHeader.DocumentNumber
    and Set_DocumentApprove.FiscalID = AP_PurchaseOrderHeader.FiscalID
    order by DocumentApproveID desc
        ),AP_PurchaseOrderHeader.CreatedByValue) else '' end as ApprovedBy 
,AP_PurchaseOrderDetail.SerialNo ,AP_PurchaseOrderHeader.OnNetAmount, AP_PurchaseOrderHeader.PODueDate , AP_PurchaseOrderHeader.NoteToReceiver, Set_PeriodCustom.PeriodSetName
,'Purchase Copy' as ReportType,1 as ReportNo
,Set_IncoTerm.Description as IncoTerm
,Case AP_PurchaseOrderHeader.DemandType
			When 1 Then 'BMR'
			When 2 Then 'Repair Maintenance'
			When 3 Then 'General'
            When 4 Then 'Agri Related'
			Else '' End As DemandType

			,Case AP_PurchaseOrderHeader.SeasonType
			When 1 Then 'Season'
			When 2 Then 'Off - Season'
			Else '' End As SeasonType
From AP_PurchaseOrderDetail
	Inner Join AP_PurchaseOrderHeader ON AP_PurchaseOrderHeader.CompanyID = AP_PurchaseOrderDetail.CompanyID 
        And AP_PurchaseOrderHeader.PeriodID = AP_PurchaseOrderDetail.PeriodID And AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID 
        And AP_PurchaseOrderHeader.Status = 1 And AP_PurchaseOrderHeader.IsActive = 1 
    Left Join AP_PurchaseOrderRequisition On AP_PurchaseOrderRequisition.CompanyID = AP_PurchaseOrderDetail.CompanyID 
		And AP_PurchaseOrderRequisition.PeriodID = AP_PurchaseOrderDetail.PeriodID And AP_PurchaseOrderRequisition.POHeaderID = AP_PurchaseOrderDetail.POHeaderID 
		And AP_PurchaseOrderRequisition.PODetailID = AP_PurchaseOrderDetail.PODetailID 
	Inner Join INV_Item ON INV_Item.ItemID = AP_PurchaseOrderDetail.ItemID 
    Inner Join INV_SET_Category On INV_SET_Category.CategoryID = INV_Item.ItemTypeID 
--  
    INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = AP_PurchaseOrderHeader.CompanyID AND SET_DocumentType.PeriodID = AP_PurchaseOrderHeader.PeriodID 
		AND SET_DocumentType.DocumentTypeID = AP_PurchaseOrderHeader.TypeCode AND isnull(SET_DocumentType.IsHide, 0) <> 1 
    Left Join SET_UnitOfMeasure ON AP_PurchaseOrderDetail.ItemUOMID = SET_UnitOfMeasure.UOMID 
	Inner Join SET_Company ON SET_Company.CompanyId = AP_PurchaseOrderDetail.CompanyID 
	Inner Join SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId 
	Inner Join SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId 
    Inner Join SET_Currency ON SET_Currency.CurrencyID = AP_PurchaseOrderHeader.CurrencyID 
	Left Join LookupDetail AS FOBLookup ON FOBLookup.LookupHeaderID = 7 And FOBLookup.LookupDetailID = AP_PurchaseOrderHeader.FOBCode 
	Left Join LookupDetail AS ShipLookup ON ShipLookup.LookupHeaderID = 10 And ShipLookup.LookupDetailID = AP_PurchaseOrderHeader.ShipViaCode 
	Left Join SET_Term ON SET_Term.TermID = AP_PurchaseOrderHeader.TermsID 
	Left Join AR_SET_Tax ON AR_SET_Tax.PeriodID = AP_PurchaseOrderDetail.PeriodID --AR_SET_Tax.CompanyID = AP_PurchaseOrderDetail.CompanyID And 
        And AR_SET_Tax.TaxID = AP_PurchaseOrderDetail.TaxID 
	Left Join AR_SET_Customer ON AR_SET_Customer.PeriodID = AP_PurchaseOrderHeader.PeriodID 
        --AR_SET_Customer.CompanyID = AP_PurchaseOrderHeader.CompanyID 
        And AR_SET_Customer.CustomerID = AP_PurchaseOrderHeader.VendorID 
	Left Join SET_City AS CitySupp ON CitySupp.CityID = AR_SET_Customer.CityID 
	Left Join SET_City AS CityComp ON CityComp.CityID = SET_OperatingUnit.CityID 
    left join PUR_QuotationHeader ON PUR_QuotationHeader.DocumentNo = AP_PurchaseOrderHeader.VendorQuoteNumber 
        and PUR_QuotationHeader.CompanyID = AP_PurchaseOrderHeader.CompanyID and PUR_QuotationHeader.PeriodID = AP_PurchaseOrderHeader.PeriodID
		and PUR_QuotationHeader.FiscalID = AP_PurchaseOrderHeader.FiscalID 
        and PUR_QuotationHeader.Status = 1 
    Left Join PUR_EnquiryHeader ON PUR_EnquiryHeader.EnquiryID = PUR_QuotationHeader.EnquiryHeaderID AND PUR_EnquiryHeader.CompanyID = PUR_QuotationHeader.CompanyID 
        AND  PUR_EnquiryHeader.PeriodID = PUR_QuotationHeader.PeriodID 
  LEFT JOIN  DocumentTypesGroup On DocumentTypesGroup.GroupID = Set_DocumentType.GroupID 
--DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And 
    Left Join INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = AP_PurchaseOrderHeader.StockRequisitionID 
        AND INV_StockTransactionHeader.CompanyID = AP_PurchaseOrderHeader.CompanyID AND INV_StockTransactionHeader.PeriodID = AP_PurchaseOrderHeader.PeriodID 
        
inner join aspnet_Users on aspnet_Users.UserId = AP_PurchaseOrderHeader.CreatedBy
left join UsersDetail on UsersDetail.UserID = aspnet_Users.UserId
left join SET_Designation on SET_Designation.DesignationID = UsersDetail.DesignationID
inner join Set_PeriodCustom on AP_PurchaseOrderHeader.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID        
        		left Join SET_DocumentApprove on  SET_DocumentApprove.DocumentNo=AP_PurchaseOrderHeader.DocumentNumber
		AND SET_DocumentApprove.DocumentTypeID=AP_PurchaseOrderHeader.TypeCode
AND SET_DocumentApprove.ApprovalStatus='Approved'
left join Set_IncoTerm on Set_IncoTerm.IncoTermID = AP_PurchaseOrderHeader.IncoTermID
where AP_PurchaseOrderDetail.CompanyID = @CompanyID And AP_PurchaseOrderDetail.PeriodID = @PeriodID And AP_PurchaseOrderDetail.Status = 1 
    And AP_PurchaseOrderDetail.IsActive = 1 And AP_PurchaseOrderHeader.DocumentNumber Between @PONumberFrom And @PONumberTo 
    {0}

union all

Select distinct AP_PurchaseOrderHeader.AdvanceAmountPercentage, AP_PurchaseOrderHeader.DeliveryPeriod as DeliveryPeriod1, AP_PurchaseOrderDetail.POHeaderID, 
        AP_PurchaseOrderHeader.CancelFlag, AP_PurchaseOrderDetail.PODetailID, RTRIM(LTRIM(INV_Item.Description)) as Description, 
        RTRIM(LTRIM(INV_SET_Category.CategoryName)) as CategoryName, IsNull(AP_PurchaseOrderDetail.ItemDescription, INV_Item.Description) As ItemDescription, 
        INV_Item.InventoryItemStatusCode, SET_UnitOfMeasure.UOMName, AP_PurchaseOrderDetail.JobCode, case AP_PurchaseOrderHeader.RateType when 1 then 'DemandDraft' when 2 then 'Ready' 
            when 3 then 'TelegraphicTransfer' when 4 then 'Cash' else null end as PaymentMode, 
        PUR_EnquiryHeader.DeliveryDate as DeliveryPeriod, PUR_QuotationHeader.PlaceOfDelivery, AP_PurchaseOrderDetail.Quantity, AP_PurchaseOrderDetail.UnitPrice, 
        AP_PurchaseOrderDetail.DiscountAmount, AP_PurchaseOrderHeader.DocumentDate, SET_DocumentType.DocumentType AS POType, AP_PurchaseOrderHeader.DocumentNumber, 
        PUR_QuotationHeader.DocumentNo AS QuotationNumber, Convert(Varchar(10), PUR_QuotationHeader.QuotedDate, 103) AS QuotationDate,
		AP_PurchaseOrderHeader.POReviseDate,AP_PurchaseOrderHeader.POReviseReason, AP_PurchaseOrderHeader.Subjects,
		AP_PurchaseOrderHeader.BankDetails, AP_PurchaseOrderHeader.TermsAndConditions,
		case AP_PurchaseOrderHeader.FreightTermsCode  when 1 then 'Delivered' when 2 then 'ExMill' else null end as FreightTerms,   
        AP_PurchaseOrderHeader.ClosedDate, Isnull(
        (
            Select top 1 (LocationName) from SET_Location inner join AP_PurchaseOrderHeader AS POHeader on POHeader.ShipToLocationID = SET_Location.LocationID 
			where POHeader.CompanyID = @CompanyID And POHeader.PeriodID = @PeriodID And POHeader.Status = 1 And POHeader.IsActive = 1 
                And POHeader.DocumentNumber = AP_PurchaseOrderHeader.DocumentNumber And POHeader.TypeCode = AP_PurchaseOrderHeader.TypeCode
		 ), '') as ShipToLocation, 
		Isnull(
        (
            Select top 1 LocationName from SET_Location inner join AP_PurchaseOrderHeader AS POHeader on POHeader.BillToLocationID = SET_Location.LocationID 
			where POHeader.CompanyID = @CompanyID And POHeader.PeriodID = @PeriodID And POHeader.Status = 1 And POHeader.IsActive = 1 
                And POHeader.DocumentNumber = AP_PurchaseOrderHeader.DocumentNumber And POHeader.TypeCode = AP_PurchaseOrderHeader.TypeCode
		), '') as BillToLocation, 
		case when AP_PurchaseOrderHeader.FreightTermsCode = 1 then 'CF' when AP_PurchaseOrderHeader.FreightTermsCode = 2 then 'FOB' else ' ' end as ShippingTerms, 
        FOBLookup.EnumeratedDescription as FOB, ShipLookup.EnumeratedDescription AS ShipVia, SET_Term.Description AS Terms, AP_PurchaseOrderHeader.Comments, 
        IsNull(AR_SET_Tax.TaxRate, 0) As TaxRate, AR_SET_Customer.CustomerName, AR_SET_Customer.AddressLine1 AS VendorAddress, 
        AR_SET_Customer.ZipCode AS VendorZipCode, CitySupp.CityName AS VendorCity, AR_SET_Customer.PhoneNumber AS VendorPhone, 
        AR_SET_Customer.ContactNumber as VenerContactNumber, AR_SET_Customer.NTNNumber as VenderNTNNumber, AR_SET_Customer.STRNNumber as VenderSalesTaxNumber,
		AR_SET_Customer.EmailAddress, SET_Business.AddressLine1 AS BusinessAddress,
        SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName AS CompanyName, SET_OperatingUnit.AddressLine1 AS CompanyAddressLine1, 
        SET_OperatingUnit.AddressLine2 AS CompanyAddressLine2, SET_OperatingUnit.AddressLine3 AS CompanyAddressLine3, SET_OperatingUnit.ZipCode AS CompanyZipCode, 
        CityComp.CityName AS CompanyCity, SET_OperatingUnit.PhoneNumber AS CompanyPhone, SET_OperatingUnit.FaxNumber AS CompanyFax, 
        SET_OperatingUnit.Logoimage as LogoImage, isnull(SET_OperatingUnit.NTNNumber, SET_Business.NTNNumber) as CompanyNTNNumber, 
        isnull(SET_OperatingUnit.SalesTaxNumber, SET_Business.SalesTaxNumber) as CompanySalesTaxNumber, SET_Currency.CurrencySign, 
        SET_Currency.CurrencyName as CurrencySignature, ISNULL(AP_PurchaseOrderDetail.NoteToVendor, '') as NoteToVendor, 
        ISNULL(AP_PurchaseOrderHeader.NoteToVendor, '') as NoteToVendorHeader, 
        ISNULL((select LookupDetail.EnumeratedDescription from LookupDetail where LookUpDetailID = AP_PurchaseOrderHeader.StatusCode and LookupHeaderID = 5), '') as 
            DocumentStatus, 
        Case When INV_StockTransactionHeader.DocumentNo IS NULL Then AP_PurchaseOrderRequisition.DocumentNo Else INV_StockTransactionHeader.DocumentNo End as ReqNO, 
        STUFF(
        (
            SELECT distinct ',' + convert(varchar(24), POR.DocumentNo) FROM AP_PurchaseOrderRequisition AS POR 
            where AP_PurchaseOrderHeader.POHeaderID = POR.POHeaderID AND AP_PurchaseOrderHeader.CompanyID = POR.CompanyID 
                AND AP_PurchaseOrderHeader.PeriodID = POR.PeriodID 
            FOR XML PATH(''), TYPE
        ).value('.', 'varchar(max)'), 1, 2, '') as ReqNOPO, 
        ISNULL(AP_PurchaseOrderHeader.otherTax, 0) AS otherTax, ISNULL(AP_PurchaseOrderHeader.FreightAmount, 0) AS FreightAmount, 
        AP_PurchaseOrderDetail.QtyRcvTolerance, AP_PurchaseOrderDetail.TaxAmount 
 ,(Case  DocumentTypesGroup.GroupName when 'Head Office-KHI' then 
         'K'+'PO' +'-'+ substring(convert(varchar(20),year(AP_PurchaseOrderHeader.DocumentDate)),3,2) + '000' +convert(varchar(24),AP_PurchaseOrderHeader.DocumentNumber)
          when null
          then '--' +' '+'PO' +'-'+ substring(convert(varchar(20),year(AP_PurchaseOrderHeader.DocumentDate)),3,2) + '000' +convert(varchar(24),AP_PurchaseOrderHeader.DocumentNumber)
          else  SUBSTRING(GroupName,1,1)+'PO' +'-'+ substring(convert(varchar(20),year(AP_PurchaseOrderHeader.DocumentDate)),3,2) + '000' +convert(varchar(24),AP_PurchaseOrderHeader.DocumentNumber) end  ) as PONumber,
        DocumentTypesGroup.Comments as BranchAddress,AP_PurchaseOrderHeader.Rate
,SET_Designation .DesignationName,UsersDetail.FullName as Username, 
isnull((AP_PurchaseOrderHeader.UpdatedByValue),AP_PurchaseOrderHeader.CreatedByValue) as PreparedBy
	,Case when AP_PurchaseOrderHeader.StatusCode=1 then isnull((
        select top 1 CreatedByValue from SET_DocumentApprove 
        where Set_DocumentApprove.DocumentTypeID = AP_PurchaseOrderHeader.TypeCode 
    and Set_DocumentApprove.DocumentNo = AP_PurchaseOrderHeader.DocumentNumber
    and Set_DocumentApprove.FiscalID = AP_PurchaseOrderHeader.FiscalID
    order by DocumentApproveID desc
        ),AP_PurchaseOrderHeader.CreatedByValue) else '' end as ApprovedBy 
,AP_PurchaseOrderDetail.SerialNo ,AP_PurchaseOrderHeader.OnNetAmount, AP_PurchaseOrderHeader.PODueDate , AP_PurchaseOrderHeader.NoteToReceiver, Set_PeriodCustom.PeriodSetName
,'Accounts Copy' as ReportType,2 as ReportNo
,Set_IncoTerm.Description as IncoTerm
,Case AP_PurchaseOrderHeader.DemandType
			When 1 Then 'BMR'
			When 2 Then 'Repair Maintenance'
			When 3 Then 'General'
            When 4 Then 'Agri Related'
			Else '' End As DemandType

			,Case AP_PurchaseOrderHeader.SeasonType
			When 1 Then 'Season'
			When 2 Then 'Off - Season'
			Else '' End As SeasonType
From AP_PurchaseOrderDetail
	Inner Join AP_PurchaseOrderHeader ON AP_PurchaseOrderHeader.CompanyID = AP_PurchaseOrderDetail.CompanyID 
        And AP_PurchaseOrderHeader.PeriodID = AP_PurchaseOrderDetail.PeriodID And AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID 
        And AP_PurchaseOrderHeader.Status = 1 And AP_PurchaseOrderHeader.IsActive = 1 
    Left Join AP_PurchaseOrderRequisition On AP_PurchaseOrderRequisition.CompanyID = AP_PurchaseOrderDetail.CompanyID 
		And AP_PurchaseOrderRequisition.PeriodID = AP_PurchaseOrderDetail.PeriodID And AP_PurchaseOrderRequisition.POHeaderID = AP_PurchaseOrderDetail.POHeaderID 
		And AP_PurchaseOrderRequisition.PODetailID = AP_PurchaseOrderDetail.PODetailID 
	Inner Join INV_Item ON INV_Item.ItemID = AP_PurchaseOrderDetail.ItemID 
    Inner Join INV_SET_Category On INV_SET_Category.CategoryID = INV_Item.ItemTypeID 
--  
    INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = AP_PurchaseOrderHeader.CompanyID AND SET_DocumentType.PeriodID = AP_PurchaseOrderHeader.PeriodID 
		AND SET_DocumentType.DocumentTypeID = AP_PurchaseOrderHeader.TypeCode AND isnull(SET_DocumentType.IsHide, 0) <> 1 
    Left Join SET_UnitOfMeasure ON AP_PurchaseOrderDetail.ItemUOMID = SET_UnitOfMeasure.UOMID 
	Inner Join SET_Company ON SET_Company.CompanyId = AP_PurchaseOrderDetail.CompanyID 
	Inner Join SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId 
	Inner Join SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId 
    Inner Join SET_Currency ON SET_Currency.CurrencyID = AP_PurchaseOrderHeader.CurrencyID 
	Left Join LookupDetail AS FOBLookup ON FOBLookup.LookupHeaderID = 7 And FOBLookup.LookupDetailID = AP_PurchaseOrderHeader.FOBCode 
	Left Join LookupDetail AS ShipLookup ON ShipLookup.LookupHeaderID = 10 And ShipLookup.LookupDetailID = AP_PurchaseOrderHeader.ShipViaCode 
	Left Join SET_Term ON SET_Term.TermID = AP_PurchaseOrderHeader.TermsID 
	Left Join AR_SET_Tax ON AR_SET_Tax.PeriodID = AP_PurchaseOrderDetail.PeriodID --AR_SET_Tax.CompanyID = AP_PurchaseOrderDetail.CompanyID And 
        And AR_SET_Tax.TaxID = AP_PurchaseOrderDetail.TaxID 
	Left Join AR_SET_Customer ON AR_SET_Customer.PeriodID = AP_PurchaseOrderHeader.PeriodID 
        --AR_SET_Customer.CompanyID = AP_PurchaseOrderHeader.CompanyID 
        And AR_SET_Customer.CustomerID = AP_PurchaseOrderHeader.VendorID 
	Left Join SET_City AS CitySupp ON CitySupp.CityID = AR_SET_Customer.CityID 
	Left Join SET_City AS CityComp ON CityComp.CityID = SET_OperatingUnit.CityID 
    left join PUR_QuotationHeader ON PUR_QuotationHeader.DocumentNo = AP_PurchaseOrderHeader.VendorQuoteNumber 
        and PUR_QuotationHeader.CompanyID = AP_PurchaseOrderHeader.CompanyID and PUR_QuotationHeader.PeriodID = AP_PurchaseOrderHeader.PeriodID
		and PUR_QuotationHeader.FiscalID = AP_PurchaseOrderHeader.FiscalID 
        and PUR_QuotationHeader.Status = 1 
    Left Join PUR_EnquiryHeader ON PUR_EnquiryHeader.EnquiryID = PUR_QuotationHeader.EnquiryHeaderID AND PUR_EnquiryHeader.CompanyID = PUR_QuotationHeader.CompanyID 
        AND  PUR_EnquiryHeader.PeriodID = PUR_QuotationHeader.PeriodID 
  LEFT JOIN  DocumentTypesGroup On DocumentTypesGroup.GroupID = Set_DocumentType.GroupID 
--DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And 
    Left Join INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = AP_PurchaseOrderHeader.StockRequisitionID 
        AND INV_StockTransactionHeader.CompanyID = AP_PurchaseOrderHeader.CompanyID AND INV_StockTransactionHeader.PeriodID = AP_PurchaseOrderHeader.PeriodID 
        
inner join aspnet_Users on aspnet_Users.UserId = AP_PurchaseOrderHeader.CreatedBy
left join UsersDetail on UsersDetail.UserID = aspnet_Users.UserId
left join SET_Designation on SET_Designation.DesignationID = UsersDetail.DesignationID
inner join Set_PeriodCustom on AP_PurchaseOrderHeader.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID        
        		left Join SET_DocumentApprove on  SET_DocumentApprove.DocumentNo=AP_PurchaseOrderHeader.DocumentNumber
		AND SET_DocumentApprove.DocumentTypeID=AP_PurchaseOrderHeader.TypeCode
AND SET_DocumentApprove.ApprovalStatus='Approved'
left join Set_IncoTerm on Set_IncoTerm.IncoTermID = AP_PurchaseOrderHeader.IncoTermID
where AP_PurchaseOrderDetail.CompanyID = @CompanyID And AP_PurchaseOrderDetail.PeriodID = @PeriodID And AP_PurchaseOrderDetail.Status = 1 
    And AP_PurchaseOrderDetail.IsActive = 1 And AP_PurchaseOrderHeader.DocumentNumber Between @PONumberFrom And @PONumberTo 
    {0}

union all

Select distinct AP_PurchaseOrderHeader.AdvanceAmountPercentage, AP_PurchaseOrderHeader.DeliveryPeriod as DeliveryPeriod1, AP_PurchaseOrderDetail.POHeaderID, 
        AP_PurchaseOrderHeader.CancelFlag, AP_PurchaseOrderDetail.PODetailID, RTRIM(LTRIM(INV_Item.Description)) as Description, 
        RTRIM(LTRIM(INV_SET_Category.CategoryName)) as CategoryName, IsNull(AP_PurchaseOrderDetail.ItemDescription, INV_Item.Description) As ItemDescription, 
        INV_Item.InventoryItemStatusCode, SET_UnitOfMeasure.UOMName, AP_PurchaseOrderDetail.JobCode, case AP_PurchaseOrderHeader.RateType when 1 then 'DemandDraft' when 2 then 'Ready' 
            when 3 then 'TelegraphicTransfer' when 4 then 'Cash' else null end as PaymentMode, 
        PUR_EnquiryHeader.DeliveryDate as DeliveryPeriod, PUR_QuotationHeader.PlaceOfDelivery, AP_PurchaseOrderDetail.Quantity, AP_PurchaseOrderDetail.UnitPrice, 
        AP_PurchaseOrderDetail.DiscountAmount, AP_PurchaseOrderHeader.DocumentDate, SET_DocumentType.DocumentType AS POType, AP_PurchaseOrderHeader.DocumentNumber, 
        PUR_QuotationHeader.DocumentNo AS QuotationNumber, Convert(Varchar(10), PUR_QuotationHeader.QuotedDate, 103) AS QuotationDate,
		AP_PurchaseOrderHeader.POReviseDate,AP_PurchaseOrderHeader.POReviseReason, AP_PurchaseOrderHeader.Subjects,
		AP_PurchaseOrderHeader.BankDetails, AP_PurchaseOrderHeader.TermsAndConditions,
		case AP_PurchaseOrderHeader.FreightTermsCode  when 1 then 'Delivered' when 2 then 'ExMill' else null end as FreightTerms, 
        AP_PurchaseOrderHeader.ClosedDate, Isnull(
        (
            Select top 1 (LocationName) from SET_Location inner join AP_PurchaseOrderHeader AS POHeader on POHeader.ShipToLocationID = SET_Location.LocationID 
			where POHeader.CompanyID = @CompanyID And POHeader.PeriodID = @PeriodID And POHeader.Status = 1 And POHeader.IsActive = 1 
                And POHeader.DocumentNumber = AP_PurchaseOrderHeader.DocumentNumber And POHeader.TypeCode = AP_PurchaseOrderHeader.TypeCode
		 ), '') as ShipToLocation, 
		Isnull(
        (
            Select top 1 LocationName from SET_Location inner join AP_PurchaseOrderHeader AS POHeader on POHeader.BillToLocationID = SET_Location.LocationID 
			where POHeader.CompanyID = @CompanyID And POHeader.PeriodID = @PeriodID And POHeader.Status = 1 And POHeader.IsActive = 1 
                And POHeader.DocumentNumber = AP_PurchaseOrderHeader.DocumentNumber And POHeader.TypeCode = AP_PurchaseOrderHeader.TypeCode
		), '') as BillToLocation, 
		case when AP_PurchaseOrderHeader.FreightTermsCode = 1 then 'CF' when AP_PurchaseOrderHeader.FreightTermsCode = 2 then 'FOB' else ' ' end as ShippingTerms, 
        FOBLookup.EnumeratedDescription as FOB, ShipLookup.EnumeratedDescription AS ShipVia, SET_Term.Description AS Terms, AP_PurchaseOrderHeader.Comments, 
        IsNull(AR_SET_Tax.TaxRate, 0) As TaxRate, AR_SET_Customer.CustomerName, AR_SET_Customer.AddressLine1 AS VendorAddress, 
        AR_SET_Customer.ZipCode AS VendorZipCode, CitySupp.CityName AS VendorCity, AR_SET_Customer.PhoneNumber AS VendorPhone, 
        AR_SET_Customer.ContactNumber as VenerContactNumber, AR_SET_Customer.NTNNumber as VenderNTNNumber, AR_SET_Customer.STRNNumber as VenderSalesTaxNumber,
		AR_SET_Customer.EmailAddress, SET_Business.AddressLine1 AS BusinessAddress,
        SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName AS CompanyName, SET_OperatingUnit.AddressLine1 AS CompanyAddressLine1, 
        SET_OperatingUnit.AddressLine2 AS CompanyAddressLine2, SET_OperatingUnit.AddressLine3 AS CompanyAddressLine3, SET_OperatingUnit.ZipCode AS CompanyZipCode, 
        CityComp.CityName AS CompanyCity, SET_OperatingUnit.PhoneNumber AS CompanyPhone, SET_OperatingUnit.FaxNumber AS CompanyFax, 
        SET_OperatingUnit.Logoimage as LogoImage, isnull(SET_OperatingUnit.NTNNumber, SET_Business.NTNNumber) as CompanyNTNNumber, 
        isnull(SET_OperatingUnit.SalesTaxNumber, SET_Business.SalesTaxNumber) as CompanySalesTaxNumber, SET_Currency.CurrencySign, 
        SET_Currency.CurrencyName as CurrencySignature, ISNULL(AP_PurchaseOrderDetail.NoteToVendor, '') as NoteToVendor, 
        ISNULL(AP_PurchaseOrderHeader.NoteToVendor, '') as NoteToVendorHeader, 
        ISNULL((select LookupDetail.EnumeratedDescription from LookupDetail where LookUpDetailID = AP_PurchaseOrderHeader.StatusCode and LookupHeaderID = 5), '') as 
            DocumentStatus, 
        Case When INV_StockTransactionHeader.DocumentNo IS NULL Then AP_PurchaseOrderRequisition.DocumentNo Else INV_StockTransactionHeader.DocumentNo End as ReqNO, 
        STUFF(
        (
            SELECT distinct ',' + convert(varchar(24), POR.DocumentNo) FROM AP_PurchaseOrderRequisition AS POR 
            where AP_PurchaseOrderHeader.POHeaderID = POR.POHeaderID AND AP_PurchaseOrderHeader.CompanyID = POR.CompanyID 
                AND AP_PurchaseOrderHeader.PeriodID = POR.PeriodID 
            FOR XML PATH(''), TYPE
        ).value('.', 'varchar(max)'), 1, 2, '') as ReqNOPO, 
        ISNULL(AP_PurchaseOrderHeader.otherTax, 0) AS otherTax, ISNULL(AP_PurchaseOrderHeader.FreightAmount, 0) AS FreightAmount, 
        AP_PurchaseOrderDetail.QtyRcvTolerance, AP_PurchaseOrderDetail.TaxAmount 
 ,(Case  DocumentTypesGroup.GroupName when 'Head Office-KHI' then 
         'K'+'PO' +'-'+ substring(convert(varchar(20),year(AP_PurchaseOrderHeader.DocumentDate)),3,2) + '000' +convert(varchar(24),AP_PurchaseOrderHeader.DocumentNumber)
          when null
          then '--' +' '+'PO' +'-'+ substring(convert(varchar(20),year(AP_PurchaseOrderHeader.DocumentDate)),3,2) + '000' +convert(varchar(24),AP_PurchaseOrderHeader.DocumentNumber)
          else  SUBSTRING(GroupName,1,1)+'PO' +'-'+ substring(convert(varchar(20),year(AP_PurchaseOrderHeader.DocumentDate)),3,2) + '000' +convert(varchar(24),AP_PurchaseOrderHeader.DocumentNumber) end  ) as PONumber,
        DocumentTypesGroup.Comments as BranchAddress,AP_PurchaseOrderHeader.Rate
,SET_Designation .DesignationName,UsersDetail.FullName as Username, 
isnull((AP_PurchaseOrderHeader.UpdatedByValue),AP_PurchaseOrderHeader.CreatedByValue) as PreparedBy
	,Case when AP_PurchaseOrderHeader.StatusCode=1 then isnull((
        select top 1 CreatedByValue from SET_DocumentApprove 
        where Set_DocumentApprove.DocumentTypeID = AP_PurchaseOrderHeader.TypeCode 
    and Set_DocumentApprove.DocumentNo = AP_PurchaseOrderHeader.DocumentNumber
    and Set_DocumentApprove.FiscalID = AP_PurchaseOrderHeader.FiscalID
    order by DocumentApproveID desc
        ),AP_PurchaseOrderHeader.CreatedByValue) else '' end as ApprovedBy 
,AP_PurchaseOrderDetail.SerialNo ,AP_PurchaseOrderHeader.OnNetAmount, AP_PurchaseOrderHeader.PODueDate , AP_PurchaseOrderHeader.NoteToReceiver, Set_PeriodCustom.PeriodSetName
,'Payment Copy' as ReportType,3 as ReportNo
,Set_IncoTerm.Description as IncoTerm
,Case AP_PurchaseOrderHeader.DemandType
			When 1 Then 'BMR'
			When 2 Then 'Repair Maintenance'
			When 3 Then 'General'
            When 4 Then 'Agri Related'
			Else '' End As DemandType

			,Case AP_PurchaseOrderHeader.SeasonType
			When 1 Then 'Season'
			When 2 Then 'Off - Season'
			Else '' End As SeasonType
From AP_PurchaseOrderDetail
	Inner Join AP_PurchaseOrderHeader ON AP_PurchaseOrderHeader.CompanyID = AP_PurchaseOrderDetail.CompanyID 
        And AP_PurchaseOrderHeader.PeriodID = AP_PurchaseOrderDetail.PeriodID And AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID 
        And AP_PurchaseOrderHeader.Status = 1 And AP_PurchaseOrderHeader.IsActive = 1 
    Left Join AP_PurchaseOrderRequisition On AP_PurchaseOrderRequisition.CompanyID = AP_PurchaseOrderDetail.CompanyID 
		And AP_PurchaseOrderRequisition.PeriodID = AP_PurchaseOrderDetail.PeriodID And AP_PurchaseOrderRequisition.POHeaderID = AP_PurchaseOrderDetail.POHeaderID 
		And AP_PurchaseOrderRequisition.PODetailID = AP_PurchaseOrderDetail.PODetailID 
	Inner Join INV_Item ON INV_Item.ItemID = AP_PurchaseOrderDetail.ItemID 
    Inner Join INV_SET_Category On INV_SET_Category.CategoryID = INV_Item.ItemTypeID 
--  
    INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = AP_PurchaseOrderHeader.CompanyID AND SET_DocumentType.PeriodID = AP_PurchaseOrderHeader.PeriodID 
		AND SET_DocumentType.DocumentTypeID = AP_PurchaseOrderHeader.TypeCode AND isnull(SET_DocumentType.IsHide, 0) <> 1 
    Left Join SET_UnitOfMeasure ON AP_PurchaseOrderDetail.ItemUOMID = SET_UnitOfMeasure.UOMID 
	Inner Join SET_Company ON SET_Company.CompanyId = AP_PurchaseOrderDetail.CompanyID 
	Inner Join SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId 
	Inner Join SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId 
    Inner Join SET_Currency ON SET_Currency.CurrencyID = AP_PurchaseOrderHeader.CurrencyID 
	Left Join LookupDetail AS FOBLookup ON FOBLookup.LookupHeaderID = 7 And FOBLookup.LookupDetailID = AP_PurchaseOrderHeader.FOBCode 
	Left Join LookupDetail AS ShipLookup ON ShipLookup.LookupHeaderID = 10 And ShipLookup.LookupDetailID = AP_PurchaseOrderHeader.ShipViaCode 
	Left Join SET_Term ON SET_Term.TermID = AP_PurchaseOrderHeader.TermsID 
	Left Join AR_SET_Tax ON AR_SET_Tax.PeriodID = AP_PurchaseOrderDetail.PeriodID --AR_SET_Tax.CompanyID = AP_PurchaseOrderDetail.CompanyID And 
        And AR_SET_Tax.TaxID = AP_PurchaseOrderDetail.TaxID 
	Left Join AR_SET_Customer ON AR_SET_Customer.PeriodID = AP_PurchaseOrderHeader.PeriodID 
        --AR_SET_Customer.CompanyID = AP_PurchaseOrderHeader.CompanyID 
        And AR_SET_Customer.CustomerID = AP_PurchaseOrderHeader.VendorID 
	Left Join SET_City AS CitySupp ON CitySupp.CityID = AR_SET_Customer.CityID 
	Left Join SET_City AS CityComp ON CityComp.CityID = SET_OperatingUnit.CityID 
    left join PUR_QuotationHeader ON PUR_QuotationHeader.DocumentNo = AP_PurchaseOrderHeader.VendorQuoteNumber 
        and PUR_QuotationHeader.CompanyID = AP_PurchaseOrderHeader.CompanyID and PUR_QuotationHeader.PeriodID = AP_PurchaseOrderHeader.PeriodID
		and PUR_QuotationHeader.FiscalID = AP_PurchaseOrderHeader.FiscalID 
        and PUR_QuotationHeader.Status = 1 
    Left Join PUR_EnquiryHeader ON PUR_EnquiryHeader.EnquiryID = PUR_QuotationHeader.EnquiryHeaderID AND PUR_EnquiryHeader.CompanyID = PUR_QuotationHeader.CompanyID 
        AND  PUR_EnquiryHeader.PeriodID = PUR_QuotationHeader.PeriodID 
  LEFT JOIN  DocumentTypesGroup On DocumentTypesGroup.GroupID = Set_DocumentType.GroupID 
--DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And 
    Left Join INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = AP_PurchaseOrderHeader.StockRequisitionID 
        AND INV_StockTransactionHeader.CompanyID = AP_PurchaseOrderHeader.CompanyID AND INV_StockTransactionHeader.PeriodID = AP_PurchaseOrderHeader.PeriodID 
        
inner join aspnet_Users on aspnet_Users.UserId = AP_PurchaseOrderHeader.CreatedBy
left join UsersDetail on UsersDetail.UserID = aspnet_Users.UserId
left join SET_Designation on SET_Designation.DesignationID = UsersDetail.DesignationID
inner join Set_PeriodCustom on AP_PurchaseOrderHeader.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID        
        		left Join SET_DocumentApprove on  SET_DocumentApprove.DocumentNo=AP_PurchaseOrderHeader.DocumentNumber
		AND SET_DocumentApprove.DocumentTypeID=AP_PurchaseOrderHeader.TypeCode
AND SET_DocumentApprove.ApprovalStatus='Approved'
left join Set_IncoTerm on Set_IncoTerm.IncoTermID = AP_PurchaseOrderHeader.IncoTermID
where AP_PurchaseOrderDetail.CompanyID = @CompanyID And AP_PurchaseOrderDetail.PeriodID = @PeriodID And AP_PurchaseOrderDetail.Status = 1 
    And AP_PurchaseOrderDetail.IsActive = 1 And AP_PurchaseOrderHeader.DocumentNumber Between @PONumberFrom And @PONumberTo 
    {0}

) tab
order by POType,DocumentNumber,ReportNo ,tab.SerialNo asc ";

            #endregion

            try
            {
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));
                sqlparams.Add(new SqlParameter("@PONumberFrom", poNoFrom));
                sqlparams.Add(new SqlParameter("@PONumberTo", poNoTo));
                sqlparams.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));

                //if (statusCode > 0)
                //    sb.Append(" AND AP_PurchaseOrderHeader.StatusCode = " + -1 + "");

                if (DocumentType.HasValue)
                    additionparameter = " AND Set_DocumentType.DocumentTypeID = " + DocumentType.Value;

                if (documentGroupID.HasValue)
                    additionparameter = additionparameter + " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    sb.Append(" AND AP_PurchaseOrderHeader.DocumentDate between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '"
                        + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "' ");
                }

                if (FromCustomer != -1 && ToCustomer != -1)
                    sb.Append(" AND AR_SET_Customer.CustomerID between " + FromCustomer + " and " + ToCustomer + " ");
                else if (FromCustomer != -1)
                    sb.Append(" AND AR_SET_Customer.CustomerID = " + FromCustomer);
                else if (ToCustomer != -1)
                    sb.Append(" AND AR_SET_Customer.CustomerID <= " + FromCustomer);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_PO_Invoice, sb.ToString() + additionparameter.ToString()),
                    sqlparams.ToArray());

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

        public DataTable Purchase_OrderSKUImport(int companyID, int periodID, int poNoFrom, int poNoTo, int statusCode, int? DocumentType, Nullable<DateTime> dateFrom = null,
            Nullable<DateTime> dateTo = null, int FromCustomer = -1, int ToCustomer = -1, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            StringBuilder sb = new StringBuilder();
            DataTable dtToReturn = null;
            string additionparameter = "";

            try
            {
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));
                sqlparams.Add(new SqlParameter("@PONumberFrom", poNoFrom));
                sqlparams.Add(new SqlParameter("@PONumberTo", poNoTo));
                sqlparams.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));

                if (statusCode > 0)
                    sb.Append(" AND AP_SaleContractHeader.StatusCode = " + statusCode + "");

                if (DocumentType.HasValue)
                    additionparameter = " AND Set_DocumentType.DocumentTypeID = " + DocumentType.Value;

                if (documentGroupID.HasValue)
                    additionparameter = additionparameter + " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    sb.Append(" AND AP_SaleContractHeader.DocumentDate between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '"
                        + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "' ");
                }

                if (FromCustomer != -1 && ToCustomer != -1)
                    sb.Append(" AND AR_SET_Customer.CustomerID between " + FromCustomer + " and " + ToCustomer + " ");
                else if (FromCustomer != -1)
                    sb.Append(" AND AR_SET_Customer.CustomerID = " + FromCustomer);
                else if (ToCustomer != -1)
                    sb.Append(" AND AR_SET_Customer.CustomerID <= " + FromCustomer);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AP.AP_PurchaseOrderDetail.Query_Purchase_OrderSKUImport, sb.ToString() + additionparameter.ToString()),
                    sqlparams.ToArray());

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

        public static DataRow GetDataByKeyRequisitionID(int companyID, int periodID, int Headerid, int detailid)
        {
            DataRow dr = null;

            #region Query
            const string Query_GetData_byKeyID = @"

       Select * from AP_PurchaseOrderDetail 
            Inner Join AP_PurchaseOrderHeader on AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID 
                and AP_PurchaseOrderHeader.CompanyID = AP_PurchaseOrderDetail.CompanyID and AP_PurchaseOrderHeader.PeriodID = AP_PurchaseOrderDetail.PeriodID 
				inner join AP_PurchaseOrderRequisition on AP_PurchaseOrderRequisition.POHeaderID = AP_PurchaseOrderHeader.POHeaderID
				and AP_PurchaseOrderDetail.PODetailID = AP_PurchaseOrderRequisition.PODetailID
                inner join INV_Item on INV_Item.CompanyID = AP_PurchaseOrderHeader.CompanyID

				
        where AP_PurchaseOrderHeader.StockRequisitionID = @StockHeaderID and AP_PurchaseOrderHeader.CompanyID = @CompanyID 
		    and AP_PurchaseOrderRequisition.RequisitionDetailID  = @RequisitionDetailID
            and AP_PurchaseOrderHeader.PeriodID = @PeriodID 
			 AND IsNull(AP_PurchaseOrderDetail.CancelledFlag, 0) = 0 
            AND AP_PurchaseOrderDetail.Status = 1
         ";
            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@StockHeaderID", Headerid);
                sqlparams[3] = new SqlParameter("@RequisitionDetailID", detailid);

                DataSet ds = null;

                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byKeyID, sqlparams);

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


        public static DataTable Get_jobCodebyDetailID(int companyID, int periodID, int PoHeaderID, int PODetailID)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            const string Query = @"select JobID from AP_PurchaseOrderDetail where CompanyID = @CompanyID and Status = 1 and POHeaderID = @PoHeaderID and PODetailID = @PODetailID and JobID is not null";
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[4];
                sqlParams[0] = new SqlParameter("@PoHeaderID", PoHeaderID);
                sqlParams[1] = new SqlParameter("@CompanyID", companyID);
                sqlParams[2] = new SqlParameter("@PeriodID", periodID);
                sqlParams[3] = new SqlParameter("@PODetailID", PODetailID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, sb.ToString()), sqlParams);

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
