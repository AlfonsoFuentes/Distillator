using Distillator.Domain.Configuration;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Client.Pages.Dialogs.ProjectForm;



// Asegúrate de que el namespace coincida con el tuyo
public partial class ProjectNamingTab
{
    [Parameter] public ProjectFormDialog.NamingSlot SelectedNamingSlot { get; set; }

    // CAMBIO: Usar EventCallback en lugar de Action
    [Parameter] public EventCallback<ProjectFormDialog.NamingSlot> SelectNamingSlot { get; set; }

    [Parameter] public Func<ProjectFormDialog.NamingSlot, string> GetNamingSlotCss { get; set; } = _ => string.Empty;
    [Parameter] public Func<ProjectFormDialog.NamingSlot, string> GetNamingSlotValue { get; set; } = _ => string.Empty;
    [Parameter] public Func<string, int, string> BuildNamingPreview { get; set; } = (_, _) => string.Empty;
    [Parameter] public int NamingStartingNumber { get; set; }
    [Parameter] public EventCallback<int> NamingStartingNumberChanged { get; set; }
    [Parameter] public Func<IEnumerable<(string Name, IEnumerable<(string Type, string Name)> Items)>> GetDiagramNamingPreview { get; set; } = () => Array.Empty<(string, IEnumerable<(string, string)>)>();
    [Parameter] public Func<string> GetNamingEffectText { get; set; } = () => string.Empty;
    [Parameter] public Func<ProjectFormDialog.NamingSlot, string> NamingSlotTitle { get; set; } = _ => string.Empty;
    [Parameter] public Func<ProjectFormDialog.NamingSlot, string> NamingSlotDescription { get; set; } = _ => string.Empty;
    [Parameter] public bool UseDiagramPrefix { get; set; }

    // CAMBIO: Usar EventCallback en lugar de Action
    [Parameter] public EventCallback<bool> SetUseDiagramPrefix { get; set; }

    [Parameter] public Func<ProjectFormDialog.NamingSlot, bool> IsSeparatorSlot { get; set; } = _ => false;
    [Parameter] public Func<IEnumerable<(string Label, string Value)>> GetSeparatorOptions { get; set; } = () => Array.Empty<(string, string)>();
    [Parameter] public Func<ProjectFormDialog.NamingSlot, string> GetSeparatorValue { get; set; } = _ => string.Empty;

    // Los Action con 2 parámetros se quedan igual, pero requieren un ajuste en el padre (ver nota abajo)
    [Parameter] public Action<ProjectFormDialog.NamingSlot, string?> SetSeparatorValue { get; set; } = (_, _) => { };

    [Parameter] public Func<bool, string> GetOptionCardCss { get; set; } = _ => string.Empty;
    [Parameter] public Func<IEnumerable<(string Type, string Prefix)>> GetNamingPrefixRows { get; set; } = () => Array.Empty<(string, string)>();

    [Parameter] public Action<string, string?> SetNamingPrefix { get; set; } = (_, _) => { };

    [Parameter] public NamingCounterScope NamingCounterScope { get; set; }

    // CAMBIO: Usar EventCallback en lugar de Action
    [Parameter] public EventCallback<NamingCounterScope> SetNamingCounterScope { get; set; }

    [Parameter] public Func<IEnumerable<NamingCounterScope>> GetAvailableNamingCounterScopes { get; set; } = () => Array.Empty<NamingCounterScope>();
    [Parameter] public Func<NamingCounterScope, string> NamingCounterScopeLabel { get; set; } = _ => string.Empty;
    [Parameter] public Func<NamingCounterScope, string> NamingCounterScopeOptionText { get; set; } = _ => string.Empty;

    // CAMBIO: Invocar el EventCallback de manera asíncrona
    private async Task OnSelectNamingSlot(ProjectFormDialog.NamingSlot slot)
    {
        if (SelectNamingSlot.HasDelegate)
        {
            await SelectNamingSlot.InvokeAsync(slot);
        }
    }

    private async Task OnNamingStartingNumberChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var value))
        {
            NamingStartingNumber = value;
            await NamingStartingNumberChanged.InvokeAsync(value);
        }
    }
}
