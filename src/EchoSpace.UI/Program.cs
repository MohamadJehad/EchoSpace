using EchoSpace.Core.Entities;
using EchoSpace.Core.DTOs;
using EchoSpace.Core.Validators.Auth;
using EchoSpace.Core.Validators.Comments;
using EchoSpace.Core.Validators.Posts;
using EchoSpace.Core.Validators.Users;
using FluentValidation;
using EchoSpace.Core.DTOs.Posts;
using EchoSpace.Core.Interfaces;
using EchoSpace.Core.Services;
using EchoSpace.Infrastructure.Data;
using EchoSpace.Infrastructure.Options;
using EchoSpace.Infrastructure.Repositories;
using EchoSpace.Infrastructure.Services;
using EchoSpace.Tools.Email;
using EchoSpace.Tools.Interfaces;
using EchoSpace.Tools.Services;
using EchoSpace.UI.Authorization;
using EchoSpace.UI.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.Threading.RateLimiting;
using FluentValidation.AspNetCore;
using Serilog;
using EchoSpace.Core.Interfaces.Services;
using EchoSpace.Infrastructure.Services.Logging;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog - using simpler configuration to avoid build issues
// In Azure App Service, logs are automatically captured by the platform
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console() // Console logs are captured by Azure App Service
    // File logging - only for local development
    // Azure App Service automatically captures console output
    .WriteTo.File(
        Path.Combine(Environment.GetEnvironmentVariable("HOME") ?? ".", "logs", "audit", "audit-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        shared: true) // Allow multiple processes to write to the same file
);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Add HttpClient for Google OAuth API calls
builder.Services.AddHttpClient();

// Add session for OAuth state storage
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Increased for OAuth flows
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax; // Allows OAuth redirects
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Force secure cookies
});

// Add HTTP context accessor for ABAC authorization handlers
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

// Add CORS support for Angular frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy =>
        {
           policy.WithOrigins(
                    "http://localhost:4200",
                    "https://localhost:4200", // if using HTTPS for Angular dev server
                    "https://echospace-angular-app-dev.azurewebsites.net" // Production frontend
                )
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
});

// Add Entity Framework
var enableDbCommandLogging = builder.Configuration.GetValue<bool>("Logging:EnableDatabaseCommandLogging", false);

builder.Services.AddDbContext<EchoSpaceDbContext>((serviceProvider, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        // Enable retry logic for transient failures (common with Azure SQL)
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
    });
    
    // Add database command interceptor for audit logging (optional - can be verbose)
    // Enable via appsettings.json: "Logging:EnableDatabaseCommandLogging": true
    // WARNING: This logs EVERY database command - use only when needed for security/compliance
    if (enableDbCommandLogging)
    {
        var auditLogService = serviceProvider.GetRequiredService<IAuditLogService>();
        options.AddInterceptors(new EchoSpace.Infrastructure.Logging.EchoSpaceDbCommandInterceptor(auditLogService, isEnabled: true));
    }
});

// Auth service will handle password hashing and validation

// Add JWT Authentication and Google OAuth
var googleClientId = builder.Configuration["Google:ClientId"];
var googleClientSecret = builder.Configuration["Google:ClientSecret"];
var hasGoogleCredentials = !string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret) && 
    googleClientId != "YOUR_GOOGLE_CLIENT_ID" && googleClientSecret != "YOUR_GOOGLE_CLIENT_SECRET";

var authBuilder = builder.Services.AddAuthentication(options =>
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
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
    
    // Add event handler to check if user is locked on each request
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var userIdClaim = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                var dbContext = context.HttpContext.RequestServices.GetRequiredService<EchoSpaceDbContext>();
                var user = await dbContext.Users.FindAsync(userId);
                
                if (user != null)
                {
                    // Check if account is locked
                    if (user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
                    {
                        context.Fail("Account is locked. Please contact support.");
                        return;
                    }
                }
            }
        }
    };
});

// Add Authorization with ABAC (Attribute-Based Access Control) policies
builder.Services.AddAuthorization(options =>
{
    // ABAC: Authenticated User Policy
    var authenticatedUserPolicy = AbacPolicyBuilder.CreateAuthenticatedUserPolicy();
    options.AddAbacPolicy(authenticatedUserPolicy, "General");

    // ABAC: Admin Role Policy
    var adminPolicy = AbacPolicyBuilder.CreateAdminRolePolicy();
    options.AddAbacPolicy(adminPolicy, "General", "Admin");

    // ABAC: Moderator or Admin Role Policy
    var moderatorOrAdminPolicy = AbacPolicyBuilder.CreateModeratorOrAdminRolePolicy();
    options.AddAbacPolicy(moderatorOrAdminPolicy, "General", "Moderate");

    // ABAC: Operation Role Policy
    var operationPolicy = AbacPolicyBuilder.CreateOperationRolePolicy();
    options.AddAbacPolicy(operationPolicy, "General", "Operation");

    // ABAC: Operation or Admin Role Policy
    var operationOrAdminPolicy = AbacPolicyBuilder.CreateOperationOrAdminRolePolicy();
    options.AddAbacPolicy(operationOrAdminPolicy, "General", "OperationOrAdmin");

    // ABAC: Owner-based policies
    var ownerOfPostPolicy = AbacPolicyBuilder.CreateOwnerPolicy("Post");
    options.AddAbacPolicy(ownerOfPostPolicy, "Post");
    
    var ownerOfCommentPolicy = AbacPolicyBuilder.CreateOwnerPolicy("Comment");
    options.AddAbacPolicy(ownerOfCommentPolicy, "Comment");

    // ABAC: Admin OR Owner policies
    var adminOrOwnerOfPostPolicy = AbacPolicyBuilder.CreateAdminOrOwnerPolicy("Post");
    options.AddAbacPolicy(adminOrOwnerOfPostPolicy, "Post", "UpdateOrDelete");
    
    var adminOrOwnerOfCommentPolicy = AbacPolicyBuilder.CreateAdminOrOwnerPolicy("Comment");
    options.AddAbacPolicy(adminOrOwnerOfCommentPolicy, "Comment", "UpdateOrDelete");

    // ABAC: Operation OR Admin OR Owner policies (for post deletion by Operation)
    var operationOrAdminOrOwnerOfPostPolicy = AbacPolicyBuilder.CreateOperationOrAdminOrOwnerPolicy("Post");
    options.AddAbacPolicy(operationOrAdminOrOwnerOfPostPolicy, "Post", "OperationDelete");

    // Legacy policy names for backward compatibility (now using ABAC)
    options.AddPolicy("AdminOnly", policy => policy.Requirements.Add(
        new EchoSpace.Core.Authorization.Requirements.AbacRequirement(adminPolicy, "General", "Admin")));
    
    options.AddPolicy("ModeratorOrAdmin", policy => policy.Requirements.Add(
        new EchoSpace.Core.Authorization.Requirements.AbacRequirement(moderatorOrAdminPolicy, "General", "Moderate")));
    
    options.AddPolicy("OperationOnly", policy => policy.Requirements.Add(
        new EchoSpace.Core.Authorization.Requirements.AbacRequirement(operationPolicy, "General", "Operation")));
    
    options.AddPolicy("OperationOrAdmin", policy => policy.Requirements.Add(
        new EchoSpace.Core.Authorization.Requirements.AbacRequirement(operationOrAdminPolicy, "General", "OperationOrAdmin")));
    
    options.AddPolicy("AuthenticatedUser", policy => policy.Requirements.Add(
        new EchoSpace.Core.Authorization.Requirements.AbacRequirement(authenticatedUserPolicy, "General")));
    
    options.AddPolicy("OwnerOfPost", policy => policy.Requirements.Add(
        new EchoSpace.Core.Authorization.Requirements.AbacRequirement(ownerOfPostPolicy, "Post")));
    
    options.AddPolicy("OwnerOfComment", policy => policy.Requirements.Add(
        new EchoSpace.Core.Authorization.Requirements.AbacRequirement(ownerOfCommentPolicy, "Comment")));
    
    options.AddPolicy("AdminOrOwnerOfPost", policy => policy.Requirements.Add(
        new EchoSpace.Core.Authorization.Requirements.AbacRequirement(adminOrOwnerOfPostPolicy, "Post", "UpdateOrDelete")));
    
    options.AddPolicy("AdminOrOwnerOfComment", policy => policy.Requirements.Add(
        new EchoSpace.Core.Authorization.Requirements.AbacRequirement(adminOrOwnerOfCommentPolicy, "Comment", "UpdateOrDelete")));
    
    options.AddPolicy("OperationOrAdminOrOwnerOfPost", policy => policy.Requirements.Add(
        new EchoSpace.Core.Authorization.Requirements.AbacRequirement(operationOrAdminOrOwnerOfPostPolicy, "Post", "OperationDelete")));
});

// Register ABAC authorization handler (primary handler for all ABAC policies)
builder.Services.AddScoped<IAuthorizationHandler, EchoSpace.UI.Authorization.Handlers.AbacRequirementHandler>();

// Keep legacy handlers for backward compatibility (can be removed after full migration)
builder.Services.AddScoped<IAuthorizationHandler, EchoSpace.UI.Authorization.Handlers.OwnerRequirementHandler>();
builder.Services.AddScoped<IAuthorizationHandler, EchoSpace.Core.Authorization.Handlers.RoleRequirementHandler>();
builder.Services.AddScoped<IAuthorizationHandler, EchoSpace.UI.Authorization.Handlers.AdminOrOwnerRequirementHandler>();

// Register repositories and services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITotpService, TotpService>();
builder.Services.AddScoped<ISearchService, SearchService>();

// Post services
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<IPostService, PostService>();

// Tag services
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddScoped<ITagService, TagService>();

// Comment services
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<ICommentService, CommentService>();

// Follow services
builder.Services.AddScoped<IFollowRepository, FollowRepository>();
builder.Services.AddScoped<IFollowService, FollowService>();

// Like services
builder.Services.AddScoped<ILikeRepository, LikeRepository>();
builder.Services.AddScoped<ILikeService, LikeService>();

// Post Report services
builder.Services.AddScoped<IPostReportRepository, PostReportRepository>();
builder.Services.AddScoped<IPostReportService, PostReportService>();

// Image services
builder.Services.AddScoped<IImageRepository, ImageRepository>();
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<IImageService, ImageService>();

// Audit logging service
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// Analytics service
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

//AuditLog repository
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IAuditLogDBService, AuditLogDBService>();
builder.Services.AddScoped<AuditActionFilter>();
builder.Services.AddControllers(options =>
{
    options.Filters.AddService<AuditActionFilter>();
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "EchoSpace API", Version = "v1" });
    
    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });
    
    // Configure Swagger to handle file uploads (IFormFile)
    c.MapType<IFormFile>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });
});



//=============================================================
// Email settings
//=============================================================

// 1. Configure the EmailSettings class to read from the "EmailSettings" section
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// 2. Register your EmailSender as a service
builder.Services.AddTransient<IEmailSender, EmailSender>();


//=============================================================
// Gemni Settings
//=============================================================
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));
builder.Services.AddScoped<IAiService, GeminiAiService>();

//=============================================================
// AI Image Generation Settings
//=============================================================
builder.Services.Configure<PollinationsOptions>(options =>
{
    options.BaseUrl = builder.Configuration["Pollinations:BaseUrl"] ?? "https://image.pollinations.ai";
});
builder.Services.AddHttpClient<PollinationsProvider>();
builder.Services.AddScoped<IAiImageGenerationService, PollinationsProvider>();


//=============================================================
// Security settings
//=============================================================

// 1. Configure Security Headers

// Added in middle ware

// 2. Configure Secure TLS

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.SslProtocols =
            System.Security.Authentication.SslProtocols.Tls13 |
            System.Security.Authentication.SslProtocols.Tls12;
    });
});


//=============================================================
// Input Validation
//=============================================================

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();


builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<EmailVerificationRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<ForgotPasswordRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<RefreshTokenRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<LogoutRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<ResetPasswordRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<TotpSetupRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<TotpVerificationRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<ValidateResetTokenRequestValidator>();

builder.Services.AddValidatorsFromAssemblyContaining<CreatePostRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<UpdatePostRequestValidator>();

builder.Services.AddValidatorsFromAssemblyContaining<CreateCommentRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<UpdateCommentRequestValidator>();

builder.Services.AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<UpdateUserRequestValidator>();

//=============================================================



// Configure Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    // Configure global rejection handler
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync(
            "Please try again later.", cancellationToken: token);
    };

    // Use IP address for login/register
    options.AddPolicy("LoginAndRegisterPolicy", context =>
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:LoginAndRegisterPolicy:PermitLimit", 3),
                Window = TimeSpan.Parse(builder.Configuration.GetValue<string>("RateLimiting:LoginAndRegisterPolicy:Window") ?? "00:01:00"),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = builder.Configuration.GetValue<int>("RateLimiting:LoginAndRegisterPolicy:QueueLimit", 0),
                AutoReplenishment = true
            });
    });

    // Use IP for refresh token (token is in body, so we use IP-based limiting)
    options.AddPolicy("RefreshTokenPolicy", context =>
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:RefreshTokenPolicy:PermitLimit", 10),
                Window = TimeSpan.Parse(builder.Configuration.GetValue<string>("RateLimiting:RefreshTokenPolicy:Window") ?? "00:01:00"),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = builder.Configuration.GetValue<int>("RateLimiting:RefreshTokenPolicy:QueueLimit", 0),
                AutoReplenishment = true
            });
    });

    // Use authenticated user ID or IP for general API
    options.AddPolicy("GeneralApiPolicy", context =>
    {
        var userId = context.User?.Identity?.IsAuthenticated == true
            ? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous"
            : "anonymous";
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var partitionKey = $"{ipAddress}:{userId}";
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:GeneralApiPolicy:PermitLimit", 100),
                Window = TimeSpan.Parse(builder.Configuration.GetValue<string>("RateLimiting:GeneralApiPolicy:Window") ?? "00:01:00"),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = builder.Configuration.GetValue<int>("RateLimiting:GeneralApiPolicy:QueueLimit", 0),
                AutoReplenishment = true
            });
    });

    // Use authenticated user ID or IP for search
    options.AddPolicy("SearchPolicy", context =>
    {
        var userId = context.User?.Identity?.IsAuthenticated == true
            ? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous"
            : "anonymous";
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var partitionKey = $"{ipAddress}:{userId}";
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:SearchPolicy:PermitLimit", 30),
                Window = TimeSpan.Parse(builder.Configuration.GetValue<string>("RateLimiting:SearchPolicy:Window") ?? "00:01:00"),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = builder.Configuration.GetValue<int>("RateLimiting:SearchPolicy:QueueLimit", 0),
                AutoReplenishment = true
            });
    });
});

// Load keys from configuration

var safeBrowsingApiKey = builder.Configuration["GoogleApis:SafeBrowsingApiKey"];
var perspectiveApiKey = builder.Configuration["GoogleApis:PerspectiveApiKey"];
if (string.IsNullOrWhiteSpace(safeBrowsingApiKey))
    throw new InvalidOperationException("API key must be set.");
if (string.IsNullOrWhiteSpace(perspectiveApiKey))
    throw new InvalidOperationException("API key must be set.");
// Register Google API services
builder.Services.AddScoped<IGoogleSafeBrowsingService>(sp =>
    new GoogleSafeBrowsingService(safeBrowsingApiKey));

builder.Services.AddScoped<IGooglePerspectiveService>(sp =>
    new GooglePerspectiveService(perspectiveApiKey));
builder.Services.AddScoped<IValidator<CreatePostRequest>, CreatePostRequestValidator>();
builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddBlobServiceClient(builder.Configuration["StorageConnection:blobServiceUri"]!).WithName("StorageConnection");
    clientBuilder.AddQueueServiceClient(builder.Configuration["StorageConnection:queueServiceUri"]!).WithName("StorageConnection");
    clientBuilder.AddTableServiceClient(builder.Configuration["StorageConnection:tableServiceUri"]!).WithName("StorageConnection");
});


var app = builder.Build();

// DIAGNOSTIC: Log configuration values to verify Azure App Service settings are being read
var diagnosticLogger = app.Services.GetRequiredService<ILogger<Program>>();
diagnosticLogger.LogInformation("=== CONFIGURATION DIAGNOSTICS ===");
diagnosticLogger.LogInformation("Jwt:Key = {Value}", string.IsNullOrEmpty(builder.Configuration["Jwt:Key"]) ? "NULL or EMPTY" : "SET (length: " + builder.Configuration["Jwt:Key"]!.Length + ")");
diagnosticLogger.LogInformation("Jwt:Issuer = {Value}", builder.Configuration["Jwt:Issuer"] ?? "NULL");
diagnosticLogger.LogInformation("Jwt:Audience = {Value}", builder.Configuration["Jwt:Audience"] ?? "NULL");
diagnosticLogger.LogInformation("GoogleApis:SafeBrowsingApiKey = {Value}", string.IsNullOrEmpty(builder.Configuration["GoogleApis:SafeBrowsingApiKey"]) ? "NULL or EMPTY" : "SET");
diagnosticLogger.LogInformation("GoogleApis:PerspectiveApiKey = {Value}", string.IsNullOrEmpty(builder.Configuration["GoogleApis:PerspectiveApiKey"]) ? "NULL or EMPTY" : "SET");
diagnosticLogger.LogInformation("StorageConnection:blobServiceUri = {Value}", builder.Configuration["StorageConnection:blobServiceUri"] ?? "NULL");
diagnosticLogger.LogInformation("StorageConnection:queueServiceUri = {Value}", builder.Configuration["StorageConnection:queueServiceUri"] ?? "NULL");
diagnosticLogger.LogInformation("StorageConnection:tableServiceUri = {Value}", builder.Configuration["StorageConnection:tableServiceUri"] ?? "NULL");
diagnosticLogger.LogInformation("ConnectionStrings:DefaultConnection = {Value}", string.IsNullOrEmpty(builder.Configuration.GetConnectionString("DefaultConnection")) ? "NULL or EMPTY" : "SET");
diagnosticLogger.LogInformation("ASPNETCORE_ENVIRONMENT = {Value}", builder.Configuration["ASPNETCORE_ENVIRONMENT"] ?? "NULL");
diagnosticLogger.LogInformation("=== END CONFIGURATION DIAGNOSTICS ===");

app.UseSecurityHeaders();
app.UseMiddleware<RequestContextMiddleware>();

// Test database connection on startup
try
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<EchoSpaceDbContext>();
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        // Log connection string info (without password)
        if (!string.IsNullOrEmpty(connectionString))
        {
            var safeConnectionString = connectionString.Contains("Password=") 
                ? connectionString.Substring(0, connectionString.IndexOf("Password=")) + "Password=***"
                : connectionString;
            logger.LogInformation("Attempting database connection. Connection string: {ConnectionString}", safeConnectionString);
        }
        
        var canConnect = dbContext.Database.CanConnect();
        if (canConnect)
        {
            logger.LogInformation("Database connection successful.");
            
            // Initialize default tags if they don't exist
            try
            {
                var tagService = scope.ServiceProvider.GetRequiredService<ITagService>();
                await tagService.InitializeDefaultTagsAsync();
                logger.LogInformation("Default tags initialized successfully.");
            }
            catch (Exception tagEx)
            {
                logger.LogWarning(tagEx, "Failed to initialize default tags. This is not critical.");
            }
        }
        else
        {
            logger.LogWarning("Database connection test returned false, but no exception was thrown.");
        }
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Failed to connect to database. Please check:");
    logger.LogError("1. Azure SQL Server firewall rules - ensure your IP address is allowed");
    logger.LogError("2. Connection string is correct in appsettings.json");
    logger.LogError("3. Database server is accessible and credentials are correct");
    logger.LogError("4. Connection string uses SQL authentication (User ID/Password) not Windows authentication");
    logger.LogError("Error details: {Message}", ex.Message);
    logger.LogError("Inner exception: {InnerException}", ex.InnerException?.Message);
    // Don't throw - let the app start so you can see the error in logs
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
    app.UseHsts();

// Reject HTTP requests middleware 
 app.Use(async (context, next) =>
 {
     if (!context.Request.IsHttps)
     {
         context.Response.StatusCode = StatusCodes.Status400BadRequest;
         await context.Response.WriteAsync("HTTPS is required.");
         return;
     }
     await next();
 });

// IMPORTANT: Order matters - UseCors, UseSession, UseAuthentication, UseAuthorization, UseRateLimiter
app.UseCors("AllowAngular");

app.UseHttpsRedirection();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();


app.Run();
