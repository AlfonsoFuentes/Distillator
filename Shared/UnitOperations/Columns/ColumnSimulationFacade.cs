using Shared.ProcessFlowDiagram;
using Shared.UnitOperations.Basiss;
using Shared.UnitOperations.Streams;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.UnitOperations.Columns
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Shared.ProcessFlowDiagram; // Ajusta según tu namespace

    public enum ColumnStateType { Created, PartiallyConnected, ReadyToCalculate, Solved }

    public class ColumnSimulationFacade : EquipmentFacade
    {
        // ==============================================================================
        // 1. ESTADO Y VARIABLES DEL EQUIPO
        // ==============================================================================
        public ColumnStateType State { get; set; } = ColumnStateType.Created;

        // --- Topología Estática (Condensador y Rehervidor) ---
        public StreamSimulationFacade? OverheadStream { get; private set; }
        public StreamSimulationFacade? BottomsStream { get; private set; }
        public StreamSimulationFacade? RefluxStream { get; private set; }
        public StreamSimulationFacade? ReboilerReturnStream { get; private set; }

        // --- Topología Dinámica (Alimentaciones y Extracciones Múltiples) ---
        public Dictionary<string, StreamSimulationFacade> Feeds { get; } = new();
        public Dictionary<string, StreamSimulationFacade> SideDraws { get; } = new();

        public ColumnSimulationFacade()
        {
            // Constructor vacío.
        }

        private void EvaluateSolverTrigger()
        {
            OnExecuteSolver?.Invoke(this);
        }

        // ==============================================================================
        // 2. INTERFAZ DE USUARIO Y ESTADO VISUAL
        // ==============================================================================
        public override string StatusText => State switch
        {
            ColumnStateType.Created => "Ready",
            ColumnStateType.PartiallyConnected => "Underspecified",
            ColumnStateType.ReadyToCalculate => "Ready to Solve",
            ColumnStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public override string StatusColor => State switch
        {
            ColumnStateType.Created => "#CBD5E0",               // Gris
            ColumnStateType.PartiallyConnected => "#F6AD55",    // Naranja
            ColumnStateType.ReadyToCalculate => "#63B3ED",      // Azul
            ColumnStateType.Solved => "#34D399",                // Verde
            _ => "#CBD5E0"
        };

        public override List<ToolTipLegend> GetToolTipLegend()
        {
            List<ToolTipLegend> result = new();

            // Aquí en el futuro puedes agregar indicadores como "Reflux Ratio", "Number of Stages", etc.
            result.Add(new ToolTipLegend("Feeds", Feeds.Count.ToString()));
            result.Add(new ToolTipLegend("Side Draws", SideDraws.Count.ToString()));

            return result;
        }

        // ==============================================================================
        // 3. TOPOLOGÍA Y CONEXIONES
        // ==============================================================================
        public override void AttachConnection(string portName, IFacade connectedFacade)
        {
            var stream = connectedFacade as StreamSimulationFacade;
            if (stream == null) return;

            // Puertos Estáticos
            if (portName == "Overhead") OverheadStream = stream;
            else if (portName == "Bottoms") BottomsStream = stream;
            else if (portName == "Reflux") RefluxStream = stream;
            else if (portName == "ReboilerReturn") ReboilerReturnStream = stream;

            // Puertos Dinámicos
            else if (portName.StartsWith("Feed"))
            {
                Feeds[portName] = stream;
            }
            else if (portName.StartsWith("SideDraw"))
            {
                SideDraws[portName] = stream;
            }
        }

        public override void DetachConnection(string portName)
        {
            // Puertos Estáticos
            if (portName == "Overhead") OverheadStream = null;
            else if (portName == "Bottoms") BottomsStream = null;
            else if (portName == "Reflux") RefluxStream = null;
            else if (portName == "ReboilerReturn") ReboilerReturnStream = null;

            // Puertos Dinámicos
            else if (portName.StartsWith("Feed") && Feeds.ContainsKey(portName))
            {
                Feeds.Remove(portName);
            }
            else if (portName.StartsWith("SideDraw") && SideDraws.ContainsKey(portName))
            {
                SideDraws.Remove(portName);
            }
        }

        // ==============================================================================
        // 4. MOTOR DE CÁLCULO (PLACEHOLDER)
        // ==============================================================================
        protected override void CalculatedEquipment()
        {
            // TODO: Implementar el cálculo riguroso (Matrices tridiagonales, balances MESH, perfiles de T y P)
            // Por ahora se mantiene vacío esperando la lógica histórica del ingeniero.

            State = ColumnStateType.ReadyToCalculate;
        }
    }
}
