using Shared.SolverQwen.Stream;
using UnitSystem;

namespace Shared.SolverConsecutive.Equipments.Columns
{
    public static class VLECurveGenerator
    {
        public static List<VLEPoint> GenerateCurve(SolverColumn column, int numPoints = 50)
        {
            var curve = new List<VLEPoint>();

            var referenceStream = GetFirstAvailableStream(column);

            if (referenceStream == null || referenceStream.ThermoMethod == null || referenceStream.MaterialStream.Components.Count == 0)
                return curve;

            var pressure = column.TopPressure.Value.GetValue(PressureUnits.Bara);

            var components = referenceStream.MaterialStream.Components
                .OrderBy(c => c.PureComponentData.BoilingPoint.GetValue(TemperatureUnits.Kelvin))
                .ToList();

            if (components.Count < 2) return curve;

            var lightComponent = components.First();
            var heavyComponent = components.Last();

            for (int i = 0; i <= numPoints; i++)
            {
                double x = i / (double)numPoints;

                // Crear stream ficticia
                var testStream = new FacadeStream($"VLE_Test_{i}");
                testStream.SetThermodynamicMethod(referenceStream.ThermoMethod);
                testStream.Pressure.SetValue(new Pressure(pressure, PressureUnits.Bara), VariableDefinedBy.Solver);

                // Fijar composición
                foreach (var comp in testStream.Composition.Components)
                {
                    if (comp.Id == lightComponent.Id)
                        comp.MolarFraction.SetValue(new Percentage(x * 100, PercentageUnits.Percentage), VariableDefinedBy.Solver);
                    else if (comp.Id == heavyComponent.Id)
                        comp.MolarFraction.SetValue(new Percentage((1 - x) * 100, PercentageUnits.Percentage), VariableDefinedBy.Solver);
                    else
                        comp.MolarFraction.SetValue(new Percentage(0, PercentageUnits.Percentage), VariableDefinedBy.Solver);
                }

                // Calcular punto de burbuja (VaporFraction = 0)
                testStream.VaporFraction.SetValue(new Percentage(0, PercentageUnits.Percentage), VariableDefinedBy.Solver);
                // El sistema calcula automáticamente el equilibrio

                // Extraer propiedades del líquido saturado
                double y = 0;
                if (testStream.VaporPhase != null)
                {
                    var vaporComp = testStream.VaporPhase.Components.FirstOrDefault(c => c.Id == lightComponent.Id);
                    if (vaporComp != null)
                        y = vaporComp.MolarFraction;
                }

                // Extraer todas las propiedades termodinámicas
                var pressureValue = testStream.Pressure.Value;
                var temperatureValue = testStream.Temperature.Value;

                var liquidEnthalpy = testStream.MassEnthalpy.Value;
                var liquidDensity = testStream.MassDensity.Value;
                var liquidMolarDensity = testStream.MolarDensity.Value;

                // Para el vapor, necesitamos acceder a la fase de vapor
                MassEnergy vaporEnthalpy = new MassEnergy(0, MassEnergyUnits.Kcal_Kg);
                MassDensity vaporDensity = new MassDensity(0, MassDensityUnits.Kg_m3);
                MolarDensity vaporMolarDensity = new MolarDensity(0, MolarDensityUnits.Kgmol_m3);

                if (testStream.VaporPhase != null)
                {
                    // Extraer propiedades del vapor si están disponibles
                    // Nota: Dependiendo de tu implementación, puede que necesites acceder
                    // a propiedades específicas de la fase de vapor
                    if (testStream.VaporPhase.MassEnthalpy != null)
                        vaporEnthalpy = testStream.VaporPhase.MassEnthalpy;
                    if (testStream.VaporPhase.MassDensity != null)
                        vaporDensity = testStream.VaporPhase.MassDensity;
                    if (testStream.VaporPhase.MolarDensity != null)
                        vaporMolarDensity = testStream.VaporPhase.MolarDensity;
                }

                // Crear punto VLE completo
                var vlePoint = new VLEPoint(
                    x, y,
                    new Pressure(pressureValue.Value, pressureValue.Unit),
                    new Temperature(temperatureValue.Value, temperatureValue.Unit),
                    new MassEnergy(liquidEnthalpy.Value, liquidEnthalpy.Unit),
                    new MassDensity(liquidDensity.Value, liquidDensity.Unit),
                    new MolarDensity(liquidMolarDensity.Value, liquidMolarDensity.Unit),
                    new MassEnergy(vaporEnthalpy.Value, vaporEnthalpy.Unit),
                    new MassDensity(vaporDensity.Value, vaporDensity.Unit),
                    new MolarDensity(vaporMolarDensity.Value, vaporMolarDensity.Unit)
                );

                curve.Add(vlePoint);
            }

            return curve;
        }
        private static IFacadeStream? GetFirstAvailableStream(SolverColumn column)
        {
            if (column.Feeds != null && column.Feeds.Any())
                return column.Feeds.First();

            if (column.RefluxInlet != null) return column.RefluxInlet;
            if (column.VaporInlet != null) return column.VaporInlet;
            if (column.VaporOutlet != null) return column.VaporOutlet;
            if (column.BottomOutlet != null) return column.BottomOutlet;

            if (column.SideDraws != null && column.SideDraws.Any())
                return column.SideDraws.First();

            return null;
        }
    }
}
