using Distillator.Domain.Configuration;

namespace Distillator.Domain.Models;

public abstract class Flowsheet : IFlowsheet
{
    private readonly List<IFlowsheetElementReference> _elements = new();
    private readonly List<IPipeReference> _pipes = new();

    public Guid Id { get; }
    public string Name { get; set; }
    public abstract string TypeCode { get; }
    public IFlowsheetType TypeDefinition { get; }

    public double Zoom { get; set; }
    public double PanX { get; set; }
    public double PanY { get; set; }
    public double DiagramWidth { get; set; }
    public double DiagramHeight { get; set; }
    public double GridSize { get; set; }
    public double GlobalScale { get; set; }

    public IReadOnlyCollection<IFlowsheetElementReference> Elements => _elements.AsReadOnly();
    public IReadOnlyCollection<IPipeReference> Pipes => _pipes.AsReadOnly();

    public IProject Project { get; }

    protected Flowsheet(string name, IFlowsheetType typeDefinition, IProject project)
    {
        Id = Guid.NewGuid();
        Name = name;
        TypeDefinition = typeDefinition ?? throw new ArgumentNullException(nameof(typeDefinition));
        Project = project ?? throw new ArgumentNullException(nameof(project));

        var cameraDefaults = project.Configuration.CameraDefaults;
        Zoom = cameraDefaults.DefaultZoom;
        PanX = cameraDefaults.DefaultPanX;
        PanY = cameraDefaults.DefaultPanY;
        GlobalScale = cameraDefaults.GlobalScale;
        GridSize = cameraDefaults.GridSize;
        DiagramWidth = 5000;
        DiagramHeight = 5000;
    }

    public void AddElementReference(IFlowsheetElementReference reference)
    {
        if (reference == null) throw new ArgumentNullException(nameof(reference));
        if (_elements.Any(e => e.ElementId == reference.ElementId)) return;
        _elements.Add(reference);
    }

    public void RemoveElementReference(Guid elementId)
    {
        var el = _elements.FirstOrDefault(e => e.ElementId == elementId);
        if (el != null) _elements.Remove(el);
    }

    public IFlowsheetElementReference? GetElementReference(Guid elementId)
        => _elements.FirstOrDefault(e => e.ElementId == elementId);

    public void AddPipe(IPipeReference pipe)
    {
        if (pipe == null) throw new ArgumentNullException(nameof(pipe));
        _pipes.Add(pipe);
    }

    public void RemovePipe(Guid pipeId)
    {
        var pipe = _pipes.FirstOrDefault(p => p.Id == pipeId);
        if (pipe != null) _pipes.Remove(pipe);
    }

    public IPipeReference? GetPipe(Guid pipeId)
        => _pipes.FirstOrDefault(p => p.Id == pipeId);

    public void ResetCameraToDefaults()
    {
        var cameraDefaults = Project.Configuration.CameraDefaults;
        Zoom = cameraDefaults.DefaultZoom;
        PanX = cameraDefaults.DefaultPanX;
        PanY = cameraDefaults.DefaultPanY;
        GlobalScale = cameraDefaults.GlobalScale;
        GridSize = cameraDefaults.GridSize;
    }
}
