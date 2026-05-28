namespace Computational_Practice.DTOs
{
    public class TeamDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserDto? Captain { get; set; }
        public List<PlayerDto> Players { get; set; } = new();
    }

    public class CreateTeamDto
    {
        public string Name { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public int CaptainId { get; set; }
    }

    public class UpdateTeamDto
    {
        public string Name { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
    }
}
