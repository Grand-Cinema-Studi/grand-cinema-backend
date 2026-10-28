using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GrandCinema.Api.OpenApi;

// Adds the "Authorize" button to Swagger UI: paste the access token returned by POST /api/tokens,
// and Swagger sends it in the Authorization header of every request.
internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    private const string SchemeName = "Bearer";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Token returned by POST /api/users or POST /api/tokens. No need to put Bearer in front of the token: Swagger adds it himself."
        };

        document.Security ??= new List<OpenApiSecurityRequirement>();
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SchemeName, document)] = new List<string>()
        });

        return Task.CompletedTask;
    }
}
