using Shared.PropertiesDtos.Enums;
using Shared.PropertiesDtos.Methods;
using Shared.Thermodynamics.Componentes;

namespace Shared.Thermodynamics.Phases
{
    public static class ActivityParameterFactory
    {
        public static double[][,] BuildMatrices(
            LiquidPhaseModel model,
            IReadOnlyList<LiquidComponentNode> components,
            List<BinaryInteractionParameterDto> dbParams)
        {
            int n = components.Count;

            switch (model)
            {
                case LiquidPhaseModel.EA_Van_Laar:
                    var vanLaar = new double[1][,];
                    vanLaar[0] = new double[n, n];
                    for (int i = 0; i < n; i++)
                        for (int j = 0; j < n; j++)
                            vanLaar[0][i, j] = BinaryInteractionManager.GetActivityParameter(
                                components[i].Id, components[j].Id, BinaryParameterType.VanLaar_Aij, dbParams);
                    return vanLaar;

                case LiquidPhaseModel.Wilson:
                case LiquidPhaseModel.WilsonASPEN:
                    var wilson = new double[2][,];
                    wilson[0] = new double[n, n]; // Wilson Aij
                    wilson[1] = new double[n, n]; // Wilson Bij
                    for (int i = 0; i < n; i++)
                        for (int j = 0; j < n; j++)
                        {
                            wilson[0][i, j] = BinaryInteractionManager.GetActivityParameter(
                                components[i].Id, components[j].Id, BinaryParameterType.Wilson_A, dbParams);
                            wilson[1][i, j] = BinaryInteractionManager.GetActivityParameter(
                                components[i].Id, components[j].Id, BinaryParameterType.Wilson_B, dbParams);
                        }
                    return wilson;

                case LiquidPhaseModel.NRTL_ASPEN:
                    var nrtl = new double[3][,];
                    nrtl[0] = new double[n, n]; // Aij
                    nrtl[1] = new double[n, n]; // Bij
                    nrtl[2] = new double[n, n]; // Alpha (Cij)
                    for (int i = 0; i < n; i++)
                        for (int j = 0; j < n; j++)
                        {
                            nrtl[0][i, j] = BinaryInteractionManager.GetActivityParameter(
                                components[i].Id, components[j].Id, BinaryParameterType.NRTL_A, dbParams);
                            nrtl[1][i, j] = BinaryInteractionManager.GetActivityParameter(
                                components[i].Id, components[j].Id, BinaryParameterType.NRTL_B, dbParams);
                            nrtl[2][i, j] = BinaryInteractionManager.GetActivityParameter(
                                components[i].Id, components[j].Id, BinaryParameterType.NRTL_C, dbParams);
                        }
                    return nrtl;

                default:
                    return Array.Empty<double[,]>();
            }
        }
    }
    //public static class ActivityParameterFactory
    //{
    //    /// <summary>
    //    /// Fabrica las matrices de interacción binaria necesarias según el modelo de actividad.
    //    /// Retorna un arreglo de matrices N x N.
    //    /// Índice 0: Parámetro A (o Aij)
    //    /// Índice 1: Parámetro B (o Bij) - Si aplica
    //    /// Índice 2: Parámetro Alpha (o Cij) - Si aplica
    //    /// </summary>
    //    public static double[][,] BuildMatrices(
    //        LiquidPhaseModel model,
    //        IReadOnlyList<StreamComponent> components,
    //        List<BinaryInteractionParameterDto> dbParams)
    //    {
    //        int n = components.Count;

    //        switch (model)
    //        {
    //            // =========================================================================
    //            // VAN LAAR: 1 Matriz (Aij)
    //            // =========================================================================
    //            case LiquidPhaseModel.EA_Van_Laar:
    //                double[][,] vanLaar = new double[1][,];
    //                vanLaar[0] = new double[n, n];

    //                for (int i = 0; i < n; i++)
    //                {
    //                    for (int j = 0; j < n; j++)
    //                    {
    //                        if (i == j)
    //                        {
    //                            vanLaar[0][i, j] = 0.0;
    //                        }
    //                        else
    //                        {
    //                            vanLaar[0][i, j] = BinaryInteractionManager.GetVanLaarParameter(
    //                                components[i].BaseProperties.Id,
    //                                components[j].BaseProperties.Id,
    //                                dbParams);
    //                        }
    //                    }
    //                }
    //                return vanLaar;

    //            // =========================================================================
    //            // WILSON / WILSON ASPEN: 2 Matrices (Aij, Bij)
    //            // =========================================================================
    //            case LiquidPhaseModel.Wilson:
    //            case LiquidPhaseModel.WilsonASPEN:
    //                double[][,] wilson = new double[2][,];
    //                wilson[0] = new double[n, n]; // Aij
    //                wilson[1] = new double[n, n]; // Bij

    //                for (int i = 0; i < n; i++)
    //                {
    //                    for (int j = 0; j < n; j++)
    //                    {
    //                        if (i == j)
    //                        {
    //                            wilson[0][i, j] = 0.0;
    //                            wilson[1][i, j] = 0.0;
    //                        }
    //                        else
    //                        {
    //                            wilson[0][i, j] = BinaryInteractionManager.GetWilsonParameter_A(
    //                                components[i].BaseProperties.Id,
    //                                components[j].BaseProperties.Id,
    //                                dbParams);

    //                            wilson[1][i, j] = BinaryInteractionManager.GetWilsonParameter_B(
    //                                components[i].BaseProperties.Id,
    //                                components[j].BaseProperties.Id,
    //                                dbParams);
    //                        }
    //                    }
    //                }
    //                return wilson;

    //            // =========================================================================
    //            // NRTL ASPEN: 3 Matrices (Aij, Bij, Alpha)
    //            // =========================================================================
    //            case LiquidPhaseModel.NRTL_ASPEN:
    //                double[][,] nrtl = new double[3][,];
    //                nrtl[0] = new double[n, n]; // Aij
    //                nrtl[1] = new double[n, n]; // Bij
    //                nrtl[2] = new double[n, n]; // Alpha

    //                for (int i = 0; i < n; i++)
    //                {
    //                    for (int j = 0; j < n; j++)
    //                    {
    //                        if (i == j)
    //                        {
    //                            nrtl[0][i, j] = 0.0;
    //                            nrtl[1][i, j] = 0.0;
    //                            nrtl[2][i, j] = 0.3; // Default alpha
    //                        }
    //                        else
    //                        {
    //                            nrtl[0][i, j] = BinaryInteractionManager.GetNRTL_ParameterA(
    //                                components[i].BaseProperties.Id,
    //                                components[j].BaseProperties.Id,
    //                                dbParams);

    //                            nrtl[1][i, j] = BinaryInteractionManager.GetNRTL_ParameterB(
    //                                components[i].BaseProperties.Id,
    //                                components[j].BaseProperties.Id,
    //                                dbParams);

    //                            nrtl[2][i, j] = BinaryInteractionManager.GetNRTL_Alpha(
    //                                components[i].BaseProperties.Id,
    //                                components[j].BaseProperties.Id,
    //                                dbParams);
    //                        }
    //                    }
    //                }
    //                return nrtl;

    //            // =========================================================================
    //            // IDEAL / STEAM TABLES: 0 Matrices
    //            // =========================================================================
    //            case LiquidPhaseModel.IdealLiquid:
    //            case LiquidPhaseModel.SteamTables:
    //            default:
    //                return Array.Empty<double[,]>();
    //        }
    //    }
    //}

}
