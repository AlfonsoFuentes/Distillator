using Microsoft.AspNetCore.Components;

namespace Client.Pages.Dialogs.ProjectForm;

public partial class ProjectReportsTab
{
    [Parameter] public string DefaultFormat { get; set; } = "PDF";
    [Parameter] public EventCallback<string> DefaultFormatChanged { get; set; }
    [Parameter] public bool AutoExportOnSimulation { get; set; }
    [Parameter] public EventCallback<bool> AutoExportOnSimulationChanged { get; set; }
}
