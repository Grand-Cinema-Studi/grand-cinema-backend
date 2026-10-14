using GrandCinema.Api.Endpoints.Screenings;
using GrandCinema.Application;
using GrandCinema.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Grand Cinema API v1"));
}

app.MapHealthChecks("/health");
app.MapScheduleEndpoints();

app.MapGet("/", () => "Grand Cinema Api");

app.Run();
