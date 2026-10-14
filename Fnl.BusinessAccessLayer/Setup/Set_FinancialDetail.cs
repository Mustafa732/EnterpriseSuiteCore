using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class Set_FinancialDetail
    {
        public short HeaderSequence
        {
            get;
            set;
        }

        #region Constant Queries

        const string Query_Delete_ByKey = @" Delete From SET_FinancialDetail where FinancialHeaderID = @FinancialHeaderID and FinancialDetailID = @FinancialDetailID ";

        const string Query_Select_FinancialDetail = @" SELECT * FROM Set_FinancialDetail 
    INNER JOIN Set_FinancialHeader On Set_FinancialHeader.FinancialID = Set_FinancialDetail.FinancialHeaderID
	and Set_FinancialHeader.CompanyID = Set_FinancialDetail.CompanyID
	 WHERE 
--Set_FinancialHeader.companyid in @CompanyID and 
FinancialName In ({0}) 
ORDER BY ElementSequence, LineSequence  ";

        const string Query_Select_By_NoteNo = @" SELECT * FROM Set_FinancialDetail 
    INNER JOIN Set_FinancialHeader On Set_FinancialHeader.FinancialID = Set_FinancialDetail.FinancialHeaderID WHERE FinancialCode = @NoteNo 
ORDER BY ElementSequence, LineSequence ";

        const string Query_Select_By_PrimaryKey = @" 
SELECT Set_FinancialDetail.FinancialDetailID, Set_FinancialDetail.FinancialHeaderID, Set_FinancialDetail.LineSequence, Set_FinancialDetail.CompanyID, 
    Set_FinancialDetail.ChartOfAccountID, Set_FinancialDetail.COACodeFrom, Set_FinancialDetail.COACodeTo, Set_FinancialDetail.BusinessPartnerID, Set_FinancialDetail.BankID, 
    Set_FinancialDetail.GroupData, Set_FinancialDetail.Formula, Set_FinancialDetail.FixedInput, Set_FinancialDetail.Amount, AR_Set_Bank.BankName, 
    AR_SET_Customer.CustomerName BusinessPartnerName, AR_SET_BankAccount.BankAccountName, Set_FinancialHeader.ElementSequence AS HeaderSequence,Set_FinancialDetail.Segment, '' AS Action ,TypeCodeID
	,AR_SET_BusinessPartner .BusinessPartnerName as BusinessPartnerType
FROM Set_FinancialDetail 
    Inner Join Set_FinancialHeader On Set_FinancialHeader.FinancialID = Set_FinancialDetail.FinancialHeaderID And Set_FinancialHeader.FinancialName = @FinancialName And  Set_FinancialHeader.FinancialCode=@FinancialCode 
    left join AR_SET_Customer on AR_SET_Customer.CustomerID = Set_FinancialDetail.BusinessPartnerID 
    left join AR_SET_BusinessPartner on AR_SET_BusinessPartner.BusinessPartnerID = Set_FinancialDetail.TypeCodeID
    left join AR_SET_BankAccount on AR_SET_BankAccount.BankAccountID = Set_FinancialDetail.BankID 
    Left Join AR_Set_Bank On AR_Set_Bank.BankID = Set_FinancialDetail.BankID ";

        const string Query_Get_AllSequence = @"Select LineSequence from SET_FinancialDetail where LineSequence = @LineSequence AND FinancialHeaderID = @FinancialheaderID";

        #endregion

        #region Public Methods

        public static DataTable Get_FinacialDetail(string reportsName, SqlConnection objConnection = null, int? Segment = null,string CompanyID = "1")
        {
            DataTable dtToReturn = null;

            try
            {
                DataSet dsToReturn = null;
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                if (Segment.HasValue)
                    sqlparams[1] = new SqlParameter("@Segment", Segment.Value);
                else
                    sqlparams[1] = new SqlParameter("@Segment", DBNull.Value);

                CompanyID = "(" + CompanyID + ")";
                if (objConnection != null)
                    dsToReturn = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_Select_FinancialDetail, reportsName).Replace("@CompanyID", CompanyID));
                else
                    dsToReturn = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Select_FinancialDetail, reportsName).Replace("@CompanyID", CompanyID));

                if (dsToReturn != null && dsToReturn.Tables.Count > 0)
                    dtToReturn = dsToReturn.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataTable Get_Data_By_NoteNo(string noteNo, SqlConnection objConnection = null)
        {
            DataSet dsToReturn = null;
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@NoteNo", noteNo);

                if (objConnection != null)
                    dsToReturn = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_Select_By_NoteNo, sqlparams);
                else
                    dsToReturn = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_NoteNo, sqlparams);

                if (dsToReturn != null && dsToReturn.Tables.Count > 0)
                    dtToReturn = dsToReturn.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataTable Get_DataByKey(string financialName,string financialCode)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@FinancialName", financialName);
                sqlparams[1] = new SqlParameter("@FinancialCode", financialCode);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_PrimaryKey, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static bool DeleteDataByKey( int FinancialHeaderID, int FinancialDetailID,SqlTransaction objTransaction)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@FinancialHeaderID", FinancialHeaderID);
                sqlparams[1] = new SqlParameter("@FinancialDetailID", FinancialDetailID);
               

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByKey, sqlparams) > 0)
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

        public static DataTable GetAllSequence(short LineSeq , int finhdrID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@LineSequence", LineSeq);
                sqlparams[1] = new SqlParameter("@FinancialheaderID", finhdrID);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_AllSequence, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
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
