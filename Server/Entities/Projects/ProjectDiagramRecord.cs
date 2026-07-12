namespace Server.Entities.Projects
{
    public class ProjectDiagramRecord : Entity, ITennant
    {
        public override bool IsTenanted => true;

        public string TenantId { get; set; } = string.Empty;
        public Guid ProjectId { get; set; }
        public ProjectRecord? Project { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TypeCode { get; set; } = string.Empty;
        public string? DiagramNumber { get; set; }
        public string CanvasStateJson { get; set; } = "{}";
    }
}
