using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;

using FnI.BusinessAccessLayer.AR.Setup;
using FnI.BusinessAccessLayer.Setup;
using System.Text;

namespace FnI.BusinessAccessLayer.Sales
{
   public partial class SAL_DispatchVehicleInformation
   {
       //RowUniqueID
       private int f_RowUniqueID;

       public int RowUniqueID
       {
           get { return f_RowUniqueID; }
           set { f_RowUniqueID = value; }
       }

       #region Constant Strings
       const string Query_VehicleInformation = @"

	
		select 

		Sal_DispatchVehicleInformation.VehicleID
        ,Sal_DispatchVehicleInformation.Chassis
		,Sal_DispatchVehicleInformation.Engine
		,Sal_DispatchVehicleInformation.ProductionNo
		,Sal_DispatchVehicleInformation.LotNo
		,Sal_DispatchVehicleInformation.StockHeaderID
		,Sal_DispatchVehicleInformation.StockDetailID
		,SET_Color.ColorName
		 from  INV_StockTransactionDetail
		INNER JOIN Sal_DispatchVehicleInformation  ON INV_StockTransactionDetail.TransactionDetailID = Sal_DispatchVehicleInformation.StockDetailID
and  Sal_DispatchVehicleInformation.StockHeaderID =INV_StockTransactionDetail.StockTransactionID  
AND  Sal_DispatchVehicleInformation.CompanyID = INV_StockTransactionDetail.CompanyID 
AND Sal_DispatchVehicleInformation.PeriodID = INV_StockTransactionDetail.PeriodID
AND INV_StockTransactionDetail.status = 1

left join SET_Color on SET_Color.ColorID  = Sal_DispatchVehicleInformation.ColorID
and SET_Color.Status =1 

where INV_StockTransactionDetail.CompanyID = @CompanyID
and INV_StockTransactionDetail.PeriodID = @PeriodID
and Sal_DispatchVehicleInformation.StockHeaderID  = @StockHeaderID
and Sal_DispatchVehicleInformation.StockDetailID  = @StockDetailID
";


       const string Query_Delete_ByKey = @" Delete From SAL_DispatchVehicleInformation
Where DispatchID = @DispatchID AND CompanyID = @CompanyID AND PeriodID = @PeriodID";

       const string Query_GetDataByDispatchID = @" Select 
SAL_DispatchVehicleInformation.DispatchID,
'True' as Checked,
SAL_DispatchVehicleInformation.VehicleInformationID as DOVehicleInformationID,
Sal_DOVehicleInformation.Chassis,
Sal_DOVehicleInformation.Engine,
Sal_DOVehicleInformation.DOHeaderID,
Sal_DOVehicleInformation.DODetailID,
Sal_DOVehicleInformation.ColorID,
ColorName,
SAL_DispatchVehicleInformation.BiltyNo,
ISNULL(SAL_DispatchVehicleInformation.VehicleInformationID,0)VehicleInformationID

From SAL_DispatchVehicleInformation

INNER JOIN Sal_DOVehicleInformation ON SAL_DispatchVehicleInformation.VehicleInformationID = Sal_DOVehicleInformation.DOVehicleInformationID
	AND SAL_DispatchVehicleInformation.CompanyID = Sal_DOVehicleInformation.CompanyID
	AND SAL_DispatchVehicleInformation.PeriodID = Sal_DOVehicleInformation.PeriodID
LEFT JOIN Set_Color ON Set_Color.ColorID = Sal_DOVehicleInformation.ColorID

Where 
SAL_DispatchVehicleInformation.CompanyID = @CompanyID
AND SAL_DispatchVehicleInformation.CompanyID = @PeriodID
AND SAL_DispatchVehicleInformation.DispatchID = @DispatchID";

       const string Query_GetVehicleForSalesInvoice = @" Select SAL_DOVehicleInformation.DOVehicleInformationID,
SAL_DOVehicleInformation.Engine,
SAL_DOVehicleInformation.Chassis,
SAL_DOVehicleInformation.SerialNo,
SET_Color.ColorName
From Sal_DispatchVehicleInformation

INNER JOIN SAl_DOVehicleInformation on SAl_DOVehicleInformation.DOVehicleInformationID = Sal_DispatchVehicleInformation.vehicleInformationID
	AND Sal_DOVehicleInformation.CompanyID = Sal_DispatchVehicleInformation.CompanyID
	AND Sal_DOVehicleInformation.PeriodID = Sal_DispatchVehicleInformation.PeriodID
LEFT JOIN SET_Color on [SAL_DOVehicleInformation].[ColorID]= [SET_Color].[ColorID]

LEFT JOIN SAL_InvoiceDetail ON SAL_InvoiceDetail.VehicleID = Sal_DispatchVehicleInformation.vehicleInformationID
	AND SAL_InvoiceDetail.CompanyID = Sal_DispatchVehicleInformation.CompanyID
	AND SAL_InvoiceDetail.PeriodID = Sal_DispatchVehicleInformation.PeriodID
LEFT JOIN SAL_InvoiceHeader ON SAL_InvoiceHeader.InvoiceID = SAL_InvoiceDetail.InvoiceHeaderID
	AND SAL_InvoiceHeader.CompanyID = SAL_InvoiceDetail.CompanyID
	AND SAL_InvoiceHeader.PeriodID = SAL_InvoiceDetail.PeriodID

WHERE Sal_DispatchVehicleInformation.CompanyID = @CompanyID
	AND Sal_DispatchVehicleInformation.PeriodID = @PeriodID
    ";

       const string Query_GetVehicleForGRN = @" 
Select 
Sal_DispatchVehicleInformation.VehicleID,
Sal_DispatchVehicleInformation.StockHeaderID,
--Sal_DispatchVehicleInformation.StockDetailID,
INVDetail.SerialNo as StockDetailID,
Sal_DispatchVehicleInformation.Engine,
Sal_DispatchVehicleInformation.Chassis,
ISNULL(Sal_DispatchVehicleInformation.ColorID,-1)ColorID,
SET_Color.ColorName,
INVDetail.SerialNo
From Sal_DispatchVehicleInformation

LEFT JOIN SET_Color ON SET_Color.ColorID = Sal_DispatchVehicleInformation.ColorID
INNER JOIN INV_StockTransactionDetail INVDetail ON INVDetail.TransactionDetailID = Sal_DispatchVehicleInformation.StockDetailID
AND INVDetail.CompanyID = Sal_DispatchVehicleInformation.CompanyID AND INVDetail.PeriodID = Sal_DispatchVehicleInformation.PeriodID
Where Sal_DispatchVehicleInformation.CompanyID = @CompanyID AND Sal_DispatchVehicleInformation.PeriodID = @PeriodID
		AND Sal_DispatchVehicleInformation.StockHeaderID = @StockHeaderID";

       const string Query_GetVehicleForPRN = @" 
Select 
-1 as RowUniqueID,
Sal_DispatchVehicleInformation.VehicleID,
Sal_DispatchVehicleInformation.StockHeaderID,
Sal_DispatchVehicleInformation.StockDetailID,
--INVDetail.SerialNo as StockDetailID,
Sal_DispatchVehicleInformation.Engine,
Sal_DispatchVehicleInformation.Chassis,
ISNULL(Sal_DispatchVehicleInformation.ColorID,-1)ColorID,
SET_Color.ColorName,
Sal_DispatchVehicleInformation.ProductionNo,
Sal_DispatchVehicleInformation.LotNo,
INVDetail.SerialNo
From Sal_DispatchVehicleInformation

LEFT JOIN SET_Color ON SET_Color.ColorID = Sal_DispatchVehicleInformation.ColorID
INNER JOIN INV_StockTransactionDetail INVDetail ON INVDetail.TransactionDetailID = Sal_DispatchVehicleInformation.StockDetailID
AND INVDetail.CompanyID = Sal_DispatchVehicleInformation.CompanyID AND INVDetail.PeriodID = Sal_DispatchVehicleInformation.PeriodID
Where Sal_DispatchVehicleInformation.CompanyID = @CompanyID AND Sal_DispatchVehicleInformation.PeriodID = @PeriodID
		AND Sal_DispatchVehicleInformation.StockHeaderID = @StockHeaderID";


       const string Query_GetDispatchedVehicles = @" Select SAL_DOVehicleInformation.DOVehicleInformationID,
SAL_DOVehicleInformation.Engine,
SAL_DOVehicleInformation.Chassis,
SAL_DOVehicleInformation.SerialNo,
SET_Color.ColorName,
Sal_DispatchHeader.DocumentTypeID,
SET_DocumentType.DocumentType
From Sal_DispatchHeader

INNER JOIN Sal_DispatchVehicleInformation sdvi on sdvi.DispatchID = Sal_DispatchHeader.DispatchID
	AND sdvi.CompanyID = Sal_DispatchHeader.CompanyID AND Sal_DispatchHeader.PeriodID = sdvi.PeriodID
	
INNER JOIN SAl_DOVehicleInformation on SAl_DOVehicleInformation.DOVehicleInformationID = sdvi.vehicleInformationID
	AND Sal_DOVehicleInformation.CompanyID = sdvi.CompanyID
	AND Sal_DOVehicleInformation.PeriodID = sdvi.PeriodID
LEFT JOIN SET_Color on [SAL_DOVehicleInformation].[ColorID]= [SET_Color].[ColorID]	

INNER JOIN SET_DocumentType ON SET_DocumentType.DocumentTypeID = Sal_DispatchHeader.DocumentTypeID
AND SET_DocumentType.CompanyID = Sal_DispatchHeader.CompanyID
AND SET_DocumentType.PeriodID = Sal_DispatchHeader.PeriodID


Where Sal_DispatchHeader.Status = 1 AND Sal_DispatchHeader.StatusCode = 1
		AND Sal_DispatchHeader.CompanyID = @CompanyID AND SAl_DispatchHeader.PeriodID = @PeriodID
		";


       const string Query_Delete_ByStockHeaderID = @" Delete From SAL_DispatchVehicleInformation
Where StockHeaderID = @StockHeaderID AND CompanyID = @CompanyID AND PeriodID = @PeriodID";

       const string Query_Delete_ByStockDetailID = @" Delete From SAL_DispatchVehicleInformation
Where StockDetailID = @StockDetailID AND CompanyID = @CompanyID AND PeriodID = @PeriodID";

       const string Query_GetVehicleForLookupVehiclePrnGrn = @"
Select 
SAl_DispatchVehicleInformation.VehicleInformationID,
SAl_DispatchVehicleInformation.Chassis, 
SAl_DispatchVehicleInformation.Engine, 
SAl_DispatchVehicleInformation.ColorID,
Set_Color.ColorName, 
SAl_DispatchVehicleInformation.VehicleID,
Set_DocumentType.DocumentType

From SAl_DispatchVehicleInformation 

INNER JOIN Set_Color ON Set_Color.ColorID = SAl_DispatchVehicleInformation.ColorID

INNER JOIN INV_StockTransactionDetail on INV_StockTransactionDetail.TransactionDetailID = SAl_DispatchVehicleInformation.StockDetailID
	AND INV_StockTransactionDetail.StockTransactionID = SAl_DispatchVehicleInformation.StockHeaderID
	AND INV_StockTransactionDetail.CompanyID = SAl_DispatchVehicleInformation.CompanyID
	AND INV_StockTransactionDetail.PeriodID = SAl_DispatchVehicleInformation.PeriodID
	
INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
	AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
	AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID
	
INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
	AND Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID 
	AND Set_DocumentType.PeriodID = INV_stockTransactionHeader.PeriodID	 

LEFT JOIN Sal_DOVehicleInformation ON Sal_DOVehicleInformation.VehicleID = Sal_DispatchVehicleInformation.VehicleID
	AND Sal_DispatchVehicleInformation.CompanyID = SAL_DOVehicleInformation.CompanyID
	AND Sal_DispatchVehicleInformation.PeriodID = SAL_DOVehicleInformation.PeriodID

INNER JOIN INV_ITem ON INV_ITem.ItemID = INV_StockTransactionDetail.ItemID
	--AND INV_Item.CompanyID = INV_StockTransactionDetail.CompanyID
	
INNER JOIN Inv_Set_Category ON Inv_Set_Category.categoryID = INV_Item.ItemTypeID
	-- 

Where ISNULL(SAl_DispatchVehicleInformation.VehicleInformationID,0) = 0 
	AND ISNULL(SAl_DispatchVehicleInformation.DispatchID,0) = 0
	AND  SAl_DispatchVehicleInformation.CompanyID = @CompanyID
	AND  SAl_DispatchVehicleInformation.PeriodID = @PeriodID
	AND ISNULL(SAl_DispatchVehicleInformation.VehicleID,0) <> ISNULL(SAL_DOVehicleInformation.VehicleID,0)";

       const string Query_UpdateDataByVehicleID = @" UPDATE [SAL_DispatchVehicleInformation] SET [VehicleInformationID] = @VehicleInformationID
		WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [VehicleID] = @VehicleID ";

       const string Query_UpdateDataByVehicleInformationID = @" UPDATE [SAL_DispatchVehicleInformation] SET [VehicleInformationID] = null
		WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [VehicleInformationID] = @VehicleInformationID";

       const string Query_ColorWiseUnAllocatedStock = @"SELECT 
INV_StockTransactionHeader.DocumentDate,
INV_SET_Category.CategoryName,
INV_Item.Description,'Yard' as status,
SAL_DispatchVehicleInformation.Chassis,
SAL_DispatchVehicleInformation.Engine,
SET_Color.ColorName,
SAL_DispatchVehicleInformation.LotNo,
INV_StockTransactionDetail.Comments

 from INV_StockTransactionHeader

 inner join SAL_DispatchVehicleInformation on INV_StockTransactionHeader.DocumentTypeID=SAL_DispatchVehicleInformation.DocumentTypeID
 
 and INV_StockTransactionHeader.DocumentNo=SAL_DispatchVehicleInformation.DocumentNo
  
inner join SET_DocumentType on SET_DocumentType.DocumentTypeID=SAL_DispatchVehicleInformation.DocumentTypeID

inner join  SET_Color on SET_Color.ColorID=SAL_DispatchVehicleInformation.ColorID

INNER JOIN INV_StockTransactionDetail ON SAL_DispatchVehicleInformation.StockHeaderID =  INV_StockTransactionDetail.StockTransactionID 

AND SAL_DispatchVehicleInformation.StockDetailID = INV_StockTransactionDetail.TransactionDetailID

INNER JOIN INV_Item ON INV_Item.ItemID = INV_StockTransactionDetail.ItemID

inner join INV_SET_Category on INV_SET_Category.CategoryID=INV_Item.ItemTypeID 
-- 

where SAL_DispatchVehicleInformation.CompanyID = @CompanyID AND
SAL_DispatchVehicleInformation.PeriodID = @PeriodID AND [INV_StockTransactionHeader].[DocumentDate]  between @FromDate and @ToDate
and SAL_DispatchVehicleInformation.VehicleInformationID is not null

";

       const string Query_ColorWiseAllocatedStock = @"SELECT 
INV_StockTransactionHeader.DocumentDate,SAL_DispatchVehicleInformation.VehicleInformationID,
INV_SET_Category.CategoryName,
INV_Item.Description,'' as status,
SAL_DispatchVehicleInformation.Chassis,
SAL_DispatchVehicleInformation.Engine,
SET_Color.ColorName,
SAL_DispatchVehicleInformation.LotNo,
INV_StockTransactionDetail.Comments

 from INV_StockTransactionHeader

 inner join SAL_DispatchVehicleInformation on INV_StockTransactionHeader.DocumentTypeID=SAL_DispatchVehicleInformation.DocumentTypeID
 
 and INV_StockTransactionHeader.DocumentNo=SAL_DispatchVehicleInformation.DocumentNo
  
inner join SET_DocumentType on SET_DocumentType.DocumentTypeID=SAL_DispatchVehicleInformation.DocumentTypeID

inner join  SET_Color on SET_Color.ColorID=SAL_DispatchVehicleInformation.ColorID

INNER JOIN INV_StockTransactionDetail ON SAL_DispatchVehicleInformation.StockHeaderID =  INV_StockTransactionDetail.StockTransactionID 

AND SAL_DispatchVehicleInformation.StockDetailID = INV_StockTransactionDetail.TransactionDetailID

INNER JOIN INV_Item ON INV_Item.ItemID = INV_StockTransactionDetail.ItemID

inner join INV_SET_Category on INV_SET_Category.CategoryID=INV_Item.ItemTypeID 
-- 

where SAL_DispatchVehicleInformation.CompanyID = @CompanyID AND
SAL_DispatchVehicleInformation.PeriodID = @PeriodID AND [INV_StockTransactionHeader].[DocumentDate]  between @FromDate and @ToDate
and SAL_DispatchVehicleInformation.VehicleInformationID is null
";


       const string Query_GetDataByChassisNo = @" Select Chassis From Sal_DispatchVehicleInformation
Where CompanyID = @CompanyID and PeriodID = @PeriodID AND Chassis = @Chassis";

       const string Query_GetDataByEngineNo = @" Select Engine From Sal_DispatchVehicleInformation
Where CompanyID = @CompanyID and PeriodID = @PeriodID AND Engine = @Engine";

       const string Query_VehicleReadyForDeliveryReport = @"
Select INV_StockTransactionHeader.DocumentNo, INV_StockTransactionHeader.DocumentDate,
SAL_DispatchVehicleInformation.VehicleID,

[INV_Item].[Description] As [Version] ,
SET_ItemCatalog.Description as Catalog,
SAL_DispatchVehicleInformation.Chassis, 
SAL_DispatchVehicleInformation.Engine, 
SAL_DispatchVehicleInformation.ProductionNo,
SAL_DispatchVehicleInformation.LotNo,
INV_StockTransactionHeader.Comments as Remarks,
Set_Color.ColorName

From INV_StockTransactionHeader

INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
	AND Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID
	AND Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID

INNER JOIN INV_StockTransactionDetail ON INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID
	AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID
	AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
	AND INV_StockTransactionDetail.Status = 1

INNER JOIN SAL_DispatchVehicleInformation ON SAL_DispatchVehicleInformation.StockHeaderID = INV_StockTransactionDetail.StockTransactionID
AND SAL_DispatchVehicleInformation.StockDetailID = INV_StockTransactionDetail.TransactionDetailID
	
INNER JOIN INV_Item ON INV_Item.ItemID = INV_StockTransactionDetail.ItemID
--AND INV_Item.CompanyID = INV_StockTransactionDetail.CompanyID

INNER JOIN SET_ItemCatalog ON SET_ItemCatalog.ItemCatalogID = INV_Item.ItemCatalogGroupID

INNER JOIN Set_Color ON Set_Color.ColorID = SAL_DispatchVehicleInformation.ColorID

INNER JOIN INV_Set_Category ON INV_Set_Category.CategoryID = INV_Item.ItemTypeID

	Where Set_DocumentType.DocumentType LIKE '%PRN%' 
	AND INV_Set_Category.CategoryID = @CategoryID
	AND INV_StockTransactionHeader.CompanyID = @CompanyID
	AND INV_StockTransactionHeader.PeriodID = @PeriodID
	--AND Cast(INV_StockTransactionHeader.DocumentDate as Date) = @Date
	AND Cast(INV_StockTransactionHeader.DocumentDate as Date) between @Date and @TODate

	Select tab.ItemID, tab.Description, SUM(tab.Today)Today, SUM(tab.DuringMonth)DuringMonth , SUM(tab.Cumulative)Cumulative From 
(
--Today
	Select [INV_Item].[ItemID],
	[INV_Item].[Description],
COUNT(SAL_DispatchVehicleInformation.VehicleID)Today, '0' as DuringMonth ,  '0' as Cumulative
From INV_StockTransactionHeader

INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
	AND Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID
	AND Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID

INNER JOIN INV_StockTransactionDetail ON INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID
	AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID
	AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
	AND INV_StockTransactionDetail.Status = 1

INNER JOIN SAL_DispatchVehicleInformation ON SAL_DispatchVehicleInformation.StockHeaderID = INV_StockTransactionDetail.StockTransactionID
AND SAL_DispatchVehicleInformation.StockDetailID = INV_StockTransactionDetail.TransactionDetailID
	
INNER JOIN INV_Item ON INV_Item.ItemID = INV_StockTransactionDetail.ItemID
--AND INV_Item.CompanyID = INV_StockTransactionDetail.CompanyID

INNER JOIN SET_ItemCatalog ON SET_ItemCatalog.ItemCatalogID = INV_Item.ItemCatalogGroupID

INNER JOIN Set_Color ON Set_Color.ColorID = SAL_DispatchVehicleInformation.ColorID

INNER JOIN INV_Set_Category ON INV_Set_Category.CategoryID = INV_Item.ItemTypeID

	Where Set_DocumentType.DocumentType LIKE '%PRN%' 
	AND INV_Set_Category.CategoryID = @CategoryID
	AND Cast(INV_StockTransactionHeader.DocumentDate as Date) = @Date
	AND INV_StockTransactionHeader.CompanyID = @CompanyID
	AND INV_StockTransactionHeader.PeriodID = @PeriodID
	Group By INV_Item.ItemID, INV_Item.Description
	
UNION ALL 
--Month
Select [INV_Item].[ItemID],
	[INV_Item].[Description],
'0' as Today, COUNT(SAL_DispatchVehicleInformation.VehicleID) DuringMonth, '0' as Cumulative
From INV_StockTransactionHeader

INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
	AND Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID
	AND Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID

INNER JOIN INV_StockTransactionDetail ON INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID
	AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID
	AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
	AND INV_StockTransactionDetail.Status = 1

INNER JOIN SAL_DispatchVehicleInformation ON SAL_DispatchVehicleInformation.StockHeaderID = INV_StockTransactionDetail.StockTransactionID
AND SAL_DispatchVehicleInformation.StockDetailID = INV_StockTransactionDetail.TransactionDetailID
	
INNER JOIN INV_Item ON INV_Item.ItemID = INV_StockTransactionDetail.ItemID
--AND INV_Item.CompanyID = INV_StockTransactionDetail.CompanyID

INNER JOIN SET_ItemCatalog ON SET_ItemCatalog.ItemCatalogID = INV_Item.ItemCatalogGroupID

INNER JOIN INV_Set_Category ON INV_Set_Category.CategoryID = INV_Item.ItemTypeID

INNER JOIN Set_Color ON Set_Color.ColorID = SAL_DispatchVehicleInformation.ColorID
	Where Set_DocumentType.DocumentType LIKE '%PRN%' 
	AND DATEPART(MONTH,INV_StockTransactionHeader.DocumentDate) = DATEPART(MONTH,@Date)
	AND INV_StockTransactionHeader.CompanyID = @CompanyID
	AND INV_StockTransactionHeader.PeriodID = @PeriodID
	AND INV_Set_Category.CategoryID = @CategoryID
	Group By INV_Item.ItemID, INV_Item.Description
	
	UNION ALL 
--Year
Select [INV_Item].[ItemID],
	[INV_Item].[Description],
'0' as Today, '0' as DuringMonth , COUNT(SAL_DispatchVehicleInformation.VehicleID)Cumulative
From INV_StockTransactionHeader

INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
	AND Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID
	AND Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID

INNER JOIN INV_StockTransactionDetail ON INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID
	AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID
	AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
	AND INV_StockTransactionDetail.Status = 1

INNER JOIN SAL_DispatchVehicleInformation ON SAL_DispatchVehicleInformation.StockHeaderID = INV_StockTransactionDetail.StockTransactionID
AND SAL_DispatchVehicleInformation.StockDetailID = INV_StockTransactionDetail.TransactionDetailID
	
INNER JOIN INV_Item ON INV_Item.ItemID = INV_StockTransactionDetail.ItemID
--AND INV_Item.CompanyID = INV_StockTransactionDetail.CompanyID

INNER JOIN SET_ItemCatalog ON SET_ItemCatalog.ItemCatalogID = INV_Item.ItemCatalogGroupID

INNER JOIN INV_Set_Category ON INV_Set_Category.CategoryID = INV_Item.ItemTypeID

INNER JOIN Set_Color ON Set_Color.ColorID = SAL_DispatchVehicleInformation.ColorID
	Where Set_DocumentType.DocumentType LIKE '%PRN%' 
--	AND DATEPART(YEAR,INV_StockTransactionHeader.DocumentDate) = DATEPART(YEAR,@Date)
	AND INV_StockTransactionHeader.CompanyID = @CompanyID
	AND INV_StockTransactionHeader.PeriodID = @PeriodID
	AND INV_Set_Category.CategoryID = @CategoryID
	Group By INV_Item.ItemID, INV_Item.Description	
	
	)tab
	Group By tab.ItemID, tab.Description";

       const string Query_VehicleReadyForDeliveryReportDated = @" 
Select INV_StockTransactionHeader.DocumentNo, INV_StockTransactionHeader.DocumentDate, SAL_DispatchVehicleInformation.VehicleID, 
    [INV_Item].[Description] As [Version], SET_ItemCatalog.Description as Catalog, SAL_DispatchVehicleInformation.Chassis, SAL_DispatchVehicleInformation.Engine, 
    SAL_DispatchVehicleInformation.ProductionNo, SAL_DispatchVehicleInformation.LotNo, INV_StockTransactionHeader.Comments as Remarks, Set_Color.ColorName 
From INV_StockTransactionHeader 
    INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
        AND Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID AND Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
    INNER JOIN INV_StockTransactionDetail ON INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID 
        AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 
        AND INV_StockTransactionDetail.Status = 1 
    INNER JOIN SAL_DispatchVehicleInformation ON SAL_DispatchVehicleInformation.StockHeaderID = INV_StockTransactionDetail.StockTransactionID 
        AND SAL_DispatchVehicleInformation.StockDetailID = INV_StockTransactionDetail.TransactionDetailID 
    LEFT JOIN Sal_DOVehicleInformation ON Sal_DOVehicleInformation.VehicleID = Sal_DispatchVehicleInformation.VehicleID 
        AND Sal_DispatchVehicleInformation.CompanyID = SAL_DOVehicleInformation.CompanyID 
        AND Sal_DispatchVehicleInformation.PeriodID = SAL_DOVehicleInformation.PeriodID 
    INNER JOIN INV_Item ON INV_Item.ItemID = INV_StockTransactionDetail.ItemID 
--AND INV_Item.CompanyID = INV_StockTransactionDetail.CompanyID 
    INNER JOIN SET_ItemCatalog ON SET_ItemCatalog.ItemCatalogID = INV_Item.ItemCatalogGroupID 
    INNER JOIN Set_Color ON Set_Color.ColorID = SAL_DispatchVehicleInformation.ColorID 
    INNER JOIN INV_Set_Category ON INV_Set_Category.CategoryID = INV_Item.ItemTypeID 
Where Set_DocumentType.DocumentType LIKE '%PRN%' AND INV_Set_Category.CategoryID = @CategoryID AND INV_StockTransactionHeader.CompanyID = @CompanyID 
    AND INV_StockTransactionHeader.PeriodID = @PeriodID AND ISNULL(SAl_DispatchVehicleInformation.VehicleID, 0) <> ISNULL(SAL_DOVehicleInformation.VehicleID, 0) 
    And ISNULL(SAl_DispatchVehicleInformation.VehicleInformationID, 0) = 0 AND ISNULL(SAl_DispatchVehicleInformation.DispatchID, 0) = 0 ";

       const string Query_GetDataForJobCardForm = @" 
Select SAL_DispatchHeader.DispatchNo, SAL_DispatchHeader.DispatchID, Set_DocumentType.DocumentType, SAL_DOVehicleInformation.DOHeaderID, 
    SAL_DOVehicleInformation.DODetailID, SAL_DOVehicleInformation.DOVehicleInformationID, INV_Item.Description as ItemName, INV_Item.CubicCentimeters as cc, 
    INV_Item.UnitWeight as GrossWeight, SAL_DOVehicleInformation.Engine, SAL_DOVehicleInformation.Chassis, SAL_DOVehicleInformation.SerialNo, 
    SAL_DispatchVehicleInformation.CreateDate as DispatchDate, SET_Color.ColorName, AR_Set_Customer.CustomerName, AR_Set_Customer.CustomerID, 
    AR_SET_Customer.AddressLine1, Ar_Set_Customer.ContactNumber 
From SAL_DispatchHeader 
    INNER JOIN Sal_DispatchVehicleInformation ON Sal_DispatchVehicleInformation.DispatchID = SAL_DispatchHeader.DispatchID 
        AND SAL_DispatchHeader.CompanyID = Sal_DispatchVehicleInformation.CompanyID AND SAL_DispatchHeader.PeriodID = Sal_DispatchVehicleInformation.PeriodID 
    INNER JOIN [SAL_DOVehicleInformation] ON [SAL_DOVehicleInformation].DOVehicleInformationID = Sal_DispatchVehicleInformation.VehicleInformationID 
        AND [SAL_DOVehicleInformation].CompanyID = Sal_DispatchVehicleInformation.CompanyID 
        AND [SAL_DOVehicleInformation].PeriodID = Sal_DispatchVehicleInformation.PeriodID 
    INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = SAL_DispatchHeader.DocumentTypeID AND Set_DocumentType.CompanyID = SAL_DispatchHeader.CompanyID 
        AND Set_DocumentType.PeriodID = SAL_DispatchHeader.PeriodID 
    LEFT JOIN SET_Color on [SAL_DOVehicleInformation].[ColorID]= [SET_Color].[ColorID] 
    INNER JOIN AR_Set_Customer On AR_Set_Customer.CustomerID = SAL_DispatchHeader.CustomerID 
--AND AR_Set_Customer.CompanyID = SAL_DispatchHeader.CompanyID 
        AND AR_Set_Customer.PeriodID = SAL_DispatchHeader.PeriodID 
    Inner Join INV_Item On 
--INV_Item.CompanyID = SAL_DOVehicleInformation.CompanyID And 
INV_Item.ItemID = SAL_DOVehicleInformation.ItemID 
WHERE Sal_DispatchVehicleInformation.CompanyID = @CompanyID AND Sal_DispatchVehicleInformation.PeriodID = @PeriodID 
    AND SAL_DOVehicleInformation.DOVehicleInformationID = @VehicleInformationID ";

       const string Query_ColorWiseUnAllocatedStock_Dashboard = @"select fintab.CategoryId,fintab.CategoryName,fintab.ItemID,fintab.Description, 
fintab.ColorID,fintab.ColorName,fintab.ColorCode
,COUNT(fintab.ItemId) as ColorCount
,ROW_NUMBER() OVER (ORDER BY fintab.ItemId Desc) AS 'Row'
from
(
SELECT 
INV_StockTransactionHeader.DocumentDate,
INV_SET_Category.CategoryID,
INV_SET_Category.CategoryName,
INV_Item.ItemID,
INV_Item.Description,'Yard' as status,
SAL_DispatchVehicleInformation.Chassis,
SAL_DispatchVehicleInformation.Engine,
SET_Color.ColorID,
SET_Color.ColorName,
SET_Color.ColorCode,
SAL_DispatchVehicleInformation.LotNo,
INV_StockTransactionDetail.Comments
,ROW_NUMBER() OVER (ORDER BY INV_StockTransactionHeader.StockTransactionID Desc) AS 'Row'
 from INV_StockTransactionHeader

 inner join SAL_DispatchVehicleInformation on INV_StockTransactionHeader.DocumentTypeID=SAL_DispatchVehicleInformation.DocumentTypeID
 
 and INV_StockTransactionHeader.DocumentNo=SAL_DispatchVehicleInformation.DocumentNo
  
inner join SET_DocumentType on SET_DocumentType.DocumentTypeID=SAL_DispatchVehicleInformation.DocumentTypeID

inner join  SET_Color on SET_Color.ColorID=SAL_DispatchVehicleInformation.ColorID

INNER JOIN INV_StockTransactionDetail ON SAL_DispatchVehicleInformation.StockHeaderID =  INV_StockTransactionDetail.StockTransactionID 

AND SAL_DispatchVehicleInformation.StockDetailID = INV_StockTransactionDetail.TransactionDetailID

INNER JOIN INV_Item ON INV_Item.ItemID = INV_StockTransactionDetail.ItemID

inner join INV_SET_Category on INV_SET_Category.CategoryID=INV_Item.ItemTypeID and INV_SET_Category.CompanyID = INV_Item.CompanyID

where SAL_DispatchVehicleInformation.CompanyID = @CompanyID AND
SAL_DispatchVehicleInformation.PeriodID = @PeriodID AND [INV_StockTransactionHeader].[DocumentDate]  between @FromDate and @ToDate
and SAL_DispatchVehicleInformation.VehicleInformationID is not null
)fintab
group by 
fintab.CategoryId,fintab.CategoryName,fintab.ItemID,fintab.Description,fintab.ColorID,fintab.ColorName,fintab.ColorCode
";


       #endregion

       #region Methods

       public static bool DeleteDataByDispatchID(SqlTransaction objTransaction, int companyID, int periodID, int dispatchID)
       {
           bool result = false;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@DispatchID", dispatchID);
               
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

       public static DataTable GetDataByDispatchID(int DOHID, int DODID, int dispatchID, int companyID, int periodID)
       {
           DataTable dtToReturn = null;
           
           try
           {
               StringBuilder sb = new StringBuilder();
               //AND Sal_DOVehicleInformation.DODetailID = @DODid
               SqlParameter[] sqlparams = new SqlParameter[5];
               sqlparams[0] = new SqlParameter("@DOHid", DOHID);
               sqlparams[1] = new SqlParameter("@DODid", DODID);
               sqlparams[2] = new SqlParameter("@CompanyID", companyID);
               sqlparams[3] = new SqlParameter("@PeriodID", periodID);
               sqlparams[4] = new SqlParameter("@DispatchID", dispatchID);

               if (DODID > 0)
                   sb.Append(" AND Sal_DOVehicleInformation.DODetailID = @DODid ");
               if (DOHID > 0)
                   sb.Append(" AND Sal_DOVehicleInformation.DODetailID = @DOHid ");

               DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDataByDispatchID, sb), sqlparams);

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

       public static DataTable GetVehicleForSalesInvoice(int companyID, int periodID,string Where = "")
       {
           DataTable dt = null;
           //StringBuilder sb = new StringBuilder();
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[2];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);

               dt = SqlHelper.ExecuteDataset(CommandType.Text,Query_GetVehicleForSalesInvoice+Where, sqlparams).Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }

       public static DataTable GetVehicleForGRN(int companyID, int periodID, int stockHeaderID)
       {
           DataTable dt = null;
           //StringBuilder sb = new StringBuilder();
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@StockHeaderID", stockHeaderID);

               dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetVehicleForGRN, sqlparams).Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }

       public static bool DeleteDataByStockHeaderID(SqlTransaction objTransaction, int companyID, int periodID, int stockHeaderID)
       {
           bool result = false;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@StockHeaderID", stockHeaderID);

               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByStockHeaderID, sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByStockHeaderID, sqlparams) > 0)
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

       public static DataTable GetVehicleForPRN(int companyID, int periodID, int stockHeaderID)
       {
           DataTable dt = null;
           //StringBuilder sb = new StringBuilder();
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@StockHeaderID", stockHeaderID);

               dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetVehicleForPRN, sqlparams).Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }

       public static bool DeleteDataByStockDetailID(SqlTransaction objTransaction, int companyID, int periodID, long stockDetailID)
       {
           bool result = false;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@StockDetailID", stockDetailID);

               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByStockDetailID, sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByStockDetailID, sqlparams) > 0)
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

       //Query_VehicleInformation

       public static DataTable GetVehicleInformation(Nullable<int> companyID, Nullable<int> periodID, Nullable < int > stHeaderID ,Nullable<int> stDetailID ,string filter = "")
       {
           DataTable dt = null;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[4];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@StockHeaderID", stHeaderID);
               sqlparams[3] = new SqlParameter("@StockDetailID", stDetailID);


               dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_VehicleInformation + " " + filter, sqlparams).Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }


       public static DataTable GetVehicleForLookupVehiclePrnGrn(Nullable<int> companyID, Nullable<int> periodID , string filter = "")
       {
           DataTable dt = null;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[2];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);

               dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetVehicleForLookupVehiclePrnGrn+ " "+filter, sqlparams).Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }

       public bool UpdateDataByVehicleID(SqlTransaction objTransaction = null)
       {
           bool result = false;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[13];
               sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
               sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);

               if (f_VehicleInformationID.HasValue)
                   sqlparams[2] = new SqlParameter("@VehicleInformationID", f_VehicleInformationID.Value);
               else
                   sqlparams[2] = new SqlParameter("@VehicleInformationID", DBNull.Value);
                
               sqlparams[3] = new SqlParameter("@VehicleID", f_VehicleID);

               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateDataByVehicleID, sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_UpdateDataByVehicleID, sqlparams) > 0)
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

       public bool UpdateDataByVehicleInformationID(SqlTransaction objTransaction = null)
       {
           bool result = false;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
               sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);

               if (f_VehicleInformationID.HasValue)
                   sqlparams[2] = new SqlParameter("@VehicleInformationID", f_VehicleInformationID.Value);
             

               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateDataByVehicleInformationID, sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_UpdateDataByVehicleInformationID, sqlparams) > 0)
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
       public static DataTable GetDataColorWiseUnAllocatedStockReport(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, SqlConnection objConnection = null, Nullable<int> categoryID = 0, Nullable<int> colorID = 0,int isallocated=0)
       {
           DataTable dtresult = null;
           try
           {
               DataSet ds = null;

               string AddtionalParameters = string.Empty;

               SqlParameter[] sqlParms = new SqlParameter[4];

               sqlParms[0] = new SqlParameter("@CompanyID", companyID);
               sqlParms[1] = new SqlParameter("@PeriodID", periodID);
               sqlParms[2] = new SqlParameter("@FromDate", DateFrom);
               sqlParms[3] = new SqlParameter("@ToDate", DateTo);
               //sqlParms[4] = new SqlParameter("@CategoryID", categoryID);

               if (categoryID > 0)
               {
                   AddtionalParameters = "AND [INV_SET_Category].[CategoryID] = " + categoryID.ToString();
               }
               if (colorID > 0)
               {
                   AddtionalParameters += " AND [SET_Color].[ColorID] = " + colorID.ToString();
               }
               if (objConnection == null)
               {
                   if (isallocated == 1)
                   {
                       ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_ColorWiseAllocatedStock + AddtionalParameters, sqlParms);
                   }
                   else
                       ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_ColorWiseUnAllocatedStock + AddtionalParameters, sqlParms);
               }
               else
               {
                   if (isallocated == 1)
                   {
                       ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_ColorWiseAllocatedStock + AddtionalParameters, sqlParms);
                   }
                   else

                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_ColorWiseUnAllocatedStock + AddtionalParameters, sqlParms);
               }

               if (ds.Tables.Count > 0)
                   return ds.Tables[0];

           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }
           return dtresult;
       }

       public static DataTable GetDispatchedVehicles(Nullable<int> companyID, Nullable<int> periodID, string filter = "")
       {
           DataTable dt = null;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
              
               dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDispatchedVehicles + " " + filter, sqlparams).Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }

       public static DataRow GetDataByEngineNo(int companyID, int periodID, string Engine, string where = "", SqlTransaction objTransaction = null)
       {
           DataRow dr = null;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@Engine", Engine);

               DataSet ds = null;

               if (objTransaction == null)
               {
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByEngineNo + where, sqlparams);
               }
               else
               {
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetDataByEngineNo + where, sqlparams);
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

       public static DataRow GetDataByChassisNo(int companyID, int periodID, string Chassis, string where = "", SqlTransaction objTransaction = null)
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
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByChassisNo + where, sqlparams);
               }
               else
               {
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetDataByChassisNo + where, sqlparams);
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

       public static DataSet GetVehicleReadyForReport(int companyID, int periodID, int categoryID, Nullable<DateTime> Date_Reporting = null, Nullable<DateTime> To_Date_Reporting = null, string type = "")
       {
           string filter = "";
           DataSet dsToReturn = null;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[6];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@Date", ((DateTime)Date_Reporting).ToString("MM-dd-yyyy"));
               sqlparams[3] = new SqlParameter("@TODate", ((DateTime)To_Date_Reporting).ToString("MM-dd-yyyy"));
               sqlparams[4] = new SqlParameter("@Type", type);
               sqlparams[5] = new SqlParameter("@CategoryID", categoryID);

               if (Date_Reporting != null)
               {
                   //filter = filter + " AND Cast(SAL_DeliveryOrderHeader.DODate As Date) ='" + ((DateTime)Date_Reporting).ToString("MM-dd-yyyy") + "'";
                   //string finalQuery = Query_VehicleReadyForDeliveryReport.Replace("{filter}", filter);
                   DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_VehicleReadyForDeliveryReport, sqlparams);

                   if (ds != null && ds.Tables.Count > 1)
                       dsToReturn = ds;
               }
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dsToReturn;
       }

       public static DataSet GetVehicleReadyForReportDated(int companyID, int periodID, int categoryID, Nullable<DateTime> Date_Reporting = null, Nullable<DateTime> Date_To = null, string type = "")
       {
           string filter = "";
           DataSet dsToReturn = null;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[6];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@Date", ((DateTime)Date_Reporting).ToString("MM-dd-yyyy"));
               sqlparams[3] = new SqlParameter("@ToDate", ((DateTime)Date_To).ToString("MM-dd-yyyy"));
               sqlparams[4] = new SqlParameter("@Type", type);
               sqlparams[5] = new SqlParameter("@CategoryID", categoryID);

               if (Date_Reporting != null)
               {
                   filter = " AND Cast(INV_StockTransactionHeader.DocumentDate as Date) between @Date AND @ToDate";
               }
                   DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_VehicleReadyForDeliveryReportDated + filter, sqlparams);

                   if (ds != null && ds.Tables[0].Rows.Count > 1)
                       dsToReturn = ds;
               
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dsToReturn;
       }

       public static DataRow GetDataForJobCardForm(int companyID, int periodID, short vehicleInformationID)
       {
           DataRow dr = null;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@VehicleInformationID", vehicleInformationID);

               DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataForJobCardForm, sqlparams);

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

       public static DataTable GetDataColorWiseUnAllocatedStockDashboard(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null, SqlTransaction objTransaction = null)
       {
           DataTable dtresult = null;
           try
           {

               string AddtionalParameters = string.Empty;

               //SqlParameter[] sqlParms = new SqlParameter[4];
               //sqlParms[0] = new SqlParameter("@CompanyID", companyID);
               //sqlParms[1] = new SqlParameter("@PeriodID", periodID);
               //sqlParms[2] = new SqlParameter("@FromDate", DateFrom);
               //sqlParms[3] = new SqlParameter("@ToDate", DateTo);
               //sqlParms[4] = new SqlParameter("@CategoryID", categoryID);

               List<SqlParameter> sqlparamsList = new List<SqlParameter>();
               sqlparamsList.Add(new SqlParameter("@PageIndex", PageIndex));
               sqlparamsList.Add(new SqlParameter("@PageSize", PageSize));
               sqlparamsList.Add(new SqlParameter("@TotalRows", ReturnTotalRows));
               sqlparamsList.Add(new SqlParameter("@CompanyID", companyID));
               sqlparamsList.Add(new SqlParameter("@PeriodID", periodID));
               sqlparamsList.Add(new SqlParameter("@FromDate", DateFrom));
               sqlparamsList.Add(new SqlParameter("@ToDate", DateTo));

               SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
               sqlparamTotalRows.Direction = ParameterDirection.Output;
               SqlParameter[] sqlparams = sqlparamsList.ToArray();


               if (objTransaction == null)
                   dtresult = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_ColorWiseUnAllocatedStock_Dashboard + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_ColorWiseUnAllocatedStock_Dashboard + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
               else
                   dtresult = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_ColorWiseUnAllocatedStock_Dashboard + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_ColorWiseUnAllocatedStock_Dashboard + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

               ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());

           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }
           return dtresult;
       }
       #endregion
   }
}
