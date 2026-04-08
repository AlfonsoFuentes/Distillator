using Shared.PropertiesDtos.Enums;

namespace Server.Entities.Thermodynamics.Methods
{
    public class ThermodynamicMethod : Entity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public VaporPhaseModel VaporModel { get; set; }
        public LiquidPhaseModel LiquidModel { get; set; }

        public ICollection<MethodComponent> MethodComponents { get; set; } = new List<MethodComponent>();
        public ICollection<BinaryInteractionParameter> BinaryParameters { get; set; } = new List<BinaryInteractionParameter>();

        public override bool IsTenanted => false;
    }
}
