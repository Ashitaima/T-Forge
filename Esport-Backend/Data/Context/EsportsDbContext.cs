using Microsoft.EntityFrameworkCore;
using Computational_Practice.Models;
namespace Computational_Practice.Data.Context
{
    public class EsportsDbContext : DbContext
    {
        public EsportsDbContext(DbContextOptions<EsportsDbContext> options) : base(options)
        {
        }

        // DbSets - представляють таблиці в базі даних
        public DbSet<User> Users { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<Tournament> Tournaments { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<MatchPlayer> MatchPlayers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Налаштування для PostgreSQL (lowercase table names)
            ConfigureUserModel(modelBuilder);
            ConfigurePlayerModel(modelBuilder);
            ConfigureTeamModel(modelBuilder);
            ConfigureTournamentModel(modelBuilder);
            ConfigureMatchModel(modelBuilder);
            ConfigureMatchPlayerModel(modelBuilder);
        }

        private void ConfigureUserModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users"); // PostgreSQL convention
                entity.HasKey(e => e.Id);

                // Обов'язкові поля
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Role).IsRequired().HasMaxLength(20);

                // Значення по замовчуванню
                entity.Property(e => e.Role).HasDefaultValue("Player");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Унікальні індекси
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });
        }

        private void ConfigurePlayerModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Player>(entity =>
            {
                entity.ToTable("players");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Nickname).IsRequired().HasMaxLength(30);
                entity.Property(e => e.Position).HasMaxLength(50);
                entity.Property(e => e.Country).HasMaxLength(100);
                entity.Property(e => e.WinRate).HasColumnType("decimal(5,2)");

                // Значення по замовчуванню
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.JoinedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Зв'язок One-to-One з User
                entity.HasOne(p => p.User)
                    .WithOne()
                    .HasForeignKey<Player>(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Зв'язок Many-to-One з Team (гравець може не мати команди)
                entity.HasOne(p => p.Team)
                    .WithMany(t => t.Players)
                    .HasForeignKey(p => p.TeamId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Унікальний нікнейм
                entity.HasIndex(e => e.Nickname).IsUnique();
            });
        }

        private void ConfigureTeamModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Team>(entity =>
            {
                entity.ToTable("teams");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Tag).HasMaxLength(10);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Region).HasMaxLength(50);

                // Значення по замовчуванню
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Зв'язок з капітаном (User)
                entity.HasOne(t => t.Captain)
                    .WithMany()
                    .HasForeignKey(t => t.CaptainId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Унікальна назва команди
                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.Tag).IsUnique();
            });
        }

        private void ConfigureTournamentModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tournament>(entity =>
            {
                entity.ToTable("tournaments");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Game).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).HasMaxLength(20);
                entity.Property(e => e.PrizePool).HasColumnType("decimal(10,2)");

                // Значення по замовчуванню
                entity.Property(e => e.Status).HasDefaultValue("Registration");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Зв'язок з організатором
                entity.HasOne(t => t.Organizer)
                    .WithMany()
                    .HasForeignKey(t => t.OrganizerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureMatchModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Match>(entity =>
            {
                entity.ToTable("matches");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Status).HasMaxLength(20);
                entity.Property(e => e.MatchType).HasMaxLength(20);
                entity.Property(e => e.Format).HasMaxLength(10);
                entity.Property(e => e.Notes).HasMaxLength(500);

                // Значення по замовчуванню
                entity.Property(e => e.Status).HasDefaultValue("Scheduled");
                entity.Property(e => e.MatchType).HasDefaultValue("GroupStage");
                entity.Property(e => e.Format).HasDefaultValue("BO1");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Зв'язки з турніром
                entity.HasOne(m => m.Tournament)
                    .WithMany(t => t.Matches)
                    .HasForeignKey(m => m.TournamentId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Зв'язки з командами
                entity.HasOne(m => m.HomeTeam)
                    .WithMany()
                    .HasForeignKey(m => m.HomeTeamId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.AwayTeam)
                    .WithMany()
                    .HasForeignKey(m => m.AwayTeamId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Зв'язок з командою-переможцем (nullable)
                entity.HasOne(m => m.WinnerTeam)
                    .WithMany()
                    .HasForeignKey(m => m.WinnerTeamId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private void ConfigureMatchPlayerModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MatchPlayer>(entity =>
            {
                entity.ToTable("match_players");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Champion).HasMaxLength(50);

                // Значення по замовчуванню
                entity.Property(e => e.IsStarter).HasDefaultValue(true);

                // Зв'язки
                entity.HasOne(mp => mp.Match)
                    .WithMany(m => m.MatchPlayers)
                    .HasForeignKey(mp => mp.MatchId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(mp => mp.Player)
                    .WithMany(p => p.MatchPlayers)
                    .HasForeignKey(mp => mp.PlayerId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Один гравець може бути тільки раз в одному матчі
                entity.HasIndex(new[] { "MatchId", "PlayerId" }).IsUnique();
            });
        }
    }
}
