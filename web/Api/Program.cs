using Api.RPC;
using App;
using Infra;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddGrpc();
builder.Services.AddSingleton<MachineCommandDispatcher>();
builder.Services.AddInfra(builder.Configuration).AddApp();
builder.Services.AddOpenApi();

var app = builder.Build();
app.MapOpenApi();

app.MapGrpcService<MachineTelemetry>();

app.Run();
