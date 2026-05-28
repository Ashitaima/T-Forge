using AutoMapper;
using Computational_Practice.Models;
using Computational_Practice.DTOs;

namespace Computational_Practice.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<User, UserDto>();
            CreateMap<CreateUserDto, User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.MapFrom(src => src.Password))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.LastLoginAt, opt => opt.MapFrom(src => DateTime.UtcNow));
            CreateMap<UpdateUserDto, User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Username, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore());

            CreateMap<Tournament, TournamentDto>();
            CreateMap<CreateTournamentDto, Tournament>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CurrentTeams, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "Registration"))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Organizer, opt => opt.Ignore())
                .ForMember(dest => dest.Teams, opt => opt.Ignore())
                .ForMember(dest => dest.Matches, opt => opt.Ignore());
            CreateMap<UpdateTournamentDto, Tournament>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CurrentTeams, opt => opt.Ignore())
                .ForMember(dest => dest.OrganizerId, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Organizer, opt => opt.Ignore())
                .ForMember(dest => dest.Teams, opt => opt.Ignore())
                .ForMember(dest => dest.Matches, opt => opt.Ignore());

            CreateMap<Team, TeamDto>();
            CreateMap<CreateTeamDto, Team>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Captain, opt => opt.Ignore())
                .ForMember(dest => dest.Players, opt => opt.Ignore());
            CreateMap<UpdateTeamDto, Team>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CaptainId, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Captain, opt => opt.Ignore())
                .ForMember(dest => dest.Players, opt => opt.Ignore());

            CreateMap<Player, PlayerDto>();
            CreateMap<CreatePlayerDto, Player>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TotalMatches, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.Wins, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.Losses, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.WinRate, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.Ranking, opt => opt.MapFrom(src => 9999))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.JoinedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Team, opt => opt.Ignore())
                .ForMember(dest => dest.MatchPlayers, opt => opt.Ignore());
            CreateMap<UpdatePlayerDto, Player>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.Nickname, opt => opt.Ignore())
                .ForMember(dest => dest.TotalMatches, opt => opt.Ignore())
                .ForMember(dest => dest.Wins, opt => opt.Ignore())
                .ForMember(dest => dest.Losses, opt => opt.Ignore())
                .ForMember(dest => dest.WinRate, opt => opt.Ignore())
                .ForMember(dest => dest.Ranking, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.JoinedAt, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Team, opt => opt.Ignore())
                .ForMember(dest => dest.MatchPlayers, opt => opt.Ignore());

            CreateMap<Match, MatchDto>();
            CreateMap<CreateMatchDto, Match>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.HomeTeamScore, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.AwayTeamScore, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "Scheduled"))
                .ForMember(dest => dest.WinnerTeamId, opt => opt.Ignore())
                .ForMember(dest => dest.StartedAt, opt => opt.Ignore())
                .ForMember(dest => dest.EndedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Tournament, opt => opt.Ignore())
                .ForMember(dest => dest.HomeTeam, opt => opt.Ignore())
                .ForMember(dest => dest.AwayTeam, opt => opt.Ignore())
                .ForMember(dest => dest.WinnerTeam, opt => opt.Ignore())
                .ForMember(dest => dest.MatchPlayers, opt => opt.Ignore());
            CreateMap<UpdateMatchDto, Match>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TournamentId, opt => opt.Ignore())
                .ForMember(dest => dest.HomeTeamId, opt => opt.Ignore())
                .ForMember(dest => dest.AwayTeamId, opt => opt.Ignore())
                .ForMember(dest => dest.MatchType, opt => opt.Ignore())
                .ForMember(dest => dest.Format, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Tournament, opt => opt.Ignore())
                .ForMember(dest => dest.HomeTeam, opt => opt.Ignore())
                .ForMember(dest => dest.AwayTeam, opt => opt.Ignore())
                .ForMember(dest => dest.WinnerTeam, opt => opt.Ignore())
                .ForMember(dest => dest.MatchPlayers, opt => opt.Ignore());

            CreateMap<MatchPlayer, MatchPlayerDto>();
        }
    }
}
