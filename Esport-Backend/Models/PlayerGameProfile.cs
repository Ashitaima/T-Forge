using System.ComponentModel.DataAnnotations;

namespace TForge.Models
{
    /// <summary>
    /// Дисципліна, у яку грає гравець, і його роль саме в ній.
    ///
    /// Player.Position лишається однією позицією на весь профіль, і цього
    /// вистачало, доки гравець грав в одну гру. Тут рядок на кожну
    /// дисципліну: один гравець буває Duelist у Valorant і AWPer у CS2, а
    /// одним полем цього не сказати.
    ///
    /// Позицію перевіряє Common/GamePositions.cs — вона мусить належати саме
    /// цій дисципліні. Пара (PlayerId, Game) унікальна: дві ролі в одній грі
    /// означали б, що список суперечить сам собі.
    /// </summary>
    public class PlayerGameProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PlayerId { get; set; }

        [Required]
        [StringLength(50)]
        public string Game { get; set; } = string.Empty;

        /// <summary>Порожня — гравець вказав дисципліну, але не роль у ній.</summary>
        [StringLength(50)]
        public string Position { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Player Player { get; set; } = null!;
    }
}
