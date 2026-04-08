namespace Shared.ProcessFlowDiagram
{
    public class DiagramArea
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "Main Area"; // Ej: "Reacción", "Destilación", "Separación"

        // Cada Área tiene su propio ecosistema de equipos
        public List<IVisualElement> Elements { get; set; } = new();
    }
}
