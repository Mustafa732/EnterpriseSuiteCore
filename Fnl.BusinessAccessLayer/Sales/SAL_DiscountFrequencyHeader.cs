using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;

using Common.Utils;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Sales
{
    public partial class SAL_DiscountFrequencyHeader
    {
        #region methods

        public DataTable Get_DataByHeader(string headerName)
        {
            DataTable dtToReturn = null;

            #region Query

            string Query_GetData_byHeaderName = @" 
SELECT Distinct '' AS Action, SAL_DiscountFrequencyHeader.CompanyID, SAL_DiscountFrequencyHeader.PeriodID, SAL_DiscountFrequencyHeader.DiscountFrequencyID, 
    SAL_DiscountFrequencyHeader.DiscountOnCustomerType, SAL_DiscountFrequencyHeader.CustomerTypeID, AR_SET_BusinessPartner.BusinessPartnerName AS CustomerTypeName, 
    SAL_DiscountFrequencyHeader.DiscountOnCustomer, SAL_DiscountFrequencyHeader.CustomerID, AR_SET_Customer.CustomerName,SAL_DiscountFrequencyHeader.CommissionForDate ,
    SAL_DiscountFrequencyHeader.DiscountOnDealer, SAL_DiscountFrequencyHeader.DealerID, DealerName.CustomerName AS DealerName, SAL_DiscountFrequencyHeader.DiscountOnLocation, SAL_DiscountFrequencyHeader.[LocationID],SET_Location.LocationName AS LocationName,SAL_DiscountFrequencyHeader.[DiscountOnItemCategory],SAL_DiscountFrequencyHeader.[ItemcategoryID],INV_SET_Category.CategoryName AS ItemcategoryName,SAL_DiscountFrequencyHeader.[DiscountOnItem],SAL_DiscountFrequencyHeader.[ItemID],INV_Item.Description As ItemName,SAL_DiscountFrequencyHeader.[DiscountOnSalesPerson],SAL_DiscountFrequencyHeader.[SalesPersonID]
,SalePerson.CustomerName AS SalesPersonName,SAL_DiscountFrequencyHeader.[DiscountOnTotalAmount],SAL_DiscountFrequencyHeader.[TotalAmount],SAL_DiscountFrequencyHeader.DiscountOnQuantity,SAL_DiscountFrequencyHeader.[TotalQuantity],SAL_DiscountFrequencyHeader.[Period],SAL_DiscountFrequencyHeader.[Frequency] AS [FrequencyID],
case SAL_DiscountFrequencyHeader.[Frequency] when '1' then 'instant' when '2' then 'Invoicewise' when '3' then 'DayWise' when '4' then 'Weekly' when '5' then 'Monthly' when '6' then 'yearly' else '' end as FrequencyName
,SAL_DiscountFrequencyHeader.CommissionToDate
,SAL_DiscountFrequencyHeader.[Cumulative],SAL_DiscountFrequencyHeader.[DiscountAmount],SAL_DiscountFrequencyHeader.[DiscountPercentage],SAL_DiscountFrequencyHeader.[GLCode],SAL_DiscountFrequencyHeader.[HeaderName]
,SAL_DiscountFrequencyHeader.[Status],SAL_DiscountFrequencyHeader.[CreateDate],SAL_DiscountFrequencyHeader.[CreatedBy],SAL_DiscountFrequencyHeader.[CreatedByValue],SAL_DiscountFrequencyHeader.[UpdateDate],SAL_DiscountFrequencyHeader.[UpdatedBy],SAL_DiscountFrequencyHeader.[UpdatedByValue]
                                 FROM [SAL_DiscountFrequencyHeader]
                                 left join AR_SET_BusinessPartner on AR_SET_BusinessPartner.BusinessPartnerID=SAL_DiscountFrequencyHeader.CustomerTypeID 
                                 left join AR_SET_BusinessPartnerTypes on AR_SET_BusinessPartnerTypes.BusinessPartnerID=SAL_DiscountFrequencyHeader.[SalesPersonID] 
                                 left join INV_Item on INV_Item.ItemID = SAL_DiscountFrequencyHeader.ItemID
                                 left join INV_SET_Category on INV_SET_Category.CategoryID=SAL_DiscountFrequencyHeader.ItemcategoryID
--inner join AR_SET_BusinessPartnerTypes on AR_SET_BusinessPartnerTypes.BusinessPartnerID=SAL_DiscountFrequencyHeader.CustomerID 
                                left join AR_SET_Customer on AR_SET_Customer.CustomerID = SAL_DiscountFrequencyHeader.CustomerID
                                and AR_SET_Customer.CompanyID =  SAL_DiscountFrequencyHeader.CompanyID
                                left join AR_SET_Customer SalePerson on SalePerson.CustomerID = SAL_DiscountFrequencyHeader.SalesPersonID
                                left join AR_SET_Customer DealerName on DealerName.CustomerID = SAL_DiscountFrequencyHeader.DealerID
                                and DealerName.CompanyID =  SAL_DiscountFrequencyHeader.CompanyID
                                left join SET_Location on SET_Location.LocationID=SAL_DiscountFrequencyHeader.LocationID
                                Where [SAL_DiscountFrequencyHeader].Status=1 AND [SAL_DiscountFrequencyHeader].HeaderName  = @HeaderName";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@HeaderName", headerName);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byHeaderName, sqlparams);

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

        public DataTable GetAllHeaderName(string headerName)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@HeaderName", headerName);

                string Query_GetHeaderNames = @"SELECT HeaderName from SAL_DiscountFrequencyHeader where HeaderName = @HeaderName";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetHeaderNames, sqlparams);

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

        public DataTable GetAllDataByDiscountFrequencyID(int frequencyID, string headername)
        {
            DataTable dtToReturn = null;

            string GetDataByDiscountFrequencyID = @"SELECT  [CompanyID],[PeriodID],[DiscountFrequencyID],[DiscountOnCustomerType] ,[CustomerTypeID],[DiscountOnCustomer],[CustomerID]
,[DiscountOnDealer],[DealerID],[DiscountOnLocation],[LocationID],[DiscountOnItemCategory],[ItemcategoryID],[DiscountOnItem],[ItemID]
,[DiscountOnSalesPerson],[SalesPersonID],[DiscountOnTotalAmount],[TotalAmount],[DiscountOnQuantity],[TotalQuantity],[Period],[Frequency],[Cumulative],[DiscountAmount]
,[DiscountPercentage],[GLCode],[Status],[CreateDate],[CreatedBy],[CreatedByValue],[UpdateDate],[UpdatedBy]
,[UpdatedByValue],[HeaderName]
  FROM [SAL_DiscountFrequencyHeader] where DiscountFrequencyID = @FrequencyID AND HeaderName = @HeaderName";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@FrequencyID", frequencyID);
                sqlparams[1] = new SqlParameter("@HeaderName", headername);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, GetDataByDiscountFrequencyID, sqlparams);

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

        public DataTable GetAllDataByDiscountFrequency()
        {
            DataTable dtToReturn = null;

            string GetDataByDiscountFrequencyID = @" SELECT  *  FROM [SAL_DiscountFrequencyHeader] ";
            try
            {
                
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, GetDataByDiscountFrequencyID);

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

        public DataTable GetAllHeaderNameByFrequency(int FrequencyID)
        {
            DataTable dtToReturn = null;

            string Query_GetHeaderNameByFrequency = @"select -1 as Frequency,-1 as DiscountFrequencyID,'select' as HeaderName
union all
SELECT distinct Frequency,DiscountFrequencyID, HeaderName 
from SAL_DiscountFrequencyHeader where Frequency = @FrequencyID";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@FrequencyID", FrequencyID);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetHeaderNameByFrequency, sqlparams);

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

        public DataTable GetAllInvoiceData(int companyID, int PeriodID, int? customerID, int? ItemID, int? CustomerTypeID, int? ItemCategoryID, 
            StringBuilder where = null, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
		{
			DataTable dt = null;

            #region Query

            string Query_GetInvoiceData = @" 
SELECT distinct InvoiceID, InvoiceNumber as SalesInvoice, SAL_InvoiceHeader.CustomerID, AmountPaid as InvoiceAmount, AR_SET_Customer.CustomerName, 
    0 as CustomerTypeID, sum(SAL_InvoiceDetail.QuantityInvoiced) as QuantityInvoiced 
FROM SAL_InvoiceHeader 
    inner join SAL_InvoiceDetail on SAL_InvoiceDetail.InvoiceHeaderID = SAL_InvoiceHeader.InvoiceID 
    inner join AR_SET_Customer on AR_SET_Customer.CustomerID=SAL_InvoiceHeader.CustomerID   
     and AR_SET_Customer.CompanyID = SAL_InvoiceHeader.CompanyID
     and AR_SET_Customer.PeriodID = SAL_InvoiceHeader.PeriodID
     
     inner join AR_SET_BusinessPartnerTypes on AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID
   and AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID
     and AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
     
     inner join AR_SET_BusinessPartner on AR_SET_BusinessPartner.BusinessPartnerID =  AR_SET_BusinessPartnerTypes.TypeCode

WHERE SAL_InvoiceHeader.StatusCode = 1 AND (SAL_InvoiceHeader.CustomerID = @CustomerID OR @CustomerID is null) AND (SAL_InvoiceDetail.ItemID = @ItemID OR @ItemID is null) AND (SAL_InvoiceDetail.ItemCategoryID = @ItemCategoryID OR @ItemCategoryID is null) 
--And SAL_InvoiceHeader.PaymentStatusType is null 
AND SAL_InvoiceHeader.PeriodID = @PeriodID
    AND SAL_InvoiceHeader.CompanyID = @CompanyID and SAL_InvoiceHeader.Status = 1 
{0}
group by SAL_InvoiceHeader.InvoiceID,SAL_InvoiceHeader.InvoiceNumber
 ,SAL_InvoiceHeader.CustomerID,SAL_InvoiceHeader.AmountPaid,AR_SET_Customer.CustomerName
";

            #endregion

            try
			{
				SqlParameter[] sqlparams = new SqlParameter[6];
				sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@CustomerID", customerID);
                sqlparams[3] = new SqlParameter("@CustomerTypeID", CustomerTypeID);
                sqlparams[4] = new SqlParameter("@ItemCategoryID", ItemCategoryID);
                sqlparams[5] = new SqlParameter("@ItemID", ItemID);
                 

				DataSet ds = null;

				if(objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_GetInvoiceData, where), sqlparams);
				else if(objConnection != null)
					ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetInvoiceData,where), sqlparams);
				else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetInvoiceData, where), sqlparams);

				if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
					dt = ds.Tables[0];
			}
			catch (Exception ex)
			{
				SqlHelper.LogException(ex);
				throw ex;
			}

			return dt;
		}

        private bool DeleteDataByHeaderID(SqlTransaction objTransaction, int DiscountFrequencyID)
        {
            bool result = false;
            string Query_DeleteData_ByKey = @"delete from SAL_DiscountFrequencyHeader where DiscountFrequencyID = @DiscountFrequencyID";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@DiscountFrequencyID", DiscountFrequencyID);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteData_ByKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DeleteData_ByKey, sqlparams) > 0)
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

        public bool InsertDiscountFrequencyHeader(List<SAL_DiscountFrequencyHeader> lstDisFre)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertDiscountFrequencyHeader");

            try
            {
                foreach (SAL_DiscountFrequencyHeader obj_dfh in lstDisFre)
                {
                    obj_dfh.DiscountFrequencyID = GetNewDiscountFrequencyID(obj_dfh.f_CompanyID, sqltrans);
                    obj_dfh.Insert(sqltrans);


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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertDiscountFrequencyHeader");
            }

            return result;
        }

        public bool UpdateDiscountFrequencyHeader(List<SAL_DiscountFrequencyHeader> lst_DisFreAdd, List<SAL_DiscountFrequencyHeader> lst_DisFreUpdate,
                                                  List<SAL_DiscountFrequencyHeader> lst_DisFreDelete)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateDiscountFrequencyHeader");

            try
            {
                foreach (SAL_DiscountFrequencyHeader obj_sdf in lst_DisFreAdd)
                {
                    obj_sdf.DiscountFrequencyID = GetNewDiscountFrequencyID(obj_sdf.f_CompanyID, sqltrans);
                    obj_sdf.Insert(sqltrans);
                }

                foreach (SAL_DiscountFrequencyHeader obj_sdf in lst_DisFreUpdate)
                {
                    obj_sdf.UpdateDataByKey(sqltrans);

                }

                foreach (SAL_DiscountFrequencyHeader obj_sdf in lst_DisFreDelete)
                {
                    SAL_DiscountFrequencyHeader.DeleteDataByKey(sqltrans, Guid.Parse(obj_sdf.f_UpdatedBy.ToString()), DateTime.Parse(obj_sdf.f_UpdateDate.ToString()), obj_sdf.f_UpdatedByValue, obj_sdf.CompanyID, obj_sdf.DiscountFrequencyID);
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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateDiscountFrequencyHeader");
            }

            return result;

        }

        public bool DeleteDiscountFrequencyHeader(List<SAL_DiscountFrequencyHeader> lst_disfre_delete)
        {
            bool result = false;

            SqlTransaction sqltrans = SqlHelper.GetTransaction("DelDiscountFrequencyHeader");

            try
            {

                foreach (SAL_DiscountFrequencyHeader obj_disfre in lst_disfre_delete)
                    obj_disfre.DeleteDataByHeaderID(sqltrans, obj_disfre.DiscountFrequencyID);

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
                    SqlHelper.RollBackTransaction(sqltrans, "DelDiscountFrequencyHeader");
            }

            return result;
        }

        #endregion
    }
}
