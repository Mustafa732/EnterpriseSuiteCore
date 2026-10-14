using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Purchase
{
	public partial class Pur_RequisitionAssigmentDetail
	{

        public static DataTable GetDataByKeyTable_Detail(int AssignmentHeaderID, int companyID, int periodID, bool status = true)
        {
            DataTable dtToReturn = null;

            #region Qurey
            const string Query_tDataByKeyTable_Detail = @"SELECT Pur_RequisitionAssigmentDetail.AssignmentHDetailID as TransactionDetailID, -1 as SerialNo, INV_SET_Category.CategoryID, 
    INV_SET_Category.CategoryID as ItemCategoryID, INV_SET_Category.CategoryName, Pur_RequisitionAssigmentDetail.ItemID, Item.Description+' '+isnull(Item.InventoryItemStatusCode,'') as ItemDescription, 
    Pur_RequisitionAssigmentDetail.TransactionUOMID,SET_UnitofMeasure.UOMName,Item.InventoryItemStatusCode,
    Abs(Pur_RequisitionAssigmentDetail.TransactionQuantity) AS TransactionQuantity, 
 
    '' [Action]
FROM Pur_RequisitionAssigmentDetail
inner join Pur_RequisitionAssigmentHeader on Pur_RequisitionAssigmentHeader.AssignmentHeaderID = Pur_RequisitionAssigmentDetail.AssignmentHeaderID 
and Pur_RequisitionAssigmentHeader.CompanyID  = Pur_RequisitionAssigmentDetail .CompanyID 
and Pur_RequisitionAssigmentHeader.PeriodID = Pur_RequisitionAssigmentDetail .PeriodID 
    INNER JOIN INV_Item Item ON Item.ItemID = Pur_RequisitionAssigmentDetail.ItemID 
        --and AR_SET_Customer.CompanyID = INV_StockTransactionDetail.CompanyID         and AR_SET_Customer.PeriodID = INV_StockTransactionDetail.PeriodID 
    LEFT JOIN INV_SET_Category ON Item.ItemTypeID = INV_SET_Category.CategoryID --and Item.CompanyID = INV_SET_Category.CompanyID 
    LEFT JOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = Pur_RequisitionAssigmentDetail.TransactionUOMID 

where Pur_RequisitionAssigmentDetail.AssignmentHeaderID = @AssignmentHeaderID and Pur_RequisitionAssigmentDetail.CompanyID = @CompanyID 
    and Pur_RequisitionAssigmentDetail.PeriodID = @PeriodID and Pur_RequisitionAssigmentDetail.Status = 1 
order by Pur_RequisitionAssigmentDetail.AssignmentHDetailID asc";
            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@AssignmentHeaderID", AssignmentHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", status);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_tDataByKeyTable_Detail, sqlparams);

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
