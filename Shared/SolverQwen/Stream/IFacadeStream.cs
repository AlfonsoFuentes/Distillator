using Shared.ProcessFlowDiagram;
using Shared.PropertiesDtos.Methods;
using Shared.SolverConsecutive;
using Shared.SolverQwen.Variables;
using Shared.Thermodynamics.Phases;
using Shared.Thermodynamics.Strategies.Equlibriums;
using Shared.Thermodynamics.Strategies.Flows;
using Shared.UnitOperations.Basiss;
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
        Calculated,             // 🔥 NUEVO: TODO calculado (Flow + Equilibrium)
        Error                   // Error en cálculo (ver logs)
    }
    public interface IFacadeStream : IFacade
    {

        LiquidPhaseMixture LiquidPhase { get; }
        VaporPhaseMixture VaporPhase { get; }

        StreamStateType State { get;  }  // Undefined, EquilibriumCalculated, FlowCalculated, etc.
        bool IsEquilibriumSolved { get; set; }
        bool IsFlowSolved { get; set; }

        // ─────────────────────────────────────────────────────────
        // 🔹 REFERENCIA AL MODELO TERMODINÁMICO
        // ─────────────────────────────────────────────────────────
        IMaterialStream MaterialStream { get; }

        // ─────────────────────────────────────────────────────────
        // 🔹 VARIABLES PRINCIPALES (UI/Solver) - Todas ProcessVariable<T>
        // ─────────────────────────────────────────────────────────
        Variable<Temperature> Temperature { get; set; }
        Variable<Pressure> Pressure { get; set; }
        Variable<MassFlow> MassFlow { get; set; }
        Variable<MolarFlow> MolarFlow { get; set; }
        Variable<VolumetricFlow> VolumetricFlow { get; set; }
        Variable<Percentage> VaporFraction { get; set; }
        Variable<EnergyFlow> EnthalpyFlow { get; set; }

        // ─────────────────────────────────────────────────────────
        // 🔹 PROPIEDADES TERMODINÁMICAS DERIVADAS
        // ─────────────────────────────────────────────────────────
        Variable<ThermalConductivity> ThermalConductivity { get; set; }
        Variable<Viscosity> Viscosity { get; set; }
        Variable<MassEntropy> MassCp { get; set; }
        Variable<MolarEntropy> MolarCp { get; set; }
        Variable<MassEnergy> MassEnthalpy { get; set; }
        Variable<MolarEnergy> MolarEnthalpy { get; set; }
        Variable<MassDensity> MassDensity { get; set; }
        Variable<MolarDensity> MolarDensity { get; set; }
        Variable<UnitLess> MolecularWeight { get; set; }

        Variable<SuperficialTension> SuperficialTension { get; set; }

        CompositionOrchestrator Composition { get; set; }
        ThermodynamicState EquilibriumState { get; }

        void SetThermodynamicMethod(ThermodynamicMethodFullDto method);


    }

    public class FacadeStream : IFacadeStream
    {
     
        public Guid Id { get; set; } = Guid.NewGuid();
        public ThermodynamicState EquilibriumState => _materialStream.CurrentState;
        public LiquidPhaseMixture LiquidPhase => _materialStream.LiquidPhase;
        public VaporPhaseMixture VaporPhase => _materialStream.VaporPhase;
        private readonly IMaterialStream _materialStream;
        private readonly EquilibriumCalculator _equilibriumCalculator;
        private readonly FlowsCalculator _flowsCalculator;
     

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
        public StreamStateType State => GetState();
        public bool IsEquilibriumSolved { get; set; }
        public bool IsFlowSolved { get; set; }
        public bool HasError { get; set; } // 🔥 NUEVO: Para detectar errores
        public IMaterialStream MaterialStream => _materialStream;

        public Variable<Temperature> Temperature { get; set; }
        public Variable<Pressure> Pressure { get; set; }
        public Variable<MassFlow> MassFlow { get; set; }
        public Variable<MolarFlow> MolarFlow { get; set; }
        public Variable<VolumetricFlow> VolumetricFlow { get; set; }
        public Variable<Percentage> VaporFraction { get; set; }
        public Variable<EnergyFlow> EnthalpyFlow { get; set; }

        public Variable<ThermalConductivity> ThermalConductivity { get; set; }
        public Variable<Viscosity> Viscosity { get; set; }
        public Variable<MassEntropy> MassCp { get; set; }
        public Variable<MolarEntropy> MolarCp { get; set; }
        public Variable<MassEnergy> MassEnthalpy { get; set; }
        public Variable<MolarEnergy> MolarEnthalpy { get; set; }
        public Variable<MassDensity> MassDensity { get; set; }
        public Variable<MolarDensity> MolarDensity { get; set; }
        public Variable<SuperficialTension> SuperficialTension { get; set; }
        public Variable<UnitLess> MolecularWeight { get; set; }
        public CompositionOrchestrator Composition { get; set; } 
     


        public FacadeStream(string name = "")
        {

            _materialStream = new MaterialStream();

            Temperature = new Variable<Temperature>(new Temperature(298.15, TemperatureUnits.Kelvin), TemperatureUnits.DegreeCelcius, 298);
            Pressure = new Variable<Pressure>(new Pressure(101325, PressureUnits.Pascala), PressureUnits.Bara, 100000);
            MassFlow = new Variable<MassFlow>(new MassFlow(1, MassFlowUnits.Kg_sg), MassFlowUnits.Kg_hr, 3);
            MolarFlow = new Variable<MolarFlow>(new MolarFlow(1, MolarFlowUnits.Kgmol_sg), MolarFlowUnits.Kgmol_hr, 3);
            VolumetricFlow = new Variable<VolumetricFlow>(new VolumetricFlow(1, VolumetricFlowUnits.m3_sg), VolumetricFlowUnits.m3_hr, 3);
            VaporFraction = new Variable<Percentage>(new Percentage(0, PercentageUnits.Percentage), PercentageUnits.Percentage, 100);
            EnthalpyFlow = new Variable<EnergyFlow>(new EnergyFlow(0, EnergyFlowUnits.J_sg), EnergyFlowUnits.Kcal_hr, 3000);

            Viscosity = new Variable<Viscosity>(new Viscosity(0, ViscosityUnits.Pa_s), ViscosityUnits.cPoise, 0.001);
            ThermalConductivity = new Variable<ThermalConductivity>(new ThermalConductivity(0, ThermalConductivityUnits.W_m_K), ThermalConductivityUnits.W_m_K, 0.1);
            MassCp = new Variable<MassEntropy>(new MassEntropy(1, MassEntropyUnits.KJ_Kg_C), MassEntropyUnits.Kcal_Kg_C, 4);
            MolarCp = new Variable<MolarEntropy>(new MolarEntropy(1, MolarEntropyUnits.KJ_Kgmol_C), MolarEntropyUnits.Kcal_Kgmol_C, 4);
            MassEnthalpy = new Variable<MassEnergy>(new MassEnergy(1, MassEnergyUnits.J_Kg), MassEnergyUnits.Kcal_Kg, 1000);
            MolarEnthalpy = new Variable<MolarEnergy>(new MolarEnergy(0, MolarEnergyUnits.KJ_Kgmol), MolarEnergyUnits.Kcal_Kgmol, 100000);
            MassDensity = new Variable<MassDensity>(new MassDensity(1000, MassDensityUnits.Kg_m3), MassDensityUnits.Kg_m3, 1000);
            MolarDensity = new Variable<MolarDensity>(new MolarDensity(1, MolarDensityUnits.Kgmol_m3), MolarDensityUnits.Kgmol_m3, 1000 / 18);
            MolecularWeight = new Variable<UnitLess>(new UnitLess(0), UnitLessUnits.None, 1);
            SuperficialTension = new Variable<SuperficialTension>(new UnitSystem.SuperficialTension(0, SuperficialTensionUnits.dyn_cm), SuperficialTensionUnits.dyn_cm, 1);
            Composition = null!;

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
            MolecularWeight.AddVariableToList += v => _equilibriumCalculator.AddVariable(v);
            SuperficialTension.AddVariableToList += v => _equilibriumCalculator.AddVariable(v);
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
            if (_materialStream.SurfaceTension != null) SuperficialTension.SetValue(_materialStream.SurfaceTension, VariableDefinedBy.StreamCalculated);
            MolecularWeight.SetValue(new UnitLess(_materialStream.MolecularWeight), VariableDefinedBy.StreamCalculated);


           

      
        }
        private StreamStateType GetState()
        {
            // 1. Si hay error, retornar Error
          

            // 2. Si TODO está calculado, retornar Calculated
            if (IsFlowSolved && IsEquilibriumSolved) return StreamStateType.Calculated;

            // 3. Si solo flujos están calculados
            if (IsFlowSolved) return StreamStateType.FlowCalculated;

            // 4. Si solo equilibrio está calculado
            if (IsEquilibriumSolved) return StreamStateType.EquilibriumCalculated;

            // 5. Si la composición está definida pero nada calculado
            if (Composition?.IsValid == true) return StreamStateType.CompositionDefined;

            // 6. Si hay método termodinámico pero composición no válida
            if (Composition != null) return StreamStateType.Initialized;

            // 7. Si no hay nada
            return StreamStateType.Undefined;
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
            Composition = new CompositionOrchestrator(_componentList);
            Composition.OnCompositionChanged += () => _materialStream.SetCompositionData(Composition);
            Composition.OnCompositionChanged += ExecuteEquilibrium;

          
        }

        public void ExecuteEquilibrium() => _equilibriumCalculator.Execute();
        public void ExecuteFlows() => _flowsCalculator.Execute();

      

    }




}