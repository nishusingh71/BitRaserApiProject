using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using DSecureApi.Utilities;

namespace DSecureApi.Attributes
{
    /// <summary>
    /// Attribute to automatically decode Base64-encoded email parameters
    /// ✅ Apply to controller actions with email parameters
    /// ✅ Automatically decodes: email, parentEmail, subuserEmail, userEmail
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class DecodeBase64EmailAttribute : ActionFilterAttribute
    {
        private readonly string[] _emailParameterNames;

        /// <summary>
        /// Constructor with default email parameter names
        /// </summary>
        public DecodeBase64EmailAttribute(params string[] parameterNames)
        {
            _emailParameterNames = parameterNames.Length > 0 
                ? parameterNames 
                : new[] { "email", "parentEmail", "subuserEmail", "userEmail", "targetEmail" };
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            foreach (var paramName in _emailParameterNames)
            {
                if (context.ActionArguments.ContainsKey(paramName))
                {
                    var value = context.ActionArguments[paramName] as string;
                    
                    if (!string.IsNullOrEmpty(value))
                    {
                        // ✅ SMART DETECTION: Check if it's already a plain email or Base64
                        if (Base64EmailEncoder.IsValidEmail(value))
                        {
                            // Already a plain email - allow it (for Swagger UI compatibility)
                            // No action needed, keep the plain email
                            continue;
                        }

                        try
                        {
                            // Try to decode as Base64 email
                            var decodedEmail = Base64EmailEncoder.Decode(value);
                            
                            // Validate email format
                            if (!Base64EmailEncoder.IsValidEmail(decodedEmail))
                            {
                                context.Result = new BadRequestObjectResult(new
                                {
                                    error = "Invalid email format",
                                    parameter = paramName,
                                    message = $"Decoded value '{Base64EmailEncoder.MaskEmail(decodedEmail)}' is not a valid email",
                                    timestamp = DateTime.UtcNow
                                });
                                return;
                            }

                            // Replace with decoded value
                            context.ActionArguments[paramName] = decodedEmail;
                        }
                        catch (FormatException)
                        {
                            // Not valid Base64 - check if it's a plain email
                            if (Base64EmailEncoder.IsValidEmail(value))
                            {
                                // It's a plain email, allow it (Swagger UI compatibility)
                                continue;
                            }

                            context.Result = new BadRequestObjectResult(new
                            {
                                error = "Invalid email parameter",
                                parameter = paramName,
                                message = $"Parameter '{paramName}' must be either a valid email or Base64-encoded email",
                                hint = "For programmatic access: Use Base64EmailEncoder.Encode(email)",
                                note = "Plain emails are accepted for testing via Swagger UI",
                                timestamp = DateTime.UtcNow
                            });
                            return;
                        }
                    }
                }
            }

            base.OnActionExecuting(context);
        }
    }

    /// <summary>
    /// Specific attribute for email parameter
    /// </summary>
    public class DecodeEmailAttribute : DecodeBase64EmailAttribute
    {
        public DecodeEmailAttribute() : base("email") { }
    }

    /// <summary>
    /// Specific attribute for parentEmail parameter
    /// </summary>
    public class DecodeParentEmailAttribute : DecodeBase64EmailAttribute
    {
        public DecodeParentEmailAttribute() : base("parentEmail") { }
    }

    /// <summary>
    /// Specific attribute for subuserEmail parameter
    /// </summary>
    public class DecodeSubuserEmailAttribute : DecodeBase64EmailAttribute
    {
        public DecodeSubuserEmailAttribute() : base("subuserEmail") { }
    }

    /// <summary>
    /// Decode multiple email parameters
    /// </summary>
    public class DecodeAllEmailsAttribute : DecodeBase64EmailAttribute
    {
        public DecodeAllEmailsAttribute() : base("email", "parentEmail", "subuserEmail", "userEmail", "targetEmail") { }
    }
}
