using FluentValidation.Results;
using OrderManager.Domain.Errors;

namespace OrderManager.Application.Extensions
{
    public static class ValidationResultExtensions
    {
        public static Error ToError(this ValidationResult validationResult)
        {
            return validationResult.Errors
                .Select(x => new Error(x.ErrorCode, x.ErrorMessage))
                .First();
        }
    }
}
