using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Inventory.Setup
{
    public partial class SET_ItemOptionsDetail
    {
        #region Constant Strings
            const string Query_GetAllOptions = @" SELECT * FROM [SET_ItemOptionsDetail] where ItemOptionsHeaderId= @ItemOptionsHeaderId ";

            const string Query_GetAllOptionsAndHeader = @" SELECT * FROM [SET_ItemOptionsDetail] 
 inner join [SET_ItemOptionsHeader]  on [SET_ItemOptionsDetail].ItemOptionsHeaderId = [SET_ItemOptionsHeader].ItemOptionsHeaderId ";
        #endregion

        #region Methods
                public static DataTable GetAllOptionsByHeaderID(int HeaderID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
            {
                DataTable dt = null;
                   //new  
                try
                {
                    DataSet ds = null;

                    SqlParameter[] sqlparams = new SqlParameter[1];
                    sqlparams[0] = new SqlParameter("@ItemOptionsHeaderId", HeaderID);


                    if (objTransaction != null)
                        ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetAllOptions, sqlparams);
                    else if (objConnection != null)
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetAllOptions, sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllOptions, sqlparams);

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

                public static DataTable GetAllOptions(string strWhere, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
                {
                    DataTable dt = null;
                    //new  
                    try
                    {
                        DataSet ds = null;

                        if (objTransaction != null)
                            ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetAllOptionsAndHeader);
                        else if (objConnection != null)
                            ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetAllOptionsAndHeader);
                        else
                            ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllOptionsAndHeader);

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
        #endregion 

               
        
    }
}
