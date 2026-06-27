using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Shared.ProcessFlowDiagram;

// Asume que tu namespace general engloba IVisualElement. 
// Si es necesario, agrega los using correspondientes a tus modelos.

public abstract class EquipmentUIBase<TElement> : ComponentBase where TElement : IVisualElement
{
    [Parameter, EditorRequired]
    public TElement Element { get; set; } = default!;

    [Parameter]
    public EventCallback<IVisualElement> OnSelect { get; set; }

    [Parameter]
    public EventCallback<MouseEventArgs> OnDragStart { get; set; }

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

    // 🔥 SIMPLIFICADO: Todas las boquillas usan el mismo cálculo
    // El rect de 10x10 debe tener su centro en (OffsetX, OffsetY)
    // 🔥 LÓGICA DIRECCIONAL: Hace que la boquilla quede a ras del equipo
    //protected double GetNozzleX(Shared.ProcessFlowDiagram.EquipmentPort port)
    //{
    //    if (port.Direction == Shared.ProcessFlowDiagram.PortDirection.Left)
    //        return port.OffsetX; // El cuadro dibuja de X hacia la derecha (hacia adentro)

    //    if (port.Direction == Shared.ProcessFlowDiagram.PortDirection.Right)
    //        return port.OffsetX - 10; // El cuadro dibuja de X hacia la izquierda (hacia adentro)

    //    // Si es Top o Bottom, va centrado horizontalmente
    //    return port.OffsetX - 5;
    //}

    //protected double GetNozzleY(Shared.ProcessFlowDiagram.EquipmentPort port)
    //{
    //    if (port.Direction == Shared.ProcessFlowDiagram.PortDirection.Top)
    //        return port.OffsetY; // El cuadro dibuja de Y hacia abajo (hacia adentro)

    //    if (port.Direction == Shared.ProcessFlowDiagram.PortDirection.Bottom)
    //        return port.OffsetY - 10; // El cuadro dibuja de Y hacia arriba (hacia adentro)

    //    // Si es Left o Right, va centrado verticalmente
    //    return port.OffsetY - 5;
    //}
}