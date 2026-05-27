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
builder.Services.AddSingleton<SimulationSessionService>();

WebApplication app = builder.Build();

app.UseCors();
app.MapControllers();

app.Run();

public partial class Program
{
}
