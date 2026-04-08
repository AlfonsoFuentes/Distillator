using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.PropertiesDtos.Enums
{
    public enum VaporPhaseModel
    {
        None=0,
        IdealGas = 1,
        SoaveRedlichKwong1972 = 2,
        SteamTables = 3  ,
        VanDerWaals=4,
        RedlichKwong=5,
        Wilson=6,
        PengRobinson=7,
        SoaveRedlichKwong1984=8,
        SoaveRedlichKwong1995=9,

    }

    public enum LiquidPhaseModel
    {
        None=0,
        IdealLiquid = 1,
        NRTL_ASPEN = 2,
        Wilson = 3,
        SteamTables = 4 ,
        EA_Van_Laar   =5,
        WilsonASPEN=6,
        UNIQUAC=7,
       

    }

    public enum BinaryParameterType
    {
        NRTL_A = 1,
        NRTL_B = 2,
        NRTL_C = 3,
        Wilson_A = 4,
        Wilson_B = 5,
        VanLaar_Aij = 6,  // ✅ Agregar esto
        VanLaar_Aji = 7   // ✅ Agregar esto
    }
}
