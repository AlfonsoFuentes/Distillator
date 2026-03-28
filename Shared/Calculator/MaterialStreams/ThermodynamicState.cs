using Shared.Calculator.Components;
using Shared.Calculator.ProcessVariables;
using Shared.Calculator.Solvers;
using Shared.Thermodynamics.Methods;
using System;
using System.Collections.Generic;
using System.Text;
using UnitSystem;

namespace Shared.Calculator.MaterialStreams
{
    public enum ThermodynamicState
    {
        Undefined = 0,
        SubcooledLiquid = 1,      // Líquido subenfriado
        SaturatedLiquid = 2,      // Líquido saturado (punto de burbuja)
        VaporLiquidMixture = 3,   // Mezcla líquido-vapor
        SaturatedVapor = 4,       // Vapor saturado (punto de rocío)
        SuperheatedVapor = 5      // Vapor sobrecalentado
    }
}
