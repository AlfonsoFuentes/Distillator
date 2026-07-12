using Distillator.Domain.Configuration;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using UnitSystem;

namespace Client.Pages.Dialogs.ProjectForm;

public partial class ProjectUnitsTab
{
    [Parameter] public IReadOnlyList<IProjectUnitSystem> UnitSystems { get; set; } = Array.Empty<IProjectUnitSystem>();

    [Parameter] public string ActiveUnitSystemName { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> ActiveUnitSystemNameChanged { get; set; }

    [Parameter] public bool IsCreatingUnitSystem { get; set; }

    [Parameter] public string NewUnitSystemName { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> NewUnitSystemNameChanged { get; set; }

    [Parameter] public EventCallback StartCreateUnitSystem { get; set; }
    [Parameter] public EventCallback CancelCreateUnitSystem { get; set; }
    [Parameter] public EventCallback CreateUnitSystem { get; set; }

    [Parameter] public IProjectUnitSystem? ActiveUnitSystem { get; set; }
    [Parameter] public IEnumerable<ProjectFormDialog.UnitRow> UnitRows { get; set; } = Array.Empty<ProjectFormDialog.UnitRow>();

    [Parameter] public Func<ProjectFormDialog.UnitSlot, IEnumerable<ProjectFormDialog.UnitOption>> GetUnitOptions { get; set; } = _ => Array.Empty<ProjectFormDialog.UnitOption>();

    // Mantenemos Action aquí por los 2 parámetros. 
    // Recuerda: El padre debe llamar a StateHasChanged() en este método.
    [Parameter] public Action<ProjectFormDialog.UnitSlot, string?> SetUnit { get; set; } = (_, _) => { };

    [Parameter] public Func<UnitMeasure, string> UnitText { get; set; } = _ => "Not configured";

    private async Task OnUnitSystemChanged(ChangeEventArgs e) =>
        await ActiveUnitSystemNameChanged.InvokeAsync(e.Value?.ToString());

    private async Task OnNewUnitSystemNameInput(ChangeEventArgs e) =>
        await NewUnitSystemNameChanged.InvokeAsync(e.Value?.ToString() ?? string.Empty);
}
