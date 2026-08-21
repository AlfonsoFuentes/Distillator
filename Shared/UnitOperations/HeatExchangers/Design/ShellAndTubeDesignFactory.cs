namespace Shared.UnitOperations.HeatExchangers.Design;

public sealed class ShellAndTubeDesignFactory : IShellAndTubeDesignFactory
{
    private readonly IReadOnlyDictionary<ShellAndTubeCalculationStandard, IShellAndTubeDesignFactory> factories;

    public ShellAndTubeDesignFactory()
        : this(new Dictionary<ShellAndTubeCalculationStandard, IShellAndTubeDesignFactory>
        {
            [ShellAndTubeCalculationStandard.Kern] = new KernShellAndTubeDesignFactory(),
            [ShellAndTubeCalculationStandard.DesignPractices] = new DesignPracticesShellAndTubeDesignFactory()
        })
    {
    }

    public ShellAndTubeDesignFactory(
        IReadOnlyDictionary<ShellAndTubeCalculationStandard, IShellAndTubeDesignFactory> factories)
    {
        this.factories = factories;
    }

    public IHeatExchangerDesign Create(HeatExchangerDesignRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!factories.TryGetValue(request.Variables.CalculationStandard, out var factory))
        {
            throw new NotSupportedException($"Shell-and-tube calculation standard '{request.Variables.CalculationStandard}' is not supported.");
        }

        return factory.Create(request);
    }
}
