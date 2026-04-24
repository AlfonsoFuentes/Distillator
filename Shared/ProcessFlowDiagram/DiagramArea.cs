using Shared.ProcessFlowDiagram.Pipes;

namespace Shared.ProcessFlowDiagram
{
    public class DiagramArea
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "Nueva Área";

        // Cada área tiene su propio listado exclusivo de equipos y tuberías
        public List<IVisualElement> Elements { get; } = new();
        public List<PipeVisualElement> Pipes { get; } = new();
    }
}
