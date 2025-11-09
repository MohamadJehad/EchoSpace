using EchoSpace.Core.Entities;
using EchoSpace.Core.DTOs;
using EchoSpace.Core.Interfaces;
using EchoSpace.Core.Services;
using EchoSpace.Infrastructure.Data;
using EchoSpace.Infrastructure.Repositories;
using EchoSpace.Infrastructure.Services;
using EchoSpace.Tools.Email;
using EchoSpace.Tools.Interfaces;
using EchoSpace.Tools.Services;
using EchoSpace.UI.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Microsoft.AspNetCore.Http;
using Serilog;
using EchoSpace.Core.Interfaces.Services;
using EchoSpace.Infrastructure.Services.Logging;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog - using simpler configuration to avoid build issues
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine("logs", "audit", "audit-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
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
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
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
            policy.WithOrigins("http://localhost:4200") // Angular dev server
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
});

// Add Entity Framework
var enableDbCommandLogging = builder.Configuration.GetValue<bool>("Logging:EnableDatabaseCommandLogging", false);

builder.Services.AddDbContext<EchoSpaceDbContext>((serviceProvider, options) =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    
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

    // Legacy policy names for backward compatibility (now using ABAC)
    options.AddPolicy("AdminOnly", policy => policy.Requirements.Add(
        new EchoSpace.Core.Authorization.Requirements.AbacRequirement(adminPolicy, "General", "Admin")));
    
    options.AddPolicy("ModeratorOrAdmin", policy => policy.Requirements.Add(
        new EchoSpace.Core.Authorization.Requirements.AbacRequirement(moderatorOrAdminPolicy, "General", "Moderate")));
    
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

// Comment services
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<ICommentService, CommentService>();

// Follow services
builder.Services.AddScoped<IFollowRepository, FollowRepository>();
builder.Services.AddScoped<IFollowService, FollowService>();

// Like services
builder.Services.AddScoped<ILikeRepository, LikeRepository>();
builder.Services.AddScoped<ILikeService, LikeService>();

// Image services
builder.Services.AddScoped<IImageRepository, ImageRepository>();
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<IImageService, ImageService>();

// Audit logging service
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

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


// 1. Configure the EmailSettings class to read from the "EmailSettings" section
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// 2. Register your EmailSender as a service
builder.Services.AddTransient<IEmailSender, EmailSender>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// IMPORTANT: Order matters - UseCors, UseSession, UseAuthentication, UseAuthorization
app.UseCors("AllowAngular");

app.UseHttpsRedirection();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();