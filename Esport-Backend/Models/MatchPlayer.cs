using System.ComponentModel.DataAnnotations;

namespace Computational_Practice.Models
{
    public class MatchPlayer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MatchId { get; set; }

        [Required]
        public int PlayerId { get; set; }

        // Статистика матчу для конкретного гравця
        public int Kills { get; set; } = 0;
        public int Deaths { get; set; } = 0;
        public int Assists { get; set; } = 0;

        [StringLength(50)]
        public string Champion { get; set; } = string.Empty; // Для LoL/Dota

        public bool IsStarter { get; set; } = true; // Стартовий склад чи заміна

        // Navigation Properties
        public virtual Match Match { get; set; } = null!;
        public virtual Player Player { get; set; } = null!;
    }
}
