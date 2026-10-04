namespace Bat.Core;

public static class MobileNumberExtensions
{
    public static bool IsMobileNumber(this string mobileNumber)
    {
        try
        {
            return BatRegex.MobileNumber.IsMatch(mobileNumber);
        }
        catch
        {
            return false;
        }
    }

    public static bool IsMobileNumber(this long mobileNumber)
    {
        try
        {
            return BatRegex.MobileNumber.IsMatch(mobileNumber.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        catch
        {
            return false;
        }
    }

    public static bool IsMciMobileNumber(string number)
    {
        if (!IsMobileNumber(number)) return false;

        if (!BatRegex.MciMobileNumber.IsMatch(number)) return false;

        return true;
    }

    public static bool IsIrancellMobileNumber(string number)
    {
        if (!IsMobileNumber(number)) return false;

        if (!BatRegex.IranCellMobileNumber.IsMatch(number)) return false;

        return true;
    }



    public static string ToMobileNumberPattern(this string mobileNumber)
    {
        string pattern = mobileNumber;

        if (string.IsNullOrEmpty(mobileNumber)) return string.Empty;
        if (mobileNumber.Length == 10) pattern = "98" + mobileNumber;
        else if (mobileNumber.Length == 11)
        {
            if (mobileNumber.StartsWith("0"))
            {
                pattern = string.Concat("98", mobileNumber.AsSpan(1));
            }
        }
        else if (mobileNumber.Length == 13)
            pattern = mobileNumber[1..];

        return pattern;
    }

    public static string ToMobileNumberPattern(this long mobileNumber) => $"0{mobileNumber}";

    public static long ToMobileNumber(this string mobileNumber)
    {
        if (long.TryParse(mobileNumber, out _))
            return long.Parse(mobileNumber);

        return 0;
    }

    public static long ToStandardMobileNumber(this string mobileNumber)
        => long.Parse(mobileNumber.StartsWith("98") ? mobileNumber[2..] : mobileNumber);

    public static long ToMobileNumber(this long mobileNumber)
    {
        var text = mobileNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!text.IsMobileNumber()) return 0;

        if (text.Length == 12) return mobileNumber;
        else if (text.Length == 11) return long.Parse("98" + text[1..]);
        else if (text.Length == 10) return long.Parse("98" + text);

        return 0;
    }

}