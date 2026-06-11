using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Shared.ProcessFlowDiagram;

// Asume que tu namespace general engloba IVisualElement. 
// Si es necesario, agrega los using correspondientes a tus modelos.

public abstract class EquipmentUIBase<TElement> : ComponentBase where TElement : IVisualElement
{
    // 1. El modelo de datos fuertemente tipado (Bomba, Corriente, etc.)
    [Parameter, EditorRequired]
    public TElement Element { get; set; } = default!;

    // 2. Eventos comunes que mandamos hacia el lienzo (Home)
    [Parameter]
    public EventCallback<IVisualElement> OnSelect { get; set; }

    [Parameter]
    public EventCallback<MouseEventArgs> OnDragStart { get; set; }

    // Añade estas dos al EquipmentUIBase.cs
    [Parameter] public bool IsDragging { get; set; }
    [Parameter] public bool IsSelected { get; set; }

    protected abstract Task OpenPropertiesDialogAsync();
    protected virtual async Task HandleSelectionAsync()
    {
        if (OnSelect.HasDelegate)
        {
            await OnSelect.InvokeAsync(Element);
        }
    }
}