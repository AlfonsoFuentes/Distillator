using Shared.SolverQwen.Stream;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.SolverConsecutive.Equipments
{
    public enum ValveStateType
    {
        Created,
        PartiallyConnected,
        ReadyToCalculate,
        Solved
    }
    public class SolverValve : SolverEquipmentBase
    {
        public IFacadeStream Inlet { get; private set; } = null!;
        public IFacadeStream Outlet { get; private set; } = null!;
        public Variable<PressureDrop> DeltaP { get; set; }
        public Variable<Percentage> Opening { get; set; } // Porcentaje de apertura
        public Variable<UnitLess> Cv { get; set; }
        public override List<ISolverEquation> Equations => GetEquations().ToList();
        public override Task PostSolveAsync()
        {
            // Aquí calculas el Cv, Power, o cualquier KPI post-convergencia
            CalculateCv();
            return Task.CompletedTask;
        }
        public SolverValve(string name)
        {
            Name = name;
            DeltaP = new Variable<PressureDrop>(new PressureDrop(0, PressureDropUnits.Pascal), PressureDropUnits.Bar, 100000);
            Opening = new Variable<Percentage>(new Percentage(100, PercentageUnits.Percentage), PercentageUnits.Percentage, 100);
            Cv = new Variable<UnitLess>(new UnitLess(0), UnitLessUnits.None, 1);
        }

        IEnumerable<ISolverEquation> GetEquations()
        {
            yield return new ValvePressureEquation(this);
            yield return new ValveConcentrationEquation(this);
            yield return new ValveMassBalanceEquation(this);
            yield return new ValveEnthalpyEquation(this);
            yield return new ValveMassEnergyBalanceEquation(this);
            // Backup legacy: la V2 de specifications usa ValveMassBalanceEquation regular.
            // yield return new ValveMassBalanceEquationSpec(this);
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

      

        public ValveStateType State => GetState();

        private ValveStateType GetState()
        {
            // 1. Topología
            if (Inlet == null || Outlet == null)
                return ValveStateType.PartiallyConnected;

            // 2. Especificaciones (Asumimos que debe tener DeltaP u Opening)
            if (!DeltaP.IsDefined /*&& !Opening.IsDefined*/)
                return ValveStateType.ReadyToCalculate;

            // 3. Resolución final (El método CalculateCv se ejecutó exitosamente)
            if (Cv != null && Cv.IsDefined)
                return ValveStateType.Solved;

            return ValveStateType.ReadyToCalculate;
        }
        private void CalculateCv()

        {
            Cv.Clear(VariableDefinedBy.Equipment);
            if (Inlet == null || Outlet == null) return;
            if (Inlet.State != StreamStateType.Calculated) return;
            if (!DeltaP.IsDefined) return;
            var deltap = DeltaP.Value.GetValue(PressureDropUnits.psi);
            if (Inlet.ThermodynamicState == ThermodynamicState.SaturatedLiquid ||
                Inlet.ThermodynamicState == ThermodynamicState.SubcooledLiquid)
            {
                var q = Inlet.VolumetricFlow.Value.GetValue(VolumetricFlowUnits.gal_min);

                var sg = Inlet.MassDensity.Value.GetValue(MassDensityUnits.Kg_m3) / 1000;
                if (deltap <= 0) return;

                var cv = q * Math.Sqrt(sg / deltap);

                Cv.SetValue(new UnitLess(cv), VariableDefinedBy.Equipment);
            }
            else if (Inlet.ThermodynamicState == ThermodynamicState.SaturatedVapor || Inlet.ThermodynamicState == ThermodynamicState.SuperheatedVapor)
            {
                var p1 = Inlet.Pressure.Value.GetValue(PressureUnits.Psia);
                var t1 = Inlet.Temperature.Value.GetValue(TemperatureUnits.Kelvin) * 1.8; // Rankine es mandatorio aquí
                var mw = Inlet.MolecularWeight.Value.GetValue(UnitLessUnits.None);
                var w = Inlet.MassFlow.Value.GetValue(MassFlowUnits.lb_hr);

                var p2 = p1 - deltap;
                var sg_gas = mw / 28.96; // Gravedad específica respecto al aire

                // Convertimos flujo másico (lb/hr) a flujo volumétrico estándar (SCFH)
                var q_g = w * 379.5 / mw;

                double cv;

                // ¿Flujo Sónico / Choked? (La caída de presión es mayor al 50% de la entrada)
                if (deltap >= 0.5 * p1)
                {
                    cv = q_g / (963.0 * p1 * Math.Sqrt(1.0 / (sg_gas * t1)));
                }
                else // Flujo Subcrítico
                {
                    cv = q_g / (963.0 * Math.Sqrt((deltap * (p1 + p2)) / (sg_gas * t1)));
                }

                Cv.SetValue(new UnitLess(cv), VariableDefinedBy.Equipment);
            }
            else
            {
                var w = Inlet.MassFlow.Value.GetValue(MassFlowUnits.lb_hr);
                var densityMix = Inlet.MassDensity.Value.GetValue(MassDensityUnits.lb_ft3);

                // Volumen específico de la mezcla (ft3/lb)
                var v_mix = 1.0 / densityMix;

                // Ecuación general de la norma ISA para flujo másico
                // 63.3 es la constante de conversión para lb/hr, psi y ft3/lb
                var cv = w / (63.3 * Math.Sqrt(deltap / v_mix));

                Cv.SetValue(new UnitLess(cv), VariableDefinedBy.Equipment);
            }
        }
       
    }
    public class ValvePressureEquation : ISolverEquation
    {
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        SolverValve valve;
        public ValvePressureEquation(SolverValve _valve)
        {
            valve = _valve;
        }
        public string Name => $"{EquationType} - {valve.Name}";
        public SolverEquationType EquationType => SolverEquationType.Pressure;
        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();
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
        List<IVariable> GetVariables()
        {
            List<IVariable> _variables = new();
            if (valve.Inlet == null || valve.Outlet == null) return _variables;
            _variables.Add(valve.DeltaP);
            _variables.Add(valve.Inlet.Pressure);
            _variables.Add(valve.Outlet.Pressure);
            return _variables;
        }
    }
    public class ValveConcentrationEquation : ISolverEquation
    {
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        SolverValve equipment;
        public ValveConcentrationEquation(SolverValve _valve)
        {
            equipment = _valve;
        }
        public string Name => $"{EquationType} - {equipment.Name}";
        public SolverEquationType EquationType => SolverEquationType.Concentration;
        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();
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
        List<IVariable> GetVariables()
        {
            List<IVariable> _variables = new();
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
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        SolverValve equipment;
        public ValveMassBalanceEquation(SolverValve _valve)
        {
            equipment = _valve;
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
    public class ValveEnthalpyEquation : ISolverEquation
    {
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        SolverValve equipment;
        public ValveEnthalpyEquation(SolverValve _valve)
        {
            equipment = _valve;
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
    public class ValveMassEnergyBalanceEquation : ISolverEquation
    {
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        SolverValve equipment;
        public ValveMassEnergyBalanceEquation(SolverValve _valve)
        {
            equipment = _valve;
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
    // Backup legacy: la V2 de specifications usa ValveMassBalanceEquation regular.
    public class ValveMassBalanceEquationSpec : ISpecSolverEquation
    {
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Spec;
        SolverValve equipment;
        public ValveMassBalanceEquationSpec(SolverValve _valve)
        {
            equipment = _valve;
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
