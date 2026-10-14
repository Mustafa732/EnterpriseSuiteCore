using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;

using Common.Utils;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.AR.Setup
{
    public partial class AR_SET_Customer
    {
        #region Class Methods

        public DataTable GetCustomers(int companyID, int periodID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                string Query_GetAllCustomer = @" Select CustomerName, CustomerID from AR_SET_Customer WHERE PeriodID = @PeriodID ";
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllCustomer, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public bool Update(List<AR_SET_BusinessPartnerType> bpTypes, int businessPartnerID, AR_SET_CustomerRefDetail CustomerRefDetail=null)
        {
            bool result = false;
            SqlTransaction objTransaction = SqlHelper.GetTransaction("UpdateCustomer");

            try
            {
                this.UpdateDataByKey(objTransaction);

                using (AR_SET_BusinessPartnerType obj_BPType = new AR_SET_BusinessPartnerType())
                    obj_BPType.Delete(this.f_CompanyID, this.f_PeriodID, this.f_CustomerID, objTransaction);

                foreach (AR_SET_BusinessPartnerType businessPartnerType in bpTypes)
                    businessPartnerType.Insert(objTransaction);

                result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            finally
            {
                if (result)
                    objTransaction.Commit();
                else
                    objTransaction.Rollback("UpdateCustomer");
            }

            return result;
        }

        public bool Insert(List<AR_SET_BusinessPartnerType> bpTypes)
        {
            bool result = false;
            SqlTransaction objTransaction = SqlHelper.GetTransaction("InsertCustomer");

            try
            {

                this.Insert(objTransaction);

                foreach (AR_SET_BusinessPartnerType businessPartnerType in bpTypes)
                    businessPartnerType.Insert(objTransaction);

                result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            finally
            {
                if (result)
                    objTransaction.Commit();
                else
                    objTransaction.Rollback("InsertCustomer");
            }

            return result;
        }

        public static DataTable GetDataByTypes(int companyID, int periodID, short typeCode, string strWhere = null, SqlConnection objConnection = null,Nullable<DateTime> FromDate = null, Nullable<DateTime> ToDate = null,string filter="")
        {
            DataSet ds = null;
            DataTable dtToReturn = null;
            string query = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TypeCode", typeCode);
                sqlparams[3] = new SqlParameter("@FromDate", FromDate);
                sqlparams[4] = new SqlParameter("@ToDate", ToDate);

                if (strWhere == null)
                    strWhere = string.Empty;

                //Old Query with BP types
//                string Query_SelectByTypes = @" 
//SELECT (Case when c.CNICNumber is not null then c.CNICNumber else c.NTNNumber END) AS CNIC, convert(varchar(16), c.CustomerID) as CustomerCode, 
//SET_City.CityName,CountryName,HRMS_Set_Employee.EmployeeCode,
//    ISNULL(bp.FK_CodeCombinationID, c.CustomerAccountCCID) as CustomerCCID, c.* , bp.typecode
//FROM AR_SET_Customer c 
//    INNER JOIN AR_SET_BusinessPartnerTypes bp ON bp.BusinessPartnerID = c.CustomerID --AND bp.CompanyID = c.CompanyID 
//AND bp.PeriodID = c.PeriodID 
//	Left Join HRMS_Set_Employee on HRMS_Set_Employee.FK_CustomerID = c.CustomerID
//	AND HRMS_Set_Employee.CompanyID = c.CompanyID
//	LEFT JOIN  SET_City ON SET_City.CityID=c.CityID
//	LEFT JOIN SET_Country ON SET_Country.CountryID=SET_City.CountryID
//WHERE c.PeriodID <= @PeriodID AND c.Status = 1 {0}";

                string Query_SelectByTypes = @" 
SELECT (Case when c.CNICNumber is not null then c.CNICNumber else c.NTNNumber END) AS CNIC, convert(varchar(16), c.CustomerID) as CustomerCode, 
SET_City.CityName,CountryName, EmployeeCode
,c.*
FROM AR_SET_Customer c 
	LEFT JOIN  SET_City ON SET_City.CityID=c.CityID
	LEFT JOIN SET_Country ON SET_Country.CountryID=SET_City.CountryID
WHERE c.PeriodID <= @PeriodID AND c.Status = 1  
{0}";


                if (typeCode > 0)
                    query = Query_SelectByTypes + " and c.CustomerID in (select BusinessPartnerID from AR_SET_BusinessPartnerTypes where TypeCode = @TypeCode )";
                else
                    query = Query_SelectByTypes;


                if (FromDate != null && ToDate != null)
                    query = query + " and c.CustomerID in (select CustomerID from Fabrics_CustomerChallanInfo where ChallanDate between @FromDate and @ToDate and Status = 1)";

            

                if(objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, filter) + strWhere + " ORDER BY c.CustomerName", sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(query, filter) + strWhere + " ORDER BY c.CustomerName", sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            //dtBind = dtBind.AsEnumerable().Distinct(DataRowComparer.Default).CopyToDataTable();
            return dtToReturn;
        }

        public static DataTable GetDataByTypesTransporter(int companyID, int periodID, short typeCode, string strWhere = null, SqlConnection objConnection = null, Nullable<DateTime> FromDate = null, Nullable<DateTime> ToDate = null, string filter = "")
        {
            DataSet ds = null;
            DataTable dtToReturn = null;
            string query = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TypeCode", typeCode);
                sqlparams[3] = new SqlParameter("@FromDate", FromDate);
                sqlparams[4] = new SqlParameter("@ToDate", ToDate);

                if (strWhere == null)
                    strWhere = string.Empty;

                string Query_SelectByTypes = @" 
SELECT (Case when c.CNICNumber is not null then c.CNICNumber else c.NTNNumber END) AS CNIC, convert(varchar(16), c.CustomerID) as CustomerCode, 
SET_City.CityName,CountryName,HRMS_Set_Employee.EmployeeCode,
    ISNULL(bp.FK_CodeCombinationID, c.CustomerAccountCCID) as CustomerCCID, c.* , bp.typecode
,c.CustomerID as TransporterID , c.CustomerName as TransporterName
FROM AR_SET_Customer c 
    INNER JOIN AR_SET_BusinessPartnerTypes bp ON bp.BusinessPartnerID = c.CustomerID --AND bp.CompanyID = c.CompanyID 
AND bp.PeriodID = c.PeriodID 
	Left Join HRMS_Set_Employee on HRMS_Set_Employee.FK_CustomerID = c.CustomerID
	AND HRMS_Set_Employee.CompanyID = c.CompanyID
	LEFT JOIN  SET_City ON SET_City.CityID=c.CityID
	LEFT JOIN SET_Country ON SET_Country.CountryID=SET_City.CountryID
WHERE c.PeriodID <= @PeriodID AND c.Status = 1 {0}";

                if (typeCode > 0)
                    query = Query_SelectByTypes + " AND bp.TypeCode = @TypeCode";
                else
                    query = Query_SelectByTypes;


                if (FromDate != null && ToDate != null)
                    query = query + " and c.CustomerID in (select CustomerID from Fabrics_CustomerChallanInfo where ChallanDate between @FromDate and @ToDate and Status = 1)";



                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, filter) + strWhere + " --ORDER BY c.CustomerID", sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(query, filter) + strWhere + " --ORDER BY c.CustomerID", sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            //dtBind = dtBind.AsEnumerable().Distinct(DataRowComparer.Default).CopyToDataTable();
            return dtToReturn;
        }

        public DataTable GetDataByTypesForCOA(int companyID, int periodID, short typeCode, string strWhere = null, SqlConnection objConnection = null)
        {
            DataSet ds = null;
            DataTable dtToReturn = null;
            string query = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TypeCode", typeCode);

                if (strWhere == null)
                    strWhere = string.Empty;


               

                //Old With BP Type
//                string Query_GetDataByTypesForCOA = @" 
//
//SELECT DISTINCT (Case when c.CNICNumber is not null then c.CNICNumber else c.NTNNumber END) AS CNIC, convert(varchar(16), c.CustomerID) as CustomerCode, c.CustomerID, 
//
//    c.CustomerName, c.FatherName, c.AddressLine1, bp.FK_CodeCombinationID as CustomerAccountCCID ,bp.TypeCode, AR_SET_BusinessPartner.BusinessPartnerName
//	, SET_City.CityName
//	, HRMS_Set_Employee.EmployeeCode
//FROM AR_SET_Customer c 
//    INNER JOIN AR_SET_BusinessPartnerTypes bp ON bp.BusinessPartnerID = c.CustomerID --AND bp.CompanyID = c.CompanyID
//AND bp.PeriodID = c.PeriodID 
//	INNER JOIN AR_SET_BusinessPartner  ON AR_SET_BusinessPartner.BusinessPartnerID = bp.TypeCode
//	Left JOin HRMS_Set_Employee on HRMS_Set_Employee.Fk_CustomerID = c.CustomerID
//	AND HRMS_Set_Employee.CompanyID = c.CompanyID
//	Inner Join SET_City on SET_City.CityID=c.CityID
//WHERE c.PeriodID <= @PeriodID AND c.Status = 1 AND c.IsActive = 1   
//";

                string Query_GetDataByTypesForCOA = @" 
SELECT DISTINCT --(Case when c.CNICNumber is not null then c.CNICNumber else c.NTNNumber END) AS CNIC,
Case when isnull(c.CNICNumber,'') != '' then c.CNICNumber 
else 
(case When isnull(c.NTNNumber,'') != '' Then c.NTNNumber Else '----- ------- -' End) END AS CNIC,
convert(varchar(16), c.CustomerID) as CustomerCode, c.CustomerID, 
c.CustomerName, c.FatherName, c.AddressLine1, '' as CustomerAccountCCID ,'' as TypeCode, '' as BusinessPartnerName, ISNULL(SET_City.CityName,'-') as CityName , ISNULL(c.EmployeeCode,'-') as EmployeeCode
,ISNULL(Cane_Grower.GrowerCode,'-') as GrowerCode
FROM AR_SET_Customer c 

	Inner Join SET_City on SET_City.CityID=c.CityID
	Left Join Cane_Grower On Cane_Grower.FK_CustomerID = c.CustomerID
WHERE c.PeriodID <= @PeriodID AND c.Status = 1 AND c.IsActive = 1
";

                if (typeCode > 0)
                    query = Query_GetDataByTypesForCOA + " and c.CustomerID in (select BusinessPartnerID from AR_SET_BusinessPartnerTypes where TypeCode = @TypeCode )";
                else
                    query = Query_GetDataByTypesForCOA;

                query = query + " {0} ";

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(query , strWhere + " ORDER BY c.CustomerName "), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, String.Format(query, strWhere + " ORDER BY c.CustomerName "), sqlparams);

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

        public DataTable GetDataByTypesForCOA_GrowerBusinessPartner(Guid UserID, int companyID, int periodID, short typeCode, string strWhere = null, SqlConnection objConnection = null)
        {
            DataSet ds = null;
            DataTable dtToReturn = null;
            string query = string.Empty;
            string BPCondition = string.Empty;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TypeCode", typeCode);
                sqlparams[3] = new SqlParameter("@UserID", UserID);

                if (strWhere == null)
                    strWhere = string.Empty;




                //Old With BP Type
                //                string Query_GetDataByTypesForCOA = @" 
                //
                //SELECT DISTINCT (Case when c.CNICNumber is not null then c.CNICNumber else c.NTNNumber END) AS CNIC, convert(varchar(16), c.CustomerID) as CustomerCode, c.CustomerID, 
                //
                //    c.CustomerName, c.FatherName, c.AddressLine1, bp.FK_CodeCombinationID as CustomerAccountCCID ,bp.TypeCode, AR_SET_BusinessPartner.BusinessPartnerName
                //	, SET_City.CityName
                //	, HRMS_Set_Employee.EmployeeCode
                //FROM AR_SET_Customer c 
                //    INNER JOIN AR_SET_BusinessPartnerTypes bp ON bp.BusinessPartnerID = c.CustomerID --AND bp.CompanyID = c.CompanyID
                //AND bp.PeriodID = c.PeriodID 
                //	INNER JOIN AR_SET_BusinessPartner  ON AR_SET_BusinessPartner.BusinessPartnerID = bp.TypeCode
                //	Left JOin HRMS_Set_Employee on HRMS_Set_Employee.Fk_CustomerID = c.CustomerID
                //	AND HRMS_Set_Employee.CompanyID = c.CompanyID
                //	Inner Join SET_City on SET_City.CityID=c.CityID
                //WHERE c.PeriodID <= @PeriodID AND c.Status = 1 AND c.IsActive = 1   
                //";

                string Query_GetDataByTypesForCOA = @" 
select * from (
SELECT DISTINCT --isnull( (Case when c.CNICNumber is not null then c.CNICNumber else c.NTNNumber END),'---- --- ---') AS CNIC,
Case when isnull(c.CNICNumber,'') != '' then c.CNICNumber 
else 
(case When isnull(c.NTNNumber,'') != '' Then c.NTNNumber Else '----- ------- -' End) END AS CNIC,
convert(varchar(16), c.CustomerID) as CustomerCode, c.CustomerID, 
c.CustomerName, c.FatherName, c.AddressLine1, '' as CustomerAccountCCID ,'' as TypeCode, '' as BusinessPartnerName, SET_City.CityName , c.EmployeeCode
,ISNULL(Cane_Grower.GrowerCode,'-') as GrowerCode
FROM AR_SET_Customer c 

	Inner Join SET_City on SET_City.CityID=c.CityID
	Left Join Cane_Grower On Cane_Grower.FK_CustomerID = c.CustomerID
WHERE c.PeriodID <= @PeriodID AND c.Status = 1 AND c.IsActive = 1  
 and c.CustomerID not in (select FK_CustomerID from Cane_Grower where PeriodID = @PeriodID)
{0}

UNION ALL

SELECT DISTINCT --isnull( (Case when c.CNICNumber is not null then c.CNICNumber else c.NTNNumber END),'---- --- ---') AS CNIC,
Case when isnull(c.CNICNumber,'') != '' then c.CNICNumber 
else 
(case When isnull(c.NTNNumber,'') != '' Then c.NTNNumber Else '----- ------- -' End) END AS CNIC,
convert(varchar(16), c.CustomerID) as CustomerCode, c.CustomerID, 
c.CustomerName, c.FatherName, c.AddressLine1, '' as CustomerAccountCCID ,'' as TypeCode, '' as BusinessPartnerName, SET_City.CityName , c.EmployeeCode
,ISNULL(Cane_Grower.GrowerCode,'-') as GrowerCode
FROM AR_SET_Customer c 

	Inner Join SET_City on SET_City.CityID=c.CityID
	Left Join Cane_Grower On Cane_Grower.FK_CustomerID = c.CustomerID
WHERE c.PeriodID <= @PeriodID AND c.Status = 1 AND c.IsActive = 1  
 and c.CustomerID in (select FK_CustomerID from Cane_Grower where PeriodID = @PeriodID)
 and 1 = (case when (Select ISNULL(GrowerBusinessPartner,0) from Usersoption where UserID = @UserID) != 0
 then 1 else 0 end)
{0}
)t
where CustomerName is not null 
{1}
 ORDER BY t.CustomerName
";

                if (typeCode > 0)
                    BPCondition = " and t.CustomerID in (select BusinessPartnerID from AR_SET_BusinessPartnerTypes where TypeCode = @TypeCode )";       
        
                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(Query_GetDataByTypesForCOA, strWhere, BPCondition), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, String.Format(Query_GetDataByTypesForCOA, strWhere, BPCondition), sqlparams);

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

        public DataTable GetDataByTypesForCOA_LeakageBusinessPartner(Guid UserID, int companyID, int periodID, short typeCode, string strWhere = null, SqlConnection objConnection = null)
        {
            DataSet ds = null;
            DataTable dtToReturn = null;
            string query = string.Empty;
            string BPCondition = string.Empty;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TypeCode", typeCode);
                sqlparams[3] = new SqlParameter("@UserID", UserID);

                if (strWhere == null)
                    strWhere = string.Empty;




                //Old With BP Type
                //                string Query_GetDataByTypesForCOA = @" 
                //
                //SELECT DISTINCT (Case when c.CNICNumber is not null then c.CNICNumber else c.NTNNumber END) AS CNIC, convert(varchar(16), c.CustomerID) as CustomerCode, c.CustomerID, 
                //
                //    c.CustomerName, c.FatherName, c.AddressLine1, bp.FK_CodeCombinationID as CustomerAccountCCID ,bp.TypeCode, AR_SET_BusinessPartner.BusinessPartnerName
                //	, SET_City.CityName
                //	, HRMS_Set_Employee.EmployeeCode
                //FROM AR_SET_Customer c 
                //    INNER JOIN AR_SET_BusinessPartnerTypes bp ON bp.BusinessPartnerID = c.CustomerID --AND bp.CompanyID = c.CompanyID
                //AND bp.PeriodID = c.PeriodID 
                //	INNER JOIN AR_SET_BusinessPartner  ON AR_SET_BusinessPartner.BusinessPartnerID = bp.TypeCode
                //	Left JOin HRMS_Set_Employee on HRMS_Set_Employee.Fk_CustomerID = c.CustomerID
                //	AND HRMS_Set_Employee.CompanyID = c.CompanyID
                //	Inner Join SET_City on SET_City.CityID=c.CityID
                //WHERE c.PeriodID <= @PeriodID AND c.Status = 1 AND c.IsActive = 1   
                //";

                string Query_GetDataByTypesForCOAleakage = @" 
select * from (
SELECT DISTINCT --isnull( (Case when c.CNICNumber is not null then c.CNICNumber else c.NTNNumber END),'---- --- ---') AS CNIC,
Case when isnull(c.CNICNumber,'') != '' then c.CNICNumber 
else 
(case When isnull(c.NTNNumber,'') != '' Then c.NTNNumber Else '----- ------- -' End) END AS CNIC,
convert(varchar(16), c.CustomerID) as CustomerCode, c.CustomerID, 
    c.CustomerName, c.FatherName, c.AddressLine1, '' as CustomerAccountCCID ,'' as TypeCode, '' as BusinessPartnerName, SET_City.CityName , c.EmployeeCode
	,ISNULL(Cane_Grower.GrowerCode,'-') as GrowerCode
FROM AR_SET_Customer c 

	Inner Join SET_City on SET_City.CityID=c.CityID
	Left Join Cane_Grower On Cane_Grower.FK_CustomerID = c.CustomerID
WHERE c.PeriodID <= @PeriodID AND c.Status = 1 AND c.IsActive = 1  
 and c.CustomerID not in (select FK_CustomerID from Cane_Grower where PeriodID = @PeriodID)
{0}

UNION ALL

SELECT DISTINCT --isnull( (Case when c.CNICNumber is not null then c.CNICNumber else c.NTNNumber END),'---- --- ---') AS CNIC,
Case when isnull(c.CNICNumber,'') != '' then c.CNICNumber 
else 
(case When isnull(c.NTNNumber,'') != '' Then c.NTNNumber Else '----- ------- -' End) END AS CNIC,
convert(varchar(16), c.CustomerID) as CustomerCode, c.CustomerID, 
    c.CustomerName, c.FatherName, c.AddressLine1, '' as CustomerAccountCCID ,'' as TypeCode, '' as BusinessPartnerName, SET_City.CityName , c.EmployeeCode
	,ISNULL(Cane_Grower.GrowerCode,'-') as GrowerCode
FROM AR_SET_Customer c 

	Inner Join SET_City on SET_City.CityID=c.CityID
	Left Join Cane_Grower On Cane_Grower.FK_CustomerID = c.CustomerID
WHERE c.PeriodID <= @PeriodID AND c.Status = 1 AND c.IsActive = 1  
 and c.CustomerID in (select FK_CustomerID from Cane_Grower where PeriodID = @PeriodID)
 and 1 = (case when (Select ISNULL(GrowerBusinessPartner,0) from Usersoption where UserID = @UserID) != 0
 then 1 else 0 end)
{0}
)t
INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = t.CustomerID 
                --AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
               -- AND AR_SET_BusinessPartnerTypes.PeriodID = t.PeriodID 
                 
   INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode
where AR_SET_BusinessPartnerTypes.TypeCode = 201 and CustomerName is not null 
{1}
 ORDER BY t.CustomerName
";

                if (typeCode > 0)
                    BPCondition = " and t.CustomerID in (select BusinessPartnerID from AR_SET_BusinessPartnerTypes where TypeCode = @TypeCode )";

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(Query_GetDataByTypesForCOAleakage, strWhere, BPCondition), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, String.Format(Query_GetDataByTypesForCOAleakage, strWhere, BPCondition), sqlparams);

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
        
        

        public DataTable GetDataByTypesForCOAAgainstBroker(int companyID, int periodID, short typeCode, string strWhere = null, string brokerstrWhere = null, SqlConnection objConnection = null)
        {
            DataSet ds = null;
            DataTable dtToReturn = null;
            string query = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TypeCode", typeCode);

                if (strWhere == null)
                    strWhere = string.Empty;

                if (brokerstrWhere == null)
                    brokerstrWhere = string.Empty;



                //Old With BP Type
                //                string Query_GetDataByTypesForCOA = @" 
                //
                //SELECT DISTINCT (Case when c.CNICNumber is not null then c.CNICNumber else c.NTNNumber END) AS CNIC, convert(varchar(16), c.CustomerID) as CustomerCode, c.CustomerID, 
                //
                //    c.CustomerName, c.FatherName, c.AddressLine1, bp.FK_CodeCombinationID as CustomerAccountCCID ,bp.TypeCode, AR_SET_BusinessPartner.BusinessPartnerName
                //	, SET_City.CityName
                //	, HRMS_Set_Employee.EmployeeCode
                //FROM AR_SET_Customer c 
                //    INNER JOIN AR_SET_BusinessPartnerTypes bp ON bp.BusinessPartnerID = c.CustomerID --AND bp.CompanyID = c.CompanyID
                //AND bp.PeriodID = c.PeriodID 
                //	INNER JOIN AR_SET_BusinessPartner  ON AR_SET_BusinessPartner.BusinessPartnerID = bp.TypeCode
                //	Left JOin HRMS_Set_Employee on HRMS_Set_Employee.Fk_CustomerID = c.CustomerID
                //	AND HRMS_Set_Employee.CompanyID = c.CompanyID
                //	Inner Join SET_City on SET_City.CityID=c.CityID
                //WHERE c.PeriodID <= @PeriodID AND c.Status = 1 AND c.IsActive = 1   
                //";

                string Query_GetDataByTypesForCOA = @" 
SELECT DISTINCT isnull( (Case when c.CNICNumber is not null then c.CNICNumber else c.NTNNumber END),'---- --- ---') AS CNIC, convert(varchar(16), c.CustomerID) as CustomerCode, c.CustomerID, 
    c.CustomerName, c.FatherName, c.AddressLine1, '' as CustomerAccountCCID ,'' as TypeCode, '' as BusinessPartnerName, SET_City.CityName , c.EmployeeCode, '' as GrowerCode
FROM AR_SET_Customer c 

	Inner Join SET_City on SET_City.CityID=c.CityID
WHERE c.PeriodID <= @PeriodID AND c.Status = 1 AND c.IsActive = 1  
";

                if (typeCode > 0)
                    query = Query_GetDataByTypesForCOA + " and c.CustomerID in (select BusinessPartnerID from AR_SET_BusinessPartnerTypes where TypeCode = @TypeCode )";
                else
                    query = Query_GetDataByTypesForCOA;

                query = query + " {0} ";

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(query, strWhere + brokerstrWhere + " ORDER BY c.CustomerName "), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, String.Format(query, strWhere + brokerstrWhere + " ORDER BY c.CustomerName "), sqlparams);

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
        public DataTable GetDataByTypesForCOA(int companyID, int periodID, short typeCode1, short typeCode2, string strWhere = null, 
            SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TypeCode1", typeCode1);
                sqlparams[3] = new SqlParameter("@TypeCode2", typeCode2);

                if (strWhere == null)
                    strWhere = string.Empty;
                string Query_GetDataByTypesForCOATwoArgs = @" 
SELECT DISTINCT --isnull( (Case when c.CNICNumber is not null then c.CNICNumber else c.NTNNumber END),'---- --- ---') AS CNIC,
Case when isnull(c.CNICNumber,'') != '' then c.CNICNumber 
else 
(case When isnull(c.NTNNumber,'') != '' Then c.NTNNumber Else '----- ------- -' End) END AS CNIC,
convert(varchar(16), c.CustomerID) as CustomerCode, c.CustomerID, 
    c.CustomerName, c.FatherName, c.AddressLine1, c.CustomerAccountCCID ,'' as BusinessPartnerName, SET_City.CityName , c.EmployeeCode
	,ISNULL(Cane_Grower.GrowerCode,'-') as GrowerCode
FROM AR_SET_Customer c 
    INNER JOIN AR_SET_BusinessPartnerTypes bp ON bp.BusinessPartnerID = c.CustomerID --AND bp.CompanyID = c.CompanyID
AND bp.PeriodID = c.PeriodID 
	Inner Join SET_City on SET_City.CityID=c.CityID
	Left Join Cane_Grower On Cane_Grower.FK_CustomerID = c.CustomerID
WHERE c.PeriodID <= @PeriodID AND (bp.TypeCode = @TypeCode1 OR bp.TypeCode = @TypeCode2) AND c.Status = 1 AND c.IsActive = 1 

";
                Query_GetDataByTypesForCOATwoArgs = Query_GetDataByTypesForCOATwoArgs + " {0} ";

                //String.Format(query , strWhere + " ORDER BY bp.TypeCode , c.CustomerName ")
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(Query_GetDataByTypesForCOATwoArgs, strWhere + " ORDER BY c.CustomerID,  c.CustomerName "), sqlparams); //bp.TypeCode ,

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

        public DataTable GetDataByTypesForCOA(int companyID, int periodID, short typeCode1, short typeCode2, short typeCode3, string strWhere = null, 
            SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TypeCode1", typeCode1);
                sqlparams[3] = new SqlParameter("@TypeCode2", typeCode2);
                sqlparams[4] = new SqlParameter("@TypeCode3", typeCode3);

                if (strWhere == null)
                    strWhere = string.Empty;

                string Query_GetDataByTypesForCOAThreeArgs = @" 



--SELECT DISTINCT (Case when c.CNICNumber is not null then c.CNICNumber else c.NTNNumber END) AS CNIC, convert(varchar(16), c.CustomerID) as CustomerCode, c.CustomerID, 
--    c.CustomerName, c.FatherName, c.AddressLine1, c.CustomerAccountCCID , bp.TypeCode
--FROM AR_SET_Customer c 
--   INNER JOIN AR_SET_BusinessPartnerTypes bp ON bp.BusinessPartnerID = c.CustomerID --AND bp.CompanyID = c.CompanyID
--AND bp.PeriodID = c.PeriodID 
--WHERE c.PeriodID <= @PeriodID AND (bp.TypeCode = @TypeCode1 OR bp.TypeCode = @TypeCode2 OR bp.TypeCode = @TypeCode3) AND c.Status = 1 

--AND c.IsActive = 1 

SELECT DISTINCT --isnull( (Case when c.CNICNumber is not null then c.CNICNumber else c.NTNNumber END) ,'---- --- ---') AS CNIC,
Case when isnull(c.CNICNumber,'') != '' then c.CNICNumber 
else 
(case When isnull(c.NTNNumber,'') != '' Then c.NTNNumber Else '----- ------- -' End) END AS CNIC,
convert(varchar(16), c.CustomerID) as CustomerCode, c.CustomerID, 
    c.CustomerName, c.FatherName, c.AddressLine1, c.CustomerAccountCCID , bp.TypeCode , '' as BusinessPartnerName, SET_City.CityName , c.EmployeeCode
	,ISNULL(Cane_Grower.GrowerCode,'-') as GrowerCode
FROM AR_SET_Customer c 
    INNER JOIN AR_SET_BusinessPartnerTypes bp ON bp.BusinessPartnerID = c.CustomerID --AND bp.CompanyID = c.CompanyID
AND bp.PeriodID = c.PeriodID 
Inner Join SET_City on SET_City.CityID=c.CityID
Left Join Cane_Grower On Cane_Grower.FK_CustomerID = c.CustomerID
WHERE c.PeriodID <= @PeriodID AND (bp.TypeCode = @TypeCode1 OR bp.TypeCode = @TypeCode2 OR bp.TypeCode = @TypeCode3) AND c.Status = 1 

AND c.IsActive = 1 


{0}




";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(Query_GetDataByTypesForCOAThreeArgs , strWhere), sqlparams);

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

        public DataTable GetDataByTypesForCOA(int companyID, int periodID, short typeCode1, short typeCode2, short typeCode3, short typeCode4, string strWhere = null, 
            SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TypeCode1", typeCode1);
                sqlparams[3] = new SqlParameter("@TypeCode2", typeCode2);
                sqlparams[4] = new SqlParameter("@TypeCode3", typeCode3);
                sqlparams[5] = new SqlParameter("@TypeCode4", typeCode4);

                if (strWhere == null)
                    strWhere = string.Empty;

                string Query_GetDataByTypesForCOAFourArgs = @" 




SELECT DISTINCT --(Case when c.CNICNumber is not null then c.CNICNumber else c.NTNNumber END) AS CNIC,
Case when isnull(c.CNICNumber,'') != '' then c.CNICNumber 
else 
(case When isnull(c.NTNNumber,'') != '' Then c.NTNNumber Else '----- ------- -' End) END AS CNIC,
convert(varchar(16), c.CustomerID) as CustomerCode, c.CustomerID, 
    c.CustomerName, c.FatherName, c.AddressLine1, c.CustomerAccountCCID ,bp.typecode
	,ISNULL(Cane_Grower.GrowerCode,'-') as GrowerCode
FROM AR_SET_Customer c 
    INNER JOIN AR_SET_BusinessPartnerTypes bp ON bp.BusinessPartnerID = c.CustomerID --AND bp.CompanyID = c.CompanyID
	Left Join Cane_Grower On Cane_Grower.FK_CustomerID = c.CustomerID
AND bp.PeriodID = c.PeriodID 
WHERE c.PeriodID <= @PeriodID 
    AND (bp.TypeCode = @TypeCode1 OR bp.TypeCode = @TypeCode2 OR bp.TypeCode = @TypeCode3 OR bp.TypeCode = @TypeCode4) AND c.Status = 1 AND c.IsActive = 1 

{0}


";


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(Query_GetDataByTypesForCOAFourArgs, strWhere), sqlparams);

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

        public DataTable GetDataByTypesForRFQ(int companyID, int periodID, short typeCode, string strWhere = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TypeCode", typeCode);

                if (strWhere == null)
                    strWhere = string.Empty;

                string QueryGetSuppliersForRFQ = @" SELECT DISTINCT top 50 c.CustomerID, c.CustomerName FROM AR_SET_Customer c 
    INNER JOIN AR_SET_BusinessPartnerTypes bp ON bp.BusinessPartnerID = c.CustomerID --AND bp.CompanyID = c.CompanyID
AND bp.PeriodID = c.PeriodID 
WHERE c.PeriodID <= @PeriodID AND bp.TypeCode = @TypeCode AND c.Status = 1 AND c.IsActive = 1 ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, QueryGetSuppliersForRFQ + strWhere + " ORDER BY c.CustomerName", sqlparams);

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

        public DataTable GetDataByTypesForRFQSPL(int companyID, int periodID, short typeCode, string strWhere = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TypeCode", typeCode);

                if (strWhere == null)
                    strWhere = string.Empty;

                string QueryGetSuppliersForRFQSPL = @" 
SELECT DISTINCT c.CustomerID, c.CustomerName FROM AR_SET_Customer c 
    INNER JOIN AR_SET_BusinessPartnerTypes bp ON bp.BusinessPartnerID = c.CustomerID --AND bp.CompanyID = c.CompanyID
AND bp.PeriodID = c.PeriodID 
    Inner Join SPL_SupplierItems ON SPL_SupplierItems.SupplierID = c.CustomerID --and SPL_SupplierItems.CompanyID = c.CompanyID 
        and SPL_SupplierItems.PeriodID = c.PeriodID and SPL_SupplierItems.Status = 1 
WHERE c.PeriodID = @PeriodID AND bp.TypeCode = @TypeCode AND c.Status = 1 AND c.IsActive = 1 ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, QueryGetSuppliersForRFQSPL + strWhere + " ORDER BY c.CustomerName", sqlparams);

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

        public static DataTable GetAllActiveData2(Nullable<int> companyID, Nullable<int> periodID, Nullable<bool> isActive = true)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@IsActive", isActive);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetDataByTypes(int companyID, int periodID, short typeCode1, short typeCode2, string filter="")
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TypeCode1", typeCode1);
                sqlparams[3] = new SqlParameter("@TypeCode2", typeCode2);

                string Query_SelectByTwoTypes = @" 
SELECT DISTINCT (Case when c.CNICNumber is not null then c.CNICNumber else c.NTNNumber END) AS CNIC, 
    (Case when bp.FK_CodeCombinationID is Null then c.CustomerAccountCCID else bp.FK_CodeCombinationID end) as CustomerCCID,
SET_City.CityName,CountryName,c.* 
,bp.TypeCode,HRMS_Set_Employee.EmployeeCode
FROM AR_SET_Customer c 
    INNER JOIN AR_SET_BusinessPartnerTypes bp ON bp.BusinessPartnerID = c.CustomerID --AND bp.CompanyID = c.CompanyID
AND bp.PeriodID = c.PeriodID 
	Left Join HRMS_Set_Employee on HRMS_Set_Employee.FK_CustomerID = c.CustomerID
	AND HRMS_Set_Employee.CompanyID = c.CompanyID
	LEFT JOIN  SET_City ON SET_City.CityID=c.CityID
	LEFT JOIN SET_Country ON SET_Country.CountryID=SET_City.CountryID
WHERE c.PeriodID <= @PeriodID AND (bp.TypeCode = @TypeCode1 OR bp.TypeCode = @TypeCode2) AND c.Status = 1 AND c.IsActive = 1 
{0}
ORDER BY c.CustomerName ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectByTwoTypes,filter), sqlparams);

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

        public DataTable GetDataByTypesForLetterOfCredit(int companyID, int periodID, short typeCode1, short typeCode2, string filter = "")
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TypeCode1", typeCode1);
                sqlparams[3] = new SqlParameter("@TypeCode2", typeCode2);

                string Query_SelectByTwoTypes = @" 
SELECT DISTINCT (Case when c.CNICNumber is not null then c.CNICNumber else c.NTNNumber END) AS CNIC, 
    (Case when bp.FK_CodeCombinationID is Null then c.CustomerAccountCCID else bp.FK_CodeCombinationID end) as CustomerCCID,
SET_City.CityName,CountryName,c.* 
,bp.TypeCode,HRMS_Set_Employee.EmployeeCode
FROM AR_SET_Customer c 
    INNER JOIN AR_SET_BusinessPartnerTypes bp ON bp.BusinessPartnerID = c.CustomerID --AND bp.CompanyID = c.CompanyID
AND bp.PeriodID = c.PeriodID 
	Left Join HRMS_Set_Employee on HRMS_Set_Employee.FK_CustomerID = c.CustomerID
	AND HRMS_Set_Employee.CompanyID = c.CompanyID
	LEFT JOIN  SET_City ON SET_City.CityID=c.CityID
	LEFT JOIN SET_Country ON SET_Country.CountryID=SET_City.CountryID
WHERE c.PeriodID <= @PeriodID 
--AND (bp.TypeCode = @TypeCode1 OR bp.TypeCode = @TypeCode2)
AND c.Status = 1 AND c.IsActive = 1 
{0}
ORDER BY c.CustomerName ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectByTwoTypes, filter), sqlparams);

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


        public DataTable GetDataByTypes(int companyID, int periodID, short typeCode1, short typeCode2, short typeCode3)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TypeCode1", typeCode1);
                sqlparams[3] = new SqlParameter("@TypeCode2", typeCode2);
                sqlparams[4] = new SqlParameter("@TypeCode3", typeCode3);

                string Query_SelectByThreeTypes = @" 
SELECT DISTINCT (Case when c.CNICNumber is not null then c.CNICNumber else c.NTNNumber END) AS CNIC, 
    (Case when bp.FK_CodeCombinationID is Null then c.CustomerAccountCCID else bp.FK_CodeCombinationID end) as CustomerCCID, c.* 
, bp.typecode,SET_City.CityName,CountryName,HRMS_Set_Employee.EmployeeCode
FROM AR_SET_Customer c 
    INNER JOIN AR_SET_BusinessPartnerTypes bp ON bp.BusinessPartnerID = c.CustomerID --AND bp.CompanyID = c.CompanyID
AND bp.PeriodID = c.PeriodID 
Left Join HRMS_Set_Employee on HRMS_Set_Employee.FK_CustomerID = c.CustomerID
	AND HRMS_Set_Employee.CompanyID = c.CompanyID
	LEFT JOIN  SET_City ON SET_City.CityID=c.CityID
	LEFT JOIN SET_Country ON SET_Country.CountryID=SET_City.CountryID
WHERE c.PeriodID <= @PeriodID AND (bp.TypeCode = @TypeCode1 OR bp.TypeCode = @TypeCode2 OR bp.TypeCode = @TypeCode3) AND c.Status = 1 
    AND c.IsActive = 1 
ORDER BY c.CustomerName ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectByThreeTypes, sqlparams);

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

        public DataTable GeBusinessPartnerReportData(int companyID, int periodID, Nullable<int> CustomerCategoryWiseTaxID, Nullable<int> BrokerID, string Query_Partner_Report, string customerName, Nullable<int> customerType = null, 
            Nullable<int> customerStatus = null, string dateFrom = null, string dateTo = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CustomerCategoryWiseTaxID", CustomerCategoryWiseTaxID);
                sqlparams[3] = new SqlParameter("@BrokerID", BrokerID);
                sqlparams[4] = new SqlParameter("@CustomerName", customerName);

                if (customerType.HasValue)
                    sqlparams[5] = new SqlParameter("@CustomerType", customerType.Value);
                else
                    sqlparams[5] = new SqlParameter("@CustomerType", DBNull.Value);

                if (customerStatus.HasValue)
                    sqlparams[6] = new SqlParameter("@CustomerStatus", customerStatus.Value);
                else
                    sqlparams[6] = new SqlParameter("@CustomerStatus", DBNull.Value);
                
                if (dateFrom != null && dateTo != null)
                    sb.Append(" and AR_SET_Customer.CreateDate between '" + dateFrom + " 00:00:00'" + " and '" + dateTo + " 23:59:59' ");

                //if (BrokerID.HasValue && BrokerID > 0)
                //    sb.Append(" AND tbl_Broker.BrokerID = " + BrokerID);
                #region Query

              

                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Partner_Report,sb.ToString()), sqlparams);

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


        public DataTable GeBusinessPartnerPFReportData(int companyID, int periodID, string Query_Partner_Report, string customerName, Nullable<int> customerType = null,
     Nullable<int> customerStatus = null, Nullable<int> EmployeeCode = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                //sqlparams[2] = new SqlParameter("@CustomerCategoryWiseTaxID", CustomerCategoryWiseTaxID);
                //sqlparams[2] = new SqlParameter("@BrokerID", BrokerID);
                //sqlparams[2] = new SqlParameter("@EmployeeCode", EmployeeCode);

                if (EmployeeCode.HasValue)
                    sqlparams[2] = new SqlParameter("@EmployeeCode", EmployeeCode.Value);
                else
                    sqlparams[2] = new SqlParameter("@EmployeeCode", DBNull.Value);

                if (customerType.HasValue)
                    sqlparams[3] = new SqlParameter("@CustomerType", customerType.Value);
                else
                    sqlparams[3] = new SqlParameter("@CustomerType", DBNull.Value);

                if (customerStatus.HasValue)
                    sqlparams[4] = new SqlParameter("@CustomerStatus", customerStatus.Value);
                else
                    sqlparams[4] = new SqlParameter("@CustomerStatus", DBNull.Value);

                //if (dateFrom != null && dateTo != null)
                //    sb.Append(" and AR_SET_Customer.CreateDate between '" + dateFrom + " 00:00:00'" + " and '" + dateTo + " 23:59:59' ");

                //if (BrokerID.HasValue && BrokerID > 0)
                //    sb.Append(" AND tbl_Broker.BrokerID = " + BrokerID);
                #region Query



                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Partner_Report, sb.ToString()), sqlparams);

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


        public DataTable BusinessPartnerSpecificationReport(int companyID, int periodID, string customerName, Nullable<int> customerType = null,
            Nullable<int> customerStatus = null, string dateFrom = null, string dateTo = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CustomerName", customerName);

                if (customerType.HasValue)
                    sqlparams[3] = new SqlParameter("@CustomerType", customerType.Value);
                else
                    sqlparams[3] = new SqlParameter("@CustomerType", DBNull.Value);

                if (customerStatus.HasValue)
                    sqlparams[4] = new SqlParameter("@CustomerStatus", customerStatus.Value);
                else
                    sqlparams[4] = new SqlParameter("@CustomerStatus", DBNull.Value);

                if (dateFrom != null && dateTo != null)
                    sb.Append(" and AR_SET_Customer.CreateDate between '" + dateFrom + " 00:00:00'" + " and '" + dateTo + " 23:59:59' ");

                #region Query

                string Query_PartnerS_Report = @" SELECT Distinct AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName, AR_SET_BusinessPartner.BusinessPartnerName,
  GL_CodeCombinationDetail.CodeCombinationID,GL_CodeCombinationDetail.Description
FROM AR_SET_Customer 
	LEFT OUTER JOIN AR_SET_BusinessPartnerTypes 
	ON AR_SET_Customer.CompanyID = AR_SET_BusinessPartnerTypes.CompanyID 
    AND AR_SET_Customer.PeriodID = AR_SET_BusinessPartnerTypes.PeriodID 
    AND AR_SET_Customer.CustomerID = AR_SET_BusinessPartnerTypes.BusinessPartnerID 
    	
	left outer join AR_SET_BusinessPartner 
	on AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
    
    LEFT OUTER  join GL_CodeCombinationDetail 
    on GL_CodeCombinationDetail.CodeCombinationID = AR_SET_Customer.CustomerAccountCCID 
    --AND GL_CodeCombinationDetail.CompanyID = AR_SET_Customer.CompanyID 

    WHERE AR_SET_Customer.PeriodID = @PeriodID 
    And AR_SET_Customer.Status = 1 
    And AR_SET_Customer.CustomerName like '%' + @CustomerName + '%' And (AR_SET_Customer.IsActive = @CustomerStatus or @CustomerStatus is null) 
    And (AR_SET_BusinessPartnerTypes.TypeCode = @CustomerType or @CustomerType is null) 
     {0} 
   ORDER BY AR_SET_Customer.CustomerName ";

                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_PartnerS_Report, sb.ToString()), sqlparams);

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

        public bool Delete(int businessPartnerID, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                using (AR_SET_BusinessPartnerType obj_BPType = new AR_SET_BusinessPartnerType())
                    obj_BPType.Delete(this.f_CompanyID, this.f_PeriodID, this.f_CustomerID, objTransaction);
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public DataTable GetAllDataForBusniessPartner(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
           Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, SqlTransaction objTransaction = null)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

            try
            {
                if (PageIndex != null)
                {
                    sqlparamsList.Add(new SqlParameter("@PageIndex", PageIndex));
                    sqlparamsList.Add(new SqlParameter("@PageSize", PageSize));
                    sqlparamsList.Add(new SqlParameter("@TotalRows", ReturnTotalRows));

                    SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                    sqlparamTotalRows.Direction = ParameterDirection.Output;

                    SqlParameter[] sqlparams = sqlparamsList.ToArray();

                    #region Query

                    string Query_SelectAll_WithPagingForBusinesPartner = @" 
select *,ROW_NUMBER() Over (Order by CompanyID, CustomerID, PeriodID) as 'Row'  from (
SELECT distinct AR_SET_Customer.CompanyID,CustomerCategory, AR_SET_Customer.PeriodID, CustomerID, CustomerName, AR_SET_Customer.AddressLine1,CityName, AR_SET_Customer.CityID, ZipCode, PhoneNumber, ExtensionNumber, ContactNumber, FaxNumber, EmailAddress, AR_SET_Customer.CNICNumber, 
    NTNNumber, STRNNumber, ChartofAccountID, CustomerAccountCCID, ContactPerson, Collector, CollectorContactNumber, CreditAnalyst, CreditChecking, 
    NextCreditReviewDate, Tolerance, DiscountTerms, DunningLetters, DunningLetterSetID, InterestCharges, PrefFunctionalCurrencyID, SendStatements, 
    CreditBalanceStatements, CreditHold, ProfileClassID, CreditRating, RiskID, StandardTermID, OverrideTerms, InterestPeriodDays, PaymentGraceDays, DiscountGraceDays, 
    StatementCycleID, PercentCollectable, AutoRecInclDisputedFlag, TaxPrintingOptionID, ChargeOnFinanceChargeFlag, GroupingRuleID, ClearingDays, ConsInvFlag, 
    ConsInvTypeID, AR_SET_Customer.Status, AR_SET_Customer.CreatedBy, AR_SET_Customer.CreateDate, AR_SET_Customer.UpdatedBy, AR_SET_Customer.UpdateDate, AR_SET_Customer.CreatedByValue, AR_SET_Customer.UpdatedByValue, AR_SET_Customer.IsActive, AR_SET_Customer.FatherName, ServiceItemTaxExempted, 
    ServiceItemTaxRate, ServiceItemTaxCOA, SuppliesTaxExempted, SuppliesTaxRate, SuppliesTaxCOA, ExemptedAmount, ContactPersonName, ContactPersonAddress, 
    TransporterTaxExempted, TransporterTaxRate, TransporterExceedLimit, TransporterTaxCOA, AdditionalDeductionTaxExempted, AdditionalDeductionTaxRate, 
    AdditionalDeductionCOA, FK_BankID, AR_SET_Customer.FK_BankBranchID, AccountNumber, AR_SET_Customer.IsStopPayment, Districtid, AR_SET_Customer.Provinceid, PaymentType, FixedDiscount, AR_SET_Customer.FK_BankAccountID, Remarks, 
    case when CustomerID <= 5000000  then 'Site' when CustomerID > 5000000 then 'HO' else null end as CustomerSatus,EmployeeCode
    --,ROW_NUMBER() Over (Order by AR_SET_Customer.CompanyID, CustomerID, AR_SET_Customer.PeriodID) as 'Row' 
FROM AR_SET_Customer 

--Left Join HRMS_Set_Employee on HRMS_Set_Employee.FK_CustomerID = AR_SET_Customer.CustomerID
--	AND HRMS_Set_Employee.CompanyID = AR_SET_Customer.CompanyID
    --and HRMS_Set_Employee.status = 1
left join AR_SET_BusinessPartnerTypes on AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer .CustomerID
left join AR_SET_BusinessPartner on AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode
left join SET_City on AR_SET_Customer.CityID = SET_City.CityID
left join Set_CustomerCategoryWiseTax on Set_CustomerCategoryWiseTax.CustomerCategoryWiseTaxID = AR_SET_Customer.CustomerCategoryID
     and Set_CustomerCategoryWiseTax.CompanyID = AR_SET_Customer.CompanyID
	 and Set_CustomerCategoryWiseTax.PeriodID = AR_SET_Customer.PeriodID
	 and Set_CustomerCategoryWiseTax.Status = 1
)as tab
";

                    #endregion

                    if (objTransaction != null)
                    {
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_SelectAll_WithPagingForBusinesPartner + strWhere
                            + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPagingForBusinesPartner + strWhere + (!string.IsNullOrEmpty(OrderBy)
                            ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    }
                    else
                    {
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_WithPagingForBusinesPartner + strWhere
                            + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPagingForBusinesPartner + strWhere + (!string.IsNullOrEmpty(OrderBy)
                            ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    }

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());

                    //if (dt != null && dt.Rows.Count > 0)
                    //    ReturnTotalRows = dt.Rows.Count;

                }
                else
                {
                    #region Query

                    string Query_SelectAll_WithoutPagingForBusinesPartner = @" 
SELECT AR_SET_Customer.CompanyID, AR_SET_Customer.PeriodID,CustomerCategory, CustomerID, CustomerName, AddressLine1,CityName, AR_SET_Customer.CityID, ZipCode, PhoneNumber, ExtensionNumber, ContactNumber, FaxNumber, EmailAddress, CNICNumber, 
    NTNNumber, STRNNumber, ChartofAccountID, CustomerAccountCCID, ContactPerson, Collector, CollectorContactNumber, CreditAnalyst, CreditChecking, 
    NextCreditReviewDate, Tolerance, DiscountTerms, DunningLetters, DunningLetterSetID, InterestCharges, PrefFunctionalCurrencyID, SendStatements, 
    CreditBalanceStatements, CreditHold, ProfileClassID, CreditRating, RiskID, StandardTermID, OverrideTerms, InterestPeriodDays, PaymentGraceDays, DiscountGraceDays, 
    StatementCycleID, PercentCollectable, AutoRecInclDisputedFlag, TaxPrintingOptionID, ChargeOnFinanceChargeFlag, GroupingRuleID, ClearingDays, ConsInvFlag, 
    ConsInvTypeID, AR_SET_Customer.Status, AR_SET_Customer.CreatedBy, AR_SET_Customer.CreateDate, AR_SET_Customer.UpdatedBy, AR_SET_Customer.UpdateDate, AR_SET_Customer.CreatedByValue, AR_SET_Customer.UpdatedByValue, AR_SET_Customer.IsActive, FatherName, ServiceItemTaxExempted, 
    ServiceItemTaxRate, ServiceItemTaxCOA, SuppliesTaxExempted, SuppliesTaxRate, SuppliesTaxCOA, ExemptedAmount, ContactPersonName, ContactPersonAddress, 
    TransporterTaxExempted, TransporterTaxRate, TransporterExceedLimit, TransporterTaxCOA, AdditionalDeductionTaxExempted, AdditionalDeductionTaxRate, 
    AdditionalDeductionCOA, FK_BankID, FK_BankBranchID, AccountNumber, IsStopPayment, Districtid, Provinceid, PaymentType, FixedDiscount, FK_BankAccountID, Remarks, 
    case when CustomerID <= 5000000 then 'Site' when CustomerID > 5000000 then 'HO' else null end as CustomerSatus 
FROM AR_SET_Customer


inner join AR_SET_BusinessPartnerTypes on AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer .CustomerID
inner join AR_SET_BusinessPartner on AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode
inner join SET_City on AR_SET_Customer.CityID = SET_City.CityID
left join Set_CustomerCategoryWiseTax on Set_CustomerCategoryWiseTax.CustomerCategoryWiseTaxID = AR_SET_Customer.CustomerCategoryID
     and Set_CustomerCategoryWiseTax.CompanyID = AR_SET_Customer.CompanyID
	 and Set_CustomerCategoryWiseTax.PeriodID = AR_SET_Customer.PeriodID
	 and Set_CustomerCategoryWiseTax.Status = 1
";

                    #endregion

                    if (objTransaction != null)
                    {
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAll_WithoutPagingForBusinesPartner + strWhere
                            + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    }
                    else
                    {
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_WithoutPagingForBusinesPartner + strWhere + (!string.IsNullOrEmpty(OrderBy)
                            ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    }
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetCustomerNameByName(int CompanyID, string CustomerName)
        {
            DataTable dt = null;
            string Fillter = string.Empty;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);

                if (CustomerName.StartsWith("#"))
                {
                    CustomerName = CustomerName.Replace("#", "");
                }
                else
                {

                    var stringNumber = CustomerName;
                    int numericValue;
                    bool isNumber = int.TryParse(stringNumber, out numericValue);

                    if (isNumber)
                        Fillter = " where CustomerID like '" + CustomerName + "%'";
                    else
                        Fillter = " where CustomerName like '%" + CustomerName + "%'";


                }

                string Query_GetCustomerNameByName = @" 
select * from (
select CustomerName + ' * ' +set_city.CityName + '*'+ case isnull(AR_SET_Customer.IsRegistered,0) when 1 then 'Registered' else 'Un-Registered' end + '*'+ convert(varchar,CustomerID) As CustomerName,CustomerID

from AR_SET_Customer
inner join set_city on set_city.cityID = AR_SET_Customer.cityID
where AR_SET_Customer.Status = 1 
AND AR_SET_Customer.IsActive = 1
) tab
";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetCustomerNameByName + Fillter, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataForAssignment()
        {
            DataTable dt = null;

            try
            {

                const string Query_SelectAllDataForAssignment = @"SELECT CustomerID, CustomerName, CONVERT(BIT,'False')AS Checked FROM AR_SET_Customer
ORDER BY CustomerID
";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllDataForAssignment).Tables[0];
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
