using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.GL.Transaction;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;

namespace FnI.BusinessAccessLayer.AR.Transaction
{
   public partial class AR_SalesReceipt
   {

       #region Query
       const string Query_SelectAllForSearch_WithPaging = @" Select AR_SalesReceiptHeader.REceiptHeaderID, AR_SalesReceiptHeader.PaymentDate, AR_SalesReceiptHeader.CreatedBy, AR_SalesReceiptHeader.CreateDate,
        AR_SalesReceiptHeader.CreatedByValue, AR_SalesReceiptHeader.UpdatedBy, AR_SalesReceiptHeader.UpdatedByValue, AR_SalesReceiptHeader.CustomerID, AR_SET_Customer.CustomerName
        , ROW_NUMBER() Over (Order by AR_SET_Customer.CustomerName) as 'Row'
        From AR_SalesReceiptHeader
        Inner Join AR_SET_Customer  on AR_SET_Customer.CustomerID = AR_SalesReceiptHeader.CustomerID
        And AR_SET_Customer.CompanyID = AR_SalesReceiptHeader.CompanyID
        And AR_SET_Customer.PeriodID = AR_SalesReceiptHeader.PeriodID
        Where AR_SalesReceiptHeader.CompanyID = @CompanyID 
        And AR_SalesReceiptHeader.PeriodID = @PeriodID";

       const string Query_SelectAllForSearch_WithoutPaging = @" Select AR_SalesReceiptHeader.REceiptHeaderID, AR_SalesReceiptHeader.PaymentDate, AR_SalesReceiptHeader.CreatedBy, AR_SalesReceiptHeader.CreateDate,
        AR_SalesReceiptHeader.CreatedByValue, AR_SalesReceiptHeader.UpdatedBy, AR_SalesReceiptHeader.UpdatedByValue, AR_SalesReceiptHeader.CustomerID, AR_SET_Customer.CustomerName
        From AR_SalesReceiptHeader
        Inner Join AR_SET_Customer  on AR_SET_Customer.CustomerID = AR_SalesReceiptHeader.CustomerID
        And AR_SET_Customer.CompanyID = AR_SalesReceiptHeader.CompanyID
        And AR_SET_Customer.PeriodID = AR_SalesReceiptHeader.PeriodID
        Where AR_SalesReceiptHeader.CompanyID = @CompanyID 
        And AR_SalesReceiptHeader.PeriodID = @PeriodID";

       const string Query_GetData_ByKey = @"
        SELECT AR_SalesReceiptHeader.ReceiptHeaderID,  AR_SalesReceiptHeader.CompanyID,     AR_SalesReceipt.SaleReceiptID, AR_SalesReceipt.PaymentBankID,AR_SET_BankBranch.BankBranchName, AR_SalesReceipt.BankBranchID, AR_SalesReceipt.BankAccountID,AR_SET_BankAccount.BankAccountNumber,AR_SET_BankAccount.BankAccountName
        , AR_SalesReceiptHeader.PaymentDate, AR_SalesReceiptHeader.CustomerID,
        AR_SET_Customer.CustomerName, AR_SalesReceipt.ChequeNo, AR_SalesReceipt.Amount, AR_SalesReceipt.Charges, '' as Action
        FROM            AR_SalesReceiptHeader

        Inner Join AR_SalesReceipt  on AR_SalesReceipt.receiptheaderid = AR_SalesReceiptHeader.receiptheaderid
        And AR_SalesReceipt.CompanyID = AR_SalesReceiptHeader.CompanyID
        And AR_SalesReceipt.PeriodID = AR_SalesReceiptHeader.PeriodID


        Inner Join AR_SET_Customer  on AR_SET_Customer.CustomerID = AR_SalesReceiptHeader.CustomerID
        And AR_SET_Customer.CompanyID = AR_SalesReceiptHeader.CompanyID
        And AR_SET_Customer.PeriodID = AR_SalesReceiptHeader.PeriodID

        Inner Join AR_SET_BankBranch  on AR_SET_BankBranch.BankBranchID = AR_SalesReceipt.PaymentBankID

        Inner Join AR_SET_BankAccount  on AR_SET_BankAccount.BankBranchID = AR_SalesReceipt.PaymentBankID

        Where AR_SalesReceipt.CompanyID = @CompanyID
        And AR_SalesReceipt.PeriodID = @PeriodID
        And AR_SalesReceiptHeader.ReceiptHeaderID = @ReceiptHeaderID
        ";

       const string Query_GetData_ByCustomerId = @"
        SELECT AR_SalesReceiptHeader.ReceiptHeaderID,  AR_SalesReceiptHeader.CompanyID,     AR_SalesReceipt.SaleReceiptID, AR_SalesReceipt.PaymentBankID,AR_SET_BankBranch.BankBranchName, AR_SalesReceipt.BankBranchID, AR_SalesReceipt.BankAccountID,AR_SET_BankAccount.BankAccountNumber,AR_SET_BankAccount.BankAccountName
        , AR_SalesReceiptHeader.PaymentDate, AR_SalesReceiptHeader.CustomerID,
        AR_SET_Customer.CustomerName, AR_SalesReceipt.ChequeNo, AR_SalesReceipt.Amount, AR_SalesReceipt.Charges,AR_SalesReceiptHeader.DocumentNo,
		AR_SalesReceiptHeader.DocumentTypeID, SET_DocumentType.DocumentType,
		convert(bit,0) as IsChecked
        FROM            AR_SalesReceiptHeader

        Inner Join AR_SalesReceipt  on AR_SalesReceipt.receiptheaderid = AR_SalesReceiptHeader.receiptheaderid
        And AR_SalesReceipt.CompanyID = AR_SalesReceiptHeader.CompanyID
        And AR_SalesReceipt.PeriodID = AR_SalesReceiptHeader.PeriodID

        Inner Join AR_SET_Customer  on AR_SET_Customer.CustomerID = AR_SalesReceiptHeader.CustomerID
        And AR_SET_Customer.CompanyID = AR_SalesReceiptHeader.CompanyID
        And AR_SET_Customer.PeriodID = AR_SalesReceiptHeader.PeriodID

        Inner Join AR_SET_BankBranch  on AR_SET_BankBranch.BankBranchID = AR_SalesReceipt.PaymentBankID

        Inner Join AR_SET_BankAccount  on AR_SET_BankAccount.BankBranchID = AR_SalesReceipt.PaymentBankID

		inner join SET_DocumentType  on SET_DocumentType.DocumentTypeID = AR_SalesReceiptHeader.DocumentTypeID
		ANd SET_DocumentType.CompanyID = AR_SalesReceiptHeader.CompanyID
		ANd SET_DocumentType.PeriodID = AR_SalesReceiptHeader.PeriodID

        Where AR_SalesReceipt.CompanyID = @CompanyID
        And AR_SalesReceipt.PeriodID = @PeriodID
        And AR_SalesReceiptHeader.CustomerID = @CustomerID
        And AR_SalesReceiptHeader.PaymentDate = @PaymentDate
        ";

       const string Query_GetData_ByCustomerIdForEdit = @"
        SELECT AR_SalesReceiptHeader.ReceiptHeaderID,  AR_SalesReceiptHeader.CompanyID,     AR_SalesReceipt.SaleReceiptID, AR_SalesReceipt.PaymentBankID,AR_SET_BankBranch.BankBranchName, AR_SalesReceipt.BankBranchID, AR_SalesReceipt.BankAccountID,AR_SET_BankAccount.BankAccountNumber,AR_SET_BankAccount.BankAccountName
        , AR_SalesReceiptHeader.PaymentDate, AR_SalesReceiptHeader.CustomerID,
        AR_SET_Customer.CustomerName, AR_SalesReceipt.ChequeNo, AR_SalesReceipt.Amount, AR_SalesReceipt.Charges,AR_SalesReceiptHeader.DocumentNo,
		AR_SalesReceiptHeader.DocumentTypeID, SET_DocumentType.DocumentType,
		Case WHen AR_SalesReceipt.isApproved = 1 Then 
		convert(bit,1) else
		convert(bit,0) end as IsChecked
        FROM            AR_SalesReceiptHeader

        Inner Join AR_SalesReceipt  on AR_SalesReceipt.receiptheaderid = AR_SalesReceiptHeader.receiptheaderid
        And AR_SalesReceipt.CompanyID = AR_SalesReceiptHeader.CompanyID
        And AR_SalesReceipt.PeriodID = AR_SalesReceiptHeader.PeriodID

        Inner Join AR_SET_Customer  on AR_SET_Customer.CustomerID = AR_SalesReceiptHeader.CustomerID
        And AR_SET_Customer.CompanyID = AR_SalesReceiptHeader.CompanyID
        And AR_SET_Customer.PeriodID = AR_SalesReceiptHeader.PeriodID

        Inner Join AR_SET_BankBranch  on AR_SET_BankBranch.BankBranchID = AR_SalesReceipt.PaymentBankID

        Inner Join AR_SET_BankAccount  on AR_SET_BankAccount.BankBranchID = AR_SalesReceipt.PaymentBankID

		inner join SET_DocumentType  on SET_DocumentType.DocumentTypeID = AR_SalesReceiptHeader.DocumentTypeID
		ANd SET_DocumentType.CompanyID = AR_SalesReceiptHeader.CompanyID
		ANd SET_DocumentType.PeriodID = AR_SalesReceiptHeader.PeriodID

        Where AR_SalesReceipt.CompanyID = @CompanyID
        And AR_SalesReceipt.PeriodID = @PeriodID
        And AR_SalesReceipt.isApproved = 0
        And AR_SalesReceiptHeader.CustomerID = @CustomerID
        And AR_SalesReceiptHeader.PaymentDate = @PaymentDate
        ";

       const string Query_GetData_ByHeaderID = @"
      SELECT distinct
--AR_SalesReceipt.SaleReceiptID ,
 GL_TransactionHeader.TransactionID,
AR_SalesReceiptHeader.ReceiptHeaderID,  AR_SalesReceiptHeader.CompanyID,     AR_SalesReceipt.SaleReceiptID, AR_SalesReceipt.PaymentBankID,AR_SET_BankBranch.BankBranchName, AR_SalesReceipt.BankBranchID, AR_SalesReceipt.BankAccountID,AR_SET_BankAccount.BankAccountNumber,AR_SET_BankAccount.BankAccountName
, AR_SalesReceiptHeader.PaymentDate, AR_SalesReceiptHeader.CustomerID,
AR_SET_Customer.CustomerName, AR_SalesReceipt.ChequeNo, AR_SalesReceipt.Amount, AR_SalesReceipt.Charges,AR_SalesReceiptHeader.DocumentNo,
		AR_SalesReceiptHeader.DocumentTypeID, SET_DocumentType.DocumentType,
		AR_ReceiptTransactionHeader.BusinessPartnerTypeID,
		Case WHen AR_SalesReceipt.isApproved = 1 Then 
		convert(bit,1) else
		convert(bit,0) end as IsChecked
		, '' as Action
FROM            AR_SalesReceiptHeader

Inner Join AR_SalesReceipt  on AR_SalesReceipt.receiptheaderid = AR_SalesReceiptHeader.receiptheaderid
And AR_SalesReceipt.CompanyID = AR_SalesReceiptHeader.CompanyID
And AR_SalesReceipt.PeriodID = AR_SalesReceiptHeader.PeriodID
Inner Join AR_SET_Customer  on AR_SET_Customer.CustomerID = AR_SalesReceiptHeader.CustomerID
And AR_SET_Customer.CompanyID = AR_SalesReceiptHeader.CompanyID
And AR_SET_Customer.PeriodID = AR_SalesReceiptHeader.PeriodID
Inner Join AR_SET_BankBranch  on AR_SET_BankBranch.BankBranchID = AR_SalesReceipt.PaymentBankID
Inner Join AR_SET_BankAccount  on AR_SET_BankAccount.BankBranchID = AR_SalesReceipt.PaymentBankID

inner join SET_DocumentType  on SET_DocumentType.DocumentTypeID = AR_SalesReceiptHeader.DocumentTypeID
ANd SET_DocumentType.CompanyID = AR_SalesReceiptHeader.CompanyID
ANd SET_DocumentType.PeriodID = AR_SalesReceiptHeader.PeriodID

Left join GL_TransactionHeader on GL_TransactionHeader.DocumentTypeID = AR_SalesReceiptHeader.DocumentTypeID
ANd GL_TransactionHeader.DocumentNo = AR_SalesReceiptHeader.DocumentNo
ANd GL_TransactionHeader.CompanyID = AR_SalesReceiptHeader.CompanyID
ANd GL_TransactionHeader.PeriodID = AR_SalesReceiptHeader.PeriodID

left join GL_TransactionDetail on GL_TransactionDetail.TransactionID = GL_TransactionHeader.TransactionID
ANd GL_TransactionDetail.CompanyID = AR_SalesReceiptHeader.CompanyID
ANd GL_TransactionDetail.PeriodID = AR_SalesReceiptHeader.PeriodID

left join AR_ReceiptTransactionHeader on AR_ReceiptTransactionHeader.DocumentTypeID = AR_SalesReceiptHeader.DocumentTypeID
And AR_ReceiptTransactionHeader.DocumentNo = AR_SalesReceiptHeader.DocumentNo
And AR_ReceiptTransactionHeader.CompanyID = AR_SalesReceiptHeader.CompanyID

        Where AR_SalesReceipt.CompanyID = @CompanyID
        And AR_SalesReceipt.PeriodID = @PeriodID
        And AR_SalesReceiptHeader.ReceiptHeaderID = @ReceiptHeaderID
        ";

       const string Query_Update_ApprovedBy = @" UPDATE [AR_SalesReceipt] SET [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue, [isApproved] = @isApproved
		WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [SaleReceiptID] = @SaleReceiptID ";

       const string Query_Update_ApprovedByHeaderID = @" UPDATE [AR_SalesReceipt] SET [isApproved] = @isApproved
		WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [ReceiptHeaderID] = @ReceiptHeaderID ";

       #endregion

       public static bool InsertReceiptDet(List<AR_SalesReceipt> ColAddBranchDet)
       {
           bool result = false;
           
           try
           {
               foreach (AR_SalesReceipt rBranch in ColAddBranchDet)
                   rBranch.Insert(null);

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
           return result;
       }

       public static DataTable GetAllDataForSearch(string strWhere, int companyid, int periodid, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
           Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, SqlTransaction objTransaction = null)
       {
           List<SqlParameter> sqlparamsList = new List<SqlParameter>();
           OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
           DataTable dt = null;

           try
           {
               if (PageIndex != null)
               {
                   sqlparamsList.Add(new SqlParameter("@CompanyID", PageIndex));
                   sqlparamsList.Add(new SqlParameter("@PeriodID", PageIndex));
                   sqlparamsList.Add(new SqlParameter("@PageIndex", PageIndex));
                   sqlparamsList.Add(new SqlParameter("@PageSize", PageSize));
                   sqlparamsList.Add(new SqlParameter("@TotalRows", ReturnTotalRows));

                   SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                   sqlparamTotalRows.Direction = ParameterDirection.Output;

                   SqlParameter[] sqlparams = sqlparamsList.ToArray();

                   if (objTransaction != null)
                       dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_SelectAllForSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAllForSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                   else
                       dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAllForSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAllForSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                   ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
               }
               else
               {
                   if (objTransaction != null)
                       dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAllForSearch_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                   else
                       dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllForSearch_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
               }
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }

       //public static DataTable GetData_ByKey(int companyid, int periodid, int receiptid, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       //{
       //    DataTable dr = null;

       //    try
       //    {
       //        SqlParameter[] sqlparams = new SqlParameter[3];
       //        sqlparams[0] = new SqlParameter("@CompanyID", companyid);
       //        sqlparams[1] = new SqlParameter("@PeriodID", periodid);
       //        sqlparams[2] = new SqlParameter("@SaleReceiptID", receiptid);

       //        DataSet ds = null;

       //        if (objTransaction != null)
       //            ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_ByKey, sqlparams);
       //        else if (objConnection != null)
       //            ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_ByKey, sqlparams);
       //        else
       //            ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_ByKey, sqlparams);

       //        if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
       //            dr = ds.Tables[0];
       //    }
       //    catch (Exception ex)
       //    {
       //        SqlHelper.LogException(ex);
       //        throw ex;
       //    }

       //    return dr;
       //}

       public static bool UpdateBank_BranchDetail(int companyid, int periodid, List<AR_SalesReceipt> ColDeleteBrDt, List<AR_SalesReceipt> ColAddBranchDt)
       {
           bool result = false;
           SqlTransaction sqltrans = SqlHelper.GetTransaction("Update_BankAndBranchDetail");

           try
           {
               
               foreach (AR_SalesReceipt CD in ColDeleteBrDt)
                   if (!(AR_SalesReceipt.DeletePermanentByKey(sqltrans, CD.SaleReceiptID)))
                       throw new BusinessLogicException("Error in delete Branch: ");

               int counter = 0;
               foreach (AR_SalesReceipt BDt in ColAddBranchDt)
               {
                   if (BDt.BankBranchID == -1)
                   {
                       BDt.BankBranchID = AR_SalesReceipt.GetNewSaleReceiptID(companyid, periodid) + counter;
                       counter++;
                   }
               }

               foreach (AR_SalesReceipt BD in ColAddBranchDt)
               {
                   BD.CompanyID = companyid;
                   BD.PeriodID = periodid;
                   if (BD.SaleReceiptID == -1)
                   {
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

       internal static bool DeletePermanentByKey(SqlTransaction objTransaction, int saleReceiptID)
       {
           bool result = false;

           try
           {
               SqlParameter[] SqlParam = new SqlParameter[1];
               SqlParam[0] = new SqlParameter("@SaleReceiptID", saleReceiptID);

               string Query_PermanentDelete_ByKey = @" Delete From AR_SalesReceipt WHERE SaleReceiptID = @SaleReceiptID; ";

               if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_PermanentDelete_ByKey, SqlParam) > 0)
                   result = true;
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw new BusinessLogicException("Error In Delete. Class: Set_BankBranch, Function: DeletePermanentByKey. " + ex.Message + ex.StackTrace);
           }
           return result;
       }

       internal bool DeletePermanentByForeignKey(SqlTransaction objTransaction, int ReceiptHeaderID)
       {
           bool result = false;

           try
           {
               SqlParameter[] SqlParam = new SqlParameter[1];
               SqlParam[0] = new SqlParameter("@ReceiptHeaderID", ReceiptHeaderID);

               string Query_Delete_ByForeignKey = @" Delete from AR_SalesReceipt Where ReceiptHeaderID = @ReceiptHeaderID; ";

               if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByForeignKey, SqlParam) > 0)
                   result = true;
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw new BusinessLogicException("Error In Delete, Class Branch" + ex.Message);
           }

           return result;
       }

       public DataTable GetAllActiveData(int companyid, int periodid, int ReceiptHeaderID, Nullable<bool> isActive = true)
       {
           DataTable dt = null;

           try
           {
               SqlParameter[] SqlParam = new SqlParameter[3];
               SqlParam[0] = new SqlParameter("@ReceiptHeaderID", ReceiptHeaderID);
               SqlParam[1] = new SqlParameter("@CompanyID", companyid);
               SqlParam[2] = new SqlParameter("@PeriodID", periodid);

               dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_ByKey, SqlParam).Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }

       public DataTable GetDataByCustomerID(int companyid, int periodid, int CustomerID, DateTime paymentDate)
       {
           DataTable dt = null;

           try
           {
               SqlParameter[] SqlParam = new SqlParameter[4];
               SqlParam[0] = new SqlParameter("@CustomerID", CustomerID);
               SqlParam[1] = new SqlParameter("@CompanyID", companyid);
               SqlParam[2] = new SqlParameter("@PeriodID", periodid);
               SqlParam[3] = new SqlParameter("@PaymentDate", paymentDate);

               dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_ByCustomerId, SqlParam).Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }

       public DataTable GetDataByCustomerIDForEdit(int companyid, int periodid, int CustomerID, DateTime paymentDate)
       {
           DataTable dt = null;

           try
           {
               SqlParameter[] SqlParam = new SqlParameter[4];
               SqlParam[0] = new SqlParameter("@CustomerID", CustomerID);
               SqlParam[1] = new SqlParameter("@CompanyID", companyid);
               SqlParam[2] = new SqlParameter("@PeriodID", periodid);
               SqlParam[3] = new SqlParameter("@PaymentDate", paymentDate);

               dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_ByCustomerIdForEdit, SqlParam).Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }

       public DataTable GetDataByHeaderID(int companyid, int periodid, int headerid)
       {
           DataTable dt = null;

           try
           {
               SqlParameter[] SqlParam = new SqlParameter[3];
               SqlParam[0] = new SqlParameter("@CompanyID", companyid);
               SqlParam[1] = new SqlParameter("@PeriodID", periodid);
               SqlParam[2] = new SqlParameter("@ReceiptHeaderID", headerid);

               dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_ByHeaderID, SqlParam).Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }

       public bool UpdateDataApprovedBy(SqlTransaction objTransaction = null)
       {
           bool result = false;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[7];
               sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
               sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
               sqlparams[2] = new SqlParameter("@SaleReceiptID", f_SaleReceiptID);

               if (f_UpdatedBy.HasValue)
                   sqlparams[3] = new SqlParameter("@UpdatedBy", f_UpdatedBy.Value);
               else
                   sqlparams[3] = new SqlParameter("@UpdatedBy", DBNull.Value);

               if (f_UpdateDate.HasValue)
                   sqlparams[4] = new SqlParameter("@UpdateDate", f_UpdateDate.Value);
               else
                   sqlparams[4] = new SqlParameter("@UpdateDate", DBNull.Value);

               if (!string.IsNullOrEmpty(f_UpdatedByValue))
                   sqlparams[5] = new SqlParameter("@UpdatedByValue", f_UpdatedByValue);
               else
                   sqlparams[5] = new SqlParameter("@UpdatedByValue", DBNull.Value);
               if (f_isApproved.HasValue)
                   sqlparams[6] = new SqlParameter("@isApproved", f_isApproved.Value);
               else
                   sqlparams[6] = new SqlParameter("@isApproved", DBNull.Value);

               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_ApprovedBy, sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_ApprovedBy, sqlparams) > 0)
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

       public bool UpdateDataApprovedByHeaderID(SqlTransaction objTransaction = null)
       {
           bool result = false;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[4];
               sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
               sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
               sqlparams[2] = new SqlParameter("@ReceiptHeaderID", f_ReceiptHeaderID);
               sqlparams[3] = new SqlParameter("@isApproved", false);


               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_ApprovedByHeaderID, sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_ApprovedByHeaderID, sqlparams) > 0)
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

       public void UpdateDatailsAndTransaction(int companyid, int periodid, int receiptHeader, int TransID, int receiptTransID)
       {

           // Delete ReceiptBanksDetail
           AR_ReceiptBanksDetail.DeleteDataByReceiptHeaderID(companyid, periodid, receiptTransID);

           // Delete REceiptAccountDetails
           AR_ReceiptAccountsDetail.DeleteDataByReceiptHeaderID(companyid, periodid, receiptTransID);

           // Delete ReceiptTransaction
           AR_ReceiptTransactionHeader.DeleteDataByHeaderID(companyid, periodid, receiptTransID);

           // Delete GL Header and Detaild
           GL_TransactionHeader.DeleteDataByTransactionID(null, TransID);

           this.CompanyID = companyid;
           this.PeriodID = periodid;
           this.ReceiptHeaderID = receiptHeader;
           this.UpdateDataApprovedByHeaderID();


       }

    }
}
