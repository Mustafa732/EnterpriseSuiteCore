using Common.Globalization;
using DAL.DataAccessLayer;
using ERP.Filters;
using FnI.Core.Web.HttpApplicationState;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// 1. Cache is required for Session
builder.Services.AddDistributedMemoryCache();

// 2. HttpContextAccessor - AppSessions ke liye lazmi
builder.Services.AddHttpContextAccessor();

// 3. MVC
builder.Services.AddControllersWithViews(o => o.Filters.Add<MainMenuFilter>());
builder.Services.AddRazorPages();

// 4. Session - Old Session_Start / Session_End ka Core replacement
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(builder.Configuration.GetValue<int>("SessionSettings:TimeoutMinutes", 288000));
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 5. Auth - Same as old FormsAuthentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = builder.Configuration["AuthSettings:LoginPath"] ?? "/Account/Login";
        options.LogoutPath = builder.Configuration["AuthSettings:LogoutPath"] ?? "/Account/Logout";
        options.AccessDeniedPath = builder.Configuration["AuthSettings:AccessDeniedPath"] ?? "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(builder.Configuration.GetValue<int>("AuthSettings:ExpireTimeSpanMinutes", 288000));
        options.SlidingExpiration = true;
        options.Cookie.Name = ".BitwareAuth";
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// AppSessions configure - UseSession ke baad hi hona chahiye
var accessor = app.Services.GetRequiredService<IHttpContextAccessor>();
FnI.Core.Web.AppSessions.Configure(accessor);
FnI.Core.Helper.Notification.Configure(accessor);
SqlHelper.Configure(app.Configuration);

// ================== OLD Global.asax Application_Start ==================
// Ye line miss thi - isi wajah se fiscal 2122 ki jagah 1819 repeat ho raha tha
try
{
    AppParameters.SetApplicationVariables();
    Console.WriteLine($"[Global.asax Fix] AppParameters Loaded - IsFiscalSystem: {AppParameters.IsFiscalSystem}, IsWelcomeScreen: {AppParameters.IsWelcomeScreen}, LicenseCheck: {AppParameters.LicenseCheck}");
}
catch (Exception ex)
{
    Console.WriteLine($"[Global.asax Fix] AppParameters load failed: {ex.Message}");
    // App start hone do, lekin log me error rahega
}

// ================== Culture Fix - Old Application_BeginRequest ==================
// Old me AppCulture.SetCurrentCulture() har request pe chalta tha
// Tumne DefaultThreadCurrentCulture set kiya tha jo sahi hai, lekin per-request bhi chahiye
var defaultCulture = new CultureInfo(builder.Configuration["AppSettings:Lanuguage-Country"] ?? "en-GB");
CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();

// ORDER IS IMPORTANT: Session -> Auth -> Authorization
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// ================== OLD Global.asax Application_BeginRequest replacement ==================
app.Use(async (context, next) =>
{
    try
    {
        // Old: AppCulture.SetCurrentCulture() - Date format 01 Oct 2021 vs ستمبر fix
        AppCulture.SetCurrentCulture();
    }
    catch { }

    await next();
});

// Tumhara session tracking middleware - UseSession ke baad sahi jagah pe
app.Use(async (context, next) =>
{
    try
    {
        if (context.Session.IsAvailable && context.User.Identity?.IsAuthenticated == true)
        {
            var sessionId = context.Session.Id;
            if (!string.IsNullOrEmpty(sessionId))
            {
                var items = new Dictionary<string, object>
                {
                    ["UserName"] = context.User.Identity.Name ?? "",
                    ["CompanyID"] = context.Session.GetInt32("CurrentCompanyID") ?? 0,
                    ["SessionId"] = sessionId,
                    ["LoginTime"] = DateTime.Now
                };
                Sessions.TrackSession(sessionId, items);
            }
        }
    }
    catch { /* Session tracking fail ho bhi jaye to app crash na ho */ }

    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();