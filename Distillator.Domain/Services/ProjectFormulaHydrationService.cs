using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;

namespace Distillator.Domain.Services;

public sealed record FormulaSpecificationHydrationSnapshot(
    Guid Id,
    string Formula,
    string? DefinedByUserId = null,
    string? DefinedByUserName = null,
    DateTime? DefinedAtUtc = null);

public sealed class ProjectFormulaHydrationService
{
    public int Restore(
        SolverEquipmentBase equipment,
        IEnumerable<FormulaSpecificationHydrationSnapshot> snapshots,
        IEnumerable<IFacadeStream> streams)
    {
        ArgumentNullException.ThrowIfNull(equipment);
        ArgumentNullException.ThrowIfNull(snapshots);
        ArgumentNullException.ThrowIfNull(streams);

        var restored = 0;
        foreach (var snapshot in snapshots)
        {
            var result = FormulaParser.Parse(snapshot.Formula, streams);
            if (!result.Succeeded)
            {
                continue;
            }

            equipment.AddSpec(new FormulaSpecification(snapshot.Formula, result.Data)
            {
                Id = snapshot.Id == Guid.Empty ? Guid.NewGuid() : snapshot.Id,
                DefinedByUserId = snapshot.DefinedByUserId,
                DefinedByUserName = snapshot.DefinedByUserName,
                DefinedAtUtc = snapshot.DefinedAtUtc
            });
            restored++;
        }

        return restored;
    }
}
