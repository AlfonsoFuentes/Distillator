using Shared.SolverConsecutive.Equipments.Columns;
using Shared.SolverQwen.Stream;
using UnitSystem;

namespace Shared.SolverConsecutive.Equipments
{
    public enum PumpStateType
    {
        Created,
        PartiallyConnected,
        ReadyToCalculate,
        Solved
    }
    public class SolverPump : SolverEquipmentBase
    {
        public PumpStateType State => GetState();
        private PumpStateType GetState()
        {
            // 1. Topología
            if (Inlet == null || Outlet == null)
                return PumpStateType.PartiallyConnected;

            // 2. Especificaciones de entrada
            bool hasEfficiency = Efficiency.IsDefined && Efficiency.Value.GetValue(PercentageUnits.Percentage) > 0;
            if (!DeltaP.IsDefined || !hasEfficiency)
                return PumpStateType.ReadyToCalculate;

            // 3. Resolución final (El método CalculatePower se ejecutó exitosamente)
            if (Power != null && Power.IsDefined)
                return PumpStateType.Solved;

            return PumpStateType.ReadyToCalculate;
        }
        public IFacadeStream Inlet { get; private set; } = null!;
        public IFacadeStream Outlet { get; private set; } = null!;
        public Variable<PressureDrop> DeltaP { get; set; }
        public Variable<Power> Power { get; set; }
        public Variable<Percentage> Efficiency { get; set; }
        public override Task PostSolveAsync()
        {
            // Aquí calculas el Cv, Power, o cualquier KPI post-convergencia
            CalculatePower();
            return Task.CompletedTask;
        }
        private void CalculatePower()
        {
            Power.Clear(VariableDefinedBy.Equipment);

            if (Inlet == null || Outlet == null) return;

            var volumetricflow = Inlet.VolumetricFlow;
            if (!volumetricflow.IsDefined || !DeltaP.IsDefined || !Efficiency.IsDefined) return;

            var flow = volumetricflow.Value.GetValue(VolumetricFlowUnits.m3_sg);
            var head = DeltaP.Value.GetValue(PressureDropUnits.Pascal);
            var eff = Efficiency.Value.GetValue(PercentageUnits.Percentage) / 100;
            if (eff <= 0) return;
            var power = flow * head / eff;

            Power.SetValue(new(power, PowerUnits.Watt), VariableDefinedBy.Equipment);

        }

        public override List<ISolverEquation> Equations => GetEquations().ToList();
        public SolverPump()
        {

            DeltaP = new Variable<PressureDrop>(new PressureDrop(0, PressureDropUnits.Pascal), PressureDropUnits.Bar, 100000);
            Power = new Variable<Power>(new UnitSystem.Power(0, PowerUnits.KiloWatt), PowerUnits.KiloWatt, 1);
            Efficiency = new Variable<Percentage>(new Percentage(50, PercentageUnits.Percentage), PercentageUnits.Percentage, 1);
        }
        IEnumerable<ISolverEquation> GetEquations()
        {
            yield return new PumpPressureEquation(this);
            yield return new PumpConcentrationEquation(this);
            yield return new PumpMassBalanceEquation(this);
            yield return new PumpEnthalpyEquation(this);
            yield return new PumpMassEnergyBalanceEquation(this);
            // Backup legacy: la V2 de specifications usa PumpMassBalanceEquation regular.
            // yield return new PumpMassBalanceEquationSpec(this);

        }
        public void SetInlet(IFacadeStream inlet)
        {
            if (inlet != null)
            {
                Inlets.Add(inlet);
                Inlet = inlet;
                Inlet.EquipmentOutlet = this;
              
            }
           
        }
        public void UnSetInlet()
        {
            if (Inlet == null) return;
            Inlets.Remove(Inlet);
            Inlet.EquipmentOutlet = null!;
            Inlet = null!;
        }
        public void SetOutlet(IFacadeStream outlet)
        {
            if (outlet != null)
            {
                Outlets.Add(outlet);
                Outlet = outlet;
                Outlet.EquipmentInlet = this;

            }
        }
        public void UnSetOutlet()
        {
            if (Outlet == null) return;
            Outlets.Remove(Outlet);
            Outlet.EquipmentInlet = null!;
            Outlet = null!;
        }

        //public override IEnumerable<ISolverEquipment> GetEquipmentInlets(IFacadeStream stream)
        //{
        //    if (stream == null) yield break;
        //    if (Outlet == stream )
        //    {
        //        yield return Inlet!.EquipmentInlet;
             
        //    }


        //}
        //public override IEnumerable<ISolverEquipment> GetEquipmentOutlets(IFacadeStream stream)
        //{
        //    if (stream == null) yield break;

        //    if (Inlet == stream )
        //    {
        //        yield return Outlet!.EquipmentOutlet;
               
        //    }


        //}
    }
    public class PumpPressureEquation : ISolverEquation
    {
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        SolverPump pump;
        public PumpPressureEquation(SolverPump _pump)
        {
            pump = _pump;
        }
        public string Name => $"{EquationType} - {pump.Name}";

        public SolverEquationType EquationType => SolverEquationType.Pressure;

        public List<double> Residuals => GetResiduals();

        public List<IVariable> Variables => GetVariables();
        List<double> GetResiduals()
        {
            List<double> _residuals = new();
            if (pump.Inlet == null || pump.Outlet == null) return _residuals;

            double inletP = pump.Inlet.Pressure.GetSolverValue();
            double outletP = pump.Outlet.Pressure.GetSolverValue();
            double deltaP = pump.DeltaP.GetSolverValue();
            _residuals.Add(inletP + deltaP - outletP);
           
            return _residuals;
        }
        List<IVariable> GetVariables()
        {
            List<IVariable> _variables = new();
            if (pump.Inlet == null || pump.Outlet == null) return _variables;
            _variables.Add(pump.DeltaP);
            _variables.Add(pump.Inlet.Pressure);
            _variables.Add(pump.Outlet.Pressure);

            return _variables;
        }
    }
    public class PumpConcentrationEquation : ISolverEquation
    {
        SolverPump pump;
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        public PumpConcentrationEquation(SolverPump _pump)
        {
            pump = _pump;
        }
        public string Name => $"{EquationType} - {pump.Name}";
        public SolverEquationType EquationType => SolverEquationType.Concentration;
        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();
        List<double> GetResiduals()
        {
            List<double> _residuals = new();
            if (pump.Inlet == null || pump.Outlet == null) return _residuals;
            for (int i = 0; i < pump.Inlet.Composition.Components.Count; i++)
            {
                double inletFlow = pump.Inlet.Composition.Components[i].MassFraction.GetSolverValue();
                double outletFlow = pump.Outlet.Composition.Components[i].MassFraction.GetSolverValue();
                _residuals.Add(inletFlow - outletFlow);
            }
        
            return _residuals;
        }
        List<IVariable> GetVariables()
        {
            List<IVariable> _variables = new();
            if (pump.Inlet == null || pump.Outlet == null) return _variables;
            for (int i = 0; i < pump.Inlet.Composition.Components.Count; i++)
            {
                _variables.Add(pump.Inlet.Composition.Components[i].MassFraction);
                _variables.Add(pump.Outlet.Composition.Components[i].MassFraction);
            }
            return _variables;
        }
    }
    public class PumpMassBalanceEquation : ISolverEquation
    {
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        SolverPump equipment;
        public PumpMassBalanceEquation(SolverPump _pump)
        {
            equipment = _pump;
        }
        public string Name => $"{EquationType} - {equipment.Name}";
        public SolverEquationType EquationType => SolverEquationType.MassBalance;
        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();
        List<double> GetResiduals()
        {
            List<double> _residuals = new();
            if (equipment.Inlet == null || equipment.Outlet == null) return _residuals;
            double inletFlow = equipment.Inlet.MassFlow.GetSolverValue();
            double outletFlow = equipment.Outlet.MassFlow.GetSolverValue();
            _residuals.Add(inletFlow - outletFlow);
           
            return _residuals;
        }
        List<IVariable> GetVariables()
        {
            List<IVariable> _variables = new();
            if (equipment.Inlet == null || equipment.Outlet == null) return _variables;
            _variables.Add(equipment.Inlet.MassFlow);
            _variables.Add(equipment.Outlet.MassFlow);
            return _variables;
        }
    }
    public class PumpEnthalpyEquation : ISolverEquation
    {
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        SolverPump equipment;
        public PumpEnthalpyEquation(SolverPump _pump)
        {
            equipment = _pump;
        }
        public string Name => $"{EquationType} - {equipment.Name}";
        public SolverEquationType EquationType => SolverEquationType.Enthalpy;
        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();
        List<double> GetResiduals()
        {
            List<double> _residuals = new();
            if (equipment.Inlet == null || equipment.Outlet == null) return _residuals;
            double inletH = equipment.Inlet.MassEnthalpy.GetSolverValue();
            double outletH = equipment.Outlet.MassEnthalpy.GetSolverValue();

            _residuals.Add(inletH - outletH);
            
            return _residuals;
        }
        List<IVariable> GetVariables()
        {
            List<IVariable> _variables = new();
            if (equipment.Inlet == null || equipment.Outlet == null) return _variables;
            _variables.Add(equipment.Inlet.MassEnthalpy);
            _variables.Add(equipment.Outlet.MassEnthalpy);

            return _variables;
        }
    }
    public class PumpMassEnergyBalanceEquation : ISolverEquation
    {
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        SolverPump equipment;
        public PumpMassEnergyBalanceEquation(SolverPump _pump)
        {
            equipment = _pump;
        }
        public string Name => $"{EquationType} - {equipment.Name}";
        public SolverEquationType EquationType => SolverEquationType.MassEnergyBalance;
        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();
        List<double> GetResiduals()
        {
            List<double> _residuals = new();
            if (equipment.Inlet == null || equipment.Outlet == null) return _residuals;
            double inletFlow = equipment.Inlet.MassFlow.GetSolverValue();
            double outletFlow = equipment.Outlet.MassFlow.GetSolverValue();
            double inletH = equipment.Inlet.MassEnthalpy.GetSolverValue();
            double outletH = equipment.Outlet.MassEnthalpy.GetSolverValue();
            _residuals.Add(inletFlow * inletH - outletFlow * outletH);
           
            return _residuals;
        }
        List<IVariable> GetVariables()
        {
            List<IVariable> _variables = new();
            if (equipment.Inlet == null || equipment.Outlet == null) return _variables;
            _variables.Add(equipment.Inlet.MassFlow);
            _variables.Add(equipment.Outlet.MassFlow);
            _variables.Add(equipment.Inlet.MassEnthalpy);
            _variables.Add(equipment.Outlet.MassEnthalpy);
            return _variables;
        }
    }

    /*
    // Backup legacy: la V2 de specifications usa PumpMassBalanceEquation regular.
    public class PumpMassBalanceEquationSpec : ISpecSolverEquation
    {
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Spec;
        SolverPump equipment;
        public PumpMassBalanceEquationSpec(SolverPump _pump)
        {
            equipment = _pump;
        }
        public string Name => $"{EquationType} - {equipment.Name}";
        public SolverEquationType EquationType => SolverEquationType.MassBalance;
        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();
        List<double> GetResiduals()
        {
            List<double> _residuals = new();
            if (equipment.Inlet == null || equipment.Outlet == null) return _residuals;
            double inletFlow = equipment.Inlet.MassFlow.GetSolverValue();
            double outletFlow = equipment.Outlet.MassFlow.GetSolverValue();
            _residuals.Add(inletFlow - outletFlow);

            return _residuals;
        }
        List<IVariable> GetVariables()
        {
            List<IVariable> _variables = new();
            if (equipment.Inlet == null || equipment.Outlet == null) return _variables;
            _variables.Add(equipment.Inlet.MassFlow);
            _variables.Add(equipment.Outlet.MassFlow);
            return _variables;
        }
        public IEnumerable<IFacadeStream> AsociatedStreams
        {
            get
            {
                if (equipment.Inlet != null)
                    yield return equipment.Inlet;

                if (equipment.Outlet != null)
                    yield return equipment.Outlet;
            }
        }
    }
    */
}
