using Shared.SolverQwen.Stream;
using UnitSystem;

namespace Shared.SolverConsecutive.Equipments
{
    public class SolverValve : SolverEquipmentBase
    {
        public IFacadeStream Inlet { get; private set; } = null!;
        public IFacadeStream Outlet { get; private set; } = null!;
        public NewVariable<PressureDrop> DeltaP { get; }
        public override string Name { get; }
        public override List<ISolverEquation> Equations => GetEquations().ToList();
        public SolverValve(string name)
        {
            Name = name;
            DeltaP = new NewVariable<PressureDrop>(new PressureDrop(0, PressureDropUnits.Pascal), PressureDropUnits.Bar, 100000);
        }
        IEnumerable<ISolverEquation> GetEquations()
        {
            yield return new ValvePressureEquation(this);
            yield return new ValveConcentrationEquation(this);
            yield return new ValveMassBalanceEquation(this);
            yield return new ValveEnthalpyEquation(this);
            yield return new ValveMassEnergyBalanceEquation(this);
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
    public class ValvePressureEquation : ISolverEquation
    {
        SolverValve valve;
        public ValvePressureEquation(SolverValve _valve)
        {
            valve = _valve;
        }
        public string Name => $"{EquationType} - {valve.Name}";
        public SolverEquationType EquationType => SolverEquationType.Pressure;
        public List<double> Residuals => GetResiduals();
        public List<INewVariable> Variables => GetVariables();
        List<double> GetResiduals()
        {
            List<double> _residuals = new();
            if (valve.Inlet == null || valve.Outlet == null) return _residuals;
            double inletP = valve.Inlet.Pressure.GetSolverValue();
            double outletP = valve.Outlet.Pressure.GetSolverValue();
            double deltaP = valve.DeltaP.GetSolverValue();
            _residuals.Add(inletP - deltaP - outletP);
            return _residuals;
        }
        List<INewVariable> GetVariables()
        {
            List<INewVariable> _variables = new();
            if (valve.Inlet == null || valve.Outlet == null) return _variables;
            _variables.Add(valve.DeltaP);
            _variables.Add(valve.Inlet.Pressure);
            _variables.Add(valve.Outlet.Pressure);
            return _variables;
        }
    }
    public class ValveConcentrationEquation : ISolverEquation
    {
        SolverValve equipment;
        public ValveConcentrationEquation(SolverValve _valve)
        {
            equipment = _valve;
        }
        public string Name => $"{EquationType} - {equipment.Name}";
        public SolverEquationType EquationType => SolverEquationType.Concentration;
        public List<double> Residuals => GetResiduals();
        public List<INewVariable> Variables => GetVariables();
        List<double> GetResiduals()
        {
            List<double> _residuals = new();
            if (equipment.Inlet == null || equipment.Outlet == null) return _residuals;
            for (int i = 0; i < equipment.Inlet.Composition.Components.Count; i++)
            {
                double inletFlow = equipment.Inlet.Composition.Components[i].MassFraction.GetSolverValue();
                double outletFlow = equipment.Outlet.Composition.Components[i].MassFraction.GetSolverValue();
                _residuals.Add(inletFlow - outletFlow);
            }
            return _residuals;
        }
        List<INewVariable> GetVariables()
        {
            List<INewVariable> _variables = new();
            if (equipment.Inlet == null || equipment.Outlet == null) return _variables;
            for (int i = 0; i < equipment.Inlet.Composition.Components.Count; i++)
            {
                _variables.Add(equipment.Inlet.Composition.Components[i].MassFraction);
                _variables.Add(equipment.Outlet.Composition.Components[i].MassFraction);
            }
            return _variables;
        }
    }

    public class ValveMassBalanceEquation : ISolverEquation
    {
        SolverValve equipment;
        public ValveMassBalanceEquation(SolverValve _valve)
        {
            equipment = _valve;
        }
        public string Name => $"{EquationType} - {equipment.Name}";
        public SolverEquationType EquationType => SolverEquationType.MassBalance;
        public List<double> Residuals => GetResiduals();
        public List<INewVariable> Variables => GetVariables();
        List<double> GetResiduals()
        {
            List<double> _residuals = new();
            if (equipment.Inlet == null || equipment.Outlet == null) return _residuals;
            double inletFlow = equipment.Inlet.MassFlow.GetSolverValue();
            double outletFlow = equipment.Outlet.MassFlow.GetSolverValue();
            _residuals.Add(inletFlow - outletFlow);
            return _residuals;
        }
        List<INewVariable> GetVariables()
        {
            List<INewVariable> _variables = new();
            if (equipment.Inlet == null || equipment.Outlet == null) return _variables;
            _variables.Add(equipment.Inlet.MassFlow);
            _variables.Add(equipment.Outlet.MassFlow);
            return _variables;
        }
    }
    public class ValveEnthalpyEquation : ISolverEquation
    {
        SolverValve equipment;
        public ValveEnthalpyEquation(SolverValve _valve)
        {
            equipment = _valve;
        }
        public string Name => $"{EquationType} - {equipment.Name}";
        public SolverEquationType EquationType => SolverEquationType.Enthalpy;
        public List<double> Residuals => GetResiduals();
        public List<INewVariable> Variables => GetVariables();
        List<double> GetResiduals()
        {
            List<double> _residuals = new();
            if (equipment.Inlet == null || equipment.Outlet == null) return _residuals;
            double inletH = equipment.Inlet.MassEnthalpy.GetSolverValue();
            double outletH = equipment.Outlet.MassEnthalpy.GetSolverValue();

            _residuals.Add(inletH - outletH);
            return _residuals;
        }
        List<INewVariable> GetVariables()
        {
            List<INewVariable> _variables = new();
            if (equipment.Inlet == null || equipment.Outlet == null) return _variables;
            _variables.Add(equipment.Inlet.MassEnthalpy);
            _variables.Add(equipment.Outlet.MassEnthalpy);

            return _variables;
        }
    }
    public class ValveMassEnergyBalanceEquation : ISolverEquation
    {
        SolverValve equipment;
        public ValveMassEnergyBalanceEquation(SolverValve _valve)
        {
            equipment = _valve;
        }
        public string Name => $"{EquationType} - {equipment.Name}";
        public SolverEquationType EquationType => SolverEquationType.MassEnergyBalance;
        public List<double> Residuals => GetResiduals();
        public List<INewVariable> Variables => GetVariables();
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
        List<INewVariable> GetVariables()
        {
            List<INewVariable> _variables = new();
            if (equipment.Inlet == null || equipment.Outlet == null) return _variables;
            _variables.Add(equipment.Inlet.MassFlow);
            _variables.Add(equipment.Outlet.MassFlow);
            _variables.Add(equipment.Inlet.MassEnthalpy);
            _variables.Add(equipment.Outlet.MassEnthalpy);
            return _variables;
        }
    }
}
