using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;

namespace FnI.BusinessAccessLayer.AR.Setup
{
    public partial class AR_SET_CustomerRefDetail : BaseClass
    {


        #region

        const string Query_GetData_byCustomerID = @" SELECT * FROM [AR_SET_CustomerRefDetail] WHERE [CompanyID] = @CompanyID and [CustomerID] = @CustomerID and [PeriodID] = @PeriodID ";

        #endregion

        public static DataRow GetDataByCustomerID(int companyID, int periodID, int customerID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@CustomerID", customerID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byCustomerID, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byCustomerID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byCustomerID, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dr = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }
        public static bool DeleteDataByCustomerID( int CustomerID, int CompanyID, int PeriodID,SqlTransaction objTransaction=null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CustomerID", CustomerID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@CompanyID", CompanyID);

                #region
                const string Query_Delete_ByCustomerID = @"Delete from AR_SET_CustomerRefDetail
Where 
CustomerID=@CustomerID AND PeriodID=@PeriodID
AND CompanyID=@CompanyID";
                #endregion


                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByCustomerID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByCustomerID, sqlparams) > 0)
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
        public  bool InertCustomerReferenceDetail()
        {   bool result = false;

        try
        {
            DeleteDataByCustomerID(this.CustomerID, this.CompanyID, this.PeriodID);

            this.Insert();
        }
        catch (Exception ex)
        {
            SqlHelper.LogException(ex);
            throw ex;
        }

        return result;
        }

        public static DataRow GetOtherValuesByImportID(int companyID, int periodID, int importID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@importID", importID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_OtherValuesByImportID, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_OtherValuesByImportID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_OtherValuesByImportID, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dr = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }

        public static DataRow GetUnitWeightByImportID(int companyID, int periodID, int FiscalID, int PODetailID, int importID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@importID", importID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@FiscalID", FiscalID);
                sqlparams[4] = new SqlParameter("@PODetailID", PODetailID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_UnitWeightByImportID, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_UnitWeightByImportID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_UnitWeightByImportID, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dr = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }

        public static DataRow GetGrossAmountByImportID(int companyID, int periodID, int FiscalID, int PODetailID, int importID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@importID", importID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@FiscalID", FiscalID);
                sqlparams[4] = new SqlParameter("@PODetailID", PODetailID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_GrossAmountByImportID, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_GrossAmountByImportID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_GrossAmountByImportID, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dr = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }

        public static DataRow GetValuesByImportID(int companyID, int periodID, int FiscalID, int importID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@importID", importID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@FiscalID", FiscalID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_OtherByImportID, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_OtherByImportID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_OtherByImportID, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dr = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }

        public static DataRow GetValuesByImportIDByItem(int companyID, int periodID, int FiscalID, int importID, int ItemID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@importID", importID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@FiscalID", FiscalID);
                sqlparams[4] = new SqlParameter("@ItemID", ItemID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_OtherByImportIDByItem, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_OtherByImportIDByItem, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_OtherByImportIDByItem, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dr = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }


        public static DataRow GetAmountByPOHeaderID(int companyID, int periodID, int FiscalID, string BLNumber, int PODetailID, int ItemID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@BLNumber", BLNumber);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@FiscalID", FiscalID);
                sqlparams[4] = new SqlParameter("@ItemID", ItemID);
                sqlparams[5] = new SqlParameter("@PODetailID", PODetailID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_AmountbyIMportID, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_AmountbyIMportID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_AmountbyIMportID, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dr = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }

    }

}
