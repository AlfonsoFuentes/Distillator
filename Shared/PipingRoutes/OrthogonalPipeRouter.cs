using Shared.ProcessFlowDiagram;

namespace Shared.PipingRoutes
{
    public enum OrthogonalEndpointOwnerKind
    {
        Equipment,
        Stream,
        OffPageConnector,
        Other
    }

    public sealed record OrthogonalRoutingEndpoint(
        CanvasPoint Port,
        PortDirection Direction,
        ScreenBoundingBox EquipmentBox,
        PortType PortType = PortType.Outlet,
        OrthogonalEndpointOwnerKind OwnerKind = OrthogonalEndpointOwnerKind.Equipment);

    public sealed record OrthogonalRoutingRequest(
        OrthogonalRoutingEndpoint Source,
        OrthogonalRoutingEndpoint Target,
        IReadOnlyCollection<ScreenBoundingBox> Obstacles);

    public static class OrthogonalPipeRouter
    {
        private const double EscapeDistance = 30.0;
        private const double BlockMargin = 8.0;
        private const double StreamBlockMargin = 14.0;
        private const double LaneMargin = 30.0;
        private const int MaxLanesPerAxis = 12;
        private const double Epsilon = 0.1;

        public static List<CanvasPoint> Route(OrthogonalRoutingRequest request)
        {
            var sourceEscape = Project(request.Source.Port, request.Source.Direction, EscapeDistance);
            var targetApproach = Project(request.Target.Port, request.Target.Direction, EscapeDistance);

            var blocks = BuildBlocks(request).ToList();
            var routeBlocks = BuildRouteBlocks(request).ToList();
            var route = SelectCompactRoute(sourceEscape, targetApproach, routeBlocks, request)
                ?? SelectBestRoute(
                BuildCandidates(sourceEscape, targetApproach, routeBlocks),
                routeBlocks,
                request,
                sourceEscape,
                targetApproach);

            route ??= BuildOuterFallback(sourceEscape, targetApproach, routeBlocks, request);

            var fullPath = new List<CanvasPoint> { request.Source.Port };
            fullPath.AddRange(route);
            fullPath.Add(request.Target.Port);
            return Normalize(fullPath);
        }

        private static List<CanvasPoint>? SelectCompactRoute(
            CanvasPoint start,
            CanvasPoint end,
            IReadOnlyCollection<ScreenBoundingBox> blocks,
            OrthogonalRoutingRequest request)
        {
            return BuildCompactCandidates(start, end)
                .Where(candidate => IsValid(candidate, blocks))
                .OrderBy(candidate => RouteScore(candidate, request, start, end))
                .ThenBy(RouteLength)
                .FirstOrDefault();
        }

        private static IEnumerable<List<CanvasPoint>> BuildCompactCandidates(CanvasPoint start, CanvasPoint end)
        {
            if (Math.Abs(start.Y - end.Y) < LaneMargin || Math.Abs(start.X - end.X) < LaneMargin)
            {
                yield return Normalize(new List<CanvasPoint> { start, end });
            }

            yield return Normalize(new List<CanvasPoint> { start, new CanvasPoint(end.X, start.Y), end });
            yield return Normalize(new List<CanvasPoint> { start, new CanvasPoint(start.X, end.Y), end });
        }

        public static List<CanvasPoint> RouteDraft(CanvasPoint sourcePort, PortDirection sourceDirection, CanvasPoint mousePoint)
        {
            var sourceEscape = Project(sourcePort, sourceDirection, EscapeDistance);
            var isVertical = sourceDirection is PortDirection.Top or PortDirection.Bottom;

            var route = isVertical
                ? new List<CanvasPoint> { sourcePort, sourceEscape, new CanvasPoint(mousePoint.X, sourceEscape.Y), mousePoint }
                : new List<CanvasPoint> { sourcePort, sourceEscape, new CanvasPoint(sourceEscape.X, mousePoint.Y), mousePoint };

            return Normalize(route);
        }

        private static IEnumerable<List<CanvasPoint>> BuildCandidates(
            CanvasPoint start,
            CanvasPoint end,
            IReadOnlyCollection<ScreenBoundingBox> blocks)
        {
            var xLanes = BuildXLanes(start, end, blocks);
            var yLanes = BuildYLanes(start, end, blocks);

            yield return Normalize(new List<CanvasPoint> { start, new CanvasPoint(end.X, start.Y), end });
            yield return Normalize(new List<CanvasPoint> { start, new CanvasPoint(start.X, end.Y), end });

            foreach (var xLane in xLanes)
            {
                yield return Normalize(new List<CanvasPoint>
                {
                    start,
                    new CanvasPoint(xLane, start.Y),
                    new CanvasPoint(xLane, end.Y),
                    end
                });
            }

            foreach (var yLane in yLanes)
            {
                yield return Normalize(new List<CanvasPoint>
                {
                    start,
                    new CanvasPoint(start.X, yLane),
                    new CanvasPoint(end.X, yLane),
                    end
                });
            }

            foreach (var xLane in xLanes)
            {
                foreach (var yLane in yLanes)
                {
                    yield return Normalize(new List<CanvasPoint>
                    {
                        start,
                        new CanvasPoint(xLane, start.Y),
                        new CanvasPoint(xLane, yLane),
                        new CanvasPoint(end.X, yLane),
                        end
                    });

                    yield return Normalize(new List<CanvasPoint>
                    {
                        start,
                        new CanvasPoint(start.X, yLane),
                        new CanvasPoint(xLane, yLane),
                        new CanvasPoint(xLane, end.Y),
                        end
                    });
                }
            }
        }

        private static List<CanvasPoint> BuildOuterFallback(
            CanvasPoint start,
            CanvasPoint end,
            IReadOnlyCollection<ScreenBoundingBox> blocks,
            OrthogonalRoutingRequest request)
        {
            var minX = blocks.Min(block => block.X) - LaneMargin;
            var maxX = blocks.Max(block => block.X + block.Width) + LaneMargin;
            var minY = blocks.Min(block => block.Y) - LaneMargin;
            var maxY = blocks.Max(block => block.Y + block.Height) + LaneMargin;

            var candidates = new[]
            {
                Normalize(new List<CanvasPoint> { start, new CanvasPoint(minX, start.Y), new CanvasPoint(minX, end.Y), end }),
                Normalize(new List<CanvasPoint> { start, new CanvasPoint(maxX, start.Y), new CanvasPoint(maxX, end.Y), end }),
                Normalize(new List<CanvasPoint> { start, new CanvasPoint(start.X, minY), new CanvasPoint(end.X, minY), end }),
                Normalize(new List<CanvasPoint> { start, new CanvasPoint(start.X, maxY), new CanvasPoint(end.X, maxY), end })
            };

            return candidates
                .Where(candidate => IsValid(candidate, blocks))
                .OrderBy(candidate => RouteScore(candidate, request, start, end))
                .ThenBy(RouteLength)
                .FirstOrDefault()
                ?? candidates.OrderBy(candidate => RouteScore(candidate, request, start, end)).ThenBy(RouteLength).First();
        }

        private static IEnumerable<ScreenBoundingBox> BuildBlocks(OrthogonalRoutingRequest request)
        {
            yield return InflateEndpointBlock(request.Source);
            yield return InflateEndpointBlock(request.Target);

            foreach (var obstacle in request.Obstacles)
            {
                yield return Inflate(obstacle, BlockMargin);
            }
        }

        private static IEnumerable<ScreenBoundingBox> BuildRouteBlocks(OrthogonalRoutingRequest request)
        {
            foreach (var block in BuildBlocks(request))
            {
                yield return block;
            }

            if (request.Source.OwnerKind == OrthogonalEndpointOwnerKind.Stream)
            {
                yield return BuildStreamBodyBlock(request.Source);
            }

            if (request.Target.OwnerKind == OrthogonalEndpointOwnerKind.Stream)
            {
                yield return BuildStreamBodyBlock(request.Target);
            }
        }

        private static ScreenBoundingBox BuildStreamBodyBlock(OrthogonalRoutingEndpoint endpoint)
        {
            return Inflate(endpoint.EquipmentBox, StreamBlockMargin);
        }

        private static List<CanvasPoint>? SelectBestRoute(
            IEnumerable<List<CanvasPoint>> candidates,
            IReadOnlyCollection<ScreenBoundingBox> blocks,
            OrthogonalRoutingRequest request,
            CanvasPoint start,
            CanvasPoint end)
        {
            List<CanvasPoint>? bestRoute = null;
            var bestScore = double.PositiveInfinity;
            var bestLength = double.PositiveInfinity;

            foreach (var candidate in candidates)
            {
                if (!IsValid(candidate, blocks))
                {
                    continue;
                }

                var score = RouteScore(candidate, request, start, end);
                var length = RouteLength(candidate);

                if (score < bestScore || (Math.Abs(score - bestScore) < Epsilon && length < bestLength))
                {
                    bestRoute = candidate;
                    bestScore = score;
                    bestLength = length;
                }
            }

            return bestRoute;
        }

        private static IReadOnlyList<double> SelectRelevantLanes(List<double> lanes, double start, double end)
        {
            var unique = new List<double>();

            foreach (var lane in lanes)
            {
                if (!unique.Any(existing => Math.Abs(existing - lane) < Epsilon))
                {
                    unique.Add(lane);
                }
            }

            return unique
                .OrderBy(lane => DistanceToSpan(lane, start, end))
                .ThenBy(lane => Math.Min(Math.Abs(lane - start), Math.Abs(lane - end)))
                .Take(MaxLanesPerAxis)
                .ToList();
        }

        private static double DistanceToSpan(double lane, double start, double end)
        {
            var min = Math.Min(start, end) - LaneMargin;
            var max = Math.Max(start, end) + LaneMargin;

            if (lane < min) return min - lane;
            if (lane > max) return lane - max;
            return 0;
        }

        private static IReadOnlyList<double> BuildXLanes(
            CanvasPoint start,
            CanvasPoint end,
            IReadOnlyCollection<ScreenBoundingBox> blocks)
        {
            var lanes = new List<double> { start.X, end.X };

            foreach (var block in blocks)
            {
                lanes.Add(block.X - LaneMargin);
                lanes.Add(block.X + block.Width + LaneMargin);
            }

            return SelectRelevantLanes(lanes, start.X, end.X);
        }

        private static IReadOnlyList<double> BuildYLanes(
            CanvasPoint start,
            CanvasPoint end,
            IReadOnlyCollection<ScreenBoundingBox> blocks)
        {
            var lanes = new List<double> { start.Y, end.Y };

            foreach (var block in blocks)
            {
                lanes.Add(block.Y - LaneMargin);
                lanes.Add(block.Y + block.Height + LaneMargin);
            }

            return SelectRelevantLanes(lanes, start.Y, end.Y);
        }

        private static bool IsValid(List<CanvasPoint> route, IReadOnlyCollection<ScreenBoundingBox> blocks)
        {
            if (route.Count < 2) return false;

            for (int i = 0; i < route.Count - 1; i++)
            {
                var start = route[i];
                var end = route[i + 1];

                if (!IsOrthogonal(start, end) || IsSamePoint(start, end))
                {
                    return false;
                }

                if (blocks.Any(block => SegmentIntersectsBoxInterior(start, end, block)))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool SegmentIntersectsBoxInterior(CanvasPoint start, CanvasPoint end, ScreenBoundingBox box)
        {
            var minX = Math.Min(start.X, end.X);
            var maxX = Math.Max(start.X, end.X);
            var minY = Math.Min(start.Y, end.Y);
            var maxY = Math.Max(start.Y, end.Y);
            var boxMaxX = box.X + box.Width;
            var boxMaxY = box.Y + box.Height;

            if (maxX <= box.X || minX >= boxMaxX || maxY <= box.Y || minY >= boxMaxY)
            {
                return false;
            }

            if (Math.Abs(start.X - end.X) < Epsilon)
            {
                return start.X > box.X && start.X < boxMaxX;
            }

            if (Math.Abs(start.Y - end.Y) < Epsilon)
            {
                return start.Y > box.Y && start.Y < boxMaxY;
            }

            return true;
        }

        private static CanvasPoint Project(CanvasPoint point, PortDirection direction, double distance)
        {
            var (dx, dy) = DirectionVector(direction);
            return new CanvasPoint(point.X + dx * distance, point.Y + dy * distance);
        }

        private static (double X, double Y) DirectionVector(PortDirection direction) => direction switch
        {
            PortDirection.Top => (0, -1),
            PortDirection.Right => (1, 0),
            PortDirection.Bottom => (0, 1),
            PortDirection.Left => (-1, 0),
            _ => (0, 0)
        };

        private static ScreenBoundingBox Inflate(ScreenBoundingBox box, double margin)
        {
            return new ScreenBoundingBox(
                box.X - margin,
                box.Y - margin,
                box.Width + margin * 2,
                box.Height + margin * 2);
        }

        private static ScreenBoundingBox InflateEndpointBlock(OrthogonalRoutingEndpoint endpoint)
        {
            var margin = endpoint.OwnerKind == OrthogonalEndpointOwnerKind.Stream
                ? StreamBlockMargin
                : BlockMargin;

            return Inflate(endpoint.EquipmentBox, margin);
        }

        private static List<CanvasPoint> Normalize(List<CanvasPoint> route)
        {
            var result = new List<CanvasPoint>();
            foreach (var point in route)
            {
                if (result.Count == 0 || !IsSamePoint(result[^1], point))
                {
                    result.Add(point);
                }
            }

            for (int i = result.Count - 2; i > 0; i--)
            {
                if (IsRedundantCollinearPoint(result[i - 1], result[i], result[i + 1]))
                {
                    result.RemoveAt(i);
                }
            }

            return result;
        }

        private static bool IsOrthogonal(CanvasPoint a, CanvasPoint b) =>
            Math.Abs(a.X - b.X) < Epsilon || Math.Abs(a.Y - b.Y) < Epsilon;

        private static bool IsSamePoint(CanvasPoint a, CanvasPoint b) =>
            Math.Abs(a.X - b.X) < Epsilon && Math.Abs(a.Y - b.Y) < Epsilon;

        private static bool AreCollinear(CanvasPoint a, CanvasPoint b, CanvasPoint c) =>
            (Math.Abs(a.X - b.X) < Epsilon && Math.Abs(b.X - c.X) < Epsilon) ||
            (Math.Abs(a.Y - b.Y) < Epsilon && Math.Abs(b.Y - c.Y) < Epsilon);

        private static bool IsRedundantCollinearPoint(CanvasPoint a, CanvasPoint b, CanvasPoint c)
        {
            if (!AreCollinear(a, b, c))
            {
                return false;
            }

            var betweenX = b.X >= Math.Min(a.X, c.X) - Epsilon &&
                           b.X <= Math.Max(a.X, c.X) + Epsilon;
            var betweenY = b.Y >= Math.Min(a.Y, c.Y) - Epsilon &&
                           b.Y <= Math.Max(a.Y, c.Y) + Epsilon;

            return betweenX && betweenY;
        }

        private static double RouteLength(List<CanvasPoint> route)
        {
            double length = 0;
            for (int i = 0; i < route.Count - 1; i++)
            {
                length += Math.Abs(route[i + 1].X - route[i].X) +
                          Math.Abs(route[i + 1].Y - route[i].Y);
            }

            return length;
        }

        private static double RouteScore(
            List<CanvasPoint> route,
            OrthogonalRoutingRequest request,
            CanvasPoint start,
            CanvasPoint end)
        {
            var score = RouteLength(route);

            score += CountBends(route) * 90;
            score += EndpointDirectionPenalty(route, request.Source, request.Target);
            score += EndpointSemanticPenalty(route, request.Source, request.Target);
            score += EnvelopePenalty(route, start, end) * 0.75;
            score += RouteSpreadPenalty(route, start, end) * 1.25;

            return score;
        }

        private static double EndpointDirectionPenalty(
            List<CanvasPoint> route,
            OrthogonalRoutingEndpoint source,
            OrthogonalRoutingEndpoint target)
        {
            var penalty = 0.0;

            if (route.Count > 1)
            {
                penalty += DirectionPenalty(route[0], route[1], source.Direction, preferSameDirection: true);
            }

            if (route.Count > 1)
            {
                penalty += DirectionPenalty(route[^2], route[^1], target.Direction, preferSameDirection: false);
            }

            return penalty;
        }

        private static double EndpointSemanticPenalty(
            List<CanvasPoint> route,
            OrthogonalRoutingEndpoint source,
            OrthogonalRoutingEndpoint target)
        {
            var penalty = 0.0;

            if (route.Count > 1)
            {
                penalty += FlowEndpointPenalty(route[0], route[1], source, isPathStart: true);
                penalty += FlowEndpointPenalty(route[^1], route[^2], target, isPathStart: false);
            }

            if (source.OwnerKind == OrthogonalEndpointOwnerKind.Stream)
            {
                penalty += CountEndpointBends(route, nearStart: true) * 25;
            }

            if (target.OwnerKind == OrthogonalEndpointOwnerKind.Stream)
            {
                penalty += CountEndpointBends(route, nearStart: false) * 25;
            }

            return penalty;
        }

        private static double FlowEndpointPenalty(
            CanvasPoint portSidePoint,
            CanvasPoint innerRoutePoint,
            OrthogonalRoutingEndpoint endpoint,
            bool isPathStart)
        {
            var (dx, dy) = DirectionVector(endpoint.Direction);
            var segmentX = Math.Sign(innerRoutePoint.X - portSidePoint.X);
            var segmentY = Math.Sign(innerRoutePoint.Y - portSidePoint.Y);
            var dot = segmentX * dx + segmentY * dy;

            var expectsLeavingPort = isPathStart;
            if (endpoint.PortType == PortType.Inlet)
            {
                expectsLeavingPort = !expectsLeavingPort;
            }

            if (Math.Abs(dot) < Epsilon)
            {
                return endpoint.OwnerKind == OrthogonalEndpointOwnerKind.Stream ? 20 : 45;
            }

            var isLeavingPort = dot > 0;
            return isLeavingPort == expectsLeavingPort ? 0 : 120;
        }

        private static int CountEndpointBends(List<CanvasPoint> route, bool nearStart)
        {
            if (route.Count < 4)
            {
                return 0;
            }

            var index = nearStart ? 1 : route.Count - 2;
            return AreCollinear(route[index - 1], route[index], route[index + 1]) ? 0 : 1;
        }

        private static double DirectionPenalty(
            CanvasPoint from,
            CanvasPoint to,
            PortDirection direction,
            bool preferSameDirection)
        {
            var (dx, dy) = DirectionVector(direction);
            var segmentX = Math.Sign(to.X - from.X);
            var segmentY = Math.Sign(to.Y - from.Y);
            var dot = segmentX * dx + segmentY * dy;

            if (Math.Abs(dot) < Epsilon)
            {
                return 35;
            }

            var isPreferred = preferSameDirection ? dot > 0 : dot < 0;
            return isPreferred ? 0 : 220;
        }

        private static double EnvelopePenalty(List<CanvasPoint> route, CanvasPoint start, CanvasPoint end)
        {
            var minX = Math.Min(start.X, end.X) - LaneMargin;
            var maxX = Math.Max(start.X, end.X) + LaneMargin;
            var minY = Math.Min(start.Y, end.Y) - LaneMargin;
            var maxY = Math.Max(start.Y, end.Y) + LaneMargin;
            var penalty = 0.0;

            foreach (var point in route)
            {
                if (point.X < minX) penalty += minX - point.X;
                if (point.X > maxX) penalty += point.X - maxX;
                if (point.Y < minY) penalty += minY - point.Y;
                if (point.Y > maxY) penalty += point.Y - maxY;
            }

            return penalty;
        }

        private static double RouteSpreadPenalty(List<CanvasPoint> route, CanvasPoint start, CanvasPoint end)
        {
            var routeMinX = route.Min(point => point.X);
            var routeMaxX = route.Max(point => point.X);
            var routeMinY = route.Min(point => point.Y);
            var routeMaxY = route.Max(point => point.Y);

            var directWidth = Math.Abs(end.X - start.X);
            var directHeight = Math.Abs(end.Y - start.Y);
            var routeWidth = routeMaxX - routeMinX;
            var routeHeight = routeMaxY - routeMinY;

            var extraWidth = Math.Max(0, routeWidth - directWidth - LaneMargin);
            var extraHeight = Math.Max(0, routeHeight - directHeight - LaneMargin);

            return extraWidth + extraHeight;
        }

        private static int CountBends(List<CanvasPoint> route)
        {
            var bends = 0;
            for (int i = 1; i < route.Count - 1; i++)
            {
                if (!AreCollinear(route[i - 1], route[i], route[i + 1]))
                {
                    bends++;
                }
            }

            return bends;
        }
    }
}
