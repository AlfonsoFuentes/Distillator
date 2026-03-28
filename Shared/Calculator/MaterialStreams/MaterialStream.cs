using Shared.Calculator.Components;
using Shared.Calculator.ProcessVariables;
using Shared.Calculator.Solvers;
using Shared.Thermodynamics.Methods;
using UnitSystem;

namespace Shared.Calculator.MaterialStreams
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class MaterialStream : StreamBase<StreamComponent>
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; set; } = "S-100";
        public ThermodynamicState CurrenState { get; set; } = ThermodynamicState.Undefined;
        public ProcessVariableUnitLess VaporFraction { get; set; }

        public LiquidPhase Liquid { get; private set; }
        public VaporPhase Vapor { get; private set; }

        public ProcessMethodProperty ProcessThermoMethod { get; set; } = null!;
        public ProcessCompositionProperty CompositionState { get; private set; }

        public MaterialStream() : base()
        {
            VaporFraction = new ProcessVariableUnitLess("Vapor Fraction", true);

            Liquid = new LiquidPhase();
            Vapor = new VaporPhase();

            ProcessThermoMethod = new ProcessMethodProperty("Thermo Method", true);
            CompositionState = new ProcessCompositionProperty(this.Components, true);
        }

        // ... (Mantén aquí todos tus métodos Validate, SetMethod, FlowConsistency y FlashEquilibrium tal cual están) ...

        // =========================================================================
        // ORQUESTADOR CENTRAL DE PROPIEDADES (Arquitectura Ultra-Limpia)
        // =========================================================================
        public void CalculateStreamProperties()
        {
            if (CurrenState == ThermodynamicState.Undefined) return;

            Amount tSys = Temperature.Data;
            Amount pSys = Pressure.Data;
            Amount tSat = SaturationTemperature;

            switch (CurrenState)
            {
                case ThermodynamicState.SubcooledLiquid:
                    // 1. FÍSICA DIRECTA
                    this.CalculateBulkProperties(ThermodynamicState.SubcooledLiquid);
                    // 2. REFERENCIA UI (Vapor fantasma a Tsat)
                  
                    break;

                case ThermodynamicState.SaturatedLiquid:
                    // 1. FÍSICA DIRECTA (Forzando Tsat en la corriente)
                    this.Temperature.SetCalculatedValue(tSat.GetValue(TemperatureUnits.Kelvin), TemperatureUnits.Kelvin, this.Name);
                    this.CalculateBulkProperties(ThermodynamicState.SaturatedLiquid);
                    // 2. REFERENCIA UI
                    PrepareAndCalculatePhase(Vapor, tSat, pSys);
                    break;

                case ThermodynamicState.SuperheatedVapor:
                    // 1. FÍSICA DIRECTA
                    this.CalculateBulkProperties(ThermodynamicState.SuperheatedVapor);

                    // 2. INYECCIÓN DE DENSIDAD REAL DE VAPOR (Ya que requiere Z y EoS)
                    PrepareAndCalculatePhase(Vapor, tSys, pSys);
                    this.MassDensity.SetCalculatedValue(Vapor.MassDensity.Data.GetValue(MassDensity.Data.Unit), MassDensity.Data.Unit, "RealGas_EoS");
                    this.MolarDensity.SetCalculatedValue(Vapor.MolarDensity.Data.GetValue(MolarDensity.Data.Unit), MolarDensity.Data.Unit, "RealGas_EoS");

                    // 3. REFERENCIA UI (Líquido fantasma a Tsat)
                    PrepareAndCalculatePhase(Liquid, tSat, pSys);
                    break;

                case ThermodynamicState.SaturatedVapor:
                    // 1. FÍSICA DIRECTA (Forzando Tsat)
                    this.Temperature.SetCalculatedValue(tSat.GetValue(TemperatureUnits.Kelvin), TemperatureUnits.Kelvin, this.Name);
                    this.CalculateBulkProperties(ThermodynamicState.SaturatedVapor);

                    // 2. INYECCIÓN DE DENSIDAD REAL
                    PrepareAndCalculatePhase(Vapor, tSat, pSys);
                    this.MassDensity.SetCalculatedValue(Vapor.MassDensity.Data.GetValue(MassDensity.Data.Unit), MassDensity.Data.Unit, "RealGas_EoS");
                    this.MolarDensity.SetCalculatedValue(Vapor.MolarDensity.Data.GetValue(MolarDensity.Data.Unit), MolarDensity.Data.Unit, "RealGas_EoS");

                    // 3. REFERENCIA UI
                    PrepareAndCalculatePhase(Liquid, tSat, pSys);
                    break;

                case ThermodynamicState.VaporLiquidMixture:
                    // MEZCLA REAL: Se requieren ambas fases completas a Tsat
                    PrepareAndCalculatePhase(Liquid, tSat, pSys);
                    PrepareAndCalculatePhase(Vapor, tSat, pSys);
                    CalculateTwoPhaseMixtureProperties(); // Regla de la Palanca
                    break;

                default:
                    ResetIntensiveProperties();
                    break;
            }
        }

        private void PrepareAndCalculatePhase<TPhase>(PhaseMixtureBase<TPhase> phase, Amount temp, Amount pres) where TPhase : PhaseComponent
        {
            if (phase is null) return;

            phase.Temperature.SetCalculatedValue(temp.GetValue(phase.Temperature.Data.Unit), phase.Temperature.Data.Unit, this.Name);
            phase.Pressure.SetCalculatedValue(pres.GetValue(phase.Pressure.Data.Unit), phase.Pressure.Data.Unit, this.Name);

            phase.CalculateTP(temp, pres);
            phase.CalculateBulkProperties();
        }

        private void CalculateTwoPhaseMixtureProperties()
        {
            if (Liquid is null || Vapor is null) { ResetIntensiveProperties(); return; }

            double vaporFractionMolar = VaporFraction.Data;

            double mwLiquid = Liquid.Components.Sum(c => c.MoleFraction * c.BaseProperties.MolecularWeight);
            double mwVapor = Vapor.Components.Sum(c => c.MoleFraction * c.BaseProperties.MolecularWeight);
            double mwMixture = (1.0 - vaporFractionMolar) * mwLiquid + vaporFractionMolar * mwVapor;

            double vaporFractionMass = (mwMixture > 0) ? (vaporFractionMolar * mwVapor) / mwMixture : 0.0;

            // Propiedades Térmicas
            double hMolar = (1.0 - vaporFractionMolar) * Liquid.MolarEnthalpy.Data.Value + vaporFractionMolar * Vapor.MolarEnthalpy.Data.Value;
            double hMass = (1.0 - vaporFractionMass) * Liquid.MassEnthalpy.Data.Value + vaporFractionMass * Vapor.MassEnthalpy.Data.Value;
            double cpMass = (1.0 - vaporFractionMass) * Liquid.MassHeatCapacity.Data.Value + vaporFractionMass * Vapor.MassHeatCapacity.Data.Value;

            MolarEnthalpy.SetCalculatedValue(hMolar, MolarEnthalpy.Data.Unit, "TwoPhaseMix");
            MassEnthalpy.SetCalculatedValue(hMass, MassEnthalpy.Data.Unit, "TwoPhaseMix");
            MassHeatCapacity.SetCalculatedValue(cpMass, MassHeatCapacity.Data.Unit, "TwoPhaseMix");

            // Densidad
            double volSpecLiquid = Liquid.MassDensity.Data.Value > 0 ? 1.0 / Liquid.MassDensity.Data.Value : 0.0;
            double volSpecVapor = Vapor.MassDensity.Data.Value > 0 ? 1.0 / Vapor.MassDensity.Data.Value : 0.0;
            double volSpecMixture = (1.0 - vaporFractionMass) * volSpecLiquid + vaporFractionMass * volSpecVapor;

            if (volSpecMixture > 0)
            {
                MassDensity.SetCalculatedValue(1.0 / volSpecMixture, MassDensityUnits.Kg_m3, "TwoPhaseMix");
            }

            // Transporte
            Viscosity.Reset();
            ThermalConductivity.Reset();
            SurfaceTension.SetCalculatedValue(Liquid.SurfaceTension.Data.GetValue(SurfaceTension.Data.Unit), SurfaceTension.Data.Unit, "TwoPhaseMix");
        }

        private void ResetIntensiveProperties()
        {
            MassEnthalpy.Reset();
            MolarEnthalpy.Reset();
            MassHeatCapacity.Reset();
            MassDensity.Reset();
            MolarDensity.Reset();
            Viscosity.Reset();
            ThermalConductivity.Reset();
            SurfaceTension.Reset();
        }
    }

    public class MaterialStream3 : StreamBase<StreamComponent>
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; set; } = "S-100";
        public ThermodynamicState CurrenState { get; set; } = ThermodynamicState.Undefined;
        public ProcessVariableUnitLess VaporFraction { get; set; }

        public LiquidPhase Liquid { get; private set; }
        public VaporPhase Vapor { get; private set; }

        public ProcessMethodProperty ProcessThermoMethod { get; set; } = null!;
        public ProcessCompositionProperty CompositionState { get; private set; }

        public MaterialStream3() : base()
        {
            VaporFraction = new ProcessVariableUnitLess("Vapor Fraction", true);

            Liquid = new LiquidPhase();
            Vapor = new VaporPhase();

            ProcessThermoMethod = new ProcessMethodProperty("Thermo Method", true);
            CompositionState = new ProcessCompositionProperty(this.Components, true);
        }

        public void SetMethod(ThermodynamicMethodFullDto methodFull)
        {
            ProcessThermoMethod.SetUserValue(methodFull);
            ApplyMethodToPhases(methodFull);
        }

        public void SetCalculatedMethod(ThermodynamicMethodFullDto methodFull, string ownerId)
        {
            ProcessThermoMethod.SetCalculatedValue(methodFull, ownerId);
            ApplyMethodToPhases(methodFull);
        }

        public void ResetMethod()
        {
            ProcessThermoMethod.Reset();
            CompositionState.Clear();
            Liquid.Components.Clear();
            Vapor.Components.Clear();
        }

        private void ApplyMethodToPhases(ThermodynamicMethodFullDto methodFull)
        {
            var streamComps = new List<StreamComponent>();
            foreach (var mc in methodFull.Components)
            {
                streamComps.Add(new StreamComponent(mc.FullData));
            }

            CompositionState.InitializeBaseStructure(streamComps);
            Liquid.SetMethod(methodFull);
            Vapor.SetMethod(methodFull);
        }

        private bool _equilibriumDirty = false;
        private bool _flowsDirty = false;

        public int ActiveComponentsCount { get; private set; }

        public void OnVariableChanged(string variableName)
        {
            switch (variableName)
            {
                case "Temperature":
                case "Pressure":
                case "VaporFraction":
                case "Composition":
                case "ThermoMethod":
                    _equilibriumDirty = true;
                    _flowsDirty = true;
                    break;

                case "MassFlow":
                case "MolarFlow":
                case "VolumetricFlow":
                    _flowsDirty = true;
                    break;

                default:
                    break;
            }
        }

        private bool ValidateCompositions()
        {
            if (CompositionState.State == VariableState.Empty || Components.Count == 0)
                return false;

            double sumMoles = 0.0;
            ActiveComponentsCount = 0;

            foreach (var comp in Components)
            {
                sumMoles += comp.MoleFraction;

                if (comp.MoleFraction > 0)
                {
                    ActiveComponentsCount++;
                }
            }

            if (Math.Abs(1.0 - sumMoles) > 1e-4)
            {
                ActiveComponentsCount = 0;
                return false;
            }

            return true;
        }

        public void Solve()
        {
            if (_equilibriumDirty)
            {
                if (!ValidateCompositions())
                {
                    ResetCalculatedProperties();
                    ResetIntensiveProperties();
                    return;
                }

                if (ValidatePhaseRule())
                {
                    // 1. Ejecutar el equilibrio (Esto llena CurrenState y VaporFraction)
                    FlashEquilibrium();

                    // ✅ 2. ¡CRÍTICO Y CENTRALIZADO! Calcular propiedades intensivas reales y de referencia
                    CalculateStreamProperties();
                }

                _equilibriumDirty = false;
                _flowsDirty = true;
            }

            if (_flowsDirty)
            {
                if (ValidateCompositions())
                {
                    CalculateFlowConsistency();
                }
                _flowsDirty = false;
            }
        }

        private void ResetCalculatedProperties()
        {
            if (MassFlow.State == VariableState.CalculatedBy) MassFlow.Reset();
            if (MolarFlow.State == VariableState.CalculatedBy) MolarFlow.Reset();
            if (VolumetricFlow.State == VariableState.CalculatedBy) VolumetricFlow.Reset();

            foreach (var comp in Components)
            {
                comp.MassFlow.SetValue(0, comp.MassFlow.Unit);
                comp.MolarFlow.SetValue(0, comp.MolarFlow.Unit);
            }
        }

        private bool ValidatePhaseRule()
        {
            int ncomp = 0;
            int grados = 0;
            int fases = 0;
            int resultado;

            int totalComponents = Components.Count;
            ncomp = ActiveComponentsCount;

            if (VaporFraction.State == VariableState.UserDefined)
            {
                fases = 1;
            }

            if ((Temperature.State == VariableState.UserDefined || Temperature.State == VariableState.CalculatedBy) &&
                 Temperature.OwnerId != this.Name)
            {
                grados++;
            }

            if ((Pressure.State == VariableState.UserDefined || Pressure.State == VariableState.CalculatedBy) &&
                 Pressure.OwnerId != this.Name)
            {
                grados++;
            }

            resultado = 2 - fases + totalComponents - ncomp - grados;

            if (resultado <= 0)
            {
                return true;
            }

            return false;
        }

        private void CalculateFlowConsistency()
        {
            double avgMw = Components.Sum(c => c.MoleFraction * c.BaseProperties.MolecularWeight);
            if (avgMw <= 0) return;

            if (MassFlow.State == VariableState.UserDefined)
            {
                double molarValue = MassFlow.Data.Value / avgMw;
                MolarFlow.SetCalculatedValue(molarValue, MolarFlow.Data.Unit, "Solver_MassBalance");
            }
            else if (MolarFlow.State == VariableState.UserDefined)
            {
                double massValue = MolarFlow.Data.Value * avgMw;
                MassFlow.SetCalculatedValue(massValue, MassFlow.Data.Unit, "Solver_MassBalance");
            }

            double totalMass = MassFlow.State != VariableState.Empty ? MassFlow.Data.Value : 0.0;
            double totalMolar = MolarFlow.State != VariableState.Empty ? MolarFlow.Data.Value : 0.0;

            foreach (var comp in Components)
            {
                comp.MassFlow.SetValue(totalMass * comp.MassFraction, comp.MassFlow.Unit);
                comp.MolarFlow.SetValue(totalMolar * comp.MoleFraction, comp.MolarFlow.Unit);
            }
        }

        private void FlashEquilibrium()
        {
            bool hasT = Temperature.State == VariableState.UserDefined ||
                       (Temperature.State == VariableState.CalculatedBy && Temperature.OwnerId != this.Name);

            bool hasP = Pressure.State == VariableState.UserDefined ||
                       (Pressure.State == VariableState.CalculatedBy && Pressure.OwnerId != this.Name);

            bool hasVF = VaporFraction.State == VariableState.UserDefined ||
                         (VaporFraction.State == VariableState.CalculatedBy && VaporFraction.OwnerId != this.Name);

            if (hasT && hasP)
            {
                CalculateFlashTP();
            }
            else if (hasP && hasVF)
            {
                CalculateFlashPVF();
            }
            else if (hasT && hasVF)
            {
                CalculateFlashTVF();
            }
        }

        private void CalculateFlashPVF()
        {
            var temperature = CalculateSaturationTemperature(Pressure.Data, VaporFraction.Data);
            Temperature.SetCalculatedValue(temperature.GetValue(Temperature.Data.Unit), Temperature.Data.Unit, this.Name);
        }

        private void CalculateFlashTVF()
        {
            var pressure = CalculateSaturationPressure(Temperature.Data, VaporFraction.Data);
            Pressure.SetCalculatedValue(pressure.GetValue(Pressure.Data.Unit), Pressure.Data.Unit, this.Name);
        }

        private void DefineFlashTProperties()
        {
            int n = Components.Count;
            for (int i = 0; i < n; i++)
            {
                var comp = Components[i];
                var liquidComp = Liquid.Components[i];
                var vaporComp = Vapor.Components[i];

                liquidComp.MoleFraction = comp.MoleFraction;
                liquidComp.MassFraction = comp.MassFraction;
                vaporComp.MoleFraction = comp.MoleFraction;
                vaporComp.MassFraction = comp.MassFraction;

                comp.SaturationTemperature = comp.GetSaturedTemperatureAtPressure(Pressure.Data);
            }
        }

        private void DefineFlashTVFProperties()
        {
            int n = Components.Count;
            for (int i = 0; i < n; i++)
            {
                var comp = Components[i];
                var liquidComp = Liquid.Components[i];
                var vaporComp = Vapor.Components[i];

                liquidComp.MoleFraction = comp.MoleFraction;
                liquidComp.MassFraction = comp.MassFraction;
                vaporComp.MoleFraction = comp.MoleFraction;
                vaporComp.MassFraction = comp.MassFraction;

                comp.SaturationPressure = comp.GetSaturedPressureAtTemperature(Temperature.Data);
            }
        }

        private double CalculateEquilibrium(Amount temperature, Amount pressure, double vaporFraction)
        {
            int n = Components.Count;
            double sumx = 0.0;
            double sumy = 0.0;

            Liquid.CalculateTP(temperature, pressure);
            Vapor.CalculateTP(temperature, pressure);

            for (int i = 0; i < n; i++)
            {
                double z_i = Components[i].MoleFraction;
                double liquidNum = Liquid.Components[i].LiquidFugacityNumerator;
                double vaporDen = Vapor.Components[i].VaporFugacityDenominator;

                double K_i = (vaporDen > 0) ? liquidNum / vaporDen : 0.0;
                double denominator = 1.0 + vaporFraction * (K_i - 1.0);

                double xliq = (denominator != 0) ? z_i / denominator : z_i;
                double yvap = K_i * xliq;

                sumy += yvap;
                sumx += xliq;

                Liquid.Components[i].MoleFraction = xliq;
                Vapor.Components[i].MoleFraction = yvap;
            }

            if (sumx > 0 || sumy > 0)
            {
                for (int i = 0; i < n; i++)
                {
                    if (sumx > 0) Liquid.Components[i].MoleFraction /= sumx;
                    if (sumy > 0) Vapor.Components[i].MoleFraction /= sumy;
                }
            }

            return sumy - sumx;
        }

        public Amount CalculateSaturationPressure(Amount temperature, double vaporFraction = 1.0)
        {
            if (Components.Count == 1 &&
                (Components[0].BaseProperties.Name == "Agua" ||
                 Components[0].BaseProperties.Name == "Water"))
            {
                SaturationPressure = Components[0].SaturationPressure;
                return SaturationPressure;
            }

            var (pGuess, minPsat, maxPsat) = CalculateSmartPressureGuess(temperature, vaporFraction);

            Amount pressureCal = new Amount(pGuess, PressureUnits.Bar);
            if (VaporFraction.Data == 0)
                CurrenState = ThermodynamicState.SaturatedLiquid;
            else if (VaporFraction.Data == 1)
                CurrenState = ThermodynamicState.SaturatedVapor;
            else
                CurrenState = ThermodynamicState.VaporLiquidMixture;

            var result = SecantSolver.Solve(
                func: (p) =>
                {
                    pressureCal.SetValue(p, PressureUnits.Bar);
                    return CalculateEquilibrium(temperature, pressureCal, vaporFraction);
                },
                x1: minPsat,
                x2: maxPsat,
                guess: pGuess
            );

            SaturationPressure.SetValue(result.Value, PressureUnits.Bar);
            return SaturationPressure;
        }

        private (double pGuess, double minPsat, double maxPsat) CalculateSmartPressureGuess(Amount temperature, double vaporFraction)
        {
            DefineFlashTVFProperties();

            int n = Components.Count;
            double minPsat = double.MaxValue;
            double maxPsat = double.MinValue;
            double pBubble = 0.0;
            double sumInverseDew = 0.0;

            for (int i = 0; i < n; i++)
            {
                double psat = Components[i].SaturationPressure.GetValue(PressureUnits.Bar);
                double xi = Components[i].MoleFraction;

                if (psat < minPsat) minPsat = psat;
                if (psat > maxPsat) maxPsat = psat;

                pBubble += xi * psat;
                if (psat > 0) sumInverseDew += xi / psat;
            }

            double pDew = (sumInverseDew > 0) ? 1.0 / sumInverseDew : maxPsat;
            double pGuess;

            if (vaporFraction == 0.0) pGuess = pBubble;
            else if (vaporFraction == 1.0) pGuess = pDew;
            else pGuess = pBubble + vaporFraction * (pDew - pBubble);

            pGuess = Math.Clamp(pGuess, minPsat, maxPsat);
            return (pGuess, minPsat, maxPsat);
        }

        private (double tGuess, double minTsat, double maxTsat) CalculateSmartTemperatureGuess(
            Amount pressure,
            double vaporFraction)
        {
            DefineFlashTProperties();

            int n = Components.Count;
            double minTsat = double.MaxValue;
            double maxTsat = double.MinValue;
            double tBubble = 0.0;

            for (int i = 0; i < n; i++)
            {
                double tsat = Components[i].SaturationTemperature.GetValue(TemperatureUnits.Kelvin);
                double xi = Components[i].MoleFraction;

                if (tsat < minTsat) minTsat = tsat;
                if (tsat > maxTsat) maxTsat = tsat;

                tBubble += xi * tsat;
            }

            double tDew = minTsat + 0.7 * (maxTsat - minTsat);
            double tGuess;

            if (vaporFraction == 0.0)
            {
                tGuess = tBubble;
            }
            else if (vaporFraction == 1.0)
            {
                tGuess = tDew;
            }
            else
            {
                tGuess = tBubble + vaporFraction * (tDew - tBubble);
            }

            tGuess = Math.Clamp(tGuess, minTsat, maxTsat);

            return (tGuess, minTsat, maxTsat);
        }

        public Amount CalculateSaturationTemperature(Amount pressure, double vaporFraction)
        {
            if (Components.Count == 1 &&
                (Components[0].BaseProperties.Name == "Agua" ||
                 Components[0].BaseProperties.Name == "Water"))
            {
                SaturationTemperature = Components[0].GetSaturedTemperatureAtPressure(pressure);
                return SaturationTemperature;
            }

            var (tGuess, minTsat, maxTsat) = CalculateSmartTemperatureGuess(pressure, vaporFraction);

            Amount temperatureCal = new Amount(tGuess, TemperatureUnits.Kelvin);
            if (VaporFraction.Data == 0)
                CurrenState = ThermodynamicState.SaturatedLiquid;
            else if (VaporFraction.Data == 1)
                CurrenState = ThermodynamicState.SaturatedVapor;
            else
                CurrenState = ThermodynamicState.VaporLiquidMixture;

            var result = BisectionSolver.Solve(
                func: (t) =>
                {
                    temperatureCal.SetValue(t, TemperatureUnits.Kelvin);
                    return CalculateEquilibrium(temperatureCal, pressure, vaporFraction);
                },
                x1: minTsat,
                x2: maxTsat,
                guess: tGuess
            );

            SaturationTemperature.SetValue(result.Value, TemperatureUnits.Kelvin);
            return SaturationTemperature;
        }

        private void CalculateFlashTP()
        {
            var state = DeterminePhaseState();
            CurrenState = state;

            if (state == ThermodynamicState.SubcooledLiquid ||
                state == ThermodynamicState.SaturatedLiquid)
            {
                VaporFraction.SetCalculatedValue(0.0, "FlashTP");

                for (int i = 0; i < Components.Count; i++)
                {
                    Liquid.Components[i].MoleFraction = Components[i].MoleFraction;
                }
                return;
            }

            if (state == ThermodynamicState.SuperheatedVapor ||
                state == ThermodynamicState.SaturatedVapor)
            {
                VaporFraction.SetCalculatedValue(1.0, "FlashTP");
                for (int i = 0; i < Components.Count; i++)
                {
                    Vapor.Components[i].MoleFraction = Components[i].MoleFraction;
                }

                return;
            }

            if (state == ThermodynamicState.VaporLiquidMixture)
            {
                var vaporFraction = CalculateVaporFraction(Temperature.Data, Pressure.Data);
                VaporFraction.SetCalculatedValue(vaporFraction, "FlashTP");
            }
        }

        private double CalculateVaporFraction(Amount temperature, Amount pressure)
        {
            var (vGuess, minV, maxV) = CalculateSmartVaporGuess(temperature, pressure);

            Amount temperatureCal = new Amount(temperature.GetValue(TemperatureUnits.Kelvin), TemperatureUnits.Kelvin);
            Amount pressureCal = new Amount(pressure.GetValue(PressureUnits.Bar), PressureUnits.Bar);

            var result = BisectionSolver.Solve(
                func: (v) =>
                {
                    return CalculateEquilibrium(temperatureCal, pressureCal, v);
                },
                x1: minV,
                x2: maxV,
                guess: vGuess
            );

            return result.Value;
        }

        private (double vGuess, double minV, double maxV) CalculateSmartVaporGuess(Amount temperature, Amount pressure)
        {
            double minV = 0.0;
            double maxV = 1.0;

            double P_bubble = CalculateSaturationPressure(temperature, 0).GetValue(PressureUnits.Bar);
            double P_dew = CalculateSaturationPressure(temperature, 1).GetValue(PressureUnits.Bar);
            double P_actual = pressure.GetValue(PressureUnits.Bar);

            double vGuess;
            double denominator = P_bubble - P_dew;

            if (denominator > 1e-6)
            {
                vGuess = (P_bubble - P_actual) / denominator;
                vGuess = Math.Clamp(vGuess, minV, maxV);
            }
            else
            {
                vGuess = 0.5;
            }

            return (vGuess, minV, maxV);
        }

        public ThermodynamicState DeterminePhaseState()
        {
            double T_actual = Temperature.Data.GetValue(TemperatureUnits.Kelvin);
            double P_actual = Pressure.Data.GetValue(PressureUnits.Bar);

            double T_bubble = 0.0;
            double T_dew = 0.0;
            double P_bubble = 0.0;
            double P_dew = 0.0;

            if (Temperature.State == VariableState.UserDefined)
            {
                P_bubble = CalculateSaturationPressure(Temperature.Data, 0).GetValue(PressureUnits.Bar);
                P_dew = CalculateSaturationPressure(Temperature.Data, 1).GetValue(PressureUnits.Bar);

                Liquid.SaturationPressure = new Amount(P_bubble, PressureUnits.Bar);
                Vapor.SaturationPressure = new Amount(P_dew, PressureUnits.Bar);
            }

            if (Pressure.State == VariableState.UserDefined)
            {
                T_bubble = CalculateSaturationTemperature(Pressure.Data, 0).GetValue(TemperatureUnits.Kelvin);
                T_dew = CalculateSaturationTemperature(Pressure.Data, 1).GetValue(TemperatureUnits.Kelvin);

                Liquid.SaturationTemperature = new Amount(T_bubble, TemperatureUnits.Kelvin);
                Vapor.SaturationTemperature = new Amount(T_dew, TemperatureUnits.Kelvin);
            }

            const double TOL_T = 0.1;
            const double TOL_P = 0.01;

            if (Temperature.State == VariableState.UserDefined)
            {
                if (Math.Abs(P_actual - P_bubble) < TOL_P)
                    return ThermodynamicState.SaturatedLiquid;

                if (Math.Abs(P_actual - P_dew) < TOL_P)
                    return ThermodynamicState.SaturatedVapor;
            }

            if (Pressure.State == VariableState.UserDefined)
            {
                if (Math.Abs(T_actual - T_bubble) < TOL_T)
                    return ThermodynamicState.SaturatedLiquid;

                if (Math.Abs(T_actual - T_dew) < TOL_T)
                    return ThermodynamicState.SaturatedVapor;
            }

            bool inMixtureT = (T_dew < T_actual && T_actual < T_bubble);
            bool inMixtureP = (P_dew < P_actual && P_actual < P_bubble);

            if (inMixtureT || inMixtureP)
                return ThermodynamicState.VaporLiquidMixture;

            if ((Pressure.State == VariableState.UserDefined && T_actual < T_bubble - TOL_T) ||
                (Temperature.State == VariableState.UserDefined && P_actual > P_bubble + TOL_P))
            {
                return ThermodynamicState.SubcooledLiquid;
            }

            if ((Pressure.State == VariableState.UserDefined && T_actual > T_dew + TOL_T) ||
                (Temperature.State == VariableState.UserDefined && P_actual < P_dew - TOL_P))
            {
                return ThermodynamicState.SuperheatedVapor;
            }

            return ThermodynamicState.Undefined;
        }

        // =========================================================================
        // ORQUESTADOR CENTRAL DE PROPIEDADES (Física Real y Referencia UI)
        // =========================================================================
        public void CalculateStreamProperties()
        {
            if (CurrenState == ThermodynamicState.Undefined)
                return;

            // Anclas Termodinámicas
            Amount tSys = Temperature.Data;
            Amount pSys = Pressure.Data;
            Amount tSat = SaturationTemperature;

            switch (CurrenState)
            {
                case ThermodynamicState.SubcooledLiquid:
                    PrepareAndCalculatePhase(Liquid, tSys, pSys); // Fase Real
                    PrepareAndCalculatePhase(Vapor, tSys, pSys);  // UI Referencia
                    CopyPropertiesFromSinglePhase(Liquid);
                    break;

                case ThermodynamicState.SaturatedLiquid:
                    PrepareAndCalculatePhase(Liquid, tSat, pSys); // Fase Real
                    PrepareAndCalculatePhase(Vapor, tSat, pSys);  // Fase Real
                    CopyPropertiesFromSinglePhase(Liquid);
                    break;

                case ThermodynamicState.VaporLiquidMixture:
                    PrepareAndCalculatePhase(Liquid, tSat, pSys); // Fase Real
                    PrepareAndCalculatePhase(Vapor, tSat, pSys);  // Fase Real
                    CalculateTwoPhaseMixtureProperties();
                    break;

                case ThermodynamicState.SaturatedVapor:
                    PrepareAndCalculatePhase(Liquid, tSat, pSys); // Fase Real
                    PrepareAndCalculatePhase(Vapor, tSat, pSys);  // Fase Real
                    CopyPropertiesFromSinglePhase(Vapor);
                    break;

                case ThermodynamicState.SuperheatedVapor:
                    PrepareAndCalculatePhase(Vapor, tSys, pSys);  // Fase Real
                    PrepareAndCalculatePhase(Liquid, tSat, pSys); // UI Referencia
                    CopyPropertiesFromSinglePhase(Vapor);
                    break;

                default:
                    ResetIntensiveProperties();
                    break;
            }
        }

        // =========================================================================
        // AYUDANTE MAESTRO DE FASE (Configura T/P y dispara cálculo)
        // =========================================================================
        private void PrepareAndCalculatePhase<TPhase>(PhaseMixtureBase<TPhase> phase, Amount temp, Amount pres) where TPhase : PhaseComponent
        {
            if (phase is null) return;

            phase.Temperature.SetCalculatedValue(temp.GetValue(phase.Temperature.Data.Unit), phase.Temperature.Data.Unit, this.Name);
            phase.Pressure.SetCalculatedValue(pres.GetValue(phase.Pressure.Data.Unit), phase.Pressure.Data.Unit, this.Name);

            phase.CalculateTP(temp, pres);
            phase.CalculateBulkProperties();
        }

        // =========================================================================
        // REGLAS PARA MEZCLA LÍQUIDO-VAPOR (Bubble-Up)
        // =========================================================================
        private void CalculateTwoPhaseMixtureProperties()
        {
            if (Liquid is null || Vapor is null)
            {
                ResetIntensiveProperties();
                return;
            }

            double vaporFractionMolar = VaporFraction.Data;

            double mwLiquid = CalculatePhaseMolecularWeight(Liquid.Components);
            double mwVapor = CalculatePhaseMolecularWeight(Vapor.Components);
            double mwMixture = CalculateMixtureMolecularWeight(mwLiquid, mwVapor, vaporFractionMolar);

            double vaporFractionMass = CalculateMassVaporFraction(vaporFractionMolar, mwLiquid, mwVapor, mwMixture);

            CalculateMixtureEnthalpyAndHeatCapacity(vaporFractionMolar, vaporFractionMass);
            CalculateMixtureDensity(vaporFractionMass);
            CalculateMixtureTransportProperties(vaporFractionMass);
        }

        private double CalculatePhaseMolecularWeight(IEnumerable<PhaseComponent> components)
        {
            if (components is null) return 0.0;
            return components.Sum(c => c.MoleFraction * c.BaseProperties.MolecularWeight);
        }

        private double CalculateMixtureMolecularWeight(double mwLiquid, double mwVapor, double vaporFractionMolar)
        {
            return (1.0 - vaporFractionMolar) * mwLiquid + vaporFractionMolar * mwVapor;
        }

        private double CalculateMassVaporFraction(double vaporFractionMolar, double mwLiquid, double mwVapor, double mwMixture)
        {
            if (mwMixture <= ThermodynamicConstants.MinPositiveValue) return 0.0;
            return (vaporFractionMolar * mwVapor) / mwMixture;
        }

        private void CalculateMixtureEnthalpyAndHeatCapacity(double vaporFractionMolar, double vaporFractionMass)
        {
            double hMolar = (1.0 - vaporFractionMolar) * Liquid.MolarEnthalpy.Data.Value + vaporFractionMolar * Vapor.MolarEnthalpy.Data.Value;
            double hMass = (1.0 - vaporFractionMass) * Liquid.MassEnthalpy.Data.Value + vaporFractionMass * Vapor.MassEnthalpy.Data.Value;
            double cpMass = (1.0 - vaporFractionMass) * Liquid.MassHeatCapacity.Data.Value + vaporFractionMass * Vapor.MassHeatCapacity.Data.Value;

            MolarEnthalpy.SetCalculatedValue(hMolar, MolarEnthalpy.Data.Unit, "TwoPhaseMix");
            MassEnthalpy.SetCalculatedValue(hMass, MassEnthalpy.Data.Unit, "TwoPhaseMix");
            MassHeatCapacity.SetCalculatedValue(cpMass, MassHeatCapacity.Data.Unit, "TwoPhaseMix");
        }

        private void CalculateMixtureDensity(double vaporFractionMass)
        {
            double volSpecLiquid = CalculateSpecificVolume(Liquid.MassDensity.Data.Value);
            double volSpecVapor = CalculateSpecificVolume(Vapor.MassDensity.Data.Value);

            double volSpecMixture = (1.0 - vaporFractionMass) * volSpecLiquid + vaporFractionMass * volSpecVapor;
            double massDensityMixture = CalculateDensityFromSpecificVolume(volSpecMixture);

            MassDensity.SetCalculatedValue(massDensityMixture, MassDensityUnits.Kg_m3, "TwoPhaseMix");
        }

        private void CalculateMixtureTransportProperties(double vaporFractionMass)
        {
            Viscosity.Reset();
            ThermalConductivity.Reset();

            SurfaceTension.SetCalculatedValue(Liquid.SurfaceTension.Data.Value, SurfaceTension.Data.Unit, "TwoPhaseMix");
        }

        // =========================================================================
        // COPIA DE PROPIEDADES PARA FASES PURAS (Líquido o Vapor)
        // =========================================================================
        private void CopyPropertiesFromSinglePhase<T>(PhaseMixtureBase<T> phase) where T : PhaseComponent
        {
            if (phase is null)
            {
                ResetIntensiveProperties();
                return;
            }

            MassEnthalpy.SetCalculatedValue(phase.MassEnthalpy.Data.GetValue(MassEnthalpy.Data.Unit), MassEnthalpy.Data.Unit, "SinglePhase");
            MolarEnthalpy.SetCalculatedValue(phase.MolarEnthalpy.Data.GetValue(MolarEnthalpy.Data.Unit), MolarEnthalpy.Data.Unit, "SinglePhase");
            MassHeatCapacity.SetCalculatedValue(phase.MassHeatCapacity.Data.GetValue(MassHeatCapacity.Data.Unit), MassHeatCapacity.Data.Unit, "SinglePhase");

            MassDensity.SetCalculatedValue(phase.MassDensity.Data.GetValue(MassDensity.Data.Unit), MassDensity.Data.Unit, "SinglePhase");
            MolarDensity.SetCalculatedValue(phase.MolarDensity.Data.GetValue(MolarDensity.Data.Unit), MolarDensity.Data.Unit, "SinglePhase");

            Viscosity.SetCalculatedValue(phase.Viscosity.Data.GetValue(Viscosity.Data.Unit), Viscosity.Data.Unit, "SinglePhase");
            ThermalConductivity.SetCalculatedValue(phase.ThermalConductivity.Data.GetValue(ThermalConductivity.Data.Unit), ThermalConductivity.Data.Unit, "SinglePhase");

            if (phase is LiquidPhase liquidPhase)
            {
                SurfaceTension.SetCalculatedValue(liquidPhase.SurfaceTension.Data.GetValue(SurfaceTension.Data.Unit), SurfaceTension.Data.Unit, "SinglePhase");
            }
            else
            {
                SurfaceTension.Reset();
            }
        }

        private double CalculateSpecificVolume(double massDensity)
        {
            if (massDensity <= ThermodynamicConstants.MinPositiveValue) return 0.0;
            return 1.0 / massDensity;
        }

        private double CalculateDensityFromSpecificVolume(double specificVolume)
        {
            if (specificVolume <= ThermodynamicConstants.MinPositiveValue) return 0.0;
            return 1.0 / specificVolume;
        }

        private void ResetIntensiveProperties()
        {
            MassEnthalpy.Reset();
            MolarEnthalpy.Reset();
            MassHeatCapacity.Reset();
            MassDensity.Reset();
            MolarDensity.Reset();
            Viscosity.Reset();
            ThermalConductivity.Reset();
            SurfaceTension.Reset();
        }
    }
    public class MaterialStream2 : StreamBase<StreamComponent>
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; set; } = "S-100";
        public ThermodynamicState CurrenState { get; set; } = ThermodynamicState.Undefined;
        public ProcessVariableUnitLess VaporFraction { get; set; }

        public LiquidPhase Liquid { get; private set; }
        public VaporPhase Vapor { get; private set; }

        // Wrapper para controlar el estado del Método Termodinámico
        public ProcessMethodProperty ProcessThermoMethod { get; set; } = null!;

        // Wrapper EXCLUSIVO de MaterialStream para controlar el estado de la Composición
        public ProcessCompositionProperty CompositionState { get; private set; }

        public MaterialStream2() : base()
        {
            VaporFraction = new ProcessVariableUnitLess("Vapor Fraction", true);

            Liquid = new LiquidPhase();
            Vapor = new VaporPhase();

            ProcessThermoMethod = new ProcessMethodProperty("Thermo Method", true);

            // Le entregamos la referencia de la lista heredada (this.Components) al administrador de estado.
            // Así evitamos la dualidad de listas en memoria.
            CompositionState = new ProcessCompositionProperty(this.Components, true);
            // Inicialización de Propiedades Críticas (usando unidades base estándar para cálculos)

        }

        /// <summary>
        /// Llamado por la UI cuando el usuario selecciona el método manualmente.
        /// </summary>
        public void SetMethod(ThermodynamicMethodFullDto methodFull)
        {
            // 1. Estado topológico: Se inyecta el DTO completo directamente al wrapper como "UserDefined"
            ProcessThermoMethod.SetUserValue(methodFull);

            // 2. Propagar el ADN a las listas y fases
            ApplyMethodToPhases(methodFull);
        }

        /// <summary>
        /// Llamado por el Solver cuando la corriente hereda el paquete termodinámico de un equipo aguas arriba.
        /// </summary>
        public void SetCalculatedMethod(ThermodynamicMethodFullDto methodFull, string ownerId)
        {
            // 1. Estado topológico: Indicamos quién calculó/heredó este método ("CalculatedBy")
            ProcessThermoMethod.SetCalculatedValue(methodFull, ownerId);

            // 2. Propagar el ADN a las listas y fases
            ApplyMethodToPhases(methodFull);
        }

        /// <summary>
        /// Desconecta el método y limpia toda la "genética" de la corriente y sus fases.
        /// </summary>
        public void ResetMethod()
        {
            ProcessThermoMethod.Reset();
            CompositionState.Clear(); // El wrapper limpia la lista base y reinicia su estado
            Liquid.Components.Clear();
            Vapor.Components.Clear();
        }

        /// <summary>
        /// Centraliza la lógica de poblar la lista global y propagar la cascada a las fases internas.
        /// </summary>
        private void ApplyMethodToPhases(ThermodynamicMethodFullDto methodFull)
        {
            // 1. Preparamos la estructura global de la corriente (fracciones z_i)
            var streamComps = new List<StreamComponent>();
            foreach (var mc in methodFull.Components)
            {
                // Instanciamos el StreamComponent con el ADN químico puro (mc.FullData)
                streamComps.Add(new StreamComponent(mc.FullData));
            }

            // 2. El wrapper inyecta la estructura base a this.Components y pone el estado en "Empty"
            CompositionState.InitializeBaseStructure(streamComps);

            // 3. CASCADA a las fases (Ellas reciben el DTO y arman sus propias listas de LiquidComponent/VaporComponent)
            Liquid.SetMethod(methodFull);
            Vapor.SetMethod(methodFull);
        }
        private bool _equilibriumDirty = false;
        private bool _flowsDirty = false;

        // Este método se dispara desde la UI (HandleInput)
        // Propiedad para guardar el número de componentes reales (útil para la Regla de Fases)
        public int ActiveComponentsCount { get; private set; }

        /// <summary>
        /// Se llama desde la UI cada vez que el usuario modifica una variable.
        /// Levanta las banderas (dirty flags) correspondientes para que el Solver sepa qué recalcular.
        /// </summary>
        public void OnVariableChanged(string variableName)
        {
            switch (variableName)
            {
                // --- VARIABLES TERMODINÁMICAS ---
                // Si cambia algo de esto, cambia el equilibrio completo
                case "Temperature":
                case "Pressure":
                case "VaporFraction":
                case "Composition":
                case "ThermoMethod":
                    _equilibriumDirty = true;
                    _flowsDirty = true; // El equilibrio cambia densidades y MW, ensuciando también los flujos
                    break;

                // --- VARIABLES DE FLUJO ---
                // Si solo cambia el flujo, el equilibrio (Flash) se mantiene intacto, 
                // solo recalculamos el balance de masa/moles
                case "MassFlow":
                case "MolarFlow":
                case "VolumetricFlow":
                    _flowsDirty = true;
                    break;

                // --- OTRAS VARIABLES ---
                default:
                    // Si en el futuro agregamos cálculos inversos (ej. Flash por Entalpía), lo manejaremos aquí.
                    break;
            }
        }
        private bool ValidateCompositions()
        {
            if (CompositionState.State == VariableState.Empty || Components.Count == 0)
                return false;

            double sumMoles = 0.0;
            ActiveComponentsCount = 0;

            foreach (var comp in Components)
            {
                sumMoles += comp.MoleFraction;

                // Contabilizamos el componente solo si realmente está en la mezcla
                if (comp.MoleFraction > 0)
                {
                    ActiveComponentsCount++;
                }
            }

            // Verificamos si la suma es 1.0 con una tolerancia de 0.0001
            if (Math.Abs(1.0 - sumMoles) > 1e-4)
            {
                ActiveComponentsCount = 0;
                return false;
            }

            return true;
        }
        /// <summary>
        /// Motor central de la corriente. Evalúa el estado termodinámico y de flujos,
        /// y ejecuta los cálculos en el orden estricto de dependencias.
        /// </summary>
        public void Solve()
        {
            // ==========================================
            // BLOQUE 1: Equilibrio Termodinámico
            // ==========================================
            if (_equilibriumDirty)
            {
                if (!ValidateCompositions())
                {
                    ResetCalculatedProperties();
                    return;
                }

                if (ValidatePhaseRule())
                {
                    // 1. Ejecutar el equilibrio (Esto llena CurrenState y VaporFraction)
                    FlashEquilibrium();

                    // ✅ 2. ¡CRÍTICO! Una vez tenemos el estado, calculamos las densidades, entalpías, etc.
                    CalculateStreamProperties();
                }

                _equilibriumDirty = false;
                _flowsDirty = true; // Notificamos que los flujos deben revisarse (por cambios en densidad/MW)
            }

            // ==========================================
            // BLOQUE 2: Consistencia de Flujos
            // ==========================================
            if (_flowsDirty)
            {
                if (ValidateCompositions())
                {
                    CalculateFlowConsistency();
                }
                _flowsDirty = false;
            }
        }

        /// <summary>
        /// Limpia las propiedades calculadas si la corriente pierde su estado de validez
        /// (ej. el usuario borra la composición o deja grados de libertad incompletos).
        /// </summary>
        private void ResetCalculatedProperties()
        {
            // Liberamos los flujos globales si fueron calculados por el Solver
            if (MassFlow.State == VariableState.CalculatedBy) MassFlow.Reset();
            if (MolarFlow.State == VariableState.CalculatedBy) MolarFlow.Reset();
            if (VolumetricFlow.State == VariableState.CalculatedBy) VolumetricFlow.Reset();

            // Liberamos las propiedades críticas de la mezcla
            //TempCritical.SetValue(0, TempCritical.Unit);
            //PressCritical.SetValue(0, PressCritical.Unit);
            //MolarVolCritical.SetValue(0, MolarVolCritical.Unit);

            // Limpiamos los flujos parciales de cada componente
            foreach (var comp in Components)
            {
                // Como son tipo Amount, simplemente los devolvemos a cero
                comp.MassFlow.SetValue(0, comp.MassFlow.Unit);
                comp.MolarFlow.SetValue(0, comp.MolarFlow.Unit);

                // Si ya tienes VolumetricFlow en tu StreamComponent, descomenta esta línea:
                // comp.VolumetricFlow.SetValue(0, comp.VolumetricFlow.Unit); 
            }
        }
        /// <summary>
        /// Verifica si existen los grados de libertad necesarios (al menos 2 variables intensivas externas)
        /// para poder ejecutar el cálculo de equilibrio termodinámico (Flash).
        /// </summary>
        private bool ValidatePhaseRule()
        {
            int ncomp = 0;
            int grados = 0;
            int fases = 0;
            int resultado;

            int totalComponents = Components.Count;

            // 1. Conteo de componentes exacto como en tu C++
            ncomp = ActiveComponentsCount;

            // 2. Fracción de vapor (fases) - En tu C++ solo evalúas si está Definido
            if (VaporFraction.State == VariableState.UserDefined)
            {
                fases = 1;
            }

            // 3. Temperatura (grados) - Definido o Calculado, y que no sea por la misma corriente
            if ((Temperature.State == VariableState.UserDefined || Temperature.State == VariableState.CalculatedBy) &&
                 Temperature.OwnerId != this.Name)
            {
                grados++;
            }

            // 4. Presión (grados) - Definido o Calculado, y que no sea por la misma corriente
            if ((Pressure.State == VariableState.UserDefined || Pressure.State == VariableState.CalculatedBy) &&
                 Pressure.OwnerId != this.Name)
            {
                grados++;
            }

            // 5. Tu ecuación exacta de C++
            resultado = 2 - fases + totalComponents - ncomp - grados;

            // 6. Retorno
            if (resultado <= 0)
            {
                return true;
            }

            return false;
        }
        // Propiedades Pseudocríticas de la Mezcla

        /// <summary>
        /// Calcula las propiedades pseudocríticas de la mezcla (Volumen, Temperatura y Presión).
        /// Requiere que las fracciones molares estén definidas y sumen 1.
        /// </summary>

        private void CalculateFlowConsistency()
        {
            // 1. Cálculo del Peso Molecular Promedio (MW_mix)
            double avgMw = Components.Sum(c => c.MoleFraction * c.BaseProperties.MolecularWeight);
            if (avgMw <= 0) return;

            // 2. El Sudoku de Flujos Globales
            if (MassFlow.State == VariableState.UserDefined)
            {
                double molarValue = MassFlow.Data.Value / avgMw;
                MolarFlow.SetCalculatedValue(molarValue, MolarFlow.Data.Unit, "Solver_MassBalance");
            }
            else if (MolarFlow.State == VariableState.UserDefined)
            {
                double massValue = MolarFlow.Data.Value * avgMw;
                MassFlow.SetCalculatedValue(massValue, MassFlow.Data.Unit, "Solver_MassBalance");
            }

            // 3. Distribución a cada StreamComponent
            double totalMass = MassFlow.State != VariableState.Empty ? MassFlow.Data.Value : 0.0;
            double totalMolar = MolarFlow.State != VariableState.Empty ? MolarFlow.Data.Value : 0.0;

            foreach (var comp in Components)
            {
                // Usamos SetValue para respetar y convertir las unidades si es necesario
                comp.MassFlow.SetValue(totalMass * comp.MassFraction, comp.MassFlow.Unit);
                comp.MolarFlow.SetValue(totalMolar * comp.MoleFraction, comp.MolarFlow.Unit);
            }
        }
        /// <summary>
        /// Enruta el cálculo de equilibrio termodinámico (Flash) dependiendo de los 
        /// grados de libertad disponibles (T-P, P-VF, o T-VF).
        /// </summary>
        private void FlashEquilibrium()
        {
            // 1. Identificamos qué variables están definidas (por el usuario o heredadas válidamente)
            bool hasT = Temperature.State == VariableState.UserDefined ||
                       (Temperature.State == VariableState.CalculatedBy && Temperature.OwnerId != this.Name);

            bool hasP = Pressure.State == VariableState.UserDefined ||
                       (Pressure.State == VariableState.CalculatedBy && Pressure.OwnerId != this.Name);

            bool hasVF = VaporFraction.State == VariableState.UserDefined ||
                        (VaporFraction.State == VariableState.CalculatedBy && VaporFraction.OwnerId != this.Name);

            // 2. Enrutamiento hacia los métodos de cálculo específicos
            if (hasT && hasP)
            {
                CalculateFlashTP();
            }
            else if (hasP && hasVF)
            {
                CalculateFlashPVF();

            }
            else if (hasT && hasVF)
            {
                CalculateFlashTVF();
            }
        }

        // =========================================================
        // MÉTODOS DE CÁLCULO INDIVIDUALES (Esperando tu lógica)
        // =========================================================



        private void CalculateFlashPVF()
        {
            var temperature = CalculateSaturationTemperature(Pressure.Data, VaporFraction.Data);

            Temperature.SetCalculatedValue(temperature.GetValue(Temperature.Data.Unit), Temperature.Data.Unit, this.Name);
        }

        private void CalculateFlashTVF()
        {
            var pressure = CalculateSaturationPressure(Temperature.Data, VaporFraction.Data);


            Pressure.SetCalculatedValue(pressure.GetValue(Pressure.Data.Unit), Pressure.Data.Unit, this.Name);

        }
        private void DefineFlashTProperties()
        {
            int n = Components.Count;
            for (int i = 0; i < n; i++)
            {
                var comp = Components[i];

                // ✅ ACCESO O(1): Como las listas son paralelas, usamos el índice 'i'
                var liquidComp = Liquid.Components[i];
                var vaporComp = Vapor.Components[i];

                liquidComp.MoleFraction = comp.MoleFraction;
                liquidComp.MassFraction = comp.MassFraction;
                vaporComp.MoleFraction = comp.MoleFraction;
                vaporComp.MassFraction = comp.MassFraction;

                comp.SaturationTemperature = comp.GetSaturedTemperatureAtPressure(Pressure.Data);
            }
        }

        private void DefineFlashTVFProperties()
        {
            int n = Components.Count;
            for (int i = 0; i < n; i++)
            {
                var comp = Components[i];

                // ✅ ACCESO O(1)
                var liquidComp = Liquid.Components[i];
                var vaporComp = Vapor.Components[i];

                liquidComp.MoleFraction = comp.MoleFraction;
                liquidComp.MassFraction = comp.MassFraction;
                vaporComp.MoleFraction = comp.MoleFraction;
                vaporComp.MassFraction = comp.MassFraction;

                comp.SaturationPressure = comp.GetSaturedPressureAtTemperature(Temperature.Data);
            }
        }
       
        
        private double CalculateEquilibrium(Amount temperature, Amount pressure, double vaporFraction)
        {
            int n = Components.Count;
            double sumx = 0.0;
            double sumy = 0.0;

            Liquid.CalculateTP(temperature, pressure);
            Vapor.CalculateTP(temperature, pressure);

            for (int i = 0; i < n; i++)
            {
                double z_i = Components[i].MoleFraction;
                double liquidNum = Liquid.Components[i].LiquidFugacityNumerator;
                double vaporDen = Vapor.Components[i].VaporFugacityDenominator;

                double K_i = (vaporDen > 0) ? liquidNum / vaporDen : 0.0;
                double denominator = 1.0 + vaporFraction * (K_i - 1.0);

                double xliq = (denominator != 0) ? z_i / denominator : z_i;
                double yvap = K_i * xliq;

                sumy += yvap;
                sumx += xliq;

                Liquid.Components[i].MoleFraction = xliq;
                Vapor.Components[i].MoleFraction = yvap;
            }

            // ✅ AGRUPADO: Normalización de x e y en un solo recorrido
            if (sumx > 0 || sumy > 0)
            {
                for (int i = 0; i < n; i++)
                {
                    if (sumx > 0) Liquid.Components[i].MoleFraction /= sumx;
                    if (sumy > 0) Vapor.Components[i].MoleFraction /= sumy;
                }
            }

            return sumy - sumx;
        }
       
        public Amount CalculateSaturationPressure(Amount temperature, double vaporFraction = 1.0)
        {
            // =========================================================================
            // CASO ESPECIAL: Componente Puro (Agua/Water)
            // =========================================================================
            if (Components.Count == 1 &&
                (Components[0].BaseProperties.Name == "Agua" ||
                 Components[0].BaseProperties.Name == "Water"))
            {
                SaturationPressure = Components[0].SaturationPressure;
                return SaturationPressure;
            }

            // =========================================================================
            // CASO GENERAL: Mezcla Multicomponente
            // =========================================================================
            // ✅ EXTRAÍDO: Todo en un método separado
            var (pGuess, minPsat, maxPsat) = CalculateSmartPressureGuess(temperature, vaporFraction);

            // =========================================================================
            // OPTIMIZACIÓN: Crear Amount UNA vez, usar SetValue en iteración
            // =========================================================================
            Amount pressureCal = new Amount(pGuess, PressureUnits.Bar);  // ← Iniciar en guess
            if (VaporFraction.Data == 0)
                CurrenState = ThermodynamicState.SaturatedLiquid;
            else if (VaporFraction.Data == 1)
                CurrenState = ThermodynamicState.SaturatedVapor;
            else
                CurrenState = ThermodynamicState.VaporLiquidMixture;
            // =========================================================================
            // USAR SecantSolver
            // =========================================================================
            var result = SecantSolver.Solve(
                func: (p) =>
                {
                    pressureCal.SetValue(p, PressureUnits.Bar);  // ← SetValue, NO new Amount
                    return CalculateEquilibrium(temperature, pressureCal, vaporFraction);

                },
                x1: minPsat,
                x2: maxPsat,
                guess: pGuess
            );

            // =========================================================================
            // GUARDAR RESULTADO
            // =========================================================================
            SaturationPressure.SetValue(result.Value, PressureUnits.Bar);

            return SaturationPressure;
        }

        private (double pGuess, double minPsat, double maxPsat) CalculateSmartPressureGuess(Amount temperature, double vaporFraction)
        {
            DefineFlashTVFProperties();

            int n = Components.Count;
            double minPsat = double.MaxValue;
            double maxPsat = double.MinValue;
            double pBubble = 0.0;
            double sumInverseDew = 0.0;

            // ✅ REFACTOR: Un solo ciclo consolida los 4 LINQs (.Min, .Max, .Sum, .Sum)
            for (int i = 0; i < n; i++)
            {
                double psat = Components[i].SaturationPressure.GetValue(PressureUnits.Bar);
                double xi = Components[i].MoleFraction;

                if (psat < minPsat) minPsat = psat;
                if (psat > maxPsat) maxPsat = psat;

                pBubble += xi * psat;
                if (psat > 0) sumInverseDew += xi / psat;
            }

            double pDew = (sumInverseDew > 0) ? 1.0 / sumInverseDew : maxPsat;
            double pGuess;

            if (vaporFraction == 0.0) pGuess = pBubble;
            else if (vaporFraction == 1.0) pGuess = pDew;
            else pGuess = pBubble + vaporFraction * (pDew - pBubble);

            pGuess = Math.Clamp(pGuess, minPsat, maxPsat);
            return (pGuess, minPsat, maxPsat);
        }
       
        private (double tGuess, double minTsat, double maxTsat) CalculateSmartTemperatureGuess(
            Amount pressure,
            double vaporFraction)
        {
            // ✅ 1. Inicializar propiedades del Flash (calcula Tsat de cada componente)
            DefineFlashTProperties();

            int n = Components.Count;
            double minTsat = double.MaxValue;
            double maxTsat = double.MinValue;
            double tBubble = 0.0;

            // ✅ REFACTOR: Un solo ciclo consolida los 3 LINQs (.Min, .Max, .Sum)
            for (int i = 0; i < n; i++)
            {
                double tsat = Components[i].SaturationTemperature.GetValue(TemperatureUnits.Kelvin);
                double xi = Components[i].MoleFraction;

                // Extraer mínimos y máximos
                if (tsat < minTsat) minTsat = tsat;
                if (tsat > maxTsat) maxTsat = tsat;

                // Sumatoria ponderada para el Bubble Point
                tBubble += xi * tsat;
            }

            // ✅ 2. Calcular guess inicial basado en la heurística
            // Para dew, el guess tiende más hacia el componente menos volátil (mayor Tsat)
            double tDew = minTsat + 0.7 * (maxTsat - minTsat);

            double tGuess;

            if (vaporFraction == 0.0)
            {
                tGuess = tBubble;
            }
            else if (vaporFraction == 1.0)
            {
                tGuess = tDew;
            }
            else
            {
                // Flash: Interpolar linealmente entre bubble y dew
                tGuess = tBubble + vaporFraction * (tDew - tBubble);
            }

            // ✅ 3. Asegurar que el guess esté dentro de los límites físicos
            tGuess = Math.Clamp(tGuess, minTsat, maxTsat);

            return (tGuess, minTsat, maxTsat);
        }
       
        public Amount CalculateSaturationTemperature(Amount pressure, double vaporFraction)
        {
            // =========================================================================
            // CASO ESPECIAL: Componente Puro (Agua/Water)
            // =========================================================================
            if (Components.Count == 1 &&
                (Components[0].BaseProperties.Name == "Agua" ||
                 Components[0].BaseProperties.Name == "Water"))
            {
                SaturationTemperature = Components[0].GetSaturedTemperatureAtPressure(pressure);
                return SaturationTemperature;
            }

            // =========================================================================
            // CASO GENERAL: Mezcla Multicomponente
            // =========================================================================
            var (tGuess, minTsat, maxTsat) = CalculateSmartTemperatureGuess(pressure, vaporFraction);

            // =========================================================================
            // OPTIMIZACIÓN: Crear Amount UNA vez, usar SetValue en iteración
            // =========================================================================
            Amount temperatureCal = new Amount(tGuess, TemperatureUnits.Kelvin);  // ← Iniciar en guess
            if (VaporFraction.Data == 0)
                CurrenState = ThermodynamicState.SaturatedLiquid;
            else if (VaporFraction.Data == 1)
                CurrenState = ThermodynamicState.SaturatedVapor;
            else
                CurrenState = ThermodynamicState.VaporLiquidMixture;
            // =========================================================================
            // USAR BisectionSolver
            // =========================================================================
            var result = BisectionSolver.Solve(
                func: (t) =>
                {
                    temperatureCal.SetValue(t, TemperatureUnits.Kelvin);  // ← SetValue, NO new Amount
                    return CalculateEquilibrium(temperatureCal, pressure, vaporFraction);
                },
                x1: minTsat,
                x2: maxTsat,
                guess: tGuess  // ← Usar el guess calculado
            );

            // =========================================================================
            // GUARDAR RESULTADO
            // =========================================================================
            SaturationTemperature.SetValue(result.Value, TemperatureUnits.Kelvin);

            return SaturationTemperature;
        }

       
        private void CalculateFlashTP()
        {
            // =========================================================================
            // 1. Determinar el estado termodinámico
            // =========================================================================
            var state = DeterminePhaseState();
            CurrenState = state;
            // =========================================================================
            // 2. CASO: Líquido Subenfriado (V = 0)
            // =========================================================================
            if (state == ThermodynamicState.SubcooledLiquid ||
                state == ThermodynamicState.SaturatedLiquid)
            {
                VaporFraction.SetCalculatedValue(0.0, "FlashTP");

                // ✅ OPTIMIZADO: O(n) con acceso directo
                for (int i = 0; i < Components.Count; i++)
                {
                    Liquid.Components[i].MoleFraction = Components[i].MoleFraction;
                }
                return;
            }

            // =========================================================================
            // 3. CASO: Vapor Sobrecalentado (V = 1)
            // =========================================================================
            if (state == ThermodynamicState.SuperheatedVapor ||
                state == ThermodynamicState.SaturatedVapor)
            {
                VaporFraction.SetCalculatedValue(1.0, "FlashTP");
                for (int i = 0; i < Components.Count; i++)
                {
                    Vapor.Components[i].MoleFraction = Components[i].MoleFraction;
                }
                
                return;
            }

            // =========================================================================
            // 4. CASO: Mezcla Líquido-Vapor (0 < V < 1) → ITERAR EN V
            // =========================================================================
            if (state == ThermodynamicState.VaporLiquidMixture)
            {
                var vaporFraction = CalculateVaporFraction(Temperature.Data, Pressure.Data);
                VaporFraction.SetCalculatedValue(vaporFraction, "FlashTP");
            }
        }

        /// <summary>
        /// Calcula la fracción de vapor iterando con Rachford-Rice
        /// Retorna: V (0 a 1) que satisface Σ(y_i - x_i) = 0
        /// </summary>
        /// <summary>
        /// Calcula la fracción de vapor iterando con Rachford-Rice
        /// Retorna: V (0 a 1) que satisface Σ(y_i - x_i) = 0
        /// </summary>
        private double CalculateVaporFraction(Amount temperature, Amount pressure)
        {
            // =========================================================================
            // Obtener guess inicial inteligente
            // =========================================================================
            var (vGuess, minV, maxV) = CalculateSmartVaporGuess(temperature, pressure);

            // =========================================================================
            // OPTIMIZACIÓN: Amount se crea UNA vez
            // =========================================================================
            Amount temperatureCal = new Amount(temperature.GetValue(TemperatureUnits.Kelvin), TemperatureUnits.Kelvin);
            Amount pressureCal = new Amount(pressure.GetValue(PressureUnits.Bar), PressureUnits.Bar);

            // =========================================================================
            // USAR BisectionSolver
            // =========================================================================
            var result = BisectionSolver.Solve(
                func: (v) =>
                {
                    return CalculateEquilibrium(temperatureCal, pressureCal, v);
                },
                x1: minV,
                x2: maxV,
                guess: vGuess  // ← Smart guess inicial
            );

            return result.Value;
        }
        /// <summary>
        /// Calcula un guess inicial inteligente para la fracción de vapor
        /// Basado en la posición relativa entre P_bubble, P_dew y P_actual
        /// Fórmula: V ≈ (P_bubble - P_actual) / (P_bubble - P_dew)
        /// </summary>
        /// <returns>Tuple con (vGuess, minV, maxV)</returns>
        private (double vGuess, double minV, double maxV) CalculateSmartVaporGuess(
            Amount temperature,
            Amount pressure)
        {
            double minV = 0.0;  // Todo líquido
            double maxV = 1.0;  // Todo vapor

            // Calcular puntos de saturación para estimar V
            double P_bubble = CalculateSaturationPressure(temperature, 0).GetValue(PressureUnits.Bar);
            double P_dew = CalculateSaturationPressure(temperature, 1).GetValue(PressureUnits.Bar);
            double P_actual = pressure.GetValue(PressureUnits.Bar);

            // =========================================================================
            // Guess basado en interpolación lineal entre bubble y dew
            // =========================================================================
            double vGuess;
            double denominator = P_bubble - P_dew;

            if (denominator > 1e-6)
            {
                vGuess = (P_bubble - P_actual) / denominator;
                vGuess = Math.Clamp(vGuess, minV, maxV);
            }
            else
            {
                // Si bubble y dew están muy cerca, usar 0.5 como guess
                vGuess = 0.5;
            }

            return (vGuess, minV, maxV);
        }
        /// <summary>
        /// Determina el estado termodinámico de la corriente (Líquido, Vapor, o Mezcla L+V)
        /// Basado en comparación con puntos de burbuja y rocío
        /// </summary>
        public ThermodynamicState DeterminePhaseState()
        {
            double T_actual = Temperature.Data.GetValue(TemperatureUnits.Kelvin);
            double P_actual = Pressure.Data.GetValue(PressureUnits.Bar);

            double T_bubble = 0.0;
            double T_dew = 0.0;
            double P_bubble = 0.0;
            double P_dew = 0.0;

            // =========================================================================
            // 1. CALCULAR PUNTOS DE SATURACIÓN (Solo lo necesario)
            // =========================================================================
            if (Temperature.State == VariableState.UserDefined)
            {
                P_bubble = CalculateSaturationPressure(Temperature.Data, 0).GetValue(PressureUnits.Bar);
                P_dew = CalculateSaturationPressure(Temperature.Data, 1).GetValue(PressureUnits.Bar);

                Liquid.SaturationPressure = new Amount(P_bubble, PressureUnits.Bar);
                Vapor.SaturationPressure = new Amount(P_dew, PressureUnits.Bar);
            }

            if (Pressure.State == VariableState.UserDefined)
            {
                T_bubble = CalculateSaturationTemperature(Pressure.Data, 0).GetValue(TemperatureUnits.Kelvin);
                T_dew = CalculateSaturationTemperature(Pressure.Data, 1).GetValue(TemperatureUnits.Kelvin);

                Liquid.SaturationTemperature = new Amount(T_bubble, TemperatureUnits.Kelvin);
                Vapor.SaturationTemperature = new Amount(T_dew, TemperatureUnits.Kelvin);
            }

            // =========================================================================
            // 2. TOLERANCIAS
            // =========================================================================
            const double TOL_T = 0.1;    // 0.1 Kelvin
            const double TOL_P = 0.01;   // 0.01 Bar

            // =========================================================================
            // 3. LÓGICA DE DETERMINACIÓN DE FASE (Orden CORRECTO)
            // =========================================================================

            // ✅ PRIMERO: Verificar condiciones SATURADAS (con tolerancia)
            if (Temperature.State == VariableState.UserDefined)
            {
                if (Math.Abs(P_actual - P_bubble) < TOL_P)
                    return ThermodynamicState.SaturatedLiquid;

                if (Math.Abs(P_actual - P_dew) < TOL_P)
                    return ThermodynamicState.SaturatedVapor;
            }

            if (Pressure.State == VariableState.UserDefined)
            {
                if (Math.Abs(T_actual - T_bubble) < TOL_T)
                    return ThermodynamicState.SaturatedLiquid;

                if (Math.Abs(T_actual - T_dew) < TOL_T)
                    return ThermodynamicState.SaturatedVapor;
            }

            // ✅ SEGUNDO: Verificar MEZCLA L+V
            bool inMixtureT = (T_dew < T_actual && T_actual < T_bubble);
            bool inMixtureP = (P_dew < P_actual && P_actual < P_bubble);

            if (inMixtureT || inMixtureP)
                return ThermodynamicState.VaporLiquidMixture;

            // ✅ TERCERO: Verificar LÍQUIDO SUBENFRIADO
            if ((Pressure.State == VariableState.UserDefined && T_actual < T_bubble - TOL_T) ||
                (Temperature.State == VariableState.UserDefined && P_actual > P_bubble + TOL_P))
            {
                return ThermodynamicState.SubcooledLiquid;
            }

            // ✅ CUARTO: Verificar VAPOR SOBRECALENTADO
            if ((Pressure.State == VariableState.UserDefined && T_actual > T_dew + TOL_T) ||
                (Temperature.State == VariableState.UserDefined && P_actual < P_dew - TOL_P))
            {
                return ThermodynamicState.SuperheatedVapor;
            }

            // ✅ Fallback
            return ThermodynamicState.Undefined;
        }
        public void CalculateStreamProperties()
        {
            if (CurrenState == ThermodynamicState.Undefined)
                return;

            // Anclas Termodinámicas
            Amount tSys = Temperature.Data;
            Amount pSys = Pressure.Data;
            Amount tSat = SaturationTemperature;

            switch (CurrenState)
            {
                case ThermodynamicState.SubcooledLiquid:
                    PrepareAndCalculatePhase(Liquid, tSys, pSys); // Fase Real
                    PrepareAndCalculatePhase(Vapor, tSat, pSys);  // UI Referencia
                    CopyPropertiesFromSinglePhase(Liquid);
                    break;

                case ThermodynamicState.SaturatedLiquid:
                    PrepareAndCalculatePhase(Liquid, tSat, pSys); // Fase Real
                    PrepareAndCalculatePhase(Vapor, tSat, pSys);  // Fase Real
                    CopyPropertiesFromSinglePhase(Liquid);
                    break;

                case ThermodynamicState.VaporLiquidMixture:
                    PrepareAndCalculatePhase(Liquid, tSat, pSys); // Fase Real
                    PrepareAndCalculatePhase(Vapor, tSat, pSys);  // Fase Real
                    CalculateTwoPhaseMixtureProperties();
                    break;

                case ThermodynamicState.SaturatedVapor:
                    PrepareAndCalculatePhase(Liquid, tSat, pSys); // Fase Real
                    PrepareAndCalculatePhase(Vapor, tSat, pSys);  // Fase Real
                    CopyPropertiesFromSinglePhase(Vapor);
                    break;

                case ThermodynamicState.SuperheatedVapor:
                    PrepareAndCalculatePhase(Vapor, tSys, pSys);  // Fase Real
                    PrepareAndCalculatePhase(Liquid, tSat, pSys); // UI Referencia
                    CopyPropertiesFromSinglePhase(Vapor);
                    break;

                default:
                    ResetIntensiveProperties();
                    break;
            }
        }

        // =========================================================================
        // AYUDANTE MAESTRO DE FASE (Configura T/P y dispara cálculo)
        // =========================================================================
        private void PrepareAndCalculatePhase<TPhase>(PhaseMixtureBase<TPhase> phase, Amount temp, Amount pres) where TPhase : PhaseComponent
        {
            if (phase is null) return;

            // Actualizamos las variables de proceso para que la mezcla lea la T y P correcta
            phase.Temperature.SetCalculatedValue(temp.GetValue(phase.Temperature.Data.Unit), phase.Temperature.Data.Unit, this.Name);
            phase.Pressure.SetCalculatedValue(pres.GetValue(phase.Pressure.Data.Unit), phase.Pressure.Data.Unit, this.Name);

            // Se propaga la T y P a los componentes puros
            phase.CalculateTP(temp, pres);

            // Se ejecutan las reglas de mezcla (Bulk) de la fase
            phase.CalculateBulkProperties();
        }

        // =========================================================================
        // REGLAS PARA MEZCLA LÍQUIDO-VAPOR (Bubble-Up)
        // =========================================================================
        private void CalculateTwoPhaseMixtureProperties()
        {
            if (Liquid is null || Vapor is null)
            {
                ResetIntensiveProperties();
                return;
            }

            double vaporFractionMolar = VaporFraction.Data;

            double mwLiquid = CalculatePhaseMolecularWeight(Liquid.Components);
            double mwVapor = CalculatePhaseMolecularWeight(Vapor.Components);
            double mwMixture = CalculateMixtureMolecularWeight(mwLiquid, mwVapor, vaporFractionMolar);

            double vaporFractionMass = CalculateMassVaporFraction(vaporFractionMolar, mwLiquid, mwVapor, mwMixture);

            CalculateMixtureEnthalpyAndHeatCapacity(vaporFractionMolar, vaporFractionMass);
            CalculateMixtureDensity(vaporFractionMass);
            CalculateMixtureTransportProperties(vaporFractionMass);
        }

        private double CalculatePhaseMolecularWeight(IEnumerable<PhaseComponent> components)
        {
            if (components is null) return 0.0;
            return components.Sum(c => c.MoleFraction * c.BaseProperties.MolecularWeight);
        }

        private double CalculateMixtureMolecularWeight(double mwLiquid, double mwVapor, double vaporFractionMolar)
        {
            return (1.0 - vaporFractionMolar) * mwLiquid + vaporFractionMolar * mwVapor;
        }

        private double CalculateMassVaporFraction(double vaporFractionMolar, double mwLiquid, double mwVapor, double mwMixture)
        {
            if (mwMixture <= ThermodynamicConstants.MinPositiveValue) return 0.0;
            return (vaporFractionMolar * mwVapor) / mwMixture;
        }

        private void CalculateMixtureEnthalpyAndHeatCapacity(double vaporFractionMolar, double vaporFractionMass)
        {
            double hMolar = (1.0 - vaporFractionMolar) * Liquid.MolarEnthalpy.Data.Value + vaporFractionMolar * Vapor.MolarEnthalpy.Data.Value;
            double hMass = (1.0 - vaporFractionMass) * Liquid.MassEnthalpy.Data.Value + vaporFractionMass * Vapor.MassEnthalpy.Data.Value;
            double cpMass = (1.0 - vaporFractionMass) * Liquid.MassHeatCapacity.Data.Value + vaporFractionMass * Vapor.MassHeatCapacity.Data.Value;

            MolarEnthalpy.SetCalculatedValue(hMolar, MolarEnthalpy.Data.Unit, "TwoPhaseMix");
            MassEnthalpy.SetCalculatedValue(hMass, MassEnthalpy.Data.Unit, "TwoPhaseMix");
            MassHeatCapacity.SetCalculatedValue(cpMass, MassHeatCapacity.Data.Unit, "TwoPhaseMix");
        }

        private void CalculateMixtureDensity(double vaporFractionMass)
        {
            double volSpecLiquid = CalculateSpecificVolume(Liquid.MassDensity.Data.Value);
            double volSpecVapor = CalculateSpecificVolume(Vapor.MassDensity.Data.Value);

            double volSpecMixture = (1.0 - vaporFractionMass) * volSpecLiquid + vaporFractionMass * volSpecVapor;
            double massDensityMixture = CalculateDensityFromSpecificVolume(volSpecMixture);

            MassDensity.SetCalculatedValue(massDensityMixture, MassDensityUnits.Kg_m3, "TwoPhaseMix");
        }

        private void CalculateMixtureTransportProperties(double vaporFractionMass)
        {
            Viscosity.Reset();
            ThermalConductivity.Reset();

            SurfaceTension.SetCalculatedValue(Liquid.SurfaceTension.Data.Value, SurfaceTension.Data.Unit, "TwoPhaseMix");
        }

        // =========================================================================
        // COPIA DE PROPIEDADES PARA FASES PURAS (Líquido o Vapor)
        // =========================================================================
        private void CopyPropertiesFromSinglePhase<T>(PhaseMixtureBase<T> phase) where T : PhaseComponent
        {
            if (phase is null)
            {
                ResetIntensiveProperties();
                return;
            }

            MassEnthalpy.SetCalculatedValue(phase.MassEnthalpy.Data.Value, MassEnthalpy.Data.Unit, "SinglePhase");
            MolarEnthalpy.SetCalculatedValue(phase.MolarEnthalpy.Data.Value, MolarEnthalpy.Data.Unit, "SinglePhase");
            MassHeatCapacity.SetCalculatedValue(phase.MassHeatCapacity.Data.Value, MassHeatCapacity.Data.Unit, "SinglePhase");

            MassDensity.SetCalculatedValue(phase.MassDensity.Data.Value, MassDensity.Data.Unit, "SinglePhase");
            MolarDensity.SetCalculatedValue(phase.MolarDensity.Data.Value, MolarDensity.Data.Unit, "SinglePhase");

            Viscosity.SetCalculatedValue(phase.Viscosity.Data.Value, Viscosity.Data.Unit, "SinglePhase");
            ThermalConductivity.SetCalculatedValue(phase.ThermalConductivity.Data.Value, ThermalConductivity.Data.Unit, "SinglePhase");

            if (phase is LiquidPhase liquidPhase)
            {
                SurfaceTension.SetCalculatedValue(liquidPhase.SurfaceTension.Data.Value, SurfaceTension.Data.Unit, "SinglePhase");
            }
            else
            {
                SurfaceTension.Reset();
            }
        }

        private double CalculateSpecificVolume(double massDensity)
        {
            if (massDensity <= ThermodynamicConstants.MinPositiveValue) return 0.0;
            return 1.0 / massDensity;
        }

        private double CalculateDensityFromSpecificVolume(double specificVolume)
        {
            if (specificVolume <= ThermodynamicConstants.MinPositiveValue) return 0.0;
            return 1.0 / specificVolume;
        }

        private void ResetIntensiveProperties()
        {
            MassEnthalpy.Reset();
            MolarEnthalpy.Reset();
            MassHeatCapacity.Reset();
            MassDensity.Reset();
            MolarDensity.Reset();
            Viscosity.Reset();
            ThermalConductivity.Reset();
            SurfaceTension.Reset();
        }
    }
}
