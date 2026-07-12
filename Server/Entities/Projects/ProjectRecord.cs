using Server.Entities.UserManagement;

namespace Server.Entities.Projects
{
    public class ProjectRecord : Entity, ITennant
    {
        public override bool IsTenanted => true;

        public string TenantId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string OwnerUserId { get; set; } = string.Empty;
        public ApplicationUser? OwnerUser { get; set; }

        public Guid? ThermodynamicMethodId { get; set; }
        public double PlantElevationValue { get; set; }
        public string PlantElevationUnit { get; set; } = "Meter";
        public string ActiveUnitSystemName { get; set; } = "SI";

        public string UnitSystemsJson { get; set; } = "[]";
        public string CameraConfigurationJson { get; set; } = "{}";
        public string NamingConfigurationJson { get; set; } = "{}";
        public string ReportConfigurationJson { get; set; } = "{}";
        public string EquipmentDesignConfigurationJson { get; set; } = "{}";

        public long Version { get; set; }
        public DateTime UpdatedOnUtc { get; set; } = DateTime.UtcNow;
        public string? UpdatedBy { get; set; }

        public ICollection<ProjectCollaborator> Collaborators { get; set; } = new List<ProjectCollaborator>();
        public ICollection<ProjectDiagramRecord> Diagrams { get; set; } = new List<ProjectDiagramRecord>();
        public ICollection<ProjectChangeLog> ChangeLogs { get; set; } = new List<ProjectChangeLog>();
    }
}
