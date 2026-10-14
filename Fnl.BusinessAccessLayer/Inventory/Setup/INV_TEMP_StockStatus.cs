using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;
using FnI.BusinessAccessLayer.Inventory.Transaction;

namespace FnI.BusinessAccessLayer.Inventory.Setup
{
    public partial class INV_TEMP_StockStatus
    {

        const string Query_SumAllTransactionQuantity = @"Delete from INV_Temp_StockStatus";

        public static bool Delete()
        {
            bool result = false;
            try
            {
                  if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_SumAllTransactionQuantity) > 0)
                  result = true;  
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        
    }
}
