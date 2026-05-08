using Shared.UnitOperations.Streams;

namespace Shared.UnitOperations.Basiss
{
    public interface IEquipmentFacade : ISolverEquationsProvider, IFacade
    {
        void AttachConnection(string portName, IStreamFacade connectedFacade);
    }

}
