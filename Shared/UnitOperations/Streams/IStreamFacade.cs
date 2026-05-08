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
    public interface IStreamFacade : IFacade
    {
        IMaterialStream MaterialStream { get; }


        NewNewVariableAmount<Temperature> Temperature { get; set; }
        NewNewVariableAmount<Pressure> Pressure { get; set; }

        NewNewVariableAmount<MolarFlow> MolarFlow { get; set; }
        NewNewVariableAmount<MassFlow> MassFlow { get; set; }

        NewNewVariableAmount<VolumetricFlow> VolumetricFlow { get; set; }

        NewNewVariableAmount<ThermalConductivity> ThermalConductivity { get; set; }
        NewNewVariableAmount<Viscosity> Viscosity { get; set; }

        NewNewVariableAmount<MassEntropy> MassCp { get; set; }
        NewNewVariableAmount<MolarEntropy> MolarCp { get; set; }

        NewNewVariableAmount<MassEnergy> MassEnthalpy { get; set; }
        NewNewVariableAmount<MolarEnergy> MolarEnthalpy { get; set; }

        NewNewVariableAmount<MassDensity> MassDensity { get; set; }
        NewNewVariableAmount<MolarDensity> MolarDensity { get; set; }

        NewNewVariableAmount<EnergyFlow> EnthalpyFlow { get; set; }

        NewNewVariableAmount<SuperficialTension> SuperficialTension { get; set; }
        bool IsEquilibriumSolved { get; set; }
        NewNewVariableComposition StreamComposition { get; set; }
        NewNewVariableDouble VaporFraction { get; set; }
        ThermodynamicState EquilibriumState { get; }
        StreamStateType State { get; }
        ThermodynamicMethodFullDto? ThermoMethod { get; set; }
        bool IsFlowSolved { get; set; }
       

        IEnumerable<INewVariable> GetSolverVariables();
        void RemoveEquilibriumCalculate();
        void RemoveFlowsCalculate();
        void SetThermodynamicMethod(ThermodynamicMethodFullDto methodDto);
    }
    public class StreamFacade : IStreamFacade
    {
        private readonly EquilibriumCalculator2 _equilibriumCalculator;
        private readonly FlowsCalculator2 _flowsCalculator;
        public Action? OnExecuteSolver { get; set; }

        List<INewNewVariable> NewEquilibriumVariables = new();
        List<INewVariable> EquilibriumVariables = new();
        List<INewNewVariable> NewFlowsVariables = new();
        public IMaterialStream MaterialStream { get; } = new MaterialStream();
        public ThermodynamicState EquilibriumState => MaterialStream.CurrentState;
        public StreamStateType State
        {
            get
            {
                if (IsEquilibriumSolved && IsFlowSolved)
                    return StreamStateType.StreamCalculated; // ¡Verde! Todo listo.

                if (IsEquilibriumSolved && !IsFlowSolved)
                    return StreamStateType.EquilibriumCalculated; // Azul. Falta tamaño de planta.

                if (!IsEquilibriumSolved && ThermoMethod != null)
                    return StreamStateType.MethodDefined; // Naranja. Faltan P, T o Flujos.

                return StreamStateType.Created; // Gris
            }
        }
        public ThermodynamicMethodFullDto? ThermoMethod { get; set; }
        public void SetThermodynamicMethod(ThermodynamicMethodFullDto methodDto)
        {
            ThermoMethod = methodDto;


            // 2. Crear componentes de composición basados en el método
            CreateFacadeComponents(methodDto);

            // 3. Sincronizar con MaterialStream
            MaterialStream.SetThermodynamicMethod(methodDto);




        }
        void CreateFacadeComponents(ThermodynamicMethodFullDto methodDto)
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
                data.AttachEvents();
            }

        }

        // ✅ Caso 2: "Des-definir" método (nueva funcionalidad)
        public void ClearThermodynamicMethod()
        {
            // 👇 Ahora sí le pasamos el chisme completo
            ThermoMethod = null;

            MaterialStream.ClearThermodynamicMethod();


        }

        // ¿El balance de materia (Flujos) está completo?
        public bool IsFlowSolved { get; set; } = false;
      

        public NewNewVariableAmount<Temperature> Temperature { get; set; }
        public NewNewVariableAmount<Pressure> Pressure { get; set; }

        public NewNewVariableAmount<MolarFlow> MolarFlow { get; set; }
        public NewNewVariableAmount<MassFlow> MassFlow { get; set; }

        public NewNewVariableAmount<VolumetricFlow> VolumetricFlow { get; set; }

        public NewNewVariableAmount<ThermalConductivity> ThermalConductivity { get; set; }
        public NewNewVariableAmount<Viscosity> Viscosity { get; set; }

        public NewNewVariableAmount<MassEntropy> MassCp { get; set; }
        public NewNewVariableAmount<MolarEntropy> MolarCp { get; set; }

        public NewNewVariableAmount<MassEnergy> MassEnthalpy { get; set; }
        public NewNewVariableAmount<MolarEnergy> MolarEnthalpy { get; set; }

        public NewNewVariableAmount<MassDensity> MassDensity { get; set; }
        public NewNewVariableAmount<MolarDensity> MolarDensity { get; set; }

        public NewNewVariableAmount<EnergyFlow> EnthalpyFlow { get; set; }

        public NewNewVariableAmount<SuperficialTension> SuperficialTension { get; set; }

        public NewNewVariableComposition StreamComposition { get; set; }
        // =========================
        // 🔹 IDENTIDAD
        // =========================
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "S-1";

        public string StatusText { get; set; } = "Initialized";
        public string StatusColor { get; set; } = "#CBD5E0";

        public bool IsEquilibriumSolved { get; set; }
        public NewNewVariableDouble VaporFraction { get; set; }
        public StreamFacade()
        {
            _equilibriumCalculator = new EquilibriumCalculator2(this);
            _equilibriumCalculator.EquilibriumReady += OnEquilibriumReady;
            _flowsCalculator = new FlowsCalculator2(this);
            _equilibriumCalculator.FlowsReady += _flowsCalculator.Execute;

            VaporFraction = new NewNewVariableDouble(0);
            VaporFraction.SendToFacadeInside += () => { MaterialStream.SetVaporFraction(VaporFraction.Value); };
            VaporFraction.ExecuteStreamCalculation += ExecuteEquilibrium;
            VaporFraction.AddToDefinedList += AddNewEquilibriumVariables;
            VaporFraction.ExecuteGeneralSolver += ExecuteSolver;

            // EN: StreamFacade.cs (dentro del constructor, justo después de inicializar StreamComposition)

            // ✅ AGREGAR justo después de crear StreamComposition:
            StreamComposition = new NewNewVariableComposition(new StreamComposition());

            // 🔥 NUEVO: Conectar referencia padre-hijo
            StreamComposition.Value.ParentVariable = StreamComposition;

            // Eventos existentes (NO CAMBIAR):
            StreamComposition.SendToFacadeInside += () => { MaterialStream.SetCompositionData(StreamComposition.Value); };
            StreamComposition.ExecuteStreamCalculation += ExecuteEquilibrium;
            StreamComposition.AddToDefinedList += AddNewEquilibriumVariables;
            StreamComposition.ExecuteGeneralSolver += ExecuteSolver;

            // 👇 ELIMINAR ESTO (ya no es necesario con el nuevo enfoque):
            // StreamComposition.Value.OnAllComponentsUpdatedBySolver = () => { ... };
            Temperature = new NewNewVariableAmount<Temperature>(new Temperature(),
                TemperatureUnits.DegreeCelcius,
                TemperatureUnits.Kelvin,
                (v, u) => new Temperature(v, u),
                298.15 // InitValue
            );
            Temperature.SendToFacadeInside += () => { MaterialStream.SetTemperature(Temperature.Value); };
            Temperature.ExecuteStreamCalculation += ExecuteEquilibrium;
            Temperature.AddToDefinedList += AddNewEquilibriumVariables;
            Temperature.ExecuteGeneralSolver += ExecuteSolver;

   


            Pressure = new NewNewVariableAmount<Pressure>(new Pressure(),
                PressureUnits.Bara,
                PressureUnits.Pascala,
                (v, u) => new Pressure(v, u),
                101325 // InitValue
            );
            Pressure.SendToFacadeInside += () => { MaterialStream.SetPressure(Pressure.Value); };
            Pressure.ExecuteStreamCalculation += ExecuteEquilibrium;
            Pressure.AddToDefinedList += AddNewEquilibriumVariables;
            Pressure.ExecuteGeneralSolver += ExecuteSolver;

           

            MassFlow = new NewNewVariableAmount<MassFlow>(new MassFlow(),
                MassFlowUnits.Kg_hr,
                MassFlowUnits.Kg_hr,
                (v, u) => new MassFlow(v, u)   ,    1000
            );
        
       
            MassFlow.ExecuteStreamCalculation += ExecuteFlows;
            MassFlow.AddToDefinedList += AddNewFlowsVariables;
            MassFlow.ExecuteGeneralSolver += ExecuteSolver;

            MolarFlow = new NewNewVariableAmount<MolarFlow>(new MolarFlow(),
                MolarFlowUnits.Kgmol_hr,
                MolarFlowUnits.Kgmol_hr,
                (v, u) => new MolarFlow(v, u),
                100 // InitValue
            );
            
            MolarFlow.ExecuteStreamCalculation += ExecuteFlows;
            MolarFlow.AddToDefinedList += AddNewFlowsVariables;
            MolarFlow.ExecuteGeneralSolver += ExecuteSolver;
            VolumetricFlow = new NewNewVariableAmount<VolumetricFlow>(new VolumetricFlow(),
                VolumetricFlowUnits.m3_hr,
                VolumetricFlowUnits.m3_hr,
                (v, u) => new VolumetricFlow(v, u) ,1
            );
           
            VolumetricFlow.ExecuteStreamCalculation += ExecuteFlows;
            VolumetricFlow.AddToDefinedList += AddNewFlowsVariables;
            VolumetricFlow.ExecuteGeneralSolver += ExecuteSolver;

            // 🔥 Propiedades calculadas (igual patrón)
            ThermalConductivity = new NewNewVariableAmount<ThermalConductivity>(new ThermalConductivity(),
                ThermalConductivityUnits.W_m_K,
                ThermalConductivityUnits.W_m_K,
                (v, u) => new ThermalConductivity(v, u)
            );
            ThermalConductivity.AddToDefinedList += AddNewEquilibriumVariables;
            Viscosity = new NewNewVariableAmount<Viscosity>(new Viscosity(),
                ViscosityUnits.cPoise,
                ViscosityUnits.cPoise,
                (v, u) => new Viscosity(v, u)
            );
            Viscosity.AddToDefinedList += AddNewEquilibriumVariables;
            MassCp = new NewNewVariableAmount<MassEntropy>(new MassEntropy(),
                MassEntropyUnits.Kcal_Kg_C,
                MassEntropyUnits.Kcal_Kg_C,
                (v, u) => new MassEntropy(v, u)
            );
            MassCp.AddToDefinedList += AddNewEquilibriumVariables;
            MolarCp = new NewNewVariableAmount<MolarEntropy>(new MolarEntropy(),
                MolarEntropyUnits.Kcal_Kgmol_C,
                MolarEntropyUnits.Kcal_Kgmol_C,
                (v, u) => new MolarEntropy(v, u)
            );
            MolarCp.AddToDefinedList += AddNewEquilibriumVariables;
            MassEnthalpy = new NewNewVariableAmount<MassEnergy>(new MassEnergy(),
                MassEnergyUnits.Kcal_Kg,
                MassEnergyUnits.Kcal_Kg,
                (v, u) => new MassEnergy(v, u)
            );
            MassEnthalpy.AddToDefinedList += AddNewEquilibriumVariables ;
            MolarEnthalpy = new NewNewVariableAmount<MolarEnergy>(new MolarEnergy(),
                MolarEnergyUnits.Kcal_Kgmol,
                MolarEnergyUnits.J_gmol,
                (v, u) => new MolarEnergy(v, u)  ,1000
            );

            MolarEnthalpy.ExecuteStreamCalculation += ExecuteEquilibrium;
            MolarEnthalpy.AddToDefinedList += AddNewEquilibriumVariables;
     

            MassDensity = new NewNewVariableAmount<MassDensity>(new MassDensity(),
                MassDensityUnits.Kg_m3,
                MassDensityUnits.Kg_m3,
                (v, u) => new MassDensity(v, u)
            );
            MassDensity.AddToDefinedList += AddNewEquilibriumVariables;
            MolarDensity = new NewNewVariableAmount<MolarDensity>(new MolarDensity(),
                MolarDensityUnits.Kgmol_m3,
                MolarDensityUnits.gmol_m3,
                (v, u) => new MolarDensity(v, u)
            );
            MolarDensity.AddToDefinedList += AddNewEquilibriumVariables;
            EnthalpyFlow = new NewNewVariableAmount<EnergyFlow>(new EnergyFlow(),
                EnergyFlowUnits.Kcal_hr,
                EnergyFlowUnits.Kcal_hr,
                (v, u) => new EnergyFlow(v, u)
            );
            EnthalpyFlow.AddToDefinedList += AddNewFlowsVariables;
            SuperficialTension = new NewNewVariableAmount   <SuperficialTension>(new SuperficialTension(),
                SuperficialTensionUnits.dyn_cm,
                SuperficialTensionUnits.dyn_cm,
                (v, u) => new SuperficialTension(v, u)
            );
            SuperficialTension.AddToDefinedList += AddNewEquilibriumVariables;
        }
       
        void ExecuteSolver()
        {
            OnExecuteSolver?.Invoke();
        }
        // EN: StreamFacade.cs (modificar el método ExecuteEquilibrium)

        void ExecuteEquilibrium()
        {
            
            _equilibriumCalculator.Execute();
        }
        void ExecuteFlows()
        {
            _flowsCalculator.Execute();
        }
        public void AddNewEquilibriumVariables(INewNewVariable variable)
        {
            NewEquilibriumVariables.Add(variable);
        }
        public void AddNewFlowsVariables(INewNewVariable variable)
        {
            NewFlowsVariables.Add(variable);
        }
        public void AddEquilibriumCalculate(INewVariable variable)
        {
            EquilibriumVariables.Add(variable);
        }
        //public void AddFlowsCalculate(INewVariable variable)
        //{
        //    FlowsVariables.Add(variable);
        //}
        // EN: StreamFacade.cs (modificar RemoveEquilibriumCalculate)

        public void RemoveEquilibriumCalculate()
        {
            foreach (var v in NewEquilibriumVariables)
            {
                v.ClearFromStream();
            }
            EquilibriumVariables.Clear();

        }

        // Y lo mismo en RemoveFlowsCalculate:
        public void RemoveFlowsCalculate()
        {
            foreach (var v in NewFlowsVariables)
            {
                v.ClearFromStream();
            }
            NewFlowsVariables.Clear();

          
        }
        // =========================
        // 🔹 UTILIDAD CLAVE (PARA SOLVER MANAGER)
        // =========================
        public IEnumerable<INewVariable> GetSolverVariables()
        {
            //yield return Temperature;
            //yield return Pressure;

            //yield return MassFlow;
            ////yield return MolarFlow;
            ////yield return VolumetricFlow;
            ///
            return null!;
        }

        private void OnEquilibriumReady()
        {
          

            ThermalConductivity.SetValueFromStream(MaterialStream.ThermalConductivity,Name);

            Viscosity.SetValueFromStream(MaterialStream.Viscosity,Name);


            MassCp.SetValueFromStream(MaterialStream.MassHeatCapacity, Name);


            MolarCp.SetValueFromStream(MaterialStream.MolarHeatCapacity, Name);


           

            MassEnthalpy.SetValueFromStream(MaterialStream.MassEnthalpy, Name);


            MassDensity.SetValueFromStream(MaterialStream.MassDensity, Name);


            MolarDensity.SetValueFromStream(MaterialStream.MolarDensity, Name);


            SuperficialTension.SetValueFromStream(MaterialStream.SurfaceTension, Name);



            IsEquilibriumSolved = true;



        }
        public List<ToolTipLegend> GetToolTipLegend() => new();

        

        public void DetachConnection(string portName)
        {
           



        }

        public void AttachConnection(string portName, IFacade connectedFacade)
        {
            
        }
    }
}



