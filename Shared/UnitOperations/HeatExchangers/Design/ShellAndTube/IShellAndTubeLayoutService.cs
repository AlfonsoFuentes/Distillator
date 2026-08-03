namespace Shared.UnitOperations.HeatExchangers.Design;

public interface IShellAndTubeLayoutService
{
    ShellAndTubeLayoutSelection SelectShellForTubeCount(ShellAndTubeLayoutRequest request);
    ShellAndTubeLayoutCapacity EstimateTubeCapacity(ShellAndTubeLayoutCapacityRequest request);
}

public sealed record ShellAndTubeLayoutRequest
{
    public required double TubeCount { get; init; }
    public required double TubeOuterDiameterInches { get; init; }
    public required double TubePitchInches { get; init; }
    public required int TubePasses { get; init; }
    public required ShellAndTubeTubeLayout TubeLayout { get; init; }
}

public sealed record ShellAndTubeLayoutCapacityRequest
{
    public required double ShellInsideDiameterInches { get; init; }
    public required double TubeOuterDiameterInches { get; init; }
    public required double TubePitchInches { get; init; }
    public required int TubePasses { get; init; }
    public required ShellAndTubeTubeLayout TubeLayout { get; init; }
}

public sealed record ShellAndTubeLayoutSelection
{
    public required double ShellInsideDiameterInches { get; init; }
    public required int MaximumTubeCount { get; init; }
    public required ShellAndTubeLayoutSource Source { get; init; }
}

public sealed record ShellAndTubeLayoutCapacity
{
    public required int MaximumTubeCount { get; init; }
    public required ShellAndTubeLayoutSource Source { get; init; }
}

public enum ShellAndTubeTubeLayout
{
    Square,
    Triangular
}

public enum ShellAndTubeLayoutSource
{
    KernTable,
    Correlation
}
