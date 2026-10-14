using System;
using Microsoft.AspNetCore.Http;

namespace FnI.Core.Web
{
    public class AppSessions
    {
        private static IHttpContextAccessor _httpContextAccessor;

        // Program.cs me isko set karna hai
        public static void Configure(IHttpContextAccessor accessor)
        {
            _httpContextAccessor = accessor;
        }

        private static ISession Session => _httpContextAccessor?.HttpContext?.Session;

        private static int GetInt(string key, int defaultValue = 0)
        {
            var val = Session?.GetString(key);
            return int.TryParse(val, out int result) ? result : defaultValue;
        }

        private static void SetInt(string key, int value)
        {
            Session?.SetString(key, value.ToString());
        }

        public static int CurrentCompanyID
        {
            get => GetInt("CompanyID");
            set => SetInt("CompanyID", value);
        }

        public static int CurrentPeriodID
        {
            get => GetInt("PeriodID");
            set => SetInt("PeriodID", value);
        }

        public static int CurrentFiscalID
        {
            get
            {
                var val = GetInt("FiscalID", 1);
                return val > 0 ? val : 1;
            }
            set => SetInt("FiscalID", value);
        }

        public static int CurrentChartOfAccountID
        {
            get => GetInt("ChartOfAccountID");
            set => SetInt("ChartOfAccountID", value);
        }

        public static Guid CurrentUserID
        {
            get
            {
                var val = Session?.GetString("CurrentUserID");
                if (Guid.TryParse(val, out Guid g)) return g;

                // Identity se bhi try karo
                var claim = _httpContextAccessor?.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(claim, out g)) return g;

                return Guid.Empty;
            }
            set => Session?.SetString("CurrentUserID", value.ToString());
        }

        // Notification ke liye chahiye tha
        public static string CurrentUserName
        {
            get
            {
                var val = Session?.GetString("CurrentUserName");
                if (!string.IsNullOrEmpty(val)) return val;
                return _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "System";
            }
            set => Session?.SetString("CurrentUserName", value);
        }

        public static string CurrentUserIP
        {
            get => Session?.GetString("CurrentUserIP") ?? _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "";
            set => Session?.SetString("CurrentUserIP", value);
        }

        public static bool HasCentralServer
        {
            get
            {
                // Config se check karo
                return !string.IsNullOrEmpty(System.Configuration.ConfigurationManager.ConnectionStrings["CentralServer"]?.ConnectionString);
            }
        }

        public static bool CentralServer
        {
            set => Session?.SetString("CentralServer", value.ToString());
        }
    }
}