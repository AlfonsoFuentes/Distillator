using System;
using UnitSystem;
Console.WriteLine(new MassFlow(17641, MassFlowUnits.Kg_hr).GetValue(MassFlowUnits.lb_hr));
Console.WriteLine(new Viscosity(0.4104, ViscosityUnits.cPoise).GetValue(ViscosityUnits.lb_ft_hr));
Console.WriteLine(new VolumetricFlow(80, VolumetricFlowUnits.gal_min).GetValue(VolumetricFlowUnits.ft3_sg));
