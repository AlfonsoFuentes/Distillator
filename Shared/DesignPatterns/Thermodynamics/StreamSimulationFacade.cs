using Shared.Thermodynamics.Methods;
using UnitSystem;

namespace Shared.DesignPatterns.Thermodynamics
{
    public class StreamSimulationFacade
    {
        private readonly EquilibriumCalculator _equilibriumCalculator;
        private MaterialStream _materialStream = new();
        private StreamState _state;
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
        public ControlledVariable<StreamComposition> StreamCompositionControlled { get;set; } = new()
        {
            Value = new StreamComposition()
        };
        public StreamSimulationFacade()
        {
            _state = new StreamCreatedState();
            _equilibriumCalculator = new EquilibriumCalculator(this, _materialStream);
            TemperatureControlled.ValueChanged += args => _materialStream.SetTemperature(args.NewValue);
            TemperatureControlled.ConstraintsChanged += () => _equilibriumCalculator.OnConstraintsChanged();

            PressureControlled.ValueChanged += args => _materialStream.SetPressure(args.NewValue);
            PressureControlled.ConstraintsChanged += () => _equilibriumCalculator.OnConstraintsChanged();

            MolarFlowControlled.ValueChanged += args => _materialStream.SetMolarFlow(args.NewValue);
            MassFlowControlled.ValueChanged += args => _materialStream.SetMassFlow(args.NewValue);
            VolumetricFlowControlled.ValueChanged += args => _materialStream.SetVolumetricFlow(args.NewValue);

            VaporFractionControlled.ValueChanged += args => _materialStream.SetVaporFraction(args.NewValue);
            VaporFractionControlled.ConstraintsChanged += () => _equilibriumCalculator.OnConstraintsChanged();
            // Suscríbete al chisme del método termodinámico
            ThermodynamicMethod.ValueChanged += args =>
            {
                if (args.NewValue != null)
                {
                    SetThermodynamicMethod(args.NewValue);
                }
            };
            StreamCompositionControlled.ValueChanged += args => _materialStream.SetCompositionData(args.NewValue!);
            StreamCompositionControlled.ConstraintsChanged += () => _equilibriumCalculator.OnConstraintsChanged();
            _equilibriumCalculator.EquilibriumReady += OnEquilibriumReady;
        }
        public StreamState State
        {
            get { return _state; }
            set
            {
                _state = value;

            }
        }
        public void CalculateEquilibrium()
        {
            _equilibriumCalculator.CalculateEquilibrium();
        }

        private void OnEquilibriumReady()
        {
            // 👇 Aquí podrás notificar a la UI cuando el equilibrio esté listo
            // Por ahora, CanCalculate ya refleja el estado
        }
        public void SetThermodynamicMethod(ThermodynamicMethodFullDto methodDto)
        {
            // 👇 El wrapper ControlledVariable YA actualizó Source/SourceId
            // No necesitamos volver a llamar SetValue aquí

            // 1. Actualizar estado interno
            State = new MethodDefinedState();

            // 2. Crear componentes de composición basados en el método
            CreateFacadeComponents(methodDto);

            // 3. Sincronizar con MaterialStream
            _materialStream.SetThermodynamicMethod(methodDto);

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

            _materialStream.ClearThermodynamicMethod();
            State = new StreamCreatedState();
        }
        // 👇 Handlers: reciben el evento y propagan al dominio

    }
}
