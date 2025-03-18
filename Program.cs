
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
builder.Services.AddHostedService<RpcClientBackgroundService>();

builder.Services.AddControllers();

var app = builder.Build();

app.UseHealthChecks("/health");
app.MapControllers();
await app.RunAsync();
