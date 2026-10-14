using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;
using System.Text;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Transaction
{
   public partial class HRMS_SalaryDetail
   {
       #region Constant Strings

       const string Query_GetSalaryHeaderIDByPersonID = @"
select isnull(SalaryHeaderID,0) as SalaryHeaderID from HRMS_SalaryHeader 
where personid= @PersonID and CompanyID = @CompanyID and PeriodID = @PeriodID and salaryDate = @Date and Status = 1
";

       const string Query_GetCalculatedData = @"
select distinct Amount,HRMS_SalaryDetail.PayrollTypeID,HRMS_PayrollType.Description from HRMS_SalaryHeader 
inner join HRMS_SalaryDetail on HRMS_SalaryDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID
and HRMS_SalaryDetail.CompanyID = HRMS_SalaryHeader.CompanyID
and HRMS_SalaryDetail.PeriodID = HRMS_SalaryHeader.PeriodID
and HRMS_SalaryDetail.Status = 1
inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
and HRMS_PayrollType.PayrollTypeCode = HRMS_SalaryDetail.PayrollTypeCode
and HRMS_PayrollType.Formula is not null
and HRMS_PayrollType.Status = 1
where personid= @PersonID and HRMS_SalaryHeader.CompanyID = @CompanyID 
and HRMS_SalaryHeader.PeriodID = @PeriodID and salaryDate between @startingdate  and @endgdate
and HRMS_SalaryHeader.Status = 1
and Description <> 'Provident Fund'
union all
select distinct Amount*-1,HRMS_SalaryDetail.PayrollTypeID,HRMS_PayrollType.Description from HRMS_SalaryHeader 
inner join HRMS_SalaryDetail on HRMS_SalaryDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID
and HRMS_SalaryDetail.CompanyID = HRMS_SalaryHeader.CompanyID
and HRMS_SalaryDetail.PeriodID = HRMS_SalaryHeader.PeriodID
and HRMS_SalaryDetail.Status = 1
inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
and HRMS_PayrollType.PayrollTypeCode = HRMS_SalaryDetail.PayrollTypeCode
and HRMS_PayrollType.Formula is not null
and HRMS_PayrollType.Status = 1
where personid= @PersonID and HRMS_SalaryHeader.CompanyID = @CompanyID 
and HRMS_SalaryHeader.PeriodID = @PeriodID and salaryDate between @startingdate  and @endgdate
and HRMS_SalaryHeader.Status = 1
and Description = 'Provident Fund'
";


       const string Query_GetPFPostingData = @"
select Distinct HRMS_SalaryDetail.SalaryHeaderID,HRMS_SET_Employee.PersonID, HRMS_SET_Employee.FK_CustomerID
--, HRMS_SalaryDetail.SalaryDetailID,HRMS_SalaryDetail.PayrollTypeID, HRMS_PayrollType.IsPFModule
from HRMS_SalaryDetail 
Inner join HRMS_SalaryHeader  
	on HRMS_SalaryDetail.CompanyID=HRMS_SalaryHeader.CompanyID
	AND HRMS_SalaryDetail.SalaryHeaderID=HRMS_SalaryHeader.SalaryHeaderID
	AND HRMS_SalaryDetail.PeriodID=HRMS_SalaryHeader.PeriodID
	AND HRMS_SalaryHeader.Status = 1
Inner join HRMS_SET_Employee 
	ON HRMS_SET_Employee.PersonID=HRMS_SalaryHeader.PersonID
	AND HRMS_SET_Employee.CompanyID=HRMS_SalaryHeader.CompanyID
	AND HRMS_SET_Employee.Status=1 --AND HRMS_SET_Employee.IsActive=1
	INNER JOIN HRMS_PayrollType  on
HRMS_SalaryDetail.PayrollTypeID = HRMS_PayrollType.PayrollTypeID AND
HRMS_SalaryDetail.PayrollTypeCode = HRMS_PayrollType.PayrollTypeCode 
AND HRMS_PayrollType.Status = 1
where HRMS_SalaryHeader.SalaryDate between @startDate and @endDate 
and HRMS_SalaryDetail.CompanyID = @CompanyID and HRMS_SalaryDetail.PeriodID = @PeriodID
and  HRMS_SalaryDetail.Status = 1 and HRMS_PayrollType.IsPFModule = 1 
and HRMS_SalaryHeader.PostID is null or HRMS_SalaryHeader.PostID = 0
order by HRMS_SalaryDetail.SalaryHeaderID asc
";
       const string Query_GetPFPostingDataDetail = @"
select Distinct HRMS_SalaryDetail.SalaryHeaderID,HRMS_SET_Employee.PersonID 
, HRMS_SalaryDetail.SalaryDetailID,HRMS_SalaryDetail.PayrollTypeID, HRMS_PayrollType.IsPFModule
from HRMS_SalaryDetail 
Inner join HRMS_SalaryHeader  
	on HRMS_SalaryDetail.CompanyID=HRMS_SalaryHeader.CompanyID
	AND HRMS_SalaryDetail.SalaryHeaderID=HRMS_SalaryHeader.SalaryHeaderID
	AND HRMS_SalaryDetail.PeriodID=HRMS_SalaryHeader.PeriodID
	AND HRMS_SalaryHeader.Status = 1
Inner join HRMS_SET_Employee 
	ON HRMS_SET_Employee.PersonID=HRMS_SalaryHeader.PersonID
	AND HRMS_SET_Employee.CompanyID=HRMS_SalaryHeader.CompanyID
	AND HRMS_SET_Employee.Status=1 --AND HRMS_SET_Employee.IsActive=1
	INNER JOIN HRMS_PayrollType  on
HRMS_SalaryDetail.PayrollTypeID = HRMS_PayrollType.PayrollTypeID AND
HRMS_SalaryDetail.PayrollTypeCode = HRMS_PayrollType.PayrollTypeCode 
AND HRMS_PayrollType.Status = 1
where HRMS_SalaryHeader.SalaryDate between @startDate and @endDate 
and HRMS_SalaryDetail.CompanyID = @CompanyID and HRMS_SalaryDetail.PeriodID = @PeriodID
and  HRMS_SalaryDetail.Status = 1 and HRMS_PayrollType.IsPFModule = 1 
--and HRMS_SalaryHeader.PostID is null 
--or HRMS_SalaryHeader.PostID = 0
and  HRMS_SalaryDetail.SalaryHeaderID = @SalaryHeaderID
order by HRMS_SalaryDetail.SalaryHeaderID asc
";

       const string Query_GetdataForSalaryDetailPF = @"
select * from HRMS_SalaryDetail where SalaryHeaderID = @SalaryHeaderID and PayrollTypeID = @PayrollTypeID 
--and SalaryDetailID = @SalaryDetailID
and CompanyID = @CompanyID and PeriodID = @PeriodID and Status = 1
{0}
";

       const string Query_InsertPFDetail = @" INSERT INTO {0}.dbo.[HRMS_SalaryDetail] ([CompanyID], [PeriodID], [SalaryHeaderID], [SalaryDetailID], [PayrollTypeID], [PayrollTypeCode], [Amount], [Status], [CreatedBy], [CreateDate], [CreatedByValue], [DefaultAmount], [MasterAmount], [UpdatedBy], [UpdateDate], [UpdatedByValue])
VALUES(@CompanyID, @PeriodID, @SalaryHeaderID, @SalaryDetailID, @PayrollTypeID, @PayrollTypeCode, @Amount, @Status, @CreatedBy, @CreateDate, @CreatedByValue, @DefaultAmount, @MasterAmount, @UpdatedBy, @UpdateDate, @UpdatedByValue) ";


       const string Query_GetCalculatedDataForHeader = @"
select distinct Amount,HRMS_SalaryDetail.PayrollTypeID,HRMS_SalaryDetail.PayrollTypeCode,HRMS_SalaryDetail.Amount,(GrossAmount) as GrossAmount,(deductionAmount) as DeductionAmount,
(netamount) as NetAmount,(GrossAmountTaxable) as GrossAmountTaxable,(MasterSalary) as MasterSalary,DaysWorking,DaysAbsent,DaysLWP,DaysLate,DaysEarly,DaysTotal,TaxAmount,ContributionEmployer  from HRMS_SalaryHeader 
inner join HRMS_SalaryDetail on HRMS_SalaryDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID
and HRMS_SalaryDetail.CompanyID = HRMS_SalaryHeader.CompanyID
and HRMS_SalaryDetail.PeriodID = HRMS_SalaryHeader.PeriodID
and HRMS_SalaryDetail.Status = 1
inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
and HRMS_PayrollType.PayrollTypeCode = HRMS_SalaryDetail.PayrollTypeCode
and HRMS_PayrollType.Formula is not null
and HRMS_PayrollType.Status = 1
where personid= @PersonID and HRMS_SalaryHeader.CompanyID = @CompanyID 
and HRMS_SalaryHeader.PeriodID = @PeriodID and salaryDate between @startingdate  and @endgdate
and HRMS_SalaryHeader.Status = 1
and Description <> 'Provident Fund'
union all
select distinct (Amount *-1) as Amount,HRMS_SalaryDetail.PayrollTypeID,HRMS_SalaryDetail.PayrollTypeCode,HRMS_SalaryDetail.Amount,(GrossAmount) as GrossAmount,(deductionAmount) as DeductionAmount,
(netamount) as NetAmount,(GrossAmountTaxable) as GrossAmountTaxable,(MasterSalary) as MasterSalary,DaysWorking,DaysAbsent,DaysLWP,DaysLate,DaysEarly,DaysTotal,TaxAmount,ContributionEmployer  from HRMS_SalaryHeader 
inner join HRMS_SalaryDetail on HRMS_SalaryDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID
and HRMS_SalaryDetail.CompanyID = HRMS_SalaryHeader.CompanyID
and HRMS_SalaryDetail.PeriodID = HRMS_SalaryHeader.PeriodID
and HRMS_SalaryDetail.Status = 1
inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
and HRMS_PayrollType.PayrollTypeCode = HRMS_SalaryDetail.PayrollTypeCode
and HRMS_PayrollType.Formula is not null
and HRMS_PayrollType.Status = 1
where personid= @PersonID and HRMS_SalaryHeader.CompanyID = @CompanyID 
and HRMS_SalaryHeader.PeriodID = @PeriodID and salaryDate between @startingdate  and @endgdate
and HRMS_SalaryHeader.Status = 1
and Description = 'Provident Fund'
";

       const string Query_DeleteData_BySalaryHeaderID = @" Delete from [HRMS_SalaryDetail]  WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [SalaryHeaderID] = @SalaryHeaderID ";

       const string Query_DeleteData_ByMonthYearSalaryDetail = @" Delete from [HRMS_SalaryDetail]  WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and  datepart(month,HRMS_SalaryDetail.CreateDate) = @Month and datename(year,HRMS_SalaryDetail.CreateDate) = @Year";

       const string Query_DeleteData_ByMonthYearSalaryHeader = @" update [HRMS_SalaryHeader]  set Status = 0 WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and  datepart(month,HRMS_SalaryHeader.SalaryDate) = @Month and datename(year,HRMS_SalaryHeader.SalaryDate) = @Year";

       const string Query_DeleteData_SalaryHeaderSelected = @" update [HRMS_SalaryHeader]  set Status = 0 WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and  datepart(month,HRMS_SalaryHeader.SalaryDate) = @Month and datename(year,HRMS_SalaryHeader.SalaryDate) = @Year and HRMS_SalaryHeader.SalaryHeaderID = @SalaryHeaderID";

       const string Query_DeleteData_SalaryHeaderSelected_SalDel = @" update [HRMS_SalaryHeader]  set Status = 0 WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and  datepart(month,HRMS_SalaryHeader.SalaryDate) = @Month and datename(year,HRMS_SalaryHeader.SalaryDate) = @Year {0}";

       const string Query_DeleteData_ByMonthYearSalaryHeader2 = @" update [HRMS_SalaryHeader]  set Status = 0 WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and  datepart(month,HRMS_SalaryHeader.SalaryDate) = @Month and datename(year,HRMS_SalaryHeader.SalaryDate) = @Year and PersonID = @PersonID";

       const string Query_DeleteData_ByMonthYearSalaryHeader2DailyWages = @" update [HRMS_SalaryHeader]  set Status = 0 WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and  FromDate = @FromDate and ToDate = @ToDate and PersonID = @PersonID";

       const string Query_DeleteData_BySalaryHeaderIDForDetail = @"  update [HRMS_SalaryDetail]  set Status = 0 WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID ";

       const string Query_DeleteData_BySalaryHeaderIDForDetailTemp = @"

 update [HRMS_SalaryDetail]  set Status = 0 WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID 
  and [HRMS_SalaryDetail].SalaryHeaderID in 
 (select SalaryHeaderID from [HRMS_SalaryHeader]
 where datepart(month,[HRMS_SalaryHeader].SalaryDate) = @month and datepart(year,[HRMS_SalaryHeader].SalaryDate) = @year And SalaryHeaderID = @SalaryHeaderID)
";


       #endregion

       #region Class Methods

       public static bool DeleteDataBySalaryHeaderID(SqlTransaction objTransaction,  int companyID, int periodID, int salaryHeaderID)
       {
           bool result = false;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[8];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[3] = new SqlParameter("@SalaryHeaderID", salaryHeaderID);
               //sqlparams[4] = new SqlParameter("@Status", false);
               //sqlparams[5] = new SqlParameter("@UpdatedBy", updatedBy);
               //sqlparams[6] = new SqlParameter("@UpdateDate", updateDate);
               //sqlparams[7] = new SqlParameter("@UpdatedByValue", updatedByValue);

               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteData_BySalaryHeaderID, sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DeleteData_BySalaryHeaderID, sqlparams) > 0)
                       result = true;
               }
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return result;
       }

       public static bool DeleteDataByMonthYearSalaryDetail(int companyID, int periodID, string month, string year, SqlTransaction objTransaction)
       {
           bool result = false;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[4];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);             
               sqlparams[2] = new SqlParameter("@Month", month);
               sqlparams[3] = new SqlParameter("@Year", year); ;

               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteData_ByMonthYearSalaryDetail, sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DeleteData_ByMonthYearSalaryDetail, sqlparams) > 0)
                       result = true;
               }
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return result;
       }

       public static bool DeleteDataByMonthYearSalaryHeader(int companyID, int periodID, string month, string year, SqlTransaction objTransaction)
       {
           bool result = false;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[5];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@Month", month);
               sqlparams[3] = new SqlParameter("@Year", year);

               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteData_ByMonthYearSalaryHeader, sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DeleteData_ByMonthYearSalaryHeader, sqlparams) > 0)
                       result = true;
               }
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return result;
       }

       public static bool DeleteDataByMonthYearSalaryHeader2(int companyID, int periodID, string month, string year, int PersonID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
       {
           bool result = false;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[5];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@Month", month);
               sqlparams[3] = new SqlParameter("@Year", year);
               sqlparams[4] = new SqlParameter("@PersonID", PersonID);

               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteData_ByMonthYearSalaryHeader2, sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DeleteData_ByMonthYearSalaryHeader2, sqlparams) > 0)
                       result = true;
               }
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return result;
       }

       public static bool DeleteDataByMonthYearSalaryHeader2DailyWages(int companyID, int periodID, string FromDate, string ToDate, int PersonID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
       {
           bool result = false;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[5];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@FromDate", FromDate);
               sqlparams[3] = new SqlParameter("@ToDate", ToDate);
               sqlparams[4] = new SqlParameter("@PersonID", PersonID);

               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteData_ByMonthYearSalaryHeader2DailyWages, sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DeleteData_ByMonthYearSalaryHeader2DailyWages, sqlparams) > 0)
                       result = true;
               }
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return result;
       }

       public static bool DeleteDataBySalaryHeader(int companyID, int periodID, int SalaryHeaderID, SqlTransaction objTransaction = null, SqlConnection objConnection = null,int? month=null,int? year=null)
       {
           bool result = false;

           try
           {
               string query = "";

               SqlParameter[] sqlparams = new SqlParameter[5];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@SalaryHeaderID", SalaryHeaderID);
               sqlparams[3] = new SqlParameter("@month", month);
               sqlparams[4] = new SqlParameter("@year", year);

               if (month.HasValue)
                   query = Query_DeleteData_BySalaryHeaderIDForDetailTemp;
               else
                   query = Query_DeleteData_BySalaryHeaderIDForDetail;



               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, query, sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, query, sqlparams) > 0)
                       result = true;
               }
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return result;
       }

       public static bool DeleteDataBySalaryHeader2(int companyID, int periodID, int SalaryHeaderID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
       {
           bool result = false;

           #region Query

           const string Query_DeleteData_BySalaryHeaderID = @" update [HRMS_SalaryDetail]  set Status = 0 WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and SalaryHeaderID = @SalaryHeaderID ";
           #endregion
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[4];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@SalaryHeaderID", SalaryHeaderID);


               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteData_BySalaryHeaderID, sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DeleteData_BySalaryHeaderID, sqlparams) > 0)
                       result = true;
               }
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return result;
       }

       public static bool DeleteDataSalaryHeaderSelected(int companyID, int periodID, string month, string year, int SalHdrID, SqlTransaction objTransaction)
       {
           bool result = false;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[5];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@Month", month);
               sqlparams[3] = new SqlParameter("@Year", year);
               sqlparams[4] = new SqlParameter("@SalaryHeaderID", SalHdrID);

               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteData_SalaryHeaderSelected, sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DeleteData_SalaryHeaderSelected, sqlparams) > 0)
                       result = true;
               }
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return result;
       }

       public int GetSalaryHeaderIDByPersonID(int companyID, int periodID, DateTime Date, int PersonID ,SqlTransaction objTransaction = null, SqlConnection objConnection = null)
       {
           int ID = 0;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[4];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@Date", Date);
               sqlparams[3] = new SqlParameter("@PersonID", PersonID);

               if (objTransaction != null)
                   ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_GetSalaryHeaderIDByPersonID, sqlparams));
               else if (objConnection != null)
                   ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_GetSalaryHeaderIDByPersonID, sqlparams));
               else
                   ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetSalaryHeaderIDByPersonID, sqlparams));
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return ID;
       }

       public static bool DeleteSalaryDataSelected(int companyID, int periodID, string month, string year, int SalHdrID, bool DeleteAll, SqlTransaction objTransaction)
       {
           bool result = false;
           StringBuilder sb = new StringBuilder();
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[4];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@Month", month);
               sqlparams[3] = new SqlParameter("@Year", year);
               //sqlparams[4] = new SqlParameter("@SalaryHeaderID", SalHdrID);

               if (DeleteAll == false)
                   sb.Append(" and HRMS_SalaryHeader.SalaryHeaderID = " + SalHdrID);

               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, string.Format(Query_DeleteData_SalaryHeaderSelected_SalDel, sb.ToString()), sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, string.Format(Query_DeleteData_SalaryHeaderSelected_SalDel, sb.ToString()), sqlparams) > 0)
                       result = true;
               }
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return result;
       }

       public static bool DeleteDataBySalaryHead(int companyID, int periodID, int SalaryHeaderID, bool DeleteAll, SqlTransaction objTransaction = null, SqlConnection objConnection = null,int? month=null,int? year=null)
       {
           bool result = false;
           StringBuilder sb = new StringBuilder();
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[4];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@month", month);
               sqlparams[3] = new SqlParameter("@year", year);
               //sqlparams[2] = new SqlParameter("@SalaryHeaderID", SalaryHeaderID);

               string query = "";

               if (DeleteAll == false)
               {
                   sb.Append(" AND SalaryHeaderID = " + SalaryHeaderID);
                   query = Query_DeleteData_BySalaryHeaderIDForDetail + sb.ToString();
               }

               if (DeleteAll == true)
               { 
                 query = Query_DeleteData_BySalaryHeaderIDForDetailTemp + sb.ToString();
               }


               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, query, sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, query, sqlparams) > 0)
                       result = true;
               }
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return result;
       }


       public static DataTable GetCalculatedData(int companyID, int periodID, int personid, DateTime startingdate, DateTime endgdate)
       {
        DataTable dt = null;
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[5];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@PersonID", personid);
               sqlparams[3] = new SqlParameter("@startingdate", startingdate);
               sqlparams[4] = new SqlParameter("@endgdate", endgdate);

               dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetCalculatedData, sqlparams).Tables[0];
                      
            
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }

       public static DataTable GetDataForPFPosting(int companyID, int periodID, DateTime startDate, DateTime endDate)
       {
           DataTable dt = null;
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[5];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               //sqlparams[2] = new SqlParameter("@PersonID", personid);
               sqlparams[3] = new SqlParameter("@startDate", startDate);
               sqlparams[4] = new SqlParameter("@endDate", endDate);

               dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetPFPostingData, sqlparams).Tables[0];


           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }

       public static DataTable GetDataForPFPostingDetail(int companyID, int periodID, DateTime startDate, DateTime endDate, int SalaryHeaderID)
       {
           DataTable dt = null;
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[5];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@SalaryHeaderID", SalaryHeaderID);
               sqlparams[3] = new SqlParameter("@startDate", startDate);
               sqlparams[4] = new SqlParameter("@endDate", endDate);

               dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetPFPostingDataDetail, sqlparams).Tables[0];


           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }


       public static DataTable GetdataForSalaryDetailPF(int companyID, int periodID, int SalaryHeaderID, int PayrollTypeID, Nullable<int> SalaryDetailID = 0, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
       {
           DataTable dt = null;
           StringBuilder sb = new StringBuilder();
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[4];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@SalaryHeaderID", SalaryHeaderID);
               //sqlparams[3] = new SqlParameter("@SalaryDetailID", SalaryDetailID);
               sqlparams[3] = new SqlParameter("@PayrollTypeID", PayrollTypeID);

               if (SalaryDetailID > 0)
               {
                   sb.Append(" and SalaryDetailID = " + SalaryDetailID + " ");
               }
               
               DataSet ds = null;

               if (objConnection == null)
               {
                   // ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetdataForAlreadySalaryGenerate + sb.ToString(), sqlparams);
                   if (sqltrans != null)
                       ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, string.Format(Query_GetdataForSalaryDetailPF, sb.ToString()), sqlparams);
                   else
                       ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetdataForSalaryDetailPF, sb.ToString()), sqlparams);
               }
               else
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetdataForSalaryDetailPF, sb.ToString()), sqlparams);

               if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                   dt = ds.Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }

       public bool InsertPFDetail(string DatabaseName, SqlTransaction objTransaction = null)
       {
           bool result = false;

           try
           {
               StringBuilder sb = new StringBuilder();
               SqlParameter[] sqlparams = new SqlParameter[16];
               sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
               sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
               sqlparams[2] = new SqlParameter("@SalaryHeaderID", f_SalaryHeaderID);
               sqlparams[3] = new SqlParameter("@SalaryDetailID", f_SalaryDetailID);
               sqlparams[4] = new SqlParameter("@PayrollTypeID", f_PayrollTypeID);
               sqlparams[5] = new SqlParameter("@PayrollTypeCode", f_PayrollTypeCode);

               if (f_Amount.HasValue)
                   sqlparams[6] = new SqlParameter("@Amount", f_Amount.Value);
               else
                   sqlparams[6] = new SqlParameter("@Amount", DBNull.Value);

               sqlparams[7] = new SqlParameter("@Status", f_Status);
               sqlparams[8] = new SqlParameter("@CreatedBy", f_CreatedBy);
               sqlparams[9] = new SqlParameter("@CreateDate", f_CreateDate);
               sqlparams[10] = new SqlParameter("@CreatedByValue", f_CreatedByValue);

               if (f_DefaultAmount.HasValue)
                   sqlparams[11] = new SqlParameter("@DefaultAmount", f_DefaultAmount.Value);
               else
                   sqlparams[11] = new SqlParameter("@DefaultAmount", DBNull.Value);

               if (f_MasterAmount.HasValue)
                   sqlparams[12] = new SqlParameter("@MasterAmount", f_MasterAmount.Value);
               else
                   sqlparams[12] = new SqlParameter("@MasterAmount", DBNull.Value);
               if (f_UpdatedBy.HasValue)
                   sqlparams[13] = new SqlParameter("@UpdatedBy", f_UpdatedBy.Value);
               else
                   sqlparams[13] = new SqlParameter("@UpdatedBy", DBNull.Value);

               if (f_UpdateDate.HasValue)
                   sqlparams[14] = new SqlParameter("@UpdateDate", f_UpdateDate.Value);
               else
                   sqlparams[14] = new SqlParameter("@UpdateDate", DBNull.Value);

               if (!string.IsNullOrEmpty(f_UpdatedByValue))
                   sqlparams[15] = new SqlParameter("@UpdatedByValue", f_UpdatedByValue);
               else
                   sqlparams[15] = new SqlParameter("@UpdatedByValue", DBNull.Value);

               //sqlparams[16] = new SqlParameter("@DatabaseName", DatabaseName);
               sb.Append(DatabaseName);

               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, string.Format(Query_InsertPFDetail, sb.ToString()), sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, string.Format(Query_InsertPFDetail, sb.ToString()), sqlparams) > 0)
                       result = true;
               }
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return result;
       }

       public static DataTable GetCalculatedDataForHeader(int companyID, int periodID, int personid, DateTime startingdate, DateTime endgdate)
       {
           DataTable dt = null;
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[5];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@PersonID", personid);
               sqlparams[3] = new SqlParameter("@startingdate", startingdate);
               sqlparams[4] = new SqlParameter("@endgdate", endgdate);

               dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetCalculatedDataForHeader, sqlparams).Tables[0];


           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }
       #endregion
   }
}
