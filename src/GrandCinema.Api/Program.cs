using GrandCinema.Api.Endpoints.Auth;
using GrandCinema.Api.Endpoints.Movies;
using GrandCinema.Api.Endpoints.Screenings;
using GrandCinema.Api.OpenApi;
using GrandCinema.Api.Security;
using GrandCinema.Application;
using GrandCinema.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication();
builder.Services.AddAuthenticationRateLimiting();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    // Behind CloudFront the API only sees its own server name over HTTP: without servers, Swagger calls the address of the page it is shown on.
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Servers?.Clear();
        return Task.CompletedTask;
    });
});
builder.Services.AddHealthChecks();

// Behind CloudFront, the visitor address arrives in X-Forwarded-For: the rate limiting must count per visitor, not per CloudFront server.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
    // Only CloudFront can reach the server (security group), and its addresses are not known in advance.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// First middleware: everything after it, the rate limiting included, sees the visitor address instead of CloudFront's.
app.UseForwardedHeaders();

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
app.MapMoviesShowingEndpoints();
app.MapRegisterUserEndpoints();
app.MapAuthenticateUserEndpoints();
app.MapCurrentUserEndpoints();

app.MapGet("/", () => "Grand Cinema Api").AllowAnonymous();

await app.RunAsync();
