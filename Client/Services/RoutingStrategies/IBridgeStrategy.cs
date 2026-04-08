using Shared.ProcessFlowDiagram;

//namespace Client.Services.RoutingStrategies
//{
//    public interface IBridgeStrategy
//    {
//        int Priority { get; }
//        bool CanHandle(CanvasPoint h, CanvasPoint r, RoutingRequest req);
//        List<CanvasPoint> Calculate(CanvasPoint h, CanvasPoint r, RoutingRequest req);
//    }
//    public class VerticalFirstLStrategy : IBridgeStrategy
//    {
//        public int Priority => 2;

//        public bool CanHandle(CanvasPoint h, CanvasPoint r, RoutingRequest req)
//        {
//            // Se activa SIEMPRE que la tubería venga del techo o del piso de A
//            return req.ADir == PortDirection.Top || req.ADir == PortDirection.Bottom;
//        }

//        public List<CanvasPoint> Calculate(CanvasPoint h, CanvasPoint r, RoutingRequest req)
//        {
//            // El Codo Azul: Viajamos en Y primero (mantenemos h.X), hasta llegar a la altura de R (r.Y)
//            var corner = new CanvasPoint(h.X, r.Y);
//            return new List<CanvasPoint> { h, corner, r };
//        }
//    }
//    public class HorizontalFirstLStrategy : IBridgeStrategy
//    {
//        public int Priority => 3;

//        public bool CanHandle(CanvasPoint h, CanvasPoint r, RoutingRequest req)
//        {
//            // Se activa SIEMPRE que la tubería venga de los costados de A
//            return req.ADir == PortDirection.Left || req.ADir == PortDirection.Right;
//        }

//        public List<CanvasPoint> Calculate(CanvasPoint h, CanvasPoint r, RoutingRequest req)
//        {
//            // El Codo Clásico: Viajamos en X primero, luego caemos/subimos a R
//            var corner = new CanvasPoint(r.X, h.Y);
//            return new List<CanvasPoint> { h, corner, r };
//        }
//    }
//    // --- 2. EL CASO INVERSO / SOLAPE (Adaptada - Mismo Priority 2) ---
//    public class DirectZBridgeStrategy : IBridgeStrategy
//    {
//        public int Priority => 1; // Máxima prioridad, se evalúa primero

//        public bool CanHandle(CanvasPoint h, CanvasPoint r, RoutingRequest req)
//        {
//            // Si la distancia horizontal entre los puntos es menor a 60px, 
//            // una "L" se vería amontonada. Mejor hacemos una "Z" para rodear.
//            return Math.Abs(h.X - r.X) < 60;
//        }

//        public List<CanvasPoint> Calculate(CanvasPoint h, CanvasPoint r, RoutingRequest req)
//        {
//            double midX = h.X + (r.X - h.X) / 2.0;
//            return new List<CanvasPoint> {
//            h,
//            new CanvasPoint(midX, h.Y),
//            new CanvasPoint(midX, r.Y),
//            r
//        };
//        }
//    }
//    public class CloseZBridgeStrategy : IBridgeStrategy
//    {
//        public int Priority => 1; // Ataja el error antes que las L

//        public bool CanHandle(CanvasPoint h, CanvasPoint r, RoutingRequest req)
//        {
//            // Si la distancia horizontal es muy pequeña (ej. < 60px)
//            return Math.Abs(h.X - r.X) < 60;
//        }

//        public List<CanvasPoint> Calculate(CanvasPoint h, CanvasPoint r, RoutingRequest req)
//        {
//            double midX = h.X + (r.X - h.X) / 2.0;
//            return new List<CanvasPoint> { h, new CanvasPoint(midX, h.Y), new CanvasPoint(midX, r.Y), r };
//        }
//    }
//    // --- 3. LA "Z" ESTÁNDAR (Adaptada - MOVIDA A PRIORIDAD 4) ---

//    public static class BridgeFactory
//    {
//        private static readonly List<IBridgeStrategy> _strategies = new List<IBridgeStrategy>
//    {
//        new DirectZBridgeStrategy(),       // Prioridad 1: Casos amontonados
//        new VerticalFirstLStrategy(),      // Prioridad 2: Casos como tus líneas azules
//        new HorizontalFirstLStrategy() ,    // Prioridad 3: El resto
//        new CloseZBridgeStrategy() ,
//    };

//        public static List<CanvasPoint> Connect(CanvasPoint h, CanvasPoint r, RoutingRequest req)
//        {
//            var strategy = _strategies
//                .OrderBy(s => s.Priority)
//                .First(s => s.CanHandle(h, r, req));

//            return strategy.Calculate(h, r, req);
//        }
//    }
//}
