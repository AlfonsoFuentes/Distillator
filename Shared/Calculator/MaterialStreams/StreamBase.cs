using Shared.Calculator.Components;
using Shared.Calculator.ProcessVariables;
using UnitSystem;

namespace Shared.Calculator.MaterialStreams
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public abstract class StreamBase<T> where T : StreamComponent
    {
        // =========================================================================
        // LISTA DE COMPONENTES
        // =========================================================================
        public List<T> Components { get; set; } = new();

        // =========================================================================
        // VARIABLES DE ESTADO Y FLUJOS
        // =========================================================================
        public ProcessVariable Temperature { get; set; }
        public ProcessVariable Pressure { get; set; }
        public ProcessVariable MolarFlow { get; set; }
        public ProcessVariable MassFlow { get; set; }
        public ProcessVariable VolumetricFlow { get; set; }
        public ProcessVariable EnthalpyFlow { get; set; }

        // =========================================================================
        // PROPIEDADES BULK
        // =========================================================================
        public ProcessVariable MolarEnthalpy { get; set; }
        public ProcessVariable MassEnthalpy { get; set; }
        public ProcessVariable MassDensity { get; set; }
        public ProcessVariable MolarDensity { get; set; }
        public ProcessVariable MassHeatCapacity { get; set; }
        public ProcessVariable MolarHeatCapacity { get; set; }
        public ProcessVariable Viscosity { get; set; }
        public ProcessVariable ThermalConductivity { get; set; }
        public ProcessVariable SurfaceTension { get; set; }

        public Amount SaturationPressure { get; set; }
        public Amount SaturationTemperature { get; set; }
        public Amount TempCritical { get; set; }
        public Amount PressCritical { get; set; }
        public Amount MolarVolCritical { get; set; }

        protected StreamBase()
        {
            Temperature = new ProcessVariable("Temperature", TemperatureUnits.DegreeCelcius, true);
            Pressure = new ProcessVariable("Pressure", PressureUnits.psi, true);

            MolarFlow = new ProcessVariable("Molar flow", MolarFlowUnits.Kgmol_hr, true);
            MassFlow = new ProcessVariable("Mass flow", MassFlowUnits.Kg_hr, true);
            VolumetricFlow = new ProcessVariable("Volumetric flow", VolumetricFlowUnits.m3_hr, true);
            EnthalpyFlow = new ProcessVariable("Enthalpy flow", EnergyFlowUnits.Kcal_hr, false);

            MolarEnthalpy = new ProcessVariable("Molar Enthalpy", MolarEnergyUnits.Kcal_Kgmol, false);
            MassEnthalpy = new ProcessVariable("Mass Enthalpy", MassEnergyUnits.Kcal_Kg, false);
            MassDensity = new ProcessVariable("Mass Density", MassDensityUnits.Kg_m3, false);
            MolarDensity = new ProcessVariable("Molar Density", MolarDensityUnits.Kgmol_m3, false);

            MassHeatCapacity = new ProcessVariable("Heat Capacity", MassEntropyUnits.Kcal_Kg_C, false);
            MolarHeatCapacity = new ProcessVariable("Heat Capacity", MolarEntropyUnits.Kcal_Kgmol_C, false);
            Viscosity = new ProcessVariable("Viscosity", ViscosityUnits.cPoise, false);
            ThermalConductivity = new ProcessVariable("Thermal Conductivity", ThermalConductivityUnits.kcal_hr_m_C, false);

            SurfaceTension = new ProcessVariable("Surface Tension", SurfaceTensionUnits.N_m, false);

            SaturationPressure = new Amount(0.0, PressureUnits.Bar);
            SaturationTemperature = new Amount(0.0, TemperatureUnits.DegreeCelcius);
            TempCritical = new Amount(0.0, TemperatureUnits.Kelvin);
            PressCritical = new Amount(0.0, PressureUnits.KiloPascal);
            MolarVolCritical = new Amount(0.0, MolarVolumeSpecificUnits.m3_Kgmol);
        }

        // =========================================================================
        // ORQUESTADOR DE BASE (Física Pura para Corriente y Fases)
        // =========================================================================
        public virtual void CalculateBulkProperties(ThermodynamicState state = ThermodynamicState.Undefined)
        {
            switch (state)
            {
                case ThermodynamicState.SubcooledLiquid:
                case ThermodynamicState.SaturatedLiquid:
                    CalculateLiquidStateProperties(state);
                    break;
                case ThermodynamicState.SuperheatedVapor:
                case ThermodynamicState.SaturatedVapor:
                    CalculateVaporStateProperties(state);
                    break;
            }
        }

        protected void CalculateLiquidStateProperties(ThermodynamicState exactState)
        {
            foreach (var comp in Components)
            {
                comp.Temperature.SetValue(Temperature.Data.GetValue(TemperatureUnits.Kelvin), TemperatureUnits.Kelvin);
                comp.Pressure.SetValue(Pressure.Data.GetValue(PressureUnits.Bar), PressureUnits.Bar);
                comp.CalculateIntensiveProperties(exactState);
            }

            CalculateLiquidMixtureHeatCapacity();
            CalculateLiquidMixtureDensity();
            CalculateLiquidMixtureThermalConductivity();
            CalculateLiquidMixtureViscosity();
            CalculateLiquidMixtureEnthalpy();
            CalculateMixtureSurfaceTension(); // Implementado correctamente genérico
        }

        protected void CalculateVaporStateProperties(ThermodynamicState exactState)
        {
            foreach (var comp in Components)
            {
                comp.Temperature.SetValue(Temperature.Data.GetValue(TemperatureUnits.Kelvin), TemperatureUnits.Kelvin);
                comp.Pressure.SetValue(Pressure.Data.GetValue(PressureUnits.Bar), PressureUnits.Bar);
                comp.CalculateIntensiveProperties(exactState);
            }

            CalculateGasMixtureHeatCapacity();
            CalculateGasMixtureEnthalpy();
            CalculateGasMixtureThermalConductivity();
            CalculateGasMixtureViscosity();
            // La densidad del vapor se inyecta externamente desde VaporPhase
        }

        // =========================================================================
        // REGLAS DE MEZCLA LÍQUIDA
        // =========================================================================
        public void CalculateLiquidMixtureHeatCapacity()
        {
            if (Components is null or { Count: 0 }) return;
            double cpMixMass = 0.0, mwMix = 0.0;

            foreach (var component in Components)
            {
                cpMixMass += component.MassFraction * component.MassHeatCapacity.GetValue(MassEntropyUnits.Kcal_Kg_C);
                mwMix += component.MoleFraction * component.BaseProperties.MolecularWeight;
            }

            MassHeatCapacity.SetCalculatedValue(cpMixMass, MassEntropyUnits.Kcal_Kg_C, "MixingRule");
            MolarHeatCapacity.SetCalculatedValue(cpMixMass * mwMix, MolarEntropyUnits.Kcal_Kgmol_C, "MixingRule");
        }

        public void CalculateLiquidMixtureDensity()
        {
            if (Components is null or { Count: 0 }) { ResetDensityProperties(); return; }

            double sumInverseDensity = 0.0, mwMix = 0.0;
            foreach (var component in Components)
            {
                double densityComponent = component.MassDensity.GetValue(MassDensityUnits.Kg_m3);
                if (densityComponent <= 0) continue;
                sumInverseDensity += component.MassFraction / densityComponent;
                mwMix += component.MoleFraction * component.BaseProperties.MolecularWeight;
            }

            if (sumInverseDensity <= 0) { ResetDensityProperties(); return; }

            double mixMassDensity = 1.0 / sumInverseDensity;
            MassDensity.SetCalculatedValue(mixMassDensity, MassDensityUnits.Kg_m3, "AmagatRule");

            if (mwMix > 0) MolarDensity.SetCalculatedValue(mixMassDensity / mwMix, MolarDensityUnits.Kgmol_m3, "AmagatRule");
            else MolarDensity.Reset();
        }

        private void ResetDensityProperties()
        {
            MassDensity.Reset();
            MolarDensity.Reset();
        }

        public void CalculateLiquidMixtureThermalConductivity()
        {
            if (Components is null or { Count: 0 }) return;
            double kMix = Components.Sum(c => c.MoleFraction * c.ThermalConductivity.GetValue(ThermalConductivityUnits.W_m_K));
            ThermalConductivity.SetCalculatedValue(kMix, ThermalConductivityUnits.W_m_K, "LinearMolarRule");
        }

        public void CalculateLiquidMixtureViscosity()
        {
            if (Components is null or { Count: 0 }) return;
            double sumCubeRoot = Components
                .Where(c => c.Viscosity.GetValue(ViscosityUnits.cPoise) > 0)
                .Sum(c => c.MoleFraction * Math.Pow(c.Viscosity.GetValue(ViscosityUnits.cPoise), 1.0 / 3.0));

            Viscosity.SetCalculatedValue(Math.Pow(sumCubeRoot, 3.0), ViscosityUnits.cPoise, "KendallMonroeRule");
        }

        public void CalculateLiquidMixtureEnthalpy()
        {
            if (Components is null or { Count: 0 }) return;

            double hMixMass = Components.Sum(c => c.MassFraction * c.MassEnthalpy.GetValue(MassEnergyUnits.J_Kg));
            double hMixMolar = Components.Sum(c => c.MoleFraction * c.MolarEnthalpy.GetValue(MolarEnergyUnits.J_Kgmol));

            MassEnthalpy.SetCalculatedValue(hMixMass, MassEnergyUnits.J_Kg, "IdealMixture");
            MolarEnthalpy.SetCalculatedValue(hMixMolar, MolarEnergyUnits.J_Kgmol, "IdealMixture");
        }

        // =========================================================================
        // TENSIÓN SUPERFICIAL (Corrección Genérica usando T)
        // =========================================================================
        public void CalculateMixtureSurfaceTension()
        {
            if (Components is null or { Count: 0 }) return;

            bool hasWater = false;
            bool hasAlcohol = false;
            T waterComponent = null!;
            T alcoholComponent = null!;

            // ✅ Usamos el tipo genérico T en lugar de LiquidComponent
            foreach (T component in Components)
            {
                string name = component.BaseProperties.Name.ToLower();
                string family = component.BaseProperties.Family?.ToLower() ?? string.Empty;

                if (name == "agua" || name == "water")
                {
                    hasWater = true;
                    waterComponent = component;
                }
                else if (family == "alcohol")
                {
                    hasAlcohol = true;
                    alcoholComponent = component;
                }
            }

            if (hasWater && hasAlcohol && Components.Count == 2 && waterComponent != null && alcoholComponent != null)
            {
                CalculateTamuraKurataSurfaceTension(waterComponent, alcoholComponent);
            }
            else
            {
                CalculateLinearSurfaceTension();
            }
        }

        private void CalculateLinearSurfaceTension()
        {
            double surfaceTensionMix = Components.Sum(c => c.MoleFraction * c.SurfaceTension.GetValue(SurfaceTensionUnits.N_m));
            SurfaceTension.SetCalculatedValue(surfaceTensionMix, SurfaceTensionUnits.N_m, "LinearMolarRule");
        }

        private void CalculateTamuraKurataSurfaceTension(T water, T alcohol)
        {
            double totalMoles = water.MoleFraction + alcohol.MoleFraction;
            if (totalMoles <= 0) return;

            double xWater = water.MoleFraction / totalMoles;
            double xAlcohol = alcohol.MoleFraction / totalMoles;
            double stWater = water.SurfaceTension.GetValue(SurfaceTensionUnits.N_m);
            double stAlcohol = alcohol.SurfaceTension.GetValue(SurfaceTensionUnits.N_m);

            if (xWater >= ThermodynamicConstants.PureComponentThreshold) { SurfaceTension.SetCalculatedValue(stWater, SurfaceTensionUnits.N_m, "TamuraKurata"); return; }
            if (xAlcohol >= ThermodynamicConstants.PureComponentThreshold) { SurfaceTension.SetCalculatedValue(stAlcohol, SurfaceTensionUnits.N_m, "TamuraKurata"); return; }

            double volWater = water.MolarVolume.GetValue(MolarVolumeSpecificUnits.m3_Kgmol);
            double volAlcohol = alcohol.MolarVolume.GetValue(MolarVolumeSpecificUnits.m3_Kgmol);
            double tempK = Temperature.Data.GetValue(TemperatureUnits.Kelvin);

            int carbonAtoms = string.IsNullOrEmpty(alcohol.BaseProperties.Formula) ? 1 : alcohol.BaseProperties.Formula.Count(c => c == 'C');
            if (carbonAtoms == 0) carbonAtoms = 1;

            double numLog = Math.Pow(xWater * volWater, 2.0);
            double denLog = (volAlcohol * xAlcohol) * (xWater * volWater + xAlcohol * volAlcohol);

            double term1 = Math.Log(numLog / denLog);
            double term2 = (ThermodynamicConstants.TamuraKurataConstant * carbonAtoms / tempK) *
                           ((stAlcohol / carbonAtoms) * Math.Pow(volAlcohol, 2.0 / 3.0) - stWater * Math.Pow(volWater, 2.0 / 3.0));

            double logPhi = Math.Pow(10.0, term1 + term2);
            double phiWater = (-logPhi + Math.Sqrt(Math.Pow(logPhi, 2.0) + 4.0 * logPhi)) / 2.0;
            double phiAlcohol = 1.0 - phiWater;

            double stMix = Math.Pow(
                phiWater * Math.Pow(stWater, ThermodynamicConstants.SurfaceTensionExponent) +
                phiAlcohol * Math.Pow(stAlcohol, ThermodynamicConstants.SurfaceTensionExponent),
                ThermodynamicConstants.SurfaceTensionPower);

            SurfaceTension.SetCalculatedValue(stMix, SurfaceTensionUnits.N_m, "TamuraKurata");
        }

        protected double CalculateMixtureMolecularWeight()
        {
            return Components.Sum(c => c.MoleFraction * c.BaseProperties.MolecularWeight);
        }

        protected void SetDensityProperties(double molarVolume, double mwMix)
        {
            double molarDensity = 1.0 / molarVolume;
            double massDensity = mwMix / molarVolume;
            MolarDensity.SetCalculatedValue(molarDensity, MolarDensityUnits.Kgmol_m3, "calculationRule");
            MassDensity.SetCalculatedValue(massDensity, MassDensityUnits.Kg_m3, "calculationRule");
        }

        // =========================================================================
        // REGLAS DE MEZCLA GASEOSA
        // =========================================================================
        public void CalculateGasMixtureThermalConductivity()
        {
            if (Components is null or { Count: 0 }) return;
            double kMix = Components.Sum(c => c.MoleFraction * c.ThermalConductivity.GetValue(ThermalConductivityUnits.W_m_K));
            ThermalConductivity.SetCalculatedValue(kMix, ThermalConductivityUnits.W_m_K, "LinearMolarRule");
        }

        public void CalculateGasMixtureViscosity()
        {
            if (Components is null or { Count: 0 }) return;
            double sumCubeRoot = Components
                .Where(c => c.Viscosity.GetValue(ViscosityUnits.cPoise) > 0)
                .Sum(c => c.MoleFraction * Math.Pow(c.Viscosity.GetValue(ViscosityUnits.cPoise), 1.0 / 3.0));

            Viscosity.SetCalculatedValue(Math.Pow(sumCubeRoot, 3.0), ViscosityUnits.cPoise, "CubeRootRule");
        }

        public void CalculateGasMixtureHeatCapacity()
        {
            if (Components is null or { Count: 0 }) return;
            double cpMixMass = 0.0, mwMix = 0.0;

            foreach (var component in Components)
            {
                cpMixMass += component.MassFraction * component.MassHeatCapacity.GetValue(MassEntropyUnits.KJ_Kg_C);
                mwMix += component.MoleFraction * component.BaseProperties.MolecularWeight;
            }

            MassHeatCapacity.SetCalculatedValue(cpMixMass, MassEntropyUnits.KJ_Kg_C, "LinearMassRule");
            MolarHeatCapacity.SetCalculatedValue(cpMixMass * mwMix, MolarEntropyUnits.KJ_Kgmol_C, "LinearMolarRule");
        }

        public void CalculateGasMixtureEnthalpy()
        {
            if (Components is null or { Count: 0 }) return;

            double hMixMass = Components.Sum(c => c.MassFraction * c.MassEnthalpy.GetValue(MassEnergyUnits.J_Kg));
            double hMixMolar = Components.Sum(c => c.MoleFraction * c.MolarEnthalpy.GetValue(MolarEnergyUnits.J_Kgmol));

            MassEnthalpy.SetCalculatedValue(hMixMass, MassEnergyUnits.J_Kg, "IdealGasMixture");
            MolarEnthalpy.SetCalculatedValue(hMixMolar, MolarEnergyUnits.J_Kgmol, "IdealGasMixture");
        }
    }
    public abstract class StreamBase2<T> where T : StreamComponent
    {
        // =========================================================================
        // LISTA DE COMPONENTES
        // =========================================================================
        public List<T> Components { get; set; } = new();

        // =========================================================================
        // VARIABLES DE ESTADO
        // =========================================================================
        public ProcessVariable Temperature { get; set; }
        public ProcessVariable Pressure { get; set; }

        // =========================================================================
        // FLUJOS GLOBALES
        // =========================================================================
        public ProcessVariable MolarFlow { get; set; }
        public ProcessVariable MassFlow { get; set; }
        public ProcessVariable VolumetricFlow { get; set; }
        public ProcessVariable EnthalpyFlow { get; set; }

        // =========================================================================
        // PROPIEDADES BULK (Molares y Másicas)
        // =========================================================================
        public ProcessVariable MolarEnthalpy { get; set; }
        public ProcessVariable MassEnthalpy { get; set; }
        public ProcessVariable MassDensity { get; set; }
        public ProcessVariable MolarDensity { get; set; }

        // =========================================================================
        // PROPIEDADES DE TRANSPORTE Y TÉRMICAS
        // =========================================================================
        public ProcessVariable MassHeatCapacity { get; set; }
        public ProcessVariable MolarHeatCapacity { get; set; }
        public ProcessVariable Viscosity { get; set; }
        public ProcessVariable ThermalConductivity { get; set; }
        public ProcessVariable SurfaceTension { get; set; }
        public Amount SaturationPressure { get; set; }
        public Amount SaturationTemperature { get; set; }

        public Amount TempCritical { get; set; }
        public Amount PressCritical { get; set; }
        public Amount MolarVolCritical { get; set; }

        protected StreamBase2()
        {
            // Inicialización de Variables de Estado (Input del usuario o calculadas)
            Temperature = new ProcessVariable("Temperature", TemperatureUnits.DegreeCelcius, true);
            Pressure = new ProcessVariable("Pressure", PressureUnits.psi, true);

            // Inicialización de Flujos
            MolarFlow = new ProcessVariable("Molar flow", MolarFlowUnits.Kgmol_hr, true);
            MassFlow = new ProcessVariable("Mass flow", MassFlowUnits.Kg_hr, true);
            VolumetricFlow = new ProcessVariable("Volumetric flow", VolumetricFlowUnits.m3_hr, true);
            EnthalpyFlow = new ProcessVariable("Enthalpy flow", EnergyFlowUnits.Kcal_hr, false);

            // Inicialización de Propiedades Bulk
            MolarEnthalpy = new ProcessVariable("Molar Enthalpy", MolarEnergyUnits.Kcal_Kgmol, false);
            MassEnthalpy = new ProcessVariable("Mass Enthalpy", MassEnergyUnits.Kcal_Kg, false);
            MassDensity = new ProcessVariable("Mass Density", MassDensityUnits.Kg_m3, false);
            MolarDensity = new ProcessVariable("Molar Density", MolarDensityUnits.Kgmol_m3, false);

            // Inicialización de Propiedades Térmicas y Transporte
            MassHeatCapacity = new ProcessVariable("Heat Capacity", MassEntropyUnits.Kcal_Kg_C, false);
            MolarHeatCapacity = new ProcessVariable("Heat Capacity", MolarEntropyUnits.Kcal_Kgmol_C, false);
            Viscosity = new ProcessVariable("Viscosity", ViscosityUnits.cPoise, false);
            ThermalConductivity = new ProcessVariable("Thermal Conductivity", ThermalConductivityUnits.kcal_hr_m_C, false);

            // Nota: En tu código original SurfaceTension tenía unidades de conductividad térmica. 
            // Lo dejé igual para no romper tu compilación, pero revisa si deberías cambiarlo a SurfaceUnits.
            SurfaceTension = new ProcessVariable("Surface Tension", ThermalConductivityUnits.kcal_hr_m_C, false);

            SaturationPressure = new Amount(0.0, PressureUnits.Bar);
            SaturationTemperature = new Amount(0.0, TemperatureUnits.DegreeCelcius);
            TempCritical = new Amount(0.0, TemperatureUnits.Kelvin);
            PressCritical = new Amount(0.0, PressureUnits.KiloPascal);
            MolarVolCritical = new Amount(0.0, MolarVolumeSpecificUnits.m3_Kgmol); // Asumiendo m3/kmol para el cálculo de R
        }

        public void CalculateLiquidMixtureHeatCapacity()
        {
            if (Components is null or { Count: 0 })
                return;

            double cpMixMass = 0.0;
            double mwMix = 0.0;

            foreach (var component in Components)
            {
                double massFraction = component.MassFraction;
                double moleFraction = component.MoleFraction;
                double cpComponent = component.MassHeatCapacity.GetValue(MassEntropyUnits.Kcal_Kg_C);

                cpMixMass += massFraction * cpComponent;
                mwMix += moleFraction * component.BaseProperties.MolecularWeight;
            }

            MassHeatCapacity.SetCalculatedValue(
                cpMixMass,
                MassEntropyUnits.Kcal_Kg_C,
                "MixingRule");

            MolarHeatCapacity.SetCalculatedValue(
                cpMixMass * mwMix,
                MolarEntropyUnits.Kcal_Kgmol_C,
                "MixingRule");
        }

        // =========================================================================
        // DENSIDAD DE MEZCLA (Regla de Amagat - Volúmenes aditivos)
        // =========================================================================
        public void CalculateLiquidMixtureDensity()
        {
            if (Components is null or { Count: 0 })
            {
                ResetDensityProperties();
                return;
            }

            double sumInverseDensity = 0.0;
            double mwMix = 0.0;

            foreach (var component in Components)
            {
                double massFraction = component.MassFraction;
                double moleFraction = component.MoleFraction;
                double densityComponent = component.MassDensity.GetValue(MassDensityUnits.Kg_m3);

                if (densityComponent <= 0)
                    continue;

                sumInverseDensity += massFraction / densityComponent;
                mwMix += moleFraction * component.BaseProperties.MolecularWeight;
            }

            if (sumInverseDensity <= 0)
            {
                ResetDensityProperties();
                return;
            }

            double mixMassDensity = 1.0 / sumInverseDensity;
            MassDensity.SetCalculatedValue(
                mixMassDensity,
                MassDensityUnits.Kg_m3,
                "AmagatRule");

            if (mwMix > 0)
            {
                double mixMolarDensity = mixMassDensity / mwMix;
                MolarDensity.SetCalculatedValue(
                    mixMolarDensity,
                    MolarDensityUnits.Kgmol_m3,
                    "AmagatRule");
            }
            else
            {
                MolarDensity.Reset();
            }
        }

        private void ResetDensityProperties()
        {
            MassDensity.Reset();
            MolarDensity.Reset();
        }

        public void CalculateLiquidMixtureThermalConductivity()
        {
            if (Components is null or { Count: 0 })
                return;

            double kMix = 0.0;

            foreach (var component in Components)
            {
                double moleFraction = component.MoleFraction;
                double kComponent = component.ThermalConductivity.GetValue(ThermalConductivityUnits.W_m_K);
                kMix += moleFraction * kComponent;
            }

            ThermalConductivity.SetCalculatedValue(
                kMix,
                ThermalConductivityUnits.W_m_K,
                "LinearMolarRule");
        }


        public void CalculateLiquidMixtureViscosity()
        {
            if (Components is null or { Count: 0 })
                return;

            double sumCubeRoot = 0.0;

            foreach (var component in Components)
            {
                double moleFraction = component.MoleFraction;
                double viscosityComponent = component.Viscosity.GetValue(ViscosityUnits.cPoise);

                if (viscosityComponent <= 0)
                    continue;

                sumCubeRoot += moleFraction * Math.Pow(viscosityComponent, 1.0 / 3.0);
            }

            double viscosityMix = Math.Pow(sumCubeRoot, 3.0);

            Viscosity.SetCalculatedValue(
                viscosityMix,
                ViscosityUnits.cPoise,
                "KendallMonroeRule");
        }


        public void CalculateLiquidMixtureEnthalpy()
        {
            if (Components is null or { Count: 0 })
                return;

            double hMixMass = 0.0;
            double hMixMolar = 0.0;

            foreach (var component in Components)
            {
                double hComponentMass = component.MassEnthalpy.GetValue(MassEnergyUnits.J_Kg);
                double hComponentMolar = component.MolarEnthalpy.GetValue(MolarEnergyUnits.J_Kgmol);

                hMixMass += component.MassFraction * hComponentMass;
                hMixMolar += component.MoleFraction * hComponentMolar;
            }

            // Hook para entalpía de exceso (mezclas no-ideales)
            // TODO: Implementar ActivityModel.CalculateExcessEnthalpy() en el futuro
            double hExcessMolar = 0.0;
            hMixMolar += hExcessMolar;

            MassEnthalpy.SetCalculatedValue(
                hMixMass,
                MassEnergyUnits.J_Kg,
                "IdealMixture");

            MolarEnthalpy.SetCalculatedValue(
                hMixMolar,
                MolarEnergyUnits.J_Kgmol,
                "IdealMixture");
        }

        private double CalculateMixtureMolecularWeight()
        {
            return Components.Sum(c => c.MoleFraction * c.BaseProperties.MolecularWeight);
        }
        public virtual void CalculateBulkProperties()
        {


        }





        private void SetDensityProperties(double molarVolume, double mwMix)
        {
            double molarDensity = 1.0 / molarVolume;
            double massDensity = mwMix / molarVolume;

            MolarDensity.SetCalculatedValue(molarDensity, MolarDensityUnits.Kgmol_m3, "calculationRule");
            MassDensity.SetCalculatedValue(massDensity, MassDensityUnits.Kg_m3, "calculationRule");
        }

        // =========================================================================
        // CONDUCTIVIDAD TÉRMICA DE MEZCLA (Regla lineal molar)
        // =========================================================================
        public void CalculateGasMixtureThermalConductivity()
        {
            if (Components is null or { Count: 0 })
                return;

            double thermalConductivityMix = Components.Sum(c =>
                c.MoleFraction * c.ThermalConductivity.GetValue(ThermalConductivityUnits.W_m_K));

            ThermalConductivity.SetCalculatedValue(
                thermalConductivityMix,
                ThermalConductivityUnits.W_m_K,
                "LinearMolarRule");
        }

        // =========================================================================
        // VISCOSIDAD DE MEZCLA (Kendall-Monroe - Raíz cúbica)
        // =========================================================================
        public void CalculateGasMixtureViscosity()
        {
            if (Components is null or { Count: 0 })
                return;

            double sumCubeRoot = Components
                .Where(c => c.Viscosity.GetValue(ViscosityUnits.cPoise) > ThermodynamicConstants.MinPositiveValue)
                .Sum(c => c.MoleFraction * Math.Pow(c.Viscosity.GetValue(ViscosityUnits.cPoise), 1.0 / 3.0));

            double viscosityMix = Math.Pow(sumCubeRoot, 3.0);

            Viscosity.SetCalculatedValue(
                viscosityMix,
                ViscosityUnits.cPoise,
                "CubeRootRule");
        }

        // =========================================================================
        // CAPACIDAD CALORÍFICA DE MEZCLA (Cp - Regla lineal másica)
        // =========================================================================
        public void CalculateGasMixtureHeatCapacity()
        {
            if (Components is null or { Count: 0 })
                return;

            double cpMixMass = 0.0;
            double mwMix = 0.0;

            foreach (var component in Components)
            {
                double massFraction = component.MassFraction;
                double moleFraction = component.MoleFraction;
                double cpComponent = component.MassHeatCapacity.GetValue(MassEntropyUnits.KJ_Kg_C);

                cpMixMass += massFraction * cpComponent;
                mwMix += moleFraction * component.BaseProperties.MolecularWeight;
            }

            MassHeatCapacity.SetCalculatedValue(
                cpMixMass,
                MassEntropyUnits.KJ_Kg_C,
                "LinearMassRule");

            MolarHeatCapacity.SetCalculatedValue(
                cpMixMass * mwMix,
                MolarEntropyUnits.KJ_Kgmol_C,
                "LinearMolarRule");
        }

        // =========================================================================
        // ENTALPÍA DE MEZCLA (Regla lineal ideal para gases)
        // =========================================================================
        public void CalculateGasMixtureEnthalpy()
        {
            if (Components is null or { Count: 0 })
                return;

            double hMixMass = Components.Sum(c =>
                c.MassFraction * c.MassEnthalpy.GetValue(MassEnergyUnits.J_Kg));

            double hMixMolar = Components.Sum(c =>
                c.MoleFraction * c.MolarEnthalpy.GetValue(MolarEnergyUnits.J_Kgmol));

            MassEnthalpy.SetCalculatedValue(
                hMixMass,
                MassEnergyUnits.J_Kg,
                "IdealGasMixture");

            MolarEnthalpy.SetCalculatedValue(
                hMixMolar,
                MolarEnergyUnits.J_Kgmol,
                "IdealGasMixture");
        }
        //public void CalculateMixtureDensity()
        //{
        //    if (Components is null or { Count: 0 })
        //    {
        //        ResetDensityProperties();
        //        return;
        //    }

        //    double temperatureKelvin = Temperature.Data.GetValue(TemperatureUnits.Kelvin);
        //    double pressureKpa = Pressure.Data.GetValue(PressureUnits.KiloPascal);

        //    if (pressureKpa <= ThermodynamicConstants.MinPositiveValue ||
        //        temperatureKelvin <= ThermodynamicConstants.MinPositiveValue)
        //    {
        //        ResetDensityProperties();
        //        return;
        //    }

        //    // Calcular peso molecular promedio de la mezcla
        //    double mwMix = CalculateMixtureMolecularWeight();

        //    // Determinar ruta de cálculo (Tablas de vapor para agua pura o EoS general)



        //    // Asignar resultados o resetear si el cálculo falló
        //    if (molarVolume > ThermodynamicConstants.MinPositiveValue)
        //    {
        //        SetDensityProperties(molarVolume, mwMix);
        //    }
        //    else
        //    {
        //        ResetDensityProperties();
        //    }
        //}
    }

}
