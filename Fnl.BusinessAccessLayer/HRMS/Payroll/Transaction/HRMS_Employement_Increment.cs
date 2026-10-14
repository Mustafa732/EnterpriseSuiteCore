using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.HRMS.Payroll.Setup;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Transaction
{
    public partial class HRMS_Employement_Increment
    {
        public bool InsertEmpIncrementAndEmpGrossSalary(List<HRMS_Set_Employee> lstEmp , int CurrentFiscalID = 0, SqlTransaction sql = null)
        {
            SqlTransaction sqltrans;
            bool result = false;
            if (sql == null)
            {
                sqltrans = SqlHelper.GetTransaction("InsertEmployeeIncrement");
            }
            else
            {
                sqltrans = sql;
            }
            try
            {
                if(lstEmp.ToList() != null)
                this.Insert(sqltrans);

                foreach (HRMS_Set_Employee objemp in lstEmp)
                {
                    objemp.UpdateBasicSalaryByPersonID(sqltrans);
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
                {
                    if (sql == null)
                    {
                        SqlHelper.CommitTransaction(sqltrans);
                    }
                    else
                    { }
                }
                else
                {
                    if (sql == null)
                    {
                        SqlHelper.RollBackTransaction(sqltrans, "InsertEmployeeIncrement");
                    }
                    else
                    { }
                }
            }

            return result;
        }

    }
}
