using System.Text;
using System.Text.Json;
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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDataProtection();

builder.Services.AddDbContext<YummyDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentityCore<AppUser>(options => {
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;

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
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Lütfen 'Bearer' yazıp boşluk bıraktıktan sonra Token'ınızı giriniz.\r\n\r\nÖrnek: \"Bearer eyJhbGci...\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
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

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.MapControllers();

app.Run();
