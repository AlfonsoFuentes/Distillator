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
            if (Inlet == null || Outlet == null) return PumpStateType.PartiallyConnected;
            if (!DeltaP.IsDefined || !Efficiency.IsDefined || Efficiency.Value.GetValue(PercentageUnits.Percentage) <= 0)
                return PumpStateType.ReadyToCalculate;
            if (Power?.IsDefined == true) return PumpStateType.Solved;
            return PumpStateType.ReadyToCalculate;
        }
        public IFacadeStream Inlet { get; private set; } = null!;
        public IFacadeStream Outlet { get; private set; } = null!;
        public Variable<PressureDrop> DeltaP { get; set; }
        public Variable<Power> Power { get; set; }
        public Variable<Percentage> Efficiency { get; set; }
        public void CalculatePower()
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

        }
        public void SetInlet(IFacadeStream inlet)
        {
            Inlet = inlet;
        }
        public void SetOutlet(IFacadeStream outlet)
        {
            Outlet = outlet;
        }
    }
    public class PumpPressureEquation : ISolverEquation
    {
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
            pump.CalculatePower();
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
            pump.CalculatePower();
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
            equipment.CalculatePower();
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
            equipment.CalculatePower();
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
            equipment.CalculatePower();
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
}
