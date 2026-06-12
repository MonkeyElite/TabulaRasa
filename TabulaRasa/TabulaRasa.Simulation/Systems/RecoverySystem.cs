using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.State;
using TabulaRasa.Simulation.Tasks.Jobs;
using TaskStatus = TabulaRasa.Simulation.Tasks.Definitions.TaskStatus;

namespace TabulaRasa.Simulation.Systems
{
    public sealed class RecoverySystem : ISystem
    {
        public string Name => "Recovery System";
        public SimulationPhase Phase => SimulationPhase.PostUpdate;
        public int Priority => 50;

        public void Execute(SimulationState state)
        {
            ExpireCooldowns(state);
            TrackActionFailures(state);
            RecoverIdleAgents(state);
            FailStaleGoals(state);
            ReleaseDanglingReservations(state);
        }

        private void ExpireCooldowns(SimulationState state)
        {
            foreach (string agentId in state.FailedTargetCooldownsByAgentId.Keys.ToList())
            {
                Dictionary<string, long> cooldowns = state.FailedTargetCooldownsByAgentId[agentId];
                foreach (string targetId in cooldowns.Keys.Where(targetId => cooldowns[targetId] <= state.ActiveTick).ToList())
                {
                    cooldowns.Remove(targetId);
                }

                if (cooldowns.Count == 0)
                {
                    state.FailedTargetCooldownsByAgentId.Remove(agentId);
                }
            }
        }

        private void TrackActionFailures(SimulationState state)
        {
            int maxFailures = state.Config.EffectiveBelievability.EffectiveRecovery.MaxRepeatedActionFailures;
            int cooldownTicks = state.Config.EffectiveBelievability.EffectiveRecovery.FailedTargetCooldownTicks;

            foreach (var result in state.ActionResults
                .Where(result => result.TargetId is not null)
                .TakeLast(Math.Max(25, state.World.Agents.Count * 4)))
            {
                string key = $"{result.AgentId}:{result.ActionType}:{result.TargetId}";
                if (result.Succeeded)
                {
                    state.RepeatedActionFailuresByKey.Remove(key);
                    continue;
                }

                int failures = state.RepeatedActionFailuresByKey.GetValueOrDefault(key) + 1;
                state.RepeatedActionFailuresByKey[key] = failures;
                if (failures < maxFailures)
                {
                    continue;
                }

                Dictionary<string, long> cooldowns = state.FailedTargetCooldownsByAgentId.GetValueOrDefault(result.AgentId) ?? [];
                cooldowns[result.TargetId!] = state.ActiveTick + cooldownTicks;
                state.FailedTargetCooldownsByAgentId[result.AgentId] = cooldowns;
                state.RepeatedActionFailuresByKey.Remove(key);
                state.EmitEvent(
                    "recovery.target_cooled_down",
                    Name,
                    $"{result.AgentId} cooled down target {result.TargetId}.",
                    result.AgentId,
                    new Dictionary<string, string>
                    {
                        ["targetId"] = result.TargetId!,
                        ["actionType"] = result.ActionType.ToString(),
                        ["expiresAtTick"] = (state.ActiveTick + cooldownTicks).ToString(System.Globalization.CultureInfo.InvariantCulture)
                    },
                    severity: "warning",
                    importance: 0.62f,
                    tags: ["recovery", "anti-stuck"]);
            }
        }

        private void RecoverIdleAgents(SimulationState state)
        {
            long idleTicks = state.Config.EffectiveBelievability.EffectiveRecovery.IdleRecoveryTicks;
            foreach (var agent in state.World.Agents.Where(agent => !agent.IsDead))
            {
                bool busy = state.ActiveMovements.Any(movement => movement.AgentId == agent.Id)
                    || state.PendingIntents.Any(intent => intent.AgentId == agent.Id)
                    || state.PendingActionRequests.Any(request => request.AgentId == agent.Id)
                    || state.Goals.Any(goal => goal.AgentId == agent.Id && goal.IsActive);

                if (busy)
                {
                    state.AgentIdleSinceTickByAgentId.Remove(agent.Id);
                    continue;
                }

                long idleSince = state.AgentIdleSinceTickByAgentId.GetValueOrDefault(agent.Id, state.ActiveTick);
                state.AgentIdleSinceTickByAgentId[agent.Id] = idleSince;
                if (state.ActiveTick - idleSince < idleTicks)
                {
                    continue;
                }

                state.AgentIdleSinceTickByAgentId[agent.Id] = state.ActiveTick;
                state.EmitEvent(
                    "recovery.agent_idle",
                    Name,
                    $"{agent.Id} has been idle for {idleTicks} ticks.",
                    agent.Id,
                    severity: "info",
                    importance: 0.35f,
                    tags: ["recovery"]);
            }
        }

        private void FailStaleGoals(SimulationState state)
        {
            long maxAge = state.Config.EffectiveBelievability.EffectiveRecovery.MaxGoalAgeTicks;
            foreach (var goal in state.Goals.Where(goal => goal.IsActive && state.ActiveTick - goal.CreatedTick >= maxAge).ToList())
            {
                goal.Fail("Goal exceeded recovery age limit.", state.ActiveTick);
                JobInstance? job = state.ActiveJobs.Concat(state.PendingJobs)
                    .FirstOrDefault(candidate => candidate.Id == goal.JobId);
                if (job is not null)
                {
                    foreach (var task in job.Tasks.Where(task => !task.IsTerminal))
                    {
                        task.Interrupt("Goal exceeded recovery age limit.");
                        state.Reservations.ReleaseByOwner(task.Id);
                    }

                    job.RefreshStatus();
                }

                state.EmitEvent(
                    "recovery.goal_failed",
                    Name,
                    $"{goal.Id} failed after exceeding recovery age limit.",
                    goal.AgentId,
                    new Dictionary<string, string>
                    {
                        ["goalId"] = goal.Id,
                        ["ageTicks"] = (state.ActiveTick - goal.CreatedTick).ToString(System.Globalization.CultureInfo.InvariantCulture)
                    },
                    severity: "warning",
                    importance: 0.66f,
                    tags: ["recovery", "goal"]);
            }
        }

        private void ReleaseDanglingReservations(SimulationState state)
        {
            HashSet<string> liveTaskIds = state.ActiveJobs.Concat(state.PendingJobs)
                .SelectMany(job => job.Tasks)
                .Where(task => task.Status is TaskStatus.Assigned or TaskStatus.InProgress)
                .Select(task => task.Id)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var reservation in state.Reservations.Reservations.ToList())
            {
                if (liveTaskIds.Contains(reservation.OwnerId))
                {
                    continue;
                }

                state.Reservations.ReleaseByOwner(reservation.OwnerId);
                state.EmitEvent(
                    "recovery.reservation_released",
                    Name,
                    $"{reservation.OwnerId} released dangling reservation {reservation.Target.Id}.",
                    reservation.OwnerId,
                    new Dictionary<string, string>
                    {
                        ["targetId"] = reservation.Target.Id,
                        ["targetType"] = reservation.Target.Type.ToString()
                    },
                    severity: "info",
                    importance: 0.42f,
                    tags: ["recovery", "reservation"]);
            }
        }
    }
}
