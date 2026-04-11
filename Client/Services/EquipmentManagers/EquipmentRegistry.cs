using Microsoft.AspNetCore.Components;
using Shared.ProcessFlowDiagram;
namespace Client.Services.EquipmentManagers
{
    public class EquipmentItem
    {
        public EquipmentType Type { get; set; }
        public RenderFragment? IconContent { get; set; }
    }

    public class EquipmentGroup
    {
        public string CategoryName { get; set; } = string.Empty;
        public List<EquipmentItem> Items { get; set; } = new();
    }

    public static class EquipmentRegistry
    {
        public static List<EquipmentGroup> Groups { get; } = new()
    {
        new EquipmentGroup
        {
            CategoryName = "Separation & Process",
            Items = new()
            {
                new EquipmentItem { Type = EquipmentType.Column, IconContent = b => {
                    b.OpenElement(0, "rect"); b.AddAttribute(1, "x", "10"); b.AddAttribute(2, "y", "4"); b.AddAttribute(3, "width", "12"); b.AddAttribute(4, "height", "24"); b.AddAttribute(5, "rx", "6"); b.AddAttribute(6, "fill", "#E2E8F0"); b.AddAttribute(7, "stroke", "#475569"); b.AddAttribute(8, "stroke-width", "1.5"); b.CloseElement();
                    b.OpenElement(9, "line"); b.AddAttribute(10, "x1", "10"); b.AddAttribute(11, "y1", "12"); b.AddAttribute(12, "x2", "22"); b.AddAttribute(13, "y2", "12"); b.AddAttribute(14, "stroke", "#475569"); b.CloseElement();
                }},
                new EquipmentItem { Type = EquipmentType.FlashDrum, IconContent = b => {
                    b.OpenElement(0, "path"); b.AddAttribute(1, "d", "M 6,10 L 26,10 Q 30,16 26,22 L 6,22 Q 2,16 6,10 Z"); b.AddAttribute(2, "fill", "#E2E8F0"); b.AddAttribute(3, "stroke", "#475569"); b.AddAttribute(4, "stroke-width", "1.5"); b.CloseElement();
                    b.OpenElement(5, "rect"); b.AddAttribute(6, "x", "14"); b.AddAttribute(7, "y", "16"); b.AddAttribute(8, "width", "12"); b.AddAttribute(9, "height", "6"); b.AddAttribute(10, "fill", "#60A5FA"); b.AddAttribute(11, "fill-opacity", "0.5"); b.CloseElement();
                }}
            }
        },
        new EquipmentGroup
        {
            CategoryName = "Heat Transfer",
            Items = new()
            {
                new EquipmentItem { Type = EquipmentType.Exchanger, IconContent = b => {
                    int i = 0;
                    b.OpenElement(i++, "g"); b.AddAttribute(i++, "transform", "translate(0, 10) scale(0.2)");
                    b.OpenElement(i++, "defs"); b.OpenElement(i++, "linearGradient");
                    b.AddAttribute(i++, "id", "hxReflexPal"); b.AddAttribute(i++, "x1", "0%"); b.AddAttribute(i++, "y1", "0%"); b.AddAttribute(i++, "x2", "0%"); b.AddAttribute(i++, "y2", "100%");
                    b.OpenElement(i++, "stop"); b.AddAttribute(i++, "offset", "0%"); b.AddAttribute(i++, "style", "stop-color:#ffffff;stop-opacity:0.6"); b.CloseElement();
                    b.OpenElement(i++, "stop"); b.AddAttribute(i++, "offset", "30%"); b.AddAttribute(i++, "style", "stop-color:#ffffff;stop-opacity:0.1"); b.CloseElement();
                    b.OpenElement(i++, "stop"); b.AddAttribute(i++, "offset", "100%"); b.AddAttribute(i++, "style", "stop-color:#000000;stop-opacity:0.2"); b.CloseElement();
                    b.CloseElement(); b.CloseElement();
                    b.OpenElement(i++, "rect"); b.AddAttribute(i++, "x", "10"); b.AddAttribute(i++, "y", "5"); b.AddAttribute(i++, "width", "140"); b.AddAttribute(i++, "height", "50"); b.AddAttribute(i++, "rx", "15"); b.AddAttribute(i++, "fill", "#CBD5E0"); b.CloseElement();
                    b.OpenElement(i++, "rect"); b.AddAttribute(i++, "x", "10"); b.AddAttribute(i++, "y", "5"); b.AddAttribute(i++, "width", "140"); b.AddAttribute(i++, "height", "50"); b.AddAttribute(i++, "rx", "15"); b.AddAttribute(i++, "fill", "url(#hxReflexPal)"); b.AddAttribute(i++, "stroke", "#334155"); b.AddAttribute(i++, "stroke-width", "4"); b.CloseElement();
                    b.OpenElement(i++, "line"); b.AddAttribute(i++, "x1", "10"); b.AddAttribute(i++, "y1", "30"); b.AddAttribute(i++, "x2", "30"); b.AddAttribute(i++, "y2", "30"); b.AddAttribute(i++, "stroke", "#334155"); b.AddAttribute(i++, "stroke-width", "4"); b.CloseElement();
                    b.OpenElement(i++, "line"); b.AddAttribute(i++, "x1", "30"); b.AddAttribute(i++, "y1", "20"); b.AddAttribute(i++, "x2", "135"); b.AddAttribute(i++, "y2", "20"); b.AddAttribute(i++, "stroke", "#334155"); b.AddAttribute(i++, "stroke-dasharray", "4,2"); b.AddAttribute(i++, "stroke-width", "4"); b.CloseElement();
                    b.OpenElement(i++, "line"); b.AddAttribute(i++, "x1", "30"); b.AddAttribute(i++, "y1", "40"); b.AddAttribute(i++, "x2", "135"); b.AddAttribute(i++, "y2", "40"); b.AddAttribute(i++, "stroke", "#334155"); b.AddAttribute(i++, "stroke-dasharray", "4,2"); b.AddAttribute(i++, "stroke-width", "4"); b.CloseElement();
                    b.CloseElement();
                }},
                new EquipmentItem { Type = EquipmentType.PlateExchanger, IconContent = b => {
                    b.OpenElement(0, "rect"); b.AddAttribute(1, "x", "8"); b.AddAttribute(2, "y", "6"); b.AddAttribute(3, "width", "16"); b.AddAttribute(4, "height", "20"); b.AddAttribute(5, "rx", "2"); b.AddAttribute(6, "fill", "#E2E8F0"); b.AddAttribute(7, "stroke", "#475569"); b.AddAttribute(8, "stroke-width", "1.5"); b.CloseElement();
                    b.OpenElement(9, "line"); b.AddAttribute(10, "x1", "12"); b.AddAttribute(11, "y1", "6"); b.AddAttribute(12, "x2", "12"); b.AddAttribute(13, "y2", "26"); b.AddAttribute(14, "stroke", "#475569"); b.AddAttribute(15, "stroke-width", "1.5"); b.CloseElement();
                    b.OpenElement(16, "line"); b.AddAttribute(17, "x1", "16"); b.AddAttribute(18, "y1", "6"); b.AddAttribute(19, "x2", "16"); b.AddAttribute(20, "y2", "26"); b.AddAttribute(21, "stroke", "#475569"); b.AddAttribute(22, "stroke-width", "1.5"); b.CloseElement();
                    b.OpenElement(23, "line"); b.AddAttribute(24, "x1", "20"); b.AddAttribute(25, "y1", "6"); b.AddAttribute(26, "x2", "20"); b.AddAttribute(27, "y2", "26"); b.AddAttribute(28, "stroke", "#475569"); b.AddAttribute(29, "stroke-width", "1.5"); b.CloseElement();
                }},
                new EquipmentItem { Type = EquipmentType.Reboiler, IconContent = b => {
                    int i = 0;
                    b.OpenElement(i++, "g"); b.AddAttribute(i++, "transform", "translate(9, 0) scale(0.22)");
                    b.OpenElement(i++, "defs"); b.OpenElement(i++, "linearGradient");
                    b.AddAttribute(i++, "id", "rebReflexPal"); b.AddAttribute(i++, "x1", "0%"); b.AddAttribute(i++, "y1", "0%"); b.AddAttribute(i++, "x2", "100%"); b.AddAttribute(i++, "y2", "0%");
                    b.OpenElement(i++, "stop"); b.AddAttribute(i++, "offset", "0%"); b.AddAttribute(i++, "style", "stop-color:#ffffff;stop-opacity:0.6"); b.CloseElement();
                    b.OpenElement(i++, "stop"); b.AddAttribute(i++, "offset", "30%"); b.AddAttribute(i++, "style", "stop-color:#ffffff;stop-opacity:0.1"); b.CloseElement();
                    b.OpenElement(i++, "stop"); b.AddAttribute(i++, "offset", "100%"); b.AddAttribute(i++, "style", "stop-color:#000000;stop-opacity:0.2"); b.CloseElement();
                    b.CloseElement(); b.CloseElement();
                    b.OpenElement(i++, "rect"); b.AddAttribute(i++, "x", "10"); b.AddAttribute(i++, "y", "10"); b.AddAttribute(i++, "width", "40"); b.AddAttribute(i++, "height", "120"); b.AddAttribute(i++, "rx", "10"); b.AddAttribute(i++, "fill", "#CBD5E0"); b.CloseElement();
                    b.OpenElement(i++, "rect"); b.AddAttribute(i++, "x", "10"); b.AddAttribute(i++, "y", "10"); b.AddAttribute(i++, "width", "40"); b.AddAttribute(i++, "height", "120"); b.AddAttribute(i++, "rx", "10"); b.AddAttribute(i++, "fill", "url(#rebReflexPal)"); b.AddAttribute(i++, "stroke", "#334155"); b.AddAttribute(i++, "stroke-width", "4"); b.CloseElement();
                    b.OpenElement(i++, "line"); b.AddAttribute(i++, "x1", "30"); b.AddAttribute(i++, "y1", "120"); b.AddAttribute(i++, "x2", "30"); b.AddAttribute(i++, "y2", "20"); b.AddAttribute(i++, "stroke", "#334155"); b.AddAttribute(i++, "stroke-dasharray", "4,3"); b.AddAttribute(i++, "stroke-width", "3"); b.CloseElement();
                    b.CloseElement();
                }}
            }
        },
        new EquipmentGroup
{
    CategoryName = "Fluid Handling",
    Items = new()
    {
        // 🚩 BOMBA REDISEÑADA: Ahora coincide con el equipo P-101 del lienzo
        new EquipmentItem { Type = EquipmentType.Pump, IconContent = b => {
            int i = 0;
            // Agrupamos para centrar y escalar ligeramente el icono
            b.OpenElement(i++, "g");
            b.AddAttribute(i++, "transform", "translate(2, 2)");

            // 1. Motor (Rectángulo redondeado a la derecha)
            b.OpenElement(i++, "path");
            b.AddAttribute(i++, "d", "M 18,14 L 26,14 Q 29,14 29,18 L 29,22 Q 29,26 26,26 L 18,26 Z");
            b.AddAttribute(i++, "fill", "#94A3B8"); // Gris más oscuro para el motor
            b.AddAttribute(i++, "stroke", "#334155");
            b.AddAttribute(i++, "stroke-width", "1.5");
            b.CloseElement();

            // 2. Cuerpo de la Bomba / Voluta (Círculo central)
            b.OpenElement(i++, "circle");
            b.AddAttribute(i++, "cx", "12");
            b.AddAttribute(i++, "cy", "20");
            b.AddAttribute(i++, "r", "8");
            b.AddAttribute(i++, "fill", "#E2E8F0");
            b.AddAttribute(i++, "stroke", "#334155");
            b.AddAttribute(i++, "stroke-width", "1.5");
            b.CloseElement();

            // 3. Boquilla de descarga (Superior)
            b.OpenElement(i++, "rect");
            b.AddAttribute(i++, "x", "10");
            b.AddAttribute(i++, "y", "8");
            b.AddAttribute(i++, "width", "4");
            b.AddAttribute(i++, "height", "5");
            b.AddAttribute(i++, "fill", "#E2E8F0");
            b.AddAttribute(i++, "stroke", "#334155");
            b.AddAttribute(i++, "stroke-width", "1.5");
            b.CloseElement();

            // 4. Base / Soporte (Línea inferior)
            b.OpenElement(i++, "path");
            b.AddAttribute(i++, "d", "M 8,28 L 16,28");
            b.AddAttribute(i++, "stroke", "#334155");
            b.AddAttribute(i++, "stroke-width", "2");
            b.AddAttribute(i++, "stroke-linecap", "round");
            b.CloseElement();

            b.CloseElement(); // Cierra g
        }},

        new EquipmentItem { Type = EquipmentType.ControlValve, IconContent = b => {
            b.OpenElement(0, "path");
            b.AddAttribute(1, "d", "M 6,12 L 26,24 L 26,12 L 6,24 Z");
            b.AddAttribute(2, "fill", "#E2E8F0");
            b.AddAttribute(3, "stroke", "#475569");
            b.AddAttribute(4, "stroke-width", "1.2");
            b.CloseElement();
            // Añadimos el actuador para que se parezca más a la CV-101
            b.OpenElement(5, "line"); b.AddAttribute(6, "x1", "16"); b.AddAttribute(7, "y1", "18"); b.AddAttribute(8, "x2", "16"); b.AddAttribute(9, "y2", "10"); b.AddAttribute(10, "stroke", "#475569"); b.CloseElement();
            b.OpenElement(11, "path"); b.AddAttribute(12, "d", "M 10,10 Q 16,4 22,10"); b.AddAttribute(13, "fill", "none"); b.AddAttribute(14, "stroke", "#475569"); b.AddAttribute(15, "stroke-width", "1.2"); b.CloseElement();
        }}
    }
},
        new EquipmentGroup
        {
            CategoryName = "Flow Logic",
            Items = new()
            {
                new EquipmentItem { Type = EquipmentType.Splitter, IconContent = b => {
                    b.OpenElement(0, "path"); b.AddAttribute(1, "d", "M 8,16 L 24,8 L 24,24 Z"); b.AddAttribute(2, "fill", "#E2E8F0"); b.AddAttribute(3, "stroke", "#475569"); b.AddAttribute(4, "stroke-width", "1.5"); b.CloseElement();
                }},
                new EquipmentItem { Type = EquipmentType.Mixer, IconContent = b => {
                    b.OpenElement(0, "path"); b.AddAttribute(1, "d", "M 8,8 L 24,16 L 8,24 Z"); b.AddAttribute(2, "fill", "#E2E8F0"); b.AddAttribute(3, "stroke", "#475569"); b.AddAttribute(4, "stroke-width", "1.5"); b.CloseElement();
                }}
            }
        },
        new EquipmentGroup
        {
            CategoryName = "Storage",
            Items = new()
            {
                new EquipmentItem { Type = EquipmentType.Tank, IconContent = b => {
                    b.OpenElement(0, "path"); b.AddAttribute(1, "d", "M 10,28 L 22,28 L 22,10 Q 16,4 10,10 Z"); b.AddAttribute(2, "fill", "#E2E8F0"); b.AddAttribute(3, "stroke", "#475569"); b.AddAttribute(4, "stroke-width", "1.5"); b.CloseElement();
                }}
            }
        }
    };
    }


}