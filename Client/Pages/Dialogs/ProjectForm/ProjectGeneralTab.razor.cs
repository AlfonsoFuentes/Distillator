using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Shared.PropertiesDtos.Methods;
using UnitSystem;

namespace Client.Pages.Dialogs.ProjectForm;

public partial class ProjectGeneralTab
{
    [Parameter] public string ProjectName { get; set; } = string.Empty;

    // CAMBIO: De EventCallback<ChangeEventArgs> a EventCallback<string> 
    // y nombrado con el sufijo 'Changed' para permitir el @bind
    [Parameter] public EventCallback<string> ProjectNameChanged { get; set; }

    // CAMBIO: De Action? a EventCallback
    [Parameter] public EventCallback ValidateName { get; set; }
    [Parameter] public string? NameError { get; set; }

    [Parameter] public string SelectedMethodId { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> SelectedMethodIdChanged { get; set; }
    [Parameter] public IReadOnlyList<ThermodynamicMethodFullDto> Methods { get; set; } = Array.Empty<ThermodynamicMethodFullDto>();

    // CAMBIO: De Action? a EventCallback
    [Parameter] public EventCallback ValidateMethod { get; set; }
    [Parameter] public string? MethodError { get; set; }

    [Parameter] public Length PlantElevation { get; set; } = new Length(0, LengthUnits.Meter);
    [Parameter] public EventCallback<Length> PlantElevationChanged { get; set; }
    [Parameter] public Pressure? AtmosphericPressure { get; set; }

    // Manejador centralizado para el input de texto
    private async Task OnNameInputChanged(ChangeEventArgs e)
    {
        var newName = e.Value?.ToString() ?? string.Empty;
        await ProjectNameChanged.InvokeAsync(newName);

        if (ValidateName.HasDelegate)
            await ValidateName.InvokeAsync();
    }

    private async Task OnNameBlur()
    {
        if (ValidateName.HasDelegate)
            await ValidateName.InvokeAsync();
    }

    // Manejador centralizado para el select
    private async Task OnSelectedMethodChanged(ChangeEventArgs e)
    {
        var newMethodId = e.Value?.ToString() ?? string.Empty;
        await SelectedMethodIdChanged.InvokeAsync(newMethodId);

        if (ValidateMethod.HasDelegate)
            await ValidateMethod.InvokeAsync();
    }

    private async Task OnMethodBlur()
    {
        if (ValidateMethod.HasDelegate)
            await ValidateMethod.InvokeAsync();
    }
}
