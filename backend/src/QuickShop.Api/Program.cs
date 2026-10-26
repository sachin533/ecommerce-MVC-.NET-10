using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using QuickShop.Api.Common;
using QuickShop.Api.Data;
using QuickShop.Api.Data.Seed;
using QuickShop.Api.Domain;
using QuickShop.Api.Endpoints;
using QuickShop.Api.Security;
using QuickShop.Api.Services;
using QuickShop.Api.Services.Email;
using QuickShop.Api.Services.Payment;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Configuration
// ---------------------------------------------------------------------------
// Sources (in order): appsettings*.json, user secrets (Development), environment
// variables, command line. Secrets (JWT signing key) never live in source — they
// are provided via `dotnet user-secrets` or environment variables (see README).
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key is missing or too short. Provide it outside source control:\n" +
        "  dotnet user-secrets set \"Jwt:Key\" \"<random string, 64+ characters>\" --project backend/src/QuickShop.Api\n" +
        "or set the environment variable Jwt__Key. See README.md.");
}

var paymentProvider = builder.Configuration["Payment:Provider"] ?? "mock";
if (!string.Equals(paymentProvider, "mock", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        $"Payment provider '{paymentProvider}' is not configured in this build. " +
        "Only the clearly labelled mock provider ships here; wire a real gateway behind IPaymentProcessor for production.");
}

// ---------------------------------------------------------------------------
// Services
// ---------------------------------------------------------------------------
builder.Services.AddHttpContextAccessor();
builder.Services.AddProblemDetails();

builder.Services.AddDbContext<QuickShopDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"),
        sql => sql.EnableRetryOnFailure()));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<QuickShopDbContext>()
    // Required for password-reset / email-confirm tokens: registers the "Default"
    // data-protector token provider that UserManager.GeneratePasswordResetTokenAsync needs.
    .AddDefaultTokenProviders();

builder.Services.AddQuickShopSecurity(builder.Configuration);

builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<IAuthCookieService, AuthCookieService>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IEmailSender, DevelopmentEmailSender>();
}
else
{
    builder.Services.AddSingleton<IEmailSender, UnconfiguredEmailSender>();
}

builder.Services.AddSingleton<IPaymentProcessor, MockPaymentProcessor>();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<AddressService>();
builder.Services.AddScoped<WishlistService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<CustomerService>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options => options.AddPolicy("frontend", policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "QuickShop API",
        Version = "v1",
        Description = "QuickShop eCommerce Minimal API: catalog, cart, checkout, orders, admin."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT here for API testing. The browser app uses the HttpOnly cookie instead."
    });

    options.AddSecurityDefinition("Cookie", new OpenApiSecurityScheme
    {
        // Cookie auth is an apiKey-style scheme whose location is the cookie jar.
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Cookie,
        Name = AuthConstants.AuthCookieName,
        Description = "Session cookie set at login (used automatically by the SPA)."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", document), new List<string>() },
        { new OpenApiSecuritySchemeReference("Cookie", document), new List<string>() }
    });
});

var app = builder.Build();

// ---------------------------------------------------------------------------
// Middleware
// ---------------------------------------------------------------------------
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Configuration.GetValue("Swagger:Enabled", app.Environment.IsDevelopment()))
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "QuickShop API v1"));
}

app.UseCors("frontend");
app.UseAuthentication();
app.UseMiddleware<CsrfMiddleware>();
app.UseAuthorization();

// ---------------------------------------------------------------------------
// Route groups
// ---------------------------------------------------------------------------
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", utc = DateTime.UtcNow }))
    .WithName("Health")
    .AllowAnonymous();

app.MapAuthEndpoints();
app.MapProductEndpoints();
app.MapCategoryEndpoints();
app.MapCartEndpoints();
app.MapAddressEndpoints();
app.MapOrderEndpoints();
app.MapWishlistEndpoints();
app.MapAdminEndpoints();

// ---------------------------------------------------------------------------
// Database migrate + idempotent development seed
// ---------------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<QuickShopDbContext>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    var applyMigrations = configuration.GetValue("Data:ApplyMigrations", false)
                          || app.Environment.IsDevelopment();
    var seedRequested = configuration.GetValue("Data:Seed", false)
                        || configuration.GetValue("seed", false); // `dotnet run -- --seed`

    if (applyMigrations)
    {
        await db.Database.MigrateAsync();
    }

    if (seedRequested)
    {
        var report = await DbSeeder.SeedAsync(scope.ServiceProvider);
        app.Logger.LogInformation(
            "Seed finished — categories: {Categories}, products: {Products}, accounts: {Accounts}, orders: {Orders} (skipped existing rows)",
            report.CategoriesAdded, report.ProductsAdded, report.AccountsCreated, report.OrdersAdded);
    }
}

app.Run();

/// <summary>Exposes the entry point to WebApplicationFactory-based integration tests.</summary>
public partial class Program;
