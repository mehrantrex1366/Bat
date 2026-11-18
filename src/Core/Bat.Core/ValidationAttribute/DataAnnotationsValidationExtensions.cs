namespace Bat.Core;

/// <summary>
/// Validates an object with its DataAnnotations attributes, including the .NET 11 <b>async</b> ones
/// (<c>AsyncValidationAttribute</c>, <c>IAsyncValidatableObject</c>), e.g. "national code not already registered" checks
/// that query a database. Use it in application services, consumers of queues, background jobs, ...
/// </summary>
public static class DataAnnotationsValidationExtensions
{
    /// <summary>Returns all validation errors (empty when valid). <paramref name="serviceProvider"/> is passed to the attributes (DB access, ...).</summary>
    public static async Task<IReadOnlyList<ValidationResult>> ValidateDataAnnotationsAsync(this object instance, IServiceProvider serviceProvider = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var results = new List<ValidationResult>();
        var context = new ValidationContext(instance, serviceProvider, items: null);
        await Validator.TryValidateObjectAsync(instance, context, results, validateAllProperties: true, cancellationToken);
        return results;
    }

    /// <summary><c>true</c> when <paramref name="instance"/> passes every (sync and async) DataAnnotations validation.</summary>
    public static async Task<bool> IsValidDataAnnotationsAsync(this object instance, IServiceProvider serviceProvider = null, CancellationToken cancellationToken = default)
        => (await instance.ValidateDataAnnotationsAsync(serviceProvider, cancellationToken)).Count == 0;

    /// <summary>Joins the error messages with <c>|</c> (the format of <c>ModelStateDictionary.GetModelError()</c> in Bat.AspNetCore).</summary>
    public static string JoinErrors(this IEnumerable<ValidationResult> results)
        => string.Join("|", results.Select(r => r.ErrorMessage));
}