using Shared.SolverConsecutive;
using Shared.SolverQwen.Stream;
using Shared.UnitOperations.Streams;
using System;
using System.Collections.Generic;
using System.Text;
using UnitSystem;

namespace Shared.PhaseEnvelopes
{
    public static class PhaseEnvelopeGenerator
    {
        /// <summary>
        /// Genera la envolvente de fases de manera asíncrona sin bloquear la UI.
        /// </summary>
        public static async Task<PhaseEnvelopeData> GenerateAsync(IFacadeStream sourceStream, int numberOfPoints = 40)
        {
            var data = new PhaseEnvelopeData();

            try
            {
                if (sourceStream.Composition == null || !sourceStream.Composition.IsValid)
                {
                    data.ErrorMessage = "Composition is invalid or not defined.";
                    return data;
                }

                // 🔥 Enviamos todo el trabajo pesado a un hilo secundario
                await Task.Run(() =>
                {
                    // 1. CREAR EL STREAM FANTASMA
                    var ghostStream = new FacadeStream("Ghost_Envelope_Stream");

                    // 2. CLONAR MÉTODO TERMODINÁMICO Y COMPOSICIÓN
                    // Asumiendo que su MaterialStream expone ThermoMethod. Ajuste si lo llama distinto.
                    // (Revise que esta parte compile bien con sus métodos de seteo)
                    var methodDto = sourceStream.MaterialStream.ThermoMethod;
                    ghostStream.SetThermodynamicMethod(methodDto);

                    // Clonar fracciones molares
                    for (int i = 0; i < sourceStream.Composition.Components.Count; i++)
                    {
                        var originalComp = sourceStream.Composition.Components[i];
                        var ghostComp = ghostStream.Composition.Components[i];

                        if (originalComp.MolarFraction.IsDefined)
                        {
                            ghostComp.MolarFraction.SetValue(originalComp.MolarFraction.Value, VariableDefinedBy.Solver);
                        }
                    }
                    // Forzamos a que el MaterialStream interno tome la composición
                    ghostStream.Composition.CompositionChanged();

                    // 3. DEFINIR LÍMITES DEL BARRIDO DE PRESIÓN (Escala Logarítmica)
                    // Iniciamos en 1 bar y vamos hasta 100 bar (límite industrial común)
                    double pMinBar = 1.0;
                    double pMaxBar = 100.0;
                    double logPMin = Math.Log10(pMinBar);
                    double logPMax = Math.Log10(pMaxBar);
                    double step = (logPMax - logPMin) / Math.Max(1, numberOfPoints - 1);

                    // 4. BARRIDO TERMODINÁMICO
                    for (int i = 0; i < numberOfPoints; i++)
                    {
                        double currentPBar = Math.Pow(10, logPMin + (step * i));
                        var pTest = new Pressure(currentPBar, PressureUnits.Bara);

                        // -------------------------------------------------------------
                        // A. PUNTO DE BURBUJA (VF = 0%)
                        // -------------------------------------------------------------
                        try
                        {
                            var vfBub = new Percentage(0, PercentageUnits.Percentage);

                            // Llamamos directamente a su función estrella en el MaterialStream interno
                            double tBubK = ghostStream.MaterialStream.SolveFlashPVF(pTest, vfBub);
                            var tBub = new Temperature(tBubK, TemperatureUnits.Kelvin);

                            // Para obtener la entalpía, fijamos las propiedades y forzamos el cálculo bulk
                            ghostStream.MaterialStream.Temperature = tBub;
                            ghostStream.MaterialStream.Pressure = pTest;
                            ghostStream.MaterialStream.SetVaporFraction(vfBub);
                            ghostStream.MaterialStream.CurrentState = ThermodynamicState.SaturatedLiquid;
                            ghostStream.MaterialStream.CalculateBulkProperties();

                            data.BubbleCurve.Add(new EnvelopePoint
                            {
                                Pressure = pTest,
                                Temperature = tBub,
                                MassEnthalpy = ghostStream.MaterialStream.MassEnthalpy,
                                MolarEnthalpy = ghostStream.MaterialStream.MolarEnthalpy
                            });
                        }
                        catch { /* Ignorar puntos que no convergen (ej. zona supercrítica) */ }

                        // -------------------------------------------------------------
                        // B. PUNTO DE ROCÍO (VF = 100%)
                        // -------------------------------------------------------------
                        try
                        {
                            var vfDew = new Percentage(100, PercentageUnits.Percentage);

                            double tDewK = ghostStream.MaterialStream.SolveFlashPVF(pTest, vfDew);
                            var tDew = new Temperature(tDewK, TemperatureUnits.Kelvin);

                            ghostStream.MaterialStream.Temperature = tDew;
                            ghostStream.MaterialStream.Pressure = pTest;
                            ghostStream.MaterialStream.SetVaporFraction(vfDew);
                            ghostStream.MaterialStream.CurrentState = ThermodynamicState.SaturatedVapor;
                            ghostStream.MaterialStream.CalculateBulkProperties();

                            data.DewCurve.Add(new EnvelopePoint
                            {
                                Pressure = pTest,
                                Temperature = tDew,
                                MassEnthalpy = ghostStream.MaterialStream.MassEnthalpy,
                                MolarEnthalpy = ghostStream.MaterialStream.MolarEnthalpy
                            });
                        }
                        catch { /* Ignorar puntos que no convergen */ }
                    }
                }); // Fin del Task.Run

                // Si logramos sacar al menos 1 punto, consideramos que fue un éxito
                data.Success = data.BubbleCurve.Count > 0 || data.DewCurve.Count > 0;
                if (!data.Success)
                {
                    data.ErrorMessage = "No convergence found for the requested pressure range.";
                }
            }
            catch (Exception ex)
            {
                data.Success = false;
                data.ErrorMessage = $"Envelope generation error: {ex.Message}";
            }

            return data;
        }
    }
}
