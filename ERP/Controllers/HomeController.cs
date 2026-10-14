using ERP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Security.Claims;
using FnI.BusinessAccessLayer.Setup;
using FnI.BusinessAccessLayer.Security;
using FnI.Core.Web.HttpApplicationState;

namespace ERP.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        public HomeController(ILogger<HomeController> logger) { _logger = logger; }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("CurrentCompanyID") == null)
                return RedirectToAction("Welcome");
            ViewBag.CompanyName = HttpContext.Session.GetString("CompanyName");
            ViewBag.PeriodName = HttpContext.Session.GetString("PeriodName");
            return View();
        }

        public IActionResult Welcome()
        {
            var model = new WelcomeViewModel();
            try
            {
                Guid currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());

                // 1. Business - Same as FillCompanyDDL()
                DataTable dtBusiness = SET_Business.GetBusniessByUser(currentUserId.ToString());
                foreach (DataRow row in dtBusiness.Rows)
                {
                    model.BusinessList.Add(new BusinessItem
                    {
                        BusinessId = Convert.ToInt32(row["BusinessId"]),
                        BusinessName = row["BusinessName"].ToString() ?? ""
                    });
                }
                // Old: if dtBusiness.Rows.Count==1 then hide ddl - we auto select first
                int sessionBiz = HttpContext.Session.GetInt32("SelectedBusinessForWelcome") ?? 0;
                model.SelectedBusinessId = sessionBiz > 0 ? sessionBiz : model.BusinessList.FirstOrDefault()?.BusinessId ?? 0;

                // 2. Operating Units - Same as ddlBusiness_SelectedIndexChanged
                if (model.SelectedBusinessId > 0)
                {
                    DataTable dtCompanies = UsersInCompanies.GetAllData_ByKey(currentUserId, model.SelectedBusinessId);
                    foreach (DataRow row in dtCompanies.Rows)
                    {
                        model.Companies.Add(new CompanyItem
                        {
                            CompanyID = Convert.ToInt32(row["CompanyID"]),
                            CompanyName = row["CompanyName"].ToString() ?? ""
                        });
                    }
                }

                // 3. Fiscal Years - IMPORTANT FIX
                int selectedCompany = HttpContext.Session.GetInt32("SelectedCompanyForWelcome") ?? 0;
                model.SelectedCompanyId = selectedCompany;

                if (selectedCompany > 0)
                {
                    // FIX: Old me bina IsFiscalSystem filter ke bhi try hota tha
                    // Pehle bina filter ke lo taake saare fiscals aayen jaise old screenshot me
                    DataTable dtPeriods = SET_CompanyPeriod.GetDataForWelcome(selectedCompany, currentUserId, AppParameters.IsFiscalSystem);

                    _logger.LogInformation($"GetDataForWelcome Company={selectedCompany} User={currentUserId} Count={dtPeriods.Rows.Count}");

                    foreach (DataRow row in dtPeriods.Rows)
                    {
                        model.Periods.Add(new PeriodItem
                        {
                            PeriodID = Convert.ToInt32(row["PeriodID"]),
                            CompanyID = Convert.ToInt32(row["CompanyID"]),
                            FiscalID = Convert.ToInt32(row["FiscalID"]),
                            ChartOfAccountID = Convert.ToInt32(row["ChartOfAccountID"]),
                            BaseCurrencyID = Convert.ToInt32(row["BaseCurrencyID"]),
                            PeriodSetName = row["PeriodSetName"].ToString() ?? "",
                            PeriodYear = row["PeriodYear"].ToString() ?? "",
                            PeriodTypeName = row["PeriodTypeName"]?.ToString() ?? "",
                            PeriodNumber = row["PeriodNumber"]?.ToString() ?? "",
                            Template = row["Template"]?.ToString() ?? "",
                            StartDate = Convert.ToDateTime(row["StartDate"]),
                            EndDate = Convert.ToDateTime(row["EndDate"]),
                            COADescription = row["COADescription"]?.ToString() ?? "Habib Sugar COA"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Welcome load failed");
            }

            ViewBag.GeneralMenus = GetMenusForCurrentUser();
            return View(model);
        }

        [HttpPost]
        public IActionResult ChangeBusiness(int businessId)
        {
            HttpContext.Session.SetInt32("SelectedBusinessForWelcome", businessId);
            HttpContext.Session.Remove("SelectedCompanyForWelcome");
            return RedirectToAction("Welcome");
        }

        [HttpPost]
        public IActionResult SelectCompany(int companyId)
        {
            // Same as rpt_Units_ItemCommand -> FillCompanyPeriodDGV
            HttpContext.Session.SetInt32("SelectedCompanyForWelcome", companyId);
            return RedirectToAction("Welcome");
        }

        [HttpPost]
        public IActionResult SelectPeriod(int periodId, int companyId, int fiscalId, int coaId, int baseCurrencyId)
        {
            HttpContext.Session.SetInt32("CurrentCompanyID", companyId);
            HttpContext.Session.SetInt32("CurrentPeriodID", periodId);
            HttpContext.Session.SetInt32("CurrentFiscalID", fiscalId);
            HttpContext.Session.SetInt32("CurrentChartOfAccountID", coaId);
            HttpContext.Session.SetInt32("CurrentCompanyBaseCurrencyID", baseCurrencyId);

            // ParentPeriod overwrite - same as old
            try
            {
                DataRow dr = SET_PeriodCustom.Get_SET_PeriodCustom(periodId);
                if (dr != null && dr["ParentPeriodID"].ToString() != "")
                    HttpContext.Session.SetInt32("CurrentPeriodID", Convert.ToInt32(dr["ParentPeriodID"]));
            }
            catch { }

            // Name fetch for display - same as old Default logic
            try
            {
                DataTable dt = SET_CompanyPeriod.GetDataForWelcome(companyId, Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
                var found = dt.AsEnumerable().FirstOrDefault(r => Convert.ToInt32(r["PeriodID"]) == periodId);
                if (found != null)
                {
                    HttpContext.Session.SetString("CompanyName", found["PeriodSetName"].ToString() + " " + found["COADescription"].ToString());
                    HttpContext.Session.SetString("PeriodName", found["PeriodSetName"].ToString() + " (" + found["PeriodYear"].ToString() + ")");
                }
            }
            catch { }

            HttpContext.Session.SetString("CurrentUserIP", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "");
            return RedirectToAction("Index");
        }

        private List<dynamic> GetMenusForCurrentUser()
        {
            var list = new List<dynamic>();
            try
            {
                Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());
                DataTable dt = MenusInUsers.GetMenus_By_RoleName(userId); // Same as old General.Master.cs CreateMenues()
                if (dt.Rows.Count == 0) return list;
                var roots = dt.Select("IsRoot = 1");
                foreach (DataRow r in roots)
                {
                    string menuName = r["GeneralMenuName"].ToString() ?? "";
                    string icon = "fa fa-desktop";
                    if (menuName == "Welcome") icon = "fa fa-home";
                    else if (menuName == "Security") icon = "fa fa-lock";
                    else if (menuName == "Setup Forms") icon = "fa fa-wpforms";

                    var children = dt.Select($"ParentMenu = {r["GeneralMenuID"]}");
                    var childList = new List<dynamic>();
                    foreach (DataRow c in children)
                    {
                        childList.Add(new
                        {
                            Name = c["GeneralMenuName"].ToString(),
                            Path = c["GeneralMenuPath"].ToString() == "#" ? "javascript:;" : Url.Content("~" + c["GeneralMenuPath"].ToString())
                        });
                    }
                    list.Add(new
                    {
                        Name = menuName,
                        Path = r["GeneralMenuPath"].ToString() == "#" ? "javascript:;" : Url.Content("~" + r["GeneralMenuPath"].ToString()),
                        Icon = icon,
                        Children = childList
                    });
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "Menu bind fail"); }
            return list;
        }
    }
}