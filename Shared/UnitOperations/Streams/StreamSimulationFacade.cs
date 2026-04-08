using Shared.PropertiesDtos.Methods;
using Shared.Thermodynamics.ControlledVariables;
using Shared.Thermodynamics.Phases;
using Shared.Thermodynamics.Strategies.Equlibriums;
using Shared.Thermodynamics.Strategies.Flows;
using UnitSystem;

namespace Shared.UnitOperations.Streams
{
    public class StreamSimulationFacade : IEquipmentFacade
    {

        private List<IControlledVariable> _calculatedVariables = new List<IControlledVariable>();
        private List<IControlledVariable> _BulkVariables = new List<IControlledVariable>();
        private List<IControlledVariable> _FlowVariables = new List<IControlledVariable>();

        private readonly EquilibriumCalculator _equilibriumCalculator;
        private readonly FlowsCalculator _flowsCalculator;
        public MaterialStream MaterialStream { get; } = new();


        public ThermodynamicState EquilibriumState => MaterialStream.CurrentState;
        public string Name { get; set; } = string.Empty;
        public ControlledAmountVariable<Temperature> TemperatureControlled { get; set; }
                     = new ControlledAmountVariable<Temperature>(
                         preferredUnit: TemperatureUnits.DegreeCelcius,  // 👇 OBLIGATORIO
                         initialValue: new Temperature(25, TemperatureUnits.DegreeCelcius)
                     );
        public ControlledAmountVariable<Pressure> PressureControlled { get; set; }
                   = new ControlledAmountVariable<Pressure>(
                       preferredUnit: PressureUnits.Bara,  // 👇 OBLIGATORIO
                       initialValue: new Pressure(1, PressureUnits.Bara)
                   );

        public ControlledAmountVariable<MolarFlow> MolarFlowControlled { get; set; }
                  = new ControlledAmountVariable<MolarFlow>(
                      preferredUnit: MolarFlowUnits.Kgmol_hr,  // 👇 OBLIGATORIO
                      initialValue: new MolarFlow(0, MolarFlowUnits.Kgmol_hr)
                  );

        public ControlledAmountVariable<MassFlow> MassFlowControlled { get; set; }
               = new ControlledAmountVariable<MassFlow>(
                   preferredUnit: MassFlowUnits.Kg_hr,  // 👇 OBLIGATORIO
                   initialValue: new MassFlow(0, MassFlowUnits.Kg_hr)
               );

        public ControlledAmountVariable<VolumetricFlow> VolumetricFlowControlled { get; set; }
            = new ControlledAmountVariable<VolumetricFlow>(
                preferredUnit: VolumetricFlowUnits.m3_hr,  // 👇 OBLIGATORIO
                initialValue: new VolumetricFlow(0, VolumetricFlowUnits.m3_hr)
            );

        public ControlledAmountVariable<ThermalConductivity> ThermalConductivity { get; set; }
           = new ControlledAmountVariable<ThermalConductivity>(
               preferredUnit: ThermalConductivityUnits.W_m_K,  // 👇 OBLIGATORIO
               initialValue: new ThermalConductivity(0, ThermalConductivityUnits.kW_m_K)
           );

        public ControlledAmountVariable<Viscosity> Viscosity { get; set; }
         = new ControlledAmountVariable<Viscosity>(
             preferredUnit: ViscosityUnits.cPoise,  // 👇 OBLIGATORIO
             initialValue: new Viscosity(0, ViscosityUnits.cPoise)

         );
        public ControlledAmountVariable<MassEntropy> MassCp { get; set; }
       = new ControlledAmountVariable<MassEntropy>(
           preferredUnit: MassEntropyUnits.Kcal_Kg_C,  // 👇 OBLIGATORIO
           initialValue: new MassEntropy(0, MassEntropyUnits.Kcal_Kg_C)

       );

        public ControlledAmountVariable<MolarEntropy> MolarCp { get; set; }
      = new ControlledAmountVariable<MolarEntropy>(
          preferredUnit: MolarEntropyUnits.Kcal_Kgmol_C,  // 👇 OBLIGATORIO
          initialValue: new MolarEntropy(0, MolarEntropyUnits.Kcal_Kgmol_C)

      );

        public ControlledAmountVariable<MassEnergy> MassEnthalpy { get; set; }
     = new ControlledAmountVariable<MassEnergy>(
         preferredUnit: MassEnergyUnits.Kcal_Kg,  // 👇 OBLIGATORIO
         initialValue: new MassEnergy(0, MassEnergyUnits.Kcal_Kg)

     );
        public ControlledAmountVariable<MolarEnergy> MolarEnthapy { get; set; }
     = new ControlledAmountVariable<MolarEnergy>(
         preferredUnit: MolarEnergyUnits.Kcal_Kgmol,  // 👇 OBLIGATORIO
         initialValue: new MolarEnergy(0, MolarEnergyUnits.Kcal_Kgmol)

     );

        public ControlledAmountVariable<MassDensity> MassDensity { get; set; }
     = new ControlledAmountVariable<MassDensity>(
         preferredUnit: MassDensityUnits.Kg_m3,  // 👇 OBLIGATORIO
         initialValue: new MassDensity(0, MassDensityUnits.Kg_m3)

     );
        public ControlledAmountVariable<MolarDensity> MolarDensity { get; set; }
    = new ControlledAmountVariable<MolarDensity>(
        preferredUnit: MolarDensityUnits.Kgmol_m3,  // 👇 OBLIGATORIO
        initialValue: new MolarDensity(0, MolarDensityUnits.Kgmol_m3)

    );
        public ControlledAmountVariable<EnergyFlow> EnthalpyFlow { get; set; }
      = new ControlledAmountVariable<EnergyFlow>(
          preferredUnit: EnergyFlowUnits.Kcal_hr,  // 👇 OBLIGATORIO
          initialValue: new EnergyFlow(0, EnergyFlowUnits.Kcal_hr)

      );

        public ControlledAmountVariable<SuperficialTension> SuperficialTension { get; set; }
     = new ControlledAmountVariable<SuperficialTension>(
         preferredUnit: SuperficialTensionUnits.dyn_cm,  // 👇 OBLIGATORIO
         initialValue: new SuperficialTension(0, SuperficialTensionUnits.dyn_cm)

     );


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
        public ControlledVariable<StreamComposition> StreamCompositionControlled { get; set; } = new ControlledVariable<StreamComposition>(new StreamComposition());
        public StreamSimulationFacade()
        {
            State = StreamStateType.Created;
            _equilibriumCalculator = new EquilibriumCalculator(this);
            _flowsCalculator = new FlowsCalculator(this);
            TemperatureControlled.ValueChanged += args => { MaterialStream.SetTemperature(args.NewValue); };
            TemperatureControlled.ConstraintsChanged += () => _equilibriumCalculator.OnConstraintsChanged();

            PressureControlled.ValueChanged += args => MaterialStream.SetPressure(args.NewValue);
            PressureControlled.ConstraintsChanged += () => _equilibriumCalculator.OnConstraintsChanged();




            MassFlowControlled.ConstraintsChanged += () => _flowsCalculator.OnConstraintsChanged();  // 👈 AGREGAR


            MolarFlowControlled.ConstraintsChanged += () => _flowsCalculator.OnConstraintsChanged();  // 👈 AGREGAR


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
        public StreamStateType State { get; set; }


        private void OnEquilibriumReady()
        {
            MaterialStream.CalculateBulkProperties(TemperatureControlled.Value!, PressureControlled.Value!);
            ThermalConductivity.SetValueCalculated(MaterialStream.ThermalConductivity, Name);
            Viscosity.SetValueCalculated(MaterialStream.Viscosity, Name);
            MassCp.SetValueCalculated(MaterialStream.MassHeatCapacity, Name);
            MolarCp.SetValueCalculated(MaterialStream.MolarHeatCapacity, Name);
            MolarEnthapy.SetValueCalculated(MaterialStream.MolarEnthalpy, Name);
            MassEnthalpy.SetValueCalculated(MaterialStream.MassEnthalpy, Name);
            MassDensity.SetValueCalculated(MaterialStream.MassDensity, Name);
            MolarDensity.SetValueCalculated(MaterialStream.MolarDensity, Name);
            SuperficialTension.SetValueCalculated(MaterialStream.SurfaceTension, Name);
            State = StreamStateType.EquilibriumCalculated;



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
            State = MaterialStream.CurrentState != ThermodynamicState.Undefined ? StreamStateType.EquilibriumCalculated : StreamStateType.MethodDefined;

            foreach (var controlledVariable in _FlowVariables)
            {
                controlledVariable.Source = MethodSource.None;
                controlledVariable.SourceId = string.Empty;
            }

            _FlowVariables.Clear();
        }
        public Guid Id { get; set; } = Guid.NewGuid();
        public string StatusText => State switch
        {
            StreamStateType.Created => "Ready",
            StreamStateType.MethodDefined => "Underspecified",
            StreamStateType.EquilibriumCalculated => "Equilibrium Solved",
            StreamStateType.StreamCalculated => "Converged",
            _ => "Unknown"
        };

        // 3. Color de Estado para pintar la flecha y el texto del Tooltip
        public string StatusColor => State switch
        {
            StreamStateType.Created => "#CBD5E0",              // Gris tenue
            StreamStateType.MethodDefined => "#F6AD55",        // Ámbar/Naranja
            StreamStateType.EquilibriumCalculated => "#63B3ED", // Azul industrial
            StreamStateType.StreamCalculated => "#34D399",      // Verde "All Clear"
            _ => "#CBD5E0"
        };

        // 4. El diccionario mágico (¡Ya lo tenías perfecto, solo lo dejo aquí por completitud!)
        public Dictionary<string, string> GetQuickViewData()
        {
            var data = new Dictionary<string, string>();

            // Usamos interpolación y formateo (ej. limitando decimales si tuvieran)
            data.Add("Temperature", TemperatureControlled.Value?.ToString("GG", null) ?? "--");
            data.Add("Pressure", PressureControlled.Value?.ToString("GG", null) ?? "--");
            data.Add("Flow", MassFlowControlled.Value?.ToString("GG", null) ?? "--");

            return data;
        }
        public IEquipmentFacade? SourceEquipment { get; private set; }

        // El equipo hacia donde va el fluido (Ej: El Intercambiador de Calor)
        public IEquipmentFacade? TargetEquipment { get; private set; }

        public void AttachConnection(string portName, IEquipmentFacade connectedFacade)
        {
            if (portName == "Inlet") SourceEquipment = connectedFacade;
            else if (portName == "Outlet") TargetEquipment = connectedFacade;

            // Disparamos el evento (Placeholder para la estrategia de cálculo)
            OnTopologyChanged?.Invoke();

            // Lógica de validación preliminar
            EvaluateAutoCalculation();
        }

        public void DetachConnection(string portName)
        {
            if (portName == "Inlet") SourceEquipment = null;
            else if (portName == "Outlet") TargetEquipment = null;

            OnTopologyChanged?.Invoke();
            EvaluateAutoCalculation();
        }

        private void EvaluateAutoCalculation()
        {
            // Placeholder: Si la corriente tiene origen y destino, 
            // simulamos que ya puede converger.
            if (SourceEquipment != null && TargetEquipment != null)
            {
                State = StreamStateType.StreamCalculated;
            }
            else if (SourceEquipment != null || TargetEquipment != null)
            {
                State = StreamStateType.MethodDefined;
            }
            else
            {
                State = StreamStateType.Created;
            }
        }
        public  Action? OnTopologyChanged{ get; set; }
    }
}
