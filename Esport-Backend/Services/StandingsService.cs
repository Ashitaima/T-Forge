using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TForge.Common;
using TForge.Data.Interfaces;
using TForge.DTOs;
using TForge.Exceptions;
using TForge.Models;
using TForge.Services.Interfaces;

namespace TForge.Services
{
    /// <summary>
    /// Таблиці рахуються на льоту з результатів матчів, а не зберігаються окремо —
    /// тож вони не можуть розійтися з тим, що реально відбулося у сітці.
    /// </summary>
    public class StandingsService : IStandingsService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public StandingsService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<TournamentStandingDto>> GetTournamentStandingsAsync(int tournamentId)
        {
            var tournament = await _unitOfWork.Tournaments.GetWithTeamsAsync(tournamentId)
                ?? throw new EntityNotFoundException("Tournament", tournamentId);

            var matches = (await _unitOfWork.Matches.GetByTournamentAsync(tournamentId))
                .Where(m => m.Round > 0)
                .ToList();

            var maxRound = matches.Count > 0 ? matches.Max(m => m.Round) : 0;

            var rows = tournament.Teams.Select(team =>
            {
                var played = matches
                    .Where(m => m.HomeTeamId == team.Id || m.AwayTeamId == team.Id)
                    .ToList();

                var completed = played.Where(m => m.Status == MatchStatus.Completed).ToList();
                var wins = completed.Count(m => m.WinnerTeamId == team.Id);

                // Вибування у single elimination — це єдина поразка
                var defeat = completed
                    .Where(m => m.WinnerTeamId != null && m.WinnerTeamId != team.Id)
                    .OrderByDescending(m => m.Round)
                    .FirstOrDefault();

                return new
                {
                    Team = team,
                    Played = completed.Count,
                    Wins = wins,
                    Losses = completed.Count - wins,
                    EliminatedInRound = defeat?.Round
                };
            }).ToList();

            // Хто не програв — вище за всіх; далі за раундом вибування, пізніше = краще
            var ordered = rows
                .OrderBy(r => r.EliminatedInRound.HasValue ? 1 : 0)
                .ThenByDescending(r => r.EliminatedInRound ?? 0)
                .ToList();

            var standings = new List<TournamentStandingDto>();
            var place = 1;

            foreach (var group in ordered.GroupBy(r => r.EliminatedInRound))
            {
                var members = group.ToList();

                foreach (var row in members)
                {
                    var stillPlaying = row.EliminatedInRound == null &&
                                       tournament.Status != TournamentStatus.Completed;

                    standings.Add(new TournamentStandingDto
                    {
                        Place = place,
                        Team = _mapper.Map<TeamSummaryDto>(row.Team),
                        Outcome = DescribeOutcome(row.EliminatedInRound, maxRound, matches, stillPlaying),
                        Played = row.Played,
                        Wins = row.Wins,
                        Losses = row.Losses,
                        StillPlaying = stillPlaying
                    });
                }

                // Команди, що вибули на одній стадії, ділять місце
                place += members.Count;
            }

            return standings;
        }

        private static string DescribeOutcome(int? eliminatedInRound, int maxRound, List<Match> matches, bool stillPlaying)
        {
            if (stillPlaying)
            {
                return "У грі";
            }

            if (eliminatedInRound == null)
            {
                return "Чемпіон";
            }

            if (eliminatedInRound == maxRound)
            {
                return "Фіналіст";
            }

            // Назва стадії, на якій команда вибула
            var roundType = matches.FirstOrDefault(m => m.Round == eliminatedInRound)?.MatchType;
            return roundType switch
            {
                MatchTypes.SemiFinal => "1/2 фіналу",
                MatchTypes.QuarterFinal => "1/4 фіналу",
                MatchTypes.RoundOf16 => "1/8 фіналу",
                MatchTypes.RoundOf32 => "1/16 фіналу",
                MatchTypes.PlayIn => "Кваліфікація",
                _ => $"Раунд {eliminatedInRound}"
            };
        }

        public async Task<IEnumerable<TeamStandingDto>> GetTeamStandingsAsync()
        {
            var matches = await _unitOfWork.Matches.GetQueryable()
                .Where(m => m.Status == MatchStatus.Completed && m.WinnerTeamId != null)
                .Select(m => new { m.HomeTeamId, m.AwayTeamId, m.WinnerTeamId, m.MatchType, m.TournamentId })
                .ToListAsync();

            var teams = await _unitOfWork.Teams.GetActiveAsync();

            var rows = teams.Select(team =>
            {
                var played = matches.Where(m => m.HomeTeamId == team.Id || m.AwayTeamId == team.Id).ToList();
                var wins = played.Count(m => m.WinnerTeamId == team.Id);
                var titles = played.Count(m => m.WinnerTeamId == team.Id && m.MatchType == MatchTypes.Final);

                return new TeamStandingDto
                {
                    Team = _mapper.Map<TeamSummaryDto>(team),
                    Played = played.Count,
                    Wins = wins,
                    Losses = played.Count - wins,
                    WinRate = played.Count == 0 ? 0 : Math.Round((decimal)wins / played.Count * 100, 1),
                    Titles = titles
                };
            })
            .OrderByDescending(r => r.Titles)
            .ThenByDescending(r => r.Wins)
            .ThenByDescending(r => r.WinRate)
            .ThenBy(r => r.Team!.Name)
            .ToList();

            for (var i = 0; i < rows.Count; i++)
            {
                rows[i].Rank = i + 1;
            }

            return rows;
        }

        public async Task<TeamSummaryStatsDto> GetTeamSummaryAsync(int teamId)
        {
            var team = await _unitOfWork.Teams.GetByIdAsync(teamId)
                ?? throw new EntityNotFoundException("Team", teamId);

            var matches = await _unitOfWork.Matches.GetQueryable()
                .Where(m => m.HomeTeamId == team.Id || m.AwayTeamId == team.Id)
                .ToListAsync();

            var record = TeamRecordCalculator.CalculateRecord(matches, team.Id);
            var streak = TeamRecordCalculator.CalculateStreak(matches, team.Id);

            return new TeamSummaryStatsDto
            {
                Played = record.Played,
                Wins = record.Wins,
                Losses = record.Losses,
                WinRate = record.WinRate,
                Streak = streak == null ? null : new StreakDto { Type = streak.Type, Count = streak.Count }
            };
        }

        public async Task<PlayerRecordCalculator.PlayerRecord> GetPlayerCareerAsync(int playerId)
        {
            var rows = await _unitOfWork.MatchPlayers.GetQueryable()
                .Include(mp => mp.Match)
                .Where(mp => mp.PlayerId == playerId)
                .ToListAsync();

            return PlayerRecordCalculator.Calculate(rows);
        }

        public async Task<IEnumerable<PlayerStandingDto>> GetPlayerStandingsAsync()
        {
            // Рахуємо лише матчі, що дійсно мають переможця — так само, як PlayerRecordCalculator,
            // щоб таблиця лідерів не розходилась із профілем гравця для нічиїх/незавершених матчів.
            var stats = await _unitOfWork.MatchPlayers.GetQueryable()
                .Include(mp => mp.Match)
                .Include(mp => mp.Player)
                .ThenInclude(p => p.Team)
                .Where(mp => mp.Match.Status == MatchStatus.Completed && mp.Match.WinnerTeamId != null)
                .ToListAsync();

            var rows = stats
                .GroupBy(mp => mp.Player)
                .Select(group =>
                {
                    var kills = group.Sum(mp => mp.Kills);
                    var deaths = group.Sum(mp => mp.Deaths);
                    var assists = group.Sum(mp => mp.Assists);

                    var wins = group.Count(mp =>
                        mp.Match.WinnerTeamId != null && mp.Match.WinnerTeamId == mp.TeamId);

                    return new PlayerStandingDto
                    {
                        Player = _mapper.Map<PlayerSummaryDto>(group.Key),
                        TeamName = group.Key.Team?.Name,
                        Matches = group.Count(),
                        Kills = kills,
                        Deaths = deaths,
                        Assists = assists,
                        Kda = Math.Round((kills + assists) / (double)Math.Max(1, deaths), 2),
                        Wins = wins,
                        Losses = group.Count() - wins,
                        WinRate = group.Count() == 0 ? 0 : Math.Round((decimal)wins / group.Count() * 100, 1)
                    };
                })
                .OrderByDescending(r => r.Kda)
                .ThenByDescending(r => r.Kills)
                .ToList();

            for (var i = 0; i < rows.Count; i++)
            {
                rows[i].Rank = i + 1;
            }

            return rows;
        }
    }
}
