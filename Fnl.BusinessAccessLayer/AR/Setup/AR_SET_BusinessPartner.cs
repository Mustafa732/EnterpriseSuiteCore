using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;
using System.Text;

namespace FnI.BusinessAccessLayer.AR.Setup
{
    public partial class AR_SET_BusinessPartner : BaseClass
    {

        #region Constant Queries
        const string Query_GetAR_SET_BusinessPartner = @" select * from AR_SET_BusinessPartner {0}";


        #endregion 

        #region Methods
        public static DataTable GetAllAR_SET_BusinessPartner(string strWhere, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {

                if (objConnection != null)
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetAR_SET_BusinessPartner,strWhere)).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAR_SET_BusinessPartner,strWhere)).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetSalesman(SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                string strWhere = "where BusinessPartnerName like '%salesman%'";

                if (objConnection != null)
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetAR_SET_BusinessPartner, strWhere)).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAR_SET_BusinessPartner, strWhere)).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetRetailer(SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                string strWhere = "where BusinessPartnerName like '%retailer%'";

                if (objConnection != null)
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetAR_SET_BusinessPartner, strWhere)).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAR_SET_BusinessPartner, strWhere)).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllActiveDataForEmployee(SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            #region 
            const string Query_SelectAllForEmplyee = @"
SELECT * FROM [AR_SET_BusinessPartner] where BusinessPartnerName = 'Employee'
               ";
            #endregion

            try
            {
                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAllForEmplyee);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAllForEmplyee);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllForEmplyee);

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

        public static DataTable GetDataByBPID(int businessPartnerID, string DatabaseName)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@BusinessPartnerID", businessPartnerID);

                string Query = @"select * from {0}.dbo.AR_SET_BusinessPartner where BusinessPartnerID = @BusinessPartnerID; ";

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


        public static DataRow GetBusinessPartnerTypeByID(int BusinessPartnerTypeID, SqlTransaction sqltrans = null)
        {
            DataRow dr = null;
            try
            {
                DataSet ds;
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@BusinessPartnerTypeID", BusinessPartnerTypeID);

                string Query_GetBusinessPartnerTypeIDbyCode = @"
select * from AR_SET_BusinessPartner where BusinessPartnerID = @BusinessPartnerTypeID
";

                if (sqltrans == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetBusinessPartnerTypeIDbyCode, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query_GetBusinessPartnerTypeIDbyCode, sqlparams);


                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    dr = ds.Tables[0].Rows[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }

        public static DataRow GetBusinessPartnerTypeByIDFor_PF(int BusinessPartnerTypeID, SqlTransaction sqltrans = null)
        {
            DataRow dr = null;
            try
            {
                DataSet ds;
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@BusinessPartnerTypeID", BusinessPartnerTypeID);

                string Query_GetBusinessPartnerTypeIDbyCode = @"
Select Top 1 AR_SET_BusinessPartnerTypes.FK_CodeCombinationID as GLCode,GL_CodeCombinationDetail.Description From AR_SET_BusinessPartner
Inner join AR_SET_BusinessPartnerTypes on AR_SET_BusinessPartnerTypes.TypeCOde = AR_SET_BusinessPartner.BusinessPartnerID
Inner join GL_CodeCombinationDetail on GL_CodeCombinationDetail.CodeCombinationID = AR_SET_BusinessPartnerTypes.FK_CodeCombinationID
Where AR_SET_BusinessPartner.BusinessPartnerID = @BusinessPartnerTypeID
";

                if (sqltrans == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetBusinessPartnerTypeIDbyCode, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query_GetBusinessPartnerTypeIDbyCode, sqlparams);


                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    dr = ds.Tables[0].Rows[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }


        public static DataRow CheckVendorStatus(int BusinessPartnerID, SqlTransaction sqltrans = null)
        {
            DataRow dr = null;
            try
            {
                DataSet ds;
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@BusinessPartnerID", BusinessPartnerID);

                string Query_GetBusinessPartnerTypeIDbyCode = @"
SELECT * FROM AR_SET_BusinessPartnerTypes WHERE TypeCode in (1,10,11) AND BusinessPartnerID=@BusinessPartnerID
";

                if (sqltrans == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetBusinessPartnerTypeIDbyCode, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query_GetBusinessPartnerTypeIDbyCode, sqlparams);


                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    dr = ds.Tables[0].Rows[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }

        #endregion
    }
}
