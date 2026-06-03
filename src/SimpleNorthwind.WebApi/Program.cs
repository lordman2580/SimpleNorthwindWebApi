using System.Text;
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
    .AddControllers(options =>
    {
        // ApiLog 外層（記錄所有呼叫含驗證失敗）→ Validation 內層
        options.Filters.Add<ApiLogActionFilter>();
        options.Filters.Add<ValidationActionFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new ClientLocalDateTimeJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableClientLocalDateTimeJsonConverter());
    });

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

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
app.MapControllers();

app.Run();

// 供 E2E 測試的 WebApplicationFactory<Program> 使用
public partial class Program;
