using Server.Entities.UserManagement;

namespace Server.Entities.Projects
{
    public class ProjectChangeLog : Entity, ITennant
    {
        public override bool IsTenanted => true;

        public string TenantId { get; set; } = string.Empty;
        public Guid ProjectId { get; set; }
        public ProjectRecord? Project { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
        public ProjectChangeOperation Operation { get; set; } = ProjectChangeOperation.Updated;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string? OldValueJson { get; set; }
        public string? NewValueJson { get; set; }
        public long ProjectVersion { get; set; }
        public DateTime OccurredOnUtc { get; set; } = DateTime.UtcNow;
    }
}
