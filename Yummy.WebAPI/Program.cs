using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Lütfen 'Bearer' yazýp boþluk býraktýktan sonra Token'ýnýzý giriniz.\r\n\r\nÖrnek: \"Bearer eyJhbGci...\"",
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

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>)); //opengeneric kullanýmýdýr. yani hangi tip istenirse onun için otomatik olarak bir GenericRepository<T> oluþtur demektir. 
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<ICategoryService, CategoryManager>();
builder.Services.AddScoped<IChefService, ChefManager>();
builder.Services.AddScoped<IProductService, ProductManager>();
builder.Services.AddScoped<IFeatureService, FeatureManager>();
builder.Services.AddScoped<IEmailService, EmailManager>();
builder.Services.AddScoped<IAppUserService, AppUserManager>();
builder.Services.AddScoped<IJwtService, JwtManager>();

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddMaps(typeof(Yummy.Business.Mapping.CategoryMapping).Assembly);
});

var jwtSettings = builder.Configuration.GetSection("TokenSettings").Get<JwtSettings>();
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

        ValidIssuer = jwtSettings!.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecurityKey)),
        ClockSkew = TimeSpan.Zero
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

app.UseStaticFiles(); //IWebHostEnvironment'in çalýþmasý için. 
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<GlobalExceptionMiddleware>(); 
app.MapControllers();

app.Run();
