using Shared.Thermodynamics.Components;
using Shared.Thermodynamics.Enums;

namespace Shared.Thermodynamics.Methods
{
    // --- DTOs Base ---

    public class BinaryInteractionParameterDto
    {
        public Guid ComponentI_Id { get; set; }
        public string ComponentI_Name { get; set; } = string.Empty;

        public Guid ComponentJ_Id { get; set; }
        public string ComponentJ_Name { get; set; } = string.Empty;

        public BinaryParameterType ParameterType { get; set; }
        public double Value { get; set; }
    }

    public class MethodComponentDto
    {
        public Guid ComponentId { get; set; }
        public string ComponentName { get; set; } = string.Empty;
        public int MatrixIndex { get; set; }
    }

    public class ThermodynamicMethodListDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public VaporPhaseModel VaporModel { get; set; }
        public LiquidPhaseModel LiquidModel { get; set; }
        public int ComponentCount { get; set; }
    }

    public class ThermodynamicMethodDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public VaporPhaseModel VaporModel { get; set; }
        public LiquidPhaseModel LiquidModel { get; set; }

        public List<MethodComponentDto> Components { get; set; } = new();
        public List<BinaryInteractionParameterDto> BinaryParameters { get; set; } = new();
    }

    // --- Herencia para Operaciones ---

    public class CreateThermodynamicMethod : ThermodynamicMethodDto
    {
    }

    public class EditThermodynamicMethod : ThermodynamicMethodDto
    {
    }

    // --- Records de Solicitud (Requests) ---
    public record GetAllCompleteMethods();
    public record GetAllFullCompleteMethods();
    public record GetAllMethods();
    public record GetMethodById(Guid Id);
    public record DeleteMethod(Guid Id);
    public record ValidateMethodName(Guid Id, string Name);
    public record GetMethodFullRequest(Guid Id);
    public class ThermodynamicMethodFullDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public VaporPhaseModel VaporModel { get; set; }
        public LiquidPhaseModel LiquidModel { get; set; }

        // Componentes con su ADN completo
        public List<MethodComponentFullDto> Components { get; set; } = new();

        // Parámetros de interacción binaria para los modelos (NRTL, Peng-Robinson, etc.)
        public List<BinaryInteractionParameterDto> BinaryParameters { get; set; } = new();
    }

    public class MethodComponentFullDto
    {
        public Guid ComponentId { get; set; }
        public string ComponentName { get; set; } = string.Empty;
        public int MatrixIndex { get; set; }

        // Aquí proyectamos la entidad ChemicalComponent completa que me pasaste
        public ChemicalComponentDto FullData { get; set; } = null!;
    }
}