using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;
using FnI.BusinessAccessLayer.AR.Transaction;
using FnI.BusinessAccessLayer.GL.Transaction;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Transaction
{
    public partial class  HRMS_OverTimePostingHeader
    {

        public bool InsertIntoOverTimePostingHeaderandDetailWithINvoiceandGL(List<HRMS_OverTimePostingDetail> colOTPDetAdd, AR_InvoiceHeader objInvoiceHeader, List<AR_InvoiceSupplierDetail> colEmplyDetinvoice ,List<HRMS_OverTime> colOverTime, GL_TransactionHeader objTranHead, List<GL_TransactionDetail> colTranDetAddMulti, SqlTransaction sqltran)
        {
            SqlTransaction sqltrans;
            bool result = false;
            if (sqltran == null)
            {
                sqltrans = SqlHelper.GetTransaction("HRMS_EmployeeOverTimePosting");
            }
            else
            {
                sqltrans = sqltran;
            }
            try
            {
                this.Insert(sqltrans);

                foreach (HRMS_OverTimePostingDetail otp in colOTPDetAdd)
                {
                    otp.OTPDetailID = short.Parse(HRMS_OverTimePostingDetail.GetNewOTPDetailID(this.CompanyID, this.OTPHeaderID, this.PeriodID, sqltrans).ToString());
                    long tempOrderDetailID = otp.OTPDetailID;
                    otp.Insert(sqltrans);
                }
                if (objInvoiceHeader.Insert())
                {
                    short invoiceSupplierID = Convert.ToInt16(AR_InvoiceSupplierDetail.GetNewInvoiceSupplierID(f_CompanyID, objInvoiceHeader.InvoiceID, f_PeriodID, sqltrans));

                    foreach (AR_InvoiceSupplierDetail invoiceSupplier in colEmplyDetinvoice)
                    {
                        invoiceSupplier.InvoiceSupplierID = invoiceSupplierID++;
                        invoiceSupplier.InvoiceHeaderID = objInvoiceHeader.InvoiceID;
                        invoiceSupplier.FiscalID = objInvoiceHeader.FiscalID;
                        invoiceSupplier.Insert(sqltrans);
                    }

                    objTranHead.Insert();
                    foreach (GL_TransactionDetail GLD in colTranDetAddMulti)
                    {
                        GLD.TransactionID = objTranHead.TransactionID;
                        GLD.FiscalID = objTranHead.FiscalID;
                        GLD.Insert();
                    }

                    foreach (HRMS_OverTime OTPost in colOverTime)
                    {
                        OTPost.UpdateEmpForPostingByKey();
                    }
                    result = true;
                }

            }
            catch (BusinessLogicException ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            finally
            {
                if (result)
                {
                    if (sqltrans == null)
                    {
                        SqlHelper.CommitTransaction(sqltrans);
                    }
                    else
                    { }
                }
                else
                {
                    if (sqltrans == null)
                    {
                        SqlHelper.RollBackTransaction(sqltrans, "HRMS_EmployeeOverTimePosting");
                    }
                    else
                    { }
                }
            }

            return result;
        }
    }
}
