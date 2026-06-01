using Microsoft.AspNetCore.Mvc;
using TabulaRasa.Api.Contracts;
using TabulaRasa.Api.Services;

namespace TabulaRasa.Api.Controllers
{
    [ApiController]
    [Route("api/simulation")]
    public sealed class SimulationController : ControllerBase
    {
        private readonly SimulationSessionService _session;

        public SimulationController(SimulationSessionService session)
        {
            _session = session;
        }

        [HttpGet("status")]
        public ActionResult<SimulationStatusDto> GetStatus()
        {
            return _session.GetStatus();
        }

        [HttpGet("current")]
        public ActionResult<SimulationSnapshotDto> GetCurrent()
        {
            return _session.GetCurrentSnapshot();
        }

        [HttpGet("ticks/{tick:long}")]
        public ActionResult<SimulationSnapshotDto> GetTick(long tick)
        {
            SimulationSnapshotDto? snapshot = _session.GetSnapshot(tick);

            return snapshot is null
                ? NotFound()
                : snapshot;
        }

        [HttpPost("step")]
        public ActionResult<SimulationSnapshotDto> Step()
        {
            try
            {
                return _session.Step();
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(exception.Message);
            }
        }

        [HttpPost("run")]
        public ActionResult<SimulationStatusDto> Run([FromBody] RunSimulationRequestDto? request)
        {
            try
            {
                return _session.Run(request?.IntervalMilliseconds ?? 500, request?.Config);
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(exception.Message);
            }
        }

        [HttpPost("pause")]
        public ActionResult<SimulationStatusDto> Pause()
        {
            try
            {
                return _session.Pause();
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(exception.Message);
            }
        }

        [HttpPost("stop")]
        public ActionResult<SimulationStatusDto> Stop()
        {
            return _session.Stop();
        }

        [HttpPost("reset")]
        public ActionResult<SimulationSnapshotDto> Reset([FromBody] ResetSimulationRequestDto? request)
        {
            return _session.Reset(request?.Config);
        }

        [HttpGet("draft")]
        public ActionResult<SimulationDraftDto> GetDraft()
        {
            return _session.GetDraft();
        }

        [HttpGet("draft-schema")]
        public ActionResult<SimulationDraftSchemaDto> GetDraftSchema()
        {
            return _session.GetDraftSchema();
        }

        [HttpPost("restart-from-draft")]
        public ActionResult<SimulationSnapshotDto> RestartFromDraft([FromBody] SimulationDraftDto draft)
        {
            RestartFromDraftResult result = _session.RestartFromDraft(draft);

            if (!result.Succeeded)
            {
                return ValidationProblem(new ValidationProblemDetails(result.Errors));
            }

            return result.Snapshot!;
        }
    }
}
