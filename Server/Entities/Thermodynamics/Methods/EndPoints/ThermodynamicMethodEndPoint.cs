using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Databases;
using Server.Entities.BaseStructure.Components;
using Server.Entities.Thermodynamics.Methods;
using Server.Services;
using Shared.Results;
using Shared.Thermodynamics.Components;
using Shared.Thermodynamics.Methods;
using UnitSystem;

public class ThermodynamicMethodEndPoint : IEndPoint
{
    public void MapEndPoint(IEndpointRouteBuilder app)
    {
        // Seguridad estricta: Solo Ingenieros Administradores (Developer)
        var group = app.MapGroup("/")
                       .RequireAuthorization(new AuthorizeAttribute { Roles = "Developer" });
        group.MapPost("/GetAllCompleteMethods", async ([FromBody] GetAllCompleteMethods request, ApplicationDbContext context) =>
        {
            // 1. Eager Loading con las rutas de navegación exactas de tus entidades
            var entities = await context.ThermodynamicMethods
                // Incluimos los componentes del método y luego la entidad ChemicalComponent asociada
                .Include(m => m.MethodComponents)
                    .ThenInclude(mc => mc.Component)
                // Incluimos los parámetros binarios y sus dos componentes (I y J) para obtener sus nombres
                .Include(m => m.BinaryParameters)
                    .ThenInclude(bp => bp.ComponentI)
                .Include(m => m.BinaryParameters)
                    .ThenInclude(bp => bp.ComponentJ)
                .AsNoTracking()
                .AsSplitQuery() // Previene la explosión cartesiana (Cartesian Explosion)
                .ToListAsync();

            // 2. Mapeo exacto de las Entidades hacia tus DTOs
            var list = entities.Select(e => new ThermodynamicMethodDto
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                VaporModel = e.VaporModel,
                LiquidModel = e.LiquidModel,

                // Mapeo de la colección MethodComponents
                Components = e.MethodComponents.Select(mc => new MethodComponentDto
                {
                    ComponentId = mc.ComponentId,
                    ComponentName = mc.Component.Name, // Obtenido gracias al ThenInclude(mc => mc.Component)
                    MatrixIndex = mc.MatrixIndex
                }).ToList(),

                // Mapeo de la colección BinaryParameters
                BinaryParameters = e.BinaryParameters.Select(bp => new BinaryInteractionParameterDto
                {
                    ComponentI_Id = bp.ComponentI_Id,
                    ComponentI_Name = bp.ComponentI.Name, // Obtenido gracias al ThenInclude
                    ComponentJ_Id = bp.ComponentJ_Id,
                    ComponentJ_Name = bp.ComponentJ.Name, // Obtenido gracias al ThenInclude
                    ParameterType = bp.ParameterType,
                    Value = bp.Value
                }).ToList()
            }).ToList();

            // 3. Retornamos el Result exitoso
            return Results.Ok(Result.Success(list));
        });
        group.MapPost("/GetMethodFullRequest", async ([FromBody] GetMethodFullRequest request, ApplicationDbContext context) =>
        {
            var entity = await context.ThermodynamicMethods
                .Include(m => m.BinaryParameters)
                    .ThenInclude(bp => bp.ComponentI)
                .Include(m => m.BinaryParameters)
                    .ThenInclude(bp => bp.ComponentJ)
                .Include(m => m.MethodComponents)
                    .ThenInclude(mc => mc.Component).ThenInclude(c => c.VaporPressure)
                .Include(m => m.MethodComponents)
                    .ThenInclude(mc => mc.Component).ThenInclude(c => c.HeatOfVaporization)
                .Include(m => m.MethodComponents)
                    .ThenInclude(mc => mc.Component).ThenInclude(c => c.LiquidHeatCapacity)
                .Include(m => m.MethodComponents)
                    .ThenInclude(mc => mc.Component).ThenInclude(c => c.GasHeatCapacity)
                .Include(m => m.MethodComponents)
                    .ThenInclude(mc => mc.Component).ThenInclude(c => c.LiquidViscosity)
                .Include(m => m.MethodComponents)
                    .ThenInclude(mc => mc.Component).ThenInclude(c => c.GasViscosity)
                .Include(m => m.MethodComponents)
                    .ThenInclude(mc => mc.Component).ThenInclude(c => c.LiquidThermalCond)
                .Include(m => m.MethodComponents)
                    .ThenInclude(mc => mc.Component).ThenInclude(c => c.GasThermalCond)
                .Include(m => m.MethodComponents)
                    .ThenInclude(mc => mc.Component).ThenInclude(c => c.Density)
                .Include(m => m.MethodComponents)
                    .ThenInclude(mc => mc.Component).ThenInclude(c => c.SurfaceTension)
                .AsNoTracking()
                .AsSplitQuery()
                .FirstOrDefaultAsync(m => m.Id == request.Id);

            if (entity == null) return Results.Ok(Result.Fail("Method not found"));

            // Proyectamos al nuevo DTO
            var dto = new ThermodynamicMethodFullDto
            {
                Id = entity.Id,
                Name = entity.Name,
                VaporModel = entity.VaporModel,
                LiquidModel = entity.LiquidModel,
                Components = entity.MethodComponents.Select(mc => new MethodComponentFullDto
                {
                    ComponentId = mc.ComponentId,
                    ComponentName = mc.Component.Name,
                    MatrixIndex = mc.MatrixIndex,
                    // Reutilizamos tu Helper MapEntityToDto para la entidad ChemicalComponent
                    FullData = MapEntityToDto(mc.Component)
                }).ToList(),
                BinaryParameters = entity.BinaryParameters.Select(bp => new BinaryInteractionParameterDto
                {
                    ComponentI_Id = bp.ComponentI_Id,
                    ComponentI_Name = bp.ComponentI.Name,
                    ComponentJ_Id = bp.ComponentJ_Id,
                    ComponentJ_Name = bp.ComponentJ.Name,
                    ParameterType = bp.ParameterType,
                    Value = bp.Value
                }).ToList()
            };

            return Results.Ok(Result.Success(dto));
        });
        // ==========================================
        // 1. GET ALL (Lista simplificada para tablas)
        // ==========================================
        group.MapPost("/GetAllMethods", async ([FromBody] GetAllMethods request, ApplicationDbContext context) =>
        {
            var list = await context.ThermodynamicMethods
                .AsNoTracking()
                .Select(m => new ThermodynamicMethodListDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    VaporModel = m.VaporModel,
                    LiquidModel = m.LiquidModel,
                    ComponentCount = m.MethodComponents.Count
                }).ToListAsync();

            return Results.Ok(Result.Success(list));
        });

        // ==========================================
        // 2. GET BY ID (Carga completa para edición)
        // ==========================================
        group.MapPost("/GetMethodById", async ([FromBody] GetMethodById request, ApplicationDbContext context) =>
        {
            var entity = await context.ThermodynamicMethods
                .Include(m => m.MethodComponents).ThenInclude(mc => mc.Component)
                .Include(m => m.BinaryParameters).ThenInclude(bp => bp.ComponentI)
                .Include(m => m.BinaryParameters).ThenInclude(bp => bp.ComponentJ)
                .AsSplitQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == request.Id);

            if (entity == null) return Results.Ok(Result.Fail("Thermodynamic method not found"));

            var dto = MapEntityToDto(entity);
            return Results.Ok(Result.Success(dto));
        });

        // ==========================================
        // 3. CREATE
        // ==========================================
        group.MapPost("/CreateThermodynamicMethod", async ([FromBody] CreateThermodynamicMethod request, ApplicationDbContext context, IWebHostEnvironment env) =>
        {
            if (await context.ThermodynamicMethods.AnyAsync(x => x.Name == request.Name))
                return Results.Ok(Result.Fail("A method with this name already exists"));

            var entity = new ThermodynamicMethod();
            await MapDtoToEntity(request, entity, context);

            context.ThermodynamicMethods.Add(entity);
            await context.SaveChangesAsync();

            // Sincronización con el ADN (CSV) para asegurar resiliencia
            await DatabaseSeeder.SyncMethodsToCsv(context, env.ContentRootPath);

            return Results.Ok(Result.Success());
        });

        // ==========================================
        // 4. EDIT
        // ==========================================
        group.MapPost("/EditThermodynamicMethod", async ([FromBody] EditThermodynamicMethod request, ApplicationDbContext context, IWebHostEnvironment env) =>
        {
            var entity = await context.ThermodynamicMethods
                .Include(m => m.MethodComponents)
                .Include(m => m.BinaryParameters)
                .FirstOrDefaultAsync(m => m.Id == request.Id);

            if (entity == null) return Results.Ok(Result.Fail("Method not found"));

            await MapDtoToEntity(request, entity, context);

            context.ThermodynamicMethods.Update(entity);
            await context.SaveChangesAsync();

            await DatabaseSeeder.SyncMethodsToCsv(context, env.ContentRootPath);

            return Results.Ok(Result.Success());
        });

        // ==========================================
        // 5. DELETE
        // ==========================================
        group.MapPost("/DeleteMethod", async ([FromBody] DeleteMethod request, ApplicationDbContext context, IWebHostEnvironment env) =>
        {
            var entity = await context.ThermodynamicMethods.FindAsync(request.Id);
            if (entity == null) return Results.Ok(Result.Fail("Method not found"));

            context.ThermodynamicMethods.Remove(entity);
            await context.SaveChangesAsync();

            await DatabaseSeeder.SyncMethodsToCsv(context, env.ContentRootPath);

            return Results.Ok(Result.Success());
        });
    }

    // ==========================================
    // HELPERS DE MAPEO (DB <-> DTO)
    // ==========================================

    private async Task MapDtoToEntity(ThermodynamicMethodDto dto, ThermodynamicMethod ent, ApplicationDbContext context)
    {
        ent.Name = dto.Name;
        ent.Description = dto.Description;
        ent.VaporModel = dto.VaporModel;
        ent.LiquidModel = dto.LiquidModel;

        // Sincronización de Componentes vinculados
        ent.MethodComponents.Clear();
        foreach (var compDto in dto.Components)
        {
            var component = await context.ChemicalComponents.FindAsync(compDto.ComponentId);
            if (component != null)
            {
                ent.MethodComponents.Add(new MethodComponent
                {
                    Component = component,
                    MatrixIndex = compDto.MatrixIndex
                });
            }
        }

        // Sincronización de Parámetros Binarios
        ent.BinaryParameters.Clear();
        foreach (var paramDto in dto.BinaryParameters)
        {
            var compI = await context.ChemicalComponents.FindAsync(paramDto.ComponentI_Id);
            var compJ = await context.ChemicalComponents.FindAsync(paramDto.ComponentJ_Id);

            if (compI != null && compJ != null)
            {
                ent.BinaryParameters.Add(new BinaryInteractionParameter
                {
                    ComponentI = compI,
                    ComponentJ = compJ,
                    ParameterType = paramDto.ParameterType,
                    Value = paramDto.Value
                });
            }
        }
    }

    private EditThermodynamicMethod MapEntityToDto(ThermodynamicMethod ent)
    {
        return new EditThermodynamicMethod
        {
            Id = ent.Id,
            Name = ent.Name,
            Description = ent.Description,
            VaporModel = ent.VaporModel,
            LiquidModel = ent.LiquidModel,
            Components = ent.MethodComponents.Select(mc => new MethodComponentDto
            {
                ComponentId = mc.ComponentId,
                ComponentName = mc.Component.Name,
                MatrixIndex = mc.MatrixIndex
            }).ToList(),
            BinaryParameters = ent.BinaryParameters.Select(bp => new BinaryInteractionParameterDto
            {
                ComponentI_Id = bp.ComponentI_Id,
                ComponentI_Name = bp.ComponentI.Name,
                ComponentJ_Id = bp.ComponentJ_Id,
                ComponentJ_Name = bp.ComponentJ.Name,
                ParameterType = bp.ParameterType,
                Value = bp.Value
            }).ToList()
        };
    }
   
    private ChemicalComponentDto MapEntityToDto(ChemicalComponent ent)
    {
        return new ChemicalComponentDto
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