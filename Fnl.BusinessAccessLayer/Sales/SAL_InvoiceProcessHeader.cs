using Common.Utils;
using DAL.DataAccessLayer;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;

namespace FnI.BusinessAccessLayer.Sales
{
    public partial class SAL_InvoiceProcessHeader : BaseClass
    {
        #region Class Methods


        public bool InsertInvoiceProcessHeaderAndDetail(List<SAL_InvoiceProcessDetail> colInvProcDet, int CurrentFiscalID , SqlTransaction sql = null)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertInvoice");

            try
            {
                this.FiscalID = CurrentFiscalID;
                this.Insert(sqltrans);

                foreach (SAL_InvoiceProcessDetail invpd in colInvProcDet)
                {
                    invpd.InvoiceProcessHeaderID = this.InvoiceProcessID;
                    invpd.InvoiceProcessDetailID = Convert.ToInt16(SAL_InvoiceProcessDetail.GetNewInvoiceProcessDetailID(CompanyID, this.InvoiceProcessID, PeriodID, sqltrans));
                    invpd.FiscalID = CurrentFiscalID;
                    invpd.Insert(sqltrans);
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
                {
                    SqlHelper.CommitTransaction(sqltrans);
                }
                else
                {
                    sqltrans = null;
                    SqlHelper.RollBackTransaction(sqltrans, "InsertInvoice");
                }
            }

            return result;
        }


        //public bool UpdateDOHeaderAndDetail(List<SAL_DeliveryOrderDetail> colAddDODetail, List<SAL_DeliveryOrderDetail> colUpdateDODetail, List<SAL_DeliveryOrderDetail> colDeleteDODetail, SET_DocumentApprove da, List<SAL_DOVehicleInformation> colUpdateDOVIDetail = null, List<SAL_DOVehicleInformation> colDeleteDOVIDetail = null, DataRow[] OrderDtl = null, int CurrentFiscalID = 0)
        //{
        //    bool result = false;
        //    short DoDetail = 0;
        //    short DeleteDoDetail = 0;
        //    SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateDeliveryOrder");

        //    try
        //    {
        //        this.UpdateDataByKey(sqltrans);

        //        foreach (SAL_DeliveryOrderDetail dodAdd in colAddDODetail)
        //        {
        //            dodAdd.DODetailID = Convert.ToInt16(SAL_DeliveryOrderDetail.GetNewDODetailID(CompanyID, DeliveryOrderID, PeriodID, sqltrans));
        //            dodAdd.FiscalID = CurrentFiscalID;
        //            //DoDetail = dodAdd.DODetailID;
        //            dodAdd.Insert(sqltrans);

        //            var Records = from objDOVehicle in colUpdateDOVIDetail
        //                          //where objDOVehicle.RowUniqueID == dodAdd.RowUniqueID
        //                          where objDOVehicle.DOHeaderID == dodAdd.SalesOrderHeaderID
        //                            && objDOVehicle.DODetailID == dodAdd.SalesOrderDetailID && objDOVehicle.DOVehicleInformationID == -1
        //                          select objDOVehicle;

        //            #region Do Vehicle Information
        //            foreach (SAL_DOVehicleInformation dovi in Records)
        //            {
        //                dovi.DOVehicleInformationID = short.Parse(SAL_DOVehicleInformation.GetNewDOVehicleInformationID(CompanyID, PeriodID, sqltrans).ToString());
        //                dovi.DOHeaderID = dodAdd.DOHeaderID;
        //                dovi.DODetailID = (int)dodAdd.DODetailID;
        //                dovi.Insert(sqltrans);
        //                SAL_DispatchVehicleInformation sdvi = new SAL_DispatchVehicleInformation();
        //                sdvi.VehicleInformationID = dovi.DOVehicleInformationID;
        //                sdvi.VehicleID = (int)dovi.VehicleID;
        //                sdvi.CompanyID = CompanyID;
        //                sdvi.PeriodID = PeriodID;

        //                sdvi.UpdateDataByVehicleID(sqltrans);
        //            }
        //            //Update Sal_DispatchVehicleInformation (VehicleInformationID) Against Chassis NO. of Dovi VehicleInformation ID 
        //            #endregion

        //            #region Checking For Sales Order Complete Status

        //            CheckQuantity(dodAdd.SalesOrderHeaderID, this.f_CompanyID, this.f_PeriodID, sqltrans, SOEnumaration.SalesOrder_Stage.Delivery, dodAdd.CreatedBy, dodAdd.CreateDate, dodAdd.CreatedByValue);

        //            #endregion
        //        }

        //        foreach (SAL_DeliveryOrderDetail dodUpdate in colUpdateDODetail)
        //        {
        //            DoDetail = (short)dodUpdate.DODetailID;
        //            dodUpdate.FiscalID = CurrentFiscalID;
        //            dodUpdate.UpdateDataByKey(sqltrans);

        //            if (colUpdateDOVIDetail != null)
        //            {
        //                var Records = from objDOVehicle in colUpdateDOVIDetail
        //                              where objDOVehicle.RowUniqueID == dodUpdate.RowUniqueID && objDOVehicle.DOVehicleInformationID == -1
        //                              select objDOVehicle;

        //                #region Do Vehicle Information

        //                foreach (SAL_DOVehicleInformation dovi in Records)
        //                {
        //                    dovi.DOVehicleInformationID = short.Parse(SAL_DOVehicleInformation.GetNewDOVehicleInformationID(CompanyID, PeriodID, sqltrans).ToString());
        //                    dovi.DOHeaderID = dodUpdate.DOHeaderID;
        //                    dovi.DODetailID = Convert.ToInt32(dodUpdate.DODetailID);
        //                    dovi.Insert(sqltrans);
        //                    SAL_DispatchVehicleInformation sdvi = new SAL_DispatchVehicleInformation();
        //                    sdvi.VehicleInformationID = dovi.DOVehicleInformationID;
        //                    sdvi.VehicleID = (int)dovi.VehicleID;
        //                    sdvi.CompanyID = CompanyID;
        //                    sdvi.PeriodID = PeriodID;
        //                    sdvi.UpdateDataByVehicleID(sqltrans);
        //                }

        //                #endregion
        //            }

        //            #region Checking For Sales Order Complete Status

        //            CheckQuantity(dodUpdate.SalesOrderHeaderID, this.f_CompanyID, this.f_PeriodID, sqltrans, SOEnumaration.SalesOrder_Stage.Delivery,
        //                f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue);

        //            #endregion



        //        }

        //        foreach (SAL_DeliveryOrderDetail dodDelete in colDeleteDODetail)
        //        {
        //            DeleteDoDetail = (short)dodDelete.DODetailID;
        //            SAL_DeliveryOrderDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, dodDelete.DODetailID,
        //                DeliveryOrderID, f_PeriodID);
        //        }

        //        #region Update CloseCode in SaleOrder
        //        if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Cancelled)))
        //        {
        //            foreach (DataRow dr in OrderDtl)
        //            {
        //                SAL_OrderHeader soh = new SAL_OrderHeader(CompanyID, int.Parse(dr["SalesOrderHeaderID"].ToString()), PeriodID, null, sqltrans);
        //                soh.ClosedCode = null;
        //                soh.FiscalID = CurrentFiscalID;
        //                soh.UpdateDataByKey(sqltrans);



        //            }
        //        }
        //        #endregion


        //        #region Al Haj Faw
        //        if (colUpdateDOVIDetail != null)
        //        {
        //            foreach (SAL_DOVehicleInformation doviUpdate in colUpdateDOVIDetail)
        //            {
        //                if (doviUpdate.DID > 0)
        //                {
        //                    doviUpdate.DOHeaderID = this.DeliveryOrderID;
        //                    doviUpdate.DODetailID = doviUpdate.DID;
        //                    doviUpdate.UpdateDataByKey(sqltrans);

        //                    SAL_DispatchVehicleInformation sdvi2 = new SAL_DispatchVehicleInformation();
        //                    sdvi2.CompanyID = CompanyID;
        //                    sdvi2.PeriodID = PeriodID;
        //                    sdvi2.VehicleInformationID = doviUpdate.DOVehicleInformationID;
        //                    sdvi2.UpdateDataByVehicleInformationID(sqltrans);

        //                    SAL_DispatchVehicleInformation sdvi = new SAL_DispatchVehicleInformation();
        //                    sdvi.VehicleInformationID = doviUpdate.DOVehicleInformationID;
        //                    sdvi.VehicleID = (int)doviUpdate.VehicleID;
        //                    sdvi.CompanyID = CompanyID;
        //                    sdvi.PeriodID = PeriodID;
        //                    sdvi.UpdateDataByVehicleID(sqltrans);
        //                }
        //            }
        //        }
        //        if (colDeleteDOVIDetail != null)
        //        {
        //            foreach (SAL_DOVehicleInformation doviDelete in colDeleteDOVIDetail)
        //            {
        //                doviDelete.DOHeaderID = this.DeliveryOrderID;
        //                doviDelete.DODetailID = DeleteDoDetail;
        //                SAL_DOVehicleInformation.VehicleDeleteByKey(sqltrans, doviDelete.UpdatedBy.Value, doviDelete.UpdateDate.Value, doviDelete.UpdatedByValue, CompanyID, doviDelete.DOVehicleInformationID, PeriodID);

        //                SAL_DispatchVehicleInformation sdvi3 = new SAL_DispatchVehicleInformation();
        //                sdvi3.CompanyID = CompanyID;
        //                sdvi3.PeriodID = PeriodID;
        //                sdvi3.VehicleInformationID = doviDelete.DOVehicleInformationID;
        //                sdvi3.UpdateDataByVehicleInformationID(sqltrans);

        //            }
        //        }
        //        #endregion

        //        #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

        //        if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
        //        {
        //            da.FiscalID = CurrentFiscalID;
        //            da.Insert(sqltrans);
        //        }

        //        #endregion

        //        result = true;
        //    }
        //    catch (BusinessLogicException ex)
        //    {
        //        SqlHelper.LogException(ex);
        //        throw ex;
        //    }
        //    catch (Exception ex)
        //    {
        //        SqlHelper.LogException(ex);
        //        throw ex;
        //    }
        //    finally
        //    {
        //        if (result)
        //            SqlHelper.CommitTransaction(sqltrans);
        //        else
        //            SqlHelper.RollBackTransaction(sqltrans, "UpdateDeliveryOrder");
        //    }

        //    return result;
        //}

     #endregion
    }
}
