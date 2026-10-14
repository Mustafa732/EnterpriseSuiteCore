using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;
using System.Text;


namespace FnI.BusinessAccessLayer.Sales
{
    public partial class SAL_FirstWeight
    {
        #region Constant Strings

        const string Query_SelectAll_WithPagingWithTables = @" SELECT [SaleFirstWeightID], SAL_FirstWeight.[CompanyID], SAL_FirstWeight.[PeriodID], 
    SAL_FirstWeight.[DocumentTypeID],DocumentType, [DocumentNo], [VehicleNo], [DriverNo], [Remarks], SAL_FirstWeight.[Status],
    [StatusCode], SAL_FirstWeight.[CreatedBy]
,SAL_FirstWeight.SaleFirstWeight, SAL_FirstWeight.[CreatedByValue], SAL_FirstWeight.[CreateDate],
SAL_FirstWeight.[UpdatedBy], SAL_FirstWeight.[UpdateDate], SAL_FirstWeight.[UpdatedByValue], [ShiftID], [ShiftDate], 
SAL_FirstWeight.[TokenID], ROW_NUMBER() Over (Order by SAL_FirstWeight.CompanyID, SaleFirstWeightID desc, 
SAL_FirstWeight.PeriodID) as 'Row' FROM [SAL_FirstWeight]
inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = SAL_FirstWeight.DocumentTypeID
and SET_DocumentType.CompanyID = SAL_FirstWeight.CompanyID ";


        const string Query_GetAllUsedTokenPassNo = @"
        SELECT * FROM [SAL_FirstWeight] WHERE [Status] = 1 AND ([CompanyID] = @CompanyID OR @CompanyID is null) 
        AND ([PeriodID] = @PeriodID OR @PeriodID is null) 
        {0}
        
        ";


        const string Query_GetData_byKey_ForBarCodeForFirstWeightSlip = @" ";

        #endregion

        #region Class Methods

        public static int GetNewDocumentNo(int companyID, int periodID, int fiscalID, int DocumentTypeID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;

            const string Query_GetMaxDocNo = @" Select ISNULL(max(DocumentNo),0)+1 DocumentNo from SAL_FirstWeight 
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

        public static DataTable GetAllDataWithTables(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_SelectAll_WithPagingWithTables + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPagingWithTables + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_WithPagingWithTables + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPagingWithTables + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAll_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetAllUnusedTokenPassNo(int companyID, int periodID, string mode = "add", SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                string query = Query_GetAllUsedTokenPassNo;

                if (mode.ToLower().Equals("add"))
                    query = string.Format(query, " And SaleFirstWeightID NOT IN (Select SaleFirstWeightID from SAL_FirstWeight)");
                else
                    query = string.Format(query, string.Empty);

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, query, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataForSalesSecondWeight(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> CurrentFiscalID, int SaleTokenNo, string type, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            #region Query
            const string Query = @"Select SAL_FirstWeight.DocumentNo, SAL_FirstWeight.DocumentTypeID,SAL_FirstWeight.DriverNo AS DriverName
,SAL_FirstWeight.VehicleNo, SAL_FirstWeight.SaleFirstWeight, SET_VehicleType.TypeName AS VehicleName
from SAL_FirstWeight

LEFT JOIN SAL_Token ON SAL_Token.SaleTokenNo = SAL_FirstWeight.TokenID
AND SAL_Token.DocumentTypeID = SAL_FirstWeight.TokenDocType
AND SAL_Token.CompanyID = SAL_FirstWeight.CompanyID
AND SAL_Token.PeriodID = SAL_FirstWeight.PeriodID
AND SAL_Token.CurrentFiscalID = SAL_FirstWeight.CurrentFiscalID
AND SAL_Token.Status = 1

LEFT JOIN SET_VehicleType ON SET_VehicleType.VehicleTypeID = SAL_Token.VehicleTypeID
AND SET_VehicleType.Status = 1

Where SAL_FirstWeight.CompanyID = @CompanyID AND SAL_FirstWeight.PeriodID = @PeriodID AND SAL_FirstWeight.CurrentFiscalID = @CurrentFiscalID
AND SAL_FirstWeight.Status = 1 AND SAL_FirstWeight.TokenID = @SalesTokenNo";
            #endregion

            if (type != "")
            {
                sb.Append(" and SAL_FirstWeight.DocumentTypeID in (Select DocumentTypeID from SET_DocumentType where DocumentType like '%" + type + "%' and CompanyID = @CompanyID) ");
            }

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SalesTokenNo", SaleTokenNo);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

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



        public static DataTable GetAllDataForSalesSecondWeight_ForDispatch(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> CurrentFiscalID, int SaleTokenNo, string type, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            #region Query
            const string Query = @"Select SAL_FirstWeight.DocumentNo, SAL_FirstWeight.DocumentTypeID,SAL_FirstWeight.DriverNo AS DriverName
,SAL_FirstWeight.VehicleNo, SAL_FirstWeight.SaleFirstWeight, SET_VehicleType.TypeName AS VehicleName
,0 as NetWeight
,SAL_Token.BiltyAddress
,SAL_Token.BiltyNo
,SAL_Token.TransporterID 
,0 as GrossWeight
, SAL_FirstWeight.SaleFirstWeight as TareWeight
,SAL_Token.DocumentTypeID as TokenTypeID
from SAL_FirstWeight

LEFT JOIN SAL_Token ON SAL_Token.SaleTokenNo = SAL_FirstWeight.TokenID
AND SAL_Token.DocumentTypeID = SAL_FirstWeight.TokenDocType
AND SAL_Token.CompanyID = SAL_FirstWeight.CompanyID
AND SAL_Token.PeriodID = SAL_FirstWeight.PeriodID
AND SAL_Token.CurrentFiscalID = SAL_FirstWeight.CurrentFiscalID
AND SAL_Token.Status = 1

LEFT JOIN SET_VehicleType ON SET_VehicleType.VehicleTypeID = SAL_Token.VehicleTypeID
AND SET_VehicleType.Status = 1

left join SAL_SecondWeight on SAL_SecondWeight.SalTokenNo = SAL_Token.SaleTokenNo
AND SAL_SecondWeight.SalTokenType = SAL_FirstWeight.TokenDocType
and SAL_SecondWeight.CompanyID = SAL_Token.CompanyID
and SAL_SecondWeight.PeriodID = SAL_Token.PeriodID
and SAL_SecondWeight.CurrentFiscalID = SAL_Token.CurrentFiscalID
and SAL_SecondWeight.Status = 1

Where SAL_FirstWeight.CompanyID = @CompanyID AND SAL_FirstWeight.PeriodID = @PeriodID AND SAL_FirstWeight.CurrentFiscalID = @CurrentFiscalID
AND SAL_FirstWeight.Status = 1 --AND SAL_FirstWeight.TokenID = @SalesTokenNo
AND SAL_Token.SaleTokenNo = @SalesTokenNo
--and SAL_SecondWeight.SalesSecondWeightID is null
";
            #endregion

            if (type != "")
            {
                sb.Append(" and SAL_FirstWeight.DocumentTypeID in (Select DocumentTypeID from SET_DocumentType where DocumentType like '%" + type + "%') ");
            }

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SalesTokenNo", SaleTokenNo);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

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

        public bool UpdateDataForSaleFirstWeight(int companyid, int periodid, int FiscalID, int DocumentTypeID, int SaleTokenNo, string VehicleNo)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", periodid);
                sqlparams[2] = new SqlParameter("@FiscalID", FiscalID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[4] = new SqlParameter("@SaleTokenNo", SaleTokenNo);
                sqlparams[5] = new SqlParameter("@VehicleNo", VehicleNo);


                string Query = @"Update SAL_FirstWeight SET VehicleNo = @VehicleNo
                Where CompanyID = @CompanyID And PeriodID = @PeriodID And CurrentFiscalID = @FiscalID And TokenDocType = @DocumentTypeID
                And TokenID = @SaleTokenNo And Status = 1";

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


        public static DataRow GetDataByKeyForBarCodeForFirstWeightSlip_PSML(int companyID, int salefirstWeightID, int periodID, string query, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@SaleFirstWeightID", salefirstWeightID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, query, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

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
       
        #endregion
    }
}
