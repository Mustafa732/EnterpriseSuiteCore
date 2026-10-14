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
    }
}
