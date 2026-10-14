using Microsoft.Data.SqlClient;
using System;
using FnI.BusinessAccessLayer.Setup;
using FnI.Core.Web;
using Microsoft.AspNetCore.Http;

namespace FnI.Core.Helper
{
    public class Notification
    {
        #region Current User Name - .NET 8 Version

        // Pehle: static string CurrentUserName = Membership.GetUser().UserName;
        // Ab: Session ya Identity se

        private static IHttpContextAccessor _httpContextAccessor;

        // Startup me set karna hoga: Notification.Configure(httpContextAccessor);
        public static void Configure(IHttpContextAccessor accessor)
        {
            _httpContextAccessor = accessor;
        }

        static string CurrentUserName
        {
            get
            {
                // 1. AppSessions se (tumhara purana logic)
                try
                {
                    if (!string.IsNullOrEmpty(AppSessions.CurrentUserName))
                        return AppSessions.CurrentUserName;
                }
                catch { }

                // 2. HttpContext se (Identity)
                try
                {
                    var context = _httpContextAccessor?.HttpContext;
                    if (context?.User?.Identity?.IsAuthenticated == true)
                        return context.User.Identity.Name;

                    var sessionUser = context?.Session.GetString("CurrentUserName");
                    if (!string.IsNullOrEmpty(sessionUser))
                        return sessionUser;
                }
                catch { }

                return "System";
            }
        }

        #endregion

        #region Paramtrize Notification Alert Function

        static public bool NotificationAlert(string documentName, string documentNumber, string mode, string createDate, SqlConnection objConnection = null)
        {
            bool result = false;
            int notificationID = -1;

            if (objConnection != null)
                notificationID = SET_Notification.GetNewNotificationID(AppSessions.CurrentCompanyID, AppSessions.CurrentPeriodID, null, objConnection);
            else
                notificationID = SET_Notification.GetNewNotificationID(AppSessions.CurrentCompanyID, AppSessions.CurrentPeriodID);

            SET_Notification notify = new SET_Notification()
            {
                CompanyID = AppSessions.CurrentCompanyID,
                PeriodID = AppSessions.CurrentPeriodID,
                NotificationID = notificationID,
                Description = documentName + " No: " + documentNumber + " has been " + mode + "ed by " + CurrentUserName + " On " + createDate
            };

            notify.Insert();

            return result;
        }

        #endregion
    }
}