using System;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Sales
{
    public partial class SAL_ProductionDetail
    {
        #region Queries

        const string Query_Delete_By_ProductionID = @" Update [SAL_ProductionDetail] Set [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [ProductionID] = @ProductionID ";

        const string Query_GetData_WithOtherTables = @" SELECT loc.LocationID, loc.[LocationName] [Location], prodet.ShiftCode AS ShiftID, 
                enum.EnumeratedDescription [Shift], prodet.ProductQuality [Quality], uom.UOMName, prodet.[AlternateProductUnitID], 
                Case when (prodet.AlternateUnitQuantityTotal / prodet.Quantity) = 1 then 0 else (prodet.AlternateUnitQuantityTotal / prodet.Quantity) end AS AlternateUnitQuantity, 
                prodet.[Quantity], prodet.[AlternateUnitQuantityTotal] 
            FROM SAL_ProductionDetail prodet 
	            INNER JOIN SET_Location loc ON loc.LocationID = prodet.LocationID 
	            INNER JOIN LookupDetail enum ON enum.LookupDetailID = prodet.ShiftCode AND enum.LookupHeaderID = 12 
	            LEFT JOIN SET_UnitofMeasure uom ON prodet.AlternateProductUnitID = uom.UOMID 
            where prodet.[CompanyID] = @CompanyID AND prodet.[PeriodID] = @PeriodID AND prodet.[ProductionID] = @ProductionID ";

        const string Query_Delete_By_ForeignKey = @" DELETE FROM ProductionDetail WHERE [ProductionID] = @ProductionID ";

        #endregion

        #region Class Methods

        public static bool DeleteDataByKey(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, int companyID, int periodID, 
            int productionID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ProductionID", productionID);
                sqlparams[3] = new SqlParameter("@Status", false);
                sqlparams[4] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[5] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[6] = new SqlParameter("@UpdatedByValue", updatedByValue);

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

        public static DataTable GetDataWithOtherTables(int companyID, int periodID, int productionID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ProductionID", productionID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_WithOtherTables, sqlparams);

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

        public static bool DeleteDataByForiegnKey(int intProductionID, SqlTransaction trans)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@ProductionID", intProductionID);
            
                if (SqlHelper.ExecuteNonQuery(trans, CommandType.Text, Query_Delete_By_ForeignKey, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw new BusinessLogicException("Error in delete class production detail: " + ex.Message);
            }

            return result;
        }

        #endregion
    }
}
