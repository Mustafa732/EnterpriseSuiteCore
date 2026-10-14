using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.AP.Setup
{
    public partial class PFUtility_Log : BaseClass
    {
        #region Const String

        const string Query_GetDataForUtilityForm = @"SELECT * FROM (
SELECT PFUtility_log.CompanyID, PFUtility_log.PeriodID, PaymentID, TransactionID, LogID, AP_PaymentHeader.DocumentNo, DocumentType, AP_PaymentHeader.DocumentDate, FromDate, ToDate, Comments, Amount as NetAmount, CONVERT(bit,0) as IsChecked, PFUtility_log.Status, PFUtility_log.FiscalID
FROM PFUtility_log
INNER JOIN AP_PaymentHeader ON AP_PaymentHeader.CompanyID = PFUtility_log.CompanyID AND AP_PaymentHeader.PeriodID = PFUtility_log.PeriodID
AND AP_PaymentHeader.FiscalID = PFUtility_log.FiscalID AND AP_PaymentHeader.PaymentID = PFUtility_log.PaymentHeaderID
AND AP_PaymentHeader.Status = 1 AND AP_PaymentHeader.StatusCode = 1
INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = AP_PaymentHeader.CompanyID AND SET_DocumentType.PeriodID = AP_PaymentHeader.PeriodID
AND SET_DocumentType.DocumentTypeID = AP_PaymentHeader.DocumentTypeID AND SET_DocumentType.Status = 1
INNER JOIN GL_TransactionHeader ON GL_TransactionHeader.CompanyID = AP_PaymentHeader.CompanyID AND GL_TransactionHeader.PeriodID = AP_PaymentHeader.PeriodID
AND GL_TransactionHeader.DocumentNo = AP_PaymentHeader.DocumentNo AND GL_TransactionHeader.DocumentTypeID = AP_PaymentHeader.DocumentTypeID
AND  GL_TransactionHeader.FiscalID = AP_PaymentHeader.FiscalID
)PFUtility_log
";

        #endregion

        #region Methods

        public static DataTable GetDataForUtilityForm(string StrngWhere)
        {
            DataTable dtToReturn = null;

            try
            {
                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataForUtilityForm + StrngWhere);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        #endregion
    }
}
