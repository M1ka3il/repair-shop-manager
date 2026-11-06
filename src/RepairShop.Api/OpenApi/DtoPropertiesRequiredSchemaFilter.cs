using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RepairShop.Api.OpenApi;

/// <summary>
/// Marks every property of a response DTO as <c>required</c> in the OpenAPI
/// document.
/// <para>
/// The DTOs are positional records: every property is set by the constructor,
/// so it is always present in the JSON — possibly as <c>null</c>, which the
/// schema already states via <c>nullable</c>. Without this filter Swashbuckle
/// leaves them optional, and the generated TypeScript types turn every field into
/// <c>field?: T</c>, forcing <c>!</c> or <c>?.</c> all over the frontend.
/// </para>
/// Request types are left alone: there, required-ness comes from
/// <c>[Required]</c> and is a real validation rule.
/// </summary>
public sealed class DtoPropertiesRequiredSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (!context.Type.Name.EndsWith("Dto", StringComparison.Ordinal)
            || schema is not OpenApiSchema concrete
            || concrete.Properties is not { Count: > 0 } properties)
        {
            return;
        }

        concrete.Required = new HashSet<string>(properties.Keys);
    }
}
