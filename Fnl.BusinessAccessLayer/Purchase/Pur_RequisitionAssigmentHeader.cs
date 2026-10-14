using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using System.Text;

namespace FnI.BusinessAccessLayer.Purchase
{
    public partial class Pur_RequisitionAssigmentHeader
    {
        public bool InsertEHeaderAndDetail(List<Pur_RequisitionAssigmentDetail> colEDetail,int FiscalID=0)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertAssignment");

            try
            {
                this.Insert(sqltrans);

                //foreach (Pur_RequisitionAssigmentDetail ed in colEDetail)
                //{
                //    ed.AssignmentHDetailID = Convert.ToInt16(Pur_RequisitionAssigmentDetail.GetNewAssignmentHDetailID(AssignmentHeaderID, CompanyID, PeriodID, sqltrans));
                //    ed.Insert(sqltrans);
                //}

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
            finally
            {
                if (result)
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "InsertAssignment");
            }

            return result;
        }

        public static DataRow GetDataByKeyWithOtherTables(int AssignmentHeaderID, int periodID, int companyID)
        {
            DataRow drToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@AssignmentHeaderID", AssignmentHeaderID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);

                #region Qurey
        const  string Query_Select_By_PrimaryKey_WithOtherTables = @"SELECT DISTINCT Pur_RequisitionAssigmentHeader.CompanyID, Pur_RequisitionAssigmentHeader.PeriodID, Pur_RequisitionAssigmentHeader.AssignmentHeaderID
, Pur_RequisitionAssigmentHeader.DocumentNo, Pur_RequisitionAssigmentHeader.AssignmentDate, Pur_RequisitionAssigmentHeader.Comments,  Pur_RequisitionAssigmentHeader.Status, Pur_RequisitionAssigmentHeader.CreatedBy, 
    Pur_RequisitionAssigmentHeader.CreateDate, Pur_RequisitionAssigmentHeader.UpdatedBy, Pur_RequisitionAssigmentHeader.UpdateDate, 
    Pur_RequisitionAssigmentHeader.CreatedByValue,RequisitionDate,Pur_RequisitionAssigmentHeader.StockHeaderID,Pur_RequisitionAssigmentHeader.UserID
	,SET_DocumentType.DocumentType,Pur_RequisitionAssigmentHeader.LocationID
FROM Pur_RequisitionAssigmentHeader
INNER JOIN INV_StockTransactionHeader  ON INV_StockTransactionHeader.StockTransactionID=Pur_RequisitionAssigmentHeader.StockHeaderID
AND INV_StockTransactionHeader.CompanyID=Pur_RequisitionAssigmentHeader.CompanyID
AND INV_StockTransactionHeader.PeriodID=Pur_RequisitionAssigmentHeader.PeriodID
    INNER JOIN SET_DocumentType ON SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
        AND SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID AND SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID 
    Left Join Set_Location On Set_Location.LocationID = Pur_RequisitionAssigmentHeader.LocationID And Set_Location.Status = 1 


WHERE Pur_RequisitionAssigmentHeader.AssignmentHeaderID = @AssignmentHeaderID AND Pur_RequisitionAssigmentHeader.PeriodID = @PeriodID
    AND Pur_RequisitionAssigmentHeader.CompanyID = @CompanyID AND Pur_RequisitionAssigmentHeader.Status = 1";
                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text,Query_Select_By_PrimaryKey_WithOtherTables + sb.ToString(), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    drToReturn = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return drToReturn;
        }
    }
}
