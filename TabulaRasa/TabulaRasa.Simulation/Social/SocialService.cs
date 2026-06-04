using System.Globalization;
using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Agents.Actions;
using TabulaRasa.Agents.Models;
using TabulaRasa.Simulation.Memory;
using TabulaRasa.Simulation.Species;
using TabulaRasa.Simulation.State;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Queries;

namespace TabulaRasa.Simulation.Social
{
    public static class SocialService
    {
        public const string SourceSystem = "Social System";

        public static void EnsureDefaultGroups(SimulationState state)
        {
            foreach (AgentEntity agent in state.World.Agents.Where(agent => !agent.IsDead))
            {
                string speciesId = SpeciesRegistry.NormalizeId(agent.SpeciesId);
                string groupId = $"species:{speciesId}";
                AgentSocialStore store = state.GetSocialStore(agent.Id);

                if (!store.TryAddGroup(new SocialGroupMembership
                {
                    GroupId = groupId,
                    DisplayName = $"{SpeciesRegistry.Get(speciesId).DisplayName} species",
                    Kind = "Species",
                    JoinedTick = state.ActiveTick
                }))
                {
                    continue;
                }

                state.EmitEvent(
                    "group.joined",
                    SourceSystem,
                    $"{agent.Id} joined {groupId}.",
                    agent.Id,
                    new Dictionary<string, string>
                    {
                        ["agentId"] = agent.Id,
                        ["groupId"] = groupId,
                        ["kind"] = "Species"
                    });
            }
        }

        public static void RememberPerceivedAgents(
            SimulationState state,
            AgentEntity observer,
            AgentPerception perception)
        {
            foreach (PerceivedEntity entity in perception.NearbyEntities
                .Where(entity => entity.EntityType is PerceivedEntityType.Agent or PerceivedEntityType.Prey or PerceivedEntityType.Predator))
            {
                SocialRelationship relationship = state
                    .GetSocialStore(observer.Id)
                    .GetOrCreateRelationship(observer.Id, entity.EntityId, state.ActiveTick);
                relationship.LastSeenTick = state.ActiveTick;
                ApplyRelationshipChange(
                    state,
                    relationship,
                    familiarity: 0.08f * Math.Clamp(entity.Relevance, 0.25f, 1),
                    fear: entity.EntityType == PerceivedEntityType.Predator ? 0.04f : 0,
                    reason: "perception");
            }
        }

        public static void RecordCommunication(SimulationState state, string speakerId, string listenerId)
        {
            SocialRelationship speakerToListener = RecordMutualInteraction(
                state,
                speakerId,
                listenerId,
                familiarity: 0.12f,
                trust: 0.06f,
                fear: -0.02f,
                affinity: 0.05f,
                reason: "communication");
            RecordMutualInteraction(
                state,
                listenerId,
                speakerId,
                familiarity: 0.10f,
                trust: 0.05f,
                fear: -0.02f,
                affinity: 0.04f,
                reason: "communication");

            state.EmitEvent(
                "communication.sent",
                SourceSystem,
                $"{speakerId} communicated with {listenerId}.",
                speakerId,
                new Dictionary<string, string>
                {
                    ["speakerId"] = speakerId,
                    ["listenerId"] = listenerId,
                    ["interactionCount"] = speakerToListener.InteractionCount.ToString(CultureInfo.InvariantCulture)
                });

            bool memoryTransferred = TryTransferMemory(state, speakerId, listenerId);
            bool learningTransferred = TryTransferLearningHint(state, speakerId, listenerId);
            if (!memoryTransferred && !learningTransferred)
            {
                state.EmitEvent(
                    "teaching.hook.empty",
                    SourceSystem,
                    $"{speakerId} had no transferable knowledge for {listenerId}.",
                    speakerId,
                    new Dictionary<string, string>
                    {
                        ["teacherId"] = speakerId,
                        ["learnerId"] = listenerId
                    });
            }
        }

        public static void RecordAttack(SimulationState state, string attackerId, string targetId)
        {
            RecordMutualInteraction(
                state,
                targetId,
                attackerId,
                familiarity: 0.10f,
                trust: -0.20f,
                fear: 0.35f,
                affinity: -0.20f,
                reason: "attack");
            RecordMutualInteraction(
                state,
                attackerId,
                targetId,
                familiarity: 0.08f,
                trust: -0.05f,
                fear: 0,
                affinity: -0.05f,
                reason: "attack");
        }

        public static void RecordReproduction(SimulationState state, string firstAgentId, string secondAgentId)
        {
            RecordMutualInteraction(
                state,
                firstAgentId,
                secondAgentId,
                familiarity: 0.20f,
                trust: 0.10f,
                fear: -0.05f,
                affinity: 0.25f,
                reason: "reproduction");
            RecordMutualInteraction(
                state,
                secondAgentId,
                firstAgentId,
                familiarity: 0.20f,
                trust: 0.10f,
                fear: -0.05f,
                affinity: 0.25f,
                reason: "reproduction");
        }

        public static IReadOnlyList<string> SharedGroups(SimulationState state, string firstAgentId, string secondAgentId)
        {
            HashSet<string> firstGroups = state.GetSocialStore(firstAgentId).Groups
                .Select(group => group.GroupId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return state.GetSocialStore(secondAgentId).Groups
                .Where(group => firstGroups.Contains(group.GroupId))
                .Select(group => group.GroupId)
                .OrderBy(groupId => groupId, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static SocialRelationship RecordMutualInteraction(
            SimulationState state,
            string agentId,
            string otherAgentId,
            float familiarity,
            float trust,
            float fear,
            float affinity,
            string reason)
        {
            SocialRelationship relationship = state
                .GetSocialStore(agentId)
                .GetOrCreateRelationship(agentId, otherAgentId, state.ActiveTick);
            relationship.InteractionCount++;
            relationship.LastInteractionTick = state.ActiveTick;
            relationship.LastSeenTick = state.ActiveTick;
            ApplyRelationshipChange(state, relationship, familiarity, trust, fear, affinity, reason);

            return relationship;
        }

        private static void ApplyRelationshipChange(
            SimulationState state,
            SocialRelationship relationship,
            float familiarity = 0,
            float trust = 0,
            float fear = 0,
            float affinity = 0,
            string reason = "updated")
        {
            float beforeFamiliarity = relationship.Familiarity;
            float beforeTrust = relationship.Trust;
            float beforeFear = relationship.Fear;
            float beforeAffinity = relationship.Affinity;

            relationship.ApplyDeltas(familiarity, trust, fear, affinity, state.ActiveTick);

            if (Math.Abs(relationship.Familiarity - beforeFamiliarity) < 0.0001f
                && Math.Abs(relationship.Trust - beforeTrust) < 0.0001f
                && Math.Abs(relationship.Fear - beforeFear) < 0.0001f
                && Math.Abs(relationship.Affinity - beforeAffinity) < 0.0001f)
            {
                return;
            }

            state.EmitEvent(
                "relationship.changed",
                SourceSystem,
                $"{relationship.AgentId} updated relationship with {relationship.OtherAgentId}.",
                relationship.AgentId,
                new Dictionary<string, string>
                {
                    ["agentId"] = relationship.AgentId,
                    ["otherAgentId"] = relationship.OtherAgentId,
                    ["reason"] = reason,
                    ["familiarity"] = Format(relationship.Familiarity),
                    ["trust"] = Format(relationship.Trust),
                    ["fear"] = Format(relationship.Fear),
                    ["affinity"] = Format(relationship.Affinity)
                });
        }

        private static bool TryTransferMemory(SimulationState state, string speakerId, string listenerId)
        {
            if (!state.MemoryStoresByAgentId.TryGetValue(speakerId, out AgentMemoryStore? speakerStore))
            {
                return false;
            }

            AgentMemoryRecord? sourceMemory = speakerStore.Memories
                .Where(memory => memory.Kind is AgentMemoryKind.Entity or AgentMemoryKind.Location)
                .Where(memory => memory.SubjectId != listenerId)
                .OrderByDescending(memory => memory.Strength * memory.Certainty)
                .ThenByDescending(memory => memory.LastUpdatedTick)
                .FirstOrDefault();

            if (sourceMemory is null)
            {
                return false;
            }

            AgentMemoryStore listenerStore = state.GetMemoryStore(listenerId);
            string sharedMemoryId = $"shared:{speakerId}:{sourceMemory.Id}";
            AgentMemoryRecord? existing = listenerStore.FindById(sharedMemoryId);
            if (existing is not null && existing.Strength >= sourceMemory.Strength * 0.75f)
            {
                return false;
            }

            AgentMemoryRecord transferred = existing ?? new AgentMemoryRecord
            {
                Id = sharedMemoryId,
                Kind = sourceMemory.Kind,
                SubjectId = sourceMemory.SubjectId,
                SubjectType = sourceMemory.SubjectType,
                Position = sourceMemory.Position,
                CreatedTick = state.ActiveTick,
                LastUpdatedTick = state.ActiveTick,
                Strength = 0,
                Certainty = 0,
                Summary = ""
            };

            transferred.LastUpdatedTick = state.ActiveTick;
            transferred.Strength = Math.Clamp(sourceMemory.Strength * 0.75f, 0, 1);
            transferred.Certainty = Math.Clamp(sourceMemory.Certainty * 0.65f, 0, 1);
            transferred.ExpiresAtTick = state.ActiveTick + state.Config.EffectiveMemory.RetentionTicks;
            transferred.Summary = $"Learned from {speakerId}: {sourceMemory.Summary}";
            transferred.Metadata["sourceAgentId"] = speakerId;
            transferred.Metadata["sourceMemoryId"] = sourceMemory.Id;
            transferred.Metadata["transferType"] = "communication";

            if (existing is null)
            {
                listenerStore.Add(transferred);
            }

            listenerStore.TrimTo(state.Config.EffectiveMemory.MaxMemoriesPerAgent);
            state.EmitEvent(
                "knowledge.transferred",
                SourceSystem,
                $"{speakerId} shared {sourceMemory.SubjectId} with {listenerId}.",
                listenerId,
                new Dictionary<string, string>
                {
                    ["teacherId"] = speakerId,
                    ["learnerId"] = listenerId,
                    ["kind"] = "Memory",
                    ["subjectId"] = sourceMemory.SubjectId,
                    ["sourceMemoryId"] = sourceMemory.Id,
                    ["memoryId"] = sharedMemoryId
                });

            return true;
        }

        private static bool TryTransferLearningHint(SimulationState state, string speakerId, string listenerId)
        {
            AgentState? speaker = state.GetAgentById(speakerId);
            AgentState? listener = state.GetAgentById(listenerId);
            AgentLearningEntry? sourceEntry = speaker?.Learning.Entries
                .Where(entry => entry.Attempts > 0)
                .OrderByDescending(entry => Math.Abs(entry.LearnedWeight))
                .ThenByDescending(entry => entry.Attempts)
                .FirstOrDefault();

            if (listener is null || sourceEntry is null)
            {
                return false;
            }

            AgentLearningEntry? existing = listener.Learning.Entries.FirstOrDefault(entry =>
                entry.ContextKey == sourceEntry.ContextKey && entry.ActionType == sourceEntry.ActionType);
            if (existing is not null && Math.Abs(existing.LearnedWeight) >= Math.Abs(sourceEntry.LearnedWeight))
            {
                return false;
            }

            AgentLearningEntry targetEntry = listener.Learning.GetOrCreate(sourceEntry.ContextKey, sourceEntry.ActionType);
            targetEntry.ApplyOutcome(
                sourceEntry.AverageOutcomeScore,
                sourceEntry.AverageOutcomeScore >= 0,
                learningRate: 0.15f);

            state.EmitEvent(
                "teaching.hook.transferred",
                SourceSystem,
                $"{speakerId} taught {listenerId} about {sourceEntry.ActionType}.",
                listenerId,
                new Dictionary<string, string>
                {
                    ["teacherId"] = speakerId,
                    ["learnerId"] = listenerId,
                    ["contextKey"] = sourceEntry.ContextKey,
                    ["actionType"] = sourceEntry.ActionType.ToString(),
                    ["kind"] = "LearningHint"
                });

            return true;
        }

        public static bool CanCommunicate(SimulationState state, AgentEntity speaker, AgentEntity listener)
        {
            if (speaker.Id == listener.Id || speaker.IsDead || listener.IsDead)
            {
                return false;
            }

            if (!string.Equals(
                SpeciesRegistry.NormalizeId(speaker.SpeciesId),
                SpeciesRegistry.NormalizeId(listener.SpeciesId),
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return speaker.Position.DistanceTo(listener.Position) <= SpatialQueries.DefaultInteractionTolerance + 0.5f;
        }

        private static string Format(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
