using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;


namespace FnI.BusinessAccessLayer.Sales
{
    public partial class SAL_DispatchNoteHeader : BaseClass
    {
        const string Query_GetData_byKey_WithOtherTable = @" 
SELECT SAL_DispatchNoteDetail.Comments, SAL_DispatchNoteDetail.Description as ItemDescription,'Update' as Action,SAL_DispatchNoteHeader.Comments as CommentsHeader,* FROM [SAL_DispatchNoteHeader]
inner join SAL_DispatchNoteDetail on SAL_DispatchNoteDetail.DispatchNoteID = SAL_DispatchNoteHeader.DispatchNoteID
and SAL_DispatchNoteDetail.CompanyID = SAL_DispatchNoteHeader.CompanyID
and SAL_DispatchNoteDetail.PeriodID = SAL_DispatchNoteHeader.PeriodID
and SAL_DispatchNoteDetail.Status = 1 
inner join ap_purchaseOrderHeader on ap_purchaseOrderHeader.POHeaderID = SAL_DispatchNotedetail.POHeaderID
and  ap_purchaseOrderHeader.CompanyID = SAL_DispatchNotedetail.CompanyID
and  ap_purchaseOrderHeader.PeriodID = SAL_DispatchNotedetail.PeriodID
and ap_purchaseOrderHeader.status = 1
 WHERE [SAL_DispatchNoteHeader].[CompanyID] = @CompanyID and [SAL_DispatchNoteHeader].[DispatchNoteID] = @DispatchNoteID and [SAL_DispatchNoteHeader].[PeriodID] = @PeriodID ";

        public bool InsertHeaderAndDetail(List<SAL_DispatchNoteDetail> lst_DispatchNoteDetail, int CurrentFiscalID = 0)
        {
            bool result = false;
            short serialNo = 1;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertPO");

            try
            {
                //this.DocumentNumber = SET_DocumentSequence.ManualDocumentSequence(f_TypeCode, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID).ToString();
                this.FiscalID = CurrentFiscalID;
                this.Insert(sqltrans);


                foreach (SAL_DispatchNoteDetail podA in lst_DispatchNoteDetail)
                {
                    podA.DispatchNoteID = this.DispatchNoteID;
                    podA.DispatchNoteDetailID = short.Parse(SAL_DispatchNoteDetail.GetNewDispatchNoteDetailID(CompanyID,DispatchNoteID, PeriodID, sqltrans).ToString());
                    podA.Insert(sqltrans);
                }
             

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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertPO");
            }

            return result;
        }
        public bool UpdateHeaderAndDetail(List<SAL_DispatchNoteDetail> lst_DispatchNoteDetail, int CurrentFiscalID = 0)
        {
            bool result = false;
            short serialNo = 1;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertPO");

            try
            {
                //this.DocumentNumber = SET_DocumentSequence.ManualDocumentSequence(f_TypeCode, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID).ToString();
                this.FiscalID = CurrentFiscalID;
                this.UpdateDataByKey(sqltrans);


                foreach (SAL_DispatchNoteDetail podA in lst_DispatchNoteDetail)
                {
                  //  podA.DispatchNoteID = this.DispatchNoteID;
                    //podA.DispatchNoteDetailID = short.Parse(SAL_DispatchNoteDetail.GetNewDispatchNoteDetailID(CompanyID, DispatchNoteID, PeriodID, sqltrans).ToString());
                    podA.UpdateDataByKey(sqltrans);
                }


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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertPO");
            }

            return result;
        }
        public static DataTable GetDataByKey_WithOtherTable(int companyID, int dispatchNoteID, int periodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@DispatchNoteID", dispatchNoteID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byKey_WithOtherTable, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byKey_WithOtherTable, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byKey_WithOtherTable, sqlparams);

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
        public static int GetNewDispatchNoteNo(int companyID, int periodID, int fiscalid, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;

            const string Query_GetMaxDNNo = @" Select ISNULL(max(DispatchNoteNo),0)+1 DispatchNoteNo from SAL_DispatchNoteHeader
where CompanyID = @CompanyID and PeriodID= @PeriodID and FiscalID = @FiscalID ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FiscalID", fiscalid);

                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_GetMaxDNNo, sqlparams));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_GetMaxDNNo, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetMaxDNNo, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public bool DeleteDispatchNoteHeaderAndDetail(List<SAL_DispatchNoteDetail> colSSPDelete)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("Deleteobj");
            try
            {
                SAL_DispatchNoteHeader.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, (this.DispatchNoteID), f_PeriodID);

                foreach (SAL_DispatchNoteDetail LdrDelete in colSSPDelete)
                    SAL_DispatchNoteDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, LdrDelete.DispatchNoteDetailID, this.DispatchNoteID, f_PeriodID);

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
                    SqlHelper.RollBackTransaction(sqltrans, "Deleteobj");
            }

            return result;

        }
    }
}
