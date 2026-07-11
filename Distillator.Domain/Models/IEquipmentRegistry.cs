using Shared.ProcessFlowDiagram;

namespace Distillator.Domain.Models
{
    /// <summary>
    /// Catálogo global de equipos de un proyecto.
    /// Cada equipo existe UNA sola vez aquí.
    /// Los flowsheets solo guardan referencias posicionales (IFlowsheetElementReference).
    /// </summary>
    public interface IEquipmentRegistry
    {
        IReadOnlyCollection<IVisualElement> AllEquipments { get; }
        IVisualElement? GetById(Guid id);
        IVisualElement? GetByName(string name);

        void Register(IVisualElement equipment);
        void Unregister(Guid id);

        IEnumerable<IVisualElement> GetByType(EquipmentType type);
        IEnumerable<IVisualElement> GetByPrefix(string prefix); // "P-", "E-", "T-"
    }
}
