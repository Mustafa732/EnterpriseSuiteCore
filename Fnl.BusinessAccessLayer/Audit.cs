using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;

namespace Audit
{
    public static class Audit
    {
        public static void Log(string eventName, string eventDescription, DateTime timeStamp, string eventUser, string currentForm, string userIP = "", string UserID = "", string DocumentNo = "", string DocumentTypeID = "",
            int CompanyID = 0, int FiscalID = 0, string Comments = "", SqlConnection objConnection = null)
        {
            try
            {
                string qry = "Insert into AuditTrail Select '" + eventName + "', '" + eventDescription + "', '" + timeStamp.ToString("yyyy-MM-dd HH:mm:ss") + "', '"
                    + eventUser + "', '" + currentForm + "', '" + userIP + "', '" + UserID + "', '" + DocumentNo + "', '" + DocumentTypeID + "', '" + CompanyID + "', '" + FiscalID + "', '" + Comments + "' ";

                if(objConnection != null)
                    SqlHelper.ExecuteNonQuery(objConnection, CommandType.Text, qry);
                else
                    SqlHelper.ExecuteNonQuery(CommandType.Text, qry);
            }
            catch
            {
                Console.WriteLine("Error while writing to log");
            }
        }
    }
}
