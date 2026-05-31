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
    public class PumpSimulationFacade2 : EquipmentFacade2
    {
        // =========================
        // 🔹 CONEXIONES
        // =========================
        public IStreamFacade2? Inlet { get; private set; }
        public IStreamFacade2? Outlet { get; private set; }
        public NewNewVariableAmount<PressureDrop> DeltaPressure { get; set; }
        public NewNewVariableDouble Efficiency { get; set; }
        public NewNewVariableAmount<Power> Power { get; set; }

        // =========================
        // 🔹 CONSTRUCTOR
        // =========================
        public PumpSimulationFacade2()
        {
            DeltaPressure = new NewNewVariableAmount<PressureDrop>(
                new PressureDrop(), PressureDropUnits.Bar, PressureDropUnits.Pascal, (v, u) => new PressureDrop(v, u));
            DeltaPressure.ExecuteGeneralSolver += ExecuteSolver;
            DeltaPressure.ExecuteStreamCalculation += CalculatePower;
            DeltaPressure.ExecuteEquipmentSolver += OnPropagatePressure;

            Efficiency = new NewNewVariableDouble();
            Efficiency.ExecuteGeneralSolver += ExecuteSolver;
            Efficiency.ExecuteStreamCalculation += CalculatePower;

            Power = new NewNewVariableAmount<Power>(
                new Power(), PowerUnits.KiloWatt, PowerUnits.Watt, (v, u) => new Power(v, u));
        }

        // =========================
        // 🔹 ECUACIONES PARA SOLVER VIEJO (se mantienen)
        // =========================
        private EquationSystem eqConc = new EquationSystem();
        private EquationSystem eqMolarFlow = new EquationSystem();
        private EquationSystem eqPressure = new EquationSystem();

        public override EquationSystem GetEquationConcentration()
        {
            var eq = new EquationSystem();
            if (Inlet == null || Outlet == null) return eq;

            eq.AddVariables(GetConcentrationVariables());
            var compsIn = Inlet.StreamComposition?.Value?.Components;
            var compsOut = Outlet.StreamComposition?.Value?.Components;

            if (compsIn != null && compsOut != null)
            {
                for (int i = 0; i < compsIn.Count; i++)
                {
                    eq.AddEquation(new Equation
                    {
                        Function = x => x[compsOut[i].MolarFractionSolver.Index] - x[compsIn[i].MolarFractionSolver.Index],
                        Type = EquationType.Model
                    });
                }
            }
            return eq;
        }

        public override EquationSystem GetEquationPressure()
        {
            var eq = new EquationSystem();
            if (Inlet == null || Outlet == null) return eq;

            eq.AddVariables(GetPressureVariables());
            eq.AddEquation(new Equation
            {
                Function = x => x[Outlet.Pressure.Index] - (x[Inlet.Pressure.Index] + x[DeltaPressure.Index]),
                Type = EquationType.Model
            });
            return eq;
        }

        public override EquationSystem GetEquationSystem()
        {
            var eq = new EquationSystem();
            if (Inlet == null || Outlet == null) return eq;

            eq.AddVariables(GetEnergyBalanceVariables());

            // Balance de masa
            eq.AddEquation(new Equation
            {
                Function = x => x[Outlet.MassFlow.Index] - x[Inlet.MassFlow.Index],
                Type = EquationType.Model
            });

            // Balance de energía (simplificado)
            var hin = Inlet.MassEnthalpy;
            var hout = Outlet.MassEnthalpy;
            eq.AddEquation(new Equation
            {
                Function = x => x[Outlet.MassFlow.Index] * x[hout.Index] - x[Inlet.MassFlow.Index] * x[hin.Index],
                Type = EquationType.Model
            });

            return eq;
        }

        // =========================
        // 🔥 NUEVO: ECUACIONES PARA SOLVER REACTIVO
        // =========================
       

        // 🔹 Helper para obtener valor de variable (con fallback)
        private double GetVarValue(List<INewNewVariable> vars, INewNewVariable target)
        {
            if (target == null) return 0;
            var found = vars?.FirstOrDefault(v => v == target) ?? vars?.FirstOrDefault(v => v?.Index == target.Index);
            return found?.GetEffectiveSolverValue() ?? target.GetSolverValue();
        }

        // =========================
        // 🔹 MÉTODOS DE PROPAGACIÓN (se mantienen)
        // =========================
        private void OnPropagateConcentrations()
        {
            if (Inlet == null || Outlet == null) return;
            eqConc = GetEquationConcentration();
            eqConc.SolveEquipmet();
        }

        private void OnPropagateMassFlow()
        {
            if (Inlet == null || Outlet == null) return;
            eqMolarFlow.Clear();
            eqMolarFlow.AddVariables(GetMassBalanceVariables());
            eqMolarFlow.AddEquation(new Equation
            {
                Function = x => x[Outlet.MassFlow.Index] - x[Inlet.MassFlow.Index],
                Type = EquationType.Model
            });
            eqMolarFlow.SolveEquipmet();
        }

        private void OnPropagatePressure()
        {
            if (Inlet == null || Outlet == null) return;
            eqPressure = GetEquationPressure();
            eqPressure.SolveEquipmet();
        }

        private void CalculatePower()
        {
            if (Inlet == null || Outlet == null) return;
            if (!Efficiency.IsDefined || !DeltaPressure.IsDefined || Efficiency.Value <= 0) return;

            var totalMassFlow = Inlet.MassFlow?.Value?.GetValue(MassFlowUnits.Kg_sg) ?? 0;
            if (Math.Abs(totalMassFlow) < 1e-9) return;

            var deltaP = DeltaPressure.Value.GetValue(PressureDropUnits.Pascal);
            var eff = Efficiency.IsDefinedByUI ? (Efficiency.Value == 0 ? 1 : Efficiency.Value / 100) : 1;
            var rho = 1000.0;  // Fallback

            var w = deltaP / (rho * eff);  // J/kg
            var power = totalMassFlow * w;  // W

            Power?.SetValueFromEquipmentSolver(power);
        }

        // =========================
        // 🔹 CONEXIÓN/DESCONEXIÓN (actualizado para nuevo solver)
        // =========================
        public override void AttachConnection(string portName, IStreamFacade2 connectedFacade)
        {
            if (portName == "Suction" && Inlet == null)
            {
                Inlet = connectedFacade;
                SubscribeStream(Inlet);
                TriggerPropagation();
            }
            else if (portName == "Discharge" && Outlet == null)
            {
                Outlet = connectedFacade;
                SubscribeStream(Outlet);
                TriggerPropagation();
            }
        }

        private void SubscribeStream(IStreamFacade2 stream)
        {
            if (stream?.StreamComposition != null)
            {
                stream.StreamComposition.ExecuteEquipmentSolver -= OnPropagateConcentrations;
                stream.StreamComposition.ExecuteEquipmentSolver += OnPropagateConcentrations;
            }
            if (stream?.MassFlow != null)
            {
                stream.MassFlow.ExecuteEquipmentSolver -= OnPropagateMassFlow;
                stream.MassFlow.ExecuteEquipmentSolver += OnPropagateMassFlow;
            }
            if (stream?.Pressure != null)
            {
                stream.Pressure.ExecuteEquipmentSolver -= OnPropagatePressure;
                stream.Pressure.ExecuteEquipmentSolver += OnPropagatePressure;
            }
        }

        private void TriggerPropagation()
        {
            OnPropagateConcentrations();
            OnPropagateMassFlow();
            OnPropagatePressure();
            ExecuteSolver();
        }

        public override void DetachConnection(string portName)
        {
            if (portName == "Suction")
            {
                UnsubscribeStream(Inlet!);
                ClearPropagatedValues(Inlet!);
                ClearPropagatedValues(Outlet!);  // 🔥 Limpiar también el otro puerto
                eqConc.ClearEquipmentSolverDefinitions();
                eqMolarFlow.ClearEquipmentSolverDefinitions();
                eqPressure.ClearEquipmentSolverDefinitions();
                Power?.ClearFromEquipmentSolver();
                Inlet = null;
                ExecuteSolver();
            }
            else if (portName == "Discharge")
            {
                UnsubscribeStream(Outlet!);
                ClearPropagatedValues(Outlet!);
                ClearPropagatedValues(Inlet!);  // 🔥 Limpiar también el otro puerto
                eqConc.ClearEquipmentSolverDefinitions();
                eqMolarFlow.ClearEquipmentSolverDefinitions();
                eqPressure.ClearEquipmentSolverDefinitions();
                Power?.ClearFromEquipmentSolver();
                Outlet = null;
                ExecuteSolver();
            }
        }

        private void UnsubscribeStream(IStreamFacade2 stream)
        {
            if (stream?.StreamComposition != null)
                stream.StreamComposition.ExecuteEquipmentSolver -= OnPropagateConcentrations;
            if (stream?.MassFlow != null)
                stream.MassFlow.ExecuteEquipmentSolver -= OnPropagateMassFlow;
            if (stream?.Pressure != null)
                stream.Pressure.ExecuteEquipmentSolver -= OnPropagatePressure;
        }

        // =========================
        // 🔹 HELPERS DE VARIABLES
        // =========================
        private IEnumerable<INewNewVariable> GetPressureVariables()
        {
            if (DeltaPressure != null) yield return DeltaPressure;
            if (Inlet?.Pressure != null) yield return Inlet.Pressure;
            if (Outlet?.Pressure != null) yield return Outlet.Pressure;
        }

        private IEnumerable<INewNewVariable> GetConcentrationVariables()
        {
            if (Inlet?.StreamComposition?.Value?.Components != null)
                foreach (var c in Inlet.StreamComposition.Value.Components)
                    yield return c.MolarFractionSolver;
            if (Outlet?.StreamComposition?.Value?.Components != null)
                foreach (var c in Outlet.StreamComposition.Value.Components)
                    yield return c.MolarFractionSolver;
        }

        private IEnumerable<INewNewVariable> GetMassBalanceVariables()
        {
            if (Inlet?.MassFlow != null) yield return Inlet.MassFlow;
            if (Outlet?.MassFlow != null) yield return Outlet.MassFlow;
        }

        public IEnumerable<INewNewVariable> GetEnergyBalanceVariables()
        {
            if (Inlet?.MassFlow != null) yield return Inlet.MassFlow;
            if (Inlet?.MassEnthalpy != null) yield return Inlet.MassEnthalpy;
            if (Outlet?.MassFlow != null) yield return Outlet.MassFlow;
            if (Outlet?.MassEnthalpy != null) yield return Outlet.MassEnthalpy;
        }

        // =========================
        // 🔹 ESTADO Y UI
        // =========================
        public PumpStateType State => GetState();
        private PumpStateType GetState()
        {
            if (Inlet == null || Outlet == null) return PumpStateType.PartiallyConnected;
            if (!DeltaPressure.IsDefined || !Efficiency.IsDefined || Efficiency.Value <= 0)
                return PumpStateType.ReadyToCalculate;
            if (Power?.IsDefined == true) return PumpStateType.Solved;
            return PumpStateType.ReadyToCalculate;
        }

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
            PumpStateType.Created => "#CBD5E0",
            PumpStateType.PartiallyConnected => "#F6AD55",
            PumpStateType.ReadyToCalculate => "#63B3ED",
            PumpStateType.Solved => "#34D399",
            _ => "#CBD5E0"
        };

        public override List<ToolTipLegend> GetToolTipLegend()
        {
            return new List<ToolTipLegend>
        {
            new("ΔP", DeltaPressure?.GetDisplayString() ?? "-"),
            new("%Efficiency", Efficiency?.GetDisplayString() ?? "-"),
            new("Power", Power?.GetDisplayString() ?? "-")
        };
        }
    }
    




    public class PumpSimulationFacade : EquipmentFacade
    {
        // ═══════════════════════════════════════════════════════════
        // 🔹 PUERTOS (nombres constantes para consistencia)
        // ═══════════════════════════════════════════════════════════
        public const string PortSuction = "Suction";
        public const string PortDischarge = "Discharge";

        // ═══════════════════════════════════════════════════════════
        // 🔹 CONEXIONES (privadas, acceso vía interfaz)
        // ═══════════════════════════════════════════════════════════
        private IStreamFacade? _suction;
        private IStreamFacade? _discharge;

        public IStreamFacade? Inlet => _suction;   // Legacy alias
        public IStreamFacade? Outlet => _discharge; // Legacy alias

        // ═══════════════════════════════════════════════════════════
        // 🔹 VARIABLES DE CONTROL
        // ═══════════════════════════════════════════════════════════
        public VariableAmount<PressureDrop> DeltaPressure { get; private set; }
        public VariableDouble Efficiency { get; private set; }
        public VariableAmount<Power> Power { get; private set; }

        // ═══════════════════════════════════════════════════════════
        // 🔹 CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════
        public PumpSimulationFacade()
        {
            DeltaPressure = new VariableAmount<PressureDrop>(
                new PressureDrop(), PressureDropUnits.Bar, PressureDropUnits.Pascal, (v, u) => new PressureDrop(v, u));
            Efficiency = new VariableDouble(0);
            Power = new VariableAmount<Power>(
                new Power(), PowerUnits.KiloWatt, PowerUnits.Watt, (v, u) => new Power(v, u));

            SubscribeVariables();
        }

        private void SubscribeVariables()
        {
            DeltaPressure.ExecuteStreamCalculation += CalculatePowerIfPossible;
            Efficiency.ExecuteStreamCalculation += CalculatePowerIfPossible;
            DeltaPressure.ExecuteGeneralSolver += () => ExecuteSolver();
            Efficiency.ExecuteGeneralSolver += () => ExecuteSolver();
            Power.ExecuteGeneralSolver += () => ExecuteSolver();
        }

        // ═══════════════════════════════════════════════════════════
        // 🔹 IMPLEMENTACIÓN DE IEquipmentFacade (MULTI-PUERTO)
        // ═══════════════════════════════════════════════════════════
        public override IEnumerable<string> GetPortNames()
        {
            yield return PortSuction;
            yield return PortDischarge;
        }

        public override IStreamFacade? GetConnectedStream(string portName)
        {
            return portName switch
            {
                PortSuction => _suction,
                PortDischarge => _discharge,
                _ => null
            };
        }

        public override IEnumerable<IVariable> GetControlledVariables()
        {
            yield return DeltaPressure;
            yield return Efficiency;
            yield return Power;
        }

        public override List<GlobalEquation> GetReactiveEquations(List<IVariable> allVariables)
        {
            var equations = new List<GlobalEquation>();
            if (_suction == null || _discharge == null) return equations;

            // Presión: P_out = P_in + ΔP
            equations.Add(new GlobalEquation
            {
                Function = vars =>
                {
                    var pIn = GetVarValue(vars, _suction!.Pressure);
                    var pOut = GetVarValue(vars, _discharge!.Pressure);
                    var dP = GetVarValue(vars, DeltaPressure);
                    return pOut - (pIn + dP);
                },
                Type = ReactiveEquationType.Model,
                EquipmentId = this.Id.ToString(),
                Description = "P_out = P_in + ΔP (Bomba)"
            });

            // Masa: ṁ_out = ṁ_in
            equations.Add(new GlobalEquation
            {
                Function = vars =>
                {
                    var mIn = GetVarValue(vars, _suction!.MassFlow);
                    var mOut = GetVarValue(vars, _discharge!.MassFlow);
                    return mOut - mIn;
                },
                Type = ReactiveEquationType.Connection,
                EquipmentId = this.Id.ToString(),
                Description = "ṁ_out = ṁ_in (Conservación de masa)"
            });

            // Energía (si eficiencia definida)
            if (Efficiency.IsDefinedByUI || Efficiency.NewSolverValue.HasValue)
            {
                equations.Add(new GlobalEquation
                {
                    Function = vars =>
                    {
                        var mIn = GetVarValue(vars, _suction!.MassFlow);
                        var hin = GetVarValue(vars, _suction!.MassEnthalpy);
                        var mOut = GetVarValue(vars, _discharge!.MassFlow);
                        var hout = GetVarValue(vars, _discharge!.MassEnthalpy);
                        var dP = GetVarValue(vars, DeltaPressure);
                        var effValue = Efficiency.GetEffectiveSolverValue();
                        var efficiency = effValue > 0 ? effValue / 100 : 1;
                        var rho = 1000.0;
                        var w = dP / (rho * efficiency);
                        return (mOut * hout) - (mIn * hin + mIn * w);
                    },
                    Type = ReactiveEquationType.EnergyBalance,
                    EquipmentId = this.Id.ToString(),
                    Description = "ṁ·h_out = ṁ·h_in + ṁ·w (Energía)"
                });
            }

            return equations;
        }

        // ═══════════════════════════════════════════════════════════
        // 🔹 CÁLCULO LOCAL DE POTENCIA
        // ═══════════════════════════════════════════════════════════
        private void CalculatePowerIfPossible()
        {
            if (_suction == null || _discharge == null) return;
            if (!Efficiency.IsDefined || !DeltaPressure.IsDefined || Efficiency.GetEffectiveSolverValue() <= 0) return;

            var massFlow = _suction.MassFlow.GetEffectiveSolverValue();
            if (Math.Abs(massFlow) < 1e-9) return;

            var deltaP = DeltaPressure.GetEffectiveSolverValue();
            var effValue = Efficiency.GetEffectiveSolverValue();
            var efficiency = effValue > 0 ? effValue / 100 : 1;
            var rho = 1000.0;
            var w = deltaP / (rho * efficiency);
            var power = massFlow * w;

            if (!Power.IsDefinedByUI) Power.NewSolverValue = power;
        }

        // ═══════════════════════════════════════════════════════════
        // 🔹 CONEXIÓN/DESCONEXIÓN
        // ═══════════════════════════════════════════════════════════
        public override void AttachConnection(string portName, IStreamFacade connectedFacade)
        {
            if (portName == PortSuction && _suction == null)
            {
                _suction = connectedFacade;
                SubscribeStream(_suction);
                TriggerPropagation();
            }
            else if (portName == PortDischarge && _discharge == null)
            {
                _discharge = connectedFacade;
                SubscribeStream(_discharge);
                TriggerPropagation();
            }
        }

        public override void DetachConnection(string portName)
        {
            if (portName == PortSuction && _suction != null)
            {
                UnsubscribeStream(_suction);
                ClearCalculatedValues(_suction);
                ClearCalculatedValues(_discharge!);
                if (!Power.IsDefinedByUI) Power.ClearFromStream();
                _suction = null;
                ExecuteSolver();
            }
            else if (portName == PortDischarge && _discharge != null)
            {
                UnsubscribeStream(_discharge);
                ClearCalculatedValues(_discharge);
                ClearCalculatedValues(_suction!);
                if (!Power.IsDefinedByUI) Power.ClearFromStream();
                _discharge = null;
                ExecuteSolver();
            }
        }

        private void SubscribeStream(IStreamFacade stream)
        {
            stream.Pressure.ExecuteStreamCalculation += CalculatePowerIfPossible;
            stream.MassFlow.ExecuteStreamCalculation += CalculatePowerIfPossible;
            stream.MassEnthalpy.ExecuteStreamCalculation += CalculatePowerIfPossible;
        }

        private void UnsubscribeStream(IStreamFacade stream)
        {
            stream.Pressure.ExecuteStreamCalculation -= CalculatePowerIfPossible;
            stream.MassFlow.ExecuteStreamCalculation -= CalculatePowerIfPossible;
            stream.MassEnthalpy.ExecuteStreamCalculation -= CalculatePowerIfPossible;
        }

        private void TriggerPropagation()
        {
            CalculatePowerIfPossible();
            ExecuteSolver();
        }

        // ═══════════════════════════════════════════════════════════
        // 🔹 ESTADO Y UI
        // ═══════════════════════════════════════════════════════════
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
            PumpStateType.Created => "#CBD5E0",
            PumpStateType.PartiallyConnected => "#F6AD55",
            PumpStateType.ReadyToCalculate => "#63B3ED",
            PumpStateType.Solved => "#34D399",
            _ => "#CBD5E0"
        };

        public PumpStateType State => GetState();

        private PumpStateType GetState()
        {
            if (_suction == null || _discharge == null) return PumpStateType.PartiallyConnected;
            if (!DeltaPressure.IsDefined || !Efficiency.IsDefined || Efficiency.GetEffectiveSolverValue() <= 0)
                return PumpStateType.ReadyToCalculate;
            return Power.IsDefined ? PumpStateType.Solved : PumpStateType.ReadyToCalculate;
        }

        public override List<ToolTipLegend> GetToolTipLegend()
        {
            return new List<ToolTipLegend>
        {
            new("ΔP", DeltaPressure.GetDisplayString()),
            new("Efficiency", Efficiency.GetDisplayString()),
            new("Power", Power.GetDisplayString())
        };
        }
    }


}
