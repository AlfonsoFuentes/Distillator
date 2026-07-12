namespace Shared.Projects
{
    public class GetUserProjectsRequest
    {
    }

    public class GetProjectRequest
    {
        public Guid ProjectId { get; set; }
    }

    public class CreateProjectRequest
    {
        public Guid? ProjectId { get; set; }
        public string Name { get; set; } = string.Empty;
        public ProjectBasicConfigurationDto Configuration { get; set; } = new();
        public List<ProjectDiagramDto> Diagrams { get; set; } = new();
    }

    public class UpdateProjectConfigurationRequest
    {
        public Guid ProjectId { get; set; }
        public string Name { get; set; } = string.Empty;
        public ProjectBasicConfigurationDto Configuration { get; set; } = new();
        public List<ProjectDiagramDto> Diagrams { get; set; } = new();
    }

    public class ProjectSummaryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string OwnerUserId { get; set; } = string.Empty;
        public long Version { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOnUtc { get; set; }
    }

    public class ProjectDocumentDto : ProjectSummaryDto
    {
        public ProjectBasicConfigurationDto Configuration { get; set; } = new();
        public List<ProjectDiagramDto> Diagrams { get; set; } = new();
        public List<ProjectCollaboratorDto> Collaborators { get; set; } = new();
    }

    public class ProjectBasicConfigurationDto
    {
        public Guid? ThermodynamicMethodId { get; set; }
        public double PlantElevationValue { get; set; }
        public string PlantElevationUnit { get; set; } = "Meter";
        public string ActiveUnitSystemName { get; set; } = "SI";
        public string UnitSystemsJson { get; set; } = "[]";
        public string CameraConfigurationJson { get; set; } = "{}";
        public string NamingConfigurationJson { get; set; } = "{}";
        public string ReportConfigurationJson { get; set; } = "{}";
        public string EquipmentDesignConfigurationJson { get; set; } = "{}";
    }

    public class ProjectDiagramDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TypeCode { get; set; } = string.Empty;
        public string? DiagramNumber { get; set; }
        public int Order { get; set; }
        public string CanvasStateJson { get; set; } = "{}";
    }

    public class ProjectCollaboratorDto
    {
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
