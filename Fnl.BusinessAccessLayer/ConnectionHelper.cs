using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;

namespace FnI.BusinessAccessLayer
{
    public class ConnectionHelper
    {

        public static SqlConnection GetOpenSqlConnection()
        {
            return DAL.DataAccessLayer.SqlHelper.GetOpenSqlConnection();
        }

        public static void CloseConnection(SqlConnection sqlcon)
        {
            DAL.DataAccessLayer.SqlHelper.CloseConnection(sqlcon);
        }

    }
}
