using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using DSecureApi.Utilities;

namespace DSecureApi.Filters
{
    /// <summary>
    /// Swagger filter to add Base64 encoding examples for email parameters
    /// Shows developers that emails must be Base64-encoded in URLs
    /// </summary>
    public class Base64EmailParameterFilter : IParameterFilter
    {
        public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
        {
            // Check if parameter name contains email-related keywords
            var emailKeywords = new[] { "email", "parentemail", "subuseremail", "useremail", "targetemail" };
            var paramNameLower = parameter.Name.ToLower();

            // Only apply to route parameters that contain email
            if (parameter.In == ParameterLocation.Path && 
                emailKeywords.Any(keyword => paramNameLower.Contains(keyword)))
            {
                // Add Base64 encoding information
                parameter.Description = "⚠️ **IMPORTANT:** Must be Base64-encoded email address.\n\n" +
                                      $"**Original Description:** {parameter.Description}\n\n" +
                                      "📧 **Example:**\n" +
                                      "- Plain email: `user@example.com`\n" +
                                      "- Base64 encoded: `dXNlckBleGFtcGxlLmNvbQ`\n\n" +
                                      "🔒 **Security:** Raw emails in URLs are **REJECTED** with 400 Bad Request.\n\n" +
                                      "💡 **How to encode:**\n" +
                                      "- JavaScript: `btoa(email).replace(/\\+/g, '-').replace(/\\//g, '_').replace(/=+$/, '')`\n" +
                                      "- C#: `Base64EmailEncoder.Encode(email)`\n" +
                                      "- Python: `base64.b64encode(email.encode()).decode().replace('+', '-').replace('/', '_').rstrip('=')`";

                // Set example to Base64-encoded email
                parameter.Example = new Microsoft.OpenApi.Any.OpenApiString("dXNlckBleGFtcGxlLmNvbQ");

                // Add schema examples
                parameter.Schema = new OpenApiSchema
                {
                    Type = "string",
                    Format = "base64",
                    Description = "Base64-encoded email address",
                    Example = new Microsoft.OpenApi.Any.OpenApiString("dXNlckBleGFtcGxlLmNvbQ"),
                    Pattern = "^[A-Za-z0-9_-]+$" // Base64 URL-safe pattern
                };
            }
        }
    }

    /// <summary>
    /// Swagger filter to add email encoding information to operations
    /// Adds warnings and examples to endpoint descriptions
    /// </summary>
    public class Base64EmailOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Check if operation has email parameters
            var hasEmailParams = operation.Parameters?.Any(p => 
                p.In == ParameterLocation.Path && 
                (p.Name.ToLower().Contains("email") || 
                 p.Name.ToLower().Contains("parentemail") || 
                 p.Name.ToLower().Contains("subuseremail"))) ?? false;

            if (hasEmailParams)
            {
                // Add email encoding warning to operation description
                var emailWarning = "\n\n---\n\n" +
                                 "## ⚠️ Email Encoding Required\n\n" +
                                 "All email parameters in the URL path **MUST** be Base64-encoded.\n\n" +
                                 "### Examples:\n\n" +
                                 "| Plain Email | Base64 Encoded |\n" +
                                 "|-------------|----------------|\n" +
                                 "| `user@example.com` | `dXNlckBleGFtcGxlLmNvbQ` |\n" +
                                 "| `admin@test.org` | `YWRtaW5AdGVzdC5vcmc` |\n" +
                                 "| `subuser@company.co.uk` | `c3VidXNlckBjb21wYW55LmNvLnVr` |\n\n" +
                                 "### How to Encode:\n\n" +
                                 "**JavaScript:**\n" +
                                 "```javascript\n" +
                                 "const encodeEmail = (email) => btoa(email)\n" +
                                 "    .replace(/\\+/g, '-')\n" +
                                 "    .replace(/\\//g, '_')\n" +
                                 "    .replace(/=+$/, '');\n\n" +
                                 "const encoded = encodeEmail('user@example.com');\n" +
                                 "// Result: 'dXNlckBleGFtcGxlLmNvbQ'\n" +
                                 "```\n\n" +
                                 "**C#:**\n" +
                                 "```csharp\n" +
                                 "using DSecureApi.Utilities;\n\n" +
                                 "var encoded = Base64EmailEncoder.Encode(\"user@example.com\");\n" +
                                 "// Result: \"dXNlckBleGFtcGxlLmNvbQ\"\n" +
                                 "```\n\n" +
                                 "**Python:**\n" +
                                 "```python\n" +
                                 "import base64\n\n" +
                                 "def encode_email(email):\n" +
                                 "    encoded = base64.b64encode(email.encode()).decode()\n" +
                                 "    return encoded.replace('+', '-').replace('/', '_').rstrip('=')\n\n" +
                                 "encoded = encode_email('user@example.com')\n" +
                                 "# Result: 'dXNlckBleGFtcGxlLmNvbQ'\n" +
                                 "```\n\n" +
                                 "### Error Handling:\n\n" +
                                 "❌ **Raw email in URL:**\n" +
                                 "```\n" +
                                 "GET /api/Users/user@example.com\n" +
                                 "Response: 400 Bad Request\n" +
                                 "{\n" +
                                 "  \"error\": \"Invalid URL format\",\n" +
                                 "  \"code\": \"EMAIL_NOT_ENCODED\"\n" +
                                 "}\n" +
                                 "```\n\n" +
                                 "✅ **Base64-encoded email:**\n" +
                                 "```\n" +
                                 "GET /api/Users/dXNlckBleGFtcGxlLmNvbQ\n" +
                                 "Response: 200 OK\n" +
                                 "```\n";

                operation.Description = (operation.Description ?? "") + emailWarning;

                // Add response examples
                if (!operation.Responses.ContainsKey("400"))
                {
                    operation.Responses.Add("400", new OpenApiResponse
                    {
                        Description = "Bad Request - Email not Base64-encoded",
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Example = new Microsoft.OpenApi.Any.OpenApiObject
                                {
                                    ["error"] = new Microsoft.OpenApi.Any.OpenApiString("Invalid URL format"),
                                    ["message"] = new Microsoft.OpenApi.Any.OpenApiString("Email addresses must be Base64-encoded in URLs"),
                                    ["code"] = new Microsoft.OpenApi.Any.OpenApiString("EMAIL_NOT_ENCODED"),
                                    ["hint"] = new Microsoft.OpenApi.Any.OpenApiString("Use Base64EmailEncoder.Encode(email) to encode emails")
                                }
                            }
                        }
                    });
                }
            }
        }
    }

    /// <summary>
    /// Swagger document filter to add global email encoding information
    /// Adds a section to Swagger UI explaining Base64 encoding requirement
    /// </summary>
    public class Base64EmailDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            // Add global description about email encoding
            swaggerDoc.Info.Description = (swaggerDoc.Info.Description ?? "") + 
                "\n\n---\n\n" +
                "## 🔒 Security: Base64 Email Encoding\n\n" +
                "**IMPORTANT:** All email addresses in URL paths MUST be Base64-encoded for security and GDPR compliance.\n\n" +
                "### Why Base64 Encoding?\n\n" +
                "1. ✅ **Privacy Protection** - Emails hidden from URL logs\n" +
                "2. ✅ **GDPR Compliance** - No PII in access logs or analytics\n" +
                "3. ✅ **Security** - Prevents email harvesting from server logs\n" +
                "4. ✅ **URL Safety** - Avoids special character issues\n\n" +
                "### Quick Reference:\n\n" +
                "| Language | Encoding Function |\n" +
                "|----------|------------------|\n" +
                "| JavaScript | `btoa(email).replace(/\\+/g, '-').replace(/\\//g, '_').replace(/=+$/, '')` |\n" +
                "| C# | `Base64EmailEncoder.Encode(email)` |\n" +
                "| Python | `base64.b64encode(email.encode()).decode().replace('+', '-').replace('/', '_').rstrip('=')` |\n\n" +
                "### Example Endpoints:\n\n" +
                "- `/api/Users/{email}` → `/api/Users/dXNlckBleGFtcGxlLmNvbQ`\n" +
                "- `/api/Sessions/by-email/{email}` → `/api/Sessions/by-email/dXNlckBleGFtcGxlLmNvbQ`\n" +
                "- `/api/Subuser/by-superuser/{parentEmail}` → `/api/Subuser/by-superuser/YWRtaW5AdGVzdC5vcmc`\n\n" +
                "See individual endpoint documentation for specific examples.\n";

            // Add example server for testing
            if (swaggerDoc.Servers == null || swaggerDoc.Servers.Count == 0)
            {
                swaggerDoc.Servers = new List<OpenApiServer>
                {
                    new OpenApiServer
                    {
                        Url = "https://localhost:7000",
                        Description = "Development Server (Use Base64-encoded emails)"
                    }
                };
            }
        }
    }
}
