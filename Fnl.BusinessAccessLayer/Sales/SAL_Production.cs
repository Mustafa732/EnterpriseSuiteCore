using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Sales
{
    public partial class SAL_Production
    {
        #region Class Methods

        public static bool Delete_ProductionAndDetail(int productionID, Guid updatedBy, DateTime updateDate, string updatedByValue, int companyID, int periodID)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("Delete_ProductionandDetail");

            try
            {
                SAL_ProductionDetail.DeleteDataByKey(sqltrans, updatedBy, updateDate, updatedByValue, companyID, periodID, productionID);
                DeleteDataByKey(sqltrans, updatedBy, updateDate, updatedByValue, companyID, periodID, productionID);
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
                    SqlHelper.RollBackTransaction(sqltrans, "Delete_ProductionandDetail");
            }

            return result;
        }

        public bool Insert_ProductionAndDetail(List<SAL_ProductionDetail> ColProdDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("Ins_ProductionandDetail");

            try
            {
                this.Insert(sqltrans);

                if (f_ProductionID == 0)
                    throw new BusinessLogicException("Invalid ProductionID");

                foreach (SAL_ProductionDetail det in ColProdDetail)
                {
                    det.ProductionID = ProductionID;
                    det.Insert(sqltrans);
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
                if(result)
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "Ins_ProductionandDetail");
            }

            return result;
        }

        public bool Update_ProductionAndDetail(int intProductionId, List<SAL_ProductionDetail> ColProdDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("Update_ProductionandDetail");

            try
            {
                if (!this.UpdateDataByKey(sqltrans))
                    throw new BusinessLogicException("Error in updating Production");

                if (!SAL_ProductionDetail.DeleteDataByForiegnKey(intProductionId, sqltrans))
                    throw new BusinessLogicException("Error in delete production detail");

                foreach (SAL_ProductionDetail det in ColProdDetail)
                {
                    det.ProductionID = f_ProductionID;
                    det.Insert(sqltrans);
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
                if(result)
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "Update_ProductionandDetail");
            }

            return result;
        }

        #endregion
    }
}
