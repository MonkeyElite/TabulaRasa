namespace TabulaRasa.World.Resources
{
    public sealed class ResourceStack
    {
        public required string StackId { get; init; }
        public required string ResourceId { get; init; }
        public int Quantity { get; set; }
    }
}
