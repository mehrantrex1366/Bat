namespace Bat.Core;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class UrlAttribute : ValidationAttribute
{
    private static readonly Regex rgx = BatRegex.Url;

    public override bool IsValid(object value)
    {
        if (value != null) return rgx.IsMatch(value.ToString());

        return false;
    }
}