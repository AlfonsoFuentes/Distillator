using Shared.UnitOperations.Basiss;
using Shared.UnitOperations.Streams;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml.Linq;

namespace Shared.ProcessFlowDiagram
{
    public record ToolTipLegend(string Variable, string Value);
    public enum EquipmentType
    {
        [Description("None")] None,

        // --- SECCIÓN SUPERIOR: FUNDAMENTALS ---
        [Description("Material Stream")] MaterialStream,
        [Description("Energy Stream (Reserved)")] EnergyStream,

        // --- EQUIPOS DE PROCESO (Clasificados) ---
        // Separation & Process
        [Description("Column")] Column,
        [Description("Flash Drum")] FlashDrum,

        // Heat Transfer
        [Description("Shell & Tube Heat Exchanger")] Exchanger,
        [Description("Plate Exchanger")] PlateExchanger,
        [Description("Reboiler")] Reboiler,

        // Fluid Handling
        [Description("Centrifugal Pump")] Pump,
        [Description("Control Valve")] ControlValve,

        // Flow Logic
        [Description("Splitter")] Splitter,
        [Description("Mixer")] Mixer,

        // Storage
        [Description("Storage Tank")] Tank,

        // Instrumentation
        [Description("Transmitter")] Instrument,
        [Description("Off-Page Connector")] OffPageConnector,
    }
    public enum PortType
    {
        Inlet,      // Entrada de materia
        Outlet,     // Salida de materia

    }

    // Dirección visual para rutear las tuberías automáticamente (Orthogonal Routing)
    public enum PortDirection
    {
        Top = 0,    // O "Up"
        Right = 1,
        Bottom = 2, // O "Down"
        Left = 3
    }

    // ==========================================
    // 2. PUERTOS (Nodos de Conexión)
    // ==========================================

    public class EquipmentPort
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty; // ej. "Liquid Outlet"
        public PortType Type { get; set; }

        // Coordenadas locales (píxeles) respecto a la esquina superior izquierda del equipo
        public double OffsetX { get; set; }
        public double OffsetY { get; set; }

        public PortDirection Direction { get; set; }

        // 👇 CRÍTICO: Saber si este puerto ya está ocupado y por quién
        public Guid? ConnectedElementId { get; set; } = null;
    }

    public record AbsoluteCoordinates(double X, double Y, PortDirection Direction);

    public interface IVisualElement
    {
        List<ToolTipLegend> GetToolTipData();
        string StatusColor { get; }
        string StatusText { get; }

        string Name { get; set; }
        Guid Id { get; set; }
        EquipmentType Type { get; }
        bool ShowLabel { get; set; }
        string Label { get; set; }
        bool IsLocked { get; set; }  // Para fijarlo en el lienzo y no moverlo por error
        int ZIndex { get; set; }     // Para saber quién tapa a quién (Profundidad)

        // Geometría y Posición
        double X { get; set; }
        double Y { get; set; }
        double Width { get; set; }
        double Height { get; set; }

        // Manipulación Espacial (El estado actual)

        bool IsFlippedHorizontal { get; set; }
        bool IsFlippedVertical { get; set; }

        // 👇 Restricciones del Equipo (Constraints)
        bool AllowFreeRotation { get; }
        bool AllowFlipHorizontal { get; }
        bool AllowFlipVertical { get; }
        bool IsResizable { get; }

        // Nodos de Conexión y Lógica
        List<EquipmentPort> Ports { get; set; }
        IFacade Facade { get; set; }
        string Prefix { get; }
        int ToolbarOffsetY { get; }
        int LabelOffsetY { get; }
        int TooltipOffsetY { get; }
        int RotationAngle { get; set; }

        bool CanConnect(string myPortName, IVisualElement targetElement, string targetPortName);

        // 2. Ejecuta la conexión en la UI y avisa a la Facade
        bool Connect(string myPortName, IVisualElement targetElement, string targetPortName);

        // 3. Rompe la conexión
        void Disconnect(string myPortName);



        // Métodos de acción
        void ToggleFlipHorizontal();
        void ToggleFlipVertical();
        void Rotate90();

        AbsoluteCoordinates GetAbsolutePortCoordinates(string portName);


        void SetDropPosition(double dropX, double dropY, Func<double, double> snapFunction);

        (double X, double Y, double Nx, double Ny) GetCanvasPoint(string portName);
    }

    // ==========================================
    // 4. LA CLASE BASE (El papá de todos los equipos)
    // ==========================================

    public abstract class VisualElementBase : IVisualElement
    {
        public abstract List<ToolTipLegend> GetToolTipData();
        public virtual bool ShowLabel { get; set; } = true;
        public string StatusColor => Facade?.StatusColor ?? "#CBD5E0";
        public string StatusText => Facade?.StatusText ?? "Unknown";
        public string Name
        {
            get { return Facade?.Name ?? "Unknown"; }
            set { Facade?.Name = value; }
        }
        public abstract EquipmentType Type { get; }
        // 1. Propiedad para guardar el ángulo de rotación (0, 90, 180, 270)
        public int RotationAngle { get; set; } = 0;

        // 2. Propiedades virtuales que dicen a qué distancia se dibuja la UI
        public virtual int ToolbarOffsetY => -35;
        public virtual int LabelOffsetY => -30;
        public virtual int TooltipOffsetY => 15;
        public Guid Id { get; set; } = Guid.NewGuid();

        public virtual string Label
        {
            // Si el Facade existe, devuelve su Name. Si no, devuelve un string vacío.
            get => Facade?.Name ?? string.Empty;

            // Al cambiar el Label desde la UI, actualizamos automáticamente el nombre en el motor de simulación.
            set
            {
                if (Facade != null)
                {
                    Facade.Name = value;
                }
            }
        }
        public bool IsLocked { get; set; } = false;
        public int ZIndex { get; set; } = 1;

        // Geometría
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; } = 60;  // Ancho base, el hijo lo puede cambiar en su constructor
        public double Height { get; set; } = 60; // Alto base


        public bool IsFlippedHorizontal { get; set; } = false;
        public bool IsFlippedVertical { get; set; } = false;

        // 👇 Restricciones (Marcadas como 'virtual' para que la Bomba pueda decir "yo no roto")
        public virtual bool AllowFreeRotation => true;
        public virtual bool AllowFlipHorizontal => true;
        public virtual bool AllowFlipVertical => true;
        public virtual bool IsResizable => false; // Los equipos P&ID rara vez cambian de tamaño

        // Colecciones y Enlaces
        public List<EquipmentPort> Ports { get; set; } = new();

        // Asignaremos la Facade desde afuera o en el constructor de cada clase hija
        public IFacade Facade { get; set; } = default!;

        // Método Helper para no repetir código al crear puertos en los hijos
        protected void AddPort(string name, PortType type, double x, double y, PortDirection dir)
        {
            Ports.Add(new EquipmentPort
            {
                Name = name,
                Type = type,
                OffsetX = x, // Mapeamos x a OffsetX
                OffsetY = y, // Mapeamos y a OffsetY
                Direction = dir
            });
        }
        public virtual bool CanConnect(string myPortName, IVisualElement targetElement, string targetPortName)
        {
            var myPort = Ports.FirstOrDefault(p => p.Name == myPortName);
            var targetPort = targetElement.Ports.FirstOrDefault(p => p.Name == targetPortName);

            // 1. Validaciones básicas
            if (myPort == null || targetPort == null) return false;
            if (myPort.ConnectedElementId != null || targetPort.ConnectedElementId != null) return false;
            if (this.Id == targetElement.Id) return false; // No conectarse a sí mismo

            // 2. Regla de Naturaleza: Equipo <-> Stream (Bipartito)
            bool iAmStream = this.Type == EquipmentType.MaterialStream || this.Type == EquipmentType.EnergyStream;
            bool targetIsStream = targetElement.Type == EquipmentType.MaterialStream || targetElement.Type == EquipmentType.EnergyStream;

            if (iAmStream == targetIsStream) return false; // Prohibido Equipo-Equipo o Stream-Stream

            // 3. Regla de Polaridad: Entrada <-> Salida
            bool isCompatible = (myPort.Type, targetPort.Type) switch
            {
                (PortType.Inlet, PortType.Outlet) => true,
                (PortType.Outlet, PortType.Inlet) => true,

                _ => false
            };

            return isCompatible;
        }
        public bool Connect(string myPortName, IVisualElement targetElement, string targetPortName)
        {
            if (!CanConnect(myPortName, targetElement, targetPortName)) return false;

            var myPort = Ports.First(p => p.Name == myPortName);
            var targetPort = targetElement.Ports.First(p => p.Name == targetPortName);

            // 1. Bloqueamos los puertos visualmente cruzando los IDs
            myPort.ConnectedElementId = targetElement.Id;
            targetPort.ConnectedElementId = this.Id;

            // 🔥 2. NUEVA LÓGICA: Solo equipos llaman AttachConnection hacia corrientes
            //if (this.Facade is IEquipmentFacade2 myEquipment && targetElement.Facade is IStreamFacade2 targetStream)
            //{
            //    myEquipment.AttachConnection(myPortName, targetStream);
            //}
            //else if (targetElement.Facade is IEquipmentFacade2 targetEquipment && this.Facade is IStreamFacade2 myStream)
            //{
            //    targetEquipment.AttachConnection(targetPortName, myStream);
            //}
            // Si ninguno es equipo, no hacer nada (CanConnect ya garantiza que uno es equipo y otro stream)

            return true;
        }

        //public bool Connect(string myPortName, IVisualElement targetElement, string targetPortName)
        //{
        //    if (!CanConnect(myPortName, targetElement, targetPortName)) return false;

        //    var myPort = Ports.First(p => p.Name == myPortName);
        //    var targetPort = targetElement.Ports.First(p => p.Name == targetPortName);

        //    // 1. Bloqueamos los puertos visualmente cruzando los IDs
        //    myPort.ConnectedElementId = targetElement.Id;
        //    targetPort.ConnectedElementId = this.Id;

        //    // 2. Avisamos a los "Cerebros" (Facades) para que crucen la termodinámica
        //    this.Facade?.AttachConnection(myPortName, targetElement.Facade!);
        //    targetElement.Facade?.AttachConnection(targetPortName, this.Facade!);

        //    return true;
        //}
        public void Disconnect(string myPortName)
        {
            var myPort = Ports.FirstOrDefault(p => p.Name == myPortName);
            if (myPort == null || myPort.ConnectedElementId == null) return;

            // 🔥 NUEVA LÓGICA: Solo equipos implementan DetachConnection
            //if (this.Facade is IEquipmentFacade2 equipment)
            //{
            //    equipment.DetachConnection(myPortName);
            //}

            // Liberamos el puerto en la UI
            myPort.ConnectedElementId = null;

            // Nota: El lienzo también deberá llamar al Disconnect del otro elemento
        }
        //public void Disconnect(string myPortName)
        //{
        //    var myPort = Ports.FirstOrDefault(p => p.Name == myPortName);
        //    if (myPort == null || myPort.ConnectedElementId == null) return;

        //    // Avisamos al cerebro que soltamos el tubo
        //    this.Facade?.DetachConnection(myPortName);

        //    // Liberamos el puerto en la UI
        //    myPort.ConnectedElementId = null;

        //    // Nota: El lienzo también deberá llamar al Disconnect del otro equipo para que queden libres ambos
        //}
        public void ToggleFlipHorizontal() { if (AllowFlipHorizontal) IsFlippedHorizontal = !IsFlippedHorizontal; }
        public void ToggleFlipVertical() { if (AllowFlipVertical) IsFlippedVertical = !IsFlippedVertical; }
        public void Rotate90()
        {
            if (AllowFreeRotation)
            {
                RotationAngle = (RotationAngle + 90) % 360;
            }
            var rotacion = RotationAngle;
        }
        public AbsoluteCoordinates GetAbsolutePortCoordinates(string portName)
        {
            var (offsetX, offsetY, direction) = GetTransformedPort(portName);
            return new AbsoluteCoordinates(X + offsetX, Y + offsetY, direction);
        }

        // Obligamos a que cada equipo defina su prefijo (ej. "P" para bombas, "S" para corrientes)
        public abstract string Prefix { get; }

        // El propio equipo calcula su centro respecto al ratón y se ajusta a la grilla
        public void SetDropPosition(double dropX, double dropY, Func<double, double> snapFunction)
        {
            // Width y Height ya están definidos en el constructor de cada equipo
            X = snapFunction(dropX - (Width / 2));
            Y = snapFunction(dropY - (Height / 2));
        }

        /// <summary>
        /// Rota una dirección en pasos de 90° CW.
        /// Implementación explícita y segura (sin aritmética modular ambigua).
        /// </summary>
        private PortDirection RotateDirection(PortDirection current, int steps)
        {
            // Normalizar steps a rango [0, 3]
            steps = ((steps % 4) + 4) % 4;

            if (steps == 0) return current;

            var result = steps switch
            {
                1 => current switch
                {
                    PortDirection.Top => PortDirection.Right,
                    PortDirection.Right => PortDirection.Bottom,
                    PortDirection.Bottom => PortDirection.Left,
                    PortDirection.Left => PortDirection.Top,
                    _ => current
                },
                2 => current switch
                {
                    PortDirection.Top => PortDirection.Bottom,
                    PortDirection.Right => PortDirection.Left,
                    PortDirection.Bottom => PortDirection.Top,
                    PortDirection.Left => PortDirection.Right,
                    _ => current
                },
                3 => current switch
                {
                    PortDirection.Top => PortDirection.Left,
                    PortDirection.Right => PortDirection.Top,
                    PortDirection.Bottom => PortDirection.Right,
                    PortDirection.Left => PortDirection.Bottom,
                    _ => current
                },
                _ => current
            };
            return result;
        }

        // Método auxiliar para que la flecha de la tubería sepa hacia dónde salir

        /// <summary>
        /// ÚNICA fuente de verdad para la posición y dirección transformada de un puerto.
        /// Retorna offset local transformado (relativo al top-left) y dirección absoluta.
        /// SIN desplazamientos adicionales.
        /// </summary>
        public (double OffsetX, double OffsetY, PortDirection Direction) GetTransformedPort(string portName)
        {
            var port = Ports.FirstOrDefault(p => p.Name == portName);
            if (port == null) return (0, 0, PortDirection.Top);

            double cx = Width / 2.0;
            double cy = Height / 2.0;

            double px = port.OffsetX;
            double py = port.OffsetY;
            PortDirection dir = port.Direction;

            // ─────────────────────────────────────────────────────────
            // 1. Flips primero
            // ─────────────────────────────────────────────────────────
            if (IsFlippedHorizontal)
            {
                px = Width - px;
                dir = dir switch
                {
                    PortDirection.Left => PortDirection.Right,
                    PortDirection.Right => PortDirection.Left,
                    _ => dir
                };
            }

            if (IsFlippedVertical)
            {
                py = Height - py;
                dir = dir switch
                {
                    PortDirection.Top => PortDirection.Bottom,
                    PortDirection.Bottom => PortDirection.Top,
                    _ => dir
                };
            }

            // ─────────────────────────────────────────────────────────
            // 2. Rotación después (matriz CW en pantalla)
            // ─────────────────────────────────────────────────────────
            double rx, ry;

            if (RotationAngle == 0)
            {
                rx = px;
                ry = py;
            }
            else if (RotationAngle == 90)
            {
                rx = cx - (py - cy);
                ry = cy + (px - cx);
                dir = RotateDirection(dir, 1);
            }
            else if (RotationAngle == 180)
            {
                rx = cx - (px - cx);
                ry = cy - (py - cy);
                dir = RotateDirection(dir, 2);
            }
            else if (RotationAngle == 270)
            {
                rx = cx + (py - cy);
                ry = cy - (px - cx);
                dir = RotateDirection(dir, 3);
            }
            else
            {
                // Fallback genérico usando matriz
                double angleRad = RotationAngle * Math.PI / 180.0;
                double cos = Math.Cos(angleRad);
                double sin = Math.Sin(angleRad);
                rx = cx + (px - cx) * cos - (py - cy) * sin;
                ry = cy + (px - cx) * sin + (py - cy) * cos;
            }

            return (rx, ry, dir);
        }  /// <summary>
           /// Retorna posición del puerto con pushDistance y vector normal para anclaje.
           /// Única fuente de verdad para tooltips y UI.
           /// </summary>
        public (double X, double Y, double Nx, double Ny) GetCanvasPoint(string portName)
        {
            var (offsetX, offsetY, _) = GetTransformedPort(portName);

            double cx = Width / 2.0;
            double cy = Height / 2.0;

            double vx = offsetX - cx;
            double vy = offsetY - cy;

            double length = Math.Sqrt(vx * vx + vy * vy);
            double nx = length > 0 ? vx / length : 0;
            double ny = length > 0 ? vy / length : -1;

            double pushDistance = 15;
            double finalX = offsetX + (nx * pushDistance);
            double finalY = offsetY + (ny * pushDistance);

            return (finalX, finalY, nx, ny);
        }

    }
}
