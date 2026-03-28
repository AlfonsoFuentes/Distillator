using Server.Entities.BaseStructure.Components;

namespace Server.Entities.Thermodynamics.Methods
{
    public class MethodComponent : Entity
    {
        public Guid MethodId { get; set; }
        public ThermodynamicMethod Method { get; set; } = null!;

        public Guid ComponentId { get; set; }
        public ChemicalComponent Component { get; set; } = null!;

        public int MatrixIndex { get; set; }

        public override bool IsTenanted => false;
    }
}
