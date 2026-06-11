using Server.Services;
using UnitSystem;

var builder = WebApplication.CreateBuilder(args);



builder.AddServerServices();
UnitManager.RegisterByAssembly(typeof(SIUnitTypes).Assembly);

var app = builder.Build();
await app.SeedDatabaseAsync();// descomentar cuando se cree nuevas bases de datos o se modifiquen las tablas, para cargar los datos iniciales desde los archivos csv (o generar los archivos si no existen)
app.UseApp();




app.Run();


