using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BitRaserApiProject.Swagger
{
    /// <summary>
    /// Swagger operation filter to add X-No-Encryption header parameter to all API endpoints
    /// Allows users to disable response encryption from Swagger UI for debugging
    /// </summary>
    public class NoEncryptionHeaderOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
     // Skip adding parameter for excluded endpoints (they're already not encrypted)
    var path = context.ApiDescription.RelativePath?.ToLowerInvariant() ?? string.Empty;
     
      var excludedPaths = new[]
            {
          "swagger",
     "health",
       "metrics",
     "favicon.ico",
           ".well-known"
       };

     if (excludedPaths.Any(p => path.Contains(p)))
      {
       return; // Don't add parameter for system endpoints
   }

            // Initialize parameters collection if null
   operation.Parameters ??= new List<OpenApiParameter>();

     // Add X-No-Encryption header parameter
            operation.Parameters.Add(new OpenApiParameter
  {
         Name = "X-No-Encryption",
       In = ParameterLocation.Header,
     Description = "Set to 'true' to disable response encryption for this request (for debugging purposes)",
             Required = false,
       Schema = new OpenApiSchema
           {
        Type = "string",
              Default = new Microsoft.OpenApi.Any.OpenApiString("false"),
           Enum = new List<Microsoft.OpenApi.Any.IOpenApiAny>
  {
     new Microsoft.OpenApi.Any.OpenApiString("false"),
              new Microsoft.OpenApi.Any.OpenApiString("true")
       }
    },
           Example = new Microsoft.OpenApi.Any.OpenApiString("true")
   });
 }
    }
}
