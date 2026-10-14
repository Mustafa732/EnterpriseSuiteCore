using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using System.Text;
using FnI.BusinessAccessLayer.GL.Transaction;
using FnI.BusinessAccessLayer.Inventory.Transaction;
using FnI.BusinessAccessLayer.Setup;


namespace FnI.BusinessAccessLayer.Sales
{
   public partial class SAL_DiscountTransactionHeader
   {
       #region Constant String

       const string Query_GetData_DiscountTransaction_Header_Detail = @"SELECT sal_dth.[CompanyID],sal_dth.[PeriodID],SAL_DTH.DiscountTransactionHeaderID,SAL_DtH.DiscountFrequencyID,SAL_DtH.[CustomerType] as CustomerTypeID,AR_SET_BusinessPartner.BusinessPartnerName as CustomerType,SAL_DTH.[DiscountOnCustomerType],SAL_DTH.[CustomerID],AR_SET_Customer.CustomerName,SAL_DTH.[DiscountOnCustomer],SAL_DTH.[DealerID],DealerName.CustomerName as Dealer
,SAL_DTH.RunDateTime,SAL_DTH.DateFrom,SAL_DTH.DateTo,SAL_DTH.InputValue,SAL_DTH.SelectedFrequency,SAL_DTH.[From] ,SAL_DTH.[To],SAL_DTH.[CalculatedAmount]
,SAL_DtH.[DiscountOnDealer],SAL_DtH.[LocationID],SET_Location.LocationName,SAL_DTH.[DiscountOnLocation],SAL_DTH.[ItemCategoryID],INV_SET_Category.CategoryName,SAL_DTH.[DiscountOnItemCategory],SAL_DTH.[ItemID],INV_Item.Description as ItemName,SAL_DTH.[DiscountOnItem],SAL_DTH.[SalesPersonID],SalePerson.CustomerName as SalePersonName
,SAL_DtH.DiscountSalesPersonID,SAL_DtH.FrequencyID as Frequency,case SAL_DTH.[FrequencyID] when '1' then 'instant' when '2' then 'Invoicewise' when '3' then 'DayWise' when '4' then 'Weekly' when '5' then 'Monthly' when '6' then 'yearly' else '' end as FrequencyName ,SAL_DtH.PeriodicCount as Period,SAL_DTH.[Cumulative],SAL_DFH.[DiscountAmount],SAL_DFH.[DiscountPercentage],SAL_DTH.[GLCode],
SAL_DTH.[Status],SAL_DTH.DiscountOnQuantity,SAL_DTH.TotalQuantity,SAL_DTH.[DiscountOnTotalAmount] ,SAL_DTH.[TotalAmount]
,SAL_DFH.[HeaderName],SAL_DtH.[CreatedBy],SAL_DtH.[CreateDate] ,SAL_DtH.[CreatedByValue],SAL_DtH.[UpdatedBy],SAL_DtH.[UpdateDate],SAL_DTH.[UpdatedByValue]
  
 FROM  Sal_DiscountTransactionHeader SAL_DTH
 
 inner join [SAL_DiscountFrequencyHeader] SAL_DFH on SAL_DFH.DiscountFrequencyID= SAL_DTH.DiscountFrequencyID
 AND SAL_DFH.CompanyID = SAL_DTH.CompanyID
 AND SAL_DFH.PeriodID= SAL_DTH.PeriodID
 
left join AR_SET_BusinessPartner on AR_SET_BusinessPartner.BusinessPartnerID=SAL_DtH.CustomerType 
                                 left join AR_SET_BusinessPartnerTypes on AR_SET_BusinessPartnerTypes.BusinessPartnerID=SAL_DTH.[SalesPersonID] 
                                 left join INV_Item on INV_Item.ItemID = SAL_DTH.ItemID
                                 left join INV_SET_Category on INV_SET_Category.CategoryID=SAL_DTH.ItemcategoryID
                                left join AR_SET_Customer on AR_SET_Customer.CustomerID = SAL_DTH.CustomerID
                                and AR_SET_Customer.CompanyID =  SAL_DTH.CompanyID
                                left join AR_SET_Customer SalePerson on SalePerson.CustomerID = SAL_DTH.SalesPersonID
                                left join AR_SET_Customer DealerName on DealerName.CustomerID = SAL_DTH.DealerID
                                and DealerName.CompanyID =  SAL_DTH.CompanyID
                                left join SET_Location on SET_Location.LocationID=SAL_DTH.LocationID

           WHERE SAL_DTH.[CompanyID] = @CompanyID and SAL_DTH.[DiscountTransactionHeaderID] = @DiscountTransactionHeaderID and SAL_DTH.[PeriodID] = @PeriodID 
";
          
//SELECT  SAL_DTH.[CompanyID],SAL_DTH.[PeriodID],SAL_DTH.[DiscountTransactionHeaderID],SAL_DTH.[RunDateTime],SAL_DTH.[DateFrom],SAL_DTH.[DateTo]
//      ,SAL_DTH.[InputValue],SAL_DTH.[SelectedFrequency],SAL_DTH.[From],SAL_DTH.[To],SAL_DTH.[CalculatedAmount],SAL_DTH.[DiscountFrequencyID],SAL_DTH.HeaderName
//      ,SAL_DTH.[CustomerType],SAL_BT.BusinessTypeID,SAL_BT.BusinessTypeName, SAL_DTH.[DiscountOnCustomerType],SAL_DTH.[CustomerID]
//      ,SAL_ASC.CustomerID,SAL_ASC.CustomerName,SAL_DTH.[DiscountOnCustomer],SAL_DTH.[DealerID],SAL_ASCT.CustomerID
//      ,SAL_ASCT.CustomerName as DealerName,SAL_DTH.[DiscountOnDealer],SAL_DTH.[LocationID],SAL_SL.LocationID,SAL_SL.LocationName
//      ,SAL_DTH.[DiscountOnLocation],SAL_DTH.[ItemCategoryID],SAL_IC.CategoryID,LTRIM(SAL_IC.CategoryName) as CategoryName
//      ,SAL_DTH.[DiscountOnItemCategory],SAL_DTH.[ItemID],SAL_I.ItemID,SAL_I.Description, SAL_DTH.[DiscountOnItem]
//      ,SAL_DTH.[SalesPersonID],SAL_ASCY.CustomerID as DHSalesPersonID,SAL_ASCY.CustomerName as DHSalesPersonName
//      ,SAL_DTH.[DiscountSalesPersonID],SAL_DTH.[FrequencyID],SAL_DTH.[PeriodicCount],SAL_DTH.[Cumulative]
//      ,SAL_DTH.[TotalAmount],SAL_DTH.[DiscountOnTotalAmount],SAL_DTH.[DiscountAmount],SAL_DTH.[DiscountPercentage]
//      ,SAL_DTH.[GLCode],SAL_DTH.[Status],SAL_DTD.CustomerTypeID,SAL_DTD.CustomerID,SAL_ASCD.CustomerID
//      ,SAL_ASCD.CustomerName as DTCustomerName,SAL_DTD.DealerID,SAL_ASCDT.CustomerID,SAL_ASCDT.CustomerName as DTDealerName
//      ,SAL_DTD.LocationID,SAL_SLT.LocationID,SAL_SLT.LocationName as DTLocationName, SAL_DTD.SalesPerSonID,SAL_ASCDS.CustomerID
//      ,SAL_ASCDS.CustomerName as DTSalesPersonName 
//      FROM [SAL_DiscountTransactionHeader] SAL_DTH
//
//Inner join  [SAL_DiscountTransactionDetail] SAL_DTD
//on SAL_DTD.CompanyID=SAL_DTH.CompanyID
//AND SAL_DTD.PeriodID=SAL_DTH.PeriodID
//AND SAL_DTD.DiscountTransactionHEaderID=SAL_DTH.DiscountTransactionHEaderID
//
//Left Join [SAL_DiscountFrequencyHeader] SALDFH
//ON SALDFH.CompanyID=SAL_DTH.CompanyID
//AND SALDFH.PeriodID=SAL_DTH.PeriodID
//AND SALDFH.DiscountFrequencyID=SAL_DTH.DiscountFrequencyID
//
//Left Join [SET_BusinessType] SAL_BT
//on SAL_BT.BusinessTypeID=SAL_DTH.CustomerType
//
//Left Join [AR_SET_Customer] SAL_ASC
//ON SAL_ASC.CompanyID=SAL_DTH.CompanyID
//AND SAL_ASC.PeriodID=SAL_DTH.PeriodID
//AND SAL_ASC.CustomerID=SAL_DTH.CustomerID
//
//LEFT JOIN [AR_SET_Customer] SAL_ASCT
//ON SAL_ASCT.CompanyID=SAL_DTH.CompanyID
//AND SAL_ASCT.PeriodID=SAL_DTH.PeriodID
//AND SAL_ASCT.CustomerID=SAL_DTH.DealerID
//
//LEFT JOIN [SET_Location] SAL_SL
//ON SAL_SL.LocationID=SAL_DTH.LocationID
//
//LEFT JOIN [INV_SET_Category] SAL_IC
//ON SAL_IC.CompanyID=SAL_DTH.CompanyID
//AND SAL_IC.CategoryID=SAL_DTH.ItemCategoryID
//
//LEFT JOIN [INV_Item] SAL_I
//ON SAL_I.CompanyID=SAL_DTH.CompanyID
//AND SAL_I.ItemID=SAL_DTH.ItemID
//
//
//Left Join [AR_SET_Customer] SAL_ASCY
//ON SAL_ASCY.CompanyID=SAL_DTH.CompanyID
//AND SAL_ASCY.PeriodID=SAL_DTH.PeriodID
//AND SAL_ASCY.CustomerID=SAL_DTH.[SalesPersonID]
//
//LEFT JOIN [AR_SET_Customer] SAL_ASCD
//ON SAL_ASCD.CompanyID=SAL_DTD.CompanyID
//AND SAL_ASCD.PeriodID=SAL_DTD.PeriodID
//AND SAL_ASCD.CustomerID=SAL_DTD.CustomerID
//
//LEFT JOIN [AR_SET_Customer] SAL_ASCDT
//ON SAL_ASCDT.CompanyID=SAL_DTD.CompanyID
//AND SAL_ASCDT.PeriodID=SAL_DTD.PeriodID
//AND SAL_ASCDT.CustomerID=SAL_DTD.DealerID
//
//Left Join [SET_Location]SAL_SLT
//ON SAL_SLT.LocationID=SAL_DTD.LocationID
//
//LEFT JOIN [AR_SET_Customer] SAL_ASCDS
//ON SAL_ASCDS.CompanyID=SAL_DTD.CompanyID
//AND SAL_ASCDS.PeriodID=SAL_DTD.PeriodID
//AND SAL_ASCDS.CustomerID=SAL_DTD.SalesPerSonID



       const string Query_GetData_DiscountTransaction_Header_Detail_With_Pagging = @"SELECT ROW_NUMBER() Over (Order by SAL_DTH.[DiscountTransactionHeaderID]) as 'Row'
,sal_dth.[CompanyID],sal_dth.[PeriodID],SAL_DTH.RunDateTime,SAL_DTH.DiscountTransactionHeaderID,SAL_DtH.DiscountFrequencyID,SAL_DtH.[CustomerType] as CustomerTypeID,AR_SET_BusinessPartner.BusinessPartnerName as CustomerType,SAL_DTH.[DiscountOnCustomerType],SAL_DTH.[CustomerID],AR_SET_Customer.CustomerName,SAL_DTH.[DiscountOnCustomer],SAL_DTH.[DealerID]
,SAL_DtH.[DiscountOnDealer],SAL_DtH.[LocationID],SET_Location.LocationName,SAL_DTH.[DiscountOnLocation],SAL_DTH.[ItemCategoryID],INV_SET_Category.CategoryName,SAL_DTH.[DiscountOnItemCategory],SAL_DTH.[ItemID],INV_Item.Description as ItemName,SAL_DTH.[DiscountOnItem],SAL_DTH.[SalesPersonID],SalePerson.CustomerName as SalePersonName
,SAL_DtH.DiscountSalesPersonID,SAL_DtH.FrequencyID as Frequency,case SAL_DTH.[FrequencyID] when '1' then 'instant' when '2' then 'Invoicewise' when '3' then 'DayWise' when '4' then 'Weekly' when '5' then 'Monthly' when '6' then 'yearly' else '' end as FrequencyName ,SAL_DtH.PeriodicCount as Period,SAL_DTH.[Cumulative],SAL_DTH.[DiscountAmount],SAL_DTH.[DiscountPercentage],SAL_DTH.[GLCode],SAL_DTH.[Status],
SAL_DTH.[DiscountOnTotalAmount] ,SAL_DTH.[TotalAmount],SAL_DTH.DiscountOnQuantity,SAL_DTH.TotalQuantity
,SAL_DFH.[HeaderName],SAL_DtH.[CreatedBy],SAL_DtH.[CreateDate] ,SAL_DtH.[CreatedByValue],SAL_DtH.[UpdatedBy],SAL_DtH.[UpdateDate],SAL_DTH.[UpdatedByValue]
  
 FROM  Sal_DiscountTransactionHeader SAL_DTH
 
 inner join [SAL_DiscountFrequencyHeader] SAL_DFH on SAL_DFH.DiscountFrequencyID= SAL_DTH.DiscountFrequencyID
 AND SAL_DFH.CompanyID = SAL_DTH.CompanyID
 AND SAL_DFH.PeriodID= SAL_DTH.PeriodID
 
left join AR_SET_BusinessPartner on AR_SET_BusinessPartner.BusinessPartnerID=SAL_DtH.CustomerType 
                                 left join AR_SET_BusinessPartnerTypes on AR_SET_BusinessPartnerTypes.BusinessPartnerID=SAL_DTH.[SalesPersonID] 
                                 left join INV_Item on INV_Item.ItemID = SAL_DTH.ItemID
                                 left join INV_SET_Category on INV_SET_Category.CategoryID=SAL_DTH.ItemcategoryID
                                left join AR_SET_Customer on AR_SET_Customer.CustomerID = SAL_DTH.CustomerID
                                --and AR_SET_Customer.CompanyID =  SAL_DTH.CompanyID
                                left join AR_SET_Customer SalePerson on SalePerson.CustomerID = SAL_DTH.SalesPersonID
                                left join AR_SET_Customer DealerName on DealerName.CustomerID = SAL_DTH.DealerID
                                and DealerName.CompanyID =  SAL_DTH.CompanyID
                                left join SET_Location on SET_Location.LocationID=SAL_DTH.LocationID";
//@"SELECT ROW_NUMBER() Over (Order by SAL_DTH.CompanyID, SAL_DTH.PeriodID, SAL_DTH.[DiscountTransactionHeaderID]) as 'Row',  
//	   SAL_DTH.[CompanyID],SAL_DTH.[PeriodID],SAL_DTH.[DiscountTransactionHeaderID],SAL_DTH.[RunDateTime],SAL_DTH.[DateFrom],SAL_DTH.[DateTo]
//      ,SAL_DTH.[InputValue],SAL_DTH.[SelectedFrequency],SAL_DTH.[From],SAL_DTH.[To],SAL_DTH.[CalculatedAmount],SAL_DTH.[DiscountFrequencyID],SAL_DTH.HeaderName
//      ,SAL_DTH.[CustomerType],SAL_BT.BusinessTypeID,SAL_BT.BusinessTypeName, SAL_DTH.[DiscountOnCustomerType],SAL_DTH.[CustomerID]
//      ,SAL_ASC.CustomerID ASC_CustomerID,SAL_ASC.CustomerName,SAL_DTH.[DiscountOnCustomer],SAL_DTH.[DealerID],SAL_ASCT.CustomerID as Dealer_CustomerID
//      ,SAL_ASCT.CustomerName as DealerName,SAL_DTH.[DiscountOnDealer],SAL_DTH.[LocationID] as DH_LocationID,SAL_SL.LocationID as SL_LocationID,SAL_SL.LocationName
//      ,SAL_DTH.[DiscountOnLocation],SAL_DTH.[ItemCategoryID],SAL_IC.CategoryID,LTRIM(SAL_IC.CategoryName) as CategoryName
//      ,SAL_DTH.[DiscountOnItemCategory],SAL_DTH.[ItemID],SAL_I.ItemID as I_ItemID,SAL_I.Description, SAL_DTH.[DiscountOnItem]
//      ,SAL_DTH.[SalesPersonID] DTHSalesPersonID,SAL_ASCY.CustomerID as DHSalesPersonID,SAL_ASCY.CustomerName as DHSalesPersonName
//      ,SAL_DTH.[DiscountSalesPersonID],SAL_DTH.[FrequencyID],SAL_DTH.[PeriodicCount],SAL_DTH.[Cumulative]
//      ,SAL_DTH.[TotalAmount],SAL_DTH.[DiscountOnTotalAmount],SAL_DTH.[DiscountAmount],SAL_DTH.[DiscountPercentage]
//      ,SAL_DTH.[GLCode],SAL_DTH.[Status],SAL_DTD.CustomerTypeID,SAL_DTD.CustomerID DT_CustomerID,SAL_ASCD.CustomerID as ASCD_CustomerID
//      ,SAL_ASCD.CustomerName as DTCustomerName,SAL_DTD.DealerID as DTDealerID,SAL_ASCDT.CustomerID as DTDealer_CustomerID,SAL_ASCDT.CustomerName as DTDealerName
//      ,SAL_DTD.LocationID DTD_LocationID,SAL_SLT.LocationID as SLT_LocationID,SAL_SLT.LocationName as DTLocationName, SAL_DTD.SalesPerSonID as DTDSalesPersonID
//      ,SAL_ASCDS.CustomerID as SalesPrsn_CustomerID
//      ,SAL_ASCDS.CustomerName as DTSalesPersonName 
//      FROM [SAL_DiscountTransactionHeader] SAL_DTH
//
//Inner join  [SAL_DiscountTransactionDetail] SAL_DTD
//on SAL_DTD.CompanyID=SAL_DTH.CompanyID
//AND SAL_DTD.PeriodID=SAL_DTH.PeriodID
//AND SAL_DTD.DiscountTransactionHEaderID=SAL_DTH.DiscountTransactionHEaderID
//
//Left Join [SAL_DiscountFrequencyHeader] SALDFH
//ON SALDFH.CompanyID=SAL_DTH.CompanyID
//AND SALDFH.PeriodID=SAL_DTH.PeriodID
//AND SALDFH.DiscountFrequencyID=SAL_DTH.DiscountFrequencyID
//
//Left Join [SET_BusinessType] SAL_BT
//on SAL_BT.BusinessTypeID=SAL_DTH.CustomerType
//
//Left Join [AR_SET_Customer] SAL_ASC
//ON SAL_ASC.CompanyID=SAL_DTH.CompanyID
//AND SAL_ASC.PeriodID=SAL_DTH.PeriodID
//AND SAL_ASC.CustomerID=SAL_DTH.CustomerID
//
//LEFT JOIN [AR_SET_Customer] SAL_ASCT
//ON SAL_ASCT.CompanyID=SAL_DTH.CompanyID
//AND SAL_ASCT.PeriodID=SAL_DTH.PeriodID
//AND SAL_ASCT.CustomerID=SAL_DTH.DealerID
//
//LEFT JOIN [SET_Location] SAL_SL
//ON SAL_SL.LocationID=SAL_DTH.LocationID
//
//LEFT JOIN [INV_SET_Category] SAL_IC
//ON SAL_IC.CompanyID=SAL_DTH.CompanyID
//AND SAL_IC.CategoryID=SAL_DTH.ItemCategoryID
//
//LEFT JOIN [INV_Item] SAL_I
//ON SAL_I.CompanyID=SAL_DTH.CompanyID
//AND SAL_I.ItemID=SAL_DTH.ItemID
//
//
//Left Join [AR_SET_Customer] SAL_ASCY
//ON SAL_ASCY.CompanyID=SAL_DTH.CompanyID
//AND SAL_ASCY.PeriodID=SAL_DTH.PeriodID
//AND SAL_ASCY.CustomerID=SAL_DTH.[SalesPersonID]
//
//LEFT JOIN [AR_SET_Customer] SAL_ASCD
//ON SAL_ASCD.CompanyID=SAL_DTD.CompanyID
//AND SAL_ASCD.PeriodID=SAL_DTD.PeriodID
//AND SAL_ASCD.CustomerID=SAL_DTD.CustomerID
//
//LEFT JOIN [AR_SET_Customer] SAL_ASCDT
//ON SAL_ASCDT.CompanyID=SAL_DTD.CompanyID
//AND SAL_ASCDT.PeriodID=SAL_DTD.PeriodID
//AND SAL_ASCDT.CustomerID=SAL_DTD.DealerID
//
//Left Join [SET_Location]SAL_SLT
//ON SAL_SLT.LocationID=SAL_DTD.LocationID
//
//LEFT JOIN [AR_SET_Customer] SAL_ASCDS
//ON SAL_ASCDS.CompanyID=SAL_DTD.CompanyID
//AND SAL_ASCDS.PeriodID=SAL_DTD.PeriodID
//AND SAL_ASCDS.CustomerID=SAL_DTD.SalesPerSonID


       const string Discount_Transaction_Crystal_Report_Query = @"
SELECT  SAL_DTH.[CompanyID],SAL_DTH.[PeriodID],SAL_DTH.[DiscountTransactionHeaderID],SAL_DTH.[RunDateTime],SAL_DTH.[DateFrom],SAL_DTH.[DateTo]
      ,SAL_DTH.[InputValue],SAL_DTH.[SelectedFrequency],SAL_DTH.[From],SAL_DTH.[To],SAL_DTH.[CalculatedAmount],SAL_DTH.[DiscountFrequencyID],SAL_DTH.HeaderName
      ,SAL_DTH.[CustomerType],SAL_BT.BusinessTypeID,SAL_BT.BusinessTypeName, SAL_DTH.[DiscountOnCustomerType],SAL_DTH.[CustomerID]
      ,SAL_ASC.CustomerID,SAL_ASC.CustomerName,SAL_DTH.[DiscountOnCustomer],SAL_DTH.[DealerID],SAL_ASCT.CustomerID
      ,SAL_ASCT.CustomerName as DealerName,SAL_DTH.[DiscountOnDealer],SAL_DTH.[LocationID],SAL_SL.LocationID,SAL_SL.LocationName
      ,SAL_DTH.[DiscountOnLocation],SAL_DTH.[ItemCategoryID],SAL_IC.CategoryID,LTRIM(SAL_IC.CategoryName) as CategoryName
      ,SAL_DTH.[DiscountOnItemCategory],SAL_DTH.[ItemID],SAL_I.ItemID,SAL_I.Description, SAL_DTH.[DiscountOnItem]
      ,SAL_DTH.[SalesPersonID],SAL_ASCY.CustomerID as DHSalesPersonID,SAL_ASCY.CustomerName as DHSalesPersonName
      ,SAL_DTH.[DiscountSalesPersonID],SAL_DTH.[FrequencyID],SAL_DTH.[PeriodicCount],SAL_DTH.[Cumulative]
      ,SAL_DTH.[TotalAmount],SAL_DTH.[DiscountOnTotalAmount],SAL_DTH.[DiscountAmount],SAL_DTH.[DiscountPercentage]
      ,SAL_DTH.[GLCode],SAL_DTH.CreateDate,SAL_DTH.CreatedByValue, SAL_DTH.[Status],SAL_DTD.CustomerTypeID,SAL_BTD.BusinessTypeID as BusinessTypeIDDT
      ,SAL_BTD.BusinessTypeName as BusinessTypeNameDT, SAL_DTD.CustomerID as CustomerIDDT,SAL_ASCD.CustomerID
      ,SAL_ASCD.CustomerName as DTCustomerName,SAL_DTD.DealerID as DealerIDDT,SAL_ASCDT.CustomerID,SAL_ASCDT.CustomerName as DTDealerName
      ,SAL_DTD.LocationID as LocationIDDT,SAL_SLT.LocationID,SAL_SLT.LocationName as DTLocationName, SAL_DTD.SalesPerSonID as SalesPerSonIDDT,SAL_ASCDS.CustomerID
      ,SAL_ASCDS.CustomerName as DTSalesPersonName 
      FROM [SAL_DiscountTransactionHeader] SAL_DTH

Inner join  [SAL_DiscountTransactionDetail] SAL_DTD
on SAL_DTD.CompanyID=SAL_DTH.CompanyID
AND SAL_DTD.PeriodID=SAL_DTH.PeriodID
AND SAL_DTD.DiscountTransactionHEaderID=SAL_DTH.DiscountTransactionHEaderID

Left Join [SAL_DiscountFrequencyHeader] SALDFH
ON SALDFH.CompanyID=SAL_DTH.CompanyID
AND SALDFH.PeriodID=SAL_DTH.PeriodID
AND SALDFH.DiscountFrequencyID=SAL_DTH.DiscountFrequencyID

Left Join [SET_BusinessType] SAL_BT
on SAL_BT.BusinessTypeID=SAL_DTH.CustomerType

Left Join [AR_SET_Customer] SAL_ASC
ON SAL_ASC.PeriodID=SAL_DTH.PeriodID
--SAL_ASC.CompanyID=SAL_DTH.CompanyID
AND SAL_ASC.PeriodID=SAL_DTH.PeriodID
AND SAL_ASC.CustomerID=SAL_DTH.CustomerID

LEFT JOIN [AR_SET_Customer] SAL_ASCT
ON SAL_ASCT.CompanyID=SAL_DTH.CompanyID
AND SAL_ASCT.PeriodID=SAL_DTH.PeriodID
AND SAL_ASCT.CustomerID=SAL_DTH.DealerID

LEFT JOIN [SET_Location] SAL_SL
ON SAL_SL.LocationID=SAL_DTH.LocationID

LEFT JOIN [INV_SET_Category] SAL_IC
ON 
--SAL_IC.CompanyID=SAL_DTH.CompanyID AND 
SAL_IC.CategoryID=SAL_DTH.ItemCategoryID

LEFT JOIN [INV_Item] SAL_I
ON SAL_I.CompanyID=SAL_DTH.CompanyID
AND SAL_I.ItemID=SAL_DTH.ItemID

Left Join [AR_SET_Customer] SAL_ASCY
ON SAL_ASCY.CompanyID=SAL_DTH.CompanyID
AND SAL_ASCY.PeriodID=SAL_DTH.PeriodID
AND SAL_ASCY.CustomerID=SAL_DTH.[SalesPersonID]

Left Join [SET_BusinessType] SAL_BTD
on SAL_BTD.BusinessTypeID=SAL_DTD.CustomerTypeID

LEFT JOIN [AR_SET_Customer] SAL_ASCD
ON SAL_ASCD.CompanyID=SAL_DTD.CompanyID
AND SAL_ASCD.PeriodID=SAL_DTD.PeriodID
AND SAL_ASCD.CustomerID=SAL_DTD.CustomerID

LEFT JOIN [AR_SET_Customer] SAL_ASCDT
ON SAL_ASCDT.CompanyID=SAL_DTD.CompanyID
AND SAL_ASCDT.PeriodID=SAL_DTD.PeriodID
AND SAL_ASCDT.CustomerID=SAL_DTD.DealerID

Left Join [SET_Location]SAL_SLT
ON SAL_SLT.LocationID=SAL_DTD.LocationID

LEFT JOIN [AR_SET_Customer] SAL_ASCDS
ON SAL_ASCDS.CompanyID=SAL_DTD.CompanyID
AND SAL_ASCDS.PeriodID=SAL_DTD.PeriodID
AND SAL_ASCDS.CustomerID=SAL_DTD.SalesPerSonID

WHERE SAL_DTH.[CompanyID] = @CompanyID and SAL_DTH.[PeriodID] = @PeriodID AND SAL_DTH.RunDateTime BETWEEN @FromDate AND @ToDate
";


       const string Query_GetInvoiceData = @"SELECT  InvoiceNumber ,InvoiceDate, AmountPaid as InvoiceAmount FROM SAL_InvoiceHeader ";
       #endregion

       #region Class Methods

       public static DataTable GetDataByTransactionHeaderIDKey(int companyID, int discountTransactionHeaderID, int periodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       {
           DataTable dt = null;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@DiscountTransactionHeaderID", discountTransactionHeaderID);
               sqlparams[2] = new SqlParameter("@PeriodID", periodID);

               DataSet ds = null;

               if (objTransaction != null)
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_DiscountTransaction_Header_Detail, sqlparams);
               else if (objConnection != null)
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_DiscountTransaction_Header_Detail, sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_DiscountTransaction_Header_Detail, sqlparams);

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

       public static DataTable DiscountTansactionCrystalReport(int CompanyID, int PeriodID, DateTime? FromDate, DateTime? ToDate)
       {
           DataTable dt=null;
           try
           {
               SqlParameter[] sqlParams = new SqlParameter[4];
               sqlParams[0] = new SqlParameter("CompanyID", CompanyID);
               sqlParams[1] = new SqlParameter("PeriodID", PeriodID);
               sqlParams[2] = new SqlParameter("FromDate", FromDate);
               sqlParams[3] = new SqlParameter("ToDate", ToDate);
               SqlConnection objConnection = null;
               DataSet ds = null;
               if (objConnection == null)
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, Discount_Transaction_Crystal_Report_Query, sqlParams);
               else
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Discount_Transaction_Crystal_Report_Query, sqlParams);

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

       public static DataTable GetAllActiveDataSearch(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                   dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_GetData_DiscountTransaction_Header_Detail_With_Pagging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty),
                                                    Query_GetData_DiscountTransaction_Header_Detail_With_Pagging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                   ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
               }
               else
                   dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_DiscountTransaction_Header_Detail_With_Pagging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }

       public static DataTable GetDataFromInvoice(int companyID, int periodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       {
           DataTable dt = null;
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[2];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);

               DataSet ds = null;

               if (objTransaction != null)
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetInvoiceData, sqlparams);
               else if (objConnection != null)
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetInvoiceData, sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetInvoiceData, sqlparams);

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


       //public static DataTable GetDataByRunDate(int companyID, int periodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       //{
       //    DataTable dt = null;

       //    try
       //    {
       //        SqlParameter[] sqlparams = new SqlParameter[2];
       //        sqlparams[0] = new SqlParameter("@CompanyID", companyID);
       //        sqlparams[1] = new SqlParameter("@PeriodID", periodID);

       //        DataSet ds = null;

       //        if (objTransaction != null)
       //            ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_DiscountTransaction_Header_Detail_With_Pagging, sqlparams);
       //        else if (objConnection != null)
       //            ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_DiscountTransaction_Header_Detail_With_Pagging, sqlparams);
       //        else
       //            ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll, sqlparams);

       //        if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
       //            dt = ds.Tables[0];
       //    }
       //    catch (Exception ex)
       //    {
       //        SqlHelper.LogException(ex);
       //        throw ex;
       //    }

       //    return dt;
       //}
       
       
       
       
       
       #endregion





       public bool InsertDiscountTransactionHeader(INV_StockTransactionHeader sth, List<INV_StockTransactionDetail> colSTDetail, SET_DocumentApprove da, SAL_DiscountTransactionHeader lstDisTransHdr, List<SAL_DiscountTransactionDetail> lstDisTransDetail, List<SAL_INVTransactionApplication> lstinvdet, List<GL_TransactionDetail> colTranDetail, GL_TransactionHeader th)
       {
           bool result = false;
           SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertDiscountTransactionHeader");

           try
           {

               #region For Stock Transaction Header & Detail

               //sth.DocumentNo = int.Parse(f_DiscountTransactionHeaderID.ToString());
               //sth.StockTransactionID = INV_StockTransactionHeader.GetNewStockTransactionID(f_CompanyID, f_PeriodID, sqltrans);
               //sth.Insert(sqltrans);

               //long transactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(f_CompanyID, f_PeriodID, sqltrans);

               //foreach (INV_StockTransactionDetail std in colSTDetail)
               //{
               //    std.StockTransactionID = sth.StockTransactionID;
               //    std.TransactionDetailID = transactionDetailID++;
               //    std.Insert(sqltrans);
               //}

               #endregion

               this.Insert(sqltrans);

            //Saleinvoice data

               int DisTransDetID = SAL_DiscountTransactionDetail.GetNewDiscountTransactionDetailID(f_CompanyID, DiscountTransactionHeaderID, f_PeriodID,sqltrans);
               foreach (SAL_DiscountTransactionDetail objdtd in lstDisTransDetail)
               {

                   objdtd.DiscountTransactionHeaderID = this.DiscountTransactionHeaderID;
                   objdtd.DiscountTransactionDetailID = SAL_DiscountTransactionDetail.GetNewDiscountTransactionDetailID(f_CompanyID, DiscountTransactionHeaderID, f_PeriodID, sqltrans);
                   objdtd.Insert(sqltrans);

                  
               
               }

               
               foreach (SAL_INVTransactionApplication objita in lstinvdet)
               {
                   objita.TransactionApplicationID = SAL_INVTransactionApplication.GetNewTransactionApplicationID(f_CompanyID, f_PeriodID,sqltrans);
                   objita.DiscountHeaderID = this.DiscountTransactionHeaderID;
                   objita.DiscountDetailID = DisTransDetID++;
                   
                   objita.Insert(sqltrans);

               
               
               }

               //lstDisTransDetail.DiscountTransactionHeaderID = this.DiscountTransactionHeaderID;
               //lstDisTransDetail.DiscountTransactionDetailID = SAL_DiscountTransactionDetail.GetNewDiscountTransactionDetailID(f_CompanyID, this.DiscountTransactionHeaderID, f_PeriodID);
               //lstDisTransDetail.Insert();

               #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

               if (th.Status == true)
               {
                   #region Insert In GL_TransactionHeader

                   th.DocumentNo = this.f_DiscountTransactionHeaderID;
                   th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                   th.Insert(sqltrans);

                   #endregion

                   #region Insert In GL_TransactionDetail

                   foreach (GL_TransactionDetail td in colTranDetail)
                   {
                       td.TransactionID = th.TransactionID;
                       td.SourceHeaderID = this.f_DiscountTransactionHeaderID;
                       td.Insert(sqltrans);
                   }

                   #endregion
               }

               #endregion


               #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

               if (th.Status)
                   da.Insert(sqltrans);

               #endregion


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
                   SqlHelper.RollBackTransaction(sqltrans, "InsertDiscountTransactionHeader");
           }

           return result;
       }
   }
}
