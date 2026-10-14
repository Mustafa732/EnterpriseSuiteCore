using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;
using Common.Utils;

using DAL.DataAccessLayer;

using FnI.BusinessAccessLayer.Inventory.Setup;
using FnI.BusinessAccessLayer.Setup;


namespace FnI.BusinessAccessLayer.Inventory.Transaction
{
    public partial class INV_GatePass
    {
        public static int GetNewGatePassNo(int companyID, int periodID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);


                const string Query_Get_MaxNo = @" SELECT  (isnull(max(GatePassNo), convert(int, (convert(varchar(10), 9900000) ))) + 1) GatePassID 
FROM INV_GatePass WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID ";


                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_MaxNo, sqlparams));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_Get_MaxNo, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_MaxNo, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static DataRow GetDatabyGatePassNo(int companyID, int periodID, int GatePassNo, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@GatePassNo", GatePassNo);

                const string Query_GetDatabyGatePassNo = @" SELECT  * FROM INV_GatePass WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID AND GatePassNo = @GatePassNo ";

                DataSet ds = null;
                 if (objTransaction != null)
                     ds = SqlHelper.ExecuteDataset(objTransaction ,CommandType.Text, Query_GetDatabyGatePassNo, sqlparams);
                else if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDatabyGatePassNo, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDatabyGatePassNo, sqlparams);

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

        public static DataTable GetDataForGatePassWeightmentData(int CompanyID, int PeriodID, long? GatePassNo, DateTime GatePassDate, int? ItemID, int? CustomerID)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);

                if (GatePassNo != null)
                {
                    sb.Append(" AND INV_GatePass.GatePassNo = " + GatePassNo);
                }

                if (GatePassDate != null)
                {
                    sb.Append(" AND CAST(INV_GatePass.CreateDate AS DATE) = '" + (GatePassDate.Date).ToString("yyyy-MM-dd") + "'");
                }

                if (CustomerID != null)
                {
                    sb.Append(" AND INV_GatePass.CustomerID = " + CustomerID);
                }

                const string Query_GetDataForGatePassWeightmentSlip = @"
select INV_Item.ItemID, INV_Item.InventoryItemStatusCode, INV_Item.Description, INV_GatePass.VehicleNo, INV_GatePass.GatePassID, INV_GatePass.GatePassNo, 
AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName, SET_VehicleType.VehicleTypeID, SET_VehicleType.TypeName, INV_GatePass.Remarks, 

INV_TareWeight.TareWeightID, INV_TareWeight.CreateDate as TareWeightDateTime, INV_TareWeight.CreatedByValue AS TareWeightUser,INV_TareWeight.TareWeight, 

INV_GrossWeight.GrossWeightID, INV_GrossWeight.CreateDate GrossWeightDateTime, INV_GrossWeight.CreatedByValue AS GrossWeightUser, INV_GrossWeight.GrossWeight, INV_GrossWeight.NetWeight

from INV_GatePass

INNER JOIN INV_Item ON INV_Item.CompanyID = INV_GatePass.CompanyID
AND INV_Item.ItemID = INV_GatePass.ItemID AND INV_Item.Status = 1 

INNER JOIN AR_SET_Customer ON AR_SET_Customer.CompanyID = INV_GatePass.CompanyID
AND AR_SET_Customer.PeriodID = INV_GatePass.PeriodID AND AR_SET_Customer.CustomerID = INV_GatePass.CustomerID
AND AR_SET_Customer.Status = 1

INNER JOIN SET_VehicleType ON SET_VehicleType.VehicleTypeID = INV_GatePass.VehicleTypeID
AND SET_VehicleType.Status = 1 AND SET_VehicleType.IsActive = 1

LEFT JOIN INV_TareWeight ON INV_TareWeight.CompanyID = INV_GatePass.CompanyID
AND INV_TareWeight.PeriodID = INV_GatePass.PeriodID AND INV_TareWeight.GatePassNo = INV_GatePass.GatePassNo
AND INV_TareWeight.Status = 1

LEFT JOIN INV_GrossWeight ON INV_GrossWeight.CompanyID = INV_TareWeight.CompanyID
AND INV_GrossWeight.PeriodID = INV_TareWeight.PeriodID AND INV_GrossWeight.TareWeightID = INV_TareWeight.TareWeightID
AND INV_GrossWeight.Status = 1

WHERE INV_GatePass.CompanyID = @CompanyID AND INV_GatePass.PeriodID = @PeriodID AND INV_GatePass.Status = 1
{0}";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDataForGatePassWeightmentSlip, sb.ToString()), sqlparams);

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

    }
}
