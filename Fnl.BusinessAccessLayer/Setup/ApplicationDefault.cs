using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Common.Utils;

using DAL.DataAccessLayer;
using System.Web;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class ApplicationDefault 
	{
        public static string CheckForBeet(string val, string changeval)
        {

            if (val.Contains(" Cane ") || val.Contains(" Cane") || val.Contains("Cane "))
            {
                val = val.Replace("Cane", changeval);
            }
            else
                if (val.Contains(" CANE ") || val.Contains(" CANE") || val.Contains("CANE "))
                {
                    val = val.Replace("CANE", changeval.ToUpper());
                }
                else if (val.Contains(" cane ") || val.Contains(" cane") || val.Contains("cane "))
                {
                    val = val.Replace("cane", changeval.ToLower());
                }

            return val;
        }
        
		#region Constant Strings

        const string Query_InsertApplicationCode = @" IF EXISTS ( SELECT DefaultValue FROM  [ApplicationDefault] WHERE  DefaultName = @DefaultName ) 
                    UPDATE  [ApplicationDefault]  SET     DefaultName = @DefaultName, DefaultValue = @DefaultValue
	                         WHERE DefaultName = @DefaultName
                                    ELSE 
	                                INSERT  INTO [ApplicationDefault](DefaultName, DefaultValue )  VALUES  ( @DefaultName, @DefaultValue ) ";

			
		#endregion

		#region Class Methods

        public bool InsertApplicationDefault(List<ApplicationDefault> ApplicatinoDefaultData )
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertDefaultApplication");

            try
            {

                foreach (ApplicationDefault ApData in ApplicatinoDefaultData)
                {
                    ApData.DefaultName = DefaultName;
                    ApData.DefaultValue = DefaultValue;
                    //if (!ApData.InsertApplicationCode(sqltrans))
                      //  throw new BusinessLogicException("Record is not inserted in Applicaiton Default!");
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

            finally  {
                if (result)
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "InsertDefaultApplication");

            }

            return result;
        }

        public static DataTable GetAllData()
        {
           
            DataTable dt = null;

            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_WithoutPaging).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

		public bool InsertApplicationCode(string ParamdefaultName,string ParamDefaultValue,  SqlTransaction objTransaction = null)
		{
			bool result = false;

			try
			{
				SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@DefaultName", ParamdefaultName);
                sqlparams[1] = new SqlParameter("@DefaultValue", ParamDefaultValue);

				if(objTransaction == null)
				{
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_InsertApplicationCode, sqlparams) > 0)
						result = true;
				}
				else
				{
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_InsertApplicationCode, sqlparams) > 0)
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

		#endregion
	}
}
