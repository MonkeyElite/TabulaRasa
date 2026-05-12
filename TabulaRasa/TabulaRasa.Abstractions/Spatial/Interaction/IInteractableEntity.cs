namespace TabulaRasa.Abstractions.Spatial.Interaction
{
    public interface IInteractableEntity
    {
        public IReadOnlyList<InteractionPoint> InteractionPoints { get; }
    }
}
