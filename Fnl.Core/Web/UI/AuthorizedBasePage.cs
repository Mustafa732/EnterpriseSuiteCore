using FnI.Core.Web.UI; // BaseController wala namespace
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;

namespace FnI.Core.Web.UI
{
    // Old: public class AuthorizedBasePage : BasePage
    // New: public class AuthorizedBaseController : BaseController
    public class AuthorizedBaseController : BaseController
    {
        public AuthorizedBaseController(IConfiguration config, IHttpContextAccessor accessor)
            : base(config, accessor) { }

        #region Protected Properties - ViewState ki jagah Session

        protected bool PagingRow
        {
            get
            {
                var val = HttpContext.Session.GetString("PagingRow");
                return !string.IsNullOrEmpty(val) && Convert.ToBoolean(val);
            }
            set => HttpContext.Session.SetString("PagingRow", value.ToString());
        }

        protected DataTable dtPageFeatures
        {
            get
            {
                // Session me DataTable ko JSON ya binary rakhna parhta hai
                // Simple ke liye TempData / ViewBag use karo, ya DB se dobara lao
                var data = TempData["dtPageFeatures"] as DataTable;
                return data;
            }
            set => TempData["dtPageFeatures"] = value;
        }

        protected string Mode
        {
            get => HttpContext.Session.GetString("Mode") ?? string.Empty;
            set => HttpContext.Session.SetString("Mode", value);
        }

        protected int PageIndex
        {
            get
            {
                var val = HttpContext.Session.GetString("PageIndex");
                return string.IsNullOrEmpty(val) ? 0 : Convert.ToInt16(val);
            }
            set => HttpContext.Session.SetString("PageIndex", value.ToString());
        }

        protected string SortExpression
        {
            get => HttpContext.Session.GetString("SortExpression");
            set => HttpContext.Session.SetString("SortExpression", value ?? "");
        }

        protected string SortDirection
        {
            get => HttpContext.Session.GetString("SortDirection") ?? "ASC";
            set => HttpContext.Session.SetString("SortDirection", value);
        }

        #endregion

        #region Grid Sorting - Old GridView_RowCreated ka MVC version

        // Old: GridView_RowCreated(object sender, GridViewRowEventArgs e)
        // New: MVC me DataTable sorting helper

        protected DataView ApplySorting(DataTable dt)
        {
            if (dt == null) return null;

            string sortExp = SortExpression;
            string sortDir = SortDirection;

            if (!string.IsNullOrEmpty(sortExp))
            {
                dt.DefaultView.Sort = $"{sortExp} {sortDir}";
            }
            return dt.DefaultView;
        }

        protected void SetSorting(string sortExpression)
        {
            if (SortExpression == sortExpression)
            {
                // Toggle ASC/DESC
                SortDirection = (SortDirection == "ASC") ? "DESC" : "ASC";
            }
            else
            {
                SortExpression = sortExpression;
                SortDirection = "ASC";
            }
        }

        // View me sorting icon dikhane ke liye helper
        protected string GetSortIcon(string columnName)
        {
            if (SortExpression == columnName)
            {
                return SortDirection == "ASC" ? "asc.gif" : "desc.gif";
            }
            return "";
        }

        #endregion
    }
}