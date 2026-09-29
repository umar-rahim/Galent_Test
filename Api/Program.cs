using Api.Data;
using Api.Models;
using Api.Repositories;
using Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var jwtSecret = configuration["JWT_SECRET"];
if (string.IsNullOrWhiteSpace(jwtSecret) || Encoding.UTF8.GetByteCount(jwtSecret) < 32)
{
    throw new InvalidOperationException("Set JWT_SECRET to a random value of at least 32 bytes.");
}

var jwtIssuer = configuration["Jwt:Issuer"] ?? "Galent.Local";
var jwtAudience = configuration["Jwt:Audience"] ?? "Galent.Client";
var jwtKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    var origin = configuration["Frontend:Origin"] ?? "http://localhost:5173";
    options.AddPolicy("LocalClient", policy => policy
        .WithOrigins(origin)
        .AllowAnyMethod()
        .AllowAnyHeader());
});

var sqliteConnection = new SqliteConnectionStringBuilder(
    configuration.GetConnectionString("DefaultConnection") ?? "Data Source=galent.db");
if (!Path.IsPathRooted(sqliteConnection.DataSource))
{
    sqliteConnection.DataSource = Path.Combine(builder.Environment.ContentRootPath, sqliteConnection.DataSource);
}
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(sqliteConnection.ToString()));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = jwtKey,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = ClaimTypes.Name,
        RoleClaimType = ClaimTypes.Role
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var jti = context.Principal?.FindFirst("jti")?.Value;
            if (string.IsNullOrWhiteSpace(jti))
            {
                context.Fail("Token identifier is missing.");
                return;
            }

            var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
            if (await authService.IsRevokedAsync(jti, context.HttpContext.RequestAborted))
            {
                context.Fail("Token has been revoked.");
            }
        }
    };
});

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IHealthService, HealthService>();
builder.Services.AddScoped<ISubmissionRepository, SubmissionRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IRevokedTokenRepository, RevokedTokenRepository>();
builder.Services.AddScoped<ISubmissionService, SubmissionService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IForm1040Calculator, Form1040Calculator>();
builder.Services.AddScoped<IForm1040Validator, Form1040Validator>();
builder.Services.Configure<Form1040PdfOptions>(configuration.GetSection("Pdf"));
builder.Services.AddScoped<IForm1040PdfService, Form1040PdfService>();
builder.Services.Configure<StorageOptions>(configuration.GetSection("Storage"));
builder.Services.AddSingleton<IDocumentStore, LocalFileDocumentStore>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("LocalClient");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Migrations and initial local accounts are applied before serving requests.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.InitializeAsync(services);
}

app.Run();

public partial class Program;
