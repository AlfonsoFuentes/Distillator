using Client.Services.EquipmentManagers;
using Shared.ProcessFlowDiagram;
using Shared.ProcessFlowDiagram.Pipes;
using System.Text;
using static Client.Pages.Home;

namespace Client.Services.RoutingStrategies
{
   

    // --- 2. EL FACTORY (El Normalizador) ---
    //public static class RoutingRequestFactory
    //{
    //    public static RoutingRequest? Create(PipeVisualElement pipe, bool isDraft, WorkspaceManager wm)
    //    {
    //        if (pipe.SourceElement == null) return null;

    //        // Datos del Puerto Origen Real
    //        var sCoords = pipe.SourceElement.GetAbsolutePortCoordinates(pipe.SourcePortName);
    //        var sPoint = new CanvasPoint(sCoords.X, sCoords.Y);

    //        // Datos del Puerto Destino Real (o Mouse)
    //        CanvasPoint tPoint;
    //        PortDirection tDir;
    //        double tW = 0, tH = 0;
    //        CanvasPoint tEquipPos;

    //        if (isDraft)
    //        {
    //            tPoint = new CanvasPoint(wm.DraftMouseLogicalX, wm.DraftMouseLogicalY);
    //            tDir = PortDirection.Left;
    //            tEquipPos = tPoint;
    //        }
    //        else
    //        {
    //            if (pipe.TargetElement == null) return null;
    //            var eCoords = pipe.TargetElement.GetAbsolutePortCoordinates(pipe.TargetPortName);
    //            tPoint = new CanvasPoint(eCoords.X, eCoords.Y);
    //            tDir = eCoords.Direction;
    //            tW = pipe.TargetElement.Width;
    //            tH = pipe.TargetElement.Height;
    //            tEquipPos = new CanvasPoint(pipe.TargetElement.X, pipe.TargetElement.Y);
    //        }

    //        // --- LA REGLA DE ORO: ¿Quién está a la izquierda? ---
    //        bool swap = sPoint.X > tPoint.X;

    //        var obstacles = ((IEnumerable<IVisualElement>)wm.Elements)
    //            .Where(e => e.Id != pipe.SourceElement.Id && (pipe.TargetElement == null || e.Id != pipe.TargetElement.Id));

    //        if (!swap)
    //        {
    //            return new RoutingRequest(
    //                sPoint, sCoords.Direction, new CanvasPoint(pipe.SourceElement.X, pipe.SourceElement.Y), pipe.SourceElement.Width, pipe.SourceElement.Height,
    //                tPoint, tDir, tEquipPos, tW, tH, obstacles);
    //        }
    //        else
    //        {
    //            // Intercambiamos: El original "Target" ahora es "A" (izquierda)
    //            return new RoutingRequest(
    //                tPoint, tDir, tEquipPos, tW, tH,
    //                sPoint, sCoords.Direction, new CanvasPoint(pipe.SourceElement.X, pipe.SourceElement.Y), pipe.SourceElement.Width, pipe.SourceElement.Height,
    //                obstacles);
    //        }
    //    }
    //}
    //public static class SvgRouteFormatter
    //{
    //    public static SvgRenderData FormatSinglePath(List<CanvasPoint> points)
    //    {
    //        if (points == null || points.Count < 2) return new SvgRenderData("");

    //        // Construimos el string "M x y L x y..."
    //        var sb = new StringBuilder();
    //        sb.Append($"M {points[0].X} {points[0].Y}");

    //        for (int i = 1; i < points.Count; i++)
    //        {
    //            sb.Append($" L {points[i].X} {points[i].Y}");
    //        }

    //        string pathD = sb.ToString();

    //        // Calculamos el centro de la tubería para la etiqueta (Label)
    //        var midIdx = points.Count / 2;
    //        var labelPt = points[midIdx];

    //        return new SvgRenderData(pathD, labelPt.X, labelPt.Y);
    //    }
    //}

    //// El nuevo modelo de datos para la UI
    //public record SvgRenderData(string MainPath, double LabelX = 0, double LabelY = 0, bool LabelIsVertical = false);
    //public class ObstacleAvoidanceStrategy : IRoutingStrategy
    //{
    //    public int Priority => 1; // Máxima prioridad

    //    public bool CanHandle(RoutingRequest req)
    //    {
    //        // Verificamos si la línea recta entre el origen y el fin choca con algo
    //        return CollisionPhysics.IsObstructed(req.Source, req.Target, req.Obstacles);
    //    }

    //    public List<CanvasPoint> Calculate(RoutingRequest req)
    //    {
    //        // Aquí implementamos la lógica de "Rodeo":
    //        // 1. Buscamos el equipo que estorba.
    //        // 2. Calculamos un punto de paso (Waypoint) por arriba o por debajo del equipo.
    //        // 3. Devolvemos una ruta de 5 o 6 puntos que "abraza" el obstáculo.

    //        // [Implementación de Waypoints basada en el BoundingBox del equipo]
    //        return RouteAroundObstacle(req);
    //    }
    //    private List<CanvasPoint> RouteAroundObstacle(RoutingRequest req)
    //    {
    //        double stubOffset = 30;
    //        double clearance = 25; // Los píxeles de "aire" para no rozar el dibujo del equipo

    //        var s1 = RoutingMath.GetOffset(req.Source, req.SourceDir, stubOffset);
    //        var s2 = RoutingMath.GetOffset(req.Target, req.TargetDir, stubOffset);

    //        // 1. Identificar contra QUÉ equipo nos estamos chocando
    //        var obstacle = req.Obstacles.FirstOrDefault(obs =>
    //            CollisionPhysics.IsObstructed(s1, s2, new[] { obs }));

    //        // Si por algún milagro ya no hay obstáculo, usamos el ruteo directo
    //        if (obstacle == null)
    //        {
    //            return new DirectZStrategy().Calculate(req);
    //        }

    //        // 2. Definir la "Caja de Seguridad" (El campo de fuerza alrededor del equipo)
    //        double safeTop = obstacle.Y - clearance;
    //        double safeBottom = obstacle.Y + obstacle.Height + clearance;
    //        double safeLeft = obstacle.X - clearance;
    //        double safeRight = obstacle.X + obstacle.Width + clearance;

    //        var pts = new List<CanvasPoint> { req.Source, s1 };

    //        // 3. Trazar la ruta de desvío
    //        if (req.SourceDir == PortDirection.Left || req.SourceDir == PortDirection.Right)
    //        {
    //            // -- RUTEO HORIZONTAL --
    //            // Decidimos si pasamos por el techo o por el piso (el camino más corto)
    //            double distToTop = Math.Abs(s1.Y - safeTop);
    //            double distToBottom = Math.Abs(s1.Y - safeBottom);
    //            double bypassY = (distToTop <= distToBottom) ? safeTop : safeBottom;

    //            // Armamos el "puente" que salta el equipo
    //            pts.Add(new CanvasPoint(s1.X, bypassY));  // Subimos o Bajamos
    //            pts.Add(new CanvasPoint(s2.X, bypassY));  // Cruzamos hacia el destino
    //        }
    //        else
    //        {
    //            // -- RUTEO VERTICAL --
    //            // Decidimos si lo esquivamos por la izquierda o por la derecha
    //            double distToLeft = Math.Abs(s1.X - safeLeft);
    //            double distToRight = Math.Abs(s1.X - safeRight);
    //            double bypassX = (distToLeft <= distToRight) ? safeLeft : safeRight;

    //            // Armamos la "C" que rodea el equipo
    //            pts.Add(new CanvasPoint(bypassX, s1.Y));  // Nos abrimos a un lado
    //            pts.Add(new CanvasPoint(bypassX, s2.Y));  // Bajamos o Subimos cruzando el equipo
    //        }

    //        // 4. Volvemos al cauce normal
    //        pts.Add(s2);
    //        pts.Add(req.Target);

    //        return pts;
    //    }
    //}
    //public class DirectZStrategy : IRoutingStrategy
    //{
    //    public int Priority => 3; // Baja prioridad: Asume que no hay obstáculos ni rodeos extraños

    //    public bool CanHandle(RoutingRequest req)
    //    {
    //        bool sourceIsHorz = req.SourceDir == PortDirection.Left || req.SourceDir == PortDirection.Right;
    //        bool targetIsHorz = req.TargetDir == PortDirection.Left || req.TargetDir == PortDirection.Right;

    //        // Si ambos son horizontales o ambos son verticales, usamos esta estrategia
    //        return sourceIsHorz == targetIsHorz;
    //    }

    //    public List<CanvasPoint> Calculate(RoutingRequest req)
    //    {
    //        double stubOffset = 30;
    //        var s1 = RoutingMath.GetOffset(req.Source, req.SourceDir, stubOffset);
    //        var s2 = RoutingMath.GetOffset(req.Target, req.TargetDir, stubOffset);

    //        var pts = new List<CanvasPoint> { req.Source, s1 };

    //        if (req.SourceDir == PortDirection.Left || req.SourceDir == PortDirection.Right)
    //        {
    //            // Eje Horizontal: El quiebre se hace partiendo la distancia X a la mitad
    //            double midX = s1.X + (s2.X - s1.X) / 2.0;
    //            pts.Add(new CanvasPoint(midX, s1.Y));
    //            pts.Add(new CanvasPoint(midX, s2.Y));
    //        }
    //        else
    //        {
    //            // Eje Vertical (Top/Bottom): El quiebre se hace partiendo la distancia Y a la mitad
    //            double midY = s1.Y + (s2.Y - s1.Y) / 2.0;
    //            pts.Add(new CanvasPoint(s1.X, midY));
    //            pts.Add(new CanvasPoint(s2.X, midY));
    //        }

    //        pts.Add(s2);
    //        pts.Add(req.Target);
    //        return pts;
    //    }
    //}
    //public class PerpendicularLStrategy : IRoutingStrategy
    //{
    //    public int Priority => 3; // Prioridad estándar

    //    public bool CanHandle(RoutingRequest req)
    //    {
    //        // Verificamos si las direcciones son perpendiculares
    //        bool sourceIsHorz = req.SourceDir == PortDirection.Left || req.SourceDir == PortDirection.Right;
    //        bool targetIsHorz = req.TargetDir == PortDirection.Left || req.TargetDir == PortDirection.Right;

    //        // Retorna true solo si uno es horizontal y el otro es vertical
    //        return sourceIsHorz != targetIsHorz;
    //    }

    //    public List<CanvasPoint> Calculate(RoutingRequest req)
    //    {
    //        double stubOffset = 30; // El "aire" que le damos al salir del equipo

    //        // 1. Calculamos los puntos de salida/entrada rectos (Stubs)
    //        var s1 = RoutingMath.GetOffset(req.Source, req.SourceDir, stubOffset);
    //        var s2 = RoutingMath.GetOffset(req.Target, req.TargetDir, stubOffset);

    //        var pts = new List<CanvasPoint> { req.Source, s1 };

    //        // 2. Calculamos la Esquina (Intersection Corner) de la "L"
    //        if (req.SourceDir == PortDirection.Top || req.SourceDir == PortDirection.Bottom)
    //        {
    //            // Si la salida original es Vertical (Arriba/Abajo), 
    //            // la línea proyecta hacia arriba/abajo (mantiene X) y choca con la Y del destino.
    //            pts.Add(new CanvasPoint(s1.X, s2.Y));
    //        }
    //        else
    //        {
    //            // Si la salida original es Horizontal (Derecha/Izquierda), 
    //            // la línea proyecta a los lados (mantiene Y) y choca con la X del destino.
    //            pts.Add(new CanvasPoint(s2.X, s1.Y));
    //        }

    //        // 3. Cerramos la ruta hacia el destino
    //        pts.Add(s2);
    //        pts.Add(req.Target);

    //        return pts;
    //    }
    //}
    //public class BackwardsZStrategy : IRoutingStrategy
    //{
    //    public int Priority => 2; // Prioridad Media: Se ejecuta ANTES que la Z normal (Priority 3)

    //    public bool CanHandle(RoutingRequest req)
    //    {
    //        var s1 = RoutingMath.GetOffset(req.Source, req.SourceDir);
    //        var s2 = RoutingMath.GetOffset(req.Target, req.TargetDir);

    //        // Detectamos si es el "Caso 2" (se dan la espalda horizontalmente)
    //        bool backwardsHorizontal = (req.SourceDir == PortDirection.Right && req.TargetDir == PortDirection.Left && s1.X >= s2.X) ||
    //                                   (req.SourceDir == PortDirection.Left && req.TargetDir == PortDirection.Right && s1.X <= s2.X);

    //        // O si se dan la espalda verticalmente
    //        bool backwardsVertical = (req.SourceDir == PortDirection.Bottom && req.TargetDir == PortDirection.Top && s1.Y >= s2.Y) ||
    //                                 (req.SourceDir == PortDirection.Top && req.TargetDir == PortDirection.Bottom && s1.Y <= s2.Y);

    //        return backwardsHorizontal || backwardsVertical;
    //    }

    //    public List<CanvasPoint> Calculate(RoutingRequest req)
    //    {
    //        // 1. Obtenemos los Stubs (usando la constante de 30px por defecto)
    //        var s1 = RoutingMath.GetOffset(req.Source, req.SourceDir);
    //        var s2 = RoutingMath.GetOffset(req.Target, req.TargetDir);

    //        var pts = new List<CanvasPoint> { req.Source, s1 };

    //        if (req.SourceDir == PortDirection.Left || req.SourceDir == PortDirection.Right)
    //        {
    //            // --- CASO 2 (Tu dibujo rojo) ---
    //            // Quebramos en Y (subimos/bajamos) hasta la mitad del espacio entre S-101 y P-101
    //            double midY = s1.Y + (s2.Y - s1.Y) / 2.0;

    //            // Subimos hasta la mitad
    //            pts.Add(new CanvasPoint(s1.X, midY));
    //            // Cruzamos todo el eje X hacia la izquierda (llegando hasta el s2.X que está a la izquierda de la bomba)
    //            pts.Add(new CanvasPoint(s2.X, midY));
    //        }
    //        else
    //        {
    //            // --- CASO VERTICAL ---
    //            // Quebramos en X (rodeamos) hasta la mitad
    //            double midX = s1.X + (s2.X - s1.X) / 2.0;

    //            pts.Add(new CanvasPoint(midX, s1.Y));
    //            pts.Add(new CanvasPoint(midX, s2.Y));
    //        }

    //        // 2. Cerramos la ruta subiendo al puerto y entrando
    //        pts.Add(s2);
    //        pts.Add(req.Target);

    //        return pts;
    //    }
    //}
    //public class BackwardsLStrategy : IRoutingStrategy
    //{
    //    public int Priority => 2; // Prioridad Media: Se ejecuta ANTES que la "L" normal (Priority 3)

    //    public bool CanHandle(RoutingRequest req)
    //    {
    //        bool sourceIsHorz = req.SourceDir == PortDirection.Left || req.SourceDir == PortDirection.Right;
    //        bool targetIsHorz = req.TargetDir == PortDirection.Left || req.TargetDir == PortDirection.Right;

    //        if (sourceIsHorz == targetIsHorz) return false; // Solo maneja conexiones perpendiculares

    //        var s1 = RoutingMath.GetOffset(req.Source, req.SourceDir);
    //        var s2 = RoutingMath.GetOffset(req.Target, req.TargetDir);

    //        // Verificamos si la esquina de la "L" natural nos hace retroceder contra la corriente
    //        if (!sourceIsHorz)
    //        {
    //            // Origen Vertical (Como tu S-101 apuntando abajo)
    //            bool backwardsY = (req.SourceDir == PortDirection.Top && s2.Y > s1.Y) ||
    //                              (req.SourceDir == PortDirection.Bottom && s2.Y < s1.Y);

    //            bool backwardsX = (req.TargetDir == PortDirection.Left && s1.X > s2.X) ||
    //                              (req.TargetDir == PortDirection.Right && s1.X < s2.X);

    //            return backwardsX || backwardsY;
    //        }
    //        else
    //        {
    //            // Origen Horizontal
    //            bool backwardsX = (req.SourceDir == PortDirection.Left && s2.X > s1.X) ||
    //                              (req.SourceDir == PortDirection.Right && s2.X < s1.X);

    //            bool backwardsY = (req.TargetDir == PortDirection.Top && s1.Y > s2.Y) ||
    //                              (req.TargetDir == PortDirection.Bottom && s1.Y < s2.Y);

    //            return backwardsX || backwardsY;
    //        }
    //    }

    //    public List<CanvasPoint> Calculate(RoutingRequest req)
    //    {
    //        var s1 = RoutingMath.GetOffset(req.Source, req.SourceDir);
    //        var s2 = RoutingMath.GetOffset(req.Target, req.TargetDir);

    //        var pts = new List<CanvasPoint> { req.Source, s1 };

    //        bool sourceIsHorz = req.SourceDir == PortDirection.Left || req.SourceDir == PortDirection.Right;

    //        // 🚩 LA MAGIA (Exactamente tu línea roja):
    //        // En lugar de quebrar primero en el eje natural, quebramos en el eje opuesto.
    //        if (!sourceIsHorz)
    //        {
    //            // En tu Caso 3: Viajamos horizontalmente primero hasta alinearnos con la izquierda de la bomba.
    //            pts.Add(new CanvasPoint(s2.X, s1.Y));
    //        }
    //        else
    //        {
    //            // Si el caso fuera al revés: Viajamos verticalmente primero.
    //            pts.Add(new CanvasPoint(s1.X, s2.Y));
    //        }

    //        pts.Add(s2);
    //        pts.Add(req.Target);

    //        return pts;
    //    }
    //}
    //public class RightToRightStrategy : IRoutingStrategy
    //{
    //    public int Priority => 2;

    //    public bool CanHandle(RoutingRequest req)
    //    {
    //        // Regla estricta: Solo Salida Derecha y Entrada Derecha
    //        return req.SourceDir == PortDirection.Right && req.TargetDir == PortDirection.Right;
    //    }

    //    public List<CanvasPoint> Calculate(RoutingRequest req)
    //    {
    //        var s1 = RoutingMath.GetOffset(req.Source, req.SourceDir);
    //        var s2 = RoutingMath.GetOffset(req.Target, req.TargetDir);

    //        // Como ambos puertos miran a la derecha, la tubería debe avanzar 
    //        // hasta el punto más a la derecha de los dos para no chocar al devolverse.
    //        double maxX = Math.Max(s1.X, s2.X);

    //        return new List<CanvasPoint>
    //    {
    //        req.Source,
    //        s1,
    //        new CanvasPoint(maxX, s1.Y), // 1. Avanza hasta el límite derecho seguro
    //        new CanvasPoint(maxX, s2.Y), // 2. Baja o sube hasta el nivel del puerto de destino
    //        s2,                          // 3. Se devuelve a la izquierda hacia el stub
    //        req.Target
    //    };
    //    }
    //}
    //public class RightToLeftStrategy : IRoutingStrategy
    //{
    //    public int Priority => 2;

    //    public bool CanHandle(RoutingRequest req)
    //    {
    //        // 🚩 REGLA ESTRICTA: Solo Salida Derecha y Entrada Izquierda
    //        return req.SourceDir == PortDirection.Right && req.TargetDir == PortDirection.Left;
    //    }

    //    public List<CanvasPoint> Calculate(RoutingRequest req)
    //    {
    //        var s1 = RoutingMath.GetOffset(req.Source, req.SourceDir);
    //        var s2 = RoutingMath.GetOffset(req.Target, req.TargetDir);

    //        var pts = new List<CanvasPoint> { req.Source, s1 };

    //        // Evaluamos físicamente dónde está el destino respecto al origen
    //        if (s1.X < s2.X)
    //        {
    //            // --- CASO A: Destino Adelante (Flujo Normal) ---
    //            // Trazamos una "Z" clásica partiendo la distancia horizontal por la mitad
    //            double midX = s1.X + (s2.X - s1.X) / 2.0;
    //            pts.Add(new CanvasPoint(midX, s1.Y));
    //            pts.Add(new CanvasPoint(midX, s2.Y));
    //        }
    //        else
    //        {
    //            // --- CASO B: Destino Atrás (Reversa) ---
    //            // Trazamos una "S" partiendo la distancia vertical por la mitad para no chocar
    //            double midY = s1.Y + (s2.Y - s1.Y) / 2.0;
    //            pts.Add(new CanvasPoint(s1.X, midY));
    //            pts.Add(new CanvasPoint(s2.X, midY));
    //        }

    //        pts.Add(s2);
    //        pts.Add(req.Target);

    //        return pts;
    //    }
    //}
    //public class RightToTopStrategy : IRoutingStrategy
    //{
    //    public int Priority => 2;

    //    public bool CanHandle(RoutingRequest req) =>
    //        req.SourceDir == PortDirection.Right && req.TargetDir == PortDirection.Top;

    //    public List<CanvasPoint> Calculate(RoutingRequest req)
    //    {
    //        var s1 = RoutingMath.GetOffset(req.Source, req.SourceDir);
    //        var s2 = RoutingMath.GetOffset(req.Target, req.TargetDir);

    //        // Sin importar el cuadrante, primero alcanzamos la altitud segura del destino (s2.Y)
    //        // viajando por nuestro pasillo seguro derecho (s1.X)
    //        return new List<CanvasPoint>
    //    {
    //        req.Source,
    //        s1,
    //        new CanvasPoint(s1.X, s2.Y), // Quiebre vertical (sube o baja seguro)
    //        s2,                          // Quiebre horizontal (avanza o retrocede seguro)
    //        req.Target
    //    };
    //    }
    //}
    //public class RightToBottomStrategy : IRoutingStrategy
    //{
    //    public int Priority => 2;

    //    public bool CanHandle(RoutingRequest req) =>
    //        req.SourceDir == PortDirection.Right && req.TargetDir == PortDirection.Bottom;

    //    public List<CanvasPoint> Calculate(RoutingRequest req)
    //    {
    //        var s1 = RoutingMath.GetOffset(req.Source, req.SourceDir);
    //        var s2 = RoutingMath.GetOffset(req.Target, req.TargetDir);

    //        return new List<CanvasPoint>
    //    {
    //        req.Source,
    //        s1,
    //        new CanvasPoint(s1.X, s2.Y), // Quiebre vertical hacia la profundidad segura
    //        s2,                          // Quiebre horizontal hacia la entrada
    //        req.Target
    //    };
    //    }
    //}
    //public class LeftToRightStrategy : IRoutingStrategy
    //{
    //    public int Priority => 2;

    //    public bool CanHandle(RoutingRequest req)
    //    {
    //        return req.SourceDir == PortDirection.Left && req.TargetDir == PortDirection.Right;
    //    }

    //    public List<CanvasPoint> Calculate(RoutingRequest req)
    //    {
    //        var s1 = RoutingMath.GetOffset(req.Source, req.SourceDir); // 30px a la izquierda
    //        var s2 = RoutingMath.GetOffset(req.Target, req.TargetDir); // 30px a la derecha

    //        var pts = new List<CanvasPoint> { req.Source, s1 };

    //        // Evaluamos posición relativa
    //        if (s1.X > s2.X)
    //        {
    //            // --- CASO A: Destino Adelante (Más a la izquierda) ---
    //            // Trazamos una "Z" clásica
    //            double midX = s1.X + (s2.X - s1.X) / 2.0;
    //            pts.Add(new CanvasPoint(midX, s1.Y));
    //            pts.Add(new CanvasPoint(midX, s2.Y));
    //        }
    //        else
    //        {
    //            // --- CASO B: Destino Atrás (Se pasó a la derecha) ---
    //            // Trazamos una "S" envolvente por el centro
    //            double midY = s1.Y + (s2.Y - s1.Y) / 2.0;
    //            pts.Add(new CanvasPoint(s1.X, midY));
    //            pts.Add(new CanvasPoint(s2.X, midY));
    //        }

    //        pts.Add(s2);
    //        pts.Add(req.Target);

    //        return pts;
    //    }
    //}

    //public class LeftToLeftStrategy : IRoutingStrategy
    //{
    //    public int Priority => 2;

    //    public bool CanHandle(RoutingRequest req)
    //    {
    //        return req.SourceDir == PortDirection.Left && req.TargetDir == PortDirection.Left;
    //    }

    //    public List<CanvasPoint> Calculate(RoutingRequest req)
    //    {
    //        var s1 = RoutingMath.GetOffset(req.Source, req.SourceDir); // 30px a la izquierda
    //        var s2 = RoutingMath.GetOffset(req.Target, req.TargetDir); // 30px a la izquierda del destino

    //        // Como ambos miran a la izquierda, debemos avanzar hasta el punto 
    //        // MAS a la izquierda (el X mínimo) para dar la vuelta de forma segura.
    //        double minX = Math.Min(s1.X, s2.X);

    //        return new List<CanvasPoint>
    //    {
    //        req.Source,
    //        s1,
    //        new CanvasPoint(minX, s1.Y), // 1. Avanza a la "pared" izquierda
    //        new CanvasPoint(minX, s2.Y), // 2. Baja o sube al nivel del destino
    //        s2,                          // 3. Regresa a la derecha hacia el stub
    //        req.Target
    //    };
    //    }
    //}

    //public class LeftToTopStrategy : IRoutingStrategy
    //{
    //    public int Priority => 2;

    //    public bool CanHandle(RoutingRequest req)
    //    {
    //        return req.SourceDir == PortDirection.Left && req.TargetDir == PortDirection.Top;
    //    }

    //    public List<CanvasPoint> Calculate(RoutingRequest req)
    //    {
    //        var s1 = RoutingMath.GetOffset(req.Source, req.SourceDir); // 30px a la izquierda
    //        var s2 = RoutingMath.GetOffset(req.Target, req.TargetDir); // 30px arriba del destino

    //        // Regla Holística: Subir/Bajar por la columna segura (s1.X) 
    //        // y luego cruzar por la fila segura (s2.Y)
    //        return new List<CanvasPoint>
    //    {
    //        req.Source,
    //        s1,
    //        new CanvasPoint(s1.X, s2.Y), // Quiebre mágico
    //        s2,
    //        req.Target
    //    };
    //    }
    //}
    //public class LeftToBottomStrategy : IRoutingStrategy
    //{
    //    public int Priority => 2;

    //    public bool CanHandle(RoutingRequest req)
    //    {
    //        return req.SourceDir == PortDirection.Left && req.TargetDir == PortDirection.Bottom;
    //    }

    //    public List<CanvasPoint> Calculate(RoutingRequest req)
    //    {
    //        var s1 = RoutingMath.GetOffset(req.Source, req.SourceDir); // 30px a la izquierda
    //        var s2 = RoutingMath.GetOffset(req.Target, req.TargetDir); // 30px abajo del destino

    //        return new List<CanvasPoint>
    //    {
    //        req.Source,
    //        s1,
    //        new CanvasPoint(s1.X, s2.Y), // Quiebre mágico
    //        s2,
    //        req.Target
    //    };
    //    }
    //}
    //public class BottomToLeftStrategy : IRoutingStrategy
    //{
    //    public int Priority => 2;

    //    public bool CanHandle(RoutingRequest req)
    //    {
    //        return req.SourceDir == PortDirection.Bottom && req.TargetDir == PortDirection.Left;
    //    }

    //    public List<CanvasPoint> Calculate(RoutingRequest req)
    //    {
    //        var s1 = RoutingMath.GetOffset(req.Source, req.SourceDir); // 30px a la izquierda
    //        var s2 = RoutingMath.GetOffset(req.Target, req.TargetDir); // 30px abajo del destino

    //        return new List<CanvasPoint>
    //    {
    //        req.Source,
    //        s1,
    //        new CanvasPoint(s1.X, s2.Y), // Quiebre mágico
    //        s2,
    //        req.Target
    //    };
    //    }
    //}
    //public class TopToTopStrategy : IRoutingStrategy
    //{
    //    public int Priority => 2;

    //    public bool CanHandle(RoutingRequest req) =>
    //        req.SourceDir == PortDirection.Top && req.TargetDir == PortDirection.Top;

    //    public List<CanvasPoint> Calculate(RoutingRequest req)
    //    {
    //        var s1 = RoutingMath.GetOffset(req.Source, req.SourceDir); // 30px Arriba
    //        var s2 = RoutingMath.GetOffset(req.Target, req.TargetDir); // 30px Arriba

    //        // Buscamos la elevación más alta (el Y mínimo en el Canvas)
    //        double minY = Math.Min(s1.Y, s2.Y);

    //        return new List<CanvasPoint>
    //    {
    //        req.Source,
    //        s1,
    //        new CanvasPoint(s1.X, minY), // Sube hasta el puente aéreo seguro
    //        new CanvasPoint(s2.X, minY), // Cruza horizontalmente
    //        s2,                          // Baja hasta el stub
    //        req.Target
    //    };
    //    }
    //}
    //public class BottomToBottomStrategy : IRoutingStrategy
    //{
    //    public int Priority => 2;

    //    public bool CanHandle(RoutingRequest req) =>
    //        req.SourceDir == PortDirection.Bottom && req.TargetDir == PortDirection.Bottom;

    //    public List<CanvasPoint> Calculate(RoutingRequest req)
    //    {
    //        var s1 = RoutingMath.GetOffset(req.Source, req.SourceDir); // 30px Abajo
    //        var s2 = RoutingMath.GetOffset(req.Target, req.TargetDir); // 30px Abajo

    //        // Buscamos la profundidad más baja (el Y máximo en el Canvas)
    //        double maxY = Math.Max(s1.Y, s2.Y);

    //        return new List<CanvasPoint>
    //    {
    //        req.Source,
    //        s1,
    //        new CanvasPoint(s1.X, maxY), // Baja hasta el túnel seguro
    //        new CanvasPoint(s2.X, maxY), // Cruza horizontalmente
    //        s2,                          // Sube hasta el stub
    //        req.Target
    //    };
    //    }
    //}
    //public class TopToBottomStrategy : IRoutingStrategy
    //{
    //    public int Priority => 2;

    //    public bool CanHandle(RoutingRequest req) =>
    //        req.SourceDir == PortDirection.Top && req.TargetDir == PortDirection.Bottom;

    //    public List<CanvasPoint> Calculate(RoutingRequest req)
    //    {
    //        var s1 = RoutingMath.GetOffset(req.Source, req.SourceDir);
    //        var s2 = RoutingMath.GetOffset(req.Target, req.TargetDir);

    //        var pts = new List<CanvasPoint> { req.Source, s1 };

    //        // Si están muy cerca horizontalmente (alineados en columna), los esquivamos por la derecha
    //        if (Math.Abs(s1.X - s2.X) < 80)
    //        {
    //            double safeX = Math.Max(s1.X, s2.X) + 60; // Damos una vuelta en "C"
    //            pts.Add(new CanvasPoint(safeX, s1.Y));
    //            pts.Add(new CanvasPoint(safeX, s2.Y));
    //        }
    //        else
    //        {
    //            // Si hay espacio, cruzamos por el medio exacto
    //            double midX = s1.X + (s2.X - s1.X) / 2.0;
    //            pts.Add(new CanvasPoint(midX, s1.Y));
    //            pts.Add(new CanvasPoint(midX, s2.Y));
    //        }

    //        pts.Add(s2);
    //        pts.Add(req.Target);
    //        return pts;
    //    }
    //}
    //public class BottomToTopStrategy : IRoutingStrategy
    //{
    //    public int Priority => 2;

    //    public bool CanHandle(RoutingRequest req) =>
    //        req.SourceDir == PortDirection.Bottom && req.TargetDir == PortDirection.Top;

    //    public List<CanvasPoint> Calculate(RoutingRequest req)
    //    {
    //        var s1 = RoutingMath.GetOffset(req.Source, req.SourceDir);
    //        var s2 = RoutingMath.GetOffset(req.Target, req.TargetDir);

    //        var pts = new List<CanvasPoint> { req.Source, s1 };

    //        if (Math.Abs(s1.X - s2.X) < 80)
    //        {
    //            double safeX = Math.Max(s1.X, s2.X) + 60;
    //            pts.Add(new CanvasPoint(safeX, s1.Y));
    //            pts.Add(new CanvasPoint(safeX, s2.Y));
    //        }
    //        else
    //        {
    //            double midX = s1.X + (s2.X - s1.X) / 2.0;
    //            pts.Add(new CanvasPoint(midX, s1.Y));
    //            pts.Add(new CanvasPoint(midX, s2.Y));
    //        }

    //        pts.Add(s2);
    //        pts.Add(req.Target);
    //        return pts;
    //    }
    //}
}
