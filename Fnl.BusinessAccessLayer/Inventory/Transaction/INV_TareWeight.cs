using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;
using System.Text;


namespace FnI.BusinessAccessLayer.Inventory.Transaction
{
    public partial class INV_TareWeight
    {
        #region Constant Strings

        const string Query_Get_MaxGatePassNo = @" SELECT isnull(max(Cast(GatePassNo as int)),0) + 1 as GatePassNo FROM INV_TareWeight WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID ";

        const string Query_Get_GatePassNo = @" SELECT IsNull(max(GatePassNo), 0) as GatePassNo FROM INV_TareWeight WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and TareWeightID=@TareWeightID";

        const string Query_GetData_byTareWeightID = @" 
        SELECT tw.[CompanyID], tw.[PeriodID], tw.[TareWeightID], tw.[VehicleTypeID], TypeName, tw.[VehicleNo], tw.[TareWeight], tw.[GatePassNo], tw.[Status], tw.[CreatedBy], tw.[CreatedByValue], tw.[CreateDate], tw.[UpdatedBy], tw.[UpdateDate], tw.[UpdatedByValue]
        FROM [INV_TareWeight] tw
        Inner join SET_VehicleType on SET_VehicleType.VehicleTypeID = tw.VehicleTypeID
        WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [TareWeightID] = @TareWeightID ";
        

        const string Query_GetDataByKey_ForGrossWtFrm = @" SELECT Set_VehicleType.[TypeName],[INV_TareWeight].* FROM [INV_TareWeight] 
        JOIN Set_VehicleType on INV_TareWeight.VehicleTypeID = Set_VehicleType.VehicleTypeID
        WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [TareWeightID] = @TareWeightID ";


        const string Query_GetDataByKey_ForTare= @"

select INV_TareWeight.CreatedByValue,INV_TareWeight.TareWeightID,INV_TareWeight.TareWeight,INV_TareWeight.GatePassNo,SET_VehicleType.TypeName,INV_TareWeight.ShiftDate,INV_TareWeight.CheckListChecked,INV_TareWeight.ShiftID,INV_TareWeight.CustomerID,INV_TareWeight.ItemID,INV_TareWeight.VehicleTypeID,INV_TareWeight.VehicleNo,INV_TareWeight.Driver,INV_TareWeight.Remarks,INV_TareWeight.ShiftID,Cane_Shift.Description as Shift,AR_SET_Customer.CustomerName,
INV_Item.Description as ItemName,INV_TareWeight.DocumentTypeID,INV_TareWeight.DocumentNo,GateInDocType,GateINNo from INV_TareWeight
left join Cane_Shift on Cane_Shift.ShiftHeaderID = INV_TareWeight.ShiftID
and Cane_Shift.CompanyID = INV_TareWeight.CompanyID
and Cane_Shift.PeriodID = INV_TareWeight.PeriodID

left join AR_SET_Customer on AR_SET_Customer.CustomerID = INV_TareWeight.CustomerID
--and AR_SET_Customer.CompanyID = INV_TareWeight.CompanyID
--and AR_SET_Customer.PeriodID = INV_TareWeight.PeriodID

left join INV_Item on INV_Item.ItemID = INV_TareWeight.ItemID
--and INV_Item.CompanyID = INV_TareWeight.CompanyID
left join SET_VehicleType on SET_VehicleType.VehicleTypeID = INV_TareWeight.VehicleTypeID
where INV_TareWeight.CompanyID =@CompanyID and INV_TareWeight.PeriodID =@PeriodID

and TareWeightID = @TareWeightID
";

        const string Query_SelectAllTareWeight_WithPaging = @"
SELECT 
	INV_TareWeight.[CompanyID], 
	INV_TareWeight.[PeriodID], 
	INV_TareWeight.[TareWeightID], 
	INV_TareWeight.[VehicleTypeID], 
	TypeName,
	INV_TareWeight.[VehicleNo], 
	INV_TareWeight.[TareWeight], 
	INV_TareWeight.[GatePassNo], 
	INV_TareWeight.[Status], 
	INV_TareWeight.[CreatedBy], 
	INV_TareWeight.[CreatedByValue], 
	INV_TareWeight.[CreateDate], 
	INV_TareWeight.[UpdatedBy], 
	INV_TareWeight.[UpdateDate], 
	INV_TareWeight.[UpdatedByValue], 
	ROW_NUMBER() Over (Order by INV_TareWeight.CompanyID, INV_TareWeight.PeriodID, INV_TareWeight.TareWeightID) as 'Row' 
FROM [INV_TareWeight]
Inner join SET_VehicleType on SET_VehicleType.VehicleTypeID = INV_TareWeight.VehicleTypeID

";

        const string Query_SelectAllTareWeight_WithoutPaging = @" 
SELECT 
	INV_TareWeight.[CompanyID], 
	INV_TareWeight.[PeriodID], 
	INV_TareWeight.[TareWeightID], 
	INV_TareWeight.[VehicleTypeID], 
	TypeName,
	INV_TareWeight.[VehicleNo], 
	INV_TareWeight.[TareWeight], 
	INV_TareWeight.[GatePassNo], 
	INV_TareWeight.[Status], 
	INV_TareWeight.[CreatedBy], 
	INV_TareWeight.[CreatedByValue], 
	INV_TareWeight.[CreateDate], 
	INV_TareWeight.[UpdatedBy], 
	INV_TareWeight.[UpdateDate], 
	INV_TareWeight.[UpdatedByValue]
	
FROM [INV_TareWeight]
Inner join SET_VehicleType on SET_VehicleType.VehicleTypeID = INV_TareWeight.VehicleTypeID

";
        const string Query_GetAllUsedGatePassNo = @"
        SELECT * FROM [INV_TareWeight] WHERE [Status] = 1 AND ([CompanyID] = @CompanyID OR @CompanyID is null) 
        AND ([PeriodID] = @PeriodID OR @PeriodID is null) 
        {0}
        
        ";

        #endregion

        #region Class Methods

        public static int GetNewGatePassNo(int companyID, int periodID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_MaxGatePassNo, sqlparams));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_Get_MaxGatePassNo, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_MaxGatePassNo, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }


        public static int GetGatePassNo(int companyID, int periodID,int TareWeightID ,SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TareWeightID", TareWeightID);

                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_GatePassNo, sqlparams));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_Get_GatePassNo, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_GatePassNo, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static DataTable GetDataByTareWeightID(int companyID, int periodID, int tareWeightID, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TareWeightID", tareWeightID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byTareWeightID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byTareWeightID, sqlparams);

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


        public static DataRow GetDataByKeyForGrossWeightfrm(int companyID, int periodID, int tareWeightID,SqlTransaction objTransaction = null,  SqlConnection objConnection = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TareWeightID", tareWeightID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetDataByKey_ForTare, sqlparams);
                else if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByKey_ForTare, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDataByKey_ForTare, sqlparams);

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


        public static DataRow GetDataByKeyForGrossWeight(int companyID, int periodID, int tareWeightID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TareWeightID", tareWeightID);

                DataSet ds = null;
                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction,CommandType.Text, Query_GetDataByKey_ForTare, sqlparams);
                else if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByKey_ForTare, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDataByKey_ForTare, sqlparams);

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


        public static DataTable GetAllTareWeightData(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc)
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAllTareWeight_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAllTareWeight_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllTareWeight_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        
        public static DataTable GetAllUnusedGatePassNo(int companyID, int periodID, string mode = "add", SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;
                
                string query = Query_GetAllUsedGatePassNo;

                if (mode.ToLower().Equals("add"))
                    query = string.Format(query, " And TareWeightID NOT IN (Select TareWeightID from INV_GrossWeight)");
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

        public static int GetNewDocumentNo(int companyID, int periodID, int DocumentTypeID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;

            const string Query_GetMaxDocNo = @" Select ISNULL(max(DocumentNo),0)+1 DocumentNo from INV_TareWeight 
where CompanyID = @CompanyID and PeriodID= @PeriodID and DocumentTypeID = @DocumentTypeID ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", DocumentTypeID);

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


        public static DataTable CheckForDuplicate(Nullable<int> companyID, Nullable<int> periodID, int GateINID, int doctype, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();

            const string Query = @" Select * from INV_TareWeight where GateINNo = @GateINNo and GateInDocType = @doctype
and INV_TareWeight.Status = 1 and [CompanyID] = @CompanyID and PeriodID = @PeriodID  ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@GateINNo", GateINID);
                sqlparams[3] = new SqlParameter("@doctype", doctype);

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
       
        #endregion
    }
}
