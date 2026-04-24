using UnitSystem;

namespace Shared.Thermodynamics.Phases
{

    public abstract class ThermodynamicBase
    {

        protected ThermodynamicBase()
        {

        }

        // State Properties
        public Temperature Temperature { get; set; } = new Temperature(0);
        public Pressure Pressure { get; set; } = new Pressure(0);

        // Extensive Properties (Allowed to be set in the leaf)
        public MolarFlow MolarFlow { get; set; } = new MolarFlow(0);
        public MassFlow MassFlow { get; set; } = new MassFlow(0);
        public VolumetricFlow VolumetricFlow { get; set; } = new VolumetricFlow(0);
        public EnergyFlow EnthalpyFlow { get; set; } = new EnergyFlow(0);

        // Intensive Properties
        public double MolecularWeight { get; set; }
        public MassDensity MassDensity { get; set; } = new MassDensity(0);
        public MolarDensity MolarDensity { get; set; } = new MolarDensity(0);
        public MolarEnergy MolarEnthalpy { get; set; } = new MolarEnergy(0);
        public MassEnergy MassEnthalpy { get; set; } = new MassEnergy(0);
        public MassEntropy MassHeatCapacity { get; set; } = new MassEntropy(0);
        public MolarEntropy MolarHeatCapacity { get; set; } = new MolarEntropy(0);
        public MassEntropy MassEntropy { get; set; } = new MassEntropy(0);
        public MolarEntropy MolarEntropy { get; set; } = new MolarEntropy(0);
        public Viscosity Viscosity { get; set; } = new Viscosity(0);
        public ThermalConductivity ThermalConductivity { get; set; } = new ThermalConductivity(0);
        public SuperficialTension SurfaceTension { get; set; } = new SuperficialTension(0);

        // Critical & Saturation Properties
        public Pressure SaturationPressure { get; set; } = new Pressure(0);
        public Temperature SaturationTemperature { get; set; } = new Temperature(0);
        public Temperature CriticalTemperature { get; set; } = new Temperature(0);
        public Pressure CriticalPressure { get; set; } = new Pressure(0);
        public MolarVolumeSpecific CriticalMolarVolume { get; set; } = new MolarVolumeSpecific(0);
        public MolarVolumeSpecific MolarVolume { get; set; } = new MolarVolumeSpecific(0);

        // Acentric Factor (Vital for Peng-Robinson/Soave-Redlich-Kwong)
        public double AcentricFactor { get; set; }

        public virtual void SetTemperature(Temperature? temperature)
        {
            Temperature = temperature ?? new Temperature(0);  // ✅ Usa default si es null
        }
        public virtual void SetPressure(Pressure? pressure)
        {
            Pressure = pressure ?? new Pressure(0);  // ✅ Usa default si es null
        }




        public virtual void SetMolarFlow(MolarFlow? molarFlow)
        {
            MolarFlow = molarFlow ?? new MolarFlow(0);
        }

        public virtual void SetMassFlow(MassFlow? massFlow)
        {

            MassFlow = massFlow ?? new MassFlow(0);
        }

        public virtual void SetVolumetricFlow(VolumetricFlow? volumetricFlow)
        {
            VolumetricFlow = volumetricFlow ?? new VolumetricFlow(0);
        }



    }
}
