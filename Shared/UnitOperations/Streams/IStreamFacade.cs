using Shared.ProcessFlowDiagram;
using Shared.PropertiesDtos.Methods;
using Shared.Thermodynamics.ControlledVariables;
using Shared.Thermodynamics.Phases;
using Shared.Thermodynamics.Strategies.Equlibriums;
using Shared.Thermodynamics.Strategies.Flows;
using Shared.UnitOperations.Basiss;
using System;
using System.Collections.Generic;
using System.Text;
using UnitSystem;

namespace Shared.UnitOperations.Streams
{


    // ───────────────────────────────────────────────────────────────
    // 🔹 INTERFAZ: IStreamFacade (nueva, minimalista)
    // ───────────────────────────────────────────────────────────────
    public interface IStreamFacade : IFacade
    {
        IMaterialStream MaterialStream { get; }

        // ═══════════════════════════════════════════════════════════
        // 🔹 VARIABLES PRINCIPALES (usando IVariable<T>)
        // ═══════════════════════════════════════════════════════════
        VariableAmount<Temperature> Temperature { get; }
        VariableAmount<Pressure> Pressure { get; }

        VariableAmount<MassFlow> MassFlow { get; }
        VariableAmount<MolarFlow> MolarFlow { get; }
        VariableAmount<VolumetricFlow> VolumetricFlow { get; }

        VariableAmount<MassEnergy> MassEnthalpy { get; }
        VariableAmount<MolarEnergy> MolarEnthalpy { get; }

        VariableAmount<MassDensity> MassDensity { get; }
        VariableAmount<MolarDensity> MolarDensity { get; }

        VariableAmount<EnergyFlow> EnthalpyFlow { get; }

        VariableComposition StreamComposition { get; }
        VariableDouble VaporFraction { get; }
        VariableDouble MolecularWeight { get; }

        // ═══════════════════════════════════════════════════════════
        // 🔹 PROPIEDADES DE ESTADO
        // ═══════════════════════════════════════════════════════════
        ThermodynamicState EquilibriumState { get; }
        StreamStateType State { get; }
        ThermodynamicMethodFullDto? ThermoMethod { get; }

        void RemoveEquilibriumCalculate();
        void SetThermodynamicMethod(ThermodynamicMethodFullDto method);
        void ClearThermodynamicMethod();

        bool IsEquilibriumSolved { get; set; }
        bool IsFlowSolved { get; set; }
        Action? OnStateChanged { get; set; }
        void RemoveFlowsCalculate();
    }

    // ───────────────────────────────────────────────────────────────
    // 🔹 CLASE: StreamFacade (implementación limpia)
    // ───────────────────────────────────────────────────────────────
    public class StreamFacade : IStreamFacade
    {
        List<IVariable> EquilibriumVariables = new();

        List<IVariable> FlowsVariables = new();
        public void RemoveEquilibriumCalculate()
        {
            foreach (var v in EquilibriumVariables)
            {
                v.ClearFromStream();
            }
            EquilibriumVariables.Clear();

        }
        public void RemoveFlowsCalculate()
        {
            foreach (var v in FlowsVariables)
            {
                v.ClearFromStream();
            }
            FlowsVariables.Clear();


        }
        private readonly EquilibriumCalculator3 _equilibriumCalculator;
        private readonly FlowsCalculator3 _flowsCalculator;
        public bool IsFlowSolved { get; set; } = false;
        public bool IsEquilibriumSolved { get; set; }
        public IMaterialStream MaterialStream { get; } = new MaterialStream();
        public ThermodynamicState EquilibriumState => MaterialStream.CurrentState;

        public ThermodynamicMethodFullDto? ThermoMethod { get; private set; }

        // ═══════════════════════════════════════════════════════════
        // 🔹 VARIABLES PRINCIPALES (IVariable<T>)
        // ═══════════════════════════════════════════════════════════
        public VariableAmount<Temperature> Temperature { get; private set; }
        public VariableAmount<Pressure> Pressure { get; private set; }

        public VariableAmount<MassFlow> MassFlow { get; private set; }
        public VariableAmount<MolarFlow> MolarFlow { get; private set; }
        public VariableAmount<VolumetricFlow> VolumetricFlow { get; private set; }

        public VariableAmount<MassEnergy> MassEnthalpy { get; private set; }
        public VariableAmount<MolarEnergy> MolarEnthalpy { get; private set; }

        public VariableAmount<MassDensity> MassDensity { get; private set; }
        public VariableAmount<MolarDensity> MolarDensity { get; private set; }

        public VariableAmount<EnergyFlow> EnthalpyFlow { get; private set; }

        public VariableComposition StreamComposition { get; private set; }
        public VariableDouble VaporFraction { get; private set; }
        public VariableDouble MolecularWeight { get; private set; }

        // ═══════════════════════════════════════════════════════════
        // 🔹 IDENTIDAD
        // ═══════════════════════════════════════════════════════════
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "S-1";

        // ═══════════════════════════════════════════════════════════
        // 🔹 EVENTOS
        // ═══════════════════════════════════════════════════════════
        public Action? OnExecuteSolver { get; set; }
        public Action? OnStateChanged { get; set; }

        // ═══════════════════════════════════════════════════════════
        // 🔹 CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════
        public StreamFacade()
        {
            _equilibriumCalculator = new EquilibriumCalculator3(this);
            _flowsCalculator = new FlowsCalculator3(this);




            // Composición (especial: no es escalar)
            StreamComposition = new VariableComposition(new StreamComposition());
            StreamComposition.Value.NewParentVariable = StreamComposition;

            // Fracción de vapor
            VaporFraction = new VariableDouble(0);

            // Peso molecular (calculado)
            MolecularWeight = new VariableDouble(0);

            // Temperatura
            Temperature = new VariableAmount<Temperature>(
                new Temperature(),
                TemperatureUnits.DegreeCelcius,
                TemperatureUnits.Kelvin,
                (v, u) => new Temperature(v, u),
                298.15
            );

            // Presión
            Pressure = new VariableAmount<Pressure>(
                new Pressure(),
                PressureUnits.Bara,
                PressureUnits.Pascala,
                (v, u) => new Pressure(v, u),
                101325
            );

            // Flujos
            MassFlow = new VariableAmount<MassFlow>(
                new MassFlow(),
                MassFlowUnits.Kg_hr,
                MassFlowUnits.Kg_hr,
                (v, u) => new MassFlow(v, u),
                1000
            );

            MolarFlow = new VariableAmount<MolarFlow>(
                new MolarFlow(),
                MolarFlowUnits.Kgmol_hr,
                MolarFlowUnits.Kgmol_hr,
                (v, u) => new MolarFlow(v, u),
                100
            );

            VolumetricFlow = new VariableAmount<VolumetricFlow>(
                new VolumetricFlow(),
                VolumetricFlowUnits.m3_hr,
                VolumetricFlowUnits.m3_hr,
                (v, u) => new VolumetricFlow(v, u),
                1
            );

            // Propiedades termodinámicas (calculadas)
            MassEnthalpy = new VariableAmount<MassEnergy>(
                new MassEnergy(),
                MassEnergyUnits.Kcal_Kg,
                MassEnergyUnits.KJ_Kg,
                (v, u) => new MassEnergy(v, u)
            );

            MolarEnthalpy = new VariableAmount<MolarEnergy>(
                new MolarEnergy(),
                MolarEnergyUnits.Kcal_Kgmol,
                MolarEnergyUnits.J_gmol,
                (v, u) => new MolarEnergy(v, u)
            );

            MassDensity = new VariableAmount<MassDensity>(
                new MassDensity(),
                MassDensityUnits.Kg_m3,
                MassDensityUnits.Kg_m3,
                (v, u) => new MassDensity(v, u)
            );

            MolarDensity = new VariableAmount<MolarDensity>(
                new MolarDensity(),
                MolarDensityUnits.Kgmol_m3,
                MolarDensityUnits.gmol_m3,
                (v, u) => new MolarDensity(v, u)
            );

            EnthalpyFlow = new VariableAmount<EnergyFlow>(
                new EnergyFlow(),
                EnergyFlowUnits.Kcal_hr,
                EnergyFlowUnits.Kcal_hr,
                (v, u) => new EnergyFlow(v, u)
            );
            SubscribeToPropagationEvents();
        }

        private void SubscribeToPropagationEvents()
        {
            // ═══════════════════════════════════════════════════════
            // 🔹 SINCRO CON MaterialStream (UI ↔ Modelo)
            // ═══════════════════════════════════════════════════════
            StreamComposition.SendToFacadeInside += () =>
                MaterialStream.SetCompositionData(StreamComposition.Value);
            
          
            Temperature.SendToFacadeInside += () =>
                MaterialStream.SetTemperature(Temperature.Value);
            
            Pressure.SendToFacadeInside += () =>
                MaterialStream.SetPressure(Pressure.Value);
          
            //VaporFraction.SendToFacadeInside += () =>
            //    MaterialStream.SetVaporFraction(VaporFraction.Value);
         

            // ═══════════════════════════════════════════════════════
            // 🔹 PROPAGACIÓN: Equilibrio termodinámico
            // ═══════════════════════════════════════════════════════
            StreamComposition.ExecuteStreamCalculation += ExecuteEquilibrium;
            Temperature.ExecuteStreamCalculation += ExecuteEquilibrium;
            Pressure.ExecuteStreamCalculation += ExecuteEquilibrium;
            VaporFraction.ExecuteStreamCalculation += ExecuteEquilibrium;
            MassEnthalpy.ExecuteStreamCalculation += ExecuteEquilibrium;

            // ═══════════════════════════════════════════════════════
            // 🔹 PROPAGACIÓN: Cálculo de flujos
            // ═══════════════════════════════════════════════════════
            MassFlow.ExecuteStreamCalculation += ExecuteFlows;
            MolarFlow.ExecuteStreamCalculation += ExecuteFlows;
            VolumetricFlow.ExecuteStreamCalculation += ExecuteFlows;
            EnthalpyFlow.ExecuteStreamCalculation += ExecuteFlows;

            // ═══════════════════════════════════════════════════════
            // 🔹 PROPAGACIÓN: Peso molecular (depende de composición)
            // ═══════════════════════════════════════════════════════
            StreamComposition.ExecuteStreamCalculation += CalculateMolecularWeight;

            Temperature.ExecuteGeneralSolver += ExecuteSolver;
            StreamComposition.ExecuteGeneralSolver += ExecuteSolver;
            Pressure.ExecuteGeneralSolver += ExecuteSolver;
            VaporFraction.ExecuteGeneralSolver += ExecuteSolver;
            MassFlow.ExecuteGeneralSolver += ExecuteSolver;
            MolarFlow.ExecuteGeneralSolver += ExecuteSolver;
            VolumetricFlow.ExecuteGeneralSolver += ExecuteSolver;
        

        }
        void ExecuteSolver()
        {
            OnExecuteSolver?.Invoke();
        }


        // ═══════════════════════════════════════════════════════════
        // 🔹 MÉTODOS DE CONFIGURACIÓN TERMODINÁMICA
        // ═══════════════════════════════════════════════════════════
        public void SetThermodynamicMethod(ThermodynamicMethodFullDto method)
        {
            ThermoMethod = method;
            CreateFacadeComponents(method);
            MaterialStream.SetThermodynamicMethod(method);
        }

        public void ClearThermodynamicMethod()
        {
            ThermoMethod = null;
            MaterialStream.ClearThermodynamicMethod();
        }

        private void CreateFacadeComponents(ThermodynamicMethodFullDto method)
        {
            var data = StreamComposition.Value;
            if (data != null)
            {
                data.Components.Clear();
                foreach (var comp in method.Components)
                {
                    var newComp = new ComponentComposition
                    {
                        ComponentId = comp.ComponentId,
                        ComponentName = comp.ComponentName,
                        MolecularWeight = comp.FullData.MolecularWeight
                    };
                    data.Components.Add(newComp);
                }
                data.AttachEvents();
            }
        }

        // ═══════════════════════════════════════════════════════════
        // 🔹 EJECUCIÓN DE CÁLCULOS
        // ═══════════════════════════════════════════════════════════
        private void ExecuteEquilibrium()
        {
            _equilibriumCalculator.Execute();
            NotifyStateChanged();
        }

        private void ExecuteFlows()
        {
            _flowsCalculator.Execute();
            NotifyStateChanged();
        }

        private void CalculateMolecularWeight()
        {
            if (StreamComposition.IsDefinedByUI || StreamComposition.NewSolverValue.HasValue)
            {
                MolecularWeight.NewSolverValue = MaterialStream.MolecularWeight;
            }
            else
            {
                MolecularWeight.ClearFromGeneralSolver();
            }
        }

        private void NotifyStateChanged() => OnStateChanged?.Invoke();

        // ═══════════════════════════════════════════════════════════
        // 🔹 ACTUALIZACIÓN DESDE CÁLCULOS (EquilibriumCalculator → Variable)
        // ═══════════════════════════════════════════════════════════
        internal void UpdateFromEquilibriumCalculation()
        {
            // Solo actualizar si la variable NO fue definida por UI
            if (!Temperature.IsDefinedByUI)
                Temperature.NewSolverValue = MaterialStream.Temperature.GetValue(Temperature.UnitForSolver);

            if (!Pressure.IsDefinedByUI)
                Pressure.NewSolverValue = MaterialStream.Pressure.GetValue(Pressure.UnitForSolver);

            if (!MassEnthalpy.IsDefinedByUI)
                MassEnthalpy.NewSolverValue = MaterialStream.MassEnthalpy.GetValue(MassEnthalpy.UnitForSolver);

            if (!MolarEnthalpy.IsDefinedByUI)
                MolarEnthalpy.NewSolverValue = MaterialStream.MolarEnthalpy.GetValue(MolarEnthalpy.UnitForSolver);

            if (!MassDensity.IsDefinedByUI)
                MassDensity.NewSolverValue = MaterialStream.MassDensity.GetValue(MassDensity.UnitForSolver);

            if (!MolarDensity.IsDefinedByUI)
                MolarDensity.NewSolverValue = MaterialStream.MolarDensity.GetValue(MolarDensity.UnitForSolver);

            if (!MolecularWeight.IsDefinedByUI)
                MolecularWeight.NewSolverValue = MaterialStream.MolecularWeight;

            NotifyStateChanged();
        }

        internal void UpdateFromFlowsCalculation()
        {
            if (!EnthalpyFlow.IsDefinedByUI)
                EnthalpyFlow.NewSolverValue = MaterialStream.EnthalpyFlow.GetValue(EnthalpyFlow.UnitForSolver);

            NotifyStateChanged();
        }

        // ═══════════════════════════════════════════════════════════
        // 🔹 ESTADO Y UI
        // ═══════════════════════════════════════════════════════════
        public StreamStateType State
        {
            get
            {
                bool hasEquilibrium = Temperature.IsDefinedByUI && Pressure.IsDefinedByUI &&
                                     StreamComposition.IsDefinedByUI && ThermoMethod != null;
                bool hasFlows = MassFlow.IsDefinedByUI || MolarFlow.IsDefinedByUI;

                if (hasEquilibrium && hasFlows)
                    return StreamStateType.StreamCalculated;

                if (hasEquilibrium)
                    return StreamStateType.EquilibriumCalculated;

                if (ThermoMethod != null)
                    return StreamStateType.MethodDefined;

                return StreamStateType.Created;
            }
        }

        public string StatusText => State switch
        {
            StreamStateType.Created => "Ready",
            StreamStateType.MethodDefined => "Underspecified",
            StreamStateType.EquilibriumCalculated => "Equilibrium Solved",
            StreamStateType.StreamCalculated => "Converged",
            _ => "Unknown"
        };

        public string StatusColor => State switch
        {
            StreamStateType.Created => "#CBD5E0",
            StreamStateType.MethodDefined => "#F6AD55",
            StreamStateType.EquilibriumCalculated => "#63B3ED",
            StreamStateType.StreamCalculated => "#34D399",
            _ => "#CBD5E0"
        };

        public List<ToolTipLegend> GetToolTipLegend()
        {
            return new List<ToolTipLegend>
        {
            new("Name", Name ?? "<Not Defined>"),
            new("Pressure", Pressure.GetDisplayString()),
            new("Temperature", Temperature.GetDisplayString()),
            new("Mass Flow", MassFlow.GetDisplayString()),
            new("Enthalpy Flow", EnthalpyFlow.GetDisplayString())
        };
        }
    }
}
