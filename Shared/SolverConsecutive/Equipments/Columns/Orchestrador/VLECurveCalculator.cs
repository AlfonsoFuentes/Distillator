using Shared.PropertiesDtos.Methods;
using Shared.SolverQwen.Stream;
using System.Collections.Immutable;
using System.Data.Common;
using UnitSystem;

namespace Shared.SolverConsecutive.Equipments.Columns.Orchestrador
{

     public sealed class VLECurveCalculator : IColumnPostSolverCalculation
        {
            public int Order => 1;

            private readonly SolverColumn _column;

            public VLECurveCalculator(SolverColumn column)
            {
                _column = column;
            }

            public async Task CalculateAsync(CancellationToken cancellationToken = default)
            {
                // 🔥 Si la presión no cambió, usar caché
                if (_column.Orchestrator != null && !_column.Orchestrator.ColumnPressureChanged)
                {
                    return;
                }

                Pressure topPressure = _column.TopPressure.Value;
                var refStream = _column.GetFirstAvailableStream();

                if (refStream == null)
                {
                    _column.Orchestrator?.SetVLECurveResult(CreateEmptyVLECurve());
                    return;
                }

                if (refStream.ThermoMethod == null)
                {
                    _column.Orchestrator?.SetVLECurveResult(CreateEmptyVLECurve());
                    return;
                }

                ThermodynamicMethodFullDto thermoMethod = refStream.ThermoMethod;
                IReadOnlyList<ComponentFacade> components = refStream.Composition.Components;
                int numPoints = 100;

                await Task.Run(() =>
                {
                    try
                    {
                        // 🔥 Pre-calcular datos comunes (una sola vez, fuera del loop)
                        var sortedComponents = components.OrderBy(c =>
                        {
                            var materialComp = thermoMethod.Components.FirstOrDefault(mc => mc.ComponentId == c.Id);
                            return materialComp?.FullData?.BoilingPoint?.GetValue(TemperatureUnits.Kelvin) ?? 0;
                        }).ToList();

                        var lightComponent = sortedComponents.First();
                        var heavyComponent = sortedComponents.Last();
                        double pressure = topPressure.GetValue(PressureUnits.Bara);

                        // 🔥 Array pre-allocado (thread-safe porque cada thread escribe en su propio índice)
                        var curve = new VLEPointResult[numPoints + 1];

                        // 🔥 Parallel.For: distribuye las 51 iteraciones entre todos los cores
                        ParallelOptions parallelOptions = new ParallelOptions
                        {
                            MaxDegreeOfParallelism = Environment.ProcessorCount,
                            CancellationToken = cancellationToken
                        };

                        Parallel.For(0, numPoints + 1, parallelOptions, i =>
                        {
                            double x = i / (double)numPoints;

                            // Cada thread crea su propio FacadeStream (independiente)
                            var testStream = new FacadeStream($"VLE_Test_{i}");
                            testStream.SetThermodynamicMethod(thermoMethod);
                            testStream.Pressure.SetValue(new Pressure(pressure, PressureUnits.Bara), VariableDefinedBy.Solver);

                            foreach (var comp in testStream.Composition.Components)
                            {
                                if (comp.Id == lightComponent.Id)
                                    comp.MolarFraction.SetValue(new Percentage(x * 100, PercentageUnits.Percentage), VariableDefinedBy.Solver);
                                else if (comp.Id == heavyComponent.Id)
                                    comp.MolarFraction.SetValue(new Percentage((1 - x) * 100, PercentageUnits.Percentage), VariableDefinedBy.Solver);
                                else
                                    comp.MolarFraction.SetValue(new Percentage(0, PercentageUnits.Percentage), VariableDefinedBy.Solver);
                            }

                            testStream.VaporFraction.SetValue(new Percentage(0, PercentageUnits.Percentage), VariableDefinedBy.Solver);

                            double y = 0;
                            if (testStream.VaporPhase != null)
                            {
                                var vaporComp = testStream.VaporPhase.Components.FirstOrDefault(c => c.Id == lightComponent.Id);
                                if (vaporComp != null)
                                    y = vaporComp.MolarFraction;
                            }

                            // 🔥 Cada thread escribe en su propio índice del array (sin locks)
                            curve[i] = new VLEPointResult
                            {
                                x = x,
                                y = y,
                                Temperature = testStream.Temperature.Value,
                                Pressure = testStream.Pressure.Value,
                                LiquidEnthalpy = testStream.MassEnthalpy.Value,
                                LiquidDensity = testStream.MassDensity.Value,
                                LiquidMolarDensity = testStream.MolarDensity.Value,
                                VaporEnthalpy = testStream.VaporPhase?.MassEnthalpy != null
                                    ? new MassEnergy(testStream.VaporPhase.MassEnthalpy.GetValue(MassEnergyUnits.J_Kg), MassEnergyUnits.J_Kg)
                                    : new MassEnergy(0, MassEnergyUnits.Kcal_Kg),
                                VaporDensity = testStream.VaporPhase?.MassDensity != null
                                    ? new MassDensity(testStream.VaporPhase.MassDensity.GetValue(MassDensityUnits.Kg_m3), MassDensityUnits.Kg_m3)
                                    : new MassDensity(0, MassDensityUnits.Kg_m3),
                                VaporMolarDensity = testStream.VaporPhase?.MolarDensity != null
                                    ? new MolarDensity(testStream.VaporPhase.MolarDensity.GetValue(MolarDensityUnits.Kgmol_m3), MolarDensityUnits.Kgmol_m3)
                                    : new MolarDensity(0, MolarDensityUnits.Kgmol_m3)
                            };
                        });


                        _column.Orchestrator?.SetVLECurveResult(
                            new VLECurveResult
                            {
                                Points = curve.ToImmutableList(),
                                Pressure = topPressure
                            });
                    }
                    catch (OperationCanceledException)
                    {
                        _column.Orchestrator?.SetVLECurveResult(CreateEmptyVLECurve());
                    }
                    catch (Exception)
                    {
                        _column.Orchestrator?.SetVLECurveResult(CreateEmptyVLECurve());
                    }
                }, cancellationToken);
            }

            private static VLECurveResult CreateEmptyVLECurve()
            {
                return new VLECurveResult
                {
                    Points = ImmutableList<VLEPointResult>.Empty,
                    Pressure = new Pressure(0, PressureUnits.Bara)
                };
            }
        }
   
}
