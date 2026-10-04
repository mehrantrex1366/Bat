namespace Bat.Core;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class PersianDateAttribute : ValidationAttribute
{
    private static readonly Regex rgx = BatRegex.PersianDate;

    public override bool IsValid(object value)
    {
        if (value != null) return rgx.IsMatch(value.ToString());

        return false;
    }
}