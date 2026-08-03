using Shared.PipingRoutes;
using Shared.ProcessFlowDiagram;
using Shared.ProcessFlowDiagram.Columns;
using Shared.ProcessFlowDiagram.ControlValves;
using Shared.ProcessFlowDiagram.Pipes;
using Shared.ProcessFlowDiagram.Pumps;
using Shared.ProcessFlowDiagram.Streams;
using Shared.ProcessFlowDiagram.Vessels;

namespace Distillator.Core.Tests.Topology;

public sealed class PipeRoutingFactoryTests
{
    [Fact]
    [Trait("Spec", "04")]
    [Trait("Level", "Unit")]
    public void GetRoute_WhenPipeHasNoSource_ShouldReturnEmptyPath()
    {
        var pipe = new PipeVisualElement();

        var route = PipeRoutingFactory.GetRoute(pipe, false, 0, 0, new List<IVisualElement>(), new List<PipeVisualElement>());

        Assert.Equal(string.Empty, route.MainPath);
        Assert.Empty(pipe.CalculatedRoute);
    }

    [Fact]
    [Trait("Spec", "04")]
    [Trait("Level", "Unit")]
    public void GetRoute_WhenPipeIsDraft_ShouldRouteFromSourcePortToMouse()
    {
        var pump = new PumpVisualElement { X = 100, Y = 100 };
        var pipe = new PipeVisualElement
        {
            SourceElement = pump,
            SourceElementId = pump.Id,
            SourcePortName = PumpVisualElement.PortDischargeName
        };

        var route = PipeRoutingFactory.GetRoute(pipe, true, 240, 40, new List<IVisualElement> { pump }, new List<PipeVisualElement> { pipe });

        Assert.StartsWith("M 130 110 L 130 80", route.MainPath);
        Assert.EndsWith("L 240 40", route.MainPath);
        Assert.Empty(pipe.CalculatedRoute);
    }

    [Fact]
    [Trait("Spec", "04")]
    [Trait("Level", "Unit")]
    public void GetRoute_WhenPipeIsFinal_ShouldStoreCalculatedRouteFromSourceToTarget()
    {
        var pump = new PumpVisualElement { X = 100, Y = 100 };
        var stream = new StreamVisualElement { X = 260, Y = 95 };
        var pipe = new PipeVisualElement
        {
            SourceElement = pump,
            SourceElementId = pump.Id,
            SourcePortName = PumpVisualElement.PortDischargeName,
            TargetElement = stream,
            TargetElementId = stream.Id,
            TargetPortName = "Inlet"
        };

        var route = PipeRoutingFactory.GetRoute(
            pipe,
            false,
            0,
            0,
            new List<IVisualElement> { pump, stream },
            new List<PipeVisualElement> { pipe });

        Assert.NotEqual(string.Empty, route.MainPath);
        Assert.True(pipe.CalculatedRoute.Count >= 2);
        Assert.Equal(new CanvasPoint(130, 110), pipe.CalculatedRoute.First());
        Assert.Equal(new CanvasPoint(256, 110), pipe.CalculatedRoute.Last());
    }

    [Theory]
    [MemberData(nameof(SourcePortCases))]
    [Trait("Spec", "04")]
    [Trait("Level", "Unit")]
    public void GetRoute_WhenFinalRouteUsesAnySourcePortDirection_ShouldNotBacktrackAcrossSourceElement(
        IVisualElement source,
        string sourcePortName)
    {
        source.X = 120;
        source.Y = 120;
        var target = new StreamVisualElement { X = 340, Y = 260 };
        var pipe = new PipeVisualElement
        {
            SourceElement = source,
            SourceElementId = source.Id,
            SourcePortName = sourcePortName,
            TargetElement = target,
            TargetElementId = target.Id,
            TargetPortName = "Inlet"
        };

        PipeRoutingFactory.GetRoute(
            pipe,
            false,
            0,
            0,
            new List<IVisualElement> { source, target },
            new List<PipeVisualElement> { pipe });

        Assert.True(pipe.CalculatedRoute.Count >= 2);
        for (int i = 1; i < pipe.CalculatedRoute.Count - 1; i++)
        {
            Assert.False(
                IntersectsElementBounds(pipe.CalculatedRoute[i], pipe.CalculatedRoute[i + 1], source),
                $"Segment {i} should not cross the source element bounds.");
        }
    }

    [Fact]
    [Trait("Spec", "04")]
    [Trait("Level", "Unit")]
    public void GetRoute_WhenSourceIsRightOfTarget_ShouldKeepCalculatedRouteFromRealSourceToRealTarget()
    {
        var valve = new ControlValveVisualElement { X = 320, Y = 120 };
        var stream = new StreamVisualElement { X = 120, Y = 120 };
        var pipe = new PipeVisualElement
        {
            SourceElement = valve,
            SourceElementId = valve.Id,
            SourcePortName = ControlValveVisualElement.PortOutletName,
            TargetElement = stream,
            TargetElementId = stream.Id,
            TargetPortName = "Inlet"
        };

        PipeRoutingFactory.GetRoute(
            pipe,
            false,
            0,
            0,
            new List<IVisualElement> { valve, stream },
            new List<PipeVisualElement> { pipe });

        Assert.Equal(new CanvasPoint(380, 170), pipe.CalculatedRoute.First());
        Assert.Equal(new CanvasPoint(116, 135), pipe.CalculatedRoute.Last());
    }

    [Fact]
    [Trait("Spec", "04")]
    [Trait("Level", "Unit")]
    public void GetRoute_WhenSourceIsBelowTarget_ShouldKeepCalculatedRouteFromBottomPortToTarget()
    {
        var column = new ColumnVisualElement { X = 260, Y = 80 };
        var stream = new StreamVisualElement { X = 120, Y = 330 };
        var pipe = new PipeVisualElement
        {
            SourceElement = column,
            SourceElementId = column.Id,
            SourcePortName = ColumnVisualElement.PortBottomsName,
            TargetElement = stream,
            TargetElementId = stream.Id,
            TargetPortName = "Inlet"
        };

        PipeRoutingFactory.GetRoute(
            pipe,
            false,
            0,
            0,
            new List<IVisualElement> { column, stream },
            new List<PipeVisualElement> { pipe });

        Assert.Equal(new CanvasPoint(300, 370), pipe.CalculatedRoute.First());
        Assert.Equal(new CanvasPoint(116, 345), pipe.CalculatedRoute.Last());
    }

    public static IEnumerable<object[]> SourcePortCases()
    {
        yield return new object[] { new PumpVisualElement(), PumpVisualElement.PortDischargeName };
        yield return new object[] { new ControlValveVisualElement(), ControlValveVisualElement.PortOutletName };
        yield return new object[] { new FlashTankVisualElement(), FlashTankVisualElement.PortLiquidName };
        yield return new object[] { new StreamVisualElement(), "Inlet" };
    }

    private static bool IntersectsElementBounds(CanvasPoint p1, CanvasPoint p2, IVisualElement element)
    {
        var rectX = element.X;
        var rectY = element.Y;
        var rectMaxX = element.X + element.Width;
        var rectMaxY = element.Y + element.Height;

        var minX = Math.Min(p1.X, p2.X);
        var maxX = Math.Max(p1.X, p2.X);
        var minY = Math.Min(p1.Y, p2.Y);
        var maxY = Math.Max(p1.Y, p2.Y);

        if (maxX < rectX || minX > rectMaxX || maxY < rectY || minY > rectMaxY)
        {
            return false;
        }

        if (Math.Abs(p1.X - p2.X) < 0.1)
        {
            return p1.X > rectX && p1.X < rectMaxX;
        }

        if (Math.Abs(p1.Y - p2.Y) < 0.1)
        {
            return p1.Y > rectY && p1.Y < rectMaxY;
        }

        return false;
    }
}
