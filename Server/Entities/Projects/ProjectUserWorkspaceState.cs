namespace Server.Entities.Projects
{
    public class ProjectUserWorkspaceState : Entity, ITennant
    {
        public override bool IsTenanted => true;

        public string TenantId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public Guid? LastProjectId { get; set; }
        public Guid? LastFlowsheetId { get; set; }
        public bool IsProjectExplorerCollapsed { get; set; }
        public bool IsDiagramExplorerCollapsed { get; set; }
        public string? ExpandedDiagramTypeCodesJson { get; set; }
        public DateTime UpdatedOnUtc { get; set; } = DateTime.UtcNow;
    }
}
