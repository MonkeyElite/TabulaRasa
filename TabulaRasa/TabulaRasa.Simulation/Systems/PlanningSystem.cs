using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Agents.Models;
using TabulaRasa.Simulation.Evolution;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.Knowledge;
using TabulaRasa.Simulation.Memory;
using TabulaRasa.Simulation.Social;
using TabulaRasa.Simulation.Species;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Entities;
using TabulaRasa.World.State;

namespace TabulaRasa.Simulation.Systems
{
    public class PlanningSystem : ISystem
    {
        public string Name => "Planning System";
        public SimulationPhase Phase => SimulationPhase.Evaluation;
        public int Priority => 0;

        public void Execute(SimulationState state)
        {
            WorldState world = state.World;
            state.LatestPerceptionsByAgentId.Clear();

            foreach (AgentEntity agentEntity in world.Agents)
            {
                AgentState? agentState = state.GetAgentById(agentEntity.Id);

                if (agentState is null || agentEntity.IsDead)
                {
                    continue;
                }

                AgentPerception perception = AgentPerceptionBuilder.Build(
                    state,
                    agentEntity,
                    state.Config.PerceptionRadius);
                AgentPerception enrichedPerception = AgentMemoryService.RememberAndEnrichPerception(
                    state,
                    agentEntity,
                    perception);
                enrichedPerception = EnrichWithKnowledgeOpportunities(state, agentEntity, enrichedPerception);
                SocialService.RememberPerceivedAgents(state, agentEntity, perception);
                state.LatestPerceptionsByAgentId[agentEntity.Id] = enrichedPerception;
                AgentPersonality personality = AgentPersonalityService.Derive(
                    agentEntity.Traits,
                    state.Config.EffectiveBelievability.EffectiveBehavior.ExplorationChance,
                    state.Config.EffectiveBelievability.EffectiveBehavior.PersonalityInfluence);
                AgentSnapshot snapshot = new(
                    agentEntity.Id,
                    agentState.NeedState.ToSnapshot(),
                    agentEntity.Position,
                    agentEntity.Inventory.Stacks
                        .GroupBy(stack => stack.ResourceId, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(group => group.Key, group => group.Sum(stack => stack.Quantity), StringComparer.OrdinalIgnoreCase),
                    SpeciesRegistry.NormalizeId(agentEntity.SpeciesId),
                    agentEntity.AgeTicks,
                    agentEntity.AgeTicks >= SpeciesRegistry.Get(agentEntity.SpeciesId, state.Config.EffectiveSpeciesRules).AdultAgeDays,
                    agentEntity.LastReproducedTick,
                    agentEntity.Traits);

                state.PendingIntents.Add(agentState.Mind.Decide(
                    enrichedPerception,
                    snapshot,
                    agentState.Learning,
                    state.Random,
                    BuildBehaviorContext(state, agentEntity.Id, personality)));
            }
        }

        private static AgentBehaviorContext BuildBehaviorContext(
            SimulationState state,
            string agentId,
            AgentPersonality personality)
        {
            var behavior = state.Config.EffectiveBelievability.EffectiveBehavior;
            IReadOnlyDictionary<string, float> biases = personality.BehaviorBiases;
            state.FailedTargetCooldownsByAgentId.TryGetValue(agentId, out Dictionary<string, long>? cooldowns);

            return new AgentBehaviorContext(
                behavior.Eat * biases.GetValueOrDefault("eat", 1),
                behavior.Drink * biases.GetValueOrDefault("drink", 1),
                behavior.Rest * biases.GetValueOrDefault("rest", 1),
                behavior.Wander * biases.GetValueOrDefault("wander", 1),
                behavior.Social * biases.GetValueOrDefault("social", 1),
                behavior.Reproduce * biases.GetValueOrDefault("reproduce", 1),
                behavior.Flee * biases.GetValueOrDefault("flee", 1),
                behavior.Attack * biases.GetValueOrDefault("attack", 1),
                behavior.Craft * biases.GetValueOrDefault("craft", 1),
                behavior.Experiment * biases.GetValueOrDefault("experiment", 1),
                personality.ExplorationChance,
                behavior.PersonalityInfluence,
                state.ActiveTick,
                cooldowns ?? new Dictionary<string, long>());
        }

        private static AgentPerception EnrichWithKnowledgeOpportunities(
            SimulationState state,
            AgentEntity agent,
            AgentPerception perception)
        {
            Dictionary<string, int> availableResources = agent.Inventory.Stacks
                .GroupBy(stack => stack.ResourceId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Sum(stack => stack.Quantity), StringComparer.OrdinalIgnoreCase);

            foreach (PerceivedEntity entity in perception.NearbyEntities)
            {
                if (entity.EntityType == PerceivedEntityType.ResourceDeposit)
                {
                    ResourceDepositEntity? deposit = state.World.ResourceDeposits.FirstOrDefault(candidate => candidate.Id == entity.EntityId);
                    if (deposit is not null && !deposit.IsEmpty)
                    {
                        availableResources[deposit.ResourceId] = availableResources.GetValueOrDefault(deposit.ResourceId) + 1;
                    }
                }

                if (entity.EntityType is PerceivedEntityType.Plant or PerceivedEntityType.Food)
                {
                    PlantEntity? plant = state.World.Plants.FirstOrDefault(candidate => candidate.Id == entity.EntityId);
                    if (plant is not null && plant.IsHarvestable)
                    {
                        availableResources[plant.ResourceId] = availableResources.GetValueOrDefault(plant.ResourceId) + 1;
                    }

                    ResourceContainerEntity? container = state.World.ResourceContainers.FirstOrDefault(candidate => candidate.Id == entity.EntityId);
                    if (container is not null && !container.IsEmpty)
                    {
                        foreach (var stack in container.Inventory.Stacks)
                        {
                            availableResources[stack.ResourceId] = availableResources.GetValueOrDefault(stack.ResourceId) + stack.Quantity;
                        }
                    }
                }
            }

            AgentKnowledgeStore knowledge = state.GetKnowledgeStore(agent.Id);
            List<InteractionOpportunity> opportunities = perception.Opportunities.ToList();
            foreach (RecipeDefinition recipe in RecipeRegistry.FindCraftableRecipes(availableResources, knowledge))
            {
                opportunities.Add(new InteractionOpportunity(
                    AgentActionType.Craft,
                    recipe.Id,
                    agent.Position,
                    recipe.Id,
                    PerceptionChannel.Internal,
                    Relevance: 0.9f));
            }

            foreach (RecipeDefinition recipe in RecipeRegistry.FindExperimentCandidates(availableResources, knowledge))
            {
                opportunities.Add(new InteractionOpportunity(
                    AgentActionType.Experiment,
                    recipe.Id,
                    agent.Position,
                    recipe.Id,
                    PerceptionChannel.Internal,
                    Relevance: 0.55f));
            }

            return new AgentPerception(perception.NearbyEntities, opportunities);
        }
    }
}
