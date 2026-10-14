using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace FnI.BusinessAccessLayer.Sales
{
    public partial class SAL_Token
    {
        const string Query_SelectAll_WithPagingSearch = @" 
        SELECT [SaleTokenID], [CompanyID], [PeriodID], [DocumentTypeID], [SaleTokenNo], [SaleTokenDate], [SaleTokenTime], [TransporterID], 
               [BiltyNo], [VehicleNo], [BiltyAddress], [BiltyDate], [VehicleTypeID], [DriverName], [FatherName], [CNICNO], [DriverMobileNo], 
               [StatusCode], [CreatedBy], [CreateDate], [UpdatedBy], [UpdateDate], [CreatedByValue], [UpdatedByValue], [Status], [CurrentFiscalID], 
ROW_NUMBER() Over (Order by CompanyID, PeriodID, SaleTokenID desc) as 'Row' FROM [SAL_Token] ";

        const string Query_SelectAll_WithoutPagingSearch = @" SELECT [SaleTokenID], [CompanyID], [PeriodID], [DocumentTypeID], [SaleTokenNo], [SaleTokenDate], 
       [SaleTokenTime], [TransporterID], [BiltyNo], [VehicleNo], [BiltyAddress], [BiltyDate], [VehicleTypeID], [DriverName], [FatherName], 
       [CNICNO], [DriverMobileNo], [StatusCode], [CreatedBy], [CreateDate], [UpdatedBy], [UpdateDate], [CreatedByValue], [UpdatedByValue], [Status], 
    [CurrentFiscalID] FROM [SAL_Token] "; 


        const string Query_GetData_byKey_Barcode = @" SELECT (CASE LEN([SAL_Token].SaleTokenNo)
                          WHEN 1
                          THEN '00000000000'
                               + CAST([SAL_Token].SaleTokenNo AS VARCHAR)
                          WHEN 2
                          THEN '0000000000'
                               + CAST([SAL_Token].SaleTokenNo AS VARCHAR)
                          WHEN 3
                          THEN '000000000'
                               + CAST([SAL_Token].SaleTokenNo AS VARCHAR)
                          WHEN 4
                          THEN '00000000'
                               + CAST([SAL_Token].SaleTokenNo AS VARCHAR)
                          WHEN 5
                          THEN '0000000'
                               + CAST([SAL_Token].SaleTokenNo AS VARCHAR)
                          ELSE CAST([SAL_Token].SaleTokenNo AS VARCHAR)
                        END ) as Barcode,SET_VehicleType.TypeName AS VehicleName, AR_SET_Customer.CustomerName AS TransporterName,* FROM [SAL_Token] 
						INNER JOIN SET_VehicleType ON SET_VehicleType.VehicleTypeID = SAL_Token.VehicleTypeID
						AND SET_VehicleType.Status = 1
						INNER JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = SAL_Token.TransporterID
						AND AR_SET_Customer.Status = 1
						WHERE [SAL_Token].[CompanyID] = @CompanyID and [SAL_Token].[SaleTokenNo] = @SaleTokenID and [SAL_Token].[PeriodID] = @PeriodID and [SAL_Token].[CurrentFiscalID] = @CurrentFiscalID and [SAL_Token].[DocumentTypeID] = @DocumentTypeID";

        const string Query_GetDataForSalesTokenUpdate = @"SELECT * FROM SAL_Token
            WHERE SaleTokenNo = @SaleTokenNo AND DocumentTypeID = @TokenTypeID AND CompanyID = @CompanyID
            AND PeriodID = @PeriodID AND CurrentFiscalID = @CurrentFiscalID AND Status = 1";


        public static int GetNewDocumentNo(int companyID, int periodID, int fiscalID, int DocumentTypeID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;

            const string Query_GetMaxDocNo = @" Select ISNULL(max(SaleTokenNo),0)+1 DocumentNo from SAL_Token 
where CompanyID = @CompanyID and PeriodID= @PeriodID and DocumentTypeID = @DocumentTypeID and CurrentFiscalID = @CurrentFiscalID ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", fiscalID);

                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_GetMaxDocNo, sqlparams));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_GetMaxDocNo, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetMaxDocNo, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static DataRow GetDataByKeyForBarcode(int companyID, int SaleTokenID, int periodID, int fiscalID, int documentTypeID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@SaleTokenID", SaleTokenID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", fiscalID);
                sqlparams[4] = new SqlParameter("@DocumentTypeID", documentTypeID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byKey_Barcode, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byKey_Barcode, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byKey_Barcode, sqlparams);

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


        public static DataTable GetAllDataForSaleFirstWeight(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> CurrentFiscalID, int SaleTokenNo, int type, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            const string Query = @" SELECT * FROM SAL_Token WHERE [CompanyID] = @CompanyID and SaleTokenNo = @SaleTokenNo and [PeriodID] = @PeriodID and DocumentTypeID =  @DocumentTypeID and CurrentFiscalID =  @CurrentFiscalID  
and SaleTokenNo not in
(Select DocumentNo from SAL_VehicleCancellation where DocumentTypeID = @DocumentTypeID )";

            //if (type != "")
            //{
            //    sb.Append(" and DocumentTypeID in (Select DocumentTypeID from SET_DocumentType where DocumentType like '%" + type + "%') ");
            //}

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SaleTokenNo", SaleTokenNo);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", type);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query + sb.ToString(), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query + sb.ToString(), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query + sb.ToString(), sqlparams);

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

        public static DataTable CheckForDuplicate(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> CurrentFiscalID, int TokenID, int TokenDocType, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();

            const string Query = @" 
Select * from SAL_FirstWeight where TokenID = @TokenID and TokenDocType = @TokenDocType
and SAL_FirstWeight.Status = 1 and [CompanyID] = @CompanyID and PeriodID = @PeriodID and CurrentFiscalID = @CurrentFiscalID
  ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TokenID", TokenID);
                sqlparams[3] = new SqlParameter("@TokenDocType", TokenDocType);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query + sb.ToString(), sb1.ToString()), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query + sb.ToString(), sb1.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query + sb.ToString(), sb1.ToString()), sqlparams);

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

        public static DataTable GetAllDataSerachMode(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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

                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_SelectAll_WithPagingSearch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPagingSearch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_WithPagingSearch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPagingSearch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAll_WithoutPagingSearch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_WithoutPagingSearch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataRow GetDataSalesTokenUpdate(int companyID, int SaleTokenNo, int periodID, int fiscalID, int tokenTypeID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@SaleTokenNo", SaleTokenNo);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", fiscalID);
                sqlparams[4] = new SqlParameter("@TokenTypeID", tokenTypeID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetDataForSalesTokenUpdate, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDataForSalesTokenUpdate, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataForSalesTokenUpdate, sqlparams);

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

        public bool UpdateTablesFromTokenNo(SAL_Token saleToken, SAL_FirstWeight saleFirstWeight, SAL_DispatchHeader disaptchHeader)
        {
            bool result = false;

            try
            {
                this.UpdateDataForSaleToken(CompanyID, PeriodID, (int)CurrentFiscalID, (int)saleToken.DocumentTypeID, (int)saleToken.SaleTokenNo, (int)saleToken.TransporterID,
                saleToken.BiltyNo, saleToken.BiltyAddress, saleToken.DriverMobileNo, saleToken.VehicleNo);

                #region Update Sales First Weight

                saleFirstWeight.UpdateDataForSaleFirstWeight(CompanyID, PeriodID, (int)CurrentFiscalID, (int)saleFirstWeight.TokenDocType, (int)saleFirstWeight.TokenID, saleFirstWeight.VehicleNo);

                #endregion

                #region Update Dispatch Header

                disaptchHeader.UpdateDataForDispatchHeader(CompanyID, PeriodID, (int)CurrentFiscalID, (int)disaptchHeader.TokenTypeID, (int)disaptchHeader.TokenNo, (int)disaptchHeader.TransporterID,
                disaptchHeader.BiltyNo, disaptchHeader.BiltyAddress, disaptchHeader.DriverContactNumber, disaptchHeader.VehicleNo);

                #endregion

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

            return result;
        }

        public bool UpdateDataForSaleToken(int companyid, int periodid, int FiscalID, int DocumentTypeID, int SaleTokenNo, 
            int TransporterID, string BiltyNo, string BiltyAddress, string DriverMobileNo, string VehicleNo)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[10];
                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", periodid);
                sqlparams[2] = new SqlParameter("@FiscalID", FiscalID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[4] = new SqlParameter("@SaleTokenNo", SaleTokenNo);
                sqlparams[5] = new SqlParameter("@TransporterID", TransporterID);
                sqlparams[6] = new SqlParameter("@BiltyNo", BiltyNo);
                sqlparams[7] = new SqlParameter("@BiltyAddress", BiltyAddress);
                sqlparams[8] = new SqlParameter("@DriverMobileNo", DriverMobileNo);
                sqlparams[9] = new SqlParameter("@VehicleNo", VehicleNo);


                string Query = @"Update SAL_Token SET TransporterID = @TransporterID, BiltyNo = @BiltyNo, VehicleNo = @VehicleNo, BiltyAddress = @BiltyAddress, DriverMobileNo = @DriverMobileNo
                Where CompanyID = @CompanyID And PeriodID = @PeriodID And CurrentFiscalID = @FiscalID And DocumentTypeID = @DocumentTypeID
                And SaleTokenNo = @SaleTokenNo And Status = 1";

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query, sqlparams) > 0)
                    result = true;

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

    }
}
