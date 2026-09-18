using FluentValidation.Results;

namespace Identity.Sso.Application.Exceptions;

public class ApplicationValidationException : Exception
{
    public List<string> ValidationErrorList { get; set; } = [];

    public ApplicationValidationException(ValidationResult validationResult) : base("One or more validation errors occurred.")
    {
        ValidationErrorList = [.. validationResult.Errors.Select(e => e.ErrorMessage)];
    }

    public ApplicationValidationException(string message) : base(message)
    {
        ValidationErrorList.Add(message);
    }
}