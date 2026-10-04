namespace Bat.Core;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class IpAttribute : ValidationAttribute
{
    private static readonly Regex rgx = BatRegex.Ip1;

    public override bool IsValid(object value)
    {
        if (value != null) return rgx.IsMatch(value.ToString());

        return false;
    }
}