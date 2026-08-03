using Shared.PropertiesDtos.Enums;
using Shared.PropertiesDtos.Methods;
using Shared.Thermodynamics.Phases;

namespace Distillator.Thermodynamics.Tests;

public sealed class BinaryInteractionManagerTests
{
    [Fact]
    [Trait("Spec", "Thermodynamics")]
    [Trait("Level", "Regression")]
    public void GetKij_ForSrk_ShouldIgnoreWilsonActivityParameters()
    {
        var methanolId = Guid.NewGuid();
        var waterId = Guid.NewGuid();
        var parameters = new List<BinaryInteractionParameterDto>
        {
            new()
            {
                ComponentI_Id = methanolId,
                ComponentJ_Id = waterId,
                ParameterType = BinaryParameterType.Wilson_A,
                Value = 1.0837
            },
            new()
            {
                ComponentI_Id = methanolId,
                ComponentJ_Id = waterId,
                ParameterType = BinaryParameterType.Wilson_B,
                Value = -580.237
            },
            new()
            {
                ComponentI_Id = waterId,
                ComponentJ_Id = methanolId,
                ParameterType = BinaryParameterType.Wilson_A,
                Value = -1.8842
            },
            new()
            {
                ComponentI_Id = waterId,
                ComponentJ_Id = methanolId,
                ParameterType = BinaryParameterType.Wilson_B,
                Value = 617.4097
            }
        };

        var kij = BinaryInteractionManager.GetKij(
            methanolId,
            waterId,
            "Methanol",
            "Water",
            VaporPhaseModel.SoaveRedlichKwong1972,
            parameters);

        Assert.Equal(0.0, kij);
    }

    [Fact]
    [Trait("Spec", "Thermodynamics")]
    [Trait("Level", "Regression")]
    public void GetKij_ForSrk_ShouldUseExplicitSrkKijParameter()
    {
        var componentI = Guid.NewGuid();
        var componentJ = Guid.NewGuid();
        var parameters = new List<BinaryInteractionParameterDto>
        {
            new()
            {
                ComponentI_Id = componentI,
                ComponentJ_Id = componentJ,
                ParameterType = BinaryParameterType.Wilson_B,
                Value = -580.237
            },
            new()
            {
                ComponentI_Id = componentI,
                ComponentJ_Id = componentJ,
                ParameterType = BinaryParameterType.SRK_Kij,
                Value = 0.0123
            }
        };

        var kij = BinaryInteractionManager.GetKij(
            componentJ,
            componentI,
            "Component J",
            "Component I",
            VaporPhaseModel.SoaveRedlichKwong1972,
            parameters);

        Assert.Equal(0.0123, kij, precision: 12);
    }
}
