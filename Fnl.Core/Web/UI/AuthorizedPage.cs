using System;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Common.Utils;
using FnI.BusinessAccessLayer.Security;
using FnI.BusinessAccessLayer.Setup;
using DAL.DataAccessLayer;

namespace FnI.Core.Web.UI
{
    // Ye tumhara purana AuthorizedPage ka.NET 8 version hai
    public class AuthorizedController : Controller, IActionFilter
    {
        private readonly IConfiguration _config;
        protected DataTable dtPageFeatures;

        public AuthorizedController(IConfiguration config)
        {
            _config = config;
        }

        // WebForms ke Page_Load ki jagah ye chalega
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Base logic yahan rahega agar chahiye
            base.OnActionExecuting(context);

            // Agar querystring me menuID nahi to InvalidRequest
            AuthorizePage();
            LoadPageFeatures();
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
        }

        #region Helper Properties - ye tumhare old CurrentCompanyID etc. ke liye

        // Ye tum AppSessions ya Claims se lete the, ab is tarah:
        protected int CurrentCompanyID => AppSessions.CurrentCompanyID;
        protected int CurrentPeriodID => AppSessions.CurrentPeriodID;
        protected int CurrentFiscalID => AppSessions.CurrentFiscalID;
        protected int CurrentChartOfAccountID => AppSessions.CurrentChartOfAccountID;
        protected Guid CurrentUserID => Guid.Parse(User?.Identity?.Name ?? AppSessions.CurrentUserID.ToString());
        protected string CurrentUserIP => HttpContext?.Connection?.RemoteIpAddress?.ToString();
        protected bool HasCentralServer => false; // pehle Session se aata tha

        #endregion

        #region Authorization Logic - Same as your old code

        private void AuthorizePage()
        {
            var strMenuID = HttpContext.Request.Query["menuID"].ToString();

            if (string.IsNullOrEmpty(strMenuID))
            {
                Response.Redirect($"/InvalidRequest?master=1&errorcode={Enumaration.PageErrorCodes.MenuIDNotMentioned.GetHashCode()}");
                return;
            }

            try
            {
                int menuID = Convert.ToInt32(strMenuID);
                MatchMenuIDWithPageURL(menuID);

                if (!IsUserAuthorizedForPage(menuID))
                {
                    Response.Redirect($"/InvalidRequest?master=1&errorcode={Enumaration.PageErrorCodes.NotAuthorized.GetHashCode()}");
                }
            }
            catch (FormatException)
            {
                Response.Redirect($"/InvalidRequest?master=1&errorcode={Enumaration.PageErrorCodes.MenuIDNotMentioned.GetHashCode()}");
            }
        }

        private bool IsUserAuthorizedForPage(int menuID)
        {
            DataTable dtAuthorize = MenusInRoles.GetMenus_By_MenuID_UserID(menuID, CurrentUserID);
            if (dtAuthorize != null && dtAuthorize.Rows.Count > 0)
                foreach (DataRow dr in dtAuthorize.Rows)
                    if (dr["MenuID"].Equals(menuID))
                        return true;
            return false;
        }

        private void MatchMenuIDWithPageURL(int menuID)
        {
            DataRow drMenu = ApplicationMenu.GetDataByKey(menuID);
            if (drMenu != null)
            {
                string absoluteURLPath = HttpContext.Request.Path.ToString().ToLower();
                var virtualDir = _config["VirtualDirectory"];
                if (!string.IsNullOrEmpty(virtualDir))
                    absoluteURLPath = absoluteURLPath.Replace(virtualDir.ToLower(), string.Empty);

                if (!drMenu["MenuPath"].ToString().ToLower().Contains(absoluteURLPath))
                {
                    Response.Redirect($"/InvalidRequest?master=1&errorcode={Enumaration.PageErrorCodes.MenuIDNotMatched.GetHashCode()}");
                }
            }
        }

        private void LoadPageFeatures()
        {
            string absoluteURLPath = HttpContext.Request.Path.ToString();
            var virtualDir = _config["VirtualDirectory"];
            if (!string.IsNullOrEmpty(virtualDir))
                absoluteURLPath = absoluteURLPath.Replace(virtualDir, string.Empty);

            dtPageFeatures = MenusInRoles.Get_PageFeatureRights(CurrentUserID, absoluteURLPath, CurrentCompanyID);
        }

        protected string[] LoadFeatureAttributes(string featureName)
        {
            if (dtPageFeatures == null) return null;
            DataRow[] drows = dtPageFeatures.Select("MenuName = '" + featureName + "'");
            if (drows.Length > 0)
            {
                string[] strArrayFeatureRoles = new string[drows.Length];
                for (int counter = 0; counter < drows.Length; counter++)
                    strArrayFeatureRoles[counter] = drows[counter]["RoleName"].ToString();
                return strArrayFeatureRoles;
            }
            return null;
        }

        // WebForms ka LoginView disable wala.NET 8 me need nahi, lekin rakh diya hai
        protected void DisableButtons(object lgv_Add, object lgv_Edit) { }

        #endregion

        #region Old Common Methods

        public int GetCompanyID() => CurrentCompanyID;
        public int GetPeriodID() => CurrentPeriodID;
        public int GetFiscalID() => CurrentFiscalID;
        public int GetChartOfAccountID() => CurrentChartOfAccountID;
        public Guid GetCurrentUserID() => CurrentUserID;
        public string GetCurrentUserIP() => CurrentUserIP;
        public bool GetHasCentralServer() => HasCentralServer;

        #endregion
    }
}