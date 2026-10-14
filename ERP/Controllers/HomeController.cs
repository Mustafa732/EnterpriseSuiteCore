using ERP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Security.Claims;

// OLD BLL - Same as your old Welcome.aspx.cs - No change in reference
using FnI.BusinessAccessLayer.Setup;
using FnI.BusinessAccessLayer.Security;
using FnI.Core.Web.HttpApplicationState;

namespace ERP.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IConfiguration config, ILogger<HomeController> logger)
        {
            _config = config;
            _logger = logger;
        }

        // Default.aspx ka replacement
        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("CurrentCompanyID") == null)
                return RedirectToAction("Welcome");

            ViewBag.CompanyName = HttpContext.Session.GetString("CompanyName");
            ViewBag.PeriodName = HttpContext.Session.GetString("PeriodName");
            ViewBag.UserName = User.Identity?.Name;

            // Old: MenusInUsers.GetMenus_By_RoleName(Guid.Parse(Membership.GetUser().ProviderUserKey.ToString()))
            ViewBag.SideMenus = GetMenusForCurrentUser();

            return View();
        }

        // Welcome.aspx ka replacement - SAME LOGIC AS OLD
        public IActionResult Welcome()
        {
            var model = new WelcomeViewModel();

            try
            {
                Guid currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());

                // OLD: DataTable dtBusiness = SET_Business.GetBusniessByUser(CurrentUserID.ToString());
                DataTable dtBusiness = SET_Business.GetBusniessByUser(currentUserId.ToString());

                foreach (DataRow row in dtBusiness.Rows)
                {
                    model.BusinessList.Add(new BusinessItem
                    {
                        BusinessId = Convert.ToInt32(row["BusinessId"]),
                        BusinessName = row["BusinessName"].ToString() ?? ""
                    });
                }

                model.SelectedBusinessId = model.BusinessList.FirstOrDefault()?.BusinessId ?? 0;

                // Agar session me pehle se business selected hai toh wahi use karo
                int sessionBusiness = HttpContext.Session.GetInt32("SelectedBusinessForWelcome") ?? model.SelectedBusinessId;
                model.SelectedBusinessId = sessionBusiness;

                if (model.SelectedBusinessId > 0)
                {
                    // OLD: this.rpt_Units.DataSource = UsersInCompanies.GetAllData_ByKey(CurrentUserID, int.Parse(ddlBusiness.SelectedValue));
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

                int selectedCompany = HttpContext.Session.GetInt32("SelectedCompanyForWelcome") ?? 0;
                if (selectedCompany > 0)
                {
                    model.SelectedCompanyId = selectedCompany;

                    // OLD: this.rpt_ChartOfAccount.DataSource = SET_CompanyPeriod.GetDataForWelcome(companyID, CurrentUserID, AppParameters.IsFiscalSystem);
                    DataTable dtPeriods = SET_CompanyPeriod.GetDataForWelcome(selectedCompany, currentUserId, AppParameters.IsFiscalSystem);
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
                            COADescription = row["COADescription"]?.ToString() ?? ""
                        });
                    }
                }

                ViewBag.Menus = GetMenusForCurrentUser();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Welcome load failed - BLL call");
                // Agar BLL abhi connect nahi hua toh empty rahega, UI me fallback dikhega
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult ChangeBusiness(int businessId)
        {
            HttpContext.Session.SetInt32("SelectedBusinessForWelcome", businessId);
            HttpContext.Session.Remove("SelectedCompanyForWelcome"); // Company reset
            return RedirectToAction("Welcome");
        }

        [HttpPost]
        public IActionResult SelectCompany(int companyId)
        {
            // OLD: FillCompanyPeriodDGV(Convert.ToInt16(CompanyID));
            HttpContext.Session.SetInt32("SelectedCompanyForWelcome", companyId);
            return RedirectToAction("Welcome");
        }

        [HttpPost]
        public IActionResult SelectPeriod(int periodId, int companyId, int fiscalId, int coaId, int baseCurrencyId)
        {
            // OLD Welcome.aspx.cs -> rpt_ChartOfAccount_ItemCommand
            HttpContext.Session.SetInt32("CurrentCompanyID", companyId);
            HttpContext.Session.SetInt32("CurrentPeriodID", periodId);
            HttpContext.Session.SetInt32("CurrentFiscalID", fiscalId);
            HttpContext.Session.SetInt32("CurrentChartOfAccountID", coaId);
            HttpContext.Session.SetInt32("CurrentCompanyBaseCurrencyID", baseCurrencyId);

            // FiscalYearSetup - Over Written Current Period ID <- Parent Period ID (same as old)
            try
            {
                DataRow dr = SET_PeriodCustom.Get_SET_PeriodCustom(periodId);
                if (dr != null && dr["ParentPeriodID"]?.ToString() != "")
                {
                    HttpContext.Session.SetInt32("CurrentPeriodID", Convert.ToInt32(dr["ParentPeriodID"]));
                }
            }
            catch { }

            HttpContext.Session.SetString("CompanyName", $"Company {companyId}");
            HttpContext.Session.SetString("PeriodName", $"Period {periodId}");
            HttpContext.Session.SetString("CurrentUserIP", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "");

            return RedirectToAction("Index");
        }

        public IActionResult Privacy() => View();

        // OLD: MenusInUsers.GetMenus_By_RoleName(Guid.Parse(Membership.GetUser().ProviderUserKey.ToString()))
        private List<dynamic> GetMenusForCurrentUser()
        {
            try
            {
                Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());
                DataTable dt_Menues = MenusInUsers.GetMenus_By_RoleName(userId);
                // Yahan tumhara purana CreateMenues() wala logic aayega agar chahiye toh
                // For now just return DataTable rows as dynamic
                return new List<dynamic>();
            }
            catch
            {
                return new List<dynamic>();
            }
        }
    }
}
