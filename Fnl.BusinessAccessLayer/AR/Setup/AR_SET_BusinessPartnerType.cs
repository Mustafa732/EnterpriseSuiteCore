using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.AR.Setup
{
    public partial class AR_SET_BusinessPartnerType : BaseClass
    {
        #region Constant Strings

        const string Query_InsertSETBusinessPartnerTypePF = @" INSERT INTO {0}.dbo.[AR_SET_BusinessPartnerTypes] ([CompanyID], [PeriodID], [BusinessPartnerID], [TypeCode], [FK_ChartofAccountID], [FK_CodeCombinationID], [WHTaxID], [WHTaxExemptionID], [WHSTaxID], [WHSTaxExemptionID])
		VALUES(@CompanyID, @PeriodID, @BusinessPartnerID, @TypeCode, @FK_ChartofAccountID, @FK_CodeCombinationID, @WHTaxID, @WHTaxExemptionID, @WHSTaxID, @WHSTaxExemptionID) ";

        #endregion


        #region Class Constructors

        public AR_SET_BusinessPartnerType()
        {

        }
        public AR_SET_BusinessPartnerType(int companyID, int periodID, int businessPartnerID, short typeCode, int chartofAccountID, string codeCombinationID)
        {
            this.f_CompanyID = companyID;
            this.f_PeriodID = periodID;
            this.f_BusinessPartnerID = businessPartnerID;
            this.f_TypeCode = typeCode;
            this.f_FK_ChartofAccountID = chartofAccountID;
            this.f_FK_CodeCombinationID = codeCombinationID;
        }

        public AR_SET_BusinessPartnerType(int companyID, int periodID, int businessPartnerID, short typeCode, int chartofAccountID, string codeCombinationID, Nullable<int> WHTaxID, Nullable<int> WHTaxExemptionID, Nullable<int> WHSTaxID, Nullable<int> WHSTaxExemptionID)
        {
            this.f_CompanyID = companyID;
            this.f_PeriodID = periodID;
            this.f_BusinessPartnerID = businessPartnerID;
            this.f_TypeCode = typeCode;
            this.f_FK_ChartofAccountID = chartofAccountID;
            this.f_FK_CodeCombinationID = codeCombinationID;
            this.f_WHTaxID = WHTaxID;
            this.f_WHSTaxID = WHSTaxID;
            this.f_WHTaxExemptionID = WHTaxExemptionID;
            this.f_WHSTaxExemptionID = WHSTaxExemptionID;
        }

        public AR_SET_BusinessPartnerType(int companyID, int periodID, int businessPartnerID, short typeCode)
        {
            this.f_CompanyID = companyID;
            this.f_PeriodID = periodID;
            this.f_BusinessPartnerID = businessPartnerID;
            this.f_TypeCode = typeCode;
        }

        #endregion

        #region Class Methods

        public DataTable GetBPTActiveData(int companyID, int periodID, int customerID, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@BusinessPartnerID", customerID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                string Query_SelectAll_ByCustomerID = @" 
SELECT AR_SET_BusinessPartnerTypes.BusinessPartnerID, TypeCode BusinessPartnerTypeCode, BusinessPartnerName BusinessPartnerTypeValue, 
    FK_ChartofAccountID ChartofAccountID, FK_CodeCombinationID CodeCombinationID, FK_CodeCombinationID AccountCode, BusinessPartnerName, 
    cast(BusinessPartnerName as varchar(25)) + ' (' + Cast(Isnull(FK_CodeCombinationID, 'No Account Code') as varchar(50)) + ')' BusinessPartnerCode, '' Action 
	,AR_SET_BusinessPartnerTypes.WHSTaxID,AR_SET_BusinessPartnerTypes.WHTaxExemptionID, AR_SET_BusinessPartnerTypes.WHTaxID,AR_SET_BusinessPartnerTypes.WHSTaxExemptionID
	,Set_WHSTaxStatementExemption_Par.WHSTaxExemptionCode,Set_WHTaxStatementExemption_Par.WHTaxExemptionCode
	,Set_WHTaxStatement_Par.WHTaxCode,Set_WH_SalTaxStatement_Par.WHSTaxCode
FROM AR_SET_BusinessPartnerTypes 
inner join AR_SET_Customer on AR_SET_Customer.CustomerID = AR_SET_BusinessPartnerTypes.BusinessPartnerID
--and AR_SET_Customer.CompanyID = AR_SET_BusinessPartnerTypes.CompanyID 
    LEFT JOIN AR_SET_BusinessPartner on AR_SET_BusinessPartnerTypes.TypeCode = AR_SET_BusinessPartner.BusinessPartnerID 
	Left Join Set_WHSTaxStatementExemption_Par on Set_WHSTaxStatementExemption_Par.WHSTaxExemptionID= AR_SET_BusinessPartnerTypes.WHSTaxExemptionID
		Left Join Set_WHTaxStatementExemption_Par on Set_WHTaxStatementExemption_Par.WHTaxExemptionID= AR_SET_BusinessPartnerTypes.WHTaxExemptionID
			Left Join Set_WHTaxStatement_Par on Set_WHTaxStatement_Par.WHTaxID= AR_SET_BusinessPartnerTypes.WHTaxID
				Left Join Set_WH_SalTaxStatement_Par on Set_WH_SalTaxStatement_Par.WHSTaxID= AR_SET_BusinessPartnerTypes.WHSTaxID
WHERE AR_SET_BusinessPartnerTypes.PeriodID <= @PeriodID AND AR_SET_BusinessPartnerTypes.BusinessPartnerID = @BusinessPartnerID ";

                if (objConnection != null)
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAll_ByCustomerID, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_ByCustomerID, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public bool InsertSETBusinessPartnerTypePF(string DatabaseName, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                StringBuilder sb = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[10];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[2] = new SqlParameter("@BusinessPartnerID", f_BusinessPartnerID);
                sqlparams[3] = new SqlParameter("@TypeCode", f_TypeCode);

                if (f_FK_ChartofAccountID.HasValue)
                    sqlparams[4] = new SqlParameter("@FK_ChartofAccountID", f_FK_ChartofAccountID.Value);
                else
                    sqlparams[4] = new SqlParameter("@FK_ChartofAccountID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_FK_CodeCombinationID))
                    sqlparams[5] = new SqlParameter("@FK_CodeCombinationID", f_FK_CodeCombinationID);
                else
                    sqlparams[5] = new SqlParameter("@FK_CodeCombinationID", DBNull.Value);

                if (f_WHTaxID.HasValue)
                    sqlparams[6] = new SqlParameter("@WHTaxID", f_WHTaxID.Value);
                else
                    sqlparams[6] = new SqlParameter("@WHTaxID", DBNull.Value);

                if (f_WHTaxExemptionID.HasValue)
                    sqlparams[7] = new SqlParameter("@WHTaxExemptionID", f_WHTaxExemptionID.Value);
                else
                    sqlparams[7] = new SqlParameter("@WHTaxExemptionID", DBNull.Value);

                if (f_WHSTaxID.HasValue)
                    sqlparams[8] = new SqlParameter("@WHSTaxID", f_WHSTaxID.Value);
                else
                    sqlparams[8] = new SqlParameter("@WHSTaxID", DBNull.Value);

                if (f_WHSTaxExemptionID.HasValue)
                    sqlparams[9] = new SqlParameter("@WHSTaxExemptionID", f_WHSTaxExemptionID.Value);
                else
                    sqlparams[9] = new SqlParameter("@WHSTaxExemptionID", DBNull.Value);

                sb.Append(DatabaseName);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, string.Format(Query_InsertSETBusinessPartnerTypePF, sb.ToString()), sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, string.Format(Query_InsertSETBusinessPartnerTypePF, sb.ToString()), sqlparams) > 0)
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


        internal bool Delete(int companyID, int periodID, int customerID, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@BusinessPartnerID", customerID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                string Query_BusinessPertnerTypeDelete_ByKey = @" DELETE FROM AR_SET_BusinessPartnerTypes 
WHERE CompanyID = @CompanyID AND PeriodID = @PeriodID AND BusinessPartnerID = @BusinessPartnerID ";

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_BusinessPertnerTypeDelete_ByKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_BusinessPertnerTypeDelete_ByKey, sqlparams) > 0)
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

        internal bool DeleteByTypeCode(int companyID, int periodID, int customerID, short typecode, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@BusinessPartnerID", customerID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@TypeCode", typecode);

                string Query_BusinessPertnerTypeDelete_ByKeyAndTypeCode = @" DELETE FROM AR_SET_BusinessPartnerTypes 
WHERE CompanyID = @CompanyID AND PeriodID = @PeriodID AND BusinessPartnerID = @BusinessPartnerID AND TypeCode = @TypeCode ";

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_BusinessPertnerTypeDelete_ByKeyAndTypeCode, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_BusinessPertnerTypeDelete_ByKeyAndTypeCode, sqlparams) > 0)
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

        public static string GetSingleCCIDFromBusinessPartnerTypes(int companyID, int periodID, int customerID, short typeCode, SqlConnection obj_SQLConnection = null)
        {
            string accountCode = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CustomerID", customerID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@TypeCode", typeCode);

                string Query_GetSingleCCIDFromBusinessPartnerTypes = @" select ISNULL(FK_CodeCombinationID, '') as FK_CodeCombinationID 
from AR_SET_BusinessPartnerTypes where BusinessPartnerID = @CustomerID and PeriodID <= @PeriodID and TypeCode = @TypeCode ";

                if (obj_SQLConnection != null)
                {
                    accountCode = Convert.ToString(SqlHelper.ExecuteScalar(obj_SQLConnection, CommandType.Text, Query_GetSingleCCIDFromBusinessPartnerTypes,
                        sqlparams));
                }
                else
                    accountCode = Convert.ToString(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetSingleCCIDFromBusinessPartnerTypes, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return accountCode;
        }

        public static string GetSingleCCIDFromBusinessPartnerTypesByName(int companyID, int periodID, int customerID, string doctype, SqlConnection obj_SQLConnection = null)
        {
            string accountCode = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CustomerID", customerID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@TypeCode", doctype);

                string Query = @"select ISNULL(FK_CodeCombinationID, '') as FK_CodeCombinationID 
from AR_SET_BusinessPartnerTypes
LEFT JOIN AR_SET_BusinessPartner on AR_SET_BusinessPartnerTypes.TypeCode = AR_SET_BusinessPartner.BusinessPartnerID 
where AR_SET_BusinessPartnerTypes.BusinessPartnerID = @CustomerID and PeriodID <= @PeriodID and BusinessPartnerName like '%advance from%' and BusinessPartnerName LIKE '%' + @TypeCode + '%' ";

                if (obj_SQLConnection != null)
                {
                    accountCode = Convert.ToString(SqlHelper.ExecuteScalar(obj_SQLConnection, CommandType.Text, Query,
                        sqlparams));
                }
                else
                    accountCode = Convert.ToString(SqlHelper.ExecuteScalar(CommandType.Text, Query, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return accountCode;
        }

        public static string GetSingleCCIDFromBusinessPartnerTypesByNameForGrower(int companyID, int periodID, int customerID, string type, SqlConnection obj_SQLConnection = null)
        {
            string accountCode = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CustomerID", customerID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@TypeCode", type);

                string Query = @"select ISNULL(FK_CodeCombinationID, '') as FK_CodeCombinationID 
from AR_SET_BusinessPartnerTypes
LEFT JOIN AR_SET_BusinessPartner on AR_SET_BusinessPartnerTypes.TypeCode = AR_SET_BusinessPartner.BusinessPartnerID 
where AR_SET_BusinessPartnerTypes.BusinessPartnerID = @CustomerID and PeriodID <= @PeriodID  and BusinessPartnerName = @TypeCode  ";

                if (obj_SQLConnection != null)
                {
                    accountCode = Convert.ToString(SqlHelper.ExecuteScalar(obj_SQLConnection, CommandType.Text, Query,
                        sqlparams));
                }
                else
                    accountCode = Convert.ToString(SqlHelper.ExecuteScalar(CommandType.Text, Query, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return accountCode;
        }

        public static int GetBusinessPartnerIDByNameForGrower(int companyID, int periodID, int customerID, string type, SqlConnection obj_SQLConnection = null)
        {
            int businesspartnertypeid;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CustomerID", customerID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@TypeCode", type);

                string Query = @"select AR_SET_BusinessPartner.BusinessPartnerID as BusinessPartnerID 
from AR_SET_BusinessPartnerTypes
LEFT JOIN AR_SET_BusinessPartner on AR_SET_BusinessPartnerTypes.TypeCode = AR_SET_BusinessPartner.BusinessPartnerID 
where AR_SET_BusinessPartnerTypes.BusinessPartnerID = @CustomerID and PeriodID <= @PeriodID  and BusinessPartnerName = @TypeCode  ";

                if (obj_SQLConnection != null)
                {
                    businesspartnertypeid = Convert.ToInt32(SqlHelper.ExecuteScalar(obj_SQLConnection, CommandType.Text, Query,
                        sqlparams));
                }
                else
                    businesspartnertypeid = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return businesspartnertypeid;
        }

        public DataTable BindBusinessPartnerCodes(int companyID, int periodID, int customerID, short enumType1, short? enumType2, short? enumType3, short? enumType4,
            short? enumType5)
        {
            DataTable dt = null;
            StringBuilder filter = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CustomerID", customerID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                //filter.Append(" AND AR_SET_BusinessPartnerTypes.TypeCode in (" + enumType1 + ", " + enumType2 + ", " + enumType3 + ", " + enumType4 + ", " + enumType5 
                //    + ") ");

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Setup.BusinessPartnerType.Query_GetBusinessPartnerCodes,
                    filter), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetCreditBusinessPartnerDataPF(int companyID, int periodID, int customerID, int type, string DatabaseName)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CustomerID", customerID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@TypeCode", type);

                string Query = @"select * from {0}.dbo.AR_SET_BusinessPartnerTypes
where BusinessPartnerID = @CustomerID and PeriodID = @PeriodID and CompanyID = @CompanyID and TypeCode = @TypeCode  ";

                sb.Append(DatabaseName);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, sb.ToString()), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetDebitBusinessPartnerDataPF(int companyID, int periodID, string DatabaseName)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                //sqlparams[0] = new SqlParameter("@CustomerID", customerID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                //sqlparams[3] = new SqlParameter("@TypeCode", type);

                string Query = @"
       select * from {0}.dbo.GL_CodeCombinationDetail where CodeCombinationID = '02-02-01-001-001' and CompanyID = @CompanyID and status = 1 
 ";

                sb.Append(DatabaseName);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, sb.ToString()), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable BindBusinessPartnerCodesAdvance(int companyID, int periodID, int customerID, short enumType1, short? enumType2, short? enumType3, short? enumType4,
        short? enumType5)
        {
            DataTable dt = null;
            StringBuilder filter = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CustomerID", customerID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                //filter.Append(" AND AR_SET_BusinessPartnerTypes.TypeCode in (" + enumType1 + ", " + enumType2 + ", " + enumType3 + ", " + enumType4 + ", " + enumType5 
                //    + ") ");

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Setup.BusinessPartnerType.Query_GetBusinessPartnerCodesAdvance,
                    filter), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public DataTable BindBusinessPartnerCodes(int companyID, int periodID, int customerID, short enumType1, short? enumType2, short? enumType3, short? enumType4,
            short? enumType5, bool BindAllTypes)
        {
            DataTable dt = null;
            StringBuilder filter = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CustomerID", customerID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                if (!BindAllTypes)
                {
                    filter.Append(" AND AR_SET_BusinessPartnerTypes.TypeCode in (" + enumType1 + ", " + enumType2 + ", " + enumType3 + ", " + enumType4 + ", "
                        + enumType5 + ") ");
                }
                else
                    filter.Append(" and AR_SET_BusinessPartnerTypes.FK_CodeCombinationID is not null ");

                //string path = 



                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Setup.BusinessPartnerType.Query_GetBusinessPartnerCodes,
                    filter), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static bool InsertBBInfoByBBReferenceID(int companyID, int periodID, int customerID, string refEmpCode, SqlTransaction objTransaction = null)
        {
            bool result = false;
            StringBuilder sb = new StringBuilder();

            try
            {
                const string query = @"
INSERT INTO AR_SET_BusinessPartnerTypes 
SELECT CompanyID, PeriodID, @CustomerID, TypeCode, FK_ChartofAccountID, FK_CodeCombinationID, null, null, null, null
FROM AR_SET_BusinessPartnerTypes WHERE CompanyID = @CompanyID AND PeriodID = @PeriodID 
{0}
AND NOT EXISTS (SELECT * FROM  AR_SET_BusinessPartnerTypes a WHERE a.CompanyID = @CompanyID AND a.PeriodID = @PeriodID AND a.BusinessPartnerID = @CustomerID
AND a.TypeCode = AR_SET_BusinessPartnerTypes.TypeCode)
";

                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CustomerID", customerID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                sb.Append("AND BusinessPartnerID = (SELECT TOP 1 FK_CustomerID FROM HRMS_Set_Employee WHERE ('0' + EmployeeCode) LIKE '" + refEmpCode + "%')");

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, string.Format(query, sb.ToString()), sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, string.Format(query, sb.ToString()), sqlparams) > 0)
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

        #endregion
    }
}