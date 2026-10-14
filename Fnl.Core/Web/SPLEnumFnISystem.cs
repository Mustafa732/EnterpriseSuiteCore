using System;
using System.Data;

namespace FnI.Core.Web
{
    public class SPLEnumFnISystem
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

        public enum BusinessPartner
        {
            Broker = 4,
            Carriage_Contractors = 5,
            Cashier = 12,
            Customer = 2,
            Drivers = 6,
            Employee = 7,
            Grower = 8,
            Loading_Contractor = 9,
            Service_Providers = 10,
            Transporter = 3,
            Unloading_Contractor = 11,
            Vendor = 1
        }

        public enum Cutting
        {
            Die_Cutting = 1,
            Polar_Cutting = 2,
            No_Cutting = 3
        }

        public enum CurrentStatus
        {
            InProcess = 5,
            ReleaseForApprove = 12,
            PartiallyApproved = 13,
            Approved = 1,
            Discuss = 14
        }

        public enum CurrentJobStatus
        {
            // These fields define the process sequence

            OffsetDivisionCompleted = 1,
            OffsetPlanningCompleted = 2,
            OffsetPrintingCompleted = 3,
            DieCuttingCompleted = 4,
            BreakingCompleted = 5,
            PastingCompleted = 6,
            PackingCompleted = 7,
            FinishedGoodsCompleted = 8,
            GravureDivisionCompleted = 11,
            GravurePlanningCompleted = 12,
            GravurePrintingCompleted = 13,
            LaminationCompleted = 14,
            SlittingCompleted = 15,
            GravureFinishedGoodCompleted = 16

        }

        public enum CylinderType
        {
            CylinderTypeOne = 1,
            CylinderTypeTwo = 2
        }

        public enum Die
        {
            DieOne = 1,
            DieTwo = 2,
            DieThree = 3,
            DieFour = 4,
            DieFive = 5
        }

        public enum DieCuttingEmbossing
        {
            Embossing = 1,
            Demossing = 2,
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

        public enum Embossing
        {
            Embossing = 1,
            Demossing = 2,
            Regular = 3,
        }
        
        public enum EnclArtWorkDetail
        {
            ArtWorkDetailOne = 1,
            ArtWorkDetailTwo = 2,
            ArtWorkDetailThree = 3,
            ArtWorkDetailFour = 4,
            ArtWorkDetailFive = 5

        }

        public enum Flexo
        {
            Gold = 1,
            Silver = 2,
            Others = 3,
        }

        public enum Gold
        {
            GoldOne = 1,
            GoldTwo = 2,
        }

        public enum PrintingColorType
        {
            Color = 1,
            Ink = 2,
        }

        public enum LabTestType
        {
            BaseOil = 1,
            BlendsOil = 2,
            WasteOil = 3
        }

        public enum Silver
        {
            SilverOne = 1,
            SilverTwo = 2,
        }

        public enum TypeName
        {
            Supplier = 1,
            TestName = 2,
            Standard = 3,
            Method = 4,
            Result = 5

        }

        public enum JobType
        {
            JobTypeOne = 1,
            JobTypeTwo = 2,
            JobTypeThree = 3,
            JobTypeFour = 4,
            JobTypeFive = 5

        }

        public enum JobCompleted
        {
            JobCompletedOne = 1,
            JobCompletedTwo = 2

        }

        public enum JobCategory
        {
            Offset = 1,
            Gravure = 2

        }

        public enum MaterialType
        {
            MaterialTypeOne = 1,
            MaterialTypeTwo = 2,
            MaterialTypeThree = 3,
            MaterialTypeFour = 4,
            MaterialTypeFive = 5

        }

        public enum Material
        {
            Board = 1,
            Paper = 2
        }

        public enum WaterBase
        {
            WaterBaseOne = 1,
            WaterBaseTwo = 2,
            WaterBaseThree = 3,
            WaterBaseFour = 4,
            WaterBaseFive = 5
        }

        public enum UVVarnish
        {
            UVVarnishOne = 1,
            UVVarnishTwo = 2,
            UVVarnishThree = 3,
            UVVarnishFour = 4,
            UVVarnishFive = 5
        }

        public enum PackingStatus
        {
            Completed = 1,
            Process = 2
        }

        public enum PackingType
        {
            Machine = 1,
            Manual = 2
        }

        public enum PaperBoardCategory
        {
            Paper = 1,
            Board = 2
        }

        public enum Printing
        {
            None = 0,
            Surface = 1,
            Reverse = 2
            
        }

        public enum PrintingTypes
        {
            PrintingTypeOne = 1,
            PrintingTypeTwo = 2,
            PrintingTypeThree = 3,
            PrintingTypeFour = 4,
            PrintingTypeFive = 5
            
        }

        public enum PastingType
        {
            Straight_Crush_Bottom = 1,
            Straight = 2,
            Clamshell_Box = 3
        }

        public enum InstalledSection
        {
            InstalledSectionOne = 1,
            InstalledSectionTwo = 2,
            InstalledSectionThree = 3,
            InstalledSectionFour = 4,
            InstalledSectionFive = 5

        }

        public enum GravurePrintingType
        {
            OneSide = 1,
            BothSide = 2,
        }

        public enum LaminationType
        { 
            SolventBase = 1,
            SolventLess = 2
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

        public enum PaymentTypes
        {
            Grower = 1,
            CarrageContractor = 2,
            LoadingContractor = 3,
            UnLoadingContractor = 4,
            Transporter = 5
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

        public enum PolarCuttingEmbossing
        {
            Regular = 1
        }

        public enum POType
        {
            Agreement = 1,
            BlanketPO = 2,
            StandardPO = 3,
            Indent = 4
        }

        public enum PriceType
        {
            Discounted = 1,
            ListPrice = 2,
            Negotiated = 3
        }

        public enum Quaterly
        {
            FirstQuater = 13,
            SecondQuater = 14,
            ThirdQuater = 15,
            ForthQuater = 16
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
            LookUpCircle = 7
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
            LoanRecovery = 2
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

        public enum Machinetype
        {
            DieCutting = 1,
            Printing = 2,
            Pasting = 3
        }
    }
}
