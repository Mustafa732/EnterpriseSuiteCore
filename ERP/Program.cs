using Microsoft.AspNetCore.Authentication.Cookies;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// 1. Cache is required for Session
builder.Services.AddDistributedMemoryCache();

// 2. HttpContextAccessor - AppSessions ke liye lazmi
builder.Services.AddHttpContextAccessor();

// 3. MVC
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// 4. Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(builder.Configuration.GetValue<int>("SessionSettings:TimeoutMinutes", 288000));
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 5. Auth
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

var cultureInfo = new CultureInfo(builder.Configuration["AppSettings:Lanuguage-Country"] ?? "ur-PK");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

var app = builder.Build();

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

// AppSessions configure - UseSession ke baad
var accessor = app.Services.GetRequiredService<IHttpContextAccessor>();
FnI.Core.Web.AppSessions.Configure(accessor);
FnI.Core.Helper.Notification.Configure(accessor);
DAL.DataAccessLayer.SqlHelper.Configure(app.Configuration);

// Tumhara session tracking wala middleware AB sahi jagah pe - UseSession ke baad
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
                Common.Globalization.Sessions.TrackSession(sessionId, items);
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