using Microsoft.AspNetCore.Components;

namespace Client.Templates.Panels
{
    public class ExplorerNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;

        public bool IsFolder { get; set; }
        public bool IsExpanded { get; set; } = true;

        // 🔥 CLAVE: El nodo lleva su propio contenido. 
        // El panel no sabe qué es, solo lo renderiza.
        public RenderFragment? Content { get; set; }

        public List<ExplorerNode> Children { get; set; } = new();
        public Func<bool, RenderFragment>? ContentFactory { get; set; }
    }
}
