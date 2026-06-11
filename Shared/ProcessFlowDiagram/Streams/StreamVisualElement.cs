using Shared.SolverQwen.Stream;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.ProcessFlowDiagram.Streams
{
    public class StreamVisualElement : VisualElementBase
    {
      
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

            //5.Facade por defecto
            Facade = new FacadeStream
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

     
        public override bool ShowLabel { get; set; } = true;

        private IFacadeStream LocalFacade => Facade as IFacadeStream ?? throw new InvalidOperationException("Facade must be IFacadeStream");

        public override string StatusColor => LocalFacade.State switch
        {
            StreamStateType.Calculated => "#28a745",              // Verde brillante (TODO OK)
            StreamStateType.FlowCalculated => "#20c997",          // Verde azulado
            StreamStateType.EquilibriumCalculated => "#ffc107",   // Amarillo
            StreamStateType.CompositionDefined => "#17a2b8",      // Azul claro
            StreamStateType.Initialized => "#6c757d",             // Gris
            StreamStateType.Error => "#dc3545",                   // Rojo
            _ => "#6c757d"                                        // Gris por defecto
        };

        public override string StatusText => LocalFacade.State switch
        {
            StreamStateType.Calculated => "Calculated",           // 🔥 NUEVO
            StreamStateType.FlowCalculated => "Flow Calculated",
            StreamStateType.EquilibriumCalculated => "Equilibrium Solved",
            StreamStateType.CompositionDefined => "Composition Defined",
            StreamStateType.Initialized => "Initialized",
            StreamStateType.Undefined => "Undefined",
            StreamStateType.Error => "Error",
            _ => "Unknown"
        };

        // 4. GetToolTipLegend (de IFacade) - Implementación básica por ahora
        public List<ToolTipLegend> GetToolTipLegend()
        {
            var legends = new List<ToolTipLegend>();

            // Variables principales (siempre se muestran, incluso si no están definidas)
            legends.Add(new ToolTipLegend("Temperature", LocalFacade.Temperature.ToUiString()));
            legends.Add(new ToolTipLegend("Pressure", LocalFacade.Pressure.ToUiString()));
            legends.Add(new ToolTipLegend("Mass Flow", LocalFacade.MassFlow.ToUiString()));
            legends.Add(new ToolTipLegend("Vapor Fraction", LocalFacade.VaporFraction.ToUiString()));

            // Composición de cada componente
            if (LocalFacade.Composition?.Components != null)
            {
                foreach (var component in LocalFacade.Composition.Components)
                {
                    legends.Add(new ToolTipLegend(component.Name, component.MassFraction.ToUiString()));
                }
            }

            // Estado general
            legends.Add(new ToolTipLegend("Status", StatusText));

            return legends;
        }
    }
   
}
