namespace Common.Utils
{
    public class SOEnumaration
    {
        public enum SalesOrder_Stage
        {
            Invoice = 2,
            Delivery = 3,
            Receipt = 4,
            Sales = 9,
            Dispatch = 13
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
    }
}
