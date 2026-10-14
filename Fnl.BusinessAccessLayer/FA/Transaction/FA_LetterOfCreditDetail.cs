using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;

using Common.Utils;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.FA.Transaction
{
    public partial class FA_LetterOfCreditDetail : BaseClass
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

        public static DataTable GetDataByKeyTable(int LCHeaderID, int companyID, int periodID, string AllowedPeriods, bool status = true, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            #region Query

            string Query_Select_By_POHeaderID_CompanyId = @" SELECT FA_LetterOfCreditDetail.LCDetailID, FA_LetterOfCreditDetail.LCHeaderID, Cast(Row_Number() over (Order By FA_LetterOfCreditDetail.LCHeaderID) AS SmallInt) as RowUniqueID, 
    FA_LetterOfCreditDetail.CompanyID, FA_LetterOfCreditDetail.PeriodID, FA_LetterOfCreditDetail.AllowPriceOverrideFlag, FA_LetterOfCreditDetail.BaseQuantity, 
    FA_LetterOfCreditDetail.BaseUOMID, FA_LetterOfCreditDetail.CancelledDate, FA_LetterOfCreditDetail.CancelledFlag, FA_LetterOfCreditDetail.CancelledReason, 
    FA_LetterOfCreditDetail.CancelledBy, FA_LetterOfCreditDetail.CapitalExpenseFlag, FA_LetterOfCreditDetail.CategoryID, FA_LetterOfCreditDetail.ClosedBy, 
    FA_LetterOfCreditDetail.ClosedCode, FA_LetterOfCreditDetail.ClosedDate, FA_LetterOfCreditDetail.ClosedFlag, FA_LetterOfCreditDetail.ClosedReason, 
    FA_LetterOfCreditDetail.CommittedAmount, FA_LetterOfCreditDetail.ContractNumber, FA_LetterOfCreditDetail.ExpirationDate, FA_LetterOfCreditDetail.FromHeaderID, 
    FA_LetterOfCreditDetail.FromDetailID, FA_LetterOfCreditDetail.HazardClassID, FA_LetterOfCreditDetail.ItemDescription, FA_LetterOfCreditDetail.ItemID, 
    FA_LetterOfCreditDetail.ItemRevision, FA_LetterOfCreditDetail.ItemUOMID, FA_LetterOfCreditDetail.ListPricePerUnit, FA_LetterOfCreditDetail.MarketPrice, 
    FA_LetterOfCreditDetail.MaxOrderQuantity, FA_LetterOfCreditDetail.MinOrderQuantity, FA_LetterOfCreditDetail.MinReleaseAmount, 
    FA_LetterOfCreditDetail.NegotiatedByPreparerFlag, FA_LetterOfCreditDetail.NotToExceedPrice, FA_LetterOfCreditDetail.NoteToVendor, 
    FA_LetterOfCreditDetail.OverToleranceErrorFlag, FA_LetterOfCreditDetail.PriceTypeCode, FA_LetterOfCreditDetail.QtyRcvTolerance, FA_LetterOfCreditDetail.Quantity, 
    FA_LetterOfCreditDetail.QuantityCommitted, FA_LetterOfCreditDetail.SecondaryQuantity, FA_LetterOfCreditDetail.SecondaryUOMID, FA_LetterOfCreditDetail.SerialNo, 
    FA_LetterOfCreditDetail.TaxID, FA_LetterOfCreditDetail.TaxName, FA_LetterOfCreditDetail.TaxableFlag, FA_LetterOfCreditDetail.TransactionReasonCode, 
    FA_LetterOfCreditDetail.UnitPrice AS UnitPrice, FA_LetterOfCreditDetail.ExciseDuty, FA_LetterOfCreditDetail.ExciseDutyAmount,
FA_LetterOfCreditDetail.JobID,FA_LetterOfCreditDetail.JobCode,
    --round(FA_LetterOfCreditDetail.Quantity * FA_LetterOfCreditDetail.UnitPrice, 2) as GrossAmount, 

    round((case when 
		[AP_PurchaseOrderHeader].Rate IS NOT NULL
     then 
		[FA_LetterOfCreditDetail].[Quantity] * [FA_LetterOfCreditDetail].[UnitPrice] * [AP_PurchaseOrderHeader].Rate
    else 
		[FA_LetterOfCreditDetail].[Quantity] * [FA_LetterOfCreditDetail].[UnitPrice] 

    end
    ),2)as GrossAmount, FA_LetterOfCreditDetail.HSCode,
    
    FA_LetterOfCreditDetail.UnorderedFlag, FA_LetterOfCreditDetail.VendorProductNumber, FA_LetterOfCreditDetail.Status, FA_LetterOfCreditDetail.CreatedBy, 
    FA_LetterOfCreditDetail.CreateDate, FA_LetterOfCreditDetail.CreatedByValue, FA_LetterOfCreditDetail.UpdatedBy, FA_LetterOfCreditDetail.UpdateDate, 
    FA_LetterOfCreditDetail.UpdatedByValue, FA_LetterOfCreditDetail.IsActive, ISNULL(FA_LetterOfCreditDetail.NetAmount, 0) as NetAmount, INV_Item.Description, 
    SET_UnitofMeasure.UOMName, INV_SET_Category.CategoryName, AR_SET_Tax.TaxName AS 'TaxNames', FA_LetterOfCreditDetail.TaxAmount, '' AS [Action] , 
    isnull(FA_LetterOfCreditDetail.ForexValue,0) as [ForexValue]
FROM FA_LetterOfCreditDetail 
    INNER JOIN INV_Item ON INV_Item.ItemID = FA_LetterOfCreditDetail.ItemID --AND INV_Item.CompanyID = FA_LetterOfCreditDetail.CompanyID 
    LEFT JOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = FA_LetterOfCreditDetail.ItemUOMID 
    LEFT JOIN INV_SET_Category ON INV_SET_Category.CategoryID = FA_LetterOfCreditDetail.CategoryID 
--and INV_SET_Category.CompanyID = FA_LetterOfCreditDetail.CompanyID 
    LEFT JOIN AR_SET_Tax ON AR_SET_Tax.TaxID = FA_LetterOfCreditDetail.TaxID --AND AR_SET_Tax.CompanyID = FA_LetterOfCreditDetail.CompanyID 
        AND AR_SET_Tax.PeriodID = FA_LetterOfCreditDetail.PeriodID 
    LEFT join AP_PurchaseOrderHeader on AP_PurchaseOrderHeader.poheaderid = FA_LetterOfCreditDetail.LCHeaderID 
and AP_PurchaseOrderHeader.companyid = FA_LetterOfCreditDetail.companyid
	and AP_PurchaseOrderHeader.PeriodID  = FA_LetterOfCreditDetail.PeriodID 
where FA_LetterOfCreditDetail.LCHeaderID = @LCHeaderID and FA_LetterOfCreditDetail.CompanyID = @CompanyID and FA_LetterOfCreditDetail.PeriodID in (@PeriodID)
    and FA_LetterOfCreditDetail.Status = @Status 
order by FA_LetterOfCreditDetail.SerialNo asc 
";

            #endregion

            //Fiscal Year Implementation  against PO
            if (AllowedPeriods != "")
                Query_Select_By_POHeaderID_CompanyId = Query_Select_By_POHeaderID_CompanyId.Replace("@PeriodID", AllowedPeriods);






            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@LCHeaderID", LCHeaderID);
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

        public DataTable POStatus(int companyID, int periodID, Nullable<int> poNumberFrom, Nullable<int> poNumberTo, Nullable<int> categoryID, Nullable<int> itemID,
            Nullable<int> vendorID, Nullable<int> documentTypeID, Guid userID, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null,
            bool IsInvisibleUponStock = false, int? DocumentType = null, bool documentTypeAuthentication = false, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            string qry_docuTypeAuthenticate = string.Empty;
            string additionalParameters = string.Empty;
            string strCatLink = "";
            DataTable dtToReturn = null;

            #region Query

            string Query_POStatus = @" Select * from 
(
    Select PM.POHeaderID, PM.DocumentNumber, PM.DocumentDate, PM.VendorID, PD.PODetailID, PD.Quantity as POQty, c.CustomerName AS supplier, 
        itm.Description as ItemName, PD.ItemID, PD.ItemUOMID, uomPurchase.UOMName As PurchaseUOM, PD.UnitPrice, Stock.DocumentNo AS GRNNo, 
        Stock.TransactionDetailID as StockDetailID, Stock.TransactionQuantity as GRnQty, Stock.TransactionCost as GRNRate, Stock.DocumentDate as GRNDate, 
        invHead.InvoiceNumber, invHead.InvoiceDate, invdet.QuantityInvoiced, invdet.UnitPrice as InvRate, DocumentTypesGroup.GroupName, 
        isnull(payType.DocumentType, payType_Invoice.DocumentType) as PaymentType, isnull(paydet.Rate,0) as PayRate, isnull(paydet.Quantity,0) as PayQty, 
        isnull(paydet.GrossAmount, payHead_invoice.Amount) as PayAmount, LookupDetail.EnumeratedDescription as PoStatus, INV_SET_Category.IsInvisibleUponStockReports,invdet.InvoiceDetailID
    from AP_PurchaseOrderHeader PM 
        inner join AP_PurchaseOrderDetail PD on PM.POHeaderID = PD.POHeaderID and PM.CompanyID = PD.CompanyID and PM.PeriodID = PD.PeriodID and PD.Status = 1 
		inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = PM.TypeCode and SET_DocumentType.CompanyID = PM.CompanyID 
            and SET_DocumentType.PeriodID = PM.PeriodID 
      Left Join DocumentTypesGroup On DocumentTypesGroup.GroupID = Set_DocumentType.GroupID --DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And 
        {2} 
		inner join INV_Item itm on PD.ItemID = itm.ItemID and itm.Status = 1 --and PD.CompanyID = itm.CompanyID 
		LEFT join INV_SET_Category on INV_SET_Category.CategoryID = PD.CategoryID 
--and INV_SET_Category.CompanyID = PD.CompanyID 
        inner join SET_UnitOfMeasure uomPurchase on uomPurchase.UOMID = PD.ItemUOMID and uomPurchase.Status = 1 
        inner join AR_SET_Customer c ON C.CustomerID = PM.VendorID AND C.PeriodID = PM.PeriodID and c.Status = 1 
        INNER JOIN AR_SET_BusinessPartnerTypes bp ON bp.BusinessPartnerID = c.CustomerID --AND bp.CompanyID = c.CompanyID 
AND bp.PeriodID = c.PeriodID 
            and PM.PeriodID = PD.PeriodID 
        Left Join LookupDetail ON PM.StatusCode = LookupDetail.LookupDetailID And LookupDetail.LookupHeaderID = 5 
        Left Join 
        (
			Select StockHeader.DocumentNo, StockDetail.TransactionDetailID, StockDetail.TransactionQuantity, StockDetail.TransactionCost, StockHeader.DocumentDate, 
                StockDetail.SourceHeaderID, StockDetail.SourceLineID, StockDetail.CompanyID, StockDetail.PeriodID, StockDetail.StockTransactionID 
			From INV_StockTransactionDetail As StockDetail 
				Inner Join INV_StockTransactionHeader As StockHeader on StockHeader.StockTransactionID = StockDetail.StockTransactionID 
                    and StockHeader.CompanyID = StockDetail.CompanyID and StockHeader.PeriodID = StockDetail.PeriodID and StockHeader.Status = 1 
				Inner Join SET_DocumentType ON SET_DocumentType.CompanyID = StockHeader.CompanyID And SET_DocumentType.PeriodID = StockHeader.PeriodID 
                    And SET_DocumentType.DocumentTypeID = StockHeader.DocumentTypeID and SET_DocumentType.Status = 1 and ISNULL(SET_DocumentType.IsHide, 0) = 0 
                    And SET_DocumentType.TransactionImpact = 'Positive' 
                {2} 
			Where StockDetail.Status = 1 And StockDetail.CompanyID = @CompanyID And StockDetail.PeriodID = @PeriodID 
                And (StockDetail.ItemID = @ItemID Or @ItemID IS NULL) And (StockHeader.BusinessPartnerID = @VendorID Or @VendorID IS NULL)
		) As Stock On Pd.POHeaderID = Stock.SourceHeaderID and PD.PODetailID = Stock.SourceLineID and PM.CompanyID = Stock.CompanyID and PM.PeriodID = Stock.PeriodID 
        left join AR_InvoiceDetail invdet on invdet.CompanyID = Stock.CompanyID And invdet.PeriodID = Stock.PeriodID 
            And invdet.StockHeaderID = Stock.StockTransactionID And invdet.StockDetailID = Stock.TransactionDetailID And invdet.Status = 1 
        left join AR_InvoiceHeader invHead on invHead.CompanyID = invdet.CompanyID And invHead.PeriodID = invdet.PeriodID 
            And invHead.InvoiceID = invdet.InvoiceHeaderID And invHead.Status = 1 
        left join Ap_paymentDetail paydet on paydet.CompanyID = invdet.CompanyID And paydet.PeriodID = invdet.PeriodID 
            And paydet.InvoiceHeaderID = invdet.InvoiceHeaderID And paydet.InvoiceDetailID = invdet.InvoiceDetailID And paydet.Status = 1 
        left join AP_PaymentHeader payHead on payHead.CompanyID = paydet.CompanyID And payHead.PeriodID = paydet.PeriodID 
            And payHead.PaymentID = paydet.PaymentHeaderID And payHead.Status = 1 
        left join AP_PaymentHeader payHead_invoice on payHead_invoice.CompanyID = PM.CompanyID And payHead_invoice.PeriodID = PM.PeriodID 
            And payHead_invoice.SourceheaderID = PM.POHeaderID And payHead_invoice.Status = 1 and payHead_invoice.vendorID = PM.VendorID 
        ---------- Adavnce Payment ----------
        left join SET_DocumentType payType ON payHead.CompanyID = payType.CompanyID And payHead.PeriodID = payType.PeriodID 
            And payHead.DocumentTypeID = payType.DocumentTypeID and payType.Status = 1 and ISNULL(payType.IsHide, 0) = 0 
            
        left join SET_DocumentType payType_Invoice ON payHead_invoice.CompanyID = payType_Invoice.CompanyID And payHead_invoice.PeriodID = payType_Invoice.PeriodID 
            And payHead_invoice.DocumentTypeID = payType_Invoice.DocumentTypeID 

        inner join Set_PeriodCustom on PM.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
        and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID

    Where PM.CompanyID = @CompanyID AND PM.PeriodID = @PeriodID AND PM.Status = 1 AND (PM.TypeCode = @DocumentTypeID OR @DocumentTypeID is null) 
        AND (PD.CategoryID = @CategoryID OR @CategoryID is null) AND ISNULL(SET_DocumentType.IsHide, 0) = 0 AND (PD.ItemID = @ItemID OR @ItemID is null) 
        AND (PM.VendorID = @VendorID OR @VendorID is null) 
        {0}
        and isnull(invHead.IsReversed,0) <> 1 and invHead.ReversalID is  null
            ) as TAB 
{1}
            order by POHeaderID asc";

            #endregion

            try
            {
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));

                #region Purchase Order Header - Document Date filter

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    additionalParameters = " AND PM.DocumentDate between @FromDate AND @ToDate ";
                    sqlparams.Add(new SqlParameter("@FromDate", dateFrom.Value));
                    sqlparams.Add(new SqlParameter("@ToDate", dateTo.Value));
                }
                else if (dateFrom.HasValue)
                {
                    additionalParameters = " AND PM.DocumentDate >= @FromDate ";
                    sqlparams.Add(new SqlParameter("@FromDate", dateFrom.Value));
                }
                else if (dateTo.HasValue)
                {
                    additionalParameters = " AND PM.DocumentDate <= @ToDate ";
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

                if (documentTypeAuthentication)
                {
                    sqlparams.Add(new SqlParameter("@UserID", userID));

                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
                        AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                if (documentGroupID.HasValue)
                    additionalParameters = " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_POStatus, additionalParameters, strCatLink, qry_docuTypeAuthenticate),
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

        
        public DataTable Purchase_Order(int companyID, int periodID, int poNoFrom, int poNoTo, int statusCode, int? DocumentType, Nullable<DateTime> dateFrom = null,
            Nullable<DateTime> dateTo = null, int FromCustomer = -1, int ToCustomer = -1, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            StringBuilder sb = new StringBuilder();
            DataTable dtToReturn = null;
            string additionparameter = "";

            #region Query

            string Query_PO_Invoice = @" SELECT * from 
(
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
,SET_Designation .DesignationName,UsersDetail.FullName as Username
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
        
where AP_PurchaseOrderDetail.CompanyID = @CompanyID And AP_PurchaseOrderDetail.PeriodID = @PeriodID And AP_PurchaseOrderDetail.Status = 1 
    And AP_PurchaseOrderDetail.IsActive = 1 And AP_PurchaseOrderHeader.DocumentNumber Between @PONumberFrom And @PONumberTo 
    {0}
) tab
order by tab.Description asc ";

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
        
        #endregion
    }
}
