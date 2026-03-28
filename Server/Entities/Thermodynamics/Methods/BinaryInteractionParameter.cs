using Server.Entities.BaseStructure.Components;
using Shared.Thermodynamics.Enums;

namespace Server.Entities.Thermodynamics.Methods
{
    public class BinaryInteractionParameter : Entity
    {
        public Guid MethodId { get; set; }
        public ThermodynamicMethod Method { get; set; } = null!;

        public Guid ComponentI_Id { get; set; }
        public ChemicalComponent ComponentI { get; set; } = null!;

        public Guid ComponentJ_Id { get; set; }
        public ChemicalComponent ComponentJ { get; set; } = null!;

        public BinaryParameterType ParameterType { get; set; }
        public double Value { get; set; }

        public override bool IsTenanted => false;
    }
}
