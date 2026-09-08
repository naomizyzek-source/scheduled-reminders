using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ScheduledReminders.Api.Auth;
using ScheduledReminders.Api.Data;
using ScheduledReminders.Api.Endpoints;
using ScheduledReminders.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var jwtOptions = BindJwtOptions(builder);
builder.Services.Configure<JwtOptions>(options =>
{
    options.Issuer = jwtOptions.Issuer;
    options.Audience = jwtOptions.Audience;
    options.ExpiryHours = jwtOptions.ExpiryHours;
    options.Key = jwtOptions.Key;
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("ScheduledReminders"));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IReminderService, ReminderService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDev", policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Scheduled Reminders API",
        Version = "v1",
        Description = "Backend foundation for the scheduled reminders take-home assignment."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Paste the token from POST /api/auth/login.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    DbSeeder.Seed(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AngularDev");
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapReminderEndpoints();

app.Run();

static JwtOptions BindJwtOptions(WebApplicationBuilder builder)
{
    var options = new JwtOptions();
    builder.Configuration.GetSection(JwtOptions.SectionName).Bind(options);

    if (string.IsNullOrWhiteSpace(options.Issuer))
    {
        options.Issuer = "ScheduledReminders";
    }

    if (string.IsNullOrWhiteSpace(options.Audience))
    {
        options.Audience = "ScheduledReminders.Api";
    }

    if (options.ExpiryHours <= 0)
    {
        options.ExpiryHours = 8;
    }

    if (string.IsNullOrWhiteSpace(options.Key))
    {
        // Development-only fallback so the API runs after a clone without extra secrets.
        // Override Jwt:Key in configuration or environment for any non-local use.
        options.Key = "DEV-ONLY-do-not-use-in-production-scheduled-reminders-signing-key";
    }

    if (Encoding.UTF8.GetByteCount(options.Key) < 32)
    {
        throw new InvalidOperationException("Jwt:Key must be at least 32 bytes for HMAC-SHA256.");
    }

    return options;
}
