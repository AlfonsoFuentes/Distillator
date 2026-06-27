using Shared.SolverQwen.Stream;
using Shared.Thermodynamics.Phases;
using Shared.Thermodynamics.PureComponents;
using Shared.UnitOperations.Streams;
using System.Collections.Immutable;
using UnitSystem;

namespace Shared.SolverConsecutive.Equipments.Columns.Orchestrador
{

    public sealed class FUGCalculationService : IColumnPostSolverCalculation
    {
        public int Order => 2;

        private readonly SolverColumn _column;

        public FUGCalculationService(SolverColumn column)
        {
            _column = column;
        }

        public async Task CalculateAsync(CancellationToken cancellationToken = default)
        {
            if (_column.State != ColumnStateType.Solved)
            {
                Console.WriteLine($"⚠️ FUG: Columna no está resuelta, saltando cálculo");
                return;
            }

            // 🔥 Si la topología no cambió, usar caché (no recalcular)
            if (_column.Orchestrator != null && !_column.Orchestrator.TopologyChanged)
            {
                Console.WriteLine($"✅ FUG: Topología sin cambios, usando caché");
                return;
            }

            IFacadeStream refluxInlet = _column.RefluxInlet ?? null!;
            IFacadeStream vaporOutlet = _column.VaporOutlet ?? null!;
            IFacadeStream bottomOutlet = _column.BottomOutlet ?? null!;
            IReadOnlyList<IFacadeStream> feeds = _column.Feeds ?? new List<IFacadeStream>();

            await Task.Run(() =>
            {
                try
                {
                    if (refluxInlet == null || vaporOutlet == null || bottomOutlet == null || feeds.Count == 0)
                    {
                        Console.WriteLine($"⚠️ FUG: Streams incompletos, retornando parámetros vacíos");
                        _column.Orchestrator?.SetDistillationParameters(CreateEmptyDistillationParameters());
                        return;
                    }

                    if (refluxInlet.State != StreamStateType.Calculated ||
                        vaporOutlet.State != StreamStateType.Calculated ||
                        bottomOutlet.State != StreamStateType.Calculated)
                    {
                        Console.WriteLine($"⚠️ FUG: Streams no calculadas, retornando parámetros vacíos");
                        _column.Orchestrator?.SetDistillationParameters(CreateEmptyDistillationParameters());
                        return;
                    }

                    // 1. Calcular Reflujo Actual (R)
                    double L = refluxInlet.MolarFlow.Value.GetValue(MolarFlowUnits.Kgmol_hr);
                    double V = vaporOutlet.MolarFlow.Value.GetValue(MolarFlowUnits.Kgmol_hr);
                    double D = V - L;

                    if (D <= 0)
                    {
                        Console.WriteLine($"⚠️ FUG: Flujo de destilado inválido (D={D}), retornando parámetros vacíos");
                        _column.Orchestrator?.SetDistillationParameters(CreateEmptyDistillationParameters());
                        return;
                    }

                    double R_actual = L / D;

                    // 2. Acceder a MaterialStream para obtener datos de fases
                    var topMaterial = vaporOutlet.MaterialStream;
                    var botMaterial = bottomOutlet.MaterialStream;
                    var feedMaterial = feeds[0].MaterialStream;

                    var topComps = vaporOutlet.Composition.Components;
                    var botComps = bottomOutlet.Composition.Components;
                    var feedComps = feeds[0].Composition.Components;
                    int numComps = topComps.Count;

                    // 3. Análisis de componentes con datos reales del equilibrio
                    var compAnalysis = new List<(int Index, double BP, double x_D, double x_B, double z_F,
                        double K_top, double K_bot, double K_feed, double alpha_eff, PureComponentData Data)>();

                    for (int i = 0; i < numComps; i++)
                    {
                        var materialComp = topMaterial.Components[i];
                        var data = materialComp.PureComponentData;

                        double x_D = topComps[i].MolarFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0;
                        double x_B = botComps[i].MolarFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0;
                        double z_F = feedComps[i].MolarFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0;

                        double x_top = topMaterial.LiquidPhase.Components[i].MolarFraction;
                        double y_top = topMaterial.VaporPhase.Components[i].MolarFraction;
                        double K_top = x_top > 1e-10 ? y_top / x_top : 0;

                        double x_bot = botMaterial.LiquidPhase.Components[i].MolarFraction;
                        double y_bot = botMaterial.VaporPhase.Components[i].MolarFraction;
                        double K_bot = x_bot > 1e-10 ? y_bot / x_bot : 0;

                        double x_feed = feedMaterial.LiquidPhase.Components[i].MolarFraction;
                        double y_feed = feedMaterial.VaporPhase.Components[i].MolarFraction;
                        double K_feed = x_feed > 1e-10 ? y_feed / x_feed : 0;

                        compAnalysis.Add((
                            i,
                            data.BoilingPoint.GetValue(TemperatureUnits.Kelvin),
                            x_D,
                            x_B,
                            z_F,
                            K_top,
                            K_bot,
                            K_feed,
                            0.0,
                            data
                        ));
                    }

                    compAnalysis = compAnalysis.OrderBy(c => c.BP).ToList();

                    // 4. Identificar componentes clave (LK y HK)
                    int lkIndex = 0;
                    int hkIndex = 1;
                    double maxCutRatio = -1.0;

                    for (int i = 0; i < numComps - 1; i++)
                    {
                        double s_i = compAnalysis[i].x_D / (compAnalysis[i].x_B + 1e-10);
                        double s_next = compAnalysis[i + 1].x_D / (compAnalysis[i + 1].x_B + 1e-10);
                        double ratio = s_i / (s_next + 1e-10);

                        if (ratio > maxCutRatio)
                        {
                            maxCutRatio = ratio;
                            lkIndex = i;
                            hkIndex = i + 1;
                        }
                    }

                    // 5. Calcular volatilidad efectiva
                    double K_HK_bot = compAnalysis[hkIndex].K_bot;
                    double[] alpha_eff = new double[numComps];

                    for (int i = 0; i < numComps; i++)
                    {
                        double K_avg = Math.Sqrt(Math.Max(compAnalysis[i].K_top, 1e-10) *
                                                Math.Max(compAnalysis[i].K_bot, 1e-10));

                        alpha_eff[i] = K_avg / Math.Max(K_HK_bot, 1e-10);
                        compAnalysis[i] = compAnalysis[i] with { alpha_eff = alpha_eff[i] };
                    }

                    double alpha_LK = alpha_eff[lkIndex];

                    // 6. Fracciones del componente clave
                    double xD_LK = compAnalysis[lkIndex].x_D;
                    double xB_LK = compAnalysis[lkIndex].x_B;
                    double xD_HK = compAnalysis[hkIndex].x_D;
                    double xB_HK = compAnalysis[hkIndex].x_B;

                    // 7. Fenske con volatilidad efectiva
                    double N_min = 0;
                    if (alpha_LK > 1.001)
                    {
                        double numerator = (xD_LK / (xD_HK + 1e-10)) * (xB_HK / (xB_LK + 1e-10));
                        double denominator = Math.Log(alpha_LK);

                        if (Math.Abs(denominator) > 1e-10 && numerator > 0)
                        {
                            N_min = Math.Log(numerator) / denominator;
                        }
                    }

                    // 8. Underwood con rango de búsqueda corregido



                    double q = CalculateQ(feeds[0], _column);


                    // Ajustar para casos extremos

                    double R_min = 0;
                    double theta = 1.0;

                    if (alpha_LK > 1.001)
                    {
                        double alpha_HK = alpha_eff[hkIndex];
                        double minAlpha = alpha_eff.Min();
                        double maxAlpha = alpha_eff.Max();

                        double left = Math.Max(alpha_HK + 0.0001, minAlpha + 0.0001);
                        double right = Math.Min(alpha_LK - 0.0001, maxAlpha - 0.0001);

                        if (left >= right)
                        {
                            left = alpha_HK * 1.001;
                            right = alpha_LK * 0.999;
                        }

                        for (int iter = 0; iter < 50; iter++)
                        {
                            theta = (left + right) / 2.0;
                            double f_theta = 0.0;

                            for (int i = 0; i < numComps; i++)
                            {
                                if (Math.Abs(alpha_eff[i] - theta) > 1e-10)
                                    f_theta += (alpha_eff[i] * compAnalysis[i].z_F) / (alpha_eff[i] - theta);
                            }

                            f_theta -= (1.0 - q);

                            if (Math.Abs(f_theta) < 1e-4) break;

                            if (f_theta > 0) left = theta;
                            else right = theta;
                        }

                        for (int i = 0; i < numComps; i++)
                        {
                            if (Math.Abs(alpha_eff[i] - theta) > 1e-10)
                                R_min += (alpha_eff[i] * compAnalysis[i].x_D) / (alpha_eff[i] - theta);
                        }
                        R_min -= 1;

                        if (R_min < 0 || double.IsNaN(R_min) || double.IsInfinity(R_min))
                        {
                            Console.WriteLine($"⚠️ FUG: R_min inválido ({R_min}), ajustando a 0");
                            R_min = 0;
                        }
                    }

                    // 9. Gilliland
                    double N_th = 0;
                    double excess = 0;

                    if (R_min > 0 && R_actual >= R_min && N_min > 0)
                    {
                        excess = ((R_actual - R_min) / R_min) * 100.0;
                        double X = (R_actual - R_min) / (R_actual + 1.0);
                        double Y = 0.75 * (1.0 - Math.Pow(X, 0.5668));
                        N_th = (N_min + Y) / (1.0 - Y);
                    }

                    Console.WriteLine($"✅ FUG calculado: R={R_actual:F2}, R_min={R_min:F2}, N_min={N_min:F1}, N_th={N_th:F1}");

                    var distParameter = new DistillationParameters
                    {
                        RefluxRatio = new UnitLess(R_actual),
                        MinRefluxRatio = new UnitLess(R_min),
                        RefluxExcess = new Percentage(excess, PercentageUnits.Percentage),
                        MinStages = new UnitLess(N_min),
                        TheoreticalStages = new UnitLess(N_th),
                        xD = xD_LK,
                        xB = xB_LK,
                        FeedQuality = q,
                        RelativeVolatilities = alpha_eff.ToImmutableList(),
                        LightKeyIndex = lkIndex,
                        HeavyKeyIndex = hkIndex
                    };
                    _column.Orchestrator?.SetDistillationParameters(distParameter);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error en FUG: {ex.Message}");
                    _column.Orchestrator?.SetDistillationParameters(CreateEmptyDistillationParameters());
                }
            }, cancellationToken);

            double CalculateQ(IFacadeStream feedMaterial, SolverColumn column)
            {

                var pressure = column.TopPressure.Value.GetValue(PressureUnits.Bara);
                IFacadeStream stream = new FacadeStream("test");
                stream.SetThermodynamicMethod(feedMaterial.ThermoMethod);
                foreach (var row in feedMaterial.Composition.Components)
                {
                    var comp = row.MolarFraction.GetSolverValue();
                    var compnew = stream.Composition.Components.FirstOrDefault(x => x.Id == row.Id);
                    if (compnew != null)
                    {
                        compnew.MolarFraction.SetValue(new Percentage(comp * 100, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
                    }
                }
                stream.Pressure.SetValue(new Pressure(pressure, PressureUnits.Bara), VariableDefinedBy.UserInput);
                stream.VaporFraction.SetValue(new Percentage(0, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
                double HL = stream.MolarEnthalpy.Value.GetValue(MolarEnergyUnits.Kcal_Kgmol);
                stream.VaporFraction.SetValue(new Percentage(100, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
                double HV = stream.MolarEnthalpy.Value.GetValue(MolarEnergyUnits.Kcal_Kgmol);

                double HF = feedMaterial.MolarEnthalpy.Value.GetValue(MolarEnergyUnits.Kcal_Kgmol);

                double q = 0;
                if (Math.Abs(HV - HL) > 0.01)
                {
                    q = (HV - HF) / (HV - HL);
                }
                //double feedVF = feedMaterial.VaporFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0;

                //double q = feedMaterial.ThermodynamicState switch
                //{
                //    ThermodynamicState.SubcooledLiquid => 1.2,  // q > 1 (líquido subenfriado)
                //    ThermodynamicState.SaturatedLiquid => 1.0,   // q = 1
                //    ThermodynamicState.VaporLiquidMixture => 1.0 - feedVF, // 0 < q < 1
                //    ThermodynamicState.SaturatedVapor => 0.0,    // q = 0
                //    ThermodynamicState.SuperheatedVapor => -0.2, // q < 0 (vapor sobrecalentado)
                //    _ => 1.0 - feedVF  // Fallback
                //};

                Console.WriteLine($"🔍 FUG: q (placeholder) = {q:F2}, Estado = {feedMaterial.ThermodynamicState}");


                return q;
            }
        }

        private static DistillationParameters CreateEmptyDistillationParameters()
        {
            return new DistillationParameters
            {
                RefluxRatio = new UnitLess(0),
                MinRefluxRatio = new UnitLess(0),
                RefluxExcess = new Percentage(0, PercentageUnits.Percentage),
                MinStages = new UnitLess(0),
                TheoreticalStages = new UnitLess(0),
                xD = 0,
                xB = 0,
                FeedQuality = 0,
                RelativeVolatilities = ImmutableList<double>.Empty,
                LightKeyIndex = 0,
                HeavyKeyIndex = 1
            };
        }
    }
}