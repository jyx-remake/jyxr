using System.ComponentModel.DataAnnotations;

namespace Game.Core.Battle;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class NotWhiteSpaceAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) =>
        value is string text && !string.IsNullOrWhiteSpace(text);
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class ProbabilityAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) =>
        value is double number && double.IsFinite(number) && number is >= 0d and <= 1d;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class NonNegativeAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => value switch
    {
        int number => number >= 0,
        double number => double.IsFinite(number) && number >= 0d,
        _ => false,
    };
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class PositiveAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => value switch
    {
        int number => number > 0,
        double number => double.IsFinite(number) && number > 0d,
        _ => false,
    };
}

internal static class BattleEffectParameterValidation
{
    public static void Validate<TParameters>(string effectId, TParameters parameters)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(parameters!);
        if (Validator.TryValidateObject(parameters!, context, results, validateAllProperties: true))
        {
            return;
        }

        var errors = string.Join("; ", results.Select(FormatResult));
        throw new InvalidOperationException(
            $"Custom battle effect '{effectId}' parameters failed validation: {errors}");
    }

    private static string FormatResult(ValidationResult result)
    {
        var members = string.Join(", ", result.MemberNames);
        return string.IsNullOrEmpty(members)
            ? result.ErrorMessage ?? "unknown validation error"
            : $"{members}: {result.ErrorMessage}";
    }
}
