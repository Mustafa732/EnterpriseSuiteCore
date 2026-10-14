using System;

namespace ERP.Models
{
    public class WelcomeViewModel
    {
        public int SelectedBusinessId { get; set; }
        public int SelectedCompanyId { get; set; }
        public List<BusinessItem> BusinessList { get; set; } = new();
        public List<CompanyItem> Companies { get; set; } = new();
        public List<PeriodItem> Periods { get; set; } = new();
    }

    public class BusinessItem
    {
        public int BusinessId { get; set; }
        public string BusinessName { get; set; } = "";
    }

    public class CompanyItem
    {
        public int CompanyID { get; set; }
        public string CompanyName { get; set; } = "";
    }

    public class PeriodItem
    {
        public int PeriodID { get; set; }
        public int CompanyID { get; set; }
        public int FiscalID { get; set; }
        public int ChartOfAccountID { get; set; }
        public int BaseCurrencyID { get; set; }
        public string PeriodSetName { get; set; } = "";
        public string PeriodYear { get; set; } = "";
        public string PeriodTypeName { get; set; } = "";
        public string PeriodNumber { get; set; } = "";
        public string Template { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string COADescription { get; set; } = "";
    }
}
