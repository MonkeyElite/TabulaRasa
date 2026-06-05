using Microsoft.EntityFrameworkCore;
using TabulaRasa.Api.Persistence;
using TabulaRasa.Api.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy
            .WithOrigins("http://localhost:3000", "http://127.0.0.1:3000")
            .AllowAnyHeader()
            .AllowAnyMethod());
});
builder.Services.AddControllers();
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));

string? simulationDatabase = builder.Configuration.GetConnectionString("SimulationDatabase");
if (string.IsNullOrWhiteSpace(simulationDatabase))
{
    builder.Services.AddSingleton<ISimulationPersistenceStore, NullSimulationPersistenceStore>();
}
else
{
    builder.Services.AddDbContextFactory<SimulationDbContext>(options =>
        options.UseNpgsql(simulationDatabase));
    builder.Services.AddSingleton<ISimulationPersistenceStore, PostgresSimulationPersistenceStore>();
}

builder.Services.AddSingleton<SimulationRegistry>();

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    ISimulationPersistenceStore store = scope.ServiceProvider.GetRequiredService<ISimulationPersistenceStore>();
    if (store.IsDurable && store.Options.ApplyMigrationsOnStartup)
    {
        IDbContextFactory<SimulationDbContext> factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SimulationDbContext>>();
        using SimulationDbContext db = factory.CreateDbContext();
        db.Database.Migrate();
    }
}

app.UseCors();
app.MapControllers();

app.Run();

public partial class Program
{
}
