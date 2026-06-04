using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Simulation.Memory;
using TabulaRasa.Simulation.Social;
using TabulaRasa.Simulation.Species;
using TabulaRasa.Simulation.Systems;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Queries;
using TabulaRasa.World.Resources;

namespace TabulaRasa.Simulation.Actions.Validation
{
    public sealed class ActionRequestValidator
    {
        public ActionValidationResult Validate(SimulationState state, ActionRequest request)
        {
            AgentEntity? agentEntity = state.World.Agents.FirstOrDefault(a => a.Id == request.AgentId);

            if (agentEntity is null)
            {
                return ActionValidationResult.Invalid("Agent does not exist.");
            }

            if (state.GetAgentById(request.AgentId) is null)
            {
                return ActionValidationResult.Invalid("Agent state does not exist.");
            }

            if (agentEntity.IsDead)
            {
                return ActionValidationResult.Invalid("Agent is dead.");
            }

            return request.ActionType switch
            {
                AgentActionType.Eat => ValidateEat(state, agentEntity, request),
                AgentActionType.PickUpResource => ValidatePickUpResource(state, agentEntity, request),
                AgentActionType.DropResource => ActionValidationResult.Valid,
                AgentActionType.ConsumeResource => ActionValidationResult.Valid,
                AgentActionType.TransferResource => ActionValidationResult.Valid,
                AgentActionType.Drink => ValidateDrink(state, agentEntity, request),
                AgentActionType.Rest => ActionValidationResult.Valid,
                AgentActionType.Attack => ValidateAttack(state, agentEntity, request),
                AgentActionType.Flee => ValidateFlee(state, agentEntity, request),
                AgentActionType.Reproduce => ValidateReproduce(state, agentEntity, request),
                AgentActionType.Communicate => ValidateCommunicate(state, agentEntity, request),
                AgentActionType.Wander => ValidateWander(state, agentEntity),
                AgentActionType.None => ActionValidationResult.Valid,
                _ => ActionValidationResult.Invalid("Unsupported action type.")
            };
        }

        private static ActionValidationResult ValidateEat(
            SimulationState state,
            AgentEntity agentEntity,
            ActionRequest request)
        {
            if (agentEntity.Inventory.GetQuantity(ResourceDefinition.FoodId) > 0)
            {
                return SpeciesRegistry.NormalizeId(agentEntity.SpeciesId) == SpeciesRegistry.HumanId
                    ? ActionValidationResult.Valid
                    : ActionValidationResult.Invalid("Species cannot eat carried food.");
            }

            if (request.TargetId is not null
                && SpatialQueries.FindAvailablePlantAtInteractionPoint(
                    state.World,
                    agentEntity.Position,
                    request.TargetId) is not null)
            {
                PlantEntity? plant = state.World.Plants.FirstOrDefault(candidate => candidate.Id == request.TargetId);
                return plant is not null && SpeciesRegistry.Get(agentEntity.SpeciesId).CanEatResource(plant.ResourceId)
                    ? ActionValidationResult.Valid
                    : ActionValidationResult.Invalid("Species cannot eat target plant.");
            }

            if (request.TargetId is null)
            {
                return ActionValidationResult.Invalid("Eat action requires carried food or a target.");
            }

            ResourceContainerEntity? container = SpatialQueries.FindAvailableFoodContainerAtInteractionPoint(
                state.World,
                agentEntity.Position,
                request.TargetId);

            if (container is null)
            {
                AgentMemoryService.MarkTargetUnavailable(
                    state,
                    request.AgentId,
                    request.TargetId,
                    "Target resource container is unavailable or out of reach.");
                return ActionValidationResult.Invalid("Target resource container is unavailable or out of reach.");
            }

            return SpeciesRegistry.NormalizeId(agentEntity.SpeciesId) == SpeciesRegistry.HumanId
                ? ActionValidationResult.Valid
                : ActionValidationResult.Invalid("Species cannot eat target resource container.");
        }

        private static ActionValidationResult ValidatePickUpResource(
            SimulationState state,
            AgentEntity agentEntity,
            ActionRequest request)
        {
            if (request.TargetId is null)
            {
                return ActionValidationResult.Invalid("Pick up action requires a target.");
            }

            ResourceContainerEntity? container = state.World.ResourceContainers.FirstOrDefault(candidate =>
                candidate.Id == request.TargetId && !candidate.IsEmpty);

            if (container is not null)
            {
                if (SpatialQueries.FindNearestAvailableInteractionPoint(
                        container,
                        agentEntity.Position,
                        SpatialQueries.DefaultInteractionTolerance) is null)
                {
                    return ActionValidationResult.Invalid("Target resource container is out of reach.");
                }

                return ActionValidationResult.Valid;
            }

            PlantEntity? plant = state.World.Plants.FirstOrDefault(candidate =>
                candidate.Id == request.TargetId && candidate.IsHarvestable);
            if (plant is not null)
            {
                return SpatialQueries.FindNearestAvailableInteractionPoint(
                        plant,
                        agentEntity.Position,
                        SpatialQueries.DefaultInteractionTolerance) is null
                    ? ActionValidationResult.Invalid("Target plant is out of reach.")
                    : ActionValidationResult.Valid;
            }

            ResourceDepositEntity? deposit = state.World.ResourceDeposits.FirstOrDefault(candidate =>
                candidate.Id == request.TargetId && !candidate.IsEmpty);
            if (deposit is not null)
            {
                return SpatialQueries.FindNearestAvailableInteractionPoint(
                        deposit,
                        agentEntity.Position,
                        SpatialQueries.DefaultInteractionTolerance) is null
                    ? ActionValidationResult.Invalid("Target resource deposit is out of reach.")
                    : ActionValidationResult.Valid;
            }

            if (container is null && plant is null && deposit is null)
            {
                return ActionValidationResult.Invalid("Target resource container is unavailable.");
            }

            return ActionValidationResult.Valid;
        }

        private static ActionValidationResult ValidateDrink(
            SimulationState state,
            AgentEntity agentEntity,
            ActionRequest request)
        {
            if (agentEntity.Inventory.GetQuantity(ResourceDefinition.WaterId) > 0)
            {
                return ActionValidationResult.Valid;
            }

            if (request.TargetId is null)
            {
                return ActionValidationResult.Valid;
            }

            WaterSourceEntity? waterSource = SpatialQueries.FindAvailableWaterSourceAtInteractionPoint(
                state.World,
                agentEntity.Position,
                request.TargetId);

            return waterSource is null
                ? ActionValidationResult.Invalid("Target water source is unavailable or out of reach.")
                : ActionValidationResult.Valid;
        }

        private static ActionValidationResult ValidateWander(SimulationState state, AgentEntity agentEntity)
        {
            bool hasDestination = state.World.Grid
                .GetAdjacentCells(SpatialQueries.GetCurrentCell(state.World, agentEntity.Position))
                .Where(cell => state.World.Grid.IsTraversable(cell))
                .Where(cell => !SpatialQueries.IsCellOccupied(state.World, cell, agentEntity.Id))
                .Any();

            return hasDestination
                ? ActionValidationResult.Valid
                : ActionValidationResult.Invalid("Agent has no available adjacent cell to wander to.");
        }

        private static ActionValidationResult ValidateAttack(
            SimulationState state,
            AgentEntity agentEntity,
            ActionRequest request)
        {
            if (request.TargetId is null)
            {
                return ActionValidationResult.Invalid("Attack action requires a target.");
            }

            AgentEntity? target = state.World.Agents.FirstOrDefault(candidate => candidate.Id == request.TargetId);
            if (target is null || target.IsDead)
            {
                return ActionValidationResult.Invalid("Attack target is unavailable.");
            }

            SpeciesDefinition attackerSpecies = SpeciesRegistry.Get(agentEntity.SpeciesId);
            SpeciesDefinition targetSpecies = SpeciesRegistry.Get(target.SpeciesId);
            if (!attackerSpecies.CanAttackSpecies(targetSpecies.Id))
            {
                return ActionValidationResult.Invalid("Species cannot attack target species.");
            }

            return agentEntity.Position.DistanceTo(target.Position) <= SpatialQueries.DefaultInteractionTolerance + 0.5f
                ? ActionValidationResult.Valid
                : ActionValidationResult.Invalid("Attack target is out of reach.");
        }

        private static ActionValidationResult ValidateFlee(
            SimulationState state,
            AgentEntity agentEntity,
            ActionRequest request)
        {
            if (request.TargetId is null)
            {
                return ActionValidationResult.Invalid("Flee action requires a target.");
            }

            AgentEntity? target = state.World.Agents.FirstOrDefault(candidate => candidate.Id == request.TargetId);
            if (target is null || target.IsDead)
            {
                return ActionValidationResult.Invalid("Flee target is unavailable.");
            }

            return SpeciesRegistry.Get(target.SpeciesId).CanAttackSpecies(SpeciesRegistry.Get(agentEntity.SpeciesId).Id)
                ? ActionValidationResult.Valid
                : ActionValidationResult.Invalid("Flee target is not a predator.");
        }

        private static ActionValidationResult ValidateReproduce(
            SimulationState state,
            AgentEntity agentEntity,
            ActionRequest request)
        {
            if (request.TargetId is null)
            {
                return ActionValidationResult.Invalid("Reproduce action requires a target.");
            }

            AgentEntity? mate = state.World.Agents.FirstOrDefault(candidate => candidate.Id == request.TargetId);
            if (mate is null)
            {
                return ActionValidationResult.Invalid("Mate is unavailable.");
            }

            if (!LifecycleSystem.CanReproduce(state, agentEntity, mate))
            {
                return ActionValidationResult.Invalid("Agents cannot reproduce right now.");
            }

            return state.World.Grid.GetAdjacentCells(agentEntity.Position.ToGridCell())
                .Any(cell => state.World.Grid.IsTraversable(cell) && !SpatialQueries.IsCellOccupied(state.World, cell))
                ? ActionValidationResult.Valid
                : ActionValidationResult.Invalid("No free adjacent cell for offspring.");
        }

        private static ActionValidationResult ValidateCommunicate(
            SimulationState state,
            AgentEntity agentEntity,
            ActionRequest request)
        {
            if (request.TargetId is null)
            {
                return ActionValidationResult.Invalid("Communicate action requires a target.");
            }

            AgentEntity? listener = state.World.Agents.FirstOrDefault(candidate => candidate.Id == request.TargetId);
            if (listener is null || listener.IsDead)
            {
                return ActionValidationResult.Invalid("Communication target is unavailable.");
            }

            return SocialService.CanCommunicate(state, agentEntity, listener)
                ? ActionValidationResult.Valid
                : ActionValidationResult.Invalid("Communication target is out of reach or incompatible.");
        }
    }
}
