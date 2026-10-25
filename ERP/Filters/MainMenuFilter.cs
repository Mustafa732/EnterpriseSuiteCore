using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using FnI.BusinessAccessLayer;
using FnI.BusinessAccessLayer.Security;
using FnI.BusinessAccessLayer.Setup;
using FnI.Core.Web.HttpApplicationState;

namespace ERP.Filters
{
    public class MainMenuItem
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public string Icon { get; set; } = "";
        public List<MainMenuItem> Children { get; set; } = new List<MainMenuItem>();
    }

    // Program.cs:
    // builder.Services.AddControllersWithViews(o => o.Filters.Add<MainMenuFilter>());
    public class MainMenuFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.Controller is not Controller controller) return;

            var action = context.RouteData.Values["action"]?.ToString() ?? "";
            if (action.Equals("Welcome", StringComparison.OrdinalIgnoreCase)) return;
            if (controller.ViewBag.MainMenus != null) return;
            if (context.HttpContext.User.Identity?.IsAuthenticated != true) return;

            var list = new List<MainMenuItem>();
            try
            {
                var userId = Guid.Parse(context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());
                DataTable dt = MenusInRoles.GetMenus_By_CompanyID_UserID(userId);

                var caneName = AppParameters.NameForCaneModule;
                if (!string.IsNullOrEmpty(caneName))
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        row["MenuName"] = row["MenuName"].ToString()!.Trim().Replace("Cane", caneName);
                    }
                }

                foreach (var root in dt.Select("IsRoot = 1"))
                {
                    list.Add(Build(root, dt, controller, 0));
                }
            }
            catch
            {
            }

            controller.ViewBag.MainMenus = list;
        }

        public void OnActionExecuted(ActionExecutedContext context) { }

        private static MainMenuItem Build(DataRow row, DataTable dt, Controller controller, int depth)
        {
            var path = row["MenuPath"]?.ToString() ?? "#";
            var item = new MainMenuItem
            {
                Name = row["MenuName"]?.ToString() ?? "",
                Path = path == "#" ? "javascript:;" : controller.Url.Content("~" + path)
            };

            if (depth < 8)
            {
                foreach (var child in dt.Select("ParentMenu = " + row["MenuID"]))
                {
                    item.Children.Add(Build(child, dt, controller, depth + 1));
                }
            }
            return item;
        }
    }
}
