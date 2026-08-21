using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;
using Shared.ProcessFlowDiagram.Designs;
using Shared.UnitOperations.HeatExchangers.Design;

namespace Shared.ProcessFlowDiagram.HeatExchangers
{
    public class HeatExchangerVisualElement : VisualElementBase  , IDesignableEquipment
    {
        private SolverHeatExchanger HX => Facade as SolverHeatExchanger ?? throw new InvalidOperationException("Facade must be SolverHeatExchanger");
        private readonly List<IEquipmentDesign> _designs = [];
        private IFacadeStream? TubeSideInlet => HX.ColdInlet;
        private IFacadeStream? TubeSideOutlet => HX.ColdOutlet;
        private IFacadeStream? ShellSideInlet => HX.HotInlet;
        private IFacadeStream? ShellSideOutlet => HX.HotOutlet;

        public override EquipmentType Type => EquipmentType.Exchanger;
        public override string Prefix => "E";
        public IReadOnlyList<IEquipmentDesign> Designs => _designs;

        public override bool AllowFreeRotation => true;
        public override bool AllowFlipHorizontal => true;
        public override bool AllowFlipVertical => true;
        public override bool IsResizable => false;

        // Constantes para nombres de puertos
        public const string PortTubeInName = "TubeIn";
        public const string PortTubeOutName = "TubeOut";
        public const string PortShellInName = "ShellIn";
        public const string PortCondensateOutName = "CondensateOut";
 

        // Propiedades fuertemente tipadas
        public EquipmentPort TubeInPort => Ports.First(p => p.Name == PortTubeInName);
        public EquipmentPort TubeOutPort => Ports.First(p => p.Name == PortTubeOutName);
        public EquipmentPort ShellInPort => Ports.First(p => p.Name == PortShellInName);
        public EquipmentPort CondensateOutPort => Ports.First(p => p.Name == PortCondensateOutName);


        public HeatExchangerVisualElement()
        {
            Width = 160;
            Height = 60;

            // Lado de tubos.
            AddPort(PortTubeInName, PortType.Inlet, 0, 35, PortDirection.Left);
            AddPort(PortTubeOutName, PortType.Outlet, 0, 15, PortDirection.Left);

            // Lado de coraza.
            AddPort(PortShellInName, PortType.Inlet, 30, -5, PortDirection.Top);
            AddPort(PortCondensateOutName, PortType.Outlet, 130, 65, PortDirection.Bottom);


            Facade = new SolverHeatExchanger("E-101")
            {
                Id = this.Id
            };
        }

        // ==============================================================================
        // IMPLEMENTACIÓN DE IEquipmentFacade
        // ==============================================================================
        public override IEnumerable<string> GetPortNames()
        {
            yield return PortTubeInName;
            yield return PortTubeOutName;
            yield return PortShellInName;
            yield return PortCondensateOutName;
      
        }

        public override IFacadeStream? GetConnectedStream(string portName)
        {
            return portName switch
            {
                PortTubeInName => TubeSideInlet,
                PortTubeOutName => TubeSideOutlet,
                PortShellInName => ShellSideInlet,
                PortCondensateOutName => ShellSideOutlet,
                _ => null
            };
        }

        public override void AttachConnection(string portName, IFacadeStream connectedFacade)
        {
            if (portName == PortTubeInName && HX.ColdInlet == null)
            {
                HX.SetColdInlet(connectedFacade);
            }
            else if (portName == PortTubeOutName && HX.ColdOutlet == null)
            {
                HX.SetColdOutlet(connectedFacade);
            }
            else if (portName == PortShellInName && HX.HotInlet == null)
            {
                HX.SetHotInlet(connectedFacade);
            }
            else if (portName == PortCondensateOutName && HX.HotOutlet == null)
            {
                HX.SetHotOutlet(connectedFacade);
            }
            // VaporVent: puerto visual sin lógica de solver por ahora
        }

        public override void DetachConnection(string portName)
        {
            if (portName == PortTubeInName)
            {
                HX.UnSetColdInlet();
            }
            else if (portName == PortTubeOutName)
            {
                HX.UnSetColdOutlet();
            }
            else if (portName == PortShellInName)
            {
                HX.UnSetHotInlet();
            }
            else if (portName == PortCondensateOutName)
            {
                HX.UnSetHotOutlet();
            }
            // VaporVent: puerto visual sin lógica de solver por ahora
        }

        public override string StatusColor => HX.State switch
        {
            HeatExchangerStateType.Created => "#CBD5E0",
            HeatExchangerStateType.PartiallyConnected => "#F6AD55",
            HeatExchangerStateType.ReadyToCalculate => "#63B3ED",
            HeatExchangerStateType.Solved => "#34D399",
            _ => "#CBD5E0"
        };

        public override string StatusText => HX.State switch
        {
            HeatExchangerStateType.Created => "Ready",
            HeatExchangerStateType.PartiallyConnected => "Underspecified",
            HeatExchangerStateType.ReadyToCalculate => "Ready to Solve",
            HeatExchangerStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public override List<ToolTipLegend> GetToolTipData()
        {
            return new List<ToolTipLegend>
        {
            new("ΔP Shell Side", HX.DeltaPHot.ToUiString()),
            new("ΔP Tube Side", HX.DeltaPCold.ToUiString()),
            new("Q Transfer", HX.TransferHeat.ToUiString())
        };
        }
        public IEquipmentDesign CreateDesign()
        {
            return CreateDesign(ShellAndTubeCalculationStandard.Kern);
        }

        public IEquipmentDesign CreateDesign(ShellAndTubeCalculationStandard calculationStandard)
        {
            var variables = new ShellAndTubeDesignVariables();
            variables.CalculationStandard = calculationStandard;
            var result = CalculateDesign(variables);
            var design = new EquipmentDesign
            {
                Id = Guid.NewGuid(),
                Name = $"{Name}-D{_designs.Count + 1}",
                Variables = variables,
                Result = result
            };

            _designs.Add(design);
            return design;
        }

        public IEquipmentDesign RecalculateDesign(IEquipmentDesign design)
        {
            ArgumentNullException.ThrowIfNull(design);

            var variables = GetShellAndTubeVariables(design);
            var result = CalculateDesign(variables);
            var recalculatedDesign = new EquipmentDesign
            {
                Id = design.Id,
                Name = design.Name,
                Variables = variables,
                Result = result
            };

            var index = _designs.FindIndex(existingDesign => existingDesign.Id == design.Id);
            if (index >= 0)
            {
                _designs[index] = recalculatedDesign;
            }

            return recalculatedDesign;
        }

        private IDesignResult CalculateDesign(ShellAndTubeDesignVariables variables)
        {
            var request = CreateDesignRequest(variables);
            var factory = new ShellAndTubeDesignFactory();
            var designer = factory.Create(request);

            return designer.Calculate();
        }

        private HeatExchangerDesignRequest CreateDesignRequest(ShellAndTubeDesignVariables variables)
        {
            return new HeatExchangerDesignRequest
            {
                HeatExchangerType = HeatExchangerType.ShellAndTube,
                Variables = variables,
                ShellSideInlet = CreateStreamSnapshot(ShellSideInlet, PortShellInName),
                ShellSideOutlet = CreateStreamSnapshot(ShellSideOutlet, PortCondensateOutName),
                TubeSideInlet = CreateStreamSnapshot(TubeSideInlet, PortTubeInName),
                TubeSideOutlet = CreateStreamSnapshot(TubeSideOutlet, PortTubeOutName),
                Equipment = this
            };
        }

        private static ShellAndTubeDesignVariables GetShellAndTubeVariables(IEquipmentDesign design)
        {
            if (design.Variables is ShellAndTubeDesignVariables variables)
            {
                return variables;
            }

            throw new InvalidOperationException("The selected design does not contain shell and tube design variables.");
        }

        private static HeatExchangerStreamSnapshot CreateStreamSnapshot(IFacadeStream? stream, string portName)
        {
            if (stream is null)
            {
                throw new InvalidOperationException($"Cannot create a design because port '{portName}' is not connected.");
            }

            return new HeatExchangerStreamSnapshot
            {
                Stream = stream
            };
        }
    }
   
}
