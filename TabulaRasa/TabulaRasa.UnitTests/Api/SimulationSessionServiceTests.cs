using TabulaRasa.Api.Contracts;
using TabulaRasa.Api.Services;

namespace TabulaRasa.UnitTests.Api
{
    public sealed class SimulationSessionServiceTests
    {
        [Fact]
        public void Reset_CreatesFreshTickZeroSnapshot()
        {
            using SimulationSessionService service = new();

            service.Step();
            SimulationSnapshotDto snapshot = service.Reset();

            Assert.Equal(0, snapshot.Tick);
            Assert.Equal(0, service.GetStatus().MinimumTick);
            Assert.Equal(0, service.GetStatus().MaximumTick);
        }

        [Fact]
        public void Step_AdvancesExactlyOneTick()
        {
            using SimulationSessionService service = new();

            SimulationSnapshotDto snapshot = service.Step();

            Assert.Equal(1, snapshot.Tick);
            Assert.Equal(1, service.GetStatus().CurrentTick);
        }

        [Fact]
        public void HistoricalSnapshots_AreAvailableAfterLaterTicks()
        {
            using SimulationSessionService service = new();

            SimulationSnapshotDto tickZero = service.GetCurrentSnapshot();
            service.Step();
            service.Step();

            SimulationSnapshotDto? historical = service.GetSnapshot(0);

            Assert.NotNull(historical);
            Assert.Equal(tickZero, historical);
            Assert.Equal(2, service.GetCurrentSnapshot().Tick);
        }

        [Fact]
        public void RestartFromDraft_RebuildsEditableStateAndClearsHistory()
        {
            using SimulationSessionService service = new();
            service.Step();

            SimulationDraftDto draft = service.GetDraft() with
            {
                Tick = 12,
                Grid = new EditableGridDto(8, 8, [new GridCellDto(4, 4)]),
                Agents =
                [
                    new EditableAgentDto(
                        "agent-custom",
                        new PositionDto(2.5f, 3.25f),
                        new AgentNeedsDto(4, 5, 6))
                ],
                Food =
                [
                    new EditableFoodDto("food-custom", new PositionDto(1, 1), true)
                ]
            };

            RestartFromDraftResult result = service.RestartFromDraft(draft);

            Assert.True(result.Succeeded);
            Assert.NotNull(result.Snapshot);
            Assert.Equal(12, result.Snapshot.Tick);
            Assert.Equal(8, result.Snapshot.Grid.Width);
            Assert.Equal(8, result.Snapshot.Grid.Height);
            Assert.Contains(new GridCellDto(4, 4), result.Snapshot.Grid.BlockedCells);
            Assert.Equal("agent-custom", result.Snapshot.Agents.Single().Id);
            Assert.Equal(4, result.Snapshot.Agents.Single().Needs.Hunger);
            Assert.Equal("food-custom", result.Snapshot.Food.Single().Id);
            Assert.True(result.Snapshot.Food.Single().IsConsumed);
            Assert.Null(service.GetSnapshot(0));
            Assert.Empty(result.Snapshot.ActiveMovements);
            Assert.Empty(result.Snapshot.Jobs);
            Assert.Empty(result.Snapshot.Reservations);
        }

        [Fact]
        public void RestartFromDraft_ReturnsValidationErrorsForInvalidDraft()
        {
            using SimulationSessionService service = new();
            SimulationDraftDto draft = service.GetDraft() with
            {
                Tick = -1,
                Grid = new EditableGridDto(0, 0, [new GridCellDto(10, 10)])
            };

            RestartFromDraftResult result = service.RestartFromDraft(draft);

            Assert.False(result.Succeeded);
            Assert.Contains(nameof(SimulationDraftDto.Tick), result.Errors.Keys);
            Assert.Contains("grid.width", result.Errors.Keys);
            Assert.Contains("grid.height", result.Errors.Keys);
        }

        [Fact]
        public void DraftSchema_ExposesEditableFieldsFromSimulationTypes()
        {
            using SimulationSessionService service = new();

            SimulationDraftSchemaDto schema = service.GetDraftSchema();

            Assert.Contains(schema.AgentFields, field => field.Path == "position.x" && field.SourceType.EndsWith("AgentEntity"));
            Assert.Contains(schema.AgentFields, field => field.Path == "needs.hunger" && field.SourceType.EndsWith("AgentNeedState"));
            Assert.Contains(schema.FoodFields, field => field.Path == "isConsumed" && field.SourceType.EndsWith("FoodEntity"));
            Assert.Contains(schema.GridFields, field => field.Path == "grid.blockedCells" && field.SourceType.EndsWith("GridMap"));
        }
    }
}
