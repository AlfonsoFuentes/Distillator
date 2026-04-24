using Shared.ProcessFlowDiagram;
using Shared.PropertiesDtos.Methods;
using Shared.Thermodynamics.ControlledVariables;
using Shared.Thermodynamics.Phases;
using Shared.Thermodynamics.Strategies.Equlibriums;
using Shared.Thermodynamics.Strategies.Flows;
using Shared.UnitOperations.Basiss;
using UnitSystem;

namespace Shared.UnitOperations.Streams
{
    public class StreamSimulationFacade : IFacade
    {
        public void ResetCalculatedVariable()
        {
            ResetEquilibriumCalculatedVariable();
            ResetFlowsCalculatedVariable();

        }

        private List<IControlledVariable> _EquilibriumCalculatedVariables = new List<IControlledVariable>();

        private List<IControlledVariable> _FlowVariables = new List<IControlledVariable>();

        private readonly EquilibriumCalculator _equilibriumCalculator;
        private readonly FlowsCalculator _flowsCalculator;
        public MaterialStream MaterialStream { get; } = new();
        public Action<IFacade>? OnExecuteSolver { get; set; }
        public void Calculate()
        {
            // 1. Obligamos al motor de equilibrio (Flash) a correr con los datos que el usuario digitó
            _equilibriumCalculator.OnConstraintsChanged();

            // 2. Obligamos al balance de masa/flujos a correr
            _flowsCalculator.OnConstraintsChanged();
        }
        public ThermodynamicState EquilibriumState => MaterialStream.CurrentState;
        public string Name { get; set; } = string.Empty;
        public ControlledAmountVariable<Temperature> Temperature { get; set; }
                     = new ControlledAmountVariable<Temperature>(
                         preferredUnit: TemperatureUnits.DegreeCelcius,  // 👇 OBLIGATORIO
                         initialValue: new Temperature(25, TemperatureUnits.DegreeCelcius)
                     );
        public ControlledAmountVariable<Pressure> Pressure { get; set; }
                   = new ControlledAmountVariable<Pressure>(
                       preferredUnit: PressureUnits.Bara,  // 👇 OBLIGATORIO
                       initialValue: new Pressure(1, PressureUnits.Bara)
                   );

        public ControlledAmountVariable<MolarFlow> MolarFlow { get; set; }
                  = new ControlledAmountVariable<MolarFlow>(
                      preferredUnit: MolarFlowUnits.Kgmol_hr,  // 👇 OBLIGATORIO
                      initialValue: new MolarFlow(0, MolarFlowUnits.Kgmol_hr)
                  );

        public ControlledAmountVariable<MassFlow> MassFlow { get; set; }
               = new ControlledAmountVariable<MassFlow>(
                   preferredUnit: MassFlowUnits.Kg_hr,  // 👇 OBLIGATORIO
                   initialValue: new MassFlow(0, MassFlowUnits.Kg_hr)
               );

        public ControlledAmountVariable<VolumetricFlow> VolumetricFlow { get; set; }
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
        public ControlledAmountVariable<MolarEnergy> MolarEnthalpy { get; set; }
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



   //     public ControlledAmountVariable<MassEntropy> MassEntropy { get; set; }
   //  = new ControlledAmountVariable<MassEntropy>(
   //      preferredUnit: MassEntropyUnits.Kcal_Kg_C,  // 👇 OBLIGATORIO
   //      initialValue: new MassEntropy(0, MassEntropyUnits.Kcal_Kg_C)

   //  );

   //     public ControlledAmountVariable<MolarEntropy> MolarEntropy { get; set; }
   //= new ControlledAmountVariable<MolarEntropy>(
   //     preferredUnit: MolarEntropyUnits.Kcal_Kgmol_C,  // 👇 OBLIGATORIO
   //       initialValue: new MolarEntropy(0, MolarEntropyUnits.Kcal_Kgmol_C)

   //);

        // ─────────────────────────────────────────────────────────
        // 🔹 Value Types (primitivos - default = 0)
        // ─────────────────────────────────────────────────────────
        public ControlledVariable<double> VaporFraction { get; set; } = new();
        // 👆 double es struct → default(double) = 0.0 → Value nunca es null

        // ─────────────────────────────────────────────────────────
        // 🔹 DTO Types (clases complejas)
        // ─────────────────────────────────────────────────────────

        // Nullable: puede ser null inicialmente
        public ControlledVariable<ThermodynamicMethodFullDto?> ThermodynamicMethod { get; set; } = new();

        // No nullable: inicializar con instancia vacía
        public ControlledVariable<StreamComposition> StreamComposition { get; set; } = new ControlledVariable<StreamComposition>(new StreamComposition());
        public StreamSimulationFacade()
        {

            _equilibriumCalculator = new EquilibriumCalculator(this);
            _flowsCalculator = new FlowsCalculator(this);
            Temperature.StateChanged += args => { MaterialStream.SetTemperature(args.NewValue); };
            Temperature.OnExecuteSolver += EvaluateSolverTrigger;

            Temperature.LocalCalculationRequested += () => _equilibriumCalculator.OnConstraintsChanged();
            Temperature.AddCalculatedVariable += AddEquilibriumCalculatedVariable;

            Pressure.StateChanged += args => { MaterialStream.SetPressure(args.NewValue); };
            Pressure.LocalCalculationRequested += () => _equilibriumCalculator.OnConstraintsChanged();
            Pressure.OnExecuteSolver += EvaluateSolverTrigger;
            Pressure.AddCalculatedVariable += AddEquilibriumCalculatedVariable;

            VaporFraction.StateChanged += args => { MaterialStream.SetVaporFraction(args.NewValue); };
            VaporFraction.LocalCalculationRequested += () => _equilibriumCalculator.OnConstraintsChanged();
            VaporFraction.OnExecuteSolver += EvaluateSolverTrigger;
            VaporFraction.AddCalculatedVariable += AddEquilibriumCalculatedVariable;

            // Suscríbete al chisme del método termodinámico
            ThermodynamicMethod.StateChanged += args =>
            {
                if (args.NewValue != null)
                {
                    SetThermodynamicMethod(args.NewValue);

                }
            };
            ThermodynamicMethod.OnExecuteSolver += EvaluateSolverTrigger;



            MassFlow.LocalCalculationRequested += () => _flowsCalculator.OnConstraintsChanged();  // 👈 AGREGAR
            MassFlow.OnExecuteSolver += EvaluateSolverTrigger;
            MassFlow.AddCalculatedVariable += AddFlowVariable;


            MolarFlow.LocalCalculationRequested += () => _flowsCalculator.OnConstraintsChanged();  // 👈 AGREGAR
            MolarFlow.OnExecuteSolver += EvaluateSolverTrigger;
            MolarFlow.AddCalculatedVariable += AddFlowVariable;

            VolumetricFlow.LocalCalculationRequested += () => _flowsCalculator.OnConstraintsChanged();  // 👈 AGREGAR
            VolumetricFlow.OnExecuteSolver += EvaluateSolverTrigger;
            VolumetricFlow.AddCalculatedVariable += AddFlowVariable;

            StreamComposition.StateChanged += args => { MaterialStream.SetCompositionData(args.NewValue!); };
            StreamComposition.OnExecuteSolver += EvaluateSolverTrigger;
            StreamComposition.LocalCalculationRequested += () => _equilibriumCalculator.OnConstraintsChanged();


            _equilibriumCalculator.EquilibriumReady += OnEquilibriumReady;
            _equilibriumCalculator.FlowsReady += _flowsCalculator.OnConstraintsChanged;

            MolarEnthalpy.LocalCalculationRequested += () => _equilibriumCalculator.OnConstraintsChanged();
            MolarEnthalpy.AddCalculatedVariable += AddEquilibriumCalculatedVariable;

            //MolarEntropy.LocalCalculationRequested += () => _equilibriumCalculator.OnConstraintsChanged();
            //MolarEntropy.AddCalculatedVariable += AddEquilibriumCalculatedVariable;


            ThermalConductivity.AddCalculatedVariable += AddEquilibriumCalculatedVariable;
            Viscosity.AddCalculatedVariable += AddEquilibriumCalculatedVariable;
            MassCp.AddCalculatedVariable += AddEquilibriumCalculatedVariable;
            MolarCp.AddCalculatedVariable += AddEquilibriumCalculatedVariable;
            MassEnthalpy.AddCalculatedVariable += AddEquilibriumCalculatedVariable;
         

            MassDensity.AddCalculatedVariable += AddEquilibriumCalculatedVariable;
            MolarDensity.AddCalculatedVariable += AddEquilibriumCalculatedVariable;
            EnthalpyFlow.AddCalculatedVariable += AddFlowVariable;
            SuperficialTension.AddCalculatedVariable += AddEquilibriumCalculatedVariable;


        }
        public StreamStateType State
        {
            get
            {
                if (IsEquilibriumSolved && IsFlowSolved)
                    return StreamStateType.StreamCalculated; // ¡Verde! Todo listo.

                if (IsEquilibriumSolved && !IsFlowSolved)
                    return StreamStateType.EquilibriumCalculated; // Azul. Falta tamaño de planta.

                if (!IsEquilibriumSolved && ThermodynamicMethod.IsDefined)
                    return StreamStateType.MethodDefined; // Naranja. Faltan P, T o Flujos.

                return StreamStateType.Created; // Gris
            }
        }

        // ¿La termodinámica (T, P, Flash) está completa?
        public bool IsEquilibriumSolved { get; set; } = false;

        // ¿El balance de materia (Flujos) está completo?
        public bool IsFlowSolved { get; set; } = false;

        private void OnEquilibriumReady()
        {
            MaterialStream.CalculateBulkProperties(Temperature.Value!, Pressure.Value!);

            ThermalConductivity.SetValueCalculated(MaterialStream.ThermalConductivity, Name);
         
            Viscosity.SetValueCalculated(MaterialStream.Viscosity, Name);
         

            MassCp.SetValueCalculated(MaterialStream.MassHeatCapacity, Name);


            MolarCp.SetValueCalculated(MaterialStream.MolarHeatCapacity, Name);


            if (!MolarEnthalpy.IsDefined)
            {
                MolarEnthalpy.SetValueCalculated(MaterialStream.MolarEnthalpy, Name);


            }

            MassEnthalpy.SetValueCalculated(MaterialStream.MassEnthalpy, Name);


            MassDensity.SetValueCalculated(MaterialStream.MassDensity, Name);


            MolarDensity.SetValueCalculated(MaterialStream.MolarDensity, Name);


            SuperficialTension.SetValueCalculated(MaterialStream.SurfaceTension, Name);




            IsEquilibriumSolved = true;



        }
        private void EvaluateSolverTrigger()
        {
            // Si el cambio viene del usuario, gritamos al Solver. Si viene de otro lado, silenciamos.
            OnExecuteSolver?.Invoke(this);
        }
        public void SetThermodynamicMethod(ThermodynamicMethodFullDto methodDto)
        {
            // 👇 El wrapper ControlledVariable YA actualizó Source/SourceId
            // No necesitamos volver a llamar SetValue aquí

            // 1. Actualizar estado interno


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
            var data = StreamComposition.Value;

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
        public void ClearThermodynamicMethod()
        {
            // 👇 Ahora sí le pasamos el chisme completo
            ThermodynamicMethod.ClearValue();

            MaterialStream.ClearThermodynamicMethod();


        }

        private void AddEquilibriumCalculatedVariable(IControlledVariable controlledVariable)
        {
            if (controlledVariable != null && !_EquilibriumCalculatedVariables.Contains(controlledVariable))
            {
                _EquilibriumCalculatedVariables.Add(controlledVariable);
            }
        }
        private void AddFlowVariable(IControlledVariable controlledVariable)
        {
            if (controlledVariable != null && !_FlowVariables.Contains(controlledVariable))
            {
                _FlowVariables.Add(controlledVariable);
            }
        }
        public void ResetEquilibriumCalculatedVariable()
        {
            IsEquilibriumSolved = false;
            foreach (var controlledVariable in _EquilibriumCalculatedVariables)
            {
                // 🚩 ¡USAMOS TU NUEVO MÉTODO AQUÍ! 
                // Así la UI se entera de que se borró y actualiza los colores/textos
                controlledVariable.RevertCalculatedValue();
            }
            _EquilibriumCalculatedVariables.Clear();
        }

        public void ResetFlowsCalculatedVariable()
        {
            IsFlowSolved = false;
            foreach (var controlledVariable in _FlowVariables)
            {
                // 🚩 IGUAL AQUÍ
                controlledVariable.RevertCalculatedValue();
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
        public List<ToolTipLegend> GetToolTipLegend()
        {
            List<ToolTipLegend> result = new();

            return result;

        }
        // 4. El diccionario mágico (¡Ya lo tenías perfecto, solo lo dejo aquí por completitud!)
        //public Dictionary<string, string> GetQuickViewData()
        //{
        //    var data = new Dictionary<string, string>();

        //    // Usamos interpolación y formateo (ej. limitando decimales si tuvieran)
        //    data.Add("Temperature", TemperatureControlled.Value?.ToString("GG", null) ?? "--");
        //    data.Add("Pressure", PressureControlled.Value?.ToString("GG", null) ?? "--");
        //    data.Add("Flow", MassFlowControlled.Value?.ToString("GG", null) ?? "--");

        //    return data;
        //}
        public IFacade? SourceEquipment { get; private set; }

        // El equipo hacia donde va el fluido (Ej: El Intercambiador de Calor)
        public IFacade? TargetEquipment { get; private set; }

        public void AttachConnection(string portName, IFacade connectedFacade)
        {
            if (portName == "Inlet") SourceEquipment = connectedFacade;
            else if (portName == "Outlet") TargetEquipment = connectedFacade;

            // Disparamos el evento (Placeholder para la estrategia de cálculo)
  


        }

        public void DetachConnection(string portName)
        {
            if (portName == "Inlet") SourceEquipment = null;
            else if (portName == "Outlet") TargetEquipment = null;

     

        }



    }
}
