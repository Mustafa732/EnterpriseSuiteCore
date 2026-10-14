using System;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using System.Resources;
using System.Collections;
using System.Globalization;


namespace FnI.BusinessAccessLayer.Setup
{
    public class ExecuteScripts
    {
        public static bool ExecuteApplicationScripts(string Con)
        {
            bool result = false;

            try
            {

                using (SqlConnection connection = new SqlConnection(Con))
                {
                    connection.Open();
                    foreach (var batch in FnI.QueryResourceLibrary.Setup.ExecuteScripts.Query_ExecuteApplicationScripts.Split(new string[] { "\nGO", "\ngo" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        try
                        {
                            new SqlCommand(batch, connection).ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            throw;
                        }
                    }

                    foreach (var batch_new in FnI.QueryResourceLibrary.Setup.ExecuteScriptsTwo.Query_ExecuteScripts.Split(new string[] { "\nGO", "\ngo" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        try
                        {
                            new SqlCommand(batch_new, connection).ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            throw;
                        }
                    }


                    connection.Close();
                }

                result = true;

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }


        public static bool ExecuteScriptsFromFile(string Con,string QueriesFromFile)
        {
            bool result = false;

            try
            {

                using (SqlConnection connection = new SqlConnection(Con))
                {
                    connection.Open();
                    foreach (var batch in QueriesFromFile.Split(new string[] { "\nGO", "\ngo" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        try
                        {
                            new SqlCommand(batch, connection).ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            throw;
                        }
                    }

                    connection.Close();
                }


                result = true;

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataTable GetDataByQuery(string Query, string objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                DataSet ds = null;
                SqlParameter[] sqlparams = new SqlParameter[3];

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);

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

        public static bool RunExecuteNonQuery(string Query, SqlTransaction objTransaction = null)
        {
            bool result = false;
            try
            {
                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query) > 0)
                    result = true;
            }
            catch { }
            return result;

        }


        public static int FindModuleIDbyMenuPath(string MenuPath)
        {
            int result = 0;

            ResourceManager MyResourceClass = new ResourceManager(typeof(FnI.QueryResourceLibrary.Setup.MenuInformation));

            ResourceSet resourceSet = MyResourceClass.GetResourceSet(CultureInfo.CurrentUICulture, true, true);
        
            foreach (DictionaryEntry entry in resourceSet)
            {
                string Menu = entry.Key.ToString();
                object ModuleID = entry.Value;

                if (MenuPath.Contains(Menu))
                {
                    result = int.Parse(ModuleID.ToString());
                    break;
                }
            }

            return result;
        }

        public static DataTable GetData(string Query, SqlTransaction objTransaction = null)
        {
            DataTable dtToReturn = null;
            string filter = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", 1);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }
    }
}
