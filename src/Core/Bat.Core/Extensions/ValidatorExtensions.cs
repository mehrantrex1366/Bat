using System.Text;

namespace Bat.Core;

public class PasswordComplexityConfig
{
    public int MinLength { get; set; } = 8;
    public int MinUpperCase { get; set; } = 1;
    public int MinLowerCase { get; set; } = 1;
    public int MinNumbers { get; set; } = 1;
    public int MinSpecialChars { get; set; } = 1;
}

public static class ValidatorExtensions
{
    private static readonly HashSet<string> _pictureExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".heic"
    };

    public static bool IsIp(this string ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return false;

        if (!BatRegex.Ip1.IsMatch(ip)) return false;

        return true;
    }

    public static bool IsIp2(this string ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return false;

        if (!BatRegex.Ip2.IsMatch(ip)) return false;

        return true;
    }

    public static bool IsUrl(this string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;

        if (!BatRegex.Url.IsMatch(url)) return false;

        return true;
    }

    public static bool IsIban(this string iban)
    {
        if (string.IsNullOrWhiteSpace(iban)) return false;
        iban = iban.ToUpper().Trim().Replace(" ", string.Empty);
        if (!BatRegex.Iban.IsMatch(iban)) return false;

        string bank = iban.Substring(4, iban.Length - 4) + iban.Substring(0, 4);
        int asciiShift = 55;
        StringBuilder sb = new();
        foreach (char c in bank)
        {
            int v;
            if (char.IsLetter(c)) v = c - asciiShift;
            else v = int.Parse(c.ToString());
            sb.Append(v);
        }

        string checkSumString = sb.ToString();
        int checksum = int.Parse(checkSumString.Substring(0, 1));
        for (int i = 1; i < checkSumString.Length; i++)
        {
            int v = int.Parse(checkSumString.Substring(i, 1));
            checksum *= 10;
            checksum += v;
            checksum %= 97;
        }
        if (checksum != 1) return false;

        return true;
    }

    public static bool IsEmail(this string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;

        if (!BatRegex.Email.IsMatch(email)) return false;

        return true;
    }

    public static bool IsTime(this string time)
    {
        if (string.IsNullOrWhiteSpace(time)) return false;

        if (!BatRegex.Time.IsMatch(time)) return false;

        return true;
    }

    public static bool IsPersianDate(this string persianDate)
    {
        if (string.IsNullOrWhiteSpace(persianDate)) return false;

        if (!BatRegex.PersianDate.IsMatch(persianDate)) return false;

        return true;
    }

    public static bool IsPicture(this string fileNameWithExtension)
    {
        // Fixed: the old condition (!jpg || !jpeg || !png ...) was always true, so this always returned false.
        var fileExtention = Path.GetExtension(fileNameWithExtension);
        return _pictureExtensions.Contains(fileExtention ?? string.Empty);
    }

    public static bool IsDateTime(this string dateTime)
    {
        if (string.IsNullOrWhiteSpace(dateTime)) return false;

        if (!BatRegex.LatinDateTime.IsMatch(dateTime)) return false;

        return true;
    }

    public static bool IsNationalCode(this string nationalCode)
    {
        if (string.IsNullOrWhiteSpace(nationalCode))
            return false;

        if (nationalCode.Length != 10)
            return false;

        if (!long.TryParse(nationalCode, out long number))
            return false;

        var numbers = nationalCode.ToCharArray().Select(i => Convert.ToInt32(i.ToString())).ToList();
        var checkNumber = numbers.Last();
        numbers.RemoveAt(9);
        numbers.Reverse();
        var sum = 0;
        for (int i = 0; i < numbers.Count; i++)
            sum += numbers[i] * (i + 2);

        // Fixed: the old nested if/else (dangling else) never checked the "remaining >= 2" case,
        // so most invalid codes were accepted.
        var remaining = sum % 11;
        if (remaining < 2) return remaining == checkNumber;

        return (11 - remaining) == checkNumber;
    }

    public static bool IsNationalCode2(this string nationalCode)
    {
        if (string.IsNullOrWhiteSpace(nationalCode))
            return false;

        if (nationalCode.Length != 10)
            return false;

        if (!long.TryParse(nationalCode, out var _))
            return false;

        List<int> list = (from i in nationalCode.ToCharArray()
                          select Convert.ToInt32(i.ToString())).ToList();
        int controlNumber = list.Last();

        list.RemoveAt(9);
        list.Reverse();

        int sum = 0;
        for (int j = 0; j < list.Count; j++)
        {
            sum += list[j] * (j + 2);
        }

        int remainder = sum % 11;
        if (remainder < 2)
            return remainder == controlNumber;

        return (11 - remainder) == controlNumber;
    }

    public static bool IsBankCardNumber(this string bankCardNumber)
    {
        if (string.IsNullOrWhiteSpace(bankCardNumber)) return false;
        if (bankCardNumber.Length != 19) return false;

        if (!BatRegex.BankCardNumber.IsMatch(bankCardNumber)) return false;

        return true;
    }

    public static bool IsBankAccountNumber(this string accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber)) return false;
        if (accountNumber.Length <= 6) return false;

        return true;
    }

    public static bool IsBankSheba(this string sheba)
    {
        if (string.IsNullOrWhiteSpace(sheba)) return false;
        if (sheba.Length != 24) return false;

        return true;
    }

    public static bool IsCarPlate(this string CarPlate)
    {
        if (string.IsNullOrWhiteSpace(CarPlate)) return false;

        if (!BatRegex.CarPlate.IsMatch(CarPlate)) return false;

        return true;
    }

    public static bool IsComplexPassword(this string password, PasswordComplexityConfig config = default)
    {
        if (password is null) return false;
        config ??= new PasswordComplexityConfig();

        // Static Regex.IsMatch uses the framework's Regex cache, so repeated calls with the same config don't re-parse.
        var regexPattern = $"^(?=.*[A-Z]{{{config.MinUpperCase},}})(?=.*[a-z]{{{config.MinLowerCase},}})(?=.*[0-9]{{{config.MinNumbers},}})(?=.*[^A-Za-z0-9]{{{config.MinSpecialChars},}}).{{{config.MinLength},}}$";
        return Regex.IsMatch(password, regexPattern);
    }

}