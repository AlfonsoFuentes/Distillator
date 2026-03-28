using Shared.Calculator.Components;
using Shared.Calculator.ProcessVariables;
using Shared.Thermodynamics.Enums;
using Shared.Thermodynamics.Methods;
using System;
using System.Collections.Generic;
using System.Text;
using UnitSystem;

namespace Shared.Calculator.MaterialStreams
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public abstract class PhaseMixtureBase<T> : StreamBase<T> where T : PhaseComponent
    {
        public double[,] KijMatrix { get; protected set; } = new double[0, 0];
        public double CompressibilityFactor { get; protected set; } = 1.0;
        public int NumberOfRoots { get; protected set; } = 0;

        public ReducedProperties Reduced { get; private set; }
        public EosParameters EosParams { get; set; }

        public ThermodynamicMethodFullDto ThermoMethod { get; protected set; } = null!;
        public LiquidPhaseModel LiquidModel => ThermoMethod.LiquidModel;
        public VaporPhaseModel VapourModel => ThermoMethod.VaporModel;

        protected void InitializeKijMatrix(List<BinaryInteractionParameterDto> binaryParameters)
        {
            int n = Components.Count;
            KijMatrix = new double[n, n];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (i == j)
                    {
                        KijMatrix[i, j] = 0.0;
                    }
                    else
                    {
                        KijMatrix[i, j] = BinaryInteractionManager.GetKij(
                            Components[i].BaseProperties.Id,
                            Components[j].BaseProperties.Id,
                            Components[i].BaseProperties.Name,
                            Components[j].BaseProperties.Name,
                            VapourModel);
                    }
                }
            }
        }

        protected PhaseMixtureBase() : base()
        {
            Reduced = new ReducedProperties();
            EosParams = new EosParameters();
        }

        public void CalculateCriticalProperties()
        {
            if (Components.Count == 0) return;

            int n = Components.Count;

            double vcMix = 0.0;
            double zcMix = 0.0;

            double[] phi = new double[n];
            double[] vc = new double[n];
            double[] tc = new double[n];

            for (int i = 0; i < n; i++)
            {
                var comp = Components[i];
                double x = comp.MoleFraction;

                vc[i] = comp.BaseProperties.CriticalVolume.GetValue(MolarVolumeSpecificUnits.m3_Kgmol);
                tc[i] = comp.BaseProperties.CriticalTemperature.GetValue(TemperatureUnits.Kelvin);
                double zc_i = comp.BaseProperties.CriticalZ;

                vcMix += x * vc[i];
                zcMix += x * zc_i;
                phi[i] = x * vc[i];
            }

            MolarVolCritical.SetValue(vcMix, MolarVolumeSpecificUnits.m3_Kgmol);

            if (vcMix > 0)
            {
                for (int i = 0; i < n; i++)
                {
                    phi[i] = phi[i] / vcMix;
                }
            }

            double tcMix = 0.0;

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    double raizVc = Math.Pow(vc[i], 1.0 / 3.0) + Math.Pow(vc[j], 1.0 / 3.0);
                    double unoMenosKij = (8.0 * Math.Sqrt(vc[i] * vc[j])) / Math.Pow(raizVc, 3.0);
                    double tc_ij = unoMenosKij * Math.Sqrt(tc[i] * tc[j]);
                    tcMix += phi[i] * phi[j] * tc_ij;
                }
            }

            TempCritical.SetValue(tcMix, TemperatureUnits.Kelvin);

            double const_R = 8.314462618;
            double pcMix = (zcMix * const_R * tcMix) / vcMix;
            PressCritical.SetValue(pcMix, PressureUnits.KiloPascal);
        }

        public abstract void SetMethod(ThermodynamicMethodFullDto _ThermoMethod);

        public void CalculateCompressibilityZ()
        {
            if (VapourModel == VaporPhaseModel.IdealGas)
            {
                CompressibilityFactor = 1.0;
                NumberOfRoots = 1;
                return;
            }

            List<double> raices = CubicSolver.Solve(EosParams.Factors);
            NumberOfRoots = raices.Count;
            var raicesValidas = raices.Where(r => r > 0.0).ToList();

            if (raicesValidas.Any())
            {
                CompressibilityFactor = SelectRoot(raicesValidas);
            }
            else
            {
                CompressibilityFactor = 1.0;
            }
        }

        protected abstract double SelectRoot(List<double> roots);

        public void CalculateReducedProperties(Amount _temperature, Amount _pressure)
        {
            int n = Components.Count;
            if (n == 0) return;

            double temperatureK = _temperature.GetValue(TemperatureUnits.Kelvin);
            double pressureKpa = _pressure.GetValue(PressureUnits.KiloPascal);

            double tcKelvin = TempCritical.GetValue(TemperatureUnits.Kelvin);
            double pcKpa = PressCritical.GetValue(PressureUnits.KiloPascal);
            double vcM3 = MolarVolCritical.GetValue(MolarVolumeSpecificUnits.m3_Kgmol);

            if (tcKelvin > 0) Reduced.Temperature = temperatureK / tcKelvin;
            if (pcKpa > 0) Reduced.Pressure = pressureKpa / pcKpa;

            if (vcM3 > 0 && Components[0].MolarVolume != null)
            {
                double vMix = 0.0;
                for (int i = 0; i < n; i++)
                {
                    vMix += Components[i].MoleFraction * Components[i].MolarVolume.GetValue(MolarVolumeSpecificUnits.m3_Kgmol);
                }
                Reduced.Volume = vMix / vcM3;
            }
        }

        public void CalculateMixtureParameters(Amount _temperature, Amount _pressure)
        {
            int n = Components.Count;
            if (n == 0) return;

            if (VapourModel == VaporPhaseModel.IdealGas && this is VaporPhase)
            {
                EosParams = new EosParameters();
                return;
            }

            const double R_Gas = 8.314472;
            double temperatureK = _temperature.GetValue(TemperatureUnits.Kelvin);
            double pressureKpa = _pressure.GetValue(PressureUnits.KiloPascal);

            double aMix = 0.0;
            double bMix = 0.0;
            double sumaDerivada = 0.0;
            double multA = n > 0 ? Components[0].EosParams.MultA : 0.45724;

            for (int i = 0; i < n; i++)
            {
                double ai = Components[i].EosParams.A;
                double xi = Components[i].MoleFraction;
                double pci = Components[i].BaseProperties.CriticalPressure.GetValue(PressureUnits.KiloPascal);
                double tci = Components[i].BaseProperties.CriticalTemperature.GetValue(TemperatureUnits.Kelvin);
                double fwi = Components[i].EosParams.FW;

                bMix += Components[i].EosParams.B * xi;

                for (int j = 0; j < n; j++)
                {
                    double aj = Components[j].EosParams.A;
                    double xj = Components[j].MoleFraction;
                    double pcj = Components[j].BaseProperties.CriticalPressure.GetValue(PressureUnits.KiloPascal);
                    double tcj = Components[j].BaseProperties.CriticalTemperature.GetValue(TemperatureUnits.Kelvin);
                    double fwj = Components[j].EosParams.FW;

                    double kij = KijMatrix[i, j];

                    aMix += xi * xj * Math.Sqrt(ai * aj) * (1.0 - kij);
                    sumaDerivada += xi * xj * (1.0 - kij) *
                        (fwj * Math.Sqrt(ai * tcj / pcj) + fwi * Math.Sqrt(aj * tci / pci));
                }
            }

            EosParams = new EosParameters
            {
                A = aMix,
                B = bMix,
                Derivada_A = -(R_Gas / 2.0) * Math.Sqrt(multA / temperatureK) * sumaDerivada,
                U = n > 0 ? Components[0].EosParams.U : 2.0,
                W = n > 0 ? Components[0].EosParams.W : -1.0,
                MultA = multA
            };

            EosParams.AAsterisk = EosParams.A * pressureKpa / Math.Pow(R_Gas * temperatureK, 2.0);
            EosParams.BAsterisk = EosParams.B * pressureKpa / (R_Gas * temperatureK);
            EosParams.Factors[0] = 1.0;
            EosParams.Factors[1] = -(1.0 + EosParams.BAsterisk - EosParams.U * EosParams.BAsterisk);
            EosParams.Factors[2] = EosParams.AAsterisk + (EosParams.W - EosParams.U) * Math.Pow(EosParams.BAsterisk, 2.0) - EosParams.U * EosParams.BAsterisk;
            EosParams.Factors[3] = -EosParams.AAsterisk * EosParams.BAsterisk - EosParams.W * Math.Pow(EosParams.BAsterisk, 2.0) - EosParams.W * Math.Pow(EosParams.BAsterisk, 3.0);
        }

        public virtual void CalculateTP(Amount _temperature, Amount _pressure)
        {
            foreach (var comp in Components)
            {
                comp.CalculateTP(_temperature, _pressure);
            }
            CalculateCriticalProperties();
            CalculateReducedProperties(_temperature, _pressure);
            CalculateMixtureParameters(_temperature, _pressure);
            CalculateCompressibilityZ();

            CalculatePhaseInteractions(_temperature, _pressure);
            UpdateEquilibriumConstants();
        }

        protected abstract void CalculatePhaseInteractions(Amount temperature, Amount pressure);
        protected virtual void UpdateEquilibriumConstants() { }
    }
    public abstract class PhaseMixtureBase2<T> : StreamBase<T> where T : PhaseComponent
    {
        public double[,] KijMatrix { get; protected set; } = new double[0, 0];
        public double CompressibilityFactor { get; protected set; } = 1.0;
        public int NumberOfRoots { get; protected set; } = 0;

       
        public ReducedProperties Reduced { get; private set; }
        public EosParameters EosParams { get; set; }
        

       

        public ThermodynamicMethodFullDto ThermoMethod { get; protected set; } = null!;
        public LiquidPhaseModel LiquidModel => ThermoMethod.LiquidModel;
        public VaporPhaseModel VapourModel => ThermoMethod.VaporModel;
        /// <summary>
        /// Inicializa la matriz bidimensional de parámetros de interacción binaria (Kij)
        /// Se ejecuta UNA SOLA VEZ al definir la topología de la corriente.
        /// </summary>
        protected void InitializeKijMatrix(List<BinaryInteractionParameterDto> binaryParameters)
        {
            int n = Components.Count;
            KijMatrix = new double[n, n];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (i == j)
                    {
                        KijMatrix[i, j] = 0.0; // La diagonal principal siempre es 0
                    }
                    else
                    {
                        // Se llama al Manager UNA SOLA VEZ por par (i,j)
                        KijMatrix[i, j] = BinaryInteractionManager.GetKij(
                            Components[i].BaseProperties.Id,
                            Components[j].BaseProperties.Id,
                            Components[i].BaseProperties.Name,
                            Components[j].BaseProperties.Name     ,
                            VapourModel);
                    }
                }
            }
        }
        protected PhaseMixtureBase2()  :base()
        {

            
           
            Reduced = new ReducedProperties();
            EosParams = new EosParameters();
        }
        /// <summary>
        /// Calcula las propiedades pseudocríticas de la mezcla (Volumen, Temperatura y Presión).
        /// Requiere que las fracciones molares estén definidas y sumen 1.
        /// Optimizado: Combina 2 passes en 1 para la inicialización.
        /// </summary>
        public void CalculateCriticalProperties()
        {
            if (Components.Count == 0) return;

            int n = Components.Count;

            // =========================================================
            // 1. VOLUMEN CRÍTICO Y Z CRÍTICO (Reglas de mezcla lineales)
            //    OPTIMIZACIÓN: Combinado con el pre-cálculo de vectores (1 pass en vez de 2)
            // =========================================================
            double vcMix = 0.0;
            double zcMix = 0.0;

            // Arrays para el cálculo de temperatura crítica (O(N²))
            double[] phi = new double[n];
            double[] vc = new double[n];
            double[] tc = new double[n];

            // ✅ PASS 1 COMBINADO: vcMix, zcMix, y pre-cálculo de vectores en un solo loop
            for (int i = 0; i < n; i++)
            {
                var comp = Components[i];
                double x = comp.MoleFraction;

                // Extraer propiedades críticas
                vc[i] = comp.BaseProperties.CriticalVolume.GetValue(MolarVolumeSpecificUnits.m3_Kgmol);
                tc[i] = comp.BaseProperties.CriticalTemperature.GetValue(TemperatureUnits.Kelvin);
                double zc_i = comp.BaseProperties.CriticalZ;

                // Sumatorias lineales para vcMix y zcMix
                vcMix += x * vc[i];
                zcMix += x * zc_i;

                // Fracción volumétrica (requiere vcMix, se calcula después del loop)
                phi[i] = x * vc[i];  // ← Guardamos x*vc, dividimos por vcMix después
            }

            // Guardar volumen crítico de la mezcla
            MolarVolCritical.SetValue(vcMix, MolarVolumeSpecificUnits.m3_Kgmol);

            // ✅ Normalizar phi después de tener vcMix completo (evita división en cada iteración)
            if (vcMix > 0)
            {
                for (int i = 0; i < n; i++)
                {
                    phi[i] = phi[i] / vcMix;
                }
            }

            // =========================================================
            // 2. TEMPERATURA CRÍTICA (Regla cuadrática basada en volumen)
            //    OPTIMIZACIÓN: for en vez de foreach, acceso directo por índice
            // =========================================================
            double tcMix = 0.0;

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    // Parámetro de interacción cruzado para volumen
                    double raizVc = Math.Pow(vc[i], 1.0 / 3.0) + Math.Pow(vc[j], 1.0 / 3.0);
                    double unoMenosKij = (8.0 * Math.Sqrt(vc[i] * vc[j])) / Math.Pow(raizVc, 3.0);

                    // Temperatura crítica cruzada
                    double tc_ij = unoMenosKij * Math.Sqrt(tc[i] * tc[j]);

                    tcMix += phi[i] * phi[j] * tc_ij;
                }
            }

            TempCritical.SetValue(tcMix, TemperatureUnits.Kelvin);

            // =========================================================
            // 3. PRESIÓN CRÍTICA (Ecuación de estado en el punto crítico)
            // =========================================================
            double const_R = 8.314462618;
            double pcMix = (zcMix * const_R * tcMix) / vcMix;

            PressCritical.SetValue(pcMix, PressureUnits.KiloPascal);
        }
     

        public abstract void SetMethod(ThermodynamicMethodFullDto _ThermoMethod);
        public void CalculateCompressibilityZ()
        {
            if (VapourModel == VaporPhaseModel.IdealGas)
            {
                CompressibilityFactor = 1.0;
                NumberOfRoots = 1;
                return; // Evitamos llamar al CubicSolver
            }
            // Resolver el polinomio cúbico con los coeficientes ya calculados
            List<double> raices = CubicSolver.Solve(EosParams.Factors);
            NumberOfRoots = raices.Count;

            // Filtrar raíces válidas (Z > 0)
            var raicesValidas = raices.Where(r => r > 0.0).ToList();

            if (raicesValidas.Any())
            {
                // Cada fase decide qué raíz tomar (sobrescribible)
                CompressibilityFactor = SelectRoot(raicesValidas);
            }
            else
            {
                CompressibilityFactor = 1.0; // Fallback
            }
        }
        protected abstract double SelectRoot(List<double> roots);
        public void CalculateReducedProperties(Amount _temperature, Amount _pressure)
        {
            int n = Components.Count;
            if (n == 0) return;

            double temperatureK = _temperature.GetValue(TemperatureUnits.Kelvin);
            double pressureKpa = _pressure.GetValue(PressureUnits.KiloPascal);

            double tcKelvin = TempCritical.GetValue(TemperatureUnits.Kelvin);
            double pcKpa = PressCritical.GetValue(PressureUnits.KiloPascal);
            double vcM3 = MolarVolCritical.GetValue(MolarVolumeSpecificUnits.m3_Kgmol);

            if (tcKelvin > 0) Reduced.Temperature = temperatureK / tcKelvin;
            if (pcKpa > 0) Reduced.Pressure = pressureKpa / pcKpa;

            // ✅ REFACTOR: Adiós LINQ (.Any y .Sum), hola for nativo
            if (vcM3 > 0 && Components[0].MolarVolume != null)
            {
                double vMix = 0.0;
                for (int i = 0; i < n; i++)
                {
                    vMix += Components[i].MoleFraction * Components[i].MolarVolume.GetValue(MolarVolumeSpecificUnits.m3_Kgmol);
                }
                Reduced.Volume = vMix / vcM3;
            }
        }
      
        public void CalculateMixtureParameters(Amount _temperature, Amount _pressure)
        {
            int n = Components.Count;
            if (n == 0) return;

            if (VapourModel == VaporPhaseModel.IdealGas && this is VaporPhase)
            {
                EosParams = new EosParameters();
                return;
            }

            const double R_Gas = 8.314472;
            double temperatureK = _temperature.GetValue(TemperatureUnits.Kelvin);
            double pressureKpa = _pressure.GetValue(PressureUnits.KiloPascal);

            double aMix = 0.0;
            double bMix = 0.0; // ✅ Se calculará en el mismo ciclo principal
            double sumaDerivada = 0.0;
            double multA = n > 0 ? Components[0].EosParams.MultA : 0.45724;

            for (int i = 0; i < n; i++)
            {
                double ai = Components[i].EosParams.A;
                double xi = Components[i].MoleFraction;
                double pci = Components[i].BaseProperties.CriticalPressure.GetValue(PressureUnits.KiloPascal);
                double tci = Components[i].BaseProperties.CriticalTemperature.GetValue(TemperatureUnits.Kelvin);
                double fwi = Components[i].EosParams.FW;

                // ✅ AGRUPADO: Calculamos bMix aquí mismo, nos ahorramos un ciclo for completo
                bMix += Components[i].EosParams.B * xi;

                for (int j = 0; j < n; j++)
                {
                    double aj = Components[j].EosParams.A;
                    double xj = Components[j].MoleFraction;
                    double pcj = Components[j].BaseProperties.CriticalPressure.GetValue(PressureUnits.KiloPascal);
                    double tcj = Components[j].BaseProperties.CriticalTemperature.GetValue(TemperatureUnits.Kelvin);
                    double fwj = Components[j].EosParams.FW;

                    double kij = KijMatrix[i, j];

                    aMix += xi * xj * Math.Sqrt(ai * aj) * (1.0 - kij);
                    sumaDerivada += xi * xj * (1.0 - kij) *
                        (fwj * Math.Sqrt(ai * tcj / pcj) + fwi * Math.Sqrt(aj * tci / pci));
                }
            }

            EosParams = new EosParameters
            {
                A = aMix,
                B = bMix,
                Derivada_A = -(R_Gas / 2.0) * Math.Sqrt(multA / temperatureK) * sumaDerivada,
                U = n > 0 ? Components[0].EosParams.U : 2.0,
                W = n > 0 ? Components[0].EosParams.W : -1.0,
                MultA = multA
            };

            EosParams.AAsterisk = EosParams.A * pressureKpa / Math.Pow(R_Gas * temperatureK, 2.0);
            EosParams.BAsterisk = EosParams.B * pressureKpa / (R_Gas * temperatureK);
            EosParams.Factors[0] = 1.0;
            EosParams.Factors[1] = -(1.0 + EosParams.BAsterisk - EosParams.U * EosParams.BAsterisk);
            EosParams.Factors[2] = EosParams.AAsterisk + (EosParams.W - EosParams.U) * Math.Pow(EosParams.BAsterisk, 2.0) - EosParams.U * EosParams.BAsterisk;
            EosParams.Factors[3] = -EosParams.AAsterisk * EosParams.BAsterisk - EosParams.W * Math.Pow(EosParams.BAsterisk, 2.0) - EosParams.W * Math.Pow(EosParams.BAsterisk, 3.0);
        }
    

        public virtual void CalculateTP(Amount _temperature, Amount _pressure)
        {
            // 1. Propiedades Críticas (Base)
            foreach (var comp in Components)
            {
                comp.CalculateTP(_temperature, _pressure);
            }
            CalculateCriticalProperties();

            // 2. Propiedades Reducidas (Base)
            CalculateReducedProperties(_temperature, _pressure);

            // 3. Parámetros de Mezcla EoS (Base)
            CalculateMixtureParameters(_temperature, _pressure);

            // 4. Calcular Z de la mezcla (Base)
            CalculateCompressibilityZ(); // Asumiendo que renombras Calcular_Z

            // 5. Calcular Propiedades Puras (Base)
           

            // 6. DELEGADO: Cálculos específicos de interacción de fase (Actividad o Fugacidad de Mezcla)
            CalculatePhaseInteractions(_temperature, _pressure);

            // 7. DELEGADO (Opcional): Actualizar el equilibrio (K)
            UpdateEquilibriumConstants();
        }

        // --- Los "Huecos" del Template Method ---
        protected abstract void CalculatePhaseInteractions(Amount temperature, Amount pressure);
        protected virtual void UpdateEquilibriumConstants() { } // Virtual porque el vapor no lo usa igual


    }

}
