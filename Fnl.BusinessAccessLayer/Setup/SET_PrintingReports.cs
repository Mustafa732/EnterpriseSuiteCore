using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//using FnI.Core.Web;
//using FnI.Core.Web.HttpApplicationState;
//using FnI.Core.Helper;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_PrintingReports
    {

        public static string PrintDocuments(int DocumentTypeID, string DocumentNo, string ReportName, int CurrentCompanyID, int CurrentPeriodID, string status, bool issheikhooreport = false)
        {

            string DocumentType = string.Empty;

            SET_DocumentType set_documenttype = new SET_DocumentType(CurrentCompanyID, DocumentTypeID, CurrentPeriodID);

            DocumentType = set_documenttype.DocumentType;

            StringBuilder sb_url = new StringBuilder();

            #region Pre-Defined Reports
            if (ReportName != "GoodsReceiveNote_AutoM" || ReportName != "GoodsReceiveNote")
            {
                if (DocumentType != null)
                {
                    if (DocumentType.ToLower().Contains("sales invoice"))
                        ReportName = "SaleInvoiceTrd";
                    else if (DocumentType.ToLower().Contains("di") && DocumentType.ToLower().Contains("(csp)"))
                        ReportName = "DI_SaleInvoiceTrd";
                    else if (DocumentType.ToLower().Contains("sales order"))
                    {
                        if (ReportName == "SalesOrderNew")
                        {
                            ReportName = "SalesOrderNew";
                        }
                        else
                        {
                            ReportName = "SalesOrder";
                        }
                    }
                    else if (DocumentType.ToLower().Contains("p.o (import)") || DocumentType.ToLower().Contains("p.o (local)"))
                        ReportName = "PurchaseOrder_MBI";
                    else if (DocumentType.ToLower().Contains("requisition- ho"))
                        ReportName = "StockRequistionReportSku";
                    else if (DocumentType.ToLower().Contains("purchase order") || DocumentType.ToLower().Contains("p.o") || DocumentType.ToLower().Contains("po"))
                    {
                        if (!DocumentType.ToLower().Contains("advance"))
                        {
                            if (ReportName == "PurchaseOrderSku")
                                ReportName = "PurchaseOrderSku";
                            else if (ReportName == "PurchaseOrderCon")
                                ReportName = "PurchaseOrderCon";
                            else if (ReportName == "PurchaseOrderHSM")
                                ReportName = "PurchaseOrderHSM";
                            else if (ReportName == "PurchaseOrderRSM")
                                ReportName = "PurchaseOrderRSM";
                            else if (ReportName == "PurchaseOrderSSM")
                                ReportName = "PurchaseOrderSSM";
                            else if (ReportName == "SalesOrderSSRR")
                                ReportName = "SalesOrderSSRR";
                            else if (ReportName == "ComparativeStatement")
                                ReportName = "ComparativeStatement";
                            else if (ReportName == "PurchaseOrder_Pattoki")
                                ReportName = "PurchaseOrder_Pattoki";
                            else if (ReportName == "SalesOrder")
                                ReportName = "SalesOrder";
                            else if (ReportName == "PurchaseOrderTSM")
                                ReportName = "PurchaseOrderTSM";
                            else
                                ReportName = "PurchaseOrderJk";
                        }
                    }
                    else if (DocumentType.ToLower().Contains("delivery order"))
                        ReportName = "DeliveryOrder";
                    else if (DocumentType.ToLower().Contains("dispatch"))
                    {
                        if (ReportName == "GatePassMBSM_TSM")
                            ReportName = "GatePassMBSM_TSM";
                        else if (ReportName == "GatePassMBSM_HSM")
                            ReportName = "GatePassMBSM_HSM";
                        else if (ReportName == "GatePassSugar_DK")
                            ReportName = "GatePassSugar_DK";
                        else if ((DocumentType.ToLower().Contains("sugar")))
                            ReportName = "GatePassSugarM";
                        else
                            ReportName = "GatePassMBSM";
                        //ReportName = "DispatchNote";
                    }
                    else if (DocumentType.ToLower().Contains("grn"))
                        if (issheikhooreport == true)
                            ReportName = "GoodsReceiveNoteSku";
                        else
                            ReportName = "GoodsReceiveNote";

                    else if (DocumentType.ToLower().Contains("gin"))
                        ReportName = "GoodsIssueNoteReport";

                    else if (DocumentType.ToLower().Contains("transfer"))
                        ReportName = "ItemTransferRreport";
                    else if (DocumentType.ToLower().Contains("delivery contract"))
                    {
                        if (ReportName == "DeliveryOrderNew")
                        {
                            ReportName = "DeliveryOrderNew";
                        }
                        else
                        {
                            ReportName = "DeliveryOrder";
                        }
                    }
                    else if (DocumentType.ToLower().Contains("purchase requisition- khi") || DocumentType.ToLower().Contains("purchase requisition- site") || DocumentType.ToLower().Contains("purchase requisition - fa") || DocumentType.ToLower().Contains("requisition - issuance"))
                        ReportName = "StockRequistionReportHSM";
                    else if (DocumentType.ToLower().Contains("requisition"))
                        if (issheikhooreport == true)
                            ReportName = "StockRequistionReportSku";
                        else if (ReportName == "StockRequistionReportJK")
                            ReportName = "StockRequistionReportJK";
                        else
                            ReportName = "StockRequistionReport";
                    else if (DocumentType.ToLower().Contains("advance payment"))
                        if (ReportName == "AdvancePaymentReceipt_HSM")
                            ReportName = "AdvancePaymentReceipt_HSM";
                        else
                            ReportName = "AdvancePaymentReceipt";
                    if (ReportName == "TransactionVoucherPrintingPS")
                    {
                        ReportName = "TransactionVoucherPrintingPS";
                    }
                    if (ReportName == "")
                    {
                        ReportName = "TransactionVoucherPrintingTwo";
                    }
                    if (DocumentType.ToLower().Contains("direct payment - ho-site transfer"))
                        ReportName = "TransactionVoucherPrintingTwo";


                }

                if (ReportName == "GrowerBankAdviceReport_SNG")
                    ReportName = "GrowerBankAdviceReport_SNG";
                else if (ReportName == "PaymentVerificationReport_SNG")
                    ReportName = "PaymentVerificationReport_SNG";
                else if (ReportName == "GrowerPaymentEntryBankAdviceReport_SNG")
                    ReportName = "GrowerPaymentEntryBankAdviceReport_SNG";
                else if (ReportName == "GrowerPaymentEntryBankAdviceSummaryReport_SNG")
                    ReportName = "GrowerPaymentEntryBankAdviceSummaryReport_SNG";
            }

            #endregion

            #region QueryString Assigning to Run Report



            if (ReportName == "GrowerBankAdviceReport_SNG" || ReportName == "PaymentVerificationReport_SNG" || ReportName == "GrowerPaymentEntryBankAdviceReport_SNG" || ReportName == "GrowerPaymentEntryBankAdviceSummaryReport_SNG")
                sb_url.Append("/ReportViewer_Cane.aspx?reportname=" + ReportName);
            else
                sb_url.Append("/ReportViewer.aspx?reportname=" + ReportName);

            int menuID = SET_DocumentType.GetFormsEnabledMenuID(sb_url.ToString());

            sb_url.Append("&menuID=" + menuID + "&autoview=true");

            sb_url.Append("&documentno=" + DocumentNo);

            if (DocumentTypeID > 0)
                sb_url.Append("&documenttypeid=" + DocumentTypeID);

            if (status != "")
                sb_url.Append("&status=" + status);
            //string url = "/ReportViewer.aspx?reportname=" + ReportName + "&menuID=" + menuID + "&autoview=true&documentno=" + DocumentNo;
            string url = sb_url.ToString();

            string fullURL = "window.open('" + url
                + "', '_blank', 'height=800, width=930, status=yes, toolbar=no, menubar=no, location=no, scrollbars=yes, resizable=yes, titlebar=no');";

            #endregion


            return fullURL;

            //ScriptManager.RegisterStartupScript(this, typeof(string), "OPEN_WINDOW", fullURL, true);
        }

        public static string PrintDocumentsFormultipleDocNO(int DocumentTypeID, string fromDocumentNo, string toDocumentNo, string ReportName, int CurrentCompanyID, int CurrentPeriodID, string status, bool issheikhooreport = false)
        {

            string DocumentType = string.Empty;

            SET_DocumentType set_documenttype = new SET_DocumentType(CurrentCompanyID, DocumentTypeID, CurrentPeriodID);

            DocumentType = set_documenttype.DocumentType;

            StringBuilder sb_url = new StringBuilder();

            #region Pre-Defined Reports
            if (ReportName != "GoodsReceiveNote_AutoM" || ReportName != "GoodsReceiveNote")
            {
                if (DocumentType != null)
                {
                    if (DocumentType.ToLower().Contains("delivery order"))
                        ReportName = "DeliveryOrder";
                    else if (DocumentType.ToLower().Contains("delivery contract"))
                        ReportName = "DeliveryOrder";
                    if (ReportName == "PurchaseContractImportReport")
                        ReportName = "PurchaseContractImportReport";
                    if (ReportName == "LettersOfCredit_Import")
                        ReportName = "LettersOfCredit_Import";
                    if (ReportName == "ImportDocument_ImportReport")
                    {
                        ReportName = "ImportDocument_ImportReport";
                    }
                    if (ReportName == "")
                        ReportName = "TransactionVoucherPrintingTwo";

                }
            }

            #endregion

            #region QueryString Assigning to Run Report

            sb_url.Append("/ReportViewer.aspx?reportname=" + ReportName);

            int menuID = SET_DocumentType.GetFormsEnabledMenuID(sb_url.ToString());

            sb_url.Append("&menuID=" + menuID + "&autoview=true");

            sb_url.Append("&documentno=" + fromDocumentNo);
            sb_url.Append("&documentnoTo=" + toDocumentNo);

            if (DocumentTypeID > 0)
                sb_url.Append("&documenttypeid=" + DocumentTypeID);

            if (status != "")
                sb_url.Append("&status=" + status);
            //string url = "/ReportViewer.aspx?reportname=" + ReportName + "&menuID=" + menuID + "&autoview=true&documentno=" + DocumentNo;
            string url = sb_url.ToString();

            string fullURL = "window.open('" + url
                + "', '_blank', 'height=800, width=930, status=yes, toolbar=no, menubar=no, location=no, scrollbars=yes, resizable=yes, titlebar=no');";

            #endregion


            return fullURL;

            //ScriptManager.RegisterStartupScript(this, typeof(string), "OPEN_WINDOW", fullURL, true);
        }


        public static string PrintDocumentsDateWise(int DocumentTypeID, int currDocNo, DateTime PODocumentDate, string fromDocumentNo, string ReportName, int CurrentCompanyID, int CurrentPeriodID, string status, bool issheikhooreport = false)
        {

            string DocumentType = string.Empty;

            SET_DocumentType set_documenttype = new SET_DocumentType(CurrentCompanyID, DocumentTypeID, CurrentPeriodID);

            DocumentType = set_documenttype.DocumentType;

            StringBuilder sb_url = new StringBuilder();

            #region Pre-Defined Reports
            if (DocumentType.ToLower().Contains("purchase order") || DocumentType.ToLower().Contains("p.o") || DocumentType.ToLower().Contains("po"))
            {
                if (!DocumentType.ToLower().Contains("advance"))
                {
                    if (ReportName == "PurchaseOrderSku")
                        ReportName = "PurchaseOrderSku";

                    else if (ReportName == "PurchaseOrderHSM")
                        ReportName = "PurchaseOrderHSM";
                    else if (ReportName == "PurchaseOrderSSM")
                        ReportName = "PurchaseOrderSSM";
                    else if (ReportName == "SalesOrderSSRR")
                        ReportName = "SalesOrderSSRR";
                    else if (ReportName == "ComparativeStatement")
                        ReportName = "ComparativeStatement";
                    else
                        ReportName = "PurchaseOrderJk";
                }
            }

            #endregion

            #region QueryString Assigning to Run Report

            sb_url.Append("/ReportViewer.aspx?reportname=" + ReportName);

            int menuID = SET_DocumentType.GetFormsEnabledMenuID(sb_url.ToString());

            sb_url.Append("&menuID=" + menuID + "&autoview=true");

            sb_url.Append("&documentno=" + fromDocumentNo);
            sb_url.Append("&poDocumentDate=" + PODocumentDate);
            sb_url.Append("&currdocno=" + currDocNo);
            if (DocumentTypeID > 0)
                sb_url.Append("&documenttypeid=" + DocumentTypeID);

            if (status != "")
                sb_url.Append("&status=" + status);
            //string url = "/ReportViewer.aspx?reportname=" + ReportName + "&menuID=" + menuID + "&autoview=true&documentno=" + DocumentNo;
            string url = sb_url.ToString();

            string fullURL = "window.open('" + url
                + "', '_blank', 'height=800, width=930, status=yes, toolbar=no, menubar=no, location=no, scrollbars=yes, resizable=yes, titlebar=no');";

            #endregion


            return fullURL;

            //ScriptManager.RegisterStartupScript(this, typeof(string), "OPEN_WINDOW", fullURL, true);
        }


        public static string PrintDocumentsForSaleInvoiceProcess(string DocumentTypeID, string DocumentNo, string ReportName, int CurrentCompanyID, int CurrentPeriodID, string status, bool issheikhooreport = false, bool isSakrandSugarMill = false, bool isRaniPurSugarMill = false)
        {

            StringBuilder sb_url = new StringBuilder();

            #region Pre-Defined Reports

            if (isSakrandSugarMill == true)
            {
                if (ReportName == "ForSaleInvoiceProcess")
                    ReportName = "NewSugarTaxInvoice_SSML";
            }
            else if (isRaniPurSugarMill== true)
            {
                if (ReportName == "ForSaleInvoiceProcess")
                    ReportName = "NewSugarTaxInvoice_HSM";
            }
            else
            {
                if (ReportName == "ForSaleInvoiceProcess")
                    ReportName = "NewSugarTaxInvoice1_HSM";
            }
            //ReportName = "NewSugarTaxInvoice_HSM";


            #endregion

            #region QueryString Assigning to Run Report

            sb_url.Append("/ReportViewer.aspx?reportname=" + ReportName);

            int menuID = SET_DocumentType.GetFormsEnabledMenuID(sb_url.ToString());

            sb_url.Append("&menuID=" + menuID + "&autoview=true");

            sb_url.Append("&documentno=" + DocumentNo);

            if (DocumentTypeID != "")
                sb_url.Append("&documenttypeid=" + DocumentTypeID);

            if (status != "")
                sb_url.Append("&status=" + status);
            //string url = "/ReportViewer.aspx?reportname=" + ReportName + "&menuID=" + menuID + "&autoview=true&documentno=" + DocumentNo;
            string url = sb_url.ToString();

            string fullURL = "window.open('" + url
                + "', '_blank', 'height=800, width=930, status=yes, toolbar=no, menubar=no, location=no, scrollbars=yes, resizable=yes, titlebar=no');";

            #endregion


            return fullURL;

            //ScriptManager.RegisterStartupScript(this, typeof(string), "OPEN_WINDOW", fullURL, true);
        }
    }
}
