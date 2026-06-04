namespace TabulaRasa.Abstractions.Agents
{
    public enum AgentActionType
    {
        None = 0,
        Wander = 1,
        Eat = 2,
        Drink = 3,
        Rest = 4,
        PickUpResource = 5,
        DropResource = 6,
        TransferResource = 7,
        ConsumeResource = 8,
        Attack = 9,
        Flee = 10,
        Reproduce = 11,
        Communicate = 12
    }
}
