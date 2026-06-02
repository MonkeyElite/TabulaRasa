using System.Globalization;
using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Abstractions.World;
using TabulaRasa.Simulation.Configuration;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Entities;

namespace TabulaRasa.Simulation.Memory
{
    public static class AgentMemoryService
    {
        private const string SourceSystem = "Agent Memory System";

        public static void Decay(SimulationState state)
        {
            MemoryConfig config = state.Config.EffectiveMemory;
            if (!config.Enabled)
            {
                return;
            }

            foreach ((string agentId, AgentMemoryStore store) in state.MemoryStoresByAgentId.ToList())
            {
                foreach (AgentMemoryRecord memory in store.Memories.ToList())
                {
                    memory.Strength = Math.Max(0, memory.Strength - config.DecayPerTick);
                    memory.Certainty = Math.Max(0, memory.Certainty - config.DecayPerTick * 0.5f);

                    if (!memory.IsExpired(state.ActiveTick, config.MinimumStrength))
                    {
                        continue;
                    }

                    store.Remove(memory);
                    EmitMemoryEvent(state, "memory.expired", agentId, memory, "forgot");
                }
            }
        }

        public static AgentPerception RememberAndEnrichPerception(
            SimulationState state,
            AgentEntity agent,
            AgentPerception perception)
        {
            MemoryConfig config = state.Config.EffectiveMemory;
            if (!config.Enabled)
            {
                return perception;
            }

            AgentMemoryStore store = state.GetMemoryStore(agent.Id);
            foreach (PerceivedEntity entity in perception.NearbyEntities)
            {
                RememberEntity(state, agent.Id, store, entity, config);
                if (entity.EntityType == PerceivedEntityType.Food)
                {
                    RememberLocation(state, agent.Id, store, entity, config);
                }
            }

            store.TrimTo(config.MaxMemoriesPerAgent);

            if (perception.HasOpportunity(AgentActionType.Eat))
            {
                return perception;
            }

            AgentMemoryRecord? rememberedFood = RecallFood(store, config);
            if (rememberedFood is null)
            {
                return perception;
            }

            float distance = agent.Position.DistanceTo(rememberedFood.Position);
            List<PerceivedEntity> nearbyEntities = perception.NearbyEntities.ToList();
            List<InteractionOpportunity> opportunities = perception.Opportunities.ToList();

            if (!nearbyEntities.Any(entity => entity.EntityId == rememberedFood.SubjectId))
            {
                nearbyEntities.Add(new PerceivedEntity(
                    rememberedFood.SubjectId,
                    PerceivedEntityType.Food,
                    rememberedFood.Position,
                    IsInteractable: true,
                    Channel: PerceptionChannel.Memory,
                    Distance: distance,
                    Certainty: rememberedFood.Certainty,
                    Relevance: rememberedFood.Strength));
            }

            opportunities.Add(new InteractionOpportunity(
                AgentActionType.Eat,
                rememberedFood.SubjectId,
                rememberedFood.Position,
                rememberedFood.SubjectId,
                PerceptionChannel.Memory,
                rememberedFood.Strength));

            return new AgentPerception(nearbyEntities, opportunities);
        }

        public static void RememberActionOutcome(SimulationState state, ActionResult result)
        {
            MemoryConfig config = state.Config.EffectiveMemory;
            if (!config.Enabled)
            {
                return;
            }

            AgentEntity? agent = state.World.Agents.FirstOrDefault(candidate => candidate.Id == result.AgentId);
            AgentMemoryStore store = state.GetMemoryStore(result.AgentId);
            string resultIndex = state.ActionResults.Count.ToString(CultureInfo.InvariantCulture);
            AgentMemoryRecord memory = new()
            {
                Id = $"action-outcome:{state.ActiveTick}:{resultIndex}:{result.ActionType}",
                Kind = AgentMemoryKind.ActionOutcome,
                SubjectId = result.TargetKey(),
                SubjectType = result.ActionType.ToString(),
                Position = agent?.Position ?? new WorldPosition(0, 0),
                CreatedTick = state.ActiveTick,
                LastUpdatedTick = state.ActiveTick,
                Strength = result.Succeeded ? 0.75f : 0.9f,
                Certainty = 1,
                ExpiresAtTick = state.ActiveTick + config.RetentionTicks,
                Summary = result.Succeeded
                    ? $"{result.ActionType} succeeded"
                    : $"{result.ActionType} failed: {result.Reason ?? "unknown"}"
            };
            memory.Metadata["actionType"] = result.ActionType.ToString();
            memory.Metadata["succeeded"] = result.Succeeded.ToString();
            memory.Metadata["reason"] = result.Reason ?? "";

            store.Add(memory);
            store.TrimTo(config.MaxMemoriesPerAgent);
            EmitMemoryEvent(state, "memory.created", result.AgentId, memory, "remembered");
        }

        public static void MarkTargetUnavailable(
            SimulationState state,
            string agentId,
            string? targetId,
            string reason)
        {
            if (targetId is null || !state.Config.EffectiveMemory.Enabled)
            {
                return;
            }

            if (!state.MemoryStoresByAgentId.TryGetValue(agentId, out AgentMemoryStore? store))
            {
                return;
            }

            foreach (AgentMemoryRecord memory in store.Memories
                .Where(memory => memory.SubjectId == targetId && (memory.Kind == AgentMemoryKind.Entity || memory.Kind == AgentMemoryKind.Location))
                .ToList())
            {
                memory.Strength = 0;
                memory.Certainty = 0;
                memory.ExpiresAtTick = state.ActiveTick;
                memory.Metadata["staleReason"] = reason;
                store.Remove(memory);
                EmitMemoryEvent(state, "memory.stale", agentId, memory, reason);
            }
        }

        private static void RememberEntity(
            SimulationState state,
            string agentId,
            AgentMemoryStore store,
            PerceivedEntity entity,
            MemoryConfig config)
        {
            UpsertMemory(
                state,
                agentId,
                store,
                $"entity:{entity.EntityType}:{entity.EntityId}",
                AgentMemoryKind.Entity,
                entity.EntityId,
                entity.EntityType.ToString(),
                entity.Position,
                entity.Certainty,
                1,
                $"Saw {entity.EntityType} {entity.EntityId}.",
                config,
                new Dictionary<string, string>
                {
                    ["channel"] = entity.Channel.ToString(),
                    ["distance"] = entity.Distance.ToString("0.###", CultureInfo.InvariantCulture),
                    ["relevance"] = entity.Relevance.ToString("0.###", CultureInfo.InvariantCulture)
                });
        }

        private static void RememberLocation(
            SimulationState state,
            string agentId,
            AgentMemoryStore store,
            PerceivedEntity entity,
            MemoryConfig config)
        {
            UpsertMemory(
                state,
                agentId,
                store,
                $"location:{entity.EntityType}:{entity.EntityId}",
                AgentMemoryKind.Location,
                entity.EntityId,
                entity.EntityType.ToString(),
                entity.Position,
                entity.Certainty,
                1,
                $"Remembered {entity.EntityType} location for {entity.EntityId}.",
                config,
                new Dictionary<string, string>
                {
                    ["resourceType"] = entity.EntityType.ToString()
                });
        }

        private static void UpsertMemory(
            SimulationState state,
            string agentId,
            AgentMemoryStore store,
            string id,
            AgentMemoryKind kind,
            string subjectId,
            string subjectType,
            WorldPosition position,
            float certainty,
            float strength,
            string summary,
            MemoryConfig config,
            IReadOnlyDictionary<string, string> metadata)
        {
            AgentMemoryRecord? existing = store.FindById(id);
            if (existing is null)
            {
                AgentMemoryRecord memory = new()
                {
                    Id = id,
                    Kind = kind,
                    SubjectId = subjectId,
                    SubjectType = subjectType,
                    Position = position,
                    CreatedTick = state.ActiveTick,
                    LastUpdatedTick = state.ActiveTick,
                    Strength = strength,
                    Certainty = certainty,
                    ExpiresAtTick = state.ActiveTick + config.RetentionTicks,
                    Summary = summary
                };

                foreach ((string key, string value) in metadata)
                {
                    memory.Metadata[key] = value;
                }

                store.Add(memory);
                EmitMemoryEvent(state, "memory.created", agentId, memory, "remembered");
                return;
            }

            existing.Position = position;
            existing.LastUpdatedTick = state.ActiveTick;
            existing.Strength = Math.Max(existing.Strength, strength);
            existing.Certainty = Math.Max(existing.Certainty, certainty);
            existing.ExpiresAtTick = state.ActiveTick + config.RetentionTicks;
            existing.Summary = summary;

            foreach ((string key, string value) in metadata)
            {
                existing.Metadata[key] = value;
            }

            EmitMemoryEvent(state, "memory.updated", agentId, existing, "refreshed");
        }

        private static AgentMemoryRecord? RecallFood(AgentMemoryStore store, MemoryConfig config)
        {
            return store.Memories
                .Where(memory => memory.SubjectType == PerceivedEntityType.Food.ToString())
                .Where(memory => memory.Kind == AgentMemoryKind.Entity || memory.Kind == AgentMemoryKind.Location)
                .Where(memory => memory.Strength >= config.RecallThreshold && memory.Certainty >= config.RecallThreshold)
                .OrderByDescending(memory => memory.Strength * memory.Certainty)
                .ThenByDescending(memory => memory.LastUpdatedTick)
                .FirstOrDefault();
        }

        private static void EmitMemoryEvent(
            SimulationState state,
            string type,
            string agentId,
            AgentMemoryRecord memory,
            string outcome)
        {
            state.EmitEvent(
                type,
                SourceSystem,
                $"{agentId} {outcome} {memory.Kind} memory {memory.SubjectId}.",
                agentId,
                new Dictionary<string, string>
                {
                    ["memoryId"] = memory.Id,
                    ["memoryKind"] = memory.Kind.ToString(),
                    ["subjectId"] = memory.SubjectId,
                    ["subjectType"] = memory.SubjectType,
                    ["strength"] = memory.Strength.ToString("0.###", CultureInfo.InvariantCulture),
                    ["certainty"] = memory.Certainty.ToString("0.###", CultureInfo.InvariantCulture)
                });
        }

        private static string TargetKey(this ActionResult result)
        {
            return $"{result.ActionType}:{result.Succeeded}:{result.Reason ?? ""}";
        }
    }
}
