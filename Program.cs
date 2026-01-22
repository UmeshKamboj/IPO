using IPOClient.Data;
using IPOClient.Middleware;
using IPOClient.Repositories.Implementations;
using IPOClient.Repositories.Interfaces;
using IPOClient.Seeds;
using IPOClient.Services.BackgroundServices;
using IPOClient.Services.Implementations;
using IPOClient.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// =======================
// Controllers with JSON options
// =======================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialize enums as strings (e.g., "Success" instead of 1)
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

        // Use camelCase for property names
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// =======================
// Swagger with JWT
// =======================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IPO Client API",
        Version = "v1"
    });

    // JWT Bearer token config
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter JWT Bearer token **without** 'Bearer ' prefix",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    c.AddSecurityDefinition("Bearer", securityScheme);

    var securityRequirement = new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    };

    c.AddSecurityRequirement(securityRequirement);
});

// =======================
// Http Context
// =======================
builder.Services.AddHttpContextAccessor();

// =======================
// Database
// =======================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<IPOClientDbContext>(options =>
    options.UseSqlServer(connectionString));

// =======================
// Repositories & Services
// =======================
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IIPORepository, IPORepository>();
builder.Services.AddScoped<IIPOGroupRepository, IPOGroupRepository>();
builder.Services.AddScoped<IIPOBuyerPlaceOrderRepository, IPOBuyerPlaceOrderRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IIPOService, IPOService>();
builder.Services.AddScoped<IIPOGroupService, IPOGroupService>();
builder.Services.AddScoped<IIPOBuyerPlaceOrderService, IPOBuyerPlaceOrderService>();

// =======================
// Background Services
// =======================
builder.Services.AddHostedService<LogCleanupService>();
// =======================
// Session
// =======================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// =======================
// JWT Authentication
// =======================
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

var key = Encoding.UTF8.GetBytes(secretKey);

// Disable default claim type mapping to preserve custom claims
Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();
System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();


// =======================
// CORS
// =======================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5), // Allow 5 minutes clock skew for token refresh
            RoleClaimType = "role", // Explicitly set role claim type
            NameClaimType = "sub" // Explicitly set name claim type
        };

        // Enable automatic token refresh
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers["Token-Expired"] = "true";
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// =======================
// Middleware
// =======================
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "IPO Client API V1");
});

// =======================
// CORS Middleware
// =======================
app.UseCors("AllowAll");
// Allow both HTTP and HTTPS
// app.UseHttpsRedirection(); // Commented out to allow HTTP connections
app.UseSession();

// API Logging Middleware (before authentication)
app.UseMiddleware<ApiLoggingMiddleware>();

app.UseAuthentication();

// Auto Token Refresh Middleware (after authentication, before authorization)
app.UseMiddleware<AutoTokenRefreshMiddleware>();

app.UseAuthorization();

app.MapControllers();

// =======================
// Database Initialization & Seed
// =======================
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<IPOClientDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            // Ensure database is created and apply any pending migrations
            logger.LogInformation("Checking database existence and applying migrations...");

            // Create database if it doesn't exist
            bool dbCreated = await db.Database.EnsureCreatedAsync();

            if (dbCreated)
            {
                logger.LogInformation("Database created successfully from entities.");
            }
            else
            {
                // Database exists, check for pending migrations
                var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    logger.LogInformation($"Applying {pendingMigrations.Count()} pending migrations...");
                    await db.Database.MigrateAsync();
                    logger.LogInformation("Migrations applied successfully.");
                }
                else
                {
                    logger.LogInformation("Database is up to date. No migrations needed.");
                }
            }

            // Seed initial data
            await DatabaseSeeder.SeedAsync(db);
            logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database. Application will continue to run.");
        }
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Failed to initialize database scope");
}

app.Run();
