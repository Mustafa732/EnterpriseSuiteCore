using System;
using System.Data;
using System.Data.Common;
using System.Globalization;
using Microsoft.Data.SqlClient;
using System.Configuration;

namespace DAL.DataAccessLayer
{
    public class DatabaseHelper : IDisposable
    {
        #region Fields / Properties
        private SqlConnection _Connection;
        private SqlTransaction _Transaction;
        private SqlCommand _Command;
        private string _StoredProcedureName;

        public string StoredProcedureName
        {
            get { return _StoredProcedureName; }
            set
            {
                if (value == null) return;
                _StoredProcedureName = value;

                if (_Command != null && _Command.Parameters.Count != 0)
                    _Command.Parameters.Clear();

                _Command = new SqlCommand(_StoredProcedureName, _Connection);
                _Command.CommandType = CommandType.StoredProcedure;
                if (_Transaction != null)
                    _Command.Transaction = _Transaction;
            }
        }
        #endregion

        #region Constructor
        public DatabaseHelper()
        {
            var connStr = ConfigurationManager.ConnectionStrings["FnISystem_ConnectionString"]?.ConnectionString;
            if (string.IsNullOrEmpty(connStr))
                connStr = ConfigurationManager.ConnectionStrings["FnISystem"]?.ConnectionString ?? "Server=.;Database=FnISystem;Trusted_Connection=True;TrustServerCertificate=True;";

            _Connection = new SqlConnection(connStr);
            _Command = new SqlCommand();
            _Command.Connection = _Connection;
        }

        public DatabaseHelper(ApplicationConnectionString connectionString)
        {
            string name = connectionString.ToString();
            var connStr = ConfigurationManager.ConnectionStrings[name]?.ConnectionString ??
                          ConfigurationManager.ConnectionStrings["FnISystem_ConnectionString"]?.ConnectionString;

            _Connection = new SqlConnection(connStr);
            _Command = new SqlCommand();
            _Command.Connection = _Connection;
        }

        public DatabaseHelper(string connectionStringNameOrConnStr)
        {
            string connStr = ConfigurationManager.ConnectionStrings[connectionStringNameOrConnStr]?.ConnectionString;
            if (string.IsNullOrEmpty(connStr))
                connStr = connectionStringNameOrConnStr;

            _Connection = new SqlConnection(connStr);
            _Command = new SqlCommand();
            _Command.Connection = _Connection;
        }
        #endregion

        #region Connection
        private void OpenConnection()
        {
            if (_Connection == null)
                throw new InvalidOperationException("Connection not initialized");

            if (_Connection.State != ConnectionState.Open)
                _Connection.Open();
        }
        private void CloseConnection()
        {
            if (_Connection != null && _Connection.State != ConnectionState.Closed)
                _Connection.Close();
        }
        #endregion

        #region Transaction
        private void BeginTransaction()
        {
            OpenConnection();
            if (_Transaction == null)
                _Transaction = _Connection.BeginTransaction();
            _Command.Transaction = _Transaction;
        }
        private void EndTransaction()
        {
            if (_Transaction != null)
            {
                _Transaction.Commit();
                _Transaction.Dispose();
                _Transaction = null;
            }
        }
        public void Commit() => EndTransaction();
        public void Rollback()
        {
            if (_Transaction != null)
            {
                _Transaction.Rollback();
                _Transaction.Dispose();
                _Transaction = null;
            }
        }
        #endregion

        #region Command.Parameters
        public void AddParameterBool(string name, bool value) => AddParameter(name, DbType.Boolean, value);
        public void AddParameterInt(string name, int value) => AddParameter(name, DbType.Int32, value);
        public void AddParameterDecimal(string name, decimal value) => AddParameter(name, DbType.Decimal, value);
        public void AddParameterMoney(string name, decimal value) => AddParameter(name, DbType.Currency, value);
        public void AddParameterDateTime(string name, DateTime value) => AddParameter(name, DbType.DateTime, value);
        public void AddParameterChar(string name, string value) => AddParameter(name, DbType.AnsiStringFixedLength, value);
        public void AddParameterVarChar(string name, string value) => AddParameter(name, DbType.AnsiString, value);

        public void AddParameter(string name, DbType dbType, object value)
        {
            var param = _Command.CreateParameter();
            param.ParameterName = name.StartsWith("@") ? name : "@" + name;
            param.DbType = dbType;
            param.Value = value ?? DBNull.Value;
            _Command.Parameters.Add(param);
        }
        public void AddOutParameter(string name, DbType dbType, int size)
        {
            var param = _Command.CreateParameter();
            param.ParameterName = name.StartsWith("@") ? name : "@" + name;
            param.DbType = dbType;
            param.Size = size;
            param.Direction = ParameterDirection.Output;
            _Command.Parameters.Add(param);
        }
        public object GetParameterValue(string name)
        {
            string pName = name.StartsWith("@") ? name : "@" + name;
            return _Command.Parameters[pName].Value;
        }
        #endregion

        #region Execute
        public IDataReader ExecuteReader()
        {
            OpenConnection();
            if (_Transaction != null)
                _Command.Transaction = _Transaction;
            return _Command.ExecuteReader();
        }
        public int ExecuteNonQuery()
        {
            BeginTransaction();
            return _Command.ExecuteNonQuery();
        }
        public object ExecuteScalar()
        {
            BeginTransaction();
            return _Command.ExecuteScalar();
        }
        public DataSet ExecuteDataSet()
        {
            OpenConnection();
            if (_Transaction != null)
                _Command.Transaction = _Transaction;

            var ds = new DataSet();
            using (var adapter = new SqlDataAdapter(_Command))
            {
                adapter.Fill(ds);
            }
            return ds;
        }
        #endregion

        #region Coalesce
        public static decimal Coalesce(object value, decimal defaultValue)
        {
            if (value != null && value != DBNull.Value && value.ToString().Length != 0)
                return Convert.ToDecimal(value, CultureInfo.CurrentCulture);
            return defaultValue;
        }
        public static int Coalesce(object value, int defaultValue)
        {
            if (value != null && value != DBNull.Value && value.ToString().Length != 0)
                return Convert.ToInt32(value, CultureInfo.CurrentCulture);
            return defaultValue;
        }
        public static string Coalesce(object value, string defaultValue)
        {
            if (value == null || value == DBNull.Value || value.ToString().Length == 0 || string.Compare(value.ToString(), "''", true, CultureInfo.CurrentCulture) == 0)
                return defaultValue;
            return value.ToString();
        }
        public static DateTime Coalesce(object value, DateTime defaultValue)
        {
            if (value != null && value != DBNull.Value)
                return Convert.ToDateTime(value, CultureInfo.CurrentCulture);
            return defaultValue;
        }
        public static bool Coalesce(object value, bool defaultValue)
        {
            if (value != null && value != DBNull.Value && value.ToString().Length != 0)
                return Convert.ToBoolean(value, CultureInfo.CurrentCulture);
            return defaultValue;
        }
        public static double Coalesce(object value, double defaultValue)
        {
            if (value != null && value != DBNull.Value && value.ToString().Length != 0)
                return Convert.ToDouble(value, CultureInfo.CurrentCulture);
            return defaultValue;
        }
        #endregion

        public static decimal ToDecimal(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            return Decimal.Parse(value.Replace(" ", ""), NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint | NumberStyles.AllowCurrencySymbol, CultureInfo.CurrentCulture);
        }

        #region IsClean
        public static bool IsClean(DataSet ds) => (ds != null && ds.Tables.Count != 0 && ds.Tables[0].Rows.Count != 0);
        public static bool IsClean(DataTable dt) => (dt != null && dt.Rows.Count != 0);
        #endregion

        #region Converters
        public static string[] TableToArray(DataTable table)
        {
            if (table == null) throw new ArgumentNullException("table");
            if (table.Rows.Count == 0) return new string[0];
            string[] data = new string[table.Rows.Count];
            for (int i = 0; i < table.Rows.Count; i++)
                data[i] = Convert.ToString(table.Rows[i][0], CultureInfo.CurrentCulture);
            return data;
        }
        #endregion

        #region Dispose Pattern
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_Transaction != null)
                {
                    try { _Transaction.Rollback(); } catch { }
                    _Transaction.Dispose();
                    _Transaction = null;
                }
                if (_Command != null)
                {
                    _Command.Dispose();
                    _Command = null;
                }
                CloseConnection();
                if (_Connection != null)
                {
                    _Connection.Dispose();
                    _Connection = null;
                }
            }
        }
        #endregion
    }

    public enum ApplicationConnectionString
    {
        FnISystem_ConnectionString,
    }
}
