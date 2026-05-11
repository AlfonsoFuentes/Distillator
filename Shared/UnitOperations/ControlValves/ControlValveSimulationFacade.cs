using Shared.MatrixSolvers;
using Shared.ProcessFlowDiagram;
using Shared.Thermodynamics.ControlledVariables;
using Shared.UnitOperations.Basiss;
using Shared.UnitOperations.Streams;
using System;
using System.Collections.Generic;
using System.Text;
using UnitSystem;

namespace Shared.UnitOperations.ControlValves
{
    public enum ValveStateType
    {
        Created,
        PartiallyConnected,
        ReadyToCalculate,
        Solved
    }

    public class ControlValveSimulationFacade : EquipmentFacade
    {
        public ControlValveSimulationFacade()
        {
        }



        // =========================================================
        // 1. VARIABLES DEL EQUIPO
        // =========================================================

        // Caída de presión (Delta P)
        public ControlledAmountVariable<PressureDrop> DeltaPressure { get; set; }
            = new ControlledAmountVariable<PressureDrop>(
                preferredUnit: PressureDropUnits.Bar,
                initialValue: new PressureDrop(0, PressureDropUnits.Bar)
            );

        // Coeficiente de la válvula (Cv) - Adimensional o unidades específicas según tu framework
        public ControlledVariable<double> ValveCv { get; set; }
            = new ControlledVariable<double>(0.0);

        // =========================================================
        // 2. ESTADOS VISUALES
        // =========================================================
        public ValveStateType State { get; set; } = ValveStateType.Created;

        public override string StatusText => State switch
        {
            ValveStateType.Created => "Ready",
            ValveStateType.PartiallyConnected => "Underspecified",
            ValveStateType.ReadyToCalculate => "Ready to Solve",
            ValveStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public override string StatusColor => State switch
        {
            ValveStateType.Created => "#CBD5E0",               // Gris
            ValveStateType.PartiallyConnected => "#F6AD55",    // Naranja
            ValveStateType.ReadyToCalculate => "#63B3ED",      // Azul
            ValveStateType.Solved => "#34D399",                // Verde
            _ => "#CBD5E0"
        };

        public override List<ToolTipLegend> GetToolTipLegend()
        {
            List<ToolTipLegend> result = new();

            if (DeltaPressure.IsDefined)
                result.Add(new("ΔP", DeltaPressure.Value?.ToString() ?? string.Empty));
            else
                result.Add(new("ΔP", "<Not Defined>"));

            if (ValveCv.IsDefined)
                result.Add(new("Cv", ValveCv.Value.ToString("F2")));
            else
                result.Add(new("Cv", "<Not Calculated>"));

            return result;
        }

        // =========================================================
        // 3. TOPOLOGÍA
        // =========================================================
        public StreamSimulationFacade? InletStream { get; private set; }
        public StreamSimulationFacade? OutletStream { get; private set; }

        public override void AttachConnection(string portName, IFacade connectedFacade)
        {
            if (portName == "Inlet") InletStream = connectedFacade as StreamSimulationFacade;
            else if (portName == "Outlet") OutletStream = connectedFacade as StreamSimulationFacade;

        }

        public override void DetachConnection(string portName)
        {
            if (portName == "Inlet") InletStream = null;
            else if (portName == "Outlet") OutletStream = null;

        }

        // =========================================================
        // 4. MOTOR DE CÁLCULO
        // =========================================================
        protected override void CalculatedEquipment()
        {

        }
        public override void BuildEquations(EquationSystem eqs)
        {

        }

        public override IEnumerable<INewVariable> GetSolverVariables()
        {
            return null!;
        }
    }


    public class ControlValveSimulationFacade2 : EquipmentFacade2
    {
        // =========================
        // 🔹 CONEXIONES
        // =========================
        public IStreamFacade? Inlet { get; private set; }
        public IStreamFacade? Outlet { get; private set; }

        public ValveStateType State { get; set; } = ValveStateType.Created;

        // 🔹 Variables de control de la válvula
        public NewNewVariableAmount<PressureDrop> DeltaPressure { get; set; }
        public NewNewVariableDouble Cv { get; set; }              // Coeficiente de flujo
        public NewNewVariableDouble ValvePosition { get; set; }   // 0-100% apertura

        // =========================
        // 🔹 CONSTRUCTOR
        // =========================
        public ControlValveSimulationFacade2()
        {
            // ΔP: Caída de presión (Bar → Pascal para solver)
            DeltaPressure = new NewNewVariableAmount<PressureDrop>(
                new PressureDrop(),
                PressureDropUnits.Bar,
                PressureDropUnits.Pascal,
                (v, u) => new PressureDrop(v, u)
            );
            // Eventos: GeneralSolver para balance global, StreamCalculation para parámetros locales, EquipmentSolver para propagación
            DeltaPressure.ExecuteGeneralSolver += ExecuteSolver;
            DeltaPressure.ExecuteStreamCalculation += CalculateValveParameters;
            DeltaPressure.ExecuteEquipmentSolver += OnPropagatePressure;

            // Cv: Coeficiente de flujo (adimensional o m³/hr/√bar según estándar)
            Cv = new NewNewVariableDouble(0);
            //Cv.ExecuteGeneralSolver += ExecuteSolver;
            Cv.ExecuteStreamCalculation += CalculateValveParameters;

            // ValvePosition: 0-100% apertura
            ValvePosition = new NewNewVariableDouble();
            //ValvePosition.ExecuteGeneralSolver += ExecuteSolver;
            ValvePosition.ExecuteStreamCalculation += CalculateValveParameters;
        }

        // =========================
        // 🔹 ECUACIONES DEL EQUIPO - PROPAGACIÓN LOCAL
        // =========================

        // Campos persistentes para EquationSystem (patrón bomba)
        EquationSystem eqConc = new EquationSystem();
        EquationSystem eqMolarFlow = new EquationSystem();
        EquationSystem eqPressure = new EquationSystem();


        // 🔹 Propagación de Concentración
        private void OnPropagateConcentrations()
        {
            if (Inlet == null || Outlet == null) return;
            eqConc = GetEquationConcentration();
            eqConc.SolveEquipmet();
        }

        public override EquationSystem GetEquationConcentration()
        {
            EquationSystem eq = new EquationSystem();
            if (Inlet == null || Outlet == null) return eq;

            eq.AddVariables(GetConcentrationVariables());
            var compsIn = Inlet.StreamComposition.Value.Components;
            var compsOut = Outlet.StreamComposition.Value.Components;

            for (int i = 0; i < compsIn.Count; i++)
            {
                var ni_in = compsIn[i].MolarFractionSolver;
                var ni_out = compsOut[i].MolarFractionSolver;
                eq.AddEquation(new Equation
                {
                    Function = x => x[ni_out.Index] - x[ni_in.Index],
                    Type = EquationType.Model
                });
            }
            return eq;
        }

        // 🔹 Propagación de Flujo Molar
        private void OnPropagateMolarFlow()
        {
            if (Inlet == null || Outlet == null) return;
            eqMolarFlow.Clear();
            eqMolarFlow.AddVariables(GetMassBalanceVariables());

            eqMolarFlow.AddEquation(new Equation
            {
                Function = x => x[Outlet.MolarFlow.Index] - x[Inlet.MolarFlow.Index],
                Type = EquationType.Model
            });
            eqMolarFlow.SolveEquipmet();
        }

        // 🔹 Propagación de Presión (CLAVE: signo MENOS para válvula)
        private void OnPropagatePressure()
        {
            if (Inlet == null || Outlet == null) return;
            eqPressure = GetEquationPressure();
            eqPressure.SolveEquipmet();
        }

        public override EquationSystem GetEquationPressure()
        {
            EquationSystem eq = new EquationSystem();
            if (Inlet == null || Outlet == null) return eq;

            eq.AddVariables(GetPressureVariables());
            var Pin = Inlet.Pressure;
            var Pout = Outlet.Pressure;

            // 🔥 P_out = P_in - ΔP (la válvula REDUCE presión)
            eq.AddEquation(new Equation
            {
                Function = x => x[Pout.Index] - (x[Pin.Index] - x[DeltaPressure.Index]),
                Type = EquationType.Model
            });

           
            return eq;
        }

        // =========================
        // 🔹 BALANCE GLOBAL (Masa/Energía) - Para SolverMatrixManager
        // =========================

        public override EquationSystem GetEquationSystem()
        {
            EquationSystem equationSystem = new EquationSystem();
            if (Inlet == null || Outlet == null) return equationSystem;

            equationSystem.AddVariables(GetEnergyBalanceVariables());

            // Balance de flujo (solo si hay especificación volumétrica)

            //eqMolarFlow.AddVariables(GetMassBalanceVariables());

            equationSystem.AddEquation(new Equation
            {
                Function = x => x[Outlet.MolarFlow.Index] - x[Inlet.MolarFlow.Index],
                Type = EquationType.Model
            });
            // 🔥 PRESIÓN: P_out = P_in - ΔP (para balance global también)
            //var Pin = Inlet.Pressure;
            //var Pout = Outlet.Pressure;
            //equationSystem.AddEquation(new Equation
            //{
            //    Function = x => x[Pout.Index] - (x[Pin.Index] - x[DeltaPressure.Index]),
            //    Type = EquationType.Model
            //});

            // 🔥 ENERGÍA: H_out = H_in (Expansión isentálpica - Joule-Thomson)
            // NO hay trabajo ni calor en válvula ideal
            var Hin = Inlet.MolarEnthalpy;
            var Hout = Outlet.MolarEnthalpy;
            equationSystem.AddEquation(new Equation
            {
                Function = x => x[Hout.Index] - x[Hin.Index],
                Type = EquationType.Model
            });

            return equationSystem;
        }

        // =========================
        // 🔹 MÉTODOS AUXILIARES PARA OBTENER VARIABLES
        // =========================

        IEnumerable<INewNewVariable> GetPressureVariables()
        {
            yield return DeltaPressure;
            if (Inlet != null)
            {
                yield return Inlet.Pressure;
               
            }
            if (Outlet != null)
            {
                yield return Outlet.Pressure;
               
            }
        }

        IEnumerable<INewNewVariable> GetConcentrationVariables()
        {
            if (Inlet != null)
                foreach (var comp in Inlet.StreamComposition.Value.Components)
                    yield return comp.MolarFractionSolver;
            if (Outlet != null)
                foreach (var comp in Outlet.StreamComposition.Value.Components)
                    yield return comp.MolarFractionSolver;
        }

        IEnumerable<INewNewVariable> GetMassBalanceVariables()
        {
            if (Inlet != null)
                yield return Inlet.MolarFlow;
            if (Outlet != null )
                yield return Outlet.MolarFlow;
        }

        public IEnumerable<INewNewVariable> GetEnergyBalanceVariables()
        {
      
            if (Inlet != null)
            {
                yield return Inlet.MolarFlow;
             
                yield return Inlet.MolarEnthalpy;
            }
            if (Outlet != null)
            {
                yield return Outlet.MolarFlow;
          
                yield return Outlet.MolarEnthalpy;
            }
        }

        // =========================
        // 🔹 CÁLCULO LOCAL DE PARÁMETROS (post-propagación)
        // =========================

        private void CalculateValveParameters()
        {
            if (Inlet == null || Outlet == null) return;

            // 🔹 Calcular Cv aproximado si ΔP y flujo están definidos
            // Fórmula simplificada para líquidos: Cv = Q / √(ΔP/SG)
            if (DeltaPressure.IsDefined && Inlet.MolarFlow.IsDefined)
            {
                double deltaP_Pa = DeltaPressure.SolverValue;
                double molarFlow = Inlet.MolarFlow.SolverValue; // kgmol/s

                // Convertir a flujo másico
                double MW = Inlet.MaterialStream.MolecularWeight;
                double massFlow_kg_s = molarFlow * MW / 3600.0;

                // Densidad (fallback agua)
                double rho = 1000.0;
                if (Inlet.MassDensity.IsDefined)
                    rho = Inlet.MassDensity.SolverValue;
                else if (Outlet.MassDensity.IsDefined)
                    rho = Outlet.MassDensity.SolverValue;

                // Cv simplificado
                double SG = rho / 1000.0;
                double deltaP_bar = deltaP_Pa / 1e5;
                double Q_m3_hr = (massFlow_kg_s * 3600.0) / rho;

                if (deltaP_bar > 0 && SG > 0)
                {
                    double cv_calc = Q_m3_hr / Math.Sqrt(deltaP_bar / SG);
                    // Cv.SetValueFromEquipmentSolver(cv_calc); // Opcional: si querés que el solver lo ajuste
                }
            }

            // 🔹 Calcular posición de válvula si Cv está definido
            if (Cv.IsDefined && Cv.Value > 0)
            {
                double cv_max = 100.0; // 👈 Configurable por usuario en futuro
                double position = Math.Clamp(Cv.Value / cv_max * 100.0, 0.0, 100.0);
                // ValvePosition.SetValueFromEquipmentSolver(position); // Opcional
            }
        }

        // =========================
        // 🔹 CONEXIONES (Patrón bomba: suscripción a eventos de propagación)
        // =========================

        public override void AttachConnection(string portName, IStreamFacade connectedFacade)
        {
            if (portName == "Inlet")
            {
                if (Inlet == null)
                {
                    Inlet = connectedFacade;

                    // Suscribirse a eventos de propagación (igual que bomba)
                    Inlet.StreamComposition.ExecuteEquipmentSolver -= OnPropagateConcentrations;
                    Inlet.StreamComposition.ExecuteEquipmentSolver += OnPropagateConcentrations;

                    Inlet.MolarFlow.ExecuteEquipmentSolver -= OnPropagateMolarFlow;
                    Inlet.MolarFlow.ExecuteEquipmentSolver += OnPropagateMolarFlow;

                    Inlet.Pressure.ExecuteEquipmentSolver -= OnPropagatePressure;
                    Inlet.Pressure.ExecuteEquipmentSolver += OnPropagatePressure;

                    // Disparar propagación inicial
                    OnPropagateConcentrations();
                    OnPropagateMolarFlow();
                    OnPropagatePressure();
                }
            }

            if (portName == "Outlet")
            {
                if (Outlet == null)
                {
                    Outlet = connectedFacade;

                    Outlet.StreamComposition.ExecuteEquipmentSolver -= OnPropagateConcentrations;
                    Outlet.StreamComposition.ExecuteEquipmentSolver += OnPropagateConcentrations;

                    Outlet.MolarFlow.ExecuteEquipmentSolver -= OnPropagateMolarFlow;
                    Outlet.MolarFlow.ExecuteEquipmentSolver += OnPropagateMolarFlow;

                    Outlet.Pressure.ExecuteEquipmentSolver -= OnPropagatePressure;
                    Outlet.Pressure.ExecuteEquipmentSolver += OnPropagatePressure;

                    OnPropagateConcentrations();
                    OnPropagateMolarFlow();
                    OnPropagatePressure();
                }
            }
        }

        public override void DetachConnection(string portName)
        {
            if (portName == "Inlet")
            {
                Inlet?.StreamComposition.ExecuteEquipmentSolver -= OnPropagateConcentrations;
                Inlet?.MolarFlow.ExecuteEquipmentSolver -= OnPropagateMolarFlow;
                Inlet?.Pressure.ExecuteEquipmentSolver -= OnPropagatePressure;

                // Disparar propagación para limpiar estado si es necesario
                OnPropagateConcentrations();
                OnPropagateMolarFlow();
                OnPropagatePressure();

                Inlet = null;
            }

            if (portName == "Outlet")
            {
                Outlet?.StreamComposition.ExecuteEquipmentSolver -= OnPropagateConcentrations;
                Outlet?.MolarFlow.ExecuteEquipmentSolver -= OnPropagateMolarFlow;
                Outlet?.Pressure.ExecuteEquipmentSolver -= OnPropagatePressure;

                OnPropagateConcentrations();
                OnPropagateMolarFlow();
                OnPropagatePressure();

                Outlet = null;
            }
        }

        // =========================
        // 🔹 UI: ESTADO Y TOOLTIP
        // =========================

        public override string StatusText => State switch
        {
            ValveStateType.Created => "Ready",
            ValveStateType.PartiallyConnected => "Underspecified",
            ValveStateType.ReadyToCalculate => "Ready to Solve",
            ValveStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public override string StatusColor => State switch
        {
            ValveStateType.Created => "#CBD5E0",
            ValveStateType.PartiallyConnected => "#F6AD55",
            ValveStateType.ReadyToCalculate => "#63B3ED",
            ValveStateType.Solved => "#34D399",
            _ => "#CBD5E0"
        };

        public override List<ToolTipLegend> GetToolTipLegend()
        {
            List<ToolTipLegend> result = new();

            if (DeltaPressure.IsDefined)
                result.Add(new("ΔP", DeltaPressure.Value?.ToString() ?? string.Empty));
            else
                result.Add(new("ΔP", "<Not Defined>"));

            if (Cv.IsDefined)
                result.Add(new("Cv", $"{Cv.Value:F2}"));
            else
                result.Add(new("Cv", "<Not Defined>"));

            if (ValvePosition.IsDefined)
                result.Add(new("Position", $"{ValvePosition.Value:F1}%"));
            else
                result.Add(new("Position", "<Not Defined>"));

            return result;
        }
    }
    
}
