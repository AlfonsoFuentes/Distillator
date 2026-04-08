using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
#nullable disable
namespace UnitSystem
{
    public class UnitLess : Amount
    {
        public UnitLess(UnitMeasure u) : base(0, u)
        {

        }
        public UnitLess(double dvalue) : base(dvalue, UnitMeasure.None)
        {

        }

    }
    public class Length : Amount
    {
        public Length() : this(0)
        {

        }
        public Length(double dvalue) : base(dvalue, LengthUnits.Meter)
        {

        }
        public Length(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public Length(UnitMeasure u) : this(0, u)
        {

        }
        public Length(double dvalue, string u) : base(dvalue, u)
        {

        }
    }
    public class Diameter : Amount
    {
        public Diameter() : this(0, DiameterUnits.Inch)
        {

        }
        public Diameter(double dvalue) : base(dvalue, DiameterUnits.Inch)
        {

        }
        public Diameter(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public Diameter(double dvalue, string u) : base(dvalue, u)
        {

        }
        public Diameter(UnitMeasure u) : this(0, u)
        {

        }

    }
    public class Area : Amount
    {
        public Area() : this(0)
        {

        }
        public Area(double dvalue) : base(dvalue, SurfaceUnits.Meter2)
        {

        }
        public Area(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public Area(UnitMeasure u) : this(0, u)
        {

        }
        public Area(double dvalue, string u) : base(dvalue, u)
        {

        }
    }
    public class Volume : Amount
    {
        public Volume() : this(0)
        {

        }
        public Volume(double dvalue) : base(dvalue, VolumeUnits.Meter3)
        {

        }
        public Volume(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public Volume(UnitMeasure u) : this(0, u)
        {

        }
        public Volume(double dvalue, string u) : base(dvalue, u)
        {

        }
    }
    public class Time : Amount
    {
        public Time() : this(0)
        {

        }
        public Time(double dvalue) : base(dvalue, TimeUnits.Second)
        {

        }
        public Time(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public Time(UnitMeasure u) : this(0, u)
        {

        }
        public Time(double dvalue, string u) : base(dvalue, u)
        {

        }

    }
    public class MilestoneDuration : Amount
    {
        public MilestoneDuration() : this(0)
        {

        }
        public MilestoneDuration(double dvalue) : base(dvalue, MilestoneDurationUnits.Day)
        {

        }
        public MilestoneDuration(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public MilestoneDuration(UnitMeasure u) : this(0, u)
        {

        }
        public MilestoneDuration(double dvalue, string u) : base(dvalue, u)
        {

        }

    }
    public class Velocity : Amount
    {
        public Velocity() : this(0)
        {

        }
        public Velocity(double dvalue) : base(dvalue, VelocityUnits.MeterPerSecond)
        {

        }
        public Velocity(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public Velocity(UnitMeasure u) : this(0, u)
        {

        }
        public Velocity(double dvalue, string u) : base(dvalue, u)
        {

        }
    }
    public class Mass : Amount
    {
        public Mass() : base()
        {

        }
        public Mass(double dvalue) : base(dvalue, MassUnits.KiloGram)
        {

        }
        public Mass(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public Mass(UnitMeasure u) : this(0, u)
        {

        }
        public Mass(double dvalue, string u) : base(dvalue, u)
        {

        }
    }
    public class Force : Amount
    {
        public Force() : this(0)
        {

        }
        public Force(double dvalue) : base(dvalue, ForceUnits.Newton)
        {

        }
        public Force(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public Force(UnitMeasure u) : this(0, u)
        {

        }
        public Force(double dvalue, string u) : base(dvalue, u)
        {

        }
    }

    public class Electric : Amount
    {
        public Electric() : this(0)
        {

        }
        public Electric(double dvalue) : base(dvalue, ElectricUnits.Ampere)
        {

        }
        public Electric(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public Electric(UnitMeasure u) : this(0, u)
        {

        }
        public Electric(double dvalue, string u) : base(dvalue, u)
        {

        }
    }
    public class Power : Amount
    {
        public Power() : this(0)
        {

        }
        public Power(double dvalue) : base(dvalue, PowerUnits.KiloWatt)
        {

        }
        public Power(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public Power(UnitMeasure u) : this(0, u)
        {

        }
        public Power(double dvalue, string u) : base(dvalue, u)
        {

        }
    }
    public class Energy : Amount
    {
        public Energy() : this(0)
        {

        }
        public Energy(double dvalue) : base(dvalue, EnergyUnits.KiloCalorie)
        {

        }
        public Energy(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public Energy(UnitMeasure u) : this(0, u)
        {

        }
        public Energy(double dvalue, string u) : base(dvalue, u)
        {

        }
    }
    public class Temperature : Amount
    {
        public Temperature() : this(0)
        {

        }
        public Temperature(double dvalue) : base(dvalue, TemperatureUnits.DegreeCelcius)
        {

        }
        public Temperature(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public Temperature(UnitMeasure u) : this(0, u)
        {

        }
        public Temperature(double dvalue, string u) : base(dvalue, u)
        {

        }
    }
    public class Pressure : Amount
    {
        public Pressure() : this(0)
        {

        }
        public static Amount manometric;

        public Pressure(double dvalue) : base(dvalue, PressureUnits.Bara)
        {

        }
        public Pressure(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public Pressure(UnitMeasure u) : this(0, u)
        {

        }
        public Pressure(double dvalue, string u) : base(dvalue, u)
        {

        }
    }
    public class MotorVelocity : Amount
    {
        public MotorVelocity() : this(0)
        {

        }
        public MotorVelocity(double dvalue) : base(dvalue, MotorVelocityUnits.Hertz)
        {

        }
        public MotorVelocity(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public MotorVelocity(UnitMeasure u) : this(0, u)
        {

        }
        public MotorVelocity(double dvalue, string u) : base(dvalue, u)
        {

        }
        public Amount NominalRPM { get; set; }
        public Amount CurrentRPM { get; set; }
        public Amount SpeedVelocity { get; set; }

        public MotorVelocity(MotorVelocity moto) : base(0, MotorVelocityUnits.Hertz)
        {
            NominalRPM = new Amount(moto.NominalRPM);
            CurrentRPM = new Amount(moto.CurrentRPM);
            SpeedVelocity = new Amount(moto.SpeedVelocity);
        }
        void CalculateRPM()
        {
            CurrentRPM.SetValue(SpeedVelocity.Value * NominalRPM.Value / 100, MotorVelocityUnits.RPM);
        }
    }
    public class Percentage : Amount
    {
        public Percentage() : this(0)
        {

        }
        public Percentage(double dvalue) : base(dvalue, PercentageUnits.Percentage)
        {

        }
        public Percentage(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public Percentage(double dvalue, string u) : base(dvalue, u)
        {

        }

        public override void SetValue(double dvalue, UnitMeasure unit)
        {
            if (dvalue < 0) dvalue = 0;
            if (dvalue > 100) dvalue = 100;

            this.dvalue = dvalue;
            this.unit = unit;
        }

    }
    public class AmountOfSubstance : Amount
    {
        public AmountOfSubstance() : this(0)
        {

        }
        public AmountOfSubstance(double dvalue) : base(dvalue, AmountOfSubstanceUnits.Mole)
        {

        }
        public AmountOfSubstance(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public AmountOfSubstance(UnitMeasure u) : this(0, u)
        {

        }
        public AmountOfSubstance(double dvalue, string u) : base(dvalue, u)
        {

        }

    }

    public class HeatTransferCoefficient : Amount
    {
        public HeatTransferCoefficient() : this(0)
        {

        }
        public HeatTransferCoefficient(double dvalue) : base(dvalue, HeatTransferCoefficientUnits.BTU_hr_ft2_F)
        {

        }
        public HeatTransferCoefficient(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public HeatTransferCoefficient(UnitMeasure u) : this(0, u)
        {

        }
        public HeatTransferCoefficient(double dvalue, string u) : base(dvalue, u)
        {

        }

    }
    public class MassDensity : Amount
    {
        public MassDensity() : this(0)
        {

        }
        public MassDensity(double dvalue) : base(dvalue, MassDensityUnits.Kg_m3)
        {

        }
        public MassDensity(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public MassDensity(UnitMeasure u) : this(0, u)
        {

        }
        public MassDensity(double dvalue, string u) : base(dvalue, u)
        {

        }
    }
    public class PressureDrop : Amount
    {
        public PressureDrop() : this(0)
        {

        }
        public PressureDrop(double dvalue) : base(dvalue, PressureDropUnits.psi)
        {

        }
        public PressureDrop(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public PressureDrop(UnitMeasure u) : this(0, u)
        {

        }
        public PressureDrop(double dvalue, string u) : base(dvalue, u)
        {

        }
    }
    public class PressureDropLength : Amount
    {
        public PressureDropLength() : this(0)
        {

        }
        public PressureDropLength(double dvalue) : base(dvalue, PressureDropLengthUnits.psi_100ft)
        {

        }
        public PressureDropLength(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public PressureDropLength(UnitMeasure u) : this(0, u)
        {

        }
        public PressureDropLength(double dvalue, string u) : base(dvalue, u)
        {

        }
    }
    public class ThermalConductivity : Amount
    {
        public ThermalConductivity() : this(0)
        {

        }
        public ThermalConductivity(double dvalue) : base(dvalue, ThermalConductivityUnits.W_m_K)
        {

        }
        public ThermalConductivity(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public ThermalConductivity(UnitMeasure u) : this(0, u)
        {

        }
        public ThermalConductivity(double dvalue, string u) : base(dvalue, u)
        {

        }

    }
    public class VolumeEnergy : Amount
    {
        public VolumeEnergy() : this(0)
        {

        }
        public VolumeEnergy(double dvalue) : base(dvalue, VolumeEnergyUnits.Kcal_m3)
        {

        }
        public VolumeEnergy(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public VolumeEnergy(UnitMeasure u) : this(0, u)
        {

        }
        public VolumeEnergy(double dvalue, string u) : base(dvalue, u)
        {

        }
    }
    public class MassEnergy : Amount
    {
        public MassEnergy() : this(0)
        {

        }
        public MassEnergy(double dvalue) : base(dvalue, MassEnergyUnits.Kcal_Kg)
        {

        }
        public MassEnergy(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public MassEnergy(UnitMeasure u) : this(0, u)
        {

        }
        public MassEnergy(double dvalue, string u) : base(dvalue, u)
        {

        }
    }
    public class MassEntropy : Amount
    {
        public MassEntropy() : this(0)
        {

        }
        public MassEntropy(double dvalue) : base(dvalue, MassEntropyUnits.Kcal_Kg_C)
        {

        }
        public MassEntropy(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public MassEntropy(UnitMeasure u) : this(0, u)
        {

        }
        public MassEntropy(double dvalue, string u) : base(dvalue, u)
        {

        }
    }
    public class MassFlow : Amount
    {
        public MassFlow() : this(0)
        {

        }
        public MassFlow(double dvalue) : base(dvalue, MassFlowUnits.Kg_hr)
        {

        }
        public MassFlow(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public MassFlow(UnitMeasure u) : this(0, u)
        {

        }
        public MassFlow(double dvalue, string u) : base(dvalue, u)
        {

        }
    }
    public class MolarFlow : Amount
    {
        public MolarFlow() : this(0)
        {

        }
        public MolarFlow(double dvalue) : base(dvalue, MolarFlowUnits.Kgmol_hr)
        {

        }
        public MolarFlow(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public MolarFlow(UnitMeasure u) : this(0, u)
        {

        }
        public MolarFlow(double dvalue, string u) : base(dvalue, u)
        {

        }
    }
    public class HeatSurfaceFlow : Amount
    {
        public HeatSurfaceFlow() : this(0)
        {

        }
        public HeatSurfaceFlow(double dvalue) : base(dvalue, HeatSurfaceFlowUnits.BTU_hr_ft2)
        {

        }
        public HeatSurfaceFlow(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public HeatSurfaceFlow(UnitMeasure u) : this(0, u)
        {

        }
        public HeatSurfaceFlow(double dvalue, string u) : base(dvalue, u)
        {

        }

    }
    public class VolumetricFlow : Amount
    {
        public VolumetricFlow() : this(0)
        {

        }
        public VolumetricFlow(double dvalue) : base(dvalue, VolumetricFlowUnits.m3_hr)
        {

        }
        public VolumetricFlow(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public VolumetricFlow(UnitMeasure u) : this(0, u)
        {

        }
        public VolumetricFlow(double dvalue, string u) : base(dvalue, u)
        {

        }
    }
    public class EnergyFlow : Amount
    {
        public EnergyFlow() : this(0)
        {

        }
        public EnergyFlow(double dvalue) : base(dvalue, EnergyFlowUnits.Kcal_hr)
        {

        }
        public EnergyFlow(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public EnergyFlow(UnitMeasure u) : this(0, u)
        {

        }
        public EnergyFlow(double dvalue, string u) : base(dvalue, u)
        {

        }
    }
    public class Viscosity : Amount
    {
        public Viscosity() : this(0)
        {

        }
        public Viscosity(double dvalue) : base(dvalue, ViscosityUnits.cPoise)
        {

        }
        public Viscosity(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public Viscosity(UnitMeasure u) : this(0, u)
        {

        }
        public Viscosity(double dvalue, string u) : base(dvalue, u)
        {

        }
    }
    public class LineVelocity : Amount
    {
        public LineVelocity() : this(0)
        {

        }
        public LineVelocity(double dvalue) : base(dvalue, LineVelocityUnits.EA_min)
        {

        }
        public LineVelocity(double dvalue, UnitMeasure u) : base(dvalue, u)
        {

        }
        public LineVelocity(UnitMeasure u) : this(0, u)
        {

        }
        public LineVelocity(double dvalue, string u) : base(dvalue, u)
        {

        }
    }

    public class MassVolumeSpecific : Amount
    {
        public MassVolumeSpecific() : this(0)
        {
        }
        public MassVolumeSpecific(double dvalue) : base(dvalue, MassVolumeSpecificUnits.cm3_g)
        {
        }
        public MassVolumeSpecific(double dvalue, UnitMeasure u) : base(dvalue, u)
        {
        }
        public MassVolumeSpecific(UnitMeasure u) : this(0, u)
        {
        }
        public MassVolumeSpecific(double dvalue, string u) : base(dvalue, u)
        {
        }
    }
    public class MolarDensity : Amount
    {
        public MolarDensity() : this(0)
        {
        }
        public MolarDensity(double dvalue) : base(dvalue, MolarDensityUnits.gmol_L)
        {
        }
        public MolarDensity(double dvalue, UnitMeasure u) : base(dvalue, u)
        {
        }
        public MolarDensity(UnitMeasure u) : this(0, u)
        {
        }
        public MolarDensity(double dvalue, string u) : base(dvalue, u)
        {
        }
    }
    public class MolarVolumeSpecific : Amount
    {
        public MolarVolumeSpecific() : this(0)
        {
        }
        public MolarVolumeSpecific(double dvalue) : base(dvalue, MolarVolumeSpecificUnits.L_gmol)
        {
        }
        public MolarVolumeSpecific(double dvalue, UnitMeasure u) : base(dvalue, u)
        {
        }
        public MolarVolumeSpecific(UnitMeasure u) : this(0, u)
        {
        }
        public MolarVolumeSpecific(double dvalue, string u) : base(dvalue, u)
        {
        }
    }
    public class MolarEnergy : Amount
    {
        public MolarEnergy() : this(0)
        {
        }
        public MolarEnergy(double dvalue) : base(dvalue, MolarEnergyUnits.Kcal_gmol)
        {
        }
        public MolarEnergy(double dvalue, UnitMeasure u) : base(dvalue, u)
        {
        }
        public MolarEnergy(UnitMeasure u) : this(0, u)
        {
        }
        public MolarEnergy(double dvalue, string u) : base(dvalue, u)
        {
        }
    }
    public class MolarEntropy:Amount
    {
        public MolarEntropy() : this(0)
        {
        }
        public MolarEntropy(double dvalue) : base(dvalue, MolarEntropyUnits.KJ_Kgmol_C)
        {
        }
        public MolarEntropy(double dvalue, UnitMeasure u) : base(dvalue, u)
        {
        }
        public MolarEntropy(UnitMeasure u) : this(0, u)
        {
        }
        public MolarEntropy(double dvalue, string u) : base(dvalue, u)
        {
        }
    }
    public class SuperficialTension : Amount
    {
        public SuperficialTension() : this(0)
        {
        }
        public SuperficialTension(double dvalue) : base(dvalue, MolarEntropyUnits.KJ_Kgmol_C)
        {
        }
        public SuperficialTension(double dvalue, UnitMeasure u) : base(dvalue, u)
        {
        }
        public SuperficialTension(UnitMeasure u) : this(0, u)
        {
        }
        public SuperficialTension(double dvalue, string u) : base(dvalue, u)
        {
        }
    }

}

