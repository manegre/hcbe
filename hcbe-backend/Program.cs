using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using HcbeApi.Data;
using HcbeApi.Services;
using HcbeApi.Endpoints;
using HcbeApi.Models;
using HcbeApi.Helpers;
using HcbeApi.Infrastructure;
using BCrypt.Net;
using System.Net;
using System.Net.Sockets;
using System.Threading.RateLimiting;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.RateLimiting;

if (args.Length > 0 && args[0] == "MigrateDatabase")
{
    var migrationConfiguration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .AddEnvironmentVariables()
        .Build();
    var migrationProvider = migrationConfiguration["Database:Provider"] ?? DatabaseConfiguration.Sqlite;
    if (!DatabaseConfiguration.IsPostgreSql(migrationProvider))
    {
        throw new InvalidOperationException("The production migration command requires Database:Provider=PostgreSQL.");
    }
    var migrationConnection = migrationConfiguration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be configured for migrations.");
    var migrationOptions = new DbContextOptionsBuilder<ApplicationDbContext>();
    DatabaseConfiguration.Configure(migrationOptions, migrationProvider, migrationConnection);
    await using var migrationContext = new ApplicationDbContext(migrationOptions.Options);
    await migrationContext.Database.MigrateAsync();
    Console.WriteLine("Database migrations applied successfully.");
    return;
}

// Check if we're running the CreateAdmin command
if (args.Length > 0 && args[0] == "CreateAdmin")
{
    var adminEmail = Environment.GetEnvironmentVariable("HCBE_ADMIN_EMAIL")?.Trim().ToLowerInvariant();
    var adminPassword = Environment.GetEnvironmentVariable("HCBE_ADMIN_PASSWORD");
    if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword) || adminPassword.Length < 12)
    {
        Console.Error.WriteLine("Set HCBE_ADMIN_EMAIL and HCBE_ADMIN_PASSWORD (minimum 12 characters) before running CreateAdmin.");
        Environment.ExitCode = 1;
        return;
    }

    var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
    var commandConfiguration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
        .AddEnvironmentVariables()
        .Build();
    var commandProvider = commandConfiguration["Database:Provider"] ?? DatabaseConfiguration.Sqlite;
    var conn = commandConfiguration.GetConnectionString("DefaultConnection") ?? "Data Source=hcbe.db";
    DatabaseConfiguration.Configure(optionsBuilder, commandProvider, conn);

    using var context = new ApplicationDbContext(optionsBuilder.Options);
    await context.Database.EnsureCreatedAsync();
    if (!DatabaseConfiguration.IsPostgreSql(commandProvider))
    {
        EnsureSqliteSecuritySchema(context);
    }

    var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);

    if (existingUser != null)
    {
        Console.WriteLine("Admin user already exists. Updating...");
        existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);
        existingUser.IsAdmin = true;
        existingUser.FirstName = "Admin";
        existingUser.LastName = "HCBE";
    }
    else
    {
        Console.WriteLine("Creating admin user...");
        var adminUser = new User
        {
            Email = adminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            FirstName = "Admin",
            LastName = "HCBE",
            IsAdmin = true
        };
        context.Users.Add(adminUser);
    }

    await context.SaveChangesAsync();
    Console.WriteLine("✓ Admin user created/updated successfully!");
    Console.WriteLine($"  Email: {adminEmail}");
    Console.WriteLine("  Password: supplied through HCBE_ADMIN_PASSWORD");
    return;
}

var builder = WebApplication.CreateBuilder(args);
// Console-first logging works consistently in local development, containers, and restricted Windows hosts.
builder.Logging.ClearProviders();
if (builder.Environment.IsProduction()) builder.Logging.AddJsonConsole();
else builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = 12 * 1024 * 1024;
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(15);
});
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddExceptionHandler<BadRequestExceptionHandler>();
builder.Services.AddExceptionHandler<UnhandledExceptionHandler>();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] =
            Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);
var signalR = builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 32 * 1024;
});
var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    signalR.AddStackExchangeRedis(redisConnection, options =>
    {
        options.Configuration.ChannelPrefix = RedisChannel.Literal("hcbe");
    });

    if (builder.Environment.IsProduction())
    {
        var encryptionKeys = builder.Configuration["DataProtection:KeyEncryptionKeys"];
        if (string.IsNullOrWhiteSpace(encryptionKeys))
            throw new InvalidOperationException("DataProtection:KeyEncryptionKeys is required in production.");
        var dataProtectionRedis = ConnectionMultiplexer.Connect(redisConnection);
        builder.Services.AddSingleton<IConnectionMultiplexer>(dataProtectionRedis);
        builder.Services.AddDataProtection()
            .SetApplicationName("HCBE Canada")
            .PersistKeysToStackExchangeRedis(dataProtectionRedis, "hcbe:data-protection:key-ring");
        builder.Services.Configure<KeyManagementOptions>(options =>
            options.XmlEncryptor = new AesGcmXmlKeyEncryptor(encryptionKeys));
    }
}
else if (builder.Environment.IsProduction())
{
    throw new InvalidOperationException("ConnectionStrings:Redis is required in production for the SignalR backplane.");
}

// Configure CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// SQLite remains available for local compatibility. Production uses managed PostgreSQL.
var databaseProvider = builder.Configuration["Database:Provider"] ?? DatabaseConfiguration.Sqlite;
var databaseConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be configured.");
if (builder.Environment.IsProduction() && !DatabaseConfiguration.IsPostgreSql(databaseProvider))
{
    throw new InvalidOperationException("Production requires Database:Provider=PostgreSQL and a managed database connection string.");
}
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    DatabaseConfiguration.Configure(options, databaseProvider, databaseConnection));

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"];
if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < 32)
{
    throw new InvalidOperationException(
        "JwtSettings:Secret must be provided through secure configuration and contain at least 32 characters.");
}

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
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            if (!string.IsNullOrWhiteSpace(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                context.Token = accessToken;
            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            var userIdValue = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdValue, out var userId))
            {
                context.Fail("Invalid user identity.");
                return;
            }

            var database = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var user = await database.Users.AsNoTracking()
                .Where(item => item.Id == userId)
                .Select(item => new { item.IsActive, item.IsAdmin })
                .SingleOrDefaultAsync(context.HttpContext.RequestAborted);
            var tokenIsAdmin = context.Principal?.IsInRole("Admin") == true;
            if (user == null || !user.IsActive || tokenIsAdmin != user.IsAdmin)
                context.Fail("Account is inactive or permissions have changed.");
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireAuthenticatedUser().RequireRole("Admin"));
    // Administrative/CMS endpoints already call RequireAuthorization(). Making the
    // secure policy the default prevents a newly added endpoint from accidentally
    // granting members access to the back office.
    options.DefaultPolicy = options.GetPolicy("AdminOnly")!;
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("PublicWrite", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("Authentication", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 8,
                Window = TimeSpan.FromMinutes(5),
                SegmentsPerWindow = 5,
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("PrivacyExport", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.GetUserId()?.ToString() ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("PrivacyWrite", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.GetUserId()?.ToString() ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 2,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
builder.Services.AddHttpClient("AssetProxy", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.MaxResponseContentBufferSize = 5 * 1024 * 1024;
});
builder.Services.AddHttpClient("BrevoTransactional", client =>
{
    client.BaseAddress = new Uri("https://api.brevo.com/v3/");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpContextAccessor();

// Register application services
var objectStorageOptions = builder.Configuration
    .GetSection(ObjectStorageOptions.SectionName)
    .Get<ObjectStorageOptions>() ?? new ObjectStorageOptions();
if (builder.Environment.IsProduction() && !objectStorageOptions.IsS3Compatible)
{
    throw new InvalidOperationException(
        "Production requires ObjectStorage:Provider to be S3, S3Compatible, or R2 so uploads remain available across instances.");
}
objectStorageOptions.Validate();
builder.Services.Configure<ObjectStorageOptions>(
    builder.Configuration.GetSection(ObjectStorageOptions.SectionName));
if (objectStorageOptions.IsS3Compatible)
{
    builder.Services.AddSingleton<IFileStorageService, S3FileStorageService>();
}
else
{
    builder.Services.AddSingleton<IFileStorageService, FileStorageService>();
}
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<IGoogleIdentityTokenValidator, GoogleIdentityTokenValidator>();
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IEventRegistrationService, EventRegistrationService>();
builder.Services.AddScoped<IServiceCaseService, ServiceCaseService>();
builder.Services.AddScoped<IEventCategoryService, EventCategoryService>();
builder.Services.AddScoped<IAssociationService, AssociationService>();
builder.Services.AddScoped<IAssociationPortalService, AssociationPortalService>();
builder.Services.AddScoped<IOpportunityService, OpportunityService>();
builder.Services.AddScoped<INewsService, NewsService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IContentService, ContentService>();
builder.Services.AddScoped<IStatisticService, StatisticService>();
builder.Services.AddScoped<ISettingService, SettingService>();
builder.Services.AddScoped<INavigationService, NavigationService>();
builder.Services.AddScoped<IFooterService, FooterService>();
builder.Services.AddScoped<ICmsContentService, CmsContentService>();
builder.Services.AddSingleton<ICmsContentNotifier, CmsContentNotifier>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ITeamMemberService, TeamMemberService>();
builder.Services.AddScoped<IMembershipApplicationService, MembershipApplicationService>();
builder.Services.AddScoped<INewsletterService, NewsletterService>();
builder.Services.AddScoped<IGrantService, GrantService>();
builder.Services.AddScoped<IConsultationService, ConsultationService>();
builder.Services.AddScoped<IUserAdminService, UserAdminService>();
builder.Services.AddScoped<IPublicSubmissionService, PublicSubmissionService>();
builder.Services.AddScoped<IMemberAccountService, MemberAccountService>();
builder.Services.AddScoped<IMemberExperienceService, MemberExperienceService>();
builder.Services.AddScoped<IEmailSender, ConfiguredEmailSender>();
builder.Services.AddScoped<IEmailOutbox, EmailOutbox>();
builder.Services.AddSingleton<IEmailTemplateRenderer, EmailTemplateRenderer>();
builder.Services.AddHostedService<EmailOutboxWorker>();
builder.Services.AddHostedService<ScheduledCampaignWorker>();
builder.Services.AddHostedService<ScheduledCmsPublishingWorker>();
builder.Services.AddScoped<INewsletterCampaignService, NewsletterCampaignService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<ICommunityService, CommunityService>();
builder.Services.AddScoped<IMentorshipJourneyService, MentorshipJourneyService>();
builder.Services.AddScoped<IImpactAnalyticsService, ImpactAnalyticsService>();
builder.Services.AddScoped<IMessagingService, MessagingService>();
builder.Services.AddScoped<IPartnerService, PartnerService>();
builder.Services.AddScoped<IPrivacyService, PrivacyService>();
builder.Services.AddHostedService<PrivacyRetentionWorker>();
var financeConfiguration = builder.Configuration.GetSection(FinanceOptions.SectionName).Get<FinanceOptions>() ?? new FinanceOptions();
if (financeConfiguration.Enabled && !financeConfiguration.Provider.Equals("Stripe", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException("Finance:Provider must be Stripe when online payments are enabled.");
}
if (financeConfiguration.Enabled &&
    (string.IsNullOrWhiteSpace(financeConfiguration.SecretKey) || string.IsNullOrWhiteSpace(financeConfiguration.WebhookSecret)))
{
    throw new InvalidOperationException("Finance:SecretKey and Finance:WebhookSecret are required when online payments are enabled.");
}
builder.Services.Configure<FinanceOptions>(builder.Configuration.GetSection(FinanceOptions.SectionName));
builder.Services.AddSingleton<IPaymentGateway, StripePaymentGateway>();
builder.Services.AddScoped<IFinanceService, FinanceService>();
builder.Services.AddHostedService<MembershipReminderWorker>();

// Enable static file serving for uploads
builder.Services.Configure<IISServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
});

var app = builder.Build();

if (DatabaseConfiguration.IsPostgreSql(databaseProvider))
{
    if (builder.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
    {
        using var migrationScope = app.Services.CreateScope();
        var migrationContext = migrationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await migrationContext.Database.MigrateAsync();
    }
}
else
{

// Optimized database initialization for Free tier - only runs once, not on every startup
// This prevents excessive CPU usage on cold starts
var initFlagPath = Path.Combine(app.Environment.ContentRootPath, ".db-initialized");
var needsInitialization = !File.Exists(initFlagPath);

if (!DatabaseConfiguration.IsPostgreSql(databaseProvider))
{
    using var securitySchemaScope = app.Services.CreateScope();
    var securitySchemaContext = securitySchemaScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (securitySchemaContext.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
    {
        securitySchemaContext.Database.EnsureCreated();
        EnsureSqliteSecuritySchema(securitySchemaContext);
    }
}

if (needsInitialization)
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Only initialize if database doesn't exist or can't connect
        var dbExists = false;
        try
        {
            dbExists = context.Database.CanConnect();
        }
        catch { }

        if (!dbExists)
        {
            // Apply migrations if they exist, otherwise ensure DB is created
            try 
            { 
                context.Database.Migrate(); 
            }
            catch 
            { 
                // If migrations fail (e.g., no migrations table), fall back to EnsureCreated
                try { context.Database.EnsureCreated(); } catch { }
            }

            // Patch missing columns for existing dev databases (ignore errors if column exists)
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Events ADD COLUMN ImageUrl TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Events ADD COLUMN TitleEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Events ADD COLUMN DescriptionEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Events ADD COLUMN LocationEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE News ADD COLUMN IsPinned INTEGER DEFAULT 0"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE News ADD COLUMN TitleEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE News ADD COLUMN ContentEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE News ADD COLUMN ExcerptEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE News ADD COLUMN ImagePosition TEXT DEFAULT 'center'"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE EventMedia ADD COLUMN CaptionEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Projects ADD COLUMN TitleEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Projects ADD COLUMN DescriptionEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Projects ADD COLUMN LocationEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Projects ADD COLUMN BeneficiariesEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE GrantPrograms ADD COLUMN TitleEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE GrantPrograms ADD COLUMN DescriptionEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE GrantPrograms ADD COLUMN AmountEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE GrantPrograms ADD COLUMN DurationEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE GrantPrograms ADD COLUMN EligibilityCriteriaEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("UPDATE GrantPrograms SET EligibilityCriteriaEn = '[]' WHERE EligibilityCriteriaEn IS NULL"); } catch { }
            try { context.Database.ExecuteSqlRaw("UPDATE GrantPrograms SET EligibilityCriteria = '[]' WHERE EligibilityCriteria IS NULL"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Consultations ADD COLUMN TitleEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Consultations ADD COLUMN DescriptionEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Consultations ADD COLUMN ActionLabelEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Consultations ADD COLUMN SecondaryActionLabelEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Documents ADD COLUMN Description TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Documents ADD COLUMN Icon TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Documents ADD COLUMN Pages TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Documents ADD COLUMN NameEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Documents ADD COLUMN DescriptionEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Documents ADD COLUMN PagesEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Documents ADD COLUMN CategoryEn TEXT"); } catch { }

            // Create Associations table if not exists (dev-safe)
            try { context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS Associations (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                NameEn TEXT,
                Description TEXT,
                DescriptionEn TEXT,
                Province TEXT NOT NULL,
                City TEXT NOT NULL,
                Contact TEXT,
                Phone TEXT,
                President TEXT,
                MemberCount TEXT,
                FoundedYear INTEGER,
                ImageUrl TEXT,
                Website TEXT,
                Domains TEXT,
                DomainsEn TEXT,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            )"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Associations ADD COLUMN NameEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Associations ADD COLUMN DescriptionEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Associations ADD COLUMN DomainsEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("UPDATE Associations SET DomainsEn = '[]' WHERE DomainsEn IS NULL"); } catch { }

            // Projects extra fields
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Projects ADD COLUMN ImageUrl TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Projects ADD COLUMN Type TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Projects ADD COLUMN FundsRaised TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Projects ADD COLUMN Beneficiaries TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Projects ADD COLUMN EndDate TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Projects ADD COLUMN IsActive INTEGER DEFAULT 1"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Projects ADD COLUMN Partners TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("UPDATE Projects SET Title = '' WHERE Title IS NULL"); } catch { }
            try { context.Database.ExecuteSqlRaw("UPDATE Projects SET Location = '' WHERE Location IS NULL"); } catch { }
            try { context.Database.ExecuteSqlRaw("UPDATE Projects SET Type = '' WHERE Type IS NULL"); } catch { }
            try { context.Database.ExecuteSqlRaw("UPDATE Projects SET Status = '' WHERE Status IS NULL"); } catch { }
            try { context.Database.ExecuteSqlRaw("UPDATE Projects SET Description = '' WHERE Description IS NULL"); } catch { }
            try { context.Database.ExecuteSqlRaw("UPDATE Projects SET Budget = '' WHERE Budget IS NULL"); } catch { }
            try { context.Database.ExecuteSqlRaw("UPDATE Projects SET FundsRaised = '' WHERE FundsRaised IS NULL"); } catch { }
            try { context.Database.ExecuteSqlRaw("UPDATE Projects SET Beneficiaries = '' WHERE Beneficiaries IS NULL"); } catch { }
            try { context.Database.ExecuteSqlRaw("UPDATE Projects SET Partners = '[]' WHERE Partners IS NULL OR Partners = ''"); } catch { }
            try { context.Database.ExecuteSqlRaw("UPDATE Projects SET IsActive = 1 WHERE IsActive IS NULL"); } catch { }
            try { context.Database.ExecuteSqlRaw("UPDATE Projects SET UpdatedAt = CreatedAt WHERE UpdatedAt IS NULL"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE PageSections ADD COLUMN TitleEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE PageSections ADD COLUMN ContentEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE ServiceContents ADD COLUMN TitleEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE ServiceContents ADD COLUMN DescriptionEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE ServiceContents ADD COLUMN CategoryEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE ServiceContents ADD COLUMN DetailsEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE ServiceContents ADD COLUMN ExtendedInfoEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE NavigationItems ADD COLUMN LabelEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE FooterLinks ADD COLUMN CategoryEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE FooterLinks ADD COLUMN LabelEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS TeamMembers (
                Id TEXT PRIMARY KEY, Name TEXT NOT NULL, Position TEXT NOT NULL, PositionEn TEXT,
                Region TEXT NOT NULL, RegionEn TEXT, Zone TEXT NOT NULL, ZoneEn TEXT,
                Photo TEXT, Bio TEXT, BioEn TEXT, Email TEXT, IsActive INTEGER NOT NULL DEFAULT 1,
                'Order' INTEGER NOT NULL DEFAULT 0, CreatedAt TEXT NOT NULL, UpdatedAt TEXT NOT NULL
            )"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE TeamMembers ADD COLUMN PositionEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE TeamMembers ADD COLUMN RegionEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE TeamMembers ADD COLUMN ZoneEn TEXT"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE TeamMembers ADD COLUMN BioEn TEXT"); } catch { }

            try { context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS MembershipApplications (
                Id TEXT PRIMARY KEY,
                FirstName TEXT NOT NULL,
                LastName TEXT NOT NULL,
                Email TEXT NOT NULL,
                Phone TEXT,
                City TEXT,
                Province TEXT,
                Profession TEXT,
                Expertise TEXT,
                Motivation TEXT,
                Status INTEGER NOT NULL DEFAULT 0,
                MemberId TEXT,
                CreatedAt TEXT NOT NULL,
                ReviewedAt TEXT,
                FOREIGN KEY (MemberId) REFERENCES Members(Id) ON DELETE SET NULL
            )"); } catch { }
            try { context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MembershipApplications_Email ON MembershipApplications(Email)"); } catch { }
            try { context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MembershipApplications_Status ON MembershipApplications(Status)"); } catch { }
            try { context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MembershipApplications_CreatedAt ON MembershipApplications(CreatedAt)"); } catch { }

            try { context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS NewsletterSubscriptions (
                Id TEXT PRIMARY KEY,
                Email TEXT NOT NULL,
                FullName TEXT NOT NULL,
                PreferredLanguage TEXT NOT NULL,
                ConsentAcceptedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                Source TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            )"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE NewsletterSubscriptions RENAME COLUMN FirstName TO FullName"); } catch { }
            try { context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_NewsletterSubscriptions_Email ON NewsletterSubscriptions(Email)"); } catch { }
            try { context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_NewsletterSubscriptions_IsActive ON NewsletterSubscriptions(IsActive)"); } catch { }
            try { context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_NewsletterSubscriptions_PreferredLanguage ON NewsletterSubscriptions(PreferredLanguage)"); } catch { }

            try { context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS GrantPrograms (
                Id TEXT PRIMARY KEY,
                Title TEXT NOT NULL,
                Description TEXT NOT NULL,
                Icon TEXT NOT NULL,
                Amount TEXT NOT NULL,
                Duration TEXT NOT NULL,
                EligibilityCriteria TEXT NOT NULL,
                ApplicationUrl TEXT,
                DisplayOrder INTEGER NOT NULL DEFAULT 0,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            )"); } catch { }

            try { context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS NewsAttachments (
                Id TEXT PRIMARY KEY,
                NewsId TEXT NOT NULL,
                FileName TEXT NOT NULL,
                Url TEXT NOT NULL,
                ContentType TEXT NOT NULL,
                SizeBytes INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                FOREIGN KEY (NewsId) REFERENCES News(Id) ON DELETE CASCADE
            )"); } catch { }
            try { context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_NewsAttachments_NewsId ON NewsAttachments(NewsId)"); } catch { }

            try { context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS EventMedia (
                Id TEXT PRIMARY KEY,
                EventId TEXT NOT NULL,
                MediaType TEXT NOT NULL,
                Url TEXT NOT NULL,
                FileName TEXT,
                ContentType TEXT,
                SizeBytes INTEGER,
                Caption TEXT,
                CaptionEn TEXT,
                DisplayOrder INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                FOREIGN KEY (EventId) REFERENCES Events(Id) ON DELETE CASCADE
            )"); } catch { }
            try { context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_EventMedia_EventId ON EventMedia(EventId)"); } catch { }

            try { context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS EventAttachments (
                Id TEXT PRIMARY KEY,
                EventId TEXT NOT NULL,
                FileName TEXT NOT NULL,
                Url TEXT NOT NULL,
                ContentType TEXT NOT NULL,
                SizeBytes INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                FOREIGN KEY (EventId) REFERENCES Events(Id) ON DELETE CASCADE
            )"); } catch { }
            try { context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_EventAttachments_EventId ON EventAttachments(EventId)"); } catch { }

            // Seed base data only on first initialization
            DbSeeder.Seed(context, app.Environment);

            // Mark as initialized to prevent running again on subsequent startups
            try
            {
                File.Create(initFlagPath).Dispose();
                Console.WriteLine("✓ Database initialized successfully");
            }
            catch (Exception ex)
            {
                // If we can't create the flag file, log but don't fail
                Console.WriteLine($"⚠ Warning: Could not create initialization flag: {ex.Message}");
            }
        }
        else
        {
            // Database exists but flag file doesn't - create flag to prevent future initialization
            try
            {
                File.Create(initFlagPath).Dispose();
                Console.WriteLine("✓ Database already exists, skipping initialization");
            }
            catch { }
        }
    }
}

// Ensure MembershipApplications table exists on existing databases
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS MembershipApplications (
            Id TEXT PRIMARY KEY,
            FirstName TEXT NOT NULL,
            LastName TEXT NOT NULL,
            Email TEXT NOT NULL,
            Phone TEXT,
            City TEXT,
            Province TEXT,
            Profession TEXT,
            Expertise TEXT,
            Motivation TEXT,
            Status INTEGER NOT NULL DEFAULT 0,
            MemberId TEXT,
            CreatedAt TEXT NOT NULL,
            ReviewedAt TEXT,
            FOREIGN KEY (MemberId) REFERENCES Members(Id) ON DELETE SET NULL
        )");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MembershipApplications_Email ON MembershipApplications(Email)");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MembershipApplications_Status ON MembershipApplications(Status)");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MembershipApplications_CreatedAt ON MembershipApplications(CreatedAt)");

        context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS NewsletterSubscriptions (
            Id TEXT PRIMARY KEY,
            Email TEXT NOT NULL,
            FullName TEXT NOT NULL,
            PreferredLanguage TEXT NOT NULL,
            ConsentAcceptedAt TEXT NOT NULL,
            IsActive INTEGER NOT NULL DEFAULT 1,
            Source TEXT NOT NULL,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL
        )");
        try { context.Database.ExecuteSqlRaw("ALTER TABLE NewsletterSubscriptions RENAME COLUMN FirstName TO FullName"); } catch { }
        context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_NewsletterSubscriptions_Email ON NewsletterSubscriptions(Email)");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_NewsletterSubscriptions_IsActive ON NewsletterSubscriptions(IsActive)");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_NewsletterSubscriptions_PreferredLanguage ON NewsletterSubscriptions(PreferredLanguage)");

        context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS GrantPrograms (
            Id TEXT PRIMARY KEY,
            Title TEXT NOT NULL,
            Description TEXT NOT NULL,
            Icon TEXT NOT NULL,
            Amount TEXT NOT NULL,
            Duration TEXT NOT NULL,
            EligibilityCriteria TEXT NOT NULL,
            ApplicationUrl TEXT,
            DisplayOrder INTEGER NOT NULL DEFAULT 0,
            IsActive INTEGER NOT NULL DEFAULT 1,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL
        )");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_GrantPrograms_DisplayOrder ON GrantPrograms(DisplayOrder)");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_GrantPrograms_IsActive ON GrantPrograms(IsActive)");

        context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS NewsAttachments (
            Id TEXT PRIMARY KEY,
            NewsId TEXT NOT NULL,
            FileName TEXT NOT NULL,
            Url TEXT NOT NULL,
            ContentType TEXT NOT NULL,
            SizeBytes INTEGER NOT NULL,
            CreatedAt TEXT NOT NULL,
            FOREIGN KEY (NewsId) REFERENCES News(Id) ON DELETE CASCADE
        )");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_NewsAttachments_NewsId ON NewsAttachments(NewsId)");

        context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS EventMedia (
            Id TEXT PRIMARY KEY,
            EventId TEXT NOT NULL,
            MediaType TEXT NOT NULL,
            Url TEXT NOT NULL,
            FileName TEXT,
            ContentType TEXT,
            SizeBytes INTEGER,
            Caption TEXT,
            CaptionEn TEXT,
            DisplayOrder INTEGER NOT NULL DEFAULT 0,
            CreatedAt TEXT NOT NULL,
            FOREIGN KEY (EventId) REFERENCES Events(Id) ON DELETE CASCADE
        )");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_EventMedia_EventId ON EventMedia(EventId)");

        context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS EventAttachments (
            Id TEXT PRIMARY KEY,
            EventId TEXT NOT NULL,
            FileName TEXT NOT NULL,
            Url TEXT NOT NULL,
            ContentType TEXT NOT NULL,
            SizeBytes INTEGER NOT NULL,
            CreatedAt TEXT NOT NULL,
            FOREIGN KEY (EventId) REFERENCES Events(Id) ON DELETE CASCADE
        )");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_EventAttachments_EventId ON EventAttachments(EventId)");

        try { context.Database.ExecuteSqlRaw("ALTER TABLE Events ADD COLUMN RegistrationMode TEXT NOT NULL DEFAULT 'External'"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE Events ADD COLUMN AllowWaitlist INTEGER NOT NULL DEFAULT 1"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE Events ADD COLUMN RestrictMeetingLinkToRegistrants INTEGER NOT NULL DEFAULT 0"); } catch { }
        context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS EventRegistrations (
            Id TEXT PRIMARY KEY,
            EventId TEXT NOT NULL,
            MemberId TEXT NOT NULL,
            Status TEXT NOT NULL DEFAULT 'Confirmed',
            ConfirmationCode TEXT NOT NULL,
            AccessibilityNeeds TEXT,
            AdminNotes TEXT,
            RegisteredAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL,
            CancelledAt TEXT,
            CheckedInAt TEXT,
            ReminderSentAt TEXT,
            FOREIGN KEY (EventId) REFERENCES Events(Id) ON DELETE CASCADE,
            FOREIGN KEY (MemberId) REFERENCES Members(Id) ON DELETE RESTRICT
        )");
        context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_EventRegistrations_ConfirmationCode ON EventRegistrations(ConfirmationCode)");
        context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_EventRegistrations_EventId_MemberId ON EventRegistrations(EventId, MemberId)");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_EventRegistrations_EventId_Status_RegisteredAt ON EventRegistrations(EventId, Status, RegisteredAt)");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_EventRegistrations_MemberId ON EventRegistrations(MemberId)");

        context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS ServiceCases (
            Id TEXT PRIMARY KEY, TicketNumber TEXT NOT NULL, MemberId TEXT NOT NULL,
            Category TEXT NOT NULL, Subject TEXT NOT NULL, Description TEXT NOT NULL,
            Status TEXT NOT NULL DEFAULT 'Submitted', Priority TEXT NOT NULL DEFAULT 'Normal',
            AssignedToUserId TEXT, InternalNotes TEXT, CreatedAt TEXT NOT NULL, UpdatedAt TEXT NOT NULL,
            LastResponseAt TEXT, ResolvedAt TEXT,
            FOREIGN KEY (MemberId) REFERENCES Members(Id) ON DELETE RESTRICT,
            FOREIGN KEY (AssignedToUserId) REFERENCES Users(Id) ON DELETE SET NULL
        )");
        context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_ServiceCases_TicketNumber ON ServiceCases(TicketNumber)");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_ServiceCases_Status_Priority_UpdatedAt ON ServiceCases(Status, Priority, UpdatedAt)");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_ServiceCases_MemberId_UpdatedAt ON ServiceCases(MemberId, UpdatedAt)");
        context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS ServiceCaseMessages (
            Id TEXT PRIMARY KEY, ServiceCaseId TEXT NOT NULL, AuthorUserId TEXT NOT NULL,
            Body TEXT NOT NULL, IsInternal INTEGER NOT NULL DEFAULT 0, CreatedAt TEXT NOT NULL,
            FOREIGN KEY (ServiceCaseId) REFERENCES ServiceCases(Id) ON DELETE CASCADE,
            FOREIGN KEY (AuthorUserId) REFERENCES Users(Id) ON DELETE RESTRICT
        )");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_ServiceCaseMessages_ServiceCaseId_CreatedAt ON ServiceCaseMessages(ServiceCaseId, CreatedAt)");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_ServiceCaseMessages_AuthorUserId ON ServiceCaseMessages(AuthorUserId)");
        context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS ServiceCaseAttachments (
            Id TEXT PRIMARY KEY, ServiceCaseId TEXT NOT NULL, UploadedByUserId TEXT NOT NULL,
            FileName TEXT NOT NULL, Url TEXT NOT NULL, ContentType TEXT NOT NULL, SizeBytes INTEGER NOT NULL,
            IsInternal INTEGER NOT NULL DEFAULT 0, CreatedAt TEXT NOT NULL,
            FOREIGN KEY (ServiceCaseId) REFERENCES ServiceCases(Id) ON DELETE CASCADE
        )");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_ServiceCaseAttachments_ServiceCaseId ON ServiceCaseAttachments(ServiceCaseId)");

        try { context.Database.ExecuteSqlRaw("ALTER TABLE Events ADD COLUMN ImageUrl TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE Events ADD COLUMN TitleEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE Events ADD COLUMN DescriptionEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE Events ADD COLUMN LocationEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE News ADD COLUMN IsPinned INTEGER DEFAULT 0"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE News ADD COLUMN TitleEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE News ADD COLUMN ContentEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE News ADD COLUMN ExcerptEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE News ADD COLUMN ImagePosition TEXT DEFAULT 'center'"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE EventMedia ADD COLUMN CaptionEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE Projects ADD COLUMN TitleEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE Projects ADD COLUMN DescriptionEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE Projects ADD COLUMN LocationEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE Projects ADD COLUMN BeneficiariesEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE Projects ADD COLUMN ImageUrl TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE Projects ADD COLUMN Type TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE Projects ADD COLUMN FundsRaised TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE Projects ADD COLUMN Beneficiaries TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE Projects ADD COLUMN EndDate TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE Projects ADD COLUMN IsActive INTEGER DEFAULT 1"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE Projects ADD COLUMN Partners TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("UPDATE Projects SET Title = '' WHERE Title IS NULL"); } catch { }
        try { context.Database.ExecuteSqlRaw("UPDATE Projects SET Location = '' WHERE Location IS NULL"); } catch { }
        try { context.Database.ExecuteSqlRaw("UPDATE Projects SET Type = '' WHERE Type IS NULL"); } catch { }
        try { context.Database.ExecuteSqlRaw("UPDATE Projects SET Status = '' WHERE Status IS NULL"); } catch { }
        try { context.Database.ExecuteSqlRaw("UPDATE Projects SET Description = '' WHERE Description IS NULL"); } catch { }
        try { context.Database.ExecuteSqlRaw("UPDATE Projects SET Budget = '' WHERE Budget IS NULL"); } catch { }
        try { context.Database.ExecuteSqlRaw("UPDATE Projects SET FundsRaised = '' WHERE FundsRaised IS NULL"); } catch { }
        try { context.Database.ExecuteSqlRaw("UPDATE Projects SET Beneficiaries = '' WHERE Beneficiaries IS NULL"); } catch { }
        try { context.Database.ExecuteSqlRaw("UPDATE Projects SET Partners = '[]' WHERE Partners IS NULL OR Partners = ''"); } catch { }
        try { context.Database.ExecuteSqlRaw("UPDATE Projects SET IsActive = 1 WHERE IsActive IS NULL"); } catch { }
        try { context.Database.ExecuteSqlRaw("UPDATE Projects SET UpdatedAt = CreatedAt WHERE UpdatedAt IS NULL"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE PageSections ADD COLUMN TitleEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE PageSections ADD COLUMN ContentEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE ServiceContents ADD COLUMN TitleEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE ServiceContents ADD COLUMN DescriptionEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE ServiceContents ADD COLUMN CategoryEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE ServiceContents ADD COLUMN DetailsEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE ServiceContents ADD COLUMN ExtendedInfoEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE NavigationItems ADD COLUMN LabelEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE FooterLinks ADD COLUMN CategoryEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE FooterLinks ADD COLUMN LabelEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS TeamMembers (
            Id TEXT PRIMARY KEY, Name TEXT NOT NULL, Position TEXT NOT NULL, PositionEn TEXT,
            Region TEXT NOT NULL, RegionEn TEXT, Zone TEXT NOT NULL, ZoneEn TEXT,
            Photo TEXT, Bio TEXT, BioEn TEXT, Email TEXT, IsActive INTEGER NOT NULL DEFAULT 1,
            'Order' INTEGER NOT NULL DEFAULT 0, CreatedAt TEXT NOT NULL, UpdatedAt TEXT NOT NULL
        )"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE TeamMembers ADD COLUMN PositionEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE TeamMembers ADD COLUMN RegionEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE TeamMembers ADD COLUMN ZoneEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE TeamMembers ADD COLUMN BioEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE GrantPrograms ADD COLUMN TitleEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE GrantPrograms ADD COLUMN DescriptionEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE GrantPrograms ADD COLUMN AmountEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE GrantPrograms ADD COLUMN DurationEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE GrantPrograms ADD COLUMN EligibilityCriteriaEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("UPDATE GrantPrograms SET EligibilityCriteriaEn = '[]' WHERE EligibilityCriteriaEn IS NULL"); } catch { }
        try { context.Database.ExecuteSqlRaw("UPDATE GrantPrograms SET EligibilityCriteria = '[]' WHERE EligibilityCriteria IS NULL"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE Consultations ADD COLUMN TitleEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE Consultations ADD COLUMN DescriptionEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE Consultations ADD COLUMN ActionLabelEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE Consultations ADD COLUMN SecondaryActionLabelEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE Documents ADD COLUMN Description TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE Documents ADD COLUMN Icon TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE Documents ADD COLUMN Pages TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE Documents ADD COLUMN NameEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE Documents ADD COLUMN DescriptionEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE Documents ADD COLUMN PagesEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE Documents ADD COLUMN CategoryEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS Associations (
            Id TEXT PRIMARY KEY,
            Name TEXT NOT NULL,
            NameEn TEXT,
            Description TEXT,
            DescriptionEn TEXT,
            Province TEXT NOT NULL,
            City TEXT NOT NULL,
            Contact TEXT,
            Phone TEXT,
            President TEXT,
            MemberCount TEXT,
            FoundedYear INTEGER,
            ImageUrl TEXT,
            Website TEXT,
            Domains TEXT,
            DomainsEn TEXT,
            IsActive INTEGER NOT NULL DEFAULT 1,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL
        )"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE Associations ADD COLUMN NameEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE Associations ADD COLUMN DescriptionEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE Associations ADD COLUMN DomainsEn TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("UPDATE Associations SET DomainsEn = '[]' WHERE DomainsEn IS NULL"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE Users ADD COLUMN MemberId TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_MemberId ON Users(MemberId)"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE MembershipApplications ADD COLUMN PasswordHash TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE NewsletterSubscriptions ADD COLUMN UnsubscribeToken TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("UPDATE NewsletterSubscriptions SET UnsubscribeToken = lower(hex(randomblob(24))) WHERE UnsubscribeToken IS NULL OR UnsubscribeToken = ''"); } catch { }
        try { context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_NewsletterSubscriptions_UnsubscribeToken ON NewsletterSubscriptions(UnsubscribeToken)"); } catch { }
        context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS NewsletterCampaigns (
            Id TEXT PRIMARY KEY,
            Subject TEXT NOT NULL,
            SubjectEn TEXT,
            Body TEXT NOT NULL,
            BodyEn TEXT,
            Status TEXT NOT NULL DEFAULT 'Draft',
            RecipientCount INTEGER NOT NULL DEFAULT 0,
            SentCount INTEGER NOT NULL DEFAULT 0,
            FailedCount INTEGER NOT NULL DEFAULT 0,
            LastError TEXT,
            CreatedByUserId TEXT NOT NULL,
            CreatedAt TEXT NOT NULL,
            SentAt TEXT
        )");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_NewsletterCampaigns_CreatedAt ON NewsletterCampaigns(CreatedAt)");
        context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS PasswordResetTokens (
            Id TEXT PRIMARY KEY,
            UserId TEXT NOT NULL,
            TokenHash TEXT NOT NULL,
            ExpiresAt TEXT NOT NULL,
            UsedAt TEXT,
            CreatedAt TEXT NOT NULL,
            FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
        )");
        context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_PasswordResetTokens_TokenHash ON PasswordResetTokens(TokenHash)");

        context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS Consultations (
            Id TEXT PRIMARY KEY,
            Title TEXT NOT NULL,
            Description TEXT NOT NULL,
            Icon TEXT NOT NULL,
            LayoutType TEXT NOT NULL DEFAULT 'card',
            ActionUrl TEXT,
            ActionLabel TEXT,
            SecondaryActionUrl TEXT,
            SecondaryActionLabel TEXT,
            AccentColor TEXT NOT NULL DEFAULT 'emerald',
            DisplayOrder INTEGER NOT NULL DEFAULT 0,
            IsActive INTEGER NOT NULL DEFAULT 1,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL
        )");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Consultations_DisplayOrder ON Consultations(DisplayOrder)");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Consultations_IsActive ON Consultations(IsActive)");

        context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS PublicSubmissions (
            Id TEXT PRIMARY KEY,
            Type TEXT NOT NULL,
            FirstName TEXT NOT NULL,
            LastName TEXT NOT NULL,
            Email TEXT NOT NULL,
            Phone TEXT,
            Subject TEXT,
            City TEXT,
            Details TEXT NOT NULL,
            MetadataJson TEXT,
            Status TEXT NOT NULL DEFAULT 'Pending',
            CreatedAt TEXT NOT NULL,
            ReviewedAt TEXT
        )");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_PublicSubmissions_Type_Status_CreatedAt ON PublicSubmissions(Type, Status, CreatedAt)");

        context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS MentorshipApplications (
            Id TEXT PRIMARY KEY,
            MemberId TEXT NOT NULL,
            Role TEXT NOT NULL,
            ProfessionalSummary TEXT NOT NULL,
            Expertise TEXT NOT NULL,
            Objectives TEXT NOT NULL,
            Availability TEXT NOT NULL,
            PreferredLanguage TEXT NOT NULL DEFAULT 'fr',
            ConsentToShare INTEGER NOT NULL DEFAULT 0,
            Status TEXT NOT NULL DEFAULT 'Pending',
            CommitteeNotes TEXT,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL,
            ReviewedAt TEXT,
            FOREIGN KEY (MemberId) REFERENCES Members(Id) ON DELETE CASCADE
        )");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MentorshipApplications_MemberId_Role_Status ON MentorshipApplications(MemberId, Role, Status)");
        context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS MentorshipMatches (
            Id TEXT PRIMARY KEY,
            MentorApplicationId TEXT NOT NULL,
            MenteeApplicationId TEXT NOT NULL,
            Status TEXT NOT NULL DEFAULT 'Proposed',
            MentorAccepted INTEGER NOT NULL DEFAULT 0,
            MenteeAccepted INTEGER NOT NULL DEFAULT 0,
            CommitteeNotes TEXT,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL,
            ActivatedAt TEXT,
            CompletedAt TEXT,
            FOREIGN KEY (MentorApplicationId) REFERENCES MentorshipApplications(Id) ON DELETE RESTRICT,
            FOREIGN KEY (MenteeApplicationId) REFERENCES MentorshipApplications(Id) ON DELETE RESTRICT
        )");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MentorshipMatches_Status ON MentorshipMatches(Status)");
        context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS NetworkingProfiles (
            Id TEXT PRIMARY KEY,
            MemberId TEXT NOT NULL,
            Headline TEXT NOT NULL,
            Bio TEXT NOT NULL,
            Expertise TEXT NOT NULL,
            Sectors TEXT NOT NULL,
            City TEXT,
            Province TEXT,
            IsVisible INTEGER NOT NULL DEFAULT 0,
            AllowContactRequests INTEGER NOT NULL DEFAULT 0,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL,
            FOREIGN KEY (MemberId) REFERENCES Members(Id) ON DELETE CASCADE
        )");
        context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_NetworkingProfiles_MemberId ON NetworkingProfiles(MemberId)");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_NetworkingProfiles_IsVisible_AllowContactRequests ON NetworkingProfiles(IsVisible, AllowContactRequests)");
        context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS ConnectionRequests (
            Id TEXT PRIMARY KEY,
            RequesterMemberId TEXT NOT NULL,
            RecipientMemberId TEXT NOT NULL,
            Message TEXT NOT NULL,
            Status TEXT NOT NULL DEFAULT 'Pending',
            CreatedAt TEXT NOT NULL,
            RespondedAt TEXT,
            FOREIGN KEY (RequesterMemberId) REFERENCES Members(Id) ON DELETE RESTRICT,
            FOREIGN KEY (RecipientMemberId) REFERENCES Members(Id) ON DELETE RESTRICT
        )");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_ConnectionRequests_RecipientMemberId_Status_CreatedAt ON ConnectionRequests(RecipientMemberId, Status, CreatedAt)");
        context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS PrivateConversations (
            Id TEXT PRIMARY KEY,
            MemberOneId TEXT NOT NULL,
            MemberTwoId TEXT NOT NULL,
            RelationshipType TEXT NOT NULL,
            RelationshipId TEXT NOT NULL,
            Status TEXT NOT NULL DEFAULT 'Active',
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL,
            LastMessageAt TEXT,
            FOREIGN KEY (MemberOneId) REFERENCES Members(Id) ON DELETE RESTRICT,
            FOREIGN KEY (MemberTwoId) REFERENCES Members(Id) ON DELETE RESTRICT
        )");
        context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_PrivateConversations_MemberOneId_MemberTwoId ON PrivateConversations(MemberOneId, MemberTwoId)");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_PrivateConversations_LastMessageAt ON PrivateConversations(LastMessageAt)");
        context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS PrivateMessages (
            Id TEXT PRIMARY KEY,
            ConversationId TEXT NOT NULL,
            SenderMemberId TEXT NOT NULL,
            Body TEXT NOT NULL,
            CreatedAt TEXT NOT NULL,
            ReadAt TEXT,
            FOREIGN KEY (ConversationId) REFERENCES PrivateConversations(Id) ON DELETE CASCADE,
            FOREIGN KEY (SenderMemberId) REFERENCES Members(Id) ON DELETE RESTRICT
        )");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_PrivateMessages_ConversationId_CreatedAt ON PrivateMessages(ConversationId, CreatedAt)");
        context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS ConversationReports (
            Id TEXT PRIMARY KEY,
            ConversationId TEXT NOT NULL,
            ReporterMemberId TEXT NOT NULL,
            Reason TEXT NOT NULL,
            Status TEXT NOT NULL DEFAULT 'Open',
            AdminNotes TEXT,
            CreatedAt TEXT NOT NULL,
            ResolvedAt TEXT,
            FOREIGN KEY (ConversationId) REFERENCES PrivateConversations(Id) ON DELETE CASCADE,
            FOREIGN KEY (ReporterMemberId) REFERENCES Members(Id) ON DELETE RESTRICT
        )");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_ConversationReports_Status_CreatedAt ON ConversationReports(Status, CreatedAt)");

        context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS Partners (
            Id TEXT PRIMARY KEY,
            Name TEXT NOT NULL,
            NameEn TEXT,
            Description TEXT,
            DescriptionEn TEXT,
            LogoUrl TEXT,
            WebsiteUrl TEXT,
            AltText TEXT,
            AltTextEn TEXT,
            IsFeatured INTEGER NOT NULL DEFAULT 1,
            IsActive INTEGER NOT NULL DEFAULT 1,
            DisplayOrder INTEGER NOT NULL DEFAULT 0,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL
        )");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Partners_DisplayOrder ON Partners(DisplayOrder)");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Partners_IsActive_IsFeatured ON Partners(IsActive, IsFeatured)");

        DbSeeder.SeedGrantsIfEmpty(context);
        DbSeeder.SeedConsultationsIfEmpty(context);
        DbSeeder.SeedStatisticsIfEmpty(context);
        DbSeeder.SeedPartnersIfEmpty(context);
    }
    catch { }
}
}

// Configure the HTTP request pipeline
app.UseForwardedHeaders();
app.UseExceptionHandler();

if (app.Environment.IsProduction())
{
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    if (app.Environment.IsProduction())
    {
        context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
    }
    context.Response.Headers["X-Correlation-ID"] = context.TraceIdentifier;
    await next();
});

app.Use(async (context, next) =>
{
    var stopwatch = Stopwatch.StartNew();
    try
    {
        await next();
    }
    finally
    {
        stopwatch.Stop();
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("HttpRequest");
        logger.LogInformation(
            "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds} ms trace={TraceId}",
            context.Request.Method,
            context.Request.Path.Value,
            context.Response.StatusCode,
            stopwatch.Elapsed.TotalMilliseconds,
            context.TraceIdentifier);
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Only use HTTPS redirection in production or when HTTPS is explicitly configured
// This prevents warnings when running HTTP-only in development
var urls = app.Configuration["ASPNETCORE_URLS"] ?? app.Configuration["urls"] ?? "";
if (urls.Contains("https", StringComparison.OrdinalIgnoreCase) || app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

// Enable CORS
app.UseCors("AllowFrontend");
app.UseRateLimiter();

// Enable static files (default wwwroot + configured upload path)
app.UseStaticFiles();

var uploadPath = app.Configuration["FileUpload:UploadPath"];
if (!string.IsNullOrWhiteSpace(uploadPath))
{
    Directory.CreateDirectory(uploadPath);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadPath),
        RequestPath = "/uploads"
    });
}

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Health check
app.MapGet("/", () => Results.Ok(new { message = "HCBE API is running" }))
    .WithName("HealthCheck")
    .WithOpenApi()
    .WithTags("Health");

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = WriteHealthResponse
}).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponse
}).AllowAnonymous();

// Map all API endpoints
app.MapAuthEndpoints();
app.MapMemberEndpoints();
app.MapMembershipApplicationEndpoints();
app.MapNewsletterEndpoints();
app.MapEventEndpoints();
app.MapEventCategoryEndpoints();
app.MapServiceCaseEndpoints();
app.MapAssociationEndpoints();
app.MapAssociationPortalEndpoints();
app.MapOpportunityEndpoints();
app.MapImpactEndpoints();
app.MapNewsEndpoints();
app.MapMediaEndpoints();
app.MapProjectEndpoints();
app.MapGrantEndpoints();
app.MapConsultationEndpoints();
app.MapUserEndpoints();
app.MapDocumentEndpoints();
app.MapContentEndpoints();
app.MapCmsEndpoints();
app.MapStatisticEndpoints();
app.MapSettingEndpoints();
app.MapNavigationEndpoints();
app.MapFooterEndpoints();
app.MapNotificationEndpoints();
app.MapPublicSubmissionEndpoints();
app.MapCommunityEndpoints();
app.MapMessagingEndpoints();
app.MapMemberAccountEndpoints();
app.MapPartnerEndpoints();
app.MapAuditEndpoints();
app.MapEmailOutboxEndpoints();
app.MapErrorIncidentEndpoints();
app.MapHub<MessagingHub>("/hubs/messaging");
app.MapHub<CmsHub>("/hubs/cms");
app.MapPrivacyEndpoints();
app.MapFinanceEndpoints();

// Direct test route for team members
app.MapGet("/api/team-members-test", () => Results.Ok("Direct test route works"));

app.MapTeamMemberEndpoints();

// Image proxy for external URLs to avoid frontend CORS issues
app.MapGet("/api/assets/proxy", async (
    string url,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    CancellationToken cancellationToken) =>
{
    try
    {
        var allowedHosts = configuration.GetSection("AssetProxy:AllowedHosts").Get<string[]>() ?? Array.Empty<string>();
        if (allowedHosts.Length == 0)
        {
            return Results.NotFound();
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            || !allowedHosts.Any(host =>
                uri.Host.Equals(host, StringComparison.OrdinalIgnoreCase)
                || uri.Host.EndsWith($".{host}", StringComparison.OrdinalIgnoreCase)))
        {
            return Results.BadRequest(new { message = "Asset host is not allowed" });
        }

        var addresses = await Dns.GetHostAddressesAsync(uri.Host, cancellationToken);
        if (addresses.Length == 0 || addresses.Any(IsPrivateOrLocalAddress))
        {
            return Results.BadRequest(new { message = "Asset host does not resolve to a public address" });
        }

        var http = httpClientFactory.CreateClient("AssetProxy");
        using var resp = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!resp.IsSuccessStatusCode)
            return Results.StatusCode((int)resp.StatusCode);

        var contentType = resp.Content.Headers.ContentType?.ToString() ?? "image/jpeg";
        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new { message = "Only image assets can be proxied" });
        }

        if (resp.Content.Headers.ContentLength is > 5 * 1024 * 1024)
        {
            return Results.BadRequest(new { message = "Asset exceeds the 5 MB limit" });
        }

        var bytes = await resp.Content.ReadAsByteArrayAsync(cancellationToken);
        if (bytes.Length > 5 * 1024 * 1024)
        {
            return Results.BadRequest(new { message = "Asset exceeds the 5 MB limit" });
        }
        return Results.File(bytes, contentType);
    }
    catch
    {
        return Results.BadRequest(new { message = "Failed to proxy image" });
    }
}).WithTags("Assets");

static bool IsPrivateOrLocalAddress(IPAddress address)
{
    if (IPAddress.IsLoopback(address) || address.IsIPv6LinkLocal || address.IsIPv6SiteLocal)
    {
        return true;
    }

    if (address.AddressFamily == AddressFamily.InterNetworkV6 && address.IsIPv4MappedToIPv6)
    {
        address = address.MapToIPv4();
    }

    if (address.AddressFamily != AddressFamily.InterNetwork)
    {
        return false;
    }

    var bytes = address.GetAddressBytes();
    return bytes[0] == 10
        || bytes[0] == 127
        || (bytes[0] == 169 && bytes[1] == 254)
        || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
        || (bytes[0] == 192 && bytes[1] == 168);
}

static void EnsureSqliteSecuritySchema(ApplicationDbContext context)
{
    try { context.Database.ExecuteSqlRaw("ALTER TABLE Events ADD COLUMN EndDate TEXT"); } catch { }
    try { context.Database.ExecuteSqlRaw("ALTER TABLE Events ADD COLUMN TimeZone TEXT NOT NULL DEFAULT 'America/Toronto'"); } catch { }
    try { context.Database.ExecuteSqlRaw("ALTER TABLE Events ADD COLUMN Format TEXT NOT NULL DEFAULT 'InPerson'"); } catch { }
    try { context.Database.ExecuteSqlRaw("ALTER TABLE Events ADD COLUMN RegistrationUrl TEXT"); } catch { }
    try { context.Database.ExecuteSqlRaw("ALTER TABLE Events ADD COLUMN CtaLabel TEXT"); } catch { }
    try { context.Database.ExecuteSqlRaw("ALTER TABLE Events ADD COLUMN CtaLabelEn TEXT"); } catch { }

    context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS EventSpeakers (
        Id TEXT PRIMARY KEY,
        EventId TEXT NOT NULL,
        Name TEXT NOT NULL,
        DisplayOrder INTEGER NOT NULL DEFAULT 0,
        FOREIGN KEY (EventId) REFERENCES Events(Id) ON DELETE CASCADE
    )");
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_EventSpeakers_EventId_DisplayOrder ON EventSpeakers(EventId, DisplayOrder)");

    context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS EventOrganizers (
        Id TEXT PRIMARY KEY,
        EventId TEXT NOT NULL,
        Name TEXT NOT NULL,
        DisplayOrder INTEGER NOT NULL DEFAULT 0,
        FOREIGN KEY (EventId) REFERENCES Events(Id) ON DELETE CASCADE
    )");
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_EventOrganizers_EventId_DisplayOrder ON EventOrganizers(EventId, DisplayOrder)");

    context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS EventCategories (
        Id TEXT PRIMARY KEY,
        Slug TEXT NOT NULL,
        Name TEXT NOT NULL,
        NameEn TEXT,
        IsActive INTEGER NOT NULL DEFAULT 1,
        DisplayOrder INTEGER NOT NULL DEFAULT 0,
        CreatedAt TEXT NOT NULL,
        UpdatedAt TEXT NOT NULL
    )");
    context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_EventCategories_Slug ON EventCategories(Slug)");
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_EventCategories_DisplayOrder ON EventCategories(DisplayOrder)");
    SeedSqliteEventCategories(context);

    try { context.Database.ExecuteSqlRaw("ALTER TABLE Users ADD COLUMN FailedLoginAttempts INTEGER NOT NULL DEFAULT 0"); } catch { }
    try { context.Database.ExecuteSqlRaw("ALTER TABLE Users ADD COLUMN LockoutEndUtc TEXT"); } catch { }
    try { context.Database.ExecuteSqlRaw("ALTER TABLE Users ADD COLUMN LastLoginAtUtc TEXT"); } catch { }
    try { context.Database.ExecuteSqlRaw("ALTER TABLE Users ADD COLUMN IsActive INTEGER NOT NULL DEFAULT 1"); } catch { }
    try { context.Database.ExecuteSqlRaw("ALTER TABLE Users ADD COLUMN MustChangePassword INTEGER NOT NULL DEFAULT 0"); } catch { }
    try { context.Database.ExecuteSqlRaw("ALTER TABLE Users ADD COLUMN AdminRole TEXT NOT NULL DEFAULT 'super-admin'"); } catch { }
    try { context.Database.ExecuteSqlRaw("ALTER TABLE Users ADD COLUMN AdminPermissions TEXT"); } catch { }
    context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS MemberPreferences (
        UserId TEXT NOT NULL PRIMARY KEY, PreferredLanguage TEXT NOT NULL DEFAULT 'fr', TimeZone TEXT NOT NULL DEFAULT 'America/Toronto',
        EmailEvents INTEGER NOT NULL DEFAULT 0, EmailOpportunities INTEGER NOT NULL DEFAULT 0,
        EmailMentorship INTEGER NOT NULL DEFAULT 0, EmailServiceUpdates INTEGER NOT NULL DEFAULT 0,
        EmailNewsletter INTEGER NOT NULL DEFAULT 0, PushNotifications INTEGER NOT NULL DEFAULT 0,
        HasCompletedPreferences INTEGER NOT NULL DEFAULT 0, UpdatedAt TEXT NOT NULL,
        FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
    )");
    try { context.Database.ExecuteSqlRaw("ALTER TABLE CmsContentItems ADD COLUMN ScheduledPublishAtUtc TEXT"); } catch { }
    try { context.Database.ExecuteSqlRaw("ALTER TABLE NewsletterCampaigns ADD COLUMN Audience TEXT NOT NULL DEFAULT 'Newsletter'"); } catch { }
    try { context.Database.ExecuteSqlRaw("ALTER TABLE NewsletterCampaigns ADD COLUMN PreferenceCategory TEXT NOT NULL DEFAULT 'newsletter'"); } catch { }
    try { context.Database.ExecuteSqlRaw("ALTER TABLE NewsletterCampaigns ADD COLUMN TargetProvince TEXT"); } catch { }
    try { context.Database.ExecuteSqlRaw("ALTER TABLE NewsletterCampaigns ADD COLUMN TargetZone TEXT"); } catch { }
    try { context.Database.ExecuteSqlRaw("ALTER TABLE NewsletterCampaigns ADD COLUMN TargetLanguage TEXT"); } catch { }
    try { context.Database.ExecuteSqlRaw("ALTER TABLE NewsletterCampaigns ADD COLUMN TargetInterest TEXT"); } catch { }
    try { context.Database.ExecuteSqlRaw("ALTER TABLE NewsletterCampaigns ADD COLUMN ScheduledAtUtc TEXT"); } catch { }
    try { context.Database.ExecuteSqlRaw("ALTER TABLE Associations ADD COLUMN OwnerMemberId TEXT"); } catch { }
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Associations_OwnerMemberId ON Associations(OwnerMemberId)");
    context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS AssociationClaimRequests (
        Id TEXT NOT NULL PRIMARY KEY, AssociationId TEXT NOT NULL, MemberId TEXT NOT NULL,
        Message TEXT NOT NULL, Status TEXT NOT NULL DEFAULT 'Pending', AdminNotes TEXT,
        CreatedAt TEXT NOT NULL, UpdatedAt TEXT NOT NULL, ReviewedAt TEXT,
        FOREIGN KEY (AssociationId) REFERENCES Associations(Id) ON DELETE CASCADE,
        FOREIGN KEY (MemberId) REFERENCES Members(Id) ON DELETE CASCADE
    )");
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_AssociationClaimRequests_AssociationId_MemberId_Status ON AssociationClaimRequests(AssociationId, MemberId, Status)");
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_AssociationClaimRequests_MemberId ON AssociationClaimRequests(MemberId)");

    context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS Opportunities (
        Id TEXT NOT NULL PRIMARY KEY, Title TEXT NOT NULL, TitleEn TEXT, Description TEXT NOT NULL, DescriptionEn TEXT,
        Type TEXT NOT NULL DEFAULT 'Volunteer', Organization TEXT NOT NULL DEFAULT 'HCBE Canada', Location TEXT,
        IsRemote INTEGER NOT NULL DEFAULT 0, Skills TEXT, ApplyUrl TEXT, DeadlineUtc TEXT, Status TEXT NOT NULL DEFAULT 'Draft',
        CreatedByUserId TEXT NOT NULL, CreatedAt TEXT NOT NULL, UpdatedAt TEXT NOT NULL
    )");
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Opportunities_Status_DeadlineUtc ON Opportunities(Status, DeadlineUtc)");
    context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS OpportunityApplications (
        Id TEXT NOT NULL PRIMARY KEY, OpportunityId TEXT NOT NULL, MemberId TEXT NOT NULL, Message TEXT NOT NULL,
        Status TEXT NOT NULL DEFAULT 'Submitted', AdminNotes TEXT, CreatedAt TEXT NOT NULL, UpdatedAt TEXT NOT NULL,
        FOREIGN KEY (OpportunityId) REFERENCES Opportunities(Id) ON DELETE CASCADE,
        FOREIGN KEY (MemberId) REFERENCES Members(Id) ON DELETE CASCADE
    )");
    context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_OpportunityApplications_OpportunityId_MemberId ON OpportunityApplications(OpportunityId, MemberId)");
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_OpportunityApplications_MemberId ON OpportunityApplications(MemberId)");

    context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS MentorshipGoals (
        Id TEXT NOT NULL PRIMARY KEY, MatchId TEXT NOT NULL, CreatedByMemberId TEXT NOT NULL, Title TEXT NOT NULL,
        Status TEXT NOT NULL DEFAULT 'Open', DueAtUtc TEXT, CreatedAt TEXT NOT NULL, UpdatedAt TEXT NOT NULL,
        FOREIGN KEY (MatchId) REFERENCES MentorshipMatches(Id) ON DELETE CASCADE
    )");
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MentorshipGoals_MatchId_Status ON MentorshipGoals(MatchId, Status)");
    context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS MentorshipCheckIns (
        Id TEXT NOT NULL PRIMARY KEY, MatchId TEXT NOT NULL, MemberId TEXT NOT NULL, Summary TEXT NOT NULL,
        Rating INTEGER NOT NULL, NeedsCommitteeSupport INTEGER NOT NULL DEFAULT 0, CreatedAt TEXT NOT NULL,
        FOREIGN KEY (MatchId) REFERENCES MentorshipMatches(Id) ON DELETE CASCADE,
        FOREIGN KEY (MemberId) REFERENCES Members(Id) ON DELETE CASCADE
    )");
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MentorshipCheckIns_MatchId_CreatedAt ON MentorshipCheckIns(MatchId, CreatedAt)");
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MentorshipCheckIns_MemberId ON MentorshipCheckIns(MemberId)");
    try { context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_Email ON Users(Email)"); } catch { }

    context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS RefreshTokens (
        Id TEXT PRIMARY KEY,
        UserId TEXT NOT NULL,
        TokenHash TEXT NOT NULL,
        CreatedAtUtc TEXT NOT NULL,
        ExpiresAtUtc TEXT NOT NULL,
        RevokedAtUtc TEXT,
        ReplacedByTokenHash TEXT,
        CreatedByIp TEXT,
        RevokedByIp TEXT,
        FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
    )");
    context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_RefreshTokens_TokenHash ON RefreshTokens(TokenHash)");
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_RefreshTokens_UserId_ExpiresAtUtc ON RefreshTokens(UserId, ExpiresAtUtc)");

    context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS AuditLogs (
        Id TEXT PRIMARY KEY,
        UserId TEXT,
        UserEmail TEXT,
        Action TEXT NOT NULL,
        EntityType TEXT NOT NULL,
        EntityId TEXT,
        ChangesJson TEXT,
        IpAddress TEXT,
        TraceId TEXT,
        CreatedAtUtc TEXT NOT NULL
    )");
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_AuditLogs_CreatedAtUtc ON AuditLogs(CreatedAtUtc)");
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_AuditLogs_EntityType_EntityId ON AuditLogs(EntityType, EntityId)");
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_AuditLogs_UserId_CreatedAtUtc ON AuditLogs(UserId, CreatedAtUtc)");

    context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS EmailOutboxMessages (
        Id TEXT PRIMARY KEY,
        Recipient TEXT NOT NULL,
        Subject TEXT NOT NULL,
        HtmlBody TEXT NOT NULL,
        Status TEXT NOT NULL,
        Attempts INTEGER NOT NULL DEFAULT 0,
        NextAttemptAtUtc TEXT NOT NULL,
        CreatedAtUtc TEXT NOT NULL,
        LockedAtUtc TEXT,
        ProcessedAtUtc TEXT,
        LastError TEXT,
        RelatedEntityType TEXT,
        RelatedEntityId TEXT
    )");
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_EmailOutboxMessages_Status_NextAttemptAtUtc ON EmailOutboxMessages(Status, NextAttemptAtUtc)");
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_EmailOutboxMessages_RelatedEntityType_RelatedEntityId ON EmailOutboxMessages(RelatedEntityType, RelatedEntityId)");

    context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS PrivacyRequests (
        Id TEXT PRIMARY KEY,
        UserId TEXT,
        Type TEXT NOT NULL,
        Status TEXT NOT NULL,
        RequestedAtUtc TEXT NOT NULL,
        ExecuteAfterUtc TEXT NOT NULL,
        CancelledAtUtc TEXT,
        CompletedAtUtc TEXT,
        SubjectReference TEXT,
        FailureReason TEXT
    )");
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_PrivacyRequests_Status_ExecuteAfterUtc ON PrivacyRequests(Status, ExecuteAfterUtc)");
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_PrivacyRequests_UserId ON PrivacyRequests(UserId)");

    context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS DonationCampaigns (
        Id TEXT NOT NULL PRIMARY KEY, Slug TEXT NOT NULL, Title TEXT NOT NULL, TitleEn TEXT,
        Description TEXT NOT NULL, DescriptionEn TEXT, GoalAmountCents INTEGER NOT NULL,
        Currency TEXT NOT NULL, ImageUrl TEXT, AllowRecurring INTEGER NOT NULL DEFAULT 1,
        IsPublished INTEGER NOT NULL DEFAULT 0, StartsAtUtc TEXT, EndsAtUtc TEXT,
        CreatedAtUtc TEXT NOT NULL, UpdatedAtUtc TEXT NOT NULL
    )");
    context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_DonationCampaigns_Slug ON DonationCampaigns(Slug)");
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_DonationCampaigns_IsPublished_StartsAtUtc_EndsAtUtc ON DonationCampaigns(IsPublished, StartsAtUtc, EndsAtUtc)");

    context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS MembershipPlans (
        Id TEXT NOT NULL PRIMARY KEY, Name TEXT NOT NULL, NameEn TEXT, Description TEXT NOT NULL,
        DescriptionEn TEXT, AmountCents INTEGER NOT NULL, Currency TEXT NOT NULL,
        BillingMode TEXT NOT NULL, StripePriceId TEXT, BenefitsJson TEXT NOT NULL DEFAULT '[]',
        IsActive INTEGER NOT NULL DEFAULT 1, DisplayOrder INTEGER NOT NULL DEFAULT 0,
        CreatedAtUtc TEXT NOT NULL, UpdatedAtUtc TEXT NOT NULL
    )");
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MembershipPlans_IsActive_DisplayOrder ON MembershipPlans(IsActive, DisplayOrder)");

    context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS PaymentWebhookEvents (
        Id TEXT NOT NULL PRIMARY KEY, ProviderEventId TEXT NOT NULL, EventType TEXT NOT NULL,
        Status TEXT NOT NULL, Error TEXT, ReceivedAtUtc TEXT NOT NULL, ProcessedAtUtc TEXT
    )");
    context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_PaymentWebhookEvents_ProviderEventId ON PaymentWebhookEvents(ProviderEventId)");
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_PaymentWebhookEvents_Status_ReceivedAtUtc ON PaymentWebhookEvents(Status, ReceivedAtUtc)");

    context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS FinancialTransactions (
        Id TEXT NOT NULL PRIMARY KEY, UserId TEXT, MembershipPlanId TEXT, DonationCampaignId TEXT,
        Kind TEXT NOT NULL, Status TEXT NOT NULL, AmountCents INTEGER NOT NULL,
        RefundedAmountCents INTEGER NOT NULL DEFAULT 0, Currency TEXT NOT NULL, PayerEmail TEXT NOT NULL,
        PayerName TEXT, IsAnonymous INTEGER NOT NULL DEFAULT 0, AllowPublicRecognition INTEGER NOT NULL DEFAULT 0,
        DonorMessage TEXT, IsRecurring INTEGER NOT NULL DEFAULT 0, StripeCheckoutSessionId TEXT,
        StripePaymentIntentId TEXT, StripeCustomerId TEXT, StripeSubscriptionId TEXT, StripeInvoiceId TEXT,
        ReceiptNumber TEXT NOT NULL, ReceiptToken TEXT NOT NULL, FailureReason TEXT,
        CreatedAtUtc TEXT NOT NULL, UpdatedAtUtc TEXT NOT NULL, PaidAtUtc TEXT, RefundedAtUtc TEXT,
        FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL,
        FOREIGN KEY (MembershipPlanId) REFERENCES MembershipPlans(Id) ON DELETE SET NULL,
        FOREIGN KEY (DonationCampaignId) REFERENCES DonationCampaigns(Id) ON DELETE SET NULL
    )");
    context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_FinancialTransactions_ReceiptNumber ON FinancialTransactions(ReceiptNumber)");
    context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_FinancialTransactions_ReceiptToken ON FinancialTransactions(ReceiptToken)");
    context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_FinancialTransactions_StripeCheckoutSessionId ON FinancialTransactions(StripeCheckoutSessionId)");
    context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_FinancialTransactions_StripeInvoiceId ON FinancialTransactions(StripeInvoiceId)");
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_FinancialTransactions_Status_CreatedAtUtc ON FinancialTransactions(Status, CreatedAtUtc)");
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_FinancialTransactions_UserId_CreatedAtUtc ON FinancialTransactions(UserId, CreatedAtUtc)");
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_FinancialTransactions_MembershipPlanId ON FinancialTransactions(MembershipPlanId)");
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_FinancialTransactions_DonationCampaignId ON FinancialTransactions(DonationCampaignId)");

    context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS MembershipStandings (
        Id TEXT NOT NULL PRIMARY KEY, UserId TEXT NOT NULL, PlanId TEXT, Status TEXT NOT NULL,
        CurrentPeriodStartUtc TEXT, CurrentPeriodEndUtc TEXT, GraceEndsAtUtc TEXT,
        AutoRenew INTEGER NOT NULL DEFAULT 0, StripeCustomerId TEXT, StripeSubscriptionId TEXT,
        LastTransactionId TEXT, LastReminderKey TEXT, LastReminderAtUtc TEXT, UpdatedAtUtc TEXT NOT NULL,
        FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE RESTRICT,
        FOREIGN KEY (PlanId) REFERENCES MembershipPlans(Id) ON DELETE SET NULL
    )");
    context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_MembershipStandings_UserId ON MembershipStandings(UserId)");
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MembershipStandings_PlanId ON MembershipStandings(PlanId)");
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MembershipStandings_Status_CurrentPeriodEndUtc ON MembershipStandings(Status, CurrentPeriodEndUtc)");

    // Local SQLite databases predate the versioned PostgreSQL CMS migration.
    // Keep development data intact while adding the same draft/publish schema.
    context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS CmsContentItems (
        Id TEXT PRIMARY KEY,
        Key TEXT NOT NULL,
        Page TEXT NOT NULL,
        Section TEXT NOT NULL,
        ContentType TEXT NOT NULL,
        Label TEXT,
        DraftValueFr TEXT,
        DraftValueEn TEXT,
        PublishedValueFr TEXT,
        PublishedValueEn TEXT,
        IsPublished INTEGER NOT NULL DEFAULT 0,
        Version INTEGER NOT NULL DEFAULT 0,
        UpdatedByUserId TEXT,
        PublishedByUserId TEXT,
        CreatedAt TEXT NOT NULL,
        UpdatedAt TEXT NOT NULL,
        PublishedAt TEXT
    )");
    context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_CmsContentItems_Key ON CmsContentItems(Key)");
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_CmsContentItems_Page_Section ON CmsContentItems(Page, Section)");

    context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS CmsContentRevisions (
        Id TEXT PRIMARY KEY,
        CmsContentItemId TEXT NOT NULL,
        Version INTEGER NOT NULL,
        ValueFr TEXT,
        ValueEn TEXT,
        PublishedByUserId TEXT,
        PublishedAt TEXT NOT NULL,
        FOREIGN KEY (CmsContentItemId) REFERENCES CmsContentItems(Id) ON DELETE CASCADE
    )");
    context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_CmsContentRevisions_CmsContentItemId_Version ON CmsContentRevisions(CmsContentItemId, Version)");
}

static void SeedSqliteEventCategories(ApplicationDbContext context)
{
    var now = DateTime.UtcNow.ToString("O");
    var categories = new[]
    {
        ("e1010000-0000-0000-0000-000000000001", "workshop", "Atelier", "Workshop"),
        ("e1010000-0000-0000-0000-000000000002", "conference", "Conférence", "Conference"),
        ("e1010000-0000-0000-0000-000000000003", "webinar", "Webinaire", "Webinar"),
        ("e1010000-0000-0000-0000-000000000004", "professional-development", "Développement professionnel", "Professional development"),
        ("e1010000-0000-0000-0000-000000000005", "diplomatic-community-meeting", "Rencontre diplomatique et communautaire", "Diplomatic and community meeting"),
        ("e1010000-0000-0000-0000-000000000006", "business-investment", "Affaires et investissement", "Business and investment"),
        ("e1010000-0000-0000-0000-000000000007", "networking", "Réseautage", "Networking"),
        ("e1010000-0000-0000-0000-000000000008", "training", "Formation", "Training"),
        ("e1010000-0000-0000-0000-000000000009", "cultural-festival", "Festival et culture", "Cultural festival"),
        ("e1010000-0000-0000-0000-000000000010", "national-celebration", "Célébration nationale et civique", "National and civic celebration"),
        ("e1010000-0000-0000-0000-000000000011", "fundraiser-solidarity", "Collecte et solidarité", "Fundraiser and solidarity"),
        ("e1010000-0000-0000-0000-000000000012", "memorial-tribute", "Hommage et commémoration", "Memorial and tribute"),
        ("e1010000-0000-0000-0000-000000000013", "social", "Activité sociale", "Social event"),
        ("e1010000-0000-0000-0000-000000000014", "other", "Autre", "Other")
    };

    for (var index = 0; index < categories.Length; index++)
    {
        var item = categories[index];
        context.Database.ExecuteSqlInterpolated($@"INSERT OR IGNORE INTO EventCategories
            (Id, Slug, Name, NameEn, IsActive, DisplayOrder, CreatedAt, UpdatedAt)
            VALUES ({item.Item1}, {item.Item2}, {item.Item3}, {item.Item4}, 1, {index}, {now}, {now})");
    }
}

static Task WriteHealthResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    var payload = JsonSerializer.Serialize(new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(entry => new
        {
            name = entry.Key,
            status = entry.Value.Status.ToString(),
            description = entry.Value.Description
        }),
        durationMs = report.TotalDuration.TotalMilliseconds
    });
    return context.Response.WriteAsync(payload);
}

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
