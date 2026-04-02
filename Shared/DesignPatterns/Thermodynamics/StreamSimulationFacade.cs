using Shared.DesignPatterns.Thermodynamics.Phases;
using Shared.DesignPatterns.Thermodynamics.Strategies.Equlibriums;
using Shared.DesignPatterns.Thermodynamics.Strategies.Flows;
using Shared.Thermodynamics.Methods;
using UnitSystem;

namespace Shared.DesignPatterns.Thermodynamics
{
    public class StreamSimulationFacade
    {

        private List<IControlledVariable> _calculatedVariables = new List<IControlledVariable>();
        private List<IControlledVariable> _BulkVariables = new List<IControlledVariable>();
        private List<IControlledVariable> _FlowVariables = new List<IControlledVariable>();

        private readonly EquilibriumCalculator _equilibriumCalculator;
        private readonly FlowsCalculator _flowsCalculator;
        public MaterialStream MaterialStream { get; } = new();
        private StreamStateType _state;

        public ThermodynamicState EquilibriumState => MaterialStream.CurrentState;
        public string Name { get; set; } = string.Empty;
        public ControlledVariable<Temperature> TemperatureControlled { get; set; } = new()
        {
            Value = new Temperature(25, TemperatureUnits.DegreeCelcius)
        };

        public ControlledVariable<Pressure> PressureControlled { get; set; } = new()
        {
            Value = new Pressure(1, PressureUnits.Bar)
        };

        public ControlledVariable<MolarFlow> MolarFlowControlled { get; set; } = new()
        {
            Value = new MolarFlow(0, MolarFlowUnits.Kgmol_hr)
        };

        public ControlledVariable<MassFlow> MassFlowControlled { get; set; } = new()
        {
            Value = new MassFlow(0, MassFlowUnits.Kg_hr)
        };

        public ControlledVariable<VolumetricFlow> VolumetricFlowControlled { get; set; } = new()
        {
            Value = new VolumetricFlow(0, VolumetricFlowUnits.m3_hr)
        };

        public ControlledVariable<ThermalConductivity> ThermalConductivity { get; set; } = new()
        {
            Value = new ThermalConductivity(0, ThermalConductivityUnits.kW_m_K),
            IsDefinedByUI = false,
        };
        public ControlledVariable<Viscosity> Viscosity { get; set; } = new()
        {
            Value = new Viscosity(0, ViscosityUnits.cPoise),
            IsDefinedByUI = false,
        };
        public ControlledVariable<MassEntropy> MassCp { get; set; } = new()
        {
            Value = new MassEntropy(0, MassEntropyUnits.Kcal_Kg_C),
            IsDefinedByUI = false,
        };
        public ControlledVariable<MolarEntropy> MolarCp { get; set; } = new()
        {
            Value = new MolarEntropy(0, MolarEntropyUnits.Kcal_Kgmol_C),
            IsDefinedByUI = false,
        };
        public ControlledVariable<MassEnergy> MassEnthalpy { get; set; } = new()
        {
            Value = new MassEnergy(0, MassEnergyUnits.Kcal_Kg),
            IsDefinedByUI = false,
        };
        public ControlledVariable<MolarEnergy> MolarEnthapy { get; set; } = new()
        {
            Value = new MolarEnergy(0, MolarEnergyUnits.Kcal_Kgmol),
            IsDefinedByUI = false,
        };

        public ControlledVariable<MassDensity> MassDensity { get; set; } = new()
        {
            Value = new MassDensity(0, MassDensityUnits.Kg_m3),
            IsDefinedByUI = false,
        };
        public ControlledVariable<MolarDensity> MolarDensity { get; set; } = new()
        {
            Value = new MolarDensity(0, MolarDensityUnits.Kgmol_m3),
            IsDefinedByUI = false,
        };

        public ControlledVariable<EnergyFlow> EnthalpyFlow { get; set; } = new()
        {
            Value = new EnergyFlow(0, EnergyFlowUnits.Kcal_hr),
            IsDefinedByUI=false,

        };

        public ControlledVariable<SuperficialTension> SuperficialTension { get; set; } = new()
        {
            Value = new SuperficialTension(0, SuperficialTensionUnits.dyn_cm),
            IsDefinedByUI = false,

        };
        // ─────────────────────────────────────────────────────────
        // 🔹 Value Types (primitivos - default = 0)
        // ─────────────────────────────────────────────────────────
        public ControlledVariable<double> VaporFractionControlled { get; set; } = new();
        // 👆 double es struct → default(double) = 0.0 → Value nunca es null

        // ─────────────────────────────────────────────────────────
        // 🔹 DTO Types (clases complejas)
        // ─────────────────────────────────────────────────────────

        // Nullable: puede ser null inicialmente
        public ControlledVariable<ThermodynamicMethodFullDto?> ThermodynamicMethod { get; set; } = new();

        // No nullable: inicializar con instancia vacía
        public ControlledVariable<StreamComposition> StreamCompositionControlled { get; set; } = new()
        {
            Value = new StreamComposition()
        };
        public StreamSimulationFacade()
        {
            _state = StreamStateType.Created;
            _equilibriumCalculator = new EquilibriumCalculator(this);
            _flowsCalculator=new  FlowsCalculator(this);
            TemperatureControlled.ValueChanged += args => { MaterialStream.SetTemperature(args.NewValue); };
            TemperatureControlled.ConstraintsChanged += () => _equilibriumCalculator.OnConstraintsChanged();

            PressureControlled.ValueChanged += args => MaterialStream.SetPressure(args.NewValue);
            PressureControlled.ConstraintsChanged += () => _equilibriumCalculator.OnConstraintsChanged();

            // ─────────────────────────────────────────────────────────
            // 🔹 FLUJOS: Agregar ConstraintsChanged (FALTABAN)
            // ─────────────────────────────────────────────────────────

            MassFlowControlled.ValueChanged += args => _flowsCalculator.OnConstraintsChanged();
            MassFlowControlled.ConstraintsChanged += () => _flowsCalculator.OnConstraintsChanged();  // 👈 AGREGAR

            MolarFlowControlled.ValueChanged += args => _flowsCalculator.OnConstraintsChanged();
            MolarFlowControlled.ConstraintsChanged += () => _flowsCalculator.OnConstraintsChanged();  // 👈 AGREGAR

            VolumetricFlowControlled.ValueChanged += args => _flowsCalculator.OnConstraintsChanged();
            VolumetricFlowControlled.ConstraintsChanged += () => _flowsCalculator.OnConstraintsChanged();  // 👈 AGREGAR

            VaporFractionControlled.ValueChanged += args => MaterialStream.SetVaporFraction(args.NewValue);
            VaporFractionControlled.ConstraintsChanged += () => _equilibriumCalculator.OnConstraintsChanged();
            // Suscríbete al chisme del método termodinámico
            ThermodynamicMethod.ValueChanged += args =>
            {
                if (args.NewValue != null)
                {
                    SetThermodynamicMethod(args.NewValue);
                }
            };
            StreamCompositionControlled.ValueChanged += args => MaterialStream.SetCompositionData(args.NewValue!);
            StreamCompositionControlled.ConstraintsChanged += () => _equilibriumCalculator.OnConstraintsChanged();
            _equilibriumCalculator.EquilibriumReady += OnEquilibriumReady;
            _equilibriumCalculator.FlowsReady += _flowsCalculator.OnConstraintsChanged;
            _BulkVariables.Add(ThermalConductivity);
            _BulkVariables.Add(Viscosity);
            _BulkVariables.Add(MassCp);
            _BulkVariables.Add(MolarCp);
            _BulkVariables.Add(MolarEnthapy);
            _BulkVariables.Add(MassEnthalpy);
            _BulkVariables.Add(MassDensity);
            _BulkVariables.Add(MolarDensity);
            _BulkVariables.Add(SuperficialTension);
        }
        public StreamStateType State
        {
            get { return _state; }
            set
            {
                _state = value;

            }
        }


        private void OnEquilibriumReady()
        {
            MaterialStream.CalculateBulkProperties(TemperatureControlled.Value!,PressureControlled.Value!);
            ThermalConductivity.SetValueCalculated(MaterialStream.ThermalConductivity, Name);
            Viscosity.SetValueCalculated(MaterialStream.Viscosity, Name);
            MassCp.SetValueCalculated(MaterialStream.MassHeatCapacity, Name);
            MolarCp.SetValueCalculated(MaterialStream.MolarHeatCapacity, Name);
            MolarEnthapy.SetValueCalculated(MaterialStream.MolarEnthalpy, Name);
            MassEnthalpy.SetValueCalculated(MaterialStream.MassEnthalpy, Name);
            MassDensity.SetValueCalculated(MaterialStream.MassDensity, Name);
            MolarDensity.SetValueCalculated(MaterialStream.MolarDensity, Name);
            SuperficialTension.SetValueCalculated(MaterialStream.SurfaceTension, Name);
            _state = StreamStateType.EquilibriumCalculated;



        }
        public void SetThermodynamicMethod(ThermodynamicMethodFullDto methodDto)
        {
            // 👇 El wrapper ControlledVariable YA actualizó Source/SourceId
            // No necesitamos volver a llamar SetValue aquí

            // 1. Actualizar estado interno
            State = StreamStateType.MethodDefined;

            // 2. Crear componentes de composición basados en el método
            CreateFacadeComponents(methodDto);

            // 3. Sincronizar con MaterialStream
            MaterialStream.SetThermodynamicMethod(methodDto);

            // 4. Disparar re-evaluación de restricciones (opcional, si cambia el método)
            _equilibriumCalculator.OnConstraintsChanged();
        }
        public void CreateFacadeComponents(ThermodynamicMethodFullDto methodDto)
        {
            // 👇 Asegurar que existe una instancia de StreamComposition
            var data = StreamCompositionControlled.Value;

            if (data != null)
            {
                data.Components.Clear();
                foreach (var comp in methodDto.Components)
                {
                    ComponentComposition newcompo = new ComponentComposition();
                    newcompo.ComponentId = comp.ComponentId;
                    newcompo.ComponentName = comp.ComponentName;
                    newcompo.MolecularWeight = comp.FullData.MolecularWeight;
                    data.Components.Add(newcompo);
                }
            }

        }

        // ✅ Caso 2: "Des-definir" método (nueva funcionalidad)
        public void ClearThermodynamicMethod(MethodSource source, string sourceId = "UI")
        {
            // 👇 SetValue acepta null explícitamente, sin necesidad de null!
            ThermodynamicMethod.ClearValue();

            MaterialStream.ClearThermodynamicMethod();
            State = StreamStateType.Created;
        }

        public void AddCalculatedVariable(IControlledVariable controlledVariable)
        {
            if (controlledVariable != null && !_calculatedVariables.Contains(controlledVariable))
            {
                _calculatedVariables.Add(controlledVariable);
            }
        }
        public void AddFlowVariable(IControlledVariable controlledVariable)
        {
            if (controlledVariable != null && !_FlowVariables.Contains(controlledVariable))
            {
                _FlowVariables.Add(controlledVariable);
            }
        }
        public void ResetCalculatedVariable()
        {
            foreach (var controlledVariable in _calculatedVariables)
            {
                controlledVariable.Source = MethodSource.None;
                controlledVariable.SourceId = string.Empty;
            }
            foreach (var controlledVariable in _BulkVariables)
            {
                controlledVariable.Source = MethodSource.None;
                controlledVariable.SourceId = string.Empty;
            }
           
            _calculatedVariables.Clear();
        
        }
        public void ResetFlowsCalculatedVariable()
        {
            
            foreach (var controlledVariable in _FlowVariables)
            {
                controlledVariable.Source = MethodSource.None;
                controlledVariable.SourceId = string.Empty;
            }
         
            _FlowVariables.Clear();
        }
 
       

    }
}
