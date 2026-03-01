var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
var app = builder.Build();
app.MapOpenApi();
app.MapGet("/", () => "AresNexus Gateway API (Placeholder)");
app.Run();
