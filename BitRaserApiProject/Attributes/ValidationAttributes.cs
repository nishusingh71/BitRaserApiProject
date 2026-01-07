using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace DSecureApi.Attributes
{
    /// <summary>
    /// Validates email format with strict RFC 5322 compliance
    /// </summary>
    public class StrictEmailAttribute : ValidationAttribute
    {
        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return new ValidationResult("Email is required");
            }

            var email = value.ToString()!;

            if (email.Length > 254)
            {
                return new ValidationResult("Email is too long (max 254 characters)");
            }

            if (!EmailRegex.IsMatch(email))
            {
                return new ValidationResult("Invalid email format");
            }

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validates password strength
    /// Minimum 8 characters, at least one uppercase, one lowercase, one digit, one special character
    /// </summary>
    public class StrongPasswordAttribute : ValidationAttribute
    {
        private const int MinLength = 8;
        private const int MaxLength = 128;

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return new ValidationResult("Password is required");
            }

            var password = value.ToString()!;

            if (password.Length < MinLength)
            {
                return new ValidationResult($"Password must be at least {MinLength} characters long");
            }

            if (password.Length > MaxLength)
            {
                return new ValidationResult($"Password must not exceed {MaxLength} characters");
            }

            if (!password.Any(char.IsUpper))
            {
                return new ValidationResult("Password must contain at least one uppercase letter");
            }

            if (!password.Any(char.IsLower))
            {
                return new ValidationResult("Password must contain at least one lowercase letter");
            }

            if (!password.Any(char.IsDigit))
            {
                return new ValidationResult("Password must contain at least one digit");
            }

            if (!password.Any(c => !char.IsLetterOrDigit(c)))
            {
                return new ValidationResult("Password must contain at least one special character");
            }

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validates string length with trimming
    /// </summary>
    public class SafeStringLengthAttribute : StringLengthAttribute
    {
        public SafeStringLengthAttribute(int maximumLength) : base(maximumLength)
        {
        }

        public override bool IsValid(object? value)
        {
            if (value == null)
            {
                return true;
            }

            var stringValue = value.ToString()?.Trim();
            return base.IsValid(stringValue);
        }
    }
}
