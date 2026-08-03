using Client.Services;
using Client.Services.EquipmentManagers;
using Client.Services.LayoutServices;
using Client.Services.ProjectWorkspace;
using Distillator.Domain.Inputs;
using Distillator.Domain.Policies;
using Distillator.Domain.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OfficeOpenXml;
using Shared.SolverConsecutive;
using Shared.WorkSpaceManagers;
using UnitSystem;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder
.AddRootComponents()
.AddClientServices();

builder.Services.AddScoped<DragStateService>();
// builder.Services.AddSingleton<INamingService, Client.Services.EquipmentManagers.EquipmentNamingService>(); // --- LEGACY (EquipmentFactory ya no usa INamingService)
builder.Services.AddSingleton<IEquipmentFactory, EquipmentFactory>();
// builder.Services.AddScoped<WorkspaceManager>(); // --- LEGACY (comentado)
builder.Services.AddScoped<FlowsheetManager>();
builder.Services.AddScoped<IMainSolver,MainSolver>();
builder.Services.AddScoped<VariableInputCommandHandler>();
builder.Services.AddScoped<CompositionInputCommandHandler>();
builder.Services.AddScoped<FormulaSpecificationCommandHandler>();

// Servicios de dominio para el nuevo workspace
builder.Services.AddScoped<ICameraService, Distillator.Domain.Services.CameraService>();
builder.Services.AddScoped<IPlacementRules, PlacementRules>();
builder.Services.AddScoped<Distillator.Domain.Policies.IEquipmentNamingService, Distillator.Domain.Policies.EquipmentNamingService>();

// Servicios de UI para el nuevo workspace
builder.Services.AddScoped<FlowsheetCanvasLayoutService>();
builder.Services.AddScoped<FlowsheetStyleService>();
builder.Services.AddScoped<EquipmentDragService>();
// 4. Registro del Motor Principal

ExcelPackage.License.SetNonCommercialOrganization("AFDS");
UnitManager.RegisterByAssembly(typeof(SIUnitTypes).Assembly);



await builder.Build().RunAsync();
