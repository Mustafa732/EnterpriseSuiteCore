using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using System.Text;

namespace FnI.BusinessAccessLayer.Sales
{
    public partial class SAL_DiscountTransactionDetail
    {

        #region Constant String
        const string Query_Discount_Transaction_Detail_On_HeaderID = @" SELECT * FROM [SAL_DiscountTransactionDetail] WHERE [CompanyID] = @CompanyID and [DiscountTransactionHeaderID] = @DiscountTransactionHeaderID and [PeriodID] = @PeriodID and Status=1";

        const string Discount_Transactio_Detail_OnHeaderID = @"SELECT distinct Row_number() OVER(Order by [DiscountTransactionDetailID]) as Row
    ,SAL_DTD.[CompanyID]
    ,SAL_DTD.[PeriodID]
    ,SAL_DTD.[DiscountTransactionHeaderID]
    ,SAL_DTD.[DiscountTransactionDetailID]
    ,SAL_DTD.[DiscountFrequencyID]
    ,SAL_DTD.[SalesInvoiceID]
    ,SAL_InvoiceHEader.InvoiceNumber
    ,SAL_DTD.[CustomerTypeID],dbo.fn_MultiDesc(AR_SET_Customer.CustomerID) as BusinessTypeName
    ,SAL_DTD.[CustomerID],AR_SET_Customer.CustomerName
    ,SAL_DTD.[DealerID],DealerName.CustomerName as DealerName
    ,SAL_DTD.[LocationID],SET_Location.LocationName
    ,sum(SAL_DTD.[InvoiceAmount]) as InvoiceAmount,sum(SAL_InvoiceDetail.QuantityInvoiced) as Quantity
    ,SAL_DTD.[SalesPersonID],SalePerson.CustomerName as SalesPersonName
    ,SAL_DTD.[Status]
  FROM [SAL_DiscountTransactionDetail] SAL_DTD
  
  inner join SAL_DiscountTransactionHeader SAL_DTH on SAL_DTH.[DiscountTransactionHeaderID] = SAL_DTD.[DiscountTransactionHeaderID]
  AND SAL_DTH.CompanyID = SAL_DTD.CompanyID
  And SAL_DTH.PeriodID = SAL_DTD.PeriodID

inner join SAL_INvoiceHeader on SAL_InvoiceHEader.InvoiceID = SAL_DTD.[SalesInvoiceID]
  And SAL_INvoiceHeader.companyID = SAL_DTD.companyID
  And SAL_INvoiceHeader.periodID = SAL_DTD.PeriodID
  
  inner join SAL_InvoiceDetail on SAL_InvoiceDetail .InvoiceHeaderID = SAL_InvoiceHeader.InvoiceID
  And SAL_INvoiceHeader.companyID = SAL_InvoiceDetail.companyID
  And SAL_INvoiceHeader.periodID = SAL_InvoiceDetail.PeriodID
  
  inner join SAL_DiscountFrequencyHeader SAL_DFH on SAL_DFH.DiscountFrequencyID = SAL_DTD.DiscountFrequencyID
  AND SAL_DFH.CompanyID = SAL_DTD.CompanyID
  And SAL_DFH.PeriodID = SAL_DTD.PeriodID
  
left join AR_SET_BusinessPartner on AR_SET_BusinessPartner.BusinessPartnerID=SAL_DTD.CustomerTypeID

  left join AR_SET_BusinessPartnerTypes on AR_SET_BusinessPartnerTypes.BusinessPartnerID=SAL_DTD.[SalesPersonID]
  left join AR_SET_Customer on AR_SET_Customer.CustomerID = SAL_DTD.CustomerID
                                and AR_SET_Customer.CompanyID =  SAL_DTD.CompanyID
                                left join AR_SET_Customer SalePerson on SalePerson.CustomerID = SAL_DTD.SalesPersonID
                                left join AR_SET_Customer DealerName on DealerName.CustomerID = SAL_DTD.DealerID
                                and DealerName.CompanyID =  SAL_DTD.CompanyID
                                left join SET_Location on SET_Location.LocationID=SAL_DTD.LocationID
WHERE SAL_DTD.[CompanyID] =@CompanyID and SAL_DTD.[PeriodID] =@PeriodID And SAL_DTD.[DiscountTransactionHeaderID]= @DiscountTransactionHeaderID
group by
SAL_DTD.[CompanyID],SAL_DTD.[PeriodID],SAL_DTD.[DiscountTransactionHeaderID],SAL_DTD.[DiscountTransactionDetailID],SAL_DTD.[DiscountFrequencyID]
    ,SAL_DTD.[SalesInvoiceID],SAL_InvoiceHEader.InvoiceNumber,SAL_DTD.[CustomerTypeID],dbo.fn_MultiDesc(AR_SET_Customer.CustomerID),SAL_DTD.[CustomerID],AR_SET_Customer.CustomerName,SAL_DTD.[DealerID],DealerName.CustomerName,SAL_DTD.[LocationID],SET_Location.LocationName,SAL_DTD.[SalesPersonID],SalePerson.CustomerName,SAL_DTD.[Status]
"; 

        //SELECT SAL_DTD.[CompanyID]
        //      ,SAL_DTD.[PeriodID]
        //      ,SAL_DTD.[DiscountTransactionHeaderID]
        //      ,SAL_DTD.[DiscountTransactionDetailID]
        //      ,SAL_DTD.[DiscountFrequencyID]
        //      ,SAL_DTD.[SalesInvoiceID]
        //      ,SAL_DTD.[CustomerTypeID],SAL_BT.BusinessTypeID,SAL_BT.BusinessTypeName
        //      ,SAL_DTD.[CustomerID], SAL_ASC.CustomerID,SAL_ASC.CustomerName
        //      ,SAL_DTD.[DealerID], SAL_ASCT.CustomerID,SAL_ASCT.CustomerName as DealerName
        //      ,SAL_DTD.[LocationID],SAL_SL.LocationID,SAL_SL.LocationName
        //      ,SAL_DTD.[InvoiceAmount]
        //      ,SAL_DTD.[SalesPersonID],SAL_ASCDS.CustomerID,SAL_ASCDS.CustomerName as SalesPersonName
        //      ,SAL_DTD.[Status]
        //  FROM [Ansari_HO].[dbo].[SAL_DiscountTransactionDetail] SAL_DTD
        //  
        //  
        //Left Join [SET_BusinessType] SAL_BT
        //on SAL_BT.BusinessTypeID=SAL_DTD.[CustomerTypeID]
        //
        //
        //Left Join [AR_SET_Customer] SAL_ASC
        //ON SAL_ASC.CompanyID=SAL_DTD.CompanyID
        //AND SAL_ASC.PeriodID=SAL_DTD.PeriodID
        //AND SAL_ASC.CustomerID=SAL_DTD.[CustomerID]
        //
        //
        //LEFT JOIN [AR_SET_Customer] SAL_ASCT
        //ON SAL_ASCT.CompanyID=SAL_DTD.CompanyID
        //AND SAL_ASCT.PeriodID=SAL_DTD.PeriodID
        //AND SAL_ASCT.CustomerID=SAL_DTD.[DealerID]
        //
        //
        //LEFT JOIN [SET_Location] SAL_SL
        //ON SAL_SL.LocationID=SAL_DTD.LocationID
        //
        //
        //LEFT JOIN [AR_SET_Customer] SAL_ASCDS
        //ON SAL_ASCDS.CompanyID=SAL_DTD.CompanyID
        //AND SAL_ASCDS.PeriodID=SAL_DTD.PeriodID
        //AND SAL_ASCDS.CustomerID=SAL_DTD.SalesPerSonID
        //  

        #endregion

        #region Class Methods

        public static DataTable GetDataByDiscountTransitionDetails(int companyID, int periodID,int DiscountTransactionHeaderID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DiscountTransactionHeaderID", DiscountTransactionHeaderID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Discount_Transactio_Detail_OnHeaderID, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Discount_Transactio_Detail_OnHeaderID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text,Discount_Transactio_Detail_OnHeaderID,sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dt = ds.Tables[0];
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
