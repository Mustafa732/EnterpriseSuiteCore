using FnI.BusinessAccessLayer.Inventory.Transaction;
using FnI.Core.Web;
using FnI.Core.Web.HttpApplicationState;
using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace FnI.Core.Helper
{
    public class IssuanceFlow
    {
        #region Methods For FIFO

        public static DataTable Get_AllDataAgainstItemFIFO(int itemid, SqlConnection objConnection = null)
        {
            string filter = "";

            if (FnI.Core.Web.HttpApplicationState.AppParameters.IssuanceFlow == "FIFO")
                filter = "Order By INV_StockTransactionHeader.DocumentDate Asc";
            else if (FnI.Core.Web.HttpApplicationState.AppParameters.IssuanceFlow == "LIFO")
                filter = "Order By INV_StockTransactionHeader.DocumentDate Desc";

            DataTable dt_StockAgainstItem = INV_StockTransactionDetail.Get_AllDataAgainstItemFIFO(AppSessions.CurrentCompanyID, AppSessions.CurrentPeriodID, itemid,
                filter, objConnection);

            return dt_StockAgainstItem;
        }

        public static DataTable Get_AllDataAgainstItemFIFOByLocation(int itemId, int locationId, SqlTransaction objTransaction = null)
        {
            string filter = "";
            if (FnI.Core.Web.HttpApplicationState.AppParameters.IssuanceFlow == "FIFO")
                filter = "Order By INV_StockTransactionHeader.DocumentDate Asc";
            else if (FnI.Core.Web.HttpApplicationState.AppParameters.IssuanceFlow == "LIFO")
                filter = "Order By INV_StockTransactionHeader.DocumentDate Desc";
            DataTable dt_StockAgainstItem = INV_StockTransactionDetail.Get_AllDataAgainstItemFIFOByLocation(
                AppSessions.CurrentCompanyID,
                AppSessions.CurrentPeriodID,
                itemId,
                locationId,
                filter);
            return dt_StockAgainstItem;
        }

        public static DataTable Get_AllDataAgainstItemFIFOByLocationByItemID(int ItemID, int _locationid, SqlTransaction objTransaction = null)
        {
            string filter = "";

            if (FnI.Core.Web.HttpApplicationState.AppParameters.IssuanceFlow == "FIFO")
                filter = "Order By INV_StockTransactionHeader.DocumentDate Asc";
            else if (FnI.Core.Web.HttpApplicationState.AppParameters.IssuanceFlow == "LIFO")
                filter = "Order By INV_StockTransactionHeader.DocumentDate Desc";

            DataTable dt_StockAgainstItem = INV_StockTransactionDetail.Get_AllDataAgainstItemFIFOByLocation(AppSessions.CurrentCompanyID, AppSessions.CurrentPeriodID,
                ItemID, _locationid, filter);

            return dt_StockAgainstItem;
        }

        public static DataTable Get_FIFOStockSetting(int itemId, int locationId, SqlTransaction objTransaction = null)
        {
            string filter = "";
            if (FnI.Core.Web.HttpApplicationState.AppParameters.IssuanceFlow == "FIFO")
                filter = "Order By INV_StockTransactionHeader.DocumentDate,INV_StockTransactionDetail.TransactionDetailID Asc";
            else if (FnI.Core.Web.HttpApplicationState.AppParameters.IssuanceFlow == "LIFO")
                filter = "Order By INV_StockTransactionHeader.DocumentDate,INV_StockTransactionDetail.TransactionDetailID Desc";
            DataTable dt_StockAgainstItem = INV_StockTransactionDetail.Get_AllForStockSetting(
                AppSessions.CurrentCompanyID,
                AppSessions.CurrentPeriodID,
                itemId,
                locationId,
                filter);
            return dt_StockAgainstItem;
        }

        public static DataTable Get_FIFOStockSettingByItemID(int ItemID, int _locationid, SqlTransaction objTransaction = null)
        {
            string filter = "";

            if (FnI.Core.Web.HttpApplicationState.AppParameters.IssuanceFlow == "FIFO")
                filter = "Order By INV_StockTransactionHeader.DocumentDate,INV_StockTransactionDetail.TransactionDetailID Asc";
            else if (FnI.Core.Web.HttpApplicationState.AppParameters.IssuanceFlow == "LIFO")
                filter = "Order By INV_StockTransactionHeader.DocumentDate,INV_StockTransactionDetail.TransactionDetailID Desc";

            DataTable dt_StockAgainstItem = INV_StockTransactionDetail.Get_AllForStockSetting(AppSessions.CurrentCompanyID, AppSessions.CurrentPeriodID,
               ItemID, _locationid, filter);

            return dt_StockAgainstItem;
        }

        public static DataTable Get_AllDataAgainstItemFIFOByLocationID(int itemid, int _locationid, SqlTransaction objTransaction = null,DateTime? dt = null)
        {
            string filter = "";

            if (FnI.Core.Web.HttpApplicationState.AppParameters.IssuanceFlow == "FIFO")
                filter = "Order By INV_StockTransactionHeader.DocumentDate Asc";
            else if (FnI.Core.Web.HttpApplicationState.AppParameters.IssuanceFlow == "LIFO")
                filter = "Order By INV_StockTransactionHeader.DocumentDate Desc";

            DataTable dt_StockAgainstItem = INV_StockTransactionDetail.Get_AllDataAgainstItemFIFOByLocation(AppSessions.CurrentCompanyID, AppSessions.CurrentPeriodID,
                itemid, _locationid, filter);

            return dt_StockAgainstItem;
        }


        public static DataTable Get_QTYFIFOByLocationID(int itemid, int _locationid, SqlTransaction objTransaction = null, DateTime? dt = null)
        {
            string filter = "";

            if (FnI.Core.Web.HttpApplicationState.AppParameters.IssuanceFlow == "FIFO")
                filter = "Order By INV_StockTransactionHeader.DocumentDate Asc";
            else if (FnI.Core.Web.HttpApplicationState.AppParameters.IssuanceFlow == "LIFO")
                filter = "Order By INV_StockTransactionHeader.DocumentDate Desc";

            DataTable dt_StockAgainstItem = INV_StockTransactionDetail.Get_QTYFIFOByLocation(AppSessions.CurrentCompanyID, AppSessions.CurrentPeriodID,
                itemid, _locationid, filter,null,dt);

            return dt_StockAgainstItem;
        }
        public static DataTable Get_AllDataAgainstItemFIFO(int itemId, SqlTransaction objTransaction = null)
        {
            string filter = "";

            if (AppParameters.IssuanceFlow == "FIFO")
                filter = "Order By INV_StockTransactionHeader.DocumentDate Asc";
            else if (AppParameters.IssuanceFlow == "LIFO")
                filter = "Order By INV_StockTransactionHeader.DocumentDate Desc";

            DataTable dt_StockAgainstItem = INV_StockTransactionDetail.Get_AllDataAgainstItemFIFO(
                AppSessions.CurrentCompanyID,
                AppSessions.CurrentPeriodID,
                itemId,
                filter);

            return dt_StockAgainstItem;
        }

        public static DataTable Get_AllDataAgainstMultipleItemsFIFO(string itemsID, SqlTransaction objTransaction = null)
        {
            string filter = "";

            if (FnI.Core.Web.HttpApplicationState.AppParameters.IssuanceFlow == "FIFO")
                filter = "Order By INV_StockTransactionHeader.DocumentDate Asc";
            else if (FnI.Core.Web.HttpApplicationState.AppParameters.IssuanceFlow == "LIFO")
                filter = "Order By INV_StockTransactionHeader.DocumentDate Desc";

            DataTable dt_StockAgainstItem = INV_StockTransactionDetail.Get_AllDataAgainstMultipleItemsFIFO(AppSessions.CurrentCompanyID, AppSessions.CurrentPeriodID, 
                itemsID, filter);

            return dt_StockAgainstItem;
        }

        public static DataTable Get_AllDataAgainstMultipleItemsFIFON(string itemsID,int _locationid, SqlTransaction objTransaction = null)
        {
            string filter = "";

            if (FnI.Core.Web.HttpApplicationState.AppParameters.IssuanceFlow == "FIFO")
                filter = "Order By INV_StockTransactionHeader.DocumentDate Asc";
            else if (FnI.Core.Web.HttpApplicationState.AppParameters.IssuanceFlow == "LIFO")
                filter = "Order By INV_StockTransactionHeader.DocumentDate Desc";

            if(FnI.Core.Web.HttpApplicationState.AppParameters.IsStockFIFO)
                filter = "Order By INV_StockTransactionHeader.DocumentDate Asc";

            DataTable dt_StockAgainstItem = INV_StockTransactionDetail.Get_AllDataAgainstMultipleItemsFIFON(AppSessions.CurrentCompanyID, AppSessions.CurrentPeriodID,
                itemsID,_locationid, filter);

            return dt_StockAgainstItem;
        }

        public static void Update_GRN_For_FIFO_LIFO(DataTable dt_StockTable, DataTable dt_StockAgainstItem, string quantity, SqlTransaction objTransaction = null)
        {
            if (dt_StockTable != null && dt_StockTable.Rows.Count > 0)
            {
                for (int k = 0; k < dt_StockTable.Rows.Count; k++)
                {
                    decimal Tquantity = decimal.Parse(dt_StockTable.Rows[k][quantity].ToString());

                    if (dt_StockAgainstItem != null && dt_StockAgainstItem.Rows.Count > 0)
                    {
                        for (int i = 0; i < dt_StockAgainstItem.Rows.Count; i++)
                        {
                            if (dt_StockTable.Rows[k]["ItemID"].ToString() == dt_StockAgainstItem.Rows[i]["ItemID"].ToString())
                            {
                                decimal grnQuantity = Convert.ToDecimal((dt_StockAgainstItem.Rows[i]["TransactionQuantity1"] != null && dt_StockAgainstItem.Rows[i]["TransactionQuantity1"] != DBNull.Value) ? dt_StockAgainstItem.Rows[i]["TransactionQuantity1"] : "0");

                                decimal issuedQuantity = Convert.ToDecimal((dt_StockAgainstItem.Rows[i]["IssuedQuantity"] != null && dt_StockAgainstItem.Rows[i]["IssuedQuantity"] != DBNull.Value) ? dt_StockAgainstItem.Rows[i]["IssuedQuantity"] : "0");

                                decimal availableQuantity = grnQuantity - issuedQuantity;

                                bool isIssued = (dt_StockAgainstItem.Rows[i]["IsIssued"] != null && dt_StockAgainstItem.Rows[i]["IsIssued"] != DBNull.Value) ? Convert.ToBoolean(dt_StockAgainstItem.Rows[i]["IsIssued"]) : false;

                                if (Tquantity != 0)
                                {
                                    if (!isIssued && Tquantity >= availableQuantity)
                                    {
                                        #region Update

                                        INV_StockTransactionDetail.UpdateIssuedQuantity(AppSessions.CurrentCompanyID, AppSessions.CurrentPeriodID,
                                            int.Parse(dt_StockAgainstItem.Rows[i]["StockTransactionID"].ToString()),
                                            int.Parse(dt_StockAgainstItem.Rows[i]["TransactionDetailID"].ToString()),
                                            decimal.Parse(dt_StockAgainstItem.Rows[i]["TransactionQuantity1"].ToString()));

                                        INV_StockTransactionDetail.UpdateIsIssued(AppSessions.CurrentCompanyID, AppSessions.CurrentPeriodID,
                                            int.Parse(dt_StockAgainstItem.Rows[i]["StockTransactionID"].ToString()),
                                            int.Parse(dt_StockAgainstItem.Rows[i]["TransactionDetailID"].ToString()), true);

                                        Tquantity = decimal.Parse(Tquantity.ToString()) - availableQuantity;

                                        #endregion
                                    }
                                    else if (!isIssued && Tquantity < availableQuantity)
                                    {
                                        #region Update

                                        INV_StockTransactionDetail.UpdateIssuedQuantity(AppSessions.CurrentCompanyID, AppSessions.CurrentPeriodID,
                                            int.Parse(dt_StockAgainstItem.Rows[i]["StockTransactionID"].ToString()),
                                            int.Parse(dt_StockAgainstItem.Rows[i]["TransactionDetailID"].ToString()), issuedQuantity + Tquantity);

                                        Tquantity = 0;

                                        #endregion
                                    }
                                }
                                else
                                    break;
                            }
                        } // FOR dt_StockAgainstItem
                    }
                }
            }
        }

        public static void CreateCollection_For_FIFO_LIFO(DataTable dt_StockTable, DataTable dt_StockAgainstItem, SqlTransaction objTransaction = null)
        {
        }

        #endregion

    }
}
