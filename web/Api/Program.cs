using System.Text.Json.Serialization;
using Api.Realtime;
using Api.RPC;
using App;
using Infra;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddGrpc();
builder.Services.AddSingleton<MachineCommandDispatcher>();
builder.Services.AddSingleton<FactoryEventBroadcaster>();
builder.Services.AddInfra(builder.Configuration).AddApp();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.AllowTrailingCommas = true;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));
builder.Services.AddOpenApi();

var app = builder.Build();
app.UseCors();
app.MapControllers();
app.MapOpenApi();

app.MapGrpcService<MachineTelemetry>();

app.Run();
