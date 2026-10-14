using Common.Utils;
using DAL.DataAccessLayer;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class Set_CustomerCategoryWiseTax
    {

        const string Query_GetData_byCustomerCategoryID = @" SELECT * FROM [Set_CustomerCategoryWiseTax] WHERE [CustomerCategoryWiseTaxID] = @CustomerCategoryWiseTaxID  ";


        public bool InsertCustomerCategoryTaxHeaderAndDetail(List<Set_CustomerCategoryWiseTaxDetail> colCCDetail, SqlTransaction sql = null)
        {
            SqlTransaction sqltrans;
            bool result = false;
            if (sql == null)
            {
                sqltrans = SqlHelper.GetTransaction("InsertCustomerCategoryWiseTax");
            }
            else
            {
                sqltrans = sql;
            }
            try
            {



                //this.ContractNo = SET_DocumentSequence.ManualDocumentSequence(f_SaleContractTypeID, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID).ToString();
                this.Insert(sqltrans);
                long tempOrderDetailID = long.Parse(Set_CustomerCategoryWiseTaxDetail.GetNewCategoryTaxDetailID(this.CustomerCategoryWiseTaxID, CompanyID, PeriodID, sqltrans).ToString());
                 

                foreach (Set_CustomerCategoryWiseTaxDetail ccd in colCCDetail)
                {
                    ccd.CategoryTaxID = this.CustomerCategoryWiseTaxID;
                    ccd.CategoryTaxDetailID = tempOrderDetailID;
                    ccd.Insert(sqltrans);
                    tempOrderDetailID++;
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
                    if (sql == null)
                    {
                        SqlHelper.CommitTransaction(sqltrans);
                    }
                    else
                    { }
                }
                else
                {
                    if (sql == null)
                    {
                        SqlHelper.RollBackTransaction(sqltrans, "InsertCustomerCategoryWiseTax");
                    }
                    else
                    { }
                }
            }

            return result;
        }


        public bool UpdateCustomerCategoryTaxHeaderAndDetail(List<Set_CustomerCategoryWiseTaxDetail> colAddCCTaxDetail, List<Set_CustomerCategoryWiseTaxDetail> colUpdateCCTaxDetail, List<Set_CustomerCategoryWiseTaxDetail> colDeleteCCTaxDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateCustomerCategoryTax");

            try
            {
                this.UpdateDataByKey(sqltrans);

                long tempCustomerDetailId = short.Parse(Set_CustomerCategoryWiseTaxDetail.GetNewCategoryTaxDetailID(this.CustomerCategoryWiseTaxID,CompanyID, PeriodID, sqltrans).ToString());
                foreach (Set_CustomerCategoryWiseTaxDetail cctdAdd in colAddCCTaxDetail)
                {
                    cctdAdd.CategoryTaxID = this.CustomerCategoryWiseTaxID;
                    cctdAdd.CategoryTaxDetailID = tempCustomerDetailId;
                    cctdAdd.Insert(sqltrans);
                   tempCustomerDetailId++;
                }

                foreach (Set_CustomerCategoryWiseTaxDetail cctdUpdate in colUpdateCCTaxDetail)
                {
                    cctdUpdate.UpdateDataByKey(sqltrans);
                }

                foreach (Set_CustomerCategoryWiseTaxDetail cctdDelete in colDeleteCCTaxDetail)
                    Set_CustomerCategoryWiseTaxDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, cctdDelete.CategoryTaxDetailID, this.CustomerCategoryWiseTaxID, f_CompanyID, f_PeriodID);


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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateCustomerCategoryTax");
            }

            return result;
        }

        public static DataTable GetDataByHeaderID(int categoryTaxID, int companyID, int periodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            const string Query_GetData_byHeaderID = @"
SELECT CategoryTaxDetailID as SerialNo ,CategoryTaxID,CategoryTaxDetailID,ItemCatalogID,CatalogDescription,[Set_CustomerCategoryWiseTaxDetail].ItemID,INV_Item.Description as  ItemDescription,WithHoldingTax,FurtherTax,SalesTax,SalesTaxUnRegister,WithHoldingTaxUnRegister ,FEDRateRegister ,FEDRateUnRegister, EffectiveDateFrom
	,EffectiveDateTo,'' as Action,CategoryTaxDetailID as SerialNo 
	FROM [Set_CustomerCategoryWiseTaxDetail] 
	inner join INV_Item on INV_Item.ItemID = [Set_CustomerCategoryWiseTaxDetail].ItemID
	WHERE [Set_CustomerCategoryWiseTaxDetail].Status=1 And [Set_CustomerCategoryWiseTaxDetail].[CompanyID] = @CompanyID and [Set_CustomerCategoryWiseTaxDetail].[CategoryTaxID] = @CategoryTaxID and [Set_CustomerCategoryWiseTaxDetail].[PeriodID] = @PeriodID

";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CategoryTaxID", categoryTaxID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byHeaderID, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byHeaderID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byHeaderID, sqlparams);

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

        public static DataRow GetDataByCustomerCategoryIDandItemID(int categoryTaxID,int itemID, int companyID, int periodID, DateTime SaleOrderDate, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;
            const string Query_GetData_byCustomerCategoryanditemID = @"Select Set_CustomerCategoryWiseTaxDetail.* from Set_CustomerCategoryWiseTax
	Inner Join  Set_CustomerCategoryWiseTaxDetail on Set_CustomerCategoryWiseTaxDetail.CategoryTaxID= Set_CustomerCategoryWiseTax.CustomerCategoryWiseTaxID
	AND Set_CustomerCategoryWiseTaxDetail.CompanyID = Set_CustomerCategoryWiseTax.CompanyID
	And Set_CustomerCategoryWiseTaxDetail.PeriodID = Set_CustomerCategoryWiseTax.PeriodID
	And Set_CustomerCategoryWiseTaxDetail.Status = 1
	Where Set_CustomerCategoryWiseTax.Status = 1
	AND Set_CustomerCategoryWiseTax.CustomerCategoryWiseTaxID= @CustomerCategoryWiseTaxID
	--And Set_CustomerCategoryWiseTax.CompanyID = @CompanyID
	And Set_CustomerCategoryWiseTax.PeriodID = @PeriodID
	And Set_CustomerCategoryWiseTaxDetail.ItemID = @ItemID
	ANd @orderDate Between Set_CustomerCategoryWiseTaxDetail.EffectiveDateFrom And Set_CustomerCategoryWiseTaxDetail.EffectiveDateTo

";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CustomerCategoryWiseTaxID", categoryTaxID);
                sqlparams[1] = new SqlParameter("@ItemID", itemID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);
                sqlparams[4] = new SqlParameter("@orderDate", SaleOrderDate);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byCustomerCategoryanditemID, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byCustomerCategoryanditemID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byCustomerCategoryanditemID, sqlparams);

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

        public static DataRow GetDataByCustomerCategoryID(int customerCategoryWiseTaxID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CustomerCategoryWiseTaxID", customerCategoryWiseTaxID);
                

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byCustomerCategoryID, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byCustomerCategoryID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byCustomerCategoryID, sqlparams);

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

        public static DataRow GetDataByName(int companyID, string customerCategory, int periodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@customerCategory", customerCategory);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                string Query = @"SELECT * FROM [Set_CustomerCategoryWiseTax] WHERE [CompanyID] = @CompanyID and 
            CustomerCategory = @customerCategory and [PeriodID] = @PeriodID";

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);

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

    }
}
