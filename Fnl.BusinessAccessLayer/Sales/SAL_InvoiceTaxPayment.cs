using DAL.DataAccessLayer;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;

namespace FnI.BusinessAccessLayer.Sales
{
    public partial class SAL_InvoiceTaxPayment
    {
        const string Query_Delete_ByInvoiceID = @" Update [SAL_InvoiceTaxPayment] Set [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [InvoiceID] = @InvoiceID ";

        public static void DeleteDataByInvoiceID(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, int companyID, int periodID, int InvoiceID)
        {

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@InvoiceID", InvoiceID);
                sqlparams[3] = new SqlParameter("@Status", false);
                sqlparams[4] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[5] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[6] = new SqlParameter("@UpdatedByValue", updatedByValue);

                if (objTransaction == null)
                {
                    SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByInvoiceID, sqlparams);

                }
                else
                {
                    SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByInvoiceID, sqlparams);

                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
        }

    }
}
