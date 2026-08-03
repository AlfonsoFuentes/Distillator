using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.PropertiesDtos.Components;
using System.Linq.Expressions;
// using Shared.Units; // Asegúrate de importar tus unidades

namespace Server.Entities.BaseStructure.Components.Configurations
{
    public class ChemicalComponentConfiguration : IEntityTypeConfiguration<ChemicalComponent>
    {
        public void Configure(EntityTypeBuilder<ChemicalComponent> builder)
        {
            // 0. Nombre de la tabla
        

            // ==========================================
            // 1. CONFIGURACIÓN DE LA CLASE BASE (Entity)
            // ==========================================
            builder.HasKey(x => x.Id);
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.Property(x => x.CreatedOn).IsRequired();
            builder.Property(x => x.CreatedBy).HasMaxLength(100);
            builder.Property(x => x.Order).HasDefaultValue(0);
            builder.Ignore(x => x.IsTenanted);

            // ==========================================
            // 2. CONFIGURACIÓN PROPIA DEL COMPONENTE
            // ==========================================
            builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
            builder.Property(x => x.Formula).HasMaxLength(50);
            builder.Property(x => x.StructuralFormula).HasMaxLength(150);
            builder.Property(x => x.Family).HasMaxLength(50);
            builder.Property(x => x.SecondaryFamily).HasMaxLength(50);
            builder.Property(x => x.VaporPressureEquationType).HasConversion<string>().HasMaxLength(80).HasDefaultValue(VaporPressureEquationType.ExtendedAntoine);
            builder.Property(x => x.SaturationTemperatureEquationType).HasConversion<string>().HasMaxLength(80).HasDefaultValue(SaturationTemperatureEquationType.FromVaporPressureSecant);
            builder.Property(x => x.HeatOfVaporizationEquationType).HasConversion<string>().HasMaxLength(80).HasDefaultValue(HeatOfVaporizationEquationType.Dippr106);
            builder.Property(x => x.LiquidHeatCapacityEquationType).HasConversion<string>().HasMaxLength(80).HasDefaultValue(LiquidHeatCapacityEquationType.Polynomial);
            builder.Property(x => x.GasHeatCapacityEquationType).HasConversion<string>().HasMaxLength(80).HasDefaultValue(GasHeatCapacityEquationType.AlyLee);
            builder.Property(x => x.LiquidViscosityEquationType).HasConversion<string>().HasMaxLength(80).HasDefaultValue(LiquidViscosityEquationType.Dippr101);
            builder.Property(x => x.GasViscosityEquationType).HasConversion<string>().HasMaxLength(80).HasDefaultValue(GasViscosityEquationType.Dippr102);
            builder.Property(x => x.LiquidThermalConductivityEquationType).HasConversion<string>().HasMaxLength(80).HasDefaultValue(LiquidThermalConductivityEquationType.Polynomial4);
            builder.Property(x => x.GasThermalConductivityEquationType).HasConversion<string>().HasMaxLength(80).HasDefaultValue(GasThermalConductivityEquationType.PolynomialRational);
            builder.Property(x => x.LiquidDensityEquationType).HasConversion<string>().HasMaxLength(80).HasDefaultValue(LiquidDensityEquationType.Rackett);
            builder.Property(x => x.SurfaceTensionEquationType).HasConversion<string>().HasMaxLength(80).HasDefaultValue(SurfaceTensionEquationType.Dippr106);
            builder.Property(x => x.LiquidEnthalpyEquationType).HasConversion<string>().HasMaxLength(80).HasDefaultValue(LiquidEnthalpyEquationType.IntegratedLiquidCp);
            builder.Property(x => x.GasEnthalpyEquationType).HasConversion<string>().HasMaxLength(80).HasDefaultValue(GasEnthalpyEquationType.IntegratedGasCpWithHvap);
            builder.Property(x => x.SaturatedMolarVolumeEquationType).HasConversion<string>().HasMaxLength(80).HasDefaultValue(SaturatedMolarVolumeEquationType.Rackett);

            // 3. Mapeo de Propiedades StoredAmount (Tipos Complejos Anidados)
            ConfigureAmount(builder, x => x.CriticalTemperature, "CriticalTemperature");
            ConfigureAmount(builder, x => x.BoilingPoint, "BoilingPoint");
            ConfigureAmount(builder, x => x.MeltingPoint, "MeltingPoint");
            ConfigureAmount(builder, x => x.CriticalPressure, "CriticalPressure");
            ConfigureAmount(builder, x => x.CriticalVolume, "CriticalVolume");
            ConfigureAmount(builder, x => x.VolumeAsterisk, "VolumeAsterisk");

            ConfigureAmount(builder, x => x.EnthalpyForm, "EnthalpyForm");
            ConfigureAmount(builder, x => x.GibbsForm, "GibbsForm");
            ConfigureAmount(builder, x => x.EntropyForm, "EntropyForm");
            ConfigureAmount(builder, x => x.CombustionEnthalpy, "CombustionEnthalpy");

            // 4. Mapeo de las Correlaciones Termodinámicas
            ConfigureCorrelation(builder, x => x.VaporPressure, "VaporPressure");
            ConfigureCorrelation(builder, x => x.HeatOfVaporization, "HeatOfVaporization");
            ConfigureCorrelation(builder, x => x.LiquidHeatCapacity, "LiquidHeatCapacity");
            ConfigureCorrelation(builder, x => x.GasHeatCapacity, "GasHeatCapacity");
            ConfigureCorrelation(builder, x => x.LiquidViscosity, "LiquidViscosity");
            ConfigureCorrelation(builder, x => x.GasViscosity, "GasViscosity");
            ConfigureCorrelation(builder, x => x.LiquidThermalCond, "LiquidThermalCond");
            ConfigureCorrelation(builder, x => x.GasThermalCond, "GasThermalCond");
            ConfigureCorrelation(builder, x => x.Density, "Density");
            ConfigureCorrelation(builder, x => x.SurfaceTension, "SurfaceTension");
        }

        // --- MÉTODOS HELPER PRIVADOS (Ahora mucho más limpios) ---

        private void ConfigureAmount(
             EntityTypeBuilder<ChemicalComponent> builder,
             Expression<Func<ChemicalComponent, StoredAmount?>> propertyExpression,
             string columnNamePrefix)
        {
            builder.OwnsOne(propertyExpression, a => {
                a.Property(p => p.Value).HasColumnName($"{columnNamePrefix}_Value");
                a.Property(p => p.UnitName).HasColumnName($"{columnNamePrefix}_Unit").HasMaxLength(50);
            });
        }

        // 👇 Agregamos el '?' en CorrelationCoefficients? 👇
        private void ConfigureCorrelation(
            EntityTypeBuilder<ChemicalComponent> builder,
            Expression<Func<ChemicalComponent, CorrelationCoefficients?>> propertyExpression,
            string prefix)
        {
            builder.OwnsOne(propertyExpression, corr => {
                corr.Property(c => c.C1).HasColumnName($"{prefix}_C1");
                corr.Property(c => c.C2).HasColumnName($"{prefix}_C2");
                corr.Property(c => c.C3).HasColumnName($"{prefix}_C3");
                corr.Property(c => c.C4).HasColumnName($"{prefix}_C4");
                corr.Property(c => c.C5).HasColumnName($"{prefix}_C5");
                corr.Property(c => c.C6).HasColumnName($"{prefix}_C6");
                corr.Property(c => c.C7).HasColumnName($"{prefix}_C7");

                // Mapear el StoredAmount de Tmin
                corr.OwnsOne(c => c.Tmin, t => {
                    t.Property(p => p.Value).HasColumnName($"{prefix}_Tmin_Value");
                    t.Property(p => p.UnitName).HasColumnName($"{prefix}_Tmin_Unit").HasMaxLength(50);
                });

                // Mapear el StoredAmount de Tmax
                corr.OwnsOne(c => c.Tmax, t => {
                    t.Property(p => p.Value).HasColumnName($"{prefix}_Tmax_Value");
                    t.Property(p => p.UnitName).HasColumnName($"{prefix}_Tmax_Unit").HasMaxLength(50);
                });
            });
        }
    }

}
