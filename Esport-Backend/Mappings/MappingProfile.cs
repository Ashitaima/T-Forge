using AutoMapper;
using TForge.Common;
using TForge.Models;
using TForge.DTOs;

namespace TForge.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.AvatarPath));
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
                .ForMember(dest => dest.OrganizerId, opt => opt.Ignore())
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
            CreateMap<Team, TeamSummaryDto>();
            CreateMap<CreateTeamDto, Team>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CaptainId, opt => opt.Ignore())
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

            // Посилання на Steam будує сервер: клієнтові лишається показати,
            // а не знати канонічний вигляд адреси.
            CreateMap<Player, PlayerDto>()
                .ForMember(dest => dest.SteamProfileUrl,
                    opt => opt.MapFrom(src => GameIdFormats.SteamProfileUrl(src.SteamId64)));
            CreateMap<Player, PlayerSummaryDto>();

            // Акаунти обох сторін їдуть у DTO, щоб клієнт міг повторити
            // перевірку DuelPolicy й не малювати кнопку, яка дасть 403.
            CreateMap<Duel, DuelDto>()
                .ForMember(dest => dest.ChallengerUserId,
                    opt => opt.MapFrom(src => src.ChallengerPlayer.UserId))
                .ForMember(dest => dest.OpponentUserId,
                    opt => opt.MapFrom(src => (int?)src.OpponentPlayer!.UserId))
                .ForMember(dest => dest.IsOpen,
                    opt => opt.MapFrom(src => src.OpponentPlayerId == null));
            CreateMap<CreatePlayerDto, Player>()
                .ForMember(dest => dest.RiotId,
                    opt => opt.MapFrom(src => GameIdFormats.Normalize(src.RiotId)))
                .ForMember(dest => dest.SteamId64,
                    opt => opt.MapFrom(src => GameIdFormats.Normalize(src.SteamId64)))
                .ForMember(dest => dest.BattleTag,
                    opt => opt.MapFrom(src => GameIdFormats.Normalize(src.BattleTag)))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.TotalMatches, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.Wins, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.Losses, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.WinRate, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.Ranking, opt => opt.MapFrom(src => 9999))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.JoinedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.TeamId, opt => opt.Ignore())
                .ForMember(dest => dest.Team, opt => opt.Ignore())
                .ForMember(dest => dest.MatchPlayers, opt => opt.Ignore());
            CreateMap<UpdatePlayerDto, Player>()
                .ForMember(dest => dest.RiotId,
                    opt => opt.MapFrom(src => GameIdFormats.Normalize(src.RiotId)))
                .ForMember(dest => dest.SteamId64,
                    opt => opt.MapFrom(src => GameIdFormats.Normalize(src.SteamId64)))
                .ForMember(dest => dest.BattleTag,
                    opt => opt.MapFrom(src => GameIdFormats.Normalize(src.BattleTag)))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.TotalMatches, opt => opt.Ignore())
                .ForMember(dest => dest.Wins, opt => opt.Ignore())
                .ForMember(dest => dest.Losses, opt => opt.Ignore())
                .ForMember(dest => dest.WinRate, opt => opt.Ignore())
                .ForMember(dest => dest.Ranking, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.JoinedAt, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.TeamId, opt => opt.Ignore())
                .ForMember(dest => dest.Team, opt => opt.Ignore())
                .ForMember(dest => dest.MatchPlayers, opt => opt.Ignore());

            CreateMap<Match, MatchDto>()
                .ForMember(dest => dest.HomeTeamCaptainId,
                    opt => opt.MapFrom(src => src.HomeTeam.CaptainId))
                // Відкритий матч ще не має гостя, тож і капітана в нього немає.
                .ForMember(dest => dest.AwayTeamCaptainId,
                    opt => opt.MapFrom(src => src.AwayTeam == null ? (int?)null : src.AwayTeam.CaptainId))
                .ForMember(dest => dest.IsOpen,
                    opt => opt.MapFrom(src => src.AwayTeamId == null));
            CreateMap<CreateMatchDto, Match>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                // Домашню команду підставляє сервіс: капітан її не надсилає.
                .ForMember(dest => dest.HomeTeamId, opt => opt.Ignore())
                .ForMember(dest => dest.HomeTeamScore, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.AwayTeamScore, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => MatchStatus.Scheduled))
                // Дисципліну визначає сервер за турніром. Без цього Ignore додавання
                // поля Game до CreateMatchDto почало б мовчки приймати її від клієнта.
                .ForMember(dest => dest.Game, opt => opt.Ignore())
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

            CreateMap<TeamMembershipRequest, MembershipRequestDto>()
                .ForMember(dest => dest.TeamName, opt => opt.MapFrom(src => src.Team.Name))
                .ForMember(dest => dest.TeamTag, opt => opt.MapFrom(src => src.Team.Tag))
                .ForMember(dest => dest.PlayerNickname, opt => opt.MapFrom(src => src.Player.Nickname))
                .ForMember(dest => dest.PlayerPosition, opt => opt.MapFrom(src => src.Player.Position))
                .ForMember(dest => dest.PlayerUserId, opt => opt.MapFrom(src => src.Player.UserId));

            CreateMap<TournamentInvitation, TournamentInvitationDto>()
                .ForMember(dest => dest.TournamentName, opt => opt.MapFrom(src => src.Tournament.Name))
                .ForMember(dest => dest.TournamentGame, opt => opt.MapFrom(src => src.Tournament.Game))
                .ForMember(dest => dest.OrganizerId, opt => opt.MapFrom(src => src.Tournament.OrganizerId))
                .ForMember(dest => dest.TeamName, opt => opt.MapFrom(src => src.Team.Name))
                .ForMember(dest => dest.TeamTag, opt => opt.MapFrom(src => src.Team.Tag))
                .ForMember(dest => dest.TeamCaptainId, opt => opt.MapFrom(src => src.Team.CaptainId));

            CreateMap<MatchChallenge, MatchChallengeDto>()
                .ForMember(dest => dest.ChallengerTeamName, opt => opt.MapFrom(src => src.ChallengerTeam.Name))
                .ForMember(dest => dest.ChallengerTeamTag, opt => opt.MapFrom(src => src.ChallengerTeam.Tag))
                .ForMember(dest => dest.OpponentTeamName, opt => opt.MapFrom(src => src.OpponentTeam.Name))
                .ForMember(dest => dest.OpponentTeamTag, opt => opt.MapFrom(src => src.OpponentTeam.Tag));
        }
    }
}
