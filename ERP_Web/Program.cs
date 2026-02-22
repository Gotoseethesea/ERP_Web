using AntDesign.ProLayout;
using Append.Blazor.Printing;
using ERP_Web.Repository;
using ERP_Web.Components;
using ERP_Web.Core.Login;
using ERP_Web.Core.Service;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using System.Net;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAntDesign();
//builder.Services.AddInteractiveStringLocalizer();
builder.Services.AddLocalization();

builder.Services.Configure<ProSettings>(builder.Configuration.GetSection("ProSettings"));

// Program.cs
builder.Services.AddAntDesign();

//builder.Services.AddScoped<IComponentIdGenerator, GuidComponentIdGenerator>();
// Add services to the container.
//builder.Services.AddRazorComponents()
//    .AddInteractiveServerComponents()
//    .AddInteractiveWebAssemblyComponents();

//#if (full)
//builder.Services.AddInteractiveStringLocalizer();
//builder.Sbuilder.Services.AddLocalization();

//builder.Services.AddRazorComponents()
//    .AddInteractiveWebAssemblyComponents()
//    .AddAuthenticationStateSerialization();

// 正确写法：所有配置加在同一个AddRazorComponents调用后面
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization(); // 把这个移到一起


builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddControllers(); // 添加控制器服务

// BLAZOR COOKIE Auth Code (begin)
// From: https://github.com/aspnet/Blazor/issues/1554
// HttpContextAccessor
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<HttpContextAccessor>();
builder.Services.AddHttpClient();

// 添加认证服务
// 关键：完整配置 Cookie 选项，确保跨站请求时正确发送和接收 Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        var uri = builder.Configuration["ASPNETCORE_URLS"] ?? string.Empty;
        var baseUri = new Uri(uri.Split(';').FirstOrDefault()?.Trim()); // 取第一个元素     // 按分号分割

        options.Cookie.Name = "ERP.Auth";
        options.LoginPath = "/login"; // 登录页路径
        options.LogoutPath = "/login"; // 登出页路径
        options.ExpireTimeSpan = TimeSpan.FromDays(30); // 设置30天有效期
        options.SlidingExpiration = true; // 每次请求后刷新过期时间
        //options.Cookie.Path = uri.Split(';').FirstOrDefault()?.Trim(); // 根路径

        // 关键修复：明确设置 Cookie 属性
        options.Cookie.Path = "/"; // 根路径
        //options.Cookie.Domain = "localhost"; // 明确指定域名
        options.Cookie.Domain = null; // 显式设为null，自动适配当前访问的域名/IP        
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // 兼容HTTP/HTTPS
        // 开发环境配置
        //if (builder.Environment.IsDevelopment())
        //{
        //options.Cookie.SecurePolicy = CookieSecurePolicy.None; // 允许HTTP
        options.Cookie.SameSite = SameSiteMode.Lax; // 允许跨站请求
                                                    //}
                                                    // 安全配置
                                                    //options.Cookie.HttpOnly = true; // 防止XSS
                                                    //options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // 强制HTTPS
                                                    //options.Cookie.SameSite = SameSiteMode.Lax; // 防止CSRF
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                // Blazor Server异步请求重定向处理，避免API请求跳登录页
                if (context.Request.Path.StartsWithSegments("/api") || context.Request.ContentType == "application/json")
                {
                    context.Response.StatusCode = 401;
                    return Task.CompletedTask;
                }
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            }
        };
    });
// 👇 新增防伪令牌配置，兼容IP访问
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = ".ERP.Antiforgery";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    //options.Cookie.Domain = "";
});
// 添加授权服务
//builder.Services.AddAuthorization();
builder.Services.AddHttpClient();
// 注册一个 HttpClient，启用 Cookie 处理和凭据传递
builder.Services.AddScoped(sp =>
{
    var handler = new HttpClientHandler
    {
        UseCookies = true,
        UseDefaultCredentials = true, // 关键：启用凭据
        AllowAutoRedirect = true,
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            builder.Environment.IsDevelopment() // 开发环境忽略证书
    };
    var uri = builder.Configuration["ASPNETCORE_URLS"] ?? string.Empty;
    var baseUri = new Uri(uri.Split(';').FirstOrDefault()?.Trim()); // 取第一个元素     // 按分号分割

    return new HttpClient(handler)
    {
        BaseAddress = new Uri("https://localhost:9000")
    };
});

// 注册一个命名的 HttpClient，专门用于处理需要 Cookie 的请求
//builder.Services.AddHttpClient("CookieEnabledClient", client =>
//{
//    client.BaseAddress = new Uri("https://localhost:9000/login");
//})

//.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
//{
//    UseCookies = true, // 启用 Cookie 容器
//    AllowAutoRedirect = false, // 禁用自动重定向
//    CookieContainer = new CookieContainer(), // 关键：共享 Cookie 容器
//    UseDefaultCredentials = true, // 携带凭据
//    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
//        builder.Environment.IsDevelopment()
//});

//// 添加CORS服务
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowWithCookie", policy =>
//    {
//        var uri = builder.Configuration["ASPNETCORE_URLS"] ?? string.Empty;
//        var baseUri = new Uri(uri.Split(';').FirstOrDefault()?.Trim()); // 取第一个元素     // 按分号分割

//        policy.WithOrigins("https://localhost:9000", "https://localhost:5000")
//              .AllowAnyHeader()
//              .AllowAnyMethod()
//              .AllowCredentials(); // 必须 // 允许凭证
//    });
//});


// 替换原来的HttpClient注册
builder.Services.AddScoped(sp =>
{
    var handler = new HttpClientHandler
    {
        UseCookies = true,
        UseDefaultCredentials = true,
        AllowAutoRedirect = true,
        // 开发环境忽略所有证书错误
        ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
    };

    // 👇 关键：动态获取当前访问地址，不要写死localhost
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    var request = httpContextAccessor.HttpContext?.Request;
    var baseAddress = request != null
        ? new Uri($"{request.Scheme}://{request.Host}")
        : new Uri("https://localhost:9000"); // 兜底

    return new HttpClient(handler)
    {
        BaseAddress = baseAddress
    };
});

// 命名HttpClient也同步改
builder.Services.AddHttpClient("CookieEnabledClient")
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    UseCookies = true,
    AllowAutoRedirect = false,
    CookieContainer = new CookieContainer(),
    UseDefaultCredentials = true,
    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true // 忽略证书
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWithCookie", policy =>
    {
        // 开发环境直接允许所有源，不用每次换IP改配置，生产环境再替换为正式域名
        policy.SetIsOriginAllowed(origin =>
            builder.Environment.IsDevelopment() ||
            origin.StartsWith("https://localhost") ||
            origin.StartsWith("https://192.168") // 允许所有内网IP段
            ||origin.StartsWith("https://10.0") // 允许所有内网IP段
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});




builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(options =>
    {
        options.DetailedErrors = true; // 显示详细异常
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
    });

// 注册自定义工厂
builder.Services.AddSingleton<CookieEnabledClientFactory>();

// 注册文件存储服务
builder.Services.AddScoped<SqlClient>();
builder.Services.AddScoped<IDbStorage, LocalFileStorage>();


//builder.Services.AddAuthorization();
//在 Program.cs 中添加以下配置
builder.Services.AddAuthorization(options =>
{
    // 1. 注册 LocalAccount 策略
   options.AddPolicy("LocalAccount", policy =>
   {
       // 策略要求：
       policy.RequireAuthenticatedUser(); // 要求已认证用户

       // 可选：添加具体规则
       // policy.RequireRole("LocalUser"); // 要求特定角色
       // policy.RequireClaim("account_type", "local"); // 要求特定声明
   });

    // 2. 其他策略配置（根据实际需要添加）
   options.AddPolicy("AdminOnly", policy =>
       policy.RequireRole("Administrator"));

   options.AddPolicy("CanEdit", policy =>
       policy.RequireClaim("permission", "edit"));

    // 3. 设置默认策略（可选）
   //options.FallbackPolicy = options.GetPolicy("LocalAccount")!;
});


// 必须添加的服务
builder.Services.AddHttpContextAccessor(); // 启用HttpContext访问
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
// 注册认证服务

// 注册HttpContextAccessor (必须)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<ERP_Web.Core.Abstractions.AuthenticationService>();
// 注册PermissionService
builder.Services.AddScoped<PermissionService>();

// 注册必需服务
builder.Services.AddRazorPages();
builder.Services.AddRazorComponents(); // 如果使用 Razor Components（可选，视项目而定）
// builder.Services.AddServerSideBlazor(); // 如果需要 Server-side Blazor（按需添加）
builder.Services.AddScoped<IPrintingService, PrintingService>();  //打印服务
// 注意：服务注册是AddPrinting()

// 新增：注册用户上下文服务，作用域为Scoped（每个请求/Blazor连接独立实例）
builder.Services.AddScoped<IUserContext, UserContext>();


var app = builder.Build();
app.UseCors("AllowWithCookie");


if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseRouting();
app.UseAuthentication(); // 必须在UseRouting之后、UseAuthorization之前
app.UseAuthorization();

//app.UseHttpsRedirection();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection(); // 仅在生产环境启用
}
app.MapControllers(); // 配置路由
//app.MapBlazorHub();   // 映射Blazor Hub
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(ERP_Web.Client._Imports).Assembly);
app.Run();
