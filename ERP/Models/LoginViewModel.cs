using System.ComponentModel.DataAnnotations;

namespace ERP.Models
{
    public class LoginViewModel
    {
        [Required]
        public string UserName { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        public bool RememberMe { get; set; } = false;

        public string? ReturnUrl { get; set; }
        public string? ErrorMessage { get; set; }

        // ===== BITWARE THEME - DB SE AAYEGA =====
        public string CompanyName { get; set; } = "BITWARE ERP SYSTEM";
        public string CompanySubText { get; set; } = "Enterprise Resource Planning";
        public string? LogoUrl { get; set; } = null; // /images/company/logo.png
        public string DatabaseName { get; set; } = "Habib_Sugar_HO_2223";
        public string ServerName { get; set; } = "MHS-SERVER";
    }
}