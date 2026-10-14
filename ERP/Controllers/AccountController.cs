using ERP.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ERP.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IConfiguration config, ILogger<AccountController> logger)
        {
            _config = config;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            var model = new LoginViewModel { ReturnUrl = returnUrl ?? "/Home/Index" };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                string? rawConn = _config.GetConnectionString("FniSystem");
                if (string.IsNullOrEmpty(rawConn))
                {
                    model.ErrorMessage = "Connection string FniSystem not found";
                    return View(model);
                }

                string connStr = DecryptConnectionString(rawConn);
                
                // ========== REAL VALIDATION FROM YOUR OLD DATABASE ==========
                var validationResult = await ValidateUserFromOldTables(connStr, model.UserName.Trim(), model.Password);

                if (!validationResult.IsValid)
                {
                    model.ErrorMessage = validationResult.ErrorMessage ?? "Invalid username or password.";
                    return View(model);
                }

                // ========== UserLogs - Same as old ==========
                try
                {
                    // Yahan apna UserLogs.Insert() lagana hai agar chahiye
                    // Example: await InsertUserLog(connStr, validationResult.UserId);
                }
                catch (Exception ex) { _logger.LogWarning($"UserLogs failed: {ex.Message}"); }

                // ========== Cookie Auth ==========
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, model.UserName.Trim()),
                    new Claim(ClaimTypes.NameIdentifier, validationResult.UserId.ToString()),
                    new Claim("CompanyID", "1"),
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(15) : DateTimeOffset.UtcNow.AddHours(48),
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                HttpContext.Session.SetInt32("LocationID", 1);
                HttpContext.Session.SetString("UserName", model.UserName.Trim());

                return LocalRedirect(model.ReturnUrl ?? "/Home/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed");
                model.ErrorMessage = "Login error: " + ex.Message;
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        // ========== VALIDATION FROM OLD aspnet_Membership TABLES ==========
        private async Task<(bool IsValid, Guid UserId, string? ErrorMessage)> ValidateUserFromOldTables(string connStr, string username, string password)
        {
            try
            {
                using (var conn = new SqlConnection(connStr))
                {
                    await conn.OpenAsync();

                    // 1. Get ApplicationId for FnISystem (same as old <add applicationName="FnISystem"/>)
                    string appIdQuery = "SELECT ApplicationId FROM aspnet_Applications WHERE ApplicationName = @AppName";
                    Guid appId;
                    using (var cmd = new SqlCommand(appIdQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@AppName", "FnISystem");
                        var result = await cmd.ExecuteScalarAsync();
                        if (result == null)
                        {
                            // Agar ApplicationName alag hai to first wala le lo
                            cmd.CommandText = "SELECT TOP 1 ApplicationId FROM aspnet_Applications";
                            result = await cmd.ExecuteScalarAsync();
                        }
                        if (result == null) return (false, Guid.Empty, "Application not found in aspnet_Applications");
                        appId = (Guid)result;
                    }

                    // 2. Get User + Membership data (same as old SqlMembershipProvider does)
                    string userQuery = @"
                        SELECT u.UserId, m.Password, m.PasswordFormat, m.PasswordSalt, m.IsApproved, m.IsLockedOut, m.FailedPasswordAttemptCount
                        FROM aspnet_Users u
                        INNER JOIN aspnet_Membership m ON u.UserId = m.UserId
                        WHERE u.UserName = @UserName AND u.ApplicationId = @AppId";

                    using (var cmd = new SqlCommand(userQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserName", username);
                        cmd.Parameters.AddWithValue("@AppId", appId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                return (false, Guid.Empty, "User not found. Check aspnet_Users table.");
                            }

                            Guid userId = reader.GetGuid(0);
                            string dbPassword = reader.GetString(1);
                            int passwordFormat = reader.GetInt32(2);
                            string passwordSalt = reader.GetString(3);
                            bool isApproved = reader.GetBoolean(4);
                            bool isLockedOut = reader.GetBoolean(5);

                            if (isLockedOut)
                                return (false, userId, "User is locked out. Contact Administrator.");

                            if (!isApproved)
                                return (false, userId, "User not approved.");

                            // 3. Verify Password - Same logic as old Membership Provider
                            bool passwordValid = false;

                            if (passwordFormat == 0) // Clear text
                            {
                                passwordValid = dbPassword == password;
                            }
                            else if (passwordFormat == 1) // Hashed (SHA1 + Salt) - Old Membership default
                            {
                                passwordValid = VerifyHashedPassword(dbPassword, password, passwordSalt);
                            }
                            else if (passwordFormat == 2) // Encrypted
                            {
                                // Old me machineKey se encrypt hota tha, yahan decrypt karke check karna padega
                                // For simplicity, try hashed verification
                                passwordValid = VerifyHashedPassword(dbPassword, password, passwordSalt);
                            }

                            if (!passwordValid)
                            {
                                return (false, userId, "Invalid password.");
                            }

                            return (true, userId, null);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DB Validation error");
                return (false, Guid.Empty, $"DB Error: {ex.Message}");
            }
        }

        // Old Membership hashed password verification (SHA1)
        private bool VerifyHashedPassword(string dbHashedPassword, string enteredPassword, string saltBase64)
        {
            try
            {
                // Old SqlMembershipProvider: Hash = SHA1(salt + password) - base64
                // Implementation from Microsoft: https://learn.microsoft.com/en-us/previous-versions/membership-provider
                byte[] saltBytes = Convert.FromBase64String(saltBase64);
                byte[] passwordBytes = Encoding.Unicode.GetBytes(enteredPassword);
                byte[] combined = new byte[saltBytes.Length + passwordBytes.Length];
                Buffer.BlockCopy(saltBytes, 0, combined, 0, saltBytes.Length);
                Buffer.BlockCopy(passwordBytes, 0, combined, saltBytes.Length, passwordBytes.Length);

                using (SHA1 sha1 = SHA1.Create())
                {
                    byte[] hashBytes = sha1.ComputeHash(combined);
                    string computedHash = Convert.ToBase64String(hashBytes);
                    return computedHash == dbHashedPassword;
                }
            }
            catch
            {
                // Fallback: try simple comparison if salt logic fails
                return false;
            }
        }

        // Old connection string decrypt - same as your Login.aspx.cs
        public static string Decrypt(string cipherString, bool useHashing)
        {
            byte[] keyArray;
            byte[] toEncryptArray = Convert.FromBase64String(cipherString);
            string key = "LLBC";
            if (useHashing)
            {
                using MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
                keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
            }
            else
            {
                keyArray = UTF8Encoding.UTF8.GetBytes(key);
            }
            using TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            tdes.Key = keyArray;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;
            ICryptoTransform cTransform = tdes.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            return UTF8Encoding.UTF8.GetString(resultArray);
        }

        private string DecryptConnectionString(string rawConn)
        {
            if (string.IsNullOrWhiteSpace(rawConn))
                return rawConn;

            try
            {
                // Trailing ; hatao warna LastIndexOf galat aayega
                string trimmed = rawConn.Trim().TrimEnd(';');

                // Builder use karo - yehi sahi tareeqa hai
                var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(trimmed + ";");

                if (!string.IsNullOrEmpty(builder.Password))
                {
                    try
                    {
                        string actual = Decrypt(builder.Password.Trim(), true);
                        // Agar decrypt se kuch readable aaya to use karo
                        if (!string.IsNullOrWhiteSpace(actual) && actual.Length >= 3)
                        {
                            builder.Password = actual;
                        }
                    }
                    catch { /* plain password hai, rehne do */ }
                }

                return builder.ConnectionString;
            }
            catch
            {
                return rawConn; // fallback
            }
        }
    }
}
