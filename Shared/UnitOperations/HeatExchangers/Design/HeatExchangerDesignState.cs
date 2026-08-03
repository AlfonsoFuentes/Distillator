namespace Shared.UnitOperations.HeatExchangers.Design;

public sealed class HeatExchangerDesignState
{
    public bool StopRequested { get; set; }

    public string Message { get; set; } = string.Empty;

    public double PreviousCalculatedFoulingResistance { get; set; }

    public bool ContinueTubePassIteration { get; set; }

    public bool ContinueDesignIteration { get; set; }

    public bool DirtyOverallCoefficientIsDecreasing { get; set; } = true;

    public int DesignIterationCount { get; set; }

    public double MinimumTubeCountRequiredByPressureDrop { get; set; }

    public List<string> RequiredMethodImplementations { get; } = [];
}
