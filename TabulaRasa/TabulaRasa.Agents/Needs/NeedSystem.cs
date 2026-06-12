using TabulaRasa.Agents.Models;

namespace TabulaRasa.Agents.Needs
{
    public class NeedSystem
    {
        public const float MaximumNeedValue = 10;
        public const float MaximumEnergyValue = 10;
        public const float EatRecoveryAmount = 5;
        public const float DrinkRecoveryAmount = 5;
        public const float RestEnergyRecoveryAmount = 4;
        public const float RestFatigueRecoveryAmount = 5;

        public static AgentNeedState ApplyNeedDecay(
            AgentNeedState needState,
            float hungerDelta = 1,
            float thirstDelta = 1,
            float energyDelta = -1,
            float fatigueDelta = 1)
        {
            needState.Hunger = ClampNeed(needState.Hunger + hungerDelta);
            needState.Thirst = ClampNeed(needState.Thirst + thirstDelta);
            needState.Energy = ClampEnergy(needState.Energy + energyDelta);
            needState.Fatigue = ClampNeed(needState.Fatigue + fatigueDelta);

            return needState;
        }

        public static AgentNeedState ApplyNeedDecay(
            AgentNeedState needState,
            float maximumNeedValue,
            float maximumEnergyValue,
            float hungerDelta = 1,
            float thirstDelta = 1,
            float energyDelta = -1,
            float fatigueDelta = 1)
        {
            needState.Hunger = ClampNeed(needState.Hunger + hungerDelta, maximumNeedValue);
            needState.Thirst = ClampNeed(needState.Thirst + thirstDelta, maximumNeedValue);
            needState.Energy = ClampEnergy(needState.Energy + energyDelta, maximumEnergyValue);
            needState.Fatigue = ClampNeed(needState.Fatigue + fatigueDelta, maximumNeedValue);

            return needState;
        }

        public static AgentNeedState ApplyEat(AgentNeedState needState)
        {
            needState.Hunger = ClampNeed(needState.Hunger - EatRecoveryAmount);

            return needState;
        }

        public static AgentNeedState ApplyEat(AgentNeedState needState, float recoveryAmount, float maximumNeedValue)
        {
            needState.Hunger = ClampNeed(needState.Hunger - recoveryAmount, maximumNeedValue);

            return needState;
        }

        public static AgentNeedState ApplyDrink(AgentNeedState needState)
        {
            needState.Thirst = ClampNeed(needState.Thirst - DrinkRecoveryAmount);

            return needState;
        }

        public static AgentNeedState ApplyDrink(AgentNeedState needState, float recoveryAmount, float maximumNeedValue)
        {
            needState.Thirst = ClampNeed(needState.Thirst - recoveryAmount, maximumNeedValue);

            return needState;
        }

        public static AgentNeedState ApplyRest(AgentNeedState needState)
        {
            needState.Energy = ClampEnergy(needState.Energy + RestEnergyRecoveryAmount);
            needState.Fatigue = ClampNeed(needState.Fatigue - RestFatigueRecoveryAmount);

            return needState;
        }

        public static AgentNeedState ApplyRest(
            AgentNeedState needState,
            float energyRecoveryAmount,
            float fatigueRecoveryAmount,
            float maximumNeedValue,
            float maximumEnergyValue)
        {
            needState.Energy = ClampEnergy(needState.Energy + energyRecoveryAmount, maximumEnergyValue);
            needState.Fatigue = ClampNeed(needState.Fatigue - fatigueRecoveryAmount, maximumNeedValue);

            return needState;
        }

        public static float ClampNeed(float value)
        {
            return Math.Clamp(value, 0, MaximumNeedValue);
        }

        public static float ClampNeed(float value, float maximumNeedValue)
        {
            return Math.Clamp(value, 0, Math.Max(0, maximumNeedValue));
        }

        public static float ClampEnergy(float value)
        {
            return Math.Clamp(value, 0, MaximumEnergyValue);
        }

        public static float ClampEnergy(float value, float maximumEnergyValue)
        {
            return Math.Clamp(value, 0, Math.Max(0, maximumEnergyValue));
        }
    }
}
