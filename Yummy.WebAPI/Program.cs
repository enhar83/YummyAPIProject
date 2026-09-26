using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Yummy.Business.BackgroundServices;
using Yummy.Business.Managers;
using Yummy.Core.IRepositories;
using Yummy.Core.IUnitOfWork;
using Yummy.Core.Services;
using Yummy.Core.Settings;
using Yummy.Data;
using Yummy.Data.Context;
using Yummy.Data.Repositories;
using Yummy.Entity;
using Yummy.WebAPI.Middlewares;
using Yummy.WebAPI.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDataProtection();

builder.Services.AddDbContext<YummyDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentityCore<AppUser>(options => {
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;

    // e-posta benzersizliği Identity tarafından her kullanıcı oluşturma/güncelleme işleminde (ChangeEmailAsync dahil) kontrol edilir.
    // FindByEmailAsync aynı e-postaya sahip birden fazla kullanıcı bulursa exception fırlattığı için bu ayar zorunludur.
    options.User.RequireUniqueEmail = true;

    // brute-force saldırılarına karşı: 5 hatalı şifre denemesinden sonra hesap 5 dakika kilitlenir.
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
})
.AddRoles<AppRole>()
.AddEntityFrameworkStores<YummyDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Yummy API", Version = "v1" });
    // Http + bearer tipinde tanımlandığı için Swagger "Bearer " ön ekini kendisi ekler; sadece token'ın yapıştırılması yeterlidir.
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Login endpoint'inden aldığınız access token'ı yapıştırınız. 'Bearer' yazmanıza gerek yoktur.\r\n\r\nÖrnek: \"eyJhbGci...\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
    });
});

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("TokenSettings"));

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>)); //open generic kullanımıdır. yani hangi tip istenirse onun için otomatik olarak bir GenericRepository<T> oluşur demektir.
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<ICategoryService, CategoryManager>();
builder.Services.AddScoped<IChefService, ChefManager>();
builder.Services.AddScoped<IContactService, ContactManager>();
builder.Services.AddScoped<IProductService, ProductManager>();
builder.Services.AddScoped<IFeatureService, FeatureManager>();
builder.Services.AddScoped<IEmailService, EmailManager>();
builder.Services.AddScoped<IAppUserService, AppUserManager>();
builder.Services.AddScoped<IAppRoleService, AppRoleManager>();
builder.Services.AddScoped<IJwtService, JwtManager>();
builder.Services.AddScoped<IReservationService, ReservationManager>();
builder.Services.AddScoped<ITestimonialService, TestimonialManager>();
builder.Services.AddScoped<IDiningTableService, DiningTableManager>();

builder.Services.AddHostedService<ReservationStatusWorker>();

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddMaps(typeof(Yummy.Business.Mapping.CategoryMapping).Assembly);
});

// TokenSettings secrets.json içerisinde tutulur ve secrets.json sadece Development ortamında yüklenir.
// Diğer ortamlarda ayarlar eksikse uygulama anlaşılır bir hata mesajıyla açılışta durdurulur.
var jwtSettings = builder.Configuration.GetSection("TokenSettings").Get<JwtSettings>()
    ?? throw new InvalidOperationException("'TokenSettings' ayarları bulunamadı. Development ortamında secrets.json, diğer ortamlarda environment variable (örn. TokenSettings__SecurityKey) üzerinden tanımlanmalıdır.");

if (string.IsNullOrWhiteSpace(jwtSettings.Issuer) || string.IsNullOrWhiteSpace(jwtSettings.Audience))
    throw new InvalidOperationException("'TokenSettings:Issuer' ve 'TokenSettings:Audience' alanları boş olamaz.");

if (string.IsNullOrEmpty(jwtSettings.SecurityKey) || Encoding.UTF8.GetByteCount(jwtSettings.SecurityKey) < 32)
    throw new InvalidOperationException("'TokenSettings:SecurityKey' HS256 için en az 32 byte (256 bit) uzunluğunda olmalıdır.");

if (jwtSettings.AccessTokenExpiration <= 0)
    throw new InvalidOperationException("'TokenSettings:AccessTokenExpiration' 0'dan büyük bir dakika değeri olmalıdır.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecurityKey)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        // imzası ve süresi geçerli olan token için, içerisindeki security stamp db'deki güncel değer ile karşılaştırılır.
        // logout, şifre/e-posta/rol değişikliğinde stamp yenilendiği için eski token'lar süreleri dolmadan reddedilir.
        // sorgu primary key üzerinden yapılır ve sadece SecurityStamp kolonu okunur. Silinmiş kullanıcılar query filter nedeniyle bulunamaz ve reddedilir.
        OnTokenValidated = async context =>
        {
            var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var tokenStamp = context.Principal?.FindFirstValue(JwtManager.SecurityStampClaimType);

            if (!Guid.TryParse(userId, out var id) || string.IsNullOrEmpty(tokenStamp))
            {
                context.Fail("Token içerisinde kullanıcı veya security stamp bilgisi yok.");
                return;
            }

            var dbContext = context.HttpContext.RequestServices.GetRequiredService<YummyDbContext>();
            var currentStamp = await dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => u.SecurityStamp)
                .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

            if (currentStamp == null || currentStamp != tokenStamp)
                context.Fail("Oturum sonlandırılmış.");
        },

        OnChallenge = context =>
        {
            context.HandleResponse();

            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";

            var result = JsonSerializer.Serialize(new { message = "Lütfen işlem yapabilmek için sisteme giriş yapınız." });
            return context.Response.WriteAsync(result);
        },

        OnForbidden = context =>
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";

            var result = JsonSerializer.Serialize(new { message = "Bu alana erişim sağlamak için gerekli yetkiye sahip değilsiniz." });
            return context.Response.WriteAsync(result);
        }
    };
});

// auth ve e-posta gönderen endpoint'lere IP bazlı istek sınırı konur (brute-force ve e-posta spam'ine karşı).
// ⚠️ uygulama reverse proxy (nginx, IIS ARR, load balancer vb.) arkasında çalışacaksa gerçek istemci IP'si için ForwardedHeaders yapılandırılmalıdır;
// aksi halde tüm istekler proxy IP'sinden geliyormuş gibi görünür ve aynı limiti paylaşır.
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy(RateLimitPolicies.Auth, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy(RateLimitPolicies.EmailSending, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();

        var result = JsonSerializer.Serialize(new { message = "Çok fazla istek gönderdiniz. Lütfen biraz bekleyip tekrar deneyin." });
        await context.HttpContext.Response.WriteAsync(result, cancellationToken);
    };
});

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(typeof(Yummy.Business.Validators.CategoryValidators.CategoryCreateValidator).Assembly);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles(); //IWebHostEnvironment'in çalışması için.
app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.MapControllers();

app.Run();
