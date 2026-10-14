using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;

using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Purchase
{
    public partial class PUR_QuotationHeader
    {
        #region Constant Queries

        const string Query_Get_MaxDocumentNo = @" Declare @StartDate DateTime , @EndDate DateTime;
Select @StartDate = StartDate from SET_PeriodCustom where FiscalId = @CurrentFiscalID
Select @EndDate = EndDate from SET_PeriodCustom where FiscalId = @CurrentFiscalID


Select IsNull(Max(Replace(DocumentNo, @Prefix, '')), 0) + 1 AS DocumentNo from [PUR_QuotationHeader] where DocumentNo like  @Prefix + '%' AND CompanyID = @CompanyID AND PeriodID = @PeriodID AND Status = 1 
and QuotedDate between @StartDate and @EndDate";

        const string Query_Get_MaxDocumentNo_RSM = @" Declare @StartDate DateTime , @EndDate DateTime;
Select @StartDate = StartDate from SET_PeriodCustom where FiscalId = @CurrentFiscalID
Select @EndDate = EndDate from SET_PeriodCustom where FiscalId = @CurrentFiscalID


Select IsNull(Max(Replace([PUR_QuotationHeader].DocumentNo, @Prefix, '')), 0) + 1 AS DocumentNo from [PUR_QuotationHeader] 
INNER JOIN dbo.PUR_EnquiryHeader ON PUR_EnquiryHeader.CompanyID = PUR_QuotationHeader.CompanyID AND PUR_EnquiryHeader.PeriodID = PUR_QuotationHeader.PeriodID AND PUR_EnquiryHeader.EnquiryID = PUR_QuotationHeader.EnquiryHeaderID
where [PUR_QuotationHeader].DocumentNo like  @Prefix + '%' AND [PUR_QuotationHeader].CompanyID = @CompanyID AND [PUR_QuotationHeader].PeriodID = @PeriodID AND [PUR_QuotationHeader].Status = 1 
and QuotedDate between @StartDate and @EndDate
AND PUR_EnquiryHeader.DocumentTypeID = @DocumentTypeID";

        const string Query_Select_By_EnquiryHID = @" SELECT DISTINCT PUR_QuotationHeader.QuotationID, PUR_QuotationHeader.DocumentNo, PUR_QuotationHeader.QuotedDate, 
    AR_SET_Customer.CustomerName AS Supplier
FROM PUR_QuotationHeader 
INNER JOIN PUR_QuotationDetail ON PUR_QuotationDetail.CompanyID = PUR_QuotationHeader.CompanyID AND PUR_QuotationDetail.PeriodID = PUR_QuotationHeader.PeriodID AND 
    PUR_QuotationDetail.QuotationHeaderID = PUR_QuotationHeader.QuotationID AND PUR_QuotationDetail.Status = 1
INNER JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = PUR_QuotationHeader.SupplierID AND AR_SET_Customer.Status = 1 
WHERE PUR_QuotationHeader.CompanyID = @CompanyID and PUR_QuotationHeader.PeriodID = @PeriodID and PUR_QuotationHeader.EnquiryHeaderID = @EnquiryHeaderID AND    
    PUR_QuotationHeader.Status = 1  --and PUR_QuotationHeader.FiscalID = @FiscalID
";

        const string Query_Update_Flag_Detail = @" UPDATE [PUR_QuotationDetail] SET [IsPurchased] = @IsPurchased, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue, [OrderDate] = @OrderDate WHERE [QuotationHeaderID] = @QuotationHeaderID AND [QuotationDetailID] = @QuotationDetailID AND [CompanyID] = @CompanyID AND [PeriodID] = @PeriodID  AND [Status] = 1 ";

        const string Query_Update_Flag_Header = @" UPDATE [PUR_QuotationHeader] SET [IsPurchased] = @IsPurchased, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [QuotationID] = @QuotationHeaderID AND [CompanyID] = @CompanyID AND [PeriodID] = @PeriodID  AND [Status] = 1 ";

        const string Query_CheckPurchaseStatus = @" SELECT [IsPurchased] FROM [PUR_QuotationDetail] WHERE [CompanyID] = @CompanyID AND [PeriodID] = @PeriodID AND [QuotationHeaderID] =  @QuotationHeaderID AND [Status] = 1 AND [IsPurchased] <> @IsPurchased ";

        const string Query_QuotationsReceived = @" SELECT COUNT([QuotationID]) FROM [PUR_QuotationHeader] WHERE [CompanyID] = @CompanyID AND [PeriodID] = @PeriodID AND [EnquiryHeaderID] =  @EnquiryHeaderID AND [Status] = 1 ";

        const string Query_GetData_WithCostCenterName = @" SELECT *, CostCenterName FROM [PUR_QuotationHeader] 
left join SET_CostCenter on SET_CostCenter.CostCenterID = PUR_QuotationHeader.CostCenterID
WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [QuotationID] = @QuotationID ";

        const string Query_Comparative_Statement_Report = @"



";

        const string Query_Quotation_SelectAll_WithPaging = @" SELECT [PUR_QuotationHeader].[CompanyID], [PUR_QuotationHeader].[PeriodID], [QuotationID], [EnquiryHeaderID], [PUR_QuotationHeader].[DocumentNo], [SupplierID] ,SET_DocumentType.DocumentType , 
[PUR_QuotationHeader].[ReferenceNumber], [QuotedDate], [PUR_QuotationHeader].[DeliveryDate], [CostCenterID], [IsPurchased], [PUR_QuotationHeader].[Status], [PUR_QuotationHeader].[CreatedBy],
[PUR_QuotationHeader].[CreateDate], [PUR_QuotationHeader].[CreatedByValue], [PUR_QuotationHeader].[UpdatedBy], [PUR_QuotationHeader].[UpdateDate], [PUR_QuotationHeader].[UpdatedByValue],
Set_PeriodCustom.PeriodSetName,[QuotedDate] as DocumentDate,[PUR_QuotationHeader].Comments,[PUR_QuotationHeader].ImageFile,
  ROW_NUMBER() Over (Order by [PUR_QuotationHeader].CompanyID, [PUR_QuotationHeader].PeriodID, QuotationID desc) as 'Row'
  FROM [PUR_QuotationHeader] 
inner join Set_PeriodCustom on  [PUR_QuotationHeader].QuotedDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1 and Set_PeriodCustom.FiscalId = @CurrentFiscalID
INNER JOIN PUR_EnquiryHeader ON PUR_EnquiryHeader.EnquiryID = [PUR_QuotationHeader].EnquiryHeaderID 
AND PUR_EnquiryHeader.CompanyID = PUR_QuotationHeader.CompanyID AND PUR_EnquiryHeader.Status = 1
LEFT JOIN SET_DocumentType ON PUR_EnquiryHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID ";


        const string Query_Quotation_WithoutPaging = @" 
SELECT [PUR_QuotationHeader].[CompanyID], [PUR_QuotationHeader].[PeriodID], [QuotationID], [EnquiryHeaderID], [PUR_QuotationHeader].[DocumentNo], [SupplierID], SET_DocumentType.DocumentType, 
[PUR_QuotationHeader].[ReferenceNumber], [QuotedDate], [PUR_QuotationHeader].[DeliveryDate], [CostCenterID], [IsPurchased], [PUR_QuotationHeader].[Status], [PUR_QuotationHeader].[CreatedBy],[PUR_QuotationHeader].Comments,[PUR_QuotationHeader].ImageFile,
[PUR_QuotationHeader].[CreateDate], [PUR_QuotationHeader].[CreatedByValue],[PUR_QuotationHeader].[UpdatedBy], [PUR_QuotationHeader].[UpdateDate], [PUR_QuotationHeader].[UpdatedByValue],
Set_PeriodCustom.PeriodSetName,[QuotedDate] as DocumentDate FROM [PUR_QuotationHeader] 
Left join Set_PeriodCustom on  [PUR_QuotationHeader].QuotedDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1 
INNER JOIN PUR_EnquiryHeader ON PUR_EnquiryHeader.EnquiryID = [PUR_QuotationHeader].EnquiryHeaderID AND PUR_EnquiryHeader.Status = 1
LEFT JOIN SET_DocumentType ON PUR_EnquiryHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID";
        
        #endregion

        #region Static Methods

        public static DataSet Comparative_Statement_Report(int companyID, int periodID, int IsSuppress, string PreparedBy, string PurchaseOfficer, 
            string DeputyManagerOfficer, string ManagerPurchase, string Director, Nullable<int> rfqNumberFrom,
            Nullable<int> rfqNumberTo, Nullable<int> customerID, Nullable<int> DocumentTypeID, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            StringBuilder AdditionalParameter = new StringBuilder();
            string AdditionalParameterString = "";
            DataTable dtToReturn = null;
            DataSet dsToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[13];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@IsSuppress", IsSuppress);
                sqlparams[3] = new SqlParameter("@PreparedBy", PreparedBy);
                sqlparams[4] = new SqlParameter("@PurchaseOfficer", PurchaseOfficer);
                sqlparams[5] = new SqlParameter("@DeputyManagerOfficer", DeputyManagerOfficer);
                sqlparams[6] = new SqlParameter("@ManagerPurchase", ManagerPurchase);
                sqlparams[7] = new SqlParameter("@Director", Director);
                sqlparams[8] = new SqlParameter("@RFQNumberFrom", rfqNumberFrom);
                sqlparams[9] = new SqlParameter("@RFQNumberTo", rfqNumberTo);//@CustomerID
                sqlparams[10] = new SqlParameter("@CustomerID", customerID);
                sqlparams[11] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[12] = new SqlParameter("@DocumentTYpeID", DocumentTypeID);
                

                if (rfqNumberFrom.HasValue && rfqNumberTo.HasValue)
                {
                    AdditionalParameter.Append(" AND PUR_EnquiryHeader.DocumentNo BETWEEN @RFQNumberFrom AND @RFQNumberTo");
                    AdditionalParameterString += " AND PUR_EnquiryHeader.DocumentNo BETWEEN @RFQNumberFrom AND @RFQNumberTo";
                }
                if (customerID > 0)
                {
                    AdditionalParameter.Append(" AND CustomerID = @CustomerID");
                    AdditionalParameterString += " AND CustomerID = @CustomerID";
                }

                //if (DocumentTypeID > 0)
                //{
                //    AdditionalParameter.Append(" AND PUR_EnquiryHeader.DocumentTypeID = " + DocumentTypeID);
                //    AdditionalParameterString += " AND PUR_EnquiryHeader.DocumentTypeID = " + DocumentTypeID;
                //}



                string additionalFilter = "";

                if (documentGroupID.HasValue)
                    additionalFilter =" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();


                string query = FnI.QueryResourceLibrary.Purchase.PUR_EnquiryHeader.Query_Comparative_Statement_Report.Replace("{AdditionalParameters}", AdditionalParameterString);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(query,additionalFilter), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];

                if (ds != null && ds.Tables.Count > 1)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static DataSet Comparative_Statement_ReportNew(int companyID, int periodID, int IsSuppress, string PreparedBy, string PurchaseOfficer,
            string DeputyManagerOfficer, string ManagerPurchase, string Director, Nullable<int> rfqNumberFrom,
            Nullable<int> rfqNumberTo, Nullable<int> customerID, Nullable<int> DocumentTypeID, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            StringBuilder AdditionalParameter = new StringBuilder();
            string AdditionalParameterString = "";
            DataTable dtToReturn = null;
            DataSet dsToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[13];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@IsSuppress", IsSuppress);
                sqlparams[3] = new SqlParameter("@PreparedBy", PreparedBy);
                sqlparams[4] = new SqlParameter("@PurchaseOfficer", PurchaseOfficer);
                sqlparams[5] = new SqlParameter("@DeputyManagerOfficer", DeputyManagerOfficer);
                sqlparams[6] = new SqlParameter("@ManagerPurchase", ManagerPurchase);
                sqlparams[7] = new SqlParameter("@Director", Director);
                sqlparams[8] = new SqlParameter("@RFQNumberFrom", rfqNumberFrom);
                sqlparams[9] = new SqlParameter("@RFQNumberTo", rfqNumberTo);//@CustomerID
                sqlparams[10] = new SqlParameter("@CustomerID", customerID);
                sqlparams[11] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[12] = new SqlParameter("@DocumentTYpeID", DocumentTypeID);


                if (rfqNumberFrom.HasValue && rfqNumberTo.HasValue)
                {
                    AdditionalParameter.Append(" AND PUR_EnquiryHeader.DocumentNo BETWEEN @RFQNumberFrom AND @RFQNumberTo");
                    AdditionalParameterString += " AND PUR_EnquiryHeader.DocumentNo BETWEEN @RFQNumberFrom AND @RFQNumberTo";
                }
                if (customerID > 0)
                {
                    AdditionalParameter.Append(" AND CustomerID = @CustomerID");
                    AdditionalParameterString += " AND CustomerID = @CustomerID";
                }
                
                string additionalFilter = "";

                if (documentGroupID.HasValue)
                    additionalFilter = " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();


                string query = FnI.QueryResourceLibrary.Purchase.PUR_EnquiryHeader.Query_Comparative_Statement_ReportNew.Replace("{AdditionalParameters}", AdditionalParameterString);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(query, additionalFilter), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];

                if (ds != null && ds.Tables.Count > 1)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static DataSet Comparative_Statement_ReportDateWise(int companyID, int periodID, Nullable<int> currDocNo, Nullable<DateTime> podocumentdate, int IsSuppress, string PreparedBy, string PurchaseOfficer,
            string DeputyManagerOfficer, string ManagerPurchase, string Director, Nullable<int> rfqNumberFrom,
            Nullable<int> rfqNumberTo, Nullable<int> customerID, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            StringBuilder AdditionalParameter = new StringBuilder();
            string AdditionalParameterString = "";
            DataTable dtToReturn = null;
            DataSet dsToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[14];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@IsSuppress", IsSuppress);
                sqlparams[3] = new SqlParameter("@PreparedBy", PreparedBy);
                sqlparams[4] = new SqlParameter("@PurchaseOfficer", PurchaseOfficer);
                sqlparams[5] = new SqlParameter("@DeputyManagerOfficer", DeputyManagerOfficer);
                sqlparams[6] = new SqlParameter("@ManagerPurchase", ManagerPurchase);
                sqlparams[7] = new SqlParameter("@Director", Director);
                sqlparams[8] = new SqlParameter("@RFQNumberFrom", rfqNumberFrom);
                sqlparams[9] = new SqlParameter("@RFQNumberTo", rfqNumberTo);//@CustomerID
                sqlparams[10] = new SqlParameter("@CustomerID", customerID);
                sqlparams[11] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[12] = new SqlParameter("@DocumentDate", podocumentdate);
                sqlparams[13] = new SqlParameter("@CurrDocNo", currDocNo);


                if (rfqNumberFrom.HasValue && rfqNumberTo.HasValue)
                {
                    AdditionalParameter.Append(" AND PUR_EnquiryHeader.DocumentNo BETWEEN @RFQNumberFrom AND @RFQNumberTo");
                    AdditionalParameterString += " AND PUR_EnquiryHeader.DocumentNo BETWEEN @RFQNumberFrom AND @RFQNumberTo";
                }
                if (customerID > 0)
                {
                    AdditionalParameter.Append(" AND CustomerID = @CustomerID");
                    AdditionalParameterString += " AND CustomerID = @CustomerID";
                }

                string additionalFilter = "";

                if (documentGroupID.HasValue)
                    additionalFilter = " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();


                string query = FnI.QueryResourceLibrary.Purchase.PUR_EnquiryHeader.Query_Comparative_Statement_ReportDateWise.Replace("{AdditionalParameters}", AdditionalParameterString);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(query, additionalFilter), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];

                if (ds != null && ds.Tables.Count > 1)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        #endregion

        #region Class Methods

        public static int GetNewDocumentNo(string prefix, int companyID, int periodID, SqlTransaction objTransaction = null, int CurrentFiscalID= 0)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@Prefix", prefix);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (objTransaction == null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_MaxDocumentNo, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_MaxDocumentNo, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static int GetNewDocumentNo_RSM(string prefix, int companyID, int periodID, SqlTransaction objTransaction = null, int CurrentFiscalID = 0, int DocumentTypeID = 0)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@Prefix", prefix);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[4] = new SqlParameter("@DocumentTypeID", DocumentTypeID);

                if (objTransaction == null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_MaxDocumentNo_RSM, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_MaxDocumentNo_RSM, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static DataTable GetDataByEnquiry(int companyID, int periodID, int enquiryHeaderID, string appendedFilter, int FiscalID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@EnquiryHeaderID", enquiryHeaderID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@FiscalID", FiscalID);
                

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_EnquiryHID + appendedFilter, sqlparams);

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

        public bool UpdateFlagDetail(SqlTransaction objTransaction, int quotationHeaderID, short quotationDetailID, bool isPurchased, int companyID, int periodID, 
            Guid updatedBy, DateTime updateDate, string updatedByValue, Nullable<DateTime> orderDate)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[9];
                sqlparams[0] = new SqlParameter("@QuotationHeaderID", quotationHeaderID);
                sqlparams[1] = new SqlParameter("@QuotationDetailID", quotationDetailID);
                sqlparams[2] = new SqlParameter("@IsPurchased", isPurchased);
                sqlparams[3] = new SqlParameter("@CompanyID", companyID);
                sqlparams[4] = new SqlParameter("@PeriodID", periodID);
                sqlparams[5] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[6] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[7] = new SqlParameter("@UpdatedByValue", updatedByValue);

                if(orderDate.HasValue)
                    sqlparams[8] = new SqlParameter("@OrderDate", orderDate.Value);
                else
                    sqlparams[8] = new SqlParameter("@OrderDate", DBNull.Value);

                SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_Flag_Detail, sqlparams);
                result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public bool UpdateFlagHeader(SqlTransaction objTransaction, int quotationHeaderID, bool isPurchased, int companyID, int periodID, Guid updatedBy, 
            DateTime updateDate, string updatedByValue)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@QuotationHeaderID", quotationHeaderID);
                sqlparams[1] = new SqlParameter("@IsPurchased", isPurchased);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);
                sqlparams[4] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[5] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[6] = new SqlParameter("@UpdatedByValue", updatedByValue);

                if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_Flag_Header, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public bool CheckPurchaseStatus(SqlTransaction sqltrans, int quotationHeaderID, bool isPurchased, int companyID, int periodID)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@QuotationHeaderID", quotationHeaderID);
                sqlparams[1] = new SqlParameter("@IsPurchased", isPurchased);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);

                DataTable dtQuotation = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query_CheckPurchaseStatus, sqlparams).Tables[0];
                if (dtQuotation != null && dtQuotation.Rows.Count == 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public bool InsertQHeaderAndDetail(List<PUR_QuotationDetail> colQDetail, int totalSuppliers,bool IsRFQClose)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertQuotation");

            try
            {
                this.Insert(sqltrans);

                foreach (PUR_QuotationDetail qd in colQDetail)
                {
                    qd.QuotationDetailID = Convert.ToInt16(PUR_QuotationDetail.GetNewQuotationDetailID(CompanyID, PeriodID, QuotationID, sqltrans));
                    qd.Insert(sqltrans);

                    if (IsRFQClose)
                    {
                        PUR_EnquiryHeader eh = new PUR_EnquiryHeader();
                        eh.UpdateFlagHeader(sqltrans, f_EnquiryHeaderID, true, this.f_CompanyID, this.f_PeriodID, f_CreatedBy, f_CreateDate, f_CreatedByValue);
                    }

                    //#region Checking For All Quotations Received

                    //CheckAllQuotationsReceived(f_EnquiryHeaderID, this.f_CompanyID, this.f_PeriodID, sqltrans, f_CreatedBy, f_CreateDate, f_CreatedByValue, "Add", 
                    //    totalSuppliers);

                    //#endregion
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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertQuotation");
            }

            return result;
        }

        public bool UpdateQHeaderAndDetail(List<PUR_QuotationDetail> colAddQDetail, List<PUR_QuotationDetail> colUpdateQDetail, List<PUR_QuotationDetail> colDeleteQDetail, int totalSuppliers, bool IsRFQClose)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateQuotation");

            try
            {
                this.UpdateDataByKey(sqltrans);
                if (IsRFQClose)
                {
                    PUR_EnquiryHeader eh = new PUR_EnquiryHeader();
                    eh.UpdateFlagHeader(sqltrans, f_EnquiryHeaderID, true, this.f_CompanyID, this.f_PeriodID, Guid.Parse(f_UpdatedBy.ToString()),DateTime.Parse(f_UpdateDate.ToString()), f_UpdatedByValue);
                }

                foreach (PUR_QuotationDetail qdAdd in colAddQDetail)
                {
                    qdAdd.QuotationDetailID = Convert.ToInt16(PUR_QuotationDetail.GetNewQuotationDetailID(CompanyID, PeriodID, QuotationID, sqltrans));
                    qdAdd.Insert(sqltrans);

                    if (IsRFQClose)
                    {
                        PUR_EnquiryHeader eh = new PUR_EnquiryHeader();
                        eh.UpdateFlagHeader(sqltrans, f_EnquiryHeaderID, true, this.f_CompanyID, this.f_PeriodID, Guid.Parse(f_UpdatedBy.ToString()), DateTime.Parse(f_UpdateDate.ToString()), f_UpdatedByValue);
                    }

                    //#region Checking For All Quotations Received

                    //CheckAllQuotationsReceived(f_EnquiryHeaderID, this.f_CompanyID, this.f_PeriodID, sqltrans, f_CreatedBy, f_CreateDate, f_CreatedByValue, "Add", 
                    //    totalSuppliers);

                    //#endregion
                }

                foreach (PUR_QuotationDetail qdUpdate in colUpdateQDetail)
                    qdUpdate.UpdateDataByKey(sqltrans);

                foreach (PUR_QuotationDetail qdDelete in colDeleteQDetail)
                {
                    PUR_QuotationDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, f_PeriodID, qdDelete.QuotationDetailID, QuotationID);

                    if (IsRFQClose)
                    {
                        PUR_EnquiryHeader eh = new PUR_EnquiryHeader();
                        eh.UpdateFlagHeader(sqltrans, f_EnquiryHeaderID, true, this.f_CompanyID, this.f_PeriodID, Guid.Parse(f_UpdatedBy.ToString()), DateTime.Parse(f_UpdateDate.ToString()), f_UpdatedByValue);
                    }

                    //#region Checking For All Quotations Received

                    //CheckAllQuotationsReceived(f_EnquiryHeaderID, this.f_CompanyID, this.f_PeriodID, sqltrans, f_CreatedBy, f_CreateDate, f_CreatedByValue, "Delete", 
                    //    totalSuppliers);

                    //#endregion
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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateQuotation");
            }

            return result;
        }

        public bool DeleteQHeaderAndDetail(List<PUR_QuotationDetail> colDeleteQDetail, int totalSuppliers)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("DeleteQuotation");

            try
            {
                DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, UpdatedByValue, f_CompanyID, f_PeriodID, QuotationID);

                foreach (PUR_QuotationDetail qdDelete in colDeleteQDetail)
                {
                    PUR_QuotationDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, UpdatedByValue, f_CompanyID, f_PeriodID, qdDelete.QuotationDetailID, QuotationID);

                    #region Checking For All Quotations Received

                    CheckAllQuotationsReceived(this.f_EnquiryHeaderID, this.f_CompanyID, this.f_PeriodID, sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, 
                        f_UpdatedByValue, "Delete", totalSuppliers);

                    #endregion
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
                    SqlHelper.RollBackTransaction(sqltrans, "DeleteQuotation");
            }
            return result;
        }

        private void CheckAllQuotationsReceived(long enquiryHeaderID, int companyID, int periodID, SqlTransaction sqltrans, Guid updatedBy, DateTime updateDate, 
            string updatedByValue, string operation, int totalSuppliers)
        {
            try
            {
                PUR_EnquiryHeader eh = new PUR_EnquiryHeader();

                if (!operation.Equals("Delete"))
                {
                    if(totalSuppliers <= this.QuotationsReceived(sqltrans, enquiryHeaderID, companyID, periodID))
                        eh.UpdateFlagHeader(sqltrans, enquiryHeaderID, true, companyID, periodID, updatedBy, updateDate, updatedByValue);
                    else
                        eh.UpdateFlagHeader(sqltrans, enquiryHeaderID, false, companyID, periodID, updatedBy, updateDate, updatedByValue);
                }
                else if (operation.Equals("Delete") && (totalSuppliers != this.QuotationsReceived(sqltrans, enquiryHeaderID, companyID, periodID)))
                    eh.UpdateFlagHeader(sqltrans, enquiryHeaderID, false, companyID, periodID, updatedBy, updateDate, updatedByValue);
            }

            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
        }

        public int QuotationsReceived(SqlTransaction sqltrans, long enquiryHeaderID, int companyID, int periodID)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@EnquiryHeaderID", enquiryHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                ID = Convert.ToInt32(SqlHelper.ExecuteScalar(sqltrans, CommandType.Text, Query_QuotationsReceived, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static DataTable GetAllDataSearchQuotation(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.desc,int CurrentFiscalID = 0)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

            sqlparamsList.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));

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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_Quotation_SelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_Quotation_SelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Quotation_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
            }

            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataSearchQuotation_RSM(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.desc, int CurrentFiscalID = 0)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

            sqlparamsList.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));

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
                    string Query_Quotation_WithPaging_RSM = FnI.QueryResourceLibrary.Purchase.PUR_EnquiryHeader.Query_Quotation_SelectAll_WithPaging_RSM.ToString();
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_Quotation_WithPaging_RSM + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_Quotation_WithPaging_RSM + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    string Query_Quotation_WithoutPaging_RSM = FnI.QueryResourceLibrary.Purchase.PUR_EnquiryHeader.Query_Quotation_WithoutPaging_RSM.ToString();
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Quotation_WithoutPaging_RSM + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }

            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataRow GetDataWithCostCenterName(int companyID, int periodID, int quotationID)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@QuotationID", quotationID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_WithCostCenterName, sqlparams);

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

        public DataTable GetAllQuotationSupplier(int CompanyID, int PeriodID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);

                string Query_GetAllQuotationSupplier = @" 
SELECT DISTINCT AR_SET_Customer.CustomerID, PUR_QuotationHeader.SupplierID, AR_SET_Customer.CustomerName, PUR_QuotationHeader.Status FROM dbo.PUR_QuotationHeader
INNER JOIN dbo.AR_SET_Customer ON AR_SET_Customer.CompanyID = PUR_QuotationHeader.CompanyID AND AR_SET_Customer.PeriodID = PUR_QuotationHeader.PeriodID AND AR_SET_Customer.CustomerID = PUR_QuotationHeader.SupplierID
WHERE PUR_QuotationHeader.CompanyID = @CompanyID
AND PUR_QuotationHeader.PeriodID = @PeriodID
AND PUR_QuotationHeader.Status = 1
ORDER BY AR_SET_Customer.CustomerName";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllQuotationSupplier, sqlparams).Tables[0];
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
