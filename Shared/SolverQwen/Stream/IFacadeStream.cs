using Shared.PropertiesDtos.Methods;
using Shared.SolverConsecutive;
using Shared.SolverQwen.Variables;
using Shared.Thermodynamics.Phases;
using Shared.Thermodynamics.Strategies.Equlibriums;
using Shared.Thermodynamics.Strategies.Flows;
using Shared.UnitOperations.Streams;
using System.Diagnostics;
using UnitSystem;

namespace Shared.SolverQwen.Stream
{

    public enum StreamStateType
    {
        Undefined,              // Sin datos válidos
        Initialized,            // Componentes y método termodinámico definidos
        CompositionDefined,     // Composición válida (Σ=100%)
        EquilibriumCalculated,  // Flash PT/PH/etc. ejecutado exitosamente
        FlowCalculated,         // Balances de flujo ejecutados exitosamente
        Error                   // Error en cálculo (ver logs)
    }
    public interface IFacadeStream
    {
        LiquidPhaseMixture LiquidPhase { get; }
        VaporPhaseMixture VaporPhase { get; }
        string Name { get; set; }
        StreamStateType State { get; set; }  // Undefined, EquilibriumCalculated, FlowCalculated, etc.
        bool IsEquilibriumSolved { get; set; }
        bool IsFlowSolved { get; set; }

        // ─────────────────────────────────────────────────────────
        // 🔹 REFERENCIA AL MODELO TERMODINÁMICO
        // ─────────────────────────────────────────────────────────
        IMaterialStream MaterialStream { get; }

        // ─────────────────────────────────────────────────────────
        // 🔹 VARIABLES PRINCIPALES (UI/Solver) - Todas ProcessVariable<T>
        // ─────────────────────────────────────────────────────────
        NewVariable<Temperature> Temperature { get; }
        NewVariable<Pressure> Pressure { get; }
        NewVariable<MassFlow> MassFlow { get; }
        NewVariable<MolarFlow> MolarFlow { get; }
        NewVariable<VolumetricFlow> VolumetricFlow { get; }
        NewVariable<Percentage> VaporFraction { get; }
        NewVariable<EnergyFlow> EnthalpyFlow { get; }

        // ─────────────────────────────────────────────────────────
        // 🔹 PROPIEDADES TERMODINÁMICAS DERIVADAS
        // ─────────────────────────────────────────────────────────
        NewVariable<ThermalConductivity> ThermalConductivity { get; set; }
        NewVariable<Viscosity> Viscosity { get; set; }
        NewVariable<MassEntropy> MassCp { get; set; }
        NewVariable<MolarEntropy> MolarCp { get; set; }
        NewVariable<MassEnergy> MassEnthalpy { get; set; }
        NewVariable<MolarEnergy> MolarEnthalpy { get; set; }
        NewVariable<MassDensity> MassDensity { get; set; }
        NewVariable<MolarDensity> MolarDensity { get; set; }


        CompositionOrchestrator Composition { get; }
        ThermodynamicState CurrentState { get; }

        void SetThermodynamicMethod(ThermodynamicMethodFullDto method);


    }

    public class FacadeStream : IFacadeStream
    {
        public ThermodynamicState CurrentState => _materialStream.CurrentState;
        public LiquidPhaseMixture LiquidPhase => _materialStream.LiquidPhase;
        public VaporPhaseMixture VaporPhase => _materialStream.VaporPhase;
        private readonly IMaterialStream _materialStream;
        private readonly EquilibriumCalculator _equilibriumCalculator;
        private readonly FlowsCalculator _flowsCalculator;
        private CompositionOrchestrator _composition;

        string _name = string.Empty;
        public string Name
        {
            get { return _name; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Stream name cannot be empty.");
                SetVariablesName(value);

            }
        }
        void SetVariablesName(string name)
        {
            _name = name;
            Temperature.SetName($"{name} Temperature");
            Pressure.SetName($"{name} Pressure");
            MassFlow.SetName($"{name} MassFlow");
            MolarFlow.SetName($"{name} MolarFlow");
            VolumetricFlow.SetName($"{name} VolumetricFlow");
            VaporFraction.SetName($"{name} VaporFraction");
            EnthalpyFlow.SetName($"{name} EnthalpyFlow");
            ThermalConductivity.SetName($"{name} ThermalConductivity");
            Viscosity.SetName($"{name} Viscosity");
            MassCp.SetName($"{name} MassCp");
            MolarCp.SetName($"{name} MolarCp");
            MassEnthalpy.SetName($"{name} MassEnthalpy");
            MolarEnthalpy.SetName($"{name} MolarEnthalpy");
            MassDensity.SetName($"{name} MassDensity");
            MolarDensity.SetName($"{name} MolarDensity");
            EnthalpyFlow.SetName($"{name} EnthalpyFlow");


        }
        public StreamStateType State { get; set; } = StreamStateType.Undefined;
        public bool IsEquilibriumSolved { get; set; }
        public bool IsFlowSolved { get; set; }
        public IMaterialStream MaterialStream => _materialStream;

        public NewVariable<Temperature> Temperature { get; }
        public NewVariable<Pressure> Pressure { get; }
        public NewVariable<MassFlow> MassFlow { get; }
        public NewVariable<MolarFlow> MolarFlow { get; }
        public NewVariable<VolumetricFlow> VolumetricFlow { get; }
        public NewVariable<Percentage> VaporFraction { get; }
        public NewVariable<EnergyFlow> EnthalpyFlow { get; }

        public NewVariable<ThermalConductivity> ThermalConductivity { get; set; }
        public NewVariable<Viscosity> Viscosity { get; set; }
        public NewVariable<MassEntropy> MassCp { get; set; }
        public NewVariable<MolarEntropy> MolarCp { get; set; }
        public NewVariable<MassEnergy> MassEnthalpy { get; set; }
        public NewVariable<MolarEnergy> MolarEnthalpy { get; set; }
        public NewVariable<MassDensity> MassDensity { get; set; }
        public NewVariable<MolarDensity> MolarDensity { get; set; }


        public CompositionOrchestrator Composition => _composition;

        public FacadeStream(string name = "")
        {

            _materialStream = new MaterialStream();

            Temperature = new NewVariable<Temperature>(new Temperature(298.15, TemperatureUnits.Kelvin), TemperatureUnits.DegreeCelcius, 298);
            Pressure = new NewVariable<Pressure>(new Pressure(101325, PressureUnits.Pascala), PressureUnits.Bara, 100000);
            MassFlow = new NewVariable<MassFlow>(new MassFlow(1, MassFlowUnits.Kg_sg), MassFlowUnits.Kg_hr, 3);
            MolarFlow = new NewVariable<MolarFlow>(new MolarFlow(1, MolarFlowUnits.Kgmol_sg), MolarFlowUnits.Kgmol_hr, 3);
            VolumetricFlow = new NewVariable<VolumetricFlow>(new VolumetricFlow(1, VolumetricFlowUnits.m3_sg), VolumetricFlowUnits.m3_hr, 3);
            VaporFraction = new NewVariable<Percentage>(new Percentage(0, PercentageUnits.Percentage), PercentageUnits.Percentage, 100);
            EnthalpyFlow = new NewVariable<EnergyFlow>(new EnergyFlow(0, EnergyFlowUnits.J_sg), EnergyFlowUnits.Kcal_hr, 3000);

            Viscosity = new NewVariable<Viscosity>(new Viscosity(0, ViscosityUnits.Pa_s), ViscosityUnits.cPoise, 0.001);
            ThermalConductivity = new NewVariable<ThermalConductivity>(new ThermalConductivity(0, ThermalConductivityUnits.W_m_K), ThermalConductivityUnits.W_m_K, 0.1);
            MassCp = new NewVariable<MassEntropy>(new MassEntropy(1, MassEntropyUnits.KJ_Kg_C), MassEntropyUnits.Kcal_Kg_C, 4);
            MolarCp = new NewVariable<MolarEntropy>(new MolarEntropy(1, MolarEntropyUnits.KJ_Kgmol_C), MolarEntropyUnits.Kcal_Kgmol_C, 4);
            MassEnthalpy = new NewVariable<MassEnergy>(new MassEnergy(1, MassEnergyUnits.J_Kg), MassEnergyUnits.Kcal_Kg, 1000);
            MolarEnthalpy = new NewVariable<MolarEnergy>(new MolarEnergy(0, MolarEnergyUnits.KJ_Kgmol), MolarEnergyUnits.Kcal_Kgmol, 100000);
            MassDensity = new NewVariable<MassDensity>(new MassDensity(1000, MassDensityUnits.Kg_m3), MassDensityUnits.Kg_m3, 1000);
            MolarDensity = new NewVariable<MolarDensity>(new MolarDensity(1, MolarDensityUnits.Kgmol_m3), MolarDensityUnits.Kgmol_m3, 1000 / 18);

            _composition = null!;

            _equilibriumCalculator = new EquilibriumCalculator(this);
            _equilibriumCalculator.EquilibriumReady += OnEquilibriumReady;
            _equilibriumCalculator.FlowsReady += OnMassFlowChanged;

            _flowsCalculator = new FlowsCalculator(this);
            SetVariablesName(name);

            SubscribeToVariableChanges();
        }

        private void SubscribeToVariableChanges()
        {
            Temperature.ValueChanged += () => _materialStream.SetTemperature(Temperature.Value);
            Temperature.ValueChanged += ExecuteEquilibrium;
            Temperature.AddVariableToList += v => _equilibriumCalculator.AddVariable(v);

            Pressure.ValueChanged += () => _materialStream.SetPressure(Pressure.Value);
            Pressure.ValueChanged += ExecuteEquilibrium;
            Pressure.AddVariableToList += v => _equilibriumCalculator.AddVariable(v);

            MassFlow.ValueChanged += OnMassFlowChanged;
            MassFlow.AddVariableToList += v => _flowsCalculator.AddVariable(v);

            MolarFlow.ValueChanged += OnMassFlowChanged;
            MolarFlow.AddVariableToList += v => _flowsCalculator.AddVariable(v);

            VolumetricFlow.ValueChanged += OnMassFlowChanged;
            VolumetricFlow.AddVariableToList += v => _flowsCalculator.AddVariable(v);

            VaporFraction.ValueChanged += () => _materialStream.SetVaporFraction(VaporFraction.Value);
            VaporFraction.ValueChanged += ExecuteEquilibrium;
            VaporFraction.AddVariableToList += v => _equilibriumCalculator.AddVariable(v);


            MassEnthalpy.ValueChanged += () =>
            {
                if (_materialStream.MassEnthalpy != null)
                    _materialStream.MassEnthalpy = MassEnthalpy.Value;
                if (MassEnthalpy.DataProcedence != VariableDefinedBy.StreamCalculated)
                {
                    ExecuteEquilibrium();
                }

            };
            MassEnthalpy.AddVariableToList += v => _equilibriumCalculator.AddVariable(v);
            MolarEnthalpy.AddVariableToList += v => _equilibriumCalculator.AddVariable(v);
            MassCp.AddVariableToList += v => _equilibriumCalculator.AddVariable(v);
            MolarCp.AddVariableToList += v => _equilibriumCalculator.AddVariable(v);
            Viscosity.AddVariableToList += v => _equilibriumCalculator.AddVariable(v);
            ThermalConductivity.AddVariableToList += v => _equilibriumCalculator.AddVariable(v);
            MassDensity.AddVariableToList += v => _equilibriumCalculator.AddVariable(v);
            MolarDensity.AddVariableToList += v => _equilibriumCalculator.AddVariable(v);

            EnthalpyFlow.AddVariableToList += v => _flowsCalculator.AddVariable(v);
        }



        private void OnMassFlowChanged() => _flowsCalculator.Execute();

        private void OnEquilibriumReady()
        {
            SyncFromMaterialStream();
            IsEquilibriumSolved = true;
        }




        public void SyncFromMaterialStream()
        {

            if (_materialStream.MolarEnthalpy != null) MolarEnthalpy.SetValue(_materialStream.MolarEnthalpy, VariableDefinedBy.StreamCalculated);
            if (_materialStream.MassDensity != null) MassDensity.SetValue(_materialStream.MassDensity, VariableDefinedBy.StreamCalculated);
            if (_materialStream.MolarDensity != null) MolarDensity.SetValue(_materialStream.MolarDensity, VariableDefinedBy.StreamCalculated);
            if (_materialStream.Viscosity != null) Viscosity.SetValue(_materialStream.Viscosity, VariableDefinedBy.StreamCalculated);
            if (_materialStream.ThermalConductivity != null) ThermalConductivity.SetValue(_materialStream.ThermalConductivity, VariableDefinedBy.StreamCalculated);
            if (_materialStream.MassHeatCapacity != null) MassCp.SetValue(_materialStream.MassHeatCapacity, VariableDefinedBy.StreamCalculated);
            if (_materialStream.MolarHeatCapacity != null) MolarCp.SetValue(_materialStream.MolarHeatCapacity, VariableDefinedBy.StreamCalculated);



            State = IsFlowSolved ? StreamStateType.FlowCalculated :
                    IsEquilibriumSolved ? StreamStateType.EquilibriumCalculated :
                    Composition.IsValid ? StreamStateType.CompositionDefined : StreamStateType.Undefined;
        }

        public void SetThermodynamicMethod(ThermodynamicMethodFullDto method)
        {
            _materialStream.SetThermodynamicMethod(method);

            List<ComponentFacade> _componentList = new();
            _componentList.Clear();
            // Dentro de FacadeStream.SetThermodynamicMethod, en el bucle foreach:
            foreach (var compDto in method.Components)
            {
                var facade = new ComponentFacade(compDto);
                _componentList.Add(facade);

                // Ya tenías estas dos:
                facade.MassFlow.AddVariableToList += v => _flowsCalculator.AddVariable(v);
                facade.MolarFlow.AddVariableToList += v => _flowsCalculator.AddVariable(v);

                // ➕ AGREGA ESTAS NUEVAS LÍNEAS:
                facade.MassFraction.ValueChanged += OnMassFlowChanged; // Dispara FlowsCalculator
                facade.MolarFraction.ValueChanged += OnMassFlowChanged; // Dispara FlowsCalculator



                facade.MassFlow.ValueChanged += OnMassFlowChanged; // Dispara FlowsCalculator
                facade.MolarFlow.ValueChanged += OnMassFlowChanged; // Dispara FlowsCalculator

                facade.MassFraction.AddVariableToList += v => _flowsCalculator.AddVariable(v);
                facade.MolarFraction.AddVariableToList += v => _flowsCalculator.AddVariable(v);
            }

            // 2. Crear orchestrator con la lista YA poblada
            _composition = new CompositionOrchestrator(_componentList);
            _composition.OnCompositionChanged += () => _materialStream.SetCompositionData(Composition);
            _composition.OnCompositionChanged += ExecuteEquilibrium;

            State = StreamStateType.Initialized;
        }

        public void ExecuteEquilibrium() => _equilibriumCalculator.Execute();
        public void ExecuteFlows() => _flowsCalculator.Execute();

        /// <summary>
        /// Limpia una variable específica o todas las de un owner, disparando cascada de invalidación.
        /// </summary>

    }




}