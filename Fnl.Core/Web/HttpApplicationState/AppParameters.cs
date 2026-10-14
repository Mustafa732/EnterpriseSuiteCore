using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;

using FnI.BusinessAccessLayer.Setup;

namespace FnI.Core.Web.HttpApplicationState
{
    public static class AppParameters
    {
        #region Constant Queries

        const string Query_Update_By_FieldName = @" UPDATE [Set_Parameter] SET [FieldValue1] = @FieldValue1 WHERE [FieldName] = @FieldName ";

        #endregion

        #region Private Static Variables

        private static bool _IsWelcomeScreen;
        private static bool _AssignPreviousPeriod;
        private static bool _MultiBusiness;
        private static bool _MultiCurrency;
        private static int _AssignCompanyID;
        private static int _AssignPeriodID;
        private static int _NoOfSegments;
        private static int _MaxDOQty;
        private static int _SegmentSize0;
        private static int _SegmentSize1;
        private static int _SegmentSize2;
        private static int _SegmentSize3;
        private static int _SegmentSize4;
        private static int _SegmentSize5;
        private static int _SegmentSize6;
        private static int _SegmentSize7;
        private static int _SegmentSize8;
        private static int _SegmentSize9;
        private static int _BaseCurrencyID;
        private static string _Separator;
        private static bool _DefaultAccountSegmentation;
        private static bool _IsManualGrowerCode;
        private static bool _IsBarcodeDisplay;
        private static int _IsAutoCPRNo;
        private static int _IsAutoTokenNo;
        private static int _LabName;
        private static string _CPRLanguage;
        private static string _CPRCopyName_1;
        private static string _CPRCopyName_2;
        private static string _CPRCopyName_3;
        private static string _CPRCopyName_4;
        private static string _CPRCopyName_5;
        private static string _LoanDedcution;
        private static bool _CPRHoldForRate;
        private static bool _PrintCPR;
        private static bool _ChangeVarietyOnFW;
        private static bool _IsUserDefinedSlips;
        private static string _TokenCopyName_1;
        private static string _TokenCopyName_2;
        private static string _TokenCopyName_3;
        private static string _TokenCopyName_4;
        private static string _TokenCopyName_5;
        private static bool _AllGrowersOnTokenWhenManualIndent;
        private static bool _BindingRounding;
        private static bool _TokenContractor;
        private static bool _FwImageCapture;
        private static string _CPRWaterMark_1;
        private static string _CPRWaterMark_2;
        private static string _CPRWaterMark_3;
        private static string _CPRWaterMark_4;
        private static string _CPRWaterMark_5;
        private static bool _DashboardDisplay;
        private static bool _IsNotification;
        private static bool _IsAlert;
        private static bool _IsTextile;
        private static bool _VoucherLocation;
        private static bool _PaymentTerms;
        private static bool _VehicleInformation;
        private static bool _IsColor;
        private static bool _SOPaymentDisplayNone;
        private static int _SeasonDays;
        private static int _QuotaProduction;
        private static int _QuotaPermit;
        private static bool _SecurityOnDocumentType;
        private static bool _ItemVehicleField;
        private static bool _DispatchWeightNotVisible;
        private static bool _DeliveryOrderRegisterSheikhoo;
        private static bool _ReceiptSheikhoo;
        private static bool _ExpenseCostCenterRequired;
        private static bool _GINSupplierRequired;
        private static bool _IsBlendFormulation;
        private static bool _SignatureForReport;
        private static bool _MultipleLoginBlocking;
        private static bool _IsVehicle;
        private static bool _IsGrossChangedToBasic;
        private static bool _IncometaxForAlnoor;
        private static bool _AKTSugar;
        private static bool _BusinessPartnerOnDispatch;
        private static decimal _SimilarWeightDifference;
        private static int _SimilarWeightCount;
        private static string _IssuanceFlow;
        private static bool _IsDisplayCostAtGRN;
        private static bool _SalesInvoiceAutoGenerate;
        private static bool _GINCommentsRequired;
        private static bool _InvisibleRequisitionInRFQ;
        private static bool _CategoryWiseSecurity;
        private static bool _CustomizeDispatch;
        private static bool _SaimaProductLine;
        private static bool _IsManualGatePassNo;
        private static bool _IsBatchWiseSale;
        private static bool _IsMultipleDO;
        private static bool _IsDirectDispatch;
        private static bool _LinkGrowerWithCircleAndCenter;
        private static int _CenterCodeLength;
        private static int _CircleCodeLength;
        private static bool _CostofSalesAccount;
        private static bool _InventoryAccountNew;
        private static bool _POScheduling;
        private static bool _SOScheduling;
        private static bool _IsLocationRackRequired;
        private static bool _DisplayStoppedCPR;
        private static bool _IsFixedAsset;
        private static bool _IsWeightRestrict;
        private static bool _PaymentTaxes;
        private static bool _IsSugar;
        private static bool _MultipleGrowerAgainstCNIC;
        private static bool _ShowManualDeductionOnFirstWeight;
        private static bool _ContractorMustSelectOnHaulage;
        private static bool _IsLocationRackRequiredNormal;
        private static bool _AgentDealerRequired;
        private static bool _CNICNotRequired;
        private static bool _DriverInfoRequired;
        private static bool _DocumentTypeRequired;
        private static bool _IsDepartmentOnSO;
        private static bool _IsCenterRequiredUponRequisition;
        private static bool _RFQQuantityRequired;
        private static bool _RequiredPOinPaymentVoucher;
        private static bool _NotRequiredVehicleTypenNo;
        private static bool _NotRequiredVehicleType_No;
        private static bool _FilterPOByAdvancePercentage;
        private static bool _FilterLocationByWareHouse;
        private static short _PrintSlipNo;
        private static bool _IsDirectPO;
        private static decimal _FWDeductionPercentage;
        private static bool _OfflineWeighment;
        private static bool _Minimumweightvalidationonsecondweight;
        private static bool _ManualDateTime;
        private static bool _BrokerIsCustomer;
        private static int _IndentSupplyDateAdd;
        private static int _LeakageSupplyDateAdd;
        private static int _ShiftHours;
        private static bool _EditableRFQQuantity;
        private static bool _InvisibleEstimatedPOPrice;
        private static bool _LotNo;
        private static bool _TrashOnNetWeight;
        private static short _DOvalidUpToDays;
        private static bool _AutoCalculateInBags;
        private static bool _IsPaymentReportView;
        private static bool _ShowDispatchReportLink;
        private static bool _ShipToDeliveryOrder;
        private static int _SODispatchDateDays;
        private static bool _MultipleBanksInTransaction;
        private static string _ELPWebServiceURL;
        private static bool _ShowELPData;
        private static string _SecondWeightCopyName_1;
        private static string _SecondWeightCopyName_2;
        private static string _SecondWeightCopyName_3;
        private static string _SecondWeightCopyName_4;
        private static string _SecondWeightCopyName_5;
        private static string _FirstWeightCopyName_1;
        private static string _FirstWeightCopyName_2;
        private static string _FirstWeightCopyName_3;
        private static string _FirstWeightCopyName_4;
        private static string _FirstWeightCopyName_5;
        private static string _DepotTPTPaymentGL;
        private static Nullable<DateTime> _LastInterestProcessDate;
        private static string _ELPProxyAddress;
        private static string _ELPProxyUserID;
        private static string _ELPProxyPassword;
        private static bool _ELPByPassProxy;
        private static bool _RoadBlocker;
        private static bool _WeightStable;
        private static short _WeightBlockerVariance;
        private static short _MinimumVehicalWeight;
        private static bool _IsSlipCompanyLabel;
        private static bool _LinkGrowerWithIndent;
        private static bool _CarriageContractorPayableVoucher;
        private static bool _IsPRNCosting;
        private static bool _IsPartialDispatch;
        private static bool _GrowerGroupRecovery;
        private static bool _IsInvisibleUponStockReports;
        private static bool _IsReceiptAgainstSO;
        private static bool _IsSOFindMode;
        private static bool _IsDispatchStockLedgerHideCancel;
        private static bool _ZeroAllowed;
        private static bool _SalesDateValidation;
        private static bool _RFQItemSelection;
        private static bool _IsPaymentTax;
        private static bool _DuplicateManualIndentSeries;
        private static bool _DisableContractorByCP;
        private static bool _TMSUploadAccess;
        private static bool _HideListPriceOnRequisition;
        private static bool _SupplierCreditOnPayment;
        private static bool _StopLoanDeduction;
        private static bool _ShowQualityOnSO;
        private static bool _TaxPercentageInAccountsGrid;
        private static bool _SalaryProcessbyTMS;
        private static bool _SalaryProcessForAsadRice;
        private static bool _LateDeductionPolicy;
        private static bool _IsEmployeeCodeEditable;
        private static bool _RoasterForCIS;
        private static bool _SalaryProcessWithoutLeaves;
        private static bool _MultipleBusinessPartners;
        private static bool _POMultiRequisition;
        private static bool _IsOrient;
        private static bool _DisplayOtherUserRecords;
        private static bool _MandatorySOQualityBatch;
        private static bool _RestrictZeroQtyOnGIN;
        private static bool _ManualWeight;
        private static bool _ShowReportOnSave;
        private static int[] _BusinessPartnerIDsRange;
        private static bool _IsSynchronize;
        private static bool _EditAllRecords;
        private static bool _DefineFAProcessStarDate;
        private static bool _SetItemCodeByColor;
        private static bool _BandhiSalarySheet;
        private static bool _GrossSalaryTotal;
        private static bool _SalesInvoiceForPolymer;
        private static bool _DeliveryOrderForPolymer;
        private static bool _DispatchNoteForPolymer;
        private static bool _SalesOrderForPolymer;
        private static bool _DIInvoiceForPolymer;
        private static bool _CompanyNameWithoutOperatingUnit;
        private static bool _DeliveryScheduleForPolymer;
        private static bool _OverTimeCalculationForBandhi;
        private static bool _AttendanceUploadForAdam;
        private static bool _LoadMonthlyDocuments;
        private static bool _CommissionOnReceipt;
        private static bool _RFQDueDateDiscard;
        private static string _NameForCaneModule;
        private static bool _isGINWithoutGLCode;
        private static bool _IsGenerateReport;
        private static bool _ShowExpenseAccountGrid;
        private static bool _ValidateBilltyNoOnDispatch;
        private static bool _CPRVouchers = true;
        private static bool _CarConPaymentVoucher = true;
        private static bool _trashdeductionOntoken;
        private static bool _ItemParts;
        private static bool _FGPostGL;
        private static bool _ContractorManualRate;
        private static bool _HideWeightDedInDashboard;
        private static bool _IsAutoPrinting;
        private static bool _CopyButtonForVoucher;
        private static bool isProfitAndLoss;
        private static bool _AllowSelectedDocumentTypes;
        private static bool _AllowInprocessOption;
        private static bool isFiscalSystem;
        private static bool isStockFIFO;
        private static bool _StopGLStockTransfer;
        private static bool _UserWiseDocumentAppear;
        private static bool _OverTimeCalculationForMBI;
        private static bool _OverTimeCalculationForAlAbbas;
        private static bool _LoanAmountTypeWise;
        private static bool _OverTimeCalculationForSheikhoo;
        private static bool _AutoGenerateBarCode;
        private static string _FabricDepartment;
        private static string _FabricCostCenter;
        private static string _FabricLocation;
        private static string _SROPercent;
        private static string _SROZeroPercent;
        private static string _FinishingCostCenter;
        private static bool _SROInvoices;
        private static bool isLocationHide;
        private static bool isNOCOGSEntry;
        private static bool _AllowDetailNarration;
        private static bool _LeavePolicy;
        private static bool _IsFabricAlImdad;
        private static bool _IsDisallowBackDate;
        private static bool _IsGRNDisallowBackDate;
        private static string _LogisticVendorDefaultCOA;
        private static string _LoadingUnloadingDefaultCOA;
        private static string _ClearingDefaultCOA;
        private static string _TransporterDefaultCOA;
        private static string _VendorDefaultCOA;
        private static bool _TMSDataExcelFormatJKS;
        private static string _BGroupDefaultCOA;
        private static string _HGroupDefaultCOA;
        private static string _CommonGroupDefaultCOA;
        private static string _CompanyNameParam;
        private static int _BranchCompany;
        private static bool _DocumentIDsForSite; // if true Document Ids generate greater than 5000000
        private static bool _DispatchTo;
        private static bool _TaxCalculationOnContractDate;
        private static bool _ManualWeightSales;
        private static bool _ManualWeightInv;
        private static bool _ManualWeightSalesSteel;
        private static bool _DispatchReturnInTareWeight;
        private static bool _CustomerNameII;
        private static bool _IsSheikhooReport;
        private static bool _IsConnectReport;
        private static bool _IsHabibSugar;
        private static bool _IsTwoStar;
        private static bool _IsPatoki;
        private static bool _IsPrintsalesFirstWeight;
        private static bool _AllowInventoryBackDateEntry;
        private static string _SalesTaxImport;
        private static string _SalesTaxServices;
        private static string _AdvanceIncomeTaxImport;
        private static double _UnregisteredCustomerMonthlyLimit;
        private static double _UnregisteredCustomerYearlyLimit;
        private static bool _FetchDataFromDOInGateIn;
        private static bool _SalesDocumentType;
        private static bool _SaleInvoiceProcess;
        private static bool _IsAKTSEmpInfo;
        private static bool _HSMMultipleCustomerDOAtLoadingPoint;
        private static bool _LodingpointCustomerDispatch;
        private static bool _IsAllowancesAddInGross;
        private static bool _AllowAllDoctypeRequisitionAssignment;
        private static bool _HSMLoadingPointSlipHTML;
        private static bool _JKLetterOfCreditPOLookUP;
        private static bool _IsOverTimePartofsalary;
        private static bool _IsFormuladisplayonotperhour;
        private static string _CompanyName;
        private static Nullable<int> _WeightDiffAgainstVehicle = null;
        private static bool _IsHideTrashOnSecondWeightAutoSlip;
        private static bool _VarietyHideOnSlips;
        private static bool _ValidateSupplyDateIndent;
        private static bool _TrashDeductionFromGrossWeight;
        private static bool _DeductionOnWeightPayable;
        private static decimal _ToDateRecovery;
        private static bool _StopPrintReportOnFirstWeight;
        private static bool _TransferOutGL;
        private static bool _StopAllDeduction;
        private static bool _PrintReportOnSecondWeight;
        private static bool _MaxSupplyValidationOnToken;
        private static bool _IsShowVariety;
        private static bool isGrowerReference;
        private static bool _IsShowAreaName;
        private static bool _IsShowSurveyID;
        private static bool _BandhiAutoPrint;
        private static int _IndentExpiredInNoOfDays;
        private static bool _BonusForHabib;
        private static bool _IsPaySlipConnect;
        private static bool _InvoiceRateSheikhoo;
        private static bool _GateParameterForHabib;
        private static bool _IsEmployeeCodeMandatory;
        private static bool _InOutTransTypeAndImpact;
        private static bool _RackBatchExpiry;
        private static bool _NACostCenter;
        private static bool _EightToEight;
        private static bool _RecoveryAnalysisNotMandatory;
        private static bool _JKManualWeight;
        private static bool _IsSalaryConnect;
        private static bool _EmployeeCodeForConnect;
        private static bool _LicenseCheck;
        private static bool _ShiftForAsadRice;
        private static bool _DirectApproval;
        private static bool _UtilityProcess;
        private static bool _GrowerBusinessPartner;
        private static bool _MillLeakageBusinessPartner;
        private static bool _IsLoandeductionFrombonus;
        private static bool _DemandSeasonRequisition;
        private static bool _IsStockLocationRackWise;
        private static bool _IsUpdatePayrollFromEmpInfo;
        private static bool _AlnoorAttendaceAndOvertime;
        private static bool _HabibOTForAdditionalPayment;
        private static bool _HouseRentUseInFormulaForDKSM;
        private static bool _IsAllowEditDescription;
        private static bool _Header;
        private static string _CompanyParameter;
        private static bool _IsAllowDecimalOnPresentdays;
        private static bool _AlnoorArrearWork;
        private static bool _IsAllowPartialAdvance;
        private static bool _MergeTaxes;
        private static bool _IsSandwich;
        private static bool _CustomerPartyOnDispatch;
        private static string _FirstSalesSecondWeight_1;
        private static string _SecondSalesSecondWeight_1;
        private static string _ThirdSalesSecondWeight_1;
        private static bool _SalesSecondWeightPrint;
        private static bool _IsSakrandSugarMill;
        private static bool _IsHideETD;
        private static bool _IsHideETA;
        private static bool _IsHidePaymentMethod;
        private static bool _IsHideRFQ;
        private static bool _POPaymentSchedule;
        private static bool _DKEOBIReport;
        private static bool _IsDKSM;
        private static bool _TokenSecondCopy;
        private static bool _SKUDOExtraField;
        private static bool _ChangeItemOnDispatch;
        private static bool _remarksondispatch;
        private static bool _MandatoryIncurredOnGIN;
        private static bool _InvoiceAdjustbyDirectPayment;
        private static bool _MandatoryIncuredOnRequisition;
        private static bool _IsSOEditAfterApprove;
        private static int _SOAllowApproveDays;
        private static bool _SOCostCenter;
        private static bool _UnlimitedAmountForSSML;
        private static bool _SeparateAmountInGD;
        private static bool _isApprovalHistoryPopup;
        private static bool _IsConnect;
        private static bool _IsSanghar;
        private static bool _SWVAlidateRemoveForRA;
        private static string _ExcessDays;
        private static bool _IsHideDataAfterDispatch;
        private static bool _IsMultipleDispatch;
        private static bool _TPTRateonNetWeight;
        private static bool _GlClosingCostCenterWise;
        private static bool _IsOverTimeUpload;
        private static bool _IsBiltyTPTRate;
        private static bool _MergeTransactionForBusinessPartnerGLDetail;
        private static bool _IsRaniPurSugarMill;
        private static bool _IsTrashPercentageOnToken;
        private static bool _DriverInfoInBiltyTPTPayment;
        private static bool _AmountToBeRoundOnDIPayment;
        private static bool _DONoOnGatePass;
        private static bool _IsTrashPercentageOnSecondWeight;
        private static bool _DispatchValidateFromCustomerParty;
        private static bool _BiltyTPTAmountShouldNotRound;
        private static int _Tandem;
        private static bool _OverTimeCalculationByFormula;
        private static bool _HaulageEnableOnToken;

        private static bool _AgentWiseCustomerValidate;
        private static bool _AllowAllBankAccountsinScrollPayment;
        private static bool _PaymentReceiptReport;
        private static bool _CashPaymentScrollWithTokenInfo;
        private static bool _IsDirectPOInvisible;
        private static int _MaxPoolSize;
        private static bool _OTCalcOnBasicByFormula;
        private static bool _IsMultiCurrencyAllowOnPO;
        private static bool _ImplicationofSRO;
        private static bool _isLoanGuarantor;
        private static bool _UniqueGuarantor;
        private static bool _EmployeeInsertForPF;
        private static bool _ScrollPaymentUserAuthorityWise;
        private static bool _StopGLImpactOnLoanAdjustment;
        private static bool _WeightStableWithOutRoadBlocker;
        private static bool _CombineWeightWithOutRoadBlocker;
        private static decimal _SurchargeOnIncomeTax;
        private static bool _AutoDefaultBank;
        private static bool _PayeeNameOnEmployeePayment;
        private static bool _DigitalInvoicing;
        private static bool _isFiller;
        private static bool _IsTSM;
        private static bool _BankBranchinDocumentType;
        private static bool _IsVehicleInCapsAndDropdownBydefault;
        private static bool _IsShowYardLineNo;
        private static bool _IsShowweighbridgeNumber;

        private static bool _printtokenfromexe;
        private static bool _printcprfromexe;
        private static bool _SetCenterCodeLength;
        private static bool _SalesTaxAmountCalAdam;
        private static string _sroScheduleNo;
        private static string _sroItemSerialNo;
        private static bool _IsTSMLCane;
        private static bool _IsGrossWithAllTypesForArrears;
        private static bool _IsInvoicePostWithBuyerName;
        private static bool _ArearAmountInGross;

        #endregion

        #region Static Properties

        public static string NameForCaneModule
        {

            get { return _NameForCaneModule; }
        }

        public static int SODispatchDateDays
        {
            get { return _SODispatchDateDays; }
        }

        public static bool ShipToDeliveryOrder
        {
            get { return _ShipToDeliveryOrder; }
        }

        public static bool AutoCalculateInBags
        {
            get { return _AutoCalculateInBags; }
        }

        public static bool LotNo
        {
            get { return _LotNo; }
        }

        public static bool IsWelcomeScreen
        {
            get { return _IsWelcomeScreen; }
        }

        public static bool AssignPreviousPeriod
        {
            get { return _AssignPreviousPeriod; }
        }

        public static bool MultiBusiness
        {
            get { return _MultiBusiness; }
        }

        public static bool MultiCurrency
        {
            get { return _MultiCurrency; }
        }

        public static int AssignCompanyID
        {
            get { return _AssignCompanyID; }
        }

        public static int AssignPeriodID
        {
            get { return _AssignPeriodID; }
        }

        public static int NoOfSegments
        {
            get { return _NoOfSegments; }
        }

        public static int MaxDOQty
        {
            get { return _MaxDOQty; }
        }

        public static int SegmentSize0
        {
            get { return _SegmentSize0; }
        }

        public static int SegmentSize1
        {
            get { return _SegmentSize1; }
        }

        public static int SegmentSize2
        {
            get { return _SegmentSize2; }
        }

        public static int SegmentSize3
        {
            get { return _SegmentSize3; }
        }

        public static int SegmentSize4
        {
            get { return _SegmentSize4; }
        }

        public static int SegmentSize5
        {
            get { return _SegmentSize5; }
        }

        public static int SegmentSize6
        {
            get { return _SegmentSize6; }
        }

        public static int SegmentSize7
        {
            get { return _SegmentSize7; }
        }

        public static int SegmentSize8
        {
            get { return _SegmentSize8; }
        }

        public static int SegmentSize9
        {
            get { return _SegmentSize9; }
        }

        public static int BaseCurrencyID
        {
            get { return _BaseCurrencyID; }
        }

        public static string Separator
        {
            get { return _Separator; }
        }

        public static bool DefaultAccountSegmentation
        {
            get { return _DefaultAccountSegmentation; }
        }

        public static bool ManualGrowerCode
        {
            get { return _IsManualGrowerCode; }
        }

        public static bool IsBarcodeDisplay
        {
            get { return _IsBarcodeDisplay; }
        }

        public static int IsAutoCPRNo
        {
            get { return _IsAutoCPRNo; }
        }

        public static int IsAutoTokenNo
        {
            get { return _IsAutoTokenNo; }
        }

        public static int LabName
        {
            get { return _LabName; }
        }

        public static string CPRLanguage
        {
            get { return _CPRLanguage; }
        }

        public static string CPRCopyName_1
        {
            get { return _CPRCopyName_1; }
        }

        public static string CPRCopyName_2
        {
            get { return _CPRCopyName_2; }
        }

        public static string CPRCopyName_3
        {
            get { return _CPRCopyName_3; }
        }

        public static string CPRCopyName_4
        {
            get { return _CPRCopyName_4; }
        }

        public static string CPRCopyName_5
        {
            get { return _CPRCopyName_5; }
        }

        public static string LoanDedcution_Cane
        {
            get { return _LoanDedcution; }
        }

        public static bool CPRHoldForRate
        {
            get { return _CPRHoldForRate; }
        }

        public static bool PrintCPR
        {
            get { return _PrintCPR; }
        }

        public static bool ChangeVarietyOnFW
        {
            get { return _ChangeVarietyOnFW; }
        }

        public static bool IsUserDefinedSlips
        {
            get { return _IsUserDefinedSlips; }
        }

        public static string TokenCopyName_1
        {
            get { return _TokenCopyName_1; }
        }

        public static string TokenCopyName_2
        {
            get { return _TokenCopyName_2; }
        }

        public static string TokenCopyName_3
        {
            get { return _TokenCopyName_3; }
        }

        public static string TokenCopyName_4
        {
            get { return _TokenCopyName_4; }
        }

        public static string TokenCopyName_5
        {
            get { return _TokenCopyName_5; }
        }

        public static bool AllGrowersOnTokenWhenManualIndent
        {
            get { return _AllGrowersOnTokenWhenManualIndent; }
        }

        public static bool BindingRounding
        {
            get { return _BindingRounding; }
        }

        public static bool TokenContractor
        {
            get { return _TokenContractor; }
        }

        public static bool FwImageCapture
        {
            get { return _FwImageCapture; }
        }

        public static string CPRWaterMark_1
        {
            get { return _CPRWaterMark_1; }
        }

        public static string CPRWaterMark_2
        {
            get { return _CPRWaterMark_2; }
        }

        public static string CPRWaterMark_3
        {
            get { return _CPRWaterMark_3; }
        }

        public static string CPRWaterMark_4
        {
            get { return _CPRWaterMark_4; }
        }

        public static string CPRWaterMark_5
        {
            get { return _CPRWaterMark_5; }
        }

        public static bool DashboardDisplay
        {
            get { return _DashboardDisplay; }
        }

        public static bool IsNotification
        {
            get { return _IsNotification; }
        }

        public static bool IsAlert
        {
            get { return _IsAlert; }
        }

        public static bool IsTextile
        {
            get { return _IsTextile; }
        }

        public static bool CategoryWiseSecurity
        {
            get { return _CategoryWiseSecurity; }
        }

        public static bool IsManualGatePassNo
        {
            get { return _IsManualGatePassNo; }
        }

        public static bool IsBatchWiseSale
        {
            get { return _IsBatchWiseSale; }
        }

        public static bool VoucherLocation
        {
            get { return _VoucherLocation; }
        }

        public static bool PaymentTerms
        {
            get { return _PaymentTerms; }
        }

        public static bool VehicleInformation
        {
            get { return _VehicleInformation; }
        }

        public static bool IsColor
        {
            get { return _IsColor; }
        }

        public static bool SOPaymentDisplayNone
        {
            get { return _SOPaymentDisplayNone; }
        }

        public static int SeasonDays
        {
            get { return _SeasonDays; }
        }
        public static int Tandem
        {
            get { return _Tandem; }
        }
        public static int QuotaProduction
        {
            get { return _QuotaProduction; }
        }

        public static int QuotaPermit
        {
            get { return _QuotaPermit; }
        }
        public static bool SecurityOnDocumentType
        {
            get { return _SecurityOnDocumentType; }
        }

        public static bool DispatchWeightNotVisible
        {
            get { return _DispatchWeightNotVisible; }
        }

        public static bool DeliveryOrderRegisterSheikhoo
        {
            get { return _DeliveryOrderRegisterSheikhoo; }
        }
        public static bool ReceiptSheikhoo
        {
            get { return _ReceiptSheikhoo; }
        }

        public static bool ExpenseCostCenterRequired
        {
            get { return _ExpenseCostCenterRequired; }
        }

        public static bool ItemVehicleField
        {
            get { return _ItemVehicleField; }
        }

        public static bool GINSupplierRequired
        {
            get { return _GINSupplierRequired; }
        }

        public static bool IsBlendFormulation
        {
            get { return _IsBlendFormulation; }
        }

        public static bool SignatureForReport
        {
            get { return _SignatureForReport; }
        }

        public static bool MultipleLoginBlocking
        {
            get { return _MultipleLoginBlocking; }
        }

        public static bool IsVehicle
        {
            get { return _IsVehicle; }
        }

        public static bool IsGrossChangedToBasic
        {
            get { return _IsGrossChangedToBasic; }
        }

        public static bool IncometaxForAlnoor
        {
            get { return _IncometaxForAlnoor; }
        }

        public static bool AKTSugar
        {
            get { return _AKTSugar; }
        }

        public static bool BusinessPartnerOnDispatch
        {
            get { return _BusinessPartnerOnDispatch; }
        }

        public static decimal SimilarWeightDifference
        {
            get { return _SimilarWeightDifference; }
        }

        public static int SimilarWeightCount
        {
            get { return _SimilarWeightCount; }
        }

        public static string IssuanceFlow
        {
            get { return _IssuanceFlow == null ? string.Empty : _IssuanceFlow; }
        }

        public static bool IsDisplayCostAtGRN
        {
            get { return _IsDisplayCostAtGRN; }
        }

        public static bool SalesInvoiceAutoGenerate
        {
            get { return _SalesInvoiceAutoGenerate; }
        }

        /// <summary>
        /// true = Requisition -> GIN -> RFQ
        /// false = Requisition -> RFQ
        /// </summary>
        public static bool GINCommentsRequired
        {
            get { return _GINCommentsRequired; }
        }

        public static bool InvisibleRequisitionInRFQ
        {
            get { return _InvisibleRequisitionInRFQ; }
        }

        public static bool CustomizeDispatch
        {
            get { return _CustomizeDispatch; }
        }

        public static bool SaimaProductLine
        {
            get { return _SaimaProductLine; }
        }

        public static bool IsMultipleDO
        {
            get { return _IsMultipleDO; }
        }

        public static bool IsDirectDispatch
        {
            get { return _IsDirectDispatch; }
        }

        public static bool LinkGrowerWithCircleAndCenter
        {
            get { return _LinkGrowerWithCircleAndCenter; }
        }

        public static int CenterCodeLength
        {
            get { return _CenterCodeLength; }
        }

        public static int CircleCodeLength
        {
            get { return _CircleCodeLength; }
        }

        public static bool CostofSalesAccount
        {
            get { return _CostofSalesAccount; }
        }

        public static bool InventoryAccountNew
        {
            get { return _InventoryAccountNew; }
        }

        public static bool POScheduling
        {
            get { return _POScheduling; }
        }

        public static bool SOScheduling
        {
            get { return _SOScheduling; }
        }

        public static bool DisplayStoppedCPR
        {
            get { return _DisplayStoppedCPR; }
        }

        public static bool IsFixedAsset
        {
            get { return _IsFixedAsset; }
        }
        public static bool IsWeightRestrict
        {
            get { return _IsWeightRestrict; }
        }

        public static bool IsLocationRackRequired
        {
            get { return _IsLocationRackRequired; }
        }

        public static bool PaymentTaxes
        {
            get { return _PaymentTaxes; }
        }

        public static bool IsSugar
        {
            get { return _IsSugar; }
        }

        public static bool MultipleGrowerAgainstCNIC
        {
            get { return _MultipleGrowerAgainstCNIC; }
        }

        public static bool ShowManualDeductionOnFirstWeight
        {
            get { return _ShowManualDeductionOnFirstWeight; }
        }

        public static bool ContractorMustSelectOnHaulage
        {
            get { return _ContractorMustSelectOnHaulage; }
        }

        public static bool IsLocationRackRequiredNormal
        {
            get { return _IsLocationRackRequiredNormal; }
        }

        public static bool AgentDealerRequired
        {
            get { return _AgentDealerRequired; }
        }

        public static bool CNICNotRequired
        {
            get { return _CNICNotRequired; }
        }

        public static bool DriverInfoRequired
        {
            get { return _DriverInfoRequired; }
        }

        public static bool DocumentTypeRequired
        {
            get { return _DocumentTypeRequired; }
        }

        public static bool IsDepartmentOnSO
        {
            get { return _IsDepartmentOnSO; }
        }

        public static bool IsCenterRequiredUponRequisition
        {
            get { return _IsCenterRequiredUponRequisition; }
        }

        public static bool RFQQuantityRequired
        {
            get { return _RFQQuantityRequired; }
        }

        public static bool RequiredPOinPaymentVoucher
        {
            get { return _RequiredPOinPaymentVoucher; }
        }

        public static bool NotRequiredVehicleTypenNo
        {
            get { return _NotRequiredVehicleTypenNo; }
        }

        public static bool NotRequiredVehicleType_No
        {
            get { return _NotRequiredVehicleType_No; }
        }

        public static bool FilterPOByAdvancePercentage
        {
            get { return _FilterPOByAdvancePercentage; }
        }

        public static bool FilterLocationByWareHouse
        {
            get { return _FilterLocationByWareHouse; }
        }

        public static short PrintSlipNo
        {
            get { return _PrintSlipNo; }
        }

        public static bool DirectPO
        {
            get { return _IsDirectPO; }
        }

        public static bool OfflineWeighment
        {
            get { return _OfflineWeighment; }
        }
        public static bool Minimumweightvalidationonsecondweight
        {
            get { return _Minimumweightvalidationonsecondweight; }
        }

        public static bool ManualDateTime
        {
            get { return _ManualDateTime; }
        }
        /// <summary>
        /// First Weight will be reduced automatically by this mentioned percentage.
        /// </summary>
        public static decimal FWDeductionPercentage
        {
            get { return _FWDeductionPercentage; }
            set { _FWDeductionPercentage = value; }
        }

        public static bool BrokerIsCustomer
        {
            get { return _BrokerIsCustomer; }
        }

        public static int IndentSupplyDateAdd
        {
            get { return _IndentSupplyDateAdd; }
        }

        public static int LeakageSupplyDateAdd
        {
            get { return _LeakageSupplyDateAdd; }
        }

        public static int ShiftHours
        {
            get { return _ShiftHours; }
        }

        public static double UnregisteredCustomerMonthlyLimit
        {
            get { return _UnregisteredCustomerMonthlyLimit; }
        }

        public static double UnregisteredCustomerYearlyLimit
        {
            get { return _UnregisteredCustomerYearlyLimit; }
        }

        public static bool EditableRFQQuantity
        {
            get { return _EditableRFQQuantity; }
        }

        public static bool InvisibleEstimatedPOPrice
        {
            get { return _InvisibleEstimatedPOPrice; }
        }

        public static bool TrashOnNetWeight
        {
            get { return _TrashOnNetWeight; }
        }

        public static short DOvalidUpToDays
        {
            get { return _DOvalidUpToDays; }
        }

        public static bool ShowDispatchReportLink
        {
            get { return _ShowDispatchReportLink; }
        }

        public static bool IsPaymentReportView
        {
            get { return _IsPaymentReportView; }
        }

        public static bool MultipleBanksInTransaction
        {
            get { return _MultipleBanksInTransaction; }
        }

        public static string ELPWebServiceURL
        {
            get { return _ELPWebServiceURL; }
        }

        public static bool ShowELPData
        {
            get { return _ShowELPData; }
        }

        public static string SecondWeightCopyName_1
        {
            get { return _SecondWeightCopyName_1; }
        }

        public static string SecondWeightCopyName_2
        {
            get { return _SecondWeightCopyName_2; }
        }

        public static string SecondWeightCopyName_3
        {
            get { return _SecondWeightCopyName_3; }
        }

        public static string SecondWeightCopyName_4
        {
            get { return _SecondWeightCopyName_4; }
        }

        public static string SecondWeightCopyName_5
        {
            get { return _SecondWeightCopyName_5; }
        }

        public static string FirstWeightCopyName_1
        {
            get { return _FirstWeightCopyName_1; }
        }

        public static string FirstWeightCopyName_2
        {
            get { return _FirstWeightCopyName_2; }
        }

        public static string FirstWeightCopyName_3
        {
            get { return _FirstWeightCopyName_3; }
        }

        public static string FirstWeightCopyName_4
        {
            get { return _FirstWeightCopyName_4; }
        }

        public static string FirstWeightCopyName_5
        {
            get { return _FirstWeightCopyName_5; }
        }

        public static string DepotTPTPaymentGL
        {
            get { return _DepotTPTPaymentGL; }
        }

        public static Nullable<DateTime> LastInterestProcessDate
        {
            get { return _LastInterestProcessDate; }
            set { _LastInterestProcessDate = value; }
        }

        public static string ELPProxyAddress
        {
            get { return _ELPProxyAddress; }
        }

        public static string ELPProxyUserID
        {
            get { return _ELPProxyUserID; }
        }

        public static string ELPProxyPassword
        {
            get { return _ELPProxyPassword; }
        }

        public static bool ELPByPassProxy
        {
            get { return _ELPByPassProxy; }
        }

        public static bool RoadBlocker
        {
            get { return _RoadBlocker; }
        }

        public static bool WeightStable
        {
            get { return _WeightStable; }
        }


        public static short WeightBlockerVariance
        {
            get { return _WeightBlockerVariance; }
        }

        public static short MinimumVehicalWeight
        {
            get { return _MinimumVehicalWeight; }
        }

        public static bool IsSlipCompanyLabel
        {
            get { return _IsSlipCompanyLabel; }
        }

        public static bool LinkGrowerWithIndent
        {
            get { return _LinkGrowerWithIndent; }
        }

        public static bool CarriageContractorPayableVoucher
        {
            get { return _CarriageContractorPayableVoucher; }
        }

        public static bool IsPRNCosting
        {
            get { return _IsPRNCosting; }
        }

        public static bool IsPartialDispatch
        {
            get { return _IsPartialDispatch; }
        }

        public static bool GrowerGroupRecovery
        {
            get { return _GrowerGroupRecovery; }
        }

        public static bool IsInvisibleUponStockReports
        {
            get { return _IsInvisibleUponStockReports; }
        }

        public static bool IsReceiptAgainstSO
        {
            get { return _IsReceiptAgainstSO; }
        }

        public static bool IsSOFindMode
        {
            get { return _IsSOFindMode; }
        }

        public static bool IsDispatchStockLedgerHideCancel
        {
            get { return _IsDispatchStockLedgerHideCancel; }
        }

        public static bool ZeroAllowed
        {
            get { return _ZeroAllowed; }
        }

        public static bool SalesDateValidation
        {
            get { return _SalesDateValidation; }
        }

        public static bool RFQItemSelection
        {
            get { return _RFQItemSelection; }
        }

        public static bool IsPaymentTax
        {
            get { return _IsPaymentTax; }
        }

        public static bool DuplicateManualIndentSeries
        {
            get { return _DuplicateManualIndentSeries; }
        }

        public static bool DisableContractorByCP
        {
            get { return _DisableContractorByCP; }
        }

        public static bool TMSUploadAccess
        {
            get { return _TMSUploadAccess; }
        }

        public static bool HideListPriceOnRequisition
        {
            get { return _HideListPriceOnRequisition; }
        }

        public static bool SupplierCreditOnPayment
        {
            get { return _SupplierCreditOnPayment; }
        }

        public static bool StopLoanDeduction
        {
            get { return _StopLoanDeduction; }
        }

        public static bool ShowQualityOnSO
        {
            get { return _ShowQualityOnSO; }
        }

        public static bool TaxPercentageInAccountsGrid
        {
            get { return _TaxPercentageInAccountsGrid; }
        }

        public static bool SalaryProcessbyTMS
        {
            get { return _SalaryProcessbyTMS; }
        }

        public static bool SalaryProcessForAsadRice
        {
            get { return _SalaryProcessForAsadRice; }
        }

        public static bool LateDeductionPolicy
        {
            get { return _LateDeductionPolicy; }
        }

        public static bool IsEmployeeCodeEditable
        {
            get { return _IsEmployeeCodeEditable; }
        }

        public static bool RoasterForCIS
        {
            get { return _RoasterForCIS; }
        }

        public static bool MultipleBusinessPartners
        {
            get { return _MultipleBusinessPartners; }
        }

        public static bool POMultiRequisition
        {
            get { return _POMultiRequisition; }
        }

        public static bool IsOrient
        {
            get { return _IsOrient; }
        }

        public static bool DisplayOtherUserRecords
        {
            get { return _DisplayOtherUserRecords; }
        }

        public static bool MandatorySOQualityBatch
        {
            get { return _MandatorySOQualityBatch; }
        }

        public static bool RestrictZeroQtyOnGIN
        {
            get { return _RestrictZeroQtyOnGIN; }
        }

        /// <summary>
        /// Option to enter weight manually with Manual Gate Pass No upon Dispatch form
        /// </summary>
        public static bool ManualWeight
        {
            get { return _ManualWeight; }
        }

        public static bool ShowReportOnSave
        {
            get { return _ShowReportOnSave; }
        }

        public static int[] BusinessPartnerIDsRange
        {
            get { return _BusinessPartnerIDsRange; }
            set { _BusinessPartnerIDsRange = value; }
        }

        public static bool IsSynchronize
        {
            get { return _IsSynchronize; }
        }

        public static bool EditAllRecords
        {
            get { return _EditAllRecords; }
        }

        public static bool DefineFAProcessStarDate
        {
            get { return _DefineFAProcessStarDate; }
        }

        public static bool SetItemCodeByColor
        {
            get { return _SetItemCodeByColor; }
        }

        public static bool BandhiSalarySheet
        {
            get { return _BandhiSalarySheet; }
        }

        public static bool GrossSalaryTotal
        {
            get { return _GrossSalaryTotal; }
        }

        public static bool SalesInvoiceForPolymer
        {
            get { return _SalesInvoiceForPolymer; }
        }

        public static bool DeliveryOrderForPolymer
        {
            get { return _DeliveryOrderForPolymer; }
        }


        public static bool DispatchNoteForPolymer
        {
            get { return _DispatchNoteForPolymer; }
        }

        public static bool SalesOrderForPolymer
        {
            get { return _SalesOrderForPolymer; }
        }

        public static bool DIInvoiceForPolymer
        {
            get { return _DIInvoiceForPolymer; }
        }

        public static bool CompanyNameWithoutOperatingUnit
        {
            get { return _CompanyNameWithoutOperatingUnit; }
        }

        public static Nullable<int> WeightDiffAgainstVehicle
        {
            get { return _WeightDiffAgainstVehicle; }
        }

        public static bool IsHideTrashOnSecondWeightAutoSlip
        {
            get { return _IsHideTrashOnSecondWeightAutoSlip; }
        }
        public static bool VarietyHideOnSlips
        {
            get { return _VarietyHideOnSlips; }
        }
        public static bool DeductionOnWeightPayable
        {
            get { return _DeductionOnWeightPayable; }
        }
        public static bool TrashDeductionFromGrossWeight
        {
            get { return _TrashDeductionFromGrossWeight; }
        }
        public static decimal ToDateRecovery
        {
            get { return _ToDateRecovery; }
            set { _ToDateRecovery = value; }
        }
        public static decimal SurchargeOnIncomeTax
        {
            get { return _SurchargeOnIncomeTax; }
            set { _SurchargeOnIncomeTax = value; }
        }
        public static bool StopPrintReportOnFirstWeight
        {
            get { return _StopPrintReportOnFirstWeight; }
            //get { return _PrintReportOnFirstWeight == null ? true : _PrintReportOnFirstWeight; }
        }
        public static bool PrintReportOnSecondWeight
        {
            get { return _PrintReportOnSecondWeight; }
        }
        public static bool MaxSupplyValidationOnToken
        {
            get { return _MaxSupplyValidationOnToken; }
        }
        public static bool IsShowVariety
        {
            get { return _IsShowVariety; }
        }

        public static bool IsShowAreaName
        {
            get { return _IsShowAreaName; }
        }

        public static bool IsGrowerReference
        {
            get { return isGrowerReference; }
        }

        public static bool IsShowSurveyID
        {
            get { return _IsShowSurveyID; }
        }
        public static bool BandhiAutoPrint
        {
            get { return _BandhiAutoPrint; }
        }
        public static int IndentExpiredInNoOfDays
        {
            get { return _IndentExpiredInNoOfDays; }
        }
        public static bool TransferOutGL
        {
            get { return _TransferOutGL; }
        }
        public static bool StopAllDeduction
        {
            get { return _StopAllDeduction; }
        }
        public static bool ValidateSupplyDateIndent
        {
            get { return _ValidateSupplyDateIndent; }
        }
        public static bool DeliveryScheduleForPolymer
        {
            get { return _DeliveryScheduleForPolymer; }
        }

        public static bool OverTimeCalculationForBandhi
        {
            get { return _OverTimeCalculationForBandhi; }
        }

        public static bool OverTimeCalculationForMBI
        {
            get { return _OverTimeCalculationForMBI; }
        }

        public static bool AttendanceUploadForAdam
        {
            get { return _AttendanceUploadForAdam; }
        }

        public static bool LoadMonthlyDocuments
        {
            get { return _LoadMonthlyDocuments; }
        }

        public static bool CommissionOnReceipt
        {
            get { return _CommissionOnReceipt; }
        }

        public static bool RFQDueDateDiscard
        {
            get { return _RFQDueDateDiscard; }
        }

        public static bool isGINWithoutGLCode
        {
            get { return _isGINWithoutGLCode; }
        }

        public static bool IsGenerateReport
        {
            get { return _IsGenerateReport; }
        }

        public static bool IsAutoPrinting
        {
            get { return _IsAutoPrinting; }
        }

        public static bool IsUpdatePayrollFromEmpInfo
        {
            get { return _IsUpdatePayrollFromEmpInfo; }
        }

        public static bool AlnoorAttendaceAndOvertime
        {
            get { return _AlnoorAttendaceAndOvertime; }
        }

        public static bool HabibOTForAdditionalPayment
        {
            get { return _HabibOTForAdditionalPayment; }
        }

        public static bool AlnoorArrearWork
        {
            get { return _AlnoorArrearWork; }
        }

        public static bool HouseRentUseInFormulaForDKSM
        {
            get { return _HouseRentUseInFormulaForDKSM; }
        }

        public static bool OverTimeCalculationForAlAbbas
        {
            get { return _OverTimeCalculationForAlAbbas; }
        }

        public static bool LoanAmountTypeWise
        {
            get { return _LoanAmountTypeWise; }
        }

        public static bool OverTimeCalculationForSheikhoo
        {
            get { return _OverTimeCalculationForSheikhoo; }
        }

        public static bool IsFabricAlImdad
        {
            get { return _IsFabricAlImdad; }
        }

        /// <summary>
        /// Show Expense Accounts grid for (Payment Against Invoices) document type
        /// </summary>
        public static bool ShowExpenseAccountGrid
        {
            get { return _ShowExpenseAccountGrid; }
        }

        public static bool ValidateBilltyNoOnDispatch
        {
            get { return _ValidateBilltyNoOnDispatch; }
        }

        public static bool CPRVouchers
        {
            get { return _CPRVouchers; }
        }
        public static bool trashdeductionOntoken
        {
            get { return _trashdeductionOntoken; }
        }

        /// <summary>
        /// Post Carriage Contractor Payment Voucher. By default, it is set to true.
        /// </summary>
        public static bool CarConPaymentVoucher
        {
            get { return _CarConPaymentVoucher; }
        }

        public static bool ItemParts
        {
            get { return _ItemParts; }
        }

        public static bool FGPostGL
        {
            get { return _FGPostGL; }
        }

        public static bool ContractorManualRate
        {
            get { return _ContractorManualRate; }
        }

        public static bool HideWeightDedInDashboard
        {
            get { return _HideWeightDedInDashboard; }
        }

        public static bool CopyButtonForVoucher
        {
            get { return _CopyButtonForVoucher; }
        }

        public static bool IsProfitAndLoss
        {
            get { return isProfitAndLoss; }
        }

        public static bool AllowSelectedDocumentTypes
        {
            get { return _AllowSelectedDocumentTypes; }
        }

        public static bool AllowInprocessOption
        {
            get { return _AllowInprocessOption; }
        }
        public static bool StopGLStockTransfer
        {
            get { return _StopGLStockTransfer; }
        }


        public static bool IsFiscalSystem
        {
            get { return isFiscalSystem; }
        }

        public static bool IsStockFIFO
        {
            get { return isStockFIFO; }
        }


        public static bool IsLocationHide
        {
            get { return isLocationHide; }
        }

        public static bool IsNOCOGSEntry
        {
            get { return isNOCOGSEntry; }
        }

        public static bool SalaryProcessWithoutLeaves
        {
            get { return _SalaryProcessWithoutLeaves; }
        }

        public static bool UserWiseDocumentAppear
        {
            get { return _UserWiseDocumentAppear; }
        }

        public static bool AutoGenerateBarCode
        {
            get { return _AutoGenerateBarCode; }
        }

        public static string FabricDepartment
        {
            get { return _FabricDepartment; }
        }
        public static string FabricCostCenter
        {
            get { return _FabricCostCenter; }
        }
        public static string FinishingCostCenter
        {
            get { return _FinishingCostCenter; }
        }
        public static string FabricLocation
        {
            get { return _FabricLocation; }
        }

        public static string SROPercent
        {
            get { return _SROPercent; }
        }
        public static string SROZeroPercent
        {
            get { return _SROZeroPercent; }
        }
        public static bool SROInvoices
        {
            get { return _SROInvoices; }
        }
        public static bool AllowDetailNarration
        {
            get { return _AllowDetailNarration; }
        }
        public static bool LeavePolicy
        {
            get { return _LeavePolicy; }
        }
        public static bool IsDisallowBackDate
        {
            get { return _IsDisallowBackDate; }
        }
        public static bool IsGRNDisallowBackDate
        {
            get { return _IsGRNDisallowBackDate; }
        }
        public static string LogisticVendorDefaultCOA
        {
            get { return _LogisticVendorDefaultCOA; }
        }
        public static string LoadingUnloadingDefaultCOA
        {
            get { return _LoadingUnloadingDefaultCOA; }
        }
        public static string ClearingDefaultCOA
        {
            get { return _ClearingDefaultCOA; }
        }

        public static string TransporterDefaultCOA
        {
            get { return _TransporterDefaultCOA; }
        }
        public static string VendorDefaultCOA
        {
            get { return _VendorDefaultCOA; }
        }
        public static bool TMSDataExcelFormatJKS
        {
            get { return _TMSDataExcelFormatJKS; }
        }
        public static string BGroupDefaultCOA
        {
            get { return _BGroupDefaultCOA; }
        }
        public static string HGroupDefaultCOA
        {
            get { return _HGroupDefaultCOA; }
        }
        public static string CommonGroupDefaultCOA
        {
            get { return _CommonGroupDefaultCOA; }
        }

        public static string CompanyNameParam
        {
            get { return _CompanyNameParam; }
        }

        public static int BranchCompany
        {
            get { return _BranchCompany; }
        }

        public static bool DocumentIDsForSite
        {
            get { return _DocumentIDsForSite; }
        }

        public static bool DispatchTo
        {
            get { return _DispatchTo; }
        }

        public static bool TaxCalculationOnContractDate
        {
            get { return _TaxCalculationOnContractDate; }
        }

        public static bool IsSheikhooReport
        {
            get { return _IsSheikhooReport; }
        }
        public static bool IsConnectReport
        {
            get { return _IsConnectReport; }
        }
        public static bool IsHabibSugar
        {
            get { return _IsHabibSugar; }
        }
        public static bool IsTwoStar
        {
            get { return _IsTwoStar; }
        }
        public static bool isLoanGuarantor
        {
            get { return _isLoanGuarantor; }
        }
        public static bool UniqueGuarantor
        {
            get { return _UniqueGuarantor; }
        }
        public static bool IsPatoki
        {
            get { return _IsPatoki; }
        }

        public static bool IsPrintsalesFirstWeight
        {
            get { return _IsPrintsalesFirstWeight; }
        }

        public static bool AllowInventoryBackDateEntry
        {
            get { return _AllowInventoryBackDateEntry; }
        }

        public static string SalesTaxImport
        {
            get { return _SalesTaxImport; }
        }

        public static string SalesTaxServices
        {
            get { return _SalesTaxServices; }
        }

        public static string AdvanceIncomeTaxImport
        {
            get { return _AdvanceIncomeTaxImport; }
        }

        public static bool ManualWeightSales
        {
            get { return _ManualWeightSales; }
        }

        public static bool ManualWeightInv
        {
            get { return _ManualWeightInv; }
        }

        public static bool ManualWeightSalesSteel
        {
            get { return _ManualWeightSalesSteel; }
        }

        public static bool DispatchReturnInTareWeight
        {
            get { return _DispatchReturnInTareWeight; }
        }


        public static bool FetchDataFromDOInGateIn
        {
            get { return _FetchDataFromDOInGateIn; }
        }

        public static bool SalesDocumentType
        {
            get { return _SalesDocumentType; }
        }

        public static bool CustomerNameII
        {
            get { return _CustomerNameII; }
        }

        public static bool SaleInvoiceProcess
        {
            get { return _SaleInvoiceProcess; }
        }
        public static bool IsAKTSEmpInfo
        {
            get { return _IsAKTSEmpInfo; }
        }
        public static bool HSMMultipleCustomerDOAtLoadingPoint
        {
            get { return _HSMMultipleCustomerDOAtLoadingPoint; }
        }

        public static bool LodingpointCustomerDispatch
        {
            get { return _LodingpointCustomerDispatch; }
        }

        public static bool IsAllowancesAddInGross
        {
            get { return _IsAllowancesAddInGross; }
        }

        public static bool IsOverTimePartofsalary
        {
            get { return _IsOverTimePartofsalary; }
        }

        public static bool IsFormuladisplayonotperhour
        {
            get { return _IsFormuladisplayonotperhour; }
        }


        public static bool AllowAllDoctypeRequisitionAssignment
        {
            get { return _AllowAllDoctypeRequisitionAssignment; }
        }

        public static bool HSMLoadingPointSlipHTML
        {
            get { return _HSMLoadingPointSlipHTML; }
        }
        public static bool JKLetterOfCreditPOLookUP
        {
            get { return _JKLetterOfCreditPOLookUP; }
        }


        public static string CompanyName
        {
            get { return _CompanyName; }
        }

        public static bool BonusForHabib
        {
            get { return _BonusForHabib; }
        }

        public static bool ShiftForAsadRice
        {
            get { return _ShiftForAsadRice; }
        }

        public static bool IsPaySlipConnect
        {
            get { return _IsPaySlipConnect; }
        }
        public static bool InvoiceRateSheikhoo
        {
            get { return _InvoiceRateSheikhoo; }
        }

        public static bool GateParameterForHabib
        {
            get { return _GateParameterForHabib; }
        }
        public static bool IsEmployeeCodeMandatory
        {
            get { return _IsEmployeeCodeMandatory; }
        }
        public static bool InOutTransTypeAndImpact
        {
            get { return _InOutTransTypeAndImpact; }
        }
        public static bool RackBatchExpiry
        {
            get { return _RackBatchExpiry; }
        }
        public static bool NACostCenter
        {
            get { return _NACostCenter; }
        }
        public static bool EightToEight
        {
            get { return _EightToEight; }
        }
        public static bool RecoveryAnalysisNotMandatory
        {
            get { return _RecoveryAnalysisNotMandatory; }
        }
        public static bool JKManualWeight
        {
            get { return _JKManualWeight; }
        }
        public static bool IsSalaryConnect
        {
            get { return _IsSalaryConnect; }
        }
        public static bool LicenseCheck
        {
            get { return _LicenseCheck; }
        }
        public static bool EmployeeCodeForConnect
        {
            get { return _EmployeeCodeForConnect; }
        }

        public static bool DirectApproval
        {
            get { return _DirectApproval; }
        }

        public static bool UtilityProcess
        {
            get { return _UtilityProcess; }
        }

        public static bool GrowerBusinessPartner
        {
            get { return _GrowerBusinessPartner; }
        }

        public static bool MillLeakageBusinessPartner
        {
            get { return _MillLeakageBusinessPartner; }
        }

        public static bool IsLoandeductionFrombonus
        {
            get { return _IsLoandeductionFrombonus; }
        }

        public static bool DemandSeasonRequisition
        {
            get { return _DemandSeasonRequisition; }
        }

        public static bool IsStockLocationRackWise
        {
            get { return _IsStockLocationRackWise; }
        }

        public static bool IsAllowEditDescription
        {
            get { return _IsAllowEditDescription; }
        }


        public static bool IsAllowDecimalOnPresentdays
        {
            get { return _IsAllowDecimalOnPresentdays; }
        }


        public static bool Header
        {
            get { return _Header; }
        }

        public static string CompanyParameter
        {
            get { return _CompanyParameter; }
        }

        public static bool IsAllowPartialAdvance
        {
            get { return _IsAllowPartialAdvance; }
        }

        public static bool MergeTaxes
        {
            get { return _MergeTaxes; }
        }

        public static bool IsSandwich
        {
            get { return _IsSandwich; }
        }

        public static bool CustomerPartyOnDispatch
        {
            get { return _CustomerPartyOnDispatch; }
        }

        public static string FirstSalesSecondWeight_1
        {
            get { return _FirstSalesSecondWeight_1; }
        }

        public static string SecondSalesSecondWeight_1
        {
            get { return _SecondSalesSecondWeight_1; }
        }

        public static string ThirdSalesSecondWeight_1
        {
            get { return _ThirdSalesSecondWeight_1; }
        }

        public static bool SalesSecondWeightPrint
        {
            get { return _SalesSecondWeightPrint; }
        }

        public static bool IsSakrandSugarMill
        {
            get { return _IsSakrandSugarMill; }
        }

        //import Document parameter
        public static bool IsHideETD
        {
            get { return _IsHideETD; }
        }
        public static bool IsHideETA
        {
            get { return _IsHideETA; }
        }
        public static bool IsHidePaymentMethod
        {
            get { return _IsHidePaymentMethod; }
        }
        /////////////////////


        // Purchase Contract Parameter
        public static bool IsHideRFQ
        {
            get { return _IsHideRFQ; }
        }

        public static bool POPaymentSchedule
        {
            get { return _POPaymentSchedule; }
        }

        public static bool DKEOBIReport
        {
            get { return _DKEOBIReport; }
        }

        public static bool IsDKSM
        {
            get { return _IsDKSM; }
        }

        public static bool TokenSecondCopy
        {
            get { return _TokenSecondCopy; }
        }

        public static bool SKUDOExtraField
        {
            get { return _SKUDOExtraField; }
        }

        public static bool ChangeItemOnDispatch
        {
            get { return _ChangeItemOnDispatch; }
        }

        public static bool remarksondispatch
        {
            get { return _remarksondispatch; }
        }

        public static bool MandatoryIncurredOnGIN
        {
            get { return _MandatoryIncurredOnGIN; }
        }

        public static bool InvoiceAdjustbyDirectPayment
        {
            get { return _InvoiceAdjustbyDirectPayment; }
        }

        public static bool MandatoryIncuredOnRequisition
        {
            get { return _MandatoryIncuredOnRequisition; }
        }

        public static bool IsSOEditAfterApprove
        {
            get { return _IsSOEditAfterApprove; }
        }

        public static int SOAllowApproveDays
        {
            get { return _SOAllowApproveDays; }
        }

        public static string ExcessDays
        {
            get { return _ExcessDays; }
        }
        public static bool SOCostCenter
        {
            get { return _SOCostCenter; }
        }

        public static bool UnlimitedAmountForSSML
        {
            get { return _UnlimitedAmountForSSML; }
        }

        public static bool SeparateAmountInGD
        {
            get { return _SeparateAmountInGD; }
        }
        public static bool isApprovalHistoryPopup
        {
            get { return _isApprovalHistoryPopup; }
        }
        public static bool IsConnect
        {
            get { return _IsConnect; }
        }
        public static bool IsSanghar
        {
            get { return _IsSanghar; }
        }
        public static bool SWVAlidateRemoveForRA
        {
            get { return _SWVAlidateRemoveForRA; }
        }

        public static bool IsHideDataAfterDispatch
        {
            get { return _IsHideDataAfterDispatch; }
        }

        public static bool IsMultipleDispatch
        {
            get { return _IsMultipleDispatch; }
        }

        public static bool TPTRateonNetWeight
        {
            get { return _TPTRateonNetWeight; }
        }

        public static bool GlClosingCostCenterWise
        {
            get { return _GlClosingCostCenterWise; }
        }

        public static bool IsOverTimeUpload
        {
            get { return _IsOverTimeUpload; }
        }

        public static bool IsBiltyTPTRate
        {
            get { return _IsBiltyTPTRate; }
        }

        public static bool MergeTransactionForBusinessPartnerGLDetail
        {
            get { return _MergeTransactionForBusinessPartnerGLDetail; }
        }

        public static bool IsRaniPurSugarMill
        {
            get { return _IsRaniPurSugarMill; }
        }

        public static bool IsTrashPercentageOnToken
        {
            get { return _IsTrashPercentageOnToken; }
        }

        public static bool DriverInfoInBiltyTPTPayment
        {
            get { return _DriverInfoInBiltyTPTPayment; }
        }

        public static bool AmountToBeRoundOnDIPayment
        {
            get { return _AmountToBeRoundOnDIPayment; }
        }

        public static bool DONoOnGatePass
        {
            get { return _DONoOnGatePass; }
        }

        public static bool IsTrashPercentageOnSecondWeight
        {
            get { return _IsTrashPercentageOnSecondWeight; }
        }

        public static bool DispatchValidateFromCustomerParty
        {
            get { return _DispatchValidateFromCustomerParty; }
        }

        public static bool BiltyTPTAmountShouldNotRound
        {
            get { return _BiltyTPTAmountShouldNotRound; }
        }

        public static bool OverTimeCalculationByFormula
        {
            get { return _OverTimeCalculationByFormula; }
        }

        public static bool HaulageEnableOnToken
        {
            get { return _HaulageEnableOnToken; }
        }

        public static bool AgentWiseCustomerValidate
        {
            get { return _AgentWiseCustomerValidate; }
        }

        public static bool AllowAllBankAccountsinScrollPayment
        {
            get { return _AllowAllBankAccountsinScrollPayment; }
        }

        public static bool PaymentReceiptReport
        {
            get { return _PaymentReceiptReport; }
        }

        public static bool CashPaymentScrollWithTokenInfo
        {
            get { return _CashPaymentScrollWithTokenInfo; }
        }

        public static bool IsDirectPOInvisible
        {
            get { return _IsDirectPOInvisible; }
        }

        public static int MaxPoolSize
        {
            get { return _MaxPoolSize; }
        }

        public static bool OTCalcOnBasicByFormula
        {
            get { return _OTCalcOnBasicByFormula; }
        }

        public static bool IsMultiCurrencyAllowOnPO
        {
            get { return _IsMultiCurrencyAllowOnPO; }
        }

        public static bool ImplicationofSRO
        {
            get { return _ImplicationofSRO; }
        }

        public static bool EmployeeInsertForPF
        {
            get { return _EmployeeInsertForPF; }
        }

        public static bool ScrollPaymentUserAuthorityWise
        {
            get { return _ScrollPaymentUserAuthorityWise; }
        }

        public static bool StopGLImpactOnLoanAdjustment
        {
            get { return _StopGLImpactOnLoanAdjustment; }
        }

        public static bool WeightStableWithOutRoadBlocker
        {
            get { return _WeightStableWithOutRoadBlocker; }
        }

        public static bool CombineWeightWithOutRoadBlocker
        {
            get { return _CombineWeightWithOutRoadBlocker; }
        }


        public static bool AutoDefaultBank
        {
            get { return _AutoDefaultBank; }
        }

        public static bool PayeeNameOnEmployeePayment
        {
            get { return _PayeeNameOnEmployeePayment; }
        }

        public static bool DigitalInvoicing
        {
            get { return _DigitalInvoicing; }
        }

        public static bool isFiller
        {
            get { return _isFiller; }
        }

        public static bool IsTSM
        {
            get { return _IsTSM; }
        }

        public static bool IsVehicleInCapsAndDropdownBydefault
        {
            get { return _IsVehicleInCapsAndDropdownBydefault; }
        }

        public static bool IsShowYardLineNo
        {
            get { return _IsShowYardLineNo; }
        }

        public static bool IsShowweighbridgeNumber
        {
            get { return _IsShowweighbridgeNumber; }
        }

        public static bool printtokenfromexe
        {
            get { return _printtokenfromexe; }
        }

        public static bool printcprfromexe
        {
            get { return _printcprfromexe; }
        }
        public static bool SetCenterCodeLength
        {
            get { return _SetCenterCodeLength; }
        }
        public static bool SalesTaxAmountCalAdam
        {
            get { return _SalesTaxAmountCalAdam; }
        }
        public static bool BankBranchinDocumentType
        {
            get { return _BankBranchinDocumentType; }
        }

        public static string sroScheduleNo
        {
            get { return _sroScheduleNo; }
        }

        public static bool IsTSMLCane
        {
            get { return _IsTSMLCane; }
        }

        public static string sroItemSerialNo
        {
            get { return _sroItemSerialNo; }
        }

        public static bool IsGrossWithAllTypesForArrears
        {
            get { return _IsGrossWithAllTypesForArrears; }
        }

        public static bool IsInvoicePostWithBuyerName
        {
            get { return _IsInvoicePostWithBuyerName; }
        }

        public static bool ArearAmountInGross
        {
            get { return _ArearAmountInGross; }
        }

        #endregion


        public static void SetApplicationVariables()
        {
            List<SET_Parameter> objList = SET_Parameter.GetAllDataList();

            if (objList == null)
                return;

            /*Set to true for all with out ref*/
            _IsPartialDispatch = true;
            _ZeroAllowed = true;

            foreach (SET_Parameter objParameter in objList)
            {
                switch (objParameter.FieldName)
                {
                    case "NameForCaneModule":
                        _NameForCaneModule = objParameter.FieldValue1.ToString();
                        break;

                    case "IsWelcomeScreen":
                        _IsWelcomeScreen = Convert.ToBoolean(objParameter.FieldValue1);
                        break;

                    case "nAssignCompanyID":
                        _AssignCompanyID = Convert.ToInt16(objParameter.FieldValue1);
                        break;

                    case "nAssignPeriodID":
                        _AssignPeriodID = Convert.ToInt16(objParameter.FieldValue1);
                        break;

                    case "IsAssignPreviousPeriod":
                        _AssignPreviousPeriod = Convert.ToBoolean(objParameter.FieldValue1);
                        break;

                    case "nNoOfSegments":
                        _NoOfSegments = Convert.ToInt16(objParameter.FieldValue1);
                        break;

                    case "MaxDOQty":
                        _MaxDOQty = Convert.ToInt16(objParameter.FieldValue1);
                        break;

                    case "nSegmentSize0":
                        _SegmentSize0 = Convert.ToInt16(objParameter.FieldValue1);
                        break;

                    case "nSegmentSize1":
                        _SegmentSize1 = Convert.ToInt16(objParameter.FieldValue1);
                        break;

                    case "nSegmentSize2":
                        _SegmentSize2 = Convert.ToInt16(objParameter.FieldValue1);
                        break;

                    case "nSegmentSize3":
                        _SegmentSize3 = Convert.ToInt16(objParameter.FieldValue1);
                        break;

                    case "nSegmentSize4":
                        _SegmentSize4 = Convert.ToInt16(objParameter.FieldValue1);
                        break;

                    case "nSegmentSize5":
                        _SegmentSize5 = Convert.ToInt16(objParameter.FieldValue1);
                        break;

                    case "nSegmentSize6":
                        _SegmentSize6 = Convert.ToInt16(objParameter.FieldValue1);
                        break;

                    case "nSegmentSize7":
                        _SegmentSize7 = Convert.ToInt16(objParameter.FieldValue1);
                        break;

                    case "nSegmentSize8":
                        _SegmentSize8 = Convert.ToInt16(objParameter.FieldValue1);
                        break;

                    case "nSegmentSize9":
                        _SegmentSize9 = Convert.ToInt16(objParameter.FieldValue1);
                        break;

                    case "IsMultiBusiness":
                        _MultiBusiness = Convert.ToBoolean(objParameter.FieldValue1);
                        break;

                    case "IsMultiCurrency":
                        _MultiCurrency = Convert.ToBoolean(objParameter.FieldValue1);
                        break;

                    case "nBaseCurrencyID":
                        _BaseCurrencyID = Convert.ToInt16(objParameter.FieldValue1);
                        break;

                    case "sSeparator":
                        _Separator = objParameter.FieldValue1;
                        break;

                    case "isDefaultAccountSegmentation":
                        bool.TryParse(objParameter.FieldValue1, out _DefaultAccountSegmentation);
                        break;

                    case "IsManualGrowerCode":
                        bool.TryParse(objParameter.FieldValue1, out _IsManualGrowerCode);
                        break;

                    case "IsBarcodeDisplay":
                        bool.TryParse(objParameter.FieldValue1, out _IsBarcodeDisplay);
                        break;

                    case "IsAutoCPRNo":
                        int.TryParse(objParameter.FieldValue1, out _IsAutoCPRNo);
                        break;

                    case "IsAutoTokenNo":
                        int.TryParse(objParameter.FieldValue1, out _IsAutoTokenNo);
                        break;

                    case "LabName":
                        int.TryParse(objParameter.FieldValue1, out _LabName);
                        break;

                    case "CPRLanguage":
                        _CPRLanguage = objParameter.FieldValue1;
                        break;

                    case "CPRCopyName_1":
                        _CPRCopyName_1 = objParameter.FieldValue1;
                        _CPRWaterMark_1 = objParameter.FieldValue2;
                        break;

                    case "CPRCopyName_2":
                        _CPRCopyName_2 = objParameter.FieldValue1;
                        _CPRWaterMark_2 = objParameter.FieldValue2;
                        break;

                    case "CPRCopyName_3":
                        _CPRCopyName_3 = objParameter.FieldValue1;
                        _CPRWaterMark_3 = objParameter.FieldValue2;
                        break;

                    case "CPRCopyName_4":
                        _CPRCopyName_4 = objParameter.FieldValue1;
                        _CPRWaterMark_4 = objParameter.FieldValue2;
                        break;

                    case "CPRCopyName_5":
                        _CPRCopyName_5 = objParameter.FieldValue1;
                        _CPRWaterMark_5 = objParameter.FieldValue2;
                        break;

                    case "LoanDedcution":
                        _LoanDedcution = objParameter.FieldValue1;
                        break;

                    case "CPRHoldForRate":
                        bool.TryParse(objParameter.FieldValue1, out _CPRHoldForRate);
                        break;

                    case "PrintCPR":
                        bool.TryParse(objParameter.FieldValue1, out _PrintCPR);
                        break;

                    case "ChangeVarietyOnFW":
                        bool.TryParse(objParameter.FieldValue1, out _ChangeVarietyOnFW);
                        break;

                    case "IsUserDefinedSlips":
                        bool.TryParse(objParameter.FieldValue1, out _IsUserDefinedSlips);
                        break;

                    case "TokenCopyName_1":
                        _TokenCopyName_1 = objParameter.FieldValue1;
                        break;

                    case "TokenCopyName_2":
                        _TokenCopyName_2 = objParameter.FieldValue1;
                        break;

                    case "TokenCopyName_3":
                        _TokenCopyName_3 = objParameter.FieldValue1;
                        break;

                    case "TokenCopyName_4":
                        _TokenCopyName_4 = objParameter.FieldValue1;
                        break;

                    case "TokenCopyName_5":
                        _TokenCopyName_5 = objParameter.FieldValue1;
                        break;

                    case "AllGrowersOnTokenWhenManualIndent":
                        bool.TryParse(objParameter.FieldValue1, out _AllGrowersOnTokenWhenManualIndent);
                        break;

                    case "BindingRounding":
                        bool.TryParse(objParameter.FieldValue1, out _BindingRounding);
                        break;

                    case "TokenContractor":
                        bool.TryParse(objParameter.FieldValue1, out _TokenContractor);
                        break;

                    case "FwImageCapture":
                        _FwImageCapture = Convert.ToBoolean(objParameter.FieldValue1);
                        break;

                    case "DashboardDisplay":
                        bool.TryParse(objParameter.FieldValue1, out _DashboardDisplay);
                        break;

                    case "IsNotification":
                        bool.TryParse(objParameter.FieldValue1, out _IsNotification);
                        break;

                    case "IsAlert":
                        bool.TryParse(objParameter.FieldValue1, out _IsAlert);
                        break;

                    case "IsTextile":
                        bool.TryParse(objParameter.FieldValue1, out _IsTextile);
                        break;


                    case "IsManualGatePassNo":
                        bool.TryParse(objParameter.FieldValue1, out _IsManualGatePassNo);
                        break;

                    case "IsBatchWiseSale":
                        bool.TryParse(objParameter.FieldValue1, out _IsBatchWiseSale);
                        break;

                    case "VoucherLocation":
                        bool.TryParse(objParameter.FieldValue1, out _VoucherLocation);
                        break;

                    case "PaymentTerms":
                        bool.TryParse(objParameter.FieldValue1, out _PaymentTerms);
                        break;

                    case "VehicleInformation":
                        bool.TryParse(objParameter.FieldValue1, out _VehicleInformation);
                        break;

                    case "IsColor":
                        bool.TryParse(objParameter.FieldValue1, out _IsColor);
                        break;

                    case "SOPaymentDisplayNone":
                        bool.TryParse(objParameter.FieldValue1, out _SOPaymentDisplayNone);
                        break;

                    case "SeasonDays":
                        int.TryParse(objParameter.FieldValue1, out _SeasonDays);
                        break;

                    case "Tandem":
                        int.TryParse(objParameter.FieldValue1, out _Tandem);
                        break;

                    case "QuotaProduction":
                        int.TryParse(objParameter.FieldValue1, out _QuotaProduction);
                        break;

                    case "QuotaPermit":
                        int.TryParse(objParameter.FieldValue1, out _QuotaPermit);
                        break;

                    case "SecurityOnDocumentType":
                        bool.TryParse(objParameter.FieldValue1, out _SecurityOnDocumentType);
                        break;

                    case "DispatchWeightNotVisible":
                        bool.TryParse(objParameter.FieldValue1, out _DispatchWeightNotVisible);
                        break;

                    case "DeliveryOrderRegisterSheikhoo":
                        bool.TryParse(objParameter.FieldValue1, out _DeliveryOrderRegisterSheikhoo);
                        break;

                    case "ReceiptSheikhoo":
                        bool.TryParse(objParameter.FieldValue1, out _ReceiptSheikhoo);
                        break;

                    case "ExpenseCostCenterRequired":
                        bool.TryParse(objParameter.FieldValue1, out _ExpenseCostCenterRequired);
                        break;

                    case "ItemVehicleField":
                        bool.TryParse(objParameter.FieldValue1, out _ItemVehicleField);
                        break;

                    case "GINSupplierRequired":
                        bool.TryParse(objParameter.FieldValue1, out _GINSupplierRequired);
                        break;

                    case "IsBlendFormulation":
                        bool.TryParse(objParameter.FieldValue1, out _IsBlendFormulation);
                        break;

                    case "SignatureForReport":
                        bool.TryParse(objParameter.FieldValue1, out _SignatureForReport);
                        break;

                    case "MultipleLoginBlocking":
                        bool.TryParse(objParameter.FieldValue1, out _MultipleLoginBlocking);
                        break;

                    case "IsVehicle":
                        bool.TryParse(objParameter.FieldValue1, out _IsVehicle);
                        break;

                    case "IsGrossChangedToBasic":
                        bool.TryParse(objParameter.FieldValue1, out _IsGrossChangedToBasic);
                        break;

                    case "IncometaxForAlnoor":
                        bool.TryParse(objParameter.FieldValue1, out _IncometaxForAlnoor);
                        break;

                    case "AKTSugar":
                        bool.TryParse(objParameter.FieldValue1, out _AKTSugar);
                        break;

                    case "BusinessPartnerOnDispatch":
                        bool.TryParse(objParameter.FieldValue1, out _BusinessPartnerOnDispatch);
                        break;

                    case "SimilarWeightDifference":
                        decimal.TryParse(objParameter.FieldValue1, out _SimilarWeightDifference);
                        break;

                    case "SimilarWeightCount":
                        int.TryParse(objParameter.FieldValue1, out _SimilarWeightCount);
                        break;

                    case "IssuanceFlow":
                        _IssuanceFlow = objParameter.FieldValue1;
                        break;

                    case "IsDisplayCostAtGRN":
                        bool.TryParse(objParameter.FieldValue1, out _IsDisplayCostAtGRN);
                        break;

                    case "SalesInvoiceAutoGenerate":
                        bool.TryParse(objParameter.FieldValue1, out _SalesInvoiceAutoGenerate);
                        break;

                    case "GINCommentsRequired":
                        bool.TryParse(objParameter.FieldValue1, out _GINCommentsRequired);
                        break;

                    case "InvisibleRequisitionInRFQ":
                        bool.TryParse(objParameter.FieldValue1, out _InvisibleRequisitionInRFQ);
                        break;

                    case "CategoryWiseSecurity":
                        bool.TryParse(objParameter.FieldValue1, out _CategoryWiseSecurity);
                        break;

                    case "CustomizeDispatch":
                        bool.TryParse(objParameter.FieldValue1, out _CustomizeDispatch);
                        break;

                    case "SaimaProductLine":
                        bool.TryParse(objParameter.FieldValue1, out _SaimaProductLine);
                        break;

                    case "IsMultipleDO":
                        bool.TryParse(objParameter.FieldValue1, out _IsMultipleDO);
                        break;

                    case "IsDirectDispatch":
                        bool.TryParse(objParameter.FieldValue1, out _IsDirectDispatch);
                        break;

                    case "LinkGrowerWithCircleAndCenter":
                        bool.TryParse(objParameter.FieldValue1, out _LinkGrowerWithCircleAndCenter);
                        break;

                    case "CenterCodeLength":
                        int.TryParse(objParameter.FieldValue1, out _CenterCodeLength);
                        break;

                    case "CircleCodeLength":
                        int.TryParse(objParameter.FieldValue1, out _CircleCodeLength);
                        break;

                    case "CostofSalesAccount":
                        bool.TryParse(objParameter.FieldValue1, out _CostofSalesAccount);
                        break;

                    case "InventoryAccountNew":
                        bool.TryParse(objParameter.FieldValue1, out _InventoryAccountNew);
                        break;

                    case "POScheduling":
                        bool.TryParse(objParameter.FieldValue1, out _POScheduling);
                        break;

                    case "SOScheduling":
                        bool.TryParse(objParameter.FieldValue1, out _SOScheduling);
                        break;

                    case "IsLocationRackRequired":
                        bool.TryParse(objParameter.FieldValue1, out _IsLocationRackRequired);
                        break;

                    case "DisplayStoppedCPR":
                        bool.TryParse(objParameter.FieldValue1, out _DisplayStoppedCPR);
                        break;

                    case "IsFixedAsset":
                        bool.TryParse(objParameter.FieldValue1, out _IsFixedAsset);
                        break;

                    case "IsWeightRestrict":
                        bool.TryParse(objParameter.FieldValue1, out _IsWeightRestrict);
                        break;

                    case "PaymentTaxes":
                        bool.TryParse(objParameter.FieldValue1, out _PaymentTaxes);
                        break;

                    case "IsSugar":
                        bool.TryParse(objParameter.FieldValue1, out _IsSugar);
                        break;

                    case "MultipleGrowerAgainstCNIC":
                        bool.TryParse(objParameter.FieldValue1, out _MultipleGrowerAgainstCNIC);
                        break;

                    case "ShowManualDeductionOnFirstWeight":
                        bool.TryParse(objParameter.FieldValue1, out _ShowManualDeductionOnFirstWeight);
                        break;

                    case "ContractorMustSelectOnHaulage":
                        bool.TryParse(objParameter.FieldValue1, out _ContractorMustSelectOnHaulage);
                        break;

                    case "IsLocationRackRequiredNormal":
                        bool.TryParse(objParameter.FieldValue1, out _IsLocationRackRequiredNormal);
                        break;

                    case "AgentDealerRequired":
                        bool.TryParse(objParameter.FieldValue1, out _AgentDealerRequired);
                        break;

                    case "CNICNotRequired":
                        bool.TryParse(objParameter.FieldValue1, out _CNICNotRequired);
                        break;
                    case "DriverInfoRequired":
                        bool.TryParse(objParameter.FieldValue1, out _DriverInfoRequired);
                        break;
                    case "DocumentTypeRequired":
                        bool.TryParse(objParameter.FieldValue1, out _DocumentTypeRequired);
                        break;

                    //IsDepartmentOnSO
                    case "IsDepartmentOnSO":
                        bool.TryParse(objParameter.FieldValue1, out _IsDepartmentOnSO);
                        break;

                    case "IsCenterRequiredUponRequisition":
                        bool.TryParse(objParameter.FieldValue1, out _IsCenterRequiredUponRequisition);
                        break;

                    case "RFQQuantityRequired":
                        bool.TryParse(objParameter.FieldValue1, out _RFQQuantityRequired);
                        break;

                    case "RequiredPOinPaymentVoucher":
                        bool.TryParse(objParameter.FieldValue1, out _RequiredPOinPaymentVoucher);
                        break;

                    case "NotRequiredVehicleType_No":
                        bool.TryParse(objParameter.FieldValue1, out _NotRequiredVehicleType_No);
                        break;

                    case "FilterPOByAdvancePercentage":
                        bool.TryParse(objParameter.FieldValue1, out _FilterPOByAdvancePercentage);
                        break;

                    case "FilterLocationByWareHouse":
                        bool.TryParse(objParameter.FieldValue1, out _FilterLocationByWareHouse);
                        break;

                    case "PrintSlipNo":
                        short.TryParse(objParameter.FieldValue1, out _PrintSlipNo);
                        break;

                    case "IsDirectPO":
                        bool.TryParse(objParameter.FieldValue1, out _IsDirectPO);
                        break;

                    case "FWDeductionPercentage":
                        decimal.TryParse(objParameter.FieldValue1, out _FWDeductionPercentage);
                        break;

                    case "OfflineWeighment":
                        bool.TryParse(objParameter.FieldValue1, out _OfflineWeighment);
                        break;

                    case "Minimumweightvalidationonsecondweight":
                        bool.TryParse(objParameter.FieldValue1, out _Minimumweightvalidationonsecondweight);
                        break;

                    case "ManualDateTime":
                        bool.TryParse(objParameter.FieldValue1, out _ManualDateTime);
                        break;

                    case "BrokerIsCustomer":
                        bool.TryParse(objParameter.FieldValue1, out _BrokerIsCustomer);
                        break;

                    case "IndentSupplyDateAdd":
                        int.TryParse(objParameter.FieldValue1, out _IndentSupplyDateAdd);
                        break;

                    case "LeakageSupplyDateAdd":
                        int.TryParse(objParameter.FieldValue1, out _LeakageSupplyDateAdd);
                        break;

                    case "ShiftHours":
                        int.TryParse(objParameter.FieldValue1, out _ShiftHours);
                        break;

                    case "UnregisteredCustomerMonthlyLimit":
                        double.TryParse(objParameter.FieldValue2, out _UnregisteredCustomerMonthlyLimit);
                        break;

                    case "UnregisteredCustomerYearlyLimit":
                        double.TryParse(objParameter.FieldValue2, out _UnregisteredCustomerYearlyLimit);
                        break;

                    case "EditableRFQQuantity":
                        bool.TryParse(objParameter.FieldValue1, out _EditableRFQQuantity);
                        break;

                    case "InvisibleEstimatedPOPrice":
                        bool.TryParse(objParameter.FieldValue1, out _InvisibleEstimatedPOPrice);
                        break;

                    case "LotNo":
                        bool.TryParse(objParameter.FieldValue1, out _LotNo);
                        break;

                    case "TrashOnNetWeight":
                        bool.TryParse(objParameter.FieldValue1, out _TrashOnNetWeight);
                        break;

                    case "DOvalidUpToDays":
                        short.TryParse(objParameter.FieldValue1, out _DOvalidUpToDays);
                        break;

                    case "AutoCalculateInBags":
                        bool.TryParse(objParameter.FieldValue1, out _AutoCalculateInBags);
                        break;

                    case "SODispatchDateDays":
                        int.TryParse(objParameter.FieldValue1, out _SODispatchDateDays);
                        break;

                    case "ShipToDeliveryOrder":
                        bool.TryParse(objParameter.FieldValue1, out _ShipToDeliveryOrder);
                        break;

                    case "ShowDispatchReportLink":
                        bool.TryParse(objParameter.FieldValue1, out _ShowDispatchReportLink);
                        break;

                    case "IsPaymentReportView":
                        bool.TryParse(objParameter.FieldValue1, out _IsPaymentReportView);
                        break;

                    case "MultipleBanksInTransaction":
                        bool.TryParse(objParameter.FieldValue1, out _MultipleBanksInTransaction);
                        break;

                    case "ELPWebServiceURL":
                        _ELPWebServiceURL = objParameter.FieldValue1;
                        break;

                    case "ShowELPData":
                        bool.TryParse(objParameter.FieldValue1, out _ShowELPData);
                        break;

                    case "SecondWeightCopyName_1":
                        _SecondWeightCopyName_1 = objParameter.FieldValue1;
                        break;

                    case "SecondWeightCopyName_2":
                        _SecondWeightCopyName_2 = objParameter.FieldValue1;
                        break;

                    case "SecondWeightCopyName_3":
                        _SecondWeightCopyName_3 = objParameter.FieldValue1;
                        break;

                    case "SecondWeightCopyName_4":
                        _SecondWeightCopyName_4 = objParameter.FieldValue1;
                        break;

                    case "SecondWeightCopyName_5":
                        _SecondWeightCopyName_5 = objParameter.FieldValue1;
                        break;

                    case "FirstWeightCopyName_1":
                        _FirstWeightCopyName_1 = objParameter.FieldValue1;
                        break;

                    case "FirstWeightCopyName_2":
                        _FirstWeightCopyName_2 = objParameter.FieldValue1;
                        break;

                    case "FirstWeightCopyName_3":
                        _FirstWeightCopyName_3 = objParameter.FieldValue1;
                        break;

                    case "FirstWeightCopyName_4":
                        _FirstWeightCopyName_4 = objParameter.FieldValue1;
                        break;

                    case "FirstWeightCopyName_5":
                        _FirstWeightCopyName_5 = objParameter.FieldValue1;
                        break;

                    case "DepotTPTPaymentGL":
                        _DepotTPTPaymentGL = objParameter.FieldValue1;
                        break;

                    case "LastInterestProcessDate":
                        if (!string.IsNullOrEmpty(objParameter.FieldValue1))
                            _LastInterestProcessDate = Convert.ToDateTime(objParameter.FieldValue1);
                        else
                            _LastInterestProcessDate = null;

                        break;

                    case "ELPProxyAddress":
                        _ELPProxyAddress = objParameter.FieldValue1;
                        break;

                    case "ELPProxyUserID":
                        _ELPProxyUserID = objParameter.FieldValue1;
                        break;

                    case "ELPProxyPassword":
                        _ELPProxyPassword = objParameter.FieldValue1;
                        break;

                    case "ELPByPassProxy":
                        bool.TryParse(objParameter.FieldValue1, out _ELPByPassProxy);
                        break;

                    case "RoadBlocker":
                        bool.TryParse(objParameter.FieldValue1, out _RoadBlocker);
                        break;

                    case "WeightStable":
                        bool.TryParse(objParameter.FieldValue1, out _WeightStable);
                        break;


                    case "WeightBlockerVariance":
                        Int16.TryParse(objParameter.FieldValue1, out _WeightBlockerVariance);
                        break;

                    case "MinimumVehicalWeight":
                        Int16.TryParse(objParameter.FieldValue1, out _MinimumVehicalWeight);
                        break;

                    case "IsSlipCompanyLabel":
                        bool.TryParse(objParameter.FieldValue1, out _IsSlipCompanyLabel);
                        break;

                    case "LinkGrowerWithIndent":
                        bool.TryParse(objParameter.FieldValue1, out _LinkGrowerWithIndent);
                        break;

                    case "CarriageContractorPayableVoucher":
                        bool.TryParse(objParameter.FieldValue1, out _CarriageContractorPayableVoucher);
                        break;

                    case "IsPRNCosting":
                        bool.TryParse(objParameter.FieldValue1, out _IsPRNCosting);
                        break;

                    case "IsPartialDispatch":
                        bool.TryParse(objParameter.FieldValue1, out _IsPartialDispatch);
                        break;

                    case "GrowerGroupRecovery":
                        bool.TryParse(objParameter.FieldValue1, out _GrowerGroupRecovery);
                        break;

                    case "IsInvisibleUponStockReports":
                        bool.TryParse(objParameter.FieldValue1, out _IsInvisibleUponStockReports);
                        break;

                    case "IsReceiptAgainstSO":
                        bool.TryParse(objParameter.FieldValue1, out _IsReceiptAgainstSO);
                        break;

                    case "IsSOFindMode":
                        bool.TryParse(objParameter.FieldValue1, out _IsSOFindMode);
                        break;

                    case "IsDispatchStockLedgerHideCancel":
                        bool.TryParse(objParameter.FieldValue1, out _IsDispatchStockLedgerHideCancel);
                        break;

                    case "ZeroAllowed":
                        bool.TryParse(objParameter.FieldValue1, out _ZeroAllowed);
                        break;

                    case "SalesDateValidation":
                        bool.TryParse(objParameter.FieldValue1, out _SalesDateValidation);
                        break;

                    case "RFQItemSelection":
                        bool.TryParse(objParameter.FieldValue1, out _RFQItemSelection);
                        break;

                    case "IsPaymentTax":
                        bool.TryParse(objParameter.FieldValue1, out _IsPaymentTax);
                        break;

                    case "DuplicateManualIndentSeries":
                        bool.TryParse(objParameter.FieldValue1, out _DuplicateManualIndentSeries);
                        break;

                    case "DisableContractorByCP":
                        bool.TryParse(objParameter.FieldValue1, out _DisableContractorByCP);
                        break;

                    case "TMSUploadAccess":
                        bool.TryParse(objParameter.FieldValue1, out _TMSUploadAccess);
                        break;

                    case "HideListPriceOnRequisition":
                        bool.TryParse(objParameter.FieldValue1, out _HideListPriceOnRequisition);
                        break;

                    case "SupplierCreditOnPayment":
                        bool.TryParse(objParameter.FieldValue1, out _SupplierCreditOnPayment);
                        break;

                    case "StopLoanDeduction":
                        if (!bool.TryParse(objParameter.FieldValue1, out _StopLoanDeduction))
                            _StopLoanDeduction = false;

                        break;

                    case "ShowQualityOnSO":
                        bool.TryParse(objParameter.FieldValue1, out _ShowQualityOnSO);
                        break;

                    case "TaxPercentageInAccountsGrid":
                        bool.TryParse(objParameter.FieldValue1, out _TaxPercentageInAccountsGrid);
                        break;

                    case "SalaryProcessbyTMS":
                        bool.TryParse(objParameter.FieldValue1, out _SalaryProcessbyTMS);
                        break;

                    case "SalaryProcessForAsadRice":
                        bool.TryParse(objParameter.FieldValue1, out _SalaryProcessForAsadRice);
                        break;

                    case "LateDeductionPolicy":
                        bool.TryParse(objParameter.FieldValue1, out _LateDeductionPolicy);
                        break;

                    case "IsEmployeeCodeEditable":
                        bool.TryParse(objParameter.FieldValue1, out _IsEmployeeCodeEditable);
                        break;

                    case "RoasterForCIS":
                        bool.TryParse(objParameter.FieldValue1, out _RoasterForCIS);
                        break;

                    case "MultipleBusinessPartners":
                        bool.TryParse(objParameter.FieldValue1, out _MultipleBusinessPartners);
                        break;

                    case "POMultiRequisition":
                        bool.TryParse(objParameter.FieldValue1, out _POMultiRequisition);
                        break;

                    case "IsOrient":
                        bool.TryParse(objParameter.FieldValue1, out _IsOrient);
                        break;

                    case "DisplayOtherUserRecords":
                        if (!bool.TryParse(objParameter.FieldValue1, out _DisplayOtherUserRecords))
                            _DisplayOtherUserRecords = false;

                        break;

                    case "MandatorySOQualityBatch":
                        bool.TryParse(objParameter.FieldValue1, out _MandatorySOQualityBatch);
                        break;

                    case "RestrictZeroQtyOnGIN":
                        bool.TryParse(objParameter.FieldValue1, out _RestrictZeroQtyOnGIN);
                        break;

                    case "ManualWeight":
                        if (!bool.TryParse(objParameter.FieldValue1, out _ManualWeight))
                            _ManualWeight = false;

                        break;

                    case "ShowReportOnSave":
                        bool.TryParse(objParameter.FieldValue1, out _ShowReportOnSave);
                        break;

                    case "BusinessPartnerIDsRange":
                        int resultValue = -1;
                        _BusinessPartnerIDsRange = new int[2];

                        if (int.TryParse(objParameter.FieldValue1, out resultValue))
                            _BusinessPartnerIDsRange[0] = resultValue;
                        else
                            _BusinessPartnerIDsRange[0] = 1;

                        if (int.TryParse(objParameter.FieldValue2, out resultValue))
                            _BusinessPartnerIDsRange[1] = resultValue;
                        else
                            _BusinessPartnerIDsRange[1] = 5000000;

                        break;

                    case "IsSynchronize":
                        bool.TryParse(objParameter.FieldValue1, out _IsSynchronize);
                        break;

                    case "EditAllRecords":
                        if (!bool.TryParse(objParameter.FieldValue1, out _EditAllRecords))
                            _EditAllRecords = false;

                        break;

                    case "DefineFAProcessStarDate":
                        bool.TryParse(objParameter.FieldValue1, out _DefineFAProcessStarDate);
                        break;

                    case "SetItemCodeByColor":
                        bool.TryParse(objParameter.FieldValue1, out _SetItemCodeByColor);
                        break;

                    case "BandhiSalarySheet":
                        bool.TryParse(objParameter.FieldValue1, out _BandhiSalarySheet);
                        break;

                    case "GrossSalaryTotal":
                        bool.TryParse(objParameter.FieldValue1, out _GrossSalaryTotal);
                        break;

                    case "SalesInvoiceForPolymer":
                        bool.TryParse(objParameter.FieldValue1, out _SalesInvoiceForPolymer);
                        break;

                    case "DeliveryOrderForPolymer":
                        bool.TryParse(objParameter.FieldValue1, out _DeliveryOrderForPolymer);
                        break;


                    case "DispatchNoteForPolymer":
                        bool.TryParse(objParameter.FieldValue1, out _DispatchNoteForPolymer);
                        break;

                    case "SalesOrderForPolymer":
                        bool.TryParse(objParameter.FieldValue1, out _SalesOrderForPolymer);
                        break;

                    case "DIInvoiceForPolymer":
                        bool.TryParse(objParameter.FieldValue1, out _DIInvoiceForPolymer);
                        break;

                    case "CompanyNameWithoutOperatingUnit":
                        bool.TryParse(objParameter.FieldValue1, out _CompanyNameWithoutOperatingUnit);
                        break;

                    case "DeliveryScheduleForPolymer":
                        bool.TryParse(objParameter.FieldValue1, out _DeliveryScheduleForPolymer);
                        break;

                    case "OverTimeCalculationForBandhi":
                        bool.TryParse(objParameter.FieldValue1, out _OverTimeCalculationForBandhi);
                        break;

                    case "OverTimeCalculationForMBI":
                        bool.TryParse(objParameter.FieldValue1, out _OverTimeCalculationForMBI);
                        break;

                    case "OverTimeCalculationForAlAbbas":
                        bool.TryParse(objParameter.FieldValue1, out _OverTimeCalculationForAlAbbas);
                        break;

                    case "LoanAmountTypeWise":
                        bool.TryParse(objParameter.FieldValue1, out _LoanAmountTypeWise);
                        break;

                    case "IsSalaryConnect":
                        bool.TryParse(objParameter.FieldValue1, out _IsSalaryConnect);
                        break;

                    case "EmployeeCodeForConnect":
                        bool.TryParse(objParameter.FieldValue1, out _EmployeeCodeForConnect);
                        break;

                    case "DirectApproval":
                        bool.TryParse(objParameter.FieldValue1, out _DirectApproval);
                        break;

                    case "UtilityProcess":
                        bool.TryParse(objParameter.FieldValue1, out _UtilityProcess);
                        break;

                    case "GrowerBusinessPartner":
                        bool.TryParse(objParameter.FieldValue1, out _GrowerBusinessPartner);
                        break;

                    case "MillLeakageBusinessPartner":
                        bool.TryParse(objParameter.FieldValue1, out _MillLeakageBusinessPartner);
                        break;

                    case "IsLoandeductionFrombonus":
                        bool.TryParse(objParameter.FieldValue1, out _IsLoandeductionFrombonus);
                        break;

                    case "DemandSeasonRequisition":
                        bool.TryParse(objParameter.FieldValue1, out _DemandSeasonRequisition);
                        break;

                    case "LicenseCheck":
                        bool.TryParse(objParameter.FieldValue1, out _LicenseCheck);
                        break;

                    case "OverTimeCalculationForSheikhoo":
                        bool.TryParse(objParameter.FieldValue1, out _OverTimeCalculationForSheikhoo);
                        break;

                    case "AttendanceUploadForAdam":
                        bool.TryParse(objParameter.FieldValue1, out _AttendanceUploadForAdam);
                        break;

                    case "LoadMonthlyDocuments":
                        if (!bool.TryParse(objParameter.FieldValue1, out _LoadMonthlyDocuments))
                            _LoadMonthlyDocuments = false;
                        break;

                    case "CommissionOnReceipt":
                        bool.TryParse(objParameter.FieldValue1, out _CommissionOnReceipt);
                        break;

                    case "RFQDueDateDiscard":
                        if (!bool.TryParse(objParameter.FieldValue1, out _RFQDueDateDiscard))
                            _RFQDueDateDiscard = false;

                        break;
                    case "isGINWithoutGLCode":
                        bool.TryParse(objParameter.FieldValue1, out _isGINWithoutGLCode);
                        break;

                    case "IsGenerateReport":
                        bool.TryParse(objParameter.FieldValue1, out _IsGenerateReport);
                        break;

                    case "ShowExpenseAccountGrid":
                        if (!bool.TryParse(objParameter.FieldValue1, out _ShowExpenseAccountGrid))
                            _ShowExpenseAccountGrid = false;

                        break;

                    case "ValidateBilltyNoOnDispatch":
                        bool.TryParse(objParameter.FieldValue1, out _ValidateBilltyNoOnDispatch);
                        break;

                    case "CPRVouchers":
                        if (!bool.TryParse(objParameter.FieldValue1, out _CPRVouchers))
                            _CPRVouchers = true;

                        break;

                    case "CarConPaymentVoucher":
                        if (!bool.TryParse(objParameter.FieldValue1, out _CarConPaymentVoucher))
                            _CarConPaymentVoucher = true;

                        break;
                    case "trashdeductionOntoken":
                        bool.TryParse(objParameter.FieldValue1, out _trashdeductionOntoken);
                        break;

                    case "ItemParts":
                        bool.TryParse(objParameter.FieldValue1, out _ItemParts);
                        break;

                    case "FGPostGL":
                        bool.TryParse(objParameter.FieldValue1, out _FGPostGL);
                        break;

                    case "ContractorManualRate":
                        if (!bool.TryParse(objParameter.FieldValue1, out _ContractorManualRate))
                            _ContractorManualRate = true;
                        break;
                    case "HideWeightDedInDashboard":
                        if (!bool.TryParse(objParameter.FieldValue1, out _HideWeightDedInDashboard))
                            _HideWeightDedInDashboard = true;
                        break;

                    case "IsAutoPrinting":
                        bool.TryParse(objParameter.FieldValue1, out _IsAutoPrinting);
                        break;

                    case "IsUpdatePayrollFromEmpInfo":
                        bool.TryParse(objParameter.FieldValue1, out _IsUpdatePayrollFromEmpInfo);
                        break;

                    case "AlnoorAttendaceAndOvertime":
                        bool.TryParse(objParameter.FieldValue1, out _AlnoorAttendaceAndOvertime);
                        break;


                    case "HabibOTForAdditionalPayment":
                        bool.TryParse(objParameter.FieldValue1, out _HabibOTForAdditionalPayment);
                        break;

                    case "AlnoorArrearWork":
                        bool.TryParse(objParameter.FieldValue1, out _AlnoorArrearWork);
                        break;

                    case "HouseRentUseInFormulaForDKSM":
                        bool.TryParse(objParameter.FieldValue1, out _HouseRentUseInFormulaForDKSM);
                        break;

                    case "CopyButtonForVoucher":
                        if (!bool.TryParse(objParameter.FieldValue1, out _CopyButtonForVoucher))
                            _CopyButtonForVoucher = false;

                        break;

                    case "isProfitAndLoss":
                        if (!bool.TryParse(objParameter.FieldValue1, out isProfitAndLoss))
                            isProfitAndLoss = false;
                        break;


                    case "AllowSelectedDocumentTypes":
                        if (!bool.TryParse(objParameter.FieldValue1, out _AllowSelectedDocumentTypes))
                            _AllowSelectedDocumentTypes = false;
                        break;
                    //AllowInprocessOption

                    case "AllowInprocessOption":
                        if (!bool.TryParse(objParameter.FieldValue1, out _AllowInprocessOption))
                            _AllowInprocessOption = false;
                        break;

                    case "StopGLStockTransfer":
                        if (!bool.TryParse(objParameter.FieldValue1, out _StopGLStockTransfer))
                            _StopGLStockTransfer = false;
                        break;


                    case "isFiscalSystem":
                        if (!bool.TryParse(objParameter.FieldValue1, out isFiscalSystem))
                            isFiscalSystem = false;
                        break;

                    case "isStockFIFO":
                        if (!bool.TryParse(objParameter.FieldValue1, out isStockFIFO))
                            isStockFIFO = false;
                        break;

                    case "isLocationHide":
                        if (!bool.TryParse(objParameter.FieldValue1, out isLocationHide))
                            isLocationHide = false;
                        break;

                    case "isNOCOGSEntry":
                        if (!bool.TryParse(objParameter.FieldValue1, out isNOCOGSEntry))
                            isNOCOGSEntry = false;
                        break;

                    case "SalaryProcessWithoutLeaves":
                        bool.TryParse(objParameter.FieldValue1, out _SalaryProcessWithoutLeaves);
                        break;
                    case "UserWiseDocumentAppear":
                        bool.TryParse(objParameter.FieldValue1, out _UserWiseDocumentAppear);
                        break;

                    case "AutoGenerateBarCode":
                        bool.TryParse(objParameter.FieldValue1, out _AutoGenerateBarCode);
                        break;

                    case "FabricDepartment":
                        _FabricDepartment = objParameter.FieldValue1;
                        break;
                    case "FabricCostCenter":
                        _FabricCostCenter = objParameter.FieldValue1;
                        break;
                    case "FabricLocation":
                        _FabricLocation = objParameter.FieldValue1;
                        break;
                    case "SROPercent":
                        _SROPercent = objParameter.FieldValue1;
                        break;
                    case "SROZeroPercent":
                        _SROZeroPercent = objParameter.FieldValue1;
                        break;
                    case "FinishingCostCenter":
                        _FinishingCostCenter = objParameter.FieldValue1;
                        break;
                    case "SROInvoices":
                        bool.TryParse(objParameter.FieldValue1, out _SROInvoices);
                        break;
                    case "AllowDetailNarration":
                        bool.TryParse(objParameter.FieldValue1, out _AllowDetailNarration);
                        break;
                    case "LeavePolicy":
                        bool.TryParse(objParameter.FieldValue1, out _LeavePolicy);
                        break;

                    case "IsFabricAlImdad":
                        bool.TryParse(objParameter.FieldValue1, out _IsFabricAlImdad);
                        break;
                    case "IsDisallowBackDate":
                        bool.TryParse(objParameter.FieldValue1, out _IsDisallowBackDate);
                        break;
                    case "IsGRNDisallowBackDate":
                        bool.TryParse(objParameter.FieldValue1, out _IsGRNDisallowBackDate);
                        break;
                    case "LogisticVendorDefaultCOA":
                        _LogisticVendorDefaultCOA = objParameter.FieldValue1;
                        break;
                    case "LoadingUnloadingDefaultCOA":
                        _LoadingUnloadingDefaultCOA = objParameter.FieldValue1;
                        break;
                    case "ClearingDefaultCOA":
                        _ClearingDefaultCOA = objParameter.FieldValue1;
                        break;
                    case "TransporterDefaultCOA":
                        _TransporterDefaultCOA = objParameter.FieldValue1;
                        break;
                    case "VendorDefaultCOA":
                        _VendorDefaultCOA = objParameter.FieldValue1;
                        break;
                    case "TMSDataExcelFormatJKS":
                        bool.TryParse(objParameter.FieldValue1, out _TMSDataExcelFormatJKS);
                        break;
                    case "BGroupDefaultCOA":
                        _BGroupDefaultCOA = objParameter.FieldValue1;
                        break;
                    case "HGroupDefaultCOA":
                        _HGroupDefaultCOA = objParameter.FieldValue1;
                        break;
                    case "CommonGroupDefaultCOA":
                        _CommonGroupDefaultCOA = objParameter.FieldValue1;
                        break;
                    case "CompanyNameParam":
                        _CompanyNameParam = objParameter.FieldValue1;
                        break;
                    case "WeightDiffAgainstVehicle":
                        _WeightDiffAgainstVehicle = Convert.ToInt32(objParameter.FieldValue1);
                        break;
                    case "IsHideTrashOnSecondWeightAutoSlip":
                        bool.TryParse(objParameter.FieldValue1, out _IsHideTrashOnSecondWeightAutoSlip);
                        break;
                    case "VarietyHideOnSlips":
                        bool.TryParse(objParameter.FieldValue1, out _VarietyHideOnSlips);
                        break;
                    case "DeductionOnWeightPayable":
                        bool.TryParse(objParameter.FieldValue1, out _DeductionOnWeightPayable);
                        break;
                    case "TrashDeductionFromGrossWeight":
                        if (!bool.TryParse(objParameter.FieldValue1, out _TrashDeductionFromGrossWeight))
                            _TrashDeductionFromGrossWeight = true;

                        break;
                    case "ToDateRecovery":
                        decimal.TryParse(objParameter.FieldValue1, out _ToDateRecovery);
                        break;
                    case "SurchargeOnIncomeTax":
                        decimal.TryParse(objParameter.FieldValue1, out _SurchargeOnIncomeTax);
                        break;
                    case "StopPrintReportOnFirstWeight":
                        if (!bool.TryParse(objParameter.FieldValue1, out _StopPrintReportOnFirstWeight))
                            _StopPrintReportOnFirstWeight = true;

                        break;
                    case "PrintReportOnSecondWeight":
                        bool.TryParse(objParameter.FieldValue1, out _PrintReportOnSecondWeight);
                        break;
                    case "MaxSupplyValidationOnToken":
                        bool.TryParse(objParameter.FieldValue1, out _MaxSupplyValidationOnToken);
                        break;
                    case "IsShowVariety":
                        if (!bool.TryParse(objParameter.FieldValue1, out _IsShowVariety))
                            isGrowerReference = false;
                        break;

                    case "IsShowAreaName":
                        bool.TryParse(objParameter.FieldValue1, out _IsShowAreaName);
                        break;

                    case "IsShowSurveyID":
                        bool.TryParse(objParameter.FieldValue1, out _IsShowSurveyID);
                        break;
                    case "BandhiAutoPrint":
                        bool.TryParse(objParameter.FieldValue1, out _BandhiAutoPrint);
                        break;
                    case "IndentExpiredInNoOfDays":
                        _IndentExpiredInNoOfDays = Convert.ToInt16(objParameter.FieldValue1);
                        break;
                    case "isGrowerReference":
                        if (!bool.TryParse(objParameter.FieldValue1, out isGrowerReference))
                            isGrowerReference = false;
                        break;
                    case "TransferOutGL":
                        bool.TryParse(objParameter.FieldValue1, out _TransferOutGL);
                        break;
                    case "StopAllDeduction":
                        bool.TryParse(objParameter.FieldValue1, out _StopAllDeduction);
                        break;
                    case "ValidateSupplyDateIndent":
                        bool.TryParse(objParameter.FieldValue1, out _ValidateSupplyDateIndent);
                        break;
                    case "BranchCompany":
                        _BranchCompany = Convert.ToInt16(objParameter.FieldValue1);
                        break;
                    case "DocumentIDsForSite":
                        if (!bool.TryParse(objParameter.FieldValue1, out _DocumentIDsForSite))
                            _DocumentIDsForSite = true;

                        break;

                    case "DispatchTo":
                        if (!bool.TryParse(objParameter.FieldValue1, out _DispatchTo))
                            _DispatchTo = true;
                        break;

                    case "TaxCalculationOnContractDate":
                        bool.TryParse(objParameter.FieldValue1, out _TaxCalculationOnContractDate);
                        break;

                    case "ManualWeightSales":
                        bool.TryParse(objParameter.FieldValue1, out _ManualWeightSales);
                        break;

                    case "ManualWeightInv":
                        bool.TryParse(objParameter.FieldValue1, out _ManualWeightInv);
                        break;

                    case "ManualWeightSalesSteel":
                        bool.TryParse(objParameter.FieldValue1, out _ManualWeightSalesSteel);
                        break;

                    case "DispatchReturnInTareWeight":
                        bool.TryParse(objParameter.FieldValue1, out _DispatchReturnInTareWeight);
                        break;


                    case "LodingpointCustomerDispatch":
                        bool.TryParse(objParameter.FieldValue1, out _LodingpointCustomerDispatch);
                        break;

                    case "CustomerNameII":
                        bool.TryParse(objParameter.FieldValue1, out _CustomerNameII);
                        break;

                    case "IsSheikhooReport":
                        bool.TryParse(objParameter.FieldValue1, out _IsSheikhooReport);
                        break;

                    case "IsConnectReport":
                        bool.TryParse(objParameter.FieldValue1, out _IsConnectReport);
                        break;

                    case "IsHabibSugar":
                        bool.TryParse(objParameter.FieldValue1, out _IsHabibSugar);
                        break;
                    case "IsTwoStar":
                        bool.TryParse(objParameter.FieldValue1, out _IsTwoStar);
                        break;
                    case "isLoanGuarantor":
                        bool.TryParse(objParameter.FieldValue1, out _isLoanGuarantor);
                        break;

                    case "UniqueGuarantor":
                        bool.TryParse(objParameter.FieldValue1, out _UniqueGuarantor);
                        break;

                    case "IsPatoki":
                        bool.TryParse(objParameter.FieldValue1, out _IsPatoki);
                        break;

                    case "IsPrintsalesFirstWeight":
                        bool.TryParse(objParameter.FieldValue1, out _IsPrintsalesFirstWeight);
                        break;

                    case "AllowInventoryBackDateEntry":
                        bool.TryParse(objParameter.FieldValue1, out _AllowInventoryBackDateEntry);
                        break;

                    case "SalesTaxImport":
                        _SalesTaxImport = objParameter.FieldValue1;
                        break;

                    case "SalesTaxServices":
                        _SalesTaxServices = objParameter.FieldValue1;
                        break;

                    case "AdvanceIncomeTaxImport":
                        _AdvanceIncomeTaxImport = objParameter.FieldValue1;
                        break;

                    case "FetchDataFromDOInGateIn":
                        bool.TryParse(objParameter.FieldValue1, out _FetchDataFromDOInGateIn);
                        break;

                    case "SalesDocumentType":
                        bool.TryParse(objParameter.FieldValue1, out _SalesDocumentType);
                        break;

                    case "SaleInvoiceProcess":
                        bool.TryParse(objParameter.FieldValue1, out _SaleInvoiceProcess);
                        break;
                    case "IsAKTSEmpInfo":
                        bool.TryParse(objParameter.FieldValue1, out _IsAKTSEmpInfo);
                        break;
                    case "HSMMultipleCustomerDOAtLoadingPoint":
                        bool.TryParse(objParameter.FieldValue1, out _HSMMultipleCustomerDOAtLoadingPoint);
                        break;
                    case "IsAllowancesAddInGross":
                        bool.TryParse(objParameter.FieldValue1, out _IsAllowancesAddInGross);
                        break;

                    case "IsOverTimePartofsalary":
                        bool.TryParse(objParameter.FieldValue1, out _IsOverTimePartofsalary);
                        break;

                    case "IsFormuladisplayonotperhour":
                        bool.TryParse(objParameter.FieldValue1, out _IsFormuladisplayonotperhour);
                        break;

                    case "AllowAllDoctypeRequisitionAssignment":
                        bool.TryParse(objParameter.FieldValue1, out _AllowAllDoctypeRequisitionAssignment);
                        break;

                    case "HSMLoadingPointSlipHTML":
                        bool.TryParse(objParameter.FieldValue1, out _HSMLoadingPointSlipHTML);
                        break;

                    case "JKLetterOfCreditPOLookUP":
                        bool.TryParse(objParameter.FieldValue1, out _JKLetterOfCreditPOLookUP);
                        break;

                    case "CompanyName":
                        _CompanyName = objParameter.FieldValue1;
                        break;

                    case "BonusForHabib":
                        bool.TryParse(objParameter.FieldValue1, out _BonusForHabib);
                        break;

                    case "ShiftForAsadRice":
                        bool.TryParse(objParameter.FieldValue1, out _ShiftForAsadRice);
                        break;

                    case "IsPaySlipConnect":
                        bool.TryParse(objParameter.FieldValue1, out _IsPaySlipConnect);
                        break;
                    case "InvoiceRateSheikhoo":
                        bool.TryParse(objParameter.FieldValue1, out _InvoiceRateSheikhoo);
                        break;

                    case "GateParameterForHabib":
                        bool.TryParse(objParameter.FieldValue1, out _GateParameterForHabib);
                        break;
                    case "IsEmployeeCodeMandatory":
                        bool.TryParse(objParameter.FieldValue1, out _IsEmployeeCodeMandatory);
                        break;
                    case "InOutTransTypeAndImpact":
                        bool.TryParse(objParameter.FieldValue1, out _InOutTransTypeAndImpact);
                        break;
                    case "RackBatchExpiry":
                        bool.TryParse(objParameter.FieldValue1, out _RackBatchExpiry);
                        break;
                    case "NACostCenter":
                        bool.TryParse(objParameter.FieldValue1, out _NACostCenter);
                        break;
                    case "EightToEight":
                        bool.TryParse(objParameter.FieldValue1, out _EightToEight);
                        break;
                    case "RecoveryAnalysisNotMandatory":
                        bool.TryParse(objParameter.FieldValue1, out _RecoveryAnalysisNotMandatory);
                        break;
                    case "JKManualWeight":
                        bool.TryParse(objParameter.FieldValue1, out _JKManualWeight);
                        break;
                    case "Header":
                        bool.TryParse(objParameter.FieldValue1, out _Header);
                        break;
                    case "IsStockLocationRackWise":
                        if (!bool.TryParse(objParameter.FieldValue1, out _IsStockLocationRackWise)) ;
                        break;
                    case "IsAllowEditDescription":
                        if (!bool.TryParse(objParameter.FieldValue1, out _IsAllowEditDescription)) ;
                        break;
                    case "IsAllowDecimalOnPresentdays":
                        if (!bool.TryParse(objParameter.FieldValue1, out _IsAllowDecimalOnPresentdays)) ;
                        break;
                    case "CompanyParameter":
                        _CompanyParameter = objParameter.FieldValue1;
                        break;
                    case "IsAllowPartialAdvance":
                        if (!bool.TryParse(objParameter.FieldValue1, out _IsAllowPartialAdvance)) ;
                        break;
                    case "MergeTaxes":
                        if (!bool.TryParse(objParameter.FieldValue1, out _MergeTaxes)) ;
                        break;
                    case "IsSandwich":
                        if (!bool.TryParse(objParameter.FieldValue1, out _IsSandwich)) ;
                        break;
                    case "CustomerPartyOnDispatch":
                        if (!bool.TryParse(objParameter.FieldValue1, out _CustomerPartyOnDispatch)) ;
                        break;

                    case "FirstSalesSecondWeight_1":
                        _FirstSalesSecondWeight_1 = objParameter.FieldValue1;
                        break;
                    case "SecondSalesSecondWeight_1":
                        _SecondSalesSecondWeight_1 = objParameter.FieldValue1;
                        break;
                    case "ThirdSalesSecondWeight_1":
                        _ThirdSalesSecondWeight_1 = objParameter.FieldValue1;
                        break;
                    case "SalesSecondWeightPrint":
                        bool.TryParse(objParameter.FieldValue1, out _SalesSecondWeightPrint);
                        break;
                    case "IsSakrandSugarMill":
                        bool.TryParse(objParameter.FieldValue1, out _IsSakrandSugarMill);
                        break;
                    case "IsHideETD":
                        bool.TryParse(objParameter.FieldValue1, out _IsHideETD);
                        break;
                    case "IsHideETA":
                        bool.TryParse(objParameter.FieldValue1, out _IsHideETA);
                        break;
                    case "IsHidePaymentMethod":
                        bool.TryParse(objParameter.FieldValue1, out _IsHidePaymentMethod);
                        break;
                    case "IsHideRFQ":
                        bool.TryParse(objParameter.FieldValue1, out _IsHideRFQ);
                        break;
                    case "POPaymentSchedule":
                        bool.TryParse(objParameter.FieldValue1, out _POPaymentSchedule);
                        break;
                    case "DKEOBIReport":
                        bool.TryParse(objParameter.FieldValue1, out _DKEOBIReport);
                        break;
                    case "IsDKSM":
                        bool.TryParse(objParameter.FieldValue1, out _IsDKSM);
                        break;
                    case "TokenSecondCopy":
                        bool.TryParse(objParameter.FieldValue1, out _TokenSecondCopy);
                        break;
                    case "SKUDOExtraField":
                        bool.TryParse(objParameter.FieldValue1, out _SKUDOExtraField);
                        break;
                    case "ChangeItemOnDispatch":
                        bool.TryParse(objParameter.FieldValue1, out _ChangeItemOnDispatch);
                        break;
                    case "remarksondispatch":
                        bool.TryParse(objParameter.FieldValue1, out _remarksondispatch);
                        break;
                    case "MandatoryIncurredOnGIN":
                        bool.TryParse(objParameter.FieldValue1, out _MandatoryIncurredOnGIN);
                        break;
                    case "InvoiceAdjustbyDirectPayment":
                        bool.TryParse(objParameter.FieldValue1, out _InvoiceAdjustbyDirectPayment);
                        break;
                    case "MandatoryIncuredOnRequisition":
                        bool.TryParse(objParameter.FieldValue1, out _MandatoryIncuredOnRequisition);
                        break;
                    case "IsSOEditAfterApprove":
                        bool.TryParse(objParameter.FieldValue1, out _IsSOEditAfterApprove);
                        break;
                    case "SOAllowApproveDays":
                        _SOAllowApproveDays = Convert.ToInt16(objParameter.FieldValue1);
                        break;
                    case "ExcessDays":
                        _ExcessDays = Convert.ToString(objParameter.FieldValue1);
                        break;

                    case "SOCostCenter":
                        bool.TryParse(objParameter.FieldValue1, out _SOCostCenter);
                        break;
                    case "UnlimitedAmountForSSML":
                        bool.TryParse(objParameter.FieldValue1, out _UnlimitedAmountForSSML);
                        break;
                    case "SeparateAmountInGD":
                        bool.TryParse(objParameter.FieldValue1, out _SeparateAmountInGD);
                        break;
                    case "isApprovalHistoryPopup":
                        bool.TryParse(objParameter.FieldValue1, out _isApprovalHistoryPopup);
                        break;
                    case "IsConnect":
                        bool.TryParse(objParameter.FieldValue1, out _IsConnect);
                        break;
                    case "IsSanghar":
                        bool.TryParse(objParameter.FieldValue1, out _IsSanghar);
                        break;
                    case "SWVAlidateRemoveForRA":
                        bool.TryParse(objParameter.FieldValue1, out _SWVAlidateRemoveForRA);
                        break;

                    case "IsHideDataAfterDispatch":
                        bool.TryParse(objParameter.FieldValue1, out _IsHideDataAfterDispatch);
                        break;

                    case "IsMultipleDispatch":
                        bool.TryParse(objParameter.FieldValue1, out _IsMultipleDispatch);
                        break;
                    case "TPTRateonNetWeight":
                        bool.TryParse(objParameter.FieldValue1, out _TPTRateonNetWeight);
                        break;

                    case "GlClosingCostCenterWise":
                        bool.TryParse(objParameter.FieldValue1, out _GlClosingCostCenterWise);
                        break;

                    case "IsOverTimeUpload":
                        bool.TryParse(objParameter.FieldValue1, out _IsOverTimeUpload);
                        break;

                    case "IsBiltyTPTRate":
                        bool.TryParse(objParameter.FieldValue1, out _IsBiltyTPTRate);
                        break;

                    case "MergeTransactionForBusinessPartnerGLDetail":
                        bool.TryParse(objParameter.FieldValue1, out _MergeTransactionForBusinessPartnerGLDetail);
                        break;

                    case "IsRaniPurSugarMill":
                        bool.TryParse(objParameter.FieldValue1, out _IsRaniPurSugarMill);
                        break;

                    case "IsTrashPercentageOnToken":
                        bool.TryParse(objParameter.FieldValue1, out _IsTrashPercentageOnToken);
                        break;

                    case "DriverInfoInBiltyTPTPayment":
                        bool.TryParse(objParameter.FieldValue1, out _DriverInfoInBiltyTPTPayment);
                        break;

                    case "AmountToBeRoundOnDIPayment":
                        bool.TryParse(objParameter.FieldValue1, out _AmountToBeRoundOnDIPayment);
                        break;

                    case "DONoOnGatePass":
                        bool.TryParse(objParameter.FieldValue1, out _DONoOnGatePass);
                        break;

                    case "IsTrashPercentageOnSecondWeight":
                        bool.TryParse(objParameter.FieldValue1, out _IsTrashPercentageOnSecondWeight);
                        break;

                    case "DispatchValidateFromCustomerParty":
                        bool.TryParse(objParameter.FieldValue1, out _DispatchValidateFromCustomerParty);
                        break;

                    case "BiltyTPTAmountShouldNotRound":
                        bool.TryParse(objParameter.FieldValue1, out _BiltyTPTAmountShouldNotRound);
                        break;

                    case "OverTimeCalculationByFormula":
                        bool.TryParse(objParameter.FieldValue1, out _OverTimeCalculationByFormula);
                        break;

                    case "HaulageEnableOnToken":
                        bool.TryParse(objParameter.FieldValue1, out _HaulageEnableOnToken);
                        break;

                    case "AgentWiseCustomerValidate":
                        bool.TryParse(objParameter.FieldValue1, out _AgentWiseCustomerValidate);
                        break;

                    case "AllowAllBankAccountsinScrollPayment":
                        bool.TryParse(objParameter.FieldValue1, out _AllowAllBankAccountsinScrollPayment);
                        break;

                    case "PaymentReceiptReport":
                        bool.TryParse(objParameter.FieldValue1, out _PaymentReceiptReport);
                        break;

                    case "CashPaymentScrollWithTokenInfo":
                        bool.TryParse(objParameter.FieldValue1, out _CashPaymentScrollWithTokenInfo);
                        break;

                    case "IsDirectPOInvisible":
                        bool.TryParse(objParameter.FieldValue1, out _IsDirectPOInvisible);
                        break;

                    case "MaxPoolSize":
                        _MaxPoolSize = Convert.ToInt16(objParameter.FieldValue1);
                        break;

                    case "OTCalcOnBasicByFormula":
                        bool.TryParse(objParameter.FieldValue1, out _OTCalcOnBasicByFormula);
                        break;

                    case "IsMultiCurrencyAllowOnPO":
                        bool.TryParse(objParameter.FieldValue1, out _IsMultiCurrencyAllowOnPO);
                        break;

                    case "ImplicationofSRO":
                        bool.TryParse(objParameter.FieldValue1, out _ImplicationofSRO);
                        break;

                    case "EmployeeInsertForPF":
                        bool.TryParse(objParameter.FieldValue1, out _EmployeeInsertForPF);
                        break;

                    case "ScrollPaymentUserAuthorityWise":
                        bool.TryParse(objParameter.FieldValue1, out _ScrollPaymentUserAuthorityWise);
                        break;

                    case "StopGLImpactOnLoanAdjustment":
                        bool.TryParse(objParameter.FieldValue1, out _StopGLImpactOnLoanAdjustment);
                        break;

                    case "WeightStableWithOutRoadBlocker":
                        bool.TryParse(objParameter.FieldValue1, out _WeightStableWithOutRoadBlocker);
                        break;

                    case "CombineWeightWithOutRoadBlocker":
                        bool.TryParse(objParameter.FieldValue1, out _CombineWeightWithOutRoadBlocker);
                        break;

                    case "AutoDefaultBank":
                        bool.TryParse(objParameter.FieldValue1, out _AutoDefaultBank);
                        break;

                    case "PayeeNameOnEmployeePayment":
                        bool.TryParse(objParameter.FieldValue1, out _PayeeNameOnEmployeePayment);
                        break;

                    case "DigitalInvoicing":
                        bool.TryParse(objParameter.FieldValue1, out _DigitalInvoicing);
                        break;

                    case "isFiller":
                        bool.TryParse(objParameter.FieldValue1, out _isFiller);
                        break;

                    case "IsTSM":
                        bool.TryParse(objParameter.FieldValue1, out _IsTSM);
                        break;

                    case "IsVehicleInCapsAndDropdownBydefault":
                        bool.TryParse(objParameter.FieldValue1, out _IsVehicleInCapsAndDropdownBydefault);
                        break;

                    case "IsShowYardLineNo":
                        bool.TryParse(objParameter.FieldValue1, out _IsShowYardLineNo);
                        break;

                    case "IsShowweighbridgeNumber":
                        bool.TryParse(objParameter.FieldValue1, out _IsShowweighbridgeNumber);
                        break;


                    case "printtokenfromexe":
                        bool.TryParse(objParameter.FieldValue1, out _printtokenfromexe);
                        break;

                    case "printcprfromexe":
                        bool.TryParse(objParameter.FieldValue1, out _printcprfromexe);
                        break;

                    case "SetCenterCodeLength":
                        bool.TryParse(objParameter.FieldValue1, out _SetCenterCodeLength);
                        break;

                    case "SalesTaxAmountCalAdam":
                        bool.TryParse(objParameter.FieldValue1, out _SalesTaxAmountCalAdam);
                        break;

                    case "BankBranchinDocumentType":
                        bool.TryParse(objParameter.FieldValue1, out _BankBranchinDocumentType);
                        break;

                    case "sroScheduleNo":
                        _sroScheduleNo = objParameter.FieldValue1;
                        break;

                    case "sroItemSerialNo":
                        _sroItemSerialNo = objParameter.FieldValue1;
                        break;

                        case "IsTSMLCane":
                        bool.TryParse(objParameter.FieldValue1, out _IsTSMLCane);
                        break;

                    case "IsGrossWithAllTypesForArrears":
                        bool.TryParse(objParameter.FieldValue1, out _IsGrossWithAllTypesForArrears);
                        break;
                    
                    case "IsInvoicePostWithBuyerName":
                        bool.TryParse(objParameter.FieldValue1, out _IsInvoicePostWithBuyerName);
                        break;

                    case "ArearAmountInGross":
                        bool.TryParse(objParameter.FieldValue1, out _ArearAmountInGross);
                        break;
                }
            }
        }

        public static bool UpdateDataByFieldName(string fieldName, string fieldValue1)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@FieldName", fieldName);
                sqlparams[1] = new SqlParameter("@FieldValue1", fieldValue1);

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_By_FieldName, sqlparams) > 0)
                {
                    SetApplicationVariables();
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
