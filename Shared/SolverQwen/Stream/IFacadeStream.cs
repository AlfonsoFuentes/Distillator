using Shared.PhaseEnvelopes;
using Shared.ProcessFlowDiagram;
using Shared.PropertiesDtos.Methods;
using Shared.SolverConsecutive;
using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Variables;
using Shared.Thermodynamics.Phases;
using Shared.Thermodynamics.Strategies;
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

        ISolverEquipment EquipmentInlet { get; set; }
        ISolverEquipment EquipmentOutlet { get; set; }
        LiquidPhaseMixture LiquidPhase { get; }
        VaporPhaseMixture VaporPhase { get; }

        StreamStateType State { get; }  // Undefined, EquilibriumCalculated, FlowCalculated, etc.
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
     
        ThermodynamicState ThermodynamicState { get; }

        void SetThermodynamicMethod(ThermodynamicMethodFullDto method);
        void RecalculateFromCurrentState();
        PhaseEnvelopeData EnvelopeCache { get; }
        Task GenerateEnvelopeAsync();
        ThermodynamicMethodFullDto ThermoMethod { get; }
        ISolverTraceSink? TraceSink { get; set; }

    }

    public class FacadeStream : IFacadeStream
    {
        public ISolverEquipment EquipmentInlet { get; set; } = null!;
        public ISolverEquipment EquipmentOutlet { get; set; } = null!;
        public Guid Id { get; set; } = Guid.NewGuid();
        public ThermodynamicState EquilibriumState => _materialStream.CurrentState;
        public LiquidPhaseMixture LiquidPhase => _materialStream.LiquidPhase;
        public VaporPhaseMixture VaporPhase => _materialStream.VaporPhase;
        private readonly IMaterialStream _materialStream;
        private readonly EquilibriumCalculator _equilibriumCalculator;
        private readonly FlowsCalculator _flowsCalculator;

        public ThermodynamicState ThermodynamicState => _materialStream.CurrentState;

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

            if (Composition != null)
            {
                SetComponentVariableNames(Composition.Components);
            }

        }

        private void SetComponentVariableNames(IEnumerable<ComponentFacade> components)
        {
            foreach (var component in components)
            {
                component.MassFraction.SetName($"{Name}.{component.Name}.MassFraction");
                component.MolarFraction.SetName($"{Name}.{component.Name}.MolarFraction");
                component.MassFlow.SetName($"{Name}.{component.Name}.MassFlow");
                component.MolarFlow.SetName($"{Name}.{component.Name}.MolarFlow");
            }
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



        private void OnMassFlowChanged()
        {
            TraceStream("Flow calculation triggered", DescribeStreamCalculationInputs());
            _flowsCalculator.Execute();
            TraceStream("Flow calculation finished", DescribeStreamCalculationOutputs());
        }

        private void OnEquilibriumReady()
        {
            SyncFromMaterialStream();
            IsEquilibriumSolved = true;
        }




        public void SyncFromMaterialStream()
        {

            if (_materialStream.MolarEnthalpy != null) CalculatedVariableSetter.SetStreamCalculated(MolarEnthalpy, _materialStream.MolarEnthalpy);
            if (_materialStream.MassDensity != null) CalculatedVariableSetter.SetStreamCalculated(MassDensity, _materialStream.MassDensity);
            if (_materialStream.MolarDensity != null) CalculatedVariableSetter.SetStreamCalculated(MolarDensity, _materialStream.MolarDensity);
            if (_materialStream.Viscosity != null) CalculatedVariableSetter.SetStreamCalculated(Viscosity, _materialStream.Viscosity);
            if (_materialStream.ThermalConductivity != null) CalculatedVariableSetter.SetStreamCalculated(ThermalConductivity, _materialStream.ThermalConductivity);
            if (_materialStream.MassHeatCapacity != null) CalculatedVariableSetter.SetStreamCalculated(MassCp, _materialStream.MassHeatCapacity);
            if (_materialStream.MolarHeatCapacity != null) CalculatedVariableSetter.SetStreamCalculated(MolarCp, _materialStream.MolarHeatCapacity);
            if (_materialStream.SurfaceTension != null) CalculatedVariableSetter.SetStreamCalculated(SuperficialTension, _materialStream.SurfaceTension);
            CalculatedVariableSetter.SetStreamCalculated(MolecularWeight, new UnitLess(_materialStream.MolecularWeight));





        }
        private StreamStateType GetState()
        {
            // 1. Si hay error, retornar Error
            if (HasError) return StreamStateType.Error;

            // 2. Verificar si TODO está realmente calculado
            bool hasValidComposition = Composition?.IsValid == true;
            bool hasMethodDefined = ThermoMethod != null;
            bool hasTemperatureDefined = Temperature.IsDefined;
            bool hasPressureDefined = Pressure.IsDefined;

            // Flujos están resueltos si al menos uno está definido
            bool hasFlowsSolved = IsFlowSolved &&
                (MassFlow.IsDefined || MolarFlow.IsDefined || VolumetricFlow.IsDefined);

            // Equilibrio está resuelto si tenemos T, P, composición y método
            bool hasEquilibriumSolved = IsEquilibriumSolved &&
                hasTemperatureDefined && hasPressureDefined &&
                hasValidComposition && hasMethodDefined;

            // 3. Si TODO está calculado, retornar Calculated
            if (hasFlowsSolved && hasEquilibriumSolved)
                return StreamStateType.Calculated;

            // 4. Si solo flujos están calculados
            if (hasFlowsSolved)
                return StreamStateType.FlowCalculated;

            // 5. Si solo equilibrio está calculado  
            if (hasEquilibriumSolved)
                return StreamStateType.EquilibriumCalculated;

            // 6. Si la composición está definida pero nada calculado
            if (hasValidComposition)
                return StreamStateType.CompositionDefined;

            // 7. Si hay método termodinámico pero composición no válida
            if (hasMethodDefined)
                return StreamStateType.Initialized;

            // 8. Si no hay nada
            return StreamStateType.Undefined;
        }
        public ThermodynamicMethodFullDto ThermoMethod { get; private set; } = null!;
        public ISolverTraceSink? TraceSink { get; set; }
        public void SetThermodynamicMethod(ThermodynamicMethodFullDto method)
        {
            if (IsSameThermodynamicMethod(method))
            {
                return;
            }

            ThermoMethod = method;
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
            SetComponentVariableNames(_componentList);
            Composition.OnCompositionChanged += () => _materialStream.SetCompositionData(Composition);
            Composition.OnCompositionChanged += ExecuteEquilibrium;
            Composition.OnCompositionChanged += () => EnvelopeCache = null!;


        }

        private bool IsSameThermodynamicMethod(ThermodynamicMethodFullDto method)
        {
            if (ThermoMethod == null || method == null)
            {
                return false;
            }

            if (ThermoMethod.Id != method.Id ||
                ThermoMethod.VaporModel != method.VaporModel ||
                ThermoMethod.LiquidModel != method.LiquidModel ||
                !string.Equals(ThermoMethod.Name, method.Name, StringComparison.Ordinal))
            {
                return false;
            }

            if (!HasSameComponents(ThermoMethod.Components, method.Components))
            {
                return false;
            }

            return HasSameBinaryParameters(ThermoMethod.BinaryParameters, method.BinaryParameters);
        }

        private static bool HasSameComponents(
            IReadOnlyList<MethodComponentFullDto> currentComponents,
            IReadOnlyList<MethodComponentFullDto> newComponents)
        {
            if (currentComponents.Count != newComponents.Count)
            {
                return false;
            }

            for (var i = 0; i < currentComponents.Count; i++)
            {
                var current = currentComponents[i];
                var next = newComponents[i];

                if (current.ComponentId != next.ComponentId ||
                    current.MatrixIndex != next.MatrixIndex ||
                    !string.Equals(current.ComponentName, next.ComponentName, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasSameBinaryParameters(
            IReadOnlyList<BinaryInteractionParameterDto> currentParameters,
            IReadOnlyList<BinaryInteractionParameterDto> newParameters)
        {
            if (currentParameters.Count != newParameters.Count)
            {
                return false;
            }

            for (var i = 0; i < currentParameters.Count; i++)
            {
                var current = currentParameters[i];
                var next = newParameters[i];

                if (current.ComponentI_Id != next.ComponentI_Id ||
                    current.ComponentJ_Id != next.ComponentJ_Id ||
                    current.ParameterType != next.ParameterType ||
                    Math.Abs(current.Value - next.Value) > 1e-12)
                {
                    return false;
                }
            }

            return true;
        }

        public void ExecuteEquilibrium()
        {
            TraceStream("Equilibrium calculation triggered", DescribeStreamCalculationInputs());
            _equilibriumCalculator.Execute();
            TraceStream("Equilibrium calculation finished", DescribeStreamCalculationOutputs());
        }

        public void ExecuteFlows()
        {
            TraceStream("Flow calculation triggered", DescribeStreamCalculationInputs());
            _flowsCalculator.Execute();
            TraceStream("Flow calculation finished", DescribeStreamCalculationOutputs());
        }

        public void RecalculateFromCurrentState()
        {
            TraceStream("Recalculate from current state started", DescribeStreamCalculationInputs());

            if (Temperature.IsDefined)
            {
                _materialStream.SetTemperature(Temperature.Value);
            }

            if (Pressure.IsDefined)
            {
                _materialStream.SetPressure(Pressure.Value);
            }

            if (VaporFraction.IsDefined)
            {
                _materialStream.SetVaporFraction(VaporFraction.Value);
            }

            if (MassEnthalpy.IsDefined && MassEnthalpy.DataProcedence != VariableDefinedBy.StreamCalculated)
            {
                _materialStream.MassEnthalpy = MassEnthalpy.Value;
            }

            if (Composition != null)
            {
                _materialStream.SetCompositionData(Composition);
                EnvelopeCache = null!;
            }

            ExecuteEquilibrium();
            ExecuteFlows();
            TraceStream("Recalculate from current state finished", DescribeStreamCalculationOutputs());
        }

        private void TraceStream(string message, string? detail = null)
        {
            if (!ShouldTraceThisStream())
            {
                return;
            }

            TraceSink?.TraceStream($"{Name}: {message}", detail);
        }

        private bool ShouldTraceThisStream()
        {
            return TraceSink?.IsStreamTraceEnabled == true &&
                   DiagnosticStreamNames.Contains(Name, StringComparer.OrdinalIgnoreCase);
        }

        private static readonly string[] DiagnosticStreamNames =
        [
            "S-123",
            "S-126",
            "S-139",
            "S-145",
            "S-146",
            "S-148",
            "S-149",
            "S-155"
        ];

        private string DescribeStreamCalculationInputs()
        {
            return $"state={State}; T={DescribeVariable(Temperature)}; P={DescribeVariable(Pressure)}; VF={DescribeVariable(VaporFraction)}; MF={DescribeVariable(MassFlow)}; MolF={DescribeVariable(MolarFlow)}; VolF={DescribeVariable(VolumetricFlow)}; Q={DescribeVariable(EnthalpyFlow)}; Hm={DescribeVariable(MassEnthalpy)}; compositionValid={Composition?.IsValid == true}";
        }

        private string DescribeStreamCalculationOutputs()
        {
            return $"state={State}; eq={IsEquilibriumSolved}; flow={IsFlowSolved}; T={DescribeVariable(Temperature)}; P={DescribeVariable(Pressure)}; VF={DescribeVariable(VaporFraction)}; MF={DescribeVariable(MassFlow)}; MolF={DescribeVariable(MolarFlow)}; VolF={DescribeVariable(VolumetricFlow)}; Q={DescribeVariable(EnthalpyFlow)}; Hm={DescribeVariable(MassEnthalpy)}";
        }

        private static string DescribeVariable(IVariable variable)
        {
            return variable.IsDefined
                ? $"{variable.ToUiString("F2")} [{variable.DataProcedence}]"
                : "<Not defined> [Undefined]";
        }

        // =========================================================
        // CACHÉ Y ESTADO DE LA ENVOLVENTE DE FASES
        // =========================================================
        // La propiedad pública de solo lectura para la UI
        public PhaseEnvelopeData EnvelopeCache { get; private set; } = null!;

        // El método que la UI llamará bajo demanda
        public async Task GenerateEnvelopeAsync()
        {
            // Llamamos al motor que creamos en el Paso 2
            EnvelopeCache = await PhaseEnvelopeGenerator.GenerateAsync(this, 50);
        }
        public async Task PostSolveAsync()
        {
            // Solo si hay cambios en composición, recalcula la envolvente
            // Opcional: podrías agregar una lógica para verificar si la composición cambió desde el último PostSolve
            //await TriggerEnvelopeCalculationAsync();

            // Aquí podrías añadir cualquier otro post-cálculo de la corriente
        }

    }




}
