using Server.Entities.UserManagement;

namespace Server.Entities.Projects
{
    public class ProjectCollaborator : Entity, ITennant
    {
        public override bool IsTenanted => true;

        public string TenantId { get; set; } = string.Empty;
        public Guid ProjectId { get; set; }
        public ProjectRecord? Project { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
        public ProjectCollaboratorRole Role { get; set; } = ProjectCollaboratorRole.Viewer;
        public DateTime? LastOpenedOnUtc { get; set; }
    }
}
