using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.WebEncoders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using SimpleNorthwind.Application;
using SimpleNorthwind.Infrastructure;
using SimpleNorthwind.Infrastructure.Options;
using SimpleNorthwind.Infrastructure.Serialization;
using SimpleNorthwind.Infrastructure.Time;
using SimpleNorthwind.WebApi.Filters;
using SimpleNorthwind.WebApi.Middleware;
using SimpleNorthwind.WebApi.Web.Http;
using SimpleNorthwind.WebApi.Web.RealTime;

// 機密加密小工具：dotnet run --project src/SimpleNorthwind.WebApi -- encrypt "<plaintext>"
if (args is ["encrypt", var plaintext, ..])
{
    Console.WriteLine(SimpleNorthwind.Infrastructure.DependencyInjection.EncryptSecret(plaintext));
    return;
}

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(context.Configuration));

builder.Services
    .AddControllersWithViews(options =>
    {
        // ApiLog 外層（記錄所有呼叫含驗證失敗）→ Validation 內層。
        // UI controller 以 [SkipApiLog] 排除，僅稽核其 loopback /api/* 呼叫。
        options.Filters.Add<ApiLogActionFilter>();
        options.Filters.Add<ValidationActionFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new ClientLocalDateTimeJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableClientLocalDateTimeJsonConverter());
    });

// Razor HtmlEncoder 預設只放行 BasicLatin，會把繁中編成 &#xXXXX; 實體（HTML 膨脹）。
// 放行全 Unicode（仍編碼 < > & " 等危險字元，安全），讓繁中 view 輸出乾淨。僅影響 Razor，不影響 API JSON。
builder.Services.Configure<WebEncoderOptions>(options =>
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();

// 即時稽核推播（SignalR）：Hub server→client 廣播；broadcaster 以介面隔離，filter 不直接碰 SignalR。
// IHubContext 為 thread-safe → broadcaster 註冊為 singleton（見 26-即時稽核推播 §4）。
builder.Services.AddSignalR();
builder.Services.AddSingleton<IApiLogBroadcaster, SignalRApiLogBroadcaster>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)  // 預設 scheme = JWT（/api/* 行為不變：未驗證回 401 JSON）
    .AddJwtBearer()
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        // UI 專用 Cookie scheme：未驗證 / 無權限 → 302 導頁（非 401 JSON）。UI controller 顯式指定此 scheme。
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        options.AccessDeniedPath = "/account/denied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);  // 對齊 JWT 有效期；SignIn 時另以 JWT exp 設 ExpiresUtc
        options.SlidingExpiration = false;
    });

// 以解密後的 JwtOptions 設定 Bearer 驗證參數
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;
        bearer.MapInboundClaims = false;
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// MVC UI → loopback typed client 打自身 /api/*（單一事實 + 稽核重用，見 19-前端架構與整合 §4）。
// 走 HttpClientFactory（禁 new HttpClient()）；DelegatingHandler 補 Bearer + 時區。
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<BearerTimeZoneHandler>();
builder.Services.AddHttpClient<NorthwindApiClient>(client =>
    {
        var baseUrl = builder.Configuration["Api:BaseUrl"]
            ?? throw new InvalidOperationException("缺少設定 Api:BaseUrl（loopback 自呼叫位址）。");
        client.BaseAddress = new Uri(baseUrl);
    })
    .AddHttpMessageHandler<BearerTimeZoneHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Simple Northwind WebApi", Version = "v1" });

    // 納入 XML doc comments（controller / action 的 summary、remarks、param、response）
    var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
    options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "貼上登入取得的 JWT（不含 Bearer 前綴）"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 設定 client 時區預設（App:DefaultTimeZone）；middleware 缺 X-Time-Zone header 時退回此值（非 UTC）
var appOptions = app.Services.GetRequiredService<IOptions<AppOptions>>().Value;
try
{
    ClientTimeZoneAccessor.SetDefault(TimeZoneInfo.FindSystemTimeZoneById(appOptions.DefaultTimeZone));
}
catch (TimeZoneNotFoundException)
{
    ClientTimeZoneAccessor.SetDefault(TimeZoneInfo.Utc);
}

app.UseSerilogRequestLogging();

app.UseStaticFiles();  // wwwroot：tz.js、靜態資源（UI 用）

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(handler => handler.Run(async context =>
{
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await Results.Problem(
        statusCode: StatusCodes.Status500InternalServerError,
        title: "伺服器發生未預期錯誤。").ExecuteAsync(context);
}));

app.UseMiddleware<ClientTimeZoneMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();  // API：attribute route（/api/*）
app.MapControllerRoute(  // UI：MVC 慣例 route（UI controller 另以 attribute route 美化路徑）
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<ApiLogHub>("/hubs/apilogs");  // 稽核即時推播 Hub（Cookie 同源；未登入連線遭拒）

app.Run();

// 供 E2E 測試的 WebApplicationFactory<Program> 使用
public partial class Program;
