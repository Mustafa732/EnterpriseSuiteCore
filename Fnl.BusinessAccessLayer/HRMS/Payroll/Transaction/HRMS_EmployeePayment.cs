using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.GL.Transaction;
using FnI.BusinessAccessLayer.AP.Transaction;
using FnI.BusinessAccessLayer.Setup;
using Common.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;

using Common.Utils;

using DAL.DataAccessLayer;

using FnI.BusinessAccessLayer.AP.Transaction;
using FnI.BusinessAccessLayer.AR.Setup;
using FnI.BusinessAccessLayer.AR.Transaction;

using FnI.BusinessAccessLayer.GL.Transaction;
using FnI.BusinessAccessLayer.Sales;
using FnI.BusinessAccessLayer.Setup;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Transaction
{
    public partial class HRMS_EmployeePayment
    {

        #region Contants

        const string Query_GetGroupedData = @"
select 
		AR_SET_BankAccount.BankAccountName
		,AR_SET_BankBranch.BankBranchName
		,AR_SET_Bank.BankName
        ,AR_SET_Bank.BankID
		,HRMS_SalaryHeader.SalaryHeaderID
		,hrms_set_employee.PersonID
		,HRMS_SET_Employee.Fullname
		,HRMS_SET_Employee.FatherName
		,HRMS_SET_Employee.JoiningDate
		,HRMS_SET_Employee.EmployeeCode
		,HRMS_SET_Employee.BasicSalary
		,HRMS_SalaryHeader.NetAmount
		,HRMS_SalaryHeader.SalaryDate
		,dateName(month, HRMS_SalaryHeader.SalaryDate) 
		+' - '+dateName(Year, HRMS_SalaryHeader.SalaryDate) as SalaryMonthYear
		 
	from HRMS_SalaryHeader

		inner join hrms_set_employee on hrms_set_employee.PersonID  =  HRMS_SalaryHeader.PersonID
		and hrms_set_employee.CompanyID  = HRMS_SalaryHeader.CompanyID
		and HRMS_SET_Employee.Status = 1

		inner join AR_SET_BankAccount on AR_SET_BankAccount.BankAccountID =  HRMS_SET_Employee.FK_BankAccountID
		and AR_SET_BankAccount.Status = 1
		and AR_SET_BankAccount.PeriodID  = HRMS_SalaryHeader.PeriodID

		inner join AR_SET_BankBranch on AR_SET_BankBranch.BankBranchID = AR_SET_BankAccount.BankBranchID
		and AR_SET_BankBranch.Status = 1

		inner  join AR_SET_Bank on AR_SET_Bank.BankID  =  AR_SET_BankBranch.BankID
		and AR_SET_Bank.Status =1 
		where postId is not null
		and PaymentID is null
		and IsApproved = 1 
		and HRMS_SalaryHeader.Status =1 
		and HRMS_SalaryHeader.CompanyID =  @CompanyID
		and HRMS_SalaryHeader.PeriodID  = @PeriodID
		and HRMS_SalaryHeader.SalaryHeaderID = @SalaryHeaderID
";

        #endregion

        #region Metohds

        public static DataTable GetAllGroupedData(int companyId, int periodId, long SalaryHeaderID, SqlTransaction objTransaction)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyId);
                sqlparams[1] = new SqlParameter("@PeriodID", periodId);
                sqlparams[2] = new SqlParameter("@SalaryHeaderID", SalaryHeaderID);

                if (objTransaction == null)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetGroupedData, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetGroupedData, sqlparams).Tables[0];

            }
            catch (Exception exp)
            {
                throw exp;
            }
            return dt;
        }

        public bool InsertScrollPayments(DataTable dtCPR, List<HRMS_EmployeePayment> cp, string BCCOAID, int CCCID, int CCCOAID, short LoanFor, short LoanOrRecoveryID,
          short DeductionMode, bool loanDeductionOnPayment, AP_PaymentHeader paymentHeader, GL_TransactionHeader th, List<GL_TransactionDetail> colTranDetail,
            List<AP_PaymentSupplierDetail> colSupplierDetail, List<AP_PaymentBanksDetail> colBankDetail,
           Nullable<int> bankAccountID, int CurrentFiscalID=0)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertPayment");

            try
            {
                DataTable dtLoan = new DataTable();
                #region Commit
                //    LoanDeductionOnPayment(ref dtLoan, ref cp, ref cpr, ref cl, ref cld, ref cgd, ref lst_PartialScrolls, LoanFor, LoanOrRecoveryID, DeductionMode,
                //        loanDeductionOnPayment, bankAccountID, sqltrans);

                //     CreateGLCollectionsForPayment(cp.FirstOrDefault(), dtLoan, dtCPR, BCCOAID, CCCID, CCCOAID, ref cld, ref cpr, ref glh, ref gld);

                //if (cpr.Count > 0)
                //    foreach (Cane_CPR _cpr in cpr)
                //        if (_cpr.DepotCPRID == null)
                //        {
                //            _cpr.IsPaid = (_cpr.PaymentAmount.HasValue && _cpr.PaymentAmount.Value >= _cpr.NetPayableAmount ? true : false);
                //            _cpr.IsScrolledPrint = true;

                //            if (!_cpr.UpdateDataByCPRNOKey(sqltrans))
                //                throw new Exception("Error!, record could not inserted in CPR.");
                //        }
                //        else
                //        {
                //            _cpr.IsPaid = (_cpr.PaymentAmount.HasValue && _cpr.PaymentAmount.Value >= _cpr.NetPayableAmount ? true : false);
                //            _cpr.IsScrolledPrint = true;

                //            if (!_cpr.UpdateDataByCPRNOKeyDepotId(sqltrans))
                //                throw new Exception("Error!, record could not inserted in CPR.");
                //        }

                //if (lst_PartialScrolls.Count > 0)
                //    foreach (Cane_PartialScroll objPartialScroll in lst_PartialScrolls)
                //        if (!objPartialScroll.UpdateDataByKey(sqltrans))
                //            throw new Exception("Error!, record could not be updated in Partial Scroll.");

                //if (cl.Count > 0)
                //    foreach (Cane_Loan _cl in cl)
                //        if (!_cl.Update_IsLoanFullDeducted(sqltrans))
                //            throw new Exception("Error!, record could not inserted in loan.");

                //if (cgd.Count > 0)
                //    foreach (Cane_GrowerLoanDetail _cgld in cgd)
                //        if (!_cgld.UpdateNoOFCPRsDeductedDataByKey(sqltrans))
                //            throw new Exception("Error!, record could not inserted in Grower Loan Detail.");

                //if (cld.Count > 0)
                //    foreach (Cane_LoanDeduction _cld in cld)
                //        if (!_cld.Insert(sqltrans))
                //            throw new Exception("Error!, record could not inserted in loan deduction.");
                #endregion
                long paymentId = (long)HRMS_EmployeePayment.GetNewPaymentHeaderID(cp.FirstOrDefault().CompanyID, cp.FirstOrDefault().PeriodID, sqltrans);
                long Emp_PaymentID = paymentId;
                foreach (HRMS_EmployeePayment _cp in cp)
                {
                    _cp.PaymentHeaderID = paymentId;
                    _cp.f_PaymentHeaderID = _cp.PaymentHeaderID;
                    if (!_cp.Insert(sqltrans))
                        throw new BusinessLogicException("Error! record could not inserted in payment");
                    paymentId++;
                }

                if (paymentHeader.PaymentID == -1)
                {
                    #region Insert In AP_PaymentHeader

                    paymentHeader.PaymentID = AP_PaymentHeader.GetNewPaymentID(paymentHeader.CompanyID, paymentHeader.PeriodID, sqltrans);
                    paymentHeader.DocumentNo = SET_DocumentSequence.ManualDocumentSequence(paymentHeader.DocumentTypeID, paymentHeader.CompanyID, paymentHeader.PeriodID, sqltrans, CurrentFiscalID);
                    //paymentHeader.SourceHeaderID = int.Parse(paymentId.ToString());
                    short PaymentSupplerID = (short)AP_PaymentSupplierDetail.GetNewPaymentSupplierID(paymentHeader.CompanyID, paymentHeader.PaymentID, paymentHeader.PeriodID, sqltrans);
                    short PaymentbankID = (short)AP_PaymentBanksDetail.GetNewPaymentBankID(paymentHeader.CompanyID, paymentHeader.PaymentID, paymentHeader.PeriodID, sqltrans);
                    
                    if (paymentHeader.Insert(sqltrans))
                    {
                        foreach (AP_PaymentSupplierDetail ps in colSupplierDetail)
                        {
                            ps.PaymentHeaderID = paymentHeader.PaymentID;
                            ps.PaymentSupplierID = PaymentSupplerID;
                            ps.EmpPaymentID = Emp_PaymentID;
                            ps.Insert(sqltrans);
                            PaymentSupplerID++;
                            Emp_PaymentID++;
                        }
                        foreach (AP_PaymentBanksDetail ps in colBankDetail)
                        {
                            ps.PaymentHeaderID = paymentHeader.PaymentID;
                            ps.PaymentBankID = PaymentbankID;
                            ps.Insert(sqltrans);
                            PaymentbankID++;
                        }

                        #region Insert In GL_TransactionHeader

                        if (th != null)
                        {
                            th.DocumentNo = paymentHeader.DocumentNo;
                            th.TransactionID = GL_TransactionHeader.GetNewTransactionID(th.CompanyID, th.PeriodID, sqltrans);
                            th.Insert(sqltrans);
                        }

                        #endregion

                        #region Insert In GL_TransactionDetail

                        foreach (GL_TransactionDetail td in colTranDetail)
                        {
                            td.TransactionID = th.TransactionID;
                            td.SourceHeaderID = paymentHeader.PaymentID;
                            td.FiscalID = th.FiscalID;
                            td.Insert(sqltrans);
                        }

                        #endregion
                    }

                    #endregion
  
                }

                result = true;
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
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "InsertPayment");
            }

            return result;
        }

        //public static void LoanDeductionOnPayment(ref DataTable dtLoan, ref List<Cane_Payment> cp, ref List<Cane_CPR> cpr, ref List<Cane_Loan> cl,
        //    ref List<Cane_LoanDeduction> cld, ref List<Cane_GrowerLoanDetail> cgd, ref List<Cane_PartialScroll> lst_PartialScroll, short LoanFor,
        //    short LoanOrRecoveryID, short DeductionMode, bool loanDeductionOnPayment, Nullable<int> bankAccountID, SqlTransaction sqltrans)
        //{
        //    int loanDeductionID = 0;

        //    if (loanDeductionOnPayment)
        //    {
        //        dtLoan = Cane_Loan.GetAllLoansForGrower(cp.FirstOrDefault().CompanyID, cp.FirstOrDefault().PeriodID, (int)cp.FirstOrDefault().SourceHeaderID, LoanFor,
        //            LoanOrRecoveryID, DeductionMode, sqltrans);

        //        loanDeductionID = Cane_LoanDeduction.GetNewLoanDeductionID(cp.FirstOrDefault().CompanyID, cp.FirstOrDefault().PeriodID, sqltrans);
        //    }

        //foreach (Cane_Payment _p in cp)
        //{
        //    decimal payableAmountCPR = (decimal)_p.PaymentAmount;
        //    decimal deductionAmountCPR = 0;
        //    if (dtLoan != null ? dtLoan.Rows.Count > 0 : false)
        //    {
        //        decimal e = (decimal)_p.CRPAmount;
        //        bool o = false;
        //        dtLoan = dtLoan.Select().Where(z => z.Field<string>("IsDeducted") == "No").OrderBy(x => x.Field<short>("LoanPriorityID")).CopyToDataTable();
        //        if (dtLoan != null ? dtLoan.Rows.Count > 0 : false)
        //            foreach (DataRow r in dtLoan.Rows)
        //            {
        //                decimal LoanAmount = decimal.Parse(r["LoanAmount"].ToString());

        //                if ((r["IsDeductionStop"].ToString() == "1" || r["IsDeductionStop"].ToString() == "True") && r["StopPercentage"].ToString() == "0")
        //                {
        //                    cgd.Add(objGrowerLoanDetail(_p, long.Parse(r["GrowerWiseLoanHeaderID"].ToString()), long.Parse(r["GrowerWiseLoanDetailID"].ToString()), int.Parse(r["NoOfCPRsDeducted"].ToString()) + 1));
        //                }
        //                else
        //                {
        //                    if (!o)
        //                        if (r["IsDeductionStop"].ToString() == "1" || r["IsDeductionStop"].ToString() == "True")
        //                        { o = true; e = (e * decimal.Parse(r["StopPercentage"].ToString())) / 100; }

        //                    if (r["IsDeductionStop"].ToString() == "1" || r["IsDeductionStop"].ToString() == "True")
        //                        cgd.Add(objGrowerLoanDetail(_p, long.Parse(r["GrowerWiseLoanHeaderID"].ToString()), long.Parse(r["GrowerWiseLoanDetailID"].ToString()), int.Parse(r["NoOfCPRsDeducted"].ToString()) + 1));

        //                    decimal p = payableAmountCPR;
        //                    switch (e > LoanAmount)
        //                    {
        //                        case true:
        //                            switch (decimal.Parse(r["Amount"].ToString()) == (LoanAmount + decimal.Parse(r["DedcutionAmount"].ToString()))) { case true: cl.Add(objLoan(_p, long.Parse(r["LoanHeaderID"].ToString()))); break; }
        //                            e = e - LoanAmount;
        //                            payableAmountCPR = p - LoanAmount;
        //                            break;
        //                        case false:
        //                            switch (decimal.Parse(r["Amount"].ToString()) == (e + decimal.Parse(r["DedcutionAmount"].ToString()))) { case true: cl.Add(objLoan(_p, long.Parse(r["LoanHeaderID"].ToString()))); break; }
        //                            payableAmountCPR = p - e;
        //                            LoanAmount = e;
        //                            e = 0;
        //                            break;
        //                    }

        //                    cld.Add(objLoanDeduction(_p, loanDeductionID++, (long)_p.CPRNo, long.Parse(r["LoanTypeID"].ToString()), (long)_p.SourceHeaderID, LoanAmount, long.Parse(r["LoanHeaderID"].ToString()), long.Parse(r["GrowerWiseLoanHeaderID"].ToString() != string.Empty ? r["GrowerWiseLoanHeaderID"].ToString() : "0"), long.Parse(r["GrowerWiseLoanDetailID"].ToString() != string.Empty ? r["GrowerWiseLoanDetailID"].ToString() : "0")));

        //                    deductionAmountCPR = cld.Where(u => u.CPRNo == (long)_p.CPRNo).Sum(xx => xx.DedcutionAmount);

        //                    r["LoanAmount"] = decimal.Parse(r["LoanAmount"].ToString()) - LoanAmount;
        //                    r["DedcutionAmount"] = decimal.Parse(r["DedcutionAmount"].ToString()) + LoanAmount;

        //                    if (decimal.Parse(r["Amount"].ToString()) == decimal.Parse(r["DedcutionAmount"].ToString()))
        //                        r["IsDeducted"] = "Yes";

        //                    if (e < 1)
        //                        break;
        //                }
        //            }
        //    }

        //    cpr.Add(CreateCPRCollection(_p));

        //#region If Partial scroll is found

        //if (_p.FK_PartialScrollID.HasValue && lst_PartialScroll != null)
        //{
        //    Cane_PartialScroll objPartialScroll = new Cane_PartialScroll(_p.CompanyID, _p.FK_PartialScrollID.Value, _p.PeriodID, null, sqltrans);

        //    if (objPartialScroll.IsDataLoaded)
        //    {
        //        objPartialScroll.IsPaid = true;
        //        objPartialScroll.ScrollBankAccountID = bankAccountID;
        //        objPartialScroll.UpdateDate = _p.CreateDate;
        //        objPartialScroll.UpdatedBy = _p.CreatedBy;
        //        objPartialScroll.UpdatedByValue = _p.CreatedByValue;
        //    }

        //    lst_PartialScroll.Add(objPartialScroll);
        //}

        //#endregion

        //        cpr.Where(x => x.CPRNo == (long)_p.CPRNo).ToList<Cane_CPR>().ForEach(z =>
        //        {
        //            z.PaymentAmount = payableAmountCPR;
        //            z.DeductionAmount = deductionAmountCPR;
        //            z.NetPayableAmount = (decimal)(_p.CRPAmount);
        //        });

        //        cp.Where(x => x.CPRNo == (long)_p.CPRNo).ToList<Cane_Payment>().ForEach(z =>
        //        {
        //            z.PaymentAmount = payableAmountCPR;
        //            z.CRPAmount = ((decimal)(_p.CRPAmount) + deductionAmountCPR);
        //        });
        //    }
        //}

        #endregion
    }
}

