using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Common.Utils;
using FnI.BusinessAccessLayer.Setup;
using FnI.BusinessAccessLayer.Sales;
using DAL.DataAccessLayer;
using FnI.Core.Web.HttpApplicationState;

namespace FnI.Core.Web.UI
{
    // .NET 8 MVC - Old BasePage : System.Web.UI.Page ka replacement
    public abstract class BaseController : Controller
    {
        protected readonly IConfiguration _configuration;
        protected readonly IHttpContextAccessor _httpContextAccessor;

        public BaseController(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        #region Session Variables - Same logic as old BasePage

        protected int CurrentCompanyID
        {
            get
            {
                var val = HttpContext.Session.GetString("CompanyID");
                if (string.IsNullOrEmpty(val))
                {
                    Response.Redirect("/Welcome");
                    return 0;
                }
                return Convert.ToInt32(val);
            }
            set => HttpContext.Session.SetString("CompanyID", value.ToString());
        }

        protected int CurrentPeriodID
        {
            get
            {
                var val = HttpContext.Session.GetString("PeriodID");
                if (string.IsNullOrEmpty(val))
                {
                    Response.Redirect("/Welcome");
                    return 0;
                }
                return Convert.ToInt32(val);
            }
            set => HttpContext.Session.SetString("PeriodID", value.ToString());
        }

        protected int CurrentFiscalID
        {
            get
            {
                var val = HttpContext.Session.GetString("FiscalID");
                if (string.IsNullOrEmpty(val)) return 1;
                int id = Convert.ToInt32(val);
                return id > 0 ? id : 1;
            }
            set => HttpContext.Session.SetString("FiscalID", value.ToString());
        }

        protected int CurrentChartOfAccountID
        {
            get
            {
                var val = HttpContext.Session.GetString("ChartOfAccountID");
                if (string.IsNullOrEmpty(val))
                {
                    Response.Redirect("/Welcome");
                    return 0;
                }
                return Convert.ToInt32(val);
            }
            set => HttpContext.Session.SetString("ChartOfAccountID", value.ToString());
        }

        protected int CurrentCompanyBaseCurrencyID
        {
            get
            {
                var val = HttpContext.Session.GetString("CurrentCompanyBaseCurrencyID");
                if (string.IsNullOrEmpty(val))
                {
                    Response.Redirect("/Welcome");
                    return 0;
                }
                return Convert.ToInt32(val);
            }
            set => HttpContext.Session.SetString("CurrentCompanyBaseCurrencyID", value.ToString());
        }

        protected string PrinterName
        {
            get
            {
                var val = HttpContext.Session.GetString("PrinterName");
                if (string.IsNullOrEmpty(val))
                {
                    Response.Redirect("/Welcome");
                    return string.Empty;
                }
                if (string.IsNullOrWhiteSpace(val))
                    throw new Exception("Error!, Document could not printed because you have not assigned a printer.");
                return val;
            }
            set => HttpContext.Session.SetString("PrinterName", value);
        }

        protected string CurrentUserIP
        {
            get
            {
                var val = HttpContext.Session.GetString("CurrentUserIP");
                if (string.IsNullOrEmpty(val))
                {
                    Response.Redirect("/Welcome");
                    return string.Empty;
                }
                return val;
            }
            set => HttpContext.Session.SetString("CurrentUserIP", value);
        }

        protected bool CentralServer
        {
            set => HttpContext.Session.SetString("CentralServer", value.ToString());
        }

        protected bool HasCentralServer
        {
            get => !string.IsNullOrEmpty(_configuration.GetConnectionString("CentralServer"));
        }

        #endregion

        #region Protected Variables - Identity based

        protected int? CurrentUserAuthorityLevel
        {
            get => GetAuthorityLevel(CurrentCompanyID, CurrentPeriodID, CurrentUserID.ToString());
        }

        protected Guid CurrentUserID
        {
            get
            {
                // Old: Membership.GetUser().ProviderUserKey
                // New: From Claims or Session
                var uid = HttpContext.Session.GetString("CurrentUserID");
                if (Guid.TryParse(uid, out Guid g)) return g;
                // Fallback from Identity
                if (User.Identity.IsAuthenticated)
                {
                    var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (Guid.TryParse(claim, out g)) return g;
                }
                return Guid.Empty;
            }
        }

        protected string CurrentUserName
        {
            get => User.Identity.Name ?? HttpContext.Session.GetString("CurrentUserName") ?? "";
        }

        protected string AllowedPeriods
        {
            get => GetPreviousAllowedPeriods(CurrentCompanyID, CurrentPeriodID);
        }

        #endregion

        #region Page_Load replacement

        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            // Decrypt connection string - same logic as old Page_Load
            try
            {
                var connStringSettings = _configuration.GetConnectionString("FnISystem");
                if (!string.IsNullOrEmpty(connStringSettings))
                {
                    string Connection = connStringSettings;
                    // Old logic had encrypted password after last ;
                    if (Connection.Contains(";"))
                    {
                        string Password = Connection.Substring(Connection.LastIndexOf(';') + 10, Connection.Length - (Connection.LastIndexOf(';') + 10));
                        Connection = Connection.Remove(Connection.LastIndexOf(';') + 10, Connection.Length - (Connection.LastIndexOf(';') + 10));
                        string ActualPassword = Decrypt(Password, true);
                        Connection = Connection + ActualPassword;
                        // SetProviderConnectionString removed - Identity uses new connection
                    }
                }

                if (!User.Identity.IsAuthenticated && CurrentUserID == Guid.Empty)
                {
                    // Optional redirect
                    // context.Result = RedirectToAction("Login", "Account");
                }
            }
            catch { }
        }

        public static string Decrypt(string cipherString, bool useHashing)
        {
            byte[] keyArray;
            byte[] toEncryptArray = Convert.FromBase64String(cipherString);
            string key = "LLBC";

            if (useHashing)
            {
                using (MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider())
                {
                    keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
                }
            }
            else
            {
                keyArray = UTF8Encoding.UTF8.GetBytes(key);
            }

            using (TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider())
            {
                tdes.Key = keyArray;
                tdes.Mode = CipherMode.ECB;
                tdes.Padding = PaddingMode.PKCS7;
                ICryptoTransform cTransform = tdes.CreateDecryptor();
                byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
                return UTF8Encoding.UTF8.GetString(resultArray);
            }
        }

        #endregion

        #region Helping Methods

        protected SqlTransaction GetTransaction(string transactionName)
        {
            return SqlHelper.GetTransaction(transactionName);
        }

        public void IsValidPeriod(int periodId, DateTime periodDate)
        {
            DataTable dtPeriod = SET_Period.ValidPeriod(periodId, periodDate);
            if (dtPeriod.Rows.Count < 1)
                throw new BusinessLogicException("Invalid period id.");
            if (!bool.Parse(dtPeriod.Rows[0]["IsValidPeriod"].ToString()))
                throw new BusinessLogicException("Invalid document date. Document date between " + DateUtil.ConvertToString(DateTime.Parse(dtPeriod.Rows[0]["StartDate"].ToString())) + " To " + DateUtil.ConvertToString(DateTime.Parse(dtPeriod.Rows[0]["EndDate"].ToString())));
        }

        protected int? GetAuthorityLevel(int CompanyID, int PeriodID, string UserID)
        {
            int? AuthorityLevel = null;
            DataTable dt_GetAuthorityLevel = SET_DocumentApprove.GetAuthorityLevelbyUserID(CompanyID, PeriodID, UserID);
            if (dt_GetAuthorityLevel != null && dt_GetAuthorityLevel.Rows.Count > 0)
            {
                AuthorityLevel = int.Parse(dt_GetAuthorityLevel.Rows[0]["AuthorityLevel"].ToString());
            }
            return AuthorityLevel;
        }

        protected string GetPreviousAllowedPeriods(int CompanyID, int PeriodID)
        {
            string AllowedPeriods = String.Empty;
            DataTable dt_AllowedPeriods = SET_DocumentApprove.GetPreviousAllowedPeriod(CompanyID, PeriodID);
            if (dt_AllowedPeriods != null && dt_AllowedPeriods.Rows.Count > 0)
            {
                AllowedPeriods = dt_AllowedPeriods.Rows[0]["AllowedPeriods"].ToString();
            }
            return AllowedPeriods;
        }

        protected int GetFiscalID(int CompanyID, int PeriodID)
        {
            int FiscalID = 0;
            DataTable dt_GetFiscalID = SET_DocumentApprove.GetFiscalID(CompanyID, PeriodID);
            if (dt_GetFiscalID != null && dt_GetFiscalID.Rows.Count > 0)
            {
                FiscalID = (int)dt_GetFiscalID.Rows[0]["PeriodID"];
            }
            return FiscalID;
        }

        #endregion

        #region Sql Helper Method - .NET 8 Microsoft.Data.SqlClient

        public static string _SiteServerConn => System.Configuration.ConfigurationManager.ConnectionStrings["FnISystem"]?.ConnectionString;
        public static string _CentralServerConn => System.Configuration.ConfigurationManager.ConnectionStrings["CentralServer"]?.ConnectionString;
        public static string _FNIServerConn => System.Configuration.ConfigurationManager.ConnectionStrings["FNIServer"]?.ConnectionString;

        // For .NET 8 config based
        public string GetSiteServerConnectionString()
        {
            return _configuration.GetConnectionString("FnISystem");
        }

        public static SqlTransaction GetUITransaction(string TransactionName, string connectionString)
        {
            string Password = "";
            string Connection = connectionString;
            Password = Connection.Substring(Connection.LastIndexOf(';') + 10, Connection.Length - (Connection.LastIndexOf(';') + 10));
            Connection = Connection.Remove(Connection.LastIndexOf(';') + 10, Connection.Length - (Connection.LastIndexOf(';') + 10));
            string ActualPassword = Decrypt(Password, true);
            Connection = Connection + ActualPassword;
            if (AppParameters.MaxPoolSize != 0)
                Connection = Connection + ";Max Pool Size=" + AppParameters.MaxPoolSize.ToString() + ";";
            else
                Connection = Connection + ";Max Pool Size=300;";
            SqlConnection sqlcon = new SqlConnection(Connection);
            sqlcon.Open();
            SqlTransaction sqltrans = sqlcon.BeginTransaction(TransactionName);
            return sqltrans;
        }

        public static SqlTransaction GetUITransaction(string TransactionName, string DataSource, string InitialCatalog, string UserID, string Password)
        {
            string ActualPassword = Decrypt(Password, true);
            string connectionString = "Data Source=" + DataSource + ";" + "Initial Catalog=" + InitialCatalog + ";" + "User ID=" + UserID + ";" + "Password=" + ActualPassword;
            SqlConnection sqlcon = new SqlConnection(connectionString);
            sqlcon.Open();
            SqlTransaction sqltrans = sqlcon.BeginTransaction(TransactionName);
            return sqltrans;
        }

        public static void CommitUITransaction(SqlTransaction transaction)
        {
            transaction.Commit();
            if (transaction.Connection != null)
                if (transaction.Connection.State == ConnectionState.Open)
                    transaction.Connection.Close();
        }

        public static void RollBackUITransaction(SqlTransaction transaction, string TransactionName)
        {
            transaction.Rollback(TransactionName);
            if (transaction.Connection != null)
                if (transaction.Connection.State == ConnectionState.Open)
                    transaction.Connection.Close();
        }

        #endregion
    }
}
