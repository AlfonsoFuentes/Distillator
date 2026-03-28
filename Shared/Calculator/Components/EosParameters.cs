using Shared.Thermodynamics.Enums;

namespace Shared.Calculator.Components
{
    public class EosParameters
    {
        public double A { get; set; }
        public double B { get; set; }
        public double AAsterisk { get; set; }
        public double BAsterisk { get; set; }
        public double U { get; set; }
        public double W { get; set; }
        public double FW { get; set; }
        public double MultA { get; set; }
        public double MultB { get; set; }
        public double Derivada_A { get; set; }

        // Arreglo para los coeficientes del polinomio cúbico de Z
        public double[] Factors { get; set; } = new double[4];
    }
    public static class EosParameterFactory
    {
        private const double R_Gas = 8.314472;

        /// <summary>
        /// Fabrica los parámetros (A, B, U, W, etc.) para una Ecuación de Estado específica
        /// evaluada para un componente puro a una T y P dadas.
        /// </summary>
        public static EosParameters CreateForPureComponent(
            VaporPhaseModel vaporModel,
            double tcKelvin,
            double pcKpa,
            double acentricFactor, // omega (w)
            double tKelvin,
            double pKpa)
        {
            if (vaporModel == VaporPhaseModel.IdealGas)
            {
                return new EosParameters(); // Todo en cero, comportamiento ideal
            }

            double tr = tKelvin / tcKelvin;
            double alfa = 0.0;

            // Instanciamos el objeto que vamos a devolver
            EosParameters parametros = new EosParameters();

            // 1. Asignación de constantes universales de la EoS según el modelo
            switch (vaporModel)
            {
                case VaporPhaseModel.VanDerWaals:
                    parametros.MultA = 27.0 / 64.0;
                    parametros.MultB = 1.0 / 8.0;
                    parametros.U = 0;
                    parametros.W = 0;
                    parametros.FW = 0;
                    alfa = 1.0;
                    break;

                case VaporPhaseModel.RedlichKwong:
                    parametros.MultA = 0.42748;
                    parametros.MultB = 0.08664;
                    parametros.U = 1;
                    parametros.W = 0;
                    parametros.FW = 1;
                    alfa = 1.0 / Math.Sqrt(tr);
                    break;

                case VaporPhaseModel.Wilson:
                    parametros.MultA = 0.42748;
                    parametros.MultB = 0.08664;
                    parametros.U = 1;
                    parametros.W = 0;
                    parametros.FW = 1.57 + 1.62 * acentricFactor;
                    alfa = (1.0 + parametros.FW * (1.0 / tr - 1.0)) * tr;
                    break;

                case VaporPhaseModel.SoaveRedlichKwong1972:
                    parametros.MultA = 0.42748;
                    parametros.MultB = 0.08664;
                    parametros.U = 1;
                    parametros.W = 0;
                    parametros.FW = 0.48 + 1.574 * acentricFactor - 0.176 * Math.Pow(acentricFactor, 2.0);
                    alfa = Math.Pow(1.0 + parametros.FW * (1.0 - Math.Sqrt(tr)), 2.0);
                    break;

                case VaporPhaseModel.PengRobinson:
                    parametros.MultA = 0.45724;
                    parametros.MultB = 0.0778;
                    parametros.U = 2;
                    parametros.W = -1;
                    parametros.FW = 0.37464 + 1.54226 * acentricFactor - 0.26992 * Math.Pow(acentricFactor, 2.0);
                    alfa = Math.Pow(1.0 + parametros.FW * (1.0 - Math.Sqrt(tr)), 2.0);
                    break;

                case VaporPhaseModel.SoaveRedlichKwong1984:
                    parametros.MultA = 0.42188;
                    parametros.MultB = 0.08333;
                    parametros.U = 1;
                    parametros.W = 0;
                    parametros.FW = 0.4998 + 1.5928 * acentricFactor - 0.19563 * acentricFactor * acentricFactor + 0.025 * Math.Pow(acentricFactor, 3.0);
                    alfa = Math.Pow(1.0 + parametros.FW * (1.0 - Math.Sqrt(tr)), 2.0);
                    break;

                case VaporPhaseModel.SoaveRedlichKwong1995:
                    parametros.MultA = 0.42188;
                    parametros.MultB = 0.08333;
                    parametros.U = 1;
                    parametros.W = 0.001736;
                    parametros.FW = 0.484 + 1.515 * acentricFactor - 0.44 * Math.Pow(acentricFactor, 2.0);
                    alfa = 1.0 + (2.756 * parametros.FW - 0.7) * Math.Pow(1.0 - Math.Sqrt(tr), 2.0) + parametros.FW * (1.0 - tr);
                    break;

                default:
                    throw new NotImplementedException($"El modelo de vapor {vaporModel} no está soportado por la fábrica.");
            }

            // 2. Cálculo de los parámetros termodinámicos (a, b) y adimensionales (A*, B*)
            parametros.A = parametros.MultA * Math.Pow(R_Gas * tcKelvin, 2.0) / pcKpa * alfa;
            parametros.B = parametros.MultB * R_Gas * tcKelvin / pcKpa;

            parametros.AAsterisk = parametros.A * pKpa / Math.Pow(R_Gas * tKelvin, 2.0);
            parametros.BAsterisk = parametros.B * pKpa / (R_Gas * tKelvin);

            // 3. Generación de los coeficientes para el polinomio cúbico de Z
            parametros.Factors[0] = 1.0;
            parametros.Factors[1] = -(1.0 + parametros.BAsterisk - parametros.U * parametros.BAsterisk);
            parametros.Factors[2] = parametros.AAsterisk + (parametros.W - parametros.U) * Math.Pow(parametros.BAsterisk, 2.0) - parametros.U * parametros.BAsterisk;
            parametros.Factors[3] = -parametros.AAsterisk * parametros.BAsterisk - parametros.W * Math.Pow(parametros.BAsterisk, 2.0) - parametros.W * Math.Pow(parametros.BAsterisk, 3.0);

            return parametros;
        }
    }
}
