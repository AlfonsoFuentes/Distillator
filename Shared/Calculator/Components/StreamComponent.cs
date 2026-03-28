using Shared.Calculator.MaterialStreams;
using Shared.Thermodynamics.Components;
using Shared.Thermodynamics.WaterProperties.Server.Thermodynamics.Engines;
using System;
using System.Collections.Generic;
using System.Text;
using UnitSystem;

namespace Shared.Calculator.Components
{
    public class StreamComponent
    {
        // =========================================================================
        // EL ADN (Constantes, Tc, Pc, Correlaciones)
        // =========================================================================
        public ChemicalComponentDto BaseProperties { get; private set; }

        // =========================================================================
        // CONCENTRACIONES GLOBALES (Zi)
        // =========================================================================
        public double MoleFraction { get; set; }
        public double MassFraction { get; set; }

        public double CompressibilityFactor { get; set; }
        // =========================================================================
        // FLUJOS PARCIALES
        // =========================================================================
        public Amount MolarFlow { get; private set; }
        public Amount MassFlow { get; private set; }
        public Amount VolumetricFlow { get; private set; }

        // =========================================================================
        // ESTADO DEL SISTEMA
        // =========================================================================
        public Amount Temperature { get; set; }
        public Amount Pressure { get; set; }
        public Amount SaturationPressure { get; set; }
        public Amount SaturationTemperature { get; set; }

        // =========================================================================
        // PROPIEDADES INTENSIVAS (Se calculan en LiquidComponent o VaporComponent)
        // =========================================================================
        public Amount MassDensity { get; set; }
        public Amount MolarDensity { get; set; }
        public Amount MassVolume { get; set; }
        public Amount MolarVolume { get; set; }
        public Amount MassEnthalpy { get; set; }
        public Amount MolarEnthalpy { get; set; }
        public Amount MassHeatCapacity { get; set; }
        public Amount MolarHeatCapacity { get; set; }
        public Amount Viscosity { get; set; }
        public Amount ThermalConductivity { get; set; }

        public Amount SurfaceTension { get; set; }
        public Amount MassEnthalpyOfVaporization { get; set; }
        public Amount MolarEnthalpyOfVaporization { get; set; }
        public EosParameters EosParams { get; protected set; }
        public EosParameters EosParamsSatured { get; private set; }

        // =========================================================================
        // CONSTRUCTOR
        // =========================================================================
        public StreamComponent(ChemicalComponentDto dto)
        {
            BaseProperties = dto ?? throw new ArgumentNullException(nameof(dto));

            MoleFraction = 0.0;
            MassFraction = 0.0;

            MolarFlow = new Amount(0.0, MolarFlowUnits.Kgmol_hr);
            MassFlow = new Amount(0.0, MassFlowUnits.Kg_hr);
            VolumetricFlow = new Amount(0.0, VolumetricFlowUnits.m3_hr);
            Temperature = new Amount(0.0, TemperatureUnits.DegreeCelcius);
            Pressure = new Amount(0.0, PressureUnits.psi);
            SaturationPressure = new Amount(0.0, PressureUnits.Bar);
            SaturationTemperature = new Amount(0.0, TemperatureUnits.DegreeCelcius);
            EosParams = new EosParameters();
            EosParamsSatured = new EosParameters();
            MassDensity = new Amount(0.0, MassDensityUnits.Kg_m3);
            MolarDensity = new Amount(0.0, MolarDensityUnits.Kgmol_m3);
            MassVolume = new Amount(0.0, MassVolumeSpecificUnits.m3_Kg);
            MolarVolume = new Amount(0.0, MolarVolumeSpecificUnits.m3_Kgmol);
            MassEnthalpy = new Amount(0.0, MassEnergyUnits.Kcal_Kg);
            MolarEnthalpy = new Amount(0.0, MolarEnergyUnits.Kcal_Kgmol);
            MassHeatCapacity = new Amount(0.0, MassEntropyUnits.Kcal_Kg_C);
            MolarHeatCapacity = new Amount(0.0, MolarEntropyUnits.Kcal_Kgmol_C);
            Viscosity = new Amount(0.0, ViscosityUnits.cPoise);
            ThermalConductivity = new Amount(0.0, ThermalConductivityUnits.W_m_K);
            MassEnthalpyOfVaporization = new Amount(0.0, MassEnergyUnits.Kcal_Kg);
            MolarEnthalpyOfVaporization = new Amount(0.0, MolarEnergyUnits.Kcal_Kgmol);
            SurfaceTension = new Amount(0, SurfaceTensionUnits.N_m);
        }

        // =========================================================================
        // ORQUESTADOR DE PROPIEDADES SEGÚN ESTADO TERMODINÁMICO
        // =========================================================================
        public void CalculateIntensiveProperties(ThermodynamicState state)
        {
            switch (state)
            {
                case ThermodynamicState.SubcooledLiquid:
                case ThermodynamicState.SaturatedLiquid:
                    // 100% Líquido: Usamos solo las ecuaciones de líquido
                    CalculateLiquidDensity();
                    CalculateLiquidHeatCapacity();
                    CalculateLiquidViscosity();
                    CalculateLiquidThermalConductivity();
                    CalculateSurfaceTension();
                    CalculateLiquidEnthalpy();

                    // Es útil tener el calor de vaporización calculado si estamos justo en la saturación
                    if (state == ThermodynamicState.SaturatedLiquid)
                    {
                        CalculateHeatOfVaporization();
                    }
                    break;

                case ThermodynamicState.SaturatedVapor:
                case ThermodynamicState.SuperheatedVapor:
                    // 100% Vapor: Usamos solo las ecuaciones de gas
                    CalculateGasDensity();
                    CalculateGasHeatCapacity();
                    CalculateGasViscosity();
                    CalculateGasThermalConductivity();
                    CalculateHeatOfVaporization();
                    CalculateGasEnthalpy();
                    break;

                case ThermodynamicState.VaporLiquidMixture:
                    // ⚠️ ATENCIÓN AQUÍ: 
                    // Si es mezcla, NO podemos llamar a ambos métodos seguidos porque 
                    // sobrescribirían la misma variable (ej. MassDensity).
                    // En este estado, la MaterialStream NO debe llamar a este método global,
                    // sino delegar a sus listas internas de LiquidPhase y VaporPhase, 
                    // y luego aplicar las reglas de mezclado (ej. DensidadMix = V * DensGas + L * DensLiq).
                    break;

                case ThermodynamicState.Undefined:
                default:
                    // El estado no ha sido calculado aún, no hacemos nada para evitar basura matemática.
                    break;
            }
        }
        public Amount GetSaturedPressureAtTemperature(Amount temp)
        {
            var pv = BaseProperties.VaporPressure;

            double tempKelvin = temp.GetValue(TemperatureUnits.Kelvin);
            double tMin = pv.Tmin.GetValue(TemperatureUnits.Kelvin);
            double tMax = pv.Tmax.GetValue(TemperatureUnits.Kelvin);
            double tc = BaseProperties.CriticalTemperature.GetValue(TemperatureUnits.Kelvin);

            if (BaseProperties.Name == "Agua" || BaseProperties.Name == "Water")
            {
                if (tempKelvin >= tc)
                {
                    return BaseProperties.CriticalPressure;
                }
                else
                {
                    double pBar = CPropiAgua.pSatW(tempKelvin);
                    return new Amount(pBar, PressureUnits.Bar);
                }
            }
            else
            {
                double tCalc = Math.Clamp(tempKelvin, tMin, tMax);

                double a = pv.C1 + (pv.C2 / (pv.C3 + tCalc)) + (pv.C4 * tCalc) +
                           (pv.C5 * Math.Log(tCalc)) + (pv.C6 * Math.Pow(tCalc, pv.C7));

                double resultBar = Math.Exp(a);

                return new Amount(resultBar, PressureUnits.Bar);
            }
        }

        // =========================================================================
        // CÁLCULO DE TEMPERATURA DE SATURACIÓN (Tsat a P dada)
        // =========================================================================
        public Amount GetSaturedTemperatureAtPressure(Amount _pressure)
        {
            double t = 0;
            double pvKpa = 0;
            double pvantKpa = 0;
            double tant = 0;
            bool formula = false;
            bool para = false;

            double pTargetKpa = _pressure.GetValue(PressureUnits.KiloPascal);

            var pvProp = BaseProperties.VaporPressure;
            double tMin = pvProp.Tmin.GetValue(TemperatureUnits.Kelvin);
            double tMax = pvProp.Tmax.GetValue(TemperatureUnits.Kelvin);
            double pcKpa = BaseProperties.CriticalPressure.GetValue(PressureUnits.KiloPascal);
            double tc = BaseProperties.CriticalTemperature.GetValue(TemperatureUnits.Kelvin);

            if (BaseProperties.Name == "Agua" || BaseProperties.Name == "Water")
            {
                double pBar = _pressure.GetValue(PressureUnits.Bar);
                t = CPropiAgua.tSatW(pBar);
                return new Amount(t, TemperatureUnits.Kelvin);
            }

            if (pTargetKpa >= pcKpa)
            {
                t = tc;
            }
            else if (pTargetKpa > 0)
            {
                t = tMax;

                do
                {
                    Amount tempIteracion = new Amount(t, TemperatureUnits.Kelvin);
                    Amount pSatCalculada = GetSaturedPressureAtTemperature(tempIteracion);
                    pvKpa = pSatCalculada.GetValue(PressureUnits.KiloPascal);

                    if (formula)
                    {
                        if (t <= tMin)
                        {
                            t = tMin;
                            para = true;
                        }
                        else if (Math.Abs(pTargetKpa - pvKpa) < 1e-3)
                        {
                            para = true;
                        }
                        else
                        {
                            double m = (t - tant) / (pvKpa - pvantKpa);
                            double b = t - m * pvKpa;

                            if (Math.Abs(pvKpa - pvantKpa) < 1e-9)
                            {
                                t = tMin;
                            }
                            else
                            {
                                pvantKpa = pvKpa;
                                tant = t;
                                t = m * pTargetKpa + b;
                            }
                        }
                    }
                    else
                    {
                        formula = true;
                        tant = t;
                        t -= 1.0;
                        pvantKpa = pvKpa;
                    }

                } while (!para);
            }

            if (t > 0)
            {
                return new Amount(t, TemperatureUnits.Kelvin);
            }
            return new Amount(tc, TemperatureUnits.Kelvin);
        }

        protected void CalculateSurfaceTension()
        {
            double tempK = Temperature.GetValue(TemperatureUnits.Kelvin);
            var sigmaCoeff = BaseProperties.SurfaceTension;

            double tMin = sigmaCoeff.Tmin.GetValue(TemperatureUnits.Kelvin);
            double tMax = sigmaCoeff.Tmax.GetValue(TemperatureUnits.Kelvin);
            double tc = BaseProperties.CriticalTemperature.GetValue(TemperatureUnits.Kelvin);

            double C1 = sigmaCoeff.C1;
            double C2 = sigmaCoeff.C2;
            double C3 = sigmaCoeff.C3;
            double C4 = sigmaCoeff.C4;
            double C5 = sigmaCoeff.C5;

            double surfaceTension = 0.0;

            if (tempK >= tMin && tempK <= tMax && tc > 0)
            {
                double Tr = tempK / tc;
                double exponente = C2 + C3 * Tr + C4 * Tr * Tr + C5 * Math.Pow(Tr, 3);
                surfaceTension = C1 * Math.Pow(1.0 - Tr, exponente);
            }

            SurfaceTension.SetValue(surfaceTension, SurfaceTensionUnits.N_m);
        }
        protected void CalculateLiquidDensity()
        {
            double tempK = Temperature.GetValue(TemperatureUnits.Kelvin);
            var densityCoeff = BaseProperties.Density;

            double tMin = densityCoeff.Tmin.GetValue(TemperatureUnits.Kelvin);
            double tMax = densityCoeff.Tmax.GetValue(TemperatureUnits.Kelvin);
            double tc = BaseProperties.CriticalTemperature.GetValue(TemperatureUnits.Kelvin);
            double mw = BaseProperties.MolecularWeight;

            double C1 = densityCoeff.C1;
            double C2 = densityCoeff.C2;
            double C3 = densityCoeff.C3;
            double C4 = densityCoeff.C4;
            double C5 = densityCoeff.C5;

            double density = 0.0;
            string formula = BaseProperties.Formula.ToLower();

            if (formula == "h2o")
            {
                density = CPropiAgua.densSatLiqTW(tempK);
            }
            else
            {
                if (tempK >= tMin && tempK <= tMax)
                {
                    double B = 1.0 + Math.Pow(1.0 - tempK / C3, C4);
                    double A = Math.Pow(C2, B);
                    density = (C1 / A) * mw;
                }
            }

            MassDensity.SetValue(density, MassDensityUnits.Kg_m3);
            MolarDensity.SetValue(density / mw, MolarDensityUnits.Kgmol_m3);
        }
        protected void CalculateLiquidHeatCapacity()
        {
            double tempK = Temperature.GetValue(TemperatureUnits.Kelvin);
            var cpCoeff = BaseProperties.LiquidHeatCapacity;

            double tMin = cpCoeff.Tmin.GetValue(TemperatureUnits.Kelvin);
            double tMax = cpCoeff.Tmax.GetValue(TemperatureUnits.Kelvin);
            double mw = BaseProperties.MolecularWeight;

            double C1 = cpCoeff.C1;
            double C2 = cpCoeff.C2;
            double C3 = cpCoeff.C3;
            double C4 = cpCoeff.C4;
            double C5 = cpCoeff.C5;

            double cpMass = 0.0;
            string formula = BaseProperties.Formula.ToLower();

            if (formula == "h2o")
            {
                cpMass = CPropiAgua.cpSatLiqTW(tempK);
            }
            else
            {
                if (tempK >= tMin && tempK <= tMax)
                {
                    cpMass = C1 + C2 * tempK + C3 * Math.Pow(tempK, 2) +
                              C4 * Math.Pow(tempK, 3) + C5 * Math.Pow(tempK, 4);
                    cpMass /= mw;
                    cpMass /= 1000;
                }
            }

            double cpMolar = (cpMass * mw);

            MassHeatCapacity.SetValue(cpMass, MassEntropyUnits.KJ_Kg_C);
            MolarHeatCapacity.SetValue(cpMolar, MolarEntropyUnits.KJ_Kgmol_C);
        }

        protected void CalculateLiquidViscosity()
        {
            double tempK = Temperature.GetValue(TemperatureUnits.Kelvin);
            var viscCoeff = BaseProperties.LiquidViscosity;

            double tMin = viscCoeff.Tmin.GetValue(TemperatureUnits.Kelvin);
            double tMax = viscCoeff.Tmax.GetValue(TemperatureUnits.Kelvin);

            double C1 = viscCoeff.C1;
            double C2 = viscCoeff.C2;
            double C3 = viscCoeff.C3;
            double C4 = viscCoeff.C4;
            double C5 = viscCoeff.C5;

            double viscosity = 0.0;
            string formula = BaseProperties.Formula.ToLower();

            if (formula == "h2o")
            {
                viscosity = CPropiAgua.viscSatLiqTW(tempK);
            }
            else
            {
                if (tempK >= tMin && tempK <= tMax)
                {
                    double v1 = C1 + C2 / tempK + C3 * Math.Log(tempK) + C4 * Math.Pow(tempK, C5);
                    viscosity = Math.Exp(v1);
                }
            }

            Viscosity.SetValue(viscosity, ViscosityUnits.Pa_s);
        }

        protected void CalculateLiquidThermalConductivity()
        {
            double tempK = Temperature.GetValue(TemperatureUnits.Kelvin);
            var condCoeff = BaseProperties.LiquidThermalCond;

            double tMin = condCoeff.Tmin.GetValue(TemperatureUnits.Kelvin);
            double tMax = condCoeff.Tmax.GetValue(TemperatureUnits.Kelvin);

            double C1 = condCoeff.C1;
            double C2 = condCoeff.C2;
            double C3 = condCoeff.C3;
            double C4 = condCoeff.C4;
            double C5 = condCoeff.C5;

            double conductivity = 0.0;
            string formula = BaseProperties.Formula.ToLower();

            if (formula == "h2o")
            {
                conductivity = CPropiAgua.thconSatLiqTW(tempK);
            }
            else
            {
                if (tempK >= tMin && tempK <= tMax)
                {
                    conductivity = C1 + C2 * tempK + C3 * Math.Pow(tempK, 2) +
                                  C4 * Math.Pow(tempK, 3) + C5 * Math.Pow(tempK, 4);
                }
            }

            ThermalConductivity.SetValue(conductivity, ThermalConductivityUnits.W_m_K);
        }

        protected void CalculateGasDensity()
        {
            double tempK = Temperature.GetValue(TemperatureUnits.Kelvin);
            double pKpa = Pressure.GetValue(PressureUnits.KiloPascal);
            double mw = BaseProperties.MolecularWeight;
            double z = CompressibilityFactor;

            const double R_Gas = 8.314472;

            // ρ = P × MW / (Z × R × T)
            double density = (pKpa * mw) / (z * R_Gas * tempK);

            MassDensity.SetValue(density, MassDensityUnits.Kg_m3);
            MolarDensity.SetValue(density / mw, MolarDensityUnits.Kgmol_m3);
        }
        protected void CalculateGasHeatCapacity()
        {
            double tempK = Temperature.GetValue(TemperatureUnits.Kelvin);
            var cpCoeff = BaseProperties.GasHeatCapacity;

            double tMin = cpCoeff.Tmin.GetValue(TemperatureUnits.Kelvin);
            double tMax = cpCoeff.Tmax.GetValue(TemperatureUnits.Kelvin);
            double mw = BaseProperties.MolecularWeight;

            double C1 = cpCoeff.C1;
            double C2 = cpCoeff.C2;
            double C3 = cpCoeff.C3;
            double C4 = cpCoeff.C4;
            double C5 = cpCoeff.C5;

            double cpMass = 0.0;
            string formula = BaseProperties.Formula.ToLower();

            if (formula == "h2o")
            {
                if (Temperature > SaturationTemperature)
                {
                    //Vapor sobrecalentado
                    double pBar = Pressure.GetValue(PressureUnits.Bar);
                    cpMass = CPropiAgua.cpW(tempK, pBar) * 1000;

                }
                else
                {
                    cpMass = CPropiAgua.cpSatVapTW(tempK) * 1000;
                }

            }
            else
            {
                if (tempK >= tMin && tempK <= tMax)
                {
                    double v1 = Math.Pow((C3 / tempK) / Math.Sinh(C3 / tempK), 2);
                    double v2 = Math.Pow((C5 / tempK) / Math.Cosh(C5 / tempK), 2);
                    cpMass = C1 + C2 * v1 + C4 * v2;
                    cpMass /= mw;
                }
            }

            double cpMolar = (cpMass * mw);

            MassHeatCapacity.SetValue(cpMass, MassEntropyUnits.KJ_Kg_C);
            MolarHeatCapacity.SetValue(cpMolar, MolarEntropyUnits.KJ_Kgmol_C);
        }
        protected void CalculateGasViscosity()
        {
            double tempK = Temperature.GetValue(TemperatureUnits.Kelvin);
            var viscCoeff = BaseProperties.GasViscosity;

            double tMin = viscCoeff.Tmin.GetValue(TemperatureUnits.Kelvin);
            double tMax = viscCoeff.Tmax.GetValue(TemperatureUnits.Kelvin);

            double C1 = viscCoeff.C1;
            double C2 = viscCoeff.C2;
            double C3 = viscCoeff.C3;
            double C4 = viscCoeff.C4;
            double C5 = viscCoeff.C5;

            double viscosity = 0.0;
            string formula = BaseProperties.Formula.ToLower();

            if (formula == "h2o")
            {
                if (Temperature > SaturationTemperature)
                {
                    //Vapor sobrecalentado
                    double pBar = Pressure.GetValue(PressureUnits.Bar);
                    viscosity = CPropiAgua.viscW(tempK, pBar);

                }
                else
                {
                    viscosity = CPropiAgua.viscSatVapTW(tempK);
                }

            }
            else
            {
                if (tempK >= tMin && tempK <= tMax)
                {
                    double numerador = C1 * Math.Pow(tempK, C2);
                    double denominador = 1 + C3 / tempK + C4 / Math.Pow(tempK, 2.0);
                    viscosity = numerador / denominador;
                }
            }

            Viscosity.SetValue(viscosity, ViscosityUnits.Pa_s);
        }
        protected void CalculateGasThermalConductivity()
        {
            double tempK = Temperature.GetValue(TemperatureUnits.Kelvin);
            var condCoeff = BaseProperties.GasThermalCond;

            double tMin = condCoeff.Tmin.GetValue(TemperatureUnits.Kelvin);
            double tMax = condCoeff.Tmax.GetValue(TemperatureUnits.Kelvin);

            double C1 = condCoeff.C1;
            double C2 = condCoeff.C2;
            double C3 = condCoeff.C3;
            double C4 = condCoeff.C4;
            double C5 = condCoeff.C5;

            double conductivity = 0.0;
            string formula = BaseProperties.Formula.ToLower();

            if (formula == "h2o")
            {
                if (Temperature > SaturationTemperature)
                {
                    //Vapor sobrecalentado
                    double pBar = Pressure.GetValue(PressureUnits.Bar);
                    conductivity = CPropiAgua.thconW(tempK, pBar);

                }
                else
                {
                    conductivity = CPropiAgua.thconSatVapTW(tempK);
                }

            }
            else
            {
                if (tempK >= tMin && tempK <= tMax)
                {
                    double numerador = C1 * Math.Pow(tempK, C2);
                    double denominador = 1 + C3 / tempK + C4 / Math.Pow(tempK, 2.0);
                    conductivity = numerador / denominador;
                }
            }

            ThermalConductivity.SetValue(conductivity, ThermalConductivityUnits.W_m_K);
        }
        // =========================================================================
        // CALOR DE VAPORIZACIÓN (Entalpía de Vaporización)
        // =========================================================================
        public void CalculateHeatOfVaporization()
        {
            double tempK = Temperature.GetValue(TemperatureUnits.Kelvin);
            var hvapCoeff = BaseProperties.HeatOfVaporization;

            double tMin = hvapCoeff.Tmin.GetValue(TemperatureUnits.Kelvin);
            double tMax = hvapCoeff.Tmax.GetValue(TemperatureUnits.Kelvin);
            double tc = BaseProperties.CriticalTemperature.GetValue(TemperatureUnits.Kelvin);
            double mw = BaseProperties.MolecularWeight;

            double C1 = hvapCoeff.C1;
            double C2 = hvapCoeff.C2;
            double C3 = hvapCoeff.C3;
            double C4 = hvapCoeff.C4;
            double C5 = hvapCoeff.C5;

            string formula = BaseProperties.Formula.ToLower();
            double hvapMass = 0.0; // Resultado en J/kg

            // 1. Caso Especial: Agua
            if (formula == "h2o")
            {
                // Asumiendo que CPropiAgua devuelve la entalpía en kJ/kg,
                // multiplicamos por 1000 para estandarizar a J/kg (igual que tu C++)
                double hf = CPropiAgua.enthalpySatVapTW(tempK);
                double hg = CPropiAgua.enthalpySatLiqTW(tempK);
                hvapMass = (hf - hg) * 1000.0;
            }
            // 2. Caso General: Ecuación DIPPR 106
            else if (tempK > tMin && tempK < tMax && tc > 0)
            {
                double tr = tempK / tc;

                // Cálculo del exponente polinomial
                double exponente = C2 + C3 * tr + C4 * tr * tr + C5 * Math.Pow(tr, 3.0);

                // Resultado DIPPR en J/kmol
                double hvapMolar = C1 * Math.Pow(1.0 - tr, exponente);

                // Convertimos a base másica (J/kg)
                hvapMass = hvapMolar / mw;
            }

            // 3. Guardar en el objeto Amount
            if (hvapMass != 0.0)
            {

                MassEnthalpyOfVaporization.SetValue(hvapMass, MassEnergyUnits.J_Kg);
                MolarEnthalpyOfVaporization.SetValue(hvapMass * mw, MolarEnergyUnits.J_Kgmol);
            }
        }  // =========================================================================
           // ENTALPÍA DE GAS IDEAL (Integral de Aly-Lee)
           // =========================================================================
        public void CalculateLiquidEnthalpy()
        {
            double tempK = Temperature.GetValue(TemperatureUnits.Kelvin);
            double hMolarJ_kmol = CalculatePureLiquidEnthalpy(tempK);

            double mw = BaseProperties.MolecularWeight;
            double hMassJ_kg = (hMolarJ_kmol / mw);

            MassEnthalpy.SetValue(hMassJ_kg, MassEnergyUnits.J_Kg);
            MolarEnthalpy.SetValue(hMassJ_kg * mw, MolarEnergyUnits.J_Kgmol);
        }
        public double CalculatePureLiquidEnthalpy(double targetTempK)
        {
            string formula = BaseProperties.Formula.ToLower();
            if (formula == "h2o" || formula == "agua")
            {
                // CPropiAgua devuelve kJ/kg. Lo pasamos a J/kmol
                return CPropiAgua.enthalpySatLiqTW(targetTempK) * 1000.0 * BaseProperties.MolecularWeight;
            }

            var cp = BaseProperties.LiquidHeatCapacity;
            double tRef = 298.15 - 25; // 0 °C (Referencia termodinámica estándar)

            // Limitar la temperatura a los rangos de la correlación
            double tCalc = Math.Clamp(targetTempK, cp.Tmin.GetValue(TemperatureUnits.Kelvin), cp.Tmax.GetValue(TemperatureUnits.Kelvin));

            // Función matemática: Integral de C1 + C2*T + C3*T^2 + C4*T^3 + C5*T^4
            double IntegralCpL(double t) =>
                cp.C1 * t +
                (cp.C2 / 2.0) * Math.Pow(t, 2) +
                (cp.C3 / 3.0) * Math.Pow(t, 3) +
                (cp.C4 / 4.0) * Math.Pow(t, 4) +
                (cp.C5 / 5.0) * Math.Pow(t, 5);

            // H = Integral(T) - Integral(T_ref)
            return IntegralCpL(tCalc) - IntegralCpL(tRef);
        }
        public void CalculateGasEnthalpy()
        {
            double tSysK = Temperature.GetValue(TemperatureUnits.Kelvin);
            double tSatK = SaturationTemperature.GetValue(TemperatureUnits.Kelvin);

            string formula = BaseProperties.Formula.ToLower();
            double hMolarJ_kmol = 0.0;

            if (formula == "h2o" || formula == "agua")
            {
                if (tSysK > tSatK)
                {
                    double pBar = Pressure.GetValue(PressureUnits.Bar);
                    // Asumiendo que enthalpyW da la entalpía del vapor sobrecalentado en kJ/kg
                    hMolarJ_kmol = CPropiAgua.enthalpyW(tSysK, pBar) * 1000.0 * BaseProperties.MolecularWeight;
                }
                else
                {
                    hMolarJ_kmol = CPropiAgua.enthalpySatVapTW(tSysK) * 1000.0 * BaseProperties.MolecularWeight;
                }
            }
            else
            {
                // PASO 1: Calentar el líquido hasta la temperatura de saturación
                // (Llamamos a la misma ecuación matemática que usaría el líquido)
                double hLiquidSat = CalculatePureLiquidEnthalpy(tSatK);

                // PASO 2: Evaporar a la temperatura de saturación (Ecuación DIPPR)
                double hVap = MolarEnthalpyOfVaporization.GetValue(MolarEnergyUnits.J_Kgmol);

                // Entalpía del vapor saturado
                hMolarJ_kmol = hLiquidSat + hVap;

                // PASO 3: Sobrecalentar el vapor (Integral Aly-Lee) si T_sistema > T_sat
                if (tSysK > tSatK)
                {
                    double hSuperheat = CalculateAlyLeeIntegral(tSatK, tSysK);
                    hMolarJ_kmol += hSuperheat;
                }
            }

            double mw = BaseProperties.MolecularWeight;
            double hMassJ_kg = (hMolarJ_kmol / mw);

            MassEnthalpy.SetValue(hMassJ_kg, MassEnergyUnits.J_Kg);
            MolarEnthalpy.SetValue(hMassJ_kg * mw, MolarEnergyUnits.J_Kgmol);
        }
        private double CalculateAlyLeeIntegral(double tInicio, double tFin)
        {
            var cp = BaseProperties.GasHeatCapacity;

            double IntegralCpG(double t)
            {
                double coth = 1.0 / Math.Tanh(cp.C3 / t);
                double tanh = Math.Tanh(cp.C5 / t);
                return cp.C1 * t + cp.C2 * cp.C3 * coth - cp.C4 * cp.C5 * tanh;
            }

            return IntegralCpG(tFin) - IntegralCpG(tInicio);
        }
    }
}
