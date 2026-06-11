namespace Shared.UnitOperations.Vessels
{
  


    //public class FlashTankSimulationFacade : EquipmentFacade
    //{
    //    public FlashTankStateType State { get; set; } = FlashTankStateType.Created;

    //    // --- Topología Estática ---
    //    public StreamSimulationFacade? VaporStream { get; private set; }
    //    public StreamSimulationFacade? Liquid1Stream { get; private set; }
    //    public StreamSimulationFacade? Liquid2Stream { get; private set; }

    //    // --- Topología Dinámica ---
    //    public Dictionary<string, StreamSimulationFacade> Feeds { get; } = new();
    //    public Dictionary<string, StreamSimulationFacade> ExtraProducts { get; } = new();

    //    public override string StatusColor => State switch
    //    {
    //        FlashTankStateType.Created => "#CBD5E0",
    //        FlashTankStateType.PartiallyConnected => "#F6AD55",
    //        FlashTankStateType.ReadyToCalculate => "#63B3ED",
    //        FlashTankStateType.Solved => "#34D399",
    //        _ => "#CBD5E0"
    //    };

    //    public override string StatusText => State switch
    //    {
    //        FlashTankStateType.Created => "Ready",
    //        FlashTankStateType.PartiallyConnected => "Underspecified",
    //        FlashTankStateType.ReadyToCalculate => "Ready to Solve",
    //        FlashTankStateType.Solved => "Converged",
    //        _ => "Unknown"
    //    };

    //    public override List<ToolTipLegend> GetToolTipLegend()
    //    {
    //        return new List<ToolTipLegend>(); // Se llenará cuando tengamos cálculos termodinámicos
    //    }

    //    public override void AttachConnection(string portName, IFacade connectedFacade)
    //    {
    //        var stream = connectedFacade as StreamSimulationFacade;
    //        if (stream == null) return;

    //        if (portName == "Vapor") VaporStream = stream;
    //        else if (portName == "Liquid_1") Liquid1Stream = stream;
    //        else if (portName == "Liquid_2") Liquid2Stream = stream;
    //        else if (portName.StartsWith("Feed") || portName.StartsWith("ExtraFeed")) Feeds[portName] = stream;
    //        else if (portName.StartsWith("ExtraProduct")) ExtraProducts[portName] = stream;
    //    }

    //    public override void DetachConnection(string portName)
    //    {
    //        if (portName == "Vapor") VaporStream = null;
    //        else if (portName == "Liquid_1") Liquid1Stream = null;
    //        else if (portName == "Liquid_2") Liquid2Stream = null;
    //        else if (Feeds.ContainsKey(portName)) Feeds.Remove(portName);
    //        else if (ExtraProducts.ContainsKey(portName)) ExtraProducts.Remove(portName);
    //    }

    //    protected override void CalculatedEquipment()
    //    {
    //        // Placeholder para el cálculo Flash Isentálpico o Isotérmico
    //        State = FlashTankStateType.ReadyToCalculate;
    //    }
    //    public override void BuildEquations(EquationSystem eqs)
    //    {

    //    }

    //    public override IEnumerable<INewVariable> GetSolverVariables()
    //    {
    //        return null!;
    //    }
    //}
    //public class FlashTankSimulationFacade2 : EquipmentFacade2
    //{
    //    public FlashTankStateType State { get; set; } = FlashTankStateType.Created;
    //    public IStreamFacade2? VaporStream { get; private set; }
    //    public IStreamFacade2? LiquidStream { get; private set; }
    //    public Dictionary<string, IStreamFacade2> Feeds { get; } = new();

    //    public override string StatusText => State switch
    //    {
    //        FlashTankStateType.Created => "Ready",
    //        FlashTankStateType.PartiallyConnected => "Underspecified",
    //        FlashTankStateType.ReadyToCalculate => "Ready to Solve",
    //        FlashTankStateType.Solved => "Converged",
    //        _ => "Unknown"
    //    };

    //    public override string StatusColor => State switch
    //    {
    //        FlashTankStateType.Created => "#CBD5E0",
    //        FlashTankStateType.PartiallyConnected => "#F6AD55",
    //        FlashTankStateType.ReadyToCalculate => "#63B3ED",
    //        FlashTankStateType.Solved => "#34D399",
    //        _ => "#CBD5E0"
    //    };

    //    public override List<ToolTipLegend> GetToolTipLegend() => new();

    //    public override void AttachConnection(string portName, IStreamFacade2 connectedFacade)
    //    {
    //        if (portName == "Vapor") VaporStream = connectedFacade;
    //        else if (portName == "Liquid") LiquidStream = connectedFacade;
    //        else if (portName.StartsWith("Feed")) Feeds[portName] = connectedFacade;
    //    }

    //    public override void DetachConnection(string portName)
    //    {
    //        if (portName == "Vapor") VaporStream = null;
    //        else if (portName == "Liquid") LiquidStream = null;
    //        else if (Feeds.ContainsKey(portName)) Feeds.Remove(portName);
    //    }

    //    //public override void BuildEquations(EquationSystem eqs)
    //    //{
    //    //    if (VaporStream == null || LiquidStream == null || Feeds.Count == 0) return;

    //    //    // 🔥 Balance por componente: Σnᵢ_feed = nᵢ_vapor + nᵢ_liquid
    //    //    var firstFeed = Feeds.Values.First();
    //    //    if (firstFeed.StreamComposition?.Value?.Components.Count > 0)
    //    //    {
    //    //        var compsFeed = firstFeed.StreamComposition.Value.Components;
    //    //        var compsVap = VaporStream.StreamComposition?.Value?.Components;
    //    //        var compsLiq = LiquidStream.StreamComposition?.Value?.Components;
    //    //        if (compsVap != null && compsLiq != null)
    //    //        {
    //    //            for (int i = 0; i < compsFeed.Count; i++)
    //    //            {
    //    //                eqs.AddEquation(
    //    //                    x => (x[compsVap[i].MolarFlowSolver.Index] + x[compsLiq[i].MolarFlowSolver.Index]) - x[compsFeed[i].MolarFlowSolver.Index],
    //    //                    EquationType.Model,
    //    //                    $"Flash component balance {compsFeed[i].ComponentName}"
    //    //                );
    //    //            }
    //    //        }
    //    //    }

    //    //    // 🔥 Equilibrio termodinámico: misma T, P en vapor y líquido
    //    //    eqs.AddEquation(x => x[VaporStream.Temperature.Index] - x[LiquidStream.Temperature.Index], EquationType.Model, "Flash T equilibrium");
    //    //    eqs.AddEquation(x => x[VaporStream.Pressure.Index] - x[LiquidStream.Pressure.Index], EquationType.Model, "Flash P equilibrium");

    //    //    // 🔥 Flash isentálpico: H_feed = VF·H_vap + (1-VF)·H_liq
    //    //    var Hfeed = firstFeed.MolarEnthalpy;
    //    //    var Hvap = VaporStream.MolarEnthalpy;
    //    //    var Hliq = LiquidStream.MolarEnthalpy;
    //    //    var VF = VaporStream.VaporFraction; // VF = 1 para vapor puro, pero usamos como variable de fracción de vapor del flash
    //    //    eqs.AddEquation(
    //    //        x => x[Hfeed.Index] - (x[VF.Index] * x[Hvap.Index] + (1 - x[VF.Index]) * x[Hliq.Index]),
    //    //        EquationType.Model,
    //    //        "Flash isenthalpic balance"
    //    //    );
    //    //}

    //    //public override IEnumerable<INewVariable> GetSolverVariables()
    //    //{
    //    //    foreach (var feed in Feeds.Values)
    //    //    {
    //    //        yield return feed.Temperature;
    //    //        yield return feed.Pressure;
    //    //        yield return feed.MolarEnthalpy;
    //    //        if (feed.StreamComposition?.Value?.Components != null)
    //    //            foreach (var c in feed.StreamComposition.Value.Components)
    //    //                yield return c.MolarFlowSolver;
    //    //    }
    //    //    if (VaporStream != null)
    //    //    {
    //    //        yield return VaporStream.Temperature;
    //    //        yield return VaporStream.Pressure;
    //    //        yield return VaporStream.MolarEnthalpy;
    //    //        yield return VaporStream.VaporFraction;
    //    //        if (VaporStream.StreamComposition?.Value?.Components != null)
    //    //            foreach (var c in VaporStream.StreamComposition.Value.Components)
    //    //                yield return c.MolarFlowSolver;
    //    //    }
    //    //    if (LiquidStream != null)
    //    //    {
    //    //        yield return LiquidStream.Temperature;
    //    //        yield return LiquidStream.Pressure;
    //    //        yield return LiquidStream.MolarEnthalpy;
    //    //        if (LiquidStream.StreamComposition?.Value?.Components != null)
    //    //            foreach (var c in LiquidStream.StreamComposition.Value.Components)
    //    //                yield return c.MolarFlowSolver;
    //    //    }
    //    //}
    //}
}
