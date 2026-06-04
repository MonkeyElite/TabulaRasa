using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Agents.Models;
using TabulaRasa.Agents.Minds;
using TabulaRasa.Agents.Needs;
using TabulaRasa.Abstractions.Entities;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Simulation.State;
using TabulaRasa.Simulation.Knowledge;
using TabulaRasa.Simulation.Lifecycle;
using TabulaRasa.Simulation.Social;
using TabulaRasa.Simulation.Species;
using TabulaRasa.Simulation.Systems;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Mutation;
using TabulaRasa.World.Queries;
using TabulaRasa.World.Resources;

namespace TabulaRasa.Simulation.Actions.Resolution
{
    public sealed class ActionResolver
    {
        private readonly WorldMutationService _mutations;

        public ActionResolver()
            : this(new WorldMutationService())
        {
        }

        public ActionResolver(WorldMutationService mutations)
        {
            _mutations = mutations;
        }

        public ActionResult Resolve(SimulationState state, ActionRequest request)
        {
            return request.ActionType switch
            {
                AgentActionType.Eat => ResolveEat(state, request),
                AgentActionType.PickUpResource => ResolvePickUpResource(state, request),
                AgentActionType.DropResource => ResolveDropResource(state, request),
                AgentActionType.ConsumeResource => ResolveConsumeResource(state, request),
                AgentActionType.Drink => ResolveDrink(state, request),
                AgentActionType.Rest => ResolveRest(state, request),
                AgentActionType.Attack => ResolveAttack(state, request),
                AgentActionType.Flee => new ActionResult(request.AgentId, request.ActionType, true),
                AgentActionType.Reproduce => ResolveReproduce(state, request),
                AgentActionType.Communicate => ResolveCommunicate(state, request),
                AgentActionType.Experiment => ResolveExperiment(state, request),
                AgentActionType.Craft => ResolveCraft(state, request),
                AgentActionType.Wander => ResolveWander(state, request),
                AgentActionType.None => new ActionResult(request.AgentId, request.ActionType, true),
                _ => new ActionResult(request.AgentId, request.ActionType, false, "Unsupported action type.")
            };
        }

        private ActionResult ResolveEat(SimulationState state, ActionRequest request)
        {
            AgentEntity? agentEntity = state.World.Agents.FirstOrDefault(a => a.Id == request.AgentId);
            AgentState? agentState = state.GetAgentById(request.AgentId);

            if (agentEntity is null || agentState is null)
            {
                return new ActionResult(request.AgentId, request.ActionType, false, "Eat action could not be resolved.");
            }

            SpeciesDefinition species = SpeciesRegistry.Get(agentEntity.SpeciesId);
            if (agentEntity.Inventory.GetQuantity(ResourceDefinition.FoodId) > 0
                && species.Id != SpeciesRegistry.HumanId)
            {
                return new ActionResult(request.AgentId, request.ActionType, false, "Species cannot eat carried food.");
            }

            if (agentEntity.Inventory.GetQuantity(ResourceDefinition.FoodId) == 0)
            {
                if (request.TargetId is null)
                {
                    return new ActionResult(request.AgentId, request.ActionType, false, "No food is available to eat.");
                }

                PlantEntity? plant = SpatialQueries.FindAvailablePlantAtInteractionPoint(
                    state.World,
                    agentEntity.Position,
                    request.TargetId);
                if (plant is not null)
                {
                    if (!species.CanEatResource(plant.ResourceId))
                    {
                        return new ActionResult(request.AgentId, request.ActionType, false, "Species cannot eat target plant.");
                    }

                    WorldMutationResult harvest = _mutations.TryHarvestPlant(
                        state.World,
                        agentEntity.Id,
                        plant.Id,
                        quantity: 1);
                    if (!harvest.Succeeded)
                    {
                        return new ActionResult(
                            request.AgentId,
                            request.ActionType,
                            false,
                            harvest.Reason ?? "Plant could not be harvested.");
                    }
                }
                else
                {
                if (species.Id != SpeciesRegistry.HumanId)
                {
                    return new ActionResult(request.AgentId, request.ActionType, false, "Species cannot eat target resource container.");
                }

                ResourceContainerEntity? container = SpatialQueries.FindAvailableFoodContainerAtInteractionPoint(
                    state.World,
                    agentEntity.Position,
                    request.TargetId);

                if (container is null)
                {
                    return new ActionResult(
                        request.AgentId,
                        request.ActionType,
                        false,
                        "Target resource container became unavailable before resolution.");
                }

                WorldMutationResult pickup = _mutations.TryPickUpResource(
                    state.World,
                    agentEntity.Id,
                    container.Id,
                    ResourceDefinition.FoodId,
                    quantity: 1);

                if (!pickup.Succeeded)
                {
                    return new ActionResult(
                        request.AgentId,
                        request.ActionType,
                        false,
                        pickup.Reason ?? "Food could not be picked up.");
                }
                }
            }

            WorldMutationResult mutation = _mutations.TryConsumeResource(
                state.World,
                agentEntity.Inventory,
                ResourceDefinition.FoodId,
                quantity: 1);

            if (!mutation.Succeeded)
            {
                return new ActionResult(
                    request.AgentId,
                    request.ActionType,
                    false,
                    mutation.Reason ?? "Food could not be consumed.");
            }

            ApplyNeedEffects(
                agentState.NeedState,
                state.World.ResourceDefinitionsById[ResourceDefinition.FoodId].NeedEffects);

            return new ActionResult(request.AgentId, request.ActionType, true);
        }

        private ActionResult ResolveAttack(SimulationState state, ActionRequest request)
        {
            if (request.TargetId is null)
            {
                return new ActionResult(request.AgentId, request.ActionType, false, "Attack action requires a target.");
            }

            AgentEntity? attacker = state.World.Agents.FirstOrDefault(agent => agent.Id == request.AgentId);
            AgentEntity? target = state.World.Agents.FirstOrDefault(agent => agent.Id == request.TargetId);
            AgentState? attackerState = state.GetAgentById(request.AgentId);

            if (attacker is null || target is null || attackerState is null || target.IsDead)
            {
                return new ActionResult(request.AgentId, request.ActionType, false, "Attack action could not be resolved.");
            }

            SpeciesDefinition attackerSpecies = SpeciesRegistry.Get(attacker.SpeciesId);
            SpeciesDefinition targetSpecies = SpeciesRegistry.Get(target.SpeciesId);
            if (!attackerSpecies.CanAttackSpecies(targetSpecies.Id))
            {
                return new ActionResult(request.AgentId, request.ActionType, false, "Species cannot attack target species.");
            }

            target.Health.Current = Math.Max(0, target.Health.Current - attackerSpecies.AttackDamage);
            attackerState.NeedState.Hunger = NeedSystem.ClampNeed(attackerState.NeedState.Hunger - 5);
            state.EmitEvent(
                "agent.attacked",
                "Action Resolver",
                $"{attacker.Id} attacked {target.Id}.",
                target.Id,
                new Dictionary<string, string>
                {
                    ["attackerId"] = attacker.Id,
                    ["targetId"] = target.Id,
                    ["damage"] = attackerSpecies.AttackDamage.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
                    ["health"] = target.Health.Current.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
                });
            SocialService.RecordAttack(state, attacker.Id, target.Id);

            if (target.Health.IsDepleted)
            {
                AgentLifecycleService.MarkDead(state, target, "Action Resolver", "predation");
            }

            return new ActionResult(request.AgentId, request.ActionType, true);
        }

        private ActionResult ResolveReproduce(SimulationState state, ActionRequest request)
        {
            if (request.TargetId is null)
            {
                return new ActionResult(request.AgentId, request.ActionType, false, "Reproduce action requires a target.");
            }

            AgentEntity? first = state.World.Agents.FirstOrDefault(agent => agent.Id == request.AgentId);
            AgentEntity? second = state.World.Agents.FirstOrDefault(agent => agent.Id == request.TargetId);
            if (first is null || second is null || !LifecycleSystem.CanReproduce(state, first, second))
            {
                return new ActionResult(request.AgentId, request.ActionType, false, "Agents cannot reproduce right now.");
            }

            WorldPosition? childPosition = FindFreeAdjacentPosition(state, first);
            if (childPosition is null)
            {
                return new ActionResult(request.AgentId, request.ActionType, false, "No free adjacent cell for offspring.");
            }

            SpeciesDefinition species = SpeciesRegistry.Get(first.SpeciesId);
            string childId = NextAgentId(state, species.Id);
            AgentEntity child = new()
            {
                Id = childId,
                Position = childPosition.Value,
                SpeciesId = species.Id,
                BornTick = state.ActiveTick,
                AgeTicks = 0,
                Health = new EntityHealth(species.MaxHealth),
                ParentIds = { first.Id, second.Id }
            };
            first.OffspringIds.Add(child.Id);
            second.OffspringIds.Add(child.Id);
            first.LastReproducedTick = state.ActiveTick;
            second.LastReproducedTick = state.ActiveTick;
            SocialService.RecordReproduction(state, first.Id, second.Id);
            state.World.Agents.Add(child);
            state.Agents.Add(new AgentState(
                child.Id,
                new AgentNeedState { Hunger = 1, Thirst = 1, Energy = 10, Fatigue = 0 },
                new DefaultAgentMind()));
            state.EmitEvent(
                "agent.born",
                "Action Resolver",
                $"{child.Id} was born.",
                child.Id,
                new Dictionary<string, string>
                {
                    ["speciesId"] = species.Id,
                    ["parentIds"] = string.Join(",", child.ParentIds)
                });

            return new ActionResult(request.AgentId, request.ActionType, true);
        }

        private static ActionResult ResolveCommunicate(SimulationState state, ActionRequest request)
        {
            if (request.TargetId is null)
            {
                return new ActionResult(request.AgentId, request.ActionType, false, "Communicate action requires a target.");
            }

            AgentEntity? speaker = state.World.Agents.FirstOrDefault(agent => agent.Id == request.AgentId);
            AgentEntity? listener = state.World.Agents.FirstOrDefault(agent => agent.Id == request.TargetId);
            if (speaker is null || listener is null || !SocialService.CanCommunicate(state, speaker, listener))
            {
                return new ActionResult(request.AgentId, request.ActionType, false, "Communication target is unavailable.");
            }

            SocialService.RecordCommunication(state, speaker.Id, listener.Id);

            return new ActionResult(request.AgentId, request.ActionType, true);
        }

        private static ActionResult ResolveExperiment(SimulationState state, ActionRequest request)
        {
            RecipeDefinition? recipe = RecipeRegistry.Find(request.TargetId);
            if (recipe is null)
            {
                return new ActionResult(request.AgentId, request.ActionType, false, "Experiment recipe is unavailable.");
            }

            bool succeeded = state.Random.NextDouble() <= Math.Clamp(recipe.DiscoveryChance, 0, 1);
            if (!succeeded)
            {
                state.EmitEvent(
                    "experiment.failed",
                    "Action Resolver",
                    $"{request.AgentId} failed to discover {recipe.DisplayName}.",
                    request.AgentId,
                    new Dictionary<string, string>
                    {
                        ["agentId"] = request.AgentId,
                        ["recipeId"] = recipe.Id,
                        ["displayName"] = recipe.DisplayName
                    });

                return new ActionResult(request.AgentId, request.ActionType, false, "Experiment did not produce a discovery.");
            }

            KnowledgeService.DiscoverRecipe(state, request.AgentId, recipe, "Experiment");

            return new ActionResult(request.AgentId, request.ActionType, true);
        }

        private ActionResult ResolveCraft(SimulationState state, ActionRequest request)
        {
            RecipeDefinition? recipe = RecipeRegistry.Find(request.TargetId);
            AgentEntity? agentEntity = state.World.Agents.FirstOrDefault(agent => agent.Id == request.AgentId);
            if (recipe is null || agentEntity is null)
            {
                return new ActionResult(request.AgentId, request.ActionType, false, "Craft action could not be resolved.");
            }

            WorldMutationResult mutation = _mutations.TryTransformResources(
                state.World,
                agentEntity.Inventory,
                recipe.Inputs.ToDictionary(input => input.ResourceId, input => input.Quantity, StringComparer.OrdinalIgnoreCase),
                recipe.Outputs.ToDictionary(output => output.ResourceId, output => output.Quantity, StringComparer.OrdinalIgnoreCase));

            if (!mutation.Succeeded)
            {
                return new ActionResult(request.AgentId, request.ActionType, false, mutation.Reason);
            }

            state.EmitEvent(
                "recipe.crafted",
                "Action Resolver",
                $"{request.AgentId} crafted {recipe.DisplayName}.",
                request.AgentId,
                new Dictionary<string, string>
                {
                    ["agentId"] = request.AgentId,
                    ["recipeId"] = recipe.Id,
                    ["displayName"] = recipe.DisplayName
                });

            return new ActionResult(request.AgentId, request.ActionType, true);
        }

        private ActionResult ResolvePickUpResource(SimulationState state, ActionRequest request)
        {
            if (request.TargetId is null)
            {
                return new ActionResult(request.AgentId, request.ActionType, false, "Pick up action requires a target.");
            }

            string resourceId = request.TargetType ?? ResourceDefinition.FoodId;

            PlantEntity? plant = state.World.Plants.FirstOrDefault(candidate => candidate.Id == request.TargetId);
            if (plant is not null)
            {
                WorldMutationResult harvest = _mutations.TryHarvestPlant(
                    state.World,
                    request.AgentId,
                    plant.Id,
                    quantity: 1);

                return harvest.Succeeded
                    ? new ActionResult(request.AgentId, request.ActionType, true)
                    : new ActionResult(request.AgentId, request.ActionType, false, harvest.Reason);
            }

            ResourceDepositEntity? deposit = state.World.ResourceDeposits.FirstOrDefault(candidate => candidate.Id == request.TargetId);
            if (deposit is not null)
            {
                WorldMutationResult harvest = _mutations.TryHarvestDeposit(
                    state.World,
                    request.AgentId,
                    deposit.Id,
                    quantity: 1);

                return harvest.Succeeded
                    ? new ActionResult(request.AgentId, request.ActionType, true)
                    : new ActionResult(request.AgentId, request.ActionType, false, harvest.Reason);
            }

            WorldMutationResult mutation = _mutations.TryPickUpResource(
                state.World,
                request.AgentId,
                request.TargetId,
                resourceId,
                quantity: 1);

            return mutation.Succeeded
                ? new ActionResult(request.AgentId, request.ActionType, true)
                : new ActionResult(request.AgentId, request.ActionType, false, mutation.Reason);
        }

        private ActionResult ResolveDropResource(SimulationState state, ActionRequest request)
        {
            string resourceId = request.TargetType ?? ResourceDefinition.FoodId;
            WorldMutationResult mutation = _mutations.TryDropResource(
                state.World,
                request.AgentId,
                resourceId,
                quantity: 1);

            return mutation.Succeeded
                ? new ActionResult(request.AgentId, request.ActionType, true)
                : new ActionResult(request.AgentId, request.ActionType, false, mutation.Reason);
        }

        private ActionResult ResolveConsumeResource(SimulationState state, ActionRequest request)
        {
            AgentEntity? agentEntity = state.World.Agents.FirstOrDefault(a => a.Id == request.AgentId);
            AgentState? agentState = state.GetAgentById(request.AgentId);
            string resourceId = request.TargetType ?? ResourceDefinition.FoodId;

            if (agentEntity is null || agentState is null)
            {
                return new ActionResult(request.AgentId, request.ActionType, false, "Consume action could not be resolved.");
            }

            WorldMutationResult mutation = _mutations.TryConsumeResource(
                state.World,
                agentEntity.Inventory,
                resourceId,
                quantity: 1);

            if (!mutation.Succeeded)
            {
                return new ActionResult(request.AgentId, request.ActionType, false, mutation.Reason);
            }

            if (state.World.ResourceDefinitionsById.TryGetValue(resourceId, out ResourceDefinition? definition))
            {
                ApplyNeedEffects(agentState.NeedState, definition.NeedEffects);
            }

            return new ActionResult(request.AgentId, request.ActionType, true);
        }

        private ActionResult ResolveDrink(SimulationState state, ActionRequest request)
        {
            AgentEntity? agentEntity = state.World.Agents.FirstOrDefault(a => a.Id == request.AgentId);
            AgentState? agentState = state.GetAgentById(request.AgentId);

            if (agentEntity is null || agentState is null)
            {
                return new ActionResult(request.AgentId, request.ActionType, false, "Drink action could not be resolved.");
            }

            if (agentEntity.Inventory.GetQuantity(ResourceDefinition.WaterId) > 0)
            {
                WorldMutationResult mutation = _mutations.TryConsumeResource(
                    state.World,
                    agentEntity.Inventory,
                    ResourceDefinition.WaterId,
                    quantity: 1);
                if (!mutation.Succeeded)
                {
                    return new ActionResult(request.AgentId, request.ActionType, false, mutation.Reason);
                }

                ApplyNeedEffects(
                    agentState.NeedState,
                    state.World.ResourceDefinitionsById[ResourceDefinition.WaterId].NeedEffects);

                return new ActionResult(request.AgentId, request.ActionType, true);
            }

            if (request.TargetId is null)
            {
                NeedSystem.ApplyDrink(agentState.NeedState);
                return new ActionResult(request.AgentId, request.ActionType, true);
            }

            WaterSourceEntity? waterSource = SpatialQueries.FindAvailableWaterSourceAtInteractionPoint(
                state.World,
                agentEntity.Position,
                request.TargetId);
            if (waterSource is null)
            {
                return new ActionResult(request.AgentId, request.ActionType, false, "Water source is unavailable.");
            }

            WorldMutationResult draw = _mutations.TryDrawWater(
                state.World,
                agentEntity.Id,
                waterSource.Id,
                amount: 1,
                addToInventory: false);
            if (!draw.Succeeded)
            {
                return new ActionResult(request.AgentId, request.ActionType, false, draw.Reason);
            }

            ApplyNeedEffects(
                agentState.NeedState,
                state.World.ResourceDefinitionsById[ResourceDefinition.WaterId].NeedEffects);

            return new ActionResult(request.AgentId, request.ActionType, true);
        }

        private static ActionResult ResolveRest(SimulationState state, ActionRequest request)
        {
            AgentState? agentState = state.GetAgentById(request.AgentId);

            if (agentState is null)
            {
                return new ActionResult(request.AgentId, request.ActionType, false, "Rest action could not be resolved.");
            }

            NeedSystem.ApplyRest(agentState.NeedState);

            return new ActionResult(request.AgentId, request.ActionType, true);
        }

        private static ActionResult ResolveWander(SimulationState state, ActionRequest request)
        {
            return new ActionResult(
                request.AgentId,
                request.ActionType,
                false,
                "Wander requires route planning before execution.");
        }

        private static void ApplyNeedEffects(AgentNeedState needState, ResourceNeedEffects effects)
        {
            needState.Hunger = NeedSystem.ClampNeed(needState.Hunger + effects.HungerDelta);
            needState.Thirst = NeedSystem.ClampNeed(needState.Thirst + effects.ThirstDelta);
            needState.Energy = NeedSystem.ClampEnergy(needState.Energy + effects.EnergyDelta);
            needState.Fatigue = NeedSystem.ClampNeed(needState.Fatigue + effects.FatigueDelta);
        }

        private static WorldPosition? FindFreeAdjacentPosition(SimulationState state, AgentEntity parent)
        {
            foreach (var cell in state.World.Grid.GetAdjacentCells(parent.Position.ToGridCell()))
            {
                if (state.World.Grid.IsTraversable(cell)
                    && !SpatialQueries.IsCellOccupied(state.World, cell))
                {
                    return new WorldPosition(cell.X + 0.5f, cell.Y + 0.5f);
                }
            }

            return null;
        }

        private static string NextAgentId(SimulationState state, string speciesId)
        {
            int index = state.World.Agents
                .Where(agent => string.Equals(SpeciesRegistry.NormalizeId(agent.SpeciesId), speciesId, StringComparison.OrdinalIgnoreCase))
                .Select(agent => agent.Id)
                .Select(id => id.StartsWith($"{speciesId}-", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(id[(speciesId.Length + 1)..], out int parsed)
                        ? parsed
                        : 0)
                .DefaultIfEmpty(0)
                .Max() + 1;

            return $"{speciesId}-{index}";
        }
    }
}
