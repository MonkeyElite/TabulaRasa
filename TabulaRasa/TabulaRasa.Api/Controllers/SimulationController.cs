using Microsoft.AspNetCore.Mvc;
using TabulaRasa.Api.Contracts;
using TabulaRasa.Api.Services;

namespace TabulaRasa.Api.Controllers
{
    [ApiController]
    [Route("api/simulations")]
    public sealed class SimulationController : ControllerBase
    {
        private readonly SimulationRegistry _registry;

        public SimulationController(SimulationRegistry registry)
        {
            _registry = registry;
        }

        [HttpGet]
        public ActionResult<IReadOnlyList<SimulationSummaryDto>> List()
        {
            return _registry.List().ToList();
        }

        [HttpGet("resource-limits")]
        public ActionResult<SimulationResourceLimitsDto> GetResourceLimits()
        {
            return _registry.Limits;
        }

        [HttpPost]
        public ActionResult<SimulationSummaryDto> Create([FromBody] CreateSimulationRequestDto? request)
        {
            try
            {
                SimulationSession session = _registry.Create(request?.Name, request?.Config);
                return CreatedAtAction(nameof(GetStatus), new { simulationId = session.SimulationId }, session.GetSummary());
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(exception.Message);
            }
        }

        [HttpPost("{simulationId}/clone")]
        public ActionResult<SimulationSummaryDto> Clone(string simulationId, [FromBody] CloneSimulationRequestDto? request)
        {
            try
            {
                SimulationSession clone = _registry.Clone(simulationId, request);
                return CreatedAtAction(nameof(GetStatus), new { simulationId = clone.SimulationId }, clone.GetSummary());
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(exception.Message);
            }
        }

        [HttpDelete("{simulationId}")]
        public IActionResult Delete(string simulationId)
        {
            return _registry.Delete(simulationId)
                ? NoContent()
                : NotFound();
        }

        [HttpGet("{simulationId}/status")]
        public ActionResult<SimulationStatusDto> GetStatus(string simulationId)
        {
            SimulationSession? session = _registry.Get(simulationId);
            return session is null ? NotFound() : session.GetStatus();
        }

        [HttpGet("{simulationId}/current")]
        public ActionResult<SimulationSnapshotDto> GetCurrent(string simulationId)
        {
            SimulationSession? session = _registry.Get(simulationId);
            return session is null ? NotFound() : session.GetCurrentSnapshot();
        }

        [HttpGet("{simulationId}/ticks/{tick:long}")]
        public ActionResult<SimulationSnapshotDto> GetTick(string simulationId, long tick)
        {
            SimulationSession? session = _registry.Get(simulationId);
            if (session is null)
            {
                return NotFound();
            }

            SimulationSnapshotDto? snapshot = session.GetSnapshot(tick);
            return snapshot is null ? NotFound() : snapshot;
        }

        [HttpPost("{simulationId}/step")]
        public ActionResult<SimulationSnapshotDto> Step(string simulationId)
        {
            SimulationSession? session = _registry.Get(simulationId);
            if (session is null)
            {
                return NotFound();
            }

            try
            {
                return session.Step();
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(exception.Message);
            }
        }

        [HttpPost("{simulationId}/run")]
        public ActionResult<SimulationStatusDto> Run(string simulationId, [FromBody] RunSimulationRequestDto? request)
        {
            try
            {
                return _registry.Run(simulationId, request?.IntervalMilliseconds ?? 500, request?.Config);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(exception.Message);
            }
        }

        [HttpPost("{simulationId}/pause")]
        public ActionResult<SimulationStatusDto> Pause(string simulationId)
        {
            try
            {
                return _registry.Pause(simulationId);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(exception.Message);
            }
        }

        [HttpPost("{simulationId}/stop")]
        public ActionResult<SimulationStatusDto> Stop(string simulationId)
        {
            try
            {
                return _registry.Stop(simulationId);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost("{simulationId}/reset")]
        public ActionResult<SimulationSnapshotDto> Reset(string simulationId, [FromBody] ResetSimulationRequestDto? request)
        {
            try
            {
                return _registry.Reset(simulationId, request?.Config);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(exception.Message);
            }
        }

        [HttpPost("{simulationId}/config")]
        public ActionResult<SimulationStatusDto> UpdateConfig(string simulationId, [FromBody] UpdateSimulationConfigRequestDto request)
        {
            try
            {
                return _registry.UpdateConfig(simulationId, request.Config);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(exception.Message);
            }
        }

        [HttpGet("{simulationId}/draft")]
        public ActionResult<SimulationDraftDto> GetDraft(string simulationId)
        {
            SimulationSession? session = _registry.Get(simulationId);
            return session is null ? NotFound() : session.GetDraft();
        }

        [HttpGet("{simulationId}/draft-schema")]
        public ActionResult<SimulationDraftSchemaDto> GetDraftSchema(string simulationId)
        {
            SimulationSession? session = _registry.Get(simulationId);
            return session is null ? NotFound() : session.GetDraftSchema();
        }

        [HttpPost("{simulationId}/restart-from-draft")]
        public ActionResult<SimulationSnapshotDto> RestartFromDraft(string simulationId, [FromBody] SimulationDraftDto draft)
        {
            try
            {
                RestartFromDraftResult result = _registry.RestartFromDraft(simulationId, draft);

                if (!result.Succeeded)
                {
                    return ValidationProblem(new ValidationProblemDetails(result.Errors));
                }

                return result.Snapshot!;
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(exception.Message);
            }
        }
    }
}
