using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;
using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.FA.Transaction
{
    public partial class FA_AssetDepreciation
    {
        #region Constant Strings

        const string Query_GetDataForDepreciation = @" 
SELECT 
FA_AssetIdentity.AssetIdentityID,

FA_AssetIdentity.SalvageValue, FA_AssetIdentity.PurchaseDate, Isnull(FA_AssetIdentity.WDV, 0) WDV, 
    FA_AssetIdentity.CurrentValue, FA_AssetIdentity.LastDepreciationDate, FA_AssetIdentity.AssetTypeID, FA_AssetIdentity.CostCenterID, FA_AssetIdentity.DepartmentID, 
    FA_AssetIdentity.LastDepreciationDate, FA_AssetIdentity.CustodianID, FA_AssetIdentity.LocationID, FA_AssetTypePolicy.DepreciationMethodCode, 
    FA_AssetTypePolicy.PeriodStatus, FA_AssetTypePolicy.AssetClassification, Isnull(FA_AssetTypePolicy.DepreciationRate, 0) DepreciationRate, 
    Case When FA_AssetTypePolicy.DepreciationMethodCode = 1 Then 'DecliningBalance' Else 'StraighLine' END DepreciationMethod, 
	Case FA_AssetTypePolicy.PeriodStatus WHEN 1 Then 'Day' WHEN 2 Then 'Month' WHEN 3 Then 'Quarter' Else 'Year' End DepreciationPeriod, 
    FA_Set_AssetLife.IntervalValue, FA_Set_AssetLife.PeriodType As IntervalType 
FROM FA_AssetIdentity 
	INNER JOIN FA_AssetTypePolicy ON FA_AssetTypePolicy.CompanyID = FA_AssetIdentity.CompanyID 

--		AND FA_AssetTypePolicy.AssetTypePolicyID = FA_AssetIdentity.AssetTypePolicyID 

AND FA_AssetTypePolicy.IsActive = 1 AND FA_AssetTypePolicy.Status = 1 
	Inner Join FA_Set_AssetLife On FA_Set_AssetLife.CompanyID = FA_AssetTypePolicy.CompanyID 
		And FA_Set_AssetLife.AssetLifeID = FA_AssetTypePolicy.AssetLifeID And FA_Set_AssetLife.Status = 1 
Where FA_AssetIdentity.CompanyID = @CompanyID And (FA_AssetIdentity.AssetTypeID = @AssetTypeID Or @AssetTypeID IS NULL) 
	And (FA_AssetTypePolicy.AssetClassification = @PolicyType Or @PolicyType IS NULL) And (FA_AssetTypePolicy.PeriodStatus = @PeriodType Or @PeriodType IS NULL) 
    And IsNull(FA_AssetIdentity.IsDisposed, 0) = 0 AND FA_AssetIdentity.Status = 1 ";


        const string Query_GetDataForDepreciationAssetInformation = @" 
select 
    
    FA_AssetInformation.AssetInfoID as AssetIdentityID,
			FA_AssetInformation.ItemID,
    FA_AssetInformation.SalvageValue, FA_AssetInformation.PurchaseDate, 
    Isnull(FA_AssetInformation.WrittenDownValue, 0) WDV, 
    FA_AssetInformation.CurrentValue, 
    FA_AssetInformation.StockGRNDate, 
    FA_AssetInformation.LastDepreciationDate as LastDepreciationDate, 
    FA_AssetInformation.AssetTypeID, 
    FA_AssetInformation.CostCenterID, 
    FA_AssetInformation.DepartmentID, 
    FA_AssetInformation.CustodianID, 
    FA_AssetInformation.LocationID, 
    FA_AssetInformation.DepreciationMethodCode, 
    FA_AssetTypePolicy.PeriodStatus, 
    FA_AssetTypePolicy.AssetClassification, 
    Isnull(FA_AssetTypePolicy.DepreciationRate, 0) DepreciationRate, 
    Case When FA_AssetTypePolicy.DepreciationMethodCode = 1 Then 'DecliningBalance' Else 'StraighLine' END DepreciationMethod, 
	Case FA_AssetTypePolicy.PeriodStatus WHEN 1 Then 'Day' WHEN 2 Then 'Month' WHEN 3 Then 'Quarter' Else 'Year' End DepreciationPeriod, 
	
    FA_Set_AssetLife.IntervalValue, FA_Set_AssetLife.PeriodType As IntervalType ,
    
    
    Case When FA_AssetInformation.DepreciationMethodCode = 1 Then 'DecliningBalance' Else 'StraighLine' END DepreciationMethod,
     isnull(datepart(mm,lastdepreciationdate),datepart(mm,StockGRNDate)) CheckDate,
    isnull(lastdepreciationdate,StockGRNDate) as CheckDateFull,
    isnull(datepart(yyyy,lastdepreciationdate),datepart(yyyy,StockGRNDate)) CheckDateYear,

    FA_AssetInformation.isdisposed, FA_AssetInformation.opening, FA_AssetInformation.TagPrefix,FA_AssetInformation.TagNumber
     from FA_AssetInformation

     left join FA_AssetTypePolicy on FA_AssetTypePolicy.DepreciationMethodCode = FA_AssetInformation.DepreciationMethodCode
     and FA_AssetTypePolicy.AssetTypeID = FA_AssetInformation.AssetTypeID AND FA_AssetTypePolicy.Status = 1
     left join FA_Set_AssetLife on FA_Set_AssetLife.AssetLifeID =   FA_AssetInformation.ExpectedLifeID AND FA_Set_AssetLife.Status = 1

     where FA_AssetInformation.CompanyID = @CompanyID and FA_AssetInformation.AssetTypeID = @AssetTypeID and FA_AssetInformation.status =1
and FA_AssetInformation.isdisposed is null and (FA_AssetInformation.ItemID= @itemId or @itemId is null)
and (FA_AssetTypePolicy.PeriodStatus = @PeriodType or @PeriodType is null) AND FA_AssetInformation.Status = 1
    {0}

    ";

        const string Query_GetDataDepreciationAssetInformationForPeriodType = @"
select distinct
    FA_AssetTypePolicy.PeriodStatus, 
    Case When FA_AssetTypePolicy.DepreciationMethodCode = 1 
	Then 'DecliningBalance' Else 'StraighLine' END DepreciationMethod, 
	Case FA_AssetTypePolicy.PeriodStatus 
	WHEN 1 Then 'Day' 
	WHEN 2 Then 'Month' 
	WHEN 3 Then 'Quarter' Else 'Year' End DepreciationPeriod
     from FA_AssetInformation

     left join FA_AssetTypePolicy on FA_AssetTypePolicy.DepreciationMethodCode = FA_AssetInformation.DepreciationMethodCode
     and FA_AssetTypePolicy.AssetTypeID = FA_AssetInformation.AssetTypeID AND FA_AssetTypePolicy.Status = 1
     left join FA_Set_AssetLife on FA_Set_AssetLife.AssetLifeID =   FA_AssetInformation.ExpectedLifeID AND FA_Set_AssetLife.Status = 1

   where FA_AssetInformation.CompanyID = @CompanyID and FA_AssetInformation.AssetTypeID = @AssetTypeID and FA_AssetInformation.status = 1
    and FA_AssetInformation.isdisposed is null and (FA_AssetInformation.ItemID= @itemId or @itemId is null)

    {0}
";

        const string Query_DisposalReport = @" 
 select SUM(DisposalAmount)as DisposalAmount,Comments,DisposalDate,GL_TransactionHeader.DocumentNo as VoucherNo
 from FA_Disposal
 inner join GL_TransactionHeader on GL_TransactionHeader.DocumentNo = FA_Disposal.TransactionID
and GL_TransactionHeader.CompanyID = FA_Disposal.CompanyID
and GL_TransactionHeader.Status = 1
where FA_Disposal.CompanyID = @CompanyID 
and FA_Disposal.Status = 1
{0}
group by GL_TransactionHeader.DocumentNo,DisposalDate,Comments

";
////////////////        const string Query_DepreciationReport = @"  select FA_AssetIdentity.TagNo AS AssetCode, 
////////////////  FA_AssetIdentity.ItemName AS Description, 
////////////////  IsNull(FA_AssetDepreciation.CurrentValue, 0) + IsNull(FA_AssetDepreciation.DepreciatedValue, 0) as OPCost,
////////////////  FA_AssetDepreciation.DepreciationRate, 
////////////////  FA_AssetDepreciation.DepreciatedValue as DeprAmount,
////////////////  INV_SET_Category.CategoryName 
////////////////  from FA_AssetDepreciation
////////////////  inner join FA_AssetIdentity on FA_AssetIdentity.AssetIdentityID = FA_AssetDepreciation.AssetIdentityID
////////////////  inner join Set_Period ON Set_Period.PeriodID = FA_AssetDepreciation.PeriodID
////////////////  inner join INV_Item on INV_Item.ItemID = FA_AssetIdentity.ItemID
////////////////  inner join INV_SET_Category on INV_SET_Category.CategoryID = INV_Item.ItemTypeID 
////////////////  where FA_AssetDepreciation.CompanyID = @CompanyID
////////////////  AND FA_AssetDepreciation.PeriodID = @PeriodID ";
        const string Query_DepreciationReport = @"
SELECT * FROM (

select DISTINCT  (FA_AssetInformation.TagPrefix) +'-' + FA_AssetInformation.TagNumber + '' AS Tag, 
  'Opening' AS Description, 
  FA_AssetInformation.Rate as OPCost,
  0 AS DepreciationRate, 
  FA_AssetInformation.OpeningDepreciation as DeprAmount,
  INV_SET_Category.CategoryName,
 INV_Item.Description AS ITEM,
 (SELECT TOP 1 DATEADD(MONTH, -1,FA_DepreciationProcess.DepreciatedUpto) FROM FA_DepreciationProcess WHERE FA_DepreciationProcess.AssetIdentityID = FA_AssetInformation.AssetInfoID AND FA_DepreciationProcess.Status = 1) AS DepreciatedUpto,
  Set_Location.LocationName,
  SET_CostCenter.CostCenterName,
  (SELECT TOP 1 DATEADD(MONTH, -1,FA_DepreciationProcess.FAUpTo) FROM FA_DepreciationProcess WHERE FA_DepreciationProcess.AssetIdentityID = FA_AssetInformation.AssetInfoID AND FA_DepreciationProcess.Status = 1 ) AS FAUpTo,
  0 AS GroupNo
  from FA_AssetInformation
  inner join FA_SET_AssetType ON FA_SET_AssetType.AssetTypeID = FA_AssetInformation.AssetTypeID AND FA_SET_AssetType.Status = 1
  inner join INV_Item on INV_Item.ItemID = FA_AssetInformation.ItemID AND INV_Item.Status = 1
  inner join INV_SET_Category on INV_SET_Category.CategoryID = INV_Item.ItemTypeID AND INV_SET_Category.Status = 1
   LEFT JOIN Set_Location ON Set_Location.LocationID = isnull(FA_AssetInformation.LocationID,FA_AssetInformation.SubLocation) AND Set_Location.Status = 1
  INNER JOIN SET_CostCenter ON SET_CostCenter.CostCenterID = FA_AssetInformation.CostCenterID AND SET_CostCenter.Status = 1
  where FA_AssetInformation.CompanyID = @CompanyID AND FA_AssetInformation.Status = 1 AND FA_AssetInformation.AssetInfoID IN (SELECT DISTINCT AssetIdentityID FROM FA_DepreciationProcess WHERE Status = 1) AND FA_AssetInformation.OpeningDepreciation IS NOT NULL
  {0}

)DT

UNION ALL

SELECT Tag, Description, OPCost, DepreciationRate, DeprAmount, CategoryName, ITEM, DepreciatedUpto, LocationName, CostCenterName, FAUpTo, GroupNo
FROM (

select (FA_AssetInformation.TagPrefix) +'-' + FA_AssetInformation.TagNumber + '' AS Tag, 
  FA_SET_AssetType.AssetTypeName AS Description, 
  IsNull(FA_DepreciationProcess.NWDV, 0) + IsNull(FA_DepreciationProcess.DepreciationAmount, 0) as OPCost,
  FA_DepreciationProcess.DepreciationRate, 
  FA_DepreciationProcess.DepreciationAmount as DeprAmount,
  FA_AssetInformation.OpeningDepreciation,
  INV_SET_Category.CategoryName,
 INV_Item.Description AS ITEM,
  FA_DepreciationProcess.DepreciatedUpto ,
  Set_Location.LocationName,
  SET_CostCenter.CostCenterName,
  FA_DepreciationProcess.FAUpTo,
  ROW_NUMBER() OVER (PARTITION BY FA_AssetInformation.TagPrefix, FA_AssetInformation.TagNumber, FA_SET_AssetType.AssetTypeName ORDER BY FA_AssetInformation.TagPrefix, FA_AssetInformation.TagNumber, FA_SET_AssetType.AssetTypeName ) AS GroupNo
  from FA_DepreciationProcess
 inner join FA_AssetInformation on FA_AssetInformation.AssetInfoID= FA_DepreciationProcess.AssetIdentityID AND FA_AssetInformation.Status = 1
  inner join FA_SET_AssetType ON FA_SET_AssetType.AssetTypeID = FA_AssetInformation.AssetTypeID AND FA_SET_AssetType.Status = 1
  inner join INV_Item on INV_Item.ItemID = FA_AssetInformation.ItemID AND INV_Item.Status = 1
  inner join INV_SET_Category on INV_SET_Category.CategoryID = INV_Item.ItemTypeID AND INV_SET_Category.Status = 1
   LEFT JOIN Set_Location ON Set_Location.LocationID = isnull(FA_AssetInformation.LocationID,FA_AssetInformation.SubLocation) AND Set_Location.Status = 1
  INNER JOIN SET_CostCenter ON SET_CostCenter.CostCenterID = FA_AssetInformation.CostCenterID AND SET_CostCenter.Status = 1
  where FA_DepreciationProcess.CompanyID = @CompanyID AND FA_DepreciationProcess.Status = 1
  {0}
  {1}
)DT
ORDER BY Tag, FAUpTo";

        const string Query_FAAssetCostReport = @"
Select FA_AssetCostHeader.CostOfAsset,FA_AssetCostHeader.Comments,FA_AssetCostHeader.CostingDate,
AR_SET_Customer.CustomerName,INV_Item.ItemID,INV_Item.Description as ItemName,FA_AssetCostDetail.Amount,AR_SET_Customer.CustomerID
,FA_Set_Cost.CostName as DetailCostName,FA_AssetCostHeader.DocumentReference,FA_AssetCostHeader.AssetCostID
 from FA_AssetCostHeader 
 
inner join FA_AssetCostDetail on FA_AssetCostDetail.AssetCostHeaderID = FA_AssetCostHeader.AssetCostID
And  FA_AssetCostDetail.CompanyID = FA_AssetCostHeader.CompanyID
And  FA_AssetCostDetail.PeriodID = FA_AssetCostHeader.periodID
And  FA_AssetCostDetail.Status = 1

inner join AR_SET_Customer on AR_SET_Customer.CustomerID = FA_AssetCostHeader.VendorId
--and AR_SET_Customer.CompanyID = FA_AssetCostHeader.CompanyId
and AR_SET_Customer.PeriodID = FA_AssetCostHeader.PeriodId
and AR_SET_Customer.Status = 1

left join [FA_AssetTransaction]  FA_AT
on FA_AT.AssetTransactionID = FA_AssetCostHeader.AssetTransactionID
and FA_AT.CompanyID = FA_AssetCostHeader.CompanyID
and FA_AT.Status = 1

left join INV_Item 
  ON INV_Item.ItemID=FA_AT.ItemID


inner join FA_Set_Cost  on FA_Set_Cost.CostID = FA_AssetCostDetail.CostID
and FA_Set_Cost.Status = 1

where FA_AssetCostHeader.Status = 1
and FA_AssetCostHeader.CompanyID = @CompanyID
and FA_AssetCostHeader.PeriodID = @PeriodID
and FA_AssetCostHeader.CostingDate between @FromDate and @ToDate
{0}

";
        const string Query_FAAssetCostSummaryReport = @"
Select Sum(FA_AssetCostHeader.CostOfAsset) as CostOfAsset,FA_AssetCostHeader.Comments,FA_AssetCostHeader.CostingDate,
AR_SET_Customer.CustomerName,INV_Item.ItemID,INV_Item.Description as ItemName,FA_AssetCostDetail.Amount,AR_SET_Customer.CustomerID,
FA_Set_Cost.CostName as DetailCostName,FA_AssetCostHeader.DocumentReference,FA_AssetCostHeader.AssetCostID

from FA_AssetCostHeader 
 
inner join FA_AssetCostDetail on FA_AssetCostDetail.AssetCostHeaderID = FA_AssetCostHeader.AssetCostID
And  FA_AssetCostDetail.CompanyID = FA_AssetCostHeader.CompanyID
And  FA_AssetCostDetail.PeriodID = FA_AssetCostHeader.periodID
And  FA_AssetCostDetail.Status = 1

inner join AR_SET_Customer on AR_SET_Customer.CustomerID = FA_AssetCostHeader.VendorId
--and AR_SET_Customer.CompanyID = FA_AssetCostHeader.CompanyId
and AR_SET_Customer.PeriodID = FA_AssetCostHeader.PeriodId
and AR_SET_Customer.Status = 1

left join [FA_AssetTransaction]  FA_AT
on FA_AT.AssetTransactionID = FA_AssetCostHeader.AssetTransactionID
and FA_AT.CompanyID = FA_AssetCostHeader.CompanyID
and FA_AT.Status = 1

left join INV_Item 
  ON INV_Item.ItemID=FA_AT.ItemID

inner join FA_Set_Cost  on FA_Set_Cost.CostID = FA_AssetCostDetail.CostID
and FA_Set_Cost.Status = 1

where FA_AssetCostHeader.Status = 1
and FA_AssetCostHeader.CompanyID = @CompanyID
and FA_AssetCostHeader.PeriodID = @PeriodID
and FA_AssetCostHeader.CostingDate between @FromDate and @ToDate 
{0}
group by AR_SET_Customer.CustomerID,FA_AssetCostHeader.CostingDate,
AR_SET_Customer.CustomerName,FA_AssetCostDetail.Amount,INV_Item.ItemID, INV_Item.Description,
FA_Set_Cost.CostName,FA_AssetCostHeader.DocumentReference,FA_AssetCostHeader.AssetCostID
,FA_AssetCostHeader.Comments
";

        const string Query_FARegisterReport = @"
SELECT  
FA_SET_AssetType.AssetTypeName
,CONVERt(Date,@FromDate)as fromdate
,CONVERt(Date,@ToDate) as ToDate
,InventoryItemStatusCode AS InventoryItemStatusCode
,Isnull(FA_AssetInformation.TagPrefix,'') + ' ' + FA_AssetInformation.TagNumber  as TagNumber
,INV_Item.Description as ItemName
,FA_AssetInformation.Rate as AsAtCost
,FA_AssetInformation.StockGRNDate as GRNDate
,FA_AssetInformation.DepreciationRate 
,0 as Addition
,CONVERT(int, ISNULL(FA_AssetInformation.isdisposed,0)) AS IsDisposed
,CONVERT(varchar,FA_Set_AssetLife.IntervalValue) + '-' + CONVERT(varchar,FA_Set_AssetLife.PeriodType) as PeriodName

,isnull((Select  SUM(FAI.OpeningDepreciation)
	 from FA_AssetInformation FAI 
	 where FAI.Status = 1 AND FAI.TagPrefix=FA_AssetInformation.TagPrefix and FAI.TagNumber=FA_AssetInformation.TagNumber 	 
	),0) +
isnull((Select  SUM(FADP.DepreciationAmount)
	 from FA_DepreciationProcess FADP
	 INNER JOIN FA_AssetInformation FAI ON FADP.AssetIdentityID = FAI.AssetInfoID
	 and FADP.CompanyID= FAI.CompanyID AND FADP.Status = 1 AND FAI.Status = 1
	 where  FAI.TagPrefix=FA_AssetInformation.TagPrefix and FAI.TagNumber=FA_AssetInformation.TagNumber AND ( CAST( FADP.DepreciatedFrom AS Date) >= SET_Period.StartDate ) and  CAST( FADP.DepreciatedFrom AS Date) < @FromDate 
	),0) AS AsAtDepreciation
	  
,isnull((Select  SUM(FADP.DepreciationAmount)
	 from FA_DepreciationProcess FADP
	 INNER JOIN FA_AssetInformation FAI ON FADP.AssetIdentityID = FAI.AssetInfoID
	 and FADP.CompanyID= FAI.CompanyID AND FADP.Status = 1 AND FAI.Status = 1
	 where  FAI.TagPrefix=FA_AssetInformation.TagPrefix and FAI.TagNumber=FA_AssetInformation.TagNumber AND (FADP.DepreciatedFrom between @FromDate AND @ToDate  Or FADP.DepreciatedUpTo  between @FromDate AND @ToDate)
	),0) as  DepreciationAmount
,ISNULL(FA_AssetInformation.LocationID, 0) AS LocationID
,ISNULL(SET_Location.LocationName, 'NA') AS LocationName
FROM FA_AssetInformation
INNER JOIN INV_Item ON INV_Item.ItemID = FA_AssetInformation.ItemID AND INV_Item.Status = 1
INNER JOIN FA_SET_AssetType ON FA_SET_AssetType.AssetTypeID = FA_AssetInformation.AssetTypeID --AND FA_SET_AssetType.CompanyID = FA_AssetInformation.CompanyID 
AND FA_SET_AssetType.Status = 1
INNER JOIn SET_Company ON FA_AssetInformation.CompanyID=SET_Company.CompanyId
AND SET_Company.Status=1 
INNER JOIN SET_CompanyPeriod ON SET_CompanyPeriod.CompanyID=SET_Company.CompanyId
AND SET_CompanyPeriod.status=1
INNER JOIN SET_Period on SET_Period.PeriodID = SET_CompanyPeriod.PeriodID
INNER JOIN FA_Set_AssetLife ON FA_Set_AssetLife.AssetLifeID = FA_AssetInformation.ExpectedLifeID AND FA_Set_AssetLife.Status = 1 --AND FA_Set_AssetLife.CompanyID = FA_AssetInformation.CompanyID
LEFT JOIN SET_Location ON SET_Location.LocationID = FA_AssetInformation.LocationID AND SET_Location.Status = 1
WHERE FA_AssetInformation.Status = 1 AND FA_AssetInformation.CompanyID = @CompanyID
{0}
ORDER BY FA_SET_AssetType.AssetTypeName, TagNumber
";

        const string Query_FARegisterReport_RSM = @"
SELECT  
FA_SET_AssetType.AssetTypeName
,CONVERt(Date,@FromDate)as fromdate
,CONVERt(Date,@ToDate) as ToDate
,InventoryItemStatusCode AS InventoryItemStatusCode
,Isnull(FA_AssetInformation.TagPrefix,'') + ' ' + FA_AssetInformation.TagNumber  as TagNumber
,INV_Item.Description as ItemName
,INV_Item.ItemID as ItemID
,FA_AssetInformation.Rate as AsAtCost
,FA_AssetInformation.StockGRNDate as GRNDate
,FA_AssetInformation.DepreciationRate 
,0 as Addition
,CONVERT(int, ISNULL(FA_AssetInformation.isdisposed,0)) AS IsDisposed
,CONVERT(varchar,FA_Set_AssetLife.IntervalValue) + '-' + CONVERT(varchar,FA_Set_AssetLife.PeriodType) as PeriodName

,isnull((Select  SUM(FAI.OpeningDepreciation)
	 from FA_AssetInformation FAI 
	 where FAI.Status = 1 AND FAI.TagPrefix=FA_AssetInformation.TagPrefix and FAI.TagNumber=FA_AssetInformation.TagNumber 	 
	),0) +
isnull((Select  SUM(FADP.DepreciationAmount)
	 from FA_DepreciationProcess FADP
	 INNER JOIN FA_AssetInformation FAI ON FADP.AssetIdentityID = FAI.AssetInfoID
	 and FADP.CompanyID= FAI.CompanyID AND FADP.Status = 1 AND FAI.Status = 1
	 where  FAI.TagPrefix=FA_AssetInformation.TagPrefix and FAI.TagNumber=FA_AssetInformation.TagNumber AND ( CAST( FADP.DepreciatedFrom AS Date) >= SET_Period.StartDate ) and  CAST( FADP.DepreciatedFrom AS Date) < @FromDate 
	),0) AS AsAtDepreciation
	  
,isnull((Select  SUM(FADP.DepreciationAmount)
	 from FA_DepreciationProcess FADP
	 INNER JOIN FA_AssetInformation FAI ON FADP.AssetIdentityID = FAI.AssetInfoID
	 and FADP.CompanyID= FAI.CompanyID AND FADP.Status = 1 AND FAI.Status = 1
	 where  FAI.TagPrefix=FA_AssetInformation.TagPrefix and FAI.TagNumber=FA_AssetInformation.TagNumber AND (FADP.DepreciatedFrom between @FromDate AND @ToDate  Or FADP.DepreciatedUpTo  between @FromDate AND @ToDate)
	),0) as  DepreciationAmount

FROM FA_AssetInformation
INNER JOIN INV_Item ON INV_Item.ItemID = FA_AssetInformation.ItemID AND INV_Item.Status = 1
INNER JOIN FA_SET_AssetType ON FA_SET_AssetType.AssetTypeID = FA_AssetInformation.AssetTypeID --AND FA_SET_AssetType.CompanyID = FA_AssetInformation.CompanyID 
AND FA_SET_AssetType.Status = 1
INNER JOIn SET_Company ON FA_AssetInformation.CompanyID=SET_Company.CompanyId
AND SET_Company.Status=1 
INNER JOIN SET_CompanyPeriod ON SET_CompanyPeriod.CompanyID=SET_Company.CompanyId
AND SET_CompanyPeriod.status=1
INNER JOIN SET_Period on SET_Period.PeriodID = SET_CompanyPeriod.PeriodID
INNER JOIN FA_Set_AssetLife ON FA_Set_AssetLife.AssetLifeID = FA_AssetInformation.ExpectedLifeID AND FA_Set_AssetLife.Status = 1 --AND FA_Set_AssetLife.CompanyID = FA_AssetInformation.CompanyID
WHERE FA_AssetInformation.Status = 1 AND FA_AssetInformation.CompanyID = @CompanyID
{0}
ORDER BY FA_SET_AssetType.AssetTypeName, TagNumber
";

        const string Query_IdleAssetsReport = @"select  INV_Item.Description, NoOfUnits ,SUM(Rate) as Rate   from FA_AssetInformation 
inner join INV_Item on INV_Item.ItemID = FA_AssetInformation.ItemID
      and INV_Item.Status = 1
where FA_AssetInformation.TransactionStatus = 1 
     and FA_AssetInformation.CompanyID = @CompanyID and FA_AssetInformation.Status = 1
	 {0}

group by INV_Item.Description, NoOfUnits 

order by INV_Item.Description";

        const string Query_DetailOfLandReport = @"
select
ROW_NUMBER() Over(Order by FA_AssetInformation.ItemID asc) as SrNo,
Acre, (Rate / isnull(Acre,1)) as RatePerAcre, Rate as Amount , SET_Location.LocationName  from FA_AssetInformation
left join INV_Item on INV_Item.ItemID = FA_AssetInformation.ItemID
left join SET_Location on SET_Location.LocationID = FA_AssetInformation.LocationID
inner join FA_SET_AssetType on FA_SET_AssetType.AssetTypeID = FA_AssetInformation.AssetTypeID
where FA_AssetInformation.CompanyID = @CompanyID and FA_AssetInformation.Status = 1
and FA_SET_AssetType.AssetTypeName like 'Land'
{0}";

        const string Query_DetailOfInvestmentProperty = @"
select 
ROW_NUMBER() Over(Order by FA_AssetInformation.ItemID asc) as SrNo,
Acre, (Rate / isnull(Acre,1)) as RatePerAcre, Rate as Amount 
, (WrittenDownValue / isnull(Acre,1)) as RatePerAcreAMV, WrittenDownValue as AmountAMV 
, SET_Location.LocationName  from FA_AssetInformation 
left join INV_Item on INV_Item.ItemID = FA_AssetInformation.ItemID
left join SET_Location on SET_Location.LocationID = FA_AssetInformation.LocationID
inner join FA_SET_AssetType on FA_SET_AssetType.AssetTypeID = FA_AssetInformation.AssetTypeID
where FA_AssetInformation.CompanyID = @CompanyID and FA_AssetInformation.Status = 1
and FA_SET_AssetType.AssetTypeName like 'Land'
{0}";

        const string Query_AdvancesForPurchaseOfLandInvestmentProperty = @"select 
ROW_NUMBER() Over(Order by FA_AssetInformation.ItemID asc) as SrNo
,AR_SET_Customer.CustomerName
,Acre
,(Rate / isnull(Acre,1)) as RatePerAcre
,Rate as Amount 
,SET_Location.LocationName  from FA_AssetInformation 
left join INV_Item on INV_Item.ItemID = FA_AssetInformation.ItemID
left join SET_Location on SET_Location.LocationID = FA_AssetInformation.LocationID
inner join FA_SET_AssetType on FA_SET_AssetType.AssetTypeID = FA_AssetInformation.AssetTypeID
left join AR_SET_Customer on AR_SET_Customer.CustomerID = FA_AssetInformation.VendorID
where FA_AssetInformation.CompanyID = @CompanyID and FA_AssetInformation.Status = 1
and FA_SET_AssetType.AssetTypeName like 'Land'
{0}";

        const string Query_FreeholdLandReport = @"select 
ROW_NUMBER() Over(Order by FA_AssetInformation.ItemID asc) as SrNo,
FA_AssetInformation.PurchaseDate, 
Acre, Rate as Amount 
, SET_Location.LocationName  from FA_AssetInformation 
left join INV_Item on INV_Item.ItemID = FA_AssetInformation.ItemID
left join SET_Location on SET_Location.LocationID = FA_AssetInformation.LocationID
inner join FA_SET_AssetType on FA_SET_AssetType.AssetTypeID = FA_AssetInformation.AssetTypeID
where FA_AssetInformation.CompanyID = @CompanyID and FA_AssetInformation.Status = 1
and FA_SET_AssetType.AssetTypeName like 'Land'
{0}";
        const string Query_SummaryOfLandReport = @"select 
SET_OperatingUnit.OperatingUnitName
,SUM(Acre) as Acre
,Sum(Rate) as Amount 
,SUM(WrittenDownValue) as WDV
from FA_AssetInformation 
left join INV_Item on INV_Item.ItemID = FA_AssetInformation.ItemID
left join SET_Location on SET_Location.LocationID = FA_AssetInformation.LocationID
inner join FA_SET_AssetType on FA_SET_AssetType.AssetTypeID = FA_AssetInformation.AssetTypeID
left join SET_Company on SET_Company.CompanyId = FA_AssetInformation.CompanyID
left join SET_OperatingUnit on SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId
where FA_AssetInformation.CompanyID = @CompanyID and FA_AssetInformation.Status = 1
and FA_SET_AssetType.AssetTypeName like 'Land'
{0}
group by SET_OperatingUnit.OperatingUnitName ";

        const string Query_LeasedAssetsReport = @"select INV_Item.Description, NoOfUnits ,Rate  from FA_LeaseHeader 
left join INV_Item on INV_Item.ItemID = FA_LeaseHeader.ItemID
where TransactionStatus = 1 and FA_LeaseHeader.CompanyID = @CompanyID and FA_LeaseHeader.Status = 1
{0}";

        const string Query_AssetAdditionReport = @"
Select stockHead.DocumentNo as GRNNo,
AP_PurchaseOrderDetail.CategoryID, 
INV_SET_Category.CategoryName,INV_Item.ItemID, INV_Item.Description As ItemName,
FA_SET_AssetType.AssetTypeName As AssetTypeName,FA_SET_AssetType.AssetTypeID
,INV_Item.Description as Group2ItemName 
,Set_UnitOfMeasure.UOMName,
(Case when FA_AssetInformation.TagPrefix IS not null
then  '1' 
else stockDet.TransactionQuantity end ) as TransactionQuantity, 
(Case when FA_AssetInformation.TagPrefix IS not null
then  AP_PurchaseOrderDetail.UnitPrice
else (stockDet.TransactionQuantity*AP_PurchaseOrderDetail.UnitPrice) end ) as Rate,
stockHead.DocumentDate as AssetDate ,
AP_PurchaseOrderHeader.DocumentDate as PurchaseDate,
SET_DocumentType.DocumentType AS TransactionType, ISNULL(
(
select LookupDetail.EnumeratedDescription from LookupDetail where LookUpDetailID = stockHead.StatusCode and LookupHeaderID = 5
), '') as DocumentStatus, DocumentTypesGroup.GroupName,FA_AssetInformation.TagPrefix +' '+ FA_AssetInformation.TagNumber as TagNumber
,SET_CostCenter.CostCenterName, SET_Location.LocationName

from AP_PurchaseOrderDetail 

inner join AP_PurchaseOrderHeader on AP_PurchaseOrderDetail.POHeaderID = AP_PurchaseOrderHeader.POHeaderID
and AP_PurchaseOrderDetail.CompanyID = AP_PurchaseOrderHeader.CompanyID
and AP_PurchaseOrderDetail.PeriodID = AP_PurchaseOrderHeader.PeriodID 

Inner Join INV_SET_Category ON AP_PurchaseOrderDetail.CategoryID = INV_SET_Category.CategoryID 

Inner Join INV_Item ON AP_PurchaseOrderDetail.ItemID = INV_Item.ItemID 

Inner Join Set_UnitOfMeasure ON AP_PurchaseOrderDetail.ItemUOMID = Set_UnitOfMeasure.UOMID 

Left Join INV_StockTransactionDetail stockDet On AP_PurchaseOrderDetail.CompanyID = stockDet.CompanyID And AP_PurchaseOrderDetail.PeriodID = stockDet.PeriodID 
And AP_PurchaseOrderDetail.POHeaderID = stockDet.SourceHeaderID And AP_PurchaseOrderDetail.PODetailID = stockDet.SourceLineID And stockDet.Status = 1 
And stockDet.IsActive = 1 AND stockDet.TransactionQuantity > 0 

Left Join INV_StockTransactionHeader stockHead On stockDet.CompanyID = stockHead.CompanyID And stockDet.PeriodID = stockHead.PeriodID 
And stockDet.StockTransactionID = stockHead.StockTransactionID And stockHead.Status = 1 And stockHead.IsActive = 1 

left JOIN FA_AssetInformation ON FA_AssetInformation.CompanyID = stockDet.CompanyID
AND FA_AssetInformation.ItemID= stockDet.ItemID
AND FA_AssetInformation.StockHeaderID=stockDet.StockTransactionID  
AND FA_AssetInformation.StockDetailID=stockDet.TransactionDetailID
AND FA_AssetInformation.Status=1

LEFT JOIN FA_SET_AssetType on FA_SET_AssetType.AssetTypeID = INV_Item.EquipmentTypeID
AND FA_SET_AssetType.CompanyID = FA_AssetInformation.CompanyID

Left Join AR_SET_Customer On stockHead.PeriodID = AR_SET_Customer.PeriodID 
And stockHead.BusinessPartnerID = AR_SET_Customer.CustomerID 

Left Join SET_DocumentType On stockHead.CompanyID = SET_DocumentType.CompanyID And stockHead.PeriodID = SET_DocumentType.PeriodID 
And stockHead.DocumentTypeID = SET_DocumentType.DocumentTypeID 

Left Join DocumentTypesGroup On DocumentTypesGroup.GroupID = Set_DocumentType.GroupID
AND DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID

left JOIN SET_CostCenter ON SET_CostCenter.CostCenterID = stockHead.CostCenterID

left Join SET_Location ON SET_Location.LocationID = FA_AssetInformation.LocationID

Where AP_PurchaseOrderDetail.CompanyID = @CompanyID And AP_PurchaseOrderDetail.PeriodID = @PeriodID And AP_PurchaseOrderDetail.Status = 1
And AP_PurchaseOrderDetail.IsActive = 1 
AND stockHead.DocumentDate between @FromDate AND @ToDate  AND stockHead.StatusCode = 1
and SET_DocumentType.DocumentTypeID = @DocumentTypeID
";

        const string Query_FixedAssetAdditionReport = @"Select stockHead.DocumentNo as GRNNo,
    AP_PurchaseOrderDetail.CategoryID, 
    INV_SET_Category.CategoryName,INV_Item.ItemID, INV_Item.Description As ItemName,
    FA_SET_AssetType.AssetTypeName As AssetTypeName,FA_SET_AssetType.AssetTypeID
    ,INV_Item.Description as Group2ItemName 
    ,Set_UnitOfMeasure.UOMName,
    (Case when FA_AssetInformation.TagPrefix IS not null
     then  '1' 
     else stockDet.TransactionQuantity end ) as TransactionQuantity, 
     FA_AssetInformation.Rate as Rate,
     FA_AssetInformation.StockGRNDate as AssetDate ,
    FA_AssetInformation.PurchaseDate as PurchaseDate,
    SET_DocumentType.DocumentType AS TransactionType, ISNULL(
    (
        select LookupDetail.EnumeratedDescription from LookupDetail where LookUpDetailID = stockHead.StatusCode and LookupHeaderID = 5
    ), '') as DocumentStatus, DocumentTypesGroup.GroupName,FA_AssetInformation.TagPrefix +' '+ FA_AssetInformation.TagNumber as TagNumber
  ,SET_CostCenter.CostCenterName
from FA_AssetInformation 
    
	 Left join AP_PurchaseOrderDetail on AP_PurchaseOrderDetail.POHeaderID = FA_AssetInformation.POHeaderID
	and AP_PurchaseOrderDetail.CompanyID = FA_AssetInformation.CompanyID
	
	Left join AP_PurchaseOrderHeader on AP_PurchaseOrderDetail.POHeaderID = AP_PurchaseOrderHeader.POHeaderID
	and AP_PurchaseOrderDetail.CompanyID = AP_PurchaseOrderHeader.CompanyID
	and AP_PurchaseOrderDetail.PeriodID = AP_PurchaseOrderHeader.PeriodID

	Left Join INV_Item ON 
FA_AssetInformation.ItemID = INV_Item.ItemID 

Left Join INV_SET_Category ON 
INV_Item.ItemTypeID = INV_SET_Category.CategoryID 

	Left Join Set_UnitOfMeasure ON AP_PurchaseOrderDetail.ItemUOMID = Set_UnitOfMeasure.UOMID 
	Left Join INV_StockTransactionDetail stockDet On AP_PurchaseOrderDetail.CompanyID = stockDet.CompanyID And AP_PurchaseOrderDetail.PeriodID = stockDet.PeriodID 
        And AP_PurchaseOrderDetail.POHeaderID = stockDet.SourceHeaderID And AP_PurchaseOrderDetail.PODetailID = stockDet.SourceLineID And stockDet.Status = 1 
        And stockDet.IsActive = 1 AND stockDet.TransactionQuantity > 0 
	Left Join INV_StockTransactionHeader stockHead On stockDet.CompanyID = stockHead.CompanyID And stockDet.PeriodID = stockHead.PeriodID 
        And stockDet.StockTransactionID = stockHead.StockTransactionID And stockHead.Status = 1 And stockHead.IsActive = 1 
	
	
	 LEFT JOIN FA_SET_AssetType on FA_SET_AssetType.AssetTypeID = INV_Item.EquipmentTypeID
	    --AND FA_SET_AssetType.CompanyID = INV_Item.CompanyID
    AND FA_SET_AssetType.CompanyID = FA_AssetInformation.CompanyID
Left Join AR_SET_Customer On --stockHead.CompanyID = AR_SET_Customer.CompanyID And 
stockHead.PeriodID = AR_SET_Customer.PeriodID 
        And stockHead.BusinessPartnerID = AR_SET_Customer.CustomerID 
	Left Join SET_DocumentType On stockHead.CompanyID = SET_DocumentType.CompanyID And stockHead.PeriodID = SET_DocumentType.PeriodID 
        And stockHead.DocumentTypeID = SET_DocumentType.DocumentTypeID 
    Left Join DocumentTypesGroup On DocumentTypesGroup.GroupID = Set_DocumentType.GroupID
--DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And  
left JOIN SET_CostCenter ON SET_CostCenter.CostCenterID = FA_AssetInformation.CostCenterID
Where FA_AssetInformation.CompanyID = @CompanyID  And FA_AssetInformation.Status = 1 
      AND FA_AssetInformation.StockGRNDate between @FromDate AND @ToDate  
    and (SET_DocumentType.DocumentTypeID = @DocumentTypeID OR @DocumentTypeID iS NULL)
    ";


        const string Query_CustodianReport = @" SELECT FA_AssetAssignment.AssignedDate,FA_AssetAssignment.TagNumber,FA_SET_AssetType.AssetTypeName,
  AR_SET_Customer.CustomerName,SET_Location.LocationName,SET_Department.DepartmentName,SET_CostCenter.CostCenterName
  FROM FA_AssetAssignment
  INNER JOIN FA_AssetIdentity on FA_AssetIdentity.TagNumber=FA_AssetAssignment.TagNumber
  AND FA_AssetIdentity.CompanyID=FA_AssetAssignment.CompanyID 
  INNER JOIN FA_SET_AssetType ON FA_SET_AssetType.AssetTypeID=FA_AssetIdentity.AssetTypeID
  AND FA_SET_AssetType.CompanyID=FA_AssetIdentity.CompanyID
  INNER JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID=FA_AssetAssignment.CustodianID
  --AND AR_SET_Customer.CompanyID=FA_AssetAssignment.CompanyID
  INNER JOIN SET_Location ON SET_Location.LocationID=FA_AssetAssignment.LocationID
  INNER JOIN SET_Department ON SET_Department.DepartmentID=FA_AssetAssignment.DepartmentID
  INNER JOIN SET_CostCenter ON SET_CostCenter.CostCenterID=FA_AssetAssignment.CostCenterID
  
  where FA_AssetAssignment.CompanyID = @CompanyID
  AND FA_AssetAssignment.Status = 1
  ";

        const string Query_AssetInformationReport = @" Select Distinct FA_AssetInformation.AssetInfoID,INV_Item.Description,FA_AssetInformation.POHeaderID,FA_AssetInformation.PurchaseDate,
FA_SET_AssetType.AssetTypeName,SET_CostCenter.CostCenterName,AR_SET_Customer.CustomerName,Convert(varchar(10),FA_Set_AssetLife.IntervalValue) +' (' + FA_Set_AssetLife.PeriodType + ')' AS [Periods] 
, FA_AssetInformation.StockGRNDate,FA_AssetInformation.Rate,(FA_AssetInformation.TagPrefix) +'-' + FA_AssetInformation.TagNumber + '' AS Tag
,SET_Location.LocationName,AP_PurchaseOrderHeader.DocumentNumber,INV_StockTransactionHeader.DocumentNo AS GRNNO,TC.CustomerName as TC
,TCS.CostCenterName AS TCS ,TL.LocationName AS TL,FA_TransferHistory.AssetTransactionID,(FA_AssetInformation.TagPrefix) +'-' + FA_AssetInformation.TagNumber + '' AS Tag1,FA_TransferHistory.DateOftransfer
From FA_AssetInformation
LEFT JOIN SET_CostCenter ON  SET_CostCenter.CostCenterID=FA_AssetInformation.CostCenterID
AND FA_AssetInformation.Status=1 And SET_CostCenter.Status=1 

LEFT JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = FA_AssetInformation.CustodianID
--AND AR_SET_Customer.CompanyID = FA_AssetInformation.CompanyID

INNER JOIN INV_Item ON INV_Item.ItemID = FA_AssetInformation.ItemID
--AND INV_Item.CompanyID = FA_AssetInformation.CompanyID

INNER JOIN INV_SET_Category ON INV_SET_Category.CategoryID=INV_Item.ItemTypeID
-- 

LEFT JOIN FA_Set_AssetLife ON FA_Set_AssetLife.AssetLifeID = FA_AssetInformation.ExpectedLifeID
AND FA_Set_AssetLife.CompanyID = FA_AssetInformation.CompanyID

Left JOIN AP_PurchaseOrderHeader ON AP_PurchaseOrderHeader.POHeaderID = FA_AssetInformation.POHeaderID
AND AP_PurchaseOrderHeader.CompanyID = FA_AssetInformation.CompanyID

INNER JOIN FA_SET_AssetType ON FA_SET_AssetType.AssetTypeID = FA_AssetInformation.AssetTypeID
AND FA_SET_AssetType.CompanyID = FA_AssetInformation.CompanyID

Left Join SET_Location ON SET_Location.LocationID = FA_AssetInformation.LocationID

Left JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = FA_AssetInformation.StockHeaderID
AND INV_StockTransactionHeader.CompanyID = FA_AssetInformation.CompanyID

Left JOIN FA_TransferHistory ON FA_TransferHistory.AssetTransactionID= FA_AssetInformation.AssetInfoID 

LEFT JOIN AR_SET_Customer TC ON TC.CustomerID = FA_TransferHistory.ResponsiblePersonID
--AND AR_SET_Customer.CompanyID = FA_TransferHistory.CompanyID

LEFT JOIN SET_CostCenter TCS ON  TCS.CostCenterID=FA_TransferHistory.TOCostCenterID
AND FA_TransferHistory.Status=1 And TCS.Status=1 

Left Join SET_Location TL ON TL.LocationID = FA_TransferHistory.ToLocationID



 where FA_AssetInformation.CompanyID = @CompanyID
  --AND FA_AssetInformation.PeriodID = @PeriodID 
  AND FA_AssetInformation.Status = 1 
{0}";
    


        #endregion

        #region Class Methods

        public static DataTable GetDataForDepreciation(int companyID, Nullable<int> assetTypeID, Nullable<short> policyTypeCode, Nullable<short> periodTypeCode, 
            SqlConnection objSQL_Connection)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if(assetTypeID.HasValue)
                    sqlparams[1] = new SqlParameter("@AssetTypeID", assetTypeID.Value);
                else
                    sqlparams[1] = new SqlParameter("@AssetTypeID", DBNull.Value);

                if(policyTypeCode.HasValue)
                    sqlparams[2] = new SqlParameter("@PolicyType", policyTypeCode.Value);
                else
                    sqlparams[2] = new SqlParameter("@PolicyType", DBNull.Value);

                if(periodTypeCode.HasValue)
                    sqlparams[3] = new SqlParameter("@PeriodType", periodTypeCode.Value);
                else
                    sqlparams[3] = new SqlParameter("@PeriodType", DBNull.Value);

                DataSet ds = SqlHelper.ExecuteDataset(objSQL_Connection, CommandType.Text, Query_GetDataForDepreciation, sqlparams);

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


        public static DataTable GetDataForDepreciationAssetInformation(int companyID, Nullable<int> assetTypeID,Nullable<int> itemId, Nullable<short> policyTypeCode, Nullable<short> periodTypeCode,StringBuilder sp,
     SqlConnection objSQL_Connection)
        {
            DataTable dtToReturn = null;

            try
            {
                StringBuilder sb = new StringBuilder();

                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (assetTypeID.HasValue)
                    sqlparams[1] = new SqlParameter("@AssetTypeID", assetTypeID.Value);
                else
                    sqlparams[1] = new SqlParameter("@AssetTypeID", DBNull.Value);

                if (policyTypeCode.HasValue)
                    sqlparams[2] = new SqlParameter("@PolicyType", policyTypeCode.Value);
                else
                    sqlparams[2] = new SqlParameter("@PolicyType", DBNull.Value);

                if (periodTypeCode.HasValue)
                    sqlparams[3] = new SqlParameter("@PeriodType", periodTypeCode.Value);
                else
                    sqlparams[3] = new SqlParameter("@PeriodType", DBNull.Value);

                if (itemId.HasValue)
                    sqlparams[4] = new SqlParameter("@itemId", itemId.Value);
                else
                    sqlparams[4] = new SqlParameter("@itemId", DBNull.Value);

                sp.Append(sp.ToString());


                DataSet ds = SqlHelper.ExecuteDataset(objSQL_Connection, CommandType.Text, String.Format(Query_GetDataForDepreciationAssetInformation,sp), sqlparams);

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

        public static DataTable GetDataForDepreciationAssetInformationToCheckPeriodType(int companyID, Nullable<int> assetTypeID, Nullable<int> itemId, Nullable<short> policyTypeCode, StringBuilder sp,
     SqlConnection objSQL_Connection)
        {
            DataTable dtToReturn = null;

            try
            {
                StringBuilder sb = new StringBuilder();

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (assetTypeID.HasValue)
                    sqlparams[1] = new SqlParameter("@AssetTypeID", assetTypeID.Value);
                else
                    sqlparams[1] = new SqlParameter("@AssetTypeID", DBNull.Value);

                if (policyTypeCode.HasValue)
                    sqlparams[2] = new SqlParameter("@PolicyType", policyTypeCode.Value);
                else
                    sqlparams[2] = new SqlParameter("@PolicyType", DBNull.Value);

                if (itemId.HasValue)
                    sqlparams[3] = new SqlParameter("@itemId", itemId.Value);
                else
                    sqlparams[3] = new SqlParameter("@itemId", DBNull.Value);

                sp.Append(sp.ToString());


                DataSet ds = SqlHelper.ExecuteDataset(objSQL_Connection, CommandType.Text, String.Format(Query_GetDataDepreciationAssetInformationForPeriodType, sp), sqlparams);

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


        public static DataTable GetDataForQuarterDepreciationAssetInformation(int companyID, Nullable<int> assetTypeID, Nullable<short> policyTypeCode, Nullable<short> periodTypeCode, string QuarterTypes,
     SqlConnection objSQL_Connection)
        {
            DataTable dtToReturn = null;

            try
            {

                #region Query
                const string Query_GetDataForExistDepreciationAssetInformation = @" 
Select  AssetIdentityID, CompanyID, DepreciationProcessID, AssetTypeID, FAFromDate, FAUpTo, QuarterTypes

from FA_DepreciationProcess
 where FA_DepreciationProcess.Status = 1 and 
  FA_DepreciationProcess.CompanyID = @CompanyID and FA_DepreciationProcess.AssetTypeID = @AssetTypeID 
   and FA_DepreciationProcess.QuarterTypes is not null
   and (FA_DepreciationProcess.PeriodStatusID = @PeriodType or @PeriodType is null)
    {0}
    Order by FA_DepreciationProcess.CreateDate DESC
    ";
                #endregion

                StringBuilder sb = new StringBuilder();

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (assetTypeID.HasValue)
                    sqlparams[1] = new SqlParameter("@AssetTypeID", assetTypeID.Value);
                else
                    sqlparams[1] = new SqlParameter("@AssetTypeID", DBNull.Value);

                if (policyTypeCode.HasValue)
                    sqlparams[2] = new SqlParameter("@PolicyType", policyTypeCode.Value);
                else
                    sqlparams[2] = new SqlParameter("@PolicyType", DBNull.Value);

                if (periodTypeCode.HasValue)
                    sqlparams[3] = new SqlParameter("@PeriodType", periodTypeCode.Value);
                else
                    sqlparams[3] = new SqlParameter("@PeriodType", DBNull.Value);

                if (QuarterTypes != "Select")
                {
                    sb.Append(" and (FA_DepreciationProcess.QuarterTypes = '" + QuarterTypes + "'");
                }



                DataSet ds = SqlHelper.ExecuteDataset(objSQL_Connection, CommandType.Text, String.Format(Query_GetDataForExistDepreciationAssetInformation, sb.ToString()), sqlparams);

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

        public static DataTable GetDataForQuarterDepreciationAssetInformationForYear(int companyID, int fiscalID, Nullable<int> assetTypeID, Nullable<short> policyTypeCode, Nullable<short> periodTypeCode, string YearTypes, string query,
     SqlConnection objSQL_Connection)
        {
            DataTable dtToReturn = null;

            try
            {
                StringBuilder sb = new StringBuilder();

                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@FiscalID", fiscalID);

                if (assetTypeID.HasValue)
                    sqlparams[2] = new SqlParameter("@AssetTypeID", assetTypeID.Value);
                else
                    sqlparams[2] = new SqlParameter("@AssetTypeID", DBNull.Value);

                if (policyTypeCode.HasValue)
                    sqlparams[3] = new SqlParameter("@PolicyType", policyTypeCode.Value);
                else
                    sqlparams[3] = new SqlParameter("@PolicyType", DBNull.Value);

                if (periodTypeCode.HasValue)
                    sqlparams[4] = new SqlParameter("@PeriodType", periodTypeCode.Value);
                else
                    sqlparams[4] = new SqlParameter("@PeriodType", DBNull.Value);

                if (YearTypes != "Select")
                {
                    sb.Append(" and (FA_DepreciationProcess.YearTypes = '" + YearTypes + "'");
                }



                DataSet ds = SqlHelper.ExecuteDataset(objSQL_Connection, CommandType.Text, String.Format(query, sb.ToString()), sqlparams);

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

        public static DataTable DepreciationReport(int companyID, string dateFrom = null, string dateTo = null, int? AssetID = null, int? PostStatusID = null) //, int periodID
        {
            DataTable result = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                //sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (AssetID.HasValue)
                {
                    sb.Append(" AND FA_SET_AssetType.AssetTypeID = " + AssetID);
                }

                if (dateFrom != string.Empty && dateTo != string.Empty)
                {
                    sb1.Append(" AND CAST( FA_DepreciationProcess.DepreciatedUpto AS Date) BETWEEN '" + dateFrom + "' AND '" + dateTo + "' ");
                }

                if (PostStatusID.HasValue)
                    sb1.Append(" AND FA_DepreciationProcess.IsPosted = " + PostStatusID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_DepreciationReport, sb.ToString(), sb1.ToString()), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    result = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataTable DisposalReport(int companyID, string dateFrom = null, string dateTo = null)
        {
            DataTable result = null;
            StringBuilder additionalParameters = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               
                if (dateFrom != string.Empty && dateTo != string.Empty)
                {
                    additionalParameters.Append(" AND CAST(FA_Disposal.DisposalDate AS Date) BETWEEN '" + dateFrom + "' AND '" + dateTo + "' ");
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_DisposalReport, additionalParameters), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    result = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataTable FARegisterReport(int CompanyID, DateTime fromDate, DateTime ToDate, int? AssetTypeID, int? CostCenterID, int? ItemID)
        {
            DataTable dt = null;
            StringBuilder additionalParameters = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@FromDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(fromDate) + " 00:00:00"));
                sqlparams[2] = new SqlParameter("@ToDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(ToDate) + " 23:59:59"));
                if (AssetTypeID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.AssetTypeID = " + AssetTypeID.Value);

                if (CostCenterID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.CostCenterID = " + CostCenterID.Value);

                if (ItemID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.ItemID = " + ItemID.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_FARegisterReport, additionalParameters), sqlparams);

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

        public static DataTable FARegisterReport_RSM(int CompanyID, DateTime fromDate, DateTime ToDate, int? AssetTypeID, int? CostCenterID, int? ItemID)
        {
            DataTable dt = null;
            StringBuilder additionalParameters = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@FromDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(fromDate) + " 00:00:00"));
                sqlparams[2] = new SqlParameter("@ToDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(ToDate) + " 23:59:59"));
                if (AssetTypeID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.AssetTypeID = " + AssetTypeID.Value);

                if (CostCenterID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.CostCenterID = " + CostCenterID.Value);

                if (ItemID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.ItemID = " + ItemID.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_FARegisterReport_RSM, additionalParameters), sqlparams);

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

        public static DataTable IdleAssetsReport(int CompanyID, DateTime fromDate, DateTime ToDate, int? AssetTypeID, int? CostCenterID, int? ItemID)
        {
            DataTable dt = null;
            StringBuilder additionalParameters = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                //sqlparams[1] = new SqlParameter("@FromDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(fromDate) + " 00:00:00"));
                //sqlparams[2] = new SqlParameter("@ToDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(ToDate) + " 23:59:59"));
                //if (AssetTypeID.HasValue)
                //    additionalParameters.Append(" AND FA_AssetInformation.AssetTypeID = " + AssetTypeID.Value);

                //if (CostCenterID.HasValue)
                //    additionalParameters.Append(" AND FA_AssetInformation.CostCenterID = " + CostCenterID.Value);

                if (ItemID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.ItemID = " + ItemID.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_IdleAssetsReport, additionalParameters), sqlparams);

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


        public static DataTable DetailOfInvestmentPropertyReport(int CompanyID, DateTime fromDate, DateTime ToDate, int? AssetTypeID, int? CostCenterID, int? ItemID)
        {
            DataTable dt = null;
            StringBuilder additionalParameters = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                //sqlparams[1] = new SqlParameter("@FromDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(fromDate) + " 00:00:00"));
                //sqlparams[2] = new SqlParameter("@ToDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(ToDate) + " 23:59:59"));
                //if (AssetTypeID.HasValue)
                //    additionalParameters.Append(" AND FA_AssetInformation.AssetTypeID = " + AssetTypeID.Value);

                //if (CostCenterID.HasValue)
                //    additionalParameters.Append(" AND FA_AssetInformation.CostCenterID = " + CostCenterID.Value);

                if (ItemID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.ItemID = " + ItemID.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_DetailOfInvestmentProperty, additionalParameters), sqlparams);

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

        public static DataTable AdvancesForPurchaseOfLandInvestmentPropertyReport(int CompanyID, DateTime fromDate, DateTime ToDate, int? AssetTypeID, int? CostCenterID, int? ItemID)
        {
            DataTable dt = null;
            StringBuilder additionalParameters = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                //sqlparams[1] = new SqlParameter("@FromDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(fromDate) + " 00:00:00"));
                //sqlparams[2] = new SqlParameter("@ToDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(ToDate) + " 23:59:59"));
                //if (AssetTypeID.HasValue)
                //    additionalParameters.Append(" AND FA_AssetInformation.AssetTypeID = " + AssetTypeID.Value);

                //if (CostCenterID.HasValue)
                //    additionalParameters.Append(" AND FA_AssetInformation.CostCenterID = " + CostCenterID.Value);

                if (ItemID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.ItemID = " + ItemID.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_AdvancesForPurchaseOfLandInvestmentProperty, additionalParameters), sqlparams);

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

        
        public static DataTable FreeholdLandReport(int CompanyID, DateTime fromDate, DateTime ToDate, int? AssetTypeID, int? CostCenterID, int? ItemID)
        {
            DataTable dt = null;
            StringBuilder additionalParameters = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                //sqlparams[1] = new SqlParameter("@FromDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(fromDate) + " 00:00:00"));
                //sqlparams[2] = new SqlParameter("@ToDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(ToDate) + " 23:59:59"));
                //if (AssetTypeID.HasValue)
                //    additionalParameters.Append(" AND FA_AssetInformation.AssetTypeID = " + AssetTypeID.Value);

                //if (CostCenterID.HasValue)
                //    additionalParameters.Append(" AND FA_AssetInformation.CostCenterID = " + CostCenterID.Value);

                if (ItemID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.ItemID = " + ItemID.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_FreeholdLandReport, additionalParameters), sqlparams);

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

        public static DataTable SummaryOfLandReport(int CompanyID, DateTime fromDate, DateTime ToDate, int? AssetTypeID, int? CostCenterID, int? ItemID)
        {
            DataTable dt = null;
            StringBuilder additionalParameters = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                //sqlparams[1] = new SqlParameter("@FromDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(fromDate) + " 00:00:00"));
                //sqlparams[2] = new SqlParameter("@ToDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(ToDate) + " 23:59:59"));
                //if (AssetTypeID.HasValue)
                //    additionalParameters.Append(" AND FA_AssetInformation.AssetTypeID = " + AssetTypeID.Value);

                //if (CostCenterID.HasValue)
                //    additionalParameters.Append(" AND FA_AssetInformation.CostCenterID = " + CostCenterID.Value);

                if (ItemID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.ItemID = " + ItemID.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SummaryOfLandReport, additionalParameters), sqlparams);

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


        public static DataTable LeasedAssetsReport(int CompanyID, DateTime fromDate, DateTime ToDate, int? AssetTypeID, int? CostCenterID, int? ItemID)
        {
            DataTable dt = null;
            StringBuilder additionalParameters = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                //sqlparams[1] = new SqlParameter("@FromDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(fromDate) + " 00:00:00"));
                //sqlparams[2] = new SqlParameter("@ToDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(ToDate) + " 23:59:59"));
                //if (AssetTypeID.HasValue)
                //    additionalParameters.Append(" AND FA_AssetInformation.AssetTypeID = " + AssetTypeID.Value);

                //if (CostCenterID.HasValue)
                //    additionalParameters.Append(" AND FA_AssetInformation.CostCenterID = " + CostCenterID.Value);

                if (ItemID.HasValue)
                    additionalParameters.Append(" AND FA_LeaseHeader.ItemID = " + ItemID.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_LeasedAssetsReport, additionalParameters), sqlparams);

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


        public static DataTable FAAssetCostReport(int companyID, int periodID,int vendorID,string FromDate = null, string ToDate = null)
        {
            DataTable result = null;
            string additionalParameters = string.Empty;
            StringBuilder additionalClause = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);
                if (vendorID > 0)
                {
                    additionalClause.Append(" and AR_SET_Customer.CustomerID = " + vendorID);
                }              

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_FAAssetCostReport, additionalClause.ToString()), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    result = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataTable FAAssetCostSummaryReport(int companyID, int periodID, int vendorID, string FromDate = null, string ToDate = null)
        {
            DataTable result = null;
            string additionalParameters = string.Empty;
            StringBuilder additionalClause = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);
                if (vendorID > 0)
                {
                    additionalClause.Append(" and AR_SET_Customer.CustomerID = " + vendorID);
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_FAAssetCostSummaryReport, additionalClause.ToString()), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    result = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataTable AssetsAdditionReport(int companyID, int periodID, int DocumentType, string isdeprection, string dateFrom = null, string dateTo = null, int? AssetID = null, int? AssetTypeID = null, int? LocationID = null)
        {
            DataTable result = null;
            string additionalParameters = string.Empty;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", DocumentType);
                sqlparams[3] = new SqlParameter("@FromDate", dateFrom);
                sqlparams[4] = new SqlParameter("@ToDate", dateTo);

                if (AssetID.HasValue)
                {
                    additionalParameters += " AND INV_Item.EquipmentTypeID = " + AssetID;
                }

                if (AssetTypeID.HasValue)
                {
                    additionalParameters += " AND FA_AssetInformation.AssetTypeID = " + AssetTypeID;
                }

                if (LocationID.HasValue)
                {
                    additionalParameters += " AND FA_AssetInformation.LocationID = " + LocationID;
                }
                    // if(isdeprection=="")

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_AssetAdditionReport + additionalParameters + isdeprection, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    result = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataTable FixedAssetsAdditionReport(int companyID, int periodID, string isdeprection, string dateFrom = null, string dateTo = null, int? AssetID = null, int? AssetTypeID = null, int? DocumentType= null)
        {
            DataTable result = null;
            string additionalParameters = string.Empty;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", DocumentType);
                sqlparams[3] = new SqlParameter("@FromDate", dateFrom);
                sqlparams[4] = new SqlParameter("@ToDate", dateTo);

                if (AssetID.HasValue)
                {
                    additionalParameters += " AND INV_Item.EquipmentTypeID = " + AssetID;
                }

                if (AssetTypeID.HasValue)
                {
                    additionalParameters += " AND FA_AssetInformation.AssetTypeID = " + AssetTypeID;
                }
                // if(isdeprection=="")

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_FixedAssetAdditionReport + additionalParameters + isdeprection, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    result = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static decimal GetDepreciationByAssetInfoID(int companyID, int AssetIdentityID)
        {
            DataTable dtToReturn = null;
            decimal DepreciationAmount = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@AssetIdentityID", AssetIdentityID);


                string Query_Get_DepreciationAmount = @"
                            select sum(DepreciationAmount)DepreciationAmount
                            From FA_DepreciationProcess 
                            Where FA_DepreciationProcess.CompanyID = @CompanyID 
                            And FA_DepreciationProcess.AssetIdentityID =  @AssetIdentityID

                ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_DepreciationAmount, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];

                if (dtToReturn != null && dtToReturn.Rows.Count > 0)
                    DepreciationAmount = dtToReturn.Rows[0]["DepreciationAmount"].ToString() != "" ? Convert.ToDecimal(dtToReturn.Rows[0]["DepreciationAmount"]) : 0;


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return DepreciationAmount;
        }


        public static DataTable FA_CustodianReport(int companyID, string dateFrom = null, string dateTo = null, int? CustodianID=null) //, int periodID
        {
            DataTable result = null;
            string additionalParameters = string.Empty;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                //sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (CustodianID.HasValue)
                {
                    additionalParameters += " AND FA_AssetAssignment.CustodianID = " + CustodianID;
                }
                    if (dateFrom != string.Empty && dateTo != string.Empty)
                {
                    additionalParameters += " AND CAST(FA_AssetAssignment.AssignedDate AS Date) BETWEEN '" + dateFrom + "' AND '" + dateTo + "' ORDER BY FA_AssetAssignment.AssignedDate ";

                }


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_CustodianReport + additionalParameters, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    result = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataTable AssetsInformationReport(int companyID, int periodID, string dateFrom = null, string dateTo = null, int? AssetID = null, int? CostCenterID = null, int? ItemID = null, int? TagID = null)
        {
            DataTable result = null;
            StringBuilder additionalParameters = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                //sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (dateFrom != string.Empty && dateTo != string.Empty)
                {
                    additionalParameters.Append ( " AND CAST(FA_AssetInformation.PurchaseDate AS Date) BETWEEN '" + dateFrom + "' AND '" + dateTo + "' ");
                }

                if (AssetID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.AssetTypeID = " + AssetID.Value);

                if (CostCenterID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.CostCenterID = " + CostCenterID.Value);

                if (ItemID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.ItemID = " + ItemID.Value);

                if (TagID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.AssetInfoID = " + TagID.Value);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_AssetInformationReport, additionalParameters), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    result = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }


        public static decimal GetAccumulatedDepreciationOfAsset(int companyID, int periodID, int? AssetID)
        {
            DataTable result = null;
            StringBuilder additionalParameters = new StringBuilder();
            decimal DepreciationAmount = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@AssetInfoID", AssetID);

                string Query = @"
                    select sum(DepreciationAmount)DepreciationAmount from FA_DepreciationProcess where AssetIdentityID = @AssetInfoID and 
                    CompanyID = @CompanyID
                    and status = 1;
                ";


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    result = ds.Tables[0];

                if (result != null && result.Rows.Count > 0)
                    DepreciationAmount = result.Rows[0]["DepreciationAmount"].ToString()!=""?Convert.ToDecimal(result.Rows[0]["DepreciationAmount"].ToString()):0;

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return DepreciationAmount;
        }
        
        
        
        #endregion




        public static object GetAllActiveDataDetailOfAgriLand(int CompanyID, int? ItemID)
        {
            DataTable dt = null;
            StringBuilder additionalParameters = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);

                if (ItemID.HasValue)
                    additionalParameters.Append(" AND FA_AssetInformation.ItemID = " + ItemID.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_DetailOfLandReport, additionalParameters), sqlparams);

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
    }
}
