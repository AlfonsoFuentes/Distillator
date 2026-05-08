using Shared.MatrixSolvers;
using Shared.ProcessFlowDiagram;
using Shared.Thermodynamics.ControlledVariables;
using Shared.UnitOperations.Basiss;
using Shared.UnitOperations.Streams;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.UnitOperations.Vessels
{
   

    public enum VesselStateType { Created, PartiallyConnected, Solved }

    public class VesselSimulationFacade : EquipmentFacade
    {
        public VesselStateType State { get; set; } = VesselStateType.Created;

        // --- Topología Dinámica ---
        public Dictionary<string, StreamSimulationFacade> InletStreams { get; } = new();
        public Dictionary<string, StreamSimulationFacade> OutletStreams { get; } = new();

        public override string StatusColor => State switch
        {
            VesselStateType.Created => "#CBD5E0",
            VesselStateType.PartiallyConnected => "#F6AD55",
            VesselStateType.Solved => "#34D399",
            _ => "#CBD5E0"
        };

        public override string StatusText => State switch
        {
            VesselStateType.Created => "Ready",
            VesselStateType.PartiallyConnected => "Underspecified",
            VesselStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public override List<ToolTipLegend> GetToolTipLegend()
        {
            return new List<ToolTipLegend>();
        }

        public override void AttachConnection(string portName, IFacade connectedFacade)
        {
            var stream = connectedFacade as StreamSimulationFacade;
            if (stream == null) return;

            if (portName.StartsWith("Inlet")) InletStreams[portName] = stream;
            else if (portName.StartsWith("Outlet")) OutletStreams[portName] = stream;

    
        }

        public override void DetachConnection(string portName)
        {
            if (portName.StartsWith("Inlet")) InletStreams.Remove(portName);
            else if (portName.StartsWith("Outlet")) OutletStreams.Remove(portName);

      
        }

        protected override void CalculatedEquipment()
        {
            // Placeholder para futuros cálculos de inventario o balance
            State = VesselStateType.Solved;
        }
        public override void BuildEquations(EquationSystem eqs)
        {

        }

        public override IEnumerable<INewVariable> GetSolverVariables()
        {
            return null!;
        }
    }
    public class VesselSimulationFacade2 : EquipmentFacade2
    {
        public VesselStateType State { get; set; } = VesselStateType.Created;
        public Dictionary<string, IStreamFacade> InletStreams { get; } = new();
        public Dictionary<string, IStreamFacade> OutletStreams { get; } = new();

        public override string StatusText => State switch
        {
            VesselStateType.Created => "Ready",
            VesselStateType.PartiallyConnected => "Underspecified",
            VesselStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public override string StatusColor => State switch
        {
            VesselStateType.Created => "#CBD5E0",
            VesselStateType.PartiallyConnected => "#F6AD55",
            VesselStateType.Solved => "#34D399",
            _ => "#CBD5E0"
        };

        public override List<ToolTipLegend> GetToolTipLegend() => new();

        public override void AttachConnection(string portName, IStreamFacade connectedFacade)
        {
            if (portName.StartsWith("Inlet")) InletStreams[portName] = connectedFacade;
            else if (portName.StartsWith("Outlet")) OutletStreams[portName] = connectedFacade;
        }

        public override void DetachConnection(string portName)
        {
            if (portName.StartsWith("Inlet")) InletStreams.Remove(portName);
            else if (portName.StartsWith("Outlet")) OutletStreams.Remove(portName);
        }

        //public override void BuildEquations(EquationSystem eqs)
        //{
        //    // 🔥 Vessel ideal: mezcla perfecta, misma T, P, composición en todas las salidas
        //    var allStreams = InletStreams.Values.Concat(OutletStreams.Values).Where(s => s != null).ToList();
        //    if (allStreams.Count < 2) return;

        //    var reference = allStreams.First();
        //    foreach (var stream in allStreams.Skip(1))
        //    {
        //        eqs.AddEquation(x => x[stream.Temperature.Index] - x[reference.Temperature.Index], EquationType.Model, $"Vessel T {stream.Name}");
        //        eqs.AddEquation(x => x[stream.Pressure.Index] - x[reference.Pressure.Index], EquationType.Model, $"Vessel P {stream.Name}");
        //        eqs.AddEquation(x => x[stream.MolarEnthalpy.Index] - x[reference.MolarEnthalpy.Index], EquationType.Model, $"Vessel H {stream.Name}");
        //    }

        //    // 🔥 Balance por componente si hay composición
        //    if (reference.StreamComposition?.Value?.Components.Count > 0)
        //    {
        //        foreach (var stream in allStreams.Skip(1))
        //        {
        //            if (stream.StreamComposition?.Value == null) continue;
        //            var compsRef = reference.StreamComposition.Value.Components;
        //            var compsStr = stream.StreamComposition.Value.Components;
        //            for (int i = 0; i < compsRef.Count && i < compsStr.Count; i++)
        //            {
        //                eqs.AddEquation(
        //                    x => x[compsStr[i].MolarFlowSolver.Index] - x[compsRef[i].MolarFlowSolver.Index],
        //                    EquationType.Model,
        //                    $"Vessel component balance {compsRef[i].ComponentName}"
        //                );
        //            }
        //        }
        //    }
        //}

        //public override IEnumerable<INewVariable> GetSolverVariables()
        //{
        //    foreach (var stream in InletStreams.Values.Concat(OutletStreams.Values).Where(s => s != null))
        //    {
        //        yield return stream.Temperature;
        //        yield return stream.Pressure;
        //        yield return stream.MolarEnthalpy;
        //        if (stream.StreamComposition?.Value?.Components != null)
        //            foreach (var c in stream.StreamComposition.Value.Components)
        //                yield return c.MolarFlowSolver;
        //    }
        //}
    }
}
