using GrandCinema.Api.Endpoints.Auth;
using GrandCinema.Api.Endpoints.Screenings;
using GrandCinema.Api.OpenApi;
using GrandCinema.Api.Security;
using GrandCinema.Application;
using GrandCinema.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication();
builder.Services.AddAuthenticationRateLimiting();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
builder.Services.AddHealthChecks();

var app = builder.Build();

// Unexpected errors: generic Problem Details response (500), without any stack trace; the details go to the logs.
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.MapOpenApi().AllowAnonymous();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Grand Cinema API v1"));
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapScheduleEndpoints();
app.MapRegisterUserEndpoints();
app.MapAuthenticateUserEndpoints();
app.MapCurrentUserEndpoints();

app.MapGet("/", () => "Grand Cinema Api").AllowAnonymous();

app.Run();
