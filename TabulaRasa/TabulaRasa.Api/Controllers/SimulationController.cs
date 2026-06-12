using Microsoft.AspNetCore.Mvc;
using TabulaRasa.Api.Contracts;
using TabulaRasa.Api.Services;
using TabulaRasa.Simulation.Scenarios;

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

        [HttpGet("runs")]
        public ActionResult<SimulationRunPageDto> ListRuns([FromQuery] int offset = 0, [FromQuery] int limit = 50)
        {
            return _registry.ListRuns(offset, limit);
        }

        [HttpGet("runs/{runId}/checkpoints")]
        public ActionResult<IReadOnlyList<SimulationCheckpointSummaryDto>> ListCheckpoints(string runId)
        {
            return _registry.ListCheckpoints(runId).ToList();
        }

        [HttpPost("runs/{runId}/load")]
        public ActionResult<SimulationSummaryDto> LoadRun(string runId)
        {
            try
            {
                return _registry.Load(runId).GetSummary();
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

        [HttpPost("runs/{runId}/fork")]
        public ActionResult<SimulationSummaryDto> ForkRun(string runId, [FromBody] ForkSimulationRunRequestDto? request)
        {
            try
            {
                SimulationSession fork = _registry.ForkRun(runId, request);
                return CreatedAtAction(nameof(GetStatus), new { simulationId = fork.SimulationId }, fork.GetSummary());
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

        [HttpPost("import-scenario")]
        public ActionResult<SimulationSummaryDto> ImportScenario([FromBody] ImportScenarioRequestDto request)
        {
            RestartFromDraftResult result = _registry.ImportScenario(request, out SimulationSession? session);
            if (!result.Succeeded)
            {
                return ValidationProblem(new ValidationProblemDetails(result.Errors));
            }

            return CreatedAtAction(nameof(GetStatus), new { simulationId = session!.SimulationId }, session.GetSummary());
        }

        [HttpPost("storage/retention/apply")]
        public ActionResult<RetentionResultDto> ApplyRetention()
        {
            return _registry.ApplyRetention();
        }

        [HttpGet("resource-limits")]
        public ActionResult<SimulationResourceLimitsDto> GetResourceLimits()
        {
            return _registry.Limits;
        }

        [HttpGet("scenarios")]
        public ActionResult<IReadOnlyList<BuiltInSimulationScenarioDto>> ListBuiltInScenarios()
        {
            return SimulationScenarioCatalog.Names
                .Select(name => new BuiltInSimulationScenarioDto(
                    name,
                    ToDisplayName(name),
                    SimulationSnapshotMapper.ToConfig(SimulationScenarioCatalog.Create(name))))
                .ToList();
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

        [HttpGet("{simulationId}/timeline")]
        public ActionResult<IReadOnlyList<SimulationTimelinePointDto>> GetTimeline(
            string simulationId,
            [FromQuery] long? from = null,
            [FromQuery] long? to = null,
            [FromQuery] int sampleEvery = 1)
        {
            SimulationSession? session = _registry.Get(simulationId);
            return session is null ? NotFound() : session.GetTimeline(from, to, sampleEvery).ToList();
        }

        [HttpPost("{simulationId}/save")]
        public ActionResult<SaveSimulationResponseDto> Save(string simulationId)
        {
            try
            {
                return _registry.Save(simulationId);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpGet("{simulationId}/export-scenario")]
        public ActionResult<ScenarioExportDto> ExportScenario(string simulationId)
        {
            try
            {
                return _registry.ExportScenario(simulationId);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
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

        private static string ToDisplayName(string name)
        {
            return string.Join(
                " ",
                name.Split('-', StringSplitOptions.RemoveEmptyEntries)
                    .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
        }
    }
}
