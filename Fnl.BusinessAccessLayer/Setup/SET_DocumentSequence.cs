using System;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using System.Text;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_DocumentSequence
    {
        #region Constant Queries

        const string Query_GetData_ByKey = @" 
SELECT SET_DocumentSequence.CompanyID, SET_DocumentSequence.DocumentTypeID, SET_DocumentSequence.Level2Description, SET_DocumentSequence.Level3Description, 
    SET_DocumentSequence.Level4Description, SET_DocumentSequence.DateValidFrom, SET_DocumentSequence.FromNo, SET_DocumentSequence.ToNo, 
    SET_DocumentSequence.CreatedBy, SET_DocumentSequence.CreateDate, SET_DocumentSequence.UpdatedBy, SET_DocumentSequence.UpdateDate, 
    SET_DocumentSequence.CreatedByValue, SET_DocumentSequence.UpdatedByValue, SET_DocumentSequence.PeriodID, SET_DocumentSequence.SerialNo, 
    SET_DocumentSequence.Status, SET_DocumentSequence.IsActive 
FROM SET_DocumentSequence 
WHERE SET_DocumentSequence.CompanyID = @CompanyID AND SET_DocumentSequence.PeriodID = @PeriodID AND SET_DocumentSequence.DocumentTypeID = @DocumentTypeID 
    AND (SET_DocumentSequence.IsActive = @IsActive OR @IsActive is null) AND SET_DocumentSequence.Status = 1 ";

        const string Query_Delete_By_DocumentType = @" DELETE FROM [SET_DocumentSequence] WHERE [DocumentTypeID] = @DocumentTypeID; ";

        const string Query_HasChildRecords = @" 
Declare @IsFixedAssetOn varchar(5);
Select @IsFixedAssetOn = FieldValue1 From Set_Parameter Where FieldName = 'IsFixedAsset';


Declare @IsVehicleFlagOn varchar(5);
Select @IsVehicleFlagOn = FieldValue1 From Set_Parameter Where FieldName = 'IsVehicle'

if(@IsFixedAssetOn = 'true')
Begin
    SELECT IsNull(SUM(tbl.DocumentNo), 0)DocumentNo, isnull(sum(tbl.HasRangeDefined), 0) HasRangeDefined, tbl.FromNo, tbl.ToNo
    FROM 
    (
	    select case when COUNT(DocumentTypeID) > 0 then 1 else 0 end HasRangeDefined, FromNo, ToNo, 0 DocumentNo
	    from SET_DocumentSequence
        where DocumentTypeID = @DocumentTypeID and CompanyID = @CompanyID and PeriodID = @PeriodID and [Status] = 1 AND IsActive = @IsActive 
        group by  FromNo,ToNo
    
        union all
    
        select 0 HasRangeDefined, 0 FromNo, 0 ToNo, MAX(MaxDocumentNo.DocumentNo) as DocumentNo From
        (
		    select ISNULL (MAX(DocumentNo),0)+1 as DocumentNo
		    FROM GL_TransactionHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID
    
		    union all
    
		    Select IsNull(MAX(DocumentNo), 0) + 1 as DocumentNo
		    FROM PUR_EnquiryHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID

		    union all
    
		    select ISNULL (MAX(CAST(DocumentNumber AS INT)),0)+1 as DocumentNo
		    FROM AP_PurchaseOrderHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and TypeCode = @DocumentTypeID

		    union all
    
		    select ISNULL (MAX(DocumentNo),0)+1 as DocumentNo
		    FROM INV_StockTransactionHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID

		    union all
    
		    Select IsNull(MAX(CAST(InvoiceNumber AS INT)), 0) + 1 as DocumentNo
		    FROM AR_InvoiceHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and InvoiceType = @DocumentTypeID

		    union all
    
		    Select IsNull(MAX(DocumentNo), 0) + 1 as DocumentNo
		    FROM AR_ReceiptTransactionHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID And DocumentTypeID = @DocumentTypeID

		    union all
    
		    select ISNULL (MAX(CAST(ContractNo AS INT)),0)+1 as DocumentNo
		    FROM SAL_OrderHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and SaleContractTypeID = @DocumentTypeID

		    union all
    
		    select ISNULL (MAX(CAST(DeliveryOrderNo AS INT)),0)+1 as DocumentNo
		    FROM SAL_DeliveryOrderHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID

		    union all
    
		    select ISNULL (MAX(CAST(DispatchNo AS INT)),0)+1 as DocumentNo
		    FROM SAL_DispatchHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID

		    union all
    
		    select ISNULL (MAX(CAST(InvoiceNumber AS INT)),0)+1 as DocumentNo
		    FROM SAL_InvoiceHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and InvoiceType = @DocumentTypeID

		    union all
    
		    Select IsNull(MAX(DocumentNo), 0) + 1 as DocumentNo
		    FROM AP_PaymentHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID
		
            union all
    
		    Select IsNull(MAX(DocumentNo), 0) + 1 as DocumentNo
		    FROM Gate_OutwardHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID

		    union all

		    Select IsNull(MAX(DocumentNo), 0) + 1 as DocumentNo
		    FROM FA_AssetTransfer
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID

            union all

		    Select IsNull(MAX(DocumentNo), 0) + 1 as DocumentNo
		    FROM SAL_ReverseHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID

            union all

            Select IsNull(MAX(DocumentNo), 0) + 1 as DocumentNo
		    FROM FA_DepreciationProcess
		    where CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID
           union all

           select ISNULL (MAX(CAST(DocumentNo AS INT)),0)+1 as DocumentNo
		    FROM INV_ItemsPriceHistory
		    where  CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID
        ) MaxDocumentNo
    ) tbl
    group by tbl.FromNo, tbl.ToNo, tbl.DocumentNo
    order by tbl.DocumentNo Desc
End
Else if(@IsVehicleFlagOn = 'true')
Begin
    SELECT IsNull(SUM(tbl.DocumentNo), 0)DocumentNo, isnull(sum(tbl.HasRangeDefined), 0) HasRangeDefined, tbl.FromNo, tbl.ToNo
    FROM 
    (
	    select case when COUNT(DocumentTypeID) > 0 then 1 else 0 end HasRangeDefined, FromNo, ToNo, 0 DocumentNo
	    from SET_DocumentSequence
        where DocumentTypeID = @DocumentTypeID and CompanyID = @CompanyID and PeriodID = @PeriodID and [Status] = 1 AND IsActive = @IsActive 
        group by  FromNo,ToNo
    
        union all
    
        select 0 HasRangeDefined, 0 FromNo, 0 ToNo, MAX(MaxDocumentNo.DocumentNo) as DocumentNo From
        (
		    select ISNULL (MAX(DocumentNo),0)+1 as DocumentNo
		    FROM GL_TransactionHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID
    
		    union all
    
		    Select IsNull(MAX(DocumentNo), 0) + 1 as DocumentNo
		    FROM PUR_EnquiryHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID

		    union all
    
		    select ISNULL (MAX(CAST(DocumentNumber AS INT)),0)+1 as DocumentNo
		    FROM AP_PurchaseOrderHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and TypeCode = @DocumentTypeID

		    union all
    
		    select ISNULL (MAX(DocumentNo),0)+1 as DocumentNo
		    FROM INV_StockTransactionHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID

		    union all
    
		    Select IsNull(MAX(CAST(InvoiceNumber AS INT)), 0) + 1 as DocumentNo
		    FROM AR_InvoiceHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and InvoiceType = @DocumentTypeID

		    union all
    
		    Select IsNull(MAX(DocumentNo), 0) + 1 as DocumentNo
		    FROM AR_ReceiptTransactionHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID And DocumentTypeID = @DocumentTypeID

		    union all
    
		    select ISNULL (MAX(CAST(ContractNo AS INT)),0)+1 as DocumentNo
		    FROM SAL_OrderHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and SaleContractTypeID = @DocumentTypeID

		    union all
    
		    select ISNULL (MAX(CAST(DeliveryOrderNo AS INT)),0)+1 as DocumentNo
		    FROM SAL_DeliveryOrderHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID

		    union all
    
		    select ISNULL (MAX(CAST(DispatchNo AS INT)),0)+1 as DocumentNo
		    FROM SAL_DispatchHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID

		    union all
    
		    select ISNULL (MAX(CAST(InvoiceNumber AS INT)),0)+1 as DocumentNo
		    FROM SAL_InvoiceHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and InvoiceType = @DocumentTypeID

		    union all
    
		    Select IsNull(MAX(DocumentNo), 0) + 1 as DocumentNo
		    FROM AP_PaymentHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID

            union all
    
		    Select IsNull(MAX(DocumentNo), 0) + 1 as DocumentNo
		    FROM Gate_OutwardHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID

            union all

		    Select IsNull(MAX(DocumentNo), 0) + 1 as DocumentNo
		    FROM SAL_ReverseHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID

            union all 
		    
		    Select IsNull(MAX(DocumentNo), 0) + 1 as DocumentNo
		    FROM AP_ProductionJobCardHeader
		    where ComapnyID = @CompanyID and DocumentTypeID = @DocumentTypeID

            union all 

            Select IsNull(MAX(DocumentNo), 0) + 1 as DocumentNo
	        from AS_InspectionHeader
	        where CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID


            union all 
		    
		    Select IsNull(MAX(DocumentNo), 0) + 1 as DocumentNo
		    FROM MRPHeader
		    where CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID
            union all

           select ISNULL (MAX(CAST(DocumentNo AS INT)),0)+1 as DocumentNo
		    FROM INV_ItemsPriceHistory
		    where  CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID

        ) MaxDocumentNo
    ) tbl
    group by tbl.FromNo, tbl.ToNo, tbl.DocumentNo
    order by DocumentNo Desc
End 
Else
Begin
    SELECT IsNull(SUM(tbl.DocumentNo), 0)DocumentNo, isnull(sum(tbl.HasRangeDefined), 0) HasRangeDefined, tbl.FromNo, tbl.ToNo
    FROM 
    (
	    select case when COUNT(DocumentTypeID) > 0 then 1 else 0 end HasRangeDefined, FromNo, ToNo, 0 DocumentNo
	    from SET_DocumentSequence
        where DocumentTypeID = @DocumentTypeID and CompanyID = @CompanyID and PeriodID = @PeriodID and [Status] = 1 AND IsActive = @IsActive 
        group by  FromNo,ToNo
    
        union all
    
        select 0 HasRangeDefined, 0 FromNo, 0 ToNo, MAX(MaxDocumentNo.DocumentNo) as DocumentNo From
        (
		    select ISNULL (MAX(DocumentNo),0)+1 as DocumentNo
		    FROM GL_TransactionHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID
    
		    union all
    
		    Select IsNull(MAX(DocumentNo), 0) + 1 as DocumentNo
		    FROM PUR_EnquiryHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID

		    union all
    
		    select ISNULL (MAX(CAST(DocumentNumber AS INT)),0)+1 as DocumentNo
		    FROM AP_PurchaseOrderHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and TypeCode = @DocumentTypeID

		    union all
    
		    select ISNULL (MAX(DocumentNo),0)+1 as DocumentNo
		    FROM INV_StockTransactionHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID

		    union all
    
		    Select IsNull(MAX(CAST(InvoiceNumber AS INT)), 0) + 1 as DocumentNo
		    FROM AR_InvoiceHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and InvoiceType = @DocumentTypeID

		    union all
    
		    Select IsNull(MAX(DocumentNo), 0) + 1 as DocumentNo
		    FROM AR_ReceiptTransactionHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID And DocumentTypeID = @DocumentTypeID

		    union all
    
		    select ISNULL (MAX(CAST(ContractNo AS INT)),0)+1 as DocumentNo
		    FROM SAL_OrderHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and SaleContractTypeID = @DocumentTypeID

		    union all
    
		    select ISNULL (MAX(CAST(DeliveryOrderNo AS INT)),0)+1 as DocumentNo
		    FROM SAL_DeliveryOrderHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID

		    union all
    
		    select ISNULL (MAX(CAST(DispatchNo AS INT)),0)+1 as DocumentNo
		    FROM SAL_DispatchHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID

		    union all
    
		    select ISNULL (MAX(CAST(InvoiceNumber AS INT)),0)+1 as DocumentNo
		    FROM SAL_InvoiceHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and InvoiceType = @DocumentTypeID

		    union all
    
		    Select IsNull(MAX(DocumentNo), 0) + 1 as DocumentNo
		    FROM AP_PaymentHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID

            union all
    
		    Select IsNull(MAX(DocumentNo), 0) + 1 as DocumentNo
		    FROM Gate_OutwardHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID

            union all

		    Select IsNull(MAX(DocumentNo), 0) + 1 as DocumentNo
		    FROM SAL_ReverseHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID
             union all

select ISNULL (MAX(CAST(DocumentNumber AS INT)),0)+1 as DocumentNo
		    FROM AP_SaleContractHeader
		    where PeriodID = @PeriodID and CompanyID = @CompanyID and TypeCode = @DocumentTypeID

		    union all
    
           select ISNULL (MAX(CAST(DocumentNo AS INT)),0)+1 as DocumentNo
		    FROM INV_ItemsPriceHistory
		    where  CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID
        ) MaxDocumentNo
    ) tbl
    group by tbl.FromNo, tbl.ToNo, tbl.DocumentNo
    order by DocumentNo Desc
End ";

     
        const string Query_HasChildRecordsFORAS = @" 
Declare @IsFixedAssetOn varchar(5);
Select @IsFixedAssetOn = FieldValue1 From Set_Parameter Where FieldName = 'IsFixedAsset';

if(@IsFixedAssetOn = 'true')
Begin
    SELECT IsNull(SUM(tbl.DocumentNo), 0)DocumentNo, isnull(sum(tbl.HasRangeDefined), 0) HasRangeDefined, tbl.FromNo, tbl.ToNo
    FROM 
    (
	    select case when COUNT(DocumentTypeID) > 0 then 1 else 0 end HasRangeDefined, FromNo, ToNo, 0 DocumentNo
	    from SET_DocumentSequence
        where DocumentTypeID = @DocumentTypeID and CompanyID = @CompanyID and PeriodID = @PeriodID and [Status] = 1 AND IsActive = @IsActive 
        group by  FromNo,ToNo
    
        union all
    
        select 0 HasRangeDefined, 0 FromNo, 0 ToNo, MAX(MaxDocumentNo.DocumentNo) as DocumentNo From
        (
		    Select IsNull(MAX(ServiceRecipeNo), 0) + 1 as DocumentNo
		    FROM AS_ServiceRecipe
		    where CompanyID = @CompanyID and ServiceRecipeTypeID = @DocumentTypeID
            
            UNION ALL
		    
		    Select IsNull(MAX(ServiceNo), 0) + 1 as DocumentNo
		    FROM AS_ServicePerformanceHeader
		    where CompanyID = @CompanyID and PeriodID = @PeriodID and DocumentTypeID = @DocumentTypeID


		    UNION ALL
		    
		    Select IsNull(MAX(OfferNumber), 0) + 1 as DocumentNo
		    FROM AS_Offer
		    where CompanyID = @CompanyID and PeriodID = @PeriodID and OfferTypeID = @DocumentTypeID
            
        ) MaxDocumentNo
    ) tbl
    group by tbl.FromNo, tbl.ToNo, tbl.DocumentNo
    order by tbl.DocumentNo Desc
End
Else
Begin
    SELECT IsNull(SUM(tbl.DocumentNo), 0)DocumentNo, isnull(sum(tbl.HasRangeDefined), 0) HasRangeDefined, tbl.FromNo, tbl.ToNo
    FROM 
    (
	    select case when COUNT(DocumentTypeID) > 0 then 1 else 0 end HasRangeDefined, FromNo, ToNo, 0 DocumentNo
	    from SET_DocumentSequence
        where DocumentTypeID = @DocumentTypeID and CompanyID = @CompanyID and PeriodID = @PeriodID and [Status] = 1 AND IsActive = @IsActive 
        group by  FromNo,ToNo
    
        union all
    
        select 0 HasRangeDefined, 0 FromNo, 0 ToNo, MAX(MaxDocumentNo.DocumentNo) as DocumentNo From
        (
		    Select IsNull(MAX(ServiceRecipeNo), 0) + 1 as DocumentNo
		    FROM AS_ServiceRecipe
		    where CompanyID = @CompanyID and ServiceRecipeTypeID = @DocumentTypeID
            
            UNION ALL
		    
		    Select IsNull(MAX(ServiceNo), 0) + 1 as DocumentNo
		    FROM AS_ServicePerformanceHeader
		    where CompanyID = @CompanyID and PeriodID = @PeriodID and DocumentTypeID = @DocumentTypeID

            UNION ALL
		    
		    Select IsNull(MAX(convert(int,OfferNumber)), 0) + 1 as DocumentNo
		    FROM AS_Offer
		    where CompanyID = @CompanyID and PeriodID = @PeriodID and OfferTypeID = @DocumentTypeID

        ) MaxDocumentNo
    ) tbl
    group by tbl.FromNo, tbl.ToNo, tbl.DocumentNo
    order by DocumentNo Desc
End 
";
        #endregion

        #region Class Methods

        public static DataTable GetDataByDocumentType(int companyID, int documentTypeID, int periodID, Nullable<bool> isActive = true)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                if (isActive.HasValue)
                    sqlparams[3] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[3] = new SqlParameter("@IsActive", DBNull.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_ByKey, sqlparams);

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

        public static DataTable HasChildsForPF(int documentTypeID, int companyID, int periodID, string DatabaseName, bool isActive = true, SqlTransaction sqlTransaction = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;

            try
            {
                StringBuilder sb = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@IsActive", isActive);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                sb.Append(DatabaseName);
                string Query = string.Empty;

                if (CurrentFiscalID > 0)
                    Query = FnI.QueryResourceLibrary.Setup.SET_DocumentSequence.Query_HasChildRecordsFiscalForPF;
                else
                    Query = FnI.QueryResourceLibrary.Setup.SET_DocumentSequence.Query_HasChildRecordsForPF;

                DataSet ds = null;
                if (sqlTransaction != null)
                    ds = SqlHelper.ExecuteDataset(sqlTransaction, CommandType.Text, string.Format(Query, sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, sb.ToString()), sqlparams);

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

        public static bool DeleteDataByKey(int documentTypeID, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] SqlParam = new SqlParameter[1];
                SqlParam[0] = new SqlParameter("@DocumentTypeID", documentTypeID);

                if (objTransaction != null)
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_By_DocumentType, SqlParam) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_By_DocumentType, SqlParam) > 0)
                        result = true;
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw new BusinessLogicException("Error In Delete Class document sequence" + ex.Message);
            }

            return result;
        }

        public static DataTable HasChilds(int documentTypeID, int companyID, int periodID, bool isActive = true, SqlTransaction sqlTransaction = null , int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@IsActive", isActive);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                string Query = string.Empty;

                if (CurrentFiscalID>0)
                    Query =  FnI.QueryResourceLibrary.Setup.SET_DocumentSequence.Query_HasChildRecordsFiscal;
                else
                    Query = Query_HasChildRecords;

                DataSet ds = null;
                if (sqlTransaction != null)
                    ds = SqlHelper.ExecuteDataset(sqlTransaction, CommandType.Text, Query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);

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

        public static DataTable HasChildsforAS(int documentTypeID, int companyID, int periodID, bool isActive = true, SqlTransaction sqlTransaction = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@IsActive", isActive);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;
                if (sqlTransaction != null)
                    ds = SqlHelper.ExecuteDataset(sqlTransaction, CommandType.Text, Query_HasChildRecordsFORAS, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_HasChildRecordsFORAS, sqlparams);

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

        public static int ManualDocumentSequence(int documentTypeId, int companyId, int periodID, SqlTransaction sqlTransaction = null, int CurrentFiscalID = 0)
        {
            DataTable dt = HasChilds(documentTypeId, companyId, periodID,true,sqlTransaction,CurrentFiscalID);
            string Documentno = dt.Rows[0]["DocumentNo"].ToString();
            int documentno = Documentno != string.Empty ? int.Parse(Documentno) : 1;
            DataRow[] row = dt.Select("HasRangeDefined = 1");

            if (row.Length > 0)
            {
                bool isProperRangeFound = false;

                foreach (DataRow r in row)
                {
                    if (documentno >= int.Parse(r["FromNo"].ToString()) && documentno <= int.Parse(r["ToNo"].ToString()))
                    {
                        isProperRangeFound = true;
                        break;
                    }

                    if (documentno < int.Parse(r["FromNo"].ToString()))
                    {
                        documentno = int.Parse(r["FromNo"].ToString());
                        isProperRangeFound = true;
                        break;
                    }
                }

                if (!isProperRangeFound)
                    throw new BusinessLogicException("Not any valid Document Number range found");
            }

            return documentno;
        }

        public static long ManualDocumentSequence_Long(int documentTypeId, int companyId, int periodID, SqlTransaction sqlTransaction = null, int CurrentFiscalID = 0)
        {
            DataTable dt = HasChilds(documentTypeId, companyId, periodID, true, sqlTransaction, CurrentFiscalID);
            string Documentno = dt.Rows[0]["DocumentNo"].ToString();
            long documentno = Documentno != string.Empty ? long.Parse(Documentno) : 1;
            DataRow[] row = dt.Select("HasRangeDefined = 1");

            if (row.Length > 0)
            {
                bool isProperRangeFound = false;

                foreach (DataRow r in row)
                {
                    if (documentno >= int.Parse(r["FromNo"].ToString()) && documentno <= int.Parse(r["ToNo"].ToString()))
                    {
                        isProperRangeFound = true;
                        break;
                    }

                    if (documentno < int.Parse(r["FromNo"].ToString()))
                    {
                        documentno = int.Parse(r["FromNo"].ToString());
                        isProperRangeFound = true;
                        break;
                    }
                }

                if (!isProperRangeFound)
                    throw new BusinessLogicException("Not any valid Document Number range found");
            }

            return documentno;
        }

        #endregion
    }
}
