using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.AP
{
    public partial class AP_ProductionJobCardHeader
    {
        #region Methods

        public bool InsertRecipeHeaderAndDetail(List<Ap_ProductionjobcardDetail> colRDDetail, List<AP_ProductionCardStoppageDetail> colStoppageDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("Insert Service Parts");
            
            try
            {

                this.Insert(sqltrans);

                short recipeDetailID = short.Parse(Ap_ProductionjobcardDetail.GetNewProCardDetailID().ToString());
                short StoppageDetailID = short.Parse(AP_ProductionCardStoppageDetail.GetNewStoppageDetailID().ToString());
                foreach (Ap_ProductionjobcardDetail rd in colRDDetail)
                {
                    rd.ProCardID = this.ProCardID;
                    rd.ProCardDetailID = recipeDetailID++;
                    rd.Insert(sqltrans);
                }

                // stoppage Detail
                foreach (AP_ProductionCardStoppageDetail SD in colStoppageDetail)
                {
                    SD.ProCardID = this.ProCardID;
                    SD.StoppageDetailID = StoppageDetailID++;

                    SD.Insert(sqltrans);
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
                    SqlHelper.RollBackTransaction(sqltrans, "Insert Service Parts");
            }

            return result;
        }

        public DataTable GetAllDataGird(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null, 
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

            try
            {
                if (PageIndex != null)
                {
                    #region Query

                    string Query_SelectAllGird_WithPaging = @" 
SELECT Ap_ProductionJobCardHeader.ComapnyID, Ap_ProductionJobCardHeader.ProCardID, Ap_ProductionJobCardHeader.DocumentTypeID, SET_DocumentType.DocumentType, 
    Ap_ProductionJobCardHeader.DocumentNo, Ap_ProductionJobCardHeader.DocumentDate, Ap_ProductionJobCardHeader.DepartmentID, SET_Department.DepartmentName, 
    Ap_ProductionJobCardHeader.BodyNo, Ap_ProductionJobCardHeader.ChassisNo, Ap_ProductionJobCardHeader.EngineNo, Ap_ProductionJobCardHeader.ColorID, 
    Ap_ProductionJobCardHeader.CategoryID, Ap_ProductionJobCardHeader.ItemID, Ap_ProductionJobCardHeader.StatusCode, Ap_ProductionJobCardHeader.Shift, 
    AP_ProductionJobCardHeader.CreatedBy, Ap_ProductionJobCardHeader.CreateDate, Ap_ProductionJobCardHeader.CreatedByValue, Ap_ProductionJobCardHeader.UpdatedBy, 
    Ap_ProductionJobCardHeader.UpdateDate, Ap_ProductionJobCardHeader.UpdatedByValue, 
    ROW_NUMBER() Over (Order by Ap_ProductionJobCardHeader.ComapnyID, ProCardID) as 'Row' 
FROM Ap_ProductionJobCardHeader 
    inner join SET_Department on SET_Department.DepartmentID = Ap_ProductionJobCardHeader.DepartmentID 
    inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = Ap_ProductionJobCardHeader.DocumentTypeID 
        and SET_DocumentType.CompanyID = Ap_ProductionJobCardHeader.ComapnyID ";

                    #endregion

                    sqlparamsList.Add(new SqlParameter("@PageIndex", PageIndex));
                    sqlparamsList.Add(new SqlParameter("@PageSize", PageSize));
                    sqlparamsList.Add(new SqlParameter("@TotalRows", ReturnTotalRows));

                    SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                    sqlparamTotalRows.Direction = ParameterDirection.Output;

                    SqlParameter[] sqlparams = sqlparamsList.ToArray();
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAllGird_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAllGird_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public bool UpdateHeaderAndDetailRecipe(AP_ProductionJobCardHeader rh, List<Ap_ProductionjobcardDetail> colSPDetAdd, List<Ap_ProductionjobcardDetail> colTranDetUpdate, List<Ap_ProductionjobcardDetail> colSPDetDelete, List<AP_ProductionCardStoppageDetail> colStoppageDetAdd, List<AP_ProductionCardStoppageDetail> colStoppageupdate, List<AP_ProductionCardStoppageDetail> colStoppageDelete, SqlTransaction sqlTrans = null)
        {

            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("Update Recipe Parts");
            try
            {
                result = rh.UpdateDataByKey(sqlTrans);

                short recipeDetailID = short.Parse(Ap_ProductionjobcardDetail.GetNewProCardDetailID().ToString());
                short StoppageDetailID = short.Parse(AP_ProductionCardStoppageDetail.GetNewStoppageDetailID().ToString());

                foreach (Ap_ProductionjobcardDetail ard in colSPDetAdd)
                {
                    ard.ProCardDetailID = recipeDetailID++;
                    ard.ProCardID = ard.ProCardID;
                    result = ard.Insert(sqlTrans);
                }

                foreach (Ap_ProductionjobcardDetail ardUpate in colTranDetUpdate)
                    ardUpate.UpdateDataByProDetailID(sqlTrans);
                    

                foreach (Ap_ProductionjobcardDetail ardDelete in colSPDetDelete)
                    Ap_ProductionjobcardDetail.DeleteDataByKey(sqlTrans, ardDelete.CompanyID, ardDelete.ProCardDetailID);


                // stoppage Detail
                foreach (AP_ProductionCardStoppageDetail SD in colStoppageDetAdd)
                {
                    SD.ProCardID = this.ProCardID;
                    SD.StoppageDetailID = StoppageDetailID++;
                    result = SD.Insert(sqlTrans);
                }

                foreach (AP_ProductionCardStoppageDetail sdUpate in colStoppageupdate)
                    sdUpate.UpdateDataByKey();


                foreach (AP_ProductionCardStoppageDetail STPDelete in colStoppageDelete)
                    AP_ProductionCardStoppageDetail.DeleteDataByKey(sqlTrans, STPDelete.CompanyID, STPDelete.StoppageDetailID);

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
                    SqlHelper.RollBackTransaction(sqltrans, "Update Recipe Parts");
            }
            return result;
        }

        #endregion

        #region Methods for reports

        public DataSet GetJobCard(int companyID, int docTypeID, int? DocumentNo, Nullable<DateTime> Date_From = null, Nullable<DateTime> Date_To = null)
        {
            DataSet dtDTR = null;

            #region Query

            string Query_GetJobCard = @" 
select ap_productionJobCardHeader.ProCardID, ap_productionJobCardHeader.DocumentNo as JobCardNo, ap_productionJobCardHeader.BodyNo, 
    ap_productionJobCardHeader.ChassisNo, ap_productionJobCardHeader.EngineNo, ap_productionJobCardHeader.Shift, 
    ap_productionJobCardHeader.DocumentDate as ProductionDate, INV_Item.Description as Product, SET_Department.DepartmentName, 
    ----------  Detail  ----------
    ap_productionJobCardDetail.MainProcessID, ap_productionJobCardDetail.ProCardDetailID, AS_MainProcess.MainProcessName, 
    ap_productionJobCardDetail.StartDate as PRo_StartDateTime, ap_productionJobCardDetail.EndDate as PRo_EndDateTime, ap_productionJobCardDetail.StoppageID, 
    SET_Color.ColorName 
from ap_productionJobCardHeader 
    inner join ap_productionJobCardDetail on ap_productionJobCardDetail.ProCardID = ap_productionJobCardHeader.ProCardID 
        and ap_productionJobCardDetail.CompanyID = ap_productionJobCardHeader.ComapnyID 
    inner join AS_MainProcess on AS_MainProcess.MainProcessID = ap_productionJobCardDetail.MainProcessID 
        and AS_MainProcess.CompanyID = ap_productionJobCardDetail.CompanyID 
    inner join INV_Item on INV_Item.ItemID = ap_productionJobCardHeader.ItemID --and INV_Item.CompanyID = ap_productionJobCardHeader.ComapnyID 
    inner join SET_Department on SET_Department.DepartmentID = ap_productionJobCardHeader.DepartmentID and SET_Department.IsActive = 1 
    left join SET_Color on SET_Color.ColorID = ap_productionJobCardHeader.ColorID 
where ap_productionJobCardHeader.ComapnyID = @CompanyID 
    {0}

----------  Stoppage Detail ----------
select DISTINCT AP_ProductionCardStoppageDetail.ProCardID, AS_SubProcess.SubProcessName, AP_StoppageReason.Reason, AP_ProductionCardStoppageDetail.StoppageID, 
    AP_ProductionCardStoppageDetail.StoppageFrom, AP_ProductionCardStoppageDetail.StoppageTo, AP_ProductionCardStoppageDetail.Remarks, 
    AP_ProductionCardStoppageDetail.StoppageDetailID 
from ap_productionJobCardHeader 
    inner join ap_productionJobCardDetail on ap_productionJobCardDetail.ProCardID = ap_productionJobCardHeader.ProCardID 
        and ap_productionJobCardDetail.CompanyID = ap_productionJobCardHeader.ComapnyID 
    inner join AS_MainProcess on AS_MainProcess.MainProcessID = ap_productionJobCardDetail.MainProcessID 
        and AS_MainProcess.CompanyID = ap_productionJobCardDetail.CompanyID 
    inner join AP_ProductionCardStoppageDetail on AP_ProductionCardStoppageDetail.ProCardID = ap_productionJobCardDetail.ProCardID 
        and ap_productionJobCardDetail.ProCardID = ap_productionJobCardHeader.ProCardID 
        and ap_productionJobCardDetail.StoppageID = ap_productionJobCardDetail.StoppageID 
        and ap_productionJobCardDetail.CompanyID = ap_productionJobCardHeader.ComapnyID 
    inner join AS_SubProcess on AS_SubProcess.SubProcessID = AP_ProductionCardStoppageDetail.SubProcessID 
        and AS_SubProcess.CompanyID = AP_ProductionCardStoppageDetail.CompanyID 
    inner join AP_StoppageReason on AP_StoppageReason.ReasonID = AP_ProductionCardStoppageDetail.ReasonID 
        and AP_StoppageReason.CompanyID = AP_ProductionCardStoppageDetail.CompanyID 
where ap_productionJobCardHeader.ComapnyID = @CompanyID 
    {0} ";

            #endregion

            try
            {
                string filter = string.Empty;

                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@DocTypeID", docTypeID);
                sqlparams[2] = new SqlParameter("@DocumentNo", DocumentNo);


                if (DocumentNo != null)
                {
                    filter = "and ap_productionJobCardHeader.DocumentNo = " + DocumentNo;
                }

                if (Date_From != null && Date_To != null)
                    filter += filter + (" and CAST(CONVERT(VARCHAR(10), AP_ProductionJobCardHeader.DocumentDate, 110) AS DATETIME) between '" + ((DateTime)Date_From).ToString("MM-dd-yyyy") + "' and '" + ((DateTime)Date_To).ToString("MM-dd-yyyy") + "'");
                else if (Date_From != null)
                    filter += filter + (" and CAST(CONVERT(VARCHAR(10), AP_ProductionJobCardHeader.DocumentDate, 110) AS DATETIME) >= '" + ((DateTime)Date_From).ToString("MM-dd-yyyy") + "'");
                else if (Date_To != null)
                    filter += filter + (" and CAST(CONVERT(VARCHAR(10), AP_ProductionJobCardHeader.DocumentDate, 110) AS DATETIME) <= '" + ((DateTime)Date_To).ToString("MM-dd-yyyy") + "'");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetJobCard,filter), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtDTR = ds;

            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }
            return dtDTR;
        }


        public DataTable GetJobCardVehicleStatus(int companyID, int docTypeID, int? DocumentNo, Nullable<DateTime> Date_From = null, Nullable<DateTime> Date_To = null)
        {
            DataTable dtDTR = null;

            #region Query

            string Query_GetJobCardVehicleStatus = @" 
select ap_productionJobCardHeader.ProCardID, ap_productionJobCardHeader.BodyNo as ProductNo, inv_Item.Description as Product, AS_MainProcess.MainProcessName, 
    case when max(ap_productionJobCardDetail.EndDate) is null then 'In-Process' when max(Ap_ProductionjobcardDetail.EndDate) is not null then 'Complete' end 
        as [Status], 
    count(ap_productionJobCardHeader.ProCardID) as [Count] 
from ap_productionJobCardHeader 
    inner join ap_productionJobCardDetail on ap_productionJobCardDetail.ProCardID = ap_productionJobCardHeader.ProCardID 
        and ap_productionJobCardDetail.CompanyID = ap_productionJobCardHeader.ComapnyID 
    inner join AS_MainProcess on AS_MainProcess.MainProcessID = ap_productionJobCardDetail.MainProcessID 
        and AS_MainProcess.CompanyID = ap_productionJobCardDetail.CompanyID 
    inner join inv_Item on inv_Item.itemID = ap_productionJobCardHeader.itemID --and inv_Item.CompanyID = ap_productionJobCardHeader.ComapnyID 
where ap_productionJobCardHeader.ComapnyID = @CompanyID 
    {0} 
group by ap_productionJobCardHeader.ProCardID, ap_productionJobCardHeader.BodyNo, inv_Item.Description, AS_MainProcess.MainProcessName ";

            #endregion

            try
            {
                string filter = string.Empty;

                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@DocTypeID", docTypeID);
                sqlparams[2] = new SqlParameter("@DocumentNo", DocumentNo);

                if (DocumentNo != null)
                    filter = "and ap_productionJobCardHeader.DocumentNo = " + DocumentNo;

                if (Date_From != null && Date_To != null)
                {
                    filter += filter + (" and CAST(CONVERT(VARCHAR(10), AP_ProductionJobCardHeader.DocumentDate, 110) AS DATETIME) between '" 
                        + ((DateTime)Date_From).ToString("MM-dd-yyyy") + "' and '" + ((DateTime)Date_To).ToString("MM-dd-yyyy") + "'");
                }
                else if (Date_From != null)
                {
                    filter += filter + (" and CAST(CONVERT(VARCHAR(10), AP_ProductionJobCardHeader.DocumentDate, 110) AS DATETIME) >= '" 
                        + ((DateTime)Date_From).ToString("MM-dd-yyyy") + "'");
                }
                else if (Date_To != null)
                {
                    filter += filter + (" and CAST(CONVERT(VARCHAR(10), AP_ProductionJobCardHeader.DocumentDate, 110) AS DATETIME) <= '" 
                        + ((DateTime)Date_To).ToString("MM-dd-yyyy") + "'");
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetJobCardVehicleStatus, filter), sqlparams);
                
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dtDTR = ds.Tables[0];
            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return dtDTR;
        }

        
        #endregion
    }
}
