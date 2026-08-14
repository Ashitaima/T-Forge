using Microsoft.AspNetCore.Mvc;
using TForge.DTOs;
using TForge.Services.Interfaces;

namespace TForge.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StandingsController : ApiControllerBase
    {
        private readonly IStandingsService _standingsService;

        public StandingsController(IStandingsService standingsService)
        {
            _standingsService = standingsService;
        }

        /// <summary>Загальна таблиця команд по всіх турнірах.</summary>
        [HttpGet("teams")]
        public async Task<ActionResult<IEnumerable<TeamStandingDto>>> GetTeamStandings()
        {
            return Ok(await _standingsService.GetTeamStandingsAsync());
        }

        /// <summary>Таблиця гравців за статистикою з ростерів матчів.</summary>
        [HttpGet("players")]
        public async Task<ActionResult<IEnumerable<PlayerStandingDto>>> GetPlayerStandings()
        {
            return Ok(await _standingsService.GetPlayerStandingsAsync());
        }
    }
}
