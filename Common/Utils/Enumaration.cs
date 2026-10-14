namespace Common.Utils
{
    public class Enumaration
    {
        public enum TransactionType
        {
            Local = 1,
            Imported = 2,
        }
        
        public enum PlantationType
        {
            Good = 1,
            Fail = 2,
            Poor = 3,
        }
        
        public enum FormOperation
        {
            Add = 1,
            Edit = 2,
            Find = 3,
            Delete = 4,
            Alert = 5,
            Start = 6,
            Stop = 7,
            PaymentMode = 8,
            Update = 9,
            Copy = 10,
            PageLoad,
            Revert,
            
        }

        public enum MessageCode
        {
            Custom = 1,
            Failure_Add = 2,
            Failure_Delete = 3,
            Failure_Update = 4,
            Success_Add = 5,
            Success_Delete = 6,
            Success_Update = 7,
            Unknown = 8,
        }

        public struct MasterRecordName
        {
            public const string Token = "Token";
            public const string Indent = "Indent";
            public const string Leakage = "Leakage";
            public const string FirstWeight = "First Weight";
            public const string SecondWeight = "Second Weight";
            public const string Payment = "Payment";
        }

        public enum MessageType
        {
            Error = 1,
            Message = 2
        }

        public enum PageErrorCodes
        {
            MenuIDNotMatched = 1,
            MenuIDNotMentioned = 2,
            NotAuthorized = 3,
            PageNotFound = 4,
            InvalidModule = 5,
            Notfoundinlibrary = 6
        }

        public enum QueryType
        {
            Equals = 0,
            Like = 1
        }

        public enum SortDirection
        {
            asc = 0,
            desc = 1
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
            NoOrder = 17
        }


      

        public enum RoutePlanFrequency
        {
            Daily = 1,
            Weekly = 2,
            Monthly = 3,
            Yearly = 4
        }

        public enum AssignmentStatus
        {
            New = 1,
            Visited = 2
        }

        public enum SubZoneInOut
        {
            In = 1,
            Out = 2
        }



        #region Not used yet

        public enum ContractType_Payment
        {
            Broker = 1,
            Contract = 2,
            DeliveryOrder = 3
        }

        public enum InvoiceType
        {
            Customer = 1,
            DeliveryOrder = 2,
            Lifting = 3
        }

        public enum PageState
        {
            Edit = 1,
            New = 2
        }

        public enum Paid
        {
            IsNotPaid = 0,
            IsPaid = 1,
            None = 2
        }

        public enum SqlExeceptionErrorCode
        {
            Error_UniqueIndex = 2601
        }

        public enum Status
        {
            Active = 1,
            Inactive = 2,
        }

        public enum SupplyOrderList
        {
            Default = 0,
            LocationWise = 1,
            PrintOrder = 2
        }

        public enum Summaries
        {
            BrokerWise = 1,
            CityWiseSaleReport = 2,
            ContractWise = 3,
            DateWise = 4,
            DOWise = 5,
            JVSalesTax = 6,
            MonthWise = 7,
            SummaryCityWise = 8,
            UnliftedDo = 9
        }

        public enum TMSFileType
        {
            Access = 1,
            Excel2000 = 2,
            Excel2007 = 3,
            CSV = 4,
            DAT = 5
        }

        #endregion
    }
}


