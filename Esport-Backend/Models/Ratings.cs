using System.ComponentModel.DataAnnotations;
using TForge.Common;

namespace TForge.Models
{
    /// <summary>
    /// Поточний рейтинг команди в одній дисципліні. Ні Team, ні Player не
    /// прив'язані до гри, тож склад, що грає два тайтли, отримує два чесні
    /// рейтинги замість одного змішаного, який не описує жодного.
    /// </summary>
    public class TeamRating
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TeamId { get; set; }

        [Required]
        [StringLength(50)]
        public string Game { get; set; } = string.Empty; // див. Common/Games.cs

        public int Rating { get; set; } = EloCalculator.BaseRating;

        /// <summary>Найвищий досягнутий рейтинг — його не забирає смуга поразок.</summary>
        public int Peak { get; set; } = EloCalculator.BaseRating;

        /// <summary>Скільки матчів уже враховано. Керує ознайомчим періодом.</summary>
        public int MatchesRated { get; set; } = 0;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual Team Team { get; set; } = null!;
    }

    /// <summary>Поточний рейтинг гравця в одній дисципліні.</summary>
    public class PlayerRating
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PlayerId { get; set; }

        [Required]
        [StringLength(50)]
        public string Game { get; set; } = string.Empty;

        public int Rating { get; set; } = EloCalculator.BaseRating;

        public int Peak { get; set; } = EloCalculator.BaseRating;

        public int MatchesRated { get; set; } = 0;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual Player Player { get; set; } = null!;
    }

    /// <summary>
    /// Незмінний запис однієї зміни рейтингу команди.
    ///
    /// Журнал існує не заради історії як такої. З нього випадають три речі,
    /// яких проста колонка з числом дати не може: подвійне нарахування стає
    /// структурно неможливим (унікальний індекс на (TeamId, MatchId) закриває
    /// дірку, яку лишає MatchService.UpdateAsync), профіль отримує графік
    /// рейтингу, а картка матчу — «+18» поруч із результатом.
    /// </summary>
    public class TeamRatingChange
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TeamId { get; set; }

        [Required]
        [StringLength(50)]
        public string Game { get; set; } = string.Empty;

        [Required]
        public int MatchId { get; set; }

        public int Delta { get; set; }

        public int RatingBefore { get; set; }

        public int RatingAfter { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Team Team { get; set; } = null!;
        public virtual Match Match { get; set; } = null!;
    }

    /// <summary>Незмінний запис однієї зміни рейтингу гравця.</summary>
    public class PlayerRatingChange
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PlayerId { get; set; }

        [Required]
        [StringLength(50)]
        public string Game { get; set; } = string.Empty;

        [Required]
        public int MatchId { get; set; }

        public int Delta { get; set; }

        public int RatingBefore { get; set; }

        public int RatingAfter { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Player Player { get; set; } = null!;
        public virtual Match Match { get; set; } = null!;
    }
}
