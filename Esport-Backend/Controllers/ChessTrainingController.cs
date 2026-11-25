using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TForge.DTOs;
using TForge.Services.Interfaces;

namespace TForge.Controllers
{
    /// <summary>
    /// Датасети й версії моделей прогнозу.
    ///
    /// Усюди саме [Authorize(Roles = "Admin")], і це не порушує правило
    /// «рольові атрибути читають Admin,Organizer, ніколи самий Organizer»: те
    /// правило існує, щоб роль не відсікала адміністратора від чужих записів.
    /// Тут жодної власності немає — запуск процесу на сервері справді належить
    /// лише адміністраторові.
    /// </summary>
    [ApiController]
    [Route("api/ml/chess")]
    [Authorize(Roles = "Admin")]
    public class ChessTrainingController : ApiControllerBase
    {
        private readonly IChessTrainingService _training;

        public ChessTrainingController(IChessTrainingService training)
        {
            _training = training;
        }

        /// <summary>Чи є чим навчати і яка версія активна. available = false — не помилка.</summary>
        [HttpGet("status")]
        public async Task<ActionResult<ChessTrainingStatusDto>> GetStatus()
        {
            return Ok(await _training.GetStatusAsync());
        }

        [HttpGet("datasets")]
        public async Task<ActionResult<List<ChessDatasetDto>>> GetDatasets()
        {
            return Ok(await _training.GetDatasetsAsync());
        }

        /// <summary>
        /// multipart: file + name. Content-Type ставить браузер сам разом із
        /// boundary — apiClient навмисно не задає дефолтного.
        /// </summary>
        [HttpPost("datasets")]
        public async Task<ActionResult<ChessDatasetDto>> UploadDataset(
            [FromForm] UploadChessDatasetDto dto, IFormFile file, CancellationToken cancellationToken)
        {
            // Автора визначає токен: UploadedByUserId немає в DTO саме тому.
            var dataset = await _training.UploadDatasetAsync(
                dto.Name, file, GetUserIdOrThrow(), cancellationToken);

            return CreatedAtAction(nameof(GetDatasets), new { }, dataset);
        }

        [HttpDelete("datasets/{id}")]
        public async Task<IActionResult> DeleteDataset(int id)
        {
            await _training.DeleteDatasetAsync(id);
            return NoContent();
        }

        [HttpGet("runs")]
        public async Task<ActionResult<List<TrainingRunDto>>> GetRuns()
        {
            return Ok(await _training.GetRunsAsync());
        }

        /// <summary>Один запуск разом із логом — саме це опитує сторінка, поки навчання йде.</summary>
        [HttpGet("runs/{id}")]
        public async Task<ActionResult<TrainingRunDto>> GetRun(int id)
        {
            return Ok(await _training.GetRunAsync(id));
        }

        [HttpPost("runs")]
        public async Task<ActionResult<TrainingRunDto>> QueueRun([FromBody] CreateTrainingRunDto dto)
        {
            var run = await _training.QueueRunAsync(dto.DatasetId, GetUserIdOrThrow());
            return CreatedAtAction(nameof(GetRun), new { id = run.Id }, run);
        }

        [HttpPost("runs/{id}/activate")]
        public async Task<ActionResult<TrainingRunDto>> ActivateRun(int id)
        {
            return Ok(await _training.ActivateRunAsync(id));
        }

        /// <summary>Повернутись до вбудованої моделі.</summary>
        [HttpPost("baseline/activate")]
        public async Task<ActionResult<ChessTrainingStatusDto>> ActivateBaseline()
        {
            await _training.ActivateBaselineAsync();
            return Ok(await _training.GetStatusAsync());
        }

        [HttpDelete("runs/{id}")]
        public async Task<IActionResult> DeleteRun(int id)
        {
            await _training.DeleteRunAsync(id);
            return NoContent();
        }
    }
}
