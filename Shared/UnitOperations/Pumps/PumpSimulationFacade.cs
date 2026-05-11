using Shared.MatrixSolvers;
using Shared.ProcessFlowDiagram;
using Shared.Thermodynamics.ControlledVariables;
using Shared.UnitOperations.Basiss;
using Shared.UnitOperations.Streams;
using System.Net.NetworkInformation;
using UnitSystem;

namespace Shared.UnitOperations.Pumps
{
    public enum PumpStateType
    {
        Created,
        PartiallyConnected,
        ReadyToCalculate,
        Solved
    }
    public class PumpSimulationFacade : EquipmentFacade
    {
        //private PumpModel? _model;
        //private EquationSystem? _eqs;
        public PumpSimulationFacade()
        {


        }
        public void InitializeSolver(EquationSystem eqs)
        {
            //_eqs = eqs;

            //_model = new PumpModel(Name, eqs);

            //_model.Inlet = SuctionStream as StreamSimulationFacadeV2;
            //_model.Outlet = DischargeStream as StreamSimulationFacadeV2;

            //_model.BuildEquations(eqs);
        }

        // 2. VARIABLES DEL EQUIPO
        public ControlledAmountVariable<PressureDrop> DeltaPressure { get; set; }
             = new ControlledAmountVariable<PressureDrop>(
                 preferredUnit: PressureDropUnits.Bar, // Usa el enum de tu dominio
                 initialValue: new PressureDrop(0, PressureDropUnits.Bar)
             );

        // Opcional: Permitir al usuario definir la presión de salida exacta en lugar del Delta P


        // Eficiencia (Adimensional / Porcentaje). Sigue la misma lógica que VaporFraction
        public ControlledVariable<double> AdiabaticEfficiency { get; set; }
            = new ControlledVariable<double>(75.0);

        // Potencia Consumida (Calculada por el PumpCalculator)
        public ControlledAmountVariable<Power> PowerConsumed { get; set; }
            = new ControlledAmountVariable<Power>(
                preferredUnit: PowerUnits.KiloWatt, // Usa el enum de tu dominio (ej. kW, HP)
                initialValue: new Power(0, PowerUnits.KiloWatt)
            );

        // 👇 EL NUEVO ESTADO DE LA MÁQUINA
        public PumpStateType State { get; set; } = PumpStateType.Created;

        // 3. ESTADO VISUAL (Aplicando tu lógica de colores)
        public override string StatusText => State switch
        {
            PumpStateType.Created => "Ready",
            PumpStateType.PartiallyConnected => "Underspecified",
            PumpStateType.ReadyToCalculate => "Ready to Solve",
            PumpStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public override string StatusColor => State switch
        {
            PumpStateType.Created => "#CBD5E0",              // Gris
            PumpStateType.PartiallyConnected => "#F6AD55",   // Naranja
            PumpStateType.ReadyToCalculate => "#63B3ED",     // Azul
            PumpStateType.Solved => "#34D399",               // Verde
            _ => "#CBD5E0"
        };
        public override List<ToolTipLegend> GetToolTipLegend()
        {
            List<ToolTipLegend> result = new();
            if (DeltaPressure.IsDefined)
            {
                result.Add(new("ΔP", DeltaPressure.Value?.ToString() ?? string.Empty));
            }
            else
            {
                result.Add(new("ΔP", "<Not Defined>"));
            }

            if (AdiabaticEfficiency.IsDefined)
            {
                result.Add(new ToolTipLegend("%Efficiency", $"{AdiabaticEfficiency.Value}"));
            }
            else
            {
                result.Add(new("%Efficiency", "<Not Defined>"));
            }
            if (PowerConsumed.IsDefined)
            {
                result.Add(new ToolTipLegend("Power", PowerConsumed.Value?.ToString() ?? string.Empty));
            }
            else
            {
                result.Add(new("Power", "<Not Calculated>"));
            }
            return result;

        }


        // 4. TOPOLOGÍA DE SIMULACIÓN
        public StreamSimulationFacade? SuctionStream { get; private set; }
        public StreamSimulationFacade? DischargeStream { get; private set; }



        public override void AttachConnection(string portName, IFacade connectedFacade)
        {
            if (portName == "Suction") SuctionStream = connectedFacade as StreamSimulationFacade;
            else if (portName == "Discharge") DischargeStream = connectedFacade as StreamSimulationFacade;



        }

        public override void DetachConnection(string portName)
        {
            if (portName == "Suction") SuctionStream = null;
            else if (portName == "Discharge") DischargeStream = null;


        }

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
    public class PumpSimulationFacade2 : EquipmentFacade2
    {
        // =========================
        // 🔹 CONEXIONES
        // =========================
        public IStreamFacade? Inlet { get; private set; }
        public IStreamFacade? Outlet { get; private set; }

        public PumpStateType State { get; set; } = PumpStateType.Created;
        public NewNewVariableAmount<PressureDrop> DeltaPressure { get; set; }
        public NewNewVariableDouble Efficiency { get; set; }

        public NewNewVariableAmount<Power> Power { get;  set; }
        // =========================
        // 🔹 CONSTRUCTOR
        // =========================
        public PumpSimulationFacade2()
        {
            DeltaPressure = new NewNewVariableAmount<PressureDrop>(
                new PressureDrop(),
                PressureDropUnits.Bar,
                PressureDropUnits.Pascal,
                (v, u) => new PressureDrop(v, u)
            );
            DeltaPressure.ExecuteGeneralSolver += ExecuteSolver;
            DeltaPressure.ExecuteStreamCalculation += CalculatePower;
            DeltaPressure.ExecuteEquipmentSolver += OnPropagatePressure;
            Efficiency = new NewNewVariableDouble();

            Efficiency.ExecuteGeneralSolver += ExecuteSolver;
            Efficiency.ExecuteStreamCalculation += CalculatePower;

            Power = new NewNewVariableAmount<Power>(
                    new Power(),
                    PowerUnits.KiloWatt,
                    PowerUnits.Watt,
                    (v, u) => new Power(v, u)
                );

        }

        // =========================
        // 🔹 ECUACIONES DEL EQUIPO
        // =========================


        private void CalculatePower()
        {
            if (Inlet == null || Outlet == null) return;

            double flow = Inlet.MolarFlow.SolverValue; // mol/s
            double deltaP = DeltaPressure.SolverValue; // Pa
            double eff = Efficiency.Value;

            double rho;

            if (Inlet.MolarDensity.IsDefined)
                rho = Inlet.MolarDensity.SolverValue;
            else if (Outlet.MolarDensity.IsDefined)
                rho = Outlet.MolarDensity.SolverValue;
            else
                rho = 55555.0; // fallback agua



            double w = deltaP / (rho * eff); // J/mol

            double power = flow * w; // J/s = W

            Power.SetValueFromEquipmentSolver(power);
        }
  
        EquationSystem eqConc = new EquationSystem();
        EquationSystem eqMolarFlow = new EquationSystem();
        EquationSystem eqPressure = new EquationSystem();
        private void OnPropagateConcentrations()
        {

            if (Inlet == null || Outlet == null)
            {
                return;
            }

            eqConc=GetEquationConcentration();
            eqConc.SolveEquipmet();

        }
        public override EquationSystem GetEquationConcentration()
        {
            EquationSystem eq = new EquationSystem();
            if (Inlet == null || Outlet == null)
            {
                return eq;
            }
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

        
        private void OnPropagateMolarFlow()
        {


            if (Inlet == null || Outlet == null)
            {
                return;
            }
            eqMolarFlow.Clear();

            eqMolarFlow.AddVariables(GetMassBalanceVariables());
            eqMolarFlow.AddEquation(new Equation
            {
                Function = x => x[Outlet.MolarFlow.Index] - x[Inlet.MolarFlow.Index],
                Type = EquationType.Model
            });

            eqMolarFlow.SolveEquipmet();

        }
        public override EquationSystem GetEquationPressure()
        {
            EquationSystem eq = new EquationSystem();
            if (Inlet == null || Outlet == null)
            {
                return eq;
            }
            eq.AddVariables(GetPressureVariables());
            var Pin = Inlet.Pressure;
            var Pout = Outlet.Pressure;
            eq.AddEquation(new Equation
            {
                Function = x => x[Pout.Index] - (x[Pin.Index] + x[DeltaPressure.Index]),
                Type = EquationType.Model
            });
           
            return eq;
        }
        private void OnPropagatePressure()
        {



            eqPressure = GetEquationPressure();

            eqPressure.SolveEquipmet();

        }


        public override EquationSystem GetEquationSystem()
        {
            EquationSystem equationSystem = new EquationSystem();
            if (Inlet == null || Outlet == null) return equationSystem;

            equationSystem.AddVariables(GetEnergyBalanceVariables());
            equationSystem.AddEquation(new Equation
            {
                Function = x => x[Outlet.MolarFlow.Index] - x[Inlet.MolarFlow.Index],
                Type = EquationType.Model
            });
            var Hin = Inlet.MolarEnthalpy;
            var Hout = Outlet.MolarEnthalpy;
            var Eff = Efficiency;
            double eff = Efficiency.Value;
            double deltaP = DeltaPressure.SolverValue;
            double rho;

            if (Inlet.MolarDensity.IsDefined)
                rho = Inlet.MolarDensity.SolverValue;
            else if (Outlet.MolarDensity.IsDefined)
                rho = Outlet.MolarDensity.SolverValue;
            else
                rho = 1000 / 18 * 1000;

            equationSystem.AddEquation(new Equation
            {
                Function = x =>
                {
                    double hin = x[Hin.Index];
                    double hout = x[Hout.Index];

                    double w = deltaP / (rho * eff);

                    return hout - (hin + w);
                },
                Type = EquationType.Model
            }
             );

            return equationSystem;
        }


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
            {
                foreach (var comp in Inlet.StreamComposition.Value.Components)
                    yield return comp.MolarFractionSolver;



            }
            if (Outlet != null)
            {
                foreach (var comp in Outlet.StreamComposition.Value.Components)
                    yield return comp.MolarFractionSolver;




            }
        }
        IEnumerable<INewNewVariable> GetMassBalanceVariables()
        {
            if (Inlet != null)
            {
                yield return Inlet.MolarFlow;
            




            }
            if (Outlet != null)
            {
                yield return Outlet.MolarFlow;

            }
        }
        //public List<Equation> BuildEnergyBalanceEquations()
        //{
        //    List<Equation> Equations = new List<Equation>();
        //    if (Inlet == null || Outlet == null)
        //    {
        //        return Equations;
        //    }
        //    Equations.Add(new Equation
        //    {
        //        Function = x => x[Outlet.MolarFlow.Index] - x[Inlet.MolarFlow.Index],
        //        Type = EquationType.Model
        //    });

        //    var Pin = Inlet.Pressure;
        //    var Pout = Outlet.Pressure;

        //    // 🔥 Pout = Pin + DeltaP
        //    Equations.Add(new Equation
        //    {
        //        Function = x => x[Pout.Index] - (x[Pin.Index] + x[DeltaPressure.Index]),
        //        Type = EquationType.Model,
        //    }

        //    );

        //    // 🔥 Balance de flujo (simple)


        //    var Hin = Inlet.MolarEnthalpy;
        //    var Hout = Outlet.MolarEnthalpy;
        //    var Eff = Efficiency;

        //    Equations.Add(new Equation
        //    {
        //        Function = x =>
        //        {
        //            double hin = x[Hin.Index];
        //            double hout = x[Hout.Index];
        //            double deltaP = x[DeltaPressure.Index];
        //            double eff = Efficiency.Value;

        //            double rho;

        //            if (Inlet.MolarDensity.IsDefined)
        //                rho = Inlet.MolarDensity.SolverValue;
        //            else if (Outlet.MolarDensity.IsDefined)
        //                rho = Outlet.MolarDensity.SolverValue;
        //            else
        //                rho = 1000 / 18 * 1000;



        //            double w = deltaP / (rho * eff);

        //            return hout - (hin + w);
        //        },
        //        Type = EquationType.Model
        //    }
        //     );
        //    return Equations;
        //}
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
        // 🔹 CONEXIONES
        // =========================
        public override void AttachConnection(string portName, IStreamFacade connectedFacade)
        {
            if (portName == "Inlet")
            {
                if (Inlet == null)
                {
                    Inlet = connectedFacade;
                    Inlet.StreamComposition.ExecuteEquipmentSolver -= OnPropagateConcentrations;
                    Inlet.StreamComposition.ExecuteEquipmentSolver += OnPropagateConcentrations;
                    Inlet.MolarFlow.ExecuteEquipmentSolver -= OnPropagateMolarFlow;
                    Inlet.MolarFlow.ExecuteEquipmentSolver += OnPropagateMolarFlow;
                    Inlet.Pressure.ExecuteEquipmentSolver -= OnPropagatePressure;
                    Inlet.Pressure.ExecuteEquipmentSolver += OnPropagatePressure;
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
        // 🔹 UI
        // =========================
        public override string StatusText => State switch
        {
            PumpStateType.Created => "Ready",
            PumpStateType.PartiallyConnected => "Underspecified",
            PumpStateType.ReadyToCalculate => "Ready to Solve",
            PumpStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public override string StatusColor => State switch
        {
            PumpStateType.Created => "#CBD5E0",              // Gris
            PumpStateType.PartiallyConnected => "#F6AD55",   // Naranja
            PumpStateType.ReadyToCalculate => "#63B3ED",     // Azul
            PumpStateType.Solved => "#34D399",               // Verde
            _ => "#CBD5E0"
        };
        public override List<ToolTipLegend> GetToolTipLegend()
        {
            List<ToolTipLegend> result = new();
            if (DeltaPressure.IsDefined)
            {
                result.Add(new("ΔP", DeltaPressure.Value?.ToString() ?? string.Empty));
            }
            else
            {
                result.Add(new("ΔP", "<Not Defined>"));
            }

            if (Efficiency.IsDefined)
            {
                result.Add(new ToolTipLegend("%Efficiency", $"{Efficiency.Value}"));
            }
            else
            {
                result.Add(new("%Efficiency", "<Not Defined>"));
            }
            if (Power.IsDefined)
            {
                result.Add(new ToolTipLegend("Power", Power.Value?.ToString() ?? string.Empty));
            }
            else
            {
                result.Add(new("Power", "<Not Calculated>"));
            }
            return result;

        }


    }
}
