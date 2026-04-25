using Server.Services;
using UnitSystem;

var builder = WebApplication.CreateBuilder(args);



builder.AddServerServices();
UnitManager.RegisterByAssembly(typeof(SIUnitTypes).Assembly);

var app = builder.Build();
//await app.SeedDatabaseAsync();
app.UseApp();




app.Run();


