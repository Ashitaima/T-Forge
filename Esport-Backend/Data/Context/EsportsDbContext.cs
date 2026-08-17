using Microsoft.EntityFrameworkCore;
using TForge.Common;
using TForge.Models;
namespace TForge.Data.Context
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
        public DbSet<TeamMembershipRequest> TeamMembershipRequests { get; set; }
        public DbSet<MatchChallenge> MatchChallenges { get; set; }
        public DbSet<TournamentInvitation> TournamentInvitations { get; set; }
        public DbSet<TeamRating> TeamRatings { get; set; }
        public DbSet<PlayerRating> PlayerRatings { get; set; }
        public DbSet<TeamRatingChange> TeamRatingChanges { get; set; }
        public DbSet<PlayerRatingChange> PlayerRatingChanges { get; set; }

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
            ConfigureTeamMembershipRequestModel(modelBuilder);
            ConfigureTournamentInvitationModel(modelBuilder);
            ConfigureRatingModels(modelBuilder);
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
                entity.Property(e => e.AvatarPath).HasMaxLength(200);

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
                // WithOne(u => u.PlayerProfile), а не WithOne(): без імені навігації
                // EF не бачить User.PlayerProfile, вважає її окремим звʼязком
                // і створює тіньову колонку players.UserId1.
                entity.HasOne(p => p.User)
                    .WithOne(u => u.PlayerProfile)
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

                // Зв'язок з капітаном (User). Іменована навігація прибирає
                // тіньову колонку teams.UserId, яку EF створював для CaptainedTeams.
                entity.HasOne(t => t.Captain)
                    .WithMany(u => u.CaptainedTeams)
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
                // Іменована навігація прибирає тіньову колонку tournaments.UserId.
                entity.HasOne(t => t.Organizer)
                    .WithMany(u => u.OrganizedTournaments)
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
                entity.Property(e => e.StreamUrl).HasMaxLength(300);
                entity.Property(e => e.TrackerUrl).HasMaxLength(300);
                entity.Property(e => e.Game).HasMaxLength(50).IsRequired().HasDefaultValue("");

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
                // Іменовані навігації прибирають тіньову колонку matches.TeamId1
                // і роблять Team.HomeMatches/AwayMatches придатними для запитів:
                // доти вони читали не ту колонку й давали хибні підсумки.
                entity.HasOne(m => m.HomeTeam)
                    .WithMany(t => t.HomeMatches)
                    .HasForeignKey(m => m.HomeTeamId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.AwayTeam)
                    .WithMany(t => t.AwayMatches)
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

                // Команду матчу не можна видалити, поки на неї посилається історія
                entity.HasOne(mp => mp.Team)
                    .WithMany()
                    .HasForeignKey(mp => mp.TeamId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Один гравець може бути тільки раз в одному матчі
                entity.HasIndex(new[] { "MatchId", "PlayerId" }).IsUnique();
            });
        }

        private void ConfigureTeamMembershipRequestModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TeamMembershipRequest>(entity =>
            {
                entity.ToTable("team_membership_requests");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Direction).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);

                entity.HasOne(r => r.Team)
                    .WithMany()
                    .HasForeignKey(r => r.TeamId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Player)
                    .WithMany()
                    .HasForeignKey(r => r.PlayerId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Лише один активний запит на пару (команда, гравець), незалежно від напряму.
                // Термінальні запити до індексу не входять — саме тому після відмови
                // можна подати заявку повторно.
                entity.HasIndex(r => new { r.TeamId, r.PlayerId })
                    .IsUnique()
                    .HasFilter($"\"Status\" = '{MembershipRequestStatus.Pending}'");
            });

            modelBuilder.Entity<MatchChallenge>(entity =>
            {
                entity.ToTable("match_challenges");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Game).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Format).HasMaxLength(10).HasDefaultValue("BO1");
                entity.Property(e => e.Message).HasMaxLength(300);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20)
                    .HasDefaultValue(MatchChallengeStatus.Pending);

                entity.HasOne(c => c.ChallengerTeam)
                    .WithMany()
                    .HasForeignKey(c => c.ChallengerTeamId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.OpponentTeam)
                    .WithMany()
                    .HasForeignKey(c => c.OpponentTeamId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Матч лишається, навіть якщо виклик колись видалять.
                entity.HasOne(c => c.Match)
                    .WithMany()
                    .HasForeignKey(c => c.MatchId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Лише один відкритий виклик у цьому напрямі. Зустрічний виклик
                // (суперник викликає у відповідь) відсіює сервіс — індекс його
                // не бачить, бо пара колонок там у зворотному порядку.
                entity.HasIndex(c => new { c.ChallengerTeamId, c.OpponentTeamId })
                    .IsUnique()
                    .HasFilter($"\"Status\" = '{MatchChallengeStatus.Pending}'");
            });
        }

        private void ConfigureTournamentInvitationModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TournamentInvitation>(entity =>
            {
                entity.ToTable("tournament_invitations");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Direction).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20)
                    .HasDefaultValue(TournamentInvitationStatus.Pending);
                entity.Property(e => e.Message).HasMaxLength(300);

                entity.HasOne(i => i.Tournament)
                    .WithMany()
                    .HasForeignKey(i => i.TournamentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(i => i.Team)
                    .WithMany()
                    .HasForeignKey(i => i.TeamId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Лише один відкритий запит на пару (турнір, команда), незалежно
                // від напряму. Термінальні запити до індексу не входять — саме
                // тому після відмови можна подати заявку повторно.
                entity.HasIndex(i => new { i.TournamentId, i.TeamId })
                    .IsUnique()
                    .HasFilter($"\"Status\" = '{TournamentInvitationStatus.Pending}'");
            });
        }

        /// <summary>
        /// Дві пари таблиць, а не одна поліморфна з колонкою SubjectType.
        /// Поліморфний варіант коротший, але його SubjectId не може нести
        /// справжній зовнішній ключ, а на лагодження п'яти тіньових FK уже
        /// пішла ціла фаза роботи. Краще дати EF два однозначні зв'язки, ніж
        /// один, від якого його доводиться відмовляти.
        /// </summary>
        private void ConfigureRatingModels(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TeamRating>(entity =>
            {
                entity.ToTable("team_ratings");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Game).IsRequired().HasMaxLength(50);

                entity.HasOne(r => r.Team)
                    .WithMany()
                    .HasForeignKey(r => r.TeamId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Один рейтинг на пару (команда, дисципліна)
                entity.HasIndex(r => new { r.TeamId, r.Game }).IsUnique();
            });

            modelBuilder.Entity<PlayerRating>(entity =>
            {
                entity.ToTable("player_ratings");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Game).IsRequired().HasMaxLength(50);

                entity.HasOne(r => r.Player)
                    .WithMany()
                    .HasForeignKey(r => r.PlayerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(r => new { r.PlayerId, r.Game }).IsUnique();
            });

            modelBuilder.Entity<TeamRatingChange>(entity =>
            {
                entity.ToTable("team_rating_changes");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Game).IsRequired().HasMaxLength(50);

                entity.HasOne(c => c.Team)
                    .WithMany()
                    .HasForeignKey(c => c.TeamId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.Match)
                    .WithMany()
                    .HasForeignKey(c => c.MatchId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Саме цей індекс робить подвійне нарахування неможливим:
                // сервіс перевіряє наявність рядка перед нарахуванням, а індекс
                // ловить те, що прослизнуло повз перевірку через гонку.
                entity.HasIndex(c => new { c.TeamId, c.MatchId }).IsUnique();
            });

            modelBuilder.Entity<PlayerRatingChange>(entity =>
            {
                entity.ToTable("player_rating_changes");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Game).IsRequired().HasMaxLength(50);

                entity.HasOne(c => c.Player)
                    .WithMany()
                    .HasForeignKey(c => c.PlayerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.Match)
                    .WithMany()
                    .HasForeignKey(c => c.MatchId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(c => new { c.PlayerId, c.MatchId }).IsUnique();
            });
        }
    }
}
