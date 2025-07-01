
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Computational_Practice.Data.Context;
using Computational_Practice.Data;
using Computational_Practice.Data.Interfaces;
using Computational_Practice.Services.Interfaces;
using Computational_Practice.Services;
using Computational_Practice.Mappings;
using Computational_Practice.Middleware;
using FluentValidation;
using FluentValidation.AspNetCore;
using Computational_Practice.Validators;

namespace Computational_Practice
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<EsportsDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            builder.Services.AddScoped<ITournamentService, TournamentService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<ITeamService, TeamService>();
            builder.Services.AddScoped<IPlayerService, PlayerService>();
            builder.Services.AddScoped<IMatchService, MatchService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<ITokenService, TokenService>();

            builder.Services.AddAutoMapper(typeof(MappingProfile));

            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddFluentValidationClientsideAdapters();
            builder.Services.AddValidatorsFromAssemblyContaining<CreateUserValidator>();

            // JWT Authentication
            var jwtSecretKey = builder.Configuration["Jwt:SecretKey"];
            var key = Encoding.UTF8.GetBytes(jwtSecretKey ?? throw new InvalidOperationException("JWT SecretKey not configured"));

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
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };
            });

            builder.Services.AddAuthorization();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Сідінг бази даних
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<EsportsDbContext>();
                await DbSeeder.SeedAsync(context);
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseMiddleware<ExceptionMiddleware>();

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
