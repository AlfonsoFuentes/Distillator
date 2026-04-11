using Shared.ProcessFlowDiagram;

namespace Client.Services.LayoutServices
{
    public class DragStateService
    {
        public EquipmentType CurrentDraggedType { get; set; } = EquipmentType.None;
    }
}
