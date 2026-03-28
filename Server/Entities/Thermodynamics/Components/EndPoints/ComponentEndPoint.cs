using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Databases;
using Server.Entities.BaseStructure.Components;
using Server.Services;
using Shared.Results;
using Shared.Thermodynamics.Components;
using UnitSystem;

namespace Server.Endpoints
{
    public class ChemicalComponentEndPoint : IEndPoint
    {
        public void MapEndPoint(IEndpointRouteBuilder app)
        {
            // Aplicamos seguridad estricta a todos los comandos de este grupo
            var group = app.MapGroup("/")
                           .RequireAuthorization(new AuthorizeAttribute { Roles = "Developer" });


            group.MapPost("/GetAllCompleteComponents", async ([FromBody] GetAllCompleteComponents request, ApplicationDbContext context) =>
            {
                // Traemos la entidad completa con todas sus relaciones (Eager Loading)
                var entities = await context.ChemicalComponents
                    .Include(c => c.VaporPressure)
                    .Include(c => c.HeatOfVaporization)
                    .Include(c => c.LiquidHeatCapacity)
                    .Include(c => c.GasHeatCapacity)
                    .Include(c => c.LiquidViscosity)
                    .Include(c => c.GasViscosity)
                    .Include(c => c.LiquidThermalCond)
                    .Include(c => c.GasThermalCond)
                    .Include(c => c.Density)
                    .Include(c => c.SurfaceTension)
                    .AsNoTracking()
                
                .AsSplitQuery()// Fundamental para que el EF Core no rastree cambios y sea ultra rápido
                    .ToListAsync();

                // Usamos tu Helper para convertir cada Entidad en el DTO inflado con Amounts
                var list = entities.Select(e => MapEntityToDto(e)).ToList();

                return Results.Ok(Result.Success(list));
            });
            // ==========================================
            // 1. GET ALL (Via POST)
            // ==========================================
            group.MapPost("/GetAllComponents", async ([FromBody] GetAllComponents request, ApplicationDbContext context) =>
            {
                var list = await context.ChemicalComponents
                .AsNoTracking()
             
                    .Select(c => new ChemicalComponentListDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Formula = c.Formula,
                        Family = c.Family,
                        MolecularWeight = c.MolecularWeight
                    }).ToListAsync();

                return Results.Ok(Result.Success(list));
            });

            // ==========================================
            // 2. GET BY ID (Via POST)
            // ==========================================
            group.MapPost("/GetComponentById", async ([FromBody] GetComponentById request, ApplicationDbContext context) =>
            {
                var entity = await context.ChemicalComponents.FindAsync(request.Id);
                if (entity == null) return Results.Ok(Result.Fail("Component not found"));

                var dto = MapEntityToDto(entity);
                return Results.Ok(Result.Success(dto));
            });

            // ==========================================
            // 3. CREATE (Via POST)
            // ==========================================
            group.MapPost("/CreateChemicalComponent", async ([FromBody] CreateChemicalComponent request, ApplicationDbContext context, IWebHostEnvironment env) =>
            {
                if (await context.ChemicalComponents.AnyAsync(x => x.Name == request.Name))
                    return Results.Ok(Result.Fail("Component alredy exist")) ;

                var entity = new ChemicalComponent();
                MapDtoToEntity(request, entity);

                context.ChemicalComponents.Add(entity);
                await context.SaveChangesAsync();

                // Sincronización con el ADN (CSV)
                await DatabaseSeeder.SyncDatabaseToCsv(context, env.ContentRootPath);

                return Results.Ok(Result.Success());
            });

            // ==========================================
            // 4. EDIT (Via POST)
            // ==========================================
            group.MapPost("/EditChemicalComponent", async ([FromBody] EditChemicalComponent request, ApplicationDbContext context, IWebHostEnvironment env) =>
            {
                var entity = await context.ChemicalComponents.FindAsync(request.Id);
                if (entity == null) return Results.Ok(Result.Fail("Component not found"));

                MapDtoToEntity(request, entity);

                context.ChemicalComponents.Update(entity);
                await context.SaveChangesAsync();

                // Sincronización con el ADN (CSV)
                await DatabaseSeeder.SyncDatabaseToCsv(context, env.ContentRootPath);

                return Results.Ok(Result.Success());
            });

            // ==========================================
            // 5. DELETE (Via POST)
            // ==========================================
            group.MapPost("/DeleteComponent", async ([FromBody] DeleteComponent request, ApplicationDbContext context, IWebHostEnvironment env) =>
            {
                var entity = await context.ChemicalComponents.FindAsync(request.Id);
                if (entity == null) return Results.Ok(Result.Fail("Component not found"));

                context.ChemicalComponents.Remove(entity);
                await context.SaveChangesAsync();

                // Sincronización con el ADN (CSV)
                await DatabaseSeeder.SyncDatabaseToCsv(context, env.ContentRootPath);

                return Results.Ok(Result.Success());
            });

            // ==========================================
            // 6. VALIDATE (Via POST) - Para PostForValidationAsync
            // ==========================================
            group.MapPost("/ValidateComponente", async ([FromBody] ValidateComponente request, ApplicationDbContext context) =>
            {
                // Ejemplo de validación: Verificar que el nombre no esté repetido en otro ID
                bool isValid = !await context.ChemicalComponents
                    .AnyAsync(x => x.Name == request.Name && x.Id != request.Id);

                // Tu método HttpService espera un Result<bool>
                return Results.Ok(Result.Success());
            });
        }

        // ==========================================
        // HELPERS DE MAPEO (DB <-> DTO Inteligente)
        // ==========================================
        private void MapDtoToEntity(ChemicalComponentDto dto, ChemicalComponent ent)
        {
            ent.Name = dto.Name;
            ent.Formula = dto.Formula;
            ent.StructuralFormula = dto.StructuralFormula;
            ent.Family = dto.Family;
            ent.SecondaryFamily = dto.SecondaryFamily;
            ent.MolecularWeight = dto.MolecularWeight;

            // Mapeo desde tus clases de UnitSystem (Amount) hacia StoredAmount
            ent.CriticalTemperature = new(dto.CriticalTemperature.Value, dto.CriticalTemperature.UnitName);
            ent.CriticalPressure = new(dto.CriticalPressure.Value, dto.CriticalPressure.UnitName);
            ent.BoilingPoint = new(dto.BoilingPoint.Value, dto.BoilingPoint.UnitName);
            ent.MeltingPoint = new(dto.MeltingPoint.Value, dto.MeltingPoint.UnitName);
            ent.CriticalVolume = new(dto.CriticalVolume.Value, dto.CriticalVolume.UnitName);
            ent.VolumeAsterisk = new(dto.VolumeAsterisk.Value, dto.VolumeAsterisk.UnitName);

            ent.CriticalZ = dto.CriticalZ;
            ent.AcentricFactor = dto.AcentricFactor;
            ent.AcentricFactorPitzer = dto.AcentricFactorPitzer;

            ent.EnthalpyForm = new(dto.EnthalpyForm.Value, dto.EnthalpyForm.UnitName);
            ent.GibbsForm = new(dto.GibbsForm.Value, dto.GibbsForm.UnitName);
            ent.EntropyForm = new(dto.EntropyForm.Value, dto.EntropyForm.UnitName);
            ent.CombustionEnthalpy = new(dto.CombustionEnthalpy.Value, dto.CombustionEnthalpy.UnitName);

            // Correlaciones
            ent.VaporPressure = MapCorrDtoToEnt(dto.VaporPressure);
            ent.HeatOfVaporization = MapCorrDtoToEnt(dto.HeatOfVaporization);
            ent.LiquidHeatCapacity = MapCorrDtoToEnt(dto.LiquidHeatCapacity);
            ent.GasHeatCapacity = MapCorrDtoToEnt(dto.GasHeatCapacity);
            ent.LiquidViscosity = MapCorrDtoToEnt(dto.LiquidViscosity);
            ent.GasViscosity = MapCorrDtoToEnt(dto.GasViscosity);
            ent.LiquidThermalCond = MapCorrDtoToEnt(dto.LiquidThermalCond);
            ent.GasThermalCond = MapCorrDtoToEnt(dto.GasThermalCond);
            ent.Density = MapCorrDtoToEnt(dto.Density);
            ent.SurfaceTension = MapCorrDtoToEnt(dto.SurfaceTension);
        }

        private EditChemicalComponent MapEntityToDto(ChemicalComponent ent)
        {
            return new EditChemicalComponent
            {
                Id = ent.Id,
                Name = ent.Name,
                Formula = ent.Formula,
                StructuralFormula = ent.StructuralFormula,
                Family = ent.Family,
                SecondaryFamily = ent.SecondaryFamily,
                MolecularWeight = ent.MolecularWeight,

                // Inyectamos de la DB al DTO para que recupere la inteligencia física
                CriticalTemperature = new Temperature(ent.CriticalTemperature.Value, ent.CriticalTemperature.UnitName),
                CriticalPressure = new Pressure(ent.CriticalPressure.Value, ent.CriticalPressure.UnitName),
                BoilingPoint = new Temperature(ent.BoilingPoint.Value, ent.BoilingPoint.UnitName),
                MeltingPoint = new Temperature(ent.MeltingPoint.Value, ent.MeltingPoint.UnitName),
                CriticalVolume = new MolarVolumeSpecific(ent.CriticalVolume.Value, ent.CriticalVolume.UnitName),
                VolumeAsterisk = new MolarVolumeSpecific(ent.VolumeAsterisk.Value, ent.VolumeAsterisk.UnitName),

                CriticalZ = ent.CriticalZ,
                AcentricFactor = ent.AcentricFactor,
                AcentricFactorPitzer = ent.AcentricFactorPitzer,

                EnthalpyForm = new MolarEnergy(ent.EnthalpyForm.Value, ent.EnthalpyForm.UnitName),
                GibbsForm = new MolarEnergy(ent.GibbsForm.Value, ent.GibbsForm.UnitName),
                EntropyForm = new MolarEntropy(ent.EntropyForm.Value, ent.EntropyForm.UnitName),
                CombustionEnthalpy = new MolarEnergy(ent.CombustionEnthalpy.Value, ent.CombustionEnthalpy.UnitName),

                // Mapeo manual de las correlaciones (Omitidas por brevedad, sigue el mismo patrón)
                VaporPressure = MapCorrEntToDto(ent.VaporPressure),
                HeatOfVaporization = MapCorrEntToDto(ent.HeatOfVaporization),
                LiquidHeatCapacity = MapCorrEntToDto(ent.LiquidHeatCapacity),
                GasHeatCapacity = MapCorrEntToDto(ent.GasHeatCapacity),
                LiquidViscosity = MapCorrEntToDto(ent.LiquidViscosity),
                GasViscosity = MapCorrEntToDto(ent.GasViscosity),
                LiquidThermalCond = MapCorrEntToDto(ent.LiquidThermalCond),
                GasThermalCond = MapCorrEntToDto(ent.GasThermalCond),
                Density = MapCorrEntToDto(ent.Density),
                SurfaceTension = MapCorrEntToDto(ent.SurfaceTension)
            };
        }

        private CorrelationCoefficients MapCorrDtoToEnt(CorrelationCoefficientsDto d) => new()
        {
            C1 = d.C1,
            C2 = d.C2,
            C3 = d.C3,
            C4 = d.C4,
            C5 = d.C5,
            C6 = d.C6,
            C7 = d.C7,
            Tmin = new(d.Tmin.Value, d.Tmin.UnitName),
            Tmax = new(d.Tmax.Value, d.Tmax.UnitName)
        };

        private CorrelationCoefficientsDto MapCorrEntToDto(CorrelationCoefficients e) => new()
        {
            C1 = e.C1,
            C2 = e.C2,
            C3 = e.C3,
            C4 = e.C4,
            C5 = e.C5,
            C6 = e.C6,
            C7 = e.C7,
            Tmin = new Temperature(e.Tmin.Value, e.Tmin.UnitName),
            Tmax = new Temperature(e.Tmax.Value, e.Tmax.UnitName)
        };
    }
}