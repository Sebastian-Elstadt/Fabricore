using Api.RPC;
using App;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddGrpc();
builder.Services.AddApp();
builder.Services.AddOpenApi();

var app = builder.Build();
app.MapOpenApi();

app.MapGrpcService<MachineTelemetry>();

app.Run();
