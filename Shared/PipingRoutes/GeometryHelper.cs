using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.PipingRoutes
{
    public static class GeometryHelper
    {
        public struct Segment
        {
            public CanvasPoint P1 { get; }
            public CanvasPoint P2 { get; }

            // 🔥 Tolerancia aumentada para absorber errores de zoom/pan decimal
            private const double TOLERANCE = 1.0;

            public Segment(CanvasPoint p1, CanvasPoint p2) { P1 = p1; P2 = p2; }

            public double X1 => P1.X; public double Y1 => P1.Y;
            public double X2 => P2.X; public double Y2 => P2.Y;

            public bool IsVertical => Math.Abs(X1 - X2) <= TOLERANCE;
            public bool IsHorizontal => Math.Abs(Y1 - Y2) <= TOLERANCE;

            public bool IntersectsHorizontal(Segment hSeg)
            {
                if (!IsVertical || !hSeg.IsHorizontal) return false;

                // Usamos el centro exacto del segmento para evitar falsos negativos
                double vX = (X1 + X2) / 2.0;
                double hY = (hSeg.Y1 + hSeg.Y2) / 2.0;

                // Verificamos si se cruzan incluyendo la tolerancia y permitiendo tocar los bordes (>=)
                bool xOverlap = vX >= Math.Min(hSeg.X1, hSeg.X2) - TOLERANCE &&
                                vX <= Math.Max(hSeg.X1, hSeg.X2) + TOLERANCE;

                bool yOverlap = hY >= Math.Min(Y1, Y2) - TOLERANCE &&
                                hY <= Math.Max(Y1, Y2) + TOLERANCE;

                return xOverlap && yOverlap;
            }
        }
    }
}
