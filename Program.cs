
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();


var messagingConnectionString = builder.Configuration["ConnectionStrings:Messaging"];
builder.Services.AddHostedService(provider =>
    new RpcClientBackgroundService(messagingConnectionString));

builder.Services.AddControllers();
builder.Services.AddOpenTelemetry().WithTracing();
var app = builder.Build();

app.UseHealthChecks("/health");
app.MapControllers();
await app.RunAsync();
