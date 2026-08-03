using Shared.ProcessFlowDiagram;
using Shared.ProcessFlowDiagram.ControlValves;
using Shared.ProcessFlowDiagram.Pumps;
using Shared.ProcessFlowDiagram.Streams;
using Shared.ProcessFlowDiagram.Vessels;

namespace Distillator.Core.Tests.Topology;

public sealed class StaticPortContractTests
{
    [Fact]
    [Trait("Spec", "04")]
    [Trait("Spec", "11")]
    [Trait("Level", "Unit")]
    public void Stream_ShouldExposeInletOnLeftAndOutletOnRight()
    {
        var stream = new StreamVisualElement();

        AssertPort(stream.Ports.Single(port => port.Name == "Inlet"), "Inlet", PortType.Inlet, PortDirection.Left, -4, 15);
        AssertPort(stream.Ports.Single(port => port.Name == "Outlet"), "Outlet", PortType.Outlet, PortDirection.Right, 64, 15);
    }

    [Fact]
    [Trait("Spec", "04")]
    [Trait("Spec", "11")]
    [Trait("Level", "Unit")]
    public void Pump_ShouldExposeSuctionAndDischargeWithPhysicalDirections()
    {
        var pump = new PumpVisualElement();

        AssertPort(pump.SuctionPort, PumpVisualElement.PortSuctionName, PortType.Inlet, PortDirection.Left, 0, 40);
        AssertPort(pump.DischargePort, PumpVisualElement.PortDischargeName, PortType.Outlet, PortDirection.Top, 30, 10);
    }

    [Fact]
    [Trait("Spec", "04")]
    [Trait("Spec", "11")]
    [Trait("Level", "Unit")]
    public void ControlValve_ShouldExposeInletOnLeftAndOutletOnRight()
    {
        var valve = new ControlValveVisualElement();

        AssertPort(valve.InletPort, ControlValveVisualElement.PortInletName, PortType.Inlet, PortDirection.Left, 0, 50);
        AssertPort(valve.OutletPort, ControlValveVisualElement.PortOutletName, PortType.Outlet, PortDirection.Right, 60, 50);
    }

    [Fact]
    [Trait("Spec", "04")]
    [Trait("Spec", "11")]
    [Trait("Level", "Unit")]
    public void FlashTank_ShouldKeepFixedThreePortContract()
    {
        var flashTank = new FlashTankVisualElement();

        AssertPort(flashTank.FeedPort, FlashTankVisualElement.PortFeedName, PortType.Inlet, PortDirection.Left, -8, flashTank.Height / 2);
        AssertPort(flashTank.VaporPort, FlashTankVisualElement.PortVaporName, PortType.Outlet, PortDirection.Top, flashTank.Width / 2, -8);
        AssertPort(flashTank.LiquidPort, FlashTankVisualElement.PortLiquidName, PortType.Outlet, PortDirection.Bottom, flashTank.Width / 2, flashTank.Height + 8);
    }

    [Theory]
    [InlineData(true, PortType.Inlet, PortDirection.Left, 0)]
    [InlineData(false, PortType.Outlet, PortDirection.Right, 80)]
    [Trait("Spec", "05")]
    [Trait("Level", "Unit")]
    public void OffPageConnector_ShouldKeepLegacyDefaultPortSideForFlowRole(
        bool isOutlet,
        PortType expectedPortType,
        PortDirection expectedDirection,
        double expectedOffsetX)
    {
        var connector = new OffPageConnectorElement(isOutlet);

        var transfer = Assert.Single(connector.Ports);
        AssertPort(transfer, "Transfer", expectedPortType, expectedDirection, expectedOffsetX, 20);
    }

    [Theory]
    [InlineData(true, OffPageConnectorPortSide.Left, PortType.Inlet, PortDirection.Left, 0)]
    [InlineData(true, OffPageConnectorPortSide.Right, PortType.Inlet, PortDirection.Right, 80)]
    [InlineData(false, OffPageConnectorPortSide.Left, PortType.Outlet, PortDirection.Left, 0)]
    [InlineData(false, OffPageConnectorPortSide.Right, PortType.Outlet, PortDirection.Right, 80)]
    [Trait("Spec", "05")]
    [Trait("Level", "Unit")]
    public void OffPageConnector_ShouldAllowVisualPortSideIndependentFromFlowRole(
        bool isOutlet,
        OffPageConnectorPortSide portSide,
        PortType expectedPortType,
        PortDirection expectedDirection,
        double expectedOffsetX)
    {
        var connector = new OffPageConnectorElement(isOutlet, portSide);

        var transfer = Assert.Single(connector.Ports);
        AssertPort(transfer, "Transfer", expectedPortType, expectedDirection, expectedOffsetX, 20);
    }

    private static void AssertPort(
        EquipmentPort port,
        string name,
        PortType type,
        PortDirection direction,
        double offsetX,
        double offsetY)
    {
        Assert.Equal(name, port.Name);
        Assert.Equal(type, port.Type);
        Assert.Equal(direction, port.Direction);
        Assert.Equal(offsetX, port.OffsetX, precision: 3);
        Assert.Equal(offsetY, port.OffsetY, precision: 3);
    }
}
