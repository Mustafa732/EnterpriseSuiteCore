using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.AR.Setup;
using FnI.BusinessAccessLayer.Inventory.Setup;
using FnI.BusinessAccessLayer.Setup;

namespace FnI.BusinessAccessLayer.AP.Transaction
{
    public partial class AR_InvoiceAdvancePaymentDetail 
    {

        public static DataTable GetDatabyInvoiceID(int CompanyID, int PeriodID, int InvoiceHeaderID)
        {
            {
                List<SqlParameter> sqlparams = new List<SqlParameter>();
                DataTable dtToReturn = null;
                try
                {
                    sqlparams.Add(new SqlParameter("@CompanyID", CompanyID));
                    sqlparams.Add(new SqlParameter("@PeriodID", PeriodID));
                    sqlparams.Add(new SqlParameter("@InvoiceHeaderID", InvoiceHeaderID));

                    string Query_GetDatabyInvoiceID = @"
select InvoiceHeaderID,PaymentHeaderID,AP_PaymentHeader.DocumentNo, AdvanceAmount as Amount , '' as Action
from AR_InvoiceAdvancePaymentDetail
inner join AP_PaymentHeader on AP_PaymentHeader.PaymentID = AR_InvoiceAdvancePaymentDetail.PaymentHeaderID
and AP_PaymentHeader.CompanyID = AR_InvoiceAdvancePaymentDetail.CompanyID
and AP_PaymentHeader.PeriodID = AR_InvoiceAdvancePaymentDetail.PeriodID
where AR_InvoiceAdvancePaymentDetail.CompanyID = @CompanyID and AR_InvoiceAdvancePaymentDetail.PeriodID = @PeriodID and AR_InvoiceAdvancePaymentDetail.InvoiceHeaderID = @InvoiceHeaderID
";


                    DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDatabyInvoiceID, sqlparams.ToArray());

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
        }

    }
}
