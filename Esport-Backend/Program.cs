
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TForge.Data.Context;
using TForge.Data;
using TForge.Data.Interfaces;
using TForge.Services.Interfaces;
using TForge.Services;
using TForge.Mappings;
using TForge.Middleware;
using TForge.Common;
using TForge.Hubs;
using FluentValidation;
using FluentValidation.AspNetCore;
using TForge.Validators;
using Microsoft.Extensions.FileProviders;
using System.Threading.RateLimiting;

namespace TForge
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // wwwroot має існувати ще до побудови хоста: інакше WebRootPath
            // лишається null, і UseStaticFiles не віддає нічого — завантажений
            // аватар зберігався б на диск, але повертав 404.
            var webRoot = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
            Directory.CreateDirectory(Path.Combine(webRoot, "uploads", "avatars"));
            builder.Environment.WebRootPath = webRoot;

            // Рядок підключення й ключ підпису живуть у user-secrets (розробка)
            // або у змінних середовища — у репозиторії лишаються тільки порожні
            // місця під них. Порожнє значення означає незроблене налаштування,
            // тож повідомлення має бути зрозумілим, а не помилкою десь у драйвері.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:DefaultConnection не налаштовано. " +
                    "Виконайте: dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"...\" " +
                    "або задайте змінну середовища ConnectionStrings__DefaultConnection.");
            }

            builder.Services.AddDbContext<EsportsDbContext>(options =>
                options.UseNpgsql(connectionString));

            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            builder.Services.AddScoped<ITournamentService, TournamentService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<ITeamService, TeamService>();
            builder.Services.AddScoped<IPlayerService, PlayerService>();
            builder.Services.AddScoped<IMatchService, MatchService>();
builder.Services.AddScoped<IDuelService, DuelService>();
            builder.Services.AddScoped<IBracketService, BracketService>();
            builder.Services.AddScoped<IStandingsService, StandingsService>();
            builder.Services.AddScoped<IMatchRosterService, MatchRosterService>();
            builder.Services.AddScoped<IMembershipRequestService, MembershipRequestService>();
            builder.Services.AddScoped<IMatchChallengeService, MatchChallengeService>();
            builder.Services.AddScoped<ITournamentInvitationService, TournamentInvitationService>();
            builder.Services.AddScoped<IRatingService, RatingService>();
            builder.Services.AddScoped<IImageUploadService, ImageUploadService>();
            builder.Services.AddScoped<IAvatarService, AvatarService>();
            builder.Services.AddScoped<ITeamLogoService, TeamLogoService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<IOrganizerRequestService, OrganizerRequestService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();

            builder.Services.AddAutoMapper(typeof(MappingProfile));

            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddFluentValidationClientsideAdapters();
            builder.Services.AddValidatorsFromAssemblyContaining<CreateUserValidator>();

            // JWT Authentication
            var jwtSecretKey = builder.Configuration["Jwt:SecretKey"];
            if (string.IsNullOrWhiteSpace(jwtSecretKey))
            {
                throw new InvalidOperationException(
                    "Jwt:SecretKey не налаштовано. " +
                    "Виконайте: dotnet user-secrets set \"Jwt:SecretKey\" \"...\" " +
                    "або задайте змінну середовища Jwt__SecretKey.");
            }

            var key = Encoding.UTF8.GetBytes(jwtSecretKey);

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

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Frontend", policy =>
                {
                    policy
                        .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            builder.Services.AddSignalR();

            // Вхід і реєстрація — єдині відкриті точки, де має сенс перебір.
            // Вікно на IP: людині, що двічі помилилася паролем, це непомітно,
            // а словниковий перебір стає безглуздим.
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy("auth", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));
            });

            // Дати з тіла запиту приходять без зсуву (див. UtcDateTimeConverter):
            // без цього Npgsql відмовляється писати їх у timestamptz, і будь-яке
            // створення матчу чи турніру падало з 500.
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
                options.JsonSerializerOptions.Converters.Add(new NullableUtcDateTimeConverter());
            });
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Міграції + сідінг бази даних
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<EsportsDbContext>();
                var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
                var ratingService = scope.ServiceProvider.GetRequiredService<IRatingService>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                await DatabaseInitializer.InitializeAsync(context, passwordHasher, ratingService, logger);
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseMiddleware<ExceptionMiddleware>();

            app.UseCors("Frontend");

            // Після UseCors: інакше відповідь 429 приходить без заголовків CORS,
            // і фронтенд бачить мережеву помилку замість зрозумілого статусу.
            app.UseRateLimiter();

            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            // Аватари лежать файлами на диску, а не в базі — віддає їх статика.
            // Провайдер задаємо явно: покладатися на WebRootPath ризиковано,
            // бо на чистій машині теки ще немає в момент старту.
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(webRoot)
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<MatchHub>("/hubs/matches");

            app.Run();
        }
    }
}
