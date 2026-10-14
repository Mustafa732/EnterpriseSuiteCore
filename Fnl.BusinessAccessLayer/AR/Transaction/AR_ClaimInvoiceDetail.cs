using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;

using Common.Utils;

using DAL.DataAccessLayer;

using FnI.BusinessAccessLayer.AP.Transaction;
using FnI.BusinessAccessLayer.GL.Transaction;
using FnI.BusinessAccessLayer.Inventory.Transaction;
using FnI.BusinessAccessLayer.Setup;
using System.Text;

namespace FnI.BusinessAccessLayer.AR.Transaction
{
    public partial class AR_ClaimInvoiceDetail
    {

        const string Query_SelectAll_WithPagingWithOtherTable = @" select *, ROW_NUMBER() Over (Order by ClaimHeaderID, CompanyID, PeriodID) as 'Row'  from (
 SELECT distinct [ClaimHeaderID], [CompanyID], [PeriodID], [ClaimDate],[Status] 
 FROM [AR_ClaimInvoiceDetail] 
 ) as AR_ClaimInvoiceDetail "; 

        public static int GetNewClaimHeaderID(int companyID, int periodID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                const string Query_Get_MaxHeaderId = @" SELECT IsNull(max(ClaimHeaderID), 0) + 1 as ClaimHeaderID FROM AR_ClaimInvoiceDetail WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID ";

                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_MaxHeaderId, sqlparams));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_Get_MaxHeaderId, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_MaxHeaderId, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static DataTable GetSupplierWiseClaimInvoices(int companyID, int periodID, int ClaimHeaderID)
        {
            DataTable dt = null;
            try
            {
                #region
                string Query_VendorWiseSaleTax = @"
Select * from (
Select distinct AR_SET_Customer.CustomerName
,(Case when AR_SET_Customer.NTNNumber='' then null
else substring(AR_SET_Customer.NTNNumber ,0,8)+'-'+
substring( AR_SET_Customer.NTNNumber ,8,len( AR_SET_Customer.NTNNumber )-1)
 end )as NTNNumber
,(Case when AR_SET_Customer.STRNNumber='' then null
else substring(AR_SET_Customer.STRNNumber ,0,3)+'-'+
substring( AR_SET_Customer.STRNNumber ,3,2) +'-'+
substring( AR_SET_Customer.STRNNumber ,5,4) +'-'+
substring( AR_SET_Customer.STRNNumber ,9,3) +'-'+
substring( AR_SET_Customer.STRNNumber ,12,len( AR_SET_Customer.STRNNumber )-1)
end )as STRNNumber
,(Case when AR_SET_Customer.CNICNumber='' then null
else substring(AR_SET_Customer.CNICNumber,0,6) +'-'+
substring(AR_SET_Customer.CNICNumber,6,7) +'-'+
substring( AR_SET_Customer.CNICNumber ,13,len( AR_SET_Customer.CNICNumber )-1)
end )as CNICNumber
,AR_InvoiceHeader.InvoiceReceivedDate,AR_InvoiceHeader.VendorReferenceNumber,AR_InvoiceHeader.InvoiceNumber as VoucherNo,Set_DocumentType.DocumentType
, 	
Case When AR_Invoice_AccountsDetail.TransactionType = 'Credit' Then 
ABS((AR_InvoiceHeader.AmountPaid)) -  AR_InvoiceHeader.AmountPaid * max(isnull(AR_InvoiceDetail.TaxAmount,0)) / (100 + max(isnull(AR_InvoiceDetail.TaxAmount,0))) else
sum(AR_InvoiceDetail.GrossAmount) end as AmountPaid,

 max(isnull(AR_InvoiceDetail.TaxAmount,0)) as TaxRate,

 Case When AR_Invoice_AccountsDetail.TransactionType = 'Credit' Then 
 AR_InvoiceHeader.AmountPaid * max(isnull(AR_InvoiceDetail.TaxAmount,0)) / (100 + max(isnull(AR_InvoiceDetail.TaxAmount,0)))
 else 
(sum(AR_InvoiceDetail.GrossAmount) * isnull(max(AR_InvoiceDetail.TaxAmount),0) / 100) end As TaxAmount

,(sum(AR_InvoiceDetail.GrossAmount)  + (sum(AR_InvoiceDetail.GrossAmount) * isnull(max(AR_InvoiceDetail.TaxAmount),0) / 100)) as WithTaxAmount

 ,AR_SET_Customer.AddressLine1, AR_SET_Customer.CityID , SET_City.CityName
 ,( Case When AR_Invoice_AccountsDetail.TransactionType = 'Credit' Then 
ABS((AR_InvoiceHeader.AmountPaid)) -  AR_InvoiceHeader.AmountPaid * max(isnull(AR_InvoiceDetail.TaxAmount,0)) / (100 + max(isnull(AR_InvoiceDetail.TaxAmount,0))) else
sum(AR_InvoiceDetail.GrossAmount) end) + (Case When AR_Invoice_AccountsDetail.TransactionType = 'Credit' Then 
 AR_InvoiceHeader.AmountPaid * max(isnull(AR_InvoiceDetail.TaxAmount,0)) / (100 + max(isnull(AR_InvoiceDetail.TaxAmount,0)))
 else 
(sum(AR_InvoiceDetail.GrossAmount) * isnull(max(AR_InvoiceDetail.TaxAmount),0) / 100) end ) as ValueIncluSaleTax
 ,AR_InvoiceHeader.InvoiceID,AR_ClaimInvoiceDetail.ClaimDate
,AR_ClaimInvoiceDetail.ClaimHeaderID,AR_ClaimInvoiceDetail.ClaimDetailID
from AR_InvoiceHeader
inner join AR_InvoiceDetail on AR_InvoiceDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID
and AR_InvoiceDetail.CompanyID = AR_InvoiceHeader.CompanyID
and AR_InvoiceDetail.PeriodID = AR_InvoiceHeader.PeriodID
and AR_InvoiceDetail.FiscalID = AR_InvoiceHeader.FiscalID
and AR_InvoiceDetail.Status=1

Left Join AR_Invoice_AccountsDetail on  AR_Invoice_AccountsDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID
and AR_Invoice_AccountsDetail.CompanyID = AR_InvoiceHeader.CompanyID
and AR_Invoice_AccountsDetail.PeriodID = AR_InvoiceHeader.PeriodID
and AR_Invoice_AccountsDetail.FiscalID =  AR_InvoiceHeader.FiscalID
AND AR_Invoice_AccountsDetail.Status = 1 

inner join Set_DocumentType on Set_DocumentType.DocumentTypeID = AR_InvoiceHeader.InvoiceType
and Set_DocumentType.CompanyID = AR_InvoiceHeader.CompanyID
and Set_DocumentType.PeriodID = AR_InvoiceHeader.PeriodID

inner join AR_SET_Customer on AR_SET_Customer.CustomerID = AR_InvoiceHeader.VendorID
--and ar_set_customer.CompanyID = AR_InvoiceHeader.CompanyID
and ar_set_customer.PeriodID = AR_InvoiceHeader.PeriodID

left Join SET_City on SET_City.CityID = ar_set_customer.CityID
And SET_City.Status=1 and ar_set_customer.CityID <> -1

inner join AR_SET_BusinessPartnerTypes on AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_InvoiceHeader.VendorID
inner join AR_SET_BusinessPartner on AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode

inner join AR_ClaimInvoiceDetail on AR_ClaimInvoiceDetail.InvoiceID = AR_InvoiceHeader.InvoiceID
and AR_ClaimInvoiceDetail.CompanyID = AR_InvoiceHeader.CompanyID
and AR_ClaimInvoiceDetail.PeriodID = AR_InvoiceHeader.PeriodID
and AR_ClaimInvoiceDetail.status = 1

where AR_InvoiceHeader.CompanyID = @CompanyID and AR_InvoiceHeader.PeriodID = @PeriodID
AND AR_InvoiceHeader.Status=1
and AR_ClaimInvoiceDetail.ClaimHeaderID = @ClaimHeaderID
and AR_SET_BusinessPartner.BusinessPartnerID = 1
{1}
{2}
group by AR_SET_Customer.CustomerName,AR_InvoiceHeader.InvoiceReceivedDate,AR_InvoiceHeader.VendorReferenceNumber,
  AR_InvoiceHeader.InvoiceNumber,  AR_Invoice_AccountsDetail.TransactionType
,Set_DocumentType.DocumentType,AR_SET_Customer.NTNNumber,AR_SET_Customer.STRNNumber,AR_SET_Customer.CNICNumber,
AR_SET_Customer.AddressLine1, AR_SET_Customer.CityID , SET_City.CityName,AR_InvoiceHeader.AmountPaid,AR_InvoiceHeader.InvoiceID,AR_ClaimInvoiceDetail.ClaimDate
,AR_ClaimInvoiceDetail.ClaimHeaderID,AR_ClaimInvoiceDetail.ClaimDetailID
) as TAB 
{0}
";

                #endregion

                StringBuilder sb = new StringBuilder();
                StringBuilder sbGroup = new StringBuilder();
                StringBuilder sbTypeFilter = new StringBuilder();
                StringBuilder sbBLFILTER = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ClaimHeaderID", ClaimHeaderID);


                string Query = "";
                Query = Query_VendorWiseSaleTax;

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, sb.ToString(), sbGroup.ToString(), sbTypeFilter.ToString()), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDatawithOthertable(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, SqlTransaction objTransaction = null)
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

                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_SelectAll_WithPagingWithOtherTable + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPagingWithOtherTable + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_WithPagingWithOtherTable + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPagingWithOtherTable + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAll_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static bool DeleteDataByClaimHeaderID(SqlTransaction objTransaction, int claimHeaderID, int companyID, int periodID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@ClaimHeaderID", claimHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                const string Query_Delete_ByKey = @" delete from [AR_ClaimInvoiceDetail] where [ClaimHeaderID] = @ClaimHeaderID and [CompanyID] = @CompanyID and [PeriodID] = @PeriodID ";


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

    }
}
