namespace Bat.Core;

/// <summary>
/// Shared, thread-safe <see cref="Regex"/> instances for <see cref="RegexPattern"/>.
/// Constructing a Regex parses the pattern every time; these are built once and reused.
/// </summary>
internal static class BatRegex
{
    private const RegexOptions Options = RegexOptions.Compiled | RegexOptions.CultureInvariant;

    internal static readonly Regex MobileNumber = new(RegexPattern.MobileNumber, Options);
    internal static readonly Regex MciMobileNumber = new(RegexPattern.MciMobileNumber, Options);
    internal static readonly Regex IranCellMobileNumber = new(RegexPattern.IranCellMobileNumber, Options);
    internal static readonly Regex Email = new(RegexPattern.Email, Options);
    internal static readonly Regex Url = new(RegexPattern.Url, Options);
    internal static readonly Regex PersianDate = new(RegexPattern.PersianDate, Options);
    internal static readonly Regex LatinDateTime = new(RegexPattern.LatinDateTime, Options);
    internal static readonly Regex Time = new(RegexPattern.Time, Options);
    internal static readonly Regex Ip1 = new(RegexPattern.Ip1, Options);
    internal static readonly Regex Ip2 = new(RegexPattern.Ip2, Options);
    internal static readonly Regex BankCardNumber = new(RegexPattern.BankCardNumber, Options);
    internal static readonly Regex CarPlate = new(RegexPattern.CarPlate, Options);
    internal static readonly Regex Iban = new("^[A-Z]{2}[0-9]{24}$", Options);
}