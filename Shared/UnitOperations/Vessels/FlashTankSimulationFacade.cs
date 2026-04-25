using Shared.ProcessFlowDiagram;
using Shared.UnitOperations.Basiss;
using Shared.UnitOperations.Streams;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.UnitOperations.Vessels
{
  
    public enum FlashTankStateType { Created, PartiallyConnected, ReadyToCalculate, Solved }

    public class FlashTankSimulationFacade : EquipmentFacade
    {
        public FlashTankStateType State { get; set; } = FlashTankStateType.Created;

        // --- Topología Estática ---
        public StreamSimulationFacade? VaporStream { get; private set; }
        public StreamSimulationFacade? Liquid1Stream { get; private set; }
        public StreamSimulationFacade? Liquid2Stream { get; private set; }

        // --- Topología Dinámica ---
        public Dictionary<string, StreamSimulationFacade> Feeds { get; } = new();
        public Dictionary<string, StreamSimulationFacade> ExtraProducts { get; } = new();

        public override string StatusColor => State switch
        {
            FlashTankStateType.Created => "#CBD5E0",
            FlashTankStateType.PartiallyConnected => "#F6AD55",
            FlashTankStateType.ReadyToCalculate => "#63B3ED",
            FlashTankStateType.Solved => "#34D399",
            _ => "#CBD5E0"
        };

        public override string StatusText => State switch
        {
            FlashTankStateType.Created => "Ready",
            FlashTankStateType.PartiallyConnected => "Underspecified",
            FlashTankStateType.ReadyToCalculate => "Ready to Solve",
            FlashTankStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public override List<ToolTipLegend> GetToolTipLegend()
        {
            return new List<ToolTipLegend>(); // Se llenará cuando tengamos cálculos termodinámicos
        }

        public override void AttachConnection(string portName, IFacade connectedFacade)
        {
            var stream = connectedFacade as StreamSimulationFacade;
            if (stream == null) return;

            if (portName == "Vapor") VaporStream = stream;
            else if (portName == "Liquid_1") Liquid1Stream = stream;
            else if (portName == "Liquid_2") Liquid2Stream = stream;
            else if (portName.StartsWith("Feed") || portName.StartsWith("ExtraFeed")) Feeds[portName] = stream;
            else if (portName.StartsWith("ExtraProduct")) ExtraProducts[portName] = stream;
        }

        public override void DetachConnection(string portName)
        {
            if (portName == "Vapor") VaporStream = null;
            else if (portName == "Liquid_1") Liquid1Stream = null;
            else if (portName == "Liquid_2") Liquid2Stream = null;
            else if (Feeds.ContainsKey(portName)) Feeds.Remove(portName);
            else if (ExtraProducts.ContainsKey(portName)) ExtraProducts.Remove(portName);
        }

        protected override void CalculatedEquipment()
        {
            // Placeholder para el cálculo Flash Isentálpico o Isotérmico
            State = FlashTankStateType.ReadyToCalculate;
        }
    }
}
