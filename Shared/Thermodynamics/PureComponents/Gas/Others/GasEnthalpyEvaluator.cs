using Shared.PropertiesDtos.Components;
using Shared.Thermodynamics.PureComponents;
using UnitSystem;

namespace Shared.Thermodynamics.PureComponents.Gas.Others
{
    // ============================================================
    // GAS ENTHALPY (solo T - casos estándar)
    // ============================================================
    public class GasEnthalpyEvaluator : IPropertyEvaluator<Temperature, MolarEnergy>
    {
        private readonly CorrelationCoefficientsDto _cpGasCoeffs;
        private readonly CorrelationCoefficientsDto _cpLiquidCoeffs;
        private readonly IPropertyEvaluator<Temperature, MolarEnergy> _hvapEvaluator;
        private readonly IPropertyEvaluator<Pressure, Temperature> _tsatEvaluator;
        private readonly Pressure _operatingPressure;
        private readonly double _molecularWeight;
        private const double Tref = 273.15;

        public GasEnthalpyEvaluator(
            CorrelationCoefficientsDto cpGasCoeffs,
            CorrelationCoefficientsDto cpLiquidCoeffs,
            IPropertyEvaluator<Temperature, MolarEnergy> hvapEvaluator,
            IPropertyEvaluator<Pressure, Temperature> tsatEvaluator,
            Pressure operatingPressure,
            double mw)
        {
            _cpGasCoeffs = cpGasCoeffs;
            _cpLiquidCoeffs = cpLiquidCoeffs;
            _hvapEvaluator = hvapEvaluator;
            _tsatEvaluator = tsatEvaluator;
            _operatingPressure = operatingPressure;
            _molecularWeight = mw;
        }

        public MolarEnergy EvaluateAt(Temperature temperature)
        {
            double tempK = temperature.GetValue(TemperatureUnits.Kelvin);

            // Obtener Tsat a la presión de operación
            var tsat = _tsatEvaluator.EvaluateAt(_operatingPressure);
            double tSatK = tsat.GetValue(TemperatureUnits.Kelvin);

            // H_liquid_sat
            double hLiquidSat = IntegralCpL(tSatK) - IntegralCpL(Tref);

            // ΔHvap
            double hvap = _hvapEvaluator.EvaluateAt(new Temperature(tSatK, TemperatureUnits.Kelvin))
                              .GetValue(MolarEnergyUnits.J_Kgmol);

            // H_sat_vapor
            double hMolar = hLiquidSat + hvap;

            // Superheat si T > Tsat
            if (tempK > tSatK)
            {
                hMolar += CalculateAlyLeeIntegral(tSatK, tempK);
            }

            return new MolarEnergy(hMolar, MolarEnergyUnits.J_Kgmol);
        }

        private double IntegralCpL(double t)
        {
            return _cpLiquidCoeffs.C1 * t
                + (_cpLiquidCoeffs.C2 / 2.0) * Math.Pow(t, 2)
                + (_cpLiquidCoeffs.C3 / 3.0) * Math.Pow(t, 3)
                + (_cpLiquidCoeffs.C4 / 4.0) * Math.Pow(t, 4)
                + (_cpLiquidCoeffs.C5 / 5.0) * Math.Pow(t, 5);
        }

        private double CalculateAlyLeeIntegral(double tInicio, double tFin)
        {
            double IntegralCpG(double t)
            {
                double coth = 1.0 / Math.Tanh(_cpGasCoeffs.C3 / t);
                double tanh = Math.Tanh(_cpGasCoeffs.C5 / t);
                return _cpGasCoeffs.C1 * t
                    + _cpGasCoeffs.C2 * _cpGasCoeffs.C3 * coth
                    - _cpGasCoeffs.C4 * _cpGasCoeffs.C5 * tanh;
            }

            return IntegralCpG(tFin) - IntegralCpG(tInicio);
        }
    }

}
