using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
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

builder.Services.AddDbContext<YummyDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentityCore<AppUser>(options => {
    options.Password.RequireDigit = false; 
    options.Password.RequiredLength = 6;
})
.AddRoles<AppRole>()
.AddEntityFrameworkStores<YummyDbContext>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>)); //opengeneric kullanýmýdýr. yani hangi tip istenirse onun için otomatik olarak bir GenericRepository<T> oluþtur demektir. 
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<ICategoryService, CategoryManager>();
builder.Services.AddScoped<IChefService, ChefManager>();
builder.Services.AddScoped<IProductService, ProductManager>();
builder.Services.AddScoped<IContactService, ContactManager>();
builder.Services.AddScoped<IFeatureService, FeatureManager>();
builder.Services.AddScoped<IEmailService, EmailManager>();

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddMaps(typeof(Yummy.Business.Mapping.CategoryMapping).Assembly);
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

app.UseAuthorization();

app.UseMiddleware<GlobalExceptionMiddleware>(); 
app.MapControllers();

app.Run();
