using Shared.DesignPatterns.NewFolder;
using Shared.Thermodynamics.Components;
using System;
using System.Collections.Generic;
using System.Text;
using UnitSystem;

namespace Shared.DesignPatterns.PureComponents
{
    public interface IPropertyEvaluator
    {
        // El método central que usará el simulador
        Amount EvaluateAt(Amount temperature);
    }
    public class ExtendedAntoineEvaluator : IPropertyEvaluator
    {
        private readonly CorrelationCoefficientsDto _coeffs;
        public ExtendedAntoineEvaluator(CorrelationCoefficientsDto coeffs) => _coeffs = coeffs;

        public Amount EvaluateAt(Amount temperature)
        {
            // TODO: Ecuación extendida de Antoine (Exp(C1 + C2/(C3+T) + ...))
            return new Amount(0.0, PressureUnits.Bar);
        }
    }

    public class WaterVaporPressureEvaluator : IPropertyEvaluator
    {
        private readonly Amount _criticalPressure;
        private readonly Amount _criticalTemperature;
        public WaterVaporPressureEvaluator(Amount pc, Amount tc) { _criticalPressure = pc; _criticalTemperature = tc; }

        public Amount EvaluateAt(Amount temperature)
        {
            // TODO: Lógica límite Tc y llamada a CPropiAgua.pSatW(T)
            return new Amount(0.0, PressureUnits.Bar);
        }
    }
    public class DipprHeatOfVaporizationEvaluator : IPropertyEvaluator
    {
        private readonly CorrelationCoefficientsDto _coeffs;
        private readonly Amount _criticalTemperature;
        public DipprHeatOfVaporizationEvaluator(CorrelationCoefficientsDto coeffs, Amount tc) { _coeffs = coeffs; _criticalTemperature = tc; }

        public Amount EvaluateAt(Amount temperature)
        {
            // TODO: Ecuación de Watson o DIPPR (C1 * (1 - Tr)^(C2 + C3*Tr + ...))
            return new Amount(0.0, MolarEnergyUnits.J_Kgmol);
        }
    }

    public class WaterHeatOfVaporizationEvaluator : IPropertyEvaluator
    {
        public Amount EvaluateAt(Amount temperature)
        {
            // TODO: Llamada a CPropiAgua para entalpía de vaporización (Hvap = Hvap_gas - Hvap_liq)
            return new Amount(0.0, MolarEnergyUnits.J_Kgmol);
        }
    }// ---- LÍQUIDO ----
    public class PolynomialLiquidCpEvaluator : IPropertyEvaluator
    {
        private readonly CorrelationCoefficientsDto _coeffs;
        public PolynomialLiquidCpEvaluator(CorrelationCoefficientsDto coeffs) => _coeffs = coeffs;

        public Amount EvaluateAt(Amount temperature)
        {
            // TODO: Ecuación polinomial para Cp líquido (C1 + C2*T + C3*T^2 + ...)
            return new Amount(0.0, MolarEntropyUnits.KJ_Kgmol_C);
        }
    }

    public class WaterLiquidCpEvaluator : IPropertyEvaluator
    {
        public Amount EvaluateAt(Amount temperature)
        {
            // TODO: Llamada a CPropiAgua.cpSatLiqW(T)
            return new Amount(0.0, MassEntropyUnits.KJ_Kg_C); // Asegúrate de unificar unidades a Molar o Másico
        }
    }

    // ---- GAS ----
    public class AlyLeeGasCpEvaluator : IPropertyEvaluator
    {
        private readonly CorrelationCoefficientsDto _coeffs;
        public AlyLeeGasCpEvaluator(CorrelationCoefficientsDto coeffs) => _coeffs = coeffs;

        public Amount EvaluateAt(Amount temperature)
        {
            // TODO: Ecuación de Aly-Lee para gas ideal (C1 + C2*exp(-C3/T) + ...)
            return new Amount(0.0, MolarEntropyUnits.KJ_Kgmol_C);
        }
    }

    public class WaterGasCpEvaluator : IPropertyEvaluator
    {
        public Amount EvaluateAt(Amount temperature)
        {
            // TODO: Llamada a CPropiAgua para Cp del vapor
            return new Amount(0.0, MassEntropyUnits.KJ_Kg_C);
        }
    }// ---- LÍQUIDO ----
    public class AndradeLiquidViscosityEvaluator : IPropertyEvaluator
    {
        private readonly CorrelationCoefficientsDto _coeffs;
        public AndradeLiquidViscosityEvaluator(CorrelationCoefficientsDto coeffs) => _coeffs = coeffs;

        public Amount EvaluateAt(Amount temperature)
        {
            // TODO: Ecuación de Andrade / DIPPR (Exp(C1 + C2/T + C3*ln(T) + ...))
            return new Amount(0.0, ViscosityUnits.cPoise);
        }
    }

    public class WaterLiquidViscosityEvaluator : IPropertyEvaluator
    {
        public Amount EvaluateAt(Amount temperature)
        {
            // TODO: Llamada a CPropiAgua para viscosidad del líquido
            return new Amount(0.0, ViscosityUnits.cPoise);
        }
    }

    // ---- GAS ----
    public class DipprGasViscosityEvaluator : IPropertyEvaluator
    {
        private readonly CorrelationCoefficientsDto _coeffs;
        public DipprGasViscosityEvaluator(CorrelationCoefficientsDto coeffs) => _coeffs = coeffs;

        public Amount EvaluateAt(Amount temperature)
        {
            // TODO: Ecuación DIPPR para viscosidad de gas (C1*T^C2 / (1 + C3/T + C4/T^2))
            return new Amount(0.0, ViscosityUnits.cPoise);
        }
    }

    public class WaterGasViscosityEvaluator : IPropertyEvaluator
    {
        public Amount EvaluateAt(Amount temperature)
        {
            // TODO: Llamada a CPropiAgua para viscosidad del gas
            return new Amount(0.0, ViscosityUnits.cPoise);
        }
    }// ---- LÍQUIDO ----
    public class PolynomialLiquidThermalCondEvaluator : IPropertyEvaluator
    {
        private readonly CorrelationCoefficientsDto _coeffs;
        public PolynomialLiquidThermalCondEvaluator(CorrelationCoefficientsDto coeffs) => _coeffs = coeffs;

        public Amount EvaluateAt(Amount temperature)
        {
            // TODO: Ecuación polinomial (C1 + C2*T + C3*T^2)
            return new Amount(0.0, ThermalConductivityUnits.W_m_K);
        }
    }

    public class WaterLiquidThermalCondEvaluator : IPropertyEvaluator
    {
        public Amount EvaluateAt(Amount temperature)
        {
            // TODO: Llamada a CPropiAgua
            return new Amount(0.0, ThermalConductivityUnits.W_m_K);
        }
    }

    // ---- GAS ----
    public class PolynomialGasThermalCondEvaluator : IPropertyEvaluator
    {
        private readonly CorrelationCoefficientsDto _coeffs;
        public PolynomialGasThermalCondEvaluator(CorrelationCoefficientsDto coeffs) => _coeffs = coeffs;

        public Amount EvaluateAt(Amount temperature)
        {
            // TODO: Ecuación polinomial o DIPPR para conductividad de gas
            return new Amount(0.0, ThermalConductivityUnits.W_m_K);
        }
    }

    public class WaterGasThermalCondEvaluator : IPropertyEvaluator
    {
        public Amount EvaluateAt(Amount temperature)
        {
            // TODO: Llamada a CPropiAgua
            return new Amount(0.0, ThermalConductivityUnits.W_m_K);
        }
    }
    public class RackettLiquidDensityEvaluator : IPropertyEvaluator
    {
        private readonly CorrelationCoefficientsDto _coeffs;
        public RackettLiquidDensityEvaluator(CorrelationCoefficientsDto coeffs) => _coeffs = coeffs;

        public Amount EvaluateAt(Amount temperature)
        {
            // TODO: Ecuación de Rackett o DIPPR 105 (C1 / C2^(1 + (1 - T/C3)^C4))
            return new Amount(0.0, MassDensityUnits.Kg_m3); // O molar, ajusta según tu base
        }
    }

    public class WaterLiquidDensityEvaluator : IPropertyEvaluator
    {
        public Amount EvaluateAt(Amount temperature)
        {
            // TODO: Llamada a CPropiAgua.densSatLiqTW(T)
            return new Amount(0.0, MassDensityUnits.Kg_m3);
        }
    }

    public class DipprSurfaceTensionEvaluator : IPropertyEvaluator
    {
        private readonly CorrelationCoefficientsDto _coeffs;
        private readonly Amount _criticalTemperature;
        public DipprSurfaceTensionEvaluator(CorrelationCoefficientsDto coeffs, Amount tc) { _coeffs = coeffs; _criticalTemperature = tc; }

        public Amount EvaluateAt(Amount temperature)
        {
            // TODO: Ecuación DIPPR de Tensión Superficial (C1 * (1 - Tr)^(C2 + C3*Tr...))
            return new Amount(0.0, SurfaceTensionUnits.N_m);
        }
    }

    public class WaterSurfaceTensionEvaluator : IPropertyEvaluator
    {
        public Amount EvaluateAt(Amount temperature)
        {
            // TODO: Ecuación IAPWS o librería para tensión superficial del agua pura
            return new Amount(0.0, SurfaceTensionUnits.N_m);
        }
    }
}
