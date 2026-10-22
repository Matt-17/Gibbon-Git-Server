using System.ComponentModel.DataAnnotations;

namespace Gibbon.Git.Server.Middleware.Attributes;

/// <summary>
/// Validates a branch name with the rules of git check-ref-format --branch.
/// </summary>
public class BranchNameAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value is null || value is string name && IsValidBranchName(name.Trim()))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(Resources.Validation_Invalid_BranchName);
    }

    public static bool IsValidBranchName(string name)
    {
        if (string.IsNullOrEmpty(name) || name == "@" || name == "HEAD" || name.StartsWith('-') || name.StartsWith('/') || name.EndsWith('/') || name.EndsWith('.'))
        {
            return false;
        }

        if (name.Contains("..") || name.Contains("//") || name.Contains("@{"))
        {
            return false;
        }

        if (name.Any(c => char.IsControl(c) || c is ' ' or '~' or '^' or ':' or '?' or '*' or '[' or '\\'))
        {
            return false;
        }

        return name.Split('/').All(part => !part.StartsWith('.') && !part.EndsWith(".lock"));
    }
}
