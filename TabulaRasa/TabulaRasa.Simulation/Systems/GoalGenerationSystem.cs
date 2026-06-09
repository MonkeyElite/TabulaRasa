using TabulaRasa.Abstractions.Agents;
using TabulaRasa.Abstractions.Execution;
using TabulaRasa.Agents.Models;
using TabulaRasa.Simulation.Goals;
using TabulaRasa.Simulation.Interfaces;
using TabulaRasa.Simulation.Knowledge;
using TabulaRasa.Simulation.State;
using TabulaRasa.Simulation.Tasks.Definitions;
using TabulaRasa.Simulation.Tasks.Jobs;
using TabulaRasa.Simulation.Tasks.Reservations;
using TabulaRasa.World.Entities;
using TabulaRasa.World.Queries;
using TabulaRasa.World.Resources;
using TaskStatus = TabulaRasa.Simulation.Tasks.Definitions.TaskStatus;

namespace TabulaRasa.Simulation.Systems
{
    public sealed class GoalGenerationSystem : ISystem
    {
        private const float HungerThreshold = 5f;
        private const float UrgentHungerThreshold = 8f;
        private const int InterruptionPriorityDelta = 20;

        public string Name => "Goal Generation System";
        public SimulationPhase Phase => SimulationPhase.Evaluation;
        public int Priority => 1;

        public void Execute(SimulationState state)
        {
            RefreshGoalStatuses(state);

            foreach (AgentEntity agentEntity in state.World.Agents)
            {
                if (agentEntity.IsDead)
                {
                    continue;
                }

                AgentState? agentState = state.GetAgentById(agentEntity.Id);
                if (agentState is null)
                {
                    continue;
                }

                AgentGoal? activeGoal = state.Goals.LastOrDefault(goal => goal.AgentId == agentEntity.Id && goal.IsActive);
                if (agentState.NeedState.Hunger >= HungerThreshold)
                {
                    int priority = BuildHungerPriority(agentState.NeedState.Hunger);
                    if (activeGoal is not null)
                    {
                        state.PendingIntents.RemoveAll(intent => intent.AgentId == agentEntity.Id);
                        if (priority <= activeGoal.Priority + InterruptionPriorityDelta)
                        {
                            continue;
                        }

                        InterruptGoal(state, activeGoal, "Higher priority hunger goal.");
                    }

                    if (IsAgentBusy(state, agentEntity.Id))
                    {
                        state.PendingIntents.RemoveAll(intent => intent.AgentId == agentEntity.Id);
                        if (agentState.NeedState.Hunger < UrgentHungerThreshold)
                        {
                            continue;
                        }

                        InterruptAgentWork(state, agentEntity.Id, "Higher priority hunger goal.");
                    }

                    CreateHungerGoal(state, agentEntity, agentState, priority);
                    state.PendingIntents.RemoveAll(intent => intent.AgentId == agentEntity.Id);
                    continue;
                }

                if (activeGoal is not null || IsAgentBusy(state, agentEntity.Id))
                {
                    continue;
                }

                TryCreateInventionGoal(state, agentEntity, agentState);
            }
        }

        private void RefreshGoalStatuses(SimulationState state)
        {
            foreach (AgentGoal goal in state.Goals.Where(goal => goal.IsActive).ToList())
            {
                JobInstance? job = state.ActiveJobs.Concat(state.PendingJobs)
                    .FirstOrDefault(candidate => candidate.Id == goal.JobId);
                if (job is null)
                {
                    continue;
                }

                switch (job.Status)
                {
                    case JobStatus.Completed:
                        goal.Complete(state.ActiveTick);
                        EmitGoalEvent(state, "goal.completed", goal, $"{goal.Id} completed.");
                        break;
                    case JobStatus.Failed:
                        goal.Fail(GetJobFailureReason(job), state.ActiveTick);
                        EmitGoalEvent(state, "goal.failed", goal, $"{goal.Id} failed: {goal.FailureReason}");
                        break;
                    case JobStatus.Cancelled:
                    case JobStatus.Interrupted:
                        goal.Interrupt(GetJobFailureReason(job), state.ActiveTick);
                        EmitGoalEvent(state, "goal.interrupted", goal, $"{goal.Id} interrupted: {goal.FailureReason}");
                        break;
                }
            }
        }

        private void CreateHungerGoal(
            SimulationState state,
            AgentEntity agentEntity,
            AgentState agentState,
            int priority)
        {
            FoodTarget? target = SelectFoodTarget(state, agentEntity);
            string goalId = $"goal:{agentEntity.Id}:hunger:{state.ActiveTick}:{state.Goals.Count + 1}";
            AgentGoal goal = new(
                goalId,
                agentEntity.Id,
                "Hunger",
                target is null ? "Search for food." : "Resolve hunger with food.",
                priority,
                state.ActiveTick,
                target?.Id,
                target?.TargetType);

            JobInstance job = target is null
                ? CreateSearchJob(goal, agentState)
                : target.FromInventory
                    ? CreateEatFromInventoryJob(goal, agentState)
                    : CreateFoodJob(goal, target, agentState);

            goal.LinkJob(job.Id, state.ActiveTick);
            state.Goals.Add(goal);
            state.PendingJobs.Add(job);

            EmitGoalEvent(state, "goal.created", goal, $"{goal.Id} created for {agentEntity.Id}.");
            if (state.Goals.Any(existing =>
                existing.AgentId == agentEntity.Id
                && existing.NeedKey == "Hunger"
                && existing.Status is GoalStatus.Failed or GoalStatus.Interrupted))
            {
                EmitGoalEvent(state, "goal.replanned", goal, $"{goal.Id} replanned hunger work.");
            }
        }

        private static void TryCreateInventionGoal(
            SimulationState state,
            AgentEntity agentEntity,
            AgentState agentState)
        {
            if (agentState.NeedState.Hunger > 4
                || agentState.NeedState.Thirst > 4
                || agentState.NeedState.Fatigue > 5)
            {
                return;
            }

            Dictionary<string, int> inventory = agentEntity.Inventory.Stacks
                .GroupBy(stack => stack.ResourceId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Sum(stack => stack.Quantity), StringComparer.OrdinalIgnoreCase);
            AgentKnowledgeStore knowledge = state.GetKnowledgeStore(agentEntity.Id);
            RecipeDefinition? craftable = RecipeRegistry.FindCraftableRecipes(inventory, knowledge).FirstOrDefault();
            RecipeDefinition? experiment = craftable is null
                ? RecipeRegistry.FindExperimentCandidates(inventory, knowledge).FirstOrDefault()
                : null;
            RecipeDefinition? recipe = craftable ?? experiment;
            if (recipe is null)
            {
                return;
            }

            AgentActionType actionType = craftable is not null ? AgentActionType.Craft : AgentActionType.Experiment;
            int priority = craftable is not null ? 18 : 12;
            string goalId = $"goal:{agentEntity.Id}:invention:{state.ActiveTick}:{state.Goals.Count + 1}";
            AgentGoal goal = new(
                goalId,
                agentEntity.Id,
                "Invention",
                craftable is not null ? "Craft known recipe." : "Experiment with resources.",
                priority,
                state.ActiveTick,
                recipe.Id,
                "Recipe");
            TaskDefinition task = new(
                actionType == AgentActionType.Craft ? "craft-recipe" : "experiment-recipe",
                actionType == AgentActionType.Craft ? "Craft Recipe" : "Experiment",
                requiredProgressTicks: 1,
                atomicAction: actionType,
                executionKind: TaskExecutionKind.Action,
                targetId: recipe.Id,
                targetType: "Recipe",
                selectedGoal: "Invention",
                contextKey: $"Invention|Recipe|{actionType}");
            JobInstance job = new(
                $"job:{goal.Id}:{task.Id}",
                new JobDefinition($"invention-{task.Id}", task.Name, [new JobStepDefinition(task.Id, task)], priority),
                agentEntity.Id,
                goal.Id);

            goal.LinkJob(job.Id, state.ActiveTick);
            state.Goals.Add(goal);
            state.PendingJobs.Add(job);
            EmitGoalEvent(state, "goal.created", goal, $"{goal.Id} created for {agentEntity.Id}.");
            state.PendingIntents.RemoveAll(intent => intent.AgentId == agentEntity.Id);
        }

        private static FoodTarget? SelectFoodTarget(SimulationState state, AgentEntity agentEntity)
        {
            if (agentEntity.Inventory.GetQuantity(ResourceDefinition.FoodId) > 0)
            {
                return new FoodTarget(null, "Food", true, 1);
            }

            AgentPerception perception = state.LatestPerceptionsByAgentId.GetValueOrDefault(agentEntity.Id)
                ?? AgentPerception.Empty;

            return perception.Opportunities
                .Where(opportunity => opportunity.ActionType == AgentActionType.Eat)
                .Where(opportunity => opportunity.TargetId is not null)
                .Where(opportunity => state.World.ResourceContainers.Any(container =>
                    container.Id == opportunity.TargetId
                    && SpatialQueries.ContainerHasFood(container)))
                .OrderByDescending(opportunity => opportunity.Relevance)
                .ThenBy(opportunity => opportunity.TargetId, StringComparer.Ordinal)
                .Select(opportunity => new FoodTarget(opportunity.TargetId, "Food", false, opportunity.Relevance))
                .FirstOrDefault();
        }

        private static JobInstance CreateSearchJob(AgentGoal goal, AgentState agentState)
        {
            TaskDefinition search = new(
                "search-food",
                "Search For Food",
                requiredProgressTicks: 1,
                atomicAction: AgentActionType.Wander,
                executionKind: TaskExecutionKind.Movement,
                selectedGoal: "Hunger",
                contextKey: "Hunger|World|Task");

            return new JobInstance(
                $"job:{goal.Id}:search-food",
                new JobDefinition("hunger-search-food", "Search For Food", [new JobStepDefinition("search-food", search)], goal.Priority),
                goal.AgentId,
                goal.Id);
        }

        private static JobInstance CreateEatFromInventoryJob(AgentGoal goal, AgentState agentState)
        {
            TaskDefinition eat = new(
                "eat-carried-food",
                "Eat Carried Food",
                requiredProgressTicks: 1,
                atomicAction: AgentActionType.Eat,
                executionKind: TaskExecutionKind.Action,
                targetType: ResourceDefinition.FoodId,
                selectedGoal: "Hunger",
                contextKey: "Hunger|Food|Inventory");

            return new JobInstance(
                $"job:{goal.Id}:eat-carried-food",
                new JobDefinition("hunger-eat-carried-food", "Eat Carried Food", [new JobStepDefinition("eat-food", eat)], goal.Priority),
                goal.AgentId,
                goal.Id);
        }

        private static JobInstance CreateFoodJob(AgentGoal goal, FoodTarget target, AgentState agentState)
        {
            string targetId = target.Id ?? "";
            IReadOnlyList<ITaskPrecondition> foodPreconditions =
            [
                new EntityExistsPrecondition(targetId),
                new FoodAvailablePrecondition(targetId)
            ];

            TaskDefinition find = new(
                "find-food",
                "Find Food",
                requiredProgressTicks: 1,
                executionKind: TaskExecutionKind.Progress,
                targetId: targetId,
                targetType: ResourceDefinition.FoodId,
                selectedGoal: "Hunger",
                contextKey: "Hunger|Food|Task",
                preconditions: foodPreconditions);
            TaskDefinition move = new(
                "move-to-food",
                "Move To Food",
                requiredProgressTicks: 1,
                atomicAction: AgentActionType.Eat,
                executionKind: TaskExecutionKind.Movement,
                targetId: targetId,
                targetType: ResourceDefinition.FoodId,
                selectedGoal: "Hunger",
                contextKey: "Hunger|Food|Task",
                preconditions: foodPreconditions,
                requirements: [new TaskRequirement(ReservationTargetType.Resource, targetId)]);
            TaskDefinition eat = new(
                "eat-food",
                "Eat Food",
                requiredProgressTicks: 1,
                atomicAction: AgentActionType.Eat,
                executionKind: TaskExecutionKind.Action,
                targetId: targetId,
                targetType: ResourceDefinition.FoodId,
                selectedGoal: "Hunger",
                contextKey: "Hunger|Food|Task",
                preconditions: foodPreconditions,
                requirements: [new TaskRequirement(ReservationTargetType.Resource, targetId)]);

            JobDefinition definition = new(
                "hunger-find-and-eat-food",
                "Find And Eat Food",
                [
                    new JobStepDefinition("find-food", find),
                    new JobStepDefinition("move-to-food", move, ["find-food"]),
                    new JobStepDefinition("eat-food", eat, ["move-to-food"])
                ],
                goal.Priority);

            return new JobInstance($"job:{goal.Id}:food", definition, goal.AgentId, goal.Id);
        }

        private static bool IsAgentBusy(SimulationState state, string agentId)
        {
            return state.ActiveMovements.Any(movement => movement.AgentId == agentId)
                || state.ActiveJobs
                    .SelectMany(job => job.Tasks)
                    .Any(task =>
                        task.AssignedAgentId == agentId
                        && task.Status is TaskStatus.Assigned or TaskStatus.InProgress);
        }

        private static void InterruptAgentWork(SimulationState state, string agentId, string reason)
        {
            foreach (TaskInstance task in state.ActiveJobs
                .SelectMany(job => job.Tasks)
                .Where(task => task.AssignedAgentId == agentId)
                .Where(task => task.Status is TaskStatus.Assigned or TaskStatus.InProgress))
            {
                task.Interrupt(reason);
                state.Reservations.ReleaseByOwner(task.Id);
            }

            foreach (var movement in state.ActiveMovements.Where(movement => movement.AgentId == agentId).ToList())
            {
                state.ActiveMovements.Remove(movement);
            }

            foreach (JobInstance job in state.ActiveJobs)
            {
                job.RefreshStatus();
            }
        }

        private static void InterruptGoal(SimulationState state, AgentGoal goal, string reason)
        {
            goal.Interrupt(reason, state.ActiveTick);
            JobInstance? job = state.ActiveJobs.Concat(state.PendingJobs)
                .FirstOrDefault(candidate => candidate.Id == goal.JobId);
            if (job is not null)
            {
                foreach (TaskInstance task in job.Tasks
                    .Where(task => task.Status is TaskStatus.Pending or TaskStatus.Assigned or TaskStatus.InProgress))
                {
                    task.Interrupt(reason);
                    state.Reservations.ReleaseByOwner(task.Id);
                }

                job.RefreshStatus();
            }

            state.PendingActionRequests.RemoveAll(request => request.SourceGoalId == goal.Id);
            foreach (var movement in state.ActiveMovements.Where(movement => movement.SourceGoalId == goal.Id).ToList())
            {
                state.ActiveMovements.Remove(movement);
            }

            EmitGoalEvent(state, "goal.interrupted", goal, $"{goal.Id} interrupted: {reason}");
        }

        private static int BuildHungerPriority(float hunger)
        {
            return (int)MathF.Round(Math.Clamp(hunger, 0, 10) * 10);
        }

        private static string GetJobFailureReason(JobInstance job)
        {
            return job.Tasks.FirstOrDefault(task => task.FailureReason is not null)?.FailureReason
                ?? $"Job is {job.Status}.";
        }

        private static void EmitGoalEvent(
            SimulationState state,
            string type,
            AgentGoal goal,
            string message)
        {
            state.EmitEvent(
                type,
                "Goal Generation System",
                message,
                goal.AgentId,
                new Dictionary<string, string>
                {
                    ["goalId"] = goal.Id,
                    ["agentId"] = goal.AgentId,
                    ["needKey"] = goal.NeedKey,
                    ["priority"] = goal.Priority.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["targetId"] = goal.TargetId ?? "",
                    ["jobId"] = goal.JobId ?? "",
                    ["status"] = goal.Status.ToString()
                });
        }

        private sealed record FoodTarget(string? Id, string TargetType, bool FromInventory, float Relevance);
    }
}
