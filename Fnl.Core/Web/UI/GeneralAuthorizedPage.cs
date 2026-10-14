using Common.Utils;
using FnI.BusinessAccessLayer.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Net; // HtmlEncode ke liye

namespace FnI.Core.Web.UI
{
    // Old: public class GeneralAuthorizedPage : AuthorizedBasePage
    // New:.NET 8 MVC
    public class GeneralAuthorizedController : AuthorizedBaseController
    {
        public GeneralAuthorizedController(IConfiguration config, IHttpContextAccessor accessor)
            : base(config, accessor) { }

        #region Class Variables

        private bool _IsCISAdmin = false;
        private bool _IsAdmin = false;
        private bool _IsAdminAllowed = false;

        #endregion

        #region Class Properties

        public bool IsAdminAllowed
        {
            set { _IsAdminAllowed = value; }
        }

        protected string RoleName
        {
            get => HttpContext.Session.GetString("RoleName");
            set => HttpContext.Session.SetString("RoleName", value ?? "");
        }

        #endregion

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            // WebForms me if(!IsPostBack) hota tha, MVC me har GET pe
            if (HttpContext.Request.Method == "GET")
            {
                AuthorizePage();
            }
        }

        #region Helping Methods

        private void AuthorizePage()
        {
            string strMenuID = WebUtility.HtmlEncode(HttpContext.Request.Query["menuID"].ToString());

            if (string.IsNullOrEmpty(strMenuID))
            {
                HttpContext.Response.Redirect($"/InvalidRequest?master=0&errorcode={Enumaration.PageErrorCodes.MenuIDNotMentioned.GetHashCode()}");
                return;
            }

            try
            {
                int menuID = Convert.ToInt16(strMenuID);
                MatchMenuIDWithPageURL(menuID);

                if (!IsUserAuthorizedForPage())
                {
                    HttpContext.Response.Redirect($"/InvalidRequest?master=0&errorcode={Enumaration.PageErrorCodes.NotAuthorized.GetHashCode()}");
                }
            }
            catch (FormatException)
            {
                HttpContext.Response.Redirect($"/InvalidRequest?master=0&errorcode={Enumaration.PageErrorCodes.MenuIDNotMentioned.GetHashCode()}");
            }
        }

        private bool IsUserAuthorizedForPage()
        {
            DataTable dtAuthorize = aspnet_UsersInRoles.GetAll_Roles(CurrentUserID);

            if (dtAuthorize != null && dtAuthorize.Rows.Count > 0)
            {
                DataRow[] drCISAdmin = dtAuthorize.Select("RoleName = 'cisadmin'");
                DataRow[] drAdmin = dtAuthorize.Select("RoleName = 'admin'");

                if (drCISAdmin.Length > 0)
                {
                    _IsCISAdmin = true;
                    RoleName = "cisadmin";
                }

                if (drAdmin.Length > 0)
                {
                    _IsAdmin = true;
                    RoleName = "admin";
                }

                if ((_IsAdmin && _IsAdminAllowed) || _IsCISAdmin)
                    return true;
                else
                    return false;
            }

            return false;
        }

        private void MatchMenuIDWithPageURL(int menuID)
        {
            DataRow drMenu = ApplicationGeneralMenu.GetDataByKey(menuID);

            if (drMenu != null)
            {
                string currentPath = HttpContext.Request.Path.ToString().ToLower();
                string menuPath = drMenu["GeneralMenuPath"].ToString().ToLower();

                if (!currentPath.Contains(menuPath))
                {
                    HttpContext.Response.Redirect($"/InvalidRequest?master=0&errorcode={Enumaration.PageErrorCodes.MenuIDNotMatched.GetHashCode()}");
                }
            }
        }

        #endregion

        #region Protected Methods

        protected new string[] LoadFeatureAttributes(string featureName)
        {
            string[] strArrayFeatureRoles = new string[1];
            strArrayFeatureRoles[0] = RoleName;
            return strArrayFeatureRoles;
        }

        #endregion
    }
}