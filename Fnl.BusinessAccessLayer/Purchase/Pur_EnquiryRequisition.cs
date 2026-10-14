using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.AR.Setup;
using FnI.BusinessAccessLayer.Inventory.Setup;
using FnI.BusinessAccessLayer.Setup;

namespace FnI.BusinessAccessLayer.Purchase
{
    public partial class Pur_EnquiryRequisition
    {
        //RowUniqueID
        private int f_RowUniqueID;

        public int RowUniqueID
        {
            get { return f_RowUniqueID; }
            set { f_RowUniqueID = value; }
        }

        #region  CONSTANT STRING

        const string Query_Delete_ByEnquiryHeaderID = @"Delete From Pur_EnquiryRequisition Where EnquiryHeaderID = @EnquiryHeaderID AND CompanyID = @CompanyID AND PeriodID = @PeriodID ";
        
        const string Query_Delete_ByEnquiryHeaderIDAndEnquiryDetail = @"Delete From Pur_EnquiryRequisition Where EnquiryHeaderID = @EnquiryHeaderID AND EnquiryDetailID = @EnquiryDetailID AND CompanyID = @CompanyID AND PeriodID = @PeriodID ";

        const string Query_GetAllDataByEnquiryID = @" 
SELECT DISTINCT Pur_EnquiryRequisition.DocumentNo, Inv_StockTransactionHeader.CostCenterID, Set_CostCenter.CostCenterName 
FROM Pur_EnquiryRequisition 
    INNER JOIN Inv_StockTransactionHeader ON Inv_StockTransactionHeader.CompanyID = Pur_EnquiryRequisition.CompanyID
        AND Inv_StockTransactionHeader.PeriodID = Pur_EnquiryRequisition.PeriodID 
        AND Inv_StockTransactionHeader.StockTransactionID = Pur_EnquiryRequisition.StockHeaderID 
    LEFT JOIN Set_CostCenter ON Set_CostCenter.CostCenterID = Inv_StockTransactionHeader.CostCenterID 
WHERE Pur_EnquiryRequisition.EnquiryHeaderID = @EnquiryHeaderID AND Pur_EnquiryRequisition.CompanyID = @CompanyID AND Pur_EnquiryRequisition.PeriodID = @PeriodID ";

        #endregion
        
        #region  METHODS

        public static bool DeleteDataByEnquiryHeaderID(SqlTransaction objTransaction, int companyID, int periodID, long enquiryHeaderID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@EnquiryHeaderID", enquiryHeaderID);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByEnquiryHeaderID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByEnquiryHeaderID, sqlparams) > 0)
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

        public static bool DeleteDataByEnquiryHeaderIDAndEnquiryDetail(SqlTransaction objTransaction, int companyID, int periodID, int enquiryHeaderID, int enquiryDetailID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@EnquiryHeaderID", enquiryHeaderID);
                sqlparams[3] = new SqlParameter("@EnquiryDetailID", enquiryHeaderID);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByEnquiryHeaderIDAndEnquiryDetail, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByEnquiryHeaderIDAndEnquiryDetail, sqlparams) > 0)
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

        public static DataTable GetAllData(int companyID, int periodID, int enquiryHeaderID, SqlTransaction objTransaction = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@EnquiryHeaderID", enquiryHeaderID);

                DataSet ds = null;

                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllDataByEnquiryID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetAllDataByEnquiryID, sqlparams);

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
