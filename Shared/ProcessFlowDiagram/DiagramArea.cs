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

        // 🔄 A2: Estado de cámara persistente por área
        public double Zoom { get; set; } = 1.0;
        public double PanX { get; set; } = 0;
        public double PanY { get; set; } = 0;

        // 🔄 A3: Dimensiones del canvas persistentes por área (0 = aún no calculado)
        public double DiagramWidth { get; set; } = 0;
        public double DiagramHeight { get; set; } = 0;
    }
}
