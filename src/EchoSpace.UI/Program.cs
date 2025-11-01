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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddDbContext<EchoSpaceDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// Add Authorization with ABAC policies
builder.Services.AddAuthorization(options =>
{
    // Existing RBAC policies
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ModeratorOrAdmin", policy => policy.RequireRole("Admin", "Moderator"));
    options.AddPolicy("AuthenticatedUser", policy => policy.RequireAuthenticatedUser());
    
    // ABAC: Owner-based policies
    options.AddPolicy("OwnerOfPost", policy => policy.Requirements.Add(
        new EchoSpace.Core.Authorization.Requirements.OwnerRequirement("Post")));
    options.AddPolicy("OwnerOfComment", policy => policy.Requirements.Add(
        new EchoSpace.Core.Authorization.Requirements.OwnerRequirement("Comment")));
    
    // ABAC: Admin OR Owner policies
    options.AddPolicy("AdminOrOwnerOfPost", policy => policy.Requirements.Add(
        new EchoSpace.Core.Authorization.Requirements.AdminOrOwnerRequirement("Post")));
    options.AddPolicy("AdminOrOwnerOfComment", policy => policy.Requirements.Add(
        new EchoSpace.Core.Authorization.Requirements.AdminOrOwnerRequirement("Comment")));
    
    // ABAC: Role-based policies (can be extended)
    options.AddPolicy("RequireAdminRole", policy => policy.Requirements.Add(
        new EchoSpace.Core.Authorization.Requirements.RoleRequirement("Admin")));
    options.AddPolicy("RequireModeratorRole", policy => policy.Requirements.Add(
        new EchoSpace.Core.Authorization.Requirements.RoleRequirement("Moderator")));
});

// Register ABAC authorization handlers
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