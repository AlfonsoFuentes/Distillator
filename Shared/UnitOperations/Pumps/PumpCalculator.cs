using Shared.Thermodynamics.ControlledVariables;
using Shared.UnitOperations.Streams;
using System;
using System.Collections.Generic;
using System.Text;
using UnitSystem;

using System;

namespace Shared.UnitOperations.Pumps
{


    
    // ========================================================================
    // 1. LOS ENUMS (Sin Magic Strings)
    // ========================================================================
    //public enum PumpEq
    //{
    //    MechBalance,
    //    SuctionThermo,
    //    IsentropicCompression,
    //    DischargeThermo,
    //    PowerBalance
    //}

    //public enum PumpVar
    //{
    //    Pin, Pout, DeltaP, Tin, Tout,
    //    HinMolar, HinMass, SinMolar, HoutMolar, HoutMass,
    //    Efficiency, MassFlow, Power
    //}

    //// ========================================================================
    //// 2. EL NODO GENÉRICO DE ECUACIÓN
    //// ========================================================================
    //public class EquationNode<TEq, TVar>
    //    where TEq : struct, Enum
    //    where TVar : struct, Enum
    //{
    //    public TEq Id { get; }
    //    public Func<Dictionary<TVar, double>, Dictionary<TVar, double>> TrySolve { get; }

    //    public EquationNode(TEq id, Func<Dictionary<TVar, double>, Dictionary<TVar, double>> trySolve)
    //    {
    //        Id = id;
    //        TrySolve = trySolve;
    //    }
    //}

    //// ========================================================================
    //// 3. EL CALCULADOR DE LA BOMBA
    //// ========================================================================
    //public class PumpCalculator : EquipmentCalculatorBase
    //{
    //    private readonly PumpSimulationFacade _pump;
    //    private readonly List<EquationNode<PumpEq, PumpVar>> _equations = new();
    //    private readonly Dictionary<PumpVar, double> _pool = new();

    //    protected override string EquipmentSourceId => _pump.Name;

    //    public PumpCalculator(PumpSimulationFacade pump)
    //    {
    //        _pump = pump;
    //        BuildEquationBank();
    //    }

    //    // ========================================================================
    //    // CONTRATOS ABSTRACTOS DE LA BASE
    //    // ========================================================================

    //    protected override bool IsTopologyValid() =>
    //        _pump.SuctionStream != null && _pump.DischargeStream != null;

    //    protected override void PropagateBaseProperties()
    //    {
    //        var suction = _pump.SuctionStream!;
    //        var discharge = _pump.DischargeStream!;

    //        // 1. PROPAGACIÓN DE MÉTODO TERMODINÁMICO
    //        if (suction.ThermodynamicMethod.IsDefined && !discharge.ThermodynamicMethod.IsDefined)
    //        {
    //            discharge.ThermodynamicMethod.SetValue(suction.ThermodynamicMethod.Value, MethodSource.Other, EquipmentSourceId);
    //            RegisterPropagatedVariable(discharge.ThermodynamicMethod);
    //        }
    //        else if (discharge.ThermodynamicMethod.IsDefined && discharge.ThermodynamicMethod.Source == MethodSource.UserInterface && !suction.ThermodynamicMethod.IsDefined)
    //        {
    //            suction.ThermodynamicMethod.SetValue(discharge.ThermodynamicMethod.Value, MethodSource.Other, EquipmentSourceId);
    //            RegisterPropagatedVariable(suction.ThermodynamicMethod);
    //        }

    //        // 2. PROPAGACIÓN DE COMPOSICIÓN
    //        if (suction.StreamCompositionControlled.IsDefined && !discharge.StreamCompositionControlled.IsDefined)
    //        {
    //            discharge.StreamCompositionControlled.SetValue(suction.StreamCompositionControlled.Value!.Clone(), MethodSource.Other, EquipmentSourceId);
    //            RegisterPropagatedVariable(discharge.StreamCompositionControlled);
    //        }
    //        else if (discharge.StreamCompositionControlled.IsDefined && discharge.StreamCompositionControlled.Source == MethodSource.UserInterface && !suction.StreamCompositionControlled.IsDefined)
    //        {
    //            suction.StreamCompositionControlled.SetValue(discharge.StreamCompositionControlled.Value!.Clone(), MethodSource.Other, EquipmentSourceId);
    //            RegisterPropagatedVariable(suction.StreamCompositionControlled);
    //        }

    //        // 3. PROPAGACIÓN DE FLUJO MÁSICO
    //        if (suction.MassFlowControlled.IsDefined && !discharge.MassFlowControlled.IsDefined)
    //        {
    //            var cloneFlow = new MassFlow(suction.MassFlowControlled.Value!.Value, suction.MassFlowControlled.Value.UnitName);
    //            discharge.MassFlowControlled.SetValue(cloneFlow, MethodSource.Other, EquipmentSourceId);
    //            RegisterPropagatedVariable(discharge.MassFlowControlled);
    //        }
    //        else if (discharge.MassFlowControlled.IsDefined && discharge.MassFlowControlled.Source == MethodSource.UserInterface && !suction.MassFlowControlled.IsDefined)
    //        {
    //            var cloneFlow = new MassFlow(discharge.MassFlowControlled.Value!.Value, discharge.MassFlowControlled.Value.UnitName);
    //            suction.MassFlowControlled.SetValue(cloneFlow, MethodSource.Other, EquipmentSourceId);
    //            RegisterPropagatedVariable(suction.MassFlowControlled);
    //        }
    //    }

    //    protected override bool IsReadyForThermodynamicCalculation()
    //    {
    //        var suction = _pump.SuctionStream!;
    //        var discharge = _pump.DischargeStream!;

    //        if (!suction.StreamCompositionControlled.IsDefined && !discharge.StreamCompositionControlled.IsDefined)
    //            return false;

    //        bool hasFlow = suction.MassFlowControlled.IsDefined || discharge.MassFlowControlled.IsDefined;
    //        bool hasEff = _pump.AdiabaticEfficiency.IsDefined;

    //        return hasFlow && hasEff;
    //    }

    //    // ========================================================================
    //    // EL BANCO DE ECUACIONES (Física + Reactividad)
    //    // ========================================================================
    //    private void BuildEquationBank()
    //    {
    //        // ECUACIÓN 1: Balance Mecánico
    //        _equations.Add(new EquationNode<PumpEq, PumpVar>(PumpEq.MechBalance, knowns => {
    //            var res = new Dictionary<PumpVar, double>();
    //            bool hasPin = knowns.ContainsKey(PumpVar.Pin);
    //            bool hasPout = knowns.ContainsKey(PumpVar.Pout);
    //            bool hasDp = knowns.ContainsKey(PumpVar.DeltaP);

    //            if (hasPin && hasPout && !hasDp)
    //            {
    //                res[PumpVar.DeltaP] = knowns[PumpVar.Pout] - knowns[PumpVar.Pin];
    //                if (!_pump.DeltaPressure.IsDefined)
    //                {
    //                    // SILENCIOSO: El DeltaP no dispara equilibrios.
    //                    _pump.DeltaPressure.SetValueCalculated(new PressureDrop(res[PumpVar.DeltaP], PressureDropUnits.Bar), EquipmentSourceId);
    //                    RegisterPropagatedVariable(_pump.DeltaPressure);
    //                }
    //            }
    //            else if (hasPin && hasDp && !hasPout)
    //            {
    //                res[PumpVar.Pout] = knowns[PumpVar.Pin] + knowns[PumpVar.DeltaP];
    //                var dis = _pump.DischargeStream!;
    //                if (!dis.PressureControlled.IsDefined)
    //                {
    //                    // ACTIVO: Usamos SetValue. Si la corriente ya tiene otra variable (ej. T o H), calculará su Flash.
    //                    dis.PressureControlled.SetValue(new Pressure(res[PumpVar.Pout], PressureUnits.Bara), MethodSource.Other, EquipmentSourceId);
    //                    RegisterPropagatedVariable(dis.PressureControlled);
    //                }
    //            }
    //            else if (hasPout && hasDp && !hasPin)
    //            {
    //                res[PumpVar.Pin] = knowns[PumpVar.Pout] - knowns[PumpVar.DeltaP];
    //                var suc = _pump.SuctionStream!;
    //                if (!suc.PressureControlled.IsDefined)
    //                {
    //                    // ACTIVO: Usamos SetValue para intentar disparar el cálculo aguas arriba.
    //                    suc.PressureControlled.SetValue(new Pressure(res[PumpVar.Pin], PressureUnits.Bara), MethodSource.Other, EquipmentSourceId);
    //                    RegisterPropagatedVariable(suc.PressureControlled);
    //                }
    //            }
    //            return res;
    //        }));

    //        // ECUACIÓN 2: Termodinámica de Succión (Leer datos del Facade)
    //        _equations.Add(new EquationNode<PumpEq, PumpVar>(PumpEq.SuctionThermo, knowns => {
    //            var res = new Dictionary<PumpVar, double>();
    //            if (knowns.ContainsKey(PumpVar.Pin) && knowns.ContainsKey(PumpVar.Tin) && !knowns.ContainsKey(PumpVar.HinMolar))
    //            {
    //                var suc = _pump.SuctionStream!;

    //                // Extraemos las propiedades que el Facade ya calculó en su Flash PT
    //                if (suc.MolarEnthalpy.IsDefined && suc.MassEnthalpy.IsDefined && suc.MolarEntropy.IsDefined)
    //                {
    //                    res[PumpVar.HinMolar] = suc.MolarEnthalpy.Value!.GetValue(MolarEnergyUnits.J_Kgmol);
    //                    res[PumpVar.HinMass] = suc.MassEnthalpy.Value!.GetValue(MassEnergyUnits.J_Kg);
    //                    res[PumpVar.SinMolar] = suc.MolarEntropy.Value!.GetValue(MolarEntropyUnits.J_Kgmol_C);
    //                }
    //            }
    //            return res;
    //        }));

    //        // ECUACIÓN 3: Compresión Isentrópica (Inyectar S temporalmente)
    //        _equations.Add(new EquationNode<PumpEq, PumpVar>(PumpEq.IsentropicCompression, knowns => {
    //            var res = new Dictionary<PumpVar, double>();
    //            if (knowns.ContainsKey(PumpVar.Pout) && knowns.ContainsKey(PumpVar.SinMolar) &&
    //                knowns.ContainsKey(PumpVar.HinMass) && knowns.ContainsKey(PumpVar.Efficiency) &&
    //                !knowns.ContainsKey(PumpVar.HoutMolar))
    //            {
    //                var dis = _pump.DischargeStream!;

    //                // 1. ACTIVO: Inyectamos Entropía con SetValue. 
    //                // Esto dispara el evento ConstraintsChanged -> EquilibriumCalculator -> PSStrategy
    //                dis.MolarEntropy.SetValue(new MolarEntropy(knowns[PumpVar.SinMolar], MolarEntropyUnits.J_Kgmol_C), MethodSource.Other, EquipmentSourceId);

    //                // 2. La reactividad es síncrona. Si calculó, la Entalpía ideal ya debe existir.
    //                if (dis.MassEnthalpy.Value != null)
    //                {
    //                    double hOutIdealMass = dis.MassEnthalpy.Value.GetValue(MassEnergyUnits.J_Kg);

    //                    // 3. Limpiamos la entropía temporal para no dejar basura en la corriente
    //                    dis.MolarEntropy.ClearValue();

    //                    // 4. Balance real
    //                    double hInMass = knowns[PumpVar.HinMass];
    //                    double eff = knowns[PumpVar.Efficiency];
    //                    double hOutRealMass = hInMass + ((hOutIdealMass - hInMass) / eff);

    //                    res[PumpVar.HoutMass] = hOutRealMass;
    //                    res[PumpVar.HoutMolar] = hOutRealMass * dis.MaterialStream.MolecularWeight;
    //                }
    //            }
    //            return res;
    //        }));

    //        // ECUACIÓN 4: Termodinámica de Descarga (Inyectar H real)
    //        _equations.Add(new EquationNode<PumpEq, PumpVar>(PumpEq.DischargeThermo, knowns => {
    //            var res = new Dictionary<PumpVar, double>();
    //            if (knowns.ContainsKey(PumpVar.Pout) && knowns.ContainsKey(PumpVar.HoutMolar) && !knowns.ContainsKey(PumpVar.Tout))
    //            {
    //                var dis = _pump.DischargeStream!;

    //                // 1. ACTIVO: Inyectamos Entalpía real con SetValue. 
    //                // Esto dispara ConstraintsChanged -> EquilibriumCalculator -> PHStrategy
    //                dis.MolarEnthalpy.SetValue(new MolarEnergy(knowns[PumpVar.HoutMolar], MolarEnergyUnits.J_Kgmol), MethodSource.Other, EquipmentSourceId);
    //                RegisterPropagatedVariable(dis.MolarEnthalpy);

    //                // 2. Extraemos la Temperatura final que acaba de calcular el Flash
    //                if (dis.TemperatureControlled.Value != null)
    //                {
    //                    res[PumpVar.Tout] = dis.TemperatureControlled.Value.GetValue(TemperatureUnits.DegreeCelcius);

    //                    // Aseguramos que si la T fue calculada, quede registrada como hija de este equipo
    //                    if (dis.TemperatureControlled.Source == MethodSource.Other && dis.TemperatureControlled.SourceId == EquipmentSourceId)
    //                    {
    //                        RegisterPropagatedVariable(dis.TemperatureControlled);
    //                    }
    //                }
    //            }
    //            return res;
    //        }));

    //        // ECUACIÓN 5: Potencia Mecánica
    //        _equations.Add(new EquationNode<PumpEq, PumpVar>(PumpEq.PowerBalance, knowns => {
    //            var res = new Dictionary<PumpVar, double>();
    //            if (knowns.ContainsKey(PumpVar.MassFlow) && knowns.ContainsKey(PumpVar.HoutMass) &&
    //                knowns.ContainsKey(PumpVar.HinMass) && !knowns.ContainsKey(PumpVar.Power))
    //            {
    //                double mFlow_kg_s = knowns[PumpVar.MassFlow] / 3600.0;
    //                double wKw = (mFlow_kg_s * (knowns[PumpVar.HoutMass] - knowns[PumpVar.HinMass])) / 1000.0;

    //                res[PumpVar.Power] = wKw;

    //                if (!_pump.PowerConsumed.IsDefined)
    //                {
    //                    // SILENCIOSO: La potencia no desencadena termodinámica
    //                    _pump.PowerConsumed.SetValueCalculated(new Power(wKw, PowerUnits.KiloWatt), EquipmentSourceId);
    //                    RegisterPropagatedVariable(_pump.PowerConsumed);
    //                }
    //            }
    //            return res;
    //        }));
    //    }

    //    // ========================================================================
    //    // EL MOTOR DE INFERENCIA ITERATIVO
    //    // ========================================================================
    //    protected override void ExecuteThermodynamics()
    //    {
    //        _pool.Clear();
    //        LoadKnownVariables();

    //        bool madeProgress = true;
    //        var solvedEquations = new HashSet<PumpEq>();

    //        while (madeProgress)
    //        {
    //            madeProgress = false;
    //            foreach (var eq in _equations)
    //            {
    //                if (solvedEquations.Contains(eq.Id)) continue;

    //                var newVars = eq.TrySolve(_pool);
    //                if (newVars.Count > 0)
    //                {
    //                    foreach (var kvp in newVars) _pool[kvp.Key] = kvp.Value;
    //                    solvedEquations.Add(eq.Id);
    //                    madeProgress = true;
    //                }
    //            }
    //        }

    //        // Si logramos resolver la T de salida y la Potencia, el equipo se declara exitoso.
    //        if (_pool.ContainsKey(PumpVar.Tout) && _pool.ContainsKey(PumpVar.Power))
    //        {
    //            _pump.State = PumpStateType.Solved;
    //        }
    //    }

    //    private void LoadKnownVariables()
    //    {
    //        var suc = _pump.SuctionStream!;
    //        var dis = _pump.DischargeStream!;

    //        if (suc.PressureControlled.IsDefined) _pool[PumpVar.Pin] = suc.PressureControlled.GetValueInUnit(PressureUnits.Bara);
    //        if (dis.PressureControlled.IsDefined) _pool[PumpVar.Pout] = dis.PressureControlled.GetValueInUnit(PressureUnits.Bara);
    //        if (_pump.DeltaPressure.IsDefined) _pool[PumpVar.DeltaP] = _pump.DeltaPressure.GetValueInUnit(PressureDropUnits.Bar);

    //        if (suc.TemperatureControlled.IsDefined) _pool[PumpVar.Tin] = suc.TemperatureControlled.GetValueInUnit(TemperatureUnits.DegreeCelcius);
    //        if (dis.TemperatureControlled.IsDefined) _pool[PumpVar.Tout] = dis.TemperatureControlled.GetValueInUnit(TemperatureUnits.DegreeCelcius);

    //        if (_pump.AdiabaticEfficiency.IsDefined) _pool[PumpVar.Efficiency] = _pump.AdiabaticEfficiency.Value / 100.0;

    //        if (suc.MassFlowControlled.IsDefined) _pool[PumpVar.MassFlow] = suc.MassFlowControlled.GetValueInUnit(MassFlowUnits.Kg_hr);
    //        else if (dis.MassFlowControlled.IsDefined) _pool[PumpVar.MassFlow] = dis.MassFlowControlled.GetValueInUnit(MassFlowUnits.Kg_hr);
    //    }
    //}
    //public class PumpCalculator : EquipmentCalculatorBase
    //{
    //    private readonly PumpSimulationFacade _pump;
    //    private IPumpCalculationStrategy? _selectedStrategy;

    //    protected override string EquipmentSourceId => _pump.Name;

    //    public PumpCalculator(PumpSimulationFacade pump) => _pump = pump;

    //    protected override bool IsTopologyValid() => _pump.SuctionStream != null && _pump.DischargeStream != null;

    //    // ========================================================================
    //    // PASO 3: PROPAGACIÓN BASE (Efecto Espejo)
    //    // ========================================================================
    //    protected override void PropagateBaseProperties()
    //    {
    //        var suction = _pump.SuctionStream!;
    //        var discharge = _pump.DischargeStream!;

    //        // --- 0. PROPAGACIÓN DEL MÉTODO TERMODINÁMICO ---
    //        if (suction.ThermodynamicMethod.IsDefined && !discharge.ThermodynamicMethod.IsDefined)
    //        {
    //            discharge.ThermodynamicMethod.SetValue(suction.ThermodynamicMethod.Value, MethodSource.Other, EquipmentSourceId);
    //            RegisterPropagatedVariable(discharge.ThermodynamicMethod);
    //        }
    //        else if (discharge.ThermodynamicMethod.IsDefined && !suction.ThermodynamicMethod.IsDefined)
    //        {
    //            suction.ThermodynamicMethod.SetValue(discharge.ThermodynamicMethod.Value, MethodSource.Other, EquipmentSourceId);
    //            RegisterPropagatedVariable(suction.ThermodynamicMethod);
    //        }

    //        // --- 1. PROPAGACIÓN DE COMPOSICIÓN ---
    //        if (suction.StreamCompositionControlled.IsDefined && !discharge.StreamCompositionControlled.IsDefined)
    //        {
    //            discharge.StreamCompositionControlled.SetValue(suction.StreamCompositionControlled.Value!.Clone(), MethodSource.Other, EquipmentSourceId);
    //            RegisterPropagatedVariable(discharge.StreamCompositionControlled);
    //        }
    //        else if (discharge.StreamCompositionControlled.IsDefined && discharge.StreamCompositionControlled.Source == MethodSource.UserInterface && !suction.StreamCompositionControlled.IsDefined)
    //        {
    //            suction.StreamCompositionControlled.SetValue(discharge.StreamCompositionControlled.Value!.Clone(), MethodSource.Other, EquipmentSourceId);
    //            RegisterPropagatedVariable(suction.StreamCompositionControlled);
    //        }

    //        // --- 2. PROPAGACIÓN DE FLUJO MÁSICO ---
    //        if (suction.MassFlowControlled.IsDefined && !discharge.MassFlowControlled.IsDefined)
    //        {
    //            var cloneFlow = new MassFlow(suction.MassFlowControlled.Value!.Value, suction.MassFlowControlled.Value.UnitName);
    //            discharge.MassFlowControlled.SetValue(cloneFlow, MethodSource.Other, EquipmentSourceId);
    //            RegisterPropagatedVariable(discharge.MassFlowControlled);
    //        }
    //        else if (discharge.MassFlowControlled.IsDefined && discharge.MassFlowControlled.Source == MethodSource.UserInterface && !suction.MassFlowControlled.IsDefined)
    //        {
    //            var cloneFlow = new MassFlow(discharge.MassFlowControlled.Value!.Value, discharge.MassFlowControlled.Value.UnitName);
    //            suction.MassFlowControlled.SetValue(cloneFlow, MethodSource.Other, EquipmentSourceId);
    //            RegisterPropagatedVariable(suction.MassFlowControlled);
    //        }
    //    }

    //    // ========================================================================
    //    // PASO 4: REGLA DE ORO Y SELECCIÓN DE ESTRATEGIA
    //    // ========================================================================
    //    protected override bool IsReadyForThermodynamicCalculation()
    //    {
    //        var suction = _pump.SuctionStream!;
    //        var discharge = _pump.DischargeStream!;
    //        _selectedStrategy = null;

    //        // 1. Requisito mínimo: Composición en algún lado
    //        if (!suction.StreamCompositionControlled.IsDefined && !discharge.StreamCompositionControlled.IsDefined)
    //            return false;

    //        // 2. Estados de las variables clave
    //        bool suctionReady = suction.State == StreamStateType.StreamCalculated;

    //        // 👇 CAMBIO VITAL: Solo miramos si la presión de salida existe
    //        bool dischargeHasPressure = discharge.PressureControlled.IsDefined;

    //        bool deltaPReady = _pump.DeltaPressure.IsDefined;

    //        // Caso 1: Forward (Succión completa + DeltaP en bomba)
    //        if (suctionReady && deltaPReady)
    //        {
    //            if (suction.VaporFractionControlled.Value > 0.0001) return false;
    //            _selectedStrategy = new PumpForwardStrategy();
    //            return true;
    //        }

    //        // Caso 3: Calcular DeltaP (Succión completa + Presión en la Salida)
    //        // 👇 Aquí es donde "esa mondá" se destraba
    //        if (suctionReady && dischargeHasPressure && !deltaPReady)
    //        {
    //            _selectedStrategy = new PumpDeltaPStrategy();
    //            return true;
    //        }

    //        // Caso 2: Backward (Si algún día necesitas calcular hacia atrás)
    //        if (discharge.State == StreamStateType.StreamCalculated && deltaPReady)
    //        {
    //            _selectedStrategy = new PumpBackwardStrategy();
    //            return true;
    //        }

    //        return false;
    //    }

    //    // ========================================================================
    //    // PASO 5: EJECUCIÓN Y REGISTRO DINÁMICO
    //    // ========================================================================
    //    protected override void ExecuteThermodynamics()
    //    {
    //        if (_selectedStrategy == null) return;

    //        // 1. La estrategia ejecuta y nos devuelve EXACTAMENTE lo que modificó
    //        IEnumerable<IControlledVariable> calculatedVariables = _selectedStrategy.Calculate(_pump, EquipmentSourceId);

    //        // 2. Registramos dinámicamente para que el borrado funcione a la perfección
    //        foreach (var variable in calculatedVariables)
    //        {
    //            RegisterPropagatedVariable(variable);
    //        }

    //        _pump.State = PumpStateType.Solved;
    //    }
    //}

    //// ========================================================================
    //// INTERFAZ DE ESTRATEGIAS
    //// ========================================================================
    //public interface IPumpCalculationStrategy
    //{
    //    IEnumerable<IControlledVariable> Calculate(PumpSimulationFacade pump, string sourceId);
    //}

    //// ========================================================================
    //// ESTRATEGIA 1: FORWARD (De Succión a Descarga)
    //// ========================================================================
    //public class PumpForwardStrategy : IPumpCalculationStrategy
    //{
    //    public IEnumerable<IControlledVariable> Calculate(PumpSimulationFacade pump, string sourceId)
    //    {
    //        var suction = pump.SuctionStream!;
    //        var discharge = pump.DischargeStream!;

    //        double pIn = suction.PressureControlled.GetValueInUnit(PressureUnits.Bara);
    //        double tIn = suction.TemperatureControlled.GetValueInUnit(TemperatureUnits.DegreeCelcius);
    //        double deltaP = pump.DeltaPressure.GetValueInUnit(PressureDropUnits.Bar);
    //        double eff = pump.AdiabaticEfficiency.Value / 100.0;
    //        double massFlowKgH = suction.MassFlowControlled.GetValueInUnit(MassFlowUnits.Kg_hr);

    //        double densityKgM3 = suction.MassDensity.IsDefined ? suction.MassDensity.GetValueInUnit(MassDensityUnits.Kg_m3) : 1000.0;
    //        double cpKcalKgC = suction.MassCp.IsDefined ? suction.MassCp.GetValueInUnit(MassEntropyUnits.Kcal_Kg_C) : 1.0;
    //        double cpKjKgC = cpKcalKgC * 4.184;

    //        // Cálculos
    //        double pOut = pIn + deltaP;
    //        double volFlowM3s = (massFlowKgH / 3600.0) / densityKgM3;
    //        double deltaPa = deltaP * 100000.0;
    //        double powerKw = eff > 0 ? (volFlowM3s * deltaPa) / (1000.0 * eff) : 0;

    //        double massFlowKgS = massFlowKgH / 3600.0;
    //        double deltaT = massFlowKgS > 0 && cpKjKgC > 0 ? powerKw / (massFlowKgS * cpKjKgC) : 0;
    //        double tOut = tIn + deltaT;

    //        // Asignación
    //        pump.PowerConsumed.SetValueCalculated(new Power(powerKw, PowerUnits.KiloWatt), sourceId);
    //        discharge.PressureControlled.SetValue(new Pressure(pOut, PressureUnits.Bara), MethodSource.Other, sourceId);
    //        discharge.TemperatureControlled.SetValue(new Temperature(tOut, TemperatureUnits.DegreeCelcius), MethodSource.Other, sourceId);

    //        // Reporte de variables calculadas
    //        return new List<IControlledVariable>
    //        {
    //            pump.PowerConsumed,
    //            discharge.PressureControlled,
    //            discharge.TemperatureControlled
    //        };
    //    }
    //}

    //// ========================================================================
    //// ESTRATEGIA 2: BACKWARD (De Descarga a Succión)
    //// ========================================================================
    //public class PumpBackwardStrategy : IPumpCalculationStrategy
    //{
    //    public IEnumerable<IControlledVariable> Calculate(PumpSimulationFacade pump, string sourceId)
    //    {
    //        var suction = pump.SuctionStream!;
    //        var discharge = pump.DischargeStream!;

    //        double pOut = discharge.PressureControlled.GetValueInUnit(PressureUnits.Bara);
    //        double tOut = discharge.TemperatureControlled.GetValueInUnit(TemperatureUnits.DegreeCelcius);
    //        double deltaP = pump.DeltaPressure.GetValueInUnit(PressureDropUnits.Bar);
    //        double eff = pump.AdiabaticEfficiency.Value / 100.0;
    //        double massFlowKgH = discharge.MassFlowControlled.GetValueInUnit(MassFlowUnits.Kg_hr);

    //        double densityKgM3 = discharge.MassDensity.IsDefined ? discharge.MassDensity.GetValueInUnit(MassDensityUnits.Kg_m3) : 1000.0;
    //        double cpKcalKgC = discharge.MassCp.IsDefined ? discharge.MassCp.GetValueInUnit(MassEntropyUnits.Kcal_Kg_C) : 1.0;
    //        double cpKjKgC = cpKcalKgC * 4.184;

    //        // Cálculos inversos
    //        double pIn = pOut - deltaP;
    //        double volFlowM3s = (massFlowKgH / 3600.0) / densityKgM3;
    //        double deltaPa = deltaP * 100000.0;
    //        double powerKw = eff > 0 ? (volFlowM3s * deltaPa) / (1000.0 * eff) : 0;

    //        double massFlowKgS = massFlowKgH / 3600.0;
    //        double deltaT = massFlowKgS > 0 && cpKjKgC > 0 ? powerKw / (massFlowKgS * cpKjKgC) : 0;
    //        double tIn = tOut - deltaT;

    //        // Asignación
    //        pump.PowerConsumed.SetValueCalculated(new Power(powerKw, PowerUnits.KiloWatt), sourceId);
    //        suction.PressureControlled.SetValue(new Pressure(pIn, PressureUnits.Bara), MethodSource.Other, sourceId);
    //        suction.TemperatureControlled.SetValue(new Temperature(tIn, TemperatureUnits.DegreeCelcius), MethodSource.Other, sourceId);

    //        // Reporte de variables calculadas
    //        return new List<IControlledVariable>
    //            {
    //                pump.PowerConsumed,
    //                suction.PressureControlled,
    //                suction.TemperatureControlled
    //            };
    //    }
    //}

    //// ========================================================================
    //// ESTRATEGIA 3: CÁLCULO DE DELTA P (Succión y Descarga definidas)
    //// ========================================================================
    //public class PumpDeltaPStrategy : IPumpCalculationStrategy
    //{
    //    public IEnumerable<IControlledVariable> Calculate(PumpSimulationFacade pump, string sourceId)
    //    {
    //        var suction = pump.SuctionStream!;
    //        var discharge = pump.DischargeStream!;

    //        double pIn = suction.PressureControlled.GetValueInUnit(PressureUnits.Bara);
    //        double pOut = discharge.PressureControlled.GetValueInUnit(PressureUnits.Bara);

    //        double calculatedDeltaP = pOut - pIn;

    //        // Asignamos el DeltaP calculado
    //        pump.DeltaPressure.SetValueCalculated(new PressureDrop(calculatedDeltaP, PressureDropUnits.Bar), sourceId);

    //        // Llamamos a la estrategia Forward para que complete la Potencia y la Temperatura
    //        var forwardStrategy = new PumpForwardStrategy();
    //        var forwardResults = forwardStrategy.Calculate(pump, sourceId);

    //        // Compilamos todos los resultados (El Delta P + Lo que calculó el Forward)
    //        var allResults = new List<IControlledVariable>
    //        {
    //            pump.DeltaPressure
    //        };

    //        allResults.AddRange(forwardResults);

    //        return allResults;
    //    }
    //}

}

