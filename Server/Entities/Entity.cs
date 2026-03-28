namespace Server.Entities
{
    public interface ISoftDeletable
    {
        bool IsDeleted { get; set; }

        DateTime? DeletedOnUtc { get; set; }
    }


    public interface IEntity : ISoftDeletable
    {
        Guid Id { get; }
        DateTime CreatedOn { get; set; }
        string? CreatedBy { get; set; }
        bool IsTenanted { get; }
        int Order { get; set; }
    }
    public interface ITennant
    {
        string TenantId { get; set; }

    }

    public abstract class Entity : IEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedOnUtc { get; set; }
        public virtual bool IsTenanted { get; } = false;
        public int Order { get; set; }


    }
    public class StoredAmount
    {
        public double Value { get; set; }
        public string UnitName { get; set; } = string.Empty;

        public StoredAmount() { }

        public StoredAmount(double value, string unitName)
        {
            Value = value;
            UnitName = unitName;
        }
    }
}
