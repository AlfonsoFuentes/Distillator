using Shared.ProcessFlowDiagram;

//namespace Client.Services.RoutingStrategies
//{
//    public interface ITargetEntryStrategy
//    {
      
//        PhaseResult Calculate(RoutingRequest req);
//    }
//    public class TargetEntryRight : ITargetEntryStrategy
//    {
//        public PortDirection Direction => PortDirection.Right;
//        public PhaseResult Calculate(RoutingRequest req)
//        {
//            var points = new List<CanvasPoint> { req.B };

//            // 1. Stub: 30px a la derecha del puerto
//            var s1 = new CanvasPoint(req.B.X + 30, req.B.Y);
//            points.Add(s1);

//            // 2. ¿De dónde viene A?
//            // Si A está arriba (A.Y < B.Y), lo esperamos por ARRIBA.
//            // Si A está abajo (A.Y > B.Y), lo esperamos por ABAJO.
//            double safeY = (req.A.Y < req.B.Y)
//                ? req.BEquipPos.Y - 30                // Borde superior - margen
//                : req.BEquipPos.Y + req.BHeight + 30; // Borde inferior + margen

//            var reception = new CanvasPoint(s1.X, safeY);
//            points.Add(reception);

//            return new PhaseResult(points, reception);
//        }
//    }
//    public class TargetEntryTop : ITargetEntryStrategy
//    {
//        public PhaseResult Calculate(RoutingRequest req)
//        {
//            var points = new List<CanvasPoint> { req.B };

//            // Solo subimos 30px (Y disminuye)
//            var reception = new CanvasPoint(req.B.X, req.B.Y - 30);
//            points.Add(reception);

//            return new PhaseResult(points, reception);
//        }
//    }
//    public class TargetEntryBottom : ITargetEntryStrategy
//    {
//        public PhaseResult Calculate(RoutingRequest req)
//        {
//            var points = new List<CanvasPoint> { req.B };

//            // Solo bajamos 30px (Y aumenta)
//            var reception = new CanvasPoint(req.B.X, req.B.Y + 30);
//            points.Add(reception);

//            return new PhaseResult(points, reception);
//        }
//    }
//    public class TargetEntryLeft : ITargetEntryStrategy
//    {
//        public PhaseResult Calculate(RoutingRequest req)
//        {
//            var points = new List<CanvasPoint> { req.B };

//            // 1. Stub: 30px hacia la izquierda (Y se mantiene, X disminuye)
//            var reception = new CanvasPoint(req.B.X - 30, req.B.Y);
//            points.Add(reception);

//            return new PhaseResult(points, reception);
//        }
//    }
//}