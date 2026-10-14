using System;
using System.Data;

using FnI.Core.Web.HttpApplicationState;
using System.ComponentModel;

namespace FnI.Core.Web
{
    public class EnumFnISystem
    {
        public static DataTable BindToEnum(Type enumType)
        {
            DataTable dt = new DataTable();

            string[] names = Enum.GetNames(enumType);
            Array values = Enum.GetValues(enumType);

            dt.Columns.Add(new DataColumn("key", typeof(string)));
            dt.Columns.Add(new DataColumn("value", typeof(string)));

            for (int i = 0; i < names.Length; i++)
            {
                DataRow row = dt.NewRow();
                row["key"] = Convert.ToInt16(values.GetValue(i)).ToString();
                row["value"] = names[i];
                dt.Rows.Add(row);
            }

            return dt;
        }

        //////
        //  In case of new addition, place the newly item in sorted order (alphabetically) but assign a new number. 
        //  (Warning) Do not change numbering of previous items due to change in sorting order, since these values are saved in the database.
        //////

        public enum EmploymentType
        {
            Regular = 1,
            Temporary = 2,
            Seasonal = 3,
            off_Seasonal = 4
            // SeasonalDailyWages = 5,
            //Trainee = 6
        }

        public enum AlAbbas_EmploymentType
        {
            Permanent_Worker = 1,
            Temporary_Worker = 2,
            Permanent_Staff = 3,
            Temporary_Staff = 4,
            off_Seasonal = 5
            // SeasonalDailyWages = 5,
            //Trainee = 6
        }

        public enum RelationShip
        {
            Select = 0,
            Son = 1,
            Daughter = 2,
            Wife = 3,
            Husband = 4,
            Father = 5,
            Mother = 6,
            Sister = 7,
            Brother = 8,
        }

        public enum EmployeeStatusType
        {
            Permanent = 1,
            Contract = 2,
            Seasonal = 3,
            Retainee = 4,
            Temporary = 5
            //SeasonalContract = 6
        }

        public enum LocationType
        {
            HO = 1,
            BR = 2,
            Plant = 3,
            Warehouse = 4
        }

        public enum SalaryProcessPointType
        {
            DaysBeforeEOM = 1,
            OnDate = 2,
        }

        public enum AccumulatedLeaveType
        {

            Annual = 1,
            Quarter = 2,
        }

        public enum InspectionTypeCombo
        {
            Commercial = 1,
            FourPoint = 2
        }

        public enum SalaryByCalender
        {

            Calender = 1,
            Fiscal = 2,

        }

        public enum SandwichLeave
        {

            Before = 1,
            After = 2,
            Both = 3

        }


        public enum VisaFor
        {
            Select = 0,
            International = 1,
            Local = 2,
            Domestic = 3
        }

        public enum VisaType
        {
            Select = 0,
            Education = 1,
            Labour = 2,
            Visit = 3,
            Medical = 4,
            Business = 5
        }

        public enum Qualification
        {
            MatriculationOrEquivalent = 1,
            IntermediateOrEquivalent = 2,
            GraduationOrEquivalent = 3,
            PostGraduationOrEquivalent = 4,
            DoctorateOrEquivalent = 5
        }

        public enum noteTypes
        {

            Profit = 1,
            Loss = 2
        }

        public enum TimeOff
        {
            LateArrival = 1,
            EarlyRelease = 2,
            Break = 3,
        }

        public enum TimeOffForAlHaj
        {
            LateArrival = 1,
            EarlyRelease = 2,
            Break = 3,
            CasualHalf = 4,
            SickHalf = 5,
            InjuryLeave = 6
        }

        /// <summary>
        /// 
        /// ///////////////////////////
        /// </summary>
        public enum ActionTypeAS
        {
            Add_Labor = 3,
            Consumable = 4,
            Parts = 1,
            Sublet = 2

        }

        public enum BusinessPartner
        {
            ShipTo = 16,
            Agent_Dealer = 14,
            Billed_to = 15,
            Advances = 16,
            Broker = 4,
            Carriage_Contractors = 5,
            Cashier = 13,
            Customer = 2,
            Drivers = 6,
            Employee = 7,
            Grower = 8,
            Loading_Contractor = 9,
            Service_Providers = 10,
            SisterConcern = 12,
            Transporter = 3,
            Unloading_Contractor = 11,
            Vendor = 1,
            LogisticVendor = 19,
            ClearingAgent = 20,
            // International_Vendor=30,
            International_Vendor = 92,
            Stitching_Contractor = 31,
            GST = 46,
            LETTER_OF_CREDIT = 32,

            LocalSaleDebtor_Sugar = 69,
            LocalSaleDebtor_Molasses = 103,
            LocalSaleDebtor_Alcohol = 104,
            LocalSaleDebtor_Bagasse = 105,
            LocalSaleDebtor_Textiles = 106,
            LocalSaleDebtor_Terminal = 107,
            LocalSaleDebtor_CO2 = 108,
            LocalSaleDebtor_Scrap = 115,
            LocalSaleDebtor_Mud = 116,
            LocalSaleDebtor_Ethanol = 104,
            AdvancefromCustomers_ethanol = 203,

            AdvancefromCustomers_Export = 75,
            AdvancefromCustomers_Sugar = 76,
            AdvancefromCustomers_Other = 109,
            AdvancefromCustomers_Alcohol = 110,
            AdvancefromCustomers_Bagasse = 111,
            AdvancefromCustomers_Textiles = 112,
            AdvancefromCustomers_Terminal = 113,
            AdvancefromCustomers_CO2 = 114,
            AdvancetoSupplierTransporter = 62,
            AdvanceGST = 200,
            CustomerParty = 201,
            Provident_Fund_Loan = 202,
            ShortTermInvestment = 204,
            LongTermInvestment = 205,


            [Description("Unrealized Gain / (Loss)")]
            UnrealizedGainandLoss = 206,

            [Description("13% of 20% with held on services (SRB)")]
            ThirteenofTwentywithheldonservicesSRB = 207,

            [Description("With held 13% on services (SRB)")]
            WithheldThirteenonservicesSRB = 208,

            MarketCommitteeFee = 209,

            AdvancefromCustomersTrading_CO2 = 210,
            LocalSaleDebtorTrading_CO2 = 211,
            SalestaxOutputTrading_CO2 = 212,
            DailyWages = 213,
            EmployeeProvidendFund = 214,
            SpecialLoanInterestBased = 215,
            SST = 216,
            AdvancefromCustomersTrading_Sugar = 217,
            LocalSaleDebtorTrading_Sugar = 218,
            SalestaxOutputTrading_Sugar = 219,

            Profiton_Mem_Contribution = 220,
            Company_Contribution = 221,
            MemberContribution = 222,
            Short_Term_Creditors_Members = 223,
            UnclaimedDividend = 224,
            Advance_Tax = 225,
            FORFIETURESANDLAPSESINCOME = 226,
            AdvancefromCustomers_Molasses = 227,

            //NonRefundableWithdrawal = 228,
            //FinalSettlement = 229,
            //ExEmployee = 230

            Short_Term_Creditors_Company = 228,
            NRW_Mem_Contribution = 229,
            NRW_Co_Contribution = 230,
            FF_Mem_Contribution = 231,
            FF_Co_Contribution = 232,
            Profit_on_Co_Contribution = 233,
            ShortTermCreditorsInterest = 234,
            Short_Term_Creditors_Payable = 235,
            WithheldFifteenonservicesSRB = 236,

            [Description("WithheldonservicesSRB 15%")]
            WithheldonservicesSRB_15 = 237,

            [Description("WithheldonservicesSRB 20%")]
            WithheldonservicesSRB_20 = 238,

            [Description("Local Sale Debtor - Fertilizer")]
            LocalSaleDebtorFertilizer = 239,

            [Description("HOD LOAN DEDUCTION")]
            HOD_LOAN_DEDUCTION = 240,

            [Description("OFFICERS LOAN DEDUCTION")]
            OFFICERS_LOAN_DEDUCTION = 241,

            [Description("WORKERS LOAN DEDUCTION")]
            WORKERS_LOAN_DEDUCTION = 242,

            [Description("SALARY ADVANCE")]
            SALARY_ADVANCE = 243,

            // for RSM //
            [Description("Advance From Customer - Calcium Carbide")]
            AdvanceFromCustomer_CalciumCarbide = 244,

            [Description("Advance From Customer - DA Gases")]
            AdvanceFromCustomer_DAGases = 245,

            [Description("Advance From Customer - Molasses")]
            AdvanceFromCustomer_Molasses = 246,

            [Description("Local Sale Debtor - Calcium Carbide")]
            LocalSaleDebtor_CalciumCarbide = 247,

            [Description("Local Sale Debtor -DA Gases")]
            LocalSaleDebtor_DAGases = 248,


            [Description("Advance to Supplier (Production Sugar)")]
            AdvancetoSupplier_ProductionSugar = 249,

            [Description("Advance to Supplier (Production Calcium Carbide)")]
            AdvancetoSupplier_ProductionCalciumCarbide = 250,

            [Description("Advance to Supplier (Production DA Gases)")]
             AdvancetoSupplier_ProductionDAGases = 251,

            [Description("Advance to Supplier (Repair Maintenance)")]
            AdvancetoSupplier_RepairMaintenance = 252,

            [Description("Advance to Supplier (Consumable)")]
            AdvancetoSupplier_Consumable = 253,

            [Description("Advance to Supplier (Chemical)")]
            AdvancetoSupplier_Chemical = 254,

            [Description("Advance to Supplier (Packaging)")]
            AdvancetoSupplier_Packaging = 255,

            [Description("Advance to Supplier (General Item)")]
            AdvancetoSupplier_GeneralItem = 256,

            [Description("Provision For Doubtful Debts - Calcium Carbide")]
            ProvisionForDoubtfulDebts_CalciumCarbide = 257,

            [Description("Provision For Doubtful Debts - DA Gases")]
            ProvisionForDoubtfulDebts_DAGases = 258,


            [Description("Vendor (Production Sugar)")]
            VendorProduction_Sugar = 259,

            [Description("Vendor (Production Calcium Carbide)")]
            VendorProduction_CalciumCarbide = 260,

            [Description("Vendor (Production DA Gases)")]
            VendorProduction_DAGases = 261,

            [Description("Vendor (Repair Maintenance)")]
            Vendor_RepairMaintenance = 262,

            [Description("Vendor (Consumable)")]
            Vendor_Consumable = 263,

            [Description("Vendor (Chemical)")]
            Vendor_Chemical = 264,

            [Description("Vendor (Packaging)")]
            Vendor_Packaging = 265,

            [Description("Vendor (General Item)")]
            Vendor_GeneralItem = 266,

            [Description("Local Sales Debtor - Other")]
            Local_Sales_Debtor_Other = 267,

            [Description("Investment")]
            Investment = 268,

            [Description("InterCompany")]
            InterCompany = 269,

            //////////////  RSM  /////////////

            [Description("18% SALES TAX RECIEVABLE INPUT")]
            Sales_Tax_18_Percent_Recievable_Input = 270,

            [Description("20% WITHHOLDING SIND TAX")]
            WithHolding_20_Percent_Sind_Tax = 271,

            [Description("SALES TAX WITH. 80% AGAINST COKE")]
            Sales_Tax_With_18_Percent_Against_Coke = 272,

            [Description("WITH. TAX AGAINST EXEMPTION")]
            With_Tax_Against_Exemption = 273,

            [Description("WITH.TAX 153 CONTRACT (FILER Co 7.5%)")]
            With_Tax_153_Contract_Filer_Co_7_Percent = 274,

            [Description("WITH.TAX 153 CONTRACT (FILER Ind 8%)")]
            With_Tax_153_Contract_Filer_Co_8_Percent = 275,

            [Description("WITH.TAX 153 CONTRACT (Non FILER Co 15%)")]
            With_Tax_153_Contract_NonFiler_Co_15_Percent = 276,

            [Description("WITH.TAX 153 CONTRACT (Non FILER Ind 16%)")]
            With_Tax_153_Contract_NonFiler_Co_16_Percent = 277,

            [Description("WITH.TAX 153 GOODS (FILER Co.5%)")]
            With_Tax_153_Goods_Filer_Co_5_Percent = 278,

            [Description("WITH.TAX 153 GOODS (FILER Ind.5.5%)")]
            With_Tax_153_Goods_Filer_Co_5_Half_Percent = 279,

            [Description("WITH.TAX 153 GOODS (Non-FILER Co 10%)")]
            With_Tax_153_Goods_NonFiler_Co_10_Percent = 280,

            [Description("WITH.TAX 153 GOODS (Non-FILER Ind 11%)")]
            With_Tax_153_Goods_NonFiler_Co_11_Percent = 281,

            [Description("WITH.TAX 153 SERVICES (FILER Co.9%)")]
            With_Tax_153_Services_Filer_Co_9_Percent = 282,

            [Description("WITH.TAX 153 Services (Non-FILER Ind 18%)")]
            With_Tax_153_Services_NonFiler_Ind_18_Percent = 283,

            [Description("WITH.TAX 153 SERVICES(FILER Ind.11%)")]
            With_Tax_153_Services_Filer_Ind_11_Percent = 284,

            [Description("WITH.TAX 153 SERVICES(NONFILER Ind.22%)")]
            With_Tax_153_Services_NonFiler_Ind_22_Percent = 285,

            [Description("WITH.TAX 153 SPECIFIED SERVICES(FILER Ind.4%)")]
            With_Tax_153_Specified_Services_Filer_Ind_4_Percent = 286,

            [Description("WITH.TAX 153 SPECIFIED SERVICES(NONFILER Ind.8%)")]
            With_Tax_153_Specified_Services_NonFiler_Ind_8_Percent = 287,

            [Description("WITHHOLDING TAX  SRB 8%")]
            WithHolding_Tax_SRB_8_Percent = 288,

            [Description("WITHOLDING TAX S-236G/H SUGAR (Non Active)")]
            WithHolding_Tax_S_236GH_Sugar_NonActive = 289,

            [Description("WITHOLDING TAX S-236G/H SUGAR 0.1% (ACTIVE)")]
            WithHolding_Tax_S_236GH_Sugar_Active = 290,

            [Description("WITHOLIDING TAX FBR 0.25% (Imp)")]
            WithHolding_Tax_FBR_Imp = 291,

            [Description("WITHOLIDING TAX SRB 15%")]
            WithHolding_Tax_SRB_15_Percent = 292,

            [Description("Local Sale Debtor - Ferroalloys")]
            LocalSaleDebtor_Ferroalloys = 293,

            [Description("Advance from Customers- Ferroalloys")]
            AdvancefromCustomers_Ferroalloys = 294,

            [Description("Advance from Customers- DA Gases")]
            AdvancefromCustomers_DAGases = 295,


            Director = 296


            
                
        }

        public enum LabTestType
        {
            BaseOil = 1,
            BlendsOil = 2,
            WasteOil = 3
        }

        public enum Season
        {
            Summer = 1,
            Winter = 2,
        }

        public enum TransactionType
        {
            Positive = 1,
            Negative = 2
        }

        public enum TypeName
        {
            Supplier = 1,
            //TestName = 2,
            Standard = 3,
            Method = 4,
            //Result = 5

        }

        public enum CaneRateType
        {
            AfterDeductionAmount = 16,
            BeforeDeductionAmount = 13,
            BarrierNumber = 15,
            Center = 4,
            Circle = 3,
            CPR = 10,
            DateAndTime = 7,
            District = 17,
            Grower = 5,
            GrowerGroup = 19,
            Haulage = 14,
            Indent = 8,
            Kilometers = 18,
            NoneVariety = 12,
            Standard = 1,
            Token = 6,
            Variety = 11,
            Weight = 9,
            Zone = 2,
            ELPDateAndTime = 20,
        }

        public enum Closed
        {
            Closed = 1,
            ClosedForInvoice = 2,
            ClosedForReceiving = 3,
            FinallyClosed = 4,
            Frozen = 5,
            NotOnHold = 6,
            NotReserved = 7,
            OnHold = 8,
            Open = 9,
            Printed = 10,
            Reserved = 11,
            Unfrozen = 12,
            PartialReceived = 13,
            PartialReturn = 14
        }

        public enum ContratorCode
        {
            X = 1,
            y = 2,
            Z = 3
        }

        public enum BarrierNo
        {
            x = 1,
            y = 2,
            z = 3
        }

        public enum CostOfAsset
        {
            Consultancy = 1,
            Erection = 2,
            Freight_Cost = 3,
            Installation_Charges = 4,
            Invoice_Cost = 5,
            Unallocated_Expenses = 6
        }

        public enum CurrentStatus
        {
            InProcess = 5,
            ReleaseForApprove = 12,
            PartiallyApproved = 13,
            Approved = 1,
            Discuss = 14,
            Cancelled = 15,
            Reversed = 16,
            Closed = 25,
            //NoOrder=17
        }

        public enum Facilitylimit
        {
            PPTFC = 1,
            SyndicateLoan = 2,
            CashFinance = 3,
            RunningFinance = 4,
            FinanceAgeTrustReceipt = 5,
            DiscountingOfLocalBills = 6,
            BookWisetotalFundedFacilities = 7,
            LetterOfCredit = 8,
            LetterOfGuarntee = 9,
            OtherLongTermLoans = 10,
            ISTISNA_TIJARAH = 11,
            ISTISNA_TIJARAH_1 = 12,
            ISTISNA_TIJARAH_2 = 13,
            SLC_ULC = 14,
            SALAM_PLEDGE = 15,
            SALAM_HYPO = 16,
            LG_ADAVNCEPAYMENT_GUARNTEE_REGULAR = 17,
            LG_ADVANCEPAYMENT_GUARNTEE_OTT = 18,
            ISLAMIC_TRANSACTIONAL_COMMODITIESFINANCE = 19,
            MARHABA = 20,



        }

        public enum LeaseType
        {
            Ijarah = 1,
            Musharka = 2,
            LeaseFinancing = 3,
        }


        public enum Facility
        {
            Mobile = 1,
            Fuel = 2,
            Medical = 3,
            vehicleRepair = 4,
            ElectricityUnits = 5,
            HouseRent = 6,
            Utility = 7,
            SalaryPad = 8,
            Scholarship = 9,
            Other = 10,
        }


        public enum MarkupBasis
        {
            Monthly = 1,
            Quarterly = 2,
            HalfYearly = 3,
            Yearly = 4,

        }

        public enum QuarterTypes
        {
            First_Quarter = 1,
            Second_Quarter = 2,
            Third_Quarter = 3,
            Fourth_Quarter = 4

        }


        public enum AssetType
        {
            Valuator = 1,
            InHouse = 2
        }

        public enum DepreciationMethod
        {
            Declining_Balance = 1,
            Straight_Line = 2
        }

        public enum DeductionType
        {
            Rate = 1,
            Weight = 2,
        }

        public enum FreightTerms
        {
            Delivered = 1,
            ExMill = 2
        }


        public enum FreightTerms_TW
        {
            Delivered = 1,
            ExMill = 2,
            FOB = 3,
            CFR = 4,
            CIF = 5
        }

        public enum Frequency
        {
            Monthly = 1,
            Yearly = 2,
            Quarterly = 3
        }

        public enum GrowerPaymentMode
        {
            BankCheque = 1,
            BankScroll = 2,
            BankAdvice = 3,
            Cash = 4,
            Commodities = 5
        }

        public enum EmployeePaymentMode
        {
            BankCheque = 1,
            Cash = 2,

        }

        public enum WeeklyReportParameters
        {
            PunjabSugarInd = 1,
            SindhSugarInd = 2,
            Address = 3,
            SrLabOficer = 4,
            ProductionManager = 5,
            GeneralManager = 6
        }

        public enum GrowerType
        {
            Depot = 1,
            Farm = 2,
            Grower = 3,
            Broker = 4,
            Vip = 5,
        }

        public enum HaulageType
        {
            Cart = 1,
            Trailor = 2,
            Trolly = 3,
            Truck = 4,
            LongTrailor = 5,
            CART2 = 6
        }

        public enum ShiftingThrough
        {
            Select = 0,
            ConveyorBelt = 1,
            TwoWheel = 2,
            Tractor = 3,
            Pipeline = 4
        }

        public enum InvoiceType
        {
            CPR = 28,
            CreditMemo = 1,
            DebitMemo = 2,
            EmployeeExpenseReport = 3,
            InterestInvoice = 4,
            MixedTypeInvoice = 11,
            PODefaultStandardInvoice = 6,
            PrepaymentsandAdvances = 7,
            QuickMatchStandardInvoice = 8,
            StandardInvoice = 9,
            WithholdingTaxInvoice = 10,
            Purchase = 5

        }

        public enum LoanDeductionMode
        {
            AgainstPayment = 1,
            AgainstLoan = 2
        }

        public enum LoanType
        {
            Advance = 1,
            Loan = 2,
            Security
        }

        public enum LoanFor
        {
            Grower = 1,
            Transporter = 2,
            LoadingContractor = 3,
            UnloadingContractor = 4
        }

        public enum LoanFrequency
        {
            EveryMonth = 1,
            Every2Months = 2,
            Every6Months = 3,
            EveryYear = 4

        }

        public enum LoanTerm
        {
            Deduction = 1,
            DirectPay = 2

        }

        public enum Month
        {
            January = 1,
            Febuary = 2,
            March = 3,
            April = 4,
            May = 5,
            June = 6,
            July = 7,
            August = 8,
            September = 9,
            October = 10,
            November = 11,
            December = 12
        }

        public enum NoOfVehicle
        {
            One = 1,
            Two = 2,
            //Three = 3, this was committed because there are two fields on Token Page
        }

        // There is a table name SET_ReceiptMethod. Kindly Use that table instead of this Enum.
        public enum PaymentMode
        {
            Bank = 1,
            Cash = 2
        }

        public enum PaymentTypes
        {
            Grower = 1,
            CarrageContractor = 2,
            LoadingContractor = 3,
            UnLoadingContractor = 4,
            Transporter = 5
        }

        public enum PaymentTypesAS
        {
            Cash = 1,
            Credit = 2,
            Goodwill = 3,
            Staff_Vehicle = 4
        }

        public enum PaymentTypeDispatch
        {
            Cash = 1,
            Credit = 2
        }

        public enum PaymentPerson
        {
            Cashier = 1
        }

        public enum PeriodTypes
        {
            Day = 1,
            Month = 2,
            Quarter = 3,
            Year = 4
        }

        public enum PhysicalCondition
        {
            Lease = 1,
            Owner = 2
        }

        public enum PossessionType
        {
            Lease = 1,
            Owner = 2
        }

        public enum POType
        {
            Agreement = 1,
            BlanketPO = 2,
            StandardPO = 3,
            Indent = 4,
            Leakage = 5
        }

        public enum PriceType
        {
            Discounted = 1,
            ListPrice = 2,
            Negotiated = 3
        }

        public enum Province
        {
            Balochistan = 1,
            KPK = 2,
            Punjab = 3,
            Sindh = 4,
            Other = 5
        }

        public enum TaxationStatus
        {
            Final = 1,
            Adjustable = 2
        }

        public enum Quaterly
        {
            FirstQuater = 13,
            SecondQuater = 14,
            ThirdQuater = 15,
            ForthQuater = 16
        }

        public enum RateCalculate
        {
            NetWeight = 1,
            PayableWeight = 2,
            AmountPayable = 3,
            FixedDeductionOnGrossPayableAmount = 4
        }

        public enum RateImpact
        {
            Add = 1,
            Subtract = 2,
            NoImpact = 3
        }

        public enum PayrollItemImpact
        {
            Add = 1,
            Subtract = 2,
        }

        public enum RateType
        {
            DemandDraft = 1,
            Ready = 2,
            TelegraphicTransfer = 3,
            Cash = 4
        }

        public enum RateTypeDataType
        {
            DateTime = 1,
            NumericWithoutDecimal = 2,
            NumericWithDecimal = 3,
            String = 4
        }

        public enum RateTypeDataTypeForCodeBehind
        {
            DateTime = 1,
            NumericWithoutDecimal = 2,
            NumericWithDecimal = 3,
            String = 4,
            LookUpZone = 5,
            LookUpCenter = 6,
            LookUpCircle = 7,
            LookUpDistrict = 8,
            LookUpVariety = 9,
            LookUpNoneVariety = 10,
            LookUpHaulage = 11,
            LookUpGrower = 12,
            LookUpGrowerGroup = 13,
            LookUpSubZone = 14,
        }

        public enum Reason
        {
            Accident = 1,
            Actual = 2,
            Budget = 3,
            Insurance = 4,
            Others = 5,
            Planned = 6
        }

        public enum Reservation
        {
            CompiledDemand = 1,
            ExpiredLot = 2,
            Forecast = 3,
            HardReservation = 4,
            IndependentDemand = 5,
            InterorganizationOrder = 6,
            IntransitShipmentScrap = 7,
            Manual = 8,
            OtherIndependentDemand = 9,
            PlannedOrder = 10,
            PlannedOrderScrap = 11,
            POInReceivingScrap = 12,
            PurchaseOrderScrap = 13,
            PurchaseRequisitionScrap = 14,
            SalesOrder = 15,
            ScheduleDemand = 16
        }

        public enum SalesOrder_Closed
        {
            Closed = 1,
            ClosedForInvoice = 2,
            ClosedForDelivery = 3,
            FinallyClosed = 4,
            Frozen = 5,
            NotOnHold = 6,
            NotReserved = 7,
            OnHold = 8,
            Open = 9,
            Printed = 10,
            Reserved = 11,
            Unfrozen = 12,
            ClosedForDispatch = 13
        }

        public enum Shifts
        {
            A = 1,
            B = 2,
            C = 3,
            G = 4
        }

        public enum ShipType
        {
            ByAir = 1,
            ByRoad = 2,
            BySea = 3
        }

        public enum SurveyCode
        {
            First = 1,
            Second = 2,
            Third = 3
        }

        public enum Loan
        {
            LoanSanction = 1,
            LoanRecovery = 2,
            LoanInterestSanction = 3,
            LoanInterestRecovery = 4
        }

        public enum TransferType
        {
            AssemblyCompletion = 1,
            AssemblyReturn = 2,
            CostUpdate = 3,
            CycleCountAdjustment = 4,
            DeliveryAdjustments = 5,
            DirectOrganizationTransfer = 6,
            IntransitReceipt = 7,
            IntransitShipment = 8,
            IssueFromStores = 9,
            PhysicalInventoryAdjustment = 10,
            ReceiptIntoStores = 11,
            StagingTransfer = 12,
            SubinventoryTransfer = 13,
            WIPScrapTransaction = 14
        }

        public enum Year
        {
            Yearly = 17
        }

        public enum tendumNo
        {
            One = 1,
            Three = 3
        }

        public enum tendumNoTwoStar
        {
            One = 1,
            Two = 2
        }

        public enum ServicePerformanceStatus
        {
            Approved = 1,
            AwaitingParts = 7,
            Completed = 4,
            Inprocess = 3,
            Rejected = 2,
            Others = 5,
            UnderObservation = 6
        }

        public enum PayrollType
        {
            Allowance = 1,
            Deduction = 2,
            Contribution = 3,
            Leave = 4,
            Loan = 5,
            BasicSalary = 1,
            ShortLeave = 7,
            UnitOfMeasure = 8,
            OverTime = 9,
            MonthlyDeductionOrAllownces = 10,
            Bonus = 11

        }


        public enum ItemSourceType
        {
            ImportedParts = 1,
            LocalParts = 2,
            PrimeMover = 3,
            CargoTruck = 4,
            DumpTruck = 5,
            XPV = 6,
            CARRIER = 7,
            DoubleCabin = 8,
            Defects = 9,
            Services = 10,
            Local = 11,
            Labour = 12



        }


        public enum ExpiryAfter
        {
            Days = 1,
            Weeks = 2,
            Months = 3,
            Years = 4

        }

        public enum FAPeriodType
        {

            Life = 1,
            Warranty = 2
        }

        public enum DeductionMode
        {
            Once = 1,
            Monthly = 2,
            Yearly = 3,
        }

        public enum Salaytype
        {
            Monthly = 1,
            BiMonthly = 2,
            DailyWadges = 3,
            Weekly = 4
        }

        public enum Taxes
        {
            AdditionalTax = 1,
            ServiceItemTax = 2,
            SuppliesTax = 3,
            TranspoterTax = 4,

        }
        public enum IPCameraFormName
        {
            FirstWeight = 1,
            SecondWeight = 2,
            TareWeight = 3,
            GrossWeight = 4,

        }

        public enum DiscFrequency
        {
            Instant = 1,
            InvoiceWise = 2,
            DayWise = 3,
            Weekly = 4,
            Monthly = 5,
            Quarterly = 6,
            Yearly = 7,

        }


        public enum LaonType
        {
            RedeemableCapital = 1,
            LongTermLoans = 2,

            ShortTermLoans = 3,
        }


        public enum AssetTransferType
        {
            Transfer = 1,
            Recieve = 2,
        }

        public enum Status
        {
            Idle = 1,
            Dispose = 2,
            NotDispose = 3,
            Scrap = 4,
            Discard = 5,
        }

        public static string BindingWeight()
        {
            if (!string.IsNullOrEmpty(AppParameters.NameForCaneModule) && AppParameters.NameForCaneModule.ToLower().Equals("beet"))
                return "0.05";
            else
                return "0.00625";
        }

        #region Fabrics

        public enum DyeType
        {
            Wash = 1,
            PFD = 2,
            Reactive = 3,
            Disperse = 4,
            Acid = 5,
            Basic = 6,
            Vat = 7,
            Disperse_Reactive = 8,
            Disperse_Acid = 9,
            Basic_Disperse = 10,
            Disperse_Vat = 11,
            Disperse_Bleach = 12,
            White_Single = 13,
            White_Double = 14,
            Bleach = 15,
            Half_Bleach = 16,
        }

        public enum RecipeType
        {
            BeforeApproval = 1,
            AfterApproval = 2,
            InternalApproval = 3,
        }

        public enum DecisionStatus
        {
            Approved = 1,
            Rejected = 0,
            Canceled = 2
        }

        public enum DecisionBy
        {
            Customer = 1,
            LabDipSection = 2,
            ProductionDept = 3,
        }

        public enum FabricFoam
        {
            Tubler = 1,
            OpenWidth = 2
        }

        public enum ProcessType
        {
            OnePortion = 1,
            TwoPortion = 2,
        }

        public enum FarmulationType
        {
            Chemical = 1,
            ReactiveDye = 2,
            DisperseDye = 3,
        }

        public enum DelaySection
        {
            Both = 1,
            Dyeing = 2,
            Finishing = 3
        }

        public enum ReversingPlating
        {
            Yes = 1,
            No = 0
        }

        public enum YarnType
        {

            Carded = 1,
            Combed = 2,
        }

        public enum measurement
        {
            m = 1,
            cm = 2,
            inch = 3,
        }

        public enum ProcessMasterType
        {
            MainProcess = 1,
            SubProcess = 2,

        }

        public enum FieldTypes
        {
            Mandatory = 1,
            Optional = 2,
            Alternate = 3,

        }

        public enum StageOfNonConformity
        {

            IncomingInspection = 1,
            Customer = 2,
            Dyeing = 3,
            Dryer = 4,
            Compaction = 5,
            Sueding = 6,
            Stentering = 7,
            Calendering = 8,
            FinalInspection = 9,
            Padder = 10,
            Raising = 11,
            InprocessInspection = 12,
        }

        public enum DecidedBy
        {
            Customer = 1,
            Internal = 2,
        }

        public enum ShadeStatus
        {
            OK = 1,
            HOLD = 2,
            SUBMITTED = 3,
        }

        public enum ComplaitArea
        {
            Customer = 1,
        }

        public enum LotNo
        {
            Normal = 1,
            Return = 2,
        }

        public enum ShrinkageType
        {
            AfterDryer = 1,
            AfterCompaction = 2,
        }

        public enum TestingStandard
        {
            HWF_Dyes = 1,
            Normal_Dyes = 2,
            //Disburse = 3,
        }

        public enum MatchWith
        {
            STD = 1,
            Spectral_Values = 2,
            Pantone_TC_TCX_C_U_TP_TPX = 3,
            QTX = 4,
        }

        public enum InspectionType
        {
            Fabric = 1,
            Rib = 2,
            TwillTape = 3,
            CollarCuff = 4,
            Other = 5,

        }

        public enum LotRateType
        {
            Flat = 1,
            Process = 2,
        }

        public enum FabricDepartment
        {
            Lab = 1,
            Dyeing = 2,
            Finishing = 3,
            Delivery = 4,
            Greige = 5,
            QC = 6,
        }

        public enum DyeingReceipeType
        {
            NormalRecipe = 1,
            AdditionalRecipe = 2,
        }

        public enum ReportFor
        {
            Dyeing = 1,
            Finishing = 2,
            Both = 3,
        }

        public enum FinalInspectionStatus
        {
            Accept = 1,
            Reject = 2,
            Hold = 3,
        }


        public enum InvoceFor
        {
            Customer = 1,
            SRO = 2,
            InComeTAX = 3,
        }

        public enum JobEndingStatus
        {
            Terminated = 1,
            Defaulter = 2,
            Resigned = 3,
            Died = 4,
            Retired = 5,
        }
        public enum RebatePolicy_Status
        {
            Open = 1,
            Closed = 2,

        }
        #endregion

        public enum MessType
        {
            GuestHouse = 1,
            OfficersMessSubisdy = 2,
            SupervisorMessSubisdy = 3,
            WorkersMessSubisdy = 4,

        }

        public enum SeasonForTransaction
        {
            Season = 1,
            OffSeason = 2,
        }

        public enum ActivityType
        {
            Own = 1,
            Contact = 2,
        }

        public enum Product
        {
            Mud = 1,
            Bagasse = 2,
            Ash = 3,
        }

        public enum ExpensesType
        {
            ContractualExpense = 1,
            OwnTractorFuel = 2,
            DailyWages = 3,
        }

        public enum UtilityType
        {
            Electricity = 1,
            Gas = 2,
            Water = 3,
        }

        public enum UtilityTypeForElectricity
        {
            Electricity = 1,
        }

        public enum LCRetirmentOption
        {
            Self = 1,
            FATR = 2,
            Usance = 3,

        }

        public enum JobCategory
        {
            CWIP = 1,
            Receivable = 2,
            Capitalization = 3,
            Expense = 4
        }
        public enum Brixs
        {
            Fresh = 1,
            Mixs = 2,
            Stale = 3,
            TopTrash = 4,
            WithoutELP = 5,
        }
        public enum GrainSize
        {
            Select = 0,
            Medium = 1,
            Bold = 2,
            Fine = 3,
            NA = 4,
        }
        public enum Packing
        {
            Select = 0,
            With_InnerLiner = 1,
            Without_InnerLiner = 2,
            NA = 3
        }
        public enum Nature
        {
            RelatedParty = 1,
            AssociateCompany = 2,
            SubsidiaryCompany = 3
        }
        public enum DemandType
        {
            BMR = 1,
            Repair_Maintenance = 2,
            General = 3,
            Agri_Related = 4,
            Improvement = 5,
            CWIP = 6
        }
        public enum SeasonType
        {
            Season = 1,
            Off_Season = 2
        }

        public enum WeightBy
        {
            [Description("Mill Weight")]
            MillWeight = 1,

            [Description("Party Weight")]
            PartyWeight = 2,
        }

        public enum InCoTerms
        {
            CNF = 1,
            EX_Works = 2,
            FOB = 3
        }

        public enum PaymentModeConnect
        {
            DA = 1,
            DP = 2,
            ADV = 3
        }

        public enum PartiallyPament
        {
            Allowed = 1,
            Not_Allowed = 2
        }

        public enum PlantationType
        {
            Good = 1,
            Fair = 2,
            Poor = 3
        }
    }
}
