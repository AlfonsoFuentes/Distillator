using Client.Services;
using Client.Services.EquipmentManagers;
using Client.Services.LayoutServices;
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
builder.Services.AddSingleton<INamingService, EquipmentNamingService>();
builder.Services.AddSingleton<IEquipmentFactory, EquipmentFactory>();
builder.Services.AddScoped<WorkspaceManager>();
builder.Services.AddScoped<IMainSolver,MainSolver>();
// 4. Registro del Motor Principal

ExcelPackage.License.SetNonCommercialOrganization("AFDS");
UnitManager.RegisterByAssembly(typeof(SIUnitTypes).Assembly);



await builder.Build().RunAsync();
