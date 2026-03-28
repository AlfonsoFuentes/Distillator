using Client;
using Client.Services;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using OfficeOpenXml;
using UnitSystem;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder
.AddRootComponents()
.AddClientServices();


ExcelPackage.License.SetNonCommercialOrganization("AFDS");
UnitManager.RegisterByAssembly(typeof(SIUnitTypes).Assembly);



await builder.Build().RunAsync();
