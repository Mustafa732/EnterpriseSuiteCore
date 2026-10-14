using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;

using Common.Utils;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Inventory.Setup
{


    public partial class StoppagesAndReason
    {
     //  const string Query_Delete_ByKey = @" Update [StoppagesAndReason] Set [Status] = @Status , [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [StoppageID] = @StoppageID ";
      
        public static bool Insert_StoppagesAndReason(int StoppageID, StoppagesAndReason objbank, List<Stoppages_ReasonDetail> ColAddStoppagesAndReasonDet)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertBank_BranchDet");

            try
            {
                DataRow drBank = StoppagesAndReason.GetDataByKey(StoppageID);

                if (drBank != null)
                    objbank.UpdateDataByKey(sqltrans);

                else
                    objbank.Insert(sqltrans);

                using (Stoppages_ReasonDetail obj_BankBranch = new Stoppages_ReasonDetail())
                    obj_BankBranch.DeletePermanentByForeignKey(sqltrans, StoppageID);

                foreach (Stoppages_ReasonDetail rBranch in ColAddStoppagesAndReasonDet)
                    rBranch.Insert(sqltrans);

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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertBank_BranchDet");
            }

            return result;
        }

        public static DataTable GetDataByName(string StoppageName, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@StoppageName", StoppageName);

                DataSet ds = null;
                const string Query_GetData_byName = @" SELECT * FROM [StoppagesAndReason] WHERE [StoppageName] = @StoppageName ";

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byName, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byName, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byName, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dr = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }

        public static bool Update_StoppagesAndReason(StoppagesAndReason objBank, List<Stoppages_ReasonDetail> ColDeleteBrDt, List<Stoppages_ReasonDetail> ColAddBranchDt)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("Update_BankAndBranchDetail");
            int ID = 0;
            try
            {
                if (!(objBank.UpdateDataByKey(sqltrans)))
                    throw new BusinessLogicException("Error in update Bank: ");

                foreach (Stoppages_ReasonDetail CD in ColDeleteBrDt)
                    if (!(Stoppages_ReasonDetail.DeletePermanentByKey(sqltrans, CD.StoppagesDetailID)))
                        throw new BusinessLogicException("Error in delete Branch: ");

                //foreach (Stoppages_ReasonDetail BDt in ColAddBranchDt)
                //{
                //    if (BDt.StoppagesDetailID == -1)
                //    {
                      
                //        BDt.StoppagesDetailID = Stoppages_ReasonDetail.GetNewStoppagesDetailID();
                       
                //        // ColAddBranchDt.Add(BDt);
                //        //ID++;
                //      //  BDt.StoppagesDetailID = ID;
                //        //    BDt.Insert(sqltrans);
                //    }

                //    //else if (!(BDt.UpdateDataByKey(sqltrans)))
                //    //    throw new BusinessLogicException("Error in update Branch: ");
                //        // BDt.StoppagesDetailID = ID;
                //   // ColAddBranchDt.Add(BDt);
                //}
                foreach (Stoppages_ReasonDetail BD in ColAddBranchDt)
                {
                    if (BD.StoppagesDetailID == -1)
                    {
                        BD.StoppagesDetailID = Stoppages_ReasonDetail.GetNewDetailID(objBank.StoppageID, sqltrans);
                        BD.StoppageID = objBank.StoppageID;
                      

                        if (!(BD.Insert(sqltrans)))
                            throw new BusinessLogicException("Error in insert Branch: ");
                    }
                    else if (!(BD.UpdateDataByKey(sqltrans)))
                        throw new BusinessLogicException("Error in update Branch: ");
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
                    SqlHelper.RollBackTransaction(sqltrans, "Update_BankAndBranchDetail");
            }

            return result;
        }


      
        //public static bool DeleteDataByKey(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, int StoppageID)
        //{
        //    bool result = false;

        //    try
        //    {
        //        SqlParameter[] sqlparams = new SqlParameter[5];
        //        sqlparams[0] = new SqlParameter("@StoppageID", StoppageID);
        //        sqlparams[1] = new SqlParameter("@Status", false);
        //        sqlparams[2] = new SqlParameter("@UpdatedBy", updatedBy);
        //        sqlparams[3] = new SqlParameter("@UpdateDate", updateDate);
        //        sqlparams[4] = new SqlParameter("@UpdatedByValue", updatedByValue);

        //        if(objTransaction == null)
        //        {
        //            if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByKey, sqlparams) > 0)
        //                result = true;
        //        }
        //        else
        //        {
        //            if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByKey, sqlparams) > 0)
        //                result = true;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        SqlHelper.LogException(ex);
        //        throw ex;
        //    }

        //    return result;
        //}

	
	
    }
}
