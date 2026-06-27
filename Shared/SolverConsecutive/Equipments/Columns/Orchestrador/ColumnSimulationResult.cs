using Shared.PropertiesDtos.Methods;
using Shared.SolverQwen.Stream;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using UnitSystem;

namespace Shared.SolverConsecutive.Equipments.Columns.Orchestrador
{
    // ═══════════════════════════════════════════════════════════════════════════════
    // 🔹 DTOs INMUTABLES (Resultados para UI)
    // ═══════════════════════════════════════════════════════════════════════════════

    public sealed record ColumnResult
    {
        public required string ColumnName { get; init; }
        public required DistillationParameters DistillationParameters { get; init; }
        public required VLECurveResult VLECurve { get; init; }
        public required ImmutableList<StageResult> Stages { get; init; }
        public required McCabeThieleData McCabeThiele { get; init; }
        public required int FeedStage { get; init; }
        public required bool Success { get; init; }
        public required string ErrorMessage { get; init; }

        public double xD => DistillationParameters.xD;
        public double xB => DistillationParameters.xB;
        public double R => DistillationParameters.RefluxRatio.Value;
        public double q => DistillationParameters.FeedQuality;
    }

    public sealed record DistillationParameters
    {
        public required UnitLess RefluxRatio { get; init; }
        public required UnitLess MinRefluxRatio { get; init; }
        public required Percentage RefluxExcess { get; init; }
        public required UnitLess MinStages { get; init; }
        public required UnitLess TheoreticalStages { get; init; }
        public required double xD { get; init; }
        public required double xB { get; init; }
        public required double FeedQuality { get; init; }
        public required ImmutableList<double> RelativeVolatilities { get; init; }
        public required int LightKeyIndex { get; init; }
        public required int HeavyKeyIndex { get; init; }
    }

    public sealed record VLECurveResult
    {
        public required ImmutableList<VLEPointResult> Points { get; init; }
        public required Pressure Pressure { get; init; }
    }

    public sealed record VLEPointResult
    {
        public required double x { get; init; }
        public required double y { get; init; }
        public required Temperature Temperature { get; init; }
        public required Pressure Pressure { get; init; }
        public required MassEnergy LiquidEnthalpy { get; init; }
        public required MassEnergy VaporEnthalpy { get; init; }
        public required MassDensity LiquidDensity { get; init; }
        public required MassDensity VaporDensity { get; init; }
        public required MolarDensity LiquidMolarDensity { get; init; }
        public required MolarDensity VaporMolarDensity { get; init; }
    }

    public sealed record StageResult
    {
        public required int StageNumber { get; init; }
        public required bool IsFeedStage { get; init; }
        public required bool IsCondenser { get; init; }
        public required bool IsReboiler { get; init; }
        public required StageStreamResult Liquid { get; init; }
        public required StageStreamResult Vapor { get; init; }
    }

    public sealed record StageStreamResult
    {
        public required Temperature Temperature { get; init; }
        public required Pressure Pressure { get; init; }
        public required MassFlow MassFlow { get; init; }
        public required MolarFlow MolarFlow { get; init; }
        public required MassEnergy Enthalpy { get; init; }
        public required MassDensity Density { get; init; }
        public required ImmutableDictionary<Guid, StageStreamComponentResult> MolarComposition { get; init; }
    }

    public sealed record StageStreamComponentResult
    {
        public required string ComponentName { get; init; }
        public double MolarComposition { get; init; }
    }

    public class StreamSnapshot
    {
        public double Temperature { get; set; }
        public double Pressure { get; set; }
        public double MassFlow { get; set; }
        public double VaporFraction { get; set; }
        public Dictionary<string, double> Composition { get; set; } = new();

        public bool Equals(StreamSnapshot? other)
        {
            if (other == null) return false;

            if (Math.Abs(Temperature - other.Temperature) > 1e-6) return false;
            if (Math.Abs(Pressure - other.Pressure) > 1e-6) return false;
            if (Math.Abs(MassFlow - other.MassFlow) > 1e-6) return false;
            if (Math.Abs(VaporFraction - other.VaporFraction) > 1e-6) return false;

            if (Composition.Count != other.Composition.Count) return false;
            foreach (var kvp in Composition)
            {
                if (!other.Composition.TryGetValue(kvp.Key, out double otherValue)) return false;
                if (Math.Abs(kvp.Value - otherValue) > 1e-6) return false;
            }

            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as StreamSnapshot);
        public override int GetHashCode() => HashCode.Combine(Temperature, Pressure, MassFlow, VaporFraction);
    }

    public class ColumnSnapshot
    {
        public double TopPressure { get; set; }
        public double DeltaP { get; set; }
        public List<StreamSnapshot> Feeds { get; set; } = new();
        public StreamSnapshot? RefluxInlet { get; set; }
        public StreamSnapshot? VaporOutlet { get; set; }
        public StreamSnapshot? BottomOutlet { get; set; }
        public List<StreamSnapshot> SideDraws { get; set; } = new();

        public bool Equals(ColumnSnapshot? other)
        {
            if (other == null) return false;

            if (Math.Abs(TopPressure - other.TopPressure) > 1e-6) return false;
            if (Math.Abs(DeltaP - other.DeltaP) > 1e-6) return false;

            if (Feeds.Count != other.Feeds.Count) return false;
            for (int i = 0; i < Feeds.Count; i++)
            {
                if (!Feeds[i].Equals(other.Feeds[i])) return false;
            }

            if (!CompareNullableStreams(RefluxInlet, other.RefluxInlet)) return false;
            if (!CompareNullableStreams(VaporOutlet, other.VaporOutlet)) return false;
            if (!CompareNullableStreams(BottomOutlet, other.BottomOutlet)) return false;

            if (SideDraws.Count != other.SideDraws.Count) return false;
            for (int i = 0; i < SideDraws.Count; i++)
            {
                if (!SideDraws[i].Equals(other.SideDraws[i])) return false;
            }

            return true;
        }

        private static bool CompareNullableStreams(StreamSnapshot? a, StreamSnapshot? b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            return a.Equals(b);
        }

        public override bool Equals(object? obj) => Equals(obj as ColumnSnapshot);
        public override int GetHashCode() => HashCode.Combine(TopPressure, DeltaP);
    }





    // ═══════════════════════════════════════════════════════════════════════════════
    // 🔹 DTOs PARA MCCABE-THIELE (Backend puro, sin ChartPoint)
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Datos crudos listos para ser mapeados por la UI al gráfico de McCabe-Thiele.
    /// </summary>
    public sealed record McCabeThieleData
    {
        public required List<(double x, double y)> DiagonalLine { get; init; }
        public required List<(double x, double y)> VLECurve { get; init; }
        public required List<(double x, double y)> RectifyingLine { get; init; }
        public required List<(double x, double y)> StrippingLine { get; init; }
        //public required List<(double x, double y)> FeedLine { get; init; }
        public required List<StaircaseStep> StaircaseSteps { get; init; }
        public required List<MarkerPoint> Markers { get; init; }
        public required List<(double x, double y)> MinRefluxRectifyingLine { get; init; }
        public required List<(double x, double y)> MinRefluxStrippingLine { get; init; }
        public required double MinRefluxRatio { get; init; }

        // 🔥 TAREA 8: 3 series separadas de proyección
        public required List<(double x, double y)> ProjectionLinesD { get; init; }
        public required List<(double x, double y)> ProjectionLinesB { get; init; }
        public required List<(double x, double y)> ProjectionLinesF { get; init; }

        // 🔥 Título y subtítulo del gráfico
        public string ChartTitle { get; init; } = "McCabe-Thiele Diagram";
        public string ChartSubtitle { get; init; } = string.Empty;
        public List<(double x, double y)> ProjectionLinesFToX { get; set; } = new();
        public List<(double x, double y)> ProjectionLinesIntersectToY { get; set; } = new();
    }

    public sealed record StaircaseStep
    {
        public required int StageNumber { get; init; }
        public required List<(double x, double y)> Points { get; init; }
        public required string StageType { get; init; } // "Rectifying" o "Stripping"
    }

    public sealed record MarkerPoint
    {
        public required string Label { get; init; } // "D", "B", "F"
        public required double X { get; init; }
        public required double Y { get; init; }
        public required string Description { get; init; }
        public Dictionary<string, string> TooltipData { get; init; } = null!;
    }
}