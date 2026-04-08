namespace Shared.UnitOperations.Streams
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
