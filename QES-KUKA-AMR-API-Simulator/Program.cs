using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QES_KUKA_AMR_API_Simulator.Auth;
using QES_KUKA_AMR_API_Simulator.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "QES KUKA AMR API Simulator",
        Version = "v1"
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Input the JWT token issued by the API Simulator login endpoint.",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

// Register SimulatorJwtOptions for dependency injection
builder.Services.AddSingleton<SimulatorJwtOptions>();

// Register RefreshToken repository as singleton (in-memory storage)
builder.Services.AddSingleton<IRefreshTokenRepository, RefreshTokenRepository>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var clockSkew = builder.Configuration.GetValue<int>("Jwt:ClockSkewMinutes", 5);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = SimulatorJwtOptions.GetStaticSigningKey(builder.Configuration),
            ClockSkew = TimeSpan.FromMinutes(clockSkew)
        };

        // Enable detailed error logging for JWT authentication failures
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"JWT Authentication Failed: {context.Exception.Message}");
                if (context.Exception.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {context.Exception.InnerException.Message}");
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("JWT Token Validated Successfully");
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
// Swagger always enabled (no environment check needed)
app.UseSwagger();
app.UseSwaggerUI();

// HTTPS redirection disabled for HTTP-only IIS deployment
// app.UseHttpsRedirection();

// Add request logging middleware
app.Use(async (context, next) =>
{
    var authHeader = context.Request.Headers["Authorization"].ToString();
    Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Request: {context.Request.Method} {context.Request.Path}");
    Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Auth Header Present: {!string.IsNullOrEmpty(authHeader)}");
    if (!string.IsNullOrEmpty(authHeader))
    {
        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Auth Header: {authHeader.Substring(0, Math.Min(50, authHeader.Length))}...");
    }

    await next();

    Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Response Status: {context.Response.StatusCode}");
    if (context.Response.StatusCode == 401)
    {
        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] ⚠️ 401 UNAUTHORIZED - User.Identity.IsAuthenticated: {context.User?.Identity?.IsAuthenticated}");
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
