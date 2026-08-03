using Shared.ProcessFlowDiagram.HeatExchangers;
using Shared.UnitOperations.HeatExchangers.Design;

namespace Distillator.Core.Tests.HeatExchangers;

public sealed class HeatExchangerDesignMethodRegistryTests
{
    private readonly HeatExchangerDesignMethodRegistry registry = new();

    [Fact]
    public void HeatExchangerVisualElement_UsesShellAndTubeDesignMethod()
    {
        var equipment = new HeatExchangerVisualElement();

        Assert.Equal(HeatExchangerDesignMethod.ShellAndTube, equipment.DesignMethod);
    }

    [Fact]
    public void PlateExchangerVisualElement_UsesPlatePlaceholderDesignMethod()
    {
        var equipment = new PlateExchangerVisualElement();
        var descriptor = registry.GetDescriptor(equipment.DesignMethod);

        Assert.False(descriptor.SupportsAutomatedDesign);
        Assert.False(descriptor.SupportsManualRecalculation);
        Assert.NotNull(descriptor.PlaceholderMessage);
    }

    [Fact]
    public void ShellAndTubeDescriptor_SupportsCreateDesignAndManualRecalculation()
    {
        var descriptor = registry.GetDescriptor(HeatExchangerDesignMethod.ShellAndTube);

        Assert.True(descriptor.SupportsAutomatedDesign);
        Assert.True(descriptor.SupportsManualRecalculation);
    }
}
