using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;

using DAL.DataAccessLayer;

using FnI.BusinessAccessLayer.Inventory.Setup;
using FnI.BusinessAccessLayer.Setup;

namespace FnI.BusinessAccessLayer.Inventory.Transaction
{
    public partial class INV_LoadingPointDetail
    {
        const string Query_Update_ForDOUpdate = @" UPDATE [INV_LoadingPointDetail] 
SET 
--[UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue,
 [DOHeaderID] = @DOHeaderID, ItemID = @ItemID
		WHERE --[DOHeaderID] = @DOHeaderIDOld 
 [CompanyID] = @CompanyID AND [PeriodID] = @PeriodID and LoadingPointHeaderID = @LoadingPointHeaderID ";

        public bool UpdateDataForDOUpdate(int companyid ,int periodid ,int doheaderid ,int doheaderidold,int loadingpointheaderid,int itemid, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
               
              
                SqlParameter[] sqlparams = new SqlParameter[6];
                
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[2] = new SqlParameter("@DOHeaderID", doheaderid);
                sqlparams[3] = new SqlParameter("@DOHeaderIDOld", doheaderidold);
                sqlparams[4] = new SqlParameter("@LoadingPointHeaderID", LoadingPointHeaderID);
                sqlparams[5] = new SqlParameter("@ItemID", itemid);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_ForDOUpdate, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_ForDOUpdate, sqlparams) > 0)
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
    }
}
