using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DAL.DataAccessLayer;
using Common.Utils;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace FnI.BusinessAccessLayer.FA.Transaction
{
   public partial class FA_Disposal
   {
       #region Constants

       const string Query_GetAmount = @"select distinct GL_TransactionDetail.Amount
 from GL_TransactionDetail 

inner join GL_TransactionHeader on GL_TransactionHeader.TransactionID = GL_TransactionDetail.TransactionID 
AND GL_TransactionHeader.CompanyID = GL_TransactionDetail.CompanyID
AND GL_TransactionHeader.PeriodID = GL_TransactionDetail.PeriodID
AND GL_TransactionHeader.Status = 1

where GL_TransactionHeader.TransactionID = @TransactionID
and GL_TransactionHeader.DocumentNo = @DocumentNo
AND  GL_TransactionHeader.CompanyID = @CompanyID
AND  GL_TransactionHeader.PeriodID = @PeriodID
AND GL_TransactionDetail.Status =1 ";
       #endregion

       #region Methods

       public static DataTable GetAmountFoVoucher(int VoucherNo, int DocumentNo, int CompanyID, int PeriodID)
       {
           DataTable dtToReturn = null;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[4];
               sqlparams[0] = new SqlParameter("@TransactionID", VoucherNo);
               sqlparams[1] = new SqlParameter("@DocumentNo", DocumentNo);            
               sqlparams[2] = new SqlParameter("@CompanyID", CompanyID);
               sqlparams[3] = new SqlParameter("@PeriodID", PeriodID);


               DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAmount, sqlparams);

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




       #endregion

   }
}
