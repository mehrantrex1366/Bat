namespace Bat.Core;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class BiggerThanZero : ValidationAttribute
{
    public override bool IsValid(object value)
    {
        // null is "not provided" (use [Required] for that); previously this threw NullReferenceException.
        if (value is null) return true;
        if (int.TryParse(value.ToString(), out var number) && number > 0) return true;

        return false;
    }
}