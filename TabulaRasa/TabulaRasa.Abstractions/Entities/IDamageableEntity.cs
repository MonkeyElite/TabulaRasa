namespace TabulaRasa.Abstractions.Entities
{
    public interface IDamageableEntity : IBaseEntity
    {
        public EntityHealth Health { get; }
    }
}
