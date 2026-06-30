using Application.Interfaces;
using Application.Common;
using Application.UseCases.Auth.Commands.DeleteUser;
using Application.UseCases.Auth.Commands.Login;
using Application.UseCases.Auth.Commands.Logout;
using Application.UseCases.Auth.Commands.RefreshToken;
using Application.UseCases.Auth.Commands.RegisterUser;
using Application.UseCases.Auth.Commands.UpdateUser;
using Application.UseCases.Auth.Queries.GetAllRoles;
using Application.UseCases.Auth.Queries.GetAllUsers;
using Application.UseCases.Users.Queries.UserExists;
using System.Text;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Security.Claims;
using Infrastructure.Service;
using API.Middlewares;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<IJwtService>(sp => sp.GetRequiredService<JwtService>());
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IGetAllRolesQueryHandler, GetAllRolesQueryHandler>();
builder.Services.AddScoped<IGetAllUsersQueryHandler, GetAllUsersQueryHandler>();
builder.Services.AddScoped<IRegisterUserCommandHandler, RegisterUserCommandHandler>();
builder.Services.AddScoped<ILoginCommandHandler, LoginCommandHandler>();
builder.Services.AddScoped<ILogoutCommandHandler, LogoutCommandHandler>();
builder.Services.AddScoped<IRefreshTokenCommandHandler, RefreshTokenCommandHandler>();
builder.Services.AddScoped<IUpdateUserCommandHandler, UpdateUserCommandHandler>();
builder.Services.AddScoped<IDeleteUserCommandHandler, DeleteUserCommandHandler>();
builder.Services.AddScoped<IUserExistsQueryHandler, UserExistsQueryHandler>();

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Values
                .SelectMany(modelState => modelState.Errors)
                .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "La solicitud es invalida." : error.ErrorMessage)
                .ToArray();

            return new BadRequestObjectResult(new ErrorResponseDto
            {
                Message = errors.Length == 0 ? "La solicitud es invalida." : string.Join(" ", errors),
                StatusCode = StatusCodes.Status400BadRequest,
                Timestamp = DateTime.UtcNow
            });
        };
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AuthDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services
    .AddIdentity<AppUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;  
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = true;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddDefaultTokenProviders();

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("Falta la configuracion Jwt:Key.");

builder.Services
.AddAuthentication(options =>
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
            Encoding.UTF8.GetBytes(jwtKey)
        ),

        RoleClaimType = ClaimTypes.Role
    };
});

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Ingrese: Bearer {token}"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await dbContext.Database.MigrateAsync();
    await Seeders.SeedAsync(scope.ServiceProvider);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();

if (!string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase))
    app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();
