using Shared.UnitOperations.Streams;

namespace Shared.ProcessFlowDiagram.Streams
{
    public class StreamVisualElement : VisualElementBase
    {
        public override List<ToolTipLegend> GetToolTipData() => Facade.GetToolTipLegend();
        public override EquipmentType Type => EquipmentType.MaterialStream;
        public override string Prefix => "S";

        // 1. Identidad
    

        // 2. Restricciones (Constraints) específicas para una Corriente
        // Una flecha SÍ puede girar libremente (Arriba, Abajo, Izquierda, Derecha)
        public override bool AllowFreeRotation => true;

        // 🎯 CAMBIO: Ahora permitimos el Flip Horizontal para facilitar el diseño rápido
        public override bool AllowFlipHorizontal => true;

        // El flip vertical usualmente no se usa en corrientes pero podrías activarlo si quieres
        public override bool AllowFlipVertical => false;

        // Las corrientes no se redimensionan estirándolas, tienen un tamaño fijo
        public override bool IsResizable => false;

        private bool IsVertical => RotationAngle == 90 || RotationAngle == 270;

        // 1. Propiedad auxiliar para saber si la flecha está vertical
        // (Esto es lo que pediste: "ya que sabemos como esta rotado segun RotationAngle")
    

        // 2. Coordenadas dinámicas para la etiqueta (Nombre)
        // Si está horizontal (0, 180): Standard (-28px respecto al Wrapper CSS bottom)
        // Si está vertical (90, 270): Empujamos más abajo (-40px) para librar la cola/punta
        public override int LabelOffsetY => IsVertical ? -60 : -33;

        // 3. Coordenadas dinámicas para la Toolbar (Botones)
        // Si está horizontal: Standard (-30px respecto al Wrapper CSS top)
        // Si está vertical: Empujamos más arriba (-45px) para librar la punta/cola
        public override int ToolbarOffsetY => IsVertical ? -55 : -30;

        // 4. Coordenadas dinámicas para el Tooltip de propiedades
        // Si está vertical, necesita "nacer" más abajo
        public override int TooltipOffsetY => IsVertical ? 35 : 5;

        public StreamVisualElement()
        {
            // 3. Geometría Base (Acorde a los ajustes de SVG que hicimos)
            Width = 60;
            Height = 30;

            // Modifica la parte de AddPort en el constructor:
            // El inicio de la flecha es 0. Le restamos 4 para el Inlet.
            AddPort("Inlet", PortType.Inlet, -4, 15, PortDirection.Left);

            // La punta de la flecha es 60. Le sumamos 4 para el Outlet.
            AddPort("Outlet", PortType.Outlet, 64, 15, PortDirection.Right);

            // 5. Facade por defecto
            Facade = new StreamFacade
            {
                Id = this.Id,
                Name = "S-New"
            };
        }

        public override bool CanConnect(string myPortName, IVisualElement targetElement, string targetPortName)
        {
            // 1. Verificación base (puertos disponibles, tipos compatibles)
            if (!base.CanConnect(myPortName, targetElement, targetPortName)) return false;

            // 2. REGLA DE NEGOCIO: Una corriente NO puede conectarse a otra corriente directamente.
            // Debe haber un equipo de por medio (Bomba, Tanque, Válvula, etc.)
            if (targetElement is StreamVisualElement) return false;

            return true;
        }
    }
   
}
