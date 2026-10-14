using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Common.Utils;
using DAL.DataAccessLayer;
using System.Text;

namespace FnI.BusinessAccessLayer.Sales
{
    public partial class SAL_DOVehicleInformation
    {
        private int f_RowUniqueID;
        private int f_DID;

        public int RowUniqueID
        {
            get { return f_RowUniqueID; }
            set { f_RowUniqueID = value; }
        }

        public int DID
        {
            get { return f_DID; }
            set { f_DID = value; }
        }

        #region Constant Strings

        const string Query_VehicleDelete_ByKey = @" delete from [SAL_DOVehicleInformation]
	WHERE [CompanyID] = @CompanyID and [DOVehicleInformationID] = @DOVehicleInformationID and [PeriodID] = @PeriodID  ";

        const string Query_SalesVehicleInoices_ = @"Select Distinct
Sal_InvoiceHeader.InvoiceNumber
,Sal_InvoiceHeader.CustomerID
, Sal_InvoiceHeader.InvoiceDate
,Sal_InvoiceDetail.QuantityInvoiced
,Ar_set_Customer.CustomerName
,Ar_set_Customer.FatherName
, Ar_set_Customer.Addressline1
,SET_OperatingUnit.AddressLine2+', '+SET_OperatingUnit.AddressLine3+', '+City.CityName+'.' AS SaleOfficeAddress
,SET_Business.AddressLine2+', '+City.CityName+', '+Set_Country.CountryName+', Tel:'+SET_Business.PhoneNumber+', '+SET_Business.AddressLine3 AS HeadOfficeAddress
,Ar_set_Customer.CNICNumber
,ARS.CustomerName AS Dealer
,SET_OperatingUnit.NTNNumber AS NTN
,SET_OperatingUnit.SalesTaxNumber as STRN
,Sal_InvoiceDetail.ItemId as INvoiceItem
,INV_StocktransactionDetail.ItemID as STItem
,INV_Item.Description
,INV_Item.Description as Model
,INV_Item.ItemTypeID
,SAL_DOVehicleInformation.Engine
,SAL_DOVehicleInformation.Chassis
,Set_Color.ColorName
,ISNULL(Sal_InvoiceDetail.taxAmount,0)taxAmount
,Sal_InvoiceDetail.GrossAmount

,Sal_InvoiceDetail.NetAmount
,SET_OperatingUnit.LogoImage
,BilledTo.CustomerName as BilledToName
,BilledTo.AddressLine1 as BilledToAddress
,BilledtoCity.CityName as BilledToCity
--,Case BilledTo.CNICNumber When '' then BilledTo.NTNNumber Else BilledTo.CNICNumber END as BilledCNIC
,Case BilledTo.CNICNumber When '' then BilledTo.NTNNumber Else 
substring(BilledTo.CNICNumber,0,5)+'-'+
substring(BilledTo.CNICNumber,5,len(BilledTo.CNICNumber)-1) +'-'+
substring(BilledTo.CNICNumber,len(BilledTo.CNICNumber),len(BilledTo.CNICNumber))
END as BilledCNIC
,BilledTo.FatherName as BilledToFatherName
,INV_Item.InventoryItemStatusCode as ItemStatusCode
,INV_Item.CubicCentimeters as CC
,(ISNULL(Sal_InvoiceDetail.taxAmount,0) / 100) *  Sal_InvoiceDetail.GrossAmount as SalesTaxAmount
,INV_StockTransactionHeader.BatteryNo
,INV_StockTransactionHeader.FrontTyreRight
,INV_StockTransactionHeader.FrontTyreleft
,INV_StockTransactionHeader.RearTyreRight
,INV_StockTransactionHeader.RearTyreLeft
,Sal_OrderHeader.ContractNo
  ,convert (varchar(20), Sal_OrderHeader.ContractDate ,106) as ContractDate
From Sal_InvoiceHeader

 Inner JOIN SAL_InvoiceDetail ON SAL_InvoiceDetail.InvoiceHeaderID = SAL_InvoiceHeader.InvoiceID 
    AND SAL_InvoiceDetail.CompanyID = SAL_InvoiceHeader.CompanyID 
    AND SAL_InvoiceDetail.PeriodID = SAL_InvoiceHeader.PeriodID 
    
 Inner JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = SAL_InvoiceDetail.StockHeaderID 
    AND INV_StockTransactionHeader.CompanyID = SAL_InvoiceDetail.CompanyID 
    AND INV_StockTransactionHeader.PeriodID = SAL_InvoiceDetail.PeriodID 
    
 Inner JOIN INV_StockTransactionDetail ON INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID 
    AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyId 
    AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 
 LEFT JOIN SAL_DispatchHeader ON SAL_DispatchHeader.DispatchNo = INV_StockTransactionHeader.DocumentNo 
   AND SAL_DispatchHeader.CompanyID = INV_StockTransactionHeader.CompanyID 
   AND SAL_DispatchHeader.PeriodID = INV_StockTransactionHeader.PeriodID 
   
 LEFT Join Sal_DeliveryOrderDetail ON Sal_DeliveryOrderDetail.CompanyID = INV_StockTransactionDetail.CompanyId 
   AND Sal_DeliveryOrderDetail.PeriodID = INV_StockTransactionDetail.PeriodID 
   AND Sal_DeliveryOrderDetail.DOHeaderID = INV_StockTransactionDetail.SourceHeaderID 
   AND Sal_DeliveryOrderDetail.DODetailID = INV_StockTransactionDetail.SourceLineID 
   
 LEFT Join Sal_DeliveryOrderHeader ON Sal_DeliveryOrderHeader.CompanyID = Sal_DeliveryOrderDetail.CompanyId 
   AND Sal_DeliveryOrderHeader.PeriodID = Sal_DeliveryOrderDetail.PeriodID 
   AND Sal_DeliveryOrderHeader.DeliveryOrderID = Sal_DeliveryOrderDetail.DOHeaderID 
   AND Sal_DeliveryOrderHeader.Status = 1 
   
 LEFT Join SAL_DOVehicleInformation ON SAL_DOVehicleInformation.CompanyID = Sal_DeliveryOrderDetail.CompanyId 
	AND SAL_DOVehicleInformation.PeriodID = Sal_DeliveryOrderDetail.PeriodID 
	AND SAL_DOVehicleInformation.DOHeaderID = Sal_DeliveryOrderDetail.DOHeaderID 
	AND SAL_DOVehicleInformation.DODetailID = Sal_DeliveryOrderDetail.DODetailID 
	AND SAL_DOVehicleInformation.Status = 1 
	
 LEFT Join Sal_OrderDetail ON Sal_OrderDetail.CompanyID = Sal_DeliveryOrderDetail.CompanyId 
	AND Sal_OrderDetail.PeriodID = Sal_DeliveryOrderDetail.PeriodID 
    AND Sal_OrderDetail.OrderDetailID = Sal_DeliveryOrderDetail.SalesOrderDetailID 
    AND Sal_OrderDetail.OrderID = Sal_DeliveryOrderDetail.SalesOrderHeaderID 
    AND Sal_OrderDetail.Status = 1 
    
LEFT JOIN Sal_OrderHeader ON Sal_OrderHeader.CompanyID = Sal_OrderDetail.CompanyId 
	AND Sal_OrderHeader.PeriodID = Sal_OrderDetail.PeriodID 
    AND Sal_OrderHeader.OrderID = Sal_OrderDetail.OrderID 
    AND Sal_OrderHeader.Status = 1 

LEFT JOIN Set_Color ON Set_Color.ColorID = Sal_OrderDetail.ColorID 
		AND Set_Color.Status = 1 

Inner JOIN INV_Item ON INV_Item.ItemID = Sal_OrderDetail.ItemID 
		--AND INV_Item.CompanyID = Sal_OrderDetail.CompanyID 
		AND INV_Item.Status = 1 

Inner JOIN INV_Set_Category ON INV_Set_Category.CategoryID = INV_Item.ItemTypeID 
	--  
    AND INV_Set_Category.Status = 1 

Inner JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = SAL_InvoiceHeader.CustomerID 
	--AND AR_SET_Customer.CompanyID = SAL_InvoiceHeader.CompanyID 
    AND AR_SET_Customer.PeriodID = SAL_InvoiceHeader.PeriodID 

Inner JOIN AR_SET_Customer ARS ON ARS.CustomerID = SAL_OrderHeader.BrokerID 
		AND ARS.CompanyID = SAL_InvoiceHeader.CompanyID 
        AND ARS.PeriodID = SAL_InvoiceHeader.PeriodID 

LEFT JOIN SET_City ON SET_City.CityID = AR_SET_Customer.CityID 

LEFT JOIN [Set_Batch] ON [Set_Batch].[BatchID] = [SAL_OrderHeader].[Season] 
		AND [Set_Batch].[CompanyID] = [SAL_OrderHeader].[CompanyID] 
        AND [Set_Batch].[PeriodID] = [SAL_OrderHeader].[PeriodID] 

LEft JOIN AR_SET_Customer BilledTo ON BilledTo.CustomerID = SAL_InvoiceHeader.billedtoid 
		AND BilledTo.CompanyID = SAL_InvoiceHeader.CompanyID 
        AND BilledTo.PeriodID = SAL_InvoiceHeader.PeriodID 

LEFT JOIN SET_City BilledtoCity ON BilledtoCity.CityID = Billedto.CityID 

left JOIN Set_OperatingUnit ON Set_OperatingUnit.CityID = Set_City.CityID
		AND Set_OperatingUnit.Status = 1
		
left JOIN Set_Business ON Set_Business.BusinessId = Set_OperatingUnit.BusinessId
		AND Set_Business.Status = 1
	
left JOIN Set_Country ON SET_Country.CountryID = SET_City.CountryID
	
left JOIN Set_City City ON City.CountryID = Set_Country.CountryID
		AND Set_Country.Status = 1 ";

        #region Old Query
        /*const string Query_SalesVehicleInoices_ = @"Select Distinct 

Sal_InvoiceHeader.InvoiceNumber
,Sal_InvoiceHeader.CustomerID
, Sal_InvoiceHeader.InvoiceDate
,Sal_InvoiceDetail.QuantityInvoiced
,Ar_set_Customer.CustomerName
,Ar_set_Customer.FatherName
, Ar_set_Customer.Addressline1
,SET_OperatingUnit.AddressLine2+', '+SET_OperatingUnit.AddressLine3+', '+City.CityName+'.' AS SaleOfficeAddress
,SET_Business.AddressLine2+', '+City.CityName+', '+Country.CountryName+', Tel:'+SET_Business.PhoneNumber+', '+SET_Business.AddressLine3 AS HeadOfficeAddress
,Ar_set_Customer.CNICNumber
,Dealer.CustomerName as Dealer
,SET_OperatingUnit.NTNNumber AS NTN
,SET_OperatingUnit.SalesTaxNumber as STRN
,Sal_InvoiceDetail.ItemId as INvoiceItem
,INV_StocktransactionDetail.ItemID as STItem
,INV_Item.Description
,INV_Item.Description as Model
,SAL_DOVehicleInformation.Engine
,SAL_DOVehicleInformation.Chassis
,Set_Color.ColorName
,ISNULL(Sal_InvoiceDetail.taxAmount,0)taxAmount
,Sal_InvoiceDetail.GrossAmount

,Sal_InvoiceDetail.NetAmount
,SET_OperatingUnit.LogoImage
,BilledTo.CustomerName as BilledToName
,BilledTo.AddressLine1 as BilledToAddress
,BilledtoCity.CityName as BilledToCity
--,Case BilledTo.CNICNumber When '' then BilledTo.NTNNumber Else BilledTo.CNICNumber END as BilledCNIC
,Case BilledTo.CNICNumber When '' then BilledTo.NTNNumber Else 
substring(BilledTo.CNICNumber,0,5)+'-'+
substring(BilledTo.CNICNumber,5,len(BilledTo.CNICNumber)-1) +'-'+
substring(BilledTo.CNICNumber,len(BilledTo.CNICNumber),len(BilledTo.CNICNumber))
END as BilledCNIC
,BilledTo.FatherName as BilledToFatherName
,INV_Item.InventoryItemStatusCode as ItemStatusCode
,INV_Item.CubicCentimeters as CC
,(ISNULL(Sal_InvoiceDetail.taxAmount,0) / 100) *  Sal_InvoiceDetail.GrossAmount as SalesTaxAmount
,INV_StockTransactionHeader.BatteryNo
,INV_StockTransactionHeader.FrontTyreRight
,INV_StockTransactionHeader.FrontTyreleft
,INV_StockTransactionHeader.RearTyreRight
,INV_StockTransactionHeader.RearTyreLeft
,Sal_OrderHeader.ContractNo
  ,convert (varchar(20), Sal_OrderHeader.ContractDate ,106) as ContractDate
From Sal_InvoiceHeader

left JOIN Ar_Set_Customer ON Ar_set_Customer.CustomerID = Sal_InvoiceHeader.CustomerID AND
	Ar_Set_Customer.CompanyID = Sal_InvoiceHeader.CompanyID AND Sal_InvoiceHeader.PeriodID =Ar_set_Customer.PeriodID 

left JOIN Sal_InvoiceDetail ON Sal_InvoiceDetail.InvoiceHeaderID = Sal_InvoiceHeader.InvoiceID AND
	Sal_InvoiceHeader.CompanyID = Sal_InvoiceDetail.CompanyID AND Sal_InvoiceDetail.PeriodID = Sal_InvoiceHeader.PeriodID


left join SAL_DOVehicleInformation
on SAL_DOVehicleInformation.VehicleID = SAL_InvoiceDetail.VehicleID
and SAL_DOVehicleInformation.CompanyID = SAL_InvoiceDetail.CompanyID
and SAL_DOVehicleInformation.PeriodID = SAL_InvoiceDetail.PeriodID
and SAL_DOVehicleInformation.Status =1 
and SAL_InvoiceDetail.Status = 1 

inner join SAL_DispatchVehicleInformation on SAL_DispatchVehicleInformation.VehicleID = SAL_InvoiceDetail.VehicleID
and SAL_DispatchVehicleInformation.CompanyID = SAL_InvoiceDetail.CompanyID
and SAL_DispatchVehicleInformation.PeriodID = SAL_InvoiceDetail.PeriodID

inner join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = SAL_DispatchVehicleInformation.StockHeaderID
and INV_StockTransactionDetail.TransactionDetailID = SAL_DispatchVehicleInformation.StockDetailID
and INV_StockTransactionDetail.CompanyID = SAL_DispatchVehicleInformation.CompanyID
and INV_StockTransactionDetail.PeriodID = SAL_DispatchVehicleInformation.PeriodID
and INV_StockTransactionDetail.Status= 1

inner join INV_StockTransactionHeader on INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID
and INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID
and INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID
and INV_StockTransactionHeader.Status =1 

left JOIN Sal_DeliveryOrderDetail ON Sal_DeliveryOrderDetail.DOHeaderID = INV_StocktransactionDetail.SourceHeaderID AND
	Sal_DeliveryOrderDetail.DODetailID =INV_StockTransactionDetail.sourceLineID 
	AND Sal_DeliveryOrderDetail.CompanyID = INV_StocktransactionDetail.companyID AND Sal_DeliveryOrderDetail.PeriodID = INV_StockTransactionDetail.PeriodID

left JOIN 	Sal_OrderDetail ON Sal_OrderDetail.OrderID = Sal_DeliveryOrderDetail.SalesOrderHeaderID AND 
	Sal_DeliveryOrderDetail.SalesOrderDetailId = Sal_OrderDetail.OrderDetailID
	
	
left JOIN Sal_OrderHeader ON Sal_OrderHeader.OrderID = Sal_OrderDetail.OrderID AND 
	Sal_OrderDetail.CompanyID = Sal_OrderHeader.CompanyID AND Sal_OrderDetail.PeriodID = Sal_OrderHeader.PeriodID

left JOIN Ar_Set_Customer Dealer ON Dealer.CustomerID = Sal_OrderHeader.BrokerID AND
	Dealer.CompanyID = Sal_OrderHeader.CompanyID AND Sal_OrderHeader.PeriodID =Dealer.PeriodID	

left JOIN INV_Item ON INV_Item.ItemID = Sal_InvoiceDetail.ItemId AND
	INV_Item.CompanyID = Sal_InvoiceDetail.CompanyID	
left JOIN Set_Color ON Set_Color.ColorID = Sal_OrderDetail.ColorID
left Join SET_Company ON SET_Company.CompanyID = Sal_InvoiceHeader.CompanyID 
left Join SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitID = SET_Company.OperatingUnitID

LEft JOIN AR_SET_Customer BilledTo ON BilledTo.CustomerID = SAL_InvoiceHeader.billedtoid
AND BilledTo.CompanyID = SAL_InvoiceHeader.CompanyID
AND BilledTo.PeriodID = SAL_InvoiceHeader.PeriodID

LEFT JOIN SET_City BilledtoCity ON BilledtoCity.CityID = Billedto.CityID

left JOIN Set_City City ON City.CityID = Set_OperatingUnit.CityID
	AND City.Status = 1
		
left JOIN Set_Business ON Set_Business.BusinessId = Set_OperatingUnit.BusinessId
	AND Set_Business.Status = 1
	
left JOIN Set_Country Country ON Country.CountryID = City.CountryID
	AND Country.Status = 1 
";*/
        #endregion
        const string Query_SalesInvoice = @"
SELECT 
distinct 
AR_Set_Customer.CustomerName,
ARS.CustomerName As BilledTo,
AR_Set_Customer.PhoneNumber,
Sal_InvoiceHeader.InvoiceDate,
Sal_InvoiceHeader.CustomerID,
Sal_InvoiceHeader.BilledToId

FROM [SAL_DOVehicleInformation]

Inner Join Sal_DeliveryOrderHeader On Sal_DeliveryOrderHeader.DeliveryOrderID = [SAL_DOVehicleInformation].DOHeaderID
	AND Sal_DeliveryOrderHeader.CompanyID = [SAL_DOVehicleInformation].CompanyID
	AND Sal_DeliveryOrderHeader.PeriodID = [SAL_DOVehicleInformation].PeriodID
	AND Sal_DeliveryOrderHeader.Status = 1
Inner Join INV_StockTransactionDetail ON INV_StockTransactionDetail.SourceHeaderID = SAL_DOVehicleInformation.DOHeaderID
	AND INV_StockTransactionDetail.CompanyID = [SAL_DOVehicleInformation].CompanyID
	AND INV_StockTransactionDetail.PeriodID = [SAL_DOVehicleInformation].PeriodID
	AND INV_StockTransactionDetail.Status = 1
Inner Join INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID
	AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID
	AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID
	AND INV_StockTransactionHeader.Status = 1
LEFT Join Sal_InvoiceDetail ON Sal_InvoiceDetail.StockHeaderID = INV_StockTransactionHeader.StockTransactionID
	AND Sal_InvoiceDetail.CompanyID = INV_StockTransactionHeader.CompanyID
	AND Sal_InvoiceDetail.PeriodID = INV_StockTransactionHeader.PeriodID
	AND Sal_InvoiceDetail.Status = 1
LEFT Join Sal_InvoiceHeader ON Sal_InvoiceHeader.InvoiceID = Sal_InvoiceDetail.InvoiceHeaderID
	AND Sal_InvoiceHeader.CompanyID = Sal_InvoiceDetail.CompanyID
	AND Sal_InvoiceHeader.PeriodID = Sal_InvoiceDetail.PeriodID
	AND Sal_InvoiceHeader.Status = 1
Inner Join AR_Set_Customer ON AR_Set_Customer.CustomerID = Sal_InvoiceHeader.CustomerID
	--AND AR_Set_Customer.CompanyID = Sal_InvoiceHeader.CompanyID
	AND AR_Set_Customer.Status = 1
Left Join AR_Set_Customer ARS ON ARS.CustomerID = Sal_InvoiceHeader.Billedtoid
	AND ARS.CompanyID = Sal_InvoiceHeader.CompanyID
	AND ARS.Status = 1	
where 
--[SAL_DOVehicleInformation].[Status] = 1 
--and 
--[SAL_DOVehicleInformation].[CompanyID] = @CompanyID
--AND [SAL_DOVehicleInformation].[PeriodID] = @PeriodID
--AND [SAL_DOVehicleInformation].DOVehicleInformationID = @VehicleInformationID
";

        const string Query_Select_By_DeliveryOrderID_CompanyId_PeriodId = @"
SELECT SAL_DOVehicleInformation.CompanyID, SAL_DOVehicleInformation.PeriodID, SAL_DOVehicleInformation.DOVehicleInformationID, SAL_DOVehicleInformation.VehicleID, SAL_DOVehicleInformation.DOHeaderID, 
    SAL_DOVehicleInformation.DODetailID, SAL_DOVehicleInformation.CategoryID, INV_SET_Category.CategoryName, SAL_DOVehicleInformation.ItemID, INV_Item.Description, 
    SAL_DOVehicleInformation.Engine, SAL_DOVehicleInformation.Chassis, SAL_DOVehicleInformation.SerialNo,SAL_DOVehicleInformation.ColorID,Set_Color.ColorName,
[SAL_OrderDetail].[OrderID] as SalesOrderHeaderID,
    [SAL_OrderDetail].[OrderDetailID] as SalesOrderDetailID,
-1 as 'RowUniqueID', SAL_DOVehicleInformation.DODetailID as DID,
'' as Action 
FROM SAL_DOVehicleInformation 
	INNER JOIN SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.CompanyID = SAL_DOVehicleInformation.CompanyID 
        AND SAL_DeliveryOrderDetail.PeriodID = SAL_DOVehicleInformation.PeriodID AND SAL_DeliveryOrderDetail.DOHeaderID = SAL_DOVehicleInformation.DOHeaderID 
        AND SAL_DeliveryOrderDetail.DODetailID = SAL_DOVehicleInformation.DODetailID 
	INNER JOIN [SAL_OrderDetail] ON [SAL_OrderDetail].[CompanyID] = [SAL_DeliveryOrderDetail].[CompanyID] 
        AND [SAL_OrderDetail].[PeriodID] = [SAL_DeliveryOrderDetail].[PeriodID] AND [SAL_OrderDetail].[OrderID] = [SAL_DeliveryOrderDetail].[SalesOrderHeaderID] 
        AND [SAL_OrderDetail].[OrderDetailID] = [SAL_DeliveryOrderDetail].[SalesOrderDetailID] AND [SAL_OrderDetail].[Status] = 1 
	INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [SAL_OrderDetail].[ItemCategoryID] 
        --AND [INV_SET_Category].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
    AND [INV_SET_Category].[Status] = 1 
	INNER JOIN INV_Item ON INV_Item.ItemID = SAL_DOVehicleInformation.ItemID 
--AND INV_Item.CompanyID = SAL_DOVehicleInformation.CompanyID 
	
	left Join Set_Color ON Set_Color.ColorID = SAL_DOVehicleInformation.ColorID 
        AND Set_Color.Isactive = 1 and Set_Color.Status = 1
WHERE SAL_DOVehicleInformation.[DOHeaderID] = @DeliveryOrderID AND SAL_DOVehicleInformation.[CompanyID] = @CompanyID 
    AND SAL_DOVehicleInformation.[PeriodID] = @PeriodID AND SAL_DOVehicleInformation.[Status] = 1";



        const string Query_GetDeliveryOrderVehicle = @"
Select distinct Sal_DeliveryOrderHeader.DeliveryOrderNo, Sal_OrderHeader.ContractNo as SONum,Sal_DeliveryOrderHeader.DODate,
SAL_DOVehicleInformation.Engine, SAL_DOVehicleInformation.Chassis,SET_Color.ColorName,Sal_OrderDetail.ColorID,
INV_Item.Description as Vehicle, case when SAL_DOVehicleInformation.vehicleID is null then 'Return' else '' end as [Version], Sal_OrderHeader.Referenceno as PSONo
,Broker.CustomerName as BrokerName, Set_City.CityName as BrokerCity
,Sal_OrderHeader.brokerID,Sal_OrderHeader.OrderID
,AR_Set_Customer.CustomerName,Sal_OrderHeader.Remarks
 From Sal_DeliveryOrderHeader 
INNER JOIN Sal_DeliveryOrderDetail ON Sal_DeliveryOrderDetail.DOHeaderID = Sal_DeliveryOrderHeader.DeliveryOrderID
AND Sal_DeliveryOrderDetail.CompanyID = Sal_DeliveryOrderHeader.CompanyID AND Sal_DeliveryOrderDetail.PeriodID = Sal_DeliveryOrderHeader.PeriodID
INNER JOIN SAL_DOVehicleInformation ON SAL_DOVehicleInformation.DOHeaderID = Sal_DeliveryOrderHeader.DeliveryOrderID AND SAL_DOVehicleInformation.DODetailID = Sal_DeliveryOrderDetail.DoDetailID
AND SAL_DOVehicleInformation.CompanyID = Sal_DeliveryOrderHeader.CompanyID AND SAL_DOVehicleInformation.PeriodID = Sal_DeliveryOrderHeader.PeriodID
INNER JOIN Sal_OrderDetail on Sal_OrderDetail.OrderID = Sal_DeliveryOrderDetail.SalesOrderHeaderID AND Sal_DeliveryOrderDetail.SalesOrderDetailID = Sal_OrderDetail.OrderDetailID
AND Sal_OrderDetail.CompanyID = Sal_DeliveryOrderDetail.CompanyID AND Sal_OrderDetail.PeriodID = Sal_DeliveryOrderDetail.PeriodID
INNER JOIN Sal_OrderHeader ON Sal_OrderHeader.OrderID = Sal_OrderDetail.OrderID
AND Sal_OrderHeader.CompanyID = Sal_OrderDetail.CompanyID AND Sal_OrderHeader.PeriodID = Sal_OrderDetail.PeriodID
Left Join  SET_Color ON SET_Color.ColorID = SAL_DOVehicleInformation.ColorID
INNER JOIN INV_Item ON INV_Item.ItemID = SAL_DOVehicleInformation.ItemID AND
--INV_Item.CompanyID = SAL_DOVehicleInformation.CompanyID 
LEft Join AR_Set_Customer as Broker ON Broker.CustomerID = Sal_OrderHeader.BrokerID
				AND Broker.CompanyID = Sal_OrderHeader.CompanyId
				AND Broker.PeriodID = Sal_OrderHeader.PeriodID
				AND Broker.Status = 1
				
			Left Join Set_City ON Set_City.CityID = Broker.CityID
				AND Set_City.Status = 1

INNER JOIN AR_Set_Customer ON Ar_set_Customer.CustomerID = Sal_DeliveryOrderHeader.CustomerID AND
--AR_Set_Customer.CompanyID = Sal_DeliveryOrderHeader.CompanyID AND 
AR_Set_Customer.PeriodID = Sal_DeliveryOrderHeader.PeriodID
{0}
Select Count(Fintab.Vehicle)TotalDeliveries,Fintab.Vehicle as VehicleSum From(
Select Sal_DeliveryOrderHeader.DeliveryOrderNo, Sal_DeliveryOrderHeader.DODate,
SAL_DOVehicleInformation.Engine, SAL_DOVehicleInformation.Chassis,SET_Color.ColorName,Sal_OrderDetail.ColorID,
INV_Item.Description as Vehicle, '??' as Version, '??' as PSONo
,Broker.CustomerName as BrokerName, Set_City.CityName as BrokerCity
,Sal_OrderHeader.brokerID,Sal_OrderHeader.OrderID
,AR_Set_Customer.CustomerName,Sal_OrderHeader.Remarks

 From Sal_DeliveryOrderHeader 
INNER JOIN Sal_DeliveryOrderDetail ON Sal_DeliveryOrderDetail.DOHeaderID = Sal_DeliveryOrderHeader.DeliveryOrderID
AND Sal_DeliveryOrderDetail.CompanyID = Sal_DeliveryOrderHeader.CompanyID AND Sal_DeliveryOrderDetail.PeriodID = Sal_DeliveryOrderHeader.PeriodID
INNER JOIN SAL_DOVehicleInformation ON SAL_DOVehicleInformation.DOHeaderID = Sal_DeliveryOrderHeader.DeliveryOrderID AND SAL_DOVehicleInformation.DODetailID = Sal_DeliveryOrderDetail.DoDetailID
AND SAL_DOVehicleInformation.CompanyID = Sal_DeliveryOrderHeader.CompanyID AND SAL_DOVehicleInformation.PeriodID = Sal_DeliveryOrderHeader.PeriodID
INNER JOIN Sal_OrderDetail on Sal_OrderDetail.OrderID = Sal_DeliveryOrderDetail.SalesOrderHeaderID AND Sal_DeliveryOrderDetail.SalesOrderDetailID = Sal_OrderDetail.OrderDetailID
AND Sal_OrderDetail.CompanyID = Sal_DeliveryOrderDetail.CompanyID AND Sal_OrderDetail.PeriodID = Sal_DeliveryOrderDetail.PeriodID
INNER JOIN Sal_OrderHeader ON Sal_OrderHeader.OrderID = Sal_OrderDetail.OrderID
AND Sal_OrderHeader.CompanyID = Sal_OrderDetail.CompanyID AND Sal_OrderHeader.PeriodID = Sal_OrderDetail.PeriodID
Left Join  SET_Color ON SET_Color.ColorID = Sal_OrderDetail.ColorID
INNER JOIN INV_Item ON INV_Item.ItemID = SAL_DOVehicleInformation.ItemID AND
--INV_Item.CompanyID = SAL_DOVehicleInformation.CompanyID 
LEft Join AR_Set_Customer as Broker ON Broker.CustomerID = Sal_OrderHeader.BrokerID
				AND Broker.CompanyID = Sal_OrderHeader.CompanyId
				AND Broker.PeriodID = Sal_OrderHeader.PeriodID
				AND Broker.Status = 1
				
			Left Join Set_City ON Set_City.CityID = Broker.CityID
				AND Set_City.Status = 1

INNER JOIN AR_Set_Customer ON Ar_set_Customer.CustomerID = Sal_DeliveryOrderHeader.CustomerID AND
--AR_Set_Customer.CompanyID = Sal_DeliveryOrderHeader.CompanyID AND 
AR_Set_Customer.PeriodID = Sal_DeliveryOrderHeader.PeriodID
--WHERE Sal_DeliveryOrderHeader.CompanyID = 1 and Sal_DeliveryOrderHeader.PeriodID = 1 and CAST(CONVERT(VARCHAR(10), Sal_DeliveryOrderHeader.DODate, 110) AS DATETIME) between '02-07-2013' and '03-07-2013' AND Sal_DeliveryOrderHeader.DeliveryOrderNo Between 47 AND 49
WHERE Sal_DeliveryOrderHeader.CompanyID = 1 and Sal_DeliveryOrderHeader.PeriodID = 1 and CAST(CONVERT(VARCHAR(10), Sal_DeliveryOrderHeader.DODate, 110) AS DATETIME) between '06-17-2013' and '06-17-2014' AND Sal_DeliveryOrderHeader.DeliveryOrderNo Between 110 AND 110  AND Sal_DOVehicleInformation.CategoryID = 28 AND Sal_DeliveryOrderHeader.StatusCode = 1
)Fintab
Group By Fintab.Vehicle
Union ALL
Select Count(Fintab2.Vehicle)TotalDeliveries,'Total Number of Deliveries' as VehicleSum From(
Select Sal_DeliveryOrderHeader.DeliveryOrderNo, Sal_DeliveryOrderHeader.DODate,
SAL_DOVehicleInformation.Engine, SAL_DOVehicleInformation.Chassis,SET_Color.ColorName,Sal_OrderDetail.ColorID,
INV_Item.Description as Vehicle, '??' as Version, '??' as PSONo
,Broker.CustomerName as BrokerName, Set_City.CityName as BrokerCity
,Sal_OrderHeader.brokerID,Sal_OrderHeader.OrderID
,AR_Set_Customer.CustomerName,Sal_OrderHeader.Remarks
,SAL_DOVehicleInformation.vehicleID
, case when SAL_DOVehicleInformation.vehicleID is null then 'Return' else '' end as ReturnStatus

 From Sal_DeliveryOrderHeader 

INNER JOIN Sal_DeliveryOrderDetail ON Sal_DeliveryOrderDetail.DOHeaderID = Sal_DeliveryOrderHeader.DeliveryOrderID
AND Sal_DeliveryOrderDetail.CompanyID = Sal_DeliveryOrderHeader.CompanyID AND Sal_DeliveryOrderDetail.PeriodID = Sal_DeliveryOrderHeader.PeriodID

INNER JOIN SAL_DOVehicleInformation ON SAL_DOVehicleInformation.DOHeaderID = Sal_DeliveryOrderHeader.DeliveryOrderID AND SAL_DOVehicleInformation.DODetailID = Sal_DeliveryOrderDetail.DoDetailID
AND SAL_DOVehicleInformation.CompanyID = Sal_DeliveryOrderHeader.CompanyID AND SAL_DOVehicleInformation.PeriodID = Sal_DeliveryOrderHeader.PeriodID


INNER JOIN Sal_OrderDetail on Sal_OrderDetail.OrderID = Sal_DeliveryOrderDetail.SalesOrderHeaderID AND Sal_DeliveryOrderDetail.SalesOrderDetailID = Sal_OrderDetail.OrderDetailID
AND Sal_OrderDetail.CompanyID = Sal_DeliveryOrderDetail.CompanyID AND Sal_OrderDetail.PeriodID = Sal_DeliveryOrderDetail.PeriodID

INNER JOIN Sal_OrderHeader ON Sal_OrderHeader.OrderID = Sal_OrderDetail.OrderID
AND Sal_OrderHeader.CompanyID = Sal_OrderDetail.CompanyID AND Sal_OrderHeader.PeriodID = Sal_OrderDetail.PeriodID

Left Join  SET_Color ON SET_Color.ColorID = SAL_DOVehicleInformation.ColorID

INNER JOIN INV_Item ON INV_Item.ItemID = SAL_DOVehicleInformation.ItemID AND
--INV_Item.CompanyID = SAL_DOVehicleInformation.CompanyID 

LEft Join AR_Set_Customer as Broker ON Broker.CustomerID = Sal_OrderHeader.BrokerID
				AND Broker.CompanyID = Sal_OrderHeader.CompanyId
				AND Broker.PeriodID = Sal_OrderHeader.PeriodID
				AND Broker.Status = 1
				
			Left Join Set_City ON Set_City.CityID = Broker.CityID
				AND Set_City.Status = 1

INNER JOIN AR_Set_Customer ON Ar_set_Customer.CustomerID = Sal_DeliveryOrderHeader.CustomerID AND
--AR_Set_Customer.CompanyID = Sal_DeliveryOrderHeader.CompanyID AND 
AR_Set_Customer.PeriodID = Sal_DeliveryOrderHeader.PeriodID
--WHERE Sal_DeliveryOrderHeader.CompanyID = 1 and Sal_DeliveryOrderHeader.PeriodID = 1 and CAST(CONVERT(VARCHAR(10), Sal_DeliveryOrderHeader.DODate, 110) AS DATETIME) between '02-07-2013' and '03-07-2013' AND Sal_DeliveryOrderHeader.DeliveryOrderNo Between 47 AND 49
{0}
)Fintab2
";
        const string Query_GetTotalvehicleSumForDOvehicle = @"";
        const string Query_AllActiveDataWithEngine = @"
        SELECT * FROM [SAL_DOVehicleInformation] WHERE [Status] = 1 AND ([CompanyID] = @CompanyID OR @CompanyID is null) AND ([PeriodID] = @PeriodID OR @PeriodID is null) And Engine Is NOt NULL";

        const string Query_AllActiveDataWithColor = @"SELECT [SAL_DOVehicleInformation].[CompanyID]
      ,[SAL_DOVehicleInformation].[PeriodID]
      ,[SAL_DOVehicleInformation].[DOVehicleInformationID]
      ,[SAL_DOVehicleInformation].[DOHeaderID]
      ,[SAL_DOVehicleInformation].[DODetailID]
      ,[SAL_DOVehicleInformation].[CategoryID]
      ,[SAL_DOVehicleInformation].[ItemID]
      ,[SAL_DOVehicleInformation].[SerialNo]
      ,[SAL_DOVehicleInformation].[Engine]
      ,[SAL_DOVehicleInformation].[Chassis]
      ,[SAL_DOVehicleInformation].[Status]
      ,[SAL_DOVehicleInformation].[CreatedBy]
      ,[SAL_DOVehicleInformation].[CreateDate]
      ,[SAL_DOVehicleInformation].[CreatedByValue]
      ,[SAL_DOVehicleInformation].[UpdatedBy]
      ,[SAL_DOVehicleInformation].[UpdateDate]
      ,[SAL_DOVehicleInformation].[UpdatedByValue]
      ,[SAL_DOVehicleInformation].[ColorID]
      ,[SET_Color].[ColorName]
       FROM [SAL_DOVehicleInformation]

left join [SET_Color] 
on [SAL_DOVehicleInformation].[ColorID]=[SET_Color].[ColorID]
where [SAL_DOVehicleInformation].[Status]=1 and [SET_Color].[Status]=1 and 
[SAL_DOVehicleInformation].[Engine] is not null and [SAL_DOVehicleInformation].[ColorID] is not null and
([SAL_DOVehicleInformation].[CompanyID] = @CompanyID OR @CompanyID is null) AND ([SAL_DOVehicleInformation].[PeriodID] = @PeriodID OR @PeriodID is null)
";




        const string Query_GetDataForDTR = @"
        Select SAL_DOVehicleInformation.*,INV_Item.Description ItemName,SET_Color.ColorName,
		        SAL_DeliveryOrderHeader.DoDate
		        from SAL_DOVehicleInformation
        JOIN INV_Item INV_Item on INV_Item.ItemId = SAL_DOVehicleInformation.ItemID
        JOIN SET_Color SET_Color on SET_Color.ColorID = SAL_DOVehicleInformation.ColorID
        JOIN SAL_DeliveryOrderHeader SAL_DeliveryOrderHeader on SAL_DeliveryOrderHeader.DeliveryOrderID = SAL_DOVehicleInformation.DOHeaderID
        Where	SAL_DOVehicleInformation.CompanyID = @CompanyID
		        AND SAL_DOVehicleInformation.PeriodID = @PeriodID
		        AND SAL_DOVehicleInformation.Engine Like '' + @Engine
";
        const string Query_GetItemName = @"
Select Distinct INV_Item.Description as ItemName ,SAL_DOVehicleInformation.Engine,SAL_DOVehicleInformation.Chassis,
SET_Color.ColorName,Sal_InvoiceHeader.InvoiceDate as CreateDate,BilledTo.CustomerID as BilledToID
,BilledTo.CustomerName as BilledToName, SAL_DOVehicleInformation.DOVehicleInformationID
,(select top 1  AS_ServiceRecipe.KM from 
AS_ServiceRecipe 
where AS_ServiceRecipe.VehicleItemID = SAL_DOVehicleInformation.ItemID
and AS_ServiceRecipe.companyID = SAL_DOVehicleInformation.companyID
and AS_ServiceRecipe.ServiceName like 'Warrant%') as KM
,INV_StockTransactionHeader.DocumentDate
--,AS_Offer.WarrantyBookNo
from SAL_DOVehicleInformation 
Inner Join INV_Item On 
--INV_Item.CompanyID = SAL_DOVehicleInformation.CompanyID And 
INV_Item.ItemID = SAL_DOVehicleInformation.ItemID 
Left join SET_Color ON SET_Color.ColorID = SAL_DOVehicleInformation.ColorID

Inner Join Sal_DeliveryOrderHeader On Sal_DeliveryOrderHeader.DeliveryOrderID = [SAL_DOVehicleInformation].DOHeaderID
	AND Sal_DeliveryOrderHeader.CompanyID = [SAL_DOVehicleInformation].CompanyID
	AND Sal_DeliveryOrderHeader.PeriodID = [SAL_DOVehicleInformation].PeriodID
	AND Sal_DeliveryOrderHeader.Status = 1
Inner Join INV_StockTransactionDetail ON INV_StockTransactionDetail.SourceHeaderID = SAL_DOVehicleInformation.DOHeaderID
	AND INV_StockTransactionDetail.CompanyID = [SAL_DOVehicleInformation].CompanyID
	AND INV_StockTransactionDetail.PeriodID = [SAL_DOVehicleInformation].PeriodID
	AND INV_StockTransactionDetail.Status = 1
Inner Join INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID
	AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID
	AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID
	AND INV_StockTransactionHeader.Status = 1
LEFT Join Sal_InvoiceDetail ON Sal_InvoiceDetail.StockHeaderID = INV_StockTransactionHeader.StockTransactionID
	AND Sal_InvoiceDetail.CompanyID = INV_StockTransactionHeader.CompanyID
	AND Sal_InvoiceDetail.PeriodID = INV_StockTransactionHeader.PeriodID
	AND Sal_InvoiceDetail.Status = 1
LEFT Join Sal_InvoiceHeader ON Sal_InvoiceHeader.InvoiceID = Sal_InvoiceDetail.InvoiceHeaderID
	AND Sal_InvoiceHeader.CompanyID = Sal_InvoiceDetail.CompanyID
	AND Sal_InvoiceHeader.PeriodID = Sal_InvoiceDetail.PeriodID
	AND Sal_InvoiceHeader.Status = 1



Left Join Ar_Set_Customer BilledTo on BilledTo.CustomerID = Sal_InvoiceHeader.billedtoid
 AND BilledTo.CompanyID = Sal_InvoiceHeader.CompanyID AND BilledTo.PeriodID = Sal_InvoiceHeader.PeriodID 

Where SAL_DOVehicleInformation.CompanyID = @CompanyID And SAL_DOVehicleInformation.PeriodID = @PeriodID 
    And SAL_DOVehicleInformation.DOVehicleInformationID = @VehicleInformationID";


        const string Query_vehicleSalesInvoices = @"
Select Distinct Sal_InvoiceHeader.InvoiceNumber,Sal_InvoiceHeader.CustomerID, Sal_InvoiceHeader.InvoiceDate,
Ar_set_Customer.CustomerName,Ar_set_Customer.FatherName, Ar_set_Customer.Addressline1,
SET_OperatingUnit.AddressLine2+', '+SET_OperatingUnit.AddressLine3+', '+City.CityName+'.' AS SaleOfficeAddress,
SET_Business.AddressLine2+', '+City.CityName+', '+Country.CountryName+', Tel:'+SET_Business.PhoneNumber+', '+SET_Business.AddressLine3 AS HeadOfficeAddress,
Ar_set_Customer.CNICNumber,Dealer.CustomerName as Dealer,   
    --AR_Set_Customer.NTNNumber AS NTN,
	--AR_Set_Customer.STRNNumber AS STRN,
SET_OperatingUnit.NTNNumber AS NTN,
SET_OperatingUnit.SalesTaxNumber as STRN,

Sal_InvoiceDetail.ItemId as INvoiceItem,INV_StocktransactionDetail.ItemID as STItem,
INV_Item.Description, INV_Item.Description as Model,
SAL_DOVehicleInformation.Engine, SAL_DOVehicleInformation.Chassis, Set_Color.ColorName,
 ISNULL(Sal_InvoiceDetail.taxAmount,0)taxAmount, Sal_InvoiceDetail.GrossAmount, Sal_InvoiceDetail.NetAmount,SET_OperatingUnit.LogoImage
,BilledTo.CustomerName as BilledToName,
BilledTo.AddressLine1 as BilledToAddress,
BilledtoCity.CityName as BilledToCity
--,Case BilledTo.CNICNumber When '' then BilledTo.NTNNumber Else BilledTo.CNICNumber END as BilledCNIC
,Case BilledTo.CNICNumber When '' then BilledTo.NTNNumber Else 
substring(BilledTo.CNICNumber,0,5)+'-'+substring(BilledTo.CNICNumber,5,len(BilledTo.CNICNumber)-1) +'-'+substring(BilledTo.CNICNumber,len(BilledTo.CNICNumber),len(BilledTo.CNICNumber))
END as BilledCNIC
,BilledTo.FatherName as BilledToFatherName
,INV_Item.InventoryItemStatusCode as ItemStatusCode
,INV_Item.CubicCentimeters as CC
From Sal_InvoiceHeader

INNER JOIN Ar_Set_Customer ON Ar_set_Customer.CustomerID = Sal_InvoiceHeader.CustomerID AND
	--Ar_Set_Customer.CompanyID = Sal_InvoiceHeader.CompanyID AND 
Sal_InvoiceHeader.PeriodID =Ar_set_Customer.PeriodID 

INNER JOIN Sal_InvoiceDetail ON Sal_InvoiceDetail.InvoiceHeaderID = Sal_InvoiceHeader.InvoiceID AND
	Sal_InvoiceHeader.CompanyID = Sal_InvoiceDetail.CompanyID AND Sal_InvoiceDetail.PeriodID = Sal_InvoiceHeader.PeriodID

INNER JOIN INV_StocktransactionDetail ON INV_StocktransactionDetail.CompanyID = Sal_InvoiceDetail.CompanyID AND
	Sal_InvoiceDetail.PeriodID = INV_StocktransactionDetail.PeriodID AND INV_StocktransactionDetail.StockTransactionID = Sal_InvoiceDetail.StockheaderID
	AND Sal_InvoiceDetail.stockDetailID = INV_StockTransactionDetail.TransactionDetailID

INNER JOIN Sal_DeliveryOrderDetail ON Sal_DeliveryOrderDetail.DOHeaderID = INV_StocktransactionDetail.SourceHeaderID AND
	Sal_DeliveryOrderDetail.DODetailID =INV_StockTransactionDetail.sourceLineID 
	AND Sal_DeliveryOrderDetail.CompanyID = INV_StocktransactionDetail.companyID AND Sal_DeliveryOrderDetail.PeriodID = INV_StockTransactionDetail.PeriodID
INNER JOIN 	Sal_OrderDetail ON Sal_OrderDetail.OrderID = Sal_DeliveryOrderDetail.SalesOrderHeaderID AND 
	Sal_DeliveryOrderDetail.SalesOrderDetailId = Sal_OrderDetail.OrderDetailID
	
INNER JOIN Sal_OrderHeader ON Sal_OrderHeader.OrderID = Sal_OrderDetail.OrderID AND 
	Sal_OrderDetail.CompanyID = Sal_OrderHeader.CompanyID AND Sal_OrderDetail.PeriodID = Sal_OrderHeader.PeriodID

INNER JOIN Ar_Set_Customer Dealer ON Dealer.CustomerID = Sal_OrderHeader.BrokerID AND
	Dealer.CompanyID = Sal_OrderHeader.CompanyID AND Sal_OrderHeader.PeriodID =Dealer.PeriodID	

INNER JOIN 	SAL_DOVehicleInformation ON SAL_DOVehicleInformation.DOHeaderID = Sal_DeliveryOrderDetail.DOHeaderID AND
	SAL_DOVehicleInformation.DODetailID = Sal_DeliveryOrderDetail.DODetailID AND 
	SAL_DOVehicleInformation.CompanyID = Sal_DeliveryOrderDetail.CompanyID AND
	SAL_DOVehicleInformation.PeriodID = Sal_DeliveryOrderDetail.PeriodID

INNER JOIN INV_Item ON INV_Item.ItemID = Sal_InvoiceDetail.ItemId --AND
	--INV_Item.CompanyID = Sal_InvoiceDetail.CompanyID	
INNER JOIN Set_Color ON Set_Color.ColorID = Sal_OrderDetail.ColorID
Inner Join SET_Company ON SET_Company.CompanyID = Sal_InvoiceHeader.CompanyID 
Inner Join SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitID = SET_Company.OperatingUnitID

LEft JOIN AR_SET_Customer BilledTo ON BilledTo.CustomerID = SAL_InvoiceHeader.billedtoid
AND BilledTo.CompanyID = SAL_InvoiceHeader.CompanyID
AND BilledTo.PeriodID = SAL_InvoiceHeader.PeriodID

LEFT JOIN SET_City BilledtoCity ON BilledtoCity.CityID = Billedto.CityID

INNER JOIN Set_City City ON City.CityID = Set_OperatingUnit.CityID
	AND City.Status = 1
		
INNER JOIN Set_Business ON Set_Business.BusinessId = Set_OperatingUnit.BusinessId
	AND Set_Business.Status = 1
	
INNER JOIN Set_Country Country ON Country.CountryID = City.CountryID
	AND Country.Status = 1

";

        const string Query_AllActiveDataWithEngineByStockTransactionID = @"
SELECT
INV_StockTransactionHeader.StockTransactionID,
SAL_DispatchHeader.DispatchNo,
SAL_DOVehicleInformation.DOHeaderID,
SAL_DOVehicleInformation.DODetailID,
SAL_DOVehicleInformation.DOVehicleInformationID,
SAL_DOVehicleInformation.Engine,
SAL_DOVehicleInformation.Chassis,
SAL_DOVehicleInformation.SerialNo
FROM SAL_DOVehicleInformation
INNER JOIN SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.DOHeaderID = SAL_DOVehicleInformation.DOHeaderID
AND SAL_DeliveryOrderDetail.DODetailID = SAL_DOVehicleInformation.DODetailID
AND SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID
AND SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID

INNER JOIN INV_StockTransactionDetail ON INV_StockTransactionDetail.SourceHeaderID = Sal_DeliveryOrderDetail.DOHeaderID
AND INV_StockTransactionDetail.SourceLineID = Sal_DeliveryOrderDetail.DODetailID
AND INV_StockTransactionDetail.CompanyID = Sal_DeliveryOrderDetail.CompanyID
AND INV_StockTransactionDetail.PeriodID = Sal_DeliveryOrderDetail.PeriodID

INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID
AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID
AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID

INNER JOIN SAL_DispatchHeader ON SAL_DispatchHeader.DispatchNo = INV_StockTransactionHeader.DocumentNo
AND SAL_DispatchHeader.CompanyID = INV_StockTransactionHeader.CompanyID
AND SAL_DispatchHeader.PeriodID = INV_StockTransactionHeader.PeriodID
WHERE INV_StockTransactionHeader.StockTransactionID =  @StockTransactionID
AND SAL_DOVehicleInformation.CompanyID = @CompanyID
AND SAL_DOVehicleInformation.PeriodID = @PeriodID
AND Engine IS NOT NULL
AND Chassis IS NOT NULL";


        const string Query_AllActiveDataWithColorByStockTransactionID = @"
SELECT
INV_StockTransactionHeader.StockTransactionID,
SAL_DispatchHeader.DispatchNo,
SAL_DOVehicleInformation.DOHeaderID,
SAL_DOVehicleInformation.DODetailID,
SAL_DOVehicleInformation.DOVehicleInformationID,
SAL_DOVehicleInformation.Engine,
SAL_DOVehicleInformation.Chassis,
SAL_DOVehicleInformation.SerialNo,
SET_Color.ColorName

FROM SAL_DOVehicleInformation

LEFT JOIN SET_Color 
on [SAL_DOVehicleInformation].[ColorID]= [SET_Color].[ColorID]

INNER JOIN SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.DOHeaderID = SAL_DOVehicleInformation.DOHeaderID
AND SAL_DeliveryOrderDetail.DODetailID = SAL_DOVehicleInformation.DODetailID
AND SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID
AND SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID

INNER JOIN INV_StockTransactionDetail ON INV_StockTransactionDetail.SourceHeaderID = Sal_DeliveryOrderDetail.DOHeaderID
AND INV_StockTransactionDetail.SourceLineID = Sal_DeliveryOrderDetail.DODetailID
AND INV_StockTransactionDetail.CompanyID = Sal_DeliveryOrderDetail.CompanyID
AND INV_StockTransactionDetail.PeriodID = Sal_DeliveryOrderDetail.PeriodID

INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID
AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID
AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID

INNER JOIN SAL_DispatchHeader ON SAL_DispatchHeader.DispatchNo = INV_StockTransactionHeader.DocumentNo
AND SAL_DispatchHeader.CompanyID = INV_StockTransactionHeader.CompanyID
AND SAL_DispatchHeader.PeriodID = INV_StockTransactionHeader.PeriodID
WHERE INV_StockTransactionHeader.StockTransactionID =  @StockTransactionID
AND SAL_DOVehicleInformation.CompanyID = @CompanyID
AND SAL_DOVehicleInformation.PeriodID = @PeriodID
AND SAL_DOVehicleInformation.Engine IS NOT NULL
AND SAL_DOVehicleInformation.Chassis IS NOT NULL
AND SAL_DOVehicleInformation.ColorID IS NOT NULL 
";

        const string Query_ReportVehicleEnquiryProgram = @"
        SELECT 
         SAL_DOVehicleInformation.Chassis,SAL_DOVehicleInformation.Engine,INV_Item.Description Vehicle,SAL_OrderDetail.ItemCategoryID,INV_SET_Category.CategoryName, SET_Color.ColorName, '' ReportingDate,
		 SAL_DeliveryOrderHeader.StatusCode AS DeliveryStatus
		,AR_SET_Bank2.BankName as NameOfBank, SAL_SOPaymentTerms.InstrumentNo InstrumentNumber, SAL_SOPaymentTerms.PaymentDate InstrumentDate,  SAL_SOPaymentTerms.Amount InustrumentAmount, SAL_SOPaymentTerms.ReceivingDate InstrumentRecDate
		
		,DODate,DeliveryOrderNo,SAL_DispatchHeader.DispatchNo DispatchNumber,SAL_OrderHeader.DispatchDate DispatchDate,Transporter.CustomerName Transporter,SAL_DispatchHeader.BiltyNo BiltyNumber,Set_City.CityName DispatchCity,'' Remarks
		
		,AR_SET_Customer.CustomerName, Dealer.CustomerName DealerName, SAL_OrderHeader.ContractNo BookingNumber, SAL_OrderHeader.referenceno PsoSsoNumber, 0 PickingSlipNumber, SAL_OrderHeader.ContractDate BookingDate
		,InvoiceNumber,InvoiceDate,SAL_InvoiceDetail.NetAmount InvoicingAmount,Year(SET_Batch.FromDate) ModelYear,Year(SET_Batch.FromDate)  ManufacturingYear
		,0 VerificationNumber, GetDate() VerificationDate, 0 ETORefNumber, GetDate() ETODate
		,AR_ReceiptTransactionHeader.ReceiptTransactionID AS ReceiptID
        ,SAL_InvoiceHeader.CustomerID AS InvoiceCustomer, SAL_OrderHeader.CustomerID AS OrderCustomer,forSaleOrderTbl.CustomerName as SaleCustomerName,SAL_OrderHeader.BrokerID,SaleOrderTblBrokrID.CustomerName as SaleBrokerName

        FROM SAL_DOVehicleInformation
 --			Select * From SAL_SOPaymentTerms
        Left JOIN INV_Item ON SAL_DOVehicleInformation.ItemID = INV_Item.ItemID	

        Left JOIN SAL_DeliveryOrderDetail ON SAL_DOVehicleInformation.DOHeaderID = SAL_DeliveryOrderDetail.DOHeaderID
		        AND SAL_DOVehicleInformation.DODetailID = SAL_DeliveryOrderDetail.DODetailID
		        AND SAL_DOVehicleInformation.CompanyID = SAL_DeliveryOrderDetail.CompanyID
		        AND SAL_DOVehicleInformation.PeriodID = SAL_DeliveryOrderDetail.PeriodID
		
        Left JOIN SAL_DeliveryOrderHeader ON SAL_DOVehicleInformation.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID
		        AND SAL_DeliveryOrderHeader.CompanyID = SAL_DOVehicleInformation.CompanyID
		        AND SAL_DeliveryOrderHeader.PeriodID = SAL_DOVehicleInformation.PeriodID
		
        Left JOIN Inv_StockTransactionDetail ON Inv_StockTransactionDetail.SourceLineID = SAL_DeliveryOrderDetail.DODetailID
		        AND Inv_StockTransactionDetail.SourceHeaderID = SAL_DeliveryOrderDetail.DOHeaderID
		        AND Inv_StockTransactionDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID
		        AND Inv_StockTransactionDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID		
		         
		Left JOIN Inv_StockTransactionHeader ON Inv_StockTransactionDetail.StockTransactionID = Inv_StockTransactionHeader.StockTransactionID
		        AND Inv_StockTransactionHeader.CompanyID = SAL_DeliveryOrderDetail.CompanyID
		        AND Inv_StockTransactionHeader.PeriodID = SAL_DeliveryOrderDetail.PeriodID		 
		        
		INNER JOIN SAL_DispatchHeader ON SAL_DispatchHeader.DocumentTypeID = Inv_StockTransactionHeader.DocumentTypeID
				AND SAL_DispatchHeader.DispatchNo = Inv_StockTransactionHeader.DocumentNo
				AND SAL_DispatchHeader.CompanyID = Inv_StockTransactionHeader.CompanyID
				AND SAL_DispatchHeader.PeriodID = Inv_StockTransactionHeader.PeriodID
				       
		
        Left JOIN SAL_InvoiceDetail ON Inv_StockTransactionDetail.TransactionDetailID = SAL_InvoiceDetail.StockDetailID
		        AND Inv_StockTransactionDetail.StockTransactionID = SAL_InvoiceDetail.StockHeaderID
		        
        Left JOIN SAL_InvoiceHeader ON SAL_InvoiceDetail.InvoiceHeaderID = SAL_InvoiceHeader.InvoiceID
		        AND SAL_InvoiceHeader.CompanyID = SAL_InvoiceDetail.CompanyID
		        AND SAL_InvoiceHeader.PeriodID = SAL_InvoiceDetail.PeriodID
		        
        Left JOIN AR_ReceiptTransactionDetail ON SAL_InvoiceDetail.InvoiceDetailID = AR_ReceiptTransactionDetail.InvoiceDetailID
		        AND SAL_InvoiceDetail.InvoiceHeaderID = AR_ReceiptTransactionDetail.InvoiceHeaderID		
		        AND AR_ReceiptTransactionDetail.CompanyID = SAL_InvoiceDetail.CompanyID
		        AND AR_ReceiptTransactionDetail.PeriodID = SAL_InvoiceDetail.PeriodID	

        Left JOIN AR_ReceiptTransactionHeader ON AR_ReceiptTransactionHeader.ReceiptTransactionID = AR_ReceiptTransactionDetail.ReceiptHeaderID		
		        AND AR_ReceiptTransactionDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID
		        AND AR_ReceiptTransactionDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID	

        Left JOIN SAL_OrderDetail ON SAL_DeliveryOrderDetail.SalesOrderHeaderID = SAL_OrderDetail.OrderID
		        AND SAL_DeliveryOrderDetail.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID		
		        AND SAL_OrderDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID
		        AND SAL_OrderDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID
		
        Left JOIN SAL_OrderHeader ON SAL_OrderDetail.OrderID = SAL_OrderHeader.OrderID
		        AND SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID
		        AND SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID

        LEFT JOIN AR_SET_Customer Dealer ON SAL_InvoiceHeader.CustomerID = Dealer.CustomerID
		        AND Dealer.CompanyID = SAL_DeliveryOrderHeader.CompanyID
		        AND Dealer.PeriodID = SAL_DeliveryOrderHeader.PeriodID

        LEFT JOIN AR_SET_Customer ON SAL_InvoiceHeader.billedtoid = AR_SET_Customer.CustomerID
		        --AND AR_SET_Customer.CompanyID = SAL_OrderHeader.CompanyID
		        AND AR_SET_Customer.PeriodID = SAL_OrderHeader.PeriodID
		        
        LEFT JOIN AR_SET_Customer Transporter ON SAL_DispatchHeader.TransporterID = Transporter.CustomerID
		        AND Transporter.CompanyID = SAL_DispatchHeader.CompanyID
		        AND Transporter.PeriodID = SAL_DispatchHeader.PeriodID

        LEFT JOIN AR_SET_Customer forSaleOrderTbl on SAL_OrderHeader.CustomerID=forSaleOrderTbl.CustomerID
				AND forSaleOrderTbl.CompanyID = SAL_OrderHeader.CompanyID
		        AND forSaleOrderTbl.PeriodID = SAL_OrderHeader.PeriodID
		        
        LEFT JOIN AR_SET_Customer SaleOrderTblBrokrID on SAL_OrderHeader.BrokerID=SaleOrderTblBrokrID.CustomerID
				AND SaleOrderTblBrokrID.CompanyID = SAL_OrderHeader.CompanyID
		        AND SaleOrderTblBrokrID.PeriodID = SAL_OrderHeader.PeriodID

        LEFT JOIN Set_City ON Set_City.CityID = SAL_DispatchHeader.CityID
		        
        LEFT JOIN SET_Location Country ON Country.LocationID = SAL_DispatchHeader.LocationID
        
        LEFT JOIN SET_Batch ON SAL_InvoiceDetail.BatchID = SET_Batch.BatchID
		        --AND SET_Batch.CompanyID = SAL_InvoiceDetail.CompanyID
		        AND SET_Batch.PeriodID = SAL_InvoiceDetail.PeriodID
		        
        LEFT JOIN SET_Color ON SAL_DOVehicleInformation.ColorID = SET_Color.ColorID
        
        Left JOIN AR_SET_BankBranch ON AR_SET_BankBranch.BankBranchID = AR_ReceiptTransactionHeader.CustomerBankBranchID
        Left JOIN AR_SET_Bank ON AR_SET_Bank.BankID = AR_SET_BankBranch.BankID
         inner join INV_SET_Category on INV_SET_Category.CategoryID=SAL_OrderDetail.ItemCategoryID
        JOIN SAL_SOPaymentTerms ON SAL_SOPaymentTerms.SOHeaderID = SAL_OrderHeader.OrderID
				AND SAL_SOPaymentTerms.CompanyID = SAL_OrderHeader.CompanyID
		        AND SAL_SOPaymentTerms.PeriodID = SAL_OrderHeader.PeriodID
 Left JOIN AR_SET_Bank as AR_SET_Bank2 ON AR_SET_Bank2.BankID = SAL_SOPaymentTerms.BankID
        Where SAL_DOVehicleInformation.CompanyID = @CompanyID
        AND SAL_DOVehicleInformation.PeriodID = @PeriodID

        ";

        const string Query_DeleteDataByDoHeaderAndDetail = @" 
Update SAL_DOVehicleInformation
Set Status = 0, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue
Where DOHeaderID = @doHeaderID AND DODetailID = @doDetailID
AND CompanyID = @CompanyID AND PeriodID =@PeriodID ";

        const string Query_GetDataForAfterSales = @"
Select Distinct * From (
SELECT 
distinct
SAL_DOVehicleInformation.DOHeaderID,
SAL_DOVehicleInformation.DODetailID,
SAL_DOVehicleInformation.DOVehicleInformationID,
SAL_DOVehicleInformation.Engine,
SAL_DOVehicleInformation.Chassis,
SAL_DOVehicleInformation.SerialNo,
SET_Color.ColorName,
As_Offer.VehicleID,

Case When As_Offer.VehicleID = SAL_DOVehicleInformation.DOVehicleInformationID Then 1
 else 0 End as Flag
 ,INV_SET_Category.CategoryName

FROM SAL_DOVehicleInformation

LEFT JOIN SET_Color 
on [SAL_DOVehicleInformation].[ColorID]= [SET_Color].[ColorID]

INNER JOIN SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.DOHeaderID = SAL_DOVehicleInformation.DOHeaderID
AND SAL_DeliveryOrderDetail.DODetailID = SAL_DOVehicleInformation.DODetailID
AND SAL_DeliveryOrderDetail.CompanyID = SAL_DOVehicleInformation.CompanyID
AND SAL_DeliveryOrderDetail.PeriodID = SAL_DOVehicleInformation.PeriodID
		
LEFT JOIN As_Offer ON As_Offer.VehicleID = SAL_DOVehicleInformation.DOVehicleInformationID AND
		As_Offer.CompanyID = SAL_DOVehicleInformation.CompanyID AND
		As_Offer.PeriodID = SAL_DOVehicleInformation.PeriodID
		
INNER jOIN SAL_DispatchVehicleInformation ON SAL_DispatchVehicleInformation.VehicleInformationID = SAL_DOVehicleInformation.DOVehicleInformationID
			AND SAL_DispatchVehicleInformation.CompanyID = SAL_DOVehicleInformation.CompanyID  
			AND SAL_DispatchVehicleInformation.PeriodID = SAL_DOVehicleInformation.PeriodID

INNER JOIN SAL_DispatchHeader ON SAL_DispatchHeader.DispatchID = SAL_DispatchVehicleInformation.DispatchID
		   AND SAL_DispatchHeader.CompanyID = SAL_DispatchVehicleInformation.CompanyID
		   AND SAL_DispatchHeader.PeriodID = SAL_DispatchVehicleInformation.PeriodID
INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = Sal_DispatchHeader.DocumentTypeID
			AND Set_DocumentType.CompanyID = Sal_DispatchHeader.CompanyID
			AND Set_DocumentType.PeriodID = Sal_DispatchHeader.PeriodID
			
		
		inner join INV_SET_Category on INV_SET_Category.CategoryID = SAL_DOVehicleInformation.CategoryID
		--and INV_SET_Category.companyID = SAL_DOVehicleInformation.companyID
		
			
		--select * from SAL_invoiceHeader order by createdate desc
		-- select * from SAL_InvoiceDetail order by createdate desc
		


WHERE 
--INV_StockTransactionHeader.StockTransactionID =  @StockTransactionID AND 
SAL_DOVehicleInformation.CompanyID = 1
AND SAL_DOVehicleInformation.PeriodID = 1
AND SAL_DOVehicleInformation.Engine IS NOT NULL
AND SAL_DOVehicleInformation.Chassis IS NOT NULL
AND SAL_DOVehicleInformation.ColorID IS NOT NULL 



and exists 
			(select * 
			from  SAL_InvoiceDetail
			where SAL_InvoiceDetail.VehicleID = SAL_DOVehicleInformation.DOHeaderID
			and SAL_InvoiceDetail.companyID = SAL_DOVehicleInformation.companyID
			)
{0}

)Tab 
Where Tab.Flag <> 1
and CategoryName like '%LCV%'
--and tab.chassis = 'AHFMVA113PD000045'


union all


-- hcv vehicle showing on dispatch Time
Select Distinct * From (
SELECT 
distinct
SAL_DOVehicleInformation.DOHeaderID,
SAL_DOVehicleInformation.DODetailID,
SAL_DOVehicleInformation.DOVehicleInformationID,
SAL_DOVehicleInformation.Engine,
SAL_DOVehicleInformation.Chassis,
SAL_DOVehicleInformation.SerialNo,
SET_Color.ColorName,
As_Offer.VehicleID,

Case When As_Offer.VehicleID = SAL_DOVehicleInformation.DOVehicleInformationID Then 1
 else 0 End as Flag
 ,INV_SET_Category.CategoryName

FROM SAL_DOVehicleInformation

LEFT JOIN SET_Color 
on [SAL_DOVehicleInformation].[ColorID]= [SET_Color].[ColorID]

INNER JOIN SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.DOHeaderID = SAL_DOVehicleInformation.DOHeaderID
AND SAL_DeliveryOrderDetail.DODetailID = SAL_DOVehicleInformation.DODetailID
AND SAL_DeliveryOrderDetail.CompanyID = SAL_DOVehicleInformation.CompanyID
AND SAL_DeliveryOrderDetail.PeriodID = SAL_DOVehicleInformation.PeriodID
		
LEFT JOIN As_Offer ON As_Offer.VehicleID = SAL_DOVehicleInformation.DOVehicleInformationID AND
		As_Offer.CompanyID = SAL_DOVehicleInformation.CompanyID AND
		As_Offer.PeriodID = SAL_DOVehicleInformation.PeriodID
		
INNER jOIN SAL_DispatchVehicleInformation ON SAL_DispatchVehicleInformation.VehicleInformationID = SAL_DOVehicleInformation.DOVehicleInformationID
			AND SAL_DispatchVehicleInformation.CompanyID = SAL_DOVehicleInformation.CompanyID  
			AND SAL_DispatchVehicleInformation.PeriodID = SAL_DOVehicleInformation.PeriodID

INNER JOIN SAL_DispatchHeader ON SAL_DispatchHeader.DispatchID = SAL_DispatchVehicleInformation.DispatchID
		   AND SAL_DispatchHeader.CompanyID = SAL_DispatchVehicleInformation.CompanyID
		   AND SAL_DispatchHeader.PeriodID = SAL_DispatchVehicleInformation.PeriodID
INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = Sal_DispatchHeader.DocumentTypeID
			AND Set_DocumentType.CompanyID = Sal_DispatchHeader.CompanyID
			AND Set_DocumentType.PeriodID = Sal_DispatchHeader.PeriodID
			
		
		inner join INV_SET_Category on INV_SET_Category.CategoryID = SAL_DOVehicleInformation.CategoryID
		--and INV_SET_Category.companyID = SAL_DOVehicleInformation.companyID
		
			
		--select * from SAL_invoiceHeader order by createdate desc
		-- select * from SAL_InvoiceDetail order by createdate desc
		


WHERE 
--INV_StockTransactionHeader.StockTransactionID =  @StockTransactionID AND 
SAL_DOVehicleInformation.CompanyID =  @CompanyID
AND SAL_DOVehicleInformation.PeriodID = @PeriodID
AND SAL_DOVehicleInformation.Engine IS NOT NULL
AND SAL_DOVehicleInformation.Chassis IS NOT NULL
AND SAL_DOVehicleInformation.ColorID IS NOT NULL 
{0}


)Tab 
Where Tab.Flag <> 1
and CategoryName like '%HCV%'
";

       
        const string Query_GetVehicleForDTRForm = @" SELECT Distinct [SAL_DOVehicleInformation].[CompanyID]
      ,[SAL_DOVehicleInformation].[PeriodID]
      ,[SAL_DOVehicleInformation].[DOVehicleInformationID]
      ,[SAL_DOVehicleInformation].[DOHeaderID]
      ,[SAL_DOVehicleInformation].[DODetailID]
      ,[SAL_DOVehicleInformation].[CategoryID]
      ,[SAL_DOVehicleInformation].[ItemID]
      ,[SAL_DOVehicleInformation].[SerialNo]
      ,[SAL_DOVehicleInformation].[Engine]
      ,[SAL_DOVehicleInformation].[Chassis]
      ,[SAL_DOVehicleInformation].[Status]
      ,[SAL_DOVehicleInformation].[CreatedBy]
      ,[SAL_DOVehicleInformation].[CreateDate]
      ,[SAL_DOVehicleInformation].[CreatedByValue]
      ,[SAL_DOVehicleInformation].[UpdatedBy]
      ,[SAL_DOVehicleInformation].[UpdateDate]
      ,[SAL_DOVehicleInformation].[UpdatedByValue]
      ,[SAL_DOVehicleInformation].[ColorID]
      ,[SET_Color].[ColorName]
       FROM [SAL_DOVehicleInformation]

left join [SET_Color] 
on [SAL_DOVehicleInformation].[ColorID]=[SET_Color].[ColorID]

Inner Join Sal_DeliveryOrderHeader On Sal_DeliveryOrderHeader.DeliveryOrderID = [SAL_DOVehicleInformation].DOHeaderID
	AND Sal_DeliveryOrderHeader.CompanyID = [SAL_DOVehicleInformation].CompanyID
	AND Sal_DeliveryOrderHeader.PeriodID = [SAL_DOVehicleInformation].PeriodID
	AND Sal_DeliveryOrderHeader.Status = 1
Inner Join INV_StockTransactionDetail ON INV_StockTransactionDetail.SourceHeaderID = SAL_DOVehicleInformation.DOHeaderID
	AND INV_StockTransactionDetail.CompanyID = [SAL_DOVehicleInformation].CompanyID
	AND INV_StockTransactionDetail.PeriodID = [SAL_DOVehicleInformation].PeriodID
	AND INV_StockTransactionDetail.Status = 1
Inner Join INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID
	AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID
	AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID
	AND INV_StockTransactionHeader.Status = 1
Inner Join Sal_InvoiceDetail ON Sal_InvoiceDetail.StockHeaderID = INV_StockTransactionHeader.StockTransactionID
	AND Sal_InvoiceDetail.CompanyID = INV_StockTransactionHeader.CompanyID
	AND Sal_InvoiceDetail.PeriodID = INV_StockTransactionHeader.PeriodID
	AND Sal_InvoiceDetail.Status = 1
Inner Join Sal_InvoiceHeader ON Sal_InvoiceHeader.InvoiceID = Sal_InvoiceDetail.InvoiceHeaderID
	AND Sal_InvoiceHeader.CompanyID = Sal_InvoiceDetail.CompanyID
	AND Sal_InvoiceHeader.PeriodID = Sal_InvoiceDetail.PeriodID
	AND Sal_InvoiceHeader.Status = 1
where [SAL_DOVehicleInformation].[Status]=1 and [SET_Color].[Status]=1 and 
[SAL_DOVehicleInformation].[Engine] is not null and [SAL_DOVehicleInformation].[ColorID] is not null and
([SAL_DOVehicleInformation].[CompanyID] = @CompanyID OR @CompanyID is null) AND ([SAL_DOVehicleInformation].[PeriodID] = @PeriodID OR @PeriodID is null)";

        const string Query_GetDataByDOHandDOD = @" Select distinct * From
 (
 Select 
0 as DispatchID,
ISNULL((Select DispatchVehicle.VehicleInformationID From SAL_DispatchVehicleInformation DispatchVehicle
Where DispatchVehicle.VehicleInformationID = Sal_DOVehicleInformation.DOVehicleInformationID
AND isnull(DispatchVehicle.DispatchID,0) <> 0
),0)DId,
'True' as Checked,
Sal_DOVehicleInformation.DOVehicleInformationID,
Sal_DOVehicleInformation.Chassis,
Sal_DOVehicleInformation.Engine,
Sal_DOVehicleInformation.DOHeaderID,
Sal_DOVehicleInformation.DODetailID,
Sal_DOVehicleInformation.ColorID,
ColorName,
'' as BiltyNo,
ISNULL(SAL_DispatchVehicleInformation.VehicleInformationID,0)VehicleInformationID
,Sal_DOVehicleInformation.vehicleID

From Sal_DOVehicleInformation
LEFT JOIN Set_Color ON Set_Color.ColorID = Sal_DOVehicleInformation.ColorID
LEFT JOIN SAL_DispatchVehicleInformation ON SAL_DispatchVehicleInformation.VehicleInformationID = Sal_DOVehicleInformation.DOVehicleInformationID
	AND SAL_DispatchVehicleInformation.CompanyID = Sal_DOVehicleInformation.CompanyID
	AND SAL_DispatchVehicleInformation.PeriodID = Sal_DOVehicleInformation.PeriodID


Where Sal_DOVehicleInformation.DOHeaderID = @DOHid
	{0}
AND Sal_DOVehicleInformation.CompanyID = @CompanyID 
	AND Sal_DOVehicleInformation.PeriodID = @PeriodID
AND Sal_DOVehicleInformation.Status = 1
)Tab
--Where Tab.DOVehicleInformationID <> Tab.VehicleInformationID
--WHERE Tab.DId = 0
{1}
";
        const string Query_GetDataByDOHandDODwithBiltyNo = @" Select distinct DispatchID,DId,Checked,DOVehicleInformationID,Chassis,Engine,DOHeaderID,DODetailID,ColorID,
ColorName,VehicleInformationID,vehicleID,BiltyNo From
 (
 Select 
0 as DispatchID,
ISNULL((Select DispatchVehicle.VehicleInformationID From SAL_DispatchVehicleInformation DispatchVehicle
Where DispatchVehicle.VehicleInformationID = Sal_DOVehicleInformation.DOVehicleInformationID
AND isnull(DispatchVehicle.DispatchID,0) <> 0
),0)DId,
'True' as Checked,
Sal_DOVehicleInformation.DOVehicleInformationID,
Sal_DOVehicleInformation.Chassis,
Sal_DOVehicleInformation.Engine,
Sal_DOVehicleInformation.DOHeaderID,
Sal_DOVehicleInformation.DODetailID,
Sal_DOVehicleInformation.ColorID,
ColorName,
'0' as BiltyNo,
ISNULL(SAL_DispatchVehicleInformation.VehicleInformationID,0)VehicleInformationID
,Sal_DOVehicleInformation.vehicleID

From Sal_DOVehicleInformation
LEFT JOIN Set_Color ON Set_Color.ColorID = Sal_DOVehicleInformation.ColorID
LEFT JOIN SAL_DispatchVehicleInformation ON SAL_DispatchVehicleInformation.VehicleInformationID = Sal_DOVehicleInformation.DOVehicleInformationID
	AND SAL_DispatchVehicleInformation.CompanyID = Sal_DOVehicleInformation.CompanyID
	AND SAL_DispatchVehicleInformation.PeriodID = Sal_DOVehicleInformation.PeriodID


Where Sal_DOVehicleInformation.DOHeaderID = @DOHid
	{0}
AND Sal_DOVehicleInformation.CompanyID = @CompanyID 
	AND Sal_DOVehicleInformation.PeriodID = @PeriodID
AND Sal_DOVehicleInformation.Status = 1
)Tab
--Where Tab.DOVehicleInformationID <> Tab.VehicleInformationID
--WHERE Tab.DId = 0
{1}
group by
DispatchID,DId,Checked,DOVehicleInformationID,Chassis,Engine,DOHeaderID,DODetailID,ColorID,
ColorName,VehicleInformationID,vehicleID,BiltyNo
";

        const string Query_DeliveryOrderVehicleInformation = @"SELECT
SAL_DeliveryOrderHeader.DeliveryOrderID,
SAL_DeliveryOrderHeader.[DeliveryOrderNo],
SAL_DeliveryOrderHeader.DODate,
SAL_DeliveryOrderHeader.DocumentTypeID,
[SET_DocumentType].[DocumentType],
SAL_DeliveryOrderHeader.CustomerID,
[AR_SET_Customer].[CustomerName],
[AR_SET_Customer].[AddressLine1],
[AR_SET_Customer].[CityID],
(SELECT
CASE [AR_SET_Customer].[Provinceid]
WHEN 0 THEN 'First'
WHEN 1 THEN 'Balochistan'
WHEN 2 THEN 'KPK'
WHEN 3 THEN 'Punjab'
WHEN 4 THEN 'Sindh'
ELSE '-'
END) AS Province,
[SET_City].[CityName],
[SAL_DOVehicleInformation].[ItemID],
[INV_Item].[Description] as VehicleModel,
[SAL_DOVehicleInformation].[Chassis],
[SAL_DOVehicleInformation].[Engine],
[SAL_DOVehicleInformation].[ColorID],
[SET_Color].[ColorName],
SAL_DOVehicleInformation.DOVehicleInformationID    
,ISNULL((select LookupDetail.EnumeratedDescription from LookupDetail where LookUpDetailID = SAL_DeliveryOrderHeader.StatusCode 
    and LookupHeaderID = 5),'') as DocumentStatus 
FROM SAL_DeliveryOrderHeader

INNER JOIN SAL_DeliveryOrderDetail ON
[SAL_DeliveryOrderHeader].[DeliveryOrderID] = [SAL_DeliveryOrderDetail].[DOHeaderID] AND
[SAL_DeliveryOrderHeader].[CompanyID] = [SAL_DeliveryOrderDetail].[CompanyID] AND
[SAL_DeliveryOrderHeader].[PeriodID] = [SAL_DeliveryOrderDetail].[PeriodID] AND
[SAL_DeliveryOrderDetail].[Status] = 1

INNER JOIN SAL_DOVehicleInformation ON
[SAL_DeliveryOrderDetail].[DOHeaderID] = [SAL_DOVehicleInformation].[DOHeaderID] AND
[SAL_DeliveryOrderDetail].[DODetailID] = [SAL_DOVehicleInformation].[DODetailID] AND
[SAL_DeliveryOrderDetail].[CompanyID] = [SAL_DOVehicleInformation].[CompanyID] AND
[SAL_DeliveryOrderDetail].[PeriodID] = [SAL_DOVehicleInformation].[PeriodID] AND
[SAL_DOVehicleInformation].[Status] = 1

LEFT JOIN AR_SET_Customer ON 
[SAL_DeliveryOrderHeader].[CustomerID] = [AR_SET_Customer].[CustomerID] 
--AND [SAL_DeliveryOrderHeader].[CompanyID] = [AR_SET_Customer].[CompanyID]
AND[SAL_DeliveryOrderHeader].[PeriodID] = [AR_SET_Customer].[PeriodID] AND
[AR_SET_Customer].[Status] = 1

LEFT JOIN INV_Item ON 
[SAL_DOVehicleInformation].[ItemID] = [INV_Item].[ItemID] AND
--[SAL_DOVehicleInformation].[CompanyID] = [INV_Item].[CompanyID] AND
[INV_Item].[Status] = 1

LEFT JOIN SET_City ON
[AR_SET_Customer].[CityID] = [SET_City].[CityID] AND
[SET_City].[Status] = 1

LEFT JOIN SET_Color ON
[SAL_DOVehicleInformation].[ColorID] = [SET_Color].[ColorID] AND
[SET_Color].[Status] = 1

LEFT JOIN SET_DocumentType ON
[SAL_DeliveryOrderHeader].[DocumentTypeID] = [SET_DocumentType].[DocumentTypeID] AND
[SAL_DeliveryOrderHeader].[CompanyID] = [SET_DocumentType].[CompanyID] AND
[SAL_DeliveryOrderHeader].[PeriodID] = [SET_DocumentType].[PeriodID] AND
[SET_DocumentType].[Status] = 1";

        const string Query_GetDataForJobCard = @"
Select SAL_DispatchHeader.DispatchNo,SAL_DispatchHeader.DispatchID,
SAL_DOVehicleInformation.DOHeaderID,
SAL_DOVehicleInformation.DODetailID,
SAL_DOVehicleInformation.DOVehicleInformationID,
SAL_DOVehicleInformation.Engine,
SAL_DOVehicleInformation.Chassis,
SAL_DOVehicleInformation.SerialNo,
SET_Color.ColorName 
From SAL_DispatchHeader

INNER JOIN Sal_DispatchVehicleInformation ON Sal_DispatchVehicleInformation.DispatchID = SAL_DispatchHeader.DispatchID AND
	SAL_DispatchHeader.CompanyID = Sal_DispatchVehicleInformation.CompanyID AND 
	SAL_DispatchHeader.PeriodID = Sal_DispatchVehicleInformation.PeriodID
	
INNER JOIN [SAL_DOVehicleInformation] ON [SAL_DOVehicleInformation].DOVehicleInformationID = Sal_DispatchVehicleInformation.VehicleInformationID AND
	[SAL_DOVehicleInformation].CompanyID = Sal_DispatchVehicleInformation.CompanyID AND
	[SAL_DOVehicleInformation].PeriodID = Sal_DispatchVehicleInformation.PeriodID
	
INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = SAL_DispatchHeader.DocumentTypeID AND
		Set_DocumentType.CompanyID = SAL_DispatchHeader.CompanyID AND
		Set_DocumentType.PeriodID = SAL_DispatchHeader.PeriodID

LEFT JOIN SET_Color on [SAL_DOVehicleInformation].[ColorID]= [SET_Color].[ColorID]	

WHERE 
Sal_DispatchVehicleInformation.CompanyID = @CompanyID
AND Sal_DispatchVehicleInformation.PeriodID = @PeriodID
";

        const string Query_GetVehicleDataByChassisNo = @" 
        Select SAL_DOVehicleInformation.DOVehicleInformationID, SAL_DOVehicleInformation.Chassis, SAL_DOVehicleInformation.Engine,
INV_Item.ItemID, INV_Item.Description, INV_SET_Category.CategoryID, INV_SET_Category.CategoryName,
Set_Color.ColorName, AS_CheckListInfo.WarrantyBookNo, AS_CheckListInfo.CreateDate
From SAL_DOVehicleInformation

INNER JOIN INV_Item On INV_Item.ItemID = SAL_DOVehicleInformation.ItemID 
--AND INV_Item.CompanyID = SAL_DOVehicleInformation.CompanyID
	
INNER JOIN INV_Set_Category ON INV_Set_Category.CategoryID = SAL_DOVehicleInformation.CategoryID
	--AND INV_Set_Category.CompanyID	= SAL_DOVehicleInformation.CompanyID
	
INNER JOIN SET_Color ON Set_Color.ColorID = SAL_DOVehicleInformation.ColorID	

left JOIN AS_CheckListInfo ON AS_CheckListInfo.DOVehicleInformationID = SAL_DOVehicleInformation.DOVehicleInformationID AND
	AS_CheckListInfo.CompanyID = SAL_DOVehicleInformation.CompanyID 
	AND AS_CheckListInfo.PeriodID = SAL_DOVehicleInformation.PeriodID

Where 
SAL_DOVehicleInformation.CompanyID = @CompanyID
AND SAL_DOVehicleInformation.PeriodID = @PeriodID
AND SAL_DOVehicleInformation.Chassis = @Chassis
";

        #endregion

        #region Class Methods

        public bool InsertDOVIHeaderAndDetail(int Cid, int pID, List<SAL_DOVehicleInformation> colDOVIDetail)
        {
            bool result = false;

            try
            {
                foreach (SAL_DOVehicleInformation dovi in colDOVIDetail)
                {
                    dovi.DOVehicleInformationID = short.Parse(SAL_DOVehicleInformation.GetNewDOVehicleInformationID(Cid, pID).ToString());
                    dovi.Insert();
                }
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

            }

            return result;
        }

        public bool UpdateDOVIHeaderAndDetail(int Cid, int pID, List<SAL_DOVehicleInformation> colUpdateDOVIDetail, List<SAL_DOVehicleInformation> colDeleteDOVIDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateDoVehicle");
            try
            {
                //this.UpdateDataByKey(sqltrans);

                foreach (SAL_DOVehicleInformation doviUpdate in colUpdateDOVIDetail)
                {
                    doviUpdate.UpdateDataByKey(sqltrans);

                    SAL_DispatchVehicleInformation sdvi = new SAL_DispatchVehicleInformation();
                    sdvi.VehicleInformationID = doviUpdate.DOVehicleInformationID;
                    sdvi.VehicleID = (int)doviUpdate.VehicleID;
                    sdvi.CompanyID = CompanyID;
                    sdvi.PeriodID = PeriodID;
                    sdvi.UpdateDataByVehicleID(sqltrans);

                }

                foreach (SAL_DOVehicleInformation doviDelete in colDeleteDOVIDetail)
                {
                    SAL_DOVehicleInformation.DeleteDataByKey(null, doviDelete.UpdatedBy.Value, doviDelete.UpdateDate.Value, doviDelete.UpdatedByValue, Cid, doviDelete.DOVehicleInformationID, pID);

                    SAL_DispatchVehicleInformation sdvi = new SAL_DispatchVehicleInformation();
                    sdvi.VehicleInformationID = null;
                    sdvi.VehicleID = (int)doviDelete.VehicleID;
                    sdvi.CompanyID = CompanyID;
                    sdvi.PeriodID = PeriodID;
                    sdvi.UpdateDataByVehicleID(sqltrans);

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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateDoVehicle");
            }

            return result;
        }

        public static DataTable GetDataByKeyTable(int deliveryOrderID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@DeliveryOrderID", deliveryOrderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_DeliveryOrderID_CompanyId_PeriodId, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_Select_By_DeliveryOrderID_CompanyId_PeriodId, sqlparams);

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


        public static DataSet Report_GetDeliveryOrderOfvehicle(int companyID, int periodID, int CategoryID, Nullable<DateTime> Date_From = null, Nullable<DateTime> Date_To = null, int DoFrom = 0, int DoTo = 0, Nullable<int> DocStatus = null)
        {
            DataSet dt = null;
            try
            {
                StringBuilder sb = new StringBuilder();
                //where Cane_Grower.PaymentMode = 3 and Cane_Grower.IsActive=1 and Cane_Grower.Status=1 

                sb.Append("WHERE Sal_DeliveryOrderHeader.CompanyID = " + companyID + " and Sal_DeliveryOrderHeader.PeriodID = " + periodID);
                if (Date_From != null && Date_To != null)
                    sb.Append(" and CAST(CONVERT(VARCHAR(10), Sal_DeliveryOrderHeader.DODate, 110) AS DATETIME) between '" + ((DateTime)Date_From).ToString("MM-dd-yyyy") + "' and '" + ((DateTime)Date_To).ToString("MM-dd-yyyy") + "'");
                else if (Date_From != null)
                    sb.Append(" and CAST(CONVERT(VARCHAR(10), Sal_DeliveryOrderHeader.DODate, 110) AS DATETIME) >= '" + ((DateTime)Date_From).ToString("MM-dd-yyyy") + "'");
                else if (Date_To != null)
                    sb.Append(" and CAST(CONVERT(VARCHAR(10), Sal_DeliveryOrderHeader.DODate, 110) AS DATETIME) <= '" + ((DateTime)Date_To).ToString("MM-dd-yyyy") + "'");


                if (DoFrom > 0 && DoTo > 0)
                {
                    sb.Append(" AND Sal_DeliveryOrderHeader.DeliveryOrderNo Between " + DoFrom + " AND " + DoTo + " ");
                }

                if (DoFrom > 0 && DoTo == 0)
                {
                    sb.Append(" AND Sal_DeliveryOrderHeader.DeliveryOrderNo = " + DoFrom);

                }

                else if (CategoryID > 0)
                {
                    sb.Append(" AND Sal_DOVehicleInformation.CategoryID = " + CategoryID);
                }

                if (DocStatus.HasValue)
                {
                    sb.Append(" AND Sal_DeliveryOrderHeader.StatusCode = " + DocStatus);
                }

                

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDeliveryOrderVehicle, sb.ToString()));
                
                if (ds != null && ds.Tables.Count > 1)
                    dt = ds;

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetAllDataWithEngine(Nullable<int> companyID, Nullable<int> periodID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_AllActiveDataWithColor, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetDataForDTR(Nullable<int> companyID, Nullable<int> periodID, string engineNo)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Engine", engineNo);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataForDTR, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;

        }

        public static DataRow GetItemName(int companyID, int periodID, short vehicleInformationID)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@VehicleInformationID", vehicleInformationID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetItemName, sqlparams);

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

        public static DataRow GetSalesInvoice(int companyID, int periodID, short vehicleInformationID)
        {
            DataRow dr = null;

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(" [SAL_DOVehicleInformation].[Status] = 1 AND [SAL_DOVehicleInformation].[CompanyID] = @CompanyID AND [SAL_DOVehicleInformation].[PeriodID] = @PeriodID AND [SAL_DOVehicleInformation].DOVehicleInformationID = @VehicleInformationID");
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@VehicleInformationID", vehicleInformationID);



                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text,string.Format( Query_SalesInvoice  + sb), sqlparams);

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

        public static DataTable GetAllDataWithEngineByStockTransactionID(Nullable<int> companyID, Nullable<int> periodID, int? stockTransactionID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@StockTransactionID", stockTransactionID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_AllActiveDataWithColorByStockTransactionID, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable Report_vehicle_SalesInvoices(int companyID, int periodID, int? itemCategoryID, Nullable<DateTime> Date_From = null, Nullable<DateTime> Date_To = null, int DoFrom = 0, int DoTo = 0, string EngineNumber = "")
        {
            DataTable dt = null;
            try
            {
                StringBuilder sb = new StringBuilder();
                //where Cane_Grower.PaymentMode = 3 and Cane_Grower.IsActive=1 and Cane_Grower.Status=1 

                sb.Append("WHERE Sal_InvoiceHeader.CompanyID = " + companyID + " and Sal_InvoiceHeader.PeriodID = " + periodID);
                if (Date_From != null && Date_To != null)
                    sb.Append(" and CAST(CONVERT(VARCHAR(10), Sal_InvoiceHeader.InvoiceDate, 110) AS DATETIME) between '" + ((DateTime)Date_From).ToString("MM-dd-yyyy") + "' and '" + ((DateTime)Date_To).ToString("MM-dd-yyyy") + "'");
                else if (Date_From != null)
                    sb.Append(" and CAST(CONVERT(VARCHAR(10), Sal_InvoiceHeader.InvoiceDate, 110) AS DATETIME) >= '" + ((DateTime)Date_From).ToString("MM-dd-yyyy") + "'");
                else if (Date_To != null)
                    sb.Append(" and CAST(CONVERT(VARCHAR(10), Sal_InvoiceHeader.InvoiceDate, 110) AS DATETIME) <= '" + ((DateTime)Date_To).ToString("MM-dd-yyyy") + "'");               

                if (DoFrom > 0 && DoTo > 0)
                {
                    sb.Append(" AND Sal_InvoiceHeader.InvoiceNumber Between " + DoFrom + " AND " + DoTo + " ");
                }
                if (itemCategoryID != 0 && itemCategoryID != null)
                {
                    sb.Append(" And INV_Item.ItemTypeID = " + itemCategoryID);
                }

                else if (DoFrom > 0 && DoTo == 0)
                {
                    sb.Append(" AND Sal_InvoiceHeader.InvoiceNumber = " + DoFrom);

                }
                if (EngineNumber != "")
                    sb.Append(" AND SAL_DOVehicleInformation.Engine = '" + EngineNumber + "' ");


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SalesVehicleInoices_ + sb.ToString()));

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

        public static DataTable Report_vehicleSalesInvoices(int companyID, int periodID, Nullable<DateTime> Date_From = null, Nullable<DateTime> Date_To = null, int DoFrom = 0, int DoTo = 0, string EngineNumber = "")
        {
            DataTable dt = null;
            try
            {
                StringBuilder sb = new StringBuilder();
                //where Cane_Grower.PaymentMode = 3 and Cane_Grower.IsActive=1 and Cane_Grower.Status=1 

                sb.Append("and Sal_InvoiceHeader.CompanyID = " + companyID + " and Sal_InvoiceHeader.PeriodID = " + periodID);
                if (Date_From != null && Date_To != null)
                    sb.Append(" and CAST(CONVERT(VARCHAR(10), Sal_InvoiceHeader.InvoiceDate, 110) AS DATETIME) between '" + ((DateTime)Date_From).ToString("MM-dd-yyyy") + "' and '" + ((DateTime)Date_To).ToString("MM-dd-yyyy") + "'");
                else if (Date_From != null)
                    sb.Append(" and CAST(CONVERT(VARCHAR(10), Sal_InvoiceHeader.InvoiceDate, 110) AS DATETIME) >= '" + ((DateTime)Date_From).ToString("MM-dd-yyyy") + "'");
                else if (Date_To != null)
                    sb.Append(" and CAST(CONVERT(VARCHAR(10), Sal_InvoiceHeader.InvoiceDate, 110) AS DATETIME) <= '" + ((DateTime)Date_To).ToString("MM-dd-yyyy") + "'");


                if (DoFrom > 0 && DoTo > 0)
                {
                    sb.Append(" AND Sal_InvoiceHeader.InvoiceNumber Between " + DoFrom + " AND " + DoTo + " ");
                }

                else if (DoFrom > 0 && DoTo == 0)
                {
                    sb.Append(" AND Sal_InvoiceHeader.InvoiceNumber = " + DoFrom);

                }
                if (EngineNumber != "")
                    sb.Append(" AND SAL_DOVehicleInformation.Engine = '" + EngineNumber + "' ");


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SalesInvoice + sb.ToString()));

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

        public static DataTable Report_VehicleEnquiryProgram(int companyId, int periodID, string chassisNo, string _PSONumber)
        {
            DataTable dt = null;
            string where = string.Empty;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyId);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);


                if (!string.IsNullOrEmpty(chassisNo))
                    where = @" AND Chassis like '%" + chassisNo + "%'";

                if (!string.IsNullOrEmpty(_PSONumber))
                    where = @" AND SAL_OrderHeader.referenceno like '" + _PSONumber + "'";


                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_ReportVehicleEnquiryProgram + where, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static bool DeleteDataByDoHeaderAndDetailID(int doHeaderID, int doDetailID, int companyID, int periodID, Guid updatedBy, DateTime updateDate, string updatedByValue)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@doHeaderID", doHeaderID);
                sqlparams[3] = new SqlParameter("@doDetailID", doDetailID);
                sqlparams[4] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[5] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[6] = new SqlParameter("@UpdatedByValue", updatedByValue);


                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteDataByDoHeaderAndDetail, sqlparams) > 0)
                    result = true;

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataTable GetVehicleForAfterSalesForm(Nullable<int> companyID, Nullable<int> periodID,string filter = "")
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDataForAfterSales,filter) , sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetVehicleForServicePerformanceForm(Nullable<int> companyID, Nullable<int> periodID, string Query_GetDataForServicePerformance, string Where = "")
        {
            if (string.IsNullOrEmpty(Where))
                Where = "";

            DataTable dt = null;
            
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataForServicePerformance + Where, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetVehicleForDTRForm(Nullable<int> companyID, Nullable<int> periodID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetVehicleForDTRForm, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetDataByDOHandDOD(int DOHID, int DODID, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();
                //AND Sal_DOVehicleInformation.DODetailID = @DODid
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@DOHid", DOHID);
                sqlparams[1] = new SqlParameter("@DODid", DODID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);

                if (DODID > 0)
                {
                    sb.Append(" AND Sal_DOVehicleInformation.DODetailID = @DODid ");
                }
                else
                {
                    sb1.Append(" WHERE Tab.DId = 0");
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDataByDOHandDOD, sb.ToString(),sb1.ToString()), sqlparams);

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

        public static DataTable GetDataByDOHandDODWIthBilty(int DOHID, int DODID, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();
                //AND Sal_DOVehicleInformation.DODetailID = @DODid
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@DOHid", DOHID);
                sqlparams[1] = new SqlParameter("@DODid", DODID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);

                if (DODID > 0)
                {
                    sb.Append(" AND Sal_DOVehicleInformation.DODetailID = @DODid ");
                }
                else
                {
                    sb1.Append(" WHERE Tab.DId = 0");
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDataByDOHandDODwithBiltyNo, sb.ToString(), sb1.ToString()), sqlparams);

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


        public static DataTable Get_DeliveryOrderVehicleInformation(int companyID, int periodID, string DONumber, Nullable<int> customerID = null, Nullable<int> categoryID = null, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null, Nullable<int> DocStatus = null,string Do_ID = "")
        {
            DataTable dtToReturn = null;

            try
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                sb.Append(" WHERE [SAL_DeliveryOrderHeader].[CompanyID] = " + companyID + " AND [SAL_DeliveryOrderHeader].[PeriodID] =  " + periodID + " AND [SAL_DeliveryOrderHeader].[Status] = 1");

                #region Approval Date filter

                if (dateFrom != null && dateTo != null)
                    sb.Append(" AND Cast(SAL_DeliveryOrderHeader.DODate AS Date) BETWEEN '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' AND '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");
                else if (dateFrom != null)
                    sb.Append(" AND Cast(SAL_DeliveryOrderHeader.DODate AS Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    sb.Append(" AND Cast(SAL_DeliveryOrderHeader.DODate AS Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                #endregion
                if (customerID > 0)
                    sb.Append(" And [SAL_DeliveryOrderHeader].[CustomerID] = " + customerID);
                
                if (categoryID > 0)
                    sb.Append(" AND [SAL_DOVehicleInformation].[CategoryID] = " + categoryID);

                if (!string.IsNullOrEmpty(DONumber))
                    sb.Append(" And [SAL_DeliveryOrderHeader].[DeliveryOrderNo] = '" + DONumber + "'");

                if (DocStatus.HasValue)
                    sb.Append(" AND SAL_DeliveryOrderHeader.StatusCode = " + DocStatus);


                if(Do_ID != "")
                    sb.Append(" AND [SAL_DeliveryOrderHeader].DeliveryOrderID = " + Do_ID);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_DeliveryOrderVehicleInformation + sb.ToString()));

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

        public static DataTable GetVehicleForJobCardForm(Nullable<int> companyID, Nullable<int> periodID, string Where = "")
        {
            if (string.IsNullOrEmpty(Where))
                Where = "";

            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataForJobCard + Where, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataRow GetVehicleDataByChassisNo(int companyID, int periodID, string Chassis, string where = "", SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Chassis", Chassis);

                DataSet ds = null;
                if (objTransaction == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetVehicleDataByChassisNo + where, sqlparams);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetVehicleDataByChassisNo + where, sqlparams);
                }

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


        public static bool VehicleDeleteByKey(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, int companyID, short dOVehicleInformationID, int periodID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@DOVehicleInformationID", dOVehicleInformationID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", false);
                sqlparams[4] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[5] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[6] = new SqlParameter("@UpdatedByValue", updatedByValue);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_VehicleDelete_ByKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_VehicleDelete_ByKey, sqlparams) > 0)
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
