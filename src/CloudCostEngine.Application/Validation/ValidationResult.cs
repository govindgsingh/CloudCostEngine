namespace CloudCostEngine.Application.Validation;

public sealed class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public IReadOnlyList<string> Errors { get; }

    private ValidationResult(IReadOnlyList<string> errors) => Errors = errors;

    public static ValidationResult Success() => new(Array.Empty<string>());

    public static ValidationResult Failure(params string[] errors) => new(errors);

    public static ValidationResult Failure(IReadOnlyList<string> errors) => new(errors);
}
